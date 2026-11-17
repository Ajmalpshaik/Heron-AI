# NEEDS-CHECKING archive — Group EL

> **Checks that were done, moved out of the live register.** These are rows of Group EL of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a Revit dialog met inside a job is answered, never waited on: `HeronDialogWatch` (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EL3

*Moved from the register on 2026-11-17.*

**Run.** Whether an add-in's own TaskDialog raises `DialogBoxShowing`. A throwaway read fragment, through `run_fragment_read`, whose whole body shows `TaskDialog.Show("Heron EL3", "Close me if you can see me.")` and returns

**Look for.** **Either** the reply lists it in `dialogs` with its id and `dialog_refused` (it does raise the event - so any Heron prompt inside a job MUST sit inside `HeronAsks()`), **or** the dialog appears and waits for a click (it does not). Write down which, and the id if one came - **Struck 2026-10-18:** answered on Revit 2024, 2026-10-18 - it raises the event - the results table under *The 2026-10-18 run* below.

---
