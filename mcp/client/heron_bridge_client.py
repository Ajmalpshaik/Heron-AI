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
LOG_DIR = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Heron", "logs")
PROTOCOL_VERSION = 1
CONNECT_TIMEOUT_S = 2.0


def newest_log():
    """The add-in writes one log file per day so retention can mean something.
    Return the most recent, or None if it has never run."""
    try:
        files = [os.path.join(LOG_DIR, f) for f in os.listdir(LOG_DIR)
                 if f.startswith("addin") and f.endswith(".log")]
    except OSError:
        return None
    return max(files, key=os.path.getmtime) if files else None


class Bridge(object):
    """One connected Revit session."""

    def __init__(self, record, path):
        self.pid = record.get("pid")
        self.pipe_name = record.get("pipeName")
        self.revit_version = record.get("revitVersion")
        self.addin_version = record.get("addinVersion")
        self.protocol_version = record.get("protocolVersion")
        self.started_at = record.get("startedAt")
        # Minted when a session connects and gone when it disconnects, so a
        # token read before a reconnect is refused rather than quietly served.
        # Always re-read the discovery file; never remember one.
        self.token = record.get("token")
        self.path = path

    @property
    def pipe_path(self):
        return r"\\.\pipe" + "\\" + self.pipe_name

    def request(self, op, timeout=CONNECT_TIMEOUT_S):
        """Send one request, return the parsed response. None if unreachable.

        Every request carries this session's token. The bridge checks it before
        it will even say whether an operation exists.
        """
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
            handle.write((json.dumps({"op": op, "token": self.token}) + "\n").encode("utf-8"))
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


def process_is_running(pid, image="Revit.exe"):
    """
    Is that process alive, AND is it still the program we think it is?

    Both halves matter. Windows reuses process ids: when Revit crashes and
    the id is handed to something else, asking only "does this number exist"
    answers yes forever. The entry is then filed as "still starting", never
    pruned, and the session picker offers a Revit that is not there - one
    keystroke from the wrong model.

    Uses tasklist rather than a library so the client stays dependency-free.

    Returns None when it cannot tell - and "cannot tell" must never be
    treated as "dead".
    """
    if not pid:
        return None
    try:
        out = subprocess.check_output(
            ["tasklist", "/FI", "PID eq %s" % pid, "/FI", "IMAGENAME eq %s" % image, "/NH"],
            stderr=subprocess.STDOUT, universal_newlines=True,
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0))
    except Exception:
        return None

    # A filter that matches nothing prints an INFO line, not a row. Look for
    # the image name at the start of a row rather than for the id anywhere in
    # the text, which would also match a memory figure or a session number.
    for line in out.splitlines():
        line = line.strip()
        if line.lower().startswith(image.lower()):
            return True
    return False


#: Programs a Heron bridge can legitimately be running inside. The
#: Revit-free host in tests/ is not Revit.exe, so matching only Revit would
#: call every test bridge dead.
BRIDGE_IMAGES = ("Revit.exe", "Heron.Bridge.TestHost.exe", "dotnet.exe")


def bridge_process_is_running(pid):
    """
    Is a bridge still alive at that process id?

    True only if the id belongs to a program a bridge actually runs in.
    None if it could not be determined - which is never treated as dead.
    """
    unknown = False
    for image in BRIDGE_IMAGES:
        answer = process_is_running(pid, image)
        if answer is True:
            return True
        if answer is None:
            unknown = True
    return None if unknown else False


