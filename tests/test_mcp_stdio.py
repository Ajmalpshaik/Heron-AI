# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The MCP server as a HOST sees it: a real subprocess, over real stdio.

    python tests/test_mcp_stdio.py

WHY THIS FILE EXISTS, and why test_mcp_serves.py could not have caught it.

That file calls the tools through the SDK's own dispatch, IN THIS PROCESS. It
proves the tools are registered, described and answerable, and on 2026-09-06 it
passed green while `heron_capabilities` was completely unanswerable from a real
host: a Claude Code tool call sat on it for thirty minutes and got nothing.

The difference is one line of stack. In-process, the trained encoder is already
imported or fails instantly. In the server, the first call reached
`import model2vec` -> `import numpy` -> loading numpy's native extension, ON THE
ASYNCIO EVENT LOOP, and the reply never came. That import costs 1.0 s in a fresh
process and was measured still running 40 s later there.

It needed BOTH halves, and neither is a mistake alone: before model2vec was
installed the import failed instantly and Heron degraded to `lexical`, so the
handler always answered. Installing it to prove the search understands meaning
(`A7`) is what made `A8` hang. Two checks, each correct, never run together.

So this file starts the server the way a host does and holds it to a DEADLINE.
A test that waits forever cannot tell a slow answer from no answer, which is
the whole failure being guarded against.

WITH ONE DIFFERENCE FROM A HOST, ON PURPOSE (row 5b-461): the child's
LOCALAPPDATA is a scratch folder, so the Revits it finds are none. A host's
server finds the real ones, and so did this one until 2026-10-14 - 121 opens
of a live Revit's pipe in a run, measured the day before, and still a pass.
tests/support/live_revit_guard.py is installed in the child as well, refuses
any named-pipe open, and fails the run if the real discovery folder was read.

AND A REVIT THAT TAKES ITS TIME - ON ANY SYSTEM, WINDOWS OR NOT. A tool that
sends a fragment can wait minutes: a slow fragment, a family build, a modeller
answering a prompt in Revit. Until 2026-10-17 a host heard nothing in that
time, and could not tell a wait from a hang. tests/support/slow_revit_server.py
serves the real server over stdio in front of a stand-in Revit that answers
after SLOW_S seconds, and a host that sent a progressToken must be told the
wait goes on every WAIT_NOTE_S - while a host that sent none is told
nothing. That part needs only the MCP SDK, so it runs off Windows too; with
no SDK it cannot run, and off Windows that leaves nothing checked: exit 3.
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading
import time

sys.path.insert(0, os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "mcp", "server"))
sys.path.insert(0, os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "mcp", "client"))
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "support"))
from heron_tools import TOOLS                                  # noqa: E402
import heron_bridge_client as bridge                           # noqa: E402
import live_revit_guard as GUARD                               # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
SLOW_SERVER = os.path.join(ROOT, "tests", "support", "slow_revit_server.py")

# How long the stand-in Revit takes to answer: a minute and a half, which a
# host told nothing would sit through in silence. Below the client's fragment
# wait (heron_config.fragment_timeout, never under FRAGMENT_FLOOR_S), so the
# answer comes from the stand-in and never from the client giving up.
SLOW_S = 90

# How often the client says the wait goes on - looked up, not assumed: a
# client too old to say it has no such name, and the checks below must then
# FAIL rather than this line crash (heron-ship 2a).
EVERY_S = getattr(bridge, "WAIT_NOTE_S", None)

# The call that sends no progressToken only has to outlast a few notes.
QUIET_S = 3 * (EVERY_S or 10.0)

COULD_NOT_RUN = 3

