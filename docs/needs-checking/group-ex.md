# Needs checking — Group EX

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group EX - the run guard: a running job cannot save, sync, export or print what its operation does not allow (2026-10-17)

Constitution Article 10 - *"Never synchronise, publish or share on your own initiative ... Neither is any
export to a shared location"* - and Golden Rule 17 were sentences with nothing in code behind them. The
add-in listened to `ViewActivated`, `DocumentClosing`, `Idling` and `DocumentClosed`, so a MODIFY fragment
calling `doc.Save()`, a sync or an export met nothing at all.

**What changed.** [`HeronRunGuard`](../../revit/Heron.Revit.Addin/HeronRunGuard.cs) is armed by the dispatcher
around each job, in a `finally`, and listens for `DocumentSaving`, `DocumentSavingAs`,
`DocumentSynchronizingWithCentral`, `FileExporting`, `ViewExporting`, `DocumentPrinting`, `DocumentCreated`,
`DocumentOpened` and `DocumentChanged`. [`HeronRunPolicy`](../../revit/Heron.Revit.Addin/HeronRunPolicy.cs)
decides, from the operation's registry risk and where the document came from:

- **Publish** (`run_fragment_publish`) - nothing is cancelled; the guard stands aside.
- **Admin** - may start a family or project file and save it; a save, sync, export or print of a model that
  was open when the job began is cancelled (`e.Cancellable` asked first).
- **Everything else** - the same, and a new document (`DocumentCreated`) is refused.
- **A document the job opened** is its own at every level; **one it left open in the background** - not
  linked, no `GetOpenUIViews()`, not open at the start - is closed with `Close(false)` as the job ends.
- **A committed change to another open model** cannot be cancelled: the job's group is rolled back, and the
  reply says that model's change stands and Heron cannot undo it (`changed_other_model`).
- **Heron's own export** - `capture_view`'s `ExportImage` into Heron's own capture folder - sits inside
  `HeronRunGuard.HeronExports()` and is let through; a save, sync or print inside it is still judged.
- **Before `group.Assimilate()`** the switches are read again from `heron.config` and `HeronStop.IsStopped`
  asked again; either refusing rolls the job back (`permission_withdrawn`).

Every refusal goes back as `run_guard`, `changed_other_model` or `permission_withdrawn`, the record rides on
the reply as `guardBlocked`, `guardStopped`, `guardRefusedDocuments`, `guardChangedElsewhere`, `guardOpened`,
`guardClosed` and `guardNotClosed`, the audit line carries `blocked` and `closed` when either is above 0, and the add-in log has a
`Run guard ...` line for each cancel and each close.

**What is compiled and what is not run.** The add-in and `tests/Heron.RunGuard.TestHost` build on all eight
releases 2020 to 2027 (`python tools/check-compile.py`, 2026-10-17); `python tests/test_run_guard.py` runs
the policy in its test host and reads the wiring as text. **None of it has run in Revit.** Every event and
member it uses was read out of each release's `RevitAPI.dll` with `tools/api-surface --members` - which says
the events EXIST on all eight, not that Revit raises them for the calls below, nor which can be cancelled.

**SCRATCH MODELS ONLY, never a working model, Revit 2020 first.** Make *Heron Guard A.rvt* and *Heron Guard
B.rvt* from the default metric template, and *Heron Guard Probe.rfa* from *Metric Generic Model.rft*, all in
one scratch folder. The probes below are scratch fragment folders made on the test PC under
`brain/fragments/` and **never committed** - delete each after its row. Rebuild and deploy the add-in for
the release under test and restart Revit first. Read what the guard did from the add-in log's `Run guard ...` lines and the audit line (`blocked`,
`closed`): the command-line client prints only a refusal's code and message, never the `guard...` fields.

