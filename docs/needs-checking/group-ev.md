# Needs checking — Group EV

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EV - what a write changed, counted from Revit's own DocumentChanged, and the run record that keeps every id (2026-10-17)

Two family builds on 2026-10-09 carried 322,725 characters of `revit_change` replies back into the
chat, about 70% of it repeated: the fragment's helpers and the same KEPT paragraph, word for word
([row 5b-407](../fragment-issues/section-5b-rows-401-425.md)). Nothing in Heron tracked a change at all.
(EJ to ET were taken by other groups, merged or open, when this was written, so this group is EV.)

**What changed.** `RevitFragment` collects what `DocumentChanged` reports from `group.Start()` until it
describes the job - before the Keep-or-Discard box asks and before Assimilate or the rollback - through
`RevitChangedSet`, the collector D-126's Keep-or-Discard box already uses (Group ET), extended rather
than a second beside it; it counts only `TransactionCommitted` on the target document and reads nothing
inside the event. `HeronChangeReport` counts each element once per part,
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
code, printed 213,332 characters through the real `revit_change` on `main` (ec400a37, re-measured after D-126) and 160,364 on
this change, 24.8% less (`tests/test_change_reporting.py` replays the same set without the SDK). An add-in from
before the change, replying to this server, prints byte for byte what it printed before. The 2026-10-09
transcript is on the owner's PC, so the figure for a real build is EV6's.

**THE ADD-IN MUST BE REBUILT AND DEPLOYED FOR EACH RELEASE AND REVIT RESTARTED**, and the Claude app
restarted for the server. Every row on a **scratch copy**, never a working model.

| # | Run | Look for |
|---|---|---|
| ~~**EV1**~~ | Inner commits inside a group. Changes ON, on a scratch copy of a project in Revit 2024: `revit_change` capability `CREATE_TEXT_NOTE` with a view, a point and `noteText=HERON EV1` | The reply's `changed:` line counts at least one added element under `Text Notes` and the reply ends with the KEPT paragraph whole; `heron_run_record` with the reply's `run` lists the note's id as `fragment added`. The audit line for that workflow (`%APPDATA%\Heron\audit\audit-<yyyymm>.jsonl`) carries `changeEvents` 1 or more and `elementsAdded` 1 or more. **FAIL:** *"changed: not counted - Revit reported no change event"* - the event did not reach a commit inside the group, and every count stays absent - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18, with `CREATE_DUCT` - the results table under *The 2026-10-18 run* below. |
| **EV2** | A setup step's commits, counted apart. On a test copy of `test projject`, prepared as the job file's header says: `python tools/batch-prove.py tools/jobs/set-curtain-wall-grid-test-projject-2026-09-24.yaml --session <pid>` (both legs roll back). **Changed 2026-10-17:** this row named the revision renumber job, whose `create-revision` setup step `validate` now refuses ([row 5b-917](../fragment-issues/section-5b-rows-401-425.md)); its revision is made by hand now, so it has no setup step that changes the model | Read the newest records with `heron_run_record`: the positive leg's record lists the wall `create-wall` made as `setup added` and the change `set-curtain-wall-grid` made as `fragment modified`, and its verdict says setup steps are *counted apart under changes*. **FAIL:** the setup's wall listed under `fragment` - the transaction name did not carry `label - stepName` as RunSetupSteps sets it |
| **EV3** | On a group's rollback. The same run as EV2 | Each leg's reply says *NOTHING WAS KEPT* and its counts are what the job did before the rollback - the revision listed as added, not deleted; the model holds no revision `HERON PROOF later` afterwards. **FAIL:** an element listed as deleted that the job added - the rollback's own event was counted, so `Describe` did not seal before `SafeRollBack(group)` |
| **EV4** | An edit-scope fragment's own transactions (D-112). On a scratch copy with a level pair: `revit_change` capability `CREATE_STAIRS`, the values its card asks for | `changed:` counts the stair and its runs and landings under the fragment - the transactions the fragment opened itself are not its setup's. **FAIL:** *not counted* while the stair exists - the event did not reach an edit scope's commits; say so on row 5b-407 |
| ~~**EV5**~~ | Categories read before Assimilate. EV1's and EV4's replies | Real category names in `changed:` - `Text Notes`, `Stairs` - not every element under `(no category)`. **FAIL:** everything under `(no category)` with elements that plainly have one - `GetElement` or `Category` could not be read with the group still open, and the read has to move - **Struck 2026-10-18:** PASS on Revit 2024 and 2027, 2026-10-18 - the results table under *The 2026-10-18 run* below. |
| **EV6** | *A measurement, not a proof.* A family build of 20 forms or more in one chat, on Revit 2024 | The `revit_change` replies' characters against the 2026-10-09 build's (row 5b-407: 322,725 across 172 replies): count them from the chat's transcript as that row did, and note how many batches printed the KEPT paragraph whole. Write the number on row 5b-407 whatever it is |
| **EV7** | The prune. Set `runs.retainDays = 1` in `%APPDATA%\Heron\config\heron.config`, leave a record older than a day in `%LOCALAPPDATA%\Heron\runs`, restart Revit | The old record is gone, today's are kept, and nothing under `%APPDATA%\Heron` was touched. Put the setting back |

### The 2026-10-18 run

**On the owner's laptop, 2026-10-18.** Revit 2024.3 session 63712, and for the rows marked 2027 Revit 2027.3
session 22024, both on add-in `0.1.0+48230c8c` - `main`, Debug, deployed 08:45 local by this run with
`tools/deploy-addin.ps1` for 2024, 2026 and 2027. Revit 2020 session 55496 belonged to another chat and ran
`ab77d5fb`; **nothing here ran on Revit 2020.** Models, all scratch copies in `%TEMP%\heron-work\safety-1018\models`:
*Heron Safety 2024* (Autodesk's `rme_basic_sample_project.rvt`, via the move-rails chat's *Rails-X2*; 21,562
elements at open), *Heron Guard B 2024* (a copy of *Rails-S*, the Dynamo sample) and *Heron Safety 2027* (Revit
2027's `Snowdon Towers Sample HVAC.rvt`, links unresolved; 9,813 elements). Changes, Admin and Publish on except
where a row says. **The owner pressed every Keep, Discard, Esc, X and Undo himself;** nothing answered a box for
him. Undo lists were read with UI Automation, read-only. Throwaway probe fragments were made in `brain/fragments/`
for the session and deleted by name at the end - none is committed.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| EV1 | 2024 | With `CREATE_DUCT`, not `CREATE_TEXT_NOTE`, kept through the box: *"changed: 3 added ((no category) 1, Duct Systems 1, Ducts 1), 3 modified ((no category) 3)"* and a `record:` line; `heron_run_record` listed 891698, 891699, 891700 as `fragment added`; the audit line carried `elementsAdded 3`, `elementsModified 3`, `changeEvents 1`. Kept on 2027 too (`CREATE_LEVEL`, *"1 added (Levels 1)"*) | **PASS** |
| EV3 | 2024 | Not on the job file; on every rolled-back preview this day the counts were what the job did - *"added 6"*, none listed deleted - and the model held none of it afterwards | **part** (seen, not the row's job) |
| EV5 | 2024, 2027 | Real names: Ducts, Duct Systems, Levels, Text Notes; one element of each duct job under *(no category)* | **PASS** |
| EV2, EV4, EV6, EV7 | - | Not arranged | **NOT RUN** |

The batch rule seen on 2027: the second `CREATE_LEVEL` reply printed *"and 4 values unchanged from the last
reply - every one is in the record."*
