# FRAGMENT-ISSUES archive — section 5, rows 76 to 100

> **Finished rows, moved out of the live register.** These are rows from section 5 of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by proving — whose state said they were
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
> Rows 76 to 100 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 99

*Moved from the register on 2026-11-17.*

**Defect.** **GOLDEN RULE 16 IS ENFORCED AS "NO FRAGMENT OPENS A `Transaction`", AND NOTHING HAD EVER NEEDED A SECOND DOCUMENT.** `tests/test_revit_gate.py` Q4 greps for `new Transaction(` across the library and had always counted zero. `write-family-table-in-project` (FRG-ELE-060) is the first to trip it — and its transaction is on a FAMILY document it opened itself, not on the one the executor handed it. The executor never sees that document, holds no `TransactionGroup` over it, and cannot commit it; leaving it uncommitted discards the import. So the rule's PURPOSE — one job, one `TransactionGroup`, one undo on the model — is arguably untouched, while its TEXT is plainly broken. **Revit requires a transaction to modify any document, so as written the rule makes editing a second document impossible for every fragment, forever.** That is a capability question, not a safety property

**State.** **MOVED TO ISSUE [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN as a question, 2026-09-16, and the fragment is WITHHELD rather than the gate widened.** FRG-ELE-060 was built, proved and then left out of the change that introduced it — partly for this, and partly because its one write leg does not work anyway (`Document.LoadFamily` needs an `IFamilyLoadOptions` and **a fragment cannot declare a class**, which is `load-family`'s ruling arriving a second time). **Widening a gate in the same change that trips it is the wrong order**, and it would have been done to admit a fragment that cannot finish its job. The question to settle first: does Rule 16 mean *the executor's document* or *any document*? Today it means any, because nothing had ever asked. The route that WORKS is `open-family-for-editing` → `write-family-size-table` → the owner clicks Load into Project

---