| # | Run | Look for |
|---|---|---|
| **EX1** | Changes and Publish on. *Heron Guard A* in front, *Heron Guard B* open behind it with one unsaved change. `python mcp/client/heron_bridge_client.py fragment --in "Heron Guard A" save-document --write --apply --set "documentPath=<Heron Guard B.rvt>" --set closeWindow=false` | `saved true`; B's file time moves; no `run_guard`; the audit line carries no `blocked`. **save-document still works under Publish** - the guard stood aside. Then the same with Publish OFF: `publish_disabled`, nothing sent - the gate's, unchanged |
| **EX2** | Changes on, Publish off. A scratch MODIFY fragment `guard-probe-save` that draws one model line in `doc` and then calls `Save()` on the open *Heron Guard B* (found by its path); `fragment --in "Heron Guard A" guard-probe-save --write --apply`. Then a second probe, `guard-probe-export`, that draws the line and exports A's default 3D view to DWG in the scratch folder | Each comes back `run_guard`: *"Stopped before it happened: a save of 'Heron Guard B'"* (then *"an export from 'Heron Guard A'"*), *"Revit confirmed it"*; the model line is NOT in A; B's file time and the scratch folder are unchanged; the log has `Run guard cancelled Save ...`; the audit line says `blocked 1` or more. **The negative**: the same probe with its save line deleted is KEPT as an ordinary change, and its audit line carries no `blocked` |
| **EX3** | **Which events can be cancelled, on 2020 and on 2027.** EX2's probe once for each act: `Save`, `SaveAs` to a new scratch path, `SynchronizeWithCentral` on a workshared scratch copy, a DWG export (`FileExporting`), an image export (`ViewExporting`), a print to a PDF printer (`DocumentPrinting`) | For each, the log's `Run guard cancelled ...` or `Run guard could NOT cancel ...` line, written into a table per release. Every `true` is stopped before it happened; every `false` must read **COULD NOT BE STOPPED ... may already have happened** - and is then a defect in this row's own words to record, with the file it wrote. Note which acts raised NO event at all - those the guard cannot see |
| **EX4** | **`Close(false)` on another document while a group is open.** Publish on: `upgrade-family-files` (PROVEN), which opens files and closes them itself inside its group, on a folder of two scratch families. Then a scratch MODIFY probe that opens *Heron Guard Probe.rfa* with `OpenDocumentFile` and returns without closing it | `upgrade-family-files` answers exactly as its proof did, and the log has no `Run guard closed` line for it. The probe is kept, the log says `Run guard closed 'Heron Guard Probe' unsaved` and the audit line `closed 1`; View > Switch Windows does not list it; the .rfa's file time is unchanged |
| **EX5** | **A thrown fragment's EditFamily document gets closed.** A loaded family in *Heron Guard A*. A scratch READ probe that calls `doc.EditFamily` on it and then throws | `fragment_threw` with the probe's message; the log's `Run guard closed` line names the family; Switch Windows shows no family window; A is untouched. **And write down whether the log has a `Run guard saw ... open` line**: whether Revit raised `DocumentOpened` for `EditFamily` at all - the guard knows the document is the job's because it was not open at the start, not because of that event |
| **EX6** | **OpenAndActivate windows are kept.** `open-family-for-editing` (EXECUTE, PROVEN) on a loaded family; then, Admin on, `create-family-document` (ADMIN, PROVEN) with a new scratch path | The family editor window is open and in front after each, and neither job closed it: no `Run guard closed` line for it. `create-family-document`'s `SaveAs` went through - the .rfa exists - with no `run_guard`: Admin may save a file it started. **Then once more with that family ALREADY open in the Family Editor before the run**, and write down what happens: if `EditFamily` hands back the open document, it was open at the start, so the guard cancels its `SaveAs` to a temporary path - which would otherwise re-point the modeller's own family at that path - and the job is `run_guard` |
| **EX7** | **Changes switched off while a job runs.** A scratch MODIFY probe that draws one model line and then sleeps 20 seconds (`System.Threading.Thread.Sleep(20000)`), run with `--write --apply`. During the sleep, set `write.enabled = false` in `heron.config` with a text editor - Revit's ribbon cannot be clicked while it runs the job | `permission_withdrawn`: *"... the Changes switch was turned off while it ran, so Heron did not keep it"* and *"Revit confirmed it"*; the line is NOT in the model. **The negative**: the same run with the file left alone keeps the line. And once with `"apply": "ask"` (D-119): change the file while the Keep or Discard box is up, then press Keep - still `permission_withdrawn`, because the switches are read after the modeller answers Then once with `HeronStop` if the owner has a way to set it by then - nothing on the ribbon does today |
| **EX8** | **transfer-views-between-documents still works.** *Heron Guard B* with one drafting view, A in front; `transfer-views-between-documents` (MODIFY, PROVEN) from B into A, `--write --apply` | The view arrives in A exactly as the fragment's proof says; no `changed_other_model`, no `run_guard` - B was only read. If Revit raises `DocumentChanged` for B here, this row FAILS and the policy is wrong about D-47's own pattern |
| **EX9** | **A change to another open model.** A scratch MODIFY probe that opens a `Transaction` in the open *Heron Guard B*, draws a model line there and commits it, run on A with `--write --apply` | `changed_other_model`: *"It changed 'Heron Guard B' ... THAT CHANGE IS COMMITTED ... HERON CANNOT UNDO IT"*, A rolled back and confirmed; the line IS in B and B's own Undo lists it. **The negative**: the same probe aimed at A's own `doc` is kept, and its audit line carries no `blocked`. And a copy whose card says `risk: PUBLISH`, Publish on - the guard stands aside and nothing is refused |
| **EX10** | **A new document under a change.** A scratch MODIFY probe that calls `app.NewFamilyDocument` on the Generic Model template and then `SaveAs` to a scratch path | `run_guard` naming the new document and the Admin switch; the scratch path has NO file (its `SaveAs` was cancelled); the log's `Run guard closed` line names the new document. **The negative** is EX6's `create-family-document` under Admin |
| **EX11** | *Watching, not a proof.* **The guard costs the modeller nothing between jobs.** With no chat connected, on a large real model's detached copy: time a Copy of a few thousand elements and a Ctrl+S, once with Heron's add-in loaded and once with it removed from the Addins folder | The same times within a second or so, and no `Run guard` line in the add-in log - every handler returns at once while no job is armed. `DocumentChanged` is now subscribed for the whole session, so this is the row that says whether that costs anything |
| **EX12** | **Heron's own picture still works.** With the guard armed, `revit_view_image` (`capture_view`, ANALYZE) on *Heron Guard A*: once for a named view, once for two elements by id | A picture comes back both times, no `run_guard`, no `Run guard cancelled` line in the log - the export is inside `HeronExports()`. **And write down which events `ExportImage` raised**, if any `Run guard` line names one: that says whether a fragment's image export is caught by `FileExporting`, `ViewExporting` or neither (EX3's image row) |

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

*Heron Safety 2024* was A, in front; *Heron Guard B 2024* was B, open behind it. The probes were MODIFY
fragments run with `--write` and no `--apply`: since D-117 the command line refuses `--apply` for a DRAFT -
*"zz-ej-runaway-probe is DRAFT ... Heron will not let it change a live model (Constitution Article 11). Nothing
was sent to Revit."* - so a row's *"KEPT as an ordinary change"* cannot happen with a probe, and the negatives
below are rolled-back runs. Every refusal below is read off the add-in log's `Run guard ...` lines and the
audit line.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| EX1 | 2024 | `save-document` on B, Publish on, `--apply`: `saved true`, B's file time 09:13:15 → 09:16:20, no `run_guard`, no `blocked`, nobody asked (no change counted in A). With Publish OFF (his ribbon, 06:37:03): `publish_disabled`, B's file time unchanged | **PASS** |
| EX2 | 2024 | Save probe: the save of B **was cancelled** - *"Run guard cancelled Save of 'Heron Guard B 2024'"*, B's file time unchanged, A rolled back - but the reply was `changed_other_model`, not `run_guard`: Revit committed *"Store dependencies"* in B before `DocumentSaving` ([5b-1092](../fragment-issues/section-5b-rows-401-425.md)). Export probe: `run_guard` *"Stopped before it happened: an export from 'Heron Safety 2024'"*, no DWG written, `blocked 1`. Negative (the line alone): ran, rolled back, no guard line, no `blocked` | **FAIL** (the save's code and words); export and negative **PASS** |
| EX3 | 2024 | Save: cancelled (EX2). Save As of B to a new path: cancelled, `run_guard`, no file. DWG export: cancelled. `ExportImage`: cancelled (the log names it *Export*). No *"could NOT cancel"* line. Sync and print not arranged; 2027 not run | **part** |
| EX4 | 2024 | `upgrade-family-files` (PUBLISH, PROVEN) on two scratch families: upgraded 2, failed 0, no `Run guard closed` line for them. The probe that opens *Heron Guard Probe.rfa* and returns: *"Run guard closed 'Heron Guard Probe' unsaved"*, audit `closed 1`, the file's time unchanged | **PASS** |
| EX5 | 2024 | READ probe, `EditFamily` then a throw: the family was closed (*"Run guard closed 'M_WSHP ... .rfa' unsaved"*), but the reply was `run_guard` *"It started a new document - '(unnamed)'"*, not `fragment_threw`. **No `Run guard saw ... open` line: Revit raised `DocumentCreated` for `EditFamily`** - [5b-1091](../fragment-issues/section-5b-rows-401-425.md) | **FAIL** (the code it answers with) |
| EX6 | 2024 | `open-family-for-editing` (EXECUTE, PROVEN) on the M_WSHP family: `run_guard` - the new-document refusal and its `SaveAs` to the scratch folder cancelled, `blocked 2`; no family left open. And `REPORT_FAMILY_TABLES_IN_PROJECT` (READ, PROVEN) through the chat: refused, *"started a new document - '(unnamed)' x3"*. `create-family-document` not run | **FAIL** - a regression of two PROVEN fragments, 5b-1091 |
| EX7 | - | Needs `write.enabled` edited in the owner's `heron.config` while a job runs - this run does not touch that switch; and a DRAFT probe cannot be kept to reach the re-read | **NOT RUN** |
| EX8 | - | B has no drafting view arranged | **NOT RUN** |
| EX9 | 2024 | Probe committing a model line in B in B's own transaction: `changed_other_model` *"THAT CHANGE IS COMMITTED IN 'Heron Guard B 2024' AND HERON CANNOT UNDO IT"*, A rolled back; B's own Undo list shows *"zz probe line in B"* | **PASS** |
| EX10 | 2024 | `NewFamilyDocument` + `SaveAs`: `run_guard` naming *Family1* and the Admin switch, the `SaveAs` cancelled, no file, *"Run guard closed 'Family1' unsaved"*, audit `blocked 2`, `closed 1` | **PASS** |
| EX11 | - | Not timed | **NOT RUN** |
| EX12 | 2024 | `revit_view_image` of a named view with the guard armed: pictures, no `Run guard` line. The element half fails for its own reason ([5b-1090](../fragment-issues/section-5b-rows-401-425.md)) | **PASS** (named view) |
