# Phase 2 — how a tool is brought across, one at a time

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — method agreed, nothing ported.** Opened 2026-09-21. **Owner:** Ajmal PS.
> **Read [`05-phases.md`](05-phases.md) first.** This is Phase 2's method, not Phase 2's design.

---

## 1. The rule that comes before everything

> Owner, 2026-09-21: *"do not take any tool until I give the go-ahead … once I specify the tool and give
> you the name, you just take that one tool."*

**One named tool at a time, and only after he names it.** Not a sweep, not a batch, not "while I was in
there". A sweep produces a pile of half-ported tools nobody can check; one tool produces one thing that
either works or does not.

---

## 1a. Two warnings from the owner, and neither is optional

> Owner, 2026-09-21: *"**Not everything in AJ tools.** There are some issues, and some tools don't work
> or aren't good, so don't take those. **I will tell you one by one** which ones we need to take."*

**So a tool being in AJ Tools is not a reason to port it.** The library contains tools that do not work
and tools he does not rate, and only he knows which. §1's rule — one named tool at a time, only when he
names it — **is that warning, and it is why the rule exists** rather than being caution for its own sake.

> Owner, 2026-09-21, on the earlier brain: *"some skills are overwritten. **For the same work, we ended
> up with multiple skills or multiple fragments** — that was the mistake, and that is why we invented
> Heron."*

**So one question comes before any fragment is written: which capability does this claim?**

