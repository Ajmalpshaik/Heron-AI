# The twenty-third session, 2026-09-02 — eight more, and one written then withdrawn

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **162 to 170**. Same container, same limitation. `A9` now names
**thirty-two**.

| | |
|---|---|
| `CREATE_WALL` | *"make a wall here"* was answering `SPLIT_MEP_RUN`. A run of points like the duct and the pipe; project elevation again; **nothing is joined**, and the height is unconnected |
| `CREATE_MATERIAL` | *"make a material"* was answering `READ_ELEMENT_MATERIAL`. **Shading colour only** — no appearance asset — so it schedules and colours correctly and renders as flat plastic, which is said rather than discovered |
| `REPORT_VIEW_FILTERS` | *"report what filters are on this view"* was answering `APPLY_VIEW_FILTER` — **a write, to a question about a drawing**, the worst shape of mis-route here. A filter on NO view is the row worth finding. Every probe sits in its own guard, because the project browser comes back from the same collector and throws |
| `SET_DATUM_BUBBLES` | *"the grid bubbles are on the wrong side"* was answering `SNAP_TO_GRID` — which MOVES elements. **Flip is not one call**: read both ends, hide one, show the other, and the two ambiguous cases are decided rather than silently skipped. Bubbles are per view; extents are not, and confusing them is how one fix becomes twenty |
| `CREATE_FILLED_REGION` | *"make a filled region here"* was answering `CREATE_CALLOUT`. **It is annotation**: in one view, in no schedule, not in the model — right for a markup, and a hole in the coordination model for anybody who thinks they modelled something |
| `CREATE_KEY_SCHEDULE` | *"make a key schedule"* was answering `CREATE_SCHEDULE`. A table of DEFINITIONS, not a list of things. Revit is ASKED whether the category allows one, because the create call throws rather than explaining |
| `FIND_OVERLAPPING_LINES` | *"find the overlapping lines"* was answering `FIND_OVERLAPPING_TAGS`. **"Are these two lines the same" is not a direct comparison** — normalise the direction into one half-plane, round both parts of the key, then compare as 1-D intervals. Skip the first step and the check "finds nothing" on a drawing full of duplicates |
| `COPY_FROM_LINK` | *"take the elements out of the link"* was answering `RELOAD_LINKS`. **The link's transform is the whole job** — without it everything lands somewhere plausible, which is worse than obviously wrong. And the copies stop tracking the link, which is right for grids and a trap for anything still changing |

### One fragment was written and then withdrawn the same hour

**`ADD_NAME_AFFIX` was built, validated, and deleted** — because `EDIT_TEXT_VALUES` already does prefix,
suffix and replace, on any parameter *or* on the element's own name. Its purpose even records that the
earlier library carried a rename fragment and a prefix/suffix fragment with the same four
transformations, and that this one deliberately merged them. **The duplicate re-created exactly the
duplication a previous session had removed on purpose.**

> **How it happened, because the mechanism matters more than the fragment.** The brain was asked
> *"add a prefix to all the names"* and answered `ADD_SCHEDULE_FIELDS`, `EDIT_TEXT_VALUES`,
> `RENAME_ELEMENTS`. **`EDIT_TEXT_VALUES` was at #2 and was not read.** This repository's own standing
> rule is *read the top three to five, never just #1* — written for exactly this, and the wrong answer
> at #1 made the list look like a gap when the answer was one line below it.
>
> **A lookup can show what is missing. It cannot show what already exists under a different name** —
> only reading the candidates does that.

And it took the sentence with it while it existed: declaring an utterance HANDS that sentence to the
new fragment by identity, ahead of whatever answered it before. Withdrawn, *"add a prefix to all the
names"* returns to `EDIT_TEXT_VALUES`, where it belongs. A row there now records the whole episode.

### The routing checker's store went stale, and reported a good fragment as unrankable

After the withdrawal, `check-routing.py` reported the replacement fragment as `#None` on **its own
declared utterances** — not ranked anywhere. Nothing was wrong with it: the store still held the
deleted fragment and had not indexed the new one.

> **Delete a fragment, then rebuild the index from scratch before believing any routing number.** A
> session that trusted that output would have rewritten a fragment that was already correct — the same
> shape as the collision list misleading two sessions in a row, and worth the same warning.

### The routing, measured against the 162 that were there before

| | before | after |
|---|---|---|
| Utterances | 946 | 986 |
| Claimed in a routing table and not reached | 3 | **3** — the same three |
| Shortlist collisions | 148 (15.6%) | 152 (15.4%) |

The rate went slightly DOWN this time, having risen last session. Every new fragment's sentences
resolve correctly through `heron_brain.lookup`. Two stale claims were fixed on the way: one of my own
wrapped across two lines *again* — the third time that mistake has appeared, and it is now the first
thing to check in a new table — and `REPLACE_MATERIAL`'s table claimed *"the client changed the spec"*
while declaring *"the client changed the material"*, which only broke once the ranking shifted.

Reciprocal rows went into twenty-three counterpart fragments.

---
