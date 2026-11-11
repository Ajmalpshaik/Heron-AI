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

AND OFF WINDOWS, THE LIBRARY ALONE (Q-67, D-132). Until 2026-10-17 the server
exited 2 on any system but Windows, so a cloud session was offered no Heron
tool at all - not even the search a chat runs before it builds a tool. Now it
starts there with heron_tools.LIBRARY and nothing else. So off Windows the real
server is started as a host starts it, and must offer exactly that list - no
revit_ tool, which is how a chat knows there is no Revit here - answer a
search inside the deadline, say there is no Revit on this machine rather than
looking for one, and refuse a Revit tool by name at once rather than hang.
Its first answer ends with the two closing paragraphs whole - which door runs
what, and how much is proved - and the next says each in one line: once per
server process, which only a process started for real can show. The one
line names heron_capabilities for the full text, and that tool, asked again,
says both paragraphs whole again.

AND WITH NO KNOWLEDGE FOLDER NAMED (D-153, row 5b-1508). The part above gives
the child a HERON_KNOWLEDGE, which a cloud chat nobody set up has not got -
and there every library tool answered only that Heron had nowhere to keep what
it knows. So the server is started once more off Windows with no
HERON_KNOWLEDGE and no HERON_DATA, and must still answer a search, list its
methods and its jobs, from a store of its own under the machine-local folder.
That part builds a second store from nothing, so it is held to COLD_DEADLINE:
its claim is that an answer comes, and how soon is the part above's.

AND ONE OF EACH (FRAGMENT-ISSUES row 5b-1554). The server's source is more
than one file. What the files share - the server object, the binding, the
pin, the approval, the watch on its own code - is defined in one of them and
imported by the rest, and a definition typed again in a second file is a
second object: two pins is a chat that checks one model and writes to another,
and nothing would say so. And the file a host starts runs as `__main__`, so
anything that imported it by name would run it a second time. Both are held
here as facts about a process: the entry file is run the way Python runs a
script, with the one call that never returns stood in by a report, and the
report must find one of each alive and the entry file loaded under one name.
tests/test_tool_registry.py holds, as text, that no listed file imports the
entry file. Off Windows only, where start-up reaches nothing but the child's
own scratch store; on Windows the same start writes the Companion's note where
Revit looks.
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
import heron_tools as REGISTRY                                 # noqa: E402
import heron_bridge_client as bridge                           # noqa: E402
import live_revit_guard as GUARD                               # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# The file a host starts: the entry file, the last on heron_tools' list of the
# files the server's source is.
SERVER = REGISTRY.server_files()[-1]
SLOW_SERVER = os.path.join(ROOT, "tests", "support", "slow_revit_server.py")

sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

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

