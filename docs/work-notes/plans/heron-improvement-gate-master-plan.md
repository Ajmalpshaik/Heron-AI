<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Heron Improvement Gate — Master Implementation Plan

> **Type:** Operational work note. **Not specification.**
>
> **Purpose:** turn the useful ideas studied from OpenDesign, Awesome LLM Apps, and OpenHands into one Heron-native system for safer features, bug fixes, refactors, upgrades, cleanup, agent improvement, and releases.
>
> **Core rule:** study external repositories, extract mechanisms, rebuild them for Heron, prove improvement, and keep only what works. Never copy-paste architecture or code into Heron.

Related notes:
- [`project-perfection-and-continuous-upgrade-plan.md`](project-perfection-and-continuous-upgrade-plan.md)
- [`external-repo-adoption-plan.md`](external-repo-adoption-plan.md)

---

# 1. Final objective

Create one reusable **Heron Improvement Gate** that every meaningful code or system change can pass through.

The gate must make a future Heron task answer these questions before completion:

1. What exactly are we changing?
2. What is the current baseline?
3. What files/capabilities can this affect?
4. Is the proposed design consistent with Heron's architecture?
5. Is the change smaller than or equal to the intended scope?
6. Does it work for required Revit versions?
7. Are positive, negative, and regression cases proven?
8. Did the change create new dependency, security, packaging, or licence risk?
9. Does it need real Revit proof?
10. Is the final result measurably better than the baseline?

A change is not complete until the gate returns a clear final status.

---

# 2. What each studied repository contributes

## 2.1 OpenDesign contribution

### Take
- one authority behind multiple interfaces;
- strict layer ownership;
- runtime/capability discovery;
- managed artifact lifecycle;
- pre-completion critique/lint;
- path-level provenance discipline.

### Add to Heron
- Capability Authority rule;
- Layer Ownership check;
- Runtime Capability Snapshot;
- Artifact Evidence Record;
- Pre-Completion Critic Gate.

### Improvement expected
- less duplicate logic;
- fewer wrong-layer fixes;
- easier debugging;
- safer refactoring;
- better traceability;
- more reliable agent planning.

## 2.2 Awesome LLM Apps contribution

### Take
- baseline-driven improvement;
- one-mutation keep/revert loop;
- scope-creep checking;
- eval/trigger cases;
- commit archaeology;
- dependency health review;
- selective multi-agent critique.

### Add to Heron
- Improvement Loop;
- Change Intent + Scope Report;
- evaluation cases beside important capabilities;
- Legacy Change Safety Check;
- Dependency Gate.

### Improvement expected
- fewer broad AI rewrites;
- measurable prompt/agent improvements;
- safer cleanup;
- better regression protection;
- less unnecessary dependency growth.

## 2.3 OpenHands contribution

### Take
- UI separated from execution;
- explicit environment/service context;
- mock/test mode;
- packaging as QA;
- workspace/security boundary.

### Add to Heron
- strict UI/Execution boundary;
- Heron Environment Context Contract;
- Heron Offline Test Harness;
- Release Verification Gate;
- stronger trust checks for unattended or write-capable actions.

### Improvement expected
- less manual Revit testing;
- safer automation;
- fewer environment assumptions;
- better release reliability;
- clearer security boundaries.

---

# 3. Heron Improvement Gate architecture

The Improvement Gate should not become a separate giant platform.

It should be a thin orchestration layer around existing Heron systems.

```text
Task / Fix / Upgrade
        ↓
Change Intent
        ↓
Baseline Capture
        ↓
Impact / Blast Radius
        ↓
Environment Snapshot
        ↓
Optional External Research Check
        ↓
Heron-Native Plan
        ↓
Implementation
        ↓
Focused Tests
        ↓
Negative + Regression Tests
        ↓
Scope Check
        ↓
Architecture Ownership Check
        ↓
Compatibility Check
        ↓
Trust / Security Check
        ↓
Dependency / Provenance Check
        ↓
Packaging Check if affected
        ↓
Independent Critique
        ↓
PASS / REVISE / SPLIT / REVERT / NEEDS_REAL_REVIT_PROOF
        ↓
Evidence Record
```

---

# 4. Gate input contract

