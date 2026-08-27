# Heron AI — Additional Master Requirements & Recommendations

> **Status:** Source-of-truth record, **part 4 of 4**, as provided by the owner on 2026-08-27.
>
> | Part | Covers |
> |---|---|
> | [Part 1](00-master-specification.md) | The platform and its organisation (76 §) |
> | [Part 2](00b-master-specification-agent-os.md) | The Agent Operating System and self-evolution (84 §) |
> | [Part 3](00c-master-handover-baseline.md) | The consolidated baseline (80 §) — authoritative on the Golden Rules |
> | **Part 4 — this document** | **Additional requirements.** Adds the Kernel, Workflow Engine, Constitution, and 30 mandatory components. |
>
> Verbatim record. Do not edit to "fix" it — record changes as decisions in [DECISIONS.md](DECISIONS.md).

---

## 1. Heron Core Kernel

A central **Heron Kernel** is strongly recommended — the foundation underneath everything.

```text
Heron Kernel
├── Configuration
├── Identity
├── Permissions
├── Event Bus
├── Agent Registry
├── Skill Registry
├── Fragment Registry
├── Tool Registry
├── Memory Manager
├── Workflow Manager
├── State Manager
├── Logging
└── Security
```

Every module communicates **through the Kernel** instead of directly depending on every other module. This makes Heron much more modular.

## 2. Capability Registry

Heron should know: *"What can I currently do?"*

```text
Capability:      Select Ducts
Provider:        Revit Selection Agent
Implementation:  Fragment X
Skill:           MEP Selection
Supported:       Revit 2020-2027
Status:          PROVEN
```

Before creating anything new, Heron checks this registry. This prevents unnecessary duplication.

## 3. Workflow Engine

The Orchestrator should not manually control every step. A dedicated **Workflow Engine** manages: sequential tasks · parallel tasks · dependencies · retries · failures · approvals · timeouts · rollback · checkpoints.

```text
Requirement -> Search -> Plan -> Generate -> Review -> Build -> Test -> Deploy
```

## 4. Agent Communication Protocol

Every agent receives:

```text
Task / Context / Input / Required Output / Constraints / Knowledge / Permissions / Deadline
```

And returns:

```text
Status / Result / Evidence / Errors / Confidence / Changes / Next Action
```

This prevents agents from becoming chaotic.

## 5. Agent Confidence System

Every important agent result carries a confidence level: `HIGH` · `MEDIUM` · `LOW` · `UNKNOWN`.

**Confidence should not replace validation.** For critical operations:

```text
AI confidence + Technical validation + Testing
```

## 6. Evidence System

Heron should know *why* it made a decision.

> *"This fragment was selected because it supports Revit 2020–2027 and has 98 successful executions."*

For important decisions, retain: source · fragment · skill · agent · version · validation result. This makes the system explainable.

## 7. Immutable Provenance

Every fragment, skill and agent has history.

```text
Fragment F-001
  v1.0 -> Created
  v1.1 -> Revit 2021 fix
  v1.2 -> Revit 2023 support
  v1.3 -> Performance improvement
```

**Never silently overwrite important knowledge.**

## 8. Version Compatibility Matrix

A dedicated compatibility database.

| Component | Revit | .NET | Status |
|---|---|---|---|
| Fragment A | 2020–2022 | 4.7.2 | Proven |
| Fragment A | 2023–2025 | New API | Proven |
| Fragment A | 2026–2027 | New API | Testing |

Extremely important as Heron grows.

## 9. Adapter Architecture

For Revit API changes, don't duplicate everything.

```text
Common Business Logic -> Compatibility Layer -> Revit Version Adapter -> Revit API
```

This preserves one logical capability across many Revit versions.

## 10. Sandbox System

**One of the strongest recommendations.** Any new code, fragment, skill, agent or plugin runs first inside a sandbox.

```text
Generated -> Sandbox -> Test -> Review -> Approved -> Production
```

## 11. Simulation / Dry Run

Before a dangerous operation, Heron should be able to say:

> *"I found 126 ducts. This operation will modify 126 elements."*

Then execution can happen. Very useful for BIM.

## 12. Transaction Safety Agent

A dedicated Revit agent responsible for: transaction handling · transaction closure · rollback · failure handling · document state.

**Separate from general Revit API logic.**

## 13. Revit Context Agent

Heron should automatically understand the current Revit context: active document · Revit version · active view · selected elements · linked models · worksets · view type · phase · design option · active user.

This removes unnecessary questions.

## 14. User Intent Memory

Heron should distinguish *"what the user normally likes"* from *"what the user asked this time."*

**Temporary instructions must not automatically become permanent memory.**

## 15. Memory Promotion System

```text
Observation -> Candidate Memory -> Repeated Evidence -> Validation -> Promotion
```

