# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Where a family build's minutes went, read from the chat and from Revit's trail.

    python tools/time-family-build.py CHAT.jsonl --audit AUDIT     # the report
    python tools/time-family-build.py CHAT.jsonl --audit AUDIT --json
    python tools/time-family-build.py CHAT.jsonl --since 2026-10-09T13:33:00Z \\
                                                 --until 2026-10-09T13:55:00Z
    python tools/time-family-build.py CHAT.jsonl --audit AUDIT \\
                       --journal journal.0016.txt --journal-utc-offset +03:00
    python tools/time-family-build.py --form-cards   # which capabilities build forms

CHAT.jsonl is a Claude Code transcript - the chat's own file under
~/.claude/projects. AUDIT is the add-in's trail, `audit-YYYYMM.jsonl` under
HeronPaths.Audit, or the folder holding it; --audit may be given more than once.
JOURNAL is Revit's own journal.NNNN.txt for the session that ran the build -
%LOCALAPPDATA%\\Autodesk\\Revit\\Autodesk Revit 20xx\\Journals - and --journal
may be given more than once. Its stamps are the PC's local time with no zone,
so --journal-utc-offset says how far that clock is from UTC.

A REPORT, NOT A GATE. It exits 0 whatever it finds, and 2 only when a file it
was named cannot be read.

WHY IT EXISTS
-------------
docs/04 section 8.7 quotes two family builds timed on the owner's laptop -
request to finish, the model's share, thinking, Revit's own time, Heron's time
around Revit, seconds a form. Both were worked out BY HAND from a transcript
and the audit trail (row 5b-378 names the files), so the next re-timing -
CREATE_FAMILY_FORMS, D-116 - had nothing to be compared with like for like.
This is that hand, written down once.

WHERE EACH NUMBER COMES FROM, AND NOTHING ELSE
----------------------------------------------
Every number names the file and the lines it was read from. A number the
inputs cannot show is printed NOT MEASURED, with why - never estimated:

  * THE CHAT'S TIMELINE. Every user and assistant line of the main chain (a
    subagent's sidechain lines are skipped and named) is split at its own
    timestamp, and the interval ENDING at a line belongs to that line: an
    assistant line - the model was producing it; a tool result - a tool was
    running; a person's prompt - Claude was waiting for the person. So the
    model's time, the tools' time and the person's time add up to request to
    finish exactly, with nothing counted twice. A permission prompt or a Revit
    dialog answered inside a call is in the tools' time: nothing in the
    transcript separates it. The Keep box is in Revit's own time as well -
    THE KEEP BOX below.
  * THINKING. Tokens from `usage.output_tokens_details.thinking_tokens`,
    seconds from a thinking line's `thinkingDurationMs`, effort from `effort` -
    a transcript that does not carry them (older Claude Code) leaves them NOT
    MEASURED. `output_tokens` counts thinking WITH the text and the tool calls,
    so it is reported as itself, never as thinking.
  * REVIT'S OWN TIME. The audit's `ms` - the add-in's stopwatch round one
    job - for each line whose `at` falls inside one of this chat's Heron calls,
    from the call's line to its result's. A line inside the chat's minutes and
    outside every call is another chat's or the person's, and is counted as
    not joined. `ms` written as a string ("27", the shape before HeronAudit
    wrote numbers) is read too. A folder given as --audit is read for the
    add-in's `audit-YYYYMM.jsonl` only: `audit-brain-*.jsonl` is the brain's
    own (brain/heron_audit.py), and its lookups are not Revit's time. The
    stopwatch runs round the WHOLE job, so since D-126 it holds the seconds
    the modeller took to answer the Keep box: the figure is the add-in's,
    not Revit's work alone (row 5b-1031).
  * THE KEEP BOX (D-126), from Revit's journal - NOT MEASURED without
    --journal. Revit writes `TaskDialog "Keep what Heron just did?"` when the
    box is shown and `"TaskDialogResult" , "Keep what Heron just did?" , _`
    when it is answered, with the button's text ("Keep it", "Discard it") as
    the next quoted string. Each of those lines takes the time of the last
    stamp at or before it - `'H 17-Oct-2026 20:56:46.362;` - so a box's
    seconds are the journal's own, shown to answered. A box is counted in
    the chat's minutes by its shown time, and inside a run when its answer
    falls between the audit line's `at` less its `ms` and its `at` (the
    add-in writes `at` once the job is over). A shown stamp earlier than
    that start is cut to it, and the run says so: the box cannot have been
    up before the job began. "Revit's own time less the boxes" is then each
    joined run's `ms` less the seconds of the boxes inside it. Any answer
    but "Keep it" - or D-138's third link, "Keep, and keep the rest of this
    job without asking", which keeps too - is counted as a Discard, because
    D-126 makes Esc, the close button and any result nobody expected a
    Discard; the answers are listed
    as read. The stamps carry no zone and no journal read so far has been
    seen to say its zone, so without --journal-utc-offset only the boxes in
    the whole journal are counted and the rest is NOT MEASURED. A journal is
    read as UTF-16 LE when it starts with that byte-order mark or its bytes
    show it, and as latin-1 otherwise. A DRAFT's one run (D-119) asks the
    same box in other words - "'<name>' has never been proved. Keep what it
    did?" - and those are read too, though no journal read so far holds one.
  * HERON'S TIME AROUND REVIT. For each batch - the Heron calls of one model
    response, all of them Heron's - from its first call line to its last
    result, less the `ms` of the audit lines joined to it. That is Heron, the
    MCP client and Claude Code's own handling together; per call it is the
    batch's share. Heron's SERVER alone is NOT MEASURED: it keeps each call's
    seconds in memory only (heron_companion.Activity, for the Companion page)
    and writes them nowhere.
  * A REPLY'S TEXT. Claude Code stores a reply from Heron's server as the
    JSON text {"result": "..."} - one string whose newlines are written as
    backslash-n. A tool result whose text parses as an object holding a
    string under "result" is read as that string; any other text as it is.
    The revit_change hand-back is the characters of the text read that way,
    so an escaped newline or quote counts once, not twice, and FORMS BUILT
    below reads its formCount from the same text (row 5b-1030).
  * FORMS BUILT. A successful run of a card that provides `formId` built one;
    a card that provides `formCount` (CREATE_FAMILY_FORMS) built what its
    reply's `formCount` line says. Which cards those are is read from
    brain/fragments, never typed - `--form-cards` lists them. A reply that does
    not print its count leaves the total NOT MEASURED: revit_change does not
    print a value the last reply of the same capability showed word for word
    (row 5b-407), and the rows sent are never counted in its place.
