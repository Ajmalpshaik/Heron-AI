<!--
Heron-Agent:  none
Heron-Step:   18
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 35 — Independent Study Notes: OpenDesign, Awesome LLM Apps, and OpenHands

**Purpose:** second-pass review of the three repositories referenced by [34](34-project-perfection-and-continuous-upgrade-plan.md), with corrections, stronger evidence, and Heron-specific recommendations.

This note is intentionally separate from the implementation plan. It records what was actually observed in the repositories and what Heron should or should not take from them.

---

## 1. Audit result for document 34

The overall direction of [34](34-project-perfection-and-continuous-upgrade-plan.md) is sound: study first, adapt to Heron, measure changes, and never treat external repositories as code to copy blindly.

### Corrections / clarifications

1. **OpenHands Agent Canvas is archived.** The archived repository points to `OpenHands/OpenHands`. Future implementation research must use the current `OpenHands/OpenHands` repository as the primary reference and use `agent-canvas` only as historical context.
2. **Do not call all three repositories “proven” as a blanket statement.** They are useful engineering references, but Heron still has to verify every concept against its own constraints and Revit workflow.
3. In the OpenDesign section, **“project refresh/refinement workflow for existing codebases” is too broad.** The stronger, directly observed idea is a managed project/file workspace with import, versioning, preview, artifact bookkeeping, critique and common daemon-backed APIs.
4. **Licence checking must be file/folder aware.** A repository may contain components with different licences or vendored material. Do not infer the licence of a useful file only from the repository name or a top-level assumption.
5. Self-improvement must remain **evaluation-gated and bounded**. The useful pattern in Awesome LLM Apps is not “let an agent rewrite itself”; it is baseline → diagnose → one targeted mutation → re-evaluate → keep or revert.

These are not reasons to discard [34]. They tighten it.

---

## 2. Study method

For each repository, this pass looked beyond the landing page and focused on architecture, skills/agent boundaries, evaluation, quality gates, runtime separation, artifact handling, scope control and provenance.

The rule for future work remains:

**README for orientation → implementation/docs for truth → compare with Heron → adapt only the useful mechanism.**

If README and implementation disagree, implementation wins.

---

# 3. OpenDesign — independent notes

Reference: `https://github.com/nexu-io/open-design`

## 3.1 Strongest finding: one authority, many interfaces

OpenDesign's architecture makes the daemon the authority for project state, file operations, runtime launch, registries, automation and related services. Web and CLI surfaces call the same backend rather than implementing separate business logic.

### Heron lesson

Heron should keep one authoritative implementation for a capability and expose it through adapters/interfaces. Avoid:

- one logic path for UI;
- another for MCP;
- another for automation;
- another for tests.

For Revit work, this means the actual Revit operation remains one controlled capability/fragment path while UI, natural-language agent, MCP or future automation only request that capability.

**High-value adoption: YES.**

---

## 3.2 Explicit ownership boundaries

OpenDesign separates web UI, daemon, runtime adapters, registries and persistent project state. The UI is not the execution engine. Runtime definitions describe how a runtime is launched/probed while shared lifecycle logic stays centralized.

### Heron lesson

Strengthen ownership rules across:

- Heron UI;
- Heron Brain;
- MCP/server orchestration;
- Revit bridge;
- capability/fragment registry;
- persistent memory/state;
- automation/update services.

When a bug occurs, the first question should be: **which layer owns this behaviour?** A fix in the wrong layer should fail architectural review even if it appears to work.

**High-value adoption: YES.**

---

## 3.3 Runtime/service discovery instead of assumptions

OpenDesign resolves runtime definitions, executable paths, model/auth/capability probes and service identities rather than assuming a fixed environment.

### Heron lesson

Heron should explicitly discover and report:

- which Revit version is active;
- which bridge/runtime is available;
- which capabilities are supported in that version;
- which optional external tools/providers are available;
- whether a capability is unavailable vs. merely not detected.

Do not silently guess environment state.

**Adopt concept.**

---

## 3.4 Registries are useful only when contracts are strict

OpenDesign has distinct functional skills, design templates, design systems and plugins. They are separated because they have different responsibilities and storage/validation rules.

### Heron lesson

Heron already has agents, capabilities, skills/fragments and related registries. Do **not** add another registry because OpenDesign has one. Instead, use this as a check that every Heron registry has:

