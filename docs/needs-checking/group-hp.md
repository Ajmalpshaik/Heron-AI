# Needs checking — Group HP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HP - SET_PARAMETER_VALUES_BY_ID v5: a level's name, and Revit's question about its views (2026-10-19)

**Tracked as GitHub issue [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

From [row 5b-1221](../fragment-issues/section-5b-rows-1201-1225.md). On Revit 2020, model *Kerala house
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
| ~~**HP1**~~ | Positive - rename with the views. `rows=key=Name\|in=Levels;Level 1\|Name=Ground Floor`… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | `written` 1; `readBack` shows `Ground Floor`; the reply's `dialogs` lists… | | |
| ~~**HP2**~~ | Positive - the level alone. The same rows, `renameViews=no` — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | `written` 1, the level reads `Ground Floor`, the dialog answered No, and the views keep… | | |
| ~~**HP3**~~ | Negative - left out. The same rows, no `renameViews` — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **Nothing written:** | | |
| ~~**HP4**~~ | Negative - a word it does not take. The same rows, `renameViews=maybe` — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Refused before the run: `bad_request_value`, "'maybe' is not an answer to it - say no or… | | |
| ~~**HP5**~~ | *The other door - recorded, not part of HP's verdict.* `RENAME_ELEMENTS` with Level 1 selected, `find=Level 1`, `replaceWith=Ground Floor` | **Say whether** `TaskDialog_Confirm_Level_Rename` appears in `dialogs`. If it does, `RENAME_ELEMENTS` has the same defect and wants the same input (row 5b-1221 records it, not fixed) **RECORDED 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0), *House Scratch 2* - a copy of `rac_basic_sample_project.rvt` (Level 1 with its plan view 312) in `%TEMP%\heron-work\issues-1120`, `SET_PARAMETER_VALUES_BY_ID` version 9, `rows=key=Name|in=Levels;Level 1|Name=Ground Floor`, for issue [#982](https://github.com/Ajmalpshaik/Heron-AI/issues/982), rolled back:** `RENAME_ELEMENTS` (now DEPRECATED, still runnable from the command line) on level 311 bound by `FILTER_ELEMENTS_BY_ID`, `find=Level 1 replaceWith=Ground Floor`, rolled back: renamed 1, **no `dialogs` in the reply**, and 180 elements modified - the count of HP1's Yes run, not HP2's No (179) - so Revit seems to have renamed the views too without Heron seeing a box. Recorded, as the row asks; whatever card took RENAME_ELEMENTS's job over is where row 5b-1221's question now lies. |

**Then sign** `set-parameter-values-by-id` on HP1 and HP3. HP2 and HP4 are the cases its
`tests/cases.yaml` names beside them. **Only then, and only if the owner asks,** rename the real model's
levels with `renameViews` as they choose.
