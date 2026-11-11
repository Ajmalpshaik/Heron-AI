# A family's 2D side - what Heron can do, what it cannot, and what was built

**2026-11-11. Active** - owner of the work: the session that wrote it, for Ajmal PS. Closure: every row
below marked MISSING is either built (and its proof plan written) or recorded as a decision not to; then
the durable part moves to the cards and [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) and this note is
deleted.

**Why.** Ajmal PS built a parametric diffuser family through Heron on Revit 2020. Adding plan-view flow
arrows - a nested 600 mm detail item, the way Autodesk's own supply diffuser carries them - stopped three
times in a row on a missing or failing tool. He asked for the whole class of "2D in a family" covered at
once, not one gap at a time.

> Nothing here is specification. Where it disagrees with a fragment's card, the card wins. Statuses are
> read from each card's `heron-status:` line on 2026-11-11 and go stale: re-read them with
> `grep -h '^heron-status:' brain/fragments/<card>/fragment.yaml`. API facts were read from the RevitAPI
> reference assemblies 2020 through 2027. **Nothing in this note has been run in Revit.**

## Scope

A 3D family (Generic Model, Air Terminal, Mechanical Equipment) carrying a plan symbol, and Detail Item and
Generic Annotation families. Eight needs, as the task named them.

**Two pieces are another session's and are not duplicated here:** LOCK_NESTED_FAMILY_TO_PLANES on a detail
item (version 4 on branch `claude/lock-nested-detail-item-no-owner-view`, row 5b-1494) and
LOCK_FAMILY_LINES_TO_PLANES refusing a plain detail line (NEEDS-CHECKING Group NC, being fixed in its own
session).

## The gap table

PROVEN / DRAFT is the card's status. **MISSING** = no card does it. **NO API** = Revit's API has no call
for it on any release 2020-2027, so no card can do it; the modeller does it by hand.

