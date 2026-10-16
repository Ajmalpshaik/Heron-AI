# Needs checking — Group ED

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-15 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group ED - a view's whole Visibility/Graphics read back: `REPORT_CATEGORY_OVERRIDES` and `AUDIT_VIEW_FILTERS` version 2 (2026-10-15)

Found 2026-10-15 when the owner asked Heron to study the Visibility/Graphics of a plan in a real HVAC
project on Revit 2020 ([row 5b-480](../fragment-issues/section-5b-rows-401-425.md)). Two existing reads
were widened, each by an **optional bool**, so every caller and job file written before stays as it was:

- [`REPORT_CATEGORY_OVERRIDES`](../../brain/fragments/report-category-overrides/fragment.yaml) (FRG-VIEW-051)
  version 2 - `allCategories` true leaves `modelCategoryList`, `subCategoryList`, `annotationCategoryList`
  and `elementOverrideList`, one string each.
- [`AUDIT_VIEW_FILTERS`](../../brain/fragments/audit-view-filters/fragment.yaml) (FRG-VIEW-078) version 2 -
  `everyFilter` true leaves `filterList`, every filter in priority order with its rule, categories, catches
  and override.

Both cards are **DRAFT**: the change made version 1's proofs stale, and `check-signatures` lists them under
STALE. Both compile for all eight releases, 2020 to 2027.

**Already run, read only, and NOT a proof.** On 2026-10-15 both were run from this branch's source through
`mcp/client/heron_bridge_client.py fragment` (`run_fragment_read`, no transaction) on the real project's
Ground Floor HVAC Airducts plan, Revit 2020, session 25244. `REPORT_CATEGORY_OVERRIDES` with `allCategories`:
85 model categories rows of the dialog, 12 hidden, 77 overridden, 520 sub-category rows, 2 annotation rows,
no per-element override - the same 85 / 12 / 77 a scratch probe outside the repository counted, plus two
annotation sub-categories the probe never walked. `AUDIT_VIEW_FILTERS` with `everyFilter`: all 21 filters,
an inverse rule written `NOT ... contains`, a filter with no rule read as every element of its categories.
**With each flag left out, both answered line for line what `main`'s code answered on the same view**, the
only additions the empty new strings and the flag reading `false`. It is a real project, not a test model,
so no value from it is in this repository and none of it can be signed: D-30 wants the arranged model below.

### The test project - one for the whole group

**"Heron VG Study Test"**, a new project from the default **Mechanical** metric template (it carries the Supply Air and Return Air duct systems), Revit 2020 (any release
2020 to 2027 will do), saved under that name, and a second project **"Heron VG Study Link"**:

| Part | What to model | Used by |
|---|---|---|
| *link* | "Heron VG Study Link": four walls on Level 1 making a 10 x 8 m room, one door. Saved, then linked into "Heron VG Study Test" Origin to Origin. **The test project has no wall of its own** - that is the case version 1 could not see | ED1, ED3 |
| *view* | In "Heron VG Study Test", "FloorPlan: Level 1" with **no view template**. Visibility/Graphics, Model Categories: **Walls** projection lines colour 128,128,128 weight 1 and Halftone; **Doors** unticked (hidden); one sub-category of **Ducts** (expand the row - Center Line, Drop or Rise, whichever the release has) projection lines colour 255,0,0; Annotation Categories: **Reference Planes** unticked | ED1, ED3 |
| *ducts* | Three rectangular ducts on Level 1, drawn in that plan: two on a **Supply Air** system, one on a **Return Air** system. Comments `A` on one Supply duct, nothing on the others. Select the two Supply ducts, Override Graphics in View > By Element, projection lines colour 0,0,255 | ED1, ED2, ED3 |
| *filters* | Four filters made in Manage > Filters, all on category Ducts, applied to that plan in this order: **Heron VG Supply** (System Classification equals Supply Air) projection lines colour 0,0,255 weight 5; **Heron VG Not Return** (System Classification does not contain `Return`) Halftone; **Heron VG Nothing** (Comments equals `HERON NO SUCH VALUE`) projection lines colour 255,0,0; **Heron VG Or** - one OR rule set, Comments equals `A` OR Comments equals `B` - with its **Visibility unticked** | ED2, ED3 |
| *plain* | "FloorPlan: Level 2" with nothing overridden, no filter and no view template | ED3 |

