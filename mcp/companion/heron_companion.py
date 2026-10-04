#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001, HERON-REVIT-CTX-007
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
The Heron Companion's server - a page beside Revit (D-108, docs/40).

PHASE 1 SHOWS; IT CHANGES NOTHING. The page says which Revit this chat is
using, which model and view are in front, and what is selected. It learns that
from the small file the add-in writes on every change (HeronLiveState.cs,
%LOCALAPPDATA%\\Heron\\live\\pid-N.json) - never by asking Revit through the
pipe. docs/40 section 4.2 says why: a page asking through the pipe would raise
the READING banner every second, queue work on Revit's own thread, and cut the
chat's own connection.

IT NEVER TALKS TO ANY AI. There are two doors and only two: HTTP from the page,
on 127.0.0.1, and reading files on this PC. Nothing here opens an outgoing
connection, and tests/test_companion.py fails if an import that could appears.

IT LIVES INSIDE THE MCP SERVER'S PROCESS, which speaks MCP to Claude Code over
STDOUT. So this file must never print. The standard library's request logging
is switched off, and nothing here writes to sys.stdout; the test runs a real
server and fails if one byte reaches it.

PROTECTED FROM OTHER WEBSITES AND PROGRAMS (docs/40 section 11):

  * 127.0.0.1 only, on a port Windows picks - no firewall prompt, no clash.
  * The Host header must name this server exactly (DNS rebinding).
  * The browser is opened by this process with a ONE-TIME code in the
    address. The page removes it from the address bar at once and trades it,
    once, within two minutes, for an HttpOnly SameSite=Strict cookie. The code
    never appears in a tool's reply, so it never reaches the chat. (It is in
    the query, not after #: Windows can drop the part after # when it hands an
    address to the default browser.)
  * Every /api call needs that cookie AND a custom header, which a page on
    another site cannot send without a pre-flight this server never answers.
    A POST must also carry this server's own Origin.
  * The page may not be framed, loads nothing from anywhere else, and runs no
    inline script (Content-Security-Policy).
