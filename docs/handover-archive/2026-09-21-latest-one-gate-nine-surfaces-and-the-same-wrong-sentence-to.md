# Session note — (latest) — ONE GATE, NINE SURFACES, AND THE SAME WRONG SENTENCE TO ALL OF THEM

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (latest) — ONE GATE, NINE SURFACES, AND THE SAME WRONG SENTENCE TO ALL OF THEM

**[Row 5b-89](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_flags.py`, the **seventh** live-path brain
module. `origin_allowed()` is Golden Rule 19's gate — *"permission comes from the user, through
Heron's own UI, per action"* — and it fails closed correctly on everything that is not the user.

**It is not the flag agent's private check.** An AST walk over `brain` and `mcp` finds it called in
**nine** places: `heron_configuration`, `heron_update`, `heron_dependencies`, `heron_brain_init`,
`heron_rag_init`, `heron_tooling`, `heron_safemode` and `mcp/server/heron_register` all borrow it
rather than keeping a second copy. That is right, and `tests/test_safemode.py` §6 says so in as many
words.

**What came back with it was a sentence about flipping a flag.** Run end to end, entering Safe Mode
from a document is refused with *"...and a flag flip is the shortest path from a sentence somebody
else wrote to a write in a live model"*. Nothing was flipping a flag. All eight borrowers write a
correct `proposal` naming their own act and then hand the reader a `why` about somebody else's — and
one of the nine is on the MCP side, so the wrong sentence reaches a modeller's conversation.

**And the suites checked the half that was right**: both assert the refusal CODE and the words *"data,
never instruction"*, which is the ORIGIN half and was correct throughout. Nothing read the rest of the
sentence. That is [rows 5b-80 and 5b-85](../FRAGMENT-ISSUES.md) again.

**FIXED**: `origin_allowed(origin, action=None)`, with nine call sites naming their own act. The
default is `an ADMIN action` — **vague rather than wrong**, because naming the wrong act sends a
reader off to check something they were never doing. Behaviour untouched. **The check that stops it
returning is derived**: `tests/test_flags.py` §3b walks both trees with `ast` and asserts every caller
names its act, so a new ADMIN surface that forgets goes red the day it is added. **Shown to fail: 13
checks** across the two suites.

**THE FIRST DRAFT CRASHED INSTEAD OF FAILING — THE THIRD TIME TODAY.** Calling the two-argument form
against the old module raised `TypeError` and sections 4 to 8 never ran. It asks `__code__.co_argcount`
first now. Rows [5b-85](../FRAGMENT-ISSUES.md), [5b-88](../FRAGMENT-ISSUES.md) and this one are the same
lesson: **a check written against a name or a signature the module may not have must ASK before it
calls.** Three in one day is a habit, not an accident.

**RECORDED, NOT FIXED — two for the next session.**

1. **`python tools/review-ledger.py` says *read 142 of 1180*, and 30 of those 142 were only PARTLY
   read.** The word `PARTIAL` lives in a free-text note, so the headline cannot tell a full read from
   a partial one, and `AGENTS.md` sends people to that number for exactly this question. A count
   derived from prose is guessed, not derived. **Not done in that change**; it needed the 30
   re-marked, and widening it would have stopped it being reviewable. **DONE in the next one — see
   [row 5b-90](../FRAGMENT-ISSUES.md) below**, and it is a seventh column rather than a third verdict,
   because 19 of the 30 also carry a defect row.
2. **`brain/heron_audit.record()` says NEVER FATAL in capitals and lets three exceptions out.** Its
   `numbers` loop does `int(value)` **outside** the `try`, so `ms="fast"` raises `ValueError`,
   `float("nan")` raises `ValueError` and `float("inf")` raises `OverflowError` — which is not even in
   the except list. **Not reachable today**: every live caller passes `len(...)` or a `rowcount`. It is
   a hardening, and the early `return False` when there is nowhere to write means it can only ever
   fire on a machine where the trail really writes.

**Live-path brain modules read: 7 of 53.** Next: `heron_gaps`, `heron_audit` (see above), then
`heron_capability`.
