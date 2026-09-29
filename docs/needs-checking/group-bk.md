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
| **BK6** | **Recorded 2026-09-29, from the owner:** BK1 and BK2 passed in Revit 2024 on *Project1* - the page read **Connected** and **3 ducts**. Revit felt slow; the owner disconnected Heron and found it **just as slow**, so the Companion was not the cause ([D-109](../DECISIONS.md)) | **PASSED** for BK1 and BK2's positive halves; their negative halves (Esc shows 0, disconnect clears the page) still owed |
| **BK7** | After the D-109 deploy: press the **Companion** button on the Heron tab with a Claude chat running | The page opens in the browser, already showing this Revit. **Negative:** close every Claude chat, press it again - a Revit message says no chat is running and nothing opens |
| **BK8** | Click the arrow under Companion, choose **Turn Companion off** | The button turns grey and reads *Companion off*; within about two seconds the page stops, and a chat question such as *"how many ducts"* still answers. **Negative:** ask the chat to *"open the Heron Companion"* - it says the Companion is switched off, and nothing opens. Turn it back on - the button is blue again and the Companion button opens the page |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the add-in compiles on
2020 to 2027, and `tests/test_companion.py`, `test_tool_registry.py`, `test_mcp_serves.py` and
`test_paths.py` pass.