"""

import http.server
import io
import json
import os
import re
import secrets
import socketserver
import sys
import threading
import time

HERE = os.path.dirname(os.path.abspath(__file__))
STATIC = os.path.join(HERE, "static")

sys.path.insert(0, os.path.join(HERE, "..", "client"))
import heron_bridge_client as bridge        # noqa: E402
import heron_config as configuration        # noqa: E402

#: How often the keeper looks at the switch and refreshes the note Revit reads.
KEEP_SECONDS = 2.0

#: The header every /api request must carry. Its value is not a secret - its
#: presence is the point: a cross-site request cannot add it without a
#: CORS pre-flight, and this server answers no pre-flight.
API_HEADER = "X-Heron-Companion"

#: How long the one-time code in the opened address stays usable.
PAIR_SECONDS = 120

#: The three files the page is made of - and the only files served.
PAGES = {
    "/": ("index.html", "text/html; charset=utf-8"),
    "/companion.js": ("companion.js", "text/javascript; charset=utf-8"),
    # The Loads panel's 3D view: a renderer of its own, so nothing is fetched
    # from the internet (docs/44 s12).
    "/loads3d.js": ("loads3d.js", "text/javascript; charset=utf-8"),
    "/companion.css": ("companion.css", "text/css; charset=utf-8"),
}

SECURITY_HEADERS = (
    ("Content-Security-Policy",
     "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; "
     "connect-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'"),
    ("X-Frame-Options", "DENY"),
    ("X-Content-Type-Options", "nosniff"),
    ("Referrer-Policy", "no-referrer"),
    ("Cache-Control", "no-store"),
)


def live_dir():
    """Where the add-in writes its live files - HeronPaths.Live, mirrored the
    way DISCOVERY_DIR mirrors HeronPaths.Bridges."""
    return os.path.join(bridge.local_app_data(), "Heron", "live")


class Activity(object):
    """
    PHASE 2: what this chat asked Heron to do, for the page (docs/40 section 7).

    One line per tool call - when, which tool, the FIRST line of its answer
    (at most 200 characters), how it ended, how long it took - in memory
    only, the last 500. Nothing new reaches disk: the add-in's audit trail
    already records every request that reached Revit (Golden Rule 14).

    HOW IT ENDED IS READ FROM THE ANSWER'S OWN WORDS, and only for display:
    an exception is "failed"; an answer saying nothing was sent to Revit, or
    that it refused, is "refused"; anything else is "ok". The chat reads the
    full answer; this is a label on a list.
    """

    LIMIT = 500
    FIRST_LINE = 200
    REFUSED = ("nothing has been sent to revit", "nothing was sent to revit",
               "has refused", "is refused", "was refused", "refused rather")
    # An answer that says the work did not happen - most tools say so in a
    # sentence rather than raising (Codex review of #362). Display only.
    FAILED = ("did not answer", "could not", "no answer", "was lost", "not connected",
              "rolled back", "failed", "unknown outcome")

    def __init__(self):
        self._lock = threading.Lock()
        self._items = []
        self._seq = 0

    #: The tools whose answer says whether Revit did the work. Every other
    #: tool is a report, and a report may quote "failed" as content - the
    #: gaps report's first line always does (Codex review of #362).
    REVIT_FACING = ("revit_", "companion_")

    @classmethod
    def outcome(cls, reply, error=None, tool=None):
        if error is not None:
            return "failed"
        if tool is not None and not str(tool).startswith(cls.REVIT_FACING):
            return "ok"
        # THE FIRST LINE ONLY: it is where a tool says what happened; the
        # lines after it are the answer, and may quote any word at all.
        lines = str(reply or "").strip().splitlines()
        text = (lines[0] if lines else "").lower()
        if any(word in text for word in cls.REFUSED):
            return "refused"
        if any(word in text for word in cls.FAILED):
            return "failed"
        return "ok"

    def record(self, tool, started, seconds, reply=None, error=None, outcome=None):
        first = str(error if error is not None else (reply or "")).strip().splitlines()
        line = first[0] if first else ""
        if len(line) > self.FIRST_LINE:
            line = line[:self.FIRST_LINE - 1] + "…"
        with self._lock:
            self._seq += 1
            self._items.append({"seq": self._seq, "at": started, "tool": tool,
                                "summary": line, "outcome": outcome or self.outcome(reply, error, tool),
                                "seconds": round(seconds, 1)})
            del self._items[:-self.LIMIT]

    def since(self, seq):
        with self._lock:
            return [dict(i) for i in self._items if i["seq"] > seq]


#: This process's activity list - one chat's.
ACTIVITY = Activity()


class Changes(object):
    """
    PHASE 3: the after-change tables (docs/40 section 21.1).

    When the chat makes a SETTINGS change - a category's graphics, a
    material's colour, a view filter's overrides - the values it used land
    here, and the page shows them as a small table the modeller can edit and
    apply again, with no message to the chat. Which changes qualify is the
    server's decision, read from the fragment's own card; this class only
    holds them and checks what comes back from the page.

    APPLY RE-RUNS THE SAME CAPABILITY, WITH THE SAME NAMES, and nothing else:
    the page may change a value, never add, drop or rename one, and a value
    may not carry a line break - values cross as one "name=value" per line,
    so a break would smuggle in a second value.
    """

    #: Thirty, not ten: Load from Revit offers one table per filter on the
    #: view, and a view with a dozen filters must not push its own first
    #: tables off the page.
    KEEP = 30
    LONGEST = 2000

    def __init__(self):
        self._lock = threading.Lock()
        self._cards = []
        self._next = 0
        #: Set by the MCP server: (capability, [(name, value)]) -> answer text.
        self.apply_hook = None
        #: Set by the MCP server: kind -> (answer text, [(capability, pairs)]).
        #: The page's Load from Revit - it READS the active view and nothing more.
        self.load_hook = None

    def clear(self):
        """The page's Clear tables: forget every table. Nothing reaches Revit."""
        with self._lock:
            self._cards = []

    def load(self, kind):
        """The page's Load from Revit: read the active view's settings of one
        kind - "filters" or "categories" - and put each on the page as its own
        table, replacing any table about the same thing. Reads only."""
        hook = self.load_hook
        if hook is None:
            return {"ok": False, "error": "The chat is not ready to read Revit yet."}
        if kind not in LOADS:
            return {"ok": False, "error": "Heron cannot load '%s' as tables." % kind}
        said, cards = hook(kind)
        return {"ok": True, "count": len(cards), "reply": str(said)[:1500]}

    #: A value that is a SETTING - a colour, an override string, a yes/no, a
    #: number - rather than WHICH THING the setting is on.
    _SETTING = re.compile(r"^(?:[0-9]{1,3} *, *[0-9]{1,3} *, *[0-9]{1,3}|.*=.*|true|false|"
                          r"yes|no|on|off|both|surface|cut|none|-?[0-9]+(?:[.][0-9]+)?)$",
                          re.IGNORECASE)

    @classmethod
    def subject(cls, pairs, names=None):
        """WHICH THING a change is about - the view, the filter, the
        categories - and never its settings. Two changes to the same thing
        share one table; changes to different things each keep their own. The
        first version keyed on the capability alone, so three filters offered
        one after another left only the last (the owner, 2026-09-29).

        WHICH INPUTS NAME THE THING is the fragment card's to say, in
        `companion-subject` - never guessed from a value's spelling, which
        took two materials named 100 and 200 for settings and merged their
        tables (Codex review of #362). The guess is kept only for a card that
        does not say."""
        if names:
            return tuple((n, v) for n, v in pairs if n in names)
        return tuple((n, v) for n, v in pairs if not cls._SETTING.match(str(v).strip()))

    def offer(self, capability, document, pairs, identity=None, subject_names=None):
        names = list(subject_names) if subject_names else None
        subject = self.subject(pairs, names)
        with self._lock:
            self._cards = [c for c in self._cards
                           if not (c["capability"] == capability and c["document"] == document
                                   and self.subject([(r["name"], r["value"]) for r in c["rows"]],
                                                    c.get("subject"))
                                   == subject)]
            self._next += 1
            self._cards.insert(0, {"id": self._next, "capability": capability,
                                   "document": document, "at": time.strftime("%H:%M:%S"),
                                   "rows": [{"name": n, "value": v} for n, v in pairs],
                                   "identity": list(identity) if identity else None,
                                   "subject": names,
                                   "last": None})
            del self._cards[self.KEEP:]

    def cards(self):
        with self._lock:
            return json.loads(json.dumps(self._cards))

    def check(self, card_id, values):
        """The card and the pairs to send, or (None, why)."""
        with self._lock:
            card = next((c for c in self._cards if c["id"] == card_id), None)
            if card is None:
                return None, "That table is no longer here - the chat has made newer changes."
            names = [r["name"] for r in card["rows"]]
            if not isinstance(values, list) or [v.get("name") if isinstance(v, dict) else None
                                                for v in values] != names:
                return None, "The table's rows do not match what Heron offered."
            pairs = []
            for v in values:
                value = v.get("value")
                if not isinstance(value, str) or len(value) > self.LONGEST \
                        or chr(10) in value or chr(13) in value:
                    return None, "A value must be one line of text."
                pairs.append((v["name"], value))
            return card, pairs

    def apply(self, card_id, values):
        hook = self.apply_hook
        if hook is None:
            return {"ok": False, "error": "The chat is not ready to apply anything yet."}
        card, pairs = self.check(card_id, values)
        if card is None:
            return {"ok": False, "error": pairs}
        def still():
            with self._lock:
                return any(c["id"] == card_id for c in self._cards)
        reply = hook(card["capability"], pairs, card.get("identity"), still)
        outcome = Activity.outcome(reply)
        with self._lock:
            for c in self._cards:
                if c["id"] == card_id:
                    if outcome == "ok":
                        c["rows"] = [{"name": n, "value": v} for n, v in pairs]
                    c["last"] = {"at": time.strftime("%H:%M:%S"), "outcome": outcome,
                                 "reply": str(reply)[:1500]}
        return {"ok": True, "outcome": outcome, "reply": str(reply)[:1500]}


