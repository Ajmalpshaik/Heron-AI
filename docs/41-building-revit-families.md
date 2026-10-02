<!--
Heron-Agent:  none
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 41 — Building Revit families: every kind, and what makes one parametric

**Asked 2026-10-01 by the owner:** *"you have very good for family creation but i need parametric family
creation also ... line based family, ceiling based, patterns, floor based, wall based, all kind of this
including family creation and parametrics, all kind of knowledge related to family creation - do the deep
research."*

**What this page is.** The knowledge behind Heron's family tools, gathered in one place and written in
Heron's own words: which template makes which kind of family, how each kind is built, what makes a family
resize, and exactly where Revit's API stops. The skills in [`brain/skills/`](../brain/skills/) carry the
METHOD for each kind in the order it has to happen; the fragment cards carry what each tool does. This page
is the reasoning both stand on. **It is not authority** - where a card, a register row or the code
disagrees with a sentence here, the more specific source wins and the sentence is a defect
([PROJECT-MAP §D](PROJECT-MAP.md)).

**Nothing here is proven.** Every tool and skill this page names is `DRAFT` until it has been run on a real
family with a negative case ([D-30](DECISIONS.md)). The register rows that owe those runs are
[NEEDS-CHECKING Group BT](needs-checking/group-bt.md).

| What it produced | Where it lives |
|---|---|
| **Eleven new tools** for the kinds of family the library could not build or could not read: the template report, filled and masking regions, flip controls, adaptive points, curves through points, conceptual forms, a Family Type parameter, a type catalog, 2D lines for profiles and detail items, an opening in a hosted family's host, a sweep drawn with a loaded profile | §11, and each card in [`brain/fragments/`](../brain/fragments/) |
| **One tool widened**: `set-family-settings` takes Cut with Voids When Loaded, Maintain Annotation Orientation, Rotate with Component, Keep Text Readable, Enable Cutting in Views, Part Type and Profile Usage as well as its four switches | [its card](../brain/fragments/set-family-settings/fragment.yaml) |
| **Seven new skills**, one per kind, and `family-creation` widened | §11 |
| **What to measure in Revit** | [Group BT](needs-checking/group-bt.md) |
| **What disagrees, and a possible defect found on the way** | [FRAGMENT-ISSUES rows 5b-278 to 5b-280](FRAGMENT-ISSUES.md) |

---

## 0. How this was researched, and how sure each fact is

Six research passes ran on 2026-10-01, one per subject: templates and hosting; pattern-based and adaptive
families; parameters, formulas, lookup tables and type catalogs; profile, detail and annotation families;
reference lines, arrays, constraints, nesting and best practice; and MEP families. A seventh pass read
**every Revit API member a tool here calls in the reference assemblies and XML documentation of all eight
releases, 2020 to 2027** - the `Nice3point.Revit.Api.RevitAPI` NuGet packages, the same source
[`tools/check-api-surface.py`](../tools/check-api-surface.py) downloads - with
[`tools/api-surface`](../tools/api-surface/)'s `--members` reader.

