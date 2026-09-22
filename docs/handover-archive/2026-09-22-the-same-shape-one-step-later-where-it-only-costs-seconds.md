# Session note — THE SAME SHAPE ONE STEP LATER, WHERE IT ONLY COSTS SECONDS

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE SAME SHAPE ONE STEP LATER, WHERE IT ONLY COSTS SECONDS

**[Row 5b-120](../FRAGMENT-ISSUES.md), FIXED.** `brain/heron_rag_init.py` read end to end — 245 lines,
2 public functions, **one suite**, nothing imports it.

`plan()` normalises the scopes it is asked about — `str(entry).strip().lower()` — then looks the
`indexes` dict up with that normalised key **against whatever the caller happened to type**, while
nothing deduplicates the scope list.

**Measured:**

| | |
|---|---|
| `plan(["global", "global"])` | **2 to build**, for one index |
| `indexes={"GLOBAL": …}` | **`no index yet`**, about an index that is there |
| `indexes={" global": …}` | **`no index yet`** |

**`HERON-INS-BRN-007` is one install step earlier and answers both**, in its own words: *"THE SAME
STORE ASKED FOR TWICE IS ONE STORE, requested twice — not a conflict and not two creates. A plan that
listed it twice would have an installer create it, then create it again over what it just made."*

**The damage here is bounded, and that is exactly why it was worth reading twice.** An index is
**derived**, so a needless rebuild costs seconds rather than a year of project memory —
[row 5b-119](../FRAGMENT-ISSUES.md) is the same shape one step earlier, where it costs the memory. **What
is wrong here is the REPORT**: a count that says two about one index, and a reason — *"no index yet"*
— that is simply untrue about the machine it describes.

Both of `heron_brain_init`'s answers are **borrowed rather than invented**, which is this
repository's rule about a second copy applied to a behaviour instead of a constant.

```bash
python tests/test_rag_init.py      # section 6b, 4 red against the module as found
```

**Nothing about the rebuild rule moved**: an index built by another backend is still rebuilt, one
recording no backend is still rebuilt, and rebuilding is still the right answer here and the wrong
one one step earlier.

**The central idea is right, and it is the one worth knowing**: steps 11 and 12 are adjacent and have
**opposite** answers to *may I rebuild this?* — and the answer comes from which **class** the artefact
is in, not from a policy either agent applies. Rebuild the knowledge store and a year of memory is
gone; refuse to rebuild the index and Heron stays on a stale one forever.

**And the trap is handled**: an index is only meaningful to the backend that built it, so the backend
is **recorded**; an index whose recorded backend is not the one configured now is rebuilt rather than
read; and an index recording **no** backend is in the same position, because nobody can say what built
it. Which backend is active is **asked for**, never read here — a value a caller states is a caller
that can make Heron record an index as built by something that never ran.
