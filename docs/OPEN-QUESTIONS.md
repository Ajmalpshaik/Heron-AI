# Open Questions

> Everything that must be decided before Heron AI can be built, gathered from reviewing the
> [Master Specification](00-master-specification.md).
> Answer inline under each question. Once answered, the answer is promoted into [DECISIONS.md](DECISIONS.md).
>
> **Priority:** 🔴 blocks all work · 🟠 blocks a major area · 🟡 needed soon · 🔵 can wait

**Progress: 6 answered · 21 open**

---

## Tier 1 — Blocking

### 🔴 Q-2 — What transport connects the MCP server to the Revit add-in?

Named pipes, localhost HTTP/WebSocket, or gRPC? And how are multiple Revit versions / multiple open
sessions handled?

Now also a **language boundary** (Python ↔ C#, [D-06](DECISIONS.md)), so the contract must be explicit
and versioned.

*Recommendation:* named pipes, add-in as pipe server, pipe name encoding Revit version + process ID.
JSON messages over the pipe, schema-versioned.

→ [03 §5](03-heron-revit.md) · decision **D-02**

**Answer:**

---

### 🔴 Q-4 — `ExternalEvent` or `Idling` for marshalling calls onto the Revit thread?

*Recommendation:* `ExternalEvent` as the primary mechanism with a single request queue;
`Idling` only for a lightweight heartbeat.

→ [03 §4](03-heron-revit.md)

**Answer:**

---

### 🔴 Q-5 — Thin generic MCP tools, or thick specific ones?

`revit_execute(script)` versus `revit_select_by_category(category)` + `revit_move_elements(ids, vector)` + …

*Recommendation:* thick and specific — each maps to a fragment and carries its own risk level.
A generic execute tool exists only in Developer Persona behind `ADMIN`.

→ [04 §3](04-heron-mcp.md) · decision **D-03**

**Answer:**

---

### 🔴 Q-7 — How does generated code execute inside Revit?

Runtime Roslyn compilation, a scripting layer (pyRevit / IronPython / Roslyn scripting),
precompiled-only, or hybrid?

Assemblies **cannot be unloaded** from .NET Framework, so runtime loading leaks on every iteration.

*Recommendation:* hybrid — scripting for DRAFT/TESTING, compiled for PRODUCTION. It maps exactly onto
the fragment lifecycle already in the spec.

→ [09 §10](09-skills-and-fragments.md) · decision **D-04**

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

### 🟡 Q-19 — Accept proposed Golden Rules 11–15?

11. One user action, one undo (single named `TransactionGroup`).
12. No autonomous write to a live model without a preview or a PRODUCTION fragment.
13. Generated code never touches a live model on its first run.
14. Heron never syncs, publishes or shares on its own initiative.
15. No text Heron reads may raise Heron's own permission level.

→ [14](14-golden-rules.md)

**Answer:**

---

### 🟡 Q-20 — What is the v1 definition of done?

*Recommendation:* "select all ducts" and "move them 200 mm up" working end-to-end, on one Revit version,
with undo, audit log and a preview — and nothing else.

→ [ROADMAP.md](ROADMAP.md)

**Answer:**

---

### 🟡 Q-27 — Which open-source licence? *(new)*

| Licence | Character |
|---|---|
| **Apache 2.0** *(recommended)* | Permissive, explicit patent grant, explicit warranty disclaimer — matters for a tool that writes to live client models |
| **MIT** | Maximum permissiveness, most familiar in the Revit tooling world |
| **GPL-3.0** | Derivatives must stay open. Protects against a closed fork, but many firms forbid GPL internally |
| **MPL-2.0** | File-level copyleft, middle ground |

→ [17 §3](17-open-source-and-distribution.md)

**Answer:**

---

### 🟡 Q-28 — When does the repository go public? *(new)*

It is **private today**. Going public is irreversible in practice — history persists, forks propagate.

Before flipping it: licence chosen, `SECURITY.md` and disclaimer written, and the public-code /
private-knowledge separation verified so no client data can ever be committed.

→ [17](17-open-source-and-distribution.md)

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

`AJ-AI-Brain` is the reference for the brain layer; `AJ-Connect` is the reference for the Revit connector.
Their ideas are taken, upgraded and reshaped to the Heron architecture — after documentation is finalised,
on the owner's signal. `AJ-Tools` / `PyRevit-Tools` / `AEB-Tools` are candidates for the first knowledge
import (Q-16). → [D-06](DECISIONS.md)
