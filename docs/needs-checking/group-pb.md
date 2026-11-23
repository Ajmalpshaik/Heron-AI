# Needs checking — Group PB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PB - four family-editor tools repaired from issue #1111: divided-surface counts under Revit's own names, a loop that crosses itself, and lists no longer printed twice - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1163](https://github.com/Ajmalpshaik/Heron-AI/issues/1163) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-21 the coordinating session gave one cloud session the family-editor issues
[#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944), [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945),
[#1111](https://github.com/Ajmalpshaik/Heron-AI/issues/1111) and [#1029](https://github.com/Ajmalpshaik/Heron-AI/issues/1029).
What needed no Revit was #1111's three rows; the rest of #944 and #945, and #1029's fix, already wait on
[Group OT](group-ot.md). With the owner's standing word that a real defect in a PROVEN tool is fixed even
though the tool goes back to DRAFT, each tool below was changed in its C#, its version raised, and set to
DRAFT; each keeps its old proof block as the record of what it proved ([D-30](../DECISIONS.md)).

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`report-divided-surfaces`](../../brain/fragments/report-divided-surfaces/fragment.yaml) (`REPORT_DIVIDED_SURFACES`) | PROVEN v2 | DRAFT v3 | [5b-1908](../fragment-issues/row-5b-1908.md), [5b-1911](../fragment-issues/row-5b-1911.md) | PB1, PB2 |
| [`set-divided-surface-layout`](../../brain/fragments/set-divided-surface-layout/fragment.yaml) (`SET_DIVIDED_SURFACE_LAYOUT`) | PROVEN v1 | DRAFT v2 | [5b-1908](../fragment-issues/row-5b-1908.md) | PB3, PB4 |
| [`create-family-forms`](../../brain/fragments/create-family-forms/fragment.yaml) (`CREATE_FAMILY_FORMS`) | DRAFT v4 | DRAFT v5 | [5b-1921](../fragment-issues/row-5b-1921.md) | PB5, PB6 (and OT1, OT2) |
| [`report-family-forms`](../../brain/fragments/report-family-forms/fragment.yaml) (`REPORT_FAMILY_FORMS`) | DRAFT v6 | DRAFT v7 | [5b-1911](../fragment-issues/row-5b-1911.md) | PB7, PB8 (and OT3, OT4) |

Every tool compiles on every release it claims, 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud
container, 2026-11-21). **None of it has run in Revit.** Each tool's own `tests/cases.yaml` holds the same cases
written out in full, marked NOT RUN. `CREATE_FAMILY_FORMS`'s new check was run off Revit on its own arithmetic -
the card's own side-against-side functions, copied into a console program in the session's scratch folder: a
figure-of-eight, a five-point star, a bow-tie touching at one corner and an arc written the wrong way round so
that it runs across the side opposite it were refused (an arc that only meets its own neighbours is no crossing,
and passes); a rectangle, an L, a hexagon, a slot of two arcs and a circle passed. That is a check of the arithmetic,
not of Revit.

**The models - Heron builds them, on Revit 2020 first, and none is the owner's own work.** PB1 to PB4 in a
scratch family from `Metric Mass.rft`, saved under `%TEMP%\heron-work\pb`; PB5 to PB8 in a scratch family from
`Metric Generic Model.rft` - Group OT's OT2 and OT3 family serves. Every run is ROLLED BACK unless its row says
KEPT. While a tool is DRAFT, `revit_change` refuses it ([D-117](../decisions/D-117.md)): each is run through the
proving client - a preview, or the try-once with the owner's Keep. `REPORT_DIVIDED_SURFACES` and
`REPORT_FAMILY_FORMS` are READ tools, so `revit_read` runs them.

### The repaired tools

