# The thirteenth session, 2026-09-01 — seven more, and the question that costs an hour

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **60 to 66**, plus one. All 66 compile on all eight releases; all
66 are `DRAFT`.

| | |
|---|---|
| `DIAGNOSE_VISIBILITY` | ***"Why can I not see my ducts"* — the most expensive question in Revit**, because there are at least eight unrelated answers and no one place to look. **It asks the ground truth first**: a collector scoped to the view returns what the view actually contains, so everything after is explaining a fact rather than predicting one. **It reports EVERY cause, not the first** — two at once is routine, and fixing one leaves it still invisible. And it **names what it did not read** rather than inventing the likeliest, because an invented cause reads exactly like a found one |
| `SET_VIEW_RANGE` | The commonest of those causes. **Reports the range before changing it, always** — setting one blind is how a coordinated drawing starts showing the storey above, and the old numbers are the way back |
| `SET_VIEW_SCALE` | Changing scale changes every tag and dimension's apparent size. A template usually **owns** scale, so a batch that refuses is pointing at the template, not the views |
| `FIND_VIEWS_SHOWING_ELEMENT` | **Ask before deleting.** An element on four issued sheets is a different decision from one on none, and Revit will not warn. Genuinely expensive — one collector pass per view, because Revit keeps no reverse index — and it says so rather than pretending otherwise |
| `ARRAY_ELEMENTS` | **The direction is normalised**, so a vector handed in between two points 12 m apart does not silently multiply the pitch twelvefold. Count includes the original, as Revit's own array does. Plain copies, not a linked Array element — which changes what every later edit does |
| `SET_ELEMENT_LEVEL` | **The one with the trap, and the trap is the point.** Height is an OFFSET from the level, so changing the level and leaving the offset makes a duct at 2800 above Level 1 into one 2800 above Level 2 — three metres higher, silently, found when somebody sections through it. The offset is recomputed and **the absolute height verified afterwards** |

### `DIAGNOSE_VISIBILITY` and `FIND_VIEWS_SHOWING_ELEMENT` are the same read asked two ways

One view, many causes — or one element, many views. Worth knowing together, because the second is what
turns *"can I delete this"* into an answer.

### An invented agent id, caught before the tool caught it

`ARRAY_ELEMENTS` was first written claiming `HERON-REVIT-GEO-007`, **which does not exist** — there is no
GEO agent in the registry, and every other geometry fragment claims `HERON-REVIT-ELE-010`. It was found
by checking rather than by assuming, which is the third invented agent id this repository has seen and
the first not to reach a checker.

**And `SET_VIEW_CROP`'s guard from the last session has a sibling worth noting**: two of this batch's
fragments (`SET_VIEW_RANGE`, `SET_VIEW_SCALE`) take an `apply` flag so the same fragment answers *"what
is it"* and *"change it"*. That is deliberate — the read is what makes the write reversible, and
splitting them would let somebody set a value without ever recording what it was.

**What none of this is.** Not one has met a model. `check-gaps` counts **66** fragments below `PROVEN`.

---
