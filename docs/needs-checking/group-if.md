# Needs checking — Group IF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-20 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IF - cinematic camera shots: `FRAME_ELEMENTS_IN_CAMERA` version 3 (2026-10-20)

From [row 5b-1287](../fragment-issues/section-5b-rows-401-425.md). On the night of 2026-10-20, with the framing
camera's version 2 signed ([Group IB](group-ib.md), [row 5b-1281](../fragment-issues/section-5b-rows-401-425.md)),
the owner asked for more than a camera that fits: *"I need this building left side like in a cinematic camera
look ... different styles, camera angles, camera height ... all kind of camera settings"*. Built the same night:

| What | Where | What it does |
|---|---|---|
| `FRAME_ELEMENTS_IN_CAMERA` v3 | [`frame-elements-in-camera`](../../brain/fragments/frame-elements-in-camera/fragment.yaml) | MODIFY - six optional inputs: a named `shot`, the `side` to stand on, `eyeHeightMm`, `lensMm`, `composition`, `levelCamera`; reads back the side, the eye height, the point aimed at, the look angle and the lens |
| `visualise-a-view` step 1 | [`brain/skills/visualise-a-view.yaml`](../../brain/skills/visualise-a-view.yaml) | the method's camera step: which shot for "cinematic", "hero", "dramatic", "aerial", "interior", "detail", and the picture looked at before it is called done |

**The shots, in Revit words** - each is Heron's starting set of values (D-33), in one table on the card:

| Shot | What the camera does in Revit |
|---|---|
| eye-level | stands 25° round from the front, eye 1600 mm above the building's level, looks dead level - every wall corner stays upright (two-point perspective), 35 mm lens |
| low-angle (hero) | eye 450 mm up, looks up 6°, 24 mm wide lens, the building on the left third line - it towers a little, verticals lean in slightly |
| high-angle | looks down 25° through the building's middle, 35 mm |
| aerial (bird's-eye) | the corner, looking down 40°, 35 mm - the whole roof and two faces |
| worm's-eye | eye 150 mm up, looks up 25°, 20 mm - the building towers, verticals lean in hard |
| three-quarter | the corner, eye 1600 mm, level, 35 mm, on the left third - both faces alike, verticals upright |
| front | square to the face, eye 1600 mm, level, 50 mm - one-point, like an elevation in perspective |
| interior | INSIDE the room, 600 mm in from its end wall, 1400 mm up, level, 24 mm - looking down the room's length (one-point) |
| close-up | 30° round, looking down 20°, 85 mm - a unit or a detail with little stretching, 15% margin |

**What is done and what is not.** The card validates (`python brain/heron_fragment.py`) and the library compiles
for 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-10-20). Every Revit member version 3 adds is one
`SET_VIEW_CAMERA` or other PROVEN fragments already call: `LocationCurve`, `Line.Direction`, `Curve.Length`,
`Element.LevelId`, `Level.ProjectElevation`, `VIEWER_EYE_ELEVATION`, `VIEWER_TARGET_ELEVATION`. **The camera
arithmetic was checked offline, with no Revit:** a scratch copy of the version 3 road in Python, run on a made-up
building 40 x 12 x 9 m, square to north and turned 20°, drew what each shot's crop window would hold. Every shot
landed as its row says - the level camera's verticals upright, the low horizon on the bottom third, the thirds on
the third line - and the turned building gave the same distances as the square one, so the plan grid is read
right. **That is arithmetic agreeing with itself, not Revit agreeing with it: nothing here has run in Revit.**
The card is DRAFT, and D-117 refuses a kept change from it until these rows pass and the owner signs.

**Version 2 still runs, untouched.** With none of the six new inputs the code takes version 2's road line for
line; `direction`, `lookDownDeg` and `marginPercent` now read `auto` when left out, which means version 2's own
south-east, 30 and 10. IF1 holds that to version 2's own signed numbers.

