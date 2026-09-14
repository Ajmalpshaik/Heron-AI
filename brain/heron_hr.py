# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-HR-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent HR - the job description, and the door Workforce Planning guards.

    python brain/heron_hr.py

WHAT IT IS FOR (docs/28, HERON-AHR-HR-002)
-------------------------------------------
"Writes the job description - responsibility, capabilities, dependencies,
tools."

It is the first step of the hiring lifecycle and the first step of the
factory. Everything the Architect designs, the Builder implements and the
Evaluator later scores is downstream of the sentence written here, so a
vague job description is not a small problem that gets tidied up later - it
is the thing every later step is measured against.

THE DOOR. THIS IS THE WHOLE POINT
----------------------------------
Workforce Planning (HERON-AHR-WFP-015) is the guard against agent
explosion, and a guard with a way around it guards nothing. The way around
it is to skip it: describe the job straight to HR, and the register grows by
one whether or not anything already covered the work.

So HR will not write a job description for a proposal Workforce Planning has
not cleared - and it does not take the caller's word for that. It ASKS
Workforce Planning again, here, about this exact name and this exact
purpose.

Reading a verdict out of a dictionary the caller supplied was the same guard
with the same way round it: `assessment={"verdict": "PROPOSE_HIRING"}` typed
by hand walked straight through, and an assessment of a different proposal
would have too. Re-running costs one pass over a register HR already holds.

An assessment handed in is still read - not for the verdict, but to check it
AGREES. One that says something different from what Workforce Planning says
now is stale or from somewhere else, and ASSESSMENT_DISAGREES says which.

That makes the order of Block 2 and Block 3 load-bearing rather than tidy.
Workforce Planning was built first so that this refusal had something real
to point at.

WHAT HR DOES NOT DECIDE
------------------------
Three things arrive in proposals and are refused rather than dropped:

    tier            docs/28-agent-registry.md owns it
    risk            docs/28-agent-registry.md owns it
    status          the implementing file's Heron-Status header owns it

This is the same rule the agent contract enforces (HERON-AHR-CON-017) and it
is enforced here for the same reason: a field that can be set in two places
is a field that will disagree with itself. Dropping them silently would be
worse than refusing - the caller would believe the value had been taken.

HR also does not invent an agent id. An id is a row in docs/28, and writing
one here would be HR editing the register rather than proposing to it. The
job comes back with `agent-id: None` and says so.

THE RISK FLOOR, WHICH IS DERIVED AND NOT ASSIGNED
--------------------------------------------------
docs/28 assigns risk. But the tools a job asks for put a FLOOR under it: an
agent that calls a MODIFY tool cannot be a READ agent, whatever the register
says. So HR reports the highest risk among the tools requested.

That is a derived number handed to the person who assigns the real one, not
a decision taken from them. It is worth having because the alternative is
assigning risk by reading a sentence, and this repository has a row in
NEEDS-CHECKING for every time reading was trusted over running.

A tool that is not in the table is refused. A job description asking for a
tool nothing provides is a job nobody can do, and the failure would surface
in the Builder or, worse, at runtime.

THE TABLE IS AN INPUT, AND THAT IS THE LAYERING, NOT A SHORTCUT
---------------------------------------------------------------
The table that matters is mcp/server/heron_tools.py, which is the one the
server enforces. `brain` may not depend on `mcp` (D-48), so HR does not read
it - it is handed the table by a caller that is allowed to, and
`tools/check-structure.py` refuses the import that would make this
convenient. The first draft of this file did import it and the gate caught
it in the same minute, which is the gate working rather than an obstacle.

So a job naming tools with NO table supplied is refused with
TOOLS_NOT_CHECKED. Writing it anyway would put a list in the job
description that reads as verified and is not, and "compared against
nothing" has to be a different answer from "compared and found fine".

The risk WORDS are docs/12's vocabulary, which is a documented ladder and
not mcp's property. Their order is declared here because a floor needs an
order; the mapping from a tool to a word comes in with the table.

