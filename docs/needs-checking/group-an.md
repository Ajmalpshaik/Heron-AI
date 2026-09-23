# Needs checking — Group AN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AN - family creation, from an empty template to a flexed family (2026-09-24)

**The owner's PC, Revit 2024 first, then 2020.** Nine fragments and one skill, written on 2026-09-24
when the owner asked for his earlier brain's family-creation skill to become part of Heron - package
C11 of [the earlier-brain plan](../work-notes/plans/earlier-brain/02-work-packages.md), taken ahead of
C7 and C9 on his word:

- [`set-family-category`](../../brain/fragments/set-family-category/fragment.yaml) - `SET_FAMILY_CATEGORY`, FRG-DOC-035
- [`add-family-parameters`](../../brain/fragments/add-family-parameters/fragment.yaml) - `ADD_FAMILY_PARAMETERS`, FRG-PAR-022
- [`set-family-type-values`](../../brain/fragments/set-family-type-values/fragment.yaml) - `SET_FAMILY_TYPE_VALUES`, FRG-PAR-023
- [`set-family-formula`](../../brain/fragments/set-family-formula/fragment.yaml) - `SET_FAMILY_FORMULA`, FRG-PAR-024
- [`create-reference-planes`](../../brain/fragments/create-reference-planes/fragment.yaml) - `CREATE_REFERENCE_PLANES`, FRG-GEO-034
- [`label-family-dimension`](../../brain/fragments/label-family-dimension/fragment.yaml) - `LABEL_FAMILY_DIMENSION`, FRG-GEO-035
- [`extrude-between-planes`](../../brain/fragments/extrude-between-planes/fragment.yaml) - `EXTRUDE_BETWEEN_PLANES`, FRG-GEO-036
- [`add-family-connector`](../../brain/fragments/add-family-connector/fragment.yaml) - `ADD_FAMILY_CONNECTOR`, FRG-MEP-053
- [`flex-family`](../../brain/fragments/flex-family/fragment.yaml) - `FLEX_FAMILY`, FRG-QA-033
- [`family-creation`](../../brain/skills/family-creation.yaml) - the skill that lists them in order

**What is already known, and it is NOT a proof.** All nine compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-09-24) and route their own sentences. **None has met a
model.** Every one is `DRAFT`, and each `tests/cases.yaml` names the negative case its proof owes.

**The arrangement is one family, built by the rows in order**, so each row is the setup for the next:
File, New, Family, the metric Generic Model template, left open in front - a new family nobody needs,
saved nowhere until AN9 has passed.

| # | Check | Expected |
|---|---|---|
| **AN1** | `set-family-category`, categoryName `Air Terminals` | `category` Air Terminals, read in Family Category and Parameters; `parametersGained` above zero, matching the flow fields in Family Types. **Negative:** `Air Terminal` refused with `Air Terminals` offered |
| **AN2** | `add-family-parameters`: `Width, Depth, Height, Neck Width, Neck Depth, Neck Height`, Length, type, group empty | Six type lengths under Dimensions in Family Types, all 0. **Negative:** the same again adds nothing; kind `Lenght` refused with nothing added |
| **AN3** | `set-family-type-values`: type `600 x 600`, `Width=600; Depth=600; Height=250; Neck Width=300; Neck Depth=300; Neck Height=100` | `typeCreated` true and the six values in Family Types. **Negative:** `Width=600mm` refused, nothing written |
| **AN4** | `create-reference-planes`, three calls: Box Left/Right at -300/300 left-right; Box Front/Back -300/300 front-back; Box Top 250 and Neck Top 350 height - then Neck Left/Right and Neck Front/Back at -150/150 | Every plane MEASURED where it was asked, in plan or elevation. **Negative:** Box Left again at -400 refused, and Box Left not moved |
| **AN5** | `label-family-dimension`: Width across Box Left/Right with `Center (Left/Right)`; Depth with `Center (Front/Back)`; the neck's two the same way; Height from `Ref. Level` to Box Top; Neck Height from Box Top to Neck Top | Each labelled dimension reads its value; typing Width 800 in Family Types moves BOTH box planes 100 mm, centred. **Negative, and the one that matters:** a label asked while its parameter still reads 0 is refused with both numbers and the planes NOT collapsed |
| **AN6** | `extrude-between-planes`: the body (Box sides, `Ref. Level`, Box Top), then the neck (Neck sides, base Box Top, top Neck Top) | 600 x 600 x 250 and 300 x 300 x 100 read back; five closed padlocks on each box, LOOKED AT |
| **AN7** | `add-family-connector`: facePlane Neck Top, system `Supply Air`, sizes `Neck Width, Neck Depth` | A rectangular Supply Air connector reading 300 x 300 mm, pointing up, selected and read in Properties. **Negative:** sizes empty refused; facePlane Box Top refused as two faces |
| **AN8** | `set-family-formula`: Neck Width = `Width - 300`, then the same with `"Width"` in quotes, then cleared | 300 mm and greyed in Family Types; the quoted one refused before Revit is asked; cleared leaves 300 editable |
| **AN9** | `flex-family`, two trials - `Width=900; Depth=500`, then `Width=400; Depth=700; Neck Width=250` - written with a pipe between them | `allHeld` true, `restored` true, the solid 900 x 500 then 400 x 700, centred, base at 0, the connector following Neck Width. **Negative:** the same flex on a copy whose body was drawn by hand WITHOUT locks - NOT HELD, naming planes that moved under a solid that did not |
| **AN10** | AN1 to AN9 on **Revit 2020** | The same answers. AddParameter and a parameter's kind take the OLDER calls there (by reflection), so 2020 is a different path, not a repeat |
| **AN11** | The `family-creation` skill asked for in one sentence in a chat - "make an air terminal family 600 by 600 with a 300 neck and a supply air connector" - on a fresh template | The host reads the skill's order and runs the nine in it, asking for any size not given. Record where it strays from the order - that is the case for D2's `steps:` list |

**Not in this group, because not built:** void cuts, round shapes, materials, subcategories and Is
Reference - the skill says so in its own words.
