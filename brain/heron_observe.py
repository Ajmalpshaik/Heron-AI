# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-OBS-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Learning observation - a success somebody had to correct is a failure
signal.

    python brain/heron_observe.py

WHAT IT IS FOR (docs/28, HERON-LRN-OBS-001)
--------------------------------------------
"Watches completed workflows, successes and failures alike. FAILURE IS
THE HIGHER-VALUE SIGNAL." T1, risk READ. It observes and concludes
nothing - HERON-LRN-ANA-002 decides whether an observation is a pattern.

FAILURE IS THE HIGHER-VALUE SIGNAL, AND THE ROW MEANS IT
----------------------------------------------------------
A run that worked tells you the thing you already believed. A run that
did not tells you where the belief was wrong, and it tells you at a
specific step. So failures are never filtered out, never summarised into
a rate, and never ranked below successes - they come back first and they
come back whole.

THE OBSERVATION PEOPLE LOSE: A SUCCESS SOMEBODY HAD TO CORRECT
---------------------------------------------------------------
A run that finished, reported success, and was then edited by hand is
recorded as a SUCCESS everywhere that counts outcomes. It is the most
useful observation in the set: it worked, and it was still wrong, and
somebody knew exactly how.

So a correction makes a run a failure signal here whatever its outcome
says, and the answer says which kind of signal each run is rather than
only whether it passed.

NO RATE, AND THAT IS D-39's RULE
----------------------------------
Nothing here returns a success percentage. D-39 keeps a disagreement log
and refuses an agreement rate for the same reason: a rate is a number
you can watch improve without anything getting better, and it hides the
one run that matters behind the ninety-nine that did not.

Counts are reported. A caller wanting a rate can divide, and will have
the numerator and denominator in front of them when they do.

IT CONCLUDES NOTHING
----------------------
No "this always fails", no "this is a pattern", no suggestion. Those
need frequency and corroboration, which is HERON-LRN-ANA-002's row, and
an observer that concludes is a judge with no appeal.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What a run must carry before anything can be observed about it.
A_RUN_CARRIES = (
    ("workflow", "which workflow it was"),
    ("when", "when it ran, so an observation can be placed in time"),
    ("outcome", "what it reported - ok or failed"),
)

FAILED = "failed"
OK = "ok"

# The three kinds of signal, most valuable first. The order is the row's:
# failure is the higher-value signal.
SIGNALS = ("failed", "corrected", "clean")


def observe(runs):
    """
    {observations, counts, why} - or a refusal.

    Nothing is concluded and nothing is stored. Failures come first.
    """
    if not runs:
        return {"observed": False, "refused": "NOTHING_TO_OBSERVE",
                "why": "no runs were handed in. An observer reporting that "
                       "nothing has gone wrong when it has seen nothing is "
                       "the most misleading answer it could give."}

    seen, first, last = [], None, None
    for run in runs:
        if not isinstance(run, dict):
            return {"observed": False, "refused": "NOT_A_RUN",
                    "why": "%r is not a run. Each carries %s."
                           % (run, ", ".join(name for name, _why
                                             in A_RUN_CARRIES))}
        missing = [name for name, _why in A_RUN_CARRIES
                   if not str(run.get(name) or "").strip()]
        if missing:
            return {"observed": False, "refused": "NOT_A_RUN",
                    "why": "a run carries no %s. %s"
                           % (", ".join(missing),
                              " ".join(why for name, why in A_RUN_CARRIES
                                       if name in missing))}

        outcome = str(run["outcome"]).strip().lower()
        if outcome not in (OK, FAILED):
            return {"observed": False, "refused": "NOT_AN_OUTCOME",
                    "why": "'%s' is neither '%s' nor '%s'. A third outcome "
                           "would be a judgement about what happened, and "
                           "this agent does not make one."
                           % (outcome, OK, FAILED)}

        corrections = [str(each) for each in (run.get("corrections") or [])
                       if str(each).strip()]
        when = str(run["when"]).strip()
        first = when if first is None or when < first else first
        last = when if last is None or when > last else last

        if outcome == FAILED:
            signal, why = "failed", (
                "it failed%s. That is the higher-value signal: a run that "
                "worked tells you what you already believed."
                % (" at '%s'" % run["at"] if run.get("at") else ""))
        elif corrections:
            signal, why = "corrected", (
                "it reported success and somebody corrected it afterwards "
                "(%d): %s. It worked, it was still wrong, and somebody knew "
                "exactly how - which is the observation every outcome count "
                "loses." % (len(corrections), "; ".join(corrections[:2])))
        else:
            signal, why = "clean", (
                "it succeeded and nobody corrected it. Recorded, and worth "
                "less than either of the others.")

        seen.append({"workflow": str(run["workflow"]).strip(),
                     "when": when, "outcome": outcome,
                     "at": run.get("at"), "signal": signal,
                     "corrections": corrections, "why": why})

    # FAILURES FIRST, then corrections, then the clean ones. The order is
    # the row's own, and a caller reading only the top of the list reads
    # the useful part.
    order = dict((name, place) for place, name in enumerate(SIGNALS))
    seen.sort(key=lambda one: (order[one["signal"]], one["when"]))

    counts = dict((name, 0) for name in SIGNALS)
    for one in seen:
        counts[one["signal"]] += 1

    return {
        "observed": True, "observations": seen, "counts": counts,
        "window": {"first": first, "last": last}, "of": len(runs),
        "why": "%d run(s): %d failed, %d succeeded and were corrected, %d "
               "clean. Failures first, and no rate."
               % (len(runs), counts["failed"], counts["corrected"],
                  counts["clean"]),
        "unjudged": [
            "NOTHING WAS CONCLUDED. No pattern, no 'this always fails', no "
            "suggestion - those need frequency and corroboration, which is "
            "HERON-LRN-ANA-002's, and an observer that concludes is a judge "
            "with no appeal.",
            "A SUCCESS SOMEBODY CORRECTED IS A FAILURE SIGNAL. %d run(s) "
            "reported success and were edited afterwards. Every outcome "
            "count in the system records those as successes, which is "
            "exactly why they are separated here."
            % counts["corrected"],
            "NO RATE WAS RETURNED, AND THAT IS D-39's RULE. It keeps a "
            "disagreement log and refuses an agreement rate for the same "
            "reason: a rate is a number you can watch improve without "
            "anything getting better, and it hides the one run that "
            "matters behind the ninety-nine that did not.",
            "THESE ARE THE RUNS THAT WERE HANDED IN, over %s. What was not "
            "recorded is not the same as what did not happen, and this "
            "agent cannot tell the difference."
            % ("%s to %s" % (first, last) if first and last
               else "an unstated window"),
        ],
    }


def main(argv):
    print("LEARNING OBSERVATION   a corrected success is a failure signal")
    print("=" * 72)

    answer = observe([
        {"workflow": "tag-and-sheet", "when": "2026-09-01", "outcome": "ok"},
        {"workflow": "tag-and-sheet", "when": "2026-09-08",
         "outcome": "failed", "at": "place"},
        {"workflow": "tag-and-sheet", "when": "2026-09-09", "outcome": "ok",
         "corrections": ["user re-tagged 6 of 47 by hand"]},
        {"workflow": "duct-sizes", "when": "2026-09-14", "outcome": "ok"},
    ])

    print("\n%s" % answer["why"])
    for one in answer["observations"]:
        print("\n  %-10s %-16s %s" % (one["signal"], one["workflow"],
                                      one["when"]))
        print("    %s" % one["why"])

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
