#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Failure Analysis Agent - never blind-retries. Runs without Revit.

The dangerous mistake this exists to prevent has a specific shape, and it is
not "Heron reported the wrong error". It is:

    The move worked. The answer was lost on the way back. Heron decided that
    looked like a failure, tried again, and moved the same ducts 200 mm twice.

Nothing in the model says that happened. The ducts are simply 400 mm out, and
the person who finds out is whoever measures them later.

So the tests below are mostly not about classification being pretty. They are
one property, asserted from several directions:

    NO failure of an operation that can change the model may EVER come back
    saying "safe to retry" unless it is PROVEN the request never ran.

    python tests/test_failure_analysis.py
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_failure as fa                                   # noqa: E402
from heron_failure import analyse, explain                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def refusal(code, message=None):
    return {"ok": False, "error": code, "message": message}


def main():
    print("Success is not a failure")
    check(analyse({"ok": True, "moved": 4}, writes=True) is None,
          "a successful reply analyses to nothing at all")

    print()
    print("THE PROPERTY: a write is never silently retried unless it provably never ran")
    offenders = []
    for code in fa._KNOWN:
        f = analyse(refusal(code), writes=True)
        if f.may_retry and f.outcome != fa.NEVER_RAN:
            offenders.append("%s (%s)" % (code, f.outcome))
    check(not offenders,
          "no known code is retryable without NEVER_RAN%s"
          % ("" if not offenders else " - offenders: " + ", ".join(offenders)))

    print()
    print("The lost answer - the case the whole file exists for")
    lost = analyse(refusal("unknown_outcome"), writes=True)
    check(lost.outcome == fa.UNKNOWN, "a lost answer is UNKNOWN, not a failure")
    check(lost.may_retry is False, "and it is NEVER retried automatically")
    check(lost.next_step == fa.LOOK_AT_THE_MODEL, "a person must look at the model")
    check(lost.touched_the_model is None,
          "'might it have changed?' answers None - genuinely unknown, not False")

    print()
    print("Nothing came back at all")
    check(analyse(None, writes=True).outcome == fa.UNKNOWN,
          "no reply to a WRITE is unknown - it cannot be assumed not to have happened")
    check(analyse(None, writes=True).may_retry is False,
          "and so it is not retried")
    check(analyse(None, writes=False).may_retry is True,
          "no reply to a READ is safe to ask again - it costs nothing")

    print()
    print("Still running is not failure, and asking again is the wrong move")
    running = analyse(refusal("still_running"), writes=True)
    check(running.outcome == fa.RUNNING, "Revit took it and is working")
    check(running.next_step == fa.WAIT, "the answer is to wait, not to retry")
    check(running.may_retry is False, "asking again would run the same job twice")

    print()
    print("Fail closed on what it has never seen")
    novel = analyse(refusal("some_future_error"), writes=True)
    check(novel.outcome == fa.UNKNOWN,
          "an unrecognised WRITE failure is treated as unknown, not as retryable")
    check(novel.may_retry is False, "so it is not retried")

    novel_read = analyse(refusal("some_future_error"), writes=False)
    check(novel_read.outcome == fa.REFUSED,
          "the same code on a READ is only a refusal - no model to endanger")

    print()
    print("Never ran means never ran - these are the only free retries")
    for code in ("not_ready", "raise_failed"):
        f = analyse(refusal(code), writes=True)
        check(f.outcome == fa.NEVER_RAN and f.may_retry,
              "%s: proven not to have reached the model, so retrying is free" % code)

    print()
    print("Refusals did not touch anything, but still need a person to act")
    for code, step in (("write_disabled", fa.FIX_FIRST),
                       ("stopped", fa.FIX_FIRST),
                       ("revit_busy", fa.FIX_FIRST),
                       ("preview_expired", fa.START_OVER),
                       ("model_moved_on", fa.START_OVER)):
        f = analyse(refusal(code), writes=True)
        check(f.touched_the_model is False and f.next_step == step,
              "%s: nothing happened to the model, next step is %s" % (code, step))

    print()
    print("Golden Rule 20 survives the classification")
    closed = analyse(refusal("document_closed"), writes=True)
    check(closed.next_step == fa.STOP,
          "the approved model being CLOSED is a stop, never a retry elsewhere")
    front = analyse(refusal("document_not_in_front"), writes=True)
    check(front.next_step == fa.FIX_FIRST and not front.may_retry,
          "clicking away is fixable by the user, but never auto-retried")

    print()
    print("A rolled-back failure left the model alone")
    rolled = analyse(refusal("move_failed"), writes=True)
    check(rolled.outcome == fa.ROLLED_BACK, "it ran and was rolled back")
    check(rolled.touched_the_model is False, "so the model is as it was")
    check(rolled.may_retry is False,
          "still not automatic - it failed for a reason nobody has addressed yet")

    print()
    print("Two halves on different versions is a stop, not a retry loop")
    check(analyse(refusal("unknown_op"), writes=False).next_step == fa.STOP,
          "an operation the add-in does not know will never start working on retry")

    print()
    print("What the user is told")
    said = explain(analyse(refusal("unknown_outcome", "The answer was lost."), writes=True))
    check("The answer was lost." in said,
          "it keeps the far side's own words rather than reinventing them")
    check("twice" in said.lower(),
          "and adds the part the far side cannot know - that repeating could do it twice")
    check(explain(None) is None, "success explains to nothing")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - never blind-retries, and fails closed on what it has not met.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