"""

import argparse
import collections
import datetime
import glob
import io
import json
import os
import re
import statistics
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

MEASURED = "MEASURED"
NOT_MEASURED = "NOT MEASURED"

#: The add-in's own monthly trail. audit-brain-YYYYMM.jsonl matches the
#: glob and is not it.
ADDIN_AUDIT = re.compile(r"^audit-\d{6}\.jsonl$")
#: How a revit_change reply prints a value: two spaces, the name, the value.
FORM_COUNT = re.compile(r"^\s+formCount\s+(\d+)\s*$", re.MULTILINE)

#: The Keep box's words, as HeronKeepQuestion holds them - its MainInstruction
#: is what Revit's journal names the box by. The first is a PROVEN
#: capability's; the second a DRAFT's one run (D-119), for any name.
KEEP_HEADLINE = "Keep what Heron just did?"
KEEP_HEADLINE_DRAFT = r"'[^\"]*' has never been proved\. Keep what it did\?"
KEEP_LINK = "Keep it"
#: D-138's third link - it keeps too, and lets the rest of the job be kept
#: without the box. HeronKeepGrant.KeepRestLink holds the same words.
KEEP_REST_LINK = "Keep, and keep the rest of this job without asking"
KEEPS = (KEEP_LINK, KEEP_REST_LINK)
_HEADLINES = r"(%s|%s)" % (re.escape(KEEP_HEADLINE), KEEP_HEADLINE_DRAFT)
BOX_SHOWN = re.compile(r'\bTaskDialog\s+"%s"' % _HEADLINES)
BOX_ANSWER = re.compile(r'"TaskDialogResult"\s*,\s*"%s"\s*,\s*(?:"([^"]*)")?' % _HEADLINES)
_QUOTED = re.compile(r'"([^"]*)"')
#: A journal's stamp, in the PC's local time: 'H 17-Oct-2026 20:56:46.362;
JOURNAL_STAMP = re.compile(r"^\s*'[A-Z]\s+(\d{1,2})-([A-Za-z]{3})-(\d{4})\s+"
                           r"(\d\d):(\d\d):(\d\d)(?:\.(\d+))?\s*;")
_MONTHS = dict((name, i) for i, name in enumerate(
    ("jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"), 1))
_OFFSET = re.compile(r"^(?:Z|([+-])(\d\d):?(\d\d))$")

_STAMP = re.compile(r"^(\d{4})-(\d\d)-(\d\d)[T ](\d\d):(\d\d):(\d\d)(?:[.,](\d+))?"
                    r"(Z|[+-]\d\d:?\d\d)?$")
_EPOCH = datetime.datetime(1970, 1, 1, tzinfo=datetime.timezone.utc)
_MICRO = datetime.timedelta(microseconds=1)


def micros(text):
    """An ISO 8601 time as whole microseconds since 1970, UTC - or None.
    Whole numbers, so a sum of intervals is exact. The audit writes seven
    fractional digits and a transcript three; a time with no zone is UTC,
    which both files are."""
    match = _STAMP.match(str(text or "").strip())
    if not match:
        return None
    year, month, day, hour, minute, second = (int(g) for g in match.groups()[:6])
    fraction = match.group(7) or ""
    micro = int((fraction + "000000")[:6]) if fraction else 0
    try:
        moment = datetime.datetime(year, month, day, hour, minute, second, micro,
                                   tzinfo=datetime.timezone.utc)
    except ValueError:
        return None
    zone = match.group(8)
    if zone and zone != "Z":
        sign = 1 if zone[0] == "+" else -1
        moment -= sign * datetime.timedelta(hours=int(zone[1:3]), minutes=int(zone[-2:]))
    return (moment - _EPOCH) // _MICRO


def utc_offset(text):
    """--journal-utc-offset as microseconds the local clock is ahead of UTC,
    or None when it does not read as Z or +HH:MM."""
    match = _OFFSET.match(str(text or "").strip())
    if not match:
        return None
    if not match.group(1):
        return 0
    sign = 1 if match.group(1) == "+" else -1
    return sign * (int(match.group(2)) * 3600 + int(match.group(3)) * 60) * 1000000


def iso(us):
    return (_EPOCH + datetime.timedelta(microseconds=us)).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"


def seconds(us):
    return us / 1e6


# ------------------------------------------------------------------ results

def source(path, lines, searched=False):
    entry = {"file": path, "lines": sorted(set(lines))}
    if searched:
        entry["searched"] = True
    return entry


def measured(value, sources, how, unit=None, **extra):
    entry = {"status": MEASURED, "value": value, "from": [s for s in sources if s], "how": how}
    if unit:
        entry["unit"] = unit
    entry.update(extra)
    return entry


def not_measured(why):
    return {"status": NOT_MEASURED, "value": None, "why": why}


def spread(values):
    """Median, range and - from four values up - the middle half."""
    extra = {"count": len(values), "low": min(values), "high": max(values)}
    if len(values) >= 4:
        quarter = statistics.quantiles(values, n=4, method="inclusive")
        extra["middle_half"] = [quarter[0], quarter[2]]
    return statistics.median(values), extra


# --------------------------------------------------------------- transcript

Line = collections.namedtuple("Line", "n at kind record")


def _blocks(record):
    content = (record.get("message") or {}).get("content")
    return [b for b in content if isinstance(b, dict)] if isinstance(content, list) else []


def _kind(record):
    """assistant, result (a tool's answer), prompt (the person) or turn (a user
    turn nobody typed - a meta line, a notification)."""
    if record.get("type") == "assistant":
        return "assistant"
    if any(b.get("type") == "tool_result" for b in _blocks(record)):
        return "result"
    if record.get("isMeta") or record.get("isCompactSummary"):
        return "turn"
    origin = record.get("origin")
    if isinstance(origin, dict) and origin.get("kind") not in (None, "human"):
        return "turn"
    return "prompt"


def read_transcript(path, since=None, until=None):
    lines, sidechain, unreadable, total = [], [], [], 0
    with io.open(path, encoding="utf-8", errors="replace") as fh:
        for n, raw in enumerate(fh, 1):
            total = n
            raw = raw.strip()
            if not raw:
                continue
            try:
                record = json.loads(raw)
            except ValueError:
                unreadable.append(n)
                continue
            if not isinstance(record, dict) or record.get("type") not in ("user", "assistant"):
                continue
            if record.get("isSidechain"):
                sidechain.append(n)
                continue
            at = micros(record.get("timestamp"))
            if at is None:
                unreadable.append(n)
                continue
            if (since is not None and at < since) or (until is not None and at > until):
                continue
            lines.append(Line(n, at, _kind(record), record))
    return lines, {"file": path, "lines_read": total, "sidechain_skipped": sidechain,
                   "unreadable": unreadable}


def _text_of(block):
    """A tool result's text as the tool wrote it. Claude Code stores a reply
    from Heron's server as the JSON text {"result": "..."}, its newlines
    written as backslash-n, so a text that parses as an object holding a
    string under "result" is that string. Any other text is kept as it is
    (row 5b-1030)."""
    content = block.get("content")
    if isinstance(content, str):
        text = content
    elif isinstance(content, list):
        text = "".join(str(part.get("text") or "") for part in content
                       if isinstance(part, dict) and part.get("type") == "text")
    else:
        return ""
    try:
        reply = json.loads(text)
    except ValueError:
        return text
    if isinstance(reply, dict) and isinstance(reply.get("result"), str):
        return reply["result"]
    return text


class Chat(object):
    """The timeline from the first prompt, its intervals, responses and calls."""

    def __init__(self, path, lines, server):
        self.path = path
        first = next((i for i, line in enumerate(lines) if line.kind == "prompt"), None)
        self.from_prompt = first is not None
        self.timeline = lines[first:] if first is not None else lines
        self.buckets = {"model": [], "tool": [], "person": [], "other": []}
        self.backwards = []
        self.ending = {}
        for before, line in zip(self.timeline, self.timeline[1:]):
            gap = line.at - before.at
            if gap < 0:
                self.backwards.append(line.n)
                gap = 0
            bucket = {"assistant": "model", "result": "tool",
                      "prompt": "person"}.get(line.kind, "other")
            self.buckets[bucket].append((line.n, gap))
            self.ending[line.n] = gap
        self._responses()
        self._calls(re.compile(r"^mcp__(?:[A-Za-z0-9_-]*_)?%s__(.+)$" % re.escape(server)))

    def _responses(self):
        self.responses = collections.OrderedDict()
        for line in self.timeline:
            if line.kind != "assistant":
                continue
            message = line.record.get("message") or {}
            key = message.get("id") or line.record.get("requestId") or "line-%d" % line.n
            self.responses.setdefault(key, []).append(line)

    def _calls(self, heron):
        results = {}
        for line in self.timeline:
            if line.kind == "result":
                for block in _blocks(line.record):
                    if block.get("type") == "tool_result" and block.get("tool_use_id"):
                        results[block["tool_use_id"]] = (line, _text_of(block))
        self.calls, self.batches = [], []
        for key, lines in self.responses.items():
            uses = [(line, block) for line in lines for block in _blocks(line.record)
                    if block.get("type") == "tool_use"]
            mine = []
            for line, block in uses:
                match = heron.match(str(block.get("name") or ""))
                if not match:
                    continue
                answer = results.get(block.get("id"))
                call = {"tool": match.group(1), "line": line, "input": block.get("input") or {},
                        "result": answer[0] if answer else None,
                        "text": answer[1] if answer else None}
                mine.append(call)
                self.calls.append(call)
            if mine:
                complete = all(c["result"] is not None for c in mine)
                self.batches.append({
                    "response": key, "calls": mine, "pure": len(mine) == len(uses),
                    "complete": complete,
                    "start": min(c["line"].at for c in mine),
                    "end": max(c["result"].at for c in mine) if complete else None,
                    "audit": []})

    @property
    def lines_searched(self):
        return [self.timeline[0].n, self.timeline[-1].n] if self.timeline else []


# -------------------------------------------------------------------- audit

def audit_files(paths):
    files, skipped, missing = [], [], []
    for path in paths:
        if os.path.isdir(path):
            for name in sorted(glob.glob(os.path.join(path, "audit-*.jsonl"))):
                (files if ADDIN_AUDIT.match(os.path.basename(name)) else skipped).append(name)
        elif os.path.isfile(path):
            files.append(path)
        else:
            missing.append(path)
    return files, skipped, missing


def _ms(value):
    if isinstance(value, bool):
        return None
    if isinstance(value, (int, float)):
        return float(value)
    try:
        return float(str(value).strip())
    except ValueError:
        return None


def read_audit(path):
    entries, unreadable, total = [], [], 0
    with io.open(path, encoding="utf-8", errors="replace") as fh:
        for n, raw in enumerate(fh, 1):
            total = n
            raw = raw.strip()
            if not raw:
                continue
            try:
                record = json.loads(raw)
            except ValueError:
                unreadable.append(n)
                continue
            at = micros(record.get("at")) if isinstance(record, dict) else None
            if at is None:
                unreadable.append(n)
                continue
            op = str(record.get("op") or "")
            entries.append({"file": path, "n": n, "at": at, "op": op,
                            "ok": record.get("ok") is True or record.get("ok") == "true",
                            "ms": _ms(record.get("ms")) if "ms" in record else None,
                            "fragment": record.get("fragment"),
                            "session": str(record.get("session") or ""),
                            "run": op.startswith("run_fragment")})
    return entries, {"file": path, "lines": total, "unreadable": unreadable}


def audit_sources(entries):
    by_file = collections.OrderedDict()
    for entry in entries:
        by_file.setdefault(entry["file"], []).append(entry["n"])
    return [source(path, lines) for path, lines in by_file.items()]


# ------------------------------------------------------------------ journal

def _journal_text(raw):
    if raw.startswith(b"\xff\xfe"):
        return raw[2:].decode("utf-16-le", "replace"), "UTF-16 LE"
    odd = raw[1:4096:2]
    if odd and odd.count(0) * 2 > len(odd):
        return raw.decode("utf-16-le", "replace"), "UTF-16 LE, no byte-order mark"
    return raw.decode("latin-1"), "latin-1"


def _journal_micros(match):
    """A journal stamp as microseconds of its own local clock."""
    day, month, year, hour, minute, second = match.groups()[:6]
    number = _MONTHS.get(month.lower())
    if number is None:
        return None
    fraction = match.group(7) or ""
    try:
        moment = datetime.datetime(int(year), number, int(day), int(hour), int(minute),
                                   int(second), int((fraction + "000000")[:6]),
                                   tzinfo=datetime.timezone.utc)
    except ValueError:
        return None
    return (moment - _EPOCH) // _MICRO


def read_journal(path, offset):
    """Every Keep box in one Revit journal: where it was shown and answered,
    the stamp each of those lines takes, and the button. `offset` is
    --journal-utc-offset in microseconds, or None - a box then has its
    seconds and no place in UTC."""
    with io.open(path, "rb") as fh:
        text, encoding = _journal_text(fh.read())
    lines = [line.rstrip("\r") for line in text.split("\n")]
    if lines and not lines[-1]:
        lines.pop()          # the newline that ends the last line starts none
    boxes, stray, bad_stamps = [], [], []
    stamp, box = None, None
    for n, line in enumerate(lines, 1):
        match = JOURNAL_STAMP.match(line)
        if match:
            local = _journal_micros(match)
            if local is None:
                bad_stamps.append(n)
            else:
                stamp = (local, n)
        shown = BOX_SHOWN.search(line)
        if shown:
            if box:
                boxes.append(box)
            box = {"file": path, "headline": shown.group(1), "shown_line": n,
                   "shown_stamp": stamp, "answer_line": None, "answer_stamp": None,
                   "button": None, "button_line": None}
            continue
        answer = BOX_ANSWER.search(line)
        if not answer:
            continue
        if not box or box["headline"] != answer.group(1):
            stray.append(n)
            continue
        button, button_line = answer.group(2), n
        if button is None:
            following = next((k for k in range(n, len(lines)) if lines[k].strip()), None)
            if following is not None:
                quoted = _QUOTED.search(lines[following])
                button, button_line = (quoted.group(1) if quoted else None), following + 1
        box.update(answer_line=n, answer_stamp=stamp, button=button, button_line=button_line)
        boxes.append(box)
        box = None
    if box:
        boxes.append(box)
    for box in boxes:
        shown, answered = box["shown_stamp"], box["answer_stamp"]
        box["lines"] = sorted(set(n for n in (shown and shown[1], box["shown_line"],
                                               answered and answered[1], box["answer_line"],
                                               box["button_line"]) if n))
        box["seconds"] = (seconds(answered[0] - shown[0])
                          if shown and answered and answered[0] >= shown[0] else None)
        box["shown"] = shown[0] - offset if shown and offset is not None else None
        box["answered"] = answered[0] - offset if answered and offset is not None else None
    return boxes, {"file": path, "lines": len(lines), "encoding": encoding,
                   "boxes": len(boxes), "answers_with_no_box": stray,
                   "unreadable_stamps": bad_stamps}


def box_sources(boxes):
    by_file = collections.OrderedDict()
    for box in boxes:
        by_file.setdefault(box["file"], []).extend(box["lines"])
    return [source(path, lines) for path, lines in by_file.items()]


# -------------------------------------------------------------------- cards

def form_cards(folder=FRAGMENTS):
    """Which fragment folders build forms, read from their cards: one a run
    when the card provides `formId`, many when it provides `formCount` - and
    never a READ, which may count forms it did not build."""
    import yaml
    one, many = {}, {}
    for path in sorted(glob.glob(os.path.join(folder, "*", "fragment.yaml"))):
        try:
            with io.open(path, encoding="utf-8") as fh:
                card = yaml.safe_load(fh) or {}
        except (OSError, yaml.YAMLError):
            continue
        if not isinstance(card, dict) or str(card.get("risk") or "READ") == "READ":
            continue
        provides = ((card.get("contract") or {}).get("provides") or [])
        names = set(p.get("name") for p in provides if isinstance(p, dict))
        name = os.path.basename(os.path.dirname(path))
        capability = str(card.get("capability") or "")
        if "formCount" in names:
            many[name] = capability
        elif "formId" in names:
            one[name] = capability
    return one, many


# ------------------------------------------------------------------ measure

def measure(chat, audit_paths, audit_entries, cards_error=None, cards=None, journals=None,
            runs=None):
    """The quantities. `journals` is (heads, boxes, offset given) from
    read_journal, or None when no --journal was given; `runs`, a list, is
    filled with each joined run's own figures."""
    q = collections.OrderedDict()
    t = chat.path
    searched = source(t, chat.lines_searched, searched=True) if chat.timeline else None

    # THE CHAT -------------------------------------------------------------
    if chat.from_prompt and len(chat.timeline) > 1:
        first, last = chat.timeline[0], chat.timeline[-1]
        span = last.at - first.at
        q["request_to_finish_s"] = measured(
            seconds(span), [source(t, [first.n, last.n])],
            "the first prompt's timestamp to the last line's", "s")
    else:
        span = None
        q["request_to_finish_s"] = not_measured(
            "no prompt from the person opens the window" if chat.timeline
            else "the transcript has no line in the window")

    def bucket(name):
        return sum(gap for _, gap in chat.buckets[name]), [n for n, _ in chat.buckets[name]]

    waiting, waiting_lines = bucket("person")
    model, model_lines = bucket("model")
    tool, tool_lines = bucket("tool")
    other, other_lines = bucket("other")
    if span is not None:
        q["waiting_on_person_s"] = measured(
            seconds(waiting), [source(t, waiting_lines)] if waiting_lines else [searched],
            "intervals ending at a later prompt from the person", "s")
        active = span - waiting
        q["active_s"] = measured(seconds(active),
                                 [source(t, [chat.timeline[0].n, chat.timeline[-1].n]
                                         + waiting_lines)],
                                 "request to finish less waiting on the person", "s")
    else:
        active = model + tool + other
        q["waiting_on_person_s"] = not_measured("request to finish is not measured")
        q["active_s"] = not_measured("request to finish is not measured")
    if chat.timeline:
        q["model_s"] = measured(seconds(model), [source(t, model_lines)] if model_lines
                                else [searched], "intervals ending at an assistant line", "s")
        q["tool_s"] = measured(seconds(tool), [source(t, tool_lines)] if tool_lines
                               else [searched], "intervals ending at a tool's result", "s")
        q["other_s"] = measured(seconds(other), [source(t, other_lines)] if other_lines
                                else [searched],
                                "intervals ending at a user turn nobody typed", "s")
    else:
        for key in ("model_s", "tool_s", "other_s"):
            q[key] = not_measured("the transcript has no line in the window")
    if chat.timeline and active > 0:
        q["model_share_of_active"] = measured(model / active, q["model_s"]["from"],
                                              "the model's own turns over active time")
    else:
        q["model_share_of_active"] = not_measured("no active time to divide by")
    if span:
        q["model_share_of_request"] = measured(model / span, q["model_s"]["from"],
                                               "the model's own turns over request to finish")
    else:
        q["model_share_of_request"] = not_measured("request to finish is not measured")

    # THINKING AND TOKENS ---------------------------------------------------
    responses = [(key, lines) for key, lines in chat.responses.items()
                 if (lines[0].record.get("message") or {}).get("model") != "<synthetic>"]
    usages = []
    for key, lines in responses:
        best = max(lines, key=lambda line: _ms(((line.record.get("message") or {})
                                                .get("usage") or {}).get("output_tokens")) or 0)
        usage = (best.record.get("message") or {}).get("usage") or {}
        detail = usage.get("output_tokens_details")
        thinking = detail.get("thinking_tokens") if isinstance(detail, dict) else None
        output = usage.get("output_tokens")
        usages.append({"key": key, "line": best.n,
                       "thinking": thinking if isinstance(thinking, int) else None,
                       "output": output if isinstance(output, int) else None,
                       "speed": usage.get("speed")})
    _summed(q, "thinking_tokens", usages, "thinking", t,
            "usage.output_tokens_details.thinking_tokens, each response once")
    _summed(q, "output_tokens", usages, "output", t,
            "usage.output_tokens, each response once - thinking, text and tool calls together")

    thoughts = [line for line in chat.timeline if line.kind == "assistant"
                and any(b.get("type") == "thinking" for b in _blocks(line.record))]
    timed = [line for line in thoughts if _ms(line.record.get("thinkingDurationMs")) is not None]
    if not thoughts:
        q["thinking_s"] = (measured(0.0, [searched], "no thinking block in the window", "s")
                           if chat.timeline else not_measured("no line in the window"))
    elif len(timed) < len(thoughts):
        q["thinking_s"] = not_measured(
            "%d of %d thinking lines carry thinkingDurationMs - this transcript does not "
            "record how long thinking took" % (len(timed), len(thoughts)))
    else:
        q["thinking_s"] = measured(
            sum(_ms(line.record["thinkingDurationMs"]) for line in timed) / 1000.0,
            [source(t, [line.n for line in timed])], "thinkingDurationMs on each thinking line", "s")

    for key, read in (("effort", lambda lines, use: lines[0].record.get("effort")),
                      ("model", lambda lines, use: (lines[0].record.get("message") or {})
                       .get("model")),
                      ("speed", lambda lines, use: use["speed"])):
        counted, where = collections.Counter(), []
        for (rkey, lines), use in zip(responses, usages):
            said = read(lines, use)
            if said:
                counted[str(said)] += 1
                where.append(lines[0].n)
        if counted:
            q[key] = measured(dict(counted), [source(t, where)],
                              "as the transcript records it, one count per response")
        else:
            q[key] = not_measured("no response in this transcript records its %s" % key)

    if responses:
        took = [(sum(chat.ending.get(line.n, 0) for line in lines), key, lines)
                for key, lines in responses]
        longest = max(took, key=lambda item: item[0])
        use = next(u for u in usages if u["key"] == longest[1])
        q["longest_model_turn"] = measured(
            seconds(longest[0]), [source(t, [line.n for line in longest[2]])],
            "the response whose lines' intervals add up to the most", "s",
            thinking_tokens=use["thinking"], output_tokens=use["output"],
            at=iso(longest[2][0].at))
    else:
        q["longest_model_turn"] = not_measured("no model response in the window")

    # HERON'S CALLS ---------------------------------------------------------
    by_tool = collections.Counter(c["tool"] for c in chat.calls)
    call_lines = [c["line"].n for c in chat.calls]
    if chat.timeline:
        q["heron_calls"] = measured(len(chat.calls), [source(t, call_lines)] if call_lines
                                    else [searched], "tool_use lines naming a Heron tool",
                                    by_tool=dict(by_tool))
        changes = [c for c in chat.calls if c["tool"] == "revit_change"]
        q["revit_change_calls"] = measured(len(changes),
                                           [source(t, [c["line"].n for c in changes])]
                                           if changes else [searched],
                                           "tool_use lines naming revit_change")
        q["heron_batches"] = measured(len(chat.batches), [source(t, call_lines)] if call_lines
                                      else [searched], "model responses that called Heron")
        answered = [c for c in changes if c["result"] is not None]
        q["revit_change_handback_chars"] = measured(
            sum(len(c["text"] or "") for c in answered),
            [source(t, [c["result"].n for c in answered])] if answered else [searched],
            "characters of every revit_change result, a {\"result\": ...} wrapper decoded",
            "characters")
    else:
        for key in ("heron_calls", "revit_change_calls", "heron_batches",
                    "revit_change_handback_chars"):
            q[key] = not_measured("the transcript has no line in the window")

    # REVIT -----------------------------------------------------------------
    why_not = None
    if not audit_paths:
        why_not = "no audit file was given (--audit), so Revit's own time cannot be read"
    elif not chat.calls:
        why_not = ("no Heron call in the transcript's window, so no audit line can be "
                   "joined to this chat")
    joined, window = _join(chat, audit_entries)
    if why_not:
        for key in ("revit_fragment_s", "revit_fragment_runs", "revit_fragment_median_ms",
                    "revit_other_ops", "revit_other_s", "revit_first_runs",
                    "revit_first_runs_s", "heron_overhead_per_batch_median_s",
                    "heron_overhead_per_call_median_s", "heron_no_revit_calls",
                    "heron_no_revit_median_s", "forms_built"):
            q[key] = not_measured(why_not)
    else:
        # A ZERO NAMES WHAT WAS SEARCHED: the audit lines inside the chat's
        # minutes, or the file when not one line falls inside them.
        searched_audit = ([dict(s, searched=True) for s in audit_sources(window)]
                          or [source(p, []) for p in audit_paths])
        _revit(q, joined, searched_audit, audit_entries)
        _overhead(q, chat, searched_audit)
        _forms(q, chat, joined, searched_audit, cards, cards_error)
    if audit_paths and chat.timeline:
        missed = [e for e in window if not e.get("batch")]
        q["audit_lines_not_joined"] = measured(
            len(missed), audit_sources(missed)
            or [dict(s, searched=True) for s in audit_sources(window)]
            or [source(p, []) for p in audit_paths],
            "audit lines inside the chat's minutes and outside every Heron call - "
            "another chat's, or the person's")
    else:
        q["audit_lines_not_joined"] = not_measured(
            "no audit file was given" if not audit_paths else "no line in the window")
    _keep_boxes(q, chat, joined, journals, runs if runs is not None else [])
    q["heron_server_s_per_call"] = not_measured(
        "Heron's MCP server keeps each call's seconds in memory only - "
        "heron_companion.Activity, for the Companion page - and writes them nowhere. "
        "Heron's time around Revit above is the transcript's time less the audit's ms, "
        "so it includes the MCP client's and Claude Code's own handling")

    forms = q["forms_built"]
    if forms["status"] != MEASURED:
        q["seconds_per_form"] = not_measured("forms built is not measured")
    elif span is None:
        q["seconds_per_form"] = not_measured("request to finish is not measured")
    elif not forms["value"]:
        q["seconds_per_form"] = not_measured("no form was built")
    else:
        q["seconds_per_form"] = measured(
            seconds(span) / forms["value"], q["request_to_finish_s"]["from"] + forms["from"],
            "request to finish over forms built", "s")
    return q


