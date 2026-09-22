# Session note — (later) — THE INSTALLER'S FIRST READING, AND TWO FOLDERS FINISHED

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (later) — THE INSTALLER'S FIRST READING, AND TWO FOLDERS FINISHED

**Section 5b rows 78 to 82. All five FIXED.** **`platform/`, `mcp/` and the repository root are now read to the end** — the first three buckets to come off the list. The whole of Stage 3 and Stage 4 landed in **#233** — 2,924
lines across `platform/Heron.Installer/`, `platform/Heron.Installer.App/`, `tools/deploy-addin.ps1` and
`tools/HeronRevit.ps1` — and **none of it had been read by anyone**. This session read all ten files
word by word. Seven came back clean; three did not, and every one of the three is the kind a compile
and a green suite cannot see.

**[Row 5b-78](../FRAGMENT-ISSUES.md) — the installer hangs rather than fails.** `PowerShellRunner.Run`
redirected both pipes and read them one after the other, `ReadToEnd` on standard output and then on
standard error, and only then called `WaitForExit`. A pipe holds a few kilobytes; a child that fills
the one nobody is reading blocks there and never closes the other, so the first read never returns
— and the **600-second ceiling below it is not late, it is unreachable**, because the thread never
gets to that line. **Measured here rather than argued**: 8 KB on standard error came back in 0.0s,
**200 KB never came back at all** and a 10-second ceiling never fired. The caller that reaches it is
the deploy, which sets `$ErrorActionPreference = "Stop"`. The same method also put both streams into
one buffer and then parsed it as JSON, so **one line on standard error made the window say no Revit
was found on a PC with three**. Both fixed: the streams are drained at once and kept apart, and only
standard output is parsed.

**[Row 5b-79](../FRAGMENT-ISSUES.md) — the only way back deleted itself first.** `Save-PreviousInstall`
removed the previous backup and *then* copied the live install into the empty folder, so a copy that
died halfway left **part** of an install where a whole one was. `-Rollback` then restored it, because
it only ever checked that the folder existed — and the verify afterwards passes, since the main
assembly is the first thing copied. It now stages the copy aside and swaps, and rollback refuses a
backup with no `replaced.json` or a file count that disagrees with its own record, both before the
live install is touched. **This is [row 5b-31](../FRAGMENT-ISSUES.md)'s shape** — the checkpoint save
that deleted the old file before renaming the new one — in the one script that is Heron's only way
back.

**[Row 5b-80](../FRAGMENT-ISSUES.md) — the wrong thing to do next, and a test that pinned it.**
`InstallPlan.Build` asked *is this Revit on the PC* before it asked *does the product run on it*, so a
release that is **both** absent and unsupported came back as *“Revit 2026 is not installed on this PC
… **Install Revit 2026 first**”* — a two-hour install for an answer that was never going to arrive,
because the product does not support 2026 either. `InstallerScreen.WhyNotOffered` already put the
stronger reason first and says why; two places deciding the same kind of thing and only one knew the
rule. **What makes it a row rather than a nit is that the suite covering it used exactly the
both-wrong case and asserted the wrong reason** — a guard whose test demonstrates the defect is worse
than no test, because the next reader takes the green as an answer. Order swapped, suite rebuilt so
one plan produces one of each case, and shown to fail with the old order put back.

**AND MY OWN FIRST NEGATIVE TEST PASSED WHEN IT SHOULD HAVE FAILED**, which is the lesson worth
carrying. The new ordering check was `text.find(a) < text.find(b)`, and `str.find` returns **-1** for
a string that is not there — so it went green **loudest exactly when the guard it was checking had
been deleted**. Caught only by breaking the guard on purpose and watching nothing happen. Four
negative tests now, one rule each, and all four go red.

**Rows [5b-81](../FRAGMENT-ISSUES.md) and [5b-82](../FRAGMENT-ISSUES.md) — two typed counts, found by
finishing the folder rather than by looking for them.** `platform/README.md` said the installer engine
has **38 checks** against a fake Revit; the test host prints **84**, and nothing measurable in the
repository is 38. Four lines below it said the window is owed `AB1` to `AB7` — **which row 5b-78 made
wrong earlier the same day**, by my own hand. `mcp/README.md` said **three tools** stand on
`heron_brain.py`, the one seam between the MCP side and the brain; an AST walk over the 34
`@server.tool()` functions says **ten**. That figure is the argument for the seam existing at all, and
three reads like a narrow door somebody could still reason about. Both numbers deleted rather than
corrected, with the command that derives them in their place and the old figure kept and dated.

**Six sweeps came back empty, and that is worth as much as the rows.** The pipe-deadlock shape of
5b-78 exists in **one** place: `WindowsAdapters.cs` is the only C# in the repository redirecting
standard error, and the three Python sites that take both pipes all handle them concurrently
(`subprocess.run`, `communicate()`, and a daemon drain thread). **No suite has all its assertions
inside a loop.** And no gate or suite can pass by discovering nothing — the two candidates both have
another check that fires first.

Three more on the second pass, all against defect shapes this register already
knows. **Every config key is declared**: `HeronConfig.Defaults` holds eight, all eight are read
somewhere, and nothing reads a ninth - the nine that looked undeclared are capability names in the
`revit.<domain>` namespace, plus a deliberate typo in `tests/test_config_and_health.py` that exists
to prove an undeclared key is refused. That is [row 5b-29](../FRAGMENT-ISSUES.md)'s shape, checked and
absent. **The delete-before-write shape of [rows 5b-31 and 5b-79](../FRAGMENT-ISSUES.md) is not a class
in Python**: one site in `brain/`, `mcp/` and `tools/` matches it, and it is
`check-fragments-compile.py` clearing its own generated build folder, which is gitignored and
rebuildable. **And the review ledger - the sweep's own memory - is opened `"a"`**, so a crash
mid-write can lose the last line and never the file. Reported as one place, not a class.

**WHAT NEEDS WINDOWS — NONE OF IT WAS TOUCHED, AND TWO ROWS WERE ADDED TO IT.** `AA10` (the
half-written backup: delete `replaced.json` by hand and confirm rollback refuses and changes nothing)
and `AB8` (make a deploy fail loudly and confirm the window comes back with a sentence rather than
stopping) are in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) and **have never run**. `platform/README.md`
now names **Group `AB`** rather than a range, so the next row added does not make it wrong again. Everything else
about the installer stays where #233 left it: three Windows adapters that have never executed a line,
`deploy-addin.ps1` not run since it was changed, and its rollback proof of 2026-09-19 **STALE** for
the current file.

**Numbers at the end of it**, all derived: **252 register rows, 32 open** (all five new rows are
FIXED, so the open count did not move), **117 of 1,179 files read, 0 stale**, ten gates green, and
`check-gaps` exit 0 with nothing on its UNFINISHED list. **What is left to read is now four folders
and nothing else**: `brain` 537, `tests` 229, `tools` 132, `docs` 125, `revit` 33.

**The root came off last, and one file in it was read to a deliberate boundary.**
`HERON_AI_MASTER_ARCHITECTURE.md` is a research brief whose body is **unedited on purpose**
([D-57](../DECISIONS.md)) — its own banner says the disagreements are the useful part and an edited
brief stops showing what was proposed. So the body cannot go stale the way a normal file does, and
the only part making a claim about today is the banner. Every claim in it was checked and holds:
D-57 exists, [32](../32-master-architecture-reconciliation.md) carries the sections it points at, the
*nine exist and four are stricter* line is 32 §2's own headline rather than a number invented here,
and every section it cites is really in the file. The mark says what was not read and why.
