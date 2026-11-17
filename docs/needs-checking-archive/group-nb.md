# NEEDS-CHECKING archive — Group NB

> **Checks that were done, moved out of the live register.** These are rows of Group NB of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a nested DETAIL ITEM locked to named planes in the plan it is drawn in: `LOCK_NESTED_FAMILY_TO_PLANES` version 3, now version 4 (2026-11-11)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row NB2

*Moved from the register on 2026-11-17.*

**Run.** *A plane it does not lie on.* Rolled back: `instance=<id>`, `locks=Center (Front/Back)=Arrow Back`

**Look for.** Revit's own refusal named with "move the nested family onto the plane first"; the call rolled back, nothing locked **PASSED 2026-11-11 on version 4, Revit 2020 (session 44624) - the run recorded below; the card signed PROVEN that day.**

---

### Row NB3

*Moved from the register on 2026-11-17.*

**Run.** *FOLLOW, kept.* NB1's positive and a second lock on the arrow's own plane at its tail (`Left`, if the arrow family gives its tail plane Is Reference Left - else run `SET_FAMILY_PLANE_REFERENCE` in it first) to "Arrow Front", through `try-once` (or `revit_change` once PROVEN) and KEEP; `FLEX_FAMILY` "Arrow Span" at 600 and 1,200

**Look for.** The arrow's insertion point read back 300 and 600 mm from Center (Front/Back) - it followed "Arrow Front"; selected, two closed padlocks **PASSED 2026-11-11 on version 4, Revit 2020 (session 44624) - the run recorded below; the card signed PROVEN that day.**

---
