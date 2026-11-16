# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-MAV-017
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Model Availability & Fallback - the degraded mark, and nothing else.

    python brain/heron_availability.py     a worked example of each outcome

WHAT THE REGISTER ASKS FOR (docs/28, rewritten 2026-11-15, PROPOSALS F20)
--------------------------------------------------------------------------
"Whether a provider is reachable, and the fallback when it is not, are the
host's, which makes every call (D-65). It keeps the one clause D-65 leaves
Heron: it reads that a result came from a fallback and marks the result as
degraded, so it never counts as evidence toward promotion."

So this module answers two questions and asks none. mark() takes a result and
the host's word on whether a fallback produced it, and puts `degraded` on it.
counts_as_evidence() is the one line every caller asks rather than deciding
for itself.

WHY A DEGRADED RESULT IS NOT EVIDENCE (D-30, D-65)
---------------------------------------------------
D-30 promotes a fragment on ONE RECORDED PROOF against a real, named model -
a positive case, a negative case, a second route where one exists, and a
staleness fingerprint - not on a count of runs. D-65 adds the half this
module keeps: a result produced after a provider fell over is missing the one
thing a proof is for, the guarantee that what ran is what was meant to run.
For promotion purposes a degraded run is a run that did not happen.

A QUESTION NOBODY ANSWERED IS NOT A NO
---------------------------------------
Only `fell_back=False` - the host saying in so many words that the first
choice answered - leaves a result undegraded. Anything else, the argument
left out included, is marked degraded: a result whose origin nobody stated
cannot be the proof D-30 asks for, and reading silence as "first choice" is
the optimistic guess this module exists to refuse.

WHAT IS NOT HERE ANY MORE, AND WHERE IT WENT (row 5b-1747, 2026-11-16)
------------------------------------------------------------------------
Until 2026-11-16 this module also judged whether a provider was reachable -
judge(), sorting a probe into unreachable, auth, slow, too small or probe
failed - and chose the fallback itself, resolve(). D-65 gives both to the
host, which makes the calls and is the only thing that can choose between
them. resolve() is gone: nothing outside the tests called it. judge() moved,
unchanged, to brain/heron_provider.py beside the one probe it reads; whether
that adapter should judge a provider at all is recorded, not settled here.
"""

import sys

# The refusal mark() gives when it is handed something that is not a result.
# A refusal is not marked: it carries no `adapter`, so counts_as_evidence()
# already says no, and a refusal that came back with a mark on it would read
# as an answer somebody could weigh.
NOT_A_RESULT = "NOT_A_RESULT"


def mark(result, fell_back=None):
    """
    A copy of `result` with `degraded` set from the host's word - or a refusal.

    `fell_back` is what the host said: True when a fallback produced the
    result, False when the first choice did. Anything else, None included, is
    the host saying nothing, and that is marked degraded too - see the module
    docstring. The result passed in is never changed.
    """
    if not isinstance(result, dict) or "adapter" not in result \
            or result.get("refused"):
        return {"refused": NOT_A_RESULT,
                "why": "only a result can be marked: this has no `adapter`, "
                       "or it is a refusal. There is nothing here to weigh, "
                       "so there is nothing to mark."}
    marked = dict(result)
    marked["degraded"] = fell_back is not False
    return marked


def counts_as_evidence(result):
    """
    May this result be counted toward promotion (D-30)?

    One line, in one place, so that no caller decides it privately. A refusal
    is not evidence either - there is no result to weigh.
    """
    # `is False`, not `not ...`. A result that omits the marker, or carries
    # None, used to count as evidence - so an older or malformed provider
    # result could become proof without ever saying whether a fallback
    # happened. Only an explicit first-choice outcome counts.
    return bool(result) and "adapter" in result \
        and result.get("degraded") is False


def main(argv):
    print("MODEL AVAILABILITY   the degraded mark, and what it means")
    print("=" * 67)
    print("reachability and the fallback are the host's (D-65)\n")

    answered = {"adapter": "the-host", "text": "three ducts"}
    for label, fell_back in (("first choice answered", False),
                             ("a fallback answered", True),
                             ("the host said nothing", None)):
        answer = mark(answered, fell_back)
        print("  %-24s degraded=%-5s evidence=%s"
              % (label, answer.get("degraded"), counts_as_evidence(answer)))

    refusal = mark({"refused": "NOTHING_AVAILABLE"}, False)
    print("  %-24s %-14s evidence=%s"
          % ("a refusal", refusal["refused"], counts_as_evidence(refusal)))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
