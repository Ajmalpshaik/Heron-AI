# Needs checking — Group IU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IU - a dictionary's entries reach the chat (`providesEntries`, `READ_ELEMENT_PARAMETERS` version 5), and the end a flex would move (`LABEL_FAMILY_DIMENSION` version 2) (2026-10-22)

From rows [5b-1325 and 5b-1326](../fragment-issues/section-5b-rows-401-425.md). Both were seen on Revit 2020
(session 78736) on 2026-10-22, building a nested floor-drain family: one parameter read on four connectors
came back as *"values 4 entry(ies)"* and nothing else, and a height labelled from a new, unpinned plane moved
that plane instead of the geometry when flexed, until the plane was pinned by hand. Both fixed the same day
with no Revit:

| What | Where | What changed |
|---|---|---|
| The add-in's reply | [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) `Report` | Every dictionary a fragment leaves travels as its count AND its first twelve entries, in `providesEntries`; `Describe` unchanged (5b-1325) |
| The chat's reply, both doors | [`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) `_provides_lines`; [`heron_run_records.py`](../../mcp/server/heron_run_records.py) `entries` | Each entry prints under its count - *"    14194: Yes"* - and past twelve how many more; NOT SENT only when no entries came. A run record pages `entry` lines (5b-1325) |
| `READ_ELEMENT_PARAMETERS` v5 | [`read-element-parameters`](../../brain/fragments/read-element-parameters/fragment.yaml) | READ. `findings`, a string, says the answer in words: each id, value and where it came from, the first twelve, then blank and absent ids (5b-1325) |
| `LABEL_FAMILY_DIMENSION` v2 | [`label-family-dimension`](../../brain/fragments/label-family-dimension/fragment.yaml) | MODIFY. Each end is read before the label - held FAST (a level, pinned, defines the origin), held SOFTLY (a labelled dimension drives it, or an EQ or a lock ties it to another plane or level) or FREE. A free end beside a softly held one, or two free ends, is named in `freeEnd` with the pin route; beside one held fast it is the ordinary height and nothing is warned of; two held fast have nothing a flex can move; an EQ across a centre settles it. `pinFreeEnd=true` pins a warned-of free end first, read back, and pins nothing, saying why, elsewhere. The same-place refusal names the route (5b-1326) |

**What is done and what is not.** The library compiles for 2020 and 2027 (`tools/check-fragments-compile.py`,
2026-10-22) and every Revit member the add-in calls exists in 2020 to 2027 (`tools/check-api-surface.py`); the add-in builds for 2020 to 2027 (`tools/check-compile.py`, all 23 projects, 2026-10-22).
`tests/test_read_door.py` §7 and `tests/test_run_records.py` carry ten new checks that fail on main's server and
pass on this change. **Nothing here has run in Revit.** Both cards are DRAFT: `revit_change` refuses
`LABEL_FAMILY_DIMENSION` until IU6 is signed, and `revit_read` answers `READ_ELEMENT_PARAMETERS` as DRAFT until
IU1 is. **The add-in must be rebuilt and deployed and Revit restarted** on each release before IU1 to IU4 mean
anything - with the old add-in the chat says NOT SENT exactly as before, which IU4 checks on purpose; the
server needs a Claude app restart. The fragments' C# is read live.

**The family.** A scratch family, never a working one: a new family from Revit 2020's *Metric Generic
Model.rft*, saved as *Heron IT Planes.rfa* in `%TEMP%\heron-work\group-iu`. For IU1 and IU2 any project with
four duct connectors and twenty walls does - a copy of a sample, named in the run record. Run every row on
**Revit 2020**, then IU1 and IU6 again on the newest Revit on the PC.

