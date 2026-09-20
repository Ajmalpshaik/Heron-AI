#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The session binding - one chat, one Revit. Runs without Revit.

This is the mechanism that stops Heron finishing a job in the wrong project,
so it is tested as four separate cases rather than one happy path. The table
comes from docs/25, and the distinction it turns on was found by running the
earlier system, not by reasoning about it:

    An assumption is not a choice.

    How it was bound   What changed            What must happen
    ----------------   --------------------    ------------------------------
    ASSUMED            a second Revit appears  ASK. They never chose this one
    ASSUMED            it closed               quietly take the remaining one
    CHOSEN             more Revits appear      keep it. Do not nag
    CHOSEN             it closed               STOP. Never slide onto another

The last row is the dangerous one. A naive "sticky" binding re-runs discovery
when its session disappears and silently rebinds to whatever is left - and the
user's next command lands on someone else's model.

    python tests/test_session_binding.py
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

from heron_session import (SessionBinding, NotBound,
                           version_for_filter, NOTHING_CONNECTED)      # noqa: E402
import heron_bridge_client as bridge                   # noqa: E402


class FakeSession(object):
    """
    A connected Revit, without a Revit.

    It answers `info` because the picker now asks each session whether it is
    free or already held by another chat (Step 6's lease). `held_by_other`
    stages that answer. A fake that cannot answer what the real thing answers
    is not a fake, it is a different object.
    """

    def __init__(self, pid, revit_version, held_by_other=False):
        self.pid = pid
        self.revit_version = revit_version
        self.held_by_other = held_by_other

    def request(self, op, **kwargs):
        if op == "info":
            return {"ok": True, "inUse": self.held_by_other, "mine": False,
                    "leaseSecondsRemaining": 300 if self.held_by_other else 0}
        return {"ok": True}

    def close(self):
        pass


def stage(binding, sessions):
    """Replace what discovery sees, so the world can change between calls."""
    binding.sessions = lambda: (sessions, [], [])


