# 15 — Glossary

Terms used throughout the Heron AI documentation. Where a term is contested or newly defined
during review, the defining document is linked.

---

## Heron concepts

| Term | Meaning |
|---|---|
| **Agent** | A component with one responsibility, a declared contract, an identity, a version and a lifecycle. Not necessarily an LLM call — see *Tier*. |
| **Department** | A group of related agents behind a stable boundary. Callers depend on the department, not on individual agents. |
| **Skill** | A user-facing capability, named in BIM language. "Select Ducts". [09](09-skills-and-fragments.md) |
| **Fragment** | A reusable implementation unit, named in technical language. One fragment serves many skills. [09](09-skills-and-fragments.md) |
| **Tier** | Whether an agent is **T1** (deterministic service, no model call), **T2** (one scoped LLM call) or **T3** (full agentic loop). [02 §6](02-architecture-overview.md) |
| **Scope** | Which knowledge partition something belongs to: Core, Company, Project, User, Community, Temporary, Experimental. [10](10-memory-and-knowledge.md) |
| **Persona** | The user's working role — BIM Modeler or Developer — which determines how much technical detail is shown. [01 §4](01-vision-and-principles.md) |
| **Knowledge Identity** | The stable ID of a knowledge object, independent of its filename. [09 §4](09-skills-and-fragments.md) |
| **Lifecycle** | The state progression of a fragment, skill or agent, from proposal through production to archive. |
| **Golden Rule** | A permanent architectural constraint. [14](14-golden-rules.md) |
| **Product / Data / Derived** | The three classes of file in the workspace. Product is replaced on update, Data belongs to the user and must survive, Derived is a rebuildable cache. [06 §2](06-heron-platform.md) |

## Revit & BIM

| Term | Meaning |
|---|---|
| **Revit API** | Autodesk's .NET API for Revit. Callable only from the Revit main thread, inside a valid API context. [03 §4](03-heron-revit.md) |
| **API context** | A moment when Revit permits API calls — inside an `IExternalCommand`, `IExternalEventHandler`, `Idling` handler, or a document/application event. |
| **ExternalEvent** | The mechanism for asking Revit to run code on its main thread from a background thread. The backbone of the Heron bridge. |
| **Transaction** | The unit of model modification. Nothing changes a Revit model outside one. |
| **TransactionGroup** | A wrapper over several transactions that can be assimilated into a single undo entry. The basis of Golden Rule 16. |
| **ElementId** | A per-document, per-session element identifier. **Not stable** — 32-bit before Revit 2024, 64-bit from 2024. |
| **UniqueId** | A stable GUID-based element identifier. What Heron stores and transmits. |
| **Worksharing** | Revit's multi-user mode: a central model plus local copies, with element ownership and Sync With Central. |
| **Workset** | A named partition of a worksharing-enabled model, with ownership and visibility. |
| **Add-in** | A `.NET` assembly plus a `.addin` manifest, loaded by Revit at startup. Cannot be unloaded without restarting Revit. |
| **LOD** | Level of Development / Detail — how much a model element is committed to at a given stage. |
| **ISO 19650** | The international standard for information management in BIM projects. |
| **QCS** | Qatar Construction Specifications. |
| **Ashghal** | Qatar's Public Works Authority; publishes its own BIM requirements. |
| **IFC** | Industry Foundation Classes — the open, vendor-neutral BIM exchange format. |

## Platform & AI

| Term | Meaning |
|---|---|
| **MCP** | Model Context Protocol — the interface through which the AI layer calls Heron's tools. [04](04-heron-mcp.md) |
| **RAG** | Retrieval-Augmented Generation — retrieving relevant knowledge before reasoning over it. [05](05-heron-brain.md) |
| **Embedding** | A vector representation of text, used for semantic similarity search. |
| **Hybrid search** | Combining keyword (BM25/FTS) and vector search, then fusing the rankings. The recommended retrieval strategy. [05 §4](05-heron-brain.md) |
| **Golden file test** | A test that compares output against a recorded expected result. How "never break a working version" is enforced. [13 §4](13-testing-and-quality.md) |
| **Sandbox** | An isolated environment — a detached model copy — where unproven code runs before it is allowed near a live project. |
| **Prompt injection** | Instructions hidden in content the system reads (a family name, an imported document) attempting to make the model act on them. Countered by Golden Rule 19. |
| **Audit log** | The append-only structured record of every significant action. What makes invisible background work acceptable. [12 §5](12-security-and-permissions.md) |

## Kernel & platform

