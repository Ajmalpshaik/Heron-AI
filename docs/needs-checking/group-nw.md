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
| **NW14** | Press **Undo** on it | Revit's undo list loses that `Heron:` entry; the status line says Undo was sent. Pressing Undo again is refused |
| **NW15** | *The negative:* switch to another model, press **Select** on the change | Refused - the change was made in the other model. Nothing is selected in this one |
| **NW16** | Press **Select** while Revit is busy (a command running) | The pane does not freeze; the status line says Revit is busy |
