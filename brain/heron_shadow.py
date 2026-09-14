# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-SHD-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Shadow Execution - the product is a disagreement log, never an agreement rate.

    python brain/heron_shadow.py

WHAT IT IS FOR (docs/28, HERON-OPS-SHD-012)
--------------------------------------------
"The harness, not the teacher. Runs a candidate in parallel with the
production one, captures both results, and guarantees the candidate can
modify nothing. Works for fragments and workflows, not only agents." T1,
risk READ.

D-39 ANSWERED THE HARD HALF, AND THE ANSWER IS NOT A NUMBER
-------------------------------------------------------------
Q-29 asked how many shadow runs earn a promotion. D-39 refused the
question: **not a count, for the same reason D-30 is not a count.**

  "Agreement is weak evidence. Two implementations can be wrong in the
  same way - they often are, because the second was written by someone
  who read the first. And an agent that silently does nothing agrees with
  everything. A hundred agreements prove less than one disagreement
  somebody sat down and explained."

So promotion out of shadow needs an analysed DISAGREEMENT - what differed,
which was right, and why - plus a human signature. Where none occurred, a
stated reason that is **not** "it always matched": too few runs, a case
never exercised, an input the shadow could not see.

WHICH IS WHY THERE IS NO PERCENTAGE IN THIS MODULE
----------------------------------------------------
D-39: "a percentage score would invite a threshold, and a threshold is the
thing D-33 already refused - it is a number somebody invents and a later
session tunes." The log counts what happened, because a count of recorded
runs is arithmetic. It never divides one by the other, and the verdict
never reads a total. An agreement rate here would be a threshold waiting
for somebody to write it.

AN AGENT THAT NEVER DISAGREES IS A FINDING, NOT A PASS
-------------------------------------------------------
It is either not running, not seeing the same inputs, or a copy of the
thing it shadows - and each is worth knowing BEFORE promotion. A shadow
log of two hundred agreements is refused here, loudly, with those three
possibilities named.

"GUARANTEES THE CANDIDATE CAN MODIFY NOTHING" IS NOT THIS AGENT'S TO GIVE
--------------------------------------------------------------------------
The register says the harness guarantees it. Nothing in this repository
can: Q-56 is open about what actually contains an agent while it runs, and
the Agent Sandbox watches rather than contains. So this agent does what it
honestly can - it states the constraints that apply to a shadow run and
REFUSES to authorise one whose constraints nothing is enforcing. It starts
no process and executes neither side. A harness that also ran the
candidate would be answering Q-56 by accident, which is the same line
HERON-OPS-HEA-006 draws.

THE CONSTRAINTS COMPOSE, THEY DO NOT COMPETE
----------------------------------------------
Q-29's table is keyed on two different axes at once - tier for how a
candidate is exercised, risk for what it may touch. A T3 agent with MODIFY
needs BOTH rows: preview only, never a transaction, AND generated in a
sandbox, never promoted. Picking one row and calling it the answer would
drop the other constraint silently, so the answer is the whole set that
applies - and a candidate matching no row at all is refused.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Q-29's table, accepted as written by D-39. Keyed by what each row is
# really about, because the table keys on two axes at once.
BY_TIER = {
    "T1": ("run it normally and compare its output against the production "
           "agent's - a read-only candidate can be exercised exactly as the "
           "real one is"),
    "T2": ("run it in parallel and log BOTH results. Not an agreement "
           "score: D-39 refused the rate, because a percentage invites a "
           "threshold and a threshold is a number somebody invents"),
    "T3": ("generate and test it in the sandbox, and NEVER promote it to "
           "PRODUCTION from a shadow run - Golden Rule 18, generated code "
           "never touches a live model on its first run"),
}

BY_RISK = {
    "MODIFY": ("produce the PREVIEW only and never open a transaction. "
               "Compare what it intended to change against what the "
               "production path actually did"),
}

# Risk levels above MODIFY are not in Q-29's table at all, and this agent
# does not extend somebody else's table. Named so the refusal can say that
# rather than only that the row is missing.
NOT_IN_THE_TABLE = ("EXECUTE", "PUBLISH", "ADMIN")

# What a disagreement has to say before it counts as analysed (D-39).
# `explanation`, not `why`: compare() already writes a `why` - the
# MACHINE's sentence about what it saw - and reading that as the person's
# analysis would let a raw comparison count as two-thirds examined the
# moment it was made. Two meanings behind one key is how a gate stops
# being a gate.
ANALYSED = (
    ("differed", "what actually differed between the two results"),
    ("right", "which one was right"),
    ("explanation", "why - the sentence somebody sat down and wrote, which "
                    "is not the `why` compare() wrote about what it saw"),
)

# The one reason D-39 rules out by name.
IT_ALWAYS_MATCHED = ("always matched", "always agreed", "never differed",
                     "no differences", "it matched", "they matched",
                     "identical every time", "same every time")