| Answer | What happens |
|---|---|
| **No owner yet** | a new fragment, a new capability name |
| **Already owned** | **widen the owner** ([§5](#5-what-this-does-to-the-proven-count-and-it-will-look-like-going-backwards)) — never a second fragment for one job |
| **Owned, but this is genuinely a different job** | a **different capability name**, stated plainly. If the name is hard to write, that is the signal it is the same job |

**Heron makes the wrong answer structurally impossible**, which is the point: `capability:` is unique
across the store, and a skill names capabilities rather than fragment ids, so nothing silently points at
a fragment that changed under it. [B9](08-lessons-from-the-brain.md) has the three checkers that watch
it.

**The failure to avoid has a shape, and it looks like diligence:** porting two tools that do one job,
writing a fragment for each, and ending with two answers to one question — which is precisely what the
new library was built to escape.

---

## 2. The loop

```text
  1  He names ONE tool
          |
  2  Study it whole            <- it is long; the point is to see the complete behaviour
          |
  3  Which parts do our fragments already do?
          |
  4  Cut ONLY those parts, wire the fragment in
          |
  5  Does the fragment actually cover what was cut?
          |
     +----+----+
     |         |
    YES        NO
     |         |
  done      6  EDIT THE FRAGMENT to match the tool  -> then re-wire
```

**Step 6 is the whole idea.** The owner's words:

> *"If you delete something and realize that fragment doesn't work, you'll need to edit our fragments to
> match our tools. This means don't delete it completely: at the same time, our fragment will be edited,
> and our tool will be edited as well."*

**"Don't delete it completely" is a working method, not caution.** Keep the tool's original code in
front of you while wiring, so the thing the fragment fails to cover is visible rather than remembered.
Cut only what is demonstrably replaced.

---

## 3. When the tool and the fragment disagree, the tool is probably right

> *"The AJA tools are working ones, not issued ones."*

This is the reasoning that makes the whole method work, and it is sound:

| | Status |
|---|---|
| **The AJ Tools tool** | used daily, in real projects, for years |
| **A Heron fragment** | 327 of 395 `PROVEN`; **68 have never met a model** |

So a disagreement is **information about the fragment**, not a problem with the port. The tool is a
specification of what the job actually needs, written by use rather than by guessing.

### The owner's own example, and what it shows

> *"if we have a color-changing category in our fragments, maybe only one thing changes (like the
> projection line). But in the tools, there might be a lot of things to change."*

A fragment that sets only the projection line is not **wrong** — it is **narrow**. Nobody noticed,
because nobody used it on a real drawing. The tool changes projection lines, surface patterns, cut
lines, transparency, and whatever else the job needs, because a real drawing needed them.

**The fragment gets widened. That is the deliverable of the port, as much as the tool is.**

---

## 4. The direction of the fix, and why it is not negotiable

**Missing logic goes into the FRAGMENT. Never into the tool.**

The owner already stated it this way; it is restated here because it is the one thing that, done
backwards, quietly destroys the point of the whole programme.

```text
   RIGHT                              WRONG
   tool  ->  fragment  ->  Revit      tool -> its own private logic -> Revit
   AI    ->  fragment  ->  Revit      AI   -> the narrow fragment    -> Revit
             ^ one body                      ^ two behaviours, one name
```

If the extra behaviour lives in the tool, then **the button does the full job and the AI still does the
narrow one** — and the fragment is now lying about what it does. [S6](00-structure.md): one body, two
front doors. A tool that holds logic the fragment does not is a front door onto a different building.

---

## 5. What this does to the PROVEN count, and it will look like going backwards

**Editing a fragment marks its proof `STALE`. This is enforced in code, not remembered.**

[D-30](../../../DECISIONS.md) fingerprints a proof against the fragment's own implementation bytes, so
changing the implementation invalidates the proof by construction. `check-docs` prints the list; today
it names two:

```
STALE - signed, then the code changed under it (2)
  tag-elements-in-view              Ajmal PS, 2026-09-14
  force-tag-leader-lshape           Ajmal PS, 2026-09-09
```

**So a session spent widening ten fragments to match a tool ends with a LOWER proven count than it
started.** That is the gate working, not a regression — but it is worth expecting, because it looks
exactly like damage on the day it happens.

**And the owner's method is already the cure**, which is the neat part:

> *"I can also check manually through the tools: if the tool is working, it means the fragment is also
> working, so I won't get any issues."*

Using the widened tool **is** the re-proving. The work that invalidates the proofs is the same work that
regenerates them.

### A `DRAFT` fragment may be used, and using it is how it stops being one

> Owner, 2026-09-21: *"Draft fragments — you can use them for creating tools. Once these draft fragments
> are used and tested, and they are working, it means they are proven. Am I right? … while creating new
> tools, if you see something we can create from those new fragments, you can use those too."*

**Nothing blocks it.** Checked 2026-09-21: there is **no status filter anywhere in the resolution path**
— `heron_brain.py`, the capability registry and the matcher never ask whether a fragment is `PROVEN`
before offering it. So the 68 `DRAFT` fragments are **68 more building blocks available today**, and no
rule has to change for that.

**`DRAFT` does not mean broken. It means nobody has watched it.** Wiring one into a tool is exactly the
watching it has been waiting for.

### But "it worked" is not a proof, and [D-30](../../../DECISIONS.md) exists because of that precise mistake

This is the half of *"am I right?"* that has to be answered honestly, and the reason is not procedural.
D-30's own context records the defect it was written from:

> *"the level chain never tried `RBS_START_LEVEL_PARAM`, so setting a level filter matched **zero**
> ducts **and reported success**. A fragment that succeeds while doing nothing passes ten runs. It
> passes a thousand."*

**A fragment that does nothing looks exactly like a fragment that works.** The tool runs, no error
appears, and the only thing that separates the two is whether anybody checked the *number*.

So a proof needs three things, and the second is the one that does the work:

| | | |
|---|---|---|
| 1 | **Positive case** | it returns what it should — *this is what using the tool already gives you* |
| 2 | **Negative case** | it returns **nothing** where nothing is correct. **D-30: *"a proof without it is not a proof"*** |
| 3 | A second route to the same answer, where one exists | two mechanisms agreeing, or a number checkable by eye |

Plus a named model, a date, and a name — *"whoever ran it records it, under their name, not a tick"*.

### What that means in practice, and it costs one extra click

**Using the tool gives case 1 for free.** Case 2 is one more run, deliberately arranged to find nothing.

Taking the owner's own colour example: run the tool on a view that **has** the category — that is case 1.
Run it on a view that has **none of it** — and it must report zero and change nothing. That is case 2,
and it is the run that would have caught the level-filter defect above.

**Then it is genuinely `PROVEN`**, in Heron's terms and not only in the sense of having been seen to
work. [`fragment-proving`](../../../../.claude/skills/fragment-proving/SKILL.md) and
[`tools/batch-prove.py`](../../../../tools/batch-prove.py) record it.

**Record it as it happens.** Otherwise the library keeps saying `DRAFT` about fragments the owner
personally watched work, and the status stops meaning anything to anybody — including to the debugging
engine ([07](07-debugging-engine.md)), which reads that status to decide what it may fix by itself.

---

## 6. Heron already has the machinery for step 6

Two agents exist for exactly the decision step 6 makes, and **both propose; neither applies**
([docs/09 §6](../../../09-skills-and-fragments.md)):

| | Agent | Answers |
|---|---|---|
| **Widen it** | [`heron_evolve.py`](../../../../brain/heron_evolve.py), `HERON-FRG-EVO-005` | KEEP · UPDATE · EXTEND · SPLIT · MERGE · BRANCH · DEPRECATE · ARCHIVE |
| **Break it in two** | [`heron_split.py`](../../../../brain/heron_split.py), `HERON-FRG-SPL-003` | Only when the extracted part has **at least two real consumers** |

**The colour example is a widen, not a split.** Projection line, surface pattern and cut line are one
job the user thinks of as one action — splitting them would give three fragments and nine layers of
indirection for no reuse. docs/09 §120 states the guard plainly: *reuse justifies a split; tidiness does
not.*

**That both agents only propose is the right shape for this work**, because the decision is the owner's
and he is the one who knows what the tool is for.

---

## 7. What is decided, and what is not

| Decided | |
|---|---|
| One tool at a time, only when named | §1 |
| **`DRAFT` fragments may be used in tools** — nothing blocks it, and using them is how they get proven | §5, owner 2026-09-21 |
| **A new fragment written during one port is available to the next** — reuse, not duplication | Owner, 2026-09-21 |
| **"It worked" is case 1 of 3.** The negative case is still owed | §5, [D-30](../../../DECISIONS.md) |
| Cut only what is demonstrably replaced | §2 |
| Disagreement means the fragment is narrow | §3 |
| Missing logic goes in the fragment, never the tool | §4 |

| Open | |
|---|---|
| Which tool is first | **Waiting on the owner.** Nothing is taken until he names it |
| Whether a widened fragment bumps its version or becomes a new one | `heron_evolve` proposes; not decided |
| How the proving session is recorded as it happens | §5's gap — needs a habit, not a tool |
| Where a ported tool's button lives | `Heron`, `Heron Doc` or `Heron MEP` — per tool, [S1](00-structure.md) |

**Nothing has been taken from AJ Tools. Nothing will be until a tool is named.**
