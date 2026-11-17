# NEEDS-CHECKING archive — Group CO

> **Checks that were done, moved out of the live register.** These are rows of Group CO of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *family parameters renamed in place, formulas and labels read back with the new names (2026-10-06)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CO1

*Moved from the register on 2026-11-17.*

**Check.** On the scratch family - CC 1 to 2 and CC 2 to 3 (type lengths), Total = CC 1 to 2 + CC 2 to 3, a dimension labelled CC 1 to 2 - renameExisting true, "CC 1 to 2=Pipe CC 1 to 2; CC 2 to 3=Pipe CC 2 to 3", `validate --write`, rolled back

**Expected.** **RAN 2026-10-07:** renamed `CC 1 to 2 -> Pipe CC 1 to 2, CC 2 to 3 -> Pipe CC 2 to 3`; formulasReadBack `Total = Pipe CC 1 to 2 + Pipe CC 2 to 3`; keptLinks `Pipe CC 1 to 2: 1 labelled dimension(s)`. Rolled back - **Struck 2026-10-18:** run as recorded, 2026-10-07; signed with CO8 - `add-family-parameters` PROVEN, fingerprint `0dde2ad244a2a319` in its `fragment.yaml`.

---

### Row CO2

*Moved from the register on 2026-11-17.*

**Check.** The same family renamed, then FLEX_FAMILY setting Pipe CC 1 to 2 to a new length

**Expected.** **RAN 2026-10-07:** `validate --setup add-family-parameters flex-family`, rolled back: after the rename, Pipe CC 1 to 2 = 450 gave the box 450 x 200 x 200 mm, all held, family restored. Negative: trials on the OLD name refused - *"CC 1 to 2" is not a parameter of this family* - **Struck 2026-10-18:** run as recorded, 2026-10-07; signed with CO8 - `add-family-parameters` PROVEN, fingerprint `0dde2ad244a2a319` in its `fragment.yaml`.

---

### Row CO3

*Moved from the register on 2026-11-17.*

**Check.** A new name already in the family in other capitals, "CC 1 to 2=cc 2 to 3"

**Expected.** **RAN 2026-10-07** (the negative of CO1): *Nothing was renamed. 'cc 2 to 3' is already a parameter in this family (as 'CC 2 to 3')*; renamed 0 - **Struck 2026-10-18:** run as recorded, 2026-10-07; signed with CO8 - `add-family-parameters` PROVEN, fingerprint `0dde2ad244a2a319` in its `fragment.yaml`.

---

### Row CO8

*Moved from the register on 2026-11-17.*

**Check.** The run records in `brain/proof-drafts/runs/` read and signed

**Expected.** Drafted 2026-10-07 from `runs/add-family-parameters.json` (CO1 + CO3; gap: no second route run). `heron_validate.py accept add-family-parameters --by "Ajmal PS"` on his word. **SIGNED 2026-10-07 by Ajmal PS on his word - PROVEN** - **Struck 2026-10-18:** run as recorded, 2026-10-07; signed with CO8 - `add-family-parameters` PROVEN, fingerprint `0dde2ad244a2a319` in its `fragment.yaml`.

---
