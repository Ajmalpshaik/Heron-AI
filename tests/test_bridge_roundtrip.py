#!/usr/bin/env python3
# Heron-Agent:  HERON-DEV-INT-012
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

# THIS SUITE OWNS HERON-DEV-INT-012, DECIDED BY THE OWNER ON 2026-09-18,
# in a sitting that ran from the 17th and crossed midnight while he answered.
#
# The row is "runs integration tests against a MOCKED REVIT BOUNDARY", and that
# is exactly what this is: tests/Heron.Bridge.TestHost runs the bridge with no
# Revit at all - pipe naming, discovery, framing, ping - because Heron.Bridge
# deliberately carries no Revit reference. docs/13 section 3 describes the
# approach; this has been it since the first commit.
#
# WHY IT WAITED, AND IT WAS NOT FOR WANT OF A FILE. D-75 put it plainly:
# "its candidates fit the row exactly and are both layer `test`. Claiming one
# makes a third agent claimed by nothing but a suite, which
# test_contract_reference.py shows rather than drops on purpose. Allowed,
# awkward, and the owner's call."
#
# So the awkwardness is real and is now ACCEPTED rather than removed. Writing a
# third file in brain/ or tools/ to dodge it would have been the agent
# explosion HERON-AHR-WFP-015 exists to refuse - a new file whose only job is
# to be a tidier place for a claim.
#
# What that costs: test_contract_reference.py will report this agent among the
# ones built without a contract, under layer `test`. That report is right to
# say so and the number is not a defect.

"""
Step 1 acceptance test - the pipe round trip, plus Step 6's lease.

Runs WITH NO REVIT. That is the whole point of it, and Step 6 made it more
valuable: the lease lives in the Kernel and knows nothing about models, so
this file verifies it without Revit ever being opened.

Starts the Revit-free bridge host, connects to it, and checks that ping comes
back. Proves the transport without Revit, without the discovery directory, and
without any cross-process filesystem assumptions.

    dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024
    python tests/test_bridge_roundtrip.py

TWO PLATFORMS, ONE SET OF CHECKS
--------------------------------
It used to print SKIP anywhere but Windows. It no longer does, because the
thing being tested is not Windows-specific: .NET implements NamedPipeServerStream
on Unix as a socket in the temp directory, so the same compiled bridge answers
the same protocol there. Only the two lines that OPEN the connection differ.

That is why this is one file with a transport shim rather than a second copy
for POSIX. Every assertion below runs on both, and a copy would have started
drifting the first time one of them was edited - this repository has been bitten
by two-lists-of-the-same-thing more than once.

BE HONEST ABOUT WHICH RUN YOU HAVE. A POSIX run proves the framing, the JSON
parser, the token check, the newest-connection-wins handover, the toggle cycle
and ALL of the lease - every one of which is plain C# with no operating system
in it. It does NOT prove the Windows named pipe itself: its naming, its
security descriptor, or the CreateNewInstance flag in note 2 of HANDOVER
section 4. Revit runs on Windows, so a POSIX pass is a strong signal and is
never the final word - A4 in NEEDS-CHECKING.md means the WINDOWS run.

Exit 0 = the bridge works. Exit 3 = the host binary has not been built, so
nothing was checked - that is NOT a pass, and the line above says what to run.
This is the "Prove it" line for Step 1 in docs/27-build-order.md, in a form
that can run in CI on a machine with no Revit installed.
"""

import json
import os
import re
import socket
import subprocess
import sys
import time

WINDOWS = os.name == "nt"

# The repository's fourth state - see tests/README.md. Not a pass, not a
# failure: the suite could not run, and says so rather than guessing.
COULD_NOT_RUN = 3

REVIT_VERSION = "2024"
HOST_LIFETIME_S = "45"
CONNECT_TIMEOUT_S = 10.0

HOST_PROJECT = os.path.join("tests", "Heron.Bridge.TestHost")

