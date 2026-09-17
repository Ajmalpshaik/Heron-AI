# Proposals — Gaps, Additions & Ideas

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | The owner, deciding what to accept, reject or defer |
> | **Authority** | **Nothing here overrides the specification.** These are findings, not decisions — a accepted one becomes a [DECISION](DECISIONS.md) |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | A reviewed gap, risk or idea. **Raw ideas go to `work-notes/ideas/` instead**, until they are worth putting in front of somebody |
> | **Its numbers** | Derive the open ones: `python tools/owner-queue.py` |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


> Engineering review of the Master Specification, carried out 2026-08-27.
> Covers **[Part 1](00-master-specification.md)** (the platform) and
> **[Part 2](00b-master-specification-agent-os.md)** (the Agent Operating System).
> Nothing here overrides the spec. These are findings for the owner to accept, reject or defer.
>
> **Status legend:** 🔴 blocking · 🟠 important · 🟡 worth doing · 🔵 idea · ✅ closed by Part 2

---

## Part 0b — What the Additional Requirements (Part 4) changed

Part 4 is the most substantive of the four after Part 1. It adds real architecture, closes four gaps the
review had raised, resolves one open tension — and creates one problem that had to be solved before
anything can be built.

### ✅ Gaps closed by Part 4

| Was | Closed by | Detail |
|---|---|---|
| **A7** — no preview before modification | **§11 Simulation / Dry Run** | *"I found 126 ducts. This operation will modify 126 elements."* Independently specified, and it is exactly what proposed [Golden Rule 17](14-golden-rules.md) asks for. Gap closed. |
| **A6** — undo and transactions never mentioned | **§12 Transaction Safety Agent** | A dedicated agent for transaction handling, closure, rollback, failure handling and document state, *separate from general Revit API logic*. The vehicle for Golden Rule 16 now exists. Half closed — the **one-Ctrl+Z guarantee** still needs stating. |
| **A10** — permission gate placement unspecified | **§29 Security Boundary** | *"AI → Permission Layer → Tool Validation → MCP → Revit. The AI requests an operation; the platform decides whether that operation is allowed."* Confirms the position taken in [12 §3](12-security-and-permissions.md) exactly. |
| *(review proposal B9)* — offline mode | **§31 Offline / Degraded Mode** | Now an official requirement. |
| *(review recommendation)* — golden-file regression testing | **§28 Golden Test Library** | The mechanism proposed in [13 §4](13-testing-and-quality.md), now official. |

### ✅ Open tension resolved

**[Q-30](OPEN-QUESTIONS.md) — Model Router vs Claude Code as host — is answered by §24.**

