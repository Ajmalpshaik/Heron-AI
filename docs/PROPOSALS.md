# Proposals — Gaps, Additions & Ideas

> Engineering review of the [Master Specification](00-master-specification.md), carried out 2026-08-27.
> Nothing here overrides the spec. These are findings for the owner to accept, reject or defer.
> Each item states the gap, the consequence of ignoring it, and a recommendation.
>
> **Status legend:** 🔴 blocking · 🟠 important · 🟡 worth doing · 🔵 idea

---

## Part A — Gaps in the specification

The specification is unusually complete on *organisation* and *governance*. The gaps are almost entirely
in **execution reality** — the physics of running code inside Revit, and the economics of running many agents.

### 🔴 A1. The Revit API threading constraint is not mentioned

The Revit API can only be called from the Revit main thread inside a valid API context. An MCP server is a separate process and **cannot call it at all**. Every operation must be marshalled back through an `ExternalEvent`.

*Consequence if ignored:* the entire bridge design is wrong and has to be rebuilt.

→ [03 §4](03-heron-revit.md). **This is the single most important missing fact in the spec.**

### 🔴 A2. No transport is defined between the MCP server and the add-in

Named pipes / localhost HTTP / gRPC — the choice affects security, multi-instance handling, and the whole tool layer.

*Consequence:* cannot start building. Recommendation: **named pipes**, add-in as server, pipe name encoding Revit version + PID.

→ [03 §5](03-heron-revit.md)

### 🔴 A3. "Agent" is used for both a service and an LLM call

~150 agents, each an LLM call, makes "select all ducts" cost seconds and real money for what is one `FilteredElementCollector` query.

*Consequence:* the platform is unusably slow and expensive, and the architecture gets blamed for it.

Recommendation: three tiers — ~95 T1 deterministic services, ~25 T2 scoped calls, ~12 T3 agentic loops. The warm happy path should make **one** model call.

→ [02 §6](02-architecture-overview.md), [08](08-agent-catalog.md)

### 🔴 A4. How generated code actually executes is undecided

Runtime Roslyn compilation, a scripting layer, precompiled only, or hybrid. Assemblies **cannot be unloaded** from .NET Framework (Revit ≤ 2024), so runtime loading leaks on every iteration.

Recommendation: **hybrid** — scripting for DRAFT/TESTING, compiled for PRODUCTION. This maps exactly onto the fragment lifecycle already in the spec.

→ [09 §10](09-skills-and-fragments.md)

### 🔴 A5. Where the platform actually runs is ambiguous

The install flow starts with "user installs Claude Code", implying a Claude Code plugin. But the target user is a BIM modeller who should not live in a terminal.

Recommendation: build the engine host-agnostic; ship the Claude Code front-end first (nearly free), add an in-Revit pane once the vertical slice works.

→ [02 §7](02-architecture-overview.md)

### 🟠 A6. Undo is never mentioned

The user must be able to reverse anything Heron did with one keystroke.

Recommendation: **Golden Rule 11** — one operation, one named `TransactionGroup`, one undo entry.

→ [03 §6](03-heron-revit.md), [14](14-golden-rules.md)

### 🟠 A7. No preview before modification

Permission levels gate *whether* an action runs, never show *what it will do*.

Recommendation: dry run for every `MODIFY` operation — *"This will move 247 ducts up 200 mm. 12 are owned by another user and will be skipped. Proceed?"* Cheap to build, and the single feature most likely to make BIM professionals trust the tool.

→ [03 §10](03-heron-revit.md)

### 🟠 A8. The .NET 8 break at Revit 2025 is not acknowledged

Revit ≤ 2024 is .NET Framework 4.8; Revit 2025+ is .NET 8. One assembly cannot target both. Revit 2024 also moved `ElementId` to 64-bit.

*Consequence:* "support 2020–2027" is eight versions across a runtime break — a very large matrix for v1.

Recommendation: multi-target from one source tree; pick **one** version for the vertical slice.

→ [03 §8](03-heron-revit.md)

### 🟠 A9. Data confidentiality has no position

Every request potentially sends project content to a model provider. Much BIM consultancy work — especially government and defence work in Qatar — prohibits this contractually.

Recommendation: **hybrid, enforced structurally**. A project marked confidential must be *incapable* of egress: local embeddings, redaction at the boundary, hard `PUBLISH` block.

*Consequence if ignored:* the platform cannot be deployed on exactly the projects it is aimed at.

→ [12 §4](12-security-and-permissions.md)

### 🟠 A10. Permission enforcement location is unspecified

If the gate lives in the AI layer, a family name or an imported document containing instructions could in principle drive a `MODIFY` call on a live model.

Recommendation: enforce in the **add-in**, from a declared tool risk level. **Golden Rule 15** — no text Heron reads may raise Heron's own permission level.

→ [12 §3](12-security-and-permissions.md)

### 🟠 A11. Testing against real Revit has no strategy

Levels 5–7 need a running Revit. It cannot run headless, CI runners do not have it, and licensing constrains where it may be installed.

Recommendation: mock the Revit boundary for levels 1–4; in-Revit test runner for 5–7; evaluate **Design Automation for Revit** before buying a machine room.

→ [13 §3](13-testing-and-quality.md)

### 🟠 A12. Autonomous agent creation has no hard safety boundary

An AI that writes, approves and activates agents which then execute against live client models is genuinely dangerous — not dramatically, but quietly, on a submission day.

Recommendation: the system may only ever assign `PROPOSED`. A human approves activation. Always.

→ [06 §4](06-heron-platform.md)

### 🟡 A13. Product / data / derived are not separated in the workspace

The §60 tree mixes code that gets replaced on update with knowledge the user spent a year building.

