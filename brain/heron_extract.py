# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-EXT-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Knowledge extraction - you cannot tell a constant from a variable that
did not vary.

    python brain/heron_extract.py

WHAT IT IS FOR (docs/28, HERON-LRN-EXT-003)
--------------------------------------------
"Turns an observed pattern into a candidate fragment, skill or rule -
PARAMETERISED, NOT HARD-CODED TO THE NUMBERS IT HAPPENED TO SEE." T3,
risk SUGGEST. It proposes a candidate; nothing is written and nothing is
promoted - HERON-LRN-PRO-004 owns the gates.

THE WHOLE DIFFICULTY IS IN ONE SENTENCE OF THE ROW
----------------------------------------------------
A pattern seen three times at 300x300 could be about 300x300, or it
could be about duct size and 300x300 is what the model happened to
contain. From the observations alone THOSE TWO ARE THE SAME DATA.

So the values are sorted into three lists rather than two, and the third
is the honest one:

    VARIED           it took different values across the observations,
                     so it is a parameter. Certain.
    NEVER VARIED     it was the same every time. This is EITHER a
                     constant OR a parameter nobody has varied yet, and
                     nothing in the observations can tell which. Offered
                     as a question, never baked in.
    ASKED FOR        a value the caller wants fixed. Allowed for one
                     that never varied, and REFUSED for one that did -
                     hard-coding a value already known to change is the
                     failure the row names.

A CANDIDATE THAT HARD-CODES SILENTLY IS WORSE THAN NO CANDIDATE
-----------------------------------------------------------------
It will work on the model it was extracted from and be wrong everywhere
else, and it will be wrong in the way that looks like success -
returning something, plausibly, for the wrong reason. That is the same
failure D-30 built the negative case to catch, one stage earlier.

A ONE-OFF IS NOT A PATTERN
----------------------------
HERON-LRN-ANA-002 answers that, and this agent refuses to extract from
something it called `a_one_off`. Building a fragment out of one sighting
is how a coincidence becomes a rule, and the verdict already exists -
re-deciding it here would put the judgement in two places.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What a candidate may be. docs/28's own three.
KINDS = ("fragment", "skill", "rule")

# HERON-LRN-ANA-002's verdict for something seen once.
A_ONE_OFF = "a_one_off"


