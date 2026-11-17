# FRAGMENT-ISSUES archive — section 5b, rows 851 to 875

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 851 to 875 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-861

*Moved from the register on 2026-11-17.*

**Defect.** **EVERY OTHER TOOL THAT MAKES A COPY PUTS IT ON THE ACTIVE WORKSET TOO, AND NONE CAN BE TOLD OTHERWISE - `COPY_ELEMENTS`' gap ([row 860](../FRAGMENT-ISSUES.md#)) in seven more cards.** Found 2026-10-17 by reading, while widening `COPY_ELEMENTS`: no implementation below names a workset (`grep -ci workset` is 0 in each `impl/`), and each makes its new elements with a call Revit puts on the ACTIVE workset. `MIRROR_ELEMENTS` (PROVEN) with `asCopy: true` - `ElementTransformUtils.MirrorElements(doc, ids, plane, true)`. `ARRAY_ELEMENTS` (PROVEN) - `CopyElement` per step. `ARRAY_ELEMENTS_RADIAL` (PROVEN) - `CopyElements`, then a turn. `SET_DESIGN_OPTION` (DRAFT) - copies into an option with `CopyElements(doc, ids, XYZ.Zero)`. `COPY_FROM_LINK` and `COPY_ELEMENTS_BETWEEN_DOCUMENTS` (both DRAFT) - into this model from another document, where "the original's own workset" means nothing (the link's worksets are not this model's) but a NAMED workset would. `PLACE_MODEL_GROUPS` (DRAFT) - `Create.PlaceGroup`, a new group on the active workset; a named workset applies, "source" does not. **No gap:** `MOVE_ELEMENTS`, `MOVE_ELEMENTS_TO_POINT`, `MIRROR_ELEMENTS` without `asCopy` and `OFFSET_ELEMENTS` make nothing new - an element keeps its workset when it moves. Not run in Revit; read off the code only.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed** (the brief that built row 540 said record, not fix). Each is the same widening as `COPY_ELEMENTS` version 3: `toWorkset`, `optional: true` with `absent: active`, read back into a string of its own - and each PROVEN one goes back to DRAFT when its `impl/` moves. Which of them is worth it, and in what order, is the owner's call; the mirror and the two arrays are the ones a modeller copying openings or fixtures would meet first.

---
