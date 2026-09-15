# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-PRO-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Learning promotion - one success is not proof, and a thousand is not either.

    python tests/test_promotion.py

WHAT IT PROVES
  1. D-30 SAYS WHAT THE AGENT SAYS IT SAYS, read out of DECISIONS.md -
     including "it passes a thousand".

  2. A THOUSAND SUCCESSFUL RUNS AND NO PROOF IS REFUSED, and the count
     is echoed back rather than ignored.

  3. A PROOF WITH NO NEGATIVE CASE IS REFUSED FIRST, before anything
     else the proof is missing.

  4. A MACHINE MAY NOT RECORD A PROOF - D-30 asks for a name, not a tick.

  5. THE LADDER IS heron_fragment's, and it only ever moves forward, one
     step at a time.

  6. docs/09 s94 STILL CARRIES THE GATE D-30 REPLACED, read out of it.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_promotion as PRO                                  # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_promotion.py"),
                    encoding="utf-8").read()
    code = whole.split("\nfrom __future__", 1)[1]
    logic = code.split("\ndef main(")[0]

    def ask(candidate, to, **kw):
        answer = PRO.promote(candidate, to, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    PROOF = {"positive": "returns the 47", "negative": "returns 0 when none",
             "at": "2026-09-15", "model": "Tower A MEP", "by": "Ajmal",
             "second_route": "checked by eye"}
    READY = {"id": "FRG-SEL-001", "status": "VALIDATED",
             "successful_runs": 1000}

    print("1. D-30 says what the agent says it says")
    decisions = " ".join(io.open(os.path.join(ROOT, "docs", "DECISIONS.md"),
                                 encoding="utf-8").read().split())
    check("A fragment is promoted by one recorded proof, not by a count of "
          "runs" in decisions, "D-30's own title")
    check("It passes a thousand" in decisions,
          "a fragment that succeeds while doing nothing passes ten runs - "
          "it passes a thousand")
    check("a proof without it is not a proof" in decisions,
          "and a proof without a negative case is not a proof")
    check("under their name and the date, not a tick" in decisions,
          "recorded under their name and the date, not a tick")
    check("**Not a count.**" in decisions, "and plainly: not a count")

    print("\n2. A thousand runs and no proof is refused")
    no_proof = ask(READY, "PROVEN")
    check(no_proof.get("refused") == "NO_PROOF",
          "1000 successful runs and no proof does not promote")
    check(no_proof["successful_runs"] == 1000,
          "and the count is echoed back: %d" % no_proof["successful_runs"])
    check("not the gate" in no_proof["why"],
          "with the reason: the count is not the gate")
    check("passes a thousand" in no_proof["why"].lower(),
          "quoting D-30's own sentence")
    with_proof = ask(READY, "PROVEN", proof=PROOF)
    check(with_proof["may_promote"] is True,
          "while ONE recorded proof does promote")
    check(with_proof["gate"] == "D-30's recorded proof",
          "and the gate is named")
    check(any("COUNTED FOR NOTHING" in line
              for line in with_proof["unjudged"]),
          "the answer says the runs counted for nothing even when it passes")

    print("\n3. A proof with no negative case is refused first")
    # WRONG THREE WAYS: no negative, no model, no date.
    worst = ask(READY, "PROVEN",
                proof={"positive": "x", "by": "Ajmal"})
    check(worst.get("refused") == "NO_NEGATIVE_CASE",
          "it is refused for the negative case, not for the others")
    check(sorted(worst["also_missing"]) == ["at", "model"],
          "and the others are still named: %s"
          % ", ".join(sorted(worst["also_missing"])))
    check("makes it not a proof" in worst["why"],
          "with the reason: the others make a proof incomplete and this "
          "one makes it not a proof")
    only_model = ask(READY, "PROVEN", proof=dict(PROOF, model=""))
    check(only_model.get("refused") == "AN_INCOMPLETE_PROOF",
          "while a proof missing only the model is merely incomplete")
    check(only_model["missing"] == ["model"], "and says which")
    for field, _why in PRO.A_PROOF_CARRIES:
        short = dict(PROOF)
        short[field] = ""
        answer = ask(READY, "PROVEN", proof=short)
        check(answer.get("refused") in ("NO_NEGATIVE_CASE",
                                        "AN_INCOMPLETE_PROOF"),
              "a proof with no `%s` is refused" % field)
    check(ask(READY, "PROVEN", proof=dict(PROOF, second_route=""))
          ["may_promote"] is True,
          "but a missing second route is NOT a refusal - D-30 asks for one "
          "where one exists")
    check(any("one fewer thing to judge it by" in line for line in
              ask(READY, "PROVEN", proof=dict(PROOF, second_route=""))
              ["unjudged"]),
          "and the answer says what its absence costs")

    print("\n4. A machine may not record a proof")
    for machine in PRO.NOT_A_PERSON:
        answer = ask(READY, "PROVEN", proof=dict(PROOF, by=machine))
        check(answer.get("refused") == "AN_INCOMPLETE_PROOF",
              "'%s' is not a person" % machine)
    check("not a tick" in ask(READY, "PROVEN",
                              proof=dict(PROOF, by="CI"))["why"].lower(),
          "quoting D-30: under their name and the date, not a tick")
    check("judge it rather than trust it" in ask(
        READY, "PROVEN", proof=dict(PROOF, by="bot"))["why"].lower(),
        "and why - a later reader must be able to judge it")

    print("\n5. The ladder is heron_fragment's")
    check(PRO.LADDER is FRAG.STATUSES,
          "the ladder IS heron_fragment's object, not a copy")
    check(PRO.NEEDS_PROOF is FRAG.NEEDS_PROOF,
          "and so is the list of states needing proof")
    for status in FRAG.STATUSES:
        check('"%s"' % status not in logic,
              "no literal '%s' in the agent" % status)
    check(ask({"status": "DRAFT"}, "TESTING")["may_promote"] is True,
          "a step below PROVEN needs no proof")
    check(ask({"status": "DRAFT"}, "TESTING")["gate"].startswith("no proof"),
          "and says so")
    check(ask({"status": "VALIDATED"}, "PRODUCTION").get("refused")
          == "SKIPS_A_STATE", "VALIDATED to PRODUCTION skips PROVEN")
    check("skipping the gate rather than passing it faster"
          in ask({"status": "VALIDATED"}, "PRODUCTION")["why"],
          "because each state has its own gate")
    for backwards in ("DRAFT", "VALIDATED", "PRODUCTION"):
        check(ask({"status": "PRODUCTION"}, backwards).get("refused")
              == "NOT_A_PROMOTION",
              "'%s' from PRODUCTION is not a promotion" % backwards)
    check(ask({"status": "ARCHIVED"}, "PRODUCTION").get("refused")
          == "NOT_A_PROMOTION",
          "and nothing moves out of ARCHIVED - docs/09 says never deleted")

    print("\n6. docs/09 still carries the gate D-30 replaced")
    skills = " ".join(io.open(os.path.join(ROOT, "docs",
                                           "09-skills-and-fragments.md"),
                              encoding="utf-8").read().split())
    check("N successful real executions" in skills,
          "docs/09 s94 still asks for N successful real executions")
    check("N to be set" in skills and "suggest 10" in skills,
          "with N still to be set, suggesting 10")
    check("Tracked as [Q-9]" in skills,
          "and still says tracked as Q-9")
    check("Answers:** [Q-9]" in decisions,
          "while D-30 answers Q-9")
    check("F21" in whole, "the agent records that as PROPOSALS F21")
    check(any("tracked as q-9" in line.lower()
              for line in with_proof["unjudged"]),
          "and every answer carries it")

    print("\n7. Every failure is named and reached")
    for bad in (None, "FRG-SEL-001", []):
        check(ask(bad, "PROVEN").get("refused") == "NOT_A_CANDIDATE",
              "%r is not a candidate" % (bad,))
    for status in ("RETIRED", "", "proven?"):
        check(ask({"status": status}, "PROVEN").get("refused")
              == "NOT_A_STATUS", "'%s' is not a status" % status)
        check(ask(READY, status).get("refused") == "NOT_A_STATUS",
              "  nor a destination")
    for writing in ("open(", "write(", "os.remove", "subprocess"):
        check(writing not in logic, "the agent never uses %s" % writing)
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-LRN-PRO-004.yaml"))
    named = contract.get("failures") or []
    check(len(named) == 7, "the contract declares 7 failures")
    for failure in named:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(with_proof["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    one recorded proof, and a thousand runs are not it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
