# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-OPT-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Optimizer - it may not improve the report by weakening the standard.

    python brain/heron_optimizer.py

WHAT IT IS FOR (docs/28, HERON-AHR-OPT-009)
--------------------------------------------
"Improves an existing agent." Risk SUGGEST, and that word is the design:
it proposes, and it changes nothing. Every return value here is a proposal
with a reason attached, for a person to accept or refuse.

THE FAILURE MODE THIS FILE IS BUILT AROUND
-------------------------------------------
An optimizer reads the Evaluator's report and proposes changes. The report
is how its own work is judged. So there are two ways to make the next report
better, and only one of them is an improvement:

    make the agent do better        the work
    make the standard easier        the shortcut

The shortcut is not hypothetical and it is not subtle in hindsight. Both of
these would clear every complaint in a report, permanently, without a line
of the agent changing:

    runs overran timeout-seconds    ->  raise timeout-seconds
    runs failed with an undeclared
    state, counted as DEFECTS       ->  add that state to `failures`

The second one is the worse of the two, because it works perfectly. A defect
is a failure outside the contract's promise; declare the state and the same
failure becomes a CORRECT REFUSAL in the next report, and the defect does
not reappear. The bug has not been fixed; it has been promised.

So this agent will not propose either. It says so, out loud, in
`refused_to_propose` - because a rule that is silently obeyed cannot be
checked, and the next hand to touch this file needs to know the omission
was deliberate. R-55 is the same rule for retrieval: a threshold is never
moved to make a report look better.

Both changes may well be RIGHT. A timeout set too low is a real defect in a
contract, and a failure state that genuinely belongs in the promise should
be declared. Neither judgement belongs to the agent whose score improves
when it is made. They go to a person, named as contract changes.

IT PROPOSES AGAINST EVIDENCE, NOT OPINION
------------------------------------------
The input is HERON-AHR-EVL-007's report - real runs of an activated agent.
Without one there is nothing here but taste, and an optimizer working from
taste rewrites code that was fine.

EVERY CONTRACT CHANGE CARRIES ITS COMPATIBILITY VERDICT
--------------------------------------------------------
A proposal that touches the contract is put through
heron_contract.compare() and comes back carrying IDENTICAL, COMPATIBLE or
BREAKING. An "improvement" that quietly breaks every caller is the thing a
version number exists to prevent, and a proposal without the verdict leaves
the person deciding to work it out themselves.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What an evaluation has to carry before anything here can read it.
REPORT_FIELDS = ("agent", "runs", "defects", "overran_timeout",
                 "expectation")


def _proposal(what, why, change=None, verdict=None):
    return {"change": what, "why": why, "contract-change": change,
            "compatibility": verdict}


