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

| # | Run | Look for |
|---|---|---|
| **EC1** | Part P1: `python tools/batch-prove.py tools/jobs/select-group-members-assembly-2026-10-14.yaml`, then read [`brain/proof-drafts/select-group-members.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept select-group-members --by "Ajmal PS"` and set `heron-status: PROVEN` | `SELECT_GROUP_MEMBERS` version 2 ([row 5b-470](../fragment-issues/section-5b-rows-401-425.md)). Positive: `containerKind` "assembly", the member count equal to Edit Assembly's, and the report naming the assembly type and the Pipes category; negative (the wall): refused by name, `containerKind` empty, no elements. A model group still opens as before - version 1's cases in its `tests/cases.yaml` are unchanged |
| **EC2** | Part P0: `python tools/batch-prove.py tools/jobs/create-flex-pipe-2026-10-14.yaml`, then read [`brain/proof-drafts/create-flex-pipe.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept create-flex-pipe --by "Ajmal PS"` and set `heron-status: PROVEN` | `CREATE_FLEX_PIPE` (FRG-MEP-066), new - the library made flexible duct and nothing flexible on a piping system. Positive: one flex pipe, `lengthMm` 600, the wet system named, and the *not connected* sentence; negative (a 2400 mm route, the same 1200 mm limit): nothing made, `created` null, `lengthMm` 0, both numbers in the refusal. Second route: the positive kept with `--write` by hand, its Length in Properties reading 600 |
