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
| **IF2** | *eye-level*, the walls, nothing else | readBack *"level - verticals stay vertical"*, eye 1600 mm above the level most of the walls stand on (named - the owner's word, row 5b-1301), stood 25° round from the front toward the left side, lens about 35 mm. **Picture:** every wall corner upright; the horizon at a standing eye; the front face and a sliver of the left; the building across the width |
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

Run 2026-10-21 on Revit 2020 (session 68704). Rows IF2 to IF11, IF14 and IF15 on `Cinematic ID Test.rvt`, a scratch
copy of Revit 2020's own `rac_basic_sample_project.rvt` in `%TEMP%\heron-work\cinematic-id`: the 24 walls
`select-by-category-name` finds in `FloorPlan: Level 1` (21 on `Level 1`, the rest on `Foundation` and
`Level 1 Living Rm.` - foundation and retaining walls down to -1500 mm among them, which is why the pictures
keep room below the building). IF1 on `Materials IA Test.rvt`. Every row's numbers come from
`validate frame-elements-in-camera --write --keep-chain` and its picture from `validate export-view-image
--allow-publish --write --keep-chain --setup ... --setup frame-elements-in-camera`; both rolled back, so nothing
in either model changed and no selection shows in the pictures. The pictures are in
`%TEMP%\heron-work\cinematic-id\pictures`, every one looked at.

**The first picture failed, and the card was corrected before these rows were run again -
[row 5b-1301](../fragment-issues/section-5b-rows-401-425.md).** IF2's building came out a third as wide as it
should: the window was worked out at the crop box's Z top (the far clip over a thousand, 92.9 mm here) and Revit
draws it 0.1 ft (30.48 mm) in front of the eye. A camera further than the far clip also cut its elements out, and
the eye height stood on `Foundation`, 800 mm below the ground floor. All three were fixed in the code; the owner
chose the third's rule (*"Ground floor"* - the level most of the elements stand on). Every result below is from
the corrected code.

| # | Result | What was seen |
|---|---|---|
| IF0 | PASS | All six refused, NOTHING WAS CHANGED, readBack empty, `framed` 0: *"'Spielberg' is not a shot - say eye-level, low-angle (hero) ... cinematic, or none"*; *"'upstairs' is not a side - say front, back, left, right ..."*; *"'5' is not a lens - a 35 mm-film focal length from 12 to 300"*; *"direction 'north' and side 'left' both say where the camera stands - give one"*; *"a level camera looks dead level, so lookDownDeg 30 cannot be kept with it"*; *"'1.6 m' is not an eye height - millimetres above the elements' base level"*. Also *"'golden' is not a composition"* and *"'wide-ish' is not a picture shape"* |
| IF1 | PASS, with the window corrected | Version 2's two columns: eye **5572,-4072,5325 mm** looking -0.6124,0.6124,-0.5 - version 2's signed eye exactly - `framed` 2 of 2, no shot sentence. The window is now X -7 to 10, Y -13 to 12 mm drawn 30 mm in front of the eye (version 2's X -10 to 13, Y -17 to 16 at the 40 mm reading, times 30.48 / 40.2). `lens` reads *"about 64 mm on 35 mm film - 31.2° across, 43.8° up and down"* - the same angles version 2's numbers meant, now true of the picture: the two columns touch the frame's edges with the 10% margin, where version 2's picture had about a third more room round them |
| IF2 | PASS | *"level - verticals stay vertical"*, *"eye 1600 mm above 'Level 1'"*, stood on the front (south) stepped 25° toward the left, 35 mm. **Picture:** the house across the frame, every corner upright, the horizon at a standing eye, the south face and the west gable end |
| IF3 | PASS | low-angle hero, side left: *"stood on the left side (its face looks west), stepped 30° round toward the front"*, eye 450 mm above `Level 1`, looking up 6°, 24 mm, thirds-left. **Picture:** the glazed west gable and the south face, the house left of centre, verticals leaning in a little, a low eye |
| IF4 | PASS | high-angle: looking down 25°, 35 mm. **Picture:** the roof, the south and west faces and the roof terrace, the house filling the frame |
| IF5 | PASS | aerial: stood south-west, looking down 40°, 35 mm. **Picture:** the whole roof, two faces, the terrace and the site - a drone still |
| IF6 | PASS, with a note | worm's-eye: eye 150 mm above `Level 1`, looking up 25°, 20 mm - *"84° across, 58.8° up and down"*, so **Revit kept a window that wide**. **Picture:** verticals leaning in, the house seen from the ground - but it does not tower: it fills the middle third, because the 24 walls' box takes in the long retaining walls and the foundations, and a worm's-eye fits the box. A worm's-eye of the house alone wants the walls above ground selected |
| IF7 | PASS | three-quarter: stood south-west, level, eye 1600 mm above `Level 1`, 35 mm, thirds-left. **Picture:** the west gable and south face about alike, corners upright, the house on the left third. The far clip was pushed out to 96184 mm to reach the deepest corner |
| IF8 | PASS | front: square to the south face, level, 50 mm, then 85 mm with low-horizon. **Picture:** one-point - the south face's horizontal lines level across the picture; the second from 100 m back (far clip pushed to 123982 mm), the horizon on the bottom third line |
| IF9 | PASS | interior, room `101 Kitchen & Dining` selected: eye 600 mm in from its west end, 1400 mm above `Level 1`, looking east, level, 24 mm; `framed` 0 with *"The camera stands inside the elements, so their boxes cannot be inside the picture"*. **Picture:** one-point down the room - the dining table in front, both glazed side walls, floor and ceiling running to the far end. The room's own walls did NOT hide the view |
| IF10 | PASS | close-up, one lounge chair: looking down 20°, 85 mm, eye named above `Level 1 Living Rm.` - its own level. **Picture:** the chair filling most of the frame, its legs and shell not stretched |
| IF11 | PASS, with a note | side north-west, eyeHeightMm 1600, lensMm 24, levelCamera true: *"stood on the north-west side, azimuth 315°"* - exactly, no step round - level, eye 1600 mm above `Level 1`, 24 mm. **Picture:** verticals upright, but what fills it is the raised deck structure on that side of the site, not the house's north and west faces - an aerial from the same side (looked at) shows the deck between that corner and the house. The camera is where it was asked; the model has something in front |
| IF12 | PASS | The owner saved `Cinematic ID Test` and pressed **Keep** (16:14): `try-once frame-elements-in-camera` with the 24 walls selected, shot eye-level, made view `Heron IF12 Eye Level` - *"KEPT  You were asked in Revit and pressed KEEP after seeing the result"*; `framed` 24 of 24, *"eye 1600 mm above 'Level 1'"*, 35 mm, *"Revit shows Eye Elevation 1600 mm and Target Elevation 1600 mm"*. **Read back:** the kept view exported (`export-view-image`, PNG 1600 px) - the house across the frame at a standing eye, corners upright, in the view's own shaded style. **Negative** (`validate --kept`, rolled back): shot `Spielberg` refused, `framed` 0, `madeNew` false. Drafted to `brain/proof-drafts/frame-elements-in-camera.yaml`; signed by nobody yet |
| IF13 | PASS on the lookup | `heron_lookup` on this PC, 2026-10-21: all six phrases - *"cinematic camera on the left side of this building"*, *"low angle hero shot"*, *"bird's eye view of the building"*, *"two point perspective at eye level"*, *"wide angle camera"*, *"camera height 1.6 m"* - answer `FRAME_ELEMENTS_IN_CAMERA` on the identity route, four of them naming the `visualise-a-view` method. A chat passing each shot and looking at its picture was not run as a chat; the shots' pictures are IF2 to IF6 |
| IF14 | PASS | cinematic, side left: level, *"eye 1200 mm above 'Level 1'"*, 28 mm, thirds-left, *"picture 2.39:1"*, from the west stepped 30° toward the front. **Picture:** a 1600 x 669 letterbox, the glazed west gable and the south face on the left third, verticals upright, the trees running out to the right |
| IF15 | PASS | aspect `16:9`: *"picture 1.78:1"*, 35 mm since no lens was given; picture 1600 x 900. Aspect `1:1`: *"picture 1:1"*, 35 mm; picture 1600 x 1600. `wide-ish` refused naming the shapes, NOTHING WAS CHANGED |

**Revit's Eye Elevation**, asked by IF12, is already answered by IF10: the chair's camera stood 1894 mm above
`Level 1 Living Rm.` (-550 mm) and Revit showed Eye Elevation 1344 mm - **the height above the project's zero,
not above the elements' level**. On `Level 1` (at zero) the two read the same: 1600 and 1600.
