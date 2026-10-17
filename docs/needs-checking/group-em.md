# Needs checking — Group EM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group EM - the run guard: a running job cannot save, sync, export or print what its operation does not allow (2026-10-17)

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
| **EM1** | Changes and Publish on. *Heron Guard A* in front, *Heron Guard B* open behind it with one unsaved change. `python mcp/client/heron_bridge_client.py fragment --in "Heron Guard A" save-document --write --apply --set "documentPath=<Heron Guard B.rvt>" --set closeWindow=false` | `saved true`; B's file time moves; no `run_guard`; the audit line carries no `blocked`. **save-document still works under Publish** - the guard stood aside. Then the same with Publish OFF: `publish_disabled`, nothing sent - the gate's, unchanged |
| **EM2** | Changes on, Publish off. A scratch MODIFY fragment `guard-probe-save` that draws one model line in `doc` and then calls `Save()` on the open *Heron Guard B* (found by its path); `fragment --in "Heron Guard A" guard-probe-save --write --apply`. Then a second probe, `guard-probe-export`, that draws the line and exports A's default 3D view to DWG in the scratch folder | Each comes back `run_guard`: *"Stopped before it happened: a save of 'Heron Guard B'"* (then *"an export from 'Heron Guard A'"*), *"Revit confirmed it"*; the model line is NOT in A; B's file time and the scratch folder are unchanged; the log has `Run guard cancelled Save ...`; the audit line says `blocked 1` or more. **The negative**: the same probe with its save line deleted is KEPT as an ordinary change, and its audit line carries no `blocked` |
| **EM3** | **Which events can be cancelled, on 2020 and on 2027.** EM2's probe once for each act: `Save`, `SaveAs` to a new scratch path, `SynchronizeWithCentral` on a workshared scratch copy, a DWG export (`FileExporting`), an image export (`ViewExporting`), a print to a PDF printer (`DocumentPrinting`) | For each, the log's `Run guard cancelled ...` or `Run guard could NOT cancel ...` line, written into a table per release. Every `true` is stopped before it happened; every `false` must read **COULD NOT BE STOPPED ... may already have happened** - and is then a defect in this row's own words to record, with the file it wrote. Note which acts raised NO event at all - those the guard cannot see |
| **EM4** | **`Close(false)` on another document while a group is open.** Publish on: `upgrade-family-files` (PROVEN), which opens files and closes them itself inside its group, on a folder of two scratch families. Then a scratch MODIFY probe that opens *Heron Guard Probe.rfa* with `OpenDocumentFile` and returns without closing it | `upgrade-family-files` answers exactly as its proof did, and the log has no `Run guard closed` line for it. The probe is kept, the log says `Run guard closed 'Heron Guard Probe' unsaved` and the audit line `closed 1`; View > Switch Windows does not list it; the .rfa's file time is unchanged |
| **EM5** | **A thrown fragment's EditFamily document gets closed.** A loaded family in *Heron Guard A*. A scratch READ probe that calls `doc.EditFamily` on it and then throws | `fragment_threw` with the probe's message; the log's `Run guard closed` line names the family; Switch Windows shows no family window; A is untouched. **And write down whether the log has a `Run guard saw ... open` line**: whether Revit raised `DocumentOpened` for `EditFamily` at all - the guard knows the document is the job's because it was not open at the start, not because of that event |
| **EM6** | **OpenAndActivate windows are kept.** `open-family-for-editing` (EXECUTE, PROVEN) on a loaded family; then, Admin on, `create-family-document` (ADMIN, PROVEN) with a new scratch path | The family editor window is open and in front after each, and neither job closed it: no `Run guard closed` line for it. `create-family-document`'s `SaveAs` went through - the .rfa exists - with no `run_guard`: Admin may save a file it started. **Then once more with that family ALREADY open in the Family Editor before the run**, and write down what happens: if `EditFamily` hands back the open document, it was open at the start, so the guard cancels its `SaveAs` to a temporary path - which would otherwise re-point the modeller's own family at that path - and the job is `run_guard` |
| **EM7** | **Changes switched off while a job runs.** A scratch MODIFY probe that draws one model line and then sleeps 20 seconds (`System.Threading.Thread.Sleep(20000)`), run with `--write --apply`. During the sleep, set `write.enabled = false` in `heron.config` with a text editor - Revit's ribbon cannot be clicked while it runs the job | `permission_withdrawn`: *"... the Changes switch was turned off while it ran, so Heron did not keep it"* and *"Revit confirmed it"*; the line is NOT in the model. **The negative**: the same run with the file left alone keeps the line. Then once with `HeronStop` if the owner has a way to set it by then - nothing on the ribbon does today |
| **EM8** | **transfer-views-between-documents still works.** *Heron Guard B* with one drafting view, A in front; `transfer-views-between-documents` (MODIFY, PROVEN) from B into A, `--write --apply` | The view arrives in A exactly as the fragment's proof says; no `changed_other_model`, no `run_guard` - B was only read. If Revit raises `DocumentChanged` for B here, this row FAILS and the policy is wrong about D-47's own pattern |
| **EM9** | **A change to another open model.** A scratch MODIFY probe that opens a `Transaction` in the open *Heron Guard B*, draws a model line there and commits it, run on A with `--write --apply` | `changed_other_model`: *"It changed 'Heron Guard B' ... THAT CHANGE IS COMMITTED ... HERON CANNOT UNDO IT"*, A rolled back and confirmed; the line IS in B and B's own Undo lists it. **The negative**: the same probe aimed at A's own `doc` is kept, and its audit line carries no `blocked`. And a copy whose card says `risk: PUBLISH`, Publish on - the guard stands aside and nothing is refused |
| **EM10** | **A new document under a change.** A scratch MODIFY probe that calls `app.NewFamilyDocument` on the Generic Model template and then `SaveAs` to a scratch path | `run_guard` naming the new document and the Admin switch; the scratch path has NO file (its `SaveAs` was cancelled); the log's `Run guard closed` line names the new document. **The negative** is EM6's `create-family-document` under Admin |
| **EM11** | *Watching, not a proof.* **The guard costs the modeller nothing between jobs.** With no chat connected, on a large real model's detached copy: time a Copy of a few thousand elements and a Ctrl+S, once with Heron's add-in loaded and once with it removed from the Addins folder | The same times within a second or so, and no `Run guard` line in the add-in log - every handler returns at once while no job is armed. `DocumentChanged` is now subscribed for the whole session, so this is the row that says whether that costs anything |
