# Needs checking — Group KB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KB - a foundation slab from typed corners: `CREATE_FOUNDATION_SLAB` (2026-11-01)

Asked for in the modeller's words: *"make a 600 thick raft under the building"*, *"add a foundation slab
on Level 0 from these corners"*, *"pour a mat foundation inside these walls"* - Revit's Structure >
Foundation > Slab. Nothing in Heron made one: `CREATE_FLOOR` makes an ordinary floor,
`CREATE_WALL_FOUNDATION` the strip under a wall and `PLACE_STRUCTURAL_FAMILY` an isolated footing family
at points. `brain/heron_retrieve.py` on each sentence, Revit 2024, offered `PLACE_AREA_REINFORCEMENT`
first for two and `CREATE_WALL_FOUNDATION` for the third.

| What | Where | What it does |
|---|---|---|
| `CREATE_FOUNDATION_SLAB` (MODIFY) | [`brain/fragments/create-foundation-slab`](../../brain/fragments/create-foundation-slab/fragment.yaml) | One foundation slab of a named Foundation Slab type from a closed boundary of corners on one level - `CREATE_FLOOR`'s boundary writing. An ordinary floor type is refused by name; with no type named nothing is made and the model's foundation slab types are listed with their thicknesses. The slab's category is read back, and one that is not in Structural Foundations is taken back, never kept. Area and Thickness read back off the slab |

**Why a new card and not a widening.** `CREATE_FLOOR` is PROVEN and makes an architectural floor; its
calls take no foundation flag, and widening it would change its job and stale its proof. Its `impl/` is
untouched; its card gained one declared sentence (below, row 5b-1372).

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `Creation.Document.NewFoundationSlab(CurveArray, FloorType,
Level, bool, XYZ)` is present 2020-2022, **obsolete at 2022 and gone from 2023**; `Floor.Create(Document,
IList<CurveLoop>, ElementId, ElementId, bool, Line, double)` is present 2022-2027. The fragment takes the
first under `#if REVIT2020 || REVIT2021` and the second otherwise. `FloorType.IsFoundationSlab`,
`CompoundStructure.GetWidth`, `Level.ProjectElevation`, `new ElementId(BuiltInCategory)`,
`OST_StructuralFoundation`, `HOST_AREA_COMPUTED`, `FLOOR_ATTR_THICKNESS_PARAM`,
`STRUCTURAL_FOUNDATION_THICKNESS` and `SubTransaction` are present 2020-2027. **Nothing here has run in
Revit.** The card is DRAFT; its risk is MODIFY - it adds one element and deletes nothing.

**Not known from the assemblies, and recorded here rather than guessed** (KB3 measures it): whether
`Floor.Create` with a foundation slab type makes an element in **Structural Foundations**. `RevitAPI.xml`
2022-2027 says only *"a new instance of floor"*, and the obsolete message on `NewFoundationSlab` says only
*"call Floor.Create()"*. The fragment reads the category back inside a SubTransaction and takes back a slab
in any other category, naming it - so on 2022 and later it either makes a foundation slab or makes
nothing. Also not known: which thickness parameter a foundation slab carries - the fragment reads
`FLOOR_ATTR_THICKNESS_PARAM`, then `STRUCTURAL_FOUNDATION_THICKNESS`, then the type's layers, and says
which it used.

