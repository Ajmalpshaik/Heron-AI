# 08 — Agent Catalogue

> Every agent named in the [Master Specification](00-master-specification.md), consolidated by department.
> Tier column is an engineering recommendation (see [02 §6](02-architecture-overview.md)), not part of the original spec:
> **T1** = deterministic service, no model call · **T2** = single scoped LLM call · **T3** = full agentic loop.
>
> This is a **target organisation chart**, not a v1 backlog. See [ROADMAP.md](ROADMAP.md) for what actually gets built first.
>
> **For the full list — every agent, what it does, its tier, risk level and build step — see
> [28 — The Complete Agent Registry](28-agent-registry.md).** This document explains the *shape*;
> that one is the reference table.

---

## Summary

| Department | Spec § | Agents |
|---|---|---|
| Orchestration & Communication | §45, §50, §51 | 5 |
| Installation | §9 | 10 |
| Revit | §12, §13 | 23 |
| MCP | §14 | 11 |
| Fragment lifecycle | §19–23, §25 | 6 |
| RAG | §26 | 15 |
| Knowledge Import & Migration | §29 | 14 |
| Naming & Taxonomy | §31 | 7 |
| Folder Architecture | §32 | 12 |
| GitHub | §36 | 10 |
| Agent lifecycle / self-growth | §37, §39, §70 | 8 |
| Development | §41 | 18 |
| BIM Standards | §46 | 9 |
| Background | §49 | 1 |
| Documentation | §63 | 8 |
| **Total (before de-duplication)** | | **~157** |

Several names appear in more than one department (Duplicate Detection, Migration, Validation, Agent Creator, Agent Trainer, Backup). After de-duplication the real count is roughly **150 distinct agents**.

---

## Which agents belong to which build step

> Added 2026-08-27. The catalogue answers *what exists*; this answers *what to build now*.
> Build order: [27](27-build-order.md).

**The whole of Phase 0 and Phase 1 needs 47 of the 250 agents** — under a fifth. The other 203 are not
deferred out of caution; they have nothing to act on until the layers below them exist.

The table below names the principal agents per step. The authoritative per-agent assignment is the
**Step** column in [28 — The Complete Agent Registry](28-agent-registry.md).

| Step | Agents built | Tier |
|---|---|---|
| **1 — prove the pipe** | MCP Server, MCP Connection, Revit Connection, Revit Version | all T1 |
| **2 — thread hop** | Revit Application, Revit Document, Revit Transaction *(skeleton only)* | all T1 |
| **3 — MCP server** | MCP Tool Registry, MCP Health, Revit Plugin Health | all T1 |
| **4 — first BIM answer** | Revit Selection, Revit Category, Revit Element · **Intent**, **Communication/Persona** | T1 + two T2 |
| **5 — multi-Revit** | MCP Recovery, MCP Version, MCP Compatibility | all T1 |
| **6 — first write** | **Transaction Safety**, Revit Context, Revit Warning, MCP Security, Failure Analysis | T1 + one T2 |

**Roughly 20 agents, and 17 of them are T1** — plain deterministic modules with no model call at all.
Only Intent, Persona and Failure Analysis reason.

### Why the agents cannot be built first

**[NOTE]** A natural question: *build the agent layer first, then assign agents to each phase.* The
definitions should indeed exist first — and they do. But **building** them first does not work, for
three reasons the mapping above makes concrete:

1. **Most agents are not a separate build.** Around 95 of the 150 are T1 services
   ([02 §6](02-architecture-overview.md)) — they *are* the implementation of steps 1–6, not a layer
   added afterwards. Building the Revit Selection Agent **is** building step 4. There is no separate
   "agent work" to schedule.
2. **An agent with nothing beneath it is a text file.** A Revit Selection Agent calls an MCP tool, which
   calls the bridge, which marshals onto the Revit thread. Until steps 1–2 exist there is nothing for it
   to call, and no way to test whether its contract is right.
3. **Contracts written before first contact are wrong.** [The field notes](00e-field-notes-proven-bridge.md)
   are the proof: the connect-time snapshot, the stale document name, the active-document hazard — none
   were predictable from a specification. They were found by running the software. An agent contract
   written before step 2 would encode the same wrong assumptions, and then 150 of them would need
   revising instead of 4.

This is also the explicit instruction closing [Part 4](00d-additional-requirements.md):

> *"Don't start by building 100+ agents. Build the Kernel, Registry, Orchestrator, Workflow Engine, RAG,
> Fragment/Skill system, MCP/Revit layer, and QA foundation first. Then Heron can create and add
> specialized agents safely as the platform grows."*

**What is right in the instinct:** agents should be **defined** and **assigned** before implementation,
so work is scoped rather than improvised. That is exactly what this catalogue and the table above are
for. The remaining ~130 arrive when the Capability Gap report ([06 §6](06-heron-platform.md)) shows real
demand — built from usage, not from a list.

---

## Orchestration & Communication

| Agent | Tier | Responsibility |
|---|---|---|
| Orchestrator | T2 | Central coordinator — understand, select, sequence, validate, return |
| Intent Agent | T2 | Classify what the user is asking for |
| Communication / Persona Agent | T2 | Detect persona and technical level; choose response style |
| Failure Analysis Agent | T2 | Determine why something failed |
| Fix Agent (selected per failure) | T3 | Apply a targeted repair |

## Installation (§9)

Installation Orchestrator (T1) · Environment Detection (T1) · Revit Installation (T1) · Revit Deployment (T1) · MCP Installation (T1) · Dependency (T1) · Configuration (T1) · Brain Initialization (T1) · RAG Initialization (T1) · Health Check (T1)

