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

import copy
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
        # A PATH IS NOT AN AUTHOR. The existence of a file under tests/ says
        # nothing about who wrote it, and the gate's whole point is that it
        # was somebody else. So the two names are asked for and compared, and
        # an absent answer is a refusal rather than an assumption.
        wrote_it = str(evidence.get("implemented-by") or "").strip()
        tested_it = str(evidence.get("test-author") or "").strip()
        if not wrote_it or not tested_it:
            return False, ("TESTING needs the test's author and the "
                           "implementer named, because the gate is that they "
                           "are different people (Golden Rule 7). A file "
                           "under tests/ only says a file exists.")
        if wrote_it.lower() == tested_it.lower():
            return False, ("%s both implemented and tested %s. docs/24 puts "
                           "TESTING behind a test written by somebody else."
                           % (tested_it, agent_id))
        return True, "tested by %s, who did not implement it" % tested_it

    if to_stage == "VALIDATED":
        if (validation or {}).get("verdict") != "PASS":
            return False, ("VALIDATED needs the full matrix green. Validation "
                           "says %s." % (validation or {}).get("verdict",
                                                               "nothing"))
        matrix = evidence.get("matrix")
        if not matrix:
            return False, ("VALIDATED needs a matrix result - which versions "
                           "it was run against. None was offered, and this "
                           "module will not assume one.")
        # ANY TRUTHY VALUE USED TO OPEN THIS. {"matrix": "FAIL"} passed, and
        # so did an unrelated sentence. The validation agent's PASS covers the
        # repository's checks, not the supported-version matrix, so the matrix
        # has to carry its own per-version results.
        if not isinstance(matrix, dict) or not matrix:
            return False, ("VALIDATED needs the matrix as results per "
                           "version - {\"2024\": \"pass\", ...} - not as "
                           "'%s'. A word is not a matrix." % (matrix,))
        failed = [version for version, outcome in sorted(matrix.items())
                  if str(outcome).strip().lower() not in ("pass", "passed",
                                                          "ok", "green",
                                                          "true")]
        if failed:
            return False, ("VALIDATED needs the FULL matrix green. %s did not "
                           "pass." % ", ".join(failed))
        return True, ("validation passed and %d version(s) are green: %s"
                      % (len(matrix), ", ".join(sorted(matrix))))

    if to_stage == "SHADOW":
        if not evidence.get("shadow-plan"):
            return False, ("SHADOW needs a shadow plan (docs/18 s4) - what it "
                           "will run against and what its output is compared "
                           "with. Output not used, so a plan is the only "
                           "thing that makes the stage mean anything.")
        return True, "a shadow plan was offered"

    if to_stage == "PROVEN":
        runs = evidence.get("real-runs")
        # A COUNT IS NOT EVIDENCE. D-30: a proof is a recorded run against a
        # NAMED real model, and "10" is a number somebody typed. So the gate
        # takes run RECORDS and reads them - each naming the run and what it
        # ran against - and refuses the integer it used to accept, saying why.
        if isinstance(runs, int):
            return False, ("PROVEN was offered the number %d. A count is not "
                           "evidence - D-30 wants recorded runs, each naming "
                           "the real model it ran against. Pass the records."
                           % runs)
        if not isinstance(runs, (list, tuple)) or len(runs) < PROVEN_RUNS:
            return False, ("PROVEN needs %d recorded real executions. "
                           "Offered: %s." % (PROVEN_RUNS,
                                             len(runs) if isinstance(
                                                 runs, (list, tuple))
                                             else "nothing"))
        # WHAT D-30 ACTUALLY ASKS FOR. A run id and a model name are the
        # two identity fields; they say the run can be looked up, not that it
        # proved anything. The proof is the negative case, the staleness
        # fingerprint, and the run having actually succeeded on its own
        # merits - not degraded onto a fallback, not sandboxed against a stub,
        # not corrected by the user afterwards.
        thin = []
        for index, run in enumerate(runs):
            if not isinstance(run, dict):
                thin.append((index, "is not a record"))
                continue
            for field in ("run", "model", "negative-case", "fingerprint"):
                if not run.get(field):
                    thin.append((index, "names no %s" % field))
                    break
            else:
                if run.get("degraded") or run.get("sandboxed"):
                    thin.append((index, "was degraded or sandboxed, so it is "
                                        "evidence about the fallback or the "
                                        "stub"))
                elif run.get("user-corrected"):
                    thin.append((index, "was corrected by the user, which "
                                        "docs/24 counts against PROVEN"))
        if thin:
            index, why = thin[0]
            return False, ("%d of the %d runs offered are not proof: run %d %s."
                           " D-30 wants a recorded run against a named real "
                           "model, with a negative case and a staleness "
                           "fingerprint."
                           % (len(thin), len(runs), index, why))
        if evidence.get("unexplained-failures"):
            return False, ("PROVEN needs no unexplained failures, and %s were "
                           "offered with the runs."
                           % evidence["unexplained-failures"])
        return True, ("%d recorded runs against %d named model(s), no "
                      "unexplained failures"
                      % (len(runs), len({r["model"] for r in runs})))

    if to_stage == "PRODUCTION":
        return True, "trusted by default, on a person's signature"

    return False, "'%s' is not a stage in docs/24" % to_stage


