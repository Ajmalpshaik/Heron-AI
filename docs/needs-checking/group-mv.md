# Needs checking — Group MV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MV - QA checklists: checks in the modeller's words saved from the chat, run in one go from the chat and from the Companion's QA Checklist page (2026-11-08)

**What was built** ([D-152](../decisions/D-152.md), proposed; [docs/50](../50-qa-checklists.md)): the chat's
`heron_checklist` saves a checklist - each check one capability that only reads, with a pass rule on one
output - and runs it check by check through `revit_read`'s own body; the Companion has a **QA Checklist**
workspace with Run, the results and Select in Revit. **Tested without Revit - never run in a real Revit.**

**The model is a test copy** with at least one linked model, rooms and Spaces. The Claude app restarted so the
chat loads the new tool. Changes may stay OFF: nothing here writes.

| ID | Do this | Pass looks like |
|---|---|---|
| **MV1** | Revit 2024, Heron connected. In the chat: *"save Heron's starter QA checklist as Before issue"*, then *"run Before issue"* | The chat names each check with **PASS**, **FAIL** or **NOT RUN**, the value read and the rule, then the counts on the named model, and says nothing in the model changed. The undo list in Revit has **no new entry**. Never the word compliant. **Negative:** unload one link in Revit and run it again - *every linked model is loaded* is **FAIL** with `notLoaded` read 1 |
| **MV2** | Companion open (Heron tab, **Companion**). Sidebar → **QA Checklist**, pick *Before issue*, press **Run** | The rows appear one by one as each check is read; the button says *Running…*; the badges are green PASS, red FAIL, amber NOT RUN. **Negative:** press **Run** again while it runs - the page says a checklist is already running, and no second run starts |
| **MV3** | Make a room unenclosed (delete one of its bounding walls on the test copy), Run again, press **Select in Revit** on *no room is left unenclosed* | That room is selected in Revit, and nothing in the model changes. **Negative:** a row whose failing ids number more than three says Heron received three of them and offers no Select button |
| **MV4** | In the chat, save a checklist with one check on a DRAFT read capability (*"heron_lookup"* names one), then run it | That check is **NOT RUN**, saying it is DRAFT, and the counts do not add it to the passes. **Negative:** ask the chat to save a check on `DELETE_ELEMENTS` - it is refused, saying a check may only read, and nothing is saved |
| **MV5** | Close Revit's model (or disconnect Heron), then run *Before issue* from the chat and from the page | Every check is **NOT RUN**, saying Revit is not connected; nothing is sent. **Negative:** reconnect and run - the checks read again |
| **MV6** | While the page's run is going, ask the chat a question that reads the model (*"how many levels are there"*) | The chat answers between two checks - the run does not hold Revit for its whole length. **Negative:** none - a wait for the whole run is the FAIL |
| **MV7** | The same as MV1 and MV2 in **Revit 2020** and **Revit 2025** | The same answers on .NET Framework and on .NET 8 |
