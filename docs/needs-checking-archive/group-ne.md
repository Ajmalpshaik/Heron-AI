# NEEDS-CHECKING archive — Group NE

> **Checks that were done, moved out of the live register.** These are rows of Group NE of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a family's 2D linked to a Yes/No by id, and an angle labelled between two planes: `LINK_FAMILY_PARAMETER` version 4 and `LABEL_FAMILY_DIMENSION` version 3 (2026-11-11)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row NE1

*Moved from the register on 2026-11-17.*

**Run.** *Version 3's form path, re-proved* - forms = the extrusion's id, links "Visible=Show Handle"; negative forms = the extrusion's and Box Left's ids; both rolled back

**Look for.** Positive: changed 1, "Extrusion id N: Visible not linked -> Show Handle". Negative: refused naming Box Left as none of the kinds linked by id - and the extrusion NOT linked (all or nothing) **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NE1b

*Moved from the register on 2026-11-17.*

**Run.** *Version 3's OWN proven case, re-proved* - category "Duct Connector", system "Supply Air", links "Flow=Nominal Air Flow"; negative "Flow=ZZZ No Such Parameter"; rolled back. Then by name: system "Exhaust Air"

**Look for.** Positive: changed 1, "Duct Connector (Supply Air) id N: Flow not linked -> Nominal Air Flow" - version 3's answer. Negative: refused, "No family parameter is called ZZZ No Such Parameter", naming Nominal Air Flow as the one that could drive Flow. Exhaust Air: refused with what the family has - **version 4's words** "Symbolic Line", "Filled Region", "Detail Line", "Model Text" where version 3 printed a Revit category; record the whole sentence **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NE2

*Moved from the register on 2026-11-17.*

**Run.** *The new ability* - forms = the symbolic line's and the filled region's ids, links "Visible=Show Arrow"; negative the symbolic line with "Visible=Arrow Length"; rolled back

**Look for.** Positive: changed 2, "Symbolic Line id N: Visible not linked -> Show Arrow", "Filled Region id M: ..." - **if Revit says Visible cannot be linked on either, that is the first thing this group learns** and the card is changed, not proved. Negative: refused naming Length and Yes/No, nothing linked **HELD 2026-11-12, Revit 2020 (session 33000), in two halves:** the symbolic line in the Generic Model family, the filled region - with a detail line - in the Detail Item family, since a 3D family has no filled region on 2020; the negative as written. Run 1 below; the card signed PROVEN that day.

---

### Row NE4

*Moved from the register on 2026-11-17.*

**Run.** *Version 2's length path, re-proved* - Box Left to Box Right, Width, centre "Center (Left/Right)"; negative the same with "Wrong Width" (450); rolled back

**Look for.** Positive: reading 600 mm, equalised true, angleDegrees 0 - version 2's answer. Negative: refused with 600 and 450, naming SET_FAMILY_TYPE_VALUES, nothing made **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NE4b

*Moved from the register on 2026-11-17.*

**Run.** *Version 2's OWN proven case, re-proved* - "Floor Top" to "Trap Top", "Trap Depth", no centre, pinFreeEnd left out; negative "Same A" to "Same B" with "Gap C"; rolled back

**Look for.** Positive: reading 300 mm in an elevation, `freeEnd` naming "Floor Top" as held by nothing and "Trap Top" as held by the dimension labelled "Rim Height" - version 2's answer. Negative: refused as in the same place, naming the pin route, nothing made **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NE5

*Moved from the register on 2026-11-17.*

**Run.** *The angle* - "Center (Front/Back)" to "Arrow Slope", "Arrow Angle", no centre; negative with "Wrong Angle" (45); rolled back

**Look for.** Positive: angleDegrees 30, labelled in Ref. Level, a finding that "Center (Front/Back)" defines the origin so "Arrow Slope" is expected to turn. Negative: refused before anything is made - "meet at 30 degrees (150 on the other side) and Wrong Angle holds 45". **If Revit refuses the arc or the references, that is the first thing learned** and the card is changed **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NE5b

*Moved from the register on 2026-11-17.*

**Run.** *Two free planes, and the pin* - "Arrow Slope" to "Arrow Low", "Spread Angle", pinFreeEnd=true; negative "Center (Front/Back)" to "Arrow Slope", "Arrow Angle", pinFreeEnd=true; rolled back

**Look for.** Positive: angleDegrees 45; `freeEnd` saying neither plane is pinned or defines the origin, "Pinned now"; `pinnedEnd` "Arrow Slope" pinned first and read back pinned. Negative: labelled, NOTHING pinned, the finding naming "Center (Front/Back)" as held fast **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.**

---

### Row NE6

*Moved from the register on 2026-11-17.*

**Run.** *The other side, and parallel planes* - the same planes with "Arrow Back Angle" (150); negative "Arrow Angle" between Box Left and Box Right; rolled back

**Look for.** Positive: angleDegrees 150, the arc on the obtuse side. Negative: refused as parallel, making no angle, nothing made **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1565's repair - Run 1 below; the card signed PROVEN that day.**

---
