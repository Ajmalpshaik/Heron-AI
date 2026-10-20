# Needs checking — Group HX

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HX - wall finishes by room: `CREATE_FROM_ROOM_BOUNDARIES` version 2 (2026-10-19)

From [row 5b-1255](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-19 a comparison of Revit
jobs Heron could not yet do found wall finishes by room missing: floors and ceilings could be built from a
room's own boundary, and a room's walls could not be lined with plaster, tile or cladding.

**What changed.** [`create-from-room-boundaries`](../../brain/fragments/create-from-room-boundaries/fragment.yaml)
(FRG-ELE-051), version 2 - widened, not duplicated, on the owner's word the same day. The type still
decides what is built, and a **wall type** now lines the room's walls: a thin wall of that type on the
INSIDE face of every wall that bounds the room, from the room's floor plus `baseOffset` (left out, 0) up to
`heightAboveLevel` above the room's floor. Neighbouring finishes meet at the corners. Across every door,
window, wall opening and embedded curtain wall in the wall behind, the finish is built only above and
below it, so a finish is several walls. Every finish is NOT room bounding and has its exterior side facing
the room; `joinToHost` (left out, false) joins each to the wall behind. A curtain wall, a separation line,
a wall in a linked model, a column, and an edge neither straight nor an arc are skipped and counted by
reason in `skippedSummary`. `rooms` (left out, all), for every type, takes numbers or names.

**What is tested and not run.** The fragment compiles for Revit 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-19), and every Revit member it calls is on all eight
(`tools/api-surface`); `Curve.Intersect` is not used - it is gone at 2027 - so where two finishes meet is
worked out in the fragment, and that plan geometry and the split round doors and windows were checked
outside Revit in a scratch copy in Python the same day. **Nothing here has run in Revit.**

**UNTIL THIS GROUP IS RUN AND SIGNED, THE TOOL IS DRAFT - FLOORS AND CEILINGS BY ROOM INCLUDED.** Version
1's proof no longer describes the code (D-30), so a change kept from it is refused
([D-117](../decisions/D-117.md)) - from a chat, and from `--write --apply`, which includes step 10 of the
`building-shell` method. What still runs: the tool's one `try-once` ([D-119](../decisions/D-119.md)), and
any rolled-back run.

**Needs.** The add-in as it is - nothing in `revit/` changed, so no rebuild. Changes on. Git Bash from the
Heron folder. **A SCRATCH PROJECT, never a working model**: *Heron Wall Finish.rvt*, from the default metric
template, **Revit 2020** first, SAVED before the rig and after it. **Order: HX0, then HX1 to HX3, then the
rest.** `validate` takes its value options BEFORE the fragment's name (Group HM's lesson).

