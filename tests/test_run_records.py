#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The run records are paged back from disk, and the short report says only what
Revit said.

    python tests/test_run_records.py

WHY THIS EXISTS
---------------
`revit_change` used to send every word of a write's reply into the chat, and
two family builds carried 322,725 characters that way (FRAGMENT-ISSUES
5b-407). It now prints a short change report and points to a run record the
add-in wrote under HeronPaths.Runs; `heron_run_record` pages that record back.
Everything that does so is mcp/server/heron_run_records.py, which needs no Revit, no
pipe and no MCP SDK - so this runs all of it, on records it writes for itself
into a folder of its own.

WHAT IT PROVES
  1. A RUN ID IS THE SAME SHAPE ON BOTH SIDES, and nothing that could be a
     path out of the folder is one.
  2. A RECORD PAGES: every line once, in a fixed order, the header on every
     page, the next offset named until the last page, and a page past the end
     said as such. Offset and limit are clamped, never trusted.
  3. A RECORD THAT IS NOT THERE IS SAID TO BE MISSING, never empty; one that
     cannot be read says so; nothing raises.
  4. THE NEWEST RECORDS ARE LISTED NEWEST FIRST, by the id's own time.
  5. THE CHANGE LINES PRINT REVIT'S COUNTS ONLY WHEN REVIT WAS SEEN TO REPORT.
     No event at all is never "nothing changed".
  6. ONCE PER BATCH, AND ONLY WHERE NOTHING IS LOST. The KEPT paragraph is
     shortened only for a kept change, only to the add-in's own sentence, and
     a rollback, an unconfirmed one and a reply from an add-in before the
     change print whole every time. A value the last reply of the same
     capability showed word for word is counted rather than printed - only
     with a run record to read it from, and never a value that changed.
  7. THE TOOL IS DECLARED READ AND CALLS NO OPERATION, and revit_change uses
     these and nothing of its own.

