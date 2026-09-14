# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-DEP-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Deployment - the gates between stages, and who may open each one.

    python brain/heron_deployment.py     one agent walked up the whole ladder

WHAT IT IS FOR
---------------
docs/28 gives this agent one line - "activates an approved agent" - and docs/24
gives it the substance: nine stages, each with a GATE TO ENTER. This module is
that table, made into something that refuses.

    DISCOVERED   identity, metadata and a stated purpose
    DRAFT        an implementation exists
    TESTING      a test written by somebody other than the implementer
    VALIDATED    the full matrix green
    SHADOW       validated, and a shadow plan
    PROVEN       N real executions, no unexplained failures  (N suggested: 10)
    PRODUCTION   human approval, explicit and recorded

IT DOES NOT WRITE THE STATUS, AND THAT IS THE POINT
-----------------------------------------------------
The status lives in the implementing file's own header, and a machine that
edits its own promotion is exactly what D-30 forbids: the machine gathers, a
person signs. So this returns a RECORD of an allowed move, for a person to
apply and the audit log to keep. Nothing here touches a source file.

THREE REFUSALS THAT ARE NOT NEGOTIABLE
---------------------------------------
  a machine may not sign        an approver that looks like a Heron agent id
                                is refused. Golden Rule 7 is not satisfied by
                                a different agent signing - only by a person
  no stage may be skipped       DRAFT to PRODUCTION in one move is the whole
                                trust model bypassed by one argument
  absent evidence is a refusal  never a quiet downgrade to a lesser stage. An
                                agent asked for PROVEN and given nothing does
                                not silently become SHADOW - somebody asked
                                for the wrong thing and needs telling

WHAT COUNTS AS EVIDENCE IS DELIBERATELY DULL
---------------------------------------------
A count of real runs, a shadow plan, a matrix result. None of it is judged
here for quality - this module checks that it was offered, and docs/24 leaves
the judging to the person who signs. A gate that tried to score the evidence
would be a second opinion nobody asked for, in the one place that must be
predictable.
"""

import os
import sys
import time

# docs/24 Axis 1, in order. The order IS the rule - a stage's gate assumes
# every gate below it has been met.
LADDER = ("DISCOVERED", "DRAFT", "TESTING", "VALIDATED", "SHADOW", "PROVEN",
          "PRODUCTION")
RETIRED = ("DEPRECATED", "ARCHIVED")

# docs/24 suggests 10 and says "suggest", so it is a default and not a law.
PROVEN_RUNS = 10

NEEDS_A_PERSON = ("PRODUCTION",)

AGENT_ID = "HERON-"


def _gate(agent_id, to_stage, evidence, validation):
    """(met, why) for one stage's gate. Nothing here writes anything."""
    evidence = evidence or {}

    if to_stage == "DISCOVERED":
        if not evidence.get("purpose"):
            return False, ("DISCOVERED needs identity, metadata and a stated "
                           "purpose. No purpose was offered.")
        return True, "identity and a stated purpose"

    if to_stage == "DRAFT":
        if not (validation or {}).get("files"):
            return False, "DRAFT needs an implementation, and nothing "\
                          "implements %s yet." % agent_id
        return True, "an implementation exists"

    if to_stage == "TESTING":
        files = (validation or {}).get("files") or []
        tests = [f for f in files if f.startswith("tests/")]
        if not tests:
            return False, ("TESTING needs a test written by somebody other "
                           "than the implementer (Golden Rule 7). Nothing "
                           "tests %s." % agent_id)
        return True, "tested by %s" % ", ".join(tests)

    if to_stage == "VALIDATED":
        if (validation or {}).get("verdict") != "PASS":
            return False, ("VALIDATED needs the full matrix green. Validation "
                           "says %s." % (validation or {}).get("verdict",
                                                               "nothing"))
        if not evidence.get("matrix"):
            return False, ("VALIDATED needs a matrix result - which versions "
                           "it was run against. None was offered, and this "
                           "module will not assume one.")
        return True, "validation passed and a matrix was offered"

    if to_stage == "SHADOW":
        if not evidence.get("shadow-plan"):
            return False, ("SHADOW needs a shadow plan (docs/18 s4) - what it "
                           "will run against and what its output is compared "
                           "with. Output not used, so a plan is the only "
                           "thing that makes the stage mean anything.")
        return True, "a shadow plan was offered"

    if to_stage == "PROVEN":
        runs = evidence.get("real-runs")
        if not isinstance(runs, int) or runs < PROVEN_RUNS:
            return False, ("PROVEN needs %d real executions with no "
                           "unexplained failures. Offered: %s."
                           % (PROVEN_RUNS, runs if runs is not None else
                              "nothing"))
        if evidence.get("unexplained-failures"):
            return False, ("PROVEN needs no unexplained failures, and %s were "
                           "offered with the runs."
                           % evidence["unexplained-failures"])
        return True, "%d real runs, no unexplained failures" % runs

    if to_stage == "PRODUCTION":
        return True, "trusted by default, on a person's signature"

    return False, "'%s' is not a stage in docs/24" % to_stage


