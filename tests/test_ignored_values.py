# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A caller value nothing declares is NAMED on the MCP path, not just the CLI.

    python tests/test_ignored_values.py

WHAT WENT WRONG
---------------
`undeclared_values` has caught this since FRAGMENT-ISSUES row 71, and it is
wired into the command line: `fragment set-category-graphics --set
categoryName=Walls` prints "IGNORED 'categoryName'" before Revit is touched.

`revit_change` never called it. So the SAME typo through the MCP path - the
one an assistant and its user actually use - was accepted, dropped, and
reported as a clean success. Measured on 2026-09-22 against Project1: the
capability ran, reported `overridden 1`, and said nothing at all about the
`categoryName=Walls` that reached nothing.

WHY THE CHECK IS NOT IN THE ADD-IN
----------------------------------
`undeclared_values`' own docstring settles it: the contract is read on this
side, before anything is sent, so catching a typo in C# would mean a round
trip to learn about it. A C# copy was written and removed on the same day for
that reason - and because a second implementation of one rule is how `binds:`
came to be honoured by the Python half and ignored by the executor.

SO THIS SUITE CHECKS TWO THINGS, AND THE SECOND IS THE ONE THAT WOULD ROT.
The helper's behaviour, and that `revit_change` actually CALLS it and PRINTS
what it returns. A helper nobody calls is exactly the state this fixes.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                            # noqa: E402

FAILURES = []


def check(condition, what):
    if condition:
        print("  ok    %s" % what)
    else:
        print("  FAIL  %s" % what)
        FAILURES.append(what)


NEEDS = [{"name": "view", "type": "View", "source": "request"},
         {"name": "categories", "type": "IList<Category>", "source": "request"},
         {"name": "overrides", "type": "OverrideGraphicSettings",
          "source": "request"},
         {"name": "doc", "type": "Document", "source": "ambient"}]


def values(*names):
    return [{"name": n, "value": "x"} for n in names]


def main():
    print("1. The helper itself")
    typo, takes = CLIENT.undeclared_values(
        values("view", "categories", "overrides", "categoryName"), NEEDS)
    check(typo == ["categoryName"],
          "a name the contract does not declare is returned - got %r" % (typo,))
    check(any(t.startswith("categories (") for t in takes),
          "and what it DOES take comes back with it")
    check(not any(t.startswith("doc (") for t in takes),
          "an ambient need is not offered - it is not the caller's to set")

    clean, _ = CLIENT.undeclared_values(
        values("view", "categories", "overrides"), NEEDS)
    check(clean == [], "a clean call returns nothing to say")

    none_given, _ = CLIENT.undeclared_values([], NEEDS)
    check(none_given == [], "no values at all returns nothing to say")

    print()
    print("2. revit_change calls it, and prints what it returns")
    try:
        text = io.open(SERVER, encoding="utf-8").read()
    except OSError as why:
        print("  FAIL  could not read %s: %s" % (SERVER, why))
        return 1

    start = text.find(chr(10) + "def revit_change(")
    check(start > 0, "revit_change is still in the server")
    if start < 0:
        return 1
    body = text[start:]
    end = body.find(chr(10) + "@server.tool()")
    if end > 0:
        body = body[:end]

    check("undeclared_values(" in body,
          "revit_change CALLS undeclared_values rather than carrying a copy")
    check("IGNORED" in body,
          "and the reply says IGNORED, so a dropped value leaves a trace")
    check("It takes:" in body,
          "and names what the capability does take, so the fix is one line away")

    # THE HELPER IS THE CLIENT'S, AND A COPY HERE WOULD DRIFT. Row 71's rule
    # has one home; this asserts the server did not grow a second one.
    check("def undeclared_values" not in text,
          "the server did not reimplement the rule")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - the MCP path names what it drops, using the one copy of the rule.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
