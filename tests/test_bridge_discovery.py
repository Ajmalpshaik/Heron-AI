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
import re
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

    # FINDING A MISMATCH IS HALF THE JOB; SAYING SO IS THE OTHER HALF.
    #
    # Added 2026-09-21. `discover()` has separated mismatched bridges since the
    # defect above, and `report_mismatched` exists to print the one sentence
    # that helps - "restart that Revit to finish updating". Five of the seven
    # commands called it on the NO-REVIT branch and not on the STILL-STARTING
    # one, which is the branch where it matters most: one Revit booting and
    # another stuck on an old add-in reads as "wait a moment", and the wait
    # never ends, because the second one needs restarting and nothing said so.
    # `cmd_list` and `cmd_ping` had it on both and are the proof it was meant
    # on both. FRAGMENT-ISSUES section 5b.
    source = io.open(os.path.join(ROOT, "mcp", "client",
                                  "heron_bridge_client.py"),
                     encoding="utf-8").read().split("\n")
    where = None
    silent = []
    seen = 0
    for number, line in enumerate(source):
        if re.match(r"^def cmd_", line):
            where = line.split("(")[0][4:]
        if "not live and starting" not in line:
            continue
        seen += 1
        # Everything up to the branch's own `return`.
        said = False
        for ahead in source[number:number + 12]:
            if "report_mismatched" in ahead:
                said = True
                break
            if re.match(r"\s+return ", ahead) and ahead is not source[number]:
                break
        if not said:
            silent.append("%s (line %d)" % (where, number + 1))
    if seen < 7:
        failures.append("only %d still-starting branch(es) found - this check "
                        "has stopped looking at what it thinks it is" % seen)
    if silent:
        failures.append("a still-starting branch does not report a protocol "
                        "mismatch: %s" % ", ".join(silent))

    # AND LOOKING IS STILL NOT CLAIMING. Added 2026-09-21, beside the check
    # above for the same reason: both are about what the seven commands do
    # with what `discover()` handed them.
    #
    # `BridgeServer.cs` exempts exactly two operations from the lease, and
    # says why - "asking who has this must not be the act of claiming it".
    # `count_elements` is not one of them, and `cmd_count` with no session
    # named asked it of every connected Revit, taking all of them for five
    # minutes. THE EXEMPT LIST IS DERIVED FROM THE C#, not typed here, so a
    # third exemption added there makes this check say so rather than quietly
    # go on describing two.
    server = io.open(os.path.join(ROOT, "revit", "Heron.Bridge",
                                  "BridgeServer.cs"), encoding="utf-8").read()
    gate = server[server.index("PING AND INFO NEED NO LEASE"):]
    gate = gate[:gate.index("HANDING IT BACK")]
    exempt = set(re.findall(r'op == "(\w+)"', gate))
    if exempt != {"ping", "info"}:
        failures.append("the bridge now exempts %s from the lease, not just "
                        "ping and info - every loop over `live` in the client "
                        "has to be read again" % ", ".join(sorted(exempt)))

    counter = source[[j for j, l in enumerate(source)
                      if re.match(r"^def cmd_count", l)][0]:]
    counter = "\n".join(counter[:counter.index("def fragment_risk(path):")]
                         if "def fragment_risk(path):" in counter else counter[:120])
    # THE CALL, NOT THE WORD. The comment explaining this very rule names
    # `count_elements` several lines above the call, so matching the bare word
    # reported the fixed code as broken - measured by watching it do exactly
    # that.
    call = 'bridge.request("count_elements")'
    asked = counter.index(call) if call in counter else -1
    # THE FLAG, which is what the code decides on - `availability()` builds
    # the sentence a person reads and a reworded sentence must not be able to
    # turn the guard off.
    looked = counter.index("lease_state(") if "lease_state(" in counter else -1
    if asked < 0:
        failures.append("cmd_count no longer asks count_elements - this check "
                        "has stopped looking at what it thinks it is")
    elif looked < 0 or looked > asked:
        failures.append("cmd_count asks count_elements of every connected "
                        "Revit without asking who holds it first, so running "
                        "it takes every Revit the user has open")

    print("Bridge discovery")
    if failures:
        for line in failures:
            print("  FAIL  " + line)
        print("\nFAILED - a dead bridge is being reported as a connected Revit.")
        return 1

    print("  PASS  a dead bridge on another protocol is pruned, not reported")
    print("  PASS  a LIVE bridge on another protocol is still reported")
    print("  PASS  cannot-tell is never treated as dead")
    print("  PASS  all %d still-starting branches report a protocol mismatch"
          % seen)
    print("  PASS  %s are the only lease-exempt ops, and counting asks who "
          "holds a Revit first" % " and ".join(sorted(exempt)))
    print("\nDiscovery tells a dead session from a mismatched one.")
    return 0


if __name__ == "__main__":
    sys.exit(run())
