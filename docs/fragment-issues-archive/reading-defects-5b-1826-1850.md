# FRAGMENT-ISSUES archive — section 5b, rows 1826 to 1850

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
> Rows 1826 to 1850 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1826

*Moved from the register on 2026-11-17.*

**Defect.** **DRAW_FAMILY_DETAIL_LINES CANNOT NAME A PROJECT'S FLOOR PLAN WHEN A CEILING PLAN HAS THE SAME NAME.** NEEDS-CHECKING Group OC's OC0 step (7), Revit 2024 (session 18772), `Heron OC Tags` (a scratch project from English\DefaultMetric.rte, whose floor plan and ceiling plan are both `Level 1`), 2026-11-16, through `revit_change` from main `bb3235af`: `view=FloorPlan: Level 1`, `view=Floor Plan: Level 1`, `view=Floor Plans: Level 1` and `view=312` (the plan's id) were each refused *"No single view is called ..."*, and `view=Level 1` as ambiguous. Every new project from that template has the clash, so a detail line cannot be drawn in its plan at all. `revit_view_image` accepts the *"ViewType: Name"* form for exactly this case. Worked round by renaming the ceiling plan *Level 1 RCP* (`EDIT_TEXT_VALUES`, field Name, suffix), after which `view=Level 1` drew the line and read it back.

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card is PROVEN; accepting `ViewType: Name` (and an id) moves its C#, so it goes back to DRAFT - the owner's call.

---

### Row 5b-1827

*Moved from the register on 2026-11-17.*

**Defect.** **NO CARD CAN SET A WALL'S LOCATION LINE; WRITE_ELEMENT_PARAMETERS REFUSES IT WITHOUT A REASON.** NEEDS-CHECKING Group OC's OC0 step (11), Revit 2024 (session 18772), `Heron OC Tags`, 2026-11-16, through `revit_change` from main `bb3235af`: wall 352519 selected by `FILTER_ELEMENTS_BY_ID`, then `WRITE_ELEMENT_PARAMETERS` `parameterName=Location Line` with `value=Finish Face: Exterior`, and again with `value=2` (`WallLocationLine.FinishFaceExterior`): both `outcome: REFUSED`, `refused [352519]`, `written 0`, `readOnly` empty and **no line saying why**. A grep of `brain/fragments` finds `WALL_KEY_REF_PARAM` only in `create-walls-by-face`. The modeller set it by hand in Properties. A refusal with no reason is the defect; the missing capability is the gap.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Whether WRITE_ELEMENT_PARAMETERS should write an enumerated wall parameter by its display text is the owner's call; the refusal must at least name why.

---

### Row 5b-1828

*Moved from the register on 2026-11-17.*

**Defect.** **MOVE_ELEMENTS v3 MOVES AN ELEVATION MARKER BUT CANNOT READ IT BACK.** NEEDS-CHECKING Group OC's OC2 by-hand half, Revit 2024 (session 18772), `Heron OC Tags`, 2026-11-16, `validate move-elements --write` from branch `claude/loving-brown-t1boj3` (PR #887), the template's elevation marker 8142 (category Elevations) by id, `offset=1000,0,0`, rolled back: `moved 0`, `unverified [8142]`, *"0 moved the whole offset, 0 part of it, 0 did not move, 1 could not be read back"*. Not refused - the row's main point holds - but OC2 expected it `moved`. An `ElevationMarker` has no `Location` point, so the read-back has nothing to compare; its bounding box in a plan would do.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Group OC's OC2 notes it; the card stays DRAFT v3 until the owner decides whether this blocks the sign.

---

### Row 5b-1830

*Moved from the register on 2026-11-17.*

**Defect.** **A DRAFT'S ONE KEPT RUN (`try-once`) THAT RAISES HERON'S DELETE BOX COMES BACK `needs_unbound` AND KEEPS NOTHING.** NEEDS-CHECKING Group OE's OE7 and OE9, Revit 2024 (session 18772), `Heron OE Roofs`, 2026-11-16, `try-once` from branch `claude/trusting-edison-vloacn` (PR #890): the elements were selected by `SET_SELECTION` and **read back as selected (4) by a separate process just before**, then `try-once set-element-level` (and, earlier, `delete-elements` on roof 352583). The add-in log shows the job ran - *"commit inside Heron's own job ['Heron: set-element-level']"* - then *"Delete box: 'set-element-level' would delete 2 elements in 'Heron OE Roofs'; the modeller answered delete."* - and the client then printed `[needs_unbound]` *"Nothing is selected in Revit ..."*; no Keep or Discard box came and the model read back unchanged. The same `try-once` route kept `shape-wall-tops-to-roof` (no delete) and, in Group OC, `move-elements`. So the kept route breaks exactly where the delete box is asked inside it.

**State.** **MOVED TO ISSUE [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) 2026-11-16** - still open, tracked there; this row is kept whole. Read 2026-11-19, not repaired: the second run is the add-in's - after Delete, RevitFragment.cs goes back to `runAgain` and binds the needs again from the live selection, which the delete box's put-back can leave empty, so `needs_unbound` comes before any Keep or Discard box. No chat-side fix: the repair is the add-in binding the second run from the first run's own ids (C#), and a Revit run proves it. Was: **OPEN - recorded, not repaired.** OE7 and OE9's kept halves are NOT RUN because of it.

---

### Row 5b-1831

*Moved from the register on 2026-11-17.*

**Defect.** **SET_ELEMENT_LEVEL v3 WOULD DELETE 2 ELEMENTS WHEN MOVING FOUR COLUMNS TO ANOTHER LEVEL.** Same run as row 5b-1830: four columns (2 architectural, 2 structural, none attached) and `newLevel=OE Plate`, `topLevelId=OE Upper`, `keepWallBase=true` - Heron's delete box counted 2 elements the job would delete, twice (12:07:13Z and 12:11:22Z). Changing a column's base and top level should delete nothing. Which two is not known: the batch's rolled-back run (OE6, PASS, `moved 4`) raises no box and does not report deletions.

**State.** **MOVED TO ISSUE [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card is held, not offered for signing, until the two are named.

---

### Row 5b-1832

*Moved from the register on 2026-11-17.*

**Defect.** **ROUTING, Group OE's OE10 on the owner's PC (Revit 2024, `model+rerank`), 2026-11-16, branch `claude/trusting-edison-vloacn`:** first through `heron_lookup` on an older scratch store, then **re-measured by `tools/score-routing.py --sentences ... --revit 2024 --backend model+rerank` on a store built from this branch's own 742 cards (tree `cab56658`)** - the same answer both times, the wanted card second each time: *"attach these walls to the roof above"* -> `ATTACH_COLUMNS` (with the line that ATTACH_WALLS is not on Revit 2024), where the row expects `SHAPE_WALL_TOPS_TO_ROOF`; *"delete this roof"* -> `REMOVE_MASS_FLOORS`, where it expects `DELETE_ELEMENTS`. The other three reached their card. The cloud container measured all five as written on its own backend.

**State.** **MOVED TO ISSUE [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - a routing row, not a card's failure.** The 2026 half was NOT RUN.

---

### Row 5b-1836

*Moved from the register on 2026-11-24.*

**Defect.** **[Q-70](../open-questions/tier-2.md)'S WARNING LINE NAMES `heron.config` UNDER A REVIT, WHERE [GROUP OB](../needs-checking/group-ob.md) OB4 FAILS `revit_health` ON "A LINE NAMING `heron.config`".** Found 2026-11-16 in a cloud container (no Revit), merging `origin/main` - [D-166](../decisions/D-166.md), accepted by the owner - into the branch that had built Q-70 (decided on the owner's delegation, not read back). D-166 governs, and its behaviour is kept: the switches line under each Revit names Settings and no file, no refusal names the file, and no line says a file decides Changes, Admin or Publish. What Q-70 keeps is one line, printed under a Revit **only when the chat's own copy of the settings is not the file that Revit reads** - by name, by a file found on one side only, or by Ask to Keep, the one switch D-166 left in the file ([`heron_health.py`](../../mcp/server/heron_health.py) `settings_lines`) - and that line names both files, so it holds `heron.config` twice. With one file nothing is printed, so OB4 passes on a PC like the office one; on the owner's laptop, whose Claude app keeps its own copy ([Group NP](../needs-checking/group-np.md) NP6), OB4 read word for word fails on Q-70's line, not on D-166's. **Not resolvable from D-166's text:** its *"No refusal or message names the file; each names Settings"* is said of the three switches, and D-166 also keeps Ask to Keep in `heron.config` - so whether OB4's *"a line"* means the switches line or any line under the Revit is a reading only the owner can give. Read from the code, not run on the laptop.

**State.** **MOVED TO ISSUE [#1011](https://github.com/Ajmalpshaik/Heron-AI/issues/1011) 2026-11-21** - still open, tracked there. Was: OPEN - recorded, not fixed; the owner decides. Owed: Ajmal PS says which OB4 means. If any line: the warning line names neither path - *"this chat's own copy of the settings is not the one this Revit reads"* - and `tests/test_config_and_health.py` and `tests/test_mcp_serves.py`, which hold both paths in it, change with a comment naming his answer. If the switches line only: OB4's fail condition says so, in a new run note, and this row closes. Until then OB4 is run on the office PC, or its result on the laptop says which line named the file.

---

### Row 5b-1839

*Moved from the register on 2026-11-24.*

**Defect.** **THE PROVIDER ADAPTER STILL PROBES, JUDGES AND CALLS A MODEL FROM INSIDE HERON, WHERE [D-58](../decisions/D-58.md) SAYS HERON MAKES NO MODEL CALLS AND [D-65](../decisions/D-65.md) GIVES REACHABILITY AND THE FALLBACK TO THE HOST.** Found 2026-11-16 cutting `HERON-KRN-MAV-017` to its register row ([row 5b-1747](../fragment-issues/row-5b-1747.md)). Its `judge()` - a probe sorted into unreachable, auth, slow, too small or probe failed - had callers beyond the agent: [`brain/heron_provider.py`](../../brain/heron_provider.py)'s `main()` and [`tests/test_provider.py`](../../tests/test_provider.py), which proves the Ollama adapter's `probe()` against it over a real socket. Rather than break them it moved there, logic unchanged. But the adapter itself, scoped by [docs/37](../37-the-provider-adapter.md) on 2026-09-17, a week after both decisions, probes and calls a local model; docs/37 cites D-58 only for the tokeniser and never names D-65, and the Development agents its §4 said it would unblock were made the host's by [D-80](../decisions/D-80.md). Nothing Heron serves imports the adapter (docs/37), so no answer goes through it today. **Stale with it, not edited:** [`brain/heron_router.py`](../../brain/heron_router.py)'s docstring still gives reachability to *"the Model Availability & Fallback Agent (HERON-KRN-MAV-017)"*; [docs/00d](../00d-additional-requirements.md) §24's note says the router and the adapter are imported *"by `heron_availability.py`"*, which imports neither now; [NEEDS-CHECKING K3](../needs-checking/group-k.md) runs `python brain/heron_availability.py` for the availability judge, which is `heron_provider.judge()` now; and docs/37's §1 table, §2.2 and diagrams name `heron_availability.judge()` (a dated note at its top says where it went). A conflicting implementation is a defect, not an amendment ([AGENTS.md](../../AGENTS.md)).

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-21** - still open, tracked there. Was: OPEN - recorded, not fixed. Owed: the owner's call whether the adapter is retired, kept by a decision as Heron's one exception to D-58 and D-65, or cut to what the host cannot do; then the router's sentence, docs/00d's note, K3's command and docs/37's sections follow that answer.

---

### Row 5b-1840

*Moved from the register on 2026-11-24.*

**Defect.** **THE AGENT COUNT HAS NO RETIRED STATE, SO A RETIRED AGENT WHOSE FILE IS KEPT IS COUNTED BUILT - WHILE THE REGISTER'S OTHER READER, READING THE SAME HEADER, CALLS IT DEPRECATED.** Found 2026-11-16 retiring `HERON-REVIT-RBK-036` on the owner's word ([row 5b-1747](../fragment-issues/row-5b-1747.md)). [`tools/agent-count.py`](../../tools/agent-count.py) sorts every row of [docs/28](../28-agent-registry.md) into BUILT, HOST, DEFERRED or LEFT, and BUILT means only that a source file's `Heron-Agent` header claims the id. Archive-never-delete (`HERON-AHR-RET-010`) keeps [`brain/heron_rollback.py`](../../brain/heron_rollback.py) and its header, so the retired row is counted BUILT, and the register's *"every row built"* line for Revit Engineering reads as every row live; [`brain/heron_agents.py`](../../brain/heron_agents.py)'s `record()` reads the same header and reports its state as `DEPRECATED`. Taking the header off to move the count would be worse: the tool's own last-commit check fires on an id claimed before and by nothing now, which is what it is for. The first retirement, `HERON-ORC-PER-003` ([PROPOSALS F19](../proposals/part-f.md), 2026-11-15), did not show this only because it is a host row and stays counted HOST.

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-21** - still open, tracked there. Was: OPEN - recorded, not fixed. Owed: whether the count gets a fifth state, RETIRED - derived the way DEFERRED is, from the row's own word with a decision named, or from the file's `Heron-Status` - with [`tests/test_agent_count.py`](../../tests/test_agent_count.py) proving it and `tools/balance-of-work.py`, which reads the tool's last line, told of the new word.

---

### Row 5b-1842

*Moved from the register on 2026-11-24.*

**Defect.** **`CONVERT_TO_FLEX_DUCT` CONVERTED NONE OF THREE DUCTS JOINED END TO END WITH NO FITTING: THE NEW FLEX DUCT'S ENDS "DID NOT JOIN".** Seen 2026-11-16 at night, Revit 2024.3, session 8292, `night-scratch` ([Group OI](../needs-checking/group-oi.md) OI10), rolled back. Set-up `create-mep-runs` (three 400x200 `Rectangular Duct: Default` Supply Air ducts, `0,10000,2700; 2000,10000,2700; 3000,10000,2700; 5000,10000,2700`) then `connect-open-ends` (`maxGapMm 5`), keep-chain; `flexDuctType=Flex - Rectangular`. Bound *"elements from connect-open-ends (3)"*. Answer, verbatim: *"Convert to flex duct with \"Flex Duct Rectangular: Flex - Rectangular\": 0 of 3 converted. Rigid duct(s) removed: none."* - the middle duct *"2 end(s) of the new flex duct did not join - put back as it was"*, the outer two *"an end is not joined to anything - Convert to Flex Duct needs both ends joined"*. **Cause not established:** the ducts meet end to end with no fitting between them (DefaultMetric loads no duct fitting), so the flex duct may have had nothing to connect to, or the card's reconnect may be short. **Run again 2026-11-17 on a mechanical-template copy with an elbow at each corner** ([Group OI](../needs-checking/group-oi.md) OI23): the ends now join, and the middle duct is put back for another reason - *"the new flex duct is not on \"Supply Air\" - put back as it was"*: the card sets `RBS_DUCT_SYSTEM_TYPE_PARAM` on the new flex only when it is writable, then compares it with the old duct's (impl, the read-back before `attempt.RollBack()`). So the open-ends half was the arrangement; the system read-back is the card's question now - why the value differs is not measured.

**State.** **MOVED TO ISSUE [#1040](https://github.com/Ajmalpshaik/Heron-AI/issues/1040) 2026-11-20** - the Revit run on the owner's PC (Group OI's OI10 and OI23); this row is kept whole. Was: **OPEN - narrowed, not repaired.** **Cause measured 2026-11-20 on Revit 2024 (build 24.3.60.12, add-in 0.2.0.0), [Group OI](../needs-checking/group-oi.md) OI29:** a rolled-back probe joined a new flex between two elbows on `Supply Air` ducts - both ends joined, its `RBS_DUCT_SYSTEM_TYPE_PARAM` *"read-only False, reads <none>"*, `Set(Supply Air)` *"returned True, reads now <none>"*, still *"<none>"* after `doc.Regenerate()`, and the flex's `MEPSystem` *"<none>"*. Inside the transaction Revit has not put the new flex on a system and will not take one, so the card's system comparison can never hold there. **Owed: the card's fix** - judge the system by the joined neighbours' systems (or leave the system to Revit and read it after the transaction), not by the flex's own parameter mid-transaction. The fix is GitHub issue [#1119](https://github.com/Ajmalpshaik/Heron-AI/issues/1119). **Repaired 2026-11-20 in `convert-to-flex-duct` version 2** (issue #1119): when the new flex reads no system, the system is judged by the two connectors it joined. DRAFT; the re-run is [Group OI](../needs-checking/group-oi.md) OI31.

---

### Row 5b-1843

*Moved from the register on 2026-11-24.*

**Defect.** **`REFIT_ROOM_ELEVATIONS` FOUND NO ELEVATION MARKER IN A ROOM WHOSE OWN MIDDLE THE MARKER WAS MADE AT.** Seen 2026-11-16 at night, Revit 2024.3, session 8292, scratch `rig` ([Group OI](../needs-checking/group-oi.md) OI18), rolled back. Kept beforehand by PROVEN cards: four `Generic - 200mm` walls round x 30000-34000, y 30000-33000 on Level 1 (base offset -100, 3000 high), a room placed at 32000,31500 (`place-rooms`, `unbounded 0`), and `create-room-elevations` at `origin=32000,31500,0`, 4 slots (*"4 elevation(s) made, pointing in the marker's DEFAULT direct..."*). Job: `roomNumbers ""`. Answer, verbatim: *"0 interior elevation(s) refitted to their room and read back."*, *"No elevation marker stands inside any room."* **Cause, measured the same night by a rolled-back probe on `rig`:** the card reads `marker.get_BoundingBox(null)` (impl line 40) and skips the marker when it is null - and **it is null for every elevation marker**: *"marker 352552 views 4 box(null)=NULL box(plan)=(100.820209974, 99.179790026, 0) / (107.070209974, 105.429790026, 0) roomAtPlanMiddle=1 Room 1"*, and the same `box(null)=NULL` for the template's four exterior markers (8142, 8174, 8208, 98746). Read with the Level 1 floor plan instead, the box's middle is inside the room and `GetRoomAtPoint` finds Room 1. **So the card can find no marker in any model as written**; the repair is to read the box in a plan view (or the marker's own position). Not repaired here.

**State.** **MOVED TO ISSUE [#1040](https://github.com/Ajmalpshaik/Heron-AI/issues/1040) 2026-11-20** - the Revit run on the owner's PC (Group OI's OI18, after row 5b-1859's repair), then his signature. Was: **REPAIRED IN CODE 2026-11-17 by [row 5b-1859](../fragment-issues/row-5b-1859.md) - compiled 2020 to 2027, NOT RUN IN REVIT.** `REFIT_ROOM_ELEVATIONS` version 2, still DRAFT, in [#1021](https://github.com/Ajmalpshaik/Heron-AI/pull/1021); it reads the marker's box in the floor plans. Needs a real Revit: re-run OI18. Was: **OPEN - cause found, not repaired.** `rig.rvt` is saved under `%TEMP%\heron-work\night-1116` with the arrangement in it.

---

### Row 5b-1849

*Moved from the register on 2026-11-24.*

**Defect.** **NO TOOL COULD PUT A FAMILY'S REFERENCE PLANES ON A SUBCATEGORY OF THEIR OWN, SO THE OWNER'S STANDING RULE FOR PLANE COLOURS COULD NOT BE KEPT.** Asked 2026-11-17 by Ajmal PS: in every family the outline and centre reference planes stay green, on Reference Planes itself, and every other plane sits on a gray subcategory. Checked that day in a cloud container: [`CREATE_LINE_STYLE`](../../brain/fragments/create-line-style/fragment.yaml) version 2 made a subcategory only under Lines or the family's own category ([row 5b-463](../fragment-issues/section-5b-rows-451-475.md) had recorded another category as not reached); [`SET_FAMILY_PLANE_REFERENCE`](../../brain/fragments/set-family-plane-reference/fragment.yaml) version 1 set Is Reference and Defines Origin only; no fragment's C# read or wrote a plane's Subcategory parameter. The owner's house rule: widen an existing tool, never build a second one.

**State.** **MOVED TO ISSUE [#1034](https://github.com/Ajmalpshaik/Heron-AI/issues/1034) 2026-11-20** - the Revit run on the owner's PC (Group OK), then his signature. Was: **FIXED IN CODE 2026-11-17, NOT YET RUN IN REVIT.** `CREATE_LINE_STYLE` version 3 takes an optional `parentCategory` - a subcategory under any category that takes one, Reference Planes above all; `SET_FAMILY_PLANE_REFERENCE` version 2 takes optional `subcategories` (per plane) and `otherPlanesSubcategory` (every plane that is not an outline or centre plane), reading every plane's Subcategory before and after. Both left out, each card is its previous version to the letter. The rule is in [`family-creation`](../../brain/skills/family-creation.yaml) step 5. Both cards DRAFT, both compile on all eight releases. **Owed:** [Group OK](../needs-checking/group-ok.md) on the owner's PC, then his signature; OK3 settles what Revit stores for "Reference Planes itself".

---

### Row 5b-1850

*Moved from the register on 2026-11-24.*

**Defect.** **[`FRAME_ELEMENTS_IN_CAMERA`](../../brain/fragments/frame-elements-in-camera/fragment.yaml) MEASURED A TOWER'S LOW-ANGLE CAMERA FROM ITS ROOF: *"EYE 450 MM ABOVE 'ROOF'"*, EYE ELEVATION 126450 MM.** Seen 2026-11-18 on Revit 2020, model *Heron Tower - First Build* (33 levels, 'Level 1' at 0 to 'Roof' at 126000 mm). 114 walls selected with `SELECT_IN_REGION`, framed with `shot=low-angle side=front-left aspect=16:9`: the eye stood 450 mm above 'Roof', not above the ground. A version 2 call on the same walls (`shot=none direction=150 lookDownDeg=-25 distanceMm=130000 lensMm=18`) read back *"eye -114940 mm above 'Roof'"* - that road places the eye on the look line, so only its stated height was wrong. **Cause, read in the C#:** version 5 took the base level as the one MOST of the elements stand on (`Element.LevelId`, a tie going to the lower) - row 5b-1301's rule, the owner's word of 2026-10-21, made because one partition on 'Foundation' put the sample house's camera 800 mm low. It does not pick the highest; on this tower 'Roof' must have held more of the 114 than any other level. The per-level counts were not read back, so how many is not known. The typed road (`eyeMm`, `targetMm`, `levelName=Level 1`) framed correctly and is the workaround. **Two rules disagree:** this row asks for the LOWEST level; row 5b-1301 asked for the most-used one. Version 6 keeps both cases: the lowest level, with one bottom level only a stray few stand on (fewer than a quarter as many as on the next level up) passed over for that next one, and a finding naming both. The fix is read and compiled, not run.

**State.** **MOVED TO ISSUE [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) 2026-11-19** - the Revit run on the owner's PC, and his choice of rule. **RUN 2026-11-20 on Revit 2020** (build 20191106_1200, add-in 0.2.0.0, file 01b4315add24c9b2), for issue [#1042](https://github.com/Ajmalpshaik/Heron-AI/issues/1042), by `batch-prove` and `validate --write` on the rolled-back path: **the tower held, the house did not.** (1) *Tower Scratch 1042* (a copy of *Heron Tower - First Build* in `%TEMP%\heron-work\issues-1120`), 114 walls (`FILTER_ELEMENTS_BY_CATEGORY Walls` then `SET_SELECTION`), `shot=low-angle side=front-left aspect=16:9`: framed 114 of 114, readBack *"eye 450 mm above 'Level 1' ... Revit shows Eye Elevation 450 mm and Target Elevation 482 mm"*. Run once more through `try-once` and KEPT by the owner; the picture of 'Heron 1042 Low Angle' (`revit_view_image`) shows the tower whole from the ground up, low, on the left third, 16:9. (2) The same walls, `shot=none direction=150 lookDownDeg=-25 distanceMm=130000 lensMm=18`: readBack *"eye 11060 mm above 'Level 1'"*, Revit Eye Elevation 11060 mm - the same eye as version 5 (126000 - 114940), now a positive height. (3) Negative, both runs: `viewName={3D}` refused *"'{3D}' is an isometric 3D view ... NOTHING WAS CHANGED"*, framed 0. (4) Row 5b-1301's case on *House Scratch 1042* (a copy of Revit 2020's `rac_basic_sample_project.rvt`), 56 walls, `shot=eye-level`: **FAILED** - *"eye 1600 mm above 'Foundation'"*, Eye Elevation 800 mm, no finding; see [row 5b-1870](../fragment-issues/row-5b-1870.md). Not signed: the card stays DRAFT until row 5b-1870 is settled and all three cases hold. Was: **FIXED IN THE CODE 2026-11-18 ([#1024](https://github.com/Ajmalpshaik/Heron-AI/pull/1024)), DRAFT - NOT PROVEN.** Card version 6, back to DRAFT, its test plan given three cases (the tower low-angle, the tower version 2 road, row 5b-1301's sample house). **Owed on Ajmal PS's PC:** the run on *Heron Tower - First Build* and on Revit 2020's sample house, with a negative case and the picture looked at (D-30). **Owed to the owner:** whether "the lowest, passing over a stray few" is the rule he wants, or the plain lowest - row 5b-1301's word is his, so he decides.

---
