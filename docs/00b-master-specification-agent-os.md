# Heron AI — Advanced Internal Architecture, Agent Operating System & Self-Evolution Specification

> **Status:** Source-of-truth record, **part 2 of 4**, as provided by the owner on 2026-08-27.
> Part 1 is [00 — Master Specification](00-master-specification.md) (the platform and its organisation).
> Part 3 is [00c — Handover Baseline](00c-master-handover-baseline.md), **authoritative on the Golden
> Rules**; Part 4 is [00d — Additional Requirements](00d-additional-requirements.md).
> This part specifies **how Heron operates internally** as an autonomous engineering organisation.
>
> This file is the **verbatim architectural intent**. Do not edit it to "fix" it —
> record changes as decisions in [DECISIONS.md](DECISIONS.md).

| Field | Value |
|---|---|
| Document Type | Advanced Technical Architecture / System Design Handover |
| Platform | Heron AI |
| Primary Domain | BIM / Revit / MEP Engineering Automation |
| Architecture | Modular Multi-Agent + RAG + MCP + Knowledge Graph + Vector Database |
| Primary Principle | Human simplicity, machine complexity |
| Target | Production-grade, scalable, self-improving engineering platform |

---

## 1. Executive Definition

Heron AI should be designed as an **AI Engineering Operating System**, not as a collection of independent AI agents. The platform should behave similarly to a large engineering organization.

A user gives a requirement. The user should not need to decide:

- which agent should handle it
- which fragment should be used
- which skill is appropriate
- which Revit API is required
- which .NET version is required
- whether existing code can be reused
- whether new code is required
- how the code should be tested
- where the resulting files should be stored
- how metadata should be created
- whether the knowledge already exists
- whether the knowledge should become a new fragment
- whether an existing fragment should be updated
- whether the Vector DB needs updating

Heron AI makes those decisions internally. The platform therefore consists of two very different worlds.

**Human World**

```text
User -> Natural Language -> Result
```

**Machine World**

```text
Intent -> Context -> Planning -> Agent Selection -> Knowledge Retrieval
-> Capability Selection -> Implementation -> Validation -> Execution
-> Testing -> Learning -> Knowledge Evolution
```

The complexity belongs inside Heron AI.

## 2. Core Architectural Principle

> **Simple outside. Extremely intelligent inside.**

The platform should expose a very small interface while internally operating potentially hundreds or thousands of specialized components.

The number of agents should **not be fixed**. The architecture must allow 10 → 50 → 200 → 1,000 agents → potentially much more, **without redesigning the core platform**.

## 3. Heron AI as an Organization

```text
                         HERON AI
                            |
                    Executive Orchestrator
                            |
       +--------------------+--------------------+
       |                    |                    |
   Engineering          Knowledge             Platform
       |                    |                    |
   Revit                 RAG                 Infrastructure
   .NET                  Memory               Updates
   C#                    Skills               Packages
   MCP                   Fragments            Security
   Testing               Library              GitHub
   QA                    Standards            Workspace
       |                    |                    |
       +--------------------+--------------------+
                            |
                       User Interface
                            |
                          USER
```

This should be a **logical** organization, not necessarily separate software processes for every agent.

## 4. Agent Operating System

The internal agent framework should be treated as an **Agent Operating System**. The Agent OS manages:

agent registration · discovery · activation · execution · communication · permissions · health · versions · dependencies · lifecycle · performance · retirement · replacement

Agents should not independently manage these responsibilities.

## 5. Agent Registry

Every agent must have a permanent identity:

```text
Agent ID / Agent Name / Department / Role / Version / Status / Capabilities
Required Skills / Required Knowledge / Allowed Tools / Permissions / Dependencies
Input Contract / Output Contract / Health / Performance Score
Creation Date / Last Updated / Owner
```

Example:

```text
Agent ID:        HERON-REVIT-SEL-001
Name:            Revit Selection Agent
Department:      Revit Engineering
Responsibility:  Element selection
Supported:       Revit 2020-2027
Status:          PRODUCTION
```

## 6. Capability Registry

**Agent identity and capability identity should be separate.** An agent may provide multiple capabilities.

```text
Capability:  SELECT_DUCTS
Required:    Revit Selection Agent
Related:     OST_DuctCurves
```

The Orchestrator should search the **Capability Registry** rather than blindly searching agent names. This allows Heron to replace an agent without changing the user workflow.

## 7. Dynamic Agent Discovery

Request: *"Find all unconnected ducts."*

