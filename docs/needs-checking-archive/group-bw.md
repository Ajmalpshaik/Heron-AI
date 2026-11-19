# NEEDS-CHECKING archive — Group BW

> **Checks that were done, moved out of the live register.** These are rows of Group BW of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *`place-room-at-point` measures a space after Revit regenerates, so an enclosed space is no longer called not enclosed (2026-10-03)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row BW1

*Moved from the register on 2026-11-17.*

**Check.** PLACE_ROOM_AT_POINT `asSpace=false` with points inside closed areas, and one point outside every enclosure

**Expected.** Every room is in `created`; only the outside point's room is in `unenclosed`. **Negative:** the outside room reads *Not Enclosed* in Properties and REPORT_ROOM_SPACE_DATA names it unenclosed **CLOSED 2026-11-13, not passed** - `PLACE_ROOM_AT_POINT` was retired into `PLACE_ROOMS` on 2026-11-01 (its `deprecated:` block); `PLACE_ROOMS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row BW2

*Moved from the register on 2026-11-17.*

**Check.** The same points with `asSpace=true`

**Expected.** Every space is in `created`; only the outside point's space is in `unenclosed` - **not** every space, which is the defect. REPORT_ROOM_SPACE_DATA agrees space by space. **Negative:** the outside space is named, so the fragment is not simply reporting none **CLOSED 2026-11-13, not passed** - `PLACE_ROOM_AT_POINT` was retired into `PLACE_ROOMS` on 2026-11-01 (its `deprecated:` block); `PLACE_ROOMS` is PROVEN at version 3, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---
