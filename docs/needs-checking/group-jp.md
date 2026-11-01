# Needs checking — Group JP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-29 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JP - a curtain system type's Grid 1 and Grid 2, and its mullions, set by the curtain wall setters: `SET_CURTAIN_WALL_GRID` version 3, `SET_CURTAIN_WALL_MULLIONS` version 2 (2026-10-29)

From [the facade plan](../work-notes/plans/facade-capability.md)'s *"Waiting for the owner's word"*: a curtain
system laid on a mass's faces (`CREATE_CURTAIN_SYSTEM`, Group JM) takes its grid and mullions from its
**curtain system type**, and nothing in Heron could set one - the [`facade`](../../brain/skills/facade.yaml)
method's route B said so. The owner said yes to widening the two PROVEN setters on 2026-10-29, in the chat,
knowing it made both proofs stale.

**What changed.** [`set-curtain-wall-grid`](../../brain/fragments/set-curtain-wall-grid/fragment.yaml)
(FRG-ELE-072) version 3 and [`set-curtain-wall-mullions`](../../brain/fragments/set-curtain-wall-mullions/fragment.yaml)
(FRG-ELE-073) version 2 each take a curtain system type as well as a curtain wall type, through the same
input, `curtainWallType`. Its declared type moved from `WallType` to a **name** matched inside the fragment,
because the add-in has no binder for a curtain system type and an optional `WallType` cannot be left out
(D-54; `CREATE_CURTAIN_SYSTEM` matches its type by name the same way). The match is the add-in's own rule -
the name exactly, capitals included, or *Family: Type* with or without a space before the colon - over wall
types and curtain system types together. For a curtain system type the keys are **grid 1** and **grid 2**
(*grid 1 layout*, *grid 1 spacing*, *grid 1 interior*, *grid 2 border 1* ...), never vertical and horizontal,
because which way each grid runs is the face's. The curtain systems of the type are counted first
(`systemsOfType`, new), as walls are. Fixed Number stays refused. **A curtain wall type is the earlier
version to the word**, with two deliberate differences: a bare name that a curtain system type also carries
is refused naming both (D-54 s3), and a name no type has is refused by the fragment - listing the model's
curtain wall and curtain system types - where the add-in refused it before the run.

**What is done and what is not.** Both compile on all eight releases (`tools/check-fragments-compile.py`,
2026-10-29, in the cloud). Every Revit member and parameter id they use was read with `tools/api-surface` off
the reference assemblies of all eight, 2020 to 2027, and the parameter ids' reference text off each release's
`RevitAPI.xml`. **Nothing has met a Revit.** Until JP19 is signed both are DRAFT and `revit_change` refuses a
change kept from either ([D-117](../decisions/D-117.md)) - so the `curtain-wall` method's grid and mullion
steps and the `facade` method's routes A and B wait on this group too. What still runs: one `try-once` per
version ([D-119](../decisions/D-119.md)) - JP18 - and any rolled-back run.

**The assumptions this group settles - none is known from the cloud:**

- **That a curtain system type carries its grids as `SPACING_LAYOUT_1`/`_2` and `SPACING_LENGTH_1`/`_2`, and its
  mullions as `AUTO_MULLION_INTERIOR_GRID1` ... `_BORDER2_GRID2`.** The reference text of all eight releases
  gives those ids only their display names - *Layout*, *Spacing*, *Interior Type* - the same names it gives
  the `_VERT`/`_HORIZ` ids a wall type uses, and nothing about which element carries which. The code refuses a
  type that has none of them, prints which grid or mullion ids the type carries on every run, and **stops,
  keeping nothing,** when a setting written by them does not read back as asked (JP2, JP11).
- **That the layout numbers mean the same layouts on a curtain system type** - None 0, Fixed Distance 1,
  Maximum Spacing 3, Minimum Spacing 5. Checked on every run against the words Revit shows (JP2 reads 3 and
  5, which no proof of this card has read back yet).
- **Which way Grid 1 and Grid 2 run on the tower's faces, and which of Revit's U and V sets each is.** The
  reference calls `CurtainGrid.Grid1Angle` *"the U grid line pattern"* and Grid 2 the V one, and says nothing
  of the direction on a face; the card claims neither. JP2, JP3, JP11 and JP12 record it, and each
  expectation is written for both answers.
- **Where Revit keeps a curtain system's Number, Justification, Angle and Offset** - on the type or on each
  curtain system. The reference lists `SPACING_NUM_DIVISIONS_1`/`_2` as *Number* and says no more; JP2's
  `readBack` measures it. Until it is known, Fixed Number is refused for the same reason as for a wall.

