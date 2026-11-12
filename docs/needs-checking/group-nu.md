# Needs checking — Group NU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NU - Heron undoes its own last change: `revit_undo_last` (2026-11-12)

[D-157](../decisions/D-157.md) lets a chat post Revit's own Undo for Heron's last kept change, only while it
is still the top of the undo list. **It compiles for 2020 to 2027, every Revit member it calls exists in
every release (`tools/check-api-surface.py`), and the guard runs without Revit** (`tests/test_undo_last.py`,
whose checks were seen to fail on the code before the change). **None of it has run in Revit.**

**Repaired the day it was written, after a review, and still not run.** The first build could send Undo for
work that was not Heron's: a kept job that left no undo entry was recorded as *on top*
([row 5b-1603](../fragment-issues/row-5b-1603.md)), and the model was checked by the Project Information
id alone, which every project from one template shares
([row 5b-1604](../fragment-issues/row-5b-1604.md)). Rows
[5b-1605](../fragment-issues/row-5b-1605.md) to [5b-1610](../fragment-issues/row-5b-1610.md) are the
rest. **Revit has no API that reads its undo list**, so everything the guard says is what Heron *saw* -
its own change land, and nothing reported in that model since - and NU9 on are what only Revit can
settle about that. **The add-in's log now carries one line for every thing the guard hears** - *"Undo
guard: undo ['Heron: ...'] in 'Tower': onTop -> undone (depth 0)"* - with the transaction names Revit
gave: read it after each row (*Heron > AI Bridge > Status* names the log folder).

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON,
except where a row says otherwise.