| ID | Do this | Pass looks like |
|---|---|---|
| **PB1** | *`REPORT_DIVIDED_SURFACES` v3 positive, row 5b-1908.* A Mass family, a 3000 x 2000 x 2500 mm box, its top face divided by `DIVIDE_FAMILY_SURFACE` with `uGrid=20 vGrid=4` (row 5b-303's arrangement, OT17's); then this tool, `withNodes` false | The surface's line reads *"U rule FixedNumber 20, Center, V rule FixedNumber 4, Center, Revit counts N U-gridlines and M V-gridlines"* - N and M what the surface's Properties show - and no count in brackets beside a rule; a finding names NEEDS-CHECKING BV21. The line is in `divided` and `findings` and **not a third time** as a numbered list under `report` (row 5b-1911). **RECORD by eye which rule made which lines** - BV21's question, asked again |
| **PB2** | *`REPORT_DIVIDED_SURFACES` v3 negative.* A new scratch project with no mass in it | `divided` empty, `surfaceCount` 0; the finding says 0 surfaces and 0 paths, and no finding names BV21 |
| **PB3** | *`SET_DIVIDED_SURFACE_LAYOUT` v2 positive, row 5b-1908.* PB1's family, `surfaceLayout` *"u number=20; v layout=number; v number=4"* - then again with *"u number=8; v number=6"*, so the counts move | `readBack`'s *"U grid: ..."* and *"V grid: ..."* rows end at each grid's own parts, with no gridline count in them; a row of its own reads *"Revit counts: U-gridlines A before, B now; V-gridlines C before, D now"*, A to D what Revit reports; the finding names BV21. Nothing refused or rolled back over a count |
| **PB4** | *`SET_DIVIDED_SURFACE_LAYOUT` v2 negative.* The same request with `dividedSurface` the box form's id | Refused naming the form as the one the surface is on, *"not the divided surface itself"*; nothing changed; `readBack` empty |
| **PB5** | *`CREATE_FAMILY_FORMS` v5 positive, row 5b-1921.* One call, two extrusion rows on *Ref. Level*: an L outline `0,0; 600,0; 600,200; 200,200; 200,500; 0,500`, and a slot `0,1000; 600,1000; arc 700,1100 600,1200; 0,1200; arc -100,1100 0,1000` | `formCount` 2, both made and read back as version 4 would - neither refused as crossing itself |
| **PB6** | *`CREATE_FAMILY_FORMS` v5 negative.* One extrusion row whose profile is a figure-of-eight with area, `0,0; 400,400; 400,0; 0,200` (sides 1 and 3 cross near 133,133; a symmetric one such as `0,0; 400,400; 400,0; 0,400` has no area and the older area check refuses it first) | Refused before anything is made: the row, *"the profile crosses or touches itself"*, side 1 (from 0,0) and side 3 (from 400,0) named; the family's form count unchanged. Version 4 sent it to Revit - **RECORD Revit's own message for it**, if an earlier run of that version is still to hand, beside this run |
| **PB7** | *`REPORT_FAMILY_FORMS` v7 positive, row 5b-1911.* OT3's family of more than 60 forms; `which` = 20 of its form ids; then OT3's family with 70 detail lines in its *Ref. Level* view, `which="detail line"` | The first call: `formCount` 20, the twenty described, `elements` 20, and **no numbered list under `parts`, `ids` or `words`** - each typed id appears in `formList` alone. The second: `twoDCount` 70, `twoDList` 60 rows then ONE row *"and 10 more 2D element(s) ..."* naming those 10 ids; `elements` 70 |
| **PB8** | *`REPORT_FAMILY_FORMS` v7 negative.* The same family, `which="123456"` - an element number, not a long id | Refused, *"Nothing was read. "123456" is not an id in this family - that is an element number ..."*; `elements` 0. The refusal is in `refused` and `findings` and **not a third time** as a numbered list under `problems` |

**Signing, once PB1 to PB8 pass** (and OT1 to OT4 for the two form tools): each tool's proof is written by
`tools/batch-prove.py` with the owner, and `heron-status` set to PROVEN in the same change - never by a session,
and never on a working model.

### Results, 2026-11-23 - Revit 2020, session 35764, scratch family `fp-scratch` (Metric Mechanical Equipment, `%TEMP%\heron-work\fam-prove`)

| Row | Result |
|---|---|
| PB5 | **PASS.** Run once through `try-once` and KEPT by the owner (D-119/D-120), in one call of 74 rows: the L outline read back X 0 to 600, Y 0 to 500, Z 0 to 100 mm, 18 L; the slot X -100 to 700, Y 1000 to 1200, 15.141 L - neither refused as crossing itself. The same call made OT2's ring (one form, 1 outline and 1 hole) and OT3's 70 boxes |
| PB6 | **PASS.** `validate --write --kept`, the figure-of-eight `0,3000; 400,3400; 400,3000; 0,3200`: `formCount` 0, refused *"Row 1: the profile crosses or touches itself - its side 1 (from 0,3000) and side 3 (from 400,3000) meet ..."*; rolled back |
| PB7 | NOT RUN - the proof's positive was OT3 (`which=solid`, below); PB7's 20-id call and its 70 detail lines are still owed |
| PB8 | **PASS.** `which=123456`: `formCount` 0, `elements` 0, refused *"Nothing was read. "123456" is not an id in this family - that is an element number ..."* |

**Signed on the owner's word, 2026-11-23, before PB1-PB4 and PB7 ran** - Ajmal PS chose to prove and sign
`CREATE_FAMILY_FORMS` and `REPORT_FAMILY_FORMS` now so a family build could go ahead; each proof is D-30's two legs
above. PB1-PB4 are other tools and still owed; PB7 is still owed against `REPORT_FAMILY_FORMS` v7.
