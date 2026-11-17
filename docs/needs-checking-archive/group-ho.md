# NEEDS-CHECKING archive — Group HO

> **Checks that were done, moved out of the live register.** These are rows of Group HO of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *PLACE_STRUCTURAL_FAMILY v4: a beam is refused, a placement with no geometry is a failure (2026-10-19)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row HO1

*Moved from the register on 2026-11-17.*

**Run.** *Positive - a column still goes in.* Two points on Level 1, the structural column type, `structuralType=Column`, `topLevelName` and `topOffset` blank

**Look for.** `placed` 2, `failed` 0, a finding *"2 placed as Column on 'Level 1'"*, and **no** finding about solid geometry. Both columns read as structural in the column schedule. Then once more with `topLevelName=Level 2`, `topOffset=-150`: Top Level and Top Offset read so in Properties, `tops` 2 lines, `topWrong` empty **CLOSED 2026-11-13, not passed** - `PLACE_STRUCTURAL_FAMILY` was retired into `PLACE_FAMILY_INSTANCES` on 2026-11-01 (its `deprecated:` block); `PLACE_FAMILY_INSTANCES` is PROVEN at version 5, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row HO2

*Moved from the register on 2026-11-17.*

**Run.** *Negative - row 5b-1201's own case.* The Structural Framing type, `structuralType=Beam`, Level 1, points `23420,0,0; 23640.272,2142.771,4000; 23569.859,4319.298,8000`

**Look for.** **Nothing placed:** `placed` empty, `failed` 0, one finding naming the family as Structural Framing and saying to ask `PLACE_LINE_BASED_FAMILY` with `curves`. The Structural Framing count is the same as before; no *"Line is too short"* or *"Can't create sweep"* warning. Then the same type with `structuralType=Column`: refused the same way. **FAIL:** any beam in the model **CLOSED 2026-11-13, not passed** - `PLACE_STRUCTURAL_FAMILY` was retired into `PLACE_FAMILY_INSTANCES` on 2026-11-01 (its `deprecated:` block); `PLACE_FAMILY_INSTANCES` is PROVEN at version 5, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row HO3

*Moved from the register on 2026-11-17.*

**Run.** *Negative - a brace of another family.* The Generic Model family, `structuralType=Brace`, one point

**Look for.** Refused, *"A brace is placed along a line between its two ends"*, naming `PLACE_LINE_BASED_FAMILY`; nothing placed. Then the column type with `structuralType=Beam`: version 3's sentence - a Structural Columns family, ask for it as a column - and nothing placed **CLOSED 2026-11-13, not passed** - `PLACE_STRUCTURAL_FAMILY` was retired into `PLACE_FAMILY_INSTANCES` on 2026-11-01 (its `deprecated:` block); `PLACE_FAMILY_INSTANCES` is PROVEN at version 5, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row HO4

*Moved from the register on 2026-11-17.*

**Run.** *The geometry read-back.* The Generic Model family (model lines only), two points, `structuralType=non-structural`

**Look for.** `placed` empty, `failed` 2, one finding naming both points as left with no solid geometry and removed again; the Generic Models count is the same as before; **no** delete box (the elements were made by the same run). **Say which it was** if Revit kept them: whether `get_Geometry` at Fine detail read a solid that is not there, or the delete was refused **CLOSED 2026-11-13, not passed** - `PLACE_STRUCTURAL_FAMILY` was retired into `PLACE_FAMILY_INSTANCES` on 2026-11-01 (its `deprecated:` block); `PLACE_FAMILY_INSTANCES` is PROVEN at version 5, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---
