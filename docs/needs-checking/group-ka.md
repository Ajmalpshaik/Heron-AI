# Needs checking — Group KA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group KA - the saved export setups and View/Sheet Sets: `LIST_EXPORT_SETUPS`, `MANAGE_EXPORT_SETUPS`, `SET_EXPORT_SETUP_OPTIONS`, `SYNC_SHEET_SETS_BY_PARAMETER`, `CHECK_SHEET_SETS_BY_PARAMETER`, and `MANAGE_SHEET_SETS` with nothing selected and its copy mode (2026-11-01)

Asked for by Ajmal PS on 2026-11-01, after Heron could not rename the DWG/DXF export setup *Setup 1* to
*Tags Export* on a job model ([FRAGMENT-ISSUES row 5b-1360](../fragment-issues/section-5b-rows-401-425.md)).
The research - which setups the Revit API reaches, on which release, and every option inside each - is
[`docs/work-notes/investigations/2026-11-01-export-setups.md`](../work-notes/investigations/2026-11-01-export-setups.md).

| What | Where | What it does |
|---|---|---|
| `LIST_EXPORT_SETUPS` (READ) | [`brain/fragments/list-export-setups`](../../brain/fragments/list-export-setups/fragment.yaml) | Every DWG/DXF, DGN, PDF (2022 on) export setup and print setting, which is active, and its key options; with one setup named, its layer table |
| `MANAGE_EXPORT_SETUPS` (MODIFY) | [`brain/fragments/manage-export-setups`](../../brain/fragments/manage-export-setups/fragment.yaml) | `mode` create, duplicate, rename or delete a setup by name; print settings duplicate, rename and delete through the print manager, the current one put back |
| `SET_EXPORT_SETUP_OPTIONS` (MODIFY) | [`brain/fragments/set-export-setup-options`](../../brain/fragments/set-export-setup-options/fragment.yaml) | Named options of one setup, all or nothing, read back after: file version, units, coordinates, solids, colours, line scaling, external references, layer standard, hide options, one layer row, and the PDF options |
| `SYNC_SHEET_SETS_BY_PARAMETER` (MODIFY) | [`brain/fragments/sync-sheet-sets-by-parameter`](../../brain/fragments/sync-sheet-sets-by-parameter/fragment.yaml) | One View/Sheet Set per value of a sheet parameter, in one call; existing sets left alone unless `updateExisting`, then made to hold exactly their value's sheets and views; print range, current set and In-Session members put back. Added the same day, [row 5b-1361](../fragment-issues/section-5b-rows-401-425.md) |
| `CHECK_SHEET_SETS_BY_PARAMETER` (READ) | [`brain/fragments/check-sheet-sets-by-parameter`](../../brain/fragments/check-sheet-sets-by-parameter/fragment.yaml) | Per value, the set named for it - in step, missing members, extra members - and the values with no set |
| `MANAGE_SHEET_SETS` (PROVEN) - its card only | [`brain/fragments/manage-sheet-sets`](../../brain/fragments/manage-sheet-sets/fragment.yaml) | `elements` marked optional, so rename and delete run with nothing selected. `impl/` untouched; the proof's fingerprint holds |

**What is done and what is not.** All three compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01), and every Revit member they call was read off the shipped
assemblies of all eight (`tools/api-surface --members`). **Nothing here has run in Revit.** The five new cards
are DRAFT.

