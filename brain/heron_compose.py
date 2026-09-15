# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-CMP-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Report composition - the shape of the answer follows the shape of the
request.

    python brain/heron_compose.py

WHAT IT IS FOR (docs/28, HERON-RPT-CMP-001)
--------------------------------------------
"Decides WHAT GOES IN and at what depth FOR THIS READER - a modeller
wants the 47 failures, a BIM manager wants the trend. Judgement, so a
model call." T2, risk READ.

Both halves of that row are written against a decision that superseded
them, and this file is built on the decision instead. PROPOSALS F19.

D-27 DECIDES IT, AND IT IS A TABLE RATHER THAN A JUDGEMENT
------------------------------------------------------------
    "Heron has one voice: plain, non-developer language, always. Persona
    is not inferred, not displayed and not pinned - it does not exist as
    a setting. WHAT VARIES IS THE SHAPE OF THE ANSWER, CHOSEN FROM THE
    SHAPE OF THE REQUEST."

    The request                         The answer
    a count                             the number, one line, nothing else
    a breakdown                         a schedule-style table, sorted the
                                        way a schedule sorts, NOT BY
                                        QUANTITY
    a narrowed set                      the items, WITH THEIR IDS
    a finished piece of work            a short close: what was done, what
                                        was VERIFIED, what still needs
                                        deciding
    two or more numbers worth           a picture, WITHOUT BEING ASKED FOR
    comparing                           ONE

So "for this reader" is not how the shape is chosen, and "judgement, so
a model call" describes a decision that has already been made. No call
is made here, and the reason is D-27 rather than thrift: a table is
deterministic, and D-27's consequence says so - "nothing has to detect
who is talking. A whole class of 'why did it answer differently today'
stops being possible rather than being made visible."

THE REQUEST'S SHAPE IS DECLARED, NOT READ
-------------------------------------------
D-01 gives the host the job of turning a sentence into an intent, and
docs/28 gives HERON-ORC-INT-002 "the host classifies what is being
asked". So the shape arrives already classified. Guessing it here would
be language understanding, which D-34 refuses, and it would put the
classification in two places.

A DEFAULT MAY BE OVERRIDDEN, AND THE OVERRIDE MUST COST ONE LINE
------------------------------------------------------------------
D-27 again, and the wording is the mechanism:

    "These are defaults, not law... They live in a small file the user
    is expected to edit, WITH A DATED LINE RECORDING EACH CHANGE - a
    format correction has to cost ONE LINE to record, or it will not get
    recorded, and reply format is the thing users correct most."

So an override is honoured and one without a date is refused. The record
is not paperwork around the change; it is the thing that makes the
change reviewable, and a rule that costs nothing to record is one nobody
records.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-27's table, in its own order and its own words.
SHAPES = {
    "count": ("the number", "one line, nothing else"),
    "breakdown": ("a schedule-style table",
                  "sorted the way a schedule sorts, not by quantity"),
    "narrowed": ("the items themselves, with their ids",
                 "a narrowed request is nearly always the setup for the "
                 "next step, and ids are what make that step possible "
                 "without re-filtering"),
    "finished": ("a short close",
                 "what was done, what was actually verified, what still "
                 "needs deciding"),
    "comparison": ("a picture", "without being asked for one"),
}

# What a finished piece of work must account for. Named because "what was
# actually verified" is the one people leave out.
A_CLOSE_CARRIES = ("done", "verified", "undecided")

# What an override must carry. D-27: a dated line, or it does not get
# recorded.
AN_OVERRIDE_CARRIES = (
    ("shape", "which default it replaces"),
    ("instead", "what to do instead"),
    ("at", "the date - D-27 asks for a DATED line, and a correction that "
           "costs nothing to record is one nobody records"),
)


