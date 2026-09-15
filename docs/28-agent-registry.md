# 28 — The Complete Agent Registry

> **Every agent, what it does, and when it gets built.**
>
> [08 — Agent Catalogue](08-agent-catalog.md) explains the *shape* of the agent organisation — tiers,
> departments, why the count is not frightening. **This document is the flat reference list.**
>
> Compiled from all four specification parts. Where a responsibility was only a name in the source, it
> has been written out here; those entries are marked **↗** and are proposals, not quotations.

---

## How to read this

| Column | Meaning |
|---|---|
| **ID** | Stable identity, per [Part 2 §5](00b-master-specification-agent-os.md). Format `HERON-{DEPT}-{ABBR}-{NNN}` |
| **Tier** | **T1** deterministic code, no model call · **T2** one scoped LLM call · **T3** agentic loop ([02 §6](02-architecture-overview.md)) |
| **Risk** | Highest permission level it can require ([12 §1](12-security-and-permissions.md)) |
| **Step** | Build step it first appears in ([27](27-build-order.md)). `—` = not in Phase 0/1 |

**Totals: 250 agents · 167 T1 · 63 T2 · 20 T3.**
Roughly two thirds never call a model at all.

> **Correction, 2026-08-27:** an earlier version of this page stated 166. The departments actually
> summed to 196 — a straight arithmetic error on my part, caught when adding the agents below.
> The count is now computed from the rows rather than asserted.

---

## 1. Orchestration & Communication — 6

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-ORC-MAIN-001` | Orchestrator | Understands the request, selects capability and agents, builds and runs the workflow, returns the result | T2 | — | 3 |
| `HERON-ORC-INT-002` | Intent Agent | Classifies what the user is asking for — command, question, debugging, development | T2 | READ | 4 |
| `HERON-ORC-PER-003` | Communication / Persona Agent | Detects role and technical level; chooses wording. BIM language out, not API calls | T2 | READ | 4 |
| `HERON-ORC-FAIL-004` | Failure Analysis Agent | Determines *why* something failed and routes it. Never blind-retries. **T1, not T2** — Heron's failures are its own bounded set of codes, so the classification is a table, and the one answer that must never be wrong is one a model should not be asked for ([D-21](DECISIONS.md)) | T1 | READ | 6 |
| `HERON-ORC-SUM-006` | **User Result Agent** | Compresses the whole internal chain — 12 agents, 37 tool calls, 4 retrievals, 3 tests — into what the user actually needs to read. *"Done. Selected all ducts, moved them 200 mm up, verified in Revit."* This is what makes simple-outside / complex-inside real ↗ | T2 | READ | 4 |
| `HERON-ORC-FIX-005` | Fix Agent | Applies a targeted repair chosen by failure analysis | T3 | MODIFY | — |

## 2. Revit Engineering — 36

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-REVIT-CON-001` | Revit Connection Agent | Is a Revit session reachable and alive | T1 | READ | 1 |
| `HERON-REVIT-VER-002` | Revit Version Agent | Which Revit version this session is | T1 | READ | 1 |
| `HERON-REVIT-APP-003` | Revit Application Agent | Application-level API surface, open documents list | T1 | READ | 2 |
| `HERON-REVIT-DOC-004` | Revit Document Agent | Active and open documents, document state, save state | T1 | READ | 2 |
| `HERON-REVIT-TRN-005` | Revit Transaction Agent | Opens, commits and rolls back transactions. **Step 6, not 2** - it is a write mechanism, and the build order is explicit that one built before its safety rails is one that ships without them | T1 | MODIFY | 6 |
| `HERON-REVIT-TSA-006` | **Transaction Safety Agent** | One named `TransactionGroup` per operation; complete rollback on failure; document state integrity. Separate from general API logic ↗ | T1 | MODIFY | **6** |
| `HERON-REVIT-CTX-007` | **Revit Context Agent** | Gathers document, version, view, selection, links, worksets, phase, design option in one cheap pass ↗ | T1 | READ | 6 |
| `HERON-REVIT-SEL-008` | Revit Selection Agent | Reads and sets the selection set | T1 | EXECUTE | 4 |
| `HERON-REVIT-CAT-009` | Revit Category Agent | Resolves a BIM word to a category — "ducts" → `OST_DuctCurves` | T1 | READ | 4 |
| `HERON-REVIT-ELE-010` | Revit Element Agent | Element lookup, creation, deletion, geometry access | T1 | MODIFY | 4 |
| `HERON-REVIT-PAR-011` | Revit Parameter Agent | Reads and writes instance, type and shared parameters | T1 | MODIFY | — |
| `HERON-REVIT-FAM-012` | Revit Family Agent | Families, types, loading, placement | T1 | MODIFY | — |
| `HERON-REVIT-VIE-013` | Revit View Agent | Views, sheets, view templates, visibility | T1 | MODIFY | — |
| `HERON-REVIT-WRK-014` | Revit Workset Agent | Worksets, element ownership, checkout state. Ownership failure is a normal outcome | T1 | MODIFY | — |
| `HERON-REVIT-LNK-015` | Revit Link Agent | Linked models — read only; never modifies a link's source | T1 | READ | — |
| `HERON-REVIT-WRN-016` | Revit Warning Agent | Revit warnings and failure preprocessing; never silently swallows | T1 | READ | 6 |
| `HERON-REVIT-PRF-017` | Revit Performance Agent | Timing, element counts, operation cost limits | T1 | READ | — |
| `HERON-REVIT-EXP-018` | Revit Export Agent | Exports — IFC, DWG, PDF, images, schedules | T1 | PUBLISH | — |
| `HERON-REVIT-IMP-019` | Revit Import Agent | Imports and links external files in | T1 | MODIFY | — |
| `HERON-REVIT-API-020` | Revit API Agent | Correct API, namespace, method, deprecations, transaction requirements for a novel operation | T2 | READ | — |
| `HERON-REVIT-ACI-034` | **API Change Intelligence Agent** | Continuously tracks what changed between Revit versions — deprecated and renamed APIs, changed methods, parameters, units, namespaces, and silent behavioural changes. Feeds Fragment Evolution before a version breaks something ↗ | T2 | READ | — |
| `HERON-REVIT-CMP-021` | Revit Compatibility Agent | Will this fragment run on this Revit version | T1 | READ | — |
| `HERON-REVIT-UI-022` | Revit UI Agent | Task dialogs, progress banner, the picker. **The picker lives in the chat, not in Revit** - that is where the question is being asked, and a second one inside Revit would be a copy nobody uses. **The progress banner is built** ([D-50](DECISIONS.md), 2026-09-06) - it waited for Step 6 as this row said it would, and Step 6 made jobs slow enough. It says whether Heron is READING or CHANGING, which nothing in Revit said before. **Compiles on all eight releases, 0 warnings** (`B5`, 2026-09-07); **never yet seen on a screen** - `B6`-`B13` in [NEEDS-CHECKING](NEEDS-CHECKING.md) need Revit | T1 | READ | 5 |
| `HERON-REVIT-RIB-023` | Revit Ribbon Agent | Ribbon tab, panels, buttons. **No Emergency Stop button since 2026-09-06** ([D-46](DECISIONS.md)) | T1 | READ | 1 |
| `HERON-REVIT-DEP-024` | Revit Deployment Agent | Builds and deploys the add-in per version | T1 | ADMIN | 1 |
| `HERON-REVIT-UNI-035` | **Unit Conversion Agent** | Owns the boundary between Revit's internal **decimal feet** and everything a user says. Lengths, angles, areas, volumes. Converts once, at one place, and **states the unit in every result** ↗ | T1 | READ | 4 |
| `HERON-REVIT-RBK-036` | **Session Rollback Agent** | The panic button. Reverses **everything Heron did this session**, newest first, using the audit log's transaction groups. Distinct from Transaction Safety, which owns one operation ↗ | T1 | MODIFY | — |
| `HERON-REVIT-HLT-025` | Revit Plugin Health Agent | Is Revit installed, add-in loaded, ribbon present, MCP connected, document open, tools registered | T1 | READ | 3 |
| `HERON-REVIT-SCH-026` | **Revit Schedule Agent** | Schedules — the way BIM people actually extract data. Read, create, modify, export ↗ | T1 | MODIFY | — |
| `HERON-REVIT-LVL-027` | **Revit Level & Grid Agent** | Levels and grids — the hosts almost everything depends on. Naming, elevation, extents ↗ | T1 | MODIFY | — |
| `HERON-REVIT-RM-028` | **Revit Room & Space Agent** | Rooms, spaces, zones, areas. MEP loads and schedules depend on spaces existing and being bounded ↗ | T1 | MODIFY | — |
| `HERON-REVIT-SHT-029` | **Revit Sheet Agent** | Sheets, numbering, titleblocks, revisions. Distinct from views ↗ | T1 | MODIFY | — |
| `HERON-REVIT-SYS-030` | **Revit MEP System Agent** | Duct and pipe systems, connectors, system naming, connectivity — including unconnected elements ↗ | T1 | MODIFY | — |
| `HERON-REVIT-DIM-031` | **Revit Dimension & Annotation Agent** | Dimensions, tags, text, keynotes. *"Create dimensions"* is one of the founding examples ↗ | T1 | MODIFY | — |
| `HERON-REVIT-PHS-032` | **Revit Phase & Design Option Agent** | Phases and design options — both change what *"all ducts"* even means ↗ | T1 | MODIFY | — |
| `HERON-REVIT-GRP-033` | **Revit Group & Assembly Agent** | Groups and assemblies. They behave unusually and break naive element edits ↗ | T1 | MODIFY | — |