**The models - scratch projects, never a 4355-* job model.** A new project from Revit 2020's default metric
template, saved as `Heron Export Setups KA`, with two DWG/DXF setups made by hand as the job file says; for
KA6 a Revit 2022 or later copy of the same. The job file is
[`tools/jobs/export-setups-scratch-2026-11-01.yaml`](../../tools/jobs/export-setups-scratch-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Proving waits for Ajmal PS's word.

| # | Run | Look for |
|---|---|---|
| **KA1** | *The list.* The job file's `list-export-setups` leg | Positive: HERON KA A reading *AutoCAD 2013 format, units Millimeter, Shared coordinates, views on sheets as external references*, HERON KA B as Revit made it, `setupCount` the DWG/DXF setups the dialog lists. **Negative:** `setupName` *ZZZ NO SUCH SETUP* refused, **naming it** and the setups that are there; `setups` empty |
| **KA2** | *The rename Ajmal PS asked for, on a scratch project.* The job file's `manage-export-setups` leg | Positive: `done` true, `kindUsed` *DWG/DXF* found with the kind left out, the dialog listing *HERON KA Tags Export* and not *HERON KA A*, and LIST_EXPORT_SETUPS reading the same options before and after. **Negative:** *ZZZ NO SUCH SETUP* refused, naming it; no *HERON KA X* in the dialog afterwards. **Whether Revit takes `Element.Name` on an export setup is the question this row settles** |
| **KA3** | *Four options at once.* The job file's `set-export-setup-options` leg | Positive: `changed` naming units, fileVersion, coordinates and externalReferences each *before -> after*, `notKept` empty, and the dialog's General tab showing them. **Negative:** *ZZZ NO SUCH SETUP* refused, naming it; HERON KA B not in millimetres afterwards. Then by hand: `options` *units=mm; colour=purple* - refused naming colour's bad value, and HERON KA B **not** in millimetres (all or nothing) |
| **KA4** | *Duplicate keeps the layers.* `mode=duplicate`, `setupName=HERON KA A`, `newName=HERON KA Copy`, kept | The copy listed; LIST_EXPORT_SETUPS with each setup's name giving the same `layerTable`; `note` empty. **A `note` naming different layer rows means `Create(doc, name, options)` does not carry the table** - record it and correct the card |
| **KA5** | *Delete, and the active setup.* Make HERON KA Copy the active setup in the DWG dialog, then `mode=delete` on it, after a yes | Record what Revit does: refused, or deleted with `note` saying it was active - and which setup the DWG dialog opens on afterwards |
| **KA6** | *PDF, Revit 2022 or later.* `MANAGE_EXPORT_SETUPS` `mode=create`, `kind=pdf`, `setupName=HERON KA PDF`; then `SET_EXPORT_SETUP_OPTIONS` *combine=yes; colors=grayscale; zoom=fit* | Created with Revit's default naming rule, or refused with Revit's reason - record which; then File - Export - PDF showing it combined, grey scale, fit to page. On 2020: both refused as needing Revit 2022 |
| **KA7** | *The layer standard.* `SET_EXPORT_SETUP_OPTIONS` `layerStandard=` each standard the DWG dialog offers, one at a time, rolled back | What text Revit keeps for each, and whether the layer table changes with it (LIST_EXPORT_SETUPS `layerTable` before and after). The card claims neither |
| **KA8** | *One layer row.* `layer:Ducts=M-HVAC-DUCT` on HERON KA B | The Layers tab showing M-HVAC-DUCT against Ducts. Negative: `layer:No Such Category=X` refused naming the row, nothing changed |
| **KA9** | *Print settings.* Two print settings saved by hand, the Print dialog on the first; `MANAGE_EXPORT_SETUPS` `kind=print`, `mode=duplicate` the second as *HERON KA Print Copy* | The copy listed in File - Print - Setup, and `note` saying the current setting is the first, as before - and the dialog opening on it. Then `mode=delete` on the copy |
| **KA10** | *The second route.* File - Export - CAD Formats - DWG and its Modify DWG/DXF Export Setups dialog, read by a person after KA2, KA3 and KA8 | Every name and option the cards reported, the same in the dialog. **A disagreement is a FRAGMENT-ISSUES row** |
| **KA11** | *Rename and delete with nothing selected.* `Heron Sheet Sets KA` after KA12 kept, the selection empty: `MANAGE_SHEET_SETS` `mode=rename`, `setName=01_IAB-ISSUED AS-BUILT`, `newName=HERON KA Renamed` - then `mode=delete` on it, after a yes | Both run with no selection and no refusal about `elements`, the set renamed then gone in File - Print - Settings. Then `mode=create` with nothing selected: refused *No views or sheets to put in the set.* - the proven negative, unchanged |
| **KA12** | *One set per value.* The job file [`sheet-sets-by-parameter-scratch-2026-11-01.yaml`](../../tools/jobs/sheet-sets-by-parameter-scratch-2026-11-01.yaml), its `sync-sheet-sets-by-parameter` leg | Positive: `created` *'01_IAB-ISSUED AS-BUILT' (3); '02_IFI-ISSUED FOR INFORMATION' (2)*, A106 in neither, `printRange` put back to Current window. **Negative:** *ZZZ NO SUCH PARAMETER* refused, naming it; no set made. After the rollback File - Print lists no set and shows Current window |
| **KA13** | *The check.* The same job file's `check-sheet-sets-by-parameter` leg, then by hand after KA12 kept and A103 changed to 02_IFI | Job: both values in `missingSets`. By hand: `outOfStep` 2 - 01_IAB *extra A103*, 02_IFI *missing A103*. **Negative:** *ZZZ NO SUCH PARAMETER* refused, naming it |
| **KA14** | *Bring existing sets back in step.* After KA13's change: SYNC with `updateExisting` left out, then `true` | Left out: `refused` saying every set already exists, `unchanged` naming both out of step and left alone. True: `updated` 01_IAB *(0 in, 1 out)*, 02_IFI *(1 in, 0 out)*; KA13's check then `inStep` 2. And whether the Print dialog's In-Session selection is as it was before - the card puts it back |
| **KA15** | *Copy a set (`MANAGE_SHEET_SETS` version 3).* The job file [`manage-sheet-sets-v3-duplicate-scratch-2026-11-01.yaml`](../../tools/jobs/manage-sheet-sets-v3-duplicate-scratch-2026-11-01.yaml), on `Heron Sheet Sets KA` with the set *HERON KA DUP* (A101, A102) made by hand, nothing selected | Positive: `done` true, `memberCount` 2, *HERON KA DUP* and *HERON KA DUP COPY* both in File - Print - Settings with A101 and A102 ticked, `printRange` put back to Current window. **Negative:** *ZZZ NO SUCH SET* refused, naming it; no *HERON KA ZZZ COPY* afterwards. Whether `SaveAs` on a saved set that was made current leaves the original unchanged is the question this row answers |
| **KA16** | *Version 2's modes, again, on version 3's code.* Group FA's job files ([`manage-sheet-sets-v2-remove-revit2020-2026-10-17.yaml`](../../tools/jobs/manage-sheet-sets-v2-remove-revit2020-2026-10-17.yaml) then [`manage-sheet-sets-v2-revit2020-2026-10-17.yaml`](../../tools/jobs/manage-sheet-sets-v2-revit2020-2026-10-17.yaml)), and by hand `mode=duplicate` with `newName` an existing set's name | FA's verdicts as before - the duplicate branch sits beside them and changes no other mode. By hand: refused *A sheet set called '...' already exists - nothing was copied.*, both sets unchanged. The card's signed proof is the run that ends LAST - make it KA15's |
