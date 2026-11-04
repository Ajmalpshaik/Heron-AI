# Needs checking — Group MO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MO - Modify > Offset, Insert > Insert from File, Analyze > Systems Analysis and Manage > Coordinates > Publish Coordinates - offset a chain of lines as a copy or a move with its corners joined, insert drafting views, legends or 2D elements from a file that is not open, start Revit's Systems Analysis, and publish this model's coordinates into a link: `OFFSET_LINE_CHAIN`, `INSERT_VIEWS_FROM_FILE`, `RUN_SYSTEMS_ANALYSIS`, `PUBLISH_COORDINATES` (2026-11-03)

Asked for as the last four gaps on Revit's ribbon: *"Offset (Modify > Offset) for a chain of lines. Offset selected
model/detail lines or a chain by a distance, copy or move"*, *"Insert views from file (Insert > Insert from File >
Insert Views from File / Insert 2D Elements from File)"*, *"Systems Analysis (Analyze > Systems Analysis)"* and
*"Publish Coordinates (Manage > Coordinates > Publish Coordinates)"*. The ribbon coverage note
([2026-11-01](../work-notes/investigations/2026-11-01-revit-ribbon-coverage.md)) had Offset and Insert from File
PARTIAL and the other two MISSING. No card in the library or on any branch called `CurveLoop.CreateViaOffset`,
`ViewSystemsAnalysisReport` or `Document.PublishCoordinates`, and none opened a project file to copy views from it.
The duplicate check (`heron_duplicates.look`) and the Matcher found nothing equal for any of the four.

**Searched and not widened, each with its reason.** `OFFSET_ELEMENTS` (PROVEN) moves each linear run on its own,
with no copy, and two pieces of a chain offset alone overlap or part at their corner; its impl/ is not touched.
`COPY_ELEMENTS` and `MOVE_ELEMENTS` (PROVEN) take one vector for everything. `TRANSFER_VIEWS_BETWEEN_DOCUMENTS`
(PROVEN) copies views from a project **already open** in the same Revit and declines to guess at a file on disk;
the new card takes the file path the modeller names - a need that card does not have - and follows its two-copy
method without calling or editing it. `COPY_ELEMENTS_BETWEEN_DOCUMENTS` (DRAFT) copies model elements from an open
document or a link. `RUN_HEATING_COOLING_LOADS` (DRAFT, this branch, Revit 2027) is Revit's **other** analysis,
Analyze > Heating and Cooling Loads, with its own report class. `ACQUIRE_COORDINATES` (PROVEN, ADMIN) is the same
dialog the other way round and never touches the linked file; `CHECK_LINK_COORDINATES` (PROVEN) is the read to run
first. **Nothing on the list was left unbuilt for want of an API**: each of the four calls exists on every release
2020 to 2027, and nothing read says any of them refuses an open transaction - whether each runs inside one is
for MO2-MO7 to show.