## 3. MCP / Bridge — 12

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-MCP-SRV-001` | MCP Server Agent | Server lifecycle, stdio/SSE transport to the host | T1 | — | 1 |
| `HERON-MCP-CON-002` | MCP Connection Agent | Opens and holds the pipe to a Revit bridge | T1 | — | 1 |
| `HERON-MCP-REG-003` | MCP Tool Registry Agent | Which tools exist, their schemas, their declared risk level | T1 | — | 3 |
| `HERON-MCP-CFG-004` | MCP Configuration Agent | Server configuration and host registration | T1 | ADMIN | 3 |
| `HERON-MCP-HLT-005` | MCP Health Agent | Liveness and readiness of the bridge | T1 | READ | 3 |
| `HERON-MCP-AUT-006` | MCP Authentication Agent | Who may call — pipe ACLs, local-only enforcement | T1 | ADMIN | — |
| `HERON-MCP-VER-007` | MCP Version Agent | Protocol and Heron version reporting | T1 | — | 5 |
| `HERON-MCP-CMP-008` | MCP Compatibility Agent | Server / add-in / Revit version triangle; refuses cleanly on mismatch | T1 | — | 5 |
| `HERON-MCP-REC-009` | MCP Recovery Agent | Reconnect with backoff. Distinguishes transport faults from real failures | T1 | — | 5 |
| `HERON-MCP-LOG-010` | MCP Logging Agent | Structured request/response logging into the audit log | T1 | — | 4 |
| `HERON-MCP-DIS-012` | **MCP Discovery Agent** | Finds **other** MCP servers installed on the machine, reads their tools, versions and capabilities, and registers them with the Tool Registry. Distinct from Bridge Discovery, which finds Revit sessions ↗ | T1 | READ | — |
| `HERON-MCP-SEC-011` | MCP Security Agent | **Enforces the permission gate in the add-in.** Returns `REQUIRES_CONFIRMATION` rather than executing | T1 | ADMIN | **6** |

## 4. Session & Bridge Management — 5 *(field-derived)*

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-SES-DIS-001` | Bridge Discovery Agent | Reads `%LOCALAPPDATA%\Heron\bridges\*.json`, verifies each is alive, removes stale files ↗ | T1 | READ | 1 |
| `HERON-SES-LST-002` | Session List Agent | Builds the picker **live** — version, project, availability. Never a cached snapshot, never a PID shown ↗ | T1 | READ | 5 |
| `HERON-SES-BND-003` | Session Binding Agent | One chat, one Revit. Asks once, stays bound, **fails closed** when that session closes ↗ | T1 | READ | 5 |
| `HERON-SES-LEA-004` | Session Lease Agent | Prevents a second chat taking over mid-job. Scoped to the process. Never blocks a rollback ↗ | T1 | READ | **6** |
| `HERON-SES-PIN-005` | Document Pinning Agent | Pins the target document by identity for any write; verifies at every step ↗ | T1 | MODIFY | **6** |

## 5. Knowledge & RAG — 17

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-RAG-LIB-001` | RAG Librarian Agent | Decides *which scopes* to search before anything is searched | T1 | READ | — |
| `HERON-RAG-DIS-002` | Knowledge Discovery Agent | Finds potentially relevant knowledge | T1 | READ | — |
| `HERON-RAG-RET-003` | Retriever Agent | Retrieves candidates from the chosen scopes | T1 | READ | — |
| `HERON-RAG-FMT-004` | Fragment Matcher Agent | Matches the request against existing fragments | T2 | READ | — |
| `HERON-RAG-SMT-005` | Skill Matcher Agent | Finds applicable skills | T2 | READ | — |
| `HERON-RAG-RNK-006` | Ranking Agent | Fuses keyword and vector results; ranks by trust, version match and success rate | T1 | READ | — |
| `HERON-RAG-CTX-007` | Context Builder Agent | Assembles the minimal context an agent actually needs | T1 | READ | — |
| `HERON-RAG-EMB-008` | Embedding Agent | Creates embeddings — local model by default | T1 | READ | — |
| `HERON-RAG-VEC-009` | Vector Search Agent | Searches the vector index | T1 | READ | — |
| `HERON-RAG-IDX-010` | Index Manager Agent | Maintains indexes and their metadata | T1 | MODIFY | — |
| `HERON-RAG-RIX-011` | Re-index Agent | Re-indexes on change, by content hash not mtime | T1 | MODIFY | — |
| `HERON-RAG-DUP-012` | Duplicate Detection Agent | Finds duplicate knowledge at write time, not only at read time | T1 | READ | — |
| `HERON-RAG-VAL-013` | Knowledge Validation Agent | Checks retrieved knowledge before it is used | T2 | READ | — |
| `HERON-RAG-CIT-014` | Citation / Source Agent | Tracks provenance. **No source, no claim** | T1 | READ | — |
| `HERON-RAG-CNF-015` | **Knowledge Conflict Agent** | Two sources disagree — compares version, source, trust, project context, test history. Asks the user when unsure ↗ | T2 | SUGGEST | — |
| `HERON-RAG-EVO-016` | Knowledge Evolution Agent | Restructures knowledge organisation when it stops fitting | T3 | MODIFY | — |
| `HERON-RAG-RSH-017` | **Research Agent** | Investigates a question Heron's own knowledge cannot answer — external docs, API references, standards, prior art. **Everything it returns carries a citation** ↗ | T3 | READ | — |

## 6. Fragment Lifecycle — 9

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-FRG-VAL-001` | Fragment Validation Agent | Logic, API, versions, dependencies, metadata, duplication, reusability | T2 | READ | — |
| `HERON-FRG-PRF-002` | Fragment Performance Agent | Success, failure, timing, user corrections, error frequency — **per Revit version** | T1 | READ | — |
| `HERON-FRG-SPL-003` | Fragment Split Agent | Decomposes a compound fragment. Only when ≥2 real consumers exist | T3 | SUGGEST | — |
| `HERON-FRG-MRG-004` | Fragment Merge Agent | Detects near-identical fragments; proposes merge, replace, keep separate or deprecate | T2 | SUGGEST | — |
| `HERON-FRG-EVO-005` | Fragment Evolution Agent | Decides KEEP / UPDATE / EXTEND / SPLIT / MERGE / BRANCH / DEPRECATE / ARCHIVE. Always proposes | T3 | SUGGEST | — |
| `HERON-FRG-REG-006` | Regression Testing Agent | Builds and tests every supported version; **rejects unsafe changes**; preserves the previous implementation | T1 | READ | — |
| `HERON-FRG-MTX-009` | **Compatibility Matrix Agent** | Owns the matrix itself — every fragment against every Revit version **and** its .NET runtime, with the status coming from **tests, never assumption**. The backbone of the 2020-to-latest commitment ↗ **Built 2026-09-07** — `brain/heron_matrix.py`, served as `heron_compatibility`. Four states, never merged: CLAIMED, COMPILES, PROVEN, unknown. The runtime per release is parsed out of `Directory.Build.props` rather than copied. Its first run: everything compiles on all eight releases, and every proof Heron holds stands on Revit 2024 alone | T1 | READ | — |
| `HERON-FRG-CRE-007` | **Fragment Creation Agent** | Authors a new fragment — identity, metadata, implementation, tests. Only after Fragment Matcher reports nothing reusable ↗ | T3 | MODIFY | — |
| `HERON-FRG-UPD-008` | **Fragment Update Agent** | **Applies** what Fragment Evolution decided. Evolution decides; this one does it, and never to `PRODUCTION` without approval ↗ | T3 | MODIFY | — |

