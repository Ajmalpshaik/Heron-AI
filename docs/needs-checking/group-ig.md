# Needs checking — Group IG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IG - a combined schedule column: separator between parts, edited in place, moved, read open (2026-10-21)

From [row 5b-1292](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21, Revit 2020, real project
4355-BHVD-3D-21S10-BL001A, schedule *'DUCT HEATER SCHEDULE (ROOF FLOOR)'*: Ajmal PS wanted its combined
**Size** column rebuilt from the shared parameters `Duct_Accessory_Width` and `Duct_Accessory_Height`.
Heron could only add a new combined column at the end, and that column read **"20001300x"** - the `x` after
the height, nothing between.

**What changed.** Two cards, both widened in place - no new capability, no new id:

| Card | Was | Now |
|---|---|---|
| [`ADD_SCHEDULE_COMBINED_FIELD`](../../brain/fragments/add-schedule-combined-field/fragment.yaml) (FRG-VIEW-085) | PROVEN, version 1 | **DRAFT, version 2.** `partSeparators` goes BETWEEN parts - entry 1 between parts 1 and 2 - and nothing is ever written after the last; more than N - 1 is refused. `editExisting` (optional, absent false) edits the column `columnName` names in place - same heading, same place - and refuses in words a plain column or a heading two columns share. `position` (optional, absent keep): first, last, after/before a column, or a number. With `editExisting` and no parts or separators, it moves the column. Reads back into `columnNow`; refusals in words in `refusedWhy` |
| [`REPORT_SCHEDULE_DEFINITION`](../../brain/fragments/report-schedule-definition/fragment.yaml) (FRG-VIEW-056) | DRAFT, version 2 ([Group IH](group-ih.md), filter rules - merged first, unchanged here) | **DRAFT, version 3.** Still read only. Each combined column in `combinedFields`: heading, column number, how Revit draws it - `'[Duct_Accessory_Width]x[Duct_Accessory_Height]'` - and each part's parameter, prefix, suffix and separator after it |

**What is done and what is not.** Both compile for Revit 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-10-21), and every member they add was read off the shipped assemblies for all eight releases. Their code
was run in a cloud container against stand-in types with the same member names - each separator drawn after its
part, as Revit was measured to: 19 cases passed, 7 went red on a copy writing each separator to the next part.
That proves the logic, not Revit's answer. **Nothing here has run in Revit.** Both cards are DRAFT until IG2/IG3 and IG7/IG8 pass and the owner signs; each
version 1 proof is kept on its card as a record and is stale. Until then `revit_change` refuses a kept
change from the combined-column tool (D-117); its one `try-once` and every rolled-back run still go.

**A NEW SCRATCH PROJECT, never a working model and never any 4355-\* model.** Revit 2020 first, the default
metric template, saved as *Heron Combined Column.rvt*. Four walls, Mark W1 to W4 and Comments North, East,
South, West. A Wall Schedule with the fields Type, Mark, Comments, Length; then **by hand**, in its Fields
tab, one combined column headed **Tag** made of Mark and Comments with `/` between them. Place it on a sheet
named *Heron Test Sheet*. A plain Door Schedule - no combined column - on a second sheet *Heron Test Sheet 2*.
Note the Wall Schedule's columns in order, and Tag's Edit Parameters dialog, before IG1.

The job file is [`tools/jobs/combined-column-ig-2026-10-21.yaml`](../../tools/jobs/combined-column-ig-2026-10-21.yaml)
(`--dry-run` reads it back and sends nothing). **Every run below is `validate --write` - rolled back, the model
left as it was - except IG12.**

