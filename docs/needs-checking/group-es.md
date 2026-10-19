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
**no transaction open** - so Revit refuses a change to the model, and the add-in counts the model around
the run ([row 5b-350](../fragment-issues/section-5b-rows-326-350.md)) - and what an EXECUTE
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
| ~~**ES1**~~ | **Selection and zoom work with Changes OFF.** In a chat: `revit_read` capability `SELECT_BY_CATEGORY_NAME` with `categoryName=Ducts`, then `revit_show` capability `SET_SELECTION` with `expect_from=select-by-category-name`; then `revit_show` capability `ZOOM_TO_ELEMENTS` with `alsoSelect=false` and the same `expect_from` | The ducts highlighted, then the camera on them - neither refused for Changes; the banner reads *Showing on screen: set-selection*; Revit's Undo list gains **no** entry; the model's element count before and after the same. **The D-30 positive for the door** - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below. |
| **ES2** | **A MODIFY fragment sent through the show door is refused, twice over.** (a) `revit_show` capability `HIDE_ELEMENTS` (MODIFY) with `view=` the plan in front and `permanent=true`. (b) The add-in's own guarantee, from a Python prompt in the Heron folder with the ducts still selected: `heron_bridge_client.discover()`, then on the one bridge `request("run_fragment_show", ...)` with that fragment's `name`, `source`, `needs` and the same `values` - the client's checks skipped on purpose | (a) refused BEFORE anything is sent: *"is declared MODIFY, and revit_show runs nothing above EXECUTE"*. (b) Revit's own refusal of a change made outside a transaction, in its words, and every selected duct STILL SHOWN in the plan, with no Undo entry. **The D-30 negative**: the guarantee is Revit's, not the client's |
| **ES3** | **Whether the temporary isolate lands in Revit's undo list.** `revit_isolate_temporary` with `category=ducts` in a plan; open Revit's Undo drop-down; then Ctrl+Z once; then `revit_reset_temporary` | Write down what the Undo drop-down shows - an entry *Heron: isolate Ducts temporarily*, or none - and whether Ctrl+Z ends the isolate. Either is an answer; it is the one thing `RevitTemporaryView.cs` says it does not know. Then the reset: the coloured border gone, and the reply *is off* |
| **ES4** | The isolate's edges: `revit_isolate_temporary` with `ids=` two duct ids from ES1's answer - one in this plan, one on another level; then with both `ids` and `category`; then on a sheet in front; then `revit_reset_temporary` twice | *Only 1 of the 2 are in this view at all*; both at once refused (*two different requests*) with nothing isolated; the sheet refused by name (*no Temporary Hide/Isolate*); the second reset *nothing to reset* and no Undo entry for it |
| **ES5** | `python tools/batch-prove.py tools/jobs/zoom-to-linked-elements-2026-10-17.yaml` on Revit 2024, `{3D}` in front with the structural link visible. Before it, the same in a chat: `revit_show` capability `ZOOM_TO_LINKED_ELEMENTS`, `linkName=Structural`, `categories=Structural Framing`, `alsoSelect=true` | It runs in that one call, with no question first (D-124). The camera on the structural beams; `selected` equal to `shown`, and Properties naming the link; `shown` equal to SELECT_FROM_LINK's `elements` for the same link and category. The negative (architectural link, Ducts): `shown` 0, nothing zoomed, the selection untouched |
| **ES6** | **Linked zoom on 2020 and on 2023 or later.** ES5's chat run once on Revit 2020 and once on 2023 (or 2027) | On 2020: the same camera move, `selected` 0 and the finding *this release of Revit cannot select inside a link from code* - nothing thrown. On 2023 and later: selected as in ES5. The reflection lookup is the one line that differs between them |
| **ES7** | A link placed with a rotation: in a scratch host, link any model with Positioning *Auto - Origin to Origin*, then rotate the link instance 30 degrees about the origin; ES5's call on one of its categories | The camera lands where the elements are DRAWN in the host, not where they would be unrotated - the case the eight transformed corners exist for |
| **ES8** | Second route: Tab onto one of ES5's beams by hand, then Revit's own Zoom to Fit on the selection | The view arrives where `ZOOM_TO_LINKED_ELEMENTS` put it, and Properties names the same link |
| ~~**ES9**~~ | **`OPEN_VIEW` with Changes OFF (D-125).** After the add-in is rebuilt and deployed: `revit_show` capability `OPEN_VIEW` with `viewName=` a plan that is not on screen, then the same with a view template's name | The plan opens and is the view on screen, with no Undo entry and Changes still off; the template is refused by name, nothing opened - **the negative**. Until 2026-10-17 the add-in refused it on this door (*runs only as a change*) because it declared `editScopeFailures` ([row 5b-498](../fragment-issues/section-5b-rows-401-425.md)) - **Struck 2026-10-18:** PASS on Revit 2024, 2026-10-18 - the results table under *The 2026-10-18 run* below. |
| **ES10** | *Not a proof - an observation.* Two projects open, the chat pinned to the one BEHIND: ES1's `ZOOM_TO_ELEMENTS` through `revit_show` | What Revit does with a selection and a Show aimed at a document that is not in front - brings it to the front, shows nothing, or refuses - and that the answer says *is not the model in front*. The executor builds a UIDocument for a document behind; nothing has measured what Show does with one |
| **ES11** | *Not a proof - an observation.* `revit_show` capability `ISOLATE_ELEMENTS` on ES1's ducts, then `revit_change` with the same, Changes ON | The first refused BEFORE anything is sent - it is MODIFY since D-125 - and the refusal names `revit_isolate_temporary`; the second isolates as it always did, one Undo entry ([row 5b-499](../fragment-issues/section-5b-rows-401-425.md)) |
| **ES12** | *Not a proof - on the owner's PC, with the re-ranker installed.* `python tests/test_routing_phrases.py` | PASSED. It passed on 2026-10-17 on the trained model with NO re-ranker, after [row 5b-501](../fragment-issues/section-5b-rows-401-425.md)'s repairs; whether the re-ranker keeps all four sentences where they now land is not measured |
| **ES13** | *Not a proof - an observation, in a NEW chat.* With Changes OFF and no Heron tool searched for yet, ask *"zoom to the beams in the structural link"* | The chat sends it to `revit_change` (`revit_show` is not given before it asks), is refused - not proven - by a sentence naming `revit_show`, and then finds and calls `revit_show` itself, with nothing changed in the model. If it stops at the refusal instead, the pointer is not enough and `revit_show` belongs in `ALWAYS_LOADED`, which is D-118's budget to decide |
| **ES14** | **Whether a temporary isolate marks the model changed.** Save the model; `revit_isolate_temporary` with `category=ducts`; then `revit_reset_temporary`; then close the model | No question to save on close - a temporary isolate is a display state, and Revit's own button asks none. If Revit DOES ask, the transaction `RevitTemporaryView` commits marks the model modified, and "changes nothing in the model" in the tool's words is not the whole truth: record it in [row 5b-593](../fragment-issues/section-5b-rows-401-425.md) and say it in the answer |
| **ES15** | *Not a proof - an observation, on a workshared model.* A plan view another user has borrowed (Worksharing Display shows it owned by them); `revit_isolate_temporary` with `category=ducts` there | Either the isolate lands, as Revit's own button does without borrowing, or Revit refuses and the answer quotes Revit's words with `isolate_failed`, the view untouched. Which it is was not known when this was written ([row 5b-593](../fragment-issues/section-5b-rows-401-425.md)) |
| **ES16** | Revit 2023 or later: ES5's job with `alsoSelect=true` while the structural link is HIDDEN in `{3D}` (Visibility/Graphics, Revit Links) | The camera still moves; if Revit refuses to select a hidden linked element, a finding says *"The selection was NOT changed: Revit would not select them..."* and quotes Revit - the run does not end in an exception ([row 5b-593](../fragment-issues/section-5b-rows-401-425.md)) |

