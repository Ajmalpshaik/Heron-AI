#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
LIST_VIEWS_ON_SHEET reads the sheet it was asked for, and says what it read
in Revit's own numbers - held in its C#, read as text.

    python tests/test_list_views_on_sheet.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 146. `brain/fragments/list-views-on-sheet` was written on
2026-10-09 so that "what views are on this sheet" had a READ to go to. A
review the same day changed its C# six ways and ran every check that reads
fragment C# - the compile on 2024, `brain/heron_fragment.py`,
`tests/test_revit_gate.py`, `check-narrow-errors`, `check-structure` and
`tests/test_csharp.py`. Five of the six changes left all of them green:

  * a blank sheet number answered from the first sheet in the model
  * the schedule filter reversed, so a sheet listed every OTHER sheet's
    schedules
  * the scale written as "1 : 100" whatever the view's scale is
  * sheet positions divided by 304.8 instead of multiplied - feet read as
    if they were thousands of millimetres
  * the "no sheet with that number" refusal made silent

A second review, also the same day, made ten more changes that left all of
those checks AND the first version of this file green: the scale read off
the sheet instead of the view, the placeholder test reversed, the view type
taken from the sheet, a reflected ceiling plan called a floor plan, X and Y
swapped, a split schedule counted once per piece, text detail numbers sorted
before numbers, the sheet-template skip removed, the perspective test
reversed, and the typed number no longer trimmed. Each is a confident wrong
answer about a drawing set, and each compiles. This file holds every rule
the card promises, so that reversing or deleting any one of them reads FAIL.

WHAT IT HOLDS
-------------
  1. THE SHEET ASKED FOR, AND NO OTHER. The typed number is trimmed, once,
     and nothing reads it untrimmed; a blank one is refused before any sheet
     is looked at; a sheet view template is skipped; the only sheet ever read
     is the one whose whole number equals the one given, ignoring case; the
     sheet in front is never asked for; a number no sheet carries is said, by
     name; a PLACEHOLDER sheet is refused with a reason and a real sheet is
     read.
  2. A SHEET'S SCHEDULES ARE ITS OWN. A schedule instance is skipped unless
     its owner view IS this sheet; the title block's revision schedule is
     counted apart; the pieces of a split schedule are grouped by the
     schedule they show, and the count is one per schedule, never one per
     piece.
  3. THE SCALE IS THE VIEW'S. It is the View Scale parameter of the viewport's
     own view - never the sheet's - and no scale is written into the code. A
     perspective view says it has no scale, and is asked before the number is.
  4. THE TYPE IS THE VIEW'S. The words come from the viewport's own view, in
     the Project Browser's words: a reflected ceiling plan says Reflected
     Ceiling Plan, a legend says Legend, and anything unnamed is said as Revit
     names it.
  5. LENGTHS ARE MILLIMETRES, X THEN Y. Every 304.8 multiplies a length in
     Revit's feet, and a point is written X first, then Y (D-20).
  6. ONE ORDER FOR DETAIL NUMBERS. Numbers first, in number order (9 before
     10), then detail numbers that are text, in text order ignoring case.
  7. A READ. No transaction of any kind, and the card says READ.

WHAT IT CANNOT DO
-----------------
Run the fragment. Reading the code as text says which rule the code was
written to; whether Revit hands back what the code expects is a question for
a real model - the proof plan is
tools/jobs/list-views-on-sheet-project1-2026-10-09.yaml, and it is owed. Only
a Revit run can say: that VIEW_SCALE on a placed view is the number its
Properties show; that GetBoxCenter is where the viewport sits on the sheet;
that ScheduleSheetInstance.Point is where a schedule was placed; that
IsTitleblockRevisionSchedule and OwnerViewId pick out what this file assumes;
that the ViewSheet collector really does hand back a sheet template; that a
placeholder carries no viewport; and that the detail numbers on a real sheet
come back as the text the sort expects.

A rule written another way that still holds will read as a FAIL here, and
the answer then is to read the new code and change this test with it, in the
same change - never one without the other.
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
FOLDER = os.path.join(ROOT, "brain", "fragments", "list-views-on-sheet")
CS = os.path.join(FOLDER, "impl", "any", "fragment.cs")
CARD = os.path.join(FOLDER, "fragment.yaml")

