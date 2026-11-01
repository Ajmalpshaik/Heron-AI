# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What the Companion's Model Dashboard shows - docs/40 section 21.10.

One read of the open model (REPORT_MODEL_DASHBOARD's `dashboardJson`, which
changes nothing) becomes three things the page draws and never works out
for itself (mcp/companion README rule 4):

    TILES      one headline number per part of the model - elements,
               warnings, views, sheets, families, links, CAD, rooms.
    CHARTS     bars: elements by category, by level, by workset;
               warnings by kind; views by type.
    LOOK AT    the things that usually cost a modeller time, each with the
               count, why it matters in Revit, where Revit shows it, and the
               element ids to paste into Manage > Select by ID.

NOTHING IS CALLED WRONG (heron_modelqa's rule, kept here). Every figure is a
count of what the model holds. A row is marked "look" because it is the
kind of thing that costs time, never because it breaks a standard - whether
it does is the engineer's or the client's call, not Heron's. No row says a
model is compliant, clean or healthy, and there is no score: a score is a
judgement dressed as a number.

WHAT WAS NOT READ IS SAID. A part the fragment could not read arrives as
null, named in `notRead`, and is shown as not read - never as zero. A list
cut at the fragment's limit is named in `capped`, and the page says so.

SINCE THE LAST READ (phase 4). With a project key, each read keeps a small
snapshot - numbers only, no ids and no element names - beside the
project's other kept answers, and the next read says what moved: elements,
warnings, sheets, rooms, the things worth a look, and the categories that
grew or shrank most. A part not read in either read is not compared.

Standard library only, and heron_designbasis for where a project's kept
answers live - the folder the Loads panel keeps its runs beside.
"""

from __future__ import annotations

import io
import json
import os
import re
import time

FORMAT = 1
#: The fragment's own format this module reads.
READS = 1
#: Bars on one chart before the rest are folded into "Others".
CHART_BARS = 12
#: Ids handed to the page per row - what the fragment caps at as well.
IDS_MAX = 500

#: What each state means and how it is coloured. Decided here, drawn there.
STATES = (("look", "#d9822b", "worth a look - usually costs time later"),
          ("ok", "#5aa469", "none found"),
          ("info", "#7a8796", "a count only - nothing to judge"),
          ("not read", "#9e9e9e", "not read from the model - see why"))
BAR = "#4f7fd1"
BAR_OTHERS = "#9fb3c8"
BAR_ERROR = "#c53030"
#: Each part of the read, and the key its value comes under - the fragment
#: sends null for a part it could not read and names the part in notRead.
PARTS = (("elements", "elementTotal"), ("categories", "categories"), ("levels", "levels"),
         ("worksets", "worksets"), ("warnings", "warnings"), ("views", "views"),
         ("sheets", "sheets"), ("families", "families"), ("groups", "groups"),
         ("links", "links"), ("cad", "cad"), ("rooms", "rooms"), ("spaces", "spaces"))


# ----------------------------------------------------------------- reading

def _int(value):
    try:
        number = int(value)
    except (TypeError, ValueError):
        return 0
    return number if number > 0 else 0


def _num(value):
    try:
        number = float(value)
    except (TypeError, ValueError):
        return None
    return number if number == number else None


def _text(value):
    return "" if value is None else str(value)


def _ids(values):
    out = []
    for value in values if isinstance(values, list) else []:
        text = _text(value).strip()
        if text.isdigit() and text not in out:
            out.append(text)
        if len(out) >= IDS_MAX:
            break
    return out


def _names(values):
    out = []
    for value in values if isinstance(values, list) else []:
        text = _text(value).strip()
        if text:
            out.append(text)
        if len(out) >= IDS_MAX:
            break
    return out


def _part(data, key):
    got = data.get(key)
    return got if isinstance(got, dict) else {}


def _rows(data, key):
    got = data.get(key)
    return [r for r in got if isinstance(r, dict)] if isinstance(got, list) else []


def read(text):
    """
    dashboardJson as the fragment returns it, checked and made safe.

    Raises ValueError with a sentence a modeller can read when the text is
    not a dashboard this Heron understands. Every number comes back a
    non-negative int, every id a digit string, every name a string; a part
    that is missing comes back empty and is listed in `notRead`.
    """
    if not isinstance(text, str) or not text.strip():
        raise ValueError("Revit sent no dashboard - the read returned nothing.")
    try:
        data = json.loads(text)
    except ValueError:
        raise ValueError("what Revit sent is not a dashboard Heron can read.")
    if not isinstance(data, dict):
        raise ValueError("what Revit sent is not a dashboard Heron can read.")
    if data.get("format") != READS:
        raise ValueError("the dashboard is in format %s; this Heron reads format %d - "
                         "the add-in and Heron are from different releases."
                         % (_text(data.get("format")) or "none", READS))
    model = _part(data, "model")
    out = {
        "model": {"title": _text(model.get("title")), "isFamily": model.get("isFamily") is True,
                  "isWorkshared": model.get("isWorkshared") is True},
        "elementTotal": _int(data.get("elementTotal")),
        "categories": [{"name": _text(r.get("name")), "instances": _int(r.get("instances")),
                        "types": _int(r.get("types"))} for r in _rows(data, "categories")],
        "levels": [{"name": _text(r.get("name")), "elevationMm": _num(r.get("elevationMm")),
                    "instances": _int(r.get("instances"))} for r in _rows(data, "levels")],
        "noLevel": _int(data.get("noLevel")),
        "worksets": [{"name": _text(r.get("name")), "open": r.get("open") is not False,
                      "instances": _int(r.get("instances"))} for r in _rows(data, "worksets")],
        "links": [{"name": _text(r.get("name")), "loaded": r.get("loaded") is True,
                   "pinned": r.get("pinned") is True, "instances": _int(r.get("instances"))}
                  for r in _rows(data, "links")],
        "notRead": _names(data.get("notRead")),
    }
    warnings = _part(data, "warnings")
    out["warnings"] = {
        "total": _int(warnings.get("total")), "errors": _int(warnings.get("errors")),
        "groups": [{"text": _text(g.get("text")),
                    "severity": "error" if g.get("severity") == "error" else "warning",
                    "count": _int(g.get("count")), "ids": _ids(g.get("ids"))}
                   for g in _rows(warnings, "groups")]}
    views = _part(data, "views")
    by_type = views.get("byType") if isinstance(views.get("byType"), dict) else {}
    out["views"] = {k: _int(views.get(k)) for k in
                    ("total", "templates", "onSheets", "notOnSheets", "withoutTemplate")}
    out["views"]["byType"] = {_text(k): _int(v) for k, v in by_type.items()}
    out["views"]["notOnSheetsIds"] = _ids(views.get("notOnSheetsIds"))
    sheets = _part(data, "sheets")
    out["sheets"] = {k: _int(sheets.get(k)) for k in ("total", "empty", "placeholders")}
    out["sheets"]["emptyIds"] = _ids(sheets.get("emptyIds"))
    families = _part(data, "families")
    out["families"] = {k: _int(families.get(k)) for k in ("loaded", "inPlace", "unused", "types")}
    out["families"]["unusedNames"] = _names(families.get("unusedNames"))
    groups = _part(data, "groups")
    out["groups"] = {k: _int(groups.get(k)) for k in ("modelTypes", "detailTypes", "unusedTypes")}
    cad = _part(data, "cad")
    out["cad"] = {k: _int(cad.get(k)) for k in ("total", "linked", "imported")}
    out["cad"]["names"] = _names(cad.get("names"))
    for kind in ("rooms", "spaces"):
        got = _part(data, kind)
        out[kind] = {k: _int(got.get(k)) for k in ("total", "placed", "unplaced", "notEnclosed")}
        out[kind]["unplacedIds"] = _ids(got.get("unplacedIds"))
        out[kind]["notEnclosedIds"] = _ids(got.get("notEnclosedIds"))
    # A PART IS NOT READ WHEN ITS VALUE IS MISSING OR null - the fragment's
    # own rule for a part that failed. A note that names a part whose value
    # came WITH it ("3 element(s) could not be read") is a caveat on that
    # number, shown beside it, and never hides it.
    out["unread"] = []
    for part, key in PARTS:
        if data.get(key) is None:
            out["unread"].append(part)
            if not any(n.lower().startswith(part) for n in out["notRead"]):
                out["notRead"].append("%s: not in what Revit sent" % part)
    out["capped"] = _names(data.get("capped"))
    # THE FILL-RATE, only when parameters were asked for. null is "not asked
    # or not read" - which of the two, notRead says - never zeros.
    fill = data.get("fill")
    if isinstance(fill, dict):
        out["fill"] = {
            "parameters": _names(fill.get("parameters")),
            "rows": [{"category": _text(r.get("category")), "parameter": _text(r.get("parameter")),
                      "populated": _int(r.get("populated")), "empty": _int(r.get("empty")),
                      "absent": _int(r.get("absent")), "emptyIds": _ids(r.get("emptyIds"))}
                     for r in _rows(fill, "rows")]}
    else:
        out["fill"] = None
    return out


# ---------------------------------------------------------------- building

def _missing(data, key):
    """The reason a part was not read, or None when it was."""
    if key not in data.get("unread", ()):
        return None
    for note in data["notRead"]:
        if note.lower().startswith(key):
            return note
    return "%s: not read" % key


def _plural(n, one, many=None):
    return "%d %s" % (n, one if n == 1 else (many or one + "s"))


def _bars(rows, label_key="name", value_key="instances", most=CHART_BARS, colour=BAR):
    rows = [r for r in rows if r.get(value_key)]
    shown = rows[:most]
    bars = [{"label": r.get(label_key) or "(no name)", "value": r[value_key], "colour": colour}
            for r in shown]
    rest = sum(r[value_key] for r in rows[most:])
    if rest:
        bars.append({"label": "Others (%d more)" % (len(rows) - most), "value": rest,
                     "colour": BAR_OTHERS})
    return bars


def tiles(data):
    """One headline number per part, with the line under it."""
    w, v, s, f = data["warnings"], data["views"], data["sheets"], data["families"]
    r, links, cad = data["rooms"], data["links"], data["cad"]
    unloaded = sum(1 for l in links if not l["loaded"])
    out = [
        {"key": "elements", "label": "Model elements", "value": data["elementTotal"],
         "note": "in %s" % _plural(len(data["categories"]), "category", "categories"),
         "state": "info"},
        {"key": "warnings", "label": "Warnings", "value": w["total"],
         "note": ("%s among them" % _plural(w["errors"], "error")) if w["errors"]
         else "%s" % _plural(len(w["groups"]), "kind"),
         "state": "look" if w["total"] else "ok"},
        {"key": "views", "label": "Views", "value": v["total"],
         "note": "%d not on a sheet" % v["notOnSheets"], "state": "info"},
        {"key": "sheets", "label": "Sheets", "value": s["total"],
         "note": "%d empty" % s["empty"], "state": "look" if s["empty"] else "info"},
        {"key": "families", "label": "Families", "value": f["loaded"],
         "note": "%d never placed" % f["unused"], "state": "look" if f["unused"] else "info"},
        {"key": "links", "label": "Revit links", "value": len(links),
         "note": ("%d not loaded" % unloaded) if unloaded else "all loaded",
         "state": "look" if unloaded else "info"},
        {"key": "cad", "label": "CAD files", "value": cad["total"],
         "note": "%d imported, %d linked" % (cad["imported"], cad["linked"]),
         "state": "look" if cad["imported"] else "info"},
        {"key": "rooms", "label": "Rooms", "value": r["total"],
         "note": "%d not placed, %d not enclosed" % (r["unplaced"], r["notEnclosed"]),
         "state": "look" if (r["unplaced"] or r["notEnclosed"]) else "info"},
    ]
    if data["model"]["isWorkshared"]:
        closed = sum(1 for x in data["worksets"] if not x["open"])
        out.insert(1, {"key": "worksets", "label": "Worksets", "value": len(data["worksets"]),
                       "note": ("%d closed" % closed) if closed else "all open",
                       "state": "look" if closed else "info"})
    else:
        out.insert(1, {"key": "worksets", "label": "Worksets", "value": None,
                       "note": "not workshared", "state": "info"})
    for tile in out:
        why = _missing(data, tile["key"])
        # A file that is not workshared has no worksets to read - its tile
        # already says so, and "not read" would read as a failure.
        if why and not (tile["key"] == "worksets" and not data["model"]["isWorkshared"]):
            tile.update(value=None, note=why, state="not read")
    return out


def charts(data):
    """Bars the page draws, each already sorted, labelled and coloured."""
    out = []
    if data["categories"]:
        out.append({"key": "categories", "title": "Elements by category",
                    "bars": _bars(data["categories"])})
    if data["levels"]:
        levels = list(data["levels"])
        if data["noLevel"]:
            levels.append({"name": "(no level)", "instances": data["noLevel"]})
        out.append({"key": "levels", "title": "Elements by level",
                    "bars": _bars(levels, most=len(levels))})
    if data["worksets"]:
        out.append({"key": "worksets", "title": "Elements by workset",
                    "bars": _bars([dict(x, name=x["name"] + ("" if x["open"] else " (closed)"))
                                   for x in data["worksets"]])})
    groups = data["warnings"]["groups"]
    if groups:
        bars = _bars(groups, label_key="text", value_key="count", most=10)
        for bar, group in zip(bars, groups):
            if group["severity"] == "error":
                bar["colour"] = BAR_ERROR
        out.append({"key": "warnings", "title": "Warnings by kind", "bars": bars})
    by_type = data["views"]["byType"]
    if by_type:
        rows = sorted(({"name": k, "instances": n} for k, n in by_type.items()),
                      key=lambda r: -r["instances"])
        out.append({"key": "views", "title": "Views by type", "bars": _bars(rows)})
    return out


def _look(key, title, count, why, where, ids=None, names=None, state=None):
    return {"key": key, "title": title, "count": count, "why": why, "where": where,
            "ids": list(ids or []), "names": list(names or []),
            "state": state or ("look" if count else "ok")}


def look_at(data):
    """
    The things that usually cost a modeller time, worth-a-look first.
    Each row's ids go to Manage > Inquiry > Select by ID as they stand.
    """
    w, v, s, f = data["warnings"], data["views"], data["sheets"], data["families"]
    rows = [
        _look("errors", "Warnings that are errors", w["errors"],
              "An error-level warning is one Revit could not resolve; it travels with the "
              "model to everyone who opens it.",
              "Manage > Inquiry > Warnings",
              ids=[i for g in w["groups"] if g["severity"] == "error" for i in g["ids"]]),
        _look("warnings", "Warnings", w["total"],
              "Warnings slow the model and hide the few that matter among many of one kind.",
              "Manage > Inquiry > Warnings",
              ids=[i for g in w["groups"] for i in g["ids"]][:IDS_MAX]),
        _look("rooms-unplaced", "Rooms not placed", data["rooms"]["unplaced"],
              "A room deleted from the plan stays in schedules with no area until it is "
              "placed or deleted.",
              "a Room schedule - rows with Area 'Not Placed'",
              ids=data["rooms"]["unplacedIds"]),
        _look("rooms-open", "Rooms not enclosed or redundant", data["rooms"]["notEnclosed"],
              "A room with no area reads as zero in every area schedule and take-off.",
              "Manage > Inquiry > Warnings, or a Room schedule - Area 'Not Enclosed'",
              ids=data["rooms"]["notEnclosedIds"]),
        _look("spaces-unplaced", "Spaces not placed", data["spaces"]["unplaced"],
              "An unplaced Space carries no load - the HVAC Load Calculation leaves it out.",
              "a Space schedule - rows with Area 'Not Placed'",
              ids=data["spaces"]["unplacedIds"]),
        _look("spaces-open", "Spaces not enclosed or redundant", data["spaces"]["notEnclosed"],
              "A Space with no area has no volume, so no air flow or load can be worked out "
              "for it.",
              "a Space schedule - Area 'Not Enclosed'",
              ids=data["spaces"]["notEnclosedIds"]),
        _look("sheets-empty", "Empty sheets", s["empty"],
              "A sheet with no view or schedule on it is printed and issued as a blank.",
              "Project Browser > Sheets", ids=s["emptyIds"]),
        _look("families-unused", "Families never placed", f["unused"],
              "Every loaded family adds to the file size and the time it takes to open and "
              "sync.",
              "Manage > Purge Unused (review the list before you purge)",
              names=f["unusedNames"]),
        _look("groups-unused", "Group types never placed", data["groups"]["unusedTypes"],
              "An unused group type is carried in the file and offered in every list.",
              "Project Browser > Groups"),
        _look("cad-imported", "CAD imported, not linked", data["cad"]["imported"],
              "An imported DWG is copied into the model and cannot be reloaded when the "
              "consultant issues a new one.",
              "Insert > Manage Links > CAD Formats", names=data["cad"]["names"]),
        _look("links-unloaded", "Revit links not loaded",
              sum(1 for l in data["links"] if not l["loaded"]),
              "An unloaded link draws nothing and coordinates nothing.",
              "Insert > Manage Links > Revit",
              names=[l["name"] for l in data["links"] if not l["loaded"]]),
        _look("links-unpinned", "Revit links not pinned",
              sum(l["instances"] for l in data["links"] if not l["pinned"]),
              "An unpinned link can be dragged by accident, moving every element of the "
              "consultant's model against yours.",
              "select the link in a view - Modify > Pin",
              names=[l["name"] for l in data["links"] if not l["pinned"]]),
        _look("worksets-closed", "Worksets closed",
              sum(1 for x in data["worksets"] if not x["open"]),
              "Elements on a closed workset are not loaded - counts, schedules and views come "
              "back low.",
              "Collaborate > Worksets",
              names=[x["name"] for x in data["worksets"] if not x["open"]]),
        _look("no-level", "Elements with no level", data["noLevel"],
              "An element on no level is left out of every 'by level' schedule and filter.",
              "a schedule with the Level field, sorted by Level"),
        _look("views-no-template", "Views without a template", v["withoutTemplate"],
              "A view with no template is set by hand and drifts from the office standard.",
              "Project Browser - the view's Properties > View Template", state="info"),
        _look("views-unplaced", "Views not on a sheet", v["notOnSheets"],
              "Working views are normal; a long list of them is worth tidying before issue.",
              "Project Browser > Views", ids=v["notOnSheetsIds"], state="info"),
        _look("families-inplace", "In-place families", f["inPlace"],
              "An in-place family cannot be reused or scheduled by type like a loaded one.",
              "select it - Modify > Edit In-Place", state="info"),
    ]
    sections = {"errors": "warnings", "warnings": "warnings", "rooms-unplaced": "rooms",
                "rooms-open": "rooms", "spaces-unplaced": "spaces", "spaces-open": "spaces",
                "sheets-empty": "sheets", "families-unused": "families",
                "groups-unused": "groups", "cad-imported": "cad",
                "links-unloaded": "links", "links-unpinned": "links",
                "worksets-closed": "worksets", "no-level": "levels",
                "views-no-template": "views", "views-unplaced": "views",
                "families-inplace": "families"}
    for row in rows:
        why = _missing(data, sections[row["key"]])
        if why:
            row.update(state="not read", count=None, why=why, ids=[], names=[])
    if not data["model"]["isWorkshared"]:
        rows = [r for r in rows if r["key"] != "worksets-closed"]
    order = {"look": 0, "info": 1, "ok": 2, "not read": 3}
    return sorted(rows, key=lambda r: order[r["state"]])


def fill(data):
    """
    Each parameter asked for, category by category, in heron_modelqa's three
    states - populated, empty, absent - with the share filled. EMPTY and
    ABSENT ARE NEVER ONE NUMBER: empty is a value nobody typed; absent is a
    parameter the category does not carry, a different afternoon's work.
    Returns None when no parameter was asked for.
    """
    got = data.get("fill")
    if not got:
        return None
    out = []
    for name in got["parameters"]:
        rows = []
        for r in got["rows"]:
            if r["parameter"].lower() != name.lower():
                continue
            total = r["populated"] + r["empty"] + r["absent"]
            share = int(round(100.0 * r["populated"] / total)) if total else 0
            rows.append(dict(r, total=total, share=share,
                             state="look" if r["empty"] else ("info" if r["absent"] else "ok")))
        rows.sort(key=lambda r: (-r["empty"], r["category"]))
        out.append({"parameter": name, "rows": rows,
                    "populated": sum(r["populated"] for r in rows),
                    "empty": sum(r["empty"] for r in rows),
                    "absent": sum(r["absent"] for r in rows)})
    return {"parameters": out}


def _fill_rows(board_fill):
    """One worth-a-look row per parameter left empty, its ids for Select."""
    rows = []
    for p in (board_fill or {}).get("parameters") or []:
        ids = [i for r in p["rows"] for i in r["emptyIds"]][:IDS_MAX]
        cats = [r["category"] for r in p["rows"] if r["empty"]]
        where = ", ".join(cats[:4]) + (" and %d more" % (len(cats) - 4) if len(cats) > 4 else "")
        rows.append(_look("fill:" + p["parameter"], "'%s' empty" % p["parameter"], p["empty"],
                          ("The parameter is there and nobody typed a value - in %s. Schedules "
                           "and tags show it blank." % where) if cats else
                          "No counted element has it blank.",
                          "a schedule of those categories with '%s' as a field, filtered blank"
                          % p["parameter"], ids=ids))
    return rows


def build(data):
    """The panel's whole content from read()'s result."""
    board_fill = fill(data)
    rows = look_at(data)
    if board_fill:
        order = {"look": 0, "info": 1, "ok": 2, "not read": 3}
        rows = sorted(_fill_rows(board_fill) + rows, key=lambda r: order[r["state"]])
    return {"format": FORMAT, "model": dict(data["model"]), "tiles": tiles(data),
            "charts": charts(data), "look": rows, "notRead": list(data["notRead"]),
            "capped": list(data.get("capped") or []), "fill": board_fill,
            "states": [{"key": k, "colour": c, "means": m} for k, c, m in STATES],
            "said": summary(data, rows)}


def summary(data, rows=None):
    """One line for the chat - the page has the rest."""
    rows = look_at(data) if rows is None else rows
    looks = [r for r in rows if r["state"] == "look"]
    title = data["model"]["title"] or "the open model"
    if data["model"]["isFamily"]:
        return ("%s is a family, not a project - the Model Dashboard reads projects. Nothing "
                "was changed." % title)
    w = data["warnings"]
    parts = ["%s in %s" % (_plural(data["elementTotal"], "model element"),
                           _plural(len(data["categories"]), "category", "categories")),
             "%s%s" % (_plural(w["total"], "warning"),
                       (" (%s)" % _plural(w["errors"], "error")) if w["errors"] else "")]
    said = "Model Dashboard for %s: %s; %s worth a look%s." % (
        title, "; ".join(parts), _plural(len(looks), "thing"),
        (" - %s" % ", ".join(r["title"].lower() for r in looks[:3])) if looks else "")
    if data["notRead"]:
        said += " Not read: %s." % "; ".join(data["notRead"][:3])
    return said + " The charts and lists are on the Companion page. Nothing was changed."


def dashboard(text, project_key=None):
    """
    read() then build(), as the MCP server calls it. Returns {"dashboard",
    "said"}; "dashboard" is None, and "said" says why, when the text could
    not be read. With a project key the read is kept and compared with the
    last one: the board's "since" says what moved.
    """
    try:
        data = read(text)
    except ValueError as why:
        return {"dashboard": None, "said": "No dashboard was made: %s" % why}
    built = build(data)
    built["since"], note = (since(project_key, data) if project_key and
                            not data["model"]["isFamily"] else (None, None))
    if note:
        built["notRead"] = built["notRead"] + [note]
    return {"dashboard": built, "said": built["said"]}


# --------------------------------------------------- since the last read

#: The headline numbers a snapshot keeps, each with the part it needs read.
HEADLINES = (
    ("elementTotal", "Model elements", "elements"),
    ("warnings.total", "Warnings", "warnings"),
    ("warnings.errors", "Warnings that are errors", "warnings"),
    ("views.total", "Views", "views"),
    ("views.notOnSheets", "Views not on a sheet", "views"),
    ("sheets.total", "Sheets", "sheets"),
    ("sheets.empty", "Empty sheets", "sheets"),
    ("families.loaded", "Families", "families"),
    ("families.unused", "Families never placed", "families"),
    ("rooms.total", "Rooms", "rooms"),
    ("rooms.unplaced", "Rooms not placed", "rooms"),
    ("rooms.notEnclosed", "Rooms not enclosed", "rooms"),
    ("spaces.unplaced", "Spaces not placed", "spaces"),
    ("cad.imported", "CAD imported", "cad"),
)
#: Categories listed under "since", the biggest moves first.
SINCE_CATEGORIES = 10


def _dig(data, path):
    got = data
    for part in path.split("."):
        got = got.get(part) if isinstance(got, dict) else None
    return got


def snapshot(data, when=None):
    """
    What a read is kept as: numbers only - the headlines and each category's
    count - with the parts not read named, so they are never compared.
    """
    return {"format": 1, "when": when or time.strftime("%Y-%m-%d %H:%M"),
            "unread": list(data.get("unread") or []),
            "numbers": {key: _dig(data, key) for key, _label, part in HEADLINES
                        if part not in (data.get("unread") or [])},
            "categories": {c["name"]: c["instances"] for c in data.get("categories") or []
                           if c.get("name")}}


def compare(was, now):
    """
    What moved between two snapshots: [{"label", "was", "now", "change"}],
    headlines in their own order, then the categories that moved most. A
    number missing from either side is left out - never read as zero.
    """
    rows = []
    for key, label, _part in HEADLINES:
        a, b = (was.get("numbers") or {}).get(key), (now.get("numbers") or {}).get(key)
        if isinstance(a, int) and isinstance(b, int) and a != b:
            rows.append({"label": label, "was": a, "now": b, "change": b - a})
    if "categories" not in (was.get("unread") or []) + (now.get("unread") or []):
        old, new = was.get("categories") or {}, now.get("categories") or {}
        moved = []
        for name in set(old) | set(new):
            a, b = old.get(name, 0), new.get(name, 0)
            if a != b:
                moved.append({"label": name, "was": a, "now": b, "change": b - a})
        moved.sort(key=lambda r: (-abs(r["change"]), r["label"]))
        rows.extend(moved[:SINCE_CATEGORIES])
    return rows


def _snap_folder(project_key):
    import heron_designbasis
    scope = heron_designbasis._scope()
    base = scope.knowledge_dir()
    if base is None:
        raise ValueError("No HERON_DATA, no Windows profile and no HERON_KNOWLEDGE, so there is "
                         "nowhere to keep the dashboard's reads. Set HERON_KNOWLEDGE to a folder.")
    if not project_key:
        raise ValueError("No project key, so the dashboard's reads are not kept (D-33).")
    return os.path.join(base, "projects", scope._safe_key(project_key) + ".dashboard")


def keep(project_key, snap):
    """Keep one snapshot; the path. Raises ValueError when there is nowhere to keep it."""
    folder = _snap_folder(project_key)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    stamp = re.sub(r"[^0-9]", "", snap.get("when") or "") or time.strftime("%Y%m%d%H%M")
    path, n = os.path.join(folder, stamp + ".json"), 1
    while os.path.exists(path):
        n += 1
        path = os.path.join(folder, "%s-%d.json" % (stamp, n))
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(snap))
    return path


def kept(project_key):
    """This project's snapshots, newest first. [] when none, or nowhere to look."""
    try:
        folder = _snap_folder(project_key)
    except ValueError:
        return []
    if not os.path.isdir(folder):
        return []
    out = []
    for name in sorted(os.listdir(folder), reverse=True):
        if not name.endswith(".json"):
            continue
        try:
            with io.open(os.path.join(folder, name), encoding="utf-8") as fh:
                got = json.loads(fh.read())
        except (OSError, ValueError):
            continue
        if isinstance(got, dict) and got.get("format") == 1:
            out.append(got)
    return out


def since(project_key, data):
    """
    Compare this read with the last one kept for the project, then keep this
    one. Returns ({"when", "rows"} or None, a note or None). The note says why
    nothing was kept - never a reason to refuse the dashboard.
    """
    now = snapshot(data)
    earlier = kept(project_key)
    moved = {"when": earlier[0].get("when"), "rows": compare(earlier[0], now)} if earlier else None
    try:
        keep(project_key, now)
    except (ValueError, OSError) as why:
        return moved, "This read was not kept: %s" % why
    return moved, None
