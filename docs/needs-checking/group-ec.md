# Needs checking — Group EC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-14 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EC - the gap pass of 2026-10-14: what the library could not do, and the tools written for it (2026-10-14)

Written 2026-10-14 by a cloud session asked by the owner to check every fragment for what is missing and
what overlaps, and then to keep adding what is missing while Revit was not free. The findings are
[rows 5b-469 to 5b-473](../fragment-issues/section-5b-rows-401-425.md). **Every row below is DRAFT, read
from the code and compiled for every release it claims - none has been run in Revit.** A compile is
not a proof ([D-30](../DECISIONS.md)).

### The test project - one for the whole group

**"Heron Gap Pass Test"**, a new project from the default metric template, Revit 2024 (any release
each row's fragment claims will do), saved under that name. Each row says which part of it it needs;
make only the parts for the rows being run. Everything is on Level 1 and drawn in
"FloorPlan: Level 1" unless a row says otherwise.

| Part | What to model | Used by |
|---|---|---|
| **P0** | Nothing - the template as it comes: a Fire Protection Wet piping system type, a flex pipe type, Level 1. Check the two type names against the job file's header | EC2 |
| **P1** | One **assembly**: three pipes of one run, Create > Assembly, naming category Pipes. Note its member count in Edit Assembly. One straight wall nearby. Nothing else that is an assembly or a wall in that plan | EC1 |
| **P2** | A **panelboard** family loaded from the library. One panel on Level 1, Panel Name PB-1, visible in "FloorPlan: Level 1", with NO panel schedule. One panel on Level 2, Panel Name PB-2, visible in "FloorPlan: Level 2", and its panel schedule made by hand | EC3 |
| **P3** | One straight **pipe**, DN50, Fire Protection Wet, Middle Elevation 3200 mm, 6000 mm long along X - the only pipe in the model. Two **pendent sprinklers** at 2700 mm, each 600 mm to one side of it in plan and at least 1000 mm in from its ends | EC4 |

| # | Run | Look for |
|---|---|---|
| **EC1** | Part P1: `python tools/batch-prove.py tools/jobs/select-group-members-assembly-2026-10-14.yaml`, then read [`brain/proof-drafts/select-group-members.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept select-group-members --by "Ajmal PS"` and set `heron-status: PROVEN` | `SELECT_GROUP_MEMBERS` version 2 ([row 5b-470](../fragment-issues/section-5b-rows-401-425.md)). Positive: `containerKind` "assembly", the member count equal to Edit Assembly's, and the report naming the assembly type and the Pipes category; negative (the wall): refused by name, `containerKind` empty, no elements. A model group still opens as before - version 1's cases in its `tests/cases.yaml` are unchanged |
| **EC2** | Part P0: `python tools/batch-prove.py tools/jobs/create-flex-pipe-2026-10-14.yaml`, then read [`brain/proof-drafts/create-flex-pipe.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept create-flex-pipe --by "Ajmal PS"` and set `heron-status: PROVEN` | `CREATE_FLEX_PIPE` (FRG-MEP-066), new - the library made flexible duct and nothing flexible on a piping system. Positive: one flex pipe, `lengthMm` 600, the wet system named, and the *not connected* sentence; negative (a 2400 mm route, the same 1200 mm limit): nothing made, `created` null, `lengthMm` 0, both numbers in the refusal. Second route: the positive kept with `--write` by hand, its Length in Properties reading 600 |
| **EC3** | Part P2: `python tools/batch-prove.py tools/jobs/create-panel-schedules-2026-10-14.yaml`, then read [`brain/proof-drafts/create-panel-schedules.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept create-panel-schedules --by "Ajmal PS"` and set `heron-status: PROVEN` | `CREATE_PANEL_SCHEDULES` (FRG-MEP-067), new - the library read a panel's circuits and made no view a sheet could carry. Positive (PB-1): one schedule view made and named; negative (PB-2, already scheduled): nothing made, PB-2 and its view in `alreadyScheduled`. Second route: the positive kept by hand, Project Browser > Panel Schedules showing one new view that opens on PB-1. **And before PROVEN**, on the owner's live backend: *"show the panel schedule for DB-1"*, *"what does the panel schedule say"* and *"open the panel schedule for DB-1"* - on the lexical route they reach this write (the card's routing comment); a question reaching a write is a defect to record |
| **EC4** | Part P3: `python tools/batch-prove.py tools/jobs/create-fixture-branches-2026-10-14.yaml`, then read [`brain/proof-drafts/create-fixture-branches.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept create-fixture-branches --by "Ajmal PS"` and set `heron-status: PROVEN` | `CREATE_FIXTURE_BRANCHES` (FRG-MEP-068), new - Revit taps a diffuser onto a duct in one call and has no pipe twin, so nothing connected a sprinkler or a fixture to a pipe. Positive: `branched` 2, each head a riser of about 500 mm and an arm of 600 mm, joined at the head, `runs` holding five pipes; negative (the same, `maxDistanceMm` 300): nothing drawn, both heads in `noMainNear`. Second route: kept by hand, then `FIT_MEP_JOINTS` on the same pipes and `TRACE_CONNECTIVITY` from a head reaching the main; neither head moved. The method that strings them together is [`sprinkler-piping`](../../brain/skills/sprinkler-piping.yaml), DRAFT, and its first real run is this row's second route |
