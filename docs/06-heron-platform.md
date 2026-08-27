# 06 — Heron Platform (Part 4)

> Derived from [Master Specification](00-master-specification.md) §7, §31, §32, §36–39, §60–63, §70, §71.
> Installation and updates have their own document: [07](07-installation-and-update.md).
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Responsibility

Installer, update system, package manager, agent registry, agent creator, agent training, agent validation, workspace management, GitHub integration, security, logging, backup, recovery, health monitoring, configuration, documentation, release management.

---

## 2. Workspace architecture

Conceptual separation (physical names finalised at implementation time, owned by the Folder Architecture Agent):

```text
Heron
|
+-- Core
+-- Agents
+-- Revit
+-- MCP
+-- Brain
+-- RAG
+-- Skills
+-- Fragments
+-- Memory
+-- Projects
+-- Company
+-- Community
+-- Packages
+-- Configuration
+-- Logs
+-- Tests
+-- Documentation
+-- Cache
+-- Backup
```

**[NOTE]** One distinction is missing and matters a great deal: **product vs. data**.

| Class | Contents | Lifecycle |
|---|---|---|
| **Product** | Core, Agents, Revit, MCP, Packages | Replaced wholesale on update. User never edits. |
| **Data** | Brain, Skills, Fragments, Memory, Projects, Company, Logs, Backup | Belongs to the user. Must survive every update, uninstall and reinstall. |
| **Derived** | Cache, vector indexes | Safe to delete at any time. Rebuildable. |

If these three sit in one tree, the first update that "cleans and reinstalls" destroys the user's accumulated knowledge — the very thing the platform exists to build. Recommendation: product under an install location, data under the user profile (or a user-chosen workspace), derived under a cache location, and the update system physically unable to write to the data class.

Tracked as [Q-13](OPEN-QUESTIONS.md).

---

## 3. Agent Registry

Every agent has an entry:

```text
Agent ID
Agent Name
Department
Responsibility
Capabilities
Inputs
Outputs
Dependencies
Required Skills
Allowed Tools
Version
Status
Health
Owner
```

Statuses: `PROPOSED` → `DEVELOPMENT` → `TESTING` → `APPROVED` → `ACTIVE` → `DISABLED` → `DEPRECATED` → `ARCHIVED`

**[NOTE]** Add three fields the spec omits:

| Field | Why |
|---|---|
| **Tier** (T1 service / T2 reasoner / T3 worker) | See [02 §6](02-architecture-overview.md). Makes cost and latency inspectable, and stops silent promotion of a cheap service into an expensive LLM call. |
| **Risk level** | Which permission level this agent's actions require. Drives the security gate. |
| **Contract schema path** | The JSON Schema for INPUT/OUTPUT. Makes contracts machine-checkable and diffable across versions. |

---

## 4. Agent Creation & Training

The **Agent Creator Agent** decides whether a new capability warrants a new agent:

1. Analyze requirement → 2. Determine responsibility → 3. Check existing agents → 4. Check overlap → 5. Design → 6. Specification → 7. Implementation → 8. Tests → 9. Validate → 10. Register → 11. Approval → 12. Activate.

The **Agent Training Agent** supplies architecture rules, coding standards, company standards, naming standards, security rules, domain knowledge, examples, approved fragments and approved skills before an agent goes active.

**[NOTE — the sharpest risk in the whole specification]**

An AI system that autonomously writes, approves and activates new agents which then execute code against live project models is a genuinely dangerous construction. Not theoretically — practically. The failure mode is not dramatic; it is a subtly wrong agent quietly promoted to `ACTIVE` and then applied to a client's model at 4pm on a submission day.

Proposed hard boundaries, to be treated as non-negotiable:

1. **Agent creation is always a proposal.** `PROPOSED` is the only status the system may assign to itself.
2. **A human approves the `PROPOSED → APPROVED` transition.** Always. This is the one place where "the user does BIM work, Heron does everything else" is deliberately broken, and it should be.
3. **New agents run in a sandbox first** — a detached copy of a model, never the live document.
4. **`ACTIVE` requires a passing test suite** the agent did not write for itself.
5. **Self-modification of Core is forbidden.** Heron may propose changes to its own core; it may not apply them.

