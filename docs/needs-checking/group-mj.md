# Needs checking — Group MJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MJ - Massing & Site before toposolids and the property line - make a toposurface from survey points, add a building pad at a level or a subregion on a toposurface, and draw a property line through points or from a table of distances and bearings: `CREATE_TOPOSURFACE`, `CREATE_PROPERTY_LINE` (2026-11-02)

Asked for in the modeller's words: *"make a toposurface from these points"*, *"add a building pad at Level 0 inside
these corners"*, *"add a subregion for the car park on the toposurface"*, *"draw the property line through these
points"* and *"add a property line from this bearing table"* - Revit's Massing & Site tab: the toposurface, its building
pads and subregions that Revit 2020 to 2023 has in place of the toposolid, and Property Line. No card made any of them:
a grep of every fragment that day for `TopographySurface`, `BuildingPad`, `SiteSubRegion` and `PropertyLine` found only
`REPORT_MASSES`, which reads the property lines' area for a plot ratio, and `brain/heron_retrieve.py` sent the five
requests to `DRAW_FAMILY_POINT_CURVES`, `PLACE_FAMILY_INSTANCES`, `ADD_DATUM_LEADERS` and `WRITE_FAMILY_SIZE_TABLE`.
heron-duplicate-checker's method found look-alikes only - `CREATE_TOPOSOLID` and `EDIT_TOPOSOLID` (its mode subdivide,
which the deprecated `ADD_TOPOSOLID_SUBDIVISION` was merged into) make and edit the TOPOSOLID, an element Revit 2024
brought and absent before; `CREATE_FLOOR` makes a floor, not a pad cut into a toposurface;
`CREATE_ROOM_SEPARATION_LINES` and `DRAW_FAMILY_POINT_CURVES` draw other lines through points - so two new cards, both
writes. The toposurface, the pad and the subregion share one card: each is one Create call on the same old site from
a list of points in millimetres, and a mode chooses which.

