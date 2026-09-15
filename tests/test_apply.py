# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-UPD-008
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Applying - a changed implementation is not still proven.

    python tests/test_apply.py

WHAT IT PROVES
  1. ONLY WHAT EVOLUTION LISTED MAY BE APPLIED, and a ruled-out verdict
     comes back with that agent's own reason for ruling it out.

  2. THREE ARE HANDED ON BY NAME, to the owners HERON-FRG-EVO-005 names
     - not refused as nonsense, because somebody chose them.

  3. UPDATE AND EXTEND DROP THE PROOF AND LAND AT DRAFT. DEPRECATE and
     ARCHIVE do not touch it. The two are told apart by whether the code
     moved, not by how big the change sounds.

  4. KEEP CHANGES NOTHING AND RETURNS NO CARD.

  5. AT PRODUCTION AN APPROVAL MUST NAME BOTH the fragment and the
     verdict, and a machine may not give one.

  6. BELOW PRODUCTION NO APPROVAL IS DEMANDED, and the answer says
     demanding one would be inventing a gate.

  7. NOTHING IS WRITTEN TO DISK.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_apply as UPD                                      # noqa: E402
import heron_evolve as EVO                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_promotion as PRO                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

PROOF = {"model": "Tower A, Revit 2024", "by": "Ajmal"}


def plan(status="PROVEN", **findings):
    card = {"id": "count-elements", "heron-status": status,
            "proof": dict(PROOF)}
    answer = EVO.consider(card, because="it failed twice on Revit 2021",
                          findings=findings or {"SPLIT": "two parts"})
    answer["card"] = dict(card)
    return answer


