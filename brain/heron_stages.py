# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-WOP-016
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Workflow optimiser - three facts about recorded runs, and no fourth.

    python brain/heron_stages.py

WHAT IT IS FOR (docs/28, HERON-KRN-WOP-016)
--------------------------------------------
"Improves THE WAY WORK IS DONE, not the agents doing it. Spots that a
workflow always fails at the same stage, that two stages could merge, or
that a step never changes the outcome." T2, risk SUGGEST.

The boundary in that first sentence is real and already has an agent on
the other side of it: HERON-AHR-OPT-009 improves the AGENTS. Asked to
change one, this refuses - NOT_ABOUT_A_WORKFLOW - rather than being
helpful across a line the register drew on purpose.

THE REGISTER NAMES THREE OBSERVATIONS AND THIS MAKES EXACTLY THREE
-------------------------------------------------------------------
    ALWAYS_FAILS_AT      every recorded run of this workflow that
                         reached this stage failed there.
    ALWAYS_CONSECUTIVE   these two stages ran one after the other, with
                         nothing between them, in every run that had
                         both.
    NEVER_CHANGED        this stage ran and changed nothing, in every
                         run it appeared in.

No fourth is invented. A fourth would be this agent's opinion about
workflows, and the register asked for three.

"ALWAYS" IS NOT A THRESHOLD, WHICH IS WHY IT CAN BE USED
----------------------------------------------------------
HERON-NAM-TAX-004 refuses to warn at N% because every N is invented.
The register avoided the problem by choosing a word that needs no
number: ALWAYS is a universal, and a universal is decidable from the
records themselves. "Fails 70% of the time" would have needed a
threshold; "fails every time" does not.

So what makes a suggestion weak is not a percentage, it is the number of
runs behind it - and that number is in every suggestion. "Always, in 1
of 1 run" is a true sentence a person reads correctly without any help
from this agent.

UNOBSERVED IS NOT NEVER
-------------------------
HERON-WSP-CLN-009 learned this about unused artefacts and it is the same
here. A stage that changed nothing in the runs recorded is not a stage
that changes nothing: it may be the one that matters on the day it
matters, and the records may simply not cover that day. Every suggestion
carries the window it was made from - when the first recorded run was
and when the last was - so "never" can be read as what it is.

IT SUGGESTS, AND NOTHING IT RETURNS IS A CHANGE
-------------------------------------------------
Risk SUGGEST. Nothing here rewrites a workflow, removes a stage or
merges two. What comes back is a sentence with the counts in it, and the
change is somebody's decision - D-35 refuses rather than warns, and a
suggestion acted on automatically is not a suggestion.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The three the register names. There is deliberately no fourth.
OBSERVATIONS = ("ALWAYS_FAILS_AT", "ALWAYS_CONSECUTIVE", "NEVER_CHANGED")

FAILED = "failed"


def _stages(run):
    """The stages of one run, as (name, outcome, changed-anything)."""
    out = []
    for stage in (run.get("stages") or []):
        if not isinstance(stage, dict):
            return None
        name = str(stage.get("stage") or "").strip()
        if not name:
            return None
        outcome = str(stage.get("outcome") or "").strip().lower()
        changed = stage.get("changed")
        out.append((name, outcome,
                    bool(changed) if changed is not None else None))
    return out


