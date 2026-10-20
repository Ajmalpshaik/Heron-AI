# Needs checking — Group IC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-20 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IC - the visualiser: a whole look in one call, what a look would do, and the method a visualisation artist follows (2026-10-20)

From [rows 5b-1269 and 5b-1270](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-20, after four
Heron-built duct heaters were placed in Project1 (Revit 2020), the owner asked for a render; the material
tools ([Group IA](group-ia.md)) and the rendering tools ([Group IB](group-ib.md)) were built that day, and
then his words: *"make like a visualizer ... what settings we normally do for good things ... what
settings will normally focus, normally change to [make it] good looking, and the materials also"*. Built
the same day, on the two groups' tools:

| What | Where | What it does |
|---|---|---|
| `APPLY_VISUAL_PRESET` | [`apply-visual-preset`](../../brain/fragments/apply-visual-preset/fragment.yaml) | MODIFY - one of seven looks given to a view in one call: Exterior Day, Exterior Dusk, Interior Day, Interior Night, Equipment Close-up, Clay, Presentation Elevation - every setting read back, every one the view or Revit's API cannot take named and skipped |
| `REPORT_VISUAL_PRESET` | [`report-visual-preset`](../../brain/fragments/report-visual-preset/fragment.yaml) | READ - a view against one preset, setting by setting, or against all seven, closest first; and the materials on what it shows with no render appearance |
| `visualise-a-view` | [`brain/skills/visualise-a-view.yaml`](../../brain/skills/visualise-a-view.yaml) | the method, served by `heron_method`: read first, camera, materials, light, render settings, preview then render, read back and look - each step's capability, why, and what to look for in the picture |

**What is done and what is not.** Both cards validate (`python brain/heron_fragment.py`) and compile for
2020 to 2027 (`tools/check-fragments-compile.py`); the method validates (`python brain/heron_skill.py`) and
every capability it names resolves (`heron_method`). Every Revit member the two fragments call is one the
Group IA and IB tools already call, but two, read with `tools/api-surface --members` on all eight
releases: `SunAndShadowSettings.GetSunset` and `View3D.IsPerspective`. The presets and the way each setting
is read are written twice, once in each fragment - they cannot share code - and
[`tests/test_visual_presets.py`](../../tests/test_visual_presets.py) holds the two copies to the same text,
the read to no write, and every preset value inside the range its own tool takes. **Nothing here has run
in Revit.** Both cards are DRAFT until their rows pass and the owner signs.

**THE PRESETS ARE HERON'S STARTING LOOKS, NOT A STANDARD** ([D-33](../DECISIONS.md)). Every value is what a
visualiser tries first; the picture is the judge (IC9), and any one setting is changed after with its own
tool. **Two settings every preset wants have no member in Revit's API** on any release 2020 to 2027 - Cast
Shadows and Show Ambient Shadows ([row 5b-1267](../fragment-issues/section-5b-rows-401-425.md)) - so the
answer names them as the modeller's to tick, and never claims them done.

**The model.** A **scratch copy** of the owner's *Project1* (Revit 2020) holding the four duct heaters placed
on 2026-10-20, saved to `%TEMP%\heron-work\visualiser-ic` - never a working model - with the floor plan
`Level 1` the heaters show in, any **elevation** (say which), a schedule named `Heron Render Schedule`, and
the perspective view **`Heron Frame - Heater`** made by `FRAME_ELEMENTS_IN_CAMERA` and kept - Group IB's IB1;
if IB has been run on its own copy, make it again here the same way. Run every row on **Revit 2020**.
**Before IC0**, Group IA's IA0 - the add-in rebuilt and redeployed with D-146's namespace - must have been
done on the PC that runs this, or the material tools in IC10 will not compile there; it passed on the owner's
PC on 2026-10-20 ([Group IA](group-ia.md)). Every change is one entry in Revit's undo
list; Ctrl+Z after a row puts it back. [`tools/jobs/visualiser-ic-2026-10-20.yaml`](../../tools/jobs/visualiser-ic-2026-10-20.yaml)
runs a read and a rolled-back write of both tools as a batch, on Exterior Day - a look that differs from
IC4's whether the batch runs before IC4 or after it; the rows below are run by name. Each MODIFY
row's kept positive is a DRAFT's ONE run (`heron_bridge_client.py try-once`, D-119) - IC4, and no other -
and every other write row is `validate <fragment> --write ...`, rolled back (D-120). **Run the rows in order:** IC2 reads the view before
IC4 changes it, and IC5 reads it after.

| # | Run | Look for |
|---|---|---|
| **IC0** | *Set up.* The scratch copy above, `Heron Frame - Heater` open in front. `REPORT_RENDER_SETTINGS` on it, its `setup` kept as the BEFORE | Four sections read. Keep the text: IC3 and IC4 are measured against it |
| **IC1** | *`REPORT_VISUAL_PRESET` - the negatives FIRST.* view `Heron Frame - Heater`, preset `Studio Glow`; then view `Heron Render Schedule`, preset `Exterior Day` | **First:** refused, naming the seven presets, wouldChange 0, presetsCompared 0. **Second:** wouldChange 0, every line of comparison *skipped* with its reason - the schedule has no visual style, no sun and no Rendering Settings |
| **IC2** | *`REPORT_VISUAL_PRESET` - the positive.* view `Heron Frame - Heater`, preset `Equipment Close-up`, nothing else. Then preset `all` | **First:** wouldChange above 0 - note it; each line's *now* matches IC0's sections and the dialogs opened by hand; Cast Shadows and Ambient Shadows *by hand*; materialsWithoutAppearance the heaters' materials with no appearance, by name - **check them against `LIST_MATERIALS` withAllTabs**. **All:** presetsCompared 7, closest first |
| **IC3** | *`APPLY_VISUAL_PRESET` - the negatives FIRST.* view `Heron Render Schedule`, preset `Exterior Day`; then view `Heron Frame - Heater`, preset `Studio Glow`; then preset `Clay`, renderFor `print` | Each refused with **NOTHING WAS CHANGED**, changed 0: *"takes none of Exterior Day's settings"*, then the seven names, then *check or issue*. `REPORT_RENDER_SETTINGS` on the camera view reads exactly IC0's BEFORE |
| **IC4** | *`APPLY_VISUAL_PRESET` - the positive, kept.* `try-once apply-visual-preset` with view `Heron Frame - Heater`, preset `Equipment Close-up`, renderFor `check`; press **Keep** | changed **equal to IC2's wouldChange**, skipped 0; readBack every setting *(as the preset)*; a finding naming Cast Shadows and Ambient Shadows by hand, and the materials line. **Open the view and say what you see:** Realistic, a light grey ground, **the light from the top right** - say whether *135/35 relative to view* reads as top right, or another corner. The Rendering, Sun Settings and Graphic Display Options dialogs show the same as readBack. **One Ctrl+Z takes the whole look back** - then Ctrl+Y |
| **IC5** | *The count follows the view (D-53).* `REPORT_VISUAL_PRESET` preset `Equipment Close-up` again; then `validate apply-visual-preset --write` with the same values | **Read:** wouldChange **0**. **Write:** changed 0, alreadySo equal to IC4's changed plus its alreadySo - nothing set twice |
| **IC6** | *Dusk - the sunset Revit gives.* `validate apply-visual-preset --write` (rolled back) on `Heron Frame - Heater`, preset `Exterior Dusk` | readBack's Sun reads *still HH:MM* and Render Lighting *Exterior: Sun and Artificial*. **Then by hand, on the same view, without saving:** Sun Settings, a Single Day study with *Sunrise to sunset* ticked, the same date - the sunset Revit shows. **Say whether readBack's time is 30 minutes before that sunset, or an hour off** (the API reference says `GetSunset` is UTC adjusted for daylight saving where the site uses it; readBack says when it does). Close the dialog with Cancel |
| **IC7** | *Presentation Elevation on an elevation.* `validate apply-visual-preset --write` (rolled back), view the elevation, preset `Presentation Elevation` | Shaded with Edges, silhouettes on, depth cueing *start=0; end=100; fade=50*, lighting *225/35 relative to view* read back; no render line - the preset asks none. How it LOOKS is IC10's, once the card is signed |
| **IC8** | *A floor plan takes part of a look.* `validate apply-visual-preset --write` (rolled back), view `Level 1`, preset `Exterior Day` | The display lines and Sun changed and read back; **Render Quality, Render Output, Render Lighting, Render Background and Exposure skipped** - *only a 3D view has Rendering Settings* - and Display Background skipped - *3D views, sections and elevations only* |
| **IC9** | *For an issued image.* `validate apply-visual-preset --write` (rolled back), `Heron Frame - Heater`, preset `Exterior Day`, renderFor `issue`, sunTime `15:00` | Render Quality *High*; Render Output *300 DPI* - **or** a not-taken line naming the DPIs Revit's printer list offers (say which); Sun *still 15:00* |
| **IC10** | *The method, end to end - the owner's job - AFTER `apply-visual-preset` is signed on IC1 to IC5, so its change can be kept through `revit_change` (a DRAFT is kept once, IC4).* In a chat on the PC: *"make the duct heater view look good"*. The chat calls `heron_method visualise-a-view` once, then the steps: read (IC2), frame (IB1's view), materials - `SET_MATERIAL_APPEARANCE` on the heaters' materials IC2 named (Group IA - PROVEN on Revit 2020 since 2026-10-20), `APPLY_VISUAL_PRESET` Equipment Close-up, the modeller ticks Cast Shadows and Show Ambient Shadows, the Realistic preview looked at, `OPEN_RENDER_DIALOG`, **the modeller presses Render** at Draft and Saves to Project, `READ_RENDERED_IMAGES`, and the image **looked at** against step 6's list | Record: the time from the first message to the read-back; which steps were by hand; **what the image showed against step 6's list** (dark, washed out, grey materials, leaning verticals, grain, size, cut off, colour) and what was changed for it. The materials are no longer flat grey. Nothing is called rendered before `READ_RENDERED_IMAGES` finds it. **Then** `Presentation Elevation` on the elevation, kept: the light from the top left, shadows falling down and to the right - and *Equipment Close-up*'s light from the top right in IC4 - say if either is another corner |
| **IC11** | *Routing on the PC's own backend.* `tools/score-routing.py --sentences` with the set below (set `9cd7d96a`), `--backend model+rerank`, on `main` with this merged | The same answers this pull request recorded on lexical and model (row 5b-1270), or the differences named. A question reaching `APPLY_VISUAL_PRESET` is a finding, not tuned away |

