# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-SCH-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Background Scheduler - it fails closed, and four ways of not answering
all mean "somebody is working".

    python tests/test_scheduler.py

WHAT IT PROVES
  1. P2-P6 NEVER RUN DURING A USER TASK. docs/21 s12 and docs/11 s7 leave
     no room in it, and neither does this.

  2. P0 AND P1 RUN ANYWAY. The rule is about background work; a user's own
     work is not held back by a rule protecting them.

  3. IT FAILS CLOSED, FOUR WAYS. No reader, a reader that raises, a reader
     answering None, and a VALUE passed where a reader belongs - all four
     answer "somebody is working". The convenient answer is never the
     default, which is the hole the deployment ladder took three review
     rounds to close.

  4. THE USER-TASK FACT IS ASKED, NOT ACCEPTED, and the reader is asked at
     the moment of the decision rather than once at the start.

  5. THE BUDGET IS ASKED FOR BACKGROUND WORK AND NEVER FOR A PERSON'S.

  6. THE CHOSEN ITEM LEAVES THE QUEUE, so two callers cannot be handed the
     same work - and a refusal takes nothing.

  7. IT EXECUTES NOTHING.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED
     BY THIS SUITE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scheduler as SCH                                 # noqa: E402
import heron_queue as QUE                                     # noqa: E402
import heron_budget as TOK                                    # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def queue_of(*priorities):
    queue = QUE.Queue()
    for priority in priorities:
        queue.add("work at P%d" % priority, priority, "a session", "a reason")
    return queue


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_scheduler.py"),
                  encoding="utf-8").read()

    def ask(queue, **kw):
        answer = SCH.decide(queue, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. P2-P6 never run during a user task")
    for priority in range(2, 7):
        answer = ask(queue_of(priority), busy=lambda: True)
        check(not answer["run"]
              and answer["refused"] == "A_PERSON_IS_WORKING",
              "P%d waits while somebody is working" % priority)
    answer = ask(queue_of(6), busy=lambda: True)
    check("docs/21" in answer["why"] and "docs/11" in answer["why"],
          "and the refusal cites the two documents that say so")
    check(SCH.MAY_RUN_DURING_A_USER_TASK == (0, 1),
          "only P0 and P1 are exempt")

    print()
    print("2. P0 and P1 run anyway")
    for priority in (0, 1):
        answer = ask(queue_of(priority), busy=lambda: True)
        check(answer["run"] and answer["item"]["priority"] == priority,
              "P%d runs while somebody is working - the rule protects them, "
              "it does not hold them back" % priority)
    # THE MOST URGENT THING DECIDES, not the queue's average.
    answer = ask(queue_of(6, 0), busy=lambda: True)
    check(answer["run"] and answer["item"]["priority"] == 0,
          "a P0 behind a P6 in the queue is still what comes out")

    print()
    print("3. It fails closed, four ways")
    for label, busy in (("no reader at all", None),
                        ("a reader that raises", lambda: 1 / 0),
                        ("a reader answering None", lambda: None),
                        ("a value where a reader belongs", False),
                        ("a value that is truthy", True),
                        ("a string", "no")):
        answer = ask(queue_of(4), busy=busy)
        check(not answer["run"]
              and answer["refused"] == "A_PERSON_IS_WORKING",
              "%s reads as 'somebody is working'" % label)
    answer = ask(queue_of(4), busy=None)
    check("does not get to use their" in answer["unjudged"][0]
          or "no reader was given" in answer["why"],
          "and the answer says WHY it defaulted that way")

    print()
    print("4. The fact is asked, not accepted, and asked at decision time")
    import inspect
    names = set(inspect.signature(SCH.decide).parameters)
    for forbidden in ("user_busy", "user_task", "interactive", "idle",
                      "may_run"):
        check(forbidden not in names,
              "decide() has no '%s' parameter a caller could assert"
              % forbidden)
    calls = []

    def counting():
        calls.append(1)
        return False
    ask(queue_of(4), busy=counting)
    check(len(calls) == 1, "the reader is called once per decision...")
    ask(queue_of(4), busy=counting)
    check(len(calls) == 2, "...and again on the next one, not cached")

    print()
    print("5. The budget is asked for background work and never a person's")
    spent = TOK.Budget()
    spent.set_budget("background", 10, "calls")
    spent.record("background", 10, "calls", "a provider")
    answer = ask(queue_of(4), busy=lambda: False, budget=spent)
    check(not answer["run"]
          and answer["refused"] == "BACKGROUND_BUDGET_SPENT",
          "background work stops when its ceiling is reached")
    answer = ask(queue_of(0), busy=lambda: False, budget=spent)
    check(answer["run"],
          "and a P0 runs anyway - stopping a person because a cleanup job "
          "spent the budget is the ceiling protecting the wrong side")
    room = TOK.Budget()
    room.set_budget("background", 100, "calls")
    answer = ask(queue_of(4), busy=lambda: False, budget=room)
    check(answer["run"], "with room left, background work runs")
    check(SCH.BACKGROUND_SCOPE == "background",
          "and the scope asked about is the background one")

    print()
    print("6. The chosen item leaves the queue; a refusal takes nothing")
    queue = queue_of(4, 6)
    answer = ask(queue, busy=lambda: False)
    check(answer["run"] and queue.waiting() == 1,
          "the dispatched item is taken, so two callers cannot get it twice")
    queue = queue_of(4, 6)
    answer = ask(queue, busy=lambda: True)
    check(not answer["run"] and queue.waiting() == 2,
          "and a refusal leaves the queue exactly as it was")
    # A CALLER THAT CANNOT RUN IT PUTS IT BACK, with the failure recorded.
    queue = queue_of(4)
    item = ask(queue, busy=lambda: False)["item"]
    queue.failed(item, "the worker died")
    check(queue.waiting() == 1
          and queue.peek()[0]["failures"] == ["the worker died"],
          "work handed back arrives with its failure on it (Golden Rule 14)")

    print()
    print("7. It executes nothing")
    for word in ("subprocess", "exec(", "eval(", "Thread", "os.system",
                 "importlib"):
        check(word not in source, "no %s in the source" % word)
    answer = ask(queue_of(4), busy=lambda: False)
    check(any("HOW LONG" in note for note in answer["unjudged"]),
          "and it says it does not know how long the work will take")

    print()
    print("8. Every failure the contract declares is named and reached")
    check(ask(None).get("refused") == "NO_QUEUE",
          "no queue is refused - this agent keeps none of its own")
    check(ask(QUE.Queue()).get("refused") == "NOTHING_WAITING",
          "an empty queue is data, not a failure")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-SCH-001.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    a scheduler that cannot see the person does not run")
    return 0


if __name__ == "__main__":
    sys.exit(main())
