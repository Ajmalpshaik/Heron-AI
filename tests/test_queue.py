# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-QUE-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Queue Manager - order, fairness, and nothing lost.

    python tests/test_queue.py

WHAT IT PROVES
  1. PRIORITY ORDER IS docs/21 s12's, and P0 comes out first however late
     it arrived.

  2. FIFO WITHIN A PRIORITY. Two P6 items run in the order they arrived -
     without that a queue under load starves whatever was added first, and
     "it is only cleanup" is how a cleanup job never runs for a month.

  3. A PRIORITY NOBODY RECOGNISES IS REFUSED, not read as the least urgent.
     Guessing here decides what yields to what.

  4. NOTHING IS LOST. A failed item goes back with its failure recorded; one
     the caller will not retry is kept in the record rather than dropped
     (Golden Rule 14).

  5. IT IS BOUNDED, and the refusal names the lowest priority held - the
     answer is usually that something in the queue should go, and it is not
     the item that just arrived.

  6. EVERY ITEM SAYS WHO ASKED AND WHY.

  7. IT DECIDES NOTHING ABOUT WHAT MAY RUN. No parameter tells it whether a
     user task is running, because a caller that can say that is a caller
     that can make background work jump the person. That rule is
     HERON-OPS-SCH-001's.

  8. READS ARE COPIES. A caller that mutates what peek() returned has not
     reordered the queue.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED
     BY THIS SUITE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_queue as QUE                                     # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_queue.py"),
                  encoding="utf-8").read()

    def add(queue, work, priority, who="a session", why="a reason"):
        answer = queue.add(work, priority, who, why)
        if "refused" in answer:
            reached.add(answer["refused"])
        return answer

    print("1. Priority order is docs/21 s12's")
    check(len(QUE.PRIORITIES) == 7 and QUE.PRIORITIES[0].startswith("P0"),
          "seven levels, P0 to P6")
    queue = QUE.Queue()
    for priority in (6, 4, 2, 0):
        add(queue, "work at P%d" % priority, priority)
    check([i["priority"] for i in queue.peek()] == [0, 2, 4, 6],
          "P0 comes out first however late it arrived")
    check(queue.highest_waiting() == 0,
          "and the most urgent waiting is reported as P0")
    check(QUE.band(0) == "interactive" and QUE.band(1) == "interactive",
          "P0 and P1 are the interactive band (docs/11 s7, docs/21 s12)")
    check(all(QUE.band(p) == "background" for p in range(2, 7)),
          "and P2 to P6 are background - the ones that may not run during "
          "a user task")

    print()
    print("2. FIFO within a priority")
    queue = QUE.Queue()
    for n in range(4):
        add(queue, "cleanup %d" % n, 6)
    check([i["work"] for i in queue.peek()]
          == ["cleanup 0", "cleanup 1", "cleanup 2", "cleanup 3"],
          "four P6 items keep the order they arrived in")
    add(queue, "urgent", 0)
    check(queue.peek()[0]["work"] == "urgent",
          "and a P0 arriving last still goes to the front")
    check([i["work"] for i in queue.peek()][1:]
          == ["cleanup 0", "cleanup 1", "cleanup 2", "cleanup 3"],
          "without disturbing the order behind it")

    print()
    print("3. A priority nobody recognises is refused")
    queue = QUE.Queue()
    for bad in (7, -1, 99, "P3", None, 2.5):
        answer = add(queue, "something", bad)
        check(answer.get("refused") == "NO_SUCH_PRIORITY",
              "%r is refused, not read as the least urgent" % (bad,))
    check(add(queue, "", 3).get("refused") == "NOTHING_TO_QUEUE",
          "and empty work is refused too")

    print()
    print("4. Nothing is lost")
    queue = QUE.Queue()
    add(queue, "the thing", 3)
    item = queue.take()["item"]
    check(queue.waiting() == 0, "taking it removes it from the queue")
    back = queue.failed(item, "the host was busy")
    check(queue.waiting() == 1, "a failed item goes back")
    check(queue.peek()[0]["failures"] == ["the host was busy"],
          "with the failure recorded on it")
    check(queue.peek()[0]["attempts"] == 1,
          "and the attempt counted, so a loop is visible")
    item = queue.take()["item"]
    queue.failed(item, "busy again", requeue=False)
    check(queue.waiting() == 0, "one the caller will not retry does not go "
                                "back...")
    kept = queue.record()
    check(len(kept) == 1 and len(kept[0]["failures"]) == 2,
          "...and is kept in the record with both failures (Golden Rule 14)")
    queue = QUE.Queue()
    answer = queue.take()
    check(answer.get("refused") == "NOTHING_WAITING",
          "an empty queue reports as data, it does not raise")
    reached.add("NOTHING_WAITING")
    add(queue, "a thing", 3)
    answer = queue.take(item_id=999)
    check(answer.get("refused") == "NO_SUCH_ITEM",
          "and an id that is not there is refused, not guessed at")
    reached.add("NO_SUCH_ITEM")

    print()
    print("5. It is bounded, and says what to do about it")
    queue = QUE.Queue(limit=3)
    for n in range(3):
        add(queue, "work %d" % n, 6)
    answer = add(queue, "one more", 3)
    check(answer.get("refused") == "QUEUE_FULL", "the limit holds")
    check("P6 cleanup" in answer["why"],
          "and the refusal names the lowest priority held")
    check(queue.waiting() == 3,
          "the refused item is not in the queue - refused is not queued")

    print()
    print("6. Every item says who asked and why")
    queue = QUE.Queue()
    for who, why in (("", "a reason"), ("somebody", ""), ("  ", "  ")):
        answer = queue.add("work", 3, who, why)
        if "refused" in answer:
            reached.add(answer["refused"])
        check(answer.get("refused") == "NO_ASKER",
              "'%s' / '%s' is refused" % (who, why))
    answer = add(queue, "work", 3, "the learning loop", "343 fragments moved")
    check(answer["item"]["asked_by"] == "the learning loop"
          and answer["item"]["because"] == "343 fragments moved",
          "and both are carried on the item")

    print()
    print("7. It decides nothing about what may run")
    import inspect
    for method in (QUE.Queue.take, QUE.Queue.peek, QUE.Queue.add):
        names = set(inspect.signature(method).parameters)
        for forbidden in ("user_task", "user_busy", "running", "may_run",
                          "interactive_running"):
            check(forbidden not in names,
                  "%s() has no '%s' parameter" % (method.__name__, forbidden))
    check("SCH-001" in source,
          "and the source says which agent does decide")
    for word in ("def run", "subprocess", "exec(", "thread", "Thread"):
        check(word not in source,
              "the queue runs nothing - no %s" % word)

    print()
    print("8. Reads are copies")
    queue = QUE.Queue()
    add(queue, "first", 0)
    add(queue, "second", 6)
    seen = queue.peek()
    seen[0]["priority"] = 6
    seen.reverse()
    check([i["work"] for i in queue.peek()] == ["first", "second"],
          "mutating what peek() returned changes nothing in the queue")
    queue.finished(queue.take()["item"])
    kept = queue.record()
    kept[0]["work"] = "rewritten"
    check(queue.record()[0]["work"] == "first",
          "and the record is a copy too")

    print()
    print("9. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-OPS-QUE-002.yaml"))
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
    print("PASS    it holds work in order and loses none of it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