# TWO OUTPUT DIRECTORIES, ON PURPOSE.
#
# Revit 2020-2024 mean net472/net48, whose build output is an .exe that only
# Windows can run. Off Windows the same sources are built for net8.0 instead,
# which produces a .dll the installed runtime launches.
#
# They cannot share a directory. AppendTargetFrameworkToOutputPath is false
# repo-wide, so every target framework writes to the SAME bin/x64/Debug - and
# `tools/check-compile.py`, which builds this project once per Revit version,
# would leave whichever it did last. That is not hypothetical: it happened the
# first time the two were run in one sitting, and this file went from 32 passes
# to "not found". The POSIX host therefore gets its own folder and the two
# never meet.
# THE POSIX TARGET FRAMEWORK IS DETECTED, NOT HARDWIRED - fixed 2026-09-02.
#
# It said net8.0. That was true of the machine it was written on and false of
# the next one: this container ships the .NET 10 SDK and NO .NET 8 runtime, so
# the project BUILT and the host would not START -
#
#   You must install or update .NET to run this application.
#   Framework: 'Microsoft.NETCore.App', version '8.0.0'
#
# and the suite reported "not found" for four sessions running, which reads as a
# missing build rather than a missing runtime. A pinned framework in a test that
# exists to run on whatever machine is in front of it is a machine-specific
# assumption written down as a constant.
#
# `dotnet --list-runtimes` is the only thing that knows, so it is asked.
def _posix_tfm():
    """The newest Microsoft.NETCore.App the machine can actually run.

    Returns e.g. "net10.0". Falls back to net8.0 - the previous hardcoded
    value - when the question cannot be answered, so a machine where dotnet
    behaves unexpectedly is no worse off than before this was written.
    """
    try:
        out = subprocess.check_output(["dotnet", "--list-runtimes"],
                                      stderr=subprocess.STDOUT).decode("utf-8", "replace")
    except Exception:                                   # noqa: BLE001
        return "net8.0"

    majors = []
    for line in out.splitlines():
        if not line.startswith("Microsoft.NETCore.App "):
            continue
        version = line.split()[1]
        head = version.split(".")[0]
        if head.isdigit():
            majors.append(int(head))

    return "net%d.0" % max(majors) if majors else "net8.0"


POSIX_TFM = _posix_tfm()

_WIN_DIR = os.path.join(HOST_PROJECT, "bin", "x64", "Debug")
_POSIX_DIR = os.path.join(HOST_PROJECT, "bin", "x64", "Debug-%s" % POSIX_TFM)

HOST_EXE = os.path.join(_WIN_DIR, "Heron.Bridge.TestHost.exe")
HOST_DLL = os.path.join(_POSIX_DIR, "Heron.Bridge.TestHost.dll")

# The exact command that produces the binary this file needs, so a failure
# says what to run rather than what is missing.
BUILD_HINT = (
    "dotnet build %s -p:RevitVersion=%s" % (HOST_PROJECT, REVIT_VERSION)
    if WINDOWS else
    "dotnet build %s -p:RevitVersion=%s -p:HeronTfm=%s -p:OutputPath=bin/x64/Debug-%s/"
    % (HOST_PROJECT, REVIT_VERSION, POSIX_TFM, POSIX_TFM))


def host_binary():
    """Where the built host is, and whether it is there at all."""
    return HOST_EXE if WINDOWS else HOST_DLL


def host_command(*args):
    """How to start the host: directly on Windows, through the runtime elsewhere."""
    if WINDOWS:
        return [HOST_EXE] + list(args)
    return ["dotnet", HOST_DLL] + list(args)


def _pipe_endpoint(pipe_name):
    """
    Where the server is listening, in this operating system's terms.

    On Windows a named pipe is a path under \\.\pipe. On Unix, .NET has no
    named pipes to use, so NamedPipeServerStream is a socket named
    CoreFxPipe_<name> in the temp directory. Both are the SAME C# object;
    only the address differs, which is why one shim is enough.
    """
    if WINDOWS:
        return "\\\\.\\pipe\\" + pipe_name
    return os.path.join(os.environ.get("TMPDIR", "/tmp"), "CoreFxPipe_" + pipe_name)


def _open_endpoint(path):
    """One connection, as a file-like object with read/write/close."""
    if WINDOWS:
        return open(path, "r+b", buffering=0)
    sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
    try:
        sock.connect(path)
    except OSError:
        sock.close()
        raise
    # buffering=0 so a read of one byte is one byte, exactly as the Windows
    # handle behaves - the framing checks below read a byte at a time on
    # purpose, to prove the bridge terminates its own lines.
    return sock.makefile("rwb", buffering=0)


