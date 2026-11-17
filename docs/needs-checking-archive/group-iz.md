# NEEDS-CHECKING archive — Group IZ

> **Checks that were done, moved out of the live register.** These are rows of Group IZ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a hexagon whose size across the flats follows a parameter: `EXTRUDE_POLYGON_BETWEEN_PLANES` version 1 (2026-10-24)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IZ0

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST.* `parameterName=Zero Flats`, `sides=6`

**Look for.** Refused before anything is made - *"... holds 0 mm in type "Test" - no size yet ..."*; `size` empty, `acrossFlatsMm` 0, nothing in the family **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568*, which records it run and passed.

---

### Row IZ2

*Moved from the register on 2026-11-17.*

**Run.** *The flex.* IZ1 as setup, `trials=Head Across Flats=46 \| Head Across Flats=10`

**Look for.** The prism MEASURED 53.12 x 46 and 11.55 x 10 mm, centred at X 0, Y 0, and put back to 20.78 x 18 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568*, which records it run and passed.

---

### Row IZ3

*Moved from the register on 2026-11-17.*

**Run.** *Other even counts.* `sides=8`, then `sides=4`; the octagon flexed as IZ2

**Look for.** Octagon 18 across the flats and 19.48 across the corners, 9 locks, flexing to 46 x 46 and 10 x 10; square 18 x 18, 5 locks, no centre plane made **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568*, which records it run and passed.

---

### Row IZ4

*Moved from the register on 2026-11-17.*

**Run.** *An odd count.* `sides=5`

**Look for.** Refused - *"an EVEN number of sides, 4 to 12 ..."* - nothing made **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568*, which records it run and passed.

---

### Row IZ5

*Moved from the register on 2026-11-17.*

**Run.** The signature, on the owner's word only, after IZ0 to IZ4

**Look for.** `python brain/heron_validate.py accept extrude-polygon-between-planes --by "Ajmal PS"` **Struck 2026-11-13:** signed - `EXTRUDE_POLYGON_BETWEEN_PLANES` PROVEN at version 1, `proof:` dated 2026-10-24 by Ajmal PS, fingerprint `506b8485027c7bb2`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
