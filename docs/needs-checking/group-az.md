# Needs checking — Group AZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AZ - `create-sheet-list` version 2: a view list as well as a sheet list, and only the placed views (2026-09-27)

**One existing fragment edited, not a new one, on the owner's rule that an existing tool is extended
rather than a second one added.** [`create-sheet-list`](../../brain/fragments/create-sheet-list/fragment.yaml) -
CREATE_SHEET_LIST, FRG-SHT-011 - takes a new value, `listKind`. Blank or `sheets` makes the sheet list it
always made; `views` makes Revit's View List through `ViewSchedule.CreateViewList`, with the same fields by
name, the same refusal listing the real names, and the same sort; anything else is refused and nothing is
created. It compiles on every release from 2020 to 2027. **It was PROVEN and is DRAFT now**: the change is
inside `impl/`, so the 2026-09-13 proof (a sheet list on test projject) no longer matches the code and is
kept in the card as the record of version 1 ([D-30](../DECISIONS.md)). [Row 5b-243](../FRAGMENT-ISSUES.md#)
has why.

**`listKind` has to be SENT, blank or not.** The add-in refuses any value the card marks `source: request`
that arrives with none, whatever the card says about it being optional
([row 5b-227](../FRAGMENT-ISSUES.md#)). So a caller that sends only the three old values is now told
`listKind` is missing; sending it blank gets exactly the old job.

**"Only the views placed on a sheet" is not new code.** A view list shows every view. `SET_SCHEDULE_FILTERS`
(FRG-VIEW-060, PROVEN, **unchanged**) already writes a `has value` rule, and a view's Sheet Number has a
value exactly when the view is on a sheet - so the chain is CREATE_SHEET_LIST `listKind=views` with Sheet
Number as a column, then FIND_SCHEDULES and SET_SCHEDULE_FILTERS `filterFieldName=Sheet Number`,
`matchType=has value`, `filterValue` blank.

**The working run: NOT RUN by the session that wrote this.** It ran in a cloud container with no Revit,
and its Heron connection failed to start, so nothing here has met a model. It is handed to the chat
working in "heron ai bulding" (Revit 2024, Heron session 46596), and AZ3 is that run.

Every proof row runs on a TEST COPY of "heron ai bulding" - never the working model. AZ3 is a chat run on
the working model, because the View List is what the owner asked to keep.

| # | Check | Expected |
|---|---|---|
| **AZ1** | The two legs ([D-30](../DECISIONS.md)) - **a draft, not yet signed.** With `HERON_CLIENT_ID=ajmal-pc` set and the test copy in front, `python tools/batch-prove.py tools/jobs/create-sheet-list-views-heron-ai-bulding-2026-09-27.yaml --dry-run`, then the same without `--dry-run`. **Do not sign yet** - the draft's `second_route` reads NOT ESTABLISHED until AZ4 is done | `PASS`. The positive - `listKind=views`, View Name, Sheet Number, Sheet Name, Title on Sheet, sorted by Sheet Number: `fieldsAdded` 4, `refused` empty. The negative - `listKind=views`, `fieldNames=Flow`: `fieldsAdded` 0, and `refused` saying *"'Flow' is not a field a view list can carry. It can carry: ..."* with the view list's real names, then the sort refusal and the NO-columns warning. Both legs rolled back - no `HERON VIEW LIST PROOF` left under Schedules/Quantities |
| **AZ2** | The old job, unchanged. One chat run on a test copy: CREATE_SHEET_LIST `listKind` blank (sent as an empty value), `fieldNames=Sheet Number, Sheet Name`, `sortByField=Sheet Number`, then open the schedule | A SHEET list, exactly as version 1 made it: every sheet a row, in number order, `refused` empty. A view list here is a failure - blank must mean sheets |
| **AZ3** | The View List the owner asked for, on the WORKING model, as chat runs with no selection-fed step: CREATE_SHEET_LIST `listKind=views`, `scheduleName=View List`, fields such as View Name, Sheet Number, Sheet Name, Title on Sheet, View Scale, Detail Number, `sortByField=Sheet Number` (a name refused is replaced by the real name the refusal lists, and which was used is written here); then FIND_SCHEDULES `nameContains=View List` and SET_SCHEDULE_FILTERS `filterFieldName=Sheet Number`, `matchType=has value`, `filterValue` blank; then REPORT_SCHEDULE_DEFINITION on it | The definition reads back the columns asked for, the filter as Sheet Number HasValue, and the sort by Sheet Number. **Heron reads the schedule's setup, not its rows**, so the owner opens it: one row for each of the four plan views on M-101 to M-104 - the Level 1 Room, HVAC Zone and Space plan layouts and "Level 1 - Air Terminal Layout" - each with its own sheet's number. The schedules on M-105 to M-107 are not rows: a view list is a schedule of views, and a schedule is not one |
| **AZ4** | The second route, THEN the signature. View tab, Schedules, View List by hand on the test copy, filtered on Sheet Number has a value, and its rows compared with the views under each sheet in the Sheets branch of the Project Browser. Write what was seen into the `second_route` field of the draft `brain/proof-drafts/` holds for `create-sheet-list`. Only then `python brain/heron_validate.py accept create-sheet-list --by "Ajmal PS"` and set `heron-status: PROVEN` | The hand-made list and the one AZ3 made show the same rows, and every placed view in the Project Browser is one of them |
| **AZ5** | The routing, on the owner's PC with the shared store rebuilt from main after this merges: `heron_lookup` on *"create a view list"*, *"make a schedule of the views on sheets"* and *"create a sheet list"* | All three name CREATE_SHEET_LIST. The first two are its own declared sentences since version 2; the third is the job it always had |
