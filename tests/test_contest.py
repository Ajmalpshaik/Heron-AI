# Heron-Agent:  HERON-RAG-RNK-006
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Stage 0b - retrieval says how contested its answer was.

    python tests/test_contest.py

WHAT IT PROVES
  1. THE UNIT IS DERIVED, NOT TYPED. One rank of fusion is computed from
     RRF_K, and every quality nudge a fragment can be OFFERED with fits
     inside one - which is the arithmetic the words "a coin toss" rest on.
  2. A DEAD HEAT IS CALLED A DEAD HEAT. Two fragments the routes rank
     (words 1, nearness 2) and (words 2, nearness 1) score identically -
     fusion is symmetric - and the report says so instead of presenting the
     alphabetical winner as the answer.
  3. A CLEAR WINNER IS NOT CALLED A COIN TOSS. The same report, opposite
     direction, on the same measurement.
  4. THE SMALL-POOL CAVEAT SURVIVES. While the library is smaller than the
     pool, "both agree" is true of everything, and the report still says the
     words brain/retrieval-history.md recorded it in.
  5. NOTHING IS DROPPED AND NOTHING IS REFUSED. Stage 0b stops at reporting.
     R-56 and R-58 need a floor, and no floor has been derived - so this
     asserts an ABSENCE, and it is meant to fail the day somebody adds
     dropping without deriving the floor first.

WHY THE ARITHMETIC IS ASSERTED AND NOT THE INTENTION
----------------------------------------------------
The quality nudge in heron_retrieve.py shipped EIGHT TIMES TOO BIG once. Its
docstring said it could not overturn a better match; the number said it could.
The test that caught it compared values instead of reading the sentence, and
this file is written the same way for the same reason.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def candidate(R, fid, fused, quality=0.0, words=None, near=None,
              words_score=None, near_score=None):
    """A Candidate with its numbers set, so the arithmetic can be checked.

    Built rather than retrieved on purpose: a shortlist that comes out of the
    real library changes every time a fragment is added, and an assertion
    about it measures the library instead of the report. docs/27 retired two
    assertions that were written the other way.
    """
    got = R.Candidate(fid, {"capability": fid, "status": "DRAFT"})
    got.fused = fused
    got.quality = quality
    got.keyword_rank = words
    got.vector_rank = near
    got.keyword_score = words_score
    got.vector_score = near_score
    return got


