# Needs checking — Group OD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OD - OPEN_FAMILY_FOR_EDITING reads what is in front, a picture of a region of a plan, and a store warning that names what to do (2026-11-16)

From Sruthina's field reports Ajmalpshaik/Heron-AI#853 (Revit 2024, `villa 2`) and Group NY's run of
2026-11-16. Three repairs made in a cloud container with no Revit - every project compiled on 2020 to
2027, every fragment compiled on every release it claims, the server's halves tested - and **none of it
has run in Revit**:

- [Row 5b-1820](../fragment-issues/row-5b-1820.md), repairing row 5b-1750 (on PR #884): **OPEN_FAMILY_FOR_EDITING
  version 3** - after `OpenAndActivateDocument` throws, the document in front is READ, and counts as
  opened only when it is the .rfa just written. **PROVEN -> DRAFT.**
- [Row 5b-1710](../fragment-issues/row-5b-1710.md): **`revit_view_image` takes `around`** - a plan
  pictured cropped to element ids or to a box in mm, from a temporary copy of the plan the always
  rolled-back group takes away. Add-in (`RevitCapture.cs`) and server. No card.
- [Row 5b-1711](../fragment-issues/row-5b-1711.md): **the store-drift warning names what reaches it**
  from where it is read, and **`tools/heron-ready.py` rebuilds a drifted store** it may build. Python
  only.

Version 3 was tightened after the edit checker's review: a window on the same path open BEFORE the run never counts (OD3's second half).

**OD1 to OD7 need the add-in rebuilt and redeployed, and Revit restarted. OD8 and OD9 need the server
restarted** (restart the Claude app). **Revit 2024 first**; the same rows on any other release are
evidence about that release only.

**The model is Heron's own scratch project, never a working model (OD0).** Every change ends with
**Discard** or Undo, except OD0's arranging.

| ID | Do this | Pass looks like |
|---|---|---|
| **OD0** | *Arrange, on Revit 2024.* Ask Heron to make a new project from `English\DefaultMetric.rte` with `CREATE_FAMILY_DOCUMENT` (as Group NK's NK0 did), and save it as `od-scratch.rvt` in `%TEMP%\heron-work\od`. In its floor plan `Level 1`, draw four walls round a 6000 x 4000 mm room with `CREATE_WALL` and **Keep**, and note their ids. Make the folders `C:\Users\Public\heron-work\od` and `C:\Users\Public\heron-work\od-2` (OD1 and OD3 save into them). Check `M_Single-Flush` is under Families > Doors - the template loads it | A saved scratch project with `M_Single-Flush` loaded, four walls in `Level 1`, the two folders made. Write down the four wall ids |
| **OD1** | *Row 5b-1820, positive - the route row 5b-1750 met.* The project in front, `revit_change OPEN_FAMILY_FOR_EDITING` with `familyName=M_Single-Flush`, `saveFolder=C:\Users\Public\heron-work\od` | The family opens in its own window, **in front** (Revit's title bar names it); `opened true`; `activeAfter` `M_Single-Flush`; `savedTo` the .rfa in that folder; **`problem` empty**, so the chat's verdict does **not** start *"STOPPED, BY ITS OWN ACCOUNT"*. **If Revit threw as on 2026-11-16**, findings carry *"Revit answered \"An internal error has occurred.\" while opening it, but the window in front IS ..."* - write down whether that line appeared: it is the read-back working. Close the family window without saving |
| **OD2** | *Row 5b-1820, negative.* The project in front, the same with `familyName=ZZZNOTAFAMILY` | `opened false`, *"No loaded family is called 'ZZZNOTAFAMILY'."*, nothing written in the folder, no window opened, `activeAfter` empty |
| **OD3** | *Row 5b-1820, negative - the read-back must go by FILE, never by name.* Repeat OD1 and leave that family window open. Click back to the project window, then run OD1 again with `saveFolder=C:\Users\Public\heron-work\od-2` | Either Revit opens the second file - `opened true`, `savedTo` under `od-2` - or it refuses, and then **`opened false` with Revit's own words as `problem`**, never `opened true` on the strength of the earlier window called `M_Single-Flush`. Write down which happened and Revit's words. Close both family windows without saving. **Then the same with the SAME folder:** OD1, leave its window open, click back to the project, run OD1 again unchanged - either Revit opens it afresh (`opened true`), or `opened false` with Revit's words and, when the save went through, *"A window on that same file was ALREADY open in Revit before this run ..."*; never `opened true` on the earlier window |
| **OD4** | *Row 5b-1710, positive - elements.* The project in front, `revit_view_image` with `view="Level 1"` and `around` two of OD0's wall ids, comma separated | One picture of just those two walls with a margin, not the whole plan; the first line reads *"A picture of a region of 'Level 1' (FloorPlan) ... round 2 element(s)"*, never a name ending `Copy 1`; the closing line *"The temporary copy of the plan is gone ... The plan itself was not changed."*. **Then look in Revit:** no `Level 1 Copy 1` under Floor Plans; `Level 1`'s crop, crop region visibility and annotation crop exactly as before; **nothing new in the undo list**. If a model-position line is given, write it down for OD5 |
| **OD5** | *Row 5b-1710, positive - a box.* `revit_view_image` with `view="Level 1"` and `around` a box in mm round one corner of the room, written `x1,y1;x2,y2` - take the numbers from OD4's model-position line, or from the walls' ends | A picture of just that corner; the first line names *"the box ... mm"*. The plan unchanged, as OD4 |
| **OD6** | *Row 5b-1710, negative - not a plan.* `revit_view_image` with a 3D view or a sheet as `view` and `around` one wall id | Refused, *"... a region is pictured from a floor, ceiling, engineering or area plan ..."*, no picture, nothing made. Then `around` an id that is in no element (`999999999`): refused naming it, *"Nothing was pictured."* |
| **OD7** | *Row 5b-1710, negative - the whole view is still the whole view.* `revit_view_image` with `view="Level 1"` and no `around` | The whole plan, as before this change; the first line has no *"region"* in it |
| **OD8** | *Row 5b-1711, on the PC's main Heron folder on branch main.* Note the shared store's card count (`python tools/heron-ready.py --check`). Then run `python tools/heron-ready.py` | When the store held every card: *"N tool cards ready"*, **no** *"rebuilt"*. When a lookup in the last session warned *"The knowledge store does not match this checkout"*: *"... rebuilt, because N card(s) here not in it: ..."*, and the next `heron_lookup` gives **no** such warning |
| **OD9** | *Row 5b-1711, the warning's words, from a worktree or another checkout* (any session's checkout under `.claude\worktrees`, ahead of the main folder by a card). Ask `heron_lookup` anything from a chat there | When it warns, it **names the main Heron folder by its path**, says `git pull` there (cards missing) or `git merge origin/main` here (rows with no card), and offers HERON_KNOWLEDGE in that folder's `.mcp.json` - and names **no** `--rebuild` and no `heron-ready`. With nothing drifted: no warning at all |

**OD1 to OD3 are the proof of OPEN_FAMILY_FOR_EDITING version 3** - the positive OD1, the negatives OD2
and OD3 - and [`tools/jobs/open-family-for-editing-od-2026-11-16.yaml`](../../tools/jobs/open-family-for-editing-od-2026-11-16.yaml)
is OD1 and OD2 as a batch-prove job, for the D-30 record. **That job runs through `validate --write`,
not `revit_change`**: if its positive passes without the throw, OD1 by `revit_change` is still the row
that answers row 5b-1750. Accepting the draft is the owner's signature.

### Results, 2026-11-16 - Revit 2024, sessions 18772 and 48700

**Arranged (OD0):** `od-scratch.rvt` made by Heron from `English\DefaultMetric.rte`, four walls 352514-352517 round
6000 x 4000, folders `C:\Users\Public\heron-work\od` and `od-2` made. **The job file had no `write: true`**, so
its first run went as a read and the run guard refused the Save As - corrected in this file; the run below used
the corrected copy. OD4-OD7 ran on this branch's add-in, built and deployed to Revit 2024 (DLL hash
`aa99b0d0ae8487f8`, equal to the build), Revit restarted.

