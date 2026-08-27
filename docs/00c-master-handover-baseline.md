# Heron AI — Master Project Handover Note

*Complete Product, Architecture, Agent, Knowledge, Development & Operations Specification*

> **Status:** Source-of-truth record, **part 3 of 3** — the consolidated **handover baseline**,
> as provided by the owner on 2026-08-27.
>
> | Part | Covers |
> |---|---|
> | [Part 1](00-master-specification.md) | The platform and its organisation (76 §) |
> | [Part 2](00b-master-specification-agent-os.md) | The Agent Operating System and self-evolution (84 §) |
> | **Part 3 — this document** | **The consolidated baseline.** Supersedes nothing; unifies both. |
>
> **This part is authoritative on the Golden Rules** — it replaces Part 1's ten rules with fifteen.
> See [14 — Golden Rules](14-golden-rules.md).
>
> Verbatim record. Do not edit to "fix" it — record changes as decisions in [DECISIONS.md](DECISIONS.md).

| Field | Value |
|---|---|
| Project Name | Heron AI |
| Project Type | Autonomous Modular AI Engineering Platform |
| Primary Domain | BIM / Revit / MEP Automation |
| Architecture | Multi-Agent + MCP + RAG + Vector Database + Skills + Fragments + Memory |
| Primary User | BIM Modeler / BIM Coordinator |
| Secondary Users | BIM Developer / Automation Developer / Administrator |
| Primary Goal | Allow users to perform complex BIM engineering work through natural language while Heron AI manages the technical complexity in the background |

---

## 1. Purpose of this Handover

This document defines the complete intended architecture and operating model of Heron AI, so that another AI, architect, programmer or development team can understand:

What Heron AI is · why it is being built · how the system should work · how users interact with it · how Revit is connected · how MCP works · how RAG works · how fragments work · how skills work · how memory works · how agents work · how agents are created · how agents communicate · how knowledge is imported · how existing knowledge is detected · how knowledge evolves · how code is generated · how code is tested · how Revit compatibility is maintained · how GitHub is integrated · how updates work · how background processes work · how the system learns · how the system protects production knowledge · how the platform can grow into an enterprise-level system.

**This document is the handover baseline.**

> The implementation team should not assume that every architectural idea is already implemented.
> **Items described as future capabilities must be explicitly marked** and implemented through
> controlled development.

## 2. Product Vision

Heron AI is intended to become an **AI-powered engineering operating platform**.

It is not simply a chatbot, an MCP server, a Revit plugin, a coding assistant, a RAG system, a collection of prompts, or a collection of scripts. It combines all of these into one coordinated platform.

The user communicates naturally:

> "Select all ducts." · "Move them 200 mm up." · "Add end caps." · "Check these against our standard." · "Create a dimension." · "Fix this."

Heron AI determines what technical work is required.

## 3. Core Design Philosophy

### 3.1 User Simplicity

The user should not need programming knowledge, nor to understand C#, .NET, the Revit API, MCP, vector databases, embeddings, RAG, agent orchestration, Git, package management or deployment.

### 3.2 Internal Complexity

```text
USER -> NATURAL LANGUAGE -> HERON ORCHESTRATOR -> SPECIALIZED AGENTS
-> KNOWLEDGE -> TOOLS -> VALIDATION -> EXECUTION -> RESULT
```

## 4. Primary Platform Components

| Component | Responsibility |
|---|---|
| **4.1 Heron Revit** | Direct Revit interaction — add-in, ribbon, commands, Revit API, document access, selection, element operations, transactions, views, parameters, families, worksets, links, model analysis, version handling, deployment |
| **4.2 Heron MCP** | Communication between the AI system and Revit — MCP server, tools, tool registry, connection, health monitoring, authentication, capability discovery, error handling, recovery |
| **4.3 Heron Brain** | Intelligence and knowledge — RAG, fragments, skills, memory, vector database, embeddings, knowledge ranking, validation, evolution |
| **4.4 Heron Platform** | Infrastructure — installer, update system, package manager, agent registry, agent creation, workspace, configuration, GitHub, logging, security, backup, recovery, monitoring |

## 5. High-Level Architecture

