# Session note — (latest) — NINETY BRAIN MODULES NOBODY CALLS, AND THE FIRST OPEN ROW IN 5b

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (latest) — NINETY BRAIN MODULES NOBODY CALLS, AND THE FIRST OPEN ROW IN 5b

**[Row 5b-83](../FRAGMENT-ISSUES.md). OPEN, and open on purpose.** Nothing was changed and no module was
wired to anything.

**Measured**: `python tools/module-reach.py` — new, and shipped with the row so the number is a
command rather than a cell. Of **145 modules in `brain/`**: **53** are reachable from `mcp/` or from
another brain module, **2** are reached only by a tool, **90 are imported by nothing but their own
test**, and **0** by nothing at all.

**Why that is a row.** [`mcp/README.md`](../../mcp/README.md) describes the state before
`heron_brain.py` was built, in its own words: the transport-only rule *"was being kept by **having no
route at all**: eight brain modules, seven fragments and ten skills, imported by nothing but their
own tests, and therefore **unreachable from any conversation**"*. One named seam was built so the
rule could hold without that, and it works — [row 5b-82](../FRAGMENT-ISSUES.md) measured ten MCP tools
standing on it. **The number that seam was built to fix has gone from eight to ninety.** And nothing
dispatches dynamically, so the count is not an artefact of the method: the three `importlib` uses
outside tests each load one named file.

**WHAT IS DELIBERATELY NOT CLAIMED: THAT ANY OF IT IS WRONG.** An agent may be a vocabulary for who
owns what rather than a runtime actor, and a library waiting for its caller is not dead code.
Settling that needs [18](../18-agent-operating-system.md) and [28](../28-agent-registry.md) read properly
and **neither has been**. [Row 5b-75](../FRAGMENT-ISSUES.md) is what it costs to decide a question like
this on half the sources — four modules changed to match a document whose own header called itself a
proposal, all four reverted.

**Three questions for the sitting**, in the order that decides the rest. **(1)** Is a brain module
meant to be reachable at runtime at all, or is `brain/` a library the fragment and capability layers
draw on? **(2)** If they are meant to be reachable, what is the route — a runner, the way
[row 141](../FRAGMENT-ISSUES.md) says skills have none, or more seams like `heron_brain.py`? **(3)**
Which of the ninety are load-bearing today, because `heron_safemode` and `heron_rollback` being
uncalled means something different from `heron_onboarding` being uncalled.

**253 register rows, 33 open** — the count moved for the first time in this session, and it moved
because this one cannot be settled by reading.