# For library_no_folder_named() alone, whose claim is that the library
# ANSWERS where nobody named a folder - not how fast. It builds a second
# store from nothing, and that is all processor: about 22 s on a quiet
# machine, 88 s with three times the work running beside it (review of
# f8f727f8, 2026-11-11), and with thirty-two busy processes on four cores it
# had not come at 90 s and came at 167 s (measured the same day, other
# sessions working on the machine too). So 90 s there was a claim about the
# machine's other work, not about Heron. A hang is still caught - only later.
# The speed of an answer is held where it is the claim: library_only() and
# as_a_host(), on DEADLINE.
COLD_DEADLINE = 3 * DEADLINE

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
    server serves the library alone, and library_only() holds that."""
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


def library_only(guard, scratch):
    """The real server, started as a host starts it on a machine with no
    Revit. Off Windows only: on Windows as_a_host() holds every tool."""
    print()
    print("  off Windows: the library and nothing else (D-132)")
    library = getattr(REGISTRY, "LIBRARY", None)
    check(library is not None,
          "heron_tools says which tools a machine with no Revit is served (LIBRARY)")
    if library is None:
        return
    env = guard.for_child(os.environ, os.path.join(scratch, "library"))
    env["LOCALAPPDATA"] = os.path.join(scratch, "local")
    env["XDG_DATA_HOME"] = os.path.join(scratch, "local")
    # A store of the child's own, so the run never rebuilds a store another
    # chat reads - test_mcp_serves' rule, for its in-process server.
    env["HERON_KNOWLEDGE"] = os.path.join(scratch, "knowledge")
    env["HERON_AUDIT"] = os.path.join(scratch, "audit")
    host = Host(env)
    try:
        t0 = time.time()
        host.send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
            "protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "heron-stdio-test", "version": "0"}}})
        host.send({"jsonrpc": "2.0", "method": "notifications/initialized"})
        init = host.read(DEADLINE)
        check(init is not None,
              "the server STARTS off Windows and answers initialize (%.1fs)%s"
              % (time.time() - t0, "" if init is not None else
                 " - it said: %s" % "".join(host.err).strip()[-300:]))
        if init is None:
            return
        try:
            told = json.loads(init)["result"].get("instructions") or ""
        except (ValueError, KeyError, TypeError, AttributeError):
            told = ""
        check("no Revit" in told,
              "and its instructions tell the chat there is no Revit on this machine")

        host.send({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
        listed = host.read(DEADLINE)
        try:
            names = [t["name"] for t in json.loads(listed)["result"]["tools"]]
        except Exception:                                   # noqa: BLE001
            names = []
        offered = set(names)
        check(offered == set(library),
              "the host is offered exactly heron_tools.LIBRARY (%d) - offered %d%s"
              % (len(library), len(offered), "" if offered == set(library) else
                 "; missing: %s; extra: %s"
                 % (sorted(set(library) - offered) or "none",
                    sorted(offered - set(library)) or "none")))
        check(not any(n.startswith("revit_") for n in names),
              "and no revit_ tool, so the list itself says there is no Revit here")

        line, took = host.call(3, "heron_capabilities", {}, DEADLINE)
        answered = text_of(line)
        check(answered is not None and "job(s) Heron knows by name" in answered,
              "heron_capabilities answers (%.1fs)" % took)

        line, took = host.call(4, "heron_lookup",
                               {"request": "select all the ducts"}, DEADLINE)
        found = text_of(line)
        check(found is not None and "would need" in found,
              "heron_lookup answers a sentence in BIM language (%.1fs)" % took)
        check(found is not None and "No Revit on this machine" in found,
              "and says there is no Revit on this machine, rather than that "
              "none is connected")

        # THE TWO CLOSING PARAGRAPHS, WHOLE ONCE PER PROCESS. Both answers
        # above ended with the same 1,400 characters - which door runs what,
        # and how much is proved - and so did every answer after them. The
        # first answer of a process still carries them whole; the next says
        # each in one line. Read off the first answer, never typed here.
        closing = (answered or "").splitlines()[-2:]
        doors = closing[0].split("Heron can run it. ", 1)[-1] if closing else ""
        proved = closing[-1] if closing else ""
        check(len(doors) > 600 and "PROVEN" in proved,
              "the first answer of this process says both closing paragraphs "
              "whole (%d and %d characters)" % (len(doors), len(proved)))
        last = (found or "").splitlines()[-2:] or ["", ""]
        check(bool(doors) and doors not in (found or doors)
              and proved not in (found or proved),
              "the next answer does not say either whole again")
        check(len(last[0]) * 3 <= len(doors)
              and all(word in last[0] for word in
                      ("WRITES", "revit_change", "revit_read", "Changes")),
              "it says which door runs what in one line - a change through "
              "revit_change, and only with Changes on (%d characters, was %d)"
              % (len(last[0]), len(doors)))
        check(len(last[-1]) < len(proved)
              and last[-1].split(" capabilities")[0]
              == proved.split(" capabilities")[0],
              "and how many are PROVEN in one line, with the same count")
        # THE ONE LINE STANDS ON ITS OWN. This process serves a whole session
        # - a cleared conversation and a subagent ask the same one - so the
        # reader of the short form may never have seen the long one: it
        # points at nothing said before, and names the tool that has the
        # whole text whenever it is asked.
        check("heron_capabilities says this in full" in last[0]
              and "first time" not in last[0],
              "the one line names heron_capabilities for the full text, and "
              "points at nothing this server said before")

        line, took = host.call(5, "revit_health", {}, DEADLINE)
        check(line is not None,
              "a Revit tool asked for anyway is answered at once, not left to "
              "hang (%.1fs)" % took)
        try:
            reply = json.loads(line) if line else {}
        except ValueError:
            reply = {}
        refused = ("error" in reply
                   or (reply.get("result") or {}).get("isError") is True)
        check(refused, "and the answer is a refusal - the tool is not served here")

        # heron_resolve SAYS THE SAME OF THIS MACHINE AS THE LOOKUP DOES (row
        # 5b-1506). It ended "No Revit is connected, so the version filter
        # did not run" - the sentence D-132 replaced in heron_lookup, typed
        # here a second time: a chat told none is connected goes looking for
        # one, on a machine where none can be reached.
        line, took = host.call(6, "heron_resolve",
                               {"capability": "SET_SELECTION"}, DEADLINE)
        resolved = text_of(line) or ""
        check("provider(s)" in resolved,
              "heron_resolve answers for a capability by name (%.1fs)" % took)
        check("No Revit on this machine" in resolved
              and "so the version filter did not run" in resolved,
              "and says there is no Revit on this machine, which is why the "
              "version filter did not run")
        check("No Revit is connected" not in resolved,
              "never that none is connected")

        # AND heron_capabilities HAS THE WHOLE TEXT EVERY TIME IT IS ASKED,
        # which is what the one line promises of it.
        line, took = host.call(7, "heron_capabilities", {}, DEADLINE)
        again = text_of(line) or ""
        check(bool(doors) and doors in again and proved in again,
              "heron_capabilities, asked a second time, says both closing "
              "paragraphs whole again (%.1fs)" % took)
    finally:
        host.close()


# THE ENTRY FILE, RUN AS PYTHON RUNS A SCRIPT, with `server.run()` - the one
# call that never returns - stood in by a report of what the process holds.
# Nothing else is replaced. The classes are found from the objects the entry
# file itself names, so no module of the server is named here.
ONE_OF_EACH = r"""
import gc, json, os, runpy, sys
entry = sys.argv[1]
try:
    from mcp.server.mcpserver import MCPServer as sdk_server      # SDK 2.x
