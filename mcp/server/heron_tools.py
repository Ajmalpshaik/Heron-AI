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

WHAT EACH TOOL COSTS A CHAT is answered here too, by measurement:

    python mcp/server/heron_tools.py --sizes
"""

import ast
import io
import json
import os
import sys

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

    # A picture of one view, for the chat to look at its own work (D-123).
    # ANALYZE, because that is what `capture_view` is declared in the add-in:
    # given element ids it makes a temporary 3D view inside a TransactionGroup
    # the add-in ALWAYS rolls back, with no apply flag anywhere to keep it.
    # NOT an export, and deliberately not named export-*: the picture goes
    # into Heron's own Derived folder, comes back in the reply and is deleted
    # there, which is not the "shared location" Article 10 and D-107 mean.
    "revit_view_image":         (ANALYZE, "capture_view"),

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
    #
    # NO PREVIEW WAS A DECISION RATHER THAN A SILENCE - D-99, 2026-09-23 - and
    # D-126 supersedes it: Revit shows the change and asks the modeller Keep
    # or Discard before it is kept, inside the add-in, under the owner's
    # write.askBeforeKeeping. Nothing in this row decides whether it asks.
    #
    # ITS ROW IS ITS OWN LEVEL, AND ALSO_REACHES BELOW SAYS WHAT ELSE IT CAN
    # SEND (D-106): a PUBLISH or ADMIN fragment goes as its own operation.
    "revit_change":             (MODIFY,  "run_fragment_write"),

    # THE READ-ONLY DOOR (package C2, 2026-09-23). The same executor as the
    # row above, reached through `run_fragment_read`, which opens NO
    # transaction - so Revit itself refuses any change the fragment attempts,
    # and it runs with Changes OFF because the add-in's gate lets ANALYZE
    # through without the switch.
    #
    # ANALYZE, because that is what `run_fragment_read` is declared in
    # HeronOperationRegistry.cs, and a tool's risk must equal its operation's
    # (tests/test_tool_registry.py). It is also the door's own ceiling: the
    # tool refuses to send a fragment whose declared risk is above it, read
    # from this row rather than from a second list.
    #
    # Until this row, every READ and ANALYZE fragment - most of them PROVEN -
    # could be reached from a chat only through revit_change: with Changes
    # ON, through the executor that opens a transaction. A question needed
    # write permission.
    "revit_read":               (ANALYZE, "run_fragment_read"),

    # THE SHOW DOOR (2026-10-17). The read door's executor and the read door's
    # guarantee - `run_fragment_show` opens NO transaction, so Revit itself
    # refuses any change to the model - one level higher, so a fragment that
    # selects, zooms or opens a view runs with Changes OFF. The add-in's
    # read-only ceiling is EXECUTE ("changes what is shown, never what
    # exists"); until this row only the MCP side held EXECUTE back, because
    # revit_read's ceiling is its own ANALYZE and revit_change needs Changes.
    #
    # EXECUTE, because that is what `run_fragment_show` is declared in
    # HeronOperationRegistry.cs, and door_refusal reads the ceiling from this
    # row - so a MODIFY fragment is refused here before anything is sent.
    "revit_show":               (EXECUTE, "run_fragment_show"),

    # Revit's own Temporary Hide/Isolate in the active view, and its reset.
    # EXECUTE through FIXED operations in the add-in, never a fragment: a
    # temporary isolate needs a transaction, and opening one for fragment code
    # with Changes off would be a write path with no switch in front of it.
    # The view's saved visibility is untouched.
    "revit_isolate_temporary":  (EXECUTE, "isolate_temporary"),
    "revit_reset_temporary":    (EXECUTE, "reset_temporary"),

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
    # A job's whole method - a skill's ordered steps - so a chat is handed
    # how a family is built instead of reading the skill file off disk.
    "heron_method":             (READ,    None),

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

    # HVAC design (HERON-MEP-HVD-001, docs/41). READ, operation None: it is
    # arithmetic over the values the caller hands it - loads, airflow,
    # ASHRAE 62.1 outdoor air, duct and pipe sizes, diffusers, fan power - and
    # it reads no model. The one thing it keeps is a project's governing
    # standards once the modeller has said them (D-111): one small file per
    # project in Heron's OWN knowledge folder, the kind of bookkeeping the
    # project label index already does on a read - never the model, never a
    # file of the modeller's. Its answers NAME the fragments that would put a
    # result into Revit; sending one is revit_change's, at revit_change's own
    # level.
    "heron_hvac":               (READ,    None),

    # Fire protection design (HERON-MEP-FPD-002, docs/42). READ, operation
    # None, for every reason heron_hvac is: arithmetic over the values the
    # caller hands it - hazard classes, sprinkler spacing and counts, pipe
    # schedules, hydraulics, supply, pumps, standpipes - reading no model. Its
    # one kept thing is a project's governing standards (D-111), in that
    # project's own FIRE record in Heron's knowledge folder, never a file of
    # the modeller's. Its answers NAME the fragments that would put a result
    # into Revit; sending one is revit_change's.
    "heron_fire":               (READ,    None),

    # Answers kept under the id every template-born project shares
    # (FRAGMENT-ISSUES 5b-324, D-113). READ, operation None, for every reason
    # heron_hvac is: it reads no model and sends nothing to Revit. On the
    # modeller's word it COPIES that model name's earlier answers into the
    # open model's own files, in Heron's own knowledge folder - the same kind
    # of bookkeeping heron_hvac's kept standards are - and moves, rewrites or
    # deletes nothing that was there.
    "heron_earlier_answers":    (READ,    None),

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

    # The run records (FRAGMENT-ISSUES 5b-407). READ, no operation: it pages a
    # file the add-in wrote under HeronPaths.Runs - every element a change
    # touched, and the whole reply revit_change only summarised - and sends
    # nothing to Revit. Like heron_gaps it reads Heron's own trail, which names
    # documents and element ids; an id says which element, never what is in
    # it, and the reply it pages is one this chat was already sent in short.
    "heron_run_record":         (READ,    None),

    # The Compatibility Matrix (HERON-FRG-MTX-009). READ, no operation:
    # it reads the fragment files, the build's runtime table and the last
    # compile record. Nothing here reaches a model or a Revit session.
    "heron_compatibility":      (READ,    None),

    # Self-Diagnostics (HERON-OPS-DIA-005). READ, and no operation of its
    # own - it composes what the other readers already report. It does
    # reach the bridge, through revit_health's own discovery, but it asks
    # nothing of a model and can change nothing.
    "heron_diagnose":           (READ,    None),

    # The Companion page (D-108, docs/40). READ, no operation: it opens a page
    # on 127.0.0.1 that reads the add-in's live status FILE - never the pipe -
    # and in Phase 1 the page changes nothing. When the page learns to apply
    # an edited table, that action gets its own row here at MODIFY, through
    # run_fragment_write, and this row stays READ.
    "heron_companion":          (READ,    None),

    # The Companion's editable table (docs/40 section 8). ANALYZE through
    # run_fragment_read, exactly revit_read's level: it reads the elements
    # into a table and changes nothing. Writing the edited cells is the
    # page's companion_table_apply below, never this tool.
    "revit_edit_table":         (ANALYZE, "run_fragment_read"),

    # Building loads (docs/44). ANALYZE through run_fragment_read, exactly
    # revit_edit_table's level: it reads every Space's take-off with
    # REPORT_SPACE_ENVELOPE, calculates in the brain, and changes nothing.
    # Writing the loads back is the page's companion_loads_finalize below.
    "revit_building_loads":     (ANALYZE, "run_fragment_read"),

    # Sprinkler hydraulics (docs/46). ANALYZE through run_fragment_read,
    # exactly revit_building_loads' level: it reads one fire protection
    # system with REPORT_SPRINKLER_NETWORK, solves it in the brain, and
    # changes nothing. Phase 1 writes nothing back, so it has no page action.
    "revit_sprinkler_hydraulics": (ANALYZE, "run_fragment_read"),

    # Sprinkler layout (docs/47). The same level: it reads the rooms with
    # REPORT_SPRINKLER_LAYOUT_SPACES and lays them out in the brain, placing
    # nothing. Placing is the page's companion_sprinkler_place below.
    "revit_sprinkler_layout":   (ANALYZE, "run_fragment_read"),

    # Puts a settings capability's CURRENT values on the Companion page as an
    # editable table, sending nothing to Revit. READ, no operation. The
    # page's Apply is companion_apply below, at MODIFY - never this tool.
    "revit_offer_settings":     (READ,    None),
}


# WHAT THE COMPANION PAGE CAN DO THAT THE CHAT CANNOT CALL (D-108, docs/40
# section 13). Not MCP tools - the host never sees them - but their risk is
# declared here, the one place a risk lives. companion_apply runs a settings
# change again with the modeller's values through revit_change's own body,
# so it is exactly revit_change's level and operation.
COMPANION_ACTIONS = {
    "companion_apply":          (MODIFY,  "run_fragment_write"),
    # The edited cells of an element table, in one SET_PARAMETER_VALUES_BY_ID
    # run through the same body: revit_change's level and operation.
    "companion_table_apply":    (MODIFY,  "run_fragment_write"),
    # The Loads panel's Finalize (docs/44 s6): the Spaces' loads and airflow
    # through the table's own Apply, then the diffusers' flows - each through
    # revit_change's body. Recalculate and Report touch no model - the brain
    # works on the take-off the panel holds - so they have no row.
    "companion_loads_finalize": (MODIFY,  "run_fragment_write"),
    # The Sprinkler Layout panel's Apply (docs/47 s6): one level of a
    # previewed layout through PLACE_FAMILY_INSTANCES, in revit_change's body.
    # Preview touches no model - the brain works on the rooms the panel holds.
    "companion_sprinkler_place": (MODIFY, "run_fragment_write"),
}


# WHAT A TOOL CAN ALSO SEND, BEYOND ITS OWN ROW - D-106, 2026-09-28.
#
# revit_change sends a fragment as the operation its OWN declared risk names:
# `run_fragment_write` for Modify and below - its row above - and one
# operation each for the two levels over it. The add-in's gate reads each of
# those at its own level and refuses it unless the owner's switch for that
# level is on in Revit, with Changes.
#
# A TABLE, NOT A SECOND RISK IN THE ROW, because the row's property - a tool's
# risk equals its operation's - is what tests/test_tool_registry.py holds the
# two languages to, and this keeps it true for every operation a tool can
# reach rather than for one of them. The client carries the same pairs as
# SWITCHED_OPERATIONS, which is what actually picks the operation; the test
# holds the two copies to each other and to the add-in.
ALSO_REACHES = {
    "revit_change": {
        PUBLISH: "run_fragment_publish",
        ADMIN: "run_fragment_admin",
    },
}


# THE TOOLS A CHAT IS GIVEN BEFORE IT ASKS, so its first Heron call is not
# preceded by a turn spent searching for the tool. With many MCP servers
# connected, Claude Code defers every tool behind a ToolSearch step; on the
# owner's PC that step came first in most Revit chats read back from the
# transcripts on 2026-10-01, about 5-10 s before the first Heron call. A tool
# named here is served with `_meta["anthropic/alwaysLoad"]`, which Claude
# Code documents as loading that one tool at session start
# (code.claude.com/docs/en/mcp, "Exempt a server from deferral").
#
# FEW, ON PURPOSE. Every tool here is paid for in context in every chat,
# asked or not - about 9,000 characters of descriptions for these five,
# measured 2026-10-01 (what they cost today is `--sizes` below, and
# tests/test_tool_sizes.py holds them to a budget) - and every other tool is
# still one search away. The two doors and the lookup cover almost any
# request (#369); health and levels
# are the first-time checks a modeller's chat makes. Loading a tool's
# description changes nothing about what it may do: its risk and labels above
# are unchanged, and the host still asks before a tool it was told can write.
#
# heron_method joined them on 2026-10-09: a lookup that names a job with a
# method sends the host straight to it, and a deferred tool there costs the
# search turn this list exists to save - on the one request, a family, where
# a chat with no memory of Heron otherwise read the method off disk.
#
# revit_show, the third door, is NOT here, and that was measured rather than
# chosen: on 2026-10-17 these six came to 4,435 characters of description
# against tests/test_tool_sizes.py's 4,500, and a door cannot be described in
# 65. Raising the budget is a decision (D-118). A chat finds it one search
# away, and revit_read's refusal of an EXECUTE capability names it.
ALWAYS_LOADED = ("heron_lookup", "heron_method", "revit_read", "revit_change",
                 "revit_health", "revit_levels")


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


def level_named(name):
    """
    The level a risk WORD names - "READ", "modify" - or None.

    For a fragment's own `risk:` line, which is a word on disk rather than a
    tool in this table. None means "not a level", and a caller must treat it
    as a refusal: a risk nobody can read is not a risk of zero.
    """
    wanted = (name or "").strip().upper()
    for level, word in NAMES.items():
        if word == wanted:
            return level
    return None


def door_refusal(door, declared, capability):
    """
    Why one fragment may not go through one tool - or None when it may.

    `door` is a tool in TOOLS. `declared` is the fragment's own `risk:` word
    as the client's reader found it, or None when it could not be read.

    THE CEILING IS THE DOOR'S OWN DECLARED RISK, read from this table rather
    than typed a second time. revit_read is ANALYZE because the operation it
    calls, `run_fragment_read`, is ANALYZE in the add-in - so a fragment
    declared above ANALYZE does not go through it. That refuses EXECUTE as
    well as MODIFY, and it has to: a fragment that only changes the SELECTION
    needs no transaction, so the one guarantee the read door stands on - Revit
    refuses a model change made outside one - would not cover it.

    revit_show is EXECUTE because `run_fragment_show` is, so the same rule
    lets the selection, the camera and the active view through it and still
    refuses MODIFY and above. What it adds to the read door's guarantee is
    exactly what EXECUTE is declared to do: UI state, never the model. That
    holds inside a linked model too - selecting or zooming to linked elements
    is not touching the link, so it asks for no confirmation (D-124).

    AN UNREADABLE RISK IS REFUSED, for the reason the client's `risk_refusal`
    gives: "I could not tell" and "it is fine" must not collapse into one
    answer on a path that runs code in somebody's model.

    BE HONEST ABOUT WHAT THIS IS - the same caveat as that function's. It
    stops a MISTAKE. The add-in never sees a fragment's own risk (FRAGMENT-
    ISSUES row 9 records why it is not sent), so a caller that skipped this
    server could put anything through run_fragment_read. What holds for every
    caller is two things, and the first is NOT a guarantee on its own: Revit
    refuses a change made outside a transaction - yet row 5b-350 is a read run
    of place-nested-family that reported that refusal and still left a real
    instance in the family, twice, cause not established (NEEDS-CHECKING CP1).
    So the add-in also counts the model before and after every read run and
    answers one that moved it `read_changed_model`, never as a read. That
    second check sees an element that appeared or disappeared and a model newly
    marked changed - not an edit in place in a model already unsaved.

    Pure - no file, no Revit, no SDK - so tests/test_read_door.py runs every
    outcome, and runs it over every fragment in the library, where CI can.
    """
    level = level_named(declared)
    if level is None:
        return ("'%s' does not say what risk it carries in a form Heron can read, "
                "so it will not be run. Nothing has been sent to Revit." % capability)

    ceiling = risk_of(door)
    if level <= ceiling:
        return None

    within = " and ".join(NAMES[n] for n in sorted(NAMES) if n <= ceiling)
    if level <= EXECUTE:
        # NOT revit_change: a capability that changes nothing that exists
        # does not need the Changes switch, and sending the chat there for it
        # is how "show me these" came to need write permission.
        instead = ("A capability declared %s changes nothing that exists, and "
                   "revit_show runs it - with Changes off, and no transaction "
                   "open." % NAMES[level])
    elif level <= MODIFY:
        instead = ("A change belongs to revit_change, and that needs Changes "
                   "switched on in Revit's ribbon.")
        # THE ONE MODIFY JOB A SHOW REQUEST IS USUALLY AFTER. Revit's temporary
        # isolate needs a transaction, so ISOLATE_ELEMENTS is MODIFY (D-125);
        # the fixed operation does it with Changes off.
        if door == "revit_show":
            instead += (" To isolate elements in the active view with Changes "
                        "off, revit_isolate_temporary does it.")
    else:
        # D-106: the level has its own switch now, and saying which one is
        # the whole of the way forward.
        switch = "Admin" if level == ADMIN else "Publish"
        instead = ("A capability declared %s belongs to revit_change, and that "
                   "needs %s switched on in Revit's Heron ribbon, with Changes."
                   % (NAMES[level], switch))
    return ("'%s' is declared %s, and %s runs nothing above %s - %s only. "
            "Nothing has been sent to Revit. %s"
            % (capability, NAMES[level], door, NAMES[ceiling], within, instead))



# THE SAFETY LABELS MCP LETS A SERVER PUT ON A TOOL, in the protocol's own
# spelling. A host reads them to decide what it may call without asking - and
# a host that is told nothing assumes the worst: not read-only, destructive,
# not idempotent. So an unlabelled tool is safe, and a WRONG label is the
# only real danger: a write marked read-only is one a host may run unasked.
#
# THAT IS WHY THEY ARE DERIVED HERE AND NEVER TYPED AT A TOOL. The risk in
# TOOLS is what the add-in's gate is checked against; a label typed beside a
# decorator would be a second declaration of the same fact, free to drift.
#
#   read-only    below EXECUTE: READ, ANALYZE and SUGGEST change nothing -
#                docs/12 section 1: observe, compute, propose
#   destructive  MODIFY and above - the boundary docs/12 draws, and the one
#                `writes()` already answers
#   idempotent   everything that does not write. Asking a read twice costs
#                nothing; asking a write twice can do the work twice, which is
#                exactly why the bridge client sends writes idempotent=False
#
# `openWorldHint` is NOT set, and that is a decision: risk says what a tool
# can change, not where it reaches, and this table has no column for the
# second. The host's default for it stands rather than a claim nobody derived.
#
# THE LABELS ARE ADVICE TO THE HOST. The gate is the add-in's, on the far side
# of the pipe, and nothing here moves it.
CAUTIOUS = {"readOnlyHint": False, "destructiveHint": True, "idempotentHint": False}


def annotations(tool):
    """
    The MCP safety labels for one tool, read off its declared risk.

    A tool nobody declared gets CAUTIOUS - the worst reading of every label,
    which is also what a host assumes of a tool with none. That is not a
    default risk (risk_of still refuses to invent one); it is the one label
    that cannot let a host call something unasked.
    """
    try:
        risk = risk_of(tool)
    except NotDeclared:
        return dict(CAUTIOUS)
    return {
        "readOnlyHint": risk < EXECUTE,
        "destructiveHint": risk >= MODIFY,
        "idempotentHint": not writes(tool),
    }


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
        # AND WHAT ELSE IT CAN SEND, on the line under it - the answer to
        # "which tools can change my model" is incomplete without it (D-106).
        for level in sorted(ALSO_REACHES.get(name, {})):
            lines.append("  %-*s  %-8s  also, as %s - only while the owner's %s "
                         "switch is on" % (width, "", NAMES[level],
                                           ALSO_REACHES[name][level],
                                           "Admin" if level == ADMIN else "Publish"))
    return "\n".join(lines)


# WHAT THE TOOL LIST COSTS A CHAT - D-03's accepted cost, measured rather than
# typed. "MCP tool schemas cost context on every request": every tool in
# ALWAYS_LOADED is in every chat's context before the modeller types, and
# every other tool joins it the moment a search loads it. The count was typed
# in prose once (the comment on ALWAYS_LOADED: about 9,000 for five,
# 2026-10-01), and on 2026-10-17 the six there measured 6,802 off the source -
# so the sizes are a command, as the inventory is.
#
# THE DESCRIPTION IS READ OFF THE SOURCE, with ast, so it needs no SDK and
# runs in CI - and it is EXACTLY what a host is served, because the server
# hands the SDK `inspect.cleandoc` of the docstring (_Labelled.tool), which is
# what ast.get_docstring returns. Before 2026-10-17 the SDK was handed the raw
# docstring, which Python 3.12 - the owner's - keeps indented four spaces a
# line, so the same tool cost more on his PC than in a 3.13 container.
# tests/test_mcp_serves.py holds the two to each other through a real SDK.
#
# THE SCHEMA AND THE WHOLE TOOL need the SDK, because the SDK writes the
# schema from the signature at run time. Without one they are NOT MEASURED,
# and said to be, never reported as zero.
SERVER_SOURCE = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                             "heron_mcp_server.py")


def descriptions(path=SERVER_SOURCE):
    """{tool: the description a host is served} for every `@server.tool()`."""
    with io.open(path, encoding="utf-8") as fh:
        tree = ast.parse(fh.read(), path)
    found = {}
    for node in tree.body:
        if isinstance(node, ast.FunctionDef) and any(
                ast.unparse(d) == "server.tool()" for d in node.decorator_list):
            found[node.name] = ast.get_docstring(node) or ""
    return found


def served_tools():
    """
    (every tool as the SDK serves it, as protocol JSON, None) - or (None, why)
    when no SDK can serve them here.

    BaseException, for the reason tests/test_mcp_serves.py gives: a broken SDK
    install raises a native panic that is not an Exception, and a size report
    must say it could not measure rather than die with a traceback.
    """
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    try:
        import asyncio
        import heron_mcp_server as served
        listed = asyncio.run(served.server.list_tools())
        return [t.model_dump(by_alias=True, exclude_none=True, mode="json")
                for t in listed], None
    except BaseException as why:                            # noqa: BLE001
        if isinstance(why, KeyboardInterrupt):
            raise
        return None, "%s: %s" % (type(why).__name__, why)


def instructions_size():
    """(characters of host.chat every chat is given, None) - or (None, why)."""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    try:
        import heron_brain
        return len(heron_brain.host_instructions()), None
    except Exception as why:                                # noqa: BLE001
        return None, "%s: %s" % (type(why).__name__, why)


def sizes(served=None, path=SERVER_SOURCE):
    """
    What each tool costs, largest first, and the totals a budget is held to.

    `served` is served_tools()'s list, or None to measure descriptions only.
    A size that was not measured is None, never 0.
    """
    by_name = dict((t.get("name"), t) for t in (served or []))
    rows = []
    for name, text in descriptions(path).items():
        tool = by_name.get(name)
        rows.append({
            "tool": name,
            "always_loaded": name in ALWAYS_LOADED,
            "description": len(text),
            "schema": (len(json.dumps(tool.get("inputSchema") or {}))
                       if tool is not None else None),
            "whole": len(json.dumps(tool)) if tool is not None else None,
        })
    rows.sort(key=lambda r: (-(r["whole"] or r["description"]), r["tool"]))

    def total(key, only_always=False):
        picked = [r[key] for r in rows if r["always_loaded"] or not only_always]
        return None if any(v is None for v in picked) else sum(picked)

    return {
        "tools": rows,
        "description": total("description"),
        "whole": total("whole"),
        "always_loaded_description": total("description", True),
        "always_loaded_whole": total("whole", True),
    }


def print_sizes(as_json=False):
    """`--sizes`: every tool's cost and the totals, derived on the spot."""
    served, why = served_tools()
    report = sizes(served)
    report["instructions"], unread = instructions_size()
    report["not_measured"] = dict((k, v) for k, v in (("schemas", why),
                                                      ("instructions", unread)) if v)
    if as_json:
        print(json.dumps(report, indent=2, sort_keys=True))
        return 0

    def say(value, absent="-"):
        return absent if value is None else "{:,}".format(value)

    print("What each MCP tool costs a chat, in characters, largest first.")
    print("* is in ALWAYS_LOADED: in every chat's context before the modeller types.")
    print()
    width = max(len(r["tool"]) for r in report["tools"])
    print("  %-*s  %11s  %7s  %7s" % (width + 2, "tool", "description", "schema", "whole"))
    for r in report["tools"]:
        print("  %-*s  %11s  %7s  %7s" % (width + 2, r["tool"] + (" *" if r["always_loaded"] else ""),
                                          say(r["description"]), say(r["schema"]), say(r["whole"])))
    print()
    print("  %d tools. Descriptions %s; the whole list as served %s."
          % (len(report["tools"]), say(report["description"]),
             say(report["whole"], "NOT MEASURED")))
    print("  ALWAYS_LOADED, %d tools: descriptions %s; as served %s."
          % (len(ALWAYS_LOADED), say(report["always_loaded_description"]),
             say(report["always_loaded_whole"], "NOT MEASURED")))
    print("  The instructions every chat is given (host.chat): %s."
          % say(report["instructions"], "NOT MEASURED"))
    for what, reason in sorted(report["not_measured"].items()):
        print()
        print("  NOT MEASURED - %s: %s" % (what, reason))
        if what == "schemas":
            print("  The descriptions above are read off the source and need no SDK;")
            print("  a schema is written by the SDK, so it needs one:  pip install --user mcp")
    print()
    print("Descriptions from heron_mcp_server.py's own docstrings; schemas and the whole")
    print("tool from the SDK's own list. The budget is tests/test_tool_sizes.py.")
    return 0


def main(argv=None):
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

    And what each one costs a chat, the same way:

        python mcp/server/heron_tools.py --sizes [--json]
    """
    argv = sys.argv[1:] if argv is None else argv
    if "--sizes" in argv:
        return print_sizes(as_json="--json" in argv)
    print("Every MCP tool Heron declares, worst first. %d in all." % len(TOOLS))
    print()
    print(describe())
    print()
    print("Derived from heron_tools.TOOLS, which is what the server enforces.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
