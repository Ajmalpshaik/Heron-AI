# Needs checking — Group LB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LB - point, line and area loads in a load case: `PLACE_STRUCTURAL_LOADS` (2026-11-01)

Asked for in the modeller's words: *"put a 10 kN point load on this beam end"*, *"add a 5 kN/m line load
along this beam"* and *"add a 2.5 kPa area load on this slab"* - Revit's Analyze > Loads, hosted or free. No
card placed a load: a grep of every fragment's C# that day for `PointLoad`, `LineLoad` and `AreaLoad` found
nothing. `PLACE_AREA_REINFORCEMENT` also goes "on this slab", but places rebar - a look-alike in words.
**One write for three kinds, on purpose:** a point, line and area load share every rule and differ only in
which `Create` is called.

| What | Where | What it does |
|---|---|---|
| `PLACE_STRUCTURAL_LOADS` (MODIFY) | [`brain/fragments/place-structural-loads`](../../brain/fragments/place-structural-loads/fragment.yaml) | Places a **point load** in kN at a member's start or end, a **line load** in kN/m along a beam, brace or column, or an **area load** in kPa over a floor, wall or foundation, on each selected element's **analytical element** - or **free** at points in mm on 2020 to 2023 - in **one named load case**, each read back with its values (`placed`, `report`, `placedIds`). Never changes or deletes a load; one that would repeat a load on that host, case, kind and force is skipped. No load case, a case nobody has, a point load with no end, or a value of 0 is refused; the direction left out is down (project -Z) and the answer says so. An element with no analytical element is named in `noAnalytical` and gets nothing |

**One of a set.** `REPORT_STRUCTURAL_LOADS` ([Group KZ](group-kz.md)) answers every question about loads,
so none reaches this write; `CREATE_LOAD_CASE` ([Group LA](group-la.md)) makes the case it needs.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: hosted creation is `PointLoad.Create(Document, AnalyticalModelStick,
AnalyticalElementSelector, ...)`, `LineLoad.Create(Document, AnalyticalModelStick, ...)` and
`AreaLoad.Create(Document, AnalyticalModelSurface, ...)` 2020-2022, and the same three taking the analytical
`ElementId` 2023-2027. **The free `Create`s - a point, two points or curve loops with no host - are present
2020-2023 and gone at 2024**, and RevitAPI.xml 2024-2027 documents no unhosted route, so from 2024 a free
load is refused with that reason. Values go through `UnitUtils.ConvertToInternalUnits` - `DisplayUnitType` in
2020, `UnitTypeId` from 2021. **Nothing here has run in Revit.** The card is DRAFT. **Only Revit can settle**
that a 5 kN/m load reads 5 kN/m in its Properties palette (the unit route), and where a point load at a
column's base or top lands - LB3 and LB5 check them.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `243861c` without the four structural load cards and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020 and 2024, with and without the host's question flag - over every declared utterance and routing-table sentence of a live card (4,351 sentences, five probes included). *"put a 10 kN point load on this beam end"*, *"add a 5 kN/m line load along this beam"* and *"add a 2.5 kPa area load on this slab"* reach this card without the flag (`PLACE_LINE_BASED_FAMILY` twice and `PLACE_AREA_REINFORCEMENT` before) and `REPORT_STRUCTURAL_LOADS` with it. No sentence `PLACE_AREA_REINFORCEMENT` or any other card declares or claims moved to this card. The first draft's `purpose` pulled two of `REPORT_ANALYTICAL_MODEL`'s questions here on 2020 - closed in the same change, [row 5b-1380](../fragment-issues/section-5b-rows-401-425.md). The probe *"which loads are on this beam"* reaches this card without the flag (a write before) and the read with it ([row 5b-1381](../fragment-issues/section-5b-rows-401-425.md)).

**The model - Group KZ's `loads-kz` scratch project** (KZ1), never a working model. Every write here is
ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/place-structural-loads-lb-2026-11-01.yaml`](../../tools/jobs/place-structural-loads-lb-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LB1** | *Arrange.* Open `loads-kz`; read every load with `REPORT_STRUCTURAL_LOADS` (KZ3) | The three loads KZ1 drew |
| **LB2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, the walls (W1, A1), a 7.5 kN/m line load in DL1: `placed` 0, `noAnalytical` A1, `skipped` W1 as not a member, no load anywhere and no free load. Positive, B1: `placed` 1, "uniform (0, 0, -7.5) kN/m; load case "DL1"; on Structural Framing ... (B1)" read back |
| **LB3** | *The second route.* LB2's positive kept once with `try-once`; B1's new load in its Properties palette | Load case DL1, Fz -7.5 kN/m - the unit route checked; KZ1's 5 kN/m load still there unchanged |
| **LB4** | *Never doubled.* LB2's positive again after LB3 | `placed` 0, `skipped` names B1 and the 7.5 kN/m load already there |
| **LB5** | *Point and area.* B1, `kind` point, `loadCase` LL1, `value` 10, `at` end; then S1, `kind` area, `loadCase` DL1, `value` 2.5; then B1 point with `at` left out | A point load at B1's end, (0, 0, -10) kN; an area load over S1, (0, 0, -2.5) kPa; with no end, refused giving B1's two end points |
| **LB6** | *Free, and the release line.* Nothing selected, `kind` point, `loadCase` DL1, `value` 10, `points` "0,0,3000" - on Revit 2020, then on 2024 or later | 2020: a free point load at (0, 0, 3000) mm. 2024 on: `placed` 0, refused naming the release |
| **LB7** | *Revit 2023 or later.* LB2 on a project made the same way in that release | The same answers, the load on B1's AnalyticalMember |
