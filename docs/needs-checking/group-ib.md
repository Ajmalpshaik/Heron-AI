# Needs checking — Group IB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-20 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IB - rendering: render settings, sun, display options, a framing camera, Revit's Render dialog and the read-back (2026-10-20)

From [rows 5b-1266 to 5b-1268](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-20, after four
duct heaters were placed in Project1 (Revit 2020), the owner asked Heron for a render of the 3D view and
Heron could only switch the view to Realistic and export an image. Seven tools were written and two
widened the same day:

| Capability | Folder | What it does |
|---|---|---|
| `SET_RENDER_SETTINGS` | [`set-render-settings`](../../brain/fragments/set-render-settings/fragment.yaml) | MODIFY - a 3D view's Rendering dialog: quality, output and DPI, lighting scheme, light groups, background, exposure, region |
| `REPORT_RENDER_SETTINGS` | [`report-render-settings`](../../brain/fragments/report-render-settings/fragment.yaml) | READ - Rendering, Sun Settings, Graphic Display and Camera of one view, in one answer |
| `SET_SUN_SETTINGS` | [`set-sun-settings`](../../brain/fragments/set-sun-settings/fragment.yaml) | MODIFY - study, date and time on the site's clock, preset, lighting angles, ground plane |
| `SET_GRAPHIC_DISPLAY_OPTIONS` | [`set-graphic-display-options`](../../brain/fragments/set-graphic-display-options/fragment.yaml) | MODIFY - sun and shadow intensity, silhouettes, smooth lines, transparency, hidden lines, sketchy lines, depth cueing, background |
| `FRAME_ELEMENTS_IN_CAMERA` | [`frame-elements-in-camera`](../../brain/fragments/frame-elements-in-camera/fragment.yaml) | MODIFY - a perspective camera that frames the selection from a compass direction |
| `OPEN_RENDER_DIALOG` | [`open-render-dialog`](../../brain/fragments/open-render-dialog/fragment.yaml) | EXECUTE - posts Revit's Render (or Render in Cloud, Render Gallery) on the 3D view in front |
| `READ_RENDERED_IMAGES` | [`read-rendered-images`](../../brain/fragments/read-rendered-images/fragment.yaml) | READ - saved renderings in the project and image files in a folder, with pixel sizes |
| `EXPORT_VIEW_IMAGE` v2 | [`export-view-image`](../../brain/fragments/export-view-image/fragment.yaml) | PUBLISH - adds JPG, TIFF, BMP, TGA and a DPI, and reads the written files back |
| `SET_VIEW_CAMERA` v2 | [`set-view-camera`](../../brain/fragments/set-view-camera/fragment.yaml) | MODIFY - adds heights from a level and the far clip |

**What is done and what is not.** Every card validates (`python brain/heron_fragment.py`) and compiles for
2020 to 2027 (`tools/check-fragments-compile.py`); every Revit member each calls was read off the shipped
reference assemblies and `RevitAPI.xml` of all eight releases on 2026-10-20. **Nothing here has run in
Revit.** Every card is DRAFT until its rows pass and the owner signs. `EXPORT_VIEW_IMAGE` and
`SET_VIEW_CAMERA` were PROVEN: their version 1 proofs are kept on the cards as records, and D-117 refuses
a kept change from either until IB11 and IB2 are signed.

**NO REVIT RELEASE LETS AN ADD-IN RUN THE RENDER.** `View3D` has `GetRenderingSettings` and
`SetRenderingSettings` on 2020 to 2027 and no member that renders, so the render itself is the modeller's
press in Revit's own dialog (IB9), and nothing is called rendered until IB10 reads an image back.

**The model.** A **scratch copy** of the owner's *Project1* (Revit 2020) holding the four duct heaters placed
on 2026-10-20, saved to `%TEMP%\heron-work\rendering-ib` - never a working model - with the floor plan `Level 1` the
heaters show in, a schedule named `Heron Render Schedule`, the default `{3D}` view, and an empty scratch folder
`%TEMP%\heron-work\rendering-ib\out`.
Run every row on **Revit 2020**. Every change is one entry in Revit's undo list; Ctrl+Z after a row puts it
back. Each MODIFY row is a DRAFT's one run (`heron_bridge_client.py try-once`, D-119) where its positive is
kept, and its negative is `validate <fragment> --write --kept ...`, rolled back (D-120). **Run the rows in
order:** IB1 makes the camera view the others use, and IB8, the read-back's negative, must run before IB9 renders.

| # | Run | Look for |
|---|---|---|
| **IB1** | *`FRAME_ELEMENTS_IN_CAMERA`.* One duct heater selected; viewName `Heron Frame - Heater`, direction south-east, lookDownDeg 30, distanceMm auto, marginPercent 10. **Negative:** the heater selected, viewName `{3D}` | **Positive:** madeNew true, framed 1. **Open the view: the heater fills it with a margin, seen from the south-east and above** - the check the numbers cannot make. Note readBack's crop box numbers beside what is seen ([row 5b-1267](../fragment-issues/section-5b-rows-401-425.md) wants them). **Negative:** refused *"is an isometric 3D view"*; `{3D}`'s camera unchanged |
| **IB2** | *`SET_VIEW_CAMERA` v2.* viewName `Heron Frame - Heater`, eyeMm and targetMm the eye and target IB1's readBack gives, levelName `Level 1`, farClipMm 20000. **Negatives:** levelName `No Such Level`; farClipMm `far` | **Positive:** readBack with Eye Elevation and Target Elevation, *"far clip on at 20000 mm"*; the view's Properties: Far Clip Active ticked, Far Clip Offset 20000. **Negatives:** refused, the camera unchanged. **Say** whether Eye Elevation reads the height above Level 1 or above the project base |
| **IB3** | *`SET_RENDER_SETTINGS`.* view `Heron Frame - Heater`: quality Draft, output 150 DPI, lighting `Exterior: Sun only`, background `Sky: Few Clouds`, exposure `value=13; white point=6500`. **Negatives:** view the floor plan, quality Draft; then the 3D view, quality `Ultra` | **Positive:** changed 5 of 5; readBack and the Rendering dialog (View Control Bar teapot) agree. **Say** whether the dialog's Quality list reads Draft, Medium, High, **Best** - Heron writes the API's VeryHigh as Best - and which DPIs the Printer list offers. **Negatives:** changed 0, refused, the dialog unchanged |
| **IB4** | *`SET_SUN_SETTINGS`.* view `Heron Frame - Heater`: study Still, date 2026-06-21, time 10:00, groundPlaneLevel `Level 1`. Then preset - a name the Sun Settings dialog's Presets list shows. **Negatives:** view the schedule, study Still; then preset `No Such Preset` | **Positive:** changed 3 of 3; the Sun Settings dialog shows 21 June 2026 at 10:00 - **or 11:00, if readBack says the site uses daylight saving: say which** - and Ground Plane at Level 1. The preset run: readBack names the preset. **Negatives:** refused; the second lists the project's presets |
| **IB5** | *`SET_GRAPHIC_DISPLAY_OPTIONS`.* view `Heron Frame - Heater`: sunIntensity 80, shadowIntensity 40, smoothLines yes, sketchyLines `jitter=5; extension=3`, castShadows on. **Negatives:** view the schedule, sunIntensity 80; then the 3D view with only castShadows on | **Positive:** changed 4 of 4, Graphic Display Options showing the same, and a finding naming Cast Shadows as having no API member - not counted. **Negatives:** refused *"has no Graphic Display Options"*; then refused *"Only settings Revit's API cannot reach were asked for"* |
| **IB6** | *`REPORT_RENDER_SETTINGS` - tracking (D-53).* view `Heron Frame - Heater`; then the floor plan; then the schedule | sectionsRead **4**, then **2**, then **0** - the count follows the view. Each section matches its dialog opened by hand |
| **IB7** | *`OPEN_RENDER_DIALOG`, through `revit_show`.* `Heron Frame - Heater` in front, command Render. **Negative:** the floor plan in front | **Positive:** posted true and Revit's Render dialog opens on the 3D view once the answer returns. **Negative:** posted false, refused *"is not a 3D view"*, no dialog |
| **IB8** | *`READ_RENDERED_IMAGES` - the negative, BEFORE IB9.* folder the empty `out` folder | renderings 0, report *"Nothing rendered was read back"* |
| **IB9** | *The render itself - by hand, no Heron call.* In the dialog IB7 opened, press **Render** at Draft; then **Save to Project** and **Export...** as PNG into `out` | A view under Renderings in the Project Browser, and a PNG in `out`. This is the step no API can take |
| **IB10** | *`READ_RENDERED_IMAGES` - the positive.* folder `out`, sinceMinutes 30 | renderings 2: the saved rendering by name and pixel size, and the PNG with the same pixel size read from its header. Compare with the PNG's properties in Windows |
| **IB11** | *`EXPORT_VIEW_IMAGE` v2 - a PUBLISH, through its switch (D-106).* The owner turns Publish on; `validate export-view-image --allow-publish --write` with views `Heron Frame - Heater`, folder `out`, filePrefix `ia10`, fileType JPG, dpi 300, no pixelWidth. **Negatives:** fileType `webp`; dpi 400 | **Positive:** exported 1, filesWritten 1, fileReport naming one `ia10*.jpg` with its pixel size. **Negatives:** exported 0, filesWritten 0, the findings naming the types and DPIs Revit takes, nothing new in `out` |
| **IB12** | *Optional - Ray Trace on a second release.* On Revit 2024 or 2027, `SET_VIEW_PROPERTIES` visualStyle `Ray Trace` on a 3D view | Refused with Revit's reason and nothing changed - 2021 on has no API value for it (read off every release's assembly on 2026-10-20); 2020 throws (measured 2026-10-13) |

