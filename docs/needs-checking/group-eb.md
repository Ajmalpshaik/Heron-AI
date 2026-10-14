# Needs checking — Group EB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-14 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EB - a wall's Base Offset read beside its level: `READ_ELEMENT_LEVEL` version 2 (2026-10-14)

Found 2026-10-14 by reading the code while fixing [row 5b-468](../fragment-issues/section-5b-rows-401-425.md),
and fixed the same day on the owner's word. Version 1 of
[`READ_ELEMENT_LEVEL`](../../brain/fragments/read-element-level/fragment.yaml) (FRG-ELE-011) read the offset
from `RBS_OFFSET_PARAM`, `INSTANCE_FREE_HOST_OFFSET_PARAM` and `INSTANCE_ELEVATION_PARAM`. A wall carries none
of them - its offset is `WALL_BASE_OFFSET`, "Base Offset" - so every wall came back with its level right and
0 mm beside it. Its proof ran on 28 duct taps and 12 sheets, never on a wall.

**What changed.** Version 2 asks `WALL_BASE_OFFSET` too, **last**, so it is reached only when none of the
first three answered: no element version 1 read an offset from can read a different one now. Nothing else in
`impl/` moved. The card is **DRAFT** - the change made version 1's proof stale, and `check-signatures` lists it
under STALE. Its purpose text is unchanged, so the search reads the same words; only the PROVEN tie-break is
lost, and row 5b-468 has what that moved.

**Read from the code, not yet run in Revit.** The new parameter is the one `SET_WALL_CONSTRAINTS` (PROVEN)
already reads and CI compiles for every release 2020 to 2027.

| # | Run | Look for |
|---|---|---|
| **EB1** | Revit 2020 (any release will do), a new project "Heron Wall Offset Test" made as the job's header says - three walls on Level 1 with Base Offset 0, 300 and -150 mm, two grids, all in "FloorPlan: Level 1": `python tools/batch-prove.py tools/jobs/read-element-level-wall-offset-2026-10-14.yaml`, then read [`brain/proof-drafts/read-element-level.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept read-element-level --by "Ajmal PS"` and set `heron-status: PROVEN` | positive: `levelName` "Level 1" for all three walls and `offsetMm` 0, 300 and -150, each equal to that wall's Base Offset in the Properties palette - **a 0 for the 300 or the -150 wall is a FAIL whatever the verdict says**; negative: both grids in `noLevel`, nothing in `levelName` |
| **EB2** | Any model with ducts or pipes on an offset, the same release | version 1's own case still holds: a duct's `offsetMm` equal to its offset in the Properties palette, as before |
