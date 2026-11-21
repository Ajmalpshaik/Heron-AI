# Needs checking — Group PD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PD - row 106: is a clearance check slow on a real MEP model? One timed run before any fix is written (2026-11-21)

**Tracked as GitHub issue [#1172](https://github.com/Ajmalpshaik/Heron-AI/issues/1172) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** row [106](../fragment-issues/section-5-rows-101-125.md), GitHub issue
[#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948). `CHECK_MINIMUM_CLEARANCE`,
`CHECK_VERTICAL_CLEARANCE` and `FIND_NEAREST_ELEMENTS` measure every element against every target with two
nested loops and no spatial filter (`CHECK_INSULATION_CLEARANCE` is retired into the first). Row 106's
measurement of 2026-09-16 ran on *test projject*, where only 98 elements had geometry, and both runs took
about 1.5-2 s - process start-up, not the loop. `CHECK_MINIMUM_CLEARANCE`'s proof of 2026-11-02 on *P1102
MEP* checked only 45 pairs. So **nobody has timed it at a size where it could be slow**, and a fix sends
two PROVEN cards and one DRAFT back to DRAFT - the reason the row waits for this run. Since its version 3
the card also keeps one text key per pair measured, so memory grows with the pairs as well as time. Read
in a cloud container (no Revit), 2026-11-21; nothing here has run.

**The model is a scratch copy, never a working model** - *P1102 MEP*, Revit 2020 first. The cards only
read; nothing is kept. Write down the run's time as Heron's reply or its log gives it.

| ID | Do this | Pass looks like |
|---|---|---|
| **PD1** | *`CHECK_MINIMUM_CLEARANCE` v4, the whole model's ducts and pipes.* Every Duct and Pipe of *P1102 MEP* (`FILTER_ELEMENTS_BY_CATEGORY` with both categories) handed in as `elements` AND as `targets`, `defaultClearance=50`, `includeInsulation=false`, `ignoreConnected=true`, `maxRows=20`. Write down how many elements went in, `pairsChecked`, and the seconds the run took | The three numbers written down, and Revit still answering after it. `pairsChecked` is at most n x (n - 1) / 2 for n elements that have a box. **This answers row 106:** the owner reads the seconds and says whether it is slow enough to fix - then the fix (a spatial filter grown by the clearance, so pairs that do not touch yet are still found) is written for the three cards. Revit stopped answering, or the run timed out, is the row confirmed |
| **PD2** | *The same, negative - one duct.* `elements` = ONE duct of the same system, `targets` = every Duct and Pipe as in PD1 | `pairsChecked` equals the targets that have a box less the duct itself (each pair counted once), and the seconds near a small run's - so PD1's figure is the loop's cost and not start-up. A `pairsChecked` above that count is a FAIL: the count would not be measuring the pairs |

**Not here:** the fix. Row 106 stays open in issue #948 until PD1 is read; nothing in any card changed.