#: This process's after-change tables - one chat's.
CHANGES = Changes()


# ------------------------------------------------------------ Load from Revit
# The page's Load buttons turn what two READ fragments say about the active
# view into the values the matching SETTINGS capability takes. Both readers
# answer as one string of fixed shape, written by Heron's own fragments:
# REPORT_VIEW_FILTERS' filterSettings and REPORT_CATEGORY_OVERRIDES'
# overrideList. A part neither reader below recognises is left out of the
# table, never guessed at - and every table is applied with the filter's
# other settings kept, so a part left out is a part left as it is.

#: What each Load button reads, and the capability its tables apply.
LOADS = {"filters": ("REPORT_VIEW_FILTERS", "APPLY_VIEW_FILTER"),
         "categories": ("REPORT_CATEGORY_OVERRIDES", "SET_CATEGORY_GRAPHICS")}

_RGB = re.compile(r"^(?:RGB)?\(?([0-9]{1,3}),([0-9]{1,3}),([0-9]{1,3})\)?$")


def _pattern(name):
    """A pattern as the override parser takes it: Revit's solid fill and
    solid line as `solid`, anything else by its name."""
    name = name.strip()
    return "solid" if name.lower() in ("<solid fill>", "solid", "solid fill") else name


def _colour(text):
    m = _RGB.match(text.strip())
    return "%s,%s,%s" % m.groups() if m else None


def _cell(text, colour_key, pattern_key, weight_key=None, visible_key=None):
    """One cell of the Filters tab - "pattern X, colour r,g,b, weight n" or
    "<Solid fill>, colour r,g,b, Visible UNTICKED" - as override settings."""
    out = []
    text = text.strip()
    if text == "no override":
        return out
    for bit in [b.strip() for b in text.split(", ") if b.strip()]:
        if bit.startswith("colour "):
            rgb = _colour(bit[7:])
            if rgb:
                out.append("%s=%s" % (colour_key, rgb))
        elif bit.startswith("weight ") and weight_key:
            if bit[7:].strip().isdigit():
                out.append("%s=%s" % (weight_key, bit[7:].strip()))
        elif bit == "Visible UNTICKED" and visible_key:
            out.append("%s=false" % visible_key)
        elif bit.startswith("pattern "):
            # A line cell names its pattern "pattern X"...
            out.append("%s=%s" % (pattern_key, _pattern(bit[8:])))
        else:
            # ...a fill cell names it bare, first: "<Solid fill>, colour ...".
            out.append("%s=%s" % (pattern_key, _pattern(bit)))
    return out


def filter_cards(view, text):
    """REPORT_VIEW_FILTERS' filterSettings -> one APPLY_VIEW_FILTER table per
    filter: [(capability, [(name, value)])]."""
    cards = []
    for line in (text or "").splitlines():
        m = re.match(r"^  '(.+)' - (.*)\.$", line)
        if not m or m.group(2).startswith("could not be read"):
            continue
        name, parts = m.group(1), m.group(2).split("; ")
        overrides, enabled, visible = [], "", "true"
        cut_applies = not any(p.startswith("cut not applicable") for p in parts)
        for part in parts:
            if part.startswith("Enable Filter "):
                enabled = {"ON": "true", "OFF": "false"}.get(part[14:], "")
            elif part.startswith("Visibility "):
                visible = "true" if part[11:].startswith("ON") else "false"
            elif part.startswith("Projection/Surface Lines: "):
                overrides += _cell(part[26:], "projection-line-colour", "projection-line-pattern",
                                   "projection-line-weight")
            elif part.startswith("Surface Patterns: foreground "):
                fore, _, back = part[29:].partition(" / background ")
                overrides += _cell(fore, "surface-foreground-colour", "surface-foreground-pattern",
                                   visible_key="surface-foreground-visible")
                overrides += _cell(back, "surface-background-colour", "surface-background-pattern",
                                   visible_key="surface-background-visible")
            elif part.startswith("Transparency ") and part[13:].isdigit():
                overrides.append("transparency=%s" % part[13:])
            elif part.startswith("Cut Lines: ") and cut_applies:
                overrides += _cell(part[11:], "cut-line-colour", "cut-line-pattern", "cut-line-weight")
            elif part.startswith("Cut Patterns: foreground ") and cut_applies:
                fore, _, back = part[25:].partition(" / background ")
                overrides += _cell(fore, "cut-foreground-colour", "cut-foreground-pattern",
                                   visible_key="cut-foreground-visible")
                overrides += _cell(back, "cut-background-colour", "cut-background-pattern",
                                   visible_key="cut-background-visible")
            elif part.startswith("Halftone "):
                overrides.append("halftone=%s" % ("true" if part[9:] == "ON" else "false"))
        cards.append(("APPLY_VIEW_FILTER", [
            ("view", view), ("filter", name),
            ("overrides", "; ".join(overrides) or "none"),
            ("visible", visible), ("enabled", enabled),
            ("keepOtherSettings", "true"), ("solidFill", "")]))
    return cards


#: REPORT_CATEGORY_OVERRIDES' words -> the override parser's. Longest first,
#: so "cut line RGB" is never read as "line RGB".
_CATEGORY_PARTS = (("cut line RGB", "cut-line-colour"), ("line RGB", "projection-line-colour"),
                   ("surface pattern ", "surface-foreground-pattern"),
                   ("surface RGB", "surface-foreground-colour"),
                   ("cut pattern ", "cut-foreground-pattern"), ("cut RGB", "cut-foreground-colour"),
                   ("line weight ", "projection-line-weight"))