*Consequence:* one update that "cleans and reinstalls" destroys the accumulated knowledge the platform exists to create.

→ [06 §2](06-heron-platform.md)

### 🟡 A14. Building on the user's machine contradicts the user promise

Step 7 of the install ("Heron builds required components") requires the .NET SDK, MSBuild and Revit SDK assemblies on a BIM modeller's locked-down laptop.

Recommendation: build in CI, ship signed prebuilt artefacts, keep the build path for Developer Mode only.

→ [07 §3](07-installation-and-update.md)

### 🟡 A15. Skill vs Fragment is never defined

Both terms are used extensively; the boundary is left implicit.

Proposed: **Skill = what the user can ask for** (BIM language, user-facing). **Fragment = how it is done** (technical, internal, reused across skills).

→ [09 §1](09-skills-and-fragments.md)

### 🟡 A16. Fragment lifecycle has states but no gates

Without explicit promotion criteria, agents will promote inconsistently and Golden Rule 6 becomes decorative.

→ [09 §5](09-skills-and-fragments.md)

### 🟡 A17. Retrieval is vector-only

BIM queries are full of exact tokens (`OST_DuctCurves`, `BuiltInParameter.RBS_*`, shared-parameter GUIDs) that embeddings handle badly. Semantic search returns the *nearly* right parameter — which in a model-modifying system is worse than nothing.

Recommendation: structured filter → hybrid keyword+vector → rank fusion → re-rank → exact-match short circuit.

→ [05 §4](05-heron-brain.md)

### 🟡 A18. Worksharing is barely addressed

Real projects are worksharing-enabled. Element ownership failures, checkout, sync timing and workset visibility are **normal outcomes**, not errors.

→ [03 §9](03-heron-revit.md)

---

## Part B — Feature ideas not in the specification

### 🔵 B1. Watch-and-learn skill capture

§34 learns from **repeated utterances**. Far more powerful: learn from what the user **does in Revit**.

The user performs a workflow manually once. Heron observes the document changes and offers: *"You selected 47 ducts by category and moved them up 200 mm. Want me to remember that as a skill?"*

This is how a BIM modeller naturally teaches a tool — by doing the job, not by describing it. It converts every user into a fragment author without any of them writing a fragment, and it directly serves Rule 1.

Technically feasible via Revit's `DocumentChanged` event. Non-trivial, high payoff.

### 🔵 B2. "What did Heron change?" report

A panel listing every change Heron made in this session — element counts, parameters touched, undo entries — with per-item revert.

Cheap (the audit log already holds it) and it is the first thing a BIM coordinator will ask for.

### 🔵 B3. Panic button — undo everything Heron did today

One control that rolls back the session's Heron transaction groups. Its existence changes how willing people are to try the tool at all.

### 🔵 B4. Explain mode for junior modellers

*"Explain what you just did."* → in BIM terms, not code: which categories, which filter, which parameter, and why.

Turns Heron from a black box into a training tool. For a company running junior modellers, that is a distinct commercial argument.

### 🔵 B5. Overnight batch mode

Run standards checks, model health reports and clash summaries across many models overnight, when Revit is free and nobody is competing for the machine.

Fits the Background Scheduler already specified, and it is the highest-value use of a licence that is otherwise idle for 14 hours a day.

### 🔵 B6. Visible cost meter

*"This session: 12 requests, 3 new fragments, $0.40."*

Users trust systems whose cost they can see. It also makes the T1/T2/T3 discipline self-enforcing — an expensive path becomes visible immediately.

### 🔵 B7. Team knowledge sharing before public community

§35 goes from personal knowledge straight to a GitHub PR into a public community. Most of the value lands earlier: **a company-internal shared brain**, on a network share or a private repository.

Same lifecycle, no confidentiality problem, and it is what an employer would actually pay for.

### 🔵 B8. Model health baseline

Record model statistics over time — element counts, warnings, file size, purgeable items — so Heron can say *"warnings have gone from 40 to 380 since last month."*

Trivial to collect, and it is the report BIM managers already produce by hand.

### 🔵 B9. Degraded / offline mode

State plainly what still works with no internet: proven fragments, cached skills, health checks, local retrieval. A tool that stops entirely when the connection drops will not be trusted on site.

---

## Part C — Strategic questions the spec does not address

| # | Question | Why it matters now |
|---|---|---|
| **C1** | **Commercial model** — open source, closed, freemium, company-internal? | Determines the licence, whether the community marketplace is even coherent, and whether the repo can stay private. |
| **C2** | **Who is the first real user?** | If it is the owner only, ship the terminal front-end and skip the installer for a year. If it is colleagues, the installer becomes v1 scope. |
| **C3** | **Name / trademark** | "Heron" is widely used in software. Worth a check before branding, packaging and a marketplace exist. |
| **C4** | **Liability** | If a Heron-generated change causes a defect in a delivered model, who is responsible? Needs an answer before anyone outside the author uses it on live work. |
| **C5** | **Relationship to the existing AJ-Tools / PyRevit-Tools / AEB-Tools repos** | Is Heron a rewrite, a wrapper, or their new home? This decides whether the knowledge import system is a v1 feature or a later one. |
| **C6** | **Autodesk App Store distribution** | If it is ever a goal, their review requirements constrain packaging and permissions — cheaper to know now than to retrofit. |

---

## Part D — The one recommendation that matters most

> **Build one thin vertical slice before building any department.**

"Select all ducts" → intent → fragment → MCP → `ExternalEvent` → Revit → selection changes on screen → result → audit log entry.

That single path touches every layer in the architecture and will settle A1, A2, A3, A5, A6 and A8 with facts rather than opinion. Everything in the master specification is easier to design correctly once it exists — and almost everything is guesswork until it does.

The full plan is in [ROADMAP.md](ROADMAP.md).
