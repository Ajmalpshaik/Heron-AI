# Needs checking — Group JZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group JZ - a wall foundation put under each selected wall: `CREATE_WALL_FOUNDATION` (2026-11-01)

Asked for in the modeller's words: *"put a wall footing under these walls"*, *"add a strip foundation
under the external walls"*, *"wall foundation under all the structural walls on Level 0"* - Revit's
Structure > Foundation > Wall. Nothing in Heron made one: `PLACE_STRUCTURAL_FAMILY` places an isolated
footing family at points, and `PLACE_FOOTING_REBAR` reinforces isolated footings and names a wall
foundation as one it skips. `brain/heron_retrieve.py` on each sentence offered `PLACE_FOOTING_REBAR`,
`PLACE_WALL_SWEEPS`, `CREATE_FROM_ROOM_BOUNDARIES`, `PLACE_AREA_REINFORCEMENT`, `SET_WALL_CONSTRAINTS`,
`CREATE_WALL` and `CHECK_STRUCTURAL_SUPPORTS`.

| What | Where | What it does |
|---|---|---|
| `CREATE_WALL_FOUNDATION` (MODIFY) | [`brain/fragments/create-wall-foundation`](../../brain/fragments/create-wall-foundation/fragment.yaml) | A wall foundation of a named type under each selected wall, through `WallFoundation.Create`. A wall that already stands on one is skipped and named, never doubled; a wall Revit will not host one on is named with Revit's words and its wall kind; with no type named nothing is made and the model's wall foundation types are listed |

**Why a new card and not a widening.** `PLACE_STRUCTURAL_FAMILY` is PROVEN and point-based; a wall
foundation is a system element hosted by its wall, with no point and no family symbol. Nothing here
touches it.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). `WallFoundation.Create(Document,
ElementId, ElementId)`, `WallFoundation.WallId`, `WallFoundationType`, `WallType.Kind` and `WallKind`,
`Wall.IsStackedWallMember`, `Wall.WallType`, `Element.LevelId`, `ElementType.FamilyName`,
`Document.Regenerate` and `SubTransaction` were read off the shipped assemblies of all eight with
`tools/api-surface --members`. **Nothing here has run in Revit.** The card is DRAFT. Its risk is MODIFY; it
adds elements and deletes nothing. The add-in has no resolver row for `WallFoundationType`, so the type
arrives as text and the fragment matches it by its whole name or as *"Family: Type"*.

**Not known from the assemblies, and recorded here rather than guessed** (JZ5 and JZ6 measure them):
whether `WallFoundation.Create` hosts a foundation on a curtain wall (JZ5) or on a stacked wall - the
stacked wall itself, or one of its members Tab-selected (JZ6). The card refuses neither on a guess: each is
tried, and Revit's refusal, if any, is named with the wall's kind. And whether Revit makes a foundation on
a non-structural wall - the same rule applies.

