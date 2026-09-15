# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-ORC-FIX-005
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Repair - only what failure analysis pointed at, and never a way round a
STOP.

    python brain/heron_fix.py

WHAT IT IS FOR (docs/28, HERON-ORC-FIX-005)
--------------------------------------------
"Applies a targeted repair chosen by failure analysis." T3, risk MODIFY.

HERON-ORC-FAIL-004 CHOOSES; THIS ONE DOES THE ONE THING IT CHOSE
-----------------------------------------------------------------
That agent classifies a failure into one of six next steps, and only ONE
of them is a repair:

    retry       send it again, unchanged - nothing to repair
    wait        it is still working - asking again is wrong
    FIX_FIRST   something must change before it can work
    look        a person must check the model before anything else
    start_over  the request is stale; build it again
    stop        do not retry, AND DO NOT WORK AROUND IT

So five of the six are refused here, by name, with what that step
actually means. A fix agent that treats `wait` as "try a repair" is how
a busy Revit gets three more requests.

STOP GETS ITS OWN REFUSAL, AND IT IS THE POINT OF THIS FILE
-------------------------------------------------------------
HERON-ORC-FAIL-004's own comment on `stop` is "do not retry, and do not
work around it". Working around it is exactly what an agent called the
Fix Agent is for, which is why it is the one refusal that names itself:
`read_only` means the model is open read-only, and a repair that opened
it writable would be Heron deciding that somebody else's lock was a
mistake.

THE REPAIR IS SUPPLIED, NOT INVENTED
--------------------------------------
Most FIX_FIRST codes are not a machine's to fix. "No model is open", "a
dialog is open or a command is running", "the Emergency Stop is on" -
Heron cannot open a model, close somebody's dialog or overrule a stop.
What it CAN repair is an input the request was missing or could not
read, and even then the value comes from the person: D-33, asked once
rather than assumed.

With nothing supplied, the answer IS the question - the analysis already
wrote it, and its wording is used rather than a new one.

THE ROUTE NAMES ARE RESTATED HERE, AND D-48 IS WHY
----------------------------------------------------
HERON-ORC-FAIL-004 lives in `mcp/server/`, and D-48 forbids `brain`
importing from `mcp`. So the six next-step names are written again in
this file - the same legitimate second copy PROPOSALS F22 grants
`mcp/server/heron_tools.py` for the risk ladder, and for the identical
reason.

A second copy is only safe if something compares them, and tests may
import from every layer: tests/test_fix.py asserts these six ARE that
module's six, and fails the day either side gains a seventh.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-ORC-FAIL-004's six, restated because D-48 forbids the import.
# tests/test_fix.py compares them against that module and fails if they
# drift - see the docstring.
RETRY = "retry"
WAIT = "wait"
FIX_FIRST = "fix_first"
LOOK_AT_THE_MODEL = "look"
START_OVER = "start_over"
STOP = "stop"

ROUTES = (RETRY, WAIT, FIX_FIRST, LOOK_AT_THE_MODEL, START_OVER, STOP)

# What each of the five non-repair routes means, so a refusal says why
# rather than only that.
NOT_A_REPAIR = {
    RETRY: "send it again unchanged - there is nothing to repair, and "
           "changing something first would make the retry a different "
           "request",
    WAIT: "it is still working. Asking again is wrong, and a repair is "
          "asking again with extra steps",
    LOOK_AT_THE_MODEL: "a person must check the model before anything "
                       "else. That is what an answer nobody can classify "
                       "means, and repairing past it is guessing at what "
                       "happened",
    START_OVER: "the request is stale - build it again. A repair would "
                "patch a request that no longer describes the model",
}

AN_ANALYSIS_CARRIES = (
    ("code", "which failure, from HERON-ORC-FAIL-004's table"),
    ("next_step", "which of the six routes it chose"),
    ("reason", "what it means, in the words that agent already wrote"),
)


def _of(thing, field):
    return getattr(thing, field, None) if not isinstance(thing, dict) \
        else thing.get(field)