```text
                         HERON AI
                            |
                     USER INTERFACE
                            |
                   CONVERSATION AGENT
                            |
                       ORCHESTRATOR
                            |
        +-------------------+-------------------+
        |                   |                   |
   TASK SYSTEM         KNOWLEDGE SYSTEM    PLATFORM SYSTEM
        |                   |                   |
     Agents              RAG                 Updates
     Workflows           Fragments           Packages
     Tools               Skills              GitHub
     Validation          Memory              Workspace
        |                   |                   |
        +-------------------+-------------------+
                            |
                       MCP LAYER
                            |
                     HERON REVIT ADD-IN
                            |
                          REVIT
```

## 6. The Orchestrator

The central workflow coordinator. It does **not** perform every task itself. It determines:

What does the user want? · Which capability is required? · Which agents are required? · Which knowledge is required? · Which skill should be used? · Which fragment should be used? · What validations are required? · What execution path should be followed? · What should happen if something fails? · What should be learned afterward?

## 7. Dynamic Agent Selection

Heron must not activate every agent for every request.

*"Select all ducts."*

```text
Intent Agent -> Capability Registry -> RAG Librarian
-> Revit Selection Agent -> Validation -> Execution
```

A complex coding request can activate a much larger workflow.

## 8. Agent Organization

Recommended departments:

Executive / Orchestration · Conversation · Revit · MCP · Knowledge / RAG · Fragment · Skill · Memory · Development · Testing / QA · GitHub · Workspace · Import / Migration · Naming / Taxonomy · Standards · Security · Monitoring · Update / Deployment · Agent Management · Documentation

## 9. Agent Responsibility Rule

> **One agent should have one primary responsibility.**

Do not create a giant `RevitEverythingAgent`. Instead: Revit Selection Agent · Revit Parameter Agent · Revit Transaction Agent · Revit API Agent · Revit Version Agent · Revit Testing Agent.

This makes the platform easier to test, replace, improve, debug and scale.

## 10. Agent Registry

Required metadata:

```text
Agent ID / Name / Department / Description / Responsibility / Capabilities
Inputs / Outputs / Dependencies / Tools / Permissions
Version / Status / Trust Score / Performance / Created Date / Updated Date
```

Possible status:

```text
PROPOSED / DESIGN / DEVELOPMENT / TRAINING / TESTING / SHADOW
APPROVED / PRODUCTION / DEPRECATED / ARCHIVED
```

## 11. Agent Creation System

When Heron discovers a required capability does not exist:

```text
Capability Gap -> Gap Analysis Agent -> Existing Agent Search
-> Existing Skill Search -> Existing Fragment Search -> Determine Need
-> Agent Architect -> Agent Builder -> Training -> Testing
-> Approval -> Registry -> Deployment
```

## 12. Agent HR Concept

Management determines: *"We need an agent that checks Revit plugin installation."*

Agent HR creates the requirement · Agent Architect defines the job · Agent Builder creates it · Training Agent teaches it Heron standards · QA tests it · Registry registers it · Deployment activates it.

## 13. Agent Training

A new agent must learn: Heron architecture · coding standards · naming standards · security policies · relevant domain knowledge · approved fragments · approved skills · relevant project standards.

A new agent should **not** become production-trusted immediately.

## 14. Agent Shadow Mode

New agents initially operate in **Shadow Mode**. They can analyze, suggest and compare results — but must **not** automatically modify production systems.

After sufficient successful testing, the agent can be promoted.

## 15. Agent Performance

Measured: success rate · failure rate · execution time · user correction · retry rate · rollback rate · compatibility failures · security violations. These create a performance score.

## 16. Agent Retirement

```text
ACTIVE -> REVIEW -> REPLACEMENT -> MIGRATION -> DEPRECATED -> ARCHIVED
```

Historical information should remain available.

## 17. Conversation System

Translates between the user and the technical system. It determines: user intent · user role · technical level · required response style · whether execution is requested · whether explanation is requested.

## 18. BIM Modeler Mode

*"Select all ducts."*

Heron should **not** respond: *"FilteredElementCollector with OST_DuctCurves..."*

Instead: *"All ducts are selected."*

Technical details remain internal unless requested.

## 19. Developer Mode

For developers, Heron can expose C#, Revit API, .NET, architecture, dependencies, build details, testing and logs.

The same system therefore supports both BIM Modeler and Developer workflows.

