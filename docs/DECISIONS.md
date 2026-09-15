# Decision Log

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone asking **what was decided, and why** |
> | **Authority** | The [Constitution](../HERON_CONSTITUTION.md) and [Golden Rules](14-golden-rules.md) win over this file. This file wins over every work note |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | A settled answer is **promoted here from [OPEN-QUESTIONS](OPEN-QUESTIONS.md)**. A reversal gets a **new entry** that supersedes the old — the original stays, so the reasoning is never lost |
> | **Its numbers** | Derive the highest decision: `grep -oE '^#+ *D-[0-9]+' docs/DECISIONS.md | grep -oE '[0-9]+' | sort -n | tail -1` |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


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
screen is [`R1b`](NEEDS-CHECKING.md): [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes)
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

> **Generated — do not edit this table by hand.** `python tools/generate-decision-summary.py`
> rebuilds it from the decisions below, and CI fails if it is stale. It was hand-written until
> 2026-09-12, by which time it stopped at **D-50** while the file had reached **D-70** — twenty
> decisions missing from the index of decisions, with nothing able to notice.
>
> **A status cell is kept verbatim once written.** *"read back 2026-09-06"* records a
> conversation, not a fact on disk, so the generator never overwrites one — it only fills in
> rows that do not exist yet.

| # | Decision | Status |
|---|---|---|
| [D-00](#d-00--documentation-first-no-implementation-yet) | Documentation first, no implementation yet | ✔ **Fulfilled** · read back 2026-09-06 |
| [D-01](#d-01--execution-host-claude-code-plugin) | Execution host: Claude Code plugin | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-02](#d-02--mcp--add-in-transport-named-pipes) | MCP ↔ add-in transport: named pipes | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-03](#d-03--mcp-tool-granularity-thick-and-specific) | MCP tool granularity: thick and specific | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-04](#d-04--generated-code-execution-hybrid) | Generated code execution: hybrid | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-09](#d-09--revit-thread-marshalling-externalevent) | Revit thread marshalling: ExternalEvent | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-05](#d-05--revit-version-support-2020-to-latest) | Revit version support: 2020 to latest | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-06](#d-06--implementation-languages-c-for-revit-python-for-brain) | Implementation languages: C# for Revit, Python for brain | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-07](#d-07--free-open-source-on-public-github) | Free open source on public GitHub | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-08](#d-08--licence-apache-20) | Licence: Apache 2.0 | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-10](#d-10--repository-stays-private-until-working-code-exists) | Repository stays private until working code exists | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-11](#d-11--adopt-master-specification-part-2-agent-operating-system) | Adopt Master Specification Part 2 (Agent Operating System) | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-12](#d-12--adopt-the-master-handover-baseline-part-3-and-its-fifteen-golden-rules) | Adopt the Master Handover Baseline (Part 3) and its fifteen Golden Rules | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-13](#d-13--adopt-additional-requirements-part-4-kernel-workflow-engine-constitution) | Adopt Additional Requirements (Part 4): Kernel, Workflow Engine, Constitution | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-14](#d-14--unify-six-status-vocabularies-into-two-orthogonal-axes) | Unify six status vocabularies into two orthogonal axes | ⏳ Proposed · ✔ re-put 2026-09-06, answer unchanged |
| [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour) | Adopt the field notes as authoritative on bridge behaviour | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope) | The session list is built live, and the Revit freeze is out of scope | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-17](#d-17--runtime-state-is-machine-local-not-roaming) | Runtime state is machine-local, not roaming | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-18](#d-18--the-transaction-agent-belongs-to-step-6-not-step-2) | The Transaction Agent belongs to Step 6, not Step 2 | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit) | Writing is off by default until the write path has met a real Revit | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-20](#d-20--millimetres-to-feet-is-arithmetic-not-unitutils) | Millimetres to feet is arithmetic, not UnitUtils | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-21](#d-21--failure-analysis-is-a-table-not-a-model-call) | Failure analysis is a table, not a model call | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over) | A second chat is refused, not allowed to take over | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-23](#d-23--the-knowledge-store-is-sqlite-one-file-per-scope) | The knowledge store is SQLite, one file per scope | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-24](#d-24--embeddings-are-computed-locally-by-default) | Embeddings are computed locally by default | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported) | The existing libraries are studied and re-authored, never imported | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-26](#d-26--the-model-file-is-never-uploaded) | The model file is never uploaded | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-27](#d-27--one-voice-and-the-answers-shape-follows-the-questions-shape) | One voice, and the answer's shape follows the question's shape | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) | Generated code is C#, compiled at run time, in process | ✅ Accepted |
| [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer) | A fragment is a composable piece, not a whole answer | ✅ Accepted |
| [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) | A fragment is promoted by one recorded proof, not by a count of runs | ✅ Accepted |
| [D-31](#d-31--product-data-and-derived-are-already-separated-and-the-code-is-the-record) | Product, data and derived are already separated, and the code is the record | ✅ Accepted |
| [D-32](#d-32--v1-must-be-able-to-change-the-model-and-reading-is-what-gets-used-first) | v1 must be able to change the model, and reading is what gets used first | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-33](#d-33--heron-never-assumes-an-input-it-asks--and-it-asks-once) | Heron never assumes an input. It asks — and it asks once | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-34](#d-34--herons-own-wording-is-english-understanding-the-user-is-not-herons-job) | Heron's own wording is English; understanding the user is not Heron's job | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-35](#d-35--a-shared-fragment-may-carry-code-and-an-unapproved-one-is-refused-not-warned-about) | A shared fragment may carry code, and an unapproved one is refused, not warned about | ✅ Accepted · ✔ read back 2026-08-29 |
| [D-36](#d-36--no-warranty--the-standard-position-and-it-is-already-in-place-twice) | No warranty — the standard position, and it is already in place twice | ✅ Accepted |
| [D-37](#d-37--the-name-is-heron-ai-and-no-trademark-check-has-been-done) | The name is Heron AI, and no trademark check has been done | ✅ Accepted |
| [D-38](#d-38--github-now-app-store-kept-possible-and-nothing-built-for-it) | GitHub now, App Store kept possible, and nothing built for it | ✅ Accepted |
| [D-39](#d-39--shadow-mode-is-approved-on-an-analysed-disagreement-not-a-count-of-agreements) | Shadow mode is approved on an analysed disagreement, not a count of agreements | ✅ Accepted |
| [D-40](#d-40--the-dependency-graph-is-sqlite-and-an-edge-is-derived-before-it-is-stored) | The dependency graph is SQLite, and an edge is derived before it is stored | ✅ Accepted |
| [D-41](#d-41--single-user-now-company-knowledge-is-a-git-repo-and-the-admin-is-the-reviewer) | Single-user now; company knowledge is a git repo, and the admin is the reviewer | ✅ Accepted |
| [D-42](#d-42--the-public-install-command-is-not-settled-the-proven-one-is-setupps1) | The public install command is not settled; the proven one is `setup.ps1` | ✅ Accepted |
| [D-43](#d-43--the-constitution-is-accepted--all-30-articles-binding) | The Constitution is accepted — all 30 Articles, binding | ✅ Accepted |
| [D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere) | A re-authored fragment starts unproven in Heron, whatever it was elsewhere | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases) | Heron tracks the MCP SDK across major versions, the way it tracks Revit releases | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-46](#d-46--the-emergency-stop-button-is-removed-the-switch-behind-it-stays) | The Emergency Stop button is removed, the switch behind it stays | ✅ Accepted · ✔ read back 2026-09-06 |
| [D-47](#d-47--a-job-can-cross-projects--both-repeating-it-and-copying-content--and-undo-does-not-cross-with-it) | A job can cross projects — both repeating it and copying content — and undo does not cross with it | ✅ Accepted |
| [D-48](#d-48--one-broken-part-costs-one-part-never-the-whole-library) | One broken part costs one part, never the whole library | ✅ Accepted |
| [D-49](#d-49--a-heavy-optional-import-never-happens-on-a-request-thread) | A heavy optional import never happens on a request thread | ✅ Accepted |
| [D-50](#d-50--revit-says-out-loud-what-heron-is-doing-to-it-and-whether-it-is-reading-or-changing) | Revit says out loud what Heron is doing to it, and whether it is reading or changing | ✅ Accepted |
| [D-51](#d-51--a-negative-case-is-judged-by-its-counts-not-by-whether-the-fragment-stayed-silent) | A negative case is judged by its counts, not by whether the fragment stayed silent | ✅ Accepted · 2026-09-07 |
| [D-52](#d-52--a-count-of-what-was-turned-down-is-not-a-count-of-what-was-found) | A count of what was turned down is not a count of what was found | ✅ Accepted · 2026-09-07 |
| [D-53](#d-53--a-fragment-that-cannot-come-back-empty-is-proved-by-tracking-instead) | A fragment that cannot come back empty is proved by TRACKING instead | ✅ Accepted · 2026-09-07 |
| [D-54](#d-54--the-callers-half-arrives-as-text-and-revit-is-what-turns-it-into-a-view) | The caller's half arrives as text, and Revit is what turns it into a view | ✅ Accepted · 2026-09-08 |
| [D-55](#d-55--a-fragments-preview-is-the-run-itself-rolled-back) | A fragment's preview is the run itself, rolled back | ✅ Accepted · 2026-09-08 |
| [D-56](#d-56--the-banner-counts-in-flight-off-the-dispatcher-because-an-end-can-arrive-before-its-own-begin) | The banner counts in flight off the dispatcher, because an End can arrive before its own Begin | ✅ Accepted · 2026-09-08 |
| [D-57](#d-57--the-master-architecture-document-is-a-research-brief-not-a-fifth-part-of-the-specification) | The Master Architecture document is a research brief, not a fifth part of the specification | ✅ Accepted · 2026-09-09 |
| [D-58](#d-58--heron-measures-what-it-can-see-and-the-cost-meter-belongs-to-the-host) | Heron measures what it can see, and the cost meter belongs to the host | ✅ Accepted · 2026-09-09 |
| [D-59](#d-59--reading-spans-loaded-links-only-when-the-modeller-asks-and-the-answer-says-how-many-it-read) | Reading spans loaded links only when the modeller asks, and the answer says how many it read | ✅ Accepted · 2026-09-09 |
| [D-60](#d-60--a-preview-selects-what-it-would-change-and-what-it-would-skip-up-to-500) | A preview selects what it would change and what it would skip, up to 500 | ✅ Accepted · 2026-09-09 |
| [D-61](#d-61--only-a-run-that-came-back-may-be-cached-and-re-indexing-forgets-what-changed-underneath-it) | Only a run that came back may be cached, and re-indexing forgets what changed underneath it | ✅ Accepted · 2026-09-09 |
| [D-62](#d-62--the-brain-writes-its-own-audit-file-and-the-reader-that-already-merges-does-the-merging) | The brain writes its own audit file, and the reader that already merges does the merging | ✅ Accepted · 2026-09-09 |
| [D-63](#d-63--a-want-is-recorded-when-a-capability-is-asked-for-by-name-and-nobody-provides-it) | A want is recorded when a capability is asked for BY NAME and nobody provides it | ✅ Accepted · 2026-09-09 |
| [D-64](#d-64--a-fragment-that-goes-looking-declares-what-it-dropped-and-the-marker-rides-only-on-the-empty-answer) | A fragment that goes looking declares what it dropped, and the marker rides only on the empty answer | ✅ Accepted · 2026-09-09 |
| [D-65](#d-65--heron-keeps-the-degraded-result-rule-and-hands-routing-to-the-host) | Heron keeps the degraded-result rule and hands routing to the host | ✅ Accepted · 2026-09-09 |
| [D-66](#d-66--heron-checks-the-licence-of-what-it-ships-by-reading-the-files-not-the-landing-page) | Heron checks the licence of what it ships by reading the files, not the landing page | ✅ Accepted · 2026-09-09 |
| [D-67](#d-67--a-point-crosses-as-three-millimetre-numbers) | A point crosses as three millimetre numbers | ✅ Accepted · 2026-09-09 |
| [D-68](#d-68--a-significant-change-states-its-intent-before-it-is-made-and-is-judged-against-it-afterwards) | A significant change states its intent before it is made, and is judged against it afterwards | ✅ Accepted · 2026-09-12 |
| [D-69](#d-69--a-script-in-tools-reads-the-code-it-checks-and-that-is-not-a-layering-violation) | A script in `tools/` reads the code it checks, and that is not a layering violation | ✅ Accepted · 2026-09-12 |
| [D-70](#d-70--heron-keeps-a-usage-counter-on-the-machine-and-it-is-numbers-rather-than-a-diary) | Heron keeps a usage counter, on the machine, and it is numbers rather than a diary | ✅ Accepted · 2026-09-12 |
| [D-71](#d-71--every-length-a-caller-types-is-millimetres-and-the-fragment-converts-it) | Every length a caller types is millimetres, and the fragment converts it | ✅ Accepted · 2026-09-13 |
| [D-72](#d-72--four-values-a-caller-could-not-type-are-now-built-from-what-they-type-and-a-face-still-is-not) | Four values a caller could not type are now built from what they type, and a face still is not | • status not stated |

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

**Status:** ✔ **Fulfilled** — its condition was met · **Date:** 2026-08-27 · **Read back and closed:** 2026-09-06 · **Affects:** the whole repository

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

### Closed 2026-09-06 — read back to the owner

**This rule is spent, not broken.** It held until the owner said start; Phase 0, Step 6 and the whole
of Phase 2 were then built on it. Read back on 2026-09-06 and marked **Fulfilled** rather than
*Accepted*, because a satisfied condition left reading as a live rule looks like a rule being ignored.
The owner added no replacement gate.

---

## D-01 — Execution host: Claude Code plugin

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-1](OPEN-QUESTIONS.md)
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
  Communication/Persona layer, and revisitable later. **Revisited 2026-09-06 and confirmed unchanged**
  — the owner was asked directly whether he now wanted a docked window inside Revit as well, with the
  size of that job stated, and chose to stay with Claude Code alone. The door in the next paragraph
  stays open; nothing is being built for it.

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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-2](OPEN-QUESTIONS.md)
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-5](OPEN-QUESTIONS.md)
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
  [D-09](#d-09--revit-thread-marshalling-externalevent) and Golden Rule 9 enforceable.
- Tools map one-to-one onto fragments, so the knowledge system accumulates naturally from use.
- **Accepts:** the tool list grows large, and MCP tool schemas cost context on every request.
  Mitigated by capability discovery — a small stable core set plus `heron_find_capability`,
  registering specific tools only when needed. This must be designed in from the start, not retrofitted.

### Read back 2026-09-06 — confirmed, and the owner gave a reason the original entry did not have

Asked with the tool count now at **329**, he kept it, and for **reuse**: *"this will be like modular so
each tool can [have] multiple usage easily."* The entry above argues granularity from **permissioning**
and from fragments accumulating; his argument is that a small tool gets used by many different jobs
while a big one gets used by its own. That is the same reasoning [D-29](#d-29--a-fragment-is-a-composable-piece-not-a-whole-answer)
arrived at independently a day later — **it was his instinct first**, and it is worth recording that the
two were reached from opposite ends and met.

---

## D-04 — Generated code execution: hybrid

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-7](OPEN-QUESTIONS.md)
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
- ~~**Open sub-decision:** which scripting runtime (pyRevit / IronPython / Python.NET / Roslyn scripting).~~
  **CLOSED 2026-08-28 by [D-28](#d-28--generated-code-is-c-compiled-at-run-time-in-process) — Roslyn C#,
  in process, through the existing bridge.** This line still read *open* on 2026-09-06, ten days after it
  was settled, and the read-back is what found it: a sub-decision closed in a **new** entry leaves the
  old entry's own text saying otherwise, and nobody re-reads an entry they think they know.

---

## D-09 — Revit thread marshalling: ExternalEvent

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-4](OPEN-QUESTIONS.md)
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-3](OPEN-QUESTIONS.md)
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-6](OPEN-QUESTIONS.md)
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-21](OPEN-QUESTIONS.md), [Q-22](OPEN-QUESTIONS.md)
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

**Asked again 2026-09-06, in the read-back, and the answer was: free and open source — and
STILL PRIVATE.** The licence is in place ([D-08](#d-08--licence-apache-20)) and working code now exists,
so [D-10](#d-10--repository-stays-private-until-working-code-exists)'s condition is met and the block is
no longer the condition — it is the owner's choice, made deliberately and freshly on that date. **The
commercial half did not move**: free, open source, anyone may install it. Only the timing of going
public did, and going public still needs him to say so.

Full plan: [17 — Open Source & Distribution](17-open-source-and-distribution.md).

---

## D-08 — Licence: Apache 2.0

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-27](OPEN-QUESTIONS.md)
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Question:** [Q-28](OPEN-QUESTIONS.md)
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

### Superseded 2026-09-06 — both conditions are MET, and the repository is private by choice

**Condition 1 was done on 2026-08-27 and condition 2 is now done too**: Phase 0, Step 6 and the whole of
Phase 2 exist and compile on 2020 through 2027. So **this decision has stopped deciding anything** — as
written it would have permitted going public, and the repository is still private.

Asked in the read-back what the rule should be now, and offered a condition he could hang it on — *after
the Revit proofs pass* — the owner chose **"when I say so"**: no written condition, no automatic trigger,
public only on his explicit word. That is narrower than what this entry allowed and it is deliberate.

**The pre-flight check above still stands and is the one thing that is NOT at his discretion:** verify no
client data is in the history before flipping, `.gitignore` being a safety net rather than a guarantee.

---

## D-11 — Adopt Master Specification Part 2 (Agent Operating System)

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
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
| **Emergency Stop** | ~~Phase 1, in the Revit ribbon~~ — **the BUTTON was removed 2026-09-06 ([D-46](#d-46--the-emergency-stop-button-is-removed-the-switch-behind-it-stays)).** Flagged as a conflict during this entry's read-back — Part 2 asks for a control that works when the agent side is stuck — and **resolved the same day by the owner: the Heron button IS that control.** It calls `bridge.Stop()`, so nothing can reach Revit at all. Part 2's requirement is **met by a different control**, not dropped |
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
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

*(Written when three parts existed. Part 4 followed — see [D-13](#d-13--adopt-additional-requirements-part-4-kernel-workflow-engine-constitution) — and field notes after that, see [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour). Part 3 remains authoritative on the rule set.)*

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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
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

**Status:** Proposed · **Date:** 2026-08-27 · **Re-put to the owner 2026-09-06 — same answer** · **Question:** [Q-34](OPEN-QUESTIONS.md)
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

### Asked a second time 2026-09-06 — and the answer did not move

Put to him again in the read-back, in the family framing that worked, and with the argument for closing
it now stated plainly: **all 329 fragments already carry both labels**, so the model is not hypothetical.
He answered **"yes, but show me on screen first"** — word for word what he said on 2026-08-28.

**So this stays Proposed and [`R1b`](NEEDS-CHECKING.md) stays open.** Two asks, eleven days apart,
same answer: he accepts the *idea* and will not sign off a trust model he has not watched work. Nothing
further is gained by asking a third time — **only a screen closes this one.**

---

## D-15 — Adopt the field notes as authoritative on bridge behaviour

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27
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
| **The AI's script runs on the thread that draws the screen; Revit is genuinely frozen while it runs; no add-in can change that** | **Confirms the Revit API threading constraint from the field.** Absent from all four specifications, present in the working code. Validates [D-09](#d-09--revit-thread-marshalling-externalevent) |
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Source:** [field notes addendum](00e-field-notes-proven-bridge.md)
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Found during:** Step 1 implementation
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-28 · **Found during:** Step 2 implementation

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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Found during:** Step 6 implementation

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
- The default flips to `true` **only** when [HANDOVER §6](HANDOVER.md#6-the-return-to-the-machine-checklist)
  has been walked end to end against a real model — not when the code merely compiles.
- Anyone reading `HeronPermissions` finds the reasoning in the file, not only here. The comment saying
  why it is off is written to be **deleted** once the path is proven, so a stale justification cannot sit
  there looking current.

### Read back 2026-09-06 — kept off, and the owner described the loop he wants it turned on FOR

He confirmed it stays off, and then said unprompted how he expects to work once it is on:

> *"We will test that separately and we need to make changes in the Revit and new creation tools while
> working. I need to test — then if that works okay it's proven, next time it will work. And if not
> working, or it did wrong, I can undo, then again I can tell what I need exactly."*

**That is [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) in his own
words, arrived at independently** — one real run that worked, watched by him, is what makes a tool
proven; not ten runs, and not a compile. It also confirms [D-04](#d-04--generated-code-execution-hybrid):
he expects **new** tools to be built mid-session, not only existing ones run.

**And it makes [Golden Rule 16](14-golden-rules.md) load-bearing rather than nice to have.** His whole
loop rests on *"I can undo"* — undo is not a convenience here, it is the thing that makes trying an
unproven tool on a live model a reasonable act. **If one undo ever fails to reverse one action, this
working method stops being safe**, which is why `D5` (one Ctrl+Z) sits in the register and why
[D-47](#d-47--a-job-can-cross-projects--both-repeating-it-and-copying-content--and-undo-does-not-cross-with-it)
has to warn him **before** a cross-model job rather than after.

---

## D-20 — Millimetres to feet is arithmetic, not UnitUtils

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-27 · **Found during:** Step 6 implementation

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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-28 · **Found during:** Step 6 implementation
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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-28 · **Found during:** Step 6, auditing for gaps
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
per-user with no administrator rights ([D-01](#d-01--execution-host-claude-code-plugin),
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
  across ([HANDOVER §7](HANDOVER.md)). This decision is the same rule applied to substance rather
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
  settled a question that designing had left open for weeks — [D-15](#d-15--adopt-the-field-notes-as-authoritative-on-bridge-behaviour)
  again, and this time the field note was somebody else's working habit rather than a Revit behaviour.

---

## D-28 — Generated code is C#, compiled at run time, in process

**Status:** Accepted · **Date:** 2026-08-28 · **Answers:** [Q-7a](OPEN-QUESTIONS.md)
**Completes** [D-04](#d-04--generated-code-execution-hybrid), which settled *hybrid* and left the runtime open.

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
evidence a cache-clear can delete is not evidence — [D-17](#d-17--runtime-state-is-machine-local-not-roaming)
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

That is the whole argument for `R1` in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md), and it is now
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
([D-01](#d-01--execution-host-claude-code-plugin)), and turning a sentence into an intent happens there,
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
**Narrows** [D-07](#d-07--free-open-source-on-public-github), which named the App Store as a later goal.

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

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-29 · **Extends:** [D-25](#d-25--the-existing-libraries-are-studied-and-re-authored-never-imported), [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs)

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

## D-45 — Heron tracks the MCP SDK across major versions, the way it tracks Revit releases

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-08-31 · **Extends:** [D-05](#d-05--revit-version-support-2020-to-latest), [D-06](#d-06--implementation-languages-c-for-revit-python-for-brain)

### Context

Heron's MCP server was written against MCP SDK 1.x and imported `FastMCP` from `mcp.server.fastmcp`.
The install line Heron itself hands the user — `pip install --user mcp`, in
[`tools/HeronRevit.ps1`](../tools/HeronRevit.ps1) — is **unpinned**, and on 2026-08-31 it resolved to
**2.x**, which renamed `FastMCP` to `MCPServer` and deleted the old module path.

The result was not a degraded feature. The import is at module scope, so the server raised `ImportError`
before registering a single tool: **every Heron tool absent from the host**, on any machine installing
that day, with no Revit and no Windows involved in the failure.

It went unseen because **every test in this repository reads that file as text.** Text cannot fail an
import. The technique that found it in ten minutes was installing the dependency and starting the thing.

### Decision

**A dependency Heron does not control is treated the way [D-05](#d-05--revit-version-support-2020-to-latest) treats a
Revit release: the version in front of it is discovered, never assumed, and an unrecognised one fails
loudly rather than silently.**

In practice, three obligations:

1. **Look the class up, newest first.** `mcp.server.mcpserver.MCPServer`, then `mcp.server.fastmcp.FastMCP`.
   Both take the same `@server.tool()` decorator and the same `run()` — measured with both installed
   side by side, not assumed from the migration notes.
2. **An unknown version is a named failure.** The final `except` re-raises naming Heron, both module
   paths tried, and the install line — never a bare `ImportError` about a module the user never typed.
3. **Something must actually import it.** [`tests/test_mcp_serves.py`](../tests/test_mcp_serves.py) is
   that obligation made mechanical; without it this decision is a paragraph nobody executes.

### Why not simply pin `mcp<2`

A pin is the smaller change and it was considered. It was rejected because it converts a loud failure
today into a quiet staleness later: the user's host, Claude Code, ships and updates its own SDK, and
Heron pinning against it would eventually fail in the direction that produces no error at all — the far
worse half of this project's one recurring failure. Pinning also freezes Heron on a version that will
stop receiving fixes, and does nothing for the machine that already has 2.x installed for another tool.

**Supporting both costs one `try`/`except` and is proven on both.** A pin costs nothing today and is
unproven on the version everyone will be running tomorrow.

### Consequences

- The version-tolerant import lives in
  [`mcp/server/heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) and nowhere else. It is the only
  place the SDK is named.
- `heron_version` reports the SDK version and the class serving, because *"Heron stopped working"* and
  *"the SDK moved underneath it"* are indistinguishable from the user's side.
- The install line stays **unpinned**, which is now a supported configuration rather than an accident.
- **This generalises past the MCP SDK.** Heron's other outside dependencies — `pyyaml`, the optional
  `model2vec` — get the same treatment: absent is a normal condition, present-but-different is
  discovered, and neither may take the whole system down without saying which one it was.

---

## D-46 — The Emergency Stop button is removed, the switch behind it stays

**Status:** Accepted · ✔ **read back 2026-09-06** · **Date:** 2026-09-06 · **Supersedes:** the ribbon placement in
[21 §4](21-resilience-and-operations.md) and the Part 2 acceptance row above

### Context

Emergency Stop was recorded in five places as a **Revit ribbon button**: [21 §4](21-resilience-and-operations.md),
the Part 2 acceptance table in this log, [the glossary](15-glossary.md), [the roadmap](ROADMAP.md) and
`HERON-OPS-STP-007` in [the agent registry](28-agent-registry.md). It shipped in Step 6 and was on the
ribbon from then until today.

On 2026-09-06 Ajmal asked what it was for. The case for it was put to him in full and unhedged — that a
stop built inside the agent system is useless when that system is stuck, that it is checked at two
separate gates before anything reaches a model, that it is sticky until a person clears it, and that it
honestly cannot interrupt a Revit API call already running. **He read that and decided he did not want
the button.** He was equally explicit about the scope: the button goes, the code behind it stays —
*"behind that we have something programming related that we created before, that function we need."*

**His reason, given afterwards, is the better argument and is why this entry exists rather than a
one-line note.** *"I can use this same thing with the main bridge button. If I don't want it I can
stop the bridge connection, it's easy. So no need."*

He is right, and disconnecting is the **stronger** of the two. Emergency Stop refused `Modify` and
above while letting reads through. Disconnecting closes the pipe, so **nothing** arrives at all —
no reads, no writes, no lease. It is one click on a button that is already there, whose picture
already tells you which state you are in, and it has exactly the same honest limit: neither can
interrupt a Revit API call that has already started. Two controls doing overlapping jobs is how
somebody ends up unsure which one is holding.

### Decision

**The ribbon carries no Emergency Stop button. Everything behind it is untouched.**

`HeronStop`, the gate in `RevitOperations.Gate` that refuses anything at `Modify` risk or above, the
second gate in `RevitWrite.Refuse`, and `EmergencyStopCommand` itself all remain exactly as they were.
One `PushButtonData` in `HeronApplication.BuildRibbon` puts the button back.

### What this costs, stated plainly

- **Nothing can switch the stop on any more.** `EmergencyStopCommand` was its only caller, and it is now
  reachable from nowhere. `HeronStop.IsStopped` is false for ever, so both gates are inert.
- **There is no stop that leaves the connection up.** The first of [21 §4](21-resilience-and-operations.md)'s
  three requirements — *reachable when Heron is misbehaving* — **is** met, but by the Heron toggle
  rather than by the control 21 §4 named. What is genuinely gone is the softer stop: the one that
  blocked changes while still letting you ask Heron what it had just done. After disconnecting,
  reconnecting is what gets that back.
- **Neither control can interrupt work Revit has already started.** That was true of the button and
  is true of disconnecting. Ctrl+Z remains the only thing that reverses a change already made.
- **The file-based kill switch that 21 §4 pairs with the button was never built.** Until it is, the
  mechanism kept here has no trigger at all.
- **`C1`, `C2`, `C4` and `C6` in [NEEDS-CHECKING.md](NEEDS-CHECKING.md) can no longer be run.** The
  rule `C6` existed to prove — the stop blocks changes only and never reads — is still written in the
  code and is now **unproven by test**.

### What this does not cost

**Writes are not left unguarded.** The permission gate is a separate mechanism from the stop and is
untouched: `write.enabled` is `false` by default, so `Modify` is still refused for a reason that has
nothing to do with Emergency Stop. `C3`, `C5`, `C7` and `C8` still run and still prove it.

And **Ctrl+Z is still what reverses a change that already happened.** It always was — the button never
could.

### Read back 2026-09-06 — and the owner named the control that replaces it

Put to him with the conflict stated plainly — Part 2 requires a stop that works when the agent side is
stuck, and after this removal nothing in the UI could set one — he answered with a question:

> *"I think this is not needed, because if I need to stop I can directly off the bridge. Am I right?"*

**He is right, and it was checked rather than agreed with.** The Heron button's command calls
`bridge.Stop()` ([Commands.cs](../revit/Heron.Revit.Addin/Commands.cs)), which stops the pipe server
outright. That is **stronger** than the switch it replaces: the Emergency Stop flag was read *inside* the
add-in after a request had already arrived, whereas a stopped bridge means **no request arrives at all**.
One button he already presses daily, and its icon shows the state from across the room.

**So Part 2's requirement is met by a different control rather than abandoned**, and the conflict recorded
against [D-11](#d-11--adopt-master-specification-part-2-agent-operating-system) earlier the same day is
closed by that.

**Two limits, stated so neither is discovered later:**

- **Neither control stops an operation already executing** inside Revit's thread — the removed button
  could not either. **Ctrl+Z is what reverses something that already happened**, which is the whole weight
  [Golden Rule 16](14-golden-rules.md) carries and why [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit)'s
  read-back records the owner's working loop resting on it.
- **The Heron button is a ribbon button**, so a frozen Revit UI cannot be clicked. **This is not a
  regression** — Emergency Stop was a ribbon button on the same ribbon with the same limitation.

**And the stop capability keeps a test.** `C1`, `C2`, `C4` and `C6` cannot be run, but `B3` and `B3a` in
[`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) press the Heron button and check it disconnects — so what is
untested is the *old* mechanism, not the ability to stop.

### Why the code was kept rather than deleted

Deleting `EmergencyStopCommand` would remove the entire trigger side of a mechanism the owner asked to
keep, leaving gates that read a flag nothing could ever set and no obvious way back. Keeping it costs one
unreferenced class and makes restoring the button a one-line change. **Read this entry before deleting it
as dead code** — it is unreferenced on purpose, not by oversight.

---

## D-47 — A job can cross projects — both repeating it and copying content — and undo does not cross with it

**Status:** Accepted · **Date:** 2026-09-06 · **Question:** [Q-41](OPEN-QUESTIONS.md)
**Affects:** [25](25-multi-session-and-binding.md), [14 — Rule 16](14-golden-rules.md), [09](09-skills-and-fragments.md), [D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope), [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over)

### Context

**Raised by the owner, not by a specification.** During the 2026-09-06 read-back he confirmed
[D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope) as written and then
immediately asked for something it does not allow: *"maybe from one project refer same, like that need to
do in another project."*

Everything about binding in Heron assumes **one job, one document**. Rule 20 pins the target document by
identity and forbids following the active window; [D-22](#d-22--a-second-chat-is-refused-not-allowed-to-take-over)
refuses a second chat rather than sharing; [D-16](#d-16--the-session-list-is-built-live-and-the-revit-freeze-is-out-of-scope)
builds a picker because a job belongs to exactly one Revit. A job that starts in Tower A and lands in
Podium crosses all three.

Offered the two readings with their costs stated — **A**, repeat the action on a second model; **B**, copy
content between models — he answered **BOTH**.

### Decision

**Both are in scope. B first, A second, and the order is cost, not preference.**

| | What it is | Where it stands |
|---|---|---|
| **B — copy the content** | Transfer view filters, line styles, parameters, standards from one model to another | ~~**Mostly written.**~~ **THAT WAS WRONG — corrected 2026-09-06, see below** |
| **A — repeat the action** | *"Do to Podium what you just did to Tower A"* — Heron replays the **job**, not the result | **New work.** Needs a replayable record of a job, and every element re-resolved by identity in the second model, because element ids do not carry across documents |

**Neither may follow the active window.** A second document is *bound* exactly as the first is — picked
explicitly, pinned by identity, verified at every step. Rule 20 is not relaxed; it is applied twice.

### Correction 2026-09-06 — B was NOT mostly written, and the second binding was never needed

**Two things in this entry were wrong, both written by reading names instead of contracts.**

**First, the evidence for *"mostly written"* does not survive being opened.** `COPY_VIEW_FILTERS` takes
`doc, sourceView, targetViews` — it copies filters from one view to *other views in the same project*.
`REMAP_LINE_STYLES` moves lines between styles inside one document. `COPY_FROM_LINK` copies from a **link**,
which is not another open project. **None of the three crosses two projects.** The only fragment that ever
did is `TRANSFER_VIEWS_BETWEEN_DOCUMENTS`. Three fragment names were cited as evidence and not one was read.

**Second, the *"second binding"* this entry asks for is not how it works.** The pattern already in the
library is simpler and better: **the destination is the bound document, and the source is found by title
among the open ones.** Nothing new is bound, Rule 20 is untouched, and the source is only ever read — so
the undo question this decision worries about does not even arise for B. It arises for A, where a second
document is written to.

**And the read half already existed by the time this was corrected**, built the same day in another
session: `run_fragment_read` takes an optional `document` title, refuses a model that is not open with the
list of ones that are, excludes linked documents, and reports `wasActiveDocument` so an answer about a
window nobody is looking at says so.

**What was actually missing was a standards transfer**, and the first one is now written:
`TRANSFER_VIEW_FILTERS_BETWEEN_DOCUMENTS` (`FRG-VIEW-097`), following the established pattern. It is
`DRAFT` and has met no model — [D-44](#d-44--a-re-authored-fragment-starts-unproven-in-heron-whatever-it-was-elsewhere)
and [D-30](#d-30--a-fragment-is-promoted-by-one-recorded-proof-not-by-a-count-of-runs) both apply.

### Undo — the part that is not ours to design

**Revit keeps a separate undo stack per document.** A job spanning two models therefore **cannot** be one
Ctrl+Z, and no amount of design makes it one.

So [Golden Rule 16](14-golden-rules.md) is **restated, not broken**: *one user action, one undo* becomes
**one undo per document**, and Heron **says so before it starts** — *"this touches two models; undoing in
Podium will not undo Tower A."* Warning afterwards is the exact failure Rule 16 exists to prevent, so the
warning is part of the operation rather than a note in the documentation.

### Consequences

- ~~**Rule 16's wording needs updating**~~ **DONE 2026-09-06.** [Rule 16](14-golden-rules.md) now reads
  *one user action, one undo — **per document***, and carries the reason as measurement rather than
  assertion: `Transaction` and `TransactionGroup` were read by reflection out of the **shipped** Revit 2020
  and 2024 assemblies, and every constructor of both takes exactly one `Document`. **What was NOT measured
  is stated too** — 2027's assembly is .NET 10 and could not be reflection-loaded, so the exhaustive
  absence of a two-document overload is proven on two releases, not eight. The
  [conventions skill](../.claude/skills/revit-addin-conventions/SKILL.md) carries the same limit, since that
  is what an implementer reads rather than this file.
- Every cross-project write is still a write: [Rule 17](14-golden-rules.md)'s preview applies to the
  second model as much as the first, and [Rule 21](14-golden-rules.md)'s re-read applies per document.
- `write.enabled` gates both models. Nothing here loosens [D-19](#d-19--writing-is-off-by-default-until-the-write-path-has-met-a-real-revit).
- **Neither half is proven, and A is not built at all.** This decision sets direction; it claims no code.

---

## D-48 — One broken part costs one part, never the whole library

**Status:** Accepted · **Date:** 2026-09-06 · **Extends:** [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases)
**Affects:** [`brain/heron_fragment.py`](../brain/heron_fragment.py), [09](09-skills-and-fragments.md), [21](21-resilience-and-operations.md)

### Context

**Raised by the owner while [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases) was being read back to him**, as a question rather than a request:

> *"If one tool update, do not remove all. Like if that tool is there is no, means need to update, so
> tell need to update but remaining old one need to be work. This is right, am I right??"*

**He was right, and the answer had two halves.** D-45's own incident is the *exception* rather than the
example: what vanished on 2026-08-31 was the MCP server itself — the floor every tool stands on — and
when the floor is missing there is no surface left to serve a surviving tool from. That case genuinely is
all or nothing.

**But the rule he stated is correct one layer down, and Heron was breaking it.** Measured the same day,
rather than reasoned about: a folder holding one good fragment, one with malformed YAML, and a second good
one returned **no fragments at all** and raised `ParserError`. `load()` promised in its own docstring to
raise `ValueError`; `yaml.YAMLError` is not a `ValueError`, so a parse error walked straight through
`load_all`'s `except ValueError` and took **all 343** fragments with it.

Nobody had seen it because every fragment in the repository is machine-written and well-formed. **The
library was correct, so the loader's behaviour on an incorrect one had never been observed** — the same
shape as `A8` and `A7` before it: an untried path, not an unknown one.

### Decision

**A part that cannot be read is skipped, named, and costs only itself.**

1. `load()` converts **every** failure into a `ValueError` carrying a readable message — parse errors and
   I/O errors included — so `load_all`'s contract is true rather than merely written down.
2. `load_all()` additionally catches **anything** a fragment can throw. A bare `except` is normally a
   smell; here it *is* the guarantee, and it is commented as such so it is not tidied away later.
3. The broken part is **reported by name**, never swallowed. *"Tell need to update"* is the half of his
   sentence that stops this from being silent degradation, which would be worse than the crash.

### Consequences

- [`tests/test_fragment_store.py`](../tests/test_fragment_store.py) now proves it, with the broken
  fragment sorted **between** the two good ones on purpose: a loader that dies on it takes the third with
  it, and a test placing it last would pass while the library still lost everything after the failure.
- **The rule generalises beyond fragments** and should be applied wherever Heron loads a set of things it
  did not write: skills, capabilities, community packages. **It is not claimed for those yet** — only the
  fragment loader is measured and fixed.
- **It does not loosen [D-45](#d-45--heron-tracks-the-mcp-sdk-across-major-versions-the-way-it-tracks-revit-releases).**
  A missing foundation still fails loudly and completely; that is the correct behaviour when there is
  nothing left to degrade to. This decision governs the many, not the floor.

---

## D-49 — A heavy optional import never happens on a request thread

**Status:** Accepted · **Date:** 2026-09-06 · **Found during:** `A8`, on the owner's PC
**Affects:** [`brain/heron_embed.py`](../brain/heron_embed.py), [`mcp/server/heron_mcp_server.py`](../mcp/server/heron_mcp_server.py), [05](05-heron-brain.md), [21](21-resilience-and-operations.md)

### Context

`heron_capabilities` stopped replying. Not slowly — **at all**: a real Claude Code tool call waited **thirty
minutes** and got nothing back. Every existing test passed while this was true.

The stack, taken with `faulthandler` rather than reasoned about:

```text
heron_capabilities            <- the request handler, on the asyncio event loop
  catalogue()
    _Open.__enter__
      heron_embed.index()
        backend()
          _load_model()
            import model2vec
              import numpy
                loading numpy's native extension   <- still here 40 s later
```

`import model2vec` costs **1.0 s** in a fresh process. On the event loop, inside a handler, it was measured
still importing at 40 s and past 170 s in another run.

**Closing [`A7`](NEEDS-CHECKING.md) is what caused it.** Until `model2vec` was installed that import
raised `ImportError` instantly, Heron degraded to the `lexical` backend, and the handler always answered.
Installing it — to prove the search understands meaning, which it does — turned an instant failure into an
unbounded wait. **Two register rows, each correct alone, and the failure lived only in their combination.**

### Decision

**A heavy or optional import never runs on a thread that owes somebody an answer.**

1. `heron_embed.warm()` imports the trained encoder on a **background thread**, started once at server
   startup before `server.run()`.
2. While that is running, `_load_model()` returns `None` **immediately** to every other caller, so the
   backend is `lexical` and says so on its own first line. **A slower answer that arrives beats a better
   one that does not.**
3. The guard **exempts the warming thread itself**. Without that it is worse than useless: the warm-up hits
   its own guard, returns, clears the flag and loads nothing — after which the next request imports on the
   event loop exactly as before. **That was this fix's first version**, and the stack that caught it looked
   identical to the stack it was meant to remove.

### Consequences

- [`tests/test_mcp_stdio.py`](../tests/test_mcp_stdio.py) drives the server as a **real subprocess over real
  stdio** and holds every reply to a **deadline**. `tests/test_mcp_serves.py` could not have caught this: it
  dispatches in-process, where the import is already done or fails instantly. **The two files differ by one
  process boundary and that boundary is the whole bug.**
- A test that waits forever cannot tell a slow answer from no answer. Any check on a thing that must reply
  carries a deadline, or it is not checking the thing that failed here.
- **The register learned something about itself.** Its rows are deliberately independent and worked down in
  order; nothing in it can express *these two are fine apart and broken together*. This is the first time
  that has bitten, and no numbering scheme fixes it — only running the earlier rows again after a later one
  changes the machine.
- The rule generalises to any optional dependency the brain may grow. **It is claimed only for the embedder
  today**, because that is the one measured.

---

## D-50 — Revit says out loud what Heron is doing to it, and whether it is reading or changing

**Status:** Accepted · **Date:** 2026-09-06 · **Found during:** the owner using it
**Affects:** [`HeronActivityBanner.cs`](../revit/Heron.Revit.Addin/HeronActivityBanner.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [25 §6](25-multi-session-and-binding.md), [28](28-agent-registry.md) `HERON-REVIT-UI-022`

### Context

The owner, in his own words: *"there is no visual identification showing if the cloud or the AI is
talking to Revit. I cannot understand what is happening visually from the Revit side."*

He is right, and the gap was structural rather than an oversight. Revit showed exactly one thing about
Heron - the ribbon button's connected picture - and that says a **pipe is open**. It says nothing about
whether anything is happening right now, nothing about what, and nothing about how it ended. A refusal
was completely invisible from Revit: the chat received a sentence and the screen showed nothing at all.

[25 §6](25-multi-session-and-binding.md) had already settled that this matters and called the banner
*"the cheapest trust feature in the platform"*, and [28](28-agent-registry.md) had reserved
`HERON-REVIT-UI-022` for it - deferred with the note *"waits for Step 6, when something is finally slow
enough to need it"*. Step 6 shipped, the executor arrived, and jobs are now slow enough. This builds it.

### The decision

**Three parts, and the third is the one that is new.**

1. **The banner is raised BEFORE the work is handed to Revit, from the listener thread.** Revit draws on
   the same thread it works on, so nothing can be painted once a job starts. Anything that decides to
   show a banner *because a job is slow* can only decide it on the very thread the job has already
   taken. There is no delayed-appearance design available, and this is the reason.

2. **It is lowered by whoever truly finished the work**, which is `Execute` on Revit's own thread - not
   the caller. A client that gave up at `still_running` has stopped waiting; **Revit has not stopped
   working**, and the screen must follow the model rather than the client. The one exception is a job
   Revit never took, where the listener lowers it because nothing else ever will. Both paths claim the
   right through one interlocked flag on the job, so the two can never both fire.

3. **It says whether Heron is READING or CHANGING**, and that is the half the earlier project never had.
   *"Something is happening"* is worth little to somebody whose real question is whether his model is
   being touched. The word and the colour come from `HeronOperationRegistry` **looked up by operation
   name** - Golden Rule 19 - so nothing arriving on the pipe can make a write wear the reading colour.

It also shows the outcome for a moment after the work, with how long it took. A freeze that lasted
twelve seconds reads as a hang; the same freeze labelled **12 s** reads as a duration.

### What was deliberately not done

- **No percentage.** A request carries no progress data - Revit does not report how far through a script
  it is - so the bar is an indeterminate sweep. A bar that fills at a made-up rate is a lie somebody
  will time their own work against.
- **No dialog, and nothing clickable.** The banner is click-through (`WS_EX_TRANSPARENT`), never
  activated, and absent from Alt+Tab, so it cannot eat a click or steal focus - Golden Rule 8.
- **Nothing was done about the freeze itself.** [25 §6a](25-multi-session-and-binding.md) settled that
  as out of scope and this does not reopen it. The banner explains the freeze; it does not shorten it.

### Consequences

- One new setting, `ui.activityBanner`, declared in both halves of the config. **It is the only default
  in that table that is on.** The others protect the model by staying off; this one protects the person
  by staying on, and somebody who does not know the banner exists is exactly who needs it.
- A cosmetic fault must never cost a request: every entry point swallows and logs, and a repeated
  failure drops the window rather than the job.
- **It compiles on all eight releases, 2020 through 2027, 0 warnings** (`B5`, 2026-09-07). That is the
  API surface agreeing across every runtime this add-in claims - net472, net48, net8.0-windows and
  net10.0-windows - which is the whole "worked in 2020, broke in 2025" class, caught without opening
  Revit. **It is not evidence that the banner appears.** `B6` to `B13` in
  [NEEDS-CHECKING.md](NEEDS-CHECKING.md) need Revit open, and `B8` - a write showing amber rather
  than blue - is the one that matters.
- **The compiler was believed absent and was not.** This was written as if nothing could be built here,
  because the .NET installer download is blocked; the distribution's own package is not.
  [docs/30](30-compiling-away-from-windows.md) already said so and it was re-learned the hard way.

## D-51 — A negative case is judged by its counts, not by whether the fragment stayed silent

**Status:** Accepted · **Date:** 2026-09-07 · **Found during:** the owner proving his first fragment with the agent
**Affects:** [`heron_validate.py`](../brain/heron_validate.py) `looks_empty`, [D-30](DECISIONS.md), every fragment that provides `findings`

### Context

`REPORT_GLOBAL_PARAMETERS` was run twice against `Project1 work_ajmal.al` — once with a global parameter
present, once after the owner deleted it. `globalCount` went **1 → 0** and the sentences changed from
*"'Length' = 0.6562 ft (200 mm) - typed in - drives 0 dimensions"* to *"this project has no global
parameters"*. That is a fragment demonstrably reading the model.

The agent flagged it anyway: **"this did NOT come back empty."**

Two separate problems were behind that, and only one of them was a bug.

**The bug.** `looks_empty` measured values with `len()`, but `RevitFragment.Describe` renders everything
as text — a collection as `"3 item(s) [a, b, c]"`, a scalar as its own string. A count of zero therefore
arrived as the **string `"0"`**, one character long, and read as "not empty". The check could never
return True for anything the executor produced. **Every** negative case was flagged, whatever it
actually returned. Fixed separately by teaching it the shapes its own executor emits.

**The question.** Even fixed, this fragment still fails, because it always provides two sentences in
`findings` — including when it finds nothing. So does every reporting fragment: **134 of the 349**
provide `findings`.

### The decision

**The counts decide. The note does not.** `findings` is excluded from the emptiness test.

The argument that settled it is that **silence is the worse answer.** A snagging inspector who hands in
a blank sheet has told you nothing — it could mean no snags, or that he never turned up. *"No snags
found"* tells you he was there. `REPORT_GLOBAL_PARAMETERS` exists precisely to separate *"this project
has none"* from *"this kind of document cannot have any"*, and it cannot make that distinction while
staying silent.

The alternative was worse in a way that matters more than strictness: the warning fired on every
negative case a reporting fragment could produce, so it would have been read past every time. **A
warning that always fires is not a safeguard, it is furniture.**

**Two conservative properties are deliberately kept**, because the failure this check exists to catch
— a fragment quietly answering about the wrong set — is still real:

1. **An unreadable value still returns False.** Anything `_as_count` cannot turn into a number gets a
   person's attention rather than a shrug.
2. **A phase that provides nothing BUT prose still returns False.** There is then no count to have been
   zero, and *"the note was written"* is not a measurement.

### What this does not do

**It does not promote anything.** `looks_empty` only decides whether the draft carries a warning; a
person still signs, and `heron-status` is still untouchable by the agent ([D-30](DECISIONS.md)).

**It does not settle the second route.** `REPORT_GLOBAL_PARAMETERS` has no second route recorded and
its draft still says `NOT ESTABLISHED`, which is correct — D-30 asks for one *"where one exists"*, and
whether one exists here has not been established either way.

### The risk being accepted, stated plainly

**134 fragments become easier to prove than they were this morning.** If `findings` ever carries a
result rather than a note in some fragment, that fragment's negative case can now pass on the strength
of its other counts alone. Nothing in the docs says `findings` is narration — it is a convention read
off 134 files, not a rule anybody wrote down. Writing it down is what this entry is for.

## D-52 — A count of what was turned down is not a count of what was found

**Status:** Accepted · **Date:** 2026-09-07 · **Found during:** proving seven fragments from two selections
**Affects:** [`heron_validate.py`](../brain/heron_validate.py) `looks_empty`, [D-30](DECISIONS.md), [D-51](DECISIONS.md)

### Context

Seven fragments were run twice against `Snowdon Towers Sample HVAC` — once with 28 duct taps selected,
once with 16 spaces. `READ_MEP_SYSTEM` returned `systemName: 28` for the taps and `systemName: 0` for
the spaces. That is a fragment reading the model.

It was flagged, because the same negative run also returned **`noSystem: 16`** — and 16 is not 0.

But `noSystem: 16` does not mean sixteen systems were found. It means **sixteen elements were examined
and none of them had one.** The same shape appears throughout the library: `notSpatial: 28`,
`unmeasurable: 28`, `noConnectors: 16`, `noPhase: 16`.

### The decision

**Fields that count what the fragment was GIVEN and could not report on are excluded from the emptiness
test.** Identified by a camelCase prefix — `no`, `not` or `un` followed by a **capital** — plus three
observed single words (`unmeasurable`, `unplaced`, `unenclosed`). The capital matters: `notes` and
`nodes` stay ordinary result fields.

**The argument is not convenience, it is that this evidence is STRONGER than silence.** A negative case
where every field is zero cannot distinguish *"it looked at sixteen spaces and none had a system"* from
*"it never looked"*. `noSystem: 16` proves it looked. Requiring the fragment to go quiet would reward
the less informative behaviour.

### Two things fixed alongside, which were bugs rather than judgements

1. **`entry(ies)` was unreadable.** `_as_count` matched `"3 item(s)"` but the executor renders a
   dictionary as `"3 entry(ies)"`, so `"0 entry(ies)"` fell through as unparseable and blocked the
   verdict. Same family as the string-`"0"` bug in [D-51](DECISIONS.md).
2. **Helper objects blocked the verdict.** `RevitFragment.Describe` falls back to `GetType().Name`, so
   `boundaryOptions` arrived as `SpatialElementBoundaryOptions` and `flow` as `` Func`3 ``. Those are
   helpers the fragment left in scope, identical in both runs, saying nothing about what was found. A
   bare type name is not a quantity that could have been zero.

### Amended the same evening — work counters too

**`scanned` blocked the first defect-finder ever proved.** `FIND_UNUSED_GROUP_TYPES` reported
`unusedGroupTypes: 0`, `unusedNames: 0`, `usedOnlyAsAttached: 0` — every result zero — alongside
`scanned: 2`, meaning it walked two group definitions. The warning fired on the 2.

A count of how many things were EXAMINED is not a count of how many were FOUND, which is the sentence
this decision is named for. `scanned: 2` beside `unusedGroupTypes: 0` is the evidence the fragment ran;
without it, an all-zero answer could not be told from a fragment that never looked.

So names containing `scanned` or `checked` are excluded too. **Thirteen such names exist across the
library** — `scanned`, `jointsChecked`, `constraintsScanned`, `sectionsNotChecked`, `roomsChecked` and
others — and **every one of them is declared `int`.** None is a result.

**This was the fourth loosening in one evening and it went to the owner as a question rather than being
decided here**, precisely because the first three had all been argued the same way and the argument was
becoming a habit.

### The risk being accepted, stated plainly

**This is the second standard relaxed in one evening, both to let fragments through, and that pattern is
worth naming rather than hiding.** [D-51](DECISIONS.md) excluded prose; this excludes rejection counts.
Each is defensible on its own and the direction is the same one, so the next one deserves more
suspicion than this one got.

**A fragment could now pass a negative case by naming its results `noFoo`.** Nothing prevents it. The
protection is that a person still reads the draft and still signs it, and that the positive case must
still find something — `looks_empty` returning True on a POSITIVE run is itself a finding.

**Seven fragments were proved under this rule the day it was written**, which is exactly the
circumstance in which a rule change should be distrusted. The evidence they rest on is recorded in each
`proof:` block and can be re-read against a stricter rule later.

### Amended 2026-09-09 — the naming is gone, the fragment says it itself

**The risk named directly above is now closed.** *"A fragment could now pass a negative case by naming
its results `noFoo`. Nothing prevents it."* Something does: nothing reads a name any more.

`REJECT_PREFIX`, `REJECT_NAMES` and `WORK_COUNTER` have been **deleted** from
[`heron_validate.py`](../brain/heron_validate.py), along with `_is_accounting` and its two call sites in
[`batch-prove.py`](../tools/batch-prove.py). All **1,192** provides in the library now carry an explicit
`role`, declared by reading what each fragment is FOR rather than what its output is called.

**That reading disagreed with these patterns 166 times, in both directions**, which is why they were
removed rather than kept as a fallback:

| Direction | Example | What the pattern did |
|---|---|---|
| an ANSWER read as bookkeeping | `select-unenclosed-rooms` `unplaced`, `unenclosed` | `REJECT_NAMES` swallowed the two faults the fragment exists to find |
| bookkeeping read as an ANSWER | `set-mep-slope.inGroup`, `flip-elements.cannotFlip` | no pattern could see them, and a non-zero one banks a proof for a run that changed nothing |

**`role` is REQUIRED now, not optional.** With nothing left to guess, an undeclared provide would fall
to `result`, so `check_contract` refuses one — the omission is a validation error somebody fixes rather
than a silent default nobody sees. `findings` is the single exemption, because [D-51](DECISIONS.md) is
a different rule and `NOTE_KEYS` still carries it.

**What is NOT claimed:** this does not make a negative case honest. A fragment can still declare a
finding as `accounting` and slip through. The difference is that it is now a sentence somebody wrote in
the contract and can be read back, rather than an accident of what the field was called.

## D-53 — A fragment that cannot come back empty is proved by TRACKING instead

**Status:** Accepted · **Date:** 2026-09-07 · **Found during:** the owner asking to carry on proving fragments
**Affects:** [`heron_validate.py`](../brain/heron_validate.py) `draft_from_record`, [D-30](DECISIONS.md)

### Context

Sixteen unproven fragments returned something for **every** selection tried — 28 duct taps, 16 spaces,
4 walls, 12 sheets, 37 equipment. Not because they were broken. `COUNT_ELEMENTS` describes whatever it
is handed; there is **no** selection that makes it report nothing. The only way to get an empty answer
is to hand it nothing, which the executor refuses as an unbound need — a refusal, not an answer.

So D-30's negative case, read as *"the answer must come back empty"*, cannot be satisfied by them. Ever.
Not for want of trying.

### What the negative case is actually FOR

[`heron_fragment.py`](../brain/heron_fragment.py) says it plainly, above `PROOF_REQUIRED`: it is
**"the one that catches 'succeeded and did nothing'"**. A fragment that ignores what it was given, or
falls back to the active view or the whole model, still returns a plausible answer. The empty case
catches that because a fallback cannot produce nothing.

**Tracking catches the same fault more directly.** Across five selections the answers were:

| Selected | 28 | 16 | 4 | 12 | 37 |
|---|---|---|---|---|---|
| `count-elements` → `count` | 28 | 16 | 4 | 12 | 37 |
| `describe-elements` → `described` | 28 | 16 | 4 | 12 | 37 |
| `report-location` → `locations` | 28 | 16 | 4 | 12 | 37 |

**A fragment falling back to anything could not match five different counts exactly.**

### The decision

**For a fragment that cannot produce an empty answer, the negative leg is met by the answer TRACKING
the input across several different inputs.** The run record carries a `tracking` list; the draft records
each pairing and says which field followed which count.

### What this does NOT license

**The tracking field must be a RESULT, not a rejection count.** Three of the sixteen matched the
selection on `withoutJoins`, `skippedNotSchedules` and `referencesNothing` — they matched because they
**rejected everything**, which is the opposite of evidence. `REPORT_JOINED_ELEMENTS` found `joins: 0` in
all five; it has never found a join here and is **not** proved by this decision.

**It does not apply to fragments that CAN come back empty.** `READ_MEP_SYSTEM` returns `systemName: 0`
on spaces, so it has a real negative case and must keep using it. Tracking is for the ones with no such
arrangement available, not a cheaper route for the ones that have.

### The risk being accepted

**This is the fifth loosening in one day** — D-51, D-52 twice, and now this — and every one has been
argued in the same direction. That pattern is the reason this one was put to the owner as a question
with the alternative *"leave them unproven"* stated first, rather than decided at the end of a long
session.

What makes it different in kind, and the only reason it is defensible: **the four before it removed
things from a check. This one replaces a check that cannot run with a different check that can** — and
five exact matches against five different inputs is more evidence than one empty answer, not less.


---

## D-54 — The caller's half arrives as text, and Revit is what turns it into a view

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner asking to prove a lot of fragments in one day
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `BindNeeds`/`FromRequest`, [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) `pull_values`/`caller_values`, [D-28](DECISIONS.md), [D-29](DECISIONS.md)

### Context

A day set aside for proving fragments reached its ceiling in about twenty minutes. Two fragments ran.
The rest refused, and the refusal was correct:

> *"'view (View)' is a value the CALLER supplies, not something the model holds — a category, a name to
> match, a distance. Heron cannot run this fragment until there is a way to pass them."*

Derived rather than guessed: **288 of the 308 unproven fragments** declare at least one need as
`source: request`, and **only 16** could be fed at all. `view` alone accounts for **53** of them, more
than twice the next value (`categories`, 22). The executor was complete; the sentence *"until there is
a way to pass them"* was the whole gap, and it had been sitting in the code as a description of
something nobody had built.

### Decision

**Values cross as text, as `{name, value}` pairs, and become their declared type inside Revit.**

Three things follow from that, and each was a live alternative:

1. **The client does not resolve anything.** It has no `Document`, so it cannot turn `"Level 1"` into a
   view, and giving it one would mean holding a Revit object across a wire — the same thing
   [D-29](DECISIONS.md) already refuses for chained values. It also keeps the client ignorant of which
   Revit release it is talking to.
2. **The pairs reuse the reader `needs` already goes through.** A flat object would have been the
   obvious JSON shape and would have needed a second hand-written parser in
   [`Json.cs`](../revit/Heron.Bridge/Json.cs). A second parser is a second thing to get wrong on a wire
   that is already parsed by hand.
3. **A name that matches twice is refused, never chosen from.** Revit lets two views share a name when
   their types differ, and a view template can carry the name of a view. Taking the first match is a
   wrong answer that looks exactly like a right one — the same failure the selection rule in
   `BindNeeds` already exists to prevent, and the same rule this repository states generally: an
   identifier is only an identifier if it is unique among the things it has to distinguish.

Supported today: `View`, `Level`, `Category`, `BuiltInCategory`, `Element` and the narrower element
classes some contract actually asks for — `WallType`, `FloorType`, `CeilingType`,
`FilledRegionType`, `HostObjAttributes`, `MEPCurveType`, `FamilySymbol`, `Phase`, `FilterElement` —
plus `string`, `int`, `double`, `bool`, and comma-separated lists of most of those. Anything else is refused **by name**, saying that
type has no way to be received yet — rather than failing somewhere inside generated code.

**`Element` is the one with a boundary inside it, and the boundary is the interesting part.** It
resolves to an element **TYPE**, by name, written the way the Properties palette writes it — `Basic
Wall: Generic - 200mm`, or the bare type name when that is unique. It refuses a particular wall or
duct, and it has to: **an instance has no name of its own.** `Element.Name` on one returns its
*type's* name, so a search over instances would match every element of that type — turning a missing
rule into a wrong answer, which is the one trade this whole decision exists to refuse. *"Which duct"*
is a question text cannot answer; the selection is the mechanism that can.

**Eight contracts were then narrowed to say what they meant** — 2026-09-09, the same day. `wallType`
is a `WallType`, `phase` is a `Phase`, and a declaration that says which kind confines the search:
*"Generic - 200mm"* is unique among **wall** types where the same name against every element type in
the model may not be. Each narrowed type was read out of the fragment's own body rather than guessed
from the need's name, and three of the eight are deliberately a **base class**, because the fragment
asking is polymorphic and narrowing further would break it:

| Declared | Because |
|---|---|
| `HostObjAttributes` | `CREATE_FROM_ROOM_BOUNDARIES` branches on `hostType is CeilingType` / `is FloorType` |
| `MEPCurveType` | `CREATE_ELECTRICAL_RUN` builds a cable tray **or** a conduit from the same value |
| `FilterElement` | `APPLY_VIEW_FILTER` says so in its own comment: a rule filter and a selection filter share a base class, and a view does not care which |

**Narrower than `Element` is the point; narrower than the fragment can use is a regression dressed as
precision.** `ParameterFilterElement` was the obvious reading of `filter` and it is the wrong one.

The classes are named with `typeof(...)` rather than looked up by string, so a class missing on one of
the eight releases is a **build failure** rather than a refusal in front of a model. `Element` itself
stays, for the needs that really are *"some type"* and for contracts nobody has narrowed yet.

### What this does NOT do

**It unlocks nine fragments outright**, the ones whose only caller value is a view. The other 279 want
a category, a parameter name, a level, a point — and each of those needs its own resolution rule, which
is the same argument as above repeated per type. `Category` and `Level` are the obvious next two.

**And it proves nothing.** A fragment that now RUNS is a fragment that has finally reached the starting
line. [D-30](DECISIONS.md) is unchanged: a positive case, a negative case, and a fingerprint.

### The stale sentence

The refusal quoted at the top said *"until there is a way to pass them"* — true when written, false the
moment this landed, and it would have sent the next reader looking for work already done. It now names
the missing **value** instead. A message that describes a gap has to be corrected when the gap closes,
or it becomes the most convincing wrong documentation in the codebase.

---

## D-55 — A fragment's preview is the run itself, rolled back

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner asking why he had to click something a fragment already does
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `Run(app, request, writing)`, [`HeronOperationRegistry`](../platform/Heron.Core/HeronOperationRegistry.cs), [D-19](DECISIONS.md), [Golden Rule 16](14-golden-rules.md), [Golden Rule 19](14-golden-rules.md)

### Context

Asked to set a graphic override by hand so a read fragment would have something to find, the owner
asked the obvious question: *"there is a fragment to do this work — why do I have to do it?"*

He was right that `OVERRIDE_GRAPHICS_IN_VIEW` exists. It could not run. **130 fragments at
`risk: MODIFY` were written, compiling on eight releases, and had no engine at all** — the tool
registry said so in its own words, predicting the operation that would fix it:

> *"Running a fragment that WRITES is a different operation that does not exist yet, and it will
> belong beside `move_elements` at Modify with a preview the user accepted."*

### The problem with "a preview the user accepted"

`move_elements` previews by **prediction**: it works out where a duct would land and describes it. That
works because moving something 200 mm is describable in advance.

**A fragment is arbitrary C#, and is not.** Nothing can say what `CREATE_ELECTRICAL_CIRCUIT` will do
without running it — which is the same reason the executor sends source rather than a name.

### Decision

**The preview is the run itself, inside a `TransactionGroup`, rolled back.**

- No `apply` — run for real, report exactly what happened, then `RollBack()`.
- `apply` — run and `Assimilate()`, so the whole group is **one** undo step.

This is **stronger than a prediction**, because nothing is guessed at: the counts are what the fragment
did, not what something thought it would do. Rolling back is the default; keeping is the deliberate act,
and a caller that forgets the flag gets the safe half.

**The answer says which happened.** A rolled-back write and a kept one produce identical counts — the
fragment genuinely did the work both times — so `applied` and a plain-English `verdict` go on the reply.
Somebody reading *"renamed 47 views"* and finding 47 unrenamed views would be right to distrust every
number Heron has ever given them.

### What was NOT done, and would have been faster

**The risk label was not changed.** The suggestion was reasonable — relabel `MODIFY` and it runs today —
and it is exactly what Golden Rule 19 forbids. The label is what makes Revit paint the **amber CHANGING**
banner rather than the blue READING one, and what trips the permission gate. Relabel a write as a read
and Heron changes a model while the screen says it is only looking. The fragments stayed `MODIFY`; the
engine learned to run `MODIFY`.

**It shares the read executor rather than living in `RevitWrite.cs`.** That is the one place this
repository's *"every write in one file"* rule is deliberately not followed, and the reason is drift:
choosing the document, binding the contract and compiling are four hundred lines, identical for both,
and a second copy would be exercised half as often. The transaction is nine lines. The gate is unchanged
and sits above the routing switch.

### Proved, not asserted

On `Snowdon-scratch_ajmal.al` (workshared local, 9,628 elements), Revit 2024:

| | Levels |
|---|---|
| before | 11 |
| `--write` | **11** — `created ElementId`, then rolled back |
| `--write --apply` | **12** — the level is there |
| one Ctrl+Z | **11** |

Revit's undo list showed **`Heron: create-level`** as a single named entry. `create-drafting-view`,
`create-revision` and `rename-workset` then ran the same way — the last on a workshared model.

---

## D-56 — The banner counts in flight off the dispatcher, because an End can arrive before its own Begin

**Status:** Accepted · **Date:** 2026-09-08 · **Found during:** the owner watching his own screen
**Affects:** [`HeronActivityBanner.cs`](../revit/Heron.Revit.Addin/HeronActivityBanner.cs), [`RevitDispatcher.cs`](../revit/Heron.Revit.Addin/RevitDispatcher.cs), [D-50](#d-50--revit-says-out-loud-what-heron-is-doing-to-it-and-whether-it-is-reading-or-changing)

### Context

The owner's screen read **"Heron AI is reading your model — Running a job: switch-active-project —
READING"** long after that job had returned. At the same moment `ping` answered in 2 ms and
`count_elements` read 34,509 elements. Revit was healthy and idle, and the banner said it was working.

That is precisely what [D-50](#d-50--revit-says-out-loud-what-heron-is-doing-to-it-and-whether-it-is-reading-or-changing)
was built to prevent — *"it comes down when Revit truly finishes, not when the caller gives up"* — and
`B6`–`B13` in [NEEDS-CHECKING.md](NEEDS-CHECKING.md) had been closed the day before with *"the banner
works, in every colour"*. So this was a case those checks did not cover, not an untested claim.

**A banner that lies about Revit being busy is worse than no banner**, because the next person to see a
real one will not believe it.

### What it actually was — and what it was not

The first suspicion was `switch-active-project`, which calls `UIDocument.RequestViewChange` — something
Revit performs *after* the operation ends, so the job might finish in a state the lowering did not
expect. **That was wrong, and checking it first was still right.** The cause has nothing to do with
view changes, and the fragment was innocent.

**`Begin` is posted and `End` runs inline, so the end of a job can overtake its own beginning.**

`Begin` is always called from a bridge listener thread, so `OnUi` posts it with `BeginInvoke` and it
waits its turn on Revit's dispatcher. `End` is called from `Execute`, which runs on Revit's *own*
thread, so `CheckAccess` is true and it runs **immediately, inline**. While Revit is inside `Execute`
nothing can be pumped — that is the whole reason the banner has to be raised early — so a `Begin` posted
during that window sits behind the very work it announces.

One `ExternalEvent.Execute` drains the **whole queue**, and the proving harness dispatches every
fragment as a pair of requests milliseconds apart. So both halves of a pair run inside one pass on
Revit's thread:

| | on Revit's thread | posted, still waiting | `_active` |
|---|---|---|---|
| 1 | — | `Begin(A)` runs | 1 |
| 2 | `Execute` starts A (890 ms) | | 1 |
| 3 | | `Begin(B)` **queued** | 1 |
| 4 | `End(A)` **inline** | `Begin(B)` still queued | 0 → outcome shown, hide timer started |
| 5 | `Execute` drains B, `End(B)` **inline** | `Begin(B)` still queued | 0 (the `> 0` guard swallows it) |
| 6 | `Execute` returns, dispatcher pumps | **`Begin(B)` finally runs** | **1** — hide timer cancelled, banner repainted |

At step 6 the banner is raised for a job that finished at step 5, and `RevitJob.TakeBannerEnd` — the
interlocked flag from D-50 — has already been spent. **Nothing is left that can ever lower it.**

Every later job then made it worse in a way that looks like the opposite: a new `Begin` took the count
to 2 and its `End` brought it back to 1, never to 0, so the card **repainted itself to whatever ran
last and stayed up**. Caught live on 2026-09-08 in Revit 2024 pid 1680 — the same session as the audit
trail — reading `transfer-object-styles-between-documents`, then `Counting what is in the model`, then
`report-category-overrides`, over an idle Revit each time.

That answers the three things the report said must not be guessed:

- **Not specific to `switch-active-project`,** and not about `RequestViewChange`. It needs only two jobs
  close enough together that the second is dispatched while Revit is inside `Execute`. That fragment was
  where it was *seen* because its 890 ms first run held the thread open long enough.
- **A later job does not clear it.** It repaints it. It stays until Revit is restarted.
- **No exception path skips the lowering.** All four paths reach `End`. The lowering was never missed —
  it was **delivered out of order**, before the raising it was meant to cancel.

### The decision

**The count is the truth, and the drawing merely follows it.**

1. **`Begin` and `End` move `_active` with `Interlocked` on the caller's own thread, before the hop.**
   Counting inside the posted action is what allowed a job to be ended before it was begun.

2. **The posted action only renders whatever the count says at the moment it finally runs.** An action
   that arrives late is then a no-op, which is exactly what it should be. `> 0` draws the working state,
   `0` draws the outcome and starts the hold — once, so a late render leaves a running hold alone
   instead of restarting it.

3. **`Dispatch` raises the banner before the job is enqueued, not after.** A queued job can be taken by
   an `Execute` already running for an earlier `Raise`, so between the enqueue and the raise the job
   could be finished and ended before it had ever been begun. `Begin` before `Enqueue` makes the
   increment happen-before anything that could decrement it, and the floor in `Release` can then never
   be reached.

4. **`Execute` guarantees both of a job's promises in a `finally`.** Only `RevitOperations.Run` had a
   catch of its own; the audit write and the reads around it did not, and anything throwing there
   escaped the loop — leaving the banner up *and* the caller waiting out its whole timeout for an answer
   that was never coming. Both are safe to reach twice: `TakeBannerEnd` is D-50's interlocked flag, and
   `Finish` re-sets an event that is already set. This closes a second, real hole that had not yet been
   hit.

### Proved, not asserted

`HeronActivityBanner.cs` has no Revit dependency, so the **real file** was compiled into a WPF harness
that drives it with the exact call pattern `RevitDispatcher` uses — `Begin` from a listener thread,
`End` inline from a blocked dispatcher — and read back the window's visibility four seconds after the
work, well past the 1400 ms hold. The same five cases were run against the file as it stood at `HEAD`
and against the fix, each case in **its own process**, so no verdict could inherit the previous one's
leak.

| Case | Before | After |
|---|---|---|
| 1 — one job on its own (control) | down, 0 in flight | down, 0 |
| 2 — a pair drained in one `Execute` | **STILL UP, 1 in flight** | down, 0 |
| 3 — caller gives up, four refusals, then the real end | down, 0 | down, 0 |
| 4 — twelve jobs from four listener threads | **STILL UP, 12 in flight** | down, 0 |
| 5 — a stray `End` with no `Begin` | down, 0 | down, 0 |

Three of the five are controls and stayed green throughout, which is what makes the other two mean
anything. Case 3 matters especially: the `still_running` path — the one D-50 deliberately leaves the
banner up for — was **not** the leak, and an earlier reading that said it was came from running the
cases in one process and inheriting case 2's stuck count.

**And then in Revit itself**, which is the only place the claim finally counts. Deployed to Revit 2024
and 2020, and driven from the bridge client: two `run_fragment_read` requests **back to back on one
connection**, which is the pair pattern the audit shows for every fragment run — a cold first run that
holds Revit's thread inside `Execute`, and a cached second one that arrives while that pass is still
open. The banner window was located inside `Revit.exe` by title and read with `IsWindowVisible` every
100 ms, so the verdict is scanned rather than eyeballed.

| Pair (session 2024/32312) | Shot 1 | Shot 2 | Banner |
|---|---|---|---|
| `find-views`, cold compile | 5,193 ms | 3 ms | up 0.2 s → **down 7.1 s**, still down after 20 s idle |
| `find-sheets` | 757 ms | 3 ms | up 0.3 s → **down 2.7 s** |
| `report-category-overrides`, refused `bad_request_value` | 5 ms | 3 ms | up 7.6 s → **down 9.1 s** |
| `find-views`, both cached | 3 ms | 3 ms | up 13.9 s → **down 15.5 s** |

Every cycle settles about 1.4 s after the work — the `OutcomeHold` — and nothing is left up. The
refusal pair matters as much as the successful ones: a job Revit turned down lowers the banner too. So
does the last row, where both shots take 3 ms: that is the tightest window of all, and it still settles.

The audit for those runs shows the pairs landing **3 to 65 ms apart**, which is the same fingerprint as
the 2026-09-08 entries that stranded it.

**Every project compiles on all eight releases, 2020 through 2027** (`python tools/check-compile.py`,
2026-09-08).

---

## D-57 — The Master Architecture document is a research brief, not a fifth part of the specification

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** the owner asking for the document to be studied and matched against the project
**Affects:** [`HERON_AI_MASTER_ARCHITECTURE.md`](../HERON_AI_MASTER_ARCHITECTURE.md), [32 — reconciled](32-master-architecture-reconciliation.md), [docs/README.md](README.md)

### Context

`HERON_AI_MASTER_ARCHITECTURE.md` arrived on 2026-09-09 in one commit, at the repository root, next to
the [Constitution](../HERON_CONSTITUTION.md) and the [README](../README.md) — the two files a reader
treats as binding. It references nothing already in this repository: not the four-part specification,
not a decision, not a Golden Rule, not a file.

**A document in that position, written in that register, will be read as the plan.** It is 24 sections
of confident architecture, and it describes a system whose purpose it states as *"AI engineering system
for the existing Heron/Revit development codebase"* — a harness that helps a developer work on this
codebase. **Heron AI is a BIM-modeller-facing platform.** Those are different products, and the
document gives a session no way to know which one it is holding.

The audit it demands of itself was then done — its own §3, §17 and §21 all require it before anything
is built — and the result is [32](32-master-architecture-reconciliation.md). **Nine of the platform
modules in its §6 already exist here. Four of those are stricter here than it asks for.**

### Decision

**It stays, unedited, as a research brief. It is not a fifth part of the specification and does not
supersede anything.**

Three things follow, and each had a live alternative:

1. **Its content is not edited to agree with the project.** The same rule the
   [Master Specification](00-master-specification.md) has always had — *never edited to "fix" it;
   changes are recorded as decisions* — applies to an incoming document for the same reason: an edited
   brief no longer shows what was actually proposed, and the disagreements are the useful part.
   **What it gets instead is a header saying where it stands and pointing at
   [32](32-master-architecture-reconciliation.md).**
2. **Its framing of Heron as a developer-assist harness is rejected outright**, not deferred. A session
   reading it alone would build a Roslyn code graph over `revit/` to help somebody refactor the add-in.
   That is off-mission and expensive, and *"not now"* would not have stopped it.
3. **Its engineering discipline is adopted where it is not already practice**, because Heron writes and
   runs C# against the Revit API **as its product** ([D-28](DECISIONS.md)). Its §14 Revit Validation
   Gate is about Heron's output, not about Heron's source, and that distinction is what makes most of
   the document apply after its framing does not.

### What this does NOT do

**It builds nothing.** [32 §4](32-master-architecture-reconciliation.md) names four gaps that are
genuinely missing, and the largest — the Context Manager and the six things beside it in
[19](19-context-and-cost.md) — has **no implementation of any kind**, which nothing in this repository
had said in one place before. Naming a gap is not closing one.

**It adds no row to [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md).** That register is for claims awaiting a
real Revit. Unbuilt work is a different thing, and the register loses its meaning the moment the two
are mixed.

**It moves no fragment status and touches no Golden Rule.** Reconciling a document proves nothing about
a model.

### The rejection worth writing down

The document's §6.4 asks for four API-verification labels — Verified, Project-proven, Version-sensitive,
Unverified. They are sensible in isolation and they are **refused**, because
[24 — The Unified Trust Model](24-trust-model.md) exists precisely to collapse six competing status
vocabularies into two orthogonal axes ([Q-34](OPEN-QUESTIONS.md)). Adding a seventh would undo the
largest single piece of clean-up in this specification, and everything those four labels carry is
already expressible as lifecycle × source plus the matrix's `CLAIMED`/`COMPILES` split.

**That is the shape of most of this reconciliation:** the incoming idea is not wrong, it is *already
here under a different name and with a harder edge* — and adopting it a second time would soften the
edge.

---

## D-58 — Heron measures what it can see, and the cost meter belongs to the host

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** the owner asking whether token cost could be read from the cloud, or the function removed
**Affects:** [`HERON-OPS-OBS-011`](28-agent-registry.md), [19 §7](19-context-and-cost.md), [`tools/measure-brain.py`](../tools/measure-brain.py), [D-01](DECISIONS.md)

### Context

[28](28-agent-registry.md) defined the Observability Agent as *"latency, token usage, model calls per
request, cost per request"*. Building the latency half
([32 §4.2](32-master-architecture-reconciliation.md)) made the rest impossible to ignore: **Heron makes
no model calls at all.** [D-01](DECISIONS.md) put conversation, intent, planning and summarisation in
Claude Code, and `heron_embed` runs a local model with no tokens, no account and no cost
([D-24](DECISIONS.md), [D-26](DECISIONS.md)).

So three of the four fields describe something Heron cannot see, and a file claiming that agent id
would have made [`check-metadata.py`](../tools/check-metadata.py) report the Observability Agent
**BUILT** while three quarters of its declared job stayed impossible.

**The owner asked the right question: could it be read from the cloud instead?** It cannot, and the
reason is not a policy one. A provider's usage API reports an **account total for a period**, not
*this request cost this much*. The per-request attribution — the only shape
[19 §7](19-context-and-cost.md) asks for, and the only shape that makes the visible cost meter
possible — exists solely inside the process that made the call. That is the host.

[D-26](DECISIONS.md) is **not** what blocks it, and saying so matters: project names, content and
reasoning may go to the cloud, only the RVT and RFA files may not. This is an arithmetic limit, not a
confidentiality one.

### Decision

**The row is split three ways rather than deleted.**

| Field | Owner |
|---|---|
| **Latency** | **Heron.** Measured today — [`measure-brain.py`](../tools/measure-brain.py) for the brain, `heron_gaps.py` for the Revit side |
| **Token usage · cost per request** | **Host-provided**, on the same footing as the four orchestrator agents [`check-metadata.py`](../tools/check-metadata.py) already prints every run |
| **Model calls per request** | **Replaced.** Heron cannot count the host's calls. It can count how often it answered with **no model needed at all** — the identity and cache routes of [`heron_search.py`](../brain/heron_search.py) |

**That third line is the substance and not a consolation.** [19 §5](19-context-and-cost.md) sets the
rule the original metric existed to protect:

> Steps 1 and 2 must be tried **before** any model is invoked, structurally — not as an optimisation
> added later. If step 4 is ever reached for *"select all ducts"* after the first time, something is
> broken.

*Model calls per request* was a proxy for that rule holding. **The share of requests Heron answered
deterministically measures the same rule directly**, from Heron's own side of the wire, with no
telemetry and nothing to configure. It is the better metric, and it was available all along.

### What this does NOT do

**It does not remove the cost meter.** [PROPOSALS](PROPOSALS.md) keeps it and
`HERON-KRN-TOK-015` keeps its budget job. What changed is where the number comes from: a host that
knows, rather than a Heron that would have to guess.

**It does not let `measure-brain.py` claim the agent.** The row still covers more than that tool does,
so its header stays `Heron-Agent: none` until the deterministic-answer share is served too.

### The shape of this, which has happened before here

A registry row asked for something the architecture had already made impossible, and nothing noticed
because **nothing had tried to build it.** [D-49](DECISIONS.md) has the same shape from the other
direction: two register rows, each correct alone, broken only in combination, found only by running
them. **A specification is checked by implementation, and a row nobody has implemented is a row nobody
has checked.**

---

## D-59 — Reading spans loaded links only when the modeller asks, and the answer says how many it read

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/check-revit-gate.py`](../tools/check-revit-gate.py) asking question 8 of the fourteen of all 360 fragments
**Affects:** 62 reading fragments, [`check-revit-gate.py`](../tools/check-revit-gate.py), [09 §4](09-skills-and-fragments.md), [D-30](DECISIONS.md), [D-52](DECISIONS.md), [D-54](DECISIONS.md)

### Context

The gate asked *are linked documents handled correctly?* of every fragment and **62 of them collect
from the host document, declare a reading risk, and say nothing about links anywhere.**

**In Qatar MEP work a model is federated as a matter of course.** The architecture is a link, the
structure is a link, and frequently the MEP a coordinator is checking is a link too. A fragment that
collects only the host returns a **confident smaller number** and nothing in the answer says a link was
skipped. That is the plausible zero this repository already legislates against
([D-52](DECISIONS.md)) wearing different clothes.

**The 45 fragments that WRITE are correctly excluded, and that is an API fact rather than a judgement.**
A linked element belongs to another document and cannot be changed through this one; you would have to
open the linked file. A writer collecting the host only is not under-reaching the way a reader is.

### Decision

**The modeller decides, job by job.** In the owner's words: *"modeller will decide and he know if need
to check from linked model or this model, they know."*

It arrives as an input, the way [D-54](DECISIONS.md) made a view an input. **Two fields, and the second
is not optional:**

| Field | Where | What it is |
|---|---|---|
| `includeLinks` | `needs`, `source: request` | The modeller's call. **Absent means host only** |
| `linksSearched` | `provides`, `int` | How many linked documents were actually read |

**`linksSearched` is the field that does the work.** The question's own warning was that *whatever is
decided, the answer has to say whether links were included.* A boolean echo of the input would not say
that — it would repeat what was asked for, not what happened. A count says both, and it makes the one
case that would otherwise be silent loud: **`includeLinks` set and `linksSearched` at 0 means "you asked
for links and none are loaded"**, which is a different answer from "host only" and must not read the
same.

### Why not the other two readings

**Not a platform rule spanning links by default.** It would change what **every recorded proof
measured**. A count taken on the host is not the count the same fragment returns afterwards, so every
[D-30](DECISIONS.md) fingerprint would be answering a question it was never signed for. Nothing is
re-proved to add a feature.

**Not a per-fragment declaration either, and this is the less obvious half.** It is honest, and it takes
the choice away from the person who has the model open. The same fragment is wanted both ways on
different days — *how many ducts on Level 2* is a host question when a modeller checks their own work
and a federated one when they check a coordination issue. A fragment that decided once serves one of
those, and the modeller would have to know which of two fragments to ask for. That is exactly the
knowledge [09](09-skills-and-fragments.md) exists to stop them needing.

### What this does NOT do

**It does not change what any fragment does today.** Absent input means host only, which is what all 62
already do and what every existing proof measured. Nothing is invalidated by this decision; the work is
additive.

**It does not make the 62 edits.** They are contract and implementation changes in C#, and this
repository cannot compile C# — [`check-compile.py`](../tools/check-compile.py) needs `dotnet` and there
is none here. **Writing 62 unverified collector rewrites would be the opposite of production-ready.**
So the rule goes into the gate, where it is checked every run, and the 62 become a tracked worklist
instead of a paragraph somebody has to remember.

**It does not settle nested links.** A link inside a link is a real Revit case and this decision does
not say whether `linksSearched` counts it. First implementation answers it against a real federated
model, not here.

---

## D-60 — A preview selects what it would change and what it would skip, up to 500

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** reading [`affaan-m/ECC`](https://github.com/affaan-m/ECC) at file level ([33 §5.1](33-external-repository-research.md))
**Affects:** [`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs), [`set-selection`](../brain/fragments/set-selection/), [Golden Rule 9](14-golden-rules.md), [Golden Rule 17](14-golden-rules.md), [D-55](DECISIONS.md)

### Context

ECC's `plan-canvas` skill opens a plan **in the human's browser** so they can point at the element they
mean instead of describing it, because *"move this, change that"* is easier pointed at than typed.

**For a modeller the browser is the wrong canvas and the right one is already open.** Heron's preview
today is a sentence — *"would move 34 ducts up 200 mm in Tower-A, skipping 6."* Everything needed to
show it instead is already in memory when the preview is offered:
[`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs) holds `preview.Ids` and `preview.Skipped`,
and [`set-selection`](../brain/fragments/set-selection/) already exists.

### Decision

**Yes — both sets, capped at 500 per set.** The owner: *"yes I think it's good."* Asked where showing
stops being better than telling, he set the number: **500**, roughly one busy MEP level.

**The skipped set is the half that earns this.** *"Skipping 6"* is the clause nobody reads carefully.
Thirty highlighted ducts and six in a second colour is `Q-46`'s question answered by showing instead of
by wording, and no sentence carries it as well.

**The cap applies to each set on its own.** A job that skips 4 out of 4,120 can still show the 4. Above
the cap the preview stays the sentence it is today **and says why** — *"4,120 elements, too many to
highlight"* — because a preview that silently stopped highlighting would be a plausible zero drawn on
the screen.

**500 is the default of a setting, not a constant.** The count that is a mess on a laptop on site is not
the count that is a mess on a workstation.

**The modeller's own selection is saved first and put back.** Nobody asked for this; it is recorded here
as the safe default so it can be overruled rather than discovered. A selection built over two minutes on
a busy federated model is work, and destroying it to show a preview trades one annoyance for a worse
one. It is restored when the preview is declined or expires. **Accepting is the one case where it is not
restored** — after a change lands, the elements that changed are what the modeller wants selected.

### Why Golden Rule 9 is not touched

Rule 9 gates the **confirmation path** in code: the token is minted for one preview, a non-matching
token is refused, and the set is re-counted against the live model before anything moves.

**Selecting elements writes nothing and mints nothing.** It changes what the modeller can see while
deciding, which is the one thing Rule 9 was never guarding. **A preview that highlights and a preview
that does not must accept and refuse exactly the same tokens** — and that is a test rather than a
promise.

### What this does NOT do

**It does not answer "approve with changes".** Taking *"yes, but 150 not 200"* and producing a new
preview at once is a better question than this one and a different change. The guarantee holds only
because the token is minted fresh, so it is a **new preview**, never a modified execution. That stays
open and is no part of this decision.

**It does not make the change.** `RevitWrite.cs` is C# and nothing here can compile it. The decision is
recorded and the shape is fixed; the edit belongs on a machine with `dotnet` and a Revit to prove it
against.

---

## D-61 — Only a run that came back may be cached, and re-indexing forgets what changed underneath it

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/measure-routes.py`](../tools/measure-routes.py) parsing the tree for real callers of `heron_search.remember()` and finding one, in a test
**Affects:** [`heron_search.py`](../brain/heron_search.py), [`measure-routes.py`](../tools/measure-routes.py), [19 §5–§6](19-context-and-cost.md), [05 §4](05-heron-brain.md), [D-30](DECISIONS.md)

### Context

`remember()` is the only function that writes the utterance cache, and **no production code calls it.**
Not `ask()`, not `find()`, not any MCP tool. So route 2 can never fire for a real user: the cache is
built, tested, indexed, and permanently empty — while [19 §5](19-context-and-cost.md) makes it **step 1**
of the pipeline that must run before any model is invoked, and [19 §6](19-context-and-cost.md) calls it
*"the one that pays for itself faster than any of the others"*.

**The obvious wiring is the dangerous one.** Have `find()` call `remember()` with whatever it just
returned, and a keyword hit — which [`heron_search.py`](../brain/heron_search.py) calls *"a candidate,
never a decision"* in its own words — becomes permanent. The next identical wording returns by route 2
and **never searches at all**. A wrong answer becomes the *fast* answer, which is exactly the
confident-wrong-retrieval failure [05 §4](05-heron-brain.md) built the keyword layer to avoid. **One
line of wiring would have built it.**

### Decision

**A wording may be remembered once the fragment it resolved to has RUN AND COME BACK. Nothing else
counts, and `remember()` enforces that rather than documenting it.**

```
remember(store, text, fragment_id, evidence)   # evidence has no default
```

**The missing default is the design.** A default would make the safe call and the dangerous call look
identical at the call site, and the dangerous one is what somebody reaches for while wiring this up in a
hurry. Every caller says what it knows, out loud, in the argument. Anything but `heron_search.RAN`
raises `NotEvidence`.

**The two rejected candidates, and why each fails on its own terms:**

| Candidate | Why not |
|---|---|
| **The user accepted it** | [D-01](DECISIONS.md) puts that conversation in the host, and the host does not call back into the brain. It is not a worse rule — it is a rule nothing can evaluate |
| **The identity route resolved it** | Safe and worthless. Those already answer in one lookup, so caching them saves nothing and adds a row that can go stale |

### What invalidates a cached wording

`recall()` already drops a row whose fragment has **vanished** — a cache must never resurrect one. It
could not see the other case: **a fragment that is EDITED keeps its id**, so the row still points at
something real and still answers, against code that has changed underneath it.

**So the row carries the fragment's `fingerprint()` at the moment it was written, and `index()` calls
`forget_stale()`.** Re-indexing is when Heron re-reads the files, so re-indexing is when a row written
against the old bytes dies.

**The hash is the same one [D-30](DECISIONS.md) uses to call a proof stale**, deliberately. A cached
wording and a recorded proof go out of date for exactly the same reason, and two mechanisms would
eventually disagree about when. A row with a NULL fingerprint — written before this existed — is
dropped rather than trusted: one lookup is a cheap price for never serving a row nobody was checking.

The check is in `forget_stale()` and not in `recall()` on purpose. **Route 2 is the fast route**, and
hashing implementation files on it would spend the saving it exists to make. `on_disk` is handed from
`index()` rather than reloaded, because [D-24](DECISIONS.md) says re-indexing must stay free.

**And the first version of `remember()` was 277 times slower than it needed to be.** It called
`FRAG.load_all()` — reading all 360 fragment files to use one — which put **1,522 ms** on a path a
modeller is waiting on. The store already knows the fragment's folder; loading that one costs 3.6 ms
with the hash itself at 0.2, so `remember()` now takes 5.5 ms.

**The saving is not the reason this is written down.** The slow version *looked* fine: `load_all()` is
what `index()` calls, it was already imported, and the cost appears only if somebody times it. That is
the shape worth keeping — the same one [D-49](DECISIONS.md) records from the other direction, where two
correct rows were broken only in combination and only a run found it.

### What this does NOT do

**It does not fill the cache.** The evidence only exists after a run, a run happens in the add-in, and
**no workflow id crosses the seam into the brain** — so nothing can join a wording to a run that
succeeded. `measure-routes.py` still reports route 2 as unable to fire, and per
[D-54](DECISIONS.md) its message was corrected in the same change: the reason moved from *"nobody has
decided"* to *"the evidence cannot reach here yet"*, and those send a reader to different places.

**The remaining work is one seam, now named.** [D-62](DECISIONS.md) built the brain's half of the
trail; the workflow id is the half that is not built. When it crosses, the cache fills and
`brain/heron_audit.py` can claim its agent row in the same change.

---

## D-62 — The brain writes its own audit file, and the reader that already merges does the merging

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/measure-routes.py`](../tools/measure-routes.py) being able to report the structural route share and never the live one
**Affects:** [`brain/heron_audit.py`](../brain/heron_audit.py), [`heron_brain.py`](../mcp/server/heron_brain.py), [`heron_gaps.py`](../brain/heron_gaps.py), [19 §7](19-context-and-cost.md), [Golden Rule 14](14-golden-rules.md)

### Context

`HeronAudit` is C# in the add-in, so **the trail only ever knew what reached Revit.** A request answered
entirely by the brain — `heron_resolve`, `heron_lookup`, `heron_capabilities` — left **no record at
all**, and the live route share was unmeasurable: the only half that says anything about how people
actually use Heron.

[Golden Rule 14](14-golden-rules.md) says every important autonomous operation must be auditable, and
**deciding which fragment answers a request is not a small operation.**

**Why it was a question.** [19 §7](19-context-and-cost.md) asks for *"one append-only record, many
readers"*, and the literal reading is two processes appending to one file across a C#/Python boundary
where no lock is shared. That is an interleaving problem with a partial line at the end of it — a
design, not a patch.

### Decision

**One file per writer, in one directory, merged at read time — because the reader already merges.**

`heron_gaps.read()` globs the audit directory for `audit-*.jsonl`, parses one JSON object per line,
skips a bad line rather than dying on it, and **sorts every entry by `at`**. It has always merged files
and does not care how many there are or who wrote them. `audit-brain-YYYYMM.jsonl` matches the glob, so
**nothing downstream changed** — not `heron_gaps.read()`, not `heron-backup.py`, not
`heron_validate.py`, and not one line of C#.

**This is not "two homes for one fact".** There is one home — the directory — and two files are how two
processes write into it without a shared lock.

### What is never written there

**Not the user's sentence.** The trail already carries project information and
[12 §5](12-security-and-permissions.md) puts it under the same egress rules as everything else. **The live
route share needs the route and whether it resolved; it does not need the words.** Recording the
sentence would add a class of content to an append-only, never-pruned file for a report that does not
read it.

The wording lives in the utterance cache instead — a local SQLite store, a different file, a different
lifetime, and one a person can delete without losing the trail.

**Numbers are written as numbers.** [`HeronAudit.cs`](../platform/Heron.Core/HeronAudit.cs) carries a
long comment about the day `ms` went in quoted and a reader compared `"9"` against `"6620"` as text.
Old lines keep their quotes for ever because the log is never pruned. This writer does not add to that
pile.

**A refusal is recorded as `ok: false` with its reason, never dropped.** A trail holding only the
successes makes a capability nobody provides look like one nobody asked for — which is the line the
Capability Gap Agent reads.

### What this does NOT do

**It does not claim `HERON-MCP-LOG-010`.** That row and `HERON-KRN-LOG-006` both say the trail is
**keyed by Workflow ID**, and no workflow id reaches the brain — every line goes out with an empty
`workflow`. [D-58](DECISIONS.md) set the precedent in the same words: a file stays `Heron-Agent: none`
until it serves the whole row, because `check-metadata.py` reporting an agent BUILT is a claim somebody
will rely on.

**It cannot break a request.** `heron_brain` reaches it through `_audit()`, which returns a no-op that
swallows the call if `heron_audit` cannot be imported, and `record()` returns False rather than raising
when there is nowhere to write. On Linux with no `APPDATA` and no `HERON_AUDIT` that is every call — the
developer machine writes nothing and pays nothing. **A logger that can break the request it was only
supposed to describe has the priority backwards.**

### Corrected on review, 2026-09-09 — three ways this was less than it claimed

An automated review of PR #44 found three, and all three were the same mistake in different clothes:
**a thing declared and then not finished.**

**1. Two of the four operations were unwired.** `context()` and a catalogue recorder were written and
never called, so `heron_context` and `heron_capabilities` left no trace at all. *"The brain side of the
trail"* was half of the brain.

**2. Every failed line was unclassified.** `heron_gaps.analyse()` buckets a failed row by its `error`
field, defaulting to `(none)`. This writer emitted `ok: false` with **no error at all**, so a request
Heron honestly could not answer inflated the failure count in the very report built to tell defects
from correct refusals apart. **That is `heron_gaps.py`'s own founding mistake repeated** — its loudest
error was the executor behaving correctly, and counting it as a gap would have commissioned work already
done. Three codes now exist and are classified as correct refusals: `no_capability`, `no_provider`,
`context_refused`.

**3. `measure-routes.py` claimed a metric it did not compute.** Its LIVE section counted **cache rows**,
which is inventory, not request-route frequency — while this decision's whole purpose was to make the
live share readable. It reads the trail now, and reports the deterministic share
([D-58](DECISIONS.md)'s replacement for *model calls per request*) directly.

**The shape is worth more than the three fixes.** Each was a claim written before the thing it described
was finished, and every one of them passed all three gates and the whole test suite. **A gate checks
what somebody thought to check.**

---

## D-63 — A want is recorded when a capability is asked for BY NAME and nobody provides it

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/check-reachable.py`](../tools/check-reachable.py) finding `heron_capability.want()` called from two tests and no production code
**Affects:** [`heron_capability.py`](../brain/heron_capability.py), [`heron_brain.py`](../mcp/server/heron_brain.py), [D-40](DECISIONS.md), [06 §6](06-heron-platform.md)

### Context

`want()` is the only writer of `capabilities_wanted`, and nothing called it. Its own docstring says what
that cost: *"this is what turns 'we have no fragment for that' from a silence into a finding."* So the
table was always empty.

**The live path is a different one and it works.** `heron_brain.catalogue()` computes gaps from the
skills' own declared `missing` capabilities, never from `capabilities_wanted`.

**[D-40](DECISIONS.md) is the rule the two are measured against:** *an edge is derived before it is
stored; store one only when it cannot be computed from an artifact on demand.* A skill's requirement is
computable and is computed. **A request for something no skill declares is not computable from any
artifact** — no pass over the library can find it, because it is not in the library. Only somebody
asking reveals it. That is the case `want()` exists for, and D-40 points at it rather than away.

### Decision

**Wire it up, at `heron_brain.resolve()`, and only for the case that is genuinely missing.**

That function already draws the distinction that matters, and it is the whole reason this is one line
and not a judgement call:

| What `resolve()` found | Recorded as a want? |
|---|---|
| A provider exists, this release is not on its list (`blocked_by_version`) | **No** |
| Nobody provides it at all | **Yes** |

**The first row is the important one.** A version wall is not a missing capability — a provider exists.
Recording it would put a capability on the gap report **that already exists**, and Agent HR would
commission a fragment to build it a second time. That is precisely the failure `heron_gaps.py` was
corrected for: its loudest error, `needs_unbound` at 38 of 176, is the executor behaving correctly, and
counting it as a gap would have commissioned work already done.

**The name recorded is Heron's own capability name, never the user's sentence** — the host asked for
`SET_MEP_SIZE`, so that is what goes in. Nothing about the user's words enters the store on this path,
which keeps it on the same footing as [D-62](DECISIONS.md).

### What this does NOT do

**It does not make `heron_lookup` record wants.** A lookup that resolves to nothing has no capability
name — only words — and `capabilities_wanted` is keyed by name. Putting a sentence in that column would
make it a different table with the same name, and the gap report reads it expecting capabilities.
**A failed lookup is a real signal and it needs its own shape, which this decision does not invent.**

---

## D-64 — A fragment that goes looking declares what it dropped, and the marker rides only on the empty answer

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [`tools/check-revit-gate.py`](../tools/check-revit-gate.py) question 14, narrowed from 143 fragments to 59
**Affects:** 59 reading fragments, [`check-revit-gate.py`](../tools/check-revit-gate.py), [D-52](DECISIONS.md), [D-54](DECISIONS.md)

### Context

Question 14 — *is rollback/error reporting clear?* — raised **143 of 360**. Too many to be a defect list
and too many to dismiss, so the rule was looked for in the library's own practice instead of reasoned
about, and it is there: a fragment that **writes** names what it refused 172 times out of 202 (85%); one
that **reads** does it 45 out of 158 (28%).

Narrowed to the shape [D-52](DECISIONS.md) is actually about — **goes looking, and can drop something on
the way** — it is **59**. One that counts a list it was handed cannot skip anything, however silent.

`filter-elements-by-type` returns `found: 0` when its exemplar has no type, and nothing separates that
from *"there are none of this type."* `FILTER_ELEMENTS_BY_CATEGORY` already solved it with
`unresolvedLevel`. One fragment in the library got there; 59 have not.

### Decision

**A validator rule, not guidance — and the third shape rather than 59 hand-written answers.**

Three parts, and the second is what makes the first affordable:

**1. The dropped-count is a declared output.** A fragment that collects and can drop declares a field in
`provides` that names what it dropped. **The rule is that one exists, never that a particular word
does** — `unresolvedLevel`, `skipped`, `refused` all satisfy it, because what was dropped varies by
fragment. A rule about the word would be a naming convention wearing a correctness rule's clothes.

**2. The marker rides only on the empty answer.** `code-review-graph`'s `uncertainty.py`
([33 §5.6](33-external-repository-research.md)) measured the cost the other way round from how it
feels: *"one short sentence on the empty case is a token saving, not a cost — it replaces a
multi-thousand-token fallback search with roughly thirty tokens of honesty."* Attaching it only when the
result is empty means **every answer that carries results stays byte-identical to today**. Nothing that
works gets longer.

**3. Capped.** Honesty with a ceiling cannot grow into a paragraph.

**And the blind-spot list stays data rather than conditionals**, so an entry is deleted when the gap
closes — [D-54](DECISIONS.md) built into the structure instead of relied on as a discipline.

### Where the rule lives now

`check-revit-gate.py`'s question 14 reads the **contract** as well as the code: a fragment declaring a
dropped-count in `provides` passes even where its code reads silent, and one declaring neither is named.
**The 59 are a checked worklist rather than a paragraph somebody has to remember**, and every fragment
written afterwards is held to the same question on the same run.

It is not in `heron_fragment.validate()` **yet**, and that is deliberate rather than a shortcut: 59
fragments would become invalid the moment it went in, and every gate in the repository would fail on a
library that is not broken. `validate()` is where it belongs **once the 59 are cleared** — and moving it
there is how the worklist is declared finished.

### What this does NOT do

**It does not make the 59 edits.** They are C# and this repository cannot compile C#. The rule is
checked; the answers are written where there is a `dotnet` and a model to prove them against.

---

## D-65 — Heron keeps the degraded-result rule and hands routing to the host

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** [D-58](DECISIONS.md) establishing that Heron makes no model calls
**Affects:** [19 §3–§4](19-context-and-cost.md), `HERON-KRN-MAV-017`, [24](24-trust-model.md), [D-01](DECISIONS.md), [D-30](DECISIONS.md)

### Context

[19 §3](19-context-and-cost.md) specifies a Model Router (task → model class) and
[19 §4](19-context-and-cost.md) a Fallback (provider unreachable → route elsewhere, **and mark the
result degraded**). Neither exists.

[D-58](DECISIONS.md) established that **Heron makes no model calls at all** — [D-01](DECISIONS.md) puts
every one of them in the host, and `heron_embed` runs locally with no tokens and no cost. A router here
would route nothing.

### Decision

**The split, not a yes or a no.**

| Clause | Owner |
|---|---|
| Task → model class | **The host.** It makes the calls, so it is the only thing that can choose between them. Retired from Heron's specification |
| Provider unreachable → route elsewhere | **The host**, for the same reason |
| **Mark the result degraded, so it never counts as evidence toward promotion** | **Heron.** Kept |

**The kept clause is not a leftover — it is the only one of the three that was ever Heron's.** Routing
is an operational choice about a resource Heron does not hold. *Never counts as evidence toward
promotion* is a statement about **what may be believed**, and belief is what [24](24-trust-model.md)
and [D-30](DECISIONS.md) govern. `HERON-KRN-MAV-017` keeps exactly that clause and loses the rest.

**A degraded run is a run that did not happen, for promotion purposes.** [D-30](DECISIONS.md) needs a
positive case, a negative case, a named model and a staleness fingerprint. A result produced after a
provider fell over is missing the one thing a proof is for: the guarantee that what ran is what was
meant to run. Promoting on it would make the lifecycle decorative, which is the same sentence
`heron_search.py` uses about running a DRAFT fragment off an exact word match.

### What this does NOT do

**It does not delete two sections and move on.** [19 §3–§4](19-context-and-cost.md) are marked as the
host's rather than struck, because a reader who finds them missing will re-specify them. **A
specification that quietly loses a section teaches nothing; one that says who owns it teaches the
boundary**, which is [D-58](DECISIONS.md)'s own lesson repeated on a second row.

---

## D-66 — Heron checks the licence of what it ships by reading the files, not the landing page

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** reading [`K-Dense-AI/scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills) at file level ([33 §5.9](33-external-repository-research.md))
**Affects:** [`tools/check-licence.py`](../tools/check-licence.py), [`tests/test_licence_check.py`](../tests/test_licence_check.py), [17](17-open-source-and-distribution.md), [09](09-skills-and-fragments.md), [D-08](DECISIONS.md)

### Context

**It is not hypothetical — it is a live example of the failure.** That project's README says it is MIT
and that you may *"modify, distribute, and use freely."* **Four of its 163 skills carry
`© 2025 Anthropic, PBC. All rights reserved.`** A fifth is MIT under a different copyright holder.
Nothing on the landing page says so; only listing the licence files does. **Their own skill scanner
checks security and never looks at a licence.**

Heron is walking into the same position. [17](17-open-source-and-distribution.md) publishes Heron under
Apache 2.0 ([D-08](DECISIONS.md)); [09](09-skills-and-fragments.md) plans **community packages**;
[Golden Rule 19](14-golden-rules.md) already names imported text as a source Heron reads. So Heron will
ship other people's words to other people, and **no Heron tool mentioned a licence.**

### Decision

**A gate, and its one rule is: read the files, not the landing page.**

[`tools/check-licence.py`](../tools/check-licence.py) walks every file of every fragment and skill,
finds the markers **in the content**, and compares them with what the unit **declared**. A declaration
that agrees with nothing is worth as much as no declaration — which is the entire finding above.

It reports three things and keeps them apart:

| Finding | What it means |
|---|---|
| **Reserves rights** | Redistribution is not granted. The one that matters most, and the one the live example carried while its README said the opposite |
| **An incompatible licence name** | Named, and not on the short compatible list |
| **A foreign copyright holder in something declared OFFICIAL** | The declaration and the file disagree |
| **Unmarked** | Reported separately and never merged with clean. *"No evidence of a problem"* and *"evidence of no problem"* are different findings, and a tool that merges them is the tool that project already has |

**It exits 1 on a finding.** Today it finds none: 370 units, all clean.

### A clean run proves nothing, so the checker is proved to fire

[`tests/test_licence_check.py`](../tests/test_licence_check.py) rebuilds the exact failure — a
reservation of rights in a nested file under a folder declaring `source: OFFICIAL` — plus an
incompatible name, a compatible one, and Heron's own copyright. **A licence checker that reports nothing
on a clean library and has never been shown a dirty one is a plausible zero
([D-52](DECISIONS.md)) wearing a gate's clothes.**

**It also locks a false positive.** The first copyright pattern matched this ordinary C#:

```csharp
var size = rule.GetCriterion(c) as PrimarySizeCriterion;
```

and reported `report-routing-preferences` as somebody else's work. `(c)` now counts only next to a year.
**A licence tool that cries wolf is a tool somebody turns off**, and this one would have been turned off
over a cast.

### What it deliberately leaves out

**`.claude/skills` is not scanned.** Those are the skills for *developing* Heron; they run on a
maintainer's machine and are covered by the repository's own `LICENSE` like any other source file.
Q-53 is about what Heron's **users** redistribute, and scanning the toolbox alongside the cargo would
bury the finding that matters under six that do not.

**It is not legal advice and does not read licence text for meaning.** It finds a reservation, a name
and a holder. That is the check nobody was running.

---

## D-67 — A point crosses as three millimetre numbers

**Status:** Accepted · **Date:** 2026-09-09 · **Found during:** the `create-*` family being the largest block of unprovable fragments in the library
**Affects:** [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs) `OnePoint`/`ManyPoints`, [`HeronUnits`](../platform/Heron.Core/HeronUnits.cs), [`generate-jobs.py`](../tools/generate-jobs.py), [D-54](DECISIONS.md), [D3 in NEEDS-CHECKING](NEEDS-CHECKING.md)

### Context

[D-54](DECISIONS.md) let a caller's value cross as text. `XYZ` was refused by name, and the refusal
said why:

> *"A point cannot be typed in yet. The Revit API works in feet and this library talks millimetres, so
> which unit the number is in has to be settled before one can be accepted — guessing it is exactly the
> mistake D3 exists to catch."*

That was a decision deferred, not a gap in the code, and it had become the **largest single block of
unprovable work in the library**: 34 needs across the `create-*` geometry family and every transform —
move, copy, mirror, rotate, array.

### Decision

**A point is three numbers in MILLIMETRES, comma separated. Several points are separated by
semicolons.**

```
    XYZ           "5000, 3000, 2800"
    IList<XYZ>    "0,0,0; 5000,0,0; 5000,3000,0"
```

**Millimetres, because that is what this library already says.** Nothing here is a new preference:

- [`HeronUnits`](../platform/Heron.Core/HeronUnits.cs) exists for exactly one job — millimetres to
  Revit's internal feet — and the ratio is exact by definition.
- **59 caller values** across the fragment library are named `...Mm`.
- **D3**, called in its own file *the single most important line in it*, is written **"200 mm. Not 200
  feet"**.

**And it was already decided, by the people who could not pass a point.** `array-elements-radial` takes
`centreXMm` and `centreYMm`; `place-detail-item` takes `atXMm` and `atYMm`. Those are points, split into
millimetre scalars because there was no way to send one. **The workaround named the unit; this only
writes it down.**

### A direction needs no separate rule, and that was checked

Six of the 34 needs are a *direction* rather than a position, and a direction has no unit — so the
obvious worry is that dividing it by 304.8 is meaningless. It is meaningless, and it is also **harmless**:
scaling all three components by one number does not change where a vector points.

That was verified rather than assumed. All six were read: `array-elements`, `move-to-ray-hit`,
`probe-around-elements` and `check-surface-fit` call `Normalize()`; `check-obstructions` hands it to
`ReferenceIntersector.FindNearest`; `place-family-on-face` uses it as a facing vector. **None uses the
magnitude.** So one rule covers both, and no contract has to declare which kind it meant — which matters,
because `XYZ` cannot say.

### Two separators, and why not one

Every other list in `FromRequest` is comma separated. A comma separated list of points is **ambiguous
the moment it is read**: `"0,0,0,1000,0,0"` is two points only if you already know they come in threes,
and a list with one number missing silently becomes a different, valid-looking list. A separator that
cannot express the mistake is worth more than consistency with the flat lists.

`IList<IList<XYZ>>` — one need in the whole library, `pointPairs` — stays refused. It would want a third
separator, and that is a decision to make when a second fragment wants one.

### The bound

Each ordinate is checked against `HeronUnits.MaxMillimetres` — 100 km — and a number past it is
**refused rather than converted**. A coordinate that far from the origin is not in any building: it is a
value that arrived in the wrong unit, or with a digit too many. That is the guard the move path already
applies to a distance, applied to a coordinate.

### What this does NOT do

**It does not check that a point is where the author meant.** A point typed in metres is a thousand
times wrong and looks exactly like a right one, and nothing downstream can catch it — which is why D3 is
a person with a tape measure and stays that way. `generate-jobs.py` therefore prints the unit **on the
blank line itself**, so somebody meets it while typing rather than after a run.

It unblocks **20 fragments** — the arrangeable library goes from 20 to 40 — and unblocking is not
proving. [D-30](DECISIONS.md) is unchanged.

---

## D-68 — A significant change states its intent before it is made, and is judged against it afterwards

**Status:** Accepted · **Date:** 2026-09-12 · **Found during:** building the improvement gate the
work-note plans asked for
**Affects:** [`check-change.py`](../tools/check-change.py), [`change-evidence.py`](../tools/change-evidence.py),
[`AGENTS.md`](../AGENTS.md), [`heron-ship`](../.claude/skills/heron-ship/SKILL.md), [13 §3a](13-testing-and-quality.md)

### Context

Every gate in this repository asks a question about **the repository**. None asked a question about
**the change**. A diff that compiles, passes every suite and quietly rewrites three unrelated
subsystems is invisible to all of them — and it is the shape an AI-written change arrives in most
often, because a model asked to fix one thing will happily tidy four others on the way.

[AGENTS.md](../AGENTS.md) already said *"make the smallest safe change"* and *"a defect you find while
doing something else is recorded, not fixed"*. Both were prose, and [34 §3](34-patterns-adapted.md)
records the lesson three separate outside projects taught this repository: **a rule in code beats a rule
in prose.**

### Decision

**Three fields, written down before the work: `intent`, `area`, `risk`.** At review time every changed
file is classified against them, and the classification is **structural, not lexical** — the layering
table in [`check-structure.py`](../tools/check-structure.py) decides whether a file is in a declared
part, in a part a declared part may depend on, or in neither.

**Only the last class raises.** Tests, documentation and build/config are counted and never questioned.
Build and config are separated **before** part membership, because a change to how Heron is built or
installed is the one class that must never hide inside another.

**And a change with no evidence record is `REVISE`, never `PASS`.** `change-evidence.py` captures the
same measurements before and after, compares them as **sets** rather than totals, and rules `KEEP`,
`REVERT` or **`NO CHANGE MEASURED`** — which is the honest verdict for a change whose effect nothing
measurable moved, and is not the same sentence as *"no harm done"*.

### Why three fields and not the ten the plan asked for

[29 §4](29-metadata-standard.md) sets the test for a new field: **would a script fail the build over
it?** Only these three survive it. Everything else the plan listed is either derived from the diff —
whether this needs a real Revit, which releases it touches, whether a public contract moved, whether
delivery is affected — or is prose a script cannot check, such as acceptance criteria. **A derived fact
beats a declared one**, which is the same rule [D-40](DECISIONS.md) applies to graph edges and
[`heron_capability.py`](../brain/heron_capability.py) applies to risk.

### The two properties that keep it honest

**It sets the homework and does not mark it.** `check-change.py` runs no gate; which gates ran is read
out of an evidence record. A tool that did both would one day mark its own.

**`change-evidence.py` cannot change anything.** It measures, compares and rules. The improvement loop
it serves — *measure, change one thing, measure again, keep or revert* — is safe to point at a prompt,
a fragment description or a routing hint precisely because the tool owning the ruling owns no mutation.
Core architecture, Revit write paths, trust rules, the installer and public contracts are outside it by
construction rather than by policy.

### The compensating control this replaces nothing of

**It is not a proof and says so on every run.** A clean scope report says a change stayed where it said
it would — not that what it does is right. [D-30](DECISIONS.md) is untouched: a fragment's behaviour
still needs a named model, a negative case and a fingerprint, and the gate returns
`NEEDS REAL REVIT PROOF` (exit 3, the same code [`tests/README.md`](../tests/README.md) uses for *could
not run here*) rather than a pass.

---

## D-69 — A script in `tools/` reads the code it checks, and that is not a layering violation

**Status:** Accepted · **Date:** 2026-09-12 · **Found during:** the first run of the Python half of the
layering check, which raised 34 imports
**Affects:** [`check-structure.py`](../tools/check-structure.py), [D-48](DECISIONS.md), [PROJECT-MAP §B](PROJECT-MAP.md)

### Context

`check-structure.py` has held a table of who may depend on whom since Step 1, and its own comment has
said since 2026-08-29 that it *"only ever ran against C# `ProjectReference`s, so the Python side has
never been checked here at all"*. That admission sat in the file for a fortnight while two thirds of the
repository went unchecked, and the comment itself warned why: **the omission read as a prohibition to
anybody who opened the file.**

Checking it raised **34 imports**, every one of them a gate in `tools/` reading `brain/`.

### Decision

**The table stays exactly as it is, and `tools/` is exempt from the Python pass, with the reason at the
exemption.** Its `set()` row was written for `ProjectReference`s and `tools/` has no project file, so it
had never applied to anything.

**A gate reads the code it checks.** [D-48](DECISIONS.md) settled that the three tools which parsed
`fragment.yaml` themselves each lost a malformed fragment silently, and that they must go through
`heron_fragment.load_all()` instead. Forbidding that import would make the honest way the illegal way.
It is the same reasoning already written beside the `Autodesk.Revit` rule a few lines below it: **a
script talks about the code rather than being it.**

**What tools reach is still printed** — as a note, not a problem. *Which* scripts read the brain is
worth being able to see; *that* a script reads the brain is not a defect.

### Why this is not a rule bent to fit the code

The test is [AGENTS.md](../AGENTS.md)'s: *a rule is never edited to match code that broke it.* Nothing
broke this rule, because the rule had never covered this case — `PROJECT-MAP §B`'s dependency table
lists `platform`, `revit`, `brain`, `mcp` and `tests`, and has never listed `tools` at all. The new pass
is stricter everywhere the rule does apply: `brain/` importing an `mcp/` module now fails the build, and
a module name owned by two parts is **reported rather than guessed**, because a guess in a layering
checker is a layering rule that is sometimes not applied.

---

---

## D-70 — Heron keeps a usage counter, on the machine, and it is numbers rather than a diary

**Status:** Accepted · **Date:** 2026-09-12 · **Found during:** Stage 8 of the RAG track, which could
not build half of itself without it
**Affects:** [R-16, R-25](work-notes/plans/rag/01-requirements.md), [Q-C](work-notes/plans/rag/00-structure.md),
[05 §4.4](05-heron-brain.md), [D-26](#d-26--the-model-file-is-never-uploaded), [Golden Rule 11](14-golden-rules.md)

### Context

[`05 §4.4`](05-heron-brain.md) says re-rank by **status, success rate, recency and version match**.
Status and version match exist. **Success rate and recency do not exist at all, because nothing records
them** — and recording them means Heron keeps a history of what was asked and what worked, which
[02 §11](work-notes/plans/rag/02-implementation.md) refused to let anybody build until the owner had
decided it in writing. **No weight moved while it was open**, through the whole of Stage 8.

He asked the sharper question back: **"Claude already does this — why do we need it on the Heron side?"**
Three reasons, and they are why Claude's memory is not a substitute:

1. **Heron has to answer with no connection** — a site visit, a basement, a locked-down network. Claude
   is not there. Heron is.
2. **It is a sorting number, not a conversation.** It changes which clause comes first *inside* Heron's
   retrieval, and nothing outside the store can reach that arithmetic.
3. **Nothing already covers it, and that is why this decision has to say it.** An earlier draft of this
   row cited [D-26](#d-26--the-model-file-is-never-uploaded) as though it already forbade sending a
   usage history. **It does not, and D-26's own header warns against exactly that reading**: it was
   refined three times *away* from *nothing may travel* toward **the file may not travel**, and its
   final rule says *"any project name, data, typing, or content being in the cloud is not an issue."*
   **Local-only for this counter is therefore a NEW constraint introduced here**, not an inheritance —
   caught by review before this decision landed.

### Decision

**Yes — Heron keeps a usage counter. It lives on the user's machine and nothing about it is sent
anywhere.**

**What was agreed is a COUNTER, and the shape matters as much as the answer:**

```
QCS clause 21.4   helped 12 times
QCS clause 9.2    helped 0 times
```

Numbers against a clause id. His own instruction on note-keeping, given the same day, is the reason the
shape is written down here rather than left to the implementer: *"if we keep everything by note that
will be big."* **A counter does not grow with use the way a log does.**

**That illustration is the SHAPE HE AGREED TO, and it is not yet a sufficient specification.**
[`05 §4.4`](05-heron-brain.md) asks for **success rate** and **recency**. A bare cumulative count has
**no denominator**, so no rate can be computed from it, and **no timestamp**, so no recency can. Review
caught this before anything was built. **The exact fields are therefore left open** — at minimum a
count needs something to divide by and a last-used time — and **R-25 is not buildable until they are
settled.** What is settled is the ANSWER and the character of the thing: counts against a clause id,
not a diary of sentences.

**Three constraints follow and are not optional:**

| | |
|---|---|
| **NOT in the disposable index** | [Golden Rule 11](14-golden-rules.md) says the index is *rebuilt* if destroyed, so deleting it is always safe. **A usage count cannot be rebuilt from the source documents — it is canonical, not derived.** Putting it in the scope store would make deleting the index silently destroy learned ranking, which is the guarantee broken rather than kept. The counts live **outside** the disposable store — [Golden Rule 14](14-golden-rules.md)'s append-only local audit is the natural home — and the per-scope ranking value is derived from them. An earlier draft of this row had it inside the store and called that "deletable"; **review caught it** |
| **Per scope** | one counter inside each store. A counter pooled across scopes would carry what a company store learned into a project answer, which is [Golden Rule 5](14-golden-rules.md) broken by the back door |
| **Never leaves** | **New here, on his words** — *"Your project data must stay on your PC"* was the third reason put to him and he answered yes to it. Not sent, not synced, not attached to a question going to a model. **This is D-70's own rule; [D-26](#d-26--the-model-file-is-never-uploaded) does not reach it** |

### What this does NOT settle, and must be asked separately

**Whether the QUESTION TEXT is stored.** It was described to him twice — once as a line holding the
question, and once, later and more precisely, as a counter against a clause id. **He said yes to the
counter.** Keeping the sentences a person typed is a materially different thing from keeping a tally
against a clause number, and it is not covered by this decision. **Anybody building R-25 stores the
clause id and the count; storing the question text needs its own answer.**

**The weights themselves are still not set.** This decision unblocks R-16 and R-25 — it does not
prescribe how much the new signals are worth. The nudge is bounded to less than one rank of fusion and
its first version was **eight times too big**, caught only because a test asserted the arithmetic rather
than the intention. The same discipline applies to these two.

---

## D-71 — Every length a caller types is millimetres, and the fragment converts it

**Status:** Accepted · **Date:** 2026-09-13 · **Decided by:** Ajmal PS, asked directly
**Found during:** the first build of his own "let Heron make what it needs to test itself" method
**Affects:** 23 fragment implementations, [D-20](#d-20--millimetres-to-feet-is-arithmetic-not-unitutils),
[D-67](DECISIONS.md), [FRAGMENT-ISSUES row 27](FRAGMENT-ISSUES.md)

### Context

`create-wall` was asked for `height=3000`, meaning three metres. It built four walls **914400 mm**
high, reported success, and was already `PROVEN`.

The plan was exactly right — 8200 × 6200 — because the add-in converts an `XYZ` at the boundary and
**cannot convert a plain `double`**: nothing in a contract says which doubles are lengths. So the 3000
went straight through and `Wall.Create` read it as 3000 **feet**.

Measured across all 360 contracts the same hour, **81 length-shaped scalars are taken at
`source: request`, following two opposite conventions with nothing declaring either**:

- **~50 are named `...Mm`** — `spacingMm`, `offsetMm`, `marginMm` — and their implementations divide
  by 304.8, sometimes through a local called `MmToFeet` or `mmPerFoot`.
- **30 are bare** — `height`, `distance`, `width`, `tolerance` — and their implementations use the
  number as internal feet.

**The proof that it was undecided rather than designed:** `create-level` takes `elevationMm` and does
`wantedMm / 304.8`. `create-levels`, the plural of the same job, takes `elevations` and uses each one
raw. Both were `PROVEN`.

[D-67](DECISIONS.md) settled this **for points** and said so plainly — *"a point is three numbers in
MILLIMETRES"* — and it never reached the scalars. The note in `create-grid` that mentions *"a factor of
304.8"* is about points too.

### Decision

**A number a caller types into a length input is MILLIMETRES. Always. Every fragment.**

The caller-facing surface already said so everywhere else and was the only half a modeller can see:
`OnePoint` is documented in millimetres, the add-in's refusal for a bad `double` reads *"Type digits
only - 250 or 250.5, not 250mm"*, and 50 needs carry the unit in their own name.

**The fragment converts, not the boundary.** `FromRequest` sees a `double` and a name; it does not know
whether that double is a length, an airflow or a count, and a contract field saying so would have to be
added to 81 needs and then trusted. The fragment knows what its own number means. One line, at the top,
before first use:

```csharp
// MILLIMETRES IN, FEET INSIDE (D-71).
const double MillimetresPerFoot = 304.8;
height = height / MillimetresPerFoot;
```

**The `...Mm` suffix stops being load-bearing and is kept only because renaming 50 needs would break
every job file and every saved proof for no gain.** A bare name now means the same as an `Mm` name. The
rule is the declaration.

### The one exception, and it is a refusal rather than an oversight

**`select-by-numeric-parameter.tolerance` is NOT converted**, and its own header says why better than
this row can:

> *"A length is decimal feet, so 500 mm is handed in as 500 / 304.8. An airflow, a pressure, an angle
> and a temperature each have a different internal unit, and the only way to know which one this
> parameter uses is a units API — the one that changed shape at 2021, which is why D-20 keeps
> conversion at the edge. Accepting millimetres here would be a promise this code cannot keep."*

That fragment compares against **whatever parameter the caller named**. It is the one place where
"millimetres" is not a fact about the number, and it stays in internal units with the read-back
stating both.

### What this costs, and it is not hidden

**Seventeen `PROVEN` fragments change code**, so seventeen proofs are now about a version that no
longer runs. They are not demoted by this row — but `heron_validate` flags a fragment whose code moved
under its proof as `RE_PROVE`, and that is the correct reading. Anything whose proof measured a
*number* rather than a *count* has to be run again.

**`report-coverage` changes what its areas mean.** `areaEach` and `areaTotal` are
`PI * coverageRadius²`, and with the radius now in feet they are square feet where they used to be
square millimetres. That is not a regression: the comparison beside them,
`nearest > 2.0 * coverageRadius`, was reading a Revit distance in **feet** against a radius in
**millimetres** and was simply wrong. Consistency is the fix; the result's own unit is a separate
question and is not settled here.

### What this does NOT do

**It does not make a wrong number right.** A height typed in metres is still a thousand times too
small, and nothing here can see that. The guard that exists is [D-67](DECISIONS.md)'s 100 km bound on a
coordinate, and it applies to points only.

**It does not touch angles, counts, flags or flows.** `angleRadians` is radians, `maxFiles` is a count,
`pixelWidth` is pixels. The rule is about **length**, and a scalar that is not a length is not covered.

---

## D-72 — Four values a caller could not type are now built from what they type, and a face still is not

**2026-09-14.** Supersedes the last paragraph of [D-67](#d-67--a-point-is-three-numbers-in-millimetres)'s
*"Two separators, and why not one"*, which said `IList<IList<XYZ>>` **stays refused** pending a second
fragment wanting a third separator. No second fragment arrived. The owner asked for `create-line` by
name instead, which is the same signal a second fragment would have been: somebody wants the thing.

### Decision

**Four types leave the refused list. One stays, and the difference between them is the whole row.**

| Type | Written as | Fragments it unblocks |
|---|---|---|
| `IList<IList<XYZ>>` | `"0,0,0; 5000,0,0 \| 0,0,0; 0,5000,0"` — a PIPE between pairs | `create-line` |
| `OverrideGraphicSettings` | `"halftone=true; transparency=50"` — nine settings | `override-graphics-in-view`, `set-category-graphics`, `set-link-graphics` |
| `ForgeTypeId` | `"Length"`, `"Text"`, `"YesNo"` | `create-global-parameter` |
| `ParameterValue` | `"length 2700"`, `"integer 3"` — the KIND first | `set-global-parameter` |
| `IList<Reference>` | **nothing. It stays refused** | `place-family-on-face` |

**A list of element ids also takes the word `selected` now**, meaning the whole Revit selection —
where the singular `Element` takes `selected` to mean exactly one. That is not an inconsistency: the
ambiguity `OneElement` refuses (*which of three did you mean*) cannot arise when the answer is allowed
to be plural. It unblocks `filter-elements-by-id`, which
[FRAGMENT-ISSUES row 68](FRAGMENT-ISSUES.md) had already recorded as unblocked and was wrong about —
the word reached `OneElement` and never reached the id-list branch.

### Why a face is different from the other four, and not merely harder

`IList<Reference>` is the one that stays, and it is worth being exact about why, because "not
supported yet" reads the same for a rule nobody has written and a thing that cannot be written.

A `Reference` is **a face**: a particular solid, on a particular element, seen in a particular view.
It is produced by a mouse coming to rest on geometry. There is no text that names one — not a
name, not a number, not a coordinate — so the gap is not a missing parser. **The keyboard cannot say
it.** Reaching `place-family-on-face` needs Revit's own picking, which is a different mechanism from
everything in `FromRequest` and is not owed by this row.

The other four all had the same shape as each other: an object with no name to look up, which
therefore has to be **built** from what somebody types. Building one is a syntax decision, and a
syntax decision is exactly what can be deferred until somebody wants it — which is what D-67 did, and
was right to do.

### The kind comes first, because the value cannot say it

`"2700"` is a length, a count and a price depending on the parameter it is going into, and a built
`ParameterValue` is an object with that choice **already made inside it**. So the caller says which:
`"length 2700"`, `"integer 3"`, `"text Level 2"`, `"yesno true"`.

**And that is where a length is converted, which is [D-71](#d-71--every-length-a-caller-types-is-millimetres-and-the-fragment-converts-it)'s exception rather than a breach of it.** D-71 puts conversion in
the FRAGMENT because the fragment is what knows which of its own numbers are lengths. Here the value
crosses as a finished object, and `SET_GLOBAL_PARAMETER`'s contract says so itself — *"Already built,
carrying its own type. A length arrives in internal feet."* A fragment handed a `DoubleParameterValue`
cannot tell a length from a count, so it cannot be the one to convert. The only place that knows is
the line where somebody typed the word `length`.

### `ForgeTypeId` is built by reflection, and that is not a style choice

`ForgeTypeId` and `SpecTypeId` arrived at **Revit 2021**. `RevitFragment.cs` compiles for **2020 to
2027 from one source**, and naming either type in it would break the 2020 build outright — so the
value is fetched off `Autodesk.Revit.DB.SpecTypeId` by name at run time and returned as `object`.

The fragments that want one already declare `revit: ["2022", ...]`, so a caller on 2020 is never asked
for it; if one is, this refuses **in words** rather than the add-in failing to load. The build defines
`REVIT2020`, `REVIT2024` and so on, so a `#if` was available and was not used: a version `#if` in this
file would be the first, and the first one should not arrive as a side effect of a proving session.

### The unknown key is refused by NAMING what is accepted

`OverrideGraphicSettings` carries far more than nine members. Nine are accepted — the ones Revit's own
Visibility/Graphics override dialog puts in front of a modeller — and **anything else is refused with
the nine listed**, so a wrong spelling is one line away from a right one rather than a shrug.

The fill PATTERNS are deliberately left out. A pattern is an **element**, and naming one is a filter's
job rather than a string parse — the same split that keeps finding a `FamilySymbol` out of
`SET_SHEET_TITLE_BLOCK`. Their **colours** are here, because a colour is three numbers and nothing
else.

### What this does NOT do

**It does not prove anything.** Four types becoming typeable makes six fragments *arrangeable*. Every
one still owes a run against a model with both halves, and a signature that is a person's
([D-30](#d-30--a-proof-needs-a-positive-and-a-negative-and-a-person-signs-it)).

**It does not touch `place-family-on-face`.** That one is named above and stays where it is.
