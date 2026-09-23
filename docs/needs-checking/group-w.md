# Needs checking — Group W

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group W — three fragments stand between the skills and three more proofs

Measured 2026-09-20 against the eleven model-half job files the skills session
generated (#199). Of the ten skills, **seven have no unproven step at all** —
every fragment in their plan is already `PROVEN`, so `batch-prove` reports
ALREADY and sends nothing, and what they are waiting for is a runner that can
execute a COMPOSITION ([row 141](../FRAGMENT-ISSUES.md)).

**Three skills have unproven steps, and between them those are only THREE
fragments:**

| skill | the step that is not proven | risk |
|---|---|---|
| `check-connectivity` | `trace-connectivity`, `report-findings` | READ, SUGGEST |
| `find-blank-parameters` | `describe-blank-parameters`, `report-findings` | ANALYZE, SUGGEST |
| `trace-system` | `trace-connectivity` | READ |

**AND ALL THREE ARE THE SAME KIND OF THING, WHICH IS THE POINT OF THIS GROUP.**
Every one of them sits in a Group V bucket that `batch-prove`'s rule cannot
judge, and not one is a defect:

* **`trace-connectivity`** seeds `reached` with its own `start`, so the answer
  can never come back empty whatever it is handed.
* **`report-findings`** answers with PROSE. Its negative is *correct* — it says
  *"NOTHING WAS CHECKED"* — and a non-empty string reads as a find.
* **`describe-blank-parameters`** is the only fragment of 395 declaring no
  `role: result` at all, so there is nothing for the rule to look at. Its run
  record is already a textbook tracking proof and nothing can score it.

**SO ONE PIECE OF MACHINERY UNBLOCKS THREE FRAGMENTS AND THREE SKILLS**: a
tracking mode for fragments, the same shape `tools/prove-agent.py vary` already
gives agents — run one fragment across **three or more** values of one input on
one model and show the answer FOLLOWING the input. [D-53](../DECISIONS.md) is the
rule, `prove-agent.py` is the working example, and `MIN_TRACKING_ROWS = 3` is
already enforced in `brain/heron_validate.py` for agents.

**IT IS THE HIGHEST-LEVERAGE THING LEFT IN THE PROVING MACHINERY** and it is
deliberately not started here: PR #198 was already large and carrying the
row 136 root-cause fix, and a feature this size belongs on its own branch
rather than delaying seventeen repairs. Recorded so the next session does not
have to re-derive which three fragments matter or why.

> **STARTED 2026-09-19, AND THE HALF THAT NEEDS NO REVIT IS BUILT AND TESTED.**
> `tools/prove-tracking.py` does every check that can be made before a model is
> opened, and `--dry-run` answers completely. **All three fragments above are
> arrangeable**, proved in `tests/test_prove_tracking.py`: `trace-connectivity`
> varying `tolerance` with `start=selected` held still, `report-findings`
> varying `whatWasChecked` with `checkedCount` held, and
> `describe-blank-parameters` varying `parameterName`.
>
> **ONE CORRECTION TO THE READING ABOVE.** `describe-blank-parameters` is
> described here as having nothing for the rule to look at because it declares
> no `role: result`. `heron_fragment.provide_role` reads an **absent** `role:`
> as `result` - the stricter reading, and deliberately so - therefore its
> `findings` IS a declared result and a tracking row can carry it.
>
> **What is not built is the runs**, which need a live session to develop
> against. The tool exits 3 and names that rather than sending something
> half-built at a model. [FRAGMENT-ISSUES row 151](../FRAGMENT-ISSUES.md).
---
