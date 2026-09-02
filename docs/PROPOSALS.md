# Proposals — Gaps, Additions & Ideas

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
