<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 34 — Project Perfection and Continuous Upgrade Plan

> **Next-work note.** This document defines how Heron AI should study proven external repositories, extract only useful engineering principles, and turn them into Heron-native improvements without copying another project’s architecture or code.
>
> **Primary rule:** external repositories are research references, not source-code suppliers.

---

## 1. Goal

Use strong open-source projects as a continuous engineering reference so Heron becomes easier to improve, safer to change, easier to debug, and harder to regress.

The target is not to make Heron look like another agent platform. The target is to improve the existing Heron system while preserving its own product identity:

- BIM-modeller-first workflow;
- Revit 2020–2027 compatibility;
- deterministic execution before model reasoning where possible;
- C# and Python kept inside Heron’s existing architecture;
- fragments, capabilities, agents, trust levels and quality gates remain Heron concepts;
- no unnecessary framework migration;
- no copying code merely because another project solved a similar problem.

Every adopted idea must be rewritten for Heron’s constraints and checked against the current repository before implementation.

---

## 2. Repositories to study

### 2.1 OpenDesign

Reference:
`https://github.com/nexu-io/open-design`

Useful ideas to study and adapt:

- clear separation between runtime, UI, registries, persistence and adapters;
- one business-logic authority instead of duplicate logic across interfaces;
- reusable skills/templates/design-system style registries;
- project refresh/refinement workflow for existing codebases;
- artifact-first workflow: brief → plan → generate → preview → critique → improve → deliver;
- strict repository ownership rules for where behaviour belongs;
- runtime adapters behind a stable contract;
- self-critique/lint gates before an artifact is considered finished;
- live project files as the source of truth instead of stale copied exports;
- package-level provenance and licence tracking.

Heron adaptation direction:

- keep Heron’s current Brain / MCP / Revit / Platform boundaries;
- improve registry contracts rather than introduce another registry system;
- use the “single authority + adapters” principle when multiple agents or interfaces call the same Heron capability;
- use artifact validation and critique gates for plans, fragments, generated code, fixes and upgrades;
- use provenance tracking whenever an external concept materially influenced a Heron feature.

Do **not** import its design-product architecture into Heron. Only take patterns that improve maintainability, quality, packaging or agent workflow.

### 2.2 Awesome LLM Apps

Reference:
`https://github.com/Shubhamsaboo/awesome-llm-apps`

Useful areas to study:

- self-improving skill loops;
- executor / analyst / mutator separation;
- generate evaluation cases before improvement;
- establish a baseline score before changing anything;
- make one targeted improvement at a time;
- keep a change only when evaluation improves;
- revert unsuccessful mutations;
- scope-creep detection;
- dependency checking;
- commit-history reasoning;
- multi-agent review patterns;
- always-on monitoring patterns;
- trust-gated workflows.

Heron adaptation direction:

- never allow uncontrolled self-rewriting of Heron core architecture;
- use the loop only inside bounded, reviewable targets such as prompts, agent instructions, fragment descriptions, tests, validation rules and low-risk workflow logic;
- require baseline → proposed change → test → compare → keep/revert;
- store a changelog and evidence for every accepted improvement;
- prefer one surgical change over large AI-generated rewrites;
- create Revit-specific evaluation cases instead of reusing general AI-app examples.

The most important principle is **measured improvement, not autonomous rewriting**.

### 2.3 OpenHands Agent Canvas / OpenHands

Original reference:
`https://github.com/OpenHands/agent-canvas`

The original Agent Canvas repository is archived and points to the current implementation in:
`https://github.com/OpenHands/OpenHands`

Useful ideas to study and adapt:

- explicit frontend/backend/system boundaries;
- agent UI does not directly execute agent work;
- one or more agent servers behind a stable API;
- switchable backends without changing the user workflow;
- local, remote and hosted runtime separation;
- automation as a separate service rather than hidden inside UI code;
- structured runtime/service discovery instead of guessing ports or capabilities;
- mock mode for UI and workflow testing;
- quality gates covering lint, tests, build, packaging and end-to-end checks;
- clear security warnings around workspace access;
- controlled sandbox/workspace scope.

Heron adaptation direction:

- keep the Revit bridge as the controlled execution boundary;
- keep UI, agent reasoning, automation and Revit execution responsibilities separated;
- expose runtime/service capability information explicitly rather than relying on assumptions;
- strengthen mock/test modes so more behaviour can be verified without opening Revit;
- add packaging and release checks to the normal quality gate;
- keep the final authority for Revit changes inside Heron’s trust/permission model.

---