- one purpose;
- stable identity;
- schema validation;
- lifecycle/status;
- clear ownership;
- compatibility information;
- no duplicated source of truth.

**Use as an audit principle, not a new subsystem.**

---

## 3.5 Artifact bookkeeping is more important than flashy UI

OpenDesign treats project files/artifacts as managed objects: files can be listed, written, versioned, previewed and associated with their generating source/skill. The project workspace is part of the workflow, not an accidental side effect.

### Heron lesson

For generated Heron outputs—plans, reports, code drafts, fragment drafts, test evidence—store enough metadata to answer:

- who/what generated it;
- for which task;
- from which source/context;
- current lifecycle state;
- whether it was verified;
- which version replaced it.

Heron's existing metadata standard is a good foundation. Extend only where a real traceability gap exists.

**High-value adoption: YES, through existing metadata architecture.**

---

## 3.6 Critique/lint before final emission

OpenDesign documents artifact lint and multi-dimensional self-critique before output is treated as finished.

### Heron lesson

Heron should use a final pre-completion critique that is different from ordinary unit tests. It should check:

- scope;
- architecture placement;
- compatibility;
- safety/trust;
- unsupported claims;
- missing negative cases;
- stale documentation;
- packaging/provenance when relevant.

This maps well to the [34] Project Perfection Loop.

**Adopt concept.**

---

## 3.7 Provenance and vendoring discipline

OpenDesign contains explicit licence and vendored-source notes in parts of the repository. Some skills/components can carry their own licensing information.

### Heron lesson

Do not use a single repository-level licence assumption when studying external code. Before any actual code/material reuse, inspect the exact path and its applicable licence/NOTICE/provenance.

**Mandatory rule.**

---

## 3.8 What NOT to copy from OpenDesign

Do not import:

- its Next.js/Express/Electron product shape;
- its design-template terminology;
- its plugin marketplace architecture unless Heron independently needs one;
- its filesystem/project model wholesale;
- its runtime stack simply because it supports many coding agents.

Heron is a BIM/Revit platform. Take mechanisms, not product shape.

---

# 4. Awesome LLM Apps — independent notes

Reference: `https://github.com/Shubhamsaboo/awesome-llm-apps`

This repository is broad. The useful part for Heron is not the number of examples. It is the small set of workflow/evaluation patterns that can make Heron changes safer.

## 4.1 Self-improving skill loop: the most useful pattern

The self-improving skill example uses distinct execution, diagnosis and mutation responsibilities. It establishes tests/evaluation criteria, runs a baseline, applies one targeted change, reruns evaluation, and keeps the mutation only when the score improves.

### Heron lesson

Build a reusable improvement protocol:

1. define target;
2. create/confirm evaluation cases;
3. run baseline;
4. diagnose failures;
5. propose one mutation;
6. run the same evaluation;
7. compare;
8. keep or revert;
9. record evidence.

This should first target prompts, agent instructions, fragment descriptions, routing hints, validation rules and tests—not Revit production write logic.

**Highest-value adoption from this repository.**

---

## 4.2 Scope Creep Detector: intent vs. diff

The repository includes a scope-creep skill that compares a stated intent with the actual Git diff and flags unrelated paths, dependency additions, API/config/CI changes, oversized changes and subsystem spread. It has explicit eval and trigger cases.

### Heron lesson

Every significant Heron change should have a one-line **change intent** before implementation. At review time, compare the final diff against that intent.

A Heron scope report should classify:

- required change;
- directly supporting change;
- test/evidence change;
- documentation change;
- unrelated/suspicious change;
- dependency/config/build changes requiring explicit justification.

This directly supports your goal of “fix or upgrade the project without making a new mess.”

**Very high-value adoption.**

---

## 4.3 Eval files beside skills are a strong pattern

The repository does not only describe some skills; it also includes expected-output evals, trigger-behaviour cases and executable tests.

### Heron lesson

A capability or agent definition should increasingly travel with proof of:

- when it should trigger;
- when it should not trigger;
- expected behaviour;
- negative/edge cases;
- deterministic tests where possible.

For Heron fragments, this can strengthen the lifecycle from DRAFT to proven status.

**Adopt concept.**

---

