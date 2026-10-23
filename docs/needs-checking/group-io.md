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

### Results - 2026-10-22, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544

Run with [Group IP](group-ip.md), on the add-in built from main at `239d0b17` and deployed for 2020, 2024 and
2027 - IP's results say how it was built, checked and loaded. **Not on the family written above:** the owner was
asleep, and with *Ask to Keep* on every change through Heron waits on his Keep or Discard, so nothing was built.
The rows ran on existing families, copied to `%TEMP%\heron-work\group-ip` and opened with `ACTIVATE_DOCUMENT`:

- ***GIP M_Skylight-Pyramid*** - a copy of Revit 2020's own `M_Skylight-Pyramid.rfa`: 9 forms, two of them voids at
  the family's own subcategory (no category, the case 5b-1311 is about), the rest solids at Glass or Frame/Mullion;
  two combinations; Lower Ref. Level is 21. It plays the family above: the void's id is
  `adb89b7a-2040-4618-8ae2-3b66e7c5b964-00008b34`, the "first solid" is the Glass solid
  `a8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9` (40441).
- ***IO7 scratch - CCTV Monitor 24 Inch*** - a scratch copy of the owner's 2026-10-21 24 Inch family, as IO7 asks:
  111 forms, every one at the family's own subcategory; Ref. Level 21, *Center (Front/Back)* 46.
- ***Heron Capture Test*** - a copy of Revit 2020's `rme_basic_sample_project.rvt`, for the project negatives.

