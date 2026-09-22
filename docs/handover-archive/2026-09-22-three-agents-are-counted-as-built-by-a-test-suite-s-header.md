# Session note — THREE AGENTS ARE COUNTED AS BUILT BY A TEST SUITE'S HEADER

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THREE AGENTS ARE COUNTED AS BUILT BY A TEST SUITE'S HEADER

**[Row 5b-145](../FRAGMENT-ISSUES.md) RAISED — OPEN, and it is the owner's.**
`tools/generate-contract-reference.py` read end to end — 718 lines — and **the tool is SOUND**. It is
marked `clean` in the ledger. What follows is what it correctly **reports**, on every run, and nothing
records.

#### The disagreement, measured

| tool | what it says |
|---|---|
| `generate-contract-reference.py` | *"**A SUITE IS NOT THE AGENT.** tests/ files carry the same `Heron-Agent` header — which is right, it is how a suite says what it proves — … It is just not read for what the agent does."* |
| `agent-count.py`, **which owns the count** | lists `"tests"` in `SOURCE_ROOTS` and makes no distinction — any file claiming an agent makes it **BUILT** |

**The three**, confirmed by calling `agent-count.built()` directly:

| agent | only claimant |
|---|---|
| `HERON-DEV-INT-012` | `tests/test_bridge_roundtrip.py` |
| `HERON-RAG-DUP-012` | `tests/test_maintenance.py` |
| `HERON-RAG-RIX-011` | `tests/test_maintenance.py` |

**What it moves.** `agent-count` reports `0 outstanding`, `docs/28` publishes **Totals: 250 agents**,
and `balance-of-work` row 3 reads **0 of 250 agents left to build**. If a suite-only claim is not a
build, that zero is wrong by three.

**One of the three is already yours and is not new**: `DEV-INT-012` is in `DECISIONS.md`'s F27 table,
which closes two of four and says *"the other two are unchanged and are the owner's"*. **The other two
are recorded nowhere as a defect** — the RAG plan notes say `RAG-RIX-011` is *"Re-index on change. The
hashing exists; **nothing fires it**"*, both marked for Stage 6.

**This is [row 5b-95](../FRAGMENT-ISSUES.md)'s shape a second time.** That row exists because this same
page *"already reports these on every run and nothing records them, so they are re-discovered and never
closed"*. This is the other half of the same page's output, and it had the same fate.

**Raised, not fixed, and it is a definition.** Does a claim by a suite alone count as built? Changing
`agent-count`'s `SOURCE_ROOTS` would move a figure in `docs/28`, in `agent-count`'s own reconciliation
and in the balance page. **You write the sentence; the two tools then follow it.**

#### What was measured and found right, so nobody re-reads it

- The page's claim table **agrees exactly with `agent-count`** — 239 each, nothing in one and not the
  other, in either direction. That is the tool's own standard, since its comment says agent-count owns
  the count.
- `claims()` reads the first **2000 characters** where `agent-count` reads the first **40 lines**. Not
  a live difference: the latest any `Heron-Agent` line starts in a claiming file is **47 characters
  in**, a margin of 1953.
- No file carries two `Heron-Agent` lines inside that window, so `dict(HEADER.findall(...))` keeping
  the last one cannot bite today.
- `tests/test_contract_reference.py` already holds the conclusion properly — six claims, both
  directions, the docstring distinction, and that every finding on the page is real.

**Run into a scratch path**, never over the committed page: 127 contracts, 1460 fields, 659 declared
refusals, 112 built without a contract, exit 0.
