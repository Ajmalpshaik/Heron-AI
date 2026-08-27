# Decision Log

> Every architectural decision that has been **made**, with the reasoning behind it.
> Answers from [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are promoted here once settled.
>
> This log is append-only. A decision that is later reversed gets a **new entry** that supersedes
> the old one — the original stays, so the reasoning history is never lost.
> This mirrors Golden Rule 3: never destroy a working record.

---

## Status summary

| # | Decision | Status |
|---|---|---|
| [D-00](#d-00--documentation-first-no-implementation-yet) | Documentation first, no implementation yet | ✅ Accepted |
| [D-01](#d-01--execution-host--claude-code-plugin) | Execution host — Claude Code plugin | ✅ Accepted |
| [D-02](#d-02--mcp--add-in-transport-pending) | MCP ↔ add-in transport | ⏳ Proposed |
| [D-03](#d-03--mcp-tool-granularity-pending) | MCP tool granularity | ⏳ Proposed |
| [D-04](#d-04--generated-code-execution-model-pending) | Generated code execution model | ⏳ Proposed |
| [D-05](#d-05--revit-version-support--2020-to-latest) | Revit version support — 2020 → latest | ✅ Accepted |
| [D-06](#d-06--implementation-languages--c-for-revit-python-for-brain) | Languages — C# for Revit, Python for brain | ✅ Accepted |
| [D-07](#d-07--free-open-source-on-public-github) | Free open source on public GitHub | ✅ Accepted |

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

## D-02 — MCP ↔ add-in transport *(pending)*

**Status:** Proposed · **Question:** [Q-2](OPEN-QUESTIONS.md)

Recommendation: named pipes, add-in as pipe server, pipe name encoding Revit version + PID.
→ [03 §5](03-heron-revit.md)

---

## D-03 — MCP tool granularity *(pending)*

**Status:** Proposed · **Question:** [Q-5](OPEN-QUESTIONS.md)

Recommendation: thick, specific tools mapping one-to-one onto fragments. Generic execute only in
Developer Persona behind `ADMIN`. → [04 §3](04-heron-mcp.md)

---

## D-04 — Generated code execution model *(pending)*

**Status:** Proposed · **Question:** [Q-7](OPEN-QUESTIONS.md)

Recommendation: hybrid — scripting for DRAFT/TESTING, compiled for PRODUCTION, matching the
fragment lifecycle. → [09 §10](09-skills-and-fragments.md)

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

*Add new decisions below as they are made.*
