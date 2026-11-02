# Needs checking — Group LL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LL - reference planes drawn in a project from a named grid or level: `CREATE_PROJECT_REFERENCE_PLANES` (2026-11-02)

Asked for in the modeller's words: *"draw a reference plane along grid B in this plan"*, *"add a reference plane
named Ceiling Datum 2700 above Level 1"* and *"add reference planes at 600 centres here"* - Revit's Architecture >
Work Plane > Ref Plane, in a project. No card drew one: a grep of every fragment's C# that day for
`NewReferencePlane` found it only in family cards. `CREATE_REFERENCE_PLANES` (PROVEN) makes planes in the family
being edited and refuses a project in its own code - its proof's negative case is that refusal on Project1 - so
this is a separate job, not a second tool for one; `SET_REFERENCE_PLANE_LENGTH` sets the length of planes already
there and makes none - look-alikes, not duplicates.

| What | Where | What it does |
|---|---|---|
| `CREATE_PROJECT_REFERENCE_PLANES` (MODIFY) | [`brain/fragments/create-project-reference-planes`](../../brain/fragments/create-project-reference-planes/fragment.yaml) | Draws reference planes **in a project**, set out from **one grid or level named by the modeller - never guessed**: none named, or a name nothing has, makes nothing and lists the grids and levels. **From a straight grid**: vertical planes along it ("0") or parallel to it, in **mm east** of it (north of one running east-west), drawn in a **plan**. **From a level**: horizontal planes in **mm above** it, drawn in a **section or elevation**, 10 m either side of the view's middle. `planes` "name=mm; mm" and/or `spacingMm` with `count` for equal centres. The view named, or **the view in front**; a template, an arc grid and a kind of view the planes cannot be drawn in are **refused by name**. A **name already used**, a name given twice and a place a plane **already lies** are **skipped and named**, the rest made. **Never moves, renames or deletes.** Each plane **read back** - facing, distance from the datum, name - and one that does not read back as asked rolls the whole call back (`created`, `skipped`, `report`, `refused`). A family in front is refused, naming `CREATE_REFERENCE_PLANES` |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `ItemFactoryBase.NewReferencePlane(XYZ, XYZ, XYZ, View)`, `ReferencePlane.Name`,
`Normal` and `GetPlane`, `Grid.Curve` and `IsCurved`, `Level.ProjectElevation`, the `View` members it reads and
`BuiltInParameter.DATUM_TEXT` are the same 2020 to 2027. **Nothing here has run in Revit.** The card is DRAFT.
**Only Revit can settle** whether `NewReferencePlane` with a section as its view and the view direction as its
cut vector gives a horizontal plane in a project as it does in the Family Editor; whether `ProjectElevation` is
the height a plane drawn in a section lands at when the project base point is moved; and what Revit says to a
plane name set twice - LL2 to LL4 check them.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores before and after (`model` backend, no
re-ranker; no filter, Revit 2020, 2024 and 2025, with and without the host's question flag), over every declared
sentence, every routing-table claim and ten probes. The three sentences reach this card by identity - before,
`DRAW_FAMILY_DETAIL_LINES`, `MEASURE_CEILING_HEIGHT` and `SET_FAMILY_PLANE_REFERENCE`. No declared sentence and no
routing-table claim changed card without the question flag. With it, six change sentences of write cards
reshuffled between two reads each - change requests asked as questions, which no write may answer either way.
`tools/check-routing.py` (its own scratch store): no question answered by a write, every routing row names a
card, one new contest on the keyword route alone - *"put a ceiling at 2700"*, `CREATE_FLOOR`'s, answered by
identity through `find` and written in both cards. The modeller's work-plane sentences - *set the work plane to
Level 2*, *show the work plane in this view*, *which work plane is this view on* - reach this card by ranking,
the question too without the flag; nothing sets, shows or reads a view's work plane yet
([row 5b-1392](../fragment-issues/section-5b-rows-401-425.md), OPEN).

**The model - `refplane-ip`**, a scratch project from the default architectural template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is
[`tools/jobs/create-project-reference-planes-ll-2026-11-02.yaml`](../../tools/jobs/create-project-reference-planes-ll-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LL1** | *Arrange.* `refplane-ip`: levels "Level 1" at 0 and "Level 2" at 4000 mm; grids "A" 0,0 to 0,20000 mm and "B" 6000,0 to 6000,20000 mm; "Section 1" looking north across both; one reference plane "Existing RP" drawn along grid A. Save | Two grids, one reference plane |
| **LL2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, `{3D}`: nothing made, refused *"{3D}" is a ThreeD view. A reference plane along a grid is drawn in a plan*. Positive, datum "B", planes "0" in "Level 1": `created` 1, `skipped` empty, the row *an unnamed plane along grid B* |
| **LL3** | *Heights, spacing, never guessed.* Datum "Level 1", planes "Ceiling Datum=2700" in "Section 1"; datum "B", `spacingMm` 600, `count` 5 in "Level 1"; the datum left out; datum "Level 1" in the floor plan | A horizontal plane 2700 above Level 1, its Name "Ceiling Datum"; five planes 600 apart east of B; the third refused listing grids A, B and both levels; the fourth refused listing "Section 1" |
| **LL4** | *Skipped, never twice; the second route.* Datum "A", planes "New RP=0; Other=1000"; datum "A", planes "Existing RP=3000"; a plane drawn by hand with Pick Lines at 600 off grid B beside the one made here; one Ctrl+Z | New RP skipped naming Existing RP, Other made; the second makes nothing and names the name in use; Existing RP not moved or renamed; 0 between the hand-drawn plane and this one; one undo takes every plane the run made |