## 20. RAG System

Not simply vector search. The workflow:

```text
User Request -> Intent -> Domain Detection -> Knowledge Scope
-> Keyword Search -> Semantic Search -> Vector Search -> Metadata Filtering
-> Ranking -> Trust Evaluation -> Conflict Detection -> Context Assembly
```

## 21. RAG Librarian Agent

Determines **where Heron should look**: fragment library · skills · company knowledge · project knowledge · personal memory · community knowledge · documentation.

## 22. Vector Database

The Vector Database is an **index**. It is **not** the ultimate source of truth. Canonical knowledge exists in controlled storage, so the Vector DB can be rebuilt if necessary.

## 23. Automatic Vector DB Maintenance

Background: index new fragments · update changed fragments · remove deleted indexes · rebuild stale indexes · update embeddings · detect duplicate vectors · validate metadata.

## 24. Knowledge Scopes

```text
GLOBAL / COMPANY / PROJECT / USER / COMMUNITY / EXPERIMENTAL / TEMPORARY
```

This prevents accidental knowledge contamination.

## 25. Fragment System

A fragment contains:

```text
Fragment ID / Name / Purpose / Capability / Implementation
Inputs / Outputs / Dependencies / Revit Versions / .NET Versions
Related Skills / Validation Rules / Success History / Failure History
Trust Level / Version / Lifecycle
```

## 26. Fragment Registry

The authoritative index of fragments. **The filename is not the identity — the Fragment ID is.**

Therefore `old_name.cs` → `new_name.cs` does **not** create a new fragment if the semantic identity remains the same.

## 27. Duplicate Fragment Detection

```text
New Fragment -> Identity Extraction -> Semantic Search
-> Code Comparison -> Metadata Comparison -> Existing Fragment Found?
```

| Result | Action |
|---|---|
| Exact match | Do not create a duplicate |
| Similar | Recommend merge/update |
| Different | Create new fragment |
| Conflict | Send to resolution workflow |

## 28. Fragment Split Agent

A large fragment may contain several responsibilities — Select, Filter, Analyze, Modify, Validate — which could become independent reusable fragments.

## 29. Fragment Merge Agent

If multiple fragments perform essentially the same operation: merge · replace · keep separate · deprecate duplicate.

## 30. Fragment Evolution Agent

Determines: `KEEP` · `UPDATE` · `EXTEND` · `SPLIT` · `MERGE` · `BRANCH` · `DEPRECATE` · `ARCHIVE`

This is the main self-growing knowledge mechanism.

## 31. Revit Version Evolution

A fragment works in Revit 2020–2022. Revit 2023 introduces an API change. Heron must first determine: **can the same implementation remain compatible?**

If yes — keep one implementation. If no — use an adapter or version-specific implementation.

```text
DUCT TOOL
 +-- Common Logic
 +-- Revit 2020-2021
 +-- Revit 2022-2024
 +-- Revit 2025-2027
```

The semantic capability remains the same.

## 32. Compatibility Agents

| Agent | Determines |
|---|---|
| Revit Version Agent | Revit version |
| Revit API Agent | API requirements |
| .NET Agent | Framework requirements |
| Dependency Agent | Package requirements |
| **Compatibility Agent** | **Combines these results** |

## 33. Development Pipeline

```text
Requirement -> Planning -> Architecture -> RAG
-> Existing Fragment Search -> Existing Skill Search -> Implementation
-> Code Review -> Build -> Unit Test -> Integration Test -> Revit Test
-> Regression Test -> QA -> Approval -> Deployment
```

## 34. Code Generation Agent

Must understand the existing Heron architecture and must not create isolated code. Before generating, it retrieves: existing services · existing fragments · architecture rules · naming rules · supported versions · dependency rules · project structure.

## 35. Code Review Agent

Checks: architecture · maintainability · API usage · performance · error handling · transaction safety · version compatibility · security · duplication.

## 36. Revit Test Agent

**Code compilation is not enough.** A separate Revit Test Agent verifies actual behaviour inside Revit: document state · element state · transactions · parameters · geometry · selection · warnings · performance.

## 37. Regression Agent

```text
Changed Fragment -> Identify Supported Versions -> Run Previous Tests
-> Run New Tests -> Compare Results -> Detect Breaking Changes
```

