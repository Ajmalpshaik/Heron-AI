#!/usr/bin/env python3
# Heron-Agent:  HERON-SES-DIS-001, HERON-SES-LST-002, HERON-MCP-CON-002, HERON-MCP-REC-009
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
    python mcp/client/heron_bridge_client.py count     # count elements in the open model
    python mcp/client/heron_bridge_client.py count 24312
    python mcp/client/heron_bridge_client.py doctor    # diagnose a failure

Two rules from the field notes (docs/00e, docs/25) are enforced here:

  * The discovery file is an ADDRESS BOOK. Static facts only. The open
    document is never read from it - that is what produced the stale-name
    trap, where the list still said BL006A after BL003A was opened.

  * A discovery file is NOT proof a bridge is alive. Revit crashes without
    cleaning up. Every session is verified by actually talking to it, and
    dead entries are removed.
"""

import io
import json
import os
import subprocess
import sys
import threading
import time
import uuid

# DERIVED state: machine-local, never roaming (D-17). A roaming discovery
# file would follow the user to a PC where that process does not exist.
DISCOVERY_DIR = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Heron", "bridges")
LOG_DIR = os.path.join(os.environ.get("LOCALAPPDATA", ""), "Heron", "logs")
PROTOCOL_VERSION = 2
CONNECT_TIMEOUT_S = 2.0

# The settings the ADD-IN reads, read from this side too. Stdlib only, so the
# client stays runnable on a machine where nothing else is installed - which is
# the whole reason `doctor` exists.
import heron_config                            # noqa: E402

# THIS CHAT, for the lifetime of this process - which is the lifetime of one
# chat. Sent with every request so the add-in's lease can tell "the same chat
# coming back after a reconnect" from "a second chat arriving". Those two look
# identical at the pipe, and confusing them is what made the old behaviour
# chop a job in half.
#
# NOT the session token: that is minted per Revit connect and read from the
# discovery file, so every chat talking to one Revit reads the SAME token. It
# authenticates the Revit, not the chat, and cannot tell them apart.
#
# Random and meaningless on purpose. It identifies a conversation, never a
# person or a machine.
#
# HERON_CLIENT_ID OVERRIDES IT, and that exists because the sentence above is
# only true of a long-running client. The MCP server is one process for one
# chat, so a fresh id per process is exactly right. A COMMAND LINE IS NOT: each
# invocation is its own process, so every command looked like a NEW chat and
# the previous one's lease sat orphaned for five minutes. Two commands in a row
# were refused - found 2026-09-06, one fragment into the first proving run.
#
# Setting it makes several commands one conversation, which is what they are.
CLIENT_ID = os.environ.get("HERON_CLIENT_ID") or uuid.uuid4().hex[:12]

# How long to wait for an answer once the bridge has accepted the connection.
#
# This must stay comfortably ABOVE any limit the bridge applies to its own work.
# From Step 2 a request is queued onto Revit's thread and only runs when Revit is
# idle, so a genuinely-running job can be quiet for a long time. Set this too low
# and the client reports "no answer" for work Revit would have finished - the
# worst kind of wrong, because the user then retries something already running.
#
# DERIVED, not chosen. It used to be the constant 90.0, which was correct only
# while the add-in's own operationTimeoutSeconds stayed at its default of 60.
# That value is configurable; this one was not. Raise the add-in's to 120 for a
# slow model and the ordering inverted silently - the client gave up first, and
# "Revit started it and is still working" was replaced by "no answer", which is
# the message that tells the user nothing. Now the add-in's setting decides
# both, so they cannot disagree.
#
# With the defaults this is 60 + 30 = 90.0, exactly what it was before, so
# nothing proven in Steps 1-5 changes behaviour.
RESPONSE_TIMEOUT_S = heron_config.response_timeout()

#: Heron's own version. Checked against Directory.Build.props by
#: tools/check-metadata.py, so the two can never quietly disagree - a version
#: that is typed in two places is a version that is wrong in one of them.
HERON_VERSION = "0.1.0"

#: Transport retries. Three attempts with a widening gap, so a bridge that is
#: mid-restart is given time to come back rather than hammered while it does.
RETRY_ATTEMPTS = 3
RETRY_BACKOFF_S = 0.15


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
        self._handle = None      # kept open across requests, see request()

    @property
    def pipe_path(self):
        return r"\\.\pipe" + "\\" + self.pipe_name

    def close(self):
        """Drop the connection. Safe to call more than once."""
        handle, self._handle = getattr(self, "_handle", None), None
        if handle is not None:
            try:
                handle.close()
            except OSError:
                pass

    def _connect(self, timeout):
        """Open the pipe, retrying while the bridge finishes standing up."""
        deadline = time.time() + timeout
        while True:
            try:
                return open(self.pipe_path, "r+b", buffering=0)
            except OSError:
                if time.time() >= deadline:
                    return None
                time.sleep(0.05)

    def _read_line(self, handle, timeout):
        """
        One newline-terminated response, or None if it never arrives.

        The read runs on a helper thread because a pipe handle cannot be given a
        deadline of its own here. On timeout the caller closes the handle, which
        unblocks the thread - it is a daemon, so it can never hold the process
        open either way.
        """
        result = {}

        def reader():
            buffer = b""
            try:
                while not buffer.endswith(b"\n"):
                    # In chunks, not byte at a time: a Step 4 answer listing
                    # several hundred elements is one read instead of thousands.
                    chunk = handle.read(4096)
                    if not chunk:
                        break
                    buffer += chunk
            except (OSError, ValueError):
                buffer = b""
            result["line"] = buffer

        worker = threading.Thread(target=reader)
        worker.daemon = True
        worker.start()
        worker.join(timeout)
        if worker.is_alive():
            return None            # still waiting; the caller closes the handle
        return result.get("line") or None

    def request(self, op, timeout=CONNECT_TIMEOUT_S, response_timeout=RESPONSE_TIMEOUT_S,
                op_args=None, idempotent=True):
        """
        Send one request, return the parsed response. None if unreachable.

        The connection is kept and reused. The bridge hands the session to
        whichever connection is newest, so a client that reconnects for every
        request spends its time preempting itself.

        Every request carries this session's token. The bridge checks it before
        it will even say whether an operation exists.

        RETRIES ONLY WHAT IS SAFE TO RETRY. A transport fault - the pipe was not
        open, or the write failed - means the request never left this machine,
        so it can always be sent again. An answer that is lost AFTER the request
        was sent is a different thing entirely: whether Revit ran it is unknown.

        Everything up to Step 5 only reads, so asking twice costs nothing and
        `idempotent` defaults to True. Step 6 writes MUST pass False - repeating
        a move because the answer went missing would move the same ducts twice,
        and "it looked like it failed" is exactly how that happens.
        """
        body = {"op": op, "token": self.token, "client": CLIENT_ID}
        if op_args:
            # Arguments sit alongside op and token, never nested one level down:
            # the bridge reads top-level keys only, deliberately.
            body.update(op_args)
        payload = (json.dumps(body) + "\n").encode("utf-8")

        for attempt in range(1, RETRY_ATTEMPTS + 1):
            if attempt > 1:
                # Backoff, so a bridge that is mid-restart is given time to come
                # back rather than hammered while it does.
                time.sleep(RETRY_BACKOFF_S * (2 ** (attempt - 2)))

            handle = getattr(self, "_handle", None)
            if handle is None:
                handle = self._connect(timeout)
                if handle is None:
                    continue          # never opened; nothing was sent
                self._handle = handle

            try:
                handle.write(payload)
            except (OSError, ValueError):
                # The write failed, so the request never left this machine.
                # Always safe to try again, whatever the operation was.
                self.close()
                continue

            line = self._read_line(handle, response_timeout)

            if line is None:
                self.close()

                # THE REQUEST WAS SENT AND THE ANSWER WAS LOST. Whether Revit
                # ran it is unknown, and no amount of looking from here can
                # settle that.
                #
                # For a read, asking again costs nothing. For a write it could
                # do the work twice - move the same ducts 200 mm up, twice -
                # and "it looked like it failed" is exactly how that happens.
                # So retrying past this point is the CALLER's decision, and the
                # default for anything that changes a model must be False.
                if not idempotent:
                    return {"ok": False, "error": "unknown_outcome",
                            "message": ("The request reached Revit but the answer was lost, so "
                                        "Heron cannot tell whether it ran. Check the model "
                                        "before trying again - repeating it could do the work "
                                        "twice.")}
                continue

            try:
                return json.loads(line.decode("utf-8").strip())
            except ValueError:
                # A reply arrived and was unreadable. That is not a transport
                # fault, and repeating the request will not produce a different
                # answer.
                return None

        return None


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


def lease_state(bridge):
    """
    Who holds this Revit: (in_use, mine, seconds_remaining).

    Asks `info`, which is lease-EXEMPT on purpose - looking must never be the
    act of claiming. Before the lease existed there was nothing truthful to
    return here at all, and the only way to know another chat was using a
    Revit was for a person to remember and say so.

    An older add-in (protocol 1) does not report it. That returns
    (None, None, 0) - unknown, which is not the same as free and must not be
    displayed as it.
    """
    reply = bridge.request("info")
    if reply is None or not reply.get("ok") or "inUse" not in reply:
        return None, None, 0
    return bool(reply.get("inUse")), bool(reply.get("mine")), int(reply.get("leaseSecondsRemaining", 0))


def availability(bridge):
    """
    The (free) / (in use) column, as one short phrase.

    docs/25 called this "the missing data that makes the list honest".
    """
    in_use, mine, seconds = lease_state(bridge)
    if in_use is None:
        return "(availability unknown)"
    if not in_use:
        return "(free)"
    if mine:
        return "(in use by this chat)"
    minutes = max(1, int((seconds + 59) // 60))
    return "(in use by another chat, ~%d min left)" % minutes


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
    out rather than talked to - but it is still put to the same liveness test,
    because a mismatched bridge whose process is GONE is just as stale as any
    other. Skipping that test is what left six dead sessions being reported as
    connected (2026-09-06).
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
        #
        # BUT A MISMATCH IS NOT A REASON TO SKIP THE LIVENESS TEST, and this
        # branch used to `continue` straight past it. A protocol-1 bridge that
        # died yesterday was therefore never classified stale and never pruned:
        # `revit_health` reported six connected sessions when the machine held
        # not one Heron pipe, and told the owner to "restart that Revit" for
        # six Revits that did not exist. Found 2026-09-06 by counting the named
        # pipes and finding none.
        #
        # The protocol says how to TALK to a bridge. The process says whether
        # it is THERE. They are different questions and the second one still
        # has to be asked.
        if bridge.protocol_version != PROTOCOL_VERSION:
            if bridge_process_is_running(bridge.pid) is False:
                stale.append(path)
            else:
                # Alive, or we cannot tell - and "cannot tell" is never
                # treated as dead, the same rule as everywhere else here.
                mismatched.append(bridge)
            continue

        if bridge.request("ping") is not None:
            live.append(bridge)
            continue

        bridge.close()          # not usable; do not leave the pipe held open
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

    for bridge in live:
        bridge.close()
    return 1 if failures else 0



def cmd_count(pid=None):
    """
    Step 2 - the first question Heron asks a real model.

    The answer always names the document. A bare number is how somebody acts
    on a count that came from a model they were not looking at.
    """
    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
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
        reply = bridge.request("count_elements")

        if reply is None:
            print("No reply from Revit %s (session %s)." % (bridge.revit_version, bridge.pid))
            failures += 1
        elif reply.get("ok"):
            unsaved = "  (unsaved changes)" if reply.get("unsaved") else ""
            print("%s elements in %s%s" % (
                "{:,}".format(reply.get("count", 0)), reply.get("document"), unsaved))
            print("        Revit %s, session %s - %s" % (
                bridge.revit_version, bridge.pid, reply.get("counts")))
            if reply.get("documentPath"):
                print("        %s" % reply.get("documentPath"))
        else:
            # revit_busy and still_running are ordinary answers, not faults -
            # they say what to do next, so print the message rather than a code.
            print("Revit %s (session %s): %s" % (
                bridge.revit_version, bridge.pid, reply.get("message") or reply.get("error")))
            failures += 1

    for bridge in live:
        bridge.close()
    return 1 if failures else 0


def cmd_fragment(name):
    """
    Run one fragment's C# against the open model - D-28's executor, reached.

    THE SOURCE IS SENT, NOT A NAME. Revit has no idea where the fragment
    library lives and should not: the add-in would then need a path into
    somebody's repository, and a fragment could be changed under it between
    the check and the run. The client reads the file it just checked and sends
    exactly that text.

    READ ONLY. The operation opens no transaction, so Revit refuses anything
    that would change the model - the guarantee is Revit's rather than ours.
    """
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    source_path = os.path.join(root, "brain", "fragments", name, "impl", "any", "fragment.cs")

    if not os.path.isfile(source_path):
        print("No fragment called '%s' - looked for %s" % (name, source_path))
        return 2

    with io.open(source_path, "r", encoding="utf-8") as fh:
        source = fh.read()

    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
        return 1
    if not live:
        print("No Revit is connected. Press Heron on the ribbon to connect first.")
        report_mismatched(mismatched)
        return 1

    failures = 0
    for bridge in live:
        reply = bridge.request("run_fragment_read",
                               op_args={"name": name, "source": source},
                               response_timeout=120.0)

        if reply is None:
            print("No reply from Revit %s (session %s)." % (bridge.revit_version, bridge.pid))
            failures += 1
            continue

        if not reply.get("ok"):
            # A compile failure or a throw is a FINDING, not a crash. It is
            # the most useful thing this whole path produces on the day a
            # fragment is wrong, so it is printed in full.
            print("%s  [%s]" % (name, reply.get("error")))
            print("    %s" % reply.get("message"))
            failures += 1
            continue

        print("%s - ran on Revit %s (session %s)" % (name, bridge.revit_version, bridge.pid))
        provides = reply.get("provides") or {}
        if not provides:
            print("    left nothing behind")
        for key in sorted(provides):
            print("    %-22s %s" % (key, provides[key]))

    for bridge in live:
        bridge.close()
    return 1 if failures else 0


def cmd_prove(names):
    """
    Run several fragments against the open model in ONE process.

    ONE PROCESS, ONE LEASE, MANY FRAGMENTS - and that is the whole point.
    The lease identifies a CHAT, and a command line that exits after each
    command leaves one orphaned for five minutes, so the second command is
    always refused. Proving twenty fragments one command at a time is a
    hundred minutes of waiting for nothing.

    IT ALSO KEEPS THE ANSWERS COMPARABLE. Every fragment here reads the same
    model in the same state, in one sitting - so a number that disagrees with
    another is a real disagreement rather than something that changed in
    between.
    """
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

    sources = []
    for name in names:
        path = os.path.join(root, "brain", "fragments", name, "impl", "any", "fragment.cs")
        if not os.path.isfile(path):
            print("No fragment called '%s'" % name)
            return 2
        with io.open(path, "r", encoding="utf-8") as fh:
            sources.append((name, fh.read()))

    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
        return 1
    if not live:
        print("No Revit is connected. Press Heron on the ribbon to connect first.")
        report_mismatched(mismatched)
        return 1

    bridge = live[0]
    for other in live[1:]:
        other.close()

    print("Revit %s, session %s - %d fragment(s)" % (
        bridge.revit_version, bridge.pid, len(sources)))
    print("")

    # The model every answer below came from, printed ONCE at the top rather
    # than on every line. A proving run that does not say which model it read
    # proves nothing about that model.
    named_document = None

    failures = 0
    for name, source in sources:
        reply = bridge.request("run_fragment_read",
                               op_args={"name": name, "source": source},
                               response_timeout=180.0)

        if reply is None:
            print("%-30s NO REPLY" % name)
            failures += 1
            continue

        if not reply.get("ok"):
            # A compile failure or a throw is the most useful thing this whole
            # path produces on the day a fragment is wrong. Printed in full.
            print("%-30s %s" % (name, reply.get("error")))
            print("%s %s" % (" " * 30, reply.get("message")))
            failures += 1
            continue

        if named_document is None:
            named_document = reply.get("document") or "(unnamed)"
            print("model: %s   active view: %s" % (
                named_document, reply.get("activeView") or "(none)"))
            print("")

        provides = reply.get("provides") or {}
        print("%-30s ok" % name)
        for key in sorted(provides):
            print("%s   %-20s %s" % (" " * 30, key, provides[key]))

    bridge.close()

    print("")
    print("%d ran, %d failed" % (len(sources) - failures, failures))
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
    closing = []
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
        closing.append(bridge)
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

    for bridge in closing:
        bridge.close()

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
    if argv[1] == "count":
        return cmd_count(argv[2] if len(argv) > 2 else None)
    if argv[1] == "doctor":
        return cmd_doctor()
    if argv[1] == "prove":
        if len(argv) < 3:
            print("Which fragments? e.g. prove list-levels list-grids")
            return 2
        return cmd_prove(argv[2:])
    if argv[1] == "fragment":
        if len(argv) < 3:
            print("Which fragment? e.g. list-levels")
            return 2
        return cmd_fragment(argv[2])

    print(__doc__.strip())
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
