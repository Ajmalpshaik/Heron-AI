# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
BIM QA - it reports its own coverage, and empty is not absent.

    python tests/test_modelqa.py

WHAT IT PROVES
  1. EVERY ASPECT docs/28 NAMES IS ACCOUNTED FOR - checked, skipped or
     impossible - and none is quietly omitted.

  2. MEP CONNECTIVITY IS NEVER `CHECKED`, whatever it is handed.

  3. EMPTY, ABSENT AND POPULATED ARE THREE STATES. A parameter nobody
     filled in and one that was never added are different, and the
     counts prove it on the same model.

  4. CATEGORIES ARE SET ARITHMETIC IN BOTH DIRECTIONS - what this model
     uses and the profile does not, and what the profile uses and this
     model does not.

  5. NAMING IS ROUTED, NOT REPEATED - the shapes are
     HERON-STD-NAM-004's own answer.

  6. NOTHING IS CALLED WRONG.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_modelqa as QA                                     # noqa: E402
import heron_convention as NAM                                 # noqa: E402
import heron_exemplar as REF                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_modelqa.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    names = ([{"name": "QA2026-MEP-DUCT-SUPPLY-L%02d" % n, "kind": "element"}
              for n in range(1, 31)]
             + [{"name": "L%02d - GROUND" % n, "kind": "level"}
                for n in (1, 2, 3)])
    model = {"names": names,
             "categories": ["OST_DuctCurves", "OST_Walls"],
             "elements": [{"Mark": "D-%03d" % n, "Comments": ""}
                          for n in range(1, 21)]
                         + [{"Mark": ""} for _ in range(4)],
             "wanted": ["Mark", "Comments", "System Type"]}
    profile = REF.profile(
        [{"name": "QA2026-MEP-DUCT-SUPPLY-L%02d" % n, "kind": "element"}
         for n in range(1, 41)],
        categories=["OST_DuctCurves", "OST_PipeCurves"])

    answer = QA.check(model, profile=profile)
    by_aspect = dict((card["aspect"], card) for card in answer["aspects"])

    print("\n1. every aspect is accounted for")
    check(len(answer["aspects"]) == len(QA.ASPECTS),
          "all %d aspects have a row (%d)"
          % (len(QA.ASPECTS), len(answer["aspects"])))
    check(sorted(by_aspect) == sorted(QA.ASPECTS),
          "and they are docs/28's own eight")
    cover = answer["coverage"]
    check(len(cover["checked"]) + len(cover["skipped"])
          + len(cover["cannot"]) == len(QA.ASPECTS),
          "each is in exactly one state (%d + %d + %d)"
          % (len(cover["checked"]), len(cover["skipped"]),
             len(cover["cannot"])))
    for card in answer["aspects"]:
        if card["state"] != "checked":
            check(bool(card.get("why")),
                  "%s says why it was not checked" % card["aspect"])

    print("\n2. MEP connectivity is never checked")
    check(by_aspect["MEP connectivity"]["state"] == "cannot be checked here",
          "it cannot be checked here")
    check("geometry" in by_aspect["MEP connectivity"]["why"],
          "and the reason is geometry, inside Revit")
    loaded = QA.check(dict(model, **{"MEP connectivity": "all connected",
                                     "connectivity": True}),
                      profile=profile)
    said = dict((c["aspect"], c) for c in loaded["aspects"])
    check(said["MEP connectivity"]["state"] == "cannot be checked here",
          "and handing it a claim about connectivity changes nothing")

    print("\n3. empty, absent and populated are three states")
    rows = dict((one["parameter"], one)
                for one in by_aspect["parameters"]["parameters"])
    check(rows["Mark"][QA.POPULATED] == 20
          and rows["Mark"][QA.EMPTY] == 4
          and rows["Mark"][QA.ABSENT] == 0,
          "Mark: 20 populated, 4 empty, 0 absent")
    check(rows["Comments"][QA.EMPTY] == 20
          and rows["Comments"][QA.ABSENT] == 4,
          "Comments: 20 EMPTY and 4 ABSENT - present-and-blank against "
          "never-added, on the same model")
    check(rows["System Type"][QA.ABSENT] == 24
          and rows["System Type"][QA.EMPTY] == 0,
          "System Type: absent from all 24, and NOT counted as empty")
    for one in rows.values():
        check(one[QA.POPULATED] + one[QA.EMPTY] + one[QA.ABSENT] == one["of"],
              "%s: the three states add up to every element"
              % one["parameter"])
    check("missing" not in str(rows).lower(),
          "and no row carries a merged `missing` count")

    print("\n4. categories are set arithmetic in both directions")
    sets = by_aspect["categories"]["categories"]
    check(sets["onlyHere"] == ["OST_Walls"],
          "used here and not in the profile: %s" % sets["onlyHere"])
    check(sets["onlyThere"] == ["OST_PipeCurves"],
          "in the profile and NOT used here: %s" % sets["onlyThere"])
    check(sets["inBoth"] == ["OST_DuctCurves"], "and one in both")
    check(len(sets["used"]) == 2, "with what this model uses reported too")
    check(any("deliberate" in line for line in answer["unjudged"]),
          "and the answer says deliberate is what a count cannot see")

    print("\n5. naming is routed, not repeated")
    theirs = NAM.look(names)
    mine = by_aspect["naming"]["shapes"]
    check([card["kind"] for card in mine]
          == [card["kind"] for card in theirs["kinds"]],
          "the kinds are HERON-STD-NAM-004's own")
    check(mine == theirs["kinds"],
          "and so are the shapes, field for field")
    check("HERON-STD-NAM-004" in by_aspect["naming"]["by"],
          "the answer names who did it: %s" % by_aspect["naming"]["by"])
    check(by_aspect["levels"]["state"] == "checked"
          and by_aspect["levels"]["kinds"] == ["level"],
          "and levels are checked as NAMES, by the same agent")
    check(by_aspect["families"]["state"] == "skipped",
          "while families, with no names handed in, are skipped")
    check("shape_of" not in logic,
          "this module computes no shape of its own")

    print("\n6. nothing is called wrong")
    check(answer["judged"] is False, "`judged` is false")
    for card in answer["aspects"]:
        check(not any(key in card for key in
                      ("wrong", "failures", "violations", "score")),
              "%s carries no verdict field" % card["aspect"])

    print("\n7. every failure is named and reached")
    for these, name in ((None, "NOT_A_MODEL"), ({}, "NOT_A_MODEL"),
                        ("a string", "NOT_A_MODEL"),
                        ({"names": []}, "NOTHING_TO_CHECK"),
                        ({"names": [], "categories": []},
                         "NOTHING_TO_CHECK")):
        said = QA.check(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-QA-BIM-011.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
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
    print("PASS    it reports its own coverage, and empty is not absent")
    return 0


if __name__ == "__main__":
    sys.exit(main())
