# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Run ONE saved command on ONE named Revit - what Revit's Heron panel starts.

    python mcp/server/heron_command_run.py --name "Room Sections" \\
        --revit-pid 12345 --set level="Level 2"

WHY A PROCESS AND NOT C# (docs/49 s3)
-------------------------------------
Every check a change goes through lives here, in Python: the capability is
read from the fragment's own files, a DRAFT or stale proof is refused (D-117,
D-30), PUBLISH and ADMIN go as their own operation, undeclared values are
named, the model is pinned. Copying that into C# for the panel would be a
second copy of the rules, and two copies drift. So the panel starts this, and
this calls revit_change's or revit_read's OWN BODY through `_through` - the
way the Companion's Apply does - and the add-in's gate, the Changes switch
and Revit's Keep or Discard question apply exactly as they do to a chat.

WHICH REVIT: the one whose panel was clicked, by its process id, chosen
explicitly - never a guess between two (Article 12a). Gone, and nothing is
sent.

WHO IT IS TO THE LEASE: the same person as the chat. `.mcp.json` sets
HERON_CLIENT_ID for every chat (D-22), and this takes the same value from the
same file, so a chat that used this Revit a minute ago does not lock its own
modeller out. Its CONVERSATION is its own, and the models it held are handed
back when it ends (D-141).

ANSWER ON STDOUT, ONE BLOCK OF PLAIN TEXT, for the panel to show. Exit 0 when
Heron answered (a refusal is an answer, said in the text), 2 when the command
line itself was wrong, 3 when Heron cannot run here at all.
"""

from __future__ import print_function

import argparse
import io
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))


def _client_id(root):
    """HERON_CLIENT_ID from the Heron folder's .mcp.json, or None."""
    try:
        with io.open(os.path.join(root, ".mcp.json"), "r", encoding="utf-8") as fh:
            config = json.load(fh)
        value = (config.get("mcpServers", {}).get("heron", {})
                 .get("env", {}).get("HERON_CLIENT_ID"))
        return value if isinstance(value, str) and value.strip() else None
    except (OSError, ValueError, AttributeError):
        return None


def parse(argv):
    """(name, pid, inputs dict) or raise SystemExit(2) with a usage line."""
    parser = argparse.ArgumentParser(description="Run one saved Heron command.")
    parser.add_argument("--name", required=True, help="the saved command's name")
    parser.add_argument("--revit-pid", required=True, help="the Revit to run it on")
    parser.add_argument("--set", action="append", default=[], metavar="INPUT=VALUE",
                        help="one input; repeat for each")
    args = parser.parse_args(argv)
    given = {}
    for pair in args.set:
        key, sep, value = pair.partition("=")
        if not sep or not key.strip():
            parser.error("--set takes INPUT=VALUE, not '%s'" % pair)
        given[key.strip()] = value
    if not str(args.revit_pid).strip().isdigit():
        parser.error("--revit-pid must be a process id")
    return args.name, str(args.revit_pid).strip(), given


def run(name, pid, given, server=None):
    """The answer text for one saved command on one Revit. `server` is the
    MCP server module - passed in by the tests, imported otherwise."""
    if server is None:
        # BEFORE THE IMPORT: the bridge client reads it once, when it loads.
        person = _client_id(ROOT)
        if person and not os.environ.get("HERON_CLIENT_ID"):
            os.environ["HERON_CLIENT_ID"] = person
        sys.path.insert(0, HERE)
        import heron_mcp_server as server
    store = server.brain.commands()
    try:
        command = store.find(name)
        values = store.fill(command, given)
    except store.CommandError as why:
        return "%s Nothing was sent to Revit." % why

    try:
        server.binding.choose(pid)
    except server.NotBound as unbound:
        return ("The Revit this panel belongs to is not connected to Heron right now. "
                "Press the Heron button to connect it, then try again. Nothing was sent.\n%s"
                % unbound)

    tool = server.revit_change if command["tool"] == "change" else server.revit_read
    request = "Saved command '%s' from the Heron panel" % command["name"]
    with server._revit_lock:
        try:
            return server._through(tool, origin="panel")(
                command["capability"], values, request=request)
        finally:
            try:
                server.bridge.hand_back_models()
            except Exception:                        # noqa: BLE001 - best effort, as the client's own
                pass


def main(argv):
    name, pid, given = parse(argv)
    try:
        answer = run(name, pid, given)
    except ImportError as why:
        print("Heron cannot run saved commands on this PC: %s" % why)
        return 3
    print(answer)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
