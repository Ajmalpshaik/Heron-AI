# Decision Log

> Every architectural decision that has been **made**, with the reasoning behind it.
> Answers from [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are promoted here once settled.
>
> This log is append-only. A decision that is later reversed gets a **new entry** that supersedes
> the old one — the original stays, so the reasoning history is never lost.
> This mirrors Golden Rule 4: never destroy a working record.

---

## ⏳ Five of these get one more pass, at the PC

**Ajmal's instruction, 2026-08-28:** *"We will do this after we finalize one more time ... when I am at
the PC, we will do it one more time. Now we just recorded, but we will do it one more time."*

**D-23, D-24, D-25, D-26 and D-27** — and the six questions they answer (Q-10, Q-11, Q-12, Q-15, Q-16,
Q-40) — were all settled in a single conversation on **2026-08-28**, from a phone, with no Revit and no
model open. They are **Accepted and are being built on**: work does not stop waiting for the review, and
nothing here is provisional in the sense of being ignorable.

What they have not had is **him sitting in front of the thing they are about to shape.** Q-12 alone moved
three times in that one conversation, each time looser, because a rule about what may leave a machine
reads differently in the abstract than it does with a client's model open. That is the argument for the
pass, and it is a good one.

**The review is not a re-vote.** It is: read the five back, confirm each still says what he meant, and
**fill in the detail that was deliberately left out** — his words, *"including deciding what details we
need to go with"*. Several were taken at the level of a principle and will need numbers, formats and
limits before anything is built on them.

**When:** at the PC, alongside the [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) run, and **before Phase 2
work begins.** A decision reviewed after the code is written is a decision that will be defended rather
than examined.

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
| [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour) | Field notes authoritative on bridge behaviour | ✅ Accepted |
| [D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope) | Live session list; freeze out of scope | ✅ Accepted |
| [D-17](#d-17--runtime-state-is-machine-local-not-roaming) | Runtime state is machine-local, not roaming | ✅ Accepted |
| [D-18](#d-18--the-transaction-agent-belongs-to-step-6-not-step-2) | The Transaction Agent belongs to Step 6, not Step 2 | ✅ Accepted |
| [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) | Writing is off by default until the write path has met a real Revit | ✅ Accepted |
| [D-20](#d-20--millimetres-to-feet-is-arithmetic-not-unitutils) | Millimetres to feet is arithmetic, not UnitUtils | ✅ Accepted |
| [D-21](#d-21--failure-analysis-is-a-table-not-a-model-call) | Failure analysis is a table, not a model call | ✅ Accepted |
| [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over) | A second chat is refused, not allowed to take over | ✅ Accepted |
| [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) | The knowledge store is SQLite, one file per scope | ✅ Accepted · ⏳ one pass at the PC |
| [D-24](#d-24--embeddings-are-computed-locally-by-default) | Embeddings are computed locally by default | ✅ Accepted · ⏳ one pass at the PC |
| [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) | The existing libraries are studied and re-authored, never imported | ✅ Accepted · ⏳ one pass at the PC |
| [D-26](#d-26--the-model-file-is-never-uploaded) | The model file is never uploaded | ✅ Accepted · ⏳ one pass at the PC |
| [D-27](#d-27--one-voice-and-the-answers-shape-follows-the-questions-shape) | One voice, and the answer's shape follows the question's shape | ✅ Accepted · ⏳ one pass at the PC |

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
| Earlier brain work | Reference for the brain / knowledge layer |
| Earlier connector work | Reference for the Revit bridge — behaviour proven live, see [field notes](00e-field-notes-proven-bridge.md) |
| `AJ-Tools`, `PyRevit-Tools`, `AEB-Tools` | Existing Revit tooling — candidates for the first knowledge import |

**Plan:** take the ideas from the earlier brain and connector work, upgrade them, and reshape them to
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

*(Written when three parts existed. Part 4 followed — see [D-13](#d-13--adopt-additional-requirements-part-4--kernel-workflow-engine-constitution) — and field notes after that, see [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour). Part 3 remains authoritative on the rule set.)*

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
  *(Two further rules were proposed later from field evidence — 20 and 21. **All six were accepted on
  2026-08-28**, so the total is now 21 official and none proposed — see [Q-19](OPEN-QUESTIONS.md). Also
  [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour).)*
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

Written as [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md), **30 Articles**, reconciled with the
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

## D-15 — Adopt the field notes as authoritative on bridge behaviour

**Status:** Accepted · **Date:** 2026-08-27
**Affects:** [25](25-multi-session-and-binding.md), [03](03-heron-revit.md), [04](04-heron-mcp.md), [14](14-golden-rules.md), [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md)

### Context

The owner provided [field notes](00e-field-notes-proven-bridge.md) describing **behaviour already proven
to work** in his earlier Revit bridge, observed live on 2026-08-20. This is a different class of input
from the four specification documents: those describe what Heron *should* be, this describes what a
running system *does*.

### Decision

**Where the field notes disagree with a specification document, the field notes win.**

Observed behaviour outranks designed behaviour. The specifications are hypotheses; this is evidence.

### What the field notes settle

| Finding | Effect |
|---|---|
| **Per-process pipes work; one shared pipe does not** — before 2026-08-20 the second Revit simply refused to start | [D-02](#d-02--mcp--add-in-transport-named-pipes) confirmed by a real failure, not a hypothetical |
| **The AI's script runs on the thread that draws the screen; Revit is genuinely frozen while it runs; no add-in can change that** | **Confirms the Revit API threading constraint from the field.** Absent from all four specifications, present in the working code. Validates [D-09](#d-09--revit-thread-marshalling--externalevent) |
| **If the user is mid-command the AI cannot interrupt — it waits** | "Revit is busy" is a normal state, not an error |
| **The session list shows a stale document name** | Identity is the **PID**. Never the document name |
| **One Revit can hold several projects open; commands land on whichever window is in front** | The most dangerous finding — see below |

### Adopted, closing a real gap in [D-02](#d-02--mcp--add-in-transport-named-pipes)

D-02 settled the transport but never said how the client **finds** the pipes. The field notes answer it:
a **discovery directory**, one JSON file per live bridge, named by PID —
`%LOCALAPPDATA%\Heron\bridges\<pid>.json`.

Two rules the notes make necessary: the discovery file **must not carry the document name** (that is
exactly what produced the stale-name trap), and a file is **not proof the bridge is alive** — Revit
crashes without cleaning up, so the client verifies before listing.

### Two new proposed Golden Rules

Both come from observed failure modes, not from review:

- **Rule 20 — bind the document, not just the session.** One Revit can hold several projects, and the
  active one changes when the user clicks. A write pins its target document by identity and verifies it
  at every step. Every step of the failure is individually correct and the change still lands in the
  wrong building.
- **Rule 21 — re-read before acting; a preview expires.** *"The real danger is not the freeze, it is the
  stale read."* Before executing an accepted preview, re-count; if the number changed, stop.

### One change to the proven behaviour

Today two chats on the same Revit **fight** — last speaker takes over and chops the running job.
Harmless for reads; not acceptable for a `MODIFY` mid-transaction.

**Adopted: a lease.** A second chat is refused with *"Revit 24312 is in use by another session"* rather
than taking over. A lease must never block a rollback — cleanup always wins. Tracked as
[Q-36](OPEN-QUESTIONS.md).

### Consequences

- The bridge problem is not only solved but **debugged**. The multi-instance failure was found and fixed
  in the field; Heron does not repeat that.
- Proposed Golden Rules rise from four to six.
- Document pinning becomes Phase 0 scope, not a later refinement — it is cheap now and a silent
  model-damage hazard if deferred.

---

## D-16 — The session list is built live, and the Revit freeze is out of scope

**Status:** Accepted · **Date:** 2026-08-27 · **Source:** [field notes addendum](00e-field-notes-proven-bridge.md)
**Affects:** [25 2a, 6a](25-multi-session-and-binding.md), [ROADMAP](ROADMAP.md), [Q-36](OPEN-QUESTIONS.md)

### Context

A second field note corrected an assumption in the first analysis. The stale document name is not a
field-level bug — **the entire session list is a snapshot taken at connect time and never updated.**

> *"You close BL006A, open BL003A, and the list still says BL006A. This actually happened on 20 Aug.
> You'd be picking from a list that lies to you — at exactly the moment where being wrong is most
> expensive."*

### Decision 1 — static facts in the file, dynamic facts on demand

The discovery file is an **address book**, not a status report.

| Kind | Where | Examples |
|---|---|---|
| **Static** — fixed for the process lifetime | discovery file | `pid`, `pipeName`, `revitVersion`, `addinVersion`, `protocolVersion`, `startedAt` |
| **Dynamic** — can change any moment | **queried live, every time the list is built** | open documents, active document, lease state, health |

Building the list costs one parallel round trip per bridge — milliseconds — and non-responding bridges
are dropped and their stale files removed. This fixes the whole class of staleness rather than one field.

### Decision 2 — the user never sees a process number

> *"Stop showing you process numbers like `39344`."*

```text
1) Revit 2024 — Tower A     (free)
2) Revit 2020 — Podium      (in use)
```

**This corrects an error in D-15**, which recorded *"identity is the PID"* without qualifying the
audience. Both are true, of different audiences:

- **Internally** — binding, leases, pipe names, audit log — the **PID**, never the document name.
- **To the user** — Revit version, project, availability; chosen by list number.

A process number is meaningless to a BIM modeller. Asking them to read one is a small violation of
[Golden Rule 1](14-golden-rules.md).

### Decision 3 — availability must be shown, which requires the lease

> *"The list doesn't say which Revit another chat is already using. Nothing shows it. That's why you have
> to tell me 'don't go to Revit, another session is running' — the information exists in Revit, it is just
> never written down where I can see it."*

That standing rule is **a human being used as a lock** — the user manually carrying state the machine
already has and never surfaces.

The lease ([Q-36](OPEN-QUESTIONS.md)) is therefore not only a safety mechanism; it is the data that makes
`(free)` / `(in use)` truthful. Without it there is no honest availability column, and the user goes on
being the lock.

### Decision 4 — the Revit freeze is out of scope. Settled.

> *"Making Revit not freeze can't be done… The second-Revit answer is the real one."*

Heron does **not** attempt to work around Revit's single-threaded design. It is a large amount of work
against the API's fundamental shape, with a poor success rate, and it would stay fragile across eight
Revit versions.

What Heron does instead: keep jobs short · show a banner · wait rather than interrupt a mid-command user ·
chunk and yield on long jobs · and tell the user plainly that **real side-by-side work means opening a
second Revit**.

Recorded so this is not re-opened later.

### Consequences

- Live session listing joins Phase 0 scope.
- [Q-36](OPEN-QUESTIONS.md) is strengthened — the lease now has two justifications, safety and honesty.
- No new open questions.

---

## D-17 — Runtime state is machine-local, not roaming

**Status:** Accepted · **Date:** 2026-08-27 · **Found during:** Step 1 implementation
**Affects:** `HeronPaths`, [25 §2](25-multi-session-and-binding.md), [06 §2](06-heron-platform.md)

### Context

The discovery directory was specified as `%APPDATA%\Heron\bridges`. Writing `HeronPaths` for Step 1
forced the question of which of the three storage classes it actually belongs to.

### Decision

**Bridge discovery files and logs are DERIVED, and live under `%LOCALAPPDATA%`.**

`%APPDATA%` (Roaming) **synchronises between machines** in a domain environment — which is precisely
where Heron's users work. A discovery file announcing process 24156 would follow the user to another PC
where that process does not exist.

The system would self-heal, because a client verifies a bridge answers before trusting its file
([25 §2](25-multi-session-and-binding.md)). But it is avoidable noise, and it puts machine state in a
place that means "follows the person".

| Class | Location | Contents |
|---|---|---|
| **Product** | install directory | assemblies. Replaced on update |
| **Data** | `%APPDATA%\Heron` | config, **audit log**, memory, knowledge. Roams. Never touched by an update |
| **Derived** | `%LOCALAPPDATA%\Heron` | bridges, logs, caches, indexes. Machine-local, rebuildable, safe to delete |

### Consequences

- Preferences and learned skills follow the user between machines. A live process id does not.
- The **audit log stays under Data**, deliberately. It is evidence ([Golden Rule 14](14-golden-rules.md))
  and must survive a cache wipe — which is the distinction the three classes exist to make.
- `HeronPaths.IsSafeToDelete` returns false for anything under Data, so a cleanup or an update cannot
  reach the user's own knowledge.

---

## D-18 — The Transaction Agent belongs to Step 6, not Step 2

**Status:** Accepted · **Date:** 2026-08-28 · **Found during:** Step 2 implementation

### Context

[The registry](28-agent-registry.md) assigned `HERON-REVIT-TRN-005`, the Revit Transaction Agent, to
Step 2. It was the only agent in that step carrying **MODIFY** risk — and Step 2 does not write. It
counts elements.

[The build order](27-build-order.md) is explicit about why that matters:

> A write path built before its safety path is a write path that will ship without one.

Step 6 exists precisely to build the rails *first* — the named transaction group, the preview, the
re-count before executing, document pinning, the permission gate, the emergency stop — and only then
the first write. Creating the transaction machinery four steps early puts the mechanism in place long
before anything that makes using it safe, and leaves it sitting there available.

### Decision

`HERON-REVIT-TRN-005` moves to **Step 6**, alongside the safety rails it cannot be used without.

Step 2 therefore builds two agents, not three: `HERON-REVIT-APP-003` and `HERON-REVIT-DOC-004`. Both
are READ.

### Consequences

- Everything through Step 5 stays read-only **by construction**, not by discipline. There is no
  transaction code to reach for.
- The rule generalises, and is recorded in the conventions skill: a read-only operation opens no
  transaction, and none is created "for later".
- Step 6 gains one agent. Its ordering does not change — the rails already come before the write.

---

## D-19 — Writing is off by default until the write path has met a real Revit

**Status:** Accepted · **Date:** 2026-08-27 · **Found during:** Step 6 implementation

### Context

Until Step 6, Heron was read-only **by construction**: there was no transaction code in the repository,
so the guarantee needed no trust and no configuration. [D-18](#d-18--the-transaction-agent-belongs-to-step-6-not-step-2)
is the decision that kept it that way.

Step 6 ends that, and it ended it under the worst available conditions. The write path was written on a
machine with **no Revit, no Windows and no .NET SDK** — it has never been compiled, never loaded, and has
never moved anything. The compiler has not read it.

Golden Rule 18 already covers the general case: *generated code never touches a live model on its first
run*. This is that rule arriving at the most consequential file in the repository.

### Decision

`HeronPermissions` refuses anything at `MODIFY` or above unless **`write.enabled = true`** is set in the
user's config. It defaults to **false**, and the refusal names the setting and the file so the user is
not left hunting.

The shape is borrowed from a property Heron already has and has already proven: **a Revit that was never
connected is invisible**, because `bridge.autoConnect` defaults to false. Nothing reaches a model the
user did not offer up. This is the same sentence applied to writing rather than to connecting.

### Consequences

- The read-only guarantee is **weaker than it was**, and that must be said plainly rather than presented
  as an improvement. It moved from "there is no code to do this" to "the code is switched off". The first
  needs no trust; the second does.
- The default flips to `true` **only** when [HANDOVER §6](../HANDOVER.md#6-the-return-to-the-machine-checklist)
  has been walked end to end against a real model — not when the code merely compiles.
- Anyone reading `HeronPermissions` finds the reasoning in the file, not only here. The comment saying
  why it is off is written to be **deleted** once the path is proven, so a stale justification cannot sit
  there looking current.

---

## D-20 — Millimetres to feet is arithmetic, not UnitUtils

**Status:** Accepted · **Date:** 2026-08-27 · **Found during:** Step 6 implementation

### Context

Step 6 needs the user's millimetres as Revit's internal length unit. The obvious route is `UnitUtils`,
and the obvious route has a hole in it across the range this repository supports.

The units API was **replaced at Revit 2021**: `DisplayUnitType.DUT_MILLIMETERS` became
`UnitTypeId.Millimeters`, the old overloads were deprecated and later removed. Heron builds 2020 through
2027 from one codebase, so `UnitUtils` would need a compile symbol around it — and
`Directory.Build.props` defines `REVIT2024_OR_GREATER` upward but has **no `REVIT2021_OR_GREATER`** to
hang it on. The symbol would have to be added to guard a conversion whose answer never changes.

The failure mode is not hypothetical. A unit call that a newer Revit rejects outright is a bug this
work has already met elsewhere, and it hid for months because nothing exercised it.

### Decision

`HeronUnits` converts by the fixed ratio. Revit stores lengths internally in decimal **feet** in every
supported release, and the international foot is **exactly 304.8 mm** by definition. The conversion has
no version, no locale and no project setting in it.

The boundary, for anyone extending it: a conversion with a **fixed ratio** (length, angle) belongs in
`HeronUnits`. A conversion that depends on what the project displays, or on a unit family Heron does not
define, genuinely needs the API — and needs the version split that comes with it.

### Consequences

- No compile symbol, no version branch, and nothing for Autodesk to move underneath it.
- It lives in `platform/Heron.Core`, not in `revit/`, because it has no Revit reference. The layering
  checker enforces that on its own.
- The ratio is **exact and must never be "improved"** to more decimal places. It is a definition, not a
  measurement.
- A ceiling of 100 km and a rejection of NaN and infinity sit alongside it. Those are not unit concerns;
  they are there because this is the last place a malformed number can be stopped before it reaches a
  transaction on somebody's building.

---

## D-21 — Failure analysis is a table, not a model call

**Status:** Accepted · **Date:** 2026-08-28 · **Found during:** Step 6 implementation
**Supersedes:** the **T2** tier given to `HERON-ORC-FAIL-004` in [28](28-agent-registry.md)

### Context

The registry assigned the Failure Analysis Agent **T2** — one scoped model call. Building it made two
things obvious.

**Heron's failures are its own bounded set.** They do not arrive as arbitrary text from an unknown
system; they arrive as error codes this repository defines, from a bridge this repository wrote —
`revit_busy`, `preview_expired`, `document_closed`, `unknown_outcome` and about twenty more. Classifying
a known set is a lookup. A model call would be asked to re-derive, each time and at cost, an answer that
is already written down.

**And the one answer that must never be wrong is exactly the one a model should not be asked for.** The
question is *"did this reach the model, and can we know?"* — and when the answer is *"it was sent and the
answer was lost"*, the only safe next step is a person looking at the model. A table gives that answer
identically every time. A model call gives it *almost* every time, and the failure mode is retrying a
move that already happened.

### Decision

`HERON-ORC-FAIL-004` is **T1** — deterministic, no model call — and it **fails closed**: an error code
it has never seen, on an operation that can write, is classified as *unknown outcome*, not as
retryable. A future operation added by someone who never read the file gets the safe answer by default
rather than the convenient one.

The registry's tier is corrected, along with the department and platform totals that quoted it
(167 T1 · 63 T2 · 20 T3).

### Consequences

- It is testable without Revit and without a model, and it is:
  `tests/test_failure_analysis.py`, 30 checks. The central one is a property asserted over **every**
  known code — no write failure may come back retryable unless the request provably never ran.
- The judgement a model *would* genuinely add — reading an unfamiliar Revit exception message and
  guessing what it means — is not needed at this layer. When it is, it belongs in a separate agent that
  this one can defer to, not inside the classification that guards the write.
- The general rule: **if the set of inputs is one Heron itself defines, the agent that reads them is
  T1.** T2 is for text Heron did not write.

---

## D-22 — A second chat is refused, not allowed to take over

**Status:** Accepted · **Date:** 2026-08-28 · **Found during:** Step 6, auditing for gaps
**Changes proven behaviour.** Supersedes *"last speaker wins"* as the rule that decides who may work.

### Context

[Step 5's own *"Not yet"*](27-build-order.md) defers three things to *"Phase 1 with writes"* — the lease,
the `(free)`/`(in use)` column, and full document pinning. Step 6 **is** that write. Document pinning
was built; the lease was not, and an audit found it missing rather than anybody noticing at the time.

Until now, two chats on one Revit **fought**: whichever spoke last took the pipe and cut the other off
mid-job. [docs/25](25-multi-session-and-binding.md) is blunt that this is tolerable only while Heron
reads — *"chopping a read is harmless. Chopping a MODIFY mid-transaction is not"* — and weighs three
options, rejecting a queue because *"an invisible queue means a command runs minutes later against a
model that has since changed."*

There was a second cost, and the owner named it himself: the list never showed which Revit another chat
was already using. His standing workaround was to say *"don't go to Revit, another session is
running"* — **a person being used as a lock**, because the information existed and was never written
down anywhere visible.

### Decision

`HeronLease` — one lease per **Revit process**, held by one chat, short and renewable.

- A second chat is **refused** with a message saying what is happening and when it clears. It is no
  longer silently cut off.
- Scoped to the **process, not the document**: the contention is at the pipe, so two chats on one Revit
  collide even when each is discussing a different project. Two models open does not make two sessions.
- **`ping` and `info` are exempt.** Asking who holds a Revit must never be the act of claiming it —
  without that exemption the honest picker this enables would be impossible.
- Renewed by every request, so an active chat never loses its hold; a chat that goes quiet releases it
  by lapsing. Cleared outright when the user presses the Heron button.
- **It cannot block a rollback**, as docs/25 requires. Not by a special case: the lease is checked once
  when a request arrives, and a rollback happens *inside* a request already admitted. Cleanup never
  asks permission.

**The transport half is unchanged.** A new connection still displaces the older pipe — that is proven
behaviour and it still happens. What changed is that taking the pipe is no longer the same thing as
taking the right to use it.

**Protocol raised to 2.** A request now carries a `client` id, because the per-Revit token cannot tell
one chat from another — every chat reading a Revit's discovery file reads the *same* token. An older
client sending no id would be refused as anonymous, which is correct but reads as a fault; the version
bump turns that into the honest message Heron already has for it — *restart that Revit to finish
updating*.

### Consequences

- **This changes behaviour proven in Step 1.** "Newest connection wins" now describes the pipe only.
  Every document stating it as the whole rule has been corrected rather than left to be discovered.
- The picker finally answers *"which of these is free?"* — the missing data, and the end of a human
  being used as a lock.
- `bridge.leaseMinutes` (default 5) is longer than `bridge.idleReleaseMinutes` on purpose: the pipe
  going quiet does not mean the chat has gone.
- Five minutes is a judgement, not a measurement. Too long and a dead chat holds a Revit; too short and
  a chat loses its hold between two of the user's own messages — the takeover this prevents, arriving
  on a timer instead. It is configurable so the number can be argued with.
- `session_in_use` classifies as **NEVER_RAN**, not unknown: the refusal happens before anything reaches
  the model, so the user must never be sent to inspect a model nothing touched.

---

*Add new decisions below as they are made.*

---

## D-23 — The knowledge store is SQLite, one file per scope

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-10](OPEN-QUESTIONS.md)

### Context

Phase 2 has to find the right fragment from a sentence somebody typed, using three kinds of match at
once: the exact phrase, the keywords, and the meaning. The choice is between a dedicated vector database
— a server to install, run and keep running — and SQLite with its extensions.

**The installation constraint decides it, and it is already proven rather than assumed.** Heron installs
per-user with no administrator rights ([D-01](#d-01--heron-runs-as-a-claude-code-plugin),
[07](07-installation-and-update.md)), and Phase 0 demonstrated that end to end on a real machine. A store
that needs a service installed breaks that on exactly the machines Heron is for: locked-down corporate
laptops where the user cannot install a service and will not be given permission to.

**And Golden Rule 5 asks for scope separation *physically*.** In a server, a scope is a column and
separation is a `WHERE` clause somebody can forget. As one file per scope, it is the filesystem, and
forgetting it is not possible.

### Decision

**SQLite, one file per knowledge scope.** FTS5 for keywords, `sqlite-vec` for meaning, ordinary SQL for
the exact-match short circuit — all three stages in one engine, one file, no server and no daemon.

### Consequences

- Backing up a scope is copying a file. Deleting one is deleting a file. Sending one to somebody is
  sending a file.
- Scope separation is enforced by the filesystem rather than by a query, which is what Golden Rule 5
  actually asks for.
- Corruption is contained: one damaged scope does not take the others with it.
- **Revisit only on measurement, never on feeling.** The retrieval interface is the seam to swap behind
  if a scope ever genuinely outgrows this. "It might get slow" is not evidence; a measured query time
  against a real scope is.

---

## D-24 — Embeddings are computed locally by default

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-11](OPEN-QUESTIONS.md)
**Amended the same day by [D-26](#d-26--the-model-file-is-never-uploaded):** the cloud opt-in below is
**withdrawn**. Note that D-26 was itself refined afterwards and its confidentiality argument narrowed —
this decision now stands on its *re-indexing* reason, which did not change. See D-26's consequences.

### Context

Searching by meaning needs embeddings, and they can be computed on the machine or bought from an API.
Three things point the same way.

**Heron reads real projects.** Room names, client names, drawing numbers, revision comments. Sending
that to a third party is a decision with contractual weight in the work this is built for, and it has
not been made.

**Re-indexing has to be free, or it stops happening.** The knowledge base grows every working day. Put a
per-call cost on rebuilding the index and rebuilding becomes something to avoid — and an index nobody
rebuilds is an index that quietly stops matching what is on disk. That failure has a long history in
work of this kind and it is not a hypothetical one.

**It has to work with no connection.** Site visits, locked-down networks, a laptop on a plane.

### Decision

~~**Local by default.** Cloud embeddings are opt-in per scope, off unless deliberately switched on, and
the setting has to name what would leave the machine rather than reading as a quality slider.~~

**Superseded within hours by [D-26](#d-26--the-model-file-is-never-uploaded): embeddings are computed
locally, full stop.** There is no opt-in and no setting. The reasoning below still holds and is
kept because it is now the *second* reason rather than the only one — a rule with two independent
justifications is worth more than a rule with one.

### Consequences

- Installing Heron needs no API key and no account.
- Matching quality is somewhat below the best cloud embedding. Accepted: the hybrid of keyword and
  meaning recovers much of it, and a wrong fragment is visible in a way that a leaked room schedule
  is not.
- **This is an engineering default, not the confidentiality policy.** [Q-12](OPEN-QUESTIONS.md) — what
  may be sent to a model provider, from which projects — is a contractual question and remains **open
  and Ajmal's to answer**. Local-by-default is the setting that is safe to hold while it is open, and
  is deliberately reversible once it is closed.

---

## D-25 — The existing libraries are studied and re-authored, never imported

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-16](OPEN-QUESTIONS.md)
**Supersedes** the Phase 2 roadmap item that read *"import the existing libraries, with duplicate
detection"*.

### Context

Q-16 asked **which** of the existing libraries to import first, and the roadmap described an import
pipeline with duplicate detection. Ajmal's answer, 2026-08-28, changes the question rather than
picking from the list:

> *"You can take all of them ... but use them as a reference only. You have to write it cleanly on our
> side because we have a splitting rule and everything. In our Heron AI, it should be written completely
> from scratch. You can refer to everything and study the remaining repos, but study them hard. There is
> no need to copy-paste. Study each and every line, word by word, and create it as a new file."*

So the answer to *"which first"* is **all of them, and none of them** — all are in scope to be read, none
is imported.

### Decision

Every existing library is **reference material**. Nothing is copied. A capability that earns a place in
Heron is **re-authored from scratch here**, in Heron's shape, obeying Heron's splitting and metadata
rules, and verified in Heron.

### Consequences

- **There is no bulk import to build.** The Phase 2 work is a study-and-re-author process, and the
  duplicate detection that an import would have needed largely disappears with it: reading five
  variations of the same job produces **one** Heron fragment, because a person decided they were the
  same job.
- **The unit of work is a capability, not a file.** A count of files in a reference library is not a
  count of fragments Heron will end up with, and should never be quoted as a target.
- It is slower per fragment, and that is the point. A copied fragment carries assumptions from where it
  came from — hard-coded paths, another project's naming, conventions nobody here can see — and those
  are invisible precisely because the code looks finished.
- **A fragment proven elsewhere is not proven in Heron.** Re-authored work starts at DRAFT and earns its
  status through Heron's own checks, whatever status it held in the library it was read from.
- It reinforces what was already true: none of those projects' names, branding or dependencies come
  across ([HANDOVER §7](../HANDOVER.md)). This decision is the same rule applied to substance rather
  than to labels.

---

## D-26 — The model file is never uploaded

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-12](OPEN-QUESTIONS.md)

> **This decision was refined three times on the day it was written, each time in the same direction:
> from *nothing may travel* toward *the file may not travel*. The rule below is the final one. It is
> narrower than the first two, and commits from that day quote the earlier framings — so the movement is
> recorded here rather than quietly overwritten, because a reader needs to know which version won.**

### Context

[12 §4](12-security-and-permissions.md) offered three positions — cloud only, local only, hybrid per
project. The first answer taken was the strictest, and it was stricter than intended:

1. *"All project content ... must remain on local machines."* → recorded as: nothing leaves.
2. *"How many ducts are there? That is no issue. But the entire model, it should not go to the cloud like
   that."* → refined to: bulk versus answer.
3. And then, plainly:

> *"Any project name, data, typing, or content being in the cloud is not an issue. The ideas, engineering
> ideas, file names, content names, and coding are all fine to go to the cloud. The main thing is that we
> should not upload the model itself, specifically the RVT or RFA files, as they will be too heavy ...
> Do not push the models."*

### Decision

**The model file is never uploaded. Everything else about the work is fine.**

| Never leaves the machine | Fine in the conversation |
|---|---|
| The `.rvt` and `.rfa` files themselves | Project names, file names, content names |
| Family and project templates | Element data — counts, sizes, parameters |
| Any Revit binary | Engineering ideas, reasoning, and code |

**And project knowledge stays segregated** — Ajmal's third point in the same message: *"project-based
knowledge must be kept segregated and separated."* Project A's knowledge does not leak into Project B.

That was already the design rather than a new requirement: Golden Rule 5 enforces **one store per
scope**, [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) makes that literally one file per
scope, and [22 §9](22-users-modes-and-extensibility.md) already stated that Project B does not inherit
Project A's decisions. Golden Rule 5's *wording* named only personal and company; it has been broadened to
name project too, because the mechanism always covered it and the rule should say what it does.

### Why it lands here rather than at the strict end

- **Heron never needs to upload a model.** The add-in reads it in place, in the Revit that has it open.
  No feature wants the file, so this rule costs nothing to keep — which is the best kind of rule.
- **Size is a real reason by itself**, and it is the one Ajmal gave: a `.rvt` is hundreds of megabytes.
  Uploading one is slow, expensive and pointless when the answer is a number.
- **The looser half is simply what using an assistant means.** A count, a size, a room name — that is the
  work. A rule forbidding it would forbid Heron.

### Consequences

- **[D-24](#d-24--embeddings-are-computed-locally-by-default) loses one of its two reasons and stands on
  the other.** Local embeddings were argued from confidentiality *and* from re-indexing having to stay
  free. The confidentiality half has now weakened. The re-indexing half has not: put a per-call cost on
  rebuilding and rebuilding stops happening, and an index nobody rebuilds quietly stops matching what is
  on disk. **Local stays** — written down so that nobody later finds a decision resting on an argument
  that had silently stopped applying.
- **The tool rule from the second refinement survives unchanged, and is now better motivated:** a tool
  answers a question and never returns the model. What made that right was never only privacy — it is
  also that no useful answer is 5,844 rows.
- **What a client can be told is short and true:** *Heron never uploads your models. What you ask it
  about goes to the assistant, the same as any AI tool you already use.*
- The `PUBLISH` boundary is untouched: a knowledge scope still leaves only by a deliberate action
  ([12 §2](12-security-and-permissions.md)).

---

## D-27 — One voice, and the answer's shape follows the question's shape

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-15](OPEN-QUESTIONS.md)
**Supersedes** the *"infer a default, display it, let the user pin it"* recommendation in
[01 §4](01-vision-and-principles.md) and the two-persona table in [22 §2](22-users-modes-and-extensibility.md).

### Context

Q-15 asked whether persona is automatic, manual or both, and both documents recommended inferring one,
showing it, and letting the user pin it.

Two assistants doing this same job every day for months were read for this question, and they settle it
in a direction nobody was designing towards: **neither of them switches persona at all**, and neither has
ever needed to. What they do instead is not a compromise between automatic and manual — it is a different
axis.

**The two things being conflated are not alike.**

- **Inferring a persona is guessing about a person.** It is wrong some of the time, it is invisible when
  it is wrong, and it makes the same question get two different answers on two days. [22 §2](22-users-modes-and-extensibility.md)
  already warned that this *"reads as unreliability rather than intelligence"* — which is right, and is
  an argument against inferring it at all rather than an argument for showing the guess.
- **Inferring the shape of an answer is reading the request.** *"How many"* is not a guess. It gives the
  same answer for the same input every time, and when the user disagrees they can see exactly why and
  say so.

And the persona axis has less left in it than the table suggests: its *Developer* column is really
Developer **Mode**, which [22 §3](22-users-modes-and-extensibility.md) already rules must be **granted,
not inferred**, because otherwise a user could talk their way into it. Take the answer-shape rules out of
persona and put mode where it belongs, and there is nothing left for a persona setting to do.

### Decision

**Heron has one voice: plain, non-developer language, always.** Persona is not inferred, not displayed
and not pinned — it does not exist as a setting.

**What varies is the shape of the answer, chosen from the shape of the request:**

| The request | The answer |
|---|---|
| A count | The number. One line, nothing else |
| A breakdown | A schedule-style table, sorted the way a schedule sorts, not by quantity |
| A narrowed set — *"the 300×300 ones"* | The items themselves, with their ids. A narrowed request is nearly always the setup for the next step, and ids are what make that step possible without re-filtering |
| A finished piece of work | A short close: what was done, what was actually verified, what still needs deciding |
| Two or more numbers worth comparing | A picture, without being asked for one |

**Wording still adapts inside Developer Mode**, which is granted rather than guessed — so the one place
persona-like variation survives is the one place it is safe.

### Consequences

- **Nothing has to detect who is talking.** A whole class of *"why did it answer differently today"*
  stops being possible rather than being made visible.
- **These are defaults, not law.** Heron is for everyone ([Q-22](OPEN-QUESTIONS.md)), so one person's
  preferences cannot be the product's. They live in a small file the user is expected to edit, with a
  dated line recording each change — **a format correction has to cost one line to record, or it will
  not get recorded**, and reply format is the thing users correct most.
- The recommendation in [01 §4](01-vision-and-principles.md) and the table in
  [22 §2](22-users-modes-and-extensibility.md) are superseded; both now point here.
- **The method is worth more than the answer.** An hour reading two systems that already do the job
  settled a question that designing had left open for weeks — [D-15](#d-15--where-the-field-notes-disagree-with-a-specification-the-field-notes-win)
  again, and this time the field note was somebody else's working habit rather than a Revit behaviour.
