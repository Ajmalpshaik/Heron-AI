# Proposals — Part A

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part A — Gaps in Master Specification Part 1

The specification is unusually complete on *organisation* and *governance*. The gaps are almost entirely
in **execution reality** — the physics of running code inside Revit, and the economics of running many agents.

### ✅ A1. The Revit API threading constraint is not mentioned

The Revit API can only be called from the Revit main thread inside a valid API context. An MCP server is a separate process and **cannot call it at all**. Every operation must be marshalled back through an `ExternalEvent`.

*Consequence if ignored:* the entire bridge design is wrong and has to be rebuilt.

→ [03 §4](../03-heron-revit.md). **This is the single most important missing fact in the spec.**

### ✅ A2. No transport is defined between the MCP server and the add-in

Named pipes / localhost HTTP / gRPC — the choice affects security, multi-instance handling, and the whole tool layer.

*Consequence:* cannot start building. Recommendation: **named pipes**, add-in as server, pipe name encoding Revit version + PID.

→ [03 §5](../03-heron-revit.md)

### ✅ A3. "Agent" is used for both a service and an LLM call — *closed by Part 2 §17, §59, §83*

~150 agents, each an LLM call, makes "select all ducts" cost seconds and real money for what is one `FilteredElementCollector` query.

*Consequence:* the platform is unusably slow and expensive, and the architecture gets blamed for it.

Recommendation: three tiers — ~95 T1 deterministic services, ~25 T2 scoped calls, ~12 T3 agentic loops. The warm happy path should make **one** model call.

→ [02 §6](../02-architecture-overview.md), [08](../08-agent-catalog.md)

### ✅ A4. How generated code actually executes is undecided

Runtime Roslyn compilation, a scripting layer, precompiled only, or hybrid. Assemblies **cannot be unloaded** from .NET Framework (Revit ≤ 2024), so runtime loading leaks on every iteration.

Recommendation: **hybrid** — scripting for DRAFT/TESTING, compiled for PRODUCTION. This maps exactly onto the fragment lifecycle already in the spec.

→ [09 §10](../09-skills-and-fragments.md)

### ✅ A5. Where the platform actually runs is ambiguous

The install flow starts with "user installs Claude Code", implying a Claude Code plugin. But the target user is a BIM modeller who should not live in a terminal.

Recommendation: build the engine host-agnostic; ship the Claude Code front-end first (nearly free), add an in-Revit pane once the vertical slice works.

→ [02 §7](../02-architecture-overview.md)

### ✅ A6. Undo is never mentioned

The user must be able to reverse anything Heron did with one keystroke.

Recommendation: **Golden Rule 16** — one operation, one named `TransactionGroup`, one undo entry.

→ [03 §6](../03-heron-revit.md), [14](../14-golden-rules.md)

### ✅ A7. No preview before modification

Permission levels gate *whether* an action runs, never show *what it will do*.

Recommendation: dry run for every `MODIFY` operation — *"This will move 247 ducts up 200 mm. 12 are owned by another user and will be skipped. Proceed?"* Cheap to build, and the single feature most likely to make BIM professionals trust the tool.

→ [03 §10](../03-heron-revit.md)

### ✅ A8. The .NET 8 break at Revit 2025 is not acknowledged

Revit ≤ 2024 is .NET Framework 4.8; Revit 2025+ is .NET 8. One assembly cannot target both. Revit 2024 also moved `ElementId` to 64-bit.

*Consequence:* "support 2020–2027" is eight versions across a runtime break — a very large matrix for v1.

Recommendation: multi-target from one source tree; pick **one** version for the vertical slice.

→ [03 §8](../03-heron-revit.md)

### ✅ A9. Data confidentiality has no position

Every request potentially sends project content to a model provider. Much BIM consultancy work — especially government and defence work in Qatar — prohibits this contractually.

Recommendation: **hybrid, enforced structurally**. A project marked confidential must be *incapable* of egress: local embeddings, redaction at the boundary, hard `PUBLISH` block.

*Consequence if ignored:* the platform cannot be deployed on exactly the projects it is aimed at.

→ [12 §4](../12-security-and-permissions.md)

### ✅ A10. Permission enforcement location is unspecified

If the gate lives in the AI layer, a family name or an imported document containing instructions could in principle drive a `MODIFY` call on a live model.

Recommendation: enforce in the **add-in**, from a declared tool risk level. **Golden Rule 19** — no text Heron reads may raise Heron's own permission level.

→ [12 §3](../12-security-and-permissions.md)

### 🟠 A11. Testing against real Revit has no strategy

Levels 5–7 need a running Revit. It cannot run headless, CI runners do not have it, and licensing constrains where it may be installed.

Recommendation: mock the Revit boundary for levels 1–4; in-Revit test runner for 5–7; evaluate **Design Automation for Revit** before buying a machine room.

→ [13 §3](../13-testing-and-quality.md)

### ✅ A12. Autonomous agent creation has no hard safety boundary — *closed by Part 2 §10 Shadow Mode*

An AI that writes, approves and activates agents which then execute against live client models is genuinely dangerous — not dramatically, but quietly, on a submission day.

Recommendation: the system may only ever assign `PROPOSED`. A human approves activation. Always.

→ [06 §4](../06-heron-platform.md)

### ✅ A13. Product / data / derived are not separated in the workspace

