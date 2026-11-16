# Needs checking — Group NW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NW - an empty keep left out of the undo list, late answers and stopping a job, the activity pane, and the pipe hardened (2026-11-12)

Items 8 to 11 of the plan written after the comparison D-155 came from. **Each compiles for 2020 to 2027
and its Revit-free half runs here; none of it has run in Revit.**

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON.

### D-160 - a keep that changed nothing

| ID | Do this | Pass looks like |
|---|---|---|
| **NW1** | Ask the chat to rename views with a find text that matches no view | The reply says *NOTHING CHANGED, SO NOTHING WAS KEPT* and *no new entry in Revit's undo list*. Revit's undo list has **no** new `Heron:` entry on top |
| **NW2** | *The negative:* the same rename with a find text that matches two views | Kept as before: one `Heron:` entry, the two views renamed |
| **NW3** | Look at the log for NW1 | The change report says it was witnessed with 0 changed. If it says *not counted - no change event*, Revit raises no event for an empty transaction and the empty keep was kept as before: note it here |

### D-161 - a lost answer read late

| ID | Do this | Pass looks like |
|---|---|---|
| **NW4** | Set `revit.operationTimeoutSeconds` low (10), ask for a PROVEN change that takes longer on a large model | The reply says the answer was lost and names `heron_run_record request=<id>` |
| **NW5** | When Revit has finished, ask `heron_run_record request=<that id>` | The run's record: what it changed, its verdict. **Not** the record of another run |
| **NW6** | *The negative:* `heron_run_record request=` an id nobody sent | *No record answers request ... yet* - nothing else is shown |

### D-162 - the pipe hardened

| ID | Do this | Pass looks like |
|---|---|---|
| **NW7** | Connect Heron, ask anything from the chat | Works as before - the server's pid matches, nothing is refused |
| **NW8** | In PowerShell: `Get-Acl \\.\pipe\<Heron's pipe name> \| Format-List` (the name is in `%LOCALAPPDATA%\Heron\bridges\*.json`) | A **Deny** rule for `NT AUTHORITY\NETWORK`, and the Allow for this user |
| **NW9** | `Get-Acl` on the bridge's `.json` file in that folder | One Allow rule, this user, full control; *AreAccessRulesProtected* True |
| **NW10** | *The negative, from a second PC on the same domain as the same user:* open `\\<this PC>\pipe\<the name>` | Access denied |

### D-163 - the Activity pane

