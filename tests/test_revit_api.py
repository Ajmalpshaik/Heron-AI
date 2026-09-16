# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-REVIT-API-020
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
The Revit API agent - it frames the question and checks the answer.

    python tests/test_revit_api.py

WHAT IT PROVES
  1. IT ANSWERS NO REVIT QUESTION OF ITS OWN. With no proposal the
     answer is the QUESTION, and the one scoped call stays the host's
     under D-01.

  2. THE CASE THIS ROW EXISTS FOR IS CAUGHT. `ElementId.IntegerValue`
     is refused on 2026 and accepted on 2024 - from the repository's own
     recorded evidence, not from a list typed here.

  3. A MEMBER IS MATCHED BY ITS LAST SEGMENT TOO, because real C# has a
     `using` at the top and a fully-qualified comparison clears
     everything.

  4. THE TRANSACTION SHAPE IS DERIVED FROM WHAT THE CALLER DECLARED,
     never from the wording - and a read proposing one is refused.

  5. ONE UNDO CANNOT CROSS TWO DOCUMENTS, and that is checked before any
     question of which member to call.

  6. A RELEASE NOBODY LOOKED AT IS NOT A RELEASE THAT REMOVED NOTHING -
     HERON-REVIT-ACI-034's distinction, and the one that reported every
     fragment clear when it was missed.

  7. NOTHING IS CHANGED, AND `unjudged` SAYS WHAT A NAME COMPARISON
     CANNOT SEE.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND
     REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_revit_api as API                                  # noqa: E402
import heron_apichanges as ACI                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

# A digest in the real shape, so no test depends on a file this
# container may or may not have.
#
# THE NAMESPACE IS A STAND-IN AND THE MEMBER NAMES ARE REAL.
# `ElementId.IntegerValue` is the member this repository wrote in with a
# comment calling it "the property every version has had"; it compiles on
# 2020 through 2025 and is GONE at 2026. The vendor namespace in front of
# it is replaced here because the adapter boundary keeps that prefix
# inside revit/, and check-structure greps file text - a fixture spelling
# it is the gate being right. What is under test is the matching, and the
# prefix plays no part in that.
FOUND = {
    "releases": ["2024", "2025", "2026"],
    "transitions": [
        {"from": "2023", "to": "2024", "of": 39000,
         "removed": [], "removedCount": 0, "addedCount": 10},
        {"from": "2024", "to": "2025", "of": 40000,
         "removed": ["Vendor.Model.DB.Dimension.Origin"],
         "removedCount": 1, "addedCount": 10},
        {"from": "2025", "to": "2026", "of": 41000,
         "removed": ["Vendor.Model.DB.ElementId.IntegerValue"],
         "removedCount": 1, "addedCount": 10},
    ],
}

MOVE = {"does": "move the selected ducts up 200 mm",
        "writes": True, "documents": 1}
