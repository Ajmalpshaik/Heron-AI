# Needs checking — Group DR

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DR - dimensions through faces, slab and terrain shape, toposolid tools, railing edits and types, divided surfaces, adaptive placement, copying between documents, purge (2026-10-13)

The eighth batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)),
the last of its missing jobs.

**What was built - nine new capabilities and two widenings.** `DIMENSION_ELEMENT_FACES` (FRG-VIEW-135): one
string per element through every face that faces one way - a wall's or slab's layers, the sills and heads in an
elevation, a curtain wall's grid lines. `EDIT_SLAB_SHAPE` (FRG-GEO-069): points, split lines, point heights, reset
and "follow" - a floor laid to the top of the terrain - on floors, footprint roofs and toposolids, on all eight
releases by finding the renamed calls by name. `EDIT_TOPOSOLID` (FRG-GEO-072, 2024 on): excavate by a floor or pad
and take it away again (2025 on), split, simplify, Smooth Surface (2025 on). `REPORT_DIVIDED_SURFACES` (FRG-GEO-070)
and `PLACE_ADAPTIVE_COMPONENTS` (FRG-GEO-071): divided surfaces and paths read, adaptive families placed on points
or on nodes. `EDIT_RAILING`, `SET_RAILING_TYPE` (FRG-ELE-096, 097). `COPY_ELEMENTS_BETWEEN_DOCUMENTS`
(FRG-ELE-098): another open project or family into the one in front. `FIND_PURGEABLE_ELEMENTS` (FRG-SEL-037, 2024
on): Revit's own unused-element list, handed on to `DELETE_ELEMENTS`. **Widened, both DRAFT and not yet on main:**
`CREATE_TOPOSOLID` reads a CAD survey's points (`cadImport`, `cadLayers`) and takes an empty boundary;
`ATTACH_WALLS` attaches a wall's base to a toposolid.

**Not built, and recorded in the plan:** grading as Revit's graded region (no API; excavation is the nearest);
parts visibility in a view waits for the owner's yes to widen PROVEN `SET_VIEW_PROPERTIES`.

**Widened 2026-10-29, still DRAFT:** `REPORT_DIVIDED_SURFACES` version 2 reads each divided surface's all grid
rotation, component rotation, flip and mirror, both pattern indents, and the levels, reference planes or other
elements that cut it - what [`SET_DIVIDED_SURFACE_LAYOUT`](group-jl.md) writes, in the words its readBack uses.
Rows **DR5a** and **DR5b**, on Group JL's rig. Each member was read with `tools/api-surface` on all eight
releases and the card compiles on each; nothing of version 2 has run in Revit. By hand, like DR5: the card's one
value, withNodes, cannot make its negative, so it has no job file.

**Measured without Revit, 2026-10-13.** Every Revit API member was read off `RevitAPI.xml` for all eight
releases; each fragment compiles on every release it declares (all eight run), the author gate passes for each,
and the job file reads `8 WOULD RUN` under `batch-prove --dry-run`. **Nothing below has run in Revit.**

