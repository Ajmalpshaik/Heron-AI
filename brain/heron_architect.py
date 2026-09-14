# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-ARC-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Architect - the job description turned into a contract, and nothing
added on the way.

    python brain/heron_architect.py

WHAT IT IS FOR (docs/28, HERON-AHR-ARC-003)
--------------------------------------------
"Designs the agent and its contract."

HR wrote what the job is. The Architect turns that into the thing the rest
of Heron actually reads: a contract (HERON-AHR-CON-017), which is data and
not prose (D-29), and which every later step - the Builder, Validation, the
deployment ladder, the compatibility verdict on the next version - treats as
the agent's promise.

IT MAY NOT ADD ANYTHING TO THE JOB
-----------------------------------
This is the rule the whole file is built around. A contract granting a tool
the job description never asked for is scope creep with a signature on it,
and it is invisible afterwards: by the time the Builder implements against
the contract, nobody is holding the job description beside it.

So every tool and every capability in the contract must appear in the job.
Not "should" - CONTRACT_EXCEEDS_THE_JOB, naming what was added. Removing
things is allowed and reported; a design that needs less than the job asked
for is a design worth looking at, not an error.

IT CANNOT DESIGN FOR AN AGENT THAT DOES NOT EXIST YET
------------------------------------------------------
A contract's first field is `agent:`, and an agent id is a row in
docs/28-agent-registry.md. HR deliberately leaves the id None - writing one
would be HR editing the register. So the id arrives here, and it is checked
against the register rather than trusted.

That puts a person between the job description and the contract, which is
where Golden Rule 7 wants one. The register row is the hiring decision; this
agent designs what was hired and does not do the hiring.

IT VALIDATES ITS OWN OUTPUT BEFORE RETURNING IT
------------------------------------------------
An architect that emits a contract which does not validate has produced
something worse than nothing: a file that looks designed and fails at the
next gate, with the failure attributed to whoever picked it up. So the
contract goes through heron_contract.validate() here, and a contract that
does not pass is a refusal rather than an output with a warning attached.

WHAT IT CANNOT DECIDE, AND SAYS SO
-----------------------------------
Three things come back in `unjudged` every time:

  - Is this the right SHAPE for the job? That is design judgement. docs/28
    calls this agent T3 - an agentic loop, a model in the middle - and there
    is no adapter to call, so the question is reported, never answered.
  - Is the timeout right? It is a guess with a default behind it until
    something has been timed. Nothing here has been timed.
  - Are the declared failure states the ones the implementation will
    actually name? They are a PROMISE made before any implementation
    exists. Agent Validation checks it afterwards, and nine of the first ten
    agents failed exactly that check.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What HR's output looks like. A job missing any of these did not come from
# HERON-AHR-HR-002, whatever else it is.
JOB_FIELDS = ("name", "responsibility", "capabilities", "dependencies",
              "tools")

# The default when nothing has been timed - which is every agent so far.
# docs/19 s3 is where a measured one would come from.
DEFAULT_TIMEOUT = 30


def _listed(value):
    return list(value or [])


