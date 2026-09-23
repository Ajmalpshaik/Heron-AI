# Fragment issues — section 3g-iv

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3g-iv. SIX SWEEPS LATER: THE ARRANGEABLE POOL IS EXHAUSTED — 2026-09-11

Six bulk passes asked **77 fragments**; 13 passed, 12 were promoted, one was put back. What remains,
recomputed from the contracts and from `FromRequest` after the last sweep:

| Count | What blocks it |
|---|---|
| 76 | Arrangeable — blocked by CONTENT or a hard negative |
| 65 | A request value that cannot be supplied ([D-54](../DECISIONS.md)) |
| **12** | **Gated by the permission tier — `ADMIN` or `PUBLISH`** |
| 6 | Its only declared result is a `bool`, a bare `ElementId` or a `string` |
| 3 | Needs TWO element sets from one selection |
| 1 | No declared result at all |
| **163** | |

**The 12 are new to this table and they are not blocked by anything technical.** `validate` runs them
today — that is the OPEN defect in §5 — and the owner's decision on 2026-09-11 is that it should not.
Until the gate is on the proving path, **every job file has to apply it by hand**, which is what
[`sweep-last-three.yaml`](../../tools/jobs/sweep-last-three.yaml) does and says.

### The eight that were left, and why five could never have run

| Fragment | Blocked on |
|---|---|
| `align-viewports-across-sheets`, `create-sheet-list`, `place-views-on-sheet`, `set-sheet-title-block` | **SHEETS.** Neither open model has one |
| `fillet-lines` | **LINES.** Neither open model has one |
| `set-element-workset` | `moved 0` at `worksetId 0` — either there is no workset 0 or the ducts are already on it |
| `place-accessory-on-run` | *"No family type called "Damper" in Snowdon-scratch"* — the model has no duct accessory |
| `report-findings` | **Cannot carry a proof at all.** Its only result is `report`, a `string`, and its whole purpose is to write a sentence *"when nothing was found at all"* — so it is never empty. A describer, and [D-53](../DECISIONS.md) tracking is the route |

### What a seventh sweep would need, and it is not another job file

Nothing is left that a cleverer arrangement reaches. The three routes out are all changes to the
library or the harness:

1. **Value passing** — 65 fragments, and `ElementId` + `Element` are 42 of the 82 refused values.
2. **A model with a drawing set, CAD imports, drafted lines, pipes and design options.** Four sheet
   fragments and a dozen others turn on content neither open model has. `Snowdon Towers Sample
   HVAC.rvt` — the delivered sample rather than the scratch copy — is the obvious candidate and was
   never opened.
3. **The gate on `validate`**, which releases 12 immediately.

> Six sweeps found thirteen proofs and eleven defects. **The defects are the better return**, and
> that is not a consolation: a sweep that only proves things tells you what already works, and this
> one kept finding fragments that report work nobody did.

---
