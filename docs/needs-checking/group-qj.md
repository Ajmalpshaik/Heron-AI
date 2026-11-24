# Needs checking — Group QJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QJ - REPORT_ROOF_REACH: does one roof reach another, measured with rays and changed nothing; and a duct's and a pipe's offset written by WRITE_ELEMENT_PARAMETERS - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1363](https://github.com/Ajmalpshaik/Heron-AI/issues/1363) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** two issues decided and routed in one change (session S14, 2026-11-24, on the owner's word that day:
*"research these two and design the answer yourselves"*).

- Row [5b-1953](../fragment-issues/row-5b-1953.md), GitHub issue
  [#1187](https://github.com/Ajmalpshaik/Heron-AI/issues/1187): asked *"does this roof reach the other roof"*,
  every backend answered `EXTEND_ROOF_TO_ROOF`, a write, and no read answered it. `MEASURE_DISTANCE` measures
  between bounding boxes, and a main roof's box takes in the space under its slopes, so a wing roof that stops
  short would read 0 mm, touching. The new card
  [`report-roof-reach`](../../brain/fragments/report-roof-reach/fragment.yaml) (`REPORT_ROOF_REACH`, DRAFT v1)
  measures what the question means: `EXTEND_ROOF_TO_ROOF`'s rays from each end of each roof to the other roof's
  upper face, and Revit's Boolean of the two solids for an overlap, changing nothing. Written in a cloud
  container with no Revit; it compiles on 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-24).
  **Nothing of it has run in Revit.** No add-in code changed: the card travels as source.
- Row [5b-1944](../fragment-issues/row-5b-1944.md), GitHub issue
  [#1169](https://github.com/Ajmalpshaik/Heron-AI/issues/1169): setting a run's offset from its level is
  `WRITE_ELEMENT_PARAMETERS`' job - the offset is one parameter, the built-in `RBS_OFFSET_PARAM`, called
  *Offset* or *Middle Elevation* in Properties as the release names it - and asking it is
  `READ_ELEMENT_LEVEL`'s, which reads that built-in. The write's proof never named that parameter. Its C# did
  not change here; **QJ5 to QJ7 are the PC run row 5b-1944 owed**, folded into this group.

**The models are scratch copies, never working models.** QJ1 to QJ4: *roofjoin-oy*, built for
[Group OY](group-oy.md) (OY1), arranged as the card's `tests/cases.yaml` says. QJ5 to QJ7: any scratch project
with one straight duct and one straight pipe on Level 1, nothing connected to either. Any release - **write
down which, and the name Properties gives the offset on it** (*Offset* or *Middle Elevation*): which release
renamed it was not settled in the cloud.

| ID | Do this | Pass looks like |
|---|---|---|
| **QJ1** | *THE CASE THAT MATTERS, RUN FIRST.* The wing roof and the main roof selected, view3d {3D}: *"does this roof reach the other roof"*. Then `MEASURE_DISTANCE` on the same two, and write both answers down | `meets` 0; `gapMm` more than 3000 and less than 7000; the first finding *"NO - the end of roof by extrusion ... stops short of roof by footprint ..."*. **Not 0 and not touching** - `MEASURE_DISTANCE` on the same pair is expected to say 0, box to box. The same distance `EXTEND_ROOF_TO_ROOF` says for this pair without `maxExtendMm` |
| **QJ2** | *Negative.* The far roof and the main roof selected, {3D} | `meets` 0, `gapMm` NaN; *"NOT MEASURED - no end of either roof faces the upper face of the other ... not the same as the roofs being apart"* - never *"NO"* with a gap |
| **QJ3** | *Negative.* (a) The wing roof alone. (b) The QJ1 pair with the main roof hidden in {3D} - put it back after | (a) *"Nothing was measured: ... it takes exactly TWO roofs, and 1 were selected"*. (b) *"NOT MEASURED ... Is the other roof showing in that view"* |
| **QJ4** | *Overlap.* In a copy saved as *roofjoin-qj-met*, `EXTEND_ROOF_TO_ROOF` on the QJ1 pair with `maxExtendMm` 10000, KEPT; then this card on the same pair | `meets` 1, `gapMm` 0; *"YES - ... already overlap"* and the report's *"OVERLAP - ... m3"*. Edit > Undo shows no entry from this card's run |
| **QJ5** | *Row 5b-1944's owed run.* The duct selected: `WRITE_ELEMENT_PARAMETERS` with `parameterName` the name Properties shows for the offset on this release and `value` 2700 | `written` 1; Properties shows the duct at 2700 mm from its level, both ends moved together; one entry in the undo list |
| **QJ6** | The pipe selected: the same card with `parameterName` `{RBS_OFFSET_PARAM}` (the built-in, whatever the release calls it) and `value` 2900. Then `READ_ELEMENT_LEVEL` on the pipe | `written` 1; Properties 2900 mm; `READ_ELEMENT_LEVEL` `offsetMm` 2900 |
| **QJ7** | *Negative.* The duct selected, `parameterName` the name the OTHER releases use (*Offset* where Properties says *Middle Elevation*, or the reverse), `value` 3100 | Expected: nothing written - the element in `absent`, the duct still at 2700. If something WAS written, write down which parameter moved: that is a finding for row 5b-1944, not a pass |
