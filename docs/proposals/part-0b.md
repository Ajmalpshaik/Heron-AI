# Proposals — Part 0b

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part 0b — What the Additional Requirements (Part 4) changed

Part 4 is the most substantive of the four after Part 1. It adds real architecture, closes four gaps the
review had raised, resolves one open tension — and creates one problem that had to be solved before
anything can be built.

### ✅ Gaps closed by Part 4

| Was | Closed by | Detail |
|---|---|---|
| **A7** — no preview before modification | **§11 Simulation / Dry Run** | *"I found 126 ducts. This operation will modify 126 elements."* Independently specified, and it is exactly what proposed [Golden Rule 17](../14-golden-rules.md) asks for. Gap closed. |
| **A6** — undo and transactions never mentioned | **§12 Transaction Safety Agent** | A dedicated agent for transaction handling, closure, rollback, failure handling and document state, *separate from general Revit API logic*. The vehicle for Golden Rule 16 now exists. Half closed — the **one-Ctrl+Z guarantee** still needs stating. |
| **A10** — permission gate placement unspecified | **§29 Security Boundary** | *"AI → Permission Layer → Tool Validation → MCP → Revit. The AI requests an operation; the platform decides whether that operation is allowed."* Confirms the position taken in [12 §3](../12-security-and-permissions.md) exactly. |
| *(review proposal B9)* — offline mode | **§31 Offline / Degraded Mode** | Now an official requirement. |
| *(review recommendation)* — golden-file regression testing | **§28 Golden Test Library** | The mechanism proposed in [13 §4](../13-testing-and-quality.md), now official. |

### ✅ Open tension resolved

**[Q-30](../OPEN-QUESTIONS.md) — Model Router vs Claude Code as host — is answered by §24.**

