# Open Questions

> Everything that must be decided before Heron AI can be built, gathered from reviewing the
> [Master Specification](00-master-specification.md).
> Answer inline under each question. Once answered, promote the answer into [DECISIONS.md](DECISIONS.md).
>
> **Priority:** 🔴 blocks all work · 🟠 blocks a major area · 🟡 needed soon · 🔵 can wait

---

## Tier 1 — Blocking. Nothing sensible can be built until these are answered.

### 🔴 Q-1 — Where does Heron actually run?

Is Heron a **Claude Code plugin** (skills + subagents + MCP server), a **standalone application** with its own chat UI docked inside Revit, or **both**?

The spec's install flow starts with "user installs Claude Code", but the target user is a BIM modeller who should not have to live in a terminal.

*Recommendation:* build the engine host-agnostic; ship the Claude Code front-end first because it is nearly free; add the in-Revit pane once the vertical slice works.

→ [02 §7](02-architecture-overview.md) · decision **D-01**

**Answer:**

---

### 🔴 Q-2 — What transport connects the MCP server to the Revit add-in?

Named pipes, localhost HTTP/WebSocket, or gRPC? And how are multiple Revit versions / multiple open sessions handled?

*Recommendation:* named pipes, add-in as pipe server, pipe name encoding Revit version + process ID.

→ [03 §5](03-heron-revit.md) · decision **D-02**

**Answer:**

---

### 🔴 Q-3 — Which Revit version is the target for the first working slice?

The spec lists 2020–2027. That is eight versions across a .NET runtime break (2025 moved to .NET 8) and an `ElementId` type change (2024 moved to 64-bit).

Which single version does v1 target? Which versions must v1.0 ship with?

*Recommendation:* pick one for the vertical slice — whichever you personally use most day to day.

→ [03 §8](03-heron-revit.md)

**Answer:**

---

### 🔴 Q-4 — `ExternalEvent` or `Idling` for marshalling calls onto the Revit thread?

*Recommendation:* `ExternalEvent` as the primary mechanism with a single request queue; `Idling` only for a lightweight heartbeat.

→ [03 §4](03-heron-revit.md)

**Answer:**

---

### 🔴 Q-5 — Thin generic MCP tools, or thick specific ones?

`revit_execute(script)` versus `revit_select_by_category(category)` + `revit_move_elements(ids, vector)` + …

*Recommendation:* thick and specific — each maps to a fragment and carries its own risk level. A generic execute tool exists only in Developer Persona behind `ADMIN`.

→ [04 §3](04-heron-mcp.md) · decision **D-03**

**Answer:**

---

### 🔴 Q-6 — What language is the server/brain written in?

The add-in must be C# (Revit API). The MCP server, orchestrator and brain could be **C#** (one language, shared types, one build) or **Python** (far stronger AI/RAG/embedding ecosystem, faster to iterate).

*Trade-off:* C# gives one codebase and no second runtime to install. Python gives a much better knowledge/RAG toolchain and quicker experimentation, at the cost of shipping a Python runtime alongside the add-in.

Your existing pyRevit work is relevant evidence here.

**Answer:**

---

### 🔴 Q-7 — How does generated code execute inside Revit?

Runtime Roslyn compilation, a scripting layer (pyRevit / IronPython / Roslyn scripting), precompiled-only, or hybrid?

Assemblies **cannot be unloaded** from .NET Framework, so runtime loading leaks on every iteration.

*Recommendation:* hybrid — scripting for DRAFT/TESTING, compiled for PRODUCTION. It maps exactly onto the fragment lifecycle already in the spec.

→ [09 §10](09-skills-and-fragments.md) · decision **D-04**

**Answer:**

---

## Tier 2 — Blocks a major area

### 🟠 Q-8 — Confirm the Skill vs Fragment definition

Proposed: **Skill** = what the user can ask for (BIM language, user-facing). **Fragment** = how it is done (technical, internal, reused across many skills).

→ [09 §1](09-skills-and-fragments.md)

**Answer:**

---

### 🟠 Q-9 — What promotes a fragment to PRODUCTION?

