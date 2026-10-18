# Needs checking — Group ER

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group ER - a picture of a view, handed to the chat: `capture_view` and `revit_view_image` (2026-10-17)

Asked for by the owner on 2026-10-17: a read-only tool that returns a PNG of a Revit view **into the
chat**, so the AI can check its own work. [D-123](../DECISIONS.md#d-123--a-picture-of-a-view-taken-for-the-chat-and-deleted-is-analyze-and-not-an-export)
is the decision, **Accepted** 2026-10-17: the owner answered that a picture is not model data under
D-82 - *"A picture is not model data, ship it"*.

**What changed.** A new add-in operation, `capture_view`
([`RevitCapture.cs`](../../revit/Heron.Revit.Addin/RevitCapture.cs)), declared ANALYZE, and the MCP tool
`revit_view_image` that sends it. Given a **view name** (empty: the view in front) it exports that view as
it stands. Given **element ids** it makes a temporary isometric 3D view - a section box round the
elements and a margin, non-model categories and datums hidden, levels, grids, base points and links
left out of the box - inside a TransactionGroup it **always** rolls back. The PNG is written into a
folder of its own under `HeronPaths.Captures`, read back, deleted, and sent in the reply as base64; the
chat receives it as an image block. Width 320-2400 px, 1280 by default.

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027
(`tools/check-compile.py`, 2026-10-17) and every Revit member it calls exists in each
(`tools/check-api-surface.py`, the same day). `tests/test_mcp_serves.py` calls the tool through the SDK's
own dispatch against a stand-in reply, on MCP SDK 2.2.0 and 1.30.0, and reads the image block back.
**None of it has run in Revit, and nothing here has been seen in Claude.** Every row below needs the
add-in rebuilt and deployed for that Revit, Revit restarted, and the Claude app restarted.

**A SCRATCH PROJECT, never a working model.** A project from the default template in Revit 2020 saved
as *Heron Capture Test.rvt*, with a floor plan *Level 1* holding a few walls and three ducts, and a
ceiling plan also called *Level 1*. Note the ducts' ids (Manage > IDs of Selection) and one level's id.

| # | Run | Look for |
|---|---|---|
| **ER1** | `revit_view_image` with `view` empty, *Level 1* floor plan in front | **The picture shows in Claude** - the chat describes the walls and ducts it sees, and the words name 'Level 1', FloorPlan and *Heron Capture Test*. The D-30 positive for a named view |
| **ER2** | Count the model's elements (`revit_use_this_model` answers with the count) and note Undo's list; then `revit_view_image` with `elements` = the three duct ids and the level's id; then count again and open Undo's list | The picture is of the three ducts in 3D, close up, with no level line or annotation in it; the words say 3 framed and the level *is a datum*. **The element count is the same before and after, and Undo's list has no new entry** |
| **ER3** | After ER2, the Project Browser's 3D Views | **No new 3D view** - the temporary view is gone. The words said *"The temporary view is gone - Revit rolled it back"*; if they said Revit did NOT confirm it, write that down here |
| **ER4** | `revit_view_image` with `view` = `Levle 1` (misspelt), then with `view` = `Level 1` | **The negative case.** The first is refused - *"There is no view called 'Levle 1'"*, no picture, nothing made. The second is refused as ambiguous, naming `CeilingPlan: Level 1` and `FloorPlan: Level 1`; typing one of those back pictures that view |
| **ER5** | `revit_view_image` with `elements` = an id that is not in the model, and with `elements` = a duct id and `view` = `Level 1` together | Each refused before anything is made - the first naming the id, the second saying one or the other. Element count and Undo unchanged |
| **ER6** | ER1, ER2 and ER3 again on **Revit 2027** | The same answers - **both ways work on 2020 and 2027** |
| **ER7** | After ER1 to ER6, the folder `HeronPaths.Captures` names (Heron's `captures` folder under Derived) | Empty, or not there: every capture deleted its own folder. A folder left in it is a capture that did not finish, and is safe to delete |
| **ER8** | `revit_view_image` with `width` = 9000 | The picture comes back 2400 px wide and the words say the width was clamped |
| **ER9** | *Watching, not a proof.* The activity banner over Revit during ER2 | It shows the reading colour and *"Taking a picture of a view for the chat"*, never a change |
| **ER10** | **The element picture after the fix for [row 5b-1090](../fragment-issues/section-5b-rows-401-425.md)** (added 2026-10-18). Until then every `elements` call answered `capture_failed` *"The referenced object is not valid ... or its creation was undone"* on 2024.3 and 2027.3: the reply read the temporary view's name and type after the rollback had taken the view away. `RevitCapture.cs` now reads both before the rollback and touches no element after it - compiled for 2020 to 2027 and checked as text by `tests/test_rollback_witness.py`, **never run in Revit**. Rebuild and deploy the add-in from that change for each release, restart Revit, then run **ER2**, **ER3** and **[EO9](group-eo.md)** on **Revit 2020, 2024 and 2027**, each on a scratch copy | On all three: ER2 gives the picture of the elements in 3D and the words *"A picture of N element(s) ... in a temporary 3D view"*, with the element count and Undo's list unchanged; ER3 finds no new 3D view and the words carry *"The temporary view is gone - Revit rolled it back"*; EO9 gives its picture and its words as written there. **One `capture_failed` on any release is a FAIL** - write down the release and its words here, and leave 5b-1090 open |

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

| # | Release | What was run and what came back | State |
|---|---|---|---|
| ER1 | 2024 | `view` empty, *Level 1 HVAC Plan* in front: the picture came into the chat - ducts, grids and rooms seen - and the words named the view, FloorPlan and the model | **PASS** |
| ER2 | 2024 | `elements` = 891701 (a kept duct), 891698, and 386031, 394114, 395978 (original equipment): every one `capture_failed` *"The referenced object is not valid ... or its creation was undone"*. The ids exist. The reply reads the temporary view's name after the rollback undid it - [5b-1090](../fragment-issues/section-5b-rows-401-425.md) | **FAIL** |
| ER3 | 2024 | No new 3D view (140 views, 11 3D, before and after), element count unchanged - but the closing words never came, ER2 failed first | **part** |
| ER4 | 2024 | `Levle 1 HVAC Plan`: *"There is no view called 'Levle 1 HVAC Plan'"*, nothing pictured. The ambiguous half: no two views in this model share a name | **PASS**; ambiguous half **NOT RUN** |
| ER5 | 2024 | `999999999`: refused naming the id. A duct id and `view` together: *"Give a view name OR element ids, not both"*. Nothing made | **PASS** |
| ER6 | 2027 | `view` empty: the Cover Sheet in front pictured. `elements` = 1365188, 1366142: the same `capture_failed` | view **PASS**, elements **FAIL** |
| ER7 | 2024 | `%LOCALAPPDATA%\Heron\captures` empty, 0 files | **PASS** |
| ER8 | 2024 | `width` 9000: a 2400 px picture and *"The width asked for was outside 320 to 2400 px, so 2400 was used."* | **PASS** |
| ER9 | - | The banner was not watched | **NOT RUN** |
