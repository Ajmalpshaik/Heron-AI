# Needs checking — Group HM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HM - when the file on disk was last saved: `REPORT_OPEN_DOCUMENTS` version 3 (2026-10-19)

Found on 2026-10-19 building a 40-floor tower through Heron on Revit 2020. The owner pressed Ctrl+S
twice, but the keys went to the Claude window, so the .rvt on disk stayed from before the build for
about 25 minutes - 10:42, while 124 changes went in after 10:45. `REPORT_OPEN_DOCUMENTS` said only
`[unsaved changes]`, which cannot tell five minutes of work from a whole tower, and the file's modified
time was read by hand. [Row 5b-1216](../fragment-issues/section-5b-rows-401-425.md).

**What changed.** [`REPORT_OPEN_DOCUMENTS`](../../brain/fragments/report-open-documents/fragment.yaml)
(FRG-DOC-026, DRAFT) version 3 gives every open project and family that has a file the time its file on
disk was last written, and how long ago, in `fileTimes` - one line, for `familyList`'s reason - with
`[unsaved changes]` beside it. A document with unsaved changes whose file is more than 15 minutes old is a
finding with that time - *"the file on disk is from 10:42 (37 min ago) - everything since is not saved"* -
telling the modeller to click inside the Revit window before Ctrl+S, and `staleFileCount` counts them.
Every output version 2 had is unchanged. The `building-shell` and `family-creation` methods now check a
save with it at step 0 and at each milestone, instead of trusting that a key was pressed.

**What is compiled and what is not run.** The fragment compiles for every release it claims, 2020 to
2027 (`tools/check-fragments-compile.py`, 2026-10-19). **None of it has run in Revit.** The time is .NET's
`File.GetLastWriteTime` on the document's `PathName`, no Revit call - so what is unproved is whether
Revit's own save moves that time as Explorer shows it, and whether `IsModified` is false straight after.

**A SCRATCH PROJECT AND A SCRATCH FAMILY, never a working model**, with Heron on the PC updated to the
merge of this change. In Revit 2020 first: a project from the default metric template saved as
*Heron Save Time.rvt*, and a family from *Metric Generic Model.rft* saved as *Heron Save Time.rfa* -
both saved, closed, and left **more than 15 minutes** before HM1, so that their files are old. Then open
both.

| # | Run | Look for |
|---|---|---|
| **HM1** | Draw one wall in *Heron Save Time*, do not save, and `revit_read` capability `REPORT_OPEN_DOCUMENTS` | `fileTimes` names *Heron Save Time* with `file written HH:mm, N min ago  [unsaved changes]`, the time its file was saved before the 15 minutes; the family, open and unchanged, beside its own time with no `[unsaved changes]`; `staleFileCount` 1; a finding *"the file on disk is from HH:mm (N min ago) - everything since is not saved"* that says to click inside the Revit window before Ctrl+S. **The D-30 positive** |
| **HM2** | Click inside the Revit window, Ctrl+S, then the same read at once | *Heron Save Time*'s time is now - `under a minute ago` - with no `[unsaved changes]`; `staleFileCount` 0 and no "not saved" finding. **The D-30 negative**: a save that reached the disk is never called stale |
| **HM3** | HM1 and HM2 on the family: a reference plane added in *Heron Save Time.rfa*, the read; then Ctrl+S in that window and the read again | The family in `fileTimes` with its .rfa's time, named in the finding, then `under a minute ago` and gone from it - a family is checked exactly as a project |
| **HM4** | After HM2, draw one more wall and read at once | `[unsaved changes]` beside a time under a minute old, `staleFileCount` 0, no "not saved" finding - recent unsaved work is not called stale |
| **HM5** | *The day's own case.* Make a change, wait 15 minutes, click in the chat window and press Ctrl+S there, then read | Still in the finding with the old time - the keys did not reach Revit, and the read says so where `[unsaved changes]` alone could not |
| **HM6** | HM1 and HM2 once on Revit 2024 and once on 2027 | The same answers - nothing in version 3 is a Revit call, so a difference is the save itself |
| **HM7** | Second route: Windows Explorer's Date modified on *Heron Save Time.rvt* and *.rfa*, beside each read | The same time as `fileTimes`, to the minute, every time |
| **HM8** | *Watching, not a proof.* A chat builds a small shell from `building-shell` in a saved scratch project | At step 0 and at each milestone it asks the modeller to click inside Revit and press Ctrl+S, then reads `REPORT_OPEN_DOCUMENTS` and says whether the file's time moved - never takes the save on trust |
