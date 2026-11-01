# The schedule settings Heron could not set - what the Revit API reaches, and what was built

**2026-11-01.** Written for [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) row **5b-1367**. Ajmal PS made a
duct length schedule through Heron on his Revit 2020 job model 4355-BHVD-3D-60B10-ST001I: create, group
by size, Length totals and the grand total all worked. **Changing the Length column to metres failed** -
no Heron tool set a schedule column's units. He then asked for a careful check of every other schedule
setting Heron could not reach, built in the cloud, with the proving done afterwards in a local session
with him, on a scratch model.

> Nothing here is specification. Where it disagrees with a fragment's card, the card and the API lookup
> it names win - re-run the lookup. Every API line below was read with
> `dotnet run --project tools/api-surface -- --members <Type>` against the reference assemblies cached
> for **2020 through 2027**, on 2026-11-01. **Nothing here has been run in Revit.** What only a Revit run
> can settle is marked **NEEDS REAL REVIT**, and is in [NEEDS-CHECKING Group KK](../../needs-checking/group-kk.md).

## 1. Where each setting lives in the Revit API, and since when

Releases are the reference packages' (NuGet, one per release). "Reached by name" means the member is
not on every release, so the fragment finds it by reflection at run time: one implementation, the same
code on every release, as `SET_PROJECT_UNITS` does. (`#if REVIT20xx` would also work - the add-in has
handed fragments the release symbols since [row 5b-181](../../fragment-issues/section-5b-rows-176-200.md)'s
fix, and `PLACE_VIEW_ON_SHEET` uses it.)

