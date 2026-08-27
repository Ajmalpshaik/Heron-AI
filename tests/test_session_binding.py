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

from heron_session import SessionBinding, NotBound      # noqa: E402


class FakeSession(object):
    """A connected Revit, without a Revit."""

    def __init__(self, pid, revit_version):
        self.pid = pid
        self.revit_version = revit_version

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
    if failures:
        print("FAILED")
        for f in failures:
            print("  - %s" % f)
        return 1

    print("Step 5 PASSED - one chat, one Revit, and it fails closed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
