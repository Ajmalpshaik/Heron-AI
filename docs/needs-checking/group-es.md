# Needs checking — Group ES

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group ES - the family method looks at what it built: `ZOOM_TO_ELEMENTS` version 2, `revit_view_image` in a family, and `family-creation` step 13 (2026-10-17)

Found on 2026-10-14 building two fixed monitor families on Revit 2020. Both were checked by
`REPORT_FAMILY_FORMS`, every size as planned, and then by a desktop screen grab of the Revit window taken
outside Heron - which showed the 55 inch family half off the screen, because the 3D view had never been
zoomed. A part on the wrong side, a missing part or one floating off the body reads back with the right
sizes. [Row 5b-512](../fragment-issues/section-5b-rows-401-425.md) has the whole of it.

**What changed.**
[`ZOOM_TO_ELEMENTS`](../../brain/fragments/zoom-to-elements/fragment.yaml) (FRG-SEL-030, DRAFT, version 2)
declares the ambient `editScopeFailures` need, so the add-in runs it inside the job's group with **no
transaction open** ([D-115](../DECISIONS.md)): `revit_change` is the only door a chat has for it, and that
door ran version 1 inside Heron's transaction, where Revit changes no view (row 5b-456). When Show is
refused, Revit's words now come first and `shown` stays 0. No new input; nothing in the add-in changed.
`family-creation` step 13 now opens "View 1", shades it at Fine with `SET_VIEW_PROPERTIES`, zooms it to
every solid form, and takes its picture with `revit_view_image` (and of "Front") - the read-only tool
[D-123](../DECISIONS.md) made for exactly this, merged the same day - and the agent looks at it in the chat
before telling the modeller it is done. [Group ER](group-er.md) checks that tool itself; the rows here
check it in a family, where it has never run either.

**What is compiled and what is not run.** The fragments compile on every release they claim
(`tools/check-fragments-compile.py`, 2026-10-17). **None of it has run in Revit.** `ZOOM_TO_ELEMENTS`'
`impl/` moved, so its 2026-09-13 proof is stale and marked so; ES3 and ES4 are the runs that replace it.
**Until version 2 is signed, a chat cannot run the zoom:** [D-117](../DECISIONS.md) refuses a DRAFT through `revit_change` before it is sent, so ES3 to ES5 run through the proving client, rolled back, and step 13 says to ask the modeller for ZF meanwhile.

**ONE SCRATCH FAMILY, never a working model.** Make it before ES1, in Revit 2020 first: a family from
*Metric Generic Model.rft* saved as *Heron Look Scratch.rfa* under `%TEMP%\heron-work`, with a box body
400 x 100 x 300 mm on Ref. Level, a bracket box 200 x 60 x 200 mm against its back, and one small cylinder
on its front set to show at Fine only (`CREATE_FAMILY_FORMS`, `detailLevels=fine` on its row). Leave
"Ref. Level" (the floor plan) in front. Changes switched on in Revit's ribbon; Publish is not needed.

| # | Run | Look for |
|---|---|---|
| **ES1** | `revit_change` capability `OPEN_VIEW`, `viewName=View 1`, with Ref. Level in front | View 1 on screen once the call has ended; `opened` true. OPEN_VIEW is PROVEN in a project and has never run in a family |
| **ES2** | `revit_change` capability `SET_VIEW_PROPERTIES`, `views=View 1`, `visualStyle=Shaded with Edges`, `detailLevel=Fine`, `scale=0` | View 1 shaded with edges and at Fine - the small cylinder now shows; the style and detail read back; the scale untouched. **Write down the style View 1 had BEFORE** - which one a family template starts in is not known |
| **ES3** | Pan and zoom View 1 by hand so half the family is off the screen. **Through the proving client, rolled back** - a DRAFT is refused by `revit_change` (D-117), and a view change survives a rolled-back group (D-115): `validate zoom-to-elements --write`, `--setup select-family-forms --setup-set forms=solid --keep-chain`, `--set alsoSelect=false` | The whole family on screen, nothing cut off at an edge; `viewAfter` "View 1"; `shown` equals the select's count (3); the selection untouched; the binding note names `editScopeFailures` and the elements as from select-family-forms. **The D-30 positive** for version 2 |
| **ES4** | ES3's negative, two parts. (a) The proving client running `zoom-to-elements` **without** `--write`. (b) Ref. Level in front instead of View 1, then ES3's run | (a) Refused before anything runs - the add-in's words that it runs only as a change (`needs_write_path`) - and the screen does not move. (b) `viewAfter` names Ref. Level or a view Revit chose, and it is NOT View 1: this tool opens no view by name, which is why the method opens it first |
| **ES5** | A scratch PROJECT with three walls on Level 2 and the Level 1 plan in front, through the proving client as ES3: `validate zoom-to-elements --write`, `--setup filter-elements-by-category --setup-set category=Walls --setup-set levelId=` Level 2's id `--keep-chain`, `--set alsoSelect=false` | The screen moves to them and `viewAfter` names the view Revit chose - version 1's own first case, with no transaction open. Through `revit_change` version 1 ran inside Heron's transaction; this is the case most likely to show the difference |
| **ES6** | Back in the family, View 1 zoomed as ES3 left it: `revit_view_image` with `view` "View 1", then with `view` "Front" | Two pictures in the chat: View 1 shaded with edges with the small cylinder in it, Front showing the body with the bracket behind it; nothing changed in the family - no new view in the Project Browser, View 1's style as ES2 left it. **The positive for `revit_view_image` in a family** |
| **ES7** | ES6's negative: `revit_view_image` with `view` "View 9", then with `view` "Ref. Level" | "View 9": refused, no such view, nothing pictured. "Ref. Level", where the template has both a floor plan and a ceiling plan of that name - refused as ambiguous, each written as "ViewType: Name" to type back; nothing pictured |
| **ES8** | *An observation.* Pan View 1 so half the family is off the screen, then `revit_view_image` with `view` "View 1" | Whether the picture shows the whole family anyway. Write down which: it says whether the zoom in step 13 is for the picture or only for the modeller's screen |
| **ES9** | ES3 and ES6 once on Revit 2024 and once on 2027 | The same answers |
| **ES10** | Second route: Zoom to Fit (ZF) in View 1 by hand, and look at the screen | The same framing as ES3, and what ES6's picture showed |
| **ES11** | *Watching, not a proof - once ES3 and ES4 are signed.* A chat builds a small fixed family - six or so forms - with one part placed on the WRONG side on purpose (the bracket in front of the body), then reaches step 13 | It opens View 1, shades it, zooms, takes the picture with `revit_view_image`, and says the bracket is on the wrong side BEFORE telling the modeller it is done. It never uses `EXPORT_VIEW_IMAGE` for the look and never offers to send the picture anywhere |
| **ES12** | **Before version 2 is signed**: `revit_change` capability `ZOOM_TO_ELEMENTS`, anything at all | Refused as DRAFT before anything is sent (D-117), naming the capability - the step 13 fallback: the chat says so and asks for ZF |