def approval(**changes):
    one = {"by": "Ajmal", "at": "2026-09-15 12:00",
           "fragment": "count-elements", "verdict": "UPDATE"}
    one.update(changes)
    return one


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_apply.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. only what Evolution listed may be applied")
    proposal = plan()
    check(UPD.apply_verdict(proposal, "DEPRECATE")["applied"] is True,
          "a verdict Evolution listed as available goes through")
    blocked = UPD.apply_verdict(proposal, "MERGE")
    reached.add(blocked.get("refused"))
    check(blocked["refused"] == "NOT_AVAILABLE",
          "one it ruled out is refused")
    ruled = [one for one in proposal["ruled_out"]
             if one["verdict"] == "MERGE"][0]
    check(ruled["why"] in blocked["why"],
          "carrying Evolution's OWN reason, not a new one")
    check("not the same as applying whatever is handed over"
          in blocked["why"],
          "with what 'applies what Evolution decided' means")
    check(blocked["available"] == [one["verdict"]
                                   for one in proposal["available"]],
          "and what WAS available, so somebody can choose again")

    print("\n2. three are handed on by name")
    check(sorted(UPD.HANDS_ON) == ["BRANCH", "MERGE", "SPLIT"],
          "SPLIT, MERGE and BRANCH")
    check(sorted(UPD.APPLIES + UPD.HANDS_ON) == sorted(EVO.VERDICTS),
          "and the two lists together are exactly docs/28's eight")
    on = UPD.apply_verdict(proposal, "SPLIT")
    reached.add(on.get("refused"))
    check(on["refused"] == "NOT_MINE_TO_APPLY",
          "a handed-on verdict is not applied")
    check(on["owner"] == EVO.OWNS["SPLIT"][0] == "HERON-FRG-SPL-003",
          "and names the owner HERON-FRG-EVO-005 names")
    check("refused as nonsense" in on["why"],
          "handed on rather than refused as nonsense - somebody chose it")

    print("\n3. update and extend drop the proof")
    check(UPD.MOVES_THE_CODE == ("UPDATE", "EXTEND"),
          "those two move the code")
    for verdict in UPD.MOVES_THE_CODE:
        answer = UPD.apply_verdict(plan(), verdict,
                                   implementation="new bytes")
        check(answer["status"] == "DRAFT" and answer["was"] == "PROVEN",
              "%s takes PROVEN down to DRAFT" % verdict)
        check(answer["proof_kept"] is False and "proof" not in answer["card"],
              "  and the proof is gone from the card")
        check("proof" in answer["changed"],
              "  listed among what changed")
    for verdict in ("DEPRECATE", "ARCHIVE"):
        where = "PROVEN" if verdict == "DEPRECATE" else "DEPRECATED"
        answer = UPD.apply_verdict(plan(status=where), verdict)
        check(answer["applied"] and answer["proof_kept"] is True,
              "%s does not touch the proof" % verdict)
        check(answer["card"].get("proof") == PROOF,
              "  it is still on the card, unchanged")
    check(UPD.LANDS_AT == "DRAFT" and UPD.LANDS_AT in FRAG.STATUSES,
          "and DRAFT is read off docs/09's ladder, not typed")
    check(UPD.NEEDS_PROOF is FRAG.NEEDS_PROOF,
          "as is the list of statuses that need a proof, by identity")

    print("\n4. KEEP changes nothing")
    kept = UPD.apply_verdict(proposal, "KEEP")
    check(kept["applied"] is True, "it is applied")
    check(kept["changed"] == [] and "card" not in kept,
          "and nothing changed - no card comes back at all")
    check(kept["status"] == kept["was"] == "PROVEN",
          "the status is where it was")
    check("quietest bug in the library" in kept["why"],
          "with why an applying agent must not write here")
    check("KEEP" not in UPD.LANDING,
          "and KEEP has no landing status, deliberately")

    print("\n5. at PRODUCTION an approval names both")
    live = plan(status="PRODUCTION")
    bare = UPD.apply_verdict(live, "UPDATE", implementation="x")
    reached.add(bare.get("refused"))
    check(bare["refused"] == "NOT_APPROVED", "without one it is refused")
    check(bare.get("asked") and "PRODUCTION" in bare["asked"],
          "and asks: %r" % bare["asked"])
    machine = UPD.apply_verdict(live, "UPDATE", implementation="x",
                                approval=approval(by="ci"))
    reached.add(machine.get("refused"))
    check(machine["refused"] == "APPROVED_BY_A_MACHINE",
          "a machine may not approve")
    check(UPD.NOT_A_PERSON is PRO.NOT_A_PERSON,
          "and the word list is HERON-LRN-PRO-004's object, not a copy")
    for wrong, what in ((approval(verdict="EXTEND"), "another verdict"),
                        (approval(fragment="something-else"),
                         "another fragment")):
        answer = UPD.apply_verdict(live, "UPDATE", implementation="x",
                                   approval=wrong)
        check(answer.get("refused") == "NOT_APPROVED",
              "an approval for %s is refused" % what)
    check("covers everything" in UPD.apply_verdict(
        live, "UPDATE", implementation="x",
        approval=approval(verdict="EXTEND"))["why"],
          "because one naming neither covers everything")
    right = UPD.apply_verdict(live, "UPDATE", implementation="x",
                              approval=approval())
    check(right["applied"] and right["status"] == "DRAFT",
          "the right approval goes through, and PRODUCTION still lands "
          "at DRAFT - the code moved either way")

    print("\n6. below PRODUCTION no approval is demanded")
    check(UPD.apply_verdict(plan(status="DRAFT"), "DEPRECATE")["applied"],
          "a DRAFT fragment needs none")
    check(any("inventing a gate" in line
              for line in proposal and UPD.apply_verdict(
                  proposal, "DEPRECATE")["unjudged"]),
          "and the answer says demanding one would be inventing a gate")
    check("docs/28 asks for one there and nowhere else"
          in " ".join(UPD.apply_verdict(proposal, "DEPRECATE")["unjudged"]),
          "there and nowhere else")

    print("\n7. nothing is written to disk")
    imports = sorted(line.split()[1] for line in logic.split("\n")
                     if line.startswith("import "))
    check(imports == ["heron_evolve", "heron_fragment", "heron_promotion",
                      "os", "sys"],
          "the import list is os, sys and three agents: %s"
          % ", ".join(imports))
    for writing in ("open(", "write(", "makedirs", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)
    check("Nothing was written to disk" in
          UPD.apply_verdict(plan(), "DEPRECATE")["why"],
          "and the answer says so")

    print("\n8. every declared failure is named and reached")
    for proposed, verdict, kwargs, name in (
            (None, "KEEP", {}, "NOTHING_TO_APPLY"),
            ({"considered": False}, "KEEP", {}, "NOT_A_PROPOSAL"),
            ("a string", "KEEP", {}, "NOT_A_PROPOSAL"),
            (plan(), "RETIRE", {}, "NOT_A_VERDICT"),
            (plan(), "", {}, "NOT_A_VERDICT"),
            (plan(), "UPDATE", {}, "NOTHING_TO_CHANGE"),
            (plan(), "EXTEND", {"implementation": "  "},
             "NOTHING_TO_CHANGE")):
        answer = UPD.apply_verdict(proposed, verdict, **kwargs)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-FRG-UPD-008.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 8, "the contract declares 8 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(kept["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a changed implementation is not still proven")
    return 0


if __name__ == "__main__":
    sys.exit(main())
