# Session note — (after the real-Revit run) — TWO THINGS #235 PROVED THAT NOTHING WAS HOLDING

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (after the real-Revit run) — TWO THINGS #235 PROVED THAT NOTHING WAS HOLDING

**Main moved while this branch was open.** [#235](https://github.com/Ajmalpshaik/Heron-AI/pull/235)
landed — Groups AA and AB run on the owner's PC against Revit 2024.3 and 2020.2.9, **14 pass, AA9
fails**, 23 proof screenshots. The eight rows of PR #241 were squashed on top of it. Checked file by
file: nothing of either side was lost.

**[Row 5b-96](../FRAGMENT-ISSUES.md). FIXED. Two halves, and the first is mine.**

[Row 5b-79](../FRAGMENT-ISSUES.md) moved the backup path and I wrote beside it *"No old backup is deleted
or moved."* **On a real machine that is false for `heron-bridge`**: its `productFolder` is `Heron`, so
its new `backupDir` is `install-backup\<version>\Heron` — **exactly where the old layout put the
backed-up folder**. The first new deploy writes over it. True for every other product, which is why
reasoning missed it and a machine did not. The note says what the machine saw now, and **keeps the old
sentence as a quote with its history**. The code was deliberately not changed: tidying the orphans
means reaching into a path this script no longer owns, which is the worse risk the paragraph names.

The second half is what `AA9` actually found: a downloaded Heron carries `ZoneId=3` on **all 2130
files**, and `RemoteSigned` refuses an unsigned downloaded script **before its first line runs** — so
`Unblock-File`, which lives *inside* `deploy-addin.ps1`, cannot clear the mark that stops
`deploy-addin.ps1` running. [D-96](../DECISIONS.md) ruled the same day: **the installer is the only
supported route**. **That made `-ExecutionPolicy Bypass` in `WindowsAdapters.cs` load-bearing** — and
it appears in exactly one place in the repository with **nothing under `tests/` mentioning it**. Its
comment argued the flag was *safe* and said nothing about the install breaking without it.

**[Row 5b-97](../FRAGMENT-ISSUES.md). FIXED. The same shape, same run.** `AB1` failed on a real machine
and was fixed the same day — a wrapping `TextBlock` instead of a bare string that clipped *"...that
exist"* with no ellipsis. **The fix is right; nothing held it.** Not one mention of `TextBlock`,
`TextWrapping` or `AB1` in `tests/test_installer_window.py`. Reverting would compile, pass every gate
and every suite, and clip again where nobody is watching.

**The lesson under both, worth more than either row.** A row in `NEEDS-CHECKING` that FAILS on the
owner's PC and gets fixed the same day leaves **a fix with a screenshot behind it and nothing on this
side**. The visual half genuinely needs Windows; **the structural half almost never does**. When the
next real-Revit run comes back, ask of every fix it produced: *what text fact would go red if somebody
undid this?* — and write that down before moving on.

**Three drafting mistakes in 5b-97's short section, all the same kind**: a check that does not look
where the thing it is about actually is. `"AB1" in window` was true against the unfixed file too;
`find("TextWrapping")` returned the first one in the file, above the tick box, because every other
block already wrapped. **A check that is true either way is not a check.**
