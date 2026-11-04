# Needs checking — Group KF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KF - one fabric sheet at a point in a slab or wall: `PLACE_FABRIC_SHEET` (2026-11-01)

Asked for in the modeller's words: *"place one fabric sheet on this slab at this point"*, *"put a single
mesh sheet here"*, *"add a B503 sheet to this wall"* - Revit's Structure > Reinforcement > Fabric Sheet.
Nothing in Heron placed one sheet: `PLACE_FABRIC_REINFORCEMENT` is Revit's Fabric Area, sheets over a
whole host. `brain/heron_retrieve.py` on each sentence, Revit 2024, offered `PLACE_FABRIC_REINFORCEMENT`
for the first two and `ADD_CURTAIN_GRID_LINES` for the third.

| What | Where | What it does |
|---|---|---|
| `PLACE_FABRIC_SHEET` (MODIFY) | [`brain/fragments/place-fabric-sheet`](../../brain/fragments/place-fabric-sheet/fragment.yaml) | Places **one** flat fabric sheet of the Fabric Sheet Type the modeller **names**, centred on the point given in millimetres - *x,y* on a slab, *x,y,z* on a wall - in the one structural floor, wall, foundation slab or Part selected. **None named, or one not loaded: nothing is placed** and the loaded sheet types come back with their sizes. Major direction x (left out), y, horizontal, vertical or an angle; face top, bottom, exterior or interior, or Revit's own when left out. Refused by name: anything that is not a floor, wall, foundation slab or Part, a floor or wall whose Structural box is off, a host Revit does not take, more than one host. A sheet of the same type already centred within 10 mm is left alone. Read back after a regenerate: host, type, face, centre and direction |

**Why a new card and not a widening.** `PLACE_FABRIC_REINFORCEMENT` is a Fabric Area: it needs a face, a
direction and a Fabric Area Type, covers the whole host and says nothing about where. One sheet needs a
point, takes no area type and leaves a different element. Widening would have hung two jobs on one card,
each with a value the other ignores. That card is DRAFT; its `impl/` is untouched and its routing table
gained one comment row naming this card.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `FabricSheet.Create(Document, Element, ElementId)`,
`IsValidHost(Element)`, `PlaceInHost(Element, Transform)`, `IsSingleFabricSheetWithinHost(Element,
Transform)`, `GetSheetLocation`, `GetWireCenterlines()`, `FabricLocation`, `HostId`, `FabricAreaOwnerId`,
`FabricSheetType.OverallLength` / `OverallWidth`, `Part.GetSourceElementIds`, `Wall.Orientation` /
`Width` and the Structural parameters of floors and walls are present and unchanged 2020-2027. **Nothing
here has run in Revit.** The card is DRAFT; its risk is MODIFY - it adds one sheet and deletes only a
sheet it has just made and Revit refused.

**Not known from the assemblies, and recorded here rather than guessed** (KF2 to KF5 measure them): what
the origin of `PlaceInHost`'s transform means - the sheet's corner, its centre, or a move from where
`Create` left it. The fragment does not rely on it: it measures the sheet's centre from its own wires and
places it again by the difference, so a wrong guess shows as a second placement, not a misplaced sheet.
Also not known: whether `GetSheetLocation`'s x axis is the sheet's major direction - if it is not,
`readBackWrong` names a 90 degree turn on every run and the read back is what is corrected, not the
sheet; whether `IsSingleFabricSheetWithinHost` answers false for a sheet inside the host near its edge;
and which face Revit puts a sheet on when none is asked.

