<!--
Heron-Agent:  none
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 33 — The external repository research matrix

**The last named gap in [32](32-master-architecture-reconciliation.md).**
[`HERON_AI_MASTER_ARCHITECTURE.md`](../HERON_AI_MASTER_ARCHITECTURE.md) §10 and §11 list fifteen
projects to study, and §17 Phase 2 asks for the result as a matrix. This is that matrix, read on
2026-09-09.

**Every row was read from the project's own page, not from memory.** Where the incoming document said
*"first verify the exact repository intended"*, the repository was identified before anything was
written about it — and one of them **could not be**, which is recorded rather than guessed at.

**Read [32 §1](32-master-architecture-reconciliation.md) before acting on any row.** These are research
inputs for a **BIM-modeller-facing platform**, not for a developer-assist harness. Several of the
projects below are the second thing, and the useful part of them is the mechanism, never the shape.

---

## 1. The most valuable finding is that four of them agree with Heron

This exercise was expected to produce ideas to adopt. What it mostly produced is **independent
confirmation**, and that is worth more than an adoption, because it is evidence about a design that is
already built rather than a plan for one that is not.

| Project | Arrived independently at | Heron already has |
|---|---|---|
| **agentmemory** | keyword (BM25) + vector + graph, **fused by Reciprocal Rank Fusion** | [`heron_retrieve.py`](../brain/heron_retrieve.py) — FTS5 and nearness fused by RRF, [D-40](DECISIONS.md)'s derived graph beside it. **The same three routes and the same fusion**, chosen here from [05 §4](05-heron-brain.md) |
| **alibaba/open-code-review** | *"hybrid architecture: **deterministic pipelines + LLM Agent**"* | [19 §5](19-context-and-cost.md) and [02 §6](02-architecture-overview.md): *the common case must be deterministic. The model is for the uncommon case.* Enforced as pipeline order, not as an optimisation |
| **Headroom** | compresses **tool outputs, logs, files and RAG chunks** — not the user's question | the rule written into [`heron_context.py`](../brain/heron_context.py) on the same day, for [05 §4](05-heron-brain.md)'s reason: compression may touch retrieved parts and **never** the request, because `OST_DuctCurves` is the load-bearing half of a BIM sentence |
| **code-review-graph** | SQLite, **incremental by content hash**, and *"blast radius"* — what a change reaches | [`heron_embed.py`](../brain/heron_embed.py) is content-hashed so re-indexing unchanged files costs nothing; [`heron_graph.py`](../brain/heron_graph.py) answers *"what breaks if this changes"*; [D-40](DECISIONS.md) derives before storing |

**Four projects, four different problems, four teams that did not talk to each other.** None of this
makes Heron right, and it is not treated as evidence that it works — nothing here has met a Revit model.
What it does mean is that the four designs most likely to be wrong by being unusual are not unusual.

---

## 2. The matrix

Decisions use the incoming document's own vocabulary: **Adopt concept · Adapt concept · Reject ·
Research further.**

