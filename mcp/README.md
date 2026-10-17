# mcp/ — Part 2, Heron MCP

**The bridge between the AI host and Revit.** Runs *outside* Revit.

| | |
|---|---|
| Language | Python |
| Runs | as a normal process on the user's PC |
| Changing it needs | no rebuild and no Revit restart — the chat closed and reopened, which starts a new server (rule 5) |
| Talks to | Claude Code over MCP (stdio) · the Revit add-in over a **local named pipe** |

## What's here

| | Does |
|---|---|
| `client/` | Bridge discovery and the pipe client (`heron_bridge_client.py`) — finds connected Revits, verifies each answers — and `heron_config.py`, the same settings file the add-in reads, read from this side |
| `server/` | The MCP server, [`heron_mcp_server.py`](server/heron_mcp_server.py) — Step 3, extended since. Every tool is an `@server.tool()` function in that one file. Beside it, one module per agent it leans on: `heron_session` (one chat, one Revit), `heron_write` (the conversation half of writing), `heron_workflow` (checkpoints and *Continue*), `heron_failure` (why it failed, never a blind retry), `heron_runtime` (what Heron can do right now), `heron_run_records` (the short change report `revit_change` prints, and the run records `heron_run_record` pages from disk), `heron_health`, `heron_diagnose`, `heron_install_health`, `heron_register`, `heron_discovery` and `heron_running_code` (whether this process still runs the Python on disk). List them with `ls server/` |
| [`companion/`](companion/README.md) | **The Heron Companion page** ([D-108](../docs/DECISIONS.md), [docs/40](../docs/40-heron-companion.md)). A small web server started inside this process when the chat calls `heron_companion`, on `127.0.0.1` only. Phase 1 **shows** the model, view and selection from the add-in's live file and changes nothing. It never talks to any AI and never prints to stdout |
| [`server/heron_tools.py`](server/heron_tools.py) | **What each tool can do, and the one place that says so.** Its risk is what the add-in's gate is checked against; the MCP safety labels a host sees, `revit_read`'s ceiling (nothing above `ANALYZE`) and `revit_show`'s (nothing above `EXECUTE`) are **derived** from it, never typed at a tool ([docs/04 §8](../docs/04-heron-mcp.md)). The list: `python server/heron_tools.py`; a served tool missing from it fails closed rather than defaulting to READ (`tests/test_tool_registry.py`) |
| [`server/heron_brain.py`](server/heron_brain.py) | **The one place this side reaches `brain/`** — Step 14. It started with three tools on it — what Heron knows how to do, who provides a capability, and which capability a sentence needs — and there are more now. **Derive how many** rather than reading a figure here: `grep -c 'brain\.' server/heron_mcp_server.py` counts the calls, and the tools are the `@server.tool()` functions around them. It said *three* until 2026-09-21, when it was ten — row 5b-82 |

## Rules for this folder

1. **Transport only.** No BIM logic, no knowledge, no decisions about what a duct is. Those belong in
   `brain/`. [docs/04](../docs/04-heron-mcp.md)

   **`heron_brain.py` does not bend this rule — it is what makes it affordable.** No knowledge lives
   here: that file opens a store, asks the registry and hands back rows. Every judgement about what a
   duct is stays in `brain/`, and every judgement about what the user meant stays in the host
   ([D-01](../docs/DECISIONS.md) — the Orchestrator is Claude Code, not this folder).

   It exists because until 2026-08-29 the rule was being kept by **having no route at all**: eight brain
   modules, seven fragments and ten skills, imported by nothing but their own tests, and therefore
   unreachable from any conversation. One named seam is what lets the rule hold without that.
2. **The discovery file is an address book**, not a status report. Static facts only; the document
   name is deliberately never stored — that is what caused the stale-name trap
   ([docs/25](../docs/25-multi-session-and-binding.md)).
3. **Never guess which Revit.** With more than one connected, send nothing until the user chooses.
4. **The user never sees a process number.** Version, project, availability — chosen by list number.
5. **"Changing it needs nothing" is true of the NEXT chat, not this one.** A running server keeps the
   Python it started with. While any of its files has changed since it was loaded, every answer says to
   close and reopen the chat, and a tool that changes the model is refused
   ([`heron_running_code.py`](server/heron_running_code.py) has the decision; NEEDS-CHECKING
   [group EU](../docs/needs-checking/group-eu.md)).

## Fix things here when

Heron can't find Revit · the wrong session is picked · a stale entry lingers · the tool list is wrong ·
an MCP tool misbehaves.
