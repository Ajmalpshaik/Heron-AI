# Needs checking — Group AO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AO - `tag-rooms`, `place-hosted-family` version 2 and `set-room-limits`: room tags on a plan's rooms, doors into the wall found at each point, and room tops tied to a level (2026-09-24)

**Three fragments from one sitting on 2026-09-24, in Project1 on Revit 2024, and all three are DRAFT.**
Each compiles on every release from 2020 to 2027. The first two have run in a chat on a working model,
and **a chat run is not a proof**: no negative case was run and no fingerprint was taken
([D-30](../DECISIONS.md)), so no row below is closed by it. The third has no recorded run.

### `tag-rooms` - a room tag on each room of a plan, from rooms collected over the whole model

**A test copy of Project1, Revit 2024, floor plan `1 - Mech` (Level 1).** One new fragment,
[`tag-rooms`](../../brain/fragments/tag-rooms/fragment.yaml) - TAG_ROOMS, FRG-VIEW-108 - written on
2026-09-24 when the owner asked in a chat to "tag all the rooms in the active view" and nothing in
Heron could place a room tag: TAG_ELEMENTS makes the tag every other category takes, and a room does
not take it; CENTER_ROOM_TAGS, STACK_TAGS and ARRANGE_TAGS only move room tags that already exist.

**It has run once, in a chat, the same day:** Project1 (Revit 2024, session 32116), plan `1 - Mech` -
15 room tags placed, 0 refused, 0 read back on the wrong room - and `SELECT_BY_CATEGORY_NAME` with
`categoryName=Room Tags` and `inViewOnly=1 - Mech` then found 15. That is Heron's answer checked by
another Heron read, not Revit's own count, so AO2's second route is still owed, and so is AO2's
re-run.

The rooms are handed in by FILTER_ELEMENTS_BY_CATEGORY, which collects from the whole model - never by
a view-scoped select, which on 2026-09-19 found 0 of a model's 7 rooms in every view. `revit_change`
finds TAG_ROOMS by name from the fragment files at once; `heron_lookup` and `heron_resolve` answer
from the knowledge store on the PC and name it only once that store has been rebuilt from a tree that
holds it (`python tools/check-routing.py` rebuilds it when the two disagree). Every row runs on the
test copy.

