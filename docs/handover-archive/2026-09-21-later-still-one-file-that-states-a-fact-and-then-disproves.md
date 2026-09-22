# Session note — (later still) — ONE FILE THAT STATES A FACT AND THEN DISPROVES IT

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (later still) — ONE FILE THAT STATES A FACT AND THEN DISPROVES IT

**[Row 5b-98](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_context.py`, the **eleventh** live-path brain
module and **1,278 lines that had never been read**.

Its module header says *"A scope store holds `fragments` and `meta` and no clause table… so that path
raises."* **Measured: a scope store holds FOUR tables** — `chunks`, `documents`, `fragments`, `meta`
— because `heron_ingest.ensure_tables` creates the first two in that same store. And
`_standard_parts`, **690 lines further down in the same file**, already knows: its docstring opens
*"Before there was a clause store this path raised by name"* and implements R-45's narrowing in four
branches. **The function followed the code; the header did not.**

**The second half is what nothing held.** R-45 says the refusal must **narrow** as the store fills,
must never **soften** into an answer, and must **stop** once there is a clause to cite.
`tests/test_context.py` §3 tested **one** of the four states — the empty store. **A break that refused
for ever would have passed every check in this suite**, on the one path `docs/05 §8` says must carry a
citation or be a bug.

**All four states are reachable here**, measured on this Linux container with no Revit and no optional
dependency: ingest a markdown clause, `heron_search.index_chunks`, `heron_embed.index_chunks`, and the
route goes `empty` → `unindexed` → `documents`.

§9 now walks three of them end to end and is **last in the suite on purpose** — it fills the store.

**Proved by breaking it, twice.** Regression A (the refusal never stops): **4 red**. Regression B (it
softens and tells the wrong nothing): **2 red**.

**AND REGRESSION B WAS GREEN UNTIL THE CHECK WAS TIGHTENED.** It asked for `NOT INDEXED`, and the
*fallback* refusal appends `answer.note`, which says *ingested and NOT INDEXED* too — so the check
matched the **note** rather than the **branch** and passed while the branch was disabled. It asks for
`INGESTED BUT NOT INDEXED` now, the branch's own words, and asserts the other two refusals are absent.

> **A check that matches a neighbour's text is not checking its own branch.** Worth keeping beside the
> `heron-ship` §2a rules: *ask before you call*, **and** *match the thing itself, not what sits next
> to it*.

**And the first draft crashed instead of failing — the fifth time today.** The *it stops refusing*
step called `assemble` bare, and a refusal here is an **exception**, so the regression it exists for
ended the run on a traceback. Caught now, and the failure quotes what it still said. That is the rule
I wrote into `heron-ship` §2a this morning, and still had to learn again in the afternoon.

**Live-path brain modules read: 11 of 53** (this one PART — the docstring, the budget and depth
tables, the three exceptions, `assemble` and `_standard_parts`; the `Untrusted` screen, `Part`,
`Context`, `_generation_parts`, the tier splitters and `report` are **not** read).

**Three marks went STALE** — `.gitignore`, `Directory.Build.props` and `tools/deploy-addin.ps1`, all
changed by [#242](https://github.com/Ajmalpshaik/Heron-AI/pull/242). **Read and cleared in the same
sitting; see [row 5b-99](../FRAGMENT-ISSUES.md) below.**
