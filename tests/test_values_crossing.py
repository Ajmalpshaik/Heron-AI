#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What a caller types into `values` reaching the add-in whole. Runs without Revit.

A NEWLINE SEPARATES VALUES AND A SEMICOLON DOES NOT. `_values_array` used to
turn every semicolon into a line break before splitting, which is fine until a
value's own text carries one - and `overrides` always does.

OneOverride in revit/Heron.Revit.Addin/RevitFragment.cs states the separator it
expects: "SEMICOLONS BETWEEN SETTINGS, because a colour is already three
comma-separated numbers and a comma cannot do both jobs". So the two sides
disagreed about what a semicolon meant, and the Python side won - silently.

    overrides=cut-line-colour=255,0,0; projection-line-colour=255,0,0

arrived as `overrides` carrying ONLY the first setting, plus a second value
named `projection-line-colour` matching no declared need, dropped without a
word. The caller was told the whole thing had been applied.

MEASURED AGAINST A REAL MODEL on 2026-09-22: four writes asked Project1's walls
to go red in a floor plan and the walls stayed white every time, because the one
setting that survived was whichever was written first - and in a plan that was
the PROJECTION colour, which a cut wall never draws. The count came back
"overridden 1" each time, which is what made it invisible.

WHAT IT CANNOT DO: it proves the string crosses whole, not that Revit applies
it. That needs a model - see the fragment-proving skill and D-30.

    python tests/test_values_crossing.py
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

try:
    import heron_mcp_server as server
except BaseException as exc:                                 # noqa: BLE001
    # BaseException rather than ImportError on purpose: an MCP SDK that is
    # importable but panicking raises out of pyo3, which ImportError misses.
    print("COULD_NOT_RUN - the MCP server module would not import here.")
    print("  %s: %s" % (type(exc).__name__, exc))
    print("  pip install --user mcp  (and --upgrade cryptography cffi if it panics)")
    sys.exit(3)

FAILURES = []


def check(condition, what):
    if condition:
        print("  ok    %s" % what)
    else:
        print("  FAIL  %s" % what)
        FAILURES.append(what)


def main():
    print(__doc__.strip().splitlines()[0])
    print()

    # ASK BEFORE CALLING, so a renamed helper is one clean failure rather than
    # an AttributeError that replaces every check below it with a traceback.
    pack = getattr(server, "_values_array", None)
    check(pack is not None, "the server still has a helper that packs `values`")
    if pack is None:
        print()
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("a value whose own text carries semicolons")
    out = pack("overrides=cut-line-colour=255,0,0; projection-line-colour=255,0,0")
    check(len(out) == 1,
          "stays ONE value, not %d - a semicolon does not start a new one" % len(out))
    if out:
        check(out[0].get("name") == "overrides",
              "is still named `overrides`")
        check(out[0].get("value")
              == "cut-line-colour=255,0,0; projection-line-colour=255,0,0",
              "arrives whole, with both settings still in it")

    print()
    print("a newline still separates values")
    two = pack("categories=Walls" + chr(10) + "view=FloorPlan: 1 - Mech")
    check(len(two) == 2, "two lines are two values")
    check([v.get("name") for v in two] == ["categories", "view"],
          "named in the order they were written")

    print()
    print("the name ends at the FIRST equals sign")
    one = pack("overrides=halftone=true")
    check(len(one) == 1 and one[0].get("name") == "overrides",
          "`overrides=halftone=true` is named `overrides`")
    check(len(one) == 1 and one[0].get("value") == "halftone=true",
          "and carries `halftone=true` as its value")

    print()
    print("nothing that says nothing is carried")
    junk = pack(chr(10).join(["", "   ", "no-equals-sign-here", "a=1"]))
    check(len(junk) == 1 and junk[0].get("name") == "a",
          "blank lines and a line with no equals sign are dropped")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - a semicolon belongs to the value, and the value crosses whole.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