ONE JOB OR TWO - REPORTED, NOT REFUSED
---------------------------------------
"Counts ducts and reports them" is one job. "Counts ducts and renames
sheets" is two. No splitter can tell those apart, because the difference is
in the meaning and not in the conjunction.

So a responsibility carrying more than one clause comes back in `unjudged`
rather than as a refusal. Workforce Planning made the same call about word
overlap and Agent Validation made it about near-duplicates, both for this
reason: refusing on a measure whose own author does not trust it teaches
callers to phrase around the check rather than to fix the job.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/12's risk ladder, worst last. The boundary that matters sits between
# EXECUTE and MODIFY. Declared here because a floor needs an order; which
# tool sits where arrives with the table, from a caller that may read it.
RISK_LADDER = ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH",
               "ADMIN")

# What HR is not allowed to be told. The value is where it does live.
NOT_HRS = {
    "tier": "docs/28-agent-registry.md",
    "risk": "docs/28-agent-registry.md",
    "status": "the implementing file's Heron-Status header",
}

# The words that join two clauses. Used only to ASK, never to refuse.
JOINERS = (" and ", " then ", "; ", ", and ", " plus ", " as well as ")


def risk_floor(tools, table):
    """
    The highest risk among `tools`, as a word - or None for a job that calls
    nothing. A floor, not an assignment: docs/28 may go higher and may not
    go lower.

    A tool whose risk word is not on docs/12's ladder contributes nothing
    rather than being read as the lowest rung. A word nobody recognises is
    not evidence that a tool is safe.
    """
    order = dict((word, level) for level, word in enumerate(RISK_LADDER))
    levels = [order.get((table or {}).get(t)) for t in (tools or [])]
    levels = [n for n in levels if n is not None]
    return RISK_LADDER[max(levels)] if levels else None


def clauses(purpose):
    """
    The responsibility split on the words that join two clauses. One entry
    means one clause, which is not the same as one job and is not claimed
    to be.
    """
    parts = [str(purpose or "")]
    for joiner in JOINERS:
        out = []
        for part in parts:
            out.extend(part.split(joiner))
        parts = out
    return [p.strip() for p in parts if p.strip()]


