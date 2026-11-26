# Needs checking — Group PX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PX - a box's id from EXTRUDE_BETWEEN_PLANES, and an Integer or Yes/No formula read back by SET_FAMILY_FORMULA - DRAFT until proved (2026-11-23)

**Tracked as GitHub issue [#1297](https://github.com/Ajmalpshaik/Heron-AI/issues/1297) since 2026-11-23** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

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
| [`SET_FAMILY_FORMULA`](../../brain/fragments/set-family-formula/fragment.yaml) v3 (DRAFT): Revit's refusal of a formula returned through `refused`, rolled back in a sub-transaction, never thrown ([row 5b-2010](../fragment-issues/row-5b-2010.md), 2026-11-24) | `set-family-formula` `fragment.cs` | PX6 |
| [`SET_FAMILY_FORMULA`](../../brain/fragments/set-family-formula/fragment.yaml) v4 (DRAFT): `formulaSet`, the set path's count - 1 when the family read back holds the formula asked for, so `accept` can sign a SET ([row 5b-2009](../fragment-issues/row-5b-2009.md), 2026-11-24) | `set-family-formula` `fragment.yaml`, `fragment.cs` | PX7 |

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
| ~~**PX6**~~ | *Revit's refusal comes back as a refusal - version 3, Revit 2020.* `SET_FAMILY_FORMULA` on *PX Box* with a Length Neck Top: parameterName `Neck Top`, formula `Neck Top + 10 mm` (a formula on the parameter it sets). [Row 5b-2010](../fragment-issues/row-5b-2010.md) | The call ends normally - **never** `fragment_threw` - with `refused` *"Revit refused the formula "Neck Top + 10 mm" on "Neck Top": ... a formula cannot use the parameter it sets, directly or through another. Nothing was kept."*, the same words in `findings`, and Family Types showing Neck Top's formula and value as they were before the call. **Positive:** then formula `Height + Neck Depth` on Neck Top is set and reads back 350, as PX5 - the sub-transaction keeps a formula Revit takes. **Record** the release |
| ~~**PX7**~~ | *The set path can be signed - version 4, any release.* `SET_FAMILY_FORMULA` on any furniture family with a type (M_Desk, as on 2026-11-24): parameterName `Leg Height`, formula `Height / 5`, then `python brain/heron_validate.py accept set-family-formula --by ...`. [Row 5b-2009](../fragment-issues/row-5b-2009.md), issue [#1322](https://github.com/Ajmalpshaik/Heron-AI/issues/1322) | `formulaSet` **1**, `formulaNow` *Height / 5*, `valueNow` a fifth of Height, `cleared` false - and `accept` signs it on `formulaSet`, never *POSITIVE EMPTY*. **Negative:** parameterName `Heron No Such Parameter 1124` - `refused` *"... is not a parameter of this family, so no formula was set."*, `formulaSet` 0, `formulaNow` empty. **Record** the release |

### Results, 2026-11-24 - Revit 2020, session 29500

**Arranged:** *PX Box* in `%TEMP%\heron-work\px`, made by Heron from *Metric Generic Model.rft* with main's PROVEN
cards (`bf62abe3b`): planes Box Left/Right -300/300, Box Front/Back -300/300, Box Top 250; type *PX Box*, Height 250,
Neck Depth 100; INSTANCE Integer *Blade Count*, INSTANCE Yes/No *Show Blades*. Run from branch
`claude/prove-three-family-tools`. Ask to Delete was OFF, so PX1's kept run had no Keep box (D-197); it was read back
instead (PX2). Not run: the Revit 2022-or-later half of PX4, and the project-in-front negatives.

| Row | Result |
|---|---|
| PX1, run 2026-11-24 | **PASS** - kept: `size` *600 x 600 x 250 mm ... from Z 0 to 250*, five locks, `formId` `cc2f6b60-...-00000a99`, `elements` 1. Negative (rolled back, `validate --write --kept`): sidePlanes with *No Such Plane* - `refused` *"Nothing was built. No reference plane or level is called "No Such Plane" (the side plane)..."*, `formId` empty, `elements` 0. Signed by Ajmal PS 2026-11-24, card PROVEN. |
| PX2, run 2026-11-24 | **PASS** - `REPORT_FAMILY_FORMS` `which` = PX1's id: *"1 form(s) of 3 in the family, by id"*, X -300 to 300, Y -300 to 300, Z 0 to 250. `SET_FAMILY_FORM_VISIBILITY` `forms` = the same id, `views=all`: `changed 1` on that extrusion. |
| PX3, run 2026-11-24 | **PASS** (rolled back) - `valueNow` **4**, finding *"... In the current type "PX Box" it gives 4."* Family Types not looked at by hand (the run was rolled back). |
| PX4, run 2026-11-24, Revit 2020 only | **PASS on 2020** (rolled back) - `Height > 200 mm` gives `valueNow` **Yes**, `Height > 300 mm` gives **No**. The 2022-or-later half is still owed. |
| PX5, run 2026-11-24 | **PASS for the length** (rolled back) - `Height + Neck Depth` on Neck Top gives `valueNow` **350.0**. **Negative FAILED:** `Neck Top + 10 mm` came back `fragment_threw` with the right words (*"Revit refused the formula ... a formula cannot use the parameter it sets ... Nothing was kept."*) instead of `refused` - handed to its own session to fix. |
| Signing, 2026-11-24 | **The set path cannot be signed as the card stands** ([row 5b-2009](../fragment-issues/row-5b-2009.md), issue [#1322](https://github.com/Ajmalpshaik/Heron-AI/issues/1322)): `accept` judged PX3's positive *"POSITIVE EMPTY - every declared result came back zero (cleared); beside a count, a sentence is not counted (formulaNow, valueNow)"*. By the owner's choice the signed positive is a CLEAR instead - Neck Top's formula *Height + Neck Depth* set with version 1 (kept), then version 2 with `formula` empty, rolled back: `cleared true`, `formulaNow` empty; negative *No Such Parameter*: `refused` *"... is not a parameter of this family, so no formula was set."* Signed by Ajmal PS 2026-11-24, card PROVEN. PX3 to PX5 are the evidence for the read-back change; the signed proof shows the clear path. |

### Results, 2026-11-26 - Revit 2020, session 32484, add-in 0.3.0.0 (file `df0d401c52ca05f4`, built from main `e8c3be150`)

**Arranged:** `pt-ahu` - a scratch COPY of Ajmal PS's AHU family *TRG_Mechanical Equipment_Air Handling Unit_DX_SKM MAH-0202S2M.rfa* (the family this group was written from), saved in `%TEMP%\heron-work\pt`; the library file untouched. Its Length
parameters stood in for PX Box's (*Pipe_Stub_Length* 75, *Control_Box_Depth* 200, *Unit_Height* 900); PX7 asks only
for a family with a type, so no furniture family was needed. Rolled back. Proof job
[`px-set-family-formula-v4-...`](../../tools/jobs/px-set-family-formula-v4-revit2020-2026-11-26.yaml) - **PASS**, a
draft waiting for the owner's signature (the set path, judged on `formulaSet`).

| Row | Result |
|---|---|
| PX6, run 2026-11-26, Revit 2020 | **PASS** - `Pipe_Stub_Length + 10 mm` on *Pipe_Stub_Length*: ended normally, never `fragment_threw`, `refused` *"Revit refused the formula "Pipe_Stub_Length + 10 mm" on "Pipe_Stub_Length": Formula setting failed. ... a formula cannot use the parameter it sets, directly or through another. Nothing was kept."*, `formulaSet` 0, `valueNow` still 75.0. **Positive:** `Control_Box_Depth / 2` on the same parameter - set, `formulaSet` 1, gives 100.0. |
| PX7, run 2026-11-26, Revit 2020 | **PASS** - `Unit_Height / 5` on *Control_Box_Depth*: `formulaSet` **1**, `formulaNow` *Unit_Height / 5*, `valueNow` 180.0, `cleared` false. **Negative:** *Heron No Such Parameter 1124* - *"... is not a parameter of this family, so no formula was set. ADD_FAMILY_PARAMETERS makes one."*, `formulaSet` 0, `formulaNow` empty. |

PX4's Revit 2022-or-later half is still owed - no such Revit was open.