def category_cards(view, text):
    """REPORT_CATEGORY_OVERRIDES' overrideList -> one SET_CATEGORY_GRAPHICS
    table per overridden category."""
    cards = []
    for entry in (text or "").split("  ||  "):
        name, sep, rest = entry.strip().partition(": ")
        if not sep or not name:
            continue
        overrides = []
        for bit in [b.strip() for b in rest.split(", ") if b.strip()]:
            if bit == "halftone":
                overrides.append("halftone=true")
                continue
            m = re.match(r"^([0-9]{1,3})% transparent$", bit)
            if m:
                overrides.append("transparency=%s" % m.group(1))
                continue
            for words, key in _CATEGORY_PARTS:
                if bit.startswith(words):
                    value = bit[len(words):].strip()
                    if key.endswith("-colour"):
                        value = _colour(value)
                    elif key.endswith("-pattern"):
                        value = None if value == "set" else _pattern(value)
                    if value:
                        overrides.append("%s=%s" % (key, value))
                    break
        if overrides:
            cards.append(("SET_CATEGORY_GRAPHICS", [
                ("view", view), ("categories", name), ("overrides", "; ".join(overrides))]))
    return cards


class Tables(object):
    """
    PHASE 3: the element table a chat opens with revit_edit_table (docs/40
    section 8) - one at a time, the newest replacing the last.

    WHAT WAS SHOWN IS KEPT HERE, AND IT IS WHAT IS SENT AS "WAS". The page
    says which cell and what the new value is; the old value comes from this
    copy, never from the page. The write fragment then refuses the whole
    Apply if Revit no longer holds it (Article 12c). Only a cell the reader
    marked editable may be sent, and a value is one line of text.
    """

    LONGEST = 2000

    def __init__(self):
        self._lock = threading.Lock()
        self._table = None
        self._next = 0
        #: Set by the MCP server: [(id, uniqueId, name, was, new)] -> (text, result).
        self.apply_hook = None

    def open(self, document, table, identity=None):
        with self._lock:
            self._next += 1
            self._table = {"id": self._next, "document": document,
                           "identity": list(identity) if identity else None,
                           "at": time.strftime("%H:%M:%S"),
                           "columns": list(table.get("columns") or []),
                           "rows": list(table.get("rows") or []),
                           "truncated": table.get("truncated") or 0,
                           "last": None}

    def current(self):
        with self._lock:
            return json.loads(json.dumps(self._table)) if self._table else None

    def check(self, table_id, changes):
        """The rows to send, or (None, why)."""
        rows, why, _identity = self.rows_and_model(table_id, changes)
        return rows, why

    def rows_and_model(self, table_id, changes):
        """(rows, why, identity) - the rows and the model they were read in,
        taken under ONE hold of the lock. Looked up separately, a table the
        chat opened in between could pair these rows with ITS model, and the
        model guard would approve the wrong target (Codex review of #362)."""
        with self._lock:
            table = self._table
            if table is None or table["id"] != table_id:
                return None, "That table is no longer open - the chat has opened a newer one.", None
            if not isinstance(changes, list) or not changes:
                return None, "No cell was changed, so there is nothing to apply.", None
            rows = {r.get("id"): r for r in table["rows"]}
            out = []
            for change in changes:
                if not isinstance(change, dict):
                    return None, "The changes could not be read.", None
                row = rows.get(change.get("id"))
                name, value = change.get("name"), change.get("value")
                cell = (row or {}).get("cells", {}).get(name) if row else None
                if cell is None:
                    return None, "A changed cell is not in the table Heron opened.", None
                if not cell.get("editable"):
                    return None, "A changed cell is one Heron marked as not editable.", None
                if not isinstance(value, str) or len(value) > self.LONGEST \
                        or chr(10) in value or chr(13) in value:
                    return None, "A value must be one line of text.", None
                out.append((row["id"], row["uniqueId"], name, cell.get("value") or "", value,
                            cell.get("raw") or ""))
            return out, None, (list(table["identity"]) if table.get("identity") else None)

    def apply(self, table_id, changes):
        hook = self.apply_hook
        if hook is None:
            return {"ok": False, "error": "The chat is not ready to apply anything yet."}
        rows, why, identity = self.rows_and_model(table_id, changes)
        if rows is None:
            return {"ok": False, "error": why}
        text, result = hook(rows, identity)
        outcome = Activity.outcome(text)
        applied = bool(result and result.get("applied"))
        with self._lock:
            table = self._table
            if table is not None and table["id"] == table_id:
                if not applied:
                    # A cell changed in Revit since the table was read: what
                    # Revit holds now becomes the value this table checks
                    # against. The page keeps the modeller's edits, so Apply
                    # again sends them against the model as it is now.
                    for s in (result or {}).get("stale") or []:
                        if "now" not in s:
                            continue
                        for row in table["rows"]:
                            cell = row.get("cells", {}).get(s.get("name"))
                            if str(row["id"]) == str(s.get("id")) and cell is not None:
                                cell["value"] = s["now"]
                                cell["raw"] = s.get("nowRaw")
                if applied:
                    back = {(b.get("id"), b.get("name")): b
                            for b in result.get("readBack") or []}
                    for row in table["rows"]:
                        for name, cell in row.get("cells", {}).items():
                            if (row["id"], name) in back:
                                cell["value"] = back[(row["id"], name)].get("value")
                                cell["raw"] = back[(row["id"], name)].get("raw")
                table["last"] = {"at": time.strftime("%H:%M:%S"),
                                 "outcome": "ok" if applied else ("refused" if outcome != "failed" else "failed"),
                                 "reply": str(text)[:1500],
                                 "stale": (result or {}).get("stale") or []}
        return {"ok": True, "applied": applied, "outcome": outcome,
                "reply": str(text)[:1500], "result": result}


#: This process's editable table - one chat's.
TABLES = Tables()