| # | Run | Look for |
|---|---|---|
| **HX0** | *The rig, KEPT, built with tools already PROVEN or by hand.* On Level 1, **room A**: four *Generic - 200mm* walls on the centrelines 0,0 - 6000,0 - 6000,4000 - 0,4000, 3000 high (`CREATE_WALL`); a door *M_Single-Flush: 0915 x 2134mm* in the south wall at x = 1500 and a window *M_Fixed: 0915 x 1220mm* in the north wall at x = 3000, both by hand; a room at 3000,2000 (`PLACE_ROOM_AT_POINT`). **Room B**, 10000 along: curtain walls (*Curtain Wall: Curtain Wall 1*) 10000,4000 - 16000,4000 - 16000,0, and room separation lines 16000,0 - 10000,0 - 10000,4000 (`CREATE_ROOM_SEPARATION_LINES`); a room at 13000,2000. **The finish type**: `DUPLICATE_TYPE` of *Generic - 200mm* to *Finish - Plaster 15mm*, its one layer set to 15 mm with `SET_COMPOUND_LAYER_WIDTH`. Save | Both rooms placed and enclosed - `SELECT_UNENCLOSED_ROOMS` returns none; room A's Area in Properties **22.04 m²** (5800 x 3800, the walls' inside faces). Note room A's and room B's Numbers for HX5. If the door or window family is not loaded, name the one used everywhere below |
| **HX1** | *The positive, rolled back.* `python tools/batch-prove.py tools/jobs/create-from-room-boundaries-wall-finish-2026-10-19.yaml --dry-run`, then without `--dry-run` | Positive: `finishWalls` above zero - **9** with these families (south: left of the door, a strip over its head, right of it; north: left of the window, under its sill, over its head, right of it; one each east and west); `finishSummary` *"<A's number> <name>: 9 finish walls, a gap at 1 door or opening to the floor, built round 1 window or opening"*; `skippedEdges` 0. Rolled back |
| **HX2** | *The D-30 negative - the same run's negative half, on room B* | `finishWalls` 0; `skippedEdges` **4**; `skippedSummary` *"a curtain wall: 2; a room separation line: 2"*; `finishSummary` *"<B's number> <name>: 0 finish walls, 4 edges not lined"*. **FAIL:** any finish on glass or on a line on the floor |
| **HX3** | *KEPT once, and measured.* Save, select room A, then `python mcp/client/heron_bridge_client.py try-once --in "Heron Wall Finish" create-from-room-boundaries --set "hostType=Basic Wall: Finish - Plaster 15mm" --set heightAboveLevel=2400 < /dev/null`; look, then press **Keep** | `KEPT`. Read back with PROVEN readers: `FILTER_ELEMENTS_BY_CATEGORY` Walls then `COUNT_ELEMENTS` - 9 more walls than before, every one *Finish - Plaster 15mm*; `READ_ELEMENT_PARAMETERS` on them - **Room Bounding No** on all nine, Base Offset 0 on the six that start at the floor; room A's **Area still 22.04 m²** (`REPORT_ROOM_SPACE_DATA`). In plan at 1200 the finish's outer face is on each wall's inside face and the four corners meet with no gap and no overlap; in the south elevation the door is clear to the floor with plaster above its head, and in the north one plaster below the sill and above the head. ONE undo entry |
| **HX4** | *Signing, after HX1 to HX3.* `python brain/heron_validate.py draft create-from-room-boundaries --from brain/proof-drafts/runs/create-from-room-boundaries.json`, read it against HX1 to HX3, then `accept --by "Ajmal PS"` | The proof block written - the positive HX3's kept run (D-120), the negative HX2's. **`heron-status: PROVEN` is a separate edit, the owner's**; until it is made, D-117 still refuses the tool - floors and ceilings included |
| **HX5** | *`rooms`, rolled back, with both rooms selected.* `python mcp/client/heron_bridge_client.py validate --in "Heron Wall Finish" --write --setup select-in-region --setup set-selection --set minMm=-1000,-1000,-1000 --set maxMm=17000,5000,4000 --set exact=false --set categories=Rooms --set "hostType=Basic Wall: Finish - Plaster 15mm" --set heightAboveLevel=2400 --set "rooms=<A's number>" --negative-set minMm=-1000,-1000,-1000 --negative-set maxMm=17000,5000,4000 --negative-set exact=false --negative-set categories=Rooms --negative-set "hostType=Basic Wall: Finish - Plaster 15mm" --negative-set heightAboveLevel=2400 --negative-set rooms=999 create-from-room-boundaries < /dev/null` | Positive: only room A in `finishSummary`, and `skippedSummary` ending *"rooms given but not named in rooms: 1"*. Negative: `finishWalls` 0 and `refused` *"No room or space given is numbered or named '999' - nothing was built for it."* Both rolled back |
| **HX6** | *The refusals, rolled back, room A selected.* The same `validate` with HX1's room A box (`--setup select-in-region --setup set-selection --set minMm=-1000,-1000,-1000 --set maxMm=7000,5000,4000 --set exact=false --set categories=Rooms`) and `--set "hostType=Basic Wall: Finish - Plaster 15mm" --set heightAboveLevel=100 --set baseOffset=100`; then again with `--set "hostType=Curtain Wall: Curtain Wall 1" --set heightAboveLevel=2400` - each with a `--negative-set` of the same values | First: `refused` *"The finish would have no height: its top, 100 mm above the room's floor, is not above where it starts, 100 mm..."*. Second: *"'Curtain Wall 1' is a curtain wall type, which cannot line a room..."*. Nothing built either time |
| **HX7** | *Floors and ceilings - version 1's path, re-proved.* Room A selected, rolled back: `--set "hostType=Floor: Generic 150mm" --set heightAboveLevel=0`; on **Revit 2024** a copy of the rig with `--set "hostType=Compound Ceiling: 600 x 600mm Grid" --set heightAboveLevel=2700`; the negative as version 1's - the four walls of room A selected instead of the room | The floor: `created` 1 entry and nothing in `finishWalls`, through the pre-2022 call on 2020. The ceiling on 2024: `created` 1. On Revit 2020 the ceiling is `refused` *"This Revit has no way to create a ceiling from code - it arrived at 2022..."* - version 1's own words. The walls: `refused` *"Not a room or space: ..."* four times. **FAIL:** any version 1 answer worded differently |
| **HX8** | *After HX4 and the owner's PROVEN edit, in a chat.* *"Plaster the walls of room A from 100 mm to 2400 mm with Finish - Plaster 15mm, joined to the walls"* on a fresh copy of the saved rig | It reaches `CREATE_FROM_ROOM_BOUNDARIES` with `baseOffset` 100 and `joinToHost` true; `notJoined` 0. `READ_ELEMENT_PARAMETERS`: Base Offset **100** on the six pieces that start low; `REPORT_JOINED_ELEMENTS` names the wall behind for each |
| **HX9** | *An arc wall, rolled back.* Add to a copy of the rig a room whose one side is an arc - `CREATE_WALL` with `arc` in the points (Group HM) - and run HX1's positive on it | The arc side gets finish walls that follow the arc - not one straight chord - and meet their straight neighbours at the corners. `skippedSummary` names no arc |
| **HX10** | *A Space, not a Room.* On a copy of the rig, a Space in room A (`PLACE_ROOMS` with spaces), the Space selected, HX1's positive | The same 9 finishes - spaces are lined as rooms are |
| **HX11** | *The other releases.* HX1 and HX2 on Revit 2024 and on 2027 (`batch-prove --release`, D-131) | The same answers. A difference is a finding for this group, not a pass |
| **HX12** | *Second route.* For HX3, a 3D view and the two elevations beside the read-backs | What the answers said: the finish on the face, the gap at the door, the finish round the window, the corners closed |
