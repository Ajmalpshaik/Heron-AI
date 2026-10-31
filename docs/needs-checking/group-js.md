# Needs checking — Group JS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-31 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JS - a phase filter made by name and the phase filters read: `CREATE_PHASE_FILTER`, `REPORT_PHASE_FILTERS` (2026-10-31)

Asked for by the owner on 2026-10-31: a phase filter named *As-Built* in his Revit 2020 project. Nothing in
Heron made one - `heron_lookup` offered only `SET_VIEW_PHASE`, which puts a filter that exists on views, and
no fragment called `PhaseFilter.Create`. A **phase** cannot be renamed or made through the API
([`REPORT_PHASES`](../../brain/fragments/report-phases/fragment.yaml), FRAGMENT-ISSUES row 107); a phase
**filter** is a separate element and can be.

| What | Where | What it does |
|---|---|---|
| `CREATE_PHASE_FILTER` (MODIFY) | [`brain/fragments/create-phase-filter`](../../brain/fragments/create-phase-filter/fragment.yaml) | a new phase filter by name, each of New, Existing, Demolished and Temporary set to By Category, Overridden or Not Displayed, every status read back; a name already used refuses and names that filter |
| `REPORT_PHASE_FILTERS` (READ) | [`brain/fragments/report-phase-filters`](../../brain/fragments/report-phase-filters/fragment.yaml) | every phase filter by exact name, its four statuses, whether it is Revit's own, and how many views and view templates use it |

**What the API does not have - the Graphic Overrides tab of Manage > Phasing.** Read off the shipped
`RevitAPI.dll` of all eight releases, 2020 to 2027, on 2026-10-31 (`tools/api-surface --members`, and a
scan of every public member's signature): the only members that take or return a phase status are
`PhaseFilter.Get/SetPhaseStatusPresentation`, `Element.GetPhaseStatus` and `ElementPhaseStatusFilter`, the
same eight on every release. Nothing carries a status's lines, patterns, halftone or material. So
*Overridden* can be chosen here, and **what it draws is set by hand**.
[Row 5b-1350](../fragment-issues/section-5b-rows-401-425.md).

**What is done and what is not.** Both compile for all eight releases (`tools/check-fragments-compile.py`,
2026-10-31). **Nothing here has run in Revit.** Both cards are DRAFT.

**The model - scratch, never a working project.** A new project from the Metric architectural template in
Revit 2020, saved as `%TEMP%\heron-work\group-js\JS Phase Filters.rvt`. The job file is
[`tools/jobs/phase-filters-js-2026-10-31.yaml`](../../tools/jobs/phase-filters-js-2026-10-31.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JS0** | *Open.* The scratch project open and in front in Revit 2020 | The title bar reads *JS Phase Filters* |
| **JS1** | *Arrange, by hand, kept in the copy.* Level 1's floor plan: Phase Filter *Show Complete*. One view template: Phase Filter *Show All*. Manage > Phasing > Phase Filters: write down every row and its four columns. No filter named *As-Built* | The list written down - it is what JS3 is judged against |
| **JS2** | *What a new filter starts as.* Through the bridge client, `--write` without `--apply`: `create-phase-filter` with `filterName=Probe` and no status given | `statusReport` names each of the four with *(not asked - as Revit made it)*. **Record the four values** - nothing in the assemblies says what `PhaseFilter.Create` leaves them at, and the card says it is not known |
| **JS3** | *`REPORT_PHASE_FILTERS`, the D-30 positive.* The job file's first leg | `filterCount` the rows JS1 wrote down, each line's four statuses as JS1 wrote them; Show Complete used by Level 1's plan, Show All by the one template. **Record which rows say *Revit's own*** (`IsDefault`) - not assumed |
| **JS4** | *Its negative - the first case to run.* `filterName=As-Built` | `filterCount` 0, the finding naming the filters there are; never another filter's settings |
| **JS5** | *`CREATE_PHASE_FILTER`, the D-30 positive.* The job file's second leg: *As-Built*, New and Existing *By Category*, Demolished and Temporary *Not Displayed*, rolled back | `created` 1, `statusReport` the four as asked |
| **JS6** | *Its negative - the first case to run.* `filterName=Show Complete`, `newDisplay=Not Displayed` | `created` 0, *NOTHING WAS MADE*, Show Complete named with its id and four statuses; afterwards Show Complete's New still *By Category* - **it was not edited** |
| **JS7** | *A value that is not one of the three.* By hand, `--write` without `--apply`: `filterName=As-Built`, `demolishedDisplay=Halftone` | *NOTHING WAS MADE*, *Halftone* named, the Graphic Overrides tab named as by hand; no *As-Built* row in the Phase Filters tab |
| **JS8** | *A name differing only in case.* `filterName=show complete` | `created` 0, Show Complete named |
| **JS9** | *Kept, through `try-once`, on the modeller's yes.* `create-phase-filter` *As-Built* as JS5, on the saved copy | Keep: the Phase Filters tab shows *As-Built* with the four as asked; then `REPORT_PHASE_FILTERS` names it - the second route |
| **JS10** | *Then put it on a view.* `SET_VIEW_PHASE` on Level 1's plan with *As-Built*, rolled back | `phaseFilterSet` Level 1's plan - the two tools chain by the filter's name |

**Then sign**, on the owner's word only, one card at a time: `python brain/heron_validate.py accept <fragment>
--by "Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**Routing, measured 2026-10-31 in a cloud container on its `model` backend, re-ranker absent** - the owner's
PC ranks with `model+rerank`, which is **NOT RUN** here. Both cards' own sentences resolve to them by
identity. A paraphrase that puts a filter on a view - *"apply the new phase filter to level 1 plan"* - reached
`CREATE_PHASE_FILTER` instead of `SET_VIEW_PHASE`; on main the same sentence already missed `SET_VIEW_PHASE`
(it reached `SET_ELEMENT_PHASE`). The DRAFT card is offered, never run unasked. Row 5b-1350.
