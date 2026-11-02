# Needs checking — Group LH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LH - a panel schedule template made, or a panel's schedule switched to one: `SET_PANEL_SCHEDULE_TEMPLATE` (2026-11-02)

Asked for in the modeller's words: *"what panel schedule templates are in this model"*, *"make a new panel
schedule template from Branch Panel called DB Standard"* and *"set panel DB-1 to use the DB Standard
template"* - Revit's Manage > Panel Schedule Templates, and Change Template on an open panel schedule. No card
made a template or switched one: a grep of `brain/fragments` that day for `PanelScheduleTemplate.Create`,
`CopyFrom` and `GenerateInstanceFromTemplate` found nothing. `CREATE_PANEL_SCHEDULES` picks an existing
template for a schedule view it is **making** and skips a panel that has one - it never makes a template and
never switches one; `REPORT_PANEL_CIRCUITS` reads circuits - look-alikes, not duplicates.

| What | Where | What it does |
|---|---|---|
| `SET_PANEL_SCHEDULE_TEMPLATE` (MODIFY) | [`brain/fragments/set-panel-schedule-template`](../../brain/fragments/set-panel-schedule-template/fragment.yaml) | `action` **make**: a new panel schedule template, a **copy of one the model has** (`fromTemplate`, exact name) under the modeller's name (`templateName`) - `Create` with the source's kind and layout, then `CopyFrom`. `action` **apply**: the **panel schedule** of each panel selected or named by Panel Name (`panelName`) switched to a template the model has (`GenerateInstanceFromTemplate`). No template or panel name is guessed: one not there exactly is refused with the names listed, one differing in capitals named and not taken, two panels with one name both named and neither touched. A new name already used, a panel already on the template, a template of another kind and a panel with **no schedule yet** are skipped and named - making a schedule is `CREATE_PANEL_SCHEDULES`'. Templates, or each schedule's template, before and after; a read-back that disagrees throws, so the whole change rolls back. Never renames, overwrites or deletes |
| `REPORT_ELECTRICAL_SETTINGS` (READ), widened | [`brain/fragments/report-electrical-settings`](../../brain/fragments/report-electrical-settings/fragment.yaml) | A sixth list, `show` **templates**: each panel schedule template's kind (branch panel, switchboard, data panel), the default, and the panel schedules on it by name (`panelScheduleTemplates`). Its other lists are [Group KU](group-ku.md)'s |

**One of a pair.** `REPORT_ELECTRICAL_SETTINGS` answers every question about the templates - which there are,
their exact names and kinds, which schedules use each - so none reaches the write (D-86).

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `PanelScheduleTemplate.Create(Document, PanelScheduleType, PanelConfiguration,
string)`, `CopyFrom(Document, PanelScheduleTemplate)`, `GetPanelScheduleType`, `GetTableData`, `IsDefault`,
`IsValidType` and `IsValidPanelConfiguration`, `PanelScheduleData.PanelConfiguration`, and
`PanelScheduleView.GenerateInstanceFromTemplate(ElementId)`, `GetTemplate`, `GetPanel` and
`IsPanelScheduleTemplate` are the same 2020 to 2027. A template is an `Element`, not an `ElementType`, so it
has no `Duplicate`; and no parameter on a panel holds its template - the template belongs to the panel's
schedule view. **Nothing here has run in Revit.** Both cards are DRAFT. **Only Revit can settle** whether
`CopyFrom` carries the source's name over (the name is read back and set again if it does), whether a copy
keeps the source's columns and sections, and what `GenerateInstanceFromTemplate` does to the schedule view
it is called on - the same view or a new one (the panel's schedule is found again either way) - LH2 to LH4.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `e827736` without
the change and with it (`model` backend, re-ranker absent; no Revit filter, 2020, 2024 and 2025; every declared
utterance and routing-table sentence in the library, 4,363 with the probes), with and without the host's
question flag. The question reaches `REPORT_ELECTRICAL_SETTINGS` with and without the flag
(`REPORT_PANEL_CIRCUITS` before); the two commands reach the write without it (`REPORT_PANEL_CIRCUITS`, a read,
before). Without the flag no other sentence moved. With it, six write sentences that no read can own -
`CREATE_BEAM_SYSTEM`'s, `CREATE_VIEW_FILTER`'s, `SET_SCHEDULE_SORT_GROUP`'s, `SET_SCHEDULE_OPTIONS`',
`SET_SCHEDULE_APPEARANCE`'s, `SET_SCHEDULE_FILTERS`' and `UPGRADE_FAMILY_FILES`' - moved from one read to
another, or to an ANALYZE or EXECUTE card; none reaches its claimant on either tree. Probes nobody said, so not
declared: *which panel schedule template does DB-1 use* and *change the template on this panel schedule* reach
the write without the flag and `REPORT_PANEL_CIRCUITS` with it.

**The model - Group KU's scratch project** (`electrical-settings-ku`, [KU1](group-ku.md)) in Revit 2020,
never a working model. The job file is
[`tools/jobs/set-panel-schedule-template-lh-2026-11-02.yaml`](../../tools/jobs/set-panel-schedule-template-lh-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every write is **rolled back** (`validate --write`) except
the one `try-once`, which the modeller keeps or discards. `REPORT_ELECTRICAL_SETTINGS` with `show` templates
before and after every row. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when
two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LH1** | *Arrange.* In KU's project, write down the panel schedule templates (Manage > Panel Schedule Templates > Manage Templates) - the job file assumes one named **Branch Panel**; place two branch panelboards, Panel Name **DB-1** and **DB-2**; make a panel schedule for DB-1 only; save | The templates the electrical template ships, and DB-1's schedule on one of them |
| **LH2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's first job | Negative, a new template named **Branch Panel**: `changed` 0, skipped *A panel schedule template is already named "Branch Panel" - left as it is, nothing made.*, `before` and `after` the same. Positive, **DB Standard** from Branch Panel: `changed` 1, *"DB Standard" (branch panel)* in `after`, and opened in Revit it has Branch Panel's columns and sections - and its own name, not "Branch Panel" |
| **LH3** | *Switching a schedule.* With DB Standard kept once (`try-once`, in a scratch copy): `action` apply, `panelName` DB-1, `templateName` DB Standard; then DB-1 selected with `panelName` left out, `templateName` Branch Panel | `changed` 1 each time; DB-1's schedule opened in Revit shows the template switched; Project Browser > Panel Schedules still ONE schedule for DB-1 |
| **LH4** | *Never guessed, never made.* `action` apply with `panelName` DB-2 (no schedule); `templateName` ZZNOTEMPLATE; the switchboard template the model ships on DB-1; `action` make with `fromTemplate` left out; `action` rename | DB-2 skipped - *has no panel schedule yet* - and no schedule made; refused naming the model's templates; skipped - *a switchboard template and ... a branch panel one*; refused - never guessed; refused - not one of the two things this does. `changed` 0 for each |
| **LH5** | *The read.* The job file's second job | Negative, ZZNOTEMPLATE: `panelScheduleTemplates` 0 and *Nothing in the lists read has a name containing "ZZNOTEMPLATE"*. Positive: every template LH1 wrote down, its kind, the default, DB-1's schedule named under its template |
| **LH6** | *Revit 2026 or 2027.* LH2 and LH3's first case again on a project from that release's template | The same answers |
