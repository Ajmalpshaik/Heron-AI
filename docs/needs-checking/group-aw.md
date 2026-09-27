# Needs checking — Group AW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-27 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AW - `revit_sheets` counts schedules: a sheet carrying only a schedule is not empty (2026-09-27)

**The owner's PC, Revit 2024, after the add-in is redeployed and Revit restarted.** Until then Revit runs
the old add-in and none of this can be seen. [Row 5b-238](../FRAGMENT-ISSUES.md#): the Sheet Agent
([`RevitSheets.cs`](../../revit/Heron.Revit.Addin/RevitSheets.cs), HERON-REVIT-SHT-029) counted only the
views on a sheet, so M-105, M-106 and M-107 in "heron ai bulding" - each carrying one schedule - were
reported as having nothing on them. It now counts schedules too, and leaves the titleblock's own revision
schedule out.

**What is already known, and it is NOT a proof.** It compiles on every release from 2020 to 2027, and
[`tests/test_sheet_contents.py`](../../tests/test_sheet_contents.py) runs the lines `revit_sheets` prints
and reads the add-in's count as text. Nothing has asked Revit's collector which schedules sit on which
sheet.

| # | Check | Expected |
|---|---|---|
| **AW1** | **The positive, read-only, on the working model.** Open "heron ai bulding" in Revit 2024 and ask *list the sheets* (`revit_sheets`). Nothing is changed, so the working model is fine | M-105, M-106 and M-107 each read `0 view(s), 1 schedule(s)` with **no** `NOTHING ON IT`, and the *"nothing placed on them"* warning no longer counts them. M-101 to M-104 still read `1 view(s)`. If the line says *"schedules not counted by this add-in"*, Revit is still running the old add-in - redeploy and restart, then ask again |
| **AW2** | **The negative - the revision schedule must not count.** On a **test copy**, never the working model: make one new sheet with the same titleblock as M-105 (check first that it shows a revision block), place nothing on it, and ask *list the sheets* | That sheet reads `0 view(s), 0 schedule(s)   <-- NOTHING ON IT`. If it reads `1 schedule(s)`, the titleblock's revision schedule is being counted and every empty sheet in a set will look full - the fix has failed its reason for existing |
| **AW3** | **Re-sign the Sheet Agent.** Its proof, [`brain/agent-proofs/HERON-REVIT-SHT-029.yaml`](../../brain/agent-proofs/HERON-REVIT-SHT-029.yaml), was signed 2026-09-19 against the code before this change, and `python tools/prove-agent.py check` now calls it STALE. Re-run `tools/prove-agent.py track` for HERON-REVIT-SHT-029 across two open models | A new proof whose fingerprint matches the changed file. Two models where the schedule count differs carry more than the three numbers the old proof moved. **Sign it after 5b-239's change is in as well** - that change edits the same file, so a proof signed before it goes stale again |

**AW4 to AW6 - the same count in `revit_export_check`** ([row 5b-239](../FRAGMENT-ISSUES.md#)). The Export
Agent ([`RevitExport.cs`](../../revit/Heron.Revit.Addin/RevitExport.cs), HERON-REVIT-EXP-018) counted a sheet
that "would print BLANK" by its views alone, so the same three sheets would have been among them. It now
calls the Sheet Agent's own schedule count, so the two answer one question by one rule. Same restart as
AW1: until it, Revit runs the old add-in. Known so far, and NOT a proof: it compiles on 2020 to 2027, and
[`tests/test_export_blank_sheets.py`](../../tests/test_export_blank_sheets.py) runs the lines
`revit_export_check` prints and reads the add-in's rule as text.

| # | Check | Expected |
|---|---|---|
| **AW4** | **The positive, read-only, on the working model.** In the same minute as AW1, ask *is this model ready to export* (`revit_export_check`) on "heron ai bulding". Nothing is changed | `would print blank` does not count M-105, M-106 or M-107, and equals the N in AW1's *"N sheet(s) have nothing placed on them"* finding (0 when AW1 has no such finding; the sheet list stops at 30, the finding does not) - the two agents now share one rule, so any difference is a defect. If the line says *"views only; schedules not counted by this add-in"*, Revit is still running the old add-in - redeploy and restart, then ask again |
| **AW5** | **The negative - the revision schedule must not count here either.** On AW2's **test copy**, with AW2's empty sheet in place, ask `revit_export_check` | `would print blank` counts that sheet - one more than before it was made. If it does not, the titleblock's revision schedule is being counted and an empty sheet will go out in an issued set |
| **AW6** | **Re-sign the Export Agent.** Its proof, [`brain/agent-proofs/HERON-REVIT-EXP-018.yaml`](../../brain/agent-proofs/HERON-REVIT-EXP-018.yaml), was signed 2026-09-18 against the code before this change, and `python tools/prove-agent.py check` now calls it STALE. Re-run `tools/prove-agent.py track` for HERON-REVIT-EXP-018 across two open models | A new proof whose fingerprint matches the changed file. A pair where one model has a sheet carrying only a schedule is the one that shows the new rule |
