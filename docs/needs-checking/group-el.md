# Needs checking — Group EJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EJ - a picture of a view, handed to the chat: `capture_view` and `revit_view_image` (2026-10-17)

Asked for by the owner on 2026-10-17: a read-only tool that returns a PNG of a Revit view **into the
chat**, so the AI can check its own work. [D-116](../DECISIONS.md#d-116--a-picture-of-a-view-taken-for-the-chat-and-deleted-is-analyze-and-not-an-export)
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
| **EJ1** | `revit_view_image` with `view` empty, *Level 1* floor plan in front | **The picture shows in Claude** - the chat describes the walls and ducts it sees, and the words name 'Level 1', FloorPlan and *Heron Capture Test*. The D-30 positive for a named view |
| **EJ2** | Count the model's elements (`revit_use_this_model` answers with the count) and note Undo's list; then `revit_view_image` with `elements` = the three duct ids and the level's id; then count again and open Undo's list | The picture is of the three ducts in 3D, close up, with no level line or annotation in it; the words say 3 framed and the level *is a datum*. **The element count is the same before and after, and Undo's list has no new entry** |
| **EJ3** | After EJ2, the Project Browser's 3D Views | **No new 3D view** - the temporary view is gone. The words said *"The temporary view is gone - Revit rolled it back"*; if they said Revit did NOT confirm it, write that down here |
| **EJ4** | `revit_view_image` with `view` = `Levle 1` (misspelt), then with `view` = `Level 1` | **The negative case.** The first is refused - *"There is no view called 'Levle 1'"*, no picture, nothing made. The second is refused as ambiguous, naming `CeilingPlan: Level 1` and `FloorPlan: Level 1`; typing one of those back pictures that view |
| **EJ5** | `revit_view_image` with `elements` = an id that is not in the model, and with `elements` = a duct id and `view` = `Level 1` together | Each refused before anything is made - the first naming the id, the second saying one or the other. Element count and Undo unchanged |
| **EJ6** | EJ1, EJ2 and EJ3 again on **Revit 2027** | The same answers - **both ways work on 2020 and 2027** |
| **EJ7** | After EJ1 to EJ6, the folder `HeronPaths.Captures` names (Heron's `captures` folder under Derived) | Empty, or not there: every capture deleted its own folder. A folder left in it is a capture that did not finish, and is safe to delete |
| **EJ8** | `revit_view_image` with `width` = 9000 | The picture comes back 2400 px wide and the words say the width was clamped |
| **EJ9** | *Watching, not a proof.* The activity banner over Revit during EJ2 | It shows the reading colour and *"Taking a picture of a view for the chat"*, never a change |
