# Needs checking — Group QP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group QP - connector sized by Duct Size and Pipe Size and taken in a ring's bore, model text measured on its letters, a freeform answer naming the live material tool - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1381](https://github.com/Ajmalpshaik/Heron-AI/issues/1381) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** rows [5b-2031](../fragment-issues/row-5b-2031.md), [5b-2032](../fragment-issues/row-5b-2032.md),
[5b-2033](../fragment-issues/row-5b-2033.md) and [5b-2034](../fragment-issues/row-5b-2034.md), seen 2026-11-24 on
Revit 2020 finishing an air handling unit family. Three cards changed C# and went back to DRAFT:
`ADD_FAMILY_CONNECTOR` version 8, `ADD_FAMILY_MODEL_TEXT` version 3, `CREATE_FAMILY_FREEFORM_SOLID` version 2.
Each card's `tests/cases.yaml` holds the full cases under its version; these rows are the run.

**What is done and what is not.** All three compile for 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-24, in a cloud container). **Never run in Revit.** For the model text it is not known which was wrong on
Revit 2020 - the letters or only Revit's bounding box; QP4 settles it.

**The model: a scratch family, never the working one** - Revit 2020 first. A Mechanical Equipment family with a
box, a flange ring on its right face sketched on "Supply Face" (opening 209 x 209), a pipe stub ending on
"Drain End", shared parameters Supply_Air_Width and Supply_Air_Height (Duct Size, 205), Condensate_Drain_Diameter
(Pipe Size, 32), and a reference plane facing front at Y -489.5.

