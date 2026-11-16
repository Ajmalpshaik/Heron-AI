# Needs checking — Group NY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NY - the delete box that names and closes, OPEN_FAMILY_FOR_EDITING's route, and the Glass a family load reports (2026-11-15)

From Sruthina's field report Ajmalpshaik/Heron-AI#853 (Revit 2024, `villa 2`, 2026-11-11). Rows
[5b-1700](../fragment-issues/row-5b-1700.md) and [5b-1701](../fragment-issues/row-5b-1701.md) were
repaired in a cloud container on 2026-11-15 - the add-in built, the server's half tested - and **none of
it has run in Revit**. NY5 is a measurement for [row 5b-1702](../fragment-issues/row-5b-1702.md), which
is recorded and not repaired. **NY1 to NY4 need the add-in rebuilt and redeployed, and Revit restarted.**

**The model:** a saved scratch copy, never the owner's work - a copy of `villa 2`, or any Revit 2024
project with a door family and a window family loaded and a few of each placed. Changes ON, Ask to Keep
as each row says. Every change ends with **Discard** or Undo.

| ID | Do this | Pass looks like |
|---|---|---|
| **NY1** | *Row 5b-1700 (1).* Ask Heron to change a placed window's type (`CHANGE_ELEMENT_TYPE`), as on 2026-11-11 | If the delete box comes up, each line is a category or `(no category)` followed by a Revit kind - `(no category) SketchPlane` - **never `(no category)` alone**. Write down every kind it lists: that is what row 5b-1700's open question needs. Press Don't delete; nothing changes |
| **NY2** | *Row 5b-1700 (2).* The same, and leave the box with nobody answering until the time the box names (*"Answer by HH:MM"*) | At that time the box **closes by itself**, the window is unchanged, the chat's answer is `delete_too_late` with *"did not answer in time, so the box closed by itself, which deletes nothing"*, and the **next** request runs - not `revit_busy`. **If the box stays open**, Revit's task dialog does not take `WM_CLOSE`: write it down, and the closer needs another way |
| **NY3** | *Row 5b-1701 (1).* `revit_change OPEN_FAMILY_FOR_EDITING` with a loaded family (a door family) and `saveFolder` a scratch folder under `%TEMP%\heron-work`. Negative: `familyName=ZZZNOTAFAMILY` | Positive: the family opens in its own window, in front; `opened true`, `savedTo` the .rfa in the scratch folder; **no** *"The document is currently modifiable"*. Negative: `opened false`, *"No loaded family is called 'ZZZNOTAFAMILY'."*, no window. **If EditFamily or OpenAndActivateDocument refuses inside the job's group**, write down Revit's words: the card then needs a door with no group at all |
| **NY4** | *Row 5b-1701 (2).* NY3's positive with `saveFolder` a folder that does not exist, so the Save As is refused | The chat's verdict starts *"STOPPED, BY ITS OWN ACCOUNT - the card reports: ..."* with Revit's own words, before the KEPT sentence; `opened false`; no family window left open |
| **NY5** | *Row 5b-1702, a measurement.* Note the project's *Glass* in Manage > Materials (Graphics: colour, Transparency). Load a new family with `LOAD_FAMILY` and **Keep**. Read *Glass* again by hand. Then load a second new family and Keep | Write down: (a) what `materialsChanged` listed each time; (b) the Glass by hand after each load. **If the hand reading after the first load is still 0,0,255 / 75% and the second load lists the same before-value, the report is early and the Glass did not change** (row 5b-1702 fix (b)); if the hand reading is 245,245,243 / 100%, the load really overwrote it (fix (a)). Undo both loads |
