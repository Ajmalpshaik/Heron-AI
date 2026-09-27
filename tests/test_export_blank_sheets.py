# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   4
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A sheet carrying a schedule is not a sheet that would print blank.

    python tests/test_export_blank_sheets.py

WHAT WENT WRONG
---------------
`revit_export_check` counts `sheetsThatWouldPrintBlank` in RevitExport.cs.
Its comment said the count was "Counted by RevitSheets and repeated", and it
was repeated from the version that counted a sheet's contents with
`ViewSheet.GetAllPlacedViews` - viewports only. A schedule goes on a sheet as
a ScheduleSheetInstance and is never in that list, so in "heron ai bulding"
(Revit 2024) sheets M-105, M-106 and M-107, each carrying only a schedule,
would be counted among the sheets that "would print BLANK". Read from the
source, never run. FRAGMENT-ISSUES 5b-239, found fixing 5b-238, which is the
same defect in `revit_sheets` and is held by tests/test_sheet_contents.py.

WHAT THIS HOLDS
---------------
  1. THE LINE A MODELLER READS. `_export_blank_line` and
     `_export_blank_caveat` are lifted out of the server and RUN: an add-in
     that says it counted schedules is believed, and prints no warning.
  2. AN OLDER ADD-IN IS NOT BELIEVED. This server and the add-in go live at
     different restarts. An add-in that does not send `schedulesCounted`
     counted views only, so its blank count may hold schedule sheets, and
     the answer says so - but only when it has a blank sheet to qualify.
  3. THE ADD-IN COUNTS SCHEDULES, READ AS TEXT. RevitExport.cs asks
     RevitSheets.ScheduleCountBySheet - the Sheet Agent's own count, which
     leaves the titleblock's revision schedule out - rather than keeping a
     copy, calls a sheet blank only with no view AND no schedule, and sends
     `schedulesCounted`.
  4. THE TOOL USES ALL OF IT. revit_export_check prints its blank line and
     its warning from these helpers and no longer carries the raw line.

WHAT IT CANNOT DO
-----------------
Run the add-in. Whether Revit's collector finds each schedule and whether the
revision schedule is really left out is a question for a real model - see
docs/needs-checking/group-aw.md for the read owed on "heron ai bulding".
"""

import io
import os
import re
import sys

for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except (AttributeError, ValueError):
        pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
EXPORT_CS = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitExport.cs")
SHEETS_CS = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitSheets.cs")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read().replace("\r\n", "\n")


def lifted(text, name):
    """One top-level helper out of the server, compiled and ready to call.

    The server imports the MCP SDK, which CI leaves out on purpose, so the
    function is read as text and run alone. Two blank lines end a top-level
    function in that file. A helper that is missing, renamed or grown a
    dependency on the module comes back None - ONE clean failure, never a
    traceback that hides every check after it.
    """
    start = text.find("\ndef %s(" % name)
    if start < 0:
        return None
    end = text.find("\n\n\n", start)
    if end < 0:
        return None
    namespace = {}
    try:
        exec(text[start + 1:end], namespace)              # noqa: S102
    except BaseException:                                 # noqa: BLE001
        return None
    return namespace.get(name)


def body_of(text, name):
    """One function's source, from its def to the next top-level def."""
    start = text.find("\ndef %s(" % name)
    if start < 0:
        return ""
    rest = text[start + 1:]
    end = re.search(r"\n(?:@server\.tool\(\)\n)?def ", rest)
    return rest[:end.start()] if end else rest


def reply(blank, counted="absent", sheets=7):
    """A check_export reply, as the add-in sends it."""
    row = {"ok": True, "sheets": sheets, "placeholderSheets": 0,
           "sheetsThatWouldPrintBlank": blank, "links": 0, "linksNotLoaded": 0,
           "rooms": 0, "roomsWithNoArea": 0, "worthSending": blank == 0}
    if counted != "absent":
        row["schedulesCounted"] = counted
    return row


def main():
    server = read(SERVER)
    export = read(EXPORT_CS)
    sheets = read(SHEETS_CS)

    line = lifted(server, "_export_blank_line")
    caveat = lifted(server, "_export_blank_caveat")

    print("1. The line a modeller reads")
    check(line is not None, "_export_blank_line exists in the server and runs alone")
    check(caveat is not None, "_export_blank_caveat exists in the server and runs alone")
    if line is not None:
        clean = line(reply(0, True))
        check(re.search(r"would print blank\s+0$", clean) is not None,
              "a new add-in with no blank sheet prints the count and nothing "
              "else - got %r" % clean)
        counted = line(reply(2, True))
        check(re.search(r"would print blank\s+2$", counted) is not None,
              "a new add-in's blank count is believed as it stands - got %r"
              % counted)
    if caveat is not None:
        check(caveat(reply(2, True)) is None,
              "and it carries no warning about schedules")
        check(caveat(reply(0, True)) is None,
              "nor does a new add-in that found nothing blank")

    print("2. An older add-in is not believed")
    if line is not None:
        old = line(reply(3))
        check("would print blank" in old and "3" in old
              and "schedules not counted" in old,
              "with no `schedulesCounted` sent, the blank line says schedules "
              "were not counted - got %r" % old)
        old_none = line(reply(0))
        check("schedules not counted" not in old_none,
              "but an older add-in that found nothing blank is not qualified - "
              "there is nothing to qualify - got %r" % old_none)
    if caveat is not None:
        warned = caveat(reply(3))
        check(warned is not None and "3" in warned
              and "schedule" in warned and "older" in warned,
              "and the answer warns that the 3 may include sheets carrying "
              "only a schedule - got %r" % warned)
        check(caveat(reply(0)) is None,
              "and gives no warning when an older add-in found nothing blank")
        check(caveat(reply(3, False)) is not None,
              "`schedulesCounted: false` is treated as not counted")

    print("3. The add-in counts schedules (RevitExport.cs, read as text)")
    check("RevitSheets.ScheduleCountBySheet(doc)" in export,
          "it asks the Sheet Agent's own schedule count rather than keeping a copy")
    check("OfClass(typeof(ScheduleSheetInstance))" not in export,
          "and has no collector of its own to drift from that one")
    check(re.search(r"internal static Dictionary<ElementId, int> ScheduleCountBySheet\(",
                    sheets) is not None,
          "RevitSheets.ScheduleCountBySheet is reachable from another agent")
    check(re.search(r"IsRevisionSchedule\(instance\)\)\s*continue", sheets) is not None,
          "and still leaves the titleblock's revision schedule out")
    check(re.search(r"PlacedViewCount\(sheet\) == 0 && schedules == 0\) blankSheets\+\+",
                    export) is not None,
          "a sheet would print blank only with no view AND no schedule")
    check(re.search(r"PlacedViewCount\(sheet\) == 0\) blankSheets\+\+", export) is None,
          "and the views-only rule is gone")
    check('Json.Bool("schedulesCounted", true)' in export,
          "it tells the server that schedules were counted")

    print("4. The tool uses all of it")
    tool = body_of(server, "revit_export_check")
    check("_export_blank_line(reply)" in tool,
          "revit_export_check builds its blank line with _export_blank_line")
    check("_export_blank_caveat(reply)" in tool,
          "and its warning with _export_blank_caveat")
    check('"  would print blank' not in tool,
          "and carries no copy of the old unqualified line")

    print("")
    if FAILURES:
        print("%d check(s) FAILED." % len(FAILURES))
        return 1
    print("All checks passed. Built, not run in Revit - the live read is owed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
