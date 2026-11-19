# NEEDS-CHECKING archive — Group JU

> **Checks that were done, moved out of the live register.** These are rows of Group JU of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the model's 3D geometry as triangles for the Companion's 3D model view: `REPORT_MODEL_GEOMETRY` (2026-11-01)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JU1

*Moved from the register on 2026-11-17.*

**Run.** *The D-30 positive.* The job file's leg - `wholeModel=true`, `maxTriangles=20000` - with `Heron empty box` active

**Look for.** `triangles` above 0 and at most 20000; `scope` `"whole model"`, no `active view:` line; `leftOut` above 0 and one `capped` line, *"triangles: the budget of 20000 was reached; N element(s) left out - the largest were drawn first"*; `len(elements) + noGeometry + leftOut = elementsRead`; every `c` indexes `categories`, every `t` index is below its `v` length / 3; `stopped` `"triangles"` (should the 40-second clock come first, `"time"` with its own `capped` line - record which) **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2024, 2026-11-01*, which records it run and passed.

---