## 5a. User & Personalization — 3 *(new department)*

**[NOTE]** Added 2026-08-27. A genuine hole: the memory **scopes** were designed
([10](10-memory-and-knowledge.md)) but no agent owned the user's own. Persona chose *wording*; nothing
held the durable facts it was choosing from.

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-USR-PRO-001` | **User Profile Agent** | Holds the durable facts: role, discipline, experience level, language, working style, technical depth. **State, not behaviour** — Persona reads this to choose how to speak ↗ | T1 | READ | — |
| `HERON-USR-MEM-002` | **User Memory Agent** | Decides what is worth remembering, what expires, what must **never** be stored, and when an old preference is superseded. The judgement is the job ↗ | T2 | MODIFY | — |
| `HERON-USR-SKL-003` | **User Skill Manager** | Keeps this user's own learned skills separate from shared ones, and manages promotion upward. Personal stays personal until explicitly raised ↗ | T1 | MODIFY | — |

---

## 5b. Learning & Self-Growth — 4 *(new department)*

**[NOTE]** Continuous learning ran through all four specification parts and **no agent owned any of it.**
Fragment Performance measured, Knowledge Evolution restructured — but nothing watched real work and
turned it into knowledge. This is the loop that makes Heron self-growing rather than merely large.

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-LRN-OBS-001` | **Learning Observation Agent** | Watches completed workflows, successes and failures alike. **Failure is the higher-value signal** ↗ | T1 | READ | — |
| `HERON-LRN-ANA-002` | **Learning Analysis Agent** | Is this genuinely new and reusable, or a one-off? Frequency and corroboration, not novelty ↗ | T2 | READ | — |
| `HERON-LRN-EXT-003` | **Knowledge Extraction Agent** | Turns an observed pattern into a candidate fragment, skill or rule — parameterised, not hard-coded to the numbers it happened to see ↗ | T3 | SUGGEST | — |
| `HERON-LRN-PRO-004` | **Learning Promotion Agent** | Walks new knowledge through the lifecycle gates. **One success is not proof**, and `PRODUCTION` still needs a human ↗ | T1 | SUGGEST | — |

---

## 6a. Skill Lifecycle — 6 *(new department)*

**[NOTE]** Fragments had a lifecycle department; skills did not. Skill work was scattered across three
others — Skill Matcher in RAG, Skill Extraction in Import, Skill Documentation in Documentation — and
nothing owned creating, updating, validating or scoring a skill. [Part 2 §29](00b-master-specification-agent-os.md)
(skill composition) and [§63](00b-master-specification-agent-os.md) (skill quality score) had no agent
at all. This department closes that.

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-SKL-RES-001` | **Skill Research Agent** | Before building: does this skill already exist, what fragments would it need, what does the standard say ↗ | T2 | READ | — |
| `HERON-SKL-CRE-002` | **Skill Creation Agent** | Authors a new skill — name, description, example utterances, required fragments, preconditions, risk level ↗ | T3 | MODIFY | — |
| `HERON-SKL-UPD-003` | **Skill Update Agent** | Revises an existing skill without breaking callers. Enters the lifecycle at `DRAFT`, never straight to production ↗ | T3 | MODIFY | — |
| `HERON-SKL-VAL-004` | **Skill Validation Agent** | Metadata complete, fragments exist and are compatible, risk level correct, no duplicate skill ↗ | T2 | READ | — |
| `HERON-SKL-CMP-005` | **Skill Composition Agent** | Builds skills from skills. Validates the graph is **acyclic**, and propagates the highest risk level upward ↗ | T2 | READ | — |
| `HERON-SKL-PRF-006` | **Skill Performance Agent** | Success, user corrections, execution time, failure rate — **per Revit version**. Poor performers enter review ↗ | T1 | READ | — |

---

## 7. Import & Migration — 14

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-IMP-MAIN-001` | Import Agent | Owns the import pipeline end to end. **Never modifies the source folder** | T2 | MODIFY | — |
| `HERON-IMP-FIL-002` | File Discovery Agent | Walks the folder, identifies file types | T1 | READ | — |
| `HERON-IMP-CLS-003` | Content Classification Agent | Code, documentation, config, metadata, asset | T2 | READ | — |
| `HERON-IMP-FEX-004` | Fragment Extraction Agent | Pulls reusable implementation units out of existing code | T3 | READ | — |
| `HERON-IMP-SEX-005` | Skill Extraction Agent | Identifies user-facing capabilities in existing tooling | T3 | READ | — |
| `HERON-IMP-MEX-006` | Metadata Extraction Agent | Author, version, dependencies, target Revit versions | T2 | READ | — |
| `HERON-IMP-DUP-007` | Duplicate Detection Agent | Compares against existing knowledge before anything is created | T1 | READ | — |
| `HERON-IMP-CMP-008` | Compatibility Agent | Determines which Revit and .NET versions the import supports | T1 | READ | — |
| `HERON-IMP-MIG-009` | Migration Agent | Transforms imported content into Heron's architecture | T3 | MODIFY | — |
| `HERON-IMP-REN-010` | Rename Agent | Applies naming conventions to imported artefacts | T1 | MODIFY | — |
| `HERON-IMP-ARC-011` | Architecture Matching Agent | Places content where the architecture says it belongs | T2 | MODIFY | — |
| `HERON-IMP-VAL-012` | Validation Agent | Verifies the migrated result before it is saved | T1 | READ | — |
| `HERON-IMP-IDX-013` | Indexing Agent | Indexes the accepted result | T1 | MODIFY | — |
| `HERON-IMP-APR-014` | Approval Agent | Presents the manifest for human review. **Everything enters at `DISCOVERED`** | T1 | SUGGEST | — |

