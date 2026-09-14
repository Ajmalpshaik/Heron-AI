# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-EVL-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Evaluator - a correct refusal is not a failure, and a count is not
evidence.

    python tests/test_evaluator.py

WHAT IT PROVES
  1. A FAILURE STATE THE CONTRACT DECLARES IS NOT A DEFECT. It is counted
     separately and lowers nothing. HERON-AHR-GAP-001 found the loudest
     error in the real audit trail - 38 of 176 - was the executor behaving
     correctly, and scoring that as a failure teaches the next version to
     attempt what it should decline.

  1b. AND IT IS NOT AUTOMATICALLY CORRECT EITHER. A contract's `failures`
     list says which outcomes a CALLER must handle; it does not say the
     refusal condition was present on that request. A broken agent that
     refuses everything with its own declared state produced a perfect
     report until 2026-09-14. A refusal is VERIFIED only when the run says
     why it was warranted, and when every scored run is an unverified
     refusal the report says so in as many words.

  2. A FAILURE STATE THE CONTRACT NEVER DECLARED IS A DEFECT, and the
     refusal names what WAS declared so the reader can judge which is wrong.

  3. THE EXPECTATION IS THE CONTRACT, QUOTED. Not a target a caller passed:
     there is no argument for a timeout or for a pass mark.

  4. A DEGRADED RUN IS COUNTED AND EXCLUDED, never dropped and never scored.
     A fallback answering is evidence about the fallback.

  5. ALL-DEGRADED IS NOT A SCORE OF ZERO, and says so.

  6. IT SCORES ONLY AFTER ACTIVATION, and the stage is read from the
     register rather than taken from the caller.

  7. NO RUNS IS UNMEASURED, NOT ZERO.

  8. A RUN THAT CANNOT SAY HOW IT ENDED IS REFUSED, not guessed at.

  9. IT NEVER SAYS THE AGENT IS GOOD.

 10. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED
     BY THIS SUITE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_evaluator as EVL                                 # noqa: E402
import heron_agents as REG                                    # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []

