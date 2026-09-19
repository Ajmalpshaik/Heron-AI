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
    python mcp/client/heron_bridge_client.py release   # hand this Revit back
    python mcp/client/heron_bridge_client.py validate list-levels
                                                       # run one fragment through the
                                                       # phases a proof needs, and
                                                       # record what came back

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
#
# ONE ID PER PERSON, SET EVERYWHERE, and the reason is the case the paragraph
# above does not cover: a single conversation that uses BOTH doors. An assistant
# holding the MCP server open and also running this client from a command line
# is one chat by any honest reading - and Heron saw two, refused the second, and
# said "in use by another chat". That refusal is correct and it is not a bug in
# the lease; it is the truth about two processes that had no way to agree on a
# name. It cost five interruptions in one session on 2026-09-12.
#
# So `.mcp.json` sets HERON_CLIENT_ID for the server, the same value is exported
# for anything run from a command line, and tools/batch-prove.py inherits it by
# setdefault rather than pinning its own.
#
# BE HONEST ABOUT WHAT THAT GIVES UP. Two of the owner's own chats now share an
# id and will NOT refuse each other, which is the protection D-22 exists for.
# That is the trade he accepted: he works alone, and a lease that refuses him on
# his own machine costs more than it saves. It stops being right the day a
# second person shares this Revit.
CLIENT_ID = os.environ.get("HERON_CLIENT_ID") or uuid.uuid4().hex[:12]

# The three keys the request envelope owns. An operation argument that reuses
# one of them would overwrite it, and the failure that produces is remote and
# misleading rather than local and obvious.
RESERVED_KEYS = frozenset(("op", "token", "client"))

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

    def release(self):
        """
        Hand the session back, so the next chat does not wait five minutes.

        THE LEASE RENEWS ON EVERY REQUEST, so finishing a batch still leaves
        this chat holding the Revit for the full lease. Saying so explicitly is
        the difference between "I have stopped" and "I have gone quiet", and
        only the first is something another chat can act on.

        Best effort by design. A hand-back that fails costs a wait, never
        correctness - the lease expires on its own either way - so this must
        never turn a finished, successful batch into an error.
        """
        try:
            reply = self.request("release", timeout=2.0, response_timeout=10.0)
        except Exception:                              # noqa: BLE001 - see above
            return False
        return bool(reply and reply.get("released"))

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
            #
            # THAT FLATNESS IS WHY THESE THREE ARE RESERVED. `op_args` used to be
            # merged straight over the envelope, so an argument called "token"
            # silently replaced the session token and the bridge refused the whole
            # request as unauthenticated - which is exactly what happened to
            # move_elements, and it cost a live-Revit session to find because
            # both sides compiled and both read a string called "token".
            # Refusing here turns that class of mistake into an immediate, local
            # error naming the key, instead of a puzzling refusal from Revit.
            clash = RESERVED_KEYS.intersection(op_args)
            if clash:
                raise ValueError(
                    "op_args may not contain %s - %s reserved for the request "
                    "envelope. Name the argument something else."
                    % (", ".join(sorted(clash)),
                       "that key is" if len(clash) == 1 else "those keys are"))
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


def only_session(live, session):
    """
    Narrow the connected Revits to the ONE that was asked for.

    WHY REFUSING BEATS PICKING. With two Revits connected, `prove` and
    `validate` took live[0] - the LOWEST PROCESS ID - and said nothing about
    it. On 2026-09-15 session 2924 beat 8084 for no reason anybody chose, and
    a proof written for a pipe model came back having read a family file. It
    reported "positive ok" on a phase that measured `scanned 0`, because a
    positive that finds nothing still runs without erroring.

    `fragment` was worse in a quieter way: it looped over EVERY session, so
    one apply reached two models.

    So a second session is a QUESTION, not a tie to break - the same rule the
    rest of Heron already keeps for a category, a view and a part name.

    Returns (chosen, refusal). `refusal` is None when it is safe to carry on.
    """
    if session is not None:
        picked = [b for b in live if str(b.pid) == str(session)]
        if not picked:
            return None, ("No connected Revit with session %s. Connected: %s"
                          % (session, ", ".join("%s (Revit %s)" % (b.pid, b.revit_version)
                                                for b in live)))
        return picked, None

    if len(live) <= 1:
        return live, None

    lines = ["More than one Revit is connected, and picking one for you is how a run",
             "reads the wrong model and still reports ok. Say which:", ""]
    for b in live:
        lines.append("  --session %-8s Revit %s" % (b.pid, b.revit_version))
    return None, chr(10).join(lines)


def pull_session(rest):
    """Take `--session <pid>` out of the argument list, wherever it sits."""
    if rest is None:
        return rest, None
    out, session, skip = [], None, False
    for i, token in enumerate(rest):
        if skip:
            skip = False
            continue
        if token == "--session" and i + 1 < len(rest):
            session = rest[i + 1]
            skip = True
            continue
        out.append(token)
    return out, session


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


# THE RISK LEVELS A FRAGMENT MAY CARRY AND STILL BE RUN FROM HERE.
#
# HeronPermissions says Publish and Admin "are not reachable in Phase 0 or
# Phase 1 at all". That was true by accident until 2026-09-08: the gate reads
# the OPERATION's risk from the tool registry - Golden Rule 19, and right - and
# `run_fragment_write` is declared Modify. So a fragment declaring ADMIN or
# PUBLISH ran under a Modify gate and nobody was consulted. `create-workset`
# (ADMIN) created a workset on the first try. Twelve fragments are above Modify.
#
# BE HONEST ABOUT WHAT THIS IS. It is a client-side guard against a MISTAKE,
# not a boundary against malice - a caller that skips this client is unaffected,
# and Golden Rule 19 forbids fixing that by sending the risk over the wire,
# because then the caller decides how dangerous its own request is. The real
# boundary stays where it is; this stops the accident that is actually likely,
# which is somebody proving fragments alphabetically and reaching `export-*`.
RUNNABLE_RISKS = ("READ", "ANALYZE", "EXECUTE", "MODIFY", "SUGGEST")


def fragment_risk(path):
    """The `risk:` a fragment declares, or None if it cannot be read.

    Read the same hand-rolled way as the needs block, and for the same reason:
    this client is stdlib only. `risk:` sits at the top level, unindented.
    """
    try:
        with io.open(path, "r", encoding="utf-8") as fh:
            for line in fh:
                if line.startswith("risk:"):
                    return line.split(":", 1)[1].strip().strip("\"'")
    except (OSError, UnicodeDecodeError):
        return None
    return None


def fragment_needs(path):
    """
    The `contract.needs` of one fragment, as a list of dicts. None if it cannot
    be read with certainty.

    WHY THIS IS NOT yaml.safe_load. This client is stdlib only and says so at
    the top - it has to run on a machine where nothing is installed, which is
    the whole reason `doctor` exists. So the one block it needs is read here.

    IT REFUSES RATHER THAN RETURNING A SHORT LIST, and that is the only thing
    that makes a hand-rolled reader acceptable. A needs list quietly missing an
    entry is the worst failure available on this path: the executor binds what
    it was told about, the fragment names something nobody put in scope, and the
    error arrives at the PC as a compile failure inside generated code. Anything
    this does not recognise makes the whole read fail, by name.

    tests/test_fragment_needs_reader.py runs it against every fragment in the
    library and compares it with PyYAML, so "it agrees with a real parser" is
    measured rather than asserted here.
    """
    try:
        with io.open(path, "r", encoding="utf-8") as handle:
            lines = handle.read().splitlines()
    except (OSError, UnicodeDecodeError):
        return None

    # Find `contract:` at the left margin, then `needs:` two spaces in.
    inside_contract = False
    inside_needs = False
    needs = []

    for raw in lines:
        line = raw.rstrip()
        if not line.strip() or line.lstrip().startswith("#"):
            continue

        indent = len(line) - len(line.lstrip())
        stripped = line.strip()

        if indent == 0:
            # A new top-level key ends the contract, whatever it is.
            if inside_needs:
                return needs
            inside_contract = (stripped == "contract:")
            continue

        if not inside_contract:
            continue

        if indent == 2:
            if inside_needs:
                return needs            # `provides:` or anything else after it
            inside_needs = (stripped == "needs:")
            continue

        if not inside_needs:
            continue

        if indent == 4 and stripped.startswith("- "):
            key, _, value = stripped[2:].partition(":")
            if key.strip() != "name":
                return None             # an item that does not start with a name
            needs.append({"name": value.strip()})
            continue

        if indent == 6 and needs:
            key, sep, value = stripped.partition(":")
            if not sep:
                return None
            needs[-1][key.strip()] = value.strip()
            continue

        # Anything else inside the needs block is a shape this does not
        # understand, and guessing at it is what this refuses to do.
        return None

    return needs if inside_needs else []


