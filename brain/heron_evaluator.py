# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-EVL-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Evaluator - a correct refusal is not a failure.

    python brain/heron_evaluator.py

WHAT IT IS FOR (docs/28, HERON-AHR-EVL-007)
--------------------------------------------
"After activation: scores real performance against expectations."

Two words in that sentence do the work. AFTER ACTIVATION - so the agent is
in PRODUCTION and its answers are being used, which is the only point at
which "real performance" exists. And AGAINST EXPECTATIONS - which are the
agent's own contract, not a number somebody had in mind.

A CORRECT REFUSAL IS NOT A FAILURE. THIS IS THE WHOLE FILE
-----------------------------------------------------------
HERON-AHR-GAP-001 learned this the expensive way, on real data: the loudest
error in the audit trail was `needs_unbound`, 38 of 176, and it is the
executor behaving exactly as designed. Counting it as a gap would have
commissioned a fragment that already existed.

The same trap is worse here, because a score is a number people act on. An
agent that refuses when it should refuse has done its job. Scoring that as
a failure teaches the next version to attempt what it should decline - and
Heron's whole trust model is built on agents that say no.

A run whose failure state the contract never declared is a DEFECT: the
contract is the promise, and a failure outside it is either a bug or a
promise that was never kept.

AND A DECLARED REFUSAL IS STILL NOT AUTOMATICALLY A CORRECT ONE
----------------------------------------------------------------
The first version of this file called every declared refusal correct, and
that is wrong in the opposite direction. A contract's `failures` list says
which outcomes a CALLER must handle. It does not say the refusal condition
was actually present on any particular request.

So a broken agent that refuses EVERYTHING, always, with one of its own
declared states, produced a perfect report: no defects, every run a correct
refusal. Reading that as health is exactly the failure this file was
written to prevent, arrived at from the other side.

A refusal is VERIFIED only when the run record says why it was warranted -
`refusal-warranted: true` and a `because`. Everything else is an UNVERIFIED
declared refusal: not a defect, and not a credit either. And when every
scored run is one, the report says so in as many words, because an agent
refusing everything and a caller asking for the wrong thing every time
produce the same numbers.

WHAT IT SCORES AGAINST IS THE CONTRACT, NOT AN OPINION
-------------------------------------------------------
Three expectations, all read from brain/agents/<id>.yaml:

    timeout-seconds     a run that took longer overran a promise
    failures            the complete list of ways it said it can fail
    retry.on-failures   what it said was worth trying again

Nothing is compared against a target somebody typed. If the expectation is
wrong, the contract is where it is wrong, and changing it is a versioned
change HERON-AHR-CON-017 will call BREAKING or not.

A DEGRADED RUN IS NOT EVIDENCE
-------------------------------
HERON-KRN-MAV-017 marks an answer degraded when it came from a fallback,
and says a degraded answer is evidence about the fallback rather than about
the work. So degraded runs are counted, reported and EXCLUDED from the
score. An agent judged on runs where a smaller model answered for it is
being judged on somebody else's work.

WHAT IT WILL NOT DO
--------------------
It will not say the agent is GOOD. It counts what happened against what was
promised. Whether the answers were right is not in a run record - that needs
a person or a model, and docs/28 makes this agent T2 with no adapter to call.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/24. "After activation" is this one and no other.
ACTIVATED_STAGE = "PRODUCTION"

# What a run record has to carry before it can be scored at all.
REQUIRED_FIELDS = ("run", "outcome")

# The words that mean it worked. The same three HERON-AHR-DEP-012's PROVEN
# gate reads, because two vocabularies for "it worked" is one vocabulary and
# one bug.
SUCCEEDED = ("OK", "SUCCESS", "COMPLETED")


