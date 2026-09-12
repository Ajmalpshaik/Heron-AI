# The fourteenth session, 2026-09-01 — four fragments, two filters, and one that was already there

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**What it did:** took the library from **66 to 70**. All 70 compile on all eight releases; all 70 are
`DRAFT`. Six candidates went in; **one turned out not to need building at all**, which is the part worth
reading.

| | |
|---|---|
| `FILTER_ELEMENTS_IN_ROOM` | **A room is a VOLUME, not a rectangle.** An L-shaped room's bounding box covers ground outside it, so a box test puts the neighbour's diffuser in your schedule — quietly, and it looks right until somebody counts on site. Uses Revit's own point-in-room test, and handles **Space as well as Room**, because an MEP model carries Spaces where the architectural one carries Rooms |
| `FILTER_ELEMENTS_BY_TYPE` | *Select All Instances*, as a request. **Whole model or this view is an input, not a default** — the two differ by an order of magnitude, and choosing silently is how a change meant for one floor reaches nine |
| `READ_ELEMENT_MATERIAL` | Every material with its volume, not the first — a wall has every layer of its build-up. A zero-volume finish is still **named**, because dropping those loses exactly what a finishes schedule is about |
| `COPY_PARAMETER_VALUE` | Compares storage types **before writing anything**, and a mismatch stops the whole batch. It copies what is **stored**, never what is displayed: a length shown as "2400 mm" is stored in feet, and copying the display into text writes a unit-suffixed string no later calculation can use |

### The one that did not need building

*"What is the elevation of this"* was answering `READ_ROOM_GEOMETRY` and looked like a missing
capability. **It was not.** `READ_ELEMENT_LEVEL` already returns the level and the offset, and those two
*are* the elevation. The gap was in the **words**, not in the library — so two utterances were added and
no fragment was written.

> A second fragment computing the same number from the same inputs is a duplicate that can one day
> disagree with the first, and nothing would say which was right. **Check whether the library already
> composes the answer before adding to it** — the same check `count them by level` passes, since
> `READ_ELEMENT_LEVEL` into `GROUP_AND_COUNT` is that job already.

### The compile harness gained a namespace, and that is a promise to unbuilt work

`FILTER_ELEMENTS_IN_ROOM` needs `Room.IsPointInRoom`, which lives in `Autodesk.Revit.DB.Architecture` —
not among the namespaces the harness supplied. The alternative was hand-rolling point-in-polygon over the
boundary segments, **which is precisely how an L-shaped room gets answered wrongly**, and that is the case
the fragment exists to get right.

> **The `USINGS` list in [`tools/check-fragments-compile.py`](../../tools/check-fragments-compile.py) is a
> contract with unbuilt work.** It declares what a fragment may assume is in scope, so
> [D-28](../DECISIONS.md)'s Roslyn executor must supply the same set. A namespace added here and not
> there compiles green and fails at the PC — the second obligation this library has placed on that
> executor, after the `REVIT20xx` compile symbols.

**What none of this is.** Not one has met a model. `check-gaps` counts **70** fragments below `PROVEN`.

---
