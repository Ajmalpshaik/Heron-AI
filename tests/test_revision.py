# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-SKL-UPD-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Skill revision - the breaking direction is not the same for every field.

    python tests/test_revision.py

WHAT IT PROVES
  1. THE DIRECTION IS RIGHT WAY ROUND, FIELD BY FIELD. Removing an
     utterance breaks and adding one does not; adding a precondition
     breaks and removing one does not. Getting this backwards is how an
     agent calls a harmless change dangerous and a dangerous one fine.

  2. RAISING RISK BREAKS AND LOWERING IT DOES NOT. docs/22 s64 - a
     permission boundary, not a display preference.

  3. AT PRODUCTION A BREAKING CHANGE IS PROPOSED, NOT APPLIED, and the
     file on disk is untouched. Below it the same change goes through
     and is reported.

  4. EVERY REVISION GOES BACK TO DRAFT, not only the breaking ones.

  5. A NO-OP IS REFUSED - the most destructive thing here would be
     knocking a PRODUCTION skill off the ladder for free.

  6. THE ID MAY NOT CHANGE, AND AN ARCHIVED SKILL MAY NOT COME BACK.

  7. WHAT IT WRITES PASSES HERON-SKL-VAL-004 CLEAN, with the step and
     version carried forward rather than reset.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_revision as UPD                                   # noqa: E402
