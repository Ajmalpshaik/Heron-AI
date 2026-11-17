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

### Row EL2

*Moved from the register on 2026-11-17.*

**Run.** Whether `OverrideResult` works for that dialog. Read EL1's `dialogs` entry

**Look for.** `answer` is Cancel or Close - Revit took the override. If it reads *none - Revit would not take an answer*, the dialog stayed open: close it by hand, record that here, and `BasicFileInfo.Extract` before `LoadFamily` (row 300's other fix) becomes the way for this dialog **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row EL5

*Moved from the register on 2026-11-17.*

**Run.** The negative. `LOAD_FAMILY` with a family saved in Revit 2020, and any read such as `revit_health`

**Look for.** Neither reply carries `dialogsAnswered` or `dialogs`, the family loads as before, and the audit lines have no `dialogs` number - a job no dialog interrupted reads exactly as it did. **The D-30 negative** **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---