| What | Where | What it does |
|---|---|---|
| `OFFSET_LINE_CHAIN` (MODIFY) | [`brain/fragments/offset-line-chain`](../../brain/fragments/offset-line-chain/fragment.yaml) | Revit's **Modify > Offset** for model lines, detail lines and walls. The pieces handed in are sorted into **chains** by their meeting ends; each chain is offset as one `CurveLoop` with `CurveLoop.CreateViaOffset`, so every piece is trimmed or extended to meet its neighbour. **Copy** by default, as Revit's Copy box is; `mode` move moves the originals. A copy keeps its line style or wall type. `towards`, a point in mm on the side wanted, turns the sign round; left out, a positive distance goes to the right of each chain as drawn and the answer says which way. Anything not a line or wall is left and named; every piece read back after one regeneration |
| `INSERT_VIEWS_FROM_FILE` (MODIFY) | [`brain/fragments/insert-views-from-file`](../../brain/fragments/insert-views-from-file/fragment.yaml) | Revit's **Insert > Insert from File** for a .rvt that is **not open**: opened detached with its worksets open, read, and **closed without saving**. Each drafting view or legend named is copied with everything drawn in it (the view, then what it owns); with `into`, the 2D elements of the one view named are copied into a view here at the same coordinates. A name the file lacks, or a view of another kind, is named and skipped; a view Revit refuses is named in its words. Every view counted back |
| `RUN_SYSTEMS_ANALYSIS` (MODIFY) | [`brain/fragments/run-systems-analysis`](../../brain/fragments/run-systems-analysis/fragment.yaml) | Revit's **Analyze > Systems Analysis**: a report view - named as asked, or the first free *Systems Analysis Report* - and the request, with the modeller's own `.epw` or `.osw` if one is named and found on the PC. The analysis runs **in the background**: the answer says whether Revit had finished, what the view held, and which weather file, workflow and output folder it used. The output folder is never set (Revit's TEMP default). Revit's refusals are passed on in its words and nothing is kept |
| `PUBLISH_COORDINATES` (ADMIN) | [`brain/fragments/publish-coordinates`](../../brain/fragments/publish-coordinates/fragment.yaml) | Revit's **Manage > Coordinates > Publish Coordinates** for one Revit link, into a named position in it - left out, its current one. Runs **only with `confirm` repeating the link's name exactly** (Article 7); anything else publishes nothing and says what would change. The linked position read before and after, beside this model's shared coordinates at the link's origin. **The linked file is not saved here** (Article 10): Revit asks at the next save of this model, and that answer is the modeller's |

**What is done and what is not.** All four compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-03, a Linux container). Every Revit member was read off the shipped
assemblies of all eight with `tools/api-surface --members`; the members are named in each card's
`compatibility-note`. **Nothing here has run in Revit.** All four are DRAFT. **Only Revit can settle**, and MO2-MO7
record: what Revit does at the joins of a chain set piece by piece, and which side a chain goes by the sign
([row 5b-1458](../fragment-issues/section-5b-rows-401-425.md)); whether a **project** file opens inside the host's
transaction as a family did for `UPGRADE_FAMILY_FILES`, what Revit does with a type name already here with no
handler, and whether a legend's components arrive ([row 5b-1459](../fragment-issues/section-5b-rows-401-425.md));
whether the systems analysis request runs inside a transaction and what becomes of the background run when the
view is rolled back ([row 5b-1460](../fragment-issues/section-5b-rows-401-425.md)); and whether the published
position reads back before the link is saved, and what Revit's save question does when the save is Heron's
([row 5b-1461](../fragment-issues/section-5b-rows-401-425.md)). **Not done:** schedules and sheets from a file;
reading a finished systems analysis report; saving the published position into the linked file - the modeller's,
in Revit's own dialog.

**Routing.** Measured in [row 5b-1462](../fragment-issues/section-5b-rows-401-425.md).

