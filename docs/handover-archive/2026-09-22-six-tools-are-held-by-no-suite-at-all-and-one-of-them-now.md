# Session note — SIX TOOLS ARE HELD BY NO SUITE AT ALL, AND ONE OF THEM NOW HAS ONE

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — SIX TOOLS ARE HELD BY NO SUITE AT ALL, AND ONE OF THEM NOW HAS ONE

**A NEGATIVE RESULT plus the guard it was missing.** `tools/check-dependencies.py` is read end to end
— 262 lines, never opened before — and **nothing is wrong with it**. No register row.

**The part-read brain queue emptied, so `tools/` was re-measured**: 47 tools, and **six are named by
no suite at all**:

| |
|---|
| `measure-graph.py` · `check-dependencies.py` · `module-reach.py` |
| `generate-decision-summary.py` · `resign-machine-proofs.py` · `recount-agent-registry.py` |

`check-dependencies` was taken first because **R-72 makes four specific claims about it and nothing
re-checked any of them**:

> It exits 1 **only** on a missing REQUIRED package … It also fails on a malformed manifest entry,
> **proved by introducing both shapes and watching it exit 1**. Size is never typed.

**All seven behaviours measured by running it**, on manifests written for the purpose:

| the manifest | exit |
|---|---|
| well formed, one optional absent | **0**, *"that is not a fault"* |
| a missing REQUIRED package | **1**, *"Heron will not run"* |
| a requirement with no comment above it | **1** |
| a comment with two fields | **1** |
| a comment with four fields | **1** |
| a blank line between comment and requirement | **1** |
| the manifest file does not exist | **1**, *"does not exist"* |

And the size claim holds: `announced_size` reads **`500 MB to 2 GB - mostly torch, not the weights`**
out of `heron_rerank.announcement()`; that string appears **nowhere** in the tool's own source; and
nothing is invented for a package no code announces.

### What was missing was the guard, not the behaviour

*"Proved by introducing both shapes and watching it exit 1"* was done **once, by hand, on
2026-09-11**. That is a measurement, not a guard — the tool can drift away from it on any afternoon
and nothing says so.

**`tests/test_check_dependencies.py` is the first suite this tool has ever had**, and it has teeth —
shown to catch three breaks:

| what was broken | red |
|---|---|
| a broken manifest no longer exits 1 | **5** |
| an absent OPTIONAL package exits 1 | **1** |
| the size typed into the tool instead of read | **1** |

**Every case builds its own manifests.** A suite reading the real `requirements.txt` would be
asserting what happens to be installed on the machine running it — a different question, and one that
changes without anybody editing anything.

**One limit stated rather than left to be discovered**: nothing here proves the manifests are
**right** — that the import name beside a pip name is the one the package installs — and a typo there
would report an installed package as MISSING. `check-dependencies` trusts the manifest by design, and
the suite says so.

**Also checked**: the work note `improvement-gate-execution-record.md` records that this tool *"has no
section in tools/README.md"*. **That has since been fixed** — the section is at line 1404 — so the
finding is closed rather than open. It is still **not in CI**, which is PROPOSALS F6 and the owner's.
