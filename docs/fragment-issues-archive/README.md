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
| 5b — found by reading | 1 to 25 | [`reading-defects-5b-001-025.md`](reading-defects-5b-001-025.md) |
| 5b — found by reading | 26 to 50 | [`reading-defects-5b-026-050.md`](reading-defects-5b-026-050.md) |
| 5b — found by reading | 51 to 75 | [`reading-defects-5b-051-075.md`](reading-defects-5b-051-075.md) |
| 5b — found by reading | 76 to 100 | [`reading-defects-5b-076-100.md`](reading-defects-5b-076-100.md) |
| 5b — found by reading | 101 to 125 | [`reading-defects-5b-101-125.md`](reading-defects-5b-101-125.md) |
| 5b — found by reading | 126 to 150 | [`reading-defects-5b-126-150.md`](reading-defects-5b-126-150.md) |

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
