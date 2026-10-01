# Needs checking — Group BR

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BR - a nested family placed in a family, locked to its planes, and its type's parameters linked (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** Three new tools, added the same day as Groups BL to BQ,
because the `family-creation` skill's own NOT YET line named nested families, and a motor in an air handler or
a handle on a valve could be neither placed, locked nor sized from the family around it:
[`place-nested-family`](../../brain/fragments/place-nested-family/fragment.yaml) - `PLACE_NESTED_FAMILY`,
FRG-ELE-075 - [`lock-nested-family-to-planes`](../../brain/fragments/lock-nested-family-to-planes/fragment.yaml)
- `LOCK_NESTED_FAMILY_TO_PLANES`, FRG-GEO-049 - and
[`link-nested-type-parameter`](../../brain/fragments/link-nested-type-parameter/fragment.yaml) -
`LINK_NESTED_TYPE_PARAMETER`, FRG-PAR-030.

**What is already known, and it is NOT a proof.** All three compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01). **The API places a LEVEL-BASED family inside a family only
from Revit 2024** - the call that takes a level is on the project's creation object 2020 to 2023 and on the
family's from 2024, and the one call a family has before that says in its own remark that it throws for a
level-based family. So the tool reaches the 2024 call by reflection, and on 2020 to 2023 a level-based nested
family is expected to be REFUSED with the advice to make it Work Plane-Based. **None of the three has met a
model**, and all are `DRAFT`. The proof plan is
[`tools/jobs/family-nested-2026-10-01.yaml`](../../tools/jobs/family-nested-2026-10-01.yaml).

| # | Check | Expected |
|---|---|---|
| **BR1** | A Work Plane-Based generic model `Heron Nested Box` (one type `300`, a Length type parameter `Width`, its centre planes Center (Left/Right) and Center (Front/Back)) loaded into Group BM's arrangement - by LOAD_FAMILY with that family in front, whose card speaks of a project, so its words are part of this check - then PLACE_NESTED_FAMILY at `0,0,0` on `Ref. Level` | Placed, its id given back, read back at the origin, LOOKED AT in the plan. **Negative:** point `0,0,300` on `Ref. Level` - refused as 300 mm off the plane, nothing placed |
| **BR2** | LOCK_NESTED_FAMILY_TO_PLANES on it: `Center (Left/Right)=Center (Left/Right); Center (Front/Back)=Center (Front/Back)` | Two padlocks closed when it is selected; its extent unchanged. **Negative:** `Center (Left/Right)=Center (Front/Back)` - refused as facing different ways |
| **BR3** | A Length type parameter `Motor Width` holding 450 in this family: LINK_NESTED_TYPE_PARAMETER, family `Heron Nested Box`, type `300`, links `Width=Motor Width`; then FLEX_FAMILY at another Motor Width | The nested box 450 wide, then the flexed width - Edit Type showing Width greyed on Motor Width. **Negative:** `Width=Show_Handle`, a Yes/No - refused naming both kinds |
| **BR4** | `Width=` an INSTANCE Length parameter of this family | **Record what Revit says** - a nested type is shared by every placed copy, and whether Revit links it to an instance parameter is not in its remarks |
| **BR5** | A LEVEL-BASED nested generic model, PLACE_NESTED_FAMILY on `Ref. Level`, on **Revit 2024** and on **Revit 2020** | 2024: placed on the level and read back. 2020: refused, the answer saying a level-based family is placed in a family through the API only from 2024 and to make it Work Plane-Based - **or, if 2020 places it after all, that is the finding and the card is corrected** |
| **BR6** | BR1 to BR3 on **Revit 2020** | The same answers - the kind check's reflection takes the other branch there |