def extract(pattern, observations, as_kind="fragment", fix=None):
    """
    {candidate, parameters, questions, why} - or a refusal.

    Nothing is written and nothing is promoted.
    """
    kind = str(as_kind or "").strip().lower()
    if kind not in KINDS:
        return {"extracted": False, "refused": "NOT_A_KIND",
                "why": "'%s' is not one of %s. docs/28 names three things a "
                       "pattern can become and a fourth is not invented "
                       "here." % (kind, ", ".join(KINDS))}

    name = str((pattern or {}).get("pattern") or "").strip() \
        if isinstance(pattern, dict) else str(pattern or "").strip()
    if not name:
        return {"extracted": False, "refused": "NOT_A_PATTERN",
                "why": "nothing was named. A candidate extracted from a "
                       "pattern nobody can name cannot be found again, "
                       "compared with anything, or argued with."}

    verdict = str((pattern or {}).get("verdict") or "").strip().lower() \
        if isinstance(pattern, dict) else ""
    if verdict == A_ONE_OFF:
        return {"extracted": False, "refused": "A_ONE_OFF_IS_NOT_A_PATTERN",
                "why": "HERON-LRN-ANA-002 called '%s' a one-off. Building a "
                       "fragment out of one sighting is how a coincidence "
                       "becomes a rule, and the verdict already exists - "
                       "re-deciding it here would put the judgement in two "
                       "places." % name}

    if not observations or len(observations) < 2:
        return {"extracted": False, "refused": "NOTHING_TO_EXTRACT",
                "why": "%d observation(s). Two is the least that can show a "
                       "value varying, and without that this agent cannot "
                       "tell a parameter from a number it happened to see - "
                       "which is the one thing the row asks it to do."
                       % len(observations or [])}

    values = {}
    for one in observations:
        if not isinstance(one, dict):
            return {"extracted": False, "refused": "NOT_AN_OBSERVATION",
                    "why": "%r is not an observation. Each is a map of what "
                           "was seen." % (one,)}
        for field, value in one.items():
            if field in ("pattern", "verdict"):
                continue
            values.setdefault(str(field), []).append(value)

    if not values:
        return {"extracted": False, "refused": "NOTHING_TO_EXTRACT",
                "why": "the observations carry no values at all, so there "
                       "is nothing to parameterise and nothing to fix."}

    varied, steady = [], []
    for field in sorted(values):
        distinct = sorted(set(str(each) for each in values[field]))
        entry = {"name": field, "seen": distinct, "of": len(values[field])}
        (varied if len(distinct) > 1 else steady).append(entry)

    wanted = sorted(set(str(each).strip() for each in (fix or [])
                        if str(each).strip()))
    changing = set(one["name"] for one in varied)
    wrong = sorted(name for name in wanted if name in changing)
    if wrong:
        return {"extracted": False, "refused": "WOULD_HARD_CODE",
                "fields": wrong,
                "why": "%s already took more than one value across the "
                       "observations, and fixing one is hard-coding a value "
                       "ALREADY KNOWN to change. The candidate would work "
                       "on what it was extracted from and be wrong "
                       "everywhere else - wrong in the way that looks like "
                       "success."
                       % ", ".join("'%s' (%s)" % (
                           field, ", ".join(one["seen"][:3]))
                           for field in wrong
                           for one in varied if one["name"] == field)}

    unknown = sorted(name for name in wanted
                     if name not in set(one["name"] for one in steady))
    if unknown:
        return {"extracted": False, "refused": "NOTHING_TO_EXTRACT",
                "why": "%s was asked to be fixed and appears in no "
                       "observation." % ", ".join("'%s'" % each
                                                  for each in unknown)}

    parameters = [one["name"] for one in varied]
    fixed = [one for one in steady if one["name"] in wanted]
    questions = [one for one in steady if one["name"] not in wanted]

    return {
        "extracted": False, "candidate": {
            "about": name, "kind": kind,
            "parameters": parameters,
            "fixed": dict((one["name"], one["seen"][0]) for one in fixed)},
        "parameters": varied, "fixed": fixed, "questions": questions,
        "of": len(observations),
        "why": "'%s' as a candidate %s: %d parameter(s), %d value(s) fixed "
               "on request, %d left as questions. Nothing was written."
               % (name, kind, len(varied), len(fixed), len(questions)),
        "unjudged": [
            "NOTHING WAS WRITTEN AND NOTHING WAS PROMOTED. This is a "
            "candidate; HERON-LRN-PRO-004 owns the gates, and D-30's proof "
            "is what a candidate has to earn.",
            "%d VALUE(S) VARIED AND ARE PARAMETERS. That part is certain: "
            "something seen taking two values is not a constant."
            % len(varied),
            "%s" % ("%d VALUE(S) NEVER VARIED AND ARE LEFT AS QUESTIONS: "
                    "%s. Each is EITHER a constant OR a parameter nobody "
                    "has varied yet, and nothing in the observations can "
                    "tell which - so none is baked in without somebody "
                    "saying so."
                    % (len(questions),
                       ", ".join(one["name"] for one in questions))
                    if questions else
                    "every value either varied or was fixed on request, so "
                    "nothing is left open."),
            "A CANDIDATE THAT HARD-CODES SILENTLY IS WORSE THAN NO "
            "CANDIDATE. It works on what it was extracted from and is "
            "wrong everywhere else, returning something plausible for the "
            "wrong reason - the same failure D-30's negative case exists "
            "to catch, one stage earlier.",
        ],
    }


def main(argv):
    print("KNOWLEDGE EXTRACTION   a constant and an unvaried variable look")
    print("                       exactly the same")
    print("=" * 72)

    observations = [
        {"size": "300x300", "system": "Supply", "level": "L2",
         "missed": "flex duct"},
        {"size": "250x250", "system": "Supply", "level": "L3",
         "missed": "flex duct"},
        {"size": "400x200", "system": "Supply", "level": "L2",
         "missed": "flex duct"},
    ]
    pattern = {"pattern": "tagging misses flex duct",
               "verdict": "corroborated"}

    answer = extract(pattern, observations)
    print("\n%s" % answer["why"])
    for one in answer["parameters"]:
        print("  parameter  %-10s varied: %s" % (one["name"],
                                                 ", ".join(one["seen"])))
    for one in answer["questions"]:
        print("  question   %-10s always %s - constant, or nobody varied it?"
              % (one["name"], one["seen"][0]))

    asked = extract(pattern, observations, fix=["system", "missed"])
    print("\nfixing the two that never varied:")
    print("  %s" % asked["why"])
    print("  candidate fixes %s" % asked["candidate"]["fixed"])

    print("\nrefused")
    for args in (({"pattern": "x", "verdict": "a_one_off"}, observations, {}),
                 (pattern, observations, {"fix": ["size"]}),
                 (pattern, observations[:1], {}),
                 ("", observations, {}),
                 (pattern, observations, {"as_kind": "policy"})):
        p, o, kw = args
        bad = extract(p, o, **kw)
        print("  %-28s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
