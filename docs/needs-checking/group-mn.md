# Needs checking — Group MN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MN - Structure > Rebar Shape, Annotate > Symbol > Area, Path and Fabric Reinforcement Symbols, and Architecture > Stair > Create Sketch - make a rebar shape from its segments and bends, put Revit's reinforcement symbols on area and path reinforcement and fabric sheets, and sketch a stair from its boundaries and riser lines: `CREATE_REBAR_SHAPE`, `PLACE_REINFORCEMENT_SYMBOLS`, `CREATE_SHAPED_STAIRS` widened (2026-11-02)

Asked for as three gaps on Revit's ribbon: *"Create a rebar shape (Structure > Reinforcement > Rebar Shape / Rebar
Shape Browser) - a new RebarShape from a list of segment lengths/angles"*, *"Path and fabric reinforcement
symbols/tags (Annotate > Symbol > Path Reinforcement Symbol, Fabric Reinforcement Symbol, and the span symbol for
area reinforcement)"* and *"Stair by sketch (Architecture > Stair > Create Sketch: boundary + riser lines)"*.
`brain/heron_retrieve.py` on a scratch store of this tree (Revit 2024) sent the rebar shape to
`CREATE_REBAR_BAR_TYPE`, `PLACE_REBAR` and `SET_REINFORCEMENT_SETTINGS`, the symbols to
`PLACE_PATH_REINFORCEMENT`, `REPORT_FLOOR_SPAN_DIRECTION` and `PLACE_FABRIC_REINFORCEMENT`, and the sketch to
`CREATE_STAIRS` and `CREATE_SHAPED_STAIRS`. None made a rebar shape or placed any of the three symbols, and no card
called `StairsRun.CreateSketchedRun`. The duplicate check (`heron_duplicates.look`) and the Matcher found nothing
equal for the two new cards.

**Searched and not widened, each with its reason.** `CREATE_REBAR_BAR_TYPE` and `CREATE_REBAR_HOOK_TYPE` make
the rebar shape's two sibling types and are PROVEN. `PLACE_REBAR` (DRAFT) places a **bar** of a shape already
loaded - its sketched route lets Revit pick or make an unnamed shape as a side effect; a named shape with no bar
and no host is a job of its own. `PLACE_SPAN_DIRECTION_SYMBOLS` (DRAFT, this branch only) is the nearest symbol
card, but its host is a **floor** and its `SpanDirectionSymbol` exists only on 2025 to 2027, so it claims those
three releases; the three reinforcement symbols are 2020 to 2027 and take a reinforcement system or a fabric
sheet. `PLACE_MULTI_REBAR_ANNOTATION` (DRAFT) annotates rebar only. A **tag** on path, area or fabric
reinforcement or a fabric area is `TAG_ELEMENTS_IN_VIEW`'s (PROVEN) - its category map works each tag category
out from Revit's own names. **`CREATE_SHAPED_STAIRS` was widened** (DRAFT, on this branch only): the same job, a
whole new stair between two levels in one edit scope, with the same levels, type, riser rules, read-back, retry
and sweep - version 2, still DRAFT.

| What | Where | What it does |
|---|---|---|
| `CREATE_REBAR_SHAPE` (MODIFY) | [`brain/fragments/create-rebar-shape`](../../brain/fragments/create-rebar-shape/fragment.yaml) | A new rebar shape from **segment lengths in mm** and the **degrees turned at each corner**, left or right; standard or stirrup/tie (its attachment named, never chosen); a start and end hook as *"135 left"* or none. Each length is driven by the model's own rebar shape parameter A, B, C ... in order. Built as a `RebarShapeDefinitionBySegments` and put to Revit's solver before anything is made; a name in use, the wrong number of bends, a model without the letters and a shape the solver cannot solve are refused. Read back: name, style, each letter's length, bends, hooks, and how many bar types it is allowed for |
| `PLACE_REINFORCEMENT_SYMBOLS` (MODIFY) | [`brain/fragments/place-reinforcement-symbols`](../../brain/fragments/place-reinforcement-symbols/fragment.yaml) | Revit's **Area, Path and Fabric Reinforcement Symbol** on each area reinforcement, path reinforcement and fabric sheet selected (a fabric area gives each of its sheets one), in one floor plan, structural plan, ceiling plan or elevation. A symbol type named is used for its own kind; the others take **the project's default for that symbol**, named in the answer. Already showing one in the view, not reinforcement, no type, refused by Revit: each named and left. Every symbol read back - type, view, host |
| `CREATE_SHAPED_STAIRS` version 2 (MODIFY) | [`brain/fragments/create-shaped-stairs`](../../brain/fragments/create-shaped-stairs/fragment.yaml) | Adds **shape sketch**: one sketched run from `leftBoundary` and `rightBoundary` (points in mm from `startPoint`, straight lines between them) and, optionally, `riserLines` (*"x,y,z; x,y,z \| ..."*). Left out, the risers the stair type's rules give are spread evenly along the two boundaries; the path runs through the middle of every riser line. L, U and spiral are made exactly as version 1 made them; the sketch values are refused on them |

