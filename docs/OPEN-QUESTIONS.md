# Open Questions

> Everything that must be decided before Heron AI can be built, gathered from reviewing the
> [Master Specification](00-master-specification.md).
> Answer inline under each question. Once answered, the answer is promoted into [DECISIONS.md](DECISIONS.md).
>
> **Priority:** 🔴 blocks all work · 🟠 blocks a major area · 🟡 needed soon · 🔵 can wait

**Progress: 13 answered · 25 open · none blocking Phase 0**

*(Five new questions — Q-29 to Q-33 — come from [Master Specification Part 2](00b-master-specification-agent-os.md).
None of them block Phase 0 either; they shape Phases 2–5.)*

---

## Tier 1 — Blocking

> ✅ **All clear.** Every question that blocked Phase 0 has been answered — see
> [D-01](DECISIONS.md) through [D-10](DECISIONS.md).
>
> **Phase 0 is unblocked.** It starts on the owner's go-ahead ([D-00](DECISIONS.md)).

One sub-decision remains open inside [D-04](DECISIONS.md), but it does not block starting:

### 🟠 Q-7a — Which scripting runtime for the sandbox?

[D-04](DECISIONS.md) settled *hybrid* — scripting while a fragment is in DRAFT/TESTING, compiled C# for
PRODUCTION. Which scripting runtime is still open: **pyRevit**, **IronPython**, **Python.NET**, or
**Roslyn scripting** (C# without compiling to an assembly).

The owner's existing `PyRevit-Tools` work is the strongest available evidence and should be reviewed
before choosing. Decidable during Phase 0 rather than before it, since Phase 0 generates no code.

**Research favours pyRevit.** A shipping Revit MCP server executes IronPython inside Revit via pyRevit's
built-in Routes server -- proven, maintained by someone else, and the owner already knows it
([26](26-prior-art-revit-mcp.md)).

→ [09 §10](09-skills-and-fragments.md)

**Answer:**

---

## Tier 2 — Blocks a major area

### 🟠 Q-8 — Confirm the Skill vs Fragment definition

Proposed: **Skill** = what the user can ask for (BIM language, user-facing).
**Fragment** = how it is done (technical, internal, reused across many skills).

→ [09 §1](09-skills-and-fragments.md)

**Answer:**

---

### 🟠 Q-9 — What promotes a fragment to PRODUCTION?

How many successful executions (suggest **N = 10**)? Who approves the final gate — always you, or can a
company BIM lead approve for their team? Now that the platform is open source, does a maintainer approve
community fragments?

→ [09 §5](09-skills-and-fragments.md)

**Answer:**

---

### 🟠 Q-10 — Which vector store?

*Recommendation:* SQLite + `sqlite-vec` + FTS5 — one file per knowledge scope, zero install, and all
three retrieval stages in one engine.

→ [05 §5](05-heron-brain.md)

**Answer:**

---

### 🟠 Q-11 — Local or cloud embeddings?

*Recommendation:* local by default — free re-indexing, works offline, and no project content leaves the
machine. Cloud as opt-in.

→ [05 §6](05-heron-brain.md)

**Answer:**

---

### 🟠 Q-12 — What is the data confidentiality position?

What may be sent to a model provider, from which projects? Is a fully local/offline mode a requirement
or a nice-to-have?

Sharper now that Heron is a public product: **other companies** will run it on **their** clients' models,
under NDAs you have never seen. The default must be safe for the most restricted user, not the least.

*Recommendation:* hybrid, enforced structurally — a project marked confidential is *incapable* of egress.

→ [12 §4](12-security-and-permissions.md)

**Answer:**

---

### 🟠 Q-13 — Where do product, data and derived files live?

Now critical: the repository is public, so **client data must be physically incapable of reaching it**.

*Recommendation:* product under the install location, data under the user profile, derived under a cache
location — and the updater physically unable to write to the data class.

→ [06 §2](06-heron-platform.md), [17 §2](17-open-source-and-distribution.md)

**Answer:**

---

### 🟠 Q-14 — How is testing against real Revit done?

Manual in-Revit test runner, a self-hosted CI machine with licensed Revit versions, or Autodesk Design
Automation for Revit? With eight supported versions ([D-05](DECISIONS.md)) this matters more than it did.

→ [13 §3](13-testing-and-quality.md), [16 §7](16-version-support-strategy.md)

**Answer:**

---

### 🟠 Q-15 — Is persona automatic, manual, or both?

*Recommendation:* infer a default, display it, let the user pin it. Silent mode-switching is a common
source of distrust.

→ [01 §4](01-vision-and-principles.md)

**Answer:**

---

## Tier 3 — Needed soon

### 🟡 Q-16 — Which existing repositories are imported first?

`AJ-Tools`, `PyRevit-Tools`, `AEB-Tools` — which are in scope for the first knowledge import, and roughly
how many tools/fragments do they hold?

Note: some are private and may contain client-specific work. Anything imported must be reviewed before it
can reach a public repository.

→ [10 §5](10-memory-and-knowledge.md)

**Answer:**

---

### 🟡 Q-17 — Interface language

English only, or does Heron need to understand instructions in other languages used on site?

**Answer:**

---

### 🟡 Q-18 — Can community packages contain executable code?

Installing a package means running third-party code inside Revit, inside the user's project. Signing,
source allowlist, version pinning — or declarative skills/fragments only, no executables?

Now a real security question rather than a hypothetical one, since anyone can publish.

→ [06 §10](06-heron-platform.md)

**Answer:**

---

### 🟡 Q-19 — Accept proposed Golden Rules 16–21?

16. One user action, one undo (single named `TransactionGroup`).
17. No autonomous write to a live model without a preview or a PRODUCTION fragment — including never triggering Sync With Central.
18. Generated code never touches a live model on its first run.
19. No text Heron reads may raise Heron's own permission level.
20. **Bind the document, not just the session.** *(field-proven)*
21. **Re-read before acting; a preview expires.** *(field-proven)*

Rules 20 and 21 come from failure modes actually observed in a working bridge — they are not speculation.

→ [14](14-golden-rules.md)

**Answer:**

---

### 🟡 Q-20 — What is the v1 definition of done?

*Recommendation:* "select all ducts" and "move them 200 mm up" working end-to-end, on one Revit version,
with undo, audit log and a preview — and nothing else.

→ [ROADMAP.md](ROADMAP.md)

**Answer:**

---

## From Master Specification Part 2

### 🟠 Q-29 — How does Shadow Mode work per agent type?

[Part 2 §10](00b-master-specification-agent-os.md) introduces Shadow Mode — an agent observes and
recommends without modifying production data. "Observe without modifying" means different things for
different agents, and needs defining:

| Agent type | Proposed shadow behaviour |
|---|---|
| Read-only (T1) | Run normally, compare output against the production agent |
| Analysis / ranking (T2) | Run in parallel, log both, score agreement |
| Model-modifying (`MODIFY`) | Produce the **preview only**. Never open a transaction |
| Code-generating (T3) | Generate and test in the sandbox. Never promote |

Also: how many shadow runs before `SHADOW MODE → APPROVED`, and does a human still sign it off?
*(Recommendation: yes — evidence plus a signature.)*

→ [18 §4](18-agent-operating-system.md)

**Answer:**

---

### 🟠 Q-38 — What is the exact install command? *(new)*

[07 §1a](07-installation-and-update.md) settles the *shape* of installation: one documented command that
fetches a **signed release**, never *"paste this URL and let the AI run what it finds"* — which is a
supply-chain attack pattern and the thing a contractor's IT department is trained to refuse.

What is not settled is the command itself:

| Option | Notes |
|---|---|
| **Claude Code plugin install** *(preferred)* | Matches [D-01](DECISIONS.md). Needs verifying against current plugin documentation — an install command that does not work is worse than none |
| **A release script** | `irm <release-url> \| iex` style. Works today, but is closer to the pattern being avoided and needs signing to be defensible |
| **Manual** | Download the release, run `deploy-addin.ps1`. Always available as the fallback, and what a cautious IT department will prefer |

Blocks nothing now — it is needed before the repository goes public ([D-10](DECISIONS.md)), because the
README's first command is the first impression.

**Answer:**

---

### 🟡 Q-37 — Can pyRevit Routes bind a per-process port? *(new, from research)*

pyRevit ships an HTTP **Routes server**, and a shipping Revit MCP server uses it as its entire bridge --
no custom add-in needed. Attractive: proven, maintained elsewhere, and the owner already uses pyRevit.

**But it listens on a fixed `localhost:48884`, which is single-instance by construction** -- exactly the
failure the owner's field notes describe and already fixed: *"every Revit tried to use one shared line
and the second one simply refused to start."*

**The question:** can pyRevit Routes bind a **configurable port per Revit process**, and can that port be
discovered?

| Answer | Consequence |
|---|---|
| **Yes** | pyRevit Routes becomes a viable transport, potentially replacing the custom bridge |
| **No** | It stays a **scripting-execution** option only (Q-7a). [D-02](DECISIONS.md) named pipes remain the transport -- multi-Revit is not negotiable |

-> [26](26-prior-art-revit-mcp.md)

**Answer:**

---

### 🟠 Q-36 — Lease or takeover when two chats target the same Revit? *(new, from the field)*

Today two chats on the same Revit **fight** — whichever speaks last takes over and cuts the other off.
Not a queue; a job running mid-way gets chopped. Harmless for a read, not acceptable for a `MODIFY`
mid-transaction.

| Option | Behaviour |
|---|---|
| **A. Warn only** | Detect the takeover and tell both chats. Cheapest; stops it being silent |
| **B. Lease** *(recommended)* | Second chat is **refused** — *"Revit 24312 is in use by another session"*. Small change, removes the hazard, fails closed |
| **C. Queue** | Second chat waits. Sounds nicer, behaves worse — an invisible queue runs a command minutes later against a model that has since changed |

A lease must never block a `MODIFY` **rollback** — cleanup always wins over the lease.

**Second justification, from the field:** the picker cannot currently show which Revit another chat is
using, which is why the user has to remember *"don't go to Revit, another session is running"* — a human
being used as a lock. A lease is what makes `(free)` / `(in use)` truthful in the list. Without it there
is nothing honest to display. ([D-16](DECISIONS.md))

→ [25 §3, §2a](25-multi-session-and-binding.md)

**Answer:**

---

### 🟠 Q-34 — Confirm the unified trust model? *(new)*

Four documents now define **six overlapping status vocabularies** for how much Heron trusts something.
Retrieval ranks by trust and promotion gates are defined per-vocabulary, so this must be settled before
anything is built.

*Proposal:* two orthogonal axes — **Lifecycle** (`DISCOVERED` → … → `ARCHIVED`, one vocabulary for
fragments, skills, capabilities and agents) and **Source** (`OFFICIAL` · `COMPANY` · `PROJECT` · `USER` ·
`COMMUNITY` · `IMPORTED` · `UNKNOWN`). Part 4 §47's four knowledge levels are kept as a derived band
used for ranking and for gating `MODIFY` operations.

→ [24 — The Unified Trust Model](24-trust-model.md) · decision **D-14**

**Answer:**

---

### 🟠 Q-35 — Confirm the Heron Constitution? *(new)*

Requested in [Part 4 §46](00d-additional-requirements.md). Written as
[HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md) — **30 Articles** across knowledge, the user's model,
boundaries, self-modification and conduct.

Reconciled with the Golden Rules rather than duplicating them: Golden Rules are design principles for
people; the Constitution is the runtime-enforceable subset written as prohibitions an agent can obey or
violate.

Worth reviewing specifically:

- Article 9 — *"show before you change"* — is a preview mandatory for **every** non-`PRODUCTION`
  `MODIFY`, or only above a size threshold?
- Article 23 — *"for any `MODIFY`, an answer with no evidence is refused, not downgraded"* — is refusing
  the right default, or too strict for early versions?
- Are 30 Articles too many to inject usefully? *(Mitigated by giving each agent only the Articles
  relevant to its permission level and department.)*

**Answer:**

---


### 🟡 Q-31 — What stores the dependency graph?

[Part 2 §41](00b-master-specification-agent-os.md) requires a graph over skills, fragments, Revit API
surfaces, runtimes, packages, capabilities and agents — so the blast radius of a change is computable.

*Recommendation:* **SQLite with recursive queries**, beside the knowledge store ([Q-10](#)). This is
ordinary relational data; a dedicated graph database is not warranted. Both specifications mention a
"Knowledge Graph", but §41 is the only place one is actually specified — build exactly this and not more.

→ [21 §1](21-resilience-and-operations.md)

**Answer:**

---

### 🟡 Q-32 — How far does multi-user / Admin Mode go?

[Part 2 §72–§73](00b-master-specification-agent-os.md) describe enterprise user management and enforced
policy. Heron is a single-user Claude Code plugin — there is no server to enforce anything.

| Option | Shape |
|---|---|
| **A. Single-user only** *(recommended now)* | Scopes are folders. "Company knowledge" is a shared repo each user syncs. No enforcement |
| **B. Company knowledge as a private git repo** *(recommended next)* | Admin = whoever reviews the pull requests. Uses machinery already specified in §38 |
| **C. Full enterprise server** | Central service, user directory, enforced policy. A different product — only on real demand |

Most of what a BIM manager actually wants — *"everyone uses our approved standards and tools"* — is
delivered by B without any infrastructure.

→ [22 §4](22-users-modes-and-extensibility.md)

**Answer:**

---

### 🟡 Q-33 — Confidence thresholds for asking the user

[Part 2 §22](00b-master-specification-agent-os.md) says knowledge conflicts fall back to asking the
user "if confidence is insufficient". [§55](00b-master-specification-agent-os.md) says to ask only at
meaningful boundaries.

Both are right, and both need a number. What confidence level triggers a question? And are answers
**recorded as decisions** so the same question is not asked again next week?

*(Recommendation: yes — an unanswered-then-re-asked question is worse than a guess.)*

→ [20 §4](20-knowledge-trust-and-conflict.md), [21 §3](21-resilience-and-operations.md)

**Answer:**

---

## Tier 4 — Strategic

### 🔵 Q-24 — Name and trademark

"Heron" is widely used in software. Worth checking before branding, packaging and an app-store listing exist.

**Answer:**

---

### 🔵 Q-25 — Liability

If a Heron-generated change causes a defect in a delivered model, who is responsible? Now a public-product
question, not a personal one. Partly addressed by an explicit disclaimer ([17 §5](17-open-source-and-distribution.md))
and by the licence choice (Q-27).

**Answer:**

---

### 🔵 Q-26 — Autodesk App Store requirements

Confirmed as a later goal ([D-07](DECISIONS.md)). Their review constrains packaging, permissions and
installer behaviour — cheaper to read the requirements before the installer is finalised than after.

**Answer:**

---

## Answered

### ✅ Q-30 — Who routes models — Heron or Claude Code? — *closed by Part 4 §24*

Part 4 §24's **AI Model Abstraction Layer** resolves this by separating two things that were conflated:

```text
Heron AI Interface  ->  Model Router  ->  Provider Adapter  ->  Model
    (Heron: intent)          (pluggable: host when hosted, Heron for batch work)
```

Heron always declares *intent* ("this needs strong reasoning"); resolution to a specific model is
pluggable. Under [D-01](DECISIONS.md) Claude Code resolves conversational work; Heron's Python side
resolves its own batch work through the same interface. Neither half hard-codes a model id.

It also makes local/cloud routing (§25) a configuration choice rather than an architectural one — a
project marked confidential selects a local provider adapter, and nothing above that layer needs to know.

→ [23 §8](23-heron-kernel.md), [19 §3](19-context-and-cost.md)


### ✅ Q-2 — Transport between MCP server and add-in? → **Named pipes**

C# add-in is the pipe server, Python MCP server is the client, pipe name encodes Revit version + PID.
Local-only by construction. → [D-02](DECISIONS.md)

### ✅ Q-4 — `ExternalEvent` or `Idling`? → **`ExternalEvent`, one queue, one handler**

`Idling` used only for a liveness heartbeat. Heron must surface "Revit is busy" rather than hanging.
→ [D-09](DECISIONS.md)

### ✅ Q-5 — MCP tool granularity? → **Thick and specific**

Each tool maps onto a fragment and carries its own risk level. Generic `revit_execute` only in
Developer Persona behind `ADMIN`. Capability discovery keeps the context cost down. → [D-03](DECISIONS.md)

### ✅ Q-7 — How does generated code execute? → **Hybrid**

Scripting sandbox while DRAFT/TESTING, compiled signed C# for PRODUCTION. The `PROVEN → PRODUCTION`
gate is where compilation happens. Sub-question Q-7a (which scripting runtime) remains open.
→ [D-04](DECISIONS.md)

### ✅ Q-27 — Which licence? → **Apache 2.0**

Chosen over MIT for its explicit patent grant and warranty disclaimer, which matter for software that
writes to live client models; over GPL because many construction firms forbid GPL internally.
→ [D-08](DECISIONS.md)

### ✅ Q-28 — When does the repo go public? → **When licence + safety files exist AND there is working code**

Safety files completed 2026-08-27. Remaining condition: Phase 0 working code. → [D-10](DECISIONS.md)

### ✅ Q-1 — Where does Heron run? → **Claude Code plugin**

Claude Code is the conversation layer and agent host. Heron supplies skills, subagents, an MCP server and
the Revit add-in. → [D-01](DECISIONS.md)

### ✅ Q-3 — Which Revit versions? → **2020 through latest, and every future release**

Accepts two API breaks (`ElementId` 64-bit at 2024, .NET 8 at 2025). Requires multi-targeting from one
source tree and an adapter layer from the first line of code. → [D-05](DECISIONS.md), [16](16-version-support-strategy.md)

### ✅ Q-6 — What language? → **C# for Revit, Python for the brain**

The language boundary sits exactly where the process boundary already had to be.
→ [D-06](DECISIONS.md)

### ✅ Q-21 — Commercial model? → **Free and open source**

Public GitHub, installable by anyone. Autodesk App Store later, also free. → [D-07](DECISIONS.md), [17](17-open-source-and-distribution.md)

### ✅ Q-22 — First users? → **Everyone**

Not personal tooling and not company-internal. The installer, health checks and persona system are
therefore real scope, and defaults must be safe for the most restricted user. → [D-07](DECISIONS.md)

### ✅ Q-23 — Relationship to the existing AJ-Tools family → **Upgrade and absorb**

The owner's earlier brain work is the reference for the brain layer, and his earlier connector work for the Revit bridge.
Their ideas are taken, upgraded and reshaped to the Heron architecture — after documentation is finalised,
on the owner's signal. `AJ-Tools` / `PyRevit-Tools` / `AEB-Tools` are candidates for the first knowledge
import (Q-16). → [D-06](DECISIONS.md)