class LoadsPanel(object):
    """
    The Loads panel a chat opens with revit_building_loads (docs/44 section 7)
    - one building at a time, the newest replacing the last.

    IT HOLDS DATA AND CALLS HOOKS - NOTHING ELSE (README rule 4). Every number
    on it was worked out in brain/ (heron_building_loads); Recalculate, Report
    and Finalize each call a hook the MCP server sets, and with none set they
    answer that the chat is gone and do nothing. The take-off a run was made
    from is kept here so Recalculate never asks Revit again.
    """

    GONE = "the chat that opened this is no longer connected - nothing was done"

    def __init__(self):
        self._lock = threading.Lock()
        self._held = None
        self._takeoff = None
        #: Set by the MCP server. recalculate_hook(takeoff, inputs, identity)
        #: -> brain answer; report_hook(panel) -> {"ok", "said", "html", "pdf",
        #: "csv"}; finalize_hook(takeoff, result, identity) -> {"ok", "said",
        #: "finalized"}; runs_hook(document) -> [run]; run_hook(document, id)
        #: -> a kept run or None; confirm_hook(takeoff, result) -> {"ok",
        #: "said", "result"}.
        self.recalculate_hook = None
        self.report_hook = None
        self.finalize_hook = None
        self.runs_hook = None
        self.run_hook = None
        self.confirm_hook = None

    def open(self, document, answer, identity=None, read_at=None):
        result = answer.get("result") or {}
        inputs = answer.get("inputs") or {}
        with self._lock:
            self._takeoff = answer.get("takeoff")
            self._held = {
                "document": document, "at": time.strftime("%H:%M:%S"),
                # When the MODEL was read - kept through a Recalculate, which
                # works on the take-off already held and reads nothing.
                "read_at": read_at or time.strftime("%H:%M:%S"),
                "identity": list(identity) if identity else None,
                "qa": list(answer.get("qa") or []), "asked": list(answer.get("asked") or []),
                "project": dict(inputs.get("project") or {}),
                "profiles": dict(inputs.get("profiles") or {}),
                "overrides": dict(inputs.get("overrides") or {}),
                # The hour-by-hour rows stay in the kept run; the page shows peaks.
                "spaces": [{k: v for k, v in s.items() if k != "cooling"}
                           for s in result.get("spaces") or []],
                "peaks": {str(s.get("id")): (s.get("cooling") or {}).get("peak")
                          for s in result.get("spaces") or []},
                "components": {str(s.get("id")): (s.get("cooling") or {}).get("components")
                               for s in result.get("spaces") or []},
                "zones": list(result.get("zones") or []),
                "building": result.get("building"), "notes": list(result.get("notes") or []),
                "run_id": result.get("run_id"), "units": result.get("units"),
                "said": answer.get("said"), "report": None, "finalized": None,
                "confirmed": bool(answer.get("confirmed")),
                "summary": answer.get("summary"),
                # The 3D view's data, served on its own route: it is the
                # biggest thing the panel holds and the page asks for it only
                # when the 3D view is opened or the run changed.
                "view": answer.get("view"),
                "result": answer.get("result")}

    def current(self):
        with self._lock:
            if not self._held:
                return None
            held = dict(self._held)
        held.pop("result", None)
        held["has_view"] = bool(held.pop("view", None))
        return json.loads(json.dumps(held))

    def view(self):
        """The 3D view's data for the run on the page - drawn by the page, made in brain/."""
        with self._lock:
            got = self._held.get("view") if self._held else None
            return json.loads(json.dumps(got)) if got else None

    def confirm(self):
        """Gate 1: the modeller says the take-off on the page is right (docs/44 s6)."""
        hook = self.confirm_hook
        if hook is None:
            return {"ok": False, "said": self.GONE}
        takeoff, result, _identity = self._snapshot()
        if takeoff is None or not result:
            return {"ok": False, "said": "nothing has been calculated yet, so there is no "
                                         "take-off to confirm"}
        got = hook(takeoff, result) or {}
        if got.get("ok"):
            with self._lock:
                if self._held is not None and self._held.get("result") is result:
                    self._held["confirmed"] = True
        return {"ok": bool(got.get("ok")), "said": got.get("said")}

    def _snapshot(self):
        with self._lock:
            if not self._held:
                return None, None, None
            return (self._takeoff, self._held.get("result"),
                    list(self._held["identity"]) if self._held.get("identity") else None)

    def recalculate(self, body):
        hook = self.recalculate_hook
        if hook is None:
            return {"ok": False, "said": self.GONE}
        if not isinstance(body, dict) or not all(
                isinstance(body.get(k, {}), dict) for k in ("project", "profiles", "overrides")):
            return {"ok": False, "said": "the inputs could not be read - nothing was calculated"}
        takeoff, _result, identity = self._snapshot()
        if takeoff is None:
            return {"ok": False, "said": "no building is open on this page - ask the chat to "
                                         "calculate the loads first"}
        with self._lock:
            document = self._held["document"]
            read_at = self._held.get("read_at")
        answer = hook(takeoff, {"project": body.get("project") or {},
                                "profiles": body.get("profiles") or {},
                                "overrides": body.get("overrides") or {}}, identity)
        if not isinstance(answer, dict) or answer.get("takeoff") is None:
            return {"ok": False, "said": (answer or {}).get("said") or self.GONE}
        self.open(document, answer, identity, read_at)
        return {"ok": True, "said": answer.get("said"), "loads": self.current()}

    def report(self):
        hook = self.report_hook
        if hook is None:
            return {"ok": False, "said": self.GONE}
        takeoff, result, _identity = self._snapshot()
        if takeoff is None or not result:
            return {"ok": False, "said": "nothing has been calculated yet, so there is no "
                                         "report to write"}
        got = hook(takeoff, result) or {}
        with self._lock:
            if self._held is not None:
                self._held["report"] = {k: got.get(k) for k in ("html", "pdf", "csv",
                                                                "takeoff_csv", "said")}
        return dict(got, ok=bool(got.get("ok")))

    def finalize(self):
        hook = self.finalize_hook
        if hook is None:
            return {"ok": False, "said": self.GONE}
        takeoff, result, identity = self._snapshot()
        if takeoff is None or not result:
            return {"ok": False, "said": "nothing has been calculated yet, so nothing was "
                                         "written"}
        got = hook(takeoff, result, identity) or {}
        with self._lock:
            if self._held is not None:
                self._held["finalized"] = {"at": time.strftime("%H:%M:%S"),
                                           "said": got.get("said"),
                                           "values": got.get("finalized")}
        return dict(got, ok=bool(got.get("ok")))

    def runs(self):
        hook = self.runs_hook
        with self._lock:
            document = self._held["document"] if self._held else None
        if hook is None or document is None:
            return []
        return hook(document) or []

    def earlier(self, run_id):
        """A kept run, read-only - never made the current one."""
        hook = self.run_hook
        with self._lock:
            document = self._held["document"] if self._held else None
        if hook is None or document is None or not isinstance(run_id, str) \
                or not run_id.replace("-", "").isdigit():
            return None
        return hook(document, run_id)


