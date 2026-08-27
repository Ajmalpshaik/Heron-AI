# 01 — Vision & Principles

> Derived from [Master Specification](00-master-specification.md) §1, §2, §43–45, §72, §74, §76.
> Annotations marked **[NOTE]** are engineering commentary added during review, not part of the original spec.

---

## 1. What Heron AI is

Heron AI is a **modular, agent-based, self-improving AI engineering platform** for BIM work.

It is explicitly **not**:

- a chatbot
- a coding assistant
- a plain MCP server
- a single monolithic "AI brain"

It is a system that lets a BIM professional describe work in ordinary language and have the full engineering pipeline — retrieval, code generation, validation, testing, execution, logging, learning — happen out of sight.

## 2. The single defining principle

> **The user focuses on BIM. Heron AI focuses on everything behind the BIM work.**

Every design decision in this repository is measured against that sentence. If a feature forces the user to think like a programmer, the feature is wrong.

## 3. What the user must never be required to know

C#, .NET, the Revit API, MCP, GitHub, RAG, vector databases, embeddings, fragments, skills, agent architecture, package management, build systems, deployment, version compatibility.

**[NOTE]** This is a *requirement on the UX*, not a claim that these things are absent. All of them exist inside Heron. The rule is that none of them ever surface unless the user has deliberately switched into Developer Persona.

## 4. The two personas

| Persona | Who | What they see |
|---|---|---|
| **BIM Modeler Persona** | BIM Modeler, BIM Coordinator, engineer | "Select all ducts." Progress. Result. Nothing else. |
| **Developer Persona** | Developer, tool author, Heron maintainer | Full technical surface — C#, .NET, Revit API, architecture, build output, agent chain. |

The **Communication Agent** detects intent, technical level and working role, then chooses the response style.

Core rule: **show the user what they need, not everything the system is doing.**

**[NOTE — open decision]** Persona should be *explicit and switchable*, not only inferred. Silent mode-switching by an AI is a common source of user distrust: the same question gets two different answers on two days and the user cannot tell why. Recommendation: infer a default, show it as a small persistent indicator, and let the user pin it. Tracked as [Q-15](OPEN-QUESTIONS.md).

## 5. Background intelligence

The user should not see agent chains, debugging, RAG operations, vector searches, compilation, compatibility checks, fragment evaluation, git operations or internal routing.

The user should see:

```text
Working...
Checking...
Completed.
```

If an important decision is required, Heron asks.

**[NOTE]** "Invisible" must not mean "unaccountable". The audit log (§55) is what makes invisibility safe: nothing is hidden from the *record*, only from the *screen*. Any operation that changes the model or the knowledge base must be reconstructable afterwards. See [12 — Security & Permissions](12-security-and-permissions.md).

## 6. The Golden Rules

The fifteen permanent architecture rules live in their own document: [14 — Golden Rules](14-golden-rules.md), together with four more proposed during engineering review. They are the constitution of the platform. Anything in this repository that contradicts them is a bug in the design, not a feature.

## 7. Target experience

> Open Revit. Open Heron. Say what you want.

- "Select all ducts."
- "Move them 200 mm up."
- "Add end caps."
- "Check the model."
- "Fix the issue."
- "Create dimensions."

## 8. Scope boundary — Revit first, not Revit forever

The core architecture must not be permanently welded to Revit. Future platform adapters: AutoCAD, Navisworks, IFC, Rhino, Civil 3D, Blender.

**[NOTE]** The practical way to honour this without paying for it up front: keep the *core* (orchestrator, brain, RAG, fragments, skills, memory, platform) free of any `Autodesk.Revit.*` reference, and put every Revit type behind the Heron Revit boundary. That single discipline costs almost nothing on day one and is what makes a second platform adapter possible later. It does **not** mean building abstractions for AutoCAD now.

## 9. Honest framing of ambition

**[NOTE]** This specification describes a mature platform, most likely several years of work. It is a **north star**, not a v1 backlog.

The correct reading:

- The **architecture** in this repository is committed to now — it shapes every file that gets written.
- The **agent catalogue** (~150 named agents) is a target organisation chart, reached over time.
- The **v1 scope** is deliberately small and is defined in [ROADMAP.md](ROADMAP.md).

Building 150 agents before one duct has been selected end-to-end would be the fastest way to kill the project. The first milestone is a single thin vertical slice through every layer.
