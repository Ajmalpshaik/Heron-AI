# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-PVL-013
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Profile validation - convention or coincidence, and the one case where
nothing matching means the PROFILE is wrong.

    python brain/heron_corroborate.py

WHAT IT IS FOR (docs/28, HERON-STD-PVL-013)
--------------------------------------------
"Checks a standard INFERRED from a reference model before it can be
trusted. Tests it against a SECOND delivered model, separates convention
from coincidence, and surfaces the exceptions rather than flagging
correct work as wrong." T2, risk READ.

CONVENTION AND COINCIDENCE ARE TOLD APART BY COUNTING TWICE
-------------------------------------------------------------
docs/10 s5a's corroboration guard: "a pattern from one project stays
DISCOVERED. Seen across several delivered projects, it can progress."

One model shows what one team did on one job. A shape that is dominant
in BOTH is a convention; one that is dominant in the first and absent
from the second was a coincidence of that job, and
HERON-STD-REF-010 could not have known which it was.

THE CASE THIS AGENT EXISTS FOR, AND IT IS THE OPPOSITE OF THE OBVIOUS ONE
--------------------------------------------------------------------------
If almost nothing in the second model matches, the tempting reading is
that the second model is wrong - four hundred violations, a report
nobody can act on, and exactly the "flagging correct work as wrong" the
row warns about.

It is far likelier that THE PROFILE DOES NOT TRANSFER. PROPOSALS F30:
a real delivered model prefixes every name with its own job number, so
the profile's dominant shape carries segments no other project will ever
have. `A-9-A-A-A-A-A9` against `A-A-A-A9` is not a naming failure. It is
the same convention wearing a different job number.

So a match rate of nothing is reported as a finding ABOUT THE PROFILE,
with the two shapes side by side and the segments that differ named as a
CANDIDATE project code. Named, never stripped - deciding which segment
is the job is F30's open question and this agent does not answer it.

IT SURFACES AND IT DOES NOT FLAG
----------------------------------
Every name that does not take the profile's dominant shape comes back in
`unmatched`, with its shape and how many share it. Not as a violation:
docs/10 s5a is explicit that every real model has deliberate exceptions,
and a check that calls them failures is worse than no check.

NOTHING IS PROMOTED
---------------------
Corroboration is reported. Whether a corroborated pattern may climb off
DISCOVERED is docs/24's trust model and a human's yes (Golden Rule 6),
and this agent writes nothing.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_convention as NAM  # noqa: E402
import heron_exemplar as REF  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# One arithmetic across all three agents - the profiler, the naming check
# and this. Two skeletons would make every comparison meaningless.
shape_of = NAM.shape_of
ENTERS_AT = REF.ENTERS_AT

# A shape splits on whatever is not a letter-run or a digit-run, which is
# exactly the separators HERON-STD-NAM-004 kept.
_SEGMENTS = re.compile(r"[A9]+")


def segments(shape):
    """A shape's parts, in order, with the separators between them."""
    return [part for part in _SEGMENTS.findall(str(shape or "")) if part]


def _extra_at_front(longer, shorter):
    """
    How many leading segments `longer` has that `shorter` does not.

    Compared from the BACK, because a job number goes on the front and
    the convention it prefixes is what the two have in common.
    """
    a, b = segments(longer), segments(shorter)
    if len(a) <= len(b) or not b:
        return 0
    kept = 0
    while kept < len(b) and a[len(a) - 1 - kept] == b[len(b) - 1 - kept]:
        kept += 1
    # EVERY TRAILING SEGMENT HAS TO MATCH, and there has to be at least
    # one. Two shapes with nothing in common are not one convention with
    # a prefix - they are two different conventions, and calling the
    # whole of the longer one a job number would be the worst kind of
    # guess. The first version returned all of it when `b` was empty.
    return len(a) - kept if kept == len(b) else 0