A change must not accidentally break older Revit versions.

## 38. Knowledge Import System

*"Import this folder."* Heron scans it automatically and identifies: code · fragments · skills · documentation · configuration · dependencies · metadata · duplicates.

## 39. Import Pipeline

```text
SCAN -> UNDERSTAND -> CLASSIFY -> EXTRACT -> COMPARE -> RENAME
-> TRANSFORM -> VALIDATE -> ARCHITECTURE MATCH -> SAVE -> INDEX
```

**Nothing should enter production merely because it was copied.**

## 40. Folder Architecture Agent

The Workspace Architecture Agent controls folders · hierarchy · file placement · naming · metadata · relationships · migration · cleanup. It is the architectural authority for the workspace.

## 41. Naming System

The Naming Agent determines the correct name from: domain · capability · purpose · platform · version · component type. The system avoids random filenames.

## 42. File Validation

After renaming or moving a file, the **Reference Update Agent** verifies: imports · references · metadata · registry · documentation · relationships.

**No broken references should be introduced.**

## 43. Skill System

Skills are reusable higher-level capabilities combining multiple fragments.

```text
SMART DUCT DIMENSIONING
 +-- Duct Discovery
 +-- Geometry Analysis
 +-- Dimension Calculation
 +-- Dimension Creation
 +-- Validation
```

## 44. Skill Lifecycle

```text
DRAFT -> TESTING -> VALIDATED -> PROVEN -> PRODUCTION -> DEPRECATED
```

## 45. Memory System

| Scope | Contents |
|---|---|
| Global Memory | Heron-wide knowledge |
| Company Memory | Approved company standards |
| Project Memory | Project-specific knowledge |
| User Memory | User preferences and successful patterns |
| Temporary Memory | Current task context |
| Experimental Memory | Unproven knowledge |

## 46. Personal Learning

Heron learns from successful user workflows and can detect repeated patterns. **Personal learning remains personal unless explicitly promoted.**

## 47. Community Knowledge

```text
User Knowledge -> Candidate -> Validation -> Security
-> Testing -> Approval -> Community
```

**Do not automatically publish personal knowledge.**

## 48. GitHub System

Manages repositories · branches · commits · issues · pull requests · tags · releases · versioning · changes. The GitHub Agent understands repository architecture.

## 49. Update System

Automatically detects: Heron updates · agent updates · skill updates · fragment updates · MCP updates · dependencies · compatibility changes.

The user receives: *"A new update is available. Update now?"*

## 50. Update Safety

```text
Current Version -> Backup -> Dependency Analysis -> Compatibility Check
-> Update -> Build -> Test -> Health Check
```

If the update fails: **Rollback**.

## 51. Background Agents

update monitoring · RAG indexing · vector maintenance · duplicate detection · fragment evaluation · skill evaluation · agent health · plugin health · MCP health · backups · documentation synchronization · GitHub monitoring · dependency monitoring · cleanup

## 52. Background Priority

```text
P0 - User Task            P4 - Learning
P1 - Required Validation  P5 - Optimization
P2 - System Health        P6 - Cleanup
P3 - Knowledge Maintenance
```

Background processes should slow down when Revit requires resources.

## 53. Event System

`RevitOpened` · `RevitClosed` · `MCPConnected` · `MCPDisconnected` · `FragmentCreated` · `FragmentUpdated` · `FragmentApproved` · `SkillCreated` · `AgentCreated` · `AgentFailed` · `PluginInstalled` · `PluginUpdated` · `RepositoryChanged` · `UpdateAvailable`

These allow subsystems to react without tight coupling.

## 54. Failure Management

```text
Agent Failure -> Failure Analysis -> Identify Cause -> Select Repair Agent
-> Repair -> Retest -> Validate -> Continue
```

**Do not repeatedly execute the same failed operation without analysis.**

## 55. Self-Healing

| Failure | Response |
|---|---|
| MCP failure | Reconnect |
| Missing dependency | Repair/install |
| Stale vector index | Rebuild |
| Invalid fragment | Quarantine |
| Broken configuration | Restore |
| Failed code | Send to repair workflow |

## 56. Security

High-risk operations: deleting files · deleting knowledge · modifying production code · GitHub push · release publishing · destructive Revit operations · external package installation. These require controlled permissions.

