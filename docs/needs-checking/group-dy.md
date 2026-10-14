# Needs checking — Group DY

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-14 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DY - a family's own subcategory given a colour, a weight and a pattern: `CREATE_LINE_STYLE` version 2, `familyCategory` (2026-10-14)

**What changed.** `CREATE_LINE_STYLE` (FRG-VIEW-126) takes an optional `familyCategory`. True, in a family open in
the Family Editor, it makes or changes a subcategory of the family's own category - Manage > Object Styles there,
the Annotation Objects or Model Objects tab - with the colour, projection weight and pattern a line style takes,
`updateExisting` meaning what it meant; the category's own name, or "none", is the category itself. Left out, it
is a line style under Lines, as version 1 ([FRAGMENT-ISSUES 5b-463](../fragment-issues/section-5b-rows-401-425.md)).
The first use: the blue of the modeller's signature, drawn by `DRAW_FAMILY_DETAIL_LINES` on Generic Annotations >
Signature and set by hand on 2026-10-14. `detail-family-creation` step 3 and `family-creation` step 12 now name it.

**What is proved and what is not.** **Nothing below has run in Revit.** Measured without Revit on 2026-10-14:
every fragment compiles on all eight releases, 2020 to 2027; the members it adds were read off
`tools/api-surface` for 2020, 2024 and 2027; the job file reads `1 WOULD RUN` under `batch-prove --dry-run`, and
Group DL's job still builds the same command for version 1's line-style leg; `heron_method` serves both methods
with `CREATE_LINE_STYLE` and nothing missing on Revit 2020; routing was measured against `main` on both backends
and is recorded in the row.

Never on a working family, and never on the modeller's own signature family. With `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null` on every line. The live Heron reads
`main`'s cards, so until this is merged version 2 runs through the bridge client from the branch, as Group DX's did.

| # | Run | Look for |
|---|---|---|
| **DY1** | Arrange on **Revit 2020**: "Heron Signature Test" as the header of [`line-style-family-category-2026-10-14.yaml`](../../tools/jobs/line-style-family-category-2026-10-14.yaml) describes - a new Metric Generic Annotation family, three or four lines drawn by `DRAW_FAMILY_DETAIL_LINES` with subcategory `Signature` | Manage > Object Styles > Annotation Objects lists Generic Annotations > Signature, black, weight 1, Solid |
| **DY2** | `python tools/batch-prove.py tools/jobs/line-style-family-category-2026-10-14.yaml` | positive: `madeNew` false, `readBack` *"colour 0,0,255, weight 1, pattern Solid"*, *"Updated subcategory "Signature" of Generic Annotations"*; negative (`updateExisting` false): refused, *"already has a subcategory named "Signature""*, `style` empty. Object Styles afterwards: Signature black again - rollback has failed on this PC before (BD1) |
| **DY3** | A scratch PROJECT in front: the same call, `styleName` "Heron Signature Test", `familyCategory` true - every value given, as in the job file; `weight` and `patternName` are never optional | refused, *"is a project, not a family open in the Family Editor"*; Manage > Additional Settings > Line Styles has no "Heron Signature Test" |
| **DY4** | DY2's positive KEPT, through `revit_change` in the test family | the lines blue in the family's view and Signature 0,0,255 in Object Styles - the second route - and one Ctrl+Z puts it back in one step |
| **DY5** | In the test family, `styleName` "Heron New Sub", colour 255,0,0, weight 3, `patternName` "Solid", `familyCategory` true | `madeNew` true, `readBack` *"colour 255,0,0, weight 3, pattern Solid"*, a new row under Generic Annotations. Run it once more with `patternName` "" and read what Revit holds for a pattern nobody set - *Solid* or *(none)* |
| **DY6** | In the test family, `styleName` "none", colour 0,128,0, weight 0, `patternName` "", `updateExisting` true, `familyCategory` true | *"Updated the family's own category "Generic Annotations""*, the category's own row green in Object Styles |
| **DY7** | In a Generic Model family, `styleName` "Heron Model Sub", colour 0,0,255, weight 2, `patternName` "", `familyCategory` true | Object Styles > Model Objects lists it with projection weight 2; its cut weight is Revit's default - this tool does not set one |
| **DY8** | Version 1 unchanged: Group DL's DL2 line-style leg, `familyCategory` left out | made then refused exactly as DL2 says |
| **DY9** | DY2 on **Revit 2024** and **2027** | the same answers - compiled on all eight, run on none |
| **DY10** | The signature, on the owner's word only, after DY2 to DY8 | `python brain/heron_validate.py accept create-line-style --by "Ajmal PS"`. `accept` reads ONE run record, `brain/proof-drafts/runs/create-line-style.json`, and DL2's job and DY2's each write it - sign from the run that is meant |
