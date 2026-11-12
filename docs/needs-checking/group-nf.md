# Needs checking — Group NF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NF - a family's 2D side through five widened cards: a detail item placed and arrayed in a family view, a symbolic line's detail levels, lines on a subcategory, an arc's radius labelled: `PLACE_DETAIL_COMPONENTS` v2, `ARRAY_FAMILY_FORMS` v4, `SET_FAMILY_FORM_VISIBILITY` v2, `SET_FAMILY_FORM_SUBCATEGORY` v2, `LABEL_FAMILY_RADIUS` v6 (2026-11-11)

Asked 2026-11-11 by Ajmal PS after his KDF square diffuser family (Revit 2020) stopped three times on its plan
flow arrows: the whole class of "2D in a family" was audited, and five PROVEN cards were the right owners of the
missing pieces. He said yes to widening them rather than adding cards ("no duplicate tools"), so each went back to
DRAFT, its version raised and its old proof kept in the card as the record. Why each, and what is not known, is
[FRAGMENT-ISSUES rows 5b-1499 to 5b-1503](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `PLACE_DETAIL_COMPONENTS` v2 (MODIFY) | [`brain/fragments/place-detail-components`](../../brain/fragments/place-detail-components/fragment.yaml) | In a family, a detail item or generic annotation loaded into it is placed IN a named family view (`FamilyCreate`); a project as before (`Create`). Both read back: exists, Owner View, point, type. Leaves `instanceId` / `instanceIds` |
| `ARRAY_FAMILY_FORMS` v4 (MODIFY) | [`brain/fragments/array-family-forms`](../../brain/fragments/array-family-forms/fragment.yaml) | A view-based nested family is arrayed in its Owner View, read by its insertion point; a step out of the view's plane refused. Forms and model families as before |
| `SET_FAMILY_FORM_VISIBILITY` v2 (MODIFY) | [`brain/fragments/set-family-form-visibility`](../../brain/fragments/set-family-form-visibility/fragment.yaml) | Symbolic lines too, the setting built in the kind the line already reads; kinds of view read back and refused if not kept; `views` absent = "unchanged". Detail lines, filled regions, nested families refused by name |
| `SET_FAMILY_FORM_SUBCATEGORY` v2 (MODIFY) | [`brain/fragments/set-family-form-subcategory`](../../brain/fragments/set-family-form-subcategory/fragment.yaml) | Detail and symbolic lines too, by line style (the subcategory's projection style), only a style the line's `GetLineStyleIds` offers; read back |
| `LABEL_FAMILY_RADIUS` v6 (MODIFY) | [`brain/fragments/label-family-radius`](../../brain/fragments/label-family-radius/fragment.yaml) | A detail or symbolic ARC labelled by a radial (or diameter) dimension in its own view; the parameter must already hold its size; label, value and radius read back |

**What is done and what is not.** All five compile on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). **Nothing here has run in Revit.** The job file
[`tools/jobs/family-2d-widen-nf-2026-11-11.yaml`](../../tools/jobs/family-2d-widen-nf-2026-11-11.yaml) is read back
by `tools/batch-prove.py --dry-run` (20 WOULD RUN); its upper-case ids are placeholders to type in first.

**The models - SAVED scratch families, never the owner's own** (not the KDF diffuser, not
`Diffuser Flow Arrow_DRAFT.rfa`). Revit 2020, all under `%TEMP%\heron-work\family-2d-widen`. Every call with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

**The arrangement (NF0), made with Heron's own PROVEN tools and KEPT:**
- *"Heron NF Arrow"* - File, New, Family, metric Detail Item. `ADD_FAMILY_PARAMETERS` a length type parameter "Arc
  Radius" = 150. `DRAW_FAMILY_DETAIL_LINES` in Ref. Level: "path 0,0; 600,0" (SHAFT-ID), then "path 0,150; arc
  150,0 0,-150" (ARC-ID). Save.
- *"Heron NF Generic"* - metric Generic Model. `CREATE_FAMILY_FORMS`: rect -300,-300 300,300 on Ref. Level, 0 to 50
  (FORM-ID), and circle 0,-800 10, 0 to 100 (CYL-ID). Length type parameters "Shaft Radius" = 10 and "Arc Diameter"
  = 300. Version 3's and version 5's negatives: an extrusion on "Center (Front/Back)", rect -1000,0 -990,100, 0 to 10
  (VANE-ID); a revolve on "Center (Front/Back)", profile "1000,300; 1060,300; 1060,500; 1000,500" about "1000,300
  1000,500" - no plane through its axis (REV-ID) - with a length type parameter "Bell Radius" = 60.
  `DRAW_FAMILY_SYMBOLIC_LINES` on Ref. Level, all levels: "path 0,0; 600,0" (SYM-LINE-ID) and "path 0,150;
  arc 150,0 0,-150" (SYM-ARC-ID). `DRAW_FAMILY_DETAIL_LINES` in Ref. Level: "path 0,-400; 600,-400"
  (DETAIL-LINE-ID). Load "Heron NF Arrow" and a plain Generic Model family "Heron NF Box". Save.
