# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-SCH-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Background Scheduler - user work always wins, and silence means it is.

    python brain/heron_scheduler.py

WHAT IT IS FOR (docs/28, HERON-OPS-SCH-001)
--------------------------------------------
"P0-P6 priority. User work always wins." T1 - no model call.

docs/21 s12 and docs/11 s7 give the rule with no room in it: **never run
P2-P6 during a user task.** Not "prefer not to" and not "rank below" -
background work YIELDS to interactive work. The queue (HERON-OPS-QUE-002)
holds the work in order; this decides what may leave it now.

IT FAILS CLOSED, AND THAT IS THE WHOLE DESIGN
-----------------------------------------------
To apply the rule it has to know whether a user task is running, and there
are only two ways to get that: be told, or ask.

Being told is a hole. A caller that can say "no user task is running" is a
caller that can make background work jump the person the rule exists to
protect, and it will be the convenient answer every time - which is exactly
how the deployment ladder's `from_stage` hole worked, three review rounds
running.

So `busy` is a READER - asked at the moment of the decision - and with no
reader supplied, or one that cannot answer, THE ANSWER IS YES. Somebody
might be working, so background work waits. D-21 set this precedent for
failure analysis: an unknown outcome on an operation that can write is
classified as unknown, never as retryable, because a future caller who
never read the file gets the safe answer by default rather than the
convenient one.

That means the default behaviour of this agent is to dispatch nothing
background at all. That is not a bug to be tuned away. A scheduler with no
way to see the user is a scheduler that should not be running their
machine's spare capacity.

THE BUDGET IS ASKED, NOT ASSUMED
----------------------------------
Background work is held to the tighter line - HERON-KRN-TOK-015 stops it at
75% so a person's work keeps the rest. This asks before dispatching
background work and never asks for interactive work: stopping a user
because a cleanup job spent the budget would be the ceiling protecting the
wrong side.

IT DISPATCHES NOTHING
----------------------
It says what should run next and takes it out of the queue. Running it is
somebody else's - this agent is T1, it makes a decision from two facts, and
a scheduler that also executed would be the thing deciding whether to
interrupt the person AND the thing that wants to.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/11 s7, docs/21 s12. The two that may run while somebody is working.
MAY_RUN_DURING_A_USER_TASK = (0, 1)

# The budget scope background work is held to (HERON-KRN-TOK-015).
BACKGROUND_SCOPE = "background"


def _ask(busy):
    """
    (answer, how) - is a user task running?

    True when nobody could tell us, and the reason says so. A reader that
    raises is the same as no reader: something is wrong with the one thing
    that protects the person, and the safe reading of that is "yes".
    """
    if busy is None:
        return True, ("no reader was given, so nothing could see whether "
                      "somebody is working. That reads as YES here: a "
                      "scheduler that cannot see the user does not get to "
                      "use their machine")
    if not callable(busy):
        return True, ("`busy` was a value rather than a reader, and a value "
                      "is a caller stating the one fact this decision turns "
                      "on. It is asked, not accepted")
    try:
        answer = busy()
    except Exception as exc:                      # the reader is the guard
        return True, ("the reader raised %s: %s. A broken guard reads as "
                      "'somebody is working' - the alternative is a broken "
                      "guard reading as permission"
                      % (type(exc).__name__, exc))
    if answer is None:
        return True, ("the reader answered None, which is not a no. An "
                      "unknown answer about whether somebody is working is "
                      "treated as yes")
    return bool(answer), ("the reader answered %s"
                          % ("yes" if answer else "no"))