## 57. Permission Levels

```text
READ / ANALYZE / SUGGEST / EXECUTE / MODIFY / PUBLISH / ADMIN
```

> **Agents should only receive the permissions they require.**

## 58. Audit System

Record: User · Request · Workflow · Agents · Knowledge Used · Fragment Used · Tools Used · Changes · Result · Errors · Approval · Version · Timestamp.

This makes Heron traceable.

## 59. Health System

Monitored: Revit · MCP · Agents · RAG · Vector DB · Storage · Database · AI Provider · GitHub · Background Workers.

States: `HEALTHY` · `WARNING` · `DEGRADED` · `FAILED`

## 60. Source of Truth

> **Vector DB is not the source of truth.** Canonical knowledge is stored separately.

If the Vector DB is lost:

```text
Canonical Knowledge -> Rebuild Embeddings -> Rebuild Index -> Restore RAG
```

## 61. Model Routing

A Model Router determines the appropriate model based on task complexity · coding requirement · reasoning requirement · speed · cost · reliability.

Simple task → efficient model. Complex architecture → stronger reasoning. Code generation → coding-capable model. Critical decision → strong model + validation.

## 62. Token and Cost Optimization

If a proven fragment already exists — **reuse it**, do not regenerate code unnecessarily.
If a trusted answer exists — **retrieve it**, do not perform expensive reasoning unnecessarily.

## 63. Cache System

Potential caches: fragment search · capability search · Revit API information · compatibility · project context · dependencies.

Cache invalidation must happen when underlying knowledge changes.

## 64. Knowledge Cleanup

Identify duplicates · obsolete fragments · unused skills · stale knowledge · temporary files — but **do not blindly delete**.

```text
Unused -> Review Candidate -> Archive
```

## 65. Multi-User Future

```text
Company
 +-- Administrator  +-- BIM Coordinator  +-- BIM Modeler
 +-- Developer      +-- Manager
```

Shared: company standards, approved skills, approved fragments.
Private: personal memory, personal workflows, personal skills.

## 66. Multi-Project Isolation

```text
Company
 +-- Project A: Memory / Skills / Fragments / Standards
 +-- Project B: Memory / Skills / Fragments / Standards
```

Project A information must not accidentally contaminate Project B.

## 67. Plugin Ecosystem

Plugins can provide agents · skills · fragments · MCP tools · integrations · BIM platforms.

Every plugin declares: Name · Version · Author · Capabilities · Dependencies · Compatibility · Permissions · Security Status.

## 68. Heron SDK

Agent SDK · Skill SDK · Fragment SDK · MCP SDK · Platform SDK.

## 69. Developer Mode

Exposes: agent registry · workflow · fragments · skills · logs · RAG · dependencies · GitHub · build pipeline · testing. Normal users should not need to see this.

## 70. Admin Mode

Controls: users · permissions · standards · company knowledge · packages · updates · agents · security · audit logs.

## 71. Disaster Recovery

Backup: configuration · agents · skills · fragments · metadata · memory · database · canonical knowledge.

```text
Restore -> Validate -> Rebuild Index -> Health Check
```

## 72. Real Example — Simple User Task

*"Select all ducts."*

```text
Conversation Agent -> Intent Agent -> Capability Registry -> RAG Librarian
-> Fragment Matcher -> Revit Version Agent -> Revit Selection Agent
-> MCP -> Revit -> Validation -> Success -> Memory/Telemetry
```

User only sees: *"All ducts selected."*

## 73. Real Example — New Tool

*"Create a tool to automatically dimension ducts."*

```text
Requirement -> Existing Capability Search -> Existing Fragment Search
-> Architecture -> Revit API -> .NET -> Code Generation -> Code Review
-> Build -> Unit Test -> Revit Test -> Regression -> QA
-> Skill Creation -> Fragment Creation -> Documentation -> Deployment
```

## 74. Real Example — Import Existing Folder

*"Import this folder into Heron."*

```text
Scan -> Classification -> Fragment Detection -> Skill Detection
-> Duplicate Detection -> Compatibility -> Naming -> Folder Architecture
-> Migration -> Validation -> Index -> Save
```

## 75. Real Example — Existing Fragment Update