## 3. Mandatory research rule during implementation

Research does **not** finish when this planning document is written.

For every feature, fix, refactor or upgrade that uses an idea from these repositories, the implementing agent must do this again at implementation time:

1. Open the relevant external repository.
2. Check the current README and the actual implementation files related to the idea.
3. Check whether the repository has changed since this planning note was written.
4. Confirm licence/provenance requirements before using anything beyond a general idea.
5. Compare the external mechanism with the current Heron implementation.
6. Write the Heron-native design before coding.
7. Implement only what fits Heron’s architecture and product goals.
8. Re-check the external source if any behaviour, boundary, lifecycle or failure case is unclear.
9. Never fill uncertainty with a guess when the reference can be inspected.
10. Record why the final Heron implementation is different where that difference matters.

**This rule is mandatory.** The external repositories are living references. Do not rely only on this document, old notes, memory or a previous AI summary.

---

## 4. No-copy rule

The following is prohibited:

- copying a large implementation and renaming it;
- importing another project’s folder structure because it looks clean;
- adopting another project’s terminology when Heron already has a stable equivalent;
- adding dependencies only to reproduce another repository’s architecture;
- pasting external prompts, skills, tests or workflows into Heron unchanged;
- copying code without checking licence obligations;
- changing Heron’s architecture simply to match a reference project.

Allowed process:

**study → understand → extract principle → map to Heron → design → implement → test → document**.

If code reuse is ever genuinely necessary, treat it as a separate legal/provenance decision, not as normal implementation.

---

## 5. The Heron Project Perfection Loop

This loop should be reusable for feature development, bug fixing, refactoring, repository cleanup and upgrades.

### Phase A — Understand

Before changing code:

- define the exact problem;
- identify affected Heron layers;
- inspect current code, tests, docs, decisions and open questions;
- inspect related external references where useful;
- define what “better” means using measurable acceptance criteria.

Output: a small implementation plan and a baseline.

### Phase B — Baseline

Capture the current state before editing:

- current tests;
- current warnings/errors;
- current fragment/agent registry state;
- current supported Revit versions;
- current behaviour for positive and negative cases;
- current performance where relevant.

A change without a baseline cannot prove improvement.

### Phase C — Diagnose

For a bug or weak behaviour:

- reproduce it first;
- identify the root cause, not only the symptom;
- identify blast radius;
- check whether the problem is local or architectural;
- inspect similar solved patterns in the research repositories if useful.

Do not begin with a rewrite.

### Phase D — Design

Prepare the smallest Heron-native solution that:

- preserves existing contracts where possible;
- avoids new dependencies unless justified;
- works across the required Revit versions;
- respects trust and transaction boundaries;
- keeps deterministic work deterministic;
- does not move responsibility into the wrong layer.

### Phase E — Implement Surgically

Prefer one bounded change at a time.

After each meaningful change:

- compile/lint;
- run focused tests;
- re-run the failing case;
- run the relevant negative case;
- stop and diagnose if the result gets worse.

Do not stack ten speculative fixes and test only at the end.

### Phase F — Critique

Before declaring completion, run an independent critique pass:

- did scope grow beyond the task?
- did the change duplicate existing functionality?
- did any responsibility move to the wrong layer?
- did Revit-version compatibility change?
- did any fallback become silent?
- did tests only prove the happy path?
- did a new dependency or licence obligation appear?
- is documentation still accurate?

### Phase G — Verify

Completion requires fresh evidence:

- target test passes;
- negative test passes;
- regression tests pass;
- build passes;
- metadata/registry checks pass;
- packaging checks pass when affected;
- supported Revit-version checks pass where applicable;
- real Revit verification is performed for behaviour that cannot be proven outside Revit.

### Phase H — Record

For accepted changes record:

- problem;
- root cause;
- design decision;
- files changed;
- tests/evidence;
- external concept used, if any;
- licence/provenance requirement, if any;
- remaining limitation.

---

## 6. Bug-Fix Perfection Workflow

When fixing a mistake:

1. reproduce the mistake;
2. create or identify a failing test/evidence case;
3. locate root cause and blast radius;
4. inspect external references only if they help explain the pattern;
5. design the smallest fix;
6. implement one focused change;
7. prove the original failure is fixed;
8. prove nearby behaviour was not broken;
9. add a regression test;
10. update docs/decisions only if behaviour or architecture changed.

A bug is not considered fixed because the code “looks correct”.

---

## 7. Upgrade Workflow

When upgrading an existing Heron subsystem:

