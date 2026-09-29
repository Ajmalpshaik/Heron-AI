# Needs checking — Group BK

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-29 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BK - the Heron Companion, Phase 1: a page beside Revit shows the model, the view and the selection (2026-09-29)

**What was built** ([D-108](../DECISIONS.md), [40 §6](../40-heron-companion.md)): the add-in writes a small file,
`%LOCALAPPDATA%\Heron\live\pid-N.json`, whenever the model, view or selection changes - only while the Heron
button is connected - and the chat's `heron_companion` tool opens a page in the browser that reads it. The page
changes nothing, and nothing is asked of Revit through the pipe for it.

**Compiled, never run in Revit.** `tests/test_companion.py` holds the page's protection, its silence on stdout
and the file rules on this machine. Whether Revit writes the file, and what it costs, is what this group asks.

**The model is a test copy** - the owner's *heron ai bulding* copy or `Snowdon-scratch_ajmal.al`. Revit must be
closed for the deploy, and the Claude app restarted so the chat loads the new tool.

| ID | Do this | Pass looks like |
|---|---|---|
| **BK1** | Revit 2024, test copy open, press Heron to connect. In a new chat ask *"open the Heron Companion"* | The browser opens a page reading **Connected**, Revit 2024, the model's name and the active view. The chat's reply is one or two lines with **no address in it**. **Negative:** press Heron again to disconnect - within about a second the page says Revit is not connected, and `%LOCALAPPDATA%\Heron\live` holds no file for that Revit |
| **BK2** | Select 3 ducts | The page shows **3 - Ducts 3** within a second. **Negative:** press Esc - it shows **0**, not the old 3 |
| **BK3** | The same as BK1 and BK2 in **Revit 2020**, the oldest release | The same answers. **Negative:** leave Revit idle for 5 minutes with nothing changing - the live file's modified time does not move |
| **BK4** | Select about 5,000 elements (a busy MEP level, window-select) | Revit stays responsive - no pause you can feel beyond Revit's own; the page shows the count and says only the first 5,000 were sorted into categories. **Negative:** close the model - the page no longer names it |
| **BK5** | With the page open, close the chat (or quit the Claude app) | Within about three seconds the page turns grey and says the chat that opened it has closed. **Negative:** the old link, pasted into a new tab, reads *link already used or too old* and shows nothing |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the add-in compiles on
2020 to 2027, and `tests/test_companion.py`, `test_tool_registry.py`, `test_mcp_serves.py` and
`test_paths.py` pass.