def _seconds(value):
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def score(agent_id, runs, records=None):
    """
    {report, unjudged, why} for an activated agent - or a refusal.

    `runs` are run records. There is no argument that says how many
    succeeded: a count is not evidence, which is the rule the PROVEN gate
    was rewritten twice to enforce (D-30).
    """
    agent_id = str(agent_id or "").strip()
    if not agent_id:
        return {"refused": "NO_SUCH_AGENT",
                "why": "no agent was named. A score belongs to one agent and "
                       "one contract."}

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

    stage = str(record.get("state") or "").upper()
    if stage != ACTIVATED_STAGE:
        return {"refused": "NOT_ACTIVATED",
                "why": "%s is at %s. This agent scores real performance AFTER "
                       "activation (docs/28), and before PRODUCTION there is "
                       "no real performance - there are test runs, which "
                       "HERON-AHR-VAL-013 already judges."
                       % (agent_id, stage or "no stage")}

    # THE EXPECTATION IS THE CONTRACT. An agent with no contract has
    # promised nothing, and nothing is not a standard to measure against.
    declared = record.get("failures")
    if declared is None or not record.get("contract"):
        return {"refused": "NO_CONTRACT",
                "why": "%s has no contract, so there is no promise to measure "
                       "against. Scoring it would be comparing what happened "
                       "with what somebody assumed." % agent_id}
    declared = set(declared)
    timeout = _seconds(record.get("timeout_seconds"))

    if not runs:
        return {"refused": "NOTHING_TO_SCORE",
                "why": "no runs were given. An agent with no recorded runs is "
                       "not a well-behaved agent and is not a badly behaved "
                       "one - it is unmeasured, and reporting zero would read "
                       "as a result."}

    bad = [r for r in runs if not isinstance(r, dict)
           or any(f not in r for f in REQUIRED_FIELDS)]
    if bad:
        return {"refused": "RUNS_NOT_RECORDS",
                "why": "%d of %d runs are missing %s. A run that cannot say "
                       "which run it was or how it ended cannot be counted "
                       "either way."
                       % (len(bad), len(runs), " or ".join(REQUIRED_FIELDS))}

    honoured, refusals, defects, overran, degraded = [], [], [], [], []
    for run in runs:
        if run.get("degraded"):
            # EVIDENCE ABOUT THE FALLBACK, NOT ABOUT THE WORK
            # (HERON-KRN-MAV-017). Counted and excluded, never dropped.
            degraded.append(run)
            continue

        took = _seconds(run.get("seconds"))
        if timeout is not None and took is not None and took > timeout:
            overran.append({"run": run.get("run"), "seconds": took,
                            "promised": timeout})

        outcome = str(run.get("outcome") or "").strip()
        if outcome.upper() in SUCCEEDED:
            honoured.append(run.get("run"))
        elif outcome in declared:
            # A DECLARED REFUSAL IS NOT AUTOMATICALLY A CORRECT ONE.
            # The contract's failure list says which outcomes a CALLER must
            # handle. It does not say the refusal condition was present on
            # any particular request - and until 2026-09-14 this counted
            # every one of them as correct, which makes an agent that refuses
            # EVERYTHING look perfectly healthy. A refusal is verified only
            # when the run says why it was warranted.
            warranted = run.get("refusal-warranted")
            refusals.append({"run": run.get("run"), "state": outcome,
                             "warranted": bool(warranted) if warranted
                                          is not None else None,
                             "because": run.get("because")})
        else:
            defects.append({"run": run.get("run"), "state": outcome,
                            "declared": sorted(declared)})

    scored = len(runs) - len(degraded)
    verified = [r for r in refusals if r["warranted"] is True]
    unverified = [r for r in refusals if r["warranted"] is not True]
    report = {
        "agent": agent_id,
        "runs": len(runs),
        "scored": scored,
        "completed": honoured,
        "declared_refusals": refusals,
        "verified_refusals": verified,
        "unverified_refusals": unverified,
        "defects": defects,
        "overran_timeout": overran,
        "degraded_excluded": [r.get("run") for r in degraded],
        "expectation": {"timeout-seconds": timeout,
                        "failures": sorted(declared)},
    }

    unjudged = [
        "WHETHER THE ANSWERS WERE RIGHT is not in a run record. This counts "
        "what happened against what was promised; correctness needs a person "
        "or a model, and there is no adapter to call.",
    ]
    if verified:
        unjudged.append(
            "%d run(s) refused with a declared state AND recorded why the "
            "refusal was warranted. Those are the agent working, not failing "
            "- HERON-AHR-GAP-001 found the loudest error in the real audit "
            "trail was the executor behaving correctly, 38 of 176. They are "
            "counted separately and they lower nothing." % len(verified))
    if unverified:
        unjudged.append(
            "%d run(s) refused with a declared state and NOTHING SAYS THE "
            "REFUSAL WAS WARRANTED. The contract's failure list says which "
            "outcomes a caller must handle; it does not say the condition "
            "was present on that request. They are not counted as defects "
            "and they are not counted as the agent working either - a run "
            "record carrying `refusal-warranted` and `because` is what moves "
            "one into either column." % len(unverified))
    if scored and len(unverified) == scored:
        unjudged.append(
            "EVERY SCORED RUN WAS AN UNVERIFIED REFUSAL. An agent that "
            "refuses everything it is asked produces exactly this report, "
            "and so does one whose callers all asked for the wrong thing. "
            "Nothing here separates them, and the report must not be read as "
            "health.")
    if degraded:
        unjudged.append(
            "%d run(s) were DEGRADED and are excluded. A fallback answered, "
            "so they are evidence about the fallback and not about this agent "
            "(HERON-KRN-MAV-017)." % len(degraded))
    if not scored:
        unjudged.append(
            "every run was degraded, so nothing was scored. That is not a "
            "score of zero.")
    if timeout is None:
        unjudged.append(
            "the contract declares no timeout, so no run could overrun one. "
            "Nothing was checked, which is not the same as nothing found.")

    return {"report": report, "unjudged": unjudged,
            "why": "%d run(s): %d completed, %d declared refusal(s) of which "
                   "%d verified, %d defect(s), %d over the promised %s, %d "
                   "degraded and excluded."
                   % (len(runs), len(honoured), len(refusals), len(verified),
                      len(defects), len(overran),
                      "%gs" % timeout if timeout is not None else "(none)",
                      len(degraded))}