def _summed(q, key, usages, field, path, how):
    carried = [u for u in usages if u[field] is not None]
    if not usages:
        q[key] = not_measured("no model response in the window")
    elif len(carried) < len(usages):
        q[key] = not_measured("%d of %d responses carry it - this transcript does not record "
                              "%s" % (len(carried), len(usages), key.replace("_", " ")))
    else:
        q[key] = measured(sum(u[field] for u in carried),
                          [source(path, [u["line"] for u in carried])], how, "tokens")


def _join(chat, entries):
    """Each audit line inside the chat's minutes, and the batch it falls in."""
    if not chat.timeline:
        return [], []
    low, high = chat.timeline[0].at, chat.timeline[-1].at
    window = [e for e in entries if low <= e["at"] <= high]
    joined = []
    for entry in window:
        entry.pop("batch", None)
        for batch in chat.batches:
            if batch["end"] is not None and batch["start"] <= entry["at"] <= batch["end"]:
                entry["batch"] = batch
                batch["audit"].append(entry)
                joined.append(entry)
                break
    return joined, window


def _revit(q, joined, files, entries):
    runs = [e for e in joined if e["run"]]
    others = [e for e in joined if not e["run"]]
    no_ms = [e for e in joined if e["ms"] is None]
    if no_ms:
        why = "audit line(s) %s carry no ms" % ", ".join(
            "%s:%d" % (os.path.basename(e["file"]), e["n"]) for e in no_ms)
        for key in ("revit_fragment_s", "revit_fragment_median_ms", "revit_other_s",
                    "revit_first_runs_s"):
            q[key] = not_measured(why)
    else:
        q["revit_fragment_s"] = measured(sum(e["ms"] for e in runs) / 1000.0,
                                         audit_sources(runs) or files,
                                         "the ms of every fragment run joined to a Heron call",
                                         "s")
        q["revit_fragment_median_ms"] = (
            measured(statistics.median(e["ms"] for e in runs), audit_sources(runs),
                     "the median ms of those runs", "ms")
            if runs else not_measured("no fragment run joined to this chat"))
        q["revit_other_s"] = measured(sum(e["ms"] for e in others) / 1000.0,
                                      audit_sources(others) or files,
                                      "the ms of every other operation joined to a Heron call",
                                      "s")
    q["revit_fragment_runs"] = measured(len(runs), audit_sources(runs) or files,
                                        "audit lines of a run_fragment_* operation joined to a "
                                        "Heron call")
    q["revit_other_ops"] = measured(len(others), audit_sources(others) or files,
                                    "other audit lines joined to a Heron call")

    seen, first = set(), set()
    for entry in sorted((e for e in entries if e["run"]), key=lambda e: e["at"]):
        key = (entry["session"], entry["fragment"])
        if key not in seen:
            seen.add(key)
            first.add(id(entry))
    firsts = [e for e in runs if id(e) in first]
    for entry in runs:
        entry["first"] = id(entry) in first
    q["revit_first_runs"] = measured(
        len(firsts), audit_sources(firsts) or files,
        "joined runs that are their fragment's first in that Revit session, in the audit "
        "files given - where the fragment is compiled")
    if not no_ms:
        q["revit_first_runs_s"] = measured(sum(e["ms"] for e in firsts) / 1000.0,
                                           audit_sources(firsts) or files,
                                           "the ms of those first runs", "s")