- *"Heron NF Project"* - File, New, Project, Revit 2020's default metric template; load "Heron NF Arrow". Save.

Record every id. **ARROW-ID** comes from NF2 run KEPT through `try-once` (the arrow placed once in Ref. Level, at
0,300,0); save after it.

| # | Run | Look for |
|---|---|---|
| **NF0** | *Make and save the three scratch files,* as above | Title bars name them; every id recorded |
| ~~**NF1**~~ | *PLACE_DETAIL_COMPONENTS, OLD path* - in "Heron NF Project", view "FloorPlan: Level 1", the arrow at 1000,2000,0; negative view "{3D}"; rolled back | One arrow on Level 1, read back (Owner View Level 1, the point, the type), `instanceId` filled; the negative refused as a 3D view, nothing placed **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| **NF2** | *PLACE_DETAIL_COMPONENTS, NEW* - in "Heron NF Generic", view "FloorPlan: Ref. Level" (the family also has a ceiling plan of that name), the arrow at 0,300,0; negative the model family "Heron NF Box"; rolled back. Then once more turned 90 at 300,0,0 with the negative view "View 1" | The arrow placed by the family's factory in Ref. Level, read back, `instanceId` its id; the negative refused as not view-based, pointed at PLACE_FAMILY_INSTANCES; the 3D view refused. **If Revit refuses the view overload in a family on 2020, that is the first thing this group learns** - the card is changed, not proved **RUN 2026-11-12, Revit 2020 (session 33000) - the positive did NOT hold:** Revit 2020 reports the nested detail item WorkPlaneBased and refuses it in a family's view (row 5b-1573); the negative was refused as written. The card is PROVEN on its project path, NF1. Run 1 below. |
| ~~**NF3**~~ | *ARRAY_FAMILY_FORMS, OLD path* - FORM-ID, count 3, step "X 700"; negative VANE-ID, count 3, step "Y 20" - version 3's own proof negative; rolled back | Arrayed in the 3D view as version 3 did, every copy read back by its box; the negative refused after the trial row - the copies moved 0 mm, "Center (Front/Back)" named - through the leavesPlane branch that now sits after version 4's ownerView branch **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF4**~~ | *ARRAY_FAMILY_FORMS, NEW* - ARROW-ID, count 4, step "X 200"; negative step "Z 100"; rolled back | Arrayed IN Ref. Level, four in all 200 mm apart, each copy owned by Ref. Level and read by its insertion point; the negative refused before anything is made (out of the view's plane) **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1574's repair - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF5**~~ | *SET_FAMILY_FORM_VISIBILITY, OLD path* - FORM-ID, "medium, fine", "front, left"; negative "rough"; rolled back | As version 1: the form's setting before and after; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF5b**~~ | *OLD, the empty list* - FORM-ID, "all", "all"; negative views "" (version 1's tests case); rolled back | Every box ticked again; the negative refused as naming no kind of view - "" binds as left out, i.e. "unchanged", and a FORM with "unchanged" is refused, so version 1's behaviour holds **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF5c**~~ | *OLD, the proof's negative* - NF5's positive, negative run against "Heron NF Project"; rolled back | `notAFamily` true on the project, nothing changed - version 1's proof negative **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF6**~~ | *SET_FAMILY_FORM_VISIBILITY, NEW* - SYM-LINE-ID, "coarse", views "unchanged"; negative DETAIL-LINE-ID; rolled back | The line at coarse only, its kinds of view as before; the finding names the kind Revit read (expected ViewSpecific - **record what it says**); the negative refused by name, no API **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF6b**~~ | *The unmeasured half, in the job file* - SYM-LINE-ID, "all", views "plan"; negative DETAIL-LINE-ID; rolled back | EITHER the line reads "plan" back, OR the call is refused naming what Revit read (or "not readable"). A refusal on the positive leg is the finding, not a fault of the run. Record which: it settles whether a symbolic line's setting keeps kinds of view, and the card's sentence is fixed from it **ANSWERED 2026-11-12, Revit 2020 (session 33000): refused - a symbolic line keeps no kinds of view through the API. Run 1 below; the card signed PROVEN that day.** |
| ~~**NF6c**~~ | *NEW, views left empty* - SYM-ARC-ID, "medium, fine", views ""; negative FORM-ID with views "unchanged"; rolled back | The arc's detail levels set, its kinds of view untouched; the negative refused - "unchanged" is for symbolic lines only **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF7**~~ | *SET_FAMILY_FORM_SUBCATEGORY, OLD path* - FORM-ID on "Casing"; negative subcategory ""; rolled back | As version 1: "Casing" made, the form on it; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF7b**~~ | *OLD, back and the proof's negative* - FORM-ID, subcategory "none"; negative run against "Heron NF Project"; rolled back | The form on the family's own category (version 1's second positive); `notAFamily` true on the project (its proof negative) **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1571's repair - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF8**~~ | *SET_FAMILY_FORM_SUBCATEGORY, NEW* - SYM-LINE-ID and SYM-ARC-ID on "Flow Arrow"; negative ARROW-ID; rolled back | "Flow Arrow" made, both lines on it, read back by line style; the negative refused as neither a form nor a line, no subcategory made. **If a new subcategory is not on the lines' offered styles until something else happens, the call refuses and rolls back - record it** **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF8b**~~ | *NEW, "none" on a line* - SYM-LINE-ID, "none"; negative subcategory ""; rolled back | The line on the family category's own projection style, read back; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF8c**~~ | *NEW, a detail line* - in "Heron NF Arrow", SHAFT-ID on "Arrow Head"; negative subcategory ""; rolled back | "Arrow Head" made under Detail Items and the shaft on it, read back - or refused by name with nothing kept if Revit does not offer it that style; record which **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF9**~~ | *LABEL_FAMILY_RADIUS, OLD path* - CYL-ID, "Shaft Radius", radius; negative FORM-ID; rolled back | As version 5: a radial dimension in the cylinder's sketch reading 10; the negative refused, no 10 mm circle **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF9b**~~ | *OLD, the proof's negative* - CYL-ID as NF9; negative REV-ID with "Bell Radius"; rolled back | Version 5's proof negative: refused, no reference plane through the revolve's axis, nothing labelled **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF10**~~ | *LABEL_FAMILY_RADIUS, NEW, symbolic* - SYM-ARC-ID, "Arc Diameter", diameter; negative SYM-LINE-ID; rolled back | A diameter dimension on the symbolic arc in the Ref. Level plan reading 300, the arc read back at radius 150; the negative refused as not an arc **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1572's repair - Run 1 below; the card signed PROVEN that day.** |
| ~~**NF11**~~ | *LABEL_FAMILY_RADIUS, NEW, detail* - in "Heron NF Arrow", ARC-ID, "Arc Radius", radius; negative SHAFT-ID; rolled back | A radial dimension on the detail arc in Ref. Level reading 150; the negative refused **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| **NF12** | *FOLLOW, KEPT - BY HAND, not in the job file: `batch-prove` runs only rolled-back legs, and this row needs a KEPT run (D-119, D-120) - the label must stay for the flex to show anything - which is `try-once` behind Revit's Keep or Discard, once per version, the owner's choice* - NF11's positive through `try-once` and KEEP; then `FLEX_FAMILY` / `SET_FAMILY_TYPE_VALUES` "Arc Radius" 200, then 100 | The arc redrawn at radius 200 and 100 (read back by a rolled-back NF11 call, "already labels" refusal naming its radius, or by hand). `FLEX_FAMILY` measures solids only (row 5b-1493) - read the arc by hand if it says nothing **PART 2026-11-12, Revit 2020 (session 33000):** kept by the owner, "Arc Radius" 200 and the arc read back at radius 200 mm; the second size, 100, is not in the run's record. Run 1 below. |
| **NF13** | *Second route.* By hand on copies: Detail Component in Ref. Level; Array in Ref. Level; Visibility Settings on the symbolic line; Subcategory in Properties on each line; a radial dimension on the arc labelled by hand | Each copy reads the same as Heron's run: Owner View, count and spacing, ticked levels, subcategory, label and value |

