#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The tool registry, in both languages, agreeing. Runs without Revit.

docs/12 section 71: *risk level is declared in the tool registry, not decided
per call.* Heron keeps that declaration twice - once in Python for the MCP
tools, once in C# for the bridge operations - because the two halves run in
different processes and only one of them can be trusted.

A rule kept in two languages by good intentions is a rule that will eventually
be kept in only one. So this test READS THE C# and compares it, rather than
asking anyone to remember. It is the same technique check-metadata.py already
uses for the version number, and for the same reason: the two sides cannot
import each other, so something has to look at both.

WHAT IT CANNOT DO: it reads the C# as text, not as a compiled program. It can
prove the declarations agree and that every declared operation has a handler
somewhere. It cannot prove the handler does what its risk level claims - that
takes Revit, and it is NEEDS-CHECKING group C.

    python tests/test_tool_registry.py
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_tools as tools                                  # noqa: E402

REGISTRY_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
HANDLER_FILES = [
    os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitOperations.cs"),
    os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitWrite.cs"),
    os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs"),
]

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def csharp_registry():
    """The add-in's declaration, read out of the source."""
    text = io.open(REGISTRY_CS, encoding="utf-8").read()
    found = re.findall(r'\{\s*"([A-Za-z_][A-Za-z0-9_]*)"\s*,\s*HeronRisk\.([A-Za-z]+)\s*\}', text)
    return dict((op, level.upper()) for op, level in found)


def csharp_handlers():
    """Every `case "op":` across the files that route operations."""
    handled = set()
    for path in HANDLER_FILES:
        text = io.open(path, encoding="utf-8").read()
        handled.update(re.findall(r'case\s+"([A-Za-z_][A-Za-z0-9_]*)"\s*:', text))
    return handled


def main():
    declared_cs = csharp_registry()
    handled = csharp_handlers()

    print("Both registries were found and are not empty")
    check(len(declared_cs) > 0, "the C# registry parsed - %d operations" % len(declared_cs))
    check(len(tools.TOOLS) > 0, "the MCP registry has %d tools" % len(tools.TOOLS))

    print()
    print("THE PROPERTY: a tool's risk equals the risk of the operation it calls")
    for tool in sorted(tools.TOOLS):
        op = tools.operation_of(tool)
        if op is None:
            continue
        mine = tools.NAMES[tools.risk_of(tool)]
        theirs = declared_cs.get(op)
        check(theirs is not None,
              "%s calls '%s', which the add-in declares" % (tool, op))
        if theirs is not None:
            check(mine == theirs,
                  "%s is %s and '%s' is %s" % (tool, mine, op, theirs)
                  + ("" if mine == theirs else "  <- THEY DISAGREE"))

    print()
    print("Every declared operation has a handler")
    for op in sorted(declared_cs):
        check(op in handled,
              "'%s' is declared and has a `case \"%s\":`" % (op, op))

    print()
    print("Every handler is declared - nothing routes that the gate has not seen")
    # A handler with no declaration would be refused by the gate, so this is
    # not a security hole. It is a bug: an operation that exists and can never
    # be reached, which is the kind of thing that gets 'fixed' by weakening
    # the gate.
    for op in sorted(handled):
        check(op in declared_cs,
              "`case \"%s\":` has a declaration in the registry" % op)

    print()
    print("Exactly one thing here can change the model")
    writers = sorted(t for t in tools.TOOLS if tools.writes(t))
    check(writers == ["revit_apply_move"],
          "the only writing tool is revit_apply_move - found: %s" % (writers or "none"))

    cs_writers = sorted(op for op, level in declared_cs.items() if level == "MODIFY")
    check(cs_writers == ["move_elements"],
          "the only MODIFY operation is move_elements - found: %s" % (cs_writers or "none"))

    print()
    print("The preview really is declared as changing nothing")
    check(declared_cs.get("preview_move") == "ANALYZE",
          "preview_move is ANALYZE, not MODIFY - it describes a change, it is not one")
    check(not tools.writes("revit_preview_move"),
          "and so the preview tool does not count as a write")

    print()
    print("Selecting is EXECUTE, not READ and not MODIFY")
    check(declared_cs.get("select_by_category") == "EXECUTE",
          "selecting alters what is highlighted, never what exists")

    print()
    print("Undeclared fails closed, in both directions")
    try:
        tools.risk_of("revit_delete_everything")
        check(False, "an undeclared tool raises rather than defaulting to READ")
    except tools.NotDeclared:
        check(True, "an undeclared tool raises rather than defaulting to READ")

    check("Writes(" in io.open(REGISTRY_CS, encoding="utf-8").read(),
          "the C# side exposes Writes() so callers need not compare levels by hand")

    print()
    print("No risk level is hardcoded in the write path any more")
    write_cs = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "RevitWrite.cs"),
                       encoding="utf-8").read()
    check("HeronRisk.Modify" not in write_cs,
          "RevitWrite names no literal risk level - it reads the registry")

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - one declaration, two languages, and they agree.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
