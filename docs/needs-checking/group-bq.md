# Needs checking — Group BQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BQ - plan symbols drawn as symbolic lines, and one form repeated in a row with its count a parameter (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** Two new tools, added the same day as Groups BL to BP, because
a family read from a picture of a grille or a fan needs a part repeated in a row and a symbol in plan, and the
library could make neither:
[`draw-family-symbolic-lines`](../../brain/fragments/draw-family-symbolic-lines/fragment.yaml) -
`DRAW_FAMILY_SYMBOLIC_LINES`, FRG-VIEW-116 - and
[`array-family-forms`](../../brain/fragments/array-family-forms/fragment.yaml) - `ARRAY_FAMILY_FORMS`,
FRG-GEO-048.

**What is already known, and it is NOT a proof.** Both compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01), and every Revit call they make reads the same 2020 to 2027.
An Integer parameter's kind is read by reflection - `SpecTypeId.Int.Integer` from 2022, `ParameterType.Integer`
before it, gone from 2023 - so **2020 is a different path, not a repeat**. **Which kind of visibility Revit
takes for a symbolic line is not in its remarks**: the tool tries the one for things shown only where they are
drawn, then the forms' one, reads the setting back, and says which it took. **Neither tool has met a model**,
and both are `DRAFT`. The proof plan is
[`tools/jobs/family-symbols-arrays-2026-10-01.yaml`](../../tools/jobs/family-symbols-arrays-2026-10-01.yaml).

| # | Check | Expected |
|---|---|---|
| **BQ1** | Group BM's arrangement: workPlane `Ref. Level`, shapes `circle 0,0 150 \| path -150,-150; 150,150 \| path -150,150; 150,-150`, detailLevels `coarse, medium`, subcategory `Plan Symbol` | Five lines and the subcategory made, LOOKED AT in the Ref. Level plan: the circle and cross at Coarse and Medium, gone at Fine, absent from 3D. Loaded into a test project and placed, the plan at Coarse shows the symbol. **Negative:** workPlane `No Such Plane` - refused naming the planes the family has, nothing drawn |
| **BQ2** | The same run's findings | **Write down which kind of visibility the answer says Revit took** - the question this row exists for. Then select a line and read its Visibility Settings dialog |
| **BQ3** | An Integer type parameter `Blade Count` holding 5, and one thin solid extrusion: count `Blade Count`, step `Y 100` | Five blades 100 mm apart along +Y, LOOKED AT; the array's count labelled Blade Count when selected. **Negative:** count `Show_Handle`, a Yes/No - refused as not an Integer parameter, nothing arrayed |
| **BQ4** | FLEX_FAMILY with Blade Count 8, then 2 | Eight blades, then two, the same spacing; the copies' ids different each time, as the card says |
| **BQ5** | count `3`, step `Y 400 last`, then LOCK_FORM_TO_PLANES on the last blade's far face to a plane a Length parameter moves, then a flex of that length | Three blades, the last 400 mm from the first. **Record whether the blades re-space when the plane moves** - the card says only that a flex is where it shows |
| **BQ6** | BQ1 to BQ5 on **Revit 2020** | The same answers - the Integer kind check takes the other branch there |
