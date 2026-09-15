# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-REF-010
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Reference model profiler - extract the pattern, then discard the model.

    python brain/heron_exemplar.py

WHAT IT IS FOR (docs/28, HERON-STD-REF-010)
--------------------------------------------
"Infers a standard from a correctly delivered model. Extracts the
profile, DISCARDS THE MODEL." T3, risk READ. The owner's own idea,
2026-08-27:

    "You can give the AI a completed work for a reference, so Heron will
     understand easily."

docs/10 s5a says why it is the right instinct, and it is worth keeping
in front of this file: nobody hands a junior a sixty-page standard. They
are told "look at how we did Tower A."

DISCARDING THE MODEL IS NOT TIDINESS, AND IT IS CHECKED
---------------------------------------------------------
docs/10 s5a calls it "the most important design decision here". A
reference model belongs to a CLIENT. The pattern is the user's; the
geometry and the project data are not, and a profile that carried
examples would be somebody's deliverable sitting in the knowledge base
(Golden Rule 5, docs/12 s4).

So the profile holds SHAPES AND COUNTS and nothing else. No element
name, no example, no id, no project. And it is not left as a promise:
`kept_nothing` re-reads the finished profile against every name it was
given and reports any that survived. A guarantee nothing checks is a
comment.

THE FIVE GUARDS ARE docs/10 s5a's OWN, AND FOUR OF THEM ARE REFUSALS
----------------------------------------------------------------------
    FREQUENCY       "Done 400 times in a model, it is a convention.
                    Done once, it is not. Record the count AND THE
                    PROPORTION, always." Both numbers travel with every
                    pattern - and no line is drawn between them, because
                    the document asks for the numbers and not for a
                    verdict.

    CORROBORATION   "A pattern from one project stays DISCOVERED." One
                    model, one project, bottom rung - and the rung is
                    HERON-IMP-APR-014's own constant rather than a
                    string typed here.

    APPROVAL        "It PROPOSES. Becoming the company standard needs an
                    explicit yes." So `adopted` is always false.

    CONFLICT        "A new reference disagreeing with an existing
                    standard is a CONFLICT, not an overwrite." Nothing
                    here reads an existing standard, and nothing here
                    writes one - HERON-RAG-CNF-015 surfaces the
                    disagreement and HERON-STD-PRJ-009 rules on it.

    EXCEPTIONS      "Every real model has deliberate exceptions. A
                    profile should record the dominant pattern AND ITS
                    KNOWN EXCEPTIONS, or it will flag correct work as
                    wrong." So the minority shapes are recorded as
                    exceptions, by that name, and never as violations.

A MODEL IS EVIDENCE, NOT A STANDARD
-------------------------------------
docs/10 s5a again: "a delivered model also contains mistakes, and Heron
must not learn a mistake as a rule." Everything here is a count of what
was done. Whether any of it is RIGHT is the human yes this agent
proposes to, and nothing in the answer pretends otherwise.

THE SHAPE RULE IS HERON-STD-NAM-004's
---------------------------------------
Bound rather than reimplemented, so the profile a reference produces and
the shapes a new model is checked against are the same arithmetic. Two
different skeletons would make every comparison meaningless.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_convention as NAM  # noqa: E402
import heron_import as IMPORT  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# HERON-STD-NAM-004's skeleton, by identity. The profile and the check
# against it have to be the same arithmetic or the comparison means
# nothing.
shape_of = NAM.shape_of
KINDS = NAM.KINDS

# docs/10 s5a: "a pattern from one project stays DISCOVERED". The rung is
# HERON-IMP-APR-014's constant, not a string typed here.
ENTERS_AT = IMPORT.ENTERS_AT

# What may appear in a profile. Anything else is the model, and the model
# is discarded.
PROFILE_CARRIES = ("kind", "shape", "of", "proportion", "dominant",
                   "exceptions", "separators")


def _percent(part, whole):
    """A proportion as a whole number of per cent, rounded down."""
    return int(part * 100 / whole) if whole else 0


