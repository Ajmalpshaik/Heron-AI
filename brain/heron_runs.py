# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-PRF-002
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Fragment performance - the same arithmetic as a skill's, and the three
findings only a fragment can have.

    python brain/heron_runs.py

WHAT IT IS FOR (docs/28, HERON-FRG-PRF-002)
--------------------------------------------
"Success, failure, timing, user corrections, error frequency - PER REVIT
VERSION." T1, risk READ.

THAT ROW IS HERON-SKL-PRF-006's ROW, ONE LAYER DOWN
-----------------------------------------------------
Word for word, nearly: the same list of aggregates with the same
qualifier in the middle of it. The subject differs - fragments here,
skills there - but the MEASUREMENT does not, and a second
implementation of "group by release and never aggregate" would be a
second chance for one of them to start averaging.

So the counting is IMPORTED. What this file adds is the three questions
that only make sense about a fragment, because a fragment has two things
a skill has not: a PROOF, and a row in the compatibility matrix.

1. A PROOF THAT THE RUNS CONTRADICT
-------------------------------------
D-30 promotes a fragment on one recorded proof. The proof names the
release it was taken on - HERON-FRG-MTX-009 reads that, and it is read
from there rather than re-parsed here.

If that release also has recorded FAILURES, two records of the same code
on the same release disagree. The proof says it worked; the runs say it
did not. Nothing here decides which is right: a proof is evidence a
person recorded and a failure is evidence a machine recorded, and
picking a winner would be this agent overruling one of them.

2. A PROOF STANDING ON A RELEASE NOBODY HAS RUN SINCE
-------------------------------------------------------
A proof is one recorded event. A release with a proof and no runs after
it has evidence from one moment, which is what D-30 asks for and is not
the same as evidence that it still works.

3. A RUN ON A RELEASE IT DOES NOT COMPILE ON
----------------------------------------------
The matrix says which releases a fragment COMPILES on, from tests rather
than assumption. A recorded run on a release the matrix calls unknown is
one of the two records being wrong about the world, and it is reported
rather than resolved - the same reason as above.

NOTHING HERE MARKS A POOR PERFORMER
-------------------------------------
HERON-SKL-PRF-006 already refused to invent that threshold and the
refusal is inherited with the arithmetic. The one thing marked is a
universal: never succeeded on a release.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402
import heron_matrix as MTX  # noqa: E402
import heron_performance as SKL  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

VERSIONS = FRAG.REVIT_VERSIONS

# HERON-SKL-PRF-006's, imported. The subject differs; the arithmetic
# does not, and a second copy would be a second chance to start
# averaging across releases.
FAILED = SKL.FAILED
OK = SKL.OK

# The matrix states that mean the code was actually built for a release.
# HERON-FRG-MTX-009's names, imported rather than spelled.
BUILT = (MTX.COMPILES, MTX.PROVEN)


