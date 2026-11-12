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
