# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-CLS-012
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Clash and coordination - a clean result over an unloaded link is not a
clean model.

    python tests/test_coordination.py

WHAT IT PROVES
  1. NO CLASH IS COMPUTED, and `clashesFound` is null rather than 0.

  2. THE THREE LINK STATES ARE THREE, and every link is in exactly one.
     Unloaded, not found, loaded-and-empty and loaded-with-elements give
     the answers they should.

  3. NOT-KNOWN IS NOT ZERO. A link reporting `elements: None` is not
     clashable; one reporting 0 is empty. They are different rows.

  4. IT READS HERON-REVIT-LNK-015's REAL RECORD, straight out of that
     agent, with nothing renamed in between.

  5. A LEVEL MISMATCH IS FOUND AND IS NOT CALLED WRONG - the case no
     clash engine reports, because nothing intersects.

  6. NAMES ARE COMPARED AS WRITTEN - `L03` and `Level 3` do not match,
     deliberately.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_coordination as CLS                               # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


HOST = {"levels": ["L01", "L02", "L03", "ROOF"], "grids": ["A", "B", "C"]}

LINKS = [
    {"name": "ARCH", "status": "loaded", "elements": 48210,
     "levels": ["L01", "L02", "L03", "ROOF"], "grids": ["A", "B", "C"]},
    {"name": "STRUCT", "status": "unloaded", "elements": None},
    {"name": "ELEC", "status": "loaded", "elements": 0},
    {"name": "SITE", "status": "not found", "elements": None},
    {"name": "PLUMB", "status": "loaded", "elements": 3100,
     "levels": ["Level 1", "Level 2", "Level 3"], "grids": ["A", "B"]},
]


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_coordination.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    answer = CLS.coordinate(HOST, LINKS)
    by_name = dict((card["name"], card) for card in answer["links"])

    print("\n1. no clash is computed")
    check(answer["coordinated"] is True, "the host and links were read")
    check(answer["clashesFound"] is None,
          "`clashesFound` is %r - null, never 0" % answer["clashesFound"])
    check(len(answer["cannot"]) == 1 and "geometry" in answer["cannot"][0]
          or "intersection" in answer["cannot"][0],
          "and what it cannot do is named: %r"
          % answer["cannot"][0][:40])
    check("Intersect" not in logic and "solid" not in logic.lower()
          .replace("solid intersection", ""),
          "the module computes no intersection of any kind")

    print("\n2. the three link states are three")
    check(by_name["ARCH"]["state"] == CLS.CLASHABLE,
          "loaded with elements -> clashable")
    check(by_name["STRUCT"]["state"] == CLS.NOT_CLASHABLE,
          "unloaded -> not clashable")
    check(by_name["SITE"]["state"] == CLS.NOT_CLASHABLE,
          "not found -> not clashable")
    check(by_name["ELEC"]["state"] == CLS.EMPTY,
          "loaded and holding nothing -> empty")
    states = [card["state"] for card in answer["links"]]
    check(len(states) == len(LINKS), "every link got a state")
    check(len(answer["clashable"]) + len(answer["notClashable"])
          + len(answer["empty"]) == len(LINKS),
          "and each is in exactly one bucket (%d)" % len(LINKS))
    check(sorted(set(states)) == sorted([CLS.CLASHABLE, CLS.NOT_CLASHABLE,
                                         CLS.EMPTY]),
          "all three states occur: %s" % ", ".join(sorted(set(states))))

    print("\n3. not-known is not zero")
    check("elements" not in by_name["STRUCT"],
          "an unloaded link carries NO element count at all")
    check(by_name["ELEC"]["elements"] == 0,
          "while the empty one carries 0")
    check(by_name["STRUCT"]["state"] != by_name["ELEC"]["state"],
          "and they are different states, not one `nothing there`")
    odd = CLS.coordinate(HOST, [{"name": "ODD", "status": "loaded",
                                 "elements": None}])
    check(odd["links"][0]["state"] == CLS.NOT_CLASHABLE,
          "a link claiming loaded with NO count is not clashable either")
    check("NOT KNOWN" in odd["links"][0]["why"],
          "and the reason names what HERON-REVIT-LNK-015 returns")

    print("\n4. it reads HERON-REVIT-LNK-015's real record")
    # THAT AGENT IS C# AND CARRIES NO CONTRACT YAML - the Revit agents do
    # not. So the field names are checked against the SOURCE that emits
    # them, which tests/ may read (D-48 puts every layer in reach here).
    source = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin",
                                  "RevitLinks.cs"), encoding="utf-8").read()
    for field in ("name", "status", "elements"):
        check('Json.Str("%s"' % field in source
              or 'Json.Num("%s"' % field in source
              or 'Json.Bool("%s"' % field in source,
              "the C# emits %r, which this agent reads" % field)
    check('Json.Str("elements", null)' in source,
          "and an unloaded link's count is emitted as NULL, not 0 - which "
          "is exactly what `not clashable` is built on")
    check("elementsWhy" in source,
          "with its own reason field beside it")
    check(CLS.LOADED == "loaded" and '"loaded"' in source,
          "and `loaded` is the same word on both sides")

    print("\n5. a level mismatch is found and not called wrong")
    plumb = [one for one in answer["hosts"] if one["link"] == "PLUMB"][0]
    levels = plumb["hosts"]["levels"]
    check(levels["inBoth"] == [],
          "PLUMB shares NO level name with the host")
    check(levels["onlyInHost"] == HOST["levels"],
          "all four are host-only: %s" % ", ".join(levels["onlyInHost"]))
    check(levels["onlyInLink"] == ["Level 1", "Level 2", "Level 3"],
          "and three are link-only: %s" % ", ".join(levels["onlyInLink"]))
    check(plumb["hosts"]["grids"]["onlyInHost"] == ["C"],
          "the grid difference is found too: %s"
          % plumb["hosts"]["grids"]["onlyInHost"])
    arch = [one for one in answer["hosts"] if one["link"] == "ARCH"][0]
    check(arch["hosts"]["levels"]["onlyInHost"] == []
          and arch["hosts"]["levels"]["onlyInLink"] == [],
          "while the link that agrees reports no difference")
    for one in answer["hosts"]:
        for what in CLS.HOSTS:
            check(not any(key in one["hosts"][what] for key in
                          ("wrong", "error", "clash")),
                  "%s/%s carries no verdict" % (one["link"], what))
    check(any("not necessarily wrong" in line for line in answer["unjudged"]),
          "and the answer says a difference may be correct")

    print("\n6. names are compared as written")
    check("L03" not in levels["inBoth"] and "Level 3" not in levels["inBoth"],
          "`L03` and `Level 3` do not match")
    check(any("AS WRITTEN" in line.upper() for line in answer["unjudged"]),
          "and the answer says so, with the reason")
    check("lower()" not in logic.split("def _named")[1].split("def ")[0],
          "the name reader does not case-fold")

    print("\n7. every failure is named and reached")
    for these, links, name in ((None, LINKS, "NOT_A_MODEL"),
                               ({}, LINKS, "NOT_A_MODEL"),
                               ("a string", LINKS, "NOT_A_MODEL"),
                               (HOST, [], "NOTHING_LINKED"),
                               ({"levels": ["L01"]}, None, "NOTHING_LINKED"),
                               (HOST, ["a string"], "NOT_A_LINK"),
                               (HOST, [123, None], "NOT_A_LINK")):
        said = CLS.coordinate(these, links)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)

    mine = CON.load(os.path.join(ROOT, "brain", "agents",
                                 "HERON-QA-CLS-012.yaml"))
    named = mine.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 6, "six things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a clean result over an unloaded link is not a clean model")
    return 0


if __name__ == "__main__":
    sys.exit(main())
