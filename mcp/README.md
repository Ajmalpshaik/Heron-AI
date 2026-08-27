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

## Rules for this folder

1. **Transport only.** No BIM logic, no knowledge, no decisions about what a duct is. Those belong in
   `brain/`. [docs/04](../docs/04-heron-mcp.md)
2. **The discovery file is an address book**, not a status report. Static facts only; the document
   name is deliberately never stored — that is what caused the stale-name trap
   ([docs/25](../docs/25-multi-session-and-binding.md)).
3. **Never guess which Revit.** With more than one connected, send nothing until the user chooses.
4. **The user never sees a process number.** Version, project, availability — chosen by list number.

## Fix things here when

Heron can't find Revit · the wrong session is picked · a stale entry lingers · the tool list is wrong ·
an MCP tool misbehaves.