def compose(shape, data, overrides=None, asked_for_a_picture=None):
    """
    {answer, includes, why, unjudged} - or a refusal.

    Nothing is rendered: HERON-RPT-RND-002 turns this into a document.
    """
    shape = str(shape or "").strip().lower()
    if shape not in SHAPES:
        return {"composed": False, "refused": "NOT_A_SHAPE",
                "why": "'%s' is not one of D-27's five: %s. The shape is "
                       "DECLARED - D-01 gives the host the job of turning a "
                       "sentence into an intent, and docs/28 gives "
                       "HERON-ORC-INT-002 'the host classifies what is "
                       "being asked'. Guessing it here would be language "
                       "understanding, which D-34 refuses, and it would put "
                       "the classification in two places."
                       % (shape, ", ".join(sorted(SHAPES)))}

    if data is None:
        return {"composed": False, "refused": "NOTHING_TO_COMPOSE",
                "why": "no data was handed in. An answer composed from "
                       "nothing is a statement about the call rather than "
                       "about the work."}

    applied = None
    for override in (overrides or []):
        if not isinstance(override, dict):
            return {"composed": False, "refused": "NOT_AN_OVERRIDE",
                    "why": "%r is not an override. Each is {shape, instead, "
                           "at}." % (override,)}
        missing = [field for field, _why in AN_OVERRIDE_CARRIES
                   if not str(override.get(field) or "").strip()]
        if missing:
            return {"composed": False, "refused": "NOT_AN_OVERRIDE",
                    "missing": missing,
                    "why": "an override carries no %s. %s"
                           % (", ".join("`%s`" % each for each in missing),
                              " ".join(why for field, why
                                       in AN_OVERRIDE_CARRIES
                                       if field in missing))}
        if str(override["shape"]).strip().lower() == shape:
            applied = override

    wanted, how = SHAPES[shape]
    includes, notes = [], []

    if shape == "count":
        if not isinstance(data, (int, float)) and not (
                isinstance(data, (list, tuple)) or isinstance(data, dict)):
            return {"composed": False, "refused": "NOTHING_TO_COMPOSE",
                    "why": "a count is a number, or something countable. "
                           "%r is neither." % (data,)}
        number = (data if isinstance(data, (int, float))
                  else len(data))
        includes.append({"part": "the number", "value": number})
        notes.append("one line, nothing else - nothing is added to a count "
                     "because the question was how many.")
    elif shape == "breakdown":
        groups = data if isinstance(data, dict) else {}
        if not groups:
            return {"composed": False, "refused": "NOTHING_TO_COMPOSE",
                    "why": "a breakdown is a map of group to value, and "
                           "this is %r." % (data,)}
        # SORTED THE WAY A SCHEDULE SORTS, NOT BY QUANTITY - D-27's words,
        # and the one rule here somebody would otherwise get backwards.
        rows = [{"group": str(name), "value": groups[name]}
                for name in sorted(groups, key=lambda each: str(each))]
        includes.append({"part": "a schedule-style table", "rows": rows})
        notes.append("sorted by group, not by quantity. A schedule sorts "
                     "the way the drawing reads, and sorting by count "
                     "turns it into a ranking nobody asked for.")
    elif shape == "narrowed":
        items = list(data) if isinstance(data, (list, tuple)) else []
        if not items:
            return {"composed": False, "refused": "NOTHING_TO_COMPOSE",
                    "why": "a narrowed set is a list of items, and this is "
                           "%r." % (data,)}
        without = [one for one in items
                   if not isinstance(one, dict) or not str(
                       one.get("id") or "").strip()]
        if without:
            return {"composed": False, "refused": "NO_IDS",
                    "how_many": len(without),
                    "why": "%d of %d item(s) carry no id. D-27: a narrowed "
                           "request is nearly always the setup for the next "
                           "step, and IDS ARE WHAT MAKE THAT STEP POSSIBLE "
                           "without re-filtering. A list without them "
                           "answers the question and blocks the next one."
                           % (len(without), len(items))}
        includes.append({"part": "the items, with their ids",
                         "items": items})
        notes.append("with ids, because the next step needs them.")
    elif shape == "finished":
        said = data if isinstance(data, dict) else {}
        missing = [part for part in A_CLOSE_CARRIES
                   if not str(said.get(part) or "").strip()]
        if missing:
            return {"composed": False, "refused": "AN_INCOMPLETE_CLOSE",
                    "missing": missing,
                    "why": "a close says what was done, what was ACTUALLY "
                           "VERIFIED and what still needs deciding, and "
                           "this has no %s. The middle one is what people "
                           "leave out, and leaving it out is how a report "
                           "of work reads as a report of finished work."
                           % ", ".join(missing)}
        for part in A_CLOSE_CARRIES:
            includes.append({"part": part, "value": said[part]})
        notes.append("short, and all three parts - done, verified, still "
                     "to decide.")
    else:  # comparison
        numbers = [one for one in (data if isinstance(data, (list, tuple))
                                   else [])
                   if isinstance(one, (int, float))]
        if len(numbers) < 2:
            return {"composed": False, "refused": "NOTHING_TO_COMPARE",
                    "why": "a comparison needs two or more numbers and %d "
                           "arrived. D-27's rule is about numbers WORTH "
                           "COMPARING; one number is a count, and this "
                           "agent does not turn it into one on the caller's "
                           "behalf." % len(numbers)}
        includes.append({"part": "a picture", "numbers": numbers})
        notes.append("a picture, without being asked for one - D-27 puts "
                     "that in the table, so it is a default rather than a "
                     "flourish.")

    return {
        "composed": True, "shape": shape, "answer": wanted, "how": how,
        "includes": includes, "override": applied,
        "why": "%s: %s. %s%s"
               % (shape, wanted, " ".join(notes),
                  " Overridden by the user's own line of %s: %s."
                  % (applied["at"], applied["instead"]) if applied else ""),
        "unjudged": [
            "THE SHAPE CAME FROM THE REQUEST, NOT THE READER. D-27: Heron "
            "has one voice and what varies is the shape of the answer, "
            "chosen from the shape of the request. docs/28's row for this "
            "agent says 'at what depth for this reader' and 'judgement, so "
            "a model call' - both written against the decision that "
            "superseded them. PROPOSALS F19.",
            "NO MODEL WAS CALLED, AND THE REASON IS D-27 RATHER THAN "
            "THRIFT. Its own consequence: 'nothing has to detect who is "
            "talking. A whole class of why did it answer differently today "
            "stops being possible rather than being made visible.'",
            "%s" % ("THE USER'S OWN LINE OF %s WAS APPLIED. D-27 calls "
                    "these defaults rather than law, and asks for a DATED "
                    "line - a correction that costs nothing to record is "
                    "one nobody records." % applied["at"] if applied else
                    "NO OVERRIDE APPLIED, so this is D-27's default. They "
                    "are defaults and not law: the user is expected to "
                    "edit them, one dated line at a time."),
            "NOTHING WAS RENDERED. HERON-RPT-RND-002 turns this into a "
            "document, HERON-RPT-VAL-004 checks it says what the data "
            "says, and HERON-RPT-RED-003 decides whether it may leave.",
        ],
    }