**Then sign** `report-visual-preset` on IC2 with IC1 (IC5 the tracking) and `apply-visual-preset` on IC4
(`validate apply-visual-preset --write --kept`) with IC3 - after IC5, before IC10. IC6 to IC9 are rolled-back
runs whose numbers are the evidence; a surprise in any of them reopens the card. Sign the method
`visualise-a-view` on IC10 (`tools/prove-skill.py`).

**IC11's sentences**, frozen on 2026-10-20 - copy them to a file of their own (`s.yaml`) to run:

```yaml
groups:
  - group: a whole look - the task's words
    expect: want APPLY_VISUAL_PRESET
    sentences:
      - make this view look good
      - make this 3D view look good for a render
      - do the render setup for this view
      - visualise this 3D view
      - give the duct heater view a nice look for rendering
      - give this view an exterior day look
      - set this view up with the clay preset
      - use the equipment close-up preset on this view
      - apply a presentation look to this elevation
      - make this view look like dusk
  - group: what a look would do - questions
    expect: want REPORT_VISUAL_PRESET
    sentences:
      - what would the clay preset do to this view
      - which preset is this view closest to
      - which materials in this view will render grey
      - what visual presets are there
      - how different is this view from the exterior day look
  - group: questions that must never reach a write
    expect: not-a-write
    sentences:
      - what does the exterior day preset change
      - is this view ready to render
      - how will this view look in a render
      - which materials have no render appearance
  - group: the neighbours keep theirs
    sentences:
      - {say: set the render quality to high, expect: want SET_RENDER_SETTINGS}
      - {say: render at 300 dpi, expect: want SET_RENDER_SETTINGS}
      - {say: change the render background to a plain white colour, expect: want SET_RENDER_SETTINGS}
      - {say: set the sun to 3 pm on 21 March, expect: want SET_SUN_SETTINGS}
      - {say: make a solar study for the whole day, expect: want SET_SUN_SETTINGS}
      - {say: turn on sketchy lines, expect: want SET_GRAPHIC_DISPLAY_OPTIONS}
      - {say: set the shadow intensity to 30, expect: want SET_GRAPHIC_DISPLAY_OPTIONS}
      - {say: put this view on realistic, expect: want SET_VIEW_PROPERTIES}
      - {say: change the visual style to hidden line, expect: want SET_VIEW_PROPERTIES}
      - {say: render it, expect: want OPEN_RENDER_DIALOG}
      - {say: open the render dialog, expect: want OPEN_RENDER_DIALOG}
      - {say: save this view as a jpg, expect: want EXPORT_VIEW_IMAGE}
      - {say: what are the render settings of this 3D view, expect: want REPORT_RENDER_SETTINGS}
      - {say: which sun does this view use, expect: want REPORT_RENDER_SETTINGS}
      - {say: frame the selected heater in a camera view, expect: want FRAME_ELEMENTS_IN_CAMERA}
      - {say: give this material a glass appearance, expect: want SET_MATERIAL_APPEARANCE}
      - {say: list the materials with their appearance, expect: want LIST_MATERIALS}
      - {say: add the starter materials, expect: want CREATE_STARTER_MATERIALS}
```

