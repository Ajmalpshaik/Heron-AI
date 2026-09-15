# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-TAX-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Taxonomy - it measures the scheme and never changes it.

    python brain/heron_taxonomy.py

WHAT IT IS FOR (docs/28, HERON-NAM-TAX-004)
--------------------------------------------
"Maintains the classification scheme." T2. The scheme is the fragment
area list in HERON-FRG-VAL-001's own module - ELE, SEL, VIEW, SHT, PAR,
MEP, GEO, QA, DOC - and that module says what maintaining it means:

    "Fixed on purpose. An unlisted area is an error rather than a guess
    - the same rule D-05 applies to Revit releases, for the same reason:
    the moment this is open, one fragment says MEP and the next says
    MECH and neither search finds both. ADDING AN AREA IS A DELIBERATE
    EDIT HERE."

So maintaining it cannot mean changing it. What is left is the useful
half: measuring where it is under strain, and writing the case a person
needs in order to make that deliberate edit.

NO THRESHOLD IS APPLIED, AND THAT IS ON PURPOSE
-------------------------------------------------
The obvious design is "warn when an area holds more than N% of
everything". Every N is invented: nothing in the specification sets one,
and an invented threshold becomes the rule by being the only one there
- the same trap HERON-NAM-VAL-002 refuses for generated names.

So this agent reports facts that need no threshold to be read:

    the share of each area
    largest against smallest, and largest against the middle
    HOW MANY EVEN SPLITS the largest would need to stop being the
      largest - a whole number, derived, and true whatever anybody
      thinks "too big" means
    areas with nothing in them at all
    ids naming an area the scheme does not have

A person reads those and decides. The agent does not decide and does not
hint, because a hint with no stated rule behind it is a threshold that
has been hidden rather than avoided.

A PROPOSAL COMES BACK AS A PROPOSAL
-------------------------------------
Hand it a change and it refuses to make one - THE_SCHEME_IS_NOT_CHANGED_
HERE - and returns the argument for it, with the numbers filled in. That
is the whole of its usefulness on the write side: the deliberate edit
stays deliberate, and whoever makes it has the counts in front of them
rather than an impression.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What somebody may propose. Each is a deliberate edit to heron_fragment,
# and each comes back as a written case rather than as a change.
PROPOSALS = ("add", "rename", "split", "merge", "remove")


def _area_of(identifier):
    """The area an id names, or None if it is not shaped like one."""
    parts = str(identifier or "").strip().split("-")
    if len(parts) != 3:
        return None
    return parts[1] or None


def _splits_to_fall(counts, area):
    """
    How many even splits `area` needs to stop being the largest.

    Threshold-free and whole: a fact about the numbers rather than an
    opinion about them. 0 when it is not the largest already, and None
    when no number of splits would do it.

    THE None CASE IS REAL AND IT LOOPED FOREVER ON THE FIRST RUN. When
    every other area holds nothing, dividing a positive count never
    reaches zero, so the loop below never ends. It is not a contrived
    edge either: one id in one area is exactly what a new workspace
    has, and it is the first thing this agent would be asked about.
    """
    held = counts.get(area, 0)
    others = [count for name, count in counts.items() if name != area]
    if not others or held <= max(others):
        return 0
    biggest = max(others)
    if biggest <= 0:
        return None
    into = 1
    while held / float(into) > biggest:
        into += 1
    return into - 1