The §60 tree mixes code that gets replaced on update with knowledge the user spent a year building.

*Consequence:* one update that "cleans and reinstalls" destroys the accumulated knowledge the platform exists to create.

→ [06 §2](../06-heron-platform.md)

### 🟡 A14. Building on the user's machine contradicts the user promise

Step 7 of the install ("Heron builds required components") requires the .NET SDK, MSBuild and Revit SDK assemblies on a BIM modeller's locked-down laptop.

Recommendation: build in CI, ship signed prebuilt artefacts, keep the build path for Developer Mode only.

→ [07 §3](../07-installation-and-update.md)

### ✅ A15. Skill vs Fragment is never defined

Both terms are used extensively; the boundary is left implicit.

Proposed: **Skill = what the user can ask for** (BIM language, user-facing). **Fragment = how it is done** (technical, internal, reused across skills).

→ [09 §1](../09-skills-and-fragments.md)

### ✅ A16. Fragment lifecycle has states but no gates

Without explicit promotion criteria, agents will promote inconsistently and Golden Rule 6 becomes decorative.

→ [09 §5](../09-skills-and-fragments.md)

### ✅ A17. Retrieval is vector-only — *closed by Part 2 §19 RAG Operating System*

BIM queries are full of exact tokens (`OST_DuctCurves`, `BuiltInParameter.RBS_*`, shared-parameter GUIDs) that embeddings handle badly. Semantic search returns the *nearly* right parameter — which in a model-modifying system is worse than nothing.

Recommendation: structured filter → hybrid keyword+vector → rank fusion → re-rank → exact-match short circuit.

→ [05 §4](../05-heron-brain.md)

### 🟡 A18. Worksharing is barely addressed

Real projects are worksharing-enabled. Element ownership failures, checkout, sync timing and workset visibility are **normal outcomes**, not errors.

→ [03 §9](../03-heron-revit.md)

### Closed since, by an Accepted decision - recorded 2026-10-18

The findings above are left as they were written on 2026-08-27. Each one marked ✅ without *closed by
Part 2* in its heading was closed afterwards by the decision named here; the marker was set on
2026-10-18 and nothing else in the finding was changed.

| | Closed by | What it settled |
|---|---|---|
| **A1** | [D-09](../DECISIONS.md) | `ExternalEvent`, one queue, one handler - and [Part 0b](part-0b.md) records it confirmed from the field |
| **A2** | [D-02](../DECISIONS.md) | Windows named pipes, the add-in as the server |
| **A4** | [D-04](../DECISIONS.md), [D-28](../DECISIONS.md) | hybrid; C# compiled at run time inside Revit's process while a fragment is unproven |
| **A5** | [D-01](../DECISIONS.md) | Heron runs as a Claude Code plugin; talking to the chat from inside Revit was decided and removed the same day ([D-104](../DECISIONS.md), [D-105](../DECISIONS.md)) |
| **A6** | Golden Rule 16, accepted 2026-08-28 ([Q-19](../OPEN-QUESTIONS.md)), and Article 8 ([D-43](../DECISIONS.md)) | one request, one named `TransactionGroup`, one undo entry |
| **A7** | Article 9 ([D-43](../DECISIONS.md)), with [D-126](../DECISIONS.md) and [D-138](../DECISIONS.md) | Revit selects what a change touched and asks Keep or Discard while the owner's *Ask to Keep* switch is on; off by default, a change is kept at once as one named undo entry |
| **A8** | [D-05](../DECISIONS.md) | Revit 2020 to the latest release, every one; the runtimes are set in `Directory.Build.props` |
| **A9** | [D-26](../DECISIONS.md), answering [Q-12](../OPEN-QUESTIONS.md) | the model file is never uploaded; the rest of the work travels like any conversation |
| **A10** | [D-106](../DECISIONS.md), with Golden Rule 19 ([Q-19](../OPEN-QUESTIONS.md)) and Article 14 ([D-43](../DECISIONS.md)) | the gate is in the add-in, which reads an operation's risk from its own tool registry by name; nothing Heron reads may raise its own permission level; writing stays off by default ([D-19](../DECISIONS.md)) |
| **A13** | [D-31](../DECISIONS.md) | product, data and derived were already separated, and the code is the record |
| **A14** - **partly, stays open** | [D-91](../DECISIONS.md) | product files come from a versioned GitHub release, nothing is built on the PC ([D-96](../DECISIONS.md)). **Not yet signed:** v0.1.0 went out unsigned on 2026-10-08, on the owner's word, and [Q-77](../OPEN-QUESTIONS.md) asks what makes signing due |
| **A15** | [D-29](../DECISIONS.md) | a skill is what is asked for; a fragment is a composable piece of how, not a whole answer |
| **A16** | [D-30](../DECISIONS.md) | one recorded proof against a named real model, with a negative case, not a count of runs |
| **A18** - **partly, stays open** | Article 12 ([D-43](../DECISIONS.md)) | element ownership is a normal outcome, reported and never seized. Checkout and workset visibility are not addressed by any decision |

**A11 is answered in part, so it keeps its marker.** [Q-14](../OPEN-QUESTIONS.md) answered the manual half,
with a written register in dependency order, [NEEDS-CHECKING](../NEEDS-CHECKING.md), and D-30 says what a
proof against a real model records. Q-14's own answer says what it does not cover: automated testing
against a real Revit, in CI.

---