Every meaningful task should start with a small machine-readable record containing:

- `intent` — one sentence describing exactly what should change;
- `task_type` — feature / bug / refactor / upgrade / cleanup / release / agent-improvement;
- `target_area` — subsystem or capability;
- `risk_level` — low / medium / high;
- `acceptance_criteria`;
- `baseline_commit` or state marker;
- `requires_revit` — yes/no/unknown;
- `supported_revit_versions` where applicable;
- `external_references` — optional;
- `human_approval_required` — yes/no.

If the task cannot define intent clearly, implementation should not start.

---

# 5. Gate outputs

The final evidence package should contain:

- intent;
- baseline summary;
- changed files;
- impacted capabilities;
- tests run;
- negative tests run;
- regressions checked;
- Revit compatibility status;
- scope-creep findings;
- architecture ownership findings;
- dependency/provenance findings;
- packaging findings where relevant;
- real Revit proof status;
- critic findings;
- final decision;
- remaining risk.

Final statuses:

- `PASS`
- `REVISE`
- `SPLIT`
- `REVERT`
- `BLOCKED`
- `NEEDS_REAL_REVIT_PROOF`

No vague "looks good" completion status.

---

# 6. Implementation phases

## Phase 0 — Baseline and repository audit

### Goal
Understand what Heron already has so the new system does not duplicate existing tools.

### Work
- inspect existing test tools;
- inspect metadata checks;
- inspect fragment validation;
- inspect agent registry validation;
- inspect current quality gates;
- inspect current packaging/update checks;
- inspect existing change-impact or graph tooling;
- inspect current work-note and evidence conventions;
- map all reusable pieces.

### External research rule
During this phase, re-open the three external repositories and inspect the current relevant files again.

### Deliverable
`improvement-gate-baseline.md`

### Exit condition
A clear table exists: **already have / partial / missing / rejected**.

---

## Phase 1 — Change Intent + Scope Gate

### Goal
Stop scope creep before it becomes normal.

### Build
A tool/workflow that compares:

- intended task;
- actual Git diff;
- file paths touched;
- dependency/config/build changes;
- public contract changes;
- documentation/test changes.

### Classification
Each changed file should be marked:

- required;
- supporting;
- test/evidence;
- documentation;
- dependency/build/config;
- suspicious/unrelated.

### Rules
- suspicious changes do not automatically fail;
- they require justification;
- unrelated changes should be split;
- a bug fix that modifies unrelated architecture returns `SPLIT` or `REVISE`.

### Tests
- clean small fix;
- legitimate test update;
- intentional dependency addition;
- unrelated file change;
- broad formatting noise;
- config/CI side effect.

### Exit condition
Given a Git diff and one-line intent, Heron produces a reliable scope report.

---

## Phase 2 — Baseline + Evidence Record

### Goal
Make every meaningful improvement prove before/after state.

### Build
A baseline recorder that captures only relevant evidence, for example:

- test result;
- warning/error count;
- current behaviour;
- fragment/agent state;
- relevant version compatibility;
- performance measurement where needed.

### Rules
- no giant generic dump;
- collect only evidence related to the task;
- evidence should be reproducible where possible;
- derived counts should come from commands/tools, not hand-typed numbers.

### Exit condition
Every task can attach a compact reproducible baseline and final result.

---

## Phase 3 — Runtime Capability Snapshot

### Goal
Give planners and agents environment truth before action.

### Build
A structured snapshot with:

- Revit version;
- document identity/state;
- active view/context where relevant;
- bridge availability;
- writable/read-only state;
- available capabilities;
- linked-data availability where relevant;
- trust mode;
- optional provider/tool availability.

### Important rule
Represent difference between:

- unsupported;
- unavailable;
- not detected;
- blocked by trust;
- requires Revit.

### Exit condition
No planning agent needs to guess active runtime/capability state.

---

## Phase 4 — Architecture Ownership Gate

### Goal
Prevent working code from being implemented in the wrong layer.

### Build
A checker plus review contract mapping directories/types to ownership rules.

### Core ownership
- UI → interaction and presentation;
- Brain → understanding/planning/context;
- orchestration → workflow sequencing;
- Revit bridge → Revit API execution;
- registry → identity/contracts/lifecycle;
- persistence → durable state;
- updater → installation/update lifecycle;
- tests/tools → proof and enforcement.