def connect(pipe_name, timeout=CONNECT_TIMEOUT_S):
    """Open the connection, retrying until the server is listening."""
    path = _pipe_endpoint(pipe_name)
    deadline = time.time() + timeout
    last = None
    while time.time() < deadline:
        try:
            return _open_endpoint(path)
        except OSError as exc:
            last = exc
            time.sleep(0.1)
    raise RuntimeError("could not open %s within %.0fs: %s" % (path, timeout, last))


TOKEN = None      # this session's secret, read from the host's own output


def send_raw(handle, text):
    """Send an exact line. Needed for requests a dict cannot express."""
    handle.write((text + "\n").encode("utf-8"))
    line = b""
    while not line.endswith(b"\n"):
        chunk = handle.read(1)
        if not chunk:
            raise RuntimeError("pipe closed before a full response arrived")
        line += chunk
    return json.loads(line.decode("utf-8").strip())


# This test's own chat identity. Step 6 added the lease (HeronLease): a request
# for anything except ping and info must say WHICH chat it is from, or it is
# refused as anonymous. Without this every non-exempt call in this file started
# failing with session_in_use - found by reading, since this file cannot run on
# the machine it was edited on.
CLIENT = "roundtrip-a"
OTHER_CLIENT = "roundtrip-b"


def call(handle, op, client=CLIENT):
    body = {"op": op, "token": TOKEN}
    if client is not None:
        body["client"] = client
    handle.write((json.dumps(body) + "\n").encode("utf-8"))
    line = b""
    while not line.endswith(b"\n"):
        chunk = handle.read(1)
        if not chunk:
            raise RuntimeError("pipe closed before a full response arrived")
        line += chunk
    return json.loads(line.decode("utf-8").strip())