| Row | Result |
|---|---|
| OD1, run 2026-11-16 | **PASS** - `opened true`, `activeAfter M_Single-Flush`, `savedTo C:/Users/Public/heron-work/od\M_Single-Flush.rfa`, `problem` empty; and **the read-back line appeared**: *"Revit answered "An internal error has occurred." while opening it, but the window in front IS ..."* - Revit still throws, the card now tells the truth. |
| OD2, run 2026-11-16 | **PASS** - `opened false`, *"No loaded family is called 'ZZZNOTAFAMILY'."*, nothing written, `activeAfter` empty. |
| OD3, run 2026-11-16 | **Safe, by another route** - with OD1's family window still open, both `saveFolder=...od-2` and the same folder were stopped by **Heron's run guard** (*"a Save As of 'M_Single-Flush'"* / *"a save of 'M_Single-Flush'"* - saving an open document is a Publish job), never `opened true` on the earlier window. Revit's own words: none, the guard stopped first. |
| OD4, run 2026-11-16 | **PASS** - *"A picture of a region of 'Level 1' (FloorPlan) ... round 2 element(s) ... from a temporary copy of the plan cropped to it"*, the model-position line (corners -720,4720 to 6720,-720), *"The temporary copy of the plan is gone ... The plan itself was not changed."* Read through the API after: no `Level 1 Copy 1`, the plan's crop off, crop region hidden, annotation crop off, as before, and the document unmodified. `view` had to be *FloorPlan: Level 1* - the template's ceiling plan is also *Level 1*. |
| OD5, run 2026-11-16 | **PASS** - *"... the box -500,-500 to 1500,1500 mm ..."*, the plan unchanged. |
| OD6, run 2026-11-16 | **PASS** - the elevation *North*: *"'North' is a Elevation, and a region is pictured from a floor, ceiling, engineering or area plan ... Nothing was pictured."*; `around=999999999`: *"1 of the 1 id(s) name no placed element ... Nothing was pictured."* (The template has no 3D view.) |
| OD7, run 2026-11-16 | **PASS** - the whole plan, no *"region"* in the first line, *"Nothing was changed."* |
| OD8, OD9 | **NOT RUN** - they test this branch's Python on the PC's main Heron folder on main, so they wait until it is merged. |