def constraints(tier=None, risk=None):
    """
    {constraints, why} - how this candidate is shadowed, or a refusal.

    The rows COMPOSE. A T3 candidate with MODIFY gets both, because
    dropping either one drops a real constraint silently.
    """
    tier = str(tier or "").strip().upper()
    risk = str(risk or "").strip().upper()

    found = []
    if tier in BY_TIER:
        found.append({"from": "tier %s" % tier, "rule": BY_TIER[tier]})
    if risk in BY_RISK:
        found.append({"from": "risk %s" % risk, "rule": BY_RISK[risk]})

    if risk in NOT_IN_THE_TABLE:
        return {"refused": "SHADOW_BEHAVIOUR_UNDEFINED",
                "why": "Q-29's table covers read-only, analysis, MODIFY and "
                       "code generation. %s is none of them, and this agent "
                       "does not extend somebody else's table - what "
                       "'observe without modifying' means for an agent that "
                       "can %s is a decision, not a gap to fill in."
                       % (risk, risk.lower()),
                "proposal": "answer it in docs/18 s4 and Q-29 first. A "
                            "shadow run nobody has defined is a real run "
                            "with a quieter name."}
    if not found:
        return {"refused": "SHADOW_BEHAVIOUR_UNDEFINED",
                "why": "nothing says how to shadow a candidate at tier %r "
                       "with risk %r. Q-29's table is keyed on both, and a "
                       "candidate matching no row is not a candidate this "
                       "agent guesses about." % (tier or None, risk or None),
                "proposal": "give the tier (T1, T2 or T3) and the risk from "
                            "docs/28. Both come from the register row, which "
                            "is where they are already written."}

    return {"constraints": found,
            "why": "%s, and they compose - dropping either drops a real "
                   "constraint silently."
                   % ("1 rule applies" if len(found) == 1
                      else "%d rules apply" % len(found)),
            "unjudged": [
                "NOTHING HERE ENFORCES THESE. Q-56 is open about what "
                "actually contains an agent while it runs, and the Agent "
                "Sandbox watches rather than contains. This states the "
                "constraints and judges the results; it starts no process "
                "and executes neither side.",
                "'guarantees the candidate can modify nothing' is the "
                "register's wording and is not this agent's to give. What "
                "it can do is refuse a shadow run whose rules nobody has "
                "written - which is what it just did not have to do.",
            ]}


def compare(production, candidate, at=None, on=None):
    """
    One log entry: agreed, or differed and in what way.

    It records the difference. It does not say which was RIGHT - that is
    the sentence a person writes, and a machine picking a winner here is
    the whole failure D-39 is guarding against.
    """
    if not isinstance(production, dict) or not isinstance(candidate, dict):
        return {"refused": "NOT_A_RUN_PAIR",
                "why": "a shadow entry is two results from the same input. "
                       "One side missing is not a quiet agreement - it is a "
                       "run that did not happen, and those are different."}
    agreed = production == candidate
    differed = sorted(set(list(production) + list(candidate)))
    differed = [key for key in differed
                if production.get(key) != candidate.get(key)]
    return {"agreed": agreed, "differed": differed, "at": at, "on": on,
            "production": production, "candidate": candidate,
            "why": ("both produced the same result"
                    if agreed else
                    "they differed on %s. WHICH ONE WAS RIGHT is not "
                    "recorded here - that is the sentence a person writes, "
                    "and a machine picking a winner is the failure D-39 "
                    "guards against." % ", ".join(differed))}


