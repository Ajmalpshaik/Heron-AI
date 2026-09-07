#!/usr/bin/env python3
# Heron-Agent:  HERON-MCP-REG-003
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
MCP Tool Registry. Which tools this server offers, and what each one can do.

docs/12 section 71: *risk level is declared in the tool registry, not decided
per call.* This is the MCP half of that. The add-in holds the other half
(`HeronOperationRegistry`) and the two must agree - a test asserts it by
reading the C# directly, because a rule kept in two languages by good
intentions is a rule that will eventually be kept in only one.

WHICH HALF IS THE BOUNDARY. Not this one. This file runs OUTSIDE Revit, in a
process the add-in does not trust and cannot verify. Everything here is for
the user's benefit - refusing early, and being able to answer "which of these
can change my model?" by reading a table instead of reading the code. The
enforcement that matters happens in the add-in, on the far side of the pipe,
where it cannot be bypassed by anything on this side being wrong or replaced.

Stating that plainly matters, because a registry that looks like a gate
invites somebody to treat it as one.
"""

# The seven levels of docs/12 section 1, in order of what they cost if wrong.
# The boundary that matters sits between EXECUTE and MODIFY.
READ = 0
ANALYZE = 1
SUGGEST = 2
EXECUTE = 3
MODIFY = 4
PUBLISH = 5
ADMIN = 6

NAMES = {
    READ: "READ", ANALYZE: "ANALYZE", SUGGEST: "SUGGEST", EXECUTE: "EXECUTE",
    MODIFY: "MODIFY", PUBLISH: "PUBLISH", ADMIN: "ADMIN",
}

# tool name -> (declared risk, the bridge operation it calls or None)
#
# The operation matters as much as the risk: it is what lets the test compare
# this table against the add-in's. A tool's risk must equal the risk of the
# operation it invokes - if it does not, one of the two files is wrong about
# what the tool does, and which one is wrong is not knowable from either side
# alone.
TOOLS = {
    "revit_health":             (READ,    None),
    "heron_version":            (READ,    None),
    "revit_use_session":        (READ,    "count_elements"),
    "revit_use_this_model":     (READ,    "count_elements"),
    "revit_select_by_category": (EXECUTE, "select_by_category"),
    "revit_preview_move":       (ANALYZE, "preview_move"),
    "revit_apply_move":         (MODIFY,  "move_elements"),

    # The brain, reachable. These three read what Heron KNOWS - the skills, the
    # capability registry and the fragment library - and none of them sends
    # anything to Revit, which is why every operation here is None.
    #
    # READ rather than ANALYZE, and the line is worth stating because it is not
    # obvious: retrieval ranks and fuses, which feels like analysis, but nothing
    # here looks at the user's MODEL. It reads files that shipped with Heron.
    # Risk is about what a tool can reach, not about how clever it is.
    "heron_capabilities":       (READ,    None),
    "heron_resolve":            (READ,    None),
    "heron_lookup":             (READ,    None),

    # The Capability Gap report (HERON-AHR-GAP-001, docs/06 s6). READ, and the
    # operation is None for the same reason as the three above - it sends
    # nothing to Revit. What it reads is Heron's OWN audit trail, which is a
    # file under the user's data.
    #
    # Worth stating because the trail is not shipped content like the others:
    # it carries document names, and docs/12 s5 says it "contains project
    # information and must obey the same egress rules as everything else". It
    # stays READ rather than rising a level because naming the model an answer
    # came from is already this repository's rule, not a new exposure - but a
    # future field carrying element ids or parameter values would change that
    # judgement, and whoever adds one should revisit this line.
    "heron_gaps":               (READ,    None),

    # The Compatibility Matrix (HERON-FRG-MTX-009). READ, no operation:
    # it reads the fragment files, the build's runtime table and the last
    # compile record. Nothing here reaches a model or a Revit session.
    "heron_compatibility":      (READ,    None),
}


class NotDeclared(Exception):
    """A tool nobody declared. Never treated as harmless."""


def risk_of(tool):
    """
    The declared risk of a tool.

    Raises rather than defaulting. There is no sensible default risk, and a
    caller that has not declared its tool has a bug that should be loud rather
    than quietly permissive.
    """
    if tool not in TOOLS:
        raise NotDeclared(
            "'%s' is not declared in the MCP tool registry. Add it to TOOLS "
            "with its risk level - being absent is a refusal, not a risk of zero."
            % tool)
    return TOOLS[tool][0]


def operation_of(tool):
    """The bridge operation a tool calls, or None if it calls none."""
    if tool not in TOOLS:
        raise NotDeclared("'%s' is not declared in the MCP tool registry." % tool)
    return TOOLS[tool][1]


def writes(tool):
    """
    Can this tool change the model?

    Used at the call site to decide how a failure is classified, so that
    `writes` is never a hand-typed True sitting next to a declaration that
    says otherwise.
    """
    return risk_of(tool) >= MODIFY


def describe():
    """
    Every tool and what it can do, worst first.

    This is the answer to "which of Heron's tools can change my model?" - the
    question that could not be answered from a table before this file existed.
    """
    rows = sorted(TOOLS.items(), key=lambda item: (-item[1][0], item[0]))
    width = max(len(name) for name in TOOLS)
    lines = []
    for name, (risk, op) in rows:
        mark = "  <- CHANGES THE MODEL" if risk >= MODIFY else ""
        lines.append("  %-*s  %-8s%s" % (width, name, NAMES[risk], mark))
    return "\n".join(lines)
