#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-SRV-001, HERON-MCP-HLT-005
# Heron-Step:   4
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Heron's MCP server - Step 3.

The moment the whole chain exists: a sentence in the AI host, through MCP, down
the named pipe, onto Revit's own thread, and back. Everything after this step
adds capability to a chain that already works, which is a far easier problem
than making the chain exist.

    python mcp/server/heron_mcp_server.py        # speaks MCP over stdio

TWO TOOLS. revit_health answers "is Revit working?". revit_select_by_category
is the Phase 0 goal - the first thing a modeller would actually ask for, and
the first time Heron does something they can SEE on screen. Writing is Step 6
and arrives with its safety rails, never before them.

The answer is written for a person, not for a machine to parse. The host reads
it aloud, so it says what is true and what to do next - never a status code.

NAMING NOTE: this repository has a folder called `mcp/` and the MCP SDK is a
package called `mcp`. They do not collide: a real installed package always wins
over a same-named directory, verified rather than assumed. Do not "fix" this by
renaming the folder.
"""

import os
import sys

# The bridge client is the layer below this one. It stays dependency-free on
# purpose, so `doctor` keeps working on a machine where nothing else does.
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "client"))

import heron_bridge_client as bridge          # noqa: E402
from mcp.server.fastmcp import FastMCP        # noqa: E402

server = FastMCP("heron")


def _describe(b, reply):
    """One session, as a line a person can read."""
    where = "Revit %s, session %s" % (b.revit_version, b.pid)

    if reply is None:
        return "  %s - connected, but not answering right now." % where

    if not reply.get("ok"):
        # revit_busy and still_running are ordinary states, not faults. Their
        # message already says what to do, so pass it through rather than
        # inventing wording for it here.
        return "  %s - %s" % (where, reply.get("message") or reply.get("error"))

    document = reply.get("document")
    count = reply.get("count")
    line = "  %s - open model: %s" % (where, document)
    if count is not None:
        line += ", %s elements" % "{:,}".format(count)
    if reply.get("unsaved"):
        line += " (unsaved changes)"
    return line


@server.tool()
def revit_health() -> str:
    """
    Check whether Revit is reachable and what model is open.

    Reports every connected Revit session: its version, whether the bridge is
    answering, and which model it has open. Use it when the user asks whether
    Revit is working or connected, before relying on any other Revit answer,
    or when a Revit request has failed and the reason is not obvious.
    """
    try:
        live, starting, stale, mismatched = bridge.discover()
    except Exception as exc:                      # never let the tool itself fail
        return ("Heron could not read its own session list: %s: %s\n"
                "Run  python mcp/client/heron_bridge_client.py doctor  for the full picture."
                % (type(exc).__name__, exc))

    lines = []

    if live:
        lines.append("Revit is connected." if len(live) == 1
                     else "%d Revit sessions are connected." % len(live))
        for b in live:
            lines.append(_describe(b, b.request("count_elements")))
            b.close()

    if starting:
        lines.append("")
        for b in starting:
            lines.append("  Revit %s (session %s) is running but its bridge is not answering yet - "
                         "it is probably still starting up." % (b.revit_version, b.pid))

    if mismatched:
        lines.append("")
        for b in mismatched:
            lines.append("  Revit %s (session %s) speaks protocol %s and this client speaks %s. "
                         "Restart that Revit to finish updating; Heron will not talk across "
                         "protocols." % (b.revit_version, b.pid, b.protocol_version,
                                         bridge.PROTOCOL_VERSION))

    if not live and not starting and not mismatched:
        lines.append("No Revit is connected.")
        lines.append("")
        lines.append("Open Revit, then press  Heron AI > Heron  on the ribbon. The button lights up "
                     "when it is connected, and a Revit that was never connected is invisible to "
                     "Heron by design - nothing reaches a model the user did not offer up.")

    if stale:
        lines.append("")
        lines.append("  (cleared %d stale entry(ies) from a Revit that did not shut down cleanly)"
                     % len(stale))

    return "\n".join(lines)


@server.tool()
def revit_select_by_category(category: str = "ducts") -> str:
    """
    Select every element of one category in the open Revit model, so the user
    can see them highlighted on screen.

    Use when the user asks to select, highlight or find elements of a kind -
    "select all ducts". Selecting changes only what is highlighted, never the
    model itself, so it is safe and needs no confirmation.

    Heron currently understands ducts. Other categories arrive as each one is
    tried against a real model.
    """
    try:
        live, starting, stale, mismatched = bridge.discover()
    except Exception as exc:
        return "Heron could not read its own session list: %s: %s" % (type(exc).__name__, exc)

    if not live:
        if starting:
            return ("Revit is running but its bridge is not answering yet - it is probably "
                    "still starting up. Try again in a moment.")
        return "No Revit is connected. Open Revit, then press  Heron AI > Heron  on the ribbon."

    if len(live) > 1:
        # Step 5 builds the picker and the session binding. Until it exists,
        # acting on a guess is precisely the wrong-model failure the field
        # notes are about - so refuse, and say nothing was sent.
        where = "\n".join("  Revit %s (session %s)" % (b.revit_version, b.pid) for b in live)
        for b in live:
            b.close()
        return ("%d Revit sessions are connected, so it is not safe to guess which one you "
                "mean:\n%s\n\nNothing has been sent to Revit. Close the one you are not "
                "using, or ask again once Heron can be bound to a session."
                % (len(live), where))

    session = live[0]
    reply = session.request("select_by_category", op_args={"category": category})
    session.close()

    if reply is None:
        return "Revit %s (session %s) did not answer." % (session.revit_version, session.pid)

    if not reply.get("ok"):
        # revit_busy, unknown_category and no_document already say what to do.
        return reply.get("message") or reply.get("error") or "The request was refused."

    selected = reply.get("selected", 0)
    if selected == 0:
        return ("No %s in %s. Nothing was selected."
                % (reply.get("category"), reply.get("document")))

    # Never a bare number: which model it came from is half the answer.
    return ("Selected %s %s in %s.\n(%s)"
            % ("{:,}".format(selected), reply.get("category"),
               reply.get("document"), reply.get("scope")))


if __name__ == "__main__":
    if os.name != "nt":
        # The bridge is a Windows named pipe, and Revit is Windows-only.
        sys.stderr.write("Heron's bridge uses Windows named pipes. Revit is Windows-only.\n")
        sys.exit(2)
    server.run()
