# Needs checking — Group OT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OT - thirteen family-editor tools repaired from issues #944, #945, #1029 and #1051, and five measurements the rows still owe - DRAFT until proved (2026-11-20)

**Tracked as GitHub issue [#1110](https://github.com/Ajmalpshaik/Heron-AI/issues/1110) since 2026-11-20** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-20 the coordinating session gave one session the family-editor issues that need no Revit -
[#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944), [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945),
[#1029](https://github.com/Ajmalpshaik/Heron-AI/issues/1029), [#1028](https://github.com/Ajmalpshaik/Heron-AI/issues/1028),
[#1051](https://github.com/Ajmalpshaik/Heron-AI/issues/1051) and [#1044](https://github.com/Ajmalpshaik/Heron-AI/issues/1044) -
with the owner's standing word that a real defect in a PROVEN tool is fixed even though the tool goes back to DRAFT.
Each tool below was changed in its C#, its version raised, and set to DRAFT; each keeps its old proof block as the
record of what it proved ([D-30](../DECISIONS.md)).

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`create-family-forms`](../../brain/fragments/create-family-forms/fragment.yaml) (`CREATE_FAMILY_FORMS`) | PROVEN v3 | DRAFT v4 | [5b-296](../fragment-issues/section-5b-rows-276-300.md), [5b-1888](../fragment-issues/row-5b-1888.md) | OT1, OT2 |
| [`report-family-forms`](../../brain/fragments/report-family-forms/fragment.yaml) (`REPORT_FAMILY_FORMS`) | PROVEN v5 | DRAFT v6 | [5b-1886](../fragment-issues/row-5b-1886.md) | OT3, OT4 |
| [`array-family-forms`](../../brain/fragments/array-family-forms/fragment.yaml) (`ARRAY_FAMILY_FORMS`) | PROVEN v4 | DRAFT v5 | [5b-1261](../fragment-issues/section-5b-rows-1251-1275.md), [5b-358](../fragment-issues/section-5b-rows-351-375.md) | OT5, OT6 |
| [`combine-family-forms`](../../brain/fragments/combine-family-forms/fragment.yaml) (`COMBINE_FAMILY_FORMS`) | PROVEN v2 | DRAFT v3 | [5b-358](../fragment-issues/section-5b-rows-351-375.md) | OT7, OT8 |
| [`flex-family`](../../brain/fragments/flex-family/fragment.yaml) (`FLEX_FAMILY`) | PROVEN v2 | DRAFT v3 | [5b-367](../fragment-issues/section-5b-rows-351-375.md) | OT9, OT10 |
| [`place-family-instances`](../../brain/fragments/place-family-instances/fragment.yaml) (`PLACE_FAMILY_INSTANCES`) | PROVEN v6 | DRAFT v7 | [5b-301](../fragment-issues/section-5b-rows-301-325.md) | OT11, OT12 |
| [`link-family-parameter`](../../brain/fragments/link-family-parameter/fragment.yaml) (`LINK_FAMILY_PARAMETER`) | PROVEN v4 | DRAFT v5 | [177](../fragment-issues/section-5-rows-176-200.md) | OT13, OT14 |
| [`add-family-connector`](../../brain/fragments/add-family-connector/fragment.yaml) (`ADD_FAMILY_CONNECTOR`) | PROVEN v5 | DRAFT v6 | [5b-1920](../fragment-issues/row-5b-1920.md) | OT15, OT16 |
| [`divide-family-surface`](../../brain/fragments/divide-family-surface/fragment.yaml) (`DIVIDE_FAMILY_SURFACE`) | PROVEN v1 | DRAFT v2 | [5b-303](../fragment-issues/section-5b-rows-301-325.md) | OT17, OT18 |
| [`report-family-template`](../../brain/fragments/report-family-template/fragment.yaml) (`REPORT_FAMILY_TEMPLATE`) | PROVEN v4 | DRAFT v5 | [5b-304](../fragment-issues/section-5b-rows-301-325.md) | OT19, OT20 |
| [`report-family-type-catalog`](../../brain/fragments/report-family-type-catalog/fragment.yaml) (`REPORT_FAMILY_TYPE_CATALOG`) | PROVEN v1 | DRAFT v2 | [5b-304](../fragment-issues/section-5b-rows-301-325.md) | OT21, OT22 |
| [`set-family-settings`](../../brain/fragments/set-family-settings/fragment.yaml) (`SET_FAMILY_SETTINGS`) | PROVEN v4 | DRAFT v5 | [5b-285](../fragment-issues/section-5b-rows-276-300.md), [5b-437](../fragment-issues/section-5b-rows-426-450.md), [5b-304](../fragment-issues/section-5b-rows-301-325.md) | OT23, OT24 |
| [`filter-elements-in-room`](../../brain/fragments/filter-elements-in-room/fragment.yaml) (`FILTER_ELEMENTS_IN_ROOM`) | PROVEN v1 | DRAFT v2 | [5b-1485](../fragment-issues/section-5b-rows-1476-1500.md) | OT25, OT26 |

Every tool compiles on every release it claims, 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud
container, 2026-11-20). **None of it has run in Revit.** Each tool's own `tests/cases.yaml` holds the same cases
written out in full, marked NOT RUN.

**The models - Heron builds them, on Revit 2020 first, and none is the owner's own work.** Scratch families
from Revit 2020's metric templates, saved under `%TEMP%\heron-work\ot`; OT17 and OT18 in a family from
`Metric Mass.rft`; OT25 and OT26 in a scratch project from `DefaultMetric.rte`. Every run is ROLLED BACK
unless its row says KEPT. While a tool is DRAFT, `revit_change` refuses it ([D-117](../decisions/D-117.md)):
each is run through the proving client - a preview, or the try-once with the owner's Keep.

### The repaired tools

| ID | Do this | Pass looks like |
|---|---|---|
| **OT1** | *`CREATE_FAMILY_FORMS` v4 negative, row 5b-296.* One extrusion row whose profile is four rectangles sharing edges - a ring - and a second call with a strip crossing a spine | Refused before anything is made, naming the row and the two loops that touch, with *"a ring is drawn as an outer loop with an inner loop inside it"*; nothing made. Version 3 sent it and Revit answered only *"code 1"* |
| **OT2** | *`CREATE_FAMILY_FORMS` v4 positive.* The same ring as an outer loop with an inner loop inside it; then one call of about 700 loops in total (row 5b-1888) | The ring made, one form, read back by `REPORT_FAMILY_FORMS`; the 700-loop call refused before Revit is asked, giving the count and *"about 300 loops a call"* - nothing made, and no 50-second wait |
| **OT3** | *`REPORT_FAMILY_FORMS` v6 positive, row 5b-1886.* A family of more than 60 forms (OT2's family with a row of 70 small boxes), `which=solid` | The form count and the overall extents first; each form's line once - never the same lines twice; past 60 forms one line saying how many more and how to get them; every form's id still given to `SET_SELECTION` with `expect_from "report-family-forms"` |
| **OT4** | *`REPORT_FAMILY_FORMS` v6 negative.* A family of 11 forms | All 11 lines, once each, no *"more"* line |
| **OT5** | *`ARRAY_FAMILY_FORMS` v5 positive, row 5b-1261.* Planes *Left* and *Leg End* 300 mm apart, the dimension labelled *Leg Length*; a 20 x 20 extrusion sketched on *Leg End*; `count=3`, `step=Z 80`. Then `FLEX_FAMILY` at Leg Length 500 | Arrayed, with the finding naming *Leg End* and *Leg Length* and saying to flex it; `memberForms` one form id per group in `members`, in the same order. RECORD what the flex finds |
| **OT6** | *`ARRAY_FAMILY_FORMS` v5 negative.* The same extrusion sketched on *Ref. Level*, `step=Y 100` | No plane finding. **RECORD whether the after-the-array finding (work planes read *"<not associated>"*) still fires** - that settles whether every array of forms reads that way, which row 5b-1261 left open |
| **OT7** | *`COMBINE_FAMILY_FORMS` v3 positive, row 5b-358.* A plate and a void circle, the void arrayed `count=3`, `step=X 200`; the plate's id with the first `members` group id | EITHER the plate cut, with the finding that the group's form was taken, OR the plain refusal that Revit will not combine a form inside an array's group - **which one is what this run answers**. Watch for a failure Revit raises at commit (*"group edited outside group edit mode"*) |
| **OT8** | *`COMBINE_FAMILY_FORMS` v3 negative.* The plate with a group made by hand of two model lines | Refused, the group holding no form; nothing combined |
| **OT9** | *`FLEX_FAMILY` v3 positive, row 5b-367.* A spring: a nested one-turn family arrayed by *Coils*, its ends locked to planes; trials `Coils=5 \| Coils=3; Top Height=200` | `restored` true; each trial line *"across N solid(s) and M nested families"*, M following Coils |
| **OT10** | *`FLEX_FAMILY` v3 negative.* A nested family moved to Z 30 and NOT locked to *Pitch*; trial `Pitch Height=60` | NOT HELD, naming the nested family that did not follow. Version 2 passed this case |
| **OT11** | *`PLACE_FAMILY_INSTANCES` v7 positive, row 5b-301.* In a Generic Model family, a loaded Work Plane-Based family whose Offset from Host is not 0 (`Round void : HWC 10 cm` from the Structural Precast library, or a scratch one with a -200 mm offset), `points=0,-1000,0` on *Ref. Level* | Placed; `readBack` *"... sits 200 mm below "Ref. Level": the family's own offset"*. Version 6 threw *"stands at 0, -1000, -200 mm, not 0, -1000, 0 mm"* |
| **OT12** | *`PLACE_FAMILY_INSTANCES` v7 negative.* The same family, `points=0,-1000,300` | Refused, *"300 mm off "Ref. Level""*, nothing placed |
| **OT13** | *`LINK_FAMILY_PARAMETER` v5 positive, row 177.* A reference plane *Port Right* at X 48 facing -X, a thin extrusion sketched on it, Length parameters *Cap Start* 0 and *Cap End* 1; `forms=` its id, `links=Extrusion Start=Cap Start; Extrusion End=Cap End` | Linked; `linkReport` says *"... runs along -X here ... a positive value goes left"* and *"the form now runs X 47 to 48 mm"*, matching Properties |
| **OT14** | *`LINK_FAMILY_PARAMETER` v5 negative.* The same extrusion, `links=Visible=Show Cap` (a Yes/No) | Linked, with no *"runs along"* note |
| **OT15** | *`ADD_FAMILY_CONNECTOR` v6 positive, issue #1029.* A flange disc Z 870 to 882, a raised face on it Z 882 to 884, a box 1 m away whose top is at Z 884; planes *Discharge Face* (Z 884) and *Discharge Flange Top* (Z 882). `facePlane=Discharge Face`, a pipe system, a diameter parameter, `form=` the raised face's id; then `facePlane=Discharge Flange Top`, `facing=up` | A connector on the raised face, with *"1 other face(s) ... left out by form="*; the second on the disc's top. Version 5 refused both |
| **OT16** | *`ADD_FAMILY_CONNECTOR` v6 negative.* `facePlane=Discharge Face`, `facing=up` alone - both faces point up; then `form=` a reference plane's id | First refused, listing both faces with their form ids, the way each faces and its centre, and saying to give `form=`; second refused by name. Nothing made |
| **OT17** | *`DIVIDE_FAMILY_SURFACE` v2 positive, row 5b-303.* A Mass family, a 3000 x 2000 x 2500 box, top face, `uGrid=20`, `vGrid=4` | `divided` *"U rule: Number 20; V rule: Number 4; Revit counts N U-gridlines and M V-gridlines"* - no count beside a rule. **RECORD by eye which rule made which lines** (NEEDS-CHECKING BV21) |
| **OT18** | *`DIVIDE_FAMILY_SURFACE` v2 negative.* The same mass, front face, `uGrid=3`, `vGrid=1000 mm`; then that face again | First divided, no count beside a rule; the second refused, the face already divided |
| **OT19** | *`REPORT_FAMILY_TEMPLATE` v5 positive, row 5b-304.* A new Detail Item family, never saved | *Family "Family1"* and *"Family1" is a detail item* - never *""* |
| **OT20** | *`REPORT_FAMILY_TEMPLATE` v5 negative.* The same family saved as `Heron Name Test.rfa` and opened again | *"Heron Name Test"* - never *""* and never *.rfa* |
| **OT21** | *`REPORT_FAMILY_TYPE_CATALOG` v2 positive, row 5b-304.* An unsaved Generic Model family with a Width type parameter | `fileName` *Family1.txt*, never *.txt* |
| **OT22** | *`REPORT_FAMILY_TYPE_CATALOG` v2 negative.* The same family saved | The saved path with *.txt* - never *Family1.txt* or *.rfa.txt* |
| **OT23** | *`SET_FAMILY_SETTINGS` v5 positive, row 5b-285.* A Generic Model family after *"Work Plane-Based=yes; Always Vertical=no"* was kept; then *"Work Plane-Based=no; Always Vertical=yes"*. Then, in a never-saved family, *"Shared=yes"* | Changed plus already-as-asked is 2, never 3; **RECORD whether Revit turned Always Vertical on itself** (NEEDS-CHECKING BS2) and whether that row reads *"(Revit's own change)"*. The never-saved family's closing finding reads *'on the family "Family1"'*, never *""* (row 5b-304's defect) |
| **OT24** | *`SET_FAMILY_SETTINGS` v5 negative.* OT23's first call again | `changed 0`, `alreadySet 2`, no *"(Revit's own change)"* row |
| **OT25** | *`FILTER_ELEMENTS_IN_ROOM` v2 positive, row 5b-1485.* A scratch project: Room 7 in four walls, two windows in its walls, and a door into Room 8 | All three in `elements`; `doorsAndWindows` gives each its side (*"its To Room"*), matching Properties' From Room and To Room. Version 1 put every window outside |
| **OT26** | *`FILTER_ELEMENTS_IN_ROOM` v2 negative.* Room 7, and a door between Room 8 and Room 9 and a window of Room 8 | Both outside, each line saying neither side is Room 7 |

### What the rows still owe, with no tool changed

| ID | Do this | What it decides |
|---|---|---|
| **OT27** | *Row 180.* In a scratch family, two separate extrusion circles; label both with ONE Length parameter by hand (Revit's own radius dimension, labelled), flex it to two sizes, and read whether it holds; then the same through `LABEL_FAMILY_RADIUS`, which refuses the second label today | Whether `LABEL_FAMILY_RADIUS`'s *"already labels a dimension"* guard protects a real conflict, or can be narrowed so a symmetric family needs no twin L and R parameters |
| **OT28** | *Row 179.* A host family with a nested family instance (a handwheel); set its Visibility/Graphics Overrides to Coarse only, Medium only, Fine only by hand, reading the instance's raw *Visibility/Graphics Overrides* value each time (`READ_ELEMENT_PARAMETERS`); then `WRITE_ELEMENT_PARAMETERS` with that raw value, read back | Whether the nested instance's detail levels can be written through the integer parameter, and its value for each level on Revit 2020 - before any tool writes it |
| **OT29** | *Row 5b-1484.* A scratch project with three instances of a scratch window family; the family file changed only in two new materials and a blank-named extra type; `LOAD_FAMILY reload=true` with `overwriteValues=true`, then `false` - previews, rolled back | Whether Revit asks to delete elements, how many, and which (the dialog's list, by hand) - and whether the box can be read before it is answered, so `LOAD_FAMILY` could name them. **Read 2026-11-21 in a cloud container (no Revit):** the box row 5b-1484 met was very likely Heron's own [D-142](../decisions/D-142.md) box, not Revit's - D-142 asks it of every kept run Revit counts a deletion in, and past its deadline a Delete is not kept, which is what the row saw. Since then the box lists what would go before it is answered - each line a category, or *(no category)* and Revit's own kind, with what it goes with, and the element ids under its details ([row 5b-1700](../fragment-issues/row-5b-1700.md), NY1 PASS on Revit 2024, 2026-11-16) - and it closes itself at its deadline ([row 5b-1748](../fragment-issues/row-5b-1748.md), NY2 PASS). **The box is asked only of a run that would be kept, never of a preview** (`RevitFragmentDelete.cs`), so run OT29's two loads through `revit_change` - `LOAD_FAMILY` is PROVEN - and press *Don't delete*, which keeps nothing, as NY1 did; a load no box comes up for is kept, so Discard it or Undo it. Write down the box's title - Heron's reads *"Heron AI - Delete N elements?"* - and its own list; the chat's reply carries the count (`deleteShown`). Whether `LOAD_FAMILY` itself should name them is then the owner's, with that list in front of him. What this run still settles: what the elements are, and why a reload deletes them |
| **OT30** | *Row 5b-1485.* `REPORT_OPEN_DOCUMENTS` (DRAFT v3): positive - a saved project and a never-saved family open; negative - the project only. `EXPORT_FAMILIES` (DRAFT v1, PUBLISH - the Publish switch on): positive - a scratch project's loaded family to `%TEMP%\heron-work\ot\export`; negative - `nameContains` matching nothing | Each card's proof, with its negative case, so the family method's save check and *"save this family out"* stop being claims. **Corrected 2026-11-21, no Revit:** `EXPORT_FAMILIES` is version 2 since 2026-11-20 ([D-192](../decisions/D-192.md), `noGroupOpen`) - version 1 could not open a family for editing inside Heron's group at all - and version 2's proof is [Group OX](group-ox.md) OX3 and OX4 (issue [#1142](https://github.com/Ajmalpshaik/Heron-AI/issues/1142)). Run those for it, not this row's export half; OT30 is `REPORT_OPEN_DOCUMENTS`'s alone |
| **OT31** | *Row 5b-358, with OT7.* Once OT7 has answered, read `memberForms` against Revit's own selection of each copy | Whether `memberForms` names the form Revit shows inside each group |

**Not in this group, and where it is:** row 5b-1351's model-text box is [Group IQ](group-iq.md) IQ9 and IQ10
(since 2026-11-21); row 5b-437's families with types are [Group DB](group-db.md) DB14; row 5b-1702's Glass
material is [Group NY](group-ny.md) NY5, then NY6 and NY7 (since 2026-11-21), then the owner's choice;
`EXPORT_FAMILIES` version 2's proof is [Group OX](group-ox.md) OX3 and OX4; `SAVE_DOCUMENT` version 3 (issue #1028) is [Group EN](group-en.md) EN6 to EN12;
`CREATE_LINE_STYLE` and `SET_FAMILY_PLANE_REFERENCE` (issue #1051 C1) are [Group OK](group-ok.md);
`ACTIVATE_DOCUMENT` (C3) is [Group OL](group-ol.md).

**Signing, once OT1 to OT26 pass:** each tool's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
