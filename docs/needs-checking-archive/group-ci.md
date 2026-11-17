# NEEDS-CHECKING archive — Group CI

> **Checks that were done, moved out of the live register.** These are rows of Group CI of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *family type values: a material by its name and a URL as text (2026-10-06)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CI1

*Moved from the register on 2026-11-17.*

**Run.** `validate --in "Family3" --negative-in "Family3" --write --setup add-family-parameters set-family-type-values` with `values=Nut Material=glass; URL=https://ajmalps.com`

**Look for.** **RAN 2026-10-06:** type `T1` made, `Nut Material = "Glass"` (found in another case - the one material that matches) and `URL = "https://ajmalps.com"`, both read back from the type; the group ROLLED BACK - **Struck 2026-10-18:** run as looked for, and signed with CI4. Version 3 ([Group CL](../needs-checking/group-cl.md)) has been signed over it since.

---

### Row CI2

*Moved from the register on 2026-11-17.*

**Run.** the same, `values=Nut Material=Unobtainium; URL=https://ajmalps.com`

**Look for.** **RAN 2026-10-06:** refused - *no material "Unobtainium" in this family ... the closest are "Default", "Glass", "Poche"* - `written 0`, `typeCreated false`: the URL beside it was not written either, and no material was made - **Struck 2026-10-18:** run as looked for, and signed with CI4. Version 3 ([Group CL](../needs-checking/group-cl.md)) has been signed over it since.

---

### Row CI3

*Moved from the register on 2026-11-17.*

**Run.** `values=Nut Material=<By Category>`

**Look for.** **RAN 2026-10-06:** `Nut Material = <By Category>` read back - the parameter cleared - **Struck 2026-10-18:** run as looked for, 2026-10-06.

---

### Row CI4

*Moved from the register on 2026-11-17.*

**Run.** the signature

**Look for.** **DONE 2026-10-06:** `heron_validate.py accept set-family-type-values --by "Ajmal PS"` on the CI1/CI2 record, on the owner's word - PROVEN - **Struck 2026-10-18:** done, as it says. Version 3 ([Group CL](../needs-checking/group-cl.md)) has been signed over it since.

---