## 8. Standards & BIM QA — 14

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-STD-BIM-001` | BIM Standard Agent | Applies a stated BIM standard to a model | T2 | ANALYZE | — |
| `HERON-STD-CMP-002` | Company Standard Agent | The organisation's own approved standard | T2 | ANALYZE | — |
| `HERON-STD-ISO-003` | ISO Standards Agent | ISO 19650 and related. **Cites, never invents** | T2 | ANALYZE | — |
| `HERON-STD-NAM-004` | Naming Standard Agent | Naming rules for elements, views, sheets, files | T2 | ANALYZE | — |
| `HERON-STD-MOD-005` | Modeling Standard Agent | How things should be modelled — connections, elevations, practice | T2 | ANALYZE | — |
| `HERON-STD-QAQ-006` | QA/QC Standard Agent | The organisation's QA process requirements | T2 | ANALYZE | — |
| `HERON-STD-LOD-007` | LOD Agent | Level of development / detail expected at this stage | T2 | ANALYZE | — |
| `HERON-STD-DOC-008` | Documentation Standard Agent | Sheet, titleblock and annotation requirements | T2 | ANALYZE | — |
| `HERON-STD-PRJ-009` | Project Standard Agent | This project's own rules — **outranks the company default**, and says so | T2 | ANALYZE | — |
| `HERON-STD-REF-010` | **Reference Model Profiler** | Infers a standard from a correctly delivered model. Extracts the profile, **discards the model** ↗ | T3 | READ | — |
| `HERON-STD-MET-014` | **Metadata & Policy Checker Agent** | Enforces the Heron metadata standard ([29](29-metadata-standard.md)) on everything Heron creates — every artefact declares its agent, step, status, version and layer. Also audits **the registry against the code**: an agent claimed by no file, or a file claiming no agent ↗ | T1 | READ | 1 |
| `HERON-STD-PVL-013` | **Profile Validation Agent** | Checks a standard **inferred** from a reference model before it can be trusted. Tests it against a second delivered model, separates convention from coincidence, and surfaces the exceptions rather than flagging correct work as wrong ↗ | T2 | READ | — |
| `HERON-QA-BIM-011` | **BIM QA Agent** | Checks the *model*: naming, parameters, categories, families, levels, worksets, views, MEP connectivity ↗ | T2 | ANALYZE | — |
| `HERON-QA-CLS-012` | **Clash / Coordination Agent** | Clash analysis, clearance, system coordination, linked-model coordination reports ↗ | T2 | ANALYZE | — |

## 9. Development — 21

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-DEV-REQ-001` | Requirement Agent | Turns a request into a buildable specification | T2 | — | — |
| `HERON-DEV-PLN-002` | Planning Agent | Sequences the work | T2 | — | — |
| `HERON-DEV-ARC-003` | Architecture Agent | Decides structure and placement within Heron's architecture | T3 | — | — |
| `HERON-DEV-GEN-004` | Code Generation Agent | Writes code — **only after Fragment Matcher has reported** | T3 | — | — |
| `HERON-DEV-CSH-005` | C# Agent | C# language and idiom | T2 | — | — |
| `HERON-DEV-NET-006` | **.NET Compatibility Check Agent** | *Read-only.* Which target framework does this need, is it available, is the package set compatible, will it build on all supported versions ↗ | T1 | READ | — |
| `HERON-DEV-NUP-019` | **.NET Update Agent** | *Changes projects.* Retargets a framework, bumps packages, migrates project files. Regression matrix must pass before it is accepted ↗ | T1 | MODIFY | — |
| `HERON-DEV-NCR-020` | **.NET Project Creation Agent** | *Creates new.* Authors project files, target frameworks, references, build configuration for a new component ↗ | T1 | MODIFY | — |
| `HERON-DEV-RAP-007` | Revit API Domain Agent | Revit API knowledge for generation *(merge candidate with 005/006)* | T2 | — | — |
| `HERON-DEV-REV-008` | Code Review Agent | Architecture, API usage, error handling, transaction safety, duplication | T2 | — | — |
| `HERON-DEV-SEC-009` | Security Review Agent | Required for anything at MODIFY or above | T2 | — | — |
| `HERON-DEV-BLD-010` | Build Agent | Compiles across all target frameworks | T1 | — | — |
| `HERON-DEV-UNT-011` | Unit Test Agent | Runs unit tests | T1 | — | — |
| `HERON-DEV-INT-012` | Integration Test Agent | Runs integration tests against a mocked Revit boundary | T1 | — | — |
| `HERON-DEV-RVT-013` | Revit Test Agent | Runs tests **inside real Revit**. Code QA ≠ Revit QA. **Running half built 2026-09-09** - `tools/batch-prove.py`, which proves many fragments in one pass and judges BOTH halves of each: the negative came back empty *and* the positive moved a declared result off zero. It refuses a fragment already at PROVEN, because one batch spent a whole pass re-proving fifteen of them. The arranging is the hard half and stays a brief rather than code - `.claude/skills/fragment-proving/SKILL.md` - but its DERIVABLE part is `tools/generate-jobs.py`, which writes the job file's fragment list, write path, setup chain and exact input names out of the library, and leaves the category and the view blank because guessing those produced eleven confident meaningless results in one batch. Judging is `HERON-FRG-VAL-001`'s, imported rather than copied | T1 | READ | — |
| `HERON-DEV-RGR-014` | Regression Test Agent | Golden-file comparison across supported versions | T1 | — | — |
| `HERON-DEV-PRF-015` | Performance Agent | Execution time and resource cost | T1 | — | — |
| `HERON-DEV-EXP-021` | **Experiment Agent** | The scientist. Designs and runs a **comparison** where the right answer is not known yet — is v2 actually better than v1, does this approach beat that one, is the change worth keeping. Reports a verdict with evidence, not an opinion ↗ | T3 | READ | — |
| `HERON-DEV-QA-016` | QA Agent | Final gate before approval. Never the implementer | T2 | — | — |
| `HERON-DEV-DOC-017` | Documentation Agent | Generates docs from registries and metadata | T1 | — | — |
| `HERON-DEV-REL-018` | Release Agent | Packages and releases | T1 | PUBLISH | — |

## 10. Agent Lifecycle & HR — 17

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-AHR-GAP-001` | **Capability Gap Agent** | *"X is needed repeatedly and no capability covers it."* A read-only report over the audit log. **Built 2026-09-07** - `brain/heron_gaps.py`, served as the `heron_gaps` MCP tool. It splits failures into DEFECTS and CORRECT REFUSALS, because the loudest error in the trail (`needs_unbound`, 38 of 176) is the executor behaving correctly, and counting it as a gap would commission a fragment that already exists. Its first run found 131 `compile_failed` on 2026-09-06 and none on 2026-09-07 | T1 | READ | — |
| `HERON-AHR-WFP-015` | **Workforce Planning Agent** | The agent that says **no**. Before anything is hired: does a capability already cover this, can an existing agent be extended, is this a fragment rather than an agent? **This is the guard against agent explosion** ↗ | T2 | SUGGEST | — |
| `HERON-AHR-SBX-016` | **Agent Sandbox Agent** | Runs a newly built agent in isolation — never against a live model, never able to write production knowledge — before it is allowed anywhere near real work ↗ | T1 | READ | — |
| `HERON-AHR-CON-017` | **Agent Contract Agent** | Owns the interface between agents: input and output schema, permissions, allowed tools, timeout, failure states, retry rules, version. Detects breaking contract changes across 249 agents ↗ | T1 | READ | — |
| `HERON-AHR-HR-002` | Agent HR Agent | Writes the job description — responsibility, capabilities, dependencies, tools | T2 | — | — |
| `HERON-AHR-ARC-003` | Agent Architect | Designs the agent and its contract | T3 | — | — |
| `HERON-AHR-BLD-004` | Agent Builder | Implements it | T3 | — | — |
| `HERON-AHR-TRN-005` | Agent Trainer | Supplies architecture, standards, security rules, approved examples | T2 | — | — |
| `HERON-AHR-CRT-006` | Agent Creator | Owns the pipeline. **May only ever assign `PROPOSED`** | T3 | ADMIN | — |
| `HERON-AHR-MEN-014` | **Agent Mentor Agent** | The senior on the team. Pairs a new agent with the **proven agent that currently owns that capability**, compares their output through Shadow Mode, and explains *why* they diverged. Active only while the new agent is in `SHADOW` ↗ | T2 | READ | — |
| `HERON-AHR-VAL-013` | **Agent Validation Agent** | *Before activation:* is this agent correct, does it meet its contract, does it follow the architecture, does it duplicate an existing agent, is its risk level right. **Never the agent that built it** ↗ | T2 | ADMIN | — |
| `HERON-AHR-EVL-007` | Agent Evaluator | *After activation:* scores real performance against expectations | T2 | READ | — |
| `HERON-AHR-REG-008` | Agent Registry Agent | System of record for every agent | T1 | ADMIN | — |
| `HERON-AHR-OPT-009` | Agent Optimizer | Improves an existing agent | T2 | SUGGEST | — |
| `HERON-AHR-RET-010` | Agent Retirement Agent | Retires with history preserved and rollback possible. **Archive, never delete** | T1 | ADMIN | — |
| `HERON-AHR-MON-011` | Architecture Monitor | Detects drift from the architecture | T2 | READ | — |
| `HERON-AHR-DEP-012` | Agent Deployment Agent | Activates an approved agent | T1 | ADMIN | — |

### The full hiring lifecycle, in company terms

**[NOTE]** Added 2026-08-27. The owner described the intended model as an engineering company: *the CEO
needs a role filled, HR hires, the new person does not know the company, so a senior teaches them.*
That is exactly what this department is for — set out here end to end so the mapping is explicit.

| Company | Heron | Agent |
|---|---|---|
| A role keeps going unfilled | A capability is needed repeatedly and nothing provides it | `Capability Gap` |
| The CEO says *"we need someone for this"* | The Orchestrator or the user asks directly | `Agent HR` |
| HR writes the job description | Responsibility, capabilities, dependencies, tools | `Agent HR` |
| The role is designed | Contract, inputs, outputs, risk level, tier | `Agent Architect` |
| The person is hired | The agent is implemented | `Agent Builder` |
| Induction — handbook, standards, security briefing | Architecture rules, coding and naming standards, approved fragments and skills | `Agent Trainer` |
| **A senior sits with them on the job** | **Paired with the proven agent, output compared, divergences explained** | **`Agent Mentor`** |
| Probation — real work, supervised, nothing shipped | `SHADOW` — runs on real requests, **output discarded** | `Agent Mentor` + `Agent Validation` |
| Sign-off by someone who did not hire them | Validation before activation, never by the builder | `Agent Validation` |
| Starts properly | `PRODUCTION` — **a human approves this transition** | `Agent Deployment` |
| Performance review | Success, corrections, rollbacks, per Revit version | `Agent Evaluator` |
| Coaching and development | Improving an existing agent | `Agent Optimizer` |
| Changing how the team works | Improving the workflow itself, not the people in it | `Workflow Optimizer` |
| Leaving — handover, records kept | Retired, replaced, archived. **Never deleted** | `Agent Retirement` |

**Two triggers into this pipeline, not one.** `Agent HR` accepts both:

- **Bottom-up** — `Capability Gap` sees the same unmet need recurring in the audit log. This is the
  honest one, because it comes from real usage rather than imagination.
- **Top-down** — the Orchestrator or the user says *"we need an agent for this."*

**[NOTE — where the company analogy stops]** A company can hire someone and let them start on Monday.
Heron cannot. `Agent Creator` **may only ever assign `PROPOSED`** ([Golden Rule 13](14-golden-rules.md)),
and a human approves activation. The mentor and the shadow period produce the *evidence*; a person still
signs. That is the one place where *"the user does BIM work, Heron does everything else"* is deliberately
broken, and it should stay broken.

### Heron is its own first customer

**[NOTE]** The owner's other point — *"these agents do our side work also, internally it will update and
upgrade, and their working ways also."* That is already the design, and it is worth naming:

| Serving the **BIM user** | Serving **Heron itself** |
|---|---|
| Revit Engineering · MCP / Bridge · Session · Knowledge & RAG · Skills · Fragments · Standards & BIM QA · Reporting | Development · Agent Lifecycle & HR · Documentation · GitHub · Workspace · Naming · Installation & Update · Operations |

Roughly half the organisation never touches a Revit model. It builds, tests, documents, deploys,
monitors and improves **Heron** — which is exactly how a real engineering company is staffed: people who
serve clients, and people who keep the firm running.

The self-improvement loop that connects them:

```text
User work -> audit log -> Capability Gap -> Agent HR -> new capability
     ^                                                        |
     +--------------- Evaluator / Optimizer <-----------------+
