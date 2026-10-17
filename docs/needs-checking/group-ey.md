# Needs checking — Group EY

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EY - the family method looks at what it built: `revit_show` and `revit_view_image` in a family, and `family-creation` step 13 (2026-10-17)

Found on 2026-10-14 building two fixed monitor families on Revit 2020. Both were checked by
`REPORT_FAMILY_FORMS`, every size as planned, and then by a desktop screen grab of the Revit window taken
outside Heron - which showed the 55 inch family half off the screen, because the 3D view had never been
zoomed. A part on the wrong side, a missing part or one floating off the body reads back with the right
sizes. [Row 5b-591](../fragment-issues/section-5b-rows-401-425.md) has the whole of it.

**What changed.** The method only - **no fragment and nothing in the add-in.** `family-creation` step 13
now opens "View 1" and zooms it to every solid form through `revit_show` - the door that runs a capability
with no transaction open, merged the same day ([Group ES](group-es.md), [D-125](../DECISIONS.md)) - shades
it at Fine with `SET_VIEW_PROPERTIES` through `revit_change`, and takes its picture with `revit_view_image`
(and of "Front") - the read-only tool [D-123](../DECISIONS.md) made for exactly this - and the agent looks
at it in the chat before telling the modeller it is done. `ZOOM_TO_ELEMENTS` and `OPEN_VIEW` are PROVEN in a
project and have never run in a family; [Group ES](group-es.md) checks the show door itself and
[Group ER](group-er.md) the picture tool, both in a project. The rows here check them in a family.

**What is compiled and what is not run.** Nothing here is new code. **None of it has run in Revit.**
`revit_show` works only once the add-in is rebuilt with it, deployed for each Revit, Revit restarted and
the Claude app restarted, as Group ES says; before that, step 13 asks the modeller for ZF (EY11).

**ONE SCRATCH FAMILY, never a working model.** Make it before EY1, in Revit 2020 first: a family from
*Metric Generic Model.rft* saved as *Heron Look Scratch.rfa* under `%TEMP%\heron-work`, with a box body
400 x 100 x 300 mm on Ref. Level, a bracket box 200 x 60 x 200 mm against its back, and one small cylinder
on its front set to show at Fine only (`CREATE_FAMILY_FORMS`, `detailLevels=fine` on its row). Leave
"Ref. Level" (the floor plan) in front. Changes switched on in Revit's ribbon for EY2; Publish is not needed.

| # | Run | Look for |
|---|---|---|
| **EY1** | `revit_show` capability `OPEN_VIEW`, `viewName=View 1`, with Ref. Level in front | View 1 on screen once the call has ended; `opened` true; no new entry in Revit's Undo list. OPEN_VIEW is PROVEN in a project and has never run in a family, nor through `revit_show` |
| **EY2** | `revit_change` capability `SET_VIEW_PROPERTIES`, `views=View 1`, `visualStyle=Shaded with Edges`, `detailLevel=Fine`, `scale=0`; if Revit asks Keep or Discard ([D-126](../DECISIONS.md)), Keep | View 1 shaded with edges and at Fine - the small cylinder now shows; the style and detail read back; the scale untouched. **Write down the style View 1 had BEFORE** - which one a family template starts in is not known |
| **EY3** | **The D-30 positive for the zoom in a family.** Pan and zoom View 1 by hand so half the family is off the screen. `revit_read` capability `SELECT_FAMILY_FORMS`, `forms=solid`; then at once `revit_show` capability `ZOOM_TO_ELEMENTS`, `expect_from=select-family-forms`, `alsoSelect=false` | The whole family on screen, nothing cut off at an edge; `viewAfter` "View 1"; `shown` equals the select's count (3); nothing selected; no new entry in Revit's Undo list |
| **EY4** | **The negative**, two parts. (a) Ref. Level in front instead of View 1, then EY3's two calls. (b) `revit_show` capability `SET_VIEW_PROPERTIES` with EY2's values | (a) `viewAfter` names Ref. Level or a view Revit chose, and it is NOT View 1: the zoom opens no view by name, which is why step 13 opens it first. (b) Refused before anything is sent - it is MODIFY, and `revit_show` runs nothing above EXECUTE - and View 1's style is as EY2 left it |
| **EY5** | Back in View 1, zoomed as EY3 left it: `revit_view_image` with `view` "View 1", then with `view` "Front" | Two pictures in the chat: View 1 shaded with edges with the small cylinder in it, Front showing the body with the bracket behind it; nothing changed in the family - no new view in the Project Browser, View 1's style as EY2 left it. **The positive for `revit_view_image` in a family** |
| **EY6** | EY5's negative: `revit_view_image` with `view` "View 9", then with `view` "Ref. Level" | "View 9": refused, no such view, nothing pictured. "Ref. Level", where the template has both a floor plan and a ceiling plan of that name - refused as ambiguous, each written as "ViewType: Name" to type back; nothing pictured |
| **EY7** | *An observation.* Pan View 1 so half the family is off the screen, then `revit_view_image` with `view` "View 1" | Whether the picture shows the whole family anyway. Write down which: it says whether the zoom in step 13 is for the picture or only for the modeller's screen |
| **EY8** | EY3 and EY5 once on Revit 2024 and once on 2027 | The same answers |
| **EY9** | Second route: Zoom to Fit (ZF) in View 1 by hand, and look at the screen | The same framing as EY3, and what EY5's picture showed |
| **EY10** | *Watching, not a proof - once EY3 and EY5 have passed.* A chat builds a small fixed family - six or so forms - with one part placed on the WRONG side on purpose (the bracket in front of the body), then reaches step 13 | It opens View 1, shades it, zooms, takes the picture with `revit_view_image`, and says the bracket is on the wrong side BEFORE telling the modeller it is done. It never uses `EXPORT_VIEW_IMAGE` for the look and never offers to send the picture anywhere |
| **EY11** | *Watching, not a proof.* EY10's chat on a PC whose add-in was built before `revit_show` | The zoom is refused or `revit_show` is not offered; the chat asks the modeller to open View 1 and press ZF, and goes on to the picture |