def needs_for(root, name):
    """
    The contract to send with a fragment's source, or None with a reason
    printed. The source and the contract come from the same folder and must be
    read together - sending one without the other is how the executor ends up
    binding a scope the snippet was not written against.
    """
    path = os.path.join(root, "brain", "fragments", name, "fragment.yaml")
    if not os.path.isfile(path):
        print("%-30s has no fragment.yaml - cannot send its contract" % name)
        return None

    needs = fragment_needs(path)
    if needs is None:
        print("%-30s contract could not be read with certainty, so nothing was sent."
              % name)
        print("%s Fix the needs block in %s" % (" " * 30, path))
        return None
    return needs


def pull_values(rest):
    """
    Lift `--set name=value` and `--view NAME` out, leaving the rest in order.

    ORDER IS PRESERVED because the callers that follow still read their own
    flags positionally - `prove --in "Project1"` looks at the front of what is
    left. Pulling these two out anywhere in the line lets a value be typed
    where it reads naturally, after the fragment it belongs to.

    `--view X` is shorthand for `--set view=X` and nothing more. It exists
    because `view` is the value 53 fragments in this library ask for, which is
    more than any other by a factor of two.

    The `--negative-` forms are the same values for the NEGATIVE case, kept in
    their own list. A proof's negative case is an arrangement in which the
    answer must be empty, and for a fragment that reads a view the arrangement
    IS a different view - there is nothing to clear at the keyboard.
    """
    kept, pairs, negatives = [], [], []
    setups, negative_setups = [], []
    index = 0

    # WHICH LIST EACH FLAG FILLS, spelled out rather than worked out from the
    # spelling. `--setup-set` and `--negative-set` both end in "set" and both
    # contain "negative" checks that used to be done with startswith/endswith,
    # and adding a third pair made those two rules disagree with each other.
    BUCKET = {
        "--set": ("pairs", False),
        "--view": ("pairs", True),
        "--negative-set": ("negatives", False),
        "--negative-view": ("negatives", True),
        # THE SETUP CHAIN'S OWN VALUES. FRAGMENT-ISSUES row 149: a chain and
        # the fragment under test could not be given different values for a
        # name they SHARE, and `categories` is shared by 88 fragment/chain
        # pairs in this library. Optional, and when it is absent every setup
        # step still gets exactly what it got before.
        "--setup-set": ("setups", False),
        "--negative-setup-set": ("negative_setups", False),
    }
    lists = {"pairs": pairs, "negatives": negatives,
             "setups": setups, "negative_setups": negative_setups}

    while index < len(rest):
        token = rest[index]
        if token in BUCKET:
            if index + 1 >= len(rest):
                print("%s needs a value after it" % token)
                return None, None, None, None, None
            value = rest[index + 1]
            which, is_view = BUCKET[token]
            lists[which].append("view=" + value if is_view else value)
            index += 2
            continue
        kept.append(token)
        index += 1
    return kept, pairs, negatives, setups, negative_setups


def risk_refusal(root, name):
    """The refusal for a fragment nothing here may run, or None.

    An UNREADABLE risk is refused too. The alternative is running a fragment
    whose danger nobody could establish, and this file's own needs reader takes
    the same line for the same reason: on a path where being wrong is expensive,
    "I could not tell" and "it is fine" must not collapse into one answer.
    """
    path = os.path.join(root, "brain", "fragments", name, "fragment.yaml")
    risk = fragment_risk(path)

    if risk is None:
        return ("%s does not say what risk it carries, so it will not be run. "
                "Add a `risk:` line to %s." % (name, path))

    if risk not in RUNNABLE_RISKS:
        return ("%s is declared risk: %s, and Heron does not run those yet - "
                "HeronPermissions puts Publish and Admin out of reach for Phase 0 "
                "and Phase 1. Nothing was sent to Revit." % (name, risk))

    return None


def caller_values(pairs):
    """
    Turn `--set view=Level 1` into what the executor reads.

    THE CALLER'S HALF, TYPED BY A PERSON. A contract may declare a need as
    `source: request` - a view, a category, a name to match, a distance - and
    Revit refuses to guess one, because guessing is how a job runs against the
    wrong thing and reports success. This is how it gets said instead.

    SPLIT ON THE FIRST '=' ONLY. View names contain equals signs about as
    often as they contain anything else, and a name silently truncated at one
    would resolve to nothing with no clue why.

    Sent as {name, value} pairs rather than one object so the add-in reads
    them with the parser `needs` already uses. Everything crosses as text:
    only Revit can turn "Level 1" into a view, and only Revit can tell that
    two views answer to that name.
    """
    values = []
    for pair in pairs:
        if "=" not in pair:
            print("'%s' is not name=value - e.g. --set view=\"Level 1\"" % pair)
            return None
        key, value = pair.split("=", 1)
        key = key.strip()
        if not key:
            print("'%s' has no name before the '='" % pair)
            return None
        values.append({"name": key, "value": value})
    return values


def undeclared_values(values, needs):
    """The typed names this fragment declares no need for, and what it does take.

    Returns (undeclared, takeable) - both lists of names, both possibly empty.

    FRAGMENT-ISSUES ROW 71. Proving `trace-connectivity`, `--set maxSteps=200`
    was passed against `--negative-set maxSteps=0` and both legs came back
    `reached 25`. The fragment declares no `maxSteps` at all, so the value was
    accepted, ignored, and never mentioned - and the fragment looked broken.
    `list-levels --set totallyMadeUpValue=42` runs clean for the same reason.

    IT IS THE MISTYPED-INPUT FAMILY, WHICH THIS REPOSITORY ALREADY KNOWS THE
    COST OF. `tools/generate-jobs.py` exists because six input names were
    mistyped on 2026-09-09, `widthMm` for `width` among them - and that tool
    only protects a GENERATED job file. A hand-run `fragment`, `prove` or
    `validate` takes anything. A mistyped name and an invented one are the same
    event, and both read as a fragment that ignores its input.

    THE CHECK IS HERE AND NOT IN THE ADD-IN BECAUSE THE CONTRACT IS HERE.
    `needs_for` has already read `contract.needs` off disk before anything is
    sent - the client knows every declared name while Revit is still untouched.
    The add-in could not do this as cheaply: it is handed the `needs` block, but
    refusing there would mean a round trip to learn about a typo.

    IT NAMES, IT DOES NOT REFUSE. Row 71's own words are "refusing an
    undeclared caller value, OR NAMING IT, would have turned a confusing run
    into a one-line answer". Naming is the half that cannot break a caller who
    is relying on today's behaviour, and it is enough: the confusing part was
    never the drop, it was the silence.
    """
    declared = set()
    takeable = []
    for need in needs or []:
        name = (need or {}).get("name")
        if not name:
            continue
        declared.add(name)
        # WHAT A PERSON CAN ACTUALLY TYPE. A need filled by an earlier fragment
        # or by the wrapper is not an answer to "what should I have written",
        # so offering it would send somebody to set a value that is not theirs
        # to set.
        if (need or {}).get("source") == "request":
            takeable.append("%s (%s)" % (name, need.get("type") or "?"))

    undeclared = [entry.get("name") for entry in (values or [])
                  if entry and entry.get("name") not in declared]
    return undeclared, takeable


def report_undeclared(fragment, values, needs):
    """Say so, once, before Revit is touched. Never refuses; returns nothing."""
    undeclared, takeable = undeclared_values(values, needs)
    if not undeclared:
        return
    # THE NAME IN THE FIRST COLUMN, because `prove` prints a 30-wide column of
    # fragment names and a warning that did not line up under one would read as
    # being about whichever fragment was last.
    for name in undeclared:
        print("%-30s IGNORED '%s' - not a value this fragment declares, so it "
              "will be dropped" % (fragment, name))
    print("%-30s it takes: %s"
          % ("", ", ".join(takeable) if takeable
             else "no caller values at all"))
    print("%-30s a mistyped name and an invented one look the same here "
          "(FRAGMENT-ISSUES row 71)" % "")


