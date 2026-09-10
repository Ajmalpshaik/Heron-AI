# Contributing to Heron AI

Thank you for considering it. Heron AI is a platform for BIM professionals, and contributions from
people who actually do BIM work are worth more than contributions from people who only write code.

> **Current stage: built ahead of what is proven, though less far than it was.** Phase 0 — the whole
> path from a sentence to a selection changing on screen — is proven in real Revit 2020 and 2024. The
> add-in has since been deployed to Revit 2020, 2024 and 2027, and D-28's executor compiles a
> fragment's C# inside Revit's own process, so fragments do now meet real models: **167 of the 360
> carry a recorded proof, 55 of them on the write path.** **All ten skills are still `DRAFT`**, and
> 193 fragments have still never met a model.
>
> Derive both numbers rather than believing this line —
> `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`.
>
> So the most useful contributions are **proof** rather than more code, and the architecture questions
> that are genuinely still open — see [Open Questions](docs/OPEN-QUESTIONS.md).
> `python tools/check-gaps.py` is computed from disk on every run and prints what is truly unfinished
> separately from what is only waiting on a machine with Revit on it. Where it and any sentence in
> this repository disagree, the tool is right.

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
6. **Run the checks.** These three need nothing but Python 3 — no Revit, no Windows, no .NET SDK —
   and all three must exit 0:

   ```
   python tools/check-structure.py    # parts in place, no layering violation
   python tools/check-docs.py         # every link and every stated count
   python tools/check-metadata.py     # headers, and the registry against the code
   ```

   `python tools/check-gaps.py` exits non-zero while real work is outstanding, which is its job — read
   it, but do not expect a 0. [tools/README.md](tools/README.md) explains the rest.
7. **Be ready to discuss.** Review here is about design fit, not gatekeeping.

---

## Code style

Most of this is enforced by a script rather than left to review, so run the checks before you open a
pull request — see below.

- **C#** — standard .NET conventions. `Autodesk.Revit` types may appear **only inside `revit/`**;
  `check-structure.py` fails the moment one appears anywhere else, because the brain has to stay
  runnable and testable on a machine with no Revit on it.
- **Python** — standard formatting and type hints. Nothing touching the Revit API.
- **Every `.cs`, `.py` and `.ps1` file carries the five-field Heron header** — agent, step, status,
  since, layer ([docs/29-metadata-standard.md](docs/29-metadata-standard.md)). `check-metadata.py`
  rejects a missing or malformed one, and checks the agent id against the registry in both
  directions. Fragments under `brain/fragments/` are the exception: their metadata lives in their
  own `fragment.yaml`, so that one fact has one home.
- Version-conditional code lives **only in adapters**, never in core logic
  ([docs/16-version-support-strategy.md](docs/16-version-support-strategy.md)).

---

## A note on scale

The [agent registry](docs/28-agent-registry.md) lists **250 agents**, of which about 70 are built.
That is a **target organisation chart**, not a to-do list — `python tools/agent-count.py` reconciles
it against the code and prints what is genuinely left. Please do not open a pull request implementing
forty of them.

The project is built one working vertical slice at a time — see [docs/ROADMAP.md](docs/ROADMAP.md).
A contribution that makes one real thing work end-to-end is worth more than one that adds ten
components nobody has exercised.