# Generous next to the 6 s it takes, tight next to the failure, which was
# unbounded. A number here is a claim about the worst acceptable wait.
DEADLINE = 90

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class Host(object):
    """The smallest thing that behaves like an MCP host over stdio."""

    def __init__(self, env, script=SERVER):
        self.proc = subprocess.Popen(
            [sys.executable, "-u", script], cwd=ROOT, env=env,
            stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1)
        self.err = []
        threading.Thread(target=self._drain, daemon=True).start()

    def _drain(self):
        for line in self.proc.stderr:
            self.err.append(line)

    def send(self, obj):
        self.proc.stdin.write(json.dumps(obj) + "\n")
        self.proc.stdin.flush()

    def read(self, timeout):
        box = {}
        t = threading.Thread(target=lambda: box.__setitem__("l", self.proc.stdout.readline()))
        t.daemon = True
        t.start()
        t.join(timeout)
        return box.get("l")

    def call(self, ident, name, args, timeout):
        t0 = time.time()
        self.send({"jsonrpc": "2.0", "id": ident, "method": "tools/call",
                   "params": {"name": name, "arguments": args}})
        line = self.read(timeout)
        return line, time.time() - t0

    def close(self):
        try:
            self.proc.terminate()
            # Waited for, so the guard's log is complete before it is read
            # and the scratch folder is free to be removed.
            self.proc.wait(10)
        except Exception:
            pass


