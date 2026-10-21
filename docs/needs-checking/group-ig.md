# Needs checking — Group IG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IG - one schedule filter rule changed, the others kept: `SET_SCHEDULE_FILTERS` and `REPORT_SCHEDULE_DEFINITION` version 2 (2026-10-21)

From [row 5b-1289](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21, on project
4355-BHVD-3D-22S10-BL001A in Revit 2020, a modeller duplicated a motorized fire damper schedule into
*DUCT HEATER SCHEDULE (FIRST FLOOR)* and *(ROOF FLOOR)*. `REPORT_SCHEDULE_DEFINITION` answered
*filterCount 4* over the two schedules and nothing more: one rule in each held the value "MFD", the other
was most likely a level, and no Heron read could say. He asked to change MFD to EDH. `SET_SCHEDULE_FILTERS`
clears every rule and writes one, so it would have wiped the level rule; he made both changes by hand.

**What changed** - both widened, not duplicated, every version 1 call running as it did:

| What | Where | What it does |
|---|---|---|
| `SET_SCHEDULE_FILTERS` v2 | [`set-schedule-filters`](../../brain/fragments/set-schedule-filters/fragment.yaml) | MODIFY. `filterAction` - **replace** (left out: version 1), **change** one rule's value and/or match type keeping every other rule, **add** a rule beside the others, **remove** one rule. A rule is picked by its column; two rules on one column are refused until `ruleNumber` says which. `filterRules` gives several at once (`column \| match type \| value; ...`). A changed rule pointing at an element - a Level rule - takes another element's NAME, of the same kind; a changed number rule is refused (no units API). Nothing is written until the whole new set is known, and a refusal from Revit puts the old set back. `rulesBefore` and `rulesAfter`: the whole set, read before and read back after, on one line each |
| `REPORT_SCHEDULE_DEFINITION` v2 | [`report-schedule-definition`](../../brain/fragments/report-schedule-definition/fragment.yaml) | READ. `filterRules`: every rule of every schedule, numbered as `ruleNumber` counts, the column by its printed heading, the match type in words, the value as Revit shows it - a level by name. Only the filter part of the report changed |

**What is tested and not run.** Both compile for Revit 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-10-21), and every Revit member they call is on all eight (`tools/api-surface --members`).
**Nothing here has run in Revit.** Version 1's proofs no longer describe the code (D-30), so both are
DRAFT: a kept `SET_SCHEDULE_FILTERS` change is refused until this group is signed ([D-117](../decisions/D-117.md)) -
its one `try-once` ([D-119](../decisions/D-119.md)) and rolled-back runs still run.
`REPORT_SCHEDULE_DEFINITION`, a read, still runs.

**Unknown until IG1:** whether *Base Constraint* in a wall schedule holds the level itself (an element
value) or its name. Revit's Filter tab offers a list of levels, which points at the first; `filterRules`
says which, and IG3 is the row that depends on it. If it holds text, record it here and run IG3 on a
schedule whose level rule does hold an element - the real project's duct heater schedules, IG9.

**Needs.** The add-in as it is - nothing in `revit/` changed, so no rebuild. Changes on. Git Bash from the
Heron folder. **A SCRATCH PROJECT, never a working model**: *Heron Schedule Filters.rvt*, from the default
metric template, **Revit 2020** first, SAVED after the rig. **Order: IG0, then IG1 to IG3, then the rest.**
`validate` takes its value options BEFORE the fragment's name (Group HM's lesson). Every `validate` below
is rolled back.