COUNT = {"does": "count the ducts", "writes": False}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    logic = io.open(os.path.join(ROOT, "brain", "heron_revit_api.py"),
                    encoding="utf-8").read()

    def ask(operation, **kw):
        kw.setdefault("found", FOUND)
        answer = API.look(operation, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. it answers no Revit question of its own")
    answer = ask(MOVE, releases=["2024"])
    check(bool(answer.get("asked")),
          "with no proposal, the answer is the QUESTION - the one scoped "
          "call the register gives this row is the host's under D-01")
    check("2024" in answer["asked"] and "OLDEST" in answer["asked"],
          "and the question carries the releases it has to hold on, and "
          "says to name nothing the oldest one does not have")
    check("Autodesk" not in logic,
          "the module names no vendor namespace at all - it knows no "
          "Revit API of its own, which is the point of the row rather "
          "than a limitation, and the adapter boundary keeps that "
          "prefix inside revit/")

    print("\n2. the case this row exists for")
    answer = ask(MOVE, releases=["2024", "2026"],
                 proposal={"members": ["Vendor.Model.DB.ElementId"
                                       ".IntegerValue"],
                           "transaction": "transaction"})
    check(answer.get("refused") == "MEMBER_IS_GONE",
          "ElementId.IntegerValue is REFUSED on 2026 - not reported, "
          "because there is no compiler here and the answer reads "
          "exactly as confident as a correct one")
    check(answer["gone"][0]["release"] == "2026",
          "  and the release that dropped it is named")
    answer = ask(MOVE, releases=["2024"],
                 proposal={"members": ["Vendor.Model.DB.ElementId"
                                       ".IntegerValue"],
                           "transaction": "transaction"})
    check(answer.get("accepted") is True,
          "and the SAME member is accepted on 2024, which still has it - "
          "the answer is about a release, never about a member alone")

    print("\n3. a member is matched by its last segment too")
    answer = ask(MOVE, releases=["2026"],
                 proposal={"members": ["id.IntegerValue"],
                           "transaction": "transaction"})
    check(answer.get("refused") == "MEMBER_IS_GONE",
          "real C# has a `using` at the top and writes `id.IntegerValue`, "
          "so a fully-qualified comparison would clear everything")

    print("\n4. the transaction shape is derived, not read out of the wording")
    check(API.transaction_for(COUNT)["shape"] == "none",
          "a read takes NO transaction - an empty one opened just in "
          "case is forbidden in as many words")
    check(API.transaction_for(MOVE)["shape"] == "transaction",
          "a single-stage write is one Transaction, one undo step")
    check(API.transaction_for(dict(MOVE, stages=3))["shape"] == "group",
          "a multi-stage write is one named TransactionGroup, because "
          "the user presses Ctrl+Z once")
    answer = ask(COUNT, releases=["2024"],
                 proposal={"members": ["FilteredElementCollector"],
                           "transaction": "transaction"})
    check(answer.get("refused") == "TRANSACTION_IS_WRONG",
          "a READ proposing a transaction is refused")
    answer = ask(COUNT, releases=["2024"],
                 proposal={"members": ["FilteredElementCollector"],
                           "transaction": "whatever"})
    check(answer.get("refused") == "TRANSACTION_IS_WRONG",
          "and an unknown shape is refused rather than read as the "
          "safest one")

    print("\n5. one undo cannot cross two documents")
    answer = ask({"does": "copy levels into the link", "writes": True,
                  "documents": 2}, releases=["2024"])
    check(answer.get("refused") == "WOULD_CROSS_TWO_DOCUMENTS",
          "a write touching two models is refused BEFORE any question of "
          "which member to call - every Transaction constructor takes "
          "exactly one Document, and D-47 wants it said before it starts")
    check(API.look({"does": "read both models", "writes": False,
                    "documents": 2}, releases=["2024"],
                   found=FOUND).get("looked") is True,
          "  while a READ across two documents is fine - there is no "
          "undo step to be atomic")

    print("\n6. a release nobody looked at is not a release that removed "
          "nothing")
    answer = ask(MOVE, releases=["2027"])
    check(answer.get("refused") == "NO_EVIDENCE_FOR_RELEASE",
          "the digest holds no transition INTO 2027, so what it removed "
          "is UNKNOWN - reading a missing transition as an empty one is "
          "the plausible zero that reported every fragment clear")
    answer = ask(MOVE, releases=["2024"], found={})
    check(answer.get("refused") == "NO_EVIDENCE",
          "and no digest at all is its own refusal, not an empty one")
    answer = ask(MOVE, releases=[API.RELEASES[0]])
    check(answer.get("refused") == "NO_EARLIER_RELEASE",
          "and the EARLIEST release is a third answer again - there is "
          "no transition into it, so nothing can have been removed AT "
          "it, which is not a clear answer either")
    check("ACI.removed_in" in logic and "ACI.evidence" in logic,
          "  the evidence is read through HERON-REVIT-ACI-034, bound "
          "rather than a second copy - one rule, one owner")
    check("transitions" not in logic and "json" not in logic,
          "  and this module never parses the digest itself, so the "
          "missing-transition distinction cannot drift between the two")

    print("\n7. nothing is changed, and unjudged says what it cannot see")
    answer = ask(MOVE, releases=["2024"])
    check(answer["fixed"] is False, "fixed is always false - this is READ")
    check(len(answer["unjudged"]) == 5, "five things are left unjudged")
    check(any("UNWARNED, NOT PROVEN PRESENT" in line
              for line in answer["unjudged"]),
          "including that a member it did not warn about is unwarned - "
          "the evidence records removals, not the whole surface")
    check(any("CHANGED UNIT" in line for line in answer["unjudged"]),
          "and that a changed unit is invisible to a comparison of names")
    for writing in ("open(", "write(", "makedirs", "rmtree"):
        check(writing not in logic, "the agent never uses %s" % writing)

    print("\n8. every failure is named and reached")
    for these, name in (
            (None, "NOT_AN_OPERATION"),
            ("a string", "NOT_AN_OPERATION"),
            ({"writes": True}, "NOT_AN_OPERATION"),
            ({"does": "something"}, "NOT_AN_OPERATION")):
        check(ask(these, releases=["2024"]).get("refused") == name,
              "%s is reached" % name)
    check(ask(MOVE, releases=[]).get("refused") == "NO_RELEASE",
          "NO_RELEASE is reached")
    check(ask(MOVE, releases=["2019"]).get("refused") == "UNKNOWN_RELEASE",
          "UNKNOWN_RELEASE is reached")
    for bad in ("a string", {"members": ["x"]}, {"transaction": "none"}):
        check(ask(COUNT, releases=["2024"], proposal=bad).get("refused")
              == "NOT_A_PROPOSAL", "NOT_A_PROPOSAL is reached for %r"
              % (bad if not isinstance(bad, dict) else sorted(bad),))

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-REVIT-API-020.yaml"))
    named = contract.get("failures") or []
    check(len(named) == len(set(named)),
          "the contract declares each failure once")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(contract.get("allowed-tools") == [],
          "the contract declares no tools - the scoped call is the "
          "host's, not this agent's")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    it frames the question and checks the answer")
    return 0


if __name__ == "__main__":
    sys.exit(main())