def decide(queue, busy=None, budget=None, now=None):
    """
    {run, item, why, unjudged} - or {run: False} with the reason.

    Nothing is executed. The chosen item is TAKEN from the queue so two
    callers cannot be handed the same work, and a caller that then cannot
    run it puts it back with `queue.failed(item, why)`.
    """
    if queue is None:
        return {"run": False, "refused": "NO_QUEUE",
                "why": "there is nothing to schedule from. The queue is "
                       "HERON-OPS-QUE-002 and this agent does not keep one "
                       "of its own - two queues is one queue and one bug."}

    waiting = queue.waiting()
    if not waiting:
        return {"run": False, "refused": "NOTHING_WAITING",
                "why": "the queue is empty. Reported as data; an idle "
                       "scheduler is not a failed one."}

    user_working, how = _ask(busy)

    top = queue.peek(1)[0]
    if top["priority"] in MAY_RUN_DURING_A_USER_TASK:
        taken = queue.take(item_id=top["id"])
        return {"run": True, "item": taken["item"],
                "why": "%s may run whether or not somebody is working "
                       "(docs/11 s7). %s."
                       % (_name(top["priority"]), how),
                "unjudged": _unjudged(user_working, how)}

    # EVERYTHING BELOW HERE IS BACKGROUND, AND THE RULE HAS NO ROOM IN IT.
    if user_working:
        return {"run": False, "refused": "A_PERSON_IS_WORKING",
                "why": "the most urgent thing waiting is %s, and P2-P6 never "
                       "run during a user task (docs/21 s12, docs/11 s7). "
                       "%s. %d item(s) wait; none of them is more important "
                       "than the person."
                       % (_name(top["priority"]), how, waiting),
                "unjudged": _unjudged(user_working, how)}

    # THE BUDGET IS ASKED FOR BACKGROUND WORK AND NEVER FOR A PERSON'S.
    if budget is not None:
        verdict = budget.may_spend(BACKGROUND_SCOPE)
        if not verdict.get("allowed"):
            return {"run": False, "refused": "BACKGROUND_BUDGET_SPENT",
                    "why": "nobody is working, and background work is held "
                           "to the tighter line so a person's work keeps the "
                           "rest (HERON-KRN-TOK-015): %s"
                           % verdict.get("why", verdict.get("posture")),
                    "unjudged": _unjudged(user_working, how)}

    taken = queue.take(item_id=top["id"])
    return {"run": True, "item": taken["item"],
            "why": "nobody is working, so %s may run. %s."
                   % (_name(top["priority"]), how),
            "unjudged": _unjudged(user_working, how)}


def _name(priority):
    import heron_queue as QUE
    return QUE.PRIORITIES[priority]


def _unjudged(user_working, how):
    found = []
    if user_working:
        found.append(
            "WHETHER SOMEBODY IS REALLY WORKING is only as good as the "
            "reader: %s. Nothing here can see a person directly, and a "
            "reader that always says no would turn this whole agent off "
            "without changing a line of it." % how)
    found.append(
        "HOW LONG the chosen work will take is not known and is not asked "
        "for. A scheduler that estimated would be guessing at the one number "
        "that decides whether it should have started at all.")
    return found


def main(argv):
    import heron_queue as QUE
    import heron_budget as TOK

    print("BACKGROUND SCHEDULER   user work always wins, and silence is yes")
    print("=" * 72)

    def fresh():
        queue = QUE.Queue()
        queue.add("answer the user", 0, "the host", "somebody asked")
        queue.add("re-index the library", 4, "the learning loop",
                  "343 fragments moved")
        queue.add("tidy the audit log", 6, "maintenance", "it is 40 MB")
        return queue

    print("  A P0 is waiting:")
    answer = decide(fresh(), busy=lambda: True)
    print("    somebody IS working   -> %s  %s"
          % ("run" if answer["run"] else answer["refused"],
             answer["item"]["work"] if answer["run"] else ""))
    print("      %s" % answer["why"][:96])

    print()
    print("  Only background work is waiting:")

    def background_only():
        queue = QUE.Queue()
        queue.add("re-index the library", 4, "the learning loop", "moved")
        queue.add("tidy the audit log", 6, "maintenance", "40 MB")
        return queue

    for label, busy in [("somebody IS working", lambda: True),
                        ("nobody is working", lambda: False),
                        ("NO reader at all", None),
                        ("a reader that raises",
                         lambda: 1 / 0),
                        ("a reader that answers None", lambda: None),
                        ("a value instead of a reader", False)]:
        answer = decide(background_only(), busy=busy)
        print("    %-28s -> %s"
              % (label, "run: %s" % answer["item"]["work"]
                 if answer["run"] else answer["refused"]))
        print("        %s" % answer["why"][:92])

    print()
    print("  Nobody is working, but the background budget is spent:")
    budget = TOK.Budget()
    budget.set_budget("background", 20, "calls")
    budget.record("background", 20, "calls", "a provider")
    answer = decide(background_only(), busy=lambda: False, budget=budget)
    print("    %-28s -> %s" % ("the ceiling holds",
                               answer.get("refused", "run")))
    print("        %s" % answer["why"][:92])

    print()
    print("  FOUR of those six READERS ANSWERED 'somebody is working' without")
    print("  anybody saying so: no reader, a raising reader, a None, and a")
    print("  value passed where a reader belongs. That is the design. A")
    print("  scheduler that cannot see the person does not get to use their")
    print("  machine, and the convenient answer is never the default.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
