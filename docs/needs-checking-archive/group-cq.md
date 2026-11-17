# NEEDS-CHECKING archive — Group CQ

> **Checks that were done, moved out of the live register.** These are rows of Group CQ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *deleting a family parameter that labels a dimension: the label taken off first, when asked (2026-10-06)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CQ1

*Moved from the register on 2026-11-17.*

**Run.** `validate --in "FamilyN" --negative-in "FamilyN" --write --set "parameterNames=Heron Unl A" --set unlabelFirst=true --negative-set "parameterNames=Heron Unl B" --negative-set unlabelFirst=true delete-family-parameters` - or the job file [`delete-family-parameters-unlabel-2026-10-06.yaml`](../../tools/jobs/delete-family-parameters-unlabel-2026-10-06.yaml)

**Look for.** `deleted` holds `Heron Unl A (type)`; `unlabelled` holds ONE line - `"Unl Left" to "Unl Right"`, its view, reading 300 mm - and the group ROLLED BACK. **RAN 2026-10-06, ROLLED BACK:** `deleted` 1 - Heron Unl A (type); `unlabelled` 1 - the dimension from "Unl Left" to "Unl Right" in "Ref. Level" (the client truncates the rest of the line); findings *Took the label off 1 dimension(s) first, as asked*. Draft fingerprint `afa22976c7d862bd` - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CQ6 - `delete-family-parameters` PROVEN, fingerprint `afa22976c7d862bd` in its `fragment.yaml`.

---

### Row CQ2

*Moved from the register on 2026-11-17.*

**Run.** the negative half of CQ1: `Heron Unl B` - a label AND a formula

**Look for.** refused quoting `"Heron Unl C" = Heron Unl B / 3`; `unlabelled` EMPTY and `deleted` empty - no label comes off in a refused call. **RAN 2026-10-06, ROLLED BACK:** refused - *"Heron Unl C" = Heron Unl B / 3 - change or clear that formula first*; `unlabelled` 0, `deleted` 0 - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CQ6 - `delete-family-parameters` PROVEN, fingerprint `afa22976c7d862bd` in its `fragment.yaml`.

---

### Row CQ3

*Moved from the register on 2026-11-17.*

**Run.** version 1's call, unchanged: `validate --in "FamilyN" --set "parameterNames=Heron Unl A" delete-family-parameters` with NO `--write` and no `unlabelFirst` (a refusal writes nothing)

**Look for.** refused in version 1's exact words - *it labels 1 dimension(s) - take the label off first (select the dimension, Label: <None>)* - `unlabelled` empty. **RAN 2026-10-06** (as `fragment --in HeronUnlabelProof`, no `--write`): refused in version 1's exact words; `unlabelled` 0 - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CQ6 - `delete-family-parameters` PROVEN, fingerprint `afa22976c7d862bd` in its `fragment.yaml`.

---

### Row CQ4

*Moved from the register on 2026-11-17.*

**Run.** `--write --set "parameterNames=Heron Unl A, Heron Unl B" --set unlabelFirst=true`

**Look for.** refused as a whole: neither deleted, neither dimension unlabelled - all or nothing. **RAN 2026-10-06, ROLLED BACK:** refused, 1 of 2 could not go (Heron Unl B, the formula); `deleted` 0, `unlabelled` 0 - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CQ6 - `delete-family-parameters` PROVEN, fingerprint `afa22976c7d862bd` in its `fragment.yaml`.

---

### Row CQ5

*Moved from the register on 2026-11-17.*

**Run.** FamilyN after CQ1 to CQ4

**Look for.** Both dimensions still labelled (`Heron Unl A`, `Heron Unl B`) in the plan, all three parameters still there - every run rolled back. Look: rollback has failed on this PC before (BD1). **CHECKED 2026-10-06** with version 1's call after CQ1 to CQ4: both names still refused because each *labels 1 dimension(s)*, B also for the formula - both labels and all three parameters still there - **Struck 2026-10-18:** run as looked for, 2026-10-06; signed with CQ6 - `delete-family-parameters` PROVEN, fingerprint `afa22976c7d862bd` in its `fragment.yaml`.

---