**The model - KB Structure in Revit 2020**, a scratch copy of Revit 2020's
`rst_advanced_sample_project.rvt` in `%TEMP%\heron-work\foundation-slab-kb`; KB3 on a Revit 2024 copy of
the same. **Which foundation slab types, floor types and levels it holds is not known here.** Every run
rolled back. The job file is
[`tools/jobs/create-foundation-slab-kb-2026-11-01.yaml`](../../tools/jobs/create-foundation-slab-kb-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its level and two types are left for KB1 to read** and
are marked in the file; until they are filled in the run should stop at the level - expected, NOT RUN.
Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected,
and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KB0** | *Open.* KB Structure open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name |
| **KB1** | *Read the arrangement.* A level; the foundation slab types off the Type Selector under Structure > Foundation > Slab, with each one's thickness in Edit Type; one ordinary floor type under Architecture > Floor; the plan 200 m east of the origin | Write the level and both types into the job file. The 20 m by 12 m patch is clear, or the boundary is moved in both legs alike. **If the model has no foundation slab type, say so here and stop** |
| **KB2** | *Make, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the ordinary floor type - `created` none, refused naming it an ordinary floor type and listing every foundation slab type with its thickness; no floor and no foundation added. Positive: one id; `report` *in Structural Foundations: Area 240 m2, Thickness* the type's; findings name the pre-2022 call |
| **KB3** | *The 2022-and-later call.* KB2's job on a Revit 2024 copy, the same values read again by KB1 there | **Record** which: the slab made and read back *in Structural Foundations*, or refused *Revit made the slab in "<category>", not in Structural Foundations, so it was taken back*. Either is the card working; made-then-kept in another category is a defect |
| **KB4** | *No type, and a thickness for a type.* KB2's level and boundary with `foundationType` left out, then `600` | `created` none both times; every foundation slab type named with its thickness, none chosen - even when there is only one |
| **KB5** | *A bad boundary.* Two corners; then `0,0,0; 6000,0,0; arc 6000,0 6000,4000; 0,4000,0` | `created` none; *needs at least three corners and 2 were given*; then the arc named as not an arc, before Revit is asked |
| **KB6** | *A crossing boundary.* `0,0,0; 10000,10000,0; 10000,0,0; 0,10000,0` | `created` none, refused with Revit's own words, nothing left |
| **KB7** | *An arc edge.* KB2's rectangle with its far side `arc 226000,6000 220000,12000` | Area 296.55 m2 (240 plus a half circle of radius 6 m) |
| **KB8** | *Kept, through `try-once`, on the modeller's yes.* KB2's positive on a saved copy | Keep or Discard; afterwards a Structural Foundation schedule shows one new row of the type asked, and a Floor schedule none - the second route |
| **KB9** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The three sentences reach this card by identity; *"make the slab"* reaches `CREATE_FLOOR` and *"make a slab under this block of rooms"* `CREATE_FROM_ROOM_BOUNDARIES`. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept create-foundation-slab
--by "Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change.
A row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `heron_brain.lookup` on a scratch
store each, against this tree with and without the card, on Revit 2020, 2024 and 2025. The three
sentences above reach this card by identity; without it they reached `PLACE_AREA_REINFORCEMENT` (the
first two) and `CREATE_WALL_FOUNDATION` (the third), ranked. Six undeclared phrasings - *"add a raft
foundation"*, *"make a foundation slab"*, *"add a mat foundation"*, *"foundation slab here"*, *"add a slab
foundation under the building"* and *"make a 300 thick slab"* - now reach this card ranked; before it they
reached `CREATE_WALL_FOUNDATION`, `PLACE_AREA_REINFORCEMENT` or, for the last, `SET_COMPOUND_LAYER_WIDTH`.
No routing table claims any of the six. **Two claimed sentences moved to this card and are CLOSED in the
same change** ([row 5b-1372](../fragment-issues/section-5b-rows-401-425.md)): *"make a slab under this
block of rooms"*, declared on `CREATE_FROM_ROOM_BOUNDARIES`, and *"make the slab"*, declared on
`CREATE_FLOOR`; both PROVEN, both fingerprints unchanged. After that, every declared sentence in the
library run through `lookup` on 2024 reaches what it reached before. `tools/check-routing.py` on a fresh
store: no question answered by a write; *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 65, as before. On the
keyword route alone `CREATE_FLOOR`'s *"add a slab here"* and *"make the slab"* now rank this card first,
and `MOVE_TO_RAY_HIT`'s *"take them up to the slab"* ranks `CREATE_FLOOR` first; each reaches its own card
by identity.

**Open, recorded and not answered by inventing utterances.** *"make a 300 thick slab"* reaches this card
ranked - it may mean an ordinary floor, and nothing claims it; if a modeller says it, which card it is
theirs to say. *"make a raft"* reaches `CREATE_FILL_PATTERN` ranked, before this card and after.
