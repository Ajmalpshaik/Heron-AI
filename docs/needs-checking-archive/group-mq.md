# NEEDS-CHECKING archive — Group MQ

> **Checks that were done, moved out of the live register.** These are rows of Group MQ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the visualiser's answer no longer says a 3D view is not one, and names what a preset replaced: `APPLY_VISUAL_PRESET` v4, `REPORT_VISUAL_PRESET` v4 (2026-11-07)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row MQ0

*Moved from the register on 2026-11-17.*

**Run.** *Open.* Revit 2024 (where it was found), on a **saved test copy** with a 3D view and a floor plan. Changes on at the ribbon

**Look for.** The title bar names the copy

---

### Row MQ1

*Moved from the register on 2026-11-17.*

**Run.** *REPORT_VISUAL_PRESET - D-30 pair, NEGATIVE FIRST.* Negative: preset "Studio Glow" on the 3D view. Positive: the 3D view, preset Exterior Day

**Look for.** Negative: refused naming the seven presets, nothing compared. Positive: `noRender` and `noDisplay` named only on the line of values that are code; **no "is not a 3D view" and no "has no Graphic Display Options" anywhere in the answer**. Version 3 printed both here

---

### Row MQ2

*Moved from the register on 2026-11-17.*

**Run.** *REPORT_VISUAL_PRESET on the floor plan, preset Exterior Day*

**Look for.** Each render setting skipped still says *"is not a 3D view, and only a 3D view has Rendering Settings"* - the sentence is made where it is true

---

### Row MQ3

*Moved from the register on 2026-11-17.*

**Run.** *APPLY_VISUAL_PRESET - D-30 pair, through Keep or Discard.* Negative: preset "Studio Glow". Positive: the 3D view, preset Exterior Day, renderFor issue

**Look for.** Negative: refused, nothing changed. Positive: the same absence as MQ1; Render Quality High and the output DPI read back as set. Keep or Discard is the modeller's

---

### Row MQ4

*Moved from the register on 2026-11-17.*

**Run.** *APPLY_VISUAL_PRESET on the floor plan*

**Look for.** The render settings skipped, each naming the reason, as MQ2

---

### Row MQ5

*Moved from the register on 2026-11-17.*

**Run.** *APPLY_VISUAL_PRESET after a hand-set value, through Keep or Discard.* SET_GRAPHIC_DISPLAY_OPTIONS Sun Intensity 60 on the 3D view, then preset Exterior Day

**Look for.** A finding *"Replaced what '<view>' had:"* naming Sun Intensity 60 -> 80, and each other setting it changed from and to. Version 3 said only the count

---