def repair(failure, supply=None, request=None):
    """
    {repaired, request, why} - or a refusal. Nothing is sent.
    """
    if not failure:
        return {"repaired": False, "refused": "NOTHING_TO_REPAIR",
                "why": "no failure was handed in. A repair with nothing to "
                       "repair is a change nobody asked for."}

    missing = [field for field, _ in AN_ANALYSIS_CARRIES
               if not str(_of(failure, field) or "").strip()]
    if missing:
        return {"repaired": False, "refused": "NOT_AN_ANALYSIS",
                "missing": missing,
                "why": "the failure is missing %s. %s This agent applies "
                       "what HERON-ORC-FAIL-004 chose, so it needs that "
                       "agent's answer rather than a description of what "
                       "went wrong."
                       % (", ".join(missing),
                          " ".join(why for field, why
                                   in AN_ANALYSIS_CARRIES
                                   if field in missing))}

    code = str(_of(failure, "code")).strip()
    route = str(_of(failure, "next_step")).strip()
    reason = str(_of(failure, "reason")).strip()

    if route not in ROUTES:
        return {"repaired": False, "refused": "NOT_AN_ANALYSIS",
                "why": "'%s' is not one of HERON-ORC-FAIL-004's six next "
                       "steps: %s." % (route, ", ".join(ROUTES))}

    # THE ONE THAT NAMES ITSELF.
    if route == STOP:
        return {"repaired": False, "refused": "MUST_NOT_WORK_AROUND",
                "code": code, "route": route,
                "why": "'%s' - %s - and HERON-ORC-FAIL-004 routes it to "
                       "%s, whose own words are 'do not retry, and do not "
                       "work around it'. Working around it is exactly what "
                       "an agent called the Fix Agent is for, which is why "
                       "this refusal names itself." % (code, reason, STOP)}

    if route != FIX_FIRST:
        return {"repaired": False, "refused": "NOT_A_REPAIR",
                "code": code, "route": route,
                "why": "'%s' - %s - routes to %s: %s."
                       % (code, reason, route, NOT_A_REPAIR[route])}

    if not supply:
        # THE ANALYSIS ALREADY WROTE THE QUESTION.
        return {"repaired": False, "refused": "NEEDS_AN_ANSWER",
                "code": code, "asked": reason,
                "why": "'%s' needs something changed before it can work, "
                       "and nothing was supplied. Most of what %s covers "
                       "is not a machine's to fix - Heron cannot open a "
                       "model, close somebody's dialog or overrule an "
                       "Emergency Stop - and what it CAN repair is an "
                       "input, whose value comes from the person. D-33: "
                       "asked once rather than assumed."
                       % (code, FIX_FIRST)}

    supply = getattr(supply, "data", supply)
    if not isinstance(supply, dict) or not supply:
        return {"repaired": False, "refused": "NEEDS_AN_ANSWER",
                "code": code, "asked": reason,
                "why": "%r is not an answer. One is {field: value} naming "
                       "what the request was missing." % (supply,)}

    original = getattr(request, "data", request)
    if not isinstance(original, dict):
        return {"repaired": False, "refused": "NOTHING_TO_REPAIR",
                "why": "no original request came with the failure, so "
                       "there is nothing to repair INTO. A repaired "
                       "request built from scratch here would be this "
                       "agent writing the request rather than fixing it."}

    # TARGETED MEANS TARGETED. A field the request has no room for is an
    # improvisation, whoever supplied it.
    stranger = sorted(one for one in supply if one not in original)
    if stranger:
        return {"repaired": False, "refused": "NOT_IN_THE_REQUEST",
                "fields": stranger, "takes": sorted(original),
                "why": "the answer supplies %s, which the request does not "
                       "take - it takes %s. docs/28 says a TARGETED "
                       "repair, and a field the request has no room for is "
                       "an improvisation whoever supplied it."
                       % (", ".join("'%s'" % one for one in stranger),
                          ", ".join(sorted(original)))}

    fixed = dict(original)
    changed = []
    for field in sorted(supply):
        if fixed.get(field) != supply[field]:
            changed.append(field)
        fixed[field] = supply[field]

    if not changed:
        return {"repaired": False, "refused": "NOTHING_TO_REPAIR",
                "code": code,
                "why": "the answer supplies %s, and the request already "
                       "says the same. Sending it again unchanged is a "
                       "retry, which HERON-ORC-FAIL-004 did not choose."
                       % ", ".join("'%s'" % one for one in sorted(supply))}

    return {
        "repaired": True, "code": code, "route": route,
        "request": fixed, "changed": changed, "was": dict(original),
        "why": "'%s' - %s - repaired by supplying %s. Nothing was sent."
               % (code, reason, ", ".join(changed)),
        "unjudged": [
            "NOTHING WAS SENT. A repaired request comes back and the "
            "bridge sends it - this agent's risk is MODIFY for the day it "
            "does, and today it hands back a value.",
            "WHETHER THE REPAIR WORKS. It supplies what the failure said "
            "was missing; whether Revit then accepts it is the next "
            "answer, not this one, and that answer goes to "
            "HERON-ORC-FAIL-004 exactly as the first did.",
            "WHERE THE VALUE CAME FROM. %s was supplied, not derived - "
            "Heron does not guess a category or a distance, and D-33 says "
            "ask once rather than assume." % ", ".join(changed),
            "THE OTHER FIVE ROUTES. Only %s is a repair; retry, wait, "
            "look, start_over and stop are refused here by name, and stop "
            "loudest - 'do not retry, and do not work around it'."
            % FIX_FIRST,
        ],
    }


def main(argv):
    print("REPAIR   only what failure analysis pointed at")
    print("=" * 72)
    print("\nHERON-ORC-FAIL-004's six, and which is a repair")
    for route in ROUTES:
        print("  %-12s %s" % (route,
                              "THE ONE" if route == FIX_FIRST
                              else "do not work around it" if route == STOP
                              else NOT_A_REPAIR[route][:46]))

    asked = {"code": "no_category", "next_step": FIX_FIRST,
             "reason": "no category was given"}
    request = {"operation": "select_by_category", "category": ""}

    answer = repair(asked, {"category": "ducts"}, request)
    print("\n%s" % answer["why"])
    print("  was %r -> now %r"
          % (answer["was"]["category"], answer["request"]["category"]))

    print("\nrefused")
    for failure, supply, req in (
            (None, {"category": "ducts"}, request),
            ({"code": "x"}, {"category": "ducts"}, request),
            ({"code": "x", "next_step": "sideways", "reason": "y"},
             {"category": "ducts"}, request),
            ({"code": "read_only", "next_step": STOP,
              "reason": "the model is open read-only"},
             {"category": "ducts"}, request),
            ({"code": "still_running", "next_step": WAIT,
              "reason": "it is still working"}, {"category": "x"}, request),
            ({"code": "not_ready", "next_step": RETRY,
              "reason": "nothing was sent"}, {"category": "x"}, request),
            (asked, None, request),
            (asked, {"category": "ducts"}, None),
            (asked, {"colour": "blue"}, request),
            (asked, {"category": ""}, request)):
        bad = repair(failure, supply, req)
        print("  %-24s %s" % (bad["refused"], bad["why"][:40]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