### Detect
- business logic in UI;
- duplicate capability logic;
- Revit API work outside controlled layer;
- persistence side effects in unrelated code;
- direct bypass of trust/transaction gates.

### Exit condition
Wrong-layer changes are flagged before completion.

---

## Phase 5 — Offline Test Harness Expansion

### Goal
Move all truthfully testable work out of manual Revit sessions.

### Offline-test candidates
- intent/routing;
- fragment selection;
- schema validation;
- capability contracts;
- parameter/type mapping logic that does not need live API objects;
- result/error contracts;
- lifecycle transitions;
- UI state;
- report generation;
- installer/update logic;
- scope/architecture checks.

### Must remain real Revit proof
- actual Revit API calls;
- transactions;
- element creation/modification;
- geometry behaviour;
- connector behaviour;
- document-specific API edge cases;
- anything dependent on Autodesk runtime behaviour.

### Rule
Never fake Revit proof with a mock.

### Exit condition
A clear matrix states **offline-provable vs. real-Revit-required**, and automated coverage increases for the first category.

---

## Phase 6 — Evaluation-Gated Improvement Loop

### Goal
Allow controlled improvement of low-risk intelligence assets.

### First supported targets
- prompts;
- agent instructions;
- routing hints;
- fragment descriptions;
- evaluation cases;
- documentation;
- low-risk deterministic rules.

### Loop
1. baseline;
2. run evals;
3. diagnose failure;
4. propose one mutation;
5. apply mutation in isolated branch/worktree;
6. rerun same evals;
7. compare;
8. keep or revert;
9. record evidence.

### Hard restrictions
No automatic mutation of:

- core architecture;
- Revit transaction logic;
- production write fragments;
- trust/permission code;
- installer/updater;
- public persistent-storage contracts.

### Exit condition
At least one low-risk Heron asset can be improved through the loop with measurable keep/revert evidence.

---

## Phase 7 — Legacy Change Safety + Dependency Gate

### Goal
Make cleanup and upgrades safer.

### Before deleting/replacing old code
Check:

- introducing commit;
- later fixes;
- related issues/tests;
- Revit version reasons;
- compatibility workarounds;
- blast radius.

### Dependency checks
Flag:

- unused dependency;
- duplicate dependency;
- conflicting version;
- obsolete package;
- new dependency with no clear need;
- licence/provenance change.

### Exit condition
Cleanup work can prove why removal is safe, and dependency growth requires explicit justification.

---

## Phase 8 — Release Verification Gate

### Goal
Treat packaging and delivery as part of engineering quality.

### Check where applicable
- clean build;
- expected package contents;
- per-user install path;
- no-admin promise;
- Revit version add-in discovery;
- update from previous version;
- rollback/recovery;
- clean restart after update;
- installer/update integrity;
- required notices/licences.

### Exit condition
A source-level PASS cannot become a release PASS unless delivery checks also pass.

---

## Phase 9 — Unified Critic and Final Decision

### Goal
Make one independent final review across all previous evidence.

### Critic questions
- Did scope expand?
- Was anything duplicated?
- Is responsibility in the correct layer?
- Are tests only happy-path?
- Did version compatibility change?
- Did fallback become silent?
- Did risk/trust increase?
- Did dependencies increase?
- Did packaging change?
- Are docs now stale?
- Is real Revit proof missing?

### Decision logic
- everything proven → `PASS`;
- fix needed inside intended scope → `REVISE`;
- unrelated valid work mixed in → `SPLIT`;
- measurable regression → `REVERT`;
- external dependency/decision missing → `BLOCKED`;
- only missing evidence is live Revit behaviour → `NEEDS_REAL_REVIT_PROOF`.

### Exit condition
Every substantial Heron change can produce one deterministic final status and evidence summary.

---

# 7. Risk levels and required gates

## Low risk
Examples:
- docs;
- prompt wording;
- metadata;
- isolated read-only deterministic tools.

Required:
- intent;
- baseline;
- tests;
- scope check;
- critic.

## Medium risk
Examples:
- routing;
- memory/context;
- registry changes;
- installer logic without model writes;
- significant refactor.

