<!--
Heron-Agent:  HERON-QA-BIM-011
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Companion Model Dashboard - the plan, phase by phase

> **Started 2026-11-01, on the owner's word:** *"Check always this as a referance and buld the conpnaiban
> app with this and what are the ideas you have you can add that aslo okkey"* - "this" being
> [BIMLens](https://github.com/IbrahimFahdah/BIMLens). **Phase 1 is built** ([docs/40 §21.10](../../40-heron-companion.md));
> the rest is a plan, and each phase is the owner's to start. **Every idea BIMLens has is in the inventory below**, each with Heron's way of doing it. Retire this note when its last phase is
> built or dropped, after moving what lasts into docs/40.

## The reference, and what may be taken from it

**BIMLens** - live model dashboards in a browser, fed by a small Revit add-in on the modeller's own PC,
Revit 2024-2026, first release 2026-10-30. **Its repository holds a README only, with no code and no
licence**, so it is all rights reserved: nothing of its code, name, wording or look comes into Heron, and
there is no code to read in any case. What it gives is the **list of ideas** below - studied and
re-authored, never imported ([D-25](../../DECISIONS.md), [docs/31](../../31-studying-the-existing-libraries.md)).

Check it again before each phase - its release notes are where a new idea shows first. Its hosted
dashboard page (`ibrahimfahdah.github.io`) is not reachable from a cloud session's network.

**One part of how BIMLens works is refused here:** its page is served from the internet and calls the
add-in on `127.0.0.1`, which needs the browser's local-network permission. The Companion is served from
the modeller's own PC and loads nothing from the internet (Companion README rule 1). That stays.

## Every idea, by phase

**R** reads only · **S** selects or zooms in Revit - not a change · **W** changes the model - through
`revit_change`'s own body, a PROVEN fragment ([D-117](../../DECISIONS.md)), one undo entry · **refused** -
breaks a rule.

| Phase | What | Kind | Status |
|---|---|---|---|
| **1** | Tiles: elements, worksets, warnings, views, sheets, families, links, CAD, rooms | R | **built**, not run in Revit |
| 1 | Bars: elements by category, level, workset; warnings by kind; views by type | R | **built**, not run in Revit |
| 1 | *Worth a look*: errors, warnings, unplaced and unenclosed rooms and Spaces, empty sheets, unused families and group types, imported CAD, unloaded or unpinned links, closed worksets, elements with no level, views with no template or off sheets, in-place families | R | **built**, not run in Revit |
| 1 | Copy IDs for Revit's Manage > Inquiry > Select by ID | R (the clipboard only) | **built** |
| 1 | Refresh - the modeller's own press, never a timer | R | **built**, not run in Revit |
| 2 | Select a row's elements in Revit; isolate them in the active view; reset | S | **built** 2026-11-01 on the owner's *"do it until finish"* - [docs/40 §21.11](../../40-heron-companion.md); three `COMPANION_ACTIONS` rows at EXECUTE. Not run in Revit. Zoom to, and clicking a bar, still planned |
| 2 | Parameter fill-rate by category: filled / empty / missing, for the parameters the modeller types | R | **built** - [docs/40 §21.12](../../40-heron-companion.md). Not run in Revit |
| 2 | Duplicate Mark / Type Mark, inconsistent values ("Lvl 1" vs "Level 01") | R | planned - `FIND_DUPLICATE_VALUES` is PROVEN |
| 2 | *The model has changed - Refresh*: the add-in marks the live file when the model changes after the read | R | **waiting on the owner** - watching for changes amends D-09 again (question 3 below) |
| 2 | Rule checks, filters, ratio and element tables behind a bar; read options (categories, links, active view only) | R | planned - see the inventory below |
| 3 | MEP: elements connected to nothing, open connectors, systems summary (counts, lengths) | R | planned - `CHECK_EQUIPMENT_CONNECTORS`, `CHECK_FIXTURE_CONNECTIVITY`, `MEASURE_RUN_QUANTITIES` are PROVEN |
| 3 | Quantities by type: lengths, areas, volumes, counts; room areas by level and department | R | planned - `REPORT_MATERIAL_TAKEOFF`, `REPORT_ROOM_SPACE_DATA`, `REPORT_AREAS` are PROVEN |
| 3 | Documentation: sheets with no title block or revision, schedules that are empty | R | planned - `REPORT_SHEET_TITLE_BLOCKS`, `LIST_REVISIONS` are PROVEN |
| 3 | Links and coordinates: shared coordinates agree, elements outside the scope box | R | planned |
| 4 | Report: Print / PDF of the dashboard alone; CSV made on the PC | R | **built** - docs/40 §21.12 |
| 4 | Snapshots: each read kept per project, numbers only; *since the last read* | R | **built** - docs/40 §21.12. A weekly trend chart still planned |
| 4 | A DEMO of an invented model, for training with no Revit | R | **built** - docs/40 §21.12 |
| 4 | Submission readiness against a client's named standard | R | planned - needs the standard quoted (`heron_standards`); never "compliant" |
| 5 | Fix from a row: fill a parameter, rename to the standard, move to the right workset | W | planned - only from PROVEN fragments, Keep or Discard while *Ask to Keep* is on ([D-126](../../DECISIONS.md)) |
| 5 | Purge unused families from the list | W, destructive | planned - Article 7: the modeller's explicit yes for that exact job |
| 6 | A 3D view of the model - the active 3D view or the whole model - drawn by Heron's own WebGL, pick to select, Show in 3D | R | **built** on the owner's ask - [docs/40 §21.13](../../40-heron-companion.md). Stops itself at 40 s by default (owner: *"Yes, stop at ~40 s"*). Not run in Revit |
| - | Bulk parameter edit in the table | W | **already designed** (docs/40 §8); refused today until `SET_PARAMETER_VALUES_BY_ID` is PROVEN ([group FE](../../needs-checking/group-fe.md)) |
| - | A page served from the internet calling the PC | refused | Companion README rule 1 |
| - | Refresh on a timer | refused | Companion README rule 3 - it would raise the READING banner and queue work on Revit's thread |
| - | A health score, or "compliant" | refused | a count is not a verdict ([heron_modelqa](../../../brain/heron_modelqa.py)'s rule) |

## Everything BIMLens does, idea by idea, and Heron's way of each

**Read 2026-11-01** from its 0.1.2 release for Revit 2024 - the add-in's text strings and the offline copy
of its dashboard, read as text for the ideas; nothing run, nothing kept, the download deleted after. Asked
for by the owner: *"Each and everything you can take from him the things and idea"*. **The ideas are
taken; the code, names, wording and look are not** - each row is re-authored the Heron way, and the
words below are Heron's own.

| BIMLens has | Heron's way | Phase |
|---|---|---|
| **Board templates** - model overview, health check, rooms and areas, missing values, material quantities, design options, linked models, doors and fire safety, walls, structure, three for IFC files | **Panels the brain defines**, each a named set of tiles, bars and checks: Overview and Health are phase 1; Rooms, Missing values, Doors, Walls, Structure, Materials, Design options, Links follow, one per reading | 1 built; 2-3 |
| **Chart kinds** - bar, donut, total, ratio, pass/fail rule, table, file statistics | Bars, totals and pass/fail rows are phase 1; ratio (share of a total) and a table of the elements behind a bar in phase 2; **no donut** - a bar reads the same numbers more exactly | 1-2 |
| **Rule checks** - has a value, exists, equals, greater than, pattern; shipped rules: Mark, Type Mark, Fire Rating, Department, Number filled; door width >= 900 mm; fire-rated door has a closer; type name pattern; wall base offset 0; room placed and enclosed; no warning | Checks **the brain** holds, with the value and the clause it came from. **A design limit - a door width, a closer - is never Heron's own number**: it is asked, or quoted from the project's standard (D-33, `heron_standards`) | 2 |
| **Parameter explorer** - search a parameter, how many have it, how many have a value, its commonest values, chart it | Fill-rate in three states - populated, empty, absent (`heron_modelqa.parameters`) - and the commonest values, for any parameter the modeller names | 2 |
| **Filters** - for the whole board, one chart, a pinned parameter; show only / exclude a value | Filters held on the page, applied to rows the brain already sent - no new read | 2 |
| **Click a bar to pick its elements**; several with Ctrl; right-click for select in Revit, select and zoom, isolate, reset | Copy IDs is phase 1. Select, zoom and isolate from the page in phase 2 - a `COMPANION_ACTIONS` row at EXECUTE through `SET_SELECTION` (PROVEN); isolate through the temporary view door the chat already uses | 1 built; 2 |
| **Colour the model by a chart** in Revit views; clear colours; keep colours after stopping | Colour a view by a bar's values in phase 5 - a view change through `revit_change`, one undo entry, Keep or Discard ([D-126](../../DECISIONS.md)); Clear is its own undo entry, never automatic | 5 |
| **Edit a parameter in a table** - propose, approve or reject, push as one named Revit transaction | **Already designed** - the Table's Apply (docs/40 §8) with a stale-row check and one undo entry; refused today until `SET_PARAMETER_VALUES_BY_ID` is PROVEN | waiting on [group FE](../../needs-checking/group-fe.md) |
| **Read settings per model** - which categories, which links (each its own choice), design options, only what the active view shows, geometry detail, Area and Volume on or off, a room for each instance (Room, From Room, To Room) | The dashboard read takes options in phase 2: categories, include links, the active view only. Kept per project as the Loads panel keeps its answers | 2 |
| **What it reads per element** - category, family and type, level, workset, design option, phase created and demolished, group, in-place, materials with area and volume, rooms, warning count and text, source model | Phase 1 counts by category, level and workset. Phase 2-3 adds design option, phase, group, material area and volume (`REPORT_MATERIAL_TAKEOFF`), the room each instance is in, and warnings per element | 2-3 |
| **File statistics** - file size, loaded families, in-place families, model and detail groups, materials, line styles, fill patterns, view templates, views not on sheets, sheets, imported CAD | Phase 1 has families, in-place, groups, views not on sheets, sheets, CAD. File size, materials, line styles, fill patterns and view templates in phase 2 - counts only | 1 built; 2 |
| **Live sync** - the dashboard updates as the model changes, through an event stream from the add-in | **Never by asking Revit on a timer** (README rule 3). Instead: the add-in already writes the live file on Idling; it can add *the model changed since the dashboard read it*, and the page says **"the model has changed - Refresh"**. The modeller still presses Refresh | 2 |
| **3D view of the whole model** - orbit, pan, zoom, click an element for its parameters, frame the selection, rooms as volumes, material colours, save a picture | Heron draws its own 3D (`loads3d.js`, `sprinkler3d.js`) and loads nothing from the internet. A whole-model view needs a geometry read with a triangle limit and its own renderer - **the largest idea here** | 6 |
| **Open a model file with no Revit** - IFC, or its own export file; a demo with a sample model when no Revit runs | **A dashboard snapshot** saved per project - the read as JSON - opens on the page with no Revit (phase 4). A **demo panel** from the tests' sample data for training (phase 4). Reading IFC is a separate decision - it needs a third-party IFC engine | 4; IFC not planned |
| **Export** - print the board or save it as PDF, CSV of a chart, PNG of the 3D view, board layouts shared as JSON templates | Report as HTML and PDF, the folder asked (the Loads report's route); CSV of a table; the panel layouts are the brain's, so a shared template is a brain file, reviewed like any other | 4 |
| **Arrange the board** - drag and resize cards, several boards as tabs, lock, zoom, favourites, rename | Not taken: the Companion is one column by the owner's decision of 2026-10-04 (docs/40 §21.6). Collapsible sections instead | - |
| **Speed** - reads parameters in bulk with a fallback, times each step, logs to a file | Heron's fragment runs are already timed in the run records; the dashboard read reports its own time in phase 2 | 2 |
| **Hosted page that calls the PC** - a page on the internet, the browser's local-network permission, open CORS for that site, a token in the address | **Refused** - README rule 1; the Companion answers no CORS at all and pairs with a one-time code | refused |
| **Sends the model to nobody** - data stays on the PC | Already Heron's rule - the page talks only to Heron on this PC | kept |

## Phase 1, as built

| Part | File |
|---|---|
| The read - one pass, one JSON string | `brain/fragments/report-model-dashboard/` (`REPORT_MODEL_DASHBOARD`, DRAFT) |
| Every tile, bar, colour and line | [`brain/heron_model_dashboard.py`](../../../brain/heron_model_dashboard.py), tested by [`tests/test_model_dashboard.py`](../../../tests/test_model_dashboard.py) |
| The brain's door | `model_dashboard` in [`mcp/server/heron_brain.py`](../../../mcp/server/heron_brain.py) |
| The chat's tool and the page's Refresh | `revit_model_dashboard`, `_read_dashboard`, `_dashboard_refresh` in [`mcp/server/heron_mcp_server.py`](../../../mcp/server/heron_mcp_server.py); its risk row in [`heron_tools.py`](../../../mcp/server/heron_tools.py) |
| The panel | `DashboardPanel` in [`mcp/companion/heron_companion.py`](../../../mcp/companion/heron_companion.py); the page in [`static/`](../../../mcp/companion/static/), tested in [`tests/test_companion.py`](../../../tests/test_companion.py) |

**Owed before it is relied on:** the fragment's proof on a named real model, with its negative case -
its own proof plan says how.

## Questions for the owner, one at a time, before phase 2

1. ~~Phase 2's Select-in-Revit button?~~ **Answered 2026-11-01: *"yes allow select button"*** (docs/40
   §21.11).
2. Which parameters should the fill-rate check by default - the office's own list, or one per client?
   Today the box starts empty and keeps what was typed last.
3. *The model has changed - Refresh* needs the add-in to watch for model changes (DocumentChanged), a
   second amendment to D-09. Allow it?
