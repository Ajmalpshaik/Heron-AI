# Needs checking — Group IF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IF - cinematic camera shots: a named shot, a side, a camera height, a lens, a picture shape and a composition (2026-10-21)

From [row 5b-1287](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-20 the owner asked: *"If I say OK I
need this building left side like in a cinematic camera look ... cinematic style or the different styles,
camera angles, camera height ... all kind of camera settings"*. `FRAME_ELEMENTS_IN_CAMERA` (PROVEN at version 2,
[Group IB](group-ib.md)) was widened, not duplicated, to version 3: six optional inputs - `shot`, `side`,
`eyeHeightMm`, `lensMm`, `aspect`, `composition` - each absent reading as version 2 did. The
[`visualise-a-view`](../../brain/skills/visualise-a-view.yaml) method's step 1 says which shot a modeller's words
pick.

| Shot | Turn from the side | Height or look-down | Lens | Picture | Composition | Camera |
|---|---|---|---|---|---|---|
| `eye-level` (two-point) | 0 | 1600 mm | 35 | 3:2 | centre | level |
| `three-quarter` (corner) | 45 | 1600 mm | 35 | 3:2 | centre | level |
| `front` (one-point) | 0 | half the elements' height | 35 | 3:2 | centre | level |
| `hero` (low angle) | 30 | 600 mm | 24 | 16:9 | thirds | aimed at the centre |
| `worm's-eye` | 20 | 150 mm | 20 | 3:2 | centre | aimed at the centre |
| `high-angle` | 30 | 35 degrees down | 35 | 3:2 | centre | aimed |
| `bird's-eye` (aerial) | 45 | 55 degrees down | 35 | 16:9 | centre | aimed |
| `top-down` (plan) | 0 | 89 degrees down | 50 | 4:3 | centre | aimed |
| `cinematic` | 30 | 1200 mm | 28 | 2.39:1 | thirds | level |
| `close-up` (detail) | 35 | 15 degrees down | 85 | 4:3 | centre | aimed |

**These are Heron's starting values, not a standard** ([D-33](../DECISIONS.md)). The picture is the judge, and each
value is the modeller's to change.

**The side** is where the camera stands: front is project south looking north, back north, left WEST, right EAST,
with project north up the plan. A compass word or degrees is taken as given.

**The lens** sizes the crop window on Revit's near plane, measured 40 mm in front of the eye
([row 5b-1281](../fragment-issues/section-5b-rows-401-425.md)): half its width is that distance times 18 over
the focal length. The camera then steps back until the elements fill the window. `readBack` gives the lens and
the picture shape the window amounts to, for any camera this tool makes.

**What is done and what is not.** The card validates (`python brain/heron_fragment.py`) and compiles for 2020 to
2027 (`tools/check-fragments-compile.py`). **Nothing here has run in Revit.** Revit was closed when the code was
ready. The card is DRAFT until the rows pass and the owner signs.

**The model.** [Group IA](group-ia.md)'s scratch project, `Materials IA Test.rvt` in `%TEMP%\heron-work\materials-ia`.
Its two original walls, `Exterior - Brick on Mtl. Stud` with the door between `x 0..6175`, `y 3825..8000`, stand in
for a small building. Select them with `SELECT_IN_REGION` `minMm 100,3700,-100`, `maxMm 6300,8100,3100`,
categories Walls. **Every picture is taken inside a rolled-back run**: `validate export-view-image --allow-publish
--write --keep-chain --setup select-in-region --setup frame-elements-in-camera`, with the frame's values and the
export's together. That puts the camera in, exports it and rolls it back, so the PNG stays and the model does not
change. Look at every picture before writing PASS.

| # | Run | Look for |
|---|---|---|
| **IF1** | The four shots without a lens or shape asked: `shot` cinematic `side` left; hero front; bird's-eye right; eye-level front | Each picture shows the two walls and the door filling the frame. **Cinematic:** wide (2.39:1), the walls on the left third, verticals vertical. **Hero:** looking up, the walls large. **Bird's-eye:** from above the east side. **Eye-level:** level, verticals vertical. `readBack` names the shot, the lens and the shape |
| **IF2** | `lensMm` 24, then 85, the same `shot` eye-level `side` front | 24: the camera close, the corners stretched. 85: the camera far back, the walls flat. Both filled. `readBack` gives 24 and 85 |
| **IF3** | `aspect` 16:9 and `composition` thirds with no `shot`; then `composition` low horizon with `eyeHeightMm` 1600 | The picture 16:9 with the walls on a third. Then the horizon on the lower third |
| **IF4** | **Version 2 unchanged.** No new input: `direction` south-east, `lookDownDeg` 30 - Group IB's IB1 again | The same eye and the same crop as version 2's record: `framed` all, window about 33 x 26 mm at -40 mm |
| **IF5** | **The negatives.** `shot` Spielberg; `side` upstairs; `lensMm` 5; `aspect` wide-ish; `composition` golden; `eyeHeightMm` 1600 with `lookDownDeg` 20 | Each refused, naming what it takes, NOTHING WAS CHANGED. The last is refused because a camera height and a look-down were both given |
| **IF6** | **The kept run.** `try-once frame-elements-in-camera` `shot` cinematic `side` left on a new view; the owner presses Keep and looks at the view | The owner sees the walls framed wide, from the west. The proof is this kept run with IF5's first negative |
| **IF7** | **The method.** `heron_method visualise-a-view` | Step 1 names the shots and which of a modeller's words pick each |

**Then sign** `frame-elements-in-camera` version 3 on IF6 with IF5, after the pictures of IF1 to IF4 have been looked
at.