def cmd_fragment(name, values=None, writing=False, apply_it=False, session=None,
                 expect=None):
    """
    Run one fragment's C# against the open model - D-28's executor, reached.

    THE SOURCE IS SENT, NOT A NAME. Revit has no idea where the fragment
    library lives and should not: the add-in would then need a path into
    somebody's repository, and a fragment could be changed under it between
    the check and the run. The client reads the file it just checked and sends
    exactly that text.

    READ BY DEFAULT. `run_fragment_read` opens no transaction, so Revit
    refuses anything that would change the model - the guarantee is Revit's
    rather than ours.

    `writing` switches to `run_fragment_write`, which is the same executor
    inside a TransactionGroup, and is what lets a fragment at risk: MODIFY run
    at all. It still keeps nothing unless `apply_it` is set: the default runs
    the change for real and rolls it back, which is a record of what happened
    rather than a prediction of what would.
    """
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    source_path = os.path.join(root, "brain", "fragments", name, "impl", "any", "fragment.cs")

    if not os.path.isfile(source_path):
        print("No fragment called '%s' - looked for %s" % (name, source_path))
        return 2

    # WHAT IT IS ALLOWED TO BE, before anything is read or sent.
    refusal = risk_refusal(root, name)
    if refusal is not None:
        print(refusal)
        return 2

    with io.open(source_path, "r", encoding="utf-8") as fh:
        source = fh.read()

    # The contract travels with the source - see needs_for.
    needs = needs_for(root, name)
    if needs is None:
        return 2

    # BEFORE REVIT IS TOUCHED. The contract is already read, so a typed name
    # this fragment does not declare can be named here rather than dropped in
    # silence eleven seconds later (row 71).
    report_undeclared(name, values, needs)

    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
        return 1
    if not live:
        print("No Revit is connected. Press Heron on the ribbon to connect first.")
        report_mismatched(mismatched)
        return 1

    # ONE SESSION, CHOSEN. This used to loop over every connected Revit, so a
    # single --apply reached two models and only the last line was read.
    live, refusal = only_session(live, session)
    if refusal is not None:
        print(refusal)
        return 2

    failures = 0
    for bridge in live:
        # RESET UNLESS THE CALLER SAID WHAT IT EXPECTS.
        #
        # Reset is right by default and the add-in's own comment says why:
        # otherwise a fragment run an hour later binds "elements collected by
        # something nobody remembers running". `--expect-from` is how a caller
        # says it DOES remember - it names the producer, the add-in checks the
        # carried values were left by that fragment before binding anything,
        # and refuses if they were not. Sending both would clear the values the
        # expectation is about, which the add-in refuses by name rather than
        # reporting as an empty chain. docs/36.
        args = {"name": name, "source": source, "needs": needs}
        if expect:
            args["expectChain"] = expect
        else:
            args["chain"] = "reset"
        if values:
            args["values"] = values
        # A STRING, because Heron's own JSON reader reads strings and nothing
        # else - the same reason a distance crosses as one. See ReadDistance.
        if writing and apply_it:
            args["apply"] = "true"

        reply = bridge.request("run_fragment_write" if writing else "run_fragment_read",
                               op_args=args, response_timeout=180.0)

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

        # WHETHER THE MODEL WAS LEFT CHANGED. A rolled-back write and a kept
        # one report identical counts, because the fragment did the work in
        # both cases. Printing it first means it cannot be missed under a list
        # of results that look the same either way.
        verdict = reply.get("verdict")
        if verdict:
            # THE LABEL IS READ FROM `rolledBack`, NOT FROM `applied`.
            #
            # It used to be `"APPLIED" if applied else "ROLLED BACK"`, and a
            # FAILED rollback is not applied - so the header read ROLLED BACK
            # directly above the sentence "THE ROLLBACK DID NOT REPORT SUCCESS
            # ... THE MODEL MAY STILL HOLD THIS CHANGE". The headline said the
            # model was safe and the small print said it might not be.
            #
            # That is the same shape as the defect this line exists to prevent:
            # the comment above says a rolled-back write and a kept one report
            # identical counts, so the label was put first where it could not
            # be missed - and then the label itself could not tell them apart.
            # Three rollbacks failed before anybody noticed; a reader scanning
            # headers would have been told each time that nothing was kept.
            #
            # Found 2026-09-12 while reading this path before running A16.
            if reply.get("applied"):
                state = "APPLIED"
            elif reply.get("rolledBack"):
                state = "ROLLED BACK"
            else:
                state = "NOT ROLLED BACK"
            print("    %s  %s" % (state, verdict))

        provides = reply.get("provides") or {}
        if not provides:
            print("    left nothing behind")
        for key in sorted(provides):
            print("    %-22s %s" % (key, provides[key]))

    for bridge in live:
        bridge.release()
        bridge.close()
    return 1 if failures else 0


