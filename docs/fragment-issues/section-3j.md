# Fragment issues — section 3j

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3j. THE REFUSALS OF §3h.1, IN FRONT OF A MODEL — 2026-09-09

**Eight of the eleven fragments PR #50 changed have now been run with both legs** against
`Snowdon-scratch_ajmal.al` (9,628 elements, Revit 2024). Drafts are in `brain/proof-drafts/`,
**unsigned**, and `heron-status` is untouched on all eight — promotion follows a signature, not a run.

The arrangement is `tools/jobs/reprove-refusals.yaml`, kept so the next round starts from what was
learned rather than from the fragment list.

| Fragment | Positive | Negative — the refusal |
|---|---|---|
| `measure-run-quantities` | `areas 22`, `lengths 22` | `measure=ZZZNOTHINGHERE` → `areas 0`, `lengths 0` |
| `color-by-parameter` | `carrying 22`, `coloured 22` | `carrying 0`, `coloured 0` |
| `add-schedule-fields` | `added 2`, `schedulesSeen 3` | `added 0`, `availableFields 0` |
| `add-schedule-combined-field` | `added 1` | `added 0`, `notASchedule 1` |
| `remove-schedule-fields` | `hidden 1` | `hidden 0` |
| `set-schedule-appearance` | `changed 1` | `changed 0` |
| `set-schedule-filters` | `filtered 1` | `filtered 0` |
| `set-schedule-sort-group` | `appliedOrder 1` | `appliedOrder 0` |

**The element count was 9,628 before and 9,628 after**, and the document's *unsaved changes* marker
cleared — every write rolled back. §1c's check, run either side of the batch as that section asks.

### Two of them show the FIX rather than merely passing

**`add-schedule-fields` is §3e end to end.** The positive reports `handed 3, schedulesSeen 3` — three
`ScheduleSheetInstance`s resolved through to the `ViewSchedule` behind them, which is exactly what it
could not do before. Two took the column; the third answered `unknownField [Comments]` and handed back
**256** real names in `availableFields`. Three outcomes, correctly separated, on one call.

**`color-by-parameter` carries the defect and the repair in one counter.** `carrying 22` on the
positive, `carrying 0` on the negative. Before PR #50 there was no such counter: `valueOf` answered
`""` both for a parameter that is absent and one that is empty, so `carrying 0` still coloured 22.

### One still blocked, and it is the arrangement

`edit-text-values` — `setup_failed` on **both** legs. `Text Notes` is not selectable on the sheet
`Notes, Symbols & Schedules`, so the seven notes §3c found live in some other view and **nothing in
the library lists which**. That is §3d's `LIST_*` gap again, and it is now the only thing between this
fragment and a proof: its refusal path is written and compiles, and has never run.

Its draft is in `brain/proof-drafts/` and says `NOT ESTABLISHED` for both legs. **It must not be
accepted.**

### Two were skipped by decision, not by failure

| Fragment | Why |
|---|---|
| `export-schedule-to-csv` | `risk: PUBLISH`, and §5 defect 9 means the client refuses to *send* anything above Modify |
| `add-revision-cloud` | needs `revisionId` as an **ElementId**, which D-54 does not resolve — the same wall as `set-global-parameter` in §3i |

### Three things about ARRANGING these, each learned by getting it wrong

**1. `open-view` CANNOT BE A SETUP STEP.** Setup runs every step down `run_fragment_read`
(`heron_bridge_client.py`), and `open-view` is `risk: EXECUTE` — so step 0 is refused and **every leg
of every job fails**, which reads as a broken arrangement. Putting it in the chain made a batch of
seven *worse* than leaving it out, where at least the negatives had run. Run it as its own `validate`
before the batch instead; give the negative a view name that does not exist and the positive's view
stays on screen.

**2. BOTH LEGS MUST SELECT IN THE SAME VIEW, or the arrangement depends on what is open.** Selection
is view-scoped (rule 3). A positive selecting on a sheet and a negative selecting in `FloorPlan: M1`
works only until some earlier job leaves the wrong view on screen — and then six jobs fail at setup
with no hint that the *view* is what moved. The fix is to find the contrast **inside one view**:
`Schedule Graphics` against `Title Blocks` on the same sheet is a real contrast and needs no view
change at all.

**3. ONE REVIT, ONE SESSION.** Two chats proving at once cannot share a Revit, and the failure does not
say so — §3i already records that a stale lease reports `Could not identify the active model`. Two more
shapes of the same collision were met today: `ping` succeeding while every real request times out (the
bridge answers on its own thread; the API thread is busy), and a countdown that goes **up** rather than
down, because each refused attempt counts as activity and renews the very lease it is waiting on.
Stop touching it and wait, or stop the other session.

**And the MCP tools and the command line hold DIFFERENT leases.** One `revit_use_this_model` from a
chat takes the lease under that chat's id; the CLI pins its own and is refused, `release` cannot hand
back a lease it does not own, and the MCP server's id is a fresh uuid per process so it cannot be
matched. Pick one and stay on it for the whole run.

---
