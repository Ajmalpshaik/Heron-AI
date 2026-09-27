#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron Talk served by a real MCP SDK - the channel itself (D-104).

    python tests/test_talk_served.py

tests/test_talk.py proves the files and the words. This proves the part only
an SDK can: that Heron's own server, run the way a Talk chat runs it, TELLS
the host it is a channel, and that a message dropped into the talk folder
comes out of the server as the event Claude Code listens for. It speaks
JSON-RPC to the server over in-memory streams, the way Claude Code speaks it
over stdio - the legacy handshake Claude Code uses, then the event, then a
tool call.

WHAT IT PROVES, when an SDK is installed
  1. With Talk on, the handshake declares `claude/channel`. Served the
     ordinary way, it does not - so a chat not started for Talk is exactly
     what it was before.
  2. Nothing is pushed before the host says the handshake is finished.
  3. A message in the talk folder arrives as `notifications/claude/channel`,
     with the modeller's words first and the routing in `meta`, and the file
     is claimed.
  4. The chat's heartbeat is written while it serves and removed when the
     host goes away.
  5. heron_selection answers through the SDK's own dispatch, from the file.
  6. A Talk chat's instructions carry the Talk paragraph.
  7. _talk_arrived binds the chat to the Revit a message came from - and
     does not overturn a Revit the modeller chose earlier.

WHAT IT CANNOT DO
  It is not Claude Code, so it cannot show that Claude Code registers the
  channel or delivers the event to the model - that needs the development
  flag on the owner's PC, NEEDS-CHECKING AS2. And it exits 3 without the
  SDK: not a pass, nothing was checked.
