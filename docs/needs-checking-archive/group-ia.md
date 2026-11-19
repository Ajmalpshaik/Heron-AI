# NEEDS-CHECKING archive — Group IA

> **Checks that were done, moved out of the live register.** These are rows of Group IA of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *materials: every tab of the Material Browser, material adding, and giving elements a material (2026-10-20)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IA3

*Moved from the register on 2026-11-17.*

**Run.** `SET_MATERIAL_PROPERTIES` on `IA Probe Steel`: `Thermal: Thermal Conductivity=50` (the units the project shows); then `Physical Asset=` a physical asset the template holds (read its name in IA1); then `Thermal Asset=No Such Asset`

**Look for.** **Positive:** `sharedAssets` names `IA Probe Copper`; `IA Probe Steel` has a new thermal asset named after it reading 50, and `IA Probe Copper`'s still reads what it did; the physical asset assigned. **Negative:** the unknown asset refused naming the closest; nothing changed **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop*, which records it run and passed.

---

### Row IA6

*Moved from the register on 2026-11-17.*

**Run.** `SET_MATERIAL_APPEARANCE` `IA Probe Steel`, `appearance` one of IA1's library names; then `appearance` `from IA Probe Wood` on `IA Probe Copper`; then `IA Probe Steel` with values `Colour=10,20,30` while it still shares with `IA Probe Copper` (on a fresh copy of the model); then `appearance` `No Such Look`; then a library Metal asset with `Glossiness=50`

**Look for.** **Positive:** Steel shows the library asset under its own name; Copper a copy of Wood's under its own name. **Negatives:** the shared edit gives Steel its own copy coloured 10,20,30 and names Copper in `sharedBefore`, Copper's colour unchanged; the unknown name refused naming the closest from the document and the library; the Metal asset with a value refused as not Generic **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop*, which records it run and passed.

---

### Row IA9

*Moved from the register on 2026-11-17.*

**Run.** `SET_ELEMENT_MATERIAL` with the two columns selected, `material` `TRG_Concrete Smooth`; again; the three ducts, `TRG_Duct Galvanised`; the two walls; the door with `materialParameter` left out, then with one of its material parameters named

**Look for.** **Positive:** the column type's Material written once, the report counting every placed column of the type; the Supply Air system type's Material written and said to reach every element of it - the ducts draw galvanised in Realistic. **Negatives:** the repeat `alreadyAsAsked` 1; the walls skipped naming `SET_COMPOUND_LAYER_WIDTH`; the door skipped naming its material parameters until one is named **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop*, which records it run and passed.

---

### Row IA11

*Moved from the register on 2026-11-17.*

**Run.** **The method.** `heron_method` for *"build a duct heater family that will be rendered"*

**Look for.** Step 12 names `SET_MATERIAL_APPEARANCE` with `matchShading` after `CREATE_MATERIAL` **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 - Revit 2020, the owner's laptop*, which records it run and passed.

---
