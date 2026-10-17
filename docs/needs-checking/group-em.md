# Needs checking — Group EM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EM - the family method looks at what it built: `ZOOM_TO_ELEMENTS` version 2, `EXPORT_VIEW_IMAGE` in a family, and `family-creation` step 13 (2026-10-17)

Found on 2026-10-14 building two fixed monitor families on Revit 2020. Both were checked by
`REPORT_FAMILY_FORMS`, every size as planned, and then by a desktop screen grab of the Revit window taken
outside Heron - which showed the 55 inch family half off the screen, because the 3D view had never been
zoomed. A part on the wrong side, a missing part or one floating off the body reads back with the right
sizes. [Row 5b-496](../fragment-issues/section-5b-rows-401-425.md) has the whole of it.

**What changed.**
[`ZOOM_TO_ELEMENTS`](../../brain/fragments/zoom-to-elements/fragment.yaml) (FRG-SEL-030, DRAFT, version 2)
declares the ambient `editScopeFailures` need, so the add-in runs it inside the job's group with **no
transaction open** ([D-115](../DECISIONS.md)): `revit_change` is the only door a chat has for it, and that
door ran version 1 inside Heron's transaction, where Revit changes no view (row 5b-456). When Show is
refused, Revit's words now come first and `shown` stays 0. No new input; nothing in the add-in changed.
[`EXPORT_VIEW_IMAGE`](../../brain/fragments/export-view-image/fragment.yaml) (FRG-VIEW-073, PROVEN) is **not
changed** - its code shows no gap in a family - but it has only ever run in a project, so it is checked here
where the method now uses it. `family-creation` step 13 now opens "View 1", shades it at Fine with
`SET_VIEW_PROPERTIES`, zooms it to every solid form, exports it (and "Front") into a folder under
`%TEMP%\heron-work`, and the agent opens the picture and looks before telling the modeller it is done.

**What is compiled and what is not run.** The fragments compile on every release they claim
(`tools/check-fragments-compile.py`, 2026-10-17). **None of it has run in Revit.** `ZOOM_TO_ELEMENTS`'
`impl/` moved, so its 2026-09-13 proof is stale and marked so; EM3 and EM4 are the runs that replace it.
`EXPORT_VIEW_IMAGE`'s `impl/` did not move - its proof stands for a project. **Until version 2 is signed, a chat cannot run the zoom:** [D-117](../DECISIONS.md) refuses a DRAFT through `revit_change` before it is sent, so EM3 to EM5 run through the proving client, rolled back, and step 13 says to ask the modeller for ZF meanwhile.

**ONE SCRATCH FAMILY, never a working model.** Make it before EM1, in Revit 2020 first: a family from
*Metric Generic Model.rft* saved as *Heron Look Scratch.rfa* under `%TEMP%\heron-work`, with a box body
400 x 100 x 300 mm on Ref. Level, a bracket box 200 x 60 x 200 mm against its back, and one small cylinder
on its front set to show at Fine only (`CREATE_FAMILY_FORMS`, `detailLevels=fine` on its row). Leave
"Ref. Level" (the floor plan) in front. Changes and Publish both switched on in Revit's ribbon.

| # | Run | Look for |
|---|---|---|
| **EM1** | `revit_change` capability `OPEN_VIEW`, `viewName=View 1`, with Ref. Level in front | View 1 on screen once the call has ended; `opened` true. OPEN_VIEW is PROVEN in a project and has never run in a family |
| **EM2** | `revit_change` capability `SET_VIEW_PROPERTIES`, `views=View 1`, `visualStyle=Shaded with Edges`, `detailLevel=Fine`, `scale=0` | View 1 shaded with edges and at Fine - the small cylinder now shows; the style and detail read back; the scale untouched. **Write down the style View 1 had BEFORE** - which one a family template starts in is not known |
| **EM3** | Pan and zoom View 1 by hand so half the family is off the screen. **Through the proving client, rolled back** - a DRAFT is refused by `revit_change` (D-117), and a view change survives a rolled-back group (D-115): `validate zoom-to-elements --write`, `--setup select-family-forms --setup-set forms=solid --keep-chain`, `--set alsoSelect=false` | The whole family on screen, nothing cut off at an edge; `viewAfter` "View 1"; `shown` equals the select's count (3); the selection untouched; the binding note names `editScopeFailures` and the elements as from select-family-forms. **The D-30 positive** for version 2 |
| **EM4** | EM3's negative, two parts. (a) The proving client running `zoom-to-elements` **without** `--write`. (b) Ref. Level in front instead of View 1, then EM3's run | (a) Refused before anything runs - the add-in's words that it runs only as a change (`needs_write_path`) - and the screen does not move. (b) `viewAfter` names Ref. Level or a view Revit chose, and it is NOT View 1: this tool opens no view by name, which is why the method opens it first |
| **EM5** | A scratch PROJECT with three walls on Level 2 and the Level 1 plan in front, through the proving client as EM3: `validate zoom-to-elements --write`, `--setup filter-elements-by-category --setup-set category=Walls --setup-set levelId=` Level 2's id `--keep-chain`, `--set alsoSelect=false` | The screen moves to them and `viewAfter` names the view Revit chose - version 1's own first case, with no transaction open. Through `revit_change` version 1 ran inside Heron's transaction; this is the case most likely to show the difference |
| **EM6** | Back in the family, View 1 zoomed as EM3 left it: `revit_change` capability `EXPORT_VIEW_IMAGE`, `views=View 1, Front`, `folder=%TEMP%\heron-work\look-ej` (typed in full), `filePrefix=Heron Look`, `pixelWidth=1600` | `exported` 2; two PNG files in that folder, each 1600 px wide; View 1 shaded with edges with the small cylinder in it, Front showing the body with the bracket behind it. Open both. **The positive for EXPORT_VIEW_IMAGE in a family** |
| **EM7** | EM6's negative: the same call with Publish switched **off** | Refused naming the Publish switch; no file written - the look the method says to report as not done |
| **EM8** | *An observation.* Pan View 1 so half the family is off the screen, then EM6 into a second folder | Whether the picture shows the whole family anyway. A view exported as a set is drawn to fit by its options, so it probably does - but nobody has measured it. Write down which: it says whether the zoom in step 13 is for the picture or only for the modeller's screen |
| **EM9** | EM3 and EM6 once on Revit 2024 and once on 2027 | The same answers |
| **EM10** | Second route: Zoom to Fit (ZF) in View 1 by hand, then File > Export > Images and Animations > Image of View 1 by hand | The same framing as EM3, and an image like EM6's |
| **EM11** | *Watching, not a proof - once EM3 and EM4 are signed.* A chat builds a small fixed family - six or so forms - with one part placed on the WRONG side on purpose (the bracket in front of the body), then reaches step 13 | It opens View 1, zooms, exports, opens the picture, and says the bracket is on the wrong side BEFORE telling the modeller it is done. With Publish off it says the look was not done and asks the modeller to look at View 1. It never offers to send the picture anywhere |
| **EM12** | **Before version 2 is signed**: `revit_change` capability `ZOOM_TO_ELEMENTS`, anything at all | Refused as DRAFT before anything is sent (D-117), naming the capability - the step 13 fallback: the chat says so and asks for ZF |