Required:
- all low-risk checks;
- architecture gate;
- compatibility check;
- dependency/provenance check;
- stronger regression coverage.

## High risk
Examples:
- Revit writes;
- transaction behaviour;
- unattended automation;
- trust/security;
- updater affecting live install;
- public storage/API contract.

Required:
- all gates;
- human review;
- real Revit proof where applicable;
- rollback evidence;
- independent critic;
- no automatic self-mutation.

---

# 8. How agents should work with this plan

For each implementation task, agents must use this sequence:

1. read Heron Constitution, Golden Rules, Decisions, and relevant module docs;
2. inspect current code before proposing changes;
3. identify existing reusable tools/checkers;
4. revisit external repositories only for the specific mechanism being implemented;
5. confirm current source, not old memory;
6. design Heron-native solution;
7. implement smallest viable change;
8. run the appropriate gate subset;
9. record evidence;
10. stop if result is not clearly better.

External repository names should not leak into Heron production naming unless attribution/licence documentation requires it.

---

# 9. No-go rules

Do not:

- build a new multi-agent framework just for this;
- replace working Heron architecture to resemble another project;
- add a new database unless a measured gap proves it is needed;
- add a new registry if an existing Heron registry owns the concept;
- auto-rewrite production Revit code;
- treat passing unit tests as proof of live Revit behaviour;
- accept a large diff without scope review;
- delete compatibility code without history review;
- accept new dependencies without justification;
- claim completion without fresh evidence.

---

# 10. Suggested implementation artifacts

The exact file names can change after repository audit, but the logical artifacts should be:

- Improvement Gate orchestrator;
- intent schema;
- evidence schema;
- runtime/environment snapshot provider;
- scope checker;
- architecture ownership checker;
- dependency/provenance checker;
- packaging verification checker;
- critic/reviewer step;
- reusable CLI/tool entry point;
- automated tests;
- work-note execution record.

Do not create all of these as separate systems if existing Heron tools can absorb them.

---

# 11. Acceptance criteria for the whole initiative

The initiative is complete only when all are true:

1. A small bug fix can run through the Improvement Gate end to end.
2. A broader refactor can be detected as scope creep when it exceeds intent.
3. A low-risk prompt/agent asset can use baseline → mutation → keep/revert.
4. An environment snapshot can distinguish unavailable vs. unsupported vs. trust-blocked.
5. A wrong-layer implementation can be flagged.
6. Offline-provable behaviour can run without Revit.
7. Real Revit-required behaviour is clearly marked and not falsely passed.
8. A dependency addition is reported and justified.
9. A release-affecting change triggers packaging checks.
10. Final status is machine-readable and evidence-backed.
11. Existing Heron architecture remains intact unless a separately approved decision changes it.
12. No external code is copied without explicit licence/provenance handling.

---

# 12. First pilot task

Do not test the system first on a dangerous Revit write feature.

Use one contained real Heron task with:

- a clear bug or improvement;
- deterministic offline tests;
- a small expected diff;
- no installer/security change;
- no live model write requirement.

The pilot should prove:

- intent capture;
- baseline;
- scope report;
- architecture check;
- tests;
- critic;
- evidence record;
- final PASS/REVISE/REVERT decision.

After that succeeds, move to a medium-risk task, then finally a Revit-write task requiring real model proof.

---

# 13. Closure and retirement

This work note is temporary.

It retires when:

- the Improvement Gate exists and has passed pilot use;
- durable architecture rules are moved into permanent Heron docs/decisions;
- quality-gate requirements are moved into testing/quality documentation;
- runtime/context rules are moved into the relevant permanent architecture pages;
- unresolved items are moved to OPEN-QUESTIONS or PROPOSALS;
- implementation evidence is preserved in the correct durable location.

Then delete this work note and repair references.

---

# 14. Final operating rule

For every future important Heron change:

**Understand → Baseline → Inspect impact → Research if needed → Plan → Implement small → Test → Check scope → Check architecture → Check compatibility → Check trust/dependencies/package → Critique → Prove → Keep / Revise / Split / Revert.**

That is the system to make Heron progressively better without letting AI-generated work slowly damage the codebase.
