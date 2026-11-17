# NEEDS-CHECKING archive — Group JG

> **Checks that were done, moved out of the live register.** These are rows of Group JG of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a wall that starts above its level: `CREATE_WALL` version 3, `baseOffset` (2026-10-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JG0

*Moved from the register on 2026-11-17.*

**Run.** *Open.* The scratch project open and in front in Revit 2020, saved with nothing unsaved

**Look for.** The title bar reads *JG Wall Offset* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JG1

*Moved from the register on 2026-11-17.*

**Run.** *The offset left out, and 0 - version 2 unchanged, rolled back. Half of the D-30 negative.* `python mcp/client/heron_bridge_client.py fragment --in "JG Wall Offset" create-wall --write --set "level=Level 1" --set "wallType=Generic - 200mm" --set "points=0,80000,0; 6000,80000,0" --set height=3000 --set structural=false`, then the same with `--set baseOffset=0` added

**Look for.** Both: `created 1`, `baseOffset 0`, `offsetsReadMm` holding one 0 - the read back off the wall, not the input - and findings exactly *"1 wall(s) built on 'Level 1' at 3000 mm high, unconnected to the level above. 0 refused, 0 skipped as too short. NOTHING has been joined..."* and nothing more: **no "Base Offset" line**. Rolled back. **FAIL:** a Base Offset line, an `offsetsReadMm` other than 0, or any word unlike HM1's **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JG2

*Moved from the register on 2026-11-17.*

**Run.** *The positive - the D-30 positive - KEPT.* Save (Ctrl+S). `python mcp/client/heron_bridge_client.py try-once --in "JG Wall Offset" create-wall --set "level=Level 1" --set "wallType=Generic - 200mm" --set "points=0,80000,0; 6000,80000,0" --set height=3000 --set structural=false --set baseOffset=1500` - Revit shows the wall and asks Keep or Discard; press **Keep**

**Look for.** `KEPT`; findings *"1 wall(s) built..."* then *"Base Offset +1500 mm from 'Level 1', so each wall stands from +1500 to +4500 mm, its 3000 mm height measured from its base. Read back off the walls: 1 of 1 as asked"*. Select the wall: Properties reads **Base Constraint Level 1, Base Offset 1500, Unconnected Height 3000** - the second route, by hand. ONE undo entry. **FAIL:** Base Offset 0 with the wall drawn up there anyway, 3000 (the offset counted twice), or an Unconnected Height other than 3000 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JG4

*Moved from the register on 2026-11-17.*

**Run.** *The negative - the D-30 negative - rolled back.* `python mcp/client/heron_bridge_client.py validate --in "JG Wall Offset" --write --kept --negative-set "level=Level 1" --negative-set "wallType=Generic - 200mm" --negative-set "points=0,90000,0; 6000,90000,0" --negative-set height=3000 --negative-set structural=false --negative-set baseOffset=200000000 create-wall` (the value options go BEFORE the fragment's name - HM4 found that)

**Look for.** Only the negative runs, rolled back: `created` empty, `refused 1`, findings *"A base offset is millimetres above the level - 1500, or -300 below it - and Revit takes none past 9144000 (30000 feet) either way. 200000000 is not one Heron can start a wall at. Nothing was built."* Nothing new in the model. **FAIL:** a wall built, or Revit's own words instead of these **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JG5

*Moved from the register on 2026-11-17.*

**Run.** *Signing.* `python brain/heron_validate.py draft create-wall --from brain/proof-drafts/runs/create-wall.json`, read the draft against JG2 to JG4, then `python brain/heron_validate.py accept create-wall --by "Ajmal PS"`

**Look for.** The proof block written - the positive the kept run (D-120), the negative JG4's. **`heron-status: PROVEN` is a separate edit, the owner's**; until it is made, D-117 still refuses CREATE_WALL's kept changes **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---
