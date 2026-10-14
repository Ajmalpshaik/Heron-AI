# Needs checking — Group DZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-14 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DZ - Object Styles read back: `LIST_CATEGORIES` version 2, each category's and subcategory's line colour, weights and pattern (2026-10-14)

**What changed.** `LIST_CATEGORIES` (FRG-DOC-031, a READ) reports, besides version 1's names, bindable count and
sub-category total, `objectStyles`: one row per line for every category - and, in a family open in the Family
Editor, the family's own category and every subcategory of it first - with its projection line colour, projection
line weight, cut line weight where the category is cut and projection line pattern, read off the category itself
as Manage > Object Styles shows them. `familySubcategories` counts the family's own subcategories. An optional
`everySubcategory` lists every other category's subcategories too; left out it is false, and names, bindable and
the first finding read exactly as version 1's. Before this nothing read a category's own graphics, so questions
about them were answered by writes ([FRAGMENT-ISSUES 5b-464](../fragment-issues/section-5b-rows-401-425.md)).

**What is proved and what is not.** **Nothing below has run in Revit.** Measured without Revit on 2026-10-14: every
fragment compiles on all eight releases, 2020 to 2027; every member version 2 adds was read off
`tools/api-surface` for all eight; the job file reads `1 WOULD RUN` under `batch-prove --dry-run`. **Two things
are claims until a run settles them:** that an internal category has no Object Styles row (DZ6), and that a
project's one-row-per-category answer is small enough to carry in a reply (DZ4).

Never on a working family or project, and never on the modeller's own signature family. With
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null` on every
line. The live Heron reads `main`'s cards, so until this is merged version 2 runs through the bridge client from
the branch.

| # | Run | Look for |
|---|---|---|
| **DZ1** | Arrange on **Revit 2020**: "Heron Object Styles Test" and "Heron No Subcategory Test" as the header of [`list-categories-object-styles-2026-10-14.yaml`](../../tools/jobs/list-categories-object-styles-2026-10-14.yaml) describes - a Generic Annotation family with one subcategory "Signature" set to 0,0,255, weight 2, Solid, and a Generic Model family with nothing under its category | Manage > Object Styles in each, read and written down BEFORE the run: Generic Annotations > Signature 0,0,255, 2, Solid; Generic Models with no row under it |
| **DZ2** | `python tools/batch-prove.py tools/jobs/list-categories-object-styles-2026-10-14.yaml` | PASS. Positive: `familySubcategories` 1, `objectStyles` opening *"This family's own category, Generic Annotations, and its 1 subcategory(ies) are the first 2 row(s)"*, then the column line, then the Generic Annotations row and *"Generic Annotations > Signature \| 0,0,255 \| 2 \| - \| Solid"*. Negative: `familySubcategories` 0, *"it has no subcategories, so its own row stands alone"*, and no *"Generic Models > "* row |
| **DZ3** | The second route: Manage > Object Styles in "Heron Object Styles Test", Annotation Objects tab, read by eye against DZ2's two rows | Line Weight, Line Color and Line Pattern the same on both rows - a colour or weight that differs is a defect in the read, not in the dialog |
| **DZ4** | A scratch **project**, `everySubcategory` left out | one row per category and NO row with " > " in it; the first line naming how many subcategories were not listed; names, bindable and the first finding the same sentence version 1 printed. Record the reply's length in characters - one row per category has to fit a reply |
| **DZ5** | The same project, `everySubcategory` true | Walls > Common Edges and Doors > Frame/Mullion among the rows; a number in the cut column for Walls and "-" for an annotation category such as Generic Annotations; nothing in the first line about subcategories left out. Record the reply's length - this is the long answer |
| **DZ6** | DZ4's top-level rows counted against Object Styles' own tabs in the same project, Model + Annotation + Analytical Model + Imported added up | the same number. A difference is recorded with the tab the extra or missing rows sit on: it means the "internal categories have no row" rule is wrong |
| **DZ7** | After the merge, on the live Heron: `heron_lookup` with *"what colour are the lines in this family"*, *"what colour is the signature subcategory"*, *"what line weight is the hidden lines subcategory"* and *"list the object styles in this family"* | each answered LIST_CATEGORIES - a READ - not a write |
| **DZ8** | DZ2 on **Revit 2024** and **2027** | the same answers - compiled on all eight, run on none |
| **DZ9** | The signature, on the owner's word only, after DZ2 to DZ6 | `python brain/heron_validate.py accept list-categories --by "Ajmal PS"` |