```text
Intent:        DUCT_CONNECTION_ANALYSIS
Capabilities:  Revit element discovery
               Duct analysis
               Connection analysis
               Result reporting
```

It then searches the capability registry. It does **not** select an agent because its name contains "duct".

## 8. Dynamic Agent Creation

Heron should identify missing capabilities. If a user repeatedly requests something no capability covers:

```text
Capability Gap Detected -> Gap Analysis Agent -> Check Existing Agents
-> Check Existing Skills -> Check Existing Fragments -> Check External Knowledge
-> Determine Requirement -> Propose New Agent
```

## 9. Agent HR System

Agent creation behaves like an HR department.

| Role | Function |
|---|---|
| Executive Orchestrator | *"We need a new capability."* |
| Agent HR | Job description, responsibility, required capabilities, dependencies, knowledge, tools |
| Agent Architect | Designs the agent |
| Agent Builder | Creates implementation |
| Training Agent | Provides required knowledge |
| QA Agent | Tests it |
| Registry Agent | Registers it |
| Deployment Agent | Activates it |

This creates a controlled self-expanding organization.

## 10. Agent Onboarding

A newly created agent should **never** immediately become trusted.

```text
PROPOSED -> DESIGNED -> IMPLEMENTED -> TRAINING -> TESTING
-> SHADOW MODE -> APPROVED -> PRODUCTION
```

**Shadow Mode:** the agent can observe and make recommendations **without actually modifying production data**. This is extremely important for safe self-evolution.

## 11. Agent Trust Score

Every agent has a dynamic trust score, factoring: successful executions, failed executions, user corrections, QA results, regression results, performance, security incidents, compatibility, age, usage frequency.

```text
Trust Score: 96.4%   ->  Production
Trust Score: Unproven ->  newly created agent
```

## 12. Agent Performance Monitoring

Continuously monitored: Success Rate · Failure Rate · Average Execution Time · Token Usage · Tool Usage · User Corrections · Rollback Frequency · Error Types · Compatibility Failures.

This feeds the Agent Optimizer.

## 13. Agent Retirement

```text
Active -> Under Review -> Replacement Found -> Migration -> Deprecated -> Archived
```

The old agent remains recoverable. **Never permanently delete important historical knowledge automatically.**

## 14. Agent Communication Protocol

Structured messages only:

```text
REQUEST / TASK / CONTEXT / INPUT / EXPECTED_OUTPUT / CONSTRAINTS
RESULT / ERROR / CONFIDENCE / STATUS
```

Agents should not dump huge amounts of context into one another. The **Context Manager** decides what information each agent actually needs.

## 15. Context Manager

A critical component. AI systems fail when given too much irrelevant information. The Context Manager decides:

what the user said · what project is active · what Revit version is active · what knowledge is relevant · what fragment is relevant · what skill is relevant · what previous conversation is relevant · what technical information is required

**Only required information should be passed to the agent.**

## 16. Context Compression

With 20,000 fragments, 5,000 skills, 500 agents and thousands of documents, the Orchestrator must never send all of them to the model.

```text
User Request -> Semantic Classification -> Relevant Domain -> Relevant Project
-> Relevant Capability -> Relevant Knowledge -> Minimal Context
```

Reduces token usage, latency, confusion, hallucination and cost.

## 17. Model Router

Heron should not always use the most expensive AI model.

| Task | Model |
|---|---|
| Simple task | Fast model |
| Complex reasoning | Stronger reasoning model |
| Code generation | Coding model |
| Document classification | Efficient model |
| Critical architectural decision | High-reasoning model + validation |

This creates an AI model hierarchy.

## 18. Model Fallback

```text
Primary Model -> Failure -> Fallback Model -> Validation
```

The system should never silently produce an inferior result without awareness.

## 19. RAG Operating System

RAG is not one simple "search documents" function. It is a complete retrieval pipeline:

```text
User Request -> Intent -> Domain -> Knowledge Scope
-> Semantic Search -> Keyword Search -> Metadata Filtering -> Vector Search
-> Candidate Ranking -> Conflict Detection -> Knowledge Validation -> Context Assembly
```

## 20. Knowledge Hierarchy

Knowledge has priority:

```text
1. Active Project Knowledge
2. Approved Company Knowledge
3. Proven User Knowledge
4. Approved Community Knowledge
5. General Heron Knowledge
6. Experimental Knowledge
7. Temporary Knowledge
```

Project-specific information should override generic information when appropriate.