def discover(prune=True):
    """
    Read the address book, then verify each entry by talking to it.

    Returns (live, starting, stale, mismatched).

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

    A bridge announcing a protocol this client does not speak is separated
    out rather than talked to.
    """
    live, starting, stale, mismatched = [], [], [], []
    if not os.path.isdir(DISCOVERY_DIR):
        return live, starting, stale, mismatched

    for name in sorted(os.listdir(DISCOVERY_DIR)):
        if not name.endswith(".json"):
            continue
        path = os.path.join(DISCOVERY_DIR, name)
        try:
            with open(path, "r", encoding="utf-8") as fh:
                record = json.load(fh)
        except (OSError, ValueError):
            # An unreadable file is NOT proof of a dead bridge - it may be
            # locked, or half-written this instant. Deleting it here removed a
            # live Revit from the list, and nothing ever re-announces it. Same
            # rule as everywhere else: prove the process is gone first.
            pid = pid_from_filename(name)
            if pid is not None and bridge_process_is_running(pid) is False:
                stale.append(path)
            continue

        bridge = Bridge(record, path)

        # A bridge speaking another protocol is refused rather than half-used.
        # Reading it with the wrong assumptions is how a wrong answer looks
        # exactly like a right one.
        if bridge.protocol_version != PROTOCOL_VERSION:
            mismatched.append(bridge)
            continue

        if bridge.request("ping") is not None:
            live.append(bridge)
            continue

        alive = bridge_process_is_running(bridge.pid)
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

    return live, starting, stale, mismatched


def pid_from_filename(name):
    """The discovery file is named <pid>.json - the last fact left when its
    contents cannot be read."""
    try:
        return int(os.path.splitext(name)[0])
    except (TypeError, ValueError):
        return None


def describe(bridge, index):
    """
    One line for the picker.

    Deliberately shows Revit version and process - never asks the user to
    read a raw process number as an identity, and never shows a document
    name from the file. Once Step 2 exists, the live-queried project name
    goes here (docs/25 section 2a).
    """
    return "  %d) Revit %s   (session %s, add-in %s)" % (
        index, bridge.revit_version, bridge.pid, bridge.addin_version)


def report_mismatched(mismatched):
    """A version mismatch is a refusal with an instruction, not a warning."""
    if not mismatched:
        return
    print("")
    for b in mismatched:
        print("  Revit %s (session %s) speaks protocol %s; this client speaks %s."
              % (b.revit_version, b.pid, b.protocol_version, PROTOCOL_VERSION))
    print("  Restart that Revit to finish updating. Heron will not talk to it "
          "until the two agree.")


def cmd_list():
    live, starting, stale, mismatched = discover()
    if not live and starting:
        print("Revit is still starting.")
        print("")
        for b in starting:
            print("  Revit %s (session %s) is running but its bridge is not answering yet." %
                  (b.revit_version, b.pid))
        print("")
        print("Give it a few seconds and try again.")
        report_mismatched(mismatched)
        return 1
    if not live:
        print("No Revit is connected.")
        print("")
        print("Open Revit, then press  Heron AI > Heron  on the ribbon to connect.")
        print("A Revit that was never connected is invisible here, by design.")
        if stale:
            print("")
            print("Removed %d stale entry(ies) from a Revit that did not shut down cleanly."
                  % len(stale))
        report_mismatched(mismatched)
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
    report_mismatched(mismatched)
    return 0


def cmd_ping(pid=None):
    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
        report_mismatched(mismatched)
        return 1
    if not live:
        print("No Revit is connected. Press Heron on the ribbon to connect first.")
        report_mismatched(mismatched)
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
        print("    2. Revit is running but the Heron button was never pressed.")
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
        agreed = bridge.protocol_version == PROTOCOL_VERSION
        print("    protocol      %s (this client expects %s)%s"
              % (bridge.protocol_version, PROTOCOL_VERSION,
                 "" if agreed else "   MISMATCH - restart that Revit"))
        if not agreed:
            print("    ping          not attempted; Heron will not talk across protocols")
            continue

        reply = bridge.request("ping")
        if reply is not None:
            print("    ping          %s" % reply)
        else:
            alive = bridge_process_is_running(bridge.pid)
            if alive is True:
                print("    ping          no reply, but process %s IS running" % bridge.pid)
                print("                  Revit is probably still starting up.")
            elif alive is False:
                print("    ping          no reply, and process %s is gone - stale entry" % bridge.pid)
            else:
                print("    ping          no reply, and could not determine whether %s is running"
                      % bridge.pid)
    print("")

    log = newest_log()
    print("Add-in log")
    print("  path            %s" % (log or os.path.join(LOG_DIR, "addin-<date>.log")))
    if log and os.path.exists(log):
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
        print(r"    %APPDATA%\Autodesk\Revit\Addins\<version>\Heron.addin")
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
