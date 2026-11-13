# Needs checking — Group JT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JT - the whole model counted part by part for the Companion's Model Dashboard: `REPORT_MODEL_DASHBOARD` (2026-11-01)

Asked for by the owner on 2026-11-01: a Model Dashboard panel in the Heron Companion
([docs/40](../40-heron-companion.md)), fed by one read of the open model. Nothing in Heron returned the
whole model at once - `heron_retrieve` for *"model dashboard"*, *"model overview"* and *"model health
summary"* offered single-part readers, and the Matcher found nothing for the capability. Each part has a
reader of its own, and this fragment reads every part **with that reader's rule**, so the two agree.

| What | Where | What it does |
|---|---|---|
| `REPORT_MODEL_DASHBOARD` (READ) | [`brain/fragments/report-model-dashboard`](../../brain/fragments/report-model-dashboard/fragment.yaml) | one JSON string, `dashboardJson` format 1: placed model elements by category, level and workset, and on no level; warnings grouped by text, errors first; views by type, on a sheet or not, with no template; sheets, empty and placeholder; families loaded, in-place and with nothing placed; group types; Revit links by file, loaded and pinned; CAD linked or imported; rooms and Spaces not placed or with no area. A part it could not read is null and named in `notRead`; a list cut at 500 is named in `capped`. **Version 2 (2026-11-01):** an optional `parameters` - up to 12 names - and a `fill` key: per category and per name, how many counted elements have it populated, empty, absent or doubled, with the empty ones' ids; left out, `fill` is null and the rest is version 1's. **Version 3 (2026-11-01):** an optional `breakdowns` - up to 8 `key\|categories\|groupBy\|sum`, written by the brain's BOARDS - and a `breakdowns` key: per breakdown, the elements of those built-in categories grouped by a special key or a built-in parameter, counted, and summed in m, m² or m³ where asked; a refused or faulted one has rows null, a category with none rows `[]`. `format` stays 1 |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01), and every Revit member it calls was read off the shipped
assemblies of all eight (`tools/api-surface --members`). Version 2, the same day, compiles on all eight
too, with the members it adds read the same way, and so does version 3. **Nothing here has run in Revit.** The card is
DRAFT. The panel, the brain module that reads the JSON and the MCP tool are the owner's, built beside it
on the same branch, and are not part of this group.

