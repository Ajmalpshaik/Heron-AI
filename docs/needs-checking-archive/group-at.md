# NEEDS-CHECKING archive — Group AT

> **Checks that were done, moved out of the live register.** These are rows of Group AT of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *`set-schedule-field-totals`: "Calculate totals" switched on for a schedule's columns (2026-09-27)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row AT2

*Moved from the register on 2026-11-17.*

**Check.** The second route, THEN the signature. Revit's own Formatting tab and the printed schedule, on the WORKING model after the chat run, or on the test copy after the positive leg run KEPT in a chat. Write what was seen into the `second_route` field of the draft `brain/proof-drafts/` holds for `set-schedule-field-totals`, with the model and the date. Only then sign it with `python brain/heron_validate.py accept set-schedule-field-totals --by "Ajmal PS"` and set `heron-status: PROVEN`

**Expected.** The Formatting tab reads "Calculate totals" for Flow and "No calculation" for Family and Type. Opened, the schedule shows a summed flow in each room's footer and, if the grand total is on, at the bottom - **read on screen, not inferred from `totalled`** **CLOSED 2026-11-13, not passed** - `SET_SCHEDULE_FIELD_TOTALS` was retired into `SET_SCHEDULE_APPEARANCE` on 2026-11-01 (its `deprecated:` block); `SET_SCHEDULE_APPEARANCE` is PROVEN at version 5, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---

### Row AT3

*Moved from the register on 2026-11-17.*

**Check.** A column showing a minimum or maximum. On the test copy, set Flow's calculation to "Calculate maximum" by hand, then in a chat: FIND_SCHEDULES `nameContains=Air Terminal Schedule`, then SET_SCHEDULE_FIELD_TOTALS `fieldNames=Flow`

**Expected.** `totalled` 1 and `replacedCalculation` naming *"its maximum"* - the old calculation reported, not silently lost - and the Formatting tab reading "Calculate totals". One Ctrl+Z takes it back to "Calculate maximum" **CLOSED 2026-11-13, not passed** - `SET_SCHEDULE_FIELD_TOTALS` was retired into `SET_SCHEDULE_APPEARANCE` on 2026-11-01 (its `deprecated:` block); `SET_SCHEDULE_APPEARANCE` is PROVEN at version 5, `proof:` dated 2026-11-02. Nothing here runs on the retired card.

---
