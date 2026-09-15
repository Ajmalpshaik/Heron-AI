# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-BIM-011
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
BIM QA - eight things to check, and it says which of them it actually
can.

    python brain/heron_modelqa.py

WHAT IT IS FOR (docs/28, HERON-QA-BIM-011)
-------------------------------------------
"Checks the MODEL: naming, parameters, categories, families, levels,
worksets, views, MEP connectivity." T2, risk ANALYZE.

EIGHT ASPECTS, AND THEY ARE NOT EIGHT DIFFERENT PROBLEMS
----------------------------------------------------------
Five of them are naming with a different noun in front. A family type
name, a level name, a workset name, a view name and a sheet number are
all NAMES, and HERON-STD-NAM-004 already counts the shapes they take -
so they are routed there rather than checked five more times here.

Two are arithmetic nobody owns yet, and are done here:

    CATEGORIES   which the model uses, and - docs/10 s5a is explicit
                 that this matters - which it DELIBERATELY DOES NOT.
                 Set arithmetic against a profile.
    PARAMETERS   how many elements carry a value for each one. A fill
                 rate is a count, not a judgement.

One cannot be done here at all:

    MEP CONNECTIVITY   whether a duct actually connects to the next
                       duct is geometry, inside Revit. Nothing in the
                       brain can see it, and saying "checked" about a
                       thing nobody looked at is the failure this whole
                       project keeps writing about.

IT REPORTS ITS OWN COVERAGE
-----------------------------
Every aspect comes back with whether it was CHECKED, SKIPPED for want of
input, or CANNOT BE CHECKED HERE and why. A QA report that quietly omits
the aspects it could not reach reads as a clean model.

A MISSING PARAMETER IS NOT A FAILING ONE
------------------------------------------
Three states, and flattening them is how a QA report lies:

    populated    the element carries a value
    empty        the element has the parameter and it is blank
    absent       the element does not have the parameter at all

The third is usually a different problem from the second - a parameter
that was never added versus one nobody filled in - and a single
"missing" count cannot tell a modeller which afternoon they are in for.

NOTHING IS CALLED WRONG
-------------------------
Every number here is a count of what the model DOES. Whether any of it
breaches a standard needs the standard, quoted - docs/05 s150, no
source, no claim - and that is HERON-STD-CMP-002's and
HERON-STD-PVL-013's work, routed to rather than repeated.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_convention as NAM  # noqa: E402
import heron_corroborate as PVL  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's own eight words, in its own order.
ASPECTS = ("naming", "parameters", "categories", "families", "levels",
           "worksets", "views", "MEP connectivity")

# The five that are naming with a different noun in front. Routed to
# HERON-STD-NAM-004 rather than checked five more times here.
BY_NAME = ("families", "levels", "worksets", "views")

# The one nothing on this side can reach, and why.
CANNOT = {
    "MEP connectivity":
        "whether a duct connects to the next duct is geometry, inside "
        "Revit. Nothing in the brain can see it, and reporting `checked` "
        "about a thing nobody looked at is worse than reporting nothing",
}

# The three states a parameter can be in on one element. Flattening the
# last two into `missing` is how a QA report stops being useful.
POPULATED = "populated"
EMPTY = "empty"
ABSENT = "absent"


def parameters(elements, wanted=None):
    """
    Fill rates, three states kept apart.

    `elements` is a list of maps, each the parameters ONE element
    carries. A parameter absent from the map is absent from the element;
    one present and blank is empty.
    """
    elements = list(elements or [])
    names = list(wanted or [])
    if not names:
        for one in elements:
            for name in (one or {}):
                if name not in names:
                    names.append(name)

    out = []
    for name in sorted(names):
        counts = {POPULATED: 0, EMPTY: 0, ABSENT: 0}
        for one in elements:
            one = one or {}
            if name not in one:
                counts[ABSENT] += 1
            elif str(one[name] or "").strip():
                counts[POPULATED] += 1
            else:
                counts[EMPTY] += 1
        out.append({"parameter": name, "of": len(elements),
                    POPULATED: counts[POPULATED], EMPTY: counts[EMPTY],
                    ABSENT: counts[ABSENT],
                    "proportion": PVL.REF._percent(counts[POPULATED],
                                                   len(elements))})
    return out


