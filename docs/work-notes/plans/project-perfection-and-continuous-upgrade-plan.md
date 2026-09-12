<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Project Perfection and Continuous Upgrade Plan

> **Type:** Operational work note. **Not specification.** This is an active implementation plan and must retire after its durable decisions are moved into the permanent Heron documentation.
>
> **Primary rule:** external repositories are research references, not source-code suppliers.

## Goal

Use strong open-source projects as engineering references so Heron becomes easier to improve, safer to change, easier to debug, and harder to regress, while preserving Heron's BIM/Revit identity and existing architecture.

Every adopted idea must be rewritten for Heron's constraints and checked against the current repository before implementation.

## Repositories to study

### OpenDesign
Reference: `https://github.com/nexu-io/open-design`

Study and adapt only useful mechanisms such as clear ownership boundaries, one business-logic authority behind multiple interfaces, runtime adapters, managed artifacts, critique/lint gates, registries with strict contracts, and provenance discipline.

Do not import its design-product architecture, framework stack, or terminology into Heron.

### Awesome LLM Apps
Reference: `https://github.com/Shubhamsaboo/awesome-llm-apps`

Study bounded self-improvement, eval-driven changes, scope-creep detection, dependency checking, commit-history reasoning, trust-gated workflows, and multi-agent review patterns.

The important pattern is: **baseline → diagnose → one targeted mutation → evaluate → keep or revert**.

Do not treat self-improvement as permission for broad autonomous rewriting.

### OpenHands / Agent Canvas
Historical reference: `https://github.com/OpenHands/agent-canvas`
Current reference: `https://github.com/OpenHands/OpenHands`

Agent Canvas is archived and moved to the current OpenHands repository. Study the current source for execution/UI separation, runtime discovery, mock/test modes, packaging gates, automation boundaries, and workspace security.

## Mandatory research rule during implementation

Research does **not** finish when this plan is written.

For every feature, fix, refactor or upgrade that uses an idea from these repositories, the implementing agent must:

1. Open the relevant external repository again.
2. Check the current README and the actual implementation files related to the idea.
3. Confirm whether the repository moved, archived, changed behaviour, or changed licence boundaries.
4. Compare the external mechanism with the current Heron implementation.
5. Write a Heron-native design before coding.
6. Implement only what fits Heron's architecture and BIM/Revit goals.
7. Re-check the source whenever behaviour, lifecycle, failure mode, or boundary is unclear.
8. Never guess when the reference can be inspected.
9. Record why the Heron implementation differs where that difference matters.
10. Check exact file/folder licensing before any actual material reuse.

## No-copy rule

Prohibited:

- copying a large implementation and renaming it;
- importing another project's folder structure because it looks clean;
- adopting foreign terminology where Heron already has a stable equivalent;
- adding dependencies only to reproduce another architecture;
- pasting prompts, skills, tests, workflows, or code unchanged;
- changing Heron's architecture simply to match a reference project.

Allowed process:

**study → understand → extract principle → map to Heron → design → implement → test → document**.

## Heron Project Perfection Loop

### A. Understand
Define the exact problem, affected Heron layers, relevant code/tests/docs/decisions, and measurable acceptance criteria.

### B. Baseline
Capture current tests, warnings/errors, registry state, supported Revit versions, positive/negative behaviour, and performance where relevant.

A change without a baseline cannot prove improvement.

### C. Diagnose
Reproduce the bug or weakness, find root cause, identify blast radius, and check whether it is local or architectural.

Do not begin with a rewrite.

### D. Design
Prepare the smallest Heron-native solution that preserves contracts, avoids unnecessary dependencies, respects Revit 2020–2027, trust/transaction boundaries, deterministic paths, and layer ownership.

### E. Implement surgically
Prefer one bounded change at a time. After each meaningful change, compile/lint, run focused tests, rerun the failing case, and run the relevant negative case.

### F. Critique
Before completion, check scope creep, duplicate functionality, wrong-layer fixes, compatibility drift, silent fallbacks, missing negative cases, dependency/licence changes, and stale documentation.

