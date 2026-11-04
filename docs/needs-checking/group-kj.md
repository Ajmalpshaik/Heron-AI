# Needs checking — Group KJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KJ - a Material Takeoff schedule view of one category: `CREATE_MATERIAL_TAKEOFF` (2026-11-01)

Asked for in the modeller's words: *"make a wall material takeoff with material name, area and
volume"*, *"create a material takeoff schedule for floors"*, *"material takeoff of all the concrete"* -
Revit's View > Schedules > Material Takeoff. Nothing in Heron made one. `brain/heron_retrieve.py` on the
three sentences, Revit 2024, offered `READ_ELEMENT_MATERIAL`, `CREATE_PARTS` and `READ_ELEMENT_MATERIAL`
first; `CREATE_SCHEDULE` makes a schedule with no material rows, and `REPORT_MATERIAL_TAKEOFF` puts the
same numbers in the reply and makes nothing.

| What | Where | What it does |
|---|---|---|
| `CREATE_MATERIAL_TAKEOFF` (MODIFY) | [`brain/fragments/create-material-takeoff`](../../brain/fragments/create-material-takeoff/fragment.yaml) | Makes one Material Takeoff schedule view of the category the modeller **names**, matched by the name Revit shows against the categories Revit allows a takeoff of. **No category named, or one Revit allows none for: nothing is made** and the allowed categories are listed. *Multi-Category* only when named. Fields by the names in the Fields tab; a name with no field is reported with the takeoff's material fields, never guessed at; a name that found the element's own field (*Area*) while a material one exists is added as asked and said. **Not one field added: the takeoff is deleted again in the same undo** and nothing is kept. Named as asked or Revit's default; a name a schedule already has is refused before anything is made. Read back as a material takeoff of that category |

**Why a new card and not a widening.** `CREATE_SCHEDULE` is PROVEN, so any change under its `impl/`
would make its proof stale; it was not edited. And the creation call, the allowed categories and the
fields all differ: `CreateMaterialTakeoff`, not `CreateSchedule`, and the `Material:` fields exist on no
ordinary schedule. *"Of all the concrete"* is a filter on the takeoff - `SET_SCHEDULE_FILTERS`, after
this - never a category.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `ViewSchedule.CreateMaterialTakeoff(Document, ElementId)`,
`GetValidCategoriesForMaterialTakeoff`, `IsValidCategoryForMaterialTakeoff`,
`ScheduleDefinition.IsMaterialTakeoff` and `CategoryId`, and `ScheduleFieldType.Material` and
`MaterialQuantity` are present and unchanged 2020-2027. **Nothing here has run in Revit.** The card is
DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KJ2 to KJ4 measure them): the
exact names Revit gives the material fields in this model (*Material: Name*, *Material: Area*,
*Material: Volume* is the English spelling); whether `GetValidCategoriesForMaterialTakeoff` and
`IsValidCategoryForMaterialTakeoff` accept the invalid id that `CreateMaterialTakeoff` reads as
Multi-Category; and whether Revit refuses a second schedule name that differs only in capitals - the
card refuses it either way.

**Routing.** Checked through `heron_retrieve.find` on two scratch stores, before and after the card, over
every declared utterance and routing-table sentence: none changed the card it reaches. What moved here
was declared nowhere - the three sentences above, and *"make a material takeoff schedule"*, *"make a
material schedule"* and *"create a material takeoff"*, each asking to make something. The questions
stayed: *"how much concrete is there"* on `READ_ELEMENT_MATERIAL`, *"give me a material takeoff"* and
*"quantities by material"* on `REPORT_MATERIAL_TAKEOFF`, *"make a schedule"* on `CREATE_SCHEDULE`. Two
moves that are not this card's, recorded on it: *"make a sheet set for each value of TCM_Project Browser
1"*, unreached by its claimant before and after, flipped between two reads on a three-way tie; and
*"material takeoff for these"* ranks this card first by words alone, still answered by
`READ_ELEMENT_MATERIAL` by identity. **Found and not caused here:** *"how much concrete is in the walls"*,
declared nowhere, reaches `PLACE_AREA_REINFORCEMENT` - a write - before this card and after it: [FRAGMENT-ISSUES row 5b-1368](../fragment-issues/section-5b-rows-401-425.md), open.

**CLOSED 2026-11-04 - THE CARD IS RETIRED, AND NOTHING BELOW IS OWED.** On the owner's yes to [FRAGMENT-ISSUES row 5b-1382](../fragment-issues/section-5b-rows-401-425.md), `CREATE_MATERIAL_TAKEOFF` is DEPRECATED, superseded by `CREATE_SCHEDULE`, which is PROVEN and whose version 3 makes the same Material Takeoff (`listKind` *material takeoff*). Its `impl/` was not touched. The takeoff is checked in Revit as [Group KK](group-kk.md)'s KK31, still owed. Every row below is struck as CLOSED, not passed: none was run. What the retired card did and the keeper does not - a Multi-Category takeoff, the allowed categories listed - is on the retired card's `deprecated:` block.

**The model - "test projject" in Revit 2024**, where `CREATE_SCHEDULE` was proved on 2026-09-13. Every
run below is rolled back except KJ5's one run. The job file is
[`tools/jobs/create-material-takeoff-kj-2026-11-01.yaml`](../../tools/jobs/create-material-takeoff-kj-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| ~~**KJ0**~~ | *Open.* "test projject" open and in front in Revit 2024, Changes on at the ribbon | The title bar reads test projject **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
| ~~**KJ1**~~ | *Check the arrangement.* At least one Basic Wall drawn in the host model, not in a link; no schedule called **HERON TEST TAKEOFF** under Schedules/Quantities | A wall selected in the host shows a Basic Wall type in Properties. Draw one if there is none, and save **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
| ~~**KJ2**~~ | *Read the category list - also the "none named" negative.* `create-material-takeoff` with `--write`, `category` left out, `fieldNames` "Material: Name" | Nothing made; refused *No category was named ... it is never chosen for you*; `validCategories` listing what Revit allows. **Record** whether *Multi-Category* is first on it **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
| ~~**KJ3**~~ | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: `category` Grids - nothing made, refused naming Grids, `validCategories` filled. Positive: `created` one id, `scheduleCalled` HERON TEST TAKEOFF, `addedFields` the three, `missingFields` empty. If any field is missing, take the spelling from `fieldsAvailable` into the job file and run again **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
| ~~**KJ4**~~ | *The refusals and the reports.* KJ3's positive with `fieldNames` "Material: Name,Colour Of Moon"; then "Colour Of Moon" alone; then `category` Floors with "Family and Type,Area"; then `category` Multi-Category | First: made, *Colour Of Moon* in `missingFields`, `fieldsAvailable` listing the material fields. Second: nothing kept, refused *Not one of the fields asked for ...*. Third: both added, and a finding naming the material's own area field. Fourth: **record** made or refused **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
| ~~**KJ5**~~ | *Kept, through `try-once`, on the modeller's yes.* KJ3's positive on a saved copy | Keep or Discard. Then the same run again is refused - *A schedule called "HERON TEST TAKEOFF" is already in the project* - and Revit's own View > Schedules > Material Takeoff of Walls with the same fields reads row for row the same: the second route **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
| ~~**KJ6**~~ | *Revit 2020.* KJ3 again | The same answers. Nothing the card calls changed between releases **CLOSED 2026-11-04, not passed** - the card was retired into `CREATE_SCHEDULE` ([row 5b-1382](../fragment-issues/section-5b-rows-401-425.md)); the takeoff is checked as KK31 |
