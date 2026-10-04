# Needs checking — Group CD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CD - what the five downloaded skill files left behind: nine fragments and a recipe (2026-10-04)

**Built 2026-10-04 from the study in [docs/45](../45-five-more-downloaded-skill-files.md). Every one was
DRAFT before it was touched, is DRAFT now, and compiles on 2020 to 2027. Nothing below has run in
Revit.** Three of them repair rows [5b-315 to 5b-317](../fragment-issues/section-5b-rows-176-200.md).
The arrangements, inputs and expected answers are in four job files, each read back with
`python tools/batch-prove.py <file> --dry-run`:
[`study-45-import-and-shared-params`](../../tools/jobs/study-45-import-and-shared-params.yaml),
[`study-45-sill-and-column-top`](../../tools/jobs/study-45-sill-and-column-top.yaml),
[`study-45-coordinates-and-storeys`](../../tools/jobs/study-45-coordinates-and-storeys.yaml) and
[`study-45-standards-and-mirrored`](../../tools/jobs/study-45-standards-and-mirrored.yaml).
**Every writing run is on a scratch or test copy and rolled back** (`validate --write --setup`); the
reads run with `validate` and no `--write`.

| # | Run | Look for |
|---|---|---|
| **CD1** | **IMPORT_PARAMETER_VALUES v3, the Yes/No round trip** - a test copy of Project1 "heron ai bulding", its 15 fan coil units: EXPORT_PARAMETERS_TO_CSV with `Show_Clearance`, every Yes/No flipped in the file, one blank `Comments` cell on a unit whose Comments reads `keep me`, `blankCells=skip` | 15 written, each tick box read back flipped; that unit's Comments still `keep me` and counted in `blankCellsSkipped`. **First look at what the export wrote for the tick box** - row 5b-315 assumes `Yes`/`No` and that is itself unmeasured |
| **CD2** | **The same, the D-30 negative** - every Yes/No cell `Maybe`, every text cell blank, `blankCells=skip` | 0 written; each `Maybe` named. Then once with `blankCells=clear` (Comments cleared, numbers refused with the reason) and once with `blankCells` empty (the run refused, nothing written) |
| **CD3** | **ADD_PROJECT_PARAMETER v2** - Manage > Shared Parameters pointed at a file that is NOT the run's; `HERON_CC_TAG` on Ducts; then a category that cannot take a bound parameter (the job says Cameras - **check it in Manage > Project Parameters first**, it is an assumption) | Ducts bound, read back; Manage > Shared Parameters **still points at the modeller's own file afterwards**, read by hand, and `sharedParameterFileRestored` true. The negative names the category as not bound. **One run starts with the setting empty**: whether Revit takes an empty path back is not known. **ADMIN risk** - it waits for the owner's Admin switch |
| **CD4** | **TRANSFER_PROJECT_PARAMETERS_BETWEEN_DOCUMENTS v2** - a second test copy as the source, holding `HERON_CC_TAG`; `nameContains=HERON_CC`, then `ZZZ NO SUCH PARAM` | The parameter transferred and the shared parameter file setting put back, read by hand; the negative transfers nothing. ADMIN, as CD3 |
| **CD5** | **PLACE_HOSTED_FAMILY v3, the sill** - scratch `Heron-CC-scratch`: Level 1 at 0, Level 2 at 4000, one Generic 200 mm wall (0,0)-(6000,0) 3000 mm high with a room each side, `M_Fixed 0915 x 1220mm` loaded; windows at x 1500 and 4500, `sillHeight=900` | `sills` reads 900 twice, Properties reads 900, the wall's Volume is down by two openings. **Negative:** `sillHeight=2500` (head 3720 over a 3000 wall) - both in `sillTooHigh`, nothing placed, Volume unchanged. **By hand:** sill blank, and `sills` says what sill Revit gave - this settles whether a window placed this way lands at 0 |
| **CD6** | **PLACE_STRUCTURAL_FAMILY v2, the column top** - the same scratch, one structural column type; two columns, `topLevelName=Level 2`, `topOffset=-150` | `tops` and Properties read 3850 mm. **Negative:** `topLevelName=Level 1` refused before anything is placed, no new column. Blank values give version 1's behaviour |
| **CD7** | **REPORT_LOCATION v3, shared coordinates** - Snowdon-scratch_ajmal.al, mechanical equipment in `FloorPlan: L3`, `sharedCoordinates` on | Shared elevation differs from internal by the survey offset (780.5 mm in this tool's 2026-09-13 proof); the angle to true north and both base points reported. A Spot Coordinate set to Survey Point, by hand, agrees. **Negative:** Project1 air terminals - shared equals internal, angle 0. **Check first** that Project1's survey point has not moved since it was measured empty. Switch off: the internal numbers identical to a switch-on run |
| **CD8** | **REPORT_LEVEL_ELEVATIONS v3, Building Story** - Snowdon-scratch, Building Story unticked by hand on one level that holds ducts | That level named with its count, ducts included; SELECT_BY_LEVEL on it agrees. **Negative:** Project1, every level ticked - the answer says so and names the levels, never a bare 0 |
| **CD9** | **The `ifc-export-readiness` recipe's two unknowns**, measured on a scratch copy before the recipe leaves DRAFT | (a) Export to IFC one wall on an unticked level and read which storey it is filed under (the file is text: `IFCRELCONTAINEDINSPATIALSTRUCTURE`); (b) on 2020 and 2024, the names Revit gives its IFC class, export and identity parameters, and whether a model has them before IFC parameters are added. A step that reads a parameter the model lacks must answer NOT CHECKED |
| **CD10** | **CHECK_MODEL_STANDARDS v2** - Snowdon-scratch (check its duct types do not already start with TRG); `requiredOn` Ducts, `requiredParameters` Family and Type, `namePatterns` all `*` except `type=*: TRG*` | Duct types reported under TYPE NAMES. **Negative:** `type=*` - no type failures. **By hand, on a test copy:** `placeholder=TBD` with Comments set to TBD on one element (counted blank), `value:Mark=` a pattern one Mark breaks (counted apart from blank), a curtain wall type for NOT READ (row 5b-203) |
| **CD11** | **REPORT_MIRRORED_INSTANCES (new, FRG-QA-034)** - a test copy of Project1 work_ajmal.al: mirror one door with MIRROR_ELEMENTS, hand-flip a second | The mirrored door under MIRRORED; the flipped one under hand-flipped ONLY - **a flipped door is not a mirrored door**. **Negative:** Air Terminals, after checking none is mirrored - nothing listed, said in words. **Needs a store row** to run before it merges ([the put_fragment recipe](../../brain/heron_scope.py)) |
| **CD12** | **The question routes to the read, not the write** - after merge and a restart, `heron_lookup` with "is this door mirrored", "is this fixture mirrored" | Both resolve to REPORT_MIRRORED_INSTANCES. Measured on a scratch store 2026-10-04 before the PR; "is this door mirrored" resolved to MIRROR_ELEMENTS, which changes the model, until this group's card took it |

**Then sign** each fragment on its positive and negative. CD9 is what lets the recipe leave DRAFT.