def cmd_prove(names, in_document=None, values=None, session=None):
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

        # WHAT IT IS ALLOWED TO BE. Refused before it is read, so a batch that
        # names one cannot send it and then discover the problem.
        refusal = risk_refusal(root, name)
        if refusal is not None:
            print(refusal)
            return 2
        with io.open(path, "r", encoding="utf-8") as fh:
            source = fh.read()

        # THE CONTRACT GOES WITH THE SOURCE. A fragment is a snippet that
        # assumes names are in scope (D-29), and only its contract says which.
        # Revit has never seen fragment.yaml, so what the executor can put in
        # scope is exactly what is sent here.
        needs = needs_for(root, name)
        if needs is None:
            return 2

        # ONE `--set` BLOCK SERVES EVERY FRAGMENT IN A `prove` RUN, so a value
        # meant for the third is undeclared by the first two and that is not a
        # mistake. It is still said, once per fragment, because the alternative
        # is silence on the run where it IS a typo.
        report_undeclared(name, values, needs)

        sources.append((name, source, needs))

    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet. Try again shortly.")
        return 1
    if not live:
        print("No Revit is connected. Press Heron on the ribbon to connect first.")
        report_mismatched(mismatched)
        return 1

    chosen, refusal = only_session(live, session)
    if refusal is not None:
        # Close the bridges we opened before walking away from them.
        for b in live:
            b.close()
        print(refusal)
        return 2
    live = chosen

    bridge = live[0]
    for other in live[1:]:
        other.close()

    print("Revit %s, session %s - %d fragment(s)" % (
        bridge.revit_version, bridge.pid, len(sources)))
    print("")

    # The model every answer below came from, printed ONCE at the top rather
    # than on every line. A proving run that does not say which model it read
    # proves nothing about that model.
    # WHICH MODEL IS IN FRONT, before anything runs. count_elements always
    # describes the ACTIVE document and takes no target, so when a fragment is
    # aimed elsewhere with --in the two lines differ - and that difference is
    # exactly what was missing when a run intended for one model came back
    # reading another.
    opening = bridge.request("count_elements")
    if opening is not None and opening.get("ok"):
        print("active: %s%s" % (opening.get("document"),
                                "  (unsaved changes)" if opening.get("unsaved") else ""))
        if opening.get("documentPath"):
            print("path:   %s" % opening.get("documentPath"))
        print("size:   %s elements" % "{:,}".format(opening.get("count", 0)))
        print("")
    elif opening is not None and not opening.get("ok"):
        print("could not identify the active model: %s"
              % (opening.get("message") or opening.get("error")))
        print("Refusing to prove anything against a model that will not name itself.")
        bridge.close()
        return 1

    named_document = None

    failures = 0
    for index, (name, source, needs) in enumerate(sources):
        args = {"name": name, "source": source, "needs": needs}
        if values:
            args["values"] = values

        # THE FIRST FRAGMENT OPENS A CHAIN; the rest continue it. Revit holds
        # what each one leaves behind so the next can consume it - a filter's
        # `provides` feeding an action's `needs`, which is D-29's whole design
        # and cannot live on this side because a client cannot hold a Revit
        # Element across a wire. Saying "reset" here is what stops a later
        # batch inheriting values from an earlier one.
        if index == 0:
            args["chain"] = "reset"

        if in_document:
            args["document"] = in_document

        reply = bridge.request("run_fragment_read",
                               op_args=args,
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
            print("read:   %s   active view: %s" % (
                named_document, reply.get("activeView") or "(none)"))
            if reply.get("wasActiveDocument") is False:
                # Everything below is TRUE of a model nobody is looking at, and
                # anything about a selection or an active view is about a
                # window that is not on screen.
                print("        NOT the document on screen - read by name.")
            print("")

        provides = reply.get("provides") or {}
        print("%-30s ok" % name)

        # WHERE THE INPUTS CAME FROM, when the host had to bind any. A run on
        # the selection and a run on the previous fragment's output look
        # identical in the results, and reading one as the other is how a
        # filter gets blamed for an answer it never produced.
        if reply.get("bound"):
            print("%s   <- %s" % (" " * 30, reply.get("bound")))
        for key in sorted(provides):
            print("%s   %-20s %s" % (" " * 30, key, provides[key]))

    # DONE - hand the Revit back rather than sitting on it for five more
    # minutes. This is what makes moving straight to another chat work.
    bridge.release()
    bridge.close()

    print("")
    print("%d ran, %d failed" % (len(sources) - failures, failures))
    return 1 if failures else 0


def model_line(opening, phases, revit_version, pid):
    """The `model:` header a proof carries, built from where the phases RAN.

    FRAGMENT-ISSUES ROW 15. This header used to come from the opening
    `count_elements` call, which answers for the document IN FRONT - and every
    phase runs against `--in` when a job pins one. The two disagreed inside the
    same proof: three fragments were signed with
    `model: Project1 work_ajmal.al (3,445 elements)` while each case's own text
    ended *"on Snowdon-scratch_ajmal.al"*, which is where they actually ran.

    **THE ELEMENT COUNT WAS THE WORST PART.** 3,445 belongs to a model those
    fragments never touched, so a reader checking the evidence against the
    model would have been checking the wrong one.

    THE OBVIOUS REPAIR IS A NO-OP AND THE ROW SAYS SO. Passing the pinned
    document to the opening call changes nothing: `RevitOperations.cs:60` is
    `case "count_elements": return CountElements(app);` - `app` only, no
    `request` - so a `document` sent with it is discarded in silence and the
    header would go on naming the active model. That route needs an add-in
    change and a deploy.

    THIS IS THE OTHER ROUTE, WHICH NEEDS NEITHER. Every phase already records
    `document` from its own reply, so the truth the header contradicted was
    sitting in the same file. Three cases, and the middle one is the point:

      every phase names the SAME document as the opening call
          nothing was pinned, or it was pinned to the active one. The count
          belongs to that model, so it is kept

      the phases name a DIFFERENT document
          the count came from `count_elements` on the ACTIVE document and does
          NOT belong to the model the fragment ran against. It is dropped
          rather than carried across, because a number attached to the wrong
          model is what made this a defect rather than a typo

      the phases DISAGREE with each other
          said out loud. A proof whose legs ran against different documents is
          a finding, and picking one of them would hide it
    """
    seen = []
    for phase in phases or []:
        where = (phase or {}).get("document")
        if where and where not in seen:
            seen.append(where)

    active = (opening or {}).get("document")
    count = (opening or {}).get("count", 0)
    tail = "Revit %s, session %s" % (revit_version, pid)

    if not seen:
        # No phase reported one - the opening call is all there is, and saying
        # so is better than a header that looks derived when it is not.
        return "%s (%s elements), %s" % (active, "{:,}".format(count), tail)

    if len(seen) > 1:
        return ("PHASES RAN AGAINST DIFFERENT DOCUMENTS: %s - active was %s, "
                "%s" % (", ".join(seen), active, tail))

    where = seen[0]
    if where == active:
        return "%s (%s elements), %s" % (where, "{:,}".format(count), tail)

    return ("%s, %s - the count is NOT recorded because %s elements was "
            "measured on %s, the document in front, and this ran against "
            "another one (row 15)"
            % (where, tail, "{:,}".format(count), active))


def cmd_validate(name, session=None, in_document=None, cross=None, negative_in=None, out=None,
                 values=None, negative_values=None, setup_values=None,
                 negative_setup_values=None, vary=None, vary_field=None,
                 writing=False, setup=None,
                 keep_chain=False, allow_publish=False):
    """
    Run ONE fragment through the phases a proof needs, and record what came back.

    THIS IS THE HALF THAT NEEDS REVIT, AND NOTHING MORE. It runs, it writes down
    what happened, and it stops. It draws no conclusion, writes into no
    fragment, and cannot promote anything - the judging is
    `brain/heron_validate.py`, which needs no Revit and can be tested on a
    machine that has never had it.

    The seam is a plain JSON run record. That is not tidiness: `brain` may
    depend only on `platform` (tools/check-structure.py), so the judging half
    cannot reach the bridge even if it wanted to, and this half stays
    dependency-free for the locked-down machine it has to run on.

    THREE PHASES, AND THE SECOND IS THE ONE THAT MATTERS
    ---------------------------------------------------
      positive      run it, on the model in front or the one named with --in
      negative      run it again where the answer MUST be nothing. Either a
                    second open model that lacks the thing (--negative-in), or
                    the selection cleared by hand at the keyboard. D-30 exists
                    for this phase: a fragment that succeeds while doing nothing
                    passes ten runs and a thousand, and only an answer that
                    should be empty can catch it
      second route  a native operation reaching the answer another way
                    (--cross count_elements, or --cross duct)

    A PHASE THAT DID NOT RUN IS SIMPLY ABSENT FROM THE RECORD, and the draft
    then says NOT ESTABLISHED. Nothing here invents a result it did not see.

    WHAT A SETUP CHAIN CAN HAND OVER, AND `keep_chain` - defect row 11
    ------------------------------------------------------------------
    By default the fragment under test RESETS the chain, so a setup chain hands
    over only what lives in Revit's own state - the SELECTION - and never a
    value. `select-by-category-name` then `set-selection` survives because
    `set-selection` writes the real selection; `read-element-parameters` then
    `group-and-count` does not, and both `group-and-count` and `sum-by-group`
    came back `needs_unbound: 'values' was never supplied` on 2026-09-10 while
    the setup reported success.

    `keep_chain=True` keeps it, and it is OPT-IN PER JOB rather than the
    default because the obvious fix is wrong. THE CHAIN OUTRANKS THE SELECTION
    (`RevitFragment.cs`, "1. THE CHAIN" then "2. THE SELECTION"), so keeping it
    everywhere would make `set-selection` decorative in the middle of every
    arrangement already written, and those proofs would quietly begin testing
    something other than what they say.
    """
    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

    # KEEPING THE CHAIN WITH NOTHING TO KEEP IS NOT A NO-OP. With no setup this
    # run puts nothing there, so what would survive is whatever some earlier run
    # of some other fragment left - "elements collected by something nobody
    # remembers running", which is the exact hazard the reset exists to prevent.
    if keep_chain and not setup:
        print("--keep-chain needs a --setup to keep something FROM.")
        print("Without one, nothing in this run fills the chain, and what would")
        print("survive is whatever an earlier run left behind - which is what")
        print("the chain reset exists to prevent.")
        return 2

    # THE RISK GATE APPLIES HERE TOO, AND DID NOT UNTIL 2026-09-13. `cmd_fragment`
    # and `cmd_prove` have both asked `risk_refusal` since 2026-09-08; this
    # function never did. So the one path that actually RUNS fragments all day -
    # proving - was the one path a PUBLISH or ADMIN fragment could reach, and
    # RUNNABLE_RISKS' own comment names the accident exactly: "somebody proving
    # fragments alphabetically and reaching `export-*`".
    #
    # That is not hypothetical. `export-parameters-to-csv` (PUBLISH) was proved
    # through here the same day, writing four real CSV files to disk, and the
    # gate said nothing. The evidence it produced is good and the proof stands;
    # what was missing was anybody DECIDING to run a Publish fragment.
    #
    # SO REFUSE BY DEFAULT AND MAKE THE EXCEPTION VISIBLE, rather than block it
    # outright. Proving these is real work that has to happen - four PUBLISH
    # fragments and one ADMIN are already PROVEN - and a gate that makes the
    # necessary thing impossible gets worked around instead of obeyed.
    # `--allow-publish` is typed per run, appears in the shell history, and is
    # the difference between a decision and an accident.
    if not allow_publish:
        refusal = risk_refusal(root, name)
        if refusal:
            print(refusal)
            print("")
            print("If you mean to prove it, say so: add --allow-publish.")
            return 2

    source_path = os.path.join(root, "brain", "fragments", name, "impl", "any",
                               "fragment.cs")
    if not os.path.isfile(source_path):
        print("No fragment called '%s'" % name)
        return 2
    with io.open(source_path, "r", encoding="utf-8") as fh:
        source = fh.read()
    needs = needs_for(root, name)
    if needs is None:
        return 2

    # BOTH LEGS, because row 71's case was a value typed into the NEGATIVE that
    # the fragment never declared - `--set maxSteps=200` against
    # `--negative-set maxSteps=0`, and both legs came back identical.
    report_undeclared(name, values, needs)
    if negative_values is not None and negative_values is not values:
        report_undeclared(name, negative_values, needs)

    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet.")
        return 1
    if not live:
        print("No Revit is connected. Press Heron on the ribbon to connect first.")
        report_mismatched(mismatched)
        return 1

    chosen, refusal = only_session(live, session)
    if refusal is not None:
        # Close the bridges we opened before walking away from them.
        for b in live:
            b.close()
        print(refusal)
        return 2
    live = chosen

    bridge = live[0]
    for other in live[1:]:
        other.close()

    opening = bridge.request("count_elements")
    if opening is None or not opening.get("ok"):
        # SAY WHAT REVIT ACTUALLY SAID. This used to print only the sentence
        # below, which reads as "the model is the problem" - and on 2026-09-07
        # it sent a session hunting the model for a while when the real answer
        # was `session_in_use`: another chat held the lease. The generic line is
        # still right about what will not be recorded, but the reason has to
        # come first or it points at the wrong thing.
        if opening is None:
            reason = "Revit did not answer at all."
        else:
            reason = (opening.get("message")
                      or opening.get("error")
                      or "Revit refused, and said nothing about why.")
        print("Could not identify the active model:")
        print("  %s" % reason)
        print("Refusing to record evidence about a model that will not name itself.")
        bridge.close()
        return 1

    # HELD BEFORE THE BRIDGE IS CLOSED. The record's header is built after the
    # phases have reported, by which point `bridge` is released and closed -
    # reading these off it there would be reading a closed object.
    revit_version, pid = bridge.revit_version, bridge.pid

    # PROVISIONAL, AND REPLACED BELOW ONCE THE PHASES HAVE REPORTED. Printed
    # here because a person watching the run wants to know what is in front
    # before anything is sent; the RECORD's header is built from where the
    # phases actually ran - see model_line and row 15.
    model = "%s (%s elements), Revit %s, session %s" % (
        opening.get("document"), "{:,}".format(opening.get("count", 0)),
        bridge.revit_version, bridge.pid)
    print("active: %s" % model)
    print("")

    phases = []

    # WHICH SETUP STEPS HAVE TO TRAVEL WITH THE FRAGMENT.
    #
    # A step at MODIFY or above cannot run on the read path - it opens no
    # transaction - and cannot usefully run as its own write, because that is
    # its own transaction group, decided before the fragment under test runs.
    # It goes in the request's `setup` array instead and the add-in runs it
    # inside the same group, where `apply` still decides whether any of it
    # survives.
    #
    # ONLY MEANINGFUL ON A WRITE PHASE. A read run has no group to put them in,
    # and a read run needing a write to arrange it is a contradiction: the
    # fragment under test cannot change anything either.
    deferred_setup = set()
    if writing:
        for step in (setup or []):
            step_risk = fragment_risk(os.path.join(root, "brain", "fragments",
                                                   step, "fragment.yaml"))
            if step_risk in ("MODIFY", "PUBLISH", "ADMIN"):
                deferred_setup.add(step)

    # A DEFERRED STEP CANNOT BE GIVEN ITS OWN VALUES, SO SAYING SO IS REFUSED
    # RATHER THAN IGNORED. A step at MODIFY or above travels WITH the fragment
    # in the request's `setup` array, and that array carries a name, a source
    # and the needs - no values. `RevitFragment.RunSetupSteps` binds every step
    # from the run's top-level `supplied`, so `--setup-set` would be accepted
    # here, sent, and quietly not applied - and if the step and the fragment
    # share a name like `categories`, the silent collision this flag exists to
    # kill is still there for write arrangements. Named by review on PR #198.
    #
    # REFUSING IS THE HONEST HALF OF A FIX THAT NEEDS A DEPLOY. Carrying
    # per-step values needs the add-in to read them, which is C# and costs a
    # rebuild and a Revit restart. Until then this is a wrong answer that
    # cannot be produced, rather than one produced silently.
    if (setup_values or negative_setup_values) and deferred_setup:
        print("setup values cannot reach a setup step that CHANGES the model.")
        print("")
        print("  %s" % ", ".join(sorted(deferred_setup)))
        print("")
        print("Those run inside the fragment's own transaction group, and the")
        print("add-in binds them from the run's values - there is nowhere to")
        print("put a value meant only for them. Give the chain and the fragment")
        print("different NAMES, or use a read-only chain, or teach the add-in")
        print("per-step values (C#, so a rebuild and a Revit restart).")
        return 2

    def deferred_specs():
        """The deferred steps as the add-in wants them, in the declared order."""
        specs = []
        for step in (setup or []):
            if step not in deferred_setup:
                continue
            step_path = os.path.join(root, "brain", "fragments", step,
                                     "impl", "any", "fragment.cs")
            if not os.path.isfile(step_path):
                return None
            with io.open(step_path, "r", encoding="utf-8") as fh:
                step_source = fh.read()
            step_needs = needs_for(root, step)
            if step_needs is None:
                return None
            # `needs` crosses as the TEXT of its array: a step arrives at the
            # add-in already flattened into a string dictionary, and re-reading
            # one named array is cheaper than a second parser on a wire that is
            # already parsed by hand.
            specs.append({"name": step, "source": step_source,
                          "needs": json.dumps(step_needs)})
        return specs

    def arrange(document, using=None, negative=False):
        """Re-make the arrangement before a phase, and say if it could not be.

        WHY THIS IS PER PHASE AND NOT ONCE. A rolled-back write CLEARS the
        Revit selection - established 2026-09-08 by doing it: select 307
        ducts, run a read twice and the selection survives, run one write
        with no `apply` and it is gone. So the second phase of every
        selection-based write arrived at an empty selection and refused,
        which is 100 fragments - the whole remaining population of provable
        work.

        THE SETUP RUNS WITH THE PHASE'S OWN VALUES. That was "always the
        positive values" for one commit, on the reasoning that the setup is
        the arrangement rather than the question - and it is wrong for the
        commonest shape there is. A fragment needing ONLY a selection has
        nothing else to vary, so its negative case IS a different selection:
        remove-tags against ducts removes tags, against sheets removes none.
        Fixing the arrangement to the positive makes those unprovable.
        Passing the phase's own values costs nothing when both phases name
        the same selection, because then they are the same values.
        """
        for position, step in enumerate(setup or []):
            # A STEP THAT CHANGES THE MODEL IS NOT RUN HERE. This path is
            # run_fragment_read, which opens no transaction, so a MODIFY step
            # throws - and sending it as its own write would make it its own
            # transaction group, kept or rolled back before the fragment under
            # test could see it. Either way there is no arrangement.
            #
            # Those steps travel WITH the fragment under test instead, in the
            # request's `setup` array, and the add-in runs them inside the same
            # group. See writing_setup below and RevitFragment.RunSetupSteps.
            if step in deferred_setup:
                continue

            step_path = os.path.join(root, "brain", "fragments", step,
                                     "impl", "any", "fragment.cs")
            if not os.path.isfile(step_path):
                print("  setup: no fragment called '%s'" % step)
                return False
            with io.open(step_path, "r", encoding="utf-8") as fh:
                step_source = fh.read()
            step_needs = needs_for(root, step)
            if step_needs is None:
                return False
            # ONLY THE FIRST STEP RESETS THE CHAIN. The whole point of a
            # setup chain is that step two consumes what step one left -
            # select-by-category-name leaves `elements`, set-selection needs
            # them - and resetting between them throws that away. Sent as
            # "reset" for every step for one commit, and set-selection
            # refused with "elements was never supplied", which reads as a
            # missing selection rather than a discarded one.
            step_args = {"name": step, "source": step_source,
                         "needs": step_needs}
            # THE SETUP CHAIN'S VALUES, WHICH ARE THE FRAGMENT'S UNLESS SAID
            # OTHERWISE. Row 142: one flat dict went to every step and to the
            # fragment, so a chain selecting on `categories` and a fragment
            # asking about `categories` collapsed into one value - silently,
            # and the job still ran. Absent, this is exactly what it was.
            # THE PHASE IS PASSED IN, NEVER INFERRED FROM `using`. It was
            # inferred for one commit and `--negative-setup-set` could not
            # work at all: a negative that differs ONLY in its setup chain
            # leaves `negative_values` empty, so `using` arrived None, this
            # read it as the positive phase and re-used the POSITIVE setup -
            # recording a negative run against the wrong arrangement and
            # calling it evidence. Named by review on PR #198 as a P1, and it
            # is the sharpest kind of defect this file can have: a proof that
            # ran, passed, and was about something else.
            if negative:
                chosen_setup = negative_setup_values or using or values
            else:
                # `using` ON THE POSITIVE SIDE TOO, AND `--vary` IS WHY. The
                # positive branch read only `values`, so a tracked run handed
                # its varied value to the FRAGMENT and not to the chain - and
                # `describe-blank-parameters`, whose chain needs the same
                # `parameterName` it does, failed every row with
                # "'parameterName (string)' is a value the CALLER supplies".
                # Found by running it, first time out. `using` is what THIS
                # phase actually ran with, which is what the chain must be
                # arranged from, and the negative branch had said so all along.
                chosen_setup = setup_values or using or values
            if chosen_setup:
                step_args["values"] = chosen_setup
            if position == 0:
                step_args["chain"] = "reset"
            if document:
                step_args["document"] = document
            reply = bridge.request("run_fragment_read", op_args=step_args,
                                   response_timeout=180.0)
            if reply is None or not reply.get("ok"):
                print("  setup: %s did not run - %s"
                      % (step, (reply or {}).get("message", "no reply")[:70]))
                return False
        return True

    def run_fragment(phase, document, arranged, reset, using=None,
                     negative=False):
        if setup and not arrange(document, using, negative):
            phases.append({"phase": phase, "ok": False, "error": "setup_failed",
                           "message": "the arrangement could not be re-made",
                           "arranged": arranged})
            print("%-14s %s" % (phase, "setup_failed"))
            return phases[-1]

        args = {"name": name, "source": source, "needs": needs}
        # `using` is the negative phase asking for DIFFERENT caller values -
        # the same fragment aimed at a view that does not have the thing. None
        # means "the ones this run was given"; an empty list would mean "none",
        # and the two must not collapse.
        chosen = values if using is None else using
        if chosen:
            args["values"] = chosen
        if reset:
            args["chain"] = "reset"
        if document:
            args["document"] = document

        # THE ARRANGEMENT THAT HAD TO CHANGE THE MODEL, travelling with the
        # fragment so the add-in can run it inside the same transaction group.
        # Nothing here is kept either: the group is rolled back with everything
        # in it unless `apply` is sent, and it never is from this path.
        if deferred_setup:
            specs = deferred_specs()
            if specs is None:
                record = {"phase": phase, "ok": False, "error": "setup_failed",
                          "message": "the arrangement could not be read from disk",
                          "arranged": arranged}
                phases.append(record)
                print("%-14s %s" % (phase, "setup_failed"))
                return record
            args["setup"] = specs

        # A MODIFY FRAGMENT IS PROVED WITHOUT KEEPING ANYTHING. `apply` is never
        # sent from here, so both phases run for real inside a transaction and
        # are rolled back: the record is what the fragment DID, and the model
        # ends untouched. A proof that required damaging a model to obtain would
        # not get run.
        #
        # Sending a MODIFY fragment down the READ path instead does not fail
        # loudly - Revit refuses the change with "Modifying is forbidden", the
        # fragment reports `refused` like any other declined request, and the
        # run looks exactly like a fragment that decided not to act. That is
        # what this line looked like for one commit, and it read as intermittent
        # worksharing behaviour rather than as a wrong operation name.
        reply = bridge.request("run_fragment_write" if writing else "run_fragment_read",
                               op_args=args, response_timeout=180.0)
        if reply is None:
            record = {"phase": phase, "ok": False, "error": "no_reply",
                      "message": "Revit did not answer", "arranged": arranged}
        elif not reply.get("ok"):
            record = {"phase": phase, "ok": False,
                      "error": reply.get("error"),
                      "message": reply.get("message"), "arranged": arranged}
        else:
            # WHAT THE RECORD MAY SAY ABOUT THE MODEL, AND WHAT IT MAY NOT.
            #
            # This used to append "run inside a transaction and ROLLED BACK, so
            # the model was left exactly as it was" whenever `writing` was set.
            # That sentence came from the FLAG THIS PROCESS PASSED IN. Nothing
            # asked Revit, and nothing re-read the model.
            #
            # On 2026-09-09 it was false in a recorded proof: edit-text-values
            # renamed 17 sheets, the rollback did not hold, and the draft said
            # the model was untouched (section 1c, section 5 row 10). A proof whose
            # text implies a change was kept, on a model where it was not, is
            # worse than no proof - and so is the reverse, which is what this
            # was.
            #
            # `applied` cannot stand in for it either: that is the same INTENT,
            # keep or discard, not whether the discard worked.
            #
            # So the reply is asked, and only what it answers is written down.
            # `rolledBack` is reported by add-in builds that check the
            # transaction group's status afterwards; an older build does not
            # send it, and THAT IS SAID rather than passed over.
            told = arranged
            if setup:
                told += (" - the arrangement was re-made first by running "
                         + ", ".join(setup) + " with this phase's own values")
            if writing:
                rolled = reply.get("rolledBack")
                if rolled is True:
                    told += (" - run inside a transaction, and Revit reported the "
                             "transaction group ROLLED BACK afterwards")
                elif rolled is False:
                    told += (" - run inside a transaction whose ROLLBACK DID NOT REPORT "
                             "SUCCESS, so THE MODEL MAY STILL HOLD THIS CHANGE. Check it "
                             "before trusting anything here")
                else:
                    told += (" - run inside a transaction with a rollback requested. "
                             "Whether it held was NOT checked: this add-in build does not "
                             "report the outcome, and nothing here re-read the model")
            record = {"phase": phase, "ok": True,
                      "provides": reply.get("provides") or {},
                      "bound": reply.get("bound"),
                      "document": reply.get("document"),
                      "arranged": told}
        phases.append(record)
        print("%-14s %s" % (phase, "ok" if record["ok"] else record.get("error")))
        return record

    # DEFECT ROW 11, and the default is deliberate - see the docstring.
    reset_chain = not keep_chain

    # NOT WHEN TRACKING. With `--vary` the positive phase IS the first tracked
    # input, and running it here first would run the fragment WITHOUT the value
    # being varied - which on the first live run came back
    # `needs_request_values`, wrote a failed phase into the record, and then ran
    # again. Two phases called "positive", one of them about nothing.
    if not vary:
        run_fragment("positive", in_document, "run as it would normally be run",
                     reset_chain)

    # ---- D-53 TRACKING, WHICH REPLACES THE NEGATIVE RATHER THAN JOINING IT --
    #
    # The FIRST value ran above as the positive phase, so the loop starts at
    # the second and every run is recorded. `draft_from_record` prefers
    # `tracking` over a negative phase when both are present, and there is no
    # negative here by construction: a fragment that cannot come back empty has
    # no arrangement that makes it.
    tracking = []
    if vary:
        vary_name, _, listed = vary.partition("=")
        vary_name = vary_name.strip()
        vary_values = [v.strip() for v in listed.split(",") if v.strip()]

        def with_value(one):
            """The caller's values with `vary_name` set to `one`."""
            kept = [v for v in (values or []) if v.get("name") != vary_name]
            return kept + [{"name": vary_name, "value": one}]

        def answered(record):
            """What the named field came back as, or None if it did not run."""
            if not record or not record.get("ok"):
                return None
            got = (record.get("provides") or {}).get(vary_field)
            return None if got is None else str(got)

        if vary_values:
            # THE FIRST TRACKED INPUT IS THE POSITIVE PHASE. A record needs one
            # and this is honestly it: the fragment run for real, with a value
            # the tracking set names.
            first = run_fragment("positive", in_document,
                                 "run with %s=%s - the first of %d tracked inputs"
                                 % (vary_name, vary_values[0], len(vary_values)),
                                 reset_chain, using=with_value(vary_values[0]))
            got = answered(first)
            if got is not None:
                tracking.append({"input": "%s=%s" % (vary_name, vary_values[0]),
                                 "field": vary_field, "value": got})

        for one in vary_values[1:]:
            record = run_fragment("tracked %s=%s" % (vary_name, one), in_document,
                                  "run with %s=%s" % (vary_name, one),
                                  reset_chain, using=with_value(one))
            got = answered(record)
            if got is None:
                print("  %s=%s did not answer with '%s' - not recorded"
                      % (vary_name, one, vary_field))
                continue
            tracking.append({"input": "%s=%s" % (vary_name, one),
                             "field": vary_field, "value": got})

        print("")
        print("TRACKED %d input(s) of '%s' against '%s':"
              % (len(tracking), vary_name, vary_field))
        for row in tracking:
            print("  %-34s %s" % (row["input"], row["value"][:60]))
        if len(tracking) < 3:
            print("")
            print("FEWER THAN THREE ANSWERED. heron_validate will refuse this")
            print("set, and it is right to: two cannot show an answer following")
            print("an input. The record is written anyway so the runs are not")
            print("lost - read it and arrange more values.")

    # `negative_setup_values` COUNTS AS A NEGATIVE CASE. Left out, a proof
    # whose two legs differ only in the ARRANGEMENT fell through to the
    # interactive prompt and waited at a keyboard nobody was at.
    # `vary` REPLACES THE NEGATIVE CASE and must not fall through to the
    # keyboard prompt below. A fragment that cannot come back empty has no
    # negative arrangement to ask a person for, which is the whole reason
    # D-53 exists.
    if vary:
        pass
    elif negative_in or negative_values or negative_setup_values:
        # THE NEGATIVE CASE FOR A VIEW FRAGMENT IS ANOTHER VIEW, and until this
        # existed there was no way to say so: `validate` could change the
        # document between phases but not the caller's values, so anything
        # taking a view could only be proved by a person clearing a selection
        # that was never bound in the first place. 53 fragments take a view.
        #
        # THE TWO COMBINE, and they have to. A view that carries an override
        # exists only in one of the two models open here, and a view that
        # carries none only in the other - so the arrangement is "that model,
        # and this view in it", and neither half alone can say it. They were
        # an if/elif for one commit and the second was unreachable whenever a
        # document was named.
        parts = []
        if negative_in:
            parts.append("against '%s'" % negative_in)
        if negative_values:
            parts.append("with " + ", ".join("%s=%s" % (v["name"], v["value"])
                                             for v in negative_values))
        # THE ARRANGEMENT GOES IN THE RECORD TOO. A proof whose legs differ
        # only in the setup chain would otherwise be written down as "run
        # instead" with nothing after it, and a reader could not tell the two
        # legs apart at all.
        if negative_setup_values:
            parts.append("arranged with " + ", ".join(
                "%s=%s" % (v["name"], v["value"]) for v in negative_setup_values))
        run_fragment("negative", negative_in or in_document,
                     "run %s instead - chosen because it should not contain what "
                     "this fragment reports" % " ".join(parts),
                     reset_chain, using=negative_values or None, negative=True)
    else:
        print("")
        print("NEGATIVE CASE. Arrange an answer that must come back empty -")
        print("clear the selection in Revit, or switch to a model without the")
        print("thing this fragment reports. Then press Enter.")
        print("Press Ctrl+C instead to stop, and the record will say the")
        print("negative case was never run rather than pretending otherwise.")
        try:
            _prompt()
        except (EOFError, KeyboardInterrupt):
            print("")
            print("skipped - the draft will say NOT ESTABLISHED")
        else:
            run_fragment("negative", in_document,
                         "run after the state was arranged by hand so the "
                         "answer had to be empty", reset_chain)

    if cross:
        # A CROSS-CHECK THAT DID NOT RUN MUST SAY SO. Silence here would reach
        # the draft as NOT ESTABLISHED, which is honest, and leave whoever asked
        # for it believing it had been done - which is not.
        if cross == "count_elements":
            reply = bridge.request("count_elements")
            arranged = ("count_elements counted the whole active document by a "
                        "different route")
            provides = {"count": (reply or {}).get("count")}
        elif cross == "duct":
            print("")
            print("The duct cross-check CHANGES what is selected in Revit.")
            reply = bridge.request("select_by_category", op_args={"category": "ducts"})
            arranged = ("select_by_category collected every duct with a "
                        "FilteredElementCollector - the model side, not the UI side")
            provides = {"selected": (reply or {}).get("selected")}
        else:
            reply, arranged, provides = None, None, None
            print("")
            print("Unknown cross-check '%s'. It is 'count_elements' or 'duct'." % cross)
            print("Nothing was run for it, and the draft will say NOT ESTABLISHED.")

        if arranged is not None:
            if reply is not None and reply.get("ok"):
                phases.append({"phase": "second_route", "ok": True,
                               "provides": provides,
                               "document": reply.get("document"),
                               "arranged": arranged})
                print("%-14s ok" % "second_route")
            else:
                print("%-14s %s - not recorded"
                      % ("second_route",
                         (reply or {}).get("error") or "no reply"))

    bridge.release()
    bridge.close()

    # ROW 15. Built from where the phases RAN, not from what was in front.
    model = model_line(opening, phases, revit_version, pid)

    record = {
        "run_record": out or "brain/proof-drafts/runs/%s.json" % name,
        "fragment": name,
        "date": time.strftime("%Y-%m-%d"),
        "model": model,
        "phases": phases,
    }
    if tracking:
        record["tracking"] = tracking
    path = out or os.path.join(root, "brain", "proof-drafts", "runs",
                               "%s.json" % name)
    folder = os.path.dirname(os.path.abspath(path))
    if not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(record, indent=2, sort_keys=True))

    print("")
    print("Recorded %s" % path)
    print("Nothing has been proved. Turn it into a draft, then read it:")
    print("  python brain/heron_validate.py draft %s --from \"%s\"" % (name, path))
    return 0


