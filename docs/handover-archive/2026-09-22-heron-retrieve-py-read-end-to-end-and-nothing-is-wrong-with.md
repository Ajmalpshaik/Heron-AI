# Session note — `heron_retrieve.py` READ END TO END AND NOTHING IS WRONG WITH IT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — `heron_retrieve.py` READ END TO END AND NOTHING IS WRONG WITH IT

**A NEGATIVE RESULT, written down so nobody reads it a third time.** 1,339 lines, completing the two
part-reads of 2026-09-21 and 2026-09-22. **No register row came out of it, and that is the finding.**

**Every live-path module on the `mcp/` side has now been read in full.** The measurement that picked
targets — suites reaching a module against its public surface — has nothing part-read left on it.

**Derived claims checked by RUNNING them**, not by reading:

| the file says | measured |
|---|---|
| one rank of fusion is `1/(K+1) - 1/(K+2)` at K=60 | **0.00026441** |
| the quality nudge spans *"about six tenths of one rank"* | **0.605** |
| status *"settles a dead heat and cannot move a fragment past one the routes ranked higher"* | largest nudge **0.454** of a rank |
| `OFFERABLE` excludes `DEPRECATED` and `ARCHIVED` | it does |

### Three things checked and dismissed, because a near miss is worth writing down

**1. `librarian()` wraps `SCOPE.open_scope` in a bare `except Exception`** — which is
[row 5b-109](../FRAGMENT-ISSUES.md)'s second half **by shape**. It is not the same defect: it does not
swallow. It records `skipped="could not be opened: …"`, and that field is carried all the way out —
printed by `heron_research` as `NOT ASKED - …`, returned by `heron_iso`, and present in **both** of
`heron_brain`'s payloads. **Traced to the seam rather than assumed.**

**2. `find_documents()` builds its `Contest` with a literal pool of `20`** while `find()` passes the
same variable it searched with. Two copies of one number, in the one function whose job is an honest
report — and this file argues against exactly that twice, *"BORROWED, NOT RE-TYPED"* and *"ONE
MEASUREMENT, NOT TWO"*. **Both are 20 today and nothing calls `documents()` with another pool, so no
sentence is wrong.** A hazard, not a defect, and a row here would be a finding made from a shape.

**3. `_until_filled_fragments` and `_until_filled_pairs` are byte-identical bodies** differing only by
`kind=EMBED.CHUNK`. Duplication, no behaviour difference.

**And one thing that looks like a gap and is right**: the `chunk_text` count in `find_documents` is
**not** wrapped, while the two queries around it narrow on *no such table*. That is correct — the
narrowing exists so a broken store cannot read as an empty one, and leaving this one bare lets a real
fault surface, which is what **D-52** asks for.
