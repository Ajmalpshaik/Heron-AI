# NEEDS-CHECKING archive — Group JP

> **Checks that were done, moved out of the live register.** These are rows of Group JP of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a curtain system type's Grid 1 and Grid 2, and its mullions, set by the curtain wall setters: `SET_CURTAIN_WALL_GRID` version 3, `SET_CURTAIN_WALL_MULLIONS` version 2 (2026-10-29)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JP2

*Moved from the register on 2026-11-17.*

**Run.** *The positive - a curtain system type, rolled back.* In *Heron JP Curtain*, Revit 2020: `python tools/batch-prove.py tools/jobs/set-curtain-wall-grid-curtain-system-2026-10-29.yaml --session <pid>`, its positive leg: `curtainWallType=Heron JP 1500`, `grid=grid 1 layout=maximum spacing; grid 1 spacing=3000; grid 2 layout=minimum spacing; grid 2 spacing=2000`

**Look for.** `systemsOfType 1`, `wallsOfType 0`, `applied 4`, `differs` empty; `readBack` *grid 1: layout Maximum Spacing [Revit's value 3], was Fixed Distance [Revit's value 1] - asked; spacing 3000.0 mm, was 1500.0 mm* and *grid 2: layout Minimum Spacing [Revit's value 5] ... 2000.0 mm*. `measured`: one curtain system on **4** faces, vertical lines **36** and horizontal **76** before; now **vertical 16 and horizontal 56** (Grid 1 upright) **or vertical 26 and horizontal 36** (Grid 1 level). **Record**: which pair, the *By face* U and V counts, the *"Of Revit's grid ids ... carries"* line, and where Number, Justification, Angle and Offset sit. **FAIL:** a refusal naming the grid ids (the assumption wrong - record the ids it printed), a stop quoting Revit, or any other pair of counts **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JP4

*Moved from the register on 2026-11-17.*

**Run.** *The family and the type.* JP2's request with `curtainWallType=Curtain System : Heron JP 1500` - the family name JP2's `readBack` printed, if it is not *Curtain System*

**Look for.** JP2's answer. Two spellings of one name, one type **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP5

*Moved from the register on 2026-11-17.*

**Run.** *The negative - the D-30 negative.* The job file's negative leg: the SAME grid aimed at `curtainWallType=Curtain Wall: Storefront`

**Look for.** `applied 0`, `readBack` and `measured` empty; refused *"'grid 1 layout' is not a grid setting this changes. Name one or more of: vertical layout, vertical spacing, horizontal layout, horizontal spacing. Nothing was changed"* - version 2's words for a key a wall type does not have. Storefront as it was **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JP6

*Moved from the register on 2026-11-17.*

**Run.** *A wall type's keys on a curtain system type.* JP3's command with `grid=vertical layout=fixed distance; vertical spacing=1500`

**Look for.** Refused *"'vertical layout' is a curtain WALL type's setting"*, naming Grid 1 and Grid 2 and the four grid keys; nothing changed. Never read as Grid 1 **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP7

*Moved from the register on 2026-11-17.*

**Run.** *Fixed Number.* JP3's command with `grid=grid 1 layout=fixed number`

**Look for.** Refused *"Fixed Number is not set here"*, saying the reference does not say whether the type or each curtain system holds the Number; nothing changed **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP8

*Moved from the register on 2026-11-17.*

**Run.** *A name no type has.* JP2's request with `curtainWallType=Heron JP No Such Type`

**Look for.** Refused *"No wall type or curtain system type called ..."*, listing the model's curtain wall types and its curtain system types, *Heron JP 1500* among them; nothing chosen **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP11

*Moved from the register on 2026-11-17.*

**Run.** *The positive - a curtain system type's mullions, rolled back.* `python tools/batch-prove.py tools/jobs/set-curtain-wall-mullions-curtain-system-2026-10-29.yaml --session <pid>`, its positive leg: `curtainWallType=Heron JP 1500`, `mullions=grid 1 interior=Rectangular Mullion: 30mm Square`

**Look for.** `systemsOfType 1`, `applied 1`, `differs` empty; `readBack` *grid 1 interior: 'Rectangular Mullion: 30mm Square', was none*. `measured`: one curtain system on 4 faces, 36 vertical and 76 horizontal lines; mullions before *none*, now **720 x vertical** (Grid 1 upright: 36 lines cut into 20) **or 760 x horizontal** (Grid 1 level: 19 lines cut into 12 on each 18000 face, into 8 on each 12000 one). **Record** which - it must agree with JP2 - and the *"Of Revit's mullion ids ... carries"* line. **FAIL:** a stop quoting Revit, or any other count **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JP12

*Moved from the register on 2026-11-17.*

**Run.** *The other grid.* `python mcp/client/heron_bridge_client.py fragment --in "Heron JP Curtain" set-curtain-wall-mullions --write --set "curtainWallType=Heron JP 1500" --set "mullions=grid 2 interior=Rectangular Mullion: 30mm Square"`

**Look for.** `applied 1`, and the mullions the **other** of JP11's two answers **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP13

*Moved from the register on 2026-11-17.*

**Run.** *Already so.* JP12's command with `mullions=grid 1 border 1=none`

**Look for.** `applied 0`, `alreadyAsAsked 1`, not a refusal; *"It is used by 1 curtain system(s) in this model"* **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP14

*Moved from the register on 2026-11-17.*

**Run.** *The negative - the D-30 negative.* The job file's negative leg: the SAME mullions aimed at `curtainWallType=Curtain Wall: Storefront`

**Look for.** `applied 0`, `readBack` and `measured` empty; refused *"'grid 1 interior' is not a mullion position. Name one or more of: vertical interior, vertical border 1, vertical border 2, horizontal interior, horizontal border 1, horizontal border 2. Nothing was changed"* - version 1's words. Storefront as it was **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JP15

*Moved from the register on 2026-11-17.*

**Run.** *A wall type's position on a curtain system type.* JP12's command with `mullions=vertical interior=Rectangular Mullion: 30mm Square`

**Look for.** Refused *"'vertical interior' is a curtain WALL type's position"*, naming Grid 1 and Grid 2 Mullions and the six grid positions; nothing changed **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JP16

*Moved from the register on 2026-11-17.*

**Run.** *A mullion type the model has not got.* JP12's command with `mullions=grid 1 interior=HERON NO SUCH MULLION`

**Look for.** Refused *"No mullion type called 'HERON NO SUCH MULLION' in this model"*, with every mullion type the model has; nothing loaded **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---
