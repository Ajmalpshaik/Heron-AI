# Needs checking — Group MR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MR - renaming views on as-built sheets: a placed floor plan `FIND_VIEWS` could not find, and a done rename `EDIT_TEXT_VALUES` called unverified - `FIND_VIEWS` v3, `EDIT_TEXT_VALUES` v9, `RENUMBER_SEQUENTIAL` v3, and quoted values keep their spaces (2026-11-07)

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
| `RENUMBER_SEQUENTIAL` v3 (MODIFY) | [`brain/fragments/renumber-sequential`](../../brain/fragments/renumber-sequential/fragment.yaml) | Optional `width`: the number zero-padded, never cut; left out, the bare number as v2. Fixed on the owner's word the same day, row 5b-215's first repair; was PROVEN at v2 |

**What is done and what is not.** All three cards compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-07, a Linux container). All three cards' purpose and utterances are their old ones word for word, the new rules written as comments, because a purpose paragraph moved routing on the model backend; `check-routing` (model) prints the same as on main and a 22-sentence lexical `score-routing` set changes 0 answers. **None of the three has met a Revit model at its new version.** **Not settled by reading:** that Revit 2020 to 2027 all keep a view name without its
edge spaces. The field run says Revit 2020 does - the four names read back without the space - and MR4 checks it.

| # | Run | Look for |
|---|---|---|
| **MR0** | *Open.* Revit 2020 (where it was found), then once on 2024. A **saved test copy** shaped like `4355-BHVD-3D-60A10-BL007A` - never the job model. Changes on at the ribbon | The title bar names the copy |
| **MR1** | *Arrange, and KEEP it.* A floor plan "PLAN TEST (Battery Room)", Duplicate as Dependent on it so the copy is "PLAN TEST (Battery Room) Copy 1", the copy placed on a sheet; a section "SECTION TEST"; four section views "Warehouse - HVAC Air ducts Sections-1" to "-4"; a view "X - Plan A" and a view "Plan A"; one wall with Comments "Warehouse - A" | All in the Project Browser; the copy shows its sheet number |
| **MR2** | *FIND_VIEWS - D-30 pair, NEGATIVE FIRST.* Negative: viewType "Floor Plan", nameContains "SECTION TEST"; then viewType "Flor Plan". Positive: viewType "Floor Plan", nameContains "(Battery Room) Copy 1"; then viewType "any", nameContains "Battery Room" | Negative: 0, `skippedOtherType` at least 1, the section named *"a Section, and FloorPlan was asked"*; the misspelling REFUSED, NOTHING WAS RETURNED. Positive: the dependent copy returned, its id in `onSheets`; with "any", both plans. Every answer carries *"Read viewType as ...; passed over ..."*. **Row 5b-1474's case** - version 2 found 0 both ways |
| **MR3** | *FIND_VIEWS spellings.* viewType "floor plans", "FLOOR-PLAN", "FloorPlan", "3D View", "Reflected Ceiling Plan" | The first three the same views; the last two the 3D views and ceiling plans, as the browser's branches |
| **MR4** | *EDIT_TEXT_VALUES - D-30 pair, through Keep or Discard.* Positive: the four "Warehouse - ..." sections, field Name, mode replace, text "Warehouse - ", replacement empty, through `revit_change` - once typed bare (`text=Warehouse - `, the space stripped, row 5b-215) and once quoted (`text="Warehouse - "`, the space kept since 2026-11-07). Negative: the wall, field Comments, mode replace, text "Warehouse -", replacement empty | Positive: `changed` 4, `unverified` 0, the names read back "HVAC Air ducts Sections-1" to "-4", `nameNote` naming 4 for the bare run; the quoted run the same with NO `nameNote` - nothing to trim. **Row 5b-1475's case** - version 8 said changed 0, unverified 4. Negative: changed 1, Comments read back " A" with its space, no `nameNote` |
| **MR5** | *EDIT_TEXT_VALUES clash on the kept name, rolled back.* "X - Plan A", field Name, mode replace, text "X -", replacement empty | REFUSED, NOTHING WAS RENAMED, `collisions` naming "Plan A" as the other view's name |
| **MR8** | *RENUMBER_SEQUENTIAL - D-30 pair, through Keep or Discard.* Fourteen rooms, parameterName Name, prefix `"Office "` (quoted), startAt 1, width 2. Negative: the same with width 12 | Positive: Office 01 ... Office 09, Office 10 ... Office 14 in ONE call, renumbered 14 - **row 5b-215's case**, two passes and a one-room fix on 2026-09-24. Negative: renumbered 0, nothing written, `widthRefused` naming 12 |
| **MR7** | *Sign.* Ajmal PS signs the three proof drafts and says whether each goes back to PROVEN | — |

**The wire strip (row 5b-215) is fixed in the same pull request**: a value in double quotes keeps its spaces through `revit_change` and `revit_read`. MR4's quoted run is its first check through a live chat.

**`OPEN_VIEW` and the add-in's view lookup are not this group's.** Both were fixed here first, then by PR #804 the same day ([row 5b-1468](../fragment-issues/section-5b-rows-401-425.md)'s `TypeWordNames`, and `OPEN_VIEW` version 4 for row 5b-1470 (a)) with the same reading plus plurals. At the merge this branch took #804's code for both, so their proof is #804's, and "Floor Plan: name" for any tool is checked there.

## 2026-11-08 - `EDIT_TEXT_VALUES` v9 PROVEN, by a different case than MR4

Run on `RenameProof-rme` - a copy of `rme_advanced_sample_project.rvt` in Revit 2020, never the job model - with `tools/batch-prove.py`, both legs rolled back. Positive: section view "Section 18" (found by `FIND_VIEWS`), field Name, mode replace, text "Section", replacement "HVAC Air Duct Section" - `changed` 1, `unverified` 0. Negative: the same view, text "ZZZNOTHINGHERE" - `changed` 0, `notMatched` 1. Signed by Ajmal PS and PROVEN the same day. It was run for the owner's rename of seven sections on `4355-BHVD-3D-22S10-BL001A`.

**Still open: MR4's own case** - the edge-space trim and `nameNote` were not exercised (no new name here had a space to lose), and MR5's clash. **Learned:** a viewport's "View Name" answered `ambiguous` 3 on sheet E301 - two parameters of that name on a viewport - so a rename goes to the views, never their viewports. `FIND_VIEWS` (MR2, MR3) and `RENUMBER_SEQUENTIAL` (MR8) are untouched by this.