#: This process's Loads panel - one chat's.
LOADS_PANEL = LoadsPanel()


def companion_dir():
    """Where each chat leaves the note the Companion button in Revit reads -
    HeronPaths.Companion, mirrored (D-109)."""
    return os.path.join(bridge.local_app_data(), "Heron", "companion")


def enabled(path=None):
    """The Companion switch, read fresh: companion.enabled in heron.config,
    flipped from the Companion button's arrow in Revit. The add-in's own
    idea of true, so the two halves cannot disagree."""
    return configuration.truthy(configuration.load(path), "companion.enabled")


#: How long one answer to "is that Revit still running?" is trusted. The
#: check asks Windows for the process list, which can take a second on a busy
#: PC; the page asks every second, so the answer is kept a while.
ALIVE_SECONDS = 15.0

_alive_cache = {}
_alive_lock = threading.Lock()


def revit_alive(pid):
    """Is a Revit still running at that process id? The bridge client's own
    check - process id AND program name, so a reused id is not taken for
    Revit - asked at most every ALIVE_SECONDS, and never through the pipe.
    Unknown counts as alive, as it does in the client (Codex review of #362:
    a Revit that was killed leaves its discovery and live files behind)."""
    now = time.time()
    with _alive_lock:
        seen = _alive_cache.get(pid)
        if seen and now - seen[0] < ALIVE_SECONDS:
            return seen[1]
    answer = bridge.bridge_process_is_running(pid) is not False
    with _alive_lock:
        _alive_cache[pid] = (now, answer)
    return answer


def connected_pids(discovery_dir=None, is_alive=None):
    """Process ids with a discovery file whose Revit is still running. A live
    file whose Revit has no discovery file, or whose process is gone, is left
    over from a crash."""
    is_alive = is_alive or revit_alive
    folder = discovery_dir or bridge.DISCOVERY_DIR
    try:
        names = os.listdir(folder)
    except OSError:
        return set()
    found = set()
    for name in names:
        if name.endswith(".json"):
            pid = bridge.pid_from_filename(name)
            if pid is not None and is_alive(pid):
                found.add(pid)
    return found


def read_live(folder=None, discovery_dir=None, is_alive=None):
    """Every readable live file whose Revit is still connected, by pid.

    A file that is missing, half-written or not JSON is skipped rather than
    shown: the add-in writes by replace, so a bad read is a race, and the next
    poll a second later sees the whole file."""
    folder = folder or live_dir()
    alive = connected_pids(discovery_dir, is_alive)
    out = {}
    try:
        names = os.listdir(folder)
    except OSError:
        return out
    for name in names:
        if not (name.startswith("pid-") and name.endswith(".json")):
            continue
        try:
            pid = int(name[4:-5])
        except ValueError:
            continue
        if pid not in alive:
            continue
        path = os.path.join(folder, name)
        try:
            with io.open(path, encoding="utf-8") as fh:
                record = json.load(fh)
            modified = os.path.getmtime(path)
        except (OSError, ValueError):
            continue
        if not isinstance(record, dict) or record.get("pid") != pid:
            continue
        record["updatedSecondsAgo"] = max(0, int(time.time() - modified))
        out[pid] = record
    return out


def state(bound_pid, folder=None, discovery_dir=None, is_alive=None):
    """What the page shows: the Revit this chat uses, and how that was decided.

    NEVER A GUESS AMONG SEVERAL (Article 12a). With this chat bound, it is that
    Revit or nothing. Unbound, one connected Revit is shown and labelled as the
    only one; several are listed and none is picked."""
    live = read_live(folder, discovery_dir, is_alive)
    if bound_pid is not None:
        record = live.get(bound_pid)
        return {"choice": "bound" if record else "bound-gone", "revit": record,
                "others": len(live) - (1 if record else 0)}
    if len(live) == 1:
        return {"choice": "only", "revit": list(live.values())[0], "others": 0}
    if not live:
        return {"choice": "none", "revit": None, "others": 0}
    listed = [{"revitVersion": r.get("revitVersion"),
               "document": (r.get("document") or {}).get("title")}
              for r in live.values()]
    return {"choice": "several", "revit": None, "others": len(live), "list": listed}


class _Server(socketserver.ThreadingMixIn, http.server.HTTPServer):
    daemon_threads = True
    allow_reuse_address = False


