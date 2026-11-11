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

**Repaired 2026-11-11, still DRAFT:** `DIVIDE_PARTS` version 2 gives the division a sketch plane
([row 5b-1418](../fragment-issues/section-5b-rows-401-425.md)): Revit 2024 refused version 1's empty one, by a
level and by a grid alike. The plane is horizontal, through the origin, with nothing drawn on it - the row has
the evidence for that choice. Rows **DQ3a** to **DQ3d**, with
[`three-draft-repairs-2026-11-11.yaml`](../../tools/jobs/three-draft-repairs-2026-11-11.yaml). Every member was
read with `tools/api-surface` on all eight releases and the card compiles on each; **nothing of version 2 has run
in Revit** - that Revit takes the plane is what DQ3a measures, and that a wall skew to a grid is cut on the grid
line is what DQ3d shows. `MERGE_PARTS` waits behind it, as before.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DQ1** | Arrange on **Revit 2024**: the two scratch projects the header of [`gap-fill-batch-7-2026-10-12.yaml`](../../tools/jobs/gap-fill-batch-7-2026-10-12.yaml) describes | the columns, the two beams, the stair, the three-layer wall, the area plan, the toposolid, Concrete with its Carbon Factor, the CAD folder |
| **DQ2** | `python tools/batch-prove.py tools/jobs/gap-fill-batch-7-2026-10-12.yaml`, both titles and the place-areas points filled | each positive answers and each negative is empty or refused as the job's notes say. Look at the model afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DQ3** | create-parts kept, then divide-parts at a grid, merge-parts on two pieces, set-parts-excluded on one, offset-part-faces top 20 mm | each step read back, in a section with Show Parts |
| **DQ3a** | *`DIVIDE_PARTS` version 2 by a level, Revit 2024 - its D-30 pair.* The scratch files the header of [`three-draft-repairs-2026-11-11.yaml`](../../tools/jobs/three-draft-repairs-2026-11-11.yaml) describes, then `python tools/batch-prove.py tools/jobs/three-draft-repairs-2026-11-11.yaml` | divide-parts `PASS`. Positive: `divided` equal to the parts handed in, `partCountAfter` above them, no *Revit refused the division* in `report`, and `findings` saying *one unnamed horizontal work plane was added with it*. Negative (*ZZZ No Such Level*): refused naming it, `divided` 0. **If Revit refuses again its words go on [row 5b-1418](../fragment-issues/section-5b-rows-401-425.md) verbatim** - the sketch plane is horizontal, through the origin, and whether Revit takes it is what this row measures |
| **DQ3b** | *By a grid, rolled back.* The same project: `python mcp/client/heron_bridge_client.py validate --in "Heron Repairs Test" --out <a scratch file> --write --setup filter-elements-by-category --keep-chain --set "cutterNames=HERON CUT" --negative-set "cutterNames=ZZZ No Such Grid" --setup-set category=Parts --negative-setup-set category=Parts divide-parts < /dev/null` | Positive: no refusal, `partCountAfter` above the parts handed in. `divided` reads every part handed in although the grid crosses three walls of five - that is [row 5b-1515](../fragment-issues/section-5b-rows-401-425.md), recorded and not fixed. Negative: refused naming the grid. `--out` keeps DQ3a's run record, the one a proof draft is made from, as it was. **A rolled-back run cannot show WHERE the skew wall was cut** - that is DQ3d |
| **DQ3c** | *A datum that crosses nothing, rolled back - a measurement, and the refusal's read-back.* The same command with `cutterNames=HERON HIGH`, the level kept above every wall | **What Revit answers is not known and is recorded as it comes** on [row 5b-1515](../fragment-issues/section-5b-rows-401-425.md): *Revit divided none of these parts*, Revit's own refusal quoted, or `divided` above 0 beside a `partCountAfter` that did not move - the last is the defect that row describes. **If it is refused: the refusal ends *NOTHING WAS DIVIDED.* and does NOT go on to say the work plane *could not be taken back and is still in the model*** - the card's own look for the plane it made. Revit lists no unnamed work planes, so there is no second route for that |
| **DQ3d** | *The skew wall, KEPT once (D-119), on the owner's yes only.* The project saved first; a plan of the first level in front with Parts Visibility *Show Parts*; every part selected: `python mcp/client/heron_bridge_client.py try-once --in "Heron Repairs Test" divide-parts --set "cutterNames=HERON CUT"` - Keep or Discard is his | In the plan: **on the SKEW wall the new seam lies ON the grid line** - along the grid, across the wall at the angle the grid crosses it - as on the two square walls; the two walls the grid misses are whole. A seam square to the skew wall, or off the grid line, means Revit read the sketch plane after all: a defect, written on [row 5b-1418](../fragment-issues/section-5b-rows-401-425.md) with what was seen. One Ctrl+Z takes back the division and its work plane together |
| **DQ4** | `CREATE_OPENING` on the Level 2 beam: a 300 x 200 mm rectangle on its web face, perpendicularFace true | an opening through the web, seen in a section; then false on its top face cuts through the flange |
| **DQ5** | A family in front: `IMPORT_CAD` of a plan DWG into "Ref. Level", thisViewOnly true; then `SET_FAMILY_PREVIEW_VIEW` "View 1" and a save | the drawing under the reference planes in that view only; the family browser shows View 1 as its picture |
| **DQ6** | create-toposolid kept on **Revit 2024**, then add-toposolid-subdivision | a sloping terrain with a hump, then a subdivision following it |
| **DQ7** | report-analytical-model on **Revit 2023** and 2027 | the columns' and beams' analytical members, each named to its physical element |
| **DQ8** | report-embodied-carbon by hand on the floor, groupBy type | 300 x the floor's concrete volume, read against a material takeoff schedule |
| **DQ9** | `READ_ROOM_GEOMETRY` with an area's name instead of a room's | its boundary and area - the read the plan says needs no new card |
| **DQ10** | The rest on **Revit 2020** and **2027** | the same answers where the card declares the release. Compiled on every declared release; what Revit DOES was measured on none |
| **DQ11** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each |
