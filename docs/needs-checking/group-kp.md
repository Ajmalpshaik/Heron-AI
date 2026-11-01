# Needs checking — Group KP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KP - which way a structural floor spans: `REPORT_FLOOR_SPAN_DIRECTION` (2026-11-01)

Asked for in the modeller's words: *"what is the span direction of this slab"*, *"which way does this floor
span"*. No card read a floor's span direction: [row 5b-1365](../fragment-issues/section-5b-rows-401-425.md)
recorded the first sentence reaching a write without the host's question flag, and a read that does not
answer it with the flag. The two writes beside it are [Group KL](group-kl.md) (`SET_FLOOR_SPAN_DIRECTION`)
and [Group KM](group-km.md) (`PLACE_SPAN_DIRECTION_SYMBOLS`).

| What | Where | What it does |
|---|---|---|
| `REPORT_FLOOR_SPAN_DIRECTION` (READ) | [`brain/fragments/report-floor-span-direction`](../../brain/fragments/report-floor-span-direction/fragment.yaml) | For each floor handed in: its span direction in **degrees** (`Floor.SpanDirectionAngle`, radians from the API), anticlockwise from the model's X axis as `SET_FLOOR_SPAN_DIRECTION` sets it, with the **axis** it means (0 up to 180 - a span is an axis); the floor's own **Span Direction parameter** beside it; and its **span direction symbols**, each with its type and the view it is drawn in, or that it has none. A floor that is **not structural** is named in `notStructural`, never given an angle; anything not a floor in `notFloors`. Opens no transaction |

**A card of its own, not `READ_ELEMENT_PARAMETERS` with *Span Direction*.** Whether Revit hands
`FLOOR_PARAM_SPAN_DIRECTION` to a lookup by that name needs a real Revit to know, and that card could not
say why a floor has no span, nor which symbols show it.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `Floor.SpanDirectionAngle` (double), `Floor.GetSpanDirectionSymbolIds`
(`ICollection<ElementId>`), `Floor.FloorType`, `Element.Id`, `Document`, `OwnerViewId`, `GetTypeId` and
`get_Parameter(BuiltInParameter)`, `Document.GetElement(ElementId)`, `ElementType.FamilyName`, `View.Name`,
`Parameter.AsInteger` and `AsValueString`, `ElementId.InvalidElementId` and `==`, and
`BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL` and `FLOOR_PARAM_SPAN_DIRECTION` are present and unchanged
2020-2027. `SpanDirectionSymbol`, the class, exists only from 2025 and is **not** used - a symbol is read as
an `Element`, so the card claims all eight releases. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies** - the same questions Group KL records, and KP2 answers them for a read:
which axis Revit measures `SpanDirectionAngle` from (the card says the model's X axis, anticlockwise);
whether it comes back folded into 0-180; how the Span Direction parameter writes its value; and whether a
floor whose Structural box is clear throws, as the documentation says, or reads 0 - the card reads the box
first, so either way such a floor is named and given no angle.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores before and after the card,
`model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025: the two sentences reach
this card with and without the host's question flag (they reached `PLACE_SPAN_DIRECTION_SYMBOLS` or
`SET_FLOOR_SPAN_DIRECTION` without it, a read that does not answer them with it). So do the probes *"which
way does this slab span"*, *"what angle is the span of this slab"* and *"show me the span direction of this
floor"*; *"what is the span direction of these floors"*, declared nowhere, still reaches
`SET_FLOOR_SPAN_DIRECTION` without the flag. Every declared utterance and routing-table sentence in the
library reaches the card it reached before, **but one**: *"add span direction symbols to these slabs in this
plan"*, `PLACE_SPAN_DIRECTION_SYMBOLS`' own, behind a 2020 or 2024 wall - where that card is not offered,
because the API cannot place a symbol before 2025 - now ranks this read first where it ranked
`SET_FLOOR_SPAN_DIRECTION`, a write that turns the span, before. No card can answer it there; written in
both cards. With the flag, only sentences writes declare moved, from one read to another.

**The model - Group HY's scratch project in Revit 2020** ([group-hy.md](group-hy.md)), the one Group KL
uses: S1 (structural floor), W1 (structural wall) and N2 (architectural floor) on Level 1, with KL1's grid 1
kept. Never a working model. Nothing below changes the model but KP1's arrangement. The job file is
[`tools/jobs/report-floor-span-direction-kp-2026-11-01.yaml`](../../tools/jobs/report-floor-span-direction-kp-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KP0** | *Open.* The HY scratch project open and in front in Revit 2020, **nothing selected** | The title bar names the scratch project |
| **KP1** | *Arrange, and KEEP it.* By hand, place one span direction symbol on S1 in "FloorPlan: Level 1" (Annotate > Symbol > Span Direction), note where its arrow points against grid 1, and save | One symbol on S1; N2 and W1 have none |
| **KP2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, the walls: `read` 0, `spans` and `symbolIds` empty, W1 in `notFloors`. Positive, the floors: `read` 1, `spans` S1 at the angle its arrow shows (or that plus 180, *"the same span as"*), `symbolIds` S1's one symbol, the report naming its type and *Level 1*; N2 in `notStructural`. **Record** the Span Direction parameter's text and whether the angle came back above 180 |
| **KP3** | *The floor that is not structural.* Select N2 alone by hand, `validate report-floor-span-direction` | `read` 0; N2 in `notStructural`, *"not structural - Revit keeps a span direction only for a floor with Structural ticked"*. **Record** nothing else - an angle here is the defect |
| **KP4** | *The second route, and tracking.* After Group KL's KL5 run is KEPT (S1 along grid 1), KP2 again | `spans` gives S1 30 (or 210), and S1's symbol in the plan points along grid 1 - this card agrees with the one that set it |
| **KP5** | *Revit 2025 or later.* KP1 and KP2 again | The same answers. Nothing the card calls changed between releases |