def suggest(runs, about=None):
    """
    {suggestions, runs, window, why, unjudged} - or a refusal.

    Nothing is changed. `about` is refused when it names an agent: that
    is HERON-AHR-OPT-009's, and the register drew the line on purpose.
    """
    if about is not None:
        named = str(about).strip()
        if named.upper().startswith("HERON-"):
            return {"changed": False, "refused": "NOT_ABOUT_A_WORKFLOW",
                    "why": "'%s' is an agent. docs/28 gives this one 'the "
                           "way work is done, NOT the agents doing it', and "
                           "HERON-AHR-OPT-009 is already on the other side "
                           "of that line. Being helpful across it would "
                           "make two agents responsible for one thing."
                           % named}

    if not runs:
        return {"changed": False, "refused": "NOTHING_TO_LEARN_FROM",
                "why": "no runs were handed in. A workflow with no recorded "
                       "runs has nothing true said about it, and 'no "
                       "problems found' would be a statement about the "
                       "records rather than about the work."}

    seen = {}
    at, first, last = {}, None, None
    for run in runs:
        if not isinstance(run, dict):
            return {"changed": False, "refused": "NOT_A_RUN",
                    "why": "%r is not a run. Each is {workflow, when, "
                           "stages}." % (run,)}
        flow = str(run.get("workflow") or "").strip()
        stages = _stages(run)
        if not flow or stages is None or not stages:
            return {"changed": False, "refused": "NOT_A_RUN",
                    "why": "a run names %s. Both are needed: which workflow "
                           "it was, and what its stages did."
                           % ("no workflow" if not flow
                              else "no usable stages")}
        when = str(run.get("when") or "").strip()
        if when:
            first = when if first is None or when < first else first
            last = when if last is None or when > last else last

        book = seen.setdefault(flow, {"runs": 0, "reached": {},
                                      "failed": {}, "changed": {},
                                      "ran": {}, "after": {},
                                      "before": {}, "orders": set()})
        book["runs"] += 1
        names = [name for name, _outcome, _changed in stages]
        for index, (name, outcome, changed) in enumerate(stages):
            book["reached"][name] = book["reached"].get(name, 0) + 1
            if outcome == FAILED:
                book["failed"][name] = book["failed"].get(name, 0) + 1
            if changed is not None:
                book["ran"][name] = book["ran"].get(name, 0) + 1
                if changed:
                    book["changed"][name] = book["changed"].get(name, 0) + 1
            if index + 1 < len(names):
                pair = (name, names[index + 1])
                book["after"][pair] = book["after"].get(pair, 0) + 1
            if index:
                book["before"][name] = book["before"].get(name, 0) + 1
        book["orders"].add(tuple(names))
        at[flow] = names

    suggestions = []
    for flow in sorted(seen):
        book = seen[flow]
        total = book["runs"]

        for name in sorted(book["reached"]):
            reached = book["reached"][name]
            if book["failed"].get(name, 0) == reached:
                suggestions.append({
                    "workflow": flow, "observation": "ALWAYS_FAILS_AT",
                    "stage": name, "runs": total, "of": reached,
                    "why": "'%s' failed in every one of the %d run(s) that "
                           "reached it, out of %d recorded. Fixing that "
                           "stage is the whole of this workflow's problem "
                           "until it stops being true."
                           % (name, reached, total)})

            ran = book["ran"].get(name, 0)
            if ran and not book["changed"].get(name, 0):
                suggestions.append({
                    "workflow": flow, "observation": "NEVER_CHANGED",
                    "stage": name, "runs": total, "of": ran,
                    "why": "'%s' ran %d time(s) and changed nothing each "
                           "time. That is what the records show, not what "
                           "the stage does - it may be the one that matters "
                           "on the day it matters, and these records may "
                           "not cover that day." % (name, ran)})

        # ONE ORDER ONLY is the real explanation when every pair qualifies,
        # and saying it once beats saying the same thing n-1 times without
        # the reason.
        fixed = len(book["orders"]) == 1 and total > 1
        for pair in sorted(book["after"]):
            first_seen = book["reached"].get(pair[0], 0)
            second_seen = book["reached"].get(pair[1], 0)
            # BOTH DIRECTIONS. "A is always followed by B" alone still
            # allows B to turn up without A, and two stages that come apart
            # in either direction are not inseparable.
            together = book["after"][pair]
            if (together and together == first_seen
                    and together == book["before"].get(pair[1], 0)
                    and together == second_seen):
                suggestions.append({
                    "workflow": flow, "observation": "ALWAYS_CONSECUTIVE",
                    "stages": list(pair), "runs": total, "of": together,
                    "one_order": fixed,
                    "why": "'%s' was followed immediately by '%s' in every "
                           "one of the %d run(s), and '%s' never appeared "
                           "without '%s' in front of it. Whether merging "
                           "them is an improvement is a judgement; that "
                           "they never came apart is a fact.%s"
                           % (pair[0], pair[1], together, pair[1], pair[0],
                              "" if not fixed else
                              " This workflow ran the same order every "
                              "time, so every pair in it qualifies - the "
                              "fact is about the workflow as much as about "
                              "these two.")})

    return {
        "changed": False, "suggestions": suggestions,
        "workflows": sorted(seen), "runs": len(runs),
        "window": {"first": first, "last": last},
        "why": "%d run(s) of %d workflow(s): %d observation(s). Nothing was "
               "changed - every one of these is a sentence, not an edit."
               % (len(runs), len(seen), len(suggestions)),
        "unjudged": [
            "NOTHING WAS CHANGED, MERGED OR REMOVED. Risk SUGGEST means a "
            "sentence with the counts in it, and a suggestion acted on "
            "automatically is not a suggestion.",
            "ALWAYS IS A UNIVERSAL, NOT A THRESHOLD, which is the only "
            "reason it can be used here - 'fails 70 per cent of the time' "
            "would have needed a number and every number is invented. What "
            "makes "
            "a suggestion weak is the run count behind it, and that count "
            "is in every one of them.",
            "UNOBSERVED IS NOT NEVER. A stage that changed nothing in "
            "these records is not a stage that changes nothing, and the "
            "window these came from is %s. HERON-WSP-CLN-009 learned the "
            "same thing about unused artefacts."
            % ("%s to %s" % (first, last) if first and last
               else "not stated - no run carried a date, so 'never' here "
                    "covers an unknown span"),
            "THE AGENTS ARE NOT THIS AGENT'S. docs/28 gives it the way "
            "work is done and gives HERON-AHR-OPT-009 the agents doing it, "
            "and only three observations are made because the register "
            "named three.",
        ],
    }


def main(argv):
    print("WORKFLOW OPTIMISER   three facts about recorded runs")
    print("=" * 72)

    def run(when, *stages):
        return {"workflow": "tag-and-sheet", "when": when,
                "stages": [{"stage": name, "outcome": outcome,
                            "changed": changed}
                           for name, outcome, changed in stages]}

    answer = suggest([
        run("2026-09-01",
            ("select", "ok", ["a", "b"]), ("check", "ok", []),
            ("tag", "ok", ["a"]), ("place", "failed", [])),
        run("2026-09-08",
            ("select", "ok", ["c"]), ("check", "ok", []),
            ("tag", "ok", ["c"]), ("place", "failed", [])),
        run("2026-09-14",
            ("select", "ok", ["d"]), ("check", "ok", []),
            ("tag", "ok", ["d"]), ("place", "failed", [])),
    ])

    print("\n%s" % answer["why"])
    for row in answer["suggestions"]:
        print("\n  %-20s %s"
              % (row["observation"],
                 row.get("stage") or " -> ".join(row["stages"])))
        print("    %s" % row["why"])

    refused = suggest([{"workflow": "x", "stages": [{"stage": "s"}]}],
                      about="HERON-AHR-OPT-009")
    print("\n  %s\n    %s" % (refused["refused"], refused["why"][:80]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
