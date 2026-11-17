# NEEDS-CHECKING archive — Group NF

> **Checks that were done, moved out of the live register.** These are rows of Group NF of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a family's 2D side through five widened cards: a detail item placed and arrayed in a family view, a symbolic line's detail levels, lines on a subcategory, an arc's radius labelled: `PLACE_DETAIL_COMPONENTS` v2, `ARRAY_FAMILY_FORMS` v4, `SET_FAMILY_FORM_VISIBILITY` v2, `SET_FAMILY_FORM_SUBCATEGORY` v2, `LABEL_FAMILY_RADIUS` v6 (2026-11-11)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row NF3

*Moved from the register on 2026-11-17.*

**Run.** *ARRAY_FAMILY_FORMS, OLD path* - FORM-ID, count 3, step "X 700"; negative VANE-ID, count 3, step "Y 20" - version 3's own proof negative; rolled back

**Look for.** Arrayed in the 3D view as version 3 did, every copy read back by its box; the negative refused after the trial row - the copies moved 0 mm, "Center (Front/Back)" named - through the leavesPlane branch that now sits after version 4's ownerView branch **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF4

*Moved from the register on 2026-11-17.*

**Run.** *ARRAY_FAMILY_FORMS, NEW* - ARROW-ID, count 4, step "X 200"; negative step "Z 100"; rolled back

**Look for.** Arrayed IN Ref. Level, four in all 200 mm apart, each copy owned by Ref. Level and read by its insertion point; the negative refused before anything is made (out of the view's plane) **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1574's repair - Run 1 below; the card signed PROVEN that day.**

---

### Row NF5

*Moved from the register on 2026-11-17.*

**Run.** *SET_FAMILY_FORM_VISIBILITY, OLD path* - FORM-ID, "medium, fine", "front, left"; negative "rough"; rolled back

**Look for.** As version 1: the form's setting before and after; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF5b

*Moved from the register on 2026-11-17.*

**Run.** *OLD, the empty list* - FORM-ID, "all", "all"; negative views "" (version 1's tests case); rolled back

**Look for.** Every box ticked again; the negative refused as naming no kind of view - "" binds as left out, i.e. "unchanged", and a FORM with "unchanged" is refused, so version 1's behaviour holds **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF5c

*Moved from the register on 2026-11-17.*

**Run.** *OLD, the proof's negative* - NF5's positive, negative run against "Heron NF Project"; rolled back

**Look for.** `notAFamily` true on the project, nothing changed - version 1's proof negative **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF6b

*Moved from the register on 2026-11-17.*

**Run.** *The unmeasured half, in the job file* - SYM-LINE-ID, "all", views "plan"; negative DETAIL-LINE-ID; rolled back

**Look for.** EITHER the line reads "plan" back, OR the call is refused naming what Revit read (or "not readable"). A refusal on the positive leg is the finding, not a fault of the run. Record which: it settles whether a symbolic line's setting keeps kinds of view, and the card's sentence is fixed from it **ANSWERED 2026-11-12, Revit 2020 (session 33000): refused - a symbolic line keeps no kinds of view through the API. Its negative ran on the box with "unchanged", refused, where the row names a detail line; the measurement the row asks to record is the positive leg. Run 1 below; the card signed PROVEN that day.**

---

### Row NF6c

*Moved from the register on 2026-11-17.*

**Run.** *NEW, views left empty* - SYM-ARC-ID, "medium, fine", views ""; negative FORM-ID with views "unchanged"; rolled back

**Look for.** The arc's detail levels set, its kinds of view untouched; the negative refused - "unchanged" is for symbolic lines only **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF7

*Moved from the register on 2026-11-17.*

**Run.** *SET_FAMILY_FORM_SUBCATEGORY, OLD path* - FORM-ID on "Casing"; negative subcategory ""; rolled back

**Look for.** As version 1: "Casing" made, the form on it; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF7b

*Moved from the register on 2026-11-17.*

**Run.** *OLD, back and the proof's negative* - FORM-ID, subcategory "none"; negative run against "Heron NF Project"; rolled back

**Look for.** The form on the family's own category (version 1's second positive); `notAFamily` true on the project (its proof negative) **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1571's repair - Run 1 below; the card signed PROVEN that day.**

---

### Row NF8

*Moved from the register on 2026-11-17.*

**Run.** *SET_FAMILY_FORM_SUBCATEGORY, NEW* - SYM-LINE-ID and SYM-ARC-ID on "Flow Arrow"; negative ARROW-ID; rolled back

**Look for.** "Flow Arrow" made, both lines on it, read back by line style; the negative refused as neither a form nor a line, no subcategory made. **If a new subcategory is not on the lines' offered styles until something else happens, the call refuses and rolls back - record it** **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF8b

*Moved from the register on 2026-11-17.*

**Run.** *NEW, "none" on a line* - SYM-LINE-ID, "none"; negative subcategory ""; rolled back

**Look for.** The line on the family category's own projection style, read back; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF8c

*Moved from the register on 2026-11-17.*

**Run.** *NEW, a detail line* - in "Heron NF Arrow", SHAFT-ID on "Arrow Head"; negative subcategory ""; rolled back

**Look for.** "Arrow Head" made under Detail Items and the shaft on it, read back - or refused by name with nothing kept if Revit does not offer it that style; record which **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF9

*Moved from the register on 2026-11-17.*

**Run.** *LABEL_FAMILY_RADIUS, OLD path* - CYL-ID, "Shaft Radius", radius; negative FORM-ID; rolled back

**Look for.** As version 5: a radial dimension in the cylinder's sketch reading 10; the negative refused, no 10 mm circle **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF9b

*Moved from the register on 2026-11-17.*

**Run.** *OLD, the proof's negative* - CYL-ID as NF9; negative REV-ID with "Bell Radius"; rolled back

**Look for.** Version 5's proof negative: refused, no reference plane through the revolve's axis, nothing labelled **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NF10

*Moved from the register on 2026-11-17.*

**Run.** *LABEL_FAMILY_RADIUS, NEW, symbolic* - SYM-ARC-ID, "Arc Diameter", diameter; negative SYM-LINE-ID; rolled back

**Look for.** A diameter dimension on the symbolic arc in the Ref. Level plan reading 300, the arc read back at radius 150; the negative refused as not an arc **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1572's repair - Run 1 below; the card signed PROVEN that day.**

---

### Row NF11

*Moved from the register on 2026-11-17.*

**Run.** *LABEL_FAMILY_RADIUS, NEW, detail* - in "Heron NF Arrow", ARC-ID, "Arc Radius", radius; negative SHAFT-ID; rolled back

**Look for.** A radial dimension on the detail arc in Ref. Level reading 150; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---
