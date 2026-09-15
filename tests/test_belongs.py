# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-ARC-011
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Architecture matching - a classification is not a kind.

    python tests/test_belongs.py

WHAT IT PROVES
  1. THE TWO VOCABULARIES REALLY SHARE NO WORD, checked against both
     agents' own lists rather than against a sentence in a docstring.

  2. NO FOLDER IS DECIDED HERE. Every placed answer is IDENTICAL to
     calling HERON-WSP-PLC-005 directly, field for field.

  3. `placed: False` IS NOT A REFUSAL. That agent declares it always
     false, and a version of this file that read it as the verdict
     placed nothing at all.

  4. EVERY ONE OF THE FIVE CLASSIFICATIONS IS INTERCEPTED, and each
     either names the agent that must run first or says nothing places
     it - never both, never neither.

  5. A REFUSAL FROM PLACEMENT COMES BACK WHOLE, not restated.

  6. NOTHING IS MOVED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_belongs as ARC                                    # noqa: E402
import heron_placement as PLC                                  # noqa: E402
import heron_classify as CLS                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_belongs.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the two vocabularies share no word")
    check(ARC.KINDS is PLC.KINDS, "ARC.KINDS IS PLC.KINDS - the same object")
    check(ARC.CATEGORIES is CLS.CATEGORIES,
          "ARC.CATEGORIES IS CLS.CATEGORIES - the same object")
    shared = sorted(set(PLC.KINDS) & set(CLS.CATEGORIES))
    check(shared == [],
          "and the two real lists overlap in nothing%s"
          % ("" if not shared else ": %s" % ", ".join(shared)))
    check(sorted(ARC.BECOMES_A_KIND) == sorted(CLS.CATEGORIES),
          "every classification has a row, and no row invents a sixth")

    print("\n2. no folder is decided here")
    answer = ARC.match([{"kind": "fragment", "name": "COUNT_DUCTS"},
                        {"kind": "skill", "name": "count-things"}])
    check(ARC.place is PLC.place, "ARC.place IS PLC.place - one decision")
    for card in answer["placed"]:
        theirs = PLC.place({"kind": card["kind"], "name": card["of"]})
        same = all(theirs.get(field) == card.get(field)
                   for field in ("folder", "kind", "class", "why"))
        check(same, "%s comes back exactly as %s answered it"
                    % (card["of"], "HERON-WSP-PLC-005"))
    check([card["folder"] for card in answer["placed"]]
          == ["Fragments", "Skills"],
          "a fragment goes to Fragments and a skill to Skills")

    print("\n3. `placed: False` is not a refusal")
    theirs = PLC.place({"kind": "fragment", "name": "COUNT_DUCTS"})
    check(theirs["placed"] is False,
          "HERON-WSP-PLC-005 says placed: False on its SUCCESS path")
    check("refused" not in theirs,
          "and carries no `refused` - which is how success is told")
    check(len(answer["placed"]) == 2 and not answer["refused_by_placement"],
          "so both items are placed here, not rejected")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-PLC-005.yaml"))
    said = (contract.get("output") or {}).get("placed", {})
    check("false" in str(said.get("description", "")).lower(),
          "that agent's own contract declares it always false: %r"
          % said.get("description", "").strip())

    print("\n4. every classification is intercepted")
    everything = ARC.match([{"kind": kind, "name": "thing.%s" % kind}
                            for kind in sorted(CLS.CATEGORIES)])
    seen = ([card["category"] for card in everything["not_yet"]]
            + [card["category"] for card in everything["no_home"]])
    check(sorted(seen) == sorted(CLS.CATEGORIES),
          "all %d are accounted for: %s"
          % (len(CLS.CATEGORIES), ", ".join(sorted(seen))))
    check(len(seen) == len(set(seen)),
          "each lands in exactly one bucket, never both")
    check(not everything["placed"],
          "and NONE of them was placed in a data folder")
    check(not everything["refused_by_placement"],
          "none was passed to HERON-WSP-PLC-005 to be refused as a typo")
    for card in everything["not_yet"]:
        check(card["needs"] and "HERON-" in card["needs"],
              "%s names the agent that must run first" % card["category"])
    for card in everything["no_home"]:
        check(card["needs"] is None and "F28" in card["why"],
              "%s says nothing places it, and points at F28"
              % card["category"])
    check(sorted(card["category"] for card in everything["no_home"])
          == ["asset", "config", "metadata"],
          "the three with nowhere to go are asset, config, metadata")

    print("\n5. a refusal from placement comes back whole")
    bad = ARC.match([{"kind": "fragment", "name": "../escape"}])
    check(len(bad["refused_by_placement"]) == 1, "it is reported")
    theirs = PLC.place({"kind": "fragment", "name": "../escape"})
    mine = bad["refused_by_placement"][0]
    check(mine["refused"] == theirs["refused"] == "NAME_IS_NOT_A_PLACE",
          "with that agent's own code")
    check(mine["why"] == theirs["why"],
          "and its own words, unaltered")
    ninth = ARC.match([{"kind": "spreadsheet", "name": "costs.xlsx"}])
    check(ninth["refused_by_placement"][0]["refused"] == "NOT_A_KIND",
          "a kind that is neither a classification NOR one of the eight "
          "still reaches placement and is refused there")

    print("\n6. nothing is moved")
    check(answer["moved"] is False, "`moved` is false")
    check("shutil" not in logic and "os.rename" not in logic
          and "makedirs" not in logic,
          "and the module cannot move anything - no rename, no makedirs")

    print("\n7. every failure is named and reached")
    for these, name in (([], "NOTHING_TO_MATCH"),
                        (None, "NOTHING_TO_MATCH"),
                        (["a string"], "NOT_AN_ITEM"),
                        ([None], "NOT_AN_ITEM")):
        said = ARC.match(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)

    mine = CON.load(os.path.join(ROOT, "brain", "agents",
                                 "HERON-IMP-ARC-011.yaml"))
    named = mine.get("failures") or []
    check(len(named) == 2, "the contract declares 2 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(answer["unjudged"]) == 5, "five things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a classification is not a kind")
    return 0


if __name__ == "__main__":
    sys.exit(main())