The **AI Model Abstraction Layer** (`Heron AI Interface → Model Router → Provider Adapter → Model`)
separates two things that were conflated: Heron always declares *intent* ("this needs strong
reasoning"); *resolution* to a specific model is pluggable and belongs to the host when hosted.

It also makes §25's local/cloud routing a configuration choice rather than an architectural one, which
matters directly for confidentiality ([Q-12](OPEN-QUESTIONS.md)): a project marked confidential selects
a local provider adapter and nothing above that layer needs to know. → [23 §8](23-heron-kernel.md)

### 🆕 The best new ideas in Part 4

| # | Idea | Why it matters |
|---|---|---|
| **P14** | **Heron Kernel** (§1) | The largest structural addition in any document. Without it, N modules depend on N modules and the dependency graph grows quadratically. With it, [Golden Rule 15](14-golden-rules.md) becomes achievable rather than aspirational. The discipline that keeps it from becoming a god object: **the Kernel is plumbing, never intelligence** — it must compile with no Revit knowledge in it at all. → [23 §1](23-heron-kernel.md) |
| **P15** | **Evidence System** (§6) | *"This fragment was selected because it supports Revit 2020–2027 and has 98 successful executions."* The single best idea in Part 4. It is what turns Heron from a black box into something a professional can sign off on — and it is nearly free, because every field already exists in the registries. → [23 §6](23-heron-kernel.md) |
| **P16** | **Workflow Engine separate from Orchestrator** (§3) | The Orchestrator decides *what* should happen; the Workflow Engine ensures *it happens correctly*. Retry, timeout and rollback logic written once instead of reinvented in every pipeline. |
| **P17** | **Checkpoints and resume** (§20, §21) | The new-tool pipeline is 18 stages, several of them expensive T3 loops. Failing at Build and discarding twelve completed stages costs real money. Makes *"Continue."* a first-class command. → [23 §4](23-heron-kernel.md) |
| **P18** | **Prompt / Instruction Registry** (§26) | Underrated and cheap. Scattered prompts are the most common reason an AI system becomes unmaintainable — behaviour changes and nobody can find which string caused it. |
| **P19** | **User Intent Memory** (§14) | *"What the user normally likes"* vs *"what the user asked this time."* Temporary instructions must not silently become permanent preferences. A subtle failure mode, correctly identified. |
| **P20** | **BIM QA as a third QA type** (§18) | Code QA ≠ Revit QA ≠ BIM QA. Naming, parameters, categories, families, levels, worksets, modelling rules, MEP connectivity. This is a major capability in its own right, not a test stage. |
| **P21** | **Revit Context Agent** (§13) | Auto-detect document, version, view, selection, links, worksets, phase, design option. *"Removes unnecessary questions"* — which is Golden Rule 1 applied to conversation. |
| **P22** | **Safe Mode** (§35) + **Feature Flags** (§36) | Disable recently installed components, return to last known-good. Pairs naturally with Shadow Mode: a flag set to `TEST` is how a shadow component gets exercised without its output being used. |
| **P23** | **Secret Management** (§30) + **Supply-chain security** (§43) | Both become urgent the moment the repo is public ([D-07](DECISIONS.md)) and Heron can install external components. |

### 🔴 The problem Part 4 created — and its resolution

**Six overlapping status vocabularies now exist** across the four documents, all describing how much
Heron trusts something. Part 4 added two of them (§38 trust levels, §47 knowledge levels) on top of the
four already present.

This is not a tidiness complaint. Retrieval ranks by trust, promotion gates are defined per-vocabulary,
and [Golden Rule 6](14-golden-rules.md) — *experimental must stay separate from production* — is
unenforceable when "experimental" means four different things.

**Resolution:** they are not six versions of one idea; they are **two ideas repeatedly collapsed into one
linear scale** — *how far has this been proven* (advances over time) and *where did this come from*
(fixed at creation). `OFFICIAL` was never a stage past `PROVEN`; it is a source.

Separated into two orthogonal axes, all six collapse cleanly and nothing is lost — in fact more becomes
expressible, since an object can now be `PROVEN` **and** `COMMUNITY`, which no single scale could say.
Part 4 §47's four levels survive as a derived band used for ranking and for talking to users.

Full proposal: **[24 — The Unified Trust Model](24-trust-model.md)**. Needs confirmation ([Q-34](OPEN-QUESTIONS.md)).

### 🆕 New artefact: the Heron Constitution

§46 requests `HERON_CONSTITUTION.md` — rules agents must never violate, injected into every agent.

**[Written and ACCEPTED 2026-08-28: [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md) — all 30 Articles, after Ajmal asked for every one to be read out rather than tapping yes ([Q-35](OPEN-QUESTIONS.md), closed).]**

It is reconciled with the Golden Rules rather than duplicating them. The distinction is real and worth
keeping: **Golden Rules are design principles for people; the Constitution is the runtime-enforceable
subset, written as prohibitions an agent can obey or violate.** *"The platform must be modular"* is not
something an agent can violate at runtime; *"do not publish user knowledge automatically"* absolutely is.

One caveat is stated prominently in the document itself: **a rule is not enforced by being written
down.** Text in agent instructions is guidance a model can be argued out of. Every Article that can be
enforced in code is also enforced in code, at the permission boundary — which is precisely what §29 asks
for.

### ✅ A1 — confirmed from the field, not merely argued

**The Revit API threading constraint is real, and the owner's own working bridge proves it:**

> *"Revit does one thing at a time, and the AI's script runs on the same thread that draws the screen.
> While it runs, Revit is genuinely frozen. No add-in can change that; it is how Revit itself is built."*
> — [field notes](00e-field-notes-proven-bridge.md)

Four specification documents, several hundred sections, and the single hardest constraint in the
platform appears in none of them — but it is written plainly in a note describing software that already
runs. That is the difference between designing a system and operating one.

A1 is no longer a review finding. It is settled fact, and [D-09](DECISIONS.md) is validated.

### 🆕 P24 — the stale read *(field-proven, never specified)*

> *"The real danger is not the freeze, it is the stale read: the AI reads the model, you change
> something, and a later step acts on the old picture."*

Not in any specification, and it generalises further than the note claims — the model can also change
because another user synced, a link reloaded, or Heron's own earlier step changed it. Becomes proposed
[Golden Rule 21](14-golden-rules.md), and it forces a rule that matters: **an accepted preview must be
re-counted before executing.** A preview accepted for 247 elements must never silently run on 261.

### 🔴 P25 — document binding *(field-proven, the sharpest hazard found so far)*

> *"One Revit can hold several projects open. Picking the Revit is only half of it — commands land on
> whichever project window is in front, and that changes when you click."*

The failure is silent and nobody makes a mistake:

```text
1. Chat bound to Revit pid 24312.  Tower-A.rvt in front.
2. "Move all ducts up 200 mm."
3. User clicks over to Tower-B.rvt to check something.
4. Heron resolves "the active document" -> Tower-B.rvt.
5. 247 ducts move in the wrong building.
```

Every step individually correct. Model still damaged. Becomes proposed
[Golden Rule 20](14-golden-rules.md), and moves into Phase 0 scope — cheap now, a silent hazard if
deferred. → [25 §4](25-multi-session-and-binding.md)

### 🔴 Still open after all four documents

**Unchanged.** Four documents, and the single hardest technical constraint in the platform is still
absent from all of them:

| # | Gap | Where handled |
|---|---|---|
| **A1** | **Revit API threading** — an MCP server cannot call the Revit API at all; every operation must marshal through `ExternalEvent` onto Revit's main thread | [03 §4](03-heron-revit.md), settled in [D-09](DECISIONS.md) |
| **A9** | **Data egress** — §25 and §30 cover local models and secrets, but not *what project content reaches an AI provider* | [12 §4](12-security-and-permissions.md), [Q-12](OPEN-QUESTIONS.md) |
| **A11** | **How a Revit test actually executes** — §27 and §28 specify what to test and that a golden library exists, not how a test runs inside a GUI application that cannot run headless | [13 §3](13-testing-and-quality.md), [Q-14](OPEN-QUESTIONS.md) |

Proposed Golden Rules 16 and 19 also remain unstated in any document — the **one-Ctrl+Z guarantee** and
**no permission escalation from untrusted text**. §12 and §29 build the machinery for both; neither
states the rule.

### ✅ And one strong validation

Part 4 closes with:

> *"Don't start by building 100+ agents. Build the Kernel, Registry, Orchestrator, Workflow Engine, RAG,
> Fragment/Skill system, MCP/Revit layer, and QA foundation first."*

This is the same conclusion as [ROADMAP.md](ROADMAP.md) reached from the other direction, and it is now
an explicit instruction rather than a review recommendation. The roadmap has been updated to name these
components directly.

---

## Part 0a — What the Master Handover Baseline (Part 3) changed

Part 3 is a **consolidation**, not a third set of requirements. Roughly 90% of it restates Parts 1 and 2.
Three things in it are genuinely new, and one of them changes the constitution.

### 🔴 The Golden Rules were replaced — ten became fifteen

[Baseline §78](00c-master-handover-baseline.md) supersedes [Part 1 §72](00-master-specification.md).
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
Full mapping in [14 — Golden Rules](14-golden-rules.md).

### 🆕 Other genuinely new content in Part 3

| # | Item | Why it matters |
|---|---|---|
| **P9** | **§1 — "do not assume every architectural idea is already implemented; future capabilities must be explicitly marked"** | A governance instruction, and the right one. It is the same position as [D-00](DECISIONS.md). Every document in this repository already separates *specified* from *decided* from *built*; this makes that separation an explicit requirement rather than a convention. |
| **P10** | **§42 — Reference Update Agent must verify imports, references, metadata, registry, documentation and relationships after any rename or move** | Stated as a hard requirement for the first time. *"No broken references should be introduced."* This is what makes the Auto Rename Agent safe — see [06 §7](06-heron-platform.md). |
| **P11** | **§32 — Compatibility Agent explicitly *combines* the results of the Revit Version, Revit API, .NET and Dependency agents** | A cleaner decomposition than either earlier document. One agent per question, one agent to reconcile them. |
| **P12** | **§57 — "Agents should only receive the permissions they require"** | Least privilege, stated explicitly for the first time. Should become a required field in the agent registry, not a guideline. |
| **P13** | **§20 — Trust Evaluation added as a retrieval pipeline stage** | Adopted. See [20 §1](20-knowledge-trust-and-conflict.md). |

### ⚠️ One thing in Part 3 to reject

**§20 moves metadata filtering *after* all three searches.** Parts 1 and 2 had it earlier in the
pipeline, which is correct. Filtering last means embedding and searching a corpus that is about to be
discarded — the most wasteful possible ordering, and it defers scope isolation
([Golden Rule 5](14-golden-rules.md)) from query time to ranking time.

The adopted order keeps Part 3's Trust Evaluation stage and Parts 1–2's filter placement.
See [20 §1](20-knowledge-trust-and-conflict.md).

### Still open after Parts 1 and 2

Unchanged. Neither part addresses:

**Revit API threading** · **undo** · **preview before modify** · **what data leaves the machine to a model provider** · **how a Revit test actually executes**.

Proposed Golden Rules 16–19 remain necessary. Part 3 strengthens the case for them.

---

## Part 0 — What Master Specification Part 2 changed

Part 2 is not more of Part 1. Part 1 said *which agents exist*; Part 2 says *how they are governed*.
It closes four gaps the Part 1 review raised, and adds several mechanisms that were missing.

### ✅ Gaps closed by Part 2

| Was | Closed by | Detail |
|---|---|---|
| **A3** — "agent" conflated with "LLM call"; 150 agents would be unusably slow and expensive | **§17 Model Router** + **§59 Cost Optimization** + **§83** | Part 2 arrives at the same conclusion from three directions: route cheap work to cheap models, never generate code when a proven fragment exists, and run *only required agents*. Combined with the T1/T2/T3 tiering, this is settled. → [19](19-context-and-cost.md) |
| **A17** — retrieval was vector-only, which handles exact technical tokens badly | **§19 RAG Operating System** | Specifies semantic **and** keyword search **and** metadata filtering, plus conflict detection. This is the hybrid stack recommended in [05 §4](05-heron-brain.md). → [20 §1](20-knowledge-trust-and-conflict.md) |
| **A12** — autonomous agent creation had no hard safety boundary | **§10 Shadow Mode** + **§54 autonomy proportional to risk** | Shadow Mode is a better answer than the proposed human-approval rule alone, because it produces *evidence* rather than a signature. Recommendation is now: keep both. → [18 §4](18-agent-operating-system.md) |
| *(implicit)* — vector DB risked becoming the only copy of knowledge | **§77 Source of Truth Principle** | States explicitly that the vector DB is an index, not truth, and must be rebuildable. Matches [05 §7](05-heron-brain.md). → [21 §7](21-resilience-and-operations.md) |

### 🆕 The best new ideas in Part 2

| # | Idea | Why it matters |
|---|---|---|
| **P1** | **Capability Registry separate from Agent Registry** (§6) | The most valuable structural idea in either document. The Orchestrator matches *capabilities*, never agent names — so agents become replaceable, retirement becomes safe, the Orchestrator stays free of domain knowledge, and capability-gap detection becomes trivial. → [18 §2](18-agent-operating-system.md) |
| **P2** | **Shadow Mode** (§10) | Safe self-evolution. A new agent runs on real requests and is scored, without its output reaching anything. → [18 §4](18-agent-operating-system.md) |
| **P3** | **Dependency Graph** (§41) | Makes "does this change break anything?" computable instead of requiring a full test sweep. This is what makes [D-05](DECISIONS.md) (8 Revit versions) and Golden Rule 4 affordable in practice. → [21 §1](21-resilience-and-operations.md) |
| **P4** | **Knowledge Conflict Resolution** (§22) | Prevents the classic decay of an accumulated knowledge base: two fragments disagree and retrieval silently picks whichever ranked higher that day. → [20 §4](20-knowledge-trust-and-conflict.md) |
| **P5** | **Emergency Stop** (§56) | Necessary as autonomy grows. Must live in the Revit add-in UI so it works when the agent system is stuck. → [21 §4](21-resilience-and-operations.md) |
| **P6** | **Workflow ID in the audit log** (§57) | The correlation key tying one user sentence to every agent, retrieval, model call and element touched. Turns "what did Heron change?" into a query. → [21 §13](21-resilience-and-operations.md) |
| **P7** | **Human approval only at meaningful boundaries** (§55) | An important corrective. Over-asking destroys a tool as surely as under-asking damages a model. → [21 §3](21-resilience-and-operations.md) |
| **P8** | **Fragment branching, one semantic fragment** (§26) | Confirms the approach already taken in [16](16-version-support-strategy.md). |

### 🔴 Still open after Part 2

Part 2 does not address these, and they remain the most important outstanding items. All are covered in
the review documents; none are in either specification:

| # | Gap | Where handled |
|---|---|---|
| **A1** | **Revit API threading** — an MCP server cannot call the Revit API at all; everything must marshal through `ExternalEvent` | [03 §4](03-heron-revit.md), settled in [D-09](DECISIONS.md) |
| **A6** | **Undo** — still never mentioned in either document. One operation must equal one Ctrl+Z | [Golden Rule 16](14-golden-rules.md) |
| **A7** | **Preview before modify** — Part 2 §54 gates *whether* an action runs, never shows *what it will do* | [Golden Rule 17](14-golden-rules.md), [03 §10](03-heron-revit.md) |
| **A9** | **Data egress** — §74 covers isolation *inside* Heron, not what leaves the machine to a model provider | [12 §4](12-security-and-permissions.md), [Q-12](OPEN-QUESTIONS.md) |
| **A11** | **How Revit tests actually run** — §43 says what to test and §48 rightly separates Code QA from Revit QA, but neither says how a test executes inside Revit | [13 §3](13-testing-and-quality.md), [Q-14](OPEN-QUESTIONS.md) |

**Golden Rules 16–19 remain necessary.** Part 2 strengthens the case for them rather than replacing them.

### ⚠️ New tensions Part 2 creates

| # | Tension | Resolution |
|---|---|---|
| **T1** | **Model Router (§17) vs Claude Code as host ([D-01](DECISIONS.md))** — Claude Code chooses the model, so a Heron-owned router partly duplicates the host | Declare routing *intent* as a cost tier in the capability registry; let the host resolve it. Route internally only for batch work later. → [19 §3](19-context-and-cost.md), [Q-30](OPEN-QUESTIONS.md) |
| **T2** | **Multi-user + Admin Mode (§72, §73) vs single-user plugin** — these describe an enterprise product with a server, user directory and enforced policy | Company knowledge as a shared **git repository**, not a server. Delivers most of the value with none of the infrastructure. → [22 §4](22-users-modes-and-extensibility.md), [Q-32](OPEN-QUESTIONS.md) |
| **T3** | **"1,000 agents or more" (§2)** | Makes the T1/T2/T3 discipline non-optional rather than merely advisable. 1,000 LLM-backed agents is not a viable system; 1,000 governed components is. → [02 §6](02-architecture-overview.md) |

---

## Part A — Gaps in Master Specification Part 1

The specification is unusually complete on *organisation* and *governance*. The gaps are almost entirely
in **execution reality** — the physics of running code inside Revit, and the economics of running many agents.

### 🔴 A1. The Revit API threading constraint is not mentioned

The Revit API can only be called from the Revit main thread inside a valid API context. An MCP server is a separate process and **cannot call it at all**. Every operation must be marshalled back through an `ExternalEvent`.

*Consequence if ignored:* the entire bridge design is wrong and has to be rebuilt.

→ [03 §4](03-heron-revit.md). **This is the single most important missing fact in the spec.**

### 🔴 A2. No transport is defined between the MCP server and the add-in

Named pipes / localhost HTTP / gRPC — the choice affects security, multi-instance handling, and the whole tool layer.

*Consequence:* cannot start building. Recommendation: **named pipes**, add-in as server, pipe name encoding Revit version + PID.

→ [03 §5](03-heron-revit.md)

### ✅ A3. "Agent" is used for both a service and an LLM call — *closed by Part 2 §17, §59, §83*

~150 agents, each an LLM call, makes "select all ducts" cost seconds and real money for what is one `FilteredElementCollector` query.

*Consequence:* the platform is unusably slow and expensive, and the architecture gets blamed for it.

Recommendation: three tiers — ~95 T1 deterministic services, ~25 T2 scoped calls, ~12 T3 agentic loops. The warm happy path should make **one** model call.

→ [02 §6](02-architecture-overview.md), [08](08-agent-catalog.md)

### 🔴 A4. How generated code actually executes is undecided

Runtime Roslyn compilation, a scripting layer, precompiled only, or hybrid. Assemblies **cannot be unloaded** from .NET Framework (Revit ≤ 2024), so runtime loading leaks on every iteration.

Recommendation: **hybrid** — scripting for DRAFT/TESTING, compiled for PRODUCTION. This maps exactly onto the fragment lifecycle already in the spec.

→ [09 §10](09-skills-and-fragments.md)

### 🔴 A5. Where the platform actually runs is ambiguous

The install flow starts with "user installs Claude Code", implying a Claude Code plugin. But the target user is a BIM modeller who should not live in a terminal.

Recommendation: build the engine host-agnostic; ship the Claude Code front-end first (nearly free), add an in-Revit pane once the vertical slice works.

→ [02 §7](02-architecture-overview.md)

### 🟠 A6. Undo is never mentioned

The user must be able to reverse anything Heron did with one keystroke.

Recommendation: **Golden Rule 16** — one operation, one named `TransactionGroup`, one undo entry.

→ [03 §6](03-heron-revit.md), [14](14-golden-rules.md)

### 🟠 A7. No preview before modification

Permission levels gate *whether* an action runs, never show *what it will do*.

Recommendation: dry run for every `MODIFY` operation — *"This will move 247 ducts up 200 mm. 12 are owned by another user and will be skipped. Proceed?"* Cheap to build, and the single feature most likely to make BIM professionals trust the tool.

→ [03 §10](03-heron-revit.md)

### 🟠 A8. The .NET 8 break at Revit 2025 is not acknowledged

Revit ≤ 2024 is .NET Framework 4.8; Revit 2025+ is .NET 8. One assembly cannot target both. Revit 2024 also moved `ElementId` to 64-bit.

*Consequence:* "support 2020–2027" is eight versions across a runtime break — a very large matrix for v1.

Recommendation: multi-target from one source tree; pick **one** version for the vertical slice.

→ [03 §8](03-heron-revit.md)

### 🟠 A9. Data confidentiality has no position

Every request potentially sends project content to a model provider. Much BIM consultancy work — especially government and defence work in Qatar — prohibits this contractually.

Recommendation: **hybrid, enforced structurally**. A project marked confidential must be *incapable* of egress: local embeddings, redaction at the boundary, hard `PUBLISH` block.

*Consequence if ignored:* the platform cannot be deployed on exactly the projects it is aimed at.

→ [12 §4](12-security-and-permissions.md)

### 🟠 A10. Permission enforcement location is unspecified

If the gate lives in the AI layer, a family name or an imported document containing instructions could in principle drive a `MODIFY` call on a live model.

Recommendation: enforce in the **add-in**, from a declared tool risk level. **Golden Rule 19** — no text Heron reads may raise Heron's own permission level.

→ [12 §3](12-security-and-permissions.md)

### 🟠 A11. Testing against real Revit has no strategy

Levels 5–7 need a running Revit. It cannot run headless, CI runners do not have it, and licensing constrains where it may be installed.

Recommendation: mock the Revit boundary for levels 1–4; in-Revit test runner for 5–7; evaluate **Design Automation for Revit** before buying a machine room.

→ [13 §3](13-testing-and-quality.md)

### ✅ A12. Autonomous agent creation has no hard safety boundary — *closed by Part 2 §10 Shadow Mode*

An AI that writes, approves and activates agents which then execute against live client models is genuinely dangerous — not dramatically, but quietly, on a submission day.

Recommendation: the system may only ever assign `PROPOSED`. A human approves activation. Always.

→ [06 §4](06-heron-platform.md)

### 🟡 A13. Product / data / derived are not separated in the workspace

The §60 tree mixes code that gets replaced on update with knowledge the user spent a year building.

*Consequence:* one update that "cleans and reinstalls" destroys the accumulated knowledge the platform exists to create.

→ [06 §2](06-heron-platform.md)

### 🟡 A14. Building on the user's machine contradicts the user promise

Step 7 of the install ("Heron builds required components") requires the .NET SDK, MSBuild and Revit SDK assemblies on a BIM modeller's locked-down laptop.

Recommendation: build in CI, ship signed prebuilt artefacts, keep the build path for Developer Mode only.

→ [07 §3](07-installation-and-update.md)

### 🟡 A15. Skill vs Fragment is never defined

Both terms are used extensively; the boundary is left implicit.

Proposed: **Skill = what the user can ask for** (BIM language, user-facing). **Fragment = how it is done** (technical, internal, reused across skills).

→ [09 §1](09-skills-and-fragments.md)

### 🟡 A16. Fragment lifecycle has states but no gates

Without explicit promotion criteria, agents will promote inconsistently and Golden Rule 6 becomes decorative.

→ [09 §5](09-skills-and-fragments.md)

### ✅ A17. Retrieval is vector-only — *closed by Part 2 §19 RAG Operating System*

BIM queries are full of exact tokens (`OST_DuctCurves`, `BuiltInParameter.RBS_*`, shared-parameter GUIDs) that embeddings handle badly. Semantic search returns the *nearly* right parameter — which in a model-modifying system is worse than nothing.

Recommendation: structured filter → hybrid keyword+vector → rank fusion → re-rank → exact-match short circuit.

→ [05 §4](05-heron-brain.md)

### 🟡 A18. Worksharing is barely addressed

Real projects are worksharing-enabled. Element ownership failures, checkout, sync timing and workset visibility are **normal outcomes**, not errors.

→ [03 §9](03-heron-revit.md)

---

## Part B — Feature ideas not in the specification

### 🔵 B1. Watch-and-learn skill capture

§34 learns from **repeated utterances**. Far more powerful: learn from what the user **does in Revit**.

The user performs a workflow manually once. Heron observes the document changes and offers: *"You selected 47 ducts by category and moved them up 200 mm. Want me to remember that as a skill?"*

This is how a BIM modeller naturally teaches a tool — by doing the job, not by describing it. It converts every user into a fragment author without any of them writing a fragment, and it directly serves Rule 1.

Technically feasible via Revit's `DocumentChanged` event. Non-trivial, high payoff.

### 🔵 B2. "What did Heron change?" report

A panel listing every change Heron made in this session — element counts, parameters touched, undo entries — with per-item revert.

Cheap (the audit log already holds it) and it is the first thing a BIM coordinator will ask for.

### 🔵 B3. Panic button — undo everything Heron did today

One control that rolls back the session's Heron transaction groups. Its existence changes how willing people are to try the tool at all.

### 🔵 B4. Explain mode for junior modellers

*"Explain what you just did."* → in BIM terms, not code: which categories, which filter, which parameter, and why.

Turns Heron from a black box into a training tool. For a company running junior modellers, that is a distinct commercial argument.

### 🔵 B5. Overnight batch mode

Run standards checks, model health reports and clash summaries across many models overnight, when Revit is free and nobody is competing for the machine.

Fits the Background Scheduler already specified, and it is the highest-value use of a licence that is otherwise idle for 14 hours a day.

### 🔵 B6. Visible cost meter

*"This session: 12 requests, 3 new fragments, $0.40."*

Users trust systems whose cost they can see. It also makes the T1/T2/T3 discipline self-enforcing — an expensive path becomes visible immediately.

### 🔵 B7. Team knowledge sharing before public community

§35 goes from personal knowledge straight to a GitHub PR into a public community. Most of the value lands earlier: **a company-internal shared brain**, on a network share or a private repository.

Same lifecycle, no confidentiality problem, and it is what an employer would actually pay for.

### 🔵 B8. Model health baseline

Record model statistics over time — element counts, warnings, file size, purgeable items — so Heron can say *"warnings have gone from 40 to 380 since last month."*

Trivial to collect, and it is the report BIM managers already produce by hand.

### 🔵 B9. Degraded / offline mode

State plainly what still works with no internet: proven fragments, cached skills, health checks, local retrieval. A tool that stops entirely when the connection drops will not be trusted on site.

---

## Part C — Strategic questions the spec does not address

| # | Question | Why it matters now |
|---|---|---|
| **C1** | **Commercial model** — open source, closed, freemium, company-internal? | Determines the licence, whether the community marketplace is even coherent, and whether the repo can stay private. |
| **C2** | **Who is the first real user?** | If it is the owner only, ship the terminal front-end and skip the installer for a year. If it is colleagues, the installer becomes v1 scope. |
| **C3** | **Name / trademark** | "Heron" is widely used in software. Worth a check before branding, packaging and a marketplace exist. |
| **C4** | **Liability** | If a Heron-generated change causes a defect in a delivered model, who is responsible? Needs an answer before anyone outside the author uses it on live work. |
| **C5** | **Relationship to the existing AJ-Tools / PyRevit-Tools / AEB-Tools repos** | Is Heron a rewrite, a wrapper, or their new home? This decides whether the knowledge import system is a v1 feature or a later one. |
| **C6** | **Autodesk App Store distribution** | If it is ever a goal, their review requirements constrain packaging and permissions — cheaper to know now than to retrofit. |

---

## Part D — The one recommendation that matters most

> **Build one thin vertical slice before building any department.**

"Select all ducts" → intent → fragment → MCP → `ExternalEvent` → Revit → selection changes on screen → result → audit log entry.

That single path touches every layer in the architecture and will settle A1, A2, A3, A5, A6 and A8 with facts rather than opinion. Everything in the master specification is easier to design correctly once it exists — and almost everything is guesswork until it does.

The full plan is in [ROADMAP.md](ROADMAP.md).

---

## Part E — Three findings taken from working Revit tooling, written as Heron's own

**Added 2026-08-27, rewritten 2026-08-28.** These came from studying Revit tooling that already works in
daily use, rather than from reasoning. They are recorded here as **Heron's own conclusions, with the
reasoning intact**, deliberately not as pointers at another codebase — because a note that says *"go and
read that other repository"* is worth nothing the day that repository stops being used, and the reasoning
is the part worth keeping anyway.

Nothing here was copied. Each is a mechanism that was understood and then written for Heron, in Heron's
shape.

### E1 — Runtime C# compilation inside Revit is viable, and Heron will want it

C# can be compiled **at run time, inside `Revit.exe`**, with Roslyn (`Microsoft.CodeAnalysis.CSharp.Scripting`),
across the same 2020→2027 range Heron targets. A sentence becomes C#, is checked, is compiled, and runs on
Revit's thread inside a `TransactionGroup` — with no rebuild and no restart. This is demonstrated, not
theoretical.

Heron today can only run **operations compiled into the add-in ahead of time**, so adding a capability
means a rebuild, a redeploy and a Revit restart. That is the correct trade for Phase 0 and Phase 1 — a
fixed, reviewable set of operations is exactly what makes the first write defensible — but it is not the
endpoint, and [D-04](DECISIONS.md) already anticipates a hybrid.

**The proposal:** when Phase 2 fragments arrive, the execution half does not need designing from first
principles. The shape is known to work on this hardware and this Revit range.

**What it does NOT solve, and this needs saying plainly:** Roslyn compiles *scripts* once the add-in is
already loaded. Every `.cs` file in `revit/` and `platform/` still needs an ahead-of-time build with the
.NET SDK. Runtime compilation is not a way around Step 6 having never met a compiler.

### E2 — Generated code needs a gate before it reaches the compiler

The mechanism worth having: scan generated code **before** compiling it, and split the result three ways.

| | |
|---|---|
| **Blocked** | Process launch, registry, network, reflection, unmanaged calls, `unsafe`, file delete/move, `#r`/`#load` directives, `using static`, type aliases |
| **Warning** | Legitimate but destructive — element delete, purge, writing a file |
| **Safe** | Runs |

Two things matter more than the list itself, and both are the kind of thing only real use teaches:

**A blocklist earns its entries by finding its own holes.** `#r "..."` is a one-line bypass of every other
check, because it pulls in an arbitrary assembly before anything else runs. Reflection generalises past
most name-based checks. `using static` renames a blocked call to a bare method name. Each of those is
invisible until someone looks for it.

**And it must say plainly what it is not.** Text matching is a speed bump against careless generated code,
**not a security boundary** — a determined bypass gets through, and only AST/semantic analysis or real
process isolation would change that. A guard that is believed to be stronger than it is, is worse than no
guard.

This is [Golden Rule 18](14-golden-rules.md) — *generated code never touches a live model on its first
run* — with a known-workable implementation shape behind it. **Not built in Step 6**, deliberately: Heron
generates no code yet, and a gate for a door that does not exist is machinery to maintain, not safety.

### E3 — A lesson taken into the code the same day

`RevitWrite.SafeRollBack` and `RevitWrite.TryRefresh` exist because of this study, and they are the one
part of it that changed Heron immediately.

The bug: an unguarded `group.RollBack()` in a catch block. When the rollback **also** throws — a group
left un-rollback-able by a `Commit()` that has just failed — the second exception escapes and buries the
first. The caller gets no result at all.

Heron had the same shape, written blind the day before, guarded only by `GetStatus() == Started`. That
check is not enough: it cannot see a group left broken by an `Assimilate()` that failed part way, and
`GetStatus()` can throw on its own. Both guards are now kept — the status check to avoid provoking an
exception in the ordinary case, the catch to handle everything it cannot see.

The same lesson from the other direction: a view refresh **after** a successful commit, if it throws,
reports an already-committed change as a failure and then tries to roll back a group it can no longer
roll back. The user is told nothing happened while their elements have in fact moved.

**The general rule, which is the part worth keeping:** a rollback is always the *second* thing going
wrong, and cleanup and cosmetics must never be able to become the first thing reported. Neither of them
is the work.

---

## Part F — Opened by building the Improvement Gate, 2026-09-12

Three of these are questions about Heron's own tooling rather than about the specification. They are
here rather than in [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) because none of them blocks anything, and
[work-notes/README](work-notes/README.md) puts a reviewed suggestion here.

### 🟡 F1. Should the runtime capability snapshot become an MCP tool?

[`mcp/server/heron_runtime.py`](../mcp/server/heron_runtime.py) turns the five different reasons
`heron_capability.resolve()` returns `None` into six verdicts that mean six different things —
`NO_PROVIDER`, `UNSUPPORTED_RELEASE`, `NOT_DETECTED`, `BLOCKED_BY_TRUST`, `NEEDS_REVIT`, `AVAILABLE`.
A planner given `None` can only say *"I cannot"*; the sentences a modeller needs are *"Heron has no
fragment for that"*, *"not on Revit 2020"*, *"press the Heron button"* and *"`write.enabled` is false"*,
which are not the same answer at all.

**Nothing is wired to it, and that was deliberate.** Offering it changes the risk table in
[`heron_tools.py`](../mcp/server/heron_tools.py), which [12 §71](12-security-and-permissions.md) says
is where risk is declared, and the add-in holds the matching half. That is a decision with a human in
it, not a wiring job. `check-reachable.py` reports the function with this reason attached.

**If it is taken**, it is `READ` with no bridge operation — it reads Heron's own library and facts the
caller already has, and sends nothing to Revit — which puts it beside `heron_capabilities` rather than
anywhere near the write path.

### 🔵 F2. `ALL_VERSIONS` is declared twice

[`check-compile.py`](../tools/check-compile.py) and [`check-api-surface.py`](../tools/check-api-surface.py)
each own a copy of the supported release list. [`check-package.py`](../tools/check-package.py) imports
the first rather than making it three, but two is already one too many for the fact that decides which
Revit releases Heron claims to support. Merging them is a change to two working gates and deserves its
own review.

### ✅ F3. The decision log's status summary stops at D-50 — CLOSED 2026-09-12, and it was twenty decisions behind

[DECISIONS.md](DECISIONS.md)'s summary table ends at `D-50` while the log itself runs past `D-69`.
Nothing enforces the table, so it drifted quietly. Completing it needs a careful one-line title per
decision — worth doing in one pass by somebody reading them, not as a side effect of another change.

**CLOSED 2026-09-12 by generating it.** This row said the summary *"stops at D-50"*. It did — and nobody had measured the gap: the file had reached **D-70**, so **twenty decisions were missing from the index of decisions**, `D-70` among them, answered by the owner the day before.

`tools/generate-decision-summary.py` rebuilds the table from the decisions themselves and `check-docs.py` §8 fails if it drifts. **Existing status cells are kept verbatim** — *"read back 2026-09-06"* records a conversation, not a fact on disk, and regenerating it away would have destroyed the record of every read-back the owner has done.

**The gate is in `check-docs.py`, not `gates.yml`.** The `gh` token here carries `repo` but not `workflow`, so no session can push a change to that workflow file — a gate nobody can install is not a gate. Worth revisiting whenever that scope is granted.

### 🔵 F4. Two documents are numbered 34, and one of them is a plan

[`34-patterns-adapted.md`](34-patterns-adapted.md) and
[`34-project-perfection-and-continuous-upgrade-plan.md`](34-project-perfection-and-continuous-upgrade-plan.md)
share a number, and `check-docs.py` does not object because nothing derives the numbering. Two things
are tangled here and they should be untangled together rather than one at a time:

- **The collision.** Renumbering touches every inbound link, including from [35](35-independent-study-notes-open-design-awesome-llm-apps-openhands.md).
- **The placement.** The second is a *plan*, and by [work-notes/README](work-notes/README.md)'s own
  test — *will this be deleted once its work is done?* — a plan belongs in `work-notes/`. Its work **is**
  done, and it now carries a banner saying so. Whether it is deleted, moved or kept for its reasoning is
  the owner's call: it is the fullest written statement of how Heron studies an outside project, and
  that part has outlived the plan around it.

### 🟡 F5. Two suites exit 1 when a dependency is missing, and one of them is the only thing `check-gaps` calls unfinished

[`tests/README.md`](../tests/README.md) sets the rule: **0 is a pass, 1 is a failure, 3 means the suite
could not run** and proves nothing either way. `test_mcp_serves.py` obeys it — no MCP SDK, exit 3, and
[`check-gaps.py`](../tools/check-gaps.py) files it under *waiting*.

`test_served_claims.py` needs the same SDK and exits **1**, so `check-gaps` files it under
**UNFINISHED**, and since its exit code follows that list alone, **the whole gate exits 1 on any
machine without the MCP SDK**. That is the entire unfinished list on a plain container. The same is
true of `test_bridge_roundtrip.py` and its .NET test host, which `check-gaps` sidesteps by skipping the
suite outright — a second answer to one question.

`tests/README.md` already records the inconsistency as an observation — *"So an exit code alone does
not tell you whether a failure is yours"* — rather than as something to fix. It is worth deciding which
it is, because the cost is that the repository's clearest "is anything actually unfinished" signal reads
red for a reason that is not work.

**Not fixed here.** Changing a suite's exit code changes what CI, `check-gaps` and the ship checklist
all read, and that deserves its own change rather than being a side effect of one about something else.

### 🟡 F6. `check-dependencies.py` is documented nowhere

`tools/check-dependencies.py` and `tests/test_dependencies.py` arrived on `main` in #124, with
`requirements.txt` and `requirements-optional.txt`. The tool is good and it closes a real gap — the
only install list used to be one row of a table that said `pyyaml` while the code imported six things.

**~~But [`tools/README.md`](../tools/README.md) has no section for it~~ — CLOSED 2026-09-12: it has one now, and this row was checked rather than assumed still true when a second tool was added to the same file.** That file's whole structure
is one section per tool. A tool nobody can find is a tool nobody runs, which is the same failure the
tool itself was written to fix one level down.

The [`heron-ship`](../.claude/skills/heron-ship/SKILL.md) skill now names it — added here, because this
change rewrote that checklist and leaving a fifth checker out of a list claiming to be complete makes
the checklist wrong. **The `tools/README.md` section is left to whoever wrote the tool**: describing
somebody else's checker from the outside is how a README comes to say something almost true.

It is also **not in `.github/workflows/gates.yml`**. That may be deliberate — it reports on the
*machine*, not on the change, and CI's machine is not anybody's — but it is worth deciding rather than
leaving unstated.

### 🟡 F7. `sqlite_vec` is the one optional package that degrades with NOTHING SAID

**Carried in from `docs/work-notes/plans/rag/03-working-note.md` as `W-10` when that note was retired,
2026-09-12.** It was the only record of it.

The rule this repository keeps is **degrade silently but SAY SO**, and three of four places keep it:
`heron_embed` says it for the encoder, `heron_rerank` for the re-ranker, `heron_ingest` for the PDF
reader. **`_try_vec_extension` in [`brain/heron_embed.py`](../brain/heron_embed.py) says it for
nothing** — it returns `False` on `ImportError` and the caller falls back to comparing vectors in
Python. No `backend()` line, no report line, no error carries it.

**So a person cannot tell the fast path from the slow one**, and the failure mode is the bad one: Heron
gets slower and stays that way, and nobody knows there is anything to install. That is the same shape
as `W-5`, where the install list said `pyyaml` and the code imported six things — a user ends up on the
weaker path **permanently**, because nothing ever told them there was a better one.

**Found 2026-09-11 while closing R-73, by checking the other fallbacks rather than assuming the one in
front of me was the only gap.** Recorded rather than fixed then because it was a second module in a
batch that had no business growing; recorded rather than fixed **now** because it is not what retiring
a note is for. It is a small, self-contained batch of its own.

### 🔵 F8. F5's closing claim is now false, and the reason is worth more than the fix

[F5](#f5-two-suites-exit-1-when-a-dependency-is-missing-and-one-of-them-is-the-only-thing-check-gaps-calls-unfinished)
says `test_served_claims.py` *"is the entire unfinished list on a plain container."* On the owner's
**Windows** checkout on 2026-09-12, `check-gaps.py` named **three** unfinished suites and
`test_served_claims` was **not among them** — `test_ingest`, `test_reachable` and
`test_document_retrieval` were.

**F5 is not wrong about Linux.** It is wrong about *"a plain container"* being the only machine anybody
runs this on, which is the same assumption [A14](NEEDS-CHECKING.md) exists to test and the same one
that made the suite total Linux-specific for weeks. Two of the three are recorded on [`A14`](NEEDS-CHECKING.md) with their causes. **The third,
`test_document_retrieval`, turned out not to be an operating-system difference at all** — it fails on
the trained backend and passes on the fallback, on one machine, and its record is the 2026-09-12
section of [`brain/retrieval-history.md`](../brain/retrieval-history.md). **This row exists so F5 is
not read as current**, and so that the first attribution is not read as the final one: *three suites
fail on Windows* was the obvious reading and it was wrong about a third of itself.

### 🟠 F9. Every fragment claims the top of the trust ladder, and nothing reads it

**Found 2026-09-14 while building [`HERON-INS-SUP-013`](../brain/heron_supply.py), the Supply Chain
Security Agent** — by checking what the field it was about actually does today, rather than assuming the
agent was the first thing that would need it.

Every one of the fragment manifests in [`brain/fragments/`](../brain/fragments) carries a top-level
`source: OFFICIAL` — the **highest** level of [docs/00d §38](00d-additional-requirements.md)'s trust
ladder, `UNKNOWN → EXPERIMENTAL → TESTED → VERIFIED → PROVEN → OFFICIAL`. Derive it rather than reading
this sentence:

```bash
grep -l '^source: OFFICIAL' brain/fragments/*/fragment.yaml | wc -l   # claiming the top
ls -d brain/fragments/*/ | wc -l                                      # fragments in total
grep -n 'EXPERIMENTAL\|VERIFIED\|OFFICIAL' brain/heron_fragment.py   # what reads it
```

The third command prints nothing. **`heron_fragment.py` requires the field and never validates its
value.** The `SOURCES` tuple it does check — `("fragment", "ambient", "request")` — belongs to
`contract.needs[].source`, a different field with the same name one level down. The one trust word the
loader does contain, `PROVEN`, is there as a [docs/24](24-trust-model.md) lifecycle *status*, which
is a different ladder that happens to share a rung.

**So a fragment's trust level is a word the file writes about itself that nothing checks** — which is
precisely the shape [Golden Rule 19](14-golden-rules.md) and [D-35](DECISIONS.md) refuse. It costs
nothing today, because all 360 were written here and `OFFICIAL` is true of every one of them. It stops
costing nothing the first time a fragment arrives from somewhere else, and on that day the field will
already have looked trustworthy for months.

**[D-35](DECISIONS.md) already called this out and it was not read as a to-do:** *"One thing must be
built now, long before community packages exist: the fragment format carries an approval record from the
first version. Retrofitting identity and provenance into a format already in use is the kind of change
that touches every file — cheap today, expensive later."* There are 360 files. The decision's own
argument is that the number only goes up.

**Not fixed here, deliberately.** What the field should mean is the owner's: whether `source` stays a
trust level and gains a validator, whether it is joined by an approval record as D-35 asks, and what a
fragment written in this repository is entitled to claim about itself are three decisions, not a patch.
`HERON-INS-SUP-013` is built to take the answer — it compares a package's claim against a register from
outside it and refuses `SELF_DECLARED_TRUST` — and needs that register to exist.

### 🟡 F10. Two files claim one agent id, and nothing has ever reported it

**Found 2026-09-15 by `HERON-WSP-REG-012` on its first run against the real repository** — not by a
fixture, and not by reading. Both of these carry `Heron-Agent: HERON-FRG-VAL-001`:

```bash
grep -l 'HERON-FRG-VAL-001' brain/*.py
head -3 brain/heron_fragment.py brain/heron_validate.py
```

| file | step | what it is |
|---|---|---|
| `brain/heron_fragment.py` | 7 | *"What a fragment IS on disk, and the validator that will not let it lie"* |
| `brain/heron_validate.py` | 17 | *"The Fragment Validation Agent. It gathers evidence for a proof. It never signs one."* |

**Nothing in the repository can see this.** [`tools/check-metadata.py`](../tools/check-metadata.py)
checks that each claimed id EXISTS in the register, then collects them with `claimed.add(aid)` into a
**set** — so two files claiming one id collapse to one entry and the count comes out right.
`tools/agent-count.py` counts the agent as built either way. Both gates pass, and have all along.

**Multi-file agents are legitimate here** — several agents span `brain/` and `mcp/`, or a module and
its C# half, and the register row for `HERON-FRG-VAL-001` is broad enough to cover both halves:
*"Logic, API, versions, dependencies, metadata, duplication, reusability"*. So this is **not
automatically a defect**, which is exactly why it needs a person rather than a patch.

**The question is which of three things it is**, and only the owner can say:

1. **One agent, two files, correctly** — the format half and the evidence half. Then nothing changes
   except that the gate should stop being blind to the pattern.
2. **Two agents wearing one id** — the schema validator is arguably `HERON-FRG-FMT-*` work and not
   validation at all. Then one of them needs its own registry row.
3. **A rename that never finished** — the usual cause, and the one the gate's own comment warns about
   two lines further down.

**Not fixed here, deliberately.** Splitting an agent or renaming one is an ADMIN act needing a
signature — `HERON-AHR-RET-010` refuses it without one — and picking a winner would make the other
file invisible, which is the precise thing `HERON-WSP-REG-012` refuses to do.

**Worth adding whichever way it goes:** a check that reports one id claimed by more than one file.
Today that costs nothing to add and surfaces a real ambiguity; it stops costing nothing the first time
a rename half-lands and two files disagree about what they are.

#### CORRECTION, the same day: that check was written, measured, and NOT kept

The line above was the obvious next move and it is wrong. Before adding it, the repository was measured:

```bash
python - <<'EOF'
# every id claimed by more than one file OUTSIDE tests/
EOF
```

**Fourteen ids are claimed by more than one non-test file, and most of them are correct.**

| a sample | why it is fine |
|---|---|
| `HERON-REVIT-CMP-021` — three C# files | one feature spread over the files that make it |
| `HERON-SES-DIS-001` — the Python client and `BridgeIdentity.cs` | the two halves of discovery, in two languages |
| `HERON-WSP-PTH-007` — `heron_paths.py` and `HeronPaths.cs` | *where* things live and *which class* they are |
| `HERON-RAG-RNK-006`, `CTX-007`, `LIB-001` — each also on `heron_retrieve.py` | that module is **the orchestrator**: its own docstring says *"the whole lookup, in the order docs/05 §4 sets out"*, so it is the place those steps happen in order |

So a shared id is **the norm here, not a defect**, and the proposed check would have reported fourteen
things of which most need no action — noise, not a guard. It was not added.

**What that leaves.** The original observation stands for `FRG-VAL-001` specifically: two files, both
validators, and nothing says which is the agent. What does not stand is the general rule. **Nothing in
a header can distinguish a deliberate split from a stale one** — the shape is identical — so the
question for the owner is not *"why are there duplicates"* but *"should a header say which file is the
agent and which files merely implement part of it"*. That is one field, or a convention, and it is a
different decision from the one this row first asked for.

### 🟠 F11. docs/06 §2 draws nineteen folders and classifies fourteen

**Found 2026-09-15 by `HERON-WSP-CRE-002`**, which reads the tree out of that section rather than
carrying a copy — so the first thing it did was ask each folder what class it was in.

[docs/06 §2](06-heron-platform.md) draws the workspace as nineteen folders, then immediately puts
folders into **Product / Data / Derived**. The table covers fourteen. These five are drawn and
classified by nothing:

```
RAG   Community   Configuration   Tests   Documentation
```

Derive it:

```bash
python brain/heron_folders.py            # the five are listed under "NO CLASS"
```

**The class is not a label — it is the only thing that answers three questions**, and every one of
them is now asked by an agent in this repository:

| question | who asks | what the wrong answer does |
|---|---|---|
| may a product update replace it wholesale? | `HERON-OPS-UPD-010` rule 6 | a practice's work is gone |
| may a cleanup delete it outright? | `HERON-WSP-CLN-009` | same, more quietly |
| does a backup cover it? | `HERON-WSP-BAK-010` | it is not there when needed |

**`Configuration` is the one to settle first.** [docs/21 §9](21-resilience-and-operations.md) makes
configuration a **security boundary** — it holds the security policy, the update policy and the
company standards, and `HERON-INS-CFG-006` already splits it into machine-specific and portable
halves. If the folder reads as **product**, an update replaces a practice's security policy with the
shipped defaults, and nothing in the specification currently says it must not.

The other four have plausible answers that are still nobody's decision on record:

- **`RAG`** — [docs/07 §8](07-installation-and-update.md) says *"the vector index is the easy case — it is derived"*, which points at **derived**. But the folder may hold more than the index.
- **`Community`** — [docs/00d §37](00d-additional-requirements.md) says imported community components are untrusted by default. `Packages` is product; is `Community` product too, or data because the user installed it?
- **`Tests`**, **`Documentation`** — most likely **product**, and cheap to say so.

**Not fixed here.** Adding a row to the class table changes what four agents do to a folder, and
`HERON-WSP-PTH-007` is deliberately not extrapolated — the same reason D-05 refuses to guess a Revit
release. Until it is answered, `heron_paths.classify()` returns `UNKNOWN` for all five and `may()`
reads UNKNOWN as **data**, so nothing removes or overwrites them. That is the safe failure, not a fix:
it also means a backup does not cover them and a cleanup leaves rubbish behind.

---

### 🟠 F12. "Template" means two opposite things, and one of them is never allowed to leave

**Found by:** building `HERON-WSP-TPL-006`, the Template Agent, 2026-09-15.
**Status:** open. The agent takes the narrow reading and says so in its own answer.

[docs/12 §97](12-security-and-permissions.md) is unusually firm, and the wording is the owner's own:

> The rule Ajmal actually set draws the line at **the file, not the information**: a `.rvt`, an
> `.rfa`, a family or project template is **never** uploaded.

[docs/00 §1115](00-master-specification.md) lists, as possible marketplace packages:

> Skills, Fragments, Agent packs, BIM standards, Revit tools, **Project templates**, Company extensions.

and [docs/00d §325](00d-additional-requirements.md) lists **company templates** the same way. A
marketplace package is, by definition, a file that leaves.

**So the specification says a project template is shippable and that a project template is never
uploaded.** Both lines are correct if the word carries two senses, and nothing written says it does:

| the word | what it is | how big | may it leave? |
|---|---|---|---|
| a Heron workspace/project template | a list of folder names a new job starts from | hundreds of bytes | the marketplace lines say yes |
| a Revit project or family template | `.rte`, `.rft` — the office's own starting file | hundreds of megabytes | docs/12 §97 says never |

In a Revit practice the second is what the word means. Anyone reading "project templates" on a
marketplace page reads it as the `.rte`.

**What the agent does until this is answered.** `HERON-WSP-TPL-006` takes the narrow reading, and
enforces it structurally rather than by remembering it: **a template names, it never carries.** A
template entry is a folder name and a reason — never a file body, never a path to one. Two separate
refusals, because they are two different mistakes:

| | |
|---|---|
| `A_REVIT_FILE_IS_NAMED` | any of `.rvt` `.rfa` `.rte` `.rft`, **by name alone**, with nothing attached. A template that names the office `.rte` is one read away from carrying it |
| `TEMPLATE_CARRIES_A_FILE` | an entry with `bytes`, `body`, `content`, `data`, `source`, `from`, `file`, `path` or `url` |

and the module itself uses no `open(`, no `shutil`, no `.read()` and no `urlopen` — so there is
nothing in a template that *could* be uploaded. The suite asserts all of that against the code.

```bash
python tests/test_templates.py           # 3. A template cannot carry
```

**Not fixed here.** Deciding it is one line in docs/00 — either *"project templates are name lists,
not `.rte` files"* or *"the marketplace may carry an `.rte` under these conditions"*. The second is a
change to the rule the owner set, so it is not one an agent may assume.

---

### 🟡 F13. A project folder and a workspace folder with the same name get different classes

**Found by:** the same build. **Status:** open, and narrower than F11.

`HERON-WSP-PTH-007`'s `classify()` tokenises a path and answers on the first **data** word it finds
anywhere in it. That is right for what it was built for and it has this consequence:

```
classify("Cache")                    ->  derived
classify("Projects/Tower A/Cache")   ->  data
```

The same leaf name, two classes, decided by what sits above it. A cleanup that clears **derived**
removes one and spares the other; a tool holding only the leaf name gets the opposite answer from one
holding the whole path — and `HERON-OPS-UPD-010` asks with a component **name**, not a path.

**What the agent does.** `HERON-WSP-TPL-006` refuses `RESERVED_WORKSPACE_NAME` for any of the
seventeen words the class table is made of, so a template cannot create a project folder whose fate
depends on who is asking. The reserved list **is** `heron_paths.FOLDERS`, read from it, not a second
copy.

**Not fixed here.** The fix is either a path-aware `classify()` that only matches at the top level,
or a documented statement that the leaf-name answer is the intended one. Both change what four
agents do to a folder, so it is the owner's call — the same reason F11 is still open.

---

### 🔴 F14. Three different projects write to one knowledge file, and the code says why that is a breach

**Found by:** building `HERON-WSP-PLC-005`, the File Placement Agent, 2026-09-15.
**Status:** open. **Measured, not read** — the numbers below come from running the code.

`brain/heron_scope.py`'s `scope_path()` turns a project key into a filename through `_safe_key()`,
which replaces every character outside `[A-Za-z0-9._-]` with a hyphen:

```bash
cd brain && HERON_KNOWLEDGE=/tmp/hk python -c "
import heron_scope as S
for k in ['Tower B', 'Tower/B', 'Tower-B']:
    print('%-10r -> %s' % (k, S.scope_path('project', k)))"
```

```
'Tower B'  -> /tmp/hk/projects/Tower-B.db
'Tower/B'  -> /tmp/hk/projects/Tower-B.db
'Tower-B'  -> /tmp/hk/projects/Tower-B.db
```

**Three projects, one file.** Two lines above the function that does it, `scope_path()`'s own
docstring says what that costs:

> Heron does not guess which project this is: guessing wrong writes one client's knowledge into
> another's file, which is a **contractual breach rather than a bug** (docs/10 §2).

It refuses to guess when the key is **missing** and then quietly collapses two keys that are
**present and different**. The second is the same harm as the first, arrived at more quietly.

**What keeps it from biting today, and what does not.** The key is *meant* to be the document's
Project Information `UniqueId` — hex and hyphens, which never collides. Nothing enforces that.
`scope_path()` accepts any string, and three shipped command lines document handing it a typed name:

| | |
|---|---|
| `brain/heron_conflict.py:12` | `--scopes company,project --project "Tower B"` — the agent's own usage line |
| `brain/heron_ingest.py:1593` | `project = _flag(argv, "--project")` |
| `brain/heron_research.py:741` | `project = _flag(argv, "--project")` |

So the safe case is the intended one and the unsafe case is the documented one.

**Not fixed here**, and the reason is not caution. Any fix moves where existing knowledge lives:

- **reject a key that is not a UniqueId** — correct, and it breaks the three command lines above and
  every store already named after a typed name;
- **hash the key instead of reducing it** — removes the collision and makes every existing
  `projects/*.db` unreachable, so it needs a migration under [docs/07 §8](07-installation-and-update.md)
  (idempotent, versioned, reversible-or-backed-up) — which `HERON-WSP-MIG-008` can now plan;
- **keep the reduction and record the original key inside the store** — smallest change, detects a
  collision after it has happened rather than preventing it.

Picking among those is the owner's call, and it is the same shape as D-05: do not extrapolate.

**What the new agent does about it.** `HERON-WSP-PLC-005` takes the opposite rule and states it:
**a name that would have to be rewritten to be usable is refused, never cleaned up.** A project
called `Tower/B` is a refusal (`NAME_IS_NOT_A_PLACE`); `Tower B` is placed at `Projects/Tower B/…`
*as given*, space and all. The suite proves the rule by running `heron_scope` and watching the three
names arrive at one file, so the finding cannot quietly stop being true:

```bash
python tests/test_placement.py     # 3. A name is refused, never cleaned up - and here is the cost
```

---

### 🟡 F15. The Naming Agent's own name has no stated shape, and two documents disagree about its parts

**Found by:** building `HERON-NAM-VAL-002`, the Naming Validation Agent, 2026-09-15.
**Status:** open. The validator refuses that one kind of name rather than guessing it.

The department's rule is stated twice, as a goal:

> **"Naming must be predictable and searchable."** — [docs/06 §134](06-heron-platform.md), [docs/00 §517](00-master-specification.md)

That is what naming is *for*. It is not a convention: it gives no separator, no case, no order and no
allowed character set. The only two statements of what a generated name is **made of** disagree:

| source | the parts |
|---|---|
| [docs/28](28-agent-registry.md), `HERON-NAM-GEN-001` | domain, capability, purpose, platform, **version** — five |
| [docs/00c §368](00c-master-handover-baseline.md) | domain · capability · purpose · platform · version · **component type** — six |

So `HERON-NAM-GEN-001` is asked to generate a name whose shape nobody has written down, and
`HERON-NAM-VAL-002` is asked to check it against a convention that does not exist.

**Everything else in the system is fine**, and that is what makes this narrow rather than alarming.
Four kinds of name *are* stated, and the validator reads each from the file that owns it:

| kind | where the rule lives |
|---|---|
| agent id | `docs/28`'s own rows, through `heron_fragment.registry_agents()` |
| fragment id | `heron_fragment.ID_PATTERN` and `.AREAS` |
| capability | `heron_fragment.CAPABILITY_PATTERN` |
| the fragment folder | [docs/29 §130](29-metadata-standard.md) — the capability, lower case, hyphens, **"derived, never invented"** |

Two more are **observed and stated nowhere**: every module in `brain/` is `heron_<name>.py` and every
suite in `tests/` is `test_<name>.py`, with no exceptions and nothing enforcing it. The validator
reports those as `UNLIKE_EVERY_OTHER` rather than `WRONG_SHAPE`, and puts the count in the answer,
because *"unlike all 63 of its neighbours"* and *"against a written rule"* are different claims.

**What it does about the seventh.** `generated-name` is refused as `UNSTATED_CONVENTION` — never
guessed. A guess here would silently **become** the convention, because this validator would be the
only thing enforcing one, and a convention arrived at that way is the hardest kind to change later.

**Not fixed here.** Settling it is one line saying which list of parts is right and what the name looks
like — a separator, a case, an order. That is a decision about what the product's filenames read like,
which is the owner's.

### SETTLED 2026-09-16 — [D-79](DECISIONS.md): six parts, version last

    <domain>-<capability>-<purpose>-<platform>-<component>-v<n>
    mep-duct-insulation-check-revit-fitting-v1

**docs/00c won.** It is the owner's own handover document and its sixth part is real, so the register's
row was corrected rather than the baseline. The version moved to the END, which 00c's listing does not
do — recorded in D-79 as a change to the order rather than folded in quietly.

The rules live in [docs/29](29-metadata-standard.md), which already owned every other name shape here.
`HERON-NAM-GEN-001` is built in `brain/heron_naming.py` beside the validator, and generates through the
validator's own rule so the department cannot produce a name its own checker rejects. **Naming &
Taxonomy is 7 of 7.**

**The half that is still impossible is declared rather than hidden.** A part may be hyphenated, so a
finished name cannot be split back into six parts — `check()` says it checked the SHAPE, in the answer,
and never claims to have checked the parts.

---

### 🟠 F16. The register asks for a synonym table and D-34 forbids one

**Found by:** building `HERON-NAM-KEY-005`, the Keyword Agent, 2026-09-15.
**Status:** open, but **the agent is built** — D-34's own consequences settle it, and the resolution is
worth confirming rather than assuming.

[docs/28](28-agent-registry.md) gives the Keyword Agent:

> Search terms and **synonyms** — "duct", "ductwork", "supply air"

[D-34](DECISIONS.md) says:

> **Heron builds nothing to understand language.** No phrase list, **no synonym table**, no parser for
> dictated near-misses. That belongs to the host and duplicating it there would be worse than the host's
> version and would need maintaining forever.

Read the register row on its own and it describes exactly the thing the decision refuses.

**D-34 answers it three lines further down**, in its own consequences:

> A site word that maps to a Revit word is a different problem and is not solved by translation. When
> somebody says something the model calls by another name, **that is knowledge** — it belongs in Heron's
> own knowledge store where it can be **looked up and corrected**, not in a language setting.

So the agent holds **knowledge, not language**, and the difference is four rules rather than a
distinction of wording. Each is a refusal in the built agent:

| | |
|---|---|
| it ships **no list** | the table starts empty and stays empty until somebody fills it. The suite proves this by *behaviour* — with nothing handed in, every word comes back unknown — not by searching the source for vocabulary |
| every entry names a **person and a date** | "looked up and corrected" needs somebody to correct and a date to correct from |
| an **inference is not a record** | an entry whose `by` reads derived, guessed, inferred, automatic, auto, model, suggested or expanded is refused, and a word recorded *only* that way stays **unknown** — the refusal is not a warning beside a usable answer |
| an **unknown term is a question** | [D-33](DECISIONS.md): Heron never assumes an input — it asks, and it asks once. Nothing is expanded quietly, and there is no partial answer beside the question |

**What is still open:** the register row's wording. It reads as the forbidden thing and points at no
decision, so the next person to build from that row alone will build a synonym table. One clause in
`docs/28` — *"recorded by a person, never inferred — see D-34"* — closes it.

---

### 🟡 F17. Three agents own metadata validity, in two departments

**Found by:** the same build, while checking whether `HERON-NAM-MET-006` already existed.
**Status:** open. **`MET-006` was deliberately not built** — see below.

| agent | department | what docs/28 gives it |
|---|---|---|
| `HERON-STD-MET-014` | Standards & BIM QA | "Enforces the Heron metadata standard on everything Heron creates… also audits the registry against the code" — **built**, `tools/check-metadata.py` |
| `HERON-FRG-VAL-001` | Fragment Lifecycle | "Logic, API, versions, dependencies, **metadata**, duplication, reusability" — **built**, and claimed by two files (that is F10) |
| `HERON-NAM-MET-006` | Naming & Taxonomy | "Metadata completeness and schema validity" — **not built** |

The third row's job is a plain subset of the first two. `MET-014` already checks that every artefact
declares its agent, step, status, version and layer; `FRG-VAL-001` already checks a fragment's card
against its schema. A third agent would be a third place for the same rule, and every other finding in
this file is about what happens when one rule lives in two places.

**Not built, and that is the point.** Building it would have cost nothing and been wrong — the same
mistake as writing a new agent over `brain/heron_architect.py` earlier today, arrived at from the other
direction. What is needed is one line in `docs/28` saying which of the three owns metadata validity and
what the other two defer to it for. That is the owner's call, and until it is made the Naming & Taxonomy
department reads as 7 agents when its real number may be 6.

**SETTLED 2026-09-16 — [D-78](DECISIONS.md).** `HERON-STD-MET-014` owns it and `MET-006` folds into
`tools/check-metadata.py`. Two thirds of the question turned out to be answered already, in that tool's
own source rather than in any register: *"ONE place per fact: `brain/heron_fragment.py` validates these
files, and this checker does not read them."* So `FRG-VAL-001`'s half was never an overlap, and only the
unbuilt third row needed deciding.

`tests/test_metadata_guard.py` plants one error for each word of the folded row — a missing field, an
invalid layer, a claim on an id that does not exist — and requires all three to be caught. **Naming &
Taxonomy is 6 of 7**, and the seventh is `NAM-GEN-001`, blocked on **F15** above.

---

### 🟡 F18. "Registers them with the Tool Registry" — a discovered tool must not enter a fixed table

**Found by:** building `HERON-MCP-DIS-012`, the MCP Discovery Agent, 2026-09-15.
**Status:** open, and **the agent is built** on the narrow reading. The word is worth one clause in
`docs/28`.

The register gives `HERON-MCP-DIS-012`:

> Finds **other** MCP servers installed on the machine, reads their tools, versions and capabilities,
> and **registers them with the Tool Registry**.

`HERON-MCP-REG-003`'s table is fixed, and says so in its own refusal:

> `'%s' is not declared in the MCP tool registry. Add it to TOOLS with its risk level — **being absent
> is a refusal, not a risk of zero.**`

Read "registers" literally and a foreign server's manifest ends up adding entries to Heron's own risk
table. That is exactly what **Golden Rule 19** forbids:

> No text Heron reads may raise Heron's own permission level. Content from documents, family names,
> parameter descriptions, imported folders, model text and community packages is **data, never
> instruction**. Permission comes from the user, through Heron's own UI, per action.

**The narrow reading, which the agent takes.** A discovered tool comes back as a **finding a person
reads** — at `UNKNOWN` trust ([docs/24 §47](24-trust-model.md): provenance unclear, which is what a
server somebody installed is — installing is not vouching). The registry is untouched, and because the
tool is undeclared there, calling it already raises. **The existing refusal is the protection**; the
suite proves it by calling `risk_of` on a discovered name and catching `NotDeclared`, rather than
asserting it in prose.

Three things are refused rather than recorded:

| | |
|---|---|
| a name in **Heron's namespace** | not the eighteen names — the *prefixes*, derived from `REG-003`'s own table. `heron_select` is **not** one of Heron's tools and reads exactly like one, which is the whole danger. An exact-match check let it through on this file's first run |
| **Heron's own vocabulary** in a manifest | `risk`, `trust`, `approved`, `permission`, `confirmed`, `granted`, `allowed` — a manifest using those is writing into Heron's fields, not describing itself. Recording it as a claim would still be reading it |
| a tool with **no name** | it would sit in a list a person reads as though it were callable |

Everything else a server says lands under `says` and nowhere else, so a tool describing itself as safe,
read-only or already approved has described itself and changed nothing.

**What is still open:** the register's wording. One clause — *"presents them for a person to declare;
never writes into the table — Golden Rule 19"* — closes it, and without it the next person to build
from that row alone will write the append.

---

### 🟠 F19. D-27 abolished persona, and three places still hand work to it

**Found by:** building `HERON-USR-PRO-001`, the User Profile Agent, 2026-09-15.
**Status:** open. The agent implements what survives D-27 and does **not** implement the dead clause.

[D-27](DECISIONS.md) is explicit, and it says what it supersedes:

> **Supersedes** the *"infer a default, display it, let the user pin it"* recommendation in
> [01 §4](01-vision-and-principles.md) and **the two-persona table in [22 §2](22-users-modes-and-extensibility.md)**.
>
> **Heron has one voice: plain, non-developer language, always.** Persona is not inferred, not displayed
> and not pinned — **it does not exist as a setting.**

[docs/22 §2](22-users-modes-and-extensibility.md)'s own `[DECIDED 2026-08-28 — D-27]` block puts it more
bluntly, and it is worth quoting separately because it lives in the other file:

> **There is no persona.** The warning above was right and it argues further than it went: if silent
> switching reads as unreliability, the fix is not to display the guess — it is not to guess. Heron has
> **one voice**, and what varies is the **shape of the answer**, read off the **shape of the request**.

Three places still describe the thing it removed:

| where | what it still says |
|---|---|
| `docs/28`, `HERON-USR-PRO-001` | *"**Persona** reads this to choose how to speak"* |
| `docs/28`, `HERON-ORC-PER-003` | *"Communication / Persona Agent — detects role and technical level; **chooses wording**"* |
| [`docs/22 §3`](22-users-modes-and-extensibility.md) NOTE | *"**Persona** may be inferred — it only changes wording. **Mode** must be granted."* |
| `docs/28`, `HERON-RPT-CMP-001` | *"Decides what goes in and at what depth **for this reader** — a modeller wants the 47 failures, a BIM manager wants the trend. **Judgement, so a model call**"* |

The third is the one that matters, because it is doing real work in a sentence about security. Its point
is sound and its example is gone: it contrasts a *grantable* mode with an *inferable* persona in order to
say why conflating them **"would let a user talk their way into `ADMIN`"**. With persona abolished there
is nothing inferable left to contrast with — which makes the rule **stronger**, not weaker, and leaves the
sentence explaining it broken.

**What the agent does.** It holds what survives and implements none of the dead clause:

| | |
|---|---|
| **a fact is declared** | somebody said it about themselves. A `by` reading inferred, derived, guessed, detected, assumed, estimated, observed, automatic, auto or model is refused — deciding from a conversation that a user is a beginner is a judgement they did not make and cannot see (D-33) |
| **a mode is granted** | it carries who granted it and when, and a grant whose `by` is not a person is refused. **No mode held is not User Mode** — a permission boundary that defaults to something is not a boundary |
| **nothing about how to speak** | there is no tone, level or phrasing field in anything it returns, whatever it is asked |

**A fourth was found later the same day**, building `HERON-RPT-CMP-001`: its row chooses *what goes
in* by reader, and calls that a judgement needing a model call. D-27 replaced exactly that judgement with
a five-row table keyed on the **request**, and gave the reason in its own consequences — *"nothing has to
detect who is talking. A whole class of 'why did it answer differently today' stops being possible rather
than being made visible."* The agent implements the table and calls no model.

**Not fixed here.** `HERON-ORC-PER-003` is one of the four agents [D-01](DECISIONS.md) delegates to the
host, so retiring or renaming it is a change to the host contract, and `docs/22 §3`'s note needs rewriting
rather than deleting — the rule it protects is the reason the User & Personalization department exists at
all. Both are the owner's, and they are one paragraph apart from being settled.

---

### 🟠 F20. "Strips project identifiers" is the framing D-26 narrowed away from

**Found by:** building `HERON-RPT-RED-003`, the Report Redaction & Release Agent, 2026-09-15.
**Status:** open. The agent implements D-26 and does **not** strip.

[D-26](DECISIONS.md) opens with a warning to its own reader, which is the reason this is worth raising
rather than quietly following:

> **This decision was refined three times on the day it was written, each time in the same direction:
> from *nothing may travel* toward *the file may not travel*. The rule below is the final one. It is
> narrower than the first two, and commits from that day quote the earlier framings — so the movement is
> recorded here rather than quietly overwritten, because a reader needs to know which version won.**

And the rule that won:

| Never leaves the machine | **Fine in the conversation** |
|---|---|
| The `.rvt` and `.rfa` files themselves | **Project names**, file names, content names |
| Family and project templates | Element data — counts, sizes, parameters |
| Any Revit binary | Engineering ideas, reasoning, and code |

`docs/28`'s row for this agent reads:

> The gate before a report can be shared or leave the machine. **Strips project identifiers**, enforces
> scope, blocks confidential-project egress.

**Project names are in D-26's right-hand column.** Stripping them implements the framing the decision
moved away from — and doing it quietly is worse than doing it wrongly, because it leaves a report that
*reads* as anonymised without anybody having decided it should be.

**What survives, and all of it is a refusal rather than a strip:**

| | |
|---|---|
| a **Revit binary** attached | `.rvt` `.rfa` `.rte` `.rft` — D-26's own left-hand column, the same rule `HERON-WSP-TPL-006` keeps on the other side of the machine |
| a **credential** in the text | article 17. Refused whole, not redacted — a report that had a key in it is one somebody should look at, not one to clean and send. The shape is named and the value never is |
| **more than one project** | D-26's third point: *"project-based knowledge must be kept segregated and separated"*. This is what "enforces scope" means once the stripping is gone, and it is the half of the row that survives whole |
| a project that has **not declared** this may leave | absence is not permission. docs/12 §85 is about contracts that prohibit egress, and a contract is something somebody signed rather than something to assume from silence |

**Not fixed here.** Rewriting the row is three words, but it is the third row this session found written
against a superseded decision — with **F16** (the Keyword Agent's "synonyms" against D-34) and **F19**
(three places still handing work to the persona D-27 abolished). Individually each is a clause; together
they suggest `docs/28` was written before several of the decisions that now govern it, and a pass over
the register against `DECISIONS.md` would find whatever else is in the same state. That pass is the
owner's call, not three more clauses.

---

### 🟡 F21. `docs/09 §94` still carries the promotion gate D-30 replaced, and still calls it open

**Found by:** building `HERON-LRN-PRO-004`, the Learning Promotion Agent, 2026-09-15.
**Status:** open. The agent follows D-30.

[docs/09 §94](09-skills-and-fragments.md) proposes the lifecycle gates, and two of its lines are stale:

> | VALIDATED → PROVEN | **N successful real executions**, zero unexplained failures, no user corrections *(N to be set — suggest 10)* |
>
> Tracked as **[Q-9]** for the value of N and who may approve.

**Q-9 is answered.** [D-30](DECISIONS.md) answers it by rejecting the count outright, and does so with a
defect from the real library:

> One fragment's record reads: the level chain never tried `RBS_START_LEVEL_PARAM`, so setting a level
> filter matched **zero** ducts **and reported success**.
>
> **A fragment that succeeds while doing nothing passes ten runs. It passes a thousand.** A count measures
> that nothing threw, which is not the property anybody cares about.

D-30's gate is **one recorded proof against a real model** — dated, naming the model, carrying a positive
case, a **negative** case (*"this is the one that catches succeeded and did nothing, and a proof without
it is not a proof"*), and a second route where one exists — recorded by whoever ran it, *"under their
name and the date, not a tick"*. It also answers the second half of Q-9: who may approve.

So `docs/09 §94` asks for a number D-30 deleted, and points at a question D-30 closed.

**What the agent does.** It follows D-30: a candidate arriving with a thousand successful runs and no
proof is refused, and **the count is echoed back** so nobody mistakes the refusal for not having noticed.
A proof with no negative case is refused **before** anything else the proof is missing — the others make
a proof incomplete, and that one makes it not a proof.

**Why this one is worth its own row.** F16, F18, F19 and F20 are all `docs/28` rows written against a
superseded decision. This is the same failure in a **different document** — which means the pass those
findings ask for is not only over the register. Anywhere a document says *"tracked as Q-n"* is worth
checking against `DECISIONS.md`, because that phrase is exactly what stops a reader looking further.

---

### 🟡 F22. One permission ladder, four copies in `brain/` alone

**Found by:** building `HERON-SKL-CMP-005`, which needed the ladder and refused to add a fifth.
**Status:** open. Measured, not read.

```bash
grep -rln 'RISK_LADDER\|RISK_ORDER\|"ANALYZE"' brain/*.py
```

| file | agent | how it carries it |
|---|---|---|
| `heron_capability.py` | `HERON-KRN-CAP-008` | `RISK_ORDER = (…)` |
| `heron_events.py` | `HERON-KRN-EVT-004` | `RISK_ORDER = (…)` |
| `heron_hr.py` | `HERON-AHR-HR-002` | `RISK_LADDER = (…)` |
| `heron_skill.py` | `HERON-SKL-VAL-004` | an **inline tuple** inside `validate()` |

All four are `("READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH", "ADMIN")`, all equal by
value, **none the same object**. Three more modules derive from it — `heron_shadow.py` maps tier to risk,
`heron_trainer.py` maps risk to permission, `heron_validation.py` keeps `READ_ONLY_RISKS`.

`mcp/server/heron_tools.py` holds a fifth as integers (`READ = 0` … `ADMIN = 6`). **That one is
legitimate**: [D-48](DECISIONS.md) forbids `brain` importing from `mcp`, so the bridge needs its own —
and `heron_tools.py`'s own comment already says a rule kept in two places by good intentions is a rule
that will eventually be kept in one.

**Why it matters more than it looks.** The ladder is ordered, and the order is the security property:
`MODIFY < PUBLISH < ADMIN` is what makes "propagate the highest risk upward" mean anything. Four
independent orderings are four chances for one to be edited — a level inserted, one renamed — and the
copies would still each look right on their own.

**Not fixed here**, and the reason is that the fix is a decision rather than an edit: `brain/` has no
module that *owns* permission levels. [docs/12 §71](12-security-and-permissions.md) puts risk in the tool
registry, which is `mcp`'s side of D-48. Somebody has to say which `brain` module is the home — or that
it belongs in `platform/`, which both layers may import — and that is the owner's call.

`HERON-SKL-CMP-005` imports `heron_capability.RISK_ORDER` and adds no copy. The suite asserts it is that
module's object, by identity.

---

## F23 — four of the five remaining Documentation agents have no distinct source, or no distinct job

Found while building [`HERON-DOC-SKL-003`](../tools/generate-skill-catalog.py) and
[`HERON-DOC-API-001`](../tools/generate-api-docs.py), which both had one. The other four do not, and
they fail in two different ways.

### Two have no input yet

| agent | docs/28 says | what exists |
|---|---|---|
| `HERON-DOC-CHG-008` | Added / Improved / Fixed / Deprecated **per version** | **one version** |
| `HERON-DOC-REL-005` | Structured notes **per release** | **no releases** |

```bash
git grep -h '^# Heron-Since:' | sed 's/.*: *//' | sort -u   # 0.1.0, and nothing else
git tag | wc -l                                              # 0
```

683 files carry `Heron-Since: 0.1.0` and there are no tags. A change log with one section listing 683
files is true and useless, and a release-notes agent has nothing to write notes about.

**There is a second problem that outlives the first.** `Fixed` and `Improved` cannot be told apart from
a diff. The only mechanical way is a word list over commit subjects — "fix", "bug", "correct" — which is
a synonym table wearing a different hat, and [D-34](DECISIONS.md) says Heron builds none. Whoever builds
`CHG-008` after the first tag still needs an answer to that, and the honest options are a commit
convention the repository adopts deliberately, or the host classifying under [D-01](DECISIONS.md).

### Two would be a second place for one rule

| agent | docs/28 says | who already does it |
|---|---|---|
| `HERON-DOC-RDM-007` | Keeps the README current | `tools/check-docs.py` |
| `HERON-DOC-ARC-006` | Keeps architecture docs in step with the registries | `tools/check-docs.py` |

`check-docs.py` is `HERON-DOC-VAL-009`, and it already reads `README.md` by name, recomputes every count
claim in every `.md` against its derived source, enforces the same claim wherever it is made, and checks
every internal link. Its own comments record three occasions when `README.md` was wrong and section 7 is
why it is not now.

A second agent keeping the README current would be a second thing that can be right on its own while
disagreeing with the first — the same objection that stopped `HERON-NAM-MET-006` being built (**F17**).

### What is proposed

1. Build `CHG-008` and `REL-005` **when the first tag is cut**, not before — and decide the
   `Fixed` / `Improved` question first, as a convention or as the host's.
2. Fold any check `ARC-006` or `RDM-007` would add **into `check-docs.py`**, rather than beside it.

### SETTLED 2026-09-16 — [D-77](DECISIONS.md): both, as proposed

`CHG-008` and `REL-005` are deferred with the tag named as what unblocks them, so they stop reading as
work somebody is neglecting. `ARC-006` and `RDM-007` are folded into `check-docs.py`, which now claims
three rows.

**The fold was checked rather than asserted**, because closing two rows by writing no code is the exact
shape of a claim this repository keeps finding it believed. `tests/test_docs_guard.py` plants an error in
each place those rows name and requires the guard to find it.

**That checking found a real defect.** `check-docs.py` had been FINDING dead links since 2026-08-31 and
exiting 0 on them. Wiring section 1 to the exit code turned up three — including a `D-30` anchor linked
correctly ten times and wrongly once, which an anchor check written hours earlier had missed because it
kept one occurrence per id. D-77 records all three.
3. Either way, the four rows in [docs/28](28-agent-registry.md) should say so, so the next person does
   not read four unbuilt agents as four missing ones.

**Not acted on.** Retiring or re-scoping a register row is the owner's call, and so is adopting a commit
convention.

---

## F24 — `docs/09 §151` asks for a percentage that the agent it asks has refused to produce

Found while building [`HERON-FRG-CRE-007`](../brain/heron_generate.py), whose register row is *"Only
after Fragment Matcher reports nothing reusable"*. [docs/09 §151](09-skills-and-fragments.md) makes that
mechanical, and names a number:

> Before the Code Generation Agent runs, the Fragment Matcher must have searched and reported. **If a
> proven fragment covers ≥80% of the request**, generation is not permitted to start from scratch — it
> must start from that fragment.

**There is no 80%.** `HERON-RAG-FMT-004` is the Fragment Matcher, and
[`brain/heron_matcher.py`](../brain/heron_matcher.py) returns `matched`, `partial`, `excluded` and
`brief` — no score anywhere, and not by oversight. Its own rule is *"a near match is not a match"*: a
fragment short of one declared need will run, half-work and look exactly like a success, so it goes in
`partial` where **no caller can reach it by reading `matched`**. Putting `0.9` beside it is the precise
thing that agent was built to refuse.

### What was built instead

The gate is enforced in the Matcher's own vocabulary, which is **stricter** than the percentage rather
than looser:

| the Matcher says | `HERON-FRG-CRE-007` does |
|---|---|
| `matched` is non-empty | refuse — Golden Rule 3, reuse proven knowledge before creating new |
| `partial` is non-empty | refuse — this IS the "≥80%" case, and the answer names what to start from |
| both empty | author, at `DRAFT` |

A percentage would let `79%` through. `partial` does not.

### What is proposed

One of two, and both are the owner's:

1. **Restate §151 in the Matcher's vocabulary** — "if the Matcher reports anything in `matched` or
   `partial`, generation may not start from scratch" — and note that this is stricter than the original
   80%.
2. **Make the Matcher score**, and accept that a number beside a near match is the thing
   `HERON-RAG-FMT-004` refuses to write. Its module says why at length.

**Not acted on.** §151 is a `[NOTE]` proposing enforcement, and changing what it proposes is not a
build decision. The agent is built to option 1 today and says so in its own answer.

---

## F25 — should the import walk skip `.git`, `node_modules` and build folders?

Found while building [`HERON-IMP-FIL-002`](../brain/heron_walk.py), whose register row is *"Walks the
folder, identifies file types"*.

[docs/10 §5](10-memory-and-knowledge.md) names the folders this feature exists for — `AJ-Tools`,
`PyRevit-Tools`, `AEB-Tools`. Every one of those is a working folder, and a working folder is rarely
only source:

| what is really in there | what a complete walk reports |
|---|---|
| `.git/` | thousands of entries, nearly all with no extension — they land in `unnamed` |
| `__pycache__/`, `bin/`, `obj/` | `.pyc`, `.dll`, `.pdb` — outputs, not knowledge |
| `node_modules/` | tens of thousands of `.js` |

**The walk skips none of them today, and that is deliberate.** A skip list is a guess about somebody
else's folder. `.git` is a safe guess; `bin` is not — plenty of people keep hand-written tooling in a
folder called `bin`, and a silent skip would drop exactly the fragments the import exists to find.
Golden Rule 14 says never silently discard, and a filter nobody asked for is a silent discard with a
sensible-sounding name.

### What it costs to leave it

The manifest [`HERON-IMP-APR-014`](../brain/heron_import.py) presents is the thing a human reads
before anything is committed. *"Found 47 candidate fragments"* is reviewable. *"Found 61,400 files,
54,000 of them with no extension"* is not — the review that constraint 2 depends on stops being
possible. So this is not cosmetic.

### What is proposed

One of two, and both are the owner's:

1. **A skip list the user sees and can turn off**, shipped with `.git`, `__pycache__`, `node_modules`,
   `bin` and `obj` in it, and reported in the answer — *"skipped 54,013 entries in 4 folders"* — so a
   skip is never silent and the reviewer can ask for the full walk.
2. **Read the folder's own `.gitignore`**, the way [`HERON-GIT-CMT-004`](../brain/heron_commit.py)
   reads Heron's. It is the author's own statement about what is not source, so it is not a guess —
   but it covers `bin` and `obj` and does **not** cover `.git`, so it is half an answer at best.

**Not acted on.** Neither is derivable from anything written down, and the first one is a list of
names somebody has to choose.

---

## F26 — the import pipeline is ordered *index → save*, and both indexes read the library

Found while building [`HERON-IMP-IDX-013`](../brain/heron_index.py), whose register row is *"Indexes
the accepted result"*.

[docs/00 §28](00-master-specification.md) numbers the pipeline, and [docs/10 §5](10-memory-and-knowledge.md)
repeats it as a chain. Both end the same way:

> … 13. Update metadata. **14. Validate. 15. Index. 16. Save.**

Heron already has the two indexes that step 15 means, and **both of them read the library**:

| index | agent | what it reads |
|---|---|---|
| keyword | `HERON-RAG-IDX-010` — [`heron_search.index`](../brain/heron_search.py) | `store.fragments()` and `heron_fragment.load_all()` |
| vector | `HERON-RAG-EMB-008` — [`heron_embed.index`](../brain/heron_embed.py) | the same two |

Neither takes a list of items. An accepted fragment that is still only a manifest row has no store row
and no file on disk, so **step 15 cannot see it until step 16 has run.** The order as written does not
execute.

### What was built instead

`HERON-IMP-IDX-013` does not resolve it. It calls the two indexes and then reports every accepted
fragment the library does not hold, by id, in `not_in_the_library` — so the pipeline owner sees exactly
which items the index could not reach, instead of a success count that quietly covers them.

### What is proposed

One of two, and both are the owner's:

1. **Read the order as written, and read "save" as something later than writing to the library** —
   committing the import session, or releasing the manifest. Then the item really is in the library
   before step 15, the numbering is right, and the word `Save` needs one sentence in docs/10 §5 saying
   what it saves.
2. **Swap steps 15 and 16** in both documents, so the chain reads *validate → save → index*. That is
   what the code does today and what `HERON-IMP-MAIN-001` will have to do whatever these documents say.

**Not acted on.** Both specifications say the same thing in the same order, which makes it a deliberate
sentence rather than a typo, and changing what a specification means is not a build decision.

---

## F27 — five Development rows are already built, and whose work they measure decides by which file

Found while building [`HERON-DEV-NET-006`](../brain/heron_dotnet.py), which turned out to be the
read-only half of a tool carrying `Heron-Agent: none`. It is not the only one.

Five more Development rows have an existing, working, unclaimed file that matches them — and for four
of them there are **two** candidates, which one is right depending on a question nobody has answered:
**does a Development agent act on the artefact Heron is building, or on Heron itself?**

| row | on the artefact | on Heron |
|---|---|---|
| `DEV-BLD-010` *compiles across all target frameworks* | `tools/check-fragments-compile.py` — every fragment's C#, every release it claims | `tools/check-compile.py` — the four projects, 2020 to 2027 |
| `DEV-UNT-011` *runs unit tests* | — | the `tests/test_*.py` sweep, which no file owns |
| `DEV-INT-012` *integration tests against a mocked Revit boundary* | — | `tests/Heron.Bridge.TestHost` + `test_bridge_roundtrip.py` |
| `DEV-RGR-014` *golden-file comparison across supported versions* | `tests/golden/cases.py`, `tests/test_golden.py` | — |
| `DEV-PRF-015` *execution time and resource cost* | — | `tools/measure-brain.py`, which measures exactly those two words |
| `DEV-PRF-015` **again — a THIRD candidate, 2026-09-16** | — | `brain/heron_devperf.py`, which times the test SUITES against check-gaps' bound. Built claiming this row and **un-claimed the same day on reading this section**; its header is `none` and the reasoning is in its docstring |

### The precedent points one way and the files point the other

The **one** Development row already claimed by a tool is `DEV-RVT-013`, on
[`tools/batch-prove.py`](../tools/batch-prove.py) — and that tool proves **fragments**. Under that
reading the department is the build pipeline for the artefact, `DEV-PRF-015` measures a fragment's cost,
and `measure-brain.py` measures the *builder* rather than the thing built.

Under the other reading, the repository is the artefact — which is how `DEV-REL-018` is already claimed
by `tools/check-package.py`, and that packages **Heron**, not a fragment.

**So the two rows already claimed disagree with each other.** That is not something a build decision can
settle.

### What was done instead

Nothing was relabelled. `tools/measure-brain.py` gained the half it was missing — docs/28 asks
`DEV-PRF-015` for *"execution time and resource cost"* and it measured only time — and its header still
reads `none`, with the reasoning in its docstring.

Its old justification for `none` was **stale** and has been corrected: it argued against claiming
`HERON-OPS-OBS-011` on a row that [D-58](DECISIONS.md) rewrote on 2026-09-09. That agent is built in
`brain/heron_observability.py`, and the corrected row **names `measure-brain.py`** as what measures the
latency half.

### What is proposed

**Say which reading governs**, in one sentence in docs/28 §9's heading. Then five rows close by claiming
files that already work, and `agent-count.py` stops reporting as unbuilt five things that are built.

### SETTLED 2026-09-16 — [D-75](DECISIONS.md): a Development agent acts on HERON ITSELF

The sentence is in docs/28 §9's heading and the department's subject is Heron's own code.

**It closed one row, not five, and the table above is what says so.** The proposal counted candidates
without reading which column they were in. Taking them column by column: `DEV-BLD-010` closes on
`tools/check-compile.py`; `DEV-UNT-011` has no file to claim because the suite sweep is inline bash in
`gates.yml`; `DEV-INT-012`'s only candidates are layer `test`; `DEV-RGR-014`'s only candidates are on
the fragment side, which the decision rules **out**; and `DEV-PRF-015` has two candidates that are both
on Heron, so the decision does not separate them. D-75 carries the row-by-row detail.

**A count of candidates is not a count of answers** — which is the same shape as this section's other
two findings, one layer up again.

### It happened, on 2026-09-16, and the check written to prevent it is what let it through

`brain/heron_devperf.py` was built claiming `DEV-PRF-015`, merged as **#159**, and un-claimed hours later on reading this section. **The row now has three candidates and one id.**

The build was not careless about it. Every candidate agent was first checked against this file and [OPEN-QUESTIONS](OPEN-QUESTIONS.md) for a recorded blocker, and `PRF-015` came back clean. **The grep searched for `HERON-DEV-PRF-015`. This section writes it `DEV-PRF-015`, with no prefix**, so the pattern could not see the one row that was about it.

**The same grep wrongly cleared nine others** — `DEV-BLD-010`, `DEV-INT-012`, `DEV-RGR-014`, `DEV-UNT-011` (all four in the table above) and `STD-BIM-001`, `STD-DOC-008`, `STD-LOD-007`, `STD-MOD-005`, `STD-QAQ-006` (all five in [F31](#)'s — **and this sentence said F29 when it was first written, which is the same defect one layer up: a reference nobody followed**). Ten of fifteen agents reported as having no recorded blocker, every one of them written about here.

This repository's rule is **prove the pattern can see what you know is there**, and it was broken by the check written to enforce it. Search an agent id **without its `HERON-` prefix** — `grep -n 'DEV-PRF-015' docs/*.md` — or the register's own spelling will hide the row.

### And then the withdrawal itself was half done

Blanking the two `Heron-Agent:` headers did not withdraw the claim.
**`brain/agents/HERON-DEV-PRF-015.yaml` was still sitting there, and a contract in that folder IS the
claim** - each of the other 125 stands for a counted agent. So the repository held 126 contracts for
215 agents, and one of them named no suite at all.

Nothing in the withdrawal noticed. `tests/test_contract_reference.py` did, on the next CI run, in the
arithmetic it asserts for precisely this reason: *126 with a contract plus 90 without is 215* - which
is 216. The contract is gone now, and the three failure names it declared live in
`brain/heron_devperf.py` beside the code that produces them, checked against that code's own source
instead of typed into the test.

**Undoing a claim touches every place that makes it.** Here that was three: two headers, and a file
whose existence was the third.

---

## F28 — the import classifies into five words the workspace has never heard of

Found while building [`HERON-IMP-ARC-011`](../brain/heron_belongs.py), whose register row is *"places
content where the architecture says it belongs"*.

Two agents in the same pipeline use two vocabularies, and **they share no word at all**:

| agent | sorts into |
|---|---|
| `HERON-IMP-CLS-003`, step 10 *classify content* | code · documentation · config · metadata · asset |
| `HERON-WSP-PLC-005`, step 12 *move into architecture* | knowledge · skill · fragment · memory · project · company · log · backup |

Not one word in common. The overlap is checked in `tests/test_belongs.py` against both agents' real
lists rather than asserted in prose.

### Two of the five have an answer; three do not

`code` and `documentation` are fine — they are not meant to be filed as they stand. Code becomes a
fragment (`IMP-FEX-004`) or a skill (`IMP-SEX-005`), both of which *are* kinds; documentation is
ingested into a **scope** (`RAG-DIS-002`), which is not a data folder at all. The agent says so and
names who runs first.

**`config`, `metadata` and `asset` have nowhere to go**, and the reason is one this repository already
found. [`HERON-WSP-CRE-002`](../brain/heron_folders.py) reported that [docs/06 §2](06-heron-platform.md)
draws **nineteen folders and classifies fourteen** — RAG, Community, **Configuration**, Tests and
Documentation appear in the tree and in no class.

So `config` has a folder with no class, and `asset` has neither.

That is not cosmetic. The class is the only thing that decides whether a product update may replace a
folder wholesale ([docs/07 §7](07-installation-and-update.md) rule 6), whether a cleanup may delete it,
and whether a backup covers it. **File an imported `.ini` into an unclassified folder and nobody can say
whether the next update deletes it.**

### What was built instead

`HERON-IMP-ARC-011` refuses rather than picks. Items carrying one of the five never reach
`HERON-WSP-PLC-005` — that agent would refuse them as though somebody had guessed at a ninth folder,
when what really happened is that an earlier step in the same pipeline produced a vocabulary nothing
downstream reads.

### What is proposed

One of two, and both are the owner's:

1. **Classify the five leftover folders** in docs/06 §2, which closes `config` and settles the four
   other unclassified folders at the same time. `asset` still needs a home named.
2. **Say that an import produces only fragments, skills and documents**, and that `config`, `metadata`
   and `asset` are recorded in the manifest and **not imported** — which is a defensible answer and
   should be written down rather than left to each agent to discover.

**Not acted on.** Either choice changes what an import *is*, and both need a folder classified or a
category dropped.

---

## F29 — the panic button cannot press itself, and Revit is the reason

Found while building [`HERON-REVIT-RBK-036`](../brain/heron_rollback.py), whose register row is *"the
panic button. Reverses **everything** Heron did this session, newest first, using the audit log's
transaction groups."*

**Revit exposes no API to undo a NAMED transaction group.** This was measured, not assumed:
`PostableCommand.Undo` was compiled against the reference assemblies for **2020, 2024 and 2027** and
exists on all three — so Heron *can* post an undo. What it cannot do is choose which one. An undo takes
the **top of the stack**.

Two things follow, and neither is a defect in this repository:

**1. A rollback is N undos, and N is not knowable from the trail.** The undo stack belongs to the
*document*, shared with the person modelling in it. If they moved a wall after Heron's last change, that
wall sits on top of Heron's entry — and nothing Heron writes records what a person did between two Heron
operations. The agent therefore reports **how many undo entries Heron made**, in order, and says plainly
that anything done in between goes first and is not counted. A number that quietly ignored the user's own
edits would be the most dangerous number in this project.

**2. The executor half does not exist.** Posting the undos has to happen inside Revit, in an API context.
`brain/heron_rollback.py` is the planner: it groups by document (an undo stack belongs to one), orders
newest first, and reverses nothing.

### What is proposed

One of two, and both are the owner's:

1. **Build the executor as a `MODIFY` bridge operation** that posts N undos into one document and
   reports what it posted. It is small — the planner already says exactly what to post — but it is a
   write path, and [`HeronPermissions`](../platform/Heron.Core/HeronPermissions.cs) keeps writing off
   until NEEDS-CHECKING group D has passed. It should not be the *second* write path built before the
   first has ever run in Revit.
2. **Say that the panic button is a plan a person carries out** — Heron prints "press Ctrl+Z twice in
   Tower B MEP" — and change the row's word from *reverses* to *tells you how to reverse*. That is
   honest, needs no new write path, and is arguably safer: the person looking at the model is better
   placed than Heron to see what else is on the stack.

**Not acted on.** Option 1 adds a write path before the existing one has been proven, and option 2
changes what the register promises.

---

## F30 — a reference model's profile carries the client's own job number

Found while building [`HERON-STD-REF-010`](../brain/heron_exemplar.py), the Reference Model Profiler —
by a test that failed, and the test was right.

[docs/10 §5a](10-memory-and-knowledge.md) gives the worked example:

> *"Tower A uses `MEP-DUCT-SUPPLY-L03`; this model has 47 ducts that do not match that pattern."*

That example has no project code in it. **Real delivered models usually do.** On a real job every name
starts with the job number — `QA-2026-ASHGHAL-MEP-DUCT-SUPPLY-L01` — and the profiler records the shape
as delivered:

| name | shape |
|---|---|
| `QA-2026-ASHGHAL-MEP-DUCT-SUPPLY-L01` | `A-9-A-A-A-A-A9` |
| `MEP-DUCT-SUPPLY-L07` (a different project) | `A-A-A-A9` |

So the profile learned from Tower A **cannot match anything in Tower B**, and checking a second model
against it would flag every single element. The capability that exists to save somebody writing the
standard out produces one that fits exactly one building.

### Why the agent does not just strip it

Because stripping means **guessing which segment is the project**. `QA-2026-ASHGHAL` is three segments
here and one on the next job; a discipline code like `MEP` sits in the same position on some standards.
Removing a segment that turns out to be a real part of the convention would teach the opposite of the
truth — and docs/10 §5a's whole argument is that the delivered model *is* the truth.

The agent reports the shape as delivered and says this out loud in its own answer rather than leaving it
to be discovered against a second model.

### What is proposed

One of three, and all of them are the owner's:

1. **Ask which segment is the project, once, when the reference is offered** — D-33 exactly. One
   question, answered by somebody who knows the job, and the profile is then portable.
2. **Profile TWO delivered models at once** and treat the segments that DIFFER between them as the
   project code. That is derivation rather than a guess, and it also satisfies docs/10 §5a's own
   corroboration guard in the same step — but it needs two references before anything can be learned.
3. **Accept it and say so**: a profile is per project, and a second project needs its own reference.
   Cheapest, and it loses most of what the capability was for.

**Not acted on.** Option 2 is the most interesting and it changes what the agent takes as input.

---

## F31 — five Standards rows differ only by their subject

Raised on the authority of [`HERON-AHR-WFP-015`](../brain/heron_workforce.py), the row whose job is to
say **no**: *"before anything is hired — does a capability already cover this, can an existing agent be
extended, is this a fragment rather than an agent? This is the guard against agent explosion."*

Five rows are left in Standards & BIM QA, and after building four of that department they look like one
agent with a different noun in front:

| row | what it would do | what is already built |
|---|---|---|
| `STD-BIM-001` *applies a stated BIM standard to a model* | cite clauses, check a model | `STD-CMP-002` cites; `QA-BIM-011` checks |
| `STD-MOD-005` *connections, elevations, practice* | cite clauses about modelling | `STD-CMP-002`, with `subject = modelling` |
| `STD-QAQ-006` *the organisation's QA process requirements* | cite clauses about process | `STD-CMP-002`, with `subject = QA process` |
| `STD-LOD-007` *level of development expected at this stage* | cite clauses about LOD | `STD-CMP-002`, plus a **stage** |
| `STD-DOC-008` *sheet, titleblock and annotation requirements* | cite clauses about documentation | `STD-CMP-002`; sheets and titleblocks are names, which `QA-BIM-011` already routes |

`HERON-STD-CMP-002` opens one scope, retrieves clauses, quotes them, keeps the four nothings apart and
hands the host the question. **Every one of these five is that, with a different search term.** Writing
five files that differ by one string is the agent explosion WFP-015 exists to prevent.

### What would genuinely distinguish each one

Worth stating, because two of them nearly have something:

- **`LOD-007` has a second input.** *"Expected at this STAGE"* — the answer depends on where the project
  is, which is a filter no other standards agent takes. That is a real difference of shape, not of
  subject.
- **`BIM-001` and `MOD-005` want to check a MODEL**, not just cite. The checkable half is
  `QA-BIM-011`'s and the rest — connections, elevations, geometric practice — needs geometry inside
  Revit, which nothing on this side can reach.
- **`QAQ-006` could ask whether the process was FOLLOWED**, and the audit trail is evidence. But its row
  says *requirements*, not compliance, so building that would be inventing a different job.
- **`DOC-008` is almost entirely already done.** Sheets, titleblocks and view names are names, and
  `QA-BIM-011` routes all of them to `STD-NAM-004`.

### What is proposed

One of two, and both are the owner's:

1. **Extend `HERON-STD-CMP-002` with a `subject`**, and record all five rows against that one file. Then
   `agent-count.py` shows five more built, one file is maintained, and `LOD-007` gets its stage
   parameter as the one genuine addition. This is what WFP-015's ladder — *keep, extend, adapt,
   version-branch* — points at.
2. **Build five files that differ by a search term**, because the register says five rows and a row is a
   row.

### SETTLED 2026-09-16 — [D-76](DECISIONS.md): option 1, and Standards is now 14 of 14

`brain/heron_company.py` takes a `subject`, its header claims six ids, and the five rows are recorded
against it. `LOD-007` got its `stage` as the one genuine addition. **221 built, 25 left.**

Three of the five are PARTIAL and each says so in its own answer rather than only in a register: `bim`
and `modelling` cite but cannot reach a model or its geometry, and `documentation` is mostly names that
`QA-BIM-011` already routes to `STD-NAM-004`.

**It did NOT settle both departments, which this section expected it to.** [F27](#f27--five-development-rows-are-already-built-and-whose-work-they-measure-decides-by-which-file)
turned out to be a different question — not *may one file carry several rows* but *what is this
department's subject* — and [D-75](DECISIONS.md) answered that one separately. Two proposals can look
like one question and be two.

---

## F32 — `redact()` returns two values and reads like it returns one

**Found while building `HERON-DEV-SEC-009`** (Security Review Agent), which has to answer *"is there
anything credential-shaped in this change"* before handing the evidence to a reviewer.

### What happened

The first version of that agent asked the redactor directly:

    hidden = keeper.redact(text)
    if hidden != text:        # ...then this field held a secret

That reasoning is sound and the code is wrong. `HERON-KRN-SEC-012.redact` returns **`(clean, found)`** —
a tuple — because a redaction nothing reports is a leak nobody can investigate, which is the right
design. But a tuple is never equal to a string, so the compare was true for **every non-empty field**.
The demo reported *five* credentials in a change carrying one: `allowed-tools`, `by`, `name`, `risk`,
`token`.

### Why it is worth writing down

It fails in the direction that looks like it is working. A security check that says *"5 credential-shaped
fields"* looks vigilant. Nobody chases a false positive as hard as a false negative, and the same
mistake made one line later — `hidden, found = ...` with the halves swapped — gives a check that finds
nothing and reports clean.

The verb is the problem. `redact(text)` reads as *"give me the redacted text"*, and every caller who
reaches for it is reaching for one value. The second return is the one that matters and it is invisible
at the call site.

### What was done here

`HERON-DEV-SEC-009` does not call `redact` at all. It binds **`Secrets.refuse_secret_input`**, which is
the method already built to answer this exact question, and which settles two further things the loop
got wrong on its own:

- **It judges the value, not the key name.** Its own comment says so — *"calling the field `token` is not
  what makes it dangerous"*. A field called `name` holding `"Ajmal"` is not a credential; a field called
  `blurb` holding a forge token is.
- **It walks nested maps and lists.** `{"auth": {"header": "Bearer ..."}}` is how a credential actually
  arrives — nobody puts one in a bare top-level field called `secret` — and a top-level loop walks
  straight past it.

### What is proposed

Nothing in `heron_secrets.py` is changed by this. Two options, and both are the owner's:

1. **Leave it, and let the composition rule carry the weight.** Golden Rule 3 already says reuse before
   creating, and a caller that reaches for `refuse_secret_input` instead of `redact` never meets the
   trap. This is what was done here.
2. **Rename it `redact_and_report()` or return a small record** with named halves, so the second value is
   visible at the call site rather than in the docstring.

**Not acted on.** Option 2 touches `HERON-KRN-SEC-012` and everything already calling it, and the agent
that found the problem is not the agent that should change a kernel interface.

---

## F33 — three contracts promised less than their code does, and nothing could see it

**Found by [`HERON-DEV-DOC-017`](../tools/generate-contract-reference.py) on its first run**, across 121
contracts. Not a proposal so much as a record of what the check found and why it could not have been
found before.

### The gap it closes

Every agent's suite checks that its own module names its own declared failures. That check is real and
it has caught things. But it is **one agent at a time**, and it only runs in one direction: *is every
promise kept?* Nobody had asked the other question — *is everything the code refuses actually promised?*
— and that is the direction that breaks at run time. A caller that handles every declared failure and
still meets an undeclared one has done everything the contract asked.

Three were undeclared:

| agent | refusal | how it comes back |
|---|---|---|
| `HERON-AHR-CRT-006` | `NEEDS_A_REGISTER_ROW` | the pipeline stops; a row in docs/28 is a person's decision |
| `HERON-AHR-REG-008` | `CONTRACT_DUPLICATED` | raised — two files declaring one agent's interface |
| `HERON-KRN-PRO-011` | `INSTRUCTION_DUPLICATED` | raised — one instruction key declared by two files |

All three are now declared. **Every declared refusal in the repository is reachable**, and every refusal
is declared.

### It also found a gap in a suite, by fixing a contract

Declaring `NEEDS_A_REGISTER_ROW` turned `tests/test_creator.py` red: its "every declared failure is
reached" check said the new one was not. It was — by a case two lines above it. The suite collected
`answer["refused"]` and `HERON-AHR-CRT-006` halts by calling `stop()`, which puts the reason in the step
record and leaves `stopped_at` and `waiting_on` at the top. **A stop is a refusal in a different shape**,
and the suite now reads both.

### What the count went through, because it matters more than the count

| reading | undeclared | why |
|---|---|---|
| every `SCREAMING_SNAKE` string | **135** | capability names, stated shapes, module constants, and examples an agent quotes to say it does *not* do that |
| ...ignoring docstrings | 135 | no better — the noise was never in docstrings |
| by position, underscore required | 5, plus **37 false "broken promise"** | `INCOMPLETE`, `MODIFIED`, `PINNED` are single-word refusals |
| ...following delegation to excuse | 5 | a pass-through is composition, not a broken promise |
| ...not reading a suite as its agent | 4 | `NOT_A_FOLDER` was in `tests/test_classify.py`'s fixtures |
| ...not reading an appended card | **3** | `WRONG_REVIT_VERSION` excludes one fragment; the call succeeds |

**A page of findings that are all wrong is worse than no page.** It teaches the reader to skip the table,
which is where the real ones are. Four of those six steps were the agent being wrong about somebody
else, and each was caught by checking the finding against the source before believing the page — which
is now what `tests/test_contract_reference.py` does for every finding, every run.

### Nothing is proposed

The three contracts are fixed, the suite is fixed, and the check runs on demand. Recorded because the
*shape* of the mistake generalises: a check that reports a real problem and forty false ones gets
switched off, and the real problem goes with it.

---

## F34 — D-52 is enforced in Python and unchecked in C#

**Found while building [`HERON-DEV-CSH-005`](../brain/heron_csharp.py)**, which had to establish what
this repository's C# idiom *actually is* before it could hold an opinion about anything.

### The measurement that killed the obvious rule first

Across the 23 C# files in `revit/`, `platform/` and `mcp/` there are **74 catch clauses**:

| | |
|---|---|
| `catch` with no type at all | **30** |
| `catch (Exception)` | **18** |
| a narrow type | 26 |

So a *"narrow exception types only"* rule — the first thing a C# agent wants to say — is **false about
two thirds of the code that already ships**. It is not asserted, and the agent says so in its answer.
A rule contradicted by the working code is an agent correcting that code on an authority it does not
have.

### What is true, and nothing checks it

[D-52](DECISIONS.md) is **the plausible zero**: an answer that is wrong in a way nobody doubts.
[`tools/check-narrow-errors.py`](../tools/check-narrow-errors.py) enforces it, and enforces it well — its
own docstring records reviews finding the same defect in four files on four consecutive days, which is
why it became a tool. But it reads **Python only**, and **`sqlite3` handlers only**, because that is
where it kept happening.

Of the 48 broad C# handlers, **27 leave a fault and the normal case indistinguishable**:

| shape | count | what the caller gets |
|---|---|---|
| `catch { }` — empty block | **14** | the fault leaves no trace at all |
| `catch { continue; }` | **10** | the item drops out of the loop and the count comes back smaller ([Golden Rule 14](14-golden-rules.md)) |
| `catch { return null; }` / `false` / `new T()` | **3** | a fault comes back as *"nothing found"* — D-52 in as many words |

### Not one of them is claimed to be a bug

Several are certainly right. Iterating every value of a large enumeration and skipping the ones that do
not resolve is a normal thing to do, and `RevitFragment.cs` does it on purpose. Whether a swallow is
correct depends on what can actually be thrown at that call, and **only a person reading it knows**. So
the agent names the line and the shape and stops there; `judged_wrong` is false and always false.

### What is proposed

Three options, and they are the owner's:

1. **Nothing.** The agent exists, it can be run over any file, and a reviewer can ask it. That is what
   was done.
2. **Extend `check-narrow-errors.py` to C#**, so the 27 are triaged once and the rule holds in both
   languages. The risk is a gate that fails on 27 pre-existing handlers on its first run, and a gate
   that has to be suppressed to be introduced teaches people to suppress it.
3. **Triage the 27 by hand**, leaving a one-line comment beside each saying what it expects to catch —
   which is what `RevitLinks.cs` already does for its four narrow ones, citing Golden Rule 14.

**Not acted on.** Option 2 changes a gate everyone has to pass, and option 3 edits 27 handlers across
files this agent did not write.

## F35 — half a hole is closed, and the other half needs the checks to change first

**Found by the second Codex review on PR #142** (`brain/heron_qa.py:174`, P1), worked on 2026-09-16.

### What the reviewer said

> A result omitting `of` skips the freshness check, so an old or fabricated `{check, passed: true}`
> satisfies the final gate for an unrelated change.

**It is true, it was reproduced, and it is not one line to fix.**

### What was closed

A result with **no** fingerprint sitting beside one that **has** is now refused —
`RESULT_IS_UNFINGERPRINTED`. The argument: once a single check in a run recorded what it ran
against, a result that did not is a check that *did not record*, not one that *cannot*. That is
precisely the company an old or fabricated result would be keeping, so it is the half worth
closing on its own.

The concession is also no longer only prose. `unfingerprinted` is a field in the answer naming
every required check whose result could not be tied to the change, so a caller that wants to
refuse on it can, without reading `unjudged`.

### What is still open, and why it was not closed here

**A run where NO result carries a fingerprint still passes.** The agent's own suite says so
deliberately:

> a result carrying NO fingerprint is accepted — most checks do not record what they ran
> against, and refusing them would make this unusable

That is a real constraint, not a lapse. `gate()` reads results a caller hands in; it runs
nothing itself, so it cannot fingerprint on the checks' behalf. Requiring `of` on every result
today refuses every run, which is why the handover called this a **sequencing decision**.

**Nothing in `brain/` or `mcp/` calls `gate()` — only `tests/test_qa.py` does.** So the cost of
making it strict is currently a test, not an outage. That is an argument for doing it, and it is
also exactly why it should be a decision rather than a quiet change: the moment a real caller
appears, strictness is either already there or it never arrives.

### What is proposed

Three, and they are the owner's:

1. **Nothing more.** The mixed case is refused, the rest is named in `unfingerprinted`, and a
   caller can decide. That is what was done.
2. **Make the checks record.** Have whatever produces a check result — `tools/change-evidence.py`
   is the obvious candidate — stamp `of` with `heron_security.fingerprint` of the change it ran
   against. Then option 3 costs nothing.
3. **Require `of` on every result now**, refusing a run where any required check cannot be tied
   to the change. Correct, fails closed, and today it would refuse every caller that exists —
   which is one test.

**Not acted on.** 2 is the sequencing the handover named; 3 without 2 is a gate nobody can pass,
and a gate that has to be suppressed to be introduced teaches people to suppress it (the same
argument as F34 option 2).


## F36 — D-59's 62 edits would un-prove 56 fragments, and four of them must not be made at all

**Found on 2026-09-16**, doing the first implementation [D-59](DECISIONS.md) asked for, with a .NET SDK
installed in the container ([§30](30-compiling-away-from-windows.md)) so the C# could actually be
compiled rather than guessed.

### D-59 says nothing is invalidated. That is true of behaviour and NOT of proofs

> **It does not change what any fragment does today.** Absent input means host only, which is what all 62
> already do and what every existing proof measured. Nothing is invalidated by this decision.

The first half is right. The second is right about **what the code does** and wrong about **what the
repository will say about it**, because a proof is not stamped against behaviour — it is stamped against
the bytes:

```
Fragment.fingerprint()   sha256 over the implementation files
proof_is_stale()         recorded != fingerprint()
```

Editing `impl/any/fragment.cs` changes the hash, so the fragment's recorded proof goes **stale** and its
`PROVEN` claim stops holding. That is [D-30](DECISIONS.md) working exactly as intended — it is the same
rule that put `set-view-section-box` back to `DRAFT` when its implementation changed after its proof was
signed.

**Derive the cost rather than reading it here:**

```bash
python tools/check-revit-gate.py --list links
```

On 2026-09-16 that was **65 fragments, and 56 of them are `PROVEN`.** Making the 62 edits in one pass
would take the library from 310 proven to **254**, and not one of them could be re-proved in a container:
re-proving needs Revit open with a federated model.

### What follows from it

**The link contracts are a PC job, not a container job** — not because the C# cannot be written here (it
can now, and it compiles), but because the *proof* has to be re-taken in the same sitting or the number
on the front of this repository falls by 56 with nothing to show for it. They should be done in a batch,
with a federated model open, and re-proved as they go.

**The nine `DRAFT` ones are free** and are where a first implementation belongs. `report-areas` was done
that way on 2026-09-16: DRAFT, no proof block, nothing to invalidate.

### And four of the nine must not be done at all

D-59 excluded the 45 writers **on an API fact rather than a judgement** — a linked element belongs to
another document and cannot be changed through this one. The same fact excludes a second group it did
not name:

| flagged, and `DRAFT` | why links do not apply |
|---|---|
| `select-by-electrical-circuit` | |
| `select-by-host` | |
| `select-touching` | `provides: elements` — and an `ElementId` from a linked document means nothing in the host's selection |
| `select-openings` | |

A `select-*` fragment hands back `IList<Element>` for `SET_SELECTION` to highlight and
`ISOLATE_ELEMENTS` to leave on screen. Elements from a link cannot be selected that way, so a
link-spanning `select-*` would return ids that the next fragment in the chain silently drops — a worse
failure than the confident smaller number D-59 exists to prevent, because it looks like it worked.

**Three more of the nine are questions about THIS model** and gain little: `find-unused-families`,
`find-unused-materials` and `check-model-standards` ask what is unused or non-compliant *here*, and
nobody purges a link.

**So the gate's count is an over-count**, and it should be: it asks one question of every fragment and a
question cannot know which answers are meaningful. But the worklist derived from it needs a judgement
per fragment, not a bulk edit — which is the opposite of how 62 identical-looking rows read.

### What is proposed

1. **Nothing, beyond what was done.** `report-areas` carries the pattern, it compiles on all eight
   releases, and the rest stay a worklist. That is what was done.
2. **Teach the gate the two exclusions**, so `select-*` and the purge questions stop appearing in a list
   headed *"handled incorrectly"*. The risk is a gate that hides a real case behind a rule somebody
   wrote once.
3. **Record the proof cost in D-59 itself**, so the next person reading *"nothing is invalidated"* is not
   surprised by 56 fragments dropping to `DRAFT` halfway through an afternoon.

**Not acted on.** 2 changes a gate everyone reads, and 3 edits a decision record that is append-only.

## F37 — a refusal was resting on a majority, and one file tipped it

**Found on 2026-09-16**, building `HERON-REVIT-PAR-011` (`RevitParameters.cs`). The suite went red on a
check nobody had touched, and the check was right.

### What flipped

`brain/heron_csharp.py` is the C# idiom agent, and its whole design is a refusal: it will not assert
*"use narrow exception types"* as a rule. Its stated reason was a measurement of this repository's own
code —

> A "narrow exception types only" rule would be false about the larger half of the code that already
> ships. It is not asserted here.

— and `tests/test_csharp.py` guarded that reason with `check(broad > narrow, ...)`, so the claim could
not quietly rot.

One new file moved it. `RevitParameters.cs` carries sixteen exception handlers and every one is narrow,
because that is what [D-52](DECISIONS.md) and `tools/check-narrow-errors.py` ask for:

| | broad | narrow |
|---|---|---|
| before | 53 | 42 |
| after | 53 | **58** |

Narrow leads for the first time. Reproduce it:

```bash
python tests/test_csharp.py     # prints the live figures in section 3
```

### The refusal survives. Its stated reason did not

Nothing was rewritten to restore the old balance — the new file follows the better style, and degrading
it to keep a docstring true would be the tail wagging the dog.

What changed is the claim. **The majority was never the real ground for the refusal.** Fifty-three broad
handlers still ship and still work, and an agent that flags fifty-three working handlers is correcting
working code on an authority it does not have — which is as true at 58–53 as it was at 42–53. A claim
that can flip on a single commit was the wrong thing to hang a refusal on, and it flipped on a single
commit.

So the suite now checks the share that does not tip: broad handlers are at least a quarter of all
handlers. Crossing that needs somebody to deliberately rewrite most of them, which is an event worth a
red suite.

### The question that is actually open

**Should `heron_csharp` now begin asserting the rule?**

The honest position is that the answer has moved and nobody has decided it. The arguments have not
changed sides so much as changed weight:

- **For.** New C# in this repository is narrow, consistently — `RevitLinks`, `RevitPhases`,
  `RevitSystems` and `RevitParameters` are 36 narrow handlers and zero broad between them. A rule would
  describe what the authors already do, which is the only kind of style rule worth having.
- **Against.** The 53 broad handlers are not a backlog anybody intends to clear. `check-narrow-errors.py`
  already enforces the shape that actually costs — D-52's plausible zero, where a fault and the normal
  case come back identical — and it enforces it where the defect kept happening. A rule about catch
  *syntax* is a wider and weaker thing than a rule about swallowed faults.

**Not acted on.** Asserting it would turn 53 working handlers into findings on the day it ships, and
whether that is worth doing is a judgement about this repository rather than about C#.

## F38 — the repository went public and two documents still say it did not

**Found on 2026-09-16**, updating the README after a run of agent work. Not found by a gate — no gate
checks a sentence about a fact outside the repository.

### One file, two answers

[`README.md`](../README.md) carried both of these:

> *This repo is public only so the work is open.* — the banner, at the top

> *It is still **private**.* — "A note on this repository", at the bottom

A reader meets the banner first and the considered-sounding paragraph last. Confirmed against GitHub:
the repository **is public**. The bottom paragraph is corrected.

[D-07](DECISIONS.md) carries the same staleness:

> publishing to public is irreversible in practice and **has not been done** — it needs an explicit
> instruction from the owner, once the licence and the public/private file separation are in place

That file is **append-only**, so nothing was edited there. This row is how it gets closed.

### The part that is not a documentation bug

D-07 set four conditions. Three are plainly met — Apache 2.0 ([D-08](DECISIONS.md)),
[`DISCLAIMER.md`](../DISCLAIMER.md), [`SECURITY.md`](../SECURITY.md). The fourth is **the public-code /
private-knowledge separation being verified**, which D-07 itself calls the one that matters most.

**No record of that verification exists in this repository.** Derive it:

```bash
grep -rn "separation" docs/DECISIONS.md docs/17-open-source-and-distribution.md
```

There is a strong argument that it holds. Knowledge is written outside the repository by construction —
[`brain/heron_scope.py`](../brain/heron_scope.py) refuses to open a store at all without `%APPDATA%` or
`HERON_KNOWLEDGE`, and [D-26](DECISIONS.md) keeps Revit model files from leaving the machine. Neither
is the same as somebody having looked at what is actually committed and said so.

**An argument that separation holds and a check that it does are different things**, and this
repository has spent a lot of words on exactly that distinction — a compiler proves the API surface
agrees, a test proves the logic agrees with itself, and neither says whether a duct moved 200
millimetres or 200 feet. The same standard applies here.

### What is proposed

1. **Nothing about the README.** It is corrected, and the correction says what changed and when.
2. **Append a decision recording that the repository went public** — the date, and by whose
   instruction. D-07's own wording makes that an owner's act; only the owner can write it.
3. **Do the separation check and record it**, or record explicitly that it was judged unnecessary and
   why. Either is a closed item. Neither is what exists now.
4. **Consider a gate.** `check-docs.py` verifies that a `Golden Rule N` reference points at a rule that
   exists; nothing verifies a sentence *about a state of the world*. This is the second time that gap
   has produced a stale claim in the file a new reader opens first — the README's *"six more are
   proposed"* line survived eight days past being false for the same reason.

**Not acted on beyond the README.** 2 is an owner's decision by D-07's own terms, 3 needs somebody to
look rather than to reason, and 4 is a gate everybody runs.

---

## F39 — five Development rows may be the host's, and the tier model already says so everywhere else

**Raised by the owner on 2026-09-17**, in one sentence, after being told those five rows needed a local
model installed before they could be built:

> *"you are its self the ai and thru ai we are acessing the heron so this ai can do this work am i
> right"*

He is right, and the evidence was already in the repository. This section records it so the decision is
made deliberately rather than by whoever writes the next agent.

### The tier model says T2 means a model call. Not one built T2 makes one

| tier | built | total |
|---|---|---|
| T1 — no model call | 152 | 167 |
| T2 — one scoped call | **55** | 63 |
| T3 — agentic loop | **18** | 20 |

**No adapter existed until 2026-09-17**, so not one of those 73 agents has ever made a model call. They
are not broken and they are not lying. They do the mechanical half and hand the language half UP, and
[`brain/heron_company.py`](../brain/heron_company.py) says so in its own result:

> *"WHICH CLAUSE ANSWERS THE QUESTION. That is language, and docs/28 makes this row T2 for it. The
> clauses go to the host with the question attached; nothing here picked one."*

**That is what a T2 is in this repository**: gather, then ask the host. The tier is a statement about
where the judgement happens, not about who holds an API key.

### And D-01 already put the host in that seat

[D-01](DECISIONS.md) — accepted, read back 2026-09-06:

> **"Claude Code is the conversation layer and the agent host."** ... *"The entire agent framework,
> conversation layer, persona handling and tool orchestration come for free."*

Four rows are already delegated on exactly that basis, and `check-metadata.py` names them and their
reasons:

| row | why it is not built here |
|---|---|
| `HERON-ORC-MAIN-001` | *the host plans and sequences the work* |
| `HERON-ORC-INT-002` | *the host classifies what is being asked* |
| `HERON-ORC-PER-003` | *the host chooses the wording and the level* |
| `HERON-ORC-SUM-006` | *the host writes the reply the user reads* |

**All four are T2.** They are excluded from "left" because the host is the model.

### The five rows, one at a time, because they are not one case

| row | tier | what the evidence says |
|---|---|---|
| `DEV-PLN-002` *sequences the work* | T2 | **`ORC-MAIN-001` is already host, and its stated reason is "the host plans and sequences the work".** The same sentence. This is the clearest of the five and may be a duplicate row rather than a delegation |
| `DEV-REQ-001` *turns a request into a buildable specification* | T2 | `ORC-MAIN-001` is host for *"understands the request"*. Turning a request into a specification is that act, named again |
| `DEV-GEN-004` *writes code, only after Fragment Matcher has reported* | T3 | The precondition is mechanical and already checkable. The writing is the host's, and was the host's for every line built in this repository so far |
| `DEV-RAP-007` *Revit API knowledge for generation* | T2 | **Its own row says "merge candidate with 005/006", and both are built**: [`heron_csharp.py`](../brain/heron_csharp.py) and [`heron_dotnet.py`](../brain/heron_dotnet.py). This is a merge question, not a build one |
| `DEV-ARC-003` *decides structure and placement within Heron's architecture* | T3 | [`HERON-IMP-ARC-011`](../brain/heron_belongs.py) is built and *"places content where the architecture says it belongs"*. Overlapping, and [F28](#f28--the-import-classifies-into-five-words-the-workspace-has-never-heard-of) already records that department's vocabulary problem |

**Three look like delegation and two look like merges.** Calling all five "host" would be the same
sweeping move that [F27](#f27--five-development-rows-are-already-built-and-whose-work-they-measure-decides-by-which-file) got wrong by counting candidates instead of reading them.

### What it would change

`agent-count.py` already has a HOST column and already excludes those rows from LEFT. Adding ids to
`HOST_PROVIDED` in `check-metadata.py` is the whole mechanism; it is audited both directions and refuses
an id that is not in the registry.

**Development would go from 12 of 21 to close to complete**, without installing anything.

### What it would cost, which is the half worth arguing about

**A host-provided agent cannot run without a host.** Delegating these five means Heron cannot plan, write
a specification or generate code:

- on a schedule, with nobody in a chat
- in a batch large enough that a conversation is impractical
- for somebody using the Revit add-in with no Claude Code session open

If Heron is only ever driven through Claude Code, that costs nothing and D-01 already accepted it. If it
is ever meant to run on its own, this is the decision that says it cannot.

### What this does NOT touch

**The provider adapter is still needed, for the work a conversation genuinely cannot do.**
[`brain/heron_provider.py`](../brain/heron_provider.py) exists for two jobs and neither is on the list
above:

- **bulk.** `heron_router.INTENTS` marks `CLASSIFY`, `EXTRACT` and `SCORE` as `bulk: True`. Classifying
  five thousand elements one at a time is not a conversation
- **confidential.** The host is a cloud model. `heron_router` refuses cloud adapters for confidential
  scopes and refuses everything when no local one is registered — so that rule can only ever be kept by
  a local adapter, never by the host

### What is proposed

**Say which of the five are the host's**, in `HOST_PROVIDED` with a reason each, the way the existing
four are written. And say separately whether `RAP-007` and `ARC-003` are merges rather than
delegations, because those are a different question with a different answer.

### SETTLED 2026-09-17 — [D-80](DECISIONS.md): all five are the host's

`HOST_PROVIDED` now holds nine. **Development goes from 9 left to 4; the register from 21 to 16.**

**The four delegated before this said so nowhere in docs/28** — `HOST_PROVIDED` knew and the register
did not. All nine now carry the same sentence, because marking only the new five would have left a
reader seeing four ordinary unbuilt rows.

**The merge question above is deliberately NOT settled.** `RAP-007`, `ARC-003` and `PLN-002` may be
duplicate rows rather than delegations, and the register says so on each. Merging a row is a different
act from delegating one, and doing both at once is how a register loses track of which happened.

**The cost is recorded in D-80 rather than buried**: Heron can no longer plan, specify or generate code
without a chat open — no schedule, no add-in colleague, no confidential scope. The owner considered
each and accepted all three.

---

## F40 — Workforce Planning scores a proposal against the register's BUILD NOTES, not only its job

**Found by breaking it, on 2026-09-17**, while recording [D-80](DECISIONS.md). CI went red on
`test_hr.py`, and the cause was one word in a sentence about bookkeeping.

`HERON-AHR-WFP-015` is the guard against agent explosion. `assess()` scores a proposed agent against
each register row:

    overlap(purpose, "%s %s" % (row["name"], does))

`does` is **the whole `Does` column**, and this repository has spent the day appending build notes to
that column — *"Built 2026-09-16 - tools/check-compile.py..."*, *"FOLDED into check-docs.py"*,
*"Provided by the host"*. Those notes are bookkeeping, not the job.

### What happened

D-80 added one sentence to nine rows, ending *"`agent-count.py` **counts** it under HOST, never under
LEFT"*. `HERON-ORC-SUM-006`'s job description already contains the word **ducts**, in an example of a
reply it might write.

So the proposal `Duct Counter` / *"counts ducts in a view"* — a fixture that exists precisely because
it is a **fragment** and must be turned down — scored **0.67** against the User Result Agent and came
back `ALREADY_AN_AGENT`.

**Two words. One of them was in a sentence about a column in a report.**

### Why it matters more than the typo

The word was changed and the suite is green again. **The fragility is not.** Every build note appended
to a row from here on is scored as though it described the job, and this register now carries dozens of
them. The guard gets noisier with every agent that is built, which is exactly backwards.

**And it fails in the dangerous direction.** A false `ALREADY_AN_AGENT` does not create a duplicate
agent — it REFUSES A REAL ONE, with a confident sentence naming an agent that does nothing of the sort.
A guard that cries wolf is [A18](NEEDS-CHECKING.md)'s lesson, and this one now has a growing supply of
wolves.

### What is proposed

**Score against the job, not the bookkeeping.** The build notes in this register are consistently
introduced by a bolded marker — `**Built`, `**FOLDED`, `**DEFERRED`, `**Claimed`, `**Provided by the
host** — so the job is the text before the first one. That is a small change in one place, and
`tests/test_workforce.py` can assert it with the very fixture that failed here.

**Not acted on.** It changes what a guard sees, and the guard's whole value is that its findings are
real. That deserves its own decision rather than riding in on a delegation.

---

## F41 — the scaffolder writes a module name the suite then rejects

Found on 2026-09-17, building [`HERON-DEV-RGR-014`](../brain/heron_buildmatrix.py). It cost one full
198-suite sweep to discover, which is the argument for fixing it.

`tools/new-agent.py` derives the module name from the register row's name: *Regression Test Agent*
became `brain/heron_regression_test.py`. That file is refused by
[`tests/test_references.py`](../tests/test_references.py) claim 5 — **no module name in `brain/` may be
a prefix of another** — because `heron_regression.py` (`HERON-FRG-REG-006`, the fragment one) has been
there since long before.

### Why the rule is not cosmetic

`HERON-DOC-REF-006` renames and searches over module names, and the rule is what lets it tell a whole
name from the start of a longer one. A prefix pair makes every rename of the shorter name ambiguous —
silently, and in the direction that edits something nobody asked for.

### Why the scaffolder cannot be left as it is

It already **refuses three things on purpose**: an id not in the register, an agent something already
claims, and a file that is already there. Its own docstring's argument for existing is that doing this
by hand 146 times is 146 chances to get a detail wrong — and this is one of the details, checked
nowhere until a suite that takes ten minutes runs.

The failure is also quiet in the worst way: the scaffold succeeds, the agent gets written, the tests
pass individually, and the collision only appears in a full sweep of everything else.

### What is proposed

**A fourth refusal, in the same place as the other three.** Before writing, compare the derived module
name against every existing name in the target part, both directions, and refuse with the colliding
name and a suggestion — the same shape as its existing refusals. Three lines, and
`tests/test_new_agent.py` can assert it against the real folder rather than a fixture.

**Not acted on here.** It is a change to the tool that scaffolds every remaining agent, and it belongs
in a change of its own rather than riding in on the one that found it. The name chosen instead —
`heron_buildmatrix.py`, for what the file compares — is recorded in that module's own docstring so the
next reader does not wonder why the file is not named after its agent.
