# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-TOK-015
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The budget - only measured numbers, and background yields first.

    python tests/test_budget.py

WHAT IT PROVES
  1. A SPEND WITH NO SOURCE IS REFUSED. Heron has no tokeniser (D-58), so a
     number nobody measured must never reach the ledger - it would be stated
     as a fact and used to refuse a modeller.

  2. AN ESTIMATE IS JUDGED AND NEVER RECORDED. It may refuse a call that would
     obviously overrun, and the refusal says whose estimate it was.

  3. UNITS ARE MATCHED, NEVER CONVERTED, in both directions - the estimate and
     the recorded spend. The conversion rate is a provider's price list, and a
     price list in here is out of date the week it is written.

  4. THE THREE POSTURES MOVE AT THE RIGHT PLACES, and STOPPED refuses rather
     than warns.

  5. BACKGROUND YIELDS BEFORE THE PERSON'S WORK DOES: it stops while the
     session is still merely tight.

  6. THE BACKGROUND REFUSAL DOES NOT CLAIM THE BUDGET IS SPENT when the same
     sentence prints what is left of it.

  7. NO BUDGET SET MEANS NOTHING IS ENFORCED, and it says so rather than
     inventing a limit.

  8. THE METER SHOWS ONLY WHAT WAS MEASURED.

  9. A BUDGET WITH NO UNIT, A NEGATIVE ONE, OR AN UNKNOWN SCOPE IS REFUSED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_budget as BUDGET                                 # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def raises(call, kind=ValueError):
    try:
        call()
    except kind:
        return True
    except Exception:                                        # noqa: BLE001
        return False
    return False


