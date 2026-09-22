# Session note — THE RESTORE CARRIED ON AFTER A SAFETY COPY IT COULD NOT TAKE

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE RESTORE CARRIED ON AFTER A SAFETY COPY IT COULD NOT TAKE

**[Row 5b-143](../FRAGMENT-ISSUES.md), FIXED.** `tools/heron-backup.py` read end to end — 454 lines, the
tool that moves a person's own data. **Four findings, all measured.**

**One.** `restore()`'s docstring makes exactly one guarantee:

> *"IT TAKES A SAFETY COPY FIRST, ALWAYS … the thing it destroys is the only copy of what was on the
> machine a second ago. If the backup turns out to be the wrong one, the state it replaced has to still
> exist."*

`safety = take(...)` and **nothing checks the answer**. `take()` returns `None` when a backup of that
label already exists, and the label is a timestamp **to the second** — so two restores inside one
second gave the second one *"A backup called X already exists. Nothing was written"* and **it
overwrote anyway**. A backup root that cannot be written raised `NotADirectoryError` straight out of
`restore`.

**Two.** The safety copy was taken from the **data root**, not from what was about to be overwritten.
Measured restoring into another folder: it copied the data root — a folder never at risk — destroyed
the other one, and printed *"The state it replaced was copied to … first"*, **which was false**.

**Three.** A restore is a **merge**, and nothing said so. A file the backup never held was left in
place while the tool printed *"Restored 2 file(s)"*. The behaviour is **right** — deleting a person's
files is the dangerous direction — but somebody restoring *because the folder is wrong* is left
believing it now matches the backup.

**Four.** `drill` on a machine where Heron has written nothing exited **1**. `docs/21` §8 asks for *a
periodic automated drill*, so that reading goes to a schedule, not to a person.

#### The fix

`take()` takes a `source`; the safety copy is of `target`, its label carries microseconds, and **a
safety copy that returns `None` or raises stops the restore** — refusing loses nothing, since the
backup is still there. The manifest note and the restore output both say a restore **removes nothing**.
`drill` answers `NOT RUN` and **3** with nothing to drill — the same remedy as rows 5b-133 and 5b-142,
and the **third time this session** a tool had no third state.

#### The suite that already owned it was extended, not duplicated

`tests/test_backup.py` is a good suite — the byte-for-byte round trip, the excluded derived index with
its reason, the unanticipated file kept anyway, verify by content, both refusals, the safety copy and
the drill. It covered none of these four. A second backup suite would be two suites for one tool.

**And case 9 found finding one by accident**: it took no safety copy at all, because the case before it
had taken one in the same second. The case meant to measure *where* the copy comes from measured that
there **was** none — which is why a suite is proved by breaking the thing it guards.

| Break | Red |
|---|---|
| **the module as found** | **5** |
| the refusal on a failed safety copy alone | 2 |
| the safety copy's source alone | 1 |
| the NOT RUN drill alone | 2 |
| the derived index no longer excluded | 4 |

**Run end to end on a fixture:** backup, a stray file, restore — the stray survives and is named;
`drill` exits **3** with nothing and **0** with data. Nothing under a real `%APPDATA%` was touched.
