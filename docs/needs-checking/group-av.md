# Needs checking — Group AV

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-25 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AV - `apply-color-fill-scheme`: a plan's colour scheme and its Color Fill Legend (2026-09-27)

**Revit 2022 or later, on a TEST COPY.** [`APPLY_COLOR_FILL_SCHEME`](../../brain/fragments/apply-color-fill-scheme/fragment.yaml)
(FRG-VIEW-110, DRAFT) sets a plan's colour scheme for one category - using a scheme that already
colours by the named parameter as it stands, or COPYING one and changing only the copy - and places
Revit's Color Fill Legend 10 mm on the sheet right of the crop region, level with its top. The proof
plan is [`tools/jobs/apply-color-fill-scheme-heron-ai-bulding-2026-09-27.yaml`](../../tools/jobs/apply-color-fill-scheme-heron-ai-bulding-2026-09-27.yaml)
and [its cases](../../brain/fragments/apply-color-fill-scheme/tests/cases.yaml).

**What was run, and it is NOT a proof.** On 2026-09-27, at the owner's request through another
session, it was run with `revit_change` and KEPT on his model *heron ai bulding* (Revit 2024, session
46596). No negative was rolled back beside a positive and no fingerprint was taken (D-30):

| View | Category, by | Scheme | Read back |
|---|---|---|---|
| Level 1 - Room Plan Layout | Rooms, Name | **copied** "Department" as "Rooms by Name"; "Department" unchanged | set; 15 rows; 1 legend, 10 mm right of the crop, 0 mm below its top |
| Level 1 - Space Plan Layout | Spaces, Name | existing "Schema 1", used as it stands | set; 15 rows; 1 legend, same place |
| Level 1 - HVAC Zone Plan Layout | HVAC Zones, Name | existing "Schema 1", used as it stands | set; 5 rows; 1 legend, same place |

Before them, Walls on the Room plan was refused and wrote nothing - *"cannot take a colour scheme for
Walls. It takes: Ducts, HVAC Zones, Pipes, Rooms, Spaces"*. After them, the Room plan was run a second
time: it used "Rooms by Name", made no copy and placed no second legend (`legendsInView` 1).

**The code changed after that run, and the change has not been run in Revit.** A review of
[PR #342](https://github.com/Ajmalpshaik/Heron-AI/pull/342) found six things, all taken: a range scheme
on the parameter now counts as a match; the view is asked before a scheme is copied; any failure after a
write throws, so a copy, or a scheme set with no legend, is rolled back instead of kept; numeric rows are
written in the project's units; and a category with nothing placed yet can still be coloured by a
built-in parameter. The path the three plans took - use or copy, set, place the legend - is the same
code, but the proof (AV1) is what shows it.

**The colours are Revit's own.** Every row came back pale - 250,230,230 for the first, stepping
through near-white tints - which is what Revit assigned; nothing here chose them. On a sheet some
neighbours are hard to tell apart (Office 09 237,230,250 against Office 12 243,230,250).

| # | Check | Expected |
|---|---|---|
| **AV1** | Run the job file on a test copy: `python tools/batch-prove.py tools/jobs/apply-color-fill-scheme-heron-ai-bulding-2026-09-27.yaml` | Positive: `schemeApplied` true, "Schema 1" used as it stands, `legendsInView` 1. Negative (Walls): refused, listing what the view takes, nothing written. The run record is the proof draft |
| **AV2** | **The second route.** On the three plans above, Properties > Color Scheme | Rooms reads "Rooms by Name", Spaces and HVAC Zones read "Schema 1"; Edit Color Scheme shows the same rows and colours the reply listed. **FAIL** - "<none>" - means the read-back inside the fragment read something the view does not hold |
| **AV3** | **Where the legend lands, by eye.** Open each plan's sheet (M-101 to M-103) | One Color Fill Legend beside each plan, its top level with the top of the crop region, about 10 mm to its right. **FAIL** - the legend's top-left corner is not at that point - means `ColorFillLegend.Origin` is not the top-left, and `legendAtMm` measures from the wrong corner |
| **AV4** | **The COPY branch, proved.** On a test copy with no "Rooms by Name", run it on a plan with Rooms, Name | "Department" (or the first Rooms scheme) is copied as "Rooms by Name" and is itself unchanged - its rows and colour-by are what they were. The copy's rows fill by themselves; if they come back empty the fragment fills them and says so |
| **AV5** | **Revit 2022 - the oldest release it claims.** The job file on a 2022 test copy | The same answers. It declares 2022 to 2027 because the API does not exist before 2022 |