**What Revit has not yet been asked, and these rows ask:** whether a perspective crop window can be shifted
well off the eye's line (the level camera's shift lens and the thirds both need it - version 2's windows were a
few millimetres off, and drew right); whether a window as wide as a 20 mm lens (84° across) is kept; whether the
view's Eye Elevation and Target Elevation read the height above the base level; and whether the front Heron
takes - the long face looking nearest south - is the face a modeller calls the front on a real building.

**The model.** A **scratch copy** of a Revit 2020 sample with a whole building in it - the architectural sample
project that ships with Revit 2020 (`rac_basic_sample_project.rvt`; any sample with a whole building
when that one is not on the laptop - named in the results) - saved to
`%TEMP%\heron-work\cinematic-id` as `Cinematic ID Test.rvt`, never a working model; and Group IA's
`Materials IA Test.rvt` for IF1, the model version 2 was signed on. Before the rows, select **the building's
exterior walls** on the ground floor plan (`select-by-category-name` Walls, then `set-selection`) and note the
count; IF9 selects one room, IF10 one piece of furniture or equipment. An empty scratch folder
`%TEMP%\heron-work\cinematic-id\pictures` takes the pictures.
Run every row on **Revit 2020**. **Every picture is exported inside a rolled-back run** - the camera made as a
setup step, the picture written, the camera rolled back:

```
python mcp/client/heron_bridge_client.py validate export-view-image --allow-publish --write \
    --setup frame-elements-in-camera --set "viewName=Heron IF2" --set shot=eye-level \
    --set "views=Heron IF2" --set folder=%TEMP%\heron-work\cinematic-id\pictures --set filePrefix=id2 \
    --set fileType=PNG
```

