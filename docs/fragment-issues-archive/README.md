# FRAGMENT-ISSUES archive

**Finished rows from sections 5 and 5b of [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md). Nothing
here is a live obligation, and nothing here is specification.**

> This page is written by [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py)
> every time it moves rows. **Change the tool, not this page** — an edit here is gone on the next
> run.

## Why this folder exists

On 2026-09-22 the register was 1,123,145 bytes — more than a session can hold at once —
and sections 5 and 5b, one paragraph per defect, were 83% of it. Most of those rows were
finished. A session sent to the queue could not read the queue, so it grepped, and every grep
returned the finished rows beside the ones still owed. Finished rows now live here, and the
register keeps one line for each.

`HANDOVER.md` met the same problem first, and [`../handover-archive/`](../handover-archive/README.md)
is the same answer for session notes.

## Finding a row

**Start at the register.** Every moved row kept its line there, with the same number and a link
straight to its full text here.

A row cited as **row 107** is in section 5, and one cited as **row 5b-62** is in section 5b. Each
file holds one fixed band of 25 row numbers and is named after it, so the file is known before it
is opened:

| Section | Rows | File |
|---|---|---|
| 5 — found by proving | 1 to 25 | [`proving-defects-001-025.md`](proving-defects-001-025.md) |
| 5 — found by proving | 26 to 50 | [`proving-defects-026-050.md`](proving-defects-026-050.md) |
| 5 — found by proving | 51 to 75 | [`proving-defects-051-075.md`](proving-defects-051-075.md) |
| 5 — found by proving | 76 to 100 | [`proving-defects-076-100.md`](proving-defects-076-100.md) |
| 5 — found by proving | 101 to 125 | [`proving-defects-101-125.md`](proving-defects-101-125.md) |
| 5 — found by proving | 126 to 150 | [`proving-defects-126-150.md`](proving-defects-126-150.md) |
| 5 — found by proving | 151 to 175 | [`proving-defects-151-175.md`](proving-defects-151-175.md) |
| 5 — found by proving | 176 to 200 | [`proving-defects-176-200.md`](proving-defects-176-200.md) |
| 5b — found by reading | 1 to 25 | [`reading-defects-5b-001-025.md`](reading-defects-5b-001-025.md) |
| 5b — found by reading | 26 to 50 | [`reading-defects-5b-026-050.md`](reading-defects-5b-026-050.md) |
| 5b — found by reading | 51 to 75 | [`reading-defects-5b-051-075.md`](reading-defects-5b-051-075.md) |
| 5b — found by reading | 76 to 100 | [`reading-defects-5b-076-100.md`](reading-defects-5b-076-100.md) |
| 5b — found by reading | 1001 to 1025 | [`reading-defects-5b-1001-1025.md`](reading-defects-5b-1001-1025.md) |
| 5b — found by reading | 101 to 125 | [`reading-defects-5b-101-125.md`](reading-defects-5b-101-125.md) |
| 5b — found by reading | 1026 to 1050 | [`reading-defects-5b-1026-1050.md`](reading-defects-5b-1026-1050.md) |
| 5b — found by reading | 1051 to 1075 | [`reading-defects-5b-1051-1075.md`](reading-defects-5b-1051-1075.md) |
| 5b — found by reading | 1076 to 1100 | [`reading-defects-5b-1076-1100.md`](reading-defects-5b-1076-1100.md) |
| 5b — found by reading | 1101 to 1125 | [`reading-defects-5b-1101-1125.md`](reading-defects-5b-1101-1125.md) |
| 5b — found by reading | 1126 to 1150 | [`reading-defects-5b-1126-1150.md`](reading-defects-5b-1126-1150.md) |
| 5b — found by reading | 1151 to 1175 | [`reading-defects-5b-1151-1175.md`](reading-defects-5b-1151-1175.md) |
| 5b — found by reading | 1176 to 1200 | [`reading-defects-5b-1176-1200.md`](reading-defects-5b-1176-1200.md) |
| 5b — found by reading | 1201 to 1225 | [`reading-defects-5b-1201-1225.md`](reading-defects-5b-1201-1225.md) |
| 5b — found by reading | 1226 to 1250 | [`reading-defects-5b-1226-1250.md`](reading-defects-5b-1226-1250.md) |
| 5b — found by reading | 1251 to 1275 | [`reading-defects-5b-1251-1275.md`](reading-defects-5b-1251-1275.md) |
| 5b — found by reading | 126 to 150 | [`reading-defects-5b-126-150.md`](reading-defects-5b-126-150.md) |
| 5b — found by reading | 1276 to 1300 | [`reading-defects-5b-1276-1300.md`](reading-defects-5b-1276-1300.md) |
| 5b — found by reading | 1301 to 1325 | [`reading-defects-5b-1301-1325.md`](reading-defects-5b-1301-1325.md) |
| 5b — found by reading | 1326 to 1350 | [`reading-defects-5b-1326-1350.md`](reading-defects-5b-1326-1350.md) |
| 5b — found by reading | 1351 to 1375 | [`reading-defects-5b-1351-1375.md`](reading-defects-5b-1351-1375.md) |
| 5b — found by reading | 1376 to 1400 | [`reading-defects-5b-1376-1400.md`](reading-defects-5b-1376-1400.md) |
| 5b — found by reading | 1401 to 1425 | [`reading-defects-5b-1401-1425.md`](reading-defects-5b-1401-1425.md) |
| 5b — found by reading | 1426 to 1450 | [`reading-defects-5b-1426-1450.md`](reading-defects-5b-1426-1450.md) |
| 5b — found by reading | 1451 to 1475 | [`reading-defects-5b-1451-1475.md`](reading-defects-5b-1451-1475.md) |
| 5b — found by reading | 1476 to 1500 | [`reading-defects-5b-1476-1500.md`](reading-defects-5b-1476-1500.md) |
| 5b — found by reading | 1501 to 1525 | [`reading-defects-5b-1501-1525.md`](reading-defects-5b-1501-1525.md) |
| 5b — found by reading | 151 to 175 | [`reading-defects-5b-151-175.md`](reading-defects-5b-151-175.md) |
| 5b — found by reading | 1526 to 1550 | [`reading-defects-5b-1526-1550.md`](reading-defects-5b-1526-1550.md) |
| 5b — found by reading | 1551 to 1575 | [`reading-defects-5b-1551-1575.md`](reading-defects-5b-1551-1575.md) |
| 5b — found by reading | 1576 to 1600 | [`reading-defects-5b-1576-1600.md`](reading-defects-5b-1576-1600.md) |
| 5b — found by reading | 1601 to 1625 | [`reading-defects-5b-1601-1625.md`](reading-defects-5b-1601-1625.md) |
| 5b — found by reading | 1626 to 1650 | [`reading-defects-5b-1626-1650.md`](reading-defects-5b-1626-1650.md) |
| 5b — found by reading | 1651 to 1675 | [`reading-defects-5b-1651-1675.md`](reading-defects-5b-1651-1675.md) |
| 5b — found by reading | 1701 to 1725 | [`reading-defects-5b-1701-1725.md`](reading-defects-5b-1701-1725.md) |
| 5b — found by reading | 1726 to 1750 | [`reading-defects-5b-1726-1750.md`](reading-defects-5b-1726-1750.md) |
| 5b — found by reading | 1751 to 1775 | [`reading-defects-5b-1751-1775.md`](reading-defects-5b-1751-1775.md) |
| 5b — found by reading | 176 to 200 | [`reading-defects-5b-176-200.md`](reading-defects-5b-176-200.md) |
| 5b — found by reading | 1776 to 1800 | [`reading-defects-5b-1776-1800.md`](reading-defects-5b-1776-1800.md) |
| 5b — found by reading | 1801 to 1825 | [`reading-defects-5b-1801-1825.md`](reading-defects-5b-1801-1825.md) |
| 5b — found by reading | 1826 to 1850 | [`reading-defects-5b-1826-1850.md`](reading-defects-5b-1826-1850.md) |
| 5b — found by reading | 201 to 225 | [`reading-defects-5b-201-225.md`](reading-defects-5b-201-225.md) |
| 5b — found by reading | 226 to 250 | [`reading-defects-5b-226-250.md`](reading-defects-5b-226-250.md) |
| 5b — found by reading | 251 to 275 | [`reading-defects-5b-251-275.md`](reading-defects-5b-251-275.md) |
| 5b — found by reading | 276 to 300 | [`reading-defects-5b-276-300.md`](reading-defects-5b-276-300.md) |
| 5b — found by reading | 301 to 325 | [`reading-defects-5b-301-325.md`](reading-defects-5b-301-325.md) |
| 5b — found by reading | 326 to 350 | [`reading-defects-5b-326-350.md`](reading-defects-5b-326-350.md) |
| 5b — found by reading | 351 to 375 | [`reading-defects-5b-351-375.md`](reading-defects-5b-351-375.md) |
| 5b — found by reading | 376 to 400 | [`reading-defects-5b-376-400.md`](reading-defects-5b-376-400.md) |
| 5b — found by reading | 401 to 425 | [`reading-defects-5b-401-425.md`](reading-defects-5b-401-425.md) |
| 5b — found by reading | 426 to 450 | [`reading-defects-5b-426-450.md`](reading-defects-5b-426-450.md) |
| 5b — found by reading | 451 to 475 | [`reading-defects-5b-451-475.md`](reading-defects-5b-451-475.md) |
| 5b — found by reading | 476 to 500 | [`reading-defects-5b-476-500.md`](reading-defects-5b-476-500.md) |
| 5b — found by reading | 501 to 525 | [`reading-defects-5b-501-525.md`](reading-defects-5b-501-525.md) |
| 5b — found by reading | 626 to 650 | [`reading-defects-5b-626-650.md`](reading-defects-5b-626-650.md) |
| 5b — found by reading | 651 to 675 | [`reading-defects-5b-651-675.md`](reading-defects-5b-651-675.md) |
| 5b — found by reading | 851 to 875 | [`reading-defects-5b-851-875.md`](reading-defects-5b-851-875.md) |
| 5b — found by reading | 876 to 900 | [`reading-defects-5b-876-900.md`](reading-defects-5b-876-900.md) |
| 5b — found by reading | 926 to 950 | [`reading-defects-5b-926-950.md`](reading-defects-5b-926-950.md) |
| 5b — found by reading | 976 to 1000 | [`reading-defects-5b-976-1000.md`](reading-defects-5b-976-1000.md) |

A number with no entry in its file was never moved: it is still in the register, in full.

How many rows each file holds is derived, not typed:

```bash
grep -c '^### Row' docs/fragment-issues-archive/*.md
```

## What moves, and what stays

A row moves only when its state begins with a closing word — FIXED, CLOSED, CLEARED, SOLVED, REPAIRED, ANSWERED, WITHDRAWN, SUPERSEDED, NOT A DEFECT, GUARD FIXED —
with nothing at its start that qualifies the claim, such as PARTLY, PARTIALLY, MOSTLY, and nothing
anywhere in it that says something is still owed, such as STILL OPEN, NOT FIXED, NOT YET, AWAITING,
OWNER'S CALL or REOPENED. The whole state is read for those, because a row closed in place usually
writes what is left at the end. An open row never moves, and neither does a row whose state the rule
does not recognise: guessing that a row is finished is how a register loses a row it still owes. The
full rule, and the checks made before anything is written, are in the tool's own docstring.

To move the rows that have closed since:

```bash
python tools/archive-fragment-issues.py            # what would move - writes nothing
python tools/archive-fragment-issues.py --write
```