KEEP_KEYS = ("keep_boxes_in_journal", "keep_boxes", "keep_boxes_s", "keep_boxes_discarded",
             "keep_boxes_in_runs", "keep_boxes_in_runs_s", "keep_boxes_not_in_runs",
             "revit_fragment_less_boxes_s", "revit_first_runs_less_boxes_s")


def _box_names(boxes):
    return ", ".join("%s line %d" % (os.path.basename(b["file"]), b["shown_line"]) for b in boxes)


def _keep_boxes(q, chat, joined, journals, runs_out):
    """The Keep box's seconds from Revit's journal, and Revit's own time
    less them, run by run."""
    if journals is None:
        for key in KEEP_KEYS:
            q[key] = not_measured("no journal was given (--journal), so the Keep box's seconds - "
                                  "inside Revit's own time since D-126 - cannot be read")
        return
    heads, boxes, offset_given = journals
    searched = [source(h["file"], [1, h["lines"]], searched=True) for h in heads]
    q["keep_boxes_in_journal"] = measured(
        len(boxes), box_sources(boxes) or searched,
        "TaskDialog lines naming the Keep box, in the whole of every journal given")
    why = None
    if not offset_given:
        why = ("the journal's stamps are the PC's local time with no zone - give "
               "--journal-utc-offset, e.g. +03:00, to place them against the chat and the audit")
    elif not chat.timeline:
        why = "the transcript has no line in the window"
    if why:
        for key in KEEP_KEYS[1:]:
            q[key] = not_measured(why)
        return

    low, high = chat.timeline[0].at, chat.timeline[-1].at
    inside = [b for b in boxes if b["shown"] is not None and low <= b["shown"] <= high]
    where = box_sources(inside) or searched
    q["keep_boxes"] = measured(len(inside), where,
                               "Keep boxes shown in the chat's minutes, by their TaskDialog "
                               "line's stamp")
    unanswered = [b for b in inside if b["seconds"] is None]
    if unanswered:
        q["keep_boxes_s"] = not_measured("the box shown on %s has no answer in the journal, or "
                                         "no stamp in order before it" % _box_names(unanswered))
    else:
        q["keep_boxes_s"] = measured(sum(b["seconds"] for b in inside), where,
                                     "each box's answer stamp less its shown stamp", "s")
    answered = [b for b in inside if b["answer_line"]]
    q["keep_boxes_discarded"] = measured(
        sum(1 for b in answered if b["button"] not in KEEPS), box_sources(answered) or where,
        "answers other than %s - Discard, Esc and the close button are all a Discard (D-126)"
        % " or ".join(repr(k) for k in KEEPS),
        by_answer=dict(collections.Counter(b["button"] or "(no button read)" for b in answered)))

    revit = q.get("revit_fragment_s") or {}
    if revit.get("status") != MEASURED:
        why = "Revit's own time is not measured: %s" % revit.get("why")
    elif unanswered:
        why = q["keep_boxes_s"]["why"]
    if why:
        for key in KEEP_KEYS[4:]:
            q[key] = not_measured(why)
        return

    runs = sorted((e for e in joined if e["run"]), key=lambda e: e["at"])
    held, outside = collections.defaultdict(list), []
    for box in inside:
        home = None
        for entry in runs:
            start = entry["at"] - int(round(entry["ms"] * 1000))
            if start <= box["answered"] <= entry["at"]:
                home = entry
                break
        if home is None:
            outside.append(box)
            continue
        held[id(home)].append((box, box["answered"] - max(box["shown"], start),
                               box["shown"] < start))
    in_runs = [box for entry in runs for box, _, _ in held[id(entry)]]
    homes = [entry for entry in runs if held[id(entry)]]
    boxed = sum(us for entry in runs for _, us, _ in held[id(entry)])
    cut = sum(1 for entry in runs for _, _, early in held[id(entry)] if early)
    where = audit_sources(homes) + box_sources(in_runs) or where
    q["keep_boxes_in_runs"] = measured(
        len(in_runs), where,
        "boxes answered inside a run joined to this chat's calls - from the audit line's at "
        "less its ms to its at")
    q["keep_boxes_in_runs_s"] = measured(
        seconds(boxed), where,
        "those boxes' seconds inside their run; a shown stamp before the run began is cut to "
        "its start", "s", cut=cut)
    q["keep_boxes_not_in_runs"] = measured(
        len(outside), box_sources(outside) or box_sources(inside) or searched,
        "boxes in the chat's minutes answered inside no run of this chat's - another chat's, "
        "or a run nothing joined")
    q["revit_fragment_less_boxes_s"] = measured(
        revit["value"] - seconds(boxed),
        (audit_sources(runs) + box_sources(in_runs)) or revit["from"],
        "Revit's own time less the seconds of the boxes inside its runs", "s")
    first = q.get("revit_first_runs_s") or {}
    firsts = [entry for entry in runs if entry.get("first")]
    if first.get("status") == MEASURED:
        q["revit_first_runs_less_boxes_s"] = measured(
            first["value"] - seconds(sum(us for entry in firsts for _, us, _ in held[id(entry)])),
            (audit_sources(firsts) + box_sources([b for entry in firsts
                                                  for b, _, _ in held[id(entry)]]))
            or first["from"],
            "the first runs' time less the seconds of the boxes inside them", "s")
    else:
        q["revit_first_runs_less_boxes_s"] = not_measured(
            "the first runs' time is not measured: %s" % first.get("why"))

    for entry in runs:
        inner = held[id(entry)]
        box_ms = sum(us for _, us, _ in inner) / 1000.0
        runs_out.append({
            "audit": "%s:%d" % (os.path.basename(entry["file"]), entry["n"]),
            "at": iso(entry["at"]), "fragment": entry["fragment"], "ms": entry["ms"],
            "first_in_session": bool(entry.get("first")),
            "boxes": [{"journal": os.path.basename(box["file"]), "lines": box["lines"],
                       "seconds_in_run": seconds(us), "journal_seconds": box["seconds"],
                       "cut_to_run_start": early, "answer": box["button"]}
                      for box, us, early in inner],
            "box_ms": box_ms, "ms_less_boxes": entry["ms"] - box_ms})


