# The eleventh session, 2026-08-31 — seven more, and a retraction

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **46 to 53**, and **withdrew a finding the previous session
reported.** All 53 compile on all eight releases; all 53 are `DRAFT`.

### The retraction, first, because it was told to the owner as fact

The tenth session added a check for *a question answered by something that writes*, said it **found six
and that four were pre-existing library defects**, and named them. **All six were false.** Nothing in the
library was wrong. The check was.

**How it was wrong, in two stages, and the second is the instructive one.**

| | |
|---|---|
| **First** | It measured `taken` — the **keyword ranking**. Its own printed claim was *"a caller acting on the top hit"*, and no caller uses the keyword route alone |
| **Then** | Corrected to `retrieve` — the fused stage. **Still wrong, and far less obviously.** The host calls `find`, which tries the **identity** and cache short circuits *first*. `retrieve` is fusion alone |
| **Why that hid it** | `check-routing` asks every fragment **its own declared utterances** — and those are exactly the sentences `find` answers by identity, *before any ranking runs*. So the check was ranking sentences the host never ranks |

Verified directly: all six resolve to their own fragment on route `identity`.

> **The rule this leaves.** A check that makes a claim about **consequence** must call the same entry
> point the system calls — not the stage that looks like it. A per-route diagnostic may report any route
> it likes; it may not describe one route's ranking as what the system does. The section above it has
> always said which route it measures, and was right to.

**The check is kept, now calling `find`, and it prints its own limit rather than a bare green.** It is
empty **by construction** while the identity route holds, and its emptiness says the identity route
works — not that no question can reach a writer. **The real risk surface is paraphrase**, which no
fragment declares and this corpus therefore does not contain. What it still catches: a declared question
that stops matching by identity, or two fragments declaring one sentence where the survivor writes.

**One thing from that session survives on its own merits** — `TAG_ELEMENTS` really was carrying a
composition sentence that belonged at the step it starts from, and moving it was right for reasons that
have nothing to do with the faulty check.

### The seven

| | |
|---|---|
| `FIND_CLASHES` | Element-against-element, **solids not bounding boxes** — a box around a diagonal duct overlaps everything in the rectangle it spans, which is how a clash report grows to hundreds of rows nobody reads. **Not `CHECK_OBSTRUCTIONS`**, which casts a ray from a *point* before anything is modelled; the two are asked for in the same words |
| `GROUP_ELEMENTS` | Reads the member count back **from the group**, not from the input. Creating a group creates the condition that makes `MOVE_ELEMENTS` report blocked |
| `UNGROUP_ELEMENTS` | Hands the released members back as `elements`, so *ungroup then move* composes. Grouped, pinned and owned all present as *"it will not move"* and have three different fixes |
| `READ_ELEMENT_PHASE` | **Both** phases. Created-in-Existing and created-in-Existing-**and-demolished** read identically until the second is asked for, and they are opposite instructions on site |
| `SET_ELEMENT_WORKSET` | `worksetId` is an **int**, deliberately — a `WorksetId` is not an `ElementId` and was untouched by 2024's 64-bit change |
| `APPLY_VIEW_TEMPLATE` | A template **overrules** hand-applied graphics rather than deleting them, silently. That is the conflict the grayout job has to route around |
| `DUPLICATE_VIEW` | The option has **no default**: `AsDependent` is not a copy in the sense anybody means, and picking it by accident produces a view that mysteriously refuses changes later |

**No new routing problems.** The seven introduced none, and the risk-direction section is empty for the
right reason. Contested sentences: **21 of 282**, all judgements.

**What none of this is.** Not one of the seven has met a model. `check-gaps` counts **53** fragments
below `PROVEN`.

---
