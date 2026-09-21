# Heron Plugin Extension — the three phases

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active.** Opened 2026-09-21. **Owner:** Ajmal PS.
> **Phase 1 is planned in this folder. Phase 2 is outlined. Phase 3 has not been told yet.**

---

## 1. Why this file exists

The owner set out a three-phase programme on 2026-09-21. This folder was opened for the installer, and
the installer turns out to be **Phase 1 of three**. This page holds the shape so the later phases are
not planned as if they were unrelated.

**Nothing here is built. Phase 1 is planned; Phase 2 is a paragraph; Phase 3 is a promise.**

---

## 2. The three phases

| | Phase | State |
|---|---|---|
| **1** | **Installation** — the installer, the three routes, the Settings panel | **Planned in this folder.** Nothing built |
| **2** | **AI that fixes and AI that builds** — bring AJ AI's debugging and tool-creation across, then port the AJ Tools tools onto Heron fragments | **Outlined below.** Not planned |
| **3** | Not told yet | The owner will walk through it. **Some of it reaches back into Phase 2**, so Phase 2 is not designed in detail until Phase 3 is heard |

**Phase 3 reaching back into Phase 2 is the instruction that matters here**, and it is the reason this
page does not plan Phase 2 further than the owner's own words. Designing Phase 2 fully now would mean
designing it twice.

---

## 3. Phase 1 — installation

Everything in this folder. Three constraints the owner stated on 2026-09-21:

1. **The manual installation is still needed.** It is not replaced by the AI routes
   ([R-1](01-requirements.md)).
2. **Nothing is mixed together, and no mistakes.** Each route stays its own front door; the rules stay
   in one engine ([S7](00-structure.md), [R-31](01-requirements.md)).
3. **Whatever the manual installation handles, the other routes handle too.** Not a subset — the same
   rules, the same refusals, the same reports.

**Constraint 3 is already the architecture**, which is why it costs nothing: the engine owns every rule
and the three routes only decide *what* to install. It is also the thing
[Stage 6's closing test](02-implementation.md) exists to prove — break one rule once, and all three
routes must fail together. If they do not, they are three engines wearing one name.

---

## 4. Phase 2 — as the owner described it

> Owner, 2026-09-21: *"In the AJAI tool, there is an AI connector as well as a debugging option. I
> previously developed a feature where if I run into any error in the AJAI tool, I can just chat through
> the AI and it will fix it automatically. There is also new tool creation: I can chat and create
> things. We need to bring that exact same idea and setup from there over to here."*

**`Ajmalpshaik/AJ-AI-Brain` has NOT been read.** `AJ-Tools-Installer` was read on 2026-09-21
([04](04-lessons-from-aj-tools.md)) and that is all. The brain repository is where the chat-to-fix and
chat-to-create behaviour actually lives, so **it is the next thing to read**, and nothing about how
those two work should be designed before it is. Recorded here so an unread repository is not later
mistaken for a studied one.

Two capabilities to bring across from AJ AI / AJ AI Brain:

| | What it does |
|---|---|
| **Chat-to-fix** | An error in a tool becomes a conversation, and the AI repairs it |
| **Chat-to-create** | A new tool is described in words and built |

And the larger job they serve: **port the working AJ Tools tools onto Heron's fragments.**

### 4a. The porting rule, in the owner's words

> 1. Copy the tool entirely.
> 2. Delete the unnecessary parts based on the fragments we have.
> 3. Wire these fragments to the tools.

> *"The reason I told you to copy from there is just to get the complete idea of the tools, because this
> tool is already working. I was successfully using it for Revit 2020, so there is almost no chance of
> mistakes in the logic itself."*

**Step 1 means start from the whole working tool so its complete behaviour is visible** — not paste it
in and keep it. Steps 2 and 3 are the rewrite. Read that way it is the same instruction he gave on
2026-08-28, already recorded in [31](../../../31-studying-the-existing-libraries.md):

> *"You can check and study everything, but don't copy-paste blindly. Check, study, and take what you
> need… 1. Check and edit. 2. If you want to add, add. 3. If you want to split, split."*

**So there is no conflict with [D-25](../../../DECISIONS.md) — studied and re-authored, never
imported — provided step 1 stays a reading step.** What lands in Heron is re-authored. What is copied
is the understanding.

### 4b. Why port at all, rather than write fresh

The owner's reasoning, and it is the strongest argument in the whole programme:

> *"At the same time, we can fix the fragments and use them to run the tool. That way, if there is any
> mistake in the fragments, we will understand it immediately. I can use these tools, and the AI can use
> them as well; while the AI is using them, if the tools are solid, it will not make mistakes."*

**A tool built on fragments is a test of those fragments that someone actually wants to run.** 327 of
395 fragments are `PROVEN` and the rest have never met a model — and a fragment nobody calls is a
fragment nobody finds the fault in. Wiring a real tool to them turns daily use into proving.

**This is [S6](00-structure.md) arrived at from the other direction.** S6 says a ribbon button should
call the same fragment body the AI calls — one body, two front doors. The owner's reason for porting is
that *both* doors then exercise the same body, so a fault shows up whoever opened it.

### 4c. The risk the owner named himself, and it is the right one

> *"The only potential issues might be Revit versions and .NET versions for newer releases, like Revit
> 2024 to 2027."*

**Correct, and Heron already owns the tools to catch it.** Code written and proven against Revit 2020
meets four breaking changes on the way to 2027 —
[`revit-version-support`](../../../../.claude/skills/revit-version-support/SKILL.md) and
[16](../../../16-version-support-strategy.md) name them, and
[`tools/check-api-surface.py`](../../../../tools/check-api-surface.py) answers whether every member a
piece of code calls exists in every release it claims. That check needs a .NET SDK, which the owner's
machine has and this container does not.

**So the logic ports and the API surface does not.** That is the split to expect, and it is a far better
problem than the reverse.

### 4d. The one thing that will surprise, and it is enforced in code

**A ported tool's fragments arrive `DRAFT`, however long the original worked.**
[D-44](../../../DECISIONS.md), closed by the owner himself on 2026-08-29:

> *"Even in the AJ AI proven fragment, don't mark in Heron this is proven, because we will check each
> and every one again in Heron AI. So mark it as not proven in Heron."*

**It is not a convention to be remembered — it cannot be bypassed.** `proof_problems` in
[`brain/heron_fragment.py`](../../../../brain/heron_fragment.py) refuses `PROVEN` unless the proof's
fingerprint matches the fragment's own implementation bytes, so a proof carried from elsewhere can never
match.

**This serves the owner's own stated goal rather than fighting it.** He wants mistakes in the fragments
to surface immediately. A fragment that arrived already marked `PROVEN` is the one nobody would check.

---

## 5. What is deliberately not decided here

| Not decided | Why |
|---|---|
| How chat-to-fix works in Heron | Phase 2, and Phase 3 reaches back into it |
| How chat-to-create works in Heron | Same |
| Which AJ Tools tools port first, and in what order | Needs the tools read; not read yet |
| Whether a ported tool becomes a fragment, a skill, or both | [09](../../../09-skills-and-fragments.md) owns that distinction; the answer is per tool |

**None of it blocks Phase 1**, which is the whole point of the ordering.