def _overhead(q, chat, files):
    batches, calls = [], []
    alone, alone_lines = [], []
    for batch in chat.batches:
        if not (batch["pure"] and batch["complete"]):
            continue
        lines = [c["line"].n for c in batch["calls"]] + [c["result"].n for c in batch["calls"]]
        took = batch["end"] - batch["start"]
        if not batch["audit"]:
            alone.extend([took / len(batch["calls"])] * len(batch["calls"]))
            alone_lines.extend(lines)
            continue
        if any(e["ms"] is None for e in batch["audit"]):
            continue
        left = took - sum(e["ms"] for e in batch["audit"]) * 1000
        batches.append((left, lines, batch["audit"]))
        calls.extend([left / len(batch["calls"])] * len(batch["calls"]))
    t = chat.path
    if batches:
        where = [source(t, [n for _, lines, _ in batches for n in lines])] + audit_sources(
            [e for _, _, audit in batches for e in audit])
        median, extra = spread([seconds(left) for left, _, _ in batches])
        q["heron_overhead_per_batch_median_s"] = measured(
            median, where, "each batch of Heron calls that reached Revit: its first call line "
            "to its last result, less the joined audit lines' ms", "s", **extra)
        median, extra = spread([seconds(left) for left in calls])
        q["heron_overhead_per_call_median_s"] = measured(
            median, where, "each such batch's time around Revit over its calls", "s", **extra)
    else:
        for key in ("heron_overhead_per_batch_median_s", "heron_overhead_per_call_median_s"):
            q[key] = not_measured("no batch of Heron calls, all answered, reached Revit")
    q["heron_no_revit_calls"] = measured(
        len(alone), [source(t, alone_lines)] + files if alone else files,
        "answered Heron calls in batches no audit line was joined to")
    if alone:
        median, extra = spread([seconds(us) for us in alone])
        q["heron_no_revit_median_s"] = measured(median, [source(t, alone_lines)],
                                                "those calls' share of their batch's time",
                                                "s", **extra)
    else:
        q["heron_no_revit_median_s"] = not_measured("every Heron call reached Revit")


