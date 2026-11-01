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
> the rest is a plan, and each phase is the owner's to start. Retire this note when its last phase is
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
| 2 | Click a bar or a row to select those elements in Revit; isolate; zoom to | S | planned - a `COMPANION_ACTIONS` row at EXECUTE, through `SET_SELECTION` / `ISOLATE_ELEMENTS`. docs/40 §18 question 7 said *no Select-in-Revit button in V1*; this asks it again |
| 2 | Parameter fill-rate by category: populated / empty / absent, against the office's or client's list | R | planned - `heron_modelqa.parameters` already counts the three states; needs a read of the values |
| 2 | Duplicate Mark / Type Mark, inconsistent values ("Lvl 1" vs "Level 01") | R | planned - `FIND_DUPLICATE_VALUES` is PROVEN |
| 3 | MEP: elements connected to nothing, open connectors, systems summary (counts, lengths) | R | planned - `CHECK_EQUIPMENT_CONNECTORS`, `CHECK_FIXTURE_CONNECTIVITY`, `MEASURE_RUN_QUANTITIES` are PROVEN |
| 3 | Quantities by type: lengths, areas, volumes, counts; room areas by level and department | R | planned - `REPORT_MATERIAL_TAKEOFF`, `REPORT_ROOM_SPACE_DATA`, `REPORT_AREAS` are PROVEN |
| 3 | Documentation: sheets with no title block or revision, schedules that are empty | R | planned - `REPORT_SHEET_TITLE_BLOCKS`, `LIST_REVISIONS` are PROVEN |
| 3 | Links and coordinates: shared coordinates agree, elements outside the scope box | R | planned |
| 4 | Report: the dashboard as an HTML / PDF sheet, the folder asked as the Loads report asks it | R | planned - the report route and `pick_folder` already exist |
| 4 | Snapshots: keep today's numbers per project, compare two - added, removed, changed - and a weekly trend | R | planned - kept per project the way `heron_designbasis` keeps answers |
| 4 | Submission readiness against a client's named standard | R | planned - needs the standard quoted (`heron_standards`); never "compliant" |
| 5 | Fix from a row: fill a parameter, rename to the standard, move to the right workset | W | planned - only from PROVEN fragments, Keep or Discard while *Ask to Keep* is on ([D-126](../../DECISIONS.md)) |
| 5 | Purge unused families from the list | W, destructive | planned - Article 7: the modeller's explicit yes for that exact job |
| - | Bulk parameter edit in the table | W | **already designed** (docs/40 §8); refused today until `SET_PARAMETER_VALUES_BY_ID` is PROVEN ([group FE](../../needs-checking/group-fe.md)) |
| - | A page served from the internet calling the PC | refused | Companion README rule 1 |
| - | Refresh on a timer | refused | Companion README rule 3 - it would raise the READING banner and queue work on Revit's thread |
| - | A health score, or "compliant" | refused | a count is not a verdict ([heron_modelqa](../../../brain/heron_modelqa.py)'s rule) |

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

1. Phase 2's Select-in-Revit button changes Revit's selection from the page (EXECUTE, not a model
   change). docs/40 §18 question 7 kept it out of V1. Add it now?
2. Which parameters should the fill-rate check by default - the office's own list, or one per client?