```

**Controlled, not autonomous.** Every arrow that changes production knowledge or activates an agent
passes a human gate. Heron proposes continuously; it promotes only with approval
([Golden Rule 6](14-golden-rules.md), [13](14-golden-rules.md)).

---

## 11. Kernel & Platform — 19

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-KRN-CFG-001` | Configuration Manager | Versioned, auditable configuration. **A security boundary** | T1 | ADMIN | 1 |
| `HERON-KRN-IDN-002` | Identity Manager | Identity for every registered object | T1 | — | 1 |
| `HERON-KRN-PRM-003` | Permission Manager | Resolves permission level for an operation | T1 | ADMIN | 6 |
| `HERON-KRN-EVT-004` | Event Bus | Internal events. Handlers notify, never perform MODIFY directly | T1 | — | — |
| `HERON-KRN-STA-005` | State Manager | Task state and checkpoints | T1 | — | **6** |
| `HERON-KRN-LOG-006` | Logging / Audit Agent | Append-only structured audit, keyed by Workflow ID | T1 | — | 4 |
| `HERON-KRN-WFL-007` | Workflow Engine | Ordering, retries, timeouts, rollback, checkpoints, resume. Never decides a retry itself — it asks `HERON-ORC-FAIL-004` | T1 | — | **6** |
| `HERON-KRN-CAP-008` | **Capability Registry Agent** | *"What can Heron currently do?"* Matched by capability, never agent name | T1 | — | — |
| `HERON-KRN-TOL-009` | Tool Registry Agent | Tools, schemas, declared risk levels | T1 | — | 3 |
| `HERON-KRN-MDL-010` | Model Router | Declares reasoning intent; resolution is pluggable | T1 | — | — |
| `HERON-KRN-PRO-011` | Prompt / Instruction Registry | One versioned, testable home for all instructions | T1 | ADMIN | — |
| `HERON-KRN-TRU-019` | **Content Trust Agent** | Enforces [Golden Rule 19](14-golden-rules.md). Scans everything Heron **reads but does not control** — family names, parameter descriptions, model text, imported folders, community packages — for text addressed to the agent. Surfaces it to the user; never acts on it ↗ | T2 | READ | — |
| `HERON-KRN-SEC-012` | Secret Manager | Credentials outside the workspace. Redaction on the way out | T1 | ADMIN | — |
| `HERON-KRN-DEP-013` | Dependency Graph Agent | Skills → fragments → API → runtime → packages. Makes blast radius computable | T1 | READ | — |
| `HERON-KRN-EVD-014` | Evidence Agent | Records *why* a decision was made. **No evidence, no MODIFY** | T1 | — | 6 |
| `HERON-KRN-WOP-016` | **Workflow Optimizer Agent** | Improves *the way work is done*, not the agents doing it. Spots that a workflow always fails at the same stage, that two stages could merge, or that a step never changes the outcome ↗ | T2 | SUGGEST | — |
| `HERON-KRN-TOK-015` | **Token & Cost Budget Agent** | Counts tokens **before** sending, enforces per-request, per-session and background budgets, and can force a cheaper model when a budget is tight. Feeds the visible cost meter ↗ | T1 | — | — |
| `HERON-KRN-MAV-017` | **Model Availability & Fallback Agent** | Is the provider actually reachable — auth, latency, context size, local or cloud. On failure, routes to the fallback **and marks the result as degraded**, so it never counts as evidence toward promotion ↗ | T1 | — | — |
| `HERON-KRN-HUM-018` | **Human Approval Agent** | Owns the one boundary Heron cannot cross alone. Asks the person, records who approved what and when, scopes consent to that single action, and **never remembers it** ↗ | T1 | ADMIN | 6 |

## 12. Workspace & Folder Architecture — 12

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-WSP-ARC-001` | Workspace Architect Agent | Owns the physical structure | T2 | ADMIN | — |
| `HERON-WSP-CRE-002` | Folder Creation Agent | Creates the structure | T1 | MODIFY | — |
| `HERON-WSP-VAL-003` | Folder Validation Agent | Detects misplaced files | T1 | READ | — |
| `HERON-WSP-REP-004` | Folder Repair Agent | Corrects placement. Dry-run by default | T1 | MODIFY | — |
| `HERON-WSP-PLC-005` | File Placement Agent | Decides where a new artefact belongs | T2 | MODIFY | — |
| `HERON-WSP-TPL-006` | Template Agent | Workspace and project templates | T1 | MODIFY | — |
| `HERON-WSP-PTH-007` | Path Manager Agent | Product / data / derived separation enforced in code | T1 | — | 1 |
| `HERON-WSP-MIG-008` | Workspace Migration Agent | Schema and layout migrations. Idempotent, versioned, backed up | T1 | ADMIN | — |
| `HERON-WSP-CLN-009` | Cleanup Agent | Identifies unused artefacts. **Archives, never deletes** | T1 | MODIFY | — |
| `HERON-WSP-BAK-010` | Backup Agent | Backs up the data class. Not the derived index. **Built 2026-09-08** — `tools/heron-backup.py`. Written after measuring that nothing in the repository backed up anything: the audit trail is Heron's only record of what it has done and is not in git. Excludes rather than includes, so a file nobody anticipated is kept by default | T1 | — | — |
| `HERON-WSP-RST-011` | Restore Agent | Restore and rebuild. Must be tested, not merely implemented. **Built 2026-09-08**, in the same commit as the Backup Agent because docs/21 s8 calls an untested restore path a belief. `drill` does the round trip into scratch and compares content hashes both ways. Refuses without `--confirm`, refuses a backup that fails verification, and takes a safety copy of whatever it overwrites | T1 | ADMIN | — |
| `HERON-WSP-REG-012` | File Registry Agent | What exists, where, and under which identity | T1 | — | — |

## 13. Naming & Taxonomy — 7

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-NAM-GEN-001` | Naming Agent | Generates a predictable name from domain, capability, purpose, platform, version | T2 | — | — |
| `HERON-NAM-VAL-002` | Naming Validation Agent | Checks a name against the convention | T1 | READ | — |
| `HERON-NAM-REN-003` | Auto Rename Agent | Renames — **only after identity exists**, never before | T1 | MODIFY | — |
| `HERON-NAM-TAX-004` | Taxonomy Agent | Maintains the classification scheme | T2 | — | — |
| `HERON-NAM-KEY-005` | Keyword Agent | Search terms and synonyms — "duct", "ductwork", "supply air" | T1 | — | — |
| `HERON-NAM-MET-006` | Metadata Agent | Metadata completeness and schema validity | T1 | — | — |
| `HERON-NAM-REF-007` | Reference Update Agent | After any rename or move: imports, references, metadata, registry, docs. **No broken references** | T1 | MODIFY | — |