**The rigs (JP0) - scratch, never a working model.**

| What | How |
|---|---|
| *Heron JP Curtain* | The owner's Project1 (Revit 2020, DefaultMetric) **saved as** *Heron JP Curtain.rvt* in `%TEMP%\heron-work\group-jp` - a copy, so `Curtain Wall: Storefront` and the eight mullion types [`set-curtain-wall-mullions`](../../brain/fragments/set-curtain-wall-mullions/fragment.yaml)' version 1 proof listed are there. On Level 1 (elevation 0), by hand: |
| - *Heron JP Tower* | Massing & Site > In-Place Mass, named *Heron JP Tower*: a rectangle from (0, 30000) to (18000, 42000) mm, Create Form, 30000 mm high - Group JM's tower. Four upright faces, two 18000 x 30000 and two 12000 x 30000 |
| - *Heron JP 1500* | Type Properties of the model's curtain system type > Duplicate, *Heron JP 1500*: Grid 1 and Grid 2 Layout **Fixed Distance**, Spacing **1500**; every mullion **None** |
| - one curtain system | Massing & Site > Curtain System, type *Heron JP 1500*, the tower's **four side faces** picked, Create System. **By hand**, so the arrangement is not `CREATE_CURTAIN_SYSTEM`, which is DRAFT itself (Group JM) |
| | Save, with **nothing selected**. Check that *Curtain Wall: Storefront* and *Rectangular Mullion: 30mm Square* are in the model |
| *Heron JP Wall Grid* | A **new project from Revit 2024's default metric template**, saved in the same folder, nothing else in it - as Project2 was when `SET_CURTAIN_WALL_GRID` version 2 was proved on 2026-10-06. Check that *Curtain Wall: Curtain Wall* exists and reads Vertical and Horizontal Layout **None**. Project2 itself serves, if it is still on the PC - change `in:` in the job file to it |

**The numbers are derived, not remembered.** 1500 and 3000 divide every face's width (18000, 12000) and
height (30000), their halves and their corners' distances from the origin (0, 18000, 30000, 36000, 42000), so a
Fixed Distance count holds wherever Revit starts a grid; Maximum and Minimum Spacing cut a face into equal
panels and never depend on it. At 1500 the four faces carry **36 upright lines and 76 level ones** (11 across
an 18000 face, 7 across a 12000 one, 19 up each). Every row's arithmetic is in the cards' `tests/cases.yaml`.

**Needs:** the add-in as it is - nothing in `revit/` changed, so no rebuild - and Changes on. Every line below
in Git Bash from the Heron folder, with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits
are connected, and `< /dev/null`; a by-hand row is `fragment ... --write` without `--apply`, so it is rolled
back. **Order: JP0, JP1, JP2 to JP8, JP10 to JP16, then JP9 - it changes the rig and the rig is never saved
after it - then JP17 to JP19.**

