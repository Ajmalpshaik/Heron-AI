# NEEDS-CHECKING archive — Group EO

> **Checks that were done, moved out of the live register.** These are rows of Group EO of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a rollback that takes the last kept job's work is said, and a rollback whose additions still stand is not called complete (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EO1

*Moved from the register on 2026-11-17.*

**Run.** **Keep A, then preview B and roll it back.** A: `CREATE_WALL` with **apply**, three points - two walls, KEPT. B: `CREATE_WALL` again, no apply, three other points

**Look for.** B's reply: `rolledBack` true, `rollbackReported` true, `rollbackCheck` `added` 2, `checked` 2, `standing` 0, `keptJob` "create-wall", `keptChecked` 2, `keptLost` 0, and the verdict says NOTHING WAS KEPT with *"none of the 2 elements this run added is still in the model"* and **no** "Rolling back this run removed". A's two walls still there, B's gone. **The check stays silent** - the D-30 positive - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row EO2

*Moved from the register on 2026-11-17.*

**Run.** **Ctrl+Z on A, then B.** Keep A as in EO1, press Ctrl+Z once in Revit (the undo entry reads *Heron: create-wall*), then preview B

**Look for.** B's verdict ends with *"The last kept job ('create-wall') was undone in Revit before this run started, so this run did not remove what it made."* and **never** *"Rolling back this run removed"*; `keptLost` 0. **Reported as undone by the user.** If it says *"What the last kept job ('create-wall') made was already gone when this run started"* instead, the listener did not match the undo - the name `GetTransactionNames()` reports for an assimilated group is not *Heron: create-wall*; write down what it is - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---

### Row EO4

*Moved from the register on 2026-11-17.*

**Run.** **The negative: B deletes A's walls, and is rolled back.** Keep A, select its two walls, `DELETE_ELEMENTS` without apply

**Look for.** `keptLost` 0, no "Rolling back this run removed", both walls back in the model - the rollback restored them, and the check says so by saying nothing. **The D-30 negative**: a loss that a rollback DOES undo is not reported - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below.

---
