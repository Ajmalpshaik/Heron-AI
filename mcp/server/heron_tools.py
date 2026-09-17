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

    # Linked models (HERON-REVIT-LNK-015). READ: it lists what the model
    # already links to and reads only the linked documents Revit already has
    # open. Nothing is loaded, unloaded or reloaded - the register's row is
    # "never modifies a link's source".
    "revit_links":              (READ,    "list_links"),
    "revit_phases":             (READ,    "list_phases"),
    "revit_systems":            (READ,    "list_systems"),

    # Parameters (HERON-REVIT-PAR-011). READ, although that agent's register
    # row is MODIFY: the column is the highest level the ROW can require, and
    # this operation reads. A parameter WRITE will be its own entry at its own
    # risk, because the risk is looked up by name and one name must mean one
    # thing.
    "revit_parameters":         (READ,    "read_parameters"),

    # Groups and assemblies (HERON-REVIT-GRP-033). READ, same reasoning as
    # the two above. It is the PRE-FLIGHT for the write tools: an element in
    # a group carries an edit into every placement of that group's type, and
    # Revit raises nothing when a move of one shifts nothing.
    "revit_groups":             (READ,    "list_groups"),

    # Levels and grids (HERON-REVIT-LVL-027). READ, same reasoning as the two
    # above. Renaming a level or moving its elevation drags every element
    # hosted on it, so a write will be its own entry at its own risk.
    "revit_levels":             (READ,    "list_levels"),

    # Worksets and ownership (HERON-REVIT-WRK-014). READ: it lists what the
    # worksets are and samples who owns what. Nothing is created, opened,
    # closed, borrowed or relinquished.
    "revit_worksets":           (READ,    "list_worksets"),

    # Views and sheets (HERON-REVIT-VIE-013, HERON-REVIT-SHT-029). READ.
    # SHT-029 OWNS sheets - both rows claimed them and the owner settled it on
    # 2026-09-17. Applying a view template changes what everybody sees in that
    # view; renumbering a sheet breaks every reference pointing at it across
    # the whole set. Neither is done here.
    "revit_views":              (READ,    "list_views"),
    "revit_sheets":             (READ,    "list_sheets"),

    # Rooms and spaces (HERON-REVIT-RM-028). READ. Placing a room, deleting an
    # unplaced one or moving a boundary all change somebody's area schedule,
    # which on most jobs is a contractual document.
    "revit_rooms":              (READ,    "list_rooms"),

    # Schedules (HERON-REVIT-SCH-026). READ - which schedules EXIST and what
    # governs them. Their ROWS belong to the export agent at PUBLISH.
    "revit_schedules":          (READ,    "list_schedules"),

    # Families and types (HERON-REVIT-FAM-012). READ, and deliberately NOT a
    # load: loading a family merges one document into another and its materials
    # overwrite the project's, silently, with no count moving.
    "revit_families":           (READ,    "list_families"),

    # Export readiness (HERON-REVIT-EXP-018). READ although that agent's row is
    # PUBLISH: the column is the highest level the ROW can require, and asking
    # whether an export WOULD be sound requires none of it. Nothing is written,
    # printed or sent. The export itself is a separate entry at PUBLISH.
    "revit_export_check":       (READ,    "check_export"),

    # What came in from outside (HERON-REVIT-IMP-019). READ. Importing is not
    # an operation here: an import is copied INTO the model and its layers and
    # text styles remain after the import itself is deleted.
    "revit_imports":            (READ,    "list_imports"),

    # Dimensions, tags, text (HERON-REVIT-DIM-031). READ. An overridden
    # dimension is the thing it exists to find.
    "revit_annotation":         (READ,    "list_annotation"),
    "revit_preview_move":       (ANALYZE, "preview_move"),
    "revit_apply_move":         (MODIFY,  "move_elements"),

    # THE SECOND WRITER, and the first that is not a preview being applied.
    #
    # revit_apply_move above can only finish something the user already saw:
    # a preview is taken, shown, and the token spent once. This one carries a
    # fragment straight to Revit and KEEPS what it did, which is a different
    # shape of risk and is why it is stated separately here rather than folded
    # in beside the move.
    #
    # What stands between it and the model is `write.enabled` - the owner's
    # ribbon switch - checked by HeronPermissions inside the add-in, not here.
    # The gate lives on the far side of the pipe on purpose: a client deciding
    # its own permission is not a permission.
    "revit_change":             (MODIFY,  "run_fragment_write"),

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

    # The Context Manager (docs/19 s1-s2). READ and no operation for the same
    # reason as the three above: it assembles what an agent would be GIVEN and
    # sends nothing to Revit. Worth stating because "context" sounds like it
    # reaches into the model - it does not. The situation part carries the
    # release and the project NAME, which are already in every answer Heron
    # gives, and nothing else about the document.
    "heron_context":            (READ,    None),

    # The grounding check (HERON-RAG-CIT-014, docs/05 s8). READ, and the
    # operation is None for the same reason: it sends nothing to Revit. It
    # takes a draft the HOST wrote and the clauses Heron already holds, and
    # reports where the two disagree.
    #
    # READ rather than anything higher even though a DRAFT crosses into it,
    # because the draft goes nowhere: it is compared in this process, against
    # this machine's own store, with no model and no network (R-47), and the
    # only thing that comes back is a report. And it can never change an
    # answer - R-53 forbids a rewrite, which is what keeps a checker from
    # quietly becoming an editor.
    "heron_check":              (READ,    None),

    # The multi-scope standards answer (HERON-RAG-LIB-001 and
    # HERON-RAG-CNF-015, docs/20). READ, and the operation is None - it sends
    # nothing to Revit and reads only this machine's own knowledge stores.
    #
    # IT OPENS MORE THAN ONE SCOPE AND THAT IS WORTH SAYING HERE, because it is
    # the only tool that does. It does not POOL them: each is asked on its own
    # and answers under its own label, which is the wall D-33 and Golden Rule 5
    # describe. What crosses between them is a number and a clause number.
    "heron_standards":          (READ,    None),

    # Stage 9's two (HERON-RAG-RSH-017, docs/28). READ, operation None, and
    # for these two that is a stronger claim than for the others: the Research
    # agent is the one part of this plan whose NAME suggests reaching outside,
    # and it does not. heron_research says what Heron's own knowledge missed
    # and what an outside answer must carry; heron_research_check reports on
    # the CITATIONS of what came back. Neither opens a socket - the host has
    # the model and the network (D-01), and tests/test_research.py asserts the
    # absence of a fetch rather than trusting this comment.
    "heron_research":           (READ,    None),
    "heron_research_check":     (READ,    None),

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

    # Self-Diagnostics (HERON-OPS-DIA-005). READ, and no operation of its
    # own - it composes what the other readers already report. It does
    # reach the bridge, through revit_health's own discovery, but it asks
    # nothing of a model and can change nothing.
    "heron_diagnose":           (READ,    None),
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


def main():
    """
    Print the registry. THE INVENTORY IS DERIVED, NEVER TYPED.

    Two places in this repository listed the MCP tools in prose - brain's
    README said "four" and this server's own module header listed seven - and
    adding `heron_check` and `heron_standards` left both of them wrong, on
    exactly the descriptions a reviewer reads to find out what Heron can
    reach and what it can change. A hand-typed list of callable surfaces is a
    security claim with a half-life. Found by a review 2026-09-11.

    So both now name this command instead, and this is the one place the
    answer lives:

        python mcp/server/heron_tools.py
    """
    print("Every MCP tool Heron declares, worst first. %d in all." % len(TOOLS))
    print()
    print(describe())
    print()
    print("Derived from heron_tools.TOOLS, which is what the server enforces.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
