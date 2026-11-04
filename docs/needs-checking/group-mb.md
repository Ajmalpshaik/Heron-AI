# Needs checking — Group MB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MB - Manage > Object Styles - one category's or subcategory's projection and cut line weight, line colour, line pattern and material, for the whole model, read back before and after: `SET_OBJECT_STYLE` (2026-11-02)

Asked for in the modeller's words: *"set the projection line weight of Doors to 3"*, *"change the cut line
colour of Walls to red"*, *"set the line pattern of Ducts to Dash"*, *"set the material of Generic Models to
Concrete"*, *"make the Hidden Lines subcategory of Ducts lineweight 1"* - and the question *"what are the
object style settings for Pipes"*. Nothing in Heron changed a category's own Object Styles:
`brain/heron_retrieve.py` on a scratch store sent the five changes to `LIST_CATEGORIES` (a read),
`CREATE_LINE_STYLE`, `CREATE_LINE_PATTERN` and `SET_MATERIAL_APPEARANCE`. `CREATE_LINE_STYLE` changes a
subcategory of Lines, or of a family's own category, and is PROVEN, so it was not widened. The question is
`LIST_CATEGORIES`' (PROVEN), declared on its card outside `impl/`.

| What | Where | What it does |
|---|---|---|
| `SET_OBJECT_STYLE` (MODIFY) | [`brain/fragments/set-object-style`](../../brain/fragments/set-object-style/fragment.yaml) | Changes **one row** of Manage > Object Styles - a category (`Doors`) or a subcategory (`Ducts > Hidden Lines`), named as this Revit names it - for the whole model: projection line weight and cut line weight (1 to 16), line colour (`R,G,B` or one of ten names the card lists), line pattern (by exact name, or `Solid`) and material (by exact name, model categories only). What is left out is kept. Refused by name, nothing changed: an unknown category or subcategory (the nearest names listed), an internal category, the Lines category (sent to `CREATE_LINE_STYLE`), a cut weight on a category Revit does not cut, a material on a category that is not a model category, a pattern or material the model has not got (the nearest listed), a weight outside 1 to 16, a colour it does not take, nothing to change. A write Revit refuses is thrown and the whole change rolled back. `before` and `after` read the row back; a value Revit did not keep is named NOT KEPT |

**Why MODIFY and not ADMIN.** Graded as the library grades the writes that touch these same settings:
`TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS` rewrites every row at once and `CREATE_LINE_STYLE` restyles
every line in a style, both MODIFY; ADMIN ([D-106](../DECISIONS.md)) is how the project is set up -
units, worksets, parameters, location. No element, value or view changes here, a view's override still
wins, and one Ctrl+Z takes it back.

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member it uses was looked
up with `tools/api-surface --members` and is 2020 to 2027 with one signature. **Nothing here has run in
Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (MB2 and MB3 measure them):
whether `Category.Material` accepts a material on every model category Object Styles shows a Material cell
for, and refuses the others by throwing; whether `SetLineWeight(..., Cut)` throws on a cuttable subcategory
whose cut cell Object Styles leaves empty; and whether a line colour set on a subcategory reads back at
once after one regeneration.

**Routing.** Measured with `tools/check-routing.py` and `heron_retrieve.find` on two scratch stores, before
and after the card: every sentence it declares reaches it by identity; the question reaches
`LIST_CATEGORIES` with and without the host's question flag
([row 5b-1425](../fragment-issues/section-5b-rows-401-425.md), FIXED). *"put it back"* moved from one write
it does not name to another, and a made-up paraphrase of a question moved onto this write without the
flag - [row 5b-1426](../fragment-issues/section-5b-rows-401-425.md), OPEN.

**The model - a scratch project from the metric architectural template, any release.** Never a working
model. Every run below is rolled back except MB1's arrangement and MB4's one run. The job file is
[`tools/jobs/set-object-style-mb-2026-11-02.yaml`](../../tools/jobs/set-object-style-mb-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MB0** | *Open.* Any release 2020 to 2027, a new project from the metric architectural template, Changes on at the ribbon | The title bar names the scratch project |
| **MB1** | *Arrange, and KEEP it.* One wall and one door on Level 1. Save. `LIST_CATEGORIES` with categories `Walls, Doors` - write down every row | "FloorPlan: Level 1" shows them; the Walls and Doors rows recorded |
| **MB2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: changed 0, *no line pattern named "ZZZ No Such Pattern"*, the nearest real names. Positive: changed 1, `after` *projection weight 5*, every other setting as MB1 recorded; Walls' subcategories and Doors unchanged |
| **MB3** | *Subcategories, colour, material and the refusals.* `Walls > Hidden Lines` with projectionWeight 1 and pattern `Dash`; `Walls` with colour `red` and cutWeight 4; `Generic Models` with a material the model has; `Generic Annotations` with cutWeight 3, then with a material; `Lines > Thin Lines`; `Wals`; `Walls > No Such Sub`; projectionWeight 17; colour `sky blue`; Walls with nothing else; Walls with the weight it already has | Hidden Lines alone changes, Walls keeps its row. Red reads `255,0,0` with the one-colour finding. Material named back - **record** whether Revit took it. Each refusal by name, nothing changed. The weight it already has: changed 0 |
| **MB4** | *Kept, through `try-once`, on the modeller's yes.* MB2's positive on the saved project, "Level 1" in front | Keep or Discard. Then Manage > Object Styles shows Walls' projection weight 5: the second route |
