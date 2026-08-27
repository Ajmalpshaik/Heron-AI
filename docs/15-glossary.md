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

## Status vocabularies

**Fragment lifecycle:** `DISCOVERED` → `DRAFT` → `TESTING` → `VALIDATED` → `PROVEN` → `PRODUCTION` → `DEPRECATED` → `ARCHIVED`

**Agent status:** `PROPOSED` → `DEVELOPMENT` → `TESTING` → `APPROVED` → `ACTIVE` → `DISABLED` → `DEPRECATED` → `ARCHIVED`

**Permission levels:** `READ` · `ANALYZE` · `SUGGEST` · `EXECUTE` · `MODIFY` · `PUBLISH` · `ADMIN`

**Scheduler priorities:** `P0` user task · `P1` required validation · `P2` required maintenance · `P3` knowledge improvement · `P4` optimization · `P5` cleanup
