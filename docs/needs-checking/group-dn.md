# Needs checking — Group DN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DN - views and sheets: 3D cameras, cut planes read and moved, browser organisation, templates compared, link display, room elevations refitted, viewport types and title lines, assemblies (2026-10-12)

The fourth batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)).

**What was built - ten new capabilities.** `SET_VIEW_CAMERA` (FRG-VIEW-128) - `CREATE_3D_VIEW` is PROVEN and
makes an isometric default, so it was not widened. `REPORT_VIEW_CUT_PLANES` and `MOVE_VIEW_CUT_PLANE`
(FRG-VIEW-129, 130). `REPORT_BROWSER_ORGANIZATION` (131), `COMPARE_VIEW_TEMPLATES` (132) - `REPORT_VIEW_TEMPLATE_CONTROL`
reads one view. `REPORT_LINKED_VIEW_USAGE` (133, Revit 2024 on). `REFIT_ROOM_ELEVATIONS` (134).
`REPORT_VIEWPORT_TYPES` and `SET_VIEWPORT_TITLE_LINE` (FRG-SHT-030, 031; the title line 2022 on).
`CREATE_ASSEMBLY` (FRG-ELE-080) - `CREATE_ASSEMBLY_VIEWS` needs an assembly that exists.

**What the code assumes and only Revit can say:** that a section's viewer is the Viewers element of the same
name, so moving it moves the cut plane; that a crop box set on an interior elevation holds; that a viewport's
title outline is a fair width to fit its line to; that `LinkVisibility` reads as the Revit Links tab shows.

**Measured without Revit, 2026-10-12.** Every Revit API member was read off `RevitAPI.xml` in the Nice3point
reference packages for all eight releases. Each fragment compiles on every release it declares
(`tools/check-fragments-compile.py`, all eight run), passes the author gate, and the job file reads `10 WOULD RUN`
under `batch-prove --dry-run`. **Nothing below has run in Revit.**

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DN1** | Arrange on **Revit 2024**: the two scratch projects the header of [`gap-fill-views-2026-10-12.yaml`](../../tools/jobs/gap-fill-views-2026-10-12.yaml) describes | the section, room 101 with its four elevations and one wall moved out, the two templates, two sheets, the link set By Linked View in one plan, the pipe run |
| **DN2** | `python tools/batch-prove.py tools/jobs/gap-fill-views-2026-10-12.yaml`, both titles filled | each positive answers and each negative is refused or empty as the job's notes say. Look at the model afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DN3** | set-view-camera by hand, createIfMissing, kept | a perspective view named as asked, eye at 1600 mm, horizon level; Show Camera in a plan shows it |
| **DN4** | move-view-cut-plane on Section 1, offsetMm 500, kept | the section line in plan 500 mm further on; `movedMm` 500 |
| **DN5** | refit-room-elevations on room 101, kept | each elevation's crop on the room as it now is, the one facing the moved wall 1000 mm deeper |
| **DN6** | set-viewport-title-line on **Revit 2022** and 2027, lengthMm 80 | each title line 80 mm on the sheet; on 2020 the card is not offered at all |
| **DN7** | create-assembly kept, then the same again | Spool 01 in the Project Browser's Assemblies; the second run refused, every pipe already in one |
| **DN8** | The rest on **Revit 2020** and **2027** | the same answers. Compiled on every declared release; what Revit DOES was measured on none |
| **DN9** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each of the ten |