def _forms(q, chat, joined, files, cards, cards_error):
    if cards is None:
        q["forms_built"] = not_measured("the fragment cards could not be read: %s" % cards_error)
        return
    one, many = cards
    folder_of = {}
    for table in (one, many):
        for folder, capability in table.items():
            folder_of[folder] = folder
            if capability:
                folder_of[capability] = folder
    built, from_audit, from_chat, problems = 0, [], [], []
    for entry in joined:
        folder = folder_of.get(str(entry.get("fragment") or ""))
        if entry["ok"] and entry["run"] and folder in one:
            built += 1
            from_audit.append(entry)
    for batch in chat.batches:
        for folder, capability in sorted(many.items()):
            runs = [e for e in batch["audit"]
                    if e["run"] and folder_of.get(str(e.get("fragment") or "")) == folder]
            if not runs:
                continue
            asked = [c for c in batch["calls"]
                     if str(c["input"].get("capability") or "").upper() == capability.upper()]
            if not asked:
                problems.append("no call in the batch on transcript line %d names %s"
                                % (batch["calls"][0]["line"].n, capability))
                continue
            for c in asked:
                found = FORM_COUNT.findall(c["text"] or "")
                if len(found) != 1:
                    problems.append(
                        "the reply on transcript line %s to %s prints no single formCount - "
                        "revit_change does not print a value the last reply of the same "
                        "capability showed" % (c["result"].n if c["result"] else "?",
                                               capability))
                    continue
                built += int(found[0])
                from_chat.append(c["result"].n)
            from_audit.extend(e for e in runs if e["ok"])
    if problems:
        q["forms_built"] = not_measured("; ".join(problems))
        return
    where = audit_sources(from_audit)
    if from_chat:
        where.insert(0, source(chat.path, from_chat))
    q["forms_built"] = measured(
        built, where or files,
        "one for each successful run of a card that provides formId, and each formCount "
        "a card that provides formCount printed")


