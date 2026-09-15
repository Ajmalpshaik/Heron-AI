# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-LRN-ANA-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Learning analysis - frequency and corroboration are two things, and the
row names both.

    python brain/heron_analysis.py

WHAT IT IS FOR (docs/28, HERON-LRN-ANA-002)
--------------------------------------------
"Is this genuinely new and reusable, or a one-off? FREQUENCY AND
CORROBORATION, NOT NOVELTY." T2, risk READ. It reads what
HERON-LRN-OBS-001 observed and says which of three things each pattern
is. It extracts nothing - that is HERON-LRN-EXT-003.

THE ROW NAMES TWO WORDS AND THEY ARE NOT THE SAME WORD
--------------------------------------------------------
    FREQUENCY       how often it was seen.
    CORROBORATION   how many INDEPENDENT places saw it - projects,
                    people, workflows.

Something seen ten times in one project by one person is frequent and
uncorroborated, and that is the most dangerous shape in the set: the
count looks like evidence and the evidence is one situation repeated.
Reporting only the frequency is the mistake the row's second word
exists to prevent, so both travel in every answer and neither is
reported without the other.

NOT NOVELTY - AND A CALLER SAYING SO CHANGES NOTHING
------------------------------------------------------
A pattern handed in marked new, novel, interesting or unusual is
weighted exactly as it would be without the word. The flag is echoed
back under `claimed` so the caller can see it was read and discarded,
because silently ignoring an input is how somebody keeps sending it.

TWO BOUNDARIES, BOTH AT "MORE THAN ONE", AND NEITHER IS TUNED
---------------------------------------------------------------
    seen once                 an observation
    seen more than once       a repeat
    from one place            one situation
    from more than one place  corroborated

