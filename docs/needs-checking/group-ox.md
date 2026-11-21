# Needs checking — Group OX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OX - a job that works only outside the model opens no transaction group in it (D-192): a new family from a chat, and an export of families - BUILT, NOT RUN IN REVIT (2026-11-20)

**Tracked as GitHub issue [#1142](https://github.com/Ajmalpshaik/Heron-AI/issues/1142) since 2026-11-20** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [D-192](../decisions/D-192.md), for issues [#1121](https://github.com/Ajmalpshaik/Heron-AI/issues/1121)
(row 5b-1912: a new family opened a write in the pinned project) and
[#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) (rows 21, 5b-1407 and 5b-1411: Revit refused
`EditFamily` inside Heron's group). A card that declares `noGroupOpen` runs, as an Admin or a Publish job,
with no `TransactionGroup` in the model it is aimed at, measured before and after. Built in a cloud container
with no Revit: it compiles for every release (`tools/check-compile.py`, the .NET 10 SDK), the dialog sentence
runs in `tests/Heron.DialogNote.TestHost`, and `tests/test_outside_the_model.py` reads the rest as text -
**none of it has met a Revit**. Rebuild the add-in and restart Revit first (`python tools/heron-ready.py`):
an add-in built before this refuses both cards (`needs_unbound`), and nothing is changed by that.

| Change | Files | Row | Checks |
|---|---|---|---|
| A new family opens no group in the pinned project, and opens in a window in one call | `RevitFragment.cs`, `create-family-document/fragment.yaml` (version 4) | [5b-1912](../fragment-issues/row-5b-1912.md) | OX1, OX2 |
| Export families reaches `EditFamily` | `RevitFragment.cs`, `export-families/fragment.yaml` (version 2) | [21](../fragment-issues/section-5-rows-001-025.md), [5b-1407, 5b-1411](../fragment-issues/section-5b-rows-1401-1425.md) | OX3, OX4 |
| The refusals and the words | `RevitFragment.cs`, `HeronDialogNote.cs`, `heron_bridge_client.py` | - | OX5 |

**The models: scratch copies, never a working model.** For OX1 and OX2 a scratch copy of a sample project
in `%TEMP%\heron-work\ox`, pinned in the chat; for OX3 and OX4 the scratch copies of rows 5b-1407 and 5b-1411 -
`P1102 MEP` (Revit 2020's `rme_advanced_sample_project.rvt`) and `Snowdon Towers Sample Structural` (Revit
2024) - with an empty folder `%TEMP%\heron-work\ox\families` to export into. Admin on for OX1 and OX2,
Publish on for OX3 and OX4; read the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **OX1** | *A new family with a project pinned and in front, positive* - pin the chat to the scratch project; `revit_change CREATE_FAMILY_DOCUMENT` with `templatePath` Metric Generic Model and `savePath` `%TEMP%\heron-work\ox\OxFam1.rfa`; then Revit's Undo list in the project | The family opens in a window **in this one call** (no *"IS SAVED, NOT YET IN A WINDOW"*); the reply's verdict says *"RAN OUTSIDE THE MODEL"*, `ranOutsideTheModel` true, `elementsCounted` the project's count, and `outcome: KEPT`; the project's Undo list has **no** *Heron: create-family-document* entry and the project is not marked changed. On Heron's Activity pane the row reads *Kept*; if an Undo button stands beside it, pressing it is refused (*undo_refused* - it is not Heron's last change there) and nothing is undone - write down which. **Negative:** `savePath` an existing file - refused by the card, no family made, no window, the project unchanged |
| **OX2** | *The same with a different model in front* - pinned to the scratch project, a second scratch model in front | As OX1: one call, a window, no Undo entry in either model. **Negative:** an add-in built before 2026-11-20 - write down that it answers `needs_unbound` naming `noGroupOpen`, and that nothing was made |
| **OX3** | *Export families on Revit 2020, positive* - row 5b-1407 again on `P1102 MEP`: `EXPORT_FAMILIES` with `nameContains` `M_Rectangular Elbow` and the empty folder, through `validate --write --allow-publish` (a proving run, sent as `run_fragment_publish`, no apply) | `saved` 2, two `.rfa` files in the folder, `refused` empty - not *"Revit refused to open it for editing"*; the verdict *"A ROLLBACK CANNOT TAKE ALL OF THIS BACK ... no transaction group open"*, `outsideStands` true; the proof draft says *"run with NO transaction group open"*; the project unchanged (count, not marked changed). **Negative:** `nameContains ZZZNOTHINGHERE` - `saved` 0, nothing written |
| **OX4** | *Export families on Revit 2024* - row 5b-1411 again on `Snowdon Towers Sample Structural`, `nameContains` `Concrete-Rectangular-Column` | `saved` 1, the file in the folder, the project unchanged. **Negative:** as OX3's |
| **OX5** | *The refusals* - a scratch card that declares `noGroupOpen` and is `risk: MODIFY`, sent through `revit_change`; then one that declares it beside `editScopeFailures` as ADMIN | Each `bad_contract`, *"Nothing was run; the card needs correcting"*, the model unchanged. **Negative:** OX1's card - runs |
