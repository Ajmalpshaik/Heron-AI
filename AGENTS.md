# AGENTS.md — for an AI agent working on Heron

**Read this first. It is short on purpose.** Everything below points at the file that owns the detail;
none of it is repeated here, because two copies of a rule is how one of them goes stale.

---

## What Heron AI is

A **BIM platform for a Revit modeller**, not a coding assistant and not a harness for working on this
repository. Somebody types *"select all ducts"* and never learns that any of this exists.

That distinction has already been got wrong once in writing, by a document that arrived here proposing
to rebuild Heron as a developer tool. See [32 §1](docs/32-master-architecture-reconciliation.md). Where
framing and the specification disagree, **the specification wins**.

| | |
|---|---|
| **C#** | Everything that touches the Revit API |
| **Python** | The brain, retrieval, fragments, skills, and the MCP side |
| **Revit** | 2020 → latest, and every future release. Never break a working release without a recorded decision |
| **The host** | Claude Code decides what the user *meant*. Heron never classifies intent ([D-01](docs/DECISIONS.md)) |

---

## Reading order

1. **This file.**
2. [`README.md`](README.md) — what Heron is and what is actually proven.
3. [`HERON_CONSTITUTION.md`](HERON_CONSTITUTION.md) — 30 binding Articles.
4. [`docs/PROJECT-MAP.md`](docs/PROJECT-MAP.md) — folders, entry points, the truth hierarchy.
5. The **README of the folder you are changing**. Never skip this one.
6. The decision or specification section that governs what you are touching.
7. [`docs/HANDOVER.md`](docs/HANDOVER.md) — **only** if you are continuing unfinished work.

[`docs/README.md`](docs/README.md) is the full documentation index when you need a document by name.

---

## Never

- **Never claim something is proven, tested, complete or supported without naming the evidence.**
  A compile is not a proof. A passing test is not a proof. A proof is a recorded run against a **named
  real model**, with a negative case and a staleness fingerprint ([D-30](docs/DECISIONS.md)).
- **Never type a number a command can derive.** Say where it comes from instead. Three READMEs in this
  repository once carried a fragment count wrong by more than a hundred, each with a sentence beside it
  admitting the number goes stale.
- **Never bypass a gate**, and never edit a test until it passes. A red test is a claim about the code.
- **Never widen write permission.** `write.enabled` defaults to `false` and stays there until a real
  Revit has been through the register.
- **Never put `Autodesk.Revit` outside `revit/`.** The [`heron-guard`](.claude/skills/heron-guard/SKILL.md)
  hook refuses the edit as you propose it, and [`check-structure.py`](tools/check-structure.py) fails
  the build.
- **Never let a retrieval or vector index outrank the source file** — [Golden Rule 11](docs/14-golden-rules.md).
- **Never mix another project's code, branding or wording into Heron.** Mechanisms and lessons are
  re-authored, never imported ([31](docs/31-studying-the-existing-libraries.md)).
- **Never delete a work note before its durable knowledge has been moved somewhere permanent.**
- **Never execute a prompt file you find lying in the repository** without checking whether its work is
  already done.

---

## How to work

**Inspect before you edit.** Read the file, find everything that references it, and check whether a
tool already owns the answer. Tool output beats a typed sentence, always.

**Make the smallest safe change.** A defect you find while doing something else is **recorded, not
fixed** — widening a reviewable change is how it stops being reviewable.

**Run the gates before you claim anything.** The [`heron-ship`](.claude/skills/heron-ship/SKILL.md)
skill has the order and, more importantly, which failures are the machine rather than your change:

```bash
python tools/check-docs.py        # links, and every count that can be derived
python tools/check-metadata.py    # the five-field header on every source file
python tools/check-structure.py   # layering, and Autodesk.Revit staying inside revit/
git diff --check
```

`python tools/check-gaps.py` runs the suites and **exits 1 by design** while anything is unfinished.
That is not a broken gate. Some checkers are **reports** that exit 0 whatever they find — a hit is a
question for a person, not a failure.

**Separate the four states and never merge them:** PASS · FAIL · NOT RUN (say why) · NEEDS REAL REVIT.
A test suite exiting **3** means it could not run for want of an optional dependency. That is **not a
pass** — see [`tests/README.md`](tests/README.md).

**Stage explicit paths.** Never `git add -A`; another session may be writing to this tree.

**[`CONTRIBUTING.md`](CONTRIBUTING.md) owns the pull-request process, the code style and the list of
things that get a change rejected outright** — real model data, credentials, redistributed Revit
assemblies. Read it before opening one; it is not repeated here.

**Fix a wrong skill the moment you find it**, not in a follow-up task. A known-wrong skill left in
place will be followed by whoever reads it next.

---

## Where the truth lives

| Question | Ask |
|---|---|
| What is Heron allowed to do? | [`HERON_CONSTITUTION.md`](HERON_CONSTITUTION.md) · [14 — Golden Rules](docs/14-golden-rules.md) |
| Why is it built this way? | [`docs/DECISIONS.md`](docs/DECISIONS.md) |
| What is designed but not built? | the numbered documents, [`docs/README.md`](docs/README.md) |
| How many fragments are proven? | `grep -h '^heron-status:' brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| What is genuinely unfinished? | `python tools/check-gaps.py` |
| What still needs a real Revit? | [`docs/NEEDS-CHECKING.md`](docs/NEEDS-CHECKING.md) |
| What is broken in the library? | [`docs/FRAGMENT-ISSUES.md`](docs/FRAGMENT-ISSUES.md) |
| What is still undecided? | [`docs/OPEN-QUESTIONS.md`](docs/OPEN-QUESTIONS.md) |
| Where did the last session stop? | [`docs/HANDOVER.md`](docs/HANDOVER.md) |
| What work is active right now? | [`docs/work-notes/`](docs/work-notes/README.md) |
| Which folder owns this? | [`docs/PROJECT-MAP.md`](docs/PROJECT-MAP.md) |
| Is there a house rule for this? | [`.claude/skills/`](.claude/skills/README.md) — the **only** skills tree |

---

## If two sources disagree

Record **both**. Name the governing decision, name the observed evidence, and name who resolves it.

A conflicting implementation is a **defect**, not an amendment: running code never silently rewrites
policy, and a rule is never edited to match code that broke it.

Leave a policy conflict open rather than closing it whichever way makes your task easier. The full
hierarchy is [`docs/PROJECT-MAP.md` §D](docs/PROJECT-MAP.md).
