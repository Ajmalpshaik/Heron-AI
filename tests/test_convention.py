# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-NAM-004
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Naming standard - it counts shapes and calls nothing wrong.

    python tests/test_convention.py

WHAT IT PROVES
  1. THE SHAPE IS THE SHAPE. Worked out by hand for names a modeller
     would actually type, including the two that must NOT collapse
     together.

  2. RUNS COLLAPSE AND SEPARATORS DO NOT. `SUP` and `EXHAUST` are one
     shape; `SUP-300` and `SUP_300` are two.

  3. IT CALLS NOTHING WRONG. No output field holds a verdict, and the
     answer is IDENTICAL whether the clause says hyphens or underscores.

  4. THE OUTLIER IS A COUNT. One name in four hundred comes back as a
     shape carried once - never flagged, never scored.

  5. ONE QUESTION PER KIND, NOT PER NAME - 400 names, one question.

  6. NO CLAUSE MEANS NO QUESTION, AND THE COUNTS STILL COME BACK.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_convention as NAM                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_convention.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the shape is the shape")
    for name, want in (("SUP-300-GALV", "A-9-A"),
                       ("EXHAUST-250-GALV", "A-9-A"),
                       ("L01-MECH-PLAN", "A9-A-A"),
                       ("Duct 4 (copy)", "A 9 (A)"),
                       ("M-001", "A-9"),
                       ("Sheet 1", "A 9"),
                       ("A1", "A9"),
                       ("2026-09-15", "9-9-9"),
                       ("plan.rvt", "A.A"),
                       ("", "")):
        check(NAM.shape_of(name) == want,
              "%-18r -> %r" % (name, want))

    print("\n2. runs collapse and separators do not")
    check(NAM.shape_of("SUP-300-GALV") == NAM.shape_of("EXHAUST-250-GALV"),
          "SUP and EXHAUST are ONE shape - a standard that cared about "
          "length would have to say so")
    check(NAM.shape_of("SUP-300-GALV") != NAM.shape_of("SUP_300_GALV"),
          "but a hyphen and an underscore are TWO - on a real job the "
          "separator is the convention")
    check(NAM.shape_of("SUP") == NAM.shape_of("sup"),
          "case does not make a new shape")
    check(NAM.separators("SUP_300-GALV (2)") == ["_", "-", " ", "(", ")"],
          "every separator is reported, in the order seen: %s"
          % NAM.separators("SUP_300-GALV (2)"))

    print("\n3. it calls nothing wrong")
    ducts = ([{"name": "SUP-%d-GALV" % n, "kind": "element"}
              for n in (100, 150, 200)]
             + [{"name": "SUP_250_GALV", "kind": "element"}])
    hyphens = [{"document": "Acme", "locator": "3.2",
                "text": "separated by hyphens"}]
    unders = [{"document": "Acme", "locator": "3.2",
               "text": "separated by underscores"}]
    one = NAM.look(ducts, clauses=hyphens)
    two = NAM.look(ducts, clauses=unders)
    check(one["judged"] is False, "`judged` is false")
    check(one["kinds"] == two["kinds"],
          "and the SHAPES are identical whichever way the clause reads - "
          "the rule changed and the counting did not")
    for card in one["kinds"]:
        for shape in card["shapes"]:
            check(not any(key in shape for key in
                          ("ok", "wrong", "valid", "score", "verdict")),
                  "no shape carries a verdict field: %s"
                  % ", ".join(sorted(shape)))

    print("\n4. the outlier is a count")
    many = [{"name": "SUP-%d-GALV" % n, "kind": "element"}
            for n in range(100, 500)]
    many.append({"name": "Duct 4 (copy)", "kind": "element"})
    answer = NAM.look(many, clauses=hyphens)
    card = answer["kinds"][0]
    check(card["of"] == 401, "401 names (%d)" % card["of"])
    check(card["commonest"] == "A-9-A",
          "the commonest shape is %r" % card["commonest"])
    check(card["alone"] == ["A 9 (A)"],
          "and exactly one shape is carried by one name: %s" % card["alone"])
    check(card["shapes"][0]["of"] == 400,
          "commonest first, with its count (%d)" % card["shapes"][0]["of"])
    check(card["shapes"][-1]["examples"] == ["Duct 4 (copy)"],
          "and the outlier's own name travels with it")

    print("\n5. one question per kind, not per name")
    check(len(answer["asks"]) == 1,
          "401 names, %d question" % len(answer["asks"]))
    mixed = NAM.look(many + [{"name": "M-001", "kind": "sheet"},
                             {"name": "M-002", "kind": "sheet"}],
                     clauses=hyphens)
    check(len(mixed["asks"]) == 2, "two kinds, two questions")
    check(sorted(one["kind"] for one in mixed["asks"])
          == ["element", "sheet"], "one each")
    check(all(ask["clauses"] == hyphens for ask in mixed["asks"]),
          "and the clause travels with every question")

    print("\n6. no clause means no question")
    quiet = NAM.look(many)
    check(quiet["looked"] is True, "it still looks")
    check(quiet["asks"] == [], "and asks nothing")
    check(quiet["kinds"] == answer["kinds"],
          "while the counts are exactly the same as with a clause")
    check("NO CLAUSE WAS HANDED IN" in quiet["why"],
          "the answer says why it asked nothing")

    print("\n7. every failure is named and reached")
    for these, name in (([], "NOTHING_TO_CHECK"),
                        (None, "NOTHING_TO_CHECK"),
                        ([123], "NOT_A_NAME"),
                        ([{"kind": "sheet"}], "NOT_A_NAME"),
                        ([{"name": "   "}], "NOT_A_NAME")):
        said = NAM.look(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)
    check(NAM.look(["SUP-300-GALV"])["kinds"][0]["kind"] == NAM.KINDS[0],
          "a bare string is read as a %r" % NAM.KINDS[0])

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-STD-NAM-004.yaml"))
    named = contract.get("failures") or []
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
    print("PASS    it counts shapes and calls nothing wrong")
    return 0


if __name__ == "__main__":
    sys.exit(main())