def write(name, purpose, assessment=None, capabilities=None,
          dependencies=None, tools=None, department=None,
          agents=None, table=None, **not_hrs):
    """
    {job, unjudged, why} for a cleared proposal - or a refusal.

    `assessment` is optional and is NOT where the verdict comes from.
    Workforce Planning is re-run here on this name and purpose; an
    assessment that arrives is compared against that answer and refused if
    it disagrees. There is no argument that says "cleared", for the same
    reason the deployment ladder has none that says which stage it is in.
    """
    if not str(name or "").strip() or not str(purpose or "").strip():
        return {"refused": "NO_PROPOSAL",
                "why": "a job description needs a name and a responsibility. "
                       "Every later step of the factory is measured against "
                       "the responsibility, so an empty one is not a small "
                       "gap."}

    # THE DOOR, AND THE ASSESSMENT IS RE-RUN RATHER THAN READ.
    # Taking the caller's word for the verdict is the guard with a way round
    # it: {"verdict": "PROPOSE_HIRING"} typed by hand walked straight
    # through until 2026-09-14. Workforce Planning is asked again, HERE,
    # about THIS name and THIS purpose - so the answer cannot belong to a
    # different proposal and cannot be invented.
    import heron_workforce as WFP
    if agents is None:
        import heron_agents as REG
        try:
            agents, _claims, _host = REG._agent_count()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}
    # ONE capability is handed over, and two are not. Workforce Planning's
    # fragment rule is about a capability in ONE OPERATION'S SHAPE (D-29);
    # a job providing two operations is not that shape, and passing the
    # first of them would have the guard answer a question nobody asked.
    named = list(capabilities or [])
    checked = WFP.assess(name, purpose,
                         capability=named[0] if len(named) == 1 else None,
                         department=department, agents=agents)
    if checked.get("refused"):
        return {"refused": "NOT_CLEARED_TO_HIRE",
                "why": "Workforce Planning could not assess this proposal: "
                       "%s. HR writes a job description only for one it "
                       "cleared." % checked.get("why")}
    verdict = checked.get("verdict")

    given = (assessment or {}).get("verdict")
    if given is not None and given != verdict:
        return {"refused": "ASSESSMENT_DISAGREES",
                "why": "the assessment handed in says '%s' and Workforce "
                       "Planning, asked again about this exact name and "
                       "purpose, says '%s'. An assessment that does not "
                       "match the proposal it arrived with is either stale "
                       "or from somewhere else." % (given, verdict)}

    if verdict != "PROPOSE_HIRING":
        return {"refused": "NOT_CLEARED_TO_HIRE",
                "why": "Workforce Planning returned %s. HR writes a job "
                       "description only for a proposal it cleared with "
                       "PROPOSE_HIRING - a guard with a way around it guards "
                       "nothing, and the way around it is to skip it."
                       % ("nothing" if verdict is None else "'%s'" % verdict)}

    # NOT HR'S TO DECIDE. Refused rather than dropped, so a caller cannot
    # believe a value was taken.
    given = sorted(k for k in not_hrs if k in NOT_HRS)
    if given:
        return {"refused": "NOT_HRS_TO_DECIDE",
                "why": "a job description may not carry %s. %s owns %s, and a "
                       "field settable in two places is a field that will "
                       "disagree with itself."
                       % (", ".join(given),
                          NOT_HRS[given[0]],
                          given[0] if len(given) == 1 else "them")}

    unknown = sorted(k for k in not_hrs if k not in NOT_HRS)
    if unknown:
        return {"refused": "NOT_A_JOB_FIELD",
                "why": "a job description has no field %s. Silently keeping "
                       "one would put a value in the record that nothing "
                       "downstream reads." % ", ".join("'%s'" % k
                                                       for k in unknown)}

    capabilities = list(capabilities or [])
    dependencies = list(dependencies or [])
    tools = list(tools or [])

    if not capabilities and not tools:
        return {"refused": "NOTHING_TO_DO",
                "why": "the job provides no capability and calls no tool. "
                       "That is a description of nothing, and the Architect "
                       "downstream would have no surface to design."}

    # EVERY TOOL MUST EXIST, AND "CHECKED AGAINST NOTHING" IS NOT "FINE".
    # brain may not read mcp (D-48), so the table arrives from a caller that
    # may - and no table means the list was compared against nothing.
    if tools and not table:
        return {"refused": "TOOLS_NOT_CHECKED",
                "why": "%d tool(s) were named and no tool table was supplied, "
                       "so they were compared against nothing. brain may not "
                       "read mcp/server/heron_tools.py itself (D-48); the "
                       "caller that may must hand it over." % len(tools)}
    missing = [t for t in tools if t not in table]
    if missing:
        return {"refused": "NO_SUCH_TOOL",
                "why": "%s is not in the tool table supplied. "
                       "A job asking for a tool nothing provides is a job "
                       "nobody can do." % ", ".join("'%s'" % t
                                                    for t in sorted(missing))}

    # EVERY DEPENDENCY MUST BE AN AGENT THAT EXISTS.
    if dependencies:
        if agents is None:
            import heron_agents as REG
            try:
                agents, _claims, _host = REG._agent_count()
            except IOError as exc:
                return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}
        absent = [d for d in dependencies if d not in agents]
        if absent:
            return {"refused": "NO_SUCH_DEPENDENCY",
                    "why": "%s is not in docs/28-agent-registry.md. A job that "
                           "depends on an agent nobody has planned cannot be "
                           "started, and the register is where an agent comes "
                           "into existence."
                           % ", ".join("'%s'" % d for d in sorted(absent))}

    unjudged = []
    found = clauses(purpose)
    if len(found) > 1:
        unjudged.append(
            "the responsibility has %d clauses - %s. Whether that is one job "
            "described fully or two jobs in one sentence needs a reading, and "
            "a splitter cannot make it. Workforce Planning guards against the "
            "second." % (len(found), "; ".join("'%s'" % c for c in found)))

    if not capabilities:
        unjudged.append(
            "no capability is named, so nothing downstream can tell whether "
            "this job duplicates one. The tools it asks for are not a "
            "capability - they are how it would reach one.")

    job = {
        "agent-id": None,
        "name": str(name).strip(),
        "responsibility": str(purpose).strip(),
        "capabilities": sorted(capabilities),
        "dependencies": sorted(dependencies),
        "tools": sorted(tools),
        "department": (str(department).strip() if department else None),
        "risk-floor": risk_floor(tools, table),
    }

    return {"job": job, "unjudged": unjudged,
            "why": "cleared by Workforce Planning, %d capability(ies), %d "
                   "dependency(ies), %d tool(s). The id is None and stays "
                   "None: an id is a row in docs/28, and writing one here "
                   "would be HR editing the register rather than proposing "
                   "to it."
                   % (len(capabilities), len(dependencies), len(tools))}


