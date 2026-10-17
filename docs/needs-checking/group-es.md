# Needs checking — Group ES

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group ES - what a write changed, counted from Revit's own DocumentChanged, and the run record that keeps every id (2026-10-17)

Two family builds on 2026-10-09 carried 322,725 characters of `revit_change` replies back into the
chat, about 70% of it repeated: the fragment's helpers and the same KEPT paragraph, word for word
([row 5b-407](../fragment-issues/section-5b-rows-401-425.md)). Nothing in Heron tracked a change at all.
(EJ to ER were taken by other groups, merged or open, when this was written, so this group is ES.)

**What changed.** `RevitFragment` listens to `DocumentChanged` from `group.Start()` until it describes
the job, just before Assimilate or the rollback; it counts only `TransactionCommitted` on the target
document and reads nothing inside the event. `HeronChangeReport` counts each element once per part,
tells a setup step's transaction (`label - stepName`) from the fragment's (`label`), and reads each
surviving element's `Category.Name` once. The reply carries `changes` - counts by category, the first
20 ids per part, the totals, and `witnessed` - beside `elementsAdded`, `elementsModified`,
`elementsDeleted` and `changeEvents`, which the dispatcher copies onto the audit line as numbers. The
dispatcher writes every element and the whole reply to `%LOCALAPPDATA%\Heron\runs\<workflow id>.json`
(`HeronPaths.Runs`, DERIVED, pruned at start-up by `runs.retainDays`, 14 by default) and adds `run` to
the reply only when the file was written. `revit_change` prints two short change lines and a `record:`
line, prints the add-in's KEPT paragraph once per batch and its one-sentence form - the undo rule
whole - on the rest, and counts the values the last reply of the same capability showed word for word.
`heron_run_record` (READ, no operation) pages a record from disk.

**What is compiled, run and not run.** The add-in and every test host compile for all eight releases
2020 to 2027 (`tools/check-compile.py`, 2026-10-17), and every member the collector uses is the same on
all eight (`tools/api-surface --members DocumentChangedEventArgs`, `UndoOperation`). The counting rule
runs in `tests/Heron.ChangeReport.TestHost`; the record writer and its prune run in
`tests/Heron.Kernel.TestHost`; the paging, the change lines and the batch rule run in
`tests/test_run_records.py`; the real `revit_change` and `heron_run_record` run through an MCP SDK
against a stand-in Revit in `tests/test_mcp_serves.py`. **None of it has run in Revit.** Nothing here
is PROVEN, and no claim about what Revit raises is made from these suites.

**A replay, not a chat.** 172 kept changes in 39 batches, shaped from `CREATE_FAMILY_EXTRUSION`'s own
code, printed 213,332 characters through the real `revit_change` on `main` (80f4fcd) and 160,364 on
this change, 24.8% less (`tests/test_change_reporting.py` replays the same set without the SDK). An add-in from
before the change, replying to this server, prints byte for byte what it printed before. The 2026-10-09
transcript is on the owner's PC, so the figure for a real build is ES6's.

**THE ADD-IN MUST BE REBUILT AND DEPLOYED FOR EACH RELEASE AND REVIT RESTARTED**, and the Claude app
restarted for the server. Every row on a **scratch copy**, never a working model.

| # | Run | Look for |
|---|---|---|
| **ES1** | Inner commits inside a group. Changes ON, on a scratch copy of a project in Revit 2024: `revit_change` capability `CREATE_TEXT_NOTE` with a view, a point and `noteText=HERON ES1` | The reply's `changed:` line counts at least one added element under `Text Notes` and the reply ends with the KEPT paragraph whole; `heron_run_record` with the reply's `run` lists the note's id as `fragment added`. The audit line for that workflow (`%APPDATA%\Heron\audit\audit-<yyyymm>.jsonl`) carries `changeEvents` 1 or more and `elementsAdded` 1 or more. **FAIL:** *"changed: not counted - Revit reported no change event"* - the event did not reach a commit inside the group, and every count stays absent |
| **ES2** | A setup step's commits, counted apart. On the revision test copy: `python tools/batch-prove.py tools/jobs/sheet-issues-revisions-renumber-2026-09-28.yaml` (both legs roll back) | Read the newest records with `heron_run_record`: the positive leg's record lists the revision `create-revision` made as `setup added` and the change `edit-revision` made as `fragment modified`, and its verdict says setup steps are *counted apart under changes*. **FAIL:** the setup's revision listed under `fragment` - the transaction name did not carry `label - stepName` as RunSetupSteps sets it |
| **ES3** | On a group's rollback. The same run as ES2 | Each leg's reply says *NOTHING WAS KEPT* and its counts are what the job did before the rollback - the revision listed as added, not deleted; the model holds no revision `HERON PROOF later` afterwards. **FAIL:** an element listed as deleted that the job added - the rollback's own event was counted, so `Describe` did not seal before `SafeRollBack(group)` |
| **ES4** | An edit-scope fragment's own transactions (D-112). On a scratch copy with a level pair: `revit_change` capability `CREATE_STAIRS`, the values its card asks for | `changed:` counts the stair and its runs and landings under the fragment - the transactions the fragment opened itself are not its setup's. **FAIL:** *not counted* while the stair exists - the event did not reach an edit scope's commits; say so on row 5b-407 |
| **ES5** | Categories read before Assimilate. ES1's and ES4's replies | Real category names in `changed:` - `Text Notes`, `Stairs` - not every element under `(no category)`. **FAIL:** everything under `(no category)` with elements that plainly have one - `GetElement` or `Category` could not be read with the group still open, and the read has to move |
| **ES6** | *A measurement, not a proof.* A family build of 20 forms or more in one chat, on Revit 2024 | The `revit_change` replies' characters against the 2026-10-09 build's (row 5b-407: 322,725 across 172 replies): count them from the chat's transcript as that row did, and note how many batches printed the KEPT paragraph whole. Write the number on row 5b-407 whatever it is |
| **ES7** | The prune. Set `runs.retainDays = 1` in `%APPDATA%\Heron\config\heron.config`, leave a record older than a day in `%LOCALAPPDATA%\Heron\runs`, restart Revit | The old record is gone, today's are kept, and nothing under `%APPDATA%\Heron` was touched. Put the setting back |
