# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-NAM-004
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Naming standard - consistency is countable, and correctness is quoted.

    python brain/heron_convention.py

WHAT IT IS FOR (docs/28, HERON-STD-NAM-004)
--------------------------------------------
"Naming rules for elements, views, sheets, files." T2, risk ANALYZE.

TWO DIFFERENT QUESTIONS WEAR ONE WORD
---------------------------------------
    IS THIS NAME CONSISTENT with the others?   countable, here
    IS THIS NAME CORRECT per the standard?     quoted, not decided here

The first is arithmetic. `SUP-300-GALV` and `EXH-250-GALV` have the same
SHAPE - three groups, letters, digits, letters, joined by hyphens - and
`Duct 4 (copy)` does not. Counting shapes is what a modeller actually
does by eye when they scroll a schedule, and it needs no standard and no
model call.

The second needs the rule, in the rule's own words, from a document
somebody loaded. docs/05 s150: no source, no claim. So this agent never
says a name is wrong. It says what the shapes ARE, attaches whatever
clause was handed to it, and asks.

THE SHAPE, AND WHY IT IS THIS SHAPE
-------------------------------------
Every run of letters becomes `A`, every run of digits `9`, and every
other character stays exactly as it is:

    SUP-300-GALV          A-9-A
    EXH-250-GALV          A-9-A
    Duct 4 (copy)         A 9 (A)
    L01-MECH-PLAN         A9-A-A

Runs collapse rather than repeat, because `SUP` and `EXHAUST` are the
same shape and a per-character skeleton would call them different.
Separators survive untouched, because on a real job the separator IS the
convention - a standard that says hyphens and a family called
`SUP_300_GALV` is the finding.

CASE IS KEPT, AND THAT IS DELIBERATE. `SUP` and `sup` are one shape by
this rule, and telling them apart is a rule the standard has to state.
The letters' case travels in the examples so a reader can see it.

THE OUTLIER IS REPORTED AND NEVER CALLED WRONG
------------------------------------------------
One name in four hundred with a shape of its own is the thing worth
looking at, and that is a COUNT, not a verdict. Calling it wrong needs a
threshold - 1%? 5? - and every candidate is a number nobody chose. So
shapes come back sorted by how many names carry them, and the judgement
goes to the host with the clause attached.

ONE QUESTION PER KIND, NOT PER NAME
-------------------------------------
docs/19 s96 again: batch work should not ask a frontier model the same
thing four thousand times. A schedule of 4,000 ducts is a handful of
shapes, so it is a handful of questions.

NO CLAUSE MEANS NO JUDGEMENT, AND THE COUNTS STILL COME BACK
--------------------------------------------------------------
Handed no rule, this does not refuse - "here is what your naming
actually looks like" is worth having on its own. It simply makes no
`asks`, and says why.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's own four words. What each of them is called in a model is
# Revit's business; this agent only groups what it is given.
KINDS = ("element", "view", "sheet", "file")

_LETTERS = "A"
_DIGITS = "9"
_RUN = re.compile(r"([A-Za-z]+|[0-9]+)")


def shape_of(name):
    """
    A name's skeleton: letter runs as A, digit runs as 9, everything else
    exactly as written.

    Runs collapse. `SUP` and `EXHAUST` are one shape, because a standard
    that cared about the length would have to say so and this agent
    invents nothing.
    """
    out = []
    for part in _RUN.split(str(name or "")):
        if not part:
            continue
        if part[0].isalpha():
            out.append(_LETTERS)
        elif part[0].isdigit():
            out.append(_DIGITS)
        else:
            out.append(part)
    return "".join(out)


def separators(name):
    """Every non-alphanumeric character in a name, in the order seen."""
    out = []
    for character in str(name or ""):
        if not character.isalnum() and character not in out:
            out.append(character)
    return out