MEASURED AGAINST THE CODE AS IT STOOD: with no heron_run_records.py and no
heron_run_record, section 1 fails cleanly and every later section says it
could not be reached - exit 1, not a traceback.
"""

import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as bridge                            # noqa: E402
import heron_tools as tools                                     # noqa: E402

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load():
    """heron_run_records, or None - one clean failure rather than a traceback (heron-ship 2a)."""
    try:
        import heron_run_records
        return heron_run_records
    except ImportError as why:
        check(False, "mcp/server/heron_run_records.py imports (%s)" % why)
        return None


def reply_with(changes=None, run=None, **extra):
    reply = {"ok": True, "ran": "create-family-extrusion", "document": "Panel",
             "bound": "workPlane, profile, startMm, endMm, isSolid as given",
             "provides": {"formId": "b753cb5a-00000b89", "built": "Solid extrusion",
                          "problems": "0 item(s)"},
             "verdict": "'create-family-extrusion' was KEPT: Revit accepted the transaction ...",
             "verdictKind": "kept",
             "verdictShort": "'create-family-extrusion' was KEPT. IF anything was written, all "
                             "of it is ONE undo step; if nothing was, there is no new undo entry "
                             "and Ctrl+Z would undo whatever you did before this.",
             "warningsDismissed": []}
    if changes is not None:
        reply["changes"] = changes
    if run is not None:
        reply["run"] = run
    reply.update(extra)
    return reply


def part(added=0, modified=0, deleted=0, rows=None):
    return {"added": added, "modified": modified, "deleted": deleted,
            "total": added + modified + deleted, "byCategory": rows or [], "firstIds": []}


def ids_and_shape(runs):
    print("1. a run id is the same shape on both sides, and never a path")
    cs = io.open(os.path.join(ROOT, "platform", "Heron.Core", "HeronRuns.cs"),
                 encoding="utf-8").read()
    pattern = re.search(r'new Regex\("([^"]+)"', cs)
    check(pattern is not None and pattern.group(1) == runs.RUN_ID.pattern,
          "heron_run_records.RUN_ID is HeronRuns.cs's pattern exactly - %s"
          % (pattern.group(1) if pattern else "none found"))
    check(runs.is_run_id("wf-20261009-133432-0042-a1b2"), "a workflow id is a run id")
    bad = ["", None, "wf-", "../wf-1", "wf-1/../../x", "wf-1.json", "C:wf-1", "wf 1",
           "run-1", "wf-1\\x", "wf-" + "a" * 65]
    check(not any(runs.is_run_id(one) or runs.record_path(one, "/tmp") for one in bad),
          "and a separator, a dot, a drive, a space, another prefix or an id too long is not")
    check(runs.record_path("wf-1") == os.path.join(bridge.RUNS_DIR, "wf-1.json"),
          "with no folder named, a record is read from the client's RUNS_DIR")


def paging(runs, folder):
    print("\n2. a record pages")
    run = "wf-20261009-133432-0042-a1b2"
    elements = [{"part": "fragment", "change": "added", "id": str(100 + i),
                 "category": "Generic Models"} for i in range(45)]
    elements.append({"part": "setup", "change": "deleted", "id": "7"})
    reply = reply_with(changes={"events": 2, "unreadable": 0, "witnessed": True, "total": 46,
                                "fragment": part(45, rows=[{"category": "Generic Models",
                                                            "added": 45, "modified": 0}]),
                                "setup": part(deleted=1)},
                       run=run,
                       warningsDismissed=[{"text": "Line is slightly off axis", "times": 2,
                                           "elements": 1, "step": None}])
    record = {"record": 1, "workflow": run, "at": "2026-10-09T13:34:32Z",
              "op": "run_fragment_write", "fragment": "create-family-extrusion",
              "document": "Panel", "elements": elements, "reply": reply}
    with io.open(os.path.join(folder, run + ".json"), "w", encoding="utf-8") as fh:
        json.dump(record, fh)

    got, why = runs.read(run, folder)
    check(got is not None and why is None, "the record is read back (%s)" % (why or "ok"))
    if got is None:
        return
    every = runs.entries(got)
    check(len(every) == 1 + 3 + 46 + 1 + 1,
          "one line per value, element, warning and the verdict, and the inputs - %d" % len(every))
    check(every[0].startswith("inputs") and every[1].startswith("value     built")
          and every[4].startswith("fragment  added    100  Generic Models")
          and every[-2].startswith("warning") and every[-1].startswith("verdict"),
          "in a fixed order: inputs, values by name, elements as Revit named them, "
          "warnings, the verdict")
    check(any(line.startswith("setup     deleted  7") and "Generic" not in line for line in every),
          "a deleted element is listed with no category - it had none left to read")

    first = runs.page(got, run, 0, 20)
    check(first.splitlines()[0].startswith("Run %s: create-family-extrusion in Panel" % run)
          and "KEPT" in first.splitlines()[0],
          "the header names the run, the fragment, the model and how it ended")
    check("Lines 0-19 of 52:" in first and first.rstrip().endswith(
              "More: heron_run_record run=%s offset=20" % run),
          "a page says which lines it holds and names the next offset")
    check("  changed: 45 added (Generic Models 45)" in first
          and "  setup:   1 deleted" in first,
          "and carries the change report on every page")
    last = runs.page(got, run, 40, 20)
    check("Lines 40-51 of 52:" in last and "More:" not in last,
          "the last page holds the rest and names no next offset")
    past = runs.page(got, run, 500, 20)
    check("Nothing at offset 500" in past and "the last at offset 51" in past,
          "a page past the end says so and names the last offset that has anything")
    check("Lines 0-0 of 52:" in runs.page(got, run, -5, 1),
          "a negative offset is read as the start")
    check("Lines 0-%d of 52:" % (min(52, runs.DEFAULT_LIMIT) - 1)
          in runs.page(got, run, "x", "y"),
          "an offset or limit that is not a number falls back rather than raising")
    many = dict(got, elements=[dict(elements[0], id=str(i)) for i in range(500)])
    check("Lines 0-%d of" % (runs.MAX_LIMIT - 1) in runs.page(many, run, 0, 10000),
          "and a limit is clamped to %d, however large the ask" % runs.MAX_LIMIT)


def missing(runs, folder):
    print("\n3. a record that is not there is missing, never empty")
    record, why = runs.read("wf-20200101-000000-0000-dead", folder)
    check(record is None and "no record" in why and "retainDays" in why,
          "a missing record says it is not on this PC and why it may have gone")
    record, why = runs.read("../../etc/passwd", folder)
    check(record is None and "not a run id" in why,
          "an id that is not one is refused before any file is opened")
    with io.open(os.path.join(folder, "wf-broken.json"), "w", encoding="utf-8") as fh:
        fh.write("{not json")
    record, why = runs.read("wf-broken", folder)
    check(record is None and "could not be read" in why, "a torn record says it could not be read")
    with io.open(os.path.join(folder, "wf-list.json"), "w", encoding="utf-8") as fh:
        fh.write("[1, 2]")
    record, why = runs.read("wf-list", folder)
    check(record is None and "not a record" in why, "and JSON that is not a record is not one")


def newest(runs, folder):
    print("\n4. the newest records first")
    # A FOLDER OF ITS OWN: the records the sections above left would sort in
    # among these by name, which says nothing about the order of jobs.
    folder = os.path.join(folder, "newest")
    os.makedirs(folder)
    for run in ("wf-20261009-120000-0001-aaaa", "wf-20261010-090000-0002-bbbb"):
        with io.open(os.path.join(folder, run + ".json"), "w", encoding="utf-8") as fh:
            json.dump({"fragment": "set-family-form-material", "document": "Panel",
                       "reply": reply_with(changes={"total": 3})}, fh)
    with io.open(os.path.join(folder, "notes.json"), "w", encoding="utf-8") as fh:
        fh.write("{}")
    found = runs.recent(folder, 10)
    order = [one["run"] for one in found]
    check(order == ["wf-20261010-090000-0002-bbbb", "wf-20261009-120000-0001-aaaa"],
          "newest by the id's own time, and a file beside them that is not a record "
          "never listed - %s" % order)
    check(found and found[0]["fragment"] == "set-family-form-material"
          and found[0]["total"] == 3 and found[0]["kind"] == "kept",
          "each with its fragment, how many elements it changed and how it ended")
    check([one["run"] for one in runs.recent(folder, 1)] == order[:1],
          "and a limit keeps the newest")
    with io.open(os.path.join(folder, "wf-20261011-000000-0003-cccc.json"), "w",
                 encoding="utf-8") as fh:
        json.dump({"fragment": "x", "reply": ["not", "an", "object"]}, fh)
    try:
        odd = runs.recent(folder, 10)
        listed = odd[0]["run"] == "wf-20261011-000000-0003-cccc" and odd[0]["total"] is None
    except (AttributeError, TypeError) as why:
        listed = False
        print("    raised: %s" % why)
    check(listed, "a record whose reply is not an object is still listed, never raised on")
    check(runs.recent(os.path.join(folder, "no-such-folder")) == [],
          "and no folder at all is no records, not an error")


def change_lines(runs):
    print("\n5. the change lines print Revit's counts only when Revit was seen to report")
    rows = [{"category": c, "added": n, "modified": 0}
            for c, n in (("Generic Models", 1), ("Lines", 4), ("Sketches", 1),
                         ("Reference Planes", 1), ("Dimensions", 1))]
    said = runs.change_lines(reply_with(
        changes={"witnessed": True, "events": 1, "total": 9,
                 "fragment": part(8, 1, rows=rows + [{"category": "Generic Models",
                                                      "added": 0, "modified": 1}]),
                 "setup": part()},
        run="wf-20261009-133432-0042-a1b2"))
    check(len(said) == 2 and said[0].startswith("  changed: 8 added (")
          and "and 1 more category" in said[0] and "1 modified (Generic Models 1)" in said[0],
          "a witnessed change names at most %d categories and counts the rest: %s"
          % (runs.CATEGORIES_SHOWN, said[:1]))
    check(said[-1] == "  record:  heron_run_record run=wf-20261009-133432-0042-a1b2 "
                      "(every id and value)",
          "and points to the record that holds every id")
    check(not any(line.startswith("  setup:") for line in said),
          "a setup that changed nothing gets no line")

    quiet = runs.change_lines(reply_with(changes={"witnessed": False, "events": 0,
                                                  "unreadable": 0, "total": 0}))
    check(len(quiet) == 1 and "not proof that nothing changed" in quiet[0],
          "no event at all is said as such - never 'nothing changed'")
    torn = runs.change_lines(reply_with(changes={"witnessed": False, "events": 3,
                                                 "unreadable": 1, "total": 2}))
    check(len(torn) == 1 and "could not be read" in torn[0] and "1 of" in torn[0],
          "an unreadable event makes every count incomplete, and says so")
    nothing = runs.change_lines(reply_with(changes={"witnessed": True, "events": 1, "total": 0,
                                                    "fragment": part(), "setup": part()}))
    check(nothing == ["  changed: nothing, by Revit's account"],
          "a witnessed job that changed nothing says it on Revit's authority")
    check(runs.change_lines(reply_with()) == [] and runs.change_lines(None) == [],
          "a reply with no change report - a read, an older add-in - adds no line")
    check(not any("record:" in line for line in runs.change_lines(
              reply_with(changes={"witnessed": True, "total": 0}, run="../x"))),
          "and a `run` that is not a run id is never offered as one")


def batching(runs):
    print("\n6. once per batch: the KEPT paragraph, and a value the last reply showed")
    kept = reply_with()
    b = runs.Batch(gap=3.0)
    check(b.turn(100.0, 100.2), "the first call opens a batch")
    first = runs.verdict_line(kept, b)
    check(not b.turn(100.3, 100.5), "a call starting inside the gap is the same batch")
    second = runs.verdict_line(kept, b)
    check(first == kept["verdict"], "the first kept change prints the paragraph whole")
    check(second == kept["verdictShort"] and "ONE undo step" in second
          and "Ctrl+Z would undo whatever you did before this" in second,
          "the next in the same batch prints the add-in's sentence - the undo rule whole")
    check(b.turn(100.5 + 3.5, 104.2) and runs.verdict_line(kept, b) == kept["verdict"],
          "and after a pause longer than the gap, a new batch and the paragraph again")
    check(not b.turn(104.3, 104.4) and runs.verdict_line(kept, b) == kept["verdictShort"],
          "every call moves the clock on, so a long quick batch stays one batch")

    rolled = reply_with(verdict="NOTHING WAS KEPT ...", verdictKind="rolledBack",
                        verdictShort=None)
    unsure = reply_with(verdict="THE ROLLBACK DID NOT REPORT SUCCESS ...",
                        verdictKind="notConfirmed", verdictShort="must never be printed")
    check(runs.verdict_line(rolled, b) == rolled["verdict"]
          and runs.verdict_line(unsure, b) == unsure["verdict"],
          "a rollback, and one Revit did not confirm, are printed whole every time")
    after_rollback = runs.Batch()
    after_rollback.turn(1.0, 1.1)
    runs.verdict_line(rolled, after_rollback)
    check(runs.verdict_line(kept, after_rollback) == kept["verdict"],
          "and a batch that began with a rollback still prints its first KEPT paragraph whole")
    old = {"ok": True, "verdict": "'x' was KEPT: the paragraph from an add-in before this"}
    b2 = runs.Batch()
    b2.turn(1.0, 1.1)
    check(runs.verdict_line(old, b2) == old["verdict"]
          and runs.verdict_line(old, b2) == old["verdict"],
          "a reply from an add-in that writes no short form prints exactly as before")
    check(runs.verdict_line(dict(kept, verdictShort=""), runs.Batch()) == kept["verdict"]
          and runs.verdict_line({"ok": True}, b2) is None,
          "and an empty short form, or no verdict at all, is never shortened into nothing")

    run = "wf-20261009-133432-0042-a1b2"
    values = runs.Batch()
    values.turn(1.0, 1.1)
    one = dict(reply_with(run=run), provides={"formId": "a", "problems": "0 item(s)",
                                              "mm": "Func`2"})
    shown, same = runs.shown_values(one, "CREATE_FAMILY_EXTRUSION", values)
    check(shown == one["provides"] and same == [],
          "the first reply of a capability in a batch prints every value")
    two = dict(one, provides={"formId": "b", "problems": "0 item(s)", "mm": "Func`2"})
    values.turn(1.2, 1.3)
    shown, same = runs.shown_values(two, "create_family_extrusion", values)
    check(shown == {"formId": "b"} and same == ["mm", "problems"],
          "the next prints what differs and names what is word for word the same")
    check(runs.unchanged_line(same) == ["  and 2 values unchanged from the last reply - "
                                        "every one is in the record."]
          and runs.unchanged_line(["mm"])[0].startswith("  and 1 value unchanged")
          and runs.unchanged_line([]) == [],
          "and the reply says on one line how many were left out, and where they are")
    refusing = dict(two, provides={"formId": "", "problems": "1 item(s) [no such plane]",
                                   "mm": "Func`2"})
    values.turn(1.4, 1.5)
    shown, same = runs.shown_values(refusing, "CREATE_FAMILY_EXTRUSION", values)
    check(shown.get("problems") == "1 item(s) [no such plane]" and "problems" not in same,
          "a value that changed - a refusal above all - is printed as ever")
    other = dict(two)
    values.turn(1.6, 1.7)
    shown, same = runs.shown_values(other, "SET_FAMILY_FORM_MATERIAL", values)
    check(same == [], "another capability's values are its own")
    no_record = dict(two, run=None)
    values.turn(1.8, 1.9)
    runs.shown_values(two, "X", values)
    shown, same = runs.shown_values(no_record, "X", values)
    check(same == [] and shown == two["provides"],
          "and with no run record nothing is left out - there would be nowhere to read it")
    values.turn(1.9 + 10, 12.0)
    shown, same = runs.shown_values(two, "X", values)
    check(same == [], "a new batch prints every value again")


def declared(runs):
    print("\n7. the tool is READ and calls no operation; revit_change uses these")
    check(tools.TOOLS.get("heron_run_record") == (tools.READ, None),
          "heron_run_record is declared READ with no bridge operation - found %r"
          % (tools.TOOLS.get("heron_run_record"),))
    check(not tools.writes("heron_run_record"), "so it cannot write")
    server = io.open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                     encoding="utf-8").read()
    check(re.search(r"@server\.tool\(\)\s*\ndef heron_run_record\(run: str = \"\", "
                    r"offset: int = 0, limit: int = 40\) -> str:", server) is not None,
          "the server serves it with a run, an offset and a limit")
    body = server.split("\ndef heron_run_record(", 1)[-1].split("\n@server.tool()", 1)[0]
    check("session" not in body and "request(" not in body and "binding" not in body,
          "and it opens no session and sends nothing - it reads a file")
    change = server.split("\ndef revit_change(", 1)[-1].split("\n@server.tool()", 1)[0]
    lines_at = change.find("runs.change_lines(reply)")
    verdict_at = change.find("runs.verdict_line(reply, _BATCH)")
    check(lines_at > 0 and verdict_at > lines_at,
          "revit_change prints the change lines, then the verdict through the batch")
    check(change.find("_BATCH.turn(started, time.monotonic())") > 0
          and change.find("_BATCH.turn(") < lines_at,
          "and places each call in the batch once its answer is in hand")
    check(re.search(r'"""\n    # [^\n]*\n(    #[^\n]*\n)*    started = time\.monotonic\(\)', change)
          is not None,
          "and takes the time it STARTED before anything else, for the batch rule")


def main():
    runs = load()
    if runs is None:
        print("\nFAILED - heron_run_records is not there, so nothing past section 1 could be reached.")
        return 1
    folder = tempfile.mkdtemp(prefix="heron-runs-")
    try:
        ids_and_shape(runs)
        paging(runs, folder)
        missing(runs, folder)
        newest(runs, folder)
        change_lines(runs)
        batching(runs)
        declared(runs)
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for what in FAILURES:
            print("  - " + what)
        return 1
    print("PASSED - a run record pages back from disk, and the short report says")
    print("only what Revit said - never 'nothing changed' from silence.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
