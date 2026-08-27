#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Step 1 acceptance test - the pipe round trip.

Starts the Revit-free bridge host, connects over the named pipe, and checks
that ping comes back. Proves the transport without Revit, without the
discovery directory, and without any cross-process filesystem assumptions.

    python tests/test_bridge_roundtrip.py

Exit 0 = the bridge works. This is the "Prove it" line for Step 1 in
docs/27-build-order.md, in a form that can run in CI on a machine with no
Revit installed.
"""

import json
import os
import re
import subprocess
import sys
import time

HOST_EXE = os.path.join(
    "tests", "Heron.Bridge.TestHost", "bin", "x64", "Debug", "Heron.Bridge.TestHost.exe")
REVIT_VERSION = "2024"
HOST_LIFETIME_S = "45"
CONNECT_TIMEOUT_S = 10.0


def connect(pipe_name, timeout=CONNECT_TIMEOUT_S):
    """Open the named pipe, retrying until the server is listening."""
    path = "\\\\.\\pipe\\" + pipe_name
    deadline = time.time() + timeout
    last = None
    while time.time() < deadline:
        try:
            return open(path, "r+b", buffering=0)
        except OSError as exc:
            last = exc
            time.sleep(0.1)
    raise RuntimeError("could not open %s within %.0fs: %s" % (path, timeout, last))


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


def call(handle, op):
    handle.write((json.dumps({"op": op}) + "\n").encode("utf-8"))
    line = b""
    while not line.endswith(b"\n"):
        chunk = handle.read(1)
        if not chunk:
            raise RuntimeError("pipe closed before a full response arrived")
        line += chunk
    return json.loads(line.decode("utf-8").strip())


def main():
    if os.name != "nt":
        print("SKIP  Windows named pipes only.")
        return 0

    if not os.path.exists(HOST_EXE):
        print("FAIL  %s not found." % HOST_EXE)
        print("      dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=%s" % REVIT_VERSION)
        return 1

    print("Starting the bridge host (no Revit)...")
    proc = subprocess.Popen(
        [HOST_EXE, REVIT_VERSION, HOST_LIFETIME_S],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        universal_newlines=True, bufsize=1)

    failures = []
    handle = None
    try:
        # Read the pipe name from the host itself rather than from the
        # discovery file - this test is about the transport, not discovery.
        pipe_name = None
        deadline = time.time() + 20
        while time.time() < deadline:
            line = proc.stdout.readline()
            if not line:
                break
            m = re.search(r"Pipe\s+(heron\.\S+)", line)
            if m:
                pipe_name = m.group(1)
                break
        if not pipe_name:
            print("FAIL  host never reported a pipe name")
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
            reply = send_raw(handle, raw)
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

        # --- a SECOND connection, proving the two-listener design ---
        second = connect(pipe_name, timeout=3.0)
        try:
            reply = call(second, "ping")
            if reply.get("ok"):
                print("  PASS  second connection served immediately (two listeners)")
            else:
                failures.append("second connection returned %r" % reply)
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
