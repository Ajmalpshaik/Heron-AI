# Fragment issues — section 5b, rows 401 to 425

> Rows 401 to 425 of section 5b of [the register](../FRAGMENT-ISSUES.md), in their own file since 2026-10-09 so that
> they can be read alone. **The section's own rules are on that page, under its heading.** A new
> row goes at the end of the section's last file, whatever its number, and the next run of
> [`tools/split-register.py`](../../tools/split-register.py) moves it to the file its number
> belongs to. [`tools/register-text.py`](../../tools/register-text.py) reads every row back into
> the register for every tool that reads it, so a row here is seen exactly as it was seen there.

| # | Defect | State |
|---|---|---|
| **402** | **`tests/test_mcp_serves.py` DOES NOT RETURN INSIDE `check-gaps`' 300-SECOND CEILING IN A CLOUD CONTAINER, SO `check-gaps` LISTS IT UNFINISHED.** Found 2026-10-09 running `python tools/check-gaps.py` end to end for the first time in the [project review](../work-notes/plans/project-review-2026-10-08.md) (at low priority, beside two other agents' builds, on 4 CPUs): *HUNG test_mcp_serves.py - still running after 300s, gave up*. Timed side by side under the same load: `main` at 661384f6 took 445 s and this branch 562 s, both exit 0. So the suite was already over the ceiling before this branch's new sections (row 5b-233's lookup, row 5b-274's activity list) were added; they add about a quarter. `SUITE_TIMEOUT` in [`tools/check-gaps.py`](../../tools/check-gaps.py) says 300 s is about four times the slowest suite measured there, 78 s on 2026-09-10. NOT measured on an unloaded machine or on the owner's PC. | **OPEN - recorded, not fixed.** Either the suite is made faster or `check-gaps` gives it a ceiling of its own, and the choice is for whoever owns that rule; raising the one ceiling for every suite would let a hung suite wait longer before it is named. |
