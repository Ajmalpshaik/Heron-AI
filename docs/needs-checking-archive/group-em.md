# NEEDS-CHECKING archive — Group EM

> **Checks that were done, moved out of the live register.** These are rows of Group EM of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a DRAFT run once on the live model, behind Keep or Discard: `"apply": "ask"` and `try-once` (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EM6

*Moved from the register on 2026-11-17.*

**Run.** EM1 and EM2 once on Revit 2024 and once on Revit 2027

**Look for.** The same - one undo entry on Keep, none on Discard - **Struck 2026-10-18:** PASS on Revit 2024 (EM1 and EM2's runs) and on 2027, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row EM1

*Moved from the register on 2026-11-17.*

**Run.** The plan in front, saved. `try-once <fragment>` with the values it needs; when the box opens, look at the view, then press **Keep it**

**Look for.** The view shows the change BEFORE the box is answered; the box reads *"'<fragment>' has never been proved. Keep what it did?"*; after Keep the change stays, the Undo list has exactly ONE new entry `Heron: <fragment>`, and the command line prints KEPT and writes `<fragment>.once.json` with `kept: true`. The box waits for you - Heron's own dialog watch (Group EL) never answers it. One Ctrl+Z takes the whole change back **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row EM2

*Moved from the register on 2026-11-17.*

**Run.** Save again. The same fragment, corrected or with other values so it is a run the modeller would not keep; press **Discard it**. Then once more, closing the box with Escape

**Look for.** Both times the model is exactly as before - **the Undo list is unchanged**, no `Heron:` entry - the reply says `rolledBack` true and DISCARDED, and the record says `kept: false`. Ctrl+Z undoes whatever was done BEFORE the run, never this **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---

### Row EM4

*Moved from the register on 2026-11-17.*

**Run.** Refusals, each before anything runs: (a) make one unsaved change, then `try-once`; (b) open a second model in front, then `try-once --in "Heron Try Once"`; (c) `try-once` on a PROVEN or READ fragment

**Look for.** (a) `ask_needs_saved_model` - *"Save the model, then ask again"*; (b) `ask_needs_model_in_front`; (c) refused by the command line, nothing sent. In every case the Undo list is unchanged **Struck 2026-11-13:** PASSED - the verdict is in this file's *The 2026-10-18 run*, which records it run and passed.

---