def design(job, agent_id, version="1.0.0", failures=None, timeout=None,
           inputs=None, outputs=None, tools=None, known_ids=None):
    """
    {contract, dropped, unjudged, why} for a job description - or a refusal.

    `tools` narrows what the job asked for. It can never widen it.
    """
    if not isinstance(job, dict):
        return {"refused": "NO_JOB_DESCRIPTION",
                "why": "the Architect designs from a job description "
                       "(HERON-AHR-HR-002), and none was given. Designing "
                       "from a name and a hope is how a contract ends up "
                       "describing what somebody assumed was wanted."}

    absent = [f for f in JOB_FIELDS if f not in job]
    if absent:
        return {"refused": "NOT_A_JOB_DESCRIPTION",
                "why": "the job is missing %s, so it did not come from HR. "
                       "The fields are not decoration - the checks below "
                       "compare the contract against them."
                       % ", ".join("'%s'" % f for f in absent)}

    if not str(job.get("responsibility") or "").strip():
        return {"refused": "NOT_A_JOB_DESCRIPTION",
                "why": "the job has no responsibility, and every field of the "
                       "contract is an answer to it."}

    # THE ID IS CHECKED AGAINST THE REGISTER, NEVER TRUSTED. HR leaves it
    # None on purpose; a person puts the row in docs/28, and that row is the
    # hiring decision this agent is downstream of (Golden Rule 7).
    agent_id = str(agent_id or "").strip()
    if not agent_id:
        return {"refused": "NO_AGENT_ID",
                "why": "a contract's first field is `agent:`, and HR leaves "
                       "the id None because an id is a row in docs/28. Add "
                       "the row, then design against it."}

    if known_ids is None:
        import heron_contract as CON
        try:
            known_ids = CON.registry_ids()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}
    if agent_id not in known_ids:
        return {"refused": "NOT_IN_THE_REGISTER",
                "why": "%s is not in docs/28-agent-registry.md. The register "
                       "row is where an agent comes into existence; a "
                       "contract for an id nobody has planned designs "
                       "something that does not exist." % agent_id}

    # NOTHING MAY BE ADDED. Narrowing is allowed and reported; widening is
    # scope creep that becomes invisible the moment the job description and
    # the contract stop being read side by side.
    asked_tools = set(_listed(job.get("tools")))
    want_tools = asked_tools if tools is None else set(_listed(tools))
    added = sorted(want_tools - asked_tools)
    if added:
        return {"refused": "CONTRACT_EXCEEDS_THE_JOB",
                "why": "the contract grants %s, which the job description "
                       "did not ask for. A contract may narrow a job and may "
                       "never widen it."
                       % ", ".join("'%s'" % t for t in added)}

    failures = sorted(set(_listed(failures)))
    if not failures:
        return {"refused": "NO_FAILURE_DECLARED",
                "why": "a contract declares at least one failure state. An "
                       "agent that cannot fail is an agent whose caller has "
                       "nothing to handle, and every agent can fail."}

    contract = {
        "agent": agent_id,
        "version": str(version),
        "input": dict(inputs or {}),
        "output": dict(outputs or {}),
        "allowed-tools": sorted(want_tools),
        "timeout-seconds": int(timeout or DEFAULT_TIMEOUT),
        "failures": failures,
        "retry": {"attempts": 0, "on-failures": []},
    }

    # IT VALIDATES ITS OWN OUTPUT. A contract that does not pass is a
    # refusal, not an output with a note attached - the note is what gets
    # lost between here and the gate that fails.
    import heron_contract as CON
    problems = CON.validate(contract, known_ids,
                            where="the designed contract")
    if problems:
        return {"refused": "CONTRACT_INVALID",
                "why": "the design does not validate: %s" % "; ".join(problems)}

    dropped = sorted(asked_tools - want_tools)

    unjudged = [
        "is this the right SHAPE for the job - the fields, their names, what "
        "is required? That is design judgement. docs/28 makes this agent T3, "
        "a model in the loop, and there is no adapter to call.",
        "is %d seconds the right timeout? Nothing has been timed, so it is a "
        "default and not a measurement (docs/19 s3)."
        % contract["timeout-seconds"],
        "are the %d declared failure state(s) the ones the implementation "
        "will name? They are a promise made before any code exists. Agent "
        "Validation checks it afterwards, and nine of the first ten agents "
        "failed exactly that check." % len(failures),
    ]
    if dropped:
        unjudged.append(
            "the design uses %d fewer tool(s) than the job asked for - %s. "
            "Narrowing is allowed and may well be right; it is reported "
            "because the job description is what the agent will be measured "
            "against." % (len(dropped), ", ".join("'%s'" % t
                                                  for t in dropped)))

    return {"contract": contract, "dropped": dropped, "unjudged": unjudged,
            "why": "designed for %s from a job with %d capability(ies) and "
                   "%d tool(s). It validates, it adds nothing to the job, "
                   "and it carries no tier and no risk - docs/28 owns those."
                   % (agent_id, len(_listed(job.get("capabilities"))),
                      len(asked_tools))}


def main(argv):
    print("AGENT ARCHITECT   a job description, turned into a contract")
    print("=" * 70)

    job = {"agent-id": None, "name": "Duct Counter",
           "responsibility": "counts ducts in a view",
           "capabilities": ["COUNT_DUCTS"], "dependencies": [],
           "tools": ["heron_lookup"], "department": None,
           "risk-floor": "READ"}

    # A REAL ID, from the register, because the checks below are against the
    # real register and an invented id would prove nothing.
    real = "HERON-AHR-ARC-003"

    cases = [
        ("no job description at all", dict(job=None, agent_id=real)),
        ("a dict that is not HR's",
         dict(job={"name": "Duct Counter"}, agent_id=real)),
        ("no agent id - HR leaves it None",
         dict(job=job, agent_id=None)),
        ("an id the register does not carry",
         dict(job=job, agent_id="HERON-AHR-NOPE-999")),
        ("a tool the job never asked for",
         dict(job=job, agent_id=real, tools=["revit_apply_move"],
              failures=["NO_VIEW"])),
        ("no failure state declared", dict(job=job, agent_id=real)),
        ("designed, using everything the job asked for",
         dict(job=job, agent_id=real, failures=["NO_VIEW", "NOTHING_FOUND"],
              inputs={"view": {"type": "string", "required": True,
                               "description": "The view to count in."}},
              outputs={"count": {"type": "number",
                                 "description": "How many."}})),
        ("designed, needing less than the job asked for",
         dict(job=job, agent_id=real, tools=[], failures=["NOTHING_FOUND"],
              inputs={"view": {"type": "string", "required": True,
                               "description": "The view to count in."}},
              outputs={"count": {"type": "number",
                                 "description": "How many."}})),
    ]

    for label, kwargs in cases:
        answer = design(**kwargs)
        if "refused" in answer:
            print("  %-44s %s" % (label, answer["refused"]))
            print("      %s" % answer["why"][:96])
        else:
            print("  %-44s designed, %d tool(s), %d failure(s)"
                  % (label, len(answer["contract"]["allowed-tools"]),
                     len(answer["contract"]["failures"])))
            for note in answer["unjudged"]:
                print("      unjudged: %s" % note[:92])
    print()
    print("  Every contract above was put through heron_contract.validate()")
    print("  before being returned. A design that does not validate is a")
    print("  refusal here rather than somebody else's failing gate.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
