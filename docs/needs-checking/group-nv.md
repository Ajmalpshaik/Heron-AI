# Needs checking — Group NV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NV - a QA check's failing ids whole, and its severity, clause and history (2026-11-12)

[D-158](../decisions/D-158.md) sends a QA check's failing ids whole, so **Select in Revit** takes every one.
**It compiles for 2020 to 2027 and runs without Revit** (`tests/test_checklists.py`,
`tests/test_checklist_tool.py`, whose new checks were seen to fail on the code before the change).
**None of it has run in Revit.**

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Its rooms are
arranged so that the starter checklist's *"no room is left unenclosed"* fails with **more than three**
rooms - delete a wall under four or more rooms.

| ID | Do this | Pass looks like |
|---|---|---|
| **NV1** | Save the starter checklist, run it from the Companion's QA Checklist page | The unenclosed-rooms row FAILS with its count, and offers **Select in Revit** - no *"received 3 of N"* |
| **NV2** | Press **Select in Revit** on that row | Every unenclosed room is selected in Revit - count them against the row |
| **NV3** | *The negative a modeller can run:* NV15 below - more failing elements than the add-in sends. *A developer's case, not a modeller's:* the same run against an add-in from before D-158, which never sends the list whole | The row says Heron received 3 of N (the developer's case) or `IdsSentPerList` of N (NV15) and offers **no** Select - nothing is half-selected |

[D-159](../decisions/D-159.md) adds a check's severity, clause and advice, and what changed since the last
run on the same model. Same scratch project.

| ID | Do this | Pass looks like |
|---|---|---|
| **NV4** | In the chat, save a checklist with a severity, a BEP clause and fix advice on one check; run it | The chat and the Companion page show the severity and clause on that row, and the advice under it when it fails |
| **NV5** | Run the same checklist again with nothing changed | Every check that failed says **STILL OPEN**; the line above the table says *0 new, 0 fixed* |
| **NV6** | Draw walls to enclose two of the unenclosed rooms, run again | That row says **STILL OPEN** with *2 no longer failing*; enclose all of them and run again: **FIXED** |
| **NV7** | *The negative:* open another model and run the same checklist | *First kept run of this checklist on this model* - **never** compared with the first model's run |
| **NV8** | Download the CSV | Columns *Since last run*, *Compared with*, *Severity*, *Clause* and *To fix*, filled as on the page; *Since last run* reads *met both times* or a label - never blank for a check |
| **NV9** | Look in `%USERPROFILE%\.heron\checklists\runs\` after more than 20 runs | One folder per checklist, one folder in it per model, at most `RUNS_KEPT` (20) files in each; no run file appears in the list of checklists |

**Repaired 2026-11-12** after an independent review of #866 (rows
[5b-1613](../fragment-issues/row-5b-1613.md) to [5b-1620](../fragment-issues/row-5b-1620.md)): a model is
its file path, a check is matched by its values too, and every doubt is NOT COMPARED. **Built, not run in
Revit.** Same scratch project, saved to a folder of its own.

| ID | Do this | Pass looks like |
|---|---|---|
| **NV10** | Run a checklist; **close Revit**, open Revit and the same file again, run the same checklist | *Since the last run on ...* - compared with the run before Revit closed. *The negative:* **Save As** under a new name and run again - *first kept run of this checklist on this model* |
| **NV11** | Start a **new project** and never save it; run a checklist twice | Both replies say *this model has no file path ... Save the model so runs can be compared*, every row **NOT COMPARED**. *The negative:* save it, run twice - the second run is compared |
| **NV12** | In the chat, replace a check of a saved checklist with the same sentence and another value (rooms to Spaces, say); run | That row is **NOT COMPARED** - *added or edited since* - never **FIXED**. *The negative:* run again with no edit - it is compared |
| **NV13** | Save a check on a capability that reads the selection (`python brain/heron_checklists.py --fragments` lists them under *reads the selection*); run with one selection, then with another | That row is **NOT COMPARED** both times - *reads what is selected in Revit* - never **FIXED** or **NEW**. *The negative:* a check in the same list that collects its own elements is compared |
| **NV14** | Arrange **more than 500** (and fewer than `IdsSentPerList`) failing elements on one check - 2,000, say; run, press its button, and time it | The button reads *Select in Revit* and Revit selects **every one** in one press, at a speed a modeller can wait for - [row 5b-1621](../fragment-issues/row-5b-1621.md), closed in code 2026-11-13. A press that takes too long to use is a finding for [Q-90](../open-questions/tier-2.md). *The negative:* 500 or fewer - the same button, every one |
| **NV15** | Arrange **more than `IdsSentPerList` (5,000)** failing elements on one check; run | The row says Heron received 5,000 of N and offers **no** Select. *The negative:* NV14's count - a button is offered |
| **NV16** | NV1, NV2 and NV5 on a **Revit 2024 or later** model, whose element ids are 64-bit | Select takes every id; the second run compares as on an earlier release |
| **NV17** | Run; **delete** some of a failing row's elements; press **Select in Revit** on that row without running again; then run again | Select picks only elements still in the model, says nothing that claims the deleted ones, and Revit shows no error; the next run says **STILL OPEN** with *N no longer failing* - never *fixed* |
