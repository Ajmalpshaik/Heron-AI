# Session note — THE CATEGORY-OVERRIDE READ-BACK COULD NOT SHOW THE FILL

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE CATEGORY-OVERRIDE READ-BACK COULD NOT SHOW THE FILL

A Windows session, Revit 2024 open. `REPORT_CATEGORY_OVERRIDES` (FRG-VIEW-051) is the read half of
`SET_CATEGORY_GRAPHICS` and `SET_CATEGORY_SOLID_FILL`, and after a solid fill on Walls it answered
`Walls: line RGB(0,255,0), cut line RGB(0,255,0), surface RGB...` — a reply cuts every list entry at
sixty characters, and the fill starts past the sixtieth. It never read the cut pattern at all.

- **Fixed in fragment source only**, so no add-in rebuild and no restart. A new `string` output,
  `overrideList`, carries every overridden category's line in full, and the cut pattern and its colour
  are read beside the surface pair. `findings` is unchanged. The MCP tool prints the new key with no
  Python change — measured through `revit_change` itself.
- **Re-proved** on `Project4` (Revit 2024, session 37184), fingerprint `140301fac35c76c9`. Positive: a
  solid fill on Walls in `FloorPlan: Level 1` reads back `... surface RGB(255,0,0), surface pattern
  <Solid fill>, cut RGB(255,0,0), cut pattern <Solid fill>`. Negative: the same fill on Ducts, which
  that view does not show, reads back empty. **Both legs were rolled back** — that view is the owner's
  house plan — and the read-back afterwards found nothing kept. **Signed as Ajmal PS on his instruction
  to re-prove, and worth a spot-check.** Compiles on 2020–2027.
- **Four register rows** came out of it, placed by the FRAGMENT-ISSUES split rather than by this
  branch: [row 173](../FRAGMENT-ISSUES.md) this defect (FIXED), row 174 the solid-fill proof's sentence
  against the code (OPEN, the owner's), row 175 Revit keeping one setting of four on Ducts while the
  write says `overridden 1` (OPEN), and row 5b-154 `READ_GRAPHIC_OVERRIDES` with the same blindness
  (OPEN).

**The balance — what is left, and whose it is:**

| | What | Whose |
|---|---|---|
| 1 | Merge the pull request for this branch | owner |
| 2 | The solid-fill proof's sentence says the read-back cannot tell a stamped pattern from none; the code and a run say the sixty-character cut hid it. Annotate it, re-take it, or leave it - it is signed in your name (row 174) | owner |
| 3 | The by-hand second route: Visibility/Graphics on a view where a solid fill can be KEPT, with Walls reading Solid fill on surface and cut | next Revit session |
| 4 | Make `SET_CATEGORY_GRAPHICS` read back what landed, per category, instead of counting the call (row 175) | fragment, small |
| 5 | The same repair for `READ_GRAPHIC_OVERRIDES`, then a re-proof with a per-element pattern override (row 5b-154) | fragment, small |
