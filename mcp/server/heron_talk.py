#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Heron Talk - the other direction (D-104).

Until this file, Heron worked one way: the modeller typed into a Claude Code
terminal outside Revit, and the work reached Revit. Talk is the way back. The
Talk button in Revit saves what the modeller typed or said, with what they had
selected, and this file hands it to the Claude Code session that is ALREADY
OPEN - through Claude Code's channels, which let an MCP server push a message
into a running session. Claude reads it exactly as if it had been typed there.

    Revit: Talk  ->  talk\\<revit pid>\\message-7.json      (the add-in)
                 ->  claimed here, pushed as a channel event
                 ->  Claude works, through Heron's tools as ever
                 ->  Revit

WHAT IT DOES NOT DO. It does not decide what the words mean - that is the
host's (D-01, D-34) - and it adds no word of its own to them beyond saying
where they came from. It never talks to Revit: messages arrive as files, and
the one thing it asks of the bridge is which Revits are live.

WHY FILES. The bridge hands its pipe to the newest connection (docs/25), so a
listener polling the pipe would cut off this chat's own requests - and
batch-prove's - in the middle of a reply. The add-in writes a message once and
this claims it once, by RENAMING it: two chats listening on one PC cannot both
take the same message, because only one rename can succeed.

WHY IT IS OFF UNLESS ASKED. A chat started without the channel flag drops
channel events without a word (Claude Code's own documentation says so), so a
server that claimed messages in such a chat would lose them. Only a chat
started by mcp/heron-talk.cmd sets HERON_TALK=on, and only such a chat
listens - which is also the only kind the Talk button will send to, because
it looks for this file's heartbeat first.

THE SELECTION IS SAVED, NOT SENT. A message carries the count, the categories
and the selection's number; the list itself - id, UniqueId, category, name for
every element - is its own file, read by `heron_selection` when the chat needs
it. Reading it touches neither the model nor the pipe, so it can never
interrupt the modeller, and the message stays small however much is selected.

THE FORMAT belongs to two files in two languages: HeronTalkMailbox.cs writes
it and this reads it. tests/test_talk_contract.py runs the one against the
other; FORMAT below is the number both carry.
"""

import io
import json
import os
import re
import sys
import time

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "client"))

import heron_bridge_client as bridge          # noqa: E402

#: The file format HeronTalkMailbox.cs writes. A message in another format is
#: not guessed at: the chat is told one arrived and could not be read.
FORMAT = 1

#: The switch, set by mcp/heron-talk.cmd and passed on by .mcp.json.
ENV = "HERON_TALK"

#: What Claude Code's channels are called, and the event that carries one.
CHANNEL = "claude/channel"
CHANNEL_EVENT = "notifications/claude/channel"

#: How often the folder is looked at. Half a second is quick enough that a
#: modeller who presses Enter and looks at the terminal sees it arrive.
POLL_S = 0.5

#: How often this chat says it is listening. HeronTalkMailbox.ListenerFresh
#: allows twenty seconds, so four missed beats still count as listening.
HEARTBEAT_S = 5.0

#: When the SDK offers no way to be told the handshake has finished, how long
#: to wait before the first event. Claude Code finishes it in well under this.
HANDSHAKE_GRACE_S = 5.0

#: Elements per page of heron_selection.
PAGE = 200

_MESSAGE = re.compile(r"^message-(\d+)\.json$")
_SELECTION = re.compile(r"^selection-(\d+)\.json$")
_META_KEY = re.compile(r"^[A-Za-z0-9_]+$")


class TalkError(Exception):
    """A message or a selection that cannot be read, said in plain words."""


# --------------------------------------------------------------------- basics

def enabled(environ=None):
    """Is Talk on for this chat? Only mcp/heron-talk.cmd turns it on."""
    env = os.environ if environ is None else environ
    return (env.get(ENV) or "").strip().lower() in ("on", "1", "true", "yes")


def root():
    """The talk folder, read at call time so a test can point it elsewhere."""
    return bridge.TALK_DIR


def instructions():
    """
    What a Talk chat is told before its first event, beside the Constitution.

    It says how the events look, that they are the modeller's own words, and
    the two things a chat would otherwise get wrong: the modeller is looking
    at Revit rather than at the terminal, and "this" means the SAVED
    selection, not whatever is selected by the time the chat acts.
    """
    return (
        "HERON TALK IS ON IN THIS CHAT (D-104). The modeller can also speak to you from inside "
        "Revit. What they type or say there arrives as a <channel source=\"heron\" message=\"..\" "
        "selection=\"..\" ...> event. Treat it as their own request, exactly as if it had been "
        "typed here, and do it with Heron's tools. They are looking at Revit, not at this "
        "terminal, so keep what you write short. What they had selected is SAVED under the "
        "event's selection number: read it with heron_selection - passing the event's revit "
        "attribute as revit= when more than one Revit is open - before acting on \"this\", "
        "\"these\" or \"the selected ...\", and never assume the live selection is still the "
        "same - they keep working while you do. The event names the Revit, model and view it "
        "came from; if Heron's answers name a different model, stop and say so rather than act.")


def read_json(path):
    """One file, as a dict. Raises TalkError with a sentence a person can act on."""
    try:
        with io.open(path, encoding="utf-8-sig") as handle:
            value = json.load(handle)
    except (IOError, OSError) as exc:
        raise TalkError("Heron could not read %s: %s" % (os.path.basename(path), exc))
    except ValueError as exc:
        raise TalkError("%s is not readable JSON: %s" % (os.path.basename(path), exc))
    if not isinstance(value, dict):
        raise TalkError("%s does not hold a Talk record." % os.path.basename(path))
    return value


def _number(value, default=0):
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def _revit_folders(folder_root):
    """(pid, path) for every Revit that has said anything, lowest pid first."""
    try:
        names = os.listdir(folder_root)
    except OSError:
        return []
    found = []
    for name in names:
        path = os.path.join(folder_root, name)
        if name.isdigit() and os.path.isdir(path):
            found.append((int(name), path))
    return sorted(found)


# ------------------------------------------------------------------- messages

def pending(folder_root=None):
    """
    Every unclaimed message, oldest first, as (pid, number, path).

    Only `message-N.json`: a message still being written is `.tmp` and one
    already claimed is `.taken.json`, and neither is anybody's to take.
    """
    folder_root = folder_root or root()
    found = []
    for pid, folder in _revit_folders(folder_root):
        try:
            names = os.listdir(folder)
        except OSError:
            continue
        for name in names:
            match = _MESSAGE.match(name)
            if match:
                found.append((pid, int(match.group(1)), os.path.join(folder, name)))
    return sorted(found, key=lambda item: (item[1], item[0]))


def claim(path):
    """
    Take one message, or None when another chat took it first.

    BY RENAMING IT. A rename either happens or it does not, so of two chats
    racing for one message exactly one succeeds - and the loser finds the file
    gone rather than half-read. The claimed file is kept beside the others:
    it is part of the short memory until that Revit closes.
    """
    taken = path[:-len(".json")] + ".taken.json"
    try:
        os.rename(path, taken)
    except OSError:
        return None
    return read_json(taken)


def channel_event(message):
    """
    The words, and where they came from, as (content, meta) for one event.

    THE MODELLER'S WORDS COME FIRST AND UNCHANGED. What follows them is a
    plain statement of source - which Revit, model, view, and what was
    selected - never a rewording. Meta keys are identifiers only, because
    Claude Code drops a key with anything else in it without saying so.
    """
    if _number(message.get("format"), -1) != FORMAT:
        content = ("A message came from Revit's Talk button in a format this Heron does not read "
                   "(format %s; this server reads %d), so its words were not read. Update Heron "
                   "so Revit and this server match, then ask the modeller to send it again."
                   % (message.get("format"), FORMAT))
        return content, {"problem": "format"}

    text = (message.get("text") or "").strip()
    number = _number(message.get("message"))
    selected = _number(message.get("selected"))
    selection = message.get("selection")

    where = "From Revit %s" % (message.get("revitVersion") or "")
    if message.get("document"):
        where += ', model "%s"' % message["document"]
    if message.get("view"):
        where += ', view "%s"' % message["view"]

    lines = [text, "", "(" + where.strip() + ".)"]
    if selected and selection is not None:
        lines.append("(Selection %d saved: %s - %s. Read it with heron_selection(%d).)"
                     % (_number(selection), _count(selected, "element"),
                        _categories(message.get("categories")), _number(selection)))
    else:
        lines.append("(Nothing was selected.)")

    meta = {
        "message": str(number),
        "revit": str(_number(message.get("revitPid"))),
        "selection": str(_number(selection)) if selection is not None else "none",
        "selected": str(selected),
    }
    return "\n".join(lines), dict((k, v) for k, v in meta.items() if _META_KEY.match(k))


def _count(number, noun):
    return "%s %s%s" % ("{:,}".format(number), noun, "" if number == 1 else "s")


def _categories(categories, limit=6):
    """'Ducts 10, Duct Fittings 2' - most first, as the add-in sorted them."""
    parts = []
    for entry in categories or []:
        if not isinstance(entry, dict):
            continue
        parts.append("%s %s" % (entry.get("name") or "(no category)",
                                "{:,}".format(_number(entry.get("count")))))
    if not parts:
        return "no categories read"
    shown = ", ".join(parts[:limit])
    if len(parts) > limit:
        shown += ", and %d more categories" % (len(parts) - limit)
    return shown


def notification(content, meta):
    """
    One channel event, shaped for whichever MCP SDK is installed.

    In 2.x a notification goes on the stream as it is; in 1.x it is wrapped
    in JSONRPCMessage first. SessionMessage exists in both from 1.x's later
    releases, and an SDK older than that cannot serve Talk at all.
    """
    import mcp.types as types
    note = types.JSONRPCNotification(jsonrpc="2.0", method=CHANNEL_EVENT,
                                     params={"content": content, "meta": meta})
    return session_message(note)


def session_message(message):
    """Wrap one JSON-RPC message the way this SDK's streams carry it."""
    import mcp.types as types
    from mcp.shared.message import SessionMessage
    wrapper = getattr(types, "JSONRPCMessage", None)
    if isinstance(wrapper, type):
        message = wrapper(message)
    return SessionMessage(message)


# ------------------------------------------------------------------ selection

def find_selection(number=0, revit=None, bound=None, folder_root=None):
    """
    The saved selection's path and number, or TalkError saying why not.

    A NUMBER IS LOOKED FOR IN EVERY REVIT, not only the one this chat is
    using. A message can come from a Revit other than the chosen one - the
    chat is then told, and asks - and reading the chosen Revit's selection 3
    for a message that meant another Revit's selection 3 would answer about
    the wrong elements with nothing to show it. So a number two Revits both
    used is a question, never a guess. `revit` - the event's own `revit`
    attribute - settles it.

    `bound` is the Revit this chat is using, and it is asked only for "the
    latest" (number 0), which means nothing across two Revits.
    """
    folder_root = folder_root or root()
    folders = _revit_folders(folder_root)
    if revit:
        folders = [(p, f) for p, f in folders if p == revit]

    found = []
    for p, folder in folders:
        try:
            names = os.listdir(folder)
        except OSError:
            continue
        for name in names:
            match = _SELECTION.match(name)
            if match:
                found.append((int(match.group(1)), p, os.path.join(folder, name)))

    if not found:
        raise TalkError("No selection has been saved%s. A selection is saved when the modeller "
                        "sends a message from Revit's Talk button with something selected, and "
                        "it is forgotten when that Revit closes."
                        % ("" if not revit else " from that Revit"))

    number = _number(number)
    if number <= 0:
        pids = sorted(set(p for _, p, _ in found))
        if len(pids) > 1 and bound in pids:
            pids = [bound]
        if len(pids) > 1:
            raise TalkError("More than one Revit has saved selections, so the latest is not one "
                            "thing. Name the selection number from the message, and pass its "
                            "revit attribute as revit=.")
        found = [f for f in found if f[1] == pids[0]]
        number = max(n for n, _, _ in found)

    matches = [(p, path) for n, p, path in found if n == number]
    if not matches:
        kept = sorted(set(n for n, _, _ in found))
        raise TalkError("There is no saved selection %d. Heron keeps the selections of the last "
                        "20 messages from each Revit; the ones kept now are %s."
                        % (number, ", ".join(str(n) for n in kept)))
    if len(matches) > 1:
        raise TalkError("Selection %d was saved by more than one Revit, so Heron will not pick "
                        "one. Pass the message's revit attribute as revit=." % number)
    return matches[0][1], number


def selection_answer(number=0, page=1, revit=None, bound=None, folder_root=None):
    """What `heron_selection` says: the saved list, a page at a time, as text."""
    try:
        path, number = find_selection(number, revit, bound, folder_root)
        record = read_json(path)
    except TalkError as why:
        return str(why)

    if _number(record.get("format"), -1) != FORMAT:
        return ("Selection %d was saved in a format this Heron does not read (format %s). "
                "Update Heron so Revit and this server match." % (number, record.get("format")))

    elements = [e for e in (record.get("elements") or []) if isinstance(e, dict)]
    total = _number(record.get("total"))
    pages = max(1, (len(elements) + PAGE - 1) // PAGE)
    page = min(max(1, _number(page, 1)), pages)
    first = (page - 1) * PAGE

    where = "Revit %s" % (record.get("revitVersion") or "")
    if record.get("document"):
        where += ', model "%s"' % record["document"]
    if record.get("view"):
        where += ', view "%s"' % record["view"]

    lines = [
        "Selection %d, saved %s from %s." % (number, record.get("takenUtc") or "(time not recorded)",
                                             where),
        "%s selected: %s." % ("{:,}".format(total), _categories(record.get("categories"), 12)),
    ]
    if record.get("truncated"):
        lines.append("Only the first %s were saved - the list stops there so a select-all on a "
                     "large model cannot hold Revit up." % "{:,}".format(len(elements)))
    if _number(record.get("unreadable")):
        lines.append("%s could not be read when it was saved - deleted, or changed underneath - "
                     "and is counted but not listed." % _count(_number(record["unreadable"]),
                                                              "element"))
    lines.append("This is what was selected when the modeller spoke. The live selection may have "
                 "changed since, and so may the model: check an element still exists before "
                 "acting on it.")
    lines.append("")

    if not elements:
        lines.append("No element could be listed.")
        return "\n".join(lines)

    shown = elements[first:first + PAGE]
    lines.append("Elements %s-%s of %s (id | UniqueId | category | name):"
                 % ("{:,}".format(first + 1), "{:,}".format(first + len(shown)),
                    "{:,}".format(len(elements))))
    for e in shown:
        lines.append("  %s | %s | %s | %s" % (e.get("id") or "", e.get("uniqueId") or "",
                                              e.get("category") or "(no category)",
                                              e.get("name") or ""))
    if page < pages:
        lines.append("")
        lines.append("More: heron_selection(%d, page=%d)." % (number, page + 1))
    return "\n".join(lines)


# ------------------------------------------------------------------ listening

def heartbeat(folder_root, listener_pid):
    """
    Say this chat is listening. The Talk button looks for a fresh one of these
    before it sends, so a chat that has closed stops being offered within the
    add-in's ListenerFresh.
    """
    os.makedirs(folder_root, exist_ok=True)
    path = os.path.join(folder_root, "listener-%d.json" % listener_pid)
    record = {"format": FORMAT, "pid": listener_pid,
              "touchedUtc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())}
    temp = path + ".tmp"
    with io.open(temp, "w", encoding="utf-8") as handle:
        handle.write(json.dumps(record))
    os.replace(temp, path)
    return path


def stop_heartbeat(folder_root, listener_pid):
    """This chat has stopped listening. Best effort: a file left behind goes stale on its own."""
    try:
        os.remove(os.path.join(folder_root, "listener-%d.json" % listener_pid))
    except OSError:
        pass


def live_revits():
    """Which Revits have a live bridge, by process id. Asked of the discovery files."""
    live, _starting, _stale, _mismatched = bridge.discover()
    return set(b.pid for b in live)


def collect(folder_root, live_pids):
    """
    Claim every message waiting from a Revit whose bridge is live.

    A message from a Revit that is not live is left where it is: the button
    refuses to send from a Revit that is not connected, so what remains is a
    Revit that disconnected in the half-second after sending, and the message
    waits for it rather than being answered about a model nobody can reach.
    """
    claimed = []
    waiting = pending(folder_root)
    if not waiting:
        return claimed
    live = live_pids() if callable(live_pids) else set(live_pids or ())
    for pid, _number_, path in waiting:
        if pid not in live:
            continue
        try:
            message = claim(path)
        except TalkError as why:
            message = {"format": None, "problem": str(why)}
        if message is not None:
            claimed.append(message)
    return claimed


# -------------------------------------------------------------------- serving

def lowlevel(server):
    """
    The SDK's low-level server behind the one heron_mcp_server builds.

    Talk has to add a capability to the handshake, and the high-level class
    offers no way to. It is found under its 2.x name, then its 1.x name - the
    same newest-first lookup heron_mcp_server uses for the class itself - and
    its absence is said, not guessed around.
    """
    for name in ("_lowlevel_server", "_mcp_server"):
        low = getattr(server, name, None)
        if low is not None and hasattr(low, "create_initialization_options"):
            return low
    return None


def _when_initialized(low, event):
    """
    Be told when the host has finished the handshake. A channel event sent
    before it may be dropped, and a dropped event is a modeller's lost words.

    True when a hook was fitted. False when this SDK offers none - the caller
    then waits HANDSHAKE_GRACE_S instead.
    """
    add = getattr(low, "add_notification_handler", None)
    if add is not None:
        import mcp.types as types

        async def on_initialized(ctx, params):              # noqa: ARG001 - the signature is the SDK's
            event.set()

        add("notifications/initialized", types.NotificationParams, on_initialized)
        return True

    handlers = getattr(low, "notification_handlers", None)
    import mcp.types as types
    kind = getattr(types, "InitializedNotification", None)
    if isinstance(handlers, dict) and kind is not None:
        async def on_initialized_v1(notification):          # noqa: ARG001
            event.set()

        handlers[kind] = on_initialized_v1
        return True
    return False


async def serve_streams(low, read_stream, write_stream, on_message=None, folder_root=None,
                        listener_pid=None, live_pids=None, poll=POLL_S, log=None):
    """
    Serve one connection with the channel declared, and listen while it lasts.

    The server runs exactly as MCPServer.run_stdio_async runs it, with one
    difference: the handshake says `claude/channel`, which is what makes Claude
    Code register a listener for this server's events. Beside it, one task
    watches the talk folder and pushes each claimed message into the session.
    """
    import anyio

    say = log or (lambda text: sys.stderr.write(text + "\n"))
    folder_root = folder_root or root()
    listener_pid = listener_pid or os.getpid()
    ready = anyio.Event()
    hooked = _when_initialized(low, ready)
    options = low.create_initialization_options(experimental_capabilities={CHANNEL: {}})

    async def send(item):
        await write_stream.send(item)

    async def grace():
        await anyio.sleep(HANDSHAKE_GRACE_S)
        ready.set()

    async def watch():
        await ready.wait()
        last_beat = 0.0
        try:
            while True:
                now = time.monotonic()
                if now - last_beat >= HEARTBEAT_S:
                    try:
                        heartbeat(folder_root, listener_pid)
                    except OSError as exc:
                        say("Heron Talk could not say it is listening: %s" % exc)
                    last_beat = now

                try:
                    arrived = await anyio.to_thread.run_sync(
                        collect, folder_root, live_pids or live_revits)
                except Exception as exc:                    # noqa: BLE001 - the loop must survive
                    say("Heron Talk could not look for messages: %s" % exc)
                    arrived = []

                for message in arrived:
                    if message.get("problem"):
                        content, meta = ("A message came from Revit's Talk button and could not "
                                         "be read: %s" % message["problem"], {"problem": "unreadable"})
                    else:
                        content, meta = channel_event(message)
                        note = None
                        if on_message is not None and "problem" not in meta:
                            try:
                                note = on_message(message)
                            except Exception as exc:        # noqa: BLE001 - a note is optional
                                note = "Heron could not check which Revit this chat is using: %s" % exc
                        if note:
                            content += "\n\n" + note
                    await send(notification(content, meta))
                await anyio.sleep(poll)
        finally:
            stop_heartbeat(folder_root, listener_pid)

    async with anyio.create_task_group() as group:
        if not hooked:
            group.start_soon(grace)
        group.start_soon(watch)
        try:
            await low.run(read_stream, write_stream, options)
        finally:
            group.cancel_scope.cancel()


def serve(server, on_message=None):
    """
    Serve over stdio with Talk on. Falls back to the plain server - tools and
    all - when this SDK cannot declare the channel, and says why on stderr,
    because a chat that works without Talk beats one that does not start.
    """
    low = lowlevel(server)
    why = "the MCP SDK's server has no low-level server Heron can add the channel to"
    try:
        import anyio
        from mcp.server.stdio import stdio_server
        from mcp.shared.message import SessionMessage          # noqa: F401 - asked for, not used
    except ImportError as exc:
        low, why = None, exc

    if low is None:
        sys.stderr.write("Heron Talk is not available with this MCP SDK (%s), so this chat "
                         "serves without it. Update the SDK:  pip install --user --upgrade mcp\n"
                         % why)
        server.run()
        return

    async def main():
        async with stdio_server() as (read_stream, write_stream):
            await serve_streams(low, read_stream, write_stream, on_message)

    anyio.run(main)
