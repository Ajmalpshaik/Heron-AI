# Session note — (final) — THE GAP REPORT TELLING A MODELLER HERON HAS NEVER DONE ANYTHING

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (final) — THE GAP REPORT TELLING A MODELLER HERON HAS NEVER DONE ANYTHING

**[Row 5b-93](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_gaps.py`, the **ninth** live-path brain
module and the reader half of the trail row 5b-91 just tested.

**`since(entries, days)` took a window with no floor.** Measured on a trail of three entries:
`--days 7` → 1, `--days 30` → 3, `--days 0` and `None` → 3, and **`--days -7` → 0**. `report()` then
prints *"The audit trail is empty. Nothing has been asked of Heron yet"*, and the MCP tool prints
*"Heron has no record of doing anything yet... nobody has asked it for anything"*.

**Both are false claims about somebody's own history, and the second is the one a modeller reads.**
`heron_gaps(days: int = 0)` declares an `int` with no lower bound and hands it straight down.

This is **`tests/test_brain_reachable.py` §7's own argument one tool along** — *"'Heron knows how to do
nothing' and 'Heron cannot read what it knows' send a user in opposite directions"* — and the module's
own `duration()` makes it about timings: it returns `None` rather than 0 because *"averaging the two
together is how a timing report starts lying."*

**And the suite pinned the half that was right.** `test_gaps.py` §7 empties the directory to prove the
message is correct for an empty trail. Nothing ever asked whether a **full** one could reach it.

**The second half is four lines away in `_stat()`.** `ordered[len(ordered) // 2]` takes the upper of
the middle pair, so **the median of two runs is the slower of them**: `_stat([10, 100])` returned
`(100, 100)`, and the report printed *median 100 ms, worst 100 ms* for a fragment that ran at 10 and
100. Two runs is the ordinary case for most of the library.

**FIXED**: `since()` raises `ValueError` below 1 — refused where the semantics live, not at each
caller, because there are two and the rule belongs to neither (row 5b-84's reasoning). The command
line says it in its own words and exits **2**. `heron_brain.gaps()` turns it into
`{"refused": "NOT_A_WINDOW", "why": ...}` and the MCP tool returns that sentence. **The refusal does
not come back looking like an answer** — no `found` key — because *an answer with nothing in it* is
the exact shape that read as *never been asked*. `_stat()` averages the middle pair for an even count.

**Shown to fail: 7 checks** across two suites, **and the three checks that a real window still answers
stay green in both**, which is how the pair proves the change was narrow.

**The new sections asked before they called** — a `try` round the refusal rather than naming a symbol
the old module has not got — so nothing crashed this time. That is
[`heron-ship`](../../.claude/skills/heron-ship/SKILL.md) §2a working the first time it was needed, one row
after it was written.

**Live-path brain modules read: 9 of 53.** Next: `heron_capability`, then `heron_context`.
