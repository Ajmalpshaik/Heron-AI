# Revit's saved export setups - what the API reaches, and what Heron builds for them

**2026-11-01.** Written before any code, for [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) row **5b-1360**.
Ajmal PS asked Heron, on his Revit 2020 job model 4355-BHVD-3D-50S20-BL001A, to rename the DWG/DXF
export setup "Setup 1" to "Tags Export". Heron had nothing that could see or change an export setup,
and he renamed it by hand. He then asked for the whole family of export setups to be covered, not
only the DWG rename.

> Nothing here is specification. Where it disagrees with a fragment's card, the card and the API
> lookup it names win - re-run the lookup. Every API line below was read with
> `dotnet run --project tools/api-surface -- --members <Type>` against the reference assemblies
> cached for **2020 through 2027**, on 2026-11-01. **Nothing here has been run in Revit.** What only a
> Revit run can settle is marked **NEEDS REAL REVIT**, and is in NEEDS-CHECKING Group **KA**.

## 1. The kinds of saved export setup a model can hold

| Setup, as the modeller sees it | Saved in the model as | Reachable through the Revit API | Releases |
|---|---|---|---|
| **DWG/DXF export setups** (File - Export - CAD Formats - DWG or DXF, "Select Export Setup") | `ExportDWGSettings`, an element. **One element holds both the DWG and the DXF options** | Yes - read, create, copy, rename, delete, change options | 2020-2027, no signature change |
| **DGN export setups** | `ExportDGNSettings`, an element | Yes - same set of calls | 2020-2027; one option moved (below) |
| **PDF export setups** (Revit's own PDF export) | `ExportPDFSettings`, an element | Yes - same set of calls | **2022-2027 only.** The type and `PDFExportOptions` do not exist in 2020 or 2021, where Revit had no PDF export of its own |
| **Print settings** (File - Print - Setup, "Name") | `PrintSetting`, an element | Yes - through the print manager's `PrintSetup` (current setting, `SaveAs`, `Rename`, `Delete`) | 2020-2027 |
| **DWF/DWFx export** | **Nothing.** `DWFExportOptions` exists, but there is no saved-setup element and no `ListNames` | **No saved setup to reach** - only the options handed to one export | - |
| **IFC export setups** (the IFC dialog's "Modify Setup") | **Not a Revit element.** The IFC exporter is a separate add-in; its saved configurations are its own data. Believed to be kept in the project in that add-in's own storage - **not verified**, and the search made for it found nothing official | **No** - the Revit API has only `IFCExportOptions`, the options handed to one export. Reading the IFC add-in's storage would be reading another product's private data | - |
| **Navisworks NWC options** | **Not in the model.** The NWC exporter's Options Editor keeps them per user, outside the project (community-reported: the registry, and a config file that only seeds the first values) | **No** - `NavisworksExportOptions` is only the options handed to one export | - |
| **Image export** | Nothing saved. `ImageExportOptions` is per export | No saved setup | - |

So **four kinds can be reached: DWG/DXF, DGN, PDF (2022+) and print settings.** The other four
are not saved in a form Revit's own API exposes, and Heron says so rather than pretending to list them.

## 2. What each reachable kind lets the API do

### DWG/DXF - `ExportDWGSettings` (2020-2027, every line unchanged)

| Do | API |
|---|---|
| List the names | `ExportDWGSettings.ListNames(doc)`; `BaseExportOptions.GetPredefinedSetupNames(doc)` lists the names too |
| Find one | `ExportDWGSettings.FindByName(doc, name)` |
| Which is active | `ExportDWGSettings.GetActivePredefinedSettings(doc)` |
| Read the options | `GetDWGExportOptions()`, `GetDXFExportOptions()` |
| Change the options | `SetDWGExportOptions(options)`, `SetDXFExportOptions(options)` |
| Create | `Create(doc, name)`, `Create(doc, name, DWGExportOptions)`, `Create(doc, name, DXFExportOptions)` |
| Copy | `Create(doc, newName, source.GetDWGExportOptions())` - **whether the copy carries the layer table, line types and patterns is NEEDS REAL REVIT** |
| Rename | `Element.Name { get; set; }` - the general element setter. **NEEDS REAL REVIT** that Revit accepts it on this element |
| Delete | `Document.Delete(id)` - an element like any other. **What Revit does when the ACTIVE setup is deleted is NEEDS REAL REVIT** |

The options inside (`DWGExportOptions : ACADExportOptions : BaseExportOptions`), all 2020-2027:

| Option in the dialog | API member | Values |
|---|---|---|
| File format version | `FileVersion` | `ACADVersion`: R2007, R2010, R2013, R2018 (Default = R2018, the same number) |
| Export units | `TargetUnit` | `ExportUnit`: Default, Inch, Foot, Millimeter, Centimeter, Meter |
| Coordinate system basis | `SharedCoords` | true = Shared, false = Project Internal |
| Solids (3D views only) | `ExportOfSolids` | `SolidGeometry`: Polymesh, ACIS |
| Colours | `Colors` | `ExportColorMode`: IndexColors, TrueColor, TrueColorPerView |
| Line scaling | `LineScaling` | `LineScaling`: ViewScale, ModelSpace, PaperSpace |
| **Export views on sheets as external references** | `MergedViews` (DWG only) | **false = external references**, true = merged into one file |
| Layer standard to load | `LayerMapping` | text - a standard's name or a mapping file. **Which text Revit takes for each standard, and whether setting it reloads the layer table, is NEEDS REAL REVIT** |
| Layer table (category - layer name - colour - cut layer) | `GetExportLayerTable()` / `SetExportLayerTable()` - `ExportLayerKey(category, subcategory, SpecialType)` to `ExportLayerInfo` (`LayerName`, `ColorNumber`, `CutLayerName`, `CutColorNumber`, modifiers) | per row |
| Export category properties BYLAYER/BYENTITY | `PropOverrides` | `PropOverrideMode`: ByEntity, ByLayer, NewLayer |
| Text treatment | `TextTreatment` | Exact, Approximate |
| Hide scope boxes / reference planes / unreferenced view tags | `HideScopeBox`, `HideReferencePlane`, `HideUnreferenceViewTags` | true/false |
| Preserve coincident lines | `PreserveCoincidentLines` | true/false |
| Export rooms, spaces and areas as polylines | `ExportingAreas` | true/false |
| Non-plot layers | `MarkNonplotLayers`, `NonplotSuffix` | true/false, text |
| Line types, hatch patterns, text fonts | `LinetypesFileName`, `HatchPatternsFileName`; `Get/SetExportLinetypeTable`, `...PatternTable`, `...FontTable` | file names, tables |
| AutoCAD Architecture objects | `ACAPreference` | `ACAObjectPreference` |

### DGN - `ExportDGNSettings` (2020-2027)

The same seven calls as DWG (`ListNames`, `FindByName`, `GetActivePredefinedSettings`, `Create`,
`GetDGNExportOptions`, `SetDGNExportOptions`). `DGNExportOptions : BaseExportOptions`, so the layer
table, colours, hide options and coincident lines are the same members as DWG. Its own:

| Option | Member | Note |
|---|---|---|
| File version | `FileVersion` | `DGNFileFormat`: DGNVersion7, DGNVersion8 (Default = V8) |
| Views on sheets merged | `MergedViews` | |
| Seed file | `SeedName` | text |
| **Units** | `MasterUnits` **2020-2021**, `WorkingUnits` **2021-2027** | **The one cross-release change found.** 2021 has both; code needs `#if` |
| Line weights | `Get/SetExportLineweightTable` | |

### PDF - `ExportPDFSettings` (2022-2027 only)

`ListNames`, `FindByName`, `GetActivePredefinedSettings`, `IsValidName`, `Create(doc, name, PDFExportOptions)`,
`GetOptions`, `SetOptions`. `PDFExportOptions` (2022-2027): `Combine`, `PaperFormat`
(`ExportPaperFormat`), `PaperOrientation` (`PageOrientationType`; `Auto` exists from 2021), `ZoomType`,
`ZoomPercentage`, `ColorDepth`, `RasterQuality`, `ExportQuality` (`PDFExportQualityType`, DPI72-DPI4000),
`AlwaysUseRaster`, `HideCropBoundaries`, `HideReferencePlane`, `HideScopeBoxes`,
`HideUnreferencedViewTags`, `MaskCoincidentLines`, `ReplaceHalftoneWithThinLines`, `ViewLinksInBlue`,
`PaperPlacement`, `OriginOffsetX/Y`, `StopOnError`, and the file **naming rule**
(`Get/SetNamingRule`, a list of `TableCellCombinedParameterData` - a parameter id, prefix, suffix,
separator). `Get/SetExportInBackground` is **2025-2027 only**.

### Print settings - `PrintSetting` through `PrintSetup` (2020-2027)

`PrintSetting` is an element with `Name { get; set; }` and `PrintParameters` (paper size, orientation,
zoom, colour, hidden-line processing, raster quality, hide options; `OriginOffsetX/Y` 2022+).
`PrintSetup` (from `doc.PrintManager`) has `CurrentPrintSetting { get; set; }`, `SaveAs`, `Rename`,
`Delete`, `Save`, `Revert`. **The print manager is application state, not document state** - an
undo does not put the current setting back (already recorded on SET_PRINT_SETTINGS and
MANAGE_SHEET_SETS), so anything that makes another setting current must put the old one back itself.

## 3. What Heron had on 2026-11-01

Searched with `heron_lookup` ("rename the DWG export setup Setup 1 to Tags Export" reached
**EXPORT_VIEWS_TO_DWG**, a PUBLISH - the export itself - with words and nearness agreeing) and by
grep over every fragment's C# for the setup types:

| Capability | What it does with a setup |
|---|---|
| EXPORT_VIEWS_TO_DWG (FRG-DOC-003, DRAFT, PUBLISH) | **Uses** a DWG setup by name (`FindByName`) and refuses a missing one. Lists, reads, writes none |
| EXPORT_SHEETS_TO_PDF (PROVEN, PUBLISH) | Builds `PDFExportOptions` of its own; no saved setup |
| EXPORT_MODEL_TO_IFC, EXPORT_MODEL_TO_NWC (DRAFT, PUBLISH), EXPORT_VIEWS_TO_FBX, EXPORT_VIEW_IMAGE | Build their own options; no saved setup - and for IFC, NWC and image none can be reached (section 1) |
| SET_PRINT_SETTINGS (FRG-SHT-019, PROVEN, MODIFY) | Applies paper size, orientation and fit to the current print setting and **saves it under a new name**. Lists none, renames none, deletes none |
| MANAGE_SHEET_SETS (FRG-SHT-015, PROVEN, MODIFY) | The shape copied: one card, a `mode` of create / rename / delete / add / remove, a name that does not exist refused naming it |
| LIST_SHEET_SETS (FRG-SHT-032, PROVEN, READ) | The read beside it - the shape copied for the list |

**Nothing listed an export setup, read its options, created, copied, renamed or deleted one, or
changed an option in one.**

## 4. The smallest set that covers it

**Widen nothing that is PROVEN.** SET_PRINT_SETTINGS and MANAGE_SHEET_SETS are PROVEN; a change under
either's `impl/` makes its proof stale, and neither is about an export setup. EXPORT_VIEWS_TO_DWG is a
PUBLISH - managing a setup is a MODIFY that writes no file - so putting setup management into it would
put a model change behind the Publish switch and an export behind the same words. So: **three new
cards, one per job**, each covering every reachable kind rather than one card per kind.

| New capability | Risk | Job | Kinds |
|---|---|---|---|
| **LIST_EXPORT_SETUPS** (FRG-DOC-056) | READ | Every saved setup by kind, which one is active, and its key options in the dialog's words; with one setup named, also its layer table | DWG/DXF, DGN, PDF (2022+), print |
| **MANAGE_EXPORT_SETUPS** (FRG-DOC-057) | MODIFY | `mode` create, duplicate, rename or delete a setup by name | DWG/DXF, DGN, PDF (2022+); print: duplicate, rename, delete (create is SET_PRINT_SETTINGS) |
| **SET_EXPORT_SETUP_OPTIONS** (FRG-DOC-058) | MODIFY | Change named options of one setup - file version, units, coordinates, solids, colours, line scaling, views on sheets as external references, layer standard, hide options, one layer-table row - all or nothing, read back after | DWG/DXF, DGN, PDF (2022+). Print options stay with SET_PRINT_SETTINGS |

**Shared rules on all three:** a setup is found by name, case-insensitive; `kind` may be left out and
the name is looked for in every kind - found in two kinds is refused, naming both; a name that does
not exist is refused, **naming it and the names that do exist**; a new name already in use in that
kind is refused. 2020 and 2021 answer that PDF setups need Revit 2022 or later instead of failing to
compile. Changing the PDF naming rule, line types, patterns and fonts is **not built** - recorded in
row 5b-1360 as the next widening of SET_EXPORT_SETUP_OPTIONS if it is asked for.

## 5. What only a Revit run can settle (NEEDS-CHECKING Group KA)

1. Renaming an export setup through `Element.Name` - accepted, and seen under the new name in the dialog.
2. A duplicated DWG setup carries the source's layer table (rows read back equal).
3. Deleting the ACTIVE DWG setup - refused by Revit, or accepted and the dialog falls back to what.
4. `LayerMapping` - the text each standard takes, and whether setting it reloads the layer table.
5. `ExportPDFSettings.Create` with a fresh `PDFExportOptions` - accepted with the default naming rule, or refused.
6. Print duplicate/rename/delete through `PrintSetup` put the current print setting back afterwards.
7. The negative cases: a setup name that does not exist refused naming it; nothing changed.

Proving waits for Ajmal PS's word, on a **scratch model - never a 4355-* job model**.

## 6. View/Sheet Sets - added to the scope the same day

Ajmal PS asked for the View/Sheet Sets to be covered in the same work: he keeps one set per value of his
sheet parameter *TCM_Project Browser 1* (*01_IAB-ISSUED AS-BUILT*, *02_IFI-ISSUED FOR INFORMATION* ...),
and that day MANAGE_SHEET_SETS made, renamed and deleted sets on 4355-BHVD-3D-50S20-BL001A - with a sheet
selection run first for rename and delete, which do not use one.

### What the API has (tools/api-surface --members, 2026-11-01)

| Type | Members | Releases |
|---|---|---|
| `ViewSheetSet` (an element) | `Name { get; set; }`, `Views { get; set; }` (a `ViewSet`) | 2020-2027 |
| | `IsAutomatic`, `OrderedViewList`, `SheetOrganizationId`, `ViewOrganizationId` - how the dialog orders a set's members | **2023-2027 only** |
| `ViewSheetSetting` (from `doc.PrintManager.ViewSheetSetting`) | `CurrentViewSheetSet { get; set; }`, `AvailableViews`, `InSession`, `Save`, `SaveAs`, `Rename`, `Delete`, `Revert` | 2020-2027 |
| `InSessionViewSheetSet` | `Views { get; set; }`; the 2023 ordering members as above | 2020-2027 |

The setting throws unless `PrintManager.PrintRange` is *Selected views/sheets*, and the print range, the
current set and the In-Session set's members are application state an Undo does not put back (recorded
on MANAGE_SHEET_SETS).

### How a set ties to print and to the export setups

**It does not tie to a setup at all.** None of `ExportDWGSettings`, `ExportDGNSettings`,
`ExportPDFSettings` or `PrintSetting` holds a set - no member on any of them names one (section 2's
lists). A set is chosen **at export or print time**: the DWG/DXF and PDF export dialogs' *Export* list and
the Print dialog's *Selected views/sheets* both offer the saved sets, and Revit exports what the set holds
with the options of the setup chosen beside it. Through the API the print uses the current set
(`PrintManager.SubmitPrint`), and `Document.Export` takes the views as ids. So in Heron:
EXPORT_VIEWS_TO_DWG takes views, and EXPORT_SHEETS_TO_PDF takes elements - **neither takes a set's name**.
*"Export the sheets in set X"* needs the set's members handed to the export; that is recorded in row
5b-1361 as a follow-up, not built here.

### What Heron had, and what was built

| Job | Before | Now |
|---|---|---|
| Create, rename, delete one set; add or remove sheets **and views** | MANAGE_SHEET_SETS (PROVEN) - `add` and `remove` already take any non-template view, not only sheets | Unchanged in code. Its `elements` is marked **optional**, so rename and delete run with nothing selected - an element list nothing filled binds empty, and every mode that reads it already refuses an empty list. The card's contract only: the proof's fingerprint covers `impl/` and still matches |
| List sets, flag empty ones and identical pairs | LIST_SHEET_SETS (PROVEN) | Unchanged |
| **One set per parameter value, in one call; bring existing ones back in step** | Nothing - one MANAGE_SHEET_SETS call and one selection per value | **SYNC_SHEET_SETS_BY_PARAMETER** (FRG-SHT-034, MODIFY, DRAFT): every non-template sheet and view carrying the parameter, grouped by value; a missing set is saved; an existing one is left alone unless `updateExisting`, then made to hold exactly its value's members; the print range, current set and In-Session members put back. **Merged later the same day into `MANAGE_SHEET_SETS` version 4, mode `by-parameter`, and DEPRECATED** - see below |
| **Find sets out of step with their value** | Nothing | **CHECK_SHEET_SETS_BY_PARAMETER** (FRG-SHT-035, READ, DRAFT): per value, the set named for it - in step, missing members, extra members - and values with no set. Read as elements, never through the print manager |
| Duplicate a set | Nothing | **Built later the same day, DRAFT:** `MANAGE_SHEET_SETS` version 3, mode `duplicate` (make current, `SaveAs`). It made the PROVEN card's proof stale, so the card is DRAFT again - on Ajmal PS's yes. Row 5b-1361, NEEDS-CHECKING KA15 and KA16 |
| Export a set's sheets to DWG or PDF by the set's name | Nothing | **Not built** - see above |

**Merged later the same day.** `SYNC_SHEET_SETS_BY_PARAMETER` is DEPRECATED: it is `MANAGE_SHEET_SETS`
version 4's mode `by-parameter` now, with the same inputs, refusals and outputs (its `unchanged` is
`setsUnchanged`), and its In-Session switch and restore taken for every mode that writes - the safer
rule. The table row above is the record of what was built first. FRAGMENT-ISSUES row 5b-1377.

What only a Revit run settles is NEEDS-CHECKING Group KA, rows KA11 to KA17.
