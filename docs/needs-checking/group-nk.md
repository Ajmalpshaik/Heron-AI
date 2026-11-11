# Needs checking — Group NK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NK - PROVEN reads that gave a wrong answer, repaired in code and DRAFT again until each is proved: the cards of rows 5b-1128, 5b-393, 5b-323, 5b-351, 5b-295, 5b-352, 5b-154 and 5b-203 (2026-11-11)

A triage on 2026-11-11 found PROVEN cards that only READ and still gave a modeller an answer he would act
on that was wrong - each with its repair already stated in its [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) row,
and each left alone because a change under `impl/` stales a proof. The owner's instruction that day: *"if
you are finding any fragment's mistake, even though it is proven, you can change that one."* The cards below
were changed, and **each is DRAFT again at a new version, its old proof block kept on the card as the record
of what it proved** ([D-30](../DECISIONS.md)).

**A read loses nothing by being DRAFT.** `revit_read` does not ask
[D-117](../decisions/D-117.md)'s question (item 4): it opens no transaction, so a DRAFT read still answers
when the chat names its capability, and the answer says DRAFT. What a DRAFT loses is the one-call short path
- words alone run only a PROVEN match, so the chat is handed the candidates and calls again.

| Card | Version | Row | What it answered | What it answers now |
|---|---|---|---|---|
| [`measure-ceiling-height`](../../brain/fragments/measure-ceiling-height/fragment.yaml) | 3 | [5b-1128](../fragment-issues/section-5b-rows-401-425.md) | A room left at Revit's default height, under a ceiling above that height, read exactly like a room with no ceiling | The room is listed in `limitBelowCeiling` and said in `findings`, with its own height and the lowest such ceiling's. It is still **not measured** until its Upper Limit is raised |
| [`select-by-material`](../../brain/fragments/select-by-material/fragment.yaml) | 4 | [5b-393](../fragment-issues/section-5b-rows-401-425.md) | With `includePaint`, only what is PAINTED with the material, under a sentence saying *"made of it OR painted with it"* | Both sets are read and joined |
| [`read-space-loads`](../../brain/fragments/read-space-loads/fragment.yaml) | 3 | [5b-323](../fragment-issues/section-5b-rows-301-325.md) | *"NO LOAD READABLE"* for a Space holding typed design loads, wherever Revit refused the calculated ones | Each figure is read on its own: what was typed is reported, what Revit refused is named |
| [`report-family-parameters`](../../brain/fragments/report-family-parameters/fragment.yaml) | 2 | [5b-351](../fragment-issues/section-5b-rows-351-375.md) | `<blank>` for a Yes/No | Yes or No, read from the stored tick where Revit's own string is empty |
| [`report-family-forms`](../../brain/fragments/report-family-forms/fragment.yaml) | 5 | [5b-295](../fragment-issues/section-5b-rows-276-300.md) | `0 L` for a solid with a void cut in it | The form says no volume is read on it and that it is in a join or cut; the join or cut is listed with its own volume |
| [`report-bounding-box`](../../brain/fragments/report-bounding-box/fragment.yaml) | 2 | [5b-352](../fragment-issues/section-5b-rows-351-375.md) | A nested family's box with the bodies its `Visible` parameters hide, and no word about it | The same box, and one finding saying a family instance's box can take in bodies the family has switched off |
| [`read-graphic-overrides`](../../brain/fragments/read-graphic-overrides/fragment.yaml) | 2 | [5b-154](../fragment-issues/section-5b-rows-151-175.md) | *"NO per-element override"* for an element overridden only by a pattern | The surface pattern, the cut pattern and its colour are read; `overrideList` carries every line in full |
| `read-element-parameters` 7 · `find-duplicate-values` 2 · `compare-elements` 2 · `compare-models` 2 · `list-materials` 7 · `report-material-takeoff` 4 · `check-sheet-sets-by-parameter` 2 | | [5b-203](../fragment-issues/section-5b-rows-201-225.md) | Whichever of two parameters sharing a name Revit returned first | The name is counted first; carried twice, it is **NOT READ**, and the answer says so |

**What is done and what is not.** Every card compiles on every release it claims, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-11, a cloud container), and
[`tests/test_same_name_guard.py`](../../tests/test_same_name_guard.py) holds the seven readers of row 5b-203 to
the refusal as text. **Nothing here has run in Revit.** A compile is not a proof.

