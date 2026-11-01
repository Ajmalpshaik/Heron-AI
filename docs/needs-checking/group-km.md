# Needs checking — Group KM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KM - span direction symbols in a plan, Revit 2025 and later: `PLACE_SPAN_DIRECTION_SYMBOLS` (2026-11-01)

Asked for in the modeller's words: *"add span direction symbols to these slabs in this plan"* - Revit's
Annotate > Symbol > Span Direction. Nothing in Heron drew one. `brain/heron_retrieve.py`, Revit 2024,
offered `PLACE_AREA_REINFORCEMENT` first. Turning the span itself is [Group KL](group-kl.md).

| What | Where | What it does |
|---|---|---|
| `PLACE_SPAN_DIRECTION_SYMBOLS` (MODIFY) | [`brain/fragments/place-span-direction-symbols`](../../brain/fragments/place-span-direction-symbols/fragment.yaml) | Places one span direction symbol on each structural floor handed in, in **one floor plan or structural plan**, of the span direction symbol type the modeller **names**, at Revit's default point (the middle of the top face). Any other view is refused by name, nothing placed. **No type named, or one no span direction type has: nothing is placed** and the loaded types are listed. Named and left: not a floor, **not structural**, already showing a span direction symbol in this plan (or a view sharing its annotation), on another level than the plan's, refused by Revit. Every symbol read back - its type, its view, and that its floor lists it - and a wrong one removed inside the same undo; each floor's span reported in degrees |

**Revit 2025 and later only - the API's limit.** `SpanDirectionSymbol`, its `Create(Document, ElementId
viewId, LinkElementId, XYZ, ElementId typeId)` and `GetDefaultPlacementPoint(Document, LinkElementId)` are
absent from the 2020 to 2024 reference assemblies and present in 2025, 2026 and 2027
(`tools/api-surface --members`, 2026-11-01). The floor is passed as a `LinkElementId`, not the `ElementId`
the request first assumed. Before 2025 the API can list a floor's span direction symbols but cannot place
one.

**What is done and what is not.** It compiles for the three releases it claims
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). `Floor.GetSpanDirectionSymbolIds`,
`Floor.SpanDirectionAngle`, `Element.OwnerViewId`, `View.GetPrimaryViewId`, `GetDependentViewIds`,
`GetCategoryHidden` and `BuiltInCategory.OST_SpanDirectionSymbol` are unchanged 2020-2027. **Nothing here
has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KM2 to KM4 measure them): which
span direction types the metric structural template loads; whether `Create` accepts a structural plan as
well as a floor plan (the card allows both; Revit's failure text names only *Floor Plan View*); whether
Revit draws a symbol on its own when a structural floor is sketched (KM1 deletes any); whether a
foundation slab takes this category's symbol or only the footing one; and whether a dependent view shares
span symbols with its primary, as `TAG_ROOMS` believes of tags.

**Routing.** Measured through `heron_retrieve.find` on two scratch stores, before and after the two
cards: every declared utterance and routing-table sentence in the library reaches the card it reached
before. **Questions nobody declares** - *"what is the span direction of this slab"*, *"which way does
this slab span"*, *"which floors have no span direction symbol"* - reached other writes before and reach
this card after; with the host's `question=true` they reach a read, never this card. No card reads a
floor's span direction or its symbols: [row 5b-1369](../fragment-issues/section-5b-rows-401-425.md), OPEN,
for the owner. Since 2026-11-01 a read reports it - `REPORT_FLOOR_SPAN_DIRECTION`, [Group KP](group-kp.md) - and declares *"what is the span direction of this slab"* and *"which way does this floor span"*; the row is FIXED.

**The model - a scratch project from the metric structural template in Revit 2026 or 2027**, where Group
HY's HY15 runs ([group-hy.md](group-hy.md)). Never a working model. Every run below is rolled back except
KM1's arrangement and KM5's one run. The job file is
[`tools/jobs/place-span-direction-symbols-km-2026-11-01.yaml`](../../tools/jobs/place-span-direction-symbols-km-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KM0** | *Open.* Revit 2026 or 2027, a new project from the metric structural template, **nothing selected**, Changes on at the ribbon | The title bar names the scratch project |
| **KM1** | *Arrange, and KEEP it.* By hand: S1 (a 200 mm structural concrete floor), W1 (a 200 mm structural concrete wall), N2 (an architectural floor, Structural clear) on Level 1, and S2 (a structural floor) on Level 2. Delete any span direction symbol Revit drew. Save | "FloorPlan: Level 1" shows S1, W1 and N2 and no span direction symbol |
| **KM2** | *Read the types - also the "none named" negative.* `place-span-direction-symbols` with `--write`, `view` Level 1, `symbolType` left out, the floors of Level 1 selected | `placed` 0, *No span direction symbol type was named, and one is never chosen for you*, `typesLoaded` listing the types. Write the one-way type into the job file, both legs |
| **KM3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: the walls - `placed` 0, W1 in `notFloors`. Positive: `placed` 1 on S1, `newSymbols` 1 id, N2 in `notStructural`, `report` naming S1's span |
| **KM4** | *The refusals.* KM3's positive with `view` a 3D view; with a structural plan of Level 1, if the template has one; S1 with S2 | 3D refused by name, nothing placed. Structural plan: **record** whether Revit placed it or refused. S2 in `otherLevel` |
| **KM5** | *Kept, through `try-once`, on the modeller's yes.* KM3's positive on the saved project, "Level 1" in front | Keep or Discard. Then the same run again places nothing - S1 in `alreadyHere` - and Select All Instances > Visible in View on the new symbol counts 1: the second route |