def main(argv):
    print("AGENT HR   the job description, and the door in front of it")
    print("=" * 70)

    # A PROPOSAL WORKFORCE PLANNING ACTUALLY CLEARS. "Duct Counter /
    # COUNT_DUCTS" is not one: a capability in one operation's shape is a
    # FRAGMENT (D-29), and the first two cases below show the guard saying
    # so through HR rather than around it.
    cleared = {"verdict": "PROPOSE_HIRING"}
    job = dict(name="Duct Sizing Reviewer",
               purpose="reviews duct sizing against the project brief")

    # A SAMPLE, and it is not the real table. The real one is
    # mcp/server/heron_tools.py, which brain may not read (D-48) - a caller
    # that may hands it in, and tests/test_hr.py does exactly that so the
    # checks below are proved against the table the server enforces.
    sample = {"heron_lookup": "READ", "revit_preview_move": "ANALYZE",
              "revit_apply_move": "MODIFY"}

    cases = [
        ("no assessment at all",
         dict(name="Duct Counter", purpose="counts ducts in a view",
              capabilities=["COUNT_DUCTS"])),
        ("Workforce Planning said no",
         dict(name="Duct Counter", purpose="counts ducts in a view",
              capabilities=["COUNT_DUCTS"],
              assessment={"verdict": "THIS_IS_A_FRAGMENT"})),
        ("cleared, but carrying a tier",
         dict(job, assessment=cleared, tools=["heron_lookup"], tier="T2")),
        ("cleared, asking for a tool nobody serves",
         dict(job, assessment=cleared, tools=["revit_delete_everything"])),
        ("cleared, depending on an agent nobody planned",
         dict(job, assessment=cleared, tools=["heron_lookup"],
              dependencies=["HERON-AHR-NOPE-999"])),
        ("cleared, providing nothing and calling nothing",
         dict(job, assessment=cleared)),
        ("cleared, one clause, reading only",
         dict(job, assessment=cleared, tools=["heron_lookup"])),
        ("cleared, two clauses, and it wants to move things",
         dict(name="Duct Sizing Reviewer",
              purpose="reviews duct sizing against the project brief and "
                      "renames the sheets",
              assessment=cleared,
              tools=["heron_lookup", "revit_apply_move"])),
    ]

    for label, kwargs in cases:
        kwargs.setdefault("table", sample)
        answer = write(**kwargs)
        if "refused" in answer:
            print("  %-42s %-22s" % (label, answer["refused"]))
            print("      %s" % answer["why"][:96])
        else:
            job = answer["job"]
            print("  %-42s %-22s risk floor: %s"
                  % (label, "written", job["risk-floor"] or "-"))
            for note in answer["unjudged"]:
                print("      unjudged: %s" % note[:92])
    print()
    print("  The risk floor is DERIVED from the tool table handed in, and")
    print("  handed to whoever assigns the real one in docs/28. It is not a")
    print("  risk assignment, and no job description carries a tier.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