| # | Check | Expected |
|---|---|---|
| **AO1** | The proof ([D-30](../DECISIONS.md)): with `HERON_CLIENT_ID=ajmal-pc` set and the test copy in front, `python tools/batch-prove.py tools/jobs/tag-rooms-project1-2026-09-24.yaml --dry-run`, then the same without `--dry-run`. Then read the draft, sign it with `python brain/heron_validate.py accept tag-rooms --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive: `tagged` equal to the Level 1 rooms that are placed, enclosed and not already tagged in `1 - Mech`, and every other Level 1 room named in `alreadyTaggedHere`, `unplaced`, `notEnclosed` or `refused`. The negative: the Level 1 ducts - `tagged` 0, `newTags` empty, every duct in `notRooms`. `NEG NOT EMPTY` means it tagged something that was not a room |
| **AO2** | The chat, KEPT, with Changes on: `revit_read` FILTER_ELEMENTS_BY_CATEGORY with `category=Rooms` and `levelId=Level 1`, then `revit_change` TAG_ROOMS with `view=1 - Mech` and `tagTypeId=Room Tag With Area`, and `expect_from` set to `filter-elements-by-category where category=Rooms; levelId=Level 1`. Then the same two calls again | The first TAG_ROOMS call: one tag per tagged room, at the point the room was placed, showing its name, number and area. Select one: Properties names M_Room Tag, Room Tag With Area. Right-click it, Select All Instances, Visible in View - Revit's own count is `tagged`, the second route. The second call: `tagged` 0 and every one of them in `alreadyTaggedHere` - nothing stacked. If `findings` says Room Tags are off in `1 - Mech`, the tags are in the model and not shown there: do the second route in a plan that shows Room Tags. One Ctrl+Z takes back each kept call |
| **AO3** | The refusals, before anything is placed: `tagTypeId` naming a duct tag type; `tagTypeId=none`; `view` naming a 3D view | Each refused with one sentence - "... is a Duct Tags type, not a Room Tags type", "No room tag type was named, and one is never guessed", "... is not a plan of one level" - and the model unchanged, with nothing in the undo list to take back |
| **AO4** | Two beliefs nothing has tested yet: the call on a DEPENDENT view of a plan whose primary view already carries room tags, and on a plan whose view template turns Room Tags off | Dependent: the rooms tagged in the primary land in `alreadyTaggedHere`, because Revit shares a primary view's annotation with its dependents and the code relies on that. Hidden: the tags placed and counted, and `findings` saying they are in the model and not shown in that plan |
| **AO5** | Whether the tags TAG_ELEMENTS put on rooms are room tags at all. On 2026-09-23 it answered "tagged 5, refused 0" on rooms in `1 - Mech` of the other Project1 - the one open in Revit session 42080 that day, not the one above - and `revit_annotation`'s tag count rose from 0 to 5, a count that reads only IndependentTags ([row 5b-216](../FRAGMENT-ISSUES.md)). On a test copy of that model: select one of the five and read its category and type in Properties; `revit_read` SELECT_BY_CATEGORY_NAME with `categoryName=Room Tags` and `inViewOnly=1 - Mech`; then `revit_change` CENTER_ROOM_TAGS on the five | Read from the code, not yet seen: CENTER_ROOM_TAGS keeps only an element of Revit's room tag class and puts anything else in `notRoomTags`, so five IndependentTags come back in `notRoomTags` with `centred` 0, and "tag the rooms" belongs to TAG_ROOMS. If CENTER_ROOM_TAGS moves them instead, the code has been misread: correct row 5b-216 and the routing comments in `tag-elements`, `center-room-tags` and `tag-rooms`. One Ctrl+Z takes back a kept call |

### `place-hosted-family` version 2 - a door into the wall found at each point, turned to swing into the room named for it

[`place-hosted-family`](../../brain/fragments/place-hosted-family/fragment.yaml) - PLACE_HOSTED_FAMILY,
FRG-ELE-065 - rewritten on 2026-09-24. Version 1 took its host as one element, which nobody could type
by name, and was never run; on 2026-09-22 a door sent through PLACE_FAMILY_INSTANCES instead came out
free-standing ([row 5b-211](../FRAGMENT-ISSUES.md)). Version 2 finds the wall at each point, reads the
host back, turns each door to swing into the room named for it, hangs it on the jamb named for it, and
sets its To Room on its own, because a facing flip does not move it ([row 5b-212](../FRAGMENT-ISSUES.md)).
The hinge is read off the door's plan swing arc, whose centre is taken as the hinge, in a floor plan
of the level. A door already within 50 mm of a point is corrected rather than doubled.

**It has run in a chat, KEPT, on a working model:** Project1 (Revit 2024), M_Single-Flush 0915 x
2134mm - 14 doors placed, and the walls' Volume went from 299.36 to 293.893 m³. That drop is 14 x
0.3905 m³, and 0.3905 m³ is 0.915 x 2.134 x the 0.2 m wall, so every door cut its wall. A read probe,
deleted afterwards, found each door's plan swing arc on its FacingOrientation side. A re-run over four
doors came back alreadyThere 4, placed 0, toRoomSwapped 4, and in the end all 14 swing into their
office with To Room Office NN and From Room Corridor 15.

**Then the hinge, the same day, at the owner's request** - doors as near the side wall as possible,
a 100 mm gap, swinging against the wall. Each of the 14 was first moved with MOVE_ELEMENTS_TO_POINT
(PROVEN), after a SELECT_IN_REGION that found exactly one door, and then one PLACE_HOSTED_FAMILY run
came back alreadyThere 14, placed 0, flipped 0, handFlipped 5, toRoomSwapped 0, with `wrongRoom`,
`wrongHinge` and `toRoomWrong` empty. A read probe, deleted afterwards, found every hinge at the jamb
100 mm from its side wall's face - Office 01's door at (6200, 23657.5), its hinge at (6100, 23200) and
the wall face at 23100. All 14 still swing into their office with the same To and From Room, the
walls' Volume stayed at 293.892692 m³, and Revit raised no warning.

**What those runs did not cover is every row below.** A door is judged by its wall, never by
`placed`: read the host walls' Volume before and after every run.

| # | Check | Expected |
|---|---|---|
| **AO6** | The proof ([D-30](../DECISIONS.md)), on a TEST model with straight walls between placed, enclosed rooms - a test copy of Project1 will do. A job file for `place-hosted-family` in the shape of `tools/jobs/tag-rooms-project1-2026-09-24.yaml` - `write: true`, `expect: placed`, no setup - with `set:` naming `symbol` (the door type), `level`, `points` on the location lines of straight walls, `toRoomAt` inside the rooms they should swing toward and `hingeAt` on the side each hinge belongs, in millimetres, "x,y,z", semicolons between, one of each per door; and `negative-set:` the same with every door point moved into the middle of its room, off every wall. With `HERON_CLIENT_ID=ajmal-pc` set, `--dry-run`, then the run. Then read the draft, sign it with `python brain/heron_validate.py accept place-hosted-family --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive: `placed` one id per point, and `noWall`, `twoWalls` and `wrongHinge` empty. The negative: `placed` empty and every point in `noWall`. `NEG NOT EMPTY` means it put a door where no wall is - the 2026-09-22 failure again. Rollback has failed before on this PC, so look at the plan afterwards, and one Ctrl+Z takes back any door that survived. The other negatives in [`tests/cases.yaml`](../../brain/fragments/place-hosted-family/tests/cases.yaml) - `twoWalls`, `wrongRoom`, a re-run into `alreadyThere`, a count mismatch in `toRoomAt` and in `hingeAt`, and a hinge point level with the door's centre - are worth running in the same sitting, and its second route: Architecture > Door by hand into the same wall, both compared |
| **AO7** | Windows, which nothing has tried: the same call with a window type - M_Fixed, or whatever the test model has loaded - at points on an exterior wall, each `toRoomAt` inside the room behind it and a `hingeAt` point beside it | Each window in its wall - the wall's Volume down by width x height x thickness per window - and its To Room the room named. Then look at which face of the wall each window's outside - its frame and sill - sits on. The swing rule was measured on a door, and a window has no swing: if turning it to face the room named puts its outside indoors, windows need a rule of their own, recorded in [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) section 5b, and until then windows are placed by hand. A window that draws no swing arc lands in `wrongHinge` - "no swing arc was found" - which says the hinge rule does not reach it, not that the window is wrong |
| **AO8** | Door families other than M_Single-Flush, where "the swing lies on the facing side" is a belief: a double door, a door whose family was built with its swing on the other side, and a sliding door, each placed with `toRoomAt` and `hingeAt` | Each swing arc drawn in the room named, and `wrongRoom` empty. **A swing on the far side while `wrongRoom` is empty is the failure worth finding**: the swing side is read from the facing, never from the drawn arc, so it would call a wrong door right. A sliding door has no swing, and its To Room is the one thing to check. The double and the sliding door land in `wrongHinge`, which is AO10's to judge |
| **AO9** | Curved walls, skipped by design: a door point on the location line of a curved wall | That point in `noWall` and nothing placed - version 2 collects only walls whose location line is straight. Its sentence then says no wall passes through the point, which is not what the modeller sees on screen: until curved walls are handled, a door in a curved wall is placed by hand with Architecture > Door |
| **AO10** | The hinge, read off the door's plan swing arc - the centre of the first arc its geometry draws in a floor plan of the level is taken as the hinge, measured only on M_Single-Flush. On the test model, with `hingeAt` given for every door: a double-leaf type - M_Double-Flush, or whichever pair of leaves is loaded; a door that draws no arc in plan - a sliding door, or one whose plan swing a family parameter turns off; and a single door on a level that has no floor plan | Double leaves: every door in `wrongHinge` - "it draws two swing arcs a jamb apart" - with `handFlipped` 0, and each still placed with its swing and To Room set. No arc, and no floor plan: in `wrongHinge` - "no swing arc was found for it in any floor plan" - and otherwise placed and set. **A FAIL is worth the same:** a single door left hinged at the jamb NOT named while `wrongHinge` is empty means a family whose arc is not centred on its hinge, which the rule cannot see - so read each hinge in plan, where the swing arc is centred on the hinged jamb |