def propose(agent_id, report, records=None, deals=None):
    """
    {proposals, refused_to_propose, unjudged, why} - or a refusal.

    Nothing is written and nothing is applied. Risk SUGGEST means every
    return value is a sentence for a person to act on.
    """
    agent_id = str(agent_id or "").strip()
    if not agent_id:
        return {"refused": "NO_SUCH_AGENT",
                "why": "no agent was named. An improvement belongs to one "
                       "agent and one contract."}

    if not isinstance(report, dict):
        return {"refused": "NO_REPORT",
                "why": "improvement is proposed against HERON-AHR-EVL-007's "
                       "report of real runs. Without one there is nothing "
                       "here but taste, and taste rewrites code that was "
                       "fine."}

    absent = [f for f in REPORT_FIELDS if f not in report]
    if absent:
        return {"refused": "NOT_AN_EVALUATION",
                "why": "the report is missing %s, so it did not come from the "
                       "Evaluator. The fields are what every proposal below "
                       "is derived from."
                       % ", ".join("'%s'" % f for f in absent)}

    if str(report.get("agent") or "").strip() != agent_id:
        return {"refused": "REPORT_IS_NOT_THIS_AGENT",
                "why": "the report is about %s and the request is about %s. "
                       "Improving one agent from another's runs is how a fix "
                       "lands in the wrong file."
                       % (report.get("agent"), agent_id)}

    if records is None:
        import heron_agents as REG
        try:
            records = REG.records()
        except IOError as exc:
            return {"refused": "REGISTER_UNREADABLE", "why": str(exc)}

    record = records.get(agent_id)
    if not record:
        return {"refused": "NO_SUCH_AGENT",
                "why": "%s is not in docs/28-agent-registry.md." % agent_id}

    defects = list(report.get("defects") or [])
    overran = list(report.get("overran_timeout") or [])
    refusals = list(report.get("declared_refusals") or [])
    expectation = report.get("expectation") or {}

    proposals = []
    refused_to_propose = []

    # A DEFECT IS FIXED IN THE CODE. The alternative - declaring its state
    # in `failures` - makes the same failure read as a correct refusal next
    # time, and the defect never appears again. That is the shortcut this
    # file exists to refuse.
    for defect in defects:
        state = defect.get("state")
        proposals.append(_proposal(
            "handle %s in the implementation, or fail with a state the "
            "contract already declares" % state,
            "run %s ended in '%s', which %s never promised. A caller "
            "handling failures exhaustively has no branch for it."
            % (defect.get("run"), state, agent_id)))
        refused_to_propose.append({
            "change": "add '%s' to the contract's `failures`" % state,
            "why": "it would clear this defect without changing a line of "
                   "the agent: the same failure would read as a CORRECT "
                   "REFUSAL in the next report and never appear again. The "
                   "bug would not be fixed, it would be promised. If '%s' "
                   "genuinely belongs in the promise, that is a person's "
                   "call and a versioned contract change - not this agent's, "
                   "whose score improves when it is made." % state})

    # THE SAME SHAPE, ONE FIELD ALONG.
    if overran:
        worst = max(overran, key=lambda o: o.get("seconds") or 0)
        proposals.append(_proposal(
            "reduce the work in the slow path",
            "%d run(s) went past the promised %ss - the worst was %s at %ss."
            % (len(overran), expectation.get("timeout-seconds"),
               worst.get("run"), worst.get("seconds"))))
        refused_to_propose.append({
            "change": "raise `timeout-seconds` to %s"
                      % (worst.get("seconds")),
            "why": "it would clear every overrun in the next report without "
                   "the agent getting any faster. A timeout set too low is a "
                   "real defect in a contract and raising it may well be "
                   "right - but it is a person's call and a versioned "
                   "contract change, not a decision for the agent the number "
                   "judges. R-55 is the same rule for a retrieval threshold."})

    # AN IMPROVEMENT THAT IS ACTUALLY AN IMPROVEMENT.
    retry = (record.get("retry") or {})
    retryable = set(retry.get("on-failures") or [])
    repeated = {}
    for refusal in refusals:
        state = refusal.get("state")
        repeated[state] = repeated.get(state, 0) + 1
    for state, count in sorted(repeated.items()):
        if count > 1 and state not in retryable:
            # A RETRY IS NOT PROPOSED, AND ARTICLE 25 IS WHY. "Do not retry a
            # genuine failure. Transport faults may be retried with backoff.
            # An operation that actually failed goes to failure analysis."
            # A declared refusal is a genuine failure until something proves
            # the operation never ran, and nothing in a run record proves
            # that - D-21 makes that classification a table's job and makes
            # it FAIL CLOSED. Proposing `retry.on-failures` for a state like
            # a rejected write would offer to repeat an operation whose
            # outcome is unknown, which is the failure mode D-21 was written
            # about.
            proposals.append(_proposal(
                "look at why '%s' is being reached %d times" % (state, count),
                "a refusal repeated is sometimes a caller asking the wrong "
                "thing repeatedly, and sometimes the agent refusing work it "
                "should do. Either way it is a reading, and neither is "
                "fixed by trying again."))
            refused_to_propose.append({
                "change": "add '%s' to `retry.on-failures`" % state,
                "why": "Constitution article 25 - do not retry a genuine "
                       "failure. Only a transport fault may be retried, and "
                       "nothing in a run record distinguishes one from an "
                       "operation that ran and failed. D-21 makes that "
                       "classification a table's job and makes it fail "
                       "closed, so proposing a retry here would offer to "
                       "repeat an operation whose outcome is unknown. If "
                       "'%s' is provably never executed, a person says so "
                       "in the failure table, not this agent." % state})

    if not proposals:
        return {"refused": "NOTHING_TO_IMPROVE",
                "why": "%d run(s), no defects and nothing over the promised "
                       "time. There is nothing here to propose, and "
                       "proposing something anyway is how a working agent "
                       "gets rewritten." % report.get("runs")}

    # EVERY CONTRACT CHANGE CARRIES ITS VERDICT.
    import heron_contract as CON
    if deals is None:
        deals = CON.contracts()
    current = None
    for _path, data in deals:
        if isinstance(data, dict) and data.get("agent") == agent_id:
            current = data
            break
    for proposal in proposals:
        if proposal["contract-change"] and current:
            after = dict(current)
            after.update(proposal["contract-change"])
            verdict, _reasons = CON.compare(current, after)
            proposal["compatibility"] = verdict

    unjudged = [
        "IS ANY OF THIS WORTH DOING? Nothing here has read the agent's code. "
        "Each proposal is derived from a report, and whether the fix is "
        "cheap, correct or wanted is a reading - docs/28 makes this agent T2 "
        "and there is no adapter to call.",
        "nothing was changed and nothing was written. Risk SUGGEST means "
        "these are sentences for a person (Golden Rule 7).",
    ]
    if refused_to_propose:
        unjudged.append(
            "%d change(s) were REFUSED rather than proposed, and they are "
            "listed. Each would have improved the next report without "
            "improving the agent. They may still be right; they are not this "
            "agent's to decide." % len(refused_to_propose))

    return {"proposals": proposals, "refused_to_propose": refused_to_propose,
            "unjudged": unjudged,
            "why": "%d proposal(s) from %d run(s); %d change(s) refused as "
                   "moving the standard rather than meeting it."
                   % (len(proposals), report.get("runs"),
                      len(refused_to_propose))}


