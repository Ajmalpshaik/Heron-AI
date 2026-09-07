# Heron AI — Documentation Index

> **Status:** **Phase 0 is complete** — Steps 1 to 5, proven in real Revit 2020 and 2024. **Step 6 (the
> first write) and the whole of Phase 2 (Steps 7 to 14) are built, compile on all eight releases, and
> have never loaded into Revit.** Every fragment and every skill is `DRAFT`.
>
> **Do not trust this paragraph over the tool.** `python tools/check-gaps.py` is computed from disk on
> every run and sorts everything into *unfinished* and *waiting*; this sentence is typed. Where they
> disagree, the tool is right. Then pick up from [**HANDOVER.md**](HANDOVER.md).

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
| **Building on** | the owner's earlier brain and Revit-connector work, upgraded to this architecture |

**All 42 questions are answered — 42 answered · 0 open — and nothing gates any phase.** `Q-41` was both
asked and answered by the owner on 2026-09-06, during the decision read-back rather than by a specification.

**Phase 1, assessed 2026-08-28** (it had not been, and the count above had quietly stopped covering the
work in progress). Three questions touched Step 6 at the time, and one of them mattered:

| | |
|---|---|
| **Q-36** — lease or takeover | **Answered.** Built as `HeronLease` in Step 6 ([D-22](DECISIONS.md)) |
| **Q-19** — accept Golden Rules 16–21? | **Answered 2026-08-28 — accepted.** They are the rules Step 6 was built to obey (16, 17, 20, 21), and they are now binding rather than proposed |
| **Q-14** — how is testing against real Revit done? | **Answered 2026-08-28** — [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md), **52 items in dependency order as it stood that day** — it has grown since, and `python tools/check-gaps.py` is what knows the current figure — with what needs Revit separated from what does not. `A1`–`A3` passed the same day: the compile gate turned out not to need Windows ([30](30-compiling-away-from-windows.md)) |

**Nothing now blocks Phase 1 except Revit itself.**

**Specification is complete in four parts.** Part 1 is the platform and its organisation; Part 2 is the
Agent Operating System; Part 3 is the consolidated baseline, **authoritative on the Golden Rules**
(ten became fifteen, [D-12](DECISIONS.md)); Part 4 adds the **Kernel**, the **Workflow Engine**, the
**Constitution** and 30 mandatory components ([D-13](DECISIONS.md)).

**Two items need confirmation before building:** the [unified trust model](24-trust-model.md) —
six competing status vocabularies resolved into two axes ([Q-34](OPEN-QUESTIONS.md)) — and the
[Constitution](../HERON_CONSTITUTION.md) ([Q-35](OPEN-QUESTIONS.md)).

Review of all four parts: [PROPOSALS](PROPOSALS.md).

---

## Start here

| If you want to… | Read |
|---|---|
| **Pick up where the last session stopped** | [**HANDOVER.md**](HANDOVER.md) |
| Understand what Heron AI is | [01 — Vision & Principles](01-vision-and-principles.md) |
| See the original specification, unaltered | [00 — Master Specification](00-master-specification.md) |
| Know what is missing or risky | [PROPOSALS.md](PROPOSALS.md) |
| Know what still needs deciding | [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) |
| Know what gets built first | [ROADMAP.md](ROADMAP.md) |
| **Actually start building** | [**27 — Build Order**](27-build-order.md) |
| Know the non-negotiables | [14 — Golden Rules](14-golden-rules.md) |

---

## Core documents

