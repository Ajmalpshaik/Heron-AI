# Needs checking — Group HP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HP - SET_PARAMETER_VALUES_BY_ID v5: a level's name, and Revit's question about its views (2026-10-19)

From [row 5b-1221](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020, model *Kerala house
design*, renaming `Level 1` to `Ground Floor` with `SET_PARAMETER_VALUES_BY_ID` met Revit's
`TaskDialog_Confirm_Level_Rename` (*"Would you like to rename corresponding views?"*); Heron declined it
and the job was rolled back. Changing the level's Elevation was kept.

**What changed.** [D-145](../DECISIONS.md#d-145--a-card-may-let-its-caller-answer-one-named-revit-dialog-for-one-job-and-every-other-dialog-is-still-declined):
`set-parameter-values-by-id` version 5 takes an optional `renameViews`. `yes` - the add-in answers that
one dialog Yes for that one job; `no` - No; left out - a level's name is refused by the fragment before
anything is written, naming `renameViews`. Every other dialog is still declined.

**What is done and what is not.** It compiles for 2020 to 2027 (`tools/check-compile.py`,
`tools/check-fragments-compile.py`), and `tests/test_dialog_note.py` checks the rule and the wiring.
**Nothing here has run in Revit.** The card is DRAFT until HP1 to HP4 pass and the owner signs; version
3's proof stays on the card as a record and is stale. Version 5 is built on version 4 ([Group HZ](group-hz.md)), which is also unproven: run HZ first or alongside, on the same scratch copy. Run on a **scratch copy** of a project with
`Level 1` and `Level 2` and the default plan and ceiling views named after them (`Level 1` floor plan,
`Level 1` ceiling plan) - never a working model. Rebuild and redeploy the add-in first: the change is in
the add-in as well as the card.

**Rolled back, every case.** Run each through the proof route (`tools/batch-prove.py` or the bridge's
`prove`), which runs inside a transaction group and rolls it back - so nothing is kept. Read the reply's
`dialogs` list on every run: it names the dialog id Revit raised and the answer Heron gave.

| # | Run | Look for |
|---|---|---|
| **HP1** | *Positive - rename with the views.* `rows=key=Name\|in=Levels;Level 1\|Name=Ground Floor`, `renameViews=yes` | `written` 1; `readBack` shows `Ground Floor`; the reply's `dialogs` lists `TaskDialog_Confirm_Level_Rename` **answered Yes**, and the job is **not** refused. Before the rollback, read the view names (a `revit_views` read in a setup step or the readBack): the `Level 1` floor plan and ceiling plan read `Ground Floor`. Revit reported the group ROLLED BACK; after it, Level 1 and its views have their old names |
| **HP2** | *Positive - the level alone.* The same rows, `renameViews=no` | `written` 1, the level reads `Ground Floor`, the dialog answered **No**, and the views keep `Level 1`. **Say what Revit did** if No renamed nothing at all - then No is not "the level alone" on this release, and the card's words are wrong |
| **HP3** | *Negative - left out.* The same rows, no `renameViews` | **Nothing written:** `written` 0, `refused` one row naming `renameViews`, and **no** dialog in the reply's `dialogs` (the fragment refused before Revit could ask). The level keeps its name |
| **HP4** | *Negative - a word it does not take.* The same rows, `renameViews=maybe` | Refused before the run: `bad_request_value`, *"'maybe' is not an answer to it - say no or yes"*. Nothing ran, no dialog |
| **HP5** | *The other door - recorded, not part of HP's verdict.* `RENAME_ELEMENTS` with Level 1 selected, `find=Level 1`, `replaceWith=Ground Floor` | **Say whether** `TaskDialog_Confirm_Level_Rename` appears in `dialogs`. If it does, `RENAME_ELEMENTS` has the same defect and wants the same input (row 5b-1221 records it, not fixed) |

**Then sign** `set-parameter-values-by-id` on HP1 and HP3. HP2 and HP4 are the cases its
`tests/cases.yaml` names beside them. **Only then, and only if the owner asks,** rename the real model's
levels with `renameViews` as they choose.
