# A family's 2D side - what Heron can do, what it cannot, and what was built

**2026-11-11. Active** - owner of the work: the session that wrote it, for Ajmal PS. Closure: the three pull
requests below are merged or closed by the owner, each widened card is proved again (its NEEDS-CHECKING group),
and the merge candidates are decided; then the durable part is on the cards and in
[FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) and this note is deleted.

**Why.** Ajmal PS built a parametric diffuser family through Heron on Revit 2020. Adding plan-view flow
arrows - a nested 600 mm detail item, the way Autodesk's own supply diffuser carries them - stopped three
times in a row on a missing or failing tool. He asked for the whole class of "2D in a family" covered at
once, not one gap at a time.

**His two rules for it (2026-11-11, relayed by the session that started this work):** WIDEN the existing
tools - the new ability is part of what they already do - and never make two tools for the same kind of
work. And: *is adding to these tools ALL that is needed, or is more missing?* - answered under
"Is this everything?" below.

> Nothing here is specification. Where it disagrees with a fragment's card, the card wins. Statuses are
> read from each card's `heron-status:` line on 2026-11-11 and go stale: re-read them with
> `grep -h '^heron-status:' brain/fragments/<card>/fragment.yaml`. API facts were read from the RevitAPI
> reference assemblies 2020 through 2027. **Nothing in this note has been run in Revit.**

## Scope

A 3D family (Generic Model, Air Terminal, Mechanical Equipment) carrying a plan symbol, and Detail Item and
Generic Annotation families. Eight needs, as the task named them.

