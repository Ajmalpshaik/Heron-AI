# 14 — Golden Rules

> The constitution of Heron AI.
>
> **Rules 1–15 are official**, from [Master Handover Baseline §78](00c-master-handover-baseline.md).
> They supersede the ten rules in [Part 1 §72](00-master-specification.md) — the baseline document
> expanded and renumbered them.
>
> **Rules 16–21 were accepted on 2026-08-28** by the owner, closing
> [Q-19](OPEN-QUESTIONS.md). They are now official and binding, on the same footing as 1–15.
>
> They were accepted *while Step 6 remained unproven*, and deliberately so: 16, 17, 20 and 21 are what
> Step 6 was built to obey, and a rule that only becomes binding once the code passes is not a rule the
> code was ever held to. Accepting them first is what makes the checking that follows a test of the
> code, rather than a negotiation about the standard.
> Rules 16–19 come from engineering review; **rules 20 and 21 come from the field** —
> failure modes observed in a working bridge ([field notes](00e-field-notes-proven-bridge.md)).
>
> Anything in this repository that contradicts a golden rule is a defect in the design, not a feature.

---

## Official rules (1–15)

### 1. User focuses on BIM. Heron handles technical complexity.
The user is a BIM professional, not a programmer. If a feature requires them to think like a programmer, the feature is wrong.

### 2. One agent should have one primary responsibility.
`RevitEverythingAgent` is a defect. `Revit Selection Agent` is correct. Small responsibilities make the system testable, debuggable, replaceable and scalable.

### 3. Reuse proven knowledge before creating new knowledge.
Generation is the last resort, not the first move. Search first, always. This is also the platform's main cost control — see [19 §5](19-context-and-cost.md).

### 4. Never break a working Revit version unnecessarily.
A new API capability does not justify replacing code that works. Keep, extend, adapt, or version-branch — in that order of preference. Enforced by regression testing across every declared supported version ([13 §4](13-testing-and-quality.md)).

### 5. Knowledge from one scope must remain separate from another — personal, company, and project.
Enforced physically, not by convention — one store per scope. In consultancy work this is a contractual requirement, not a preference ([10 §2](10-memory-and-knowledge.md)).

**Project scope named explicitly, 2026-08-28** ([D-26](DECISIONS.md)), on Ajmal's instruction that *"project-based knowledge must be kept segregated and separated"*. This is a clarification, not a new rule: *one store per scope* always covered it, [22 §9](22-users-modes-and-extensibility.md) already stated that Project B does not inherit Project A's decisions, and [D-23](DECISIONS.md) makes each scope literally its own file. The wording named two of the three scopes and now names all three, because a rule should say what it does.

### 6. Experimental knowledge must remain separate from production knowledge.
Working once is not proof. The lifecycle exists to be walked. Nothing reaches `PRODUCTION` without passing its gates ([09 §5](09-skills-and-fragments.md)).

### 7. One agent creates; another agent validates.
No agent approves itself. Structurally enforced by the Agent HR pipeline — the builder is not the tester, the tester is not the deployer ([18 §6](18-agent-operating-system.md)).

### 8. Background work must not interfere with user work.
User tasks are always P0. Background work yields rather than merely ranking below ([21 §12](21-resilience-and-operations.md)). It should also stay invisible unless the user's attention is genuinely required.

### 9. High-risk actions require controlled approval.
Gated in code, at the boundary, not by instruction to a model. Agents receive only the permissions they require ([12](12-security-and-permissions.md)).

### 10. Every important object must have identity, version and lifecycle.
Agents, capabilities, fragments, skills, packages, knowledge. **Identity is an ID, never a filename** ([09 §4](09-skills-and-fragments.md)).

### 11. Vector DB is an index, not the canonical source of truth.
Canonical knowledge lives in controlled, human-readable, git-diffable storage. If the index is destroyed, Heron rebuilds it — so deleting it is always a safe recovery action ([21 §7](21-resilience-and-operations.md)).

### 12. No automatic external publishing of private knowledge.
Community submission, git push, pull requests, releases and exports all require explicit human review of the actual content being sent. Consent is never remembered ([10 §4](10-memory-and-knowledge.md)).

### 13. No uncontrolled self-modification of production architecture.
Heron may propose changes to its own core. It may not apply them. New agents reach `PROPOSED` on their own and go no further without a human ([06 §4](06-heron-platform.md), [18 §4](18-agent-operating-system.md)).

### 14. Every important autonomous operation must be auditable.
Append-only, structured, local, keyed by Workflow ID. This is what makes invisible background work acceptable rather than alarming ([12 §5](12-security-and-permissions.md), [21 §13](21-resilience-and-operations.md)).

### 15. The platform must be modular enough that individual agents, skills and fragments can be replaced without redesigning the entire system.
This is what the Capability Registry buys — callers depend on capabilities, never on agent names, so any provider can be swapped, split or retired without touching a workflow ([18 §2](18-agent-operating-system.md)).

