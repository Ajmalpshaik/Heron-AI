# Needs checking — Group DO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DO - model and file setup: files linked, link paths switched, links' shared coordinates checked, material textures read, unused images found (2026-10-12)

The fifth batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)).

**What was built - five new capabilities.** `LINK_FILES` (FRG-DOC-043): a Revit model or CAD drawings, one,
several or a folder - `RELOAD_LINKS` and `UNLOAD_LINKS` act on links already there. `SET_LINK_PATH_TYPE` (044).
`REPORT_MATERIAL_TEXTURES` (045), read without naming the appearance-asset types. `CHECK_LINK_COORDINATES`
(FRG-QA-035). `FIND_UNUSED_IMAGES` (FRG-SEL-036), which deletes nothing: it hands the images to
`DELETE_ELEMENTS`, the one delete path, already PROVEN.

**Not built, and recorded in the plan:** save-as and compact (Revit cannot save a document while a change is
open, and every fragment runs inside one); repathing or removing a texture (needs the appearance-asset
namespace added to the fragment contract - a decision for the owner); removing non-primary design options
(`SELECT_BY_DESIGN_OPTION` then `DELETE_ELEMENTS` already does it).

**What the code assumes and only Revit can say:** that `ProjectLocation.GetProjectPosition` of a point is that
point's shared coordinates in both the host and the link; that a bitmap property's text is reachable by
reflection on every release; that `RevitLinkType.PathType` can be set inside a transaction.

**Measured without Revit, 2026-10-12.** Every Revit API member was read off `RevitAPI.xml` in the Nice3point
reference packages for all eight releases. Each fragment compiles on every release it declares
(`tools/check-fragments-compile.py`, all eight run), passes the author gate, and the job file reads `5 WOULD RUN`
under `batch-prove --dry-run`. **Nothing below has run in Revit.**

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DO1** | Arrange on **Revit 2024**: the two scratch projects the header of [`gap-fill-model-setup-2026-10-12.yaml`](../../tools/jobs/gap-fill-model-setup-2026-10-12.yaml) describes | the link-test folder, the moved link, the missing texture, the three images |
| **DO2** | `python tools/batch-prove.py tools/jobs/gap-fill-model-setup-2026-10-12.yaml`, both titles filled | linked 3 then 0; path types switched then none found; the missing texture named then none; the moved link named then no links; one unused image then none. Look at Manage Links afterwards - nothing new: rollback has failed on this PC before (BD1) |
| **DO3** | check-link-coordinates by hand on a link placed by shared coordinates and not moved | `agreeing` 1, nothing mismatched |
| **DO4** | find-unused-images, then `DELETE_ELEMENTS` with expect_from the find, kept | only the unused image gone from Manage Images; the placed one and the Type Image one still there |
| **DO5** | link-files with placement shared on a folder of drawings set out to the survey | each lands on its coordinates |
| **DO6** | The rest on **Revit 2020** and **2027** | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DO7** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each of the five |
