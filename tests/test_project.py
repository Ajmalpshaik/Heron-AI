# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-PRJ-009
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Project standard - it outranks, and the loser is still in the answer.

    python tests/test_project.py

WHAT IT PROVES
  1. THE LADDER IS heron_conflict's OBJECT, not a copy of docs/20 s2.

  2. IT RULES ON heron_conflict's REAL RECORDS - built with that module's
     own Value and Disagreement classes, so a renamed field fails here.

  3. PROJECT BEATS COMPANY, and the sentence docs/20 s2 asks for is in
     the answer.

  4. THE OVERRIDDEN CLAUSE IS STILL THERE, with its document and
     locator - "overrides must not mean silently replaces".

  5. TWO SOURCES ON ONE RUNG ARE NOT SEPARATED. This is the case a
     ranking would have settled quietly.

  6. A SCOPE THE LADDER DOES NOT NAME GETS NO PLACE AT THE BOTTOM.

  7. EVERY RUNG IS TESTED, not just the pair in the register's row.

  8. AN EMPTY LIST IS NOT A REPORT THAT THE SOURCES AGREE.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_project as PRJ                                    # noqa: E402
import heron_conflict as CNF                                   # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def value(scope, document, number, unit="mm", locator="3.1"):
    """A REAL heron_conflict Value. A renamed field fails here, not later."""
    return CNF.Value(scope=scope, label=scope, unit=unit, value=number,
                     document=document, locator=locator,
                     path="standards/%s.pdf" % document, chunk=1,
                     document_id=document)


def found(unit, *values):
    """A REAL heron_conflict Disagreement."""
    return CNF.Disagreement(unit, list(values))


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_project.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the ladder is heron_conflict's object")
    check(PRJ.HIERARCHY is CNF.HIERARCHY,
          "PRJ.HIERARCHY IS CNF.HIERARCHY - the same tuple")
    # NOT A WORD SEARCH for the scope names: the module names them in its
    # prose, correctly. The proof is that no tuple of its own exists.
    check(logic.count("HIERARCHY = CNF.HIERARCHY") == 1
          and SCOPE.PROJECT not in [line.strip() for line in logic.split("\n")],
          "and it builds no second ladder of its own")

    print("\n2. it rules on heron_conflict's real records")
    pair = found("length",
                 value(SCOPE.PROJECT, "Tower B Project Specification", "40"),
                 value(SCOPE.COMPANY, "Acme BIM Standard 2026", "30"))
    answer = PRJ.governs([pair])
    check(answer["ruled"] is True, "a real Disagreement is ruled on")
    check(answer["of"] == 1, "one disagreement in, one counted")

    print("\n3. project beats company, and it says so")
    ruling = answer["settled"][0]
    check(ruling["governs"]["scope"] == SCOPE.PROJECT,
          "the project clause governs")
    check(ruling["governs"]["value"] == "40", "which is the 40mm one")
    # docs/20 s2 writes the sentence out; the answer must carry that shape.
    for part in ("using", "Tower B Project Specification", "differs from",
                 "Acme BIM Standard 2026"):
        check(part in ruling["says"], "the sentence names %r" % part)

    print("\n4. the overridden clause is still there")
    kept = ruling["overridden"]
    check(len(kept) == 1, "one clause was overridden and it is in the answer")
    check(kept[0]["value"] == "30" and kept[0]["scope"] == SCOPE.COMPANY,
          "the company's 30mm, by value and scope")
    for field in ("document", "locator", "unit"):
        check(kept[0].get(field),
              "and carries its %s, so a reader can go and look" % field)

    print("\n5. two sources on one rung are not separated")
    same = PRJ.governs([found("length",
                              value(SCOPE.PROJECT, "Spec Rev A", "40"),
                              value(SCOPE.PROJECT, "Addendum 2", "45"))])
    check(not same["settled"], "nothing was settled")
    check(len(same["unordered"]) == 1, "it comes back unordered")
    check(len(same["asks"]) == 1, "with a question, not a pick")
    check(len(same["asks"][0]["clauses"]) == 2,
          "and the question carries both clauses")
    check(SCOPE.PROJECT in same["unordered"][0]["why"],
          "the reason names the scope they share")

    print("\n6. a scope the ladder does not name gets no place at the bottom")
    outside = PRJ.governs([found("length",
                                 value("community", "A forum post", "25"),
                                 value(SCOPE.COMPANY, "Acme 2026", "30"))])
    check(not outside["settled"],
          "the company clause does NOT win by the other being unranked")
    check(len(outside["asks"]) == 1, "it is asked about instead")
    check("community" in outside["unordered"][0]["why"],
          "and the reason names the scope that has no rung")

    print("\n7. every rung is tested")
    ladder = list(PRJ.HIERARCHY)
    for above in range(len(ladder)):
        for below in range(above + 1, len(ladder)):
            one = PRJ.governs([found("length",
                                     value(ladder[below], "Lower", "10"),
                                     value(ladder[above], "Upper", "20"))])
            won = one["settled"] and \
                one["settled"][0]["governs"]["scope"] == ladder[above]
            if not won:
                check(False, "%s should outrank %s"
                             % (ladder[above], ladder[below]))
    check(True, "%d rung pairs, higher always governs"
                % (len(ladder) * (len(ladder) - 1) // 2))
    # AND THE ORDER IS NOT ALPHABETICAL BY ACCIDENT. company < project as
    # strings, and the ladder says the opposite.
    check(SCOPE.COMPANY < SCOPE.PROJECT
          and ruling["governs"]["scope"] == SCOPE.PROJECT,
          "'company' sorts before 'project' alphabetically, and the ladder "
          "still puts project on top - so it is not string order")

    print("\n8. an empty list is not a report that the sources agree")
    for these in (None, []):
        said = PRJ.governs(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOTHING_TO_RULE",
              "%r is refused" % (these,))
        check("agree" in said["why"],
              "and the refusal says why an empty list cannot mean agreement")

    print("\n9. every failure is named and reached")
    for these in (["a string"], [None], [found("length")]):
        said = PRJ.governs(these)
        reached.add(said.get("refused"))
        check(said.get("refused") == "NOT_A_DISAGREEMENT",
              "%r is refused" % (these,))

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-STD-PRJ-009.yaml"))
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
    print("PASS    it outranks, and the loser is still in the answer")
    return 0


if __name__ == "__main__":
    sys.exit(main())