**The model - JZ Structure in Revit 2020**, a scratch copy of Revit 2020's
`rst_advanced_sample_project.rvt` (`LIST_REBAR_TYPES` was proved on a copy of it, Group HY) in
`%TEMP%\heron-work\wall-foundation-jz`. **Which wall foundation types it holds, and which walls already
stand on one, are not known here.** Every run rolled back. The job file is
[`tools/jobs/create-wall-foundation-jz-2026-11-01.yaml`](../../tools/jobs/create-wall-foundation-jz-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its two plan views and its type are left for JZ1 to
read** and are marked in the file; until they are filled in, no view answers to them and the setup should
stop there, before the foundation runs - expected, NOT RUN. Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JZ0** | *Open.* JZ Structure open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name |
| **JZ1** | *Read the arrangement.* The wall foundation types off Revit's Type Selector under Structure > Foundation > Wall; a Structural Foundation schedule, or the plan views by eye, for which walls already stand on a wall foundation | A plan view with **a few walls** - under twenty - none on a wall foundation; a plan view whose walls **all** stand on one; a type. Write all three into the job file. **If the model has no wall foundation type, say so here and stop**: making one is not this group's to do. If no wall has one yet, JZ2's negative is arranged by running its positive inside the same rolled-back group first |
| **JZ2** | *Make, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: walls that already stand on one - `created` 0, each named *already has a wall foundation ... not doubled*, the foundation count unchanged. Positive: `created` equal to the walls, a line each *wall foundation made (<id>), type "<type>"*; each read in the same rolled-back group hosted by its wall |
| **JZ3** | *Not a wall.* The Structural Columns of the first view, `validate create-wall-foundation --write` with the same type | `created` 0, each named *not a wall*; refused *None of the N selected element(s) is a wall* |
| **JZ4** | *No type named, and a type the model has not got.* The first view's walls with `foundationType` left out, then with a name no type has | `created` 0 both times; refused naming **every** wall foundation type in the model, none chosen - even when there is only one |
| **JZ5** | *A curtain wall.* One curtain wall, selected by hand | **Record** whether a foundation is made and read back, or the wall named *a curtain wall ... NOT made - Revit refused* and Revit's words. Either is the card working; made-then-missing is a defect |
| **JZ6** | *A stacked wall.* One stacked wall, then one of its members Tab-selected | **Record** the same for each |
| **JZ7** | *Kept, through `try-once`, on the modeller's yes.* Make, as JZ2's positive, on a saved copy | Keep or Discard; afterwards a Structural Foundation schedule shows one new wall foundation row per wall `report` names - the second route |
| **JZ8** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The three sentences reach this card by identity; *"place the pad footings"* still reaches `PLACE_STRUCTURAL_FAMILY`. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept create-wall-foundation --by
"Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `heron_brain.lookup` on a scratch
store each, against this tree with and without the card, on Revit 2024 and 2025. The three sentences above
reach this card by identity; without it they reached `PLACE_FOOTING_REBAR` (*"put a wall footing under
these walls"*) and `PLACE_AREA_REINFORCEMENT` (the other two) - two writes that reinforce, ranked. Six
undeclared phrasings - *"add a wall foundation"*, *"strip footing under this wall"*, *"wall foundation
under these"*, *"foundation under the walls"*, *"put a foundation under this wall"* and *"add a
foundation"* - now reach this card ranked; before it each reached `PLACE_WALL_SWEEPS`,
`PLACE_FOOTING_REBAR`, `PLACE_AREA_REINFORCEMENT`, `CREATE_FROM_ROOM_BOUNDARIES` or, for the last,
`CHECK_STRUCTURAL_SUPPORTS`. No routing table claims any of the six. **No declared sentence moved**: every
utterance of `PLACE_STRUCTURAL_FAMILY`, `PLACE_FOOTING_REBAR`, `CREATE_WALL`, `CREATE_FLOOR`,
`CHECK_STRUCTURAL_SUPPORTS`, `SET_WALL_CONSTRAINTS`, `PLACE_WALL_SWEEPS`, `ATTACH_WALLS` (2025),
`CREATE_FROM_ROOM_BOUNDARIES`, `PLACE_AREA_REINFORCEMENT`, `PLACE_FABRIC_REINFORCEMENT`,
`PLACE_LINE_BASED_FAMILY`, `CREATE_WALLS_BY_FACE` and `REPORT_COMPOUND_STRUCTURE` reaches its own card by
identity, before and after. `tools/check-routing.py` on a fresh store: no question answered by a write,
*CLAIMED IN A ROUTING TABLE AND NOT REACHED* 65 before and after, the same rows; two declared questions now
rank this card first on the keyword route alone - *"what type is this wall"* (`DESCRIBE_ELEMENTS`) and
*"what is this wall made of"* (`REPORT_COMPOUND_STRUCTURE`) - and each still reaches its own card by
identity.

**Open, recorded and not answered by inventing utterances.** *"put footings under the walls"*, *"add wall
footings"* and *"add continuous footings"* reach `PLACE_FOOTING_REBAR` ranked, before this card and after
it - a write that reinforces isolated footings, which refuses a wall and would place nothing here. Nobody
has yet said them; if the modeller does, they are this card's to declare. And on Revit 2024, where
`ATTACH_WALLS` is not offered, *"attach the bases of these walls to the floor below"* reached
`CREATE_PARTS` ranked before this card and `CREATE_FROM_ROOM_BOUNDARIES` after - neither this card nor the
right one, moved by the keyword weights the new card shifts.
