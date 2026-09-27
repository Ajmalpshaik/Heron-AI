# Needs checking — Group AT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AT - `set-schedule-field-totals`: "Calculate totals" switched on for a schedule's columns (2026-09-27)

**One fragment from one sitting on 2026-09-27, in Project1 on Revit 2024, and it is DRAFT.**
[`set-schedule-field-totals`](../../brain/fragments/set-schedule-field-totals/fragment.yaml) -
SET_SCHEDULE_FIELD_TOTALS, FRG-VIEW-109 - switches on "Calculate totals" for named columns of a schedule, the
drop-down on the schedule's Formatting tab, so each group footer and the grand total add the column up. It
compiles on every release from 2020 to 2027. It has run once, in a chat, on the owner's working model, and
**a chat run is not a proof**: no fingerprint was taken and nothing was rolled back ([D-30](../DECISIONS.md)),
so no row below is closed by it.

**Why it exists.** The Air Terminal Schedule in "heron ai bulding" is grouped by Room: Name with a header and
a footer, and its Flow column had no total: a footer adds up only the columns whose calculation is set, and
nothing in the library set one. *"calculate totals for the flow column"* resolved to `MEASURE_ELEMENT_VOLUME`
and *"sum this column in the schedule"* to `SET_SCHEDULE_APPEARANCE`.

**The working run, 2026-09-27 - a chat run, not a proof.** "heron ai bulding", Revit 2024, Heron session
46596, Changes on. Each call was a `revit_read` FIND_SCHEDULES `nameContains=Air Terminal Schedule`, which
found that one schedule, then `revit_change` SET_SCHEDULE_FIELD_TOTALS with `expect_from` naming it:

1. `fieldNames=Family and Type` - `totalled` 0, `cannotTotal` 1 naming it. Nothing set.
2. `fieldNames=Flow` - `totalled` 1, `replacedCalculation` empty, so Flow had "No calculation" before. **Kept.**
3. `fieldNames=Flow` again - `totalled` 0, `alreadyTotalled` 1. This is the read-back: nothing else in the
   library reads a column's calculation, and REPORT_SCHEDULE_DEFINITION, run between 2 and 3, reported the
   schedule sorted by Room: Name ascending *with header + footer*.

Heron read its own write; Revit's Formatting tab and the printed footers were not looked at by the session,
so AT2 is still owed. **The reply cut the sentence saying where the total prints** at about sixty characters,
so `appliedTotals`' *"prints in each ... footer and in the grand total"* was not seen in the chat either.

Every row runs on a TEST COPY of "heron ai bulding" - never the working model.

| # | Check | Expected |
|---|---|---|
| **AT1** | The proof ([D-30](../DECISIONS.md)). **A copy saved after 2026-09-27 already totals Flow**, so first, on the test copy only: Air Terminal Schedule, Properties, Formatting, select Flow, set the calculation to "No calculation", OK. Then, with `HERON_CLIENT_ID=ajmal-pc` set and the test copy in front, `python tools/batch-prove.py tools/jobs/set-schedule-field-totals-project1-2026-09-27.yaml --dry-run`, then the same without `--dry-run`. Then read the draft, sign it with `python brain/heron_validate.py accept set-schedule-field-totals --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive: `totalled` 1 and `replacedCalculation` empty. The negative - `Family and Type` on the same schedule: `totalled` 0, `cannotTotal` naming it and `notPresent` empty. `POSITIVE EMPTY` with `alreadyTotalled` 1 means the arrangement was skipped. Afterwards the Formatting tab reads "No calculation" for Flow again - both legs are rolled back |
| **AT2** | The second route - Revit's own Formatting tab and the printed schedule, on the WORKING model after the chat run, or on the test copy after the positive leg run KEPT in a chat | The Formatting tab reads "Calculate totals" for Flow and "No calculation" for Family and Type. Opened, the schedule shows a summed flow in each room's footer and, if the grand total is on, at the bottom - **read on screen, not inferred from `totalled`** |
| **AT3** | A column showing a minimum or maximum. On the test copy, set Flow's calculation to "Calculate maximum" by hand, then in a chat: FIND_SCHEDULES `nameContains=Air Terminal Schedule`, then SET_SCHEDULE_FIELD_TOTALS `fieldNames=Flow` | `totalled` 1 and `replacedCalculation` naming *"its maximum"* - the old calculation reported, not silently lost - and the Formatting tab reading "Calculate totals". One Ctrl+Z takes it back to "Calculate maximum" |
