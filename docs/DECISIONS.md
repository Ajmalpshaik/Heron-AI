# Decision Log

> Every architectural decision that has been **made**, with the reasoning behind it.
> Answers from [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) are promoted here once settled.
>
> This log is append-only. A decision that is later reversed gets a **new entry** that supersedes
> the old one — the original stays, so the reasoning history is never lost.
> This mirrors Golden Rule 4: never destroy a working record.

---

## ✅ The read-back happened — 2026-08-29, and nothing moved

**Ajmal's instruction, 2026-08-28:** *"We will do this after we finalize one more time ... when I am at
the PC, we will do it one more time. Now we just recorded, but we will do it one more time."*

**Done on 2026-08-29, and it covered D-23 to D-43 rather than only the five.** Twenty-one decisions were
read back and twenty-one were confirmed. Three were put to him one at a time because they carried real
consequence, and all three came back unchanged:

| | |
|---|---|
| [**D-33**](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) | The boundary this log itself flagged as **never actually stated by him** — Heron never invents a Revit number and does decide its own code. **Confirmed as written**, so the decision stands rather than moves |
| [**D-26**](#d-26--the-model-file-is-never-uploaded) | The model file never leaves; names, counts, sizes and reasoning are fine. **Confirmed** after moving three times on the day it was written |
| [**D-32**](#d-32--v1-must-be-able-to-change-the-model-and-reading-is-what-gets-used-first) | v1 both reads and writes, reading first, writing off by default. **Confirmed** after being reversed once within the hour |

**It happened in conversation rather than at the PC**, which is recorded rather than smoothed over. What
*at the PC* was for — him sitting with them rather than tapping yes — did happen. What still needs a
screen is [`R1b`](../NEEDS-CHECKING.md): [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes)
stays **Proposed** until he has seen the trust model working with his own fragments in it.

**And it happened AFTER Phase 2 was built, not before**, which was his own override and is weaker than
intended — a decision reviewed once the code exists gets defended rather than examined. Worth recording
what that cost: **nothing measurable.** The two that had been reversed within hours did not move a fourth
time, and no detail was found missing. That is evidence about these particular decisions, not a reason to
review late next time.

**This heading said *"Five of these get one more pass"* while the table below marked NINE** — D-32 to
D-35 were added later and the sentence was not. Nobody noticed because the sentence and the markers were
never read together. All nine are now confirmed, so the discrepancy is closed by the work rather than by
an edit.

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
| [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) | The knowledge store is SQLite, one file per scope | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-24](#d-24--embeddings-are-computed-locally-by-default) | Embeddings are computed locally by default | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) | The existing libraries are studied and re-authored, never imported | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-26](#d-26--the-model-file-is-never-uploaded) | The model file is never uploaded | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-27](#d-27--one-voice-and-the-answers-shape-follows-the-questions-shape) | One voice, and the answer's shape follows the question's shape | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) | Generated code is C#, compiled at run time, in process | ✅ Accepted |
| [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer) | A fragment is a composable piece, not a whole answer | ✅ Accepted |
| [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) | A fragment is promoted by one recorded proof, not a count of runs | ✅ Accepted |
| [D-31](#d-31--product-data-and-derived-are-already-separated-and-the-code-is-the-record) | Product, data and derived are already separated | ✅ Accepted |
| [D-32](#d-32--v1-must-be-able-to-change-the-model-and-reading-is-what-gets-used-first) | v1 must change the model; reading is used first | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) | Heron never assumes an input. It asks — and it asks once | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-34](#d-34--herons-own-wording-is-english-understanding-the-user-is-not-herons-job) | Heron's own wording is English; understanding the user is not Heron's job | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-35](#d-35--a-shared-fragment-may-carry-code-and-an-unapproved-one-is-refused-not-warned-about) | A shared fragment may carry code; an unapproved one is refused, not warned about | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-36](#d-36--no-warranty--the-standard-position-and-it-is-already-in-place-twice) | No warranty — the standard position, already in place twice | ✅ Accepted |
| [D-37](#d-37--the-name-is-heron-ai-and-no-trademark-check-has-been-done) | The name is Heron AI, and no trademark check has been done | ✅ Accepted |
| [D-38](#d-38--github-now-app-store-kept-possible-and-nothing-built-for-it) | GitHub now, App Store kept possible, and nothing built for it | ✅ Accepted |
| [D-39](#d-39--shadow-mode-is-approved-on-an-analysed-disagreement-not-a-count-of-agreements) | Shadow mode is approved on an analysed disagreement, not a count of agreements | ✅ Accepted |
| [D-40](#d-40--the-dependency-graph-is-sqlite-and-an-edge-is-derived-before-it-is-stored) | The dependency graph is SQLite, and an edge is derived before it is stored | ✅ Accepted |
| [D-41](#d-41--single-user-now-company-knowledge-is-a-git-repo-and-the-admin-is-the-reviewer) | Single-user now; company knowledge is a git repo and the admin is the reviewer | ✅ Accepted |
| [D-42](#d-42--the-public-install-command-is-not-settled-the-proven-one-is-setupps1) | The public install command is not settled; the proven one is setup.ps1 | ✅ Accepted |
| [D-43](#d-43--the-constitution-is-accepted-all-30-articles-binding) | The Constitution is accepted — all 30 Articles, binding | ✅ Accepted |
| [D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere) | A re-authored fragment starts unproven in Heron, whatever it was elsewhere | ✅ Accepted |
| [D-45](#d-45--the-library-is-built-out-first-and-proved-in-one-pass-later) | The library is built out first, and proved in one pass later | ✅ Accepted |
| [D-46](#d-46--a-context-fragment-is-consumed-by-the-host-not-by-another-fragment) | A context fragment is consumed by the host, not by another fragment | 🔶 Proposed — needs the owner |

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
  may be sent to a model provider, from which projects — is the contractual question, and this bullet
  said it *"remains open and Ajmal's to answer"* **until 2026-08-29, having been closed the same day it
  was written**: [D-26](#d-26--the-model-file-is-never-uploaded) answers Q-12, and D-24's own header has
  pointed at D-26 all along. A decision amended by another on the same day is exactly where a sentence
  goes stale without anybody noticing, because the amendment is read and the consequences are not.
  Local-by-default stays, now on the re-indexing argument rather than on the confidentiality one.

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

---

## D-28 — Generated code is C#, compiled at run time, in process

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-7a](OPEN-QUESTIONS.md)
**Completes** [D-04](#d-04--generated-code-execution--hybrid), which settled *hybrid* and left the runtime open.

### Context

D-04 settled the shape — scripting while a fragment is DRAFT or TESTING, compiled C# for PRODUCTION — and
left open **which** scripting runtime: pyRevit, IronPython, Python.NET, or Roslyn C# scripting. The
research note favoured pyRevit: a shipping Revit MCP server executes IronPython through pyRevit's Routes
server, it is maintained by somebody else, and Ajmal already knows it.

Reading a system that has been doing exactly this job daily points the other way, for three reasons the
research could not show:

**1. What he already runs is C#, not Python.** That system composes stored C# snippets and compiles them
inside Revit at run time. *"He already knows pyRevit"* is true and is not the relevant fact.

**2. Routes is an HTTP server, and Heron's add-in has no network code at all.** That is not an accident:
[D-02](#d-02--mcp--add-in-transport-named-pipes) made the transport named pipes, per-PID, **local-only by
construction**, and it was verified against the source this session — zero networking types in the add-in
or the bridge. Adopting Routes would put an HTTP listener inside Revit and turn a structural guarantee
into a promise about configuration. It would also re-open the multi-Revit binding problem that per-PID
pipe naming already solved, which is precisely what [Q-37](OPEN-QUESTIONS.md) is asking about.

**3. One language means one compile gate.** Heron already compiles every project against every Revit
release it can reach ([`check-compile.py`](../tools/check-compile.py)) and checks the API surface for the
releases it cannot ([`check-api-surface.py`](../tools/check-api-surface.py)). C# fragments pass through
both. Python fragments would pass through neither — leaving the version boundary uncovered for exactly
the code most likely to be newest, which is the failure this project already hit once this same day.

### Decision

**Roslyn C# scripting, in process, through the existing bridge.** A fragment is C#: composed and compiled
at run time while DRAFT or TESTING, compiled into an assembly for PRODUCTION. That is what D-04's hybrid
always meant, with the runtime now named. **No Python runtime is embedded and no HTTP server enters the
add-in.**

### Consequences

- [Q-37](OPEN-QUESTIONS.md) — *can pyRevit Routes bind a per-process port?* — stops applying to Heron.
  It should be closed as not applicable rather than researched.
- One language, one debugger, one compile gate, for stored and generated code alike.
- **The warning worth more than the decision.** In that same working system, roughly half the C# that
  reaches Revit is *not* a stored fragment — it is generated line by line by the tool layer at run time.
  That half went unchecked for months, and a Revit API call removed after 2020 sat in **eight** places
  with a green compile check the whole time, because the checker only ever read the stored library.
  **Heron will have exactly the same shape**, so its compile gate must cover generated code as well as
  stored fragments — and it must walk **branches, not tools**: in that case three of the eight copies
  appeared only on one filter mode and one only on a numeric branch, so calling every tool once would
  have caught none of them.

---

## D-29 — A fragment is a composable piece, not a whole answer

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-8](OPEN-QUESTIONS.md)

### Context

The proposed definition — **Skill** = what the user can ask for, **Fragment** = how it is done — is right
as far as it goes. The field adds the part that decides the design: **a fragment is not a whole *how*.**

In a library of several hundred working fragments the unit is smaller than a job. A job is *composed*: a
**filter** fragment answers *which elements*, an **action** fragment answers *what to do to them*, and
they are joined. Each declares its contract in its own header — one reads *"assumes `elements` and `sb`
already exist ... NOT STANDALONE"*. Where a job genuinely cannot be composed it is a **recipe**: bespoke,
multi-stage, and named as a third kind rather than allowed to masquerade as a fragment.

**Why the distinction is load-bearing.** Read *"fragment = how it is done"* as one fragment per job and
the library grows one entry per sentence a user might say, nothing is ever reused, and every entry needs
its own proof. Composition is what lets a few hundred fragments cover far more than a few hundred jobs —
*"how many 300×300 VCDs"* and *"list the 300×300 VCDs"* share a filter — and it is what makes each piece
small enough to test on its own.

### Decision

| | |
|---|---|
| **Skill** | What the user can ask for, in BIM language. User-facing. **Confirmed as proposed** |
| **Fragment** | A **composable piece** of the how, with a declared contract: what it needs in scope, what it leaves in scope. Not standalone by default |
| **Recipe** | A bespoke multi-stage job that genuinely cannot be composed. A **third kind**, named, so it cannot quietly become a giant fragment |

### Consequences

- **The contract is data, not prose.** A fragment declares what it needs and what it provides in a form a
  machine can read, because composition then becomes checkable: a filter that provides nothing the action
  needs is a defect a tool can find before Revit does.
- Reuse is the measure. Two questions sharing a filter is the design working.
- **A rising recipe count means composition is failing** and is worth watching as a signal, not accepted
  as growth.

---

## D-30 — A fragment is promoted by one recorded proof, not by a count of runs

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-9](OPEN-QUESTIONS.md)

### Context

Q-9 proposed a count — *how many successful executions, suggest N = 10?*

A working library shows why a count is the wrong gate, and shows it with a real defect. One fragment's
record reads: the level chain never tried `RBS_START_LEVEL_PARAM`, so setting a level filter matched
**zero** ducts **and reported success**.

**A fragment that succeeds while doing nothing passes ten runs. It passes a thousand.** A count measures
that nothing threw, which is not the property anybody cares about.

What caught it was a **comparison** — 3 against 0, side by side. And the strongest records in that library
share a shape: they say what was checked, against what, on what date, and they include a case that should
come back empty. One records that two different mechanisms returned the same five elements, *"so neither
can be quietly wrong on its own"*.

### Decision

A fragment reaches `PRODUCTION` on **one recorded proof against a real model** — dated, naming the model
or the kind of model — containing:

1. **A positive case.** It returns what it should.
2. **A negative case.** It returns *nothing* when it should return nothing. This is the one that catches
   *succeeded and did nothing*, and a proof without it is not a proof.
3. **A second route to the answer where one exists.** Two mechanisms agreeing, or a number the user can
   check by eye.

**Not a count.** Runs after the first add confidence; they are not the gate.

**Who approves:** whoever ran it records it, under their name and the date, not a tick. A company's BIM
lead may approve for that company's own scope — the proof travels with the fragment as evidence, so a
later reader can *judge* it rather than trust it. A community submission meets the same bar, and one
arriving without a negative case is returned rather than reviewed.

### Consequences

- **A proof can go stale, and that must be visible.** Heron's golden test library already reports proofs
  as `STALE` when the files they rested on change; the same mechanism applies per fragment.
- *"It worked"* is not a proof and there is nowhere to record it as one.
- The gate is cheap for a well-scoped fragment and expensive for a vague one, which is the right way
  round: a fragment whose negative case is hard to state is a fragment whose purpose is not yet clear.

---

## D-31 — Product, data and derived are already separated, and the code is the record

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-13](OPEN-QUESTIONS.md)

### Context

Q-13 recommended product under the install location, data under the user profile, derived under a cache
location, and the updater physically unable to write to the data class — *"now critical: the repository is
public, so client data must be physically incapable of reaching it."*

**Checked against the code: it is already built, and has been running since Step 1.** `HeronPaths` is the
single place permitted to construct a Heron path — [`check-structure.py`](../tools/check-structure.py)
enforces that nothing else does — and it draws the three classes:

| Class | Where | Why |
|---|---|---|
| **PRODUCT** | Where the assemblies live | Replaced wholesale on update; the user never edits it |
| **DATA** | `%APPDATA%\Heron` | The user's own. **Roaming** — a preference or a learned skill should follow the person between machines |
| **DERIVED** | `%LOCALAPPDATA%\Heron` | Rebuildable. **Deliberately not roaming**: runtime state belongs to the machine, and a bridge file announcing process 24156 on somebody else's PC is meaningless here |

The updater half was **verified rather than assumed**: `deploy-addin.ps1` writes only into the Revit
add-ins folder and never into `%APPDATA%\Heron`.

One placement looks wrong and is right: **the audit log is DATA, not DERIVED.** It is evidence, and
evidence a cache-clear can delete is not evidence — [D-17](#d-17--state-lives-where-its-lifetime-says-it-should)
already.

### Decision

**Confirmed as built.** Q-13 is answered by code that has been running for weeks rather than by a fresh
choice.

### Consequences

- The public-repository worry is answered **structurally**: client data cannot reach the repository
  because it is never written inside it — DATA and DERIVED are both under the user profile.
- Adding a new file means adding a property to `HeronPaths` and choosing its class. There is no other
  legal way to build the path, and the structure check fails anyone who tries.
- **A question can be answered by code that already exists.** This one sat open while its answer ran
  every day. Worth asking of the remaining open questions before designing anything for them.

---

## D-32 — v1 must be able to change the model, and reading is what gets used first

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-20](OPEN-QUESTIONS.md)
**Reversed once, within the hour.** This decision was first recorded as *"v1 is read-only"*. Asked the
same question again, Ajmal answered **"it must change things too"**. The rule below is the one that
stands; the reversal is kept in view because a commit from the same day argues the opposite case at
length.

### Context

Q-20 proposed a v1 of *select and move, end to end, with undo, audit and a preview.* Two questions were
put to Ajmal:

1. **Which job first?** → *answering questions about the model.*
2. **Is reading enough for v1, or must it change things?** → first *"reading first, writing soon after"*,
   then on being asked again, **"it must change things too"**.

**The two answers are not in conflict, and reading them as one picture is what makes sense of it.**
Answering questions is what he will *use* first — it is the daily work, and it is the half already proven
against a real Revit. But a Heron that cannot change anything is not a finished product to him; it is a
report tool. **First-to-use and finished are different things**, and the earlier read-only decision
collapsed them into one.

### Decision

**v1 ships with both.** Heron answers questions about the model *and* can change it — moving elements and
setting parameters, behind the preview, the single undo and the permission gate that Step 6 already
builds.

**Reading is what gets built out and used first**, because it is proven, it is the daily value, and it
carries no risk while the write path is still being tested. It is the first thing in Ajmal's hands, not a
separate release.

**Writing stays off by default** ([D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit))
— that is about the *setting a user turns on deliberately*, not about whether the capability ships. Both
remain true at once.

### What "answers questions" means, concretely

Asked which kind of question he asks most in real work, Ajmal answered **all of them**. Four shapes, and
the answer that matters is what they have in common:

| The question | The answer |
|---|---|
| *How many VCDs?* | A number. One line |
| *What duct sizes, and how many of each?* | A schedule-style table, sorted the way a schedule sorts |
| *The 300×300 ones on Level 2* | The items themselves, **with their ids** — this is almost always the step before doing something to them |
| *What is missing or wrong?* | Blank Marks, unset system names, parameters nobody filled in |

**All four share the same first half.** Each is a *filter* — which elements — joined to a different small
*action*: count, group and count, report with ids, check for blanks. That is
[D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer) arriving as a practical result rather
than a principle: **the filters are built once and answer all four**, so "all of them" is far less work
than four separate features, provided the split holds.

It also sets the build order inside the reading half: **filters first, actions after.** A missing action
means one question shape is unavailable; a missing filter means every shape is unavailable for that kind
of element.

### Consequences

- **The whole register gates v1 again.** Groups `C`, `D`, `E`, `G` and `H` — the gate, the move, the
  refusals, the failures, the lease — are not deferred to a later version. All 47 Revit items stand
  between here and v1, and `D3` (*move them, then measure one*) is on the critical path rather than
  beside it.
- **Phase 1's proof is required, not optional.** Its definition of done — *one Ctrl+Z puts it back, and a
  failed operation leaves the model untouched* — is a v1 requirement.
- **Phase 2 still comes next, and for the reason it always did**: what makes the reading half good is
  knowing which question to answer and how. That work is independent of the write path and can proceed
  while the register is worked through at the PC.
- [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer)'s filter/action split still earns its
  keep first on the reading side, which is the half with no way to damage a model.

### The reversal is the point

**Two decisions were reversed within hours of being recorded on 2026-08-28** — this one, and
[D-26](#d-26--the-model-file-is-never-uploaded), which moved three times. Neither reversal was a mistake
by Ajmal; both were the first answer being sharpened once its consequence was visible.

That is the whole argument for `R1` in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md), and it is now
evidence rather than caution: **a decision taken in one pass, from a phone, reads differently when its
consequence is in front of you.** Read them back before building on them.

---

## D-33 — Heron never assumes an input. It asks — and it asks once

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-33](OPEN-QUESTIONS.md)

### Context

Q-33 asked for a **number**: what confidence level triggers a question? Asked plainly whether Heron
should stop and ask or make its best guess, Ajmal answered **"always ask before assuming anything."**

**That dissolves the question rather than answering it, and that is the better outcome.** A confidence
threshold is a figure somebody invents, nobody can justify, and every future session is free to tune —
and the first time it is tuned to reduce interruptions, it starts guessing about the thing it was set up
to protect. *Never assume* is a rule. It needs no number and cannot drift.

It is also what the code already does. `RevitWrite` is described in its own header as *deliberately shaped
to REFUSE rather than to guess*, the failure analysis fails closed
([D-21](#d-21--failure-analysis-is-a-table-not-a-model-call)), and writing is off until proven
([D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)). This makes the
habit a rule.

### Decision

**Heron never supplies a value the user did not give.** Not a clearance, not a distance, not a size, not
a level, not a category it inferred from a word it half-recognised. If an input is missing, it asks.

**And it asks once.** The answer is recorded and reused, because *always ask* without memory becomes noise
— and noise is clicked through without being read, which is worse than not asking at all. The pairing is
not a convenience; it is what keeps the rule working.

### The one boundary, and it needs confirming

**A technical choice is not an assumption.** Which API call, which filter, how to structure the code —
Heron decides those itself and says what it did.

The reasoning stands on its own: asking somebody to choose between options they have no basis to judge is
not consultation. It transfers the decision without transferring the ability to make it, and the answer
that comes back is a guess wearing the user's name. Heron would have assumed anyway — it would just have
laundered the assumption through a question.

**This line was drawn from Ajmal's standing way of working, not from his answer to Q-33**, which did not
mention it — so it was put to him on its own in the `R1` read-back, with the option of moving it either
way: asking him about code choices too, or letting Heron supply some standard numbers.

**CONFIRMED AS WRITTEN, 2026-08-29.** He kept both halves: every Revit number is asked for, and the
technical choices are Heron's to make and report. The line stands, and it is now his rather than
inferred from his habits.

### Consequences

- **Numbers are where this bites hardest.** Clearances, spacings, heights, margins — a plausible default
  is the most dangerous thing Heron could offer, because it is the one nobody checks. A fragment whose
  inputs are marked *edit every time, never a fixed default* is this rule written into the fragment.
- **Every asked question is a small decision that must be stored**, findable by whatever it was about —
  so the storage design in [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) has one more
  customer than Phase 2 planned for.
- It makes Heron slower to start on an unfamiliar job and faster on a repeated one, which is the right
  way round for work that damages a model when it is wrong.
- **The failure mode to watch for is not too many questions — it is a remembered answer applied to a job
  it does not fit.** *Asks once* must be scoped to something real, not to the whole product.

---

## D-34 — Heron's own wording is English; understanding the user is not Heron's job

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-17](OPEN-QUESTIONS.md)

### Context

Q-17 asks two questions that look like one: *English only, or does Heron need to understand instructions
in other languages used on site?* Ajmal was asked the first and answered **English only for now**. The
second half answers itself, and in his favour.

**Heron does not interpret language at all.** It runs inside Claude Code
([D-01](#d-01--heron-runs-as-a-claude-code-plugin)), and turning a sentence into an intent happens there,
before Heron is called. So a request typed in Arabic, in mixed Arabic and English, or dictated roughly and
half-corrected, already works — and has all day: this decision and the eight before it were settled
through exactly that kind of conversation.

What is English is **Heron's own fixed wording**: its error messages, its dialogs, its tool descriptions,
its report headings. Those are strings this repository writes.

### Decision

**Heron's own wording is English.** No translation layer is built, and none is scaffolded — Ajmal was
offered *"English now, but built ready for Arabic"* and chose plain English only, so the preparation work
is not done either.

**Heron builds nothing to understand language.** No phrase list, no synonym table, no parser for dictated
near-misses. That belongs to the host and duplicating it there would be worse than the host's version and
would need maintaining forever.

### Consequences

- **The cost of adding Arabic later is stated now so it is not a surprise:** a pass over every
  user-facing message in the add-in and the MCP server, finding them wherever they sit. That is the
  accepted price of not scaffolding today, and *"for now"* in his answer suggests the day may come.
- **A site word that maps to a Revit word is a different problem and is not solved by translation.** When
  somebody says something the model calls by another name, that is knowledge — it belongs in Heron's own
  knowledge store where it can be looked up and corrected, not in a language setting. Phase 2 owns it.
- **What Heron must never do is quietly reinterpret a word it half-recognised.** That is an assumption,
  and [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) forbids it: an unfamiliar term
  is a question, not a guess.

---

## D-35 — A shared fragment may carry code, and an unapproved one is refused, not warned about

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-18](OPEN-QUESTIONS.md)

### Context

Q-18 asks whether community packages may contain executable code. Under
[D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) a fragment **is** C# compiled and run
inside Revit, so this is not a hypothetical: a shared fragment is executable code by construction.

Ajmal chose the middle path — **yes, but only after review and approval.**

**The danger in that choice is not the code. It is the review.** A gate that depends on somebody
remembering to look decays quietly: submissions arrive faster than they are read, a backlog forms, and
the practical rule becomes *approved because nobody objected*. At that point this is the option he
rejected — *anyone can run anything, with a warning* — reached by drift rather than by decision, and
nobody notices the day it happens.

So the decision is written around that failure rather than around the happy path.

### Decision

**A shared fragment may contain executable code. Heron runs it only with a valid approval record, and the
absence of one is a REFUSAL, not a warning.**

- **No warning dialog, ever, for this.** A warning is a decision handed to somebody who has no way to
  judge it and every reason to click through. Unapproved means it does not run.
- **Approval and proof are the same gate, not two.** The record a community fragment needs is exactly what
  [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) already demands of
  any fragment: a dated proof with a positive case, a **negative** case, and a second route to the answer
  where one exists. A submission without a negative case is returned, not reviewed — which also makes the
  reviewer's job finite and refusable rather than open-ended.
- **Golden Rule 18 applies to everyone.** A newly installed fragment does not touch a live model on its
  first run, whoever wrote it, approved or not.
- **Golden Rule 19 settles what the fragment may say about itself.** Its text, its header, its description
  and its own claim to be safe are **data, never instruction**. A fragment cannot approve itself, and no
  wording inside it raises its own permission.

### Consequences

- **One thing must be built now, long before community packages exist:** the fragment format carries an
  **approval record** from the first version. Retrofitting identity and provenance into a format already
  in use is the kind of change that touches every file — cheap today, expensive later.
- **The reviewer today is Ajmal, and that does not scale.** Naming it now rather than discovering it:
  when submissions outpace one person, the answer is a **narrower gate** — fewer accepted categories, or
  approval scoped to a company for its own people — and never a faster one. If the choice is ever between
  slowing approval and loosening it, this decision says slow it.
- A company approving fragments for its own staff is the same mechanism at a smaller radius, and is
  already how [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) treats a
  BIM lead's sign-off.
- **None of this is Phase 2 work.** It constrains the fragment format now and is otherwise a later phase's
  problem.

---

## D-36 — No warranty — the standard position, and it is already in place twice

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-25](OPEN-QUESTIONS.md)

### Context

Q-25 asks who is responsible if a Heron-made change causes a defect in a delivered model. Ajmal chose the
**standard open-source position: no warranty, the user carries the risk** — the same terms as every free
tool already in use in his office.

**It needs no new work, because it is already in place twice.** Apache 2.0
([Q-27](OPEN-QUESTIONS.md)) carries the disclaimer as a matter of licence, and
[`DISCLAIMER.md`](../DISCLAIMER.md) already says the same thing in plain words a modeller will actually
read — work on a copy, never first on a live deliverable, verify before issuing, use the preview, know how
to undo.

**So he chose the lighter of the two options offered and already has the stronger one.** That is worth
saying rather than leaving him with less than he has.

### Decision

**No warranty. The licence text is the legal instrument; `DISCLAIMER.md` is the honest one.** Nothing new
is written.

### Consequences

- Nothing to build, and nothing blocked.
- **`DISCLAIMER.md` is load-bearing and must not drift from what Heron actually does.** It currently
  promises a preview, a single undo entry, that owned elements are skipped with a message, and that Heron
  never synchronises with central on its own. **Every one of those is unproven today** — they are Step 6's
  claims, and the register exists to test them. If any turns out false, that file is a promise Heron
  breaks, and it must change with the code rather than after it.
- **This records a choice, not legal advice**, and it holds for Heron as it is today: free and open source
  ([Q-21](OPEN-QUESTIONS.md)). **If Heron is ever sold, bundled into paid deliverables, or supplied to a
  client as part of a service, the question is a different one** and this decision should be reopened
  rather than assumed to carry over.

---

## D-37 — The name is Heron AI, and no trademark check has been done

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-24](OPEN-QUESTIONS.md)

### Context

Q-24 notes that *"Heron" is widely used in software* and asks that it be checked before branding,
packaging and an app-store listing exist. Offered the choice, Ajmal chose **keep Heron AI**, and did not
take the option to check first.

### Decision

**The name is Heron AI.** It stays in the ribbon tab, the assemblies, the folder names, the install
command and the documents — **53 code files** carry it today.

### What has NOT been done, recorded so nobody assumes otherwise

**No trademark or existing-product search has been carried out.** Not by this session and not, as far as
this repository records, by anyone. Q-24 asked for one and it was declined rather than performed, so the
question's own concern — that the name is widely used in software — **stands unexamined**.

That is a legitimate choice for a free tool with no branding to defend, and it is recorded here so that a
later session reading *"Q-24 answered"* does not conclude the name was cleared. **It was chosen, not
cleared.**

### Consequences

- **The technical window to rename stays open until the repository goes public**
  ([Q-28](OPEN-QUESTIONS.md) — when licence, safety files and working code all exist). While it is
  private and nobody has installed anything, a rename is a mechanical change across 53 files. **After
  publication it is a breaking change** to installed add-ins, folder paths under the user profile, and
  anything anyone has written down.
- **The two places where this could actually bite are both still ahead**, and neither is imminent:
  publication, and an Autodesk App Store listing ([Q-26](OPEN-QUESTIONS.md), still open), where a name
  clash is somebody else's decision rather than ours.
- If a check is ever wanted, **before publication is the moment it is cheap** — and it is the last one.

---

## D-38 — GitHub now, App Store kept possible, and nothing built for it

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-26](OPEN-QUESTIONS.md)
**Narrows** [D-07](#d-07--distribution--github-first-app-store-later), which named the App Store as a later goal.

### Context

Q-26 warns that Autodesk's review constrains packaging, permissions and installer behaviour, and that it
is *"cheaper to read the requirements before the installer is finalised than after."* Ajmal chose
**GitHub now, App Store later maybe** — keep the door open, build nothing for it.

### Decision

**Distribution is a GitHub download.** One install command, released whenever Ajmal likes, no review and
no external timing. **Nothing is built, packaged or signed for an App Store listing**, and no requirement
is designed around on the strength of a guess about what it might be.

**The door is kept open by not closing it**, which costs nothing: no decision is taken that would make a
listing impossible, and there is no such decision on the table today.

### The requirements have NOT been read — and that is the actual answer to Q-26

Q-26 asked for them to be read before the installer is finalised. **They have not been, and this session
is the wrong place to pretend otherwise:** stating Autodesk's current packaging, signing and review rules
from memory would be exactly the failure this repository has already been bitten by twice with the Revit
API — a confident answer nobody checked. **Read them from Autodesk, at the time, or not at all.**

So Q-26 is answered as a *direction*, and the reading it asks for is deferred with it. **Before any
listing is attempted, that reading is the first task, not the last.**

### What is already true, and may or may not help

Stated as facts about Heron rather than as compliance claims, because no requirement has been read
against them:

- It **installs per user with no administrator rights**, proven end to end in Phase 0.
- The add-in contains **no network code at all**, verified against the source.
- It is a **standard `.addin` manifest plus assemblies** — the ordinary shape of a Revit add-in.
- The licence is **Apache 2.0** ([Q-27](OPEN-QUESTIONS.md)), permissive and already chosen.

### Consequences

- [Q-38](OPEN-QUESTIONS.md) — the exact install command — is the live piece of this and is still open. It
  belongs to the GitHub route and needs no App Store input.
- **The name matters here too.** A store listing is where a name clash stops being ours to decide
  ([D-37](#d-37--the-name-is-heron-ai-and-no-trademark-check-has-been-done)), and no check has been done.
- Signing is not undertaken, and if a listing is ever pursued, **signing and the requirements reading are
  the same piece of work** rather than two.

---

## D-39 — Shadow mode is approved on an analysed disagreement, not a count of agreements

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-29](OPEN-QUESTIONS.md)

### Context

Q-29's per-agent-type table is right and is accepted as proposed — *"observe without modifying"* genuinely
does mean different things for a read-only agent, a ranking agent, a `MODIFY` agent and a code generator,
and the table says what each one means. What was open is the promotion rule: **how many shadow runs**, and
whether a human still signs off.

**Not a count, for the same reason [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs)
is not a count.** Agreement is weak evidence. Two implementations can be wrong in the same way — they
often are, because the second was written by someone who read the first. And an agent that silently does
nothing agrees with everything. **A hundred agreements prove less than one disagreement somebody sat down
and explained.**

### Decision

The table stands as written. Promotion out of shadow needs two things:

1. **At least one disagreement, examined and explained** — what differed, which was right, and why. If
   none occurred, a stated reason that is not *"it always matched"*: too few runs, a case never exercised,
   an input the shadow could not see.
2. **A human signature.** Evidence plus a name, as Q-29 recommended.

### Consequences

- **Shadow mode's product is a disagreement log, not an agreement rate.** Build the log. A percentage
  score would invite a threshold, and a threshold is the thing
  [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) already refused for the same
  reason: it is a number somebody invents and a later session tunes.
- **An agent that never disagrees is a finding to investigate, not a pass.** Either it is not running,
  not seeing the same inputs, or it is a copy of what it is shadowing — and each of those is worth
  knowing before promotion, not after.

---

## D-40 — The dependency graph is SQLite, and an edge is derived before it is stored

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-31](OPEN-QUESTIONS.md)

### Context

Q-31 recommends SQLite with recursive queries, beside the knowledge store, and warns against building a
general *"Knowledge Graph"* when only [Part 2 §41](00b-master-specification-agent-os.md) actually specifies
one. Both points are taken: it is ordinary relational data, the engine is already chosen
([D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope)), and a graph database would be a second
technology for no gain.

**Today added a second rule that matters more than the storage choice.**
[`check-api-surface.py`](../tools/check-api-surface.py) answers *"which Revit API members does Heron
depend on?"* by reading the compiled assembly. That answer is **always current and cannot go stale**,
because it is computed from the artifact rather than remembered about it. A hand-maintained table of the
same facts would drift the first time somebody changed code without updating it — and **a stale dependency
graph is worse than none**, because blast radius is precisely the question people trust it for.

### Decision

**SQLite, beside the knowledge store. Build §41's graph and nothing wider.**

**And an edge is derived before it is stored.** Store an edge only when it cannot be computed from an
artifact on demand — a fragment's declared inputs, an assembly's references, a manifest's contents. What
can be read is read.

### Consequences

- The stored half shrinks to the genuinely declarative: which skill claims which fragment, which
  capability a fragment offers. The rest is computed.
- Freshness stops being a maintenance problem for the computed half, which is most of it.
- **A derived edge needs its deriver to be checked**, which is [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs)
  again: `check-api-surface.py` was believed only after it was shown to catch a defect it was known to
  have. Any future deriver earns trust the same way.

---

## D-41 — Single-user now; company knowledge is a git repo, and the admin is the reviewer

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-32](OPEN-QUESTIONS.md)

### Context

Q-32 sets out three shapes and recommends **A now, B next**. The deciding fact is in the question itself:
Heron is a single-user plugin and **there is no server to enforce anything.** Building user management
into a product with no enforcement point produces security theatre — a settings screen that describes a
policy nothing can apply.

### Decision

**A now: scopes are folders**, which they already are —
[D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) makes each one its own file, and Golden
Rule 5 keeps them apart physically.

**B when there is demand: company knowledge is a private git repository**, and *"admin"* is whoever
reviews the pull requests.

**C — a central enterprise server — is not built without real demand**, and would be a different product.

### Consequences

- **B costs almost nothing new, and that is the point.** A shared scope is already a file in a folder;
  making that folder a git repository adds no Heron code at all.
- **The admin mechanism already exists — it is [D-35](#d-35--a-shared-fragment-may-carry-code-and-an-unapproved-one-is-refused-not-warned-about) at a smaller radius.**
  A company approving fragments for its own staff and a maintainer approving them for everyone are the
  same gate, the same approval record, and the same refusal when it is missing. One mechanism, two uses,
  rather than an enterprise feature built alongside a community one.
- **Nothing is enforced, and the documentation must not imply otherwise.** What a BIM manager actually
  wants — *everyone uses our approved standards* — is delivered by a reviewed shared repository. What
  cannot be delivered is stopping someone who does not want to comply, and no wording should suggest it
  can.

---

## D-42 — The public install command is not settled; the proven one is `setup.ps1`

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-38](OPEN-QUESTIONS.md) — **partly, and it says which part**

### Context

Q-38 settles the *shape* — one documented command fetching a signed release, never *"paste this URL and
let the AI run what it finds"* — and leaves the command itself open. Its preferred option, a Claude Code
plugin install, carries its own warning: it **needs verifying against current plugin documentation**, and
*"an install command that does not work is worse than none."*

**That warning is the answer for now.** The documentation has not been read in this session, and writing
an install command from memory is precisely the failure this repository has already had twice — most
recently this same day, when a Revit API property that reads like the obvious choice turned out not to
exist before 2024. A wrong install command fails on a stranger's machine, at the first thing they ever try.

### Decision

**The public headline command is deferred to publication, when the documentation can be read.** It is
named as the first task of publishing, not the last.

**What is settled, because it is proven:** `tools\setup.ps1` — one command that detects every installed
Revit, builds for each, and deploys per-user with no administrator rights. It ran end to end in Phase 0.
It is the fallback Q-38 calls *"always available"*, and it is also the route a cautious IT department will
prefer.

### Consequences

- **Nothing is blocked.** Q-38 says so itself, and the working route exists today.
- **This is on the publication checklist, not the build one**, alongside reading the App Store
  requirements ([D-38](#d-38--github-now-app-store-kept-possible-and-nothing-built-for-it)) — both are
  *read the current documentation* tasks, both deferred for the same reason, and both cheap at that moment.
- **The README's first command is the first impression**, and it must be one somebody has actually run on
  a clean machine. Until then it should show the route that has been run.

---

## D-43 — The Constitution is accepted — all 30 Articles, binding

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-35](OPEN-QUESTIONS.md)

### Context

[`HERON_CONSTITUTION.md`](../HERON_CONSTITUTION.md) had stood as *proposed, pending confirmation* since it
was written. Offered the choice of accepting it, deferring it, or having all 30 read out, **Ajmal asked
for all 30 to be read**, and accepted them after reading.

**That is the difference between a confirmation and a tap**, and it is why the option existed. A
30-article document accepted by pressing a button is not accepted; it is unread.

### Decision

**All 30 Articles are binding.** The document's status moves from proposed to accepted.

Its own Amendment clause now applies: an Article may not contradict a Golden Rule, changing one is a
**decision recorded here with its reasoning and compensating control**, and Articles are *"never weakened
silently, and never by an agent."*

### Reading it aloud found three stale statements in it

None changed what any Article requires. All three would have been read as current by whoever implements
the enforcement:

- It described its own basis as *"Golden Rules 1–15 (official) and 16–19 (proposed)"* — **wrong twice**,
  since all **21** became official earlier the same day ([Q-19](OPEN-QUESTIONS.md)).
- **Eight Articles cited a *"Proposed"* Golden Rule** that was no longer proposed.
- It said an agent receives *"not all 27"* Articles. There are **30** — the count predates 12a, 12b
  and 12c.

All corrected on acceptance, and named in the file rather than quietly fixed. **A document about to
become binding must not misdescribe its own authority.**

### Consequences

- **The belt-and-braces split in the file is the part that matters most**, and it is already written
  there: *"a rule stated here is not enforced by being stated here."* Text in an agent's instructions is
  guidance a model can be argued out of. **Every Article that can be enforced in code must also be
  enforced in code**, at the add-in's permission boundary — and the file's own enforcement table says
  which ones those are.
- Articles 8, 9, 11, 12a, 12b and 12c are **exactly what the register tests**. Accepting them does not
  make them true: groups `C`, `D` and `E` are still what proves the code obeys them.
- Article 24 — *confidence is not validation* — is this same day's lesson stated as a rule. A property
  that read like the obvious choice compiled cleanly and did not exist in Revit 2020.
- Article 20 — *a new capability starts unproven and stays unproven* — is
  [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) at constitutional
  level, and the two agree.

---

## D-44 — A re-authored fragment starts unproven in Heron, whatever it was elsewhere

**Status:** Accepted · **Date:** 2026-08-29 · **Extends:** [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported), [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs)

### Context

[D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) settled that the
owner's existing libraries are studied and re-authored here, never imported. It did not say what happens
to the **status** of a fragment whose original had been proven against a real model — and the originals
carry exactly that, dated and detailed, which makes carrying it over look reasonable rather than lazy.

Ajmal closed it himself on 2026-08-29, as the re-authoring began:

> *"Even in the AJ AI proven fragment, don't mark in Heron this is proven, because we will check each and
> every one again in Heron AI. So mark it as not proven in Heron."*

### Decision

**A re-authored fragment enters Heron at `DRAFT` and is promoted only by a proof taken here, against
Heron's own implementation.** A proof recorded elsewhere is evidence about the code that ran there. It
is not evidence about the code in this repository, however faithfully that code was re-authored — and
"faithfully" is the word doing all the work in that sentence, which is the reason.

### Why this is not over-caution

The re-authoring is a **rewrite**: different contract shape, different composition model, different
naming, split differently, and running inside a wrapper this repository generates rather than the one it
came from. Every one of those is a place a behaviour can change without anybody intending it. The
original's proof says the ORIGINAL worked.

And the failure it guards against is the one this whole project keeps meeting: a thing that succeeds
while doing nothing. The move fragment studied on the same day is precisely that story — its own library
found, live, that Revit's move call **returns normally and moves nothing** for a group member. A proof
inherited rather than re-taken is a claim nobody has watched.

### Consequences

- **It is enforced, not agreed.** `proof_problems` in
  [`brain/heron_fragment.py`](../brain/heron_fragment.py) now refuses `PROVEN` or `PRODUCTION` when the
  proof does not match this fragment's own implementation bytes. The fingerprint in a proof is a hash of
  those bytes, so a carried-over proof cannot match one.
- **That gate existed and nothing stood on it.** `can_promote` refused a stale proof; `validate` — which
  is what `python brain/heron_fragment.py` and `check-gaps.py` actually run — did not. Measured on
  2026-08-29 by writing a fragment that declared `PROVEN` on a proof from another model and watching
  every check in the repository pass it.
- **The lessons still cross, and they are the valuable half.** What a studied fragment carries is *where
  Revit lies to you* — and that knowledge belongs in the new file's own words, argued from what it
  guards against rather than from having been proven somewhere else.
- **Expect the DRAFT count to rise as re-authoring goes on**, and expect that to look like a growing
  debt. It is: a debt of Revit-checking, which is the only kind the owner's standing instruction wants
  left.

---

## D-45 — The library is built out first, and proved in one pass later

**Status:** Accepted · **Date:** 2026-08-30 · **Revises:** [31 §4](31-studying-the-existing-libraries.md),
**Extends:** [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported), [D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere)

### Context

[31 §4](31-studying-the-existing-libraries.md) said the opposite of this, and said it for reasons that
were good at the time: *"not a race to a number"*, *"most of the 398 will correctly never be made"*,
*"take the jobs that are done; let the rest wait for a request."* The argument was that a library does
not know which of its own entries matter, and that re-authoring everything imports that guesswork.

**The owner has decided otherwise, and his argument is about sequencing rather than about volume.** In
his words, 2026-08-30:

> *"If we build everything now, checking for any small or even big errors later will take much less time
> compared to trying to generate and make everything as we go… Doing a real review and checking might
> take two to three days, but this way we can get everything completed first."*

### Decision

**Re-author the library out in full, then prove it in one concentrated Revit pass.** Building is not
gated on proving. [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported)'s
method is unchanged — studied and re-authored, never copied — and
[D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere) is unchanged:
every one arrives `DRAFT`.

### Why this is right, stated in full rather than deferred to

- **Checking has a large fixed cost per sitting and a small one per fragment.** One Revit session with
  one model open, worked down a list, is far cheaper than the same fragments checked in three hundred
  separate sittings. The saving is real and it is the owner's to claim.
- **Building is not blind.** The compile gate reads every fragment against every release it claims, so
  the entire *worked-in-2020-broke-in-2024* class is caught before Revit is opened at all.
- **There is no Revit for the moment**, so the alternative to building is not "build less, prove more".
  It is idling.

### The condition this decision carries, because without it the arithmetic fails

**A repeated mistake is the one thing that breaks the plan.** The risk is not the number of unproven
fragments; it is one misunderstood mechanism copied across many of them. A wrong level lookup, a wrong
unit conversion, a parameter that snaps — found on the first day at the PC — is not one fix, it is
eighty, and 2–3 days becomes two weeks. This is not hypothetical here: the system these fragments come
from carried one removed Revit call in **eight** places behind a green compile, and the two bugs found
while re-authoring were the same bug wearing different clothes.

**So a mechanism that more than one fragment needs is written ONCE, as its own fragment, and composed.**
That is [31 §3](31-studying-the-existing-libraries.md)'s split rule applied deliberately rather than
opportunistically: with the whole library in view, the shared mechanisms are visible up front instead of
emerging after the tenth copy. Proving those few mechanisms then validates most of what stands on them,
and a defect found in one is fixed in one place.

### Consequences

- **[31 §4](31-studying-the-existing-libraries.md) no longer governs scope** and now points here. Its
  method sections (Rules 0–3) are untouched and still binding — this changes *how many*, never *how*.
- Rule 0's *"does it earn a place"* narrows rather than disappears: it stops asking *is this job one the
  owner does* and asks only **does Heron already cover it**. Duplication is still the thing to avoid;
  scarcity no longer is.
- **The DRAFT count will rise steeply and that is the plan, not a fault.** `check-gaps.py` keeps
  *unfinished* and *waiting* apart precisely so a growing pile of unproven work stays visible as a debt
  rather than disappearing into a pass.
- **A recipe is not a fragment.** The 44 multi-stage recipes read as **skills** in Heron's shape, which
  name capabilities rather than fragments; they are re-authored as skills or not at all.

---

## D-46 — A context fragment is consumed by the host, not by another fragment

**Status:** Proposed · **Date:** 2026-08-31 · **Revisits:** [31 §1](31-studying-the-existing-libraries.md),
**Touches:** [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer), Step 13's dependency graph

### Context

[31 §1](31-studying-the-existing-libraries.md) recorded that the earlier library has **context**
fragments — what is open, which view, which document — and judged that they need no new `kind` in
Heron, because context reads the session rather than elements, which is *"a filter whose `provides` is
a document or a view."* It ended with an instruction: **revisit when a real one is written and the
contract is in front of you.**

It is now in front of us. `GET_ACTIVE_VIEW` (`FRG-DOC-003`) exists, and it was not written on a hunch —
**the dependency graph asked for it.** `TAG_ELEMENTS_IN_VIEW` needs both `elements` and a `view`, and
the graph reported it as an action nothing could feed, because every filter in the library provides
elements and none provided a view.

### What the contract turned out to say

**Half the judgement holds and half of it does not.**

**It holds as a `kind`.** `GET_ACTIVE_VIEW` declares `kind: filter`, provides a `View`, and nothing
about the contract strained. No fourth kind is needed, exactly as 31 §1 predicted.

**It does not hold in the graph.** `composable(producer, consumer)` requires **one producer to satisfy
every fragment-sourced need of the consumer**. So two filters can never jointly feed one action, and a
context fragment — whose output the HOST takes and passes on — has no fragment-level consumer at all.
The graph therefore reports it as *"a filter nothing can consume"* forever, which is true of the model
and false about the fragment.

### What was actually done, and what was NOT

**The right fix was to the contract, not to the graph.** `view` on an action is `source: request` —
which view the user means is the host's to resolve from *"this view"*, exactly like `category` on a
filter. Applied to `TAG_ELEMENTS_IN_VIEW`, that removed the orphan honestly, and the graph's report
turns out to have been **correct about a mislabelled need** rather than a limitation being hit.

**`GET_ACTIVE_VIEW` was written and then removed.** It was authored to feed the `view` need, and the
contract fix made it unnecessary the same hour — nothing in the library consumes it, and there is no
executor or host path that calls it yet. [31 §2](31-studying-the-existing-libraries.md) Rule 2 says
*add nothing speculative*, and keeping it would also have left `check-gaps.py` permanently red on a
condition already understood. **A checker that is always red stops meaning anything**, which is the
exact failure the 2026-08-26 daily-check note records. It goes back when something needs it.

Its `kind` was not the problem, and that half of 31 §1 stands confirmed: it declared `kind: filter`,
provided a `View`, and nothing about the contract strained. **No fourth kind is needed.**

### The open question, and why it waits

**The graph cannot express an action fed by two filters.** `composable(producer, consumer)` requires
ONE producer to satisfy EVERY fragment-sourced need. Nothing needs that today, because `view` was
always a request input. But a genuine two-filter composition will exist eventually, and lifting the
limit means letting several producers jointly satisfy one consumer — a real change to Step 13's design.

That change is cheap to make and expensive to be wrong about: it decides what *"what breaks if this
changes"* answers, which is the question Step 13 exists to answer. **The evidence should come from a
real composition running at the PC, not from a library that has never executed.** So it is recorded
here and left, rather than a graph quietly taught to stay quiet.

`tests/test_graph.py` was narrowed accordingly: it asserts that **the break's** orphans heal, not that
the library has none. That assertion was a claim about the whole library, and it held only while every
fragment was a filter feeding an action.