def main():
    a = FakeSession(1111, "2024")
    b = FakeSession(2222, "2020")
    failures = []

    def check(condition, message):
        if condition:
            print("  PASS  %s" % message)
        else:
            failures.append(message)

    # --- refuses to guess ---------------------------------------------------
    binding = SessionBinding()
    stage(binding, [a, b])
    try:
        binding.resolve()
        failures.append("two connected and nothing chosen: it guessed instead of asking")
    except NotBound as unbound:
        text = str(unbound)
        check("not safe to guess" in text and "1)" in text and "2)" in text,
              "two connected, none chosen -> asks, with a numbered list")
        check("Nothing has been sent to Revit" in text,
              "the refusal says nothing was sent")

    # --- an explicit choice sticks -----------------------------------------
    chosen = binding.choose("1")
    check(chosen.pid == a.pid and binding.was_chosen,
          "choosing by number binds, and is recorded as a CHOICE")

    stage(binding, [a, b])
    check(binding.resolve().pid == a.pid,
          "CHOSEN + others still open -> keeps the choice, does not nag")

    # --- THE DANGEROUS ONE: chosen session closes ---------------------------
    stage(binding, [b])
    try:
        landed = binding.resolve()
        failures.append(
            "CHOSEN session closed and it slid onto Revit %s (session %s) - "
            "this is the wrong-model failure" % (landed.revit_version, landed.pid))
    except NotBound as unbound:
        text = str(unbound)
        check("has closed" in text and "stopped" in text,
              "CHOSEN session closes -> STOPS, never slides onto another model")
        check("Nothing has been sent to Revit" in text,
              "and says nothing was sent")

    # --- an assumption is not a choice --------------------------------------
    binding = SessionBinding()
    stage(binding, [a])
    check(binding.resolve().pid == a.pid and not binding.was_chosen,
          "one connected -> used without asking, recorded as ASSUMED")

    stage(binding, [b])
    check(binding.resolve().pid == b.pid,
          "ASSUMED session closes -> quietly takes the remaining one")

    binding = SessionBinding()
    stage(binding, [a])
    binding.resolve()
    stage(binding, [a, b])
    try:
        binding.resolve()
        failures.append(
            "ASSUMED, then a second Revit appeared, and it kept using the first one silently - "
            "the exact bug this distinction exists to prevent")
    except NotBound as unbound:
        check("no longer safe to assume" in str(unbound),
              "ASSUMED + a second appears -> ASKS, because they never chose")

    # --- the picker tells the truth about other chats (Step 6's lease) ------
    binding = SessionBinding()
    stage(binding, [FakeSession(100, "2024"), FakeSession(200, "2020", held_by_other=True)])
    picker = binding.describe(binding.sessions()[0])
    check("(free)" in picker,
          "the picker marks a Revit nobody is using as (free)")
    check("in use by another chat" in picker,
          "and says plainly when another chat already holds one")

    # --- the availability phrase itself, all four states ---------------------
    class Reply(object):
        def __init__(self, payload):
            self.payload = payload
        def request(self, op, **kwargs):
            return self.payload

    check(bridge.availability(Reply({"ok": True, "inUse": False, "mine": False,
                                     "leaseSecondsRemaining": 0})) == "(free)",
          "nobody holding it reads as (free)")
    check("this chat" in bridge.availability(Reply({"ok": True, "inUse": True, "mine": True,
                                                    "leaseSecondsRemaining": 120})),
          "held by me is said differently from held by someone else")
    check("another chat" in bridge.availability(Reply({"ok": True, "inUse": True, "mine": False,
                                                       "leaseSecondsRemaining": 120})),
          "held by another chat says so, with roughly how long is left")
    check(bridge.availability(Reply({"ok": True})) == "(availability unknown)",
          "an older add-in that cannot report it says UNKNOWN, never (free)")

    # --- nothing connected --------------------------------------------------
    binding = SessionBinding()
    stage(binding, [])
    try:
        binding.resolve()
        failures.append("nothing connected: it returned a session anyway")
    except NotBound as unbound:
        check("No Revit is connected" in str(unbound),
              "nothing connected -> says so, and which button to press")

    print()
    print("6. the version wall says WHICH case, and never slides")
    # FRAGMENT-ISSUES row 130: a modeller with two Revits open was told none
    # was connected, because `_revit_version` returned (None, None) for
    # "nothing connected" AND for "several connected, none chosen", and its
    # caller printed the first sentence for both. Measured with sessions
    # 20472 and 36908 live.
    #
    # THIS IS TESTED HERE AND NOT BESIDE ITS CALLER because heron_session
    # imports no MCP SDK - CI cannot import heron_mcp_server at all, so a
    # test living there would never run on the machine that matters.
    a, b = FakeSession(20472, "2020"), FakeSession(36908, "2024")

    release, how = version_for_filter([], None, False)
    check(release is None and how == NOTHING_CONNECTED,
          "nothing connected -> no release, and it says so")

    release, how = version_for_filter([a, b], None, False)
    check(release is None and "2 Revits are connected" in how
          and "none has been chosen" in how,
          "two connected and none chosen -> a DIFFERENT sentence, with the "
          "count in it: %r" % how)
    check(how != NOTHING_CONNECTED,
          "and it is never the nothing-connected one, which is row 130")

    release, how = version_for_filter([a, b], 36908, True)
    check(release == "2024" and how == "chosen",
          "a chosen session decides the release, and says it was chosen")
    release, how = version_for_filter([a, b], 36908, False)
    check(release == "2024" and how == "assumed, not chosen",
          "and an assumption is still named as one")

    release, how = version_for_filter([a], None, False)
    check(release == "2020" and how == "the only Revit connected",
          "one connected -> used, and named as the assumption it is")

    # THE DANGEROUS ROW OF THIS FILE'S OWN TABLE, at the version wall.
    # CHOSEN + it closed -> STOP. Never slide onto another. The old code
    # slid: with the chosen session gone and one other live, it returned
    # THAT one's release as "the only Revit connected".
    release, how = version_for_filter([a], 36908, True)
    check(release is None and "you chose" in how and "has closed" in how,
          "a CHOSEN Revit that has closed stops the wall rather than "
          "sliding onto the survivor: %r" % how)
    check(release != "2020",
          "and the survivor's release is NOT used - that is the slide")

    # ASSUMED + it closed -> quietly take the remaining one. Same table,
    # different row, and the two must not be collapsed.
    release, how = version_for_filter([a], 36908, False)
    check(release == "2020" and how == "the only Revit connected",
          "an ASSUMED session that closed does quietly take the remaining "
          "one - the other row of the same table")

    print()
    if failures:
        print("FAILED")
        for f in failures:
            print("  - %s" % f)
        return 1

    print("Step 5 PASSED - one chat, one Revit, and it fails closed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
