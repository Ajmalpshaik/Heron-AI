# Needs checking — Group OI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OI - a night on Revit 2024 with the owner asleep: DRAFT cards proved on a scratch project, everything they need made inside the rolled-back run (2026-11-16)

The owner's night rules ([fragment-proving skill, *A night run*](../../.claude/skills/fragment-proving/SKILL.md))
on the 84 DRAFT cards of main f86a5843 - 524 PROVEN, 6 STALE by `check-signatures.py`. Each card was read
against its own register rows first; the ones a fresh project can be given in a rolled-back run are the
ten below. **Nothing was kept, nothing signed.**

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
| OI6 | **PASS** - `created 1` (id 352552), *"22 risers of 181.8 mm (22 by run), 250 mm treads, 1500 mm run width, 0 landing(s), 22 riser line(s) spread evenly along the boundaries"*, and *"Revit added 2 railings of type '1100mm' with the stair"*; negative refused *"3 riser lines make risers of 1333.3 mm"*. **Evidence for [row 5b-1456](../fragment-issues/section-5b-rows-401-425.md) (issue #903): Revit 2024 accepted 22 riser lines for 22 risers, the first and last included.** The row stays the owner's to close |
| OI7 | **PASS** - `tagged 1`, *"1 area tag(s) placed in \"Heron Area Plan\" (Gross Building)"*, bound *"elements from place-rooms as 'created' (1)"*; negative `tagged 0`, refused *"'Room Tag' is 'M_Room Tag: Room Tag' (Room Tags), not an area tag type"*. The first run of `create-room-separation-lines` and `place-rooms` on their area branch |
| OI8 | **PASS** - `newDimensions` 1 (352857), *"1 run(s) dimensioned with 1 dimension(s)"*; negative empty. `dimensionType` given as `""` (row 5b-1841) |
| OI9 | **PASS** - `newTags` 1 (352866), *"1 tag(s) placed in 1 view(s) by 1 rule(s)"*; negative empty. First run refused the bare name: *"2 family types in night-scratch are called \"UB305x165x40\" ... UB-Universal Beam-Section: UB305x165x40, or UB-Universal Beams: UB305x165x40"* |
| OI10 | **POSITIVE EMPTY** - *"0 of 3 converted"*; the middle duct: *"2 end(s) of the new flex duct did not join - put back as it was"*, the outer two *"an end is not joined to anything"*. **Cause not established** - the ducts were joined end to end with no fitting (DefaultMetric loads no duct fittings); whether the card or the arrangement is short is not known. Owed on a project from the mechanical template |

**Every positive on the sign list was read for *"NOTHING WAS"*, *"Revit refused"*, *"could not"* and
*"threw"*: none carries one** (OI4's *"NOTHING WAS RELOADED"* is its negative). Every draft has one gap,
*no second route was run*. Six of the nine name an `expect:` in the job; `accept` judges on the
contract alone, so a refusal at signing is possible and is the signal, not a fault of the night.

**Seen tonight, for other rows:** five batches ran back to back, each handing the models back, and no
job was locked out by the one before - real-Revit evidence for [row 5b-1656](../fragment-issues/row-5b-1656.md).
**Not seen:** a DRAFT card as a set-up step inside batch-prove, and the "Virtual Memory - High Usage"
closer ([Group IW](group-iw.md)) - Revit never went busy.

**Not run tonight, and why** (the morning list):

| Card(s) | Why |
|---|---|
| export-families, export-model-to-ifc, export-model-to-nwc, set-worksharing-ownership, sync-with-central (PUBLISH); publish-coordinates, remove-project-parameter (ADMIN) | No batch sends one (D-106); Publish was switched on only to save scratch models |
| set-element-level | Held by [row 5b-1831](../fragment-issues/row-5b-1831.md) |
| report-bounding-box | Ran PASS in [Group NK](group-nk.md) NK6, its premise did not hold (row 5b-352) - the owner's call |
| report-level-elevations (STALE) | Needs a model with a survey point vertical offset; only `set-project-location` (ADMIN) makes one |
| pick-on-screen, place-legend-components, set-design-option, add-area-based-load, adjust- / associate-analytical-elements | The owner's hands: a pick, a hand-placed legend component, a design option, Area Based Load Boundary lines, a kept DRAFT |
| six fabrication cards, connect-into-main, place-flow-arrows, draw-mep-from-cad, import-gbxml | Content a DefaultMetric project cannot be given by a set-up step: a fabrication configuration, tap fittings, a flow arrow family, a test DWG, a gbXML file |
| check-rebar-against-spec, create-rebar-shape, place-fabric-reinforcement, place-fabric-sheet, place-multi-rebar-annotation, place-rebar, place-reinforcement-symbols, place-structural-connections, report-structural-connections | Rebar and connection content the structural template carries and DefaultMetric does not |
| cut-geometry, join-geometry (STALE), report-geometry-cuts, merge-parts, add-stairs-landing, place-stair-tread-numbers, create-assembly-views, refit-room-elevations, add-key-schedule-rows, convert-cad-to-directshape | A set-up step cannot hand on what they need: only `create-wall` hands its elements on, a read step after a change step is refused (row 5b-917), stair and assembly creators are refused as set-up steps (row 5b-1556) - a KEPT rig on a saved scratch would open them |
| report-active-workset, set-active-workset, create-workset-3d-views, report-element-ownership (STALE), report-linked-view-usage, reload-links (STALE), repoint-view-reference, insert-views-from-file, add-family-type-parameter | A workshared model, a link, reference views, a second saved project, a family document - none on the night's model |
| trace-connectivity, refresh-view, report-browser-organization, report-project-units, report-open-documents, create-energy-model | D-53 tracking by `validate`, one at a time - not a batch |
| place-column-rebar, set-mep-justification, create-extrusion-roof, run-systems-analysis, measure-available-fall, measure-distance, describe-blank-parameters, show-analysis-heatmap, place-span-direction-symbols, create-property-line, create-arc-length-dimension, create-radial-dimension, run-heating-cooling-loads | An open row (5b-1644, 5b-1397/1409, 5b-1648, 5b-1460), an input shape no job can type, a contract `accept` cannot judge, or a `revit:` list without 2024 |
