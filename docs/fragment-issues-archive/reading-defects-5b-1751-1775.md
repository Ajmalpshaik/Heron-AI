# FRAGMENT-ISSUES archive — section 5b, rows 1751 to 1775

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
> Rows 1751 to 1775 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1752

*Moved from the register on 2026-11-17.*

**Defect.** **`tools/split-needs-checking.py` REFUSES TO PLAN A NEW GROUP, BECAUSE IT CANNOT READ BACK GROUP BV - A GROUP THE NEW ONE DOES NOT TOUCH.** Found 2026-11-16 adding [Group OB](../needs-checking/group-ob.md) for [D-166](../decisions/D-166.md) the way the register's page says - written on the page in full, then the tool. Its dry run raised `RegisterBroken`: *"needs-checking/group-bv.md does not open its first section with "## Proven 2026-10-18 (Revit 2020, session 64176, the owner's scratch families Family3 and Family4)", the heading that names it"*. With nothing new on the page, the same dry run plans nothing and exits 0, so the fault is in how the plan reads back a page that gained a group - most likely a group file whose first `## ` is not the group's own heading, which [row 5b-363](../fragment-issues/section-5b-rows-351-375.md) says the tool can rewrite. Not looked into further: recorded, not fixed, because the change that found it is about something else.

**State.** **FIXED 2026-11-16** - the same cause as [row 5b-1583](../fragment-issues/row-5b-1583.md), fixed there: a later section of a group file now goes to the file just above it when that file holds its heading, so group BV's *Proven 2026-10-18 ...* is no longer read as another group's. Measured on a copy of the tree with a trial group added to the page: `python tools/split-needs-checking.py` raised `RegisterBroken` on the old code, and with the fix plans *to move: 1* and reads back byte for byte. Was: **OPEN.** Group OB (Group OA until main took those letters) was written by hand in the tool's shape, as [F44](../proposals/f44.md) was for the proposals register, and `python tools/needs-checking-register.py` reads its five rows. Whoever next adds a group meets this first.

---
