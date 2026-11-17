# NEEDS-CHECKING archive — Group JD

> **Checks that were done, moved out of the live register.** These are rows of Group JD of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *every entry reaches the chat, and one subcategory's style copied on its own: `READ_ELEMENT_PARAMETERS` version 6, the add-in's `providesEntries`, `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS` version 2 (2026-10-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row JD0

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST.* `SELECT_BY_FAMILY familyName=VCD exactMatch=false categories=Duct Accessories includeLinks=false`, then `READ_ELEMENT_PARAMETERS parameterName=ZZZNOTHINGHERE`

**Look for.** `findings` *"ZZZNOTHINGHERE": 0 of 38 carry a value, 0 blank, 38 absent. Absent: <all 38 ids>.* - every id, no "... and N more"; `values 0 entry(ies)` with no line under it **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---

### Row JD2

*Moved from the register on 2026-11-17.*

**Run.** `parameterName=Mark` on the same 38

**Look for.** 37 values and `Blank: 7987914` (VCD005 carries no Mark) - every value printed **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---

### Row JD4

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST for the copy.* `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS sourceDocumentTitle=4355-BHVD-3D-21S10-BL001A nameContains=Generic Annotations subcategoryContains=ZZZNOTHINGHERE`, in the target, rolled back

**Look for.** `changed` 0, `created` 0, `refused` *"No categories in '...' matching 'Generic Annotations' with a subcategory matching 'ZZZNOTHINGHERE'."*; LIST_CATEGORIES on the target's Generic Annotations reads as before **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---

### Row JD5

*Moved from the register on 2026-11-17.*

**Run.** *The fault's own case.* The same with `subcategoryContains=Signature`

**Look for.** `changed` (or `created`) holds ONLY *Generic Annotations : Signature*, its colour moving to 30,80,170; nothing for the Generic Annotations row itself or any other subcategory **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---

### Row JD7

*Moved from the register on 2026-11-17.*

**Run.** `subcategoryContains` LEFT OUT, `nameContains=Generic Annotations`, rolled back

**Look for.** version 1's answer: the category and every subcategory that differs in `changed`, the missing ones in `created` - proves absent means all **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---

### Row JD9

*Moved from the register on 2026-11-17.*

**Run.** `sourceDocumentTitle=4355-BHVD-3D-21S10-BL001A.rvt`

**Look for.** the same as JD5 - ".rvt" is read as the title **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---

### Row JD10

*Moved from the register on 2026-11-17.*

**Run.** *A link is no source.* `sourceDocumentTitle=` the exact title of one of the source's LINKED models (4355-BU3D-21S10-BL001A)

**Look for.** `refused` *"No open project matching ..."*, and its "open now" list names no linked model - added 2026-10-28 when the first JD5 run listed seven links among the open projects **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972*, which records it run and passed.

---