class Companion(object):
    """One chat's Companion. Started on request; lives as long as the chat."""

    def __init__(self, bound_pid=lambda: None, folder=None, discovery_dir=None, is_alive=None):
        self._bound_pid = bound_pid
        self._is_alive = is_alive
        self._folder = folder
        self._discovery_dir = discovery_dir
        self._lock = threading.Lock()
        # START AND STOP ARE ONE STEP EACH: the keeper thread and a tool call
        # may both start the page at once, and two servers bound with only
        # one recorded would leave the other reachable after the switch
        # turns the Companion off (Codex review of #362).
        self._life = threading.RLock()
        # WHEN THIS CHAT STARTED, which never changes - the Revit button
        # picks the newest chat by it. The note's own file time is a
        # heartbeat, refreshed every two seconds, so it cannot say that.
        self._born = int(time.time())
        self._codes = {}          # one-time code -> expiry (time.time())
        self._sessions = set()    # cookie values issued
        self._server = None
        self._thread = None
        # THE STANDING CODE: the one the note for Revit's button carries. It
        # has no clock - the button may be pressed hours later - so it is
        # single use instead, and replaced the moment it is redeemed.
        self._standing = None
        self._note = None
        self._note_text = None
        self._seen = 0.0           # when a page last asked for the state

    # ------------------------------------------------------------ lifecycle

    @property
    def running(self):
        return self._server is not None

    @property
    def port(self):
        return self._server.server_address[1] if self._server else None

    def start(self):
        with self._life:
            if self._server is not None:
                return self.port
            companion = self

            class Handler(_Handler):
                owner = companion

            self._server = _Server(("127.0.0.1", 0), Handler)
            self._thread = threading.Thread(target=self._server.serve_forever,
                                            name="heron-companion", daemon=True)
            self._thread.start()
            return self.port

    def stop(self):
        with self._life:
            # The page that was polling is gone with its server; the next
            # offer must open a new tab (Codex review of #362).
            self._seen = 0.0
            server, self._server = self._server, None
            if server is not None:
                server.shutdown()
                server.server_close()

    def pairing_address(self):
        """A fresh address carrying a one-time code. For the browser only -
        never put it in a reply to the chat."""
        self.start()
        code = secrets.token_urlsafe(24)
        with self._lock:
            now = time.time()
            self._codes = {c: t for c, t in self._codes.items() if t > now}
            self._codes[code] = now + PAIR_SECONDS
        return "http://127.0.0.1:%d/?pair=%s" % (self.port, code)

    # --------------------------------------------------------------- checks

    def redeem(self, code):
        with self._lock:
            if code and code == self._standing:
                self._standing = None           # used: the next publish mints another
                expiry = time.time() + 1
            else:
                expiry = self._codes.pop(code, None)
            if expiry is None or expiry < time.time():
                return None
            session = secrets.token_urlsafe(32)
            self._sessions.add(session)
            return session

    # ------------------------------------------------ the note for Revit (D-109)

    def publish(self, folder=None):
        """Write, or refresh, this chat's note: its port and a standing one-time
        code, so the Companion button in Revit can open the page. Rewritten
        only when something in it changed."""
        folder = folder or companion_dir()
        with self._lock:
            if not self._standing:
                self._standing = secrets.token_urlsafe(24)
            try:
                bound = self._bound_pid()
            except Exception:                        # noqa: BLE001 - display only
                bound = None
            text = json.dumps({"format": "1", "mcpPid": str(os.getpid()),
                               "port": str(self.port), "code": self._standing,
                               "started": str(self._born),
                               "revitPid": "" if bound is None else str(bound)})
            path = os.path.join(folder, "chat-%d.json" % os.getpid())
            if text == self._note_text and os.path.exists(path):
                # TOUCHED EVERY LOOK: the Revit button ignores a note older
                # than 30 seconds, so a chat that crashed - whose process id
                # Windows may give to something else - is never mistaken for
                # a running one (Codex review of #362).
                os.utime(path, None)
                return path
            os.makedirs(folder, exist_ok=True)
            partial = path + ".tmp"
            with io.open(partial, "w", encoding="utf-8") as fh:
                fh.write(text)
            os.replace(partial, path)
            self._note, self._note_text = path, text
            return path

    def unpublish(self):
        with self._lock:
            path, self._note, self._note_text, self._standing = self._note, None, None, None
        if path:
            try:
                os.remove(path)
            except OSError:
                pass

    def keep_once(self, config_path=None, folder=None):
        """One look at the switch: on -> the page is up and the note is fresh;
        off -> the page is down and the note is gone. Heron itself is never
        touched either way."""
        if enabled(config_path):
            self.start()
            self.publish(folder)
            return True
        self.unpublish()
        self.stop()
        return False

    def knows(self, session):
        with self._lock:
            return bool(session) and session in self._sessions

    def cookie_name(self):
        # PER PORT. A cookie belongs to a host, not a port, so two chats'
        # Companions on 127.0.0.1 would otherwise overwrite each other's.
        return "heron_companion_%d" % self.port

    def saw_page(self, server=None):
        """A page polled - through THIS server. A request still in flight on
        a server the switch just stopped must not mark the new one as seen
        (Codex review of #362)."""
        with self._life:
            if server is not None and server is not self._server:
                return
            self._seen = time.time()

    def recently_seen(self, seconds=10):
        """Whether a page asked for the state in the last few seconds - so a
        table opened from the chat need not open a second browser tab."""
        return self.running and time.time() - self._seen < seconds

    def hosts(self):
        return ("127.0.0.1:%d" % self.port, "localhost:%d" % self.port)

    def origins(self):
        return tuple("http://" + h for h in self.hosts())

    def state(self):
        try:
            pid = self._bound_pid()
        except Exception:                            # noqa: BLE001 - display only
            pid = None
        return state(pid, self._folder, self._discovery_dir, self._is_alive)


_shared = None
_shared_lock = threading.Lock()


def shared(bound_pid=lambda: None):
    """This process's one Companion, made on first use."""
    global _shared
    with _shared_lock:
        if _shared is None:
            _shared = Companion(bound_pid=bound_pid)
        return _shared


def keep(bound_pid=lambda: None):
    """Start the keeper: every two seconds it follows the Companion switch
    (D-109). It reads one settings file and writes one small note - it never
    touches the pipe, so it cannot stall on a Revit the way Talk's listener
    did (D-105), and a fault in it is swallowed rather than reaching the chat.
    Called once, when the MCP server starts."""
    import atexit
    companion = shared(bound_pid)

    def loop():
        while True:
            try:
                companion.keep_once()
            except Exception:                        # noqa: BLE001 - never reach the chat
                pass
            time.sleep(KEEP_SECONDS)

    atexit.register(companion.unpublish)
    threading.Thread(target=loop, name="heron-companion-keeper", daemon=True).start()
    return companion


