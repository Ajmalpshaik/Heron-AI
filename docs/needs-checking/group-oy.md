# Needs checking — Group OY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OY - EXTEND_ROOF_TO_ROOF, a roof by extrusion extended to meet another roof by rays: the owner's other way to Join/Unjoin Roof - DRAFT until proved (2026-11-20)

**Tracked as GitHub issue [#1144](https://github.com/Ajmalpshaik/Heron-AI/issues/1144) since 2026-11-20** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** row [5b-1877](../fragment-issues/row-5b-1877.md), GitHub issue
[#1087](https://github.com/Ajmalpshaik/Heron-AI/issues/1087), on the owner's answer in
[#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918): Revit's API has no Join/Unjoin Roof on any release, so
cast a ray from the end of one roof to the other roof's face, work out how far the first has to extend, and extend
it there - offered first, and run only on a yes. The new card
[`extend-roof-to-roof`](../../brain/fragments/extend-roof-to-roof/fragment.yaml) (`EXTEND_ROOF_TO_ROOF`, DRAFT
v1) does that for a **roof by extrusion**, whose extent is its Extrusion Start and End on every release. **Version 2**
(2026-11-21, issue [#1143](https://github.com/Ajmalpshaik/Heron-AI/issues/1143)) extends a **roof by footprint** from
Revit 2022 by moving its gable-end line in sketch edit mode, every line keeping its slope - NOT RUN; OY6 to OY8 are its
look - and on 2020 and 2021 refuses one, naming the release (row [5b-1930](../fragment-issues/row-5b-1930.md)). The `join-roofs` method offers it in its step 1. Written in a cloud
container with no Revit; it compiles on 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-20). **Version 1
ran on Revit 2024 on 2026-11-21 (the run below) and was signed PROVEN; version 2 has not run, so the card is DRAFT
again until OY2 to OY4 are run again on version 2 and OY6 to OY8 are run.** No add-in code changed: the card travels as source.

**The model is a scratch copy, never a working model** - *roofjoin-oy*, the arrangement in the card's
`tests/cases.yaml`, and the job file
[`tools/jobs/extend-roof-to-roof-oy-2026-11-20.yaml`](../../tools/jobs/extend-roof-to-roof-oy-2026-11-20.yaml).
Every run rolled back unless the row says kept. Any release; the one the owner's runs use first.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**OY1**~~ | *Build roofjoin-oy* - the arrangement in the card's `tests/cases.yaml`. From the default metric architectural template: Level 2 at 3000 mm; a roof by footprint on Level 2, *Generic - 400mm*, the rectangle 0,0 to 12000,8000 mm, south and north edges Defining Slope at 30 degrees, the ends upright; a reference plane *Wing* in Level 2 from 4000,-6000 to 8000,-6000 mm and on it a roof by extrusion, *Generic - 400mm*, profile *0,500; 2000,1655; 4000,500* (its eaves 500 mm above Level 2), Extrusion Start 0 and End 3000 mm (`CREATE_EXTRUSION_ROOF` makes it); a second the same on a plane *Far* from 20000,0 to 24000,0 mm; a copy of the main roof 30000 mm north. Save it under `%TEMP%\heron-work`. **Write down the wing's Extrusion Start and End as Properties shows them** | The wing runs NORTH from y = -6000 and stops about 3000 mm short of the main roof's south eave - if it runs south, set Start and End by hand so it runs north, and write down what they are; the far roof runs north beside the main roof, facing nothing; {3D} shows all four roofs |
| **OY2** | *Negative - THE FIRST CASE TO RUN.* The job file's negative: the wing and the main roof selected, view3d {3D}, `maxExtendMm=500` | `extended` 0; `refused` *"... has to extend <needMm> mm to meet roof by footprint ..., more than the 500 mm allowed. Nothing was changed"*; `needMm` more than 3000 and less than 7000; the wing's two values as written down in OY1 |
| **OY3** | *Positive.* The job file's positive, `maxExtendMm=10000`. Then, as a run of its own, the same pair with `maxExtendMm` left out (values `minMm=-500,-6500,2500 maxMm=12500,8500,9000 exact=false categories=Roofs view3d={3D}`), its output `brain/proof-drafts/runs/extend-roof-to-roof-oy3-measure.json` | `extended` 1, `extendedRoof` the wing's id, `report` *"EXTENDED: ... its end moved <needMm> mm toward roof by footprint ... read back off the roof's geometry, its other end where it was"*, and a finding that the flat end went further elsewhere - through the main roof and on under it - offering JOIN_GEOMETRY. In a section through x = 6000 the wing's ridge reaches the main roof's slope with no gap; in a section through x = 4500 the end passes through the slope and on under it, as the finding says; its south end has not moved. **Measure needMm against a dimension in the x = 6000 section.** Left out: `extended` 0, `refused` begins *"MEASURED, NOTHING CHANGED"* with the same distance |
| **OY4** | *Negatives - nothing faces, and two footprint roofs*, each a run of its own. (a) The main roof and the far roof: `minMm=-500,-500,2500 maxMm=24500,8500,9000 exact=false categories=Roofs view3d={3D} maxExtendMm=50000`, output `...-oy4a.json`. (b) The main roof and its copy: `minMm=-500,-500,2500 maxMm=12500,38500,9000 exact=false categories=Roofs view3d={3D} maxExtendMm=10000`, output `...-oy4b.json` | (a) `refused` begins *"NOTHING MET"*, the report lists both ends of the far roof with 0 points meeting. (b) on Revit 2020 or 2021, `refused` *"Both roofs are roofs by footprint, and Revit <release>'s API has no sketch editing for an add-in ... Modify > Join/Unjoin Roof"*, naming the release; from 2022 (version 2) `refused` begins *"NOTHING MET"* - neither roof's gable ends face the other. Nothing changed either time |
| **OY6** | *Version 2, a roof by footprint - Revit 2022 or later. THE LOOK ROW 5b-1930 ASKED FOR.* Add to roofjoin-oy a roof by footprint *Wing F*, *Generic - 400mm*, on Level 2, the rectangle 4000,11000 to 8000,17000 mm - its east and west edges Defining Slope at 30 degrees, its north and south edges upright, so its south gable end faces the main roof's north slope 3000 mm away. Write down each of its four lines' Defines Slope and Slope. Run: Wing F and the main roof, view3d {3D}, `maxExtendMm=10000`, output `brain/proof-drafts/runs/extend-roof-to-roof-oy6.json` | `extended` 1, `report` *"EXTENDED: roof by footprint ...'s end line <id> moved <needMm> mm out, with the two lines beside it lengthened to meet it ... every footprint line kept its slope"*. In Revit, Edit Footprint shows the south line moved north by needMm and the east and west lines lengthened to it, each line's Defines Slope and Slope as written down; a section through x = 6000 shows Wing F's ridge reaching the main roof's slope. **If Revit refuses the sketch edit, or the read-back throws, write down Revit's words word for word** - that is the answer row 5b-1930 owes |
| **OY7** | *Version 2, negative - Revit 2022 or later.* Wing F and the main roof, `maxExtendMm=500`; then Wing F's four edges all Defining Slope (a hip), `maxExtendMm=10000` | First: `refused` *"... has to extend <needMm> mm ..., more than the 500 mm allowed. Nothing was changed"*, Wing F's sketch as it was. Second: `refused` begins *"NOTHING MET ... (a roof by footprint is measured from its gable ends - lines that define no slope)"* - a hip has no gable end |
| **OY8** | *Version 2 on Revit 2020 or 2021.* Wing F and the main roof, `maxExtendMm=10000` | `refused` *"Both roofs are roofs by footprint, and Revit <release>'s API has no sketch editing for an add-in (SketchEditScope came in 2022), so this tool cannot extend a roof by footprint on this release ... Modify > Join/Unjoin Roof ... Nothing was changed"*, naming the release - the same refusal as OY4(b), because both roofs are roofs by footprint. Nothing changed. *(Corrected 2026-11-21: this said `refused` begins "NOTHING MET" with the words in `findings`, but version 2's code refuses a pair of footprint roofs on 2020 and 2021 before any ray is cast - `impl/any/fragment.cs`, the `!footprintsEditable` branch.)* |
| **OY5** | *The second route.* On a scratch copy KEPT once with OY3's positive, then on another copy as saved before it, Modify > Join/Unjoin Roof by hand: the wing's north end edge, then the main roof's south slope | Write down the wing's Extrusion End (or Start) both ways. Revit's own join cuts the end to the slope; the card's flat end reaches the farthest point - say whether the difference is the card's nearest-to-farthest spread |

### The 2026-11-21 run - Revit 2024 (build 24.3.60.12), add-in 0.2.0+c9af86d1

*roofjoin-oy* was built through Heron on Revit 2024, saved as `%TEMP%\heron-work\runs-1121\roofjoin-oy.rvt`:
`CREATE_FAMILY_DOCUMENT` from `DefaultMetric.rte` (it ran with no transaction group open in the model in front, D-192);
Level 2's Elevation 4000 -> 3000 mm by `WRITE_ELEMENT_PARAMETERS`; the main roof by `CREATE_FLOOR` (DRAFT v4, its one
kept run, D-119) and `SLOPE_ROOF_EDGES` gable at 30 degrees - edges 2 and 4, 12000 mm, read back; its copy 30000 mm
north by `COPY_ELEMENTS`; a grid *Wing* from 4000,-6000 to 8000,-6000 by `CREATE_GRIDS`, and the wing by
`CREATE_EXTRUSION_ROOF` (DRAFT v2, its one kept run) on the reference plane it drew along that grid. **The far roof is
a COPY of the wing moved 16000,6000,0** - x 20000 to 24000, y 0 to 3000 - not a second roof on a plane *Far*: the same
place, facing nothing. Boxes read back by `REPORT_BOUNDING_BOX`: main roof (0,0,3000) to (12000,8000,5771) mm; its copy
y 30000 to 38000; the wing 4000 x 3000 x 1617 mm from (4000,-6000,3038); the far roof from (20000,0,3038). The wing's
Extrusion Start 0 and End 3000 mm as `CREATE_EXTRUSION_ROOF` read them back.

**One word of OY1 does not hold, and nothing below depended on it:** Revit takes the extrusion profile as the roof's
TOP, so the wing's eaves underside is at 3038 mm - 38 mm above Level 2, not 500 mm above it.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| OY1 | 2024 | Built as above | **done** - the far roof by copy |
| OY2 | 2024 | `batch-prove` negative, run first, `maxExtendMm=500`: `extended` 0, `refused` *"roof by extrusion 352563 'Generic - 400mm' has to extend 5067 mm to meet roof by footprint 352517 'Generic - 400mm', more than the 500 mm allowed. Nothing was changed"*, rolled back | **PASS** |
| OY3 | 2024 | Positive, `maxExtendMm=10000`: `extended` 1, `extendedRoof` 352563, *"EXTENDED: ... Extrusion End is 8067 mm (was 3000 mm), and its end moved 5067 mm toward roof by footprint 352517 ... read back off the roof's geometry, its other end where it was"*; report *"4 of 6 point(s) of its end meet the upper face ..., nearest 3066 mm, farthest 5067 mm away"*, the other end 0 of 6; a finding that the flat end went to its FARTHEST point. `needMm` 5067 lies between 3000 and 7000 and equals the estimate made before the run: the 3000 mm gap plus the main roof's slope climbing from its eave top (3000 + 400/cos 30 = 3462 mm) to the wing's ridge top (4655 mm), (4655 - 3462) / tan 30 = 2067 mm. Measure-only run, `maxExtendMm` left out: *"MEASURED, NOTHING CHANGED: ... has to extend 5067 mm for its end to meet roof by footprint 352517 ... (its nearest point is 3066 mm away). To extend it, ask again with maxExtendMm at least 5067"*. Every run rolled back, so the sections through x = 6000 and 4500 were not looked at | **PASS** - the two sections are the owner's look, with OY5 |
| OY4 | 2024 | (a) the far roof and the main roof, `maxExtendMm=50000`: *"NOTHING MET: no ray from either end of a roof by extrusion met the upper face of the other roof in the 3D view '{3D}' ... Nothing was changed"*, both ends of roof 352582 0 of 6. (b) the main roof and its copy: *"Both roofs are roofs by footprint, and this tool does not extend a roof by footprint on Revit 2024 ... Modify > Join/Unjoin Roof ... Nothing was changed"* | **PASS** |
| OY5 | - | Needs the owner's hands | **NOT RUN** |

The proof draft was `brain/proof-drafts/extend-roof-to-roof.yaml` on the owner's PC. **Signed the same day:** Ajmal PS
signed it on 2026-11-21 (pull request #1178), and version 1 was PROVEN. Then pull request #1168 merged version 2, so
the card is DRAFT again and that proof is the record of version 1 only - `python tools/check-signatures.py` lists it
as STALE. Version 2 needs OY2 to OY4 run again and OY6 to OY8 run, then a new signature (written 2026-11-21 by the
review of that day's merges).