**What the cloud session could not reach, said plainly:** the environment's network policy refused
`help.autodesk.com`, `forums.autodesk.com`, The Building Coder's blog host, `revitapidocs.com` and AUGI,
and the session's web-search allowance ran out part-way. So facts from Autodesk Help and the forums come
from **search-engine extracts** of the named pages, and facts from The Building Coder and the API help
from public **mirrors** of them (Jeremy Tammik's own `tbc` archive and the ADN API help pages on GitHub).
Every fact below carries one of three marks:

| Mark | Means |
|---|---|
| **API** | Read in Revit's own reference assemblies and XML remarks, for each release named, by this session |
| **SOURCED** | Stated by a named Autodesk page, Autodesk developer post or a published Revit API answer, read through an extract or a mirror |
| **UNSURE** | Practice, inference, or two sources disagreeing - owed a run in Revit before anything is built on it |

**Nothing was copied.** The mechanisms are re-authored, as [31](31-studying-the-existing-libraries.md)
requires; no sentence, code or file name from any source is carried into Heron.

---

## 1. The template decides the kind, and nothing changes it later

A family's KIND - how it is placed, what it hangs on, how its size reaches it - comes from the template it
was started from. **SOURCED:** Revit has no function to turn a wall-based family into a face-based one; the
accepted routes are starting again in the right template and copying the geometry across, or nesting the
old family inside a new one. **API:** the kind can be READ - `Family.FamilyPlacementType`,
`IsCurtainPanelFamily`, `IsConceptualMassFamily`, `AdaptiveComponentFamilyUtils.IsAdaptiveComponentFamily`
and the family's "Host" parameter (`FAMILY_HOSTING_BEHAVIOR`), all present 2020 to 2027 - and that is what
[`REPORT_FAMILY_TEMPLATE`](../brain/fragments/report-family-template/fragment.yaml) does before anything is
built.

| Kind | Placement Revit reports (**API**) | How it is placed | What the template already holds | Heron's method |
|---|---|---|---|---|
| Level-based (Generic Model, furniture, equipment) | `OneLevelBased` | one click, on a level | Center (Left/Right), Center (Front/Back), Ref. Level | `family-creation` |
| Work-plane-based | `WorkPlaneBased`, with the Work Plane-Based switch on | on a level, plane or face, and moves with it | as above | `family-creation` |
| Face-based | `WorkPlaneBased`, the switch off - the rule BT1 checks | on any face, any angle, in a linked model too | a placeholder host the project never shows (**SOURCED**) | `hosted-family-creation` |
| Wall-, ceiling-, floor-, roof-based | `OneLevelBasedHosted` | only on that host | a sample wall, ceiling, floor or roof | `hosted-family-creation` |
| Line-based | `CurveBased` | two clicks; they set its Length | the built-in instance Length (`FAMILY_LINE_LENGTH_PARAM`) and the planes it is measured between | `line-based-family-creation` |
| Two-level (a column) | `TwoLevelsBased` | between a base and a top level | a base and a top level | `family-creation` |
| Structural framing | `CurveDrivenStructural` | between two points | - | not covered here |
| Adaptive component | `Adaptive` | its numbered points clicked in order | whatever points are placed | `adaptive-family-creation` |
| Pattern-based curtain panel | `IsCurtainPanelFamily` true | applied to a divided surface, one per cell | a tile pattern grid, its points and lines (**SOURCED**) | `pattern-based-family-creation` |
| Detail item / line-based detail | `ViewBased` / `CurveBasedDetail` | in one view; the line-based one with two clicks | its view, its planes | `detail-family-creation` |
| Profile | the Profiles category | chosen in a sweep, wall sweep, reveal, railing or mullion | one plan view and two planes (**SOURCED**) | `profile-family-creation` |
| Annotation, tag, title block | an annotation category | in a view or on a sheet | its view | `detail-family-creation` - labels are by hand, §8.3 |

**Which template to start from - the order that decides it.** **SOURCED** (Autodesk's content team, via The
Building Coder): decide by FUNCTION first - is it hosted, and on what; if not, is it level-based, two-level,
line-based, face-based or adaptive - and only then by category. The template carries the hosting, the
placement and any special editor; the CATEGORY carries the options in Family Category and Parameters, the
subcategories, the built-in parameters, and how it schedules. **SOURCED:** a family that must sit on walls
or ceilings in a LINKED model has to be face-based - wall- and ceiling-based families cannot host on a
linked element - and where a category has no face-based template, the route Autodesk teaches is a
face-based Generic Model with its category changed.

**Where the templates are.** **SOURCED, and the sources disagree:** Autodesk's own pages print the family
template folder both as `...\RVT 2024 Release\Family Templates\` and as `...\RVT 2026\Family Templates\`;
the imperial set is `English_I` and the metric set, named `Metric ...`, is `English`. From Revit 2022 the
templates install as optional "Essential" content, and from 2025 they arrive in a package that can be
missing. **So a path is never typed** - [`CREATE_FAMILY_DOCUMENT`](../brain/fragments/create-family-document/fragment.yaml)
takes the template as an input, and the right folder is Options > File Locations, which the API reads as
`Application.FamilyTemplatePath`. Which metric file names ship in each release was not confirmed by any
source this session could read - **UNSURE**, and the reason no Heron card names one as fact.

---

## 2. What makes a family parametric

**SOURCED** (Autodesk's content team): *bones, muscle, skin*. The bones are reference planes and lines; the
muscle is the dimensions, the labels on them and the formulas; the skin is the solids, voids and symbolic
lines. The strongest constraint a family has is a **labelled dimension between two reference planes**, and
geometry is tied to the planes, never to other geometry. **API** echoes it in Revit's own warning: constraints
between geometry *"can behave unpredictably"*; constrain to levels, reference planes or reference lines.

The order, which is the order of `family-creation` and is not a preference:

1. **Category and settings** - the category brings built-in parameters everything after builds on.
2. **Parameters**, then **their values in a type** - a new length reads 0, and a label put on a 0 drives its
   planes onto each other. **API:** `FamilyManager.Set` needs a current type; a family with none has no
   `CurrentType` and refuses.
3. **Formulas** where one size follows another.
4. **Reference planes** at the positions the sizes put them, each with its **Is Reference**.
5. **Labels** on dimensions between the planes; an **EQ** across a centre plane keeps a shape centred.
6. **Forms drawn exactly on the planes, then locked to them.** A face drawn at the right place and not locked
   stays where it was drawn when the plane moves, and looks parametric until the first flex.
7. **Flex** - change the values and measure the geometry, at sizes that are really different and not square.

**Reference planes - what Is Reference does (SOURCED; integer values UNSURE).** Is Reference decides what a
project can dimension or snap to once the family is placed. The named values - Left, Center (Left/Right),
Right, Front, Center (Front/Back), Back, Bottom, Center (Elevation), Top - are strong references, each held by
at most one plane, and **API** a dimension to a named reference survives the family being swapped for another
with the same names, where one to a Strong or Weak Reference is not guaranteed to. A Not a Reference plane
still builds the family but gives the project nothing. A plane's NAME has nothing to do with its Is
Reference. Two planes with **Defines Origin** make the insertion point; the template's own planes carry it,
and are neither moved nor deleted.

**Constraint failures, in Revit's own words (API, `BuiltInFailures`).** *"Constraints are not satisfied"*;
*"Dimension overconstrains the Family"*; *"Reference Planes are overconstrained"*; *"Labeling this dimension
would overconstrain the sketch"*. A padlock cannot be set on a labelled, radial, diameter, arc-length or
multi-segment dimension; an EQ works on a multi-segment linear dimension only.

**Arrays (API).** A linear array keeps 2 to 200 members and a radial one 3 to 200; from 2025 Revit's remarks
add that in a FAMILY an array may hold 0 or 1, its missing members kept as placeholders. Its count is
labelled with an Integer parameter, and Revit itself suggests a NESTED family arrayed rather than identical
forms, for speed.

---

## 3. Parameters

**Three kinds (SOURCED).** Built-in parameters cannot be removed or renamed and schedule. FAMILY parameters do
not schedule and cannot be tagged. SHARED parameters carry a GUID from the office's shared parameter file and
are the only ones a tag or a multi-category schedule can read. **API:** `ReplaceParameter` turns a family
parameter into a shared one and back, carrying formulas and labels with it.

**Type or instance.** A size that describes the product is TYPE; anything that varies per placed copy - a
line-based family's Length, a nominal diameter a fitting takes from its run - is INSTANCE. **API:** a type
parameter's formula cannot use an instance parameter (*"Instance Parameters can't be used in Type Parameter
formulas"*), and an Image parameter can only be type.

**Reporting parameters (SOURCED, API).** A reporting parameter is read OFF the geometry by its dimension - it
never drives it and takes no formula. It is instance, a length or an angle, on one dimension, never a
multi-segment one; an arc length can only be labelled as reporting. **SOURCED:** it may feed other formulas
only when its dimension's references are all host elements, such as levels; a reporting parameter measured
off the family's own geometry may label but not feed a formula.

**The data types (API, `SpecTypeId`).** Common: Angle, Area, Cost per Area, Currency, Distance, Fill Pattern
(from 2023), Image, Integer, Length, Mass Density, Material, Multiline Text, Number, Rotation Angle, Slope,
Speed, Text, Time, URL, Volume, Yes/No, with discipline kinds such as HVAC Air Flow (`SpecTypeId.AirFlow`),
Piping Flow (`Flow`) and Electrical Power. A **Family Type** parameter's data type is a CATEGORY.

**The 2021 to 2025 migration (API, SOURCED).** 2021 brought `ForgeTypeId` units and specs; 2022 deprecated
`ParameterType` for `GetDataType()` and added `GroupTypeId`, with `ForgeTypeId` overloads of `AddParameter`
and `ReplaceParameter`; 2023 removed `ParameterType`; 2024 deprecated the `BuiltInParameterGroup` overloads;
2025 removed them. So every Heron tool that adds a parameter picks its call by signature at run time, and
Revit 2020 always takes a different path from 2027. The palette's "Other" group is an EMPTY `ForgeTypeId`.
**API:** `FamilyManager.SetDescription` sets a parameter's tooltip.

**A Family Type parameter (API, SOURCED).** It swaps WHICH TYPE of a nested part - or which loaded profile - a
type uses. It belongs to one category, and every loaded family of that category is offered; Revit refuses to
make one while no family of the category is loaded. **SOURCED:** linking an element's parameter to a family
parameter can fail (an Autodesk-acknowledged defect), and setting a value on the current type before linking
is the reported way round it - which is the order
[`ADD_FAMILY_TYPE_PARAMETER`](../brain/fragments/add-family-type-parameter/fragment.yaml) takes. Which of a
nested copy's own parameters carries the link no source could show: **UNSURE**, and that tool reports the one
Revit took.

---

## 4. Formulas

**SOURCED (Autodesk Help, extracts) and API:**

- Operators `+ - * / ^`; functions including `sqrt`, `sin`, `cos`, `tan`, `asin`, `acos`, `atan`, `round`,
  `roundup`, `rounddown`, `if`, `and`, `or`, `not`, `size_lookup`. **API:** `FormulaManager.GetFunctions()`
  answers for the running release.
- `if(condition, value if true, value if false)`; comparisons are `<`, `>` and `=` - **Autodesk's help says
  `<=` and `>=` are not implemented**, so `a <= b` is written `not(a > b)`. No source shows a later release
  adding them: **UNSURE** for 2025 to 2027.
- `and`, `or` and `not` take Yes/No values; text may be an `if` result, but no operation works on text.
- A value may carry its unit: `3000mm`, `Width + 100 mm`.
- **Parameter names are case-sensitive** and keep their spaces.
- **SOURCED:** a formula string is written WITHOUT a leading `=` (Autodesk's own training labs), and
  `SetFormula(p, null)` clears one (the API help). **API:** `SetFormula` refuses a circular chain of
  references, a parameter that cannot take a formula, and a family with no current type.

**Patterns built from that syntax - not proven in Revit:** keep a size from collapsing,
`if(L < 1 mm, 1 mm, L)`; clamp, `if(W < 300 mm, 300 mm, if(W > 1200 mm, 1200 mm, W))`; a count that follows a
length, `rounddown(Length / Spacing) + 1`, guarded `if(n < 2, 2, n)` on 2020 to 2024.

**Two sources disagree, and both are recorded.** The API's own remarks for a global parameter's formula say a
Length may not feed an Integer or Number formula, while `Count = Length / Spacing` is everyday family practice.
**NEEDS-CHECKING BT14** settles it in Revit.

---

## 5. Lookup tables and type catalogs

**Lookup tables (SOURCED, API).** Since Revit 2014 a lookup table's CSV is imported INTO the family and kept
there. Its header names each column `Name##KIND##UNIT` - `ND##length##millimeters`, `Note##other##`,
`Count##number##general`; **API** the header separator may be a comma, semicolon, colon or pipe. A formula
reads it with `size_lookup(Table, "Column", default, key1, key2, ...)`, the default answering when no row
matches. **SOURCED, and in conflict with a Heron card:** several sources say the FIRST column holds row names
and is never searched - the first key is matched against the second column - while
[`write-family-size-table`](../brain/fragments/write-family-size-table/fragment.yaml)'s purpose calls the first
column the key. Both are recorded ([row 5b-279](FRAGMENT-ISSUES.md)); **NEEDS-CHECKING BT15** settles it with
a negative case. **SOURCED (Autodesk's developers, via The Building Coder):** from Revit 2021.1 a UTF-8 or
UTF-16 CSV is read as such only WITH a byte-order mark; without one it is read as ANSI. **API:**
`FamilySizeTableManager` creates, imports, exports and removes tables, in a family or a project document.

**Type catalogs (SOURCED, API).** A text file with the family's name and `.txt`, beside the `.rfa`, makes Revit
show **Specify Types** on loading, so a project takes only the types it picks. Its first line STARTS with the
delimiter and names each column `Name##KIND##UNIT`; each line after is a type and its values. Revit has no
call that writes or reads one, but **API** it does give the exact words: `UnitUtils.GetTypeCatalogStringForSpec`
and `GetTypeCatalogStringForUnit` from 2021, `GetTypeCatalogString` before - which matters because **SOURCED**
Revit renamed some unit words in 2021 and older catalogs stopped loading.
[`REPORT_FAMILY_TYPE_CATALOG`](../brain/fragments/report-family-type-catalog/fragment.yaml) builds the text
from those calls and writes nothing; File > Export > Family Types is Revit's own way and the comparison its
proof uses. How Revit reads Yes/No cells, quoted cells and the file's encoding are **UNSURE** (BT10).

---

## 6. Forms: the classic Family Editor and the conceptual one

**The classic forms** - extrusion, blend, revolve, sweep, swept blend, solid or void - are built in every
family that is not conceptual, on reference planes and levels; Heron builds them with the form tools of
[Group BL](needs-checking/group-bl.md) and makes them behave with
[Group BM](needs-checking/group-bm.md). A void made through the API cuts nothing until it is combined.

**The conceptual forms** - in a mass, an adaptive component and a pattern-based panel - are one kind,
`Form`, built on POINTS and the LINES drawn through them. **API:** a single closed profile and no depth makes a
surface (`NewFormByCap`), a closed profile pushed along its normal an extrusion (`NewExtrusionForm`, the
direction's length its depth), two or more profiles a loft (`NewLoftForm`); `NewRevolveForms` can make several
forms from one call, and `NewFormByThickenSingleSurface` thickens a single-surface form. Points come from
`NewReferencePoint`, lines through them from `NewCurveByPoints` - **API:** both refuse a family that is not
conceptual; **SOURCED:** Autodesk's own adaptive sample calls them in an adaptive family. **API:** in these families a
solid can be cut by a void with `SolidSolidCutUtils`, which refuses ordinary families. **SOURCED:** a profile
family cannot be used in a conceptual family; a small nested adaptive family stands in for one.

Autodesk's remark on `NewExtrusionForm` names the families it refuses as *"Conceptual Mass, 2D, or other
family where extrusions cannot be created"* - the same words as the classic blend's, and very likely copied:
which conceptual families take each call is **NEEDS-CHECKING BT6**.

---

## 7. Hosted, line-based, pattern-based and adaptive families

**Hosted (SOURCED unless marked).** A wall-, ceiling-, floor- or roof-based family is placed only where such
a host exists; its template carries a sample host. The Family Editor's **Opening** tool is offered in hosted
templates; **API** `FamilyItemFactory.NewOpening(host, profile)` takes a wall or a ceiling and nothing else,
and an opening's sketch cannot be locked to planes through the API - so an API-made opening is a fixed size.
Whether a VOID in a wall-, ceiling-, floor- or roof-based template cuts the host without Cut Geometry: no
source said - **UNSURE**, BT12. In a FACE-BASED family an unattached void does not cut the host on placement:
either the void is cut into the template's host extrusion with Cut Geometry in the Family Editor, so every
placement cuts its host, or **Cut with Voids When Loaded** is switched on and the modeller cuts in the project.
**API:** `InstanceVoidCutUtils` is the project's half - a copy with unattached voids and that switch on cuts
host elements and Generic Model or structural copies. **Two Autodesk pages list different categories** a
family's voids may cut (one adds Casework, Furniture and Specialty Equipment): both recorded, **UNSURE**.

**Line-based.** Its Length is a built-in INSTANCE parameter, set by the two clicks; geometry runs the full
length by being locked to the start and end planes, and a part that repeats is an array whose last member is
locked to the end plane, its count a formula of the Length. **API:** a line-based model family is placed with
`NewFamilyInstance(Line, ...)` overloads; a line-based detail item with `NewFamilyInstance(Line, FamilySymbol,
View)`.

**Pattern-based panels (SOURCED, API).** The template carries a tile pattern grid - Rectangle by default - and
the points of one tile. **API:** the tile patterns are Rectangle, Triangle (bent and flat), Rhomboid, Hexagon,
Half Step, Third Step, Triangle, Rectangle and Rhomboid Checkerboard, Triangle Step, Arrows, ZigZag, Octagon and
Octagon Rotate; `Family.CurtainPanelTilePattern` is **read-only on every release**, and only the spacing can be
set - the pattern is chosen by hand in the Type Selector, and it must be the pattern of the divided surface the
panel is applied to. A divided surface takes a tile pattern or a curtain panel family's type as its own type.

**Adaptive components (SOURCED, API).** A point is a Reference Point, a Placement Point (clicked, in its number
order) or a Shape Handle Point (moved after placement). Each placement point's **Orientation** - Instance
(xyz) by default, Instance (z) then Host (xy), Host (xyz), Host and Loop System (xyz), Global (xyz), Global (z)
then Host (xy) - maps to `AdaptivePointOrientationType`. **Repeat** spreads copies along a divided path or
surface, and refuses a family with shape handles.

---

## 8. 2D families: profiles, detail items, annotations

**8.1 Profiles (SOURCED, API).** A profile is drawn with DETAIL lines in the template's one plan view, and
**every line in a profile family is part of the profile** - closed loops that do not cross, a loop inside
another a hole; construction lines belong in a nested detail item. **Profile Usage** (`FAM_PROFILE_USAGE`,
the `ProfileFamilyUsage` enum) decides where Revit offers it: Any (Generic, offered everywhere), Wall Sweep,
Reveal, Fascia, Gutter, Slab Edge, Railing, Stair Nosing, Mullion, Slab Metal Deck, Continuous Footing, Stair
Tread, Stair Riser, Stair Support. **SOURCED:** the Profiles category is internal and is not in a document's
category list - Heron reads it from the loaded families. Inside another family a sweep takes a loaded
profile as its Profile (`PROFILE_FAM_TYPE`, with Angle, flip and offsets beside it) - and a Family Type
parameter of the Profiles category swaps it per type.

**8.2 Detail items (SOURCED, API).** Detail lines, filled regions, masking regions, nested detail items and
text. **API:** a filled region in a 2D family's view is `FilledRegion.Create(..., viewId, ...)` on every release;
in a 3D model family a region on a sketch plane arrived in **2023**, and `CreateMaskingRegion` in **2024**
for both. Before 2024 a filled region type with its Masking box ticked hides what is behind it the same way.
**SOURCED:** a detail line's detail levels are not set like a symbolic line's; they live in bits of its
`GEOM_VISIBILITY_PARAM`.

**8.3 Annotations, tags and title blocks - the one wall (SOURCED, API).** A **LABEL** - the text that shows a
parameter's value - **cannot be created through Revit's API on any release from 2020 to 2027**: there is no
call for it, Autodesk's developers said so, and 2027 adds only a read-only `AnnotationLabel`. A text note can be
placed in a family's view where the family allows text. So a tag or title block is made by hand, or from a seed
family that already holds its labels - and a Heron skill says so BEFORE starting, never after. Rotate with
Component, Keep Text Readable and, from 2021, Rotate Text with Component are the family's own switches.

---

## 9. Nested families, reference lines and angles

**Nesting (SOURCED).** SHARED nested families can be selected, tagged and scheduled on their own in a
project; non-shared ones act as one unit with their host. A nested parameter is driven by linking it to a
host parameter of the same kind - the Associate Family Parameter button - and one host parameter may drive
many. Heron's tools for it are [Group BR](needs-checking/group-br.md)'s and `ADD_FAMILY_TYPE_PARAMETER`.

**Reference lines and angles - where the API stops, and why Heron stops with it.** **SOURCED:** a part that
rotates - a door leaf, a louvre blade, a lamp head - is built on a REFERENCE LINE, not a plane: its end can be
locked as a hinge, an angular dimension to it is labelled with an Angle parameter, and the line carries its own
work planes - one along it, one across it and one at each end - that the part is modelled on. **API:** the line
itself can be made (`NewModelCurve`, then `ModelCurve.ChangeToReferenceLine`, every release) and an angle
labelled (`NewAngularDimension` in a family, `Dimension.FamilyLabel`). **But no documented call gives a
reference to one of the line's own planes**, so no form can be put on one. **SOURCED:** the only route found,
by a forum user's trial, builds such a reference out of the line's stable representation with a numeric suffix -
a string Autodesk's own remarks say is not meant to be parsed. Heron does not build on an undocumented string;
reference-line angles stay **NOT YET**, recorded in [row 5b-278](FRAGMENT-ISSUES.md). The documented route to a
bend whose angle the types choose is a REVOLVE whose end angle is linked to an Angle parameter
(`LINK_FAMILY_FORM_PARAMETER`). **SOURCED:** since 2019 users report errors at angles below 0 or above 180
degrees with the line's end locked.

---

## 10. MEP families

**Connectors (API, SOURCED).** Five kinds: duct, pipe, electrical, conduit, cable tray -
`ConnectorElement.CreateDuctConnector` and its four siblings, each on a planar face, every release. A connector
points OUT of its face, and the arrow says which way a duct or pipe is drawn from it - not the flow. **API:**
`ChangeHostReference` (2023+) says a connector on a face alone sits at the face's PLANE ORIGIN and can be moved
along it, where an edge loop fixes it at the loop - **which puts a sentence of Heron's
[`add-family-connector`](../brain/fragments/add-family-connector/fragment.yaml) card in doubt** ("at that face's
centre"), recorded in [row 5b-280](FRAGMENT-ISSUES.md) and not fixed here. A family has one PRIMARY connector
per domain (`AssignAsPrimary`); linked connectors pass flow through only when their system is Global. A
FITTING's connectors take the Fitting system, which is what shows their Angle - and an elbow whose connector
Angle is not linked to its Angle parameter does not flex in a run.

**Part Type (API, SOURCED).** `FAMILY_CONTENT_PART_TYPE`, the `PartType` enum. It decides which routing
preference group offers a fitting and what a part may be swapped for: pipe fittings Cap, Cross, Elbow, Flange,
Lateral Cross, Lateral Tee, Mechanical Coupling, Multi Port, Spud (adjustable, perpendicular), Tee, Transition,
Union, Wye; duct fittings add Offset, Pants and Taps; pipe accessories Attaches To, Breaks Into, End Cap,
Valve - Breaks Into, Valve - Normal; electrical equipment Panelboard, Switchboard, Transformer, Other Panel,
Equipment Switch.

**Fixtures and equipment (SOURCED).** Plumbing fixtures take cold and hot water IN and sanitary OUT; a supply
terminal is In and its source Out. Lighting fixtures define their light source - shape and distribution,
including a photometric web (IES) - in the Family Editor; **API** `LightFamily` sets the shape and
distribution styles (not wrapped by any Heron tool). Revit 2022 added the Fire Protection, Medical Equipment and
Audio Visual Devices categories and 2023 Mechanical Control Devices and Plumbing Equipment - none exists on
2020 or 2021. Room Calculation Point keeps a family at a room's edge in the right room. **API:** an
electrical connector's "Apparent Load" was renamed "Apparent Power" in 2025 - parameters are found by their
built-in id, never their name.

---

## 11. What Heron can build now, kind by kind

| Kind | Skill | The new tools it uses |
|---|---|---|
| Level- and work-plane-based | [`family-creation`](../brain/skills/family-creation.yaml), widened | `REPORT_FAMILY_TEMPLATE`, `ADD_FAMILY_TYPE_PARAMETER`, `DRAW_FAMILY_FILLED_REGION`, `ADD_FAMILY_FLIP_CONTROL`, `REPORT_FAMILY_TYPE_CATALOG` |
| Wall-, ceiling-, floor-, roof-, face-based | [`hosted-family-creation`](../brain/skills/hosted-family-creation.yaml) | `CREATE_FAMILY_HOST_OPENING`, `SET_FAMILY_SETTINGS` (Cut with Voids When Loaded, Maintain Annotation Orientation) |
| Line-based | [`line-based-family-creation`](../brain/skills/line-based-family-creation.yaml) | `REPORT_FAMILY_TEMPLATE` with the arrays and locks already built |
| Pattern-based panel | [`pattern-based-family-creation`](../brain/skills/pattern-based-family-creation.yaml) | `DRAW_FAMILY_POINT_CURVES`, `CREATE_CONCEPTUAL_FORM` |
| Adaptive component | [`adaptive-family-creation`](../brain/skills/adaptive-family-creation.yaml) | `PLACE_ADAPTIVE_POINTS`, `DRAW_FAMILY_POINT_CURVES`, `CREATE_CONCEPTUAL_FORM` |
| Profile | [`profile-family-creation`](../brain/skills/profile-family-creation.yaml) | `DRAW_FAMILY_DETAIL_LINES`, `SET_FAMILY_SETTINGS` (Profile Usage), `SET_FAMILY_SWEEP_PROFILE` |
| Detail item, annotation symbol | [`detail-family-creation`](../brain/skills/detail-family-creation.yaml) | `DRAW_FAMILY_DETAIL_LINES`, `DRAW_FAMILY_FILLED_REGION` |
| Duct or pipe fitting | [`mep-fitting-family-creation`](../brain/skills/mep-fitting-family-creation.yaml) | `SET_FAMILY_SETTINGS` (Part Type) with the size table and connector tools already built |

**NOT YET, and why - so that nobody reads this table as more than it is:**

| Not built | Why |
|---|---|
| A part that rotates about a reference line | No documented call gives a reference line's planes (§9) |
| A label in a tag, symbol or title block | Revit's API cannot make one on any release (§8.3) |
| Setting a pattern-based panel's tile pattern | `CurtainPanelTilePattern` is read-only (§7) |
| A point hosted on a line at a ratio; a profile on a point's plane | Calls exist (`PointOnEdge`, a point's coordinate planes) and are not wrapped yet |
| A connector's primary flag, linked connectors, a light source | Calls exist and are not wrapped yet |
| A hosted opening whose size follows a parameter | An opening's sketch cannot be locked through the API; a void is the route, owed BT12 |

---

## 12. Before a family leaves the modeller's desk

**SOURCED** where a source said it; the rest is practice and marked so.

- **The template by function first, the category second** (§1); a face-based family where a linked model
  will host it.
- **Planes, then labels, then forms locked to the planes** - never geometry to geometry (§2).
- **Values before labels**, two or more types made before any geometry, and every type flexed, the host
  flexed too for a hosted family (SOURCED: Autodesk's content team's process).
- **Name what a project dimensions to** with the named Is Reference values, consistently across families that
  will be swapped for one another.
- **Shared parameters** for anything a tag or schedule reads (§3).
- **Light geometry:** visibility by detail level, a plan symbol at coarse, nested arrays rather than many
  identical forms (SOURCED, Revit's own warning text); **no imported CAD in a family** (SOURCED: it
  severely degrades performance); few voids (practice, UNSURE).
- **In-place families are for one-off project content** - they live only in that project and cannot be opened
  as family documents through the API (SOURCED).
- **Author in the oldest release the office uses** - a family cannot be saved back to an earlier release
  (practice; the rule as a rule is UNSURE).
- The Autodesk Revit Model Content Style Guide and the NBS BIM Object Standard could not be reached this
  session; nothing here is attributed to them.

---

## 13. Where each conflict and debt is kept

| What | Where |
|---|---|
| The runs every new tool owes, with negative cases | [NEEDS-CHECKING Group BT](needs-checking/group-bt.md) and [`tools/jobs/family-kinds-2026-10-02.yaml`](../tools/jobs/family-kinds-2026-10-02.yaml) |
| The new tools and skills, and reference-line angles left NOT YET | [Row 5b-278](FRAGMENT-ISSUES.md) |
| A lookup table's first column: a Heron card and the sources disagree | [Row 5b-279](FRAGMENT-ISSUES.md) |
| A connector's position on its face: a Heron card and Autodesk's remark disagree | [Row 5b-280](FRAGMENT-ISSUES.md) |

---

## 14. Sources

Read through the extract or mirror each line says. None is quoted at length; the mechanisms are re-authored.

- **Revit API reference assemblies and XML documentation, 2020 to 2027** - `Nice3point.Revit.Api.RevitAPI`
  2020.2.90, 2021.1.100, 2022.1.80, 2023.1.90, 2024.3.60, 2025.4.60, 2026.4.10 and 2027.3.0, on nuget.org.
- **Autodesk Help** (search extracts): About Family Templates; Family Category and Parameters; Cut Geometry in a
  Family; Openings in families; Reference planes and Is Reference; Reference lines and angular constraints;
  Valid formula syntax; Conditional statements; Lookup tables; Create a type catalog; Reporting parameters;
  Connector properties and Part Types; Light source definition - help.autodesk.com, cloudhelp 2016 to 2027.
- **The Building Coder** (Jeremy Tammik's `tbc` archive on GitHub): key concepts of the Family Editor (Autodesk's
  content team); family API; type catalogs; encoding of lookup tables; form creation and debugging; What's New
  posts for the 2011, 2013, 2014, 2016, 2021, 2022, 2023, 2024 and 2025 APIs.
- **ADN DevTech** Revit API help pages and training labs on GitHub; Autodesk's AEC DevBlog posts on profiles,
  detail lines and labels.
- **Autodesk forums** (search extracts): converting hosted to face-based families; face-based voids;
  reference line planes; line-based families; Family Type parameter association; rotation limits since 2019.
- **AUGI, Arkance, Imaginit, Novedge and practitioners' notes** (search extracts) for practice, each marked
  where used.