| # | Run | Look for |
|---|---|---|
| **JP0** | *The rigs, by hand*, as above, each saved | The tower, the type, ONE curtain system on four faces, nothing selected; the 2024 project with *Curtain Wall: Curtain Wall* at Layout None both ways |
| **JP1** | *The grid's wall-type path again - version 2's own proof, rolled back. The half of the negative that says nothing moved.* In *Heron JP Wall Grid*, Revit 2024: `python tools/batch-prove.py tools/jobs/set-curtain-wall-grid-wall-type-2026-10-29.yaml --session <pid>` | **Version 2's answer word for word** ([the card's kept proof block](../../brain/fragments/set-curtain-wall-grid/fragment.yaml)), with `systemsOfType 0` beside it. Positive: `wallsOfType 1`, `applied 4`, `readBack` *"vertical: layout Fixed Distance [Revit's value 1], was None [Revit's value 0] - asked; spacing 2500.0 mm, was 0.0 mm - asked \| horizontal: ... 4000.0 mm ... \| Number, Justification, Angle and Offset, both ways, measured - on the TYPE: none of them; on wall ...: all eight"*, `measured` *"... 9000 mm long: vertical grid lines 0 before, 3 (Revit's U set 0, V set 3) now; horizontal 0 before, 0 now ..."*. Negative: refused *"A vertical spacing does nothing while the vertical layout is None ..."*. **FAIL:** any word unlike version 2's |
| **JP2** | *The positive - a curtain system type, rolled back.* In *Heron JP Curtain*, Revit 2020: `python tools/batch-prove.py tools/jobs/set-curtain-wall-grid-curtain-system-2026-10-29.yaml --session <pid>`, its positive leg: `curtainWallType=Heron JP 1500`, `grid=grid 1 layout=maximum spacing; grid 1 spacing=3000; grid 2 layout=minimum spacing; grid 2 spacing=2000` | `systemsOfType 1`, `wallsOfType 0`, `applied 4`, `differs` empty; `readBack` *grid 1: layout Maximum Spacing [Revit's value 3], was Fixed Distance [Revit's value 1] - asked; spacing 3000.0 mm, was 1500.0 mm* and *grid 2: layout Minimum Spacing [Revit's value 5] ... 2000.0 mm*. `measured`: one curtain system on **4** faces, vertical lines **36** and horizontal **76** before; now **vertical 16 and horizontal 56** (Grid 1 upright) **or vertical 26 and horizontal 36** (Grid 1 level). **Record**: which pair, the *By face* U and V counts, the *"Of Revit's grid ids ... carries"* line, and where Number, Justification, Angle and Offset sit. **FAIL:** a refusal naming the grid ids (the assumption wrong - record the ids it printed), a stop quoting Revit, or any other pair of counts |
| **JP3** | *Grid 2 alone.* `python mcp/client/heron_bridge_client.py fragment --in "Heron JP Curtain" set-curtain-wall-grid --write --set "curtainWallType=Heron JP 1500" --set "grid=grid 2 layout=fixed distance; grid 2 spacing=3000"` | `applied 1`, `alreadyAsAsked 1`; grid 1 *not asked, left as it was*. Only the direction Grid 2 runs in changes - level lines 76 to 36, or upright 36 to 16 - and it is **the other one** from JP2's Grid 1 |
| **JP4** | *The family and the type.* JP2's request with `curtainWallType=Curtain System : Heron JP 1500` - the family name JP2's `readBack` printed, if it is not *Curtain System* | JP2's answer. Two spellings of one name, one type |
| **JP5** | *The negative - the D-30 negative.* The job file's negative leg: the SAME grid aimed at `curtainWallType=Curtain Wall: Storefront` | `applied 0`, `readBack` and `measured` empty; refused *"'grid 1 layout' is not a grid setting this changes. Name one or more of: vertical layout, vertical spacing, horizontal layout, horizontal spacing. Nothing was changed"* - version 2's words for a key a wall type does not have. Storefront as it was |
| **JP6** | *A wall type's keys on a curtain system type.* JP3's command with `grid=vertical layout=fixed distance; vertical spacing=1500` | Refused *"'vertical layout' is a curtain WALL type's setting"*, naming Grid 1 and Grid 2 and the four grid keys; nothing changed. Never read as Grid 1 |
| **JP7** | *Fixed Number.* JP3's command with `grid=grid 1 layout=fixed number` | Refused *"Fixed Number is not set here"*, saying the reference does not say whether the type or each curtain system holds the Number; nothing changed |
| **JP8** | *A name no type has.* JP2's request with `curtainWallType=Heron JP No Such Type` | Refused *"No wall type or curtain system type called ..."*, listing the model's curtain wall types and its curtain system types, *Heron JP 1500* among them; nothing chosen |
| **JP9** | *One bare name on two kinds of type - LAST of the rig's rows, and the rig never saved after it.* By hand, duplicate *Curtain Wall: Storefront* and name the copy *Heron JP 1500*; then JP2's request | Refused *"2 types in ... are called "Heron JP 1500" - wall types and curtain system types are both searched"*, saying which: the curtain wall type and the curtain system type; nothing chosen (D-54 s3). Then close the rig **without saving**, or delete the copy |
| **JP10** | *The mullions' wall-type path again - version 1's own proof, rolled back.* In *Heron JP Curtain*: `python tools/batch-prove.py tools/jobs/set-curtain-wall-mullions-wall-type-2026-10-29.yaml --session <pid>` | **Version 1's answer word for word** ([its kept proof block](../../brain/fragments/set-curtain-wall-mullions/fragment.yaml)), with `systemsOfType 0`. Positive: `applied 1`, `wallsOfType 0`, `readBack` *"vertical interior: 'Rectangular Mullion: 30mm Square', was 'Rectangular Mullion: 50 x 150mm' - asked for ..."*. Negative: refused *"No mullion type called 'Rectangular Mullion: ZZZ No Such Size' in this model"* with the same eight types. **FAIL:** any word unlike version 1's |
| **JP11** | *The positive - a curtain system type's mullions, rolled back.* `python tools/batch-prove.py tools/jobs/set-curtain-wall-mullions-curtain-system-2026-10-29.yaml --session <pid>`, its positive leg: `curtainWallType=Heron JP 1500`, `mullions=grid 1 interior=Rectangular Mullion: 30mm Square` | `systemsOfType 1`, `applied 1`, `differs` empty; `readBack` *grid 1 interior: 'Rectangular Mullion: 30mm Square', was none*. `measured`: one curtain system on 4 faces, 36 vertical and 76 horizontal lines; mullions before *none*, now **720 x vertical** (Grid 1 upright: 36 lines cut into 20) **or 760 x horizontal** (Grid 1 level: 19 lines cut into 12 on each 18000 face, into 8 on each 12000 one). **Record** which - it must agree with JP2 - and the *"Of Revit's mullion ids ... carries"* line. **FAIL:** a stop quoting Revit, or any other count |
| **JP12** | *The other grid.* `python mcp/client/heron_bridge_client.py fragment --in "Heron JP Curtain" set-curtain-wall-mullions --write --set "curtainWallType=Heron JP 1500" --set "mullions=grid 2 interior=Rectangular Mullion: 30mm Square"` | `applied 1`, and the mullions the **other** of JP11's two answers |
| **JP13** | *Already so.* JP12's command with `mullions=grid 1 border 1=none` | `applied 0`, `alreadyAsAsked 1`, not a refusal; *"It is used by 1 curtain system(s) in this model"* |
| **JP14** | *The negative - the D-30 negative.* The job file's negative leg: the SAME mullions aimed at `curtainWallType=Curtain Wall: Storefront` | `applied 0`, `readBack` and `measured` empty; refused *"'grid 1 interior' is not a mullion position. Name one or more of: vertical interior, vertical border 1, vertical border 2, horizontal interior, horizontal border 1, horizontal border 2. Nothing was changed"* - version 1's words. Storefront as it was |
| **JP15** | *A wall type's position on a curtain system type.* JP12's command with `mullions=vertical interior=Rectangular Mullion: 30mm Square` | Refused *"'vertical interior' is a curtain WALL type's position"*, naming Grid 1 and Grid 2 Mullions and the six grid positions; nothing changed |
| **JP16** | *A mullion type the model has not got.* JP12's command with `mullions=grid 1 interior=HERON NO SUCH MULLION` | Refused *"No mullion type called 'HERON NO SUCH MULLION' in this model"*, with every mullion type the model has; nothing loaded |
| **JP17** | *Routing, on the PC's backend.* Ask `heron_lookup` *"set the curtain system grid spacing to 1500"* and *"put mullions on the curtain system type"* - NOT declared, see the cards' utterance notes - then each card's earlier sentences; then *"what is the grid spacing on this curtain system"* and *"which mullions are on this curtain system"*, each with `question: true` and again without; then `python tests/test_routing_phrases.py` | The first reaches `SET_CURTAIN_WALL_GRID`, the second `SET_CURTAIN_WALL_MULLIONS`, by ranking; the earlier sentences where they reached before. **Measured in the cloud on 2026-10-29 (trained backend, Revit 2024, private stores):** both requests reach their setters, words #1 and nearness #1; *"what is the grid spacing on this curtain system"* reaches `REPORT_CURTAIN_WALL_TYPE`, a read, as before; *"which mullions are on this curtain system"* reaches the read **with** the question flag and `SET_CURTAIN_WALL_MULLIONS` **without** it, where before this change it reached the read by a tie (D-86: the fix is a reader of a curtain system type - `REPORT_CURTAIN_WALL_TYPE` widened, the owner's word, as it is PROVEN). The two sentences were declared first and taken back: declared, they sent three of row 5b-329's select guards in `test_routing_phrases.py` to a write. That test fails only its two failures of the base commit. Record what each reaches on the PC |
| **JP18** | *The one kept run per version (D-119), on the owner's yes only.* Save the rig, a 3D view of the tower in front: `python mcp/client/heron_bridge_client.py try-once --in "Heron JP Curtain" set-curtain-wall-grid --set "curtainWallType=Heron JP 1500" --set "grid=grid 1 layout=maximum spacing; grid 1 spacing=3000; grid 2 layout=minimum spacing; grid 2 spacing=2000"`, and later the same for `set-curtain-wall-mullions` with JP11's values | JP2's (or JP11's) answer; in the view, the tower's lines (or mullions) redrawn; the owner chooses **Keep or Discard**. A Keep is the positive half of that card's proof (D-120); Ctrl+Z afterwards puts the rig back for any row still to run |
| **JP19** | *The signatures, on the owner's word only*, after JP1 to JP16 pass | `python brain/heron_validate.py draft set-curtain-wall-grid --from brain/proof-drafts/runs/set-curtain-wall-grid.json` - the record JP2 and JP5 left (JP1 runs first and is read by hand) - then `accept set-curtain-wall-grid --by "Ajmal PS"`; the same for `set-curtain-wall-mullions` from JP11 and JP14. **`heron-status: PROVEN` is a separate edit, the owner's.** JP1's Revit 2024 run is owed as a `proof.releases` entry ([D-131](../decisions/D-131.md)) |