| Repository | Problem solved | Useful principle | Heron fit | Risks | Licence | Decision |
|---|---|---|---|---|---|---|
| [`rohitg00/agentmemory`](https://github.com/rohitg00/agentmemory) | agent amnesia between sessions | **four memory tiers** — working, episodic, semantic, procedural — and hybrid retrieval fused by RRF | retrieval is **already this**. The four tiers are a sharper cut than [10](10-memory-and-knowledge.md)'s categories and worth comparing against them | its value is in the tiering, which is a re-organisation of something Heron already has and would cost a migration | Apache-2.0 — compatible | **Adapt concept** — compare the four tiers against [10](10-memory-and-knowledge.md)'s list, adopt only a difference that names a real gap |
| [`tirth8205/code-review-graph`](https://github.com/tirth8205/code-review-graph) | whole-repository context sent to a model for review | **blast-radius analysis** before loading anything; Tree-sitter → SQLite; incremental by SHA-256 | the *principle* is [D-40](DECISIONS.md) and [`heron_graph.py`](../brain/heron_graph.py). The *implementation* is a source-code graph, which [32 §5](32-master-architecture-reconciliation.md) rejects for Heron | adopting the tool means adopting Tree-sitter and a code graph over `revit/` — the off-mission build | MIT — compatible | **Reject the implementation, principle already held** |
| [`headroomlabs-ai/headroom`](https://github.com/headroomlabs-ai/headroom) | token cost of tool output and RAG chunks | compress **what came back**, never what was asked; reversible compression so the original can be retrieved | this is the missing half of [19 §2](19-context-and-cost.md). [`heron_context.py`](../brain/heron_context.py) declares the boundary and implements no compression | a compressor that ever touches a Revit token destroys the only load-bearing part of the sentence | MIT — compatible | **Research further** — the reversible-retrieval idea is the one worth taking; nothing before [19 §2](19-context-and-cost.md)'s budgets are agreed |
| [`alibaba/open-code-review`](https://github.com/alibaba/open-code-review) | code review at scale | **deterministic pipeline first, agent second**; line-level findings; a built-in ruleset | the split is [19 §5](19-context-and-cost.md)'s. The line-level shape is what [`check-revit-gate.py`](../tools/check-revit-gate.py) does per question | its rulesets are NPE, thread-safety, XSS, SQL injection — a web/Java surface. Heron's ruleset is the Revit API, and none of theirs transfers | Read the repository's own licence before any reuse | **Adopt concept** — already held; the ruleset itself does not transfer |
| [`alibaba/aacr-bench`](https://github.com/alibaba/aacr-bench) | no way to measure a review agent | a **benchmark with expert-verified answers** for repository-level review | [18](18-agent-operating-system.md) and the incoming §18 both ask for an evaluation suite and Heron has none. This is the shape one takes | its dataset is general code. A Heron benchmark has to be Revit tasks, which only the owner can author | Read before any reuse | **Adapt concept** — the *shape* of a benchmark, never its cases |
| [`volcengine/OpenViking`](https://github.com/volcengine/OpenViking) | context organisation for agents | **tiered loading** — L0 abstract, L1 overview, L2 details, loaded only as needed; observable retrieval paths | the tiering is a real addition to [`heron_context.py`](../brain/heron_context.py)'s parts, and *observable retrieval* is already there as `Part.source` | **the main project is AGPLv3** — see §3. Idea only, and the idea must be re-derived rather than read from its code | **AGPLv3** (CLI and examples Apache-2.0) | **Adapt concept, code strictly off-limits** |
| [`karpathy/llm-council`](https://github.com/karpathy/llm-council) | one model reviewing its own work | first opinions → **anonymised** peer review → a Chairman synthesises | [D-39](DECISIONS.md) already requires an *analysed disagreement*. **Anonymising which agent produced which answer is a genuine sharpening** of Shadow Mode | it is N model calls per question, which [19 §5](19-context-and-cost.md) exists to avoid. Only for the high-risk path the incoming §6.9 describes | MIT — compatible | **Adapt concept** — take the anonymising, not the council |
| [`obra/superpowers`](https://github.com/obra/superpowers) | agents coding before understanding | brainstorm → design → plan → execute, with review checkpoints; skills as the unit | the discipline is [27](27-build-order.md)'s and this repository's practice | its skills are general software workflows; Heron's name **capabilities**, deliberately ([09](09-skills-and-fragments.md)) | MIT — compatible | **Adopt concept** — already held |
| [`garrytan/gstack`](https://github.com/garrytan/gstack) | solo developer without a team | a seven-stage cycle and 23 named specialist roles | [28](28-agent-registry.md) already defines 250 agents by department | adding 23 more roles creates a **second registry**, which is [32 §5](32-master-architecture-reconciliation.md)'s rejection of the incoming §6.7 repeated | MIT — compatible | **Reject** |
| [`affaan-m/ECC`](https://github.com/affaan-m/ECC) | plans lost in chat, standards forgotten | **the plan as an editable artifact** rather than chat history; hooks enforcing standards outside the model's context | the artifact idea is [23](23-heron-kernel.md)'s checkpoints and [`heron_workflow.py`](../mcp/server/heron_workflow.py). Enforcement outside the model's context is what `tools/check-*.py` are | 68 agents, 284 skills, 94 commands. The scale IS the thing being rejected — [D-01](DECISIONS.md) gives the host the commands and [09](09-skills-and-fragments.md) gives skills capabilities | MIT — compatible | **Adopt the artifact principle, reject the scale** |
| [`ruvnet/ruflo`](https://github.com/ruvnet/ruflo) | orchestrating many agents | swarm coordination; **Raft, Byzantine and Gossip consensus**; 100+ agents | almost none. Heron has **one Revit, one pipe, one queue, one handler** ([D-09](DECISIONS.md)) | consensus answers *"which of my disagreeing replicas is right"*. Heron has no replicas. The incoming §10.2 says it itself: *avoid unnecessary agent complexity* | MIT — compatible | **Reject** |
| [`thedotmack/claude-mem`](https://github.com/thedotmack/claude-mem) | context lost at compaction | capture the session, **compress it**, inject relevant context next time | the *lifecycle* is [10](10-memory-and-knowledge.md)'s. The *capture everything* half is the opposite of Heron's rule | it captures whole sessions and stores them in ChromaDB, and part of it is a paid subscription. [D-24](DECISIONS.md) and [D-26](DECISIONS.md) require local, free, offline and no account | Apache-2.0 — compatible | **Reject the mechanism**; the selective-memory principle is already held |
| [`PrimeIntellect-ai/prime-agent`](https://github.com/PrimeIntellect-ai/prime-agent) | long-running autonomous work | **bounded autonomous mode** — explicit token and time budgets; sessions that survive a disconnect | the budget idea belongs beside [19 §2](19-context-and-cost.md)'s. Session survival is [23](23-heron-kernel.md)'s checkpoints | its bounds are for unattended running. Heron's [Golden Rule 9](14-golden-rules.md) puts a person in front of a high-risk action instead | MIT — compatible | **Research further** — only the *bound*, and only once [19 §2](19-context-and-cost.md)'s budgets are agreed |
| [`K-Dense-AI/scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills) | domain skills scattered across documentation | a **domain skill library** with per-skill metadata and host auto-discovery | the shape is [`brain/skills/`](../brain/skills/) and [09](09-skills-and-fragments.md) | **individual skills carry their own licences**, which the repository says explicitly. A library whose entries are separately licensed is a supply-chain question, not a reading question | MIT for the repository; **per-skill otherwise** | **Adopt concept** — already held. Its licence structure is a warning worth carrying |
| [`ai-boost/awesome-harness-engineering`](https://github.com/ai-boost/awesome-harness-engineering) | no map of the field | an index, and the incoming §10.10 is right that it is one | a reading list for whoever answers `Q-45` and the compression question | an index is not a dependency, and treating it as one is how a list becomes a roadmap | CC0 | **Research further**, as an index only |
| *Claude CEO / CEO-style agent* (§10.15) | — | — | — | — | — | **NOT IDENTIFIED.** The incoming document says *"first identify the exact repository intended… study only if verified"*, and it could not be. **Nothing was studied and nothing is claimed.** [D-01](DECISIONS.md) already gives Heron the orchestrator-in-the-host pattern this row was reaching for |

---

## 3. The licence finding, which is the one that could have cost something

**[`volcengine/OpenViking`](https://github.com/volcengine/OpenViking)'s main project is AGPLv3.**
Heron is **Apache 2.0** ([D-08](DECISIONS.md)) and is going public ([17](17-open-source-and-distribution.md)).

AGPL is copyleft and network-triggered. Copying code from it into Heron would place an obligation on
Heron that Apache 2.0 cannot carry, and the incoming document's own §2 names the failure mode exactly:

> code reuse must follow its license and **must never happen accidentally**.

**It is the most interesting project in the list and the one whose code must never be read for
transcription.** Its tiered-loading idea is taken as an idea — L0/L1/L2, restated in Heron's terms
against [19 §2](19-context-and-cost.md)'s parts budget — and nothing else.

That is [D-25](DECISIONS.md)'s rule, which this repository already applies to the owner's own earlier
library: **studied and re-authored, never imported.** [31](31-studying-the-existing-libraries.md) is the
method, and it applies to an outside project with more force than to one's own.

**Two more licence notes worth carrying:**

- **`scientific-agent-skills` is MIT, but its individual skills are not necessarily.** A library whose
  entries carry their own licences is a supply-chain question. [17](17-open-source-and-distribution.md)
  and [Golden Rule 12](14-golden-rules.md) already say private knowledge is never published
  automatically; nothing yet says what happens when *imported* knowledge carries a licence Heron's own
  users then redistribute.
- **`claude-mem` has a paid tier.** Not a licence problem, and it is worth naming beside
  [D-24](DECISIONS.md): a memory system that stops working when a subscription lapses is the opposite
  of *re-indexing has to be free or it stops happening*.

---

## 4. What actually comes out of this, ranked

1. **Nothing is adopted as code.** Not one line, from any of the fifteen. [D-25](DECISIONS.md).
2. **Anonymised peer review** ([`llm-council`](https://github.com/karpathy/llm-council)) is the single
   sharpest new idea, and it is small: when Shadow Mode compares a candidate against production, hide
   which is which from whatever judges them. [D-39](DECISIONS.md) already demands an *analysed*
   disagreement; anonymising removes the one bias that analysis cannot see.
3. **Tiered loading** (OpenViking, idea only) is the second, and it belongs to
   [`heron_context.py`](../brain/heron_context.py): a part could carry an abstract, an overview and a
   full body, and a path's budget could name the tier rather than only the part.
4. **A benchmark's shape** (`aacr-bench`) is the third, and it is the one with a hard prerequisite —
   the cases must be real Revit tasks, and only the owner can author them.
5. **Everything else is already here, correctly, or is rejected with a reason.** Nine of the fifteen
   needed no action at all.

**None of this goes in front of the proving pass.** 218 fragments have never met a model, that is the
critical path, and every row above runs on any machine at any time.