The same reasoning applies to Fragment promotion — see [09](09-skills-and-fragments.md).

---

## 5. Agent Retirement

Retire when: unused, duplicated, unreliable, replaced, incompatible, or insecure. Retirement **preserves history and allows rollback** — archive, never delete.

---

## 6. Self-Growing Agents

Agent Creator, Agent Evaluator, Agent Trainer, Agent Registry, Agent Optimizer, Agent Retirement Agent, Architecture Monitor, Capability Gap Agent.

The **Capability Gap Agent** detects *"the system repeatedly needs capability X, but no suitable agent exists"* and proposes one.

**[NOTE]** This is the most valuable agent in the entire specification and probably the cheapest to build. It is essentially a report over the audit log — which requests failed, which required fallback, which the user corrected. It needs no autonomy at all to be useful. Recommendation: build it early, as a **read-only report**, long before agent auto-creation exists. It will tell you what to build next, from real usage rather than guesswork.

---

## 7. Naming & Taxonomy Department

Naming Agent, Naming Validation Agent, Auto Rename Agent, Taxonomy Agent, Keyword Agent, Metadata Agent, Reference Update Agent.

Naming must be predictable and searchable.

**[NOTE]** The Auto Rename Agent is only safe because of **Knowledge Identity** (§30): identity is an ID, never a filename. Order of operations matters — identity must exist *before* anything is allowed to rename automatically. Renaming files that are identified by name is how knowledge bases lose track of themselves.

---

## 8. Folder Architecture Department

Workspace Architect, Folder Creation, Folder Validation, Folder Repair, File Placement, Template, Path Manager, Migration, Cleanup, Backup, Restore, File Registry.

**[NOTE]** `Cleanup Agent` and `Folder Repair Agent` both delete or move user files. Both must be `MODIFY`-gated, dry-run by default, and must never touch the **data** class without explicit per-run confirmation. An automatic cleanup that removes a fragment the user spent a month refining is unrecoverable trust damage.

---

## 9. GitHub Department

GitHub Agent, Repository Agent, Branch Agent, Commit Agent, Pull Request Agent, Issue Agent, Release Agent, Version Agent, Change Detection Agent, Community Contribution Agent.

> GitHub should never be treated as the user's private memory automatically.

**[NOTE]** Push, PR creation and release publishing are `PUBLISH`-level and outward-facing — they leave the machine and cannot be fully retracted. Each requires explicit per-action confirmation. In particular, **nothing derived from a client project may be pushed to a public repository, ever**, regardless of permission level. That should be a hard filter in code, not a policy in a document.

---

## 10. Package System

Optional capabilities install independently: additional BIM skills, additional Revit tools, OCR, document analysis, image analysis, speech, GraphRAG, additional AI providers, additional BIM platforms.

User says *"Install this capability."* The Package Manager resolves dependencies and compatibility.

**[NOTE]** Installing a package means **executing third-party code inside Revit, inside the user's project**. This is the highest-severity supply-chain surface in the platform. Before any marketplace exists, decide: signing, source allowlist, version pinning, and whether community packages may contain executable code at all (as opposed to declarative skills/fragments only). Tracked as [Q-18](OPEN-QUESTIONS.md).

---

## 11. Marketplace / Community

Every community package carries: version, author, dependencies, compatibility, quality score, approval status, security status.

**[NOTE]** This is a later-phase concern and should be explicitly deferred. It needs a legal position (licensing, liability for a package that damages a model), a moderation process, and an identity system. None of that should compete with getting one duct selected correctly.

---

## 12. Documentation Agents

API Documentation, Agent Documentation, Skill Documentation, Fragment Documentation, Release Notes, Architecture Documentation, README, Change Log.

**[NOTE]** Documentation should be **generated from the registries and metadata that already exist**, not written separately and kept in sync by hand. The agent registry, skill metadata and fragment metadata are already structured; the docs are a rendering of them. Anything hand-maintained alongside them will drift within weeks.