def kept_nothing(profile, names):
    """
    Which of the model's names survived into the profile.

    Re-read rather than promised. Every string anywhere in the profile is
    searched for every name that went in, so a leak through a field
    nobody thought about is still found.
    """
    hay = _strings(profile)
    return sorted(set(name for name in names
                      if name and any(name in one for one in hay)))


def _strings(thing):
    """Every string anywhere inside a nested answer."""
    out = []
    if isinstance(thing, str):
        out.append(thing)
    elif isinstance(thing, dict):
        for key, value in thing.items():
            out.append(str(key))
            out.extend(_strings(value))
    elif isinstance(thing, (list, tuple)):
        for value in thing:
            out.extend(_strings(value))
    return out


def profile(names, categories=None):
    """
    {profiled, patterns, enters_at, adopted} - or a refusal. The model is
    discarded: no name, no example and no id survives.
    """
    names = list(names or [])
    if not names:
        return {"profiled": False, "refused": "NOTHING_TO_PROFILE",
                "why": "no names were handed in. A reference model is read "
                       "for what it did, and nothing was offered."}

    by_kind, given = {}, []
    for item in names:
        item = getattr(item, "data", item)
        if isinstance(item, str):
            item = {"name": item, "kind": KINDS[0]}
        if not isinstance(item, dict) \
                or not str(item.get("name") or "").strip():
            return {"profiled": False, "refused": "NOT_A_NAME",
                    "why": "%r is not a name from the model. Each is "
                           "{name, kind}." % (item,)}
        text = str(item["name"])
        given.append(text)
        kind = str(item.get("kind") or KINDS[0]).strip().lower()
        by_kind.setdefault(kind, []).append(text)

    patterns = []
    for kind in sorted(by_kind):
        here = by_kind[kind]
        counts, marks = {}, {}
        for text in here:
            skeleton = shape_of(text)
            counts[skeleton] = counts.get(skeleton, 0) + 1
            for mark in NAM.separators(text):
                marks.setdefault(skeleton, [])
                if mark not in marks[skeleton]:
                    marks[skeleton].append(mark)

        ordered = sorted(counts.items(), key=lambda pair: (-pair[1], pair[0]))
        top, most = ordered[0]
        patterns.append({
            "kind": kind,
            "of": len(here),
            # THE DOMINANT PATTERN, with both numbers docs/10 s5a asks
            # for. No line is drawn between them - the document asks for
            # the count and the proportion, not for a verdict.
            "dominant": {"shape": top, "of": most,
                         "proportion": _percent(most, len(here)),
                         "separators": marks.get(top, [])},
            # AND ITS KNOWN EXCEPTIONS, by that name. docs/10 s5a: a
            # profile that records only the dominant pattern will flag
            # correct work as wrong.
            "exceptions": [{"shape": shape, "of": count,
                            "proportion": _percent(count, len(here)),
                            "separators": marks.get(shape, [])}
                           for shape, count in ordered[1:]],
        })

    card = {
        "profiled": True,
        "patterns": patterns,
        "categories": sorted(set(str(one) for one in (categories or []))),
        "enters_at": ENTERS_AT,
        "adopted": False,
        "of": len(given),
    }

    # THE DISCARD, RE-READ RATHER THAN PROMISED.
    survived = kept_nothing(card, given)
    card["kept_nothing"] = not survived
    card["survived"] = survived
    card["why"] = (
        "%d name%s across %d kind%s. %s Enters at %s - one project is one "
        "project (docs/10 s5a). Nothing is adopted; this proposes."
        % (len(given), "" if len(given) == 1 else "s", len(patterns),
           "" if len(patterns) == 1 else "s",
           "The model is discarded: no name survived into the profile."
           if not survived else
           "WARNING: %d name(s) survived into the profile - %s."
           % (len(survived), ", ".join(survived)),
           ENTERS_AT))
    card["unjudged"] = [
        "WHETHER ANY OF THIS IS RIGHT. docs/10 s5a: a delivered model "
        "also contains mistakes, and Heron must not learn a mistake as a "
        "rule. Everything here is a count of what was DONE.",
        "THE COUNT AND THE PROPORTION BOTH TRAVEL, AND NO LINE IS DRAWN "
        "BETWEEN THEM. docs/10 s5a asks for both numbers - \"done 400 "
        "times it is a convention, done once it is not\" - and asks for "
        "no threshold, so none is invented.",
        ("%d SHAPE%s RECORDED AS AN EXCEPTION, NOT A VIOLATION. Every real "
         "model has deliberate ones, and a profile holding only the "
         "dominant pattern flags correct work as wrong."
         % (sum(len(one["exceptions"]) for one in patterns),
            "" if sum(len(one["exceptions"])
                      for one in patterns) == 1 else "S")
         if any(one["exceptions"] for one in patterns) else
         "every name in every kind took the dominant shape, so there were "
         "no exceptions to record."),
        "IT PROPOSES. Becoming the company standard needs an explicit yes "
        "(Golden Rule 6), so `adopted` is false and this enters at %s - "
        "one project is one project until another agrees with it."
        % ENTERS_AT,
        "NOTHING WAS COMPARED WITH AN EXISTING STANDARD. docs/10 s5a: a "
        "new reference disagreeing with one is a CONFLICT, not an "
        "overwrite. HERON-RAG-CNF-015 surfaces that and "
        "HERON-STD-PRJ-009 rules on it.",
        "THE SHAPE CARRIES WHATEVER THE NAMES CARRIED, INCLUDING THE "
        "PROJECT CODE. If a delivered model prefixes every name with its "
        "own job number, the dominant shape has segments no other project "
        "will ever have, and checking a second model against it would "
        "flag all of it. Stripping them would mean guessing which segment "
        "is the project - PROPOSALS F30.",
        ("THE MODEL IS DISCARDED AND IT WAS CHECKED, not promised: every "
         "string in this answer was searched for every name that went in, "
         "and %s."
         % ("none survived" if not survived
            else "%d survived" % len(survived))),
    ]
    return card


