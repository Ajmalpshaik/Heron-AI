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
| **NV3** | *The negative:* the same run on an add-in from before D-158 (or with `sendIds` removed from the request) | The row says Heron received 3 of N and offers **no** Select - nothing is half-selected |

[D-159](../decisions/D-159.md) adds a check's severity, clause and advice, and what changed since the last
run on the same model. Same scratch project.

| ID | Do this | Pass looks like |
|---|---|---|
| **NV4** | In the chat, save a checklist with a severity, a BEP clause and fix advice on one check; run it | The chat and the Companion page show the severity and clause on that row, and the advice under it when it fails |
| **NV5** | Run the same checklist again with nothing changed | Every check that failed says **STILL OPEN**; the line above the table says *0 new, 0 fixed* |
| **NV6** | Draw walls to enclose two of the unenclosed rooms, run again | That row says **STILL OPEN** with *2 fixed since*; enclose all of them and run again: **FIXED** |
| **NV7** | *The negative:* open another model and run the same checklist | *First kept run of this checklist on this model* - **never** compared with the first model's run |
| **NV8** | Download the CSV | Columns *Since last run*, *Severity*, *Clause* and *To fix*, filled as on the page |
| **NV9** | Look in `%USERPROFILE%\.heron\checklists\runs\` after more than 20 runs | One folder per checklist, at most 20 files in it; no run file appears in the list of checklists |
