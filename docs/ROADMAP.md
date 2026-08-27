# Roadmap

> A phased plan for reaching the architecture in the [Master Specification](00-master-specification.md).
> The spec is the destination. This is the route.
>
> **Nothing here is committed until the Tier 1 questions in [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are answered.**

---

## The governing idea

The specification describes roughly 150 agents across 15 departments. Built in the order written, the first
useful moment arrives after a very long time — and by then most of it will have been designed against
assumptions that turned out to be wrong.

Built as **one thin vertical slice first**, the first useful moment arrives early and every subsequent
decision is made against facts.

> **Phase 0 exists to answer questions Q-1 through Q-7 with working code rather than opinion.**

---

## Phase 0 — Prove the bridge *(the only phase that matters right now)*

**Goal:** one sentence typed by a human changes the selection in a running Revit.

```text
"Select all ducts."
  -> intent
  -> skill lookup
  -> MCP tool call
  -> named pipe
  -> add-in listener
  -> ExternalEvent
  -> Revit main thread
  -> FilteredElementCollector (OST_DuctCurves)
  -> selection set
  -> result
  -> audit log entry
```

**Scope — deliberately minimal:**

- **One** Revit version to start — with the multi-target structure and adapter layer already in place,
  so fanning out to 2020 → latest is later work, not a rewrite *([16 §8](16-version-support-strategy.md))*
- One transport *(Q-2)*
- Three MCP tools: `revit_health`, `revit_select_by_category`, `revit_get_selection`
- No RAG, no vector DB, no fragments, no learning, no installer, no code generation
- Hard-coded skill mapping — no matching intelligence at all

**Already settled, so this phase implements rather than explores:**
Claude Code as host ([D-01](DECISIONS.md)) · C# add-in + Python MCP server ([D-06](DECISIONS.md)) ·
Revit 2020 → latest as the eventual target ([D-05](DECISIONS.md))

**Structure to put in place from the first commit** (cheap now, ruinous to retrofit):
multi-targeting (`net48` + `net8.0-windows`) · adapter layer for version differences ·
`UniqueId` for all element identity · public-code / private-data separation ([17 §2](17-open-source-and-distribution.md))

**Definition of done:** it works twice in a row, from a cold Revit start, and the audit log shows what happened.

**What this settles:** Q-2, Q-4, Q-5 — and it produces the skeleton every later phase builds on.

**Phase 0.5 — add the second runtime.** One `net48` version and one `net8` version, both working.
This is where the .NET break stops being theoretical, and it is much cheaper to face here than after
six phases of code assume one runtime.

---

## Phase 1 — Make it modify, safely

**Goal:** "Move them 200 mm up" — with a preview, one undo, and a clean rollback on failure.

- `TransactionGroup` wrapper — Golden Rule 16
- Dry run / preview before any `MODIFY`
- Permission gate enforced **in the add-in**
- **Emergency Stop** in the Revit ribbon — works even when the agent side is stuck
  *([21 §4](21-resilience-and-operations.md))*
- Failure handling that does not retry blindly
- The audit log records elements touched, keyed by **Workflow ID** *([21 §13](21-resilience-and-operations.md))*

**Definition of done:** a wrong instruction can be reversed with one Ctrl+Z, and a failed operation leaves the model untouched.

**Why this is second:** the moment Heron can write to a model, trust becomes the product. Everything after this assumes it.

---

## Phase 2 — Fragments, capabilities and knowledge

**Goal:** stop hard-coding skills. Start accumulating.

- **Capability Registry** — separate from the agent registry, with cost tier and risk level per
  capability *([18 §2](18-agent-operating-system.md) — the highest-leverage single component in Part 2)*
- Fragment storage format — folder + metadata + tests *(see [09 §4](09-skills-and-fragments.md))*
- Knowledge identity — IDs, not filenames
- SQLite + FTS + vectors, one file per scope *(Q-10, Q-11)*
- Hybrid retrieval with exact-match short circuit + **utterance cache**
- Scope separation enforced physically — Golden Rule 5
- **Dependency graph** — skills → fragments → API → runtime *([21 §1](21-resilience-and-operations.md))*
- **Import the existing AJ-Tools / PyRevit-Tools libraries** *(Q-16)*, with duplicate detection
  *([20 §5](20-knowledge-trust-and-conflict.md))*

**Definition of done:** ten real skills work, none of them hard-coded, importing an existing repo
produces a reviewable manifest, and the Orchestrator resolves requests through capabilities rather
than agent names.

**Why this is the highest-value phase after safety:** it is what turns Heron from a demo into something
that gets better every week. The Capability Registry is what stops the Orchestrator accumulating domain
knowledge — build it here, not later, because retrofitting it means rewriting every call site.

---

## Phase 3 — The BIM surface

**Goal:** enough real Revit capability to be useful daily.

- Element, Parameter, Category, View, Selection, Transaction, Warning agents
- Worksharing handling — ownership as a normal outcome, not an error
- Progress, cancellation and job IDs for long operations
- "What did Heron change?" report *(PROPOSALS B2)*
- Model health baseline *(PROPOSALS B8)*

**Definition of done:** a normal day's MEP modelling work can be driven through Heron.

---

## Phase 4 — Learning and trust

**Goal:** the system improves from use, and knows how much to trust itself.

- Fragment lifecycle with real promotion gates *(Q-9)*
- **Knowledge trust levels and conflict resolution** *([20 §3–4](20-knowledge-trust-and-conflict.md))*
- **Agent / fragment / skill trust scores**, per capability **per Revit version** *([18 §5](18-agent-operating-system.md))*
- Success / failure tracking that counts user corrections as failures
- **Capability Gap report** — *"here are the ten things people asked for that I could not do"*
- Personal pattern learning, with confirmation before saving

**[Note]** The Capability Gap report should arrive **much earlier than this phase** if it can — it is a read-only report over the audit log and it tells you what to build next from real usage. Build it as soon as the audit log exists.

---

## Phase 5 — Code generation

**Goal:** Heron creates new capability, safely.

- The hybrid execution model from [D-04](DECISIONS.md); scripting runtime chosen *(Q-7a)*
- Sandbox execution — Golden Rule 18
- **Shadow Mode** for new agents and fragments *([18 §4](18-agent-operating-system.md), Q-29)*
- Code review by a separate agent — Golden Rule 7
- **Code QA and Revit QA as separate gates** *([21 §11](21-resilience-and-operations.md))*
- Regression testing across versions with golden files, scoped by the dependency graph
- Human approval before PRODUCTION

**Why this is late despite being the most exciting part:** it is the highest-risk capability in the platform, and it is only safe once the lifecycle, the sandbox, the testing matrix and the audit log all exist.

---

## Phase 6 — Platform

- Installer with prebuilt binaries *(Q-6, [07 §3](07-installation-and-update.md))*
- Update system with rollback and data-class protection
- Multi-version support and the regression matrix *(Q-14)*
- Package manager

---

## Phase 7 — Scale

- Standards department with citation enforcement
- Team / company shared knowledge *(PROPOSALS B7)*
- Agent creation, still human-approved
- Community, marketplace — only with Q-21, Q-24 and Q-25 answered

---

## What is deliberately **not** in the plan yet

| Deferred | Until |
|---|---|
| Community marketplace | commercial model and liability are settled |
| Autonomous agent creation | the human-approval boundary is proven in practice |
| Non-Revit platforms (AutoCAD, IFC, Rhino) | Revit works properly |
| Multi-user server infrastructure | there is more than one user |
| The full 150-agent catalogue | real usage says which ones are actually needed |

The catalogue in [08](08-agent-catalog.md) is the destination, reached by building what usage demands —
not by working down a list.

---

## Sequencing principle

> Every phase must end with something that **works and is used**, not something that is merely built.

If a phase ends with infrastructure nobody has exercised, the phase was wrong.
