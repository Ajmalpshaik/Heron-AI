# FRAGMENT-ISSUES archive — section 5b, rows 1176 to 1200

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
> Rows 1176 to 1200 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1176

*Moved from the register on 2026-11-17.*

**Defect.** **[D-138](../decisions/D-138.md) SAYS TWO THINGS ABOUT A PUBLISH WITH *ASK TO KEEP* OFF.** Found 2026-10-18 by the second docs pass. Its item 5: a publish is *"Still asked every time, grant or not"*. Its *What it accepts*: with Ask to Keep off *"A publish is then done without asking too"*. The code follows the second - `UnderTheGrant` in `RevitFragment.cs` shows no box when neither asking nor Ask to Keep is on - and [02](../02-architecture-overview.md) and [03](../03-heron-revit.md) were corrected to it on 2026-10-18. Decisions are append-only, so item 5's words stay.

**State.** **MOVED TO ISSUE [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the owner's.** A short decision that says item 5 reads *"while Ask to Keep is on"*, or a change to the code if item 5 is what was meant.
