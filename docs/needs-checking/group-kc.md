# Needs checking — Group KC

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KC - a truss along typed lines: `CREATE_TRUSS` (2026-11-01)

Asked for in the modeller's words: *"place a Howe truss between these two points on Level 2"*, *"add a
truss along grid C from A to F"*, *"put a roof truss of type Pratt 12m here"* - Revit's Structure > Truss.
Nothing in Heron made one: `PLACE_LINE_BASED_FAMILY` lays a beam or a brace along a line and
`CREATE_BEAM_SYSTEM` fills a bay with beams. `brain/heron_retrieve.py` on each sentence, Revit 2024,
offered `CREATE_WALL`, `DIMENSION_GRIDS_AND_LEVELS` and `CREATE_ROOF` first.

| What | Where | What it does |
|---|---|---|
| `CREATE_TRUSS` (MODIFY) | [`brain/fragments/create-truss`](../../brain/fragments/create-truss/fragment.yaml) | A truss of a named truss type along each straight line given - `PLACE_LINE_BASED_FAMILY`'s line writing - laid on one level at its project elevation, on a sketch plane made from that level. A vertical, zero-length, curved or rising line is refused by name, a line given twice or already carrying a truss is skipped and named; with no type named nothing is made and the model's truss types are listed. Each truss read back - still there, of the type asked, its length off its own location line - before it is kept |

**Why a new card and not a widening.** `PLACE_LINE_BASED_FAMILY` is PROVEN and places a family instance
with a `StructuralType`; a truss is made by `Truss.Create` on a sketch plane and owns the members it
generates, so widening that card would change its job and stale its proof. Its `impl/` and its card are
untouched.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `Truss.Create(Document, ElementId, ElementId, Curve)`,
`Truss.Members`, `TrussType` (a `FamilySymbol`), `SketchPlane.Create(Document, ElementId)`,
`SketchPlane.GetPlane`, `Level.ProjectElevation`, `LocationCurve.Curve`, `OST_Truss` and `SubTransaction`
are present and unchanged 2020-2027. `RevitAPI.xml` 2020 and 2027 give `Truss.Create`'s curve the same
words - *"must be a line, must not be a vertical line, and must be within the sketch plane"* - and both
list `TrussType` among the classes a class filter does not find, so the types are collected as family
types in Structural Trusses. **Nothing here has run in Revit.** The card is DRAFT; its risk is MODIFY -
it adds trusses and one sketch plane and deletes nothing.

**Not known from the assemblies, and recorded here rather than guessed** (KC2 measures them): whether
the sketch plane Revit makes from a level lies at that level's project elevation - the fragment reads its
plane and stops before any truss with both heights named if it does not; and whether a truss reports the
level its sketch plane came from as its own (`Element.LevelId`) - the fragment says which level Revit gives
it when that differs, and keeps the truss.

**The model - KC Structure in Revit 2020**, a scratch copy of Revit 2020's
`rst_advanced_sample_project.rvt` in `%TEMP%\heron-work\truss-kc`. **Which truss types and levels it holds
is not known here.** Every run rolled back. The job file is
[`tools/jobs/create-truss-kc-2026-11-01.yaml`](../../tools/jobs/create-truss-kc-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its level and type are left for KC1 to read** and are
marked in the file; until they are filled in the run should stop at the level - expected, NOT RUN. Every
line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KC0** | *Open.* KC Structure open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name |
| **KC1** | *Read the arrangement.* A level; the truss types off the Type Selector under Structure > Truss; the plan 200 m east of the origin | Write the level and type into the job file. The 12 m stretch is clear, or the line is moved in both legs alike. **If the copy holds no truss type, load one from Revit's own library into the scratch copy and save it, and say so here** |
| **KC2** | *Make, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the vertical line - `created` none, the report naming Line 1 *VERTICAL*; no truss and no sketch plane added. Positive: one id; `report` *truss made (<id>), type "<type>", 12000 mm long, on "<level>"*; read in the same rolled-back group, the truss in Structural Trusses on that level's plane. **Record** what Revit gave as its level |
| **KC3** | *No type, and a name for a type.* KC2's level and line with `trussType` left out, then `Howe` | `created` none both times; every truss type named, none chosen - even when there is only one |
| **KC4** | *A rising line.* `200000,0,0; 212000,0,1500` | `created` none; Line 1 named as rising 1500 mm |
| **KC5** | *The same line twice.* `200000,0,0; 212000,0,0 \| 212000,0,0; 200000,0,0` | `created` one; Line 2 named *the same line as line 1 - one truss per line, not two* |
| **KC6** | *Kept, through `try-once`, on the modeller's yes.* KC2's positive on a saved copy, a plan of the level in front | Keep or Discard; afterwards a Structural Truss schedule shows one new row of the type asked - the second route - and one Ctrl+Z takes it and its members away |
| **KC7** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The three sentences reach this card by identity. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept create-truss --by "Ajmal
PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A row that
fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `brain/heron_retrieve.py` on a
scratch store each, against this tree with and without the card, on Revit 2024. The three sentences reach
this card by identity; without it they reached `CREATE_WALL`, `DIMENSION_GRIDS_AND_LEVELS` and
`CREATE_ROOF`, ranked. Three undeclared phrasings - *"place a truss"*, *"draw a truss from here to here on
level 3"* and *"add trusses along these lines"* - reach this card ranked; no routing table claims any of
them. `tools/check-routing.py` on a fresh store each, with and without the card: no question answered by a
write; *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 65 both times. **No claimed sentence moved to this
card**, so no row was owed. On the keyword route alone one line is new - `PLACE_FAMILY_INSTANCES`'s
*"place a diffuser"* now ranks `CONNECT_AIR_TERMINALS` first there, not this card - and the host's lookup
still answers it, and *"place a beam between two points"*, *"put a beam from here to here"*, *"add the
bracing"*, *"put secondary beams in this bay"*, *"make the roof on this level"* and *"create a grid line"*,
with the same card as before, by identity.