A row that fails stays open; nothing in a row changes to make it pass.

### Results - Revit 2020, 2026-11-01

**Not run on JP0's rigs.** Run in the owner's scratch *TEST MASS* (Revit 2020, session 6588; his real
*4355-BHVD-3D-50S20-BL001A* was the document in front, and every run was pinned with `--in "TEST MASS"` - both
records name TEST MASS for both legs). The arrangement was re-made inside each rolled-back run by
`validate --setup create-curtain-system`: one curtain system of the template's *Curtain System : 1500 x 3000mm*
on the 12 upright faces of the loaded mass *TRG_Mass_Acrobatic_7F* (instance 351071; Group JM). So JP2's and
JP11's derived counts do not apply. Fingerprints: grid `e550585eab50dee5`, mullions `aa8013e76b5a93e3`.

| # | Result | What it read |
|---|---|---|
| JP0, JP1 | NOT RUN | No hand rig; no Revit 2024 project |
| JP2 | PASS, rolled back | `grid=grid 1 layout=maximum spacing; grid 1 spacing=3000; grid 2 layout=minimum spacing; grid 2 spacing=2000`: `systemsOfType` 1, `wallsOfType` 0, `applied` 3, `alreadyAsAsked` 1 (grid 1 spacing was already 3000), `differs` empty; readBack *grid 1: layout Maximum Spacing [Revit's value 3], was Fixed Distance [Revit's value 1]* and *grid 2: layout Minimum Spacing [Revit's value 5] ... 2000.0 mm, was 1500.0 mm*. *"Of Revit's grid ids, 'Curtain System: 1500 x 3000mm' carries: SPACING_LAYOUT_1, SPACING_LAYOUT_2, SPACING_LENGTH_1, SPACING_LENGTH_2, ..."*. Number, Justification, Angle and Offset: on the type none, on the curtain system all eight. measured, 12 faces: vertical lines 96 before and after (Revit's V set), horizontal 64 -> 32 (Revit's U set) - **Grid 1 runs upright on these faces, Grid 2 level** |
| JP3, JP4, JP6 to JP9 | NOT RUN | |
| JP5 | PASS, rolled back | The same grid on *Curtain Wall: Storefront*: `applied` 0, readBack and measured empty, refused *"'grid 1 layout' is not a grid setting this changes. Name one or more of: vertical layout, vertical spacing, horizontal layout, horizontal spacing. Nothing was changed"* |
| JP10 | NOT RUN | |
| JP11 | PASS, rolled back | `mullions=grid 1 interior=Rectangular Mullion: 30mm Square`: `systemsOfType` 1, `applied` 1, `differs` empty; readBack *grid 1 interior: 'Rectangular Mullion: 30mm Square', was none*; *"Of Revit's mullion ids ... carries: AUTO_MULLION_INTERIOR_GRID1 ... AUTO_MULLION_BORDER2_GRID2; not: AUTO_MULLION_INTERIOR_VERT, AUTO_MULLION_INTERIOR_HORIZ"*. measured: 96 vertical and 64 horizontal lines, mullions none before, **580 x vertical** now - agreeing with JP2 that Grid 1 is upright |
| JP12, JP13, JP15 to JP18 | NOT RUN | |
| JP14 | PASS, rolled back | The same mullions on *Curtain Wall: Storefront*: `applied` 0, refused *"'grid 1 interior' is not a mullion position. Name one or more of: vertical interior, ... horizontal border 2. Nothing was changed"* |
| JP19 | PASS | `accept --by "Ajmal PS"` on both, run in his own terminal on his word *"Yes, sign both"*; both cards PROVEN in the same change |

Still owed: JP1 and JP10 (the curtain WALL type path again, word for word with the earlier versions - not re-run
here), JP3, JP4, JP6 to JP9, JP12, JP13, JP15 to JP18, and Revit 2024 / 2027 release entries.

**What this group does not settle.** Reading a curtain system type's settings back - `REPORT_CURTAIN_WALL_TYPE`
reads a wall type and refuses this name, and nothing else reads one. Changing ONE curtain system without its
type: `DUPLICATE_TYPE` and `CHANGE_ELEMENT_TYPE` have not been run on a curtain system, and the setters' answer
says so. A curtain system on a sloped or curved face, where neither grid runs upright or level.

### More results - Revit 2020, 2026-11-01, later the same day

The same rig and method as above, every leg rolled back and pinned to *TEST MASS*.

| # | Result | What it read |
|---|---|---|
| JP1 | PASS, on Revit 2020 | `--setup create-wall` (a 9000 mm *Curtain Wall: Curtain Wall* on Level 1), vertical and horizontal fixed distance 2500 / 4000: `systemsOfType` 0, `wallsOfType` 1, `applied` 4; readBack and measured version 2's words - *"vertical grid lines 0 before, 3 (Revit's U set 0, V set 3) now; horizontal 0 before, 0 now"*. Negative: the vertical-spacing-without-layout refusal. Revit 2024, as the row asks, NOT RUN |
| JP3 | PASS | `grid 2 layout=fixed distance; grid 2 spacing=3000`: `applied` 1, `alreadyAsAsked` 1, grid 1 *not asked, left as it was*; level lines 64 -> 28, upright 96 unchanged - only Grid 2's direction moved, the other one from JP2's Grid 1 |
| JP4 | PASS | `curtainWallType=Curtain System: 1500 x 3000mm` (no space before the colon): JP2's answer, label *Curtain System: 1500 x 3000mm* |
| JP6 | PASS | `vertical layout` on the curtain system type refused: *"'vertical layout' is a curtain WALL type's setting ... Name one or more of: grid 1 layout, grid 1 spacing, grid 2 layout, grid 2 spacing. Nothing was changed"* |
| JP7 | PASS | `grid 1 layout=fixed number` refused: *"Fixed Number is not set here ..."* |
| JP8 | PASS | *"No wall type or curtain system type called "Heron JP No Such Type" in TEST MASS ..."*, listing the three curtain wall types and *Curtain System: 1500 x 3000mm* |
| JP9 | NOT RUN | Needs a curtain wall type renamed to the curtain system type's bare name |
| JP10 | PASS, on Revit 2020 | Storefront `vertical interior=Rectangular Mullion: 30mm Square`: `applied` 1, version 1's readBack words; `wallsOfType` 32 (the building's Storefront walls - version 1's model had none), measured on wall 345820: 42 vertical 50 x 150 before, 38 x 30mm Square + 4 x 50 x 150 (borders) now |
| JP12 | PASS | `grid 2 interior`: **550 x horizontal** - the other of JP11's two answers |
| JP13 | PASS | `grid 1 border 1=none`: `applied` 0, `alreadyAsAsked` 1, no refusal |
| JP15 | PASS | `vertical interior` on the curtain system type refused, naming Grid 1 and Grid 2 Mullions and the six grid positions |
| JP16 | PASS | *"No mullion type called 'HERON NO SUCH MULLION' in this model ..."*, with the eight mullion types |
| JP17 | PASS | `heron_lookup` on the PC: *"set the curtain system grid spacing to 1500"* -> `SET_CURTAIN_WALL_GRID`, *"put mullions on the curtain system type"* -> `SET_CURTAIN_WALL_MULLIONS` (words #1, nearness #1); *"what is the grid spacing on this curtain system"* (question) -> `REPORT_CURTAIN_WALL_TYPE`; *"which mullions are on this curtain system"* -> `REPORT_CURTAIN_WALL_TYPE` with the question flag and `SET_CURTAIN_WALL_MULLIONS` without it, as the cloud measured, with Heron's *"CHECK THIS IS A CHANGE YOU MEANT TO MAKE"* beside it. `tests/test_routing_phrases.py` PASSED on the PC (exit 0, with an empty scratch `HERON_KNOWLEDGE`) |

Still owed: JP9, JP18 (the kept runs), JP1 on Revit 2024, and the Revit 2024 / 2027 release entries.