def main(argv):
    import heron_agents as REG
    records = REG.records()

    print("AGENT EVALUATOR   a correct refusal is not a failure")
    print("=" * 70)

    agent_id = "HERON-AHR-WFP-015"
    answer = score(agent_id, [{"run": "r1", "outcome": "OK"}],
                   records=records)
    print("  %-42s %s" % ("a real agent, which is at DRAFT",
                          answer.get("refused", "scored")))
    print("      %s" % answer["why"][:96])

    # PRODUCTION is a stage nothing has reached, so it is staged here rather
    # than claimed. The register is not edited to make a demo work.
    staged = dict((k, dict(v)) for k, v in records.items())
    staged[agent_id]["state"] = "PRODUCTION"

    answer = score(agent_id, [], records=staged)
    print("  %-42s %s" % ("activated, with no runs",
                          answer.get("refused", "scored")))
    print("      %s" % answer["why"][:96])

    runs = [
        {"run": "r1", "outcome": "OK", "seconds": 2},
        {"run": "r2", "outcome": "OK", "seconds": 41},
        {"run": "r3", "outcome": "NOTHING_TO_ASSESS", "seconds": 1,
         "refusal-warranted": True, "because": "the proposal had no name"},
        {"run": "r4", "outcome": "REGISTER_UNREADABLE", "seconds": 1},
        {"run": "r5", "outcome": "KeyError", "seconds": 3},
        {"run": "r6", "outcome": "OK", "seconds": 2, "degraded": True},
    ]
    answer = score(agent_id, runs, records=staged)
    report = answer["report"]
    print("  %-42s scored" % "activated, six runs")
    print("      %s" % answer["why"])
    print()
    print("      completed          %s" % ", ".join(report["completed"]))
    print("      refusals, verified %s"
          % (", ".join("%s (%s)" % (r["run"], r["state"])
                       for r in report["verified_refusals"]) or "-"))
    print("      refusals, NOT       %s"
          % (", ".join("%s (%s)" % (r["run"], r["state"])
                       for r in report["unverified_refusals"]) or "-"))
    print("      DEFECTS            %s"
          % ", ".join("%s (%s)" % (d["run"], d["state"])
                      for d in report["defects"]))
    print("      over its timeout   %s"
          % ", ".join("%s (%gs of %gs)"
                      % (o["run"], o["seconds"], o["promised"])
                      for o in report["overran_timeout"]))
    print("      degraded, excluded %s"
          % ", ".join(report["degraded_excluded"]))
    print()
    for note in answer["unjudged"]:
        print("      unjudged: %s" % note[:92])
    print()
    print("  r3 and r4 both ended in states the contract declares. Only r3")
    print("  says WHY the refusal was warranted, so only r3 counts as the")
    print("  agent working; r4 is a declared refusal nothing has verified.")
    print("  r5 ended in a state never declared - the only defect here.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
