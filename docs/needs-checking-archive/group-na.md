# NEEDS-CHECKING archive — Group NA

> **Checks that were done, moved out of the live register.** These are rows of Group NA of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a form's SKETCH locked to named planes - a sweep's path lines, a circle's centre: `LOCK_FORM_SKETCH_TO_PLANES` (2026-11-10)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row NA1

*Moved from the register on 2026-11-17.*

**Run.** *The D-30 negative FIRST, then the positive - the sweep.* The job file's first job, rolled back

**Look for.** Negative ("Ring Left, Spigot Y" - all or nothing): refused, the nearest path lines named 200 mm off "Spigot Y", and NO lock on "Ring Left" either. Positive: four locks, one per path line, in Ref. Level - **if Revit refuses a straight sketch line's reference, that is the first thing this group learns** and the card is changed, not proved **PASSED 2026-11-10, Revit 2020 (session 9912), rolled back - the run recorded below; the card signed PROVEN that day.**

---

### Row NA2

*Moved from the register on 2026-11-17.*

**Run.** *The circle - negative first, then positive.* The job file's second job, rolled back

**Look for.** Negative: the centre named 450 mm off "Ring Left". Positive: two locks on ONE half-arc's centre; a line saying "Center Mark Visible" was switched on, or not **PASSED 2026-11-10, Revit 2020 (session 9912), rolled back - the run recorded below; the card signed PROVEN that day.**

---

### Row NA3

*Moved from the register on 2026-11-17.*

**Run.** *A plane that cannot be locked.* The job file's third job: the sweep with planes "Ref. Level"

**Look for.** Refused as not standing square to the path, saying a sweep's ends follow its path - and NOT naming `LOCK_FORM_TO_PLANES` **PASSED 2026-11-10, Revit 2020 (session 9912), rolled back - the run recorded below; the card signed PROVEN that day.**

---

### Row NA4

*Moved from the register on 2026-11-17.*

**Run.** *FOLLOW - the sweep, kept.* Run NA1's positive through `try-once` (or `revit_change` once PROVEN) and KEEP; label "Ring Left"-"Ring Right" and "Ring Front"-"Ring Back" with a length "Ring Size" (600, EQ about the centre planes); `FLEX_FAMILY` "Ring Size" at 450 and 900

**Look for.** The sweep rebuilt at each size, mitred corners, read back 450 and 900 across; **no "Can't create sweep"** - the failure of 2026-11-10 gone. LOOK at Edit Sweep > Edit Path: four closed padlocks **PASSED 2026-11-10, Revit 2020 (session 9912) - the second run recorded below; the card signed PROVEN that day.**

---

### Row NA5

*Moved from the register on 2026-11-17.*

**Run.** *FOLLOW - the circle, kept.* NA2's positive kept; label "Spigot X" from "Center (Left/Right)" and "Spigot Y" from "Center (Front/Back)" (150 / 100); `FLEX_FAMILY` them to 250 / 50

**Look for.** The circle's centre read back at (250, 50), its 150 mm diameter unchanged - both halves moved together **PASSED 2026-11-10, Revit 2020 (session 9912) - the second run recorded below; the card signed PROVEN that day.**

---
