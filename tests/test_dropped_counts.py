# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
D-64's dropped counts, held on the real cards that carry them.

    python tests/test_dropped_counts.py

WHY IT EXISTS - WORK-NOTE ITEM WN-7 OF ISSUE #1005, ROW 5b-1909. D-64: a
reading card that goes looking and can drop a candidate declares how many it
dropped, as a provide, and says it only on the empty answer.
`tools/check-revit-gate.py` question 14 is the worklist, and it is a REPORT -
it exits 0 whatever it finds, so a card that loses its count goes back on the
list and nothing turns red. This file is what turns red.

WHAT IT PROVES, for every card in CARRIED below:
  1. The gate's question 14 answers ANSWERED on the real card - so the card
     is off D-64's worklist, and a change that loses its count puts it back
     on and fails here.
  2. The count is a provide the contract declares `role: accounting` - what
     the card was handed and could not use, never a thing it found.
  3. The card's C# does more than declare the name: it is counted (`++`,
     `+=`), added to, or set from a test, somewhere after the line that makes
     it. A provide declared and never moved would satisfy the gate and count
     nothing.

WHAT IT DOES NOT PROVE. That the count is right on a model. Nothing static
can - each card's NEEDS-CHECKING group runs it in Revit.
"""

import os
import re
import sys
import importlib.util

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
FAILURES = []

# slug -> the provide that carries its dropped count. Batch 1 of WN-7,
# 2026-11-24 (NEEDS-CHECKING Group QH). REPORT_WALL_ATTACHMENTS' `unread` was
# already in its C#; the gate learned the word that day.
CARRIED = {
    "report-design-options": "unresolvedElements",
    "filter-elements-by-type": "unresolvedType",
    "report-masses": "skippedMasses",
    "select-by-insulation": "unresolvedCoverings",
    "select-by-electrical-circuit": "unresolvedCircuits",
    "report-mep-pressure-drop": "skippedSections",
    "report-panel-circuits": "unresolvedCircuits",
    "report-area-volume-computations": "unresolvedAreas",
    "report-sprinkler-network": "skippedConnectors",
    "report-level-elevations": "unreadable",
    "report-wall-attachments": "unread",
    # Batch 2, 2026-11-24 (NEEDS-CHECKING Group QO). ORDER_ELEMENTS_BY_POSITION
    # and LIST_SHEET_SETS only declared what their C# already counted.
    "report-constraints": "unresolvedConstraints",
    "find-degenerate-lines": "skippedCurves",
    "check-structural-supports": "skippedMembers",
    "select-by-host": "unresolvedElements",
    "select-by-pin-state": "unresolvedElements",
    "report-tags-and-targets": "unresolvedTags",
    "report-browser-organization": "unresolvedItems",
    "order-elements-by-position": "noPlace",
    "list-sheet-sets": "unread",
}


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_gate():
    path = os.path.join(ROOT, "tools", "check-revit-gate.py")
    spec = importlib.util.spec_from_file_location("check_revit_gate", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def moved(code, name):
    """Whether the C# does something to `name` after the line declaring it."""
    body = "\n".join(line for line in code.splitlines()
                     if not line.strip().startswith("//"))
    declared = re.search(r"\bvar\s+%s\s*=([^;]*);" % re.escape(name), body)
    if not declared:
        return False
    if "?" in declared.group(1):
        return True                  # set from a test where it is made
    after = body[declared.end():]
    return bool(re.search(
        r"\b%s\s*(\+\+|\+=|\.Add\(|\.AddRange\()" % re.escape(name), after))


def main():
    gate = load_gate()
    loaded, _problems = gate.library()
    entries = dict((entry[1], entry) for entry in loaded)
    for slug, name in sorted(CARRIED.items()):
        print(slug)
        entry = entries.get(slug)
        check(entry is not None, "%s is a fragment the gate reads" % slug)
        if entry is None:
            continue
        verdict, detail = gate.ask(*entry)[13]
        check(verdict == gate.ANSWERED,
              "%s: question 14 is ANSWERED, so it is off D-64's worklist (%s)"
              % (slug, verdict))
        provides = (entry[2].get("contract") or {}).get("provides") or []
        declared = [p for p in provides if p.get("name") == name]
        check(len(declared) == 1 and declared[0].get("role") == "accounting",
              "%s declares `%s` as accounting" % (slug, name))
        check(moved(entry[4] or "", name),
              "%s's C# counts `%s`, not only declares it" % (slug, name))

    if FAILURES:
        print("\nFAILED - %d check(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  " + failure)
        return 1
    print("\nPASSED - %d card(s) carry the dropped count D-64 asks for. A "
          "static check: it says the count is there and moved, not that it "
          "is right on a model." % len(CARRIED))
    return 0


if __name__ == "__main__":
    sys.exit(main())
