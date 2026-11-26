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
| ~~**PY1**~~ | *Negative - a pattern Revit does not offer.* `divide-family-surface` with the box's form id, `face=top`, `uGrid=20`, `vGrid=4`, `pattern=Not A Pattern` - the negative OT17's job already asks for. | **A refusal, NOT `fragment_threw`.** `refused` begins *"Nothing was divided. No pattern or panel called "Not A Pattern" is offered for this surface - Revit offers ..."* and names what Revit offers; `surfaceId` and `divided` are empty. **LOOKED AT:** the top face is not divided. A FAIL is a throw, a divided face left behind, or a list of offered types that is empty when the Type Selector shows some - each a new row. |
| **PY2** | *Positive, straight after PY1 on the same form.* The same call with `pattern=` empty, then once more on another face with a pattern from PY1's list. | The top face divided as version 2 did - *"U rule: Number 20; V rule: Number 4; Revit counts ... U-gridlines and ... V-gridlines"* - and NOT refused as *"already divided"* (PY1's rollback left nothing). The second call's pattern is read back by name. **RECORD** whether the face reference still worked after PY1's rolled-back `SubTransaction`. |
| **PY3** | *Positive, as version 1 was proved.* `report-pipe-segments` with `nameContains=Copper`, then `ZZZNOTASEGMENT`. | The same rows and counts as version 1's proof (6 of 19, each with its OD range); the first finding ends *"0 whose sizes could not be read"*; `unreadable` empty. The negative is as before: 0 reported of a non-zero `scanned`. |
| **PY4** | *Measure - does `GetSizes` ever throw?* `report-pipe-segments` with `nameContains` empty on every model to hand. | **RECORD** `unreadable`'s count on each. Zero everywhere means the new branch has never been seen run - say so, never call it proved. One that is not zero: its row says *"sizes NOT READ - Revit says: ..."*, it is not in `noSizes`, and Revit's words are written here. |

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`, built from main `e8c3be150`)

**Arranged:** PY1 and PY2 on *PY Box 2020* - a copy of Revit 2020's `US Metric\Mass\M_Box.rfa` in
`%TEMP%\heron-work\py`, one solid form `bcb0b59e-...-0000786e`, nothing divided. PY3 and PY4 on *Project1*, a new
never-saved project from Revit 2020's default template (17 pipe segments) - no `PIPE*.rvt` (version 1's model) was found
under Documents or D:\ on this PC. Every run rolled back, through `heron_bridge_client.py` from branch `claude/revit-runs-1126`.
Proof jobs [`py-divide-family-surface-v3-...`](../../tools/jobs/py-divide-family-surface-v3-revit2020-2026-11-26.yaml)
and [`py-report-pipe-segments-v2-...`](../../tools/jobs/py-report-pipe-segments-v2-revit2020-2026-11-26.yaml) - both
**PASS**, drafts waiting for the owner's signature.

| Row | Result |
|---|---|
| PY1, run 2026-11-26 | **PASS** - a refusal, not `fragment_threw`: *"Nothing was divided. No pattern or panel called "Not A Pattern" is offered for this surface - Revit offers _No Pattern, Rectangle, Triangle (bent), Rhomboid, Hexagon, 1/2 Step, 1/3 Step, Triangle Checkerboard (bent), Rectangle Checkerboard, Rhomboid Checkerboard, Triangle Step (bent), Arrows, Zig Zag, Octagon, Octagon Rotate, Triangle (flat), Triangle Checkerboard (flat)."* `surfaceId` and `divided` empty. Not looked at by eye - the run is rolled back, and the next call (PY2) found the face undivided. |
| PY2, run 2026-11-26 | **PASS** - the next call, `pattern=` empty: *"Divided its top face, 81 m2 ...: U rule: Number 20; V rule: Number 4; Revit counts 7 U-gridlines and 23 V-gridlines; pattern _No Pattern ... read back"* - not refused as already divided. Then `face=front`, `uGrid=6`, `vGrid=3`, `pattern=Rhomboid` (from PY1's list): *"... Revit counts 8 U-gridlines and 11 V-gridlines; pattern Rhomboid ... read back."* **RECORD:** each call is its own rolled-back run, so whether one face reference survives a rolled-back `SubTransaction` inside ONE call was not seen; across calls nothing was left behind. |
| PY3, run 2026-11-26 | **HELD on another model - not as written** (the row asks for version 1's own rows and counts) - `nameContains=Copper`: 4 of 17 (*Copper - A* and *B* 17 sizes OD 6.35 to 203.2 mm, *C* 5 sizes 9.52 to 25.4, *D* 9 sizes 31.75 to 152.4); first finding *"4 pipe segment(s) reported of 17 scanned; 0 carry no sizes; 0 whose sizes could not be read"*; `unreadable` empty. `ZZZNOTASEGMENT`: 0 of 17 scanned. Not version 1's 6 of 19 - a different model. |
| PY4, run 2026-11-26 | **RECORDED, not proved** - `nameContains` empty on Project1: `unreadable` **0** of 17. The family models open have no pipe segments. The new *"sizes NOT READ"* branch has still never been seen run. |

**Signed 2026-11-26 by Ajmal PS** (his word in chat, "6 sing"; signature put on by the session): `divide-family-surface` version 3 and `report-pipe-segments` version 2, both cards PROVEN.
