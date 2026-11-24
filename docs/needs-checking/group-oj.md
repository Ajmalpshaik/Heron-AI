# Needs checking — Group OJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OJ - a setting-out check: this model's grids and levels against one linked model's, within a tolerance: `CHECK_DATUMS_AGAINST_LINK` (2026-11-17)

**Tracked as GitHub issue [#1036](https://github.com/Ajmalpshaik/Heron-AI/issues/1036) since 2026-11-19** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Asked for in the modeller's words on 2026-11-17: *"Before every issue, check our grids and levels line up
with the linked architect's (or structural) model."* Nothing in the library measured a linked grid against
one of this model's, so a new READ card was written in a cloud container:
[`check-datums-against-link`](../../brain/fragments/check-datums-against-link/fragment.yaml)
(`FRG-QA-038`, version 1, **DRAFT**). It overlaps the level half of `REPORT_LEVEL_ELEVATIONS`'
`includeLinks` lines, which compare linked levels by name with no tolerance - recorded on both cards for
`heron-duplicate-checker`.

It compiles on every release it claims (2020 to 2027, `tools/check-fragments-compile.py`, the cloud
container's .NET SDK). **It has not run in Revit.** No add-in code changed, so nothing needs rebuilding: the
card travels as source from the checkout. A READ runs through `revit_read` while DRAFT and says so, so it
needs no Changes switch and no one-run offer. The batch is
[`tools/jobs/check-datums-against-link-oj-2026-11-17.yaml`](../../tools/jobs/check-datums-against-link-oj-2026-11-17.yaml)
(OJ1); OJ2 to OJ4 are hand runs, because a batch writes one proof draft per card. **Revit 2024 first.**

**The models: "datum-link" and "datum-host", scratch projects from Revit 2024's own
`English\DefaultMetric.rte`, saved under `%TEMP%\heron-work\oj` - never a working model.** Every run
below is a read; only OJ0 changes anything, and it changes only these two files.

| ID | Do this | Pass looks like |
|---|---|---|
| **OJ0** | *Arrange, Revit 2024, kept.* (1) New project from `DefaultMetric.rte`, saved as `datum-link.rvt`. With `create-grids` (PROVEN) `gridLines=0,0,0; 0,20000,0 \| 8000,0,0; 8000,20000,0 \| -2000,0,0; 10000,0,0 \| -2000,6000,0; 10000,6000,0 \| 30000,0,0; 30000,20000,0`, `gridNames=A, B, 1, 2, X9`; then `centreMm=20000,0`, `radiiMm=5000` for one arc grid, renamed **R1** by hand. Save, close. (2) New project from the same template, saved as `datum-host.rvt`. `create-grids` `gridLines=0,20000,0; 0,0,0 \| 8003,0,0; 8003,20000,0 \| -2000,0,0; 10000,0,0 \| -2000,6000,0; 10000,6000,0 \| -9000,0,0; -9000,20000,0`, `gridNames=A, B, 1, G2, H7`; the same arc grid, renamed **R1**; `create-levels` `names=Level 3`, `elevations=8000`; `link-files` `paths=<the full path of datum-link.rvt>`, `placement=origin`. Save; leave `datum-host` IN FRONT | `datum-host` has grids A, B, 1, G2, H7, R1, levels Level 1, Level 2, Level 3, and ONE link placement, `datum-link.rvt : 1 : location <Not Shared>`. **Write down** the template's Level 2 height in both (it must be the same) and the link placement's name as Manage Links shows it. If `create-grids` names the arc grid itself, rename it in both - a name that differs is a different case (OJ1's G2) **Arranged 2026-11-20 another way, on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), not OJ0's two new projects - Heron's save tool is DRAFT, so a project with grids drawn could not be saved for linking - but *House Scratch 1042* (a copy of Revit 2020's `rac_basic_sample_project.rvt`, 14 grids, 6 levels) with *Datum Link 1036.rvt*, a second untouched copy of the same file, linked origin to origin by `LINK_FILES` (kept), both in `%TEMP%\heron-work\issues-1120`, for issue [#1036](https://github.com/Ajmalpshaik/Heron-AI/issues/1036).** Before any move: compared 20, aligned 20, failures 0. Then the host's grid B (195411) moved 3 mm with `MOVE_ELEMENTS offset=0,3,0` (kept; the tool said *"1 could not be read back"* - a grid has no box - and the check below read the 3 mm). Not struck: the reversed, renamed, arc, host-only and link-only datums OJ1 lists were not made. |
| **OJ1** | *`check-datums-against-link` v1 - batch.* Every value left out. Negative: `toleranceMm=5` | Positive `linkUsed` the placement's name; `failures` 1; `failingIds` and `elements` grid B alone; `report` rows *"grid \| B \| off \| 3.00 mm \| 0.0000 deg \|"*, *"grid \| A \| aligned \| 0.00 mm \| 0.0000 deg \| drawn the opposite way"*, *"grid \| G2 \| aligned \| ... \| name differs - the link calls it '2'"*, *"grid \| R1 \| aligned \| 0.00 mm \| - \| centres 0.00 mm apart, radius +0.00 mm in the link"*, *"grid \| X9 \| link only"*, *"grid \| H7 \| host only"*, both template levels *"aligned \| 0.00 mm"*, *"level \| Level 3 \| host only"*; `compared` 7, `aligned` 6, `notChecked` 0, `hostOnly` 2, `linkOnly` 1. The binding note says every value came "not given". Negative: the same 3 mm on B, now *"aligned \| 3.00 mm"*; `failures` 0, `failingIds` empty, `compared` 7, `aligned` 7 **Run 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), not OJ0's two new projects - Heron's save tool is DRAFT, so a project with grids drawn could not be saved for linking - but *House Scratch 1042* (a copy of Revit 2020's `rac_basic_sample_project.rvt`, 14 grids, 6 levels) with *Datum Link 1036.rvt*, a second untouched copy of the same file, linked origin to origin by `LINK_FILES` (kept), both in `%TEMP%\heron-work\issues-1120`, for issue [#1036](https://github.com/Ajmalpshaik/Heron-AI/issues/1036), by `batch-prove` (PASS, *positive: failures 1*) and by hand:** positive, every value left out - `failures` 1, `failingIds` [195411], `elements` [B], *"grid \| B \| off \| 3.00 mm \| 0.0000 deg \|"*, compared 20, aligned 19; negative `toleranceMm=5` - *"grid \| B \| aligned \| 3.00 mm \| 0.0000 deg \|"*, failures 0, failingIds empty, compared 20, aligned 20. Proof draft `brain/proof-drafts/check-datums-against-link.yaml` written, **not signed - the owner's word**. Not struck: this arrangement has no reversed, renamed, arc, host-only or link-only datum, so those report rows were not seen. |
| ~~**OJ2**~~ | The same card, by hand - the placement counts. In `datum-host`, Move the link instance 100 mm UP… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Both template levels "off \| 100.00 mm \| - \| the link's is higher", every grid row… |
| ~~**OJ3**~~ | By hand - the refusals. With every value left out except, in turn: `datums=walls`; `toleranceMm=0`… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Each refused, `compared` 0, `report` empty, findings ending "NOTHING WAS COMPARED."… |
| **OJ4** | *By hand - the second route.* In a floor plan of `datum-host` with the link showing, an aligned dimension from the link's grid B to this model's grid B; then a section through the levels | The dimension reads 3 mm, OJ1's B offset. The link's level heads and this model's sit on one line for both template levels, and Level 3 has none beside it |

**Read what each verdict quotes before signing: the runner cannot read a sentence**, and OJ1's negative is
judged only on its counts.