| # | Document | Covers |
|---|---|---|
| 00 | [Master Specification — Part 1](00-master-specification.md) | The platform and its organisation, §1–§76. Source of truth. |
| 00b | [Master Specification — Part 2](00b-master-specification-agent-os.md) | The Agent Operating System and self-evolution, §1–§84. Source of truth. |
| 00c | [Master Handover Baseline — Part 3](00c-master-handover-baseline.md) | The consolidated baseline, §1–§80. **Authoritative on the Golden Rules.** |
| 00d | [Additional Requirements — Part 4](00d-additional-requirements.md) | Kernel, Workflow Engine, Constitution, 30 mandatory components, §1–§48. |
| 00e | [Field Notes — Proven Bridge Behaviour](00e-field-notes-proven-bridge.md) | **Observed, not designed.** Multi-Revit, session binding, the stale read. Proven live 2026-08-20. |
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
| 14 | [Golden Rules](14-golden-rules.md) | The constitution — **21 official rules**, 16–21 accepted 2026-08-28 |
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

### From Additional Requirements Part 4

| # | Document | Covers |
|---|---|---|
| 23 | [Heron Kernel & Workflow Engine](23-heron-kernel.md) | **The Kernel**, five registries, Workflow Engine, checkpoints, **Evidence System**, model abstraction, prompt registry |
| 24 | [The Unified Trust Model](24-trust-model.md) | **Resolves six competing status vocabularies** into two orthogonal axes: lifecycle and source |
| — | [**HERON_CONSTITUTION.md**](../HERON_CONSTITUTION.md) | 30 Articles agents must never violate — the runtime-enforceable subset of the Golden Rules |

### From the field

| # | Document | Covers |
|---|---|---|
| 25 | [Multi-Session & Document Binding](25-multi-session-and-binding.md) | **Field-proven.** Bridge discovery, one chat one Revit, **document pinning**, **the stale read**, turn-taking, cross-document work |

### Research

| # | Document | Covers |
|---|---|---|
| 26 | [Prior Art: Existing Revit MCP Servers](26-prior-art-revit-mcp.md) | What already exists, what it confirms, **the two gaps every project shares**, pyRevit Routes |
| 27 | [**Build Order**](27-build-order.md) | **Start here to build.** Six numbered steps, each independently provable |
| 28 | [**The Complete Agent Registry**](28-agent-registry.md) | **All 250 agents** — ID, what each one does, tier, risk level, build step |
| 29 | [Metadata Standard](29-metadata-standard.md) | The five fields every artefact carries — and how they tie the code back to the registry |
| 30 | [Compiling Away From Windows](30-compiling-away-from-windows.md) | **The compile gate runs anywhere.** All eight releases, 2020–2027, from NuGet on Linux; what a pass proves; the 2020-only defect it caught on its first run; and why three releases were skipped for a reason that was about the SDK package, not the operating system |
| 31 | [Studying The Existing Libraries](31-studying-the-existing-libraries.md) | **How a fragment is studied and re-authored, never imported.** What travels (the mechanism, the scar) and what cannot (the code, the words, the proof); the owner's three rules — check and edit, add, split; and why 221 verified fragments arrive here as 221 unproven ones |

## Working documents

| Document | Purpose |
|---|---|
| [**HANDOVER.md**](HANDOVER.md) | Where the last session stopped — what exists, what is **proven** rather than merely built, and what to say to carry on |
| [**NEEDS-CHECKING.md**](NEEDS-CHECKING.md) | The proving register — every unproven claim as a numbered item, grouped by what it needs. `python tools/check-gaps.py` reads this file |
| [PROPOSALS.md](PROPOSALS.md) | Gaps found in review, feature ideas, strategic questions |
| [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) | 42 questions, prioritised, with answer slots |
| [ROADMAP.md](ROADMAP.md) | Phase 0 → Phase 7, and what is deliberately deferred |
| [DECISIONS.md](DECISIONS.md) | Append-only log of decisions actually made |
| [../tools/](../tools/README.md) | Scripts that keep these documents honest — link checker, count recomputer, map generator |

---

## Reading conventions

- **[NOTE]** blocks are engineering commentary added during review — not part of the original specification.
- The [Master Specification](00-master-specification.md) is never edited to "fix" it. Changes are recorded as decisions.
- 🔴 blocking · 🟠 important · 🟡 worth doing · 🔵 idea

---

## The one-line version

> The user focuses on BIM. Heron AI focuses on everything behind the BIM work.
