# Revit ribbon coverage - what Heron has, what is missing

**2026-11-01.** One research session: every Revit ribbon tab (2020-2027) checked tool by tool
against the Heron library, and every gap checked against the Revit API reference online.

> Nothing here is specification and nothing here is proven. It is a file-reading audit: no Revit
> was run. "Covered" means a fragment exists for the job - its own `heron-status` says whether it
> is PROVEN or DRAFT. Counts below were taken from the four tables on the day and go stale as
> fragments are added; re-derive with
> `grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c`.
> Some API reference sites could not be reached from the cloud session, so every **UNSURE** is a
> question still open, not a no.

**Lifecycle:** **built except for the items named under the next heading, and not retired** (read
2026-11-11, corrected the same day after a fact-check). Most of the "build next" list below was built by
[#781](https://github.com/Ajmalpshaik/Heron-AI/pull/781) (`b0bc60a7`, merged 2026-11-04). The condition
this line stated until then - *retire once each "build next" item has a fragment, or has been moved to
[PROPOSALS.md](../../PROPOSALS.md) / [OPEN-QUESTIONS.md](../../OPEN-QUESTIONS.md)* - is met only because
what was not built is now in the register: rows 5b-1423 and 5b-1529 to 5b-1532 of
[FRAGMENT-ISSUES](../../fragment-issues/section-5b-rows-401-425.md). **Two things still keep the file, and each is a person's to
settle** - in the [work-notes README](../README.md)'s lifecycle, *knowledge preserved* comes before
*retired*:

- *The API cannot do it*, *Still UNSURE* and the four tab-by-tab tables are in no register and no
  numbered document; this file is their only copy.
- Fragment cards and [Group MO](../../needs-checking/group-mo.md) name this file by its path -
  `grep -rl revit-ribbon-coverage brain docs` lists them. Deleting it needs those lines re-pointed
  first.

**Nothing here is a build list any more.** Ask `heron_lookup`, or search `brain/fragments`, before
building anything a table below calls MISSING: the tables are the picture of 2026-11-01.

## Summary on the day

| Tab group | Covered | Partial | Missing |
|---|---|---|---|
| Architecture, Massing & Site, Insert | 44 | 12 | 29 |
| Structure, Steel, Precast, Analyze (structural) | 26 | 11 | 42 |
| Systems (HVAC, Fabrication, Plumbing, Electrical) + Analyze (MEP) | 40 | 14 | 35 |
| Annotate, View, Manage, Collaborate, Modify | 76 | 32 | 68 |

**Steel (fabrication) and Precast have no coverage at all.** Structure, HVAC, Plumbing and View are
mostly covered.

## Build next - missing, and the Revit API can do it

> **MOSTLY BUILT, MERGED 2026-11-04 - history, not a list to work from.**
> [#781](https://github.com/Ajmalpshaik/Heron-AI/pull/781) wrote the tools for this list, every one at
> DRAFT and none run in Revit that day; each card's own `heron-status` says what it is now, and the
> NEEDS-CHECKING groups that pull request added hold the runs owed. Checked 2026-11-11 by searching
> `brain/fragments/*/impl` for the Revit member each item names, and - after a fact-check found that
> search had passed item 1 on the word *structural* alone - by reading the card and code behind each
> item that widens a tool Heron already had. **Five things on the list are not built, or not built as
> the list meant them. Each is a row now, so none lives only in this file:**
>
> - **Item 1, a structural floor - not built.** `CREATE_FLOOR` has no structural input and passes
>   `false`: [row 5b-1529](../../fragment-issues/section-5b-rows-401-425.md).
> - **Item 8, insulation on fittings - not built.** `SET_MEP_INSULATION` still refuses a fitting, an
>   accessory and a flex run; the other two halves, remove insulation and duct lining, were built:
>   [row 5b-1530](../../fragment-issues/section-5b-rows-401-425.md).
> - **Item 17, a slanted column - in code, on no card's words, never run.** `PLACE_LINE_BASED_FAMILY`
>   makes the call the table below names when it is asked for a column; the table had it as MISSING:
>   [row 5b-1531](../../fragment-issues/section-5b-rows-401-425.md).
> - **Item 17, a structural plan - not built.** `CREATE_PLAN_VIEW` makes a floor plan or a ceiling
>   plan and refuses anything else: [row 5b-1532](../../fragment-issues/section-5b-rows-401-425.md). The span
>   direction third of the item is `place-span-direction-symbols`.
> - **Item 19, Reload Latest - not built.** Revit refuses it while any transaction group is open, and
>   every Heron door that may change a model runs inside one:
>   [row 5b-1423](../../fragment-issues/section-5b-rows-401-425.md), OPEN, the owner's to choose.
>
> **A fragment that names an item's Revit member is not that item built as a modeller would ask for
> it**, and the first reading of 2026-11-11 said *"a fragment was found for every item but these"* of
> two items where five belonged. The longer per-tab priority lists further down were **not** re-checked
> item by item.

Most used by a modeller first, across all tabs.

1. **Structural floor** - `create-floor` has no structural flag (`Floor.Create` isStructural from 2022, `NewFloor` before)
2. **Wall foundation** - `WallFoundation.Create`
3. **Foundation slab** - `NewFoundationSlab` 2020-2022; from 2022 a foundation-slab type through `Floor.Create` (UNSURE which overload)
4. **Openings: wall opening and shaft** - `NewOpening(Wall, ...)`, `NewOpening(Level, Level, CurveArray)`; `create-opening` may not take walls
5. **Tags: Area, Material, Multi-Category** - `NewAreaTag`, `IndependentTag.Create` with `TM_ADDBY_MATERIAL` / `TM_ADDBY_MULTICATEGORY`
6. **Edit Circuit / Select Panel / Disconnect Panel** - `ElectricalSystem.AddToCircuit`, `RemoveFromCircuit`, `SelectPanel`, `DisconnectPanel`
7. **Add to / remove from duct or pipe system** - `MEPSystem.Add` / `Remove`
8. **Insulation on fittings, remove insulation, duct lining** - `DuctInsulation` / `PipeInsulation` / `DuctLining.Create`
9. **Attach column top/base** - `ColumnAttachment.AddColumnAttachment`
10. **Rebar by shape or sketch, on any host** - `Rebar.CreateFromRebarShape`, `Rebar.CreateFromCurves`
11. **Trim / Extend** - `LocationCurve.Curve`
12. **Material takeoff schedule** - `ViewSchedule.CreateMaterialTakeoff`
13. **Duct and pipe placeholders, and converting them** - `CreatePlaceholder`, `ConvertDuctPlaceholders` / `ConvertPipePlaceholders`
14. **Truss** - `Truss.Create`
15. **Fabric sheet (single), rebar coupler** - `FabricSheet.Create`, `RebarCoupler.Create`
16. **Rebar numbering, multi-rebar annotation** - `NumberingSchema`, `MultiReferenceAnnotation.Create`
17. **Span direction, slanted column, structural plan** - `SpanDirectionSymbol.Create`, `ViewPlan.Create`
18. **Spot coordinate, dimension types** - `NewSpotCoordinate`, `DimensionType.Duplicate`
19. **Active workset, reload latest** - `WorksetTable.SetActiveWorksetId`, `Document.ReloadLatest`
20. **Size tables, electrical settings, panel schedule templates** - `DuctSizeSettings`, `Segment.AddSize`, `PanelScheduleTemplate.Create`
21. **Import PDF / image, work plane, reference plane in a project, roof by extrusion** - `ImageType.Create`, `View.SketchPlane`, `NewReferencePlane`, `NewExtrusionRoof`
22. **Steel connection** - `StructuralConnectionHandler.Create`
23. **Structural loads and boundary conditions** - `PointLoad` / `LineLoad` / `AreaLoad.Create`, `LoadCase.Create`, `BoundaryConditions`

## The API cannot do it (or needs another API)

- **Ramp** - no create method found in the reference (Autodesk has not said so in writing)
- **Model Text in a project, in-place family / mass, Roof by Face**
- **Spot Slope, Repeating Detail, Legend Component, first Legend, Plan Region, Scope Box create, View Reference create, Phase create, Copy/Monitor, Scale, Split wall**
- **Switch Systems**, zone creation on Revit 2027
- **Steel plates, bolts, welds and cuts** - not the Revit API; the Advance Steel API through the Steel Connections add-in
- Radial and arc-length dimensions only from Revit 2025

## Still UNSURE - research before building

Precast split, CAM export and configurations · steel numbering, Connection Automation, steel cut
types · Generate Layout, Parallel Pipes / Conduits, fabrication multi-point routing, P&ID · Decal,
Coordination Model, Soffit, Floor by Face, Graded Region, Label Contours, Link Topography · about 30
Annotate/View/Manage items marked in the table below.

---


## Heron coverage audit: Architecture, Massing & Site, Insert (Revit 2020-2027)

Source: a one-line index of every `brain/fragments/*/fragment.yaml` taken on the day plus `brain/fragments/<f>/fragment.yaml` and `impl/any/fragment.cs`. P = PROVEN, D = DRAFT.
Generic tools (move/copy/rotate/mirror/delete/set-parameter/change-type/tag/hide/isolate) are assumed for every element and are not listed as gaps.
"API?" is only filled in for PARTIAL/MISSING rows. It was checked against the Revit API reference (help.autodesk.com / revitapidocs / rvtdocs) or forum answers from Autodesk staff where those exist. UNSURE means it could not be confirmed.

### Architecture tab

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Wall: Architectural | create-wall (straight + arc, base offset) | COVERED (P) | - | - |
| Wall: Structural | create-wall `structural` flag | COVERED (P) | - | - |
| Wall by Face | create-walls-by-face | COVERED (D) | not proven | - |
| Wall: Sweep | place-wall-sweeps | COVERED (P) | - | - |
| Wall: Reveal | place-wall-sweeps (reveal type) | COVERED (P) | - | - |
| Door | place-hosted-family | COVERED (P) | - | - |
| Window | place-hosted-family | COVERED (P) | - | - |
| Component: Place | place-family-instances, place-hosted-family, place-family-on-face, place-line-based-family (+ load-family) | COVERED (P) | - | - |
| Component: Model In-Place | - | MISSING | in-place family | NO. The API cannot create in-place families (Revit API forum, accepted answer) |
| Column: Architectural | place-family-instances (any family at points) | COVERED (P) | - | - |
| Roof by Footprint | create-roof + slope-roof-edges | COVERED (P) | - | - |
| Roof by Extrusion | - | MISSING | extrusion roof | YES: `Document.NewExtrusionRoof(CurveArray, ReferencePlane, Level, RoofType, start, end)` |
| Roof by Face | - | MISSING | roof on mass face | NO. There is no public RoofBase factory for by-face roofs (forum) |
| Roof: Soffit | - | MISSING | soffit | UNSURE. No create method found |
| Roof: Fascia | create-edge-sweeps | COVERED (P) | - | - |
| Roof: Gutter | create-edge-sweeps | COVERED (P) | - | - |
| Ceiling: Automatic | create-from-room-boundaries (ceiling per room) | COVERED (P) | 2022+ only | 2020-21: NO (`Ceiling.Create` is 2022+) |
| Ceiling: Sketch | create-ceiling | COVERED (P) | 2022+ only, and on 2020-21 it says so | 2020-21: NO |
| Floor: Architectural | create-floor, create-from-room-boundaries | COVERED (P) | - | - |
| Floor: Structural | create-floor (always non-structural) | PARTIAL | no structural flag | YES: `Floor.Create(doc, loops, typeId, levelId, isStructural, slopeArrow, slope)` (2022+); `NewFloor(..., structural)` before 2022 |
| Floor by Face | create-mass-floors makes only the mass floors | PARTIAL | floor element from a mass floor | UNSURE. No FaceFloor create found |
| Floor: Slab Edge | create-edge-sweeps | COVERED (P) | - | - |
| Curtain System (by face) | create-curtain-system | COVERED (P) | - | - |
| Curtain Grid | add-curtain-grid-lines (instance); set-curtain-wall-grid (type, P) | COVERED (D) | instance grid not proven | - |
| Mullion | add-curtain-grid-lines (with mullions, D); set-curtain-wall-mullions (type, P) | COVERED (D/P) | per-segment mullion not proven | - |
| Railing: Sketch Path | create-railing (path), edit-railing | COVERED (P) | - | - |
| Railing: Place on Stair/Ramp | create-railing (host) | COVERED (P) | - | - |
| Ramp | - | MISSING | ramp | NO. No ramp creation method exists in the API reference. Ramps only appear in failure IDs and `Railing.Create(host)`. Absence confirmed by search; Autodesk has not said so in writing |
| Stair by Component | create-stairs (straight or U run plus automatic landing) | PARTIAL | L, spiral, winder, multi-run, custom landing | YES: `StairsRun.CreateSpiralRun`, `CreateStraightRun`, `StairsLanding.CreateSketchedLanding` in a `StairsEditScope` |
| Stair by Sketch | - | MISSING | sketched run boundary/riser | YES: `StairsRun.CreateSketchedRun` |
| Model Text | add-family-model-text (family editor only) | PARTIAL | project model text | NO in a project: `FamilyItemFactory.NewModelText` is family-only. The workaround is a model-text family |
| Model Line | create-line (asDetail=false) | COVERED (P) | lines on horizontal planes only | (arbitrary plane: YES, `NewModelCurve` + `SketchPlane.Create`) |
| Model Group (create/place) | group-elements, place-model-groups, ungroup-elements | COVERED (P) | - | - |
| Room | place-rooms, place-room-at-point | COVERED (P) | - | - |
| Room Separator | create-room-separation-lines | COVERED (P) | - | - |
| Tag Room | tag-rooms, center-room-tags | COVERED (P) | - | - |
| Area | place-areas, create-area-plan | COVERED (P) | - | - |
| Area Boundary | create-area-boundary-lines | COVERED (P) | - | - |
| Tag Area | - (tag-elements uses IndependentTag, which does not make area tags) | MISSING | area tag | YES: `Document.NewAreaTag(ViewPlan, Area, UV)` |
| Area and Volume Computations | set-area-volume-computations, report-area-volume-computations | COVERED (P) | - | - |
| Opening: By Face | create-opening (`perpendicularFace=true`, floor/roof/ceiling) | COVERED (P) | proof ran the vertical case only | - |
| Opening: Shaft | - (create-stairs cuts only its own slab) | MISSING | multi-level shaft | YES: `Document.NewOpening(Level bottom, Level top, CurveArray)` |
| Opening: Wall | create-opening | PARTIAL | wall path unproven. The fragment's own note says the slab overload may not take a wall | YES: `Document.NewOpening(Wall, XYZ, XYZ)` |
| Opening: Vertical | create-opening (`perpendicularFace=false`) | COVERED (P) | - | - |
| Opening: Dormer | - | MISSING | dormer cut | NO dedicated call. The substitute is `NewOpening(roof, profile, false)` |
| Datum: Level | create-level, create-levels | COVERED (P) | - | - |
| Datum: Grid | create-grid, create-grids, create-radial-grids | COVERED (P) | - | - |
| Work Plane: Set | - | MISSING | set the view's work plane | YES: `SketchPlane.Create` + `View.SketchPlane` setter |
| Work Plane: Show | - | MISSING | show/hide work plane | YES: `View.ShowActiveWorkPlane()` / `HideActiveWorkPlane()` |
| Work Plane: Ref Plane | create-reference-planes (family editor only) | PARTIAL | reference planes in a project | YES: `Document.Create.NewReferencePlane` (works in projects) |
| Work Plane: Viewer | - | MISSING | UI viewer | UNSURE. UI-only window |

### Massing & Site tab

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| In-Place Mass | - (create-conceptual-form works inside a loadable mass family) | MISSING | in-place mass | NO. No in-place family API |
| Place Mass | place-family-instances + load-family | COVERED (P) | - | - |
| Curtain System by Face | create-curtain-system | COVERED (P) | - | - |
| Roof by Face | - | MISSING | - | NO (see Architecture) |
| Wall by Face | create-walls-by-face | COVERED (D) | not proven | - |
| Floor by Face | create-mass-floors (P), set-mass-floor-usage (D) | PARTIAL | floor elements | UNSURE |
| Toposurface (2020-2023) | - | MISSING (pre-2024) | create-toposolid is 2024+ only | YES: `TopographySurface.Create(doc, IList<XYZ>)` (obsolete from 2024) |
| Toposolid (2024+) | create-toposolid (P), edit-toposolid (D), edit-slab-shape (P) | COVERED (P) | - | - |
| Site Component | place-family-instances | COVERED (P) | - | - |
| Parking Component | place-family-instances | COVERED (P) | - | - |
| Building Pad (2020-2023) | edit-toposolid (excavate by floor/pad, 2024+, D) | MISSING (pre-2024) | create pad | YES: `BuildingPad.Create(doc, typeId, levelId, IList<CurveLoop>)` |
| Property Line | - | MISSING | property line | YES on 2027 only: `PropertyLine.Create(doc, IList<CurveLoop>)` / `(doc, IList<PropertyTableEntry>)`. NO on 2020-2026 |
| Subregion / Sub-Divide | add-toposolid-subdivision (2024+) | PARTIAL (D) | 2020-23 subregion | YES: `SiteSubRegion.Create(doc, loops, hostId)` (obsolete from 2024) |
| Split Surface / Split Toposolid | edit-toposolid (split, 2024+) | PARTIAL (D) | 2020-23 toposurface split | 2024+: YES `Toposolid.Split`; 2020-23: UNSURE |
| Merge Surfaces | - | MISSING | merge toposurfaces | UNSURE |
| Graded Region | - | MISSING | graded region | UNSURE. No create method found |
| Label Contours | - | MISSING | contour labels | UNSURE |

### Insert tab

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Link Revit | link-files | COVERED (D) | not proven | - |
| Link IFC | - | MISSING | IFC link | YES: `RevitLinkType.CreateFromIFC` (needs the IFC import-by-reference .rvt first; this is heavy) |
| Link CAD | link-files | COVERED (D) | not proven | - |
| Link Topography | - | MISSING | Civil 3D/Docs topography link | UNSURE |
| DWF Markup | - | MISSING | DWF markup link | UNSURE |
| Decal (place/types) | - | MISSING | decals | UNSURE. Forum answers conflict and no create method was found |
| Point Cloud | - | MISSING | link point cloud | YES: `PointCloudType.Create` + `PointCloudInstance.Create` (project only) |
| Coordination Model | - | MISSING | NWD/NWC link | UNSURE. No API found |
| Manage Links | list-linked-models, reload-links, unload-links, set-link-path-type, set-link-graphics, set-link-room-bounding, report-external-references | COVERED (P) | - | - |
| Import CAD | import-cad (P); extract-cad-curves (P); convert-cad-to-directshape (D) | COVERED (P) | - | - |
| Import gbXML | - | MISSING | gbXML import | YES: `Document.Import(string, GBXMLImportOptions)` |
| Import PDF | - | MISSING | PDF underlay | YES: `ImageType.Create(doc, ImageTypeOptions)` + `ImageInstance.Create` (2020+; check `PageNumber` per release) |
| Import Image | - | MISSING | raster image | YES: same `ImageType.Create` + `ImageInstance.Create` (2020+) |
| Manage Images | find-unused-images (report, D); delete-elements | PARTIAL | reload / reload-from | YES: `ImageType.Reload` / `ReloadFrom` (2020+) |
| Load Family | load-family | COVERED (P) | - | - |
| Load from Library (Autodesk content) | find-library-families (local folders only) | PARTIAL | Autodesk cloud library | UNSURE |
| Insert from File (Views / 2D) | transfer-views-between-documents (P), copy-elements-between-documents (P) | PARTIAL | the source file must already be open; nothing pulls from a closed file | YES: `Application.OpenDocumentFile` + `ElementTransformUtils.CopyElements` |

### Tally

| Tab | Covered | Partial | Missing |
|---|---|---|---|
| Architecture (51) | 33 | 6 | 12 |
| Massing & Site (17) | 6 | 3 | 8 |
| Insert (17) | 5 | 3 | 9 |
| **Total (85)** | **44** | **12** | **29** |

### Missing or partial where the API can do it, in priority order (most used by a modeller first)

1. Opening: Wall. Prove or fix with `NewOpening(Wall, XYZ, XYZ)`
2. Tag Area: `NewAreaTag`
3. Opening: Shaft: `NewOpening(Level, Level, CurveArray)`
4. Import PDF / Import Image: `ImageType.Create` + `ImageInstance.Create`
5. Floor: Structural: the `Floor.Create` isStructural overload
6. Work Plane Set / Show: `View.SketchPlane`, `ShowActiveWorkPlane`
7. Reference Plane in project: `NewReferencePlane`
8. Stair by Component, other shapes (L, spiral, landings): `StairsRun`/`StairsLanding` create methods
9. Roof by Extrusion: `NewExtrusionRoof`
10. Insert from File from a closed file: `OpenDocumentFile` + `CopyElements`
11. Stair by Sketch: `StairsRun.CreateSketchedRun`
12. Point Cloud: `PointCloudType.Create` + `PointCloudInstance.Create`
13. Toposurface / Building Pad / Subregion for 2020-2023: `TopographySurface.Create`, `BuildingPad.Create`, `SiteSubRegion.Create`
14. Manage Images reload: `ImageType.ReloadFrom`
15. Link IFC: `RevitLinkType.CreateFromIFC`
16. Import gbXML: `Document.Import(GBXMLImportOptions)`
17. Property Line (2027 only): `PropertyLine.Create`

Blocked by the API (NO): Ramp, Model In-Place / In-Place Mass, Roof by Face, Dormer (dedicated), Model Text in a project, Ceiling on 2020-21.


## Heron coverage: Structure, Steel, Precast, Analyze (structural), Revit 2020-2027

Source: a one-line index of every `brain/fragments/*/fragment.yaml` taken on the day and each fragment's `fragment.yaml` (`purpose:`, `revit:`, and the API names it calls). Status is the fragment's `heron-status`. Generic tools (move, copy, rotate, mirror, delete, set-parameter-values-by-id, select/count/tag/hide/isolate, join-geometry) are counted as covering any category and are not listed as gaps.
API column: **YES** = confirmed in Autodesk's reference or SDK material (sources at the end). **UNSURE** = could not confirm. **NO** = confirmed not in the API. Some sites could not be reached from this session (rvtdocs.com would not resolve), so the earliest release is given only where a source showed it.

### Structure tab

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Beam | place-line-based-family (StructuralType asked for) | PROVEN | - | - |
| Wall (structural) | create-wall (`structural` input) | PROVEN | - | - |
| Column (vertical) | place-structural-family (base, top level and offset) | PROVEN | - | - |
| Column (slanted) | none | MISSING | point-based only | YES: `NewFamilyInstance(Curve, FamilySymbol, Level, StructuralType.Column)` |
| Floor: Structural | create-floor | PARTIAL | no structural flag | YES: `NewFloor(..., bool structural)` up to 2021, `Floor.Create(doc, loops, typeId, levelId, isStructural, slopeArrow, slope)` from 2022 |
| Floor: Slab Edge | create-edge-sweeps | PROVEN | - | - |
| Truss | none | MISSING | - | YES: `Truss.Create` |
| Brace | place-line-based-family | PROVEN | - | - |
| Beam System | create-beam-system | PROVEN | - | - |
| Connection | none | MISSING | - | YES: `StructuralConnectionHandler.Create` / `CreateGenericConnection` (in the 2020 mirror; detailed types need the Steel Connections add-in) |
| Connection Automation | none | MISSING | - | UNSURE (the UI runs Dynamo graphs; no API found) |
| Foundation: Isolated | place-structural-family (footing) | PROVEN | - | - |
| Foundation: Wall | none | MISSING | - | YES: `WallFoundation.Create(doc, typeId, wallId)` |
| Foundation: Slab | none (create-floor takes no foundation type) | MISSING | - | YES: `NewFoundationSlab` 2020-2022 (removed 2023). From 2022 use `Floor.Create` with a foundation-slab type, per the SDK FoundationSlab sample. Exact overload UNSURE |
| Rebar (host-specific sets) | place-beam-rebar, place-footing-rebar | PROVEN | - | - |
| Rebar (columns) | place-column-rebar | DRAFT | not proven | - |
| Rebar (standalone, any host: shape, sketch, free form; wall/slab U-bars, edge bars) | none | PARTIAL | only beam, column and footing patterns | YES: `Rebar.CreateFromRebarShape`, `Rebar.CreateFromCurves`, `Rebar.CreateFreeForm` |
| Area reinforcement | place-area-reinforcement (slab, foundation slab, wall) | PROVEN | - | - |
| Path reinforcement | place-path-reinforcement | PROVEN | - | - |
| Fabric Area | place-fabric-reinforcement (`FabricArea.Create`) | DRAFT | not proven | - |
| Fabric Sheet (single) | none | MISSING | - | YES: `FabricSheet.Create` |
| Cover (element) | set-rebar-cover | PROVEN | - | - |
| Rebar Cover Settings | create-rebar-cover-type | PROVEN | edits an existing cover type's distance only through the generic tool | - |
| Rebar Coupler | none | MISSING | - | YES: `RebarCoupler.Create` (2020-2027) |
| Reinforcement Settings | none | MISSING | - | YES: `ReinforcementSettings.GetReinforcementSettings` (area/path host rules, `NumberingMethod`, etc.) |
| Rebar Shape (create) / Shape Browser | list-rebar-types (lists shapes) | PARTIAL | no create | YES: `RebarShape.Create(doc, RebarShapeDefinition, ...)` (2026 adds a terminations overload) |
| Rebar bar / hook types | create-rebar-bar-type, create-rebar-hook-type, list-rebar-types | PROVEN | - | - |
| Rebar view state | set-rebar-view-state | PROVEN | - | - |
| Rebar report | report-host-rebar | PROVEN | - | - |
| Rebar numbering / partitions | none (renumber-sequential writes Mark, not Revit's rebar numbers) | MISSING | - | YES: `NumberingSchema` (`AssignElementsToSequence`, `MergeSequences`, `RemoveGaps`), Partition parameter |
| Multi-rebar annotation | none | MISSING | - | YES: `MultiReferenceAnnotation.Create(doc, viewId, MultiReferenceAnnotationOptions)` |
| Model Text (project) | add-family-model-text (Family Editor only) | PARTIAL | not in a project | NO: `FamilyItemFactory.NewModelText` works in family documents only. Workaround: load a family that holds model text |
| Model Line | create-line (model mode, horizontal plane) | PROVEN | sloped or vertical planes | YES: `NewModelCurve` on any `SketchPlane` |
| Model Group | place-model-groups, group-elements, ungroup-elements | PROVEN | - | - |
| Opening: By Face | create-opening (`perpendicularFace=true`); framing web/flange opening | PROVEN | - | - |
| Opening: Vertical | create-opening (`perpendicularFace=false`) | PROVEN | - | - |
| Opening: Wall | create-opening | PARTIAL | uses the general call. The fragment's own trap note says walls take `NewOpening(Wall, XYZ, XYZ)`. Proof was on a floor, not a wall | YES: `NewOpening(Wall, XYZ, XYZ)` |
| Opening: Shaft | none (select-openings only reads them) | MISSING | - | YES: `NewOpening(Level bottom, Level top, CurveArray)` |
| Datum: Level, Grid | create-level(s), create-grid(s), create-radial-grids | PROVEN | - | - |
| Datum: Reference Plane (project) | create-reference-planes (Family Editor only) | PARTIAL | not in a project | YES: `Document.Create.NewReferencePlane` |
| Work Plane: Set | none | MISSING | - | YES: `SketchPlane.Create` + `View.SketchPlane` |
| Structural Settings | none | MISSING | - | YES: `StructuralSettings.GetStructuralSettings` (brace symbols, cutbacks, boundary-condition symbols; members differ by release) |
| Span Direction | none | MISSING | - | YES: `Floor.SpanDirectionAngle` (structural floors only), `SpanDirectionSymbol.Create`. Earliest release not confirmed |
| Beam/column justification | set-parameter-values-by-id (generic) | PARTIAL | no dedicated tool. Y/Z justification is only a parameter write | YES: parameters; `StructuralFramingUtils` |
| Attach column top/base | none | MISSING | - | YES: `ColumnAttachment.AddColumnAttachment` / `RemoveColumnAttachment` (2020+) |
| Framing cut-back / extension / join | disallow-join (framing joins), cope-framing; extensions through generic params | PARTIAL | no end-extension or cut-back tool | YES: `StructuralFramingUtils` (end join, `SetEndReference`), Start/End Extension parameters |
| Cope | cope-framing | PROVEN | - | - |
| Parts / Assembly | create-parts, create-assembly (PROVEN); divide-parts, merge-parts, create-assembly-views (DRAFT) | PROVEN / DRAFT | - | - |

### Steel tab (all need the Steel Connections for Revit add-in / Advance Steel components)

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Plate | none | MISSING | - | YES: Advance Steel API inside Revit (`FabricationTransaction`). Revit 2021 SDK `SampleCommandsSteelElements` CreatePlate. Not core `Autodesk.Revit` |
| Bolts | none | MISSING | - | YES: Advance Steel bolt patterns (AS API walkthrough). Revit sample name not confirmed |
| Anchors | none | MISSING | - | YES: AS API anchor pattern (2021 SDK sample) |
| Holes | none | MISSING | - | UNSURE (plates with holes appear in the sample, but no separate hole tool was seen) |
| Shear Studs | none | MISSING | - | YES: `CreateShearStudPattern` (2021 SDK sample) |
| Welds | none | MISSING | - | YES: `CreateWeldPoint` (2021 SDK sample) |
| Contour Cut | none | MISSING | - | YES: `CreateContourCut` (2021 SDK sample) |
| Shorten | none | MISSING | - | YES: `CreateShortening` (2021 SDK sample); `ParameterTypeId.SteelElemPlateShortenCutstraight` |
| Corner Cut, Cope Skewed, Cope (steel), Miter, Cut Through, Cut By, Parametric Cuts | none (cope-framing is Revit's coping cut, not the steel feature) | MISSING | - | UNSURE (no sample or reference found) |
| Connection / Connection Automation | none | MISSING | - | YES / UNSURE, as on the Structure tab |
| Steel fabrication parameter edit | set-parameter-values-by-id (generic, untested on steel elements) | PARTIAL | not checked on fabrication elements | YES: `SteelElementProperties` (`GetSteelElementProperties`, `AddFabricationInformationForRevitElements`) |
| Steel element numbering | none | MISSING | - | UNSURE (the UI tool is documented, but no API method was found) |

### Precast tab (Structural Precast extension; in Revit from 2023 help onward, add-in before that)

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Split (walls/floors) | create-parts / divide-parts are Revit parts, not precast segmentation | MISSING | - | UNSURE (no segmentation API in the Precast API material) |
| Reinforcement (precast config) | none | MISSING | - | UNSURE |
| Shop Drawings | create-assembly-views (Revit assembly views, not precast shop drawings) | PARTIAL (DRAFT) | not the precast generator | YES: Precast API shop-drawing command plus creation events (one DLL; settings fall back to Precast Configuration) |
| CAM Export | none | MISSING | - | UNSURE (UI writes Unitechnik 5.2/6.0, PXML 1.3; no API found) |
| Precast configurations | none | MISSING | - | UNSURE |
| Mounting parts | none | MISSING | - | UNSURE |
| Custom fabrication parameters / production numbers | none | MISSING | - | YES (partial): the Precast API targets numbering (production numbers, assembly names, position numbers) |

### Analyze tab (structural half)

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Loads: Point / Line / Area | none | MISSING | - | YES: `PointLoad.Create`, `LineLoad.Create`, `AreaLoad.Create`. Host overloads changed in 2023: old AnalyticalModel hosts were removed, analytical elements are hosts from 2023 |
| Load Cases (and natures) | none | MISSING | - | YES: `LoadCase.Create`, `LoadNature.Create`, `LoadUsage.Create` |
| Load Combinations | none | MISSING | - | YES: `LoadCombination.Create` + `SetComponents` / `SetUsageIds` |
| Boundary Conditions | none | MISSING | - | YES: `Document.Create.NewPoint/Line/AreaBoundaryConditions` (2020-2026; obsolete in 2027 in favour of `BoundaryConditions.CreatePointBoundaryConditions` etc.) |
| Analytical Model: read | report-analytical-model | DRAFT | 2023-2027 only; 2020-2022 not covered | YES: `AnalyticalModel` (2020-2022) |
| Analytical Members / Panels / Openings: create | none | MISSING | - | YES (2023+): `AnalyticalMember.Create`, `AnalyticalPanel.Create(doc, CurveLoop)`, `AnalyticalOpening` (Create not confirmed) |
| Analytical Links | none | MISSING | - | YES: `AnalyticalLink.Create(doc, typeId, hub1, hub2)` (2025-2027 docs; earliest UNSURE) |
| Adjust analytical | none | MISSING | - | YES (2023+): `AnalyticalMember.SetCurve`; pre-2023 manual adjustment UNSURE |
| Analytical Automation | none | MISSING | - | UNSURE (Dynamo-based in the UI; could be built from the create calls) |
| Check Supports | check-structural-supports (geometric check of the physical model) | DRAFT | not proven; not Revit's analytical check | - |
| Consistency Checks | none | MISSING | - | UNSURE |
| Analysis Results | show-analysis-heatmap (AVF display of any number) | PARTIAL (DRAFT) | no stored structural results | YES, outside the core API: Results Builder (Structural Analysis Toolkit / SDK, `Autodesk...ResultsBuilder.Storage`) |

### Missing, API YES: priority order (most-used by a structural modeller first)

1. Structural floor: add `isStructural` to create-floor (`Floor.Create` 2022+, `NewFloor` up to 2021)
2. Wall foundation: `WallFoundation.Create`
3. Foundation slab: `NewFoundationSlab` up to 2022, `Floor.Create` + foundation type from 2022
4. Attach column top/base: `ColumnAttachment.AddColumnAttachment`
5. Standalone rebar by shape, sketch or free form, any host (wall/slab bars): `Rebar.CreateFromRebarShape` / `CreateFromCurves` / `CreateFreeForm`
6. Shaft opening `NewOpening(Level, Level, CurveArray)`; wall opening via `NewOpening(Wall, XYZ, XYZ)`
7. Slanted column: `NewFamilyInstance(Curve, ..., StructuralType.Column)`
8. Span direction: `Floor.SpanDirectionAngle`, `SpanDirectionSymbol.Create`
9. Rebar numbering / partitions: `NumberingSchema`
10. Multi-rebar annotation: `MultiReferenceAnnotation.Create`
11. Truss: `Truss.Create`
12. Fabric sheet (single): `FabricSheet.Create`
13. Loads (point/line/area): `PointLoad/LineLoad/AreaLoad.Create`
14. Load cases and combinations: `LoadCase.Create`, `LoadNature.Create`, `LoadCombination.Create`
15. Boundary conditions: `NewPoint/Line/AreaBoundaryConditions` (2027: `BoundaryConditions.Create*`)
16. Analytical members, panels, links, create and adjust (2023+)
17. Structural connection: `StructuralConnectionHandler.Create` / `CreateGenericConnection`
18. Reference plane in a project (`NewReferencePlane`) and Set Work Plane (`SketchPlane.Create` + `View.SketchPlane`)
19. Framing end extension / cut-back / justification as a dedicated tool: `StructuralFramingUtils` + parameters
20. Rebar coupler: `RebarCoupler.Create`
21. Reinforcement settings: `ReinforcementSettings`
22. Rebar shape create: `RebarShape.Create`
23. Structural settings: `StructuralSettings`
24. Analytical model reader for 2020-2022: `AnalyticalModel`
25. Steel plate, anchors, shear studs, welds, contour cut, shorten: Advance Steel API in Revit. Needs the Steel Connections add-in. Calls a non-`Autodesk.Revit` assembly, so it needs a recorded decision first
26. Precast shop drawings and production numbers: Precast API (separate DLL)
27. Structural analysis results: Results Builder (Toolkit SDK, outside the core API)

### Sources
- NewModelText (family only): https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/f7a49b85-9b18-8546-6935-d76b784c9cfa.htm
- StructuralSettings: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/1d1b89be-c2ae-ca39-01ce-f5b01178fb1e.htm
- NewFoundationSlab removed 2023: https://forums.autodesk.com/t5/revit-api-forum/revit-api-creating-a-foundation-slab-in-revit-2023-and-later/td-p/12831251
- Steel elements SDK sample (2021): https://blog.csdn.net/weixin_44153630/article/details/123023231 ; https://cfanz.cn/resource/detail/nAAPALyNkXAGl ; AS API bolts/welds: https://help.autodesk.com/cloudhelp/2016/ENU/AdvSteel-API/files/GUID-0BC13443-7B8B-4393-A1D8-2F79476BB165.htm
- SteelElementProperties: https://revapidocs.com/2026/911b649a-d108-14a2-dc09-8e97d489c17d.htm ; steel numbering (UI): https://help.autodesk.com/cloudhelp/2027/ENU/Revit-DocumentPresent/files/GUID-03B93302-A74E-45A1-B37B-035C2B2A3D0D.htm
- StructuralConnectionHandler.Create: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/a3fe213c-d8bf-cb9c-c48d-d6eb601aa936.htm
- Precast API shop drawings: https://help.autodesk.com/cloudhelp/2023/ENU/Revit-StructEng/files/GUID-F4049C69-116C-414C-8BA2-7CFD4C748F54.htm ; ribbon: https://help.autodesk.com/cloudhelp/2022/ENU/Revit-StructEng/files/GUID-287AFCC7-DF8E-4CD0-8A31-CA0889D66360.htm ; numbering scope: https://www.autodesk.com/blogs/aec/2018/05/09/new-structural-precast-extension-for-revit-2019/
- Loads / cases / combinations: https://help.autodesk.com/cloudhelp/2018/ENU/Revit-API/Revit_API_Developers_Guide/Discipline_Specific_Functionality/Structural_Engineering/Loads.html
- Boundary conditions: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/db4e68f0-d06c-8207-98fb-83e84b3cb2d1.htm ; https://www.revitapidocs.com/2027/db4e68f0-d06c-8207-98fb-83e84b3cb2d1.htm
- Analytical model 2023: https://help.autodesk.com/cloudhelp/2023/ENU/Revit-Analyze/files/GUID-A1157199-4E27-41F9-BF45-53A5CD79E9A1.htm ; AnalyticalLink.Create: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/eea725bf-30dd-4aaa-5870-9ffa2f0c898c.htm
- ColumnAttachment: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/27944249-fe98-f6ef-ba85-6408535b2d0b.htm ; RebarCoupler.Create: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/52fc10f6-e5b0-0f47-20fa-90be57b48004.htm
- NumberingSchema / ReinforcementSettings: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/41b7c755-b580-71a4-fa3c-76ed4f4a326a.htm ; MultiReferenceAnnotation: https://jeremytammik.github.io/tbc/a/1021_multi_reference_annot.htm
- RebarShape.Create: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/dd926f0a-458d-d64d-1c85-07f3d2c68baa.htm
- Span direction: https://help.autodesk.com/cloudhelp/2026/ENU/Revit-API-MainReference/files/html/9f4d9bd1-0b6a-e0f0-113f-53230b722daf.htm ; https://www.revitapidocs.com/2025/d029779d-cc65-5ed5-5ff1-a7abab977dc6.htm
- Results Builder: https://forums.autodesk.com/t5/revit-api-forum/structural-analysis-toolkit-resultsbuilder-reviewing-stored/td-p/8778306


## Heron coverage: Revit Systems tab + Analyze (MEP), Revit 2020-2027

Source: a one-line index of every `brain/fragments/*/fragment.yaml` taken on the day (534 PROVEN / 84 DRAFT), with `purpose:` and `impl/` read where the one-line description was not enough. P = PROVEN, D = DRAFT.
Generic tools (move/copy/rotate/delete, parameter edits, select/count/tag/hide/isolate, change-element-type) are assumed for every category and are not listed as gaps.
API notes were checked against the Autodesk Revit API reference (help.autodesk.com) and revitapidocs/apidocs mirrors on 2026-11-01. UNSURE means I could not confirm it.

### HVAC (Mechanical panel)

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Duct | create-duct (P), create-duct-runs (D) | COVERED | - | - |
| Duct Placeholder | none | MISSING | Draw placeholders, then convert to duct | YES `Duct.CreatePlaceholder` + `MechanicalUtils.ConvertDuctPlaceholders` |
| Duct Fitting | place-mep-fitting (P), fit-mep-joints (P) | COVERED | - | - |
| Duct Accessory | place-accessory-on-run (D) | COVERED (DRAFT) | Not proven yet | - |
| Convert to Flex Duct | none | MISSING | Turn a rigid section into flex | NO direct method found. Can be built: delete the rigid section, then `Document.NewFlexDuct(Connector, Connector, FlexDuctType)` / `FlexDuct.Create` |
| Flex Duct | create-flex-duct (P) | COVERED | - | - |
| Air Terminal | place-family-instances (P), place-family-on-face (P), set-air-terminal-flow (P), connect-air-terminals (P) | COVERED | - | - |
| Mechanical Equipment | place-family-instances (P), check-equipment-connectors (P), check-equipment-clearance (P) | COVERED | - | - |
| Mech. Settings: duct sizes | none | MISSING | Add or remove sizes in the duct size table | YES `DuctSizeSettings.GetDuctSizeSettings` / `.AddSize(DuctShape, MEPSize)` |
| Mech. Settings: duct settings / conversion | none | MISSING | Air density, fitting angles, conversion defaults | Partly YES `DuctSettings.GetDuctSettings` (air density, `FittingAngleUsage`). Conversion settings: UNSURE |
| Duct Sizing | auto-size-mep (P), set-mep-size (P) | PARTIAL | Velocity method only. No friction or static-regain method, no height/width limits | YES, by working the size out and writing it. No API for Revit's own Duct Sizing dialog found: UNSURE |
| Routing Preferences | report-/set-/copy-routing-preferences (P) | COVERED | - | - |
| Justification | set-mep-justification (D), align-mep-elevation (P) | COVERED (DRAFT) | Justification not proven yet | - |
| Generate Layout | none | MISSING | Proposes routes for a system | UNSURE. No API found. Can be approximated with placeholders and Heron's own routing |
| Add / Remove from System | create-mep-system (P) builds a new system only | PARTIAL | Cannot add to or remove from an existing system | YES `MEPSystem.Add(ConnectorSet)`, `MEPSystem.Remove(ICollection<ElementId>)` |
| Create Duct System | create-mep-system (P), create-mep-system-type (P), set-mep-system-type (P) | COVERED | - | - |
| Insulation: add / edit | set-mep-insulation (P) | PARTIAL | Ducts and pipes only. Fittings, accessories and flex are **refused** | YES `DuctInsulation.Create` / `PipeInsulation.Create` accept a fitting or accessory as host |
| Insulation: remove | select-by-insulation (P) finds it | PARTIAL | No call that removes insulation only | YES `InsulationLiningBase.GetInsulationIds` + `Document.Delete` |
| Lining: add / edit / remove | none (only read by clearance checks) | MISSING | Duct lining | YES `DuctLining.Create` (duct, fitting, accessory), `InsulationLiningBase.GetLiningIds` |
| Change Slope | set-mep-slope (P), measure-mep-slope (P) | COVERED | - | - |
| Disconnect | disconnect-connectors (P) | COVERED | - | - |
| Connect Into | connect-air-terminals (P), fit-mep-joints (P, an end sitting on an equipment outlet), connect-open-ends (P) | PARTIAL | No automatic route from an equipment or fixture connector into a chosen main | YES by combining calls Heron already uses: `Duct.Create` + `Document.Create.NewTakeoffFitting` / `NewElbowFitting`. No single ConnectInto call found |
| Inherit Elevation / Size | align-mep-elevation (P), set-mep-size (P), match-element-type (P) | COVERED (equivalent) | The draw-mode toggle itself is UI only | - |
| Trim / Extend | trim-extend-elements (P) | COVERED | Corner solved in plan only. A riser turning into a horizontal run is not handled | - |

### Fabrication

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Fabrication Part | none (selection by category only) | MISSING | Place a fabrication part | YES `FabricationPart.Create(Document, FabricationServiceButton, int, ElementId)` (2016+) |
| MEP Fabrication Settings | none | MISSING | Set the configuration and profile, load services | YES `FabricationConfiguration.SetConfiguration` / `ReloadConfiguration` / `LoadServices` |
| Fabrication Parts browser | none | MISSING | List services, palettes and buttons | YES, through loaded services / `FabricationServiceButton`. Exact enumeration member: UNSURE |
| Optimize Lengths | none | MISSING | - | YES `FabricationPart.OptimizeLengths` (signature differs between 2021 and 2025+) |
| Design to Fabrication | none | MISSING | Convert design duct/pipe to fabrication parts | YES `DesignToFabricationConverter.Convert(ISet<ElementId>, int serviceId)` (2017+) |
| Change Service / Size | none | MISSING | - | YES `FabricationNetworkChangeService` (`ChangeService`, `ChangeSize`, `ApplyChange`) |
| Hangers | none | MISSING | - | YES `FabricationPart.CreateHanger` |
| Spool / Assembly | create-assembly (P), create-assembly-views (D), group-by-assembly (P) | PARTIAL | No spool or item numbering for fabrication | YES `AssemblyInstance.Create` (already used). Fabrication numbering: UNSURE |
| Multi-point Routing | none | MISSING | - | UNSURE (no routing API found; `FabricationNetworkChangeService` only changes existing parts) |

### Plumbing & Piping

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Pipe | create-pipe (P) | COVERED | - | - |
| Pipe Placeholder | none | MISSING | - | YES `Pipe.CreatePlaceholder` + `PlumbingUtils.ConvertPipePlaceholders` |
| Parallel Pipes | none | MISSING | - | UNSURE (no direct API found). Can be built with `Pipe.Create` at offsets + fit-mep-joints |
| Pipe Fitting | place-mep-fitting (P), fit-mep-joints (P), cap-open-pipe-ends (P) | COVERED | - | - |
| Pipe Accessory | place-accessory-on-run (D) | COVERED (DRAFT) | Not proven yet | - |
| Flex Pipe | create-flex-pipe (P) | COVERED | - | - |
| Plumbing Fixture | place-family-instances / place-hosted-family / place-family-on-face (P), check-fixture-connectivity (P), create-fixture-branches (D) | COVERED | Branch drawing still DRAFT | - |
| Sprinkler | place-family-instances (P), report-sprinkler-layout-spaces (P), create-fixture-branches (D), report-sprinkler-network (D) | COVERED | - | - |
| Mech. Settings: segments / sizes | create-pipe-segment (P, new segment with sizes added), report-pipe-segments (P), report-pipe-schedules (P) | PARTIAL | Cannot add or remove a size on an existing segment | YES `Segment.AddSize(MEPSize)` / `Segment.RemoveSize` |
| Mech. Settings: pipe settings (slopes, angles, fluids) | none | MISSING | - | UNSURE |
| Change Slope | set-mep-slope (P), measure-mep-slope (P) | COVERED | - | - |
| Pipe Sizing | auto-size-pipe (P) | PARTIAL | Velocity method only, no friction-loss criterion | YES, by working the size out and writing it (as for duct) |
| Create Piping System | create-mep-system (P) | COVERED | Add / remove: see HVAC | - |
| Pipe Insulation | set-mep-insulation (P) | PARTIAL | Pipe fittings and accessories refused | YES `PipeInsulation.Create` on a fitting or accessory host |

### Electrical

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Wire | create-wires (P) | COVERED | - | - |
| Cable Tray | create-electrical-run (P) | COVERED | - | - |
| Conduit | create-electrical-run (P) | COVERED | - | - |
| Parallel Conduits | none | MISSING | - | UNSURE (no direct API found). Can be built with `Conduit.Create` at offsets |
| Cable Tray / Conduit Fittings | fit-mep-joints (P on duct and pipe. Its code includes the tray and conduit fitting categories) | PARTIAL | Never proven on tray or conduit | YES, the same `NewElbowFitting` / `NewTeeFitting` route. Needs a tray/conduit proof |
| Electrical Equipment | place-family-instances (P), place-family-on-face (P) | COVERED | - | - |
| Device (all types) | place-family-on-face (P), place-hosted-family (P), place-family-instances (P) | COVERED | - | - |
| Lighting Fixture | same as Device | COVERED | - | - |
| Component | place-family-instances (P) | COVERED | - | - |
| Elec. Settings: voltages | none | MISSING | - | YES `ElectricalSetting.AddVoltageType` |
| Elec. Settings: distribution systems | none | MISSING | - | YES `ElectricalSetting.AddDistributionSysType` |
| Elec. Settings: demand factors | none | MISSING | - | YES `ElectricalDemandFactorDefinition.Create` |
| Elec. Settings: load classifications | none | MISSING | - | YES `ElectricalLoadClassification.Create(Document, string)` |
| Elec. Settings: wire types | none | MISSING | - | YES `ElectricalSetting.AddWireType` up to 2025 (obsolete in 2026), `WireType.Duplicate` in 2026+ |
| Elec. Settings: conduit / tray sizes | none | MISSING | - | YES `ConduitSizeSettings.AddSize`. Tray sizes: UNSURE |
| Panel Schedules: create | create-panel-schedules (P) | COVERED | - | - |
| Panel Schedules: edit template | none | MISSING | Create or duplicate a template and edit its layout | YES `PanelScheduleTemplate.Create(Document, PanelScheduleType, PanelConfiguration, string)` |
| Create Power / Data / Lighting circuit | create-electrical-circuit (P; power, data, fire alarm, security, telephone, nurse call, controls, communication) | COVERED | - | - |
| Edit Circuit (add / remove) | none | MISSING | - | YES `ElectricalSystem.AddToCircuit(ElementSet)` / `RemoveFromCircuit(ElementSet)` |
| Select Panel | create-electrical-circuit (P), when the circuit is created only | PARTIAL | Cannot move an existing circuit to another panel | YES `ElectricalSystem.SelectPanel(FamilyInstance)` |
| Disconnect Panel | none | MISSING | - | YES `ElectricalSystem.DisconnectPanel()` |
| Circuit naming | none | MISSING | - | YES `CircuitNamingScheme.Create` + `CircuitNamingSchemeSettings.CircuitNamingSchemeId` (seen in the 2024-2026 docs. Earliest release: UNSURE) |
| Wire Tags | tag-elements (P), tag-elements-in-view (P) | COVERED | - | - |
| Electrical analytical / Power Distribution System | none | MISSING | Analytical nodes: source, bus, transformer, transfer switch, equipment load | YES 2023+ `ElectricalAnalyticalNode.Create(Document, ElectricalAnalyticalNodeType, string)` |
| Area-based loads | none | MISSING | - | YES `AreaBasedLoadType.Create`, `ElectricalLoadAreaData.CreateElectricalLoadAreas`, `AreaBasedLoadData.ConnectToUpstreamNode` (earliest release: UNSURE) |
| Switch Systems | none | MISSING | - | NO (Autodesk forum: no API to create a SwitchSystem) |
| (circuit reading) | report-panel-circuits (P), select-by-electrical-circuit (P) | COVERED | - | - |

### Analyze (MEP half)

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Space | place-rooms (P, space mode), place-room-at-point (P), sync-spaces-with-linked-rooms (P) | COVERED | - | - |
| Space Separator | create-room-separation-lines (P, space mode) | COVERED | - | - |
| Space Tag | tag-rooms (P) | COVERED | - | - |
| Zone | create-hvac-zone (P), tag-rooms (P) | COVERED (2020-2026) | Revit 2027 removed zone creation and `Zone.AddSpaces` from the API (the fragment found this and says so) | NO on 2027 |
| Heating & Cooling Loads | read-space-loads (P), report-space-envelope (P), report-space-airflow (P) | PARTIAL | Reads the results but cannot run the analysis | YES `ViewHVACLoadsReport.RunLoadsAnalysis` (confirmed in the 2027 docs. Earlier releases: UNSURE) |
| Panel Schedule check | report-panel-circuits (P) | COVERED | - | - |
| System Inspector | report-mep-pressure-drop (P), report-connector-loads (P), measure-mep-velocity (P) | PARTIAL | No click-to-inspect view of flow and pressure along a run | The data: YES (already read). An interactive inspector would be UI, not API |
| Check Duct / Pipe Systems | find-system-islands (P), select-by-connection-status (P), check-flow-direction (P), trace-connectivity (D), read-model-warnings (P) | COVERED | - | - |
| Check Circuits | report-panel-circuits (P, lists unassigned circuits), read-model-warnings (P) | COVERED | - | - |
| Show Disconnects | select-by-connection-status (P) | COVERED | - | - |
| Duct / Pipe Pressure Loss Report | report-mep-pressure-drop (P) | COVERED | Revit's own HTML report file is not produced | Exporting that HTML report: UNSURE |
| Duct / Pipe Color Fill Legend | apply-color-fill-scheme (P; rooms, spaces and zones only), color-by-parameter (P; graphic overrides) | PARTIAL | No duct or pipe colour-fill scheme or legend | YES `ColorFillLegend.Create` (2022+), category taken from `SupportedColorFillCategoryIds`. Creating a duct/pipe scheme: UNSURE |
| Energy Model | none | MISSING | - | YES `EnergyAnalysisDetailModel.Create` (marked obsolete in 2027 on a third-party mirror) |
| Systems Analysis | none | MISSING | - | YES `ViewSystemsAnalysisReport.Create` + `RequestSystemsAnalysis(SystemsAnalysisOptions)` (seen in the 2024-2027 docs. Earliest release: UNSURE) |
| Route Analysis (Path of Travel) | none | MISSING | - | YES `PathOfTravel.CreateMapped` (2020), `PathOfTravel.Create(View, XYZ, XYZ)` (2021+) |
| P&ID Modeler | none | MISSING | - | UNSURE (separate Autodesk add-in. No public API found) |

### Counts (rows above)

| Discipline | COVERED | PARTIAL | MISSING |
|---|---|---|---|
| HVAC | 13 | 5 | 6 |
| Fabrication | 0 | 1 | 8 |
| Plumbing & Piping | 8 | 3 | 3 |
| Electrical | 10 | 2 | 14 |
| Analyze MEP | 9 | 3 | 4 |
| **Total** | **40** | **14** | **35** |

The extra "(circuit reading)" row in Electrical is not counted. COVERED includes 3 rows whose only cover is DRAFT: Duct Accessory, Pipe Accessory, Justification.

### Missing or partial, API YES: priority order (most used by an MEP modeller first)

1. **Edit Circuit: add / remove devices**: `ElectricalSystem.AddToCircuit` / `RemoveFromCircuit`
2. **Select Panel on an existing circuit / Disconnect Panel**: `ElectricalSystem.SelectPanel` / `DisconnectPanel`
3. **Add to / Remove from an existing duct or pipe system**: `MEPSystem.Add` / `MEPSystem.Remove`
4. **Insulation on fittings and accessories, and remove insulation**: `DuctInsulation.Create` / `PipeInsulation.Create` on fitting hosts. `GetInsulationIds` + `Delete`
5. **Duct lining**: `DuctLining.Create`
6. **Cable tray / conduit fittings, proven**: fit-mep-joints already includes them. Needs a proof on tray and conduit
7. **Connect Into (equipment or fixture into a main)**: `Duct.Create`/`Pipe.Create` + `NewTakeoffFitting`
8. **Duct / pipe placeholders and convert**: `Duct/Pipe.CreatePlaceholder` + `ConvertDuctPlaceholders` / `ConvertPipePlaceholders`
9. **Size tables**: `DuctSizeSettings.AddSize`, `Segment.AddSize/RemoveSize`, `ConduitSizeSettings.AddSize`
10. **Electrical Settings**: `AddVoltageType`, `AddDistributionSysType`, `ElectricalLoadClassification.Create`, `ElectricalDemandFactorDefinition.Create`, `WireType.Duplicate` (2026+) / `AddWireType` (to 2025)
11. **Panel schedule templates**: `PanelScheduleTemplate.Create`
12. **Circuit naming scheme**: `CircuitNamingScheme.Create` + settings id
13. **Duct / pipe colour-fill legend**: `ColorFillLegend.Create` (2022+)
14. **Run heating & cooling loads**: `ViewHVACLoadsReport.RunLoadsAnalysis` (version support UNSURE before 2027)
15. **Convert to flex duct**: built from delete + `NewFlexDuct` (no direct API)
16. **Fabrication set**: `DesignToFabricationConverter.Convert`, `FabricationPart.Create`, `FabricationNetworkChangeService`, `OptimizeLengths`, `CreateHanger`, `FabricationConfiguration.LoadServices`
17. **Duct / pipe sizing by friction rate**: work the size out, then set-mep-size
18. **Electrical analytical model and area-based loads (2023+)**: `ElectricalAnalyticalNode.Create`, `AreaBasedLoadType.Create`, `ElectricalLoadAreaData.CreateElectricalLoadAreas`
19. **Energy model / Systems Analysis**: `EnergyAnalysisDetailModel.Create`, `ViewSystemsAnalysisReport.RequestSystemsAnalysis`
20. **Path of Travel**: `PathOfTravel.Create`

API NO or UNSURE (not listed above): Switch Systems (NO), Zones on 2027 (NO), Generate Layout, Parallel Pipes/Conduits (no direct API; could be built from offsets), Multi-point Routing, P&ID Modeler, pipe settings (slopes/fluids), duct conversion settings.

Caveat: rvtdocs.com was blocked in this session, so signatures come from help.autodesk.com, revitapidocs.com and apidocs.co as shown in search results. The earliest release is stated only where a source gave it.


## Heron coverage — Annotate, View, Manage, Collaborate, Modify ribbon tabs (Revit 2020–2027)

Source: a one-line index of every `brain/fragments/*/fragment.yaml` taken on the day (618 fragments) + `purpose:` of ambiguous ones. P = PROVEN, D = DRAFT.
API column: YES (class/method) · NO (evidence) · UNSURE (not confirmed — not guessed).
"repo skill" = `.claude/skills/revit-version-support/SKILL.md` (measured against reference assemblies 2020–2027).
Web check limited to search results (rvtdocs.com blocked in this session; fetches not possible).

Counts (rows in the tables below): **COVERED 76 · PARTIAL 32 · MISSING 68** (176 rows; View Reference and Keynoting counted once).

### Annotate

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Aligned dimension | create-linear-dimension | COVERED (P) | — | — |
| Linear dimension | create-linear-dimension, create-dimension, dimension-grids-and-levels, dimension-rooms, dimension-wall-openings, dimension-element-faces, dimension-mep-runs, dimension-family-instances | COVERED (P) | — | — |
| Angular dimension | create-angular-dimension | COVERED (P) | — | — |
| Radial dimension | create-radial-dimension | PARTIAL (D) | DRAFT; 2025+ only | YES 2025+ `RadialDimension.Create`; NO 2020–24 (repo skill) |
| Diameter dimension | create-radial-dimension (isDiameter) | PARTIAL (D) | DRAFT; 2025+ only | YES 2025+ same call `isDiameter:true`; `DiameterDimension.Create` on no release (repo skill) |
| Arc Length dimension | — | MISSING | — | YES 2025+ `ArcLengthDimension.Create`; NO 2020–24 (repo skill) |
| Spot Elevation | add-spot-elevations | COVERED (P) | — | — |
| Spot Coordinate | report-spot-dimensions (reads only) | MISSING | no placement | YES `Document.NewSpotCoordinate` |
| Spot Slope | — | MISSING | — | NO — no create method (Autodesk forum 2013–2019, Ideas request) |
| Detail Line | create-line (+ fillet-lines, remap-line-styles) | COVERED (P) | — | — |
| Region — Filled | create-filled-region | COVERED (P) | — | — |
| Region — Masking | — (project); draw-family-filled-region is family-only | MISSING | fragment notes it skips `CreateMaskingRegion` | YES 2024+ `FilledRegion.CreateMaskingRegion`; 2020–23 UNSURE (no method) |
| Component — Detail | place-detail-components | COVERED (P) | — | — |
| Component — Repeating Detail | — | MISSING | — | NO direct create (forum); workaround copy an existing one with `ElementTransformUtils.CopyElements` |
| Legend Component | — | MISSING | — | NO direct (Autodesk forum); copy-existing workaround UNSURE |
| Revision Cloud | add-revision-cloud | COVERED (P) | — | — |
| Detail Group | group-elements | PARTIAL (P) | proven generically, not for view-specific detail elements; place-model-groups is model only | YES create `Document.Create.NewGroup`; placing a detail group type in a view UNSURE |
| Insulation (detail) | — | MISSING | — | UNSURE |
| Text | create-text-note, set-text-type, report-text-types | COVERED (P) | — | — |
| Spell Check | — | MISSING | — | NO API found (UI-only) — UNSURE |
| Find/Replace | edit-text-values | COVERED (P) | — | — |
| Tag by Category | tag-elements, tag-elements-in-view, tag-rooms | COVERED (P) | — | — |
| Tag All | tag-elements-in-view + find-untagged-elements | COVERED (P) | — | — |
| Beam Annotations | — (compose tag-elements + add-spot-elevations) | PARTIAL | no batch beam tool | YES by composition (`IndependentTag.Create`, `NewSpotElevation`) |
| Multi-Category Tag | copy-tags-to-replacements (re-tag only) | PARTIAL | cannot place new multi-category tags | YES `IndependentTag.Create` `TagMode.TM_ADDBY_MULTICATEGORY` (already used in that fragment) |
| Material Tag | — | MISSING | — | YES `IndependentTag.Create` `TagMode.TM_ADDBY_MATERIAL` |
| Area Tag | — (tag-rooms = room/space/zone) | MISSING | — | YES `Document.Create.NewAreaTag` |
| Room Tag | tag-rooms, center-room-tags | COVERED (P) | — | — |
| Space Tag | tag-rooms | COVERED (P) | — | — |
| View Reference (also View tab) | repoint-view-reference (retargets section marks) | MISSING (D for repoint) | cannot place | NO — no create method; `ReferenceableViewUtils` only retargets (forum + Ideas) |
| Stair Tread/Riser Number | — | MISSING | — | YES `NumberSystem.Create` (docs seen 2024–2026; earliest release UNSURE) |
| Multi-Rebar annotation | — | MISSING | — | YES `MultiReferenceAnnotation.Create` |
| Keynote — Element/Material/User | report-keynotes (reads) | MISSING (D) | no placement | NO reliable — `IndependentTag.Create` gives a keynote tag without key source (forum); material keynote API on roadmap |
| Keynoting Settings | — | MISSING | — | YES `KeynoteTable.GetKeynoteTable` + `LoadFrom` / `Reload` |
| Symbol | place-detail-components (annotation symbol) | COVERED (P) | — | — |
| Span Direction | — | MISSING | — | YES `SpanDirectionSymbol.Create` |
| Path Reinforcement symbol | place-path-reinforcement (bars, not symbol) | MISSING | — | YES `PathReinSpanSymbol.Create` |
| Area Reinforcement symbol | place-area-reinforcement (bars, not symbol) | MISSING | — | UNSURE |
| Fabric Reinforcement symbol | place-fabric-reinforcement (D, sheets) | MISSING | — | YES `FabricReinSpanSymbol.Create` |
| Color Fill Legend | apply-color-fill-scheme, report-color-fill-scheme | COVERED (P) | — | — |
| Duct/Pipe Legend | — | MISSING | — | UNSURE |
| Tags settings (leader arrowheads) | — | MISSING | — | UNSURE |
| Arrowheads | set-text-type (arrowhead of a text type only) | PARTIAL | no arrowhead type create/edit | UNSURE |
| Dimension Types | — (set-text-type covers text only) | MISSING | — | YES `DimensionType.Duplicate` + type parameters |

### View

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Visibility/Graphics | set-category-visibility, set-category-graphics, set-category-solid-fill, set-link-graphics, set-view-workset-visibility, report-category-visibility/overrides, diagnose-visibility, reset-view-graphics | COVERED (P) | — | — |
| Filters | create-view-filter(s-by-rule/-by-value), apply-view-filter, set-view-filter-categories, remove-view-filter, copy-view-filters, audit-view-filters, test-view-filter-match | COVERED (P) | — | — |
| Thin Lines | — | MISSING | — | YES `ThinLinesOptions.AreThinLinesEnabled` (session-wide UI setting) |
| Show Hidden Lines | — | MISSING | — | UNSURE |
| Remove Hidden | — | MISSING | — | UNSURE |
| Cut Profile | — | MISSING | — | UNSURE |
| Render | open-render-dialog, set-render-settings, report-render-settings, read-rendered-images | PARTIAL (P) | opens dialog; modeller presses Render | NO render-run API found (dialog via PostCommand only) |
| 3D View | create-3d-view, set-view-camera, frame-elements-in-camera, set-view-section-box | COVERED (P) | — | — |
| Section | create-section-view, move-view-cut-plane | COVERED (P) | — | — |
| Callout | create-callout | COVERED (P) | — | — |
| Plan — Floor | create-plan-view | COVERED (P) | — | — |
| Plan — Reflected Ceiling | create-plan-view | COVERED (P) | — | — |
| Plan — Structural | — | MISSING | create-plan-view is floor/ceiling only | YES `ViewPlan.Create` with a StructuralPlan `ViewFamilyType` |
| Plan — Area | create-area-plan | COVERED (P) | — | — |
| Plan Region | — | MISSING | — | NO — not exposed (forum, wish CF-2198) |
| Elevation | create-wall-elevation, create-room-elevations, refit-room-elevations (D) | COVERED (P) | — | — |
| Framing Elevation | — | MISSING | — | UNSURE |
| Drafting View | create-drafting-view | COVERED (P) | — | — |
| Duplicate View (plain / with detailing / dependent) | duplicate-view, duplicate-views | COVERED (P) | — | — |
| Legends | create-legend-view | PARTIAL (P) | duplicate only | NO for the first legend (fragment: checked against assemblies at both ends) |
| Schedule/Quantities | create-schedule, create-key-schedule, add/remove-schedule-fields, set-schedule-*, add-schedule-combined-field | COVERED (P) | — | — |
| Material Takeoff | report-material-takeoff (reply only) | MISSING | no schedule view | YES `ViewSchedule.CreateMaterialTakeoff` |
| Sheet List | create-sheet-list | COVERED (P) | — | — |
| Note Block | — | MISSING | — | YES `ViewSchedule.CreateNoteBlock` |
| View List | create-sheet-list (view-list mode) | COVERED (P) | — | — |
| Graphical Column Schedule | — | MISSING | — | UNSURE |
| Scope Box | assign-scope-box-to-view, read-scope-box-extent, select-scope-boxes | PARTIAL (P) | cannot create / resize | NO create (Autodesk staff, forum); height/name only |
| Sheet | create-sheet, create-sheets, duplicate-sheets | COVERED (P) | — | — |
| Title Block | set-sheet-title-block, report-sheet-title-blocks | COVERED (P) | — | — |
| Matchline | — | MISSING | — | UNSURE (no method found) |
| View Reference | see Annotate | — | — | — |
| Guide Grid | — | MISSING | — | UNSURE |
| Revisions | create/edit/delete/merge/reorder-revision, set-sheet-revisions, revision numbering sequences, set-revision-settings | COVERED (P; delete-revision-numbering-sequence D) | — | — |
| Viewports | place-view(s)-on-sheet, place-schedule-on-sheet, align-viewports-across-sheets, center-viewports-on-sheets, report-viewport-types, set-viewport-title-line | COVERED (P) | — | — |
| Activate View (on sheet) | — | MISSING | — | UNSURE |
| Switch Windows | open-view, activate-document, switch-active-project (D) | COVERED (P) | — | — |
| Close Windows | close-view-tabs | COVERED (P) | — | — |
| Tile | — | MISSING | — | UNSURE (no DB API; PostCommand at most) |
| Tab Views | — | MISSING | — | UNSURE |
| User Interface (browser/properties) | — | MISSING | — | YES `DockablePane.Show/Hide` with built-in pane ids |
| View Templates — apply | apply-view-template, remove-view-template | COVERED (P) | — | — |
| View Templates — create | create-view-template-from-view, duplicate-view-template | COVERED (P) | — | — |
| View Templates — manage | set-view-template-control, compare-view-templates, report-view-template-control, find-views-without-template | COVERED (P) | — | — |
| Temporary Hide/Isolate | hide-elements, isolate-elements, show-elements, highlight-vs-rest | COVERED (P) | — | — |
| Crop Region | set-view-crop, set-view-crop-to-shape, set-crop-box-settings | COVERED (P) | — | — |

### Manage

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Materials | create-material, set-material-colour/-properties/-appearance, replace-material, list-materials, create-starter-materials | COVERED (P) | — | — |
| Object Styles | list-categories (read), transfer-object-styles (P), create-line-style (subcategories) | PARTIAL | cannot edit a project category's weight/colour/pattern/material | YES `Category.SetLineWeight`, `LineColor`, `SetLinePatternId`, `Material` |
| Snaps | — | MISSING | — | UNSURE |
| Project Information | report-project-information | PARTIAL (P) | no dedicated write (write-element-parameters not shown on Project Info) | YES `Document.ProjectInformation` parameters |
| Project Parameters | add-project-parameter, transfer-project-parameters | PARTIAL (D) | DRAFT; no edit/remove | YES `BindingMap.Insert/ReInsert/Remove` |
| Shared Parameters | report-shared-parameter-file (D), add-family-shared-parameters (P) | PARTIAL | cannot create definitions in the file | YES `DefinitionFile.Groups.Create`, `Definitions.Create(ExternalDefinitionCreationOptions)` |
| Global Parameters | report-global-parameters (P), create/set-global-parameter (D) | PARTIAL | write side DRAFT | YES `GlobalParameter.Create`, `SetValue` |
| Transfer Project Standards | transfer-line-styles/materials/object-styles/view-filters/views (P), -project-parameters (D), copy-elements-between-documents | PARTIAL | no fill patterns, text/dimension types, system types, MEP settings | YES `ElementTransformUtils.CopyElements` doc→doc |
| Purge Unused | find-purgeable-elements + delete-elements | COVERED (P) | two calls; delete needs confirmation (Art. 7) | — |
| Project Units | set-project-units (P), report-project-units (D) | COVERED (P) | — | — |
| Structural Settings | — | MISSING | — | YES `StructuralSettings.GetStructuralSettings` |
| MEP Settings | routing prefs, pipe segments/schedules, system types | PARTIAL (P) | duct/pipe/electrical settings values (sizes, voltages, conversion) | YES `DuctSettings`, `PipeSettings`, `ElectricalSetting`, `DuctSizeSettings` |
| Panel Schedule Templates | create-panel-schedules (uses existing) | PARTIAL (P) | cannot create/edit templates | UNSURE |
| Fill Patterns | create-fill-pattern | COVERED (P) | — | — |
| Line Styles | create-line-style, remap-line-styles | COVERED (P) | — | — |
| Line Weights (table) | — | MISSING | — | UNSURE (no table setter found; pen numbers only) |
| Line Patterns | create-line-pattern | COVERED (P) | — | — |
| Halftone/Underlay | — | MISSING | — | UNSURE |
| Annotation Objects | set-text-type (arrowhead on text type) | PARTIAL | arrowhead/spot types | UNSURE |
| Detail Level (scale map) | — | MISSING | — | UNSURE |
| Temporary Dimensions | — | MISSING | — | UNSURE |
| Assembly Code | check-family-standards (D, reads) | MISSING | no table load | UNSURE |
| Browser Organization | report-browser-organization | PARTIAL (D) | read only | UNSURE (setting) |
| Keynoting | see Annotate | — | — | — |
| Rebar Cover Settings | create-rebar-cover-type, set-rebar-cover | COVERED (P) | — | — |
| Sheet Issues/Revisions | set-revision-settings | COVERED (P) | — | — |
| Location | set-project-location, report-location | COVERED (P) | — | — |
| Acquire Coordinates | acquire-coordinates, check-link-coordinates | COVERED (P) | — | — |
| Publish Coordinates | — | MISSING | writes into the linked model (Art. 7 confirmation) | YES `Document.PublishCoordinates(LinkElementId)` (2018+) |
| Specify Coordinates at Point | — | MISSING | — | YES `ProjectLocation.SetProjectPosition` |
| Rotate True North | set-project-location | COVERED (P) | — | — |
| Position (Relocate / Rotate Project North) | — | MISSING | — | UNSURE |
| Mirror Project | — | MISSING | — | UNSURE |
| Design Options | report-design-options, select-by-design-option (P), set-design-option (D) | PARTIAL | cannot create sets/options or accept primary | UNSURE (only failure `CannotCreateNewDesignOption` found) |
| Phases | report-phases, set-element-phase, set-view-phase (P), create-phase-filter, report-phase-filters (D) | PARTIAL | cannot create phases | NO — `Phase` has no Create on any release (repo skill) |
| Selection save/load/edit | create-selection-filter, select-from-saved-set, update-saved-set | COVERED (P) | — | — |
| Inquiry — IDs of Selection | read-selection, describe-elements | COVERED (P) | — | — |
| Inquiry — Select by ID | filter-elements-by-id + set-selection, zoom-to-elements | COVERED (P) | — | — |
| Inquiry — Warnings / Review Warnings | read-model-warnings, select-with-warnings | COVERED (P) | — | — |
| Macros | — | MISSING | likely out of scope (runs arbitrary code) | UNSURE (`MacroManager`) |
| Dynamo | — | MISSING | out of scope | NO — not a Revit API feature |

### Collaborate

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Worksets | create-workset, rename-workset, list-worksets, set-element-workset, set-view-workset-visibility, create-workset-3d-views (D) | COVERED (P) | — | — |
| Active Workset | — (copy-elements notes copies land on it) | MISSING | — | YES `WorksetTable.SetActiveWorksetId` |
| Gray Inactive Worksets | — | MISSING | — | UNSURE |
| Editing Requests | read-element-ownership, report-element-ownership | PARTIAL (P) | no request/grant; Art. 12 forbids seizing | UNSURE |
| Relinquish | set-worksharing-ownership, sync-with-central (relinquish) | PARTIAL (D) | DRAFT | YES `WorksharingUtils.RelinquishOwnership` |
| Sync with Central | sync-with-central | PARTIAL (D) | DRAFT. Explicit user request only (Art. 10) — **not a gap to automate** | YES `Document.SynchronizeWithCentral` |
| Reload Latest | — | MISSING | — | YES `Document.ReloadLatest(ReloadLatestOptions)` 2020–2027 |
| Show History | — | MISSING | — | UNSURE (`WorksharingUtils.GetWorksharingTooltipInfo` gives per-element creator/last-changed only) |
| Restore Backup | — | MISSING | — | UNSURE |
| Copy/Monitor | copy-from-link (D, copy only) | MISSING | no monitor | NO — only `GetMonitoredElementIds` read; PostCommand opens UI (forum) |
| Coordination Review | — | MISSING | — | UNSURE (forum: not exposed) |
| Reconcile Hosting | — | MISSING | — | UNSURE |
| Interference Check | find-clashes, check-minimum-clearance | PARTIAL (P) | Heron's own clash test, not Revit's Interference Report | geometry YES (`ElementIntersectsSolidFilter`); the report tool UNSURE |
| Collaborate in Cloud | — | MISSING | a share action (Art. 10) | UNSURE |

### Modify

| Ribbon tool | Heron coverage | Status | Gap | API can do it? |
|---|---|---|---|---|
| Align | align-elements, align-mep-elevation | COVERED (P) | — | — |
| Offset | offset-elements (linear runs) | PARTIAL (P) | moves only; no copy option, no line chains | YES `ElementTransformUtils.CopyElement` + perpendicular vector |
| Mirror — Pick Axis | mirror-elements | COVERED (P) | — | — |
| Mirror — Draw Axis | mirror-elements (two points) | COVERED (P) | — | — |
| Move | move-elements, move-elements-to-point, move-to-ray-hit, snap-to-grid | COVERED (P) | — | — |
| Copy | copy-elements, copy-elements-between-documents | COVERED (P) | — | — |
| Rotate | rotate-elements, rotate-elements-about-axis | COVERED (P) | — | — |
| Trim/Extend to Corner | trim-extend-elements | COVERED (P) | — | — |
| Trim/Extend Single | — | MISSING | — | YES set `LocationCurve.Curve` to boundary intersection |
| Trim/Extend Multiple | — | MISSING | — | YES same |
| Split Element | split-mep-run (duct/pipe) | PARTIAL (P) | walls, beams, lines | MEP YES (`MechanicalUtils/PlumbingUtils.BreakCurve`); walls NO direct (fragment: break exists for duct and pipe only) |
| Split with Gap | — | MISSING | — | UNSURE |
| Pin/Unpin | set-pin-state, select-by-pin-state | COVERED (P) | — | — |
| Array — Linear | array-elements | COVERED (P) | — | — |
| Array — Radial | array-elements-radial | COVERED (P) | — | — |
| Scale | — | MISSING | — | NO — `ElementTransformUtils` has no scale |
| Delete | delete-elements | COVERED (P) | — | — |
| Join Geometry | join-geometry | COVERED (P) | — | — |
| Unjoin Geometry | join-geometry (join=false) (P), unjoin-geometry (D) | COVERED (P) | — | — |
| Switch Join Order | switch-join-order | PARTIAL (D) | DRAFT | YES `JoinGeometryUtils.SwitchJoinOrder` |
| Cut Geometry | — (combine-family-forms is family-only) | MISSING | — | YES `InstanceVoidCutUtils.AddInstanceVoidCut`, `SolidSolidCutUtils.AddCutBetweenSolids` |
| Uncut Geometry | — | MISSING | — | YES `RemoveInstanceVoidCut`, `RemoveCutBetweenSolids` |
| Wall Joins | disallow-join | PARTIAL (P) | allow/disallow only; no butt/miter/square-off | YES `LocationCurve.JoinType[end]`, `WallUtils.AllowWallJoinAtEnd` |
| Beam/Column Joins | cope-framing | PARTIAL (P) | cope only | YES `StructuralFramingUtils` (join at end, end reference) |
| Split Face | — | MISSING | — | UNSURE |
| Paint / Remove Paint | paint-faces | COVERED (P) | — | — |
| Demolish | set-element-phase (phase demolished) | COVERED (P) | — | — |
| Match Type Properties | match-element-type, change-element-type | COVERED (P) | — | — |
| Create Similar | — | MISSING | — | YES `UIDocument.PromptForFamilyInstancePlacement` / `PostRequestForElementTypePlacement` (interactive) |
| Select links/underlay/pinned/by face | select-from-link, select-by-pin-state (queries) | PARTIAL (P) | the selection toggles themselves | UNSURE |
| Measure | measure-element-lengths, measure-perimeter (P), measure-distance (D) | PARTIAL | point-to-point DRAFT | YES (geometry) |
| Create Parts | create-parts, set-parts-excluded, offset-part-faces (P), divide/merge-parts (D) | COVERED (P) | — | — |
| Create Assembly | create-assembly (P), create-assembly-views (D) | COVERED (P) | — | — |
| Group / Ungroup | group-elements, ungroup-elements, place-model-groups | COVERED (P) | — | — |


### Missing or partial, API YES — priority order (most used by a modeller first)

1. Material Tag — `IndependentTag.Create` `TM_ADDBY_MATERIAL`
2. Multi-Category Tag placement — `TM_ADDBY_MULTICATEGORY`
3. Area Tag — `Document.Create.NewAreaTag`
4. Trim/Extend Single & Multiple — `LocationCurve.Curve`
5. Material Takeoff schedule — `ViewSchedule.CreateMaterialTakeoff`
6. Spot Coordinate — `Document.NewSpotCoordinate`
7. Active Workset — `WorksetTable.SetActiveWorksetId`
8. Reload Latest — `Document.ReloadLatest`
9. Structural Plan — `ViewPlan.Create` (StructuralPlan type)
10. Dimension Types — `DimensionType.Duplicate`
11. Object Styles edit — `Category.SetLineWeight/LineColor/SetLinePatternId`
12. Cut/Uncut Geometry — `InstanceVoidCutUtils`, `SolidSolidCutUtils`
13. Wall join type (butt/miter/square) — `LocationCurve.JoinType`
14. Offset with copy — `ElementTransformUtils.CopyElement`
15. Arc Length dimension (2025+) — `ArcLengthDimension.Create`
16. Masking Region (2024+) — `FilledRegion.CreateMaskingRegion`
17. Keynote file load/reload — `KeynoteTable.LoadFrom/Reload`
18. Note Block — `ViewSchedule.CreateNoteBlock`
19. Create Similar — `UIDocument.PromptForFamilyInstancePlacement`
20. Thin Lines — `ThinLinesOptions.AreThinLinesEnabled`
21. Stair tread/riser numbers — `NumberSystem.Create`
22. Shared parameter definitions — `DefinitionFile` / `ExternalDefinitionCreationOptions`
23. Project Parameters (prove DRAFT; edit/remove) — `BindingMap`
24. Project Information write — `Document.ProjectInformation`
25. Specify Coordinates at Point — `ProjectLocation.SetProjectPosition`
26. MEP Settings — `DuctSettings`, `PipeSettings`, `ElectricalSetting`
27. Transfer standards: types, fill patterns — `ElementTransformUtils.CopyElements`
28. Beam/Column joins — `StructuralFramingUtils`
29. Span Direction symbol — `SpanDirectionSymbol.Create`
30. Multi-Rebar annotation — `MultiReferenceAnnotation.Create`
31. Path / Fabric reinforcement symbols — `PathReinSpanSymbol.Create`, `FabricReinSpanSymbol.Create`
32. Structural Settings — `StructuralSettings`
33. Publish Coordinates — `Document.PublishCoordinates` (touches link: Art. 7 confirmation)
34. User Interface panes — `DockablePane.Show/Hide`
35. Beam Annotations — compose tag + spot elevation

Proving DRAFTs (already written, API YES): radial/diameter dimension, switch-join-order, global parameters create/set, relinquish (set-worksharing-ownership), sync-with-central (explicit request only, never automatic).

Confirmed NO (do not plan): Spot Slope, Repeating Detail create, Legend Component place, first Legend, Plan Region, Scope Box create, View Reference create, Phase create, Copy/Monitor, Scale, wall Split, radial/arc dims on 2020–24, Render run.
