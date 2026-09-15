# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-PRO-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Learning promotion - one success is not proof, and a thousand is not
either.

    python brain/heron_promotion.py

WHAT IT IS FOR (docs/28, HERON-LRN-PRO-004)
--------------------------------------------
"Walks new knowledge through the lifecycle gates. ONE SUCCESS IS NOT
PROOF, and PRODUCTION still needs a human." T1, risk SUGGEST. Nothing is
promoted here: a verdict comes back.

THE ROW IS RIGHT AND D-30 IS STRONGER THAN IT
-----------------------------------------------
"One success is not proof" suggests the fix is more successes. D-30
answered Q-9 and rejected that outright, with a defect from the real
library:

    "One fragment's record reads: the level chain never tried
    RBS_START_LEVEL_PARAM, so setting a level filter matched ZERO ducts
    AND REPORTED SUCCESS. A fragment that succeeds while doing nothing
    passes ten runs. IT PASSES A THOUSAND. A count measures that nothing
    threw, which is not the property anybody cares about."

So a thousand successes are not proof either, and this agent promotes on
one recorded proof rather than on any number of runs. A candidate
arriving with a run count and no proof is refused, and the count is
echoed back so that nobody mistakes the refusal for not having noticed.

WHAT A PROOF IS, IN D-30's OWN THREE PARTS
--------------------------------------------
    A POSITIVE CASE     it returns what it should.
    A NEGATIVE CASE     it returns NOTHING when it should return
                        nothing. "This is the one that catches SUCCEEDED
                        AND DID NOTHING, and A PROOF WITHOUT IT IS NOT A
                        PROOF."
    A SECOND ROUTE      where one exists. Two mechanisms agreeing, or a
                        number the user can check by eye.

And it is dated, it names the model or the kind of model, and it is
recorded by a person - "whoever ran it records it, UNDER THEIR NAME AND
THE DATE, NOT A TICK".

The negative case is the one this agent refuses without, because D-30
says in as many words that a proof without it is not one.

docs/09 s94 STILL CARRIES THE GATE D-30 REPLACED
--------------------------------------------------
Its proposed table reads "VALIDATED -> PROVEN: N successful real
executions... (N to be set - suggest 10)" and closes "Tracked as Q-9 for
the value of N and who may approve". Q-9 is answered - by D-30, which
rejected the count and named who approves. Recorded as PROPOSALS F21.
This agent follows D-30.

NOTHING IS EVER DELETED
-------------------------
docs/09 s94: "DEPRECATED -> ARCHIVED: after a retention period; NEVER
DELETED". So ARCHIVED is the end of the ladder and there is nothing past
it to move to.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/09 s5's ladder, read from HERON-FRG-VAL-001's own module.
LADDER = FRAG.STATUSES

# The states D-30's proof is the gate for - also read rather than retyped.
NEEDS_PROOF = FRAG.NEEDS_PROOF

# D-30's three parts, and the middle one is the one it says a proof is not
# a proof without.
A_PROOF_CARRIES = (
    ("positive", "a positive case - it returns what it should"),
    ("negative", "a negative case - it returns NOTHING when it should "
                 "return nothing. D-30: this is the one that catches "
                 "succeeded and did nothing, and a proof without it is not "
                 "a proof"),
    ("at", "the date"),
    ("model", "the model, or the kind of model, it was run against"),
    ("by", "who recorded it - under their name, not a tick"),
)

# Words that say a machine recorded it. D-30 asks for a person.
NOT_A_PERSON = ("auto", "automatic", "ci", "pipeline", "script", "bot",
                "model", "inferred", "system")


def _person(who):
    name = str(who or "").strip().lower()
    if not name:
        return False
    return not any(word == name or word in name.split()
                   for word in NOT_A_PERSON)


