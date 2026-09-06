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

**Phase 0 is complete — Steps 1 to 5, all proven in real Revit 2020 and 2024.**

**Phase 1 is BUILT AND UNPROVEN, and Phase 2 is built and barely proven — those are different words
on purpose.** The C# —
the write path, and the fragment bodies — compiles on all eight releases from 2020 to 2027 with zero
warnings. The Python that reasons about it has sixteen test suites, all passing. A compiler proves the
API surface agrees; a test proves the logic agrees with itself; neither says whether a duct moves 200
millimetres or 200 feet.

**Until 2026-09-06 this paragraph went on to say none of it had ever loaded into Revit and no fragment
had met a model. Both have now happened.** The add-in is deployed in Revit 2024 and D-28's executor
compiles a fragment's C# inside Revit's own process against the assemblies Revit has actually loaded.
**14 of the 348 fragments are `PROVEN`** — each on a recorded proof against a named model, with a
negative case and a staleness fingerprint ([D-30](docs/DECISIONS.md)). **The other 334 have still never
met a model**, and the write path is still unproven.
[`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) carries that debt item by item, and
`python tools/check-gaps.py` prints what is genuinely unfinished versus what is only waiting on a
machine.

| | |
|---|---|
| Specification | ✅ Complete in 4 parts — [platform](docs/00-master-specification.md) · [Agent OS](docs/00b-master-specification-agent-os.md) · [baseline](docs/00c-master-handover-baseline.md) · [additional requirements](docs/00d-additional-requirements.md) |
| Architecture review | ✅ Complete — [gaps, ideas, tensions](docs/PROPOSALS.md) across all four parts |
| Constitution | ✅ **Accepted 2026-08-28** — all [30 Articles](HERON_CONSTITUTION.md), confirmed after every one was read out rather than tapped through. Reading them aloud found three stale statements inside |
| Open questions | ✅ **42 answered · 0 open** — `Q-41` was raised *and* answered by the owner on 2026-09-06 — nothing gates any phase — [OPEN-QUESTIONS.md](docs/OPEN-QUESTIONS.md) |
| Licence & safety files | ✅ Complete — Apache 2.0, security policy, disclaimer, contribution guide |
| Roadmap | ✅ Drafted — [Phase 0 → 7](docs/ROADMAP.md) |
| Step 1 — the bridge | ✅ **Proven.** One button connects and disconnects, per-session token, newest connection takes the pipe. Two Revits at once, each with its own pipe. **Since Step 6 a lease decides who may actually send anything** ([D-22](docs/DECISIONS.md)) — taking the pipe is no longer taking the right to use it, and that half is unproven |
| Step 2 — the thread hop | ✅ **Proven.** `5,844 elements in Project1` from Revit 2024, `3,167` from 2020 — and a clean *"Revit is busy"* instead of a hang |
| Step 3 — the MCP server | ✅ **Proven.** *"is Revit working?"* answered inside Claude Code from both live Revits, no command run |
| Step 4 — the first BIM answer | ✅ **Proven.** *"select all ducts"* — 4 found and highlighted on screen in both Revits, plus the audit trail |
| Step 5 — more than one Revit | ✅ **Proven.** Refused to guess between two, took "1", and **stopped** when that Revit closed rather than using the other |
| ⛔ **Phase 0 ends here** | Everything above is **read-only**. Nothing can change a model |
| Step 6 — the first write | ⚠️ **Built and compiled. Never run.** The rails came first as the [build order](docs/27-build-order.md) requires — one `TransactionGroup`, preview, re-count, document pinning, permission gate, emergency stop, then the move. The chat half is tested, and a compiler has now read every line on **all eight releases, zero warnings** — which cost one 2020-only defect to discover. It has still **never loaded into Revit and has never moved anything.** Writing stays off until it has ([D-19](docs/DECISIONS.md), [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md)) |
| ⛔ **Phase 1 ends here, unproven** | `write.enabled` defaults to **`false`** and stays there until a real Revit has been through the register. Heron can no longer be read-only by construction, so it is read-only by default instead — a real weakening, made deliberately and written down rather than smoothed over |
| Steps 7–14 — Phase 2 | ✅ **Built in full, and almost none of it proven.** The fragment store, one knowledge store per scope, exact-word search, local offline embeddings, the two fused behind a hard Revit-version filter, the capability registry, the dependency graph, and ten skills that name capabilities rather than fragments. **All ten skills and 334 of the 348 fragments are `DRAFT`; 14 fragments are `PROVEN` as of 2026-09-07** — a fragment re-authored from an earlier library arrives here unproven whatever it was there ([D-44](docs/DECISIONS.md)), and that rule is enforced in code rather than remembered |
| The brain, reachable | ✅ Three read-only MCP tools resolve a request through a **capability**, never a fragment id. **Resolving is not running** — and nothing could run a fragment at all until D-28's executor landed on 2026-09-06. It runs one **READ-ONLY**: it opens no transaction, so Revit itself refuses any model change. Running a fragment that WRITES is a separate operation that still does not exist |

**What is proven and what is only built are different things.**
[HANDOVER.md](HANDOVER.md) §3 keeps that distinction honest, item by item.

### Decided

| | |
|---|---|
| **Host** | Claude Code plugin — skills, subagents, MCP server, Revit add-in |
| **Revit support** | **2020 → latest**, and every future release |
| **Languages** | C# for everything touching Revit · Python for the brain and RAG |
| **Transport** | Named pipes — add-in is the server, local-only by construction |
| **Revit threading** | One `ExternalEvent`, one request queue, one handler |
| **MCP tools** | Thick and specific, one per fragment, each with its own risk level |
| **Generated code** | Hybrid — scripting sandbox while testing, compiled C# for production |
| **Licence** | Apache 2.0 |
| **Distribution** | Free and **open source** on public GitHub; Autodesk App Store later, also free |
| **Building on** | the owner's earlier brain and Revit-connector work, upgraded to this architecture |

```text
Claude Code           host: conversation, agents, persona, orchestration
     |  MCP
Heron MCP Server      Python — brain, RAG, fragments, skills, memory
     |  named pipe
Heron Revit Add-in    C# — one build per Revit version
     |  ExternalEvent
Revit
```

---

## Picking this up

**Start with [HANDOVER.md](HANDOVER.md)** — what exists, what is proven versus merely built, the five
things that will bite you, and what is waiting on a decision.

## Documentation

Full index: **[docs/README.md](docs/README.md)**

**Read first:**

- [Vision & Principles](docs/01-vision-and-principles.md) — what this is and why
- [Master Specification](docs/00-master-specification.md) — the complete original spec
- [Proposals](docs/PROPOSALS.md) — what is missing, what is risky, what to add
- [Open Questions](docs/OPEN-QUESTIONS.md) — what must be decided
- [Roadmap](docs/ROADMAP.md) — what gets built, in what order
- [**Build Order**](docs/27-build-order.md) — **the six numbered steps to actually start**
- [**Agent Registry**](docs/28-agent-registry.md) — all 250 agents, what each one does
- [tools/](tools/README.md) — scripts that verify links and derive every count from source
- [Golden Rules](docs/14-golden-rules.md) — the non-negotiables

---

## The Golden Rules

1. User focuses on BIM. Heron handles technical complexity.
2. One agent should have one primary responsibility.
3. Reuse proven knowledge before creating new knowledge.
4. Never break a working Revit version unnecessarily.
5. Personal knowledge must remain separate from company knowledge.
6. Experimental knowledge must remain separate from production knowledge.
7. One agent creates; another agent validates.
8. Background work must not interfere with user work.
9. High-risk actions require controlled approval.
10. Every important object must have identity, version and lifecycle.
11. Vector DB is an index, not the canonical source of truth.
12. No automatic external publishing of private knowledge.
13. No uncontrolled self-modification of production architecture.
14. Every important autonomous operation must be auditable.
15. The platform must be modular enough that agents, skills and fragments can be replaced without redesigning the system.

*(Six more are [proposed](docs/14-golden-rules.md): undo, preview-before-modify, sandboxing generated code, permission escalation from untrusted text — plus two learned in the field, **bind the document not just the session** and **a preview expires**.)*

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

**Owner:** Ajmal PS · **Domain:** BIM / Revit engineering automation
