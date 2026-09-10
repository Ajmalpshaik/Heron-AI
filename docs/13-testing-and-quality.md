# 13 — Testing & Quality

> Derived from [Master Specification](00-master-specification.md) §25, §40, §64.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Eight testing levels

| Level | Scope | Runs where |
|---|---|---|
| 1 | Agent unit testing | CI, no Revit |
| 2 | Fragment testing | CI, mocked Revit |
| 3 | Skill testing | CI, mocked Revit |
| 4 | MCP testing | CI, mock add-in |
| 5 | Revit integration testing | **Needs real Revit** |
| 6 | Version regression testing | **Needs every supported Revit** |
| 7 | Full workflow testing | Needs real Revit |
| 8 | User acceptance testing | Human |

## 2. QA principle (§40)

> **One agent creates. Another agent validates. No agent approves itself.**

```text
Requirement -> Architecture -> Implementation -> Review -> Build
-> Test -> Revit Test -> QA -> Approval -> Release
```

## 2a. Three kinds of QA, not one

[Part 4 §18](00d-additional-requirements.md) and [Part 2 §48](00b-master-specification-agent-os.md)
together establish that "QA" means three genuinely different things:

| QA type | Asks | Runs |
|---|---|---|
| **Code QA** | Is the code correct, safe, maintainable? | CI, no Revit |
| **Revit QA** | Does it behave correctly *inside Revit*? | In Revit |
| **BIM QA** | Is the resulting **model** correct? | In Revit, against the model |

**Code QA ≠ Revit QA ≠ BIM QA.** All three are required, and the third is the one BIM professionals
actually care about.

**BIM QA** checks naming · parameters · categories · families · levels · worksets · views · modelling
rules · MEP connectivity · coordination requirements.

**[NOTE]** BIM QA is not really a test stage — it is a **product capability**. *"Check this model
against our standard"* is one of the six example requests in [Part 1 §1](00-master-specification.md).
The same engine that validates Heron's own output is the one a user invokes directly. Building it once
and using it in both places is a significant economy, and it means the QA engine gets exercised daily
rather than only in CI.

## 2b. Evaluation System and the Golden Test Library

[Part 4 §27–28](00d-additional-requirements.md) make automated evaluation a mandatory component.

| | |
|---|---|
| **Agent** | Duct Selection Agent |
| **Test** | Select all ducts |
| **Expected** | All visible ducts selected |
| **Result** | PASS |

> A permanent collection of known-good cases. Every major update runs against these tests.

**[NOTE]** This adopts the golden-file mechanism proposed in §4 below, and it is what makes
[Golden Rule 4](14-golden-rules.md) executable rather than aspirational.

Two things determine whether it works:

1. **Start it in Phase 1, not Phase 5.** A golden library only has value proportional to how long it has
   been accumulating. Started late, it encodes today's behaviour as correct — including today's bugs.
2. **Every fixed bug adds a case.** That is the discipline that makes the library grow from real
   failures rather than from imagination, and it is the same signal the Capability Gap report uses.

---

## 3. **[NOTE — the hardest engineering problem in the project]** Testing against Revit

Levels 5–7 require a running Revit with a document open. That is genuinely difficult to automate:

- Revit is a large GUI application with a licence check.
- It cannot run headless in any officially supported way.
- Standard test runners cannot drive the Revit API — code must execute inside Revit's process, in API context.
- CI runners do not have Revit, and Autodesk licensing generally does not permit putting it on arbitrary cloud machines.

**Known approaches:**

| Approach | Description | Assessment |
|---|---|---|
| **In-Revit test runner add-in** | A Heron add-in command that runs the suite inside Revit and writes results to disk | Practical, well-established pattern. Needs a person or a scheduled job to launch Revit. |
| **Revit in journal/automation mode** | Drive Revit via journal files | Brittle, poorly documented, but usable for smoke tests. |
| **Self-hosted CI runner with Revit installed** | A physical or VM machine with licensed Revit versions, registered as a CI runner | The realistic answer for level 6. Costs a machine and licences. |
| **Design Automation for Revit (APS)** | Autodesk's cloud service that runs Revit add-ins | Real, supported, cloud-based — but a constrained environment, no UI, and it costs per run. Worth evaluating seriously for regression testing. |
| **Mock the Revit API** | Interface over every Revit type, mock in unit tests | Essential for levels 1–4. Proves the logic, proves nothing about Revit. |

**Recommendation:**

- Levels 1–4 in normal CI against a **mocked Revit boundary**. This is why every Revit type must sit behind the Heron Revit boundary ([01 §8](01-vision-and-principles.md)) — testability, not just portability.
- Levels 5–7 via an **in-Revit test runner**, launched manually at first, then on a self-hosted runner once the matrix is worth automating.
- Evaluate **Design Automation for Revit** for level 6 before building a machine room.

Tracked as [Q-14](OPEN-QUESTIONS.md).

---

## 4. Regression testing (§25)

On every change to an existing fragment:

1. Identify supported versions
2. Build/test each supported implementation
3. Compare previous behaviour
4. Detect breaking changes
5. **Reject unsafe changes**
6. **Preserve the previous working implementation**

**[NOTE]** "Compare previous behaviour" needs a concrete mechanism, or it will not happen. The workable one is **golden-file testing**: each fragment has a fixed test model and a recorded expected result (element counts, `UniqueId` sets, parameter values, geometry hashes). A change that alters the golden output is a breaking change by definition, and must be explicitly acknowledged rather than silently accepted.

This makes Golden Rule 4 executable instead of aspirational.

## 5. **[NOTE]** Test models are project assets

The regression suite needs a small library of controlled `.rvt` files: an MEP model with ducts and pipes, a worksharing-enabled model, a model with known warnings, a model with links, a deliberately messy model.

These must be:

- **version-migrated** for each supported Revit release (a 2020 file opened in 2025 is silently upgraded — the suite must use per-version copies),
- **small** (git-friendly; consider Git LFS),
- **synthetic** — never a real client model. A client model in a git repository is a confidentiality breach waiting to happen.

Building these early costs a day and saves months.

## 6. **[NOTE]** What "success rate" actually measures

The Fragment Performance Agent tracks success rates, and the fragment lifecycle promotes on them. Worth being precise, or the number will mislead:

- **Success ≠ correct.** A fragment that selects 0 ducts "succeeded". Validation must assert the *expected* outcome, not merely the absence of an exception.
- **A user correction is a failure**, even if the operation succeeded technically. If the user immediately undoes it or rephrases, that is the strongest negative signal available and should be weighted accordingly.
- **Sample size matters.** 3/3 is not a 100% success rate. Promotion gates need a minimum N ([09 §5](09-skills-and-fragments.md)).

## 7. Quality gates before PRODUCTION

| Gate | Requirement |
|---|---|
| Tests exist | Written by a different agent than the implementer |
| Tests pass | On **every** declared supported version |
| No regression | Golden files unchanged, or change explicitly approved |
| Code review | By a separate agent (§40) |
| Security review | For anything at `MODIFY` or above |
| Real usage | N successful executions, no unexplained failures |
| **Human approval** | One person, recorded |
