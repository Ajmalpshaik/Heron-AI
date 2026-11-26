# Needs checking — Group QQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group QQ - the rest of the night of 2026-11-24 on Revit 2024: nine MEP and view-filter cards on Night Ducts 1124, and two arrangements owed - NOT RUN (2026-11-24)

**Tracked as GitHub issue [#1388](https://github.com/Ajmalpshaik/Heron-AI/issues/1388) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit 2024 on the owner's PC.** A cloud session cannot run any of it.

**Why:** the night of 2026-11-24 stopped proving when Revit 2024 reached 13.2 GB of private memory, the night's
stop line for new compiles, before Night Ducts 1124 was opened. Its job file was written and never run:
[`tools/jobs/night-ducts-revit2024-2026-11-24.yaml`](../../tools/jobs/night-ducts-revit2024-2026-11-24.yaml). Two
cards of the batch-5 and batch-2 job files ran and were not proved, for want of an arrangement. Every run is rolled
back, and `batch-prove.py` never signs: a PASS leaves a draft for the owner to read and sign.

**The models**, both in `%TEMP%\heron-work\night-1124`: *Night Ducts 1124*, a file copy of the 2026-11-20 scratch
*Ducts 1040* (three 400 x 200 rectangular ducts; the filters Domestic, Sanitary and Vent on *FloorPlan: 1 - Mech*),
and *Night Rig 1124* (no filters, no MEP), the negative document for two of the nine. **The job file's values were
taken from earlier models of the same template and are NOT checked on Night Ducts 1124** - the view names, whether
the ducts are insulated, and the pipe segment names. Read them against the model before the run.

| ID | Do this | Pass looks like |
|---|---|---|
| **QQ1** | Restart Revit 2024. Open *Night Ducts 1124* and *Night Rig 1124* from `%TEMP%\heron-work\night-1124` with `validate` / `fragment activate-document` (the job file's read path), not by hand. Settings: Changes ON, Publish ON, Ask to Keep OFF. Read the job file's view names, insulation and segment values against *Night Ducts 1124*. Then `python tools/batch-prove.py tools/jobs/night-ducts-revit2024-2026-11-24.yaml --dry-run`. | Both models open; every value the job file names is found in the model (a value that is not is corrected in the job file before the run, and said); the dry run lists nine jobs and refuses none. |
| **QQ2** | `python tools/batch-prove.py tools/jobs/night-ducts-revit2024-2026-11-24.yaml --unattended --session <pid>` - the pid of the restarted Revit 2024, from `python mcp/client/heron_bridge_client.py list`. Nine cards: `report-view-filters`, `apply-view-filter`, `set-view-filter-categories`, `report-connectors`, `select-by-insulation`, `report-mep-pressure-drop`, `report-pipe-segments`, `set-mep-justification`, `auto-size-mep`. | Each card's verdict is **PASS** - both halves held, a draft written for the owner to read. Anything else is recorded by card: **NEG NOT EMPTY** is a finding about the card (its negative found something); **POSITIVE EMPTY** or **NO NEGATIVE** is a missing arrangement; **REFUSED**, **TIMEOUT** or **DID NOT RUN** says the leg never gave an answer. None of these is a pass. |
| **QQ3** | `report-category-overrides` (batch 5, [`night-rig-5-revit2024-2026-11-24.yaml`](../../tools/jobs/night-rig-5-revit2024-2026-11-24.yaml)): arrange a negative view with **no category hidden** - `set-category-visibility` in the negative's setup, or a view made for it - and run the card again. | **PASS**: the positive reports Walls' halftone override in *FloorPlan: Level 1*; the negative's `hiddenCategories` - a declared result - comes back empty. Tonight both negatives came back **NEG NOT EMPTY** on `hiddenCategories`: the template hides 15 categories in *FloorPlan: Level 1* and 10 in *{3D}*. |
| **QQ4** | `report-level-elevations` (batch 2, [`night-rig-2-revit2024-2026-11-24.yaml`](../../tools/jobs/night-rig-2-revit2024-2026-11-24.yaml)): run it across two models, one of them with a survey point vertical offset - the arrangement NEEDS-CHECKING OI30 owes, and the same sitting as OI30. | **PASS** with the offset model's levels read at their survey elevation, not 0. Tonight's negative in *M_Desk* still read one level (*Ground floor*, 0 mm), so it was not empty. |

**Named here, not for this run** - each waits on something else, and none is a check of this group:

- `report-browser-organization` passed by tracking tonight, but waits on the owner's call recorded as OI19 in
  [Group OI](group-oi.md).
- `measure-distance` and `measure-available-fall` are blocked by issues
  [#1244](https://github.com/Ajmalpshaik/Heron-AI/issues/1244) and
  [#1308](https://github.com/Ajmalpshaik/Heron-AI/issues/1308).
- `set-family-formula` passed in batch-prove but `accept` refuses its set path - it waits on the fix of
  issue [#1322](https://github.com/Ajmalpshaik/Heron-AI/issues/1322) (row 5b-2009).

**Not checked when this was written:** whether a PASS here also answers the draft-version checks other groups hold
for the same cards - [Group OR](group-or.md) (`set-mep-justification`, `report-view-filters`,
`report-category-overrides`), [Group OU](group-ou.md) (`auto-size-mep`, `report-level-elevations`),
[Group PK](group-pk.md) and [Group PN](group-pn.md) (`report-category-overrides`,
`report-view-filters`). Read each before ticking it from this run.
