# NEEDS-CHECKING archive — Group JM

> **Checks that were done, moved out of the live register.** These are rows of Group JM of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a curtain system laid on the faces of a mass: `CREATE_CURTAIN_SYSTEM` version 1 (2026-10-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JM2

*Moved from the register on 2026-11-17.*

**Run.** *The positive, rolled back.* `python tools/batch-prove.py tools/jobs/create-curtain-system.yaml --session <pid>`, its positive leg: `host=Heron JM Tower`, `curtainSystemType=Heron JM 1500`, faces left out

**Look for.** `created` 1, `facesAsked` 4, `facesTaken` 4, `alreadyNear` 0, `refused` null; `readBack` *4 face(s) asked, 1800 m2 (its faces: 4 vertical, 1 top, 1 underside)* and four grids - two of 11 and 19 lines and 240 panels, two of 7 and 19 lines and 160 panels, 800 in all, mullions 0 on each. **Record** which of U and V held the upright lines **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---

### Row JM3

*Moved from the register on 2026-11-17.*

**Run.** *The top.* JM2 with `faces=top`

**Look for.** `facesAsked` 1, `facesTaken` 1; one grid of 11 and 7 lines, 96 panels **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JM4

*Moved from the register on 2026-11-17.*

**Run.** *Every face.* JM2 with `faces=all`

**Look for.** `facesAsked` 6, `facesTaken` 6 - the underside included, as the card says; 992 panels **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JM8

*Moved from the register on 2026-11-17.*

**Run.** *A second one on the same faces - only if JM1 was kept.* JM2 again, rolled back, on the tower that now carries JM1's curtain system

**Look for.** `alreadyNear` 1 and a finding naming JM1's id; **record** what Revit did - a second curtain system over the first, a warning, or a refusal **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JM11

*Moved from the register on 2026-11-17.*

**Run.** *Routing, on the PC's backend.* Ask `heron_lookup` the modeller's three sentences, then *"how many panels are on this curtain system"*, *"how much glass is in this facade"* and *"what is this facade made of"*

**Look for.** The three reach `CREATE_CURTAIN_SYSTEM`; the three questions reach `REPORT_CURTAIN_ELEMENTS` - a read - never this write (D-86). On this container's backend all six did **Struck 2026-11-13:** PASSED - the verdict is in this file's *More results - Revit 2020, 2026-11-01, later the same day*, which records it run and passed.

---

### Row JM12

*Moved from the register on 2026-11-17.*

**Run.** *The signature, on the owner's word only*, after JM1 (kept) or JM2, and JM9

**Look for.** `python brain/heron_validate.py accept create-curtain-system --by "Ajmal PS"`, and `heron-status` set to PROVEN in the same change **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2020, 2026-11-01*, which records it run and passed.

---
