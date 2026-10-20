# Needs checking — Group HS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HS - CREATE_STAIRS v4: one riser more, Revit's words, side stringers, nothing left behind (2026-10-19)

From rows [5b-1239, 5b-1240 and 5b-1241](../fragment-issues/section-5b-rows-401-425.md), and
[5b-446](../fragment-issues/section-5b-rows-401-425.md). Version 3 refused every U stair in the owner's
`Kerala house design` (Revit 2020): Revit will not make a `Monolithic Stair` run whose riser is 177.8 mm
(*"Can't make stairs"*), the refusal never said so, a stringer type was refused as "outside the hall",
and each refusal was recorded as keeping two elements that did not exist.

**What changed.** [`create-stairs`](../../brain/fragments/create-stairs/fragment.yaml) version 4: one
riser more, once, when Revit rolls the runs back; on a U of an odd count the smaller half first when only
that fits; the runs moved in by what the type's side supports overhang, measured on a trial layout;
Revit's words quoted; anything new after a refusal deleted again. The add-in: `FailureDiscipline.ToString`
says what Revit refused (5b-1240), and `RevitChangedSet` stops counting an element the model no longer
holds (5b-1241).

**What is done and what is not.** The fragment ran **rolled back** on Revit 2020 against a copy of the
Kerala model on 2026-10-19 (the four results in rows 1239 and 446). The add-in compiles for 2020, 2024,
2026 and 2027 and `tests/test_change_report.py` passes; **the add-in change has not been in a Revit** -
deploy it to every release and restart Revit before HS3 and HS4. The card is DRAFT until HS1 to HS5 pass
and the owner signs. Run on a **saved copy** of `Kerala house design` (or any model with Level 1 at 0 and
Level 2 at 3200, a slab on Level 2 over the hall and `Monolithic Stair` and `190mm max riser 250mm going`
loaded) - never the working model. Count Stairs, Railings and Shaft Openings in `{3D}` before each run.

| # | Run | Look for |
|---|---|---|
| **HS1** | *Positive - the owner's stair.* baseLevel=Level 1, topLevel=Level 2, stairType=Monolithic Stair, shape=u, runWidth=1100, area=11057.5,6057.5,0; 15887.5,9887.5,0, entrySide=south, firstRunSide=west, startOffset=300, cutOpening=true - first as a preview, then kept on the copy | One stair, 19 risers of 168.4 mm (9 + 10), one landing at the north end, its plan extent inside the hall, an opening in the Level 2 slab, 2 railings; a finding *"Revit would not make 18 risers of 177.8 mm with 'Monolithic Stair' ... so the stair was made with 19"*. Checked in 3D and a section; Ctrl+Z once removes all of it |
| **HS2** | *Positive - side stringers (5b-446).* HS1's values with stairType=190mm max riser 250mm going | Made: 17 risers of 188.2 mm (9 + 8), a finding that the runs sit 50 mm in from the west and east sides, and in plan the stringers touch the hall's faces and do not cross them |
| **HS3** | *Revit's words (5b-1240), after the add-in is deployed.* A stair Revit refuses twice - e.g. HS1's values on a model whose Level 2 is at 3200 and in which 19 risers of `Monolithic Stair` are also refused; if none can be found, the preview of HS1 with a scratch fragment that forces 18 risers, rolled back | The refusal ends with *"Revit would not let it through: 'Can't make stairs' ..."* - Revit's own text. On an add-in from before this change the same refusal quotes nothing, and says nothing wrong |
| **HS4** | *Negative - a refusal leaves nothing (5b-1241).* HS1's values with runWidth=2500, kept (not a preview) | Refused *"Two runs side by side need 5000 mm across and the hall is 4830 mm"*, nothing made, `leftBehind` 0, and the run's record (`heron_run_record`) counts **no** element added - version 3 recorded "2 added ((no category) 2)". Then any refusal that reaches Revit (HS3's) and the same: no element added in its record |
| **HS5** | *Negative - by name.* HS1's values with stairType=Spiral Feature Stair | Refused naming the loaded stair types, nothing made, `railings` empty |
