# Needs checking — Group NQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NQ - an arc's or a circle's CENTRE locked to named planes in a 2D family: `LOCK_FAMILY_LINES_TO_PLANES` version 3 (2026-11-12)

**Tracked as GitHub issue [#1015](https://github.com/Ajmalpshaik/Heron-AI/issues/1015) from 2026-11-16 - done 2026-11-19:** #871 struck NQ2, and NQ1, NQ3, NQ4, NQ5 and NQ6 are struck on the results of 2026-11-12 in their own rows. Every id in this group's table is struck. Every result stays in this file.

Built 2026-11-12 on the owner's word ("make the arc centre lock tool") - the gap table's row 3e
([2026-11-11-family-2d-capabilities](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)). Widened, not
made new: locking a 2D line to planes is this card's job, and `LOCK_FORM_SKETCH_TO_PLANES` (PROVEN) locks a FORM's
circle centre the same way. Why, and what is not known, is
[FRAGMENT-ISSUES row 5b-1576](../fragment-issues/row-5b-1576.md).

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
| ~~**NQ1**~~ | Trial, rolled back - lines = the arc, planes "Arc X, Arc Y" — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | 2 locks, the arc's centre to each; ends unchanged | |
| ~~**NQ2**~~ | The D-30 negative, rolled back - the arc, planes "Spare" — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | refused, the centre 300 mm off "Spare"; nothing locked | |
| ~~**NQ3**~~ | The positive, KEPT - through `try-once`, lines = the arc and the two halves of "circle 200,100 50"… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | the owner presses Keep | |
| ~~**NQ4**~~ | It follows - "Arc X" set to 300 (SET_FAMILY_TYPE_VALUES), read back with REPORT_FAMILY_FORMS "2d" — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | centre (300, 100), radius 100; the shaft still (-300, 0) to (0, 0) | |
| ~~**NQ5**~~ | A whole circle - a closed detail circle, centre on both planes — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | its centre locked twice, no end locks | |
| ~~**NQ6**~~ | Version 2 unchanged - Group NC's straight line locks — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | as Group NC | |

**Proof:** the kept NQ3 and the negative after NQ4 - SIGNED by Ajmal PS 2026-11-12; the card is PROVEN on Revit 2024.

### Revit 2020 - 2026-11-12, session 10900

A SAVED scratch family `%TEMP%\heron-work\rev2020\R20 Arc Lock.rfa` (Metric Detail Item), arranged as above with PROVEN
tools. **NQ7** - rolled back, the arc `...-00000810` (centre 200,100, radius 100): 4 locks, as on 2024; "Spare"
refused. Recorded for the proof on a second arc at the same centre, `...-00000820` (radius 50), as it had to be
unlocked: 4 locks, "Spare" refused - `proof.releases` entry for 2020 SIGNED by Ajmal PS. **NQ8** - the first arc
locked through `revit_change` and KEPT (4 locks; Heron's delete box for 2 Automatic Sketch Dimensions, row 5b-1579),
then Arc X 200 -> 300: the arc read back at centre (300, 100), radius 100. The unlocked second arc moved too, through
Revit's Automatic Sketch Dimensions, as row 5b-1579 records.