def verdict(log, reason=None, signature=None):
    """
    {promote, why, unjudged} - may this candidate leave shadow?

    D-39, in two conditions and not one number: at least one disagreement
    examined and explained, or a stated reason that is not "it always
    matched" - and a human signature either way.
    """
    if not isinstance(log, list) or not log:
        return {"promote": False, "refused": "NO_RUNS",
                "why": "an empty shadow log is not a clean record. Nothing "
                       "ran, so nothing was observed, and 'no disagreements' "
                       "is what that looks like from the outside."}

    agreements, disagreements, unanalysed = 0, [], []
    for index, entry in enumerate(log):
        if not isinstance(entry, dict) or "agreed" not in entry:
            return {"promote": False, "refused": "NOT_A_RUN_PAIR",
                    "why": "entry %d is not a comparison. A log this agent "
                           "did not write is a log it cannot read."
                           % (index + 1)}
        if entry.get("agreed"):
            agreements += 1
            continue
        missing = [what for field, what in ANALYSED
                   if not str(entry.get(field) or "").strip()]
        if missing:
            unanalysed.append((index + 1, missing))
        else:
            disagreements.append(entry)

    if unanalysed:
        return {"promote": False, "refused": "NOT_ANALYSED",
                "why": "%d disagreement%s recorded without being examined. "
                       "D-39 asks for what differed, which was right, and "
                       "why - a difference nobody explained is the same "
                       "evidence as no difference at all."
                       % (len(unanalysed),
                          "" if len(unanalysed) == 1 else "s"),
                "missing": [{"entry": index, "wants": wants}
                            for index, wants in unanalysed]}

    if not disagreements:
        said = str(reason or "").strip()
        if not said:
            return {"promote": False, "refused": "NEVER_DISAGREED",
                    "why": "%d run%s and not one disagreement. That is a "
                           "FINDING, not a pass: the candidate is either "
                           "not running, not seeing the same inputs, or a "
                           "copy of the thing it shadows (D-39). Each is "
                           "worth knowing before promotion, not after."
                           % (agreements, "" if agreements == 1 else "s"),
                    "proposal": "investigate which of the three it is, then "
                                "say so. A reason is accepted here - too few "
                                "runs, a case never exercised, an input the "
                                "shadow could not see. 'It always matched' "
                                "is not one."}
        for excuse in IT_ALWAYS_MATCHED:
            if excuse in said.lower():
                return {"promote": False,
                        "refused": "REASON_IS_THAT_IT_MATCHED",
                        "why": "'%s' is the one reason D-39 rules out by "
                               "name. Agreement is the observation being "
                               "explained, so it cannot also be the "
                               "explanation." % said,
                        "proposal": "too few runs, a case never exercised, "
                                    "or an input the shadow could not see - "
                                    "one of those, or find out which."}

    signed = str((signature or {}).get("by") or "").strip() \
        if isinstance(signature, dict) else ""
    when = str((signature or {}).get("at") or "").strip() \
        if isinstance(signature, dict) else ""
    if not signed or not when:
        return {"promote": False, "refused": "NOT_SIGNED",
                "why": "evidence plus a name, and there is no name. Golden "
                       "Rule 7 - no agent approves itself - and D-39 asks "
                       "for a human signature whether the evidence is a "
                       "disagreement or a reason there was none."}

    return {
        "promote": True, "signed_by": signed, "at": when,
        "analysed": len(disagreements), "agreements": agreements,
        "reason": str(reason or "").strip() or None,
        "why": "%s. %s signed at %s."
               % ("%d disagreement%s examined and explained"
                  % (len(disagreements),
                     "" if len(disagreements) == 1 else "s")
                  if disagreements else
                  "no disagreement occurred, and the stated reason is not "
                  "that it always matched", signed, when),
        "unjudged": [
            "THERE IS NO RATE HERE. The counts are arithmetic on recorded "
            "runs; nothing divides one by the other. D-39: a percentage "
            "invites a threshold, and a threshold is a number somebody "
            "invents and a later session tunes.",
            "%d agreement%s were counted and they are NOT evidence. "
            "They say the candidate ran and produced something; two "
            "implementations are often wrong the same way, because the "
            "second was written by somebody who read the first."
            % (agreements, "" if agreements == 1 else "s"),
            "this says the candidate may LEAVE SHADOW. What stage it goes "
            "to, and what that stage needs, is the deployment ladder's "
            "(HERON-AHR-DEP-013) and not this agent's.",
        ],
    }


def main(argv):
    print("SHADOW EXECUTION   a disagreement log, never an agreement rate")
    print("=" * 72)

    print("  How a candidate is shadowed (Q-29's table, accepted by D-39):")
    for tier, risk in (("T1", "READ"), ("T2", "ANALYZE"), ("T3", "MODIFY"),
                       ("T1", "ADMIN"), (None, None)):
        answer = constraints(tier=tier, risk=risk)
        print("    %-12s %s" % ("%s / %s" % (tier, risk),
                                answer.get("refused") or answer["why"]))
        for rule in answer.get("constraints", []):
            print("        %-10s %s" % (rule["from"], rule["rule"][:62]))

    print()
    print("  A hundred agreements prove less than one explained difference:")
    log = [compare({"count": 247}, {"count": 247}) for _ in range(199)]
    answer = verdict(log)
    print("    199 agreements    %s" % answer["refused"])
    print("      %s" % answer["why"][:88])
    answer = verdict(log, reason="it always matched")
    print("    ...with a reason  %s" % answer["refused"])
    print("      %s" % answer["why"][:88])
    answer = verdict(log, reason="only view-plan cases ran; nothing "
                                 "exercised a section",
                     signature={"by": "ajmal", "at": "2026-09-14T12:00Z"})
    print("    ...a real reason  promote=%s" % answer["promote"])

    print()
    print("  And the one difference, examined:")
    entry = compare({"count": 247, "kind": "duct"},
                    {"count": 261, "kind": "duct"}, on="align-mep-elevation")
    print("    %s" % entry["why"][:92])
    answer = verdict([entry])
    print("    unexamined        %s - %s"
          % (answer["refused"], answer["missing"][0]["wants"][0][:46]))
    entry.update({"right": "production",
                  "explanation": "the candidate re-read after the user's "
                                 "edit and counted 14 ducts a link had "
                                 "reloaded"})
    answer = verdict([entry], signature={"by": "ajmal", "at": "T"})
    print("    examined, signed  promote=%s  %s"
          % (answer["promote"], answer["why"][:56]))
    print()
    print("  No field in that answer is a rate, and none ever will be:")
    print("    %s" % ", ".join(sorted(answer)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