def main():
    print("1. A number nobody measured never reaches the ledger")
    book = BUDGET.Budget()
    book.set_budget(BUDGET.SESSION, 100, "calls")
    check(raises(lambda: book.record(BUDGET.SESSION, 5, "calls", "")),
          "a spend with no source is refused")
    check(raises(lambda: book.record(BUDGET.SESSION, 5, "calls", None)),
          "and None is not a source either")
    book.record(BUDGET.SESSION, 5, "calls", reported_by="the adapter")
    check(book.ledger == [(BUDGET.SESSION, 5.0, "calls", "the adapter")],
          "a reported spend is recorded with the source that reported it")

    print()
    print("1b. A reported number that is not a number, or is negative")
    for bad, what in ((-5, "a negative spend"), (float("nan"), "a NaN spend"),
                      (float("inf"), "an infinite spend"),
                      ("lots", "a spend that is not a number"),
                      (None, "a spend of None")):
        check(raises(lambda: book.record(BUDGET.SESSION, bad, "calls",
                                         "the adapter")),
              "%s is refused" % what)
    check(book.remaining(BUDGET.SESSION) == 95,
          "and none of them moved the budget")
    check(len(book.ledger) == 1, "nor reached the ledger")

    print()
    print("1c. A limit is a number too")
    for bad in (float("nan"), float("inf"), "plenty", None):
        check(raises(lambda: book.set_budget(BUDGET.SESSION, bad, "calls")),
              "a budget of %r is refused" % (bad,))
    check(book.remaining(BUDGET.SESSION) == 95,
          "and the budget that was already set is untouched")

    print()
    print("2. An estimate is judged as an estimate")
    small = BUDGET.Budget()
    small.set_budget(BUDGET.REQUEST, 10, "calls")
    answer = small.may_spend(BUDGET.REQUEST, estimate=12, unit="calls")
    check(not answer["allowed"], "an estimate that would overrun refuses")
    check("ESTIMATE" in answer["why"] and "not a count Heron made"
          in answer["why"], "and the refusal says whose number it was")
    check(small.ledger == [], "the estimate was not recorded")
    check(small.may_spend(BUDGET.REQUEST, estimate=3,
                          unit="calls")["allowed"],
          "an estimate that fits is allowed")

    for bad in (float("nan"), float("inf"), -5, "some"):
        answer = small.may_spend(BUDGET.REQUEST, estimate=bad, unit="calls")
        check(not answer["allowed"],
              "an estimate of %r is refused, not weighed" % (bad,))

    print()
    print("2b. A unit is required whether or not a budget exists yet")
    fresh = BUDGET.Budget()
    check(raises(lambda: fresh.record(BUDGET.SESSION, 5, None, "adapter")),
          "a spend with no unit is refused before any budget is set")
    fresh.record(BUDGET.SESSION, 5, "calls", "adapter")
    check(raises(lambda: fresh.record(BUDGET.SESSION, 5, "usd", "adapter")),
          "and a second spend in another unit is refused too")

    check(raises(lambda: fresh.set_budget(BUDGET.SESSION, 10, "usd")),
          "and a budget in another unit cannot rename what was already spent")
    check(fresh.meter()[0]["spent"] == 5
          and fresh.meter()[0]["limit"] is None,
          "the meter shows measured spend even with no limit set")

    print()
    print("3. Units are matched, never converted")
    answer = small.may_spend(BUDGET.REQUEST, estimate=1, unit="usd")
    check(answer.get("refused") == "UNIT_MISMATCH",
          "an estimate in another unit is refused")
    check(raises(lambda: small.record(BUDGET.REQUEST, 1, "usd", "adapter")),
          "and so is a recorded spend in another unit")

    print()
    print("4, 5 and 6. Postures, and who yields first")
    book = BUDGET.Budget()
    book.set_budget(BUDGET.SESSION, 100, "calls")
    book.set_budget(BUDGET.BACKGROUND, 20, "calls")
    check(book.posture(BUDGET.SESSION) == BUDGET.NORMAL, "empty is NORMAL")
    book.record(BUDGET.SESSION, 76, "calls", "adapter")
    check(book.posture(BUDGET.SESSION) == BUDGET.TIGHT,
          "past three quarters the session is TIGHT, and still allowed")
    check(book.may_spend(BUDGET.SESSION)["allowed"],
          "TIGHT is a signal to ask for cheaper thinking, not a refusal")
    book.record(BUDGET.BACKGROUND, 15, "calls", "adapter")
    check(book.posture(BUDGET.BACKGROUND) == BUDGET.STOPPED,
          "background is STOPPED at the same fraction that leaves the "
          "session merely TIGHT")
    stopped = book.may_spend(BUDGET.BACKGROUND)
    check(not stopped["allowed"] and stopped["refused"] == "BUDGET_EXCEEDED",
          "and STOPPED refuses rather than warns")
    check("stops at 75%" in stopped["why"] and "spent -" not in stopped["why"],
          "the refusal does not call a budget spent while printing what is "
          "left of it")
    check("Nothing has been sent" in stopped["why"],
          "and it answers the question every refusal is asked")

    book.record(BUDGET.SESSION, 24, "calls", "adapter")
    check(book.posture(BUDGET.SESSION) == BUDGET.STOPPED,
          "a session at its limit is STOPPED")
    check("is spent" in book.may_spend(BUDGET.SESSION)["why"],
          "and that one IS spent, so it says so")

    print()
    print("6b. An estimate is judged against the ceiling its scope stops at")
    yielding = BUDGET.Budget()
    yielding.set_budget(BUDGET.BACKGROUND, 100, "calls")
    yielding.record(BUDGET.BACKGROUND, 70, "calls", "adapter")
    check(yielding.posture(BUDGET.BACKGROUND) == BUDGET.NORMAL,
          "at 70 of 100 background is still NORMAL")
    answer = yielding.may_spend(BUDGET.BACKGROUND, estimate=20, unit="calls")
    check(not answer["allowed"],
          "and a 20-call estimate is refused - it would eat the quarter "
          "reserved for the person's work before the posture caught up")
    check("ceiling it stops at" in answer["why"],
          "the refusal names the ceiling rather than the limit")
    check(yielding.may_spend(BUDGET.BACKGROUND, estimate=4,
                             unit="calls")["allowed"],
          "an estimate that fits under the ceiling is still allowed")

    print()
    print("7. No budget set is not a budget of zero")
    empty = BUDGET.Budget()
    answer = empty.may_spend(BUDGET.SESSION)
    check(answer["allowed"] and "no budget is set" in answer["why"],
          "with nothing set, nothing is enforced and it says so")
    check("may invent" in answer["why"],
          "and it says why it will not pick a limit itself")

    print()
    print("8. The meter shows only measured numbers")
    rows = {row["scope"]: row for row in book.meter()}
    check(rows[BUDGET.SESSION]["spent"] == 100
          and rows[BUDGET.SESSION]["unit"] == "calls",
          "the meter reports the recorded spend, with its unit")
    check(BUDGET.REQUEST not in rows,
          "a scope with no budget does not appear as a zero")

    print()
    print("9. A budget nobody could check is refused")
    check(raises(lambda: book.set_budget(BUDGET.SESSION, 5, "")),
          "a budget with no unit is refused")
    check(raises(lambda: book.set_budget(BUDGET.SESSION, -1, "calls")),
          "a negative budget is refused")
    check(raises(lambda: book.set_budget("whenever", 5, "calls")),
          "an unknown scope is refused")
    check(empty.may_spend("whenever").get("refused") == "UNKNOWN_SCOPE",
          "and asking against one comes back as a refusal, not an exception")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    only measured numbers, and Heron's own work yields first")
    return 0


if __name__ == "__main__":
    sys.exit(main())
