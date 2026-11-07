# Needs checking — Group MR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MR - renaming views on as-built sheets: a placed floor plan `FIND_VIEWS` could not find, and a done rename `EDIT_TEXT_VALUES` called unverified - `FIND_VIEWS` v3, `EDIT_TEXT_VALUES` v9, and the add-in's "Floor Plan: name" (2026-11-07)

On 2026-11-07, on `4355-BHVD-3D-60A10-BL007A` in Revit 2020, the owner renamed views on as-built sheets. Every
rename landed and was read back, but two tools misreported. `FIND_VIEWS` found 0 for a floor plan that was on a
sheet ([row 5b-1474](../fragment-issues/section-5b-rows-401-425.md)), and `EDIT_TEXT_VALUES` answered `changed 0`,
`unverified 4` for four views it had renamed correctly ([row 5b-1475](../fragment-issues/section-5b-rows-401-425.md)).
Both were PROVEN and are DRAFT, version raised, until this group proves them. **Never run on the job model** -
a saved test copy only.

| What | Where | What changed |
|---|---|---|
| `FIND_VIEWS` v3 (READ) | [`brain/fragments/find-views`](../../brain/fragments/find-views/fragment.yaml) | `viewType` read the Project Browser's way - spaces, hyphens, capitals and plurals ignored, "3D View", "Reflected Ceiling Plan", "Structural Plan", "Drafting View" mapped; "", "any", "all" every type; anything else refused. `skippedOtherType`, and a finding on every call naming how many views were passed over and why; an empty answer names the views whose name matched and why each was passed over |
| `EDIT_TEXT_VALUES` v9 (MODIFY) | [`brain/fragments/edit-text-values`](../../brain/fragments/edit-text-values/fragment.yaml) | For field Name, the new name trimmed before it is planned, checked for a clash, written and read back - Revit keeps no space at either end of a name. `nameNote` counts the names that lost one. Parameters compared exactly, as before |
| The add-in's view lookup | [`revit/Heron.Revit.Addin/RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) `OneView` | "Floor Plan: Level 1" is read as "FloorPlan: Level 1" for every tool that takes a view by name - spaces and hyphens taken out of the type before it is compared |

**What is done and what is not.** Both cards compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-07, a Linux container), and the add-in builds for all eight
(`tools/check-compile.py`, same day). Both cards' purpose and utterances are their old ones word for word, the new rules written as comments, because a purpose paragraph moved routing on the model backend; `check-routing` (model) prints the same as on main and a 22-sentence lexical `score-routing` set changes 0 answers. **Neither card has met a Revit model at its new version**, and the add-in
change has not been deployed. **Not settled by reading:** that Revit 2020 to 2027 all keep a view name without its
edge spaces. The field run says Revit 2020 does - the four names read back without the space - and MR4 checks it.

| # | Run | Look for |
|---|---|---|
| **MR0** | *Open.* Revit 2020 (where it was found), then once on 2024. A **saved test copy** shaped like `4355-BHVD-3D-60A10-BL007A` - never the job model. Changes on at the ribbon | The title bar names the copy |
| **MR1** | *Arrange, and KEEP it.* A floor plan "PLAN TEST (Battery Room)", Duplicate as Dependent on it so the copy is "PLAN TEST (Battery Room) Copy 1", the copy placed on a sheet; a section "SECTION TEST"; four section views "Warehouse - HVAC Air ducts Sections-1" to "-4"; a view "X - Plan A" and a view "Plan A"; one wall with Comments "Warehouse - A" | All in the Project Browser; the copy shows its sheet number |
| **MR2** | *FIND_VIEWS - D-30 pair, NEGATIVE FIRST.* Negative: viewType "Floor Plan", nameContains "SECTION TEST"; then viewType "Flor Plan". Positive: viewType "Floor Plan", nameContains "(Battery Room) Copy 1"; then viewType "any", nameContains "Battery Room" | Negative: 0, `skippedOtherType` at least 1, the section named *"a Section, and FloorPlan was asked"*; the misspelling REFUSED, NOTHING WAS RETURNED. Positive: the dependent copy returned, its id in `onSheets`; with "any", both plans. Every answer carries *"Read viewType as ...; passed over ..."*. **Row 5b-1474's case** - version 2 found 0 both ways |
| **MR3** | *FIND_VIEWS spellings.* viewType "floor plans", "FLOOR-PLAN", "FloorPlan", "3D View", "Reflected Ceiling Plan" | The first three the same views; the last two the 3D views and ceiling plans, as the browser's branches |
| **MR4** | *EDIT_TEXT_VALUES - D-30 pair, through Keep or Discard.* Positive: the four "Warehouse - ..." sections, field Name, mode replace, text "Warehouse - ", replacement empty, through `revit_change` (which strips the trailing space - row 5b-215). Negative: the wall, field Comments, mode replace, text "Warehouse -", replacement empty | Positive: `changed` 4, `unverified` 0, the names read back "HVAC Air ducts Sections-1" to "-4", `nameNote` naming 4. **Row 5b-1475's case** - version 8 said changed 0, unverified 4. Negative: changed 1, Comments read back " A" with its space, no `nameNote` |
| **MR5** | *EDIT_TEXT_VALUES clash on the kept name, rolled back.* "X - Plan A", field Name, mode replace, text "X -", replacement empty | REFUSED, NOTHING WAS RENAMED, `collisions` naming "Plan A" as the other view's name |
| **MR6** | *The add-in's view lookup.* Any tool that takes a view by name - `OPEN_VIEW` aside, below - given "Floor Plan: PLAN TEST (Battery Room)" where a ceiling plan of the same name exists | The floor plan, not "no view called". Before: "no view called" |
| **MR7** | *Sign.* Ajmal PS signs both proof drafts and says whether each goes back to PROVEN | — |

**Left, recorded in row 5b-1474:** `OPEN_VIEW` (PROVEN) has its own "kind: name" match with the same raw compare,
and stays as it is under the owner's hold on C# changes to PROVEN cards ([row 5b-1470](../fragment-issues/section-5b-rows-401-425.md)).
**Left, recorded in row 5b-1475:** the wire strip itself, row 5b-215 - an open decision, not this group's.
