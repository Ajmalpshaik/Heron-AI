# NEEDS-CHECKING archive — Group ER

> **Checks that were done, moved out of the live register.** These are rows of Group ER of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a picture of a view, handed to the chat: `capture_view` and `revit_view_image` (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row ER1

*Moved from the register on 2026-11-17.*

**Run.** `revit_view_image` with `view` empty, *Level 1* floor plan in front

**Look for.** **The picture shows in Claude** - the chat describes the walls and ducts it sees, and the words name 'Level 1', FloorPlan and *Heron Capture Test*. The D-30 positive for a named view **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row ER5

*Moved from the register on 2026-11-17.*

**Run.** `revit_view_image` with `elements` = an id that is not in the model, and with `elements` = a duct id and `view` = `Level 1` together

**Look for.** Each refused before anything is made - the first naming the id, the second saying one or the other. Element count and Undo unchanged **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row ER7

*Moved from the register on 2026-11-17.*

**Run.** After ER1 to ER6, the folder `HeronPaths.Captures` names (Heron's `captures` folder under Derived)

**Look for.** Empty, or not there: every capture deleted its own folder. A folder left in it is a capture that did not finish, and is safe to delete **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row ER8

*Moved from the register on 2026-11-17.*

**Run.** `revit_view_image` with `width` = 9000

**Look for.** The picture comes back 2400 px wide and the words say the width was clamped **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---