"""

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
COULD_NOT_RUN = 3
FAILURES = []

# A TALK CHAT, as mcp/heron-talk.cmd starts one - set before the server is
# imported, because the server reads it when it builds its instructions.
os.environ["HERON_TALK"] = "on"

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(path, record):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(json.dumps(record))


def main():
    # BaseException, not ImportError: on some images the SDK imports and then
    # PANICS inside `cryptography` (heron-ship section 2), and that must read
    # as "could not run" rather than as a failure of Talk.
    try:
        import anyio                                    # noqa: F401
        import mcp.types                                # noqa: F401
        from mcp.shared.message import SessionMessage   # noqa: F401
        import heron_mcp_server as server_module
    except BaseException as exc:                        # noqa: BLE001 - see above
        print("COULD NOT RUN - the MCP SDK is not usable here: %s: %s" % (type(exc).__name__, exc))
        print("  pip install --user mcp")
        print("  This is exit 3, which is NOT a pass: nothing was checked.")
        return COULD_NOT_RUN

    work = tempfile.mkdtemp(prefix="heron-talk-served-")
    try:
        return run(server_module, work)
    finally:
        shutil.rmtree(work, ignore_errors=True)


def run(server_module, work):
    import anyio
    import heron_bridge_client as bridge
    import heron_talk as talk

    low = talk.lowlevel(server_module.server)
    print("0. The SDK's server can carry a channel at all")
    check(low is not None, "the low-level server is found behind heron_mcp_server's")
    if low is None:
        return finish()

    print()
    print("1. Served the ordinary way, nothing about Talk changes")
    ordinary = low.create_initialization_options().capabilities
    experimental = getattr(ordinary, "experimental", None) or {}
    check(talk.CHANNEL not in experimental,
          "a chat not started for Talk does not declare a channel")

    root = os.path.join(work, "talk")
    bridge.TALK_DIR = root              # where heron_selection will read
    write(os.path.join(root, "4100", "selection-1.json"), {
        "format": talk.FORMAT, "selection": 1, "takenUtc": "2026-09-27T13:05:11Z",
        "revitPid": 4100, "revitVersion": "2024", "document": "Project1",
        "documentPath": "", "view": "Level 1 - Mech", "total": 2, "saved": 2,
        "unreadable": 0, "truncated": False,
        "categories": [{"name": "Ducts", "count": 2}],
        "elements": [{"id": "1491217", "uniqueId": "u-1", "category": "Ducts", "name": "Duct"},
                     {"id": "1491220", "uniqueId": "u-2", "category": "Ducts", "name": "Duct"}]})

    arrived = []

    def on_message(message):
        arrived.append(message.get("message"))
        return "NOTE FROM THE BINDING"

    seen = {}

    async def scenario():
        from mcp.shared.memory import create_client_server_memory_streams

        async with create_client_server_memory_streams() as (client, server_side):
            client_read, client_write = client
            server_read, server_write = server_side

            async def send(payload):
                await client_write.send(talk.session_message(to_jsonrpc(payload)))

            async def receive():
                with anyio.fail_after(10):
                    item = await client_read.receive()
                if isinstance(item, Exception):
                    raise item
                return as_dict(item.message)

            async with anyio.create_task_group() as group:
                group.start_soon(lambda: talk.serve_streams(
                    low, server_read, server_write, on_message=on_message,
                    folder_root=root, listener_pid=9999,
                    live_pids=lambda: set([4100]), poll=0.05))

                await send({"jsonrpc": "2.0", "id": 1, "method": "initialize",
                            "params": {"protocolVersion": handshake_version(),
                                       "capabilities": {},
                                       "clientInfo": {"name": "heron-test", "version": "0"}}})
                reply = await receive()
                seen["capabilities"] = (reply.get("result") or {}).get("capabilities") or {}
                seen["instructions"] = (reply.get("result") or {}).get("instructions") or ""

                # A MESSAGE IS WAITING BEFORE THE HANDSHAKE ENDS, on purpose:
                # nothing may be pushed until the host says it is ready.
                write(os.path.join(root, "4100", "message-1.json"), {
                    "format": talk.FORMAT, "message": 1, "text": "resize these to 400x300",
                    "sentUtc": "2026-09-27T13:05:11Z", "revitPid": 4100,
                    "revitVersion": "2024", "document": "Project1", "documentPath": "",
                    "view": "Level 1 - Mech", "selection": 1, "selected": 2,
                    "categories": [{"name": "Ducts", "count": 2}]})
                await anyio.sleep(0.4)
                seen["early"] = os.path.exists(os.path.join(root, "4100", "message-1.json"))

                await send({"jsonrpc": "2.0", "method": "notifications/initialized"})
                event = await receive()
                seen["event"] = event
                seen["claimed"] = os.path.exists(os.path.join(root, "4100", "message-1.taken.json"))
                seen["listening"] = os.path.exists(os.path.join(root, "listener-9999.json"))

                await send({"jsonrpc": "2.0", "id": 2, "method": "tools/call",
                            "params": {"name": "heron_selection", "arguments": {"number": 1}}})
                result = await receive()
                texts = [c.get("text", "") for c in (result.get("result") or {}).get("content") or []]
                seen["selection"] = "\n".join(texts)

                await client_write.aclose()      # the host goes away

    try:
        anyio.run(scenario)
    except Exception as exc:                     # noqa: BLE001 - reported as the failure it is
        check(False, "the served conversation completed: %s: %s" % (type(exc).__name__, exc))
        return finish()

    print()
    print("2. With Talk on, the handshake says so")
    check(talk.CHANNEL in (seen["capabilities"].get("experimental") or {}),
          "the handshake declares claude/channel: %s" % seen["capabilities"].get("experimental"))
    check(talk.instructions() in seen["instructions"],
          "a Talk chat's instructions carry the Talk paragraph")
    check("HERON TALK IS ON" in server_module._host_instructions(),
          "and the server builds them that way when HERON_TALK is on")

    print()
    print("3. Nothing is pushed before the host is ready")
    check(seen["early"], "a message waiting before the handshake ended was still waiting")

    print()
    print("4. A message arrives as the channel event")
    event = seen["event"]
    params = event.get("params") or {}
    check(event.get("method") == talk.CHANNEL_EVENT,
          "the event is %s: %s" % (talk.CHANNEL_EVENT, event.get("method")))
    check((params.get("content") or "").startswith("resize these to 400x300"),
          "the modeller's words come first")
    check("NOTE FROM THE BINDING" in (params.get("content") or "") and arrived == [1],
          "the binding was asked about it, and what it said travels with it")
    check(params.get("meta") == {"message": "1", "revit": "4100", "selection": "1", "selected": "2"},
          "the routing is in meta: %s" % params.get("meta"))
    check(seen["claimed"], "and the file was claimed")

    print()
    print("5. The chat says it is listening, and stops saying so")
    check(seen["listening"], "the heartbeat was there while it served")
    check(not os.path.exists(os.path.join(root, "listener-9999.json")),
          "and was removed when the host went away")

    print()
    print("6. heron_selection through the SDK")
    check("Selection 1, saved 2026-09-27T13:05:11Z from Revit 2024" in seen["selection"]
          and "1491220 | u-2 | Ducts | Duct" in seen["selection"],
          "it answers from the saved file")

    print()
    print("7. Speaking from a Revit chooses it")
    check_binding(server_module)

    return finish()


def check_binding(server_module):
    class Binding(object):
        def __init__(self, pid, chosen):
            self.pid, self.was_chosen, self.asked = pid, chosen, []

        def choose(self, number_or_pid):
            self.asked.append(number_or_pid)
            self.pid, self.was_chosen = int(number_or_pid), True

    real = server_module.binding
    try:
        server_module.binding = Binding(None, False)
        note = server_module._talk_arrived({"revitPid": 4100})
        check(note is None and server_module.binding.asked == ["4100"],
              "an unbound chat binds to the Revit the message came from")

        server_module.binding = Binding(3300, False)
        note = server_module._talk_arrived({"revitPid": 4100})
        check(note is None and server_module.binding.pid == 4100,
              "an ASSUMED binding gives way to the Revit the modeller spoke from")

        server_module.binding = Binding(3300, True)
        note = server_module._talk_arrived({"revitPid": 4100})
        check(note is not None and "Ask which one is meant" in note
              and server_module.binding.pid == 3300 and not server_module.binding.asked,
              "a CHOSEN binding is not overturned - the chat is told, and asks")

        server_module.binding = Binding(4100, True)
        check(server_module._talk_arrived({"revitPid": 4100}) is None,
              "a message from the Revit already in use changes nothing")
    finally:
        server_module.binding = real


def handshake_version():
    """The newest version the LEGACY handshake speaks - the one Claude Code uses."""
    try:
        from mcp.types.version import LATEST_HANDSHAKE_VERSION
        return LATEST_HANDSHAKE_VERSION
    except ImportError:
        return "2025-06-18"


def to_jsonrpc(payload):
    import mcp.types as types
    if "id" in payload:
        return types.JSONRPCRequest(**payload)
    return types.JSONRPCNotification(**payload)


def as_dict(message):
    """One JSON-RPC message as a plain dict, whichever SDK shaped it."""
    return json.loads(message.model_dump_json(by_alias=True, exclude_none=True))


def finish():
    print()
    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("PASSED - a Talk chat declares the channel and delivers what Revit sent.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