## 21. Knowledge Trust

Every knowledge object has a trust level:

```text
EXPERIMENTAL -> UNVERIFIED -> TESTED -> VALIDATED -> PROVEN -> PRODUCTION -> DEPRECATED
```

RAG should prefer high-trust knowledge.

## 22. Knowledge Conflict Resolution

If two fragments disagree, Heron must not randomly choose one.

```text
Conflict Detected -> Compare Version -> Compare Source -> Compare Trust
-> Compare Project Context -> Compare Test History
-> Conflict Resolution Agent -> Decision
```

If confidence is insufficient: **ask the user.**

## 23. Fragment Identity System

Identity must be independent of filename, folder, code formatting, author and storage location. It is based on:

semantic purpose · capability · implementation identity · metadata · relationships

This prevents duplicate knowledge.

## 24. Fragment Duplicate Detection

```text
New Fragment -> Identity Extraction -> Semantic Comparison -> Metadata Comparison
-> Code Comparison -> Existing Fragment Search
```

| Result | Action |
|---|---|
| Exact duplicate | Already exists. No update required. |
| Similar | Existing fragment may be improved. |
| New | Create new fragment. |
| Conflict | Human / Resolution Agent review required. |

## 25. Fragment Versioning

```text
Fragment: DUCT-ENDCAP
  v1.0  Revit 2020
  v1.1  Revit 2020-2022
  v2.0  Revit 2020-2027
```

Older implementations remain available if still supported.

## 26. Fragment Branching

```text
Common Fragment
       |
       +-- Revit 2020-2021 implementation
       |
       +-- Revit 2022-2024 implementation
       |
       +-- Revit 2025-2027 implementation
```

The common **semantic capability remains one fragment**. Implementation details may branch internally. This is preferable to creating completely unrelated fragments.

## 27. Fragment Evolution Engine

Decides: `KEEP` · `UPDATE` · `EXTEND` · `SPLIT` · `MERGE` · `BRANCH` · `DEPRECATE` · `ARCHIVE`

The main mechanism for keeping the knowledge base clean.

## 28. Skill Architecture

A skill represents a reusable capability and can use multiple fragments.

```text
SMART DUCT DIMENSIONING SKILL
  Duct discovery fragment
  Geometry fragment
  Dimension placement fragment
  Revit transaction fragment
  Validation fragment
```

## 29. Skill Composition

A complex skill can be built from smaller skills.

```text
MODEL QA SKILL
 +-- Element Validation Skill
 +-- Parameter Validation Skill
 +-- Naming Skill
 +-- Geometry Skill
 +-- Standard Checking Skill
 +-- Reporting Skill
```

## 30. Personal Memory Architecture

```text
USER: Ajmal
 +-- Preferences
 +-- Working Style
 +-- Successful Workflows
 +-- Personal Skills
 +-- Personal Fragments
 +-- Personal Projects
```

This should not automatically become company knowledge.

## 31. Company Memory

BIM standards · naming standards · project standards · coding standards · approved workflows · approved fragments · approved skills.

Company knowledge has higher trust than experimental personal knowledge.

## 32. Project Memory

```text
Project A
 +-- BIM Standards / Revit Models / Project Skills
 +-- Project Fragments / Project Decisions / Project Memory
```

Project B must not automatically inherit Project A's project-specific decisions.

## 33. Community Knowledge

External until validated.

```text
Community -> Import -> Security Scan -> Compatibility Check
-> Quality Check -> Testing -> Approval -> Community Knowledge
```

## 34. External Folder Intelligence

The user provides `C:\MyOldTools`. Heron understands it without manual organization, inspecting: code, documentation, configuration, fragments, skills, images, metadata, dependencies, project files, manifests.

## 35. Intelligent Migration

Imported content should never simply be copied.

```text
SCAN -> UNDERSTAND -> CLASSIFY -> COMPARE -> RENAME
-> TRANSFORM -> VALIDATE -> PLACE -> INDEX
```

Only after successful validation does content enter the production architecture.

## 36. File Naming Intelligence

The Naming Agent understands: What is this? What does it do? Which domain? Which platform? Which version? Which component? — then generates a predictable name.

**Filename alone should never be the primary knowledge identity.**

## 37. Folder Architecture Intelligence

The Folder Architect maintains hierarchy, naming, ownership, dependencies, permissions, lifecycle and location.

```text
Folder Validator -> Detect -> Classify -> Recommend/Repair
```

