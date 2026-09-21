# Security Policy

Heron AI executes code inside Autodesk Revit and can modify live project models.
Security issues here are not abstract — they can damage a real project someone is about to deliver.
Please treat them accordingly.

---

## Reporting a vulnerability

**Do not open a public issue for a security vulnerability.**

Report privately through **[GitHub Security Advisories](https://github.com/Ajmalpshaik/Heron-AI/security/advisories/new)**.

If that is unavailable, contact the maintainer directly and mark the message as a security report.

Please include:

- What the issue is, and what an attacker or a mistake could cause
- Steps to reproduce
- Revit version, Heron version, operating system
- Whether it affects a live model or only Heron's own files

You will get an acknowledgement within a few days. Fixes for issues that can damage a model are
prioritised above everything else.

---

## What counts as a security issue in Heron

Beyond the usual categories, these are specific to what Heron does:

| Category | Example |
|---|---|
| **Unauthorised model modification** | Any path that writes to a Revit model without passing the permission gate |
| **Permission escalation** | Anything that raises Heron's own permission level without a person approving it |
| **Prompt injection** | Text inside a family name, parameter description, imported document or community fragment that causes Heron to take an action the user did not ask for |
| **Data leakage** | Project data, model content or audit logs leaving the machine when they should not |
| **Supply chain** | A community package, fragment or skill that executes unexpected code |
| **Transport** | Anything that lets another process on the machine drive the Revit add-in |
| **Escaping the first-run guard** | Generated code reaching a live model before it has been approved. The guard **watches rather than contains** ([D-84](docs/DECISIONS.md)) — report anything that gets past what it does hold |

---

## Design rules that exist to prevent these

These are enforced in the architecture, not left to good behaviour. If you find a way around any of
them, that is a vulnerability worth reporting:

- **The permission gate lives in the Revit add-in**, not in the AI layer. High-risk operations return
  `REQUIRES_CONFIRMATION` and do not execute.
- **No text Heron reads may raise Heron's own permission level.** Content from documents, model text,
  family names and community packages is data, never instruction. *(Golden Rule 19)*
- **Every model change runs in one named `TransactionGroup`** and rolls back completely on failure.
  *(Golden Rule 16)*
- **Generated code never touches a live model on its first run.** *(Golden Rule 18)*
- **No automatic external publishing of private knowledge.** *(Golden Rule 12)*
- **Heron never triggers Sync With Central on its own initiative.** *(Golden Rule 17)*
- **Project knowledge never leaves the machine** without explicit, per-item human review of the
  actual content being sent.

Full detail: [docs/12-security-and-permissions.md](docs/12-security-and-permissions.md) and
[docs/14-golden-rules.md](docs/14-golden-rules.md).

---

## Reporting accidentally committed data

If you find **client project data, a real `.rvt` model, credentials or personal information** committed
to this repository, report it privately and immediately. Do not open a public issue, and do not fork
the repository.

---

## Supported versions

Heron AI is in specification stage. No released version exists yet, so nothing is currently under
a support commitment. This section will list supported versions once releases begin.
