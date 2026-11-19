#!/usr/bin/env python3
# Heron-Agent:  HERON-OPS-STP-007
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A stop names the ONE Revit session it is for, or nothing is stopped.

    python tests/test_bridge_stop.py

WHY
---
Row 5b-1094. `heron_bridge_client.py stop` took no session and sent `stop`
to every connected Revit. On 2026-10-18, run with a stranger's
HERON_CLIENT_ID during NEEDS-CHECKING EZ3, Revit 2024 - whose lease the
run held - refused it, as it should; Revit 2020, another chat's and idle,
took it, and Heron there refused every change until that Revit was
restarted. A stop lasts until Revit restarts (D-129, D-130), so one sent
to the wrong Revit costs a restart.

Q-71, decided 2026-11-15 on the owner's delegation: A STOP MUST NAME THE
EXACT REVIT SESSION IT IS FOR. A stop becomes a choice, never a broadcast
that silently switches off another chat's idle Revit until it restarts.
The add-in half was built 2026-11-16: the operation is `stop_session`,
carrying the Revit's own session (release and process id), and the add-in
stops only when that session is its own. The old name, `stop`, named
nothing, and the add-in now refuses it.

This suite holds the client to:

  * `stop --session <pid>` read by the client's own `main` and handed to
    `cmd_stop` - nothing connects and nothing is discovered
  * no `--session`: NOTHING sent, whether one Revit is connected or two,
    and every connected Revit listed as a `--session` to choose
  * with `--session`, the stop reaches that Revit and no other, as
    `stop_session` carrying that Revit's own session - never as `stop`
  * a session that is not connected refused, nothing sent
  * the lease handed back only when this run took it
  * an add-in from before the change - which stops on `stop` from anyone
    and knows no `stop_session` - is never stopped by it: its unknown_op
    is said as "older", nothing stopped, and the run fails

and the add-in to, read from its source (no Revit here):

  * `Dispatch` answers `stop` and `stop_session` before the queue, both
    in `AnswerStop`, which is handed the request
  * `AnswerStop` refuses before `HeronStop.Stop()` - a `stop` naming no
    session, and a `stop_session` whose session is not this Revit's own,
    compared exactly - records the refusal, and says nothing was stopped
  * `stop_session` declared Read in the registry, and both refusals
    classified in heron_failure as refused, nothing changed