## 14. GitHub — 10

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-GIT-MAIN-001` | GitHub Agent | Owns repository interaction | T1 | PUBLISH | — |
| `HERON-GIT-REP-002` | Repository Agent | Structure, remotes, settings | T1 | PUBLISH | — |
| `HERON-GIT-BRN-003` | Branch Agent | Branching strategy | T1 | MODIFY | — |
| `HERON-GIT-CMT-004` | Commit Agent | Stages and writes commits | T2 | MODIFY | — |
| `HERON-GIT-PR-005` | Pull Request Agent | Opens PRs — **explicit confirmation, every time** | T1 | PUBLISH | — |
| `HERON-GIT-ISS-006` | Issue Agent | Issues and triage | T1 | PUBLISH | — |
| `HERON-GIT-REL-007` | Release Agent | Tags and publishes releases | T1 | PUBLISH | — |
| `HERON-GIT-VER-008` | Version Agent | Semantic versioning and compatibility promises | T1 | — | — |
| `HERON-GIT-CHG-009` | Change Detection Agent | Detects upstream changes affecting fragments or skills | T1 | READ | — |
| `HERON-GIT-COM-010` | Community Contribution Agent | Prepares a submission. **Per-item human review of the actual payload** | T2 | PUBLISH | — |

## 15. Installation & Update — 13

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-INS-ORC-001` | Installation Orchestrator | Owns the install workflow | T1 | ADMIN | — |
| `HERON-INS-ENV-002` | Environment Detection Agent | Windows, Revit versions, .NET runtimes, storage, dependencies | T1 | READ | — |
| `HERON-INS-RVT-003` | Revit Installation Agent | Deploys the add-in per version, per-user, no admin rights | T1 | ADMIN | — |
| `HERON-INS-MCP-004` | MCP Installation Agent | Registers the MCP server with the host | T1 | ADMIN | — |
| `HERON-INS-DEP-005` | Dependency Agent | Checks and installs dependencies. **Confirm, never automatic** | T1 | ADMIN | — |
| `HERON-INS-CFG-006` | Configuration Agent | Writes initial configuration | T1 | ADMIN | — |
| `HERON-INS-BRN-007` | Brain Initialization Agent | Initialises knowledge stores | T1 | ADMIN | — |
| `HERON-INS-RAG-008` | RAG Initialization Agent | Creates vector store, indexes, embedding config | T1 | ADMIN | — |
| `HERON-INS-HLT-009` | Health Check Agent | Verifies the install end to end | T1 | READ | — |
| `HERON-INS-PKG-012` | **Package Manager Agent** | Optional Heron capabilities — skill packs, Revit tool packs, additional providers. Resolves dependencies, checks compatibility, installs, registers, and can uninstall cleanly. Named in Part 4's mandatory list and previously unowned ↗ | T1 | ADMIN | — |
| `HERON-INS-SUP-013` | **Supply Chain Security Agent** | Anything arriving from outside: verify source, package identity and version, scan dependencies, check hashes and signatures, detect modification. **The highest-severity surface in the platform** once packages can install themselves ↗ | T2 | ADMIN | — |
| `HERON-INS-EXT-011` | **External Tool Manager** | Optional tooling — AI CLIs, development utilities, additional MCP servers. Detect, check compatibility, **ask permission**, install, configure, verify, register. Never silent ↗ | T1 | ADMIN | — |
| `HERON-INS-ONB-010` | First-Run Onboarding Agent | Guides the user once, then gets out of the way | T2 | READ | — |

## 16. Operations, Health & Resilience — 12

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-OPS-SCH-001` | Background Scheduler Agent | P0–P6 priority. User work always wins | T1 | — | — |
| `HERON-OPS-QUE-002` | Queue Manager | Background work queue | T1 | — | — |
| `HERON-OPS-RES-003` | Resource Manager | CPU, RAM, disk, AI spend, **Revit responsiveness**. Pauses background work | T1 | — | — |
| `HERON-OPS-HLT-004` | Platform Health Agent | HEALTHY / WARNING / DEGRADED / FAILED across every component | T1 | READ | 3 |
| `HERON-OPS-DIA-005` | Self-Diagnostics Agent | *"Diagnose Heron"* — one clean report. **Built 2026-09-07** — `mcp/server/heron_diagnose.py`, served as `heron_diagnose`. It COMPOSES rather than reimplements: `heron_health` for the live picture, the brain seam for the knowledge layer, the Compatibility Matrix for releases, the Capability Gap report for history. Its contract is that it never raises — the first test written against it broke every probe in turn and got an exception each time, so the guard moved to the one place every probe passes through | T1 | READ | — |
| `HERON-OPS-HEA-006` | Self-Healing Agent | Repairs **derived** state freely; proposes everything else | T1 | MODIFY | — |
| `HERON-OPS-STP-007` | **Emergency Stop Agent** | Global halt. Sticky. **No trigger since 2026-09-06** — the ribbon button was removed ([D-46](DECISIONS.md)); the gates it feeds are still in place ↗ | T1 | READ | **6** |
| `HERON-OPS-SHD-012` | **Shadow Execution Agent** | The harness, not the teacher. Runs a candidate **in parallel** with the production one, captures both results, and guarantees the candidate can modify nothing. Works for fragments and workflows, not only agents ↗ | T1 | READ | — |
| `HERON-OPS-SAF-008` | Safe Mode Agent | Disables recent components, returns to last-known-good | T1 | ADMIN | — |
| `HERON-OPS-FLG-009` | Feature Flag Agent | Flags for staged rollout and shadow running | T1 | ADMIN | — |
| `HERON-OPS-UPD-010` | Update Agent | Detects, downloads, migrates, validates, **rolls back** | T1 | ADMIN | — |
| `HERON-OPS-OBS-011` | Observability Agent | **Latency, and the share of requests answered with no thinking at all.** Corrected 2026-09-09 ([D-58](DECISIONS.md)): this row used to read *"latency, token usage, model calls per request, cost per request"* and three of those four are things Heron cannot see — [D-01](DECISIONS.md) puts every model call in the host, and `heron_embed` runs locally with no tokens and no cost. **Token usage and cost per request are host-provided**, like the four orchestrator agents. **Model calls per request is replaced** by the number Heron can see and that [19 §5](19-context-and-cost.md) actually cares about: how often the cache or identity route answered, so no model was needed. The latency half is measured today by [`tools/measure-brain.py`](../tools/measure-brain.py) for the brain and `heron_gaps.py` for the Revit side | T1 | READ | — |

## 17. Documentation — 9

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-DOC-API-001` | API Documentation Agent | Generated from tool schemas | T1 | — | — |
| `HERON-DOC-AGT-002` | Agent Documentation Agent | Generated from the agent registry | T1 | — | — |
| `HERON-DOC-SKL-003` | Skill Documentation Agent | Generated from skill metadata. **Built 2026-09-15** — `tools/generate-skill-catalog.py`, the parallel to the fragment catalogue one layer up. Every skill with the words somebody actually says to reach it, and an **effective status**: the lowest rung on [09](09-skills-and-fragments.md)'s ladder among the fragments serving it. A card can say anything; the chain underneath is the fact. Reachability is per Revit release, never overall | T1 | — | — |
| `HERON-DOC-FRG-004` | Fragment Documentation Agent | Generated from fragment metadata. **Built 2026-09-08** — `tools/generate-fragment-catalog.py`, the parallel to the agent map one layer up. Every fragment on one searchable page, including whether its declared negative case can actually be run. It reported 0 stranded cases on its first run against a library holding 18, which is why a generator acquired a test | T1 | — | — |
| `HERON-DOC-REL-005` | Release Notes Agent | Structured notes per release | T2 | — | — |
| `HERON-DOC-ARC-006` | Architecture Documentation Agent | Keeps architecture docs in step with the registries | T2 | — | — |
| `HERON-DOC-RDM-007` | README Agent | Keeps the README current | T2 | — | — |
| `HERON-DOC-VAL-009` | **Documentation Validation Agent** | Do the documents match reality. Recomputes every stated count from its source, checks every internal link resolves, flags any figure asserted rather than derived ↗ | T1 | READ | — |
| `HERON-DOC-CHG-008` | Change Log Agent | Added / Improved / Fixed / Deprecated per version | T1 | — | — |

