# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   4
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A sheet carrying a schedule is not a sheet with nothing on it.

    python tests/test_sheet_contents.py

WHAT WENT WRONG
---------------
On 2026-09-27, in "heron ai bulding" (Revit 2024, session 46596), three
schedules were placed on sheets M-105, M-106 and M-107 and read back at
25 mm, 570 mm. In the same minute `revit_sheets` called all three
"0 view(s) <-- NOTHING ON IT" and warned "3 sheet(s) have nothing placed on
them", while `revit_schedules` showed all three schedules as placed.

The add-in counted what is on a sheet with `ViewSheet.GetAllPlacedViews`,
which answers with viewports only. A schedule goes on a sheet as a
ScheduleSheetInstance and is never in that list, so a sheet carrying only a
schedule read as empty - a false alarm on exactly the check the tool exists
for. FRAGMENT-ISSUES 5b-238.

WHAT THIS HOLDS
---------------
  1. THE LINE A MODELLER READS. `_sheet_line` is lifted out of the server and
     RUN, the way tests/test_read_door.py runs its helper: a sheet with a
     schedule and no view is not NOTHING ON IT and says "1 schedule(s)"; a
     sheet with neither is; a view sheet still reads "1 view(s)"; a
     placeholder is still a placeholder.
  2. AN OLDER ADD-IN IS NOT BELIEVED. This server and the add-in go live at
     different restarts. An add-in that sends no schedule count cannot say a
     sheet is empty, only that it has no views, and the finding says so.
  3. THE ADD-IN COUNTS SCHEDULES, READ AS TEXT. RevitSheets.cs collects
     ScheduleSheetInstance, leaves the titleblock's revision schedule out,
     sends `schedules` on every row, and calls a sheet empty only when both
     counts are zero.
  4. THE TOOL USES ALL OF IT. revit_sheets builds its lines and its finding
     from these helpers and no longer carries a copy of the old rule.

WHAT IT CANNOT DO
-----------------
Run the add-in. Whether Revit's collector finds each schedule and whether the
revision schedule is really excluded is a question for a real model - see
docs/needs-checking for the run owed on "heron ai bulding".
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


def sheet(number, name, views, schedules="absent", placeholder=False,
          titleblock="A1 metric"):
    row = {"number": number, "name": name, "views": views,
           "titleblock": titleblock, "revisions": 0, "placeholder": placeholder}
    if schedules != "absent":
        row["schedules"] = schedules
    return row


def main():
    server = read(SERVER)
    cs = read(SHEETS_CS)

    line = lifted(server, "_sheet_line")
    finding = lifted(server, "_empty_sheets_finding")
    counted = lifted(server, "_schedules_counted")

    print("1. The line a modeller reads")
    check(line is not None, "_sheet_line exists in the server and runs alone")
    if line is not None:
        schedule_only = line(sheet("M-105", "Schedule - Spaces", 0, 1))
        check("NOTHING ON IT" not in schedule_only,
              "a sheet carrying one schedule and no view is NOT 'nothing on it'"
              " - got %r" % schedule_only)
        check("0 view(s), 1 schedule(s)" in schedule_only,
              "and it says 0 view(s), 1 schedule(s) - got %r" % schedule_only)

        empty = line(sheet("M-199", "Spare", 0, 0))
        check("NOTHING ON IT" in empty,
              "a sheet with no view and no schedule IS 'nothing on it' - got %r"
              % empty)

        view_sheet = line(sheet("M-101", "Level 01 - HVAC", 1, 0))
        check("1 view(s), 0 schedule(s)" in view_sheet
              and "NOTHING" not in view_sheet,
              "a sheet with one view still reads 1 view(s) - got %r" % view_sheet)

        both = line(sheet("M-110", "Plan and schedule", 1, 2))
        check("1 view(s), 2 schedule(s)" in both,
              "views and schedules are counted apart - got %r" % both)

        spare = line(sheet("M-900", "Reserved", 0, 0, placeholder=True))
        check("placeholder" in spare and "NOTHING" not in spare,
              "a placeholder is still a placeholder, never empty - got %r" % spare)

        bare = line(sheet("M-120", "No border", 0, 1, titleblock=None))
        check("no titleblock" in bare,
              "a schedule sheet with no titleblock is still flagged for it - got %r"
              % bare)

    print("2. An older add-in is not believed")
    if line is not None:
        old = line(sheet("M-105", "Schedule - Spaces", 0))
        check("NOTHING ON IT" not in old,
              "with no schedule count sent, a viewless sheet is NOT called empty"
              " - got %r" % old)
        check("schedules not counted" in old,
              "and the line says schedules were not counted - got %r" % old)
        old_view = line(sheet("M-101", "Level 01 - HVAC", 1))
        check(old_view.rstrip().endswith("1 view(s)"),
              "and a view sheet shows no invented schedule count - got %r"
              % old_view)
    check(counted is not None, "_schedules_counted exists and runs alone")
    if counted is not None:
        check(counted([sheet("M-105", "s", 0, 1)]) is True,
              "a reply carrying `schedules` counted them")
        check(counted([sheet("M-105", "s", 0)]) is False,
              "a reply without `schedules` did not")
    check(finding is not None, "_empty_sheets_finding exists and runs alone")
    if finding is not None:
        new = finding(2, True)
        check("2 sheet(s)" in new and "no view and no schedule" in new,
              "the finding says what 'nothing' means - got %r" % new)
        stale = finding(3, False)
        check("NOT" in stale and "schedule" in stale
              and "nothing placed" not in stale,
              "from an older add-in it says schedules were not counted and "
              "never 'nothing placed' - got %r" % stale)

    print("3. The add-in counts schedules (RevitSheets.cs, read as text)")
    check("OfClass(typeof(ScheduleSheetInstance))" in cs,
          "it collects ScheduleSheetInstance, which GetAllPlacedViews never lists")
    check("IsTitleblockRevisionSchedule" in cs,
          "it asks whether each one is the titleblock's revision schedule")
    check(re.search(r"IsRevisionSchedule\(instance\)\)\s*continue", cs) is not None,
          "and leaves that one out of the count")
    guard = re.search(r"IsTitleblockRevisionSchedule;\s*\}\s*catch[^{]*\{\s*return (\w+);",
                      cs)
    check(guard is not None and guard.group(1) == "true",
          "when Revit will not say, it is NOT counted - a sheet wrongly called "
          "full is the blank drawing that gets issued")
    check("OwnerViewId" in cs, "each schedule is credited to the sheet that owns it")
    check('Json.Num("schedules", schedules)' in cs,
          "every sheet row sends its schedule count")
    check(re.search(r"views == 0 && schedules == 0\) emptySheets\+\+", cs) is not None,
          "a sheet is counted empty only with no view AND no schedule")

    print("4. The tool uses all of it")
    tool = body_of(server, "revit_sheets")
    check("_sheet_line(sheet)" in tool, "revit_sheets builds each line with _sheet_line")
    check("_empty_sheets_finding(" in tool and "_schedules_counted(" in tool,
          "and its finding with _empty_sheets_finding, told whether schedules counted")
    check("NOTHING ON IT" not in tool,
          "and carries no copy of the old views-only rule")

    print("")
    if FAILURES:
        print("%d check(s) FAILED." % len(FAILURES))
        return 1
    print("All checks passed. Built, not run in Revit - the live read is owed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