def measure(fragment, runs, matrix=None):
    """
    Everything HERON-SKL-PRF-006 reports, plus what only a fragment has.

    {versions, untested, never_succeeded, proof, contradicted, ...} - or
    a refusal, including one passed straight through from the agent that
    owns the counting.
    """
    if not fragment:
        return {"measured": False, "refused": "NOT_A_FRAGMENT",
                "why": "no fragment was handed in. Runs with nothing to "
                       "attribute them to are a list of outcomes."}

    card = getattr(fragment, "data", fragment)
    if not isinstance(card, dict):
        return {"measured": False, "refused": "NOT_A_FRAGMENT",
                "why": "%r is not a fragment. One carries an id, `revit` "
                       "and, once it has been proven, a proof." % (card,)}

    who = str(getattr(fragment, "slug", None) or card.get("id") or "").strip()
    if not who:
        return {"measured": False, "refused": "NOT_A_FRAGMENT",
                "why": "the fragment has no id, so nothing here can say "
                       "what these runs are about."}

    declared = [str(one) for one in (card.get("revit") or [])]
    if not declared:
        return {"measured": False, "refused": "NO_DECLARED_VERSIONS",
                "asked": "Which Revit releases does '%s' support?" % who,
                "why": "'%s' declares no `revit`. Per-release is the whole "
                       "row, and a fragment that has not said which "
                       "releases it is for cannot have a release reported "
                       "as UNTESTED - there is nothing it was expected to "
                       "work on." % who}

    # THE COUNTING IS HERON-SKL-PRF-006's. A refusal from it is passed
    # through in its own words rather than re-worded here.
    counted = SKL.measure(runs, declared=declared)
    if not counted.get("measured"):
        answer = dict(counted)
        answer["refused_by"] = "HERON-SKL-PRF-006"
        answer["fragment"] = who
        return answer

    by_release = dict((one["revit"], one) for one in counted["versions"])

    # 1 and 2. THE PROOF, read by the agent that owns reading it.
    standing, why_not = MTX.proof_release(fragment) \
        if hasattr(fragment, "proof") else (None, "not a fragment object")
    contradicted, unwatched = None, None
    if standing:
        here = by_release.get(standing)
        if here and here["failed"]:
            contradicted = {
                "revit": standing, "failed": here["failed"],
                "runs": here["runs"],
                "why": "the proof for '%s' was taken on %s and %d of %d "
                       "run(s) there failed. Two records of the same code "
                       "on the same release disagree - a proof is evidence "
                       "a person recorded and a failure is evidence a "
                       "machine recorded, and nothing here picks between "
                       "them." % (who, standing, here["failed"],
                                  here["runs"])}
        elif not here:
            unwatched = {
                "revit": standing,
                "why": "the proof for '%s' stands on %s and there are no "
                       "runs on %s at all. One recorded event is what D-30 "
                       "asks for; it is not the same as evidence that it "
                       "still works." % (who, standing, standing)}

    # 3. THE MATRIX, when one was handed in.
    off_matrix, ready = [], []
    if matrix:
        states = dict((str(key), str(value))
                      for key, value in dict(matrix).items())
        for release in sorted(by_release):
            if states.get(release) not in BUILT:
                off_matrix.append({
                    "revit": release, "state": states.get(release),
                    "runs": by_release[release]["runs"],
                    "why": "%d run(s) recorded on %s, which the matrix "
                           "calls %r. One of the two records is wrong "
                           "about the world."
                           % (by_release[release]["runs"], release,
                              states.get(release))})
        ready = [release for release in declared
                 if release not in by_release
                 and states.get(release) in BUILT]

    return {
        "measured": True, "fragment": who,
        "versions": counted["versions"],
        "untested": counted["untested"],
        "never_succeeded": counted["never_succeeded"],
        "declared": declared, "of": counted["of"],
        "proof_on": standing, "no_proof_because": why_not,
        "contradicted": contradicted, "unwatched_since_proof": unwatched,
        "ran_where_it_does_not_build": off_matrix,
        "ready_to_prove": ready,
        "why": "'%s': %s%s"
               % (who, counted["why"],
                  " The proof stands on %s." % standing if standing
                  else " No proof names a single release - %s." % why_not),
        "unjudged": counted["unjudged"] + [
            "%s" % (contradicted["why"] if contradicted else
                    unwatched["why"] if unwatched else
                    "the proof and the runs do not contradict each other "
                    "on any release." if standing else
                    "THERE IS NO PROOF STANDING ON ONE RELEASE (%s), so "
                    "nothing here could be checked against it." % why_not),
            "%s" % ("%d RELEASE(S) CARRY RUNS BUT DO NOT BUILD ACCORDING "
                    "TO THE MATRIX: %s. Reported, never resolved - "
                    "HERON-FRG-MTX-009 owns what compiles and this agent "
                    "owns what ran."
                    % (len(off_matrix),
                       ", ".join(one["revit"] for one in off_matrix))
                    if off_matrix else
                    "every release with runs also builds, per the matrix."
                    if matrix else
                    "NO MATRIX WAS HANDED IN, so nothing was checked "
                    "against what actually compiles. A run on a release "
                    "the code does not build for would have gone "
                    "unnoticed here."),
            "THE COUNTING IS HERON-SKL-PRF-006's, IMPORTED. The subject "
            "differs and the arithmetic does not - a second copy would be "
            "a second chance for one of them to start averaging across "
            "releases, which is the one thing both rows forbid.",
        ],
    }


def main(argv):
    print("FRAGMENT PERFORMANCE   the same arithmetic, three new findings")
    print("=" * 72)

    found, problems = FRAG.load_all()
    proven = [one for one in found.values() if one.status == "PROVEN"]
    print("\n%d fragment(s) in the library, %d at PROVEN"
          % (len(found), len(proven)))

    frag = proven[0] if proven else list(found.values())[0]
    standing, why_not = MTX.proof_release(frag)
    print("  looking at %s - proof on %s"
          % (frag.slug, standing or "(%s)" % why_not))

    runs = [{"revit": standing or "2024", "outcome": "ok", "seconds": 1.2},
            {"revit": standing or "2024", "outcome": "failed",
             "seconds": 0.4, "corrections": ["user re-ran it"]},
            {"revit": "2021", "outcome": "failed", "seconds": 0.2}]
    answer = measure(frag, runs,
                     matrix={"2024": MTX.PROVEN, "2021": MTX.UNKNOWN,
                             "2025": MTX.COMPILES})
    print("\n%s" % answer["why"])
    for one in answer["versions"]:
        print("  %-6s %d run(s), %d failed, rate %.2f"
              % (one["revit"], one["runs"], one["failed"],
                 one["failure_rate"]))
    if answer["contradicted"]:
        print("\n  CONTRADICTED  %s" % answer["contradicted"]["why"])
    for one in answer["ran_where_it_does_not_build"]:
        print("  OFF MATRIX    %s" % one["why"])
    print("  ready to prove: %s"
          % (", ".join(answer["ready_to_prove"]) or "none"))

    print("\nrefused")
    for fragment, these in ((None, runs), ("a string", runs),
                            ({"id": ""}, runs),
                            ({"id": "x", "revit": []}, runs),
                            ({"id": "x", "revit": ["2024"]}, []),
                            ({"id": "x", "revit": ["2024"]},
                             [{"revit": "1999", "outcome": "ok"}])):
        bad = measure(fragment, these)
        print("  %-22s %-18s %s"
              % (bad["refused"], bad.get("refused_by", "(its own)"),
                 bad["why"][:32]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
