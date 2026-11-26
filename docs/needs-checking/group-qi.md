# Needs checking — Group QI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group QI - temporary HIDE in the active view, and a form's long id taken by the hide and the isolate - BUILT, NOT RUN IN REVIT (2026-11-24)

**Tracked as GitHub issue [#1357](https://github.com/Ajmalpshaik/Heron-AI/issues/1357) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [row 5b-2024](../fragment-issues/row-5b-2024.md). On 2026-11-24, on Revit 2020 building a family, the
modeller asked to hide the outside panels for now to see inside, and Heron could only isolate. The fix widens
`revit_isolate_temporary` with `mode` - `isolate` (the default) or `hide` - sent as its own add-in operation,
`hide_temporary`, and lets `ids` take a family form's long UniqueId as well as Revit's number.

**What is done and what is not.** The add-in compiles for 2020 to 2027 (`tools/check-compile.py`, 2026-11-24,
in a cloud container), and `tests/test_mcp_serves.py` and `tests/test_tool_registry.py` hold what the server
sends and says and where the add-in routes it. **Never run in Revit.** Whether `HideElementsTemporary` lands
in the Undo list, as the isolate's does (ES3), is not known. The add-in must be rebuilt and redeployed first:
an add-in older than this answers `unknown_op` to a hide, and that refusal is QI5.

**The model: a saved copy, never the working model** - a family with several forms (a panel family is ideal),
and a project plan with ducts and walls. Revit 2020 first. Changes OFF for every row.

| ID | Do this | Pass looks like |
|---|---|---|
| **QI1** | In the family, `REPORT_FAMILY_FORMS`, then `revit_isolate_temporary` with `mode=hide` and `ids` = three of the long ids it printed | The three forms vanish from the active view, the coloured Temporary Hide/Isolate border shows, the rest stay; the answer starts *"Hid 3 element(s)"*. Nothing in the family changed - no save question on close beyond any the family had already |
| **QI2** | With QI1's hide still on, `mode=hide` again with two more long ids; then `revit_reset_temporary` | All five hidden - the second added to the first, the answer saying *"hidden on top of it"*; after the reset all five are back and the border is gone |
| **QI3** | In a project plan, `revit_isolate_temporary` with `mode=hide`, `category=walls`; then `mode=isolate` with `ids` = two duct ids in Revit's number and one in its long form | The walls hidden, everything else shown; then ONLY the three ducts shown, the answer saying the earlier temporary state was replaced. Write down what Revit's Undo drop-down shows for the hide (*Heron: hide Walls temporarily*, or none) |
| **QI4** | **Negative:** `mode=hide` with `ids` = one real long id and one long id from ANOTHER family (or typed with one hex digit changed); then the same with a type's id; then on a sheet | Each refused, *"NOTHING was hidden"*: the first names the long id that names nothing in this model; the second *"These ids name element TYPES"*; the sheet *"cannot hide or isolate temporarily"*. The view unchanged each time |
| **QI5** | **Negative, older add-in:** with the add-in from before this change still loaded, `mode=hide`, `category=walls` | *"The Heron add-in in this Revit is older than temporary hide, so nothing was hidden"* - and NOTHING isolated: an isolate here would be the very failure the separate operation exists to stop |

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`, built from main `e8c3be150`)

**Arranged:** `pt-ahu` - a scratch COPY of Ajmal PS's AHU family *TRG_Mechanical Equipment_Air Handling Unit_DX_SKM MAH-0202S2M.rfa* (the family this group was written from), saved in `%TEMP%\heron-work\pt`; the library file untouched, its default `{3D}` view in
front, through the chat's `revit_isolate_temporary` and `revit_reset_temporary` (this chat's Heron, main `e8c3be150`).

| Row | Result |
|---|---|
| QI1, run 2026-11-26 | **PASS for the answer** - `mode=hide`, three long ids (supply ring, fresh-air ring, drain stub): *"Hid 3 element(s) in "{3D}", in pt-ahu"*. A picture from `revit_view_image` does not show Revit's Temporary Hide/Isolate border, so whether the three vanished on screen was not seen - the owner's eye is owed. |
| QI2, run 2026-11-26 | **PASS for the answers; the screen owed, as QI1** - two more long ids: *"Hid 2 element(s) ... A temporary hide/isolate was already on in this view, and these were hidden on top of it, as Revit's own HH does."* Then `revit_reset_temporary`: *"The Temporary Hide/Isolate in "{3D}" is off, and the view shows what its own settings show."* |
| QI3 | **NOT RUN** - needs a project plan with walls and ducts; none was open. |
| QI4, run 2026-11-26, first case only | **PASS, first case** - a real long id and one with a changed hex digit: *"These long ids name nothing in "pt-ahu": cc308a19-...-00002fff. NOTHING was hidden."* The type's id and the sheet cases are **NOT RUN**. |
| QI5 | **NOT RUN** - needs the add-in from before this change; this Revit has the current one. |