def promote(candidate, to, proof=None):
    """
    {may_promote, gate, why} - or a refusal. Nothing is promoted.
    """
    if not isinstance(candidate, dict):
        return {"promoted": False, "refused": "NOT_A_CANDIDATE",
                "why": "a candidate is a map with a `status`. A %s is not "
                       "one." % type(candidate).__name__}

    now = str(candidate.get("status") or "").strip().upper()
    want = str(to or "").strip().upper()
    if now not in LADDER:
        return {"promoted": False, "refused": "NOT_A_STATUS",
                "why": "'%s' is not on the ladder: %s. Read from "
                       "HERON-FRG-VAL-001's own module rather than retyped."
                       % (now, " -> ".join(LADDER))}
    if want not in LADDER:
        return {"promoted": False, "refused": "NOT_A_STATUS",
                "why": "'%s' is not on the ladder: %s."
                       % (want, " -> ".join(LADDER))}

    here, there = LADDER.index(now), LADDER.index(want)
    if there <= here:
        return {"promoted": False, "refused": "NOT_A_PROMOTION",
                "why": "'%s' is at '%s' and this would %s. A fragment only "
                       "ever moves ALONG this list - docs/09 s5 - and "
                       "moving back would lose the record of how far it "
                       "got."
                       % (candidate.get("id") or "the candidate", now,
                          "stay there" if there == here
                          else "move it back to '%s'" % want)}
    if there - here > 1:
        return {"promoted": False, "refused": "SKIPS_A_STATE",
                "why": "'%s' to '%s' skips %s. Each state has its own gate, "
                       "and skipping one is skipping the gate rather than "
                       "passing it faster."
                       % (now, want,
                          ", ".join("'%s'" % LADDER[step]
                                    for step in range(here + 1, there)))}

    runs = candidate.get("successful_runs")
    if want not in NEEDS_PROOF:
        return {"promoted": False, "may_promote": True, "from": now,
                "to": want, "gate": "no proof is needed below '%s'"
                % NEEDS_PROOF[0],
                "why": "'%s' to '%s' does not need D-30's proof - only %s "
                       "do. Nothing was promoted."
                       % (now, want, " and ".join(NEEDS_PROOF)),
                "unjudged": _unjudged(runs, None)}

    if not isinstance(proof, dict):
        return {"promoted": False, "refused": "NO_PROOF",
                "successful_runs": runs,
                "why": "'%s' needs a recorded proof and none was given.%s "
                       "D-30: 'a fragment that succeeds while doing nothing "
                       "passes ten runs. It passes a thousand.' A count "
                       "measures that nothing threw, which is not the "
                       "property anybody cares about."
                       % (want,
                          " %s successful run(s) were reported and they are "
                          "not the gate." % runs if runs else "")}

    missing = [field for field, _why in A_PROOF_CARRIES
               if not str(proof.get(field) or "").strip()]
    if "negative" in missing:
        return {"promoted": False, "refused": "NO_NEGATIVE_CASE",
                "also_missing": [field for field in missing
                                 if field != "negative"],
                "why": "the proof has no negative case. D-30, in as many "
                       "words: 'this is the one that catches SUCCEEDED AND "
                       "DID NOTHING, and A PROOF WITHOUT IT IS NOT A "
                       "PROOF.' It is refused before anything else the "
                       "proof is missing, because the others make a proof "
                       "incomplete and this one makes it not a proof."}
    if missing:
        return {"promoted": False, "refused": "AN_INCOMPLETE_PROOF",
                "missing": missing,
                "why": "the proof carries no %s. %s"
                       % (", ".join(missing),
                          " ".join(why for field, why in A_PROOF_CARRIES
                                   if field in missing))}
    if not _person(proof.get("by")):
        return {"promoted": False, "refused": "AN_INCOMPLETE_PROOF",
                "missing": ["by"],
                "why": "'%s' recorded the proof, which is not a person. "
                       "D-30: 'whoever ran it records it, under their name "
                       "and the date, NOT A TICK.' A proof travels with the "
                       "fragment as evidence so a later reader can JUDGE it "
                       "rather than trust it, and a machine's name gives "
                       "them nothing to judge." % proof.get("by")}

    second = str(proof.get("second_route") or "").strip()
    return {
        "promoted": False, "may_promote": True, "from": now, "to": want,
        "gate": "D-30's recorded proof",
        "proof": {"at": str(proof["at"]).strip(),
                  "model": str(proof["model"]).strip(),
                  "by": str(proof["by"]).strip(),
                  "second_route": second or None},
        "why": "'%s' to '%s' on one recorded proof: positive, negative, %s, "
               "dated %s, against %s, recorded by %s. Nothing was promoted."
               % (now, want,
                  "a second route (%s)" % second if second
                  else "no second route offered",
                  proof["at"], proof["model"], proof["by"]),
        "unjudged": _unjudged(runs, second),
    }


def _unjudged(runs, second):
    """The same four, whatever the verdict."""
    return [
        "NOTHING WAS PROMOTED. A verdict comes back and a caller records "
        "the move.",
        "%s" % ("%s SUCCESSFUL RUN(S) WERE REPORTED AND COUNTED FOR "
                "NOTHING. D-30 rejected the count: a fragment that succeeds "
                "while doing nothing passes ten runs and it passes a "
                "thousand. Runs after the first add confidence; they are "
                "not the gate." % runs if runs else
                "NO RUN COUNT WAS OFFERED, AND IT WOULD NOT HAVE HELPED. "
                "D-30 promotes on one recorded proof, not on any number of "
                "runs."),
        "%s" % ("A SECOND ROUTE WAS GIVEN (%s). D-30 asks for one WHERE ONE "
                "EXISTS - two mechanisms agreeing, or a number the user can "
                "check by eye." % second if second else
                "NO SECOND ROUTE WAS OFFERED. D-30 asks for one where one "
                "exists, so its absence is not a refusal - but a reader "
                "judging this proof has one fewer thing to judge it by."),
        "docs/09 s94's TABLE STILL ASKS FOR N SUCCESSFUL EXECUTIONS AND "
        "STILL SAYS 'TRACKED AS Q-9'. Q-9 is answered - by D-30, which "
        "rejected the count and named who approves. This agent follows "
        "D-30. PROPOSALS F21.",
    ]


def main(argv):
    print("LEARNING PROMOTION   one success is not proof, and a thousand")
    print("                     is not either")
    print("=" * 72)

    print("\nthe ladder, read from HERON-FRG-VAL-001")
    print("  %s" % " -> ".join(LADDER))
    print("  proof is the gate for: %s" % ", ".join(NEEDS_PROOF))

    good = {"id": "FRG-SEL-001", "status": "VALIDATED",
            "successful_runs": 1000}
    proof = {"positive": "returns the 47 ducts", "negative": "returns 0 for "
             "a category with none", "at": "2026-09-15",
             "model": "Tower A MEP", "by": "Ajmal",
             "second_route": "count checked by eye against the schedule"}

    answer = promote(good, "PROVEN", proof=proof)
    print("\n%s" % answer["why"])

    print("\nrefused")
    for candidate, to, p in (
            (good, "PROVEN", None),
            (good, "PROVEN", dict(proof, negative="")),
            (good, "PROVEN", dict(proof, model="")),
            (good, "PROVEN", dict(proof, by="CI")),
            (good, "PRODUCTION", proof),
            ({"status": "PRODUCTION"}, "DRAFT", None),
            ({"status": "ARCHIVED"}, "PRODUCTION", None)):
        bad = promote(candidate, to, proof=p)
        print("  %-24s %s" % (bad["refused"], bad["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