# ------------------------------------------------------------------- report

ORDER = (
    ("THE CHAT", (
        ("request_to_finish_s", "request to finish"),
        ("waiting_on_person_s", "waiting on the person"),
        ("active_s", "active"),
        ("model_s", "the model's own turns"),
        ("model_share_of_active", "  share of active"),
        ("model_share_of_request", "  share of request to finish"),
        ("tool_s", "tool calls"),
        ("other_s", "turns nobody typed"),
        ("thinking_s", "thinking"),
        ("thinking_tokens", "thinking tokens"),
        ("output_tokens", "output tokens"),
        ("longest_model_turn", "longest model turn"),
        ("model", "model"),
        ("effort", "effort"),
        ("speed", "speed"))),
    ("HERON'S CALLS", (
        ("heron_calls", "Heron calls"),
        ("revit_change_calls", "revit_change calls"),
        ("heron_batches", "batches"),
        ("revit_change_handback_chars", "revit_change hand-back"))),
    ("REVIT, FROM THE AUDIT", (
        ("revit_fragment_s", "Revit's own time"),
        ("revit_fragment_runs", "fragment runs"),
        ("revit_fragment_median_ms", "median a run"),
        ("revit_first_runs", "first runs in a session"),
        ("revit_first_runs_s", "  their time"),
        ("revit_other_ops", "other operations"),
        ("revit_other_s", "  their time"),
        ("audit_lines_not_joined", "audit lines not this chat's"))),
    ("THE KEEP BOX, FROM REVIT'S JOURNAL", (
        ("keep_boxes_in_journal", "boxes in the whole journal"),
        ("keep_boxes", "boxes in the chat's minutes"),
        ("keep_boxes_s", "  shown to answered"),
        ("keep_boxes_discarded", "  not kept"),
        ("keep_boxes_in_runs", "boxes inside this chat's runs"),
        ("keep_boxes_in_runs_s", "  their time in the runs"),
        ("keep_boxes_not_in_runs", "boxes in no run of this chat's"),
        ("revit_fragment_less_boxes_s", "Revit's own time less boxes"),
        ("revit_first_runs_less_boxes_s", "  first runs less the boxes"))),
    ("HERON'S TIME AROUND REVIT", (
        ("heron_overhead_per_batch_median_s", "median a batch"),
        ("heron_overhead_per_call_median_s", "median a call"),
        ("heron_no_revit_calls", "calls that reached no Revit"),
        ("heron_no_revit_median_s", "  median a call"),
        ("heron_server_s_per_call", "Heron's server alone"))),
    ("FORMS", (
        ("forms_built", "forms built"),
        ("seconds_per_form", "seconds a form"))),
)


def _value_text(key, entry):
    value, unit = entry["value"], entry.get("unit")
    if isinstance(value, dict):
        return ", ".join("%s x%d" % (k, v) for k, v in sorted(value.items()))
    if key.startswith("model_share"):
        return "%.1f%%" % (value * 100)
    if unit == "s":
        text = "%.3f s" % value if abs(value) < 10 else "%.1f s" % value
    elif unit == "ms":
        text = "%.0f ms" % value
    else:
        text = "%s%s" % (value, " " + unit if unit else "")
    extra = []
    if "middle_half" in entry:
        extra.append("middle half %.3f to %.3f" % tuple(entry["middle_half"]))
    if "low" in entry and entry.get("count", 0) > 1:
        extra.append("range %.3f to %.3f, %d" % (entry["low"], entry["high"], entry["count"]))
    if entry.get("thinking_tokens") is not None or "output_tokens" in entry:
        extra.append("%s thinking tokens, %s output" % (
            entry.get("thinking_tokens") if entry.get("thinking_tokens") is not None
            else "NOT MEASURED", entry.get("output_tokens")))
    if entry.get("by_tool"):
        extra.append(", ".join("%s %d" % kv for kv in sorted(entry["by_tool"].items())))
    if entry.get("by_answer"):
        extra.append("answered " + ", ".join('"%s" %d' % kv
                                              for kv in sorted(entry["by_answer"].items())))
    if entry.get("cut"):
        extra.append("%d shown before its run began, cut to the start" % entry["cut"])
    return text + (" (%s)" % "; ".join(extra) if extra else "")


def _where(entry):
    parts = []
    for s in entry.get("from") or []:
        name, lines = os.path.basename(s["file"]), s["lines"]
        if not lines:
            parts.append("%s, no line" % name)
        elif s.get("searched"):
            parts.append("%s lines %d to %d, searched" % (name, lines[0], lines[-1]))
        elif len(lines) <= 6:
            parts.append("%s line%s %s" % (name, "s" if len(lines) > 1 else "",
                                           ", ".join(str(n) for n in lines)))
        else:
            parts.append("%s, %d lines from %d to %d" % (name, len(lines), lines[0], lines[-1]))
    return "; ".join(parts)