class _Handler(http.server.BaseHTTPRequestHandler):
    owner = None                  # set per server by Companion.start
    server_version = "HeronCompanion"
    sys_version = ""

    # NEVER PRINT. The base class logs every request to stderr, which is safe
    # for MCP but noisy; silence is simpler to hold.
    def log_message(self, fmt, *args):
        return

    # ------------------------------------------------------------- replies

    def _send(self, status, body, content_type, extra=()):
        data = body if isinstance(body, bytes) else body.encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(data)))
        for name, value in SECURITY_HEADERS:
            self.send_header(name, value)
        for name, value in extra:
            self.send_header(name, value)
        self.end_headers()
        if self.command != "HEAD":
            self.wfile.write(data)

    def _json(self, status, payload, extra=()):
        self._send(status, json.dumps(payload), "application/json; charset=utf-8", extra)

    def _refuse(self, status, why):
        self._json(status, {"ok": False, "error": why})

    # -------------------------------------------------------------- checks

    def _host_ok(self):
        return self.headers.get("Host", "") in self.owner.hosts()

    def _cookie(self):
        raw = self.headers.get("Cookie", "")
        for part in raw.split(";"):
            name, _, value = part.strip().partition("=")
            if name == self.owner.cookie_name():
                return value
        return None

    def _api_ok(self, needs_session=True):
        if not self.headers.get(API_HEADER):
            return "missing header"
        if self.command == "POST" and self.headers.get("Origin", "") not in self.owner.origins():
            return "wrong origin"
        if needs_session and not self.owner.knows(self._cookie()):
            return "not paired"
        return None

    # ------------------------------------------------------------- routes

    def do_GET(self):
        if not self._host_ok():
            return self._refuse(421, "wrong host")
        path = self.path.split("?", 1)[0]
        if path in PAGES:
            name, kind = PAGES[path]
            try:
                with io.open(os.path.join(STATIC, name), "rb") as fh:
                    body = fh.read()
            except OSError:
                return self._refuse(500, "page missing")
            return self._send(200, body, kind)
        if path == "/api/state":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            self.owner.saw_page(self.server)
            return self._json(200, {"ok": True, "state": self.owner.state()})
        if path == "/api/activity":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            query = self.path.split("?", 1)[1] if "?" in self.path else ""
            since = 0
            for part in query.split("&"):
                name, _, value = part.partition("=")
                if name == "since" and value.isdigit():
                    since = int(value)
            return self._json(200, {"ok": True, "items": ACTIVITY.since(since)})
        if path == "/api/table":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            return self._json(200, {"ok": True, "table": TABLES.current()})
        if path == "/api/changes":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            return self._json(200, {"ok": True, "cards": CHANGES.cards()})
        if path == "/api/loads":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            return self._json(200, {"ok": True, "loads": LOADS_PANEL.current()})
        if path == "/api/loads/view":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            return self._json(200, {"ok": True, "view": LOADS_PANEL.view()})
        if path == "/api/loads/runs":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            return self._json(200, {"ok": True, "runs": LOADS_PANEL.runs()})
        if path == "/api/loads/run":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            query = self.path.split("?", 1)[1] if "?" in self.path else ""
            run_id = None
            for part in query.split("&"):
                name, _, value = part.partition("=")
                if name == "id":
                    run_id = value
            return self._json(200, {"ok": True, "run": LOADS_PANEL.earlier(run_id)})
        return self._refuse(404, "not found")

    def do_POST(self):
        if not self._host_ok():
            return self._refuse(421, "wrong host")
        path = self.path.split("?", 1)[0]
        if path == "/api/pair":
            why = self._api_ok(needs_session=False)
            if why:
                return self._refuse(403, why)
            try:
                length = min(int(self.headers.get("Content-Length", "0")), 4096)
                body = json.loads(self.rfile.read(length).decode("utf-8") or "{}")
            except (ValueError, UnicodeDecodeError):
                return self._refuse(400, "unreadable")
            session = self.owner.redeem(str(body.get("code", "")))
            if session is None:
                return self._refuse(403, "code used or expired")
            return self._json(200, {"ok": True}, extra=(
                ("Set-Cookie", "%s=%s; HttpOnly; SameSite=Strict; Path=/"
                 % (self.owner.cookie_name(), session)),))
        if path == "/api/table/apply":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            try:
                length = min(int(self.headers.get("Content-Length", "0")), 1048576)
                body = json.loads(self.rfile.read(length).decode("utf-8") or "{}")
                table_id = int(body.get("id"))
            except (ValueError, TypeError, UnicodeDecodeError):
                return self._refuse(400, "unreadable")
            return self._json(200, TABLES.apply(table_id, body.get("changes")))
        if path == "/api/change/apply":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            try:
                length = min(int(self.headers.get("Content-Length", "0")), 65536)
                body = json.loads(self.rfile.read(length).decode("utf-8") or "{}")
                card_id = int(body.get("id"))
            except (ValueError, TypeError, UnicodeDecodeError):
                return self._refuse(400, "unreadable")
            return self._json(200, CHANGES.apply(card_id, body.get("values")))
        if path in ("/api/loads/recalculate", "/api/loads/report", "/api/loads/finalize",
                    "/api/loads/confirm"):
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            try:
                length = min(int(self.headers.get("Content-Length", "0")), 1048576)
                body = json.loads(self.rfile.read(length).decode("utf-8") or "{}")
            except (ValueError, UnicodeDecodeError):
                return self._refuse(400, "unreadable")
            if path.endswith("/recalculate"):
                return self._json(200, LOADS_PANEL.recalculate(body))
            if path.endswith("/report"):
                return self._json(200, LOADS_PANEL.report())
            if path.endswith("/confirm"):
                return self._json(200, LOADS_PANEL.confirm())
            return self._json(200, LOADS_PANEL.finalize())
        if path == "/api/changes/clear":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            CHANGES.clear()
            return self._json(200, {"ok": True})
        if path == "/api/changes/load":
            why = self._api_ok()
            if why:
                return self._refuse(403, why)
            try:
                length = min(int(self.headers.get("Content-Length", "0")), 4096)
                body = json.loads(self.rfile.read(length).decode("utf-8") or "{}")
            except (ValueError, UnicodeDecodeError):
                return self._refuse(400, "unreadable")
            return self._json(200, CHANGES.load(str(body.get("kind", ""))))
        return self._refuse(404, "not found")

    def do_OPTIONS(self):
        # No CORS, ever: a pre-flight is refused, so a page on another site
        # can never send the custom header.
        return self._refuse(405, "not allowed")

    def do_PUT(self):
        return self._refuse(405, "not allowed")

    do_DELETE = do_PUT
    do_PATCH = do_PUT