def categories(used, profile=None):
    """
    {used, alsoInProfile, onlyHere, onlyThere} - set arithmetic.

    docs/10 s5a wants the categories a model uses AND the ones it
    deliberately does not, so both differences are reported and neither
    is called a fault.
    """
    mine = sorted(set(str(one) for one in (used or [])))
    theirs = sorted(set(str(one) for one in
                        ((profile or {}).get("categories") or [])))
    return {
        "used": mine,
        "inBoth": [one for one in mine if one in theirs],
        "onlyHere": [one for one in mine if one not in theirs],
        "onlyThere": [one for one in theirs if one not in mine],
    }


def check(model, profile=None, clauses=None):
    """
    {checked, aspects, coverage} - or a refusal. Nothing is called wrong.
    """
    model = getattr(model, "data", model)
    if not isinstance(model, dict) or not model:
        return {"checked": False, "refused": "NOT_A_MODEL",
                "why": "%r is not a model's facts. One carries `names`, and "
                       "may carry `categories` and `elements`." % (model,)}

    names = list(model.get("names") or [])
    if not names and not model.get("categories") \
            and not model.get("elements"):
        return {"checked": False, "refused": "NOTHING_TO_CHECK",
                "why": "the model's facts are empty. Nothing was read out "
                       "of Revit, which is not the same as a model with "
                       "nothing wrong with it."}

    aspects, done, skipped, cannot = [], [], [], []

    # NAMING, and the four aspects that are naming with a different noun.
    looked = NAM.look(names, clauses=clauses) if names else None
    against = PVL.validate(profile, names) if (profile and names) else None
    kinds = ([card["kind"] for card in looked["kinds"]]
             if looked and looked.get("looked") else [])

    for aspect in ASPECTS:
        if aspect in CANNOT:
            aspects.append({"aspect": aspect, "state": "cannot be checked "
                                                       "here",
                            "why": CANNOT[aspect]})
            cannot.append(aspect)
            continue

        if aspect == "naming" or aspect in BY_NAME:
            kind = None if aspect == "naming" else aspect.rstrip("s")
            here = [k for k in kinds
                    if kind is None or k == kind or k == aspect]
            if not names or (kind is not None and not here):
                aspects.append({
                    "aspect": aspect, "state": "skipped",
                    "why": "no %s names were handed in. A QA report that "
                           "omits an aspect it could not reach reads as a "
                           "clean model, so it is said."
                           % (kind or "element")})
                skipped.append(aspect)
                continue
            aspects.append({
                "aspect": aspect, "state": "checked",
                "by": "HERON-STD-NAM-004"
                      + (" and HERON-STD-PVL-013" if against else ""),
                "kinds": here,
                "shapes": [card for card in looked["kinds"]
                           if card["kind"] in here],
                "againstProfile": [card for card in (against or {}).get(
                    "kinds", []) if card["kind"] in here] or None,
            })
            done.append(aspect)
            continue

        if aspect == "categories":
            if not model.get("categories"):
                aspects.append({"aspect": aspect, "state": "skipped",
                                "why": "no categories were handed in."})
                skipped.append(aspect)
                continue
            aspects.append({"aspect": aspect, "state": "checked",
                            "by": "this agent - set arithmetic",
                            "categories": categories(model["categories"],
                                                     profile)})
            done.append(aspect)
            continue

        if aspect == "parameters":
            if not model.get("elements"):
                aspects.append({"aspect": aspect, "state": "skipped",
                                "why": "no elements were handed in, so "
                                       "there is nothing to count a fill "
                                       "rate over."})
                skipped.append(aspect)
                continue
            aspects.append({"aspect": aspect, "state": "checked",
                            "by": "this agent - three states, kept apart",
                            "parameters": parameters(
                                model["elements"], model.get("wanted"))})
            done.append(aspect)

    return {
        "checked": True,
        "aspects": aspects,
        "coverage": {"of": len(ASPECTS), "checked": done,
                     "skipped": skipped, "cannot": cannot},
        "judged": False,
        "why": "%d of %d aspects checked, %d skipped for want of input, %d "
               "that cannot be checked here. Nothing was called wrong."
               % (len(done), len(ASPECTS), len(skipped), len(cannot)),
        "unjudged": [
            "NOTHING WAS CALLED WRONG. Every number here is a count of "
            "what the model DOES. Whether any of it breaches a standard "
            "needs the standard, quoted - docs/05 s150, no source, no "
            "claim - and that is HERON-STD-CMP-002's and "
            "HERON-STD-PVL-013's work.",
            "FIVE OF THE EIGHT ARE NAMING WITH A DIFFERENT NOUN IN FRONT. "
            "A family type, a level, a workset, a view and a sheet are all "
            "NAMES, and HERON-STD-NAM-004 counts the shapes they take - so "
            "they are routed there rather than checked five more times.",
            ("%d ASPECT%s CANNOT BE CHECKED HERE AT ALL: %s. Reported as "
             "that rather than omitted, because a QA report that quietly "
             "leaves out what it could not reach reads as a clean model."
             % (len(cannot), "" if len(cannot) == 1 else "S",
                ", ".join(cannot))),
            ("%d ASPECT%s SKIPPED FOR WANT OF INPUT: %s. Nothing was read "
             "out of Revit here - this agent is handed facts, and what it "
             "was not handed it did not check."
             % (len(skipped), "" if len(skipped) == 1 else "S",
                ", ".join(skipped))
             if skipped else
             "every aspect that can be checked here had the facts it "
             "needed."),
            "A MISSING PARAMETER IS NOT A FAILING ONE. `empty` is a "
            "parameter nobody filled in; `absent` is one that was never "
            "added to the element. Those are different afternoons, and a "
            "single `missing` count cannot tell a modeller which.",
            ("A CATEGORY THE PROFILE USES AND THIS MODEL DOES NOT IS "
             "REPORTED AND NOT JUDGED. docs/10 s5a wants the categories a "
             "model deliberately does NOT use, and deliberate is exactly "
             "what a count cannot see."
             if profile else
             "NO PROFILE WAS HANDED IN, so nothing was compared with "
             "anything - the counts stand on their own."),
        ],
    }