**Then sign** `frame-elements-in-camera` on IB1, `set-view-camera` on IB2, `set-render-settings` on IB3,
`set-sun-settings` on IB4, `set-graphic-display-options` on IB5, `report-render-settings` on IB6 (tracking),
`open-render-dialog` on IB7, `read-rendered-images` on IB10 with IB8, and `export-view-image` on IB11. The
read-only and show rows are in [`tools/jobs/rendering-ib-2026-10-20.yaml`](../../tools/jobs/rendering-ib-2026-10-20.yaml);
the MODIFY rows' positives are `try-once` runs and IB11 is run by name, because no batch sends a PUBLISH.

### Run 2026-10-20 - Revit 2020, the owner's laptop - all nine signed by Ajmal PS, PROVEN

**The model.** Not a copy of Project1: Project1 had never been saved, so there was no file to copy. The run
used Group IA's scratch project, `Materials IA Test.rvt` in `%TEMP%\heron-work\materials-ia`. Its two columns
stood in for the heaters, and `Space Outdoor Air Schedule` for `Heron Render Schedule`; its plan is `1 - Mech`
and its level `Level 1`. The view the rows make is named **`Heron Frame - Columns`**. Kept runs went through
`try-once` (D-119), the owner pressing Keep, and the negatives through `validate --kept` (D-120). Every other
run was rolled back.