**Not done, and said so the card does not read as more than it is:**

- `measure-ceiling-height` does not look above a room's upper limit. Row 5b-1128 offered that as its other
  repair; which level bounds the look is a choice nobody has made. Its `purpose` still says the plan-overlap
  test *"never meets the trap at all"* - it does; correcting that sentence moved other cards' routing, so it is
  left and recorded on the row.
- `report-bounding-box` does not measure only what shows.
- [Row 5b-1404](../fragment-issues/section-5b-rows-401-425.md) - `list-materials` with `withAllTabs` crashing
  inside Revit 2020 - is **not repaired**: it was never traced to a line, so no repair is stated. That is why
  this group runs on Revit 2024.
- `export-parameters-to-csv` (PUBLISH) and the three MODIFY cards of
  [row 5b-1534](../fragment-issues/section-5b-rows-401-425.md) still take the first match.

### The run

**One job file covers all fourteen cards:**
[`tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml`](../../tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml).
Its header lists the scratch files; NK0 below is the same arrangement, step by step.

```bash
python tools/batch-prove.py tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml --dry-run
python tools/batch-prove.py tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml --session <pid>
```

**The PC, Revit 2024, SAVED scratch files under `%TEMP%\heron-work\nk` - never the owner's own model.** Every
line by hand with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`. Two jobs run a setup step that changes the model and are rolled back; nothing in this group
keeps a change except NK0, which the modeller makes by hand.

**The batch judges two things and no more: a declared result moved off zero, and the negative came back
empty. It cannot read a sentence.** So each row below says what to READ in the answer the batch printed, and
five rows ask one more run by hand, because the half that changed is a sentence, or a refusal that no batch
positive can be.

| # | Run | Look for |
|---|---|---|
| **NK0** | *Arrange, KEEP it, by hand.* **"Heron Reads Test"**, a new project from the default metric template. Materials "Heron A" and "Heron B", each a duplicate of any material. Wall type "Heron Made", a duplicate of "Generic - 200mm" with its structure layer's material set to "Heron A". On Level 1: wall W1 of "Heron Made" and W2, W3, W4 of "Generic - 200mm" closing a rectangle, and W5 of "Generic - 200mm" across the middle - two rooms, **five walls**; Mark typed W1 "A", W2 "A", W3 "B", W4 "C", W5 "E"; one face of W2 painted "Heron A" (Modify, Paint). A Room in each half, with Tag on Placement on and its limits untouched. A Ceiling over each room at 2600 mm. Two Spaces, one per room: on the first type Design Heating Load 3000 W, Design Cooling Load 5000 W, Specified Supply Airflow 250 L/s; Area and Volume Computations on; **never run Heating and Cooling Loads here**. In the Level 1 plan, W3 alone: Override Graphics in View, By Element, Surface Patterns, a foreground pattern and no colour. **"Heron Two Bodies.rfa"**, a Generic Model family: two 200 mm cubes 2000 mm apart, each with Visible linked to its own Yes/No type parameter "Show A", "Show B"; loaded, placed once on Level 1, "Show B" unticked. **Last**, Manage, Project Parameters: "Comments" (Text, Instance) bound to Walls, and "Keywords" (Text, Instance) bound to Materials; on "Heron A", Identity tab, Keywords "probe". Save. Save As "Heron Reads Test - earlier.rvt" beside it; open "Heron Reads Test" again, change W3's Mark to "D" and type something in the first Comments of W3; Save; **close the "earlier" file**. **"Heron Flag Test.rfa"**, a Generic Model family: Yes/No "Flag" ticked, "Derived" = `not(Flag)`, "Plain" unticked with no formula, Integer "Count" = 1; a solid extrusion 1000 x 1000 from 0 to 1000 mm and a void extrusion, circle radius 100, from -10 to 1010 through it, cut with Cut Geometry. And a scratch copy of Revit 2024's `Samples\Snowdon Towers Sample Architectural.rvt`, as [round 20](../../tools/jobs/p1102-round20-revit2024-arch-2026-11-02.yaml) used it | Each title bar names a scratch file. A wall, selected, shows **Comments twice** in Properties, and a material shows **Keywords twice** - if either shows it once, Revit refused the second parameter and NK8 has nothing to refuse: write down what Revit said. A room's Limit Offset reads below 2600 |
| **NK1** | *`measure-ceiling-height`, row 5b-1128.* The batch job: SET_ROOM_LIMITS raises both rooms to Level 2 inside the rolled-back run. **Then by hand, limits left alone:** select the two rooms, `python mcp/client/heron_bridge_client.py fragment measure-ceiling-height --set includeLinks=false` | Batch: `mainHeight` 2 entries at 2600 mm, `limitBelowCeiling` 0, `findings` 0 - the limit raised, the ceiling is measured. **By hand - the D-30 pair for what changed:** both rooms in `limitBelowCeiling` and in `noCeiling`, `mainHeight` empty, and one finding each naming the room, its limit's height above the floor, *1 ceiling(s)* above it and *2600 mm*. Negative: the same call on a room drawn where no ceiling is - in `noCeiling` alone, **no** finding. If the default rooms ARE measured, the template's limit is above 2600: lower one room's Limit Offset to 2000 and run again |
| **NK2** | *`select-by-material`, row 5b-393.* The batch job: "Heron A", `includePaint` true, Walls; negative "Heron B". **Then by hand:** the same with `includePaint=false` | Batch: `elements` **2** - W1, made of it, and W2, painted with it - and the finding ends *"made of it OR painted with it"*; negative `0 of N scanned`. `elements` 1 with W2 alone is version 3's defect still there. **By hand:** `elements` 1, W1 alone, *"Paint was NOT included"*. The three answers are the row's own test: {W1}, then {W1, W2} |
| **NK3** | *`read-space-loads`, row 5b-323.* The batch job on the two Spaces; negative the walls | `coolingW`, `heatingW` and `airflowLs` hold **one** entry each - the first Space's 5000 W, 3000 W and 250 L/s, each *"(entered)"*, the same numbers Properties shows. The second Space is in `noLoad`. **READ the first Space's finding:** if it ends *"Revit refused Calculated Heating Load, ..."* the repair was exercised - write the names down. If it names nothing, Revit refused nothing on this model and only the unchanged path ran: turn **Area and Volume Computations off** (Architecture, Room and Area panel), run `fragment read-space-loads` on the Spaces by hand, and record what the first Space reads - version 2 said *"NO LOAD READABLE"* for it. **And check the unit, which no proof has yet:** 5000 W typed must read 5000 W; a constant factor off is the card's own first warning |
| **NK4** | *`report-family-parameters`, row 5b-351.* The batch job on "Heron Flag Test"; negative `nameContains=ZZZNOTAPARAMETER`. **Then by hand:** untick Flag in Family Types, run `fragment report-family-parameters --set nameContains=` again | `parameterList` reads *"Flag [Integer/instance] = Yes"* (or `/type`), *"Derived ... = No (formula: not(Flag))"*, *"Plain ... = No"*, and *"Count [Integer/...] = 1"* - **never Yes for Count**. By hand: Flag No, Derived Yes. A `<blank>` on any of the three is the row still there - record which, and whether it is the formula one: that Revit gives the stored tick for a formula-driven Yes/No is **not established** until this runs. This also answers [CP2](group-cp.md) |
| **NK5** | *`report-family-forms`, row 5b-295.* The batch job on "Heron Flag Test", `which=all`; negative the project | `formList`: the solid extrusion's row says *"no volume read on the form itself - it is in a join or cut"* and **not** *"0 L"*; the void carries no volume, as before. `combinationList`: the cut, both members, then *"968.584 L as joined or cut"* (1000 L less a 100 mm radius cylinder 1000 mm long). Negative: `notAFamily` true, nothing read. **By hand, the negative for what changed:** on a copy of the family with the cut removed (Uncut Geometry), the solid reads *"1000 L"* and no row says *"join or cut"*. If the cut solid reads its OWN volume in this Revit, the 0 was release-specific: record the release and the figure |
| **NK6** | *`report-bounding-box`, row 5b-352.* The batch job on the "Heron Two Bodies" instance; negative the two room tags | `measured` 1 and the finding *"1 of the 1 measured are family instances ..."*. **Write down the box:** about 200 mm long means Revit measured body A alone; about 2200 means it took in hidden body B - the question [CP3](group-cp.md) asks. Either way the sentence stands, because it says *can*. **By hand, the negative for what changed:** select two walls, `fragment report-bounding-box --set maxRows=5` - **no** finding about family instances |
| **NK7** | *`read-graphic-overrides`, row 5b-154.* The batch job on the five walls in the Level 1 plan; negative the generic model | `withOverride` **1** and `overrideList` reading *"Generic - 200mm (id ...): surface pattern <the pattern's name>"*; the other four walls each *"NO per-element override in this view"*. `POSITIVE EMPTY` is version 1's defect still there. Negative: `withOverride` 0, `overrideList` empty. **Then, by hand,** give W4 a cut pattern with a red colour and run it again: *"cut RGB(255,0,0), cut pattern <name>"* on W4's line. Undo the override afterwards |
| **NK8** | *The seven readers of row 5b-203, a name two parameters share.* The batch runs all seven. **Four have the refusal as their own negative:** `read-element-parameters` and `find-duplicate-values` on "Comments", `compare-models` on "Comments", and `compare-elements`, whose positive carries it. **Three by hand:** (a) `list-materials` - after the batch, delete the "Keywords" project parameter and run `fragment list-materials --set withAllTabs=true --set "namePattern=Heron A"`; (b) `report-material-takeoff` - add a Number project parameter "Carbon Factor" to Materials **twice** (two parameters of that name; if Revit refuses the second, make one shared and one project), type 100 on "Heron A", select the walls, `fragment report-material-takeoff --set "factorParameter=Carbon Factor"`; (c) `check-sheet-sets-by-parameter` - in the Snowdon copy add a Text project parameter "Drawn By" bound to Sheets, run `fragment check-sheet-sets-by-parameter --set "parameterName=Drawn By" --set setPrefix=none`, then delete the parameter | Batch: `read-element-parameters` negative `values` 0, all five walls in `ambiguous`, findings *"NOT READ - two or more parameters called "Comments" ..."*; `find-duplicate-values` positive `duplicated` 2 (W1, W2), negative `duplicated` 0 and `ambiguous` 5 - **never** *"every Comments is unique"*; `compare-elements` `differing` holds Mark and `ambiguous` holds one line for Comments naming the walls; `compare-models` positive `changed` 1 (W3, 'B' -> 'D'), negative `changed` 0 with `ambiguous` above 0 and a finding naming 'Comments'; `list-materials` identity *"keywords NOT READ - 2 parameters share that name"*, never 'probe'; `report-material-takeoff` and `check-sheet-sets-by-parameter` as their last proofs, `sharedFactor` and `ambiguous` empty. **By hand:** (a) *"keywords 'probe'"* - the name read again once it occurs once; (b) `totalKg` 0 for Heron A, its name in `sharedFactor`, a finding *"FACTOR NOT READ"*, and **not** in `noFactor`; (c) refused *"Nothing was checked: every sheet and view ... has two or more parameters of that name"*, every sheet in `ambiguous`, `inStep` 0 and `outOfStep` 0. **`NEG NOT EMPTY` on the first two is the finding this row exists for** - a value read off whichever parameter came first - unless NK0's second parameter never bound: check Properties before blaming the card. This arranges what [Group AP](group-ap.md) AP11 asks of the other readers |
| **NK9** | *Second route, by eye.* For each card, the route its own `second_route` names: a section through a room for the ceiling; a wall type's structure and the Paint tool for the material; a Space's Properties for the loads; Family Types for the Yes/No; COMBINE_FAMILY_FORMS' own "before, after" for the cut; a section box for the extents; Visibility/Graphics, Override By Element for the pattern; Properties for the two parameters | Each agrees with what the card said. A person's route has no recorded run, so `second_route` on a proof stays NOT ESTABLISHED: write what was seen here |

**Then, for each card whose rows passed:** `python brain/heron_validate.py review <fragment>`, read the
draft, and `python brain/heron_validate.py accept <fragment> --by "Your Name"` - the machine never signs.
`accept` writes the proof; `heron-status: PROVEN` is a line a person edits, and
`python tools/check-signatures.py` says when one is signed and still DRAFT. **A row that fails reopens its
FRAGMENT-ISSUES row**, with what was passed and what came back, verbatim; nothing in a row is changed to make
it pass.
