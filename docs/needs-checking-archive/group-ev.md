# NEEDS-CHECKING archive — Group EV

> **Checks that were done, moved out of the live register.** These are rows of Group EV of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *what a write changed, counted from Revit's own DocumentChanged, and the run record that keeps every id (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EV1

*Moved from the register on 2026-11-17.*

**Run.** Inner commits inside a group. Changes ON, on a scratch copy of a project in Revit 2024: `revit_change` capability `CREATE_TEXT_NOTE` with a view, a point and `noteText=HERON EV1`

**Look for.** The reply's `changed:` line counts at least one added element under `Text Notes` and the reply ends with the KEPT paragraph whole; `heron_run_record` with the reply's `run` lists the note's id as `fragment added`. The audit line for that workflow (`%APPDATA%\Heron\audit\audit-<yyyymm>.jsonl`) carries `changeEvents` 1 or more and `elementsAdded` 1 or more. **FAIL:** *"changed: not counted - Revit reported no change event"* - the event did not reach a commit inside the group, and every count stays absent - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18, with `CREATE_DUCT` - the results table under *The 2026-10-18 run* below.

---