Depending on permissions, Heron may automatically correct misplaced files.

## 38. GitHub Intelligence

```text
Requirement -> Branch -> Implementation -> Testing -> Commit -> Review
-> Pull Request -> Validation -> Merge -> Release
```

Heron should never blindly push experimental code into production.

## 39. Repository Intelligence

The GitHub Agent understands repository structure, branches, tags, releases, issues, pull requests, commits, project files, dependencies and version history — and detects changes that may affect fragments and skills.

## 40. Automatic Update Intelligence

Update monitoring at multiple levels: Heron version · Agent version · Skill version · Fragment version · MCP version · Revit compatibility · .NET compatibility · Dependencies · Knowledge sources.

A change in one component triggers dependency analysis.

## 41. Dependency Graph

```text
Skill -> Fragment -> Revit API -> .NET -> Package
```

If .NET changes:

```text
.NET Update -> Dependency Graph -> Affected Fragments
-> Affected Skills -> Affected Agents -> Testing
```

This enables controlled updates.

## 42. Build Pipeline

```text
Requirement -> Architecture -> Code -> Static Analysis -> Build
-> Unit Tests -> Integration Tests -> Revit Tests -> Regression Tests
-> QA -> Package -> Release
```

**No component should enter production by simply compiling successfully.**

## 43. Revit Testing

Verify: document state · transactions · element validity · geometry · parameters · worksets · views · selection · performance · version compatibility.

Where possible, tests should run against supported Revit versions.

## 44. Revit Version Matrix

| Component | R2020 | R2021 | R2022 | R2023 | R2024 | R2025+ |
|---|---|---|---|---|---|---|
| Fragment A | ✓ | ✓ | ✓ | ✓ | ✓ | Test |
| Fragment B | ✓ | ✓ | Adapter | Adapter | ✓ | ✓ |

The actual status must come from testing. **Never assume compatibility.**

## 45. .NET Compatibility Agent

Determines target framework, API availability, package compatibility, compiler requirements, project configuration. Coordinates with the Revit API Agent.

## 46. Revit API Agent

Determines correct API, namespace, method, version changes, deprecated APIs, alternative API, transaction requirements. **It should not independently rewrite architecture.**

## 47. Code Review Agent

Verifies architecture, readability, performance, API usage, error handling, transaction handling, compatibility, security, maintainability.

## 48. Revit QA Agent

After code-level testing, a **separate** Revit QA Agent verifies actual behaviour inside Revit.

> **Code QA ≠ Revit QA.** Both are required.

## 49. Background Worker System

Update Check · Indexing · Re-indexing · Knowledge Cleanup · Duplicate Detection · Fragment Scoring · Agent Health · Backup · Documentation · Repository Monitoring · Compatibility Analysis.

These run independently from interactive BIM tasks.

## 50. Task Priority

```text
P0 - User interaction
P1 - Required execution
P2 - Required validation
P3 - Maintenance
P4 - Learning
P5 - Optimization
P6 - Cleanup
```

If the user starts a heavy Revit operation, background indexing should reduce or pause its workload.

## 51. Event-Driven Architecture

Internal events: `FragmentCreated` · `FragmentUpdated` · `FragmentApproved` · `SkillCreated` · `AgentCreated` · `AgentFailed` · `AgentRetired` · `PluginInstalled` · `PluginUpdated` · `MCPConnected` · `MCPDisconnected` · `RevitOpened` · `RevitClosed` · `RepositoryChanged`

Events allow systems to react without tight coupling.

## 52. Health Monitoring

Monitored: Revit Connection · MCP · Agents · RAG · Vector DB · Database · Storage · GitHub · AI Provider · Background Workers.

Each reports: `HEALTHY` · `WARNING` · `DEGRADED` · `FAILED`

## 53. Self-Healing Architecture

| Condition | Flow |
|---|---|
| MCP disconnected | Health Agent → Detect → Recovery Agent → Reconnect → Health Check |
| Vector index corrupted | Index Health → Detect → Rebuild → Validate |
| Dependency missing | Dependency Agent → Detect → Install/Repair → Build → Validate |

## 54. Safety Architecture

**Autonomy must be proportional to risk.**

| Risk | Autonomy | Examples |
|---|---|---|
| **Low** | Automatic | search, classification, indexing, analysis, backup, health check |
| **Medium** | Controlled automatic | file rename, file movement, code generation, fragment updates |
| **High** | Require approval | deleting production knowledge, modifying important repositories, publishing releases, destructive Revit operations, major architecture changes |

