# Needs checking — Group MI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MI - Electrical Settings > Wiring Types and the electrical analytical model - add a wire type by its material, rating and insulation, an area-based load type in W/m2 put on the selected spaces, and power distribution nodes connected to the node that feeds them: `ADD_WIRE_TYPE`, `ADD_AREA_BASED_LOAD`, `ADD_ELECTRICAL_ANALYTICAL_NODE` (2026-11-02)

Asked for in the modeller's words: *"add a wire type called XLPE 4C with copper conductor and 90 degree
insulation"*, *"what wire types are in this model"*, *"add an area-based load of 25 W/m2 for Office lighting"*,
*"assign area-based loads to these spaces"*, *"add a power distribution node for transformer TX-1"* and *"connect
the MDB node to the transformer node"* - Revit's Manage > MEP Settings > Electrical Settings > Wiring Types, and
Revit 2023's electrical analytical model (Analyze > Area Based Load, Power Distribution System). The question was
already answered: `REPORT_ELECTRICAL_SETTINGS` lists the wire types with their values, and declares the sentence now.
No card made any of the rest: a grep of every fragment that day for `AddWireType`, `AreaBasedLoad`,
`ElectricalLoadArea` and `ElectricalAnalyticalNode` found nothing, and `brain/heron_retrieve.py` sent the five
requests to `ADD_ELECTRICAL_DISTRIBUTION`, `SET_FAMILY_LIGHT_SOURCE`, `PLACE_ROOMS` and
`PLACE_STRUCTURAL_CONNECTIONS`. heron-duplicate-checker's method found look-alikes only - `CREATE_WIRES` draws wires
of a type the model has, `ADD_ELECTRICAL_DISTRIBUTION` adds voltages and distribution systems (a supply setting, not
a wire type or a node), `READ_SPACE_LOADS` reads heating and cooling loads, `CONNECT_INTO_MAIN` joins pipes and
ducts - so three new cards, all writes. The area-based load type and the loads share one card because the density
lives on the type; every upstream connection, node or load, is one contract on the node card.

| What | Where | What it does |
|---|---|---|
| `ADD_WIRE_TYPE` (MODIFY) | [`brain/fragments/add-wire-type`](../../brain/fragments/add-wire-type/fragment.yaml) | Adds **one wire type** - its name, conductor material, temperature rating and insulation, and optionally its max size - **each matched by its exact name, never guessed** (one not there is refused with **the names the model has**; one differing only in capitals is named). Insulation left out is the rating's only one, else refused with the list. The neutral multiplier, neutral required, neutral size and conduit are **copied from a wire type already there** (`basedOn`, or the first by name) and the report names which. A name already present is **skipped**. Made inside a SubTransaction: a value Revit refuses keeps nothing. Every wire type with its values **before and after** (`added`) |
| `ADD_AREA_BASED_LOAD` (MODIFY) | [`brain/fragments/add-area-based-load`](../../brain/fragments/add-area-based-load/fragment.yaml) | Makes an **area-based load type** with its **load density in W/m2** and load classification when none has that name (one already there is **never changed** - a different density is named); with **spaces selected**, puts the electrical load area inside each space in an **area-based load of that type on its level** - one per level and phase, added to when one of that name and type is there, skipped when it is of another type. A level with **no load areas** has them made by Revit, the whole level, and says so; a space with no load area inside it on a level that has some is refused. Types and loads with densities, load areas and W and VA **before and after** (`typesAdded`, `loadsAdded`, `areasAssigned`). Revit 2023 on |
| `ADD_ELECTRICAL_ANALYTICAL_NODE` (MODIFY) | [`brain/fragments/add-electrical-analytical-node`](../../brain/fragments/add-electrical-analytical-node/fragment.yaml) | Adds **one node** - power source, bus, transformer, transfer switch or equipment load - by name, and/or **connects a node or an area-based load to the node upstream** that feeds it, both ends **by exact name** (one not there lists the nodes with their kinds). A name already present is skipped; a node already fed from **another** node is refused - **Heron never disconnects** - a transfer switch's second source left to Revit's own check. Every node with its kind, what feeds it, what it feeds and its total load **before and after** (`added`, `connected`). Revit 2023 on; an area-based load is connected from 2024 |

