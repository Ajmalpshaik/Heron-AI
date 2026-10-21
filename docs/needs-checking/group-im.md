# Needs checking — Group IM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IM - the interior camera inside its room, the close-range picture on the elements' middle, and a sun's time on the site's clock: `FRAME_ELEMENTS_IN_CAMERA` version 4 and `REPORT_RENDER_SETTINGS` version 2 (2026-10-21)

From rows [5b-1302, 5b-1303, 5b-1304, 5b-1305 and 5b-1307](../fragment-issues/section-5b-rows-401-425.md). On
2026-10-21, after [Group IF](group-if.md) signed the cinematic camera, the owner made real interior views on
Revit 2020's sample house and found four faults in the camera and one in the render report. Fixed the same
day, in the cloud, with no Revit:

| What | Where | What changed |
|---|---|---|
| `FRAME_ELEMENTS_IN_CAMERA` v4 | [`frame-elements-in-camera`](../../brain/fragments/frame-elements-in-camera/fragment.yaml) | MODIFY. **Interior** (5b-1303): the eye stands 600 mm in from the ROOM's own wall, not its box, and steps further in until nothing solid is within 150 mm - or refuses. **Close range** (5b-1304): a window too small for the elements is centred on their middle, and says so. **Phase** (5b-1307): a new view takes the active view's phase and the model's `Show Complete` filter. **Far clip and window** (5b-1302): say when they could not do their job |
| `REPORT_RENDER_SETTINGS` v2 | [`report-render-settings`](../../brain/fragments/report-render-settings/fragment.yaml) | READ. The sun's time is read with the PC's own time zone (`ToLocalTime`), so it is the time the Sun Settings dialog shows (5b-1305) |

**Not here, by agreement:** `SET_SUN_SETTINGS`, `APPLY_VISUAL_PRESET` and `REPORT_VISUAL_PRESET` carry the
same time-zone fault and are being fixed in the session on the owner's PC, under a row and group of their
own.

**What is done and what is not.** Both cards validate (`python brain/heron_fragment.py`) and the library
compiles for 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-10-21). Every Revit member version 4 adds
is one a PROVEN card already calls - `IsPointInRoom`, `IsPointInSpace` (`FILTER_ELEMENTS_IN_ROOM`), the solid
clash test (`CHECK_EQUIPMENT_CLEARANCE`), `VIEW_PHASE` and `VIEW_PHASE_FILTER` (`SET_VIEW_PHASE`) - and
`Document.GetRoomAtPoint`. **Nothing here has run in Revit.** Both cards are DRAFT, and D-117 refuses a kept
change from the camera until these rows pass and the owner signs.

**The model.** `Cinematic ID Test.rvt` in `%TEMP%\heron-work\cinematic-id` - Group IF's scratch copy of Revit
2020's `rac_basic_sample_project.rvt` - never a working model; and `Materials IA Test.rvt` for IM8. Run every
row on **Revit 2020**, on the owner's laptop (its time zone is not Boston's - that is the point of IM7). Every
row's numbers come from `validate frame-elements-in-camera --write`, rolled back, and every picture from
`validate export-view-image --allow-publish --write --setup ... --setup frame-elements-in-camera`, rolled
back, as Group IF ran them; the Raytracer pictures (IM2, IM5) need a view that stays, so they are made by
`try-once` (D-119) with the owner pressing **Keep**, rendered by hand from the Rendering dialog, and the view
deleted after. **LOOK AT EVERY PICTURE.**

| # | Run | Look for |
|---|---|---|
| **IM0** | *Negatives FIRST, rolled back.* Room `Kitchen & Dining 101` selected, shot `interior`, eyeHeightMm `20000`, viewName an EXISTING perspective view (Group IF's kept `Heron IF12 Eye Level`); then shot `Spielberg` | The first refused *"never enters the room ... lower than the eye"*, NOTHING WAS CHANGED - that view's camera, crop and phase as before; the second refused *"is not a shot"*. `madeNew` false both times |
| **IM1** | *The black render's own case (5b-1303).* Room `Kitchen & Dining 101`, shot `interior`, side `right` | The finding *"Interior: the eye stands ... mm in from 'Kitchen & Dining 101''s own boundary on the right side, which is ... mm inside the elements' box there"*; readBack's eye x well inside the wall line x 6860 mm - **not** 6960. Say the numbers |
| **IM2** | *The same, kept and rendered.* IM1's call by `try-once`, Keep; the view rendered with the Autodesk Raytracer (Rendering dialog, Draft) | **The render is NOT black:** the room seen from its east end. Then delete the view (one Ctrl+Z, or by hand) |
| **IM3** | *Walls instead of the room.* The room's bounding walls selected, shot `interior`, side `right` | The same eye as IM1, the finding adding *"the room is the one at the elements' middle"* |
| **IM4** | *Group IF's IF9 again* - room `Kitchen & Dining 101`, shot `interior`, no side | Stood in from the west end as IF9 did; the finding names how far from the room's wall. **Picture:** one-point down the room, as IF9's |
| **IM5** | *Close range (5b-1304).* The 10 dining chairs and the tables, shot `cinematic`, side `north`, distanceMm `2500` | The finding *"... the elements are wider ... than a 28 mm lens takes in, so the picture is centred on their middle ..."*; readBack's window Y no longer -36 to -19 mm - say what it reads. **Picture:** chair backs and the table top in the frame, not the legs alone |
| **IM6** | *Phase (5b-1307).* Any shot making a NEW view, the active view a plan of `Level 1` | The finding *"The new view: phase '...' (the active view's, ...); phase filter 'Show Complete' ..."*; readBack ends with that phase and filter; the view's Properties agree. **Picture:** existing-phase walls in their own colours, not grey. Then re-aim the same view with another side: no "The new view:" finding |
| **IM7** | *The sun's time (5b-1305).* `report-render-settings` on a 3D view whose Sun Settings dialog shows a Still sun - open the dialog first and note its time; then a schedule (the negative) | Sun Settings reads the dialog's time to the minute - **not** 8 hours early. The schedule: `sectionsRead` 0, each section said in words |
| **IM8** | *Nothing else moved.* On `Materials IA Test`, Group IF's IF1 call (the two columns, south-east, 30, 10) and on `Cinematic ID Test` IF14's (the 24 walls, cinematic, side left) | IF1's eye **5572,-4072,5325 mm** and window X -7 to 10, Y -13 to 12 mm, `framed` 2 of 2. IF14: level, eye 1200 mm above `Level 1`, 28 mm, thirds-left kept, *"picture 2.39:1"* - say its eye and window. Neither carries a new finding but the phase one. **Picture:** IF14's letterbox as before |

**Then sign** `frame-elements-in-camera` version 4 on IM2's kept run with IM0's first negative, and
`report-render-settings` version 2 on IM7 - after IM1 to IM8 have passed, every picture looked at. A row that
fails reopens its card; the 150 mm, the 600 mm and the shots' values change on the owner's word, never to make
a row pass.