| ID | Do this | Pass looks like |
|---|---|---|
| **QP1** | `ADD_FAMILY_CONNECTOR` facePlane "Supply Face", system "Supply Air", sizeParameters "Supply_Air_Width, Supply_Air_Height"; then "Drain End", "Sanitary", "Condensate_Drain_Diameter" | A 205 x 205 duct connector centred in the opening, both sizes associated (the = sign in Properties), and the finding *"... 2 and 2 mm clear each side"*; a 32 mm pipe connector associated to Condensate_Drain_Diameter. Then Supply_Air_Width set to 200 in Family Types: the connector reads 200 wide |
| **QP2** | **Negative:** the same ring with the two sizes set to 215 and 205; then a whole 400 x 300 neck face with sizes 390 and 290; then a Duct Size parameter on the pipe stub | Each refused, nothing made: *"would not fit inside that opening either way round"*; *"On a whole face the port must be the face's own size"*; *"is a Duct Size parameter, which sizes a duct connector, not a pipe one"* |
| **QP3** | The ring with Length parameters holding its own 209 x 209 | Made as version 7 made it - no clearance finding. The exact match did not change |
| **QP4** | `ADD_FAMILY_MODEL_TEXT` `text=AHU-01 workPlane=<the plane at Y -489.5> at=0,300 heightMm=40 depthMm=0.5`; then on `Center (Front/Back)` with `offsetMm=-489.5`. Measure one letter's depth with Revit's Measure tool in plan | Placed, *"measured on its solids"*, letters Y -489.5 to -490 (or -489 to -489.5). **Copy any finding naming the bounding box into the result** - it says version 2's 494.5 mm was the box. If instead the call refuses *"... mm thick through the plane, measured on their own solids"*, nothing is kept and row 5b-2033 is reopened for the depth Revit builds |
| ~~**QP5**~~ | **Negative:** the same text on `Center (Front/Back)` with no offset, depthMm=2 | Letters Y 0 to -2, NO bounding-box finding - one here would mean the solids are measured wrong |
| **QP6** | `CREATE_FAMILY_FREEFORM_SOLID` any solid loft from its card; then follow its answer: `LINK_FAMILY_PARAMETER` with the id it gave and Material linked to a family material parameter. **Negative:** the same as a void | The solid's answer names LINK_FAMILY_PARAMETER, never SET_FAMILY_FORM_MATERIAL, and the link is made; the void's answer names COMBINE_FAMILY_FORMS and no material tool |
| **QP7** | Added 2026-11-26, row [5b-2035](../fragment-issues/row-5b-2035.md). In the QP scratch family, a form left at <By Category>: `SELECT_BY_MATERIAL` with a material the family holds; then follow its finding - `LINK_FAMILY_PARAMETER` with that form's id and `Material=<that material>`, and select again | The family finding says to set one *"with LINK_FAMILY_PARAMETER (Material=)"*, never SET_FAMILY_FORM_MATERIAL; the second select finds one form more. Nothing changed by the select |
| ~~**QP8**~~ | Added 2026-11-26, row [5b-2035](../fragment-issues/row-5b-2035.md). **Negative:** with the family in front, `SET_ELEMENT_MATERIAL` on any form, material "Glass" | Refused, naming LINK_FAMILY_PARAMETER for a form's material and SET_FAMILY_TYPE_VALUES for a type's - never SET_FAMILY_FORM_MATERIAL; nothing changed |

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`, built from main `e8c3be150`)

**Arranged:** `pt-ahu` - a scratch COPY of Ajmal PS's AHU family *TRG_Mechanical Equipment_Air Handling Unit_DX_SKM MAH-0202S2M.rfa* (the family this group was written from), saved in `%TEMP%\heron-work\pt`; the library file untouched. Its
planes are named *Supply Air Face* (not "Supply Face") and *Pipe Stub Ends* (five stubs, the drain at X -402,
Z 186); no plane sits at Y -489.5. A thin port plate lies on each port's plane beside the flange, so the flange was
named by `form=` (supply ring `cc308a19-...-00002013`, opening 209 x 209; drain stub `...-00001b3e`). Every run
rolled back. Proof jobs [`qp-text-freeform-...`](../../tools/jobs/qp-text-freeform-revit2020-2026-11-26.yaml)
(ADD_FAMILY_MODEL_TEXT v3, CREATE_FAMILY_FREEFORM_SOLID v2) and
[`qp-select-by-material-...`](../../tools/jobs/qp-select-by-material-revit2020-2026-11-26.yaml) (SELECT_BY_MATERIAL
v5) - all **PASS**, drafts waiting for the owner's signature.
[`qp-connector-v8-...`](../../tools/jobs/qp-connector-v8-revit2020-2026-11-26.yaml) came back **NO NEGATIVE** - QP2.

| Row | Result |
|---|---|
| QP1, run 2026-11-26 | **PASS for the two connectors; the flex owed** - duct: *"The port, 205 x 205 mm from its parameters, sits centred in the ring face's opening of 209 x 209 mm - 2 and 2 mm clear each side"*, made at X 1450, Y 0, Z 587 pointing +X, `sizedBy` *connector width <- Supply_Air_Width, connector height <- Supply_Air_Height* (Duct Size). Pipe: Sanitary, round 32 mm at X -402, Y -562.5, Z 186 pointing -Y, *connector diameter <- Condensate_Drain_Diameter* (Pipe Size). The width-to-200 flex in Family Types was not done (rolled back). |
| QP2, run 2026-11-26 | **FAILED, one of three** - (i) sizes 215 x 205 on the ring: the right words - *"... hold 215 and 205 mm - a port sized by them would not fit inside that opening either way round. NOTHING from this call was kept."* - but as **`fragment_threw`**, not a refusal (`throw` at `impl/any/fragment.cs:1004`); row [5b-2051](../fragment-issues/row-5b-2051.md), issue [#1428](https://github.com/Ajmalpshaik/Heron-AI/issues/1428). (iii) *Supply_Air_Width* on the drain stub: **PASS** - *"Nothing was made. "Supply_Air_Width" is a Duct Size parameter, which sizes a duct connector, not a pipe one."* (ii) a whole face: **NOT RUN** - this family has no whole-face neck (the fresh-air port is a ring, 867 x 692). |
| QP3, run 2026-11-26 | **NOT AS WRITTEN** - the ring's Length parameters here hold 205 x 205, not its own 209 x 209: made, sized by them, with the clearance finding (correct for 205). The exact-match case is still owed. |
| QP4, run 2026-11-26 | **NOT SETTLED** - on *Center (Front/Back)* with `offsetMm=-489.5`, depthMm 0.5: placed, with *"Row 1: Revit gave no solid to measure, and its bounding box reaches 499.5 mm through the plane where 0.5 mm deep was asked and the Depth reads 0.5 mm - look at the letters in the Family Editor."* - letters reported Y -989 to -489.5, from the box. So the box is wrong and the Depth right; whether the letters are 0.5 mm needs Revit's Measure tool, by hand. No plane at Y -489.5 exists in this family for the first form. |
| QP5, run 2026-11-26 | **PASS** - no offset, depthMm 2: *"Its letters reach X -0.27 to 188.36, Y -2 to 0, Z 299.37 to 340.18 mm - its left end where asked, measured on its solids."* No bounding-box finding. |
| QP6, run 2026-11-26 | **PASS for the answers** - a solid loft (rectangle to circle, at X 5000): *"Its material is LINK_FAMILY_PARAMETER with this id ..."*, never SET_FAMILY_FORM_MATERIAL; as a void: *"... COMBINE_FAMILY_FORMS does with this id and the solid's"*, no material tool. **Following it - the link made - NOT RUN:** it needs the loft kept. |
| QP7, run 2026-11-26 | **PASS for the finding** - `materialName=TRG_AHU_Actuator Grey`: 9 of 698 forms; *"This is a family: all 698 form(s) in it were scanned whatever the category list says ... 16 form(s) take their material <By Category> and report none - set one with LINK_FAMILY_PARAMETER (Material=) to have it found"*. Negative, a material it does not hold: 0, listing the 44 it has. **Following it - NOT RUN:** the finding does not name the 16 forms' ids, and the link needs keeping. |
| QP8, run 2026-11-26 | **PASS** - `SET_ELEMENT_MATERIAL` material *Glass*, the family in front: `refused` *"This is a family. In the Family Editor a form's material is LINK_FAMILY_PARAMETER and a family type's Material parameter is SET_FAMILY_TYPE_VALUES - nothing was changed"*, `writtenIds` 0. (Its read-only setup step did not bind its values; the refusal comes before any form is read.) |

**Signed 2026-11-26 by Ajmal PS** (his word in chat, "6 sing"; signature put on by the session): `add-family-model-text` version 3, `create-family-freeform-solid` version 2 and `select-by-material` version 5, all three cards PROVEN. `add-family-connector` version 8 is NOT signed - its negative threw (row 5b-2051, issue #1428).

### What QP1 and QP2 owe now - `ADD_FAMILY_CONNECTOR` version 9, 2026-11-26 (row 5b-2051, issue [#1428](https://github.com/Ajmalpshaik/Heron-AI/issues/1428))

**Fixed in the code, compiled for 2020 to 2027, never run in Revit.** Version 9 makes the connector and checks its fit inside a SubTransaction. A port that will not fit its face or ring opening is rolled back and comes back through `refused`, with the same words and nothing kept. Version 8 threw. Version 9 changed the card's C#, so the results above are version 8's. They do not count for version 9's proof.

Owed, in the same scratch family `pt-ahu` on Revit 2020 - re-run [`qp-connector-v8-...`](../../tools/jobs/qp-connector-v8-revit2020-2026-11-26.yaml) as it stands (the file keeps its name; its comments say version 9):

| Check | Owed on version 9 | Pass looks like |
|---|---|---|
| QP1 | The positive again - the ring with 205 and 205 (the job's positive leg); the 32 mm Sanitary pipe connector on Condensate_Drain_Diameter; the flex of Supply_Air_Width to 200 | Made, sized by both parameters, *"2 and 2 mm clear each side"*; the pipe connector as on 2026-11-26; the connector reads 200 wide after the flex. **The job file holds only the duct leg** - the pipe leg needs one added, with the drain stub's full id read from the family (recorded above only as `...-00001b3e`) |
| QP2 (i) | The ring with 215 and 205 - the job's negative leg; then the same values once by `revit_change` on `pt-ahu`, the owner pressing Discard | Verdict PASS, the negative leg answered through **`refused`** - *"... would not fit inside that opening either way round. NOTHING from this call was kept."* - never `fragment_threw`. The rolled-back job cannot show that version 9's own rollback left nothing (the host takes the whole run back anyway); the live call can - its answer says nothing changed, no element added |
| QP2 (ii) | A whole 400 x 300 neck face with 390 and 290 | Still **NOT RUN** - `pt-ahu` has no whole-face neck; it needs a family that has one. Refused the same way, *"On a whole face the port must be the face's own size"* |
| QP2 (iii) | A Duct Size parameter on the pipe stub | Owed again on version 9 (its refusal comes before the connector is made, so it should read as on 2026-11-26). Not in the job file - it goes with the pipe leg above |

Nothing above is struck: every check here is owed again on version 9.

### Results, later 2026-11-26 - version 9, Revit 2020, session 32484, add-in file `df0d401c52ca05f4`

Run from this pull request's head `17cf1d586`, on the same scratch `pt-ahu`, rolled back.

| Row | Result |
|---|---|
| QP2 (i), run again 2026-11-26 on version 9 | **PASS** - the job [`qp-connector-v8-...`](../../tools/jobs/qp-connector-v8-revit2020-2026-11-26.yaml), negative leg: `refused` *"The opening of the ring face on "Supply Air Face" is 209 x 209 mm, and "Supply_Air_Width" and "Supply_Air_Height" hold 215 and 205 mm - a port sized by them would not fit inside that opening either way round. NOTHING from this call was kept."*, `connector` empty, no `fragment_threw`. The by-hand `revit_change` half was not done. |
| QP2 (iii), run again 2026-11-26 on version 9 | **PASS** - *"Nothing was made. "Supply_Air_Width" is a Duct Size parameter, which sizes a duct connector, not a pipe one. A Length parameter sizes any connector."* |
| QP1, positive leg of the same job on version 9 | Made as before - 205 x 205, centred in the 209 x 209 opening, `sizedBy` both Duct Size parameters. |

**Signed 2026-11-26 by Ajmal PS** (his typed word in chat, "Yes, sign it"; signature put on by the session):
`add-family-connector` version 9, card PROVEN, on that job (QP1 positive, QP2 (i) negative). Groups PO and OT's
connector rows and QP2 (ii) are still owed.

