# NEEDS-CHECKING archive — Group IB

> **Checks that were done, moved out of the live register.** These are rows of Group IB of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *rendering: render settings, sun, display options, a framing camera, Revit's Render dialog and the read-back (2026-10-20)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IB1

*Moved from the register on 2026-11-17.*

**Run.** *`FRAME_ELEMENTS_IN_CAMERA`.* One duct heater selected; viewName `Heron Frame - Heater`, direction south-east, lookDownDeg 30, distanceMm auto, marginPercent 10. **Negative:** the heater selected, viewName `{3D}`

**Look for.** **Positive:** madeNew true, framed 1. **Open the view: the heater fills it with a margin, seen from the south-east and above** - the check the numbers cannot make. Note readBack's crop box numbers beside what is seen ([row 5b-1267](../fragment-issues/section-5b-rows-1251-1275.md) wants them). **Negative:** refused *"is an isometric 3D view"*; `{3D}`'s camera unchanged **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop - all nine signed by Ajmal PS, PROVEN*, which records it run and passed.

---

### Row IB6

*Moved from the register on 2026-11-17.*

**Run.** *`REPORT_RENDER_SETTINGS` - tracking (D-53).* view `Heron Frame - Heater`; then the floor plan; then the schedule

**Look for.** sectionsRead **4**, then **2**, then **0** - the count follows the view. Each section matches its dialog opened by hand **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop - all nine signed by Ajmal PS, PROVEN*, which records it run and passed.

---

### Row IB7

*Moved from the register on 2026-11-17.*

**Run.** *`OPEN_RENDER_DIALOG`, through `revit_show`.* `Heron Frame - Heater` in front, command Render. **Negative:** the floor plan in front

**Look for.** **Positive:** posted true and Revit's Render dialog opens on the 3D view once the answer returns. **Negative:** posted false, refused *"is not a 3D view"*, no dialog **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop - all nine signed by Ajmal PS, PROVEN*, which records it run and passed.

---

### Row IB8

*Moved from the register on 2026-11-17.*

**Run.** *`READ_RENDERED_IMAGES` - the negative, BEFORE IB9.* folder the empty `out` folder

**Look for.** renderings 0, report *"Nothing rendered was read back"* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop - all nine signed by Ajmal PS, PROVEN*, which records it run and passed.

---
