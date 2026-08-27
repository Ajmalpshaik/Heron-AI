# Heron AI — Documentation Index

> **Status:** Specification and planning only. No implementation until the owner says to start —
> see [DECISIONS.md](DECISIONS.md) D-00.

## Decided so far

| | Decision |
|---|---|
| **Host** | Claude Code plugin — skills, subagents, MCP server, Revit add-in *(D-01)* |
| **Revit support** | 2020 → latest, and every future release *(D-05)* |
| **Languages** | C# for everything touching Revit · Python for the brain *(D-06)* |
| **Transport** | Named pipes — add-in is the server, local-only by construction *(D-02)* |
| **Revit threading** | One `ExternalEvent`, one request queue, one handler *(D-09)* |
| **MCP tools** | Thick and specific, one per fragment, each with its own risk level *(D-03)* |
| **Generated code** | Hybrid — scripting sandbox while testing, compiled C# for production *(D-04)* |
| **Licence** | Apache 2.0 *(D-08)* |
| **Distribution** | Free and open source on public GitHub; app store later, also free *(D-07)* |
| **Repo visibility** | Private until Phase 0 code exists; licence and safety files already done *(D-10)* |
| **Building on** | `AJ-AI-Brain` (brain) and `AJ-Connect` (Revit connector), upgraded to this architecture |

**12 of 32 questions answered — none of the remaining 20 block Phase 0.**
It starts on the owner's go-ahead.

**Specification is complete in three parts.** Part 1 is the platform and its organisation; Part 2 is
the Agent Operating System; Part 3 is the consolidated baseline and is **authoritative on the Golden
Rules** — it replaced the original ten with fifteen, and every cross-reference in this repository was
remapped accordingly ([D-12](DECISIONS.md)). See [PROPOSALS Part 0a](PROPOSALS.md).

---

## Start here

| If you want to… | Read |
|---|---|
| Understand what Heron AI is | [01 — Vision & Principles](01-vision-and-principles.md) |
| See the original specification, unaltered | [00 — Master Specification](00-master-specification.md) |
| Know what is missing or risky | [PROPOSALS.md](PROPOSALS.md) |
| Know what still needs deciding | [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) |
| Know what gets built first | [ROADMAP.md](ROADMAP.md) |
| Know the non-negotiables | [14 — Golden Rules](14-golden-rules.md) |

---

## Core documents

| # | Document | Covers |
|---|---|---|
| 00 | [Master Specification — Part 1](00-master-specification.md) | The platform and its organisation, §1–§76. Source of truth. |
| 00b | [Master Specification — Part 2](00b-master-specification-agent-os.md) | The Agent Operating System and self-evolution, §1–§84. Source of truth. |
| 00c | [Master Handover Baseline — Part 3](00c-master-handover-baseline.md) | The consolidated baseline, §1–§80. **Authoritative on the Golden Rules.** |
| 01 | [Vision & Principles](01-vision-and-principles.md) | What Heron is, personas, the defining principle |
| 02 | [Architecture Overview](02-architecture-overview.md) | Layers, four parts, orchestration, **agent tiers** |
| 03 | [Heron Revit](03-heron-revit.md) | Add-in, Revit agents, **threading, transactions, versions** |
| 04 | [Heron MCP](04-heron-mcp.md) | The bridge, tool design, permission boundary |
| 05 | [Heron Brain](05-heron-brain.md) | RAG, retrieval strategy, vector store, embeddings |
| 06 | [Heron Platform](06-heron-platform.md) | Workspace, registry, agent lifecycle, packages, GitHub |
| 07 | [Installation & Update](07-installation-and-update.md) | Install flow, build strategy, updates, self-healing |
| 08 | [Agent Catalogue](08-agent-catalog.md) | All ~150 agents by department, with tiers |
| 09 | [Skills & Fragments](09-skills-and-fragments.md) | The knowledge units, lifecycle, gates, code execution |
| 10 | [Memory & Knowledge](10-memory-and-knowledge.md) | Scopes, isolation, import, community |
| 11 | [Orchestration & Workflows](11-orchestration-and-workflows.md) | Reference workflows, failure handling, performance |
| 12 | [Security & Permissions](12-security-and-permissions.md) | Permission levels, gate location, confidentiality, audit |
| 13 | [Testing & Quality](13-testing-and-quality.md) | Eight test levels, testing against real Revit, regression |
| 14 | [Golden Rules](14-golden-rules.md) | The constitution — **15 official rules** plus 4 proposed |
| 15 | [Glossary](15-glossary.md) | Terms, Revit concepts, status vocabularies |
| 16 | [Version Support Strategy](16-version-support-strategy.md) | **Revit 2020 → latest** — the two API breaks, multi-targeting, adapters, test matrix |
| 17 | [Open Source & Distribution](17-open-source-and-distribution.md) | Licence, public/private separation, contribution, disclaimer, channels |

### From Master Specification Part 2 — the Agent Operating System

| # | Document | Covers |
|---|---|---|
| 18 | [Agent Operating System](18-agent-operating-system.md) | **Capability Registry**, dynamic discovery, Agent HR, **Shadow Mode**, trust scores, events |
| 19 | [Context & Cost](19-context-and-cost.md) | Context Manager, compression, model routing, fallback, caching, observability |
| 20 | [Knowledge Trust & Conflict](20-knowledge-trust-and-conflict.md) | Knowledge hierarchy, trust levels, **conflict resolution**, fragment branching, quality scores |
| 21 | [Resilience & Operations](21-resilience-and-operations.md) | **Dependency graph**, safety tiers, **emergency stop**, health, self-healing, backup, migration |
| 22 | [Users, Modes & Extensibility](22-users-modes-and-extensibility.md) | Conversation intelligence, User/Developer/Admin modes, multi-user, platform adapters, plugins, SDK |

## Working documents

| Document | Purpose |
|---|---|
| [PROPOSALS.md](PROPOSALS.md) | Gaps found in review, feature ideas, strategic questions |
| [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) | 26 questions, prioritised, with answer slots |
| [ROADMAP.md](ROADMAP.md) | Phase 0 → Phase 7, and what is deliberately deferred |
| [DECISIONS.md](DECISIONS.md) | Append-only log of decisions actually made |

---

## Reading conventions

- **[NOTE]** blocks are engineering commentary added during review — not part of the original specification.
- The [Master Specification](00-master-specification.md) is never edited to "fix" it. Changes are recorded as decisions.
- 🔴 blocking · 🟠 important · 🟡 worth doing · 🔵 idea

---

## The one-line version

> The user focuses on BIM. Heron AI focuses on everything behind the BIM work.
