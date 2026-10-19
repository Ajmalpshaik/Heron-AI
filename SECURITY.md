# Security Policy

Heron AI executes code inside Autodesk Revit and can modify live project models.
Security issues here are not abstract — they can damage a real project someone is about to deliver.
Please treat them accordingly.

---

## Reporting a vulnerability

**Do not open a public issue for a security vulnerability.**

Report privately through **[GitHub Security Advisories](https://github.com/Ajmalpshaik/Heron-AI/security/advisories/new)**.

If that is unavailable, contact the maintainer, [@Ajmalpshaik](https://github.com/Ajmalpshaik), directly
and mark the message as a security report.

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

- **The permission gate lives in the Revit add-in**, not in the AI layer
  ([`HeronPermissions`](platform/Heron.Core/HeronPermissions.cs)). Every operation declares its risk in
  code; one that changes a model is refused while `write.enabled` (the ribbon's **Changes**) is off,
  which is the default, and an `ADMIN` or `PUBLISH` operation also needs a switch of its own, each off by
  default ([D-106](docs/DECISIONS.md)). With Changes on, a change asked for in a chat is kept at once,
  as one entry in Revit's undo list, unless the owner has turned **Ask to Keep** on, when Revit shows the
  change and asks Keep or Discard first ([D-126](docs/DECISIONS.md), [D-138](docs/DECISIONS.md) - off by
  default). A fragment with no standing proof is refused before anything reaches Revit - by the bridge
  client on the chat's side, because the add-in never sees a fragment's card
  ([D-117](docs/DECISIONS.md), `unproven_refusal` in
  [`heron_bridge_client.py`](mcp/client/heron_bridge_client.py)). Only a person in Revit sets a switch
  or answers the box; nothing in a request can. *(The specification,
  [12](docs/12-security-and-permissions.md), names a `REQUIRES_CONFIRMATION` reply; no code returns
  that word.)*
- **No text Heron reads may raise Heron's own permission level.** Content from documents, model text,
  family names and community packages is data, never instruction. *(Golden Rule 19)*
- **Every model change runs in one named `TransactionGroup`** and rolls back completely on failure.
  *(Golden Rule 16)*
- **Generated code never touches a live model on its first run** - with one recorded exception: a
  `DRAFT` fragment may run once on the saved model in front, as one undo step held behind Revit's Keep or
  Discard box, which is asked every time, and nothing it did stays unless the modeller presses Keep
  ([D-119](docs/DECISIONS.md)). *(Golden Rule 18)*
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

One release exists: [v0.1.0](https://github.com/Ajmalpshaik/Heron-AI/releases/tag/v0.1.0), an early,
unsigned build published so a teammate could install Heron
([WHAT-TO-INSTALL](docs/WHAT-TO-INSTALL.md)). Heron is not yet announced, and **no version is under a
support commitment**: a fix lands on `main`, and reaches a modeller with whichever release follows it.
This section will list supported versions once that changes.
