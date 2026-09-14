# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-DEP-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Deployment - each gate refuses what it is for, and no machine signs.

    python tests/test_deployment.py

WHAT IT PROVES
  1. EVERY GATE IN docs/24 REFUSES WITHOUT ITS OWN EVIDENCE - an
     implementation, a test, a matrix, a shadow plan, ten real runs, a
     signature - and each refusal names which gate.

  2. A MACHINE MAY NOT SIGN. Golden Rule 7 is not satisfied by a different
     agent signing; only by a person. Any approver that looks like a Heron
     agent id is refused.

  3. PRODUCTION WITHOUT A SIGNATURE IS REFUSED, because docs/24 says human
     approval, explicit and recorded.

  4. NO STAGE MAY BE SKIPPED, and going back down is not deployment's to do -
     that is retirement.

  5. ABSENT EVIDENCE IS A REFUSAL AND NEVER A QUIET DOWNGRADE. An agent asked
     for PROVEN and given nothing does not silently become SHADOW: somebody
     asked for the wrong thing and needs telling.

  6. NOTHING IS WRITTEN. The module contains no file write at all - the status
     lives in the implementing file's header, and a machine editing its own
     promotion is what D-30 forbids.

  7. THE RECORD CARRIES WHAT THE AUDIT LOG NEEDS: the agent, both stages, who
     signed, what was offered, when - and `applied: False`, because allowing a
     move is not making it.

  8. A VALIDATION REFUSAL STOPS EVERYTHING ABOVE DRAFT.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_deployment as DEP                                # noqa: E402

FAILURES = []

PASSED = {"verdict": "PASS",
          "files": ["brain/heron_events.py", "tests/test_events.py"]}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    agent = "HERON-KRN-EVT-004"

    print("1. Each gate refuses without its own evidence")
    cases = [
        ("DRAFT", {}, {"verdict": "PASS", "files": []}, "an implementation"),
        ("TESTING", {}, {"verdict": "PASS",
                         "files": ["brain/heron_events.py"]}, "a test"),
        ("VALIDATED", {}, PASSED, "a matrix"),
        ("SHADOW", {}, PASSED, "a shadow plan"),
        ("PROVEN", {"real-runs": 3}, PASSED, "ten real runs"),
        ("DISCOVERED", {}, PASSED, "a stated purpose"),
    ]
    for stage, evidence, validation, what in cases:
        answer = DEP.activate(agent, stage, evidence=evidence,
                              validation=validation)
        check(not answer["activated"] and answer["refused"] == "GATE_NOT_MET",
              "%s without %s is refused" % (stage, what))
        check(stage in answer["why"],
              "and the refusal names the gate it was for")

    print()
    print("   ... and each one passes when its evidence is there")
    for stage, evidence in (("DISCOVERED", {"purpose": "to notify"}),
                            ("DRAFT", {}),
                            ("TESTING", {}),
                            ("VALIDATED", {"matrix": "3.11 on linux"}),
                            ("SHADOW", {"shadow-plan": "beside the log"}),
                            ("PROVEN", {"real-runs": 12})):
        answer = DEP.activate(agent, stage, evidence=evidence,
                              validation=PASSED)
        check(answer["activated"], "%s is allowed once its gate is met" % stage)

    print()
    print("2 and 3. Who may sign")
    answer = DEP.activate(agent, "PRODUCTION", validation=PASSED)
    check(answer["refused"] == "NEEDS_HUMAN_APPROVAL",
          "PRODUCTION without a signature is refused")
    for machine in ("HERON-AHR-CRT-006", "heron-ahr-bld-004"):
        answer = DEP.activate(agent, "PRODUCTION", approved_by=machine,
                              validation=PASSED)
        check(answer["refused"] == "MACHINE_MAY_NOT_SIGN",
              "'%s' cannot sign" % machine)
        check("Golden Rule 7" in answer["why"], "and the rule is cited")
    answer = DEP.activate(agent, "PRODUCTION", approved_by="the owner",
                          validation=PASSED)
    check(answer["activated"] and answer["record"]["approved_by"] == "the owner",
          "a person signs, and the record says who")

    print()
    print("4. The ladder is a ladder")
    answer = DEP.activate(agent, "PRODUCTION", from_stage="DRAFT",
                          approved_by="the owner", validation=PASSED)
    check(answer["refused"] == "STAGE_SKIPPED",
          "DRAFT straight to PRODUCTION is refused")
    check("TESTING" in answer["why"] and "SHADOW" in answer["why"],
          "and the refusal names the stages it would have skipped")
    answer = DEP.activate(agent, "DRAFT", from_stage="PROVEN",
                          validation=PASSED)
    check(answer["refused"] == "STAGE_SKIPPED"
          and "HERON-AHR-RET-010" in answer["why"],
          "going back down is sent to retirement, not done here")
    answer = DEP.activate(agent, "ASCENDED", validation=PASSED)
    check(answer["refused"] == "UNKNOWN_STAGE",
          "a stage docs/24 does not have is refused")

    print()
    print("5. Absent evidence is a refusal, never a downgrade")
    answer = DEP.activate(agent, "PROVEN", evidence={}, validation=PASSED)
    check(not answer["activated"] and "record" not in answer,
          "no lesser stage is quietly granted instead")
    answer = DEP.activate(agent, "PROVEN",
                          evidence={"real-runs": 12,
                                    "unexplained-failures": 2},
                          validation=PASSED)
    check(not answer["activated"] and "unexplained" in answer["why"],
          "ten runs with unexplained failures among them is still refused")

    print()
    print("6. Nothing is written")
    source = io.open(os.path.join(ROOT, "brain", "heron_deployment.py"),
                     encoding="utf-8").read()
    for forbidden in ('"w"', "'w'", "makedirs", "yaml.dump", "json.dump"):
        check(forbidden not in source,
              "the module contains no %s" % forbidden)

    print()
    print("7. The record is what the audit log keeps")
    answer = DEP.activate(agent, "SHADOW", from_stage="VALIDATED",
                          evidence={"shadow-plan": "beside the log writer"},
                          validation=PASSED)
    record = answer["record"]
    check(record["agent"] == agent and record["from"] == "VALIDATED"
          and record["to"] == "SHADOW",
          "both stages and the agent are in the record")
    check(record["evidence"] == {"shadow-plan": "beside the log writer"},
          "what was offered is kept, not just that something was")
    check(record["applied"] is False,
          "and applied is False - allowing a move is not making it")
    check(record["at"].endswith("Z"), "the time is stamped in UTC")

    print()
    print("8. A validation refusal stops everything above DRAFT")
    refused = {"verdict": "REFUSED", "findings": ["no contract"],
               "files": ["brain/heron_events.py", "tests/test_events.py"]}
    answer = DEP.activate(agent, "TESTING", validation=refused)
    check(answer["refused"] == "NOT_VALIDATED",
          "TESTING is refused while validation refuses")
    check(DEP.activate(agent, "DRAFT", validation=refused)["activated"],
          "DRAFT is still allowed - it only means an implementation exists")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    every gate refuses what it is for, and no machine signs")
    return 0


if __name__ == "__main__":
    sys.exit(main())
