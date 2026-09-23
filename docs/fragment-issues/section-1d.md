# Fragment issues — section 1d

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 1d. A ROLLED-BACK WRITE CLEARS THE SELECTION — and that blocks 100 proofs

Established by running it, 2026-09-08:

| Step | Selection afterwards |
|---|---|
| `select-by-category-name` chained into `set-selection` | **307** |
| a READ fragment | 307 |
| the same READ fragment again | 307 |
| **a WRITE fragment with no `apply`** — so nothing was kept | **gone** — `needs_unbound` |

Reads leave it alone. The rollback takes it with them.

### Why this is not a small thing

**100 fragments need a selection AND a caller value** — that combination is the whole remaining
population of provable work, and the negative case for every one of them is a second run with a
different value. `validate` runs both phases in one go, so the second phase always arrives at an empty
selection and refuses. `create-selection-filter` proved its positive this afternoon and could not reach
its negative for exactly this reason.

**It is arguably correct behaviour.** The selected elements were deleted and recreated by the rollback,
so the ids Revit was holding no longer exist; clearing is safer than pointing at ghosts. But correct or
not, it means no selection-based write fragment can be proved by the current tooling.

### …and `--setup` cannot run a step that WRITES — found 2026-09-09

Setup steps go through `run_fragment_read`, which opens no transaction, so a `MODIFY` fragment used as
setup throws *"Attempt to modify the model outside of transaction"*.

`show-elements` is the fragment that needs it: to prove it, something must be hidden **and stay
hidden**, so its arrangement is `hide-elements` — a write. And a write in the setup would have to be
APPLIED to survive into the phase, which is the one thing the whole rollback design exists to avoid.

**A genuine chicken-and-egg, worth deciding rather than patching:** either the setup gets its own
applied-then-undone bracket around both phases, or fragments that need a written arrangement are proved
by TRACKING instead. It is the same shape as `update-saved-set`, which needs a saved set that another
fragment creates and rolls back.

### FIXED, 2026-09-08 — `validate --setup`

`--setup <fragment>` names a fragment to run before **each** phase, repeatable and in order. It runs
with the POSITIVE values always, because it is the arrangement rather than the question: *"these
elements, selected"* is what both phases are asked about, and only the question changes.

```
validate create-selection-filter --write   --setup select-by-category-name --setup set-selection   --set categoryName=Ducts --set "inViewOnly=FloorPlan: L3"   --set filterName="HERON SEL Z9"   --negative-set filterName=Domestic ...
```

**`create-selection-filter` was the first selection-based WRITE ever proved** — 307 elements saved to a
new filter, 0 when the name was already taken. `assign-location-data` followed.

One thing had to be got right and was got wrong first: **only the first setup step resets the chain.**
The whole point is that step two consumes what step one left — `select-by-category-name` leaves
`elements`, `set-selection` needs them — and resetting between them threw that away. The symptom read
as a missing selection rather than a discarded one.

### The reasoning that led there, kept because it generalises

`validate` needs to re-establish the arrangement before EACH phase, not once at the start — the same
way it already sends different caller values per phase. A `--setup` chain naming fragments to run first
(`select-by-category-name`, `set-selection`) would do it, and the machinery already exists: `prove` runs
a chain in one lease, and D-29's chaining carried `elements` between those two fragments correctly on
the first try.

Until then, a selection-based write fragment can be RUN once and its positive recorded, but not proved.

---