**The models - copies, never a working project.** Revit 2024: a **fresh** copy of
`Snowdon Towers Sample HVAC.rvt` (its readers answered on 2026-09-13: 145 warnings in 5 kinds, 6 links all
loaded, 7 views on no sheet, 17 sheets), and a new Generic Model family saved as `Family1`, both open. For
JT6, a workshared copy - `Project1 work_ajmal.al` or the owner's own local. The job file is
[`tools/jobs/report-model-dashboard-2026-11-01.yaml`](../../tools/jobs/report-model-dashboard-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| ~~**JT0**~~ | *Open.* The Snowdon copy and `Family1` open in Revit 2024, Snowdon in front | Both titles in the Window list **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2024, 2026-11-01*, which records it run and passed. |
| ~~**JT1**~~ | *The D-30 positive.* The job file's leg on Snowdon | `elementTotal` above 0 and equal to the sum of the categories' `instances`; the levels' `instances` plus `noLevel` equal it too; `notRead` empty; `warnings.total` 145 in 5 kinds, errors first **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - Revit 2024, 2026-11-01*, which records it run and passed. |
| **JT2** | *Its negative - the first case to run.* The job file's leg on `Family1`, Snowdon still open | `elementTotal` 0, `sectionsNotRead` 15 (the job sends `parameters=Mark` and a breakdown, so `fill` and `breakdowns` are the fourteenth and fifteenth); the model block reads `Family1` with `isFamily` true; every other part **null**, never 0 or `[]`; none of Snowdon's numbers anywhere |
| **JT3** | *How long it takes.* The positive's time on the reply | Recorded. On a model of several hundred thousand elements this is the run to repeat: the add-in stops a fragment at `revit.operationTimeoutSeconds` (60 s by default) |
| **JT4** | *Each part beside its reader - the second route.* On Snowdon, in the same session: `READ_MODEL_WARNINGS`, `FIND_VIEWS` (placement unplaced, then withoutTemplate true), `FIND_SHEETS`, `LIST_LINKED_MODELS`, `FIND_PURGEABLE_ELEMENTS`, `SELECT_UNENCLOSED_ROOMS` (rooms only, then spaces only) - the readers named here until 2026-11-01, when they were merged into these | Each agrees with its part: `warningCount` = `warnings.total`; the unplaced views = `views.notOnSheets`; the untemplated views = `views.withoutTemplate`; 17 sheets; six link rows; `wholeFamilyUnused` = `families.unused`; `unusedGroupTypeNames` counted = `groups.unusedTypes`; unplaced and unenclosed = the rooms' and spaces' counts. **A disagreement is a FRAGMENT-ISSUES row**, never a rounding |
| **JT5** | *Revit's own windows.* Manage > Warnings, Manage Links, the Project Browser's Sheets, one level's Properties | The same warning total; the same link files loaded the same way; the same sheet count; that level's `elevationMm` the Elevation its Properties show |
| **JT6** | *A closed workset.* On the workshared copy, one user workset closed in Collaborate > Worksets, then every workset open | `isWorkshared` true; the closed one `open` false with `instances` 0; `elementTotal` lower than with every workset open; `findings` says nothing on a closed workset is counted. **NOT YET SEEN by any Heron reader** - LIST_WORKSETS' proof never met a closed one |
| **JT7** | *Zero is not null.* A new project from the default template, nothing placed | Every part a value - `links` `[]`, `cad.total` 0, `rooms.total` 0, `sheets.total` 0 - and `notRead` empty; a project that is not workshared has `worksets` `[]` and NO worksets entry in `notRead` |
| **JT8** | *Rooms and sheets, arranged.* In the JT7 project: one room placed then deleted from its plan, one room whose walls are opened; one sheet with only a title block, one with one schedule and nothing else | `rooms.unplaced` 1 and `rooms.notEnclosed` 1, each id in its own list only; `sheets.empty` counts the title-block sheet and not the schedule sheet |
| **JT9** | *The brain reads it.* The JT1 `dashboardJson` through `heron_model_dashboard.dashboard()` | It reads whole - no "No dashboard was made" - and each `notRead` and `capped` line is shown as the page says |
| **JT10** | *Version 2: arrange, by hand, kept in the copy.* On the Snowdon copy, before JT1: one mechanical equipment item's Mark typed `AHU-01`, another's Mark cleared; write both ids down | The two ids written down - JT11 is judged against them |
| **JT11** | *Version 2's positive - it rides on JT1.* The job file sends `parameters=Mark` | `fill.parameters` `["Mark"]`; one row per category in `categories`, by name; in each row `populated` + `empty` + `absent` + `doubled` equals that category's `instances`; the Mechanical Equipment row counts `AHU-01` as populated and lists the cleared one's id in `emptyIds`; a Mechanical Equipment schedule with Mark, filtered to Mark has a value, shows the same populated count |
| **JT12** | *Version 2's negative - the fill case that matters.* By hand: `validate report-model-dashboard --in "Snowdon Towers Sample HVAC" --set "parameters=Mark, Heron No Such Parameter"` | Every `Heron No Such Parameter` row: `populated` 0, `empty` 0, `doubled` 0, `absent` its category's `instances`, `emptyIds` `[]` - **absent, never empty**; the Mark rows exactly JT11's |
| **JT13** | *Names forgiven, and the type read.* `parameters=  type MARK ,  mark  ` | `fill.parameters` `["type MARK", "mark"]`, in that order; the counts of `Type Mark, Mark`; a type whose Type Mark is filled once counts every instance of it as populated |
| **JT14** | *The limit, and asking for nothing.* 13 names; then `parameters` left out; then `parameters=` | 13: `fill` null, one `fill:` line naming the limit of 12, every other part read. Left out and empty: version 1's answer with `"fill": null` and **no** `fill:` line - not asking is not a part that failed |
| **JT15** | *A name two parameters share.* In the JT7 project: a shared parameter `Fire Rating` bound to Doors beside the door family's own; `parameters=Fire Rating` | The doors carrying both are counted `doubled` - never populated or empty - and each door is in exactly one of the four counts (D-54 s3, FRAGMENT-ISSUES 5b-203) |
| **JT16** | *Mark in another language - when a non-English Revit is to hand.* The same copy opened in it, `parameters=Mark` | The same Mark counts as JT11: Mark is read through `ALL_MODEL_MARK`, not by its English name. **NOT ARRANGED** - no non-English Revit has been named |
| **JT17** | *Version 3's positive - it rides on JT1.* The job file sends `breakdowns=ducts-system\|OST_DuctCurves\|@system\|@length` | One `breakdowns` entry, key `ducts-system`, unit `m`; a row per duct system type as the ducts' Properties show it, most ducts first; the counts add up to the categories part's Ducts `instances`; each row's `sum` matches a Duct schedule grouped by System Type with Length totalled, in metres, to the millimetre; `noSum` 0 |
| **JT18** | *Version 3's negative - the breakdown cases that matter.* By hand, the value in quotes (the dry run prints it unquoted, and a shell reads `\|` as a pipe): `--set "breakdowns=ducts-system\|OST_DuctCurves\|@system\|@length;none-here\|OST_Planting\|@type\|;bad-name\|OST_NoSuchCategory\|@type\|"` - first check in Visibility/Graphics that the host model holds no planting | `none-here` rows `[]` - **never null** - with no notRead line; `bad-name` rows **null** and one line `breakdowns: bad-name - Revit has no built-in category named OST_NoSuchCategory`; `ducts-system` exactly JT17's |
| **JT19** | *Rooms, by level, in square metres.* In the JT7 project with rooms on two levels and one room not enclosed: `breakdowns=rooms-level\|OST_Rooms\|@level\|@area` | Unit `m²`; a row per level, its sum the Room schedule's Area total for that level in m²; the not-enclosed room counted with 0 m² and in its level's ids; an unplaced room in no row |
| **JT20** | *A sum that is not a length, an area or a volume, and nothing to sum.* `breakdowns=ducts-flow\|OST_DuctCurves\|@system\|RBS_DUCT_FLOW_PARAM;equipment-length\|OST_MechanicalEquipment\|@type\|@length` | `ducts-flow` rows null, its line saying only a length, an area or a volume is summed - never airflow, never in feet; `equipment-length` unit `m`, every row's `sum` **null** - never 0 - and `noSum` equal to its `count` |
| **JT21** | *The limit, asking for nothing, and the boards.* Nine breakdowns; then `breakdowns` left out; then each of the brain's boards in turn, as `heron_model_dashboard.board_request()` writes it | Nine: `breakdowns` null, one line naming the limit of 8, the rest read. Left out: version 2's answer with `"breakdowns": null` and no line. Each board: an entry per breakdown, read or with its reason - `phases`' two `*` rows each adding up to `elementTotal` |
| **JT22** | *The panel itself, from the chat.* With the Companion on, ask the chat for *"a model dashboard"* on the Snowdon copy | The page opens with the Model Dashboard: the tiles' numbers are JT1's, the bars match the categories and levels, and the chat's one line ends *Nothing was changed*. Then **Refresh**: the time under *Read from Revit* moves and no other number changes. A second Refresh pressed while one reads is told so |
| **JT23** | *Select, Isolate, Reset - on screen only.* On a *Worth a look* row with ids (warnings, say): **Select in Revit**, then **Isolate in view**, then **Reset isolate** | Revit selects exactly that row's elements (the status bar's count = the row's ids); the active view shows only them with Revit's temporary-isolate border; Reset ends it. **Nothing in the model changed**: Revit's undo list gains no Heron entry for Select; for Isolate write down what it shows (ES3 asks the same) |
| **JT24** | *A board, and a bar that selects.* Pick *Board: MEP systems*, Refresh; click a bar's name under *Duct length by system* | The board's charts appear, lengths in m; clicking the bar selects that system's ducts in Revit - compare with the System Browser |
| **JT25** | *Since the last read.* Refresh, delete one duct, Refresh again; then Undo in Revit and Refresh | The second read lists *Model elements* down by 1 and *Ducts* down by 1; the third lists them back up. A model with no project id says nothing is kept, and the panel still shows |
| **JT26** | *Parameters filled, from the page.* Type `Mark, Fire Rating` in the box, Refresh; then Select on the *'Mark' empty* row | The fill table matches JT10's counts; Select picks exactly the elements a schedule shows with Mark blank |
| **JT27** | *Colour the view, then Clear.* With Changes on, in a plan showing ducts: type `System Type`, **Colour the view**, answer Keep if Revit asks; then **Clear colours** | Each distinct system type in its own colour, the same again on a second run; Revit's undo list gains ONE entry per press, named for the capability; Undo takes the colours off in one step; Clear takes off only per-element colours, a category's own colour stays. With Changes off, Revit refuses and nothing changes |
| **JT28** | *The model has changed (D-147).* With the redeployed add-in: open the dashboard, Refresh, then draw one duct in Revit and wait two seconds; then Refresh again; then switch to another open model | After the duct the page shows *The model has changed in Revit since this read - press Refresh*, and nothing refreshes by itself; after Refresh the note is gone; on the other model the note does not show for the first one's change. `%LOCALAPPDATA%\Heron\live\pid-N.json` carries `"changes"` rising by one per committed change and `null` with no model open. Revit stays as responsive as before while drawing - the event only counts |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept report-model-dashboard
--by "Ajmal PS"` once JT1, JT2, JT11 and JT17 pass, and `heron-status` set to PROVEN in the same change. A row that
fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the owner's
PC ranks with `model+rerank`, which is **NOT RUN** here. *"model dashboard"*, *"model overview"* and *"model
health summary"* reach this card, and so do *"show me the model dashboard"* and *"give me a model
overview"*; plain *"list warnings"*, *"is the model healthy"* and *"how many warnings"* still reach
`READ_MODEL_WARNINGS`. `check-routing` against a run taken before the card: no question reaches a write, the
card takes no other card's declared sentence (a first purpose that said *"how many ... MEP Spaces"* took
`FILTER_ELEMENTS_BY_CATEGORY`'s *"how many MEP spaces are in the model"* on the keyword route, and was
reworded), and one contest moved without the card's words in it: *"what flow does each air terminal
have"*, `REPORT_CONNECTOR_LOADS`' own sentence, now ranks `SET_AIR_TERMINAL_FLOW` first **on the keyword
route alone** - one more card shifts every word's weight. `heron_brain.lookup` answers it, and two
paraphrases of it, with `REPORT_CONNECTOR_LOADS` (a third, *"what airflow is on each diffuser"*, with
`REPORT_SPACE_AIRFLOW` - a read too), so the host's route is unchanged; recorded, not chased.

**What the reply carries, measured off the code on 2026-11-01.** A provided string crosses whole:
`RevitFragment.Describe` returns it unchanged, the bridge writes the reply as one line with no cap, and the
client reads to the newline with no cap. The bridge's one size limit is on the REQUEST -
`BridgeServer.MaxRequestChars`, 1 MiB a line, which the fragment's own source travels in. What bounds a big
model is the run's time (JT3), and the JSON's size is bounded by its caps - at most 500 ids or names a list.

### Results - Revit 2024, 2026-11-01

Run by a session with the owner at the PC. Revit 2024.3, session 15400. A fresh copy of
`Snowdon Towers Sample HVAC.rvt` from Revit 2024's `Samples` folder, in a scratch folder under `%TEMP%\heron-work`,
with the six linked sample models copied beside it and reloaded; `Family1` - Metric Generic Model, made by
`CREATE_FAMILY_DOCUMENT` and saved in the same folder - open behind it. `batch-prove --unattended --session 15400`
on the job file, code fingerprint `26e34c5fe86c31a2`; the run record names Snowdon for the positive and `Family1`
for the negative. **JT10 was not arranged**: no Mark was typed or cleared.

**A copy of the sample away from its links is a different case.** The first run used the copy before its
links were beside it: every link came back `loaded` false, all 67 spaces `notEnclosed`, and `warnings.total`
211 - 67 *Space is not in a properly enclosed region* in place of 1 *Space volumes overlap*. The dashboard read
that copy as it was; the arrangement was wrong. A fresh copy for this group needs the six linked models in the
same folder, or the reading of 145 does not apply.

| # | Result | What it read |
|---|---|---|
| JT0 | PASS | `REPORT_OPEN_DOCUMENTS`: Snowdon active, `Family1` open in the Family Editor |
| JT1 | PASS, second run | `elementTotal` 4750 = the categories' `instances` 4750 = the levels' 4623 + `noLevel` 127; `notRead` []; `warnings.total` 145, 0 errors, 5 kinds; `links` six rows, each loaded; spaces 67, `notEnclosed` 0. Three of the categories are named *Center line* - [row 5b-1355](../fragment-issues/section-5b-rows-401-425.md) |
| JT1, first run | NOT JUDGED - arrangement | The links not found, as above: `elementTotal` 4682, `warnings.total` 211. Not a reading of the card |
| JT2 | PASS | `Family1`: `model` {title `Family1`, `isFamily` true, `isWorkshared` false}; provided `elementTotal` 0, `sectionsNotRead` 15; every other part null, `fill` and `breakdowns` among them; `capped` []; neither 4750 nor 145 anywhere in its JSON |
| JT3 | NOT RUN | The reply's time was not read |
| JT11 | NOT JUDGED | Rides on JT1: `fill.parameters` ["Mark"]; 14 rows, each row's four counts equal to its own category's `instances` (three rows are *Center line*, row 5b-1355); Mechanical Equipment 37 populated, 0 empty. JT10 was not arranged, so the AHU-01 and cleared-Mark halves have nothing to be judged against |
| JT17 | PART - the schedule half owed | `breakdowns` one entry, `ducts-system`, unit `m`: Supply Air 633 ducts 721.629 m, Exhaust Air 250 ducts 292.576 m, Return Air 170 ducts 266.247 m, most first; 633 + 250 + 170 = 1053, the Ducts `instances`; `noSum` 0 on each. The Duct schedule grouped by System Type NOT RUN |
| JT22 | PART | Asked from the chat with the Companion on, on the first run's copy: *"Model Dashboard for Snowdon Towers Sample HVAC: 4682 model elements in 13 categories; 211 warnings; 6 things worth a look ... Nothing was changed."*; the owner confirmed the page opened with tiles, bars and *Worth a look*. Refresh NOT RUN |

Still owed before the signature: JT10 then JT11, and JT17's schedule half. The draft from the second run is
on the PC (`brain/proof-drafts/report-model-dashboard.yaml`, not in the repository) - **not signed**.

### Later - 2026-11-02: proved and signed, on another model

`REPORT_MODEL_DASHBOARD` was proved on 2026-11-02 and signed by Ajmal PS - Revit 2020, session 12404, positive
on *P1102 STR*, negative on *P1102 Light* - with the same code fingerprint `26e34c5fe86c31a2` as the run above
([#791](https://github.com/Ajmalpshaik/Heron-AI/pull/791)); the card's `proof:` block holds that run. So the
*not signed* and *still owed* lines above describe 2026-11-01 and are kept as that day's record. Which of JT10, JT11
and JT17's schedule half that proof covered is not judged here; its `proof:` block is the record. [Row 5b-1355](../fragment-issues/section-5b-rows-401-425.md)
(three categories named *Center line*) is untouched by it and stays OPEN.