def validate(profile, names):
    """
    {validated, kinds, corroborated} - or a refusal. Nothing is promoted
    and no name is called wrong.
    """
    profile = getattr(profile, "data", profile)
    if not isinstance(profile, dict) or not profile.get("profiled") \
            or not isinstance(profile.get("patterns"), list):
        return {"validated": False, "refused": "NOT_A_PROFILE",
                "why": "%r is not a profile. HERON-STD-REF-010 returns "
                       "{profiled, patterns, ...} and a refusal carries no "
                       "patterns." % (profile,)}

    names = list(names or [])
    if not names:
        return {"validated": False, "refused": "NOTHING_TO_VALIDATE",
                "why": "no second model was handed in. A profile from one "
                       "project stays %s until another project agrees with "
                       "it (docs/10 s5a), and one model cannot corroborate "
                       "itself." % ENTERS_AT}

    by_kind = {}
    for item in names:
        item = getattr(item, "data", item)
        if isinstance(item, str):
            item = {"name": item, "kind": NAM.KINDS[0]}
        if not isinstance(item, dict) \
                or not str(item.get("name") or "").strip():
            return {"validated": False, "refused": "NOT_A_NAME",
                    "why": "%r is not a name from the second model. Each is "
                           "{name, kind}." % (item,)}
        by_kind.setdefault(
            str(item.get("kind") or NAM.KINDS[0]).strip().lower(),
            []).append(str(item["name"]))

    known = dict((one["kind"], one) for one in profile["patterns"])
    kinds, transferred = [], []
    for kind in sorted(set(list(known) + list(by_kind))):
        pattern = known.get(kind)
        here = by_kind.get(kind, [])

        if pattern is None:
            kinds.append({"kind": kind, "of": len(here), "inProfile": False,
                          "why": "the profile says nothing about %s, so "
                                 "there is nothing to corroborate. A model "
                                 "doing something the reference never did "
                                 "is not a failure" % kind})
            continue
        if not here:
            kinds.append({"kind": kind, "of": 0, "inProfile": True,
                          "corroborated": False,
                          "why": "the second model has no %s at all, so the "
                                 "profile's pattern for it is neither "
                                 "confirmed nor contradicted" % kind})
            continue

        wanted = pattern["dominant"]["shape"]
        counts = {}
        for text in here:
            skeleton = shape_of(text)
            counts[skeleton] = counts.get(skeleton, 0) + 1
        ordered = sorted(counts.items(),
                         key=lambda pair: (-pair[1], pair[0]))
        matched = counts.get(wanted, 0)
        card = {
            "kind": kind, "of": len(here), "inProfile": True,
            "profileShape": wanted,
            "profileProportion": pattern["dominant"]["proportion"],
            "matched": matched,
            "proportion": REF._percent(matched, len(here)),
            "theirDominant": ordered[0][0],
            "theirProportion": REF._percent(ordered[0][1], len(here)),
            "unmatched": [{"shape": shape, "of": count,
                           "proportion": REF._percent(count, len(here))}
                          for shape, count in ordered if shape != wanted],
            "corroborated": matched > 0 and ordered[0][0] == wanted,
        }
        kinds.append(card)

        # THE FINDING ABOUT THE PROFILE, not about the model. Nothing
        # matching, and both models having a clear dominant, is the F30
        # shape: the same convention wearing a different job number.
        if matched == 0:
            extra = _extra_at_front(wanted, card["theirDominant"])
            card["profileDidNotTransfer"] = True
            card["candidateProjectSegments"] = extra
            card["why"] = (
                "NOTHING in the second model takes the profile's shape. "
                "That is a finding about the PROFILE, not %d failures: "
                "%s against %s%s. PROPOSALS F30 - a delivered model "
                "prefixes every name with its own job number, and the "
                "profile carries it."
                % (len(here), wanted, card["theirDominant"],
                   ", and the profile's first %d segment%s are a CANDIDATE "
                   "job number - named, never stripped"
                   % (extra, "" if extra == 1 else "s") if extra else ""))
            transferred.append(card)
        else:
            card["profileDidNotTransfer"] = False
            card["why"] = (
                "%d of %d take the profile's shape (%d%%). %s"
                % (matched, len(here), card["proportion"],
                   "The pattern is a convention - dominant in both models."
                   if card["corroborated"] else
                   "It is present and not dominant here, so it is not "
                   "corroborated - one project's coincidence until a third "
                   "model says otherwise."))

    checked = [card for card in kinds if card.get("inProfile") and card["of"]]
    agreed = [card for card in checked if card.get("corroborated")]
    return {
        "validated": True,
        "kinds": kinds,
        "corroborated": [card["kind"] for card in agreed],
        "coincidence": [card["kind"] for card in checked
                        if not card.get("corroborated")
                        and not card.get("profileDidNotTransfer")],
        "didNotTransfer": [card["kind"] for card in transferred],
        "promoted": False,
        "stays_at": ENTERS_AT,
        "why": "%d kind%s checked against a second model: %d corroborated, "
               "%d not dominant here, %d where the PROFILE did not "
               "transfer. Nothing was promoted and no name was called "
               "wrong."
               % (len(checked), "" if len(checked) == 1 else "s",
                  len(agreed),
                  len([card for card in checked
                       if not card.get("corroborated")
                       and not card.get("profileDidNotTransfer")]),
                  len(transferred)),
        "unjudged": [
            "CONVENTION OR COINCIDENCE IS COUNTED, NOT DECIDED. A shape "
            "dominant in BOTH models is a convention; one dominant in the "
            "first and not the second was a coincidence of that job, and "
            "HERON-STD-REF-010 could not have known which (docs/10 s5a).",
            ("%d KIND%s WHERE THE PROFILE DID NOT TRANSFER: %s. Nothing "
             "matching is a finding about the PROFILE, not a model full of "
             "failures - and reading it the other way is the \"flagging "
             "correct work as wrong\" this row exists to prevent."
             % (len(transferred), "" if len(transferred) == 1 else "S",
                ", ".join(card["kind"] for card in transferred))
             if transferred else
             "every kind the profile covers had at least one name matching "
             "it here, so the profile transferred."),
            ("A CANDIDATE JOB NUMBER IS NAMED AND NEVER STRIPPED: %s. "
             "Deciding which segment is the project is PROPOSALS F30's "
             "open question, and removing one that turns out to be part of "
             "the convention teaches the opposite of the truth."
             % ", ".join("%s (%d leading segment%s)"
                         % (card["kind"], card["candidateProjectSegments"],
                            "" if card["candidateProjectSegments"] == 1
                            else "s")
                         for card in transferred
                         if card.get("candidateProjectSegments"))
             if any(card.get("candidateProjectSegments")
                    for card in transferred) else
             "no candidate job number could be derived, because no pair of "
             "shapes shared an ending."),
            "EVERY UNMATCHED NAME IS AN EXCEPTION, NOT A VIOLATION. docs/10 "
            "s5a is explicit that every real model has deliberate ones, and "
            "a check that calls them failures is worse than no check.",
            "NOTHING WAS PROMOTED. Corroboration is reported and the "
            "pattern stays at %s. Whether it may climb is docs/24's trust "
            "model and a human's yes (Golden Rule 6)." % ENTERS_AT,
        ],
    }


