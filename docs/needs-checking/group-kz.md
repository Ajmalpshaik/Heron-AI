# Needs checking — Group KZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KZ - which load cases, loads and boundary conditions a model has: `REPORT_STRUCTURAL_LOADS` (2026-11-01)

Asked for in the modeller's words: *"what load cases are in this model"* and *"list the loads on this
beam"* - Revit's Analyze > Load Cases, Loads and Boundary Conditions - beside four writes, Groups
[LA](group-la.md), [LB](group-lb.md) and [LC](group-lc.md). No card read or wrote any of them: a grep of
every fragment's C# that day for `LoadCase`, `LoadNature`, `PointLoad`, `LineLoad`, `AreaLoad` and
`BoundaryConditions` found nothing. `REPORT_ANALYTICAL_MODEL` reads the analytical members and panels -
their end points and physical elements - and no load, case or support, and is 2023 on only:
a look-alike. `CHECK_STRUCTURAL_SUPPORTS` is a geometric sweep, not a boundary condition.
`ADD_LOAD_CLASSIFICATION`, `READ_SPACE_LOADS` and `REPORT_PANEL_CIRCUITS` mean electrical or heating and
cooling load - a different job sharing a word.

| What | Where | What it does |
|---|---|---|
| `REPORT_STRUCTURAL_LOADS` (READ) | [`brain/fragments/report-structural-loads`](../../brain/fragments/report-structural-loads/fragment.yaml) | Lists the **load natures**, the **load cases** with number, nature, category and how many loads each holds, every **point, line and area load** with its point or ends in mm, its force in kN, kN/m or kPa, its load case and the beam, column, wall or floor it sits on (through its analytical element), and every **boundary condition** with its kind, place, fixity and host (`report`, `loadCount`, `caseCount`, `boundaryCount`, `names`). `show` picks lists, `loadCase` narrows the loads, a selection narrows loads and supports to those elements; an element with no analytical element is named in `noAnalytical`, never read as carrying no load. Opens no transaction |

**The read half of a set** - every question about load cases, natures, loads and boundary conditions is
answered here, so none reaches `CREATE_LOAD_CASE`, `PLACE_STRUCTURAL_LOADS` or `PLACE_BOUNDARY_CONDITIONS`.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `LoadNature`, `LoadCase`, `LoadBase`, `PointLoad`, `LineLoad`,
`AreaLoad` and `BoundaryConditions` carry the members it reads with the same signatures 2020-2027; the host's
physical element is `AnalyticalModel.GetElementId()` 2020-2022 and
`AnalyticalToPhysicalAssociationManager.GetAssociatedElementId` from 2023. Forces go through
`UnitUtils.ConvertFromInternalUnits` - `DisplayUnitType` in 2020, `UnitTypeId` from 2021. **Nothing here has
run in Revit.** The card is DRAFT. **Only Revit can settle** that the values read back equal what each load's
Properties palette shows, and how the fixity parameters read (`AsValueString`) - KZ3 checks both.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `243861c` without the four structural load cards and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020 and 2024, with and without the host's question flag - over every declared utterance and routing-table sentence of a live card (4,351 sentences, five probes included). *"what load cases are in this model"* and *"list the loads on this beam"* reach this card with and without the flag (`ADD_LOAD_CLASSIFICATION` and `COPE_FRAMING`, writes, without it before; `READ_SPACE_LOADS` and `REPORT_SPACE_ENVELOPE` with it). So do the probes *"what loads are in this model"*, *"list the load cases"* and *"which load case is this load in"*. With the flag, every write sentence of the set reaches this read. **Without the flag no other sentence any card declares or claims moved** but *"give me the analytical model for the structural engineer"* on 2020, which `REPORT_ANALYTICAL_MODEL` (2023 on) cannot take there - `SELECT_FROM_LINK` to this card, read to read. With it, nine writes' own sentences moved, each read to read, none from a card that declares or claims it. The first draft pulled two of `REPORT_ANALYTICAL_MODEL`'s questions to a write on 2020 - closed in the same change, [row 5b-1380](../fragment-issues/section-5b-rows-401-425.md). **Still reaching a write without the flag:** the probes *"which loads are on this beam"* (`PLACE_STRUCTURAL_LOADS`; `CREATE_BEAM_SYSTEM` before) and *"show me the boundary conditions"* (`PLACE_BOUNDARY_CONDITIONS` on 2020, `MEASURE_PERIMETER` with the flag) - nobody's words, so not declared ([row 5b-1381](../fragment-issues/section-5b-rows-401-425.md)). `tools/check-routing.py` on a scratch store: no question answered by a write; *claimed in a routing table and not reached* 71 before and after, the same rows.

**The model - a scratch project new from Revit's metric STRUCTURAL template in Revit 2020**, saved as
`loads-kz` under `%TEMP%\heron-work` - never a working model. Groups LA, LB and LC use it too. Nothing
below changes the model. The job file is
[`tools/jobs/report-structural-loads-kz-2026-11-01.yaml`](../../tools/jobs/report-structural-loads-kz-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KZ1** | *Arrange, by hand.* On Level 1: steel beam **B1** between concrete columns **C1** and **C2**, structural floor **S1**, structural wall **W1**, and architectural wall **A1** with Structural cleared. Analyze > Load Cases: add **DL1** (Dead) and **LL1** (Live). Analyze > Loads: a 5 kN/m line load down on B1 in DL1, a 10 kN point load down at C1's top in LL1, a 2.5 kPa area load down on S1 in DL1. Analyze > Boundary Conditions: a Pinned point at C1's base. Save. Write down every case, nature and load value | The arrangement every LA, LB and LC row uses |
| **KZ2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, the walls (W1, A1): `loadCount` 0, `noAnalytical` A1 only, findings *No load is hosted on the analytical element(s) of what was handed in*. Positive, B1: `loadCount` 1 - B1's line load, uniform (0, 0, -5) kN/m, load case DL1, on Structural Framing ... (B1) |
| **KZ3** | *The second route.* The card with nothing selected and `show`, `loadCase` left out | Every nature and case as Analyze > Load Cases shows them (DL1 2 loads, LL1 1), the three loads with the values KZ1 wrote down, and C1's boundary condition with its fixity as its Properties palette reads |
| **KZ4** | *A load case nobody has.* `loadCase` "ZZNOTACASE" | No Loads section, `loadCount` 0; findings name every load case - nothing guessed |
| **KZ5** | *Revit 2023 or later.* KZ2 and KZ3 again on a project made the same way in that release | The same answers - the host named through the association manager |
