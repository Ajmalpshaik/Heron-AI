# Needs checking — Group AH

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AH - the MCP server's read-only door, its rules and its labels (package C2, 2026-09-23)

Built in a cloud session with no Revit: `revit_read`, the server's instructions (`host.chat`) and the
safety labels on every tool ([04 §8](../04-heron-mcp.md)), and the owner's answer to D1 recorded as
[D-99](../DECISIONS.md#d-99--a-change-asked-for-in-a-chat-is-kept-at-once-with-no-preview-and-article-9-says-so) - so there is **no preview row here**: `revit_change` keeps changing at once, by
his choice. **Everything below passed only as code and against a stand-in Revit.** Use a **named** model,
and say which in the result.

| # | Check | Expected |
|---|---|---|
| **AH1** | **Changes OFF.** In a fresh chat on a named model, ask a question a PROVEN read answers - *"which views have no template"* - so the host calls `revit_read` | An answer naming the model, the capability's proof line (`PROVEN on ... and its code has not changed since`), and *"Nothing in the model was changed"*. The banner reads READING, not CHANGING; **nothing** is added to Revit's undo list. If it is refused for want of Changes ON, the add-in's gate disagrees with `HeronOperationRegistry` and that is the finding |
| **AH2** | **Changes OFF.** Ask `revit_read` for a capability above ANALYZE - `SET_SELECTION` (EXECUTE) and `DELETE_ELEMENTS` (MODIFY) | Both refused with *"Nothing has been sent to Revit"*; the selection on screen does not change; Heron's audit log has **no** entry for either, because nothing reached the add-in |
| **AH3** | Pin **Project1**, click into **Project2**, ask the same read again | The answer is about **Project1**, says it is not the model in front, and Project2 is untouched. The read door aims at the pin exactly as `revit_change` does (E16's write half) |
| **AH4** | A **DRAFT** read - one `heron_lookup` offers that is not PROVEN | It runs, and its answer says DRAFT and *"Treat this answer as a claim"* - never PROVEN |
| **AH5** | In a **fresh** Claude Code chat with Heron connected, before any tool call, ask *"what are your rules about undoing a change, and about saying compliant?"* | The answer comes from the server's instructions: Revit's own Undo, never a reversing change; never "compliant", the engineer or authority decides; and `revit_change` keeps a change at once (D-99). If the host cannot quote them, the instructions did not arrive - check `/mcp` for the server first |
| **AH6** | Look at Heron's tools in the host's tool list (`/mcp` in Claude Code) | `revit_change` and `revit_apply_move` are **not** marked read-only; `revit_read` and the list tools are. If the host shows no labels at all, say so - it is advice the host may not display, not a failure |
| **AH7** | Sign | A person reads AH1 to AH6 and signs. The machine never signs |

---
