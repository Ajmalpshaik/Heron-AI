# Heron Plugin Extension — the plan folder

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — nothing built from it yet.** Opened 2026-09-20.
> **Owner:** Ajmal PS.

---

## What this folder is

> **This folder is Phase 1 of three** — see [`05-phases.md`](05-phases.md). Phase 2 brings AJ AI's
> chat-to-fix and chat-to-create across and ports the AJ Tools tools onto Heron fragments; Phase 3 has
> not been told yet and reaches back into Phase 2, so Phase 2 is not designed in detail until it is.


Heron today is **one tab with three buttons**. This folder plans the step from that to **several Heron
products**, each its own Revit tab, and **one installer window** that installs whichever ones the user
ticks.

It exists so that a build can start without re-reading nine documents and guessing which sentence still
applies — and so that an AI picking this up **reads facts instead of inventing them**.

## Read in this order

| | |
|---|---|
| [`00-structure.md`](00-structure.md) | **What shape this is.** The nine structural decisions. **Read first — structure decides the requirements** |
| [`01-requirements.md`](01-requirements.md) | **What the installer must do.** One row per requirement, each with a source |
| [`02-implementation.md`](02-implementation.md) | **How it gets built.** Ten stages, and what proves each one |
| [`03-open-questions.md`](03-open-questions.md) | **What is not decided**, and the parking space for new ideas. Nine open, two answered on 2026-09-21 |
| [`04-lessons-from-aj-tools.md`](04-lessons-from-aj-tools.md) | **What AJ Tools' installer already learned the hard way.** Eight lessons, three of which corrected a row written the day before |
| [`05-phases.md`](05-phases.md) | **Where this folder sits.** The installer is Phase 1 of three. Phase 2 outlined, Phase 3 not told yet |
| [`06-porting-method.md`](06-porting-method.md) | **How one AJ Tools tool is brought across**, and why widening the fragment is the deliverable. Nothing taken yet |
| [`07-debugging-engine.md`](07-debugging-engine.md) | **The debugging engine survey.** Two-thirds of it already exists; Heron repairs situations, not code, and that is the gap |
| [`08-lessons-from-the-brain.md`](08-lessons-from-the-brain.md) | **What the earlier brain already learned.** Eight lessons; three Heron already has, and one answers an open question |

## The three rules of this folder

1. **Every claim carries its evidence.** A file path, a decision number, or a command that derives it.
   A sentence with no source is somebody's opinion and does not belong here.
2. **DECIDED, OPEN and ASSUMED are different words and are never mixed.** An assumption written as a
   decision is how a build goes wrong quietly.
3. **Measured numbers are stamped with the day they were measured**, and the command that derives them
   is printed next to them. They move; the command does not.

## Reviewed end to end

**2026-09-21** — the owner asked for the whole folder to be checked against everything he had said.
What that pass found and fixed:

| | |
|---|---|
| Routes were still called **A, B, C** in five files | renamed 1, 2, 3 — and **R-28/R-29 still described the superseded "cloud" and "setup file" version** |
| **Install All / Custom Install** was only in the brainstorm | now [R-36a, R-36b](01-requirements.md) |
| `AJ-AI-Brain` looked studied | it has **not been read** — said so plainly in [`05-phases.md`](05-phases.md) |
| The debugging engine's scope read *"fragments only"* | the owner widened it to **everything**; [Q-DE-2](07-debugging-engine.md) answered, with the permission split Golden Rule 13 requires |
| Counts here were stale | corrected, and derived with the commands in each file rather than typed |

**A review that found nothing would have been the suspicious result.**

## Closure condition

Every stage in [`02-implementation.md`](02-implementation.md) is either **DONE with evidence** or
**withdrawn with a reason**. The durable part then moves to
[`docs/07`](../../../07-installation-and-update.md) and
[`docs/06`](../../../06-heron-platform.md), the decisions move to
[DECISIONS.md](../../../DECISIONS.md), and **this folder is deleted.**
