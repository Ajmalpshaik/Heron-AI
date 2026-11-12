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

**The model:** a saved scratch project copied for the purpose - never the owner's own work. Changes ON.

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