def _prompt():
    """input() on both Pythons. Its own function so the tests can stand in for it."""
    try:
        return raw_input()                                   # noqa: F821
    except NameError:
        return input()


def cmd_release():
    """
    Give this Revit back by hand.

    THE ORDINARY CASE NEEDS NOTHING - `prove` hands back when it finishes. This
    is for the times it could not: a command killed part-way, a chat closed
    mid-job, or a session id that was set for one run and is now being reused.

    IT CANNOT TAKE A REVIT FROM ANOTHER CHAT. The bridge only lets the holder
    give up its own claim, so running this while somebody else is working says
    so and changes nothing. The one thing that frees another chat's session is
    still the Heron button, which is a person deciding at the machine.
    """
    live, starting, _, mismatched = discover()
    if not live and starting:
        print("Revit is still starting - its bridge is not answering yet.")
        return 1
    if not live:
        print("No Revit is connected.")
        report_mismatched(mismatched)
        return 1

    for index, bridge in enumerate(live):
        reply = bridge.request("release")
        if reply is None:
            print("%s  no reply" % describe(bridge, index))
        else:
            print("%s  %s" % (describe(bridge, index),
                              reply.get("message") or reply.get("error")))
        bridge.close()
    return 0


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
    if argv[1] == "release":
        return cmd_release()
    if argv[1] == "prove":
        rest, pairs, _, _, _ = pull_values(argv[2:])
        if rest is None:
            return 2
        # prove --in "Project1" list-levels ...  reads a model that is open
        # but not necessarily the one in front.
        rest, session = pull_session(rest)
        in_document = None
        if len(rest) >= 2 and rest[0] == "--in":
            in_document = rest[1]
            rest = rest[2:]
        if not rest:
            print("Which fragments? e.g. prove list-levels list-grids")
            print("               or  prove --in \"Project1\" list-levels")
            return 2
        values = caller_values(pairs)
        if values is None:
            return 2
        return cmd_prove(rest, in_document, values, session=session)
    if argv[1] == "fragment":
        rest, pairs, _, _, _ = pull_values(argv[2:])
        # --write runs it inside a transaction so a MODIFY fragment can run.
        # --apply is the separate, deliberate act of KEEPING what it did; on
        # its own --apply means nothing, because a read has nothing to keep.
        writing = rest is not None and "--write" in rest
        apply_it = rest is not None and "--apply" in rest
        expect = None
        if rest is not None:
            for i, token in enumerate(rest):
                if token == "--expect-from" and i + 1 < len(rest):
                    expect = rest[i + 1]
                    break
            if expect is not None:
                at = rest.index("--expect-from")
                rest = rest[:at] + rest[at + 2:]
            rest = [r for r in rest if r not in ("--write", "--apply")]
        if rest is None or not rest:
            print("Which fragment? e.g. list-levels")
            print("  --view \"Level 1\"        a view the fragment asks the caller for")
            print("  --set name=value        any other value it asks for")
            print("  --session <pid>         which Revit, when more than one is connected")
            print("  --write                 run a MODIFY fragment, in a transaction")
            print("  --write --apply         ...and KEEP what it did (one Ctrl+Z undoes it)")
            print("  --expect-from <fragment>  consume what THAT fragment left, instead")
            print("                          of resetting. Refused if something else")
            print("                          left it. Add \"where k=v\" to check its")
            print("                          inputs too:")
            print("                            --expect-from \"select-by-categories")
            print("                             where categories=Pipes\"")
            return 2
        if apply_it and not writing:
            print("--apply only means something with --write. A read leaves nothing to keep.")
            return 2
        rest, session = pull_session(rest)
        if not rest:
            print("Which fragment? e.g. list-levels")
            return 2
        values = caller_values(pairs)
        if values is None:
            return 2
        return cmd_fragment(rest[0], values, writing, apply_it, session=session,
                            expect=expect)
    if argv[1] == "validate":
        rest, pairs, negatives, setup_pairs, negative_setup_pairs = pull_values(argv[2:])
        if rest is None:
            return 2
        # --write proves a MODIFY fragment. `apply` is never sent from here, so
        # both phases run for real and are rolled back: the record is what the
        # fragment DID, and the model ends untouched.
        writing = "--write" in rest
        rest = [r for r in rest if r != "--write"]
        # --keep-chain keeps what the setup chain left, instead of resetting it
        # before the fragment under test. Defect row 11, and OPT-IN because the
        # chain outranks the selection - see cmd_validate.
        keep_chain = "--keep-chain" in rest
        rest = [r for r in rest if r != "--keep-chain"]
        # --allow-publish runs a fragment declaring PUBLISH or ADMIN. Refused
        # without it; see the note beside the check in cmd_validate.
        allow_publish = "--allow-publish" in rest
        rest = [r for r in rest if r != "--allow-publish"]
        # --setup names a fragment to run BEFORE each phase, repeatable and in
        # order. It re-makes the arrangement - typically select-by-category-name
        # then set-selection - because a rolled-back write clears the selection.
        rest, session = pull_session(rest)
        setup = []
        # --vary NAME=a,b,c PROVES A FRAGMENT THAT CANNOT COME BACK EMPTY.
        # D-53: some fragments describe whatever they are handed, so no
        # arrangement makes the answer empty and D-30's negative leg cannot be
        # met by one - COUNT_ELEMENTS is the example the decision was written
        # against. The leg is met instead by the answer FOLLOWING the input
        # across several different inputs. NEEDS-CHECKING Group W names the
        # three fragments this unblocks and the three skills behind them.
        #
        # `heron_validate` HAS JUDGED THIS SINCE THE DECISION WAS WRITTEN and
        # nothing produced it. It reads `record["tracking"]`, refuses fewer
        # than three rows in those words, refuses rows that all came back the
        # same, and writes the negative text itself. Only the RUNNING half was
        # missing, which is why this is an addition rather than a mechanism.
        vary, vary_field = None, None
        setup, cleaned, skip = [], [], False
        for index, token in enumerate(rest):
            if skip:
                skip = False
                continue
            if token == "--setup":
                if index + 1 >= len(rest):
                    print("--setup needs a fragment name after it")
                    return 2
                setup.append(rest[index + 1])
                skip = True
                continue
            if token == "--vary":
                if index + 1 >= len(rest):
                    print("--vary needs NAME=value,value,value after it")
                    return 2
                vary = rest[index + 1]
                skip = True
                continue
            if token == "--vary-field":
                if index + 1 >= len(rest):
                    print("--vary-field needs the name of a declared result")
                    return 2
                vary_field = rest[index + 1]
                skip = True
                continue
            cleaned.append(token)
        rest = cleaned

        if vary:
            # THE FIELD IS NAMED AND NEVER GUESSED. Which result has to follow
            # the input is knowledge OF THE FRAGMENT, and `generate-jobs.py`
            # leaves `expect:` blank for the same reason. Picking the first
            # declared result here would quietly track an accounting counter on
            # some fragment and call the answer proved.
            if not vary_field:
                print("--vary needs --vary-field too: name the ONE declared")
                print("result that has to follow the input. Guessing it is how")
                print("a tracking set follows a counter and reads as a proof.")
                return 2
            if "=" not in vary:
                print("--vary takes NAME=value,value,value - at least three")
                print("values, because two cannot show an answer FOLLOWING an")
                print("input (D-53, and heron_validate refuses two by name).")
                return 2
        options = {"in_document": None, "cross": None, "negative_in": None,
                   "out": None}
        flags = {"--in": "in_document", "--cross": "cross",
                 "--negative-in": "negative_in", "--out": "out"}
        while len(rest) >= 2 and rest[0] in flags:
            options[flags[rest[0]]] = rest[1]
            rest = rest[2:]
        if len(rest) != 1:
            print("Which fragment? e.g. validate list-levels")
            print("  --in \"Doc\"           run against a model that is open but not in front")
            print("  --negative-in \"Doc\"  take the negative case from a second model")
            print("  --cross count_elements | duct")
            print("  --view \"Level 1\"     a view the fragment asks the caller for")
            print("  --set name=value     any other value it asks for")
            print("  --negative-view \"X\"  the view for the NEGATIVE case - one that")
            print("                       should NOT have what this reports")
            print("  --negative-set n=v    any other value for the negative case")
            print("  --allow-publish       run a PUBLISH or ADMIN fragment. Refused")
            print("                        without it - proving one is a decision")
            print("  --write               a MODIFY fragment - both phases run inside a")
            print("                        transaction and are ROLLED BACK, keeping nothing")
            print("  --setup <fragment>    run this BEFORE each phase to re-make the")
            print("                        arrangement. Repeatable, in order. A rolled-back")
            print("                        write clears the selection, so a selection-based")
            print("                        write needs it: --setup select-by-category-name")
            print("                        --setup set-selection")
            print("  --keep-chain          keep what the setup chain left, instead of")
            print("                        resetting it. Needed when the setup PRODUCES a")
            print("                        value the fragment consumes - the selection")
            print("                        survives a reset and a value does not. Needs")
            print("                        --setup, and changes what binds: the chain")
            print("                        outranks the selection")
            return 2
        values = caller_values(pairs)
        if values is None:
            return 2
        negative_values = caller_values(negatives)
        if negative_values is None:
            return 2
        # ROW 142. Absent, these are empty and every setup step gets exactly
        # what the fragment gets - which is what happened before this existed.
        setup_values = caller_values(setup_pairs)
        if setup_values is None:
            return 2
        negative_setup_values = caller_values(negative_setup_pairs)
        if negative_setup_values is None:
            return 2
        return cmd_validate(rest[0], session=session, values=values,
                            negative_values=negative_values,
                            setup_values=setup_values,
                            negative_setup_values=negative_setup_values,
                            vary=vary, vary_field=vary_field,
                            writing=writing,
                            setup=setup, keep_chain=keep_chain,
                            allow_publish=allow_publish, **options)

    print(__doc__.strip())
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv))