def review(identifiers, scheme=None, propose=None):
    """
    {areas, largest, undeclared, why, unjudged} - or a refusal.

    Nothing is changed. `scheme` defaults to HERON-FRG-VAL-001's own
    list, read from it rather than copied.
    """
    areas = FRAG.AREAS if scheme is None else scheme
    if not isinstance(areas, dict) or not areas:
        return {"changed": False, "refused": "NOT_A_SCHEME",
                "why": "a scheme is a map of area to what it covers. %s is "
                       "not one, and measuring against a scheme that is not "
                       "there would report every id as undeclared."
                       % type(areas).__name__}

    if propose is not None:
        what = str((propose or {}).get("do")
                   if isinstance(propose, dict) else propose).strip().lower()
        if what not in PROPOSALS:
            return {"changed": False, "refused": "NOT_A_PROPOSAL",
                    "why": "'%s' is not something that can be proposed about "
                           "a scheme. Known: %s."
                           % (what, ", ".join(PROPOSALS))}

    if not identifiers:
        return {"changed": False, "refused": "NOTHING_TO_CLASSIFY",
                "why": "no ids were given. An empty scheme report says every "
                       "area is unused, which is a statement about the call "
                       "rather than about the scheme."}

    counts = dict((name, 0) for name in areas)
    undeclared, unshaped = {}, []
    for identifier in identifiers:
        area = _area_of(identifier)
        if area is None:
            unshaped.append(str(identifier))
            continue
        if area in counts:
            counts[area] += 1
        else:
            undeclared.setdefault(area, []).append(str(identifier))

    placed = sum(counts.values())
    if undeclared:
        return {"changed": False, "refused": "AREA_NOT_DECLARED",
                "undeclared": dict((area, sorted(ids))
                                   for area, ids in undeclared.items()),
                "why": "%d id(s) name %d area(s) the scheme does not have: "
                       "%s. An unlisted area is an error rather than a "
                       "guess - the moment it is open, one fragment says "
                       "MEP and the next says MECH and neither search finds "
                       "both. Adding one is a deliberate edit to "
                       "heron_fragment, not something reported and moved "
                       "past."
                       % (sum(len(ids) for ids in undeclared.values()),
                          len(undeclared), ", ".join(sorted(undeclared)))}

    order = sorted(counts.items(), key=lambda pair: (-pair[1], pair[0]))
    numbers = sorted(counts.values())
    middle = numbers[len(numbers) // 2]
    biggest, most = order[0]
    smallest, fewest = order[-1]
    empty = [name for name, count in order if count == 0]
    falls = _splits_to_fall(counts, biggest)

    answer = {
        "changed": False,
        "areas": [{"area": name, "held": count,
                   "share": round(100.0 * count / placed, 1) if placed else 0.0,
                   "covers": areas[name]}
                  for name, count in order],
        "of": placed, "unshaped": unshaped,
        "largest": biggest, "smallest": smallest,
        "largest_over_smallest": (round(most / float(fewest), 1)
                                  if fewest else None),
        "largest_over_middle": (round(most / float(middle), 1)
                                if middle else None),
        "splits_to_fall": falls,
        "empty": empty,
        "why": "%d id(s) across %d area(s). '%s' holds %d (%.1f%%) and '%s' "
               "holds %d. No threshold is applied: nothing in the "
               "specification sets one, and an invented one becomes the "
               "rule by being the only one there."
               % (placed, len(areas), biggest, most,
                  100.0 * most / placed if placed else 0.0, smallest, fewest),
        "unjudged": [
            "NOTHING WAS CHANGED AND NOTHING IS RECOMMENDED. The scheme is "
            "fixed on purpose in heron_fragment, whose own comment says "
            "adding an area is a DELIBERATE EDIT there.",
            "NO THRESHOLD WAS APPLIED AND NO HINT WAS GIVEN. A hint with "
            "no stated rule behind it is a threshold hidden rather than "
            "avoided. %s, which is a fact about the numbers and not an "
            "opinion about them."
            % ("'%s' would need %d even split(s) to stop being the largest"
               % (biggest, falls) if falls is not None else
               "no number of even splits would stop '%s' being the "
               "largest, because every other area holds nothing" % biggest),
            "%s" % ("%d area(s) hold nothing: %s. An unused area is not a "
                    "wrong one - it may be waiting for work nobody has "
                    "done - so it is reported and not judged."
                    % (len(empty), ", ".join(empty)) if empty else
                    "every area in the scheme holds at least one id."),
            "%s" % ("%d id(s) are not shaped like one and were counted "
                    "nowhere: %s. Golden Rule 14 - carried, not dropped."
                    % (len(unshaped), ", ".join(unshaped[:3]))
                    if unshaped else
                    "every id handed in was shaped like one."),
        ],
    }

    if propose is not None:
        what = str((propose or {}).get("do")
                   if isinstance(propose, dict) else propose).strip().lower()
        about = str((propose or {}).get("area") or "").strip() \
            if isinstance(propose, dict) else ""
        held = counts.get(about)
        answer["changed"] = False
        answer["refused"] = "THE_SCHEME_IS_NOT_CHANGED_HERE"
        answer["case"] = (
            "Proposal: %s %s.\n"
            "  It holds %s of %d id(s)%s.\n"
            "  The largest area is '%s' with %d; the smallest is '%s' with "
            "%d.\n"
            "  %s.\n"
            "  No threshold is applied here and none is implied. Making "
            "this change is a deliberate edit to heron_fragment.AREAS."
            % (what, about or "(no area named)",
               "nothing" if held is None else str(held), placed,
               "" if held is None else
               " (%.1f%%)" % (100.0 * held / placed if placed else 0.0),
               biggest, most, smallest, fewest,
               "'%s' would need %d even split(s) to stop being the largest"
               % (biggest, falls) if falls is not None else
               "no number of even splits would stop '%s' being the largest, "
               "because every other area holds nothing" % biggest))
        answer["why"] = ("a proposal comes back as a proposal. %s"
                         % answer["why"])
    return answer


def main(argv):
    print("TAXONOMY   it measures the scheme and never changes it")
    print("=" * 72)

    import io
    folder = os.path.join(ROOT, "brain", "fragments")
    ids = []
    for entry in sorted(os.listdir(folder)):
        card = os.path.join(folder, entry, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        for line in io.open(card, encoding="utf-8"):
            if line.strip().startswith("id:"):
                ids.append(line.split(":", 1)[1].strip().strip('"').strip("'"))
                break

    answer = review(ids)
    print("\n%s" % answer["why"])
    for row in answer["areas"]:
        print("  %-5s %4d  %5.1f%%  %s"
              % (row["area"], row["held"], row["share"], row["covers"]))
    print("\n  largest over smallest  %s" % answer["largest_over_smallest"])
    print("  largest over middle    %s" % answer["largest_over_middle"])
    print("  even splits to fall    %d" % answer["splits_to_fall"])

    asked = review(ids, propose={"do": "split", "area": "VIEW"})
    print("\n%s" % asked["refused"])
    for line in asked["case"].split("\n"):
        print("  %s" % line)

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
