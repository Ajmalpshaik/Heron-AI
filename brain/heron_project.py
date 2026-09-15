# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-STD-PRJ-009
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Project standard - it outranks the company default, and it SAYS SO.

    python brain/heron_project.py

WHAT IT IS FOR (docs/28, HERON-STD-PRJ-009)
--------------------------------------------
"This project's own rules - OUTRANKS THE COMPANY DEFAULT, and says so."
T2, risk ANALYZE. The last four words are the whole design.

THE ORDER EXISTS ALREADY AND NOBODY WAS ALLOWED TO APPLY IT
-------------------------------------------------------------
docs/20 s2 sets out seven rungs, project at the top, and then adds the
caveat that carries this agent:

    "overrides" must not mean "silently replaces". When project
    knowledge overrides company knowledge, the answer should say so -
    "using this project's duct sizing table, which differs from the
    company default". A silent override is how a modeller ends up
    confidently applying the wrong standard, having never been told a
    choice was made.

HERON-RAG-CNF-015 holds that order as `HIERARCHY` and marks it REPORTED,
NEVER APPLIED - it sorts the printed lines and does nothing else,
because applying it inside a retrieval answer would be the silent
replacement docs/20 warns about. That was right, and it left the ladder
with nobody standing on it.

This agent applies it out loud. The overridden clause travels in the
answer, with its scope, its document and its locator, so a reader can go
and look at the standard that did not win. Golden Rule 14: never
silently discard.

THE ORDER IS BORROWED, NOT RETYPED
------------------------------------
`HIERARCHY` is bound by identity from heron_conflict. One ladder, and if
docs/20's rungs ever change there is one line to follow.

IT DOES NOT DECIDE WHAT TWO CLAUSES ARE ABOUT
-----------------------------------------------
Whether "insulate to 30mm" and "insulate to 40mm" are two answers to one
question is language, and D-34 says Heron builds nothing for that.
heron_conflict already did that part, by unit and by number, and this
agent works over its Disagreement records rather than over text. Hand it
nothing and it rules on nothing.

TWO CASES THE LADDER CANNOT SETTLE, AND IT ASKS RATHER THAN PICKS
-------------------------------------------------------------------
    TWO SOURCES IN ONE SCOPE. Two project documents giving different
    numbers - an old revision and a new one, a specification and an
    addendum. The ladder has one rung for both and says nothing about
    which of them governs. This is common on a real job and it is the
    case a ranking would have quietly resolved.

    A SCOPE THE LADDER DOES NOT NAME. docs/20's fourth rung is
    "Approved Community Knowledge" and this repository has no community
    scope, so anything arriving from outside the six has no place on the
    ladder rather than a place at the bottom.

Neither is guessed at. Both come back in `unordered` with a scoped
question in `asks` - which is what makes this row T2 rather than T1: the
ladder is a table, and everything the table is silent about is a
judgement somebody else makes.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_conflict as CNF  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/20 s2's order, through HERON-RAG-CNF-015 rather than retyped here.
# That agent holds it REPORTED, NEVER APPLIED; this one is where it is
# applied, and the difference between the two is the sentence docs/20 asks
# for.
HIERARCHY = CNF.HIERARCHY

# The sentence docs/20 s2 writes out in full, kept as the shape of the
# answer rather than as prose in a comment.
SAYS_SO = "using %s (%s), which differs from %s (%s)"


def _rung(scope):
    """Where a scope sits, or None when the ladder does not name it."""
    try:
        return HIERARCHY.index(scope)
    except ValueError:
        return None


def _card(value):
    """One clause, with enough to go and read it."""
    return {"scope": value.scope, "label": value.label,
            "document": value.document, "locator": value.locator,
            "value": value.value, "unit": value.unit}


