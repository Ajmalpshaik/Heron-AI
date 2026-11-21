#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A nested family kept upright on a plane that is not level says so in its
read-back - row 5b-1883 part (2), issue #902. Runs without Revit.

    python tests/test_nested_upright.py

WHAT HAPPENED
-------------
On 2026-11-20, Revit 2020, arranging NEEDS-CHECKING Group JB, a Work
Plane-Based part was nested on the vertical reference plane `JB Front` and
PLACE_FAMILY_INSTANCES said *"1 x "Heron JB Part : Heron JB Part" nested on
"JB Front" at 0, -300, 0 mm - read back"*. The part lay FLAT: its family is
Always Vertical (the template's default), so Revit hosted it on the plane and
kept it level - its transform's Z still the model's Z, the plane facing
(0,1,0) - and a family kept level has no work plane to flip. The read-back read
as standing on the plane, and FLIP_ELEMENTS then refused with no reason given.

WHAT THIS HOLDS
---------------
The card's C# compares the placed instance's own Z with the normal of the plane
it sits on whenever that plane is not level, and adds a finding that names the
family as kept upright - with Always Vertical as the setting that does it and
SET_FAMILY_SETTINGS always vertical=no as the way to change it - so "nested on"
is never read as standing on the plane. The finding is made from what the
instance reads back, not assumed from the family's settings, and a level plane
asks nothing.

WHAT THIS DOES NOT PROVE: that Revit places it so on every release, or that the
sentence appears in a real run. That is NEEDS-CHECKING Group PP, on a real Revit.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARD = os.path.join(ROOT, "brain", "fragments", "place-family-instances")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def code(text):
    """C# with comments and string literals blanked."""
    text = re.sub(r"/\*.*?\*/", " ", text, flags=re.S)
    text = re.sub(r'@"(?:[^"]|"")*"', '""', text)
    text = re.sub(r'"(?:\\.|[^"\\\n])*"', '""', text)
    return re.sub(r"//[^\n]*", "", text)


def at(text, needle):
    return text.find(needle) if needle else -1


def main():
    source = read(os.path.join(CARD, "impl", "any", "fragment.cs"))
    cases = read(os.path.join(CARD, "tests", "cases.yaml"))
    yaml_text = read(os.path.join(CARD, "fragment.yaml"))
    bare = code(source)
    check(bool(source), "the card's C# is there")

    print("PLACE_FAMILY_INSTANCES - a nested family kept upright on a plane that is not level")
    read_back = at(bare, "readBack = nestedMade.Count")
    asked = re.search(r"if\s*\(\s*nestedKind\s*==\s*FamilyPlacementType\.WorkPlaneBased\s*&&\s*"
                      r"!hostLevelLike\s*\)", bare)
    check(asked is not None,
          "it is asked only of a Work Plane-Based family on a plane that is not level - a "
          "level plane asks nothing")
    check(re.search(r"GetTransform\(\)\s*\.\s*BasisZ\s*\.\s*DotProduct\(\s*hostPlane\.Normal\s*\)", bare)
          is not None,
          "it is judged from the instance's own Z against the plane's normal, as read back")
    said = re.search(r"if\s*\(\s*keptUpright\s*>\s*0\s*\)\s*findings\.Add\(", bare)
    check(said is not None and asked is not None and asked.start() < said.start()
          and read_back >= 0 and read_back < said.start(),
          "the finding follows the read-back line it qualifies, and only when one was kept upright")
    check("kept upright" in source and "Always Vertical" in source
          and "always vertical=no" in source,
          "the finding names the family as kept upright, Always Vertical as the setting, and "
          "SET_FAMILY_SETTINGS always vertical=no as the change")
    check("does not stand on" in source,
          "and says plainly that it does not stand on the plane, so 'nested on' is not read so")
    check("kept upright" in cases and "Always Vertical" in cases,
          "the card's cases hold an Always Vertical family nested on a vertical plane")
    check(re.search(r"^version:\s*8\s*$", yaml_text, re.M) is not None,
          "the card is version 8: its C# moved (row 5b-1883)")

    print()
    if FAILURES:
        print("FAILED - %d check(s)" % len(FAILURES))
        return 1
    print("PASSED - a nested family kept upright on a plane that is not level says so in its "
          "read-back.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
