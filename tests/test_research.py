# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-RSH-017
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 9 - Heron says what it does not know, and never fetches.

    python tests/test_research.py

WHAT IT PROVES
  1. NOTHING HERE REACHES THE NETWORK, and the check is the absence of one.
     The same shape tests/test_ground.py uses for R-47, and for a stronger
     reason: a fetched page is text a stranger controls arriving at the moment
     of the question, which is Golden Rule 19 under load. Adding a fetch has to
     fail the suite rather than pass review.

  2. THE VERDICT CEILING IS UNVERIFIED AND THERE IS NO RUNG ABOVE IT. The
     module carries no verdict named verified, correct, true or approved, and
     this check is meant to fail the day somebody adds one. WELL_FORMED means
     "a person could look this up", never "it is right".

  3. A CITATION THAT CANNOT BE LOOKED UP IS CAUGHT, and caught by what it is
     MISSING rather than by a score. "per ISO 19650" names a document and no
     edition and no clause; the report says which of the three are absent,
     because "vague" without that is not actionable.

  4. THE GAP IS DERIVED FROM THE LIBRARIAN'S OWN ANSWER. Certain only when
     every scope returned nothing by name; and where clauses DID come back,
     the report says plainly that whether they answer cannot be established
     (W-8) rather than inventing a floor (R-60) or classifying the question
     (R-62).

WHAT IT DOES NOT PROVE
  That any external answer is correct. Nothing in this file, and nothing in the
  module it tests, can establish that - which is the stage's own point.