### Run 2026-10-20 (evening) - Revit 2020, the owner's laptop

**The model.** Not a copy of Project1: the Project1 in this Revit had never been saved, so there was nothing to
copy. The run used [Group IA](group-ia.md)'s scratch project, `Materials IA Test.rvt` in
`%TEMP%\heron-work\materials-ia`, the model Group IB ran on. **Its names stand in for the plan's**, the same
way every time:

| The plan's name | Used here |
|---|---|
| `Heron Frame - Heater` | `Heron Frame - Columns`, IB1's kept perspective of the two columns |
| `Level 1` | `1 - Mech`, the template's plan of Level 1 |
| `Heron Render Schedule` | `Space Outdoor Air Schedule` |
| the elevation | `South - Mech` |

Rows were run from the command line (`HERON_CLIENT_ID=ajmal-pc`), each write rolled back. IC4, IC5 and IC10
need the owner and are not run yet. **Nothing is signed.**

| # | Result | What was seen |
|---|---|---|
| IC0 | PASS | `REPORT_RENDER_SETTINGS` on `Heron Frame - Columns`: `sectionsRead` 4. The BEFORE, kept: *Rendering - quality Draft; output Printer 150 DPI; lighting Exterior: Sun only; background SkyFewClouds; exposure value 13 ... white point 6500 / Sun Settings - Still study, 2026-06-21 10:00 ... 'Boston, MA' ... time zone -5 / Graphic Display - visual style HLR; sun intensity 80, shadow intensity 40; silhouettes off, smooth lines on ... sketchy lines on, jitter 5, extension 3 / Camera - perspective, eye 9500,-8000,7000 mm* |
| IC1 | PASS | `Studio Glow`: refused, *"No visual preset is called 'Studio Glow'. The presets: Exterior Day, Exterior Dusk, Interior Day, Interior Night, Equipment Close-up, Clay, Presentation Elevation. Nothing was read."*; `wouldChange` 0, `presetsCompared` 0. The schedule, `Exterior Day`: `wouldChange` 0, *"0 setting(s) would differ, 0 already so, 13 this view cannot take, 2 by hand"*, each line skipped with *"has no Graphic Display Options"* |
| IC2 | PASS | `Equipment Close-up`: `wouldChange` **6**, 7 already so, 0 the view cannot take, 2 by hand. Visual Style Hidden Line -> Realistic, Sketchy Lines on -> off, Display Background none -> gradient, Sun still 10:00 -> lighting 135/35 relative to view, Render Output 150 DPI -> Screen, Render Background Sky: Few Clouds -> colour 245,245,245. Each *now* matches IC0. `materialsWithoutAppearance` 0 - *"Materials 10 on what the view shows, 0 with no render appearance"*: every material in the view has one after Group IA. `all`: `presetsCompared` **7**, closest first - Clay 5, Presentation Elevation 5, Exterior Day 6, Equipment Close-up 6, Interior Day 7 ... **Dialogs by hand: owed** |
| IC3 | PASS | The schedule, `Exterior Day`: refused *"takes none of Exterior Day's settings ... NOTHING WAS CHANGED"*. `Studio Glow`: refused naming the seven. `Clay` with renderFor `print`: refused *"'print' is not what the render is for. Say check ... or issue ..."*. `changed` 0 each time. `REPORT_RENDER_SETTINGS` afterwards: its `setup` **identical** to IC0's BEFORE |
| IC4 | NOT RUN - owner's Keep | The one kept run (`try-once`) needs the model saved by the owner and his Keep |
| IC5 | NOT RUN - follows IC4 | |
| IC6 | **FAIL on the Sun line ([row 5b-1282](../fragment-issues/section-5b-rows-401-425.md))** | Rolled back, `Exterior Dusk`: `changed` 9, `alreadySo` 3, `skipped` 1. Realistic, sun 40, shadow 70, the dusk gradient, Exterior: Sun and Artificial, Sky: Very Few Clouds, exposure 11 - each *(as the preset)*. **Sun skipped:** *"Revit would not give the sunset for the site: Revit does not accept input DateTime objects if they are of kind DateTypeKind.Unspecified Parameter name: dateTime"*. Not fixed, as the run's brief says - the owner decides. The sunset comparison by hand waits for the fix |
| IC7 | **FAIL on Depth Cueing ([row 5b-1283](../fragment-issues/section-5b-rows-401-425.md))** | Rolled back, `South - Mech`, `Presentation Elevation`: `changed` 4. Shaded with Edges, silhouettes off -> on, smooth lines off -> on, *"Sun lighting 225/35 relative to view (as the preset)"*. **Depth Cueing skipped:** *"'South - Mech' cannot use depth cueing - Revit offers it in elevations and sections"* - on an elevation. Measured with rolled-back probes: `CanUseDepthCueing` is false on every elevation of this Systems template; not changed by Far Clipping or any visual style; **true once View Discipline is Architectural** (and still true at Fine). The skip is Revit's, but the reason given is wrong |
| IC8 | PASS | Rolled back, `1 - Mech`, `Exterior Day`: `changed` 3, `alreadySo` 3, `skipped` 6 - Render Quality, Output, Lighting, Background and Exposure *"'1 - Mech' is not a 3D view, and only a 3D view has Rendering Settings"*; Display Background *"Revit sets a display background on 3D views, sections and elevations only"* |
| IC9 | PASS | Rolled back, `Heron Frame - Columns`, `Exterior Day`, renderFor `issue`, sunTime `15:00`: Render Quality **High**, Render Output **300 DPI** - taken, so no not-taken line - Sun **still 15:00**; `changed` 8, `alreadySo` 5, `skipped` 0 |
| IC10 | NOT RUN - after the owner signs `apply-visual-preset` | |
| IC11 | **Findings, recorded** | `score-routing.py --sentences` (set `9cd7d96a`, 37 sentences) on **model+rerank**, this PC: **24 of 33** wanted. Row 5b-1270 recorded lexical 28 and model 24. 1 question reached a write. Wanted, something else answered: *make this 3D view look good for a render* -> SET_RENDER_SETTINGS (wants APPLY_VISUAL_PRESET, 3rd); *do the render setup for this view* -> REPORT_RENDER_SETTINGS; *what would the clay preset do to this view* -> APPLY_VISUAL_PRESET (wants REPORT_VISUAL_PRESET, 2nd); *which materials in this view will render grey* -> SET_MATERIAL_APPEARANCE (4th); *what visual presets are there* -> APPLY_VISUAL_PRESET (2nd). **Four neighbours differ from row 5b-1270's "the neighbours held"**: *put this view on realistic* -> SET_MATERIAL_APPEARANCE; *change the visual style to hidden line* -> CREATE_LINE_PATTERN (5th); *render it* -> REPORT_RENDER_SETTINGS (2nd); *what are the render settings of this 3D view* -> SET_RENDER_SETTINGS (2nd). Question -> write: *what does the exterior day preset change* -> APPLY_VISUAL_PRESET, as 5b-1270 recorded. Asked through `heron_lookup` with the question flag, *what are the render settings of this 3D view* reached REPORT_RENDER_SETTINGS - D-128 leaves the writes out. Not tuned |

