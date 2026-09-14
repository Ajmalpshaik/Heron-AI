# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-TOK-015
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Token & Cost Budget - what has been spent, in a unit somebody measured.

    python brain/heron_budget.py      a session spending itself down to STOPPED

WHAT THE REGISTER ASKS FOR, AND THE ONE PART THAT CANNOT BE DONE HONESTLY
--------------------------------------------------------------------------
The row reads: "Counts tokens BEFORE sending, enforces per-request, per-session
and background budgets, and can force a cheaper model when a budget is tight.
Feeds the visible cost meter."

Everything there is built except counting tokens before sending, and that is
not an omission. D-58 settles it and docs/19 s2 states it plainly: **Heron has
no tokeniser, and inventing a number would be worse than counting a real
thing.** A guessed count would be stated as a number, read as a fact, and used
to refuse a modeller's request. That is the worst shape an estimate can take.

So the rule here is: SPEND IS ONLY EVER WHAT A PROVIDER REPORTED. A caller may
pass an estimate to `may_spend()`, it is judged as an estimate, and it is never
recorded. What is recorded came back from something that actually knows.

Counting before sending becomes possible the day an adapter offers a real
count - the seam is `may_spend(..., estimate=)`, and nothing else changes.

THREE BUDGETS, AND BACKGROUND YIELDS FIRST
-------------------------------------------
    request      one job asked for by a person
    session      everything since Heron started talking to this user
    background   work HERON started on its own - embedding, batch scoring

Background yields first, by having a smaller budget and by being STOPPED while
the session is merely TIGHT. A system that lets its own housekeeping consume
the budget, and then refuses the person who asked for something, has its
priorities the wrong way round.

UNITS ARE NOT CONVERTED, THEY ARE MATCHED
------------------------------------------
A budget is declared in a unit and spend must arrive in that unit. Tokens are
not pounds and neither is a call. This module refuses a mismatch rather than
converting, because the conversion rate is a provider's price list and a price
list in here would be out of date the week it was written.