def activate(agent_id, to_stage, from_stage=None, approved_by=None,
             evidence=None, validation=None):
    """
    {activated, record, why} - or a refusal. It never edits a file.
    """
    to_stage = (to_stage or "").upper()
    from_stage = (from_stage or "").upper() or None

    if to_stage not in LADDER:
        return {"activated": False, "refused": "UNKNOWN_STAGE",
                "why": "'%s' is not a stage. docs/24 has one vocabulary for "
                       "fragments, skills, capabilities and agents: %s."
                       % (to_stage, ", ".join(LADDER))}

    if from_stage and from_stage in LADDER:
        step = LADDER.index(to_stage) - LADDER.index(from_stage)
        if step > 1:
            return {"activated": False, "refused": "STAGE_SKIPPED",
                    "why": "%s to %s skips %s. Each gate assumes the ones "
                           "below it were met, so skipping one is the trust "
                           "model bypassed by an argument."
                           % (from_stage, to_stage,
                              ", ".join(LADDER[LADDER.index(from_stage) + 1:
                                               LADDER.index(to_stage)]))}
        if step <= 0:
            return {"activated": False, "refused": "STAGE_SKIPPED",
                    "why": "%s is not above %s. Going back down is retirement "
                           "and belongs to HERON-AHR-RET-010."
                           % (to_stage, from_stage)}

    if to_stage in NEEDS_A_PERSON and not approved_by:
        return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                "why": "%s is trusted by default, so docs/24 asks for human "
                       "approval, explicit and recorded. Nothing was signed."
                       % to_stage}

    if approved_by and str(approved_by).upper().startswith(AGENT_ID):
        return {"activated": False, "refused": "MACHINE_MAY_NOT_SIGN",
                "why": "'%s' is a Heron agent. No agent approves itself "
                       "(Golden Rule 7), and another agent signing is the "
                       "same rule broken by one more step." % approved_by}

    if validation and validation.get("verdict") == "REFUSED" \
            and to_stage not in ("DISCOVERED", "DRAFT"):
        return {"activated": False, "refused": "NOT_VALIDATED",
                "why": "validation refused %s: %s" % (
                    agent_id, "; ".join(validation.get("findings") or [])[:200])}

    met, why = _gate(agent_id, to_stage, evidence, validation)
    if not met:
        return {"activated": False, "refused": "GATE_NOT_MET", "why": why}

    return {
        "activated": True,
        "why": "%s entered on %s" % (to_stage, why),
        "record": {
            "agent": agent_id,
            "from": from_stage,
            "to": to_stage,
            "approved_by": approved_by,
            "evidence": dict(evidence or {}),
            "at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "applied": False,
        },
    }


def main(argv):
    agent = "HERON-KRN-EVT-004"
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import heron_validation as VAL
    validation = VAL.validate(agent)

    print("AGENT DEPLOYMENT   one agent walked up the ladder")
    print("=" * 70)
    steps = [
        ("DRAFT", {}, None),
        ("TESTING", {}, None),
        ("VALIDATED", {"matrix": "python 3.11 on linux"}, None),
        ("SHADOW", {}, None),
        ("SHADOW", {"shadow-plan": "run beside the log writer for a week"},
         None),
        ("PROVEN", {"real-runs": 3}, None),
        ("PROVEN", {"real-runs": 12}, None),
        ("PRODUCTION", {}, None),
        ("PRODUCTION", {}, "HERON-AHR-CRT-006"),
        ("PRODUCTION", {}, "the owner"),
    ]
    stage = "DISCOVERED"
    for to_stage, evidence, approver in steps:
        answer = activate(agent, to_stage, from_stage=stage,
                          approved_by=approver, evidence=evidence,
                          validation=validation)
        mark = "ok" if answer["activated"] else answer["refused"]
        print("  %-11s -> %-11s %-22s %s"
              % (stage, to_stage, mark, answer["why"][:70]))
        if answer["activated"]:
            stage = to_stage
    print()
    print("  Nothing above touched a source file. The status lives in the")
    print("  implementing file's header, and a machine that edits its own")
    print("  promotion is what D-30 exists to forbid.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
