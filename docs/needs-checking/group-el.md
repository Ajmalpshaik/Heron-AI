# Needs checking — Group EL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EL - a Revit dialog met inside a job is answered, never waited on: `HeronDialogWatch` (2026-10-17)

Found on 2026-10-03 loading `Catch_Basin new.rfa` into Family1 on Revit 2024: Revit opened its modal *"saved
in a later version of Revit and cannot be retrieved in this version"* dialog inside `LOAD_FAMILY`, and the
chat read `unknown_outcome`, then `revit_busy`, then `still_running` for over 90 s until somebody closed it
by hand - [row 5b-300](../fragment-issues/section-5b-rows-276-300.md).

**What changed.** The add-in subscribes to Revit's `DialogBoxShowing` at startup
([`HeronDialogWatch`](../../revit/Heron.Revit.Addin/HeronDialogWatch.cs)), and the dispatcher arms it around
`RevitOperations.Run` alone. While armed, a dialog is answered from
[`HeronDialogRule`](../../revit/Heron.Revit.Addin/HeronDialogNote.cs): **Cancel, then Close if Revit will not
take Cancel, and the job fails** - the fragment and move write paths roll their group back before they keep
anything, and a read returns the refusal. The table holds **no row** yet: a row needs a DialogId Revit really
reported, and none is recorded. The reply carries `dialogsAnswered` and `dialogs` (each dialog's id, kind,
Revit's words and the answer); the audit line carries a `dialogs` number; both are absent when no dialog
opened. The error is `dialog_refused` when Revit confirmed the rollback (or the job only read) and
`dialog_unconfirmed` otherwise. A dialog already open before a job is untouched: Revit runs no job behind
it, so the answer is still `revit_busy`. `HeronAsks()` stands the watch down for a prompt of Heron's own; no
Heron prompt runs inside a job today.

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027
(`tools/check-compile.py`, 2026-10-17), and `tests/test_dialog_note.py` builds and runs the table, the record
and the reply in `tests/Heron.DialogNote.TestHost`. **None of it has run in Revit.** It works only once the
add-in is rebuilt, deployed for each Revit and Revit restarted. No fragment changed, so no fingerprint moved -
but a PROVEN fragment that met a dialog before (EL4) now stops where it used to wait.

**SCRATCH FILES ONLY, never a working model.** Revit 2020 first: a project from the default metric template
saved as *Heron Dialog Project.rvt*; a family from *Metric Generic Model.rft* with one extrusion, **saved in
Revit 2024** as *Heron Later Release.rfa*. Changes ON for EL1, EL2 and EL4.

| # | Run | Look for |
|---|---|---|
| **EL1** | Row 300 reproduced. In Revit 2020, *Heron Dialog Project* in front: `revit_change` capability `LOAD_FAMILY` with *Heron Later Release.rfa* | **No dialog stays on screen**, and the answer arrives within seconds - not `unknown_outcome`, `revit_busy` or `still_running`. The error is `dialog_refused`; the message quotes Revit's words and says the job was rolled back; `dialogs` lists the dialog with its **id** (write the id down - it is the first real row's key), its kind and `answer` Cancel or Close; the project has no new family and no new Undo entry. The add-in log has one `Revit dialog during a Heron job` line. The D-30 positive |
| **EL2** | Whether `OverrideResult` works for that dialog. Read EL1's `dialogs` entry | `answer` is Cancel or Close - Revit took the override. If it reads *none - Revit would not take an answer*, the dialog stayed open: close it by hand, record that here, and `BasicFileInfo.Extract` before `LoadFamily` (row 300's other fix) becomes the way for this dialog |
| **EL3** | Whether an add-in's own TaskDialog raises `DialogBoxShowing`. A throwaway read fragment, through `run_fragment_read`, whose whole body shows `TaskDialog.Show("Heron EL3", "Close me if you can see me.")` and returns | **Either** the reply lists it in `dialogs` with its id and `dialog_refused` (it does raise the event - so any Heron prompt inside a job MUST sit inside `HeronAsks()`), **or** the dialog appears and waits for a click (it does not). Write down which, and the id if one came |
| **EL4** | "Duplicate Types" is cancelled and the rollback holds. Two scratch projects, each with a drafting view holding a filled region of a type named *Heron Dup* with a **different** fill pattern; `revit_change` capability `TRANSFER_VIEWS_BETWEEN_DOCUMENTS` from one into the other | If Revit asks "Duplicate Types": the reply is `dialog_refused` naming it, and the target project has **no** new view, its *Heron Dup* is unchanged and there is no new Undo entry - the rollback held. If no dialog appears, write that down: the API then answers duplicate types without asking, and this row is answered by that |
| **EL5** | The negative. `LOAD_FAMILY` with a family saved in Revit 2020, and any read such as `revit_health` | Neither reply carries `dialogsAnswered` or `dialogs`, the family loads as before, and the audit lines have no `dialogs` number - a job no dialog interrupted reads exactly as it did. **The D-30 negative** |
| **EL6** | The modeller's own dialogs are left alone. With no Heron job running, open any Revit dialog that asks something (Manage > Purge Unused, then Cancel) | It shows and waits for the modeller as always. Then, with that dialog still open, ask the chat for any read: the answer is `revit_busy` - the job never started, so nothing touched the open dialog |
| **EL7** | EL1, EL5 and EL6 once on Revit 2024 and once on 2027 | The same answers - the watch is in the add-in every release loads |

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

*Heron Later Release.rfa* is a copy of Revit 2027's `rac_basic_sample_family.rfa`; *Heron 2020 Family.rfa* a
copy of Revit 2020's `basic_sample_family.rfa`. The loads were rolled-back runs (`--write`, no `--apply`).

| # | Release | What was run and what came back | State |
|---|---|---|---|
| **EL1** | 2024 | `load-family` *Heron Later Release.rfa*: answered in 7 s, `dialog_refused` quoting *"The file Heron Later Release.rfa was saved in a later version of Revit and cannot be retrieved in this version."*, rolled back, audit `dialogs 1`, one *"Revit dialog during a Heron job"* log line, and no dialog window left (only Revit's main window visible). **The dialog has no id** - the log reads `id ''` - so it cannot be the table's first row by id | **PASS** |
| **EL2** | 2024 | `answer` Cancel - Revit took the override | **PASS** |
| **EL3** | 2024 | READ probe whose body is `TaskDialog.Show("Heron EL3", "Close me if you can see me.")`: `dialog_refused` in under a second, task dialog, **no id**, answered Cancel. **An add-in's own TaskDialog DOES raise `DialogBoxShowing`** - so a Heron prompt inside a job must sit inside `HeronAsks()`, and the Keep box does (it always waited for a click) | answered: **it raises the event** |
| **EL4** | - | Not arranged | **NOT RUN** |
| **EL5** | 2024 | `load-family` *Heron 2020 Family.rfa*: loaded, three types, no `dialogs` or `dialogsAnswered` on the reply, no log line, no `dialogs` on the audit line | **PASS** |
| **EL6** | 2024 | His Manage > Purge Unused left open, then a `count`: `revit_busy` after 10 s, *"A dialog may be open"*; his window still open and untouched until he cancelled it. **Seen beside it:** the activity banner read *"Heron AI is reading your model ... Counting what is in the model - 8.4 s"* while Revit never ran the count | **PASS** |
| **EL7** | 2027 | EL5 on 2027: loaded, 35 elements added, no dialogs, rolled back. EL1 cannot exist on 2027 - no later release; EL6 not repeated | **PASS** (EL5) |
