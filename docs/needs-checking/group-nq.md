# Needs checking — Group NQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NQ - an arc's or a circle's CENTRE locked to named planes in a 2D family: `LOCK_FAMILY_LINES_TO_PLANES` version 3 (2026-11-12)

Built 2026-11-12 on the owner's word ("make the arc centre lock tool") - the gap table's row 3e
([2026-11-11-family-2d-capabilities](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)). Widened, not
made new: locking a 2D line to planes is this card's job, and `LOCK_FORM_SKETCH_TO_PLANES` (PROVEN) locks a FORM's
circle centre the same way. Why, and what is not known, is
[FRAGMENT-ISSUES row 5b-1576](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `LOCK_FAMILY_LINES_TO_PLANES` version 3 (MODIFY) | [`brain/fragments/lock-family-lines-to-planes`](../../brain/fragments/lock-family-lines-to-planes/fragment.yaml) | an arc or a whole circle drawn as a detail or symbolic line, its CENTRE on a plane named: locked there by `CurveElement.CenterPointReference`, the Center Mark switched on first when it is hidden. Version 2's along and end locks unchanged |

**Compiled** on Revit 2020, 2024 and 2027 (`tools/check-fragments-compile.py`, 2026-11-12, the owner's PC).

**The model:** the SAVED scratch family `%TEMP%\heron-work\rev2024\R24 Arc Lock.rfa`, Metric Detail Item, Revit 2024,
session 13164 - never the owner's own families. Built with PROVEN tools: a type length "Arc X" 200 labelling
"Center (Left/Right)" to plane "Arc X" at X 200; plane "Arc Y" at Y 100 and "Spare" at X 500; a detail arc
"path 300,100; arc 200,200 100,100" (centre 200,100, radius 100, id ...000008f7) and a shaft "path -300,0; 0,0"
(...000008fb), never locked.

| # | Run | Look for | Result 2026-11-12 |
|---|---|---|---|
| **NQ1** | *Trial, rolled back* - lines = the arc, planes "Arc X, Arc Y" | 2 locks, the arc's centre to each; ends unchanged | **held**: 4 locks - the centre to "Arc X" and "Arc Y", and the arc's two ends to "Arc Y", which they lie on (version 2's end lock); ends read back (300, 100) to (100, 100) |
| **NQ2** | *The D-30 negative, rolled back* - the arc, planes "Spare" | refused, the centre 300 mm off "Spare"; nothing locked | **held**: "Nothing was locked. Nothing lies along "Spare" and nothing ends on it" - the centre named 300 mm off |
| **NQ3** | *The positive, KEPT* - through `try-once`, lines = the arc and the two halves of "circle 200,100 50" (...0903, ...0907, drawn after NQ1), planes "Arc X, Arc Y" | the owner presses Keep | **KEPT by Ajmal PS**: 12 locks - each of the three arcs' centres to "Arc X" and "Arc Y", and their six ends to "Arc Y"; Center Mark already on (centreMarks 0); ends read back unchanged |
| **NQ4** | *It follows* - "Arc X" set to 300 (SET_FAMILY_TYPE_VALUES), read back with REPORT_FAMILY_FORMS "2d" | centre (300, 100), radius 100; the shaft still (-300, 0) to (0, 0) | **held**: the arc centre (300, 100) radius 100, both halves centre (300, 100) radius 50, the shaft (-300, 0) to (0, 0). The D-30 negative was then run on this state (`validate --write --kept`, planes "Spare"): refused, the centre named 200 mm off, nothing locked |
| **NQ5** | *A whole circle* - a closed detail circle, centre on both planes | its centre locked twice, no end locks | NOT RUN - Heron draws a circle as two half-arcs (DRAW_FAMILY_DETAIL_LINES "circle"), so a closed one needs drawing by hand. The two halves of "circle 200,100 50" go in NQ3 instead |
| **NQ6** | *Version 2 unchanged* - Group NC's straight line locks | as Group NC | NOT RUN - Group NC's own re-run is owed |

**Proof:** the kept NQ3 and the negative after NQ4 - SIGNED by Ajmal PS 2026-11-12; the card is PROVEN on Revit 2024.
