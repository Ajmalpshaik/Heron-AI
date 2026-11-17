# NEEDS-CHECKING archive — Group KJ

> **Checks that were done, moved out of the live register.** These are rows of Group KJ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a Material Takeoff schedule view of one category: `CREATE_MATERIAL_TAKEOFF` (2026-11-01)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row KJ0

*Moved from the register on 2026-11-17.*

**Run.** *Open.* "test projject" open and in front in Revit 2024, Changes on at the ribbon

**Look for.** The title bar reads test projject **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---

### Row KJ1

*Moved from the register on 2026-11-17.*

**Run.** *Check the arrangement.* At least one Basic Wall drawn in the host model, not in a link; no schedule called **HERON TEST TAKEOFF** under Schedules/Quantities

**Look for.** A wall selected in the host shows a Basic Wall type in Properties. Draw one if there is none, and save **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---

### Row KJ2

*Moved from the register on 2026-11-17.*

**Run.** *Read the category list - also the "none named" negative.* `create-material-takeoff` with `--write`, `category` left out, `fieldNames` "Material: Name"

**Look for.** Nothing made; refused *No category was named ... it is never chosen for you*; `validCategories` listing what Revit allows. **Record** whether *Multi-Category* is first on it **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---

### Row KJ3

*Moved from the register on 2026-11-17.*

**Run.** *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file

**Look for.** Negative: `category` Grids - nothing made, refused naming Grids, `validCategories` filled. Positive: `created` one id, `scheduleCalled` HERON TEST TAKEOFF, `addedFields` the three, `missingFields` empty. If any field is missing, take the spelling from `fieldsAvailable` into the job file and run again **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---

### Row KJ4

*Moved from the register on 2026-11-17.*

**Run.** *The refusals and the reports.* KJ3's positive with `fieldNames` "Material: Name,Colour Of Moon"; then "Colour Of Moon" alone; then `category` Floors with "Family and Type,Area"; then `category` Multi-Category

**Look for.** First: made, *Colour Of Moon* in `missingFields`, `fieldsAvailable` listing the material fields. Second: nothing kept, refused *Not one of the fields asked for ...*. Third: both added, and a finding naming the material's own area field. Fourth: **record** made or refused **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---

### Row KJ5

*Moved from the register on 2026-11-17.*

**Run.** *Kept, through `try-once`, on the modeller's yes.* KJ3's positive on a saved copy

**Look for.** Keep or Discard. Then the same run again is refused - *A schedule called "HERON TEST TAKEOFF" is already in the project* - and Revit's own View > Schedules > Material Takeoff of Walls with the same fields reads row for row the same: the second route **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---

### Row KJ6

*Moved from the register on 2026-11-17.*

**Run.** *Revit 2020.* KJ3 again

**Look for.** The same answers. Nothing the card calls changed between releases **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-1376-1400.md)); the takeoff is checked as KK31

---
