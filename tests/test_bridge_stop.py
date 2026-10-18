#!/usr/bin/env python3
# Heron-Agent:  HERON-OPS-STP-007
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
`stop` goes to ONE Revit - the one named, or the only one connected.

    python tests/test_bridge_stop.py

WHY
---
Row 5b-1094. `heron_bridge_client.py stop` took no session and sent `stop`
to every connected Revit. On 2026-10-18, run with a stranger's
HERON_CLIENT_ID during NEEDS-CHECKING EZ3, Revit 2024 - whose lease the
run held - refused it, as it should; Revit 2020, another chat's and idle,
took it, and Heron there refused every change until that Revit was
restarted. A stop lasts until Revit restarts (D-129), so one sent to the
wrong Revit costs a restart.

This suite holds the client to:

  * `stop --session <pid>` read by the client's own `main` and handed to
    `cmd_stop` - nothing connects and nothing is discovered
  * with two Revits connected and no `--session`, NOTHING sent, and every
    connected Revit listed as a `--session` to choose - the same refusal
    `fragment`, `prove`, `validate` and `try-once` give
  * with `--session`, the stop reaches that Revit and no other
  * a session that is not connected refused, nothing sent
  * one Revit connected and no `--session` still stopped - the case D-129
    was built for, a second terminal beside the chat that holds it
  * the lease handed back only when this run took it

WHAT IT CANNOT SAY
------------------
What a Revit does with the stop - who the add-in lets stop an idle Revit
is its rule, not the client's, and it is Q-71. That a real Revit 2020 and
2024 connected side by side take the stop as this suite's fakes do is
NEEDS-CHECKING EZ6.
"""

import contextlib
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-bridge-stop-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                          # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class FakeBridge(object):
    """A connected Revit that keeps every operation it was sent."""

    addin_version = "0.1.0+test"

    def __init__(self, pid, revit_version, reply=None):
        self.pid = pid
        self.revit_version = revit_version
        self.reply = reply or {"ok": True, "stopped": True, "running": False,
                               "message": "Heron is stopped. Nothing was running."}
        self.sent = []
        self.released = False

    def request(self, op, *args, **kwargs):
        self.sent.append(op)
        return dict(self.reply)

    def release(self):
        self.released = True

    def close(self):
        pass


def parse(*tokens):
    """The client's own main, with cmd_stop replaced. Returns (code, calls, out)."""
    calls = []

    def capture(*args, **kwargs):
        calls.append((args, kwargs))
        return 0

    real, CLIENT.cmd_stop = CLIENT.cmd_stop, capture
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            code = CLIENT.main(["heron_bridge_client.py", "stop"] + list(tokens))
    finally:
        CLIENT.cmd_stop = real
    return code, calls, out.getvalue()


def session_of(call):
    """The session one captured cmd_stop call was given, however it was passed."""
    args, kwargs = call
    if "session" in kwargs:
        return kwargs["session"]
    return args[0] if args else None


def stop(bridges, session=None, client_id=None):
    """cmd_stop against fake Revits. Returns (code, out)."""
    saved = (CLIENT.refuse_off_windows, CLIENT.discover,
             os.environ.get("HERON_CLIENT_ID"))
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = lambda prune=True: (list(bridges), [], [], [])
    if client_id is None:
        os.environ.pop("HERON_CLIENT_ID", None)
    else:
        os.environ["HERON_CLIENT_ID"] = client_id
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            try:
                code = (CLIENT.cmd_stop(session=session) if session is not None
                        else CLIENT.cmd_stop())
            except TypeError as error:
                # A client whose stop takes no session is a FAIL below, not a
                # traceback in place of every check (heron-ship 2a).
                code = None
                out.write(u"%s" % error)
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover = saved[0], saved[1]
        if saved[2] is None:
            os.environ.pop("HERON_CLIENT_ID", None)
        else:
            os.environ["HERON_CLIENT_ID"] = saved[2]
    return code, out.getvalue()