**The models - scratch, any one release 2021 to 2027** (2020 with its 2020.1 update). Never a working model. Every
run below is rolled back except MO1's arrangement and the one KEPT run in a copy that MO5 and MO7 name. The job file
is [`tools/jobs/offset-insert-views-systems-analysis-mo-2026-11-03.yaml`](../../tools/jobs/offset-insert-views-systems-analysis-mo-2026-11-03.yaml)
(`--dry-run` reads it back and sends nothing); `PUBLISH_COORDINATES` is ADMIN and is not in it. Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MO0** | *Open.* Any release 2021 to 2027. New projects saved under `%TEMP%\heron-work`: "Heron MO Offset", "Heron MO Source", "Heron MO Insert" and "Heron MO Link" from the metric architectural template; "Heron MO Systems" and "Heron MO Host" from the metric mechanical template; Changes on at the ribbon, Admin on for MO6 only | The title bar names the scratch project |
| **MO1** | *Arrange, and KEEP it.* Each project as its card's `tests/cases.yaml` says: in "Heron MO Offset" the detail-line chain C1, the detail line D1, the model-line rectangle M1, the walls W1 and the text note T1 in "Level 1"; in "Heron MO Source" the drafting view "MO Detail" and the legend "MO Legend", then **close it**; in "Heron MO Insert" the empty drafting view "MO Target"; in "Heron MO Systems" one room with a space and an HVAC zone, Manage > Location set to a city, an energy model, and a plan duplicated as "MO Report"; "Heron MO Link" saved and closed, then linked into "Heron MO Host" origin to origin and moved 10000 mm east. Save each | Each arrangement as listed |
| **MO2** | *OFFSET_LINE_CHAIN - D-30 pair, NEGATIVE FIRST.* The job file's first job; then by hand, rolled back: C1 alone with `distance=500; towards=1500,1000`, M1 with `distance=300; mode=move`, W1 with `distance=200` | Negative (T1): offsetCount 0, T1 in notOffset. Positive: offsetCount 8 in 3 chains. C1 inside: copies at (0,500) -> (2500,500) -> (2500,1500) -> (0,1500), **meeting at both corners**. M1 still closed. **RECORD** any piece the read-back names, which way the sign took M1, and whether W1's copies are joined (row 5b-1458) |
| **MO3** | *INSERT_VIEWS_FROM_FILE - D-30 pair, NEGATIVE FIRST.* The job file's second job, its path written out; then by hand, rolled back, `viewNames=MO Detail; into=MO Target`, and once more with "Heron MO Source" opened first | Negative ("Level 1"): newViews empty, notFound names it a FloorPlan. Positive: two views, *4 of 4* and *2 of 2*, "closed without saving"; View > Switch Windows does not list Heron MO Source and its file time is unchanged. Into: four elements in MO Target. Opened first: "already open ... left open", still open after. **RECORD** whether the legend component arrived and any Revit refusal (row 5b-1459) |
| **MO4** | *RUN_SYSTEMS_ANALYSIS - D-30 pair, NEGATIVE FIRST.* The job file's third job | Negative ("MO Report"): reportView null, nothing made. Positive: a view "MO Systems Report", requested true, the workflow and output folder named. **RECORD** analysisCompleted and reportContent, and whether files appear in the output folder **after** the rollback (row 5b-1460) |
| **MO5** | *The analysis, KEPT once in a copy.* Save "Heron MO Systems" as "Heron MO Systems Kept"; run with `reportName=MO Systems 1`, `--apply`, Keep | The view "MO Systems 1". **RECORD** how long Revit took to finish, what the finished report shows, and that Analyze > Systems Analysis by hand in the same copy gives a report of the same kind (the second route) |
| **MO6** | *PUBLISH_COORDINATES - by name, NEGATIVE FIRST.* Admin and Changes on. `validate publish-coordinates --in "Heron MO Host" --allow-publish --write --set "linkInstance=<the instance's name>" --set "confirm=Heron MO Link.rvt" --negative-set "linkInstance=<the same>" --negative-set "confirm=Heron MO Other.rvt"` | Negative: published false, *"... It is done only with confirm repeating the link's name exactly ... NOTHING WAS PUBLISHED"*, before and expected filled. Positive: published true, positionName as Revit names it, waitingToSave true, *"THE LINKED FILE IS NOT SAVED YET"*. **RECORD** whether `after` matches `expected`, and any run guard line naming the link (row 5b-1461). Heron MO Link.rvt's file time unchanged |
| **MO7** | *The save question.* In a saved copy of "Heron MO Host" and of its link: the positive with `--apply`, Keep; then Ctrl+S by hand | **RECORD** Revit's Location Position Changed question and its choices; choose Save, then Manage > Coordinates > Report Shared Coordinates on the link and CHECK_LINK_COORDINATES - the same values as `expected` (the second route). Then once more with SAVE_DOCUMENT instead of Ctrl+S, in a fresh copy: **RECORD** what Revit does with the waiting position (row 5b-1461) |
| **MO8** | *The second routes.* Modify > Offset by hand on C1 (Numerical 500, Copy, Tab for the chain, cursor inside); Insert > Insert from File > Insert Views from File by hand on MO Detail | Revit's own copies at the same places as MO2's; the same view and four elements as MO3's. One Ctrl+Z takes each Heron run back as one entry |