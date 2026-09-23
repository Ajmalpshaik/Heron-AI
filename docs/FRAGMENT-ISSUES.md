<!-- Heron-Agent:  none -->
<!-- Heron-Step:   17 -->
<!-- Heron-Status: DRAFT -->
<!-- Heron-Since:  0.1.0 -->
<!-- Heron-Layer:  brain -->
<!-- See docs/29-metadata-standard.md -->

# Fragments with something wrong — the sit-down list

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone proving fragments — **this file is the queue** |
> | **Authority** | [DECISIONS](DECISIONS.md) and the [Golden Rules](14-golden-rules.md) win. A row here records what was seen, on the day it was seen |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | One row per fragment or defect, with **the model it was seen on named**. A defect found while tidying is **recorded, not fixed** |
> | **Its numbers** | Counts inside a row describe **the day it was written** and are deliberately not updated. Derive today's: `grep -rh '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c` |
> | **Where each section lives** | Since 2026-09-23 **each section is its own file** in [`fragment-issues/`](fragment-issues/section-1.md) — [section 1's](fragment-issues/section-1.md), for one — and this page keeps the rules and, where each section was, its heading and a line naming its file. **Sections 5 and 5b keep their own words here, and their rows are in files of 25 by number:** row 5b-62 is in `fragment-issues/section-5b-rows-051-075.md`. **A new row goes at the end of its section's last file.** The whole register, read as one text: `python tools/register-text.py docs/FRAGMENT-ISSUES.md` |
> | **Finished rows** | A finished row of section 5 or 5b keeps its line in its rows file — its number, a one-line title and the opening of its state — and its full text moves to [`fragment-issues-archive/`](fragment-issues-archive/README.md). **Nothing is deleted, and an open row always stays in full.** `python tools/archive-fragment-issues.py` says what would move |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


**What this is.** Every fragment that was PUT IN FRONT OF A REAL MODEL and did not come away proved,
with the reason. Opened 2026-09-08 at the owner's request: *"we will sit for this specially, that
issued one we can do together."*

**What this is NOT.** It is not the list of unproven fragments — that is 301 and most of them have
simply not been tried yet. Everything here has been RUN. A fragment earns a row by failing, refusing,
or passing in a way that proves nothing.

**How to use it.** Read `Status` first. `NEEDS THE OWNER` means the model has to be arranged by hand
and nothing else will do. `SUSPECT` means it did something to Revit that has not been explained and it
should not be run again casually.

Derive the counts rather than trusting any typed here:

```bash
grep -h '^heron-status:' brain/fragments/*/fragment.yaml | sort | uniq -c
```

---

## 1. SUSPECT — these upset Revit, and why is not known

**Its own file:** [`fragment-issues/section-1.md`](fragment-issues/section-1.md)

## 1c. A WRITE INTERRUPTED BY A DIALOG DOES NOT FULLY ROLL BACK — 2026-09-08

**Its own file:** [`fragment-issues/section-1c.md`](fragment-issues/section-1c.md)

## 1b. NEEDS A HUMAN AT THE KEYBOARD — Revit opens a dialog Heron cannot answer

**Its own file:** [`fragment-issues/section-1b.md`](fragment-issues/section-1b.md)

## 1d. A ROLLED-BACK WRITE CLEARS THE SELECTION — and that blocks 100 proofs

**Its own file:** [`fragment-issues/section-1d.md`](fragment-issues/section-1d.md)

## 2. CANNOT PROVE — Revit itself declines

**Its own file:** [`fragment-issues/section-2.md`](fragment-issues/section-2.md)

## 3. NEEDS THE OWNER — the model has to be arranged by hand

**Its own file:** [`fragment-issues/section-3.md`](fragment-issues/section-3.md)

## 3b. NO POSITIVE CASE IN THIS MODEL — found 2026-09-08, second round

**Its own file:** [`fragment-issues/section-3b.md`](fragment-issues/section-3b.md)

## 3b-i. THE `POSITIVE EMPTY` POOL IS EXHAUSTED FOR THIS MODEL — triaged 2026-09-10

**Its own file:** [`fragment-issues/section-3b-i.md`](fragment-issues/section-3b-i.md)

## 3b-ii. A SECOND MODEL CLEARS A BLOCK THAT WAS NEVER ABOUT THE FRAGMENT — 2026-09-10

**Its own file:** [`fragment-issues/section-3b-ii.md`](fragment-issues/section-3b-ii.md)

## 3b-iii. THE SNOWDON ROUND THAT WAS WRITTEN AND NEVER RUN — 2026-09-10

**Its own file:** [`fragment-issues/section-3b-iii.md`](fragment-issues/section-3b-iii.md)

## 3g-iii. A THIRTY-FRAGMENT SWEEP, AND WHAT IT SEGREGATED — 2026-09-10

**Its own file:** [`fragment-issues/section-3g-iii.md`](fragment-issues/section-3g-iii.md)

## 3c. THE NEGATIVE CASE HAS NOT BEEN FOUND YET — 2026-09-08, third round

**Its own file:** [`fragment-issues/section-3c.md`](fragment-issues/section-3c.md)

## 3d. GAPS — what proving showed is MISSING, not broken

**Its own file:** [`fragment-issues/section-3d.md`](fragment-issues/section-3d.md)

## 3e. THE SCHEDULE WALL HAS A DOOR — found 2026-09-09

**Its own file:** [`fragment-issues/section-3e.md`](fragment-issues/section-3e.md)

## 3f. PROVED, BUT QUERIED — worth a second look at the sit-down

**Its own file:** [`fragment-issues/section-3f.md`](fragment-issues/section-3f.md)

## 3g. RUNNING FRAGMENTS IN BULK IS A WORKING NEED, NOT A TESTING ONE

**Its own file:** [`fragment-issues/section-3g.md`](fragment-issues/section-3g.md)

## 3g-ii. WHAT BLOCKS THE REMAINING 172, DERIVED RATHER THAN ASSUMED — 2026-09-10

**Its own file:** [`fragment-issues/section-3g-ii.md`](fragment-issues/section-3g-ii.md)

## 3g-iv. SIX SWEEPS LATER: THE ARRANGEABLE POOL IS EXHAUSTED — 2026-09-11

**Its own file:** [`fragment-issues/section-3g-iv.md`](fragment-issues/section-3g-iv.md)

## 3h. FOUR THINGS THAT WOULD IMPROVE THIS, RANKED — 2026-09-09

**Its own file:** [`fragment-issues/section-3h.md`](fragment-issues/section-3h.md)

## 3i. THE ELEMENT-SHAPED WALL — found 2026-09-09, second sitting

**Its own file:** [`fragment-issues/section-3i.md`](fragment-issues/section-3i.md)

## 3j. THE REFUSALS OF §3h.1, IN FRONT OF A MODEL — 2026-09-09

**Its own file:** [`fragment-issues/section-3j.md`](fragment-issues/section-3j.md)

## 4. FIXED during proving — kept because the shape returns

**Its own file:** [`fragment-issues/section-4.md`](fragment-issues/section-4.md)

## 5. HERON'S OWN DEFECTS found by proving

> **This heading said *"six still open"* from the day it was written until 2026-09-16, and by then it was TWENTY-NINE.** Ninety-four rows were appended under a heading nobody re-read - the prose-total drift [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) records against itself, happening here too. **No number is typed here now. Derive it:**
>
> ```bash
> python tools/open-defects.py
> ```

**Its rows, in order:** [001-025](fragment-issues/section-5-rows-001-025.md) · [026-050](fragment-issues/section-5-rows-026-050.md) · [051-075](fragment-issues/section-5-rows-051-075.md) · [076-100](fragment-issues/section-5-rows-076-100.md) · [101-125](fragment-issues/section-5-rows-101-125.md) · [126-150](fragment-issues/section-5-rows-126-150.md) · [151-175](fragment-issues/section-5-rows-151-175.md) · [176-200](fragment-issues/section-5-rows-176-200.md)

---

## 5b. HERON'S OWN DEFECTS found by reading the repository, file by file

> Section 5 holds what **proving against a model** found. This holds what **reading** found — the word-by-word sweep of every tracked file that is not `brain/**.yaml`, recorded in [`REVIEW-LEDGER.tsv`](REVIEW-LEDGER.tsv). **How many that is, and how many have been read, is derived and not typed here.**
>
> **They are kept apart because they are found differently and they go stale differently.** A proving defect is re-tested by running the fragment again. A reading defect is re-tested by reading the file again — and the ledger knows when that is necessary, because the file's content hash stopped matching the one recorded when it was read.
>
> **No number is typed here.** Derive it:
>
> ```bash
> python tools/open-defects.py          # both sections, by id
> python tools/review-ledger.py         # how much of the repository has been read
> ```

**THE SWEEP RECORDS, IT DOES NOT REPAIR.** A file found wrong is marked `issue` in the ledger and written up here; the file itself is left alone. A sweep that also fixes things stops being a sweep — the reader loses track of which files have actually been read, and a repair made in passing is a change nobody reviewed. Anything big enough to need its own sitting gets **`OPEN — needs a sitting`** and waits there.

**A ROW HERE IS WRITTEN FOR A SESSION THAT WAS NOT PRESENT.** Name the file and the line; say what is wrong in the words you would use out loud; and say what you did **not** check, so the next reader knows where your reading stopped rather than assuming it was complete.

**Its rows, in order:** [001-025](fragment-issues/section-5b-rows-001-025.md) · [026-050](fragment-issues/section-5b-rows-026-050.md) · [051-075](fragment-issues/section-5b-rows-051-075.md) · [076-100](fragment-issues/section-5b-rows-076-100.md) · [101-125](fragment-issues/section-5b-rows-101-125.md) · [126-150](fragment-issues/section-5b-rows-126-150.md) · [151-175](fragment-issues/section-5b-rows-151-175.md) · [176-200](fragment-issues/section-5b-rows-176-200.md)

*No count is typed here — `python tools/open-defects.py` derives it, and `python tools/review-ledger.py` says how much of the repository has been read. A short table means nothing without the second number: it cannot tell you whether little was found or little was looked at.*

---

## 6. WHAT CANNOT BE RUN AT ALL — 100 fragments, by what they need

**Its own file:** [`fragment-issues/section-6.md`](fragment-issues/section-6.md)

## How to arrange a case, learned by getting it wrong all day

**Its own file:** [`fragment-issues/how-to-arrange-a-case.md`](fragment-issues/how-to-arrange-a-case.md)

## Tag leaders, 2026-09-21 — five things measured against Project1

**Its own file:** [`fragment-issues/tag-leaders-2026-09-21.md`](fragment-issues/tag-leaders-2026-09-21.md)

## A drawing set and a house plan, 2026-09-22 — seven things measured against Project1 and Project4

**Its own file:** [`fragment-issues/a-drawing-set-and-a-house-plan-2026-09-22.md`](fragment-issues/a-drawing-set-and-a-house-plan-2026-09-22.md)

## Add to this file, do not start another

A fragment that fails in front of a model belongs in this register the same day, with what was passed to
it and what came back **verbatim**. The value of the list is that every row was observed rather than expected —
the moment it fills with things somebody thought might be wrong, it stops being worth the sit-down.

**Where to write it.** Since 2026-09-23 each section is its own file in
[`fragment-issues/`](fragment-issues/section-1.md), named under its heading above. A new row of section 5
or 5b goes **at the end of that section's last rows file**, whatever its number, and the next run of
[`tools/split-register.py`](../tools/split-register.py) moves it to the file its number belongs to.
Anything else goes in its section's file, and a new section goes on this page, in full, for the next run
to move.

**Nothing leaves this register except by [`tools/archive-fragment-issues.py`](../tools/archive-fragment-issues.py)**,
which moves a finished row of section 5 or 5b to [`fragment-issues-archive/`](fragment-issues-archive/README.md)
and leaves its line in its rows file with a link to it. The archive is never written by hand, and a new row
never goes there: it goes in this register, the same day.
