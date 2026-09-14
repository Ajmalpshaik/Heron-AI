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

  9. THE CURRENT STAGE IS READ FROM THE REGISTRY RECORD, NEVER ASSERTED. Two
     reviews were needed for this one. The first found that omitting
     from_stage skipped the ladder check; the second found that REQUIRING it
     changed nothing, because a caller could still state "PROVEN" for an agent
     the register has at DRAFT and walk to PRODUCTION on one signature. A
     caller that can state its own stage is a caller that can skip the ladder.

 10. PROVEN TAKES RECORDED RUNS, NOT A COUNT. D-30 wants a run against a NAMED
     real model; "10" is a number somebody typed, and the gate now says so.
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

# The stage below each one. Every call passes it, because a promotion with no
# current stage is now refused - see claim 9.
BELOW = {"DISCOVERED": "DISCOVERED", "DRAFT": "DISCOVERED",
         "TESTING": "DRAFT", "VALIDATED": "TESTING", "SHADOW": "VALIDATED",
         "PROVEN": "SHADOW", "PRODUCTION": "PROVEN"}

# Ten recorded runs, each naming what it ran against. A count is not evidence.
RUNS = [{"run": "w-%d" % n, "model": "Snowdon Towers Sample HVAC",
         "negative-case": "a view with no ducts returned 0",
         "fingerprint": "a1b2c3", "degraded": False, "sandboxed": False}
        for n in range(12)]

# The evidence each gate actually wants, in the shape it wants it.
AUTHORS = {"implemented-by": "a session", "test-author": "the owner"}
MATRIX = {"matrix": {"2024": "pass", "2027": "pass"}}


def SIGNED(stage="PRODUCTION"):
    return {"by": "the owner", "at": "2026-09-14T03:00:00Z",
            "agent": "HERON-KRN-EVT-004", "stage": stage}