**Before the owner's own families.** Only once NF is through, and each card only once its own rows are. The first
run on the KDF diffuser is the owner's choice, through `try-once`, behind Revit's Keep or Discard.

## Run 1 - Revit 2020, 2026-11-12 (session 33000, Ajmal PS watching)

**Models:** the SAVED scratch files `%TEMP%\heron-work\family-2d-widen\` Heron NF Generic.rfa (Generic Model),
Heron NF Arrow.rfa (Detail Item), Heron NF Box.rfa and Heron NF Project.rvt - all made by `CREATE_FAMILY_DOCUMENT`
this session, never the owner's own. Rolled back unless it says KEPT. Four defects were found and fixed in the run
(rows 5b-1571 to 5b-1574); every row below was re-run on the final code.

| Row | Positive | Negative | Verdict |
|---|---|---|---|
| NF1 | arrow placed on Level 1 of the project, read back, `instanceId` filled | view template "Architectural Plan" refused (the project has no 3D view; the plan named "{3D}") | held |
| NF2 | **inside a family Revit 2020 reports the arrow WorkPlaneBased and refuses it in a view** - now refused up front, naming the route | Heron NF Box refused, "placed OneLevelBased, category Generic Models" | finding, row 5b-1573; the arrow nested instead with `PLACE_FAMILY_INSTANCES` |
| NF3 | 3 x 700 mm along X, every copy read back | "Y 20" on the vane refused after the trial row (version 3's own negative) | held |
| NF4 | first run "An internal error has occurred" - fixed; then 4 arrows 200 mm apart in Ref. Level | "Z 100" refused, out of the plan | held after row 5b-1574's fix |
| NF5, NF5b, NF5c | the box's levels and kinds set and read back; "all" again | "rough" refused; views "" refused for a form; the project refused | held |
| NF6 | the symbolic line at coarse only, kinds of view kept; "settings are of the ViewSpecific kind" | the nested arrow refused, no API | held |
| NF6b | refused: "Revit would not take kinds of view for the symbolic line ... ViewSpecific" | the box with "unchanged" refused | **the allowed answer**: a symbolic line keeps no kinds of view through the API |
| NF6c | the arc's levels set, kinds untouched | the box with "unchanged" refused | held |
| NF7 | "Casing" made, the box on it | "" refused | held |
| NF7b | first runs threw "The input subcategory is illegal" - fixed; then "Casing -> Generic Models" (Casing KEPT by the owner first) | the project refused | held after row 5b-1571's fix |
| NF8, NF8b | "Flow Arrow" made, both symbolic lines on it; a line back to "none" | the nested arrow refused; "" refused | held |
| NF8c | the arrow family's detail line on a new "Arrow Head" | "" refused | held |
| NF9, NF9b | the cylinder's radius 10 mm in its sketch | the box refused; the revolve with no plane through its axis refused | held |
| NF10 | first run "not in Ref. Level" - fixed; then the symbolic arc's diameter 300 mm | a straight line refused | held after row 5b-1572's fix |
| NF11 | the detail arc's radius 150 mm | the shaft refused | held |
| NF12 | the detail arc's label KEPT by the owner; "Arc Radius" 200 - the arc read back at radius 200 mm | - | **held: a 2D arc follows its parameter** |
| NF13 | - | - | NOT RUN |

**Proof drafts:** the five cards' drafts are from the last runs on the final code (NF1, NF4, NF7b, NF10, NF6) and
await the owner's signature.

**SIGNED 2026-11-12 by Ajmal PS - all five cards are PROVEN** on those drafts (the proof block in each card, Revit
2020, session 33000): `PLACE_DETAIL_COMPONENTS` version 2 on NF1, `ARRAY_FAMILY_FORMS` version 4 on NF4,
`SET_FAMILY_FORM_SUBCATEGORY` version 2 on NF7b, `LABEL_FAMILY_RADIUS` version 6 on NF10 and
`SET_FAMILY_FORM_VISIBILITY` version 2 on NF6. **Revit 2024, the same day:** each carries a `proof.releases`
entry signed by Ajmal PS - session 13164, the scratch files "R24 Generic" and "R24 Project", both halves rolled
back (the 2024 section of the [work note](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)).
**`PLACE_DETAIL_COMPONENTS` is proved on its PROJECT path on both releases; a detail item placed in a family's own
view, NF2, has no run that held** - [FRAGMENT-ISSUES row 5b-1499](../fragment-issues/section-5b-rows-401-425.md)
stays open for it. Still open: NF2; NF12's second size; NF13 (second route). "Each went back to DRAFT" and
"Nothing here has run in Revit", above, were true on 2026-11-11.
*Written back 2026-11-12 from the cards, in a cloud container (no Revit); nothing was run here.*
