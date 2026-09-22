# Session note — (last of the day) — THE MORE PEOPLE ASKED, THE LESS THE GAP REPORT COULD SAY

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (last of the day) — THE MORE PEOPLE ASKED, THE LESS THE GAP REPORT COULD SAY

**[Row 5b-94](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_capability.py`, the **tenth** live-path brain
module — and the first file finished because the ledger's new column said what was owed. Its earlier
mark listed `rebuild`, the gap report and `main` as **not read**, so that is where the reading started
and that is where the defect was. **[Row 5b-90](../FRAGMENT-ISSUES.md) paying for itself within the
hour.**

`want(store, name, why)` did `INSERT ... ON CONFLICT(name) DO UPDATE SET why = ?`. **The last writer
won.** Measured: three different things wanting `TRACE_DUCT_SYSTEM` — skill `trace-system`, skill
`size-check`, and a user asking by name — left **one** sentence.

**And the one that survives in production is the generic one.** `heron_brain.resolve()` writes *"asked
for by name and no fragment provides it"* every time somebody asks for a capability nobody provides,
so **one person asking erases which skills were blocked**. `tests/test_skills.py` loops over every
skill calling `want()`, so two skills needing one capability already lose one of the two.

**The more people ask, the less the report can say about who needs it** — backwards for a function
whose docstring says it *"turns 'we have no fragment for that' from a silence into a finding"*.

**FIXED**: the table still holds **one row per capability** (docs/18: the gap IS the name), and the
reasons share the cell, joined by `WHY_JOIN`, with `wanted_by()` reading them back in arrival order.
**De-duplication is not cosmetic** — the common caller is a loop over every skill, which would
otherwise grow the cell on every run. An empty `why` records *wanted, with no reason recorded* rather
than blanking somebody else's.

**Shown to fail: 3 checks**, and the fourth — that a repeat does not grow the cell — stays green in
both. **The section asked before it called**, so it reported failures rather than a traceback:
`heron-ship` §2a for the second row running.

**Live-path brain modules read: 10 of 53.** Next: `heron_context`, then `heron_retrieve`.

**A habit worth keeping, and it is the session's own finding twice over.** Both of the last two rows
came from *finishing* something rather than starting it — 5b-93 from the reader half of the trail
5b-91 had just tested, 5b-94 from the part of a file an earlier read had left. **The ledger now says
which files are in that state**, and it is the most productive queue in the repository right now.