| # | Run | Look for |
|---|---|---|
| **ED1** | `python tools/batch-prove.py tools/jobs/report-category-overrides-all-categories-2026-10-15.yaml`, then read [`brain/proof-drafts/report-category-overrides.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept report-category-overrides --by "Ajmal PS"` and set `heron-status: PROVEN` | positive (`allCategories` true): **Walls in `modelCategoryList` with colour 128,128,128, weight 1 and halftone and NO "in view" count** - the host has no wall - while `overrideList` does not name Walls; Doors `HIDDEN`; the Ducts sub-category in `subCategoryList` with 255,0,0; Reference Planes `HIDDEN` in `annotationCategoryList`; `elementOverrideList` one group, `2 x Ducts: projection lines colour 0,0,255`, with both ids. Negative (`allCategories` false, the same view): the four strings **empty**, and `findings`, `overriddenCategories` and `overrideList` what version 1 gave |
| **ED2** | `python tools/batch-prove.py tools/jobs/audit-view-filters-every-filter-2026-10-15.yaml`, then the same three steps for `audit-view-filters` | positive (`everyFilter` true): four rows in the order the Filters tab lists them (on 2020 the heading says that release cannot give the order - compare by hand); Heron VG Supply catching **2**; Heron VG Not Return's rule **`NOT System Classification contains 'Return'`** - never plain `contains` - catching **2**; Heron VG Nothing **catches 0**; Heron VG Or's rule **bracketed with OR**, `Visibility OFF`. Negative (`everyFilter` false): `filterList` **empty** and the rest version 1's |
| **ED3** | By hand, the same model, through `revit_read` | the second route: open Visibility/Graphics on Level 1 and read each row of ED1 and ED2 against the dialog - Model Categories, Annotation Categories, Filters. Then `REPORT_CATEGORY_OVERRIDES` with `allCategories` true on **Level 2**: only categories with host elements listed, each `V/G: no override`, and 0 sub-category and 0 annotation rows - **a version that read every Visible tick as set would list every category**. Then a sheet as the view: version 1's "does not accept graphic overrides" and every new string empty |
| **ED4** | **Record, do not assume:** Heron VG Or's `catches` with its Visibility unticked | the count reads **0 or 1**. `filterList` calls a count "host element(s) drawn here"; if a view-scoped collector still counts an element a hidden filter hides, that wording is wrong for a HIDDEN filter and is to be fixed - write the number in [row 5b-480](../fragment-issues/section-5b-rows-401-425.md) |
| **ED5** | Revit 2021 or later (Snowdon Towers Sample HVAC's templated plans will do) | `filterList` in the dialog's order with **Enable Filter** on every row; on a view whose template owns the filters, the heading names the template; on a view whose template owns V/G, `modelCategoryList`'s heading names it too |
| **ED6** | **Two sources disagree - look, do not assume.** The same model's Level 1 plan, the link's Display Settings left By Host View | whether the LINKED walls draw grey 128,128,128 and halftone. [`SET_CATEGORY_GRAPHICS`](../../brain/fragments/set-category-graphics/fragment.yaml)'s routing comment says a category override on a linked job *"greys nothing"*, the host having no walls; the 21S10 plan overrides 77 categories its host draws almost none of, and that is the plan's grey background. Write which is true in [row 5b-480](../fragment-issues/section-5b-rows-401-425.md) - it decides whether `modelCategoryList`'s heading, which says such an override still acts on linked models, stands |