class Listener(object):
    """Every line a server writes, with when it arrived - read on a thread of
    its own, so two servers can be listened to at once."""

    def __init__(self, host):
        self.lines = []
        self._host = host
        threading.Thread(target=self._read, daemon=True).start()

    def _read(self):
        for line in self._host.proc.stdout:
            try:
                message = json.loads(line)
            except ValueError:
                continue
            self.lines.append((time.time(), message))

    def answer(self, ident, timeout):
        """The response to `ident`, with when it came, or (None, None)."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            for at, message in list(self.lines):
                if message.get("id") == ident and ("result" in message or "error" in message):
                    return at, message
            time.sleep(0.2)
        return None, None

    def progress(self):
        """Every notifications/progress, with when it came."""
        return [(at, m.get("params") or {}) for at, m in list(self.lines)
                if m.get("method") == "notifications/progress"]


def sdk_missing():
    """Why the MCP SDK cannot be imported here, or None when it can.

    BaseException, for test_mcp_serves.sdk_version's reason: a half-broken
    install raises a panic out of a native extension, not an ImportError."""
    try:
        import mcp                                              # noqa: F401
    except BaseException as exc:                                # noqa: BLE001
        return "%s: %s" % (type(exc).__name__, exc)
    return None


def first_read_capability():
    """A capability revit_read will send: the first fragment declaring risk
    READ that has code behind it. Read from the cards, so no name is typed
    here that a rename would leave behind."""
    folder = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(folder)):
        card = os.path.join(folder, name, "fragment.yaml")
        code = os.path.join(folder, name, "impl", "any", "fragment.cs")
        if not (os.path.isfile(card) and os.path.isfile(code)):
            continue
        seen = {}
        with open(card, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                for key in ("capability", "risk"):
                    if line.startswith(key + ":"):
                        seen[key] = line.split(":", 1)[1].strip().strip("\"'")
        if seen.get("risk") == "READ" and seen.get("capability"):
            return seen["capability"]
    return None


def start_slow(guard, scratch, seconds):
    """The real server, in front of a Revit that answers after `seconds`,
    initialised as a host would."""
    env = guard.for_child(os.environ, os.path.join(scratch, "slow-%d" % seconds))
    env["LOCALAPPDATA"] = os.path.join(scratch, "local")
    env["XDG_DATA_HOME"] = os.path.join(scratch, "local")
    env["HERON_TEST_SLOW_REVIT_S"] = str(seconds)
    host = Host(env, script=SLOW_SERVER)
    listener = Listener(host)
    host.send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
        "protocolVersion": "2024-11-05", "capabilities": {},
        "clientInfo": {"name": "heron-stdio-test", "version": "0"}}})
    host.send({"jsonrpc": "2.0", "method": "notifications/initialized"})
    return host, listener


def slow_revit(guard, scratch):
    """A host that asked for progress is told the wait goes on; one that did
    not is told nothing. Both calls run at once, so the run costs SLOW_S."""
    print()
    print("  a Revit that takes %d s to answer (tests/support/slow_revit_server.py)" % SLOW_S)
    capability = first_read_capability()
    check(capability is not None, "the library has a READ capability to send (%s)" % capability)
    if capability is None:
        return

    check(EVERY_S is not None,
          "the bridge client has an interval for saying the wait goes on (WAIT_NOTE_S)")
    every = EVERY_S or 10.0
    token = "heron-wait-%d" % os.getpid()
    told, told_hears = start_slow(guard, scratch, SLOW_S)
    quiet, quiet_hears = start_slow(guard, scratch, QUIET_S)
    try:
        ready = (told_hears.answer(1, DEADLINE)[1], quiet_hears.answer(1, DEADLINE)[1])
        check(all(ready), "both servers answer initialize")
        if not all(ready):
            return

        sent = time.time()
        told.send({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {
            "name": "revit_read", "arguments": {"capability": capability},
            "_meta": {"progressToken": token}}})
        quiet.send({"jsonrpc": "2.0", "id": 2, "method": "tools/call", "params": {
            "name": "revit_read", "arguments": {"capability": capability}}})

        quiet_at, quiet_answer = quiet_hears.answer(2, QUIET_S + DEADLINE)
        told_at, told_answer = told_hears.answer(2, SLOW_S + DEADLINE)

        print()
        print("  the host that sent a progressToken")
        said = text_of(json.dumps(told_answer)) if told_answer else None
        check(said is not None and "read Heron slow stand-in" in said,
              "the answer still arrives, and is the stand-in's (%s)"
              % ("%.0f s" % (told_at - sent) if told_at else "never"))
        if told_at:
            check(told_at - sent >= SLOW_S - 1,
                  "and only once the stand-in answered - %.0f s, not before %d"
                  % (told_at - sent, SLOW_S))
        notes = told_hears.progress()
        at = [when for when, _ in notes]
        params = [p for _, p in notes]
        # Notes at every, 2*every ... up to the answer: one fewer than fit in
        # SLOW_S, and one more when the last falls just before the answer.
        least = int(SLOW_S // every) - 1
        check(least <= len(notes) <= least + 1,
              "it is told the wait goes on: %d progress notifications in %d s, "
              "one every %d s (%d expected, or %d)"
              % (len(notes), SLOW_S, every, least, least + 1))
        check(bool(notes) and all(p.get("progressToken") == token for p in params),
              "every one carries the token the host sent")
        check(bool(notes) and all("Waiting for Revit" in (p.get("message") or "") for p in params),
              "every one says, in words, that Heron is waiting for Revit - %r"
              % (params[0].get("message") if params else None))
        values = [p.get("progress") for p in params]
        check(bool(values) and all(isinstance(v, (int, float)) for v in values)
              and all(b > a for a, b in zip(values, values[1:])),
              "progress only rises, as MCP requires: %s" % values)
        if at:
            check(at[0] - sent <= every + 5,
                  "the first comes within %d s of the call, not at the end (%.1f s)"
                  % (every + 5, at[0] - sent))
            gaps = [b - a for a, b in zip(at, at[1:])]
            check(all(every - 3 <= g <= every + 3 for g in gaps),
                  "and they come every %d s, never in a flood: gaps %s"
                  % (every, ["%.1f" % g for g in gaps]))
            check(told_at is None or at[-1] <= told_at,
                  "none comes after the answer")

        print()
        print("  the host that sent none")
        said = text_of(json.dumps(quiet_answer)) if quiet_answer else None
        check(said is not None and "read Heron slow stand-in" in said,
              "the answer arrives (%s)" % ("%.0f s" % (quiet_at - sent) if quiet_at else "never"))
        check(not quiet_hears.progress(),
              "and it is told nothing in %d s - only a host that asked is told "
              "(%d notifications)" % (QUIET_S, len(quiet_hears.progress())))
    finally:
        told.close()
        quiet.close()


def text_of(line):
    if not line:
        return None
    try:
        d = json.loads(line)
    except ValueError:
        return None
    if "result" not in d:
        return None
    return "".join(c.get("text", "") for c in d["result"].get("content", []))


def as_a_host(guard, scratch):
    """The real server, started as a host starts it. Windows only: off it the
    server refuses to start, by design."""
    env = guard.for_child(os.environ, scratch)
    # THE FIX: the child's machine-local folder is a scratch one, so the
    # discovery folder it reads is empty - "no Revit connected". Only the
    # environment crosses into a child, and this is the variable the client
    # already resolves that folder from on Windows (local_app_data), so no
    # production code learns a test's switch. It moves the child's bridge
    # logs and companion files out of the owner's folder too.
    env["LOCALAPPDATA"] = os.path.join(scratch, "local")
    host = Host(env)
    try:
        t0 = time.time()
        host.send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
            "protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "heron-stdio-test", "version": "0"}}})
        host.send({"jsonrpc": "2.0", "method": "notifications/initialized"})
        init = host.read(DEADLINE)
        check(init is not None, "the server answers initialize (%.1fs)" % (time.time() - t0))

        host.send({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
        listed = host.read(DEADLINE)
        names = []
        if listed:
            try:
                names = [t["name"] for t in json.loads(listed)["result"]["tools"]]
            except Exception:
                names = []
        # THE COUNT IS DERIVED, because typing it has now gone stale twice -
        # left at ten when the eleventh arrived, and at eleven when the twelfth
        # did. heron_tools.TOOLS is the declaration; this asserts the server
        # SERVES exactly what is declared, which catches everything the typed
        # number caught and cannot rot.
        declared = set(TOOLS)
        offered = set(names)
        check(offered == declared,
              "the host is offered exactly the declared tools (declared %d, "
              "offered %d%s)"
              % (len(declared), len(offered),
                 "" if offered == declared else
                 "; only in registry: %s; only served: %s"
                 % (sorted(declared - offered) or "none",
                    sorted(offered - declared) or "none")))
        check("heron_capabilities" in names, "heron_capabilities is among them")
        check("heron_gaps" in names, "heron_gaps is among them")

        print()
        print("  the call that hung for thirty minutes")
        line, took = host.call(3, "heron_capabilities", {}, DEADLINE)
        answered = text_of(line)
        check(answered is not None,
              "heron_capabilities REPLIES, within %ds (took %.1fs)" % (DEADLINE, took))
        if answered:
            check("job(s) Heron knows by name" in answered,
                  "it lists the jobs it knows")

            # THIS ASSERTION WAS RE-BASED ON 2026-09-06, hours after it was
            # written, and the reason is worth more than the check.
            #
            # It used to require the word "cannot" - Heron saying it could not
            # RUN anything it named. That was true when this file was written
            # and false by the time it next ran: D-28's executor landed in
            # another worktree the same day, and a read-only fragment now runs
            # against the open model. The test encoded yesterday's truth about
            # a system that had moved, which is the same shape as test_embed
            # and test_retrieve being broken by A7 - and the standard is the
            # same too: re-base it on what is true, with the reason written
            # down, never edit it until green.
            #
            # IT HAPPENED A SECOND TIME, TO THIS VERY ASSERTION, and the
            # paragraph above is left standing because it predicted it.
            # "The limit that has NOT moved" was: a fragment that WRITES
            # cannot reach Revit at all. It moved on 2026-09-15 - #144,
            # "Changes ON now changes something" - and _cannot_run() now says
            # a writing fragment reaches Revit through revit_change, which
            # KEEPS what it did, while the ribbon switch is on. The old
            # sentence survives ONLY in a comment in heron_mcp_server.py, so
            # this check could never pass again.
            #
            # Re-based on what is true, by the standard written above: the
            # durable claim is no longer "writing cannot happen" but "writing
            # is GATED, and Heron says so". That is what must never silently
            # stop being true - a Heron that claimed it could write freely
            # would be the actual danger. Asserting both halves keeps it
            # falsifiable: delete the permission wording and this fails.
            check("Changes" in answered and "refuses" in answered,
                  "it says plainly that writing is GATED - reaching Revit only "
                  "while Changes is switched on, refused by name when it is not")
            check("PROVEN" in answered and "not" in answered,
                  "and it still separates what is proved from what is merely "
                  "present")

        print()
        print("  and the tool beside it still works")
        line, took = host.call(4, "heron_lookup",
                               {"request": "select all the ducts"}, DEADLINE)
        found = text_of(line)
        check(found is not None,
              "heron_lookup replies too (%.1fs)" % took)
        if found:
            check("would need" in found,
                  "it names a capability for a sentence in BIM language")
    finally:
        host.close()


def main():
    print("Heron over real stdio, as a host sees it")
    missing = sdk_missing()
    if os.name != "nt":
        print("  SKIPPED - the real server refuses to start off Windows by design,")
        print("  so only the stand-in Revit's part can run here.")
        if missing:
            print()
            print("COULD NOT RUN - no MCP SDK here (%s), so nothing was checked." % missing)
            print("This is exit %d, which is NOT a pass. Install it with:" % COULD_NOT_RUN)
            print("  pip install --user mcp")
            return COULD_NOT_RUN

    # NO CALL HERE MAY REACH THE REVIT RUNNING ON THIS PC (row 5b-461). The
    # child is guarded as this process would be: a pipe open is refused, and
    # whatever it touched is read back below.
    scratch = tempfile.mkdtemp(prefix="heron-mcp-stdio-")
    guard = GUARD.Guard.around(bridge)
    if os.name == "nt":
        as_a_host(guard, scratch)
    if missing:
        print()
        print("  NOT RUN - the stand-in Revit's part needs the MCP SDK (%s)" % missing)
    else:
        slow_revit(guard, scratch)

    print()
    print("  the Revit running on this PC, never reached (row 5b-461)")
    touched = guard.touched()
    check(not touched,
          "the server never listed %s or opened any named pipe - found %s"
          % (guard.describe(), GUARD.summary(touched)))
    shutil.rmtree(scratch, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    if os.name == "nt":
        print("PASSED - a real host starts Heron, is offered every declared tool,")
        print("and gets an")
        print("answer from every one it asks - inside a deadline rather than eventually.")
    else:
        print("PASSED - only the stand-in Revit's part: off Windows the real server")
        print("refuses to start, so nothing here shows a host offered every tool.")
    if not missing:
        print("A host that sent a progressToken is told every %d s that Heron is"
              % EVERY_S)
        print("waiting for Revit, and one that sent none is told nothing - against a")
        print("stand-in. Whether Claude Code SHOWS it is NEEDS-CHECKING EJ1.")
    print()
    # DERIVED, because it was typed as "13 of 348" and the library has been
    # 44 of 349 for some time. A number in a closing sentence goes stale the
    # day after it is written and is believed for months.
    try:
        sys.path.insert(0, os.path.join(
            os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "brain"))
        import heron_fragment as HF
        found, _problems = HF.load_all()
        proven = sum(1 for f in found.values() if f.status in ("PROVEN", "PRODUCTION"))
        tally = "%d of %d are PROVEN" % (proven, len(found))
    except Exception:
        tally = "most are not PROVEN"
    print("It says NOTHING about whether any fragment WORKS. %s" % tally)
    print("and the rest are claims, and D3 in NEEDS-CHECKING.md is still")
    print("the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