## 4.4 Dependency Doctor / Commit Archaeology

These patterns are useful because they force the agent to inspect dependency health and historical intent before “cleaning” code that may exist for a reason.

### Heron lesson

Before deleting or replacing suspicious legacy code:

- inspect the commit that introduced it;
- inspect later fixes around it;
- check tests/bugs it was protecting;
- check whether the dependency or workaround is still necessary.

This is especially important for Revit-version compatibility code where something that looks ugly may be protecting 2020–2027 behaviour.

**Adopt as review discipline.**

---

## 4.5 Trust-gated and multi-agent patterns

The repository contains examples of multi-agent collaboration and trust-gated workflows.

### Heron lesson

Use multiple logical roles only when risk justifies them. Do not build a swarm for ordinary deterministic work.

Good use:

- architecture change;
- production Revit write operation;
- installer/update change;
- security/trust change;
- large migration.

Bad use:

- simple selection/filter capability;
- deterministic metadata edit;
- trivial docs fix.

**Adopt selectively.**

---

## 4.6 What NOT to copy from Awesome LLM Apps

Do not import:

- generic agent frameworks simply because examples use them;
- external model/provider dependencies without a Heron need;
- demo UIs;
- domain-specific agents unrelated to BIM;
- general-purpose self-evolving behaviour.

The repository is a pattern library for us, not a dependency library.

---

# 5. OpenHands / Agent Canvas — independent notes

Historical reference: `https://github.com/OpenHands/agent-canvas`

Current reference: `https://github.com/OpenHands/OpenHands`

## 5.1 Important correction: current source moved

`OpenHands/agent-canvas` is archived and its README points to `OpenHands/OpenHands`.

### Heron rule

Future research must always verify whether a reference repository has moved, been archived or changed ownership before planning against it.

This check belongs in the mandatory research step in [34].

---

## 5.2 UI is not the execution boundary

The Agent Canvas architecture explicitly separates the frontend from agent execution, sandbox/workspace isolation and automation backend responsibilities.

### Heron lesson

Heron UI must never become the place where Revit business logic lives.

The UI should:

- gather intent;
- show state/progress;
- request actions;
- present evidence/results.

The controlled backend/Revit bridge owns execution.

**High-value confirmation of Heron's direction.**

---

## 5.3 Backend/service information should be explicit

Agent Canvas uses backend-provided runtime service information rather than making the frontend guess service URLs/ports.

### Heron lesson

The same principle applies beyond networking: agents should receive explicit authoritative environment context instead of inferring it.

Examples:

- active document and Revit version;
- writable/read-only state;
- supported capability set;
- selected project/context;
- available linked models/data sources;
- execution/trust mode.

**Adopt concept.**

---

## 5.4 Mock mode is strategically important

Agent Canvas has a mock development mode and CI gates for lint, tests, builds and packaging.

### Heron lesson

Heron's biggest long-term speed improvement is not another agent. It is increasing the amount of behaviour that can be tested without manually opening Revit.

Build or expand deterministic simulation/mocking around:

- routing;
- plan generation;
- fragment selection;
- parameter/type validation;
- result/error contracts;
- lifecycle/state transitions;
- installer/update logic;
- UI state.

Real Revit remains mandatory for API behaviour that cannot be truthfully simulated.

**Very high-value adoption.**

---

## 5.5 Local workspace access is a security boundary

OpenHands explicitly treats workspace access and local execution as security-sensitive.

### Heron lesson

Heron's equivalent boundary is stronger because a mistake can change a live BIM model.

Treat as privileged:

- model write operations;
- file writes outside Heron-owned paths;
- external process launch;
- package/update installation;
- credentials/tokens;
- automation that can execute without a live user confirmation.

Never let convenience blur the trust model.

**Mandatory safety principle.**

---

## 5.6 Packaging is part of quality, not an afterthought

The current OpenHands/Agent Canvas architecture documents packaging and CI checks, including application/library build and package verification.

### Heron lesson

A change is not complete if source tests pass but installation/update is broken.

For release-affecting work, the Heron gate should include:

- clean build;
- expected install location;
- no-admin install behaviour where promised;
- Revit version add-in discovery;
- upgrade from previous version;
- rollback/recovery behaviour;
- clean uninstall where supported;
- package contents audit.