| Term | Meaning |
|---|---|
| **Heron Kernel** | The spine every module talks through instead of depending on each other. Holds the registries, permissions, event bus, state and logging. **Plumbing, never intelligence** — no BIM knowledge lives in it. [23 §1](23-heron-kernel.md) |
| **Capability Registry** | *"What can Heron currently do?"* The Orchestrator matches **capabilities**, never agent names — which is what makes agents replaceable. [18 §2](18-agent-operating-system.md) |
| **Workflow Engine** | Owns ordering, retries, timeouts, rollback and checkpoints. The Orchestrator decides *what*; this ensures it *happens correctly*. [23 §3](23-heron-kernel.md) |
| **Checkpoint** | Persisted stage output letting a failed multi-stage workflow resume from the failed step rather than restarting. [23 §4](23-heron-kernel.md) |
| **Evidence** | The stated reasons behind a decision — which fragment, which versions, what success history. For a `MODIFY`, **no evidence means refused, not downgraded**. [23 §6](23-heron-kernel.md) |
| **Confidence** | An agent's self-reported certainty (`HIGH`/`MEDIUM`/`LOW`/`UNKNOWN`). **Never a substitute for validation.** [23 §5](23-heron-kernel.md) |
| **Model Router** | Chooses the model class for a task. Heron declares *intent*; the host resolves it. [23 §8](23-heron-kernel.md) |
| **Prompt/Instruction Registry** | One versioned, testable home for all system and agent instructions — never scattered through code. [23 §9](23-heron-kernel.md) |
| **Dependency graph** | Skills → fragments → API → runtime → packages. Makes the blast radius of a change computable instead of requiring a full test sweep. [21 §1](21-resilience-and-operations.md) |
| **Shadow Mode** | A new agent or fragment runs on real requests with its **output discarded**, so evidence accrues without risk. [18 §4](18-agent-operating-system.md) |
| **Emergency Stop** | Global halt, in the Revit ribbon so it works when the agent side is stuck. Sticky until a person restarts. [21 §4](21-resilience-and-operations.md) |
| **Safe Mode** | Disables recently installed components and returns to last-known-good. [21](21-resilience-and-operations.md) |
| **Workflow ID** | The correlation key tying one user sentence to every agent, retrieval, model call and element touched. [21 §13](21-resilience-and-operations.md) |
| **Constitution** | The runtime-enforceable subset of the Golden Rules, written as prohibitions and injected into agents. [HERON_CONSTITUTION.md](../HERON_CONSTITUTION.md) |

## Bridge & sessions *(field-proven)*

| Term | Meaning |
|---|---|
| **Bridge** | The per-Revit connection between the MCP server and the add-in. One per Revit **process**. [25](25-multi-session-and-binding.md) |
| **Discovery file** | `%APPDATA%\Heron\bridges\<pid>.json` — an **address book**, not a status report. Static facts only; the document name is deliberately absent. [25 §2](25-multi-session-and-binding.md) |
| **Session binding** | One chat, one Revit. Asked once, sticky, and **fails closed** if that Revit closes. Never guessed. [25 §3](25-multi-session-and-binding.md) |
| **Lease** | Prevents a second chat taking over a Revit mid-job — and is what makes `(free)`/`(in use)` truthful in the picker. Scoped to the **process**, not the document. [25 §3](25-multi-session-and-binding.md) |
| **Document pinning** | Binding a write to a specific document by identity, so the user clicking to another project cannot move the target. [Golden Rule 20](14-golden-rules.md), [25 §4](25-multi-session-and-binding.md) |
| **Stale read** | Acting on a picture of the model that has since changed. *"The real danger is not the freeze, it is the stale read."* [Golden Rule 21](14-golden-rules.md), [25 §5](25-multi-session-and-binding.md) |

## Standards & references

| Term | Meaning |
|---|---|
| **Reference model** | A correctly delivered model used as a source to **infer** a standard from, rather than writing one out. [10 §5a](10-memory-and-knowledge.md) |
| **Profile** | The abstraction extracted from a reference model — conventions, frequencies, exceptions, provenance. **The model is discarded; the profile is kept.** [10 §5a](10-memory-and-knowledge.md) |
| **Checkset** | Autodesk Model Checker's XML rule format. One of Heron's export targets — readable and runnable **without Heron**. [10 §5a](10-memory-and-knowledge.md) |
| **IDS** | buildingSMART **Information Delivery Specification** — the open XML standard for machine-readable information requirements, official since June 2024. Heron's other export target. |

## Status vocabularies

> **Superseded by the [unified trust model](24-trust-model.md) ([D-14](DECISIONS.md), proposed).**
> Six competing vocabularies across the four specifications were collapsed into two orthogonal axes.

**Lifecycle** — one vocabulary for fragments, skills, capabilities **and** agents:

`DISCOVERED` → `DRAFT` → `TESTING` → `VALIDATED` → `SHADOW` → `PROVEN` → `PRODUCTION` → `DEPRECATED` → `ARCHIVED`

**Source** — fixed at creation, never advances:

`OFFICIAL` · `COMPANY` · `PROJECT` · `USER` · `COMMUNITY` · `IMPORTED` · `UNKNOWN`

**Knowledge levels** — a derived band over lifecycle, used for ranking and for gating writes:

| Level | Stages | May be used for |
|---|---|---|
| **L1 RAW** | `DISCOVERED`, `DRAFT` | inspection only |
| **L2 TESTED** | `TESTING`, `VALIDATED` | `READ` / `ANALYZE` / `SUGGEST` |
| **L3 VERIFIED** | `SHADOW`, `PROVEN` | `MODIFY` **with an accepted preview** |
| **L4 PROVEN** | `PRODUCTION` | `MODIFY` unattended |

**Permission levels:** `READ` · `ANALYZE` · `SUGGEST` · `EXECUTE` · `MODIFY` · `PUBLISH` · `ADMIN`

**Scheduler priorities:** `P0` user task · `P1` required validation · `P2` system health · `P3` knowledge maintenance · `P4` learning · `P5` optimization · `P6` cleanup