def main(argv):
    print("PROFILE VALIDATION   convention or coincidence, and when the "
          "PROFILE is wrong")
    print("=" * 72)

    first = REF.profile(
        [{"name": "QA2026-MEP-DUCT-SUPPLY-L%02d" % n, "kind": "element"}
         for n in range(1, 41)]
        + [{"name": "QA2026-M-%03d" % n, "kind": "sheet"}
           for n in range(1, 13)])
    print("\nprofile from model one")
    for one in first["patterns"]:
        print("  %-9s dominant %-18s %d%%"
              % (one["kind"], one["dominant"]["shape"],
                 one["dominant"]["proportion"]))

    print("\n--- a second model with the SAME job number ---")
    same = validate(first,
                    [{"name": "QA2026-MEP-PIPE-CHW-L%02d" % n,
                      "kind": "element"} for n in range(1, 21)]
                    + [{"name": "QA2026 sheet %d" % n, "kind": "sheet"}
                       for n in (1, 2)])
    print("  %s" % same["why"])
    for card in same["kinds"]:
        print("    %-9s %s" % (card["kind"], card["why"][:60]))

    print("\n--- a second model from a DIFFERENT job ---")
    # THE PROFILE CARRIES `QA2026`, so the same convention without it has
    # one segment fewer. That is F30, and it is what this agent is for.
    other = validate(first,
                     [{"name": "MEP-DUCT-SUPPLY-L%02d" % n,
                       "kind": "element"} for n in range(1, 31)])
    print("  %s" % other["why"])
    for card in other["kinds"]:
        print("    %-9s %s" % (card["kind"], card["why"][:64]))
        if card.get("candidateProjectSegments"):
            print("              candidate job number: %d leading segment(s)"
                  % card["candidateProjectSegments"])

    print("\nrefused")
    for these, names in ((None, ["x"]), ({"profiled": False}, ["x"]),
                         (first, []), (first, [123])):
        bad = validate(these, names)
        print("  %-22s %s" % (bad["refused"], bad["why"][:40]))

    print("\nwhat this agent does not judge")
    for line in other["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
