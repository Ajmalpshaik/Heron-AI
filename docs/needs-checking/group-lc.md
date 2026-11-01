# Needs checking — Group LC

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LC - fixed, pinned or roller boundary conditions: `PLACE_BOUNDARY_CONDITIONS` (2026-11-01)

Asked for in the modeller's words: *"add a pinned boundary condition at these column bases"* - Revit's
Analyze > Boundary Conditions (point, line, area). No card placed one: a grep of every fragment's C# that day
for `BoundaryConditions` found nothing. `CHECK_STRUCTURAL_SUPPORTS` finds column bases bearing on nothing,
geometrically; `ATTACH_COLUMNS` attaches a column's base to a footing - neither is an analytical support.

| What | Where | What it does |
|---|---|---|
| `PLACE_BOUNDARY_CONDITIONS` (MODIFY) | [`brain/fragments/place-boundary-conditions`](../../brain/fragments/place-boundary-conditions/fragment.yaml) | Places a **point** boundary condition at each selected column's base or top or beam's end, a **line** along a beam or brace, or an **area** over a floor, wall or foundation - **Fixed, Pinned or Roller** - on each element's analytical element, each read back with the preset Revit shows (`placed`, `report`, `placedIds`). Never changes or deletes one; one already on that host, kind and end is skipped. No fixity or a point with no end is refused. An element with no analytical element is named in `noAnalytical` |

**One of a set.** `REPORT_STRUCTURAL_LOADS` ([Group KZ](group-kz.md)) answers every question about boundary
conditions, so none reaches this write.

**What is done and what is not - and why only four releases.** It compiles for the four it claims - 2020,
2021, 2022 and 2027 (`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped
assemblies and RevitAPI.xml: 2020-2022 make them through `Document.Create.NewPointBoundaryConditions` and
`NewLineBoundaryConditions` with a reference from `AnalyticalModel.GetReference(AnalyticalModelSelector)` -
documented as *a beam's, brace's or column's analytical line end* - and `NewAreaBoundaryConditions` on the
wall, slab or foundation; 2027 through `BoundaryConditions.CreatePointBoundaryConditions`,
`CreateLineBoundaryConditions` and `CreateAreaBoundaryConditions` on the analytical element's id. **2023 to
2026 have only the `Document.Create` methods, whose documentation still asks for an analytical line end's
reference, and `AnalyticalModel`, which gave it, is gone at 2023** - no documented member gives an
`AnalyticalMember`'s end as a `Reference`. So the card does not claim those releases, and on them the C#
refuses with the reason; LC5 is the run that would widen it. **Nothing here has run in Revit.** The card is
DRAFT. **Only Revit can settle** that Fixed / Release passed per axis reads back as Revit's Fixed, Pinned or
Roller preset - LC3 checks it.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `243861c` without the four structural load cards and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020 and 2024, with and without the host's question flag - over every declared utterance and routing-table sentence of a live card (4,351 sentences, five probes included). *"add a pinned boundary condition at these column bases"* reaches this card without the flag on 2020 and with no filter (`ATTACH_COLUMNS` before) - and still `ATTACH_COLUMNS` on 2024, where this card claims nothing ([row 5b-1381](../fragment-issues/section-5b-rows-401-425.md), OPEN). With the flag it reaches `SELECT_BY_PIN_STATE`, as before. No sentence any other card declares or claims moved to this card. The probe *"show me the boundary conditions"*, nobody's words, reaches this card without the flag on 2020 (`MEASURE_PERIMETER`, a read, before) - recorded in the same row.

**The model - Group KZ's `loads-kz` scratch project** (KZ1) on Revit 2020, never a working model. Every write
here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/place-boundary-conditions-lc-2026-11-01.yaml`](../../tools/jobs/place-boundary-conditions-lc-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LC1** | *Arrange.* Open `loads-kz`; read the boundary conditions with `REPORT_STRUCTURAL_LOADS` `show` "boundary" | C1's Pinned point at its base, KZ1's |
| **LC2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, the walls (W1, A1), point at base, pinned: `placed` 0, `noAnalytical` A1, `skipped` W1 as not a member. Positive, the columns (C1, C2): `placed` 1 - a Point at C2's base, Pinned expected in "Revit shows it as ..." - and `skipped` C1 with its support already there |
| **LC3** | *The second route.* LC2's positive kept once with `try-once`; C2's new boundary condition in its Properties palette and its symbol in a 3D view | X, Y, Z translation Fixed, rotations Released, the preset Revit shows; C1's support unchanged |
| **LC4** | *Line, area, never guessed.* B1 line fixed; S1 area roller; C2 point with `fixity` left out; C2 point with `at` left out | A Line along B1, Fixed; an Area over S1, Z only fixed; refused asking for the fixity; refused giving C2's base and top |
| **LC5** | *Revit 2023 to 2026 - the release line.* LC2 on a project made the same way in Revit 2024 | `placed` 0, refused naming the release - the card claims 2027 and 2020-2022 only. A route found by hand here would widen it |
| **LC6** | *Revit 2027.* LC2 and LC3 on a project made the same way in Revit 2027 | The same answers through `BoundaryConditions.Create*` |