def main():
    if not os.path.exists(host_binary()):
        # EXIT 3, NOT 1, AND THE DIFFERENCE IS THE WHOLE OF FRAGMENT-ISSUES
        # ROW 162. A missing host binary is a build step nobody took, not a
        # claim about the code - and while this path returned 1,
        # tools/check-gaps.py had to silence the suite BY NAME to stop the
        # sweep reporting a FAIL for it. A silenced suite prints nothing at
        # all, so "every test passes" quietly meant one fewer than there are.
        # Saying "could not run" out loud is what lets the sweep name it.
        print("COULD NOT RUN - %s not found." % host_binary())
        print("      Build it first: %s" % BUILD_HINT)
        print("      This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    if WINDOWS:
        print("Starting the bridge host (no Revit)...")
    else:
        print("Starting the bridge host (no Revit, no Windows)...")
        print("  NOTE  this proves the protocol and the lease, NOT the Windows")
        print("        named pipe itself. A4 still means the Windows run.")
    proc = subprocess.Popen(
        host_command(REVIT_VERSION, HOST_LIFETIME_S),
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        universal_newlines=True, bufsize=1)

    failures = []
    handle = None
    try:
        # Read the pipe name from the host itself rather than from the
        # discovery file - this test is about the transport, not discovery.
        pipe_name = None
        # Keep what the host actually said. Without this the failure below
        # reports the SYMPTOM - no pipe name - for every possible cause, and
        # the commonest cause on a fresh machine is not a bridge fault at all
        # (see the runtime note under the failure).
        said = []
        deadline = time.time() + 20
        while time.time() < deadline:
            line = proc.stdout.readline()
            if not line:
                break
            if len(said) < 40:          # enough to diagnose, not a transcript
                said.append(line.rstrip())
            m = re.search(r"Pipe\s+(heron\.\S+)", line)
            if m:
                pipe_name = m.group(1)
            t = re.search(r"Token\s+(\S+)", line)
            if t:
                globals()["TOKEN"] = t.group(1)
            if pipe_name and TOKEN:
                break
        if not pipe_name:
            print("FAIL  host never reported a pipe name")
            # The host is built for net8.0 on purpose. A machine carrying only
            # the .NET 10 SDK - which is exactly what docs/30 tells you to
            # install, because it is the one that compiles all eight releases -
            # has no 8.0 runtime, so the host cannot start at all. That is the
            # environment, not the bridge, and saying so here saves the reader
            # debugging a transport that never ran.
            blob = "\n".join(said)
            if "must install or update .NET" in blob or "Microsoft.NETCore.App" in blob:
                print("      The host could not START: no .NET 8 runtime on this machine.")
                print("      It compiled fine - nothing here is evidence about the bridge.")
                print("      Either:  apt-get install -y dotnet-runtime-8.0")
                print("      or:      DOTNET_ROLL_FORWARD=Major python tests/test_bridge_roundtrip.py")
            elif said:
                print("      The host said, before giving up:")
                for said_line in said[:8]:
                    print("        | %s" % said_line)
            else:
                print("      The host said nothing at all.")
            return 1
        print("  pipe: %s" % pipe_name)

        handle = connect(pipe_name)
        print("  connected")

        # --- ping ---
        started = time.time()
        reply = call(handle, "ping")
        elapsed_ms = (time.time() - started) * 1000.0
        if reply.get("ok") and reply.get("pong") is True:
            print("  PASS  ping -> pong  (%.1f ms)" % elapsed_ms)
        else:
            failures.append("ping returned %r" % reply)

        # --- info ---
        reply = call(handle, "info")
        if reply.get("ok") and str(reply.get("revitVersion")) == REVIT_VERSION:
            print("  PASS  info -> Revit %s, protocol %s"
                  % (reply.get("revitVersion"), reply.get("protocolVersion")))
        else:
            failures.append("info returned %r" % reply)

        # A FREE LEASE, OBSERVED WHILE IT IS STILL FREE.
        #
        # This has to happen here and nowhere later. ping and info are the only
        # exempt operations, so this is the last moment in the file at which
        # nothing has claimed the Revit - the very next check sends an unknown
        # op, and BridgeServer claims the lease for anything that is not ping
        # or info, an unknown op included.
        #
        # It used to sit further down, in the lease section, where it read
        # nicely and was false: chat A had already taken the lease by then and
        # info correctly said so. Nothing was wrong with the bridge. The check
        # was simply asserted from reading rather than from running, and this
        # file could not run on the machine it was written on.
        if reply.get("inUse") is False and reply.get("mine") is False:
            print("  PASS  nothing has claimed it yet")
        else:
            failures.append("info said inUse=%r mine=%r before anyone claimed it"
                            % (reply.get("inUse"), reply.get("mine")))

        # --- unknown op must fail cleanly, not crash the bridge ---
        reply = call(handle, "no_such_operation")
        if reply.get("ok") is False and reply.get("error") == "unknown_op":
            print("  PASS  unknown op -> clean refusal, connection still open")
        else:
            failures.append("unknown op returned %r" % reply)

        # --- the bridge survives that and still answers ---
        reply = call(handle, "ping")
        if reply.get("ok"):
            print("  PASS  still responsive after a bad request")
        else:
            failures.append("bridge stopped responding after a bad request")

        # --- the request parser reads keys, it does not search for them ---
        # Every one of these would have fooled the substring search this
        # replaced, and every one becomes reachable at Step 2, when a
        # request starts carrying arguments.
        parser_cases = [
            ('{"args": {"op": "no_such_operation"}, "op": "ping"}',
             "a nested op is not the real op"),
            ('{"note": "the op is ping", "op": "ping"}',
             "a key name inside a value is not a key"),
            ('{"op": "ping", "trailing": "no_such_operation"}',
             "a later value is not mistaken for the op"),
            ('{ "op" : "ping" }',
             "whitespace around the key and the colon"),
            ('{"path": "C:\\\\Temp\\\\a b.rvt", "op": "ping"}',
             "escaped backslashes in an earlier value"),
            ('{"depth": {"a": {"b": [1, 2, {"op": "no_such_operation"}]}}, "op": "ping"}',
             "an op buried inside nested arrays and objects"),
        ]
        for raw, why in parser_cases:
            # Token appended LAST on purpose: the parser has to walk the whole
            # object, nested structures included, before it even authenticates.
            reply = send_raw(handle, raw[:-1] + ', "token": "%s"}' % TOKEN)
            if reply.get("ok") and reply.get("pong") is True:
                print("  PASS  parser: %s" % why)
            else:
                failures.append("parser case (%s) on %s returned %r" % (why, raw, reply))

        # --- malformed input is refused, and the bridge stays up ---
        reply = send_raw(handle, '{"op": ')
        if reply.get("ok") is False:
            print("  PASS  malformed JSON -> clean refusal")
        else:
            failures.append("malformed JSON returned %r" % reply)

        if not call(handle, "ping").get("ok"):
            failures.append("bridge stopped responding after malformed JSON")

        # --- a wrong token is refused before anything else ---
        reply = send_raw(handle, '{"op": "ping", "token": "not-the-token"}')
        if reply.get("ok") is False and reply.get("error") == "unauthorized":
            print("  PASS  wrong token -> refused")
        else:
            failures.append("wrong token returned %r" % reply)

        reply = send_raw(handle, '{"op": "ping"}')
        if reply.get("ok") is False and reply.get("error") == "unauthorized":
            print("  PASS  no token -> refused")
        else:
            failures.append("missing token returned %r" % reply)

        # ------------------------------------------------------------------
        # THE LEASE (Step 6, D-22). One Revit is one door.
        #
        # All of this runs on Windows with NO REVIT, because the lease lives in
        # the Kernel and knows nothing about models. That matters: it moves
        # verifying the lease out of "needs Revit" and into "needs Windows",
        # which is a much shorter wait.
        # ------------------------------------------------------------------
        print("The lease:")

        # ping and info are EXEMPT. Asking who holds a Revit must never be the
        # act of claiming it, or an honest (free)/(in use) list is impossible.
        reply = call(handle, "ping", client=None)
        if reply.get("ok") and reply.get("pong") is True:
            print("  PASS  ping needs no lease - looking is not claiming")
        else:
            failures.append("anonymous ping returned %r" % reply)

        reply = call(handle, "info", client=None)
        if reply.get("ok") and "inUse" in reply:
            print("  PASS  info needs no lease, and reports who holds it")
        else:
            failures.append("anonymous info returned %r" % reply)

        # By now chat A holds it - the unknown-op probe further up claimed it,
        # because claiming is what the bridge does for every operation that is
        # not exempt. So the honest check here is not that the lease is free,
        # it is that an ANONYMOUS caller is told the truth about it: taken, and
        # not yours. A stranger has to be able to see that without claiming it,
        # or the picker's (in use) column cannot exist.
        if reply.get("inUse") is True and reply.get("mine") is False:
            print("  PASS  a stranger is told it is taken, and that it is not theirs")
        else:
            failures.append("anonymous info said inUse=%r mine=%r while chat A held it"
                            % (reply.get("inUse"), reply.get("mine")))

        # An anonymous request for real work is refused: without an identity
        # that survives reconnection there is no way to tell "the same chat
        # coming back" from "a second chat arriving".
        reply = call(handle, "no_such_operation", client=None)
        if reply.get("ok") is False and reply.get("error") == "session_in_use":
            print("  PASS  anonymous work -> refused, cannot be told apart from a second chat")
        else:
            failures.append("anonymous non-exempt op returned %r" % reply)

        # Chat A claims it by doing anything.
        reply = call(handle, "no_such_operation", client=CLIENT)
        if reply.get("ok") is False and reply.get("error") == "unknown_op":
            print("  PASS  chat A claims the lease and gets a real answer")
        else:
            failures.append("first client's request returned %r" % reply)

        reply = call(handle, "info", client=CLIENT)
        if reply.get("inUse") is True and reply.get("mine") is True:
            print("  PASS  info now reports it held, and held by THIS chat")
        else:
            failures.append("info after claiming returned %r" % reply)

        # Chat B is refused rather than taking it over mid-job.
        reply = call(handle, "no_such_operation", client=OTHER_CLIENT)
        if reply.get("ok") is False and reply.get("error") == "session_in_use":
            print("  PASS  chat B refused - not allowed to take it over")
        else:
            failures.append("second client's request returned %r" % reply)

        if "another chat" in (reply.get("message") or ""):
            print("  PASS  and the refusal says what is happening, in words")
        else:
            failures.append("refusal message was %r" % reply.get("message"))

        # But chat B can still LOOK, and is told it is not theirs.
        reply = call(handle, "info", client=OTHER_CLIENT)
        if reply.get("inUse") is True and reply.get("mine") is False:
            print("  PASS  chat B can see it is taken, and that it is not theirs")
        else:
            failures.append("info for the second client returned %r" % reply)

        # Chat A still works. It never lost its hold.
        reply = call(handle, "ping", client=CLIENT)
        if reply.get("ok"):
            print("  PASS  chat A was never cut off")
        else:
            failures.append("first client stopped working: %r" % reply)

        # An unauthenticated caller must not even learn which operations exist.
        reply = send_raw(handle, '{"op": "no_such_operation", "token": "wrong"}')
        if reply.get("error") == "unauthorized":
            print("  PASS  unknown op with a bad token -> unauthorized, not unknown_op")
        else:
            failures.append("bad token leaked the op verdict: %r" % reply)

        # --- a SECOND connection takes the session: newest wins ---
        second = connect(pipe_name, timeout=3.0)
        try:
            if call(second, "ping").get("ok"):
                print("  PASS  second connection served immediately")
            else:
                failures.append("second connection was not served")

            # The first connection should now be gone - dropped, not queued.
            try:
                stale = call(handle, "ping")
                failures.append("the older connection still answered: %r" % stale)
            except Exception:
                print("  PASS  the older connection was dropped, not left waiting")

            # And a third preempts the second just as fast.
            third = connect(pipe_name, timeout=3.0)
            try:
                if call(third, "ping").get("ok"):
                    print("  PASS  a third connection preempts just as fast")
                else:
                    failures.append("third connection was not served")
            finally:
                third.close()
        finally:
            second.close()

    except Exception as exc:
        failures.append("%s: %s" % (type(exc).__name__, exc))
    finally:
        if handle:
            try:
                handle.close()
            except OSError:
                pass
        proc.terminate()
        try:
            proc.wait(timeout=5)
        except Exception:
            proc.kill()

    # --- the ribbon toggle: connect, disconnect, connect again ---
    # Stop() clears the listener threads and removes the discovery file, so
    # the second Start() has to rebuild both. That is what the button does on
    # every second press, and nothing else here covers it.
    print()
    print("Toggling the bridge off and on (no Revit)...")
    cycled = subprocess.Popen(
        host_command(REVIT_VERSION, HOST_LIFETIME_S, "cycle"),
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        universal_newlines=True, bufsize=1)
    handle2 = None
    try:
        pipe2, token2, seen = None, None, []
        deadline = time.time() + 25
        while time.time() < deadline:
            line = cycled.stdout.readline()
            if not line:
                break
            if line.strip().startswith("cycle:"):
                seen.append(line.strip())
                print("  " + line.strip())
            m = re.search(r"Pipe\s+(heron\.\S+)", line)
            if m:
                pipe2 = m.group(1)
            t = re.search(r"Token\s+(\S+)", line)
            if t:
                token2 = t.group(1)
            if pipe2 and token2:
                break
    
        if len(seen) != 3:
            failures.append("toggle cycle did not report all three states: %r" % seen)
        else:
            if "running=True" not in seen[0] or "announced=True" not in seen[0]:
                failures.append("after connect, expected running and announced: %s" % seen[0])
            if "running=False" not in seen[1] or "announced=False" not in seen[1]:
                failures.append("after disconnect, expected stopped and unannounced: %s" % seen[1])
            if "running=True" not in seen[2] or "announced=True" not in seen[2]:
                failures.append("after reconnect, expected running and announced: %s" % seen[2])
            else:
                print("  PASS  disconnect stops it and withdraws the announcement")
    
        if not pipe2:
            failures.append("cycled host never reported a pipe name")
        else:
            globals()["TOKEN"] = token2   # this host minted its own
            handle2 = connect(pipe2, timeout=10.0)
            if call(handle2, "ping").get("ok"):
                print("  PASS  answers again after being toggled off and back on")
            else:
                failures.append("no reply after the toggle cycle")
    except Exception as exc:
        failures.append("toggle cycle: %s: %s" % (type(exc).__name__, exc))
    finally:
        if handle2:
            try:
                handle2.close()
            except OSError:
                pass
        cycled.terminate()
        try:
            cycled.wait(timeout=5)
        except Exception:
            cycled.kill()

    print()
    if failures:
        print("FAILED")
        for f in failures:
            print("  - %s" % f)
        return 1

    print("Step 1 PASSED - the bridge works, without Revit.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
