# NEEDS-CHECKING archive — Group DE

> **Checks that were done, moved out of the live register.** These are rows of Group DE of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a family parameter replaced in place by the office's shared parameter (2026-10-12)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row DE1

*Moved from the register on 2026-11-17.*

**Run.** Arrange on **Revit 2020**: Manage > Shared Parameters, Create, a scratch file with a group "Heron Test" holding **Heron Unit Width** and **Heron Unit Depth** (Length) and **Heron Test Tag** (Text) - write down the file that was set before. File, New, Family, Metric Generic Model.rft, saved as **Heron Replace Shared Test**. `ADD_FAMILY_PARAMETERS` "Width, Half Width, Depth", Length, type, Dimensions; `SET_FAMILY_TYPE_VALUES` Width 600, Half Width 300, and a second type with Width 900; a dimension between the template's Left and Right planes labelled Width (`LABEL_FAMILY_DIMENSION`); `SET_FAMILY_FORMULA` "Half Width = Width / 2"

**Look for.** two types; Width labelling one dimension; Half Width driven by the formula - **Struck 2026-10-18:** arranged and read back 2026-10-12 - *Run 2026-10-12* below.

---

### Row DE2

*Moved from the register on 2026-11-17.*

**Run.** `python tools/batch-prove.py tools/jobs/replace-family-shared-parameters-2026-10-12.yaml`, FAMILY-TITLE filled

**Look for.** positive ("Width=Heron Unit Width; Depth=Heron Unit Depth", two in one call as the owner's four will be): `replaced` two entries - Width -> Heron Unit Width and Depth -> Heron Unit Depth, Length, type, under Dimensions, the file's GUIDs; formulasReadBack "Half Width = Heron Unit Width / 2"; keptLinks 1 labelled dimension and both types' values. negative ("Width=Heron Test Tag"): refused naming Length and Text, `replaced` empty. Look at Family Types afterwards - Width still there: rollback has failed on this PC before (BD1) - **Struck 2026-10-18:** `batch-prove` PASS 2026-10-12 - *Run 2026-10-12* below - and signed with DE9.

---

### Row DE4

*Moved from the register on 2026-11-17.*

**Run.** The owner's case: in a fresh copy of DE1's family, the workaround first - `ADD_FAMILY_SHARED_PARAMETERS` "Heron Unit Width" beside Width, `SET_FAMILY_FORMULA` "Width = Heron Unit Width" - then the replace "Width=Heron Unit Width"

**Look for.** **refused**, naming Width's formula and that Heron Unit Width is already in the family; nothing changed. Then clear the formula (`SET_FAMILY_FORMULA`), delete Heron Unit Width (`DELETE_FAMILY_PARAMETERS`) and replace again: as DE2's positive. This is the path his real family takes - **Struck 2026-10-18:** run 2026-10-12 as looked for, the replace kept - *Run 2026-10-12* below.

---

### Row DE9

*Moved from the register on 2026-11-17.*

**Run.** The signature, on the owner's word only, after DE2, DE3 and DE4

**Look for.** `python brain/heron_validate.py accept add-family-shared-parameters --by "Ajmal PS"`. Drafted 2026-10-12 from `runs/add-family-shared-parameters.json` (DE2; gap: no second route run). **SIGNED 2026-10-12 by Ajmal PS on his word ("Yes, I sign it") - PROVEN** - **Struck 2026-10-18:** signed, as it says (fingerprint `d9d2e24338639f6f`). The card is DRAFT again at version 3, and keeps this proof as a record ([Group DF](../needs-checking/group-df.md)).

---