How many successful executions (suggest **N = 10**)? Who approves the final gate — always you, or can a company BIM lead approve for their team?

→ [09 §5](09-skills-and-fragments.md)

**Answer:**

---

### 🟠 Q-10 — Which vector store?

*Recommendation:* SQLite + `sqlite-vec` + FTS5 — one file per knowledge scope, zero install, and all three retrieval stages in one engine.

→ [05 §5](05-heron-brain.md)

**Answer:**

---

### 🟠 Q-11 — Local or cloud embeddings?

*Recommendation:* local by default — free re-indexing, works offline, and no project content leaves the machine. Cloud as opt-in.

→ [05 §6](05-heron-brain.md)

**Answer:**

---

### 🟠 Q-12 — What is the data confidentiality position?

What may be sent to a model provider, from which projects, and under what client agreements? Is a fully local/offline mode a requirement or a nice-to-have?

This determines whether Heron is deployable on Qatar government / Ashghal / defence work at all.

*Recommendation:* hybrid, enforced structurally — a project marked confidential is *incapable* of egress.

→ [12 §4](12-security-and-permissions.md)

**Answer:**

---

### 🟠 Q-13 — Where do product, data and derived files live?

*Recommendation:* product under an install location, data under the user profile or a chosen workspace, derived under a cache location — and the updater physically unable to write to the data class.

→ [06 §2](06-heron-platform.md)

**Answer:**

---

### 🟠 Q-14 — How is testing against real Revit done?

Manual in-Revit test runner, a self-hosted CI machine with licensed Revit versions, or Autodesk Design Automation for Revit?

→ [13 §3](13-testing-and-quality.md)

**Answer:**

---

### 🟠 Q-15 — Is persona automatic, manual, or both?

*Recommendation:* infer a default, display it, let the user pin it. Silent mode-switching is a common source of distrust.

→ [01 §4](01-vision-and-principles.md)

**Answer:**

---

## Tier 3 — Needed soon

### 🟡 Q-16 — Which existing repositories are imported first?

`AJ-Tools`, `PyRevit-Tools`, `AEB-Tools`, `AJ-AI-Brain` — which are in scope for the first knowledge import, and roughly how many tools/fragments do they hold?

This is what makes Heron useful on day one instead of after a year of accumulation.

→ [10 §5](10-memory-and-knowledge.md)

**Answer:**

---

### 🟡 Q-17 — Interface language

English only, or does Heron need to understand instructions in other languages used on site?

**Answer:**

---

### 🟡 Q-18 — Can community packages contain executable code?

Installing a package means running third-party code inside Revit, inside the user's project. Signing, source allowlist, version pinning — or declarative skills/fragments only, no executables?

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

*Recommendation:* "select all ducts" and "move them 200 mm up" working end-to-end, in one Revit version, with undo, audit log and a preview — and nothing else.

→ [ROADMAP.md](ROADMAP.md)

**Answer:**

---

## Tier 4 — Strategic, can wait but shapes everything

### 🔵 Q-21 — Commercial model

Open source, closed source, freemium, or company-internal only? Determines the licence, whether a community marketplace is coherent, and whether this repository stays private.

**Answer:**

---

### 🔵 Q-22 — Who is the first real user besides you?

If it is only you for the first year, the installer and the persona system can be deferred entirely and the terminal front-end is enough.

**Answer:**

---

### 🔵 Q-23 — Relationship to the existing AJ-Tools family

Is Heron a rewrite, a wrapper around them, or their new home? Decides whether knowledge import is a v1 feature.

**Answer:**

---

### 🔵 Q-24 — Name and trademark

"Heron" is widely used in software. Worth checking before branding, packaging and a marketplace exist.

**Answer:**

---

### 🔵 Q-25 — Liability

If a Heron-generated change causes a defect in a delivered model, who is responsible? Needs an answer before anyone else uses it on live project work.

**Answer:**

---

### 🔵 Q-26 — Autodesk App Store distribution

If ever a goal, their review requirements constrain packaging and permissions. Cheaper to know now than to retrofit.

**Answer:**

---

## Answered

*(Move questions here with their answers as they are resolved, then record the decision in [DECISIONS.md](DECISIONS.md).)*
