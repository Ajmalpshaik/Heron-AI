# NEEDS-CHECKING archive — Group CC

> **Checks that were done, moved out of the live register.** These are rows of Group CC of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *building loads from the model: the take-off, the loads written back, and their read-back (2026-10-04)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CC1

*Moved from the register on 2026-11-17.*

**Run.** **A window type's U and SHGC** - open one window type's Type Properties, Analytical Properties, beside the take-off's `types` entry for it

**Look for.** `u_w_m2k` and `shgc` are the **same numbers** the palette shows. The U is read as is, on the assumption that Revit's internal unit for it IS W/(m2.K) - **the first thing checked**. A type with no SHGC set reads `null`, never 0 **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** M_Fixed 0915 x 1220mm: the take-off's u_w_m2k 3.6886 and shgc 0.78, the Analytical Properties' 3.6886 W/(m2.K) and 0.78 (read by a temporary read probe). The M_Skylight-Flat 600 x 600mm type carries no thermal values and reads null for both, never 0. **Struck 2026-10-18:** passed 2026-10-06, as recorded in this row - struck like CC5 to CC7 beside it.

---

### Row CC2

*Moved from the register on 2026-11-17.*

**Run.** **A wall type's U and absorptance**, and a roof's, the same way

**Look for.** Same numbers as the palette. A Basic Wall whose thermal values come from its layers: say whether Revit fills the type's U at all, or leaves it empty (the brain then refuses that Space, by name) **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Exterior - Render on Brick on Block: U 0.230561, absorptance 0.7 against the palette's 0.2306 and 0.7; Warm Roof - Concrete 0.586061 against 0.5861. Revit DOES fill a Basic Wall's U from its layers - from the layers ONLY, no surface resistances (Generic - 225mm Masonry: R 0.1731 = 0.225 m / 1.30 W/(m.K)); what that does to a load is [FRAGMENT-ISSUES 5b-331](../FRAGMENT-ISSUES.md). **Struck 2026-10-18:** passed 2026-10-06, as recorded in this row - struck like CC5 to CC7 beside it.

---

### Row CC5

*Moved from the register on 2026-11-17.*

**Run.** **A corner office** - its outside walls measured by hand on the plan

**Look for.** Two wall faces `beyond: outside`, facing two different ways; each face's `area_m2` against the hand take-off (finish face, floor to the Space's top) within 2 %; each window's `area_m2` its width x height **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Office 101: the south face 21.7493 m2 (azimuth 180) and the west 17.9493 m2 (270), both beyond outside; by hand 5.7235 x 3.8 m and 4.7235 x 3.8 m - exact. Each window 1.1163 m2 = 0.915 x 1.22, its centre 1.51 m up (900 sill + 610).

---

### Row CC6

*Moved from the register on 2026-11-17.*

**Run.** **A Space under the roof** and **the corridor wall between two offices**

**Look for.** A `top` face `beyond: outside`; the corridor wall `beyond: space` with the OTHER Space's id **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Office 201's top face (Warm Roof - Concrete) beyond outside; the partition between Offices 101 and 102 beyond space 352679 seen from 101, and 352678 seen from 102.

---

### Row CC7

*Moved from the register on 2026-11-17.*

**Run.** **A Space not enclosed** (Not Enclosed in the Space schedule) - THE NEGATIVE CASE

**Look for.** `placed: false` and `faces: []` - never a zero-area row WITH faces. The brain lists it and leaves it out **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Space 202, placed in the open at (30000, 5000) mm on Level 1, Not Enclosed: placed false, area 0, no faces - and the loads list it under left out (not placed): 1.

---

### Row CC16

*Moved from the register on 2026-11-17.*

**Run.** **The take-off gate** - Finalize before and after **The take-off is right**, then a wall moved in Revit and the loads read again

**Look for.** Before: Finalize is shut and the report says DRAFT. After: Finalize opens and the report names who confirmed it and when. After the wall moved: the confirmation is gone and must be given again **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Finalize stayed shut until 'The take-off is right' and opened after it (the owner's screenshot: Confirmed - the report is final, Finalize to Revit blue). The partition between Offices 101 and 102 moved 100 mm: the next read said NOT CONFIRMED (take-off 6052bf9e86367a19, Office 101 27.507 m2). Moved back to the confirmed geometry (take-off a76b5e966e1e3ab6 again): still NOT CONFIRMED - it had to be given again; placing diffusers cleared it too. The record names 'the modeller, in the Heron Companion' and the time, not a person.

---

### Row CC18

*Moved from the register on 2026-11-17.*

**Run.** **A Space under a Space** - an office on Level 1 under an office on Level 2, a concrete slab between

**Look for.** The office's top face reads beyond `space` with the upper office's id - never `unknown`. Try a slab thicker than 150 mm: the look starts past its thickness **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Office 103's top face, a 225 mm Insitu Concrete slab, reads beyond space 352683 - Office 201 - and Office 201's bottom reads 352680; never unknown.

---

### Row CC19

*Moved from the register on 2026-11-17.*

**Run.** **A flat roof drawn as a Floor**

**Look for.** Its face reads `unknown`, and Heron asks once for that element: answered `outside`, it is counted as a roof with sun on it **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** Floor: Beam and Block 200mm over Offices 101 and 102 and the Meeting Room read beyond unknown, and Heron asked once for that element (beyond:352590, naming the three Spaces). Answered outside, it is counted as a roof with sun: Office 101's 357.4 W is sol-air with 645 W/m2 on the horizontal less the 3.7 K sky term (the hand check gives 357.5 W).

---

### Row CC25

*Moved from the register on 2026-11-17.*

**Run.** **A window in an outside wall behind which no Space is placed**

**Look for.** A finding names it: its load is counted nowhere. Known limits, to see on the model: a wall that bounds no Space at all is never looked at, and an opening whose box middle sits far from the wall (deep shading in the family, a door modelled open) is called an orphan though a Space is behind it **RUN 2026-10-06 on Heron loads test (Revit 2024, session 51820): PASS.** The finding names window 352699 - the Store's, in the west wall that bounds Office 101 - 'so no load is counted for it'. Its wording says 'The windows 352699 ... sits'.

---