---

## Summary

| Department | Agents | T1 | T2 | T3 |
|---|---|---|---|---|
| Orchestration & Communication | 6 | 1 | 4 | 1 |
| Revit Engineering | 36 | 34 | 2 | 0 |
| MCP / Bridge | 12 | 12 | 0 | 0 |
| Session & Bridge Management | 5 | 5 | 0 | 0 |
| Knowledge & RAG | 17 | 11 | 4 | 2 |
| Fragment Lifecycle | 9 | 3 | 2 | 4 |
| **User & Personalization** | **3** | 2 | 1 | 0 |
| **Learning & Self-Growth** | **4** | 2 | 1 | 1 |
| **Skill Lifecycle** | **6** | 1 | 3 | 2 |
| Import & Migration | 14 | 7 | 4 | 3 |
| Standards & BIM QA | 14 | 1 | 12 | 1 |
| Development | 21 | 11 | 7 | 3 |
| Agent Lifecycle & HR | 17 | 6 | 8 | 3 |
| Kernel & Platform | 19 | 17 | 2 | 0 |
| Workspace & Folder Architecture | 12 | 10 | 2 | 0 |
| Naming & Taxonomy | 7 | 5 | 2 | 0 |
| GitHub | 10 | 8 | 2 | 0 |
| Installation & Update | 13 | 11 | 2 | 0 |
| Operations, Health & Resilience | 12 | 12 | 0 | 0 |
| Documentation | 9 | 6 | 3 | 0 |
| **Reporting & Output** | **4** | 2 | 2 | 0 |
| **Total** | **250** | **167** | **63** | **20** |

**[NOTE]** The distribution is the point. **167 of 250 agents never call a model** — they are ordinary
classes with a method or two. Of the rest, 63 make one scoped call and 20 run a real agentic loop.

Read that way, the platform is a normal application with about 167 services, 63 narrow model calls, and
20 genuine agentic workflows. That is a tractable system, not an intimidating one.

**Phase 0 and Phase 1 need 49 of these** — the ones carrying a step number in the tables above.
See [08](08-agent-catalog.md) and [27](27-build-order.md).

> **Correction, 2026-08-27:** this line said *"about 20"*, and [08](08-agent-catalog.md) said *"45 …
> the other 175"*. Counting the rows gives **46** (11 · 2 · 7 · 9 · 6 · 14 across steps 1–6), leaving
> **201**. Three wrong numbers about the same set, in two documents, none of them derived from the
> rows they describe. `tools/check-metadata.py` now counts the rows and fails if a sentence here
> disagrees, which is the only reason to trust the figure above over the three it replaces.

---

## Completeness audit

**[NOTE]** Run 2026-08-27. Method: take every capability named in the four specification parts, the
field notes and the Golden Rules, and check that **some agent owns it**. A requirement with no owner is
a gap; an agent with no requirement is bloat.

**Five gaps found and closed** — each traceable to a specific requirement that had no owner:

| Requirement | Source | Was owned by | Now |
|---|---|---|---|
| Unit convention, feet vs millimetres | [04 §6a](04-heron-mcp.md) | nobody | `Unit Conversion Agent` |
| *"Undo everything Heron did today"* | [PROPOSALS B3](PROPOSALS.md) | nobody — Transaction Safety owns one operation, not a session | `Session Rollback Agent` |
| No text Heron reads may raise its permission | [Golden Rule 19](14-golden-rules.md) | the rule existed; nothing detected an attempt | `Content Trust Agent` |
| Package manager | [Part 4](00d-additional-requirements.md) mandatory list | nobody | `Package Manager Agent` |
| Supply-chain security | [Part 4 §43](00d-additional-requirements.md) | nobody | `Supply Chain Security Agent` |

**Deliberately still unowned**, and correctly so:

| Requirement | Why no agent yet |
|---|---|
| Marketplace | Explicitly deferred ([ROADMAP](ROADMAP.md) Phase 7) until licensing and liability are settled. `Community Contribution` covers submission |
| AutoCAD / Navisworks / IFC / Rhino adapters | Deferred until Revit works properly. The Capability Registry is what will make them additive |
| Localization | Open question [Q-17](OPEN-QUESTIONS.md). English only until answered |
| Multi-user server, admin enforcement | [Q-32](OPEN-QUESTIONS.md) — company knowledge as a shared repository, not a server |

**Coverage as it stands:** every capability named in the specifications, every Golden Rule that can have
an enforcer, and every finding from the field notes now has exactly one owning agent — and every
creating agent has a named validator.

The registry is **complete against what has been specified.** It will grow again, but from the
Capability Gap report reading real usage — not from another pass through the documents.

---

## Every creator has a named validator

**[NOTE]** Added 2026-08-27 at the owner's instruction: *whatever the AI creates, there must be a
checking and validating agent for it.*

This is [Golden Rule 7](14-golden-rules.md) — *one agent creates, another validates* — made auditable.
The rule is only real if the pairing can be **checked**, so the pairs are listed. A creating agent with
no named validator is a defect in this registry, not an acceptable state.

| What is created | Created by | Validated by |
|---|---|---|
| Code | `Code Generation` | `Code Review` + `Security Review` + `Unit/Integration/Revit Test` + `QA` |
| A fragment | `Fragment Creation` | `Fragment Validation` |
| A changed fragment | `Fragment Update` | `Fragment Validation` + `Regression Testing` |
| A skill | `Skill Creation` | `Skill Validation` |
| A composed skill | `Skill Composition` | `Skill Validation` *(acyclic graph, risk propagation)* |
| An agent | `Agent Builder` | `Agent Validation` — **never the builder** |
| A project / build config | `.NET Project Creation` | `Build` + `Unit Test` |
| A migrated import | `Migration` | `Validation` *(Import)* |
| A folder structure | `Folder Creation` | `Folder Validation` |
| A name | `Naming` | `Naming Validation` + `Reference Update` |
| Knowledge structure | `Knowledge Evolution` | `Knowledge Validation` |
| **A report** | `Report Composition` + `Report Rendering` | **`Report Validation`** *(is it right)* + `Report Redaction` *(may it leave)* |
| **Documentation** | the 8 Documentation agents | **`Documentation Validation`** |
| **An inferred standard** | `Reference Model Profiler` | **`Profile Validation`** |
| A workflow change | `Workflow Optimizer` | `Experiment` *(is the new shape actually better)* |
| An experiment verdict | `Experiment` | **a human** — see below |

### The three that were missing, and why they matter

- **Report Validation.** `Report Redaction` gated whether a report may *leave*; nothing checked whether
  it was *true*. A report stating 47 failures when there are 52 is worse than no report, because it
  will be acted on and it looks authoritative.
- **Documentation Validation.** Documentation is generated from the registries, so when generation
  drifts, the documents lie confidently. **This page proved it:** it asserted 166 agents while its own
  departments summed to 196. Nothing would have caught that except a person adding up columns by hand.
  Recomputing every stated figure from its source is exactly this agent's job.
- **Profile Validation.** A standard inferred from a reference model is a **hypothesis**, not a
  standard. Without a validator, one project's mistake becomes the company rule.

### Where the chain stops

Validation cannot recurse forever — something must eventually be trusted. Two rules end it:

