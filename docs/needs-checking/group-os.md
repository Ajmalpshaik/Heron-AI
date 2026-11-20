# Needs checking — Group OS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OS - the add-in's own fixes for issues #932, #933, #934 and #1057: the move quotes Revit under one workflow id and tells template-born models apart, a refusal says whether its rollback held, a run still waiting names its run, an undo reaches the run record, the bridge caps an answer, the chain belongs to a model - BUILT, NOT RUN IN REVIT (2026-11-20)

**Tracked as GitHub issue [#1104](https://github.com/Ajmalpshaik/Heron-AI/issues/1104) since 2026-11-20** - tick the group in that issue when the checks are done. Every result stays in this file.

**Why:** on 2026-11-20 the coordinating session gave one session the add-in core, the bridge and run
records - issues [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932),
[#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933),
[#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) and
[#1057](https://github.com/Ajmalpshaik/Heron-AI/issues/1057). Every change below is C# in `revit/` or
`platform/`, a card line, or a reader of the run record, and **none of it has met a Revit**. It compiles
for every release (`tools/check-compile.py`, 2026-11-20, a cloud container with the .NET 10 SDK), and the
parts that touch no Revit type are run without one by the test hosts named on each row. The add-in must be
rebuilt and Revit restarted before any row below can be run (`python tools/heron-ready.py`).

| Change | Files | Row | Checks |
|---|---|---|---|
| A refused move quotes Revit and says it was rolled back | `RevitWrite.cs` | [5b-1062](../fragment-issues/section-5b-rows-1051-1075.md) | OS1 |
| One move, one workflow id on both audit lines | `RevitWrite.cs` | [5b-1061](../fragment-issues/section-5b-rows-1051-1075.md) | OS2 |
| The move's model key carries the model's own CreationGUID | `RevitWrite.cs` | [5b-1060](../fragment-issues/section-5b-rows-1051-1075.md) | OS3 |
| `awaiting_modeller` and `still_running` carry the run's workflow id | `RevitDispatcher.cs`, `Json.cs` | [5b-1159](../fragment-issues/section-5b-rows-1151-1175.md) | OS4, OS5 |
| A refusal after a rollback carries `rolledBack` and `rollbackReported`; a read that stopped carries `elementsCounted` | `RevitDispatcher.cs`, `RevitFragment.cs`, `Json.cs` | [5b-1559](../fragment-issues/section-5b-rows-1551-1575.md) | OS6 |
| A kept run's undo and redo are stamped on its run record | `HeronUndoGuard.cs`, `RevitUndo.cs`, `RevitDispatcher.cs`, `HeronRuns.cs`, `heron_run_records.py` | [5b-1163](../fragment-issues/section-5b-rows-1151-1175.md) | OS7 |
| The bridge refuses an answer over 16 MiB, with its size and run record | `BridgeServer.cs` | [5b-1167](../fragment-issues/section-5b-rows-1151-1175.md) | OS8 |
| The chain is matched to a model by its key, not its title | `RevitFragment.cs` | [5b-1164](../fragment-issues/section-5b-rows-1151-1175.md) | OS9 |
| One element id handed to a need that wants a list is a list of one | `RevitFragment.cs` | [5b-1406](../fragment-issues/section-5b-rows-1401-1425.md) | OS10 |
| `COPY_VIEW_FILTERS`' `onlyFilterNamed` may be left out | `copy-view-filters/fragment.yaml` | [5b-227](../fragment-issues/section-5b-rows-226-250.md), [101](../fragment-issues/section-5-rows-101-125.md) | OS11 |
| `editScopeFailures` is described as *opens its own transactions* | `RevitFragment.cs`, `tools/check-revit-gate.py` | [5b-1557](../fragment-issues/section-5b-rows-1551-1575.md) | OS12 |
| Issue #1057's three lessons a Revit has to settle | - | [row 5b-1904](../fragment-issues/row-5b-1904.md) | OS13, OS14, OS15 |

**Proved off Revit, 2026-11-20:** `tests/test_failure_note.py` (its host now runs `Json.Error` with fields,
`Json.HasKey`, `Json.WithRollback` and `Json.Set`), `tests/test_undo_last.py` (its host runs the guard's
`RunMoved`), `tests/test_run_records.py` section 11, `tests/test_bridge_roundtrip.py` (an over-size answer,
from its host's new `test_big`), `tests/test_chain_expectation.py`, `tests/test_binding_note.py` and
`tests/test_read_door.py` section 14 - each new check seen to FAIL on the code as it stood. **Read as text,
not run:** every line of it that calls the Revit API.

**The models, every row: scratch copies, never a working model.** *Heron D3 move test* for OS1 to OS3 -
the copy of `rme_basic_sample_project.rvt` Group D and Group G used, at `%TEMP%\heron-work\d3` - and a
second new project from the same default template beside it, saved as `or-template-twin.rvt`; for OS9 two
copies of one small project saved under **one file name** in two folders, `%TEMP%\heron-work\or\a\Twin.rvt`
and `%TEMP%\heron-work\or\b\Twin.rvt`; for OS13 a copy of `Snowdon Towers Sample HVAC.rvt` with its links
beside it. Revit 2024 or later for OS3 (2020 to 2023 have no `CreationGUID`, and the key is then what it
always was). **OS13 deletes and unloads a link: run it on the Snowdon copy only, and only after the owner
has said yes for that run.** Read the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **OS1** | *Move refused by Revit, positive* - Group G's G1 again: on *Heron D3 move test* preview its 935 duct fittings up 200 mm, then approve | `move_failed` whose message begins *"Revit would not let it through: "* with Revit's own words and an element count, then *"The move was rolled back completely, so nothing was moved and the model is as it was."*; the audit line's `reason` holds the same words; the fittings' box re-read unchanged. **Negative:** the three ducts G4 moved, up 200 mm and kept - `moved 3`, no *"would not let it through"* anywhere, one undo entry |
| **OS2** | *One workflow id, positive* - after OS1's negative, read `%APPDATA%\Heron\audit\audit-<yyyyMM>.jsonl` | The move wrote its lines under **one** workflow id: the line with `elements`, `documentId` and `undoEntry`, and the dispatcher's line with `session` and `ms`, carry the same `wf-...`. **Negative:** OS1's refused move - its `move_failed` line and the dispatcher's line share one id too, and no id appears on one line alone |
| **OS3** | *Template-born models, positive* - Group E's E3 again: preview the D3 ducts up 200 mm in *Heron D3 move test*, `activate-document` on `or-template-twin.rvt`, then approve | `document_not_in_front`: *"That preview was made for Heron D3 move test, but or-template-twin is in front now. Heron D3 move test is still open - click back to it and ask again. Nothing was changed."* - not `model_moved_on`; neither model moved, re-read. **Negative:** click back to *Heron D3 move test* and approve a fresh preview - it moves. Write down, without deciding it, what two copies of one file do (row 5b-1060's owner question) |
| **OS4** | *A wait that ran out names its run, positive* - *Ask to Keep* ON; `revit_change` a small MODIFY on a scratch model and leave the Keep or Discard box open past `revit.askTimeoutSeconds` | The reply is `awaiting_modeller` and carries `workflow` `wf-...`; then press Keep, and `heron_run_record` with that id reads the run's record, KEPT. **Negative:** the same call answered at once - its reply is the ordinary kept answer with `run`, and no `awaiting_modeller` |
| **OS5** | *Still running names its run* - an operation Revit takes longer than `revit.operationTimeoutSeconds` over (the longest read the scratch model has, with that timeout set low in Heron's Settings) | `still_running` with `workflow` `wf-...`; once Revit finishes, the audit line carries that same id. **Negative:** the same read with the timeout back where it was - answered, no `still_running` |
| **OS6** | *A refusal says whether its rollback held, positive* - a MODIFY fragment that throws inside its run (a `revit_change` dry run of a card handed a value its C# throws on, read from its own refusal list), and then a read that throws | The write refusal (`fragment_threw` or the card's own) carries `rolledBack` **true** and `rollbackReported` **true**, and the model is as it was; the read refusal carries `elementsCounted`. **Negative:** a refusal from before anything ran - a card left without a value it needs (`needs_request_values`) - carries neither field |
| **OS7** | *An undo reaches the run record, positive* - keep a small change from a chat, then Ctrl+Z in Revit, then `heron_run_record` with its run; then Ctrl+Y, and read it again | First read: *"UNDONE in Revit at <time>"*; second: *"Undone and then REDONE in Revit at <time>"*; every read of a kept record also says an undo the add-in could not see is not written there. **Negative:** after a keep, make an edit by hand and Ctrl+Z only that edit - the record says no UNDONE, and the add-in log's *Undo guard* line says the state Heron's keep was left in |
| **OS8** | *An answer over the limit* - only if a model gives one: the largest read the biggest scratch model can make (a parameter table of every element, say) | Over 16 MiB: `reply_too_large`, *"its answer is <N> MB - more than the 16 MB ..."*, `replyBytes` above `limitBytes`, and the run record named where the add-in wrote one. **Negative:** the same read narrowed to one category - answered whole. Write down the largest real answer seen, which row 5b-1167 says was never measured |
| **OS9** | *The chain belongs to a model, positive* - open `or\a\Twin.rvt` and `or\b\Twin.rvt` in one Revit (write down whether Revit allows it - row 5b-1164 says that was never measured); in `a`, `FILTER_ELEMENTS_BY_CATEGORY` *Walls*; `activate-document` `b`; then a consumer with `expect_from` naming that filter | Refused - nothing carried in `b` - and nothing changed in either. **Negative:** the same two steps both in `a` - the consumer binds `elements from filter-elements-by-category` |
| **OS10** | *One id for a list, positive* - row 5b-1406's job `p1102-round11` again: `setup: [create-text-note]` then `FIND_DATES_IN_VIEWS`, and `EDIT_TEXT_VALUES` with `keep-chain` | Each runs - its binding note reads `elements from create-text-note (1)` - with no cast error. **Negative:** the same setup run as a dry run first, so the chain is marked taken back - `chain_taken_back`, nothing bound |
| **OS11** | *`COPY_VIEW_FILTERS` with no filter named, positive* - a view with two filters as the source, one target view, `onlyFilterNamed` left out | `filtersApplied` 2 - both copied - and the binding note says `onlyFilterNamed` took the card's empty text. **Negative:** `onlyFilterNamed=ZZZNOTHINGHERE` - none copied, as the card's negative case reads |
| **OS12** | *editScopeFailures in words* - `revit_read CREATE_ASSEMBLY` | `needs_write_path`: *"This fragment opens its own transactions inside Heron's ..."* - no *edit modes*; and on a `revit_change` dry run of it, the binding note names *"Heron's failure discipline, for the transactions the fragment opens itself"*. **Negative:** a read card that declares no such need - no such line |
| **OS13** | *A link deleted or unloaded, then rolled back - the owner's yes first, Snowdon copy only* - `revit_change DELETE_ELEMENTS` dry run handed one link TYPE; then by hand unload one link, Ctrl+Z | Write down, for each, whether Manage Links shows the link loaded again after the rollback and after Ctrl+Z, and what Heron's reply said. Row 5b-1904 and the question to the owner wait on this. **Negative:** the same dry run handed one plain duct - restored, re-read |
| **OS14** | *Dry run and kept run meet the same refusal* (D-155) - a MODIFY change Revit refuses with an Error at commit (Group E's E21 *"Constraints are not satisfied"* case), once as a dry run and once asked to keep | Both refuse with the same *"Revit would not let it through"* words and the same element count, and both say the rollback was confirmed. **Negative:** a change Revit accepts - the dry run says it rolled back, the kept run keeps it |
| **OS15** | *Undo names with the box shown* (Group NU) - *Ask to Keep* ON, keep a change through the box, then Ctrl+Z | The add-in log's *Undo guard: undo [...]* line names what Revit reported - the group's name, *"Heron: show the change"*, or several - and the guard's state is *undone*, not *unsure*. Write down which. **Negative:** *Ask to Keep* OFF, the same change - its undo line names *"Heron: <card>"* alone |
