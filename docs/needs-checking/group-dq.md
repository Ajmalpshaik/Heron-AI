# Needs checking — Group DQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DQ - structural coping, beam systems and supports, analytical model, areas, railings, parts, toposolids, carbon, CAD import, library search, family preview, geometry, beam openings (2026-10-12)

The seventh batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)).

**What was built - nineteen new capabilities and one widening.** Structural: `COPE_FRAMING`, `CREATE_BEAM_SYSTEM`,
`REPORT_ANALYTICAL_MODEL` (Revit 2023 on) (FRG-ELE-087 to 089), `CHECK_STRUCTURAL_SUPPORTS` (FRG-QA-036) - a
geometric check by bounding box, never a structural analysis. Areas: `PLACE_AREAS`, `CREATE_AREA_BOUNDARY_LINES`
(FRG-GEO-064, 065); an area's own boundary is read by `READ_ROOM_GEOMETRY`, PROVEN, which takes any room, space
or area - row DQ9 checks it on an area. `CREATE_RAILING` (FRG-ELE-090). Parts: `CREATE_PARTS`, `DIVIDE_PARTS`,
`MERGE_PARTS`, `SET_PARTS_EXCLUDED`, `OFFSET_PART_FACES` (FRG-ELE-091 to 095). Site (Revit 2024 on):
`CREATE_TOPOSOLID`, `ADD_TOPOSOLID_SUBDIVISION` (FRG-GEO-066, 067). `REPORT_ELEMENT_GEOMETRY` (FRG-GEO-068).
`REPORT_EMBODIED_CARBON` (FRG-PAR-034): the factor is the modeller's, a Number parameter on the material.
`IMPORT_CAD` (FRG-DOC-046), the family underlay among its uses. `FIND_LIBRARY_FAMILIES` (FRG-DOC-047).
`SET_FAMILY_PREVIEW_VIEW` (FRG-DOC-048). **`CREATE_OPENING` version 2** (DRAFT before and after): a beam, brace
or column takes Revit's framing opening, through the web or the flanges, found by reflection because its face
type lives in a namespace the fragment contract does not supply.

**Not built, and recorded in the plan:** a ramp (the API has no ramp creation), grading, floors and walls
fitted to terrain, editing toposolid points, railing type set-up and railing path edits, copying elements
between two families.

**Measured without Revit, 2026-10-12.** Every Revit API member was read off `RevitAPI.xml` for all eight
releases; each fragment compiles on every release it declares (all eight run), and the job file reads
`14 WOULD RUN` under `batch-prove --dry-run`. **Nothing below has run in Revit.**

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DQ1** | Arrange on **Revit 2024**: the two scratch projects the header of [`gap-fill-batch-7-2026-10-12.yaml`](../../tools/jobs/gap-fill-batch-7-2026-10-12.yaml) describes | the columns, the two beams, the stair, the three-layer wall, the area plan, the toposolid, Concrete with its Carbon Factor, the CAD folder |
| **DQ2** | `python tools/batch-prove.py tools/jobs/gap-fill-batch-7-2026-10-12.yaml`, both titles and the place-areas points filled | each positive answers and each negative is empty or refused as the job's notes say. Look at the model afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DQ3** | create-parts kept, then divide-parts at a grid, merge-parts on two pieces, set-parts-excluded on one, offset-part-faces top 20 mm | each step read back, in a section with Show Parts |
| **DQ4** | `CREATE_OPENING` on the Level 2 beam: a 300 x 200 mm rectangle on its web face, perpendicularFace true | an opening through the web, seen in a section; then false on its top face cuts through the flange |
| **DQ5** | A family in front: `IMPORT_CAD` of a plan DWG into "Ref. Level", thisViewOnly true; then `SET_FAMILY_PREVIEW_VIEW` "View 1" and a save | the drawing under the reference planes in that view only; the family browser shows View 1 as its picture |
| **DQ6** | create-toposolid kept on **Revit 2024**, then add-toposolid-subdivision | a sloping terrain with a hump, then a subdivision following it |
| **DQ7** | report-analytical-model on **Revit 2023** and 2027 | the columns' and beams' analytical members, each named to its physical element |
| **DQ8** | report-embodied-carbon by hand on the floor, groupBy type | 300 x the floor's concrete volume, read against a material takeoff schedule |
| **DQ9** | `READ_ROOM_GEOMETRY` with an area's name instead of a room's | its boundary and area - the read the plan says needs no new card |
| **DQ10** | The rest on **Revit 2020** and **2027** | the same answers where the card declares the release. Compiled on every declared release; what Revit DOES was measured on none |
| **DQ11** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each |
