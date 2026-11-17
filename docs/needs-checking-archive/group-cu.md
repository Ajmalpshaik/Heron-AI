# NEEDS-CHECKING archive — Group CU

> **Checks that were done, moved out of the live register.** These are rows of Group CU of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a parametric coil spring: swept blend labels, a nested family arrayed up, its last copy locked (2026-10-07)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CU1

*Moved from the register on 2026-11-17.*

**Run.** `validate --in SpringProof --negative-in SpringProof label-family-radius --write --set form=<half-turn> --set "parameterName=Mean Radius" --set diameter=false`, negative the same with `parameterName=Odd Size`

**Look for.** `labelled` the path arc's radius reading 40, its centre locked to Center (Front/Back) and Center (Left/Right); the negative refused - *"Nothing in the swept blend is 22 mm in size"*, its sizes listed. **RAN 2026-10-07, both legs ok, rolled back; SIGNED by Ajmal PS - PROVEN** - **Struck 2026-10-18:** run and signed, as it says - `label-family-radius` PROVEN, fingerprint `90c6543736410662` in its `fragment.yaml`.

---

### Row CU2

*Moved from the register on 2026-11-17.*

**Run.** the second route for CU1: `validate ... flex-family --write --setup label-family-radius` with `trials=Mean Radius=60 \| Mean Radius=25`, negative `trials=Odd Size=30`

**Look for.** the FORM moving, not only the label reading (the lesson of PR #402): **RAN 2026-10-07** - 130 x 65 x 25 mm at 60, 60 x 28.53 x 25 at 25, the negative unchanged at 90 x 45 x 25. Written into the CU1 draft's second route - **Struck 2026-10-18:** run 2026-10-07 and written into CU1's second route, signed with it.

---

### Row CU3

*Moved from the register on 2026-11-17.*

**Run.** CU1 with `Wire Radius` (5), then `Half Pitch` (15)

**Look for.** two radial labels, one per profile; then a plane `Half Pitch` made through the top circle's centre, the centre locked, labelled from Ref. Level reading 15. Measured kept on SpringProbe 2026-10-07 and flexed 136 x 68 x 41 and 56 x 26.63 x 12 mm - **Struck 2026-10-18:** measured and flexed 2026-10-07, as recorded in this row.

---

### Row CU4

*Moved from the register on 2026-11-17.*

**Run.** `validate --in SpringProof --negative-in SpringProof array-family-forms --write --set form=<second nested family> --set count=3 --set "step=Z 60 last"`, negative `form=<the array>`

**Look for.** three nested families up +Z, every copy read back, `members` two group ids in order; the negative refused - neither a form nor a nested family. **RAN 2026-10-07, both legs ok; SIGNED by Ajmal PS - PROVEN** - **Struck 2026-10-18:** run and signed 2026-10-07, fingerprint `d55056abd9b5cf1e`. `array-family-forms` is DRAFT again at a later version; that re-proof is not this row's.

---

### Row CU5

*Moved from the register on 2026-11-17.*

**Run.** `validate --in SpringProof --negative-in SpringProof lock-nested-family-to-planes --write --set instance=<the array's last copy> --set "locks=Top=Top Turn Start"`, negative `instance=<the array>`

**Look for.** the nested family inside the group found and its Top locked, in an elevation, its extent unchanged; the negative refused - not a nested family. **RAN 2026-10-07, both legs ok; SIGNED by Ajmal PS - PROVEN** - **Struck 2026-10-18:** run and signed, as it says - `lock-nested-family-to-planes` PROVEN, fingerprint `44a4578e0c006726` in its `fragment.yaml`.

---