| # | Need | Card | Status | Gap, and what is done about it |
|---|---|---|---|---|
| 1a | Place a nested DETAIL ITEM or GENERIC ANNOTATION into a family VIEW (owner view) | PLACE_DETAIL_COMPONENTS - projects only (`doc.Create`); PLACE_FAMILY_INSTANCES v5 - nested families on a plane or level only | PROVEN, PROVEN | **MISSING.** `FamilyCreate.NewFamilyInstance(XYZ, FamilySymbol, View)` exists on every release (ItemFactoryBase). Widen PLACE_DETAIL_COMPONENTS to a family document - see "Widenings" |
| 1b | Rotate it | ROTATE_ELEMENTS (vertical axis) | PROVEN | Covered for an item in a PLAN view. Not measured in a family. An item in an elevation view turns about the wrong axis - recorded, not built |
| 1c | Mirror it | MIRROR_ELEMENTS (vertical plane through two plan points) | PROVEN | Covered in plan; not measured in a family |
| 1d | Array it | ARRAY_FAMILY_FORMS v3 (nested family: arrays in a 3D view) | PROVEN | **MISSING for a view-based nested family** - a detail item does not show in a 3D view, so its array must be made in its owner view. Widen - see "Widenings" |
| 2 | Lock nested 2D families to planes | LOCK_NESTED_FAMILY_TO_PLANES | DRAFT | Another session's (above) |
| 3a | Draw detail lines / symbolic lines, arcs included | DRAW_FAMILY_DETAIL_LINES v2, DRAW_FAMILY_SYMBOLIC_LINES v1 | PROVEN, PROVEN | Covered |
| 3b | Lock a line ALONG a plane, a line END to a crossing plane | LOCK_FAMILY_LINES_TO_PLANES v1 | DRAFT | Covered on paper; its first run refused a plain detail line (Group NC) - another session's |
| 3c | Slanted lines follow parameters | the same, by end locks | DRAFT | As 3b |
| 3d | An ARC's RADIUS following a parameter | LABEL_FAMILY_RADIUS (form sketches only); CREATE_RADIAL_DIMENSION refuses a family | PROVEN, PROVEN | **MISSING.** `FamilyItemFactory.NewRadialDimension(View, Reference, XYZ)` exists 2020-2027. Widen LABEL_FAMILY_RADIUS to a detail/symbolic arc - see "Widenings" |
| 3e | An arc's CENTRE locked to planes | LOCK_FAMILY_LINES_TO_PLANES (straight lines + ends); LOCK_FORM_SKETCH_TO_PLANES (forms' circles) | DRAFT, DRAFT | **MISSING.** Belongs in LOCK_FAMILY_LINES_TO_PLANES, which is another session's - recorded for it, not built here |
| 4a | Draw a filled / masking region | DRAW_FAMILY_FILLED_REGION v2 | PROVEN | Covered. A 3D family's region needs Revit 2023+; a masking region 2024+ - the API's limit |
| 4b | Lock a region's edges / corners | LOCK_FAMILY_LINES_TO_PLANES | DRAFT | As 3b |
| 4c | Change a region's type | CHANGE_ELEMENT_TYPE v2 | PROVEN | Covered on paper (`ChangeTypeId`); not measured on a region in a family |
| 4d | A region's Visible on a Yes/No | LINK_FAMILY_PARAMETER v3 (ids: forms and nested families only) | PROVEN | **MISSING.** Widen - see "Widenings" |
| 4e | A region's Visibility Settings (view direction, detail level) | - | - | **NO API.** `FilledRegion` has no Get/SetVisibility on any release |
| 5a | Label plane to plane, and EQ | LABEL_FAMILY_DIMENSION v2 | PROVEN | Covered |
| 5b | Label line to line, line to plane | - | - | Not built, on purpose: lines are locked to planes (3b) and the PLANES labelled (5a), as Autodesk's own families are made. A label on a line stretches nothing a locked plane would not |
| 5c | ANGULAR label (an angle parameter between two planes) | LABEL_FAMILY_DIMENSION (parallel planes); CREATE_ANGULAR_DIMENSION refuses a family | PROVEN, PROVEN | **MISSING.** `FamilyItemFactory.NewAngularDimension(View, Arc, Reference, Reference)` exists 2020-2027. Widen LABEL_FAMILY_DIMENSION - see "Widenings" |
| 6a | Symbolic line visibility (plan / front-back / left-right, coarse / medium / fine) | DRAW_FAMILY_SYMBOLIC_LINES sets it when drawing; SET_FAMILY_FORM_VISIBILITY changes forms only | PROVEN, PROVEN | **MISSING for lines already drawn** (`SymbolicCurve.SetVisibility` exists 2020-2027). Widen SET_FAMILY_FORM_VISIBILITY |
| 6b | A nested family's Visibility Settings | - | - | **NO API** - the Family Editor shows it for a nested instance; the API has no call. By hand |
| 6c | Yes/No linking of 2D | LINK_FAMILY_PARAMETER (forms, nested families) | PROVEN | Nested families covered; **lines and regions MISSING** - as 4d |
| 6d | "Visible in plan only" | symbolic lines (6a) in a 3D family; a detail item in a plan owner view (1a) | - | Follows from 6a and 1a |
| 6e | Maintain Annotation Orientation | SET_FAMILY_SETTINGS v4 (set in the nested family) | PROVEN | Covered |
| 6f | Subcategory of 2D | DRAW_* set it when drawing; SET_FAMILY_FORM_SUBCATEGORY forms only | PROVEN | **MISSING for lines already drawn** (`CurveElement.LineStyle`). Widen SET_FAMILY_FORM_SUBCATEGORY |
| 6g | Object Styles of a subcategory | SET_OBJECT_STYLE v1 | DRAFT | Covered on paper; not measured in a family document |
| 7a | Family Type parameter switching a nested type | ADD_FAMILY_TYPE_PARAMETER v1 | DRAFT | Covered on paper, unproven |
| 7b | Nested TYPE parameter links | LINK_FAMILY_PARAMETER v3 (`family=`, `type=`) | PROVEN | Covered |
| 7c | Nested INSTANCE parameter links | LINK_FAMILY_PARAMETER v3 | PROVEN | Covered |
| 8 | Read it all back - view, ends, locks, links | REPORT_FAMILY_FORMS (forms), REPORT_FAMILY_LABELS (labels) | PROVEN | **MISSING. BUILT:** REPORT_FAMILY_2D_ELEMENTS, new READ card, DRAFT - row 5b-1496, [Group ND](../../needs-checking/group-nd.md) |

