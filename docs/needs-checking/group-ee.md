# Needs checking — Group EE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-15 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EE - an existing view filter renamed and its rule changed in place: `SET_VIEW_FILTER_CATEGORIES` version 2 (2026-10-15)

Found 2026-10-15 on a live project ([row 5b-481](../fragment-issues/section-5b-rows-401-425.md)): the owner
asked for the filter `OUTSEL_TRG_MECH_Level` (Levels; Type Name does not contain 'TRG_MEC_Level') to become
`OUTSEL_TRG_MECH_Level_Grid` on Levels and Grids with Type Name does not contain 'TRG_MEC'. No tool could
rename a filter or change its rule, so it was done by making a second filter and swapping it onto two views.
Version 2 of [`SET_VIEW_FILTER_CATEGORIES`](../../brain/fragments/set-view-filter-categories/fragment.yaml)
(FRG-VIEW-112) takes `newName`, `parameterName` and `matchValue` beside `categories`, each blank to keep what
the filter has, and does any of the three in one transaction. The card is **DRAFT**: the change made
version 1's proof stale, and `check-signatures` lists it under STALE.

**Already run, 2026-10-15, on Revit 2020 session 25244, `Project2`** - a new, never-saved project from the
Systems template, open beside two live 4355 models and reached by name. Every run was ROLLED BACK, and a
throwaway read probe that shares no code with the fragment then read every filter in Project2: Domestic
still held its four pipe categories and "System Classification contains 'Domestic'", and no filter the runs
made or renamed was left. Nothing below is signed.

| # | Run | Look for |
|---|---|---|
| **EE1** | **RUN, PASS - the draft waits for the owner.** `python tools/batch-prove.py tools/jobs/set-view-filter-categories-v2-revit2020-2026-10-15.yaml --session <pid>` on a new Systems-template project (its header says how). Then read [`brain/proof-drafts/set-view-filter-categories.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept set-view-filter-categories --by "Ajmal PS"` and set `heron-status: PROVEN`. Its `second_route` is EE5's, written into the draft | positive: `changed` 1 and `readBack` "'HERON PROOF Domestic Pipes' (changed - renamed from 'Domestic', categories set, rule set): categories Pipe Fittings, Pipes; rule System Classification does not contain 'Sanitary'" - **a readBack still called Domestic or still saying "contains 'Domestic'" is a FAIL whatever the verdict says**; negative (renamed to Sanitary, a name another filter has): `changed` 0 and `refused` "NOTHING WAS CHANGED. 'Sanitary' is already the name of another filter in this project". Both seen on 2026-10-15 |
| **EE2** | **RUN 2026-10-15, outside the draft** - the owner's own case. `validate --setup create-view-filters-by-rule` with one set of values both read: `filterNames` "HERON PROOF OUTSEL Level", `categories` "Levels, Grids", `parameterName` "Type Name", `matchValues` "does not contain TRG_MEC_Level" (the setup's), `newName` "HERON PROOF OUTSEL Level Grid", `matchValue` "does not contain TRG_MEC" (this one's). A setup step that changes the model takes the run's own values, which is why the categories cannot differ between the two here | seen: "'HERON PROOF OUTSEL Level Grid' (changed - renamed from 'HERON PROOF OUTSEL Level', rule set): categories Grids, Levels; rule Type Name does not contain 'TRG_MEC'; catches 2 element(s) in the model today (Levels 2)" - so Revit 2020 takes Type Name for Levels and Grids together, and the named-parameter path builds the rule. Negative renamed to Sanitary: `changed` 0, refused by name |
| **EE3** | **RUN 2026-10-15, outside the draft** - a new rule with the categories KEPT. Domestic, `categories` blank, `matchValue` "contains Domestic Cold" (parameter kept); negative `parameterName` "Segment Description", which Pipes may be filtered by and Pipe Insulations, Pipe Accessories and Pipe Fittings may not | seen: positive "(changed - rule set) … rule System Classification contains 'Domestic Cold'", four categories kept; negative `changed` 0, "NOTHING WAS CHANGED … the new rule cannot apply to every category it holds: Pipe Insulations - it cannot be filtered by 'Segment Description'; …" - no category quietly taken off |
| **EE4** | **RUN 2026-10-15, outside the draft** - version 1's own call, the three new values blank: Domestic to "Pipes, Pipe Fittings"; negative Walls | seen: the same answers version 1's signed proof recorded - "(changed - categories set): categories Pipe Fittings, Pipes; rule System Classification contains 'Domestic'"; negative left as it was, never emptied, `leftOut` "Walls - it cannot be filtered by 'System Classification'". Only the state words gained "- categories set" |
| **EE5** | **RUN 2026-10-15 - the second route, KEPT, in Project2.** `create-view-filters-by-rule` (PROVEN) made 'HERON PROOF OUTSEL Level' (Levels; Type Name does not contain 'TRG_MEC_Level'), then this fragment `--write --apply`: `newName` 'HERON PROOF OUTSEL Level Grid', `categories` 'Levels, Grids', `parameterName` blank, `matchValue` 'does not contain TRG_MEC'. Then the throwaway read probe, sharing no code with the fragment. **That filter is still in Project2, on no view** - a never-saved project; nothing else there was kept | seen: readBack "(changed - renamed from 'HERON PROOF OUTSEL Level', categories set, rule set): categories Grids, Levels; rule Type Name does not contain 'TRG_MEC'"; the probe "HERON PROOF OUTSEL Level Grid [Grids, Levels] NOT FilterStringContains 'TRG_MEC'", nothing under the old name, Domestic unchanged; the same call sent again (rolled back) `alreadySet` 1, `changed` 0. Two routes agree. A look in Manage, Filters on screen is still his to take if he wants it |
| **EE6** | **NOT RUN - Revit 2021 to 2027.** The same job on any later release. The factories are reached by reflection; 2023 and later take the two-argument spelling, which no run has used yet | as EE1 |
