# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-CRT-006
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Creator - it owns the pipeline and it may only ever assign PROPOSED.

    python brain/heron_creator.py
    python brain/heron_creator.py "Duct Counter" "counts ducts in a view"

WHAT IT IS FOR (docs/28, HERON-AHR-CRT-006)
--------------------------------------------
"Owns the pipeline. MAY ONLY EVER ASSIGN `PROPOSED`." Risk ADMIN.

It is the one agent in the factory that runs the others, and the register
gives it the highest permission there is with a single sentence of
restriction in bold. That sentence is the whole design.

ADMIN AND STILL UNABLE TO PROMOTE
----------------------------------
ADMIN is what lets it drive the pipeline. `PROPOSED` is the only status it
can attach to anything that comes out. It does not touch the deployment
ladder, it holds no approval, and there is no argument anywhere in this
file that takes a status - a caller that could pass one could promote by
passing it.

That is not caution, it is Golden Rule 7 arriving at the one place where it
would be most convenient to forget: the agent that automates the hiring is
exactly the agent that should not be able to complete it.

WHERE IT STOPS, AND WHY THAT IS THE POINT
------------------------------------------
The pipeline is: Workforce Planning, then HR, then the Architect. It runs
as far as it can and stops at the first step needing a person, saying which
one and what they have to do.

Today it always stops in the same place, and that is by design rather than
by omission. HR leaves the agent id None because an id is a row in
docs/28-agent-registry.md; the Architect will not design for an id the
register does not carry. So the pipeline reaches "somebody must add the
register row" and halts. The register row IS the hiring decision, and this
agent's job is to have everything ready for it, not to make it.

NOTHING IS SWALLOWED
---------------------
Every step's refusal is carried out whole - the state and the sentence -
and the pipeline stops there. Golden Rule 14: never silently discard. A
pipeline that turned six refusals into one "failed" would lose exactly the
information the person needs.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The ONLY status this agent may attach to anything. docs/28, in bold.
MAY_ASSIGN = "PROPOSED"

# The steps, in order, with who owns each.
PIPELINE = (
    ("assess", "HERON-AHR-WFP-015", "is this even a new agent?"),
    ("describe", "HERON-AHR-HR-002", "what is the job?"),
    ("register", "a person", "add the row to docs/28-agent-registry.md"),
    ("design", "HERON-AHR-ARC-003", "what is the contract?"),
)


def run(name, purpose, capability=None, department=None, agent_id=None,
        tools=None, dependencies=None, table=None, failures=None,
        inputs=None, outputs=None, agents=None, **not_ours):
    """
    {pipeline, stopped_at, waiting_on, assigned, unjudged, why}.

    `assigned` is always PROPOSED. There is no parameter that sets it, and
    no path through this function that returns anything else.
    """
    if not str(name or "").strip() or not str(purpose or "").strip():
        return {"refused": "NOTHING_TO_CREATE",
                "why": "a pipeline starts with a name and a responsibility. "
                       "Workforce Planning reads the responsibility to decide "
                       "whether this should exist at all."}

    # NO CALLER MAY HAND A STATUS IN. A parameter that sets one is a
    # promotion route through the agent that automates the hiring.
    forbidden = sorted(k for k in not_ours
                       if k in ("status", "state", "stage", "approved",
                                "approval", "assigned"))
    if forbidden:
        return {"refused": "MAY_ONLY_ASSIGN_PROPOSED",
                "why": "%s cannot be passed here. This agent may only ever "
                       "assign PROPOSED (docs/28), and a parameter that sets "
                       "a status is a way of promoting through the agent that "
                       "automates the hiring."
                       % ", ".join("'%s'" % k for k in forbidden)}
    if not_ours:
        return {"refused": "NOT_A_PIPELINE_INPUT",
                "why": "this pipeline has no %s. Keeping it would put a value "
                       "in the record that no step reads."
                       % ", ".join("'%s'" % k for k in sorted(not_ours))}

    import heron_workforce as WFP
    import heron_hr as HR
    import heron_architect as ARC

    if agents is None:
        import heron_agents as REG
        try:
            agents, _claims, _host = REG._agent_count()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}

    steps = []

    def stop(step, owner, answer, what_now):
        steps.append({"step": step, "owner": owner,
                      "refused": answer.get("refused"),
                      "why": answer.get("why")})
        return {"pipeline": steps, "stopped_at": step, "waiting_on": owner,
                "what_now": what_now, "assigned": MAY_ASSIGN,
                "unjudged": _unjudged(steps),
                "why": "stopped at '%s': %s" % (step,
                                                answer.get("refused"))}

    # 1. SHOULD THIS EXIST AT ALL. The guard runs first or it guards nothing.
    assessment = WFP.assess(name, purpose, capability=capability,
                            department=department, agents=agents)
    if assessment.get("refused"):
        return stop("assess", "HERON-AHR-WFP-015", assessment,
                    "give the proposal a name and a responsibility")
    if assessment.get("verdict") != "PROPOSE_HIRING":
        return stop("assess", "a person",
                    {"refused": assessment["verdict"],
                     "why": assessment.get("why")},
                    "Workforce Planning says this is not a new agent. Read "
                    "its matches and either do what it suggests or disagree "
                    "with it on the record.")
    steps.append({"step": "assess", "owner": "HERON-AHR-WFP-015",
                  "result": assessment["verdict"],
                  "unjudged": assessment.get("unjudged")})

    # 2. WHAT IS THE JOB.
    job = HR.write(name, purpose, assessment=assessment,
                   capabilities=[capability] if capability else [],
                   dependencies=dependencies, tools=tools,
                   department=department, agents=agents, table=table)
    if job.get("refused"):
        return stop("describe", "HERON-AHR-HR-002", job,
                    "fix the proposal and run it again")
    steps.append({"step": "describe", "owner": "HERON-AHR-HR-002",
                  "result": job["job"], "unjudged": job.get("unjudged")})

    # 3. THE REGISTER ROW, WHICH IS A PERSON'S. HR left the id None on
    # purpose and the Architect will not design without one.
    if not str(agent_id or "").strip():
        return stop("register", "a person",
                    {"refused": "NEEDS_A_REGISTER_ROW",
                     "why": "the job description is ready and its id is None. "
                            "An id is a row in docs/28-agent-registry.md, and "
                            "that row is the hiring decision - this agent "
                            "prepares it and does not make it."},
                    "add the row to docs/28-agent-registry.md with a tier and "
                    "a risk, then run this again with the new id")

    # 4. THE CONTRACT.
    design = ARC.design(job["job"], agent_id, failures=failures,
                        inputs=inputs, outputs=outputs, tools=tools)
    if design.get("refused"):
        return stop("design", "HERON-AHR-ARC-003", design,
                    "fix the design inputs, or the register row, and run "
                    "this again")
    steps.append({"step": "design", "owner": "HERON-AHR-ARC-003",
                  "result": design["contract"],
                  "unjudged": design.get("unjudged")})

    return {"pipeline": steps, "stopped_at": None,
            "waiting_on": "a person",
            "what_now": "review the contract, then build it. Nothing here "
                        "may promote it past PROPOSED.",
            "assigned": MAY_ASSIGN, "unjudged": _unjudged(steps),
            "why": "%d step(s) ran and the contract is designed. Assigned "
                   "%s, which is the only status this agent may assign."
                   % (len(steps), MAY_ASSIGN)}