def print_report(report):
    print("WHERE A FAMILY BUILD'S MINUTES WENT - read, never estimated")
    print("=" * 78)
    head = report["transcript"]
    print("  transcript  %s  (%d lines; %d sidechain skipped; %d unreadable)" % (
        head["file"], head["lines_read"], len(head["sidechain_skipped"]),
        len(head["unreadable"])))
    for trail in report["audit"]:
        print("  audit       %s  (%d lines; %d unreadable%s)" % (
            trail["file"], trail["lines"], len(trail["unreadable"]),
            ": " + ", ".join(str(n) for n in trail["unreadable"]) if trail["unreadable"] else ""))
    for skipped in report.get("audit_skipped") or []:
        print("  not read    %s  (not the add-in's audit-YYYYMM.jsonl)" % skipped)
    if not report["audit"]:
        print("  audit       none given")
    for journal in report.get("journal") or []:
        print("  journal     %s  (%d lines, %s; %d Keep boxes%s%s)" % (
            journal["file"], journal["lines"], journal["encoding"], journal["boxes"],
            "; answers with no box shown: lines %s" % ", ".join(
                str(n) for n in journal["answers_with_no_box"])
            if journal["answers_with_no_box"] else "",
            "; stamps not read: lines %s" % ", ".join(str(n) for n in journal["unreadable_stamps"])
            if journal["unreadable_stamps"] else ""))
    if report.get("journal_utc_offset"):
        print("  its clock   UTC%s, as --journal-utc-offset says" % report["journal_utc_offset"])
    window = report["window"]
    if window.get("first"):
        print("  window      %s to %s" % (window["first"], window["last"]))
    quantities = report["quantities"]
    for title, rows in ORDER:
        print()
        print(title)
        for key, label in rows:
            entry = quantities[key]
            if entry["status"] == MEASURED:
                print("  %-30s %s" % (label, _value_text(key, entry)))
                print("  %-30s   from %s" % ("", _where(entry)))
            else:
                print("  %-30s NOT MEASURED - %s" % (label, entry["why"]))
    if report.get("runs"):
        print()
        print("RUNS JOINED TO THIS CHAT - Revit's own time less the Keep box, run by run")
        for run in report["runs"]:
            boxes = run["boxes"]
            print("  %-24s %-28s %8.0f ms  %s  less the box %8.0f ms" % (
                run["audit"], run["fragment"] or "?", run["ms"],
                "box %6.3f s" % (run["box_ms"] / 1000.0) if boxes else "no box     ",
                run["ms_less_boxes"]))
            for box in boxes:
                print("  %-24s   from %s line%s %s%s" % (
                    "", box["journal"], "s" if len(box["lines"]) > 1 else "",
                    ", ".join(str(n) for n in box["lines"]),
                    "; shown before the run began, cut to its start"
                    if box["cut_to_run_start"] else ""))
    if report["requests"]:
        print()
        print("REQUESTS - each prompt to the last line before the next")
        for r in report["requests"]:
            print("  line %-6d %s  %8.1f s  to line %d" % (r["line"], r["at"], r["seconds"],
                                                          r["end_line"]))
    print()
    print("A report, not a gate: it exits 0 whatever it finds.")


def requests_of(chat):
    prompts = [i for i, line in enumerate(chat.timeline) if line.kind == "prompt"]
    found = []
    for k, i in enumerate(prompts):
        end = (prompts[k + 1] - 1) if k + 1 < len(prompts) else len(chat.timeline) - 1
        last = chat.timeline[end]
        found.append({"line": chat.timeline[i].n, "at": iso(chat.timeline[i].at),
                      "seconds": seconds(last.at - chat.timeline[i].at), "end_line": last.n})
    return found


def batches_of(chat):
    return [{"lines": [c["line"].n for c in b["calls"]], "calls": len(b["calls"]),
             "tools": [c["tool"] for c in b["calls"]], "pure": b["pure"],
             "answered": b["complete"],
             "seconds": seconds(b["end"] - b["start"]) if b["complete"] else None,
             "audit_lines": ["%s:%d" % (os.path.basename(e["file"]), e["n"]) for e in b["audit"]],
             "revit_ms": sum(e["ms"] or 0 for e in b["audit"])} for b in chat.batches]


def main(argv=None):
    parser = argparse.ArgumentParser(description="Where a family build's minutes went.")
    parser.add_argument("transcript", nargs="?", help="a Claude Code transcript (.jsonl)")
    parser.add_argument("--audit", action="append", default=[],
                        help="the add-in's audit-YYYYMM.jsonl, or the folder holding it")
    parser.add_argument("--since", help="ISO time; lines before it are left out (UTC if no zone)")
    parser.add_argument("--until", help="ISO time; lines after it are left out (UTC if no zone)")
    parser.add_argument("--journal", action="append", default=[],
                        help="Revit's journal.NNNN.txt for the session that ran the build")
    parser.add_argument("--journal-utc-offset",
                        help="how far the journal's local clock is from UTC, e.g. +03:00 - "
                             "its stamps carry no zone; one behind UTC is written "
                             "--journal-utc-offset=-05:00")
    parser.add_argument("--server", default="heron", help="the MCP server's name (heron)")
    parser.add_argument("--form-cards", action="store_true",
                        help="list which capabilities build forms, from their cards")
    parser.add_argument("--json", action="store_true", help="machine-readable output")
    args = parser.parse_args(argv)

    cards, cards_error = None, None
    try:
        cards = form_cards()
    except ImportError as error:
        cards_error = "PyYAML is not installed (%s)" % error

    if args.form_cards:
        if cards is None:
            print("Cannot read the cards: %s" % cards_error)
            return 2
        listing = {"one_form": sorted(cards[0]), "many_forms": sorted(cards[1])}
        if args.json:
            print(json.dumps(listing, indent=2))
        else:
            print("Build one form a run (provide formId):  %s" % ", ".join(listing["one_form"]))
            print("Build many, said by formCount:          %s" % ", ".join(listing["many_forms"]))
        return 0

    if not args.transcript:
        parser.error("name a transcript, or ask for --form-cards")
    since, until = micros(args.since), micros(args.until)
    for given, got, name in ((args.since, since, "--since"), (args.until, until, "--until")):
        if given and got is None:
            parser.error("%s %r is not an ISO time" % (name, given))
    offset = utc_offset(args.journal_utc_offset) if args.journal_utc_offset else None
    if args.journal_utc_offset and offset is None:
        parser.error("--journal-utc-offset %r is not Z or +HH:MM" % args.journal_utc_offset)
    if args.journal_utc_offset and not args.journal:
        parser.error("--journal-utc-offset says how to read a journal - name one with --journal")
    if not os.path.isfile(args.transcript):
        print("No transcript at %s - nothing was read." % args.transcript)
        return 2
    files, skipped, missing = audit_files(args.audit)
    if missing:
        print("No audit file or folder at %s - nothing was read." % ", ".join(missing))
        return 2
    gone = [path for path in args.journal if not os.path.isfile(path)]
    if gone:
        print("No journal at %s - nothing was read." % ", ".join(gone))
        return 2

    lines, head = read_transcript(args.transcript, since, until)
    chat = Chat(args.transcript, lines, args.server)
    entries, trails = [], []
    for path in files:
        got, trail = read_audit(path)
        entries.extend(got)
        trails.append(trail)
    journals, heads = None, []
    if args.journal:
        boxes = []
        for path in args.journal:
            got, journal = read_journal(path, offset)
            boxes.extend(got)
            heads.append(journal)
        journals = (heads, boxes, offset is not None)
    runs = []
    report = {
        "transcript": head,
        "audit": trails,
        "audit_skipped": skipped,
        "journal": heads,
        "journal_utc_offset": args.journal_utc_offset,
        "window": {"since": args.since, "until": args.until,
                   "first": iso(chat.timeline[0].at) if chat.timeline else None,
                   "last": iso(chat.timeline[-1].at) if chat.timeline else None,
                   "out_of_order_lines": chat.backwards},
        "quantities": measure(chat, files, entries, cards_error, cards, journals, runs),
        "runs": runs,
        "requests": requests_of(chat),
        "batches": batches_of(chat),
    }
    if args.json:
        print(json.dumps(report, indent=2))
    else:
        print_report(report)
    return 0


if __name__ == "__main__":
    sys.exit(main())
