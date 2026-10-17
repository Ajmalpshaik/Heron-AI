# Needs checking — Group FA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FA - sheet sets v2: sheets added to and taken out of an existing set, the print range put back, and two sets holding the same sheets flagged: `MANAGE_SHEET_SETS` and `LIST_SHEET_SETS` version 2 (2026-10-17)

Three things, from [row 5b-466](../fragment-issues/section-5b-rows-401-425.md) and two findings of
2026-10-14 on Ajmal PS's Revit 2020 job model 4355-BHVD-3D-21S10-BL001A. [Row 5b-545](../fragment-issues/section-5b-rows-401-425.md)
has the measurements. Ajmal PS starting the task on 2026-10-17 was his yes to change the PROVEN
`MANAGE_SHEET_SETS`.

**What changed.** [`MANAGE_SHEET_SETS`](../../brain/fragments/manage-sheet-sets/fragment.yaml) (FRG-SHT-015,
MODIFY) is **version 2 and DRAFT** - its proof of 2026-09-09 is kept as the record it is, and its
fingerprint no longer matches. Two new modes, `add` and `remove`, change what an EXISTING set holds under its
own name: the set is made current, its members read back, the elements given merged in or taken out, and
`ViewSheetSetting.Save()` called. A call that would change nothing is refused before `Save()` is asked
(Revit's own reference says it throws on an unchanged set), and so is one that would leave the set empty.
No new required input: they take `elements` and `setName`, as `create` does. New outputs `changed`,
`unchanged` and `printRange`. **And the print range is read first and put back** at the end, in a
`finally`, whatever happened in between; version 1 set it to "Selected views/sheets" and left it there.
[`LIST_SHEET_SETS`](../../brain/fragments/list-sheet-sets/fragment.yaml) (FRG-SHT-032, READ) is **version 2,
still DRAFT**: two sets holding exactly the same sheets and views, member for member, are each marked
"SAME sheets and views as" the other, paired in a new `sameSets`, and named in a finding.

**Declared 2020 to 2027 and compiles on all eight** (`tools/check-fragments-compile.py`). Every new Revit
member was looked up on every release with `tools/api-surface --members`. **Nothing here has run in Revit.**

**The model for every row but FA5's second half and FA6 is a NEW project**, "Heron Sheet Sets FA", made by
hand as the header of [`tools/jobs/manage-sheet-sets-v2-revit2020-2026-10-17.yaml`](../../tools/jobs/manage-sheet-sets-v2-revit2020-2026-10-17.yaml)
says: sheets A101 to A105; "HERON FA SET" = A101, A102 and the Level 1 floor plan; "HERON FA SHEETS" and
"HERON FA SHEETS COPY" = A101 and A102; Print Range left on **Current window**. Never a 4355 model for a
write.

| # | Run | Look for |
|---|---|---|
| **FA2** | **RUN FIRST.** Revit 2020, "Heron Sheet Sets FA" open: `python tools/batch-prove.py tools/jobs/manage-sheet-sets-v2-remove-revit2020-2026-10-17.yaml` - mode `remove`, all five sheets, rolled back. Its own file, because every run of one fragment writes the same record and the one signed is FA1's | positive on "HERON FA SET": `changed` 2, `unchanged` the ids of A103-A105, `memberCount` 1 (the plan left), `printRange` "Print range put back to Current window, as it was before."; negative on "HERON FA SHEETS": `done` false, `changed` 0, `refused` "Taking these out would leave 'HERON FA SHEETS' holding nothing, so nothing was taken out. To remove the whole set, use mode delete." **A set saved empty is a FAIL whatever the verdict says** |
| **FA1** | **THE PROOF.** `python tools/batch-prove.py tools/jobs/manage-sheet-sets-v2-revit2020-2026-10-17.yaml` - mode `add`, all five sheets, rolled back. When FA2 to FA4 are seen as written, read [`brain/proof-drafts/manage-sheet-sets.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept manage-sheet-sets --by "Ajmal PS"` and set `heron-status: PROVEN` | positive on "HERON FA SET": `changed` 3, `unchanged` the ids of A101 and A102, `memberCount` 6, `done` true, `printRange` put back to Current window; negative on "HERON FA NO SUCH SET": `done` false, `changed` 0, `refused` "No sheet set called 'HERON FA NO SUCH SET'." **and File - Print offers no set of that name afterwards - add never creates.** Then File - Print: every HERON FA set holds what it was made with |
| **FA3** | **By hand - the add that changes nothing.** In the Project Browser select A101 and A102 only, then `python mcp/client/heron_bridge_client.py fragment manage-sheet-sets --write --set "setName=HERON FA SHEETS" --set mode=add --set newName=` (no `--apply`: rolled back) | `done` false, `changed` 0, `unchanged` both ids, `refused` "Every sheet and view given is already in 'HERON FA SHEETS' - nothing was added." - not a Revit error about an unchanged set, which is what calling `Save()` here would have raised |
| **FA4** | **The print range - the finding of 2026-10-14.** Read off FA1 and FA2: before each, File - Print shows **Current window**; after each, and after its rollback, File - Print shows **Current window** again. Then once with the range set to **Selected views/sheets** first, FA1 again | after: the dialog on what it was before, and `printRange` saying so; with Selected first, `printRange` "Print range was already Selected views/sheets and is left there." **If the dialog shows Selected views/sheets while `printRange` says it was put back**, the dialog does not read the print manager the range was put back on: version 2 does not call `PrintManager.Apply()`, on purpose - Revit's reference says it pushes every local print setting to the global ones for all documents, a wider change than version 1 ever made. Record what was seen in row 5b-545; whether to call it is Ajmal PS's choice, not the next session's |
| **FA5** | **The read.** The third job in FA1's file - `list-sheet-sets` on "Heron Sheet Sets FA", negative Group EA's empty project "Heron Sheet Sets Empty" open beside it. Then Group EA's [EA1](group-ea.md) on BL001A, read only, run AFTER this, so its record is the one signed for `LIST_SHEET_SETS` | "HERON FA SHEETS" and "HERON FA SHEETS COPY" each marked "SAME sheets and views as" the other, `sameSets` "HERON FA SHEETS = HERON FA SHEETS COPY", a finding "(2 sheets, 0 views each)"; "HERON FA SET" - the same two sheets AND a plan - **NOT** in `sameSets`; negative: `setCount` 0, `sameSets` empty. On BL001A: if the seventh set made there on 2026-10-14 is still saved, it and the first (8 sheets, the same 8 numbers) are paired in `sameSets` |
| **FA6** | **The finding's own shape: a KEPT add, then Undo.** In a chat on "Heron Sheet Sets FA", *"add A103 to the HERON FA SHEETS set"*, **Keep** in Revit's Keep or Discard box ([Group ET](group-et.md)), then Ctrl+Z in Revit | File - Print: HERON FA SHEETS back to A101 and A102, **and the Print Range still Current window** - version 1 left it on Selected views/sheets here |
| **FA7** | **NOT RUN - Revit 2024 or later.** FA1 and FA2 on a release where a set keeps its own print order (2023+), on a set whose order is set by hand in the View/Sheet Set dialog | the same answers; and WHERE the added sheets land in a hand-ordered set - version 2 does not read or write `OrderedViewList`, so the order is Revit's choice. Record what it does |
| **FA8** | **A question, recorded, not fixed: the CURRENT set.** After FA1, which set File - Print shows as the current one - before and after the rollback | rename, delete and now add and remove make the set they work on the CURRENT set, and nothing puts the old one back (version 1 did the same for rename and delete). Record what the dialog shows; a fix is a change of its own |