**What is done and what is not.** `ADD_WIRE_TYPE` compiles for all eight releases, 2020 to 2027, the other two
for 2023 to 2027, the releases they claim (`tools/check-fragments-compile.py`, 2026-11-02, a Linux container).
Read off the shipped assemblies with `tools/api-surface --members`: `ElectricalSetting.AddWireType` and the
`WireMaterialType` / `TemperatureRatingType` / `InsulationType` / `WireSize` objects are **2020 to 2026, obsolete in
2026 and gone in 2027**, so from 2026 (`#if REVIT2026_OR_GREATER`) the wire type is `WireType.Duplicate` of the base
with `ConductorMaterial`, `TemperatureRating` and `InsulationMaterial` ids and a size name; `AreaBasedLoadType`,
`Zone.CreateAreaBasedLoad`, `AreaBasedLoadData`, `ElectricalLoadAreaData` and `ElectricalAnalyticalNode` are **2023
on**, and `AreaBasedLoadData.ConnectToUpstreamNode` **2024 on** (`#if REVIT2024_OR_GREATER`). **Nothing here has
run in Revit.** All three cards are DRAFT. **Only Revit can settle**: the exact names the templates give the
materials, ratings and insulations; whether Revit 2026 takes a rating and an insulation that belonged to another
material before the upgrade; that `LoadDensity` is the electrical power density W/m2 is read in, and which quantity
a node's `TotalLoad` holds; whether a load area made by Revit for a level has its location point inside the space it
fills; and whether a node already fed accepts a second source anywhere but a transfer switch. MI2 to MI5 RECORD each.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `d7acd42` and of this
change (`model` backend; no Revit filter, 2024 and 2020; with and without the host's question flag), over every
declared utterance and routing-table sentence of every live card (4,486 sentences). Without the flag the five
requests reach these cards; with it a read. *"what wire types are in this model"* reaches `REPORT_ELECTRICAL_SETTINGS`
with and without the flag, before and after. *"what latitude is the site set to"*, claimed by `REPORT_LOCATION`'s
routing table, moved without the flag from that read to `SET_PROJECT_LOCATION`, an Admin write, and is declared on
`REPORT_LOCATION`, outside `impl/`, fingerprint unchanged -
[row 5b-1438](../fragment-issues/section-5b-rows-401-425.md), FIXED. With the flag seven write sentences moved from
one read to another, and *"put it back"* flipped write to write again ([row 5b-1375](../fragment-issues/section-5b-rows-401-425.md),
OPEN) - recorded there, not tuned. `tests/test_routing_phrases.py` fails exactly its known seven.

**The model - `electrical-mi`**, a scratch project from Revit's metric Electrical template on **Revit 2024**,
saved under `%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps
it. The job file is
[`tools/jobs/electrical-wire-type-area-load-nodes-mi-2026-11-02.yaml`](../../tools/jobs/electrical-wire-type-area-load-nodes-mi-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MI1** | *Arrange.* `electrical-mi`: on Level 1, four walls making two rooms and a space in each (Analyze > Space); Level 2 empty. Open Electrical Settings > Wiring Types and Wire Sizes and write every wire type and the exact material, temperature rating and insulation names **into the job file**; note the Area Based Load Types and the Power Distribution System the template has (most have none). Save | The wire types and names as the dialog shows them |
| **MI2** | *Wire type - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back; then by hand: material `copper` (lower case); temperatureRating `90`; insulation left out where the rating has several | Negative: added 0, skipped, the list unchanged. Positive: the new row in Wiring Types with **those** values and the neutral and conduit of the wire type named; lower case named *differs only in capitals*; `90` lists the ratings; no insulation lists them - nothing added in any. **RECORD the template's names** |
| **MI3** | *Area-based load - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back; then by hand: nothing selected, `Office lighting` at 25 kept, then asked again at 40 | Negative, the walls: no load and no load area assigned. Positive: the type at **25 W/m2 in the type's properties - not 2.3 and not 269** - Revit's load areas made on Level 1, one area-based load holding both, its load 25 x the two areas in W; at 40 *already here at 25 W/m2 - left as it is*. **RECORD whether each load area sits inside its space** |
| **MI4** | *Node - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's third job, `--write`, rolled back | Negative, `switchboard`: nothing added, the kinds named. Positive: TX-1 a transformer in the Power Distribution System view |
| **MI5** | *Connect, and never disconnect.* By hand with `validate --write`, kept in order and rolled back at the end: `Utility` power source; `TX-1` transformer upstream `Utility`; `MDB` bus upstream `TX-1`; `TX-1` upstream `MDB`; `MDB` upstream `the transformer node`; the area-based load from MI3 upstream `MDB` | TX-1 under Utility and MDB under TX-1 in the view; TX-1 to MDB *already fed from "Utility" - Heron never disconnects*; `the transformer node` lists the nodes; the area-based load under MDB. **RECORD the total load each node reads** |
| **MI6** | *The second route.* Revit's own Wiring Types Add, Area Based Load tools and Power Distribution System by hand on copies of the same saved file | The same wire type, type, load and connections as the reports gave; Undo takes each run back as one entry |