IT NEVER PICKS A PROVIDER. When things are tight it says TIGHT, and the caller
asks the Model Router for cheaper thinking. Two agents, one decision each.
"""

import sys

REQUEST, SESSION, BACKGROUND = "request", "session", "background"
SCOPES = (REQUEST, SESSION, BACKGROUND)

NORMAL, TIGHT, STOPPED = "NORMAL", "TIGHT", "STOPPED"

# The fraction of a budget at which the posture changes. Background is held to
# the tighter line on purpose - see the docstring.
TIGHT_AT = 0.75
BACKGROUND_STOPS_AT = 0.75


class Budget(object):
    """One user's budgets and what they have spent. No file, no clock, no I/O."""

    def __init__(self):
        self._limit = {}             # scope -> (amount, unit)
        self._spent = {}             # scope -> amount
        self.ledger = []             # (scope, amount, unit, reported_by)

    # ----------------------------------------------------------------- set
    def set_budget(self, scope, amount, unit):
        if scope not in SCOPES:
            raise ValueError("'%s' is not a budget scope - one of: %s"
                             % (scope, ", ".join(SCOPES)))
        if amount is None or amount < 0:
            raise ValueError("a budget of '%s' is not a budget" % amount)
        if not unit:
            raise ValueError("a budget with no unit is a number nobody can "
                             "check - say what it counts")
        self._limit[scope] = (float(amount), unit)
        self._spent.setdefault(scope, 0.0)

    def remaining(self, scope):
        if scope not in self._limit:
            return None
        limit, _unit = self._limit[scope]
        return limit - self._spent.get(scope, 0.0)

    def unit(self, scope):
        return self._limit[scope][1] if scope in self._limit else None

    # ------------------------------------------------------------- posture
    def posture(self, scope):
        """NORMAL, TIGHT or STOPPED for one scope."""
        if scope not in self._limit:
            return NORMAL
        limit, _unit = self._limit[scope]
        if limit == 0:
            return STOPPED
        used = self._spent.get(scope, 0.0) / limit
        if used >= 1.0:
            return STOPPED
        if scope == BACKGROUND and used >= BACKGROUND_STOPS_AT:
            return STOPPED
        if used >= TIGHT_AT:
            return TIGHT
        return NORMAL

    # ---------------------------------------------------------- may I spend
    def may_spend(self, scope, estimate=None, unit=None):
        """
        {allowed, posture, why} before a call is made.

        `estimate` is treated as what it is. It can refuse a call that would
        obviously overrun, and it is never added to the ledger - the ledger is
        for numbers something measured.
        """
        if scope not in SCOPES:
            return {"allowed": False, "posture": STOPPED,
                    "refused": "UNKNOWN_SCOPE",
                    "why": "'%s' is not a budget scope - one of: %s"
                           % (scope, ", ".join(SCOPES))}

        if scope not in self._limit:
            return {"allowed": True, "posture": NORMAL,
                    "note": "NO_BUDGET_SET",
                    "why": "NO_BUDGET_SET: no budget is set for %s, so nothing is enforced. "
                           "That is a choice somebody has to make, not a "
                           "limit this agent may invent." % scope}

        limit, budget_unit = self._limit[scope]
        spent = self._spent.get(scope, 0.0)
        posture = self.posture(scope)
        numbers = "%s of %s %s spent, %s left" % (
            _short(spent), _short(limit), budget_unit,
            _short(limit - spent))

        if posture == STOPPED:
            # Background stops with budget still in it, and saying "spent"
            # there would be a lie a reader can check - the same line prints
            # the remainder.
            if scope == BACKGROUND and spent < limit:
                why = ("background work stops at %d%% of its budget so that "
                       "the person's work keeps the rest - %s. Nothing has "
                       "been sent." % (BACKGROUND_STOPS_AT * 100, numbers))
            else:
                why = ("the %s budget is spent - %s. Nothing has been sent."
                       % (scope, numbers))
            return {"allowed": False, "posture": STOPPED,
                    "refused": "BUDGET_EXCEEDED", "why": why}

        if estimate is not None:
            if unit and unit != budget_unit:
                return {"allowed": False, "posture": posture,
                        "refused": "UNIT_MISMATCH",
                        "why": "the estimate is in %s and the %s budget is in "
                               "%s. Heron does not convert between them - the "
                               "rate is a provider's price list."
                               % (unit, scope, budget_unit)}
            if spent + estimate > limit:
                return {"allowed": False, "posture": posture,
                        "refused": "BUDGET_EXCEEDED",
                        "why": "the caller's ESTIMATE of %s %s would take the "
                               "%s budget past its limit - %s. The estimate is "
                               "the caller's, not a count Heron made."
                               % (_short(estimate), budget_unit, scope,
                                  numbers)}

        return {"allowed": True, "posture": posture, "why": numbers}

    # --------------------------------------------------------------- record
    def record(self, scope, amount, unit, reported_by):
        """
        Add what a call actually cost. Refuses anything nobody measured.

        `reported_by` is required and is not decoration: it is the difference
        between a ledger of facts and a ledger of guesses, and the second kind
        is the one that ends up in a cost meter somebody trusts.
        """
        if scope not in SCOPES:
            raise ValueError("'%s' is not a budget scope" % scope)
        if not reported_by:
            raise ValueError(
                "a spend with no source is a guess. Heron has no tokeniser "
                "(D-58) - record what the provider reported, and name it.")
        if scope in self._limit and unit != self._limit[scope][1]:
            raise ValueError(
                "spend is in %s and the %s budget is in %s. Units are matched "
                "here, never converted." % (unit, scope, self._limit[scope][1]))
        self._spent[scope] = self._spent.get(scope, 0.0) + float(amount)
        self.ledger.append((scope, float(amount), unit, reported_by))
        return self._spent[scope]

    # ---------------------------------------------------------------- meter
    def meter(self):
        """What the visible cost meter shows. Only measured numbers reach it."""
        rows = []
        for scope in SCOPES:
            if scope not in self._limit:
                continue
            limit, unit = self._limit[scope]
            rows.append({"scope": scope, "spent": self._spent.get(scope, 0.0),
                         "limit": limit, "unit": unit,
                         "posture": self.posture(scope)})
        return rows


def _short(number):
    """A number a person reads, without a trailing .0 on a whole one."""
    return "%g" % number


def main(argv):
    budget = Budget()
    budget.set_budget(SESSION, 100, "calls")
    budget.set_budget(BACKGROUND, 20, "calls")

    print("TOKEN & COST BUDGET   a session spending itself down")
    print("=" * 67)
    for spend in (70, 6, 20, 10):
        answer = budget.may_spend(SESSION)
        print("  session   allowed=%-5s %-7s %s"
              % (answer["allowed"], answer["posture"], answer["why"]))
        if answer["allowed"]:
            budget.record(SESSION, spend, "calls", reported_by="the adapter")

    print()
    print("  Background is held to the tighter line, so Heron's own work")
    print("  stops before a person's does:")
    budget.record(BACKGROUND, 15, "calls", reported_by="the adapter")
    answer = budget.may_spend(BACKGROUND)
    print("  background allowed=%-5s %-7s %s"
          % (answer["allowed"], answer["posture"], answer["why"]))

    print()
    print("  An estimate is judged as an estimate, and never recorded:")
    fresh = Budget()
    fresh.set_budget(REQUEST, 10, "calls")
    answer = fresh.may_spend(REQUEST, estimate=12, unit="calls")
    print("  request   allowed=%-5s %s" % (answer["allowed"], answer["why"]))
    print("  ledger    %d entries" % len(fresh.ledger))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