def main(argv):
    print("REFERENCE MODEL PROFILER   extract the pattern, discard the model")
    print("=" * 72)
    print("\nshape rule: HERON-STD-NAM-004's, by identity")
    print("enters at : %s (docs/10 s5a - one project is one project)"
          % ENTERS_AT)

    # A DELIVERED MODEL, WITH THE CLIENT ALL OVER IT. Every name carries
    # the project code, which is exactly what must not survive.
    names = [{"name": "QA2026-MEP-DUCT-SUPPLY-L%02d" % n, "kind": "element"}
             for n in range(1, 41)]
    names += [{"name": "QA2026 duct copy %d" % n, "kind": "element"}
              for n in (1, 2)]
    names += [{"name": "QA2026-M-%03d" % n, "kind": "sheet"}
              for n in range(1, 13)]
    names += [{"name": "QA2026_M_099", "kind": "sheet"}]

    answer = profile(names, categories=["OST_DuctCurves", "OST_Sheets"])
    print("\n%s" % answer["why"])
    for one in answer["patterns"]:
        top = one["dominant"]
        print("\n  %-9s %d name(s)" % (one["kind"], one["of"]))
        print("    DOMINANT   %-16s %4d  %3d%%  separators %s"
              % (top["shape"], top["of"], top["proportion"],
                 ", ".join("%r" % m for m in top["separators"]) or "none"))
        for other in one["exceptions"]:
            print("    EXCEPTION  %-16s %4d  %3d%%"
                  % (other["shape"], other["of"], other["proportion"]))

    print("\nthe discard, re-read rather than promised")
    print("  kept nothing : %s" % answer["kept_nothing"])
    print("  survived     : %s" % (", ".join(answer["survived"]) or "nothing"))
    print("  and the client code `QA2026` appears in the profile: %s"
          % ("QA2026" in " ".join(_strings(answer))))

    print("\nrefused")
    for these in ([], None, [123], [{"kind": "sheet"}]):
        bad = profile(these)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
