# NEEDS-CHECKING archive — Group CL

> **Checks that were done, moved out of the live register.** These are rows of Group CL of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *family types renamed and deleted in the Family Editor (2026-10-06)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CL1

*Moved from the register on 2026-11-17.*

**Run.** `validate --write --set "typeName=Standard" --set "values=Type Name=Test - A" --negative-set "typeName=Standard" --negative-set "values=Type Name=t2" --in "Family14" --negative-in "Family14" set-family-type-values`

**Look for.** **RAN 2026-10-06:** `typeUsed Test - A`, `typesNow "T2", "Test - A"` - the type renamed, no `Standard` left; the group ROLLED BACK - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL2

*Moved from the register on 2026-11-17.*

**Run.** the negative half of CL1: `Type Name=t2` - a name `T2` already has, in another case

**Look for.** **RAN 2026-10-06:** refused - *The family already has a type "T2", and Revit does not tell type names apart by case* - `typesNow "T2", "Standard"`, nothing renamed - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL3

*Moved from the register on 2026-11-17.*

**Run.** `typeName=T2`, `values=` (empty), `deleteType=true`

**Look for.** **RAN 2026-10-06, rolled back:** `typesNow "Standard"`, `typeUsed Standard` - T2 gone and Standard current - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL4

*Moved from the register on 2026-11-17.*

**Run.** `values=Type Name=Nut [M12]`

**Look for.** **RAN 2026-10-06, rolled back:** refused naming `[ ]`, nothing renamed - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL5

*Moved from the register on 2026-11-17.*

**Run.** `deleteType=true` with `values=URL=x`

**Look for.** **RAN 2026-10-06, rolled back:** refused - a delete is done alone, nothing changed - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL6

*Moved from the register on 2026-11-17.*

**Run.** a rename of a type that does not exist

**Look for.** **RAN 2026-10-06** (no-transaction run, before the arrangement): refused - *a rename never makes one - the family's types are none* - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL7

*Moved from the register on 2026-11-17.*

**Run.** Family14 after CL1 to CL5

**Look for.** **RAN 2026-10-06:** `typesNow "Standard", "T2"` - every run rolled back - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CL8 - `set-family-type-values` PROVEN, fingerprint `1281aa005776f57a` in its `fragment.yaml`.

---

### Row CL8

*Moved from the register on 2026-11-17.*

**Run.** the signature

**Look for.** **DONE 2026-10-06:** `heron_validate.py accept set-family-type-values --by "Ajmal PS"` on the CL1/CL2 record, on the owner's word - PROVEN **Struck 2026-11-13:** passed, as the result written in this row records.

---