## 55. Human Approval Gates

Heron should not ask the user for every tiny operation. Approval occurs only at **meaningful boundaries**.

Useful:

> *"I found a new implementation that changes the existing production fragment for Revit 2020–2027. Approve replacement?"*

Never require approval for:

> *"I am searching the fragment database."*

## 56. Emergency Stop

A global emergency mechanism:

```text
STOP ALL AGENTS
STOP BACKGROUND TASKS
DISABLE AUTO-UPDATE
DISABLE AUTO-EXECUTION
```

Especially important as autonomy increases.

## 57. Audit Architecture

```text
User Request -> Workflow ID -> Agents -> Knowledge Used -> Fragment Used
-> Code Generated -> Tests -> Result -> Changes
```

This allows complete debugging.

## 58. Observability

Metrics: task latency · agent latency · retrieval latency · model latency · token usage · success rate · failure rate · retry count · rollback count.

Allows the platform to identify bottlenecks.

## 59. Cost Optimization

Avoid wasting AI calls.

- If a **proven fragment exists** → **DO NOT GENERATE NEW CODE.**
- If an **exact answer exists in trusted knowledge** → do not perform expensive reasoning unnecessarily.

Reduces token cost, execution time and complexity.

## 60. Caching

Caches: capability lookup · fragment lookup · API knowledge · version compatibility · dependency analysis · project context.

Cache invalidation must happen when relevant knowledge changes.

## 61. Knowledge Garbage Collection

Over time: duplicates, obsolete fragments, unused skills, outdated documentation, temporary data.

A Knowledge Cleanup Agent identifies them. It should **not blindly delete**.

```text
Unused -> Candidate for Archive -> Review -> Archive
```

## 62. Knowledge Quality Score

Accuracy · Relevance · Freshness · Usage · Success Rate · Source Trust · Compatibility → combined quality score, used by RAG during ranking.

## 63. Skill Quality Score

Skill Success · User Corrections · Execution Time · Failure Rate · Compatibility.

Poor-performing skills automatically enter review.

## 64. Continuous Learning

```text
User Request -> Execution -> Success -> Pattern Detected -> Existing Skill Improved
```

Learning is separated into: `Observed` → `Candidate` → `Validated` → `Approved` → `Production`.

This prevents accidental learning from becoming production truth.

## 65. Conversation Intelligence

The conversation layer understands whether the user is asking, commanding, explaining, debugging, planning, coding, modeling or reviewing.

| Utterance | Requires |
|---|---|
| *"Why did this fail?"* | investigation |
| *"Select all ducts."* | execution |
| *"Make a tool for this."* | development workflow |

## 66. Role-Aware Communication

| Persona | Response |
|---|---|
| BIM Modeler | *"The ducts are selected."* |
| Developer | *"The Revit selection command completed successfully using the approved selection service."* |

The underlying system remains the same. Only communication changes.

## 67. Multi-Platform Architecture

```text
Heron Core
 +-- Revit Adapter
 +-- AutoCAD Adapter
 +-- Navisworks Adapter
 +-- IFC Adapter
 +-- Civil 3D Adapter
 +-- Rhino Adapter
 +-- Blender Adapter
```

The core Brain and Agent OS should not depend directly on Revit.

## 68. Platform Adapter Principle

Each external application exposes standardized capabilities: Element Selection · Document Access · Object Creation · Object Modification · Transaction · Export · Validation.

Each platform implements its own adapter. This keeps Heron modular.

## 69. Plugin Ecosystem

A plugin can provide agents, skills, fragments, MCP tools, UI, integrations.

Plugins declare: Name · Version · Dependencies · Capabilities · Compatibility · Permissions · Author · Security Status.

## 70. Heron SDK

| SDK | Purpose |
|---|---|
| Agent SDK | Create Heron agents |
| Skill SDK | Create reusable skills |
| Fragment SDK | Create knowledge/action fragments |
| Platform SDK | Create integrations |
| MCP SDK | Create MCP tools |

## 71. Developer Mode

Normal users should not see internal complexity. Developers can activate **Developer Mode** and inspect: agents, workflows, fragments, skills, RAG, logs, API, versions, dependencies, GitHub, build pipeline.

Two experiences: `USER MODE` and `DEVELOPER MODE`.

## 72. Admin Mode

Enterprise administrators get: user management · permissions · company knowledge · standards · agent policies · update policies · package policies · security · audit logs.