| # | Run | Look for |
|---|---|---|
| **IG1** | *A new column, placed.* The Wall Schedule clicked on its sheet. `validate add-schedule-combined-field --write`, rolled back: columnName=HERON IG1, partFields=Mark,Comments, partSeparators=x, position=after Type (the `validate` line `--dry-run` prints for the job, with these values) | `added` 1; `columnNow` *"... / 'HERON IG1', column 2 of 6: '[Mark]x[Comments]' - added"*; nothing after Comments. **FAIL:** `x` at the end, or the column last |
| **IG2** | *Edit in place - the D-30 positive.* The job file's `add-schedule-combined-field` job: columnName=Tag, editExisting=true, partFields=Comments,Mark, partSeparators=x | `edited` 1, `added` 0; `columnNow` *"... / 'Tag', column 5 of 5: '[Comments]x[Mark]' - was '[Mark]/[Comments]', same place"* (the *was* exactly as Tag's dialog showed). Rows read NorthxW1 and so on. Tag still the 5th column, still headed Tag, same width |
| **IG3** | *A plain column - the D-30 negative.* The same job's negative: columnName=Length | `edited` 0; `refusedWhy` *"'Length' is a plain column of one parameter, not a combined column ... Nothing was changed on this schedule."* Length's values the walls' lengths, as before |
| **IG4** | *Two columns, one heading.* In Revit, head Comments **Mark** too (Formatting tab). Then columnName=Mark, editExisting=true, partFields=Type | `refusedWhy` names columns 2 and 3 and says nothing was changed. Put the heading back after |
| **IG5** | *Version 1's shape.* No editExisting, columnName=HERON IG5, partFields=Mark,Comments, partSeparators=-,x | `added` 0; `refusedWhy` *"2 part(s) take 1 separator(s) ... and 2 were given"*. **FAIL:** a column drawn `[Mark]-[Comments]x` |
| **IG6** | *A move.* columnName=Tag, editExisting=true, position=first, partFields and partSeparators left out | `edited` 1; `columnNow` ends *"column 1 of 5 ... moved"*; Tag's parts and `/` unchanged. Then position=after Mark on **Length**, a plain column: Length moves, still plain |
| **IG7** | *The read - its positive.* `validate report-schedule-definition` (the job file's second job), the Wall Schedule | `combinedFields` one line for Tag: *"column 5: draws '[Mark]/[Comments]' - 1. Mark - prefix none, suffix none, separator after it '/'; 2. Comments - ... separator after it none"*, matching Tag's dialog field for field; first finding *"... 1 combined ..."* |
| **IG8** | *The read - its negative.* The same, on the plain Door Schedule (*Heron Test Sheet 2*) | `combinedFields` empty, first finding *"0 combined"*, every Door Schedule column still listed |
| **IG9** | *A version 1 caller.* `python tools/batch-prove.py --only add-schedule-combined-field tools/jobs/reprove-refusals.yaml` - it sends neither `editExisting` nor `position` | Runs, never refused for a missing value; the binding note reads `editExisting not given - false (optional)` and `position not given - "keep", the card's own default (optional)`; the column added LAST, `-` between Mark and Comments (its spaces trimmed - row 5b-215) |
| **IG10** | IG2, IG3 and IG7 once on Revit 2024 and once on 2027 | The same answers |
| **IG11** | *Routing, on the owner's PC.* `heron_lookup` *"edit the size column to use width x height"*, *"change what the combined column is made of"*, *"move this column after Equipment Tag"* | `ADD_SCHEDULE_COMBINED_FIELD` first for all three. Then the questions *"what is the size column made of"* and *"why does the size column show the x at the end"*: **never** the combined-column tool - a question answered by a write. Measured in the cloud on lexical and the trained backend without the re-ranker (row 5b-1292): the three reach it on both; on the trained one the two questions reached it too. If they still do on the PC, the owner gives a question in their own words for `REPORT_SCHEDULE_DEFINITION` |
| **IG12** | *The owner's job - only after signing, on the working model, with Keep or Discard.* 'DUCT HEATER SCHEDULE (ROOF FLOOR)': columnName=Size, editExisting=true, partFields=Duct_Accessory_Width,Duct_Accessory_Height, partSeparators=x. Then `REPORT_SCHEDULE_DEFINITION` on it | Size reads 2000x1300 in the same place; the report draws `'[Duct_Accessory_Width]x[Duct_Accessory_Height]'`. The column version 1 added at the end - remove it with `REMOVE_SCHEDULE_FIELDS`, the modeller's call |

**Then sign** `add-schedule-combined-field` on IG2/IG3 and `report-schedule-definition` on IG7/IG8. **One signature covers the report card's code as it stands**, version 2's filter rules and version 3's combined columns alike - so read [Group IH](group-ih.md)'s report rows on the same run before it is signed. Ajmal PS
says whether each goes back to PROVEN.
