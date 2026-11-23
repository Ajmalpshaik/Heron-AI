# Needs checking — Group PX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PX - a box's id from EXTRUDE_BETWEEN_PLANES, and an Integer or Yes/No formula read back by SET_FAMILY_FORMULA - DRAFT until proved (2026-11-23)

**Why:** two reply gaps found building a parametric louvre family in Revit 2020 on 2026-11-23 (session 49564,
add-in 0.3.0.0). [Row 5b-2000](../fragment-issues/row-5b-2000.md): the box tool gave no id, so nine boxes each
needed `REPORT_FAMILY_FORMS` and a match by extents before a later step could name them.
[Row 5b-2001](../fragment-issues/row-5b-2001.md): an Integer formula read back as *"it gives ."* - blank. Both
fixed in code on 2026-11-23 in a cloud session with no Revit; **neither has met a Revit.** Both compile for
Revit 2020 to 2027 (`tools/check-fragments-compile.py`). Both cards were `PROVEN` version 1 and are `DRAFT`
version 2 until this group is run.

| Change | Files | Checks |
|---|---|---|
| [`EXTRUDE_BETWEEN_PLANES`](../../brain/fragments/extrude-between-planes/fragment.yaml) v2 (DRAFT, was PROVEN v1): `formId`, the box's unique id, and the box handed on as `elements` | `extrude-between-planes` `fragment.cs` | PX1, PX2 |
| [`SET_FAMILY_FORMULA`](../../brain/fragments/set-family-formula/fragment.yaml) v2 (DRAFT, was PROVEN v1): the value read back by kind - a whole number, Yes or No, a text, or a measured value as before | `set-family-formula` `fragment.cs` | PX3 to PX5 |

**The models: scratch copies, never a working model** - in `%TEMP%\heron-work\px`: *PX Box*, new from
*Metric Generic Model.rft*, with reference planes Box Left / Box Right at X -300 / 300, Box Front / Box Back at
Y -300 / 300 and Box Top at Z 250 (`CREATE_REFERENCE_PLANES`), and one type (`SET_FAMILY_TYPE_VALUES`); a
Length parameter Height = 250, an INSTANCE Integer parameter Blade Count and an INSTANCE Yes/No parameter Show
Blades (`ADD_FAMILY_PARAMETERS`). A scratch project for the project-in-front cases. **Run from the pull
request's checkout until it is merged** - the client sends the fragment code from the checkout it is run in.
PX4 is run on Revit 2020 AND on one release 2022 or later: the Yes/No check takes a different road on each.

| ID | Do this | Pass looks like |
|---|---|---|
| **PX1** | *The box's id.* `EXTRUDE_BETWEEN_PLANES` on *PX Box*: sidePlanes `Box Left, Box Right, Box Front, Box Back`, basePlane `Ref. Level`, topPlane `Box Top` | `size` 600 x 600 x 250 mm and five locks, as version 1. **New:** `formId` a unique id (`<guid>-<hex>`), `elements` one element, and the finding ends *"Its id is <that id> ..."*. Select the box by hand and check its id matches. **Negative:** sidePlanes with `Box Lft` (no such plane) - refused listing the family's planes, nothing built, `formId` empty, `elements` empty |
| **PX2** | *The id works in the next step, with nothing between.* PX1's box KEPT, then `SET_FAMILY_FORM_VISIBILITY` with `forms` PX1's `formId`, and `REPORT_FAMILY_FORMS` with `which` the same id | Visibility set on that one box, and the report reads that one box, 600 x 600 x 250 - no `REPORT_FAMILY_FORMS` call and no match by extents before it. **Negative:** a project in front for `EXTRUDE_BETWEEN_PLANES` - `notAFamily` true, `formId` empty, nothing built |
| **PX3** | *An Integer formula read back.* `SET_FAMILY_FORMULA`, parameterName `Blade Count`, formula `rounddown((Height - 36 mm) / 45 mm)` | `valueNow` **4** - a whole number, not empty - and the finding *"... it gives 4."*; Family Types shows 4. Change Height to 400 by hand: Family Types shows 8. **Negative:** then formula `rounddown(("Height" - 36 mm) / 45 mm)` - refused before Revit is asked (the name in quotes), nothing changed, `formulaNow` still the first formula and `valueNow` still 4 |
| **PX4** | *A Yes/No formula read back - on Revit 2020 and on 2022 or later.* parameterName `Show Blades`, formula `Height > 200 mm`, then `Height > 300 mm` | `valueNow` **Yes**, then **No**, with Height 250 - and Family Types showing the box ticked, then clear. **Negative:** a project in front - `notAFamily` true and no formula set anywhere. **Record** the release each was run on |
| **PX5** | *A length still reads as before.* A Length parameter Neck Top and a Length Neck Depth = 100; parameterName `Neck Top`, formula `Height + Neck Depth` | `valueNow` as Revit shows a length - 350 with Height 250, the form version 1 gave (*"it gives 594.0"* in the louvre build). **Negative:** formula `Neck Top + 10` on Neck Top - the call refused with Revit's words about the loop, nothing kept |
