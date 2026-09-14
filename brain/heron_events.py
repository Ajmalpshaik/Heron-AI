# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-EVT-004
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Event Bus - how one part of Heron tells another without depending on it.

    python brain/heron_events.py        a worked example, and what it refuses

WHAT IT IS FOR (docs/23 s1)
---------------------------
"Every module communicates through the Kernel instead of directly depending on
every other module." Without a bus, the module that notices something has to
know who cares - so the thing that detects a lost Revit imports the thing that
writes the log, the thing that pauses the queue, and the thing that tells the
user. Three imports, and the fourth listener cannot be added without editing
the detector.

With a bus, the detector says `revit.lost` and stops caring.

THE RULE FROM THE REGISTER: HANDLERS NOTIFY, NEVER MODIFY
----------------------------------------------------------
docs/28 states it in six words - "Handlers notify, never perform MODIFY
directly" - and this module enforces it rather than repeating it. A subscriber
declares its risk level, and one declaring anything above ANALYZE is refused at
subscribe time, with the reason.

It matters because of what an event is: something that ALREADY HAPPENED, told
to whoever is listening. A handler that changes a model turns a notification
into an action nobody asked for, running under whatever permission the
publisher happened to hold, with no preview and no undo step of its own - every
safety rail in docs/14 bypassed by a subscription. A handler that wants a model
changed asks for it through the workflow, where the gates are.

THREE THINGS A BUS GETS WRONG IF NOBODY DECIDES THEM
-----------------------------------------------------
  ORDER      handlers run in subscription order, and the order is reported.
             "Whatever the dictionary gives" is a bug that appears the day two
             handlers start to matter to each other.
  FAILURE    a handler that raises does NOT stop the rest, and is recorded.
             Golden Rule 14: never silently discard - a swallowed handler
             failure is the one kind of evidence nothing else can reconstruct.
  RECURSION  a handler may publish, but an event that comes back round to
             itself is stopped at the cycle and reported. An unbounded bus
             takes the whole process down, and it does it in production.

Each handler is given its OWN COPY of the payload. Two handlers sharing one
dictionary means handler 2's input depends on handler 1's tidiness, which is a
dependency the bus exists to remove.
"""

import copy
import sys

# The risk levels from docs/12, lowest first. A handler may hold one of the
# first two: it may look at things and work things out. It may not act.
RISK_ORDER = ("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH",
              "ADMIN")
MAY_HANDLE = ("READ", "ANALYZE")


class Bus(object):
    """
    Subscriptions and delivery. One bus per process; never a global, because a
    test that cannot get a clean bus ends up asserting against whatever the
    last test subscribed.
    """

    def __init__(self):
        self._handlers = {}          # event -> [(name, risk, callable)]
        self._in_flight = []         # the publish chain, for cycle detection
        self._cycles = []            # cycles found inside the current chain
        self.history = []            # (event, delivered, failed) per publish

    # ------------------------------------------------------------ subscribe
    def subscribe(self, event, handler, name, risk="READ"):
        """
        Register a handler. Returns its name; raises PermissionError for a
        handler that declares it may do more than notify.
        """
        risk = (risk or "READ").upper()
        if risk not in RISK_ORDER:
            raise ValueError(
                "handler '%s' declares risk '%s', which is not one of: %s"
                % (name, risk, ", ".join(RISK_ORDER)))
        if risk not in MAY_HANDLE:
            raise PermissionError(
                "HANDLER_MAY_NOT_MODIFY: handler '%s' declares %s. An event "
                "handler notifies and does "
                "not act (docs/28, HERON-KRN-EVT-004). Ask for the change "
                "through the workflow, where the preview and the undo step "
                "are." % (name, risk))
        self._handlers.setdefault(event, []).append((name, risk, handler))
        return name

    def subscribers(self, event):
        """The handler names for an event, in the order they will run."""
        return [name for name, _risk, _fn in self._handlers.get(event, [])]

    # -------------------------------------------------------------- publish
    def publish(self, event, payload=None):
        """
        Tell everyone listening. Returns {delivered, failed, order}.

        Never raises on a handler's behalf. The publisher is reporting
        something that already happened; it cannot be the publisher's problem
        that a listener is broken, and a raise here would make it one.
        """
        if event in self._in_flight:
            chain = " -> ".join(self._in_flight + [event])
            result = {"delivered": 0, "order": [],
                      "failed": [(None, "EVENT_CYCLE: %s" % chain)]}
            # RECORDED FOR THE PUBLISHER THAT STARTED THE CHAIN, not only for
            # whoever republished. A handler that ignores the return value -
            # most do - left the original publisher told `delivered: 1,
            # failed: []` while a delivery had in fact been stopped, and the
            # only place that said otherwise was the history.
            self._cycles.append(chain)
            self.history.append((event, 0, result["failed"]))
            return result

        root = not self._in_flight
        if root:
            self._cycles = []

        delivered, failed, order = 0, [], []
        self._in_flight.append(event)
        try:
            for name, _risk, handler in list(self._handlers.get(event, [])):
                order.append(name)
                try:
                    handler(copy.deepcopy(payload) if payload else {})
                    delivered += 1
                except Exception as exc:                     # noqa: BLE001
                    # Deliberately broad. A handler is somebody else's code,
                    # and the one thing this bus must never do is let one
                    # listener stop the others - or lose the reason why.
                    failed.append((name, "HANDLER_FAILED: %s: %s"
                                   % (type(exc).__name__, exc)))
        finally:
            self._in_flight.pop()

        if root and self._cycles:
            for chain in self._cycles:
                failed.append((None, "EVENT_CYCLE: %s" % chain))
            self._cycles = []

        # HISTORY KEEPS ITS OWN LIST. Handing the caller the same object
        # meant a publisher that cleared its result also erased the only
        # persistent account this bus keeps of a stopped or broken delivery.
        self.history.append((event, delivered, list(failed)))
        return {"delivered": delivered, "failed": failed, "order": order}


def main(argv):
    bus = Bus()
    seen = []

    bus.subscribe("revit.lost", lambda p: seen.append("log"), "log")
    bus.subscribe("revit.lost", lambda p: seen.append("pause queue"),
                  "queue", risk="ANALYZE")

    def broken(payload):
        raise RuntimeError("the log file was locked")

    bus.subscribe("revit.lost", broken, "broken-listener")
    bus.subscribe("revit.lost", lambda p: seen.append("tell the user"),
                  "reply")

    print("EVENT BUS   one event, four listeners, one of them broken")
    print("=" * 67)
    result = bus.publish("revit.lost", {"session": "2024 / pid 8123"})
    print("  order      %s" % ", ".join(result["order"]))
    print("  delivered  %d" % result["delivered"])
    for name, why in result["failed"]:
        print("  failed     %s - %s" % (name, why))
    print("  ran        %s" % ", ".join(seen))

    print()
    print("  A handler that declares it may change a model:")
    try:
        bus.subscribe("revit.lost", lambda p: None, "mover", risk="MODIFY")
        print("  FAIL       it was allowed, and must not be")
        return 1
    except PermissionError as exc:
        print("  refused    %s" % exc)

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
