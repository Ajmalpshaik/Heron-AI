# Needs checking — Group OP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OP - the owner's answers of 2026-11-19/20 in code: an empty `absent:` binds, replies carry dictionary entries whole, three one-reason refusals become one string, and a rollback that cannot take a job back says so (2026-11-20)

**Tracked as GitHub issue [#1218](https://github.com/Ajmalpshaik/Heron-AI/issues/1218) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

The owner answered four rows in their GitHub issues, and one pull request made each change:
[row 5b-1841](../fragment-issues/row-5b-1841.md) (issue #1041, answer A),
[row 5b-194](../fragment-issues/section-5b-rows-176-200.md) and
[row 108](../fragment-issues/section-5-rows-101-125.md) (issue #912), and
[row 5b-1594](../fragment-issues/row-5b-1594.md) (issue #911). **Nothing here has met a Revit.** Built
and tested in a cloud container on 2026-11-20 - the add-in compiled for every release 2020 to 2027, every
fragment compiled on every release it claims, and the suites named in each row pass - which is not a
proof (D-30).

**Before any row: the add-in must be rebuilt and Revit restarted.** Rows OP1 to OP5 change the binder
(`HeronBindingNote.DeclaredAbsent`), OP9 the reply (`RevitFragment.Report`) and OP10 to OP12 the verdict
(`RevitFragment.WithVerdict`) - all C# in `revit/`. Bring the checkout up to `main`, run
`python tools/heron-ready.py`, and restart Revit, so the add-in it loads is this one. A Revit still
running the old add-in refuses OP1 to OP5 exactly as before, and that is not a failed check.

**The model, every row:** a scratch copy, never a live project (OP6 needs a workshared one). **No row
signs a proof**: OP6 to OP8 are DRAFT cards whose proof is re-taken the usual way (the
[fragment-proving](../../.claude/skills/fragment-proving/SKILL.md) skill), and the five cards of OP1 to
OP5 stay PROVEN - their C# did not move; each owes one run with the value left out, to see it bind.

| Row | What to check | Pass when |
|---|---|---|
| **OP1** | *`create-paths-of-travel` (PROVEN) - `rooms` and `toDoor` LEFT OUT (row 5b-1841).* Through a chat, `revit_change` CREATE_PATHS_OF_TRAVEL with `view=Level 1`, `fromPoints=20000,20000`, `toPoint=30000,20000` and nothing else, on a floor plan with room for one path ([Group OI](group-oi.md)'s OI5 arrangement). **Negative:** `fromPoints` left out as well, so there is no start | Positive: it RUNS - no *"'rooms (string)', 'toDoor (string)' are values the CALLER supplies"*; the `bound` line names `rooms` and `toDoor` *"not given - \"\", the card's own default (optional)"*; one path is drawn. Negative: the card's OWN refusal that no start was given and nothing is drawn - never the binder's *"values the CALLER supplies"* |
| **OP2** | *`create-project-reference-planes` (PROVEN) - `planes` LEFT OUT.* `datum=Level 1`, `spacingMm=600`, `count=2`, in a section, and nothing else. **Negative:** `datum` left out as well | Positive: it RUNS and makes 2 reference planes 600 and 1200 mm above Level 1, the `bound` line naming `planes` as the card's own default. Negative: nothing is made and the grids and levels are LISTED - what the card says leaving `datum` out means - not a refusal for a missing value |
| **OP3** | *`dimension-views-by-rules` (PROVEN) - `dimensionType` LEFT OUT.* OI8's arrangement: two parallel ducts at z 1000, the same `rules`, no `dimensionType`. **Negative (the binder's own):** `rules` left out - it is NOT marked optional | Positive: `newDimensions` 1, made with the project's DEFAULT linear dimension type (read it in Revit: Type Properties), the `bound` line naming `dimensionType` as the card's own default. Negative: refused before anything runs, *"'rules (string)' is a value the CALLER supplies"* - an empty `absent:` frees only a need the card marks optional |
| **OP4** | *`report-paths-of-travel` (PROVEN) - `room` LEFT OUT.* Through a chat, `revit_read` REPORT_PATHS_OF_TRAVEL on OP1's plan with nothing given but the view - *"report the paths of travel on Level 1"*. **Negative:** `room=999`, a number no room has | Positive: `pathCount` 1, every path in the plan - no *"'room (string)' is a value the CALLER supplies"*. Negative: `pathCount` 0, or the card's own words that no room answers to 999 |
| **OP5** | *`set-view-work-plane` (PROVEN) - `display` LEFT OUT.* `view=Level 1`, `plane=Level 1`, nothing else. **Negative:** both `plane` and `display` left out | Positive: the work plane is Level 1 (Architecture, Work Plane, Set shows it) and Show Work Plane is as it was. Negative: it runs and says nothing was changed - neither was asked - and the view's work plane and its display are as they were |
| **OP6** | *`rename-workset` v2 (DRAFT) - `refused` is one string (row 108).* A workshared scratch copy: rename `Workset1` to `HERON PROOF WS`. **Negative:** `newName` = the name of another workset | Positive: `renamed true`, the new name read in Collaborate, Worksets. Negative: `renamed false` and `refused` WHOLE on its own line - *"... finding that out mid-rename is worse than being told now"* to its last word - never `1 item(s) [...]` cut at 60 characters |
| **OP7** | *`set-view-section-box` v2 (DRAFT) - `refusalReasons` is one string.* A 3D view, one element, `marginMm=500`. **Negative:** the same on a floor plan | Positive: `applied true`, `enclosed 1`, the box visible round it. Negative: `viewRefused true`, nothing changed, and `refusalReasons` WHOLE - ending *"... ISOLATE_ELEMENTS"* |
| **OP8** | *`set-view-template-control` v2 (DRAFT) - `refused` is one string.* A template holding View Scale, `parameterNames=View Scale`, `include=false`. **Negative:** an ordinary view passed instead of the template | Positive: View Scale in `nowFree`, and the Scale row editable on a view following the template. Negative: nothing changed and `refused` WHOLE - ending *"... rather than reported as done"* |
| **OP9** | *A dictionary entry reaches the chat WHOLE (row 5b-194).* `revit_read` DESCRIBE_ELEMENTS on two elements whose description is longer than 60 characters - a fan coil unit's *"Mechanical Equipment \| <family> : <type> \| id N"* is. Then `heron_run_record` on a KEPT change's `record:` line. **Negative:** a dictionary whose values are all short - READ_ELEMENT_PARAMETERS of one Yes/No parameter | Positive: the summary line is as before - `described 2 entry(ies)` - and under it each entry prints whole, `\| id N` at its end; the kept change's `record:` line reads *"(every id it changed, and each value as the add-in sent it)"*. Negative: its entries print exactly as before - nothing a short value showed has moved |
| **OP10** | *A Publish job sent without apply (row 5b-1594).* Changes and Publish on: `heron_bridge_client.py fragment export-schedule-to-csv --write --set ...` with no `--apply`, a scratch schedule into `%TEMP%\heron-work`. **Negative:** a MODIFY card the same way - `fragment rename-workset --write`, no `--apply` | Positive: the verdict begins *"A ROLLBACK CANNOT TAKE ALL OF THIS BACK. 'export-schedule-to-csv' ran for real as a Publish job"*, says what it did outside the group STILL STANDS and not to send it again with apply; the reply carries `outsideStands true`, `rolledBack true`; the CSV is on disk; NO second "NOT TAKEN BACK" line under it. Negative: *"NOTHING WAS KEPT ... Send it again with apply to keep it"* as before, `outsideStands false` |
| **OP11** | *An Admin job sent without apply.* Admin on: `fragment create-family-document --write --set ...`, no `--apply`, on a scratch model | The verdict says *"... ran for real as an Admin job"* and that what it did outside the group still stands, `outsideStands true`; write down whether the new family document is still open after the rollback - that is what the sentence is for |
| **OP12** | *A card that runs with no transaction open, sent without apply.* `fragment open-view --write --set view=Level 2`, no `--apply`, with another view in front | The verdict says *"... ran for real as a job that runs with no transaction open (noTransactionOpen)"*, `outsideStands true`, and Level 2 is the view in front after it - the rollback did not take it back |