# The view type in the Project Browser's words - the table the card promises.
# A value changed here is a decision about what a modeller reads, so it is
# changed in the C# and here together.
TYPE_WORDS = {
    "FloorPlan": "Floor Plan",
    "CeilingPlan": "Reflected Ceiling Plan",
    "EngineeringPlan": "Structural Plan",
    "AreaPlan": "Area Plan",
    "Elevation": "Elevation",
    "Section": "Section",
    "Detail": "Detail View",
    "ThreeD": "3D View",
    "DraftingView": "Drafting View",
    "Legend": "Legend",
    "Rendering": "Rendering",
}

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read().replace("\r\n", "\n")


def without_comments(text):
    """The C# with its comments taken out and its strings KEPT.

    One scanner, for the reason brain/heron_apichanges.unquoted gives: a `//`
    inside a string is not a comment, and a quote inside a comment opens
    nothing. Strings stay because several rules here are about what a
    string says - a scale written as text, a refusal that names the number,
    the words a view type is given.
    """
    out = []
    index, size = 0, len(text)
    while index < size:
        pair = text[index:index + 2]
        char = text[index]
        if pair == "//":
            while index < size and text[index] != "\n":
                index += 1
        elif pair == "/*":
            index += 2
            while index < size and text[index:index + 2] != "*/":
                if text[index] == "\n":
                    out.append("\n")
                index += 1
            index += 2
        elif char in "\"'":
            quote = char
            verbatim = index > 0 and text[index - 1] == "@"
            out.append(char)
            index += 1
            while index < size:
                here = text[index]
                if here == "\\" and not verbatim:
                    out.append(text[index:index + 2])
                    index += 2
                    continue
                out.append(here)
                index += 1
                if here == quote:
                    if verbatim and text[index:index + 1] == quote:
                        out.append(quote)
                        index += 1
                        continue
                    break
        else:
            out.append(char)
            index += 1
    return "".join(out)


def block_span(code, start):
    """(inner text, index of its `{`, index of its `}`) for the first block
    opening at or after `start`, or (None, -1, -1).

    Counted brace by brace, so a block holding blocks of its own is read
    whole. Strings were kept by `without_comments`; a brace inside one would
    miscount, and the fragment has none.
    """
    if start < 0:
        return None, -1, -1
    brace = code.find("{", start)
    if brace < 0:
        return None, -1, -1
    depth = 0
    for index in range(brace, len(code)):
        if code[index] == "{":
            depth += 1
        elif code[index] == "}":
            depth -= 1
            if depth == 0:
                return code[brace + 1:index], brace, index
    return None, -1, -1


def block_after(code, opening):
    """The text inside the braces that follow `opening`, or None."""
    return block_span(code, code.find(opening))[0]


def lambda_of(code, name):
    """(parameter names, body) of the lambda the C# assigns to `name`.

    `Func<View, string> scaleOf = view => { ... }` gives (["view"], the
    block); `Func<XYZ, string> millimetres = point => <expression>;` gives
    the expression. (None, None) when there is no such lambda.
    """
    found = re.search(r"\b(?:Func|Comparison)<[^=;]*>\s+%s\s*=\s*"
                      r"(\(\s*[\w\s,]*\)|\w+)\s*=>\s*" % re.escape(name), code)
    if not found:
        return None, None
    params = [p.strip() for p in found.group(1).strip("()").split(",") if p.strip()]
    rest = code[found.end():]
    if rest.startswith("{"):
        return params, block_span(rest, 0)[0]
    end = rest.find(";")
    return params, (rest[:end] if end >= 0 else None)


def squash(text):
    return re.sub(r"\s+", " ", text or "").strip()


def assigned(code, name):
    """Every right-hand side `name` is given with a plain `=`, squashed."""
    return [squash(value) for value in
            re.findall(r"\b%s\s*=(?![=>])\s*([^;]+);" % re.escape(name), code)]