**Two pieces are other sessions' and are not redone here:** LOCK_NESTED_FAMILY_TO_PLANES on a detail item
with no owner view ([#842](https://github.com/Ajmalpshaik/Heron-AI/pull/842), row 5b-1494) and
LOCK_FAMILY_LINES_TO_PLANES refusing a plain detail line (Group NC, being fixed in its own session).

## The gap table

**Answer** column: **have PROVEN** / **have DRAFT** = a card does it today; **widen** = an existing card is
widened in one of the pull requests below; **new** = a new card (none - every gap went to an existing card);
**NO API** = Revit's API has no call for it on any release 2020-2027, so no tool can do it and the modeller
does it by hand.

| # | Need | Card | Answer | Detail |
|---|---|---|---|---|
| 1a | Place a nested DETAIL ITEM or GENERIC ANNOTATION into a family VIEW (owner view) | PLACE_DETAIL_COMPONENTS | **widen** (PR C) | Today projects only (`doc.Create`). `FamilyCreate.NewFamilyInstance(XYZ, FamilySymbol, View)` exists on every release. **Nested flow arrows should be placed with THIS card in the plan view, not with PLACE_FAMILY_INSTANCES** - that one places on a level or plane, which gives a detail item no owner view (row 5b-1494) |
| 1b | Rotate it | ROTATE_ELEMENTS (vertical axis) | **have PROVEN** | In a PLAN view. Not measured in a family; an item in an elevation view turns about the wrong axis - recorded, not built |
| 1c | Mirror it | MIRROR_ELEMENTS (vertical plane through two plan points) | **have PROVEN** | In plan; not measured in a family |
| 1d | Array it | ARRAY_FAMILY_FORMS | **widen** (PR C) | Today arrays a nested family in a 3D view, where a detail item does not show; widened to its owner view |
| 2 | Lock nested 2D families to planes | LOCK_NESTED_FAMILY_TO_PLANES | **have PROVEN** | v4 (#842, merged 2026-11-11), proved on Revit 2020 (Group NB), a detail item with no owner view included |
| 3a | Draw detail lines / symbolic lines, arcs included | DRAW_FAMILY_DETAIL_LINES, DRAW_FAMILY_SYMBOLIC_LINES | **have PROVEN** | - |
| 3b | Lock a line ALONG a plane, a line END to a crossing plane | LOCK_FAMILY_LINES_TO_PLANES | **have DRAFT** | Its first run refused a plain detail line (Group NC); fixed in v2 ([#844](https://github.com/Ajmalpshaik/Heron-AI/pull/844), merged 2026-11-11, row 5b-1495), still DRAFT until Group NC passes |
| 3c | Slanted lines follow parameters | the same, by end locks | **have DRAFT** | As 3b |
| 3d | An ARC's RADIUS following a parameter | LABEL_FAMILY_RADIUS | **widen** (PR C) | Today form sketches only; `FamilyItemFactory.NewRadialDimension` on a 2D arc |
| 3e | An arc's CENTRE locked to planes | LOCK_FAMILY_LINES_TO_PLANES | **widen - owed, not in these PRs** | Belongs to that card, which its own session is changing now; given to it, not built here, so two sessions do not edit one card |
| 4a | Draw a filled / masking region | DRAW_FAMILY_FILLED_REGION | **have PROVEN** | A 3D family's region needs Revit 2023+, a masking region 2024+ - the API's limit |
| 4b | Lock a region's edges / corners | LOCK_FAMILY_LINES_TO_PLANES | **have DRAFT** | As 3b |
| 4c | Change a region's type | CHANGE_ELEMENT_TYPE | **have PROVEN** | `ChangeTypeId`; not measured on a region in a family |
| 4d | A region's Visible on a Yes/No | LINK_FAMILY_PARAMETER | **widen** (PR B) | Today forms and nested families only |
| 4e | A region's Visibility Settings (view direction, detail level) | - | **NO API** | `FilledRegion` has no Get/SetVisibility on any release |
| 5a | Label plane to plane, and EQ | LABEL_FAMILY_DIMENSION | **have PROVEN** | - |
| 5b | Label line to line, line to plane | - | **not built, on purpose** | Lines are locked to planes (3b) and the PLANES labelled (5a), as Autodesk's own families are made; a label on a line stretches nothing a locked plane would not |
| 5c | ANGULAR label (an angle parameter between two planes) | LABEL_FAMILY_DIMENSION | **widen** (PR B) | `FamilyItemFactory.NewAngularDimension`; CREATE_ANGULAR_DIMENSION refuses a family |
| 5d | A reference plane drawn AT AN ANGLE (what an angle label needs) | CREATE_REFERENCE_PLANES (planes along the family's axes only) | **widen - owed, not in these PRs** | Found while writing PR B (2026-11-11): its proof draws the sloped plane by hand. Recorded in row 5b-1498 |
| 6a | Symbolic line visibility (plan / front-back / left-right, coarse / medium / fine) after drawing | SET_FAMILY_FORM_VISIBILITY | **widen** (PR C) | `SymbolicCurve.SetVisibility`; DRAW_FAMILY_SYMBOLIC_LINES sets it only when drawing |
| 6b | A nested family's Visibility Settings | - | **NO API** | The Family Editor shows it for a nested instance; the API has no call. By hand |
| 6c | Yes/No linking of 2D | LINK_FAMILY_PARAMETER | **have PROVEN** for nested families; **widen** (PR B) for lines and regions | As 4d |
| 6d | "Visible in plan only" | symbolic lines (6a), a detail item in a plan owner view (1a) | follows from 6a and 1a | - |
| 6e | Maintain Annotation Orientation | SET_FAMILY_SETTINGS | **have PROVEN** | Set in the nested family |
| 6f | Subcategory of lines after drawing | SET_FAMILY_FORM_SUBCATEGORY | **widen** (PR C) | `CurveElement.LineStyle`; DRAW_* set it only when drawing |
| 6g | Object Styles of a subcategory | SET_OBJECT_STYLE | **have DRAFT** | Not measured in a family document |
| 7a | Family Type parameter switching a nested type | ADD_FAMILY_TYPE_PARAMETER | **have DRAFT** | Unproven |
| 7b | Nested TYPE parameter links | LINK_FAMILY_PARAMETER (`family=`, `type=`) | **have PROVEN** | - |
| 7c | Nested INSTANCE parameter links | LINK_FAMILY_PARAMETER | **have PROVEN** | - |
| 8 | Read it all back - view, ends, locks, links | REPORT_FAMILY_FORMS | **widen** (PR A) | Today forms only. First written as a new card, then folded in under the no-duplicate rule |

## Is this everything?

**No - but nearly.** Adding to the EIGHT tools above in pull requests A, B and C
(REPORT_FAMILY_FORMS, LINK_FAMILY_PARAMETER, LABEL_FAMILY_DIMENSION, PLACE_DETAIL_COMPONENTS,
ARRAY_FAMILY_FORMS, SET_FAMILY_FORM_VISIBILITY, SET_FAMILY_FORM_SUBCATEGORY, LABEL_FAMILY_RADIUS) closes every
gap but these:

1. **An arc's centre locked to planes (3e)** - one more widening, of LOCK_FAMILY_LINES_TO_PLANES, owed to the
   session already changing that card.
2. **A reference plane at an angle (5d)** - one more widening, of CREATE_REFERENCE_PLANES. Until then the
   sloped plane an angle label needs is drawn by hand.
3. **The in-flight fixes have landed:** LOCK_NESTED_FAMILY_TO_PLANES v4 (#842) is merged and PROVEN;
   LOCK_FAMILY_LINES_TO_PLANES v2 (#844) is merged, DRAFT until Group NC passes.
4. **Two things no tool can do (NO API)** - a filled region's and a nested family's Visibility Settings.
   Done by hand, or avoided: put the region inside the nested 2D family and control that family's
   visibility by its view and Visible.
5. **PROOFS.** Every "have DRAFT" and "widen" row is unproven until its NEEDS-CHECKING group runs on a
   scratch family. Nothing in this note has run in Revit.

So the six tools first named were not the whole list: the read-back (REPORT_FAMILY_FORMS) and the 2D arc's
radius (LABEL_FAMILY_RADIUS) were added; the arc centre and the angled plane are still owed.

## The pull requests

| PR | Cards (each back to DRAFT, version raised) | Proof plan | Why split so |
|---|---|---|---|
| A - `claude/report-family-2d-elements` | REPORT_FAMILY_FORMS v5 (READ) + this note | [Group ND](../../needs-checking/group-nd.md), row 5b-1496 | READ: `revit_read` still runs a DRAFT, so nothing is blocked |
| B - #849, **MERGED 2026-11-11** | LINK_FAMILY_PARAMETER v4, LABEL_FAMILY_DIMENSION v3 | Group NE, rows 5b-1497, 5b-1498 | Used in every family build: small, so it can be merged and re-proved first and family work is blocked as briefly as possible |
| C - `claude/family-2d-place-show-label` | PLACE_DETAIL_COMPONENTS v2, ARRAY_FAMILY_FORMS v4, SET_FAMILY_FORM_VISIBILITY v2, SET_FAMILY_FORM_SUBCATEGORY v2, LABEL_FAMILY_RADIUS v6 | Group NF, rows 5b-1499 to 5b-1503 | The rest |

**What a widening costs, said plainly:** a change under a PROVEN card's `impl/` makes its proof stale, so the
card goes back to DRAFT and `revit_change` refuses it until it is proved again. Each proof plan re-proves the
card's OLD job and proves the new one, each with a negative case.

## Existing cards that may already be duplicates (merge candidates)

Not merged - each waits for the owner's yes, then goes to `heron-fragment-merger`. From the duplicate
checker's second pass (2026-11-11, origin/main 1279ce3c): **no two live cards on a family's 2D side do the
SAME job today.** Three pairs OVERLAP and are candidates, none urgent:

| Pair | Verdict | If merged |
|---|---|---|
| DRAW_FAMILY_DETAIL_LINES / DRAW_FAMILY_SYMBOLIC_LINES | OVERLAP, weak - same shape language and read-back; each refuses the other's kind of family (2D vs 3D), as Revit keeps two tools | keep DRAW_FAMILY_DETAIL_LINES (it already took in CREATE_LINE) |
| LOCK_FAMILY_LINES_TO_PLANES / LOCK_FORM_SKETCH_TO_PLANES | OVERLAP - the same lock-what-already-lies-there method, loose 2D lines vs a form's sketch | revisit once Group NC proves the lines card; one card would also hold the arc centre (3e) |
| ADD_FAMILY_TYPE_PARAMETER / LINK_FAMILY_PARAMETER | OVERLAP, weak - both link a nested part's parameter; only the first makes the Family Type parameter and sets each type's value before linking, so the parts never swap | keep separate unless LINK_FAMILY_PARAMETER gains that guard |

Already merged earlier (DEPRECATED, nothing to do): ROTATE_ELEMENTS_ABOUT_AXIS into ROTATE_ELEMENTS,
CREATE_FILLED_REGION into DRAW_FAMILY_FILLED_REGION, PLACE_NESTED_FAMILY into PLACE_FAMILY_INSTANCES.
LOOK-ALIKES, kept apart with a reason: PLACE_DETAIL_COMPONENTS / PLACE_FAMILY_INSTANCES (split by family
kind), CREATE_ANGULAR_DIMENSION and CREATE_RADIAL_DIMENSION / the LABEL_FAMILY_* cards (project vs family),
SET_OBJECT_STYLE / SET_FAMILY_FORM_SUBCATEGORY, PLACE_FLOW_ARROWS / PLACE_DETAIL_COMPONENTS, ADD_FAMILY_MODEL_TEXT
/ CREATE_TEXT_NOTE.

## What the duplicate check found (heron-duplicate-checker, 2026-11-11)

`brain/heron_merge.py` over the library: no signature group near these jobs. Read card by card, every gap
has an existing owner, so no new card was made. Traps it read in the C#, handed to the widenings:
PLACE_DETAIL_COMPONENTS calls `doc.Create` and has no read-back; ARRAY_FAMILY_FORMS reads a nested instance's
extent with `get_BoundingBox(null)`, empty for a view-based instance (the trap LOCK_NESTED_FAMILY_TO_PLANES v3
hit); a symbolic line's visibility is `FamilyElementVisibilityType.ViewSpecific`, not `Model` as
SET_FAMILY_FORM_VISIBILITY writes - and whether a view-specific setting has view directions at all is
unmeasured; a detail line's style must be one of `GetLineStyleIds()`; REMAP_LINE_STYLES sees only styles
under Lines, never a family's own subcategories; LINK_FAMILY_PARAMETER's kind check refuses all but forms and
nested families; LABEL_FAMILY_DIMENSION checks values as lengths and needs an angle parameter for 5c.

## Where it stands after the Revit 2020 runs (2026-11-12) - read this section first

Groups NE and NF were run on Revit 2020 on saved scratch families made for the purpose (never the owner's own),
every gap met on the way built and proved in the same families, on the owner's standing word. Each card below was
SIGNED by Ajmal PS and marked PROVEN on the run named; second routes and Revit 2024 were not run.

| Row | Need | Now | Where |
|---|---|---|---|
| 1a | Detail item into a family view | **NOT POSSIBLE on Revit 2020 through the API**: inside a family Revit reports a nested Detail Items family WorkPlaneBased and refuses it in a view. The route that works and is PROVEN: `PLACE_FAMILY_INSTANCES` on the level, then `LOCK_NESTED_FAMILY_TO_PLANES` (locks it in the plan of its level). `PLACE_DETAIL_COMPONENTS` v2 PROVEN for projects and refuses the family case naming that route | row 5b-1573, #846 |
| 1d | Array it | **PROVEN** - a detail item with no view arrayed in its level's plan | `ARRAY_FAMILY_FORMS` v4, row 5b-1574, #846 |
| 2 | Lock nested 2D to planes | **PROVEN** | `LOCK_NESTED_FAMILY_TO_PLANES` v4, #842 (merged) |
| 3b, 3c, 4b | Lock lines / ends / region edges | **DRAFT** - fixed in v2, Group NC not yet re-run | #844 (merged) |
| 3d | Arc radius follows a parameter | **PROVEN** - detail arc 150 -> 200 kept and read back | `LABEL_FAMILY_RADIUS` v6, row 5b-1572, #846 |
| 3e | Arc centre locked to planes | **STILL OWED** | - |
| 4d, 6c | Visible of lines / regions / model text on a Yes/No | **PROVEN** | `LINK_FAMILY_PARAMETER` v4, #849 + #856 (merged) |
| 5c | Angular label | **PROVEN** - 30, 45 and 150 degrees (the obtuse side fixed) | `LABEL_FAMILY_DIMENSION` v3, row 5b-1565, #856 (merged) |
| 5d | Plane at an angle | **PROVEN** | `CREATE_REFERENCE_PLANES` v2, row 5b-1564, #856 (merged) |
| 6a | Symbolic line visibility after drawing | **PROVEN for detail levels**; kinds of view **NOT POSSIBLE through the API** - a symbolic line's setting is ViewSpecific and Revit takes no view kinds on it; the card refuses rather than guesses | `SET_FAMILY_FORM_VISIBILITY` v2, #846 |
| 6f | Subcategory of lines after drawing | **PROVEN**, and "none" works on 2020 for the first time | `SET_FAMILY_FORM_SUBCATEGORY` v2, row 5b-1571, #846 |
| 8 | Read it all back | **PROVEN** (lines and regions; the lock-reading rows of Group ND not run) | `REPORT_FAMILY_FORMS` v5, #843 |
| - | Air flow and other measured type values (met in Group NE) | **PROVEN** | `SET_FAMILY_TYPE_VALUES` v4, row 5b-1575, #858 |

**Is this everything now?** Nearly. Still owed: an arc's centre locked to planes (3e), Group NC re-run on
`LOCK_FAMILY_LINES_TO_PLANES` v2, Group ND's lock-reading rows, and every test on Revit 2024. Beyond any tool (NO
API): a filled region's, a nested family's and a symbolic line's view-direction settings, and placing a nested
detail item in a family VIEW.

## Revit 2024, 2026-11-12 - the ten cards of this job proved there too

Run on Revit 2024 (session 13164) on SAVED scratch files made for it under `%TEMP%\heron-work\rev2024` (R24 Generic,
R24 Arrow, R24 Project), arranged with the cards PROVEN on 2020 that morning - the angled planes by
`CREATE_REFERENCE_PLANES` v2, the air flow by `SET_FAMILY_TYPE_VALUES` v4, the Rim Height label by
`LABEL_FAMILY_DIMENSION` v3, the nested arrow by `PLACE_FAMILY_INSTANCES`. Every one of the ten held both halves,
rolled back, and was SIGNED by Ajmal PS as a `proof.releases` entry for 2024 (D-131): `LINK_FAMILY_PARAMETER` (a
symbolic line, a **masking region in a 3D family** - Revit 2023+ only, never run before - and model text),
`LABEL_FAMILY_DIMENSION` (150 degrees), `CREATE_REFERENCE_PLANES` (45 degrees; a typed 0,0 refused),
`PLACE_DETAIL_COMPONENTS` (project), `ARRAY_FAMILY_FORMS` (the nested arrow in plan), `SET_FAMILY_FORM_VISIBILITY`,
`SET_FAMILY_FORM_SUBCATEGORY`, `LABEL_FAMILY_RADIUS` (symbolic arc 300 mm), `REPORT_FAMILY_FORMS` (2D read-back) and
`SET_FAMILY_TYPE_VALUES` (air flow 94.4 -> 120 L/s through the 2022+ units route).

A Generic Model family holds no filled-region type of its own - a 3D family's region is a masking region unless a
filled-region type is loaded into it. Still owed after this: the arc-centre lock, Group NC's re-run, Group ND's
lock rows; Revit 2025 to 2027 not run.

## The arc-centre lock (3e), 2026-11-12 - built and run on Revit 2024

Row 3e is built: `LOCK_FAMILY_LINES_TO_PLANES` version 3 locks an arc's centre - or a whole circle's - to each plane
named that it lies on, by `CurveElement.CenterPointReference`, switching a hidden Center Mark on first and saying so
(row 5b-1576, NEEDS-CHECKING Group NQ). Widened, not made new, on the owner's word ("make the arc centre lock tool").
Run on the SAVED scratch family `%TEMP%\heron-work\rev2024\R24 Arc Lock.rfa` (Revit 2024, session 13164): rolled
back first, then KEPT by the owner through `try-once` - an arc and the two halves of a circle, 12 locks - and with
"Arc X" moved 200 -> 300 every centre read back at (300, 100), radii unchanged, an unlocked shaft unmoved. The
negative ("Spare") refused. Signed by Ajmal PS: PROVEN on Revit 2024. A closed circle is not run: Heron draws a
circle as two half-arcs. Still owed then: Group NC's re-run, Group ND's lock rows;
Revit 2025 to 2027 not run.

**Group NC, Run 2 (Revit 2024, the same day):** the arrow's eight locks KEPT on the scratch family R24 Line Lock and
followed Length 900 and 300, the head's corners staying joined; NC1 and all three NC3 refusals held. The locks were
read back by `REPORT_FAMILY_FORMS` "2d", which also covers Group ND's lock rows on 2024. The "control" line
followed too, through Revit's Automatic Sketch Dimensions - no control at all (row 5b-1579). Still owed: NC6 by
hand, Revit 2020 for the arc centre, Revit 2025 to 2027.

**Later the same day:** the arc-centre lock was proved on Revit 2020 too (a `proof.releases` entry signed by
Ajmal PS, Group NQ NQ7-NQ8), and a closed circle drawn by hand locked by its centre and followed on 2024 (NQ5). What
is left of this job: NC6 by hand, and Revit 2026 and 2027 for the eleven cards (Revit 2025 is not installed on the
owner's PC).
