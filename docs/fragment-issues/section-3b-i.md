# Fragment issues — section 3b-i

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3b-i. THE `POSITIVE EMPTY` POOL IS EXHAUSTED FOR THIS MODEL — triaged 2026-09-10

**`batch-prove.judge` reports `POSITIVE EMPTY` for 41 DRAFT fragments, and that verdict reads like a
worklist: it means the ARRANGEMENT was wrong, not the fragment.** It was treated as one, and it is
not. Triaged rather than retried:

| | |
|---|---|
| POSITIVE EMPTY, still DRAFT | **41** |
| already explained in §1, §1b, §1c, §1d, §2, §3, §3b or §3c | **34** |
| left over | **7** |

**AND EVERY ONE OF THE SEVEN IS BLOCKED BY SOMETHING ALREADY KNOWN, not by a bad arrangement:**

| Fragment | What actually blocks it |
|---|---|
| `select-in-region` | **§5 row 14** — it converts millimetres to feet a second time, so every volume is 304.8x too small. A units defect, and it must not be re-run until that is fixed |
| `set-element-workset` | **The `LIST_*` gap.** It takes `worksetId` as an `int`, which IS receivable — but `list-worksets` provides only `findings`, `worksetCount`, `closedCount` and `workshared`. **The model is workshared with 2 worksets and their ids cannot be learned**, and typing a guess is how a job runs against the wrong thing and reports success |
| `place-views-on-sheet` | The same gap from the other end. 17 sheets exist, so `sheetNumber` is easy; `elements` must be the VIEWS to place, and nothing in the library can put views into a selection |
| `dimension-rooms`, `report-door-room-links` | Both need Rooms. Snowdon is an HVAC model and carries **Spaces**, not Rooms — `FloorPlan: M1` holds 5 Spaces and 0 Rooms |
| `reset-graphic-overrides` | Needs elements that already carry an override. `read-graphic-overrides` sits in §3 for the same reason: nothing in this model has one |
| `place-flow-arrows` | Needs `arrowFamilyName` and `arrowTypeName` — a specific annotation family that has to be loaded first |

**WHAT THIS MEANS FOR THE NEXT SESSION, and it is the useful part.** Further proving against
Snowdon-scratch is not blocked on effort or on arrangements. It is blocked on three things, and each
is a decision rather than a batch: **build the `LIST_*` fragments** (§3i names this the binding
constraint), **fix §5 row 14**, or **build the content on purpose in a scratch model** the way the
first eight write proofs were taken on 2026-09-10.

**Read this table before picking a fragment out of a failure list.** `find-dead-ends` was re-run on
2026-09-10 against a row in §3b that had already recorded, on 2026-09-08, why it can never pass here.

---
