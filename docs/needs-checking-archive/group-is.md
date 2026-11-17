# NEEDS-CHECKING archive — Group IS

> **Checks that were done, moved out of the live register.** These are rows of Group IS of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a revolve's radius labelled on Revit 2020, both references checked before Revit is asked: `LABEL_FAMILY_RADIUS` version 5 (2026-10-22)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IS0

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST.* `validate --in "Heron IS Radius" label-family-radius --write --set form=<C> --set "parameterName=Bell Radius" --set diameter=false`

**Look for.** Refused - *"No reference plane runs through the revolve's axis"*, naming `CREATE_REFERENCE_PLANES`; `labelled` empty, `valueMm` 0; nothing locked, Undo unchanged **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - the rows above, in their own scratch family*, which records it run and passed.

---

### Row IS3

*Moved from the register on 2026-11-17.*

**Run.** *The plan case.* `--set form=<D> --set "parameterName=Outlet Disc Radius" --set diameter=false`

**Look for.** As IS1, from *Center (Front/Back)* in the Ref. Level plan, 41 mm out. Either outcome copied whole **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - the rows above, in their own scratch family*, which records it run and passed.

---

### Row IS7

*Moved from the register on 2026-11-17.*

**Run.** The signature, on the owner's word only, after IS1, IS3 and IS4 have run with IS0's negative and IS5 agrees

**Look for.** `python brain/heron_validate.py accept label-family-radius --by "Ajmal PS"` - PROVEN again, at version 5 **Struck 2026-11-13:** signed - `LABEL_FAMILY_RADIUS` PROVEN at version 6, `proof:` dated 2026-11-12 by Ajmal PS, fingerprint `8555de577f0c8128`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