| ID | Do this | Pass looks like |
|---|---|---|
| **NW11** | Heron menu > **Activity** | A pane docks on the right, titled Heron activity, in the brand's dark colours; the list or *No change from Heron is recorded* |
| **NW12** | Keep a change from the chat, wait five seconds | It appears at the top: name, model, time, *Kept*, its counts, and **Select**, **Zoom**, **Undo** |
| **NW13** | Press **Select**, then **Zoom** | The change's elements are selected, then the view zooms to them; the status line says how many, in which model |
| **NW14** | Changes ON. Keep a change from the chat, then press **Undo** on its row at once - within three minutes, while that chat still holds the model. *Since [row 5b-1741](../fragment-issues/row-5b-1741.md), 2026-11-16: the button sent `undo_last`, which nothing answered, so this row could not pass before then* | Revit's undo list loses that `Heron:` entry; the status line says *Revit's own Undo was sent for '...'* - in Revit words, never an operation's name or a list of them, and never *in use by another chat*. Pressing Undo again is refused - *already sent* or *already undone* - and nothing more is undone |
| **NW15** | *The negative:* switch to another model, press **Select** on the change | Refused - the change was made in the other model. Nothing is selected in this one |
| **NW16** | Press **Select** while Revit is busy (a command running) | The pane does not freeze; the status line says Revit is busy |
| **NW17** | Keep a change from a SECOND chat in the same model, then press **Undo** on its row | Undone as in NW14 - the pane's Undo is the modeller's own click, whichever chat made the change (the owner's answer, 2026-11-16). Then keep another change from that second chat and ask the FIRST chat to *undo that*: still refused, *made in another chat* - the pane gave no chat a way round [Q-88](../open-questions/tier-2.md) |
| **NW18** | *The negative:* keep a change from the chat, then move a wall yourself in that model, then press **Undo** on the change | Refused: *'...' was done after it ... Nothing was undone*. The wall stays moved, Heron's change stays, and Revit's undo list is as it was - your move on top |
| **NW19** | After NW14, press Ctrl+Y (Redo) in Revit, wait five seconds, then press **Undo** on the pane again | Ctrl+Y puts Heron's change back and its `Heron:` entry is on top of the undo list again; the second **Undo** takes it again. If it is refused instead, note the add-in log's *Undo guard: redo* line - Revit's redo names are what the guard reads |
| **NW20** | *The negative:* keep a change, switch to another open model, press **Undo** on the change | Refused: *This change was made in "...", and the model on screen in Revit is "..." ... Nothing was sent to Revit and nothing was undone*. Revit's undo list in the model on screen is untouched |
| **NW21** | *The negative:* Changes OFF, press **Undo** on a kept change | Refused in the Changes switch's own words; nothing undone. Turning Changes back on and pressing again undoes it, as NW14 |

### Run record - 2026-11-14 night, Revit 2024 (session 3592), the owner asleep

Revit 2024, add-in 0.2.0.0 from `main`, *Ask to Keep* off, the scratch *Heron Reads Test*
(`%TEMP%\heron-work\nk`). Asked through the chat's own `revit_change`, as a modeller's chat would. Revit's Undo list
read by UI Automation - the drop-down opened, its items read, closed again; no item pressed.

| # | Result | What was seen |
|---|---|---|
| NW1 | **FAIL as written** | `FIND_VIEWS viewType=FloorPlan` (3), then `EDIT_TEXT_VALUES field=Name mode=replace text=ZZQ replacement=X`: *"outcome: KEPT"*, *"changed: not counted - Revit reported no change event for this job"*, `changed 0`, `notMatched 3`. **Not** *NOTHING CHANGED, SO NOTHING WAS KEPT*. The undo list, though, has **no** `Heron: edit-text-values` entry - the half about Revit's list holds. [Row 5b-1657](../fragment-issues/row-5b-1657.md) |
| NW2 | **PASS** | The same with `text=Level replacement=Floor`: `changed: 2 modified (Views 2)`, `changed [312, 695]`, Site in `notMatched`; the undo list's top entry *"Heron: edit-text-values"*, one entry |
| NW3 | **Noted, as the row asks** | The log: *"Undo guard: kept, 0 element(s) counted in 'Heron Reads Test': onTop -> nothing, job 'edit-text-values'"* - while the reply says *not counted*: Revit 2024 raises no change event for a rename that matches nothing, and the empty keep was kept as before |
| NW4-NW6 | **NOT RUN - needs a Revit restart** | The add-in reads `revit.operationTimeoutSeconds` once, when the dispatcher is made (`RevitDispatcher.cs` line 160), not per call - so a lowered timeout reaches Revit only after a restart, and Revit is not restarted while the owner is away. For his hands, with the setting put back to 60 afterwards |
| NW7 | **PASS** | Every chat call of the night answered - `revit_health`, `revit_show`, `revit_views`, `revit_read`, `revit_change` - and the log holds no refusal of the server's process |
| NW8 | **PASS, by another route** | The row's `Get-Acl \.\pipe\heron.2024.3592` fails on Windows PowerShell 5.1 - *"Cannot find path ... because it does not exist"* - with the pipe listed in `\.\pipe\`. Read instead through `NamedPipeClientStream` opened with `ReadData, ReadPermissions` and `GetAccessControl()`: *"NT AUTHORITY\NETWORK \| Deny \| FullControl"* and *"Ajmal_PS\ajmal \| Allow \| CreateNewInstance, ReadWrite, Synchronize"*, owner this user |
| NW9 | **PASS** | `Get-Acl` on `%LOCALAPPDATA%\Heron\bridges\3592.json`: *"AJMAL_PS\ajmal Allow FullControl"*, one rule, `AreAccessRulesProtected` True |
| NW10 | **NOT RUN** | No second PC |
| NW11-NW16 | **NEEDS THE OWNER'S HANDS** | The Activity pane's buttons - for the morning |

*Heron Reads Test* now holds, unsaved: the floor plans *Floor 1* and *Floor 2* (were Level 1 and Level 2).