### `set-room-limits` - where rooms and spaces stop at the top: a level and an offset, set together

[`set-room-limits`](../../brain/fragments/set-room-limits/fragment.yaml) - SET_ROOM_LIMITS, FRG-GEO-037 -
written on 2026-09-24 when the owner chose to take every room's top to Upper Limit Level 2 with a Limit
Offset of -300. WRITE_ELEMENT_PARAMETERS refuses a parameter that stores an element id, by design, so
nothing in Heron could tie a room's top to a level. It sets the level and the offset together, refuses a
top at or below the room's own floor, takes rooms and MEP spaces alike, and reads every one back.

| # | Check | Expected |
|---|---|---|
| **AO11** | The proof ([D-30](../DECISIONS.md)), on a test copy of Project1: a job file in the shape of `tools/jobs/tag-rooms-project1-2026-09-24.yaml` - `write: true`, `expect: changed`, `keep-chain: true`, `setup: filter-elements-by-category` with `setup-set` `category=Rooms` and `levelId=Level 1`, `set` `upperLevel=Level 2` and `limitOffset=-300`, and for the negative `negative-setup-set` `category=Walls` with the same `set`. With `HERON_CLIENT_ID=ajmal-pc` set, `--dry-run`, then the run. Then read the draft, sign it with `python brain/heron_validate.py accept set-room-limits --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive: `changed` equal to the Level 1 rooms not already at Level 2 and -300, and the rest in `alreadyRight`. Check the values with SELECT_BY_PARAMETER_VALUE on Upper Limit and SELECT_BY_NUMERIC_PARAMETER on Limit Offset, never by the fragment's own count, and one room in Properties - the second route. The negative: the Level 1 walls - `changed` 0 and every wall in `notSpatial`. `NEG NOT EMPTY` means it wrote to something that is not a room. Worth running in the same sitting, from [`tests/cases.yaml`](../../brain/fragments/set-room-limits/tests/cases.yaml): Level 1 and -300, where every room lands in `topBelowFloor` and nothing is written; and MEP spaces, which nothing has tried |