def look(names, clauses=None):
    """
    {looked, kinds, asks} - or a refusal. No name is called wrong.
    """
    names = list(names or [])
    if not names:
        return {"looked": False, "refused": "NOTHING_TO_CHECK",
                "why": "no names were handed in."}

    by_kind = {}
    for item in names:
        item = getattr(item, "data", item)
        if isinstance(item, str):
            item = {"name": item, "kind": KINDS[0]}
        if not isinstance(item, dict):
            return {"looked": False, "refused": "NOT_A_NAME",
                    "why": "%r is not a name. Each is {name, kind}, and a "
                           "bare string is read as %r." % (item, KINDS[0])}
        text = str(item.get("name") or "")
        if not text.strip():
            return {"looked": False, "refused": "NOT_A_NAME",
                    "why": "a name is blank. An empty name is a finding "
                           "about the model, not something to give a shape."}
        kind = str(item.get("kind") or KINDS[0]).strip().lower()
        by_kind.setdefault(kind, []).append(text)

    clauses = list(clauses or [])
    out, asks = [], []
    for kind in sorted(by_kind):
        shapes = {}
        for text in by_kind[kind]:
            card = shapes.setdefault(shape_of(text),
                                     {"shape": shape_of(text), "names": [],
                                      "separators": []})
            card["names"].append(text)
            for mark in separators(text):
                if mark not in card["separators"]:
                    card["separators"].append(mark)

        # SORTED BY HOW MANY CARRY IT, commonest first, then by the shape
        # itself so two shapes with the same count do not swap between runs.
        ordered = sorted(shapes.values(),
                         key=lambda card: (-len(card["names"]), card["shape"]))
        for card in ordered:
            card["of"] = len(card["names"])
            card["examples"] = sorted(card["names"])[:3]
            del card["names"]

        marks = []
        for card in ordered:
            for mark in card["separators"]:
                if mark not in marks:
                    marks.append(mark)

        kind_card = {"kind": kind, "of": len(by_kind[kind]),
                     "shapes": ordered, "separators": marks,
                     "commonest": ordered[0]["shape"],
                     "alone": [card["shape"] for card in ordered
                               if card["of"] == 1]}
        out.append(kind_card)

        if clauses:
            asks.append({
                "kind": kind,
                "question": "%d %s name%s in %d shape%s. Which of these "
                            "shapes does the standard allow, and which does "
                            "it not?"
                            % (kind_card["of"], kind,
                               "" if kind_card["of"] == 1 else "s",
                               len(ordered), "" if len(ordered) == 1 else "s"),
                "shapes": ordered,
                "separators": marks,
                "clauses": clauses,
            })

    everything = sum(card["of"] for card in out)
    lonely = sum(len(card["alone"]) for card in out)
    return {
        "looked": True,
        "of": everything,
        "kinds": out,
        "asks": asks,
        "clauses": clauses,
        "judged": False,
        "why": "%d name%s across %d kind%s, in %d shape%s. %d shape%s "
               "carried by exactly one name. %s"
               % (everything, "" if everything == 1 else "s", len(out),
                  "" if len(out) == 1 else "s",
                  sum(len(card["shapes"]) for card in out),
                  "" if sum(len(card["shapes"])
                            for card in out) == 1 else "s",
                  lonely, "" if lonely == 1 else "s",
                  "%d clause%s attached for the host to judge against."
                  % (len(clauses), "" if len(clauses) == 1 else "s")
                  if clauses else
                  "NO CLAUSE WAS HANDED IN, so nothing was asked and "
                  "nothing is judged."),
        "unjudged": [
            "NO NAME WAS CALLED WRONG. Shape is CONSISTENCY, which is "
            "arithmetic; correctness is the STANDARD, which has to be "
            "quoted from a document somebody loaded (docs/05 s150 - no "
            "source, no claim).",
            ("%d SHAPE%s CARRIED BY EXACTLY ONE NAME, reported as a count "
             "and not as a fault. One name in four hundred with a shape of "
             "its own is worth looking at; calling it wrong needs a "
             "threshold, and every candidate for one is a number nobody "
             "chose." % (lonely, "" if lonely == 1 else "S")
             if lonely else
             "every shape is carried by more than one name, so there is no "
             "outlier to look at."),
            ("SEPARATORS ARE REPORTED SEPARATELY: %s. On a real job the "
             "separator IS the convention, and a standard that says hyphens "
             "against a family called SUP_300_GALV is the finding."
             % ", ".join("%r" % mark for mark in
                         sorted(set(mark for card in out
                                    for mark in card["separators"])))
             if any(card["separators"] for card in out) else
             "no name carried a separator at all, which is itself worth "
             "seeing."),
            "CASE IS NOT A SHAPE. `SUP` and `sup` group together here, and "
            "telling them apart is a rule the standard has to state. The "
            "case travels in the examples so a reader can see it.",
            ("ONE QUESTION PER KIND, NOT PER NAME - %d question%s for %d "
             "names. docs/19 s96: batch work should not ask the same thing "
             "four thousand times."
             % (len(asks), "" if len(asks) == 1 else "s", everything)
             if asks else
             "NOTHING WAS ASKED, because no clause was handed in. The "
             "counts above stand on their own; a judgement would not."),
        ],
    }


def main(argv):
    print("NAMING STANDARD   consistency is countable, correctness is quoted")
    print("=" * 72)
    print("\nshape: letter runs -> %s, digit runs -> %s, everything else "
          "as written" % (_LETTERS, _DIGITS))
    for name in ("SUP-300-GALV", "EXHAUST-250-GALV", "Duct 4 (copy)",
                 "L01-MECH-PLAN", "SUP_300_GALV"):
        print("  %-20s %s" % (name, shape_of(name)))

    ducts = [{"name": "SUP-%d-GALV" % (100 + n * 50), "kind": "element"}
             for n in range(8)]
    ducts.append({"name": "Duct 4 (copy)", "kind": "element"})
    ducts.append({"name": "SUP_600_GALV", "kind": "element"})
    sheets = [{"name": "M-%03d" % n, "kind": "sheet"} for n in (1, 2, 3)]
    sheets.append({"name": "Sheet 1", "kind": "sheet"})

    print("\nwith no clause - counts only")
    answer = look(ducts + sheets)
    print("  %s" % answer["why"])

    clause = {"document": "Acme BIM Standard 2026", "locator": "3.2",
              "text": "Every duct type shall be named SYSTEM-SIZE-MATERIAL, "
                      "separated by hyphens."}
    answer = look(ducts + sheets, clauses=[clause])
    print("\nwith the clause attached")
    print("  %s" % answer["why"])
    for card in answer["kinds"]:
        print("\n  %-10s %d name(s), separators %s"
              % (card["kind"], card["of"],
                 ", ".join("%r" % m for m in card["separators"]) or "none"))
        for shape in card["shapes"]:
            print("    %-14s %3d  %s" % (shape["shape"], shape["of"],
                                         ", ".join(shape["examples"])))
    for ask in answer["asks"]:
        print("\n  ASKS  %s" % ask["question"])

    print("\nrefused")
    for these in ([], None, [123], [{"name": "   "}]):
        bad = look(these)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