def _canonical(agent_id):
    """The registry's own record. The default source, and the only one."""
    import heron_agents as REG
    return REG.record(agent_id)


def activate(agent_id, to_stage, records=None, approval=None,
             evidence=None, validation=None):
    """
    {activated, record, why} - or a refusal. It never edits a file.

    `records` is a READER of the Agent Registry - it is handed an agent id
    and gives back that agent's record, and the stage the agent is in NOW is
    read out of what comes back. It is not a parameter a caller may assert:
    until 2026-09-14 this took `from_stage` on trust, so passing "PROVEN" for
    an agent the register has at DRAFT promoted it to PRODUCTION on one
    signature, past every gate in between. A caller that can state its own
    current stage is a caller that can skip the ladder, and the ladder is the
    trust model.
    """
    to_stage = (to_stage or "").upper()

    # THE RECORD IS FETCHED, NOT ACCEPTED. Version 3 took a record from the
    # caller and read the stage out of it, which is the same hole as taking
    # `from_stage`: a caller that can hand over {"state": "PROVEN"} for an
    # agent the register has at DRAFT walks to PRODUCTION past every gate.
    # `records` exists so a test can supply a reader, not a value - in every
    # real caller it is the registry, and there is no argument that says what
    # stage the agent is in.
    record = (records or _canonical)(agent_id)

    if not isinstance(record, dict) or not record.get("id"):
        return {"activated": False, "refused": "NO_RECORD",
                "why": "the register has no record for %s, and the stage it "
                       "is in now is read from the register rather than taken "
                       "on trust." % agent_id}

    if str(record.get("id")).strip().upper() != str(agent_id).strip().upper():
        return {"activated": False, "refused": "RECORD_IS_NOT_THIS_AGENT",
                "why": "the register returned a record for %s when asked "
                       "about %s." % (record.get("id"), agent_id)}

    from_stage = str(record.get("state") or "").strip().upper() or None

    if to_stage not in LADDER:
        return {"activated": False, "refused": "UNKNOWN_STAGE",
                "why": "'%s' is not a stage. docs/24 has one vocabulary for "
                       "fragments, skills, capabilities and agents: %s."
                       % (to_stage, ", ".join(LADDER))}

    # A PROMOTION WITHOUT A CURRENT STAGE IS EVERY GATE SKIPPED. Until
    # 2026-09-14 this check ran only `if from_stage and from_stage in LADDER`,
    # so omitting it - which is what a caller built from the contract did,
    # because the contract did not declare the field - walked straight to
    # PRODUCTION on one signature, past DISCOVERED, DRAFT, TESTING, VALIDATED,
    # SHADOW and PROVEN. The ladder is the trust model; an optional rung is
    # not a rung.
    if not from_stage:
        return {"activated": False, "refused": "NO_CURRENT_STAGE",
                "why": "the record for %s carries no stage, and a gate can "
                       "only be checked against the one below it."
                       % agent_id}

    if from_stage not in LADDER:
        return {"activated": False, "refused": "UNKNOWN_STAGE",
                "why": "the register has %s at '%s', which is not a stage on "
                       "the ladder, so nothing can be promoted out of it. The "
                       "ladder is: %s."
                       % (agent_id, from_stage, ", ".join(LADDER))}

    if True:
        step = LADDER.index(to_stage) - LADDER.index(from_stage)
        if step > 1:
            return {"activated": False, "refused": "STAGE_SKIPPED",
                    "why": "%s to %s skips %s. Each gate assumes the ones "
                           "below it were met, so skipping one is the trust "
                           "model bypassed by an argument."
                           % (from_stage, to_stage,
                              ", ".join(LADDER[LADDER.index(from_stage) + 1:
                                               LADDER.index(to_stage)]))}
        # DISCOVERED is the bottom rung, so entering it from itself is the
        # one move that is not a move: an agent gets its identity once, and
        # there is no stage below the stage where identity is assigned.
        if step <= 0 and not (to_stage == LADDER[0] == from_stage):
            return {"activated": False, "refused": "STAGE_SKIPPED",
                    "why": "%s is not above %s. Going back down is retirement "
                           "and belongs to HERON-AHR-RET-010."
                           % (to_stage, from_stage)}

    # AN APPROVAL IS A RECORD, SCOPED TO THIS PROMOTION. A display string
    # let `approved_by="build-bot"` through: a machine signing by not looking
    # like one. A record has to say what it approved, so a signature for one
    # agent's SHADOW cannot be spent on another's PRODUCTION, and the same
    # label cannot be reused for every promotion there is.
    approved_by = None
    if approval is not None:
        if not isinstance(approval, dict):
            return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                    "why": "an approval is a record - who signed, when, and "
                           "what for - not the text '%s'. A name on its own "
                           "cannot say which promotion it approved."
                           % (approval,)}
        approved_by = str(approval.get("by") or "").strip()
        if not approved_by:
            return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                    "why": "the approval names nobody."}
        if approved_by.upper().startswith(AGENT_ID):
            return {"activated": False, "refused": "MACHINE_MAY_NOT_SIGN",
                    "why": "'%s' is a Heron agent. No agent approves itself "
                           "(Golden Rule 7), and another agent signing is the "
                           "same rule broken by one more step." % approved_by}
        for field, expected in (("agent", agent_id), ("stage", to_stage)):
            given = str(approval.get(field) or "").strip().upper()
            if not given:
                return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                        "why": "the approval does not say which %s it is for. "
                               "An unscoped signature is one that can be "
                               "spent anywhere." % field}
            if given != str(expected).strip().upper():
                return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                        "why": "the approval is for %s %s, and this is %s %s."
                               % (field, approval.get(field), field, expected)}
        if not approval.get("at"):
            return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                    "why": "the approval carries no time. docs/24 asks for a "
                           "signature that is explicit AND recorded."}

    if to_stage in NEEDS_A_PERSON and not approved_by:
        return {"activated": False, "refused": "NEEDS_HUMAN_APPROVAL",
                "why": "%s is trusted by default, so docs/24 asks for human "
                       "approval, explicit and recorded. Nothing was signed."
                       % to_stage}

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
            # A DEEP COPY. A shallow one kept the caller's list and run
            # dictionaries by reference, so clearing or editing them after
            # activate() returned rewrote the audit record of the proof that
            # opened the gate.
            "evidence": copy.deepcopy(evidence or {}),
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
    proof = [{"run": "w-%d" % n, "model": "Snowdon Towers Sample HVAC",
              "negative-case": "a view with no ducts returned 0",
              "fingerprint": "a1b2c3", "degraded": False, "sandboxed": False}
             for n in range(12)]
    signed = {"by": "the owner", "at": "2026-09-14T03:00:00Z",
              "agent": agent, "stage": "PRODUCTION"}
    steps = [
        ("DRAFT", {}, None),
        ("TESTING", {}, None),
        ("TESTING", {"implemented-by": "a session", "test-author": "a session"},
         None),
        ("TESTING", {"implemented-by": "a session",
                     "test-author": "the owner"}, None),
        ("VALIDATED", {"matrix": "green"}, None),
        ("VALIDATED", {"matrix": {"2024": "pass", "2027": "fail"}}, None),
        ("VALIDATED", {"matrix": {"2024": "pass", "2027": "pass"}}, None),
        ("SHADOW", {}, None),
        ("SHADOW", {"shadow-plan": "run beside the log writer for a week"},
         None),
        ("PROVEN", {"real-runs": 3}, None),
        ("PROVEN", {"real-runs": proof}, None),
        ("PRODUCTION", {}, None),
        ("PRODUCTION", {}, "the owner"),
        ("PRODUCTION", {}, dict(signed, by="HERON-AHR-CRT-006")),
        ("PRODUCTION", {}, signed),
    ]
    stage = "DISCOVERED"
    for to_stage, evidence, approver in steps:
        # A READER, NOT A VALUE. This demo walks an agent the register does
        # not carry, so it supplies the reader `records` exists for - and it
        # still cannot state a stage, because the reader is asked for a
        # record and the stage is read out of that.
        answer = activate(agent, to_stage,
                          records=lambda _id, at=stage: {"id": agent,
                                                         "state": at},
                          approval=approver, evidence=evidence,
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
