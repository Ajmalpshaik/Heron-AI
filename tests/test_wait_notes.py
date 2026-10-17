#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The bridge client says the wait goes on - and never at the answer's cost.

    python tests/test_wait_notes.py

Runs without Revit, without the MCP SDK and on any system: the pipe is a
stand-in, and everything above it is the client's own Bridge.request and
_read_line.

WHAT IT HOLDS (docs/04 section 7.1). A caller that passes `on_wait` is called
every WAIT_NOTE_S while the answer is awaited, with the seconds waited, and
never once the answer or the deadline has come. A callback that raises is
dropped for the rest of that read, and the read still returns the answer. A
caller that passes none waits exactly as before. And nothing about the wait
goes down the pipe: one request is one line, as it was - a heartbeat would be
a protocol change, PROTOCOL_VERSION and all.

The server's half - that a host which sent a progressToken is told, and one
that sent none is not - is tests/test_mcp_stdio.py's, over real stdio.
"""

import json
import os
import sys
import threading
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
import heron_bridge_client as bridge                          # noqa: E402

FAILURES = []

# Short, so the suite takes about a second. The client reads the name when it
# waits, so setting it here is the same loop at another pace.
EVERY_S = 0.05
REPLY = {"ok": True, "document": "stand-in"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class Pipe(object):
    """A pipe whose Revit answers after `seconds`, or never when None."""

    def __init__(self, seconds):
        self.seconds = seconds
        self.sent = []
        self.answered_at = None
        self._closed = threading.Event()

    def write(self, data):
        self.sent.append(data)
        return len(data)

    def read(self, _size):
        if self.seconds is None:
            self._closed.wait(5)
            return b""
        time.sleep(self.seconds)
        self.answered_at = time.time()
        return (json.dumps(REPLY) + "\n").encode("utf-8")

    def close(self):
        self._closed.set()


class StandIn(bridge.Bridge):
    """The client's own Bridge, with only the pipe replaced."""

    def __init__(self, pipe):
        super(StandIn, self).__init__(
            {"pid": 4242, "pipeName": "heron-wait-notes", "revitVersion": "2024",
             "protocolVersion": bridge.PROTOCOL_VERSION, "token": "stand-in"}, None)
        self.pipe = pipe

    def _connect(self, timeout):
        return self.pipe


def heard():
    calls = []

    def on_wait(waited):
        calls.append((time.time(), waited))
    return calls, on_wait


def main():
    print("The bridge client, waiting on a Revit that takes its time")
    interval = getattr(bridge, "WAIT_NOTE_S", None)
    check(interval is not None,
          "the client has an interval for saying the wait goes on (WAIT_NOTE_S = %s)" % interval)
    takes = getattr(bridge.Bridge.request, "__code__", None)
    check(takes is not None and "on_wait" in takes.co_varnames[:takes.co_argcount],
          "Bridge.request takes on_wait")
    if FAILURES:
        # Asked before it is called (heron-ship 2a): an old client would raise
        # TypeError below, and one traceback would replace every check.
        print()
        print("FAILED - %d check(s), and the rest cannot be asked of this client." % len(FAILURES))
        return 1
    bridge.WAIT_NOTE_S = EVERY_S

    print()
    print("  a caller that asked to be told")
    pipe = Pipe(12 * EVERY_S)
    calls, on_wait = heard()
    reply = StandIn(pipe).request("run_fragment_read", response_timeout=5.0, on_wait=on_wait)
    check(reply == REPLY, "the answer arrives, unchanged: %s" % reply)
    least = 12 // 2
    check(least <= len(calls) <= 12,
          "it is told every WAIT_NOTE_S while it waits: %d calls in 12 intervals" % len(calls))
    waited = [w for _, w in calls]
    check(bool(waited) and all(b > a for a, b in zip(waited, waited[1:])),
          "with the seconds waited, rising: %s" % ["%.2f" % w for w in waited])
    check(bool(calls) and pipe.answered_at is not None
          and all(at <= pipe.answered_at + EVERY_S for at, _ in calls),
          "and never after the answer")
    check(len(pipe.sent) == 1 and pipe.sent[0].count(b"\n") == 1,
          "one request is one line down the pipe, as before - the wait sends nothing")
    sent = json.loads(pipe.sent[0].decode("utf-8")) if pipe.sent else {}
    check(not any("wait" in key.lower() for key in sent),
          "and the request carries nothing about it: %s" % sorted(sent))

    print()
    print("  a callback that fails")
    pipe = Pipe(8 * EVERY_S)
    tries = []

    def broken(waited):
        tries.append(waited)
        raise RuntimeError("the host went away")
    reply = StandIn(pipe).request("run_fragment_read", response_timeout=5.0, on_wait=broken)
    check(reply == REPLY, "costs the read nothing: the answer still arrives")
    check(len(tries) == 1, "and is dropped after its first fault, not called again: %d call(s)"
          % len(tries))

    print()
    print("  a caller that did not ask")
    pipe = Pipe(4 * EVERY_S)
    reply = StandIn(pipe).request("run_fragment_read", response_timeout=5.0)
    check(reply == REPLY, "waits exactly as before, and gets the answer")

    print()
    print("  a Revit that never answers")
    pipe = Pipe(None)
    calls, on_wait = heard()
    deadline = 10 * EVERY_S
    started = time.time()
    # idempotent=False: one attempt, so the deadline is one read's.
    reply = StandIn(pipe).request("run_fragment_write", response_timeout=deadline,
                                  idempotent=False, on_wait=on_wait)
    took = time.time() - started
    check(isinstance(reply, dict) and reply.get("error") == "unknown_outcome",
          "the deadline still decides, and a lost write is still unknown_outcome: %s"
          % (reply or {}).get("error"))
    check(took < deadline + 1.0, "it gives up at the deadline, not later (%.2f s of %.2f)"
          % (took, deadline))
    check(all(at <= started + deadline + EVERY_S for at, _ in calls),
          "and is never told the wait goes on past the deadline (%d calls)" % len(calls))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1
    print("PASSED - the client says the wait goes on, never past the answer or")
    print("the deadline, never at the answer's cost, and sends Revit nothing more.")
    print("That a HOST is told is tests/test_mcp_stdio.py's; that Claude Code shows")
    print("it is NEEDS-CHECKING EJ1.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