| What | Where | What it does |
|---|---|---|
| `CREATE_TOPOSURFACE` (MODIFY) | [`brain/fragments/create-toposurface`](../../brain/fragments/create-toposurface/fragment.yaml) | One run makes **one** thing, by `mode`: a **toposurface** through points `x,y,z` with heights in millimetres above the internal origin (`CREATE_TOPOSOLID`'s rule), three or more, two at one x,y refused; a **building pad** of a named type at a named level inside corners `x,y`, closed automatically and drawn at the level's height; or a **subregion** on a toposurface named by id inside corners `x,y`. A level, pad type or host **not named is taken only when the model has exactly one**, otherwise refused with **the list**. Read back: the toposurface's points, edge points, lowest and highest heights and projected and surface areas; the pad's type, level, height offset, the toposurface it cut into, its edges and area; the subregion's edges, projected area and **whether its host now lists it**. Revit 2020 to 2023; on a later release it refuses and names the toposolid's words |
| `CREATE_PROPERTY_LINE` (MODIFY) | [`brain/fragments/create-property-line`](../../brain/fragments/create-property-line/fragment.yaml) | Draws **one property line**, one of two ways as Revit's own dialog offers: **through points** `x,y` in millimetres, kept as given - **open unless the last point repeats the first** - checked by Revit's `IsValidBoundary`; or **from a table** of rows `distance, bearing` - millimetres and a quadrant bearing (`N45-30-00E`, `S12.5W`) or degrees clockwise from north - with arc rows `distance, bearing, arc, radius, left/right`, each row checked by Revit, and an optional start point. Both or neither refused. Read back: sketched or from a table, **closed with its area or open**, its start point, and **each row's distance and bearing as Revit holds them beside what was typed**. Revit 2027 only |

**What is done and what is not.** `CREATE_TOPOSURFACE` compiles for 2020 to 2023 and `CREATE_PROPERTY_LINE` for
2027, the releases they claim (`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped
assemblies with `tools/api-surface --members`: `TopographySurface.Create(Document, IList<XYZ>)`, `SiteSubRegion.Create`
and `BuildingPad.Create` are present **2020 to 2027**, and the first two are **marked obsolete from 2024**, when
toposolids replaced the toposurface - so the card stops at 2023 and points 2024 on at `CREATE_TOPOSOLID` and
`EDIT_TOPOSOLID`; `PropertyLine.Create` (both forms), `PropertyTableEntry` and every member the card reads back are
**2027 only** - before 2027 the API offers no way to make a property line. **Nothing here has run in Revit.** Both
cards are DRAFT. **Only Revit can settle**: whether `BuildingPad.Create` finds the toposurface under an outline drawn
at the level's height on its own; whether `SiteSubRegion.Create` cares about the outline's height (it is drawn at the
internal origin's); and **which way, and from which north, Revit measures a `PropertyTableEntry`'s bearing** -
RevitAPI.xml says only "an angle in 0 to 2*PI", and the card passes clockwise from north. MJ3 to MJ5 RECORD each.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `dd31db0` and of this change (`model` backend; no Revit filter; with and without the host's question flag), over every declared utterance, identity and routing-table sentence of every live card (5,799 sentences). Without the flag the five requests reach these cards. *"draw a new duct"*, claimed for `CREATE_MEP_RUNS` by `SPLIT_MEP_RUN`'s routing table, moved without the flag to `CONVERT_TO_FLEX_DUCT`, a coin toss of 0.4 of one rank, and is declared on `CREATE_MEP_RUNS`; declaring it sent *"how long is this duct run"* from `MEASURE_RUN_QUANTITIES` to `CREATE_MEP_RUNS`, so that one is declared on `MEASURE_RUN_QUANTITIES` - both PROVEN cards, outside `impl/`, fingerprints unchanged - [row 5b-1439](../fragment-issues/section-5b-rows-401-425.md), FIXED, which also records two sentences answered by neither claimant before or after and thirteen read-to-read moves with the flag, not tuned. `tools/check-routing.py` on a scratch store finds no question answered by a write and the same 66 routing-table sentences unreached as before; `tests/test_routing_phrases.py` fails exactly its known seven.

**The models.** Two, because the cards share no release - **`site-mj-2023`**, a scratch project from Revit 2023's
metric architectural template, and **`site-mj-2027`**, the same from Revit 2027's - each saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file
is [`tools/jobs/toposurface-property-line-mj-2026-11-02.yaml`](../../tools/jobs/toposurface-property-line-mj-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process
id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MJ1** | *Arrange.* `site-mj-2023`: Massing & Site > Toposurface by hand, four points at 0,0 / 60000,0 / 60000,40000 / 0,40000 mm, elevation 0 - toposurface A; one wall on Level 1 - wall W; both ids **into the job file** (Manage > IDs of Selection); note the building pad types the template has. `site-mj-2027`: nothing drawn. A site plan in front of each. Save | The ids, and the pad type names as the type selector shows them |
| **MJ2** | *Subregion - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back; then by hand: host left out (the model has one toposurface - A is taken and named) | Negative, wall W's id: nothing made, the toposurfaces listed. Positive: a subregion on A, **300 m2** in its Properties, listed among A's subregions. **RECORD whether the subregion lies on A's surface, not at the internal origin's height** |
| **MJ3** | *Toposurface and building pad, by hand with `validate --write`, rolled back.* `mode toposurface`, `pointsMm "100000,0,0; 130000,0,0; 130000,20000,1500; 100000,20000,1500; 115000,10000,2500"`; then `"0,0,0; 1000,0,0; 1000,0,500"`. `mode pad`, corners `"10000,10000; 30000,10000; 30000,25000; 10000,25000"`, `level "Level 1"`, typeName left out; then `typeName "No Such Pad"` | A toposurface of **5 points, heights 0 to 2500 mm, 600 m2 projected**; the second refused, two points at one x,y. A pad on Level 1, **300 m2**, cut into A; `No Such Pad` lists the types. typeName left out never read as a type called "none". **RECORD the pad's host as Revit shows it** |
| **MJ4** | *Property line by points - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, on `site-mj-2027`, `--write`, rolled back; then by hand `pointsMm "0,0; 40000,0; 40000,25000"` | Negative, the edges crossing: nothing made. Positive: a sketched property line, **closed, 1000 m2** in its Properties; the three points an open line, *open - no area* |
| **MJ5** | *Property line by table, by hand with `validate --write`, rolled back.* `table "40000, N90E; 25000, S0E; 40000, S90W; 25000, N0E"`, `startMm "100000,0"`; then `table "40000, N95E; 25000, S0E"` | A table-based line, **closed, 1000 m2**, starting at 100000,0; the bearings held **beside those typed - 90, 180, 270, 0**. **RECORD them and Revit's own table's bearings (Edit Table)**: a difference is the API's convention and the card is corrected before any proof. `N95E` refused, nothing made |
| **MJ6** | *The second route.* Revit's own Toposurface, Building Pad, Subregion and Property Line (sketch and table) by hand on copies of the same saved files | The same areas, levels, types and bearings as the reports gave; Undo takes each run back as one entry |
