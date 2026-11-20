# FRAGMENT-ISSUES archive — section 5b, rows 1651 to 1675

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1651 to 1675 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1651

*Moved from the register on 2026-11-17.*

**Defect.** **WHAT A PLAN SHOWS, READ THROUGH THAT PLAN, CHANGED IN THE MIDDLE OF ONE JOB WITH NOTHING ADDED TO OR TAKEN FROM THE MODEL - 258 DUCTS BEFORE THE TAGS WERE PLACED, 557 AFTER - AND ONLY 90 OF THE 244 DUCT TAGS JUST MADE READ AS SHOWN IN IT.** Seen 2026-11-14 on the owner's PC running Group NR's NR1 (`annotate-a-view`). Revit 2020, session 30228, the scratch copy *P1102 MEP NR*, *Level 1 HVAC Duct Plan* (1:100, its own settings, no view template, Phase Filter *Show All*, NR4's four colour filters on it). `FILTER_ELEMENTS_BY_CATEGORY` with `inViewOnly` that plan read *Ducts 258* in NR4 (model 21,463 elements) and again at NR1's first step (*Ducts 258, Air Terminals 105*, model 21,467); `TAG_ELEMENTS_IN_VIEW` then answered `tagged 349` (*Duct Tags 244, Air Terminal Tags 105*), `notTagged 0`, model 21,816. After two kept `ARRANGE_TAGS` calls, the same filter read *Ducts 557, Air Terminals 105, Duct Tags 90, Air Terminal Tags 105* - model still 21,816 - and read the same twice; `FIND_UNTAGGED_ELEMENTS` answered `elements 467`, `alreadyTagged 195`; `REPORT_TAGS_AND_TARGETS` `tagCount 195`, `orphanTags 0`. The duct-tag filter between the two arrange calls read 111. `DIAGNOSE_VISIBILITY` on two of the duct tags not shown (1020385, 1020369) answered only *"4 view filter(s) are applied and one may hide it"* - NR4's filters, which set colour only. The owner saw no Temporary Hide/Isolate border on the plan; his whole-plan screenshot was too small to count tags by. **It flips back and forth.** After the owner opened the plan and zoomed it to fit, the same filter read *Ducts 258* again, `FIND_UNTAGGED_ELEMENTS` `elements 14` (the risers) and `alreadyTagged 349`, the Duct Tags filter 244, and `FIND_OVERLAPPING_TAGS` compared *349 annotation element(s)*. After the next change the owner kept at a Keep box (NR3's `ARRANGE_TAGS`), it read *Ducts 586, Duct Tags 81* and the overlap read compared *186*. Then the owner zoomed the plan out in Revit (a click and a zoom, nothing changed): the filter read *Ducts 258, Duct Tags 244* again. Every overlap read taken straight after a Keep box compared 185 to 195 of the 349 tags. NR1's `ARRANGE_TAGS` was therefore handed 111 of the 244 duct tags and never saw the rest.

