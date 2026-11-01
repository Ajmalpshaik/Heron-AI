# Needs checking — Group KL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KL - the span direction of structural floors: `SET_FLOOR_SPAN_DIRECTION` (2026-11-01)

Asked for in the modeller's words: *"set the span direction of this slab to run along grid 1"*, *"change
the floor span to 90 degrees"* - the span direction of a structural floor (Revit's Structure > Span
Direction). Nothing in Heron set it. `brain/heron_retrieve.py`, Revit 2024, sent the first sentence to
`MAXIMIZE_DATUM_EXTENTS` and the second to `ROTATE_ELEMENTS`. The symbol that shows the span in a plan is
a card of its own, [Group KM](group-km.md).

| What | Where | What it does |
|---|---|---|
| `SET_FLOOR_SPAN_DIRECTION` (MODIFY) | [`brain/fragments/set-floor-span-direction`](../../brain/fragments/set-floor-span-direction/fragment.yaml) | Sets the span direction of each structural floor handed in - to an angle in **degrees** (radians to the API), or **along** a grid, a named reference plane or a straight model or detail line, the direction **read off that element**; with both, the angle is measured from that element. A curved grid, an arc line, a multi-segment grid, a line that runs straight up, an unknown name, or neither value given: **nothing is changed** and the reason is named. Named and left: not a floor, **not structural** (Revit's documentation, all eight releases: *"An exception will be thrown if the floor is non structural"*), already spanning that way (90 and 270 are one span), refused by Revit. Every floor read back after a regeneration - the API's angle and the floor's Span Direction parameter - and one that did not take is put back |

**Why two cards and not one.** Turning the span changes the floor; the symbol is an annotation in one
view. They need different values, and `SpanDirectionSymbol.Create` exists only from 2025 while
`Floor.SpanDirectionAngle` is settable 2020 to 2027 - one card would have had to claim the shorter list.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `Floor.SpanDirectionAngle` (double, get and set),
`Floor.GetSpanDirectionSymbolIds`, `Grid.Curve`, `Grid.IsCurved`, `MultiSegmentGrid`,
`ReferencePlane.BubbleEnd` and `FreeEnd`, `CurveElement.GeometryCurve` and
`BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL` and `FLOOR_PARAM_SPAN_DIRECTION` are present and unchanged
2020-2027. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KL2 to KL4 measure them):
which axis Revit measures `SpanDirectionAngle` from - the card assumes the model's X axis, anticlockwise,
and the positive along a 30-degree grid tells; whether Revit hands the angle back as set or folded into
0-180 (the read-back compares the span either way); how the Span Direction parameter writes its value;
and whether a floor whose Structural box is clear really throws, as the documentation says, or reads 0.

**Routing.** Measured through `heron_retrieve.find` on two scratch stores, before and after the two
cards: every declared utterance and routing-table sentence in the library reaches the card it reached
before; on the keyword route alone *"set out the structural grid"* (`CREATE_GRIDS`' own) now ranks this
card first, and the host answers it by identity, so `CREATE_GRIDS` keeps it. **Questions nobody declares**
- *"what is the span direction of these floors"*, *"what angle is the span of this slab"* - reached other
writes before and reach this card after; with the host's `question=true` they reach a read, never this
card. No card reads a floor's span direction: [row 5b-1365](../fragment-issues/section-5b-rows-401-425.md),
OPEN, for the owner. Since 2026-11-01 a read reports it - `REPORT_FLOOR_SPAN_DIRECTION`, [Group KP](group-kp.md) - and declares *"what is the span direction of this slab"* and *"which way does this floor span"*; the row is FIXED.

**The model - Group HY's scratch project in Revit 2020** ([group-hy.md](group-hy.md)): a new project from
the metric structural template holding S1 (structural floor), W1 (structural wall) and N2 (architectural,
non-structural floor) on Level 1. Never a working model. Every run below is rolled back except KL1's
arrangement and KL5's one run. The job file is
[`tools/jobs/set-floor-span-direction-kl-2026-11-01.yaml`](../../tools/jobs/set-floor-span-direction-kl-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KL0** | *Open.* The HY scratch project open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar names the scratch project |
| **KL1** | *Arrange, and KEEP it.* By hand: a straight grid **1** at 30 degrees from the model's X axis, and an arc grid **R1**. Save | Both grids in "FloorPlan: Level 1" |
| **KL2** | *Read S1's span first.* By hand, place a span direction symbol on S1 in "FloorPlan: Level 1" (Annotate > Symbol > Span Direction) and note where its arrow points; then delete it, unsaved | If S1 already spans along grid 1, turn it by hand to 0 and save, or KL3's positive is *already there* |
| **KL3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: the walls - `changed` 0, W1 in `notFloors`. Positive: `changed` 1, `spanDegrees` 30, N2 in `notStructural`, `report` *span ... deg -> 30 deg*. **Record** the Span Direction parameter's text and whether the read-back came as 30 or 210 |
| **KL4** | *The refusals and the angle.* `validate set-floor-span-direction --write` with KL3's arrangement: `along` *grid R1*; `along` *grid 99*; neither value; `angleDegrees` 90 alone; `along` *grid 1* with `angleDegrees` 90; N2 alone | R1 refused *curved*; 99 refused *No grid is called*; neither refused *never guessed*; 90 alone - S1 spans 90; with grid 1 - 120; N2 alone - `notStructural`, nothing changed. **Record** whether N2's angle threw or read a value |
| **KL5** | *Kept, through `try-once`, on the modeller's yes.* KL3's positive on the saved scratch project, "FloorPlan: Level 1" in front | Keep or Discard. Then a span direction symbol placed by hand on S1 points along grid 1 - the second route - and the same run again leaves S1 in `alreadyThere` |
| **KL6** | *Revit 2024 or later.* KL3 again | The same answers. Nothing the card calls changed between releases |