| ID | Do this | Pass looks like |
|---|---|---|
| **NU1** | Keep one PROVEN change - rename a view - then ask the chat to undo it | The reply says Undo was SENT for it. The view has its old name, and Revit's undo list no longer shows `Heron: <name>` on top; Redo shows it |
| **NU2** | *The negative:* keep a change, then move a wall yourself, then ask the chat to undo | **Refused**, naming what was done after it. Nothing undone: the wall is where you put it, Heron's change still stands |
| **NU3** | After NU1, ask the chat to undo again | Refused - already undone. Nothing more undone |
| **NU4** | Keep a change, press Ctrl+Z yourself, then ask the chat to undo | Refused - already undone. **This is the row that shows whether Revit raises one undo event for an assimilated group or several** - look at the log for the guard's lines |
| **NU5** | Keep a change, then ask for a DRY RUN of another (D-155), then ask to undo | Undo is sent for the KEPT change - the dry run, rolled back, covered nothing |
| **NU6** | Keep a change, then isolate something temporarily through Heron, then ask to undo | Refused - the isolate committed after it. If Revit's list shows no isolate entry, the guard is stricter than it needs to be, which is safe; note it |
| **NU7** | Keep a change in model A, switch to model B on screen, ask to undo | Refused - not the model on screen. Nothing undone in either |
| **NU8** | Workshared model: keep a change, Sync with Central, ask to undo | Refused - synchronized, Revit cleared its list |
| **NU9** | **A keep that leaves no undo entry.** Move a wall yourself. Then ask the chat to open a view through `revit_change` (`OPEN_VIEW`, a capability that runs with no transaction). Look at Revit's Undo arrow: is there a `Heron: open-view` entry? Then ask the chat to undo | **Refused**: *"Heron's last job here, 'open-view' at ..., changed nothing in this model that Revit counted"*. The wall is where you put it. The kept reply itself ended *"Nothing to take back"*. **Write down whether Revit's list showed an entry for the job** - if it did, the refusal is stricter than it needs to be, which is safe |
| **NU10** | *Its pair:* keep a rename, then ask for `OPEN_VIEW` through `revit_change`, then ask to undo | Refused the same way - the open-view is Heron's last job. The rename stands, and Ctrl+Z takes it back |
| **NU11** | **Two projects from one template.** Start Project1 and Project2 from the same template and save both. In a chat working on Project1, keep a rename. Bring Project2 to the front, move a wall in it yourself, and ask the chat to undo | **Refused, nothing sent**: *"This chat works on "Project1", and the model on screen in Revit now is "Project2""*. The wall in Project2 is where you put it; the rename in Project1 stands. *The negative:* bring Project1 back to the front and ask again - Undo is sent |
| **NU12** | *The same with an unsaved pair:* two new projects from one template, neither saved (both pinned by title) | Refused with Project2 in front. If Revit gave both the same title, refused whichever is in front - *"two open models share its name"* |
| **NU13** | **A family in front.** In a chat working on a project, keep a change. Open a family for editing (it comes to the front), ask to undo | Refused, nothing sent, naming the family as the model on screen |
| **NU14** | **Undo x3, redo x1.** Keep a rename (X1). Move a wall. Keep the SAME rename capability again (X2 - one name, two keeps). Press Ctrl+Z three times, then Ctrl+Y once. Ask the chat to undo | **Refused** - the top is X1, the older keep. Press Ctrl+Y twice more and ask again: Undo is sent, for X2, and the reply gives X2's time. The log shows the depth counting down |
| **NU15** | **Ctrl+Y, then ask again.** After NU1, press Ctrl+Y (Redo), then ask the chat to undo | Undo is sent again - Heron's change came back and nothing else happened. *The negative:* after NU1 move a wall, then press Ctrl+Y (Revit has nothing to redo), then ask - refused |
| **NU16** | **Your own temporary hide/isolate.** Keep a change. Use Revit's own Temporary Hide/Isolate (the sunglasses) on something, then ask to undo | **Not known which.** If Revit reports the isolate as a change, refused, naming it. If it does not, Undo is sent - then look at what it took: Heron's change, or your isolate. **If it took the isolate, that is a defect: record it, and the guard must count it** |
| **NU17** | **Reload Latest** (Revit 2021 or later, workshared): keep a change, Collaborate > Reload Latest, ask to undo | Refused - *"was followed by Reload Latest ... Revit clears its undo list then"*. **On Revit 2020** the event does not exist and the log says so at startup: do the same and write down what the chat answers - if Undo is sent, look at what Revit did with it |
| **NU18** | **First save.** New project, never saved. Keep a change. Save it (its name changes). Look at Revit's Undo arrow - is `Heron: ...` still there? Ask to undo | Refused - *"the model was saved under a new name since"*. **Write down whether Revit's list still held the entry**: if it did, the refusal is stricter than it needs to be, and a later decision can relax it |
| **NU19** | **Save As**, a saved project under a new name: the same | The same answer, and the same thing to write down. *The negative:* an ordinary Save (Ctrl+S, same name) after a keep, then ask - Undo is sent; check Revit's list still had the entry, and if Save cleared it, record that as a defect |
| **NU20** | **Undo while Revit cannot run it.** Keep a change. Start sketching a floor (do not finish), then ask to undo | Refused - Revit will not take the command now - **or** Revit never takes the request at all (*"Revit is busy"*). Either way nothing is undone. If the reply says Undo was SENT, finish the sketch and look: **what did Undo take?** Record it |
| **NU21** | **`PostCommand(Undo)` on Revit 2020** - NU1 and NU2, on Revit 2020 | As NU1 and NU2. This is the oldest release, and nothing has posted Undo from an external event there |
| **NU22** | **Changes OFF.** Keep a change with Changes on, turn Changes off in Heron's Settings, ask to undo | Refused - Changes is off. Nothing undone. Then ask the chat to *check* whether it could undo (NU25): it answers, with Changes still off |
| **NU23** | **Revit in the middle of a command.** Keep a change. Start Revit's Move on a wall and leave it waiting for a click, then ask to undo | *"Revit is busy"* - nothing sent - or refused. Never an Undo that lands inside your Move |
| **NU24** | **Another job straight behind the undo.** Ask the chat to undo and, in the same message, to rename another view | The undo is SENT; the rename either runs AFTER Revit has undone - the log shows `undo [...]` before the rename's commit - or is held back once (*"Revit has not reported it done yet ... nothing was run"*) and works when asked again. **Never:** the rename undone and the first change still standing |
| **NU25** | **`check_only`.** Keep a change and ask the chat *"could you undo that?"* (`revit_undo_last` with `check_only`). Then move a wall and ask again | First: *"Yes - Undo would be sent now, for ..."* and **nothing is undone** - Revit's Undo arrow still shows `Heron: ...` on top. Then: *"No - Undo would not be sent now"* with the reason. Both work with Changes off |
| **NU26** | **The kept reply's last line.** Keep a rename; keep an `OPEN_VIEW` | The rename's reply ends *"To take this back: revit_undo_last sends Revit's own Undo for it ..."*; the open-view's ends *"Nothing to take back ..."* |
| **NU27** | **A job that syncs or saves, then undo.** With Publish on, keep a job that synchronizes with central (or saves) through Heron, then ask to undo | Refused - Revit counted no element change in it, or the sync cleared the list. **Not handled apart in the code:** if the job also changed elements and Undo is SENT, write down what Revit undid - a sync is not taken back by Undo, and the reply must not read as if it were |
