# Fragment issues — section 3b

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3b. NO POSITIVE CASE IN THIS MODEL — found 2026-09-08, second round

Ran correctly and returned the honest empty answer. The model simply has none of the thing.

| Fragment | What was tried | What came back |
|---|---|---|
| `select-openings` | `inViewOnly=FloorPlan: L3` | `elements 0` — there are no openings in that view. Needs a view with a wall or floor opening in it, or one drawn on purpose |
| `select-from-saved-set` | `filterName=Domestic`, `view=FloorPlan: L2` | `elements 0` — *"'Domestic' is a RULE. It matches 0 element(s) in view"*. Consistent with `audit-view-filters`, which found all four filters on the Mechanical Plan template are switched off AND carry an empty override. **The filters in this model do nothing**, so nothing here can prove a fragment that reads them |
| `report-areas` | `schemeNameContains=` (everything) | `areas 0` — the model has no Area scheme with placed areas. The negative returned 0 too, so the two cases are identical and nothing separates working from doing nothing |
| `check-ceiling-coordination` | 307 ducts in L3, `tolerance=99999` | `outOfPlane 0`. A tolerance nothing can satisfy still found nothing, because the ceilings are in the architectural LINK and the executor skips linked documents by design. Needs a ceiling drawn in the host |
| `check-fixture-connectivity` | air terminals in M1, three services required | `missingService 0`. Demanding Supply, Return AND Exhaust of every terminal still found none incomplete. Needs a terminal with a service genuinely absent |
| `find-overlapping-lines` | 307 ducts, `toleranceMm=99999` | `overlapping 0`. **Not a tolerance defect — it works on model LINES, and a duct is not one.** Needs detail or model lines, which this selection never contained |
| `find-dead-ends` | 307 ducts, `stubLength=99999` | `deadEnds 0`, confirming the earlier run rather than resting on it. `select-by-connection-status` proved every duct end in this model is connected, so there is nothing to find at any stub length. **Confirmed a THIRD time 2026-09-10 with `stubLength=10`, and this run names the mechanism rather than the outcome: `openEndsFound 0` across all 307.** The fragment filters open ends that are meant to be there - a run reaching a terminal, a fixture or a cap - but here there were no open ends AT ALL to filter, so the empty answer is settled at the source. **STOP RE-RUNNING THIS ONE.** Three runs at three stub lengths have now agreed, and the row said so before the third. **This still stands FOR THIS MODEL — but the fragment is PROVEN, on `Project1` which has four open duct ends. The row named what it needed and a different model had it: §3b-ii** |
| `select-subcomponents` | 10 Mechanical Equipment (Heat Recovery Units) in L3, `recursive=true` | `elements 0`, and the accounting proves it LOOKED rather than skipped: `withoutSubComponents 10` and *"0 nested element(s) found under 10 parent(s), followed 0 levels"*. A nested shared family lives inside a loadable family instance, and none of this model's equipment has one. **Its NEGATIVE is already perfect and is worth keeping** - 22 Ducts return `withoutSubComponents 0` with *"22 of what was given"* excluded, because ducts are SYSTEM families and structurally cannot nest anything in any model, which is a stronger negative than a count that a richer model could overturn. Needs one piece of equipment with a nested shared family, or a multi-component fixture |

---