`EXPORT_VIEW_IMAGE` is a PUBLISH, so the owner's Publish switch is on for these runs (D-106), and off after.
Each row's numbers come from `frame-elements-in-camera` run alone the same way (`validate
frame-elements-in-camera --write`, rolled back) with the same values. **LOOK AT EVERY PICTURE** against the
row's "Look for" before the row is called a pass - version 1 was signed on numbers and drew a dot
([row 5b-1281](../fragment-issues/section-5b-rows-401-425.md)).

| # | Run | Look for |
|---|---|---|
| **IF0** | *Negatives FIRST, rolled back.* The walls selected: shot `Spielberg`; side `upstairs`; lensMm `5`; side `left` with direction `north`; levelCamera `true` with lookDownDeg `30`; eyeHeightMm `1.6 m` | Each refused, naming what it takes - the shots, the sides, 12 to 300, *"both say where the camera stands"*, *"a level camera looks dead level"*, millimetres - `madeNew` false, no new view in the Project Browser |
| **IF1** | *Version 2 unchanged.* On `Materials IA Test`, the same two columns version 2 was signed on selected; viewName a new name, direction south-east, lookDownDeg 30, marginPercent 10, nothing else | readBack equal to version 2's signed record: eye **5572,-4072,5325 mm** looking -0.6124,0.6124,-0.5, window X -10 to 13, Y -17 to 16 mm; no shot sentence; `framed` 2. `lens` filled in - say what it reads |
| **IF2** | *eye-level*, the walls, nothing else | readBack *"level - verticals stay vertical"*, eye 1600 mm above the walls' lowest level (named), stood 25° round from the front toward the left side, lens about 35 mm. **Picture:** every wall corner upright; the horizon at a standing eye; the front face and a sliver of the left; the building across the width |
| **IF3** | *The owner's words: low-angle, side left.* The walls | eye 450 mm, looking up 6°, lens about 24 mm, composition thirds-left, stood off the left face stepped 30° toward the front. **Picture:** the LEFT face of the building and its front, the building's middle on the left third line, a low horizon and a lot of sky, verticals leaning in a little |
| **IF4** | *high-angle* | looking down 25°, lens about 35. **Picture:** the roof seen, two faces, the building filling the frame |
| **IF5** | *aerial* | looking down 40°, stood on the front-left corner. **Picture:** the whole roof and two faces, like a drone still |
| **IF6** | *worm's-eye* | eye 150 mm, looking up 25°, lens about 20, angle across about 84°. **Picture:** the building towering, verticals leaning in hard. **Say** if Revit kept a window that wide |
| **IF7** | *three-quarter* | level, eye 1600, stood on the front-left corner, thirds-left. **Picture:** both faces about alike, corners upright, the building on the left third |
| **IF8** | *front*, then *front* with lensMm 85 and composition low-horizon | Square to the front, level, lens about 50, then 85. **Picture:** one-point - the front's horizontal lines level in the picture; the second with the horizon on the bottom third line and the building smaller, further away |
| **IF9** | *interior*, one room selected | eye inside the room, 600 mm in from its left end, 1400 mm up, level, lens about 24; `framed` 0 with the finding that says why. **Picture:** the far wall in the middle, both side walls, floor and ceiling running to it (one-point). **Say** whether the room's own walls hide the view - a camera inside a room in a model with walls is the case |
| **IF10** | *close-up*, one piece of furniture or equipment | looking down 20°, lens about 85, margin 15. **Picture:** the item filling most of the frame, corners not stretched |
| **IF11** | *No shot - the inputs alone.* The walls: side north-west, eyeHeightMm 1600, lensMm 24, levelCamera true | Stood exactly north-west (no step round), level, eye 1600, lens about 24. **Picture:** the north and west faces, verticals upright |
| **IF12** | *Eye Elevation.* IF2's view kept by `try-once` (D-119) - the walls selected, shot eye-level - the owner presses Keep | The view's Properties: Eye Elevation and Target Elevation beside readBack's eye height above the base level - **say** whether Revit's Eye Elevation is above that level or above the project's origin |
| **IF13** | *The owner's request through a chat, on his PC (model and re-ranker).* In a chat: *"cinematic camera on the left side of this building"*, then *"low angle hero shot"*, *"bird's eye view of the building"*, *"two point perspective at eye level"*, *"wide angle camera"*, *"camera height 1.6 m"* | `heron_lookup` names `FRAME_ELEMENTS_IN_CAMERA` for each; the chat passes the method's shot for each and the picture is looked at |

| **IF14** | *cinematic* - the owner's own word (row 5b-1287) - side left, the walls | level, eye 1200 mm, lens about 28, composition thirds-left, readBack *"picture 2.39:1"*. **Picture:** a wide letterbox picture, the walls on the left third, verticals upright, from the west side stepped 30° |
| **IF15** | *The picture's shape alone.* The walls: aspect `16:9`, then `1:1`, then `wide-ish` | readBack *"picture 1.78:1"*, then *"1:1"* - each with a 35 mm lens since no lens was given; the third refused naming the shapes it takes, NOTHING WAS CHANGED. **Picture:** the two shapes |

**Then sign** `frame-elements-in-camera` - after IF14's cinematic picture has been looked at too - on IF12's kept run (the positive) with IF0's first negative - after IF1
to IF11 have passed, every picture looked at. A row that fails reopens the card; the shot's starting values
change on the owner's word, never to make a row pass.

### Results

| # | Result | What was seen |
|---|---|---|
| IF0 | NOT RUN - built in a session with no Revit | |
| IF1 | NOT RUN | |
| IF2 | NOT RUN | |
| IF3 | NOT RUN | |
| IF4 | NOT RUN | |
| IF5 | NOT RUN | |
| IF6 | NOT RUN | |
| IF7 | NOT RUN | |
| IF8 | NOT RUN | |
| IF9 | NOT RUN | |
| IF10 | NOT RUN | |
| IF11 | NOT RUN | |
| IF12 | NOT RUN | |
| IF13 | NOT RUN | |