def main(argv):
    print("REPORT COMPOSITION   the answer's shape follows the request's")
    print("=" * 72)

    print("\nD-27's table")
    for shape in sorted(SHAPES):
        print("  %-12s %-34s %s" % (shape, SHAPES[shape][0],
                                    SHAPES[shape][1][:32]))

    print("\ncomposed")
    for shape, data in (
            ("count", 47),
            ("breakdown", {"Supply": 31, "Extract": 12, "Return": 4}),
            ("narrowed", [{"id": 418302, "size": "300x300"},
                          {"id": 418303, "size": "300x300"}]),
            ("finished", {"done": "tagged 47 ducts",
                          "verified": "12 spot-checked against the model",
                          "undecided": "the 3 with no system name"}),
            ("comparison", [47, 52])):
        answer = compose(shape, data)
        print("  %-12s %s" % (shape, answer["why"][:64]))

    print("\nrefused")
    for shape, data in (("trend", 1), ("count", None),
                        ("narrowed", [{"size": "300x300"}]),
                        ("finished", {"done": "x", "undecided": "y"}),
                        ("comparison", [47])):
        answer = compose(shape, data)
        print("  %-22s %s" % (answer["refused"], answer["why"][:44]))

    undated = compose("count", 47, overrides=[{"shape": "count",
                                               "instead": "a table"}])
    print("  %-22s %s" % (undated["refused"], undated["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in compose("count", 47)["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
