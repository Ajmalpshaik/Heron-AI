# Needs checking — Group MP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MP - Manage > Sheet Issues/Revisions and revision clouds - swap a sheet's last revision, read every cloud, move or delete clouds of one revision on chosen sheets, and check each sheet's revisions: `SET_SHEET_REVISIONS` fixed and widened, `LIST_REVISIONS` widened, `LIST_REVISION_CLOUDS`, `EDIT_REVISION_CLOUDS` (2026-11-07)

On 2026-11-07, on `4355-BHVD-3D-60A10-BL007A` in Revit 2020, the owner asked for today's revision (Seq. 21,
07-Nov-2026) to be the last one on four sheets, keeping the Roof Plan sheet on Seq. 20. `SET_SHEET_REVISIONS`
answered that Seq. 20 was off three sheets while revision clouds kept it on two of them
([row 5b-1463](../fragment-issues/section-5b-rows-401-425.md)), and no tool could move those clouds, so they
were done by hand. This group is the proof owed for the four cards that cover the job.

**Widened, not made new, and why.** The duplicate check (`heron-duplicate-checker`, 2026-11-07) found no SAME JOB
or OVERLAP pair among the live revision cards. `SET_SHEET_REVISIONS` (was PROVEN) took the fix and an optional
`replaceWith`; left out, it is version 1's job. `LIST_REVISIONS` (was PROVEN) took the sheet-by-sheet check and an
optional `expectedLast`. Comments and Mark on clouds are `WRITE_ELEMENT_PARAMETERS`' (PROVEN, unchanged), and
selecting and zooming to clouds is `SET_SELECTION`'s (PROVEN, unchanged), each handed the clouds
`LIST_REVISION_CLOUDS` found. **Made new**, each with its reason in its card: `LIST_REVISION_CLOUDS` - no card
listed revision clouds or read a cloud's tag; `EDIT_REVISION_CLOUDS` - no card changes an existing cloud's
revision (`MERGE_REVISION` moves every cloud of a revision and deletes it; `WRITE_ELEMENT_PARAMETERS` refuses a
revision reference), and its delete carries a count `confirm`, which `DELETE_ELEMENTS` has not got and could not
take without sending every delete in Heron back to DRAFT.

| What | Where | What it does |
|---|---|---|
| `SET_SHEET_REVISIONS` v2 (MODIFY) | [`brain/fragments/set-sheet-revisions`](../../brain/fragments/set-sheet-revisions/fragment.yaml) | Every sheet judged on what it PRINTS after the write; a sheet a cloud keeps a revision on is `cloudDriven`, with the cloud ids in `cloudsHolding`; never-on sheets are `notOnSheet`. `replaceWith` swaps one revision for another, only where the first was, in one undo entry |
| `LIST_REVISIONS` v2 (READ) | [`brain/fragments/list-revisions`](../../brain/fragments/list-revisions/fragment.yaml) | `sheetTable` - each sheet's revisions in sequence, what each prints, ticked / clouded / both, and its current revision; `lastDiffers` against `expectedLast`; `cloudOnly` |
| `LIST_REVISION_CLOUDS` (READ) | [`brain/fragments/list-revision-clouds`](../../brain/fragments/list-revision-clouds/fragment.yaml) | Every cloud's revision, the view it is drawn in, the sheets it prints on, Comments, Mark and tag - by sheet, view and revision; hands the clouds on |
| `EDIT_REVISION_CLOUDS` (MODIFY) | [`brain/fragments/edit-revision-clouds`](../../brain/fragments/edit-revision-clouds/fragment.yaml) | Moves the handed clouds to another revision, or deletes them with `confirm` = the count; names what each sheet's revision block gained and lost |

**What is done and what is not.** All four compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-07, a Linux container). Every Revit member was read off the shipped
reference assemblies with `tools/api-surface --members`. **None has met a Revit model**, and the job file
[`tools/jobs/revision-clouds-test-copy-2026-11-07.yaml`](../../tools/jobs/revision-clouds-test-copy-2026-11-07.yaml)
reads back `4 WOULD RUN` under `--dry-run`. Not read off any assembly: whether Revit lets a cloud of an ISSUED
revision be moved through the API - MP5 shows it.

| # | Run | Look for |
|---|---|---|
| **MP0** | *Open.* Revit 2020 (the owner's release for this job), then once on 2024. A **saved test copy** shaped like `4355-BHVD-3D-60A10-BL007A` - never the job model. Changes on at the ribbon | The title bar names the copy |
| **MP1** | *Arrange, and KEEP it.* As the job file's header says: Seq. A and Seq. B; S1 with Seq. A ticked AND clouded (cloud in a plan placed on S1, with Comments, Mark and a cloud tag); S2 with Seq. A held only by a cloud drawn on the sheet; S3 with Seq. A ticked and a cloud of its own; S4 carrying neither | Manage > Sheet Issues/Revisions shows both; each sheet's revision block shows Seq. A |
| **MP2** | *LIST_REVISION_CLOUDS - D-30 pair, NEGATIVE FIRST.* The job file's first job | Negative (S3, Seq. B): nothing listed. Positive (S1, S2, Seq. A): S1's cloud and S2's, each with the view it is drawn in and the sheets it prints on, S1's with Comments, Mark and tag id - **and not S3's** |
| **MP3** | *LIST_REVISIONS - D-30 pair.* The job file's second job | Negative (expectedLast Seq. A): `lastDiffers` empty. Positive (Seq. B): all three in `lastDiffers`; `sheetTable` S1 "ticked and clouded", S2 "clouded only", S3 "ticked"; `cloudOnly` names S2 only. The 2026-10-17 table rows unchanged |
| **MP4** | *SET_SHEET_REVISIONS - D-30 pair, rolled back.* The job file's third job: Seq. A off S1, S2, S3, replaceWith Seq. B | **Row 5b-1463's case.** S3: Seq. A gone, Seq. B on, counted. S1 and S2 in `cloudDriven`, each line in `cloudsHolding` naming the cloud, **not counted** - `sheetsChanged 1`. Negative (S4): `notOnSheet`, nothing changed. Then once with `replaceWith` left out and attach false on the same three: the same `cloudDriven`, and version 1's answer for S3 |
| **MP5** | *EDIT_REVISION_CLOUDS - D-30 pair, rolled back.* The job file's fourth job | Positive: S1 and S2's Seq. A clouds on Seq. B; `sheetEffect` S2 "no longer prints Seq. A"; **S3's Seq. A cloud untouched**. Negative (fromRevision Seq. B): both skipped, nothing changed. Then once with Seq. A **issued**: whatever Revit says, named, and the whole call rolled back |
| **MP6** | *EDIT_REVISION_CLOUDS delete, rolled back.* action=delete on S2's cloud, first with `confirm=2`, then `confirm=1` | `confirm=2`: nothing deleted, `refused` naming 1. `confirm=1`: the cloud gone, its tag in `alsoDeleted`, `sheetEffect` S2 no longer prints Seq. A |
| **MP7** | *The owner's whole job, through `try-once`, on the copy.* LIST_REVISIONS expectedLast, SET_SHEET_REVISIONS replaceWith on `lastDiffers`, LIST_REVISION_CLOUDS + EDIT_REVISION_CLOUDS on its `cloudDriven`, LIST_REVISIONS again | Every chosen sheet ends on Seq. B, the sheet kept on Seq. A still does; each change one entry in the undo list. Keep or Discard is the owner's |