def main():
    print("1. `stop --session <pid>` reaches cmd_stop")
    code, calls, out = parse("--session", "63712")
    check(code == 0 and len(calls) == 1,
          "`stop --session 63712` is read, not refused - exit %r, %d call(s)"
          % (code, len(calls)))
    check(calls and str(session_of(calls[0])) == "63712",
          "and cmd_stop is handed session 63712 - got %r"
          % (session_of(calls[0]) if calls else None))
    code, calls, out = parse()
    check(code == 0 and len(calls) == 1 and session_of(calls[0]) is None,
          "plain `stop` still reaches cmd_stop, with no session")
    code, calls, out = parse("--session")
    check(code == 2 and not calls,
          "`--session` with no pid is refused before anything is sent - exit %r"
          % code)
    code, calls, out = parse("63712")
    check(code == 2 and not calls,
          "a bare word after `stop` is refused, never read as a session - exit %r"
          % code)
    check("--session" in out, "and the refusal says how to name one")

    print("2. two Revits and no --session: nothing is sent")
    old = FakeBridge(55496, "2020")
    new = FakeBridge(63712, "2024")
    code, out = stop([old, new])
    check(not old.sent and not new.sent,
          "neither Revit was sent `stop` - sent to 2020: %r, to 2024: %r"
          % (old.sent, new.sent))
    check(code == 2, "the run exits 2, a question for the caller - got %r" % code)
    check("--session 55496" in out and "--session 63712" in out,
          "both connected Revits are listed to choose from")
    check("Nothing was sent" in out, "and it says no Revit was stopped")

    print("3. --session: the stop reaches that Revit and no other")
    old = FakeBridge(55496, "2020")
    new = FakeBridge(63712, "2024")
    code, out = stop([old, new], session="63712")
    check(new.sent == ["stop"], "Revit 2024 (63712) was sent `stop` - %r" % new.sent)
    check(not old.sent, "Revit 2020 (55496) was sent nothing - %r" % old.sent)
    check(code == 0, "exit 0 - got %r" % code)
    check("Heron is stopped" in out and "63712" in out,
          "the Revit's own answer is printed beside the session it came from")

    print("4. a session that is not connected is refused")
    old = FakeBridge(55496, "2020")
    new = FakeBridge(63712, "2024")
    code, out = stop([old, new], session="11111")
    check(not old.sent and not new.sent, "nothing was sent to either Revit")
    check(code == 2, "exit 2 - got %r" % code)
    check("11111" in out and "55496" in out and "63712" in out,
          "the refusal names the session asked for and the ones connected")

    print("5. one Revit and no --session: it is stopped, as before")
    only = FakeBridge(63712, "2024")
    code, out = stop([only])
    check(only.sent == ["stop"], "the one connected Revit was sent `stop` - %r" % only.sent)
    check(code == 0, "exit 0 - got %r" % code)
    only = FakeBridge(63712, "2024")
    code, out = stop([only], session="63712")
    check(only.sent == ["stop"] and code == 0,
          "and naming it with --session does the same")

    print("6. the lease is handed back only when this run took it")
    only = FakeBridge(63712, "2024")
    stop([only])
    check(only.released, "no HERON_CLIENT_ID: the lease this stop claimed is released")
    only = FakeBridge(63712, "2024")
    stop([only], client_id="ez6-holder")
    check(not only.released,
          "HERON_CLIENT_ID set: the chat's own lease is left alone, mid-job")

    print("7. a Revit that refuses the stop is reported, and the run fails")
    held = FakeBridge(63712, "2024", reply={
        "ok": False, "error": "session_in_use",
        "message": "This Revit is in use by another chat."})
    code, out = stop([held], session="63712")
    check(code == 1, "exit 1 - got %r" % code)
    check("in use by another chat" in out, "the refusal is printed as Revit gave it")

    print()
    if FAILURES:
        print("%d check(s) FAILED" % len(FAILURES))
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
