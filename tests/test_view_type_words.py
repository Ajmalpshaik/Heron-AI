#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A view named "Floor Plan: Level 3", as the Project Browser writes it, reaches
that floor plan. Runs without Revit.

    python tests/test_view_type_words.py

WHY THIS EXISTS
---------------
FRAGMENT-ISSUES row 5b-1468, issue #799 item 6. On Villa 1, Revit 2024,
APPLY_VIEW_TEMPLATE templated 1 of 23 views: one plan name was a floor plan, a
ceiling plan and a structural plan. The add-in's `OneView`
(revit/Heron.Revit.Addin/RevitFragment.cs) already took "FloorPlan: Level 3" -
but only in the enum's spelling. Measured 2026-11-07 on Test for Issue, Revit
2024, session 41844: "FloorPlan: L1 - Architectural" resolved, "Floor Plan: L1
- Architectural" answered "No view called", and a structural plan could be
named only as "EngineeringPlan", a word no modeller uses.

WHAT IT CHECKS, IN THE ONE PLACE A COMPILER CANNOT
--------------------------------------------------
`OneView` and `TypeWordNames` touch Autodesk types, so no test host here can
link them (test_binding_note.py gives the reason). This reads the source as
text, the technique test_category_by_name.py uses, and then RUNS the word
matching as written - the alias table is read out of the C# `switch`, so a
case deleted there fails here:

  1. `OneView` narrows a shared name through `TypeWordNames`, not through a
     comparison with the enum's own spelling;
  2. spaces and capitals are dropped and a plural is read as one;
  3. the Project Browser's words reach their view types - Floor Plan, Ceiling
     Plans, Structural Plan, 3D View, Drafting View, Sheet - and the enum's
     own spelling still does;
  4. and a word names only its own type: "Section" is not a floor plan.

WHAT IT CANNOT DO: say that Revit resolves the view. That run is on row 5b-1468.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SOURCE = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitFragment.cs")

FAILED = []


def check(ok, said):
    print("  %s  %s" % ("ok  " if ok else "FAIL", said))
    if not ok:
        FAILED.append(said)


def body_of(text, signature):
    """One method, signature to closing brace; None when it is not there."""
    start = text.find(signature)
    if start < 0:
        return None
    opened = text.find("{", start)
    if opened < 0:
        return None
    depth = 0
    for at in range(opened, len(text)):
        if text[at] == "{":
            depth += 1
        elif text[at] == "}":
            depth -= 1
            if depth == 0:
                return text[start:at + 1]
    return None


def code_only(text):
    """The method without its comments, so a sentence cannot satisfy a check."""
    text = re.sub(r"//[^\n]*", "", text)
    return re.sub(r"/\*.*?\*/", "", text, flags=re.S)


def aliases(body):
    """The C# switch as {word: enum member}: each run of `case "x":` labels
    ends at the `ViewType.Member` its block tests."""
    table = {}
    pending = []
    for token in re.finditer(r'case\s+"([a-z0-9]+)"\s*:|ViewType\.(\w+)', body):
        if token.group(1):
            pending.append(token.group(1))
        elif pending:
            for word in pending:
                table[word] = token.group(2)
            pending = []
    return table


def names(table, member, word):
    """TypeWordNames as written: letters and digits only, lower case, then the
    word and the word less a final s, each against the enum's spelling and the
    switch."""
    said = "".join(ch for ch in word if ch.isalnum()).lower()
    if not said:
        return False
    for form in (said, said[:-1] if said.endswith("s") else None):
        if not form:
            continue
        if form == member.lower() or table.get(form) == member:
            return True
    return False


def main():
    print("A view named by the word the Project Browser uses (5b-1468)")
    try:
        text = io.open(SOURCE, encoding="utf-8").read()
    except (IOError, OSError) as exc:
        print("COULD NOT RUN - %s" % exc)
        return 3

    one_view = body_of(text, "private static object OneView(")
    words = body_of(text, "private static bool TypeWordNames(")
    check(one_view is not None, "OneView is in RevitFragment.cs")
    check(words is not None, "TypeWordNames is in RevitFragment.cs")
    one_code = code_only(one_view or "")
    check("TypeWordNames(candidate.ViewType, kind)" in one_code
          and "candidate.ViewType.ToString(), kind" not in one_code,
          "OneView narrows a shared name through TypeWordNames, not the enum's spelling alone")

    code = code_only(words or "")
    check("char.IsLetterOrDigit" in code and "ToLowerInvariant" in code,
          "spaces and capitals are dropped before the word is compared")
    check('EndsWith("s")' in code,
          "and a plural is read as one - the browser's headings are plural")

    table = aliases(code)
    cases = [
        ("FloorPlan", "Floor Plan", True),
        ("FloorPlan", "floor plans", True),
        ("FloorPlan", "FloorPlan", True),
        ("CeilingPlan", "Ceiling Plans", True),
        ("CeilingPlan", "Reflected Ceiling Plan", True),
        ("EngineeringPlan", "Structural Plan", True),
        ("EngineeringPlan", "EngineeringPlan", True),
        ("ThreeD", "3D View", True),
        ("ThreeD", "3D Views", True),
        ("Section", "Sections", True),
        ("Elevation", "Elevation", True),
        ("DraftingView", "Drafting View", True),
        ("Legend", "Legends", True),
        ("DrawingSheet", "Sheet", True),
        ("FloorPlan", "Section", False),
        ("CeilingPlan", "Floor Plan", False),
        ("EngineeringPlan", "Floor Plan", False),
        ("FloorPlan", "", False),
    ]
    for member, word, expected in cases:
        got = names(table, member, word)
        check(got == expected,
              "%r %s %s" % (word, "names" if expected else "does not name", member))

    print()
    if FAILED:
        print("FAILED - %d check(s)" % len(FAILED))
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