def governs(found):
    """
    {ruled, settled, unordered, asks} - or a refusal. Nothing is dropped
    and nothing is rewritten.
    """
    if found is None:
        return {"ruled": False, "refused": "NOTHING_TO_RULE",
                "why": "no disagreements were handed in. Note that "
                       "heron_conflict returns an empty list both when the "
                       "sources agree and when nobody was asked, so this is "
                       "not a report that they agree."}

    found = list(found)
    if not found:
        return {"ruled": False, "refused": "NOTHING_TO_RULE",
                "why": "the list of disagreements is empty. heron_conflict "
                       "returns that both when every source says the same "
                       "thing and when no scope was asked - and those are "
                       "different, so nothing is claimed here."}

    settled, unordered, asks = [], [], []
    for one in found:
        values = getattr(one, "values", None)
        unit = getattr(one, "unit", None)
        if not values or not unit:
            return {"ruled": False, "refused": "NOT_A_DISAGREEMENT",
                    "why": "%r is not a disagreement. heron_conflict's "
                           "records carry a `unit` - the dimension - and the "
                           "`values` that differ." % (one,)}

        # By SOURCE, which is a document and not a scope - R-24, and
        # heron_conflict already keys on it for exactly this reason.
        by_source = {}
        for value in values:
            by_source.setdefault(value.source, []).append(value)

        ranked = []
        unplaced = []
        for source in sorted(by_source, key=lambda s: str(s)):
            scope = by_source[source][0].scope
            rung = _rung(scope)
            if rung is None:
                unplaced.append(by_source[source][0])
            else:
                ranked.append((rung, source, by_source[source][0]))

        if unplaced:
            unordered.append({
                "unit": unit,
                "clauses": [_card(v) for v in values],
                "why": "%s %s not on the ladder at all. docs/20 s2's fourth "
                       "rung is community knowledge and this repository has "
                       "no community scope, so a source from outside the six "
                       "has no place rather than a place at the bottom."
                       % (", ".join(sorted(set(v.scope for v in unplaced))),
                          "is" if len(set(v.scope for v in unplaced)) == 1
                          else "are")})
            asks.append({
                "unit": unit,
                "question": "which of these governs? The ladder does not "
                            "reach %s."
                            % ", ".join(sorted(set(v.scope
                                                   for v in unplaced))),
                "clauses": [_card(v) for v in values]})
            continue

        ranked.sort(key=lambda row: (row[0], str(row[1])))
        top = ranked[0][0]
        tied = [row for row in ranked if row[0] == top]

        if len(tied) > 1:
            # THE CASE A RANKING WOULD HAVE SETTLED QUIETLY. Two documents
            # in one scope - a specification and its addendum, an old
            # revision and a new one. The ladder has one rung for both.
            unordered.append({
                "unit": unit,
                "clauses": [_card(v) for v in values],
                "why": "%d sources are all in the %s scope, and the ladder "
                       "has one rung for them. Which revision or which "
                       "document governs is not something docs/20 s2 says."
                       % (len(tied), tied[0][2].scope)})
            asks.append({
                "unit": unit,
                "question": "two %s documents disagree - which one governs? "
                            "An addendum, a later revision or a specific "
                            "clause can outrank another inside one scope, "
                            "and none of that is written down."
                            % tied[0][2].scope,
                "clauses": [_card(v) for v in values]})
            continue

        winner = tied[0][2]
        losers = [row[2] for row in ranked[1:]]
        settled.append({
            "unit": unit,
            "governs": _card(winner),
            "overridden": [_card(v) for v in losers],
            "says": SAYS_SO % (winner.document or winner.label, winner.scope,
                               losers[0].document or losers[0].label,
                               losers[0].scope),
            "why": "%s outranks %s on docs/20 s2's ladder. Both clauses are "
                   "in this answer - docs/20 s2 again: overrides must not "
                   "mean silently replaces."
                   % (winner.scope,
                      ", ".join(sorted(set(v.scope for v in losers))))})

    return {
        "ruled": True,
        "of": len(found),
        "settled": settled,
        "unordered": unordered,
        "asks": asks,
        "why": "%d disagreement%s: %d the ladder settles, %d it does not."
               % (len(found), "" if len(found) == 1 else "s",
                  len(settled), len(unordered)),
        "unjudged": [
            "THE LADDER IS docs/20 s2's, BOUND FROM HERON-RAG-CNF-015 RATHER "
            "THAN RETYPED. That agent holds it reported and never applied, "
            "because applying it inside a retrieval answer is the silent "
            "replacement docs/20 warns about. This is where it is applied, "
            "and the overridden clause travels with the answer.",
            ("NOTHING WAS DROPPED. %d overridden clause%s in the answer with "
             "%s scope, document and locator, so a reader can go and look at "
             "the standard that did not win (Golden Rule 14)."
             % (sum(len(row["overridden"]) for row in settled),
                "" if sum(len(row["overridden"]) for row in settled) == 1
                else "s",
                "its" if sum(len(row["overridden"]) for row in settled) == 1
                else "their")
             if settled else
             "nothing was overridden, so nothing had to be kept visible."),
            "WHAT THE CLAUSES ARE ABOUT WAS NOT DECIDED HERE. That two "
            "numbers answer one question is heron_conflict's finding, by "
            "unit and by number. Deciding it from the words would be "
            "language, and D-34 says Heron builds nothing for that.",
            ("%d disagreement%s the ladder cannot settle, each with a "
             "question rather than a pick - two sources in one scope, or a "
             "scope docs/20 s2 does not name."
             % (len(unordered), "" if len(unordered) == 1 else "s")
             if unordered else
             "every disagreement fell on two different rungs, so the ladder "
             "had something to say about all of them."),
            "WHETHER THE WINNING CLAUSE IS RIGHT. The ladder says which "
            "source governs, not whether that source is correct. A project "
            "specification with a typo in it still outranks the company "
            "default, and this agent will say so.",
        ],
    }