import heron_skill as SKILL                                    # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_capability as CAP                                 # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def live(**changes):
    card = {"id": "tally-terminals", "name": "How many terminals",
            "domain": "revit.reporting", "purpose": "Counts them.",
            "utterances": ["how many air terminals", "count the terminals"],
            "needs": ["COUNT_ELEMENTS"],
            "preconditions": ["a document is open"],
            "risk": "MODIFY", "revit": ["2024", "2025"],
            "heron-status": "PRODUCTION", "heron-step": 14,
            "heron-since": "0.1.0", "heron-layer": "brain"}
    card.update(changes)
    return card


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_revision.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]
    where = tempfile.mkdtemp()

    try:
        print("\n1. the direction is right way round, field by field")
        # REMOVING an utterance breaks; ADDING one cannot.
        fewer = UPD.breaks(live(), live(utterances=["how many air terminals"]))
        check([one["field"] for one in fewer] == ["utterances"]
              and fewer[0]["direction"] == "removed",
              "removing an utterance breaks - whoever says it has nothing "
              "to say now")
        more = UPD.breaks(live(), live(utterances=live()["utterances"]
                                       + ["terminal count"]))
        check(more == [],
              "and ADDING one does not break anybody")
        # ADDING a precondition breaks; REMOVING one cannot.
        tighter = UPD.breaks(live(), live(preconditions=[
            "a document is open", "the view is not a sheet"]))
        check([one["field"] for one in tighter] == ["preconditions"]
              and tighter[0]["what"] == ["the view is not a sheet"],
              "adding a precondition breaks, and the answer names which")
        check(UPD.breaks(live(), live(preconditions=[])) == [],
              "and REMOVING one does not - the opposite direction from "
              "utterances, which is the whole reason it is written down")
        check(UPD.breaks(live(), live(needs=["COUNT_ELEMENTS",
                                             "MOVE_ELEMENTS"]))[0]["field"]
              == "needs",
              "adding a need breaks - the new capability may have no "
              "provider")
        check(UPD.breaks(live(), live(revit=["2024"]))[0]["direction"]
              == "removed",
              "and dropping a Revit release breaks whoever is on it")

        print("\n2. raising risk breaks, lowering does not")
        up = UPD.breaks(live(), live(risk="ADMIN"))
        check([one["field"] for one in up] == ["risk"]
              and up[0]["what"] == ["MODIFY", "ADMIN"],
              "raising MODIFY to ADMIN breaks, and both values come back")
        check("permission boundary" in up[0]["why"],
              "with docs/22 s64's reason: a permission boundary is not a "
              "display preference")
        check(UPD.breaks(live(), live(risk="READ")) == [],
              "and LOWERING it breaks nobody")

        print("\n3. at PRODUCTION it is proposed, not applied")
        first = UPD.revise(live(), {"name": "Terminal count"}, into=where)
        check(first["revised"], "a harmless revision goes through")
        before = io.open(first["path"], encoding="utf-8").read()
        blocked = UPD.revise(live(), {"risk": "ADMIN"}, into=where)
        reached.add(blocked.get("refused"))
        check(blocked["refused"] == "BREAKS_CALLERS",
              "a breaking revision to a PRODUCTION skill is refused")
        check(blocked["broke"] and blocked["broke"][0]["field"] == "risk",
              "and it comes back with WHAT broke, not just that something "
              "did")
        check("pull request" in blocked["propose"],
              "pointing at docs/09 s118 - a proposal, in practice a pull "
              "request")
        check(io.open(first["path"], encoding="utf-8").read() == before,
              "and the file on disk is byte-for-byte unchanged")
        # BELOW PRODUCTION THERE ARE NO CALLERS TO BREAK.
        drafted = UPD.revise(live(**{"heron-status": "DRAFT"}),
                             {"risk": "ADMIN"}, into=where)
        check(drafted["revised"] and drafted["broke"],
              "the SAME change at DRAFT is applied - and still reported "
              "as breaking")
        check(any("no callers to break" in line
                  for line in drafted["unjudged"]),
              "with the reason it was allowed")

        print("\n4. every revision goes back to DRAFT")
        check(first["was"] == "PRODUCTION" and first["status"] == "DRAFT",
              "a harmless rename knocks PRODUCTION back to DRAFT")
        check(first["broke"] == [],
              "and nothing broke - so it is not the breakage that did it")
        check(first["card"]["heron-status"] == "DRAFT",
              "the written card says DRAFT")
        check(UPD.ENTERS_AT in FRAG.STATUSES,
              "and DRAFT is read off docs/09's ladder, not typed here")

        print("\n5. a no-op is refused")
        same = UPD.revise(live(), {"name": live()["name"]}, into=where)
        reached.add(same.get("refused"))
        check(same["refused"] == "NOTHING_CHANGED",
              "setting a field to the value it already has is not a "
              "revision")
        check("destructive" in same["why"],
              "because applying it would knock a PRODUCTION skill off the "
              "ladder for free, wearing the clothes of housekeeping")

        print("\n6. the id may not change, and ARCHIVED does not come back")
        renamed = UPD.revise(live(), {"id": "something-else"}, into=where)
        reached.add(renamed.get("refused"))
        check(renamed["refused"] == "NOT_A_REVISION",
              "a rename is authoring a second skill, not revising this one")
        gone = UPD.revise(live(**{"heron-status": "ARCHIVED"}),
                          {"risk": "READ"}, into=where)
        reached.add(gone.get("refused"))
        check(gone["refused"] == "NOT_REVISABLE",
              "an ARCHIVED skill is not revived as a DRAFT")

        print("\n7. what it writes passes HERON-SKL-VAL-004 clean")
        written = SKILL.load(first["path"])
        problems = SKILL.validate(written)
        check(not problems,
              "the validator finds nothing%s"
              % ("" if not problems else ": %s" % "; ".join(problems)))
        check(written.data["heron-step"] == 14
              and written.data["heron-since"] == "0.1.0",
              "the step and version are CARRIED FORWARD from the card it "
              "revised, not reset to this agent's own")
        check(set(type(one).__name__
                  for one in written.data["revit"]) == {"str"},
              "and `revit` is still strings after the round trip")

        print("\n8. every declared failure is named and reached")
        check(UPD.RISK is CAP.RISK_ORDER and UPD.VERSIONS
              is FRAG.REVIT_VERSIONS,
              "the ladders are the same objects, not copies")
        for existing, changes, name in (
                (None, {"risk": "READ"}, "NO_SUCH_SKILL"),
                (live(), None, "NOTHING_TO_APPLY"),
                ({"id": "thin"}, {"risk": "READ"}, "INCOMPLETE"),
                (live(), {"revit": ["2028"]}, "NOT_A_VERSION"),
                (live(), {"risk": "DELETE"}, "NOT_A_RISK")):
            answer = UPD.revise(existing, changes, into=where)
            reached.add(answer.get("refused"))
            check(answer.get("refused") == name, "%s is reached" % name)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-SKL-UPD-003.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 9, "the contract declares 9 failures")
        for failure in named:
            check(failure in logic, "the code names %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(first["unjudged"]) == 4, "four things are left unjudged")
    finally:
        shutil.rmtree(where)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    back to DRAFT, and PRODUCTION is not broken quietly")
    return 0


if __name__ == "__main__":
    sys.exit(main())