That is the whole of the arithmetic. There is no "enough" number
anywhere, because every "enough" is invented - the same wall
HERON-NAM-TAX-004 refused to build - and the counts travel so a person
reading "seen 9 times, all in Tower A, all by one person" needs nothing
further from this agent.
"""

from __future__ import annotations

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# What the caller may say that this agent deliberately ignores.
NOVELTY = ("new", "novel", "interesting", "unusual", "first", "never seen")

# The three answers, in the order a reader should take them.
VERDICTS = ("corroborated", "frequent_but_uncorroborated", "a_one_off")

# What makes a source independent. Named so the answer can say which kind
# of corroboration it found rather than only that it found some.
SOURCES = ("project", "by", "workflow")

# What to call each one in a sentence. "2 bys" is not English, and an
# answer a person cannot read is an answer they will not read.
CALLED = {"project": ("project", "projects"),
          "by": ("person", "people"),
          "workflow": ("workflow", "workflows")}


def analyse(observations):
    """
    {patterns, why, unjudged} - or a refusal.

    Nothing is extracted and nothing is promoted. Each pattern gets a
    verdict, its counts, and what it was corroborated across.
    """
    if not observations:
        return {"analysed": False, "refused": "NOTHING_TO_ANALYSE",
                "why": "no observations were handed in. Deciding that "
                       "nothing is a pattern after seeing nothing is the "
                       "same answer as deciding it after seeing "
                       "everything, and they must not be confused."}

    groups = {}
    for one in observations:
        if not isinstance(one, dict):
            return {"analysed": False, "refused": "NOT_AN_OBSERVATION",
                    "why": "%r is not an observation. Each carries a "
                           "`pattern` and where it was seen." % (one,)}
        pattern = str(one.get("pattern") or "").strip()
        if not pattern:
            return {"analysed": False, "refused": "NOT_AN_OBSERVATION",
                    "why": "an observation names no pattern. Something seen "
                           "that nobody can name cannot be counted with "
                           "anything else, so it would be a one-off however "
                           "often it happened."}
        book = groups.setdefault(pattern, {"seen": 0, "claimed": set()})
        book["seen"] += 1
        for source in SOURCES:
            value = str(one.get(source) or "").strip()
            if value:
                book.setdefault(source, set()).add(value)
        for word in NOVELTY:
            for field in ("note", "claim", "why"):
                if word in str(one.get(field) or "").lower():
                    book["claimed"].add(word)

    patterns = []
    for pattern in sorted(groups):
        book = groups[pattern]
        across = dict((source, sorted(book[source]))
                      for source in SOURCES if book.get(source))
        independent = [source for source, values in across.items()
                       if len(values) > 1]

        if book["seen"] < 2:
            verdict = "a_one_off"
            why = ("seen once. That is an observation, not a repeat - and "
                   "it stays one however new it looks, because the row "
                   "says frequency and corroboration, not novelty.")
        elif not independent:
            verdict = "frequent_but_uncorroborated"
            why = ("seen %d times and all of it from %s. This is the "
                   "shape that reads as evidence and is one situation "
                   "repeated - the count looks like proof and the proof "
                   "is a single %s."
                   % (book["seen"],
                      " and ".join("one %s (%s)" % (CALLED[source][0],
                                                     values[0])
                                   for source, values in sorted(
                                       across.items())) or "one place",
                      CALLED[sorted(across)[0]][0] if across else "place"))
        else:
            verdict = "corroborated"
            why = ("seen %d times across %s. Both words the row names are "
                   "satisfied: it happened more than once, and more than "
                   "one %s saw it."
                   % (book["seen"],
                      " and ".join("%d %s" % (len(across[source]),
                                                CALLED[source][1])
                                   for source in sorted(independent)),
                      " and ".join(CALLED[source][0]
                                   for source in sorted(independent))))

        patterns.append({
            "pattern": pattern, "verdict": verdict,
            "seen": book["seen"], "across": across,
            "corroborated_by": sorted(independent),
            "claimed": sorted(book["claimed"]),
            "why": why})

    order = dict((name, place) for place, name in enumerate(VERDICTS))
    patterns.sort(key=lambda one: (order[one["verdict"]], one["pattern"]))
    counts = dict((name, 0) for name in VERDICTS)
    for one in patterns:
        counts[one["verdict"]] += 1
    claimed = sorted(set(word for one in patterns for word in one["claimed"]))

    return {
        "analysed": True, "patterns": patterns, "counts": counts,
        "of": len(observations),
        "why": "%d observation(s), %d pattern(s): %d corroborated, %d "
               "frequent but uncorroborated, %d one-off."
               % (len(observations), len(patterns), counts["corroborated"],
                  counts["frequent_but_uncorroborated"],
                  counts["a_one_off"]),
        "unjudged": [
            "FREQUENCY AND CORROBORATION ARE REPORTED TOGETHER, NEVER "
            "APART. Something seen ten times in one project by one person "
            "is frequent and uncorroborated, and that is the most "
            "dangerous shape here - the count looks like evidence and the "
            "evidence is one situation repeated.",
            "%s" % ("NOVELTY WAS CLAIMED (%s) AND CHANGED NOTHING. It is "
                    "echoed back rather than silently dropped, because "
                    "quietly ignoring an input is how somebody keeps "
                    "sending it." % ", ".join(claimed) if claimed else
                    "NOVELTY WAS NOT CLAIMED. If it had been it would have "
                    "changed nothing - the row says frequency and "
                    "corroboration, NOT NOVELTY."),
            "THERE IS NO 'ENOUGH' NUMBER ANYWHERE. The only two boundaries "
            "are at more than one - once against a repeat, one place "
            "against more than one - and neither is tuned. Every 'enough' "
            "is invented, which is the wall HERON-NAM-TAX-004 refused to "
            "build.",
            "NOTHING WAS EXTRACTED OR PROMOTED. Turning a pattern into a "
            "candidate is HERON-LRN-EXT-003's, and walking one through the "
            "lifecycle gates is HERON-LRN-PRO-004's - where one success is "
            "still not proof.",
        ],
    }


def main(argv):
    print("LEARNING ANALYSIS   frequency and corroboration are two things")
    print("=" * 72)

    answer = analyse([
        {"pattern": "tagging misses flex duct", "project": "Tower A",
         "by": "Ajmal", "workflow": "tag-and-sheet"},
        {"pattern": "tagging misses flex duct", "project": "Tower B",
         "by": "Sara", "workflow": "tag-and-sheet"},
        {"pattern": "tagging misses flex duct", "project": "Tower A",
         "by": "Ajmal", "workflow": "tag-and-sheet"},
        {"pattern": "sheet numbering restarts", "project": "Tower A",
         "by": "Ajmal", "workflow": "sheets"},
        {"pattern": "sheet numbering restarts", "project": "Tower A",
         "by": "Ajmal", "workflow": "sheets"},
        {"pattern": "sheet numbering restarts", "project": "Tower A",
         "by": "Ajmal", "workflow": "sheets"},
        {"pattern": "schedule sorted wrong", "project": "Tower A",
         "by": "Ajmal", "note": "never seen this before, very unusual"},
    ])

    print("\n%s" % answer["why"])
    for one in answer["patterns"]:
        print("\n  %-28s %s" % (one["verdict"], one["pattern"]))
        print("    %s" % one["why"])
        if one["claimed"]:
            print("    claimed: %s  (changed nothing)"
                  % ", ".join(one["claimed"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
