# Needs checking — Group FB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FB - the sheets of one revision, latest on the sheet or not, and a typed list of sheet numbers: `FIND_SHEETS_BY_REVISION_OR_NUMBER` (2026-10-17)

**Tracked as GitHub issue [#988](https://github.com/Ajmalpshaik/Heron-AI/issues/988) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Found on 2026-10-14 on a real Revit 2020 job: a sheet set of the 8 sheets carrying revision Seq. 15
"IAB-ISSUED AS-BUILT" could only be made through `SELECT_BY_PARAMETER_VALUE` on Sheets with *Current
Revision Description* equal to the description - which picks a sheet only while that revision is the
LATEST on it. `FIND_SHEETS` takes one part of a number or a name, so *"these 8 sheets by number"* had no
route either. [Row 5b-550](../fragment-issues/section-5b-rows-526-550.md) has the whole of it.

**What changed.** [`FIND_SHEETS_BY_REVISION_OR_NUMBER`](../../brain/fragments/find-sheets-by-revision-or-number/fragment.yaml)
(FRG-SHT-033, DRAFT, READ) leaves `elements` - the sheets of one revision found anywhere in each sheet's
own list of revisions (`ViewSheet.GetAllRevisionIds`), the sheets in a typed list of whole sheet
numbers, or both - for `MANAGE_SHEET_SETS`, a PDF export or any action that takes sheets. The revision is
chosen by sequence, description (whole first, then a part) or date as typed; a description that fits two
revisions is refused with both named. All four inputs are `optional: true` with `absent:`
([PR #545](https://github.com/Ajmalpshaik/Heron-AI/pull/545)), so a value not mentioned is left out.

**What is compiled and what is not run.** It compiles on all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17). Its matching rules ran in a scratch harness against
stand-in classes - not Revit - on 2026-10-17, and gave answers of the shape FB1 to FB5 expect. **Nothing of it has run
in Revit.** Whether a revision set to *Show: None* is still in `GetAllRevisionIds`, and whether a
placeholder sheet can carry a revision at all, are not known; FB7 and FB8 find out.

**A NEW SCRATCH PROJECT, never a working model.** In Revit 2020 first, from the default metric template,
saved as *Heron Sheets By Revision.rvt*. The add-in must be built from `main` on or after PR #545 and
deployed - an older one refuses a value left out. Arrange it by hand:

1. Sheets **179, 180, 181, 182, 183, 184, 185, 186** and **1790**, any title block; and one placeholder
   sheet **P-01** (View > Sheet Composition > New Sheet > *New placeholder sheet*).
2. Sheet Issues/Revisions: the template's Revision 1 is Seq 1. Add three - **Seq 2** *FB ISSUED AS-BUILT*,
   date *14.10.2026*; **Seq 3** *FB DRAFT AS-BUILT*, date *14.10.2026*; **Seq 4** *FB LATER*, date
   *20.10.2026*. If Revit gives other sequence numbers, write them down and use them everywhere below and
   in the job file.
3. Revisions on Sheet: tick **Seq 2** on 179 to 185; **Seq 3** on 1790 only; **Seq 4** on 180 and 181 as
   well, so Seq 2 is no longer the latest on those two.
4. Sheet 186: place any floor plan on it, draw one revision cloud in that plan set to **Seq 2**, and tick
   nothing on the sheet.

[`tools/jobs/find-sheets-by-revision-or-number-2026-10-17.yaml`](../../tools/jobs/find-sheets-by-revision-or-number-2026-10-17.yaml)
runs FB1 to FB3 through `batch-prove`; `--dry-run` reads it back without Revit.

| # | Run | Look for |
|---|---|---|
| ~~**FB1**~~ | `revit_read` capability `FIND_SHEETS_BY_REVISION_OR_NUMBER` with `revisionSequence=2` and nothing… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **Eight** |
| ~~**FB2**~~ | The same with `sheetNumbers=17` and nothing else — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | `elements` empty and `notFound` 17 - neither 179 nor 1790 picked. |
| ~~**FB3**~~ | `revisionSequence=2`, `sheetNumbers=179, 180, 1790, 999` — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | 179 and 180 picked; `listedWithoutRevision` 1790; `notFound` 999; `revisionsMatched` ends… |
| ~~**FB4**~~ | `revisionDescription=FB ISSUED AS-BUILT`; then `revisionDescription=as-built` — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | The first picks FB1's eight and does not choose Seq 3. |
| ~~**FB5**~~ | `revisionDate=14.10.2026`; then `revisionSequence=9`; then no values at all — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | The date picks 179 to 186 and 1790, both revisions named with their counts. |
| ~~**FB6**~~ | Second route, by hand: each sheet's Revisions on Sheet, and a sheet list schedule with Current… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Seq 2 ticked or clouded on exactly FB1's eight; the schedule shows Seq 4 as current on… |
| ~~**FB7**~~ | An observation. Set Seq 2 to Show: None in Sheet Issues/Revisions, then FB1 again; set it back — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Whether the eight still come back. |
| ~~**FB8**~~ | An observation. Try to tick Seq 2 on placeholder P-01; if Revit allows it, FB1 again — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Whether P-01 is picked and its id is in `placeholders`. |
| ~~**FB9**~~ | Then the chain the 2026-10-14 job needed: FB1's sheets to `revit_change` `MANAGE_SHEET_SETS`… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | `memberCount` 8 and `notASheet` empty. |
| ~~**FB10**~~ | FB1 and FB2 once on Revit 2024 and once on 2027 — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | The same answers - one implementation, every release CLOSED 2026-11-13, not passed… |
