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
| **QP5** | **Negative:** the same text on `Center (Front/Back)` with no offset, depthMm=2 | Letters Y 0 to -2, NO bounding-box finding - one here would mean the solids are measured wrong |
| **QP6** | `CREATE_FAMILY_FREEFORM_SOLID` any solid loft from its card; then follow its answer: `LINK_FAMILY_PARAMETER` with the id it gave and Material linked to a family material parameter. **Negative:** the same as a void | The solid's answer names LINK_FAMILY_PARAMETER, never SET_FAMILY_FORM_MATERIAL, and the link is made; the void's answer names COMBINE_FAMILY_FORMS and no material tool |
| **QP7** | Added 2026-11-26, row [5b-2035](../fragment-issues/row-5b-2035.md). In the QP scratch family, a form left at <By Category>: `SELECT_BY_MATERIAL` with a material the family holds; then follow its finding - `LINK_FAMILY_PARAMETER` with that form's id and `Material=<that material>`, and select again | The family finding says to set one *"with LINK_FAMILY_PARAMETER (Material=)"*, never SET_FAMILY_FORM_MATERIAL; the second select finds one form more. Nothing changed by the select |
| **QP8** | Added 2026-11-26, row [5b-2035](../fragment-issues/row-5b-2035.md). **Negative:** with the family in front, `SET_ELEMENT_MATERIAL` on any form, material "Glass" | Refused, naming LINK_FAMILY_PARAMETER for a form's material and SET_FAMILY_TYPE_VALUES for a type's - never SET_FAMILY_FORM_MATERIAL; nothing changed |
