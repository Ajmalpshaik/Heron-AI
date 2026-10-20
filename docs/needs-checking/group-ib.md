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
