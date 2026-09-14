# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-QUE-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Queue Manager - it holds work in order and it runs none of it.

    python brain/heron_queue.py

WHAT IT IS FOR (docs/28, HERON-OPS-QUE-002)
--------------------------------------------
"Background work queue." T1 - no model call, nothing to be clever about.

docs/21 s12 gives the ladder, and the names matter more than the numbers:

    P0 User interaction    P1 Required execution   P2 Required validation
    P3 Maintenance         P4 Learning             P5 Optimization
    P6 Cleanup

WHAT IT DOES NOT DO, AND WHY THAT IS THE DESIGN
-------------------------------------------------
It does not decide what may run NOW. That is HERON-OPS-SCH-001, which
applies docs/21's actual rule - never run P2-P6 during a user task - and
asks the budget before anything background is dispatched.

Splitting them is not tidiness. A queue that also decided would need to be
told whether a user task is running, and a caller that can state that is a
caller that can make background work jump the interactive one. The queue
holds; the scheduler decides; and the thing the scheduler needs to know is
read from the world rather than passed in.

NOTHING IS LOST, EVER
----------------------
Golden Rule 14. A work item that cannot be queued is REFUSED with a reason,
never dropped; an item taken out and failed goes back with its failure
recorded rather than vanishing. A queue that quietly loses work is worse
than one that is full, because a full queue says so.

FIFO WITHIN A PRIORITY
-----------------------
Two P6 items run in the order they arrived. Without that, a queue under
load starves whatever was unlucky enough to be added first, and "it is only
cleanup" is how a cleanup job never runs for a month.

BOUNDED, AND IT SAYS SO WHEN IT IS FULL
-----------------------------------------
An unbounded queue is a memory leak with a waiting list. This one has a
limit, refuses past it with QUEUE_FULL, and the refusal names the lowest
priority currently held - because the answer is almost always "something in
here should be dropped, and it is not the thing you just tried to add".

