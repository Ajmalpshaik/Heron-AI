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
