# Decision Log

> Every architectural decision that has been **made**, with the reasoning behind it.
> Answers from [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are promoted here once settled.
>
> This log is append-only. A decision that is later reversed gets a **new entry** that supersedes
> the old one — the original stays, so the reasoning history is never lost.
> This mirrors Golden Rule 4: never destroy a working record.

---

## Status summary

| # | Decision | Status |
|---|---|---|
| [D-00](#d-00--documentation-first-no-implementation-yet) | Documentation first, no implementation yet | ✅ Accepted |
| [D-01](#d-01--execution-host--claude-code-plugin) | Execution host — Claude Code plugin | ✅ Accepted |
| [D-02](#d-02--mcp--add-in-transport-named-pipes) | MCP ↔ add-in transport — named pipes | ✅ Accepted |
| [D-03](#d-03--mcp-tool-granularity--thick-and-specific) | MCP tool granularity — thick and specific | ✅ Accepted |
| [D-04](#d-04--generated-code-execution--hybrid) | Generated code execution — hybrid | ✅ Accepted |
| [D-05](#d-05--revit-version-support-2020-to-latest) | Revit version support — 2020 → latest | ✅ Accepted |
| [D-06](#d-06--implementation-languages-c-for-revit-python-for-brain) | Languages — C# for Revit, Python for brain | ✅ Accepted |
| [D-07](#d-07--free-open-source-on-public-github) | Free open source on public GitHub | ✅ Accepted |
| [D-08](#d-08--licence--apache-20) | Licence — Apache 2.0 | ✅ Accepted |
| [D-09](#d-09--revit-thread-marshalling--externalevent) | Revit thread marshalling — ExternalEvent | ✅ Accepted |
| [D-10](#d-10--repository-stays-private-until-working-code-exists) | Repo stays private until code exists | ✅ Accepted |
| [D-11](#d-11--adopt-master-specification-part-2-agent-operating-system) | Adopt Master Specification Part 2 (Agent OS) | ✅ Accepted |
| [D-12](#d-12--adopt-the-master-handover-baseline-part-3-and-its-fifteen-golden-rules) | Adopt Baseline (Part 3) + 15 Golden Rules | ✅ Accepted |
| [D-13](#d-13--adopt-additional-requirements-part-4--kernel-workflow-engine-constitution) | Adopt Part 4 — Kernel, Workflow Engine, Constitution | ✅ Accepted |
| [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes) | Unify six status vocabularies into two axes | ⏳ Proposed |

**All Tier 1 blocking questions are now answered.** Phase 0 is unblocked — awaiting the owner's
go-ahead to start building ([D-00](#d-00--documentation-first-no-implementation-yet)).

---

## Format

```markdown
## D-NN — Short title

**Status:** Proposed | Accepted | Superseded by D-NN | Rejected
**Date:** YYYY-MM-DD
**Question:** Q-NN
**Affects:** which documents / components

### Context
What made this decision necessary.

### Decision
What was decided. One or two sentences, stated plainly.

### Alternatives considered
What else was on the table, and why it lost.

### Consequences
What this makes easy. What this makes hard. What it locks in.
```

---

## D-00 — Documentation first, no implementation yet

**Status:** Accepted · **Date:** 2026-08-27 · **Affects:** the whole repository

### Context

The full platform vision was specified in one pass. Building from it directly would mean designing
~150 agents against untested assumptions about how Revit, MCP and code execution actually interact.

### Decision

The repository holds **specification and planning only**. No implementation code is written until the
owner explicitly says to start.

### Consequences

- The architecture is inspectable and arguable before anything is committed to code.
- Open questions are visible rather than discovered one at a time during implementation.
- Nothing runs yet. First code is Phase 0 in [ROADMAP.md](ROADMAP.md).

---

## D-01 — Execution host: Claude Code plugin

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-1](OPEN-QUESTIONS.md)
**Affects:** [02](02-architecture-overview.md), [04](04-heron-mcp.md), [07](07-installation-and-update.md), [ROADMAP](ROADMAP.md)

### Context

Heron could be a Claude Code plugin, a standalone application with a chat panel docked inside Revit,
or both. The spec's install flow already began with "user installs Claude Code".

### Decision

**Heron AI runs as a Claude Code plugin.** Claude Code is the conversation layer and the agent host.
Heron supplies skills, subagents, an MCP server and the Revit add-in.

### Alternatives considered

- **Standalone app / in-Revit panel** — better UX for a non-technical BIM modeller, but a very large
  amount of work before anything runs, and it would mean rebuilding an agent framework that already exists.
- **Both from the start** — more work than the current stage justifies.

### Consequences

**Makes easy:**
- The entire agent framework, conversation layer, persona handling and tool orchestration come for free.
- Installation becomes a plugin install plus an add-in deploy.
- Much of Part 4 (Heron Platform) shrinks dramatically — Claude Code already provides skills,
  subagents, MCP configuration and updates.

**Makes hard / accepts:**
- The user must install and run Claude Code, and needs a Claude subscription.
- A BIM modeller works in a terminal, which is in tension with Golden Rule 1. Mitigated by the
  Communication/Persona layer, and revisitable later.

**Locks in:**
- Agent definitions follow Claude Code conventions (skills, subagents, MCP tools).
- The resulting architecture stack:

```text
Claude Code           (host: conversation, agents, orchestration)
     |  MCP
Heron MCP Server      (Python — brain, RAG, fragments, skills)
     |  IPC (D-02)
Heron Revit Add-in    (C# — per Revit version)
     |  ExternalEvent
Revit
```

**Deliberately not closed:** the core stays host-agnostic where it is free to do so, so an in-Revit
panel remains possible later without a rewrite.

---

## D-02 — MCP ↔ add-in transport: named pipes

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-2](OPEN-QUESTIONS.md)
**Affects:** [03 §5](03-heron-revit.md), [04](04-heron-mcp.md)

### Decision

**Windows named pipes.** The C# add-in is the pipe **server**; the Python MCP server is the client.
Pipe name encodes Revit version and process ID: `heron.{revitVersion}.{pid}`.
Messages are JSON, schema-versioned, request/response with correlation IDs.

### Alternatives considered

- **Localhost HTTP/WebSocket** — easiest to debug, but brings port conflicts, firewall prompts, and
  requires authentication or any local process could drive Revit.
- **gRPC** — typed contracts and streaming, but heavier and adds a codegen step across two languages.

### Consequences

- **Local-only by construction** — no network surface at all, which removes a whole class of security
  problem before it exists.
- Pipe ACLs restrict access to the current user.
- The version+PID naming handles the real case of Revit 2023 and 2025 open simultaneously; Heron must
  still resolve "which Revit did you mean?" when more than one is running.
- The pipe contract is also the **Python ↔ C# language boundary** ([D-06](#d-06--implementation-languages-c-for-revit-python-for-brain)),
  so it must be explicitly versioned and treated as a public API between the two halves.

---

## D-03 — MCP tool granularity: thick and specific

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-5](OPEN-QUESTIONS.md)
**Affects:** [04 §3](04-heron-mcp.md), [09](09-skills-and-fragments.md), [12](12-security-and-permissions.md)

### Decision

**Narrow, specific tools** — `revit_select_by_category`, `revit_move_elements`, `revit_get_parameter` —
each mapping onto a fragment and carrying its own declared risk level.

A generic `revit_execute(script)` exists **only** in Developer Persona, behind `ADMIN`, and never
against a live project model without a preview or a detached copy.

### Alternatives considered

- **Thin generic tools** (`revit_query`, `revit_execute`) — flexible, but effectively remote code
  execution against a live project model, impossible to permission meaningfully, and captures nothing
  reusable for the fragment system.

### Consequences

- Every tool has a clear risk level and a clear permission gate — this is what makes
  [D-09](#d-09--revit-thread-marshalling--externalevent) and Golden Rule 9 enforceable.
- Tools map one-to-one onto fragments, so the knowledge system accumulates naturally from use.
- **Accepts:** the tool list grows large, and MCP tool schemas cost context on every request.
  Mitigated by capability discovery — a small stable core set plus `heron_find_capability`,
  registering specific tools only when needed. This must be designed in from the start, not retrofitted.

---

## D-04 — Generated code execution: hybrid

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-7](OPEN-QUESTIONS.md)
**Affects:** [09 §10](09-skills-and-fragments.md), [13](13-testing-and-quality.md)

### Decision

**Hybrid, mapped onto the fragment lifecycle:**

| Lifecycle stage | Execution |
|---|---|
| `DISCOVERED` → `TESTING` | **Scripting sandbox** — iterate freely, no assembly leak, fast feedback |
| `VALIDATED` → `PRODUCTION` | **Compiled, signed C#** — shipped as a tested assembly |

The `PROVEN → PRODUCTION` gate is exactly where a fragment gets compiled and signed.

### Alternatives considered

- **Runtime Roslyn compilation into Revit** — assemblies **cannot be unloaded** from .NET Framework
  (Revit ≤ 2024), so every iteration leaks. `AssemblyLoadContext` helps only on .NET 8 (Revit 2025+),
  which does not cover the supported range.
- **Precompiled only** — safest and fastest, but "Heron builds a new tool during the session" becomes
  impossible, losing a core part of the product idea.

### Consequences

- Iteration happens where it is cheap; permanence happens where it is safe.
- The lifecycle in spec §18 turns out to *describe* this hybrid — a good sign the design is coherent.
- Golden Rule 18 (generated code never touches a live model first) is enforced by the sandbox.
- **Open sub-decision:** which scripting runtime (pyRevit / IronPython / Python.NET / Roslyn scripting).
  The owner's existing pyRevit work is the strongest evidence and should be reviewed before choosing.

---

## D-09 — Revit thread marshalling: ExternalEvent

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-4](OPEN-QUESTIONS.md)
**Affects:** [03 §4](03-heron-revit.md)

### Decision

**`ExternalEvent` with a single request queue and one `IExternalEventHandler`** — not one event per
operation. `Idling` is used only for a lightweight liveness heartbeat.

### Consequences

- One queue makes ordering, cancellation and timeouts tractable, and avoids exhausting Revit's
  appetite for registered external events.
- Every operation is asynchronous from the caller's perspective; the MCP layer owns timeouts.
- `ExternalEvent.Raise()` is a request, not a guarantee — if a modal dialog is open or the user is
  mid-command, nothing runs. Heron must surface **"Revit is busy"** rather than hanging.
- Nothing may block the Revit main thread. Long operations report progress, yield, and are cancellable.
- No `Document` or `Element` may be cached across handler invocations — `UniqueId` is stored and
  re-resolved each time.

---

## D-05 — Revit version support: 2020 to latest

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-3](OPEN-QUESTIONS.md)
**Affects:** [03](03-heron-revit.md), [13](13-testing-and-quality.md), [16](16-version-support-strategy.md)

### Context

The platform could target one Revit version first and expand, or commit to broad support from the start.

### Decision

**Heron AI supports Revit 2020 through the latest release, and every future release.**

### Consequences

**Accepts:**
- Two API breaks must be handled permanently: `ElementId` 32→64-bit at Revit 2024, and
  .NET Framework → .NET 8 at Revit 2025.
- An eight-version test matrix that grows annually.

**Requires, from the first line of code:**
- Multi-targeting from one source tree (`net48` + `net8.0-windows`) — never branch-per-version.
- An adapter layer holding all version-conditional code.
- Core logic that never references `Autodesk.Revit.*` directly.
- `UniqueId` used for element identity everywhere, never raw `ElementId`.

**Sequencing note:** supporting all versions is a requirement of the finished platform. The first
vertical slice is still built and proven on **one** version, with the multi-target structure already
in place, then fanned out. Full strategy in [16](16-version-support-strategy.md).

---

## D-06 — Implementation languages: C# for Revit, Python for brain

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-6](OPEN-QUESTIONS.md)
**Affects:** [03](03-heron-revit.md), [04](04-heron-mcp.md), [05](05-heron-brain.md)

### Context

The Revit add-in must be C#. The server, orchestrator and brain could be C# (one language) or
Python (much stronger AI/RAG ecosystem).

### Decision

**C# for everything that talks to Revit. Python for the brain, RAG and knowledge layer.**
The two meet at the IPC boundary (D-02).

```text
Python   Heron MCP server, brain, RAG, fragments, skills, memory, orchestration support
  |
  |  IPC boundary — the only place the two languages meet
  |
C#       Revit add-in, ExternalEvent handler, all Revit API access, per-version adapters
```

### Consequences

**Makes easy:**
- Best-in-class embedding, vector search and RAG tooling on the Python side.
- Correct, idiomatic, fully-supported Revit API access on the C# side.
- The language boundary sits exactly where the process boundary already had to be — so it costs
  nothing architecturally that D-02 was not already paying.

**Accepts:**
- Two runtimes to ship and two toolchains to maintain.
- The IPC contract must be explicit and versioned, since it is now also a language boundary.

### Reference implementations

The owner has existing work to build on rather than starting from zero:

| Repository | Role |
|---|---|
| `AJ-AI-Brain` | Reference for the brain / knowledge layer |
| `AJ-Connect` | Reference for the Revit connector / bridge |
| `AJ-Tools`, `PyRevit-Tools`, `AEB-Tools` | Existing Revit tooling — candidates for the first knowledge import |

**Plan:** take the ideas from `AJ-AI-Brain` and `AJ-Connect`, upgrade them, and reshape them to
the Heron architecture. This work happens **after** documentation is finalised, on the owner's signal.

---

## D-07 — Free open source on public GitHub

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-21](OPEN-QUESTIONS.md), [Q-22](OPEN-QUESTIONS.md)
**Affects:** [06](06-heron-platform.md), [10](10-memory-and-knowledge.md), [12](12-security-and-permissions.md), [17](17-open-source-and-distribution.md)

### Context

Heron could be personal tooling, a company-internal product, or a public product.

### Decision

**Heron AI is a free, open-source product published on public GitHub**, installable by anyone.
An Autodesk App Store listing follows later, also free.

### Consequences

**Makes easy:**
- The community knowledge lifecycle (§35) maps naturally onto pull requests.
- The marketplace concept becomes coherent rather than hypothetical.

**Requires:**
- A licence — not yet chosen, see [Q-27](OPEN-QUESTIONS.md). Recommendation: **Apache 2.0**.
- `CONTRIBUTING.md`, `SECURITY.md`, a warranty disclaimer, issue templates, CI.
- **Absolute separation between public code and private knowledge.** Client project data,
  user memory and audit logs must live outside the repository directory entirely — a single
  accidental commit is permanent and unrecoverable.
- Semantic versioning and stated compatibility promises, since others will depend on it.

**Note on the current repository:** `Ajmalpshaik/Heron-AI` is **still private**. Flipping it to
public is irreversible in practice and has not been done — it needs an explicit instruction from
the owner, once the licence and the public/private file separation are in place.

Full plan: [17 — Open Source & Distribution](17-open-source-and-distribution.md).

---

## D-08 — Licence: Apache 2.0

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-27](OPEN-QUESTIONS.md)
**Affects:** `LICENSE`, `NOTICE`, [17 §3](17-open-source-and-distribution.md)

### Context

The owner delegated the choice. Four candidates were considered against two criteria that matter for
this particular product: **company legal teams must be able to approve it** (the users work at
contractors and consultancies), and it must **disclaim warranty explicitly** (the software writes to
live client models).

### Decision

**Apache License 2.0.** Canonical text fetched from GitHub's licence API, copyright
"2026 Ajmal PS".

### Alternatives considered

| Licence | Why not |
|---|---|
| **MIT** | Most familiar in Revit tooling and the closest alternative. Lacks an explicit patent grant and has a much thinner warranty clause — weaker protection for software that modifies client deliverables. |
| **GPL-3.0** | Would prevent a closed commercial fork, but many construction and engineering firms forbid GPL software internally. Directly conflicts with D-07's goal of installation by anyone. |
| **MPL-2.0** | Reasonable middle ground, but less familiar and buys little that Apache 2.0 does not. |

### Consequences

- Anyone may use, modify and commercialise Heron AI, including in closed products.
- The explicit warranty disclaimer supports the position taken in `DISCLAIMER.md` and partially
  addresses the liability question (Q-25).
- The patent grant protects contributors and users.
- **Accepts:** someone could fork Heron into a closed paid product. Judged acceptable — adoption
  matters more than defensiveness at this stage.
- The copyright holder name should be confirmed by the owner and corrected if it is not their
  preferred legal name.

---

## D-10 — Repository stays private until working code exists

**Status:** Accepted · **Date:** 2026-08-27 · **Question:** [Q-28](OPEN-QUESTIONS.md)
**Affects:** repository visibility, [17](17-open-source-and-distribution.md)

### Context

Heron AI will be public open source ([D-07](#d-07--free-open-source-on-public-github)), but publishing
is irreversible in practice — history persists, forks propagate, GitHub caches.

### Decision

**The repository stays private until both conditions are met:**

1. Licence and safety files are in place — **done 2026-08-27**
2. There is working code — Phase 0 complete

### Safety files completed under condition 1

| File | Purpose |
|---|---|
| `LICENSE` | Apache 2.0, canonical text |
| `NOTICE` | Copyright, Autodesk trademark position, non-redistribution of Revit API assemblies |
| `SECURITY.md` | Private vulnerability reporting; Heron-specific threat categories |
| `DISCLAIMER.md` | No warranty; use on a copy; professional responsibility stays with the user |
| `CONTRIBUTING.md` | Contribution process, fragment requirements, absolute rules |
| `CODE_OF_CONDUCT.md` | Conduct standards |
| `.github/ISSUE_TEMPLATE/` | Bug, feature, fragment proposal, security redirect |
| `.gitignore` | **Hardened** — blocks `.rvt`/`.rfa`/`.ifc`/`.dwg`, the entire data class, knowledge stores, audit logs, Revit journals, secrets, and Autodesk API assemblies |

### Consequences

- Nothing blocks Phase 0 — the repository can go public the moment there is something worth publishing.
- A public repository with no working code attracts no users anyway, so nothing is lost by waiting.
- Before flipping to public, verify once more that no client data has entered the history — the
  `.gitignore` is a safety net, not a guarantee against a deliberate `git add -f`.

---

## D-11 — Adopt Master Specification Part 2 (Agent Operating System)

**Status:** Accepted · **Date:** 2026-08-27
**Affects:** [18](18-agent-operating-system.md), [19](19-context-and-cost.md), [20](20-knowledge-trust-and-conflict.md), [21](21-resilience-and-operations.md), [22](22-users-modes-and-extensibility.md), [ROADMAP](ROADMAP.md)

### Context

A second specification was provided, covering how Heron operates internally as an autonomous
engineering organisation. It is not more of Part 1 — Part 1 says *which agents exist*, Part 2 says
*how they are governed*.

### Decision

**Part 2 is adopted as source of truth alongside Part 1.** Both are preserved verbatim and neither is
edited to reconcile with the other; conflicts are recorded as decisions here.

The following Part 2 mechanisms are adopted into the architecture:

| Mechanism | Where it lands |
|---|---|
| **Capability Registry**, separate from the agent registry | Phase 2. The Orchestrator resolves capabilities, never agent names |
| **Shadow Mode** for agent onboarding | Phase 5, alongside the sandbox |
| **Dependency graph** over skills → fragments → API → runtime | Phase 2. Makes change blast-radius computable |
| **Knowledge trust levels and conflict resolution** | Phase 4, sharing one status vocabulary with the fragment lifecycle |
| **Emergency Stop** | Phase 1, in the Revit ribbon — must work when the agent side is stuck |
| **Workflow ID** in the audit log | Phase 1 |
| **Approval only at meaningful boundaries** | Applies immediately to all permission design |
| **Fragment branching** — one semantic fragment, many implementations | Confirms [16](16-version-support-strategy.md) |

### What Part 2 closed

Four gaps raised in the Part 1 review are resolved by Part 2 — agent/LLM conflation (§17, §59, §83),
vector-only retrieval (§19), no safety boundary on agent creation (§10 Shadow Mode), and the vector DB
as sole source of truth (§77). Detail in [PROPOSALS Part 0](PROPOSALS.md).

### What Part 2 did not change

**Golden Rules 16–19 remain necessary.** Neither specification mentions undo, preview-before-modify,
data egress to a model provider, or how a Revit test actually executes. Part 2 strengthens the case for
these rules rather than replacing them.

### Conflicts recorded, not resolved by Part 2

| Conflict | Position taken |
|---|---|
| **Model Router (§17) vs Claude Code as host ([D-01](#d-01--execution-host-claude-code-plugin))** | Routing *intent* is declared as a cost tier in the capability registry; the host resolves it. Heron may route its own batch work later. Open as [Q-30](OPEN-QUESTIONS.md) |
| **Multi-user / Admin Mode (§72–73) vs single-user plugin** | Company knowledge as a shared **git repository**, not a server. Open as [Q-32](OPEN-QUESTIONS.md) |
| **"1,000 agents or more" (§2)** | Not a conflict but a reinforcement — makes the T1/T2/T3 tiering non-optional |

### Consequences

- No change to any prior decision. Part 2 refines and reinforces; it does not overturn.
- Five new open questions (Q-29 to Q-33). **None block Phase 0** — they shape Phases 2–5.
- The Capability Registry moves into Phase 2 rather than later: retrofitting it would mean rewriting
  every call site.

---

## D-12 — Adopt the Master Handover Baseline (Part 3) and its fifteen Golden Rules

**Status:** Accepted · **Date:** 2026-08-27
**Affects:** [14 — Golden Rules](14-golden-rules.md) and **every document that cross-references a rule number**

### Context

A third document was provided: a consolidated *Master Project Handover Note*, describing itself as
"the handover baseline". Roughly 90% of it restates Parts 1 and 2. But **§78 replaces the ten Golden
Rules with fifteen**, renumbers two of the originals, and shifts the meaning of a third.

Since the Golden Rules are the constitution — referenced by number throughout the repository, in
`SECURITY.md` and in `CONTRIBUTING.md` — this could not be absorbed silently.

### Decision

**Part 3 is adopted as the consolidated baseline and is authoritative on the Golden Rules.**
All three parts remain source of truth and are preserved verbatim; where they differ, Part 3 governs
the rule set, and Parts 1 and 2 remain the detailed reference for everything else.

**All cross-references in the repository were mechanically remapped** to the new numbering:

| Was | Now |
|---|---|
| Rule 3 — never break a working implementation | **Rule 4** — never break a working Revit version |
| Rule 4 — reuse proven fragments | **Rule 3** — reuse proven knowledge |
| Rule 8 — background *remains invisible* | **Rule 8** — background *must not interfere* (visibility → performance) |
| *(new)* | **Rules 11–15** — source of truth, no auto-publishing, no self-modification, auditability, modularity |
| Proposed 11 — one undo | **Proposed 16** |
| Proposed 12 — no autonomous write without preview | **Proposed 17** (now also carries the Sync With Central clause) |
| Proposed 13 — sandbox generated code | **Proposed 18** |
| Proposed 14 — never publish on own initiative | **absorbed into official Rule 12** |
| Proposed 15 — no permission escalation from text | **Proposed 19** |

Rules 1, 2, 5, 6, 7, 9 and 10 keep their numbers.

### Also adopted from Part 3

| § | Item |
|---|---|
| §1 | Future capabilities must be **explicitly marked** as not-yet-implemented. Matches [D-00](#d-00--documentation-first-no-implementation-yet) |
| §42 | **Reference Update Agent** must verify imports, references, metadata, registry, documentation and relationships after any rename or move. *No broken references.* |
| §32 | **Compatibility Agent combines** the Revit Version, Revit API, .NET and Dependency agent results, rather than duplicating them |
| §57 | **Least privilege** — agents receive only the permissions they require. Becomes a required registry field |
| §20 | **Trust Evaluation** as a retrieval pipeline stage |

### Rejected from Part 3

**§20's placement of metadata filtering after all three searches.** Parts 1 and 2 filter earlier, which
is correct: filtering last means embedding and searching a corpus about to be discarded, and it defers
scope isolation (Rule 5) from query time to ranking time. The adopted pipeline keeps Part 3's Trust
Evaluation stage and Parts 1–2's filter placement — see [20 §1](20-knowledge-trust-and-conflict.md).

### Consequences

- The constitution is now **15 official + 4 proposed** rules, and it is stable across all three documents.
- No prior decision (D-01 to D-11) is overturned. Part 3 consolidates; it does not redirect.
- The two tensions recorded in [D-11](#d-11--adopt-master-specification-part-2-agent-operating-system)
  remain open — Part 3 restates Model Routing (§61) and Multi-User (§65) without resolving either.
- No new open questions. Part 3 raises none that Parts 1 and 2 had not already raised.

---

## D-13 — Adopt Additional Requirements (Part 4): Kernel, Workflow Engine, Constitution

**Status:** Accepted · **Date:** 2026-08-27
**Affects:** [23](23-heron-kernel.md), [24](24-trust-model.md), [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md), [ROADMAP](ROADMAP.md), [19](19-context-and-cost.md)

### Context

A fourth document was provided — *Additional Master Requirements & Recommendations* — framed as
recommendations but closing with 30 items described as **"mandatory, not optional"**, and an explicit
build order.

### Decision

**Part 4 is adopted.** It adds real architecture rather than restating; the mandatory list and the build
order are both accepted.

Principal additions:

| Addition | Lands |
|---|---|
| **Heron Kernel** — config, identity, permissions, event bus, four registries, memory, workflow, state, logging, security | Phase 0/1 foundation. [23 §1](23-heron-kernel.md) |
| **Workflow Engine**, separate from the Orchestrator | Phase 1. Orchestrator decides *what*; the engine ensures it *happens correctly* |
| **Checkpoints / resume** — *"Continue."* from the failed step | Phase 1. [23 §4](23-heron-kernel.md) |
| **Evidence System** — every important decision states its reasons | Phase 1, alongside the audit log |
| **AI Model Abstraction Layer** | Phase 2. Resolves [Q-30](OPEN-QUESTIONS.md) |
| **Prompt / Instruction Registry** | Phase 0. Cheap now, unmaintainable later |
| **Transaction Safety Agent**, separate from Revit API logic | Phase 1 |
| **Revit Context Agent** | Phase 1 |
| **BIM QA** as a third QA type, distinct from Code QA and Revit QA | Phase 3 |
| **Golden Test Library** | Phase 1 onward, grows continuously |
| **Safe Mode**, **Feature Flags**, **Self-Diagnostics**, **Resource Manager** | Phase 6 (Safe Mode earlier if cheap) |
| **Secret management**, **supply-chain security** | Phase 0 for secrets; Phase 7 for supply chain |
| **Heron Constitution** | Written now — [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md) |

### Gaps this closes

Four review gaps are closed by Part 4 independently arriving at the same conclusions:
**A7** preview before modify (§11 Dry Run) · **A6** transactions (§12 Transaction Safety Agent) ·
**A10** permission gate placement (§29 Security Boundary — confirms [12 §3](12-security-and-permissions.md)
exactly) · review proposal **B9** offline mode (§31). §28 Golden Test Library adopts the regression
mechanism proposed in [13 §4](13-testing-and-quality.md).

### On the Constitution

Written as [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md), **27 Articles**, reconciled with the
Golden Rules rather than duplicating them:

> Golden Rules are design principles for **people**. The Constitution is the runtime-enforceable subset,
> written as prohibitions an **agent** can obey or violate.

Stated prominently in the document: **a rule is not enforced by being written down.** Every Article that
can be enforced in code is also enforced in code at the permission boundary — which is what §29 requires.
Articles are assembled into agent instructions from that one file via the Prompt/Instruction Registry,
so there are no copies to drift.

### Consequences

- No prior decision is overturned.
- [Q-30](OPEN-QUESTIONS.md) is closed by §24's abstraction layer.
- Two new questions: [Q-34](OPEN-QUESTIONS.md) (trust model unification, see D-14) and
  [Q-35](OPEN-QUESTIONS.md) (confirm the Constitution).
- The roadmap gains named foundation components rather than a general "build a slice first" instruction.

---

## D-14 — Unify six status vocabularies into two orthogonal axes

**Status:** Proposed · **Date:** 2026-08-27 · **Question:** [Q-34](OPEN-QUESTIONS.md)
**Affects:** [24](24-trust-model.md), [09](09-skills-and-fragments.md), [18](18-agent-operating-system.md), [20](20-knowledge-trust-and-conflict.md)

### Context

Across the four documents, **six different vocabularies** describe how much Heron trusts something:
fragment lifecycle (Part 1 §18), knowledge trust (Part 2 §21), agent status (Part 3 §10), skill lifecycle
(Part 3 §44), trust levels (Part 4 §38) and knowledge levels (Part 4 §47).

They overlap, they disagree, and they apply to the same objects. Retrieval ranks by trust; promotion
gates are defined per-vocabulary; and [Golden Rule 6](14-golden-rules.md) is unenforceable when
"experimental" means four different things.

### Decision *(proposed)*

They are not six versions of one idea. They are **two ideas repeatedly collapsed into one linear scale**:

| Axis | Question | Changes? |
|---|---|---|
| **Lifecycle** | *How far has this been proven?* | Advances over time |
| **Source** | *Where did this come from?* | Fixed at creation |

`OFFICIAL` was never a stage past `PROVEN` — it means *shipped by Heron*, a **source**. `UNKNOWN` was
never a stage before `EXPERIMENTAL` — it means *provenance unclear*, also a source. That conflation is
why the vocabularies kept multiplying.

**Axis 1 — Lifecycle**, one vocabulary for fragments, skills, capabilities and agents:

```text
DISCOVERED -> DRAFT -> TESTING -> VALIDATED -> SHADOW -> PROVEN -> PRODUCTION -> DEPRECATED -> ARCHIVED
```

**Axis 2 — Source:** `OFFICIAL` · `COMPANY` · `PROJECT` · `USER` · `COMMUNITY` · `IMPORTED` · `UNKNOWN`

**Part 4 §47's four levels are kept** as a derived band over lifecycle, used for ranking and for talking
to users — and they do real work: L3 may `MODIFY` only with an accepted preview; L4 may `MODIFY`
unattended. That is proposed [Golden Rule 17](14-golden-rules.md) made mechanical.

### Consequences

- Nothing is lost. Every distinction any of the six drew remains expressible — several more precisely,
  since an object can now be `PROVEN` **and** `COMMUNITY`, which no single scale could express.
- `SHADOW` becomes available to fragments, not only agents — a fragment can run in parallel with the
  production one, results compared, output discarded. The cheapest way to earn `PROVEN` without risk.
- Version compatibility, scope and level become **hard filters before ranking**, not weights. For
  anything that writes to a model, only that is acceptable.

Full proposal: [24 — The Unified Trust Model](24-trust-model.md).

---

*Add new decisions below as they are made.*
