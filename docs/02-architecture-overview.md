# 02 — Architecture Overview

> Derived from [Master Specification](00-master-specification.md) §3, §4, §50, §56–58, §73.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Layer stack

```text
USER
 |
 v
CONVERSATION / PERSONA LAYER
 |
 v
INTENT + ORCHESTRATOR
 |
 +-------------------------------+
 |                               |
 v                               v
TASK / WORKFLOW ENGINE       KNOWLEDGE SYSTEM
 |                               |
 v                               v
SPECIALIZED AGENTS           RAG / LIBRARY / MEMORY
 |
 v
VALIDATION
 |
 v
EXECUTION
 |
 v
RESULT
 |
 v
LEARNING / MEMORY / FRAGMENT EVOLUTION
```

## 2. The four product parts

| Part | Owns | Document |
|---|---|---|
| **Heron Revit** | Everything touching the Revit API and the add-in | [03](03-heron-revit.md) |
| **Heron MCP** | The communication bridge between AI and Revit | [04](04-heron-mcp.md) |
| **Heron Brain** | Skills, Fragments, RAG, memory, knowledge | [05](05-heron-brain.md) |
| **Heron Platform** | Install, update, packages, registry, security, GitHub | [06](06-heron-platform.md) |

## 3. Company analogy

```text
                    HERON AI
                       |
                 CEO / ORCHESTRATOR
                       |
       +---------------+----------------+
       |               |                |
   ENGINEERING      KNOWLEDGE       PLATFORM
       |               |                |
     Revit            RAG             Install
     MCP              Brain           Update
     .NET             Memory          Package
     Coding           Skills          Security
     QA               Fragments       Workspace
     Testing          Library         GitHub
       |               |                |
       +---------------+----------------+
                       |
                 COMMUNICATION
                       |
                  BIM USER
```

## 4. Orchestrator responsibilities

1. Understand request
2. Identify required capability
3. Select relevant agents
4. Build workflow
5. Execute workflow
6. Handle failures
7. Validate result
8. Return result
9. Trigger background learning

The Orchestrator coordinates. It does **not** accumulate domain logic. The moment it starts knowing about ducts, it has become the monolith the architecture exists to prevent.

## 5. Dynamic agent selection

Not every agent runs for every request.

| Request | Chain |
|---|---|
| "Select all ducts." | Intent → Orchestrator → Fragment Librarian → Revit Selection → Execution → Validation |
| "Create a tool that dimensions ducts." | Requirement → Architecture → RAG → Revit API → .NET → Code Gen → Review → Build → Test → Revit Test → QA → Release |

## 6. **[NOTE — critical]** "Agent" must not mean "LLM call"

This is the single most important correction to make before any code is written.

The specification names roughly **150 agents**. If each one is an LLM call, then "select all ducts" costs dozens of model round-trips: several seconds of latency and real money, for an operation that is fundamentally one `FilteredElementCollector` query.

Heron must therefore recognise **three distinct kinds of "agent"**:

| Tier | Name | Implementation | Cost | Examples |
|---|---|---|---|---|
| **T1** | **Service** | Plain deterministic code. No model call, ever. | ~0 ms | Revit Selection, Revit Transaction, Vector Search, Index Manager, Health Check, Environment Detection, File Discovery, Naming Validation |
| **T2** | **Reasoner** | One scoped LLM call with a tight contract | 100s of ms – seconds | Intent Detection, Fragment Matcher, Ranking, Failure Analysis, Content Classification |
| **T3** | **Worker** | Full agentic loop with tools | seconds – minutes | Code Generation, Architecture, Code Review, Import & Migration, Agent Creator |

Rules that follow from this:

1. **Default to T1.** An agent is only promoted to T2/T3 when the task genuinely requires judgement over ambiguous input.
2. **The happy path must be LLM-free.** Once "select all ducts" has a proven fragment, the *entire* execution — match, compatibility check, execute, validate — should be deterministic. The model is consulted for intent, then gets out of the way.
3. **Every T2/T3 agent declares its tier in the registry** (§38), so cost and latency are inspectable.
4. **Background work is T1 wherever possible.** A vector re-index must never cost money.

Rough target for a warm, cached "select all ducts": **one T2 call (intent), zero others.**

## 7. Where the orchestration runs — **decided**

> **[D-01](DECISIONS.md), 2026-08-27: Heron AI runs as a Claude Code plugin.**

Claude Code is the conversation layer and the agent host. Heron supplies skills, subagents, an MCP server and the Revit add-in.

Combined with **[D-06](DECISIONS.md)** (C# for Revit, Python for the brain), the stack is:

```text
Claude Code           host: conversation, agents, persona, orchestration
     |  MCP
Heron MCP Server      Python — brain, RAG, fragments, skills, memory
     |  IPC  (transport = D-02, pending)
Heron Revit Add-in    C# — one build per Revit version
     |  ExternalEvent
Revit
```

**What this buys.** The agent framework, conversation layer, subagent orchestration, tool routing and update mechanism all come for free. A large part of Part 4 (Heron Platform) shrinks accordingly — Claude Code already provides skills, subagents, MCP configuration and plugin updates. What remains genuinely Heron's to build is the Revit bridge, the brain, and the knowledge system.

**What this costs.** The user must install and run Claude Code and needs a Claude subscription. A BIM modeller works in a terminal, which sits in tension with Golden Rule 1 — mitigated by the Communication/Persona layer.

**[NOTE]** Keep the core host-agnostic wherever that is free. An in-Revit docked panel remains the better long-term experience for a BIM modeller, and nothing in this decision should make adding one later require a rewrite. The MCP server and the add-in are already host-independent by construction; the discipline applies only to whatever orchestration logic ends up living on the Heron side.

## 8. Agent contracts

Every agent declares:

```text
INPUT
PROCESS
OUTPUT
ERROR
STATUS
DEPENDENCIES
```

No uncontrolled agent-to-agent chatter. The Workflow/Orchestrator layer coordinates.

**[NOTE]** Recommend expressing every contract as a **JSON Schema** stored beside the agent. That gives, for free: validation at the boundary, structured-output enforcement for T2/T3 agents, generated documentation, and contract-diff detection when an agent version changes.

## 9. One responsibility per agent

| Bad | Good |
|---|---|
| `RevitSuperAgent` | `Revit Selection Agent` |
| `KnowledgeEverythingAgent` | Librarian / Retriever / Ranking / Context Builder / Validator |

**[NOTE]** The counter-pressure is real: 150 tiny modules with 150 contracts is its own maintenance burden. The discipline that keeps this healthy is **departments** — a stable outer boundary with many small parts inside. Callers depend on the *department*, not on individual agents, so agents can be split, merged or retired without breaking anything outside their department. This is what makes the Fragment Split / Merge / Evolution agents (§21–23) safe to apply to the agent layer itself.
