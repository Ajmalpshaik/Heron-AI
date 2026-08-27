# 21 — Resilience, Safety & Operations

> Derived from [Master Specification Part 2](00b-master-specification-agent-os.md)
> §38–§45, §49–§58, §75–§79.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Dependency Graph — **new in Part 2, and important**

```text
Skill -> Fragment -> Revit API -> .NET -> Package
```

```text
.NET Update -> Dependency Graph -> Affected Fragments
-> Affected Skills -> Affected Agents -> Testing
```

**[NOTE]** This is the missing piece that makes [D-05](DECISIONS.md) (Revit 2020 → latest) and
[Golden Rule 3](14-golden-rules.md) operationally possible.

Without a dependency graph, "does this change break anything?" can only be answered by testing
everything — which, across eight Revit versions and thousands of fragments, is not affordable. With
one, the blast radius of a change is computable, and only the affected subset needs the full matrix.

It is also what turns the [Regression Testing Agent](13-testing-and-quality.md) from a good intention
into a targeted operation.

### What it needs to hold

| Edge | Example |
|---|---|
| skill → skill | `Model QA` composes `Parameter Validation` |
| skill → fragment | `Select Ducts` uses `filter-by-category` |
| fragment → fragment | one fragment calls another |
| fragment → Revit API surface | uses `FilteredElementCollector`, `OST_DuctCurves` |
| fragment → runtime | requires `net48` or `net8` |
| fragment → package | third-party dependency |
| capability → agent | which agents provide it |
| agent → capability | what it provides |

**[NOTE]** Both specifications name a **Knowledge Graph** in the architecture line, but this dependency
graph is the only place it is actually specified. That is fine — a dependency graph over capabilities,
skills, fragments and API surfaces is far more useful here than a general semantic knowledge graph, and
it has a clear job. Recommend building exactly this and not more.

**Storage:** this is ordinary relational data with recursive queries. SQLite handles it well
(`WITH RECURSIVE`), and it sits naturally beside the knowledge store recommended in
[05 §5](05-heron-brain.md). A dedicated graph database is not warranted. Tracked as
[Q-31](OPEN-QUESTIONS.md).

---

## 2. Safety architecture — autonomy proportional to risk

| Risk | Autonomy | Examples |
|---|---|---|
| **Low** | Automatic | search, classification, indexing, analysis, backup, health check |
| **Medium** | Controlled automatic | file rename, file movement, code generation, fragment updates |
| **High** | Requires approval | deleting production knowledge, modifying repositories, publishing releases, destructive Revit operations, major architecture changes |

**[NOTE]** This maps cleanly onto the permission levels in [12 §1](12-security-and-permissions.md):
Low ≈ `READ`/`ANALYZE`, Medium ≈ `SUGGEST`/`EXECUTE`, High ≈ `MODIFY`/`PUBLISH`/`ADMIN`.

One adjustment: **`MODIFY` on a live project model belongs in High, not Medium.** Part 2 places "code
generation" and "fragment updates" in Medium, which is right — those touch Heron's own artefacts. But
moving 247 ducts in a client's model is a different category of act, and belongs behind a preview and a
confirmation ([Golden Rule 12](14-golden-rules.md)).

---

## 3. Human approval gates

> Approval occurs only at **meaningful boundaries**.

Useful: *"I found a new implementation that changes the existing production fragment for Revit 2020–2027.
Approve replacement?"*

Never: *"I am searching the fragment database."*

**[NOTE]** This is an important corrective. Over-asking destroys a tool as surely as under-asking
damages a model — a user who is asked twelve times per task starts clicking Yes without reading, and
then the gate protects nothing.

Proposed rule of thumb:

> **Ask when the answer would change what Heron does, and the user is the only one who can know it.**

Ask about: replacing a production fragment · modifying a live model · publishing anything · installing
third-party code · resolving a knowledge conflict Heron cannot settle.

Never ask about: searching, ranking, indexing, classifying, health checks, or anything read-only.

---

## 4. Emergency Stop — **new in Part 2, and necessary**

```text
STOP ALL AGENTS
STOP BACKGROUND TASKS
DISABLE AUTO-UPDATE
DISABLE AUTO-EXECUTION
```

**[NOTE]** Correctly identified as increasingly important as autonomy grows. Three requirements to make
it real rather than nominal:

1. **It must be reachable when Heron is misbehaving.** A stop button implemented *inside* the agent
   system is useless when that system is stuck. It belongs in the **Revit add-in UI** — a ribbon button
   that works regardless of what the Python side is doing — plus a file-based kill switch the add-in
   checks before executing anything.
2. **Stopping must leave the model clean.** A stop mid-operation rolls back the open `TransactionGroup`
   ([Golden Rule 11](14-golden-rules.md)). Never a half-applied change.
3. **It must be sticky.** After an emergency stop, Heron stays stopped until a person restarts it. It
   does not quietly resume on the next request or after a restart.

**[NOTE]** Worth pairing with the **panic button** proposed in [PROPOSALS B3](PROPOSALS.md) —
*"undo everything Heron did today"*. Emergency Stop halts what is running; the panic button reverses
what already ran. Together they are the two controls that make people willing to try an autonomous tool
on real work at all.

---

## 5. Health monitoring

Monitored: Revit Connection · MCP · Agents · RAG · Vector DB · Database · Storage · GitHub ·
AI Provider · Background Workers. Each reports `HEALTHY` · `WARNING` · `DEGRADED` · `FAILED`.

**[NOTE]** One structured health object, one overall verdict, per-check detail — consumed by the
installer, the self-healing system, the user-facing "is Heron working?" answer, and failure analysis.
See [03 §3](03-heron-revit.md).