"""

from __future__ import print_function

import inspect
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    import heron_research as R
    import heron_retrieve as RETRIEVE

    source = inspect.getsource(R)

    print("1. NOTHING HERE REACHES THE NETWORK")
    for forbidden in ("import requests", "import urllib", "urllib.request",
                      "import socket", "import http", "httplib",
                      "http://", "https://", "urlopen", "api_key", "API_KEY"):
        check(forbidden not in source,
              "no %r anywhere in the module - Heron says what it does not "
              "know and the HOST, which has the model and the network, goes "
              "and finds out (D-01)" % forbidden)
    print()

    print("2. THERE IS NO VERDICT ABOVE UNVERIFIED")
    check("UNVERIFIED" in source, "UNVERIFIED exists and is named")
    # AT THE START OF A LINE, because "VERIFIED =" is a substring of
    # "UNVERIFIED =" and the first version of this check failed on the very
    # constant it exists to protect. Caught by running it.
    for banned in ("VERIFIED", "CORRECT", "TRUE", "APPROVED", "CONFIRMED"):
        check(not re.search(r"^%s\s*=" % banned, source, re.M),
              "there is no %s verdict - this check is meant to fail the day "
              "somebody adds one, because the best this module can reach is "
              "'well-formed AND unread'" % banned)
    check(R.UNVERIFIED.startswith("UNVERIFIED"),
          "and the constant says so in the words a person reads, not just in "
          "the name a programmer reads")
    for banned in ("def repair", "def rewrite", "def correct", "def fix"):
        check(banned not in source,
              "and no %s - R-53: the check flags and never rewrites, because "
              "repairing its own finding turns a wrong answer into an "
              "invisible one" % banned)
    print()

    print("3. A CITATION IS JUDGED BY WHAT IT IS MISSING")
    well = R.check("Ductwork shall be insulated to 25mm per "
                   "ISO 19650-2:2018 clause 5.1.4.")
    check(well.claims[0].verdict == R.WELL_FORMED,
          "a document, an edition and a locator is well-formed")
    check(well.ok, "and the report says every claim could be looked up")

    no_edition = R.check("Ductwork shall be insulated to 25mm per ISO 19650.")
    got = no_edition.claims[0]
    check(got.verdict == R.VAGUE,
          "a standard named with no edition is VAGUE - ISO 19650 is five "
          "parts across several years, so a clause number without an edition "
          "points into whichever copy the reader is holding")
    check(got.cited and "the edition or year" in got.cited.missing,
          "and the report names WHICH part is missing, because 'vague' on its "
          "own is not something a person can act on")

    nothing = R.check("Ductwork shall be insulated to 25mm as per the standard.")
    check(nothing.claims[0].verdict == R.VAGUE
          and "the document" in nothing.claims[0].cited.missing,
          "'per the standard' names no document - this exact phrase is what "
          "gets written under a confident paragraph by something that had no "
          "source at all")

    bare = R.check("Ductwork shall be insulated to 25mm.")
    check(bare.claims[0].verdict == R.UNCITED,
          "a claim citing nothing is UNCITED - docs/05 s8 and R-21 call that a "
          "BUG in a standards answer, not a low-confidence answer")
    check(not bare.ok, "and the report is not ok")

    prose = R.check("This is worth reviewing with the MEP lead.")
    check(prose.claims[0].verdict == R.NOT_A_CLAIM,
          "a sentence asserting nothing is not flagged - flagging it would "
          "teach people to ignore flags")
    print()

    print("4. A REQUIREMENT WITH NO NUMBER IS STILL A CLAIM")
    # THE DEFECT THIS SECTION EXISTS FOR WAS FOUND BY RUNNING THE SEAM, on the
    # afternoon it was written. _is_claim borrowed heron_ground's test, which
    # asks "is there a VALUE here that could be fabricated?" - so a sentence
    # with no number was never checked, and the module's own vague-source list
    # was unreachable for the exact phrase it was written to catch.
    vague_no_number = R.check("Revisions are lettered per the standard.")
    got = vague_no_number.claims[0]
    check(got.verdict == R.VAGUE,
          "'per the standard' with no number in the sentence is still VAGUE - "
          "grounding asks whether a VALUE could be fabricated, and this asks "
          "whether a SOURCE is owed, which is a wider question")
    normative = R.check("Ducts shall be insulated.")
    check(normative.claims[0].verdict == R.UNCITED,
          "and a 'shall' with no source is UNCITED - shall, must and is "
          "required is how ISO, BS, NFPA and QCS each write a requirement, in "
          "English, whatever the subject. That is grammar, not the hand-written "
          "domain word list R-60 forbids")
    prose_limit = R.check("Container names use six fields.")
    check(prose_limit.claims[0].verdict == R.NOT_A_CLAIM,
          "while a bare assertion with no number, no modal and no source "
          "phrase reads as prose and is NOT checked - the recorded limit. "
          "Widening further starts flagging 'let me know if you need more "
          "detail', and a report that flags everything is one nobody reads")
    print()

    print("5. A MEASUREMENT IS NOT A CLAUSE NUMBER")
    ranged = R.check("A fall of 1.5 to 2.5 per cent is acceptable.")
    check(ranged.claims[0].verdict == R.UNCITED,
          "a bare dotted number with no document named is not read as a "
          "locator - heron_graph learned the same lesson from the other side, "
          "and a review had to point it out twice")
    print()

    print("6. THE GAP COMES FROM THE LIBRARIAN, NOT FROM A GUESS")
    empty = [RETRIEVE.Asked("company", skipped="nothing is indexed"),
             RETRIEVE.Asked("project", skipped="no project is identified")]
    sure = R.gap("anything", empty)
    check(sure.certain,
          "every scope returning nothing is a CERTAIN gap - so an outside "
          "answer is not being preferred over Heron's own knowledge, because "
          "Heron has none")
    check("NOTHING IN THE SCOPES ASKED ANSWERS THIS" in sure.sentence(),
          "and it says so in words")

    import heron_search as SEARCH

    answered = RETRIEVE.Asked(
        "company",
        answer=SEARCH.Answer("documents", "c1",
                             candidates=[{"id": "c1"}, {"id": "c2"}],
                             note="2 chunk(s)"))
    unsure = R.gap("anything", [answered])
    check(not unsure.certain,
          "clauses coming back is NOT a certain gap, whatever they say")
    said = unsure.sentence()
    check("W-8" in said and "not established" in said,
          "and the report says whether they ANSWER cannot be established - "
          "the floor that would decide it has to come from a measurement, and "
          "the measurement says it cannot be derived on this backend")
    check("R-62" in said,
          "and it puts that judgement on the reader rather than making it - "
          "no classifier in brain/")
    print()

    print("7. THE BRIEF CARRIES THE CONTRACT AND THE WAY OUT")
    text = R.brief(sure, ["company"])
    for wanted in ("THE DOCUMENT", "THE EDITION OR YEAR",
                   "THE CLAUSE, SECTION OR TABLE NUMBER"):
        check(wanted in text, "the brief asks for %s" % wanted.lower())
    check("heron_ingest.py" in text,
          "and it names the one command that turns an external answer into a "
          "checkable one - ingest the source and the next asking is answered "
          "from inside, with a chunk id")
    check("UNVERIFIED" in text,
          "and it says up front what Heron will not do with the answer")
    print()

    print("8. Nothing here decides what the user meant")
    for banned in ("def classify", "def intent", "def guess", "def decide"):
        check(banned not in source,
              "no %s - R-62 puts that in the host, which is the only party "
              "that has read the question and the clauses together" % banned)
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - Heron names what it does not know, states what an outside")
    print("answer must carry, and checks the shape of what comes back without")
    print("ever fetching anything or calling any of it true.")
    print()
    print("It proves NOTHING about whether an external answer is correct.")
    print("Heron has not read the source and cannot. The only route from")
    print("UNVERIFIED to grounded is ingesting the document it cites, which")
    print("is one command and is in the brief every time.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