The **AI Model Abstraction Layer** (`Heron AI Interface → Model Router → Provider Adapter → Model`)
separates two things that were conflated: Heron always declares *intent* ("this needs strong
reasoning"); *resolution* to a specific model is pluggable and belongs to the host when hosted.

It also makes §25's local/cloud routing a configuration choice rather than an architectural one, which
matters directly for confidentiality ([Q-12](../OPEN-QUESTIONS.md)): a project marked confidential selects
a local provider adapter and nothing above that layer needs to know. → [23 §8](../23-heron-kernel.md)

### 🆕 The best new ideas in Part 4

| # | Idea | Why it matters |
|---|---|---|
| **P14** | **Heron Kernel** (§1) | The largest structural addition in any document. Without it, N modules depend on N modules and the dependency graph grows quadratically. With it, [Golden Rule 15](../14-golden-rules.md) becomes achievable rather than aspirational. The discipline that keeps it from becoming a god object: **the Kernel is plumbing, never intelligence** — it must compile with no Revit knowledge in it at all. → [23 §1](../23-heron-kernel.md) |
| **P15** | **Evidence System** (§6) | *"This fragment was selected because it supports Revit 2020–2027 and has 98 successful executions."* The single best idea in Part 4. It is what turns Heron from a black box into something a professional can sign off on — and it is nearly free, because every field already exists in the registries. → [23 §6](../23-heron-kernel.md) |
| **P16** | **Workflow Engine separate from Orchestrator** (§3) | The Orchestrator decides *what* should happen; the Workflow Engine ensures *it happens correctly*. Retry, timeout and rollback logic written once instead of reinvented in every pipeline. |
| **P17** | **Checkpoints and resume** (§20, §21) | The new-tool pipeline is 18 stages, several of them expensive T3 loops. Failing at Build and discarding twelve completed stages costs real money. Makes *"Continue."* a first-class command. → [23 §4](../23-heron-kernel.md) |
| **P18** | **Prompt / Instruction Registry** (§26) | Underrated and cheap. Scattered prompts are the most common reason an AI system becomes unmaintainable — behaviour changes and nobody can find which string caused it. |
| **P19** | **User Intent Memory** (§14) | *"What the user normally likes"* vs *"what the user asked this time."* Temporary instructions must not silently become permanent preferences. A subtle failure mode, correctly identified. |
| **P20** | **BIM QA as a third QA type** (§18) | Code QA ≠ Revit QA ≠ BIM QA. Naming, parameters, categories, families, levels, worksets, modelling rules, MEP connectivity. This is a major capability in its own right, not a test stage. |
| **P21** | **Revit Context Agent** (§13) | Auto-detect document, version, view, selection, links, worksets, phase, design option. *"Removes unnecessary questions"* — which is Golden Rule 1 applied to conversation. |
| **P22** | **Safe Mode** (§35) + **Feature Flags** (§36) | Disable recently installed components, return to last known-good. Pairs naturally with Shadow Mode: a flag set to `TEST` is how a shadow component gets exercised without its output being used. |
| **P23** | **Secret Management** (§30) + **Supply-chain security** (§43) | Both become urgent the moment the repo is public ([D-07](../DECISIONS.md)) and Heron can install external components. |

### 🔴 The problem Part 4 created — and its resolution

**Six overlapping status vocabularies now exist** across the four documents, all describing how much
Heron trusts something. Part 4 added two of them (§38 trust levels, §47 knowledge levels) on top of the
four already present.

This is not a tidiness complaint. Retrieval ranks by trust, promotion gates are defined per-vocabulary,
and [Golden Rule 6](../14-golden-rules.md) — *experimental must stay separate from production* — is
unenforceable when "experimental" means four different things.

**Resolution:** they are not six versions of one idea; they are **two ideas repeatedly collapsed into one
linear scale** — *how far has this been proven* (advances over time) and *where did this come from*
(fixed at creation). `OFFICIAL` was never a stage past `PROVEN`; it is a source.

Separated into two orthogonal axes, all six collapse cleanly and nothing is lost — in fact more becomes
expressible, since an object can now be `PROVEN` **and** `COMMUNITY`, which no single scale could say.
Part 4 §47's four levels survive as a derived band used for ranking and for talking to users.

Full proposal: **[24 — The Unified Trust Model](../24-trust-model.md)**. Needs confirmation ([Q-34](../OPEN-QUESTIONS.md)).

### 🆕 New artefact: the Heron Constitution

§46 requests `HERON_CONSTITUTION.md` — rules agents must never violate, injected into every agent.

**[Written and ACCEPTED 2026-08-28: [HERON_CONSTITUTION.md](../../HERON_CONSTITUTION.md) — all 30 Articles, after Ajmal asked for every one to be read out rather than tapping yes ([Q-35](../OPEN-QUESTIONS.md), closed).]**

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
> — [field notes](../00e-field-notes-proven-bridge.md)

Four specification documents, several hundred sections, and the single hardest constraint in the
platform appears in none of them — but it is written plainly in a note describing software that already
runs. That is the difference between designing a system and operating one.

A1 is no longer a review finding. It is settled fact, and [D-09](../DECISIONS.md) is validated.

### 🆕 P24 — the stale read *(field-proven, never specified)*

> *"The real danger is not the freeze, it is the stale read: the AI reads the model, you change
> something, and a later step acts on the old picture."*

Not in any specification, and it generalises further than the note claims — the model can also change
because another user synced, a link reloaded, or Heron's own earlier step changed it. Becomes proposed
[Golden Rule 21](../14-golden-rules.md), and it forces a rule that matters: **an accepted preview must be
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
[Golden Rule 20](../14-golden-rules.md), and moves into Phase 0 scope — cheap now, a silent hazard if
deferred. → [25 §4](../25-multi-session-and-binding.md)

### 🔴 Still open after all four documents

**Unchanged.** Four documents, and the single hardest technical constraint in the platform is still
absent from all of them:

| # | Gap | Where handled |
|---|---|---|
| **A1** | **Revit API threading** — an MCP server cannot call the Revit API at all; every operation must marshal through `ExternalEvent` onto Revit's main thread | [03 §4](../03-heron-revit.md), settled in [D-09](../DECISIONS.md) |
| **A9** | **Data egress** — §25 and §30 cover local models and secrets, but not *what project content reaches an AI provider* | [12 §4](../12-security-and-permissions.md), [Q-12](../OPEN-QUESTIONS.md) |
| **A11** | **How a Revit test actually executes** — §27 and §28 specify what to test and that a golden library exists, not how a test runs inside a GUI application that cannot run headless | [13 §3](../13-testing-and-quality.md), [Q-14](../OPEN-QUESTIONS.md) |

Proposed Golden Rules 16 and 19 also remain unstated in any document — the **one-Ctrl+Z guarantee** and
**no permission escalation from untrusted text**. §12 and §29 build the machinery for both; neither
states the rule.

### ✅ And one strong validation

Part 4 closes with:

> *"Don't start by building 100+ agents. Build the Kernel, Registry, Orchestrator, Workflow Engine, RAG,
> Fragment/Skill system, MCP/Revit layer, and QA foundation first."*

This is the same conclusion as [ROADMAP.md](../ROADMAP.md) reached from the other direction, and it is now
an explicit instruction rather than a review recommendation. The roadmap has been updated to name these
components directly.

---
