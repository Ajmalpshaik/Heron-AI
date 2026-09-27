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
| **AW3** | **Re-sign the Sheet Agent.** Its proof, [`brain/agent-proofs/HERON-REVIT-SHT-029.yaml`](../../brain/agent-proofs/HERON-REVIT-SHT-029.yaml), was signed 2026-09-19 against the code before this change, and `python tools/prove-agent.py check` now calls it STALE. Re-run `tools/prove-agent.py track` for HERON-REVIT-SHT-029 across two open models | A new proof whose fingerprint matches the changed file. Two models where the schedule count differs carry more than the three numbers the old proof moved |
