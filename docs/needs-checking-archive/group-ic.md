# NEEDS-CHECKING archive — Group IC

> **Checks that were done, moved out of the live register.** These are rows of Group IC of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the visualiser: a whole look in one call, what a look would do, and the method a visualisation artist follows (2026-10-20)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IC1

*Moved from the register on 2026-11-17.*

**Run.** *`REPORT_VISUAL_PRESET` - the negatives FIRST.* view `Heron Frame - Heater`, preset `Studio Glow`; then view `Heron Render Schedule`, preset `Exterior Day`

**Look for.** **First:** refused, naming the seven presets, wouldChange 0, presetsCompared 0. **Second:** wouldChange 0, every line of comparison *skipped* with its reason - the schedule has no visual style, no sun and no Rendering Settings **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 (evening) - Revit 2020, the owner's laptop*, which records it run and passed.

---

### Row IC3

*Moved from the register on 2026-11-17.*

**Run.** *`APPLY_VISUAL_PRESET` - the negatives FIRST.* view `Heron Render Schedule`, preset `Exterior Day`; then view `Heron Frame - Heater`, preset `Studio Glow`; then preset `Clay`, renderFor `print`

**Look for.** Each refused with **NOTHING WAS CHANGED**, changed 0: *"takes none of Exterior Day's settings"*, then the seven names, then *check or issue*. `REPORT_RENDER_SETTINGS` on the camera view reads exactly IC0's BEFORE **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 (evening) - Revit 2020, the owner's laptop*, which records it run and passed.

---

### Row IC9

*Moved from the register on 2026-11-17.*

**Run.** *For an issued image.* `validate apply-visual-preset --write` (rolled back), `Heron Frame - Heater`, preset `Exterior Day`, renderFor `issue`, sunTime `15:00`

**Look for.** Render Quality *High*; Render Output *300 DPI* - **or** a not-taken line naming the DPIs Revit's printer list offers (say which); Sun *still 15:00* **Struck 2026-11-13:** PASSED - the verdict is in this file's *Run 2026-10-20 (evening) - Revit 2020, the owner's laptop*, which records it run and passed.

---