| # | Run | Look for |
|---|---|---|
| **IU0** | *Negative FIRST.* The four connectors of one duct selected (`FILTER_ELEMENTS_BY_ID`, or Revit's selection), then `revit_read READ_ELEMENT_PARAMETERS parameterName=ZZZNOTHINGHERE` | `findings` reads *"ZZZNOTHINGHERE": 0 of 4 carry a value, 0 blank, 4 absent. Absent: <the four ids>.*; `values 0 entry(ies)` with NO line under it and NO "NOT SENT"; `absent 4 item(s)` |
| **IU1** | *The fault's own case.* The same four connectors, `parameterName=Allow Slope Adjustments` | The reply shows `values 4 entry(ies)` and under it four lines *"<id>: Yes"* (or No), `resolvedFrom 4 entry(ies)` and under it four *"<id>: instance"*; NO "NOT SENT" anywhere; `findings` names all four ids with their values. **Copy the reply's `values` block into the run record whole** - it is the proof that the entries travel |
| **IU2** | *Past twelve.* Twenty walls selected, `parameterName=Comments` with every Comments filled in (SET_PARAMETER_VALUES_BY_ID first if needed) | Twelve *"<id>: <text>"* lines under `values 20 entry(ies)` then *"... and 8 more: the first 12 of 20 are shown"*; `findings` names twelve and says *"... and 8 more - read fewer at a time, or GROUP_AND_COUNT"* |
| **IU3** | *A write's record.* Any `revit_change` whose reply shows an `N entry(ies)` value (one the card provides as a dictionary), kept, then `heron_run_record run=<its id>` | The reply shows that dictionary's entries under its count, and the record pages *"entry     <name>  <key>: <value>"* lines right under *"value     <name>  N entry(ies)"* |
| **IU4** | *An add-in from before the field.* On a Revit whose Heron add-in has NOT been rebuilt from this change, IU1 again | *"NOT SENT: what is in resolvedFrom and values ..."* exactly as before this change - the server does not invent entries it was not sent |
| **IU5** | *Negative FIRST for the label - the ordinary height.* In the scratch family: `CREATE_REFERENCE_PLANES` "Box Top" at Z 250, nothing on it; `ADD_FAMILY_PARAMETERS` Height (length); `SET_FAMILY_TYPE_VALUES` Height=250; then `LABEL_FAMILY_DIMENSION fromPlane=Ref. Level toPlane=Box Top parameterName=Height`, `pinFreeEnd` left out; Undo; the same with `pinFreeEnd=true` | `labelled` *Height drives "Ref. Level" to "Box Top", reading 250 mm*; `freeEnd` EMPTY; a finding *""Ref. Level" is a level and "Box Top" is held by nothing, so a flex of "Height" moves "Box Top" - the ordinary case ..."*; `pinnedEnd` empty. With `pinFreeEnd=true`: `pinnedEnd` still empty, a finding *"pinFreeEnd was asked and nothing was pinned: "Ref. Level" is a level, so "Box Top" is the only end a flex can move ..."*, and Box Top shows NO pin in Properties. **No free-end warning either time** - this is the negative case |
| **IU6** | *The fault's own case.* A new family: "Trap Top" at Z 300 with TWO other labelled dimensions on it (e.g. Trap Top to a "Rim" plane at Z 350 labelled Rim Height, holding 50), "Floor Top" a NEW plane at Z 0 - unpinned, nothing on it; "Trap Depth" (length) = 300; `LABEL_FAMILY_DIMENSION fromPlane=Floor Top toPlane=Trap Top parameterName=Trap Depth`, `pinFreeEnd` left out | `labelled` reading 300 mm AND `freeEnd` *""Floor Top" is held by nothing ... while "Trap Top" is held by the dimension labelled "Rim Height" ... A flex of "Trap Depth" may move "Floor Top" and leave "Trap Top" where it is. Pin it (SET_PIN_STATE, pinned=true) or dimension it to a level ..."*; `pinnedEnd` empty; Floor Top shows NO pin. **Then** `SET_FAMILY_TYPE_VALUES` Trap Depth=400: Floor Top moves to Z 100 (the fault, as warned) - look in the Front elevation, then Undo |
| **IU7** | IU6 again with `pinFreeEnd=true` | `pinnedEnd` *""Floor Top" pinned first, as asked (pinFreeEnd), and reads back pinned: a flex of "Trap Depth" now leaves it where it is and moves "Trap Top" instead."*; Floor Top shows the pin; `SET_FAMILY_TYPE_VALUES` Trap Depth=400 now moves TRAP TOP to Z 400 (and Rim to 450) and Floor Top stays at Z 0 |
| **IU8** | *Both free.* Two NEW planes "A" at Z 0 and "B" at Z 200, nothing on either, "Gap" = 200; label A to B with `pinFreeEnd=true` | `freeEnd` *"Both ends are held by nothing ... which one a flex of "Gap" moves is Revit's choice ... pins "A", the first named."*; `pinnedEnd` names "A"; A pinned, B not; Gap=300 moves B to Z 300 |
| **IU9** | *Same place.* "A" and "C" both at Z 0, "Gap" = 0; label A to C | Refused: *"... are in the same place - there is no distance to label. Pin the one that must stay (SET_PIN_STATE, pinned=true), move the other ..., set "Gap" to that distance (SET_FAMILY_TYPE_VALUES), then label ..."*; nothing made, nothing pinned |
| **IU10** | *Both held fast.* "Box Top" at Z 250 PINNED (`SET_PIN_STATE`), Height=250; label Ref. Level to Box Top, `pinFreeEnd=true` | `freeEnd` EMPTY; a finding *"Both ends are held fast - "Ref. Level" is a level; "Box Top" is pinned - so a flex of "Height" has nothing it can move. Unpin the one meant to move ..."*; `pinnedEnd` empty and the pinFreeEnd finding says *"both ends are already held fast"*. **Then** Height typed as 400 in Family Types: **write down what Revit does** - a refusal box, or nothing moves - into the run record; it is what the finding claims |
| **IU11** | *An EQ across the centre.* "Box Left" at X -300 and "Box Right" at X 300, nothing on either, "Width" = 600; label with `centrePlane=Center (Left/Right)` and `pinFreeEnd=true` | `labelled` and `equalised true` as version 1; `freeEnd` EMPTY; a finding *"With the EQ across "Center (Left/Right)", "Box Left" and "Box Right" move equally about it when "Width" changes - no end is free to move on its own."*; NOTHING pinned, the pinFreeEnd finding saying a pinned end would fight the EQ; Width=800 moves both planes 100 outward |
| **IU12** | *A lock to geometry does not count.* After `EXTRUDE_BETWEEN_PLANES` has built a box locked to "Box Top" and Ref. Level, IU5 again on a fresh "Box Top 2" that the box is also locked to | The finding still reads "Box Top 2" as held by nothing - the lock to the form is not a hold on the plane - and a flex moves the plane and the face with it |

**Then sign** `read-element-parameters` version 5 on IU1 with IU0's negative, after IU2 to IU4 have passed, and
`label-family-dimension` version 2 on IU6 and IU7 with IU5's negative, after IU8 to IU12 have passed - IU10's run record saying what Revit did with two ends held fast, whatever it was. A row that
fails reopens its row; nothing in a row changes to make it pass. IU4 is a statement about the old add-in, not
a signature: it needs no row of its own if every Revit on the PC already runs the new one.

**Route 2 needs `Q-PE-12` as well as `Q-PE-16`.** Route 1 needs only the door.

### Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 (`label-family-dimension`)

**The family.** *Heron IT Planes.rfa* in `%TEMP%\heron-work\group-iu`, made from Revit 2020's *Metric Generic Model.rft*
by `CREATE_FAMILY_DOCUMENT` (so saved from the start). Kept in it, through `revit_change` with PROVEN tools: height
planes Box Top 250, Trap Top 300, Rim 350, Floor Top 0, A 0, B 200, C 0, Box Top P 250 (pinned, `SET_PIN_STATE`),
Box Top 2 220; left-right Box Left -300, Box Right 300, Side West -100, Side East 100; front-back Side South -100,
Side North 100; type *Test* with Height 250, Trap Depth 300, Rim Height 50, Gap 200, Gap C 0, Width 600, Height P 250,
Height 2 220. *Rim Height* labels Trap Top to Rim - ONE labelled dimension on Trap Top, not two - put on by
`try-once` and kept by the owner (`brain/proof-drafts/runs/label-family-dimension.once.json`, which also names both
of its ends free). Five boxes by `EXTRUDE_BETWEEN_PLANES`, all inside Side West/East/South/North: Floor Top to Trap
Top, A to B, Ref. Level to Box Top P, Ref. Level to Box Top 2, and one 600 wide between Box Left and Box Right, Ref.
Level to Box Top. They were built AFTER the first label runs, so that IU5 and IU11 ran with nothing on their planes.
**Box Top 2 sits at 220, not 250**, so that the box locked to it is the only thing on it.

**How each row was run.** Every label: `validate --in "Heron IT Planes" label-family-dimension --write`, rolled back.
Every flex: the same, with `label-family-dimension` as `--setup` and a copy of `flex-family` in the session's
scratchpad that adds one string joining its findings - the reply cuts a list to three items of 60 characters, and the
trial line holds the measurement. The flex reads the five solids together, so a plane is located by the extents of
all five: base Z below 0 means a base plane moved down, a height above 300 means a top plane moved up.

| # | What came back | State |
|---|---|---|
| IU5 | `labelled` *Height drives "Ref. Level" to "Box Top", reading 250 mm*; `freeEnd` empty; finding *""Ref. Level" is a level and "Box Top" is held by nothing, so a flex of "Height" moves "Box Top" - the ordinary case, as a height from Ref. Level is."*; `pinnedEnd` empty. With `pinFreeEnd=true` nothing pinned and the finding saying why | **PASS** |
| IU6 | `labelled` reading 300 mm; `freeEnd` *""Floor Top" is held by nothing - not pinned, not a level, not an origin plane, and no labelled dimension, EQ or lock references it - while "Trap Top" is held by the dimension labelled "Rim Height". A flex of "Trap Depth" may move "Floor Top" and leave "Trap Top" where it is. Pin it ..."*; `pinnedEnd` empty. Flex Trap Depth=400: *"solid 600 x 200 x 400 mm ... base Z -100"* - FLOOR TOP moved down to Z -100, Trap Top stayed. **The row's "Floor Top moves to Z 100" is an arithmetic slip:** Trap Top held at 300 and 400 between them puts Floor Top at -100, which is what Revit did | **PASS** |
| IU7 | `pinnedEnd` *""Floor Top" pinned first, as asked (pinFreeEnd), and reads back pinned: a flex of "Trap Depth" now leaves it where it is and moves "Trap Top" instead."* Flex Trap Depth=400: *"600 x 200 x 400 mm ... base Z 0"* - Floor Top stayed at 0, Trap Top went to 400 | **PASS** |
| IU8 | `freeEnd` *"Both ends are held by nothing ... pins "A", the first named. Pinned now - see pinnedEnd."*; `pinnedEnd` names "A". Flex Gap=500 (not 300, so it shows above Trap Top's 300): *"600 x 200 x 500 mm ... base Z 0"* - B moved to 500, A stayed | **PASS** |
| IU9 | Refused: *"Nothing was made. "A" and "C" are in the same place - there is no distance to label. Pin the one that must stay (SET_PIN_STATE, pinned=true), move the other to where it belongs, set "Gap C" to that distance (SET_FAMILY_TYPE_VALUES), then label ..."*; `labelled` empty, `distanceMm` 0 | **PASS** |
| IU10 | *Version 2 as merged:* *"Both ends are held fast - "Ref. Level" is a level; "Box Top P" is pinned - so a flex of "Height P" has nothing it can move."* **What Revit did** with Height P=400: *"600 x 200 x 450 mm ... base Z -150"* - **Revit moved Ref. Level down 150 mm** and Box Top P stayed at 250. Ref. Level read back NOT pinned. No refusal box: the finding's claim was false - row [5b-1332](../fragment-issues/section-5b-rows-401-425.md) | **FAIL**, fixed the same day |
| IU10 again | *After the 5b-1332 fix:* *""Box Top P" is pinned and "Ref. Level" is a level that is not pinned, so a flex of "Height P" MOVES "Ref. Level" - and every form standing on it - and leaves "Box Top P" where it is (measured on Revit 2020). Unpin "Box Top P" (SET_PIN_STATE, pinned=false) so the flex moves it instead, or label from a plane that is free."*; *"pinFreeEnd was asked and nothing was pinned: "Ref. Level" is the end a flex would move, and pinning the level is not what pinFreeEnd does - unpin "Box Top P" instead."* The flex still *"base Z -150"*: the words now say what Revit does | **PASS** |
| IU11 | `equalised true`; *"With the EQ across "Center (Left/Right)", "Box Left" and "Box Right" move equally about it when "Width" changes - no end is free to move on its own."*; nothing pinned, the pinFreeEnd finding naming the EQ. Flex Width=800: *"800 x 200 x 300 mm, centre X 0"* - 100 outward each side | **PASS** |
| IU12 | *""Ref. Level" is a level and "Box Top 2" is held by nothing, so a flex of "Height 2" moves "Box Top 2" ..."* - the box's lock not counted. Flex Height 2=500: *"600 x 200 x 500 mm ... base Z 0"* - the plane and the face moved together, the level stayed | **PASS** |

IU5 to IU12 were all run again on the fixed code the same day, labels and flexes, and read as above. IU0 to IU4
(`read-element-parameters` version 5) were not run: they need the add-in rebuilt and Revit restarted.

**The draft's negative is IU9, not IU5.** `heron_validate.py draft` judges every `role: result`, and in IU5 the
label is made - `labelled` and `distanceMm` are filled - so with IU5 as the negative the draft read *"the negative
case returned content"*. IU5 is the negative for the WARNING (`freeEnd` empty, recorded above); IU9 is the case with
nothing to label, where every result is empty. The draft at `brain/proof-drafts/label-family-dimension.yaml` has IU6
as its positive and IU9 as its negative, fingerprint `f1dfef10a5fe066a`, second route NOT ESTABLISHED - the flex
measurements above are recorded here, not as a route.