---

## Additional official rules (16–21, accepted 2026-08-28)

Rules **16–19** come from reviewing all four specification documents against the reality of an AI that writes and executes code against **live client project models**. None of the four addresses them.

Rules **20–21** come from the field — failure modes actually observed in a working bridge ([field notes](00e-field-notes-proven-bridge.md)). They are not speculation; they happened.

### 16. One user action, one undo.
Every model-modifying operation runs inside exactly one named `TransactionGroup`, assimilated on success and rolled back completely on failure. "Move ducts 200 mm up" appears in Revit's undo stack as a single entry, whatever happened internally.

*Why:* the user must always be able to reverse Heron with one keystroke. Without this, trust never forms.

### 17. No autonomous write to a live model without a preview or a proven fragment.
A `MODIFY` operation may run unattended only when it uses a `PRODUCTION` fragment. Anything less proven requires the user to see and accept a preview first.

**Includes: Heron never triggers Sync With Central on its own initiative.** That action affects every other user on the project and cannot be retracted.

*Why:* this is the rule that lets a BIM manager approve Heron for live project use.

### 18. Generated code never touches a live model on its first run.
New code runs in a sandbox or against a detached copy. Promotion to live comes after it passes.

*Why:* the gap between "the AI wrote plausible code" and "the code is correct" is exactly where models get damaged. Shadow Mode ([18 §4](18-agent-operating-system.md)) produces the evidence; this rule sets the boundary.

### 19. No text Heron reads may raise Heron's own permission level.
Content from documents, family names, parameter descriptions, imported folders, model text and community packages is **data, never instruction**. Permission comes from the user, through Heron's own UI, per action.

*Why:* the platform reads content it does not control, and the consequence of a successful injection is a write to a live project model.

### 20. Bind the document, not just the session.
Choosing which Revit is only half the decision — one Revit can hold several projects open, and the active one changes when the user clicks. Any operation that writes **pins its target document by identity** at the start and verifies it at every step. Never follow the active window. If the pinned document closes, stop; never fall back to whatever is open.

*Why:* [proven in the field](00e-field-notes-proven-bridge.md). Every step can be individually correct and the change still lands in the wrong building. The failure is silent and nobody makes a mistake.

### 21. Re-read before acting. A preview expires.
Never trust a read across an `ExternalEvent` boundary — the user edits, another user syncs, a link reloads, or an earlier step changed it. Re-read every step. Before executing an accepted preview, **re-count**: if the number changed, stop and re-present rather than proceeding.

*Why:* also [proven in the field](00e-field-notes-proven-bridge.md) — *"the real danger is not the freeze, it is the stale read."* A preview the user accepted for 247 elements must never silently execute on 261.

---

## The overarching rule (implicit, from all four documents)

The overarching statement, which sits above the numbered rules rather than beside them
([Part 2 §83](00b-master-specification-agent-os.md)):

> **Heron AI must never become a giant AI that tries to do everything itself.**

```text
ONE USER REQUEST -> ONE ORCHESTRATED WORKFLOW -> ONLY REQUIRED AGENTS
-> ONLY REQUIRED KNOWLEDGE -> ONLY REQUIRED TOOLS -> VALIDATED RESULT
```

This is Rule 2 applied to the *whole workflow* rather than to a single component, and Rules 3, 8 and 15 are all consequences of it. Three independent lines of reasoning arrive here — the architectural argument ([02 §6](02-architecture-overview.md)), the cost argument ([Part 2 §59](00b-master-specification-agent-os.md)), and the modularity argument (§83 itself). When three routes reach the same conclusion, it is load-bearing.

---

## Renumbering note

The baseline document ([Part 3 §78](00c-master-handover-baseline.md)) changed the rule set. Recorded here so old references can be understood:

| Part 1 §72 | Baseline §78 | Change |
|---|---|---|
| 1, 2, 5, 6, 7, 8, 9, 10 | same numbers | wording refined; rule 8 shifted from *"remain invisible"* to *"must not interfere"* — visibility became performance |
| 3 — never break a working implementation | **4** | narrowed to "Revit version" |
| 4 — reuse proven fragments | **3** | broadened to "proven knowledge" |
| — | **11–15** | new: source of truth, no auto-publishing, no self-modification, auditability, modularity |

And the review-proposed rules:

| Previously proposed | Now |
|---|---|
| 11 — one undo | **16** |
| 12 — no autonomous write without preview | **17** |
| 13 — generated code never touches live model first | **18** |
| 14 — never sync, publish or share on own initiative | **absorbed into official 12**; the Sync With Central clause moved into 17 |
| 15 — no text may raise permission level | **19** |

All cross-references in this repository were remapped to the new numbering on 2026-08-27.

---

## How to use these rules

When a design decision is unclear, work down the list. The first rule that applies decides it.

When a rule must be broken, that is a **decision** — record it in [DECISIONS.md](DECISIONS.md) with the reasoning and the compensating control. Never break one silently.