### G. Verify
Require fresh evidence: target test, negative test, regression tests, build, metadata/registry checks, packaging when affected, version compatibility, and real Revit proof where offline testing cannot be truthful.

### H. Record
Record problem, root cause, design decision, files changed, evidence, external concept used, provenance/licence notes if applicable, and remaining limitations.

## Bug-fix workflow

1. Reproduce the defect.
2. Create or identify failing evidence.
3. Find root cause and blast radius.
4. Research external patterns only if useful.
5. Design the smallest fix.
6. Implement one focused change.
7. Prove the original failure is fixed.
8. Prove nearby behaviour is not broken.
9. Add regression protection.
10. Update permanent docs/decisions only when behaviour or architecture actually changed.

A bug is not fixed because code merely looks correct.

## Upgrade workflow

1. Inventory the current subsystem.
2. Identify pain points using evidence.
3. Compare against useful external patterns.
4. List what Heron already does correctly.
5. List only real gaps.
6. Rank gaps by value, risk, and implementation cost.
7. Improve one boundary at a time.
8. Keep compatibility adapters where required.
9. Migrate tests before removing old behaviour.
10. Remove old code only after the replacement is proven.

Do not rewrite a working subsystem merely because another repository looks newer.

## Self-improvement boundary

Allowed automatic or semi-automatic targets include prompts, agent instructions, fragment descriptions, routing hints, test cases, evaluation criteria, documentation, and low-risk deterministic rules with strong tests.

Human review is required before accepting changes to core architecture, Revit write operations, transaction handling, trust/permission rules, installers/updaters, security boundaries, persistent storage contracts, public APIs, and production fragment execution code.

Use **baseline → diagnose → one mutation → evaluate → keep/revert**.

## Project-perfection quality gates

Gradually include:

- metadata validation;
- architecture-boundary checks;
- fragment schema validation;
- agent registry validation;
- deterministic unit tests;
- negative-case tests;
- regression tests;
- Revit-version compatibility checks;
- static analysis/warnings;
- dependency audit;
- scope-creep check;
- packaging/install/update checks;
- documentation-link validation;
- licence/provenance checks;
- change-impact/blast-radius report;
- independent final review.

Final question:

> **Is this change proven better than the baseline, safe for Heron's architecture, and safe for the supported Revit workflow?**

If evidence is missing, status remains DRAFT.

## Logical roles for complex improvements

Use role separation only when risk justifies it:

- Planner — scope, constraints, acceptance criteria;
- Researcher — Heron + relevant external references;
- Implementer — smallest approved change;
- Tester — positive, negative, regression evidence;
- Critic — scope creep, assumptions, architectural drift;
- Reviewer — accept/reject from evidence.

These are roles, not necessarily six permanent agents.

## Attribution and naming

External repository names should not appear unnecessarily inside production code, class names, method names, fragment IDs, or user-facing Heron terminology.

When attribution is required or useful, keep it in README/NOTICE/provenance documentation and preserve required licence notices.

## Implementation order

### Stage 1 — Reusable improvement framework
Create a shared baseline record, evaluation cases, failure diagnosis, targeted change proposal, keep/revert decision, evidence/changelog record, and critique checklist.

### Stage 2 — Low-risk targets
Apply first to prompts, documentation, agent instructions, fragment descriptions, validation rules, and tests.

### Stage 3 — Bug fixing
Use the same framework for reproduced defects, diagnosis, targeted fixes, and regression protection.

### Stage 4 — Subsystem upgrades
Apply to registry, routing, memory, context, orchestration, installer/update, and Revit capability improvements.

### Stage 5 — Continuous maintenance
Periodically re-check external references only when they solve a measured Heron problem. Do not chase features for their own sake.

## Closure condition

This work note can retire when Heron has a reusable, evidence-backed improvement workflow and the durable rules/decisions have been moved into the permanent specification/decision documents.

Target operating sequence:

**understand → baseline → research → diagnose → design → implement → critique → verify → record**
