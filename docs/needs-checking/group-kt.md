# Needs checking — Group KT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KT - spot coordinates in a view: `ADD_SPOT_COORDINATES` (2026-11-01)

Asked for in the modeller's words: *"add spot coordinates to these column corners"*, *"put a spot
coordinate on this point"*, *"show the north/east coordinates of these grid intersections"* - Revit's
Annotate > Spot Coordinate. Nothing in Heron placed one: `REPORT_SPOT_DIMENSIONS` reads them,
`ADD_SPOT_ELEVATIONS` places heights, not north and east. `brain/heron_retrieve.py`, Revit 2024, offered
`REPORT_SPOT_DIMENSIONS`, `ACQUIRE_COORDINATES` and `REPORT_LOCATION` for the three.

| What | Where | What it does |
|---|---|---|
| `ADD_SPOT_COORDINATES` (MODIFY) | [`brain/fragments/add-spot-coordinates`](../../brain/fragments/add-spot-coordinates/fragment.yaml) | Places a spot coordinate on each element handed in, in **one view**, of the spot coordinate type the modeller **names** - at the middle of the face that looks at the viewer, or with `at` "corners" at each end of its straight edges; with two or more **grids** selected, at each grid intersection, on the highest upward face of the other selected elements under it (plans only). **No type named, or one no spot coordinate type has: nothing is placed** and the loaded types are listed. Views Revit cannot annotate - a schedule, a sheet, a legend, a drafting view, a perspective, an unlocked 3D view, a template - are refused by name. Named and left: no face looking at the viewer, a spot already within 1 mm of the point, refused by Revit, of another model. Every spot read back - its type, its view and the text Revit shows - and a wrong one removed inside the same undo |

**What is done and what is not.** It compiles on all eight releases (`tools/check-fragments-compile.py`,
2026-11-01, a Linux container). Every Revit member it uses was looked up with `tools/api-surface
--members` for 2020 to 2027 and is present with the same signature in all eight -
`NewSpotCoordinate(View, Reference, XYZ, XYZ, XYZ, XYZ, bool)` among them. **Nothing here has run in
Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KT2 to KT5 measure them): which
spot coordinate types the metric architectural template has; **whether `Dimension.ValueString` gives back
a spot coordinate's north and east at all** - the card says so when it gives back nothing; whether
`Dimension.Origin` of a spot is the point it marks (the card's no-stacking check relies on it); whether
Revit accepts a column's TOP face as the reference in a plan where the column is cut below its top
(`ADD_SPOT_ELEVATIONS`' proof saw many *"references are no longer valid"* on L3); and whether Revit
places a spot coordinate in an elevation, a section and a locked 3D view, all of which the card allows.

**Routing.** Measured through `heron_retrieve.find` on two scratch stores, before and after the card,
with and without the question flag: *"what are the coordinates of this point"* moved onto this write and
was declared on `REPORT_LOCATION` - [row 5b-1377](../fragment-issues/section-5b-rows-401-425.md), FIXED.
*"space these round a point"* moved from one write it does not name to another - row 5b-1378, OPEN.

**The model - a scratch project from the metric architectural template in Revit 2024.** Never a working
model. Every run below is rolled back except KT1's arrangement and KT5's one run. The job file is
[`tools/jobs/add-spot-coordinates-kt-2026-11-01.yaml`](../../tools/jobs/add-spot-coordinates-kt-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KT0** | *Open.* Revit 2024, a new project from the metric architectural template, **nothing selected**, Changes on at the ribbon | The title bar names the scratch project |
| **KT1** | *Arrange, and KEEP it.* By hand on Level 1: F1 (a rectangular 200 mm floor), C1 (a 400 x 400 mm concrete column standing on F1), grids A and B one way and 1 and 2 the other, all crossing over F1, and one text note T1 in "FloorPlan: Level 1". Save | "FloorPlan: Level 1" shows F1, C1, the four grids and T1, and no spot coordinate |
| **KT2** | *Read the types - also the "none named" negative.* `add-spot-coordinates` with `--write`, `view` Level 1, `spotType` left out, F1 selected | `placed` 0, *No spot coordinate type was named, and one is never chosen for you*, `typesLoaded` listing the types. Write one into the job file, both legs |
| **KT3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: the text note - `placed` 0, T1 in `notPlaced`. Positive: `placed` 1 on F1, `newSpots` 1 id, `report` quoting the text the spot shows - **record** whether it gave back text at all |
| **KT4** | *Corners, grids and the refusals.* C1 alone with `at` corners; the four grids with F1; F1 with `view` an unlocked 3D view, then a schedule; F1 with `spotType` a spot elevation type; F1 in a section and an elevation of Level 1 | C1: `placed` 4 - **record** whether Revit took the column's top face in a plan that cuts it. Grids: `placed` 4, *grid A/1* and its like. 3D and schedule refused by name. Spot elevation type refused as one. Section and elevation: **record** whether Revit placed it or refused |
| **KT5** | *Kept, through `try-once`, on the modeller's yes.* KT3's positive on the saved project, "Level 1" in front | Keep or Discard. Then the same run again places nothing - *a spot coordinate is already there* - and REPORT_SPOT_DIMENSIONS on Level 1 lists the spot with the same text: the second route |
