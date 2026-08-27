# 12 — Security, Permissions & Audit

> Derived from [Master Specification](00-master-specification.md) §53–55.
> **[NOTE]** blocks are engineering commentary added during review.

---

## 1. Permission levels

```text
READ
ANALYZE
SUGGEST
EXECUTE
MODIFY
PUBLISH
ADMIN
```

Normal BIM operations can be automated. High-risk operations require confirmation.

**[NOTE]** Proposed concrete mapping:

| Level | Meaning | Confirmation | Examples |
|---|---|---|---|
| `READ` | Observe only | none | count elements, read parameters, list views |
| `ANALYZE` | Compute over what was read | none | clash summary, standards check, model statistics |
| `SUGGEST` | Propose a change without applying it | none | "these 12 ducts breach the standard, here is the fix" |
| `EXECUTE` | Non-destructive model action | none | change selection, open a view, zoom |
| `MODIFY` | Changes the model | **preview + confirm** | move elements, set parameters, place families |
| `PUBLISH` | Leaves the machine | **explicit, per action** | git push, PR, release, community submission, export to a shared location |
| `ADMIN` | Changes Heron itself | **explicit, per action** | install package, modify core config, activate an agent, run arbitrary code |

The important boundary is `EXECUTE | MODIFY`. Everything above it changes something the user cares about and cannot always undo.

---

## 2. High-risk operations (§53)

Deleting files · deleting Revit elements · purge · replacing code · modifying GitHub repositories · pushing code · publishing releases · changing configuration · installing external packages.

**[NOTE]** Additions to that list, drawn from what actually goes wrong in Revit work:

| Operation | Why |
|---|---|
| **Sync With Central** | Pushes changes to every other user on the project. Never automatic. |
| **Delete / purge unused** | Frequently irreversible in practice, and removes things other users depend on. |
| **Bulk parameter write** | One wrong value across 5,000 elements is a very bad afternoon. |
| **Family reload / replace** | Affects every instance in the model. |
| **Workset changes** | Affects other users' visibility and ownership. |
| **Changing view templates** | Silently changes every view using the template — often noticed only at print. |
| **Anything that touches a linked model** | Not your model to modify. |

---

## 3. Where the gate is enforced — **confirmed by Part 4 §29**

> ```text
> AI -> Permission Layer -> Tool Validation -> MCP -> Revit
> ```
>
> *"The AI requests an operation; the platform decides whether that operation is allowed."*
> — [Part 4 §29](00d-additional-requirements.md)

**The permission gate must live in the Revit add-in, not in the AI layer.**

The AI layer is a persuadable component. Heron reads content it does not control: imported documents, family names, shared parameter descriptions, model text notes, community fragments. Any of those can carry text aimed at the model. Prompt injection through a family name is an unusual attack, but the consequence — a `MODIFY` or `ADMIN` call on a live project model — is severe enough to design against.

Therefore:

1. Risk level is declared in the **tool registry**, not decided per call.
2. The **add-in** enforces the gate. A high-risk tool returns `REQUIRES_CONFIRMATION` plus a description of the intended effect. It does not execute.
3. Confirmation comes from the **user**, through UI Heron controls, scoped to **that one call**.
4. The model cannot grant itself permission, cannot mark a call pre-approved, and cannot escalate its own level.
5. All of it is logged.

Stated as a rule: **no text Heron reads may ever raise Heron's own permission level.**

---

## 4. **[NOTE]** Data confidentiality — the question that decides deployability

Every request potentially sends project content — element names, parameter values, family names, project identifiers, sometimes drawing content — to a model provider.

For BIM consultancy work, and especially for government or defence projects in Qatar and the wider region, this is frequently prohibited by contract.

Three positions, and the choice must be deliberate:

| Position | Meaning | Consequence |
|---|---|---|
| **Cloud only** | All reasoning via a hosted model API | Best quality. Unusable on restricted projects. |
| **Local only** | Local model, local embeddings, no egress | Deployable anywhere. Lower reasoning quality, real hardware cost. |
| **Hybrid, per project** | Project metadata sets the policy | Best fit for reality. Requires the policy to be enforced in code. |

**Recommendation: hybrid, enforced structurally.** A project marked `confidential` must be **incapable** of egress — not "the prompt says not to". Concretely: local embeddings, redaction at the boundary, and a hard block on `PUBLISH` for that project's scope.

Also needed regardless of position:

- A plain statement of **what is sent, to whom, and when**, that a user can show their client.
- **Redaction** of identifiers before anything leaves.
- **Zero-retention** confirmation from the provider where the deployment requires it.
- An honest answer to *"does using Heron breach our NDA?"* — the person deploying it will be asked.

Tracked as [Q-12](OPEN-QUESTIONS.md).

---

## 5. Audit log (§55)

Recorded for every important action: user request · selected agents · selected fragment · execution · result · errors · changes · approvals · version · timestamp.

**[NOTE]** Additions:

| Field | Why |
|---|---|
| **Document identity** | Which model, which project, which Revit version |
| **Permission decision** | What was gated, what was confirmed, by whom |
| **Element identities touched** | `UniqueId` list — the only way to answer "what did Heron change?" later |
| **Transaction group name** | Ties the log entry to the Revit undo stack |
| **Model call count and cost** | Makes spend visible instead of surprising |
| **Duration per stage** | Feeds performance work and the Capability Gap Agent |

Storage: append-only, structured (JSONL), rotated, and **local**. The audit log contains project information and must obey the same egress rules as everything else.

The audit log is what makes the "invisible background work" of §2.2 acceptable rather than alarming. Invisible on screen, fully recorded on disk. This is now [Golden Rule 14](14-golden-rules.md).

---

## 5a. Secret management (Part 4 §30)

> API keys, GitHub tokens and credentials must **never** be stored inside fragments, skills, prompts,
> source code or logs.

**[NOTE]** This becomes urgent rather than theoretical once the repository is public
([D-07](DECISIONS.md)). Anything committed to a public repository is compromised permanently — history
persists, forks propagate.

Requirements:

1. **A credential store** outside the workspace, in the **data** class ([06 §2](06-heron-platform.md)) —
   Windows Credential Manager or DPAPI-protected local storage. Never a file in the repo tree.
2. **Redaction on the way out.** The audit log, error messages, evidence records and anything shown to a
   model pass through a redactor. A token that reaches a log has already leaked.
3. **Secrets are never a fragment input.** A fragment that needs credentials receives a *handle*, and the
   Kernel resolves it at call time.
4. **Pre-commit scanning**, as a second layer — see [17 §2](17-open-source-and-distribution.md).

This is Article 17 of the [Constitution](../HERON_CONSTITUTION.md).

---

## 6. **[NOTE]** Safety rules specific to an AI that edits live models

Proposed additions to the Golden Rules, in [14](14-golden-rules.md):

> **16.** Every model-modifying operation runs in exactly one named `TransactionGroup`. One user action, one undo.

> **17.** No autonomous write to a live project model without either a preview the user accepted, or a proven PRODUCTION fragment. Heron never triggers Sync With Central on its own initiative.

> **18.** Generated code never executes against a live model on its first run. Sandbox, or detached copy, first.

> **19.** No text Heron reads may raise Heron's own permission level.

These four are what separate a tool a BIM manager will approve for use on live projects from one they will ban after the first incident.

The fifth originally proposed here — *"Heron never syncs, publishes or shares on its own initiative"* — is now **official Golden Rule 12** ([Baseline §78](00c-master-handover-baseline.md)). Its Revit-specific half, Sync With Central, moved into rule 17 above.
