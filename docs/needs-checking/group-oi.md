# Needs checking — Group OI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OI - a night on Revit 2024 with the owner asleep: DRAFT cards proved on a scratch project, everything they need made inside the rolled-back run (2026-11-16)

**Tracked as GitHub issue [#1040](https://github.com/Ajmalpshaik/Heron-AI/issues/1040) since 2026-11-19** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

The owner's night rules ([fragment-proving skill, *A night run*](../../.claude/skills/fragment-proving/SKILL.md))
on the 84 DRAFT cards of main f86a5843 - 524 PROVEN, 6 STALE by `check-signatures.py`. Each card was read
against its own register rows first; the ones a fresh project can be given in a rolled-back run are the
ten below (OI1-OI10), and eight more ran on a kept rig (OI11-OI18). **Fourteen PASS and two tracked (OI19, OI20) - all sixteen
signed by the owner the same night, below; the only thing kept is the rig, in its own scratch file.**

**The PC, every row:** Revit 2024.3 (build 24.3.60.12), session 8292, add-in 0.2.0.0 file
`80819edd3983a0d5` (main f86a5843); Ask to Keep OFF, Changes / Admin / Publish ON (pressed by the owner
~19:50 - all three read false at Revit start). **The model:** `night-scratch` - a file copy of
`%TEMP%\heron-work\ny-scratch.rvt` (Heron's DefaultMetric project, [Group NY](group-ny.md)), 3,203 placed
elements, saved. **The door:** the command line only, `HERON_CLIENT_ID=ajmal-pc`, one Revit.
**The job file:** [`tools/jobs/night-revit2024-oi-2026-11-16.yaml`](../../tools/jobs/night-revit2024-oi-2026-11-16.yaml),
with the values that ran. `python tools/batch-prove.py <it> --unattended --session 8292`, in five batches
back to back.

| Row | What to check | Pass when |
|---|---|---|
| **OI1** | *`report-analytical-model` ([Group MG](group-mg.md)).* Setup `create-analytical-elements` draws one analytical member, `points=0,0,0; 6000,0,0` on Level 2. Negative: `level=Level 9` | Positive `memberCount 1`; negative `memberCount 0` |
| **OI2** | *`set-circuit-naming-scheme` v3 ([Group LI](group-li.md), LI2 re-arranged).* `action=make`, `parts=panel; circuit number`. Negative: `parts=panel; colour` | Positive `changed 1`; negative `changed 0`, `colour` refused |
| **OI3** | *`list-images`.* Setup `import-image` (a PNG) on Level 1. Negative: a PNG that does not exist | Positive `imageCount 1`; negative `imageCount 0` |
| **OI4** | *`reload-images`.* Setup `import-image`; the image by its name. Negative: a name not in the model | Positive `reloaded 1`; negative 0 |
| **OI5** | *`report-paths-of-travel` ([Group LJ](group-lj.md), LJ2 adapted).* Setup `create-paths-of-travel`, one path 20000,20000 to 30000,20000 mm on Level 1. Negative: no start, so nothing is drawn | Positive `pathCount 1`; negative 0 |
| **OI6** | *`create-shaped-stairs` ([Group MN](group-mn.md), MN6's sketch pair).* Boundaries 7000 mm long, 1500 mm apart, Level 1 to Level 2 (4000 mm). Negative: 3 riser lines | Positive `created 1`; negative refused |
| **OI7** | *`tag-areas` ([Group KH](group-kh.md), KH3).* Setup `create-area-plan`, `create-room-separation-lines` (area), `place-rooms` (area). Negative: `tagType=Room Tag` | Positive `tagged 1`, bound `elements from place-rooms as 'created' (1)`; negative 0 |
| **OI8** | *`dimension-views-by-rules` ([Group MU](group-mu.md), MU2).* Setup `create-mep-runs`, two parallel ducts at z 1000. Negative: reach 1 mm | Positive `newDimensions` 1; negative empty |
| **OI9** | *`tag-views-by-rules` ([Group MT](group-mt.md), MT3).* Setup `place-line-based-family`, one `UB-Universal Beams: UB305x165x40` beam. Negative: a shortest run of 100000 mm | Positive `newTags` 1; negative empty |
| **OI10** | *`convert-to-flex-duct` ([Group LP](group-lp.md), LP2 re-arranged).* Setup `create-mep-runs` + `connect-open-ends`, three ducts joined end to end. Negative: `flexDuctType=Flex` | Positive: the middle duct converted |

### Run 2026-11-16, night - nine PASS, one POSITIVE EMPTY, drafts in `brain/proof-drafts/` on the PC

| Row | Result |
|---|---|
| OI1 | **PASS** - `memberCount 1`, *"1 analytical member(s)"*; negative `memberCount 0`. Only the member tied to no physical element is exercised; DQ7 (members named to their beams) is still owed |
| OI2 | **PASS** - `changed 1`, *"1 circuit naming scheme made."*; negative `changed 0`, refused *"\"colour\" is not a circuit parameter Heron can name."* LI7 still needs the owner's two project parameters |
| OI3 | **PASS** - `imageCount 1`, *"1 image(s) in Manage Images"*; negative `imageCount 0`, *"No image or PDF page is in this model's Manage Images."* |
| OI4 | **PASS** - *"1 image(s) reloaded and read back; 0 not reloaded."*; negative refused *"None of the names given is an image here. NOTHING WAS RELOADED."* |
| OI5 | **PASS** - `pathCount 1`; negative `pathCount 0`. Only after the optional texts were given as `""`: left out, they were refused - [row 5b-1841](../fragment-issues/row-5b-1841.md) |
| OI6 | **PASS** - `created 1` (id 352552), *"22 risers of 181.8 mm (22 by run), 250 mm treads, 1500 mm run width, 0 landing(s), 22 riser line(s) spread evenly along the boundaries"*, and *"Revit added 2 railings of type '1100mm' with the stair"*; negative refused *"3 riser lines make risers of 1333.3 mm"*. **Evidence for [row 5b-1456](../fragment-issues/section-5b-rows-1451-1475.md) (issue #903): Revit 2024 accepted 22 riser lines for 22 risers, the first and last included.** The row stays the owner's to close |
| OI7 | **PASS** - `tagged 1`, *"1 area tag(s) placed in \"Heron Area Plan\" (Gross Building)"*, bound *"elements from place-rooms as 'created' (1)"*; negative `tagged 0`, refused *"'Room Tag' is 'M_Room Tag: Room Tag' (Room Tags), not an area tag type"*. The first run of `create-room-separation-lines` and `place-rooms` on their area branch |
| OI8 | **PASS** - `newDimensions` 1 (352857), *"1 run(s) dimensioned with 1 dimension(s)"*; negative empty. `dimensionType` given as `""` (row 5b-1841) |
| OI9 | **PASS** - `newTags` 1 (352866), *"1 tag(s) placed in 1 view(s) by 1 rule(s)"*; negative empty. First run refused the bare name: *"2 family types in night-scratch are called \"UB305x165x40\" ... UB-Universal Beam-Section: UB305x165x40, or UB-Universal Beams: UB305x165x40"* |
| OI10 | **POSITIVE EMPTY** - *"0 of 3 converted"*; the middle duct: *"2 end(s) of the new flex duct did not join - put back as it was"*, the outer two *"an end is not joined to anything"*. **Cause not established** - the ducts were joined end to end with no fitting (DefaultMetric loads no duct fittings); whether the card or the arrangement is short is not known. Owed on a project from the mechanical template - [row 5b-1842](../fragment-issues/row-5b-1842.md) |

**Every positive on the sign list was read for *"NOTHING WAS"*, *"Revit refused"*, *"could not"* and
*"threw"*: none carries one** (OI4's *"NOTHING WAS RELOADED"* is its negative). Every draft has one gap,
*no second route was run*. Six of the nine name an `expect:` in the job; `accept` judges on the
contract alone, so a refusal at signing is possible and is the signal, not a fault of the night.

### The rig - eight more cards on a KEPT arrangement (2026-11-16, ~21:00-21:30)

The cards a set-up step cannot arrange (the morning list below, row *cut-geometry ...*) were arranged on
**`rig`**, a second file copy of `ny-scratch.rvt`, opened by `activate-document`. Twelve elements were KEPT
in it by PROVEN cards - `fragment --in rig <card> --write --apply`, Ask to Keep off,
`HERON_CONVERSATION_ID=rig-1116` for the series - **and no box came up**:

| # | Card | Kept |
|---|---|---|
| 1 | `create-wall` | room box: 4 walls `Generic - 200mm`, x 30000-34000, y 30000-33000, base offset -100, 3000 high |
| 2 | `create-wall` | 2 walls end to end along y 37000, x 30000-34000 |
| 3-4 | `select-in-region` (read, 2) -> `create-parts --expect-from select-in-region` | `madeFrom 2`, parts 352524, 352525 |
| 5 | `create-floor` `Floor: Generic 150mm` | S2 (352528) under the room box |
| 6 | `create-floor` | S1 (352544), 2 x 2 m at x 30000-32000, y 41000-43000 |
| 7 | `place-rooms` at 32000,31500 | room 352550, `unbounded 0` |
| 8 | `create-room-elevations` at 32000,31500, 4 slots | marker and Elevation 1 - a to d |
| 9 | `place-line-based-family` `UB-Universal Beams: UB305x165x40` | two beams crossing at 41000,30000 (352589, 352591) |
| 10-11 | `select-in-region` (read, 1) -> `create-assembly` | "RIG Assembly", member 352544 |
| 12 | `create-schedule` | key schedule "RIG Room Key" (352597) |
| 13 | `create-schedule` | "RIG Wall List" of walls (352605) |
| 14 | `create-stairs` straight, 1500 wide, x 47000-48600 | stair 352613: 22 risers, 1 run, 0 landings, 2 railings |

**Saved from behind:** `activate-document` night-scratch, `save-document` (Publish) `rig.rvt` - *"'rig' saved
to ...rig.rvt. This cannot be undone"*, the file's time and size changed (21:20:29, 5,804,032 bytes) - then
`rig` in front again. The same loop had been tried first on one kept grid line at 20:50. **Jobs:**
[`tools/jobs/night-rig-oi-2026-11-16.yaml`](../../tools/jobs/night-rig-oi-2026-11-16.yaml), all rolled back.
Afterwards `rig` was saved and closed by `save-document` with `closeWindow` (*"'rig' is closed."*).

| Row | Card | Result |
|---|---|---|
| OI11 | `join-geometry` v4 (STALE) | **PASS** - S2 the target, 8 elements bound: `changed 4`, *"4 joined with 'Generic 150mm'. 0 were already joined"*; negative S1 the target: `changed 0`, *"0 joined with 'Generic 150mm'"* - **row 5b-1661's own check, seen in Revit: a floor 4 m from every wall joins nothing** |
| OI12 | `merge-parts` | **PASS** - *"2 part(s) merged into one"*; negative (walls, no part) *"Two parts or more are needed to merge. NOTHING WAS MERGED."* |
| OI13 | `create-assembly-views` | **PASS** - RIG Assembly, `created` 1 entry; negative (floors, no assembly) `created` 0 |
| OI14 | `place-stair-tread-numbers` | **PASS** - *"1 run(s) numbered in 'Level 1'"*, `placedIds` 352770; negative (the 2 railings) `placed 0`, *"Railings '1100mm' (id 352621): not a stair or a stair run"* |
| OI15 | `add-key-schedule-rows` | **PASS** - RIG Room Key, `added 2`; negative *"NO KEY WAS ADDED: 'RIG Wall List' is a schedule of elements"* |
| OI16 | `cut-geometry` | **POSITIVE EMPTY - the arrangement:** *"Structural Framing UB305x165x40 (id 352589) cannot cut anything: it is not a family with a void that cuts when loaded, and Revit does not allow it in a cut between solids. NOTHING WAS CHANGED"* - Revit's rule (Group LW's LW2 warned of it); owed on a pair Revit lets cut |
| OI17 | `report-geometry-cuts` | **POSITIVE EMPTY** - rests on OI16: *"0 cut(s) across 2 element(s)"*. **But the DRAFT `cut-geometry` ran as its set-up step inside batch-prove** - bound *"elements from cut-geometry (2)"* - the first time that path was seen in a real Revit |
| OI18 | `refit-room-elevations` | **POSITIVE EMPTY - a card defect, measured:** `roomNumbers ""`: *"0 interior elevation(s) refitted to their room and read back."*, *"No elevation marker stands inside any room."* - though the marker was made at 32000,31500, the room's own point, inside walls standing from -100. A rolled-back probe then showed every elevation marker's model box is null, so the card skips them all - [row 5b-1843](../fragment-issues/row-5b-1843.md) |

**Seen tonight, for other rows:** seven batches ran back to back, each handing the models back, and no
batch was locked out by the batch before it - real-Revit evidence for [row 5b-1656](../fragment-issues/row-5b-1656.md).
The rig batch did wait: *"held by another chat, asking again in 30 s (try 2 of 25): Model 'rig' is in use by
another chat"*, tries 2 to 4 - the kept series' own `HERON_CONVERSATION_ID` hold, 3 quiet minutes, as
designed - and then ran: `--unattended`'s asking again, seen working.
**Seen once:** a DRAFT card as a set-up step inside batch-prove (OI17). **Not seen:** the "Virtual Memory -
High Usage" closer ([Group IW](group-iw.md)) - Revit never went busy. **Memory:** Revit 2024's private bytes
rose from 1.97 GB at the start to 11.6 GB after the rig's jobs; the night stopped new compiles there, short
of the 13 GB limit.

**Not run tonight, and why** (the morning list):

| Card(s) | Why |
|---|---|
| export-families, export-model-to-ifc, export-model-to-nwc, set-worksharing-ownership, sync-with-central (PUBLISH); publish-coordinates, remove-project-parameter (ADMIN) | No batch sends one (D-106); Publish was switched on only to save scratch models |
| set-element-level | Held by [row 5b-1831](../fragment-issues/row-5b-1831.md) |
| report-bounding-box | Ran PASS in [Group NK](group-nk.md) NK6, its premise did not hold (row 5b-352) - the owner's call |
| report-level-elevations (STALE) | Needs a model with a survey point vertical offset; only `set-project-location` (ADMIN) makes one |
| pick-on-screen, place-legend-components, set-design-option, add-area-based-load, adjust-analytical-elements, associate-analytical-elements | The owner's hands: a pick, a hand-placed legend component, a design option, Area Based Load Boundary lines, a kept DRAFT |
| add-fabrication-hangers, change-fabrication-service-size, convert-to-fabrication, optimize-fabrication-lengths, place-fabrication-part, report-fabrication-setup, connect-into-main, place-flow-arrows, draw-mep-from-cad, import-gbxml | Content a DefaultMetric project cannot be given by a set-up step: a fabrication configuration, tap fittings, a flow arrow family, a test DWG, a gbXML file |
| check-rebar-against-spec, create-rebar-shape, place-fabric-reinforcement, place-fabric-sheet, place-multi-rebar-annotation, place-rebar, place-reinforcement-symbols, place-structural-connections, report-structural-connections | Rebar and connection content the structural template carries and DefaultMetric does not |
| add-stairs-landing, convert-cad-to-directshape | A set-up step cannot hand on what they need (only `create-wall` hands its elements on, a read step after a change step is refused - row 5b-917, stair creators are refused as set-up steps - row 5b-1556), and the rig above had no landing-less two-run stair or CAD with solids. cut-geometry, report-geometry-cuts and refit-room-elevations: OI16-OI18 |
| report-active-workset, set-active-workset, create-workset-3d-views, report-element-ownership (STALE), report-linked-view-usage, reload-links (STALE), repoint-view-reference, insert-views-from-file, add-family-type-parameter | A workshared model, a link, reference views, a second saved project, a family document - none on the night's model |
| refresh-view, report-project-units, report-open-documents, create-energy-model | D-53 tracking by `validate`, one at a time - not a batch |
| report-structural-settings | Never empty: [Group MF](group-mf.md) MF2 read NEG NOT EMPTY on 2026-11-14, and its tracking negative (MF5) needs two ADMIN cards |
| place-column-rebar, set-mep-justification, create-extrusion-roof, run-systems-analysis, measure-available-fall, measure-distance, describe-blank-parameters, show-analysis-heatmap, place-span-direction-symbols, create-property-line, create-arc-length-dimension, create-radial-dimension, run-heating-cooling-loads | An open row (5b-1644, 5b-1397/1409, 5b-1648, 5b-1460), an input shape no job can type, a contract `accept` cannot judge, or a `revit:` list without 2024 |

### Two cards that cannot come back empty, proved by tracking (D-53), later the same night

`validate --write --vary`, one at a time, on `night-scratch`, rolled back; Revit 2024 at 11.55-12.26 GB.

| Row | Card | Result |
|---|---|---|
| OI19 | `report-browser-organization` | **TRACKED** - set-up `create-sheets` (`titleBlockName=none`), `--vary "numbers=HZ-T1\|HZ-T1,HZ-T2\|HZ-T1,HZ-T2,HZ-T3"`, field `organization`: the Sheets line read *"0 hidden by its filter, 1 shown"*, then *"2 shown"*, then *"3 shown"*; the Views line stayed *"10 shown"*. Draft written. **The owner's call:** whether following the sheet count is enough for a card about the browser's organisation - its own positive, a filter that hides views, no card can make |
| OI20 | `trace-connectivity` | **TRACKED** - set-up `create-mep-runs`, `--vary` one Supply Air run of 1, 2 and 3 ducts, field `islandSizes`: *"1 entry(ies)"*, *"2 entry(ies)"*, *"3 entry(ies)"*, bound *"elements from create-mep-runs (1)"*, *(2)*, *(3)*. Draft written. **The owner's call:** with no duct fitting in the template the ducts stay unjoined, so each is its own island - it follows the pieces, not a connected run |

**Signed 2026-11-16, as Ajmal PS, on the owner's word** (*"Yes, sign all 14"*): `report-analytical-model`,
`set-circuit-naming-scheme`, `list-images`, `reload-images`, `report-paths-of-travel`, `create-shaped-stairs`,
`tag-areas`, `dimension-views-by-rules`, `tag-views-by-rules`, `join-geometry`, `merge-parts`,
`create-assembly-views`, `place-stair-tread-numbers` and `add-key-schedule-rows` - `accept` on each draft, none
refused, and each card's status flipped DRAFT to PROVEN. OI10 and OI16-OI18 stay owed.

**Signed 2026-11-16, as Ajmal PS, on the owner's word** (*"Yes, sign both"*): `report-browser-organization`
(OI19) and `trace-connectivity` (OI20) - `accept` on each tracking draft, none refused, each status flipped
DRAFT to PROVEN.

### Run 2026-11-17, morning - after the owner restarted Revit 2024 (session 34788)

The night stopped new compiles at 13.5 GB; he restarted Revit 2024 at ~05:12, and the cards the night could not
arrange were run on three scratch copies, from the read-only plan written overnight. Changes, Admin and
Publish on, Ask to Keep off (his clicks). Every proof rolled back; the arrangements below were KEPT by PROVEN
cards (`fragment --in <doc> <card> --write --apply`) in scratch copies and saved from behind by `save-document`.
No box came up. Revit's private bytes: 1.8 GB at the start, 7.7 GB after.

- **`rig`** (night-1116\rig.rvt): a copy of `%TEMP%\heron-work\Family1.rfa` (an empty Generic Model) became
  "HN Cut Void" - `set-family-settings` *"read back: Cut with Voids When Loaded No -> Yes"*, `create-family-forms`
  one VOID extrusion 400 x 400 x 600 and one 50 mm SOLID (the void unattached) - saved and closed, `load-family`
  into rig, `place-family-instances` at 31000,31000 on Level 1 (353594), through floor S2 (352528).
- **`mech-scratch`**: a file copy of `mg\Heron JX Ducts.rvt` (Heron's project from the mechanical template,
  Group JX). Nothing kept.
- **`struct-scratch`**: a file copy of `mg\Heron MG Analytical.rvt` (the structural template, Group MG):
  concrete beams 422018 and 422020 (`M_Concrete-Rectangular Beam: 300 x 600mm`), steel beam 422023
  (`UB-Universal Beams: UB305x165x40`), `place-beam-rebar` on 422018 (*"3 rebar set(s) placed and read back"*,
  bottom 3 x 16M, top 2 x 13M, stirrups 10M @ 200), `create-section-view` across it - "Section 1".

**Job files:** `tools/jobs/morning-rig-cut-oi-2026-11-17.yaml`, `morning-mech-flex-oi-2026-11-17.yaml`,
`morning-struct-rebar-oi-2026-11-17.yaml`.

| Row | Card | Result |
|---|---|---|
| OI21 | `cut-geometry` (owed since OI16) | **PASS** - cutter the void instance (selected by region), S2 by id: *"1 element(s) cut by Generic Models HN Cut Void (id 353594)"*, *"Floors Generic 150mm (id 352528): cut by Generic Models HN Cut Void (id 353594) (void cut) - read back: cut"*; negative `cut false`: *"0 element(s) uncut ... 1 were already not cut by it and were left alone"* |
| OI22 | `report-geometry-cuts` (owed since OI17) | **PASS** - `cut-geometry` (DRAFT then) as its last set-up step: *"Floors Generic 150mm (id 352528) is CUT BY Generic Models HN Cut Void"*, *"1 cut(s) across 1 element(s)"*; negative *"0 cut(s) across 1 element(s)"* |
| OI23 | `convert-to-flex-duct` (row 5b-1842) | **POSITIVE EMPTY** - set-up `create-mep-runs` (three ducts, two corners) -> `fit-mep-joints` (an elbow at each corner), bound *"elements from fit-mep-joints (3)"*: the middle duct *"the new flex duct is not on \"Supply Air\" - put back as it was"*, the outer two *"an end is not joined to anything"*. **The ends now join** - with elbows the flex is no longer refused for open ends; the card's read-back of `RBS_DUCT_SYSTEM_TYPE_PARAM` against the old duct's is what puts it back. Why that value differs is not measured |
| OI24 | `check-rebar-against-spec` | **PASS** - beam 422018: `meets 5` (bottom 3 x 16M, top 2 x 13M, link type 10M, spacing 200, hook 135); negative the steel beam - not a rebar host, `meets 0`, `doesNotMeet 0` |
| OI25 | `place-rebar` | **PASS** - beam 422020: `placed 1` (422041), straight 16M, `readBackWrong` empty; negative the steel beam, nothing placed |
| OI26 | `place-multi-rebar-annotation` | **PASS** - "Section 1", the bars of 422018: `placed 1`. First run named `annotationType` "MRA Rebar Section" and was refused: *"'MRA Rebar Section' is 'M_Structural MRA Rebar Section: MRA Rebar Section' (Structural Rebar Tags), not a multi-rebar annotation type"*, listing *"'Multi-Rebar Annotations: Structural Rebar Section'"* - run again with that name |

Every positive read for *"NOTHING WAS"*, *"Revit refused"*, *"could not"* and *"threw"*: none. Each draft's one
gap: no second route. **Not signed** - the owner's word.

| Row | Card | Result |
|---|---|---|
| OI27 | `add-family-type-parameter` | **POSITIVE EMPTY - row 5b-302 (issue #901) again, on Revit 2024.** "HN Nest Host" (a copy of `Family1.rfa`, Generic Model) with "HN Cut Void" nested by `load-family` + `place-family-instances` (*"nested on the level \"Ref. Level\" at 0, 0, 0 mm - read back"*) and a first type made by `set-family-type-values`; `validate --in "HN Nest Host"`, `category=Generic Models`, `elements=HN Cut Void`, negative-in `rig`: *"\"HN Cut Void\" (...-00000e86) has no type or profile Revit lets be linked to a family parameter - only a nested family's copy or a sweep's profile can be."* (before the type was made, also *"The family has no type yet ..."*); negative *"\"rig\", is a project, not a family"*. 5b-302 found Revit 2024's API refusing every carrier; this is the same refusal on a nested Generic Model - its fix (probe 2027/2020, or retire the card) is unchanged |

**Signed 2026-11-17, as Ajmal PS, on the owner's word** (*"Yes, sign all 5"*): `cut-geometry` (OI21),
`report-geometry-cuts` (OI22), `check-rebar-against-spec` (OI24), `place-rebar` (OI25) and
`place-multi-rebar-annotation` (OI26) - `accept` on each draft, none refused, each status flipped DRAFT to PROVEN.

## Issue #1040 on Revit 2024, 2026-11-20

Revit 2024 (build 24.3.60.12, add-in 0.2.0.0, hash 692548233445f708), session 49656, on the owner's PC, for GitHub
issue [#1040](https://github.com/Ajmalpshaik/Heron-AI/issues/1040). Changes, Admin and Publish on, Ask to Keep off (his
clicks). Scratch copies only, in `%TEMP%\heron-work\issues-1120`; every run rolled back.

| Row | Card | Result |
|---|---|---|
| ~~**OI28**~~ | `refit-room-elevations` v2 (OI18 again, after PR #1021 - row 5b-1843) | **PASS** - *Rig 1040*, a file copy of `night-1116\rig.rvt`; `batch-prove` job `roomNumbers ""`, `marginMm 300`: `refitted 4`, *"4 interior elevation(s) refitted to their room and read back."*, *"room 1, "Elevation 1 - a": crop 3400 x 3038 mm, far clip 2200 mm"* (and b 4400 x 3038 / 1700, c 3400 x 3038 / 2200, d 4400 x 3038 / 1700); negative `roomNumbers 999`: `refitted 0`, *"0 interior elevation(s) refitted to their room and read back in room(s) 999."*, *"No elevation marker stands inside those rooms."* Draft written, **not signed** |
| **OI29** | `convert-to-flex-duct` (OI23's cause, row 5b-1842) | **CAUSE MEASURED - the card's read-back, not the arrangement.** A rolled-back scratch probe (session scratchpad, nothing in the repository) on *Ducts 1040*, a file copy of `mg\Heron JX Ducts.rvt`: three 400 x 200 `Rectangular Duct: Radius Elbows / Taps` ducts on `Supply Air`, Level 1, OI23's points, `NewElbowFitting` at both corners. The middle duct read system type *"Supply Air (id 548513)"*, its system *"Mechanical Supply Air 6"*; the two elbows' connectors *"Mechanical Supply Air 5"* and *"... 6"*. Middle duct deleted, `NewFlexDuct(elbow end, elbow end, "Flex - Rectangular")`: both ends joined (*"flex open ends 0"*); its `RBS_DUCT_SYSTEM_TYPE_PARAM` *"read-only False, reads <none>"*; `Set(Supply Air)` *"returned True, reads now <none>"*; after `doc.Regenerate()` the same parameter and a fresh `get_Parameter` both *"<none>"*, and the flex's `MEPSystem` *"<none>"*. **So inside the transaction Revit has not put the new flex on any system, and will not let it be set** - the card's comparison of the flex's system with the old duct's can never hold before the transaction ends. Not struck: the card's fix is owed (row 5b-1842) |
| **OI30** | `report-level-elevations` (STALE) | **NOT RUN.** A file copy of `Snowdon Towers Sample HVAC.rvt` (its 2026-09-13 proof's model, survey offset 780.5 mm) would not open through `activate-document`: Revit's warnings list came up - journal *"Warning: Space is not in a properly enclosed region - 67 times"*, `Dialog_Revit_DocWarnDialog` - and Heron answered it Cancel, as it answers every box, so the open stopped. The copy's six links are not beside it, so its spaces lose their bounds. Opening it with its links is ~250 MB more on a PC that had 1.3 GB of its 15.4 GB free; no Heron card sets a survey point's height. Owed: open the copy once by hand (Close on the warnings), or a smaller model with a survey offset |
| **OI31** | `convert-to-flex-duct` **v2** (issue [#1119](https://github.com/Ajmalpshaik/Heron-AI/issues/1119), row 5b-1842) | **OWED - a Revit run.** Version 2 judges the system by the two connectors the flex joined when the flex itself reads none (OI29). Run OI23 again: `tools/jobs/morning-mech-flex-oi-2026-11-17.yaml` on a fresh file copy of `mg\Heron JX Ducts.rvt`, rolled back. Pass: the middle duct `1 of 3 converted`, *"... 400x200 on \"Supply Air\" (read from the system of the fittings it joins ...), joined at both ends"*, the outer two *"an end is not joined to anything"*; negative (`Flex - Round`) *"a rectangular duct ... is round"*, 0 converted. A second negative worth a probe: an elbow on another system type beside the duct - *"the fitting it joins is on ..."* and the duct put back |
