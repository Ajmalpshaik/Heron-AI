# NEEDS-CHECKING archive — Group DI

> **Checks that were done, moved out of the live register.** These are rows of Group DI of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *an element's full parameter record by GUID, with where it stands and its tags (2026-10-12)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row DI1

*Moved from the register on 2026-11-17.*

**Run.** ~~Positive: the exhaust fan *TRG_FAN_EXHAUST FAN: AX45 (450mm Long)*, id 7614932, sampleOnly, type parameters, record, tags~~

**Look for.** **DONE 2026-10-12.** Both *Classification.Uniclass.Pr.Number* on its type, GUID 8212e2e8... `<blank>` and GUID ee217f5d... `Pr_65_67_29_05`; `sameNameTwice` 4 (the two Uniclass pairs, and OmniClass Number and Title - a shared one beside the built-in); `tagsFound` 5, each *TCM_TG_HVAC Equipment: Equipment Tag* with its view, head and leader; location, bounding box and facing (0, 1, 0)

---

### Row DI2

*Moved from the register on 2026-11-17.*

**Run.** ~~Negative: the louvre *TCM_EAL_T001_ExhaustLouvre: Standard*, id 7701431, the same switches~~

**Look for.** **DONE 2026-10-12.** `sameNameTwice` 0, `tagsFound` 0, the record still whole. `batch-prove` says NEG NOT EMPTY: the parameter counts are never empty for a real element, so version 1 was proved by tracking (D-53) and the new outputs are read by eye

---

### Row DI3

*Moved from the register on 2026-11-17.*

**Run.** ~~Before and after: version 1 from a base worktree of origin/main against version 2, same fan, same selection~~

**Look for.** **DONE 2026-10-12.** 322 parameters against 329 - 156 to 158 on the instance, 166 to 171 on the type. 535 elements in one call came back as a 6.2 MB record, whole

---
