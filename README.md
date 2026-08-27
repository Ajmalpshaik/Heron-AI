# Heron AI

**A modular, agent-based AI engineering platform for BIM.**

> The user focuses on BIM. Heron AI focuses on everything behind the BIM work.

---

## What it is

Heron AI lets a BIM Modeler, BIM Coordinator or engineer drive complex Revit automation by describing
the work in ordinary language:

```
"Select all ducts."
"Move them 200 mm up."
"Add end caps."
"Check this model against our BIM standard."
```

Behind that sentence, Heron performs the analysis, knowledge retrieval, code generation, validation,
testing, execution, logging and learning. The user never has to know C#, the Revit API, MCP, RAG,
vector databases, build systems or version compatibility.

It is **not** a chatbot, a coding assistant, or a plain MCP server.

## Four parts

| Part | Responsibility |
|---|---|
| **Heron Revit** | The add-in, ribbon, and everything touching the Revit API |
| **Heron MCP** | The controlled bridge between the AI layer and Revit |
| **Heron Brain** | Skills, fragments, RAG, memory, knowledge |
| **Heron Platform** | Install, update, packages, registry, security, GitHub |

---

## Status

**Specification and planning. No implementation until the owner says to start.**

| | |
|---|---|
| Specification | ✅ Complete — [76 sections](docs/00-master-specification.md) |
| Architecture review | ✅ Complete — [18 gaps, 9 ideas](docs/PROPOSALS.md) |
| Open questions | ⏳ 6 answered, 21 open — [OPEN-QUESTIONS.md](docs/OPEN-QUESTIONS.md) |
| Roadmap | ✅ Drafted — [Phase 0 → 7](docs/ROADMAP.md) |
| Implementation | ⛔ Not started, by design ([D-00](docs/DECISIONS.md)) |

### Decided

| | |
|---|---|
| **Host** | Claude Code plugin — skills, subagents, MCP server, Revit add-in |
| **Revit support** | **2020 → latest**, and every future release |
| **Languages** | C# for everything touching Revit · Python for the brain and RAG |
| **Distribution** | Free and **open source** on public GitHub; Autodesk App Store later, also free |
| **Building on** | `AJ-AI-Brain` (brain) and `AJ-Connect` (Revit connector), upgraded to this architecture |

```text
Claude Code           host: conversation, agents, persona, orchestration
     |  MCP
Heron MCP Server      Python — brain, RAG, fragments, skills, memory
     |  IPC
Heron Revit Add-in    C# — one build per Revit version
     |  ExternalEvent
Revit
```

### Still blocking

Transport between server and add-in · the Revit threading mechanism · MCP tool granularity ·
how generated code executes. All four in [OPEN-QUESTIONS.md](docs/OPEN-QUESTIONS.md).

---

## Documentation

Full index: **[docs/README.md](docs/README.md)**

**Read first:**

- [Vision & Principles](docs/01-vision-and-principles.md) — what this is and why
- [Master Specification](docs/00-master-specification.md) — the complete original spec
- [Proposals](docs/PROPOSALS.md) — what is missing, what is risky, what to add
- [Open Questions](docs/OPEN-QUESTIONS.md) — what must be decided
- [Roadmap](docs/ROADMAP.md) — what gets built, in what order
- [Golden Rules](docs/14-golden-rules.md) — the non-negotiables

---

## The Golden Rules

1. User does BIM work. Heron AI does everything else.
2. One Agent = One Responsibility.
3. Never break a working implementation unnecessarily.
4. Reuse proven fragments before generating new code.
5. Do not mix personal, project, company and community knowledge.
6. Unproven knowledge must not automatically become production knowledge.
7. One agent creates; another agent validates.
8. Background work should remain invisible unless user attention is required.
9. High-risk actions require controlled permission.
10. Every important component must have identity, version and lifecycle.

*(Five more are [proposed](docs/14-golden-rules.md), covering undo, previews, sandboxing, publishing and permission escalation.)*

---

## First milestone

Phase 0 is one thin vertical slice through every layer:

```
"Select all ducts."  ->  intent  ->  skill  ->  MCP  ->  named pipe
  ->  add-in  ->  ExternalEvent  ->  Revit main thread
  ->  selection changes on screen  ->  audit log entry
```

No RAG, no fragments, no installer, no code generation. Just proof that the bridge works —
which settles the remaining blocking questions with facts instead of opinion.

---

## A note on this repository

It is currently **private**. It becomes public once the licence is chosen, the disclaimer and
`SECURITY.md` are written, and the public-code / private-knowledge separation is verified — because
publishing is irreversible in practice and client project data must never be able to reach it.
See [17 — Open Source & Distribution](docs/17-open-source-and-distribution.md).

---

**Owner:** Ajmal (AjmalPS) · **Domain:** BIM / Revit engineering automation