def REC(stage):
    """The registry record shape activate() reads the current stage out of."""
    return {"id": "HERON-KRN-EVT-004", "state": stage}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    agent = "HERON-KRN-EVT-004"

    print("1. Each gate refuses without its own evidence")
    cases = [
        ("DRAFT", {}, {"verdict": "PASS", "files": []}, "an implementation"),
        ("TESTING", AUTHORS, {"verdict": "PASS",
                              "files": ["brain/heron_events.py"]}, "a test"),
        ("VALIDATED", {}, PASSED, "a matrix"),
        ("SHADOW", {}, PASSED, "a shadow plan"),
        ("PROVEN", {"real-runs": 3}, PASSED, "ten recorded runs"),
        ("DISCOVERED", {}, PASSED, "a stated purpose"),
    ]
    for stage, evidence, validation, what in cases:
        answer = DEP.activate(agent, stage, record=REC(BELOW[stage]),
                              evidence=evidence, validation=validation)
        check(not answer["activated"] and answer["refused"] == "GATE_NOT_MET",
              "%s without %s is refused" % (stage, what))
        check(stage in answer["why"],
              "and the refusal names the gate it was for")

    print()
    print("   ... and each one passes when its evidence is there")
    for stage, evidence in (("DISCOVERED", {"purpose": "to notify"}),
                            ("DRAFT", {}),
                            ("TESTING", AUTHORS),
                            ("VALIDATED", MATRIX),
                            ("SHADOW", {"shadow-plan": "beside the log"}),
                            ("PROVEN", {"real-runs": RUNS})):
        answer = DEP.activate(agent, stage, record=REC(BELOW[stage]),
                              evidence=evidence, validation=PASSED)
        check(answer["activated"], "%s is allowed once its gate is met" % stage)

    print()
    print("2 and 3. Who may sign")
    answer = DEP.activate(agent, "PRODUCTION", record=REC("PROVEN"),
                          validation=PASSED)
    check(answer["refused"] == "NEEDS_HUMAN_APPROVAL",
          "PRODUCTION without a signature is refused")
    for machine in ("HERON-AHR-CRT-006", "heron-ahr-bld-004"):
        answer = DEP.activate(agent, "PRODUCTION", record=REC("PROVEN"),
                              approval=dict(SIGNED(), by=machine), validation=PASSED)
        check(answer["refused"] == "MACHINE_MAY_NOT_SIGN",
              "'%s' cannot sign" % machine)
        check("Golden Rule 7" in answer["why"], "and the rule is cited")
    answer = DEP.activate(agent, "PRODUCTION", record=REC("PROVEN"),
                          approval=SIGNED(), validation=PASSED)
    check(answer["activated"] and answer["record"]["approved_by"] == "the owner",
          "a person signs, and the record says who")
    check(DEP.activate(agent, "PRODUCTION", record=REC("PROVEN"),
                       approval="build-bot",
                       validation=PASSED).get("refused")
          == "NEEDS_HUMAN_APPROVAL",
          "a display name is not a signature - 'build-bot' is refused")
    check(DEP.activate(agent, "PRODUCTION", record=REC("PROVEN"),
                       approval=SIGNED("SHADOW"),
                       validation=PASSED).get("refused")
          == "NEEDS_HUMAN_APPROVAL",
          "a signature for another STAGE cannot be spent here")
    check(DEP.activate(agent, "PRODUCTION", record=REC("PROVEN"),
                       approval=dict(SIGNED(), agent="HERON-OTHER-001"),
                       validation=PASSED).get("refused")
          == "NEEDS_HUMAN_APPROVAL",
          "nor one for another AGENT")

    print()
    print("4. The ladder is a ladder")
    answer = DEP.activate(agent, "PRODUCTION", record=REC("DRAFT"),
                          approval=SIGNED(), validation=PASSED)
    check(answer["refused"] == "STAGE_SKIPPED",
          "DRAFT straight to PRODUCTION is refused")
    check("TESTING" in answer["why"] and "SHADOW" in answer["why"],
          "and the refusal names the stages it would have skipped")
    answer = DEP.activate(agent, "DRAFT", record=REC("PROVEN"),
                          validation=PASSED)
    check(answer["refused"] == "STAGE_SKIPPED"
          and "HERON-AHR-RET-010" in answer["why"],
          "going back down is sent to retirement, not done here")
    answer = DEP.activate(agent, "ASCENDED", record=REC("DRAFT"),
                          validation=PASSED)
    check(answer["refused"] == "UNKNOWN_STAGE",
          "a stage docs/24 does not have is refused")

    print()
    print("5. Absent evidence is a refusal, never a downgrade")
    answer = DEP.activate(agent, "PROVEN", record=REC("SHADOW"), evidence={},
                          validation=PASSED)
    check(not answer["activated"] and "record" not in answer,
          "no lesser stage is quietly granted instead")
    answer = DEP.activate(agent, "PROVEN", record=REC("SHADOW"),
                          evidence={"real-runs": RUNS,
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
    answer = DEP.activate(agent, "SHADOW", record=REC("VALIDATED"),
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
    answer = DEP.activate(agent, "TESTING", record=REC("DRAFT"),
                          validation=refused)
    check(answer["refused"] == "NOT_VALIDATED",
          "TESTING is refused while validation refuses")
    check(DEP.activate(agent, "DRAFT", record=REC("DISCOVERED"),
                       validation=refused)["activated"],
          "DRAFT is still allowed - it only means an implementation exists")

    print()
    print("9. A promotion with no current stage is refused")
    answer = DEP.activate(agent, "PRODUCTION", approval=SIGNED(),
                          validation=PASSED)
    check(answer.get("refused") == "NO_RECORD",
          "PRODUCTION with no registry record is refused, signature or not")
    check("taken on trust" in answer["why"],
          "and the refusal says the stage is read, not asserted")
    for absent in ("", None):
        check(DEP.activate(agent, "DRAFT", record={"id": agent, "state": absent},
                           validation=PASSED).get("refused")
              == "NO_CURRENT_STAGE",
              "a from_stage of %r is refused too" % absent)
    check(DEP.activate(agent, "DRAFT", record=REC("SOMEWHERE"),
                       validation=PASSED).get("refused") == "UNKNOWN_STAGE",
          "and a stage docs/24 does not have is refused by name")

    print()
    print("10. PROVEN wants records, not a number")
    answer = DEP.activate(agent, "PROVEN", record=REC("SHADOW"),
                          evidence={"real-runs": 10}, validation=PASSED)
    check(answer.get("refused") == "GATE_NOT_MET"
          and "A count is not evidence" in answer["why"],
          "the integer that used to pass is refused, and told why")
    check("D-30" in answer["why"], "and the decision that settles it is cited")
    for missing, what in (("model", "a run that names no model"),
                          ("negative-case", "a run with no negative case"),
                          ("fingerprint", "a run with no staleness "
                                          "fingerprint")):
        thin = [{k: v for k, v in run.items() if k != missing}
                for run in RUNS]
        answer = DEP.activate(agent, "PROVEN", record=REC("SHADOW"),
                              evidence={"real-runs": thin}, validation=PASSED)
        check(not answer["activated"] and "names no %s" % missing
              in answer["why"], "%s is not proof" % what)
    for marker in ("degraded", "sandboxed", "user-corrected"):
        marked = [dict(run, **{marker: True}) for run in RUNS]
        answer = DEP.activate(agent, "PROVEN", record=REC("SHADOW"),
                              evidence={"real-runs": marked},
                              validation=PASSED)
        check(not answer["activated"],
              "a run marked %s is evidence about something else" % marker)
    answer = DEP.activate(agent, "PROVEN", record=REC("SHADOW"),
                          evidence={"real-runs": RUNS[:3]},
                          validation=PASSED)
    check(not answer["activated"], "three recorded runs are not ten")

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
