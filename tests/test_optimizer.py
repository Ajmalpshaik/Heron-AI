# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-OPT-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Optimizer - it will not move the line it is measured by.

    python tests/test_optimizer.py

WHAT IT PROVES
  1. IT WILL NOT PROPOSE DECLARING A DEFECT'S STATE AS A FAILURE. That
     change clears the defect permanently without a line of the agent
     changing - the same failure reads as a CORRECT REFUSAL in the next
     report. The bug would not be fixed, it would be promised.

  2. IT WILL NOT PROPOSE RAISING A TIMEOUT PAST THE RUNS THAT BROKE IT. Same
     shape, one field along. R-55 is the same rule for a retrieval
     threshold.

  3. BOTH REFUSALS ARE VISIBLE, NOT SILENT. A rule nobody can see is a rule
     nobody can check, and both say the change may still be right and is a
     person's to make.

  4. IT PROPOSES THE REAL FIX INSTEAD, naming the run and the state.

  5. IT PROPOSES FROM EVIDENCE, NOT TASTE. No report, a report from another
     agent, or a report that did not come from the Evaluator is refused.

  6. A CLEAN REPORT PRODUCES NOTHING. Proposing something anyway is how a
     working agent gets rewritten.

  7. EVERY PROPOSED CONTRACT CHANGE CARRIES ITS COMPATIBILITY VERDICT.

  8. NOTHING IS WRITTEN AND NOTHING IS APPLIED. Risk SUGGEST.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED
     BY THIS SUITE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_optimizer as OPT                                 # noqa: E402
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
    declared = sorted(records[AGENT]["failures"])
    promised = float(records[AGENT]["timeout_seconds"])
    reached = set()

    clean = {"agent": AGENT, "runs": 3, "scored": 3,
             "completed": ["a", "b", "c"], "declared_refusals": [],
             "defects": [], "overran_timeout": [], "degraded_excluded": [],
             "expectation": {"timeout-seconds": promised,
                             "failures": declared}}

    def ask(report, agent_id=AGENT):
        answer = OPT.propose(agent_id, report, records=records)
        if "refused" in answer:
            reached.add(answer["refused"])
        return answer

    print("1. It will not propose promising a defect away")
    with_defect = dict(clean, runs=4,
                       defects=[{"run": "r5", "state": "KeyError",
                                 "declared": declared}])
    answer = ask(with_defect)
    refused = " ".join(r["change"] for r in answer["refused_to_propose"])
    check("add 'KeyError' to the contract's `failures`" in refused,
          "declaring the defect's state is named as a refused change")
    proposed = " ".join(p["change"] for p in answer["proposals"])
    check("failures" not in proposed,
          "and no proposal touches `failures`")
    reason = " ".join(r["why"] for r in answer["refused_to_propose"])
    check("would not be fixed, it would be promised" in reason,
          "the reason is stated: the bug would be promised, not fixed")

    print()
    print("2. It will not propose raising a timeout past the runs that broke")
    slow = dict(clean, runs=4,
                overran_timeout=[{"run": "r2", "seconds": promised + 11,
                                  "promised": promised}])
    answer = ask(slow)
    refused = " ".join(r["change"] for r in answer["refused_to_propose"])
    check("raise `timeout-seconds`" in refused,
          "raising the timeout is named as a refused change")
    check(str(promised + 11) in refused,
          "and the number it would have been raised to is quoted")
    proposed = " ".join(p["change"] for p in answer["proposals"])
    check("timeout" not in proposed.lower(),
          "and no proposal raises it")

    print()
    print("3. Both refusals are visible, and neither claims to be the answer")
    both = dict(clean, runs=6,
                defects=[{"run": "r5", "state": "KeyError",
                          "declared": declared}],
                overran_timeout=[{"run": "r2", "seconds": promised + 11,
                                  "promised": promised}])
    answer = ask(both)
    check(len(answer["refused_to_propose"]) == 2,
          "both refused changes are listed, not silently omitted")
    for entry in answer["refused_to_propose"]:
        check("may well be right" in entry["why"]
              or "may still be right" in entry["why"]
              or "genuinely belongs" in entry["why"],
              "'%s' says it may still be right" % entry["change"][:44])
        check("person" in entry["why"],
              "and that it is a person's call")
    check(any("REFUSED rather than proposed" in n
              for n in answer["unjudged"]),
          "and the count is raised in unjudged, not left to be noticed")

    print()
    print("4. It proposes the real fix instead")
    answer = ask(with_defect)
    fix = answer["proposals"][0]
    check("handle KeyError in the implementation" in fix["change"],
          "the proposal is to handle it in the code")
    check("r5" in fix["why"] and "KeyError" in fix["why"],
          "and it names the run and the state it came from")

    print()
    print("5. It proposes from evidence, not taste")
    for bad, why in ((None, "no report"), ("a report", "a string"),
                     (42, "a number")):
        check(ask(bad).get("refused") == "NO_REPORT",
              "%s is refused" % why)
    check(ask({"agent": AGENT}).get("refused") == "NOT_AN_EVALUATION",
          "a dict missing the Evaluator's fields is refused")
    check(ask(dict(clean, agent="HERON-AHR-VAL-013")).get("refused")
          == "REPORT_IS_NOT_THIS_AGENT",
          "another agent's report is refused - a fix would land in the "
          "wrong file")
    check(ask(clean, agent_id="HERON-AHR-NOPE-999").get("refused")
          in ("NO_SUCH_AGENT", "REPORT_IS_NOT_THIS_AGENT"),
          "an agent nobody planned is refused")
    check(ask(dict(clean, agent="HERON-AHR-NOPE-999"),
              agent_id="HERON-AHR-NOPE-999").get("refused")
          == "NO_SUCH_AGENT",
          "and so is one whose own report names it")
    check(OPT.propose("", clean).get("refused") == "NO_SUCH_AGENT",
          "no agent at all is refused before anything is read")

    print()
    print("6. A clean report produces nothing")
    answer = ask(clean)
    check(answer.get("refused") == "NOTHING_TO_IMPROVE",
          "no defects and nothing slow means nothing to propose")
    check("gets rewritten" in answer["why"],
          "and the refusal says why proposing anyway is a cost")

    print()
    print("7. Every proposed contract change carries its verdict")
    repeated = dict(clean, runs=5,
                    declared_refusals=[{"run": "r3", "state": declared[0]},
                                      {"run": "r4", "state": declared[0]}],
                    defects=[{"run": "r5", "state": "KeyError",
                              "declared": declared}])
    answer = ask(repeated)
    # A RETRY IS THE THIRD CHANGE IT REFUSES TO PROPOSE, and Constitution
    # article 25 is why: do not retry a genuine failure. A declared refusal
    # is genuine until something proves the operation never ran, and nothing
    # in a run record proves that - D-21 makes the classification a table's
    # job and makes it fail closed.
    refused = " ".join(r["change"] for r in answer["refused_to_propose"])
    check("`retry.on-failures`" in refused,
          "a retry for a repeated refusal is refused, not proposed")
    reason = " ".join(r["why"] for r in answer["refused_to_propose"])
    check("article 25" in reason and "D-21" in reason,
          "and the refusal cites article 25 and D-21 rather than taste")
    proposed = " ".join(p["change"] for p in answer["proposals"])
    check("retry" not in proposed,
          "and no proposal offers to try the operation again")
    check("look at why" in proposed,
          "what it proposes instead is reading why it keeps happening")
    plain = [p for p in answer["proposals"] if not p["contract-change"]]
    check(all(p["compatibility"] is None for p in plain),
          "and a proposal that changes no contract claims no verdict")
    check(not [p for p in answer["proposals"] if p["contract-change"]],
          "no proposal on this path carries a contract change at all")

    print()
    print("8. Nothing is written and nothing is applied")
    source = open(os.path.join(ROOT, "brain", "heron_optimizer.py"),
                  encoding="utf-8").read()
    for call in ("open(", "makedirs", "json.dump", "yaml.dump", ".write(",
                 "os.replace", "shutil"):
        check(call not in source,
              "the source has no %s - risk SUGGEST proposes, it does not act"
              % call)

    print()
    print("9. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-OPT-009.yaml"))
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
    print("PASS    the standard is not the optimizer's to move")
    return 0


if __name__ == "__main__":
    sys.exit(main())