def main(argv):
    print("PROJECT STANDARD   it outranks the company default, and says so")
    print("=" * 72)
    print("\nthe ladder, from heron_conflict (docs/20 s2)")
    for rung, scope in enumerate(HIERARCHY, 1):
        print("  %d. %s" % (rung, scope))

    class Say(object):
        """A stand-in for one of heron_conflict's values, for the demo."""

        def __init__(self, scope, document, value, unit="mm",
                     locator="3.1", label=None):
            self.scope = scope
            self.label = label or scope
            self.document = document
            self.value = value
            self.unit = unit
            self.locator = locator

        @property
        def source(self):
            return (self.label, self.document)

    class Found(object):
        def __init__(self, unit, values):
            self.unit = unit
            self.values = values

    answer = governs([
        Found("length", [
            Say("project", "Tower B Project Specification", "40"),
            Say("company", "Acme Engineering BIM Standard 2026", "30")]),
        Found("length", [
            Say("project", "Tower B Specification Rev A", "40"),
            Say("project", "Tower B Addendum 2", "45")]),
        Found("gradient", [
            Say("community", "A forum post", "1in100", unit="1in100"),
            Say("company", "Acme Engineering BIM Standard 2026", "1in80",
                unit="1in80")])])

    print("\n%s" % answer["why"])
    for row in answer["settled"]:
        print("\n  SETTLED  %s" % row["says"])
        print("           %s" % row["why"])
        for card in row["overridden"]:
            print("           kept: %s %s, %s %s"
                  % (card["value"], card["unit"], card["document"],
                     card["locator"]))
    for row in answer["unordered"]:
        print("\n  UNORDERED %s" % row["why"])
    for row in answer["asks"]:
        print("  ASKS      %s" % row["question"])

    print("\nrefused")
    for these in (None, [], ["a string"]):
        bad = governs(these)
        print("  %-22s %s" % (bad["refused"], bad["why"][:44]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
