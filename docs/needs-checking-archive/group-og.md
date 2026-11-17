# NEEDS-CHECKING archive — Group OG

> **Checks that were done, moved out of the live register.** These are rows of Group OG of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a wall's top shaped to the roof above it by editing its profile, on Revit 2022 to 2024, doors and windows kept, a sketch that does not follow the roof: `SHAPE_WALL_TOPS_TO_ROOF` (2026-11-16) - SUPERSEDED 2026-11-17 by Group OE* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row OG1

*Moved from the register on 2026-11-17.*

**Run.** ~~*Arrange, Revit 2024.* `of-gable`: on Level 1, four `Generic - 200mm` walls on their centrelines from (0,0) to (8000,0) to (8000,6000) to (0,6000) and back, unconnected height 2440; a single door in the WEST wall (x = 0); a roof by footprint on Level 1, base offset 2440, picked on the four walls with a 600 overhang, Defines Slope ticked on the NORTH and SOUTH edges only at 30 degrees - a gable roof whose ridge runs east-west, the east and west walls standing at the plate with the triangle open above them. Apart from it, a fifth wall from (14000,0) to (18000,0), height 2440, nothing over it. Save~~ **Struck 2026-11-17:** CLOSED, not passed - superseded 2026-11-17 by [Group OE](../needs-checking/group-oe.md) on main, where the card was proved and signed 2026-11-16.

**Look for.** ~~A section across x = 0 shows the open triangle over the west wall; the fifth wall has nothing above it~~

---

### Row OG2

*Moved from the register on 2026-11-17.*

**Run.** ~~*D-30 positive and negative, THE NEGATIVE FIRST.* The job file, `--write`, rolled back~~ **Struck 2026-11-17:** CLOSED, not passed - superseded 2026-11-17 by [Group OE](../needs-checking/group-oe.md) on main, where the card was proved and signed 2026-11-16.

**Look for.** ~~Negative, the fifth wall: shaped 0, `noRoof` naming it, nothing changed. Positive, the west wall: shaped 1, `keptInserts` 1 (the door), a finding naming the roof and the highest point above the base - at the ridge (y = 3000), some 3000 x tan 30 = 1730 mm above the 2440 plate, give or take where Revit puts the roof's base; read it against a section through the wall - and the sentence that the top is a sketch that does NOT follow the roof~~

---

### Row OG4

*Moved from the register on 2026-11-17.*

**Run.** ~~*The refusals.* On `of-gable`: the positive kept once through `try-once` and the same call again; then `redraw=true`; the south wall attached by hand (Attach Top/Base to the roof) and handed in~~ **Struck 2026-11-17:** CLOSED, not passed - superseded 2026-11-17 by [Group OE](../needs-checking/group-oe.md) on main, where the card was proved and signed 2026-11-16.

**Look for.** ~~The second call shaped 0, *"its profile is already edited ... Ask again with redraw"*; with `redraw` shaped 1 and the door still in it; the attached wall *"its top is already attached in Revit"*~~

---

### Row OG5

*Moved from the register on 2026-11-17.*

**Run.** ~~*Release 2021, refused in words.* A chat connected to Revit 2021 (or 2020) asks *"attach all walls to the roof"*; then `python mcp/client/heron_bridge_client.py validate shape-wall-tops-to-roof ...` with that Revit connected~~ **Struck 2026-11-17:** CLOSED, not passed - superseded 2026-11-17 by [Group OE](../needs-checking/group-oe.md) on main, where the card was proved and signed 2026-11-16.

**Look for.** ~~The lookup's answer opens *"NOT ON REVIT 2021"* and says Heron cannot do this on Revit 2021 and it is done by hand; the client refuses *"shape-wall-tops-to-roof does not claim Revit 2021 ... No fragment was sent to Revit"*~~

---

### Row OG6

*Moved from the register on 2026-11-17.*

**Run.** ~~*Kept once, and the second route.* Through `try-once` on the modeller's yes, the west wall~~ **Struck 2026-11-17:** CLOSED, not passed - superseded 2026-11-17 by [Group OE](../needs-checking/group-oe.md) on main, where the card was proved and signed 2026-11-16.

**Look for.** ~~ONE entry in Revit's undo list; a section through the west wall: the top runs up under the roof, the door where it was; Edit Profile shows the outline drawn~~

---
