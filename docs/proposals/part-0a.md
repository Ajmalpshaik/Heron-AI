# Proposals — Part 0a

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part 0a — What the Master Handover Baseline (Part 3) changed

Part 3 is a **consolidation**, not a third set of requirements. Roughly 90% of it restates Parts 1 and 2.
Three things in it are genuinely new, and one of them changes the constitution.

### 🔴 The Golden Rules were replaced — ten became fifteen

[Baseline §78](../00c-master-handover-baseline.md) supersedes [Part 1 §72](../00-master-specification.md).
This is the most consequential change in the document, because every other document cross-references
these rules by number.

**Five new official rules**, all of which were previously scattered as principles rather than rules:

| # | New rule | Previously |
|---|---|---|
| 11 | Vector DB is an index, not the canonical source of truth | Part 2 §77, a principle |
| 12 | No automatic external publishing of private knowledge | Part 1 §35, a workflow step — **and it absorbs the review-proposed rule 14** |
| 13 | No uncontrolled self-modification of production architecture | implied by Part 2 §10, never stated |
| 14 | Every important autonomous operation must be auditable | Part 1 §55, a subsystem |
| 15 | Platform modular enough to replace agents/skills/fragments without redesign | Part 2 §2, a requirement |

**Two rules also swapped position** — old rule 3 (never break a working implementation) is now rule 4,
narrowed to "Revit version"; old rule 4 (reuse proven fragments) is now rule 3, broadened to
"proven knowledge". And rule 8 shifted meaning: from *"background work should remain invisible"* to
*"background work must not interfere"* — from a visibility rule to a performance rule.

**All cross-references in this repository were remapped on 2026-08-27.** The review-proposed rules moved
to 16–19, and the proposed *"never publish on own initiative"* rule is now covered by official rule 12.
Full mapping in [14 — Golden Rules](../14-golden-rules.md).

### 🆕 Other genuinely new content in Part 3

| # | Item | Why it matters |
|---|---|---|
| **P9** | **§1 — "do not assume every architectural idea is already implemented; future capabilities must be explicitly marked"** | A governance instruction, and the right one. It is the same position as [D-00](../DECISIONS.md). Every document in this repository already separates *specified* from *decided* from *built*; this makes that separation an explicit requirement rather than a convention. |
| **P10** | **§42 — Reference Update Agent must verify imports, references, metadata, registry, documentation and relationships after any rename or move** | Stated as a hard requirement for the first time. *"No broken references should be introduced."* This is what makes the Auto Rename Agent safe — see [06 §7](../06-heron-platform.md). |
| **P11** | **§32 — Compatibility Agent explicitly *combines* the results of the Revit Version, Revit API, .NET and Dependency agents** | A cleaner decomposition than either earlier document. One agent per question, one agent to reconcile them. |
| **P12** | **§57 — "Agents should only receive the permissions they require"** | Least privilege, stated explicitly for the first time. Should become a required field in the agent registry, not a guideline. |
| **P13** | **§20 — Trust Evaluation added as a retrieval pipeline stage** | Adopted. See [20 §1](../20-knowledge-trust-and-conflict.md). |

### ⚠️ One thing in Part 3 to reject

**§20 moves metadata filtering *after* all three searches.** Parts 1 and 2 had it earlier in the
pipeline, which is correct. Filtering last means embedding and searching a corpus that is about to be
discarded — the most wasteful possible ordering, and it defers scope isolation
([Golden Rule 5](../14-golden-rules.md)) from query time to ranking time.

The adopted order keeps Part 3's Trust Evaluation stage and Parts 1–2's filter placement.
See [20 §1](../20-knowledge-trust-and-conflict.md).

### Still open after Parts 1 and 2

Unchanged. Neither part addresses:

**Revit API threading** · **undo** · **preview before modify** · **what data leaves the machine to a model provider** · **how a Revit test actually executes**.

Proposed Golden Rules 16–19 remain necessary. Part 3 strengthens the case for them.

---
