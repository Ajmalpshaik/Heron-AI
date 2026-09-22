# Session note — ALL SIX TOOLS NO SUITE NAMED ARE NOW HELD

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — ALL SIX TOOLS NO SUITE NAMED ARE NOW HELD

**[Row 5b-127](../FRAGMENT-ISSUES.md), FIXED.** `tools/measure-graph.py` read end to end — 394 lines,
never opened before. **The last of the six.**

It answers **Q-52** by running it, and the numbers it prints are in [docs/33 §5.16](../33-external-repository-research.md). Its settings were read by
hand out of `argv`, with an `in` test and an index.

**Measured by running it:**

| what was typed | what happened |
|---|---|
| `--wieght 0.05 --sedes 1` | the **full measurement ran**, printed `settings weight=0.30 seeds=5`, exited **0** |
| `--weight` | `IndexError: list index out of range` |
| `--weight abc` | `ValueError: could not convert string to float` |
| `--seeds 2.5` | `ValueError: invalid literal for int()` |
| `--sweep --weight 0.1` | the weight silently ignored — the sweep has its own list of six |

**A number recorded against a setting nobody asked for is worse than no number, because it is
quoted afterwards** — and the whole point of this tool is that Q-52 was *run* rather than argued.
It is the same defect [rows 5b-112 and 5b-114](../FRAGMENT-ISSUES.md) fixed in `brain/`, in a tool,
where the cost is a recorded figure.

`settings_from()` now reads every flag **before the store is opened** and refuses with **2**,
naming what the tool does take — the house refusal `heron_company` and the rest already give.

### The rest of the file is a negative result, and the suite records it as one

All green **before the fix and after it**: the four query shapes and the two they decline to
repeat; `rank_of` returning `None` rather than a large rank for an answer that never came back;
the tally dividing MRR by everything asked, misses included; the empty-ranking path keeping its
two-value shape; and the fusion leaving the order alone at weight 0 while lifting a neighbour two
seeds reach.

**The suite never runs the measurement.** That opens the knowledge store, re-indexes it and asks
396 fragments four ways — minutes, and it writes outside the repository — so `main()` is called
only with argv it must refuse.

### The best thing in the file is its honesty, and it is worth knowing

The prediction is written down **before** the run: *"for a query whose right answer is ALREADY
first, a third stream can only leave it there or push it down … a tool that can only report good
news is not a measurement."* The answer key is the library's own `semantic-identity`, and the
docstring says plainly that it is **real and easy**, so three degraded query shapes are measured
beside the exact one. `--sweep` tries six settings, so a loss is an answer rather than a setting.
And one of the four *READ IT AS* endings is **"a TRADE, not a win"**, which hands the judgement to
the owner.

**Two Codex findings from PR #44 are still fixed and still explained in place**: the empty-ranking
return shape, and the answer key obeying the same version wall as retrieval.

### Run again on this container, and it reproduces

Lexical backend, 396 fragments. At `weight=0.30 seeds=5` the graph costs **10.6 points of P@1** on
the exact shape. At the gentlest setting asked for, `--weight 0.05 --seeds 3`, it still costs
**1.2** and returns nothing — *"the graph cost something and returned nothing"*. That is docs/33's
recorded finding **reproduced rather than assumed**.

**Not a D-30 proof**, and the tool says so itself: it measures whether the fragment whose own
sentence was typed comes back first, which is a proxy, and nothing here has met a real model.

### The sweep of the six, finished

| tool | outcome |
|---|---|
| `check-dependencies` | **sound** — a negative result, plus the guard it was missing |
| `resign-machine-proofs` | [5b-122](../FRAGMENT-ISSUES.md) |
| `recount-agent-registry` | [5b-123](../FRAGMENT-ISSUES.md) |
| `generate-decision-summary` | [5b-124](../FRAGMENT-ISSUES.md) fixed, [5b-125](../FRAGMENT-ISSUES.md) for the owner |
| `module-reach` | [5b-126](../FRAGMENT-ISSUES.md) |
| `measure-graph` | [5b-127](../FRAGMENT-ISSUES.md) |

**Next by the same measurement**: `tools/` is 47 tools and the six unheld ones are done, so the
next target is whichever tool the ledger shows as unread or stale — `python tools/review-ledger.py
--next 5`.