| # | Result | What was seen |
|---|---|---|
| IB1 | **PASS** | Kept, with the two columns selected (`select-by-category-name` Columns in `1 - Mech`, then `set-selection`): `framed` 2, `madeNew` true, azimuth 135 (south-east), look-down 30. Negative `{3D}`: `framed` 0, refused, `{3D}` unchanged. The exported picture (IB11) shows both columns, the walls, the door and the ducts, seen from the south-east and above |
| IB2 | **PASS** (version 2) | Kept on `Heron Frame - Columns`: eye 9500,-8000,7000 mm, target 1500,0,2000, `Level 1`, far clip 20000. The kept run's record could not be signed, because `madeNew` - its only count - is false on a view that already exists. **Signed record, as version 1's was:** a NEW view `Heron Camera IB2`, rolled back: `madeNew` true, readBack *"eye 9500,-8000,7000 mm, looking -0.6468,0.6468,-0.4042, perspective, heights from "Level 1" - Eye Elevation 7000 mm, Target Elevation 6877 mm, far clip on at 20000 mm"*. Negative `levelName` `No Such Level`: refused, nothing changed. **Eye Elevation above Level 1 or the project base:** cannot be told apart here, because Level 1 is at 0 |
| IB3 | **FAIL on version 1, fixed in version 2 ([row 5b-1278](../fragment-issues/section-5b-rows-401-425.md)), PASS on version 2** | Version 1 threw *"The resolution target is not printer."* - nothing kept. Version 2, rolled back: quality Draft, output Printer 150 DPI, lighting Exterior: Sun only, background Sky: Few Clouds, exposure 13 / white point 6500 - `changed` 5 of 5. The owner pressed Keep on a second try, but the run outlived Heron's wait, so that one is not recorded; `REPORT_RENDER_SETTINGS` reads the five on the view. **Signed record:** other values, rolled back - Medium, 300 DPI, Exterior: Sun and Artificial, Sky: Cloudy - `changed` 4 of 4. Negative, the schedule: refused *"is not a 3D view"*, `changed` 0. **By eye, not answered:** the dialog's Quality words and Printer DPIs |
| IB4 | **PASS** | Kept: Still study, 2026-06-21 10:00, ground plane at Level 1 - `changed` 3 of 3, readBack *"preset none matches ... site 'Boston, MA', latitude 42.21, longitude -71.03, time zone -5"*. Negative, the schedule: refused, `changed` 0. **Not run:** the preset run and `No Such Preset`. **By eye, not answered:** 10:00 or 11:00 in the Sun Settings dialog |
| IB5 | **FAIL on version 1, fixed in version 2 ([row 5b-1279](../fragment-issues/section-5b-rows-401-425.md)), PASS on version 2** | Version 1 threw *"This view cannot use Depth Cueing"* on the perspective view - nothing kept. On version 2, smooth lines were then refused, *"The silhouette line style id is not a valid line style to apply to the view"*: the new camera view held silhouette style -1. Version 2 gives such a view Wide Lines first. **Kept:** sun 80, shadow 40, smooth lines on, sketchy jitter 5 / extension 3 - `changed` 4 of 4. Findings: Cast Shadows has no API member, and *"had no silhouette line style ... it was given Wide Lines; silhouettes stay off"*. Negative, the schedule: refused, `changed` 0 |
| IB6 | **PASS** (tracking) | `sectionsRead` **4** on `Heron Frame - Columns`, **2** on `1 - Mech`, **0** on the schedule - the count follows the view. The signed record is 4 against 0 |
| IB7 | **PASS** | With `Heron Frame - Columns` in front, Render: `posted` true, and Revit's Render dialog opened - the owner rendered in it. Negative `1 - Mech`: `posted` false, refused *"is not the view in front"*. **Recorded:** the refusal names the view in front, not *"is not a 3D view"* as the row expected |
| IB8 | **PASS** | Before the render: `renderings` 0, *"Nothing rendered was read back: no view under Renderings in this project and no image file in ...\out"* |
| IB9 | **Done by the owner** | Render at Draft, then **Save to Project**, named `Export PNG`. The PNG export to `out` was not done; IB11's JPG stands in for a file in the folder |
| IB10 | **PASS** | `renderings` 1: *"saved rendering 'Export PNG' 3794 x 1715 px"*. Negative in `Project2` with an empty folder: `renderings` 0. The PNG's pixel size against Windows: not run - there was no PNG |
| IB11 | **PASS** (version 2, Publish on) | `exported` 1, `filesWritten` 1, *"'ib11 - 3D View - Heron Frame - Columns.jpg' 2030 x 859 px, 164 KB"*. Negative `fileType` `webp`: `exported` 0, `filesWritten` 0, *"No image type is called 'webp'"*. **Not run:** dpi 400 |
| IB12 | **NOT RUN** - optional, Revit 2024 or 2027 | |

**Signed 2026-10-20 by Ajmal PS**, on his word (*"finish it all"*): all nine above - `frame-elements-in-camera`,
`set-view-camera` v2, `set-render-settings` v2, `set-sun-settings`, `set-graphic-display-options` v2,
`report-render-settings`, `open-render-dialog`, `read-rendered-images` and `export-view-image` v2. Each draft's
gap is *"no second route was run"*.
