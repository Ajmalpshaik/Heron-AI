# Heron AI — Documentation Index

> **Status:** Specification and planning only. No implementation yet — see [DECISIONS.md](DECISIONS.md) D-00.

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
| 00 | [Master Specification](00-master-specification.md) | The complete original spec, §1–§76. Source of truth. |
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
| 14 | [Golden Rules](14-golden-rules.md) | The constitution — rules 1–10 plus 5 proposed |
| 15 | [Glossary](15-glossary.md) | Terms, Revit concepts, status vocabularies |

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
