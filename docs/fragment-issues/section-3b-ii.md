# Fragment issues — section 3b-ii

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3b-ii. A SECOND MODEL CLEARS A BLOCK THAT WAS NEVER ABOUT THE FRAGMENT — 2026-09-10

Every row above says *"in this model"*, and it was easy to read them as *"this fragment cannot be
proved"*. One of them was tested against a different model and fell immediately.

**`find-dead-ends` is PROVEN.** It was set aside three times on Snowdon, and §3b ends with **STOP
RE-RUNNING THIS ONE** — which was the correct instruction and is still correct *for Snowdon*. The row
also said exactly what would lift it: *"Needs a duct run with a loose end."* `Project1 work_ajmal.al`
was drawn by hand with five ducts, four of them left open:

| Measured 2026-09-10, `select-by-connection-status` `wantOpenEnds=true` | |
|---|---|
| Ducts | **4 of 5** have at least one OPEN end |
| Duct Fittings | 0 of 2, and `withoutConnectors 0` |

Positive `deadEnds 4`, negative `deadEnds 0` and `stubs 0`. Judged **PASS**.

> A blocked row names a MODEL, not a fragment. Before writing one off, read what the row says it
> needs and ask whether some other model has it.

### The view was not called what everyone assumed, and it cost a session

`Project1` is an MEP template. Its **levels** are `Level 1` and `Level 2`; its **plan views** are
`1 - Mech`, `2 - Mech`, `1 - Plumbing` and `2 - Plumbing`. Earlier probes asked for `Level 1` and
`FloorPlan: Level 1`, got nothing back, and concluded the model *"has no floor plan view at all"* —
which is written into [`creators-round-two.yaml`](../../tools/jobs/creators-round-two.yaml). It has four.

The level name and the view name are different things, and in every delivered template they differ.
`find-views` with `viewType=FloorPlan` answers this in one call and no probe needs to guess again.

### What this model blocks, and it is a different list from Snowdon's

| Fragment | What was tried | What came back |
|---|---|---|
| `report-door-room-links` | 1 door, phase `New Construction` against phase `Existing` | **POSITIVE EMPTY.** The pair is honest and shows the purpose's own phase-dependence claim — the same door reads `(outside) -> Room 1` on New Construction and `(outside) -> (outside)` on Existing. But the only `role: result` field is `disagreements`, and it is 0 in both legs. A disagreement needs Revit's `FromRoom`/`ToRoom` to contradict where the rooms physically are — a real modelling fault, which a clean four-wall model does not have and which cannot honestly be manufactured. Needs a model with a genuinely mis-facing door |
| `check-flow-direction` | 5 ducts in `1 - Mech` | `jointsChecked 0`, `bidirectionalSkipped 4`. The ducts are drawn but carry no flow — no system, no equipment, nothing to set a direction. Needs ducts on a real system |
| `find-overlapping-lines` | 5 ducts, then 4 walls, `toleranceMm=99999` | `overlapping 0`, `notStraight 0`, starts and ends read for every element. It looked and there is genuinely nothing stacked. Needs two lines drawn on top of each other — **and note §3b blamed the category for this on Snowdon; here the category was varied and the answer did not change** |
| `find-nearest-elements` | 5 ducts, `metric=distance` | Refused before running: *"this fragment needs 2 separate sets of elements and one selection cannot say which is which… Running anyway would report 0 results, which reads as 'there was nothing to find' rather than 'nobody was asked'."* The two-set gap of §5, refusing honestly. Nothing about this model can fix it |

---