except ImportError:
    from mcp.server.fastmcp import FastMCP as sdk_server          # SDK 1.x

def report(server, *_a, **_k):
    main = vars(sys.modules["__main__"])
    again = os.path.splitext(os.path.basename(entry))[0]
    gc.collect()
    alive = {}
    for name in ("server", "binding", "pinned", "approval"):
        kind = type(main[name]) if name in main else None
        alive[name] = sum(1 for o in gc.get_objects() if type(o) is kind) if kind else None
    watch = [o for o in gc.get_objects() if type(o).__name__ == "RunningCode"]
    alive["watch"] = len(watch)
    sys.stdout.write(json.dumps({
        "as_main": os.path.samefile(sys.modules["__main__"].__file__, entry),
        "again": again in sys.modules, "alive": alive,
        "same_server": main.get("server") is server}) + chr(10))
    sys.stdout.flush()
    os._exit(0)

sdk_server.run = report
sys.argv = [entry]
sys.path.insert(0, os.path.dirname(entry))
runpy.run_path(entry, run_name="__main__")
"""


def one_of_each(guard, scratch):
    """One server object, one binding, one pin, one approval and one watch in
    a process started on the entry file - and that file loaded once. Off
    Windows only (the module header says why)."""
    print()
    print("  started on the entry file, the process holds one of each (row 5b-1554)")
    env = guard.for_child(os.environ, os.path.join(scratch, "one-of-each"))
    env["LOCALAPPDATA"] = os.path.join(scratch, "local")
    env["XDG_DATA_HOME"] = os.path.join(scratch, "local")
    env["HERON_KNOWLEDGE"] = os.path.join(scratch, "knowledge")
    env["HERON_AUDIT"] = os.path.join(scratch, "audit")
    try:
        ran = subprocess.run([sys.executable, "-c", ONE_OF_EACH, SERVER], cwd=ROOT, env=env,
                             capture_output=True, text=True, encoding="utf-8",
                             timeout=COLD_DEADLINE)
        said, why = ran.stdout, ran.stderr.strip()[-300:]
    except subprocess.TimeoutExpired:
        said, why = "", "it did not reach server.run() in %d s" % COLD_DEADLINE
    found = {}
    for line in said.splitlines():
        try:
            found = json.loads(line)
        except ValueError:
            continue
    check(bool(found), "the entry file ran as far as server.run()%s"
          % ("" if found else " - it said: %s" % why))
    if not found:
        return
    check(found.get("as_main") is True and found.get("again") is False,
          "it ran as `__main__` and was not loaded a second time under its own name")
    alive = found.get("alive") or {}
    for name, what in (("server", "server object"), ("binding", "binding"), ("pinned", "pin"),
                       ("approval", "approval"), ("watch", "watch on its own code")):
        check(alive.get(name) == 1,
              "exactly one %s is alive in the process (%s)" % (what, alive.get(name)))
    check(found.get("same_server") is True,
          "and the server that runs is the one the tools registered on")


def library_no_folder_named(guard, scratch):
    """The same server on a machine nobody set up for Heron: off Windows, no
    HERON_KNOWLEDGE and no HERON_DATA. That is a cloud chat opened from a
    phone, and until 2026-11-11 every library tool there answered only that
    Heron had nowhere to keep what it knows (row 5b-1508). library_only()
    above could not see it: it names a store for the child, which is the one
    thing such a session has not got."""
    print()
    print("  off Windows, no knowledge folder named: the library still answers (D-153)")
    env = guard.for_child(os.environ, os.path.join(scratch, "unnamed"))
    local = os.path.join(scratch, "unnamed-local")
    env["LOCALAPPDATA"] = local
    env["XDG_DATA_HOME"] = local
    for name in ("HERON_KNOWLEDGE", "HERON_DATA"):
        env.pop(name, None)
    host = Host(env)
    # Every answer is matched to its own call by id, on a reader of its own:
    # Host.read() takes whichever line comes next, so one late answer would
    # be read as the next call's and fail every check after it.
    hears = Listener(host)
    try:
        t0 = time.time()
        host.send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
            "protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "heron-stdio-test", "version": "0"}}})
        host.send({"jsonrpc": "2.0", "method": "notifications/initialized"})
        init = hears.answer(1, COLD_DEADLINE)[1]
        check(init is not None,
              "the server starts with no knowledge folder named (%.1fs)%s"
              % (time.time() - t0, "" if init is not None else
                 " - it said: %s" % "".join(host.err).strip()[-300:]))
        if init is None:
            return
        for ident, name, args, want, what in (
                (2, "heron_lookup", {"request": "select all the ducts"},
                 "would need", "heron_lookup answers a sentence in BIM language"),
                (3, "heron_method", {"job": ""},
                 "pass one as `job`", "heron_method lists the jobs it has a method for"),
                (4, "heron_capabilities", {},
                 "job(s) Heron knows by name", "heron_capabilities answers")):
            asked = time.time()
            host.send({"jsonrpc": "2.0", "id": ident, "method": "tools/call",
                       "params": {"name": name, "arguments": args}})
            at, reply = hears.answer(ident, COLD_DEADLINE)
            said = text_of(json.dumps(reply)) if reply is not None else None
            check(said is not None and want in said and "nowhere to keep" not in said,
                  "%s, with no folder named (%.1fs of %d allowed)%s"
                  % (what, (at or time.time()) - asked, COLD_DEADLINE,
                     "" if said is not None and want in said else
                     " - it said: %s" % " ".join((said or "nothing").split())[:160]))
    finally:
        host.close()
    stores = [os.path.join(folder, name) for folder, _dirs, names in os.walk(local)
              for name in names if name == "global.db"]
    under = os.path.join(local, "Heron", "index") + os.sep
    check(len(stores) == 1 and stores[0].startswith(under),
          "and its store is one of its own, under the machine-local Heron folder's "
          "index (%s)" % (", ".join(os.path.relpath(s, local) for s in stores) or "none made"))


def main():
    print("Heron over real stdio, as a host sees it")
    missing = sdk_missing()
    if os.name != "nt" and missing:
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
    else:
        library_only(guard, scratch)
        one_of_each(guard, scratch)
        library_no_folder_named(guard, scratch)
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
        print("PASSED - off Windows the real server starts with the library alone,")
        print("offers no Revit tool and answers a search inside the deadline; the")
        print("Revit tools are held on Windows, where as_a_host() runs.")
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
        # OF THE CARDS STILL IN THE LIBRARY. A retired card is not a claim
        # waiting to be proved (row 5b-1521): docs/09's ladder, from
        # DEPRECATED on, is left out of both numbers.
        retired = HF.STATUSES[HF.STATUSES.index("DEPRECATED"):]
        live = [f for f in found.values() if f.status not in retired]
        proven = sum(1 for f in live if f.status in ("PROVEN", "PRODUCTION"))
        tally = "%d of %d are PROVEN" % (proven, len(live))
    except Exception:
        tally = "most are not PROVEN"
    print("It says NOTHING about whether any fragment WORKS. %s" % tally)
    print("and the rest are claims, and D3 in NEEDS-CHECKING.md is still")
    print("the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