This prevents bad habits from becoming system knowledge.

## 16. Knowledge Conflict Agent

If Company Standard A and Project Standard B disagree, Heron detects the conflict instead of blindly choosing. **Priority should be configurable.**

## 17. Standards Agent

A dedicated standards subsystem for ISO · BIM standards · company standards · project standards · client standards · Revit standards · naming standards.

**Standards checking should be on-demand** unless background monitoring is explicitly required.

## 18. BIM QA Agent

Separate from coding QA. Checks: naming · parameters · categories · families · levels · worksets · views · modelling rules · MEP connectivity · coordination requirements.

This becomes a major Heron capability.

## 19. Clash / Coordination Agent

Eventually handles clash analysis · clearance · system coordination · linked models · Navisworks-related workflows · coordination reports.

## 20. Task Memory

Heron remembers current task state. If something fails, the task is **paused** and it knows where it stopped. Then *"Continue."* resumes from the correct checkpoint.

## 21. Resume / Checkpoint System

```text
Step 1 ✓  Step 2 ✓  Step 3 ✓  Step 4 FAILED
```

After fixing Step 4: **continue from Step 4**, not restart everything.

## 22. Queue Manager

```text
Priority 1  User request
Priority 2  Required validation
Priority 3  System maintenance
Priority 4  RAG indexing
Priority 5  Learning
```

This prevents background agents from consuming resources needed by Revit.

## 23. Resource Manager

Monitors CPU · RAM · disk · AI usage · vector DB load · Revit responsiveness.

**If Revit is busy, background jobs reduce or pause automatically.**

## 24. AI Model Abstraction Layer

Do not hard-code Heron around one AI provider.

```text
Heron AI Interface -> Model Router -> Provider Adapter -> Model
```

This allows different models later without rebuilding Heron.

## 25. Local / Cloud Model Routing

Simple task → local model. Normal task → standard model. Complex coding → powerful coding model. Critical architecture → strongest reasoning model.

**The user should not need to know which model was selected.**

## 26. Prompt / Instruction Registry

Do not scatter prompts throughout the code. A controlled registry for: system instructions · agent instructions · skill instructions · coding rules · BIM language rules.

Then they can be **versioned and tested**.

## 27. Evaluation System

An automated evaluation framework. Every important agent has test cases.

```text
Agent:     Duct Selection Agent
Test:      Select all ducts
Expected:  All visible ducts selected
Result:    PASS
```

The equivalent of an employee performance review.

## 28. Golden Test Library

A permanent collection of known-good cases. Every major update runs against these tests.

**Extremely important for preventing regressions.**

## 29. Security Boundary Between AI and Revit

AI should never receive unlimited direct control.

```text
AI -> Permission Layer -> Tool Validation -> MCP -> Revit
```

**The AI requests an operation; the platform decides whether that operation is allowed.**

## 30. Secret Management

API keys, GitHub tokens and credentials must never be stored inside fragments · skills · prompts · source code · logs. A secure credential system is required.

## 31. Offline / Degraded Mode

Heron should know what to do when internet, AI provider, GitHub, MCP or the vector DB is unavailable.

**Clearly identify which capabilities are unavailable** instead of failing unpredictably.

## 32. Installation Wizard

```text
Install Heron -> Detect Environment -> Detect Revit Versions -> Install Components
-> Build Required Add-in -> Deploy -> Start Revit -> Connect MCP -> Health Check -> Ready
```

The user should not need coding knowledge.

## 33. First-Run Onboarding Agent

```text
Welcome to Heron AI.
Checking your environment...
Revit detected ✓   Heron Revit ✓   MCP ✓
Knowledge Base ✓   Vector DB ✓   Agents ✓   Skills ✓
```

Then guide the user once. After that, normal usage should be almost invisible.

## 34. Self-Diagnostics

A `Diagnose Heron` command checks Revit · MCP · Agents · Skills · Fragments · RAG · Vector DB · Storage · Dependencies · Permissions · GitHub · Updates — then provides a clean report.

## 35. Emergency Recovery Mode

**Heron Safe Mode** can disable recently installed agents, plugins, skills and fragments, and return to the last known-good configuration.

## 36. Feature Flags

New functionality deployable behind flags.

```text
SmartDimensioning = OFF
ExperimentalRAG   = ON
NewAgentSystem    = TEST
```

## 37. Marketplace / Extension System

A **Heron Marketplace** for Skills · Fragments · Agents · MCP tools · BIM packages · company templates.

**Imported community components should never automatically become trusted production components.**

## 38. Trust Levels

```text
UNKNOWN -> EXPERIMENTAL -> TESTED -> VERIFIED -> PROVEN -> OFFICIAL
```

