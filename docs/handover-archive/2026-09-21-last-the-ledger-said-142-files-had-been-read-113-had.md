# Session note — (last) — THE LEDGER SAID 142 FILES HAD BEEN READ; 113 HAD

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (last) — THE LEDGER SAID 142 FILES HAD BEEN READ; 113 HAD

**[Row 5b-90](../FRAGMENT-ISSUES.md). FIXED.** Found while marking eight modules for row 5b-89 that had
only been read at one function.

**Thirty of the 142 files `docs/REVIEW-LEDGER.tsv` counted as read carried notes opening *"PARTIAL
READ and said so"*** — honest prose, written by sessions doing the right thing, **in a column nothing
counts**. `AGENTS.md` sends people to `python tools/review-ledger.py` for how much of the repository
has been read, and section 5b of `FRAGMENT-ISSUES.md` closes with *"A short table means nothing
without the second number: it cannot tell you whether little was found or little was looked at."*
**That second number is this one, and it was soft by 21%.**

**It is a seventh column, not a third verdict.** `partial` as a verdict would have dropped **nineteen
real findings** out of *read, issue found* to fix a count — 19 of the 30 carry a defect row as well.
How much was read and what was found are separate questions about the same file, so `scope` holds
`full` or `part` beside the verdict.

The summary now separates **opened at all** from **READ WORD BY WORD**, and says which line is the
one `AGENTS.md` asks for. **`--part` refuses without a `--note`** saying which part: a part-read mark
nobody can resume is *worse* than no mark, because it takes the file out of the never-opened queue
and puts nothing in its place.

**Nothing was rewritten.** The ledger is append-only and `tests/test_review_ledger.py` §3 holds that,
so each of the 29 still in scope got a **new row naming whose read it was** — re-classifying somebody
else's mark is not reading the file again and must not read as if it were. A row written before the
column has six cells and means `full`; a scope nobody defined is **malformed rather than assumed
safe**, the rule the verdict column already had.

**Shown to fail: 6 checks** in a new §7. **Two of its nine pass against the old tool for the wrong
reason** — a seven-cell row is refused there as the wrong cell count, which happens to give the right
answer — and that is recorded rather than counted as proof.

**AND THE FIRST DRAFT CRASHED INSTEAD OF FAILING, FOR THE FOURTH TIME IN ONE DAY.** It named
`RL.PART`, which the old tool has not got. Rows 5b-85, 5b-88, 5b-89 and 5b-90 are **one mistake made
four times, twice after the lesson was written down** — so this time it went into a house rule rather
than another paragraph: **[`.claude/skills/heron-ship`](../../.claude/skills/heron-ship/SKILL.md) §2a**
now carries *a fix is not proved until its test has been seen to FAIL*, the `getattr` and
`__code__.co_argcount` forms that ask before they call, and the table of all four.

**Read this before writing a negative proof.** That section is the only thing standing between the
next session and a fifth.

**That `heron_audit` note is now [row 5b-91](../FRAGMENT-ISSUES.md) below, fixed, with a suite.**

**Live-path brain modules read: 7 of 53**, and `brain/heron_audit.py` read but not yet marked.