Existing: Revit 2020–2022. New requirement: Revit 2023 support.

```text
Existing Implementation -> Revit API Changes -> .NET Changes
-> Can Existing Implementation Continue?
```

If yes — extend compatibility. If no — create adapter / version-specific implementation. Then run regression tests against all supported versions.

## 76. Real Example — Failure

```text
Execution -> Failure -> Failure Analysis Agent -> Revit API Agent
-> .NET Agent -> Repair Agent -> Build -> Re-test -> Revit Test -> QA
```

The user should receive a concise result rather than the entire internal chain.

## 77. Self-Growing Loop

```text
USER WORK -> OBSERVATION -> EXECUTION -> RESULT -> SUCCESS / FAILURE
-> ANALYSIS -> KNOWLEDGE UPDATE -> FRAGMENT EVALUATION
-> SKILL EVALUATION -> AGENT EVALUATION -> IMPROVEMENT
```

**Every promotion should pass through trust and validation controls.**

## 78. Heron AI Golden Rules

> **These fifteen rules supersede the ten in [Part 1 §72](00-master-specification.md).**
> See [14 — Golden Rules](14-golden-rules.md) for the working version with cross-references.

| # | Rule |
|---|---|
| 1 | User focuses on BIM. Heron handles technical complexity. |
| 2 | One agent should have one primary responsibility. |
| 3 | Reuse proven knowledge before creating new knowledge. |
| 4 | Never break a working Revit version unnecessarily. |
| 5 | Personal knowledge must remain separate from company knowledge. |
| 6 | Experimental knowledge must remain separate from production knowledge. |
| 7 | One agent creates; another agent validates. |
| 8 | Background work must not interfere with user work. |
| 9 | High-risk actions require controlled approval. |
| 10 | Every important object must have identity, version and lifecycle. |
| 11 | Vector DB is an index, not the canonical source of truth. |
| 12 | No automatic external publishing of private knowledge. |
| 13 | No uncontrolled self-modification of production architecture. |
| 14 | Every important autonomous operation must be auditable. |
| 15 | The platform must be modular enough that individual agents, skills and fragments can be replaced without redesigning the entire system. |

## 79. Final Heron AI Organization

```text
HERON AI
|
+-- Executive / Orchestrator
+-- Conversation
+-- Revit Engineering
|     +-- Revit API / Selection / Elements / Parameters
|     +-- Transactions / Views / Families / Versions
+-- MCP
+-- Knowledge
|     +-- RAG / Vector DB / Library / Retrieval
+-- Fragments
|     +-- Registry / Split / Merge / Evolution / Validation
+-- Skills
+-- Memory
|     +-- Global / Company / Project / User / Experimental
+-- Development
|     +-- Architecture / Coding / .NET / Revit API
+-- QA
|     +-- Code / Build / Revit / Regression
+-- GitHub
+-- Import / Migration
+-- Workspace Architecture
+-- Naming / Taxonomy
+-- Standards
+-- Security
+-- Monitoring
+-- Updates
+-- Agent HR
+-- Documentation
```

## 80. North Star

> A BIM professional should be able to work with Heron AI as if they are working with a highly
> experienced engineering organization behind them.

The user gives the requirement. Heron determines the process. Specialized agents perform the work. RAG provides knowledge. Fragments provide reusable implementation knowledge. Skills provide capabilities. MCP connects the AI to Revit. Revit executes the operation. QA validates the result. Memory records experience. Background agents maintain the system. The Evolution Engine improves the system.

And the entire organization remains **modular, controlled, auditable and scalable**.

---

## Heron AI — Final Operating Model

```text
                HUMAN
                  |
                  v
          NATURAL LANGUAGE
                  |
                  v
             ORCHESTRATOR
                  |
          +-------+-------+
          v               v
      KNOWLEDGE         AGENTS
          |               |
       RAG/Memory      Specialists
          |               |
          +-------+-------+
                  v
               SKILLS
                  |
                  v
             FRAGMENTS
                  |
                  v
              MCP / TOOLS
                  |
                  v
               REVIT
                  |
                  v
              VALIDATION
                  |
                  v
                RESULT
                  |
                  v
              LEARNING
                  |
                  v
              EVOLUTION
                  |
                  +--------> BACKGROUND
```
