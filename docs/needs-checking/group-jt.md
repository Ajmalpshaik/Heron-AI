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
| `REPORT_MODEL_DASHBOARD` (READ) | [`brain/fragments/report-model-dashboard`](../../brain/fragments/report-model-dashboard/fragment.yaml) | one JSON string, `dashboardJson` format 1: placed model elements by category, level and workset, and on no level; warnings grouped by text, errors first; views by type, on a sheet or not, with no template; sheets, empty and placeholder; families loaded, in-place and with nothing placed; group types; Revit links by file, loaded and pinned; CAD linked or imported; rooms and Spaces not placed or with no area. A part it could not read is null and named in `notRead`; a list cut at 500 is named in `capped` |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01), and every Revit member it calls was read off the shipped
assemblies of all eight (`tools/api-surface --members`). **Nothing here has run in Revit.** The card is
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
| **JT0** | *Open.* The Snowdon copy and `Family1` open in Revit 2024, Snowdon in front | Both titles in the Window list |
| **JT1** | *The D-30 positive.* The job file's leg on Snowdon | `elementTotal` above 0 and equal to the sum of the categories' `instances`; the levels' `instances` plus `noLevel` equal it too; `notRead` empty; `warnings.total` 145 in 5 kinds, errors first |
| **JT2** | *Its negative - the first case to run.* The job file's leg on `Family1`, Snowdon still open | `elementTotal` 0, `sectionsNotRead` 13; the model block reads `Family1` with `isFamily` true; every other part **null**, never 0 or `[]`; none of Snowdon's numbers anywhere |
| **JT3** | *How long it takes.* The positive's time on the reply | Recorded. On a model of several hundred thousand elements this is the run to repeat: the add-in stops a fragment at `revit.operationTimeoutSeconds` (60 s by default) |
| **JT4** | *Each part beside its reader - the second route.* On Snowdon, in the same session: `READ_MODEL_WARNINGS`, `FIND_UNPLACED_VIEWS`, `FIND_VIEWS_WITHOUT_TEMPLATE`, `LIST_SHEETS`, `LIST_LINKED_MODELS`, `FIND_UNUSED_FAMILIES`, `FIND_UNUSED_GROUP_TYPES`, `SELECT_UNENCLOSED_ROOMS` (rooms only, then spaces only) | Each agrees with its part: `warningCount` = `warnings.total`; the unplaced views = `views.notOnSheets`; the untemplated views = `views.withoutTemplate`; 17 sheets; six link rows; `wholeFamilyUnused` = `families.unused`; `unusedGroupTypes` = `groups.unusedTypes`; unplaced and unenclosed = the rooms' and spaces' counts. **A disagreement is a FRAGMENT-ISSUES row**, never a rounding |
| **JT5** | *Revit's own windows.* Manage > Warnings, Manage Links, the Project Browser's Sheets, one level's Properties | The same warning total; the same link files loaded the same way; the same sheet count; that level's `elevationMm` the Elevation its Properties show |
| **JT6** | *A closed workset.* On the workshared copy, one user workset closed in Collaborate > Worksets, then every workset open | `isWorkshared` true; the closed one `open` false with `instances` 0; `elementTotal` lower than with every workset open; `findings` says nothing on a closed workset is counted. **NOT YET SEEN by any Heron reader** - LIST_WORKSETS' proof never met a closed one |
| **JT7** | *Zero is not null.* A new project from the default template, nothing placed | Every part a value - `links` `[]`, `cad.total` 0, `rooms.total` 0, `sheets.total` 0 - and `notRead` empty; a project that is not workshared has `worksets` `[]` and NO worksets entry in `notRead` |
| **JT8** | *Rooms and sheets, arranged.* In the JT7 project: one room placed then deleted from its plan, one room whose walls are opened; one sheet with only a title block, one with one schedule and nothing else | `rooms.unplaced` 1 and `rooms.notEnclosed` 1, each id in its own list only; `sheets.empty` counts the title-block sheet and not the schedule sheet |
| **JT9** | *The brain reads it.* The JT1 `dashboardJson` through `heron_model_dashboard.dashboard()` | It reads whole - no "No dashboard was made" - and each `notRead` and `capped` line is shown as the page says |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept report-model-dashboard
--by "Ajmal PS"` once JT1 and JT2 pass, and `heron-status` set to PROVEN in the same change. A row that
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
