# NEEDS-CHECKING archive — Group JA

> **Checks that were done, moved out of the live register.** These are rows of Group JA of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a duct port on a flange ring, sized by its opening: `ADD_FAMILY_CONNECTOR` version 5 (2026-10-24)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JA0

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST.* `sizeParameters=Ring Width, Ring Height` (640, 440 - the ring's outside)

**Look for.** Refused - *"The opening of the ring face on "Casing Right" is 600 x 400 mm ..."* - `sizedBy` empty, nothing kept **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 94928*, which records it run and passed.

---

### Row JA1

*Moved from the register on 2026-11-17.*

**Run.** *The port.* `sizeParameters=Width, Height`

**Look for.** `connector` *"duct connector, Supply Air, rectangular 600 x 400 mm, at X 150, Y 0, Z 0, pointing right (+X) ... centred in the ring face's opening and sized by it"* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 94928*, which records it run and passed.

---

### Row JA2

*Moved from the register on 2026-11-17.*

**Run.** *The flex.* JA1 kept on the owner's word, then *Length* 400, *Width* 1000, *Height* 250 through `SET_FAMILY_TYPE_VALUES` and the connector read

**Look for.** At X 200, Y 0, Z 0, 1000 x 250 mm; put back to 300 / 600 / 400 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 94928*, which records it run and passed.

---

### Row JA3

*Moved from the register on 2026-11-17.*

**Run.** *A face of one loop is unchanged.* `facePlane` naming a plain box's face, as version 4's proof

**Look for.** The same answer as version 4, no word of an opening **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 94928*, which records it run and passed.

---

### Row JA4

*Moved from the register on 2026-11-17.*

**Run.** The signature, on the owner's word only, after JA0 to JA3

**Look for.** `python brain/heron_validate.py accept add-family-connector --by "Ajmal PS"` **Struck 2026-11-13:** signed - `ADD_FAMILY_CONNECTOR` PROVEN at version 5, `proof:` dated 2026-10-24 by Ajmal PS, fingerprint `43d83188366b7b85`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
