# NEEDS-CHECKING archive — Group IT

> **Checks that were done, moved out of the live register.** These are rows of Group IT of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a selection that refuses a mode it does not know, takes the whole model when no view is named, and knows a connector by its name: `SELECT_BY_PARAMETER_VALUE` v3, `SELECT_BY_CATEGORIES` v3, `SELECT_OPENINGS` v3, the binder's absent View and category aliases (2026-10-22)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IT0

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST.* `SELECT_BY_PARAMETER_VALUE` with `parameterName=Name matchText=Outlet Port matchMode=exactly categories=Reference Planes`

**Look for.** Refused *"matchMode 'exactly' is not a way of matching this knows, so NOTHING was scanned. Use contains, equals (or exact, is), begins, ends, notcontains or notequals"*; no `elements` left behind, so a next step with `expect_from` refuses. Never a count **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972 - IT0 to IT3 ('select-by-parameter-value' version 3)*, which records it run and passed.

---

### Row IT1

*Moved from the register on 2026-11-17.*

**Run.** *The fault's own case.* The same with `matchMode=exact`

**Look for.** `elements` 1 - *Outlet Port* alone - and the finding *"1 of N scanned element(s) match Name equals 'Outlet Port'"*. Never *Outlet Port Bore* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972 - IT0 to IT3 ('select-by-parameter-value' version 3)*, which records it run and passed.

---

### Row IT2

*Moved from the register on 2026-11-17.*

**Run.** `matchMode=is`, then `matchMode=EXACT`

**Look for.** The same answer as IT1, both times **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972 - IT0 to IT3 ('select-by-parameter-value' version 3)*, which records it run and passed.

---

### Row IT3

*Moved from the register on 2026-11-17.*

**Run.** `matchMode=contains`

**Look for.** 2 - both planes, asked for by name; the finding reads `contains` **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-28, the owner's laptop: Revit 2020, session 33972 - IT0 to IT3 ('select-by-parameter-value' version 3)*, which records it run and passed.

---
