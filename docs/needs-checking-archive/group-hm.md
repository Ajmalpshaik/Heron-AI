# NEEDS-CHECKING archive — Group HM

> **Checks that were done, moved out of the live register.** These are rows of Group HM of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a wall and a slab edge along an arc: CREATE_WALL and CREATE_FLOOR version 2 (2026-10-19)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row HM1

*Moved from the register on 2026-11-17.*

**Run.** *Version 1 unchanged, rolled back.* `python mcp/client/heron_bridge_client.py fragment --in "test projject" create-wall --write --set "level=Level 1" --set "wallType=Generic - 200mm" --set "points=0,0,0; 6000,0,0; 6000,4000,0" --set height=3000 --set structural=false < /dev/null`, then the same with `points=0,60000,0; 0,60000,0`; then `fragment --in "test projject" create-floor --write --set "level=Level 1" --set "floorType=Generic 300mm" --set "boundary=0,0,0; 6000,0,0; 6000,4000,0; 0,4000,0"`, and again with `boundary=0,0,0; 6000,0,0`

**Look for.** Word for word what version 1's proofs recorded: `created` 2 and *"2 wall(s) built on 'Level 1' at 3000 mm high, unconnected to the level above. 0 refused, 0 skipped as too short..."*; then `created` 0, `tooShort` 1. The floor: an id and *"Floor created on 'Level 1' using the Revit 2022 and later call..."*; then *"A floor needs at least three boundary points and 2 were given"*. No *"arc"* line in any of them. **FAIL:** any other word, or a point read in other units **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-19, Revit 2020 (laptop, session 15072) - PASS, signed, both PROVEN*, which records it run and passed.

---

### Row HM2

*Moved from the register on 2026-11-17.*

**Run.** *An arc curtain wall, KEPT and measured.* Save the copy (Ctrl+S). `python mcp/client/heron_bridge_client.py try-once --in "test projject" create-wall --set "level=Level 1" --set "wallType=Curtain Wall: Storefront" --set "points=0,60000,0; arc 5000,65000 10000,60000" --set height=3000 --set structural=false < /dev/null` - Revit shows the wall and asks Keep or Discard; press **Keep**

**Look for.** The client prints `KEPT`; findings *"1 wall(s) built..."* and *"1 of them follow an arc"*. In Revit, select the wall: **Length in Properties 15708 mm** (pi x 5000, to the millimetre) - **not 10000**, the chord. In plan, a half circle bulging north to y = 65000. ONE undo entry. **Record, not judged:** how Storefront's panels and mullions sit on the arc - flat between grid lines, or curved - in a 3D view **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-19, Revit 2020 (laptop, session 15072) - PASS, signed, both PROVEN*, which records it run and passed.

---

### Row HM5

*Moved from the register on 2026-11-17.*

**Run.** *Signing.* For each: `python brain/heron_validate.py draft <fragment> --from brain/proof-drafts/runs/<fragment>.json`, read the draft against HM2 to HM4, then `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"`

**Look for.** Both proof blocks written - the positive the kept run, the negative HM4's. **`heron-status: PROVEN` is a separate edit, the owner's**; until it is made, D-117 still refuses both **Struck 2026-11-13:** signed - `CREATE_WALL` PROVEN at version 3, `proof:` dated 2026-11-01 by Ajmal PS, fingerprint `cb379fa16bc2aee6`; `CREATE_FLOOR` PROVEN at version 3, `proof:` dated 2026-11-02 by Ajmal PS, fingerprint `db997d34c6317f6a`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
