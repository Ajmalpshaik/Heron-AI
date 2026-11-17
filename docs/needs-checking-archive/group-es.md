# NEEDS-CHECKING archive — Group ES

> **Checks that were done, moved out of the live register.** These are rows of Group ES of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *"show me / zoom to / isolate these" with Changes OFF: `revit_show`, the temporary isolate, and `ZOOM_TO_LINKED_ELEMENTS` (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row ES1

*Moved from the register on 2026-11-17.*

**Run.** **Selection and zoom work with Changes OFF.** In a chat: `revit_read` capability `SELECT_BY_CATEGORY_NAME` with `categoryName=Ducts`, then `revit_show` capability `SET_SELECTION` with `expect_from=select-by-category-name`; then `revit_show` capability `ZOOM_TO_ELEMENTS` with `alsoSelect=false` and the same `expect_from`

**Look for.** The ducts highlighted, then the camera on them - neither refused for Changes; the banner reads *Showing on screen: set-selection*; Revit's Undo list gains **no** entry; the model's element count before and after the same. **The D-30 positive for the door** - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row ES9

*Moved from the register on 2026-11-17.*

**Run.** **`OPEN_VIEW` with Changes OFF (D-125).** After the add-in is rebuilt and deployed: `revit_show` capability `OPEN_VIEW` with `viewName=` a plan that is not on screen, then the same with a view template's name

**Look for.** The plan opens and is the view on screen, with no Undo entry and Changes still off; the template is refused by name, nothing opened - **the negative**. Until 2026-10-17 the add-in refused it on this door (*runs only as a change*) because it declared `editScopeFailures` ([row 5b-498](../fragment-issues/section-5b-rows-476-500.md)) - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row ES11

*Moved from the register on 2026-11-17.*

**Run.** *Not a proof - an observation.* `revit_show` capability `ISOLATE_ELEMENTS` on ES1's ducts, then `revit_change` with the same, Changes ON

**Look for.** The first refused BEFORE anything is sent - it is MODIFY since D-125 - and the refusal names `revit_isolate_temporary`; the second isolates as it always did, one Undo entry ([row 5b-499](../fragment-issues/section-5b-rows-476-500.md)) **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---