| # | Setting, as the Schedule Properties dialog shows it | API | Releases |
|---|---|---|---|
| 1 | Formatting - Field Format - *Use project settings*, units, rounding, symbol | `ScheduleField.GetFormatOptions` / `SetFormatOptions`, `FormatOptions.UseDefault`; the column's unit type is `ScheduleField.UnitType` (2020-2021, obsolete 2021) or `GetSpecTypeId()` (2021 on) | 2020-2027; the units half split at 2021 - `DisplayUnitType`/`UnitSymbolType` on 2020, `ForgeTypeId` from 2021 - reached by name, the way `SET_PROJECT_UNITS` does |
| 2 | Formatting - Calculate totals / minimum / maximum / minimum and maximum | `ScheduleField.DisplayType` (`ScheduleFieldDisplayType` Standard, Totals, MinMax, Max, Min), `CanTotal()`, `CanDisplayMinMax()` | 2020-2027, unchanged |
| 3 | Formatting - Multiple values indication | `ScheduleField.MultipleValuesDisplayType` (Project, Varies, Custom), `MultipleValuesCustomText` | **2022-2027 only** - the enum is not in 2020 or 2021 |
| 4 | A column's font, bold, italic, underline, colour (Modify Schedule - Font, on a column) | `ScheduleField.GetStyle` / `SetStyle`, `TableCellStyle`, `TableCellStyleOverrideOptions` | 2020-2027 |
| 5 | Sorting/Grouping - Footer: title, count | `ScheduleSortGroupField.ShowFooterTitle`, `ShowFooterCount` | 2020-2027 |
| 6 | Sorting/Grouping - Blank line | `ScheduleSortGroupField.ShowBlankLine` | 2020-2027 |
| 7 | Sorting/Grouping - per level ascending/descending, header, footer | `ScheduleSortGroupField.SortOrder`, `ShowHeader`, `ShowFooter`, one object per level | 2020-2027 |
| 8 | Sorting/Grouping - Grand totals title text, count | `ScheduleDefinition.GrandTotalTitle`, `ShowGrandTotalTitle`, `ShowGrandTotalCount` | 2020-2027 |
| 9 | Fields - the order of the columns; a field put in at a place | `ScheduleDefinition.SetFieldOrder`, `GetFieldOrder`, `InsertField(SchedulableField, int)` | 2020-2027 |
| 10 | Fields - Include elements in links | `ScheduleDefinition.IncludeLinkedFiles`, `CanIncludeLinkedFiles()` | 2020-2027 |
| 11 | Appearance - Show Title, Show Headers, Grid lines on sheets | `ScheduleDefinition.ShowTitle`, `ShowHeaders`, `ShowGridLines` | 2020-2027 |
| 12 | Appearance - Title text, Header text, Body text (text types) | `ViewSchedule.TitleTextTypeId`, `HeaderTextTypeId`, `BodyTextTypeId`, `IsValidTextTypeId` | 2020-2027 |
| 13 | Appearance - Stripe rows; on sheets; stripe colour; row height | `ViewSchedule.HasStripedRows` (2020-2027); `UseStripedRowsOnSheets`, `GetStripedRowsColor`/`SetStripedRowsColor` with `StripedRowPattern` (**2021-2027**); `RowHeight`, `RowHeightOverride` with `RowHeightOverrideOptions` (**2024-2027**; `ImageRowHeight` on 2020-2024 is the image rows only and obsolete at 2024) | as stated |
| 14 | Group / ungroup column headers (Modify Schedule - Group) | `ViewSchedule.GroupHeaders`, `UngroupHeaders`, `CanGroupHeaders`, `CanUngroupHeaders` | 2020-2027 |
| 15 | New Material Takeoff, Note Block, Revision schedule, Keynote Legend, area schedule by scheme | `ViewSchedule.CreateMaterialTakeoff`, `CreateNoteBlock` (+ `GetValidFamiliesForNoteBlock`), `CreateRevisionSchedule`, `CreateKeynoteLegend`, `CreateSchedule(doc, categoryId, areaSchemeId)` | 2020-2027 |
| 15 | Fields - Embedded schedule | `ScheduleDefinition.AddEmbeddedSchedule`, `RemoveEmbeddedSchedule`, `CanHaveEmbeddedSchedule`, `IsValidCategoryForEmbeddedSchedule` | 2020-2027 |
| 15 | Filter - Filter by sheet | `ScheduleDefinition.IsFilteredBySheet`, `IsValidCategoryForFilterBySheet()` | **2023-2027 only** |
| 16 | A new key in a key schedule (Modify Schedule - Insert row) | `TableSectionData.InsertRow`, `CanInsertRow` on the body section | 2020-2027 - **whether it adds a key element is NOT said by the reference: NEEDS REAL REVIT** |
| 17 | Split a schedule; place one segment | `ViewSchedule.Split(int)` / `Split(IList<double>)`, `IsSplit`, `GetSegmentCount`, `ScheduleSheetInstance.Create(..., int segmentIndex)`, `SegmentIndex` | **2022-2027 only** in the reference packages (Autodesk's notes give 2022.1). Revit 2020 and 2021 cannot split a schedule through the API |

## 2. Checked, reported, not built

**18. Calculated Value (formula) and Percentage fields.** *Formula:* confirmed a Revit limit on every release
2020-2027. `ScheduleFieldType.Formula` exists and `AddField(ScheduleFieldType)` accepts it at compile time,
but **no member on any release gives a field its formula** - `ScheduleField` has none, and
`TableCellCalculatedValueData` has only `GetName()`. This was first measured on 2026-09-07
([31 - studying the existing libraries](../../31-studying-the-existing-libraries.md), *"A gap claim of my
own, refuted by trying to build it"*) and is unchanged on 2025-2027. *Percentage:* **the reference does not
confirm the forum claim for this one.** `ScheduleField.PercentageOf` and `PercentageBy` both have a setter
on 2020-2027, and `ScheduleFieldType.Percentage` exists, so `AddField(ScheduleFieldType.Percentage)` and
then `PercentageOf` might be accepted. Whether Revit takes it is **NEEDS REAL REVIT** - one rolled-back probe
(Group KK's probe row). Nothing built until it has run. Whether `GetSchedulableFields()` leaves both out was
not checked here: it needs a model.

**19. Conditional formatting.** A Revit limit on every release 2020-2027. No public type for it exists in
any release read (`api-surface --members ConditionalFormatOptions`: *no public type*), and `ScheduleField`'s
whole member list on 2020-2027 has no conditional member. The native half of `RevitAPI.dll` carries the
string `OwnerArr<ConditionalFormatOptions>` on 2020, 2024 and 2027 - Revit keeps it internally and does not
expose it.

**20. Freeze header.** `ViewSchedule.IsHeaderFrozen` is a **static** property (2020-2027): one switch for the
Revit session's schedule views, not a setting saved on a schedule. Not built; a cloud-built tool that
flipped it would change the modeller's screen, not the model.

**21. Do these four take a schedule?** Read in their code, 2026-11-01; none was run on a schedule.

| Capability | Reaches a schedule? | How, from its code |
|---|---|---|
| `DUPLICATE_VIEW` (PROVEN) | **Yes, by name** | `views` is `IList<View>` from the request; the add-in's `ViewsNamed` collects `OfClass(typeof(View))`, which includes `ViewSchedule`. It asks `CanViewBeDuplicated(option)` first and refuses a template. Which of the three options Revit allows on a schedule is **NEEDS REAL REVIT** |
| `APPLY_VIEW_TEMPLATE` (PROVEN) | **Yes, by name** | The same `IList<View>` binding; it sets `ViewTemplateId` and reads it back. It does not check the template's kind first - a plan template on a schedule is caught only by Revit's throw or the read-back, and reported as refused |
| `SET_VIEW_PHASE` (PROVEN) | **Only through a chain** | `elements` comes from the selection or a chain, and the code takes `element as View`. A schedule clicked on a sheet arrives as a `ScheduleSheetInstance`, which is not a `View`, and is reported unsupported. A `ViewSchedule` handed on by `FIND_SCHEDULES` (it provides `elements`) is phased. **Its comment says "a schedule has a phase and no phase filter"; Schedule Properties in Revit appears to show a Phase Filter too - not checked, recorded here, not fixed** |
| `RENAME_ELEMENTS` (PROVEN) | **Only through a chain** | The same `elements` binding, find and replace on `Element.Name`, read back. A `ViewSchedule` from `FIND_SCHEDULES` is renamed like any view; a clicked `ScheduleSheetInstance` is renamed only if its `Name` takes the value, which is **NEEDS REAL REVIT** - the read-back reports it refused if not |

## 3. What was built

See the table at the top of the pull request and [NEEDS-CHECKING Group KK](../../needs-checking/group-kk.md),
which hold the same list with the proof owed for each. All of it is DRAFT; nothing has run in Revit.
