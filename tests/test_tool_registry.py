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
    """
    Every operation routed anywhere, in either form the code uses.

    `case "op":` covers the switch in RevitOperations. `op == "op"` covers
    ping and info, which BridgeServer routes with an if rather than a switch
    because Step 6 made them lease-exempt and they had to be handled before the
    lease check. Matching only the switch form quietly reported both as
    unhandled - which is this check working, and then being wrong about why.
    """
    handled = set()
    for path in HANDLER_FILES:
        text = io.open(path, encoding="utf-8").read()
        handled.update(re.findall(r'case\s+"([A-Za-z_][A-Za-z0-9_]*)"\s*:', text))
        handled.update(re.findall(r'op\s*==\s*"([A-Za-z_][A-Za-z0-9_]*)"', text))
    return handled


def labels(declared_cs):
    """
    THE SAFETY LABELS A HOST IS GIVEN ARE THE REGISTRY'S, AND NO OTHER.

    MCP lets a server mark a tool read-only, destructive or idempotent, and a
    host may call a read-only tool without asking. So a label is a claim about
    what a tool can change - the same claim this file already holds the two
    languages to - and a write labelled read-only is the one wrong answer that
    matters. The labels are therefore DERIVED from heron_tools.TOOLS, never
    typed at a tool, and this holds both halves: what the derivation says, and
    that the server has nowhere else to type one.
    """
    print()
    print("The safety labels a host is given are the registry's risk")
    annotations = getattr(tools, "annotations", None)
    check(annotations is not None,
          "heron_tools derives the labels - annotations() exists")
    if annotations is None:
        return

    for tool in sorted(tools.TOOLS):
        risk = tools.risk_of(tool)
        label = annotations(tool)
        op = tools.operation_of(tool)
        if risk <= tools.ANALYZE:
            ok = label == {"readOnlyHint": True, "destructiveHint": False,
                           "idempotentHint": True}
            what = "read-only"
        elif tools.writes(tool):
            ok = label == {"readOnlyHint": False, "destructiveHint": True,
                           "idempotentHint": False}
            what = "destructive and not idempotent"
        else:
            ok = (label["readOnlyHint"] is False
                  and label["destructiveHint"] is False)
            what = "neither read-only nor destructive"
        # AND THE ADD-IN AGREES. A tool's risk already equals its operation's
        # (checked above); a label derived from one is therefore the label
        # the other implies, and this says so for the tool in hand.
        theirs = declared_cs.get(op) if op else None
        agrees = (theirs is None
                  or (label["readOnlyHint"] == (theirs in ("READ", "ANALYZE", "SUGGEST"))
                      and label["destructiveHint"] == (theirs in ("MODIFY", "PUBLISH", "ADMIN"))))
        check(ok and agrees,
              "%s is %s - labelled %s" % (tool, tools.NAMES[risk], what)
              + ("" if agrees else "  <- the add-in's %s says otherwise" % theirs))

    check(annotations("revit_delete_everything") ==
          {"readOnlyHint": False, "destructiveHint": True, "idempotentHint": False},
          "an undeclared tool gets the worst label on every count - which is "
          "also what a host assumes of a tool with none")

    server = io.open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                     encoding="utf-8").read()
    typed = re.findall(r"@server\.tool\((?!\))", server)
    check(not typed,
          "every tool is registered as a bare @server.tool() - no label, and no "
          "argument that could carry one, is typed at a tool")
    check("class _Labelled(_Server)" in server
          and "tools.annotations(" in server
          and "server = _Labelled(" in server,
          "and the server registers every tool through _Labelled, which reads "
          "the label off the registry as the tool registers")


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
              "'%s' is declared and is routed somewhere" % op)

    print()
    print("Every handler is declared - nothing routes that the gate has not seen")
    # A handler with no declaration would be refused by the gate, so this is
    # not a security hole. It is a bug: an operation that exists and can never
    # be reached, which is the kind of thing that gets 'fixed' by weakening
    # the gate.
    for op in sorted(handled):
        check(op in declared_cs,
              "the routed operation '%s' has a declaration in the registry" % op)

    print()
    print("What can change the model is a SHORT, NAMED list")
    # This was "exactly one thing" until 2026-09-08, and the number is not the
    # point - the list being written down here is. Every addition to it has to
    # be typed into this test by somebody who meant to, which is the whole
    # value: an operation that quietly becomes a write fails here rather than
    # in a model.
    writers = sorted(t for t in tools.TOOLS if tools.writes(t))
    # revit_change was added 2026-09-15 at the owner's explicit instruction,
    # and typing it here is the deliberate act this test exists to require. It
    # is the first writer with NO preview in front of it - since 2026-09-23 a
    # recorded exception to Article 9, D-99, which names what stands in for
    # the preview: the owner's ribbon switch (write.enabled, read by
    # HeronPermissions inside the add-in), the refusal of anything above
    # Modify, one undo entry and the pin. A THIRD writer still fails here.
    check(writers == ["revit_apply_move", "revit_change"],
          "the writing tools are exactly revit_apply_move and revit_change "
          "- found: %s" % (writers or "none"))

    cs_writers = sorted(op for op, level in declared_cs.items() if level == "MODIFY")
    check(cs_writers == ["move_elements", "run_fragment_write"],
          "the MODIFY operations are exactly move_elements and run_fragment_write "
          "- found: %s" % (cs_writers or "none"))

    # THE READ EXECUTOR MUST NEVER BECOME A WRITE ONE. They share every line
    # up to the transaction, so the thing that separates them is this pair of
    # declarations and nothing else.
    check(declared_cs.get("run_fragment_read") == "ANALYZE",
          "run_fragment_read is ANALYZE - it opens no transaction, so Revit itself refuses "
          "any change")
    check(declared_cs.get("run_fragment_write") == "MODIFY",
          "run_fragment_write is MODIFY - it opens one, so the permission gate and the amber "
          "banner both have to know")

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
    print("Everything answered OUTSIDE the gate is a named, READ-risk allow-list")
    # FRAGMENT-ISSUES section 5b, row 14. RevitOperations.Gate heads itself
    # with "Every operation, one place, BEFORE any routing. This is what makes
    # docs/12 section 71 true." BridgeServer answers ping, info and release
    # itself and returns, before the lease claim and before RequestHandler -
    # which is the only route to RevitOperations.Run, and therefore to Gate.
    #
    # ALL THREE ARE CORRECT AND THE REASON THEY SIT THERE IS GOOD: none
    # touches a model, all three are declared Read, and they must precede the
    # lease because "claiming in order to release would renew the very thing
    # being given up". Nothing is wrong. What was wrong is that there is a
    # second, unguarded place to add an operation, with a worked precedent of
    # three and a stated rationale a future author can honestly follow - and
    # Gate's own justification is "for the sake of the operation nobody has
    # written yet".
    #
    # So the allow-list is named here rather than in a comment: a fourth `if`
    # added beside them fails this, and a reviewer is asked whether it really
    # belongs outside the gate.
    OUTSIDE_THE_GATE = set(["ping", "info", "release"])
    bridge_cs = io.open(os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs"),
                        encoding="utf-8").read()
    answered_in_bridge = set(re.findall(r'op\s*==\s*"([A-Za-z_][A-Za-z0-9_]*)"', bridge_cs))
    unexpected = sorted(answered_in_bridge - OUTSIDE_THE_GATE)
    check(not unexpected,
          "BridgeServer answers only %s outside the gate%s"
          % (", ".join(sorted(OUTSIDE_THE_GATE)),
             "" if not unexpected else " - and also " + ", ".join(unexpected)))
    missing = sorted(OUTSIDE_THE_GATE - answered_in_bridge)
    check(not missing,
          "and the list is not stale - every name on it is still answered there%s"
          % ("" if not missing else " (%s is not)" % ", ".join(missing)))
    for op in sorted(OUTSIDE_THE_GATE):
        check(declared_cs.get(op) == "READ",
              "'%s' skips the gate and is declared READ, which the gate would "
              "have permitted anyway" % op)

    labels(declared_cs)

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
