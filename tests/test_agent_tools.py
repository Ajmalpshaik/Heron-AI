# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What a background agent may call - read from the agent files, checked
against the server that serves the tools.

    python tests/test_agent_tools.py

A BACKGROUND AGENT WORKS WHERE NOBODY IS WATCHING
--------------------------------------------------
`.claude/agents/heron-model-auditor.md` runs in the background: the modeller
asks for an audit and carries on working while it reads the model. Nobody
sees its tool calls as they happen, so nobody can stop one. Its safety is
therefore its TOOL LIST - Claude Code gives a subagent only the tools that
list names, and a tool it has not been given is not a tool it can be talked
into calling.

That makes the list the thing to hold, and it can fail three ways, each
silently:

  - it can go MISSING, and a subagent with no list inherits every tool the
    chat has, including the ones that change the model;
  - it can GROW by one line in review, and the new line can be a write;
  - it can go STALE - a tool renamed on the server is dropped from the list
    without a word, and the agent reports a gap nobody caused.

WHAT IT PROVES
  1. EVERY AGENT MARKED `background: true` CARRIES A TOOL LIST.
  2. NOTHING ON THAT LIST CHANGES THE MODEL OR THE CHAT. The tools that do
     are named below with what each one changes.
  3. EVERY HERON TOOL ANY AGENT NAMES EXISTS ON THE SERVER, found by reading
     `mcp/server/heron_mcp_server.py` rather than by listing them here.
  4. THE NAMES IN (2) EXIST TOO, so the list of what changes things cannot
     rot into names the server no longer has.
  5. THE COMPARISON IS NOT EMPTY. At least one background agent is found.

WHAT IT DOES NOT PROVE
  That an agent's report is right. That takes a run against a named model.
"""

from __future__ import annotations

import ast
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AGENTS = os.path.join(ROOT, ".claude", "agents")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
PREFIX = "mcp__heron__"

# Every tool on the server that changes something, and what it changes. A
# background agent may name none of them.
CHANGES = {
    "revit_change":             "changes the model and keeps the change",
    "revit_apply_move":         "moves elements in the model",
    "revit_preview_move":       "leaves a move waiting for revit_apply_move",
    "revit_select_by_category": "changes the selection on the modeller's screen",
    "revit_use_session":        "moves the whole chat onto another Revit",
    "revit_use_this_model":     "moves the whole chat onto another model",
}

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def server_tools():
    """The functions the server registers with `@server.tool()`.

    Found by the decorator's shape rather than by `ast.unparse`, which is
    Python 3.9, so an older interpreter reads the same answer.
    """
    names = set()
    for node in ast.parse(read(SERVER)).body:
        if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):
            continue
        for decorator in node.decorator_list:
            call = decorator.func if isinstance(decorator, ast.Call) else decorator
            if isinstance(call, ast.Attribute) and call.attr == "tool":
                names.add(node.name)
    return names


def frontmatter(text):
    """(name, tools or None, background) out of an agent file's header."""
    head = text.split("---", 2)[1] if text.startswith("---") else ""
    name = re.search(r"^name:\s*(.+)$", head, re.M)
    tools = re.search(r"^tools:\s*(.+)$", head, re.M)
    background = re.search(r"^background:\s*(.+)$", head, re.M)
    return (name.group(1).strip() if name else None,
            [t.strip() for t in tools.group(1).split(",") if t.strip()]
            if tools else None,
            bool(background) and background.group(1).strip().lower() == "true")


def main():
    served = server_tools()

    print("\nThe server, and the names this suite calls changing")
    check(len(served) >= len(CHANGES),
          "found %d tool(s) on the server - enough to be checking against"
          % len(served))
    for tool in sorted(CHANGES):
        check(tool in served, "%s is still a tool on the server" % tool)

    agents = sorted(n for n in os.listdir(AGENTS) if n.endswith(".md"))
    background = []

    print("\nEvery Heron tool an agent names is one the server has")
    for filename in agents:
        name, tools, runs_behind = frontmatter(read(os.path.join(AGENTS, filename)))
        if runs_behind:
            background.append((filename, tools))
        for tool in tools or ():
            if tool.startswith(PREFIX):
                bare = tool[len(PREFIX):]
                check(bare in served, "%s: %s is on the server" % (filename, bare))

    print("\nWhat a background agent may call")
    check(len(background) >= 1,
          "found %d background agent(s) - enough to be checking something"
          % len(background))
    for filename, tools in background:
        check(tools is not None,
              "%s: carries a tool list - without one it inherits every tool "
              "the chat has" % filename)
        named = {t[len(PREFIX):] for t in tools or () if t.startswith(PREFIX)}
        for tool in sorted(named & set(CHANGES)):
            check(False, "%s: names %s, which %s"
                  % (filename, tool, CHANGES[tool]))
        if not named & set(CHANGES):
            check(True, "%s: names nothing that changes the model or the chat"
                  % filename)

    print()
    if FAILURES:
        print("FAILED")
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - every background agent reads, and every tool it names is "
          "one the server has.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