Element counts, views and Undo were read as Group IP's results say.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| IO0 | 2020 | CCTV copy: `999999999`: *"1 of the 1 id(s) name no placed element in "IO7 scratch - CCTV Monitor 24 Inch": 999999999. An id belongs to one model, or one family, and is gone once its element is deleted. Nothing was pictured."* `21,46` (Ref. Level, *Center (Front/Back)*): *"None of the 2 element(s) can be framed in 3D: 21 is a datum (Levels); 46 is a datum (Reference Planes). Give the ids of model elements, or name a view. Nothing was pictured."* Project: `378117,586795` (Level 1, Grid A): *"None of the 2 element(s) can be framed in 3D: 378117 is a datum (Levels); 586795 is a datum (Grids). ..."* No picture; the family's 9 views (2 3D) and the project's 140 (11 3D) the same afterwards; Undo disabled in both when read after the pictures | **PASS** |
| IO1 | 2020 | CCTV copy, before: 111 forms, 6,398 elements, 9 views (2 3D), the family opened fresh minutes earlier. `2713` - the screen, `solid extrusion ce590062-ddf7-4c0d-99c8-3603c9951e09-00000a99 (on Center (Front/Back)): X -273.7 to 273.7, Y 0 to 10, Z 0 to 322.9 mm, ... subcategory (the family's own)`, no category: *"A picture of 1 element(s) in IO7 scratch - CCTV Monitor 24 Inch (Revit 2020, session 60544), in a temporary 3D view, 1280 px wide. The temporary view is gone - Revit rolled it back. Nothing was changed."* **The picture:** the monitor's screen in 3D, black inside its white bezel, the stand's two columns behind and below it, cut by the box; no level or reference plane drawn. After: 111 forms, 6,398 elements, 9 views (2 3D), Undo disabled. Large green circles over it - [row 5b-1327](../fragment-issues/section-5b-rows-401-425.md) | **PASS**; the framing is row 5b-1327 |
| IO2 | 2020 | Skylight: `35636,34964,3559,21` - a void, the Glass solid it cuts, the Frame/Mullion sweep, and Lower Ref. Level: *"A picture of 3 element(s) in GIP M_Skylight-Pyramid ... Left out of the frame: 21 is a datum (Levels). The temporary view is gone - Revit rolled it back. Nothing was changed."* **The picture:** the whole skylight on its square of roof - curb, sloped glass, frame. The two subcategory solids framed as before, the category-less void with them | **PASS** |
| IO3 | 2020 | Skylight, `which` = the void's id: `formCount` 1, the void alone - `X -352 to 352, Y 121 to 514.8, Z 3960.8 to 4195 mm`, as `which=all` lists it; `scanned` 9; *"1 form(s) of 9 in the family, by id; 2 join(s) or cut(s)."* Recorded with IO6's first negative by `validate` (`brain/proof-drafts/runs/report-family-forms.json`) and drafted, `brain/proof-drafts/report-family-forms.yaml`, fingerprint `5e77ab6d7c862c25` - **not signed** | **PASS** |
| IO4 | 2020 | `which` = the Glass solid's id, the Frame/Mullion sweep's (`7d344b56-10f6-4313-9ccd-58a27cd974a6-00000de7`), the Glass solid's again: `formCount` 2, each listed once, `scanned` 9 | **PASS** |
| IO5 | 2020 | `all`: `formCount` 9. `void`: 2. Left out: 9, the binding note *"which not given - "all", the card's own default (optional)"* | **PASS** |
| IO6 | 2020 | Each `formCount` 0, `scanned` 0. The Glass solid's id with its first character a made b: *"Nothing was read. "b8cc9e63-4da8-4dd2-9b35-df4c8c3f3d63-00009df9" is not an id in this family - nor a kind of form or a side (solid, void, extrusion, revolve, blend, sweep, swept blend)."* The solid's id and *Center (Front/Back)*'s UniqueId: *"Nothing was read. "b6735d75-895e-11d3-a7e4-00105aa73639-0000002e" is "Center (Front/Back)" (Reference Planes), not a form."* - the solid not read either. `void, <the solid's id>`: *"Nothing was read. Name forms by their ids OR by kind and side, not both in one call - ..."* `40441`: *"Nothing was read. "40441" is not an id in this family - that is an element number; give the long id the form tools gave back."* `cylinder`: *"Nothing was read. "cylinder" is not an id in this family - nor a kind of form or a side ..."* The project, the solid's id: `notAFamily` true, nothing read. The whole words were read by running the card's own source unchanged with one joined string added - the reply cuts each item to about 60 characters | **PASS** |
| IO7 | 2020 | CCTV copy, `which` = the screen's id: `formCount` 1, its one line exactly as `which=all` lists it, `scanned` 111. **The flex, rolled back:** `FLEX_FAMILY` puts every value back before it answers, and a kept change waits on the Keep box the owner was asleep for, so the change and the read ran in ONE rolled-back group - `validate --write --setup set-family-type-values` (PROVEN) with *Floor Stand Height* 1000 → 1300 mm, then `REPORT_FAMILY_FORMS` with `which` = the left stand column's id `8af31a10-85ce-4207-81b0-75057d2dd536-00001e0f`: one line, *"solid extrusion ... (on Column Base): X -240 to -190, Y 75 to 125, Z -1175 to 289 mm, 3.66 L"* - 300 mm longer than its *"Z -875 to 289 mm, 2.91 L"* in the family as it stands; `scanned` 111. The same with the id's first character changed: *"Nothing was read"*, `formCount` 0. Afterwards *Floor Stand Height* read 1000 again. Record `%TEMP%\heron-work\night-1022\io7-flex-read.json`, not the proof's. **The picture:** `7695` (that column): *"A picture of 1 element(s) ..."* - the whole floor stand, the column being its full height: screen on top, shelf, H base and castors, with row 5b-1327's circles over it | **PASS** |

**Seen beside the rows.** `combinationList` and the finding's *"N join(s) or cut(s)"* are the whole family's
whatever `which` reads - IO4's two solids are in no combination and the answer still lists the family's two. That
is version 2's behaviour too (the combinations are collected after the forms, unfiltered), so it is not a
version 3 change.

**Left for the owner.** Sign `report-family-forms` version 3 on IO3 and IO6's first negative:
`python brain/heron_validate.py accept report-family-forms --by "Ajmal PS"`, on his word. Row 5b-1311 closes on
IO0 to IO2 above - no card, `capture_view` is the add-in's own. Nothing else in this group is left.