def calls(code, name):
    """The argument text of every call to `name(...)` with no nested call."""
    return [squash(arg) for arg in re.findall(r"\b%s\(([^()]*)\)" % re.escape(name), code)]


def main():
    print("0. The fragment is there")
    present = os.path.isfile(CS) and os.path.isfile(CARD)
    check(present, "brain/fragments/list-views-on-sheet has its card and its C#")
    if not present:
        print("")
        print("%d check(s) FAILED. Nothing else can be read without the fragment."
              % len(FAILURES))
        return 1

    code = without_comments(read(CS))
    card = read(CARD)

    # ------------------------------------------------------------------
    print("1. The sheet asked for, and no other")
    wanted = assigned(code, "wantedNumber")
    check(wanted == ['(sheetNumber ?? "").Trim()'],
          "the typed sheet number is trimmed, once - got %s" % (wanted or "nothing"))
    typed = len(re.findall(r"\bsheetNumber\b", code))
    check(typed == 1,
          "and nothing reads the untrimmed number - sheetNumber appears %d time(s)" % typed)

    blank = re.search(
        r"if\s*\(\s*(?:wantedNumber\.Length\s*==\s*0"
        r"|string\.IsNullOr(?:White[Ss]pace|Empty)\(\s*wantedNumber\s*\))\s*\)\s*\{",
        code)
    first_collector = code.find("FilteredElementCollector")
    check(blank is not None and 0 <= blank.start() < first_collector,
          "a blank sheet number is tested for BEFORE any sheet is collected")
    refusal = block_after(code, blank.group(0)) if blank else None
    check(refusal is not None
          and re.search(r'sheetProblem\s*=\s*"[^"]+"', refusal) is not None
          and "FilteredElementCollector" not in refusal
          and "GetElement" not in refusal,
          "and is refused there, with a reason in sheetProblem and no sheet read")

    sheets = re.search(r"foreach\s*\(\s*var\s+(\w+)\s+in\s+new\s+FilteredElementCollector"
                       r"\(\s*doc\s*\)\s*\.OfClass\(\s*typeof\(\s*ViewSheet\s*\)\s*\)", code)
    loop = block_span(code, sheets.end())[0] if sheets else None
    skip = re.search(r"if\s*\(([^{};]*)\)\s*continue\s*;", loop or "")
    chosen_at = (loop or "").find("target = ")
    check(sheets is not None and skip is not None
          and squash(skip.group(1)) == "%s == null || %s.IsTemplate"
          % (sheets.group(1), sheets.group(1))
          and 0 <= skip.start() < chosen_at,
          "a sheet view template is skipped before any sheet is chosen - never read "
          "as the sheet - got %r" % (squash(skip.group(1)) if skip else None))

    targets = assigned(code, "target")
    check(squash(" | ".join(targets)) == "null | candidate",
          "`target` is only ever null or the matching candidate - got %s"
          % (targets or "nothing"))
    chosen = re.search(r"if\s*\(([^{};]*)\)\s*\{\s*target\s*=\s*candidate\s*;", code)
    check(chosen is not None
          and squash(chosen.group(1)) == "string.Equals(candidate.SheetNumber, "
                                         "wantedNumber, StringComparison.OrdinalIgnoreCase)",
          "a sheet is chosen ONLY by its whole number equalling the one given, "
          "ignoring case - got %r" % (squash(chosen.group(1)) if chosen else None))
    check(re.search(r"\bActive(?:View|UIDocument)\b", code) is None,
          "the sheet in front is never asked for")

    missing = block_after(code, "if (target == null)")
    check(missing is not None
          and re.search(r'sheetProblem\s*=\s*"[^"]+"\s*\+\s*wantedNumber\b', missing) is not None,
          "a number no sheet carries is said in sheetProblem, naming the number")

    placeholder = list(re.finditer(r"else\s+if\s*\(([^{};]*IsPlaceholder[^{};]*)\)\s*\{", code))
    check(len(placeholder) == 1 and squash(placeholder[0].group(1)) == "target.IsPlaceholder"
          and len(re.findall(r"\bIsPlaceholder\b", code)) == 1,
          "the one placeholder test is `target.IsPlaceholder`, not reversed - got %s"
          % [squash(m.group(1)) for m in placeholder])
    refused, _, refused_end = (block_span(code, placeholder[0].start())
                               if placeholder else (None, -1, -1))
    check(refused is not None
          and re.search(r'sheetProblem\s*=(?!=)[^;]*"[^"]*placeholder[^"]*"[^;]*;', refused)
          is not None
          and "GetAllViewports" not in refused
          and "FilteredElementCollector" not in refused,
          "a placeholder sheet is refused, with a reason in sheetProblem, and nothing "
          "on it is read")
    after = code[refused_end + 1:] if refused_end >= 0 else ""
    otherwise = (block_span(after, 0)[0]
                 if re.match(r"\s*else\s*\{", after) else None)
    check(otherwise is not None and "target.GetAllViewports()" in otherwise
          and "OfClass(typeof(ScheduleSheetInstance))" in otherwise
          and len(re.findall(r"\bGetAllViewports\b", code)) == 1,
          "a sheet that is NOT a placeholder - the else after it - is the one read")

    # ------------------------------------------------------------------
    print("2. A sheet's schedules are its own")
    check("OfClass(typeof(ScheduleSheetInstance))" in code,
          "schedule instances are collected - GetAllViewports never lists them")
    skips = [squash(m.group(1)) for m in
             re.finditer(r"if\s*\(([^{};]*OwnerViewId[^{};]*)\)\s*continue\s*;", code)]
    check(len(skips) == 1 and "OwnerViewId != target.Id" in skips[0]
          and "==" not in skips[0].replace("== null", ""),
          "an instance is skipped unless its owner view IS this sheet - got %s" % skips)
    revision = block_after(code, "if (instance.IsTitleblockRevisionSchedule)")
    check(revision is not None and "revisionSchedules++" in revision
          and re.search(r"\bcontinue\s*;", revision) is not None,
          "the title block's revision schedule is counted apart and never listed")
    check(re.search(r"\bscheduleId\s*=\s*instance\.ScheduleId\s*;", code) is not None
          and re.search(r"\bvar\s+pieces\s*=\s*new\s+Dictionary<\s*ElementId\s*,\s*int\s*>"
                        r"\(\)\s*;", code) is not None
          and re.findall(r"\bpieces\[(\w+)\]\s*\+\+", code) == ["scheduleId"],
          "pieces of one split schedule are counted under the schedule they show")
    per_schedule = block_after(code, "foreach (var scheduleId in pieces.Keys)")
    check(per_schedule is not None and "scheduleRows.Add(" in per_schedule
          and code.count("scheduleRows.Add(") == 1,
          "one row per SCHEDULE, written in the loop over the schedules")
    counted = assigned(code, "scheduleCount")
    check(counted == ["0", "scheduleRows.Count"]
          and re.search(r"\bscheduleCount\s*(?:\+\+|\+=|-=)", code) is None,
          "scheduleCount is the number of schedules, so a split schedule counts once "
          "- got %s" % counted)
    check(re.search(r'pieces\[scheduleId\]\s*>\s*1\s*\?\s*"[^"]*split into\s*"\s*\+\s*'
                    r'pieces\[scheduleId\]', code) is not None,
          "a split schedule says how many pieces")

    # ------------------------------------------------------------------
    print("3. The scale is the view's")
    scale_params, scale_body = lambda_of(code, "scaleOf")
    check(scale_params is not None and len(scale_params) == 1 and scale_body is not None,
          "the scale is worked out in one place, `scaleOf`, from the view handed to it")
    own = scale_params[0] if scale_params else None
    read_off = re.findall(r"(\w+)\.get_Parameter\(\s*BuiltInParameter\.VIEW_SCALE\s*\)", code)
    check(own is not None and read_off == [own]
          and len(re.findall(r"\bVIEW_SCALE\b", code)) == 1
          and "VIEW_SCALE" in (scale_body or ""),
          "the View Scale parameter is read off THAT view, never the sheet - read off %s"
          % (read_off or "nothing"))
    given = calls(code, "scaleOf")
    check(given == ["view"],
          "scaleOf is handed the viewport's view and nothing else - got %s" % given)
    view_is = assigned(code, "view")
    port_is = assigned(code, "port")
    check(view_is == ["doc.GetElement(port.ViewId) as View"]
          and port_is == ["doc.GetElement(portId) as Viewport"]
          and re.search(r"foreach\s*\(\s*var\s+portId\s+in\s+target\.GetAllViewports\(\)\s*\)",
                        code) is not None,
          "and `view` is the view each of this sheet's viewports shows - got %s, %s"
          % (view_is, port_is))
    holder = re.search(r"\bvar\s+(\w+)\s*=\s*%s\.get_Parameter\(\s*BuiltInParameter\.VIEW_SCALE"
                       % re.escape(own or "?"), scale_body or "")
    built = re.findall(r'"1 : "\s*\+\s*(\w+)\.AsInteger\(\)', code)
    check(holder is not None and built == [holder.group(1)],
          'the "1 : n" text is built from that parameter\'s value - got %s' % built)
    written = re.findall(r'"[^"\n]*\b1\s*:\s*\d[^"\n]*"', code)
    check(not written,
          "no scale is written into the code - got %s" % (written or "none"))
    as_3d = re.search(r"\bvar\s+(\w+)\s*=\s*%s\s+as\s+View3D\s*;" % re.escape(own or "?"),
                      scale_body or "")
    perspective = re.search(r'if\s*\(([^{};]*IsPerspective[^{};]*)\)\s*return\s*"([^"]*)"\s*;',
                            scale_body or "")
    check(as_3d is not None and perspective is not None
          and squash(perspective.group(1)) == "%s != null && %s.IsPerspective"
          % (as_3d.group(1), as_3d.group(1))
          and "no scale" in perspective.group(2)
          and len(re.findall(r"\bIsPerspective\b", code)) == 1
          and perspective.start() < (scale_body or "").find("VIEW_SCALE"),
          "a PERSPECTIVE view says it has no scale, asked before any number is read - got %r"
          % (squash(perspective.group(1)) if perspective else None))

    # ------------------------------------------------------------------
    print("4. The type is the view's")
    type_params, type_body = lambda_of(code, "typeWords")
    switch = re.search(r"switch\s*\(\s*(\w+)\.ViewType\s*\)", type_body or "")
    check(type_params is not None and len(type_params) == 1 and switch is not None
          and switch.group(1) == type_params[0],
          "the words are chosen by the ViewType of the view handed to typeWords")
    given = calls(code, "typeWords")
    check(given == ["view"],
          "typeWords is handed the viewport's view, never the sheet - got %s" % given)
    cases = re.findall(r'case\s+ViewType\.(\w+)\s*:\s*return\s+"([^"]*)"\s*;', type_body or "")
    table = dict(cases)
    check(table.get("CeilingPlan") == "Reflected Ceiling Plan",
          "a reflected ceiling plan is called one - got %r" % table.get("CeilingPlan"))
    check(table.get("Legend") == "Legend",
          "a legend says Legend - got %r" % table.get("Legend"))
    check(len(cases) == len(table) and table == TYPE_WORDS,
          "every view type is said in the Project Browser's words - differs at %s"
          % sorted(set(table.items()) ^ set(TYPE_WORDS.items())))
    fallback = re.search(r"default\s*:\s*return\s+(\w+)\.ViewType\.ToString\(\)\s*;",
                         type_body or "")
    check(fallback is not None and type_params and fallback.group(1) == type_params[0],
          "a type not in the table is said as Revit names it, never dropped")

    # ------------------------------------------------------------------
    print("5. Lengths are millimetres, X then Y")
    every = len(re.findall(r"304\.8", code))
    multiplied = len(re.findall(r"\*\s*304\.8\b", code))
    check(every > 0 and every == multiplied,
          "every 304.8 multiplies a length in feet - %d of %d" % (multiplied, every))
    check(re.search(r"/\s*(?:304\.8|0\.3048)\b", code) is None
          and re.search(r"\*\s*0\.3048\b", code) is None,
          "nothing divides by 304.8 or turns feet the other way")
    point_params, point_body = lambda_of(code, "millimetres")
    point = point_params[0] if point_params else "?"
    xs = [m.start() for m in re.finditer(r"Math\.Round\(\s*%s\.X\s*\*\s*304\.8\s*\)"
                                         % re.escape(point), point_body or "")]
    ys = [m.start() for m in re.finditer(r"Math\.Round\(\s*%s\.Y\s*\*\s*304\.8\s*\)"
                                         % re.escape(point), point_body or "")]
    between = (point_body or "")[xs[0]:ys[0]] if len(xs) == 1 and len(ys) == 1 else ""
    check(len(xs) == 1 and len(ys) == 1 and xs[0] < ys[0] and '", "' in between
          and re.search(r"\.Z\b", point_body or "") is None,
          "a point is written X first, then Y, each in millimetres - X at %s, Y at %s"
          % (xs, ys))
    where = sorted(squash(arg) for arg in
                   re.findall(r'\bmillimetres\((.*?)\)\s*\+\s*"\) mm"', code))
    check(where == ["point", "port.GetBoxCenter()"]
          and len(re.findall(r"\bmillimetres\(", code)) == 2
          and re.search(r'"\s*centre \("\s*\+\s*millimetres\(port\.GetBoxCenter\(\)\)', code)
          is not None,
          "the viewport's centre and the schedule's placement point are the two things "
          "written in millimetres - got %s" % where)

    # ------------------------------------------------------------------
    print("6. One order for detail numbers")
    sort_params, sort_body = lambda_of(code, "byDetail")
    first, second = (sort_params + ["?", "?"])[:2] if sort_params else ("?", "?")
    check(sort_params is not None and len(sort_params) == 2
          and re.search(r"\bvar\s+leftIsNumber\s*=\s*int\.TryParse\(\s*%s\.Key\s*,\s*out\s+left"
                        r"\s*\)\s*;" % re.escape(first), sort_body or "") is not None
          and re.search(r"\bvar\s+rightIsNumber\s*=\s*int\.TryParse\(\s*%s\.Key\s*,\s*out\s+"
                        r"right\s*\)\s*;" % re.escape(second), sort_body or "") is not None,
          "each detail number is asked whether it is a whole number, left for left")
    steps = [(squash(condition), squash(result)) for condition, result in
             re.findall(r"(?:if\s*\(([^{};]*)\)\s*)?return\s+([^;]+);", sort_body or "")]
    check(("leftIsNumber && rightIsNumber", "left.CompareTo(right)") in steps[:1],
          "two numbers are put in number order, so 9 comes before 10 - got %s"
          % (steps[:1] or "nothing"))
    check(("leftIsNumber != rightIsNumber", "leftIsNumber ? -1 : 1") in steps[1:2],
          "a number comes before a detail number that is text - got %s"
          % (steps[1:2] or "nothing"))
    check(steps[2:] == [("", "string.Compare(%s.Key, %s.Key, StringComparison.OrdinalIgnoreCase)"
                         % (first, second))],
          "two text detail numbers are put in text order, ignoring case - got %s"
          % (steps[2:] or "nothing"))
    keyed = re.search(r"rows\.Add\(\s*new\s+KeyValuePair<\s*string\s*,\s*string\s*>\(\s*detail\s*,",
                      code)
    sorted_at = code.find("rows.Sort(byDetail);")
    joined_at = code.find("viewsOnSheet = string.Join(")
    check(keyed is not None and code.count("rows.Sort(") == 1
          and keyed.start() < sorted_at < joined_at,
          "the rows are keyed by detail number and sorted that way before they are written")

    # ------------------------------------------------------------------
    print("7. A read")
    tokens = re.findall(r"\b(?:Sub)?Transaction(?:Group)?\b", code)
    check(not tokens, "no transaction of any kind - got %s" % (tokens or "none"))
    risk = re.search(r"^risk:\s*(\S+)", card, re.M)
    check(risk is not None and risk.group(1) == "READ",
          "the card says risk READ - got %s" % (risk.group(1) if risk else None))

    print("")
    if FAILURES:
        print("%d check(s) FAILED." % len(FAILURES))
        return 1
    print("All checks passed. Read as text, not run in Revit - the proof is owed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