EVERY ITEM SAYS WHO ASKED AND WHY
-----------------------------------
A queue nobody can read is a queue nobody can drain. `asked_by` and
`because` are required, not decoration: the first question about a stuck
queue is always what is in it and who wanted it.
"""

import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/21 s12, in order. The index IS the priority.
PRIORITIES = ("P0 user interaction", "P1 required execution",
              "P2 required validation", "P3 maintenance", "P4 learning",
              "P5 optimization", "P6 cleanup")

# docs/21 s12 and docs/11 s7: "never run P2-P6 during a user task". P0 and
# P1 are the two that may. Declared here because the queue REPORTS which
# band an item is in; HERON-OPS-SCH-001 is what enforces it.
INTERACTIVE = (0, 1)

# A default, not a law. A caller that knows its own memory says so.
DEFAULT_LIMIT = 1000


def band(priority):
    """'interactive' for P0-P1, 'background' for P2-P6."""
    return "interactive" if priority in INTERACTIVE else "background"


class Queue(object):
    """
    Work waiting, in priority order and FIFO within a priority.

    One queue per process and never a global, for the reason the Event Bus
    gives: a test that cannot get a clean one ends up asserting against
    whatever the last test added.
    """

    def __init__(self, limit=DEFAULT_LIMIT):
        self._items = []          # in priority then arrival order
        self._next_id = 1
        self._done = []           # taken and finished, newest last
        self.limit = int(limit)

    # ------------------------------------------------------------ adding
    def add(self, work, priority, asked_by, because, now=None):
        """
        {item} for work accepted - or a refusal. Nothing is ever dropped.
        """
        if not str(work or "").strip():
            return {"refused": "NOTHING_TO_QUEUE",
                    "why": "a queue item names the work. An empty one would "
                           "sit in the queue as a thing nobody can do and "
                           "nobody can cancel."}

        if not isinstance(priority, int) or priority < 0 \
                or priority >= len(PRIORITIES):
            return {"refused": "NO_SUCH_PRIORITY",
                    "why": "'%s' is not a priority. docs/21 s12 has %d, P0 to "
                           "P%d: %s. A priority nobody recognises is not read "
                           "as the least urgent one - it is refused, because "
                           "guessing here decides what yields to what."
                           % (priority, len(PRIORITIES), len(PRIORITIES) - 1,
                              ", ".join(PRIORITIES))}

        if not str(asked_by or "").strip() or not str(because or "").strip():
            return {"refused": "NO_ASKER",
                    "why": "every item says who asked and why. The first "
                           "question about a stuck queue is what is in it and "
                           "who wanted it, and an item that cannot answer "
                           "that is one nobody will dare to drop."}

        if len(self._items) >= self.limit:
            lowest = max(item["priority"] for item in self._items)
            return {"refused": "QUEUE_FULL",
                    "why": "%d items is the limit. The lowest priority held "
                           "is %s - something in the queue should probably go "
                           "before this one is turned away, and it is not "
                           "usually the item that just arrived."
                           % (self.limit, PRIORITIES[lowest])}

        item = {"id": self._next_id, "work": str(work).strip(),
                "priority": priority, "band": band(priority),
                "asked_by": str(asked_by).strip(),
                "because": str(because).strip(),
                "queued_at": now if now is not None else time.time(),
                "attempts": 0, "failures": []}
        self._next_id += 1

        # PRIORITY THEN ARRIVAL. Inserted rather than sorted, so two items
        # at the same priority keep the order they arrived in - a sort by
        # priority alone is not stable across implementations, and "it is
        # only cleanup" is how a cleanup job never runs for a month.
        where = len(self._items)
        for index, existing in enumerate(self._items):
            if existing["priority"] > priority:
                where = index
                break
        self._items.insert(where, item)
        return {"item": dict(item),
                "why": "queued at %s, position %d of %d"
                       % (PRIORITIES[priority], where + 1, len(self._items))}

    # ------------------------------------------------------------ reading
    def peek(self, at_most=None):
        """What is waiting, in the order it will come out. A copy."""
        items = [dict(item) for item in self._items]
        return items if at_most is None else items[:at_most]

    def waiting(self, band_wanted=None):
        """How many are waiting, optionally in one band only."""
        if band_wanted is None:
            return len(self._items)
        return len([i for i in self._items if i["band"] == band_wanted])

    def highest_waiting(self):
        """The priority of the most urgent thing waiting, or None."""
        return self._items[0]["priority"] if self._items else None

    # ------------------------------------------------------------- taking
    def take(self, item_id=None):
        """
        Remove and return the next item - or one named by id.

        `item_id` exists for HERON-OPS-SCH-001, which decides what may run
        now and then takes THAT. Without it the queue would have to be told
        whether a user task is running, which is the decision it does not
        make.
        """
        if not self._items:
            return {"refused": "NOTHING_WAITING",
                    "why": "the queue is empty. That is not an error and is "
                           "reported as data rather than raised."}
        if item_id is None:
            item = self._items.pop(0)
        else:
            found = [i for i in self._items if i["id"] == item_id]
            if not found:
                return {"refused": "NO_SUCH_ITEM",
                        "why": "item %s is not in the queue. It may have been "
                               "taken already, which is a different thing "
                               "from never having been there - and neither is "
                               "guessed at here." % item_id}
            item = found[0]
            self._items.remove(item)
        item["attempts"] += 1
        return {"item": item, "why": "taken from %s"
                                     % PRIORITIES[item["priority"]]}

    def finished(self, item):
        """Record an item as done. It leaves the queue and is kept."""
        self._done.append(dict(item))
        return {"why": "%s done after %d attempt(s)"
                       % (item.get("work"), item.get("attempts", 1))}

    def failed(self, item, why, requeue=True):
        """
        Put a failed item back, with the failure recorded on it.

        Golden Rule 14: never silently discard. `requeue=False` keeps it out
        of the queue and still keeps the record - a caller that has decided
        not to try again says so, and the item is not lost either way.
        """
        item = dict(item)
        item["failures"] = list(item.get("failures") or []) + [str(why)]
        if not requeue:
            self._done.append(item)
            return {"item": item,
                    "why": "not requeued, and kept in the record with %d "
                           "failure(s). Nothing is dropped."
                           % len(item["failures"])}
        where = len(self._items)
        for index, existing in enumerate(self._items):
            if existing["priority"] > item["priority"]:
                where = index
                break
        self._items.insert(where, item)
        return {"item": item,
                "why": "back at %s with %d failure(s) recorded"
                       % (PRIORITIES[item["priority"]],
                          len(item["failures"]))}

    def record(self):
        """Everything taken out and finished, oldest first. A copy."""
        return [dict(item) for item in self._done]


def main(argv):
    print("QUEUE MANAGER   it holds work in order and runs none of it")
    print("=" * 70)

    queue = Queue(limit=4)
    for work, priority, who, why in [
            ("re-index the fragment library", 4, "the learning loop",
             "343 fragments changed"),
            ("answer the user's question", 0, "the host", "somebody asked"),
            ("tidy the audit log", 6, "maintenance", "it is 40 MB"),
            ("validate the last write", 2, "the executor",
             "a move was applied")]:
        answer = queue.add(work, priority, who, why)
        print("  add  %-32s %s" % (work[:32], answer.get("why",
                                                         answer.get("refused"))))

    print()
    print("  Waiting, in the order they will come out:")
    for item in queue.peek():
        print("    %-24s %-24s %s" % (PRIORITIES[item["priority"]],
                                      item["work"][:24], item["asked_by"]))

    print()
    for label, args in [("a fifth item, with the queue full",
                         ("one more thing", 3, "somebody", "because")),
                        ("a priority that does not exist",
                         ("something", 9, "somebody", "because")),
                        ("work with nobody asking",
                         ("something", 3, "", ""))]:
        answer = queue.add(*args)
        print("  %-34s %s" % (label, answer.get("refused")))
        print("      %s" % answer["why"][:94])

    print()
    taken = queue.take()["item"]
    print("  take          %s (%s)" % (taken["work"],
                                       PRIORITIES[taken["priority"]]))
    back = queue.failed(taken, "the host was busy")
    print("  it failed     %s" % back["why"])
    print("  and it is back in the queue at position %d of %d"
          % ([i["id"] for i in queue.peek()].index(taken["id"]) + 1,
             queue.waiting()))
    print()
    print("  %d waiting: %d interactive, %d background."
          % (queue.waiting(), queue.waiting("interactive"),
             queue.waiting("background")))
    print("  WHICH OF THEM MAY RUN NOW IS NOT DECIDED HERE. docs/21 s12 says")
    print("  never run P2-P6 during a user task, and HERON-OPS-SCH-001 is")
    print("  what enforces it - a queue that decided would have to be told")
    print("  whether a user task is running, and a caller that can say that")
    print("  is a caller that can make background work jump the person.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
