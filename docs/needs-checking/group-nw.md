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