## Built in this pass

**REPORT_FAMILY_2D_ELEMENTS** (`brain/fragments/report-family-2d-elements`, FRG-GEO-084, DRAFT). Reads
every detail line, symbolic line, filled and masking region and nested family in the family open: view or
plane, ends, style, visibility, Visible and its link, nested parameter links, and every lock, label and EQ
on it, said in words ("locked along Head Top", "locked at an end to Tip"). A new card rather than a
widened REPORT_FAMILY_FORMS: that one is PROVEN and about forms. READ only, so it can run through
`revit_read` as soon as it is merged and the store rebuilt - and it is the measurement the Group NC and NB
sessions need ("is the shaft really a loose detail line?").

## Widenings - waiting on the owner's yes

Every remaining MISSING row is closed by widening a PROVEN card. **A change under a PROVEN card's `impl/`
makes its proof stale: it goes back to DRAFT and `revit_change` refuses it until it is proved again** - so
each one stops a tool that works today, until its proof is run. The fragment-author rule is to say so and
wait for the owner's yes before touching it. Asked on 2026-11-11; the answer decides the second pull
request.

| Card to widen | Rows it closes | The API call it adds |
|---|---|---|
| PLACE_DETAIL_COMPONENTS | 1a | `doc.FamilyCreate.NewFamilyInstance(XYZ, FamilySymbol, View)` in a family document |
| ARRAY_FAMILY_FORMS | 1d | the array made in the nested instance's owner view, not a 3D view |
| SET_FAMILY_FORM_VISIBILITY | 6a | `SymbolicCurve.SetVisibility` |
| SET_FAMILY_FORM_SUBCATEGORY | 6f | `CurveElement.LineStyle` for detail and symbolic lines |
| LINK_FAMILY_PARAMETER | 4d, 6c | lines' and regions' ids accepted for Visible |
| LABEL_FAMILY_DIMENSION | 5c | `FamilyItemFactory.NewAngularDimension` labelled with an angle parameter |
| LABEL_FAMILY_RADIUS | 3d | `FamilyItemFactory.NewRadialDimension` on a 2D arc |

Recorded for LOCK_FAMILY_LINES_TO_PLANES's session, not built here: an arc's centre (3e).

## What the duplicate check found (heron-duplicate-checker, 2026-11-11)

`brain/heron_merge.py` over the library: no signature group near these jobs. Read card by card:

- **Item 8** OVERLAPS REPORT_FAMILY_FORMS (the same kind of answer for forms). Widening it or a new card was
  the choice; a new card was made so a PROVEN tool is not made stale - the same reasoning as
  LOCK_FAMILY_LINES_TO_PLANES on the same day. REPORT_FAMILY_LABELS' "Visible linked to" is the pattern
  followed. If the owner prefers one card, a merge later is `heron-fragment-merger`'s job.
- **Traps for the widenings**, read in the C#: PLACE_DETAIL_COMPONENTS calls `doc.Create` and has no
  read-back; ARRAY_FAMILY_FORMS reads a nested instance's extent with `get_BoundingBox(null)`, empty for
  a view-based instance (the trap LOCK_NESTED_FAMILY_TO_PLANES v3 hit); a symbolic line's visibility is
  `FamilyElementVisibilityType.ViewSpecific`, not `Model` as SET_FAMILY_FORM_VISIBILITY writes - and
  whether a view-specific setting has view directions at all is unmeasured; a detail line's style must
  be one of `GetLineStyleIds()`; REMAP_LINE_STYLES sees only styles under Lines, never a family's own
  subcategories; LINK_FAMILY_PARAMETER's kind check refuses all but forms and nested families;
  LABEL_FAMILY_DIMENSION checks values as lengths and needs an angle parameter for 5c.
