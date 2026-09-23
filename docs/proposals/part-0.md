# Proposals — Part 0

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part 0 — What Master Specification Part 2 changed

Part 2 is not more of Part 1. Part 1 said *which agents exist*; Part 2 says *how they are governed*.
It closes four gaps the Part 1 review raised, and adds several mechanisms that were missing.

### ✅ Gaps closed by Part 2

| Was | Closed by | Detail |
|---|---|---|
| **A3** — "agent" conflated with "LLM call"; 150 agents would be unusably slow and expensive | **§17 Model Router** + **§59 Cost Optimization** + **§83** | Part 2 arrives at the same conclusion from three directions: route cheap work to cheap models, never generate code when a proven fragment exists, and run *only required agents*. Combined with the T1/T2/T3 tiering, this is settled. → [19](../19-context-and-cost.md) |
| **A17** — retrieval was vector-only, which handles exact technical tokens badly | **§19 RAG Operating System** | Specifies semantic **and** keyword search **and** metadata filtering, plus conflict detection. This is the hybrid stack recommended in [05 §4](../05-heron-brain.md). → [20 §1](../20-knowledge-trust-and-conflict.md) |
| **A12** — autonomous agent creation had no hard safety boundary | **§10 Shadow Mode** + **§54 autonomy proportional to risk** | Shadow Mode is a better answer than the proposed human-approval rule alone, because it produces *evidence* rather than a signature. Recommendation is now: keep both. → [18 §4](../18-agent-operating-system.md) |
| *(implicit)* — vector DB risked becoming the only copy of knowledge | **§77 Source of Truth Principle** | States explicitly that the vector DB is an index, not truth, and must be rebuildable. Matches [05 §7](../05-heron-brain.md). → [21 §7](../21-resilience-and-operations.md) |

### 🆕 The best new ideas in Part 2

| # | Idea | Why it matters |
|---|---|---|
| **P1** | **Capability Registry separate from Agent Registry** (§6) | The most valuable structural idea in either document. The Orchestrator matches *capabilities*, never agent names — so agents become replaceable, retirement becomes safe, the Orchestrator stays free of domain knowledge, and capability-gap detection becomes trivial. → [18 §2](../18-agent-operating-system.md) |
| **P2** | **Shadow Mode** (§10) | Safe self-evolution. A new agent runs on real requests and is scored, without its output reaching anything. → [18 §4](../18-agent-operating-system.md) |
| **P3** | **Dependency Graph** (§41) | Makes "does this change break anything?" computable instead of requiring a full test sweep. This is what makes [D-05](../DECISIONS.md) (8 Revit versions) and Golden Rule 4 affordable in practice. → [21 §1](../21-resilience-and-operations.md) |
| **P4** | **Knowledge Conflict Resolution** (§22) | Prevents the classic decay of an accumulated knowledge base: two fragments disagree and retrieval silently picks whichever ranked higher that day. → [20 §4](../20-knowledge-trust-and-conflict.md) |
| **P5** | **Emergency Stop** (§56) | Necessary as autonomy grows. Must live in the Revit add-in UI so it works when the agent system is stuck. → [21 §4](../21-resilience-and-operations.md) |
| **P6** | **Workflow ID in the audit log** (§57) | The correlation key tying one user sentence to every agent, retrieval, model call and element touched. Turns "what did Heron change?" into a query. → [21 §13](../21-resilience-and-operations.md) |
| **P7** | **Human approval only at meaningful boundaries** (§55) | An important corrective. Over-asking destroys a tool as surely as under-asking damages a model. → [21 §3](../21-resilience-and-operations.md) |
| **P8** | **Fragment branching, one semantic fragment** (§26) | Confirms the approach already taken in [16](../16-version-support-strategy.md). |

### 🔴 Still open after Part 2

Part 2 does not address these, and they remain the most important outstanding items. All are covered in
the review documents; none are in either specification:

| # | Gap | Where handled |
|---|---|---|
| **A1** | **Revit API threading** — an MCP server cannot call the Revit API at all; everything must marshal through `ExternalEvent` | [03 §4](../03-heron-revit.md), settled in [D-09](../DECISIONS.md) |
| **A6** | **Undo** — still never mentioned in either document. One operation must equal one Ctrl+Z | [Golden Rule 16](../14-golden-rules.md) |
| **A7** | **Preview before modify** — Part 2 §54 gates *whether* an action runs, never shows *what it will do* | [Golden Rule 17](../14-golden-rules.md), [03 §10](../03-heron-revit.md) |
| **A9** | **Data egress** — §74 covers isolation *inside* Heron, not what leaves the machine to a model provider | [12 §4](../12-security-and-permissions.md), [Q-12](../OPEN-QUESTIONS.md) |
| **A11** | **How Revit tests actually run** — §43 says what to test and §48 rightly separates Code QA from Revit QA, but neither says how a test executes inside Revit | [13 §3](../13-testing-and-quality.md), [Q-14](../OPEN-QUESTIONS.md) |

**Golden Rules 16–19 remain necessary.** Part 2 strengthens the case for them rather than replacing them.

### ⚠️ New tensions Part 2 creates

| # | Tension | Resolution |
|---|---|---|
| **T1** | **Model Router (§17) vs Claude Code as host ([D-01](../DECISIONS.md))** — Claude Code chooses the model, so a Heron-owned router partly duplicates the host | Declare routing *intent* as a cost tier in the capability registry; let the host resolve it. Route internally only for batch work later. → [19 §3](../19-context-and-cost.md), [Q-30](../OPEN-QUESTIONS.md) |
| **T2** | **Multi-user + Admin Mode (§72, §73) vs single-user plugin** — these describe an enterprise product with a server, user directory and enforced policy | Company knowledge as a shared **git repository**, not a server. Delivers most of the value with none of the infrastructure. → [22 §4](../22-users-modes-and-extensibility.md), [Q-32](../OPEN-QUESTIONS.md) |
| **T3** | **"1,000 agents or more" (§2)** | Makes the T1/T2/T3 discipline non-optional rather than merely advisable. 1,000 LLM-backed agents is not a viable system; 1,000 governed components is. → [02 §6](../02-architecture-overview.md) |

---