### The 2026-10-18 run

**On the owner's laptop, 2026-10-18.** Revit 2024.3 session 63712, and for the rows marked 2027 Revit 2027.3
session 22024, both on add-in `0.1.0+48230c8c` - `main`, Debug, deployed 08:45 local by this run with
`tools/deploy-addin.ps1` for 2024, 2026 and 2027. Revit 2020 session 55496 belonged to another chat and ran
`ab77d5fb`; **nothing here ran on Revit 2020.** Models, all scratch copies in `%TEMP%\heron-work\safety-1018\models`:
*Heron Safety 2024* (Autodesk's `rme_basic_sample_project.rvt`, via the move-rails chat's *Rails-X2*; 21,562
elements at open), *Heron Guard B 2024* (a copy of *Rails-S*, the Dynamo sample) and *Heron Safety 2027* (Revit
2027's `Snowdon Towers Sample HVAC.rvt`, links unresolved; 9,813 elements). Changes, Admin and Publish on except
where a row says. **The owner pressed every Keep, Discard, Esc, X and Undo himself;** nothing answered a box for
him. Undo lists were read with UI Automation, read-only. Throwaway probe fragments were made in `brain/fragments/`
for the session and deleted by name at the end - none is committed.

The owner turned **Changes, Admin and Publish off** on Revit 2024's ribbon at 06:36:57-06:37:03 and back on at
06:42:28-06:42:33 (the add-in log's lines). **Seen beside it:** this session's shell and Heron server read the
Claude app's private copy of `heron.config`, which still said `true` throughout; only Revit read the real one.
*Heron Safety 2024* has no placed links, so ES5 to ES8 and ES16 could not be arranged on it.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| ES1 | 2024 | `SELECT_BY_CATEGORY_NAME` Ducts in *Level 1 HVAC Plan* (256) → `revit_show SET_SELECTION` (256 selected) → `revit_show ZOOM_TO_ELEMENTS` `alsoSelect false` with `expect_from=set-selection` (256 shown). Neither refused for Changes; 21,573 elements before and after; the Undo list unchanged. With `expect_from=select-by-category-name` the zoom was refused - the chain had moved on to set-selection's | **PASS** |
| ES2 | 2024 | (a) `revit_show HIDE_ELEMENTS`: *"is declared MODIFY, and revit_show runs nothing above EXECUTE ... Nothing has been sent to Revit."* (b) The raw `run_fragment_show` from a script, the client's check skipped: **refused by the session's auto-mode safety check before it ran** | (a) **PASS**, (b) **NOT RUN** |
| ES3 | 2024 | `revit_isolate_temporary` ducts: **the Undo list gains `Heron: isolate Ducts temporarily`**, with Changes off. His Ctrl+Z left the Undo list as it was and the view isolated - the key press undid nothing, so whether Ctrl+Z ends it is not answered. `revit_reset_temporary`: *"is off"*. Each later isolate AND each reset also added an entry (`Heron: isolate 2 element(s) temporarily`, `Heron: reset temporary hide/isolate`) | answered: **it is in the Undo list**; Ctrl+Z **inconclusive** |
| ES4 | 2024 | `ids` 392168 (Level 1) and 419031 (Level 2): *"Only 1 of the 2 are in this view at all"*. Both `ids` and `category`: *"two different requests ... NOTHING was isolated"*. The second reset: *"nothing to reset and nothing was done"*. The sheet half not run | **PASS** |
| ES9 | 2024 | `revit_show OPEN_VIEW` *Level 2 HVAC Plan*: opened, Changes off. *Mechanical Plan* (a template): `opened false`, *"[view template, cannot be opened]"* | **PASS** |
| ES11 | 2024 | `revit_show ISOLATE_ELEMENTS`: refused before sending, naming `revit_isolate_temporary` | **PASS** |
| ES5-ES8, ES10, ES12-ES16 | - | No links; the rest not arranged | **NOT RUN** |

With the switches off, a `create-duct` preview from the command line came back `write_disabled` and
`save-document` `publish_disabled` - the gates held where the server's own copy of the switches said otherwise.
