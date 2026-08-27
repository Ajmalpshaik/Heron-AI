# Contributing to Heron AI

Thank you for considering it. Heron AI is a platform for BIM professionals, and contributions from
people who actually do BIM work are worth more than contributions from people who only write code.

> **Current stage: specification and planning.** There is no implementation yet, so the most useful
> contributions right now are to the architecture — see [Open Questions](docs/OPEN-QUESTIONS.md).

---

## Before you start

Read these three, in order:

1. **[docs/14-golden-rules.md](docs/14-golden-rules.md)** — the non-negotiables. Anything that
   contradicts a Golden Rule will be declined, however good it is otherwise.
2. **[docs/README.md](docs/README.md)** — the documentation index.
3. **[docs/DECISIONS.md](docs/DECISIONS.md)** — what has already been decided and why. If you disagree
   with a decision, open a discussion about *that decision* rather than a PR that quietly works around it.

---

## Ways to contribute

### Architecture and design

The [open questions](docs/OPEN-QUESTIONS.md) are genuinely open. If you have run into these problems
before — Revit API threading, multi-version support, RAG over technical corpora — your experience is
valuable. Open an issue or a discussion.

### Fragments and skills

The most valuable long-term contribution. A **fragment** is a reusable implementation unit;
a **skill** is a user-facing capability. See [docs/09-skills-and-fragments.md](docs/09-skills-and-fragments.md).

Every contributed fragment must include:

- [ ] Complete, schema-valid metadata
- [ ] A stated purpose, inputs and expected output
- [ ] **Declared and tested** Revit version compatibility — declared alone is not enough
- [ ] Tests that pass on at least one .NET Framework and one .NET 8 Revit version
- [ ] A declared risk level
- [ ] No duplicate of an existing fragment (search first — Golden Rule 3)

Contributed fragments enter the lifecycle at `DISCOVERED`. Nothing becomes production knowledge because
a contributor said it works (Golden Rule 6).

### Bug reports

Include the Revit version, the Heron version, what you asked for, what happened, and — if a model was
modified — whether undo restored it correctly. That last one matters more than it sounds.

### Documentation

Corrections and clarifications are always welcome. Note that
[docs/00-master-specification.md](docs/00-master-specification.md) is a **historical record** and is not
edited to "fix" it — changes to intent are recorded as decisions instead.

---

## Absolute rules

These will get a pull request closed immediately, without review:

| Never commit | Why |
|---|---|
| **Real client models or project data** | Confidentiality. Permanent once pushed — forks and caches propagate. |
| **Real `.rvt` files from live projects** | Same. Test models must be synthetic. |
| **Credentials, API keys, tokens** | Anything committed to a public repository is compromised forever. |
| **Personal data of any kind** | Yours or anyone else's. |
| **Redistributed `RevitAPI.dll` / `RevitAPIUI.dll`** | Autodesk's, not redistributable. Reference them with *Copy Local = false*. |

If you commit any of these by accident, **report it privately** via
[SECURITY.md](SECURITY.md) rather than force-pushing over it.

---

## Pull request process

1. **Open an issue first** for anything beyond a small fix. It saves you writing something that will be
   declined for architectural reasons.
2. **One concern per pull request.** Mirrors Golden Rule 2 — one responsibility.
3. **Explain the "why", not just the "what".** The reasoning is what gets reviewed.
4. **Never break a working version** (Golden Rule 4). If your change affects Revit version support,
   say which versions you tested and how.
5. **Tests, written to fail before your fix.**
6. **Be ready to discuss.** Review here is about design fit, not gatekeeping.

---

## Code style

To be defined once implementation starts. Until then:

- **C#** — standard .NET conventions. Nothing touching Revit outside the add-in.
- **Python** — standard formatting and type hints. Nothing touching the Revit API.
- Version-conditional code lives **only in adapters**, never in core logic
  ([docs/16-version-support-strategy.md](docs/16-version-support-strategy.md)).

---

## A note on scale

The [agent catalogue](docs/08-agent-catalog.md) lists ~150 agents. That is a **target organisation
chart**, not a to-do list. Please do not open a pull request implementing forty of them.

The project is built one working vertical slice at a time — see [docs/ROADMAP.md](docs/ROADMAP.md).
A contribution that makes one real thing work end-to-end is worth more than one that adds ten
components nobody has exercised.
