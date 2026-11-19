# NEEDS-CHECKING archive — Group JE

> **Checks that were done, moved out of the live register.** These are rows of Group JE of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *three sheets moved onto another title block family in one call: `SET_SHEET_TITLE_BLOCK` version 2 (2026-10-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JE0

*Moved from the register on 2026-11-17.*

**Run.** *Open.* The copy for this release open and in front in that Revit, nothing else of the owner's in it

**Look for.** The title bar reads *JE Title Blocks* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---

### Row JE3

*Moved from the register on 2026-11-17.*

**Run.** *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/set-sheet-title-block-je-2026-10-28.yaml --session <pid>`, its positive leg

**Look for.** **`replaced` 3**, JE-01, JE-02 and JE-03 each named *"'C 17 x 22 Horizontal : C 17 x 22 Horizontal' was DELETED and replaced"*; `refused` empty, `swappedInPlace` 0, `alreadyCarried` 0. The binding note reads *"elements from find-sheets (3)"*. **FAIL:** *"threw while running ... The referenced object is not valid"* - version 1's answer - or `refused` naming JE-02 or JE-03 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---

### Row JE4

*Moved from the register on 2026-11-17.*

**Run.** *The negative - the D-30 negative.* JE3's negative leg, the same three sheets onto the type they carry

**Look for.** `replaced` empty, `swappedInPlace` 0, **`alreadyCarried` 3**, `refused` empty **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---

### Row JE5

*Moved from the register on 2026-11-17.*

**Run.** *Nothing kept.* JE2 again

**Look for.** All three still on *C 17 x 22 Horizontal*: both legs were rolled back **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-10-28*, which records it run and passed.

---
