# Needs checking — Group ES

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group ES - "show me / zoom to / isolate these" with Changes OFF: `revit_show`, the temporary isolate, and `ZOOM_TO_LINKED_ELEMENTS` (2026-10-17)

**What was wrong.** The add-in has always let EXECUTE through with Changes off -
`HeronPermissions.ReadOnlyCeiling` is Execute, *"changes what is shown, never what exists"* - and only the
MCP side held it back: `revit_read`'s ceiling is its own ANALYZE, so `ZOOM_TO_ELEMENTS`, `SET_SELECTION`
and every other EXECUTE fragment could reach Revit from a chat only through `revit_change`, with Changes
ON, inside a transaction. A request that changes only the screen needed write permission.
[Row 5b-497](../fragment-issues/section-5b-rows-401-425.md).

**What changed.** `revit_show` (EXECUTE) sends `run_fragment_show`, declared EXECUTE in
`HeronOperationRegistry.cs` and routed to `RevitFragment.Run(app, request)` - the read path's overload,
**no transaction open** - so Revit itself still refuses any change to the model, and what an EXECUTE
fragment can change is the selection, the camera and the active view. `heron_tools.door_refusal` refuses
anything above EXECUTE before it is sent. Revit's Temporary Hide/Isolate needs a transaction, so it is
**not** a fragment: `revit_isolate_temporary` and `revit_reset_temporary` call the fixed operations
`isolate_temporary` and `reset_temporary` in `RevitTemporaryView.cs`, which open one transaction round
`IsolateElementsTemporary`, `IsolateCategoriesTemporary` or `DisableTemporaryViewMode` and nothing else.
`ZOOM_TO_LINKED_ELEMENTS` (FRG-SEL-039, DRAFT) zooms the active view to elements inside a loaded link,
their boxes put through the link's transform, and selects them with `Selection.SetReferences` - looked up
by reflection, because it arrived at Revit 2023. It asks for no confirmation first: a selection or a zoom
inside a link is not touching it ([D-124](../DECISIONS.md), the owner's answer to Q-65).

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027 and every
fragment compiles on each (`tools/check-compile.py`, `tools/check-fragments-compile.py`, 2026-10-17).
`tests/Heron.Kernel.TestHost` runs the gate over every switch combination; `tests/test_read_door.py` holds
the show door's ceiling over every fragment in the library; `tests/test_mcp_serves.py` calls all three
tools through a real SDK against a stand-in Revit. **None of it has run in Revit.** It works only once the
add-in is rebuilt, deployed for each Revit, Revit restarted and the Claude app restarted.

**A SCRATCH COPY, never a working model.** ES1 to ES4 on any project with ducts in a plan view - a copy of
*Snowdon Towers Sample HVAC* serves. ES5 to ES8 on the copy SELECT_FROM_LINK was proved on,
*Snowdon-scratch_ajmal.al*, with its six links loaded: [the job file](../../tools/jobs/zoom-to-linked-elements-2026-10-17.yaml).
**Changes OFF in Revit's Heron ribbon for every row**, and Admin and Publish off.

| # | Run | Look for |
|---|---|---|
| **ES1** | **Selection and zoom work with Changes OFF.** In a chat: `revit_read` capability `SELECT_BY_CATEGORY_NAME` with `categoryName=Ducts`, then `revit_show` capability `SET_SELECTION` with `expect_from=select-by-category-name`; then `revit_show` capability `ZOOM_TO_ELEMENTS` with `alsoSelect=false` and the same `expect_from` | The ducts highlighted, then the camera on them - neither refused for Changes; the banner reads *Showing on screen: set-selection*; Revit's Undo list gains **no** entry; the model's element count before and after the same. **The D-30 positive for the door** |
| **ES2** | **A MODIFY fragment sent through the show door is refused, twice over.** (a) `revit_show` capability `HIDE_ELEMENTS` (MODIFY) with `view=` the plan in front and `permanent=true`. (b) The add-in's own guarantee, from a Python prompt in the Heron folder with the ducts still selected: `heron_bridge_client.discover()`, then on the one bridge `request("run_fragment_show", ...)` with that fragment's `name`, `source`, `needs` and the same `values` - the client's checks skipped on purpose | (a) refused BEFORE anything is sent: *"is declared MODIFY, and revit_show runs nothing above EXECUTE"*. (b) Revit's own refusal of a change made outside a transaction, in its words, and every selected duct STILL SHOWN in the plan, with no Undo entry. **The D-30 negative**: the guarantee is Revit's, not the client's |
| **ES3** | **Whether the temporary isolate lands in Revit's undo list.** `revit_isolate_temporary` with `category=ducts` in a plan; open Revit's Undo drop-down; then Ctrl+Z once; then `revit_reset_temporary` | Write down what the Undo drop-down shows - an entry *Heron: isolate Ducts temporarily*, or none - and whether Ctrl+Z ends the isolate. Either is an answer; it is the one thing `RevitTemporaryView.cs` says it does not know. Then the reset: the coloured border gone, and the reply *is off* |
| **ES4** | The isolate's edges: `revit_isolate_temporary` with `ids=` two duct ids from ES1's answer - one in this plan, one on another level; then with both `ids` and `category`; then on a sheet in front; then `revit_reset_temporary` twice | *Only 1 of the 2 are in this view at all*; both at once refused (*two different requests*) with nothing isolated; the sheet refused by name (*no Temporary Hide/Isolate*); the second reset *nothing to reset* and no Undo entry for it |
| **ES5** | `python tools/batch-prove.py tools/jobs/zoom-to-linked-elements-2026-10-17.yaml` on Revit 2024, `{3D}` in front with the structural link visible. Before it, the same in a chat: `revit_show` capability `ZOOM_TO_LINKED_ELEMENTS`, `linkName=Structural`, `categories=Structural Framing`, `alsoSelect=true` | It runs in that one call, with no question first (D-124). The camera on the structural beams; `selected` equal to `shown`, and Properties naming the link; `shown` equal to SELECT_FROM_LINK's `elements` for the same link and category. The negative (architectural link, Ducts): `shown` 0, nothing zoomed, the selection untouched |
| **ES6** | **Linked zoom on 2020 and on 2023 or later.** ES5's chat run once on Revit 2020 and once on 2023 (or 2027) | On 2020: the same camera move, `selected` 0 and the finding *this release of Revit cannot select inside a link from code* - nothing thrown. On 2023 and later: selected as in ES5. The reflection lookup is the one line that differs between them |
| **ES7** | A link placed with a rotation: in a scratch host, link any model with Positioning *Auto - Origin to Origin*, then rotate the link instance 30 degrees about the origin; ES5's call on one of its categories | The camera lands where the elements are DRAWN in the host, not where they would be unrotated - the case the eight transformed corners exist for |
| **ES8** | Second route: Tab onto one of ES5's beams by hand, then Revit's own Zoom to Fit on the selection | The view arrives where `ZOOM_TO_LINKED_ELEMENTS` put it, and Properties names the same link |
| **ES9** | **`OPEN_VIEW` with Changes OFF (D-125).** After the add-in is rebuilt and deployed: `revit_show` capability `OPEN_VIEW` with `viewName=` a plan that is not on screen, then the same with a view template's name | The plan opens and is the view on screen, with no Undo entry and Changes still off; the template is refused by name, nothing opened - **the negative**. Until 2026-10-17 the add-in refused it on this door (*runs only as a change*) because it declared `editScopeFailures` ([row 5b-498](../fragment-issues/section-5b-rows-401-425.md)) |
| **ES10** | *Not a proof - an observation.* Two projects open, the chat pinned to the one BEHIND: ES1's `ZOOM_TO_ELEMENTS` through `revit_show` | What Revit does with a selection and a Show aimed at a document that is not in front - brings it to the front, shows nothing, or refuses - and that the answer says *is not the model in front*. The executor builds a UIDocument for a document behind; nothing has measured what Show does with one |
| **ES11** | *Not a proof - an observation.* `revit_show` capability `ISOLATE_ELEMENTS` on ES1's ducts, then `revit_change` with the same, Changes ON | The first refused BEFORE anything is sent - it is MODIFY since D-125 - and the refusal names `revit_isolate_temporary`; the second isolates as it always did, one Undo entry ([row 5b-499](../fragment-issues/section-5b-rows-401-425.md)) |
| **ES12** | *Not a proof - on the owner's PC, with the re-ranker installed.* `python tests/test_routing_phrases.py` | PASSED. It passed on 2026-10-17 on the trained model with NO re-ranker, after [row 5b-501](../fragment-issues/section-5b-rows-401-425.md)'s repairs; whether the re-ranker keeps all four sentences where they now land is not measured |
| **ES13** | *Not a proof - an observation, in a NEW chat.* With Changes OFF and no Heron tool searched for yet, ask *"zoom to the beams in the structural link"* | The chat sends it to `revit_change` (`revit_show` is not given before it asks), is refused - not proven - by a sentence naming `revit_show`, and then finds and calls `revit_show` itself, with nothing changed in the model. If it stops at the refusal instead, the pointer is not enough and `revit_show` belongs in `ALWAYS_LOADED`, which is D-118's budget to decide |
