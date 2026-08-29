# mcp/ — Part 2, Heron MCP

**The bridge between the AI host and Revit.** Runs *outside* Revit.

| | |
|---|---|
| Language | Python |
| Runs | as a normal process on the user's PC |
| Changing it needs | nothing — no restart, no rebuild |
| Talks to | Claude Code over MCP (stdio) · the Revit add-in over a **local named pipe** |

## What's here

| | Does |
|---|---|
| `client/` | Bridge discovery and the pipe client. Finds connected Revits, verifies each answers |
| `server/` | The MCP server — Step 3 |
| [`server/heron_brain.py`](server/heron_brain.py) | **The one place this side reaches `brain/`** — Step 14. Three tools stand on it: what Heron knows how to do, who provides a capability, and which capability a sentence needs |

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

## Fix things here when

Heron can't find Revit · the wrong session is picked · a stale entry lingers · the tool list is wrong ·
an MCP tool misbehaves.
