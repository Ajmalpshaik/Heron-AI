# NEEDS-CHECKING archive — Group CW

> **Checks that were done, moved out of the live register.** These are rows of Group CW of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a family's forms picked by id, kind or material, to select or delete (2026-10-08)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CW1

*Moved from the register on 2026-11-17.*

**Run.** `validate --in "FormSelectProof" select-family-forms --set "forms=<b64>, <b70>" --negative-set "forms=<b64>, da0cea1e-82fa-11d3-a7db-00105aa73639-00000030"`

**Look for.** positive: `elements` 2, *solid extrusion ...b64 \|\| solid revolve ...b70*, `scanned` 3. negative: refused, *"...00000030" is "Center (Left/Right)" (Reference Planes), not a form*, `elements` 0 - the good id NOT carried. **RAN 2026-10-08:** exactly that; record `brain/proof-drafts/runs/select-family-forms.json`, draft fingerprint `7a661d39878bef87`, UNSIGNED **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CW2

*Moved from the register on 2026-11-17.*

**Run.** `validate --in "FormSelectProof" select-by-material --set materialName=Glass --set includePaint=false --set "categories=Generic Models"` and `--negative-set materialName=Default Wall` with the same two

**Look for.** positive: `elements` 1 (the extrusion), *1 of 3 scanned*, and the family line - all 3 forms scanned, 1 form <By Category> (the revolve; the void is not counted). negative: 0 of the same 3. **RAN 2026-10-08:** exactly that; record `brain/proof-drafts/runs/select-by-material.json`, UNSIGNED. **The same call on main's code, the same family: *0 of 0 scanned*** - the owner's report, reproduced **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CW3

*Moved from the register on 2026-11-17.*

**Run.** `fragment --in "FormSelectProof" select-family-forms --set forms=void`; then `forms=revolve, void`

**Look for.** `elements` 1, the void alone; then 0 with nothing refused - 3 looked at, none a void revolve. **RAN 2026-10-08:** both **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CW4

*Moved from the register on 2026-11-17.*

**Run.** `forms=cylinder`; then `forms=void, <b6b>`

**Look for.** refused - *neither an id in this family nor a kind or side*; then refused - *ids OR kind and side, not both*. **RAN 2026-10-08:** both **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CW5

*Moved from the register on 2026-11-17.*

**Run.** `fragment --in "Project2" select-family-forms --set forms=all`

**Look for.** `notAFamily` true, refused, `elements` 0. **RAN 2026-10-08** **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CW7

*Moved from the register on 2026-11-17.*

**Run.** `forms=<b6b>`, then `fragment --in "FormSelectProof" delete-elements --write --expect-from "select-family-forms where forms=<b6b>"` (no `--apply`)

**Look for.** ROLLED BACK; `askedFor` 1, `deleted` 5, `alsoWent` 4 - the void and its sketch. **RAN 2026-10-08**, and `forms=all` afterwards still read all three forms **Struck 2026-11-13:** passed, as the result written in this row records.

---