| # | Run | Look for |
|---|---|---|
| **IG0** | *The rig, KEPT, by hand.* Four *Generic - 200mm* walls: two on Level 1 with Mark **MFD** and **EDH**, two on Level 2 with Mark **MFD** and **EDH**. A Walls schedule **IG WALLS (LEVEL 1)**, fields Mark and Base Constraint, Filter tab: *Mark equals MFD* AND *Base Constraint equals Level 1*. Duplicate it (Duplicate View - Duplicate) and rename the copy **IG WALLS (LEVEL 2)** - its rules stay Level 1's, as the real copy's did. A third, **IG WALLS (TWO ON MARK)**: *Mark begins with M* AND *Mark ends with D*. Sheets **IG01 - LEVEL 1**, **IG02 - LEVEL 2** and **IG03 - TWO ON MARK**, each with its schedule placed. Save | IG WALLS (LEVEL 1) and (LEVEL 2) each show ONE row - the Level 1 MFD wall; TWO ON MARK shows both MFD walls. If a sheet's view is not found by that name in IG1, name the one used everywhere below |
| **IG1** | *The read, positive and negative.* `python tools/batch-prove.py tools/jobs/schedule-filter-rules-2026-10-21.yaml --dry-run`, then without `--dry-run` - its first job | Positive (IG01): `filterRules` *"'IG WALLS (LEVEL 1)': 1. Mark equals 'MFD'; 2. Base Constraint equals 'Level 1'"* - the level by NAME, the numbers in the Filter tab's order; the same rules in `findings`. Negative (the Level 1 walls): `filterRules` empty, `skippedNotSchedules` 2. **FAIL:** a number where the level should be, or rules numbered in another order than the Filter tab's |
| **IG2** | *The modeller's job, rolled back.* The same job file's second job | Positive (IG01): `rulesBefore` as IG1's rules; `rulesAfter` *"... 1. Mark equals 'EDH'; 2. Base Constraint equals 'Level 1'"* - **the level rule kept**; `replacedRules` *"'IG WALLS (LEVEL 1)' rule 1 was: Mark equals 'MFD'"*. **The D-30 negative** (IG03, both rules on Mark, no number): `cannotFilterBy` *"2 filter rules on 'IG WALLS (TWO ON MARK)' use 'Mark' - rules 1 and 2 - so which one was meant cannot be told..."*, `rulesAfter` empty, `filtered` 0. Afterwards IG1's read on IG03 shows its two rules unchanged |
| **IG3** | *The level rule, KEPT once.* Save, click the schedule on sheet IG02, then `python mcp/client/heron_bridge_client.py try-once --in "Heron Schedule Filters" set-schedule-filters --set filterAction=change --set "filterFieldName=Base Constraint" --set "filterValue=Level 2" < /dev/null`; look, then press **Keep** | `KEPT`. `rulesAfter` *"'IG WALLS (LEVEL 2)': 1. Mark equals 'MFD'; 2. Base Constraint equals 'Level 2'"*. On the sheet the schedule now shows the **Level 2** MFD wall; its Filter tab reads the same. IG1's read on IG02 says it too. ONE undo entry |
| **IG4** | *The refusals, rolled back, on IG01.* `validate --write` with the IG01 arrangement (`--setup select-by-category-name --setup set-selection --set "categoryName=Schedule Graphics" --set "inViewOnly=IG01 - LEVEL 1"`, the same as `--negative-set`) and `--set filterAction=change`, each of: **a** `--set filterFieldName=Comments --set filterValue=X`; **b** `--set "filterFieldName=Base Constraint" --set "filterValue=Level 9"`; **c** `--set filterFieldName=Mark --set ruleNumber=2 --set filterValue=X`; **d** `--set filterAction=roughly --set filterFieldName=Mark --set filterValue=X` | **a** `notPresent` *"'Comments' on 'IG WALLS (LEVEL 1)' - a schedule cannot filter on a field it does not carry..."*. **b** `notPresent` *"rule 2 ... points at a Level, and no Level is called 'Level 9'. Those in the model: Level 1, Level 2"*. **c** `cannotFilterBy` *"rule 2 ... is on Base Constraint, not on 'Mark' - the number and the column disagree..."*. **d** `refused` naming replace, change, add and remove, and `rulesBefore` empty. `rulesAfter` empty and the rules unchanged every time |
| **IG5** | *Add, then remove, rolled back.* On IG01: `--set filterAction=add --set filterFieldName=Mark --set "matchType=has value"`. On IG03: `--set filterAction=remove --set ruleNumber=2` | Add: `rulesAfter` three rules, the old two first and *"3. Mark has a value"* last. Remove: `rulesAfter` *"'IG WALLS (TWO ON MARK)': 1. Mark begins with 'M'"*, `replacedRules` naming the removed *Mark ends with 'D'* |
| **IG6** | *Several at once, rolled back, on IG01.* `--set filterAction=change --set "filterRules=Mark \| \| EDH; Base Constraint \| \| Level 2"` | `rulesAfter` *"1. Mark equals 'EDH'; 2. Base Constraint equals 'Level 2'"* - both match types kept. Then with `--set filterFieldName=Mark` added: `refused` *"a column was named on its own AND a list of rules was given..."* |
| **IG7** | *Version 1's call, unchanged, on IG01.* `--set filterFieldName=Mark --set "filterValue=" --set "matchType=has value"` and no `filterAction` - version 1's own arrangement in [`reprove-refusals.yaml`](../../tools/jobs/reprove-refusals.yaml) | `rulesAfter` *"1. Mark has a value"* - ONE rule; `replacedRules` naming both old rules. Version 1's answer shape: `filtered` 1, `appliedRules` one entry |
| **IG8** | *A refusal from Revit puts the old set back, on IG01.* `--set "filterFieldName=Base Constraint" --set "filterValue=Level 2" --set matchType=equals`, no `filterAction` - a NEW level rule written as text | If IG1 showed an element value: `cannotFilterBy` quoting Revit's refusal and ending *"The rules it had were put back."*; IG1's read shows the two old rules. Version 1 left this schedule with no rules. If Revit accepts the text, record what `rulesAfter` says it holds - a finding for this group either way |
| **IG9** | *The real project, READ ONLY - the observation, not the proof.* `revit_read` with `REPORT_SCHEDULE_DEFINITION` on 4355-BHVD-3D-22S10-BL001A, the two DUCT HEATER schedules clicked on their sheets | `filterRules` names both rules of each schedule - the column holding EDH and the level rule by its level's name. The answer to *"what was the other rule"* |
| **IG10** | *Signing, after IG1 to IG3.* `python brain/heron_validate.py draft <fragment> --from brain/proof-drafts/runs/<fragment>.json` for each, read against IG1 to IG3, then `accept --by "Ajmal PS"` | The proof blocks written - `SET_SCHEDULE_FILTERS`' positive IG3's kept run (D-120), its negative IG2's. **`heron-status: PROVEN` is a separate edit, the owner's** |
| **IG11** | *The other releases.* IG1 and IG2 on Revit 2024 and on 2027 (`batch-prove --release`, D-131) | The same answers. A difference is a finding for this group, not a pass |
| **IG12** | *Second route.* Revit's schedule Properties, Filter tab, opened beside every read-back above | What the answers said, rule for rule and in the same order |
