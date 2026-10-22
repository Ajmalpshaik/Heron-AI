# Needs checking — Group IO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IO - one form of a family pictured and read on its own: `revit_view_image` with `elements` in a family, and `REPORT_FAMILY_FORMS` version 3 (2026-10-22)

From rows [5b-1311 and 5b-1312](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21 Ajmal PS built
five detailed CCTV monitor families through Heron in the Revit 2020 Family Editor and met two gaps, each with
a way round: a picture of a few forms was refused - *"None of the 12 element(s) can be framed in 3D: 2829 has
no category"* - and one form's extents could not be read by its id. Both widened on 2026-10-22, in the cloud,
with no Revit:

| What | Where | What changed |
|---|---|---|
| `revit_view_image` with `elements` | [`RevitCapture.cs`](../../revit/Heron.Revit.Addin/RevitCapture.cs) (`capture_view`, ANALYZE, [D-123](../DECISIONS.md)) | In a family open in the Family Editor, a form is framed by its box whatever its category - one left at the family's own subcategory has none, and was refused (5b-1311). Nothing changes in a project, and datums, links and annotation are still left out |
| `REPORT_FAMILY_FORMS` v3 | [`report-family-forms`](../../brain/fragments/report-family-forms/fragment.yaml) | READ. `which` also takes the forms' ids - the long UniqueIds the form tools give back, commas between - read as `SELECT_FAMILY_FORMS` reads them: ids and words not mixed, one id that is not a form of this family refuses the call (5b-1312). Words, "all" or nothing answer as version 2 |

**What is done and what is not.** The add-in compiles for 2020 to 2027 (`tools/check-compile.py`), every
Revit member it calls exists in each (`tools/check-api-surface.py`), and the library compiles for 2020 to 2027
(`tools/check-fragments-compile.py`), all 2026-10-22. `which`'s splitting into words and ids was cut unchanged
out of the card into a scratch C# harness: left out, empty, "all", " ALL ", "void", "solid, revolve" and
"Swept Blend" give no ids, so take version 2's path; one id, two ids with spaces round them, `all, <id>`,
"cylinder" and "2929" give ids and no words; `void, <id>` gives both. The ids' lookup and everything after
it need Revit. **Nothing here has run in Revit.** `REPORT_FAMILY_FORMS` is DRAFT; its version 2 proof stays in
the card as a record. Rebuild and deploy the add-in from this change for Revit 2020, restart Revit, and restart
the Claude app (the tool's description changed).

**The family.** A scratch family, never a working one: a new family from Revit 2020's *Metric Generic
Model.rft*, saved as *Heron IO Forms.rfa* in `%TEMP%\heron-work\group-io`. In it, through `revit_change`
with `create-family-extrusion`: a solid extrusion *"circle 0,0 150"* on Ref. Level from 0 to 500 and a void
*"circle 0,0 50"* from -10 to 510 (the card's own test family, `tests/cases.yaml`); a second solid
*"rect 400,-100 600,100"* from 0 to 200, given a subcategory *IO Part* with `SET_FAMILY_FORM_SUBCATEGORY`.
Note each form's id as the form tools answered (`formIds`), and each one's number from Manage > IDs of
Selection. **And a scratch project** from the default template, for the project negatives. Run every row on
**Revit 2020**; `REPORT_FAMILY_FORMS` through `revit_read`.

| # | Run | Look for |
|---|---|---|
| **IO0** | *Negatives FIRST.* In the family: `revit_view_image`, `elements` = `999999999`; then `elements` = Ref. Level's number and the *Center (Front/Back)* reference plane's. In the project: `elements` = a level's number and a grid's | The first refused naming the id, *"name no placed element"*. The second and third refused `nothing_to_frame`, each one named *"is a datum"* - **a family's datums are still left out, and nothing changed for a project**. No picture, nothing made, the family's Views list and Undo unchanged |
| **IO1** | *The fault's own case (5b-1311).* In the family: count the forms (`REPORT_FAMILY_FORMS`) and note Undo; `revit_view_image`, `elements` = the first solid's number | **The picture of the solid in 3D**, close up, with no level or reference plane drawn; the words say 1 framed, nothing left out. Then the forms counted again, Undo unchanged, and no new 3D view under Views |
| **IO2** | `elements` = all three forms' numbers, and Ref. Level's | 3 framed, Ref. Level left out *"is a datum"*. **Picture:** both solids, the hole the void cuts in the first one. The subcategory solid framed as before the change - it had a category |
| **IO3** | *The read by id (5b-1312).* `REPORT_FAMILY_FORMS`, `which` = the void's id exactly as `create-family-extrusion` gave it back | `formCount` 1, the void alone with that id, X and Y -50 to 50 mm, Z -10 to 510 mm; `scanned` 3; the finding *"1 form(s) of 3 in the family, by id"* |
| **IO4** | `which` = the two solids' ids, commas between, the first typed twice | `formCount` 2, each once; `scanned` 3 |
| **IO5** | *Nothing else moved.* `which` = `all`, `void`, and left out | `formCount` 3, 1 and 3, as version 2 answers; the left-out call's binding note *which not given - "all", the card's own default (optional)* |
| **IO6** | *Negatives.* `which` = the first solid's id with its FIRST character changed (never the last eight: those are the element's number); the solid's id and *Center (Front/Back)*'s UniqueId; `void, <the solid's id>`; the solid's number, digits only; `cylinder`. Then in the project, `which` = the solid's id | Each refused, *"Nothing was read"*, `formCount` 0, `scanned` 0: *"is not an id in this family"*; the plane named *", not a form"*, the solid not read either; *"not both in one call"*; *"that is an element number"*; *"nor a kind of form or a side"*. The project: `notAFamily` true, nothing read |
| **IO7** | *The job it was for.* On a scratch copy of one of the 2026-10-21 CCTV monitor families: flex it (`flex-family`, or a type value changed and put back), then `REPORT_FAMILY_FORMS` with one form's id, and `revit_view_image` with that form's number | Its extents after the flex on one line, not ~100 forms; the picture of that one part. **Look at the picture** |

**Then sign** `report-family-forms` version 3 on IO3 with IO6's first negative, after IO3 to IO6 have passed;
and close row 5b-1311 on IO0 to IO2 - `capture_view` is the add-in's own, not a fragment, so there is no card
to sign. A row that fails reopens its row; nothing in a row changes to make it pass.

**Recorded, not fixed.** `revit_view_image` takes element numbers only, and the form tools answer with
UniqueIds, so a chat pictures one form only once somebody reads its number off Manage > IDs of Selection.
[Row 5b-651](../fragment-issues/section-5b-rows-401-425.md) said the same of the family method's step 13; it is
a change to the tool's input, outside these two rows.
