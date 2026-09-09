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

**Every row was first read from the project's own page, not from memory.** Where the incoming document
said *"first verify the exact repository intended"*, the repository was identified before anything was
written about it — and one of them **could not be**, which is recorded rather than guessed at.

**Then every repository was cloned and read at file level** — its source, its hooks, its rules, its
tests — because a landing page describes what a project means to be and only its files say what it is.
[§5](#5-the-file-level-pass-repository-by-repository) is that second pass, **complete: all fifteen
entries and sixteen repositories**, one at a time, each recorded with the commit it was read at so the
reading can be repeated or found stale. **Where the file-level pass contradicts the matrix, the matrix
row is corrected in place and the correction is named in §5** — a claim that quietly changes is worse
than one that was wrong out loud.

**It disagreed with the matrix in ten of the fifteen entries**, changed two decisions, corrected five
licence cells and three of this document's own claims, promoted two projects into §1, and found two
stale claims in Heron's own documents. [§4a](#4a-what-the-second-pass-changed-counted) is the tally.

**Read [32 §1](32-master-architecture-reconciliation.md) before acting on any row.** These are research
inputs for a **BIM-modeller-facing platform**, not for a developer-assist harness. Several of the
projects below are the second thing, and the useful part of them is the mechanism, never the shape.

---

## 1. The most valuable finding is that six of them agree with Heron

This exercise was expected to produce ideas to adopt. What it mostly produced is **independent
confirmation**, and that is worth more than an adoption, because it is evidence about a design that is
already built rather than a plan for one that is not.

| Project | Arrived independently at | Heron already has |
|---|---|---|
| **agentmemory** | keyword (BM25) + vector + graph, **fused by weighted Reciprocal Rank Fusion at `RRF_K = 60`** | [`heron_retrieve.py`](../brain/heron_retrieve.py) — FTS5 and nearness fused by weighted RRF, **at `RRF_K = 60`, chosen independently** from [05 §4](05-heron-brain.md). **Two streams, not three** — [§5.5](#55-rohitg00agentmemory) corrects this row: Heron's graph exists and is not one of them |
| **alibaba/open-code-review** | *"hybrid architecture: **deterministic pipelines + LLM Agent**"* | [19 §5](19-context-and-cost.md) and [02 §6](02-architecture-overview.md): *the common case must be deterministic. The model is for the uncommon case.* Enforced as pipeline order, not as an optimisation |
| **Headroom** — [§5.14](#514-headroomlabs-aiheadroom) | compresses **tool outputs, logs, files and RAG chunks** — not the user's question, **and locally** | the rule written into [`heron_context.py`](../brain/heron_context.py) on the same day, for [05 §4](05-heron-brain.md)'s reason: compression may touch retrieved parts and **never** the request, because `OST_DuctCurves` is the load-bearing half of a BIM sentence |
| **code-review-graph** | SQLite, **incremental by content hash**, and *"blast radius"* — what a change reaches | [`heron_embed.py`](../brain/heron_embed.py) is content-hashed so re-indexing unchanged files costs nothing; [`heron_graph.py`](../brain/heron_graph.py) answers *"what breaks if this changes"*; [D-40](DECISIONS.md) derives before storing |
| **code-review-graph**, again — [§5.6](#56-tirth8205code-review-graph) | *"a bare `result_count: 0` is ambiguous… it can mean **this graph cannot see that relationship**"* — and a marker attached **only** to the empty case | [D-52](DECISIONS.md) exactly, and `FILTER_ELEMENTS_BY_CATEGORY` reporting `unresolvedLevel` so a broken lookup reads as *"12 found, 12 with no level"*. **Found only at file level**, and it is direct evidence for [Q-46](OPEN-QUESTIONS.md) and [Q-48](OPEN-QUESTIONS.md) |
| **superpowers** — [§5.8](#58-obrasuperpowers) | *"NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE"*, and **a regression test that has only ever passed is not evidence** — the red-green cycle must have been seen | [D-30](DECISIONS.md)'s negative case, exactly. **Found only at file level**, and Heron's version is the stronger one: theirs is prose addressed to a model, Heron's is a proof *format* a tool checks |

**Six projects, six different problems, six teams that did not talk to each other.** None of this
makes Heron right, and it is not treated as evidence that it works — nothing here has met a Revit model.
What it does mean is that the six designs most likely to be wrong by being unusual are not unusual.

**The fifth and sixth were found only by the file-level pass.** The fifth is the one that matters most
today: it lands on [Q-46](OPEN-QUESTIONS.md) and [Q-48](OPEN-QUESTIONS.md), which are still unanswered.
The sixth is the one that matters most **next**, because it is the discipline the 218 DRAFT fragments are
waiting on.

---

## 2. The matrix

Decisions use the incoming document's own vocabulary: **Adopt concept · Adapt concept · Reject ·
Research further.**

| Repository | Problem solved | Useful principle | Heron fit | Risks | Licence | Decision |
|---|---|---|---|---|---|---|
| [`rohitg00/agentmemory`](https://github.com/rohitg00/agentmemory) — [§5.5](#55-rohitg00agentmemory) | agent amnesia between sessions | **four memory tiers** — working, episodic, semantic, procedural — and hybrid retrieval fused by RRF | retrieval is **already this**. The four tiers are a sharper cut than [10](10-memory-and-knowledge.md)'s categories and worth comparing against them | its value is in the tiering, which is a re-organisation of something Heron already has and would cost a migration | Apache-2.0 — compatible | **Adapt concept** — compare the four tiers against [10](10-memory-and-knowledge.md)'s list, adopt only a difference that names a real gap |
| [`tirth8205/code-review-graph`](https://github.com/tirth8205/code-review-graph) — [§5.6](#56-tirth8205code-review-graph) | whole-repository context sent to a model for review | **blast-radius analysis** before loading anything; Tree-sitter → SQLite; incremental by SHA-256 | the *principle* is [D-40](DECISIONS.md) and [`heron_graph.py`](../brain/heron_graph.py). The *implementation* is a source-code graph, which [32 §5](32-master-architecture-reconciliation.md) rejects for Heron | adopting the tool means adopting Tree-sitter and a code graph over `revit/` — the off-mission build | MIT — compatible | **Reject the implementation, principle already held** |
| [`headroomlabs-ai/headroom`](https://github.com/headroomlabs-ai/headroom) — [§5.14](#514-headroomlabs-aiheadroom) | token cost of tool output and RAG chunks | compress **what came back**, never what was asked; reversible compression; **compression runs locally, nothing is sent anywhere**; a `ContentRouter` picks a compressor **by content type** | this is the missing half of [19 §2](19-context-and-cost.md). [`heron_context.py`](../brain/heron_context.py) declares the boundary and implements no compression | a compressor that ever touches a Revit token destroys the only load-bearing part of the sentence | **Apache-2.0 with a `NOTICE`** — [§5.14](#514-headroomlabs-aiheadroom) corrects the *MIT* this row assumed. Compatible, but `NOTICE` travels with any redistribution | **Research further** — the reversible-retrieval idea is the one worth taking; nothing before [19 §2](19-context-and-cost.md)'s budgets are agreed |
| [`alibaba/open-code-review`](https://github.com/alibaba/open-code-review) — [§5.13](#513-alibabaopen-code-review-and-alibabaaacr-bench) | code review at scale | **deterministic pipeline first, agent second**; line-level findings; a built-in ruleset | the split is [19 §5](19-context-and-cost.md)'s. The line-level shape is what [`check-revit-gate.py`](../tools/check-revit-gate.py) does per question | its rulesets are NPE, thread-safety, XSS, SQL injection — a web/Java surface. Heron's ruleset is the Revit API, and none of theirs transfers | **Apache-2.0** — read at file level, including `extensions/vscode/`. Compatible | **Adopt concept** — already held; the ruleset itself does not transfer |
| [`alibaba/aacr-bench`](https://github.com/alibaba/aacr-bench) — [§5.13](#513-alibabaopen-code-review-and-alibabaaacr-bench) | no way to measure a review agent | a **benchmark with expert-verified answers** for repository-level review | [18](18-agent-operating-system.md) and the incoming §18 both ask for an evaluation suite and Heron has none. This is the shape one takes | its dataset is general code. A Heron benchmark has to be Revit tasks, which only the owner can author. **Its `dataset/` is two files — `positive_samples.json` and `negative_samples.json`**, which is [D-30](DECISIONS.md)'s shape | **Apache-2.0** — read at file level. Compatible | **Adapt concept** — the *shape* of a benchmark, never its cases |
| [`volcengine/OpenViking`](https://github.com/volcengine/OpenViking) — [§5.4](#54-volcengineopenviking) | context organisation for agents | **tiered loading** — L0 abstract, L1 overview, L2 details, loaded only as needed, and **built on write**; observable retrieval paths | [§5.4](#54-volcengineopenviking): Heron's fragments **already are** L0/L1/L2 — `semantic-identity`, the yaml, the `.cs` — and `BUDGET` already loads by depth. What is missing is the *vocabulary* and a per-folder abstract | **the main project is AGPLv3** — see §3. Idea only, and it was taken from the README, so no source was opened | **AGPL-3.0** — 1,116 files carry that SPDX header against 18 Apache. Apache covers **`crates/ov_cli`** (the Rust CLI, *not* `openviking_cli/`), `examples/`, and the TS/npm SDKs; `bot/` is MIT | **Adapt concept, code strictly off-limits** |
| [`karpathy/llm-council`](https://github.com/karpathy/llm-council) — [§5.12](#512-karpathyllm-council) | one model reviewing its own work | first opinions → **anonymised** peer review → a Chairman synthesises | [D-39](DECISIONS.md) already requires an *analysed disagreement*. **Anonymising which agent produced which answer is a genuine sharpening** of Shadow Mode — but [§5.12](#512-karpathyllm-council) found the labels are **not shuffled**, so anonymise *and* shuffle | it is N model calls per question, which [19 §5](19-context-and-cost.md) exists to avoid. Only for the high-risk path the incoming §6.9 describes | **NO LICENCE — all rights reserved by default.** [§5.12](#512-karpathyllm-council) corrects the *MIT* this row assumed. Idea only, transcribe nothing | **Adapt concept** — take the anonymising, not the council |
| [`obra/superpowers`](https://github.com/obra/superpowers) — [§5.8](#58-obrasuperpowers) | agents coding before understanding | brainstorm → design → plan → execute, with review checkpoints; skills as the unit | the discipline is [27](27-build-order.md)'s and this repository's practice | its skills are general software workflows; Heron's name **capabilities**, deliberately ([09](09-skills-and-fragments.md)) | MIT — compatible | **Adopt concept** — already held |
| [`garrytan/gstack`](https://github.com/garrytan/gstack) — [§5.7](#57-garrytangstack) | solo developer without a team | **not 23 roles** — 54 skills and one agent file. Three of them (`careful`, `freeze`, `guard`) declare a **PreToolUse hook in the skill's own frontmatter** | the 54 workflow skills are a developer's and transfer to nobody here. **The hook-in-the-skill shape is the fourth and best answer to [Q-49](OPEN-QUESTIONS.md)** — the guard installs with the capability | [§5.7](#57-garrytangstack) names three traps that silently turn such a hook into decoration. `ETHOS.md` measures itself in lines of code per day, which is [D-57](DECISIONS.md)'s frame exactly | MIT — compatible | **Reject the workflow, adopt the packaging** *(changed at file level — the row's 23-roles premise was not there)* |
| [`affaan-m/ECC`](https://github.com/affaan-m/ECC) — [§5.1](#51-affaan-mecc) | plans lost in chat, standards forgotten | **the plan as an artifact the human points at**, not chat history; hooks that **block the tool call** rather than checks a person remembers to run | the artifact idea is [23](23-heron-kernel.md)'s checkpoints and [`heron_workflow.py`](../mcp/server/heron_workflow.py). `tools/check-*.py` hold the same *rules* but **nothing runs them automatically** — [§5.1](#51-affaan-mecc) corrects the page-level claim that they are the equivalent | 68 agents, 286 skills, 94 commands. The scale IS the thing being rejected — [D-01](DECISIONS.md) gives the host the commands and [09](09-skills-and-fragments.md) gives skills capabilities | MIT — but [§5.1](#51-affaan-mecc) names a second project inside it | **Adopt the artifact principle, reject the scale.** File-level pass added two items — see [§5.1](#51-affaan-mecc) |
| [`ruvnet/ruflo`](https://github.com/ruvnet/ruflo) — [§5.2](#52-ruvnetruflo) | orchestrating many agents | swarm coordination; **Raft, Byzantine and Gossip consensus**; 100+ agents. **Under it, a security programme the landing page does not advertise** — a guard between retrieval and context assembly | almost none of the swarm. Heron has **one Revit, one pipe, one queue, one handler** ([D-09](DECISIONS.md)) — but the retrieval guard is aimed at the exact path Heron's RAG work will create ([§5.2](#52-ruvnetruflo)) | consensus answers *"which of my disagreeing replicas is right"*. Heron has no replicas. **314 MCP tools against Heron's 14** | MIT — compatible | **Reject the swarm** — and [§5.2](#52-ruvnetruflo) takes one question from underneath it |
| [`thedotmack/claude-mem`](https://github.com/thedotmack/claude-mem) — [§5.3](#53-thedotmackclaude-mem) | context lost at compaction | capture the session, **compress it**, inject relevant context next time | the *lifecycle* is [10](10-memory-and-knowledge.md)'s. The *capture everything* half is the opposite of Heron's rule | **compression is a model call, and every observation costs quota** — so the store cannot be rebuilt, against [D-24](DECISIONS.md) and [Golden Rule 11](14-golden-rules.md). ([§5.3](#53-thedotmackclaude-mem) corrects the store — **SQLite + FTS5, not ChromaDB** — and finds its retrieval ranks by *recency*, simpler than Heron's) | Apache-2.0 with a `NOTICE` — compatible | **Reject the mechanism**; the selective-memory principle is already held |
| [`PrimeIntellect-ai/prime-agent`](https://github.com/PrimeIntellect-ai/prime-agent) — [§5.11](#511-primeintellect-aiprime-agent) | long-running autonomous work | **there is no token or time budget** — [§5.11](#511-primeintellect-aiprime-agent) corrects this row. The mechanism is `shouldStopAfterTurn(context)`, a predicate handed to the embedder. Sessions do survive a disconnect, via a daemon | the predicate **is [D-01](DECISIONS.md)** written as a type: whoever runs the loop decides when it stops. Session survival is [23](23-heron-kernel.md)'s — but a Heron session pins a Revit and a document, so surviving means **re-verifying the pins, not restoring the state** | its daemon is for unattended running. [Golden Rule 9](14-golden-rules.md) puts a person in front of a high-risk action instead | MIT — but the copyright line names an individual, not the publishing organisation (§3) | **Changed: nothing to take.** The absent budget is the finding, and the shape confirms [D-01](DECISIONS.md) |
| [`K-Dense-AI/scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills) — [§5.9](#59-k-dense-aiscientific-agent-skills) | domain skills scattered across documentation | a **domain skill library** with per-skill metadata and host auto-discovery | the shape is [`brain/skills/`](../brain/skills/) and [09](09-skills-and-fragments.md) | **the repository does NOT say so — [§5.9](#59-k-dense-aiscientific-agent-skills) corrects this.** Its README says *"MIT… use freely"* while four of its 163 skills carry **"© 2025 Anthropic, PBC. All rights reserved."** Only `find -iname LICENSE*` shows it | MIT at the root; **four skills all-rights-reserved, one MIT under another holder** | **Adopt concept** — already held. The licence warning is now demonstrated rather than vague, and becomes [Q-53](OPEN-QUESTIONS.md) |
| [`ai-boost/awesome-harness-engineering`](https://github.com/ai-boost/awesome-harness-engineering) — [§5.10](#510-ai-boostawesome-harness-engineering) | no map of the field | an index, and the incoming §10.10 is right that it is one | a reading list for whoever answers `Q-45` and the compression question | an index is not a dependency, and treating it as one is how a list becomes a roadmap | CC0 | **Research further**, as an index only |
| *Claude CEO / CEO-style agent* (§10.15) — [§5.15](#515-claude-ceo--ceo-style-agent-1015--searched-properly-still-not-identified) | — | — | — | — | — | **NOT IDENTIFIED.** The incoming document says *"first identify the exact repository intended… study only if verified"*, and it could not be. **Nothing was studied and nothing is claimed.** [D-01](DECISIONS.md) already gives Heron the orchestrator-in-the-host pattern this row was reaching for |

---

## 3. The licence finding, which is the one that could have cost something

**[`volcengine/OpenViking`](https://github.com/volcengine/OpenViking)'s main project is AGPLv3** — and [§5.4](#54-volcengineopenviking)'s file-level audit found **1,116 files carrying that SPDX header against 18 Apache ones**, with the Apache island being `crates/ov_cli`, *not* the similarly named Python `openviking_cli/`.
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

**The file-level pass added a reading rule, from three repositories that each proved it:**

> **Read the copyright line, not just the licence name — and read it per directory, not per repository.**

- **ECC** ([§5.1](#51-affaan-mecc)) is MIT, and its fact-forcing gate names `zunoworks/gateguard` as its
  own origin in the file header. Nothing on the landing page says so.
- **OpenViking** ([§5.4](#54-volcengineopenviking)) is AGPL-3.0 at the root, with `bot/` under **MIT,
  "nanobot contributors"** and five vendored C/C++ libraries under their own.
- **prime-agent** ([§5.11](#511-primeintellect-aiprime-agent)) carries **"Copyright (c) 2025 Mario
  Zechner"** in a repository published by PrimeIntellect.

And the one that would actually have cost something — **scientific-agent-skills**
([§5.9](#59-k-dense-aiscientific-agent-skills)), whose README says *"MIT… use freely"* over four skills
marked **"All rights reserved."** Three of these four are visible only by listing licence files; none is
visible from a project page. That is [Q-53](OPEN-QUESTIONS.md).

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
   **Sharpened at file level ([§5.12](#512-karpathyllm-council)): anonymise *and* shuffle.** Their labels
   are assigned by position and never shuffled, so the candidate would always be `Response B` — and a
   position bias in whatever judges them would stop being noise and become a systematic bias in favour
   of production, which is the exact bias Shadow Mode exists to detect.
3. **Tiered loading** (OpenViking, idea only) is the second, and it belongs to
   [`heron_context.py`](../brain/heron_context.py): a part could carry an abstract, an overview and a
   full body, and a path's budget could name the tier rather than only the part.
   **Narrowed at file level ([§5.4](#54-volcengineopenviking)): Heron already has three quarters of it.**
   `semantic-identity` is the abstract, the yaml is the overview, the `.cs` is the body, and `BUDGET`
   already loads by depth. What is missing is a *vocabulary* for the depth and a per-folder abstract.
4. **A benchmark's shape** (`aacr-bench`) is the third, and it is the one with a hard prerequisite —
   the cases must be real Revit tasks, and only the owner can author them. **Confirmed at file level
   ([§5.13](#513-alibabaopen-code-review-and-alibabaaacr-bench)): its `dataset/` is exactly two files,
   `positive_samples.json` and `negative_samples.json`** — [D-30](DECISIONS.md)'s own structure.
5. **Everything else is already here, correctly, or is rejected with a reason.**

**None of this goes in front of the proving pass.** 218 fragments have never met a model, that is the
critical path, and every row above runs on any machine at any time.

---

## 4a. What the second pass changed, counted

The page-level matrix ended with *"nine of the fifteen needed no action at all."* **Reading the files
disagreed with that in ten of fifteen entries**, so the tally is recorded rather than described:

| | |
|---|---|
| **Licence cells wrong or unverified** | **5 of 16 rows.** `llm-council` **had no licence at all** where the row said MIT; `headroom` is **Apache-2.0 with a `NOTICE`**, not MIT; `OpenViking`'s Apache island is `crates/ov_cli`, **not** the similarly named `openviking_cli/`; both Alibaba cells said *"read before reuse"* and are now **read** |
| **Factual corrections** | `claude-mem` stores in **SQLite + FTS5, not ChromaDB**; `gstack` has **54 skills and one agent file**, not *"23 roles"*; `prime-agent` has **no token or time budget**; ECC has 286 skills, not 284 |
| **Corrections to this document's own claims** | §1 said agentmemory and Heron share *"the same three routes"* — **Heron fuses two** ([§5.5](#55-rohitg00agentmemory)); the ECC row said `tools/check-*.py` are the equivalent of ECC's hooks — **Heron has no hooks at all** ([§5.1](#51-affaan-mecc)) |
| **Decisions changed** | **2.** `gstack` *Reject* → **Reject the workflow, adopt the packaging**. `prime-agent` *Research further* → **nothing to take** |
| **Promoted into §1** | **2.** `code-review-graph`'s `uncertainty.py` and `superpowers`' verification rule — **neither visible from a landing page**, and the agreement count went four → six |
| **New questions for the owner** | **5** — [Q-49](OPEN-QUESTIONS.md) hooks, [Q-50](OPEN-QUESTIONS.md) show don't tell, [Q-51](OPEN-QUESTIONS.md) the retrieval guard, [Q-52](OPEN-QUESTIONS.md) a third stream, [Q-53](OPEN-QUESTIONS.md) imported licences |
| **Built, not parked** | **[24 §7](24-trust-model.md)** — the *who is speaking* trust table, derived entirely from existing rules, which gives [Q-51](OPEN-QUESTIONS.md) and [Q-53](OPEN-QUESTIONS.md) the vocabulary they were missing |
| **Stale claims found in Heron's own documents** | **2.** [17 §4](17-open-source-and-distribution.md)'s readiness table had **five wrong rows**, including `LICENSE` marked *pending* a question answered days earlier; and `Q-46`'s count had stayed at 143 after the rule narrowed it to 59 |
| **Still not identified** | **1** — §10.15, now with a recorded search behind it ([§5.15](#515-claude-ceo--ceo-style-agent-1015--searched-properly-still-not-identified)) |

**The pattern across all sixteen is one sentence: a landing page says what a project means to be, and
only its files say what it is.** Every correction above was invisible from a project page, and two of
them — a licence that did not exist and a licence that was stricter than claimed — are the kind that
cost something later rather than immediately.

**And the second-order finding is the one worth keeping.** Three of the corrections were to *this
document*, and two of the stale claims were in *Heron's own documents* — found not by auditing Heron but
by asking somebody else's repository a question and then asking the same question here. **That is the
cheapest audit method in this exercise**, and it is available any time.

---

## 5. The file-level pass, repository by repository

Each repository below was **cloned and read** — not browsed. The commit is recorded so the reading has
a fingerprint, the way [D-30](DECISIONS.md) requires of a proof: a claim about a moving target is worth
nothing without the moment it was true.

Each entry says four things and nothing else: **what was actually opened**, **what the mechanism turns
out to be**, **what it changes about the matrix row**, and **what Heron should do**. Where the answer is
*nothing*, it says nothing — a repository that confirms what is already built is a good outcome, not a
row to fill.

---

### 5.1 `affaan-m/ECC`

**Read at** `5064474d4d762dc9640234a41617cccb79185cec`, committed 2026-09-07. MIT.

**Opened:** `hooks/hooks.json`, `hooks/README.md`, `scripts/hooks/gateguard-fact-force.js` (1,175
lines), `RULES.md`, `WORKING-CONTEXT.md`, `skills/plan-canvas/SKILL.md`, and the tree itself —
68 agent files, 286 `SKILL.md`, 94 commands, 5 hook entries, 122 rule files.

**Correction to the matrix row.** It said *284 skills*; the tree holds **286**, and the project's
README agrees. Small, and fixed — but the same paragraph made a larger claim that the file-level read
does not support, below.

#### The claim that was wrong: Heron has no hooks at all

The row said *"enforcement outside the model's context is what `tools/check-*.py` are."* Half of that is
true and the half that matters is not.

ECC's `hooks/hooks.json` registers **PreToolUse** entries against `Bash`, `Write`, `Edit|Write`,
`Bash|PowerShell|Write|Edit|MultiEdit` and `.*`. A PreToolUse hook **runs before the tool does and can
refuse it**. The enforcement is not a document the model is asked to obey; it is a process the model
cannot talk its way past.

Heron holds the same *rules* — `check-structure.py` refuses `Autodesk.Revit` outside `revit/`,
`check-metadata.py` refuses a file without a header, `check-docs.py` refuses a broken link. But **Heron
has no `.claude/settings.json` and no hooks of any kind.** Every one of those gates runs only when a
person types it. A gate that has to be remembered is a gate that is skipped on the day it matters, and
that day is the busy one.

This is not a small difference and it is not a licence question — the mechanism is the host's, not
ECC's. It is recorded as **[Q-49](OPEN-QUESTIONS.md)**.

#### The idea worth having: fact-forcing is not confirming

`gateguard-fact-force.js` opens with its own reasoning, and it is the sharpest sentence in the
fifteen repositories:

> Instead of asking "are you sure?" (which LLMs always answer "yes"), this hook demands concrete facts:
> importers, public API, data schemas. **The act of investigation creates awareness that
> self-evaluation never did.**

Before an edit it refuses until the agent has listed every file importing the target, named the public
functions affected, shown any data schema touched, and **quoted the user's instruction verbatim**.
Before a destructive shell command: list what it will delete, write a one-line rollback, quote the
instruction.

**Heron already holds the stronger half of this, and does not hold the other half.**

The stronger half is that Heron never asks *"are you sure?"* about a write at all.
[`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs) builds a preview carrying `willMove`,
`willSkip`, the category, the distance in the units a modeller said, and the document title — then, at
execute time, **re-counts against the live model and refuses if the set moved** (`now.SetEquals(preview.Ids)`).
That is a fact a model cannot produce by being confident, which is exactly ECC's complaint about
self-evaluation, answered in the one place where it can be answered properly.

The half Heron does not have: **the facts run one way.** Heron shows facts to the *person*. Nothing
makes the *model* state what it is about to touch before it asks for the write. And the same asymmetry
is already open under a different name — **[Q-46](OPEN-QUESTIONS.md)**: 59 fragments go looking, drop
candidates, and name none of them. ECC's sentence is independent evidence that Q-46 is a real defect
rather than a tidiness complaint: the value of naming what was dropped is not the record, it is the
looking.

#### The idea worth taking: Heron's plan canvas is Revit

`skills/plan-canvas/SKILL.md` is better than the "editable artifact" line written from the landing
page. The plan opens **in the human's browser**; they annotate *the element they mean* rather than
describing it; they return **Approve plan / Request changes**; the agent blocks on one CLI call that
returns the verdict as JSON. Its own justification: feedback like *"move this, change that"* is easier
pointed at than typed.

For a BIM modeller that is obviously right and the browser is obviously wrong. **Heron already has the
canvas: it is the model.** And the add-in already holds what it would need — `preview.Ids` and
`preview.Skipped` are both in memory when the preview is offered, and
[`set-selection`](../brain/fragments/set-selection/) already exists as a fragment.

So the Heron-shaped version of plan-canvas is one sentence: **before the modeller answers, select the
elements the change would touch, so they see them highlighted in the view instead of reading a count.**
And the second half answers Q-46 in the same motion — selecting the *skipped* set shows what Heron
decided not to do, which no sentence conveys as well as thirty highlighted ducts.

Recorded as **[Q-50](OPEN-QUESTIONS.md)**. Not built: it changes a confirmation path, and
[Golden Rule 9](14-golden-rules.md) says that path is gated in code, so it is the owner's call and not
a tidy-up.

#### Two notes for the licence file

- **There is a second project inside this one.** `gateguard-fact-force.js` names its own origin in its
  header — `zunoworks/gateguard`, also distributed as a `pip` package. ECC's own `LICENSE` is MIT and
  says nothing about it. This is the `scientific-agent-skills` warning in §3 appearing a second time,
  in a repository where nothing on the landing page suggested it: **the licence of a repository is not
  the licence of everything in it.** Nothing is being taken from either project, so nothing turns on it
  here — but the reading rule does: *read the file, not the repository, before reuse.*
- **Their own two documents disagree.** `hooks/README.md` says a PreToolUse hook blocks with **exit
  code 2**; `RULES.md` says *"exit `1` only when blocking behavior is intentional."* Both cannot be
  right. It is a small thing in someone else's repository and it is worth naming only because it is the
  precise failure `tools/check-docs.py` exists to prevent, and because this repository shipped the same
  class of defect for eight days ([32 §4](32-master-architecture-reconciliation.md) — four binding
  rules described as proposals). **A large catalogue outgrows its own description.** ECC's
  `WORKING-CONTEXT.md`, dated 2026-04-08, still calls the catalogue *"47 agents, 79 commands, 181
  skills"* against a tree holding 68, 94 and 286.

**Decision: unchanged.** Adopt the artifact principle, reject the scale. The file-level pass adds two
questions ([Q-49](OPEN-QUESTIONS.md), [Q-50](OPEN-QUESTIONS.md)) and corrects one claim.

---

### 5.2 `ruvnet/ruflo`

**Read at** `e341ec8c4aba8ea616499180dee53035af7e295c`, committed 2026-09-08. MIT.

**Opened:** `.harness/mcp-policy.json`, `.harness/README.md`,
`v3/@claude-flow/memory/src/agentdb-retrieval-guard.ts`,
`v3/@claude-flow/hooks/src/workers/memory-poison-forensics.ts`, `README.md`, and the tree —
2,154 TypeScript files, 1,886 markdown, 39 Rust.

**The rejection stands and now has a number.** The matrix said *"almost none"* applies to Heron. Its own
README says a user need not *"learn 314 MCP tools or 26 CLI commands"*. **Heron has 14 MCP tools.** That
is not a difference of degree.

**But the rejection was pointed at the wrong thing, and the repository is worth more than its row.**
Under the swarm is a security programme, and two of its files are directly about the problem Heron is
walking towards.

#### The finding that matters most, and it is about RAG

`agentdb-retrieval-guard.ts` puts a guard **between retrieval and context assembly**: every chunk coming
back from the vector search is scanned before it is allowed into the prompt. Its reasoning, from the
file:

> AgentDB's retrieval path has zero certified defenses against poisoned memory entries — SMSR shows
> 93-100% undefended attack success, reduced to 0% behind a certified content guard.

And one engineering detail that is worth the whole read:

> Oversized chunks are flagged (or dropped in strict mode) **rather than truncated** — truncation would
> let an attacker pad a payload past the guardrail's own scan window.

**Heron enforces [Golden Rule 19](14-golden-rules.md) where it counts today, and that half is done
properly.** Risk comes from the operation registry inside the add-in, never from the request
([`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs): *"Nothing in the request decides
the operation name"*). **No text arriving on the pipe can raise Heron's permission level**, which is
exactly what Rule 19 says and exactly where a rule of that kind has to live.

**The other half has not arrived yet, and the code already says so.** Rule 19 governs what text may
*authorise*. It says nothing about what happens to the text itself on the way up. Heron makes no model
calls ([D-01](DECISIONS.md), [D-58](DECISIONS.md)) — so Heron is not the thing that can be injected. It
is the thing that **carries**. [`heron_context.py`](../brain/heron_context.py) assembles parts and hands
them to the host, and every part is stamped with `source`, which the rendered context prints.

Today that is safe for a reason that is about to expire: **every source is Heron's own** — the caller's
request, the fragment library, its tests, the API surface. There is one exception and it is already
written down, in the part list itself:

```python
STANDARD = "standard"        # the clauses cited - source does not exist yet
```

**The day that source exists is the day Heron carries text it did not write** — a project standard, a
specification, a family description, an imported package. That is the same day the RAG work makes
Heron's retrieval useful, so the two arrive together and the guard has to be designed with the index
rather than added after it.

Recorded as **[Q-51](OPEN-QUESTIONS.md)**. Nothing is built: there is nothing to guard yet, and a
scanner written against no corpus is a scanner written against a guess.

`memory-poison-forensics.ts` is the same programme's second phase — anomaly detection on write
*sequences* rather than content. It does not transfer: it exists because *"AgentDB accepts writes from
any swarm agent"*, and Heron has one writer, one pipe, one queue ([D-09](DECISIONS.md)). Recorded here
only so the reason is a reason and not an omission.

#### Default-deny: Heron's is the stronger one, and one half of it is not a gate

`.harness/mcp-policy.json` declares `"defaultDeny": true`, `allowShell: false`, `auditLog: true`,
`requireApprovalForDangerous: true`, a dangerous-pattern list and `maxToolCallsPerTurn: 200`.

Heron holds four of those five, and holds them better, because **ruflo's is a JSON file a scanner
reads and Heron's is a branch in the one code path every operation passes**
([`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs)):

> UNDECLARED IS REFUSED, never assumed harmless. The list of what IS available comes from the registry
> rather than being typed here.

Same guard, in order: undeclared refused → emergency stop for anything at `MODIFY` or above → permission
level. A config file cannot be forgotten by a code path that does not consult it; a code path every
operation passes cannot be forgotten at all.

**One thing worth saying plainly, because the file-level read is what showed it.**
[`heron_tools.py`](../mcp/server/heron_tools.py) has the right shape — `risk_of()` raises `NotDeclared`
rather than defaulting, and its own message says *"being absent is a refusal, not a risk of zero."* But
at runtime it is consulted in exactly **one** place, `heron_mcp_server.py:486`, to classify a failure,
with the tool name written as a string literal. **It is a declaration and a cross-check, not a gate**,
and the gate is in the add-in where it belongs — the only side that can see the model is the only side
whose refusal means anything. That is right. It is worth writing down so nobody later mistakes the
Python registry for the thing that stops something.

**The fifth, `maxToolCallsPerTurn`, is not Heron's.** Bounding a loop is the host's job
([D-01](DECISIONS.md)). Named here so its absence is a decision.

#### Licence

MIT, and clean at the top level. Nothing is taken.

**Decision: unchanged — Reject.** The swarm, the consensus and the 314 tools are the thing being
rejected and the file-level pass makes the rejection sharper, not softer. **One question comes out of
it** ([Q-51](OPEN-QUESTIONS.md)), and it is the most valuable single item the whole research programme
has produced, because it lands on work that has not been done yet rather than on work already finished.

---

### 5.3 `thedotmack/claude-mem`

**Read at** `8bc631a71a487424b866756e43a6efa4574cc66b`, committed 2026-09-08. Apache-2.0, with a `NOTICE`
file — which Apache 2.0 requires be carried forward, and which nothing here needs to carry because
nothing is taken.

**Opened:** `src/storage/sqlite/schema.ts`, `src/storage/sqlite/memory-items.ts`,
`src/shared/SettingsDefaultsManager.ts`, `src/shared/cmem-gateway.ts`, `src/shared/quota-cooldown.ts`,
`src/services/telemetry/common.ts`, `src/npx-cli/commands/telemetry.ts`, `ragtime/README.md`, `README.md`.

**The project has been renamed.** Its own README, first line of the body: *"Claude-Mem is now Grok Mem.
The package is still `claude-mem`."* The repository identified in the matrix is the right one; the name
in the row is the package, not the product.

#### Two corrections, and the second one goes Heron's way

**It is not ChromaDB.** The matrix row said it *"captures whole sessions and stores them in ChromaDB."*
The store is **SQLite**, with a Postgres backend beside it. Chroma survives as six configuration keys in
`SettingsDefaultsManager.ts` and nothing else in the storage path. Written from the landing page,
believed, and wrong — which is the whole reason for this pass.

**Its retrieval is simpler than Heron's, not richer.** `memory-items.ts` searches an FTS5 virtual table
and orders the result:

```sql
WHERE memory_items_fts MATCH ?
ORDER BY memory_items.updated_at_epoch DESC
```

**Keyword match, ranked by recency.** Not by relevance, not by nearness, not fused.
[`heron_retrieve.py`](../brain/heron_retrieve.py) runs FTS5 **and** embedding nearness, fuses them by
Reciprocal Rank Fusion, and applies the Revit version filter as a hard wall before either route ranks
anything. The incoming §10.3 sent this project to be studied *for retrieval*. Read at file level, **the
retrieval is the part Heron should not take.**

#### The reason to reject it is better than the one in the row

The row rejected the mechanism because *"it captures whole sessions"* and because of a paid tier. The
real reason is one layer down and it is disqualifying on its own:

- `SettingsDefaultsManager.ts`: `CLAUDE_MEM_CLAUDE_AUTH_METHOD: 'subscription'`.
- `quota-cooldown.ts` describes *"a doomed request per observation, for the rest of the billing cycle."*
- `cmem-gateway.ts`: *"Once the free trial ends without a subscription, the gateway answers with…"*

**The memory is built by making model calls.** Compression is a model summarising a session, and every
observation costs quota.

That is not a licence problem and not a pricing complaint. It is a direct collision with
[D-24](DECISIONS.md) — *re-indexing has to be free or it stops happening* — and with
[Golden Rule 11](14-golden-rules.md), which makes the store **derived**, so
[`heron_scope.py`](../brain/heron_scope.py)'s `rebuild()` is always safe to run. **Heron's knowledge
store can be thrown away and rebuilt offline at no cost. This one cannot be rebuilt at all.** A store
that is expensive to rebuild is a store nobody rebuilds, and a store nobody rebuilds goes stale — which
is [D-30](DECISIONS.md)'s staleness fingerprint arriving at the same conclusion from the other end.

**Telemetry, stated fairly.** It ships a PostHog key and a default host and is on unless turned off —
but it honours `DO_NOT_TRACK`, `CLAUDE_MEM_TELEMETRY=0` and a `telemetry disable` command, and scrubs
before sending. That is the standard handled properly. It is still the opposite of what
[D-26](DECISIONS.md) asks of Heron, and it is named here as a difference of purpose rather than a fault.

#### One small technical note, kept because it has a date on it

Their FTS query builder strips every character that is not a letter, digit or underscore, and quotes
each surviving token. [`heron_search.py`](../brain/heron_search.py) does the same class of thing —
`_FTS_UNSAFE = [^\w\s]` — and its comment already reasons about why: *"a user typing '300x300 duct?' is
asking a question, not writing a query."*

Both keep `_`, so `OST_DuctCurves` survives intact. Both split on `-` and `.`, so `EF-01` becomes two
prefix terms and `M_Single-Flush` becomes two words.

**For Heron's corpus today that is correct and costs nothing** — the text being searched is Heron's own
fragment library, where nobody types an equipment tag. It stops being correct on exactly the date
[Q-51](OPEN-QUESTIONS.md) names: when the corpus is project documents and model data, `EF-01` and
`M_Single-Flush` are the tokens a modeller types most, and `EF*  OR  01*` matches every extract fan on
the job. Not opened as its own question, because Q-51 already owns that day and one date should not have
two questions.

**Decision: unchanged — Reject the mechanism.** The selective-memory principle is already held. The file-level
pass replaced a weak reason with a strong one and corrected two claims.

---

### 5.4 `volcengine/OpenViking`

**Read at** `39670e7b95a26ab6d562e018c7b8681ae3dba17a`, committed 2026-09-09.

**This is the one where the reading rule matters, so it is stated before the finding.** Two things were
opened and nothing else: **the licence files** and **the README**. No source file was read for its
logic. The greps that produced the licence audit below matched `SPDX-License-Identifier` headers and
filenames — a licence audit is not a transcription.

**And it turned out not to be a sacrifice.** The whole idea worth taking is on the project's own README,
in plain English, in eleven lines. The AGPL source was never needed.

#### The licence audit, which corrects the row in the direction that could have cost something

The matrix said *"AGPLv3 (CLI and examples Apache-2.0)"*. That is right and **dangerously imprecise**.
What is actually there:

| | licence | |
|---|---|---|
| root, and the Python package | **AGPL-3.0** | `pyproject.toml` declares it; **1,116 files carry the `AGPL-3.0` SPDX header** |
| `crates/ov_cli` — the **Rust** CLI | Apache-2.0 | this is "the CLI" the row meant |
| `examples/` | Apache-2.0 | |
| `sdk/typescript`, `npm/cli` | Apache-2.0 | declared in their `package.json` |
| `bot/license/` | **MIT** | *"Copyright (c) 2025 nanobot contributors"* — a third project inside the second |
| `third_party/` | five vendored C/C++ libraries | leveldb, rapidjson, spdlog, croaring, krl, each under its own |

**Eighteen files carry an Apache SPDX header against 1,116 AGPL.** And the Python directory named
`openviking_cli/` — the one anybody would reach for on hearing *"the CLI is Apache"* — carries **no
separate licence and is AGPL**. A row saying "the CLI is Apache" is exactly how the incoming §2's
*"must never happen accidentally"* happens accidentally. **The row is corrected to name the crate.**

#### The idea, taken from the README, and Heron already has three quarters of it

> Content is processed into three tiers — **L0 abstract, L1 overview, L2 details** — and loaded on
> demand… Each directory carries its own L0/L1 layers, so relevance can be judged before any full file
> is read.

With sizes: `.abstract` ~100 tokens, `.overview` ~2k, the file itself loaded only when needed. And the
tiers are built **on write**, not on read.

**Heron's fragment library is already this, and nobody named it:**

| OpenViking | Heron | where it lives |
|---|---|---|
| **L0** abstract | `semantic-identity` — *"put these elements in the current selection"* | in the SQLite `fragments` table, so it is what search matches and what `find()` ranks |
| **L1** overview | the rest of `fragment.yaml` — `purpose`, `contract`, `risk`, `domain` | on disk, reached through the store's `folder` column |
| **L2** details | `impl/any/fragment.cs` | on disk, read only when generating |

And [`heron_context.py`](../brain/heron_context.py)'s `BUDGET` already loads different depths for
different paths — `CACHED` gets the capability line, `GENERATION` gets the neighbouring fragment's full
source and its tests. **That is "loaded only as deep as the task requires", already running.**

**So what is actually missing is two things, and both are small:**

1. **The depth is not a declared, checkable thing.** `BUDGET` names *part kinds*, not tiers. Nothing
   stops a future path from reaching L2 where L0 would have answered, because there is no vocabulary in
   which that sentence can be written down — and this module was built precisely so that a budget
   violation raises rather than passes ([`OverBudget`](../brain/heron_context.py)).
2. **There is no directory-level abstract.** OpenViking can judge a *folder* before opening anything in
   it. Heron has no per-domain summary, so nothing can weigh `revit.selection` against `revit.mep`
   without reading fragments from both.

**And the sharpest detail is the one that decides whether any of it is affordable: the tiers are built
on write.** Which raises the only question that matters here — *who writes the abstract?* For Heron the
answer is already settled and it is the good one: `semantic-identity` is **written by a person in the
yaml and derived into the store**, so it costs nothing to rebuild ([Golden Rule 11](14-golden-rules.md),
[D-24](DECISIONS.md)). The moment an abstract is produced by a model, the store stops being rebuildable
and becomes [§5.3](#53-thedotmackclaude-mem)'s problem.

**Not opened as a question.** It is a naming and a small addition to a module built two days ago, it
belongs with the tiered-loading item already ranked third in §4, and Heron has no second corpus to
abstract until [Q-51](OPEN-QUESTIONS.md)'s day arrives.

**Decision: unchanged — Adapt concept, code strictly off-limits.** The concept came from the README, so
"off-limits" cost nothing at all. The licence row is corrected to say `crates/ov_cli` rather than
"the CLI".

---

### 5.5 `rohitg00/agentmemory`

**Read at** `e04ba88819c365c9acf9d6661ea802143e728bd6`, committed 2026-08-23. Apache-2.0.

**Opened:** `src/state/hybrid-search.ts`, `src/state/reranker.ts`, `src/functions/query-expansion.ts`,
`src/types.ts`, `DESIGN.md`, `README.md`.

**This row carries §1's headline claim, so it was the one that most needed checking.** It survives, with
one correction that is mine rather than theirs.

#### What is confirmed, and it is stronger than the page suggested

`hybrid-search.ts` line 20:

```ts
const RRF_K = 60;
```

[`heron_retrieve.py`](../brain/heron_retrieve.py) line 64:

```python
RRF_K = 60
```

**The same technique and the same constant, reached by two projects that did not talk to each other.**
Heron's comment explains the choice from first principles — *"60 is the value the technique is normally
used with; what it does is stop rank 1 from dwarfing everything"* — and theirs simply uses it. Both
weight the streams rather than fusing them flat.

The weighting differs, and Heron's is the better-founded of the two. agentmemory hard-codes
`bm25Weight = 0.4`, `vectorWeight = 0.6`, `graphWeight = 0.3`. Heron's weight **follows the backend**,
because Step 10 measured what each backend is actually worth — a fixed 0.6 for "the vector route"
assumes every vector route is equally good, and Heron's `lexical` and `model` backends are not.

#### The correction: Heron has two streams, not three

**§1 said *"the same three routes"*. That is wrong and it is corrected above.**

agentmemory fuses **three**: BM25, vector, and a graph stream. Heron fuses **two** — `keyword_rank` and
`vector_rank`. [`heron_graph.py`](../brain/heron_graph.py) exists, but `heron_retrieve.py` does not
import it, and its only production caller is [`check-gaps.py`](../tools/check-gaps.py).

**And the obvious repair is probably wrong**, which is why it is a question rather than a task.
agentmemory's graph stream expands *entities found in the query* — it is a recall widener, closer in
spirit to Heron's keyword route than to anything else. Heron's graph answers a different question
entirely: `composes_into` / `composes_from`, derived from the contracts, *A provides what B needs*.

That is a **composition** graph, not an entity graph. Fusing it into retrieval would mix *"which
fragment answers this request"* with *"which fragment goes next to that one"*, and a strong helper could
outrank the fragment that actually answers. The idea is worth testing precisely because copying the
shape without the semantics is the mistake. Recorded as **[Q-52](OPEN-QUESTIONS.md)**, and Heron can now
measure the answer rather than argue it — [`measure-routes.py`](../tools/measure-routes.py) gives the
before.

#### Their two recall boosters, and Heron may have exactly one of them

**Query expansion is a model call.** `query-expansion.ts` opens with a system prompt — *"You are a query
expansion engine… generate 3-5 reformulations"* — and generates reformulations and temporal
concretizations before searching. For Heron that is settled, not missing: [D-01](DECISIONS.md) and
[D-58](DECISIONS.md) put every model call in the host. If a request needs rephrasing, the host rephrases
it and asks Heron again. **Not a gap.**

**The reranker is not a model call, and it is Heron-shaped.** `reranker.ts` loads a local quantised
cross-encoder — `@huggingface/transformers`, `Xenova/ms-marco-MiniLM-L-6-v2`, `dtype: "q8"` — off by
default behind `RERANK_ENABLED`. Local, offline, no account, quantised: that is
[D-24](DECISIONS.md) and [D-26](DECISIONS.md) satisfied, and the same shape as
[`heron_embed.py`](../brain/heron_embed.py)'s model2vec backend. Heron has no reranker at all.

**With one scar attached.** [D-49](DECISIONS.md) is a thirty-minute hang caused by importing the trained
encoder on the asyncio event loop. A cross-encoder is a **second** model load on the same path, and
[`measure-brain.py`](../tools/measure-brain.py) now times exactly that stage — so the cost is
measurable before it is paid. Not opened as a question: a reranker reorders a list, and nothing can say
whether Heron's list needs reordering until there is a corpus and a measurement it fails.

**The four tiers are real** — `types.ts` carries `"episodic" | "semantic" | "procedural"` as a union and
`ProceduralMemory` as an interface, so the matrix row's *"four memory tiers"* is a structure and not a
README diagram. The row's decision stands.

**Decision: unchanged — Adapt concept.** Compare the four tiers against [10](10-memory-and-knowledge.md).
The file-level pass corrects §1's headline, confirms the constant, and adds
[Q-52](OPEN-QUESTIONS.md).

---

### 5.6 `tirth8205/code-review-graph`

**Read at** `b58668751ab0c7670c078cf7cbd4d1f5b8e54f81`, committed 2026-08-26. MIT.

**Opened:** `code_review_graph/uncertainty.py`, `incremental.py`, `context_savings.py`, `cli.py`,
`README.md`.

**The row's claims hold.** `incremental.py` hashes with `hashlib.sha256(raw).hexdigest()` — content
hashing confirmed, the same choice as [`heron_embed.py`](../brain/heron_embed.py). `cli.py` registers
`impact` with the help text *"Analyze the blast radius of changes"* — confirmed. **And the row is still
the least interesting thing in the repository**, because it was written from the landing page and the
landing page does not mention `uncertainty.py`.

#### `uncertainty.py` is the plausible zero, arrived at independently — the fifth agreement

Its own opening paragraph:

> A bare `result_count: 0` is ambiguous. It can mean "the code really has no such relationship", or it
> can mean "**this graph cannot see that relationship**": the target was never indexed, the graph is
> behind the working tree, or the target's language has a known static-analysis blind spot. Reading
> agents take the first meaning, and then either draw a wrong conclusion or abandon the graph and grep
> the whole repository.

**That is [D-52](DECISIONS.md), written by somebody who has never seen it.** Heron's version is
`FILTER_ELEMENTS_BY_CATEGORY` reporting `unresolvedLevel` so a broken lookup reads as *"12 found, 12
with no level"* rather than as a clean zero. Theirs is a static-analysis blind spot reading as *"this
relationship does not exist."* **Same failure, same fix, different industry.**

**This is direct evidence for two questions the owner has not yet answered**, and it is evidence rather
than an opinion because it comes from a project with no stake in Heron:

- **[Q-46](OPEN-QUESTIONS.md)** — 59 fragments go looking, can drop a candidate, and name none of them.
- **[Q-48](OPEN-QUESTIONS.md)** — 62 reading fragments collect the host document and say nothing about
  links, which in federated MEP work is a *confident smaller number*.

#### And their engineering answer removes the objection to answering them

The reason to hesitate over Q-46 and Q-48 is cost: every fragment saying what it could not see makes
every answer longer. **They measured it the other way round**:

> One short sentence on the empty case is therefore a **token saving, not a cost**: it replaces a
> multi-thousand-token fallback search with roughly thirty tokens of honesty.

With three design choices that make it hold, all three transferable:

1. **The marker is attached only when the result list is empty**, so *"every response that carries
   results stays byte-identical to before."* Nothing already working gets longer.
2. **It is hard-capped** — `MAX_CONFIDENCE_CHARS`. Honesty with a ceiling cannot become a paragraph.
3. **The blind-spot list is data, not scattered conditionals** — *"every entry describes a gap that is
   real in this codebase today; capabilities that have since been implemented are deliberately
   absent."* That is [D-54](DECISIONS.md) — *a message describing a gap must be corrected when the gap
   closes* — as a structural choice rather than a discipline.

**A fragment reporting nothing found is the same sentence with the same ceiling.** The cheapest version
of Q-46 is now visible: not 59 edits, but one rule that fires only on the empty case.

#### One nuance about D-58, which it does not overturn

`context_savings.py` estimates tokens at **4 characters per token**, labels the number an estimate
everywhere, and offers `verify_with_tiktoken` to calibrate it.

**[D-58](DECISIONS.md) is untouched by this** and the distinction is worth keeping sharp: D-58 says
*cost per request* and *token usage* belong to the process that made the call, because per-request
attribution exists nowhere else. Estimating **the size of what Heron itself assembled** is not that
claim — Heron wrote those characters and can count them.

Which matters because [`heron_context.py`](../brain/heron_context.py) budgets in **parts**, and
[19 §2](19-context-and-cost.md) asks for budgets. A part budget cannot say *"this path may spend two
thousand tokens"*. A labelled estimate can, in about thirty lines, without claiming to know a price.
**Recorded here rather than opened as a question**, because it belongs to
[19 §2](19-context-and-cost.md)'s budgets, which the Headroom row already names as the prerequisite for
anything in this area.

**Decision: unchanged — Reject the implementation, principle already held.** The Tree-sitter code graph
over `revit/` is still the off-mission build. The file-level pass promotes this project into §1 as the
**fifth** independent agreement and hands [Q-46](OPEN-QUESTIONS.md) a cheaper answer than the one it was
written with.

---

### 5.7 `garrytan/gstack`

**Read at** `c8f0c4e368fd59ec316c0eb0d1f4ebfa896c2d16`, committed 2026-09-08. MIT.

**Opened:** `freeze/SKILL.md`, `freeze/bin/check-freeze.sh`, `careful/SKILL.md`, `guard/SKILL.md`,
`ETHOS.md`, and the tree.

**The row's premise was wrong.** It said *"23 named specialist roles"* and rejected the project for
creating a second registry beside [28](28-agent-registry.md)'s 250 agents.

At file level `agents/` holds **one** file — `openai.yaml`. What the landing page calls roles are **54
top-level skill directories**, each a `SKILL.md`. **There is no registry to be a second of.** The
rejection was aimed at something that is not there.

#### Three of the fifty-four are the mechanism [Q-49](OPEN-QUESTIONS.md) is about

`careful`, `freeze` and `guard` are not workflow skills. They are **safety hooks, and the hook is
declared in the skill's own frontmatter**:

```yaml
name: freeze
description: Restrict file edits to a specific directory for the session.
hooks:
  PreToolUse:
    - matcher: "Edit"
      hooks:
        - type: command
          command: "bash $HOME/.claude/skills/gstack/freeze/bin/check-freeze.sh"
```

`careful` warns before `rm -rf`, `DROP TABLE`, force-push, `git reset --hard`. `freeze` confines edits
to one directory. `guard` is the two composed — and it **calls the other two skills' scripts rather than
copying them**.

**This is a fourth shape for [Q-49](OPEN-QUESTIONS.md), and it is the one that fits Heron.** The
question offered three: nothing, a git pre-commit hook, or a `.claude/settings.json`. gstack's answer is
better than all three: **the hook travels with the skill**, so installing the capability installs its
guard and the two cannot drift apart. Heron already has [`.claude/skills/`](../.claude/skills/) — the
shelf is there and empty of exactly this.

#### And `check-freeze.sh` names three traps that would silently defeat a naive attempt

This is the part no landing page could have given, and each one turns a hook into decoration:

1. **The decision must be nested.** *"The decision MUST be nested under `hookSpecificOutput` — Claude
   Code ignores a top-level `permissionDecision`, which silently no-ops the block."* A hook written the
   obvious way blocks nothing and reports no error.
2. **A hook that dies is read as permission.** *"Any unexpected non-zero death… would otherwise exit
   with no decision JSON, which Claude Code treats as non-blocking — the edit proceeds."* They install
   an `EXIT` trap that emits a deny, so a crash cannot become an allow.
3. **Polarity is a decision, not a default.** *"freeze is a DENY-tier hook, so an unreadable payload
   DENIES (fail closed)… a boundary that fails open is not a boundary."* `careful` is ask-tier and
   deliberately fails the other way.

**That third sentence is this repository's own reasoning in someone else's words**, and the first two
are the kind of thing that is learned by shipping it wrong. All three are now written into
[Q-49](OPEN-QUESTIONS.md), so if the answer is yes, the first attempt is not the broken one.

#### What is still rejected, and one thing that looks like a gap and is not

The 54 skills are a **solo developer's engineering workflow** — `ios-qa`, `land-and-deploy`,
`design-review`, `scrape`, `make-pdf`. Not one transfers to a BIM modeller, and
[32 §1](32-master-architecture-reconciliation.md)'s mission correction applies to every one of them.
`ETHOS.md` measures its own value in *"10,000+ usable lines of code per day"*, which is the developer-harness
frame this repository rejected outright ([D-57](DECISIONS.md)).

**`ETHOS.md` is injected into every workflow skill's preamble automatically, and Heron does not inject
its Golden Rules anywhere.** That looks like a gap and is not: [D-01](DECISIONS.md) gives the host the
persona and the orchestration, and the host already reads `CLAUDE.md`. Heron's rules reach the model
through the host's mechanism, which is the correct one. Named here so the absence reads as a decision.

**Decision: changed — from *Reject* to *Reject the workflow, adopt the packaging*.** The 54 workflow
skills stay rejected on mission. The **hook-in-the-skill-frontmatter shape** becomes the recommended
answer to [Q-49](OPEN-QUESTIONS.md) should the owner want one, and the three traps come with it.

---

### 5.8 `obra/superpowers`

**Read at** `b36e0829c6d0140e93cfef2ca599b1b07d4a7797`, committed 2026-08-12. MIT.

**Opened:** `skills/verification-before-completion/SKILL.md`, `skills/systematic-debugging/SKILL.md`,
`README.md`, and the tree — **14 skills, 3.2 MB.** After ECC's 286 and gstack's 54, the smallest and
the most disciplined of the three, which is itself the point the project is making.

**The row said *"Adopt concept — already held"* and that is confirmed.** What the file-level read adds is
a **sixth independent agreement**, and it is on the discipline Heron's whole proving pass rests on.

#### `verification-before-completion` is D-30, from a different direction

```
NO COMPLETION CLAIMS WITHOUT FRESH VERIFICATION EVIDENCE
```

> If you haven't run the verification command in this message, you cannot claim it passes.

And in its failures table, one row that is [D-30](DECISIONS.md)'s negative case exactly:

| Claim | Requires | **Not sufficient** |
|---|---|---|
| Regression test works | **Red-green cycle verified** | **Test passes once** |

**A test that has only ever passed is not evidence.** It has to have been seen to fail for the right
reason. That is why [D-30](DECISIONS.md) demands a negative case beside every positive one, and it was
reached here by somebody debugging web software, not Revit fragments.

**Two more rows transfer without translation**, and both are about this exercise rather than about
Revit:

- *Bug fixed* requires **testing the original symptom**, not *"code changed, assumed fixed"*.
- *Agent completed* requires **a VCS diff showing changes**, not *"agent reports success"*.

`systematic-debugging` carries the companion rule — *"ALWAYS find root cause before attempting fixes.
Symptom fixes are failure"* — which is why `"flake"` is not a root cause and why the five heuristic
false positives in [32 §4](32-master-architecture-reconciliation.md) were each traced rather than
patched.

#### Where Heron's version is stronger, and it is the same lesson as §5.1

**Superpowers' rule is prose addressed to a model.** It is well written, it is emphatic — *"Skip any
step = lying, not verifying"* — and it depends entirely on the model reading it and choosing to comply.

[D-30](DECISIONS.md) is enforced by a tool. A proof is a **format**: a positive case, a negative case,
a named model and a staleness fingerprint, checked by
[`batch-prove.py`](../tools/batch-prove.py) rather than remembered. A fragment without one stays
`DRAFT`, and no amount of confidence changes the field.

**That is §5.1's finding again from the other side.** ECC showed Heron a rule it holds in prose that
somebody else holds in code (the hooks). This shows Heron a rule it holds in code that somebody else
holds in prose. **Both directions are the same lesson: a rule in code beats a rule in prose**, and the
score across the two projects is one each.

**Nothing is adopted.** The failures table is a good checklist and Heron's proof format already
encodes the rows that apply to it.

**Decision: unchanged — Adopt concept, already held.** Promoted into §1 as the **sixth** agreement.

---

### 5.9 `K-Dense-AI/scientific-agent-skills`

**Read at** `9cf7d9aea7d84754db4c167ab04b299d33c444bc`, committed 2026-09-07.

**Opened:** `LICENSE.md`, `README.md`, the five per-skill licence files, `scan_skills.py`, and the
tree — **163 skills, 495 MB.**

**This row's warning was the vaguest thing in the matrix and it is now the sharpest finding in the
pass.** The row said *"individual skills carry their own licences, which the repository says
explicitly."* It does not say it explicitly. It says the opposite.

#### A README that says "use freely" over a tree that says "all rights reserved"

`README.md` §License:

> This project is licensed under the **MIT License**… ✅ **Free for any use** (commercial and
> noncommercial) ✅ **Open source** — modify, distribute, and use freely

`LICENSE.md` at the root is indeed MIT, K-Dense Inc. But **five of the 163 skills carry their own
licence file**, and four of them say this:

| skill | its own licence file says |
|---|---|
| `skills/docx/LICENSE.txt` | **© 2025 Anthropic, PBC. All rights reserved.** |
| `skills/xlsx/LICENSE.txt` | **© 2025 Anthropic, PBC. All rights reserved.** |
| `skills/pptx/LICENSE.txt` | **© 2025 Anthropic, PBC. All rights reserved.** |
| `skills/pdf/LICENSE.txt` | **© 2025 Anthropic, PBC. All rights reserved.** |
| `skills/pacsomatic/LICENSE` | MIT — but **a different copyright holder** (Beifang Niu) |

**"All rights reserved" is not MIT and it is not "use freely."** Anybody who read the README, trusted
it, and vendored one of those four directories would be redistributing an all-rights-reserved work
while believing they had permission.

This is the incoming document's own §2 — *"code reuse must follow its license and **must never happen
accidentally**"* — happening in the wild, and **a landing page cannot show it.** The correction to the
row is not a detail: the repository does not warn you, and the only thing that does is `find . -iname
"LICENSE*"`.

**And their own tooling cannot see it either.** `scan_skills.py` is a real scanner — findings with
severity, per-skill reports, results cached by content hash, the same idea as
[`heron_embed.py`](../brain/heron_embed.py). **It checks security. Nothing in it, or in their CI, looks
at a licence at all.**

#### What this means for Heron, which is going public with an import path

[17](17-open-source-and-distribution.md) publishes Heron under Apache 2.0 ([D-08](DECISIONS.md)), and
[09](09-skills-and-fragments.md) plans **community packages** — [Golden Rule 19](14-golden-rules.md)
names them as a source Heron reads. So Heron will one day import knowledge somebody else wrote, and its
users will redistribute what Heron ships.

**None of Heron's 19 tools mentions a licence.** `check-metadata.py` checks headers,
`check-structure.py` checks boundaries, `check-docs.py` checks claims. Nothing checks what an imported
package permits — which is exactly the position this repository is in, with 163 skills and a scanner
that looks at everything except the thing that would have caught this.

Recorded as **[Q-53](OPEN-QUESTIONS.md)**.

#### And it found a stale claim in Heron's own documents

Checking whether Heron has a licence gate meant reading [17 §4](17-open-source-and-distribution.md)'s
*"repository files required before going public"* table. **Five of its eight rows were wrong.**
`LICENSE` was marked *"⏳ pending Q-27"* — a question answered as [D-08](DECISIONS.md) and read back on
2026-09-06, over a `LICENSE` file that has been on disk for days. `CONTRIBUTING.md`,
`CODE_OF_CONDUCT.md`, `SECURITY.md` and `.github/ISSUE_TEMPLATE/` all exist and were all marked pending.

Corrected, with `NOTICE` added — Apache 2.0 makes that file mean something — and the two genuinely
outstanding rows (`CHANGELOG.md`, `.github/workflows/`) left marked and pointed at
[Q-49](OPEN-QUESTIONS.md).

**[D-54](DECISIONS.md) for the third time this week**, and worth noticing *how* it was found: not by
auditing Heron, but by asking someone else's repository a question and then asking the same question
here.

**Decision: unchanged — Adopt concept, already held.** The library shape is
[`brain/skills/`](../brain/skills/)'s. The licence warning is upgraded from vague to demonstrated, and
becomes [Q-53](OPEN-QUESTIONS.md).

---

### 5.10 `ai-boost/awesome-harness-engineering`

**Read at** `3be1ff8dde95dd76facf93e98a71672a1c188edc`, committed 2026-09-09. **CC0** — confirmed from
the `LICENSE` file, so there is nothing to honour and nothing to carry.

**Opened:** `README.md` (649 lines, **235 unique repository links**), its *Evals & Verification* and
*Human-in-the-Loop* sections in full, `templates/`, `verify_urls.py`.

**The row said *"Research further, as an index only"*, with the warning that treating an index as a
dependency is how a list becomes a roadmap. That warning is honoured here literally: of 235 links,
this pass names three and pursues none.** The other 232 are deliberately not listed — a research
document that reproduces an index has become the index.

*(One small thing worth noticing in passing: `verify_urls.py` exists to check that every link in their
README still resolves. That is [`check-docs.py`](../tools/check-docs.py)'s job, done by somebody else,
for the same reason — a document that quietly stops being true is worse than one that was never
written.)*

#### The three that touch a named Heron question

**1. "Approve with changes" — and it goes straight to [Q-50](OPEN-QUESTIONS.md).** The entry on the
Claude Agent SDK's approval mechanics names the pattern:

> The **"approve with changes"** pattern — modifying tool input before execution — is the reference
> design for safe-by-default harnesses that don't simply block or permit.

**Heron's approval is deliberately binary.** [`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs)
mints a token for one preview, refuses a token that does not match it, and re-counts against the live
model before writing. That is not an oversight to be relaxed — it is the guarantee. **But binary is
about the token, not about the conversation.** A modeller who says *"yes, but 150 not 200"* is today
starting over, and the Heron-shaped version of "approve with changes" is not executing something
modified: it is **taking the correction and producing a new preview immediately**, with the same
guarantee intact. Added to [Q-50](OPEN-QUESTIONS.md).

**2. The lucky pass, which is [D-30](DECISIONS.md) for the third time.** *AgentLens* found that **up to
23.2% of passing runs are "lucky passes"** — regression cycles, blind retries, missing verification —
and that model rankings move by five positions when scored on *how* the answer was reached rather than
on whether the test went green. That is why a Heron proof is a positive case, **a negative case**, a
named model and a fingerprint, rather than *"it worked."*

**3. One eval requirement Heron gets for free, and should know it has.** Anthropic's *eval awareness*
entry documents a model identifying the benchmark by name and decrypting the answer key, and concludes
that **evaluating in a network-isolated environment is now a harness requirement rather than hygiene.**
Heron's proofs run against a local Revit model on a machine that needs no network
([D-26](DECISIONS.md)). **The isolation other people have to engineer is the environment Heron already
runs in** — worth writing down so it is defended rather than traded away later.

#### One thing the index frames well and Heron answers differently, on purpose

Martin Fowler's three postures — humans **outside**, **in**, or **on** the loop — with the argument that
*"humans on the loop"* (maintaining the harness rather than reviewing outputs) is the only one that
scales with agent throughput.

**Heron puts the human *in* the loop for every write, deliberately** ([Golden Rule 9](14-golden-rules.md),
[D-32](DECISIONS.md)'s read-only v1), and that is not a failure to scale. Throughput is the wrong
measure: a modeller approving a change is not reviewing an agent's output, they are **approving a change
to their own building model**, which is the thing they are professionally responsible for. Recorded so
the choice reads as a choice.

*(Anthropic's autonomy study in the same section is the honest counterweight: experienced users move
from per-action approval toward intervention-only oversight as trust builds. It is a real finding and it
is about developers approving their own tools, not about a coordinator approving a change to a
federated model at 4pm on an issue day.)*

**Decision: unchanged — Research further, as an index only.** Three entries recorded, none adopted, 232
deliberately not listed.

---

### 5.11 `PrimeIntellect-ai/prime-agent`

**Read at** `bcdcd6e65e10959c9904ec4467747528303493b0`, committed 2026-09-09. MIT.

**Opened:** `packages/agent/src/types.ts`, `packages/agent/src/agent.ts`,
`packages/coding-agent/src/cli/daemon-command.ts`, `LICENSE`, `README.md`.

**The row's first claim is not in the code.** It said *"bounded autonomous mode — **explicit token and
time budgets**"* and decided *"Research further — only the **bound**."* There is no such budget. Every
`maxTokens` in the tree is a **provider parameter**, and `thinkingBudgets` is a model's thinking-token
setting, not a bound on autonomy.

**What is actually there is better, and it is [D-01](DECISIONS.md) written as a type:**

```ts
shouldStopAfterTurn?: (context: ShouldStopAfterTurnContext) => boolean | Promise<boolean>;
```

**The harness refuses to decide when to stop.** It hands the decision to whoever embedded it, with
everything needed to make it — the turn's assistant message, its tool results, the full context, and the
messages this invocation produced. There is no number in it because a number would be the harness
choosing on the caller's behalf.

**For Heron that closes the item rather than advancing it.** [D-01](DECISIONS.md) gives the host the
loop; a stop predicate belongs to whoever runs the loop; **Heron should not grow a bound.** §4's *"only
the bound, and only once [19 §2](19-context-and-cost.md)'s budgets are agreed"* is corrected to **there
is no bound here to take** — and the *shape* of what is here says the same thing Heron already decided.

#### Session survival is real, and it means something different in a Revit process

The daemon is genuine: `owned-session-worker.ts`, and `daemon` commands including `detach` and
`restart`. A session outlives its client.

**Heron cannot copy that, and the reason is the interesting part.** A Heron session pins **which Revit**
(Step 5) and **which document inside it** ([Golden Rule 20](14-golden-rules.md)) — and both can change
while nothing is watching. Revit can be closed. The active document changes the moment somebody clicks
another window.

So **a Heron session that survives a disconnect is a session whose pins may have expired**, and restoring
its state faithfully would restore a claim about a model that is no longer true. Survival for Heron
therefore means **re-verifying the pins, not restoring the state** — which is exactly why
[`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs) re-counts against the live model instead of
trusting the preview it holds. [23](23-heron-kernel.md)'s checkpoints are the right shape and this is the
constraint they have to respect.

#### A provenance note, and it is the third of its kind

`LICENSE` reads **"Copyright (c) 2025 Mario Zechner"** in a repository published by PrimeIntellect. MIT
either way and nothing turns on it — but it is the **third** repository in this pass whose copyright line
names somebody other than the organisation publishing it, after ECC carrying `zunoworks/gateguard` and
OpenViking carrying `nanobot`. That is now a pattern rather than a coincidence, and it is recorded as a
reading rule in [§3](#3-the-licence-finding-which-is-the-one-that-could-have-cost-something).

**Decision: changed — from *Research further* to *nothing to take, and the shape confirms
[D-01](DECISIONS.md)*.** Session survival stays [23](23-heron-kernel.md)'s, with the pin constraint
written down.

---

### 5.12 `karpathy/llm-council`

**Read at** `92e1fccb1bdcf1bab7221aa9ed90f9dc72529131`, committed **2025-11-22**.

**Opened:** `README.md`, `backend/council.py`, and a search of the whole tree for a licence.

#### The licence cell was wrong, and this is the one row where that was worth catching

**There is no licence.** No `LICENSE` file, nothing in `pyproject.toml`, nothing in the README. The
matrix said *"MIT — compatible"* and that was assumed, not read.

**No licence means all rights reserved by default.** The author's own framing is generous —

> I'm not going to support it in any way, it's provided here as is **for other people's inspiration**
> and I don't intend to improve it.

— but *"for inspiration"* is a statement of intent, not a grant. **So the same discipline applied to
OpenViking applies here for the opposite reason:** read it to understand it, take the idea, transcribe
nothing. Which is what the row's decision already said — *"take the anonymising, not the council"* — so
the plan survives the correction. **The cell did not.**

**It is also nearly a year old and explicitly unmaintained** — last commit 2025-11-22, and *"99% vibe
coded as a fun Saturday hack."* Nothing here depends on it, so nothing follows; recorded because a
matrix row that ages quietly is the failure this whole document keeps finding elsewhere.

#### The idea is confirmed, and it is incomplete in a way that matters to Heron

`council.py` does exactly what the README says. Each response is relabelled `Response A`, `Response B`,
`Response C`, the judge sees only the labels, and a `label_to_model` map is kept on the side and never
shown.

**But the labels are assigned by position, and nothing shuffles them.** The list is zipped straight
against `A, B, C…` in the order the responses arrive. **Anonymised, yes — positionally stable, also
yes.**

That is a real gap and it lands precisely on what [§4](#4-what-actually-comes-out-of-this-ranked) ranked
as the sharpest new idea. Heron's use would be **Shadow Mode** — comparing a candidate against
production ([D-39](DECISIONS.md)). If the candidate is always `Response B`, then any **position bias** in
whatever judges them — and preferring the first option is a well-known one — stops being noise and
becomes a **systematic bias in favour of production**, which is the exact bias Shadow Mode exists to
detect.

**So §4's item 2 is sharpened rather than confirmed: anonymise *and* shuffle.** Hiding the name removes
one bias and leaves another sitting in the same place. Recorded in §4.

**Decision: unchanged in substance — Adapt concept, take the anonymising.** The licence cell is
corrected from *MIT* to **no licence, all rights reserved**, and the idea now carries the shuffle.

---

### 5.13 `alibaba/open-code-review` and `alibaba/aacr-bench`

**Read at** `14b84f08a3af7f042d702c721f21f7952d841970` (2026-09-09) and
`68a569759289a83654a59d06db2a72910edf0a4a` (2026-08-24).

**Both licence cells said *"read the repository's own licence before any reuse."* Done: both are
Apache-2.0**, the same licence as Heron ([D-08](DECISIONS.md)), so both are compatible. Applying
§3's per-directory rule found one more licence file — `extensions/vscode/LICENSE` — **also Apache-2.0**.
Two cells that were open questions are now answers.

**Opened:** `ASSURANCE_CASE.md`, `README.md` §*Core Design*, and `aacr-bench`'s `dataset/` and
`evaluation/`.

#### The row's principle is confirmed, in better words than the row used

> For review steps that **must not go wrong**, engineering logic — not the language model — guarantees
> correctness.

That is [19 §5](19-context-and-cost.md) and [02 §6](02-architecture-overview.md), said more sharply than
either. And one of its four deterministic pieces is a claim Heron acts on without having stated:
*"template-engine-based rule matching is **more stable and predictable** than purely language-driven
rule guidance."* Heron's rules are the fragment contract and
[`heron_context.py`](../brain/heron_context.py)'s budget — matched in code, never described to a model
and hoped for.

#### `ASSURANCE_CASE.md` is the finding, and it has already been acted on

Its first table is not a lifecycle. It is a list of **actors with trust levels** — and one row is
Heron's open question written as a trust level rather than as a guard:

> **Git repository — Semi-trusted — diffs may contain adversarial content.**

**Heron has a trust model and it answers a different question.** [24](24-trust-model.md) collected six
competing vocabularies and replaced them with two axes — *how proven is this artifact* and *where did it
come from*. **Neither asks how much Heron may believe whatever is talking to it right now**, which is
why [Q-51](OPEN-QUESTIONS.md) was hard to phrase: there was no vocabulary for *the source of this text*.

**So this one was built rather than parked.** [24 §7](24-trust-model.md) is a new table of **who is
speaking** — the modeller, the host, the pipe, the model's own content, a linked document, the API's
answer, an imported package, an indexed standard, the network. **Every row is derived from a rule that
already exists**, and it is explicitly *not* a third vocabulary: an artifact has a lifecycle and a
source, a speaker has neither.

Two things the table makes visible that the prose did not:

- **Exactly one cell is undecided** — the indexed standard, which is [Q-51](OPEN-QUESTIONS.md), opening
  on the day the RAG index exists.
- **The last row is a strength that could be traded away by accident.** Most of their assurance case —
  TLS, a semi-trusted provider API, DNS rebinding against a local viewer — is attack surface **Heron
  does not have**, because it makes no model calls and needs no network. Any future feature that puts
  Heron on the network is not adding a feature; it is adding that whole table's worth of rows.

#### `aacr-bench` answers *"what shape does a benchmark take"* — and its shape is D-30's

`dataset/` holds exactly two files:

```
positive_samples.json
negative_samples.json
```

**A benchmark split into positive and negative cases**, which is [D-30](DECISIONS.md)'s structure
arriving for the **fourth** time in this pass — after `superpowers` ([§5.8](#58-obrasuperpowers)),
AgentLens ([§5.10](#510-ai-boostawesome-harness-engineering)), and Heron's own proof format.

Its build process is the other half of the answer: **200 real pull requests, 50 projects, 10
languages**, quality assured by *"GitHub human comments → LLM enhancement → expert multi-round
cross-annotation → consistency validation."*

**The row already said the cases must be real Revit tasks that only the owner can author, and that is
unchanged.** What is new is that the *process* is nameable: an expert annotates, more than once, and
the annotations are checked against each other. For Heron that is proving fragments against a real
model — which is the critical path, and which the row correctly refuses to put anything in front of.

**Decision: both unchanged.** `open-code-review` — *Adopt concept, already held*. `aacr-bench` — *Adapt
the shape of a benchmark, never its cases*. Two open licence cells are now answered, and the assurance
case produced [24 §7](24-trust-model.md).

---

### 5.14 `headroomlabs-ai/headroom`

**Read at** `e67b3c8a29443a60d6b0018fb22f525c5cd7e709`, committed 2026-09-06.

**Opened:** `README.md`, `LICENSE`, `NOTICE`, `plugins/headroom-oauth2/LICENSE`,
`sbom/headroom-python-licenses.csv`.

**Licence correction — the third in this pass.** The row said *"MIT — compatible."* It is
**Apache-2.0**, with a `NOTICE` file. Still compatible — it is Heron's own licence
([D-08](DECISIONS.md)) — but Apache 2.0 §4(d) makes `NOTICE` **travel with any redistribution**, which
MIT does not. A cell that said MIT would have understated an obligation rather than a restriction. Their
per-plugin licence is exemplary practice worth copying: an **SPDX identifier** plus one line saying it
matches the upstream licence.

#### `sbom/headroom-python-licenses.csv` is [Q-53](OPEN-QUESTIONS.md)'s mechanism, already built

**330 dependencies, each with `Name, Version, License, URL`, generated as a build artifact.**

That is exactly what [Q-53](OPEN-QUESTIONS.md) is asking whether Heron needs — and it sharpens the
question usefully, because **Heron's dependency SBOM would be nearly empty.** Heron is local, offline
and dependency-light on purpose ([D-24](DECISIONS.md), [D-26](DECISIONS.md)). **The inventory Heron needs
is not of its libraries but of its imported knowledge** — community packages, which
[Golden Rule 19](14-golden-rules.md) already names as a source Heron reads and
[24 §7](24-trust-model.md) now records as semi-trusted. Same artifact, different contents.

#### The row's principle is confirmed, and one clause makes it Heron-compatible

> Headroom compresses everything your AI agent reads — tool outputs, logs, RAG results…
> **Compression runs on your machine; no prompt or file content is sent anywhere to be compressed.**

**Local.** That was the unstated prerequisite: a compressor that phones home is [D-26](DECISIONS.md)
denied, whatever it saves. Reversibility is confirmed too — *"originals are cached locally and retrieved
on demand"* — which is the half [§4](#4-what-actually-comes-out-of-this-ranked) already ranked as the
one worth taking.

And the architecture answers a question the page-level row left vague: **a `ContentRouter` picks a
compressor by content type** — one for JSON, one for source code (AST-based), one for prose. Not one
compressor with a threshold. That matters for Heron, where a fragment's `.cs` body, a test case list and
an API surface are three different kinds of text and only one of them is prose.

#### The idea Heron's own part order sits backwards for

> **CacheAligner** flags volatile content that would bust a provider KV-cache prefix. **It never
> rewrites prompts.**

**Volatile content early in a prompt destroys the cache for everything after it.** Now look at
[`heron_context.py`](../brain/heron_context.py)'s budget, whose comment says *"order is the order a
reader gets them"*:

```
GENERATION: (REQUEST, SITUATION, CAPABILITY_PART, EXCLUDED, NEIGHBOUR, TESTS, API)
```

**The two most volatile parts are first.** `REQUEST` changes every single time. `SITUATION` changes the
moment the modeller clicks another window. The stable material — a neighbouring fragment's source, its
tests, the API surface — is **last**, behind them.

**For a human reading the rendered context that order is right**, and it was chosen for that reason.
**For a prefix cache it is the worst possible order.**

**Nothing is changed and no question is opened, and the reason is [D-58](DECISIONS.md).** Heron makes no
model calls. Whether any cache exists depends entirely on how the **host** assembles its own prompt
around Heron's blob, and Heron cannot see that. Reordering for a cache Heron cannot observe would be
optimising against a guess — and it would trade a real property (a reader gets the request first) for a
speculative one. **Recorded so that if the host ever reports cache behaviour, the cause is already
written down.** It belongs with [19 §2](19-context-and-cost.md)'s budgets, beside
[§5.6](#56-tirth8205code-review-graph)'s token-estimate note.

**Decision: unchanged — Research further**, and the prerequisite is unchanged too: nothing before
[19 §2](19-context-and-cost.md)'s budgets are agreed. The licence cell is corrected **MIT → Apache-2.0
with a NOTICE**.

---

### 5.15 *Claude CEO / CEO-style agent* (§10.15) — searched properly, still not identified

**Nothing was cloned, because nothing could be identified.** The incoming §10.15 says *"first identify
the exact repository intended… **study only if verified**"*, and the page-level pass recorded that it
could not be. **The file-level pass owes that row a better answer than "I could not find it", so the
search is written down instead of the failure.**

**What was searched, and what came back:**

| query | result |
|---|---|
| `claude-ceo in:name` | **20 repositories.** Top by stars: `Claudefarid/claude-ceo-skill` — **4 stars** |
| `ceo in:name claude code agent stars:>10` | **0 results** |
| `CEO agent orchestrator delegation subagents in:description stars:>50` | **0 results** |

**That is the finding: there is no well-known project of this name.** This was not a failed lookup of
something obvious — **no repository matching the description has more than ten stars.** The field is
about twenty small personal projects with no shared referent.

**The closest match by description**, recorded so the owner has something concrete to confirm or reject:

> [`nhangen/claude-ceo`](https://github.com/nhangen/claude-ceo) — *"Autonomous CEO agent for Claude
> Code. Reads Obsidian vault, prioritizes work, delegates to subagents, learns from corrections."*
> **2 stars.**

Its description does line up with §10.15's list — *top-level delegation, task ownership, specialist
routing, progress and state management*. **It is not confirmed and it is not studied**, because a
description that matches is not the identification the incoming document asked for, and guessing here
would put an unverified project into a matrix that has just spent fifteen entries correcting verified
ones.

**This is the one row only the owner can close, and it costs him one line.** Either a link, or *"drop
it"* — and if it is dropped, nothing is lost: [D-01](DECISIONS.md) already gives Heron the
orchestrator-in-the-host pattern this row was reaching for, which is what the original row said and
what fourteen file-level readings have not contradicted.

**Decision: unchanged — NOT IDENTIFIED. Nothing studied, nothing claimed**, and now with a repeatable
search behind the statement rather than a single failed guess.
