# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-PVL-013
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Profile validation - nothing matching is a finding about the PROFILE.

    python tests/test_corroborate.py

WHAT IT PROVES
  1. IT RUNS ON HERON-STD-REF-010's REAL PROFILE, and shares its
     arithmetic by identity.

  2. DOMINANT IN BOTH IS A CONVENTION; present and not dominant is a
     COINCIDENCE. The two are counted, and they are different answers.

  3. NOTHING MATCHING IS A FINDING ABOUT THE PROFILE. Thirty correct
     names from another job produce ONE finding, not thirty violations.

  4. THE CANDIDATE JOB NUMBER IS DERIVED FROM THE DIFFERENCE, and never
     stripped - which is PROPOSALS F30's option 2, measured.

  5. A KIND THE PROFILE NEVER SAW IS NOT A FAILURE, and neither is a
     kind the second model has none of.

  6. NOTHING IS PROMOTED, AND NO NAME IS CALLED WRONG.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_corroborate as PVL                                # noqa: E402
import heron_exemplar as REF                                   # noqa: E402
import heron_convention as NAM                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def elements(pattern, count, kind="element"):
    return [{"name": pattern % n, "kind": kind} for n in range(1, count + 1)]


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_corroborate.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. it runs on HERON-STD-REF-010's real profile")
    first = REF.profile(elements("QA2026-MEP-DUCT-SUPPLY-L%02d", 40)
                        + elements("QA2026-M-%03d", 12, "sheet"))
    check(first["profiled"] is True, "a real profile was built")
    check(PVL.shape_of is NAM.shape_of,
          "PVL.shape_of IS NAM.shape_of - one arithmetic")
    check(PVL.ENTERS_AT is REF.ENTERS_AT,
          "and the rung IS that agent's constant")

    print("\n2. dominant in both is a convention; not dominant is a "
          "coincidence")
    same = PVL.validate(first, elements("QA2026-MEP-PIPE-CHW-L%02d", 20))
    check(same["corroborated"] == ["element"],
          "the element pattern is corroborated: %s" % same["corroborated"])
    by_kind = dict((card["kind"], card) for card in same["kinds"])
    check(by_kind["element"]["matched"] == 20
          and by_kind["element"]["proportion"] == 100,
          "20 of 20 take it (%d%%)" % by_kind["element"]["proportion"])
    # PRESENT AND NOT DOMINANT is the third answer, and it is neither of
    # the other two.
    mixed = PVL.validate(first,
                         elements("QA2026-MEP-DUCT-SUPPLY-L%02d", 3)
                         + elements("QA2026 duct %d", 9))
    check(mixed["coincidence"] == ["element"],
          "present and not dominant is a coincidence: %s"
          % mixed["coincidence"])
    check(mixed["corroborated"] == [], "and NOT corroborated")
    check(mixed["didNotTransfer"] == [],
          "and not a transfer failure either - it did match, just not "
          "dominantly")
    check(dict((c["kind"], c) for c in mixed["kinds"])["element"]["matched"]
          == 3, "three matched")

    print("\n3. nothing matching is a finding about the profile")
    other = PVL.validate(first, elements("MEP-DUCT-SUPPLY-L%02d", 30))
    check(other["didNotTransfer"] == ["element"],
          "it is reported as the profile not transferring: %s"
          % other["didNotTransfer"])
    card = dict((c["kind"], c) for c in other["kinds"])["element"]
    check(card["of"] == 30, "over 30 names (%d)" % card["of"])
    check(card["matched"] == 0, "none matched")
    check(len(other["didNotTransfer"]) == 1,
          "ONE finding, not 30 violations")
    check("about the PROFILE" in card["why"],
          "and the reason says whose problem it is")
    # NOT A WORD SEARCH. The module says "not 30 FAILURES" and "not a
    # VIOLATION", correctly, so searching its prose finds its own denials.
    # The property that matters is structural: the answer is about SHAPES
    # and KINDS, and no individual name from the second model appears in
    # it anywhere - nobody gets named and shamed.
    said = " ".join(REF._strings(other))
    leaked = [card["name"] for card in elements("MEP-DUCT-SUPPLY-L%02d", 30)
              if card["name"] in said]
    check(not leaked,
          "and not one of the 30 names appears in the answer%s"
          % ("" if not leaked else ": %s" % ", ".join(leaked[:3])))

    print("\n4. the candidate job number is derived, never stripped")
    check(card["candidateProjectSegments"] == 1,
          "one leading segment differs (%d)"
          % card["candidateProjectSegments"])
    check(PVL.segments("A9-A-A-A-A9") == ["A9", "A", "A", "A", "A9"],
          "a shape splits into its parts: %s"
          % PVL.segments("A9-A-A-A-A9"))
    check(PVL._extra_at_front("A9-A-A-A-A9", "A-A-A-A9") == 1,
          "and the extra is counted FROM THE BACK, because a job number "
          "goes on the front")
    check(PVL._extra_at_front("A-A-A-A9", "A9-A-A-A-A9") == 0,
          "a shorter profile yields no candidate")
    check(PVL._extra_at_front("A-9-A", "X-Y-Z") == 0,
          "and two shapes with nothing in common yield none either")
    check(card["profileShape"] != card["theirDominant"],
          "both shapes are reported side by side: %r vs %r"
          % (card["profileShape"], card["theirDominant"]))
    check(any("NEVER STRIPPED" in line.upper()
              for line in other["unjudged"]),
          "and the answer says it is named, never stripped")

    print("\n5. an unseen kind is not a failure")
    extra = PVL.validate(first, elements("Level %d", 4, "level"))
    level = dict((c["kind"], c) for c in extra["kinds"])["level"]
    check(level["inProfile"] is False,
          "a kind the profile never saw is marked as such")
    check("not a failure" in level["why"],
          "and said to be no failure: %r" % level["why"][-32:])
    check("level" not in extra["didNotTransfer"],
          "it is not counted as a transfer failure")
    sheet = dict((c["kind"], c) for c in extra["kinds"])["sheet"]
    check(sheet["of"] == 0 and sheet["corroborated"] is False,
          "and a kind the second model has none of is neither confirmed "
          "nor contradicted")
    check("sheet" not in extra["didNotTransfer"],
          "nor counted as a failure")

    print("\n6. nothing is promoted and no name is called wrong")
    check(same["promoted"] is False, "`promoted` is false")
    check(same["stays_at"] == "DISCOVERED",
          "and it stays at %r" % same["stays_at"])
    for answer in (same, other, mixed):
        for card in answer["kinds"]:
            check(not any(key in card for key in
                          ("violations", "wrong", "failures", "score")),
                  "%s carries no verdict field: %s"
                  % (card["kind"], ", ".join(sorted(card))))
    for one in other["kinds"]:
        for shape in one.get("unmatched", []):
            check(not any(key in shape for key in ("wrong", "ok", "score")),
                  "no unmatched shape carries a verdict: %s"
                  % ", ".join(sorted(shape)))

    print("\n7. every failure is named and reached")
    for these, names, name in ((None, ["x"], "NOT_A_PROFILE"),
                               ({"profiled": False}, ["x"], "NOT_A_PROFILE"),
                               ("a string", ["x"], "NOT_A_PROFILE"),
                               (first, [], "NOTHING_TO_VALIDATE"),
                               (first, None, "NOTHING_TO_VALIDATE"),
                               (first, [123], "NOT_A_NAME"),
                               (first, [{"kind": "sheet"}], "NOT_A_NAME")):
        said = PVL.validate(these, names)
        reached.add(said.get("refused"))
        check(said.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-STD-PVL-013.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 3, "the contract declares 3 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(other["unjudged"]) == 5, "five things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    nothing matching is a finding about the profile")
    return 0


if __name__ == "__main__":
    sys.exit(main())