def main(argv):
    print("BIM QA   eight aspects, and which of them it actually can check")
    print("=" * 72)
    print("\naspects: %s" % ", ".join(ASPECTS))

    import heron_exemplar as REF
    profile = REF.profile(
        [{"name": "QA2026-MEP-DUCT-SUPPLY-L%02d" % n, "kind": "element"}
         for n in range(1, 41)],
        categories=["OST_DuctCurves", "OST_DuctFitting", "OST_PipeCurves"])

    model = {
        "names": ([{"name": "QA2026-MEP-DUCT-SUPPLY-L%02d" % n,
                    "kind": "element"} for n in range(1, 31)]
                  + [{"name": "QA2026 duct copy", "kind": "element"}]
                  + [{"name": "L%02d - GROUND" % n, "kind": "level"}
                     for n in (1, 2, 3)]),
        "categories": ["OST_DuctCurves", "OST_DuctFitting", "OST_Walls"],
        "elements": [{"Mark": "D-%03d" % n, "Comments": ""}
                     for n in range(1, 21)]
                    + [{"Mark": ""} for _ in range(4)],
        "wanted": ["Mark", "Comments", "System Type"],
    }

    answer = check(model, profile=profile)
    print("\n%s" % answer["why"])
    print("\n%-18s %-24s %s" % ("aspect", "state", "by"))
    for card in answer["aspects"]:
        print("  %-16s %-24s %s"
              % (card["aspect"], card["state"], card.get("by", "")))

    for card in answer["aspects"]:
        if card["aspect"] == "categories":
            sets = card["categories"]
            print("\n  categories used here     %s" % ", ".join(sets["used"]))
            print("  in the profile too       %s" % ", ".join(sets["inBoth"]))
            print("  only here                %s"
                  % (", ".join(sets["onlyHere"]) or "none"))
            print("  only in the profile      %s"
                  % (", ".join(sets["onlyThere"]) or "none"))
        if card["aspect"] == "parameters":
            print("\n  %-14s %5s %9s %6s %7s" % ("parameter", "of",
                                                 "populated", "empty",
                                                 "absent"))
            for one in card["parameters"]:
                print("    %-12s %5d %9d %6d %7d"
                      % (one["parameter"], one["of"], one[POPULATED],
                         one[EMPTY], one[ABSENT]))

    print("\nrefused")
    for these in (None, {}, "a string", {"names": []}):
        bad = check(these)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