**The batch** - `tools/jobs/visualiser-ic-2026-10-20.yaml` with the names above swapped in, a local copy:
**2 PASS**. `report-visual-preset` `wouldChange` 6 against `Studio Glow` refused. `apply-visual-preset` rolled back,
`changed` 6, against the schedule refused. Both drafts wait for the owner's word. `apply-visual-preset`'s plan
signs it on IC4's kept run, which is not run yet.

**IC4 and IC5, run once the owner was back (same evening):**

| # | Result | What was seen |
|---|---|---|
| IC4 | PASS | `try-once`, saved model, the owner pressed **Keep**: `changed` **6** - IC2's `wouldChange` - `alreadySo` 7, `skipped` 0. readBack every setting *(as the preset)*: Realistic, sun 80, shadow 40, smooth lines, the light grey gradient, *"Sun lighting 135/35 relative to view"*, Draft on Screen, Exterior: Sun only, colour 245,245,245, exposure 13; Cast Shadows and Ambient Shadows named as by hand. **The light:** the owner did not notice. In the exported picture of the view, the columns' right faces and tops are bright and their left faces dark - **top right**. The dialogs by hand and the Ctrl+Z / Ctrl+Y look were not made |
| IC5 | PASS | `REPORT_VISUAL_PRESET` Equipment Close-up again: `wouldChange` **0**. `apply-visual-preset --write`, rolled back: `changed` **0**, `alreadySo` **13** - IC4's 6 plus its 7 |

**Signed 2026-10-20 by Ajmal PS**, on his word (*"you can sign it now"*): `report-visual-preset` (batch: Exterior Day
`wouldChange` 6 against `Studio Glow` refused) and `apply-visual-preset` (IC4 kept, against the schedule refused),
both PROVEN. Still owed: IC6 and IC7's repairs (rows 5b-1282 and 5b-1283, the owner's word), IC10 (the method
end to end, his Render), and the method's own signature.