def main(argv):
    import heron_agents as REG
    records = REG.records()
    agent_id = "HERON-AHR-WFP-015"
    declared = sorted(records[agent_id]["failures"])
    promised = float(records[agent_id]["timeout_seconds"])

    print("AGENT OPTIMIZER   it will not move the line it is measured by")
    print("=" * 70)

    clean = {"agent": agent_id, "runs": 3, "scored": 3,
             "completed": ["a", "b", "c"], "declared_refusals": [],
             "defects": [], "overran_timeout": [], "degraded_excluded": [],
             "expectation": {"timeout-seconds": promised,
                             "failures": declared}}
    answer = propose(agent_id, clean, records=records)
    print("  %-42s %s" % ("a clean report",
                          answer.get("refused", "proposed")))
    print("      %s" % answer["why"][:96])

    messy = dict(clean, runs=6,
                 defects=[{"run": "r5", "state": "KeyError",
                           "declared": declared}],
                 overran_timeout=[{"run": "r2", "seconds": promised + 11,
                                   "promised": promised}],
                 declared_refusals=[{"run": "r3", "state": declared[0]},
                                   {"run": "r4", "state": declared[0]}])
    answer = propose(agent_id, messy, records=records)
    print("  %-42s proposed" % "a report with a defect and an overrun")
    print("      %s" % answer["why"])
    print()
    print("    PROPOSED")
    for proposal in answer["proposals"]:
        print("      - %s" % proposal["change"])
        print("          %s" % proposal["why"][:88])
        if proposal["compatibility"]:
            print("          contract change: %s"
                  % proposal["compatibility"])
    print()
    print("    REFUSED TO PROPOSE")
    for refused in answer["refused_to_propose"]:
        print("      - %s" % refused["change"])
        print("          %s" % refused["why"][:88])
    print()
    print("  Both refused changes would have cleared the report completely.")
    print("  Neither would have changed a line of the agent. Both may still")
    print("  be right, and neither is decided by the thing they flatter.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
