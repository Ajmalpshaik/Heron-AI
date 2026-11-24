# FRAGMENT-ISSUES archive — section 5b, rows 976 to 1000

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 976 to 1000 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-977

*Moved from the register on 2026-11-17.*

**Defect.** **A READ-LEVEL JOB CAN STILL OPEN A TRANSACTION OF ITS OWN IN THE MODEL IT RUNS IN, COMMIT IT, AND KEEP THE CHANGE - AND THE RUN GUARD DOES NOT CATCH IT.** Found 2026-10-17 building [row 976](../fragment-issues/section-5b-rows-401-425.md)'s guard. `run_fragment_read`'s guarantee is that it opens NO transaction, so Revit refuses any change - `mcp/server/heron_tools.py` says *"What holds for every caller is Revit's own refusal of a change made outside a transaction"*. But nothing stops the fragment's own code calling `new Transaction(doc)` and committing; only `needs_write_path` refuses a fragment that DECLARES it opens its own. The run guard treats every change to the model a job runs in as the transaction group's business, so this one passes it too. Read in the source, not run. Measured 2026-10-17: no fragment in the library at READ, ANALYZE or EXECUTE opens a `Transaction`, `SubTransaction` or `TransactionGroup` today, so nothing exercises the hole. **Narrowed the same day by [row 350](../fragment-issues/section-5b-rows-326-350.md)'s detector**: a read run whose model's element or type count, or `IsModified`, moves is now answered `read_changed_model`. That catches such a commit in a saved model, or one that adds or removes an element; a commit that only edits existing elements in a model that already had unsaved changes still passes both. **And by [row 592](../fragment-issues/section-5b-rows-401-425.md)**: the add-in now refuses, from its source, a read or show fragment that opens a transaction of its own. What is left is behaviour the source does not show.

**State.** **MOVED TO ISSUE [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** The run guard is where it belongs: a committed `DocumentChanged` in ANY open model while a job below Modify runs is that job seen changing a model. Not done in row 976's change because a read-level fragment that legitimately commits something would then be refused - none is known, and whether one exists is to be listed and checked in Revit before the rule changes.