**Adopt concept.**

---

## 5.7 What NOT to copy from OpenHands

Do not import:

- browser/terminal coding-agent UI as Heron's main interaction model;
- general coding sandbox architecture into Revit execution;
- its backend stack wholesale;
- host-filesystem agent access without Heron's tighter trust controls.

Use its separation, testing and security lessons—not its product shape.

---

# 6. Combined findings — what all three teach Heron

The three repositories differ, but their strongest useful ideas converge into one engineering system:

### A. One source of truth

Do not duplicate business logic across UI, agents, MCP and automation.

### B. Explicit boundaries

Every behaviour has an owner: UI, Brain, orchestration, Revit bridge, registry, persistence or updater.

### C. Artifact + evidence, not chat memory

Plans, tests, results and improvement evidence must be stored as inspectable artifacts.

### D. Baseline before change

You cannot prove an upgrade without knowing the starting state.

### E. Small mutation, fast verification

Prefer one targeted change followed by focused proof over large speculative rewrites.

### F. Intent vs. diff

Every change should be checked for scope creep before acceptance.

### G. Quality includes packaging and operations

Build/test success alone is not sufficient for a desktop/Revit product.

### H. Mock more, but never fake Revit proof

Shift everything that can be truthfully tested outside Revit into fast automated tests. Keep real Revit verification for actual Revit API behaviour.

### I. Discovery instead of guessing

Runtime, version, capability and service state should be explicit.

### J. Provenance is path-specific

When actual external material is reused, inspect the exact source and licence. Repository-level assumptions are not enough.

---

# 7. My recommended Heron-native feature: Improvement Gate

The best combined idea from this study is a single reusable **Heron Improvement Gate** rather than three separate copied systems.

It should operate on a proposed feature/fix/upgrade and produce one evidence package.

## Inputs

- one-line intent;
- affected subsystem;
- acceptance criteria;
- risk level;
- baseline commit/state;
- relevant tests;
- optional external research references.

## Pipeline

1. **Baseline** — capture current behaviour and checks.
2. **Impact scan** — identify likely affected files/capabilities/tests.
3. **Research check** — only when useful; revisit current external reference files.
4. **Plan** — smallest Heron-native change.
5. **Implement** — bounded diff.
6. **Focused verification** — target + negative + regression.
7. **Scope check** — compare actual diff to one-line intent.
8. **Architecture check** — confirm ownership/boundary correctness.
9. **Compatibility check** — Revit/version/platform where relevant.
10. **Security/trust check** — especially for write/automation/update actions.
11. **Package check** — if release/install surface changed.
12. **Critique** — independent completion review.
13. **Decision** — accept, revise, split or revert.
14. **Evidence record** — store what proved the decision.

## Output

A machine-readable + human-readable result containing:

- status: `PASS | REVISE | SPLIT | REVERT | NEEDS_REVIT_PROOF`;
- intended scope;
- actual scope;
- test evidence;
- negative-case evidence;
- compatibility evidence;
- architecture findings;
- external research references used;
- provenance/licence notes if applicable;
- remaining risk;
- final reviewer decision.

This one mechanism can later support feature work, bug fixing, cleanup, refactoring, agent improvement and release preparation.

---

# 8. Priority implementation order after this research

1. **Change-intent + scope-creep report** for Git diffs.
2. **Reusable baseline/evaluation record** for any improvement task.
3. **Keep/revert mutation loop** for low-risk prompts/instructions/fragments.
4. **Architecture ownership checker/checklist** tied to Heron's existing layers.
5. **Mock/simulation expansion** to reduce manual Revit testing where truthful.
6. **Packaging/update verification gate**.
7. **Unified Improvement Gate** combining the above.

Do not start by building a new multi-agent framework. Use Heron's existing agent/orchestration system and add these capabilities to it.

---

# 9. Final research rule

When implementation starts, this note is **not enough**.

The implementing agent must return to the current external repositories and inspect the exact relevant files again. External repositories can move, archive, change implementation, change licence boundaries or invalidate an old assumption.

Use these notes as a map, never as a substitute for source verification.

**Heron's standard remains:**

> Study deeply. Understand the mechanism. Keep only the principle that solves a real Heron problem. Rebuild it in Heron's architecture. Prove it is better. Reject it if it is not.