1. **A validator is never the creator.** That single constraint provides most of the value, at one hop.
2. **The chain terminates at a human**, at exactly one point: the `PROVEN → PRODUCTION` transition
   ([Golden Rule 6](14-golden-rules.md), [13](14-golden-rules.md)). Everything below that may be
   machine-validated; nothing becomes trusted-by-default without a person.

That is why no validator has a validator. The last check is a signature.

---

## Seniority — worker, pro, manager, scientist

**[NOTE]** Added 2026-08-27. The owner asked about *researcher, scientist, manager, worker, pro* agents.

Four of those five already exist — **as the tier system**, arrived at from the cost side rather than the
company side. Two independent routes reaching the same structure is a good sign, and the right response
is to **map the metaphor onto the existing vocabulary, not to add a second one**. Two names for one
concept is precisely the six-competing-vocabularies problem that
[24 — The Unified Trust Model](24-trust-model.md) exists to undo.

| Company role | Heron | Count | What it means in practice |
|---|---|---|---|
| **Worker** | **T1** — deterministic service | 167 | Does one job, the same way every time. No judgement, no model call, no cost |
| **Pro / skilled** | **T2** — one scoped model call | 63 | One judgement over ambiguous input, then out of the way |
| **Senior / lead** | **T3** — agentic loop | 20 | Owns a hard problem end to end, decides its own steps |
| **Manager** | **Orchestrator** + **Workflow Engine** | 2 | Decides *what* happens and ensures it *happens correctly*. Deliberately **not** one manager per department — [Part 2 §83](00b-master-specification-agent-os.md) forbids the extra hops |
| **Researcher** | `Research Agent` | 1 | Finds out what is already known. Everything it returns carries a citation |
| **Scientist** | `Experiment Agent` | 1 | Finds out what **nobody** knows yet — designs a comparison and reports a verdict with evidence |

### Why researcher and scientist are two agents, not one

The [split test](#when-to-split-an-agent-and-when-not-to) separates them cleanly, and the distinction is
real:

- **Research** answers *"what is already known?"* It retrieves — from documentation, standards, prior
  art, the knowledge base. Its failure mode is a **wrong citation**.
- **Experiment** answers *"which of these is actually better?"* It compares — two fragments, two
  approaches, before and after. Its failure mode is a **wrong conclusion**, which is worse, because it
  gets acted on.

Different tier (T2 vs T3), different failure mode, different consumers — Research serves the Brain,
Experiment serves Fragment Evolution and Agent Optimizer, which have no other way to answer *"is this
change worth keeping?"*

### Why there is no separate manager layer

A department head per department would add a hop to every request, for coordination the Orchestrator and
Workflow Engine already provide. [Part 2 §83](00b-master-specification-agent-os.md) is explicit:

> *ONE USER REQUEST → ONE ORCHESTRATED WORKFLOW → ONLY REQUIRED AGENTS*

Middle management is the thing that rule exists to prevent. If the Orchestrator ever becomes the
bottleneck the field notes predict, the fix is the **Capability Registry** doing more of the routing —
not a layer of managers between it and the work.

---

## When to split an agent, and when not to

**[NOTE]** Added 2026-08-27 after a good question: *should every kind of work get its own agent —
checking, updating, creating, all separate?*

Mostly yes, and the `.NET Agent` above was split into three for exactly that reason. But "separate verb,
separate agent" taken to its limit produces thousands of agents and a system nobody can hold in their
head. [Golden Rule 2](14-golden-rules.md) pulls one way; over-decomposition
([09 §6](09-skills-and-fragments.md)) pulls the other.

**Split when any of these differ. Do not split when none of them do:**

| Test | Why it matters |
|---|---|
| **Risk level** | Checking is `READ`; updating is `MODIFY`. One permission gate cannot serve both honestly — that alone justifies a split |
| **Consumers** | Compatibility checking is called constantly; project creation runs during scaffolding only. Different callers, different lifetimes |
| **Failure mode** | A wrong check gives a wrong answer. A wrong update breaks the build. They fail differently and are tested differently |
| **Tier** | If one half needs a model call and the other does not, splitting removes cost from the common path ([02 §6](02-architecture-overview.md)) |
| **Lifecycle** | If one half changes every Revit release and the other never changes, they should version independently |

**The .NET split passes on four of five** — risk, consumers, failure mode and lifecycle. That is why it
was done.

**A counter-example, so the rule has teeth:** `C# Agent`, `.NET Compatibility Check Agent` and
`Revit API Domain Agent` are all *"know a technical domain, answer questions about it."* Same risk, same
consumers, same tier, same failure mode. Those are marked as merge candidates
([08](08-agent-catalog.md)), not split further.

> **The list grows from real gaps, not from grammar.** A new agent should be traceable to something that
> was needed and had no owner — which is what the [Capability Gap Agent](#10-agent-lifecycle--hr--17)
> exists to detect.

---

## 17a. Reporting & Output — 4 *(new department)*

**[NOTE]** Added 2026-08-27 from a good question: *does producing a visual report need its own agent?*

Yes, and nothing covered it. The Documentation department documents **Heron itself**; nothing produced
output about **the user's model**. A standards check with 47 failures, a clash report, a model health
baseline or a *"what did Heron change?"* summary are all far more useful as a page than a paragraph.

**And a report is an egress surface.** A page carrying model data can leave the machine — which makes
this a [Golden Rule 12](14-golden-rules.md) concern, not a presentation concern. That is why redaction
is a separate agent with its own gate rather than a step inside rendering.

| ID | Agent | Does | Tier | Risk | Step |
|---|---|---|---|---|---|
| `HERON-RPT-CMP-001` | **Report Composition Agent** | Decides *what goes in* and at what depth for this reader — a modeller wants the 47 failures, a BIM manager wants the trend. Judgement, so a model call ↗ | T2 | READ | — |
| `HERON-RPT-RND-002` | **Report Rendering Agent** | Deterministic templates → page, PDF, schedule, CSV, image. **No model call** — data in, document out, same input same output ↗ | T1 | READ | — |
| `HERON-RPT-VAL-004` | **Report Validation Agent** | Does the report say what the data says. Counts match the query, nothing dropped silently, every figure traceable to its source. A report claiming 47 failures when there are 52 is worse than no report ↗ | T2 | READ | — |
| `HERON-RPT-RED-003` | **Report Redaction & Release Agent** | The gate before a report can be shared or leave the machine. Strips project identifiers, enforces scope, blocks confidential-project egress. **A report is an egress surface** ↗ | T1 | PUBLISH | — |

**Why three and not one** — the [split test](#when-to-split-an-agent-and-when-not-to), applied:

| Test | Result |
|---|---|
| **Risk** | Composition and rendering are `READ`. Redaction is a `PUBLISH` gate. **Differs** |
| **Tier** | Composition is judgement (T2). Rendering is templates (T1). **Differs** — and keeping rendering deterministic removes a model call from every report |
| **Failure mode** | Bad composition = wrong content. Bad rendering = broken page. Bad redaction = **client data leaves the building**. **Differs, severely** |
| **Consumers** | Rendering is reused by exports and documentation. Redaction is reused by the GitHub and Community agents. **Differs** |

Four of five. The split holds.

### On a separate "show widget" agent — **not required**

**[NOTE]** The same test says no, and it is worth recording why so it is not revisited.

An inline widget — a preview panel, a picker, a progress display — is the *same work* as a report at a
different size. Same risk, same consumers, same tier, same failure mode as rendering. The only distinct
decision is *"should this be shown visually or said in a sentence?"* — and that is presentation, which
already belongs to the **Communication / Persona Agent** (`HERON-ORC-PER-003`).

Adding a widget agent would create exactly the near-duplicate the Fragment Merge Agent exists to
prevent. The Persona Agent decides the format; the Rendering Agent produces it.

---

## Maintaining this list

This is a **document today and a generated artefact later**. Once the Agent Registry
([`HERON-AHR-REG-008`](#10-agent-lifecycle--hr--17)) exists, this page should be produced from it by the
Agent Documentation Agent rather than hand-maintained — otherwise it drifts within weeks
([06 §12](06-heron-platform.md)).

Entries marked **↗** have responsibilities written during review rather than quoted from a
specification. They are proposals and should be confirmed as each agent is built.
