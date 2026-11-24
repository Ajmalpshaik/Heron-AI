# Needs checking — Group PY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PY - a wrong divided-surface pattern is refused, not thrown, and a pipe segment's size list that will not read is never "no sizes" - DRAFT until proved (2026-11-23)

**Tracked as GitHub issue [#1311](https://github.com/Ajmalpshaik/Heron-AI/issues/1311) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** two cards reported a failure wrongly, both found by reading with no Revit.
[Row 5b-1989](../fragment-issues/row-5b-1989.md): `DIVIDE_FAMILY_SURFACE` version 2 looked the pattern up only
after the surface was made, so a name Revit does not offer threw and the whole call failed - a failure, not a
refusal. Version 3 makes the division inside a `SubTransaction`, looks the pattern up the moment the surface
exists, and on a miss rolls that back and refuses, naming what Revit offers.
[Row 5b-1982](../fragment-issues/row-5b-1982.md): `REPORT_PIPE_SEGMENTS` version 1 swallowed a throw from
`GetSizes` and wrote the segment *"NO SIZES"*. Version 2 writes *"sizes NOT READ - Revit says: ..."*, puts the
segment in the new `unreadable` and never in `noSizes`, and counts it in the first finding.
Both compile for Revit 2020 to 2027 (`python tools/check-fragments-compile.py`, 2026-11-23, cloud container).
**Not tested in Revit.** Both cards are `DRAFT` until their checks pass ([D-30](../DECISIONS.md)). Rebuild the
add-in and restart Revit first (`python tools/heron-ready.py`) - that needs Revit closed, so it is the owner's step.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`divide-family-surface`](../../brain/fragments/divide-family-surface/fragment.yaml) (`DIVIDE_FAMILY_SURFACE`) | PROVEN v2 | DRAFT v3 | [5b-1989](../fragment-issues/row-5b-1989.md) | PY1, PY2 |
| [`report-pipe-segments`](../../brain/fragments/report-pipe-segments/fragment.yaml) (`REPORT_PIPE_SEGMENTS`) | PROVEN v1 | DRAFT v2 | [5b-1982](../fragment-issues/row-5b-1982.md) | PY3, PY4 |

**The models: scratch copies, never a working one.** For PY1 and PY2 the box mass Group OT and Group PQ use
(`PQ Box 2020` on Revit 2020, or the Revit 2024 box in
[`tools/jobs/ot-p1102-box-revit2024-2026-11-21.yaml`](../../tools/jobs/ot-p1102-box-revit2024-2026-11-21.yaml)),
with its top face not yet divided. For PY3 and PY4 a scratch copy of the `PIPE` project version 1 was proved on.

| ID | Do this | Pass looks like |
|---|---|---|
| **PY1** | *Negative - a pattern Revit does not offer.* `divide-family-surface` with the box's form id, `face=top`, `uGrid=20`, `vGrid=4`, `pattern=Not A Pattern` - the negative OT17's job already asks for. | **A refusal, NOT `fragment_threw`.** `refused` begins *"Nothing was divided. No pattern or panel called "Not A Pattern" is offered for this surface - Revit offers ..."* and names what Revit offers; `surfaceId` and `divided` are empty. **LOOKED AT:** the top face is not divided. A FAIL is a throw, a divided face left behind, or a list of offered types that is empty when the Type Selector shows some - each a new row. |
| **PY2** | *Positive, straight after PY1 on the same form.* The same call with `pattern=` empty, then once more on another face with a pattern from PY1's list. | The top face divided as version 2 did - *"U rule: Number 20; V rule: Number 4; Revit counts ... U-gridlines and ... V-gridlines"* - and NOT refused as *"already divided"* (PY1's rollback left nothing). The second call's pattern is read back by name. **RECORD** whether the face reference still worked after PY1's rolled-back `SubTransaction`. |
| **PY3** | *Positive, as version 1 was proved.* `report-pipe-segments` with `nameContains=Copper`, then `ZZZNOTASEGMENT`. | The same rows and counts as version 1's proof (6 of 19, each with its OD range); the first finding ends *"0 whose sizes could not be read"*; `unreadable` empty. The negative is as before: 0 reported of a non-zero `scanned`. |
| **PY4** | *Measure - does `GetSizes` ever throw?* `report-pipe-segments` with `nameContains` empty on every model to hand. | **RECORD** `unreadable`'s count on each. Zero everywhere means the new branch has never been seen run - say so, never call it proved. One that is not zero: its row says *"sizes NOT READ - Revit says: ..."*, it is not in `noSizes`, and Revit's words are written here. |
