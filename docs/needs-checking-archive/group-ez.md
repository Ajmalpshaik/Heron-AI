# NEEDS-CHECKING archive — Group EZ

> **Checks that were done, moved out of the live register.** These are rows of Group EZ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a fragment that never ends: the run clock, and a stop sent over the bridge (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EZ1

*Moved from the register on 2026-11-17.*

**Run.** **A runaway fragment on a scratch model is stopped and rolled back within the timeout.** `python mcp/client/heron_bridge_client.py fragment zz-ej-runaway-probe --write --apply`, with `revit.operationTimeoutSeconds` at its default 60, timed

**Look for.** Within 60 s - near 50 - the answer is `ran_too_long`, naming *50 seconds* and the 60 Heron waits, and *Revit confirmed it: the model is exactly as it was*. The model line count is what it was before, and Revit's Undo list has no *Heron: zz-ej-runaway-probe* entry although `--apply` was sent. **The negative:** the same with `zz-ej-swallow-probe`, which catches the throw and returns normally - the same `ran_too_long`, and still no line. **A FAIL** - `still_running` at 60 s, or a line left in the model - would mean the clock never tripped or the trip was not asked after the script: check the add-in deployed is this one before anything else **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row EZ2

*Moved from the register on 2026-11-17.*

**Run.** **Revit stays responsive** - EZ1 again, watching Revit rather than the chat

**Look for.** Revit is frozen while the probe runs, as it always was: the clock cannot make the loop share the thread. What is new is that it COMES BACK: inside the 60 s the activity banner lowers with *Ran too long - Heron stopped it*, Revit takes clicks again, its process id is the one it had before, `ping` answers, and a real read straight after returns the model. **A FAIL** - Revit still frozen after 60 s, or a new process id - means the loop was never stopped, and Revit has to be ended by hand as before this change **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---
