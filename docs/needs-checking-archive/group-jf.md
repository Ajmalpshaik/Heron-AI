# NEEDS-CHECKING archive — Group JF

> **Checks that were done, moved out of the live register.** These are rows of Group JF of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a parameter named by its GUID when two shared parameters carry one name: `SET_PARAMETER_VALUES_BY_ID` version 6 (2026-10-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JF2

*Moved from the register on 2026-11-17.*

**Run.** *Check the arrangement.* `REPORT_PARAMETER_INVENTORY` on the JF-WALL wall's id, `parameterFilter=jf tag`, `fullRecord=true`

**Look for.** Two *JF Tag*, both String, shared, writable, GUIDs *A* and *B*, both blank; *"A NAME CARRIED TWICE"* names them **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---

### Row JF3

*Moved from the register on 2026-11-17.*

**Run.** *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/set-parameter-values-by-id-jf-2026-10-28.yaml --session <pid>`, its positive leg: `key=Mark\|in=Walls;JF-WALL\|JF Tag {A}=ONE`

**Look for.** **`written` 1**; readBack name *JF Tag {9a6f71be-...}*, value *ONE*; `refused` empty; rolled back. **FAIL:** version 5's *"two parameters on it are called JF Tag"* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---

### Row JF4

*Moved from the register on 2026-11-17.*

**Run.** *The negative - the D-30 negative.* Its negative leg: `key=Mark\|in=Walls;JF-WALL\|JF Tag=ONE`, no GUID

**Look for.** `written` 0; `refused` names JF-WALL - two parameters on it are called JF Tag, and says to name the one meant as *'JF Tag {its GUID}'* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---

### Row JF5

*Moved from the register on 2026-11-17.*

**Run.** *Nothing kept.* JF2 again

**Look for.** Both *JF Tag* still blank **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---
