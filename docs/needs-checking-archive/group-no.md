# NEEDS-CHECKING archive — Group NO

> **Checks that were done, moved out of the live register.** These are rows of Group NO of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a family connector's shape changed, and a connector moved onto a reference plane: `SET_FAMILY_CONNECTOR_ROLES` version 5 (2026-11-11)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row NO1

*Moved from the register on 2026-11-17.*

**Run.** *Shape and its size in one call* - connectors "Box Top", settings "shape=Round; sizeParameters=Neck Diameter"; negative the same connector with "sizeParameters=Neck Diameter" alone; both rolled back

**Look for.** Positive: changed "shape rectangular -> round", "diameter <- Neck Diameter"; read back round 250 mm. **If Revit refuses the Shape parameter, or Heron refuses because its number is not the connector's shape, that is the first thing this group learns** - record the sentence and the number, and the card is changed, not proved. Negative: refused - rectangular, TWO length parameters - with the hint to give "shape=Round"; nothing changed **Struck 2026-11-13:** passed - this row's inputs and answers are in `SET_FAMILY_CONNECTOR_ROLES`'s `proof:` block, dated 2026-11-12 on Heron Connector Shape Test (1,533 elements), Revit 2020, session 45552, fingerprint `334167c5b046a931`.

---

### Row NO9

*Moved from the register on 2026-11-17.*

**Run.** *The proof* - `heron validate` on NO1 (positive) and NO1's negative, Revit 2020; then the signing

**Look for.** A proof block naming the model, both cases and a fingerprint; the card PROVEN again on Ajmal PS's word **Struck 2026-11-13:** signed - `SET_FAMILY_CONNECTOR_ROLES` PROVEN at version 5, `proof:` dated 2026-11-12 by Ajmal PS, fingerprint `334167c5b046a931`, in its `fragment.yaml`. That signature is what this row asked for; it does not stand for any other row of this group.

---