AGENT = "HERON-AHR-WFP-015"


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    records = REG.records()
    live = dict((k, dict(v)) for k, v in records.items())
    live[AGENT]["state"] = "PRODUCTION"
    declared = sorted(records[AGENT]["failures"])
    reached = set()

    def ask(runs, agent_id=AGENT, **kw):
        kw.setdefault("records", live)
        answer = EVL.score(agent_id, runs, **kw)
        if "refused" in answer:
            reached.add(answer["refused"])
        return answer

    print("1. A declared failure state is the agent working")
    check(declared, "%s declares %d failure state(s)" % (AGENT,
                                                         len(declared)))
    answer = ask([{"run": "a", "outcome": declared[0]},
                  {"run": "b", "outcome": "OK"}])
    report = answer["report"]
    check([r["run"] for r in report["declared_refusals"]] == ["a"],
          "the declared refusal is counted as a declared refusal")
    check(report["defects"] == [], "and not as a defect")
    check(report["completed"] == ["b"], "the OK run is counted separately")

    print()
    print("1b. A declared refusal is not automatically a correct one")
    check(report["verified_refusals"] == [],
          "with nothing saying it was warranted it is NOT verified")
    check([r["run"] for r in report["unverified_refusals"]] == ["a"],
          "it lands in unverified - not a defect, not a credit")
    check(any("NOTHING SAYS THE REFUSAL WAS WARRANTED" in n
              for n in answer["unjudged"]),
          "and the report says so rather than leaving it to be noticed")
    warranted = ask([{"run": "a", "outcome": declared[0],
                      "refusal-warranted": True,
                      "because": "the proposal had no name"},
                     {"run": "b", "outcome": "OK"}])
    check([r["run"] for r in warranted["report"]["verified_refusals"]]
          == ["a"],
          "a run recording WHY it refused is verified")
    check(any("not failing" in n for n in warranted["unjudged"]),
          "and only then does the report say it is the agent working")
    # THE BROKEN AGENT THAT REFUSES EVERYTHING.
    always = ask([{"run": "r%d" % n, "outcome": declared[0]}
                  for n in range(10)])
    check(always["report"]["defects"] == [],
          "an agent that refuses every request records no defect...")
    check(any("EVERY SCORED RUN WAS AN UNVERIFIED REFUSAL" in n
              for n in always["unjudged"]),
          "...and the report refuses to be read as health")

    print()
    print("2. An undeclared failure state is a defect")
    answer = ask([{"run": "a", "outcome": "KeyError"}])
    report = answer["report"]
    check([d["run"] for d in report["defects"]] == ["a"],
          "a state the contract never declared is a defect")
    check(report["defects"][0]["declared"] == declared,
          "and the defect carries what WAS declared, to judge which is wrong")
    check(report["declared_refusals"] == [],
          "it is not quietly counted as a refusal")

    print()
    print("3. The expectation is the contract, quoted")
    import inspect
    names = set(inspect.signature(EVL.score).parameters)
    for forbidden in ("timeout", "target", "pass_mark", "expected"):
        check(forbidden not in names,
              "score() has no '%s' parameter - the contract is the promise"
              % forbidden)
    answer = ask([{"run": "a", "outcome": "OK"}])
    expectation = answer["report"]["expectation"]
    check(expectation["failures"] == declared,
          "the expectation's failures are the contract's, unchanged")
    check(expectation["timeout-seconds"]
          == float(records[AGENT]["timeout_seconds"]),
          "and its timeout is the contract's, unchanged")
    promised = expectation["timeout-seconds"]
    answer = ask([{"run": "slow", "outcome": "OK", "seconds": promised + 1},
                  {"run": "fine", "outcome": "OK", "seconds": promised}])
    over = answer["report"]["overran_timeout"]
    check([o["run"] for o in over] == ["slow"],
          "a run over the promised timeout is named; one exactly at it is not")
    check(over[0]["promised"] == promised,
          "and the promise it broke is quoted beside it")

    print()
    print("4. A degraded run is counted and excluded")
    answer = ask([{"run": "a", "outcome": "OK"},
                  {"run": "b", "outcome": "KeyError", "degraded": True}])
    report = answer["report"]
    check(report["degraded_excluded"] == ["b"], "the degraded run is named")
    check(report["defects"] == [],
          "and its failure is NOT scored against the agent")
    check(report["scored"] == 1 and report["runs"] == 2,
          "2 runs, 1 scored - both numbers reported, neither hidden")
    check(any("about the fallback" in n for n in answer["unjudged"]),
          "and the reason is the availability rule, named")

    print()
    print("5. All degraded is not a score of zero")
    answer = ask([{"run": "a", "outcome": "OK", "degraded": True}])
    check(answer["report"]["scored"] == 0,
          "nothing was scored")
    check(any("not a score of zero" in n for n in answer["unjudged"]),
          "and the report says that is not a zero")

    print()
    print("6. It scores only after activation, and the stage is read")
    for forbidden in ("stage", "state", "activated"):
        check(forbidden not in names,
              "score() has no '%s' parameter a caller could assert"
              % forbidden)
    for stage in ("DRAFT", "TESTING", "VALIDATED", "SHADOW", "PROVEN"):
        world = dict((k, dict(v)) for k, v in records.items())
        world[AGENT]["state"] = stage
        answer = ask([{"run": "a", "outcome": "OK"}], records=world)
        check(answer.get("refused") == "NOT_ACTIVATED",
              "an agent at %s is refused - PROVEN is not activated" % stage)
    check(EVL.ACTIVATED_STAGE == "PRODUCTION",
          "and PRODUCTION is the only stage that is")

    print()
    print("7. No runs is unmeasured, not zero")
    for empty in ([], None):
        answer = ask(empty)
        check(answer.get("refused") == "NOTHING_TO_SCORE",
              "no runs is refused rather than reported as 0")
    check("would read as a result" in ask([])["why"],
          "and the refusal says why zero would mislead")

    print()
    print("8. A run that cannot say how it ended is refused")
    for bad in ([{"run": "a"}], [{"outcome": "OK"}], ["a run"], [None]):
        answer = ask(bad)
        check(answer.get("refused") == "RUNS_NOT_RECORDS",
              "%r is refused, not guessed at" % (bad[0],))

    print()
    print("9. It never says the agent is good")
    answer = ask([{"run": "a", "outcome": "OK"}])
    source = open(os.path.join(ROOT, "brain", "heron_evaluator.py"),
                  encoding="utf-8").read()
    for word in ("grade", "pass_mark", "percentage", "rating"):
        check(word not in answer["report"],
              "the report has no '%s'" % word)
    check(any("WHETHER THE ANSWERS WERE RIGHT" in n
              for n in answer["unjudged"]),
          "and every report says correctness is not in a run record")
    check("good" not in answer["why"].lower(),
          "the sentence it returns counts, it does not praise")

    print()
    print("10. Every declared failure is named by the code and reached here")
    world = dict((k, dict(v)) for k, v in records.items())
    world[AGENT] = dict(world[AGENT], state="PRODUCTION", contract=None,
                        failures=None)
    answer = ask([{"run": "a", "outcome": "OK"}], records=world)
    check(answer.get("refused") == "NO_CONTRACT",
          "an agent with no contract has promised nothing to measure")
    check(ask([{"run": "a", "outcome": "OK"}],
              agent_id="HERON-AHR-NOPE-999").get("refused") == "NO_SUCH_AGENT",
          "an agent nobody planned is refused")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-EVL-007.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached - {"REGISTER_UNREADABLE"})
    check(not unreached,
          "and every state but REGISTER_UNREADABLE was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    saying no when it should is not a mark against it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
