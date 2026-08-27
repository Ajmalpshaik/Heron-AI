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

    python mcp/client/heron_bridge_client.py           # list connected sessions
    python mcp/client/heron_bridge_client.py ping      # ping every session
    python mcp/client/heron_bridge_client.py ping 24312
    python mcp/client/heron_bridge_client.py doctor    # diagnose a failure

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
import subprocess
import sys
import time

# DERIVED state: machine-local, never roaming (D-17). A roaming discovery
# file would follow the user to a PC where that process does not exist.
DISCOVERY_DIR = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Heron", "bridges")
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


def process_is_running(pid):
    """
    Is that process alive? Uses tasklist rather than a library so the client
    stays dependency-free.

    Returns None when it cannot tell - and "cannot tell" must never be
    treated as "dead".
    """
    if not pid:
        return None
    try:
        out = subprocess.check_output(
            ["tasklist", "/FI", "PID eq %s" % pid, "/NH"],
            stderr=subprocess.STDOUT, universal_newlines=True,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
        return str(pid) in out
    except Exception:
        return None


def discover(prune=True):
    """
    Read the address book, then verify each entry by talking to it.

    Returns (live, starting, stale).

    A bridge that does not answer is NOT automatically stale. Revit takes
    the better part of a minute to start, and the add-in publishes its file
    before the listener is ready - so a single failed ping during startup
    used to delete a perfectly good entry, and the user saw "no Revit is
    connected" while Revit was visibly loading. Found by running it.

    So the liveness test is the PROCESS, not the reply:

        answers            -> live
        no reply, alive    -> starting. Leave the file alone
        no reply, gone     -> stale. Safe to remove
        no reply, unknown  -> leave it alone. Never delete on a guess
    """
    live, starting, stale = [], [], []
    if not os.path.isdir(DISCOVERY_DIR):
        return live, starting, stale

    for name in sorted(os.listdir(DISCOVERY_DIR)):
        if not name.endswith(".json"):
            continue
        path = os.path.join(DISCOVERY_DIR, name)
        try:
            with open(path, "r", encoding="utf-8") as fh:
                record = json.load(fh)
        except (OSError, ValueError):
            stale.append(path)          # unreadable file, nothing to preserve
            continue

        bridge = Bridge(record, path)
        if bridge.request("ping") is not None:
            live.append(bridge)
            continue

        alive = process_is_running(bridge.pid)
        if alive is False:
            stale.append(path)
        else:
            starting.append(bridge)     # running, or we cannot tell

    if prune:
        for path in stale:
            try:
                os.remove(path)
            except OSError:
                pass

    return live, starting, stale


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
    live, starting, stale = discover()
    if not live and starting:
        print("Revit is still starting.")
        print("")
        for b in starting:
            print("  Revit %s (session %s) is running but its bridge is not answering yet." %
                  (b.revit_version, b.pid))
        print("")
        print("Give it a few seconds and try again.")
        return 1
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
    if starting:
        print("")
        print("(%d starting up, not ready yet)" % len(starting))
    if stale:
        print("")
        print("(removed %d stale entry(ies))" % len(stale))
    return 0


def cmd_ping(pid=None):
    live, starting, _ = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
        return 1
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



def cmd_doctor():
    """
    Self-diagnostics - the prototype of HERON-OPS-DIA-005.

    Everything needed to explain a failure, in one paste. Reports what it
    found rather than what it expected, so the output is useful even when
    the conclusion is wrong.
    """
    import platform

    print("Heron doctor")
    print("=" * 60)
    print("")
    print("Environment")
    print("  python          %s" % sys.version.split()[0])
    print("  os              %s" % platform.platform())
    print("  LOCALAPPDATA    %s" % os.environ.get("LOCALAPPDATA", "(not set)"))
    print("")

    print("Discovery directory")
    print("  path            %s" % DISCOVERY_DIR)
    print("  exists          %s" % os.path.isdir(DISCOVERY_DIR))
    entries = []
    if os.path.isdir(DISCOVERY_DIR):
        entries = [f for f in sorted(os.listdir(DISCOVERY_DIR)) if f.endswith(".json")]
        print("  files           %s" % (", ".join(entries) if entries else "(none)"))
    print("")

    if not entries:
        print("  No Revit has announced itself.")
        print("")
        print("  Most likely, in order:")
        print("    1. Revit is not running.")
        print("    2. Revit is running but Connect Heron was never pressed.")
        print("       A Revit that was never connected is invisible here, by design.")
        print("    3. The add-in did not load. Check the log below.")
        print("")

    print("Bridges")
    for name in entries:
        path = os.path.join(DISCOVERY_DIR, name)
        try:
            with open(path, "r", encoding="utf-8") as fh:
                record = json.load(fh)
        except (OSError, ValueError) as exc:
            print("  %s  UNREADABLE: %s" % (name, exc))
            continue

        bridge = Bridge(record, path)
        print("  %s" % name)
        print("    pipe          %s" % bridge.pipe_name)
        print("    revit         %s" % bridge.revit_version)
        print("    addin         %s" % bridge.addin_version)
        print("    protocol      %s (this client expects %s)"
              % (bridge.protocol_version, PROTOCOL_VERSION))
        reply = bridge.request("ping")
        if reply is not None:
            print("    ping          %s" % reply)
        else:
            alive = process_is_running(bridge.pid)
            if alive is True:
                print("    ping          no reply, but process %s IS running" % bridge.pid)
                print("                  Revit is probably still starting up.")
            elif alive is False:
                print("    ping          no reply, and process %s is gone - stale entry" % bridge.pid)
            else:
                print("    ping          no reply, and could not determine whether %s is running"
                      % bridge.pid)
    print("")

    log = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Heron", "logs", "addin.log")
    print("Add-in log")
    print("  path            %s" % log)
    if os.path.exists(log):
        try:
            with open(log, "r", encoding="utf-8", errors="replace") as fh:
                tail = fh.readlines()[-15:]
            print("  last %d lines:" % len(tail))
            for line in tail:
                print("    %s" % line.rstrip())
        except OSError as exc:
            print("  unreadable: %s" % exc)
    else:
        print("  NOT FOUND - the add-in has never started.")
        print("  That means Revit did not load it. Check that the manifest is at:")
        print("    %%APPDATA%%\Autodesk\Revit\Addins\<version>\Heron.addin")
    print("")
    print("=" * 60)
    print("Paste all of the above when reporting a problem.")
    return 0


def main(argv):
    if os.name != "nt":
        print("Heron's bridge uses Windows named pipes. Revit is Windows-only.")
        return 2

    if len(argv) < 2:
        return cmd_list()
    if argv[1] == "ping":
        return cmd_ping(argv[2] if len(argv) > 2 else None)
    if argv[1] == "doctor":
        return cmd_doctor()

    print(__doc__.strip())
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