`DEGRADED` is a useful addition over Part 1's binary framing: Heron working without GitHub, or without
the vector index, is degraded rather than failed, and should say so instead of pretending everything
is fine.

---

## 6. Self-healing

| Condition | Flow | Permission |
|---|---|---|
| MCP disconnected | Detect → Recovery Agent → Reconnect → Health check | automatic |
| Vector index corrupted | Detect → Rebuild → Validate | automatic |
| Dependency missing | Detect → Install/Repair → Build → Validate | **confirm** |

**[NOTE]** The split from [07 §9](07-installation-and-update.md) holds: Heron may freely repair
**derived** state (indexes, caches, connections) and may freely *propose* anything else. Installing a
dependency means executing third-party code and should be confirmed, not automatic.

---

## 7. Source of Truth Principle

```text
Canonical Knowledge  ->  Vector DB
```

> **The Vector DB is an index, not the ultimate source of truth.**

**[NOTE — this confirms the position in [05 §7](05-heron-brain.md)]**

Part 2 states it explicitly, and it is the rule that makes disaster recovery possible: if the index is
corrupt, deleting and rebuilding it must always be a safe action. Fragments and skills live as
human-readable, git-diffable files with metadata; the index is derived.

This also means **the index never needs backing up** — only the canonical knowledge does. That
simplifies the backup strategy considerably.

---

## 8. Backup and disaster recovery

Backup covers: configuration · agents · skills · fragments · metadata · database · vector indexes ·
memory · project knowledge. Versioned.

```text
Detect -> Repair -> Restore Configuration -> Restore Knowledge
-> Rebuild Vector Index -> Health Check
```

**[NOTE]** Refinement, following from §7: back up the **data class** ([06 §2](06-heron-platform.md)) and
configuration. Do **not** back up the vector index — rebuild it. Backing up a derived artefact adds
size and creates the risk of restoring a stale index over fresh knowledge.

**[NOTE]** Restore must be **tested**, not merely implemented. An untested restore path is not a
restore path — it is a belief. Recommend a periodic automated drill: restore into a scratch location,
rebuild the index, run health checks, compare.

---

## 9. Configuration management

Version-controlled: supported Revit versions · enabled agents · enabled skills · AI provider ·
model routing · security policies · update policies · company standards. Changes auditable.

**[NOTE]** Two constraints:

1. **Security and permission policy is configuration, so configuration is a security boundary.** Editing
   it is `ADMIN`. Nothing Heron reads — a document, a community package, a model comment — may change
   it ([Golden Rule 15](14-golden-rules.md)).
2. **Configuration is Product/Data-split too.** Machine-specific settings (Revit paths, which versions
   are installed) differ from portable policy (company standards, enabled skills). Only the second
   should be shareable or committed.

---

## 10. Migration architecture

```text
Heron v1 -> Migration Agent -> Schema Migration -> Knowledge Migration
-> Agent Migration -> Skill Migration -> Validation -> Heron v2
```

> Old data should not simply be discarded.

**[NOTE]** Every migration must be **idempotent** (safe to run twice), **versioned** (data records the
schema version it was written with), and **backed up first**. The vector index is the easy case —
delete and rebuild. Fragments, memory and the agent registry are the real work.

See [07 §8](07-installation-and-update.md).

---

## 11. Build pipeline and QA separation

```text
Requirement -> Architecture -> Code -> Static Analysis -> Build
-> Unit Tests -> Integration Tests -> Revit Tests -> Regression Tests
-> QA -> Package -> Release
```

> **No component should enter production by simply compiling successfully.**

> **Code QA ≠ Revit QA.** Both are required.

**[NOTE]** The separation of Code QA from Revit QA (§48) is a real insight and matches how BIM tooling
actually fails. Code that compiles, passes unit tests and reviews cleanly can still be wrong inside
Revit — wrong transaction handling, wrong element filter, wrong behaviour on a worksharing model, wrong
result on a linked model.

This is the strongest argument for solving the "how do we test against real Revit" problem properly
([13 §3](13-testing-and-quality.md), [Q-14](OPEN-QUESTIONS.md)). Every other test level can run in CI.
This one cannot, and it is the one that catches the failures that reach the user.

---

## 12. Background workers and priority

```text
P0 User interaction   P1 Required execution   P2 Required validation
P3 Maintenance        P4 Learning             P5 Optimization        P6 Cleanup
```

> If the user starts a heavy Revit operation, background indexing should reduce or pause its workload.

**[NOTE]** Part 2 expands Part 1's six levels to seven and adds the crucial behavioural rule: background
work **yields** to interactive work rather than merely ranking below it.

The three constraints from [11 §7](11-orchestration-and-workflows.md) still apply — never run P2–P6
during a user task, prefer running heavy work while Revit is closed, and keep background work cheap
(which is another argument for local embeddings, [05 §6](05-heron-brain.md)).

---

## 13. Audit architecture

```text
User Request -> Workflow ID -> Agents -> Knowledge Used -> Fragment Used
-> Code Generated -> Tests -> Result -> Changes
```

**[NOTE]** The **Workflow ID** is new in Part 2 and is exactly right. It is the correlation key that
ties one user sentence to every agent call, retrieval, model call, transaction and element touched — and
it is what makes the "what did Heron change?" report ([PROPOSALS B2](PROPOSALS.md)) a query rather than
a feature.

Combined with the fields recommended in [12 §5](12-security-and-permissions.md) — document identity,
permission decisions, element `UniqueId`s, transaction group name, model call count, per-stage duration
— the audit log becomes the single source for debugging, the cost meter, the capability gap report and
the trust scores. One append-only record, many readers.