def main():
    import heron_retrieve as R

    print("1. One rank of fusion is derived from RRF_K, never typed")
    expected = 1.0 / (R.RRF_K + 1) - 1.0 / (R.RRF_K + 2)
    check(abs(R.ONE_RANK - expected) < 1e-12,
          "ONE_RANK is 1/(K+1) - 1/(K+2) at K=%d, which is %.6f"
          % (R.RRF_K, R.ONE_RANK))

    offerable = [R.QUALITY[s] for s in R.OFFERABLE if s in R.QUALITY]
    span = max(offerable) - min(offerable)
    check(span < R.ONE_RANK,
          "every status a fragment can be OFFERED at spans %.6f, inside one "
          "rank of %.6f - so a gap under one rank COULD have been set by "
          "status alone, which is what the report says" % (span, R.ONE_RANK))
    print()

    print("2. A dead heat is reported as a dead heat")
    # Fusion is symmetric: (words 1, nearness 2) and (words 2, nearness 1)
    # score identically. heron_retrieve's own comment records that the very
    # first run of that file settled this pair by ALPHABETICAL ORDER.
    one = 1.0 / (R.RRF_K + 1)
    two = 1.0 / (R.RRF_K + 2)
    tied = [candidate(R, "FRG-AAA-001", one + two, words=1, near=2),
            candidate(R, "FRG-ZZZ-001", two + one, words=2, near=1)]
    heat = R.Contest(tied, eligible_count=200, pool=20, breadth=40)
    check(abs(heat.top_gap) < 1.0,
          "the two are %.2f of a rank apart, so the gap is under one"
          % heat.top_gap)
    check("COIN TOSS" in heat.sentence(),
          "and the sentence says COIN TOSS rather than naming a winner")
    check(heat.agreed == 2,
          "both were found by both routes, and that is reported as 2 of 2")
    print()

    print("3. A clear winner is not reported as a coin toss")
    clear = [candidate(R, "FRG-AAA-001", 2.0 * one, words=1, near=1),
             candidate(R, "FRG-BBB-002", 1.0 / (R.RRF_K + 9), words=9),
             candidate(R, "FRG-CCC-003", 1.0 / (R.RRF_K + 18), near=18)]
    won = R.Contest(clear, eligible_count=200, pool=20, breadth=40)
    check(won.top_gap > 1.0,
          "the winner is %.1f ranks clear, which is more than one" % won.top_gap)
    check("COIN TOSS" not in won.sentence(),
          "so the sentence does not say COIN TOSS")
    check("clear of the runner-up" in won.sentence(),
          "it says how many ranks clear instead")
    check(won.spread > won.top_gap,
          "and the shortlist spans %.1f ranks, wider than the top gap of %.1f "
          "- first-to-last and first-to-second are different questions"
          % (won.spread, won.top_gap))
    check(won.agreed == 1,
          "only one of the three was found by both routes")
    print()

    print("4. The small-pool caveat survives, in the words it was recorded in")
    small = R.Contest(clear, eligible_count=7, pool=20, breadth=7)
    check(not small.pool_is_evidence,
          "7 eligible against a pool of 20 is not evidence of agreement")
    check("means nothing here yet" in small.sentence(),
          "and the sentence still carries the phrase retrieval-history used")
    big = R.Contest(clear, eligible_count=200, pool=20, breadth=40)
    check(big.pool_is_evidence and "means nothing here yet" not in big.sentence(),
          "past the pool the caveat goes away, because then it is evidence")
    print()

    print("5. A words route that matched everything says so")
    everything = R.Contest(clear, eligible_count=200, pool=20, breadth=200)
    check(everything.words_selected_nothing,
          "matching 200 of 200 eligible means the route selected nothing")
    check("ranked the library rather than selecting from it"
          in everything.sentence(),
          "and the sentence says that, because a rank out of everything is "
          "not a claim on the sentence")
    check(not big.words_selected_nothing,
          "matching 40 of 200 is a selection, and is not flagged")
    print()

    print("6. NOTHING is dropped and NOTHING is refused - Stage 0b stops here")
    # R-56 drops a candidate with no claim; R-58 refuses a question nothing
    # covers. Both need a floor, R-60 says a floor comes from a measurement,
    # and the measurement on the lexical backend does not separate a BIM
    # question from a question about cats - twelve questions, every column
    # overlapping, recorded in brain/retrieval-history.md.
    #
    # THIS CHECK IS MEANT TO FAIL THE DAY SOMEBODY ADDS DROPPING. That is not
    # an obstacle, it is the point: changing it should take a deliberate hand
    # and a derived floor, not a quiet edit.
    check(R.Contest(clear, 200, 20, 40).count == len(clear),
          "the contest counts the shortlist it was given and returns it whole")
    check(not hasattr(R.Contest(clear, 200, 20, 40), "dropped"),
          "there is no `dropped` count, because nothing drops anything yet")
    check(not any(name.startswith("FLOOR") or name.endswith("_FLOOR")
                  for name in dir(R)),
          "and heron_retrieve declares no floor constant of any kind")
    print()

    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a shortlist now says how contested it was, in a unit the")
    print("file already lives by, and the one comparison it makes is against")
    print("the width of the quality nudge rather than against a number")
    print("somebody chose.")
    print()
    print("It proves nothing about whether the shortlist is any GOOD. Saying")
    print("how contested an answer was and having a better answer are")
    print("different claims, and only the first one is tested here.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