1. inventory the current subsystem;
2. identify pain points using evidence, not preference;
3. compare current design with proven patterns from the research repositories;
4. list what Heron already does correctly;
5. list only real gaps;
6. rank gaps by value, risk and implementation cost;
7. improve one boundary at a time;
8. keep compatibility adapters where needed;
9. migrate tests before removing old behaviour;
10. remove old code only after the replacement is proven.

Do not rewrite a working subsystem merely because another repository has a newer-looking architecture.

---

## 8. Self-Improvement Boundary

Heron may become better from its own failures, but the improvement loop must be controlled.

Allowed automatic or semi-automatic improvement targets:

- prompts;
- agent instructions;
- fragment descriptions;
- routing hints;
- test cases;
- evaluation criteria;
- documentation;
- low-risk deterministic rules with strong tests.

Human review required before accepting changes to:

- core architecture;
- Revit write operations;
- transaction handling;
- trust/permission rules;
- installers/updaters;
- security boundaries;
- persistent storage contracts;
- public APIs;
- fragment execution code affecting production models.

Use **baseline → diagnose → one mutation → evaluate → keep/revert**.

Never use “self-improving” as permission for broad autonomous rewriting.

---

## 9. Project-Perfection Quality Gates

A future Heron quality gate should gradually include:

- repository metadata validation;
- architecture-boundary checks;
- fragment schema validation;
- agent registry validation;
- deterministic unit tests;
- negative-case tests;
- regression tests;
- Revit-version compatibility checks;
- static analysis / warnings;
- dependency audit;
- scope-creep check;
- packaging/install/update checks;
- documentation-link validation;
- licence/provenance checks;
- change-impact / blast-radius report;
- final independent review.

The final gate should answer one simple question:

> **Is this change proven better than the baseline, safe for Heron’s architecture, and safe for the supported Revit workflow?**

If evidence is missing, status remains DRAFT.

---

## 10. Agent Roles for Complex Improvements

For larger upgrades, use lightweight role separation instead of one agent designing, coding and approving its own work.

Recommended logical roles:

- **Planner** — defines scope, constraints and acceptance criteria;
- **Researcher** — studies Heron plus relevant external references;
- **Implementer** — makes the smallest approved change;
- **Tester** — proves positive, negative and regression behaviour;
- **Critic** — looks for scope creep, hidden assumptions and architectural drift;
- **Reviewer** — accepts/rejects the result based on evidence.

These are roles, not necessarily six permanently running agents. Reuse the same orchestration machinery wherever possible.

---

## 11. Attribution and Naming Rule

External repository names should **not** appear unnecessarily inside production code, class names, method names, fragment IDs or user-facing Heron terminology.

If a material implementation is derived from an external project and attribution is required or useful:

- record it in README/NOTICE/provenance documentation;
- preserve required licence notices;
- describe the concept factually;
- keep Heron code and naming Heron-native.

General engineering ideas do not need decorative attribution inside code.

---

## 12. Implementation Order

### Stage 1 — Build the reusable improvement framework

First create the shared mechanism that later work can reuse:

- baseline record;
- evaluation cases;
- failure diagnosis;
- targeted change proposal;
- keep/revert decision;
- changelog/evidence record;
- critique checklist.

Do this once as a reusable Heron mechanism instead of rebuilding it separately for every agent or subsystem.

### Stage 2 — Apply it to low-risk targets

Start with:

- prompts;
- documentation;
- agent instructions;
- fragment descriptions;
- validation rules;
- tests.

Prove the loop before allowing it near production Revit execution.

### Stage 3 — Apply it to bug fixing

Use the same framework for:

- reproduced bug;
- failing evidence;
- diagnosis;
- one targeted fix;
- regression protection.

### Stage 4 — Apply it to subsystem upgrades

Use it for registry, routing, memory, context, agent orchestration, installer/update and Revit capability improvements.

### Stage 5 — Continuous maintenance

Periodically re-check the reference repositories for genuinely useful new patterns.

Do not chase every new feature. Only adopt a concept when it solves a measured Heron problem.

---

## 13. Definition of Done

This initiative is successful when Heron can repeatedly improve a feature or fix a defect using the same disciplined workflow:

**understand → baseline → research → diagnose → design → implement → critique → verify → record**

and when every external idea is:

- independently understood;
- adapted to Heron;
- tested against Heron’s requirements;
- traceable when necessary;
- rejected when it does not fit.

The objective is not “more AI”.

The objective is a Heron codebase that becomes progressively **cleaner, safer, easier to maintain, easier to test, easier to upgrade and harder to break**.