**Repaired 2026-11-11, both still DRAFT:** `SET_RAILING_TYPE` version 2 reads the primary handrail only when
the type has one ([row 5b-1415](../fragment-issues/section-5b-rows-401-425.md)) - rows **DR6a** to **DR6c**.
`COPY_ELEMENTS_BETWEEN_DOCUMENTS` version 4 refuses words that find more than one open document
([rows 5b-1123 and 5b-1516](../fragment-issues/section-5b-rows-401-425.md)) - rows **DR10a** to **DR10c**. Both with
[`three-draft-repairs-2026-11-11.yaml`](../../tools/jobs/three-draft-repairs-2026-11-11.yaml). Each card compiles
on every release it declares; **nothing of either version has run in Revit.** Revit's title for a document
differs by release - without the file extension from 2022, with it on 2020 and 2021 where the user's settings
show extensions - so type titles as the title bar shows them, and read DR10a's two outcomes.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DR1** | Arrange on **Revit 2024**: the two scratch projects the header of [`gap-fill-batch-8-2026-10-13.yaml`](../../tools/jobs/gap-fill-batch-8-2026-10-13.yaml) describes, both open | the three-layer wall with a window, the floor, the railing, the curtain wall, the toposolid, the adaptive panel loaded, the unused wall type and door; three generic models in the source |
| **DR2** | `python tools/batch-prove.py tools/jobs/gap-fill-batch-8-2026-10-13.yaml`, both titles and the adaptive type filled | each positive answers and each negative is empty or refused as the job's notes say. Look at the model afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DR3** | `DIMENSION_ELEMENT_FACES` by hand: the window wall in an elevation, vertical; the curtain wall in a plan, along; a floor in a section, across | base, sill, head, top; the grid spacing; the floor's layers - **and whether a compound wall's layers come as separate faces**, which only this run can say |
| **DR4** | `EDIT_SLAB_SHAPE` follow: a floor beside the sloping toposolid, shapeMm its id and 1000 | most points within 5 mm of the terrain, the rest named flat; then reset puts it back |
| **DR5** | A mass family with one face divided 10 x 6 and a line divided into 8: `REPORT_DIVIDED_SURFACES` withNodes true, then `PLACE_ADAPTIVE_COMPONENTS` of a two-point family on the path's nodes | the grids and node positions read; seven beams node to node - **and whether the path's geometry gives its nodes as points**, which decides the onNodesOf route |
| **DR5a** | *Version 2: a surface's own settings and its cuts.* [Group JL](group-jl.md)'s rig after JL1 was **kept** - its one divided surface flipped, all grid rotation 45, border tiles Empty, cut by *Level 2* and *Level 3*: `REPORT_DIVIDED_SURFACES`, withNodes false, read only. Then the surface selected, by eye | the surface's line reads *border tiles Empty, all grid rotation 45 degrees*, *component rotation 0 degrees*, *component flip yes*, *component mirror no*, a U and a V indent, and *cut by level Level 2, level Level 3* - JL1's `readBack` words; Properties and Intersects on the ribbon show the same. **Whether the component settings and indents read on a surface with no tile pattern is not known**: a *not read (...)* is recorded with Revit's reason, never taken as a no. Where the two questions are routed is [JL12](group-jl.md)'s |
| **DR5b** | *Negative.* The same rig with the box's **back** face divided too (JL7's arrangement) after JL1 - that surface never flipped, rotated or cut; *Level 1* never added as a cut to either | two surface lines. The back one reads *all grid rotation 0 degrees*, *component flip no* and *cut by none* - nothing of the front surface's carried onto it - and the front one as in DR5a; *Level 1* named on neither line. The family unchanged afterwards |
| **DR6** | `EDIT_RAILING` with a new path and host none; `SET_RAILING_TYPE` handrailHeightMm on a type whose handrail type another railing type shares | the new path's length; the report naming the other railing type that changed with it |
| **DR6a** | *`SET_RAILING_TYPE` version 2 on a type with no handrail, Revit 2024 - its D-30 pair.* A scratch copy of *Snowdon Towers Sample Architectural* open: `python tools/batch-prove.py tools/jobs/three-draft-repairs-2026-11-11.yaml` | set-railing-type `PASS`. Positive: `railingType` a type, and `report` reading *Copied "Handrail - Pipe" as "Heron Guard"; top rail ... at 1000 mm, handrail none, read back as asked* - never *The rail has no primary hand rail*. Negative (*ZZZ No Such Railing*): refused, naming the railing types there are. Afterwards no *Heron Guard* type is left |
| **DR6b** | *A type that HAS a primary handrail still reads it, rolled back.* The same model, a railing type whose Type Properties show a Handrail 1 Type other than None: `python mcp/client/heron_bridge_client.py validate --in "Snowdon Towers Sample Architectural" --out <a scratch file> --write --set "duplicateAs=Heron Guard" --set handrailHeightMm=0 --set handrailSide= --set handrailTypeName= --set "railingTypeName=<that type>" --set topRailHeightMm=1000 --set topRailTypeName= --negative-set "duplicateAs=Heron Guard" --negative-set handrailHeightMm=0 --negative-set handrailSide= --negative-set handrailTypeName= --negative-set "railingTypeName=ZZZ No Such Railing" --negative-set topRailHeightMm=1000 --negative-set topRailTypeName= set-railing-type < /dev/null` | `report` names the handrail type, its side and its height in mm, as version 1 did - the read the repair must not have lost - and ends *read back as asked* |
| **DR6c** | *`handrailSide none` on a type with no handrail, rolled back - a measurement.* `python mcp/client/heron_bridge_client.py validate --in "Snowdon Towers Sample Architectural" --out <a scratch file> --write --set "duplicateAs=Heron Guard" --set handrailHeightMm=0 --set handrailSide=none --set handrailTypeName= --set "railingTypeName=Handrail - Pipe" --set topRailHeightMm=1000 --set topRailTypeName= --negative-set "duplicateAs=Heron Guard" --negative-set handrailHeightMm=0 --negative-set handrailSide=none --negative-set handrailTypeName= --negative-set "railingTypeName=ZZZ No Such Railing" --negative-set topRailHeightMm=1000 --negative-set topRailTypeName= set-railing-type < /dev/null` | **What Revit answers is not known and is recorded as it comes** on [row 5b-1415](../fragment-issues/section-5b-rows-401-425.md): RevitAPI.xml documents *The rail has no primary hand rail* on setting the side, so a refusal in those words is expected although the type already has none; *handrail none, read back as asked* would mean Revit took it |
| **DR7** | `CREATE_TOPOSOLID` with an empty boundary and `cadImport` a survey DWG holding 3D polylines, `cadLayers` its levels layer | a toposolid through the drawing's heights, the outline drawn by Revit round them |
| **DR8** | **Revit 2025**: `ATTACH_WALLS` base on walls standing on the sloping toposolid | each base attached to the toposolid and following it - or Revit's refusal named |
| **DR9** | **Revit 2025**: `EDIT_TOPOSOLID` excavate by a floor sunk into the terrain, then unexcavate; split by one loop; smooth on | the cut seen in a section and gone again; two toposolids; the setting read back |
| **DR10** | A family in front and another family open: `COPY_ELEMENTS_BETWEEN_DOCUMENTS` categories all; then `FIND_PURGEABLE_ELEMENTS` in that family | its forms arrive in the family in front; the purge list matches Manage > Purge Unused there |
| **DR10a** | *`COPY_ELEMENTS_BETWEEN_DOCUMENTS` version 4, Revit 2024 - its D-30 pair, the negative the new refusal.* The scratch files the header of [`three-draft-repairs-2026-11-11.yaml`](../../tools/jobs/three-draft-repairs-2026-11-11.yaml) describes, *Heron Twin.rvt*, *Heron Twin.rfa* and *Heron Twin 2.rvt* all open: `python tools/batch-prove.py tools/jobs/three-draft-repairs-2026-11-11.yaml` | copy-elements-between-documents `PASS`. Positive (*Heron Copy Source*): `copied` 3, read back, Generic Models 3. Negative (*Heron Twin*): `copied` 0 and `report` reading *2 open documents match "Heron Twin"* - the two titled *Heron Twin*, each with its path, the .rvt and the .rfa, *Heron Twin 2* not named - then *Some of them share a title ... close the ones you do not mean* and *NOTHING WAS COPIED*. **On Revit 2020 or 2021 showing file extensions the same run reads *3 open documents match*, each title with its extension, and *Give the whole title of the one you mean*** - also a pass, and written down as seen. **Read the report**: *No other open document matches* means a title was typed wrong, not that the refusal works. The project in front unchanged |
| **DR10b** | *Words that are ONE document's whole title are still found, rolled back.* *Heron Twin.rfa* closed, *Heron Twin.rvt* and *Heron Twin 2.rvt* open: `python mcp/client/heron_bridge_client.py validate --in "Heron Repairs Test" --out <a scratch file> --write --set "categories=Generic Models" --set elementIds= --set offsetMm=0,20000,0 --set "sourceDocumentTitle=Heron Twin" --negative-set "categories=Generic Models" --negative-set elementIds= --negative-set offsetMm=0,20000,0 --negative-set "sourceDocumentTitle=No Such File" copy-elements-between-documents < /dev/null` | Positive: `copied` 1 from *Heron Twin*, no refusal, although *Heron Twin 2* holds the same words. Negative: refused, naming the documents that are open |
| **DR10c** | *Words that fit several documents of different titles, rolled back - row 5b-1516's case.* The same three open as DR10b plus *Heron Copy Source*: `python mcp/client/heron_bridge_client.py validate --in "Heron Repairs Test" --out <a scratch file> --write --set "categories=Generic Models" --set elementIds= --set offsetMm=0,20000,0 --set "sourceDocumentTitle=Heron Copy Source" --negative-set "categories=Generic Models" --negative-set elementIds= --negative-set offsetMm=0,20000,0 --negative-set "sourceDocumentTitle=Twin" copy-elements-between-documents < /dev/null` | Positive: `copied` 3. Negative: `copied` 0 and `report` reading *2 open documents match "Twin"*, naming *Heron Twin* and *Heron Twin 2* each with its path, then *Give the whole title of the one you mean* and *NOTHING WAS COPIED*. Version 3 copied from whichever Revit listed first |
| **DR11** | The rest on **Revit 2020** and **2027** | the same answers where the card declares the release. Compiled on every declared release; what Revit DOES was measured on none |
| **DR12** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each |