def _unjudged(steps):
    """
    Every step's unjudged notes, carried out whole and attributed.

    Golden Rule 14: never silently discard. A pipeline that summarised six
    steps' caveats into one line would lose the caveats.
    """
    found = ["NOTHING HERE IS APPROVED. This agent may only ever assign %s "
             "(docs/28). It holds no approval, it does not touch the "
             "deployment ladder, and no argument of its own takes a status."
             % MAY_ASSIGN]
    for step in steps:
        for note in step.get("unjudged") or []:
            found.append("%s (%s): %s" % (step["step"], step["owner"], note))
    return found


def main(argv):
    import heron_agents as REG
    agents, _claims, _host = REG._agent_count()

    if len(argv) > 2:
        answer = run(argv[1], argv[2], agents=agents)
        if answer.get("refused"):
            print("%s\n  %s" % (answer["refused"], answer["why"]))
            return 1
        print("PIPELINE   %s" % argv[1])
        print("=" * 70)
        for step in answer["pipeline"]:
            print("  %-10s %-22s %s"
                  % (step["step"], step["owner"],
                     step.get("refused") or "ok"))
            if step.get("why"):
                print("      %s" % step["why"])
        print()
        print("  assigned     %s" % answer["assigned"])
        print("  waiting on   %s" % answer["waiting_on"])
        print("  what now     %s" % answer["what_now"])
        return 0

    # A SAMPLE TOOL TABLE, handed in the way HR requires - brain may not
    # read mcp/server/heron_tools.py (D-48), so the pipeline passes through
    # whatever its own caller supplied.
    sample = {"heron_lookup": "READ"}

    print("AGENT CREATOR   ADMIN, and still unable to promote anything")
    print("=" * 70)
    print("  The pipeline: %s"
          % " -> ".join("%s (%s)" % (s, who) for s, who, _q in PIPELINE))
    print()

    for label, kwargs in [
            ("nothing to create", dict(name="", purpose="")),
            ("a caller trying to hand it a status",
             dict(name="Duct Counter", purpose="counts ducts in a view",
                  status="PROVEN")),
            ("a job Workforce Planning turns down",
             dict(name="Agent Sandbox Agent",
                  purpose="runs a newly built agent in isolation, never "
                          "against a live model")),
            ("a job named as one operation - a fragment, not an agent",
             dict(name="Duct Counter", purpose="counts ducts in a view",
                  capability="COUNT_DUCTS")),
            ("a genuinely new job, with no register row yet",
             dict(name="Duct Sizing Reviewer",
                  purpose="reviews duct sizing against the project brief",
                  tools=["heron_lookup"], table=sample)),
            ("the same job once somebody has added the row",
             dict(name="Duct Sizing Reviewer",
                  purpose="reviews duct sizing against the project brief",
                  tools=["heron_lookup"], table=sample,
                  agent_id="HERON-AHR-CRT-006",
                  failures=["NO_DUCTS_IN_VIEW"],
                  inputs={"view": {"type": "string", "required": True,
                                   "description": "The view to read."}},
                  outputs={"findings": {"type": "list",
                                        "description": "Where it departs."}})),
    ]:
        kwargs.setdefault("agents", agents)
        answer = run(**kwargs)
        if answer.get("refused"):
            print("  %-44s %s" % (label, answer["refused"]))
            print("      %s" % answer["why"][:96])
        else:
            print("  %-44s %s, waiting on %s"
                  % (label,
                     "stopped at '%s'" % answer["stopped_at"]
                     if answer["stopped_at"] else "ran every step",
                     answer["waiting_on"]))
            print("      %s" % answer["what_now"][:96])
        print("      assigned: %s"
              % answer.get("assigned", "- (refused before any step)"))
    print()
    print("  Every run above assigned PROPOSED or assigned nothing at all.")
    print("  There is no path through this file that returns another status")
    print("  and no parameter that could carry one in.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