**All T1.** Installation is deterministic. Nothing here should ever cost a model call.

## Revit (§12, §13)

Connection · Document · Application · UI · Ribbon · Selection · Element · Category · Parameter · Family · View · Workset · Link · Transaction · Warning · Performance · Export · Import · API · Version · Compatibility · Deployment · **Plugin Health**

All **T1** except **Revit API Agent (T2)**, which reasons about correct API usage for novel operations.

## MCP (§14)

Connection · Server · Tool Registry · Configuration · Health · Authentication · Version · Compatibility · Recovery · Logging · Security

**All T1.** Transport must never require a model call.

## Fragment lifecycle (§19–23, §25)

| Agent | Tier | Responsibility |
|---|---|---|
| Fragment Validation Agent | T2 | Correctness, API, versions, deps, metadata, duplication, reusability |
| Fragment Performance Agent | T1 | Success, failure, timing, corrections, reuse, error frequency |
| Fragment Split Agent | T3 | Decompose a compound fragment into reusable units |
| Fragment Merge Agent | T2 | Detect and propose merging near-identical fragments |
| Fragment Evolution Agent | T3 | Keep / improve / adapt / version-branch / split / merge / deprecate |
| Regression Testing Agent | T1 | Build and test every supported version; reject unsafe changes |

## RAG (§26)

Librarian · Knowledge Discovery · Retriever · Fragment Matcher · Skill Matcher · Ranking · Context Builder · Embedding · Vector Search · Index Manager · Re-index · Duplicate Detection · Knowledge Validation · Citation/Source · Knowledge Evolution

Mostly **T1**; Fragment Matcher, Skill Matcher and Knowledge Validation are **T2**; Knowledge Evolution is **T3**.

## Knowledge Import & Migration (§29)

Import · File Discovery · Content Classification · Fragment Extraction · Skill Extraction · Metadata Extraction · Duplicate Detection · Compatibility · Migration · Rename · Architecture Matching · Validation · Indexing · Approval

Extraction and classification are **T2/T3** — this is genuine judgement work over unstructured input. Discovery, indexing and rename are **T1**.

## Naming & Taxonomy (§31)

Naming · Naming Validation · Auto Rename · Taxonomy · Keyword · Metadata · Reference Update

**T1** except Naming and Taxonomy (**T2**).

## Folder Architecture (§32)

Workspace Architect · Folder Creation · Folder Validation · Folder Repair · File Placement · Template · Path Manager · Migration · Cleanup · Backup · Restore · File Registry

**All T1.** Cleanup, Repair and Migration are `MODIFY`-gated and dry-run by default.

## GitHub (§36)

GitHub · Repository · Branch · Commit · Pull Request · Issue · Release · Version · Change Detection · Community Contribution

**All T1** except Commit (T2, for message generation) and Community Contribution (T2).
Push / PR / Release are `PUBLISH`-level and require explicit per-action confirmation.

## Agent lifecycle & self-growth (§37, §39, §70)

| Agent | Tier | Responsibility |
|---|---|---|
| Agent Creator | T3 | Decide whether a new agent is needed; design and generate it |
| Agent Evaluator | T2 | Assess an agent's real performance |
| Agent Trainer | T2 | Supply standards, rules, examples before activation |
| Agent Registry | T1 | System of record for every agent |
| Agent Optimizer | T2 | Improve an existing agent |
| Agent Retirement Agent | T1 | Retire with history preserved and rollback possible |
| Architecture Monitor | T2 | Detect architectural drift |
| Capability Gap Agent | T1 | *"X is needed repeatedly and no agent covers it."* |

**Capability Gap Agent is the highest value / lowest cost agent in the catalogue** — it is a report over the audit log and should be built early. See [06 §6](06-heron-platform.md).

## Development (§41)

Requirement · Planning · Architecture · Code Generation · C# · .NET · Revit API · Code Review · Security Review · Build · Unit Test · Integration Test · Revit Test · Regression Test · Performance · QA · Documentation · Release

Build and the test runners are **T1**. Requirement, Architecture, Code Generation, Code Review, Security Review and QA are **T2/T3**.

**[NOTE]** `C# Agent`, `.NET Agent` and `Revit API Agent` overlap heavily and are strong candidates for merging into a single **Code Domain Agent** with retrieval over three knowledge sets. Three near-identical agents is exactly the duplication the Fragment Merge Agent exists to prevent — the same discipline should apply to agents themselves.

## BIM Standards (§46)

BIM Standard · Company Standard · ISO Standards · Naming Standard · Modeling Standard · QA/QC Standard · LOD · Documentation Standard · Project Standard

**T2**, all of them citation-bound: no indexed source, no claim (§47).

## Background (§49)

Background Scheduler Agent — **T1**. Priorities P0 (user task) through P5 (cleanup). User work always wins.

## Documentation (§63)

API Docs · Agent Docs · Skill Docs · Fragment Docs · Release Notes · Architecture Docs · README · Change Log

**T1** where generated from registries and metadata; **T2** only for prose.

---

## **[NOTE]** How to read this catalogue

Roughly **95 of ~150 agents are T1** — ordinary deterministic modules. Many are a single class with a handful of methods.

The realistic count of things that are "AI agents" in the expensive sense is around **25 T2** and **12 T3**. That is a tractable number, and it is the number that determines running cost and latency.

Framing the catalogue this way turns an intimidating 150-agent architecture into: a normal application with about 95 services, 25 narrow model calls, and 12 genuine agentic workflows.
