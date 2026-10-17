# Needs checking — Group FU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FU - one Object Styles row by name: `LIST_CATEGORIES` version 3, `categories` narrows the answer, a missing name gets the nearest names (2026-10-17)

To check one subcategory's line colour in a family - Generic Annotations > Signature, before and after
`CREATE_LINE_STYLE` recoloured it - `LIST_CATEGORIES` version 2 printed every Object Styles row, all 303 of them,
each time ([FRAGMENT-ISSUES 5b-630](../fragment-issues/section-5b-rows-401-425.md)). (Groups up to FP were taken by other
groups, merged or open, when this was written - FE on another branch - and more sessions were running, so this group skipped to FU.)

**What changed.** `LIST_CATEGORIES` (FRG-DOC-031, a READ) takes an optional `categories`: the categories and
subcategories to report, comma separated - *"Generic Annotations"* is that category's own row and every
subcategory of it, *"Generic Annotations > Signature"* is that one row, and a bare name no category has is every
subcategory of that name. Capitals and extra spaces are forgiven. Left out it is *"all"* (`absent: all` on the
card), and *"all"* or an empty value is version 2's answer exactly. A name the document has not got gets the
nearest names it has and **no table** - one line, no column line - so it never reads as "nothing there". A new
count, `styleRows`, says how many rows came back. `CREATE_LINE_STYLE` is unchanged; its card now says its
`readBack` already shows a recoloured subcategory's colour, weight and pattern, so a chat need not read again
after a recolour.

**What is proved and what is not.** **Nothing below has run in Revit.** Measured without Revit on 2026-10-17:
every fragment compiles on all eight releases, 2020 to 2027; the narrowing code itself, cut out of the fragment
unchanged and run against stand-in categories, gave one row for *"Generic Annotations > Signature"*, the same
row for *"  generic   annotations>SIGNATURE "*, and for *"Generic Models > Signature"* in a family with no
subcategory `styleRows` 0 and one line with no column line; both job files read `1 WOULD RUN` under
`batch-prove --dry-run`. **What only Revit can settle:** that Revit's own names and subcategories behave as the
stand-ins did (FU2, FU3), and that the nearest names a real project offers are useful (FU5).

Never on a working family or project, never on the modeller's own signature family, and **never on the 4355 job
model**. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null` on every line. The live Heron reads `main`'s cards, so until this is merged version 3 runs through
the bridge client from the branch.

| # | Run | Look for |
|---|---|---|
| **FU1** | Arrange on **Revit 2020**: "Heron Object Styles Test" and "Heron No Subcategory Test" as the header of [`list-categories-narrowed-2026-10-17.yaml`](../../tools/jobs/list-categories-narrowed-2026-10-17.yaml) describes - [Group DZ](group-dz.md)'s two scratch families, so one arrangement serves both groups | Manage > Object Styles in each, read and written down BEFORE the run: Generic Annotations > Signature 0,0,255, 2, Solid; Generic Models with no row under it |
| **FU2** | `python tools/batch-prove.py tools/jobs/list-categories-narrowed-2026-10-17.yaml` | Positive: `styleRows` 1 and `objectStyles` *"Object Styles, 1 row(s) ... Only the name(s) asked: Generic Annotations > Signature."*, the column line, then ONE row, *"Generic Annotations > Signature \| 0,0,255 \| 2 \| - \| Solid"*. Negative: `styleRows` 0, `familySubcategories` 0 and ONE line, *"... Generic Models has no subcategory "Signature" - it has no subcategories at all ..."*, with no column line. **The runner will most likely print `NEG NOT EMPTY`** naming `names`, `bindable` and `objectStyles`, which are filled in every document - read those three numbers above in the run record; they decide it |
| **FU3** | In "Heron Object Styles Test", `categories` set to *"Generic Annotations"*, then to *"  generic   annotations>SIGNATURE "* | 2 rows (Generic Annotations and its Signature), then the same 1 row as FU2 - capitals and spaces forgiven |
| **FU4** | The same family, `categories` *"Signiture"* (misspelt), then *"Generic Annotations > Stamp"* | `styleRows` 0, one line and no column line each time; the nearest names open with *Generic Annotations > Signature* |
| **FU5** | A scratch **project**, `categories` *"Hidden Lines"*, then *"Walls > Common Edges, Doorz"* | First: one row per category that has a Hidden Lines subcategory, each as *"Category > Hidden Lines"*. Second: the Walls > Common Edges row, and the line naming *"Doorz"* with Doors among its nearest names. Record whether the nearest names would have helped a modeller |
| **FU6** | The same project, `categories` left out | the same answer version 2 gives - DZ4's - and `styleRows` equal to its row count |
| **FU7** | The second route: Manage > Object Styles > Annotation Objects in "Heron Object Styles Test", read by eye against FU2's one row | Line Weight, Line Color and Line Pattern the same - a difference is a defect in the read, not in the dialog |
| **FU8** | `CREATE_LINE_STYLE` with `familyCategory` and `updateExisting`, styleName *"Signature"*, colour *"255,0,0"*, on the same scratch family, through the bridge client's `validate`, which rolls a change back | the run's `readBack` says *"colour 255,0,0, weight 2, pattern Solid"* - the card's claim that no second read is needed after a recolour. Afterwards FU2's positive again: still 0,0,255, because the change was rolled back |
| **FU9** | After the merge, on the live Heron: `heron_lookup` with *"what colour is the Signature subcategory"*, *"what is the object style of walls"* and *"show the object style of Generic Annotations"* | the first two answer LIST_CATEGORIES. The third is expected to reach REMAP_LINE_STYLES on a PC without the trained model - recorded in row 5b-630, not fixed - and LIST_CATEGORIES with it |
| **FU10** | FU2 on **Revit 2024** and **2027** | the same answers - compiled on all eight, run on none |
| **FU11** | The signature, on the owner's word only, after FU2 to FU8 and Group DZ | `python brain/heron_validate.py accept list-categories --by "Ajmal PS"` |