**The model - Group HY's scratch project in Revit 2020**, from Revit's metric structural template, in
`%TEMP%\heron-work\rebar-hy`: S1 the 200 mm structural concrete floor, W1 the 200 mm structural concrete
wall and N2 the architectural floor ([group-hy.md](group-hy.md)). **Which fabric sheet types it has
loaded, if any, is not known here**, nor where S1 and W1 sit. Every run rolled back. The job file is
[`tools/jobs/place-fabric-sheet-kf-2026-11-01.yaml`](../../tools/jobs/place-fabric-sheet-kf-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its sheet type and point are left for KF1 to read** and
are marked in the file; until they are filled in the run is refused naming the loaded types - expected,
NOT RUN. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KF0** | *Open.* The HY scratch project open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name; S1, W1 and N2 show in `FloorPlan: Level 1` |
| **KF1** | *Read the sheet types and the points - also the "none named" negative.* This card on S1 with `fabricSheetType` left out; S1's and W1's extents read off the model | `placed` 0, *No fabric sheet type was named - Heron never chooses one*, `sheetTypes` listing what is loaded with sizes. Write a type and a point well inside S1 into the job file, both places, and a point on W1's face with its height for KF4. **If none is loaded**, make one fabric sheet type by hand in the scratch copy, save, and run KF1 again |
| **KF2** | *One sheet in S1, the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the columns instead of the floors - `placed` 0, every column *not a floor, a wall, a foundation slab or a Part*, *Nothing was placed*. Positive: `placed` 1 in S1, `report` centred on the point, 0 deg from the model's x axis; N2 skipped as *not structural*; `readBackWrong` empty. **Record** whether a second placement was needed, and the face Revit chose - that settles the transform's origin and the default face |
| **KF3** | *A second run, N2 alone, and a name not loaded.* KF2's positive twice in one rolled-back group; then N2 alone; then `fabricSheetType` `KF no such sheet` | Second run: `placed` 0, `alreadyThere` naming the first sheet. N2: skipped *not structural - its Structural box is off*. The unknown name: refused, the loaded types named |
| **KF4** | *A wall, and turned.* W1 with the point KF1 read on it, `location` exterior; then S1 with `majorDirection` 30 | W1: `placed` 1, exterior face, 0 deg from horizontal, its centre on the point. S1 at 30: the sheet turned 30 degrees in plan and `readBackWrong` empty - if it names a 90 degree turn on both, `GetSheetLocation`'s x axis is the minor direction, and the read back is what changes |
| **KF5** | *Off the host, and no height.* S1 with a point outside its edge; W1 with `x,y` only | S1: `placed` 0, skipped - Revit says the sheet does not lie within it, or Revit's own refusal - and nothing left in S1. W1: skipped *a wall needs the height too* |
| **KF6** | *Kept, through `try-once`, on the modeller's yes.* KF2's positive on a saved copy, `FloorPlan: Level 1` in front at Detail Level Fine | Keep or Discard; afterwards Revit's own Fabric Sheet tool, the same type placed by hand in S1, matches in size and turn - the second route - and one Ctrl+Z takes the sheet back |
| **KF7** | *Revit 2026 or 2027.* KF2 again | The same answer. Nothing this card calls changed between releases |
| **KF8** | *Routing, on the owner's PC.* The routing paragraph below, measured again on `model+rerank` | The three sentences reach this card by identity. Any sentence the re-ranker moves to this card from another is a row of its own |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept place-fabric-sheet --by
"Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A row
that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here - through `brain/heron_retrieve.py` and
`tools/check-routing.py` on a scratch store each, against this tree with and without the card. The three
sentences reach this card by identity on 2020 and 2024; without it the first two ranked
`PLACE_FABRIC_REINFORCEMENT` first and the third `ADD_CURTAIN_GRID_LINES`. `PLACE_FABRIC_REINFORCEMENT`'s
three declared sentences, and *"lay a mesh sheet in this slab"* and *"A393 mesh bottom of the ground
slab"* that its routing table claims, still reach it on both releases. Undeclared phrasings that moved
here from `PLACE_FABRIC_REINFORCEMENT`: *"place a fabric sheet in this floor"*, *"put a fabric sheet in
this wall"*, *"place a mesh sheet at this point"*, *"add one fabric sheet to the slab"* - one sheet each,
so here is right - and *"what fabric sheet types are loaded"*, a question that reached a writer before
this card as well as after it (`LIST_REBAR_TYPES` answers it; nobody has said it, so it is not declared
anywhere). Two sheet-of-drawings phrasings moved here too, neither claimed by any card: *"place a sheet"*
(was `PLACE_VIEWS_ON_SHEET`) and *"put a single sheet here"* (was `PLACE_SCHEDULE_ON_SHEET`) - for either,
this card refuses unless a slab or wall is selected and a sheet type named. `tools/check-routing.py`: no
risk-ladder crossing either time; *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 65 both times, the same 65;
*SENTENCES TWO FRAGMENTS BOTH WANT* 697 both times, of 3738 utterances before and 3741 after. On its
keyword route alone `CREATE_FLOOR`'s *"add a slab here"* went from `CREATE_FOUNDATION_SLAB` answering to
this card answering; through `find` it reaches `CREATE_FLOOR` by identity on 2020 and 2024 before and
after. **No claimed sentence moved**, so no row was owed.