WHAT IT CANNOT SAY
------------------
That a real Revit 2020 and 2024 connected side by side take the stop as
this suite's stand-ins and its reading of the source say they do. That is
NEEDS-CHECKING EZ6 to EZ9, on the owner's PC.
"""

import contextlib
import io
import os
import re
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-bridge-stop-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_bridge_client as CLIENT                          # noqa: E402
import heron_failure as FAILURE                               # noqa: E402

DISPATCHER_CS = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitDispatcher.cs")
REGISTRY_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def in_order(code, *needles):
    """Every needle is there, each after the one before it. `str.find` answers
    -1 for a needle that is missing, which an `a < b` alone reads as first."""
    last = -1
    for needle in needles:
        found = code.find(needle, last + 1)
        if found < 0:
            return False
        last = found
    return True


class FakeBridge(object):
    """A connected Revit that keeps every operation it was sent."""

    addin_version = "0.1.0+test"

    def __init__(self, pid, revit_version, reply=None):
        self.pid = pid
        self.revit_version = revit_version
        self.reply = reply or {"ok": True, "stopped": True, "running": False,
                               "message": "Heron is stopped. Nothing was running."}
        self.sent = []
        self.args = []
        self.released = False

    def request(self, op, *args, **kwargs):
        self.sent.append(op)
        self.args.append(dict(kwargs.get("op_args") or {}))
        return self.answer(op)

    def answer(self, op):
        return dict(self.reply)

    def release(self):
        self.released = True

    def close(self):
        pass


class OldAddin(FakeBridge):
    """
    A Revit running an add-in built before Q-71's change.

    Its rule, as the add-in stood on 2026-11-15: `stop` stops Heron whoever
    sent it and whatever else the request carried; a name its registry does
    not declare is answered by the gate, `unknown_op`, and nothing runs.
    """

    addin_version = "0.1.0+48230c8c"

    def __init__(self, pid, revit_version):
        super(OldAddin, self).__init__(pid, revit_version)
        self.stopped = False

    def answer(self, op):
        if op == "stop":
            self.stopped = True
            return {"ok": True, "stopped": True, "running": False,
                    "message": "Heron is stopped. Nothing was running."}
        return {"ok": False, "error": "unknown_op",
                "message": "Heron has no operation called '%s'. It has: ping, info, "
                           "release, stop, count_elements." % op}


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


def source(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def between(text, start, end):
    """The text from `start` up to `end`, or "" when either is not there."""
    at = text.find(start)
    if at < 0:
        return ""
    stop_at = text.find(end, at + len(start))
    return text[at:stop_at] if stop_at >= 0 else ""


def client():
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
          "plain `stop` still reaches cmd_stop, with no session, to be refused there")
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
          "neither Revit was sent anything - sent to 2020: %r, to 2024: %r"
          % (old.sent, new.sent))
    check(code == 2, "the run exits 2, a question for the caller - got %r" % code)
    check("--session 55496" in out and "--session 63712" in out,
          "both connected Revits are listed to choose from")
    check("Nothing was sent" in out, "and it says no Revit was stopped")

    print("3. --session: the stop reaches that Revit and no other")
    old = FakeBridge(55496, "2020")
    new = FakeBridge(63712, "2024")
    code, out = stop([old, new], session="63712")
    check(new.sent == ["stop_session"],
          "Revit 2024 (63712) was sent `stop_session` - %r" % new.sent)
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

    # UNTIL Q-71 THIS SECTION SAID "it is stopped, as before": one Revit and
    # no --session was sent `stop`. That is the broadcast the decision ended
    # - the one connected Revit can be another chat's, idle - so the check
    # that held it is the one that changed, on the decision's word.
    print("5. one Revit and no --session: nothing is sent, and it is named to choose")
    only = FakeBridge(63712, "2024")
    code, out = stop([only])
    check(not only.sent, "the one connected Revit was sent nothing - %r" % only.sent)
    check(code == 2, "exit 2, a question for the caller - got %r" % code)
    check("--session 63712" in out and "Revit 2024" in out,
          "and it is listed by its session and release, to choose")
    check("Nothing was sent" in out, "and it says no Revit was stopped")
    only = FakeBridge(63712, "2024")
    code, out = stop([only], session="63712")
    check(only.sent == ["stop_session"] and code == 0,
          "naming it with --session stops it - %r, exit %r" % (only.sent, code))

    print("6. the lease is handed back only when this run took it")
    only = FakeBridge(63712, "2024")
    stop([only], session="63712")
    check(only.released, "no HERON_CLIENT_ID: the lease this stop claimed is released")
    only = FakeBridge(63712, "2024")
    stop([only], session="63712", client_id="ez6-holder")
    check(not only.released,
          "HERON_CLIENT_ID set: the chat's own lease is left alone, mid-job")

    print("7. a Revit that refuses the stop is reported, and the run fails")
    held = FakeBridge(63712, "2024", reply={
        "ok": False, "error": "session_in_use",
        "message": "This Revit is in use by another chat."})
    code, out = stop([held], session="63712")
    check(code == 1, "exit 1 - got %r" % code)
    check("in use by another chat" in out, "the refusal is printed as Revit gave it")

    print("8. on the wire the stop names its Revit's own session (Q-71)")
    old = FakeBridge(55496, "2020")
    new = FakeBridge(63712, "2024")
    stop([old, new], session="63712")
    check("stop" not in new.sent,
          "the old name `stop`, which named no Revit, is never sent - %r" % new.sent)
    check(new.args[:1] == [{"session": "2024/63712"}],
          "`stop_session` carries the session as the add-in names itself, "
          "release/process - %r" % new.args)
    old = FakeBridge(55496, "2020")
    new = FakeBridge(63712, "2024")
    stop([old, new], session="55496")
    check(old.sent == ["stop_session"] and old.args[:1] == [{"session": "2020/55496"}]
          and not new.sent,
          "and naming the other Revit sends its own session to it alone - %r %r, 2024 %r"
          % (old.sent, old.args, new.sent))

    print("9. an add-in from before the change is never stopped by it")
    legacy = OldAddin(55496, "2020")
    code, out = stop([legacy], session="55496")
    check(not legacy.stopped,
          "an add-in that stops on `stop` from anyone was not stopped - sent %r"
          % legacy.sent)
    check(code == 1, "the run fails, exit 1 - got %r" % code)
    check("older" in out and "Nothing was stopped" in out,
          "and it says that Revit's Heron is older and nothing was stopped")
    check("Heron has no operation called" not in out,
          "in Revit's words, not the gate's raw list of operations")
    check("55496" in out, "naming the Revit it asked")

    # AND AN OLD ADD-IN WHOSE THREAD A JOB HOLDS QUEUES THE UNKNOWN NAME
    # behind that job, so the gate never sees it: Revit is busy, never
    # stopped. The new add-in answers the stop before the queue and so is
    # never busy for it - `revit_busy` from a stop means an older add-in.
    busy = FakeBridge(55496, "2020", reply={
        "ok": False, "error": "revit_busy",
        "message": "Revit is busy and did not take the request. A dialog may be open, "
                   "or a command may be running."})
    code, out = stop([busy], session="55496")
    check(code == 1 and "older" in out and "Nothing was stopped" in out,
          "a stop queued behind a job by an older add-in is said the same way - exit %r"
          % code)

    print("10. the refusals the add-in gives are printed as it gives them")
    other = FakeBridge(63712, "2024", reply={
        "ok": False, "error": "stop_wrong_session",
        "message": "This stop is for session 2020/55496, and this is Revit 2024, "
                   "session 63712. Nothing was stopped here."})
    code, out = stop([other], session="63712")
    check(code == 1 and "Nothing was stopped here" in out,
          "a stop the Revit refuses as not its own fails the run, in its words - exit %r"
          % code)


def addin():
    print("11. the add-in: Dispatch answers both names before the queue, in AnswerStop")
    text = source(DISPATCHER_CS)
    dispatch = between(text, "public string Dispatch(string request)", "public void Execute(")
    check(bool(dispatch), "RevitDispatcher.Dispatch was found, and Execute after it")
    answered = set(re.findall(r'op\s*==\s*"([A-Za-z_][A-Za-z0-9_]*)"', dispatch))
    check(answered == set(["stop", "stop_session"]),
          "Dispatch answers `stop` and `stop_session` before the queue, and nothing "
          "else - found %s" % (", ".join(sorted(answered)) or "none"))
    check(re.search(r"AnswerStop\(\s*op\s*,\s*request\s*\)", dispatch) is not None,
          "and hands AnswerStop the operation and the request, so it can read what "
          "the stop names")
    check(in_order(dispatch, '"stop_session"', "AnswerStop(", "var raiser = _event"),
          "before the queue - ahead of the ExternalEvent, which a held thread never runs")

    answer = between(text, "private string AnswerStop(", "public void Execute(")
    check(bool(answer), "AnswerStop was found")
    check('Json.ReadString(request, "session")' in answer,
          "it reads the session the stop names off the request")
    check(re.search(r"string\.Equals\(\s*named\s*,\s*_session\s*,\s*"
                    r"StringComparison\.Ordinal\s*\)", answer) is not None,
          "and compares it with this Revit's own session, exactly")
    check(in_order(answer, '"stop_session"', 'Json.ReadString(request, "session")'),
          "the session is read only for `stop_session` - a `stop` names none, "
          "whatever it carries")
    stops_at = answer.find("HeronStop.Stop()")
    check(stops_at > 0 and "Json.Error(" in answer[:stops_at]
          and "Json.Error(" not in answer[stops_at:],
          "every refusal returns BEFORE HeronStop.Stop(), and none after it - nothing "
          "is stopped by one")
    check(answer.count("HeronStop.Stop()") == 1,
          "and HeronStop.Stop() is called in one place - found %d"
          % answer.count("HeronStop.Stop()"))
    check('"stop_unnamed"' in answer and '"stop_wrong_session"' in answer,
          "the two refusals have codes of their own: stop_unnamed, stop_wrong_session")
    check(in_order(answer, "RecordRefusal(", "HeronStop.Stop()"),
          "and a refused stop is recorded in the trail, as every refusal is")
    check("Nothing was stopped" in answer,
          "its sentence says nothing was stopped")
    check("Stop asked over the bridge" not in between(answer, "private string AnswerStop(",
                                                      "HeronStop.Stop()"),
          "and a refused stop does not log the line a stop logs (EZ6 reads the log for it)")

    print("12. the registry and the failure table")
    declared = dict(re.findall(r'\{\s*"([A-Za-z_][A-Za-z0-9_]*)"\s*,\s*HeronRisk\.([A-Za-z]+)\s*\}',
                               source(REGISTRY_CS)))
    check(declared.get("stop_session") == "Read",
          "`stop_session` is declared Read - found %r" % declared.get("stop_session"))
    check(declared.get("stop") == "Read",
          "`stop` stays declared Read, so its refusal is answered before the queue "
          "rather than waiting behind the job a stop is for")
    known = getattr(FAILURE, "_KNOWN", {})
    for code in ("stop_unnamed", "stop_wrong_session"):
        row = known.get(code)
        check(row is not None and row[0] == FAILURE.REFUSED,
              "%s is classified REFUSED - nothing was stopped - found %r" % (code, row))


def main():
    client()
    addin()

    print()
    if FAILURES:
        print("%d check(s) FAILED" % len(FAILURES))
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