**What is done and what is not.** All three compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member was read off the shipped
assemblies of all eight with `tools/api-surface --members`; the per-release members are named in each card's
`compatibility-note` - `RebarShape.Create`'s hook form is 2020-2026 and its terminations form 2026-2027, and a
symbol's host is `TaggedLocalElementId` on 2020-2021 and `GetTaggedLocalElementIds` from 2022. **Nothing here has
run in Revit.** All three are DRAFT. **Only Revit can settle**, and MN2-MN6 record: whether Revit's acute and obtuse
are the angle turned or the angle between the legs, and whether its own shapes measure to the outside of a bend
([row 5b-1454](../fragment-issues/section-5b-rows-401-425.md)); whether Revit takes each symbol kind's own
category, and a symbol in a structural plan or a section
([row 5b-1455](../fragment-issues/section-5b-rows-401-425.md)); and how many risers a sketch of N riser lines
makes ([row 5b-1456](../fragment-issues/section-5b-rows-401-425.md)). **Not done:** arc, spiral, 180 degree and 3D
rebar shapes; symbols on reinforcement in a linked model; arcs in a stair sketch.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `9659655` and of this change, Revit 2024, with and without the host's question flag (the `READ` ceiling), over every declared utterance and routing-table sentence of the library and 24 probes. Every sentence the three cards declare reaches its card by identity. *"what shape is this element"* (claimed by `MEASURE_ELEMENT_VOLUME`) moved to `CREATE_REBAR_SHAPE` and *"make the leaders L-shaped"* (claimed by `FORCE_TAG_LEADER_LSHAPE`) to `CREATE_SHAPED_STAIRS`; each is now declared on its owner (card text only - both PROVEN, impl/ and proofs untouched). Four made-up questions and two made-up tag requests reach a new write without the flag, a read with it - [row 5b-1457](../fragment-issues/section-5b-rows-401-425.md), OPEN. `tests/test_routing_phrases.py` fails exactly its known seven, *"put the duct tags above the ducts with a leader"* still reaching `OFFSET_TAGS_FROM_HOST`. `tools/check-routing.py`: no question answered by a write.

**The models - scratch, any one release 2020 to 2027.** Never a working model. Every run below is rolled back
except MN1's arrangement. The job file is
[`tools/jobs/rebar-shape-symbols-sketch-stair-mn-2026-11-02.yaml`](../../tools/jobs/rebar-shape-symbols-sketch-stair-mn-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MN0** | *Open.* Any release 2020 to 2027. "Heron MN Rebar", a new project from the metric structural template saved under `%TEMP%\heron-work`; "Heron Shaped Stair Test", Group LS's scratch project (LS1); Changes on at the ribbon | The title bar names the scratch project |
| **MN1** | *Arrange, and KEEP it.* In "Heron MN Rebar": the template's rebar shapes loaded (Structure > Rebar Shape Browser lists them); on Level 1 a 200 mm structural floor F1 with one area reinforcement AR1, one path reinforcement PR1 along its edge and one fabric area FA1; a structural wall W1. Delete any reinforcement symbol Revit drew. Place one Area, Path and Fabric Reinforcement Symbol by hand in a section and delete them, so each is the project's default. Save. In "Heron Shaped Stair Test", nothing near 20000,0,0 | The Rebar Shape Browser's shapes; "Level 1" with no reinforcement symbol |
| **MN2** | *CREATE_REBAR_SHAPE - D-30 pair, NEGATIVE FIRST.* The job file's first job; then by hand, rolled back, `shapeName=HN L 45; segments=500, 500; bends=45 right; shapeStyle=standard; endHook=90 left` | Negative (one bend for three segments): created 0, refused, nothing in the browser. Positive: created 1, readBack *A 300 mm, B 600 mm, C 300 mm; bends 90 left, 90 left*. The 45: **RECORD** whether a finding says the bend categories solved swapped, and what the browser draws (row 5b-1454) |
| **MN3** | *The second route for MN2.* Structure > Rebar Shape Browser after the positive KEPT once in a copy | The new shape by name, three legs; **RECORD** the lengths its Type Properties show and whether they are outside dimensions (row 5b-1454) |
| **MN4** | *PLACE_REINFORCEMENT_SYMBOLS - D-30 pair, NEGATIVE FIRST.* The job file's second job; then by hand, rolled back, AR1, PR1 and FA1 together | Negative (W1): placed 0, W1 in notReinforcement. Positive (PR1): placed 1, the finding naming the project's default type. All three: one symbol on AR1, one on PR1, one on each of FA1's sheets. **RECORD** any Revit refusal naming a category (row 5b-1455) |
| **MN5** | *The views.* PR1 by hand, rolled back, with `view` a structural plan of Level 1, a section through F1 and `{3D}` | `{3D}` refused by name. **RECORD** what Revit says in the structural plan and the section (row 5b-1455) |
| **MN6** | *CREATE_SHAPED_STAIRS version 2 - D-30 pair, NEGATIVE FIRST.* The job file's third job; then the first job of `tools/jobs/shaped-stairs-ls-2026-11-02.yaml` once more | Negative (three riser lines for a 3000 mm climb): refused before the model is touched, nothing made. Positive: one stair, ONE run, no landing, between x 20000 and 21000. **RECORD** the risers Revit made against the riser lines drawn (row 5b-1456). The L: made exactly as LS2 recorded |
| **MN7** | *The second route.* Annotate > Symbol > Path Reinforcement Symbol on PR1 by hand in a saved copy, then the card on PR1; Architecture > Stair > Create Sketch by hand along the same boundaries | PR1 in alreadyHere; the hand sketch's riser count beside the card's; one Ctrl+Z takes each Heron run back as one entry |