The system should use trust level when selecting knowledge.

## 39. Community Contribution System

```text
Submitted -> Security Scan -> Duplicate Check -> Architecture Check
-> Compatibility -> Testing -> Human/Policy Approval -> Community Candidate -> Verified
```

## 40. Documentation Agent

Every production capability automatically maintains documentation.

```text
Code -> Documentation Agent -> README -> Metadata -> Changelog -> Compatibility
```

**Documentation should not become outdated.**

## 41. Changelog Agent

Every meaningful change creates a structured changelog entry.

```text
Heron 1.4.0
Added:       Smart Duct Dimensioning
Improved:    Revit 2023 API compatibility
Fixed:       Transaction handling
Deprecated:  Old Dimension Fragment
```

## 42. Dependency Intelligence

Continuously understand .NET · Revit API · NuGet packages · external libraries · MCP dependencies · Python dependencies · system requirements.

**Detect incompatible combinations before deployment.**

## 43. Supply-Chain Security

For external code and plugins: verify source · check package identity · check version · scan dependencies · detect suspicious modifications · maintain hashes/signatures where practical.

Important once Heron can install external components automatically.

## 44. Human Approval Boundary

Heron should be autonomous, but not blindly autonomous. Three modes:

| Mode | For |
|---|---|
| **Automatic** | Safe operation |
| **Ask** | Important operation |
| **Require Approval** | High-risk operation |

This gives the "company employee" model without allowing uncontrolled changes.

## 45. Heron Organizational Model

```text
                    HERON CEO
                       |
                 ORCHESTRATOR
                       |
        +--------------+--------------+
        |              |              |
       HR          OPERATIONS      KNOWLEDGE
        |              |              |
   Agent Factory    Workflow        RAG
   Agent Training   QA              Memory
   Agent Review     Deployment      Fragments
                    Security        Skills
```

## 46. Most Important New Addition: Heron Constitution

Create one permanent document: **`HERON_CONSTITUTION.md`** — the rules agents must never violate.

- Do not destroy production knowledge without authorization.
- Do not overwrite proven fragments blindly.
- Do not break supported Revit versions.
- Do not expose private memory to other users.
- Do not publish user knowledge automatically.
- Do not bypass security.
- Do not modify core architecture without approval.
- Do not execute destructive Revit operations without permission.
- Prefer reuse over regeneration.
- Validate before promotion.
- Preserve backward compatibility wherever technically possible.

**Every agent should receive the relevant constitutional rules.**

## 47. Four Knowledge Levels

```text
LEVEL 1 — RAW        Imported but untrusted
LEVEL 2 — TESTED     Technically tested
LEVEL 3 — VERIFIED   Reviewed and validated
LEVEL 4 — PROVEN     Repeated successful production usage
```

Only proven knowledge should become the strongest default recommendation.

## 48. Final Architecture

```text
                         HERON AI
                            |
                       HERON KERNEL
                            |
      +---------------------+---------------------+
      |                     |                     |
 ORCHESTRATION          KNOWLEDGE             PLATFORM
      |                     |                     |
 Workflow              RAG                     Installer
 Agents                Vector DB               Updates
 Agent HR              Fragments               GitHub
 Agent Factory         Skills                  Plugins
 Model Router          Memory                  Security
      |                     |                     |
      +---------------------+---------------------+
                            |
                     VALIDATION LAYER
                            |
          +-----------------+-----------------+
          |                 |                 |
       Code QA           BIM QA          Security QA
          |                 |                 |
          +-----------------+-----------------+
                            |
                       MCP / TOOLS
                            |
                     HERON REVIT ADD-IN
                            |
                          REVIT
                            |
                    RESULT / TELEMETRY
                            |
                       LEARNING
                            |
                       EVOLUTION
```

---

## Strongest recommendations — treated as mandatory, not optional

Heron Kernel · Capability Registry · Workflow Engine · Agent Registry · Agent Factory / HR · RAG Librarian · Fragment Registry + lifecycle · Skill Registry + lifecycle · Separated Memory · Vector DB management · Revit Compatibility Layer · Sandbox · Automated Testing · Golden Test Library · Version/Provenance system · Security + Permission Layer · Audit Trail · Rollback / Recovery · Background Queue · Self-Diagnostics · Automatic Installer · Automatic Update Manager · GitHub Agent · Standards Agent · BIM QA Agent · Conversation/Persona Agent · Model Router · Knowledge Trust System · Agent Performance System · Heron Constitution

## Build order

> **Don't start by building 100+ agents.**
>
> Build the **Kernel, Registry, Orchestrator, Workflow Engine, RAG, Fragment/Skill system,
> MCP/Revit layer, and QA foundation** first.
>
> Then Heron can create and add specialized agents safely as the platform grows.