## 73. Multi-User Architecture

```text
Company
 +-- Admin
 +-- BIM Coordinators
 +-- BIM Modelers
 +-- Developers
 +-- Managers
```

Each user has personal memory, permissions and personal skills, while sharing approved company knowledge, skills and fragments.

## 74. Data Isolation

Clearly separate: User Data · Project Data · Company Data · Community Data · System Data.

**No accidental cross-contamination.**

## 75. Backup Strategy

Cover: configuration · agents · skills · fragments · metadata · database · vector indexes · memory · project knowledge. Backups should be versioned.

## 76. Disaster Recovery

```text
Detect -> Repair -> Restore Configuration -> Restore Knowledge
-> Rebuild Vector Index -> Health Check
```

The system should reconstruct indexes from authoritative knowledge storage.

## 77. Source of Truth Principle

```text
Canonical Knowledge
        |
        v
    Vector DB
```

**The Vector DB is an index, not the ultimate source of truth.** If it is destroyed, Heron rebuilds it.

## 78. Configuration Management

Version-controlled configuration: supported Revit versions · enabled agents · enabled skills · AI provider · model routing · security policies · update policies · company standards.

Configuration changes should be auditable.

## 79. Migration Architecture

```text
Heron v1 -> Migration Agent -> Schema Migration -> Knowledge Migration
-> Agent Migration -> Skill Migration -> Validation -> Heron v2
```

Old data should not simply be discarded.

## 80. Final End-to-End Example

User: *"Create a Revit tool that selects all ducts, checks their size against our project standard, and highlights the ones that fail."*

Heron internally:

```text
 1. Intent Agent                14. Code Review
 2. BIM Persona Agent           15. Build
 3. Project Context Agent       16. Unit Test
 4. Capability Discovery        17. Revit Integration Test
 5. RAG Librarian               18. Standards Validation
 6. Fragment Search             19. Regression Test
 7. Skill Search                20. QA
 8. Project Standard Search     21. Package
 9. Revit Version Detection     22. Deploy
10. Revit API Analysis          23. Execute
11. .NET Compatibility Analysis 24. Validate Result
12. Architecture Planning       25. Record Success
13. Code Generation             26. Update Knowledge
                                27. Update Performance Metrics
```

The user sees:

> *"The tool is ready. I found 14 ducts that do not meet the project size requirement."*

## 81. The Ultimate Heron AI Loop

```text
OBSERVE -> UNDERSTAND -> PLAN -> RETRIEVE -> EXECUTE
-> VALIDATE -> LEARN -> EVALUATE -> IMPROVE -> REPEAT
```

## 82. Final Architecture Vision

> An autonomous engineering organization implemented as software.

| Component | Represents |
|---|---|
| User | The engineering decision-maker |
| Orchestrator | Management |
| Agents | Specialized employees |
| Skills | Professional capabilities |
| Fragments | Institutional knowledge |
| RAG | The company library |
| Memory | Organizational experience |
| Vector DB | The searchable knowledge index |
| MCP | Communication infrastructure |
| Revit Add-in | The engineering execution environment |
| GitHub | The controlled engineering repository |
| QA agents | Quality control |
| Agent Creator | HR / recruitment |
| Training Agent | Employee onboarding |
| Evolution Engine | Continuous improvement |
| Background Worker System | Employees working continuously behind the scenes |

## 83. Most Important Architectural Rule

Heron AI must **never** become a giant AI that tries to do everything itself. Instead:

```text
ONE USER REQUEST
        |
ONE ORCHESTRATED WORKFLOW
        |
ONLY REQUIRED AGENTS
        |
ONLY REQUIRED KNOWLEDGE
        |
ONLY REQUIRED TOOLS
        |
VALIDATED RESULT
```

This keeps the platform modular, scalable, testable, maintainable, fast, understandable, secure and self-improving.

## 84. Final Target

```text
USER      "Select all ducts."
   |
HERON AI  Understands the request.
          Finds the correct capability.
          Finds the proven fragment.
          Checks Revit environment.
          Executes.
          Validates.
          Learns from the result.
   |
USER      "Done."
```

The user should never need to think about the internal machinery unless they deliberately enter Developer/Admin Mode. That is the fundamental difference between Heron AI and a normal AI assistant.

---

## HERON AI — NORTH STAR

> Make complex engineering automation feel as simple as talking to an experienced BIM colleague,
> while maintaining an enterprise-grade autonomous engineering organization behind the conversation.
