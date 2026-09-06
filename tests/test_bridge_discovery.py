#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Discovery prunes what is dead - INCLUDING a bridge that spoke another protocol.
Runs without Revit.

THE DEFECT THIS GUARDS, found 2026-09-06 by counting named pipes.

`revit_health` reported SIX connected Revit 2024 sessions. The machine held
not one Heron named pipe and not one of the processes; the entries were from
the day before. It then advised "restart that Revit" - six times, for six
Revits that did not exist. The tool whose entire job is answering *is Revit
reachable* was confidently wrong, which is the worst shape a check can take.

The cause was one `continue`:

    if bridge.protocol_version != PROTOCOL_VERSION:
        mismatched.append(bridge)
        continue                     # <- jumped past the liveness test

Every other path in `discover()` asks whether the PROCESS is still there
before calling an entry stale. The protocol branch returned first, so a
mismatched entry could never be classified stale and was never pruned, however
long it had been dead.

    The protocol says how to TALK to a bridge.
    The process says whether it is THERE.

They are different questions, and the second one still has to be asked.

    python tests/test_bridge_discovery.py
"""

import io
import json
import os
import shutil
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as bridge_client       # noqa: E402


def write_entry(folder, pid, protocol):
    """One address-book file, the shape the add-in actually publishes."""
    record = {
        "pid": pid,
        "pipeName": "heron.2024.%d" % pid,
        "revitVersion": "2024",
        "addinVersion": "0.1.0-testhost",
        "protocolVersion": protocol,
        "token": "x" * 32,
        "startedAt": "2026-09-05T21:26:50.0000000Z",
    }
    path = os.path.join(folder, "%d.json" % pid)
    with io.open(path, "w", encoding="utf-8") as fh:
        json.dump(record, fh)
    return path


def run():
    failures = []

    folder = tempfile.mkdtemp(prefix="heron-discovery-")
    original_dir = bridge_client.DISCOVERY_DIR
    original_running = bridge_client.bridge_process_is_running

    # Every process is dead. Nothing here should survive a prune.
    dead_pid = 424242
    other_protocol = bridge_client.PROTOCOL_VERSION + 1

    try:
        bridge_client.DISCOVERY_DIR = folder
        bridge_client.bridge_process_is_running = lambda pid: False

        mismatched_path = write_entry(folder, dead_pid, other_protocol)

        live, starting, stale, mismatched = bridge_client.discover(prune=True)

        # THE DEFECT: before the fix this came back as one "mismatched"
        # session, reported to the owner as connected, and the file stayed on
        # disk for ever.
        if mismatched:
            failures.append(
                "a DEAD bridge speaking another protocol was reported as "
                "mismatched-but-present. That is how six sessions that did not "
                "exist were reported as connected.")

        if mismatched_path not in stale:
            failures.append("a dead mismatched bridge was not classified stale")

        if os.path.exists(mismatched_path):
            failures.append("a dead mismatched bridge was not pruned from disk")

        if live or starting:
            failures.append("a dead bridge was reported live or starting")

        # THE OTHER HALF, and it matters just as much: a mismatched bridge that
        # is ALIVE must still be reported. Pruning that one would hide a real
        # Revit the owner needs told about - it is the entry that says
        # "restart this to finish updating", and that advice is only right when
        # there is something to restart.
        bridge_client.bridge_process_is_running = lambda pid: True
        alive_path = write_entry(folder, dead_pid + 1, other_protocol)

        live, starting, stale, mismatched = bridge_client.discover(prune=True)

        if len(mismatched) != 1:
            failures.append(
                "a LIVE bridge on another protocol was not reported as "
                "mismatched - the owner would never be told to restart it")

        if not os.path.exists(alive_path):
            failures.append("a live mismatched bridge was pruned from disk")

        # AND THE RULE THIS REPOSITORY REPEATS EVERYWHERE: cannot-tell is never
        # treated as dead. `bridge_process_is_running` returns None when
        # tasklist could not answer.
        bridge_client.bridge_process_is_running = lambda pid: None

        live, starting, stale, mismatched = bridge_client.discover(prune=True)

        if stale:
            failures.append(
                "an entry was called stale when the process could not be "
                "checked. Cannot-tell is not dead - deleting on a guess "
                "removes a Revit that is only slow to start")

        if not os.path.exists(alive_path):
            failures.append("an unknown-state entry was pruned from disk")

    finally:
        bridge_client.DISCOVERY_DIR = original_dir
        bridge_client.bridge_process_is_running = original_running
        shutil.rmtree(folder, ignore_errors=True)

    print("Bridge discovery")
    if failures:
        for line in failures:
            print("  FAIL  " + line)
        print("\nFAILED - a dead bridge is being reported as a connected Revit.")
        return 1

    print("  PASS  a dead bridge on another protocol is pruned, not reported")
    print("  PASS  a LIVE bridge on another protocol is still reported")
    print("  PASS  cannot-tell is never treated as dead")
    print("\nDiscovery tells a dead session from a mismatched one.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
