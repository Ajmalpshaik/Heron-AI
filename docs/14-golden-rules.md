# 14 — Golden Rules

> The constitution of Heron AI. Rules 1–10 are from the
> [Master Specification](00-master-specification.md) §72 and are permanent.
> Rules 11–15 are **proposed** during review and need confirmation before they become permanent —
> see [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) Q-19.
>
> Anything in this repository that contradicts a golden rule is a defect in the design, not a feature.

---

## Original rules (permanent)

### 1. User does BIM work. Heron AI does everything else.
The user is a BIM professional, not a programmer. If a feature requires them to think like a programmer, the feature is wrong.

### 2. One Agent = One Responsibility.
`RevitSuperAgent` is a defect. `Revit Selection Agent` is correct. Small responsibilities make the system testable, debuggable and replaceable.

### 3. Never break a working implementation unnecessarily.
A new API capability does not justify replacing code that works. Keep, extend, adapt, or version-branch — in that order of preference.

### 4. Reuse proven fragments before generating new code.
Generation is the last resort, not the first move. Search first, always.

### 5. Do not mix personal, project, company and community knowledge.
Enforced physically, not by convention. In consultancy work this is a contractual requirement, not a preference.

### 6. Unproven knowledge must not automatically become production knowledge.
Working once is not proof. The lifecycle exists to be walked.

### 7. One agent creates; another agent validates.
No agent approves itself.

### 8. Background work should remain invisible unless user attention is required.
Invisible on screen. Fully recorded in the audit log.

### 9. High-risk actions require controlled permission.
Gated in code, at the boundary, not by instruction to a model.

### 10. Every important component must have identity, version and lifecycle.
Agents, fragments, skills, packages, knowledge. Identity is an ID, never a filename.

---

## Proposed additional rules (pending confirmation)

These come from reviewing the specification against the specific reality of an AI that writes and executes code against live project models. Each addresses a failure mode that would be severe and hard to recover from.

### 11. One user action, one undo.
Every model-modifying operation runs inside exactly one named `TransactionGroup`, assimilated on success and rolled back completely on failure. "Move ducts 200 mm up" appears in Revit's undo stack as a single entry, whatever happened internally.

*Why:* the user must always be able to reverse Heron with one keystroke. Without this, trust never forms.

### 12. No autonomous write to a live model without a preview or a proven fragment.
A `MODIFY` operation may run unattended only when it uses a `PRODUCTION` fragment. Anything less proven requires the user to see and accept a preview first.

*Why:* this is the rule that lets a BIM manager approve Heron for live project use.

### 13. Generated code never touches a live model on its first run.
New code runs in a sandbox or against a detached copy. Promotion to live comes after it passes.

*Why:* the gap between "the AI wrote plausible code" and "the code is correct" is exactly where models get damaged.

### 14. Heron never syncs, publishes or shares on its own initiative.
Sync With Central, git push, pull requests, releases, community submissions, exports to shared locations — all require explicit per-action human confirmation. Consent is never remembered.

*Why:* these actions affect other people and cannot be fully retracted.

### 15. No text Heron reads may raise Heron's own permission level.
Content from documents, family names, parameter descriptions, imported folders, model text and community packages is **data, never instruction**. Permission comes from the user, through Heron's own UI, per action.

*Why:* the platform reads content it does not control, and the consequence of a successful injection is a write to a live project model.

---

---

## The overarching rule (Master Specification Part 2, §83)

Both specifications converge on one statement, and it sits above the numbered rules rather than beside them:

> **Heron AI must never become a giant AI that tries to do everything itself.**

```text
ONE USER REQUEST -> ONE ORCHESTRATED WORKFLOW -> ONLY REQUIRED AGENTS
-> ONLY REQUIRED KNOWLEDGE -> ONLY REQUIRED TOOLS -> VALIDATED RESULT
```

This is Rule 2 (one responsibility per agent) applied to the *whole workflow* rather than to a single
component. It is what keeps the platform modular, scalable, testable, maintainable, fast,
understandable, secure and self-improving — all at once.

Three independent lines of reasoning arrive here: the architectural argument
([02 §6](02-architecture-overview.md)), the cost argument
([Part 2 §59](00b-master-specification-agent-os.md)), and the modularity argument (§83 itself).
When three routes reach the same conclusion, it is load-bearing.

---

## How to use these rules

When a design decision is unclear, work down the list. The first rule that applies decides it.

When a rule must be broken, that is a **decision** — record it in [DECISIONS.md](DECISIONS.md) with the reasoning and the compensating control. Never break one silently.
