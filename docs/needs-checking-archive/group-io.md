# NEEDS-CHECKING archive — Group IO

> **Checks that were done, moved out of the live register.** These are rows of Group IO of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *one form of a family pictured and read on its own: `revit_view_image` with `elements` in a family, and `REPORT_FAMILY_FORMS` version 3 (2026-10-22)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IO0

*Moved from the register on 2026-11-17.*

**Run.** *Negatives FIRST.* In the family: `revit_view_image`, `elements` = `999999999`; then `elements` = Ref. Level's number and the *Center (Front/Back)* reference plane's. In the project: `elements` = a level's number and a grid's

**Look for.** The first refused naming the id, *"name no placed element"*. The second and third refused `nothing_to_frame`, each one named *"is a datum"* - **a family's datums are still left out, and nothing changed for a project**. No picture, nothing made, the family's Views list and Undo unchanged **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO1

*Moved from the register on 2026-11-17.*

**Run.** *The fault's own case (5b-1311).* In the family: count the forms (`REPORT_FAMILY_FORMS`) and note Undo; `revit_view_image`, `elements` = the first solid's number

**Look for.** **The picture of the solid in 3D**, close up, with no level or reference plane drawn; the words say 1 framed, nothing left out. Then the forms counted again, Undo unchanged, and no new 3D view under Views **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO2

*Moved from the register on 2026-11-17.*

**Run.** `elements` = all three forms' numbers, and Ref. Level's

**Look for.** 3 framed, Ref. Level left out *"is a datum"*. **Picture:** both solids, the hole the void cuts in the first one. The subcategory solid framed as before the change - it had a category **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO3

*Moved from the register on 2026-11-17.*

**Run.** *The read by id (5b-1312).* `REPORT_FAMILY_FORMS`, `which` = the void's id exactly as `create-family-extrusion` gave it back

**Look for.** `formCount` 1, the void alone with that id, X and Y -50 to 50 mm, Z -10 to 510 mm; `scanned` 3; the finding *"1 form(s) of 3 in the family, by id"* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO4

*Moved from the register on 2026-11-17.*

**Run.** `which` = the two solids' ids, commas between, the first typed twice

**Look for.** `formCount` 2, each once; `scanned` 3 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO5

*Moved from the register on 2026-11-17.*

**Run.** *Nothing else moved.* `which` = `all`, `void`, and left out

**Look for.** `formCount` 3, 1 and 3, as version 2 answers; the left-out call's binding note *which not given - "all", the card's own default (optional)* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO6

*Moved from the register on 2026-11-17.*

**Run.** *Negatives.* `which` = the first solid's id with its FIRST character changed (never the last eight: those are the element's number); the solid's id and *Center (Front/Back)*'s UniqueId; `void, <the solid's id>`; the solid's number, digits only; `cylinder`. Then in the project, `which` = the solid's id

**Look for.** Each refused, *"Nothing was read"*, `formCount` 0, `scanned` 0: *"is not an id in this family"*; the plane named *", not a form"*, the solid not read either; *"not both in one call"*; *"that is an element number"*; *"nor a kind of form or a side"*. The project: `notAFamily` true, nothing read **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---

### Row IO7

*Moved from the register on 2026-11-17.*

**Run.** *The job it was for.* On a scratch copy of one of the 2026-10-21 CCTV monitor families: flex it (`flex-family`, or a type value changed and put back), then `REPORT_FAMILY_FORMS` with one form's id, and `revit_view_image` with that form's number

**Look for.** Its extents after the flex on one line, not ~100 forms; the picture of that one part. **Look at the picture** **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544*, which records it run and passed.

---
