# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-EVT-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The event bus - it notifies, it does not act, and it never loses a failure.

    python tests/test_events.py

WHAT IT PROVES
  1. HANDLERS RUN IN SUBSCRIPTION ORDER, and the order is reported. "Whatever
     order the dictionary gives" is a bug that only appears once two handlers
     matter to each other.

  2. A HANDLER THAT RAISES DOES NOT STOP THE OTHERS, and the reason is kept.
     Golden Rule 14 - a swallowed handler failure is the one piece of evidence
     nothing else can reconstruct.

  3. PUBLISHING NEVER RAISES AT THE PUBLISHER. The publisher is reporting
     something that already happened. A broken listener cannot be allowed to
     become the publisher's problem.

  4. A HANDLER DECLARING MORE THAN ANALYZE IS REFUSED AT SUBSCRIBE TIME. This
     is the rule docs/28 states in six words, and the reason it matters is
     that an event handler runs with no preview and no undo step of its own.

  5. EACH HANDLER GETS ITS OWN COPY OF THE PAYLOAD. Two handlers sharing one
     dictionary makes the second one's input depend on the first one's
     tidiness.

  6. A CYCLE IS STOPPED AND NAMED, not followed until the process dies.

  7. A HANDLER MAY PUBLISH A DIFFERENT EVENT. Stopping cycles must not mean
     stopping the thing cycles are a misuse of.

  8. AN UNKNOWN RISK LEVEL IS REFUSED, rather than treated as the safe one.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_events as EVENTS                                 # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    print("1 and 2. Order is kept, and a failure does not stop the rest")
    bus = EVENTS.Bus()
    ran = []
    bus.subscribe("revit.lost", lambda p: ran.append("first"), "first")

    def broken(payload):
        raise RuntimeError("the log file was locked")

    bus.subscribe("revit.lost", broken, "broken")
    bus.subscribe("revit.lost", lambda p: ran.append("third"), "third")

    result = bus.publish("revit.lost", {"session": "2024"})
    check(result["order"] == ["first", "broken", "third"],
          "handlers ran in the order they subscribed, and it is reported")
    check(ran == ["first", "third"],
          "the handler after the broken one still ran")
    check(result["delivered"] == 2, "two of three delivered")
    check(len(result["failed"]) == 1
          and "the log file was locked" in result["failed"][0][1],
          "the failure is kept with the reason it gave")
    check(result["failed"][0][0] == "broken",
          "and with the name of the handler that gave it")

    print()
    print("3. The publisher is never handed somebody else's exception")
    raised = False
    try:
        bus.publish("revit.lost", {})
    except Exception:                                        # noqa: BLE001
        raised = True
    check(not raised, "publishing an event with a broken listener does not raise")

    print()
    print("4. A handler may notify. It may not act")
    for risk in ("MODIFY", "EXECUTE", "PUBLISH", "ADMIN", "SUGGEST"):
        refused = False
        try:
            bus.subscribe("x", lambda p: None, "actor-%s" % risk, risk=risk)
        except PermissionError:
            refused = True
        check(refused, "a handler declaring %s is refused at subscribe time"
              % risk)
    allowed = True
    try:
        bus.subscribe("x", lambda p: None, "reader", risk="READ")
        bus.subscribe("x", lambda p: None, "analyst", risk="ANALYZE")
    except PermissionError:
        allowed = False
    check(allowed, "READ and ANALYZE handlers are allowed")

    print()
    print("5. One handler cannot change what the next one sees")
    bus = EVENTS.Bus()
    seen = []
    bus.subscribe("job.done", lambda p: p.pop("count", None), "greedy")
    bus.subscribe("job.done", lambda p: seen.append(dict(p)), "watcher")
    bus.publish("job.done", {"count": 126, "model": "Tower-A.rvt"})
    check(seen and seen[0].get("count") == 126,
          "the second handler still sees what the first one deleted")

    print()
    print("6 and 7. A cycle stops; a different event does not")
    bus = EVENTS.Bus()
    depth = []

    def republish(payload):
        depth.append(1)
        bus.publish("loop.me", payload)

    bus.subscribe("loop.me", republish, "republisher")
    result = bus.publish("loop.me", {})
    check(len(depth) == 1, "the handler ran once, not until the stack died")
    check(bus.history and any("EVENT_CYCLE" in str(entry)
                              for entry in bus.history),
          "the cycle is recorded by name, not silently dropped")

    bus = EVENTS.Bus()
    chained = []
    bus.subscribe("a.happened",
                  lambda p: bus.publish("b.happened", p), "chain")
    bus.subscribe("b.happened", lambda p: chained.append("b"), "listener")
    bus.publish("a.happened", {})
    check(chained == ["b"], "a handler may publish a DIFFERENT event")

    print()
    print("8. An unknown risk level is refused, not assumed safe")
    refused = False
    try:
        bus.subscribe("z", lambda p: None, "mystery", risk="PROBABLY_FINE")
    except ValueError:
        refused = True
    check(refused, "a risk level nobody has defined is refused")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the bus notifies, keeps order, and loses nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
