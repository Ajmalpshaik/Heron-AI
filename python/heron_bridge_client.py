#!/usr/bin/env python3
# Heron-Agent:  HERON-SES-DIS-001, HERON-SES-LST-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Heron bridge client - Step 1.

Finds every connected Revit, talks to one, and proves the chain works.
No Revit API, no MCP yet. Just: can something outside Revit reach inside it?

    python python/heron_bridge_client.py           # list connected sessions
    python python/heron_bridge_client.py ping      # ping every session
    python python/heron_bridge_client.py ping 24312

Two rules from the field notes (docs/00e, docs/25) are enforced here:

  * The discovery file is an ADDRESS BOOK. Static facts only. The open
    document is never read from it - that is what produced the stale-name
    trap, where the list still said BL006A after BL003A was opened.

  * A discovery file is NOT proof a bridge is alive. Revit crashes without
    cleaning up. Every session is verified by actually talking to it, and
    dead entries are removed.
"""

import json
import os
import sys
import time

DISCOVERY_DIR = os.path.join(os.environ.get("APPDATA", ""), "Heron", "bridges")
PROTOCOL_VERSION = 1
CONNECT_TIMEOUT_S = 2.0


class Bridge(object):
    """One connected Revit session."""

    def __init__(self, record, path):
        self.pid = record.get("pid")
        self.pipe_name = record.get("pipeName")
        self.revit_version = record.get("revitVersion")
        self.addin_version = record.get("addinVersion")
        self.protocol_version = record.get("protocolVersion")
        self.started_at = record.get("startedAt")
        self.path = path

    @property
    def pipe_path(self):
        return r"\\.\pipe" + "\\" + self.pipe_name

    def request(self, op, timeout=CONNECT_TIMEOUT_S):
        """Send one request, return the parsed response. None if unreachable."""
        deadline = time.time() + timeout
        handle = None
        while handle is None:
            try:
                handle = open(self.pipe_path, "r+b", buffering=0)
            except OSError:
                if time.time() >= deadline:
                    return None
                time.sleep(0.05)

        try:
            handle.write((json.dumps({"op": op}) + "\n").encode("utf-8"))
            line = b""
            while not line.endswith(b"\n"):
                chunk = handle.read(1)
                if not chunk:
                    break
                line += chunk
            if not line:
                return None
            return json.loads(line.decode("utf-8").strip())
        except (OSError, ValueError):
            return None
        finally:
            try:
                handle.close()
            except OSError:
                pass


def discover(prune=True):
    """
    Read the address book, then verify each entry by talking to it.

    Returns (live, stale). Stale files are deleted when prune is set - a
    crashed Revit never removes its own.
    """
    live, stale = [], []
    if not os.path.isdir(DISCOVERY_DIR):
        return live, stale

    for name in sorted(os.listdir(DISCOVERY_DIR)):
        if not name.endswith(".json"):
            continue
        path = os.path.join(DISCOVERY_DIR, name)
        try:
            with open(path, "r", encoding="utf-8") as fh:
                record = json.load(fh)
        except (OSError, ValueError):
            stale.append(path)
            continue

        bridge = Bridge(record, path)
        if bridge.request("ping") is not None:
            live.append(bridge)
        else:
            stale.append(path)

    if prune:
        for path in stale:
            try:
                os.remove(path)
            except OSError:
                pass

    return live, stale


def describe(bridge, index):
    """
    One line for the picker.

    Deliberately shows Revit version and process - never asks the user to
    read a raw process number as an identity, and never shows a document
    name from the file. Once Step 2 exists, the live-queried project name
    goes here (docs/25 section 2a).
    """
    warn = ""
    if bridge.protocol_version != PROTOCOL_VERSION:
        warn = "  [protocol %s, expected %s - restart Revit to finish updating]" % (
            bridge.protocol_version, PROTOCOL_VERSION)
    return "  %d) Revit %s   (session %s, add-in %s)%s" % (
        index, bridge.revit_version, bridge.pid, bridge.addin_version, warn)


def cmd_list():
    live, stale = discover()
    if not live:
        print("No Revit is connected.")
        print("")
        print("Open Revit, then press  Heron AI > Connect Heron  on the ribbon.")
        print("A Revit that was never connected is invisible here, by design.")
        if stale:
            print("")
            print("Removed %d stale entry(ies) from a Revit that did not shut down cleanly."
                  % len(stale))
        return 1

    print("Connected Revit sessions:")
    print("")
    for i, bridge in enumerate(live, 1):
        print(describe(bridge, i))
    if stale:
        print("")
        print("(removed %d stale entry(ies))" % len(stale))
    return 0


def cmd_ping(pid=None):
    live, _ = discover()
    if not live:
        print("No Revit is connected. Press Connect Heron on the ribbon first.")
        return 1

    if pid is not None:
        live = [b for b in live if str(b.pid) == str(pid)]
        if not live:
            print("No connected Revit with session %s." % pid)
            return 1

    failures = 0
    for bridge in live:
        started = time.time()
        pong = bridge.request("ping")
        info = bridge.request("info")
        elapsed_ms = (time.time() - started) * 1000.0

        if pong and pong.get("ok") and pong.get("pong"):
            print("pong  <-  Revit %s, session %s   (%.0f ms)" % (
                bridge.revit_version, bridge.pid, elapsed_ms))
            if info and info.get("ok"):
                print("        add-in %s, protocol %s" % (
                    info.get("addinVersion"), info.get("protocolVersion")))
        else:
            print("NO REPLY from session %s" % bridge.pid)
            failures += 1

    return 1 if failures else 0


def main(argv):
    if os.name != "nt":
        print("Heron's bridge uses Windows named pipes. Revit is Windows-only.")
        return 2

    if len(argv) < 2:
        return cmd_list()
    if argv[1] == "ping":
        return cmd_ping(argv[2] if len(argv) > 2 else None)

    print(__doc__.strip())
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