**State.** **MOVED TO ISSUE [#935](https://github.com/Ajmalpshaik/Heron-AI/issues/935) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not explained.** The pattern seen: the plan read through Heron loses tags and gains ducts after a change kept at a Keep box, and comes back after the owner opens or zooms the plan - whether the click or the zoom does it was not separated. Whether the Keep box's own select-and-isolate step leaves the plan in that state, and which reading Revit itself would give (a box over the plan, then Filter), was not found. Until it is, a find run straight after a kept change can hand the next step part of the view.

---

### Row 5b-1652

*Moved from the register on 2026-11-17.*

**Defect.** **THE `annotate-a-view` METHOD MOVES TAGS APART ONE TAG CATEGORY A CALL, SO A TAG OF ONE CATEGORY PRINTING ON A TAG OF ANOTHER IS SEEN BY NEITHER CALL AND IS NEVER MOVED - THE METHOD'S OWN CHECK STILL FINDS IT.** Seen 2026-11-14 on the owner's PC running Group NR's NR1, which expects the second overlap read to answer `pairs 0`. Revit 2020, session 30228, *P1102 MEP NR*, *Level 1 HVAC Duct Plan* at 1:100, ducts and air terminals tagged with the project's default tags, no leader. `FIND_OVERLAPPING_TAGS` with `minOverlapMm=2` answered `pairs 77`. Step 4 as the method gives it: a filter of *Duct Tags* (111), `ARRANGE_TAGS` with `minGapMm=200` (2 mm on the paper times the scale), `maxPasses=10` - kept, `moved 75`, `stillOverlapping 50`; then a filter of *Air Terminal Tags* (105) and the same call - kept, `moved 54`, `stillOverlapping 29`. The overlap read again: `pairs 42`, and 40 of the 42 are *"tag 'M_Diffuser Tag' over tag 'M_Bottom Elevation Duct Tag'"* or the other way round. NR3 then asked it on the same plan: one filter of *Duct Tags, Air Terminal Tags* (349) binds, and `ARRANGE_TAGS` takes it - `moved 317` (215 and 102), `stillOverlapping 249` - but it gives every tag handed in ONE size, the median: `halfWidth` 3.27 ft and `halfHeight` 0.67 ft, the duct tag's, where the diffuser tags alone measured 2.59 and 2.10 ft in NR1.

**State.** **MOVED TO ISSUE [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Handing both categories in one call, as NR3 did, is not yet the repair: a tag of the other size is spaced as if it were the median's. The tags still overlapping inside one category (50 and 29) are the card's own `stillOverlapping`, a plan with no room, not this row.

---

### Row 5b-1653

*Moved from the register on 2026-11-17.*

**Defect.** **THE `hvac-spaces-and-zones` METHOD TELLS THE CHAT THAT *"Areas only"* IS *"what a new mechanical project starts on"* - REVIT 2020'S OWN METRIC MECHANICAL TEMPLATE STARTS ON *"Areas and Volumes"*.** Seen 2026-11-14 on the owner's PC running Group NR's NR7. Revit 2020, session 30228: a new project made with `CREATE_FAMILY_DOCUMENT` from `C:\ProgramData\Autodesk\RVT 2020\Templates\US Metric\Mechanical-Default_Metric.rte`, saved as *NR Spaces Host.rvt*. After 91 spaces were made, `REPORT_AREA_VOLUME_COMPUTATIONS` answered *"Volume Computations: Areas and Volumes - Revit computes volumes. 0 of 0 placed room(s) and 91 of 91 placed space(s) have a volume right now"*. Step 8 of the method says *"On \"Areas only\" - what a new mechanical project starts on - EVERY space reads no volume"*, and Group NR's NR7 is written on the same belief. A chat that repeats the method's sentence tells the modeller something this template does not do.

**State.** **FIXED 2026-11-19 - step 8 of brain/skills/hvac-spaces-and-zones.yaml no longer says what a new mechanical project starts on - READ THE SETTING, NEVER ASSUME IT, naming Revit 2020's metric mechanical template, which starts on Areas and Volumes. Group NR's NR7 arranges Areas only by hand, so its premise is an arrangement and stands. tests/test_brain_reachable.py.** Tracked as issue [#960](https://github.com/Ajmalpshaik/Heron-AI/issues/960) until then. Was: **OPEN - recorded, not repaired.** Other releases' and other languages' mechanical templates were not read; whether the sentence is right for any of them, or should say *"read the setting, never assume it"*, is the repair's. The card itself read the setting correctly both ways.

---

### Row 5b-1654

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_HVAC_ZONE` ASKED FOR A NAME ANOTHER ZONE ALREADY HAS MAKES A SECOND ZONE UNDER THAT SAME NAME ON REVIT 2020, AND ITS `refused` SAYS NOTHING OF IT - THE ONLY WORD IS REVIT'S *"Elements have duplicate \"Name\" values"* WARNING, WHICH HERON DISMISSED.** Seen 2026-11-14 on the owner's PC running Group NR's NR10 (b). Revit 2020, session 30228, the scratch host *NR Spaces Host*, where NR7 had made *NR Zone L1*, *NR Zone L2* and *NR Zone L3*. `CREATE_HVAC_ZONE zoneName=NR Zone L2 level=02 - Floor phase=New Construction` with one space (Stair 201, from `FILTER_ELEMENTS_BY_ID`), kept: `added 1`, `movedFromAnotherZone` *"Stair 201 (id 918760) LEFT zone 'NR Zone L2'"*, `refused` only its two information lines, and below the answer *"Revit raised 1 warning while this ran, and Heron dismissed it ... \"Elements have duplicate \"Name\" values.\" (2 elements)"*. A Spaces-free read of HVAC Zones then listed *Default, NR Zone L1, NR Zone L2, NR Zone L3, NR10 Moved, NR Zone L2* - two zones of one name. The card's contract, the `hvac-spaces-and-zones` method (step 6) and NR10 all say the second zone is made *"under whatever name Revit gave it"* and that a line in `refused` says so; on this release Revit gives it the asked name.

**State.** **MOVED TO ISSUE [#943](https://github.com/Ajmalpshaik/Heron-AI/issues/943) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Whether the card refuses a name that is taken, or makes the zone and says in `refused` that its name is now a duplicate, is the repair's. Revit 2024 and later were not tried.

---

### Row 5b-1655

*Moved from the register on 2026-11-17.*

**Defect.** **`SYNC_SPACES_WITH_LINKED_ROOMS` ASKED FOR THE FIRST PHASE MAKES ITS SPACES IN THE LAST ONE ON REVIT 2020 - THEY READ BACK `Phase` *New Construction* AFTER `phaseName=Existing` - AND ITS ANSWER NEVER SAYS WHICH PHASE THEY ARE IN.** Seen 2026-11-14 on the owner's PC running Group NR's NR9. Revit 2020, session 30228, the scratch host *NR Spaces Host 2* (made from `Mechanical-Default_Metric.rte`: phases *Existing* then *New Construction*), `P1102 ARCH.rvt` linked and Room Bounding. `SYNC_SPACES_WITH_LINKED_ROOMS linkName=P1102 ARCH.rvt phaseName=Existing updateNames=false`, kept: `created 91`, `wantedPhase Existing`, no line naming a phase. The same call again: `alreadyMatching 91`, `created 0`. `READ_ELEMENT_PARAMETERS parameterName=Phase` on three of the new spaces (Vest. 101, Lobby 102, Cafeteria 121): *"918894: New Construction (instance); 918895: New Construction (instance); 918896: New Construction (instance)"*. The card's C# finds the phase by name and passes it to `doc.Create.NewSpace(onLevel, onPhase, ...)` (its line 150), so why Revit's space carries the last phase was not found. Row 5b-1589's worry - a second run not seeing first-phase spaces - was therefore not tested: no space reached the first phase.

**State.** **MOVED TO ISSUE [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not explained.** The card should read each new space's phase back and say it, and refuse or warn when it is not the phase asked. Revit 2024 and later were not tried.

---

### Row 5b-1671

*Moved from the register on 2026-11-17.*

**Defect.** **NO TOOL JOINS ONE ROOF TO ANOTHER (Join/Unjoin Roof), AND NONE CAN: REVIT HAS NO API FOR IT. ON REVIT 2024 *"JOIN THE MAIN ROOF TO THE WING ROOF"* REACHES `CREATE_EXTRUSION_ROOF`, WHICH MAKES A NEW ROOF.** Reported 2026-11-11 by Sruthina on Villa 2, Revit 2024 (issue #852; #853 items A2 and B5): the main roof could not be joined to the wing roof, so it was rebuilt clipped to the valley; a stray roof piece was deleted and the roofs rebuilt. **Confirmed in the API, not run:** no Join Roof member in RevitAPI.xml 2020.2.90, 2024.0.0 or 2025.4.60 - only failure names (`CannotJoinRoof`). The one route is `PostableCommand.JoinOrUnjoinRoof` (2020 and 2024), which starts Revit's tool and waits for the modeller to pick the roof edge and the face - not a job an add-in can finish. `JOIN_GEOMETRY` (DRAFT) is Join Geometry, a different command: two elements share their overlap, nothing is extended. **Measured 2026-11-15** (`heron_brain.lookup`, Revit 2024, model backend, scratch store): *"join the main roof to the wing roof"* ranks `CREATE_EXTRUSION_ROOF` first, then `SLOPE_ROOF_EDGES` and `CREATE_EDGE_SWEEPS` - none joins a roof. *"cut away this piece of roof"* ranks `CUT_GEOMETRY` (DRAFT), which asks Revit `CanElementCutElement` first and refuses a pair Revit will not cut with Revit's reason; whether Revit cuts a roof with a roof is not known until that card is run.

**State.** **FIXED 2026-11-20 - on the owner's answer of 2026-11-19, *"join the main roof to the wing roof"* reaches SHAPE_WALL_TOPS_TO_ROOF - the other way, for the walls under a roof - with a new method, `brain/skills/join-roofs.yaml`, named first beside it. The method says Revit's API has no Join/Unjoin Roof, naming the release revit_health reports (read 2026-11-20 in the API documentation of the reference assemblies 2020.2 to 2027.4: only failure names), offers SHAPE_WALL_TOPS_TO_ROOF (2022 on) or ATTACH_WALLS (2025 on) and asks first, and otherwise sends the job to Modify > Join/Unjoin Roof by hand, never to a tool that makes a new roof. A paragraph in SHAPE_WALL_TOPS_TO_ROOF's purpose carries the words there, and a routing row on that card claims the sentence. `tests/test_brain_reachable.py` checks the answer and the method's order, on `lexical` and on `model`; on the tree before, its five checks fail. Measured on the frozen set, the roof change alone: `lexical` 2 more of the 4 join sentences to the card, nothing away; `model` all 4 and one other, nothing away. The roof-to-roof tool - measure with a ray where they meet, then extend - is wanted and not built: row 5b-1877.** Tracked as issue [#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918) until then. Was: **OPEN - recorded; no capability can be written.** What can be fixed is the answer: the sentence should reach no tool and say that Join/Unjoin Roof is done by hand in Revit (Modify > Join/Unjoin Roof). Heron has no way to say *"no tool"* for words that rank a near tool, so this needs either a declared "done by hand" entry the lookup can match, or the owner's word that a near miss is acceptable. `CUT_GEOMETRY` is already the tool for B5 and waits on its proof.

---
