# Session note — Sitting of 2026-09-16 - the notes were the backlog

> **Archived session note** from 2026-09-16. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---


# Sitting of 2026-09-16 - the notes were the backlog

**Asked for:** pull GitHub down so local and remote are identical, read the handover and say what the
balance is, read the issue registers, **and fix the notes themselves where a note was already done
and still said otherwise.** That last half is what this section is mostly about.

**Nothing was proved against Revit here.** No fragment was run, no add-in was built, no count moved
because work was done. Every number below is derived, and every correction below is a correction to a
MARK, never to a result.

## The repository

`main` was **9 commits behind** `origin/main` and fast-forwarded clean to `749b580`. Working tree was
clean before and after. **Local and GitHub are now identical.**

**Five local branches survive, and they are two different things:**

| branch | what |
|---|---|
| `claude/friendly-hypatia-196de4`, `claude/gifted-burnell-61c3c3`, `claude/nifty-mendel-ce45f7` | **Merged - their code is on `main`** (#147, #148, #149). What is unique to them is **stale README prose** - *"it is still private"*, *"298 PROVEN"*. Each also owns a live **worktree** under `.claude/worktrees/`, so another session may be standing in one. **Do not delete them without checking that.** |
| `ci/run-check-signatures`, `ci/wire-three-checkers` | **Real work, unpushable.** Both touch `.github/workflows/gates.yml` and the `gh` token has no `workflow` scope |

**`ci/wire-three-checkers` was documented NOWHERE** until this line. The section above records only
`ci/run-check-signatures` and says *"two things are parked on it"* - it is three, and the second
branch is the bigger one: **32 lines wiring `check-licence`, `check-narrow-errors` and
`check-fragments-compile`**, none of which CI runs today. All three exit **0** on the tree right now,
so the branch is ready and waiting on one command in an interactive terminal:

```bash
gh auth refresh -h github.com -s workflow
```

## The balance, derived rather than read

| | | derive it with |
|---|---|---|
| Fragments | **310 `PROVEN` / 62 `DRAFT`**, 372 total | `grep -h "^heron-status:" brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| Waiting on the owner | **140 items** - 25 decisions, 80 needing Revit, 4 needing the PC only, 2 needing a network, 29 to be read | `python tools/owner-queue.py` |
| Heron's own open defects | **29**, ids printed | `python tools/open-defects.py` |
| Stale signatures | **1** - `set-wall-constraints` | `python tools/check-signatures.py` |
| Gates | `check-docs`, `check-metadata`, `check-structure` all **exit 0** | |

**The section above says `298 PROVEN / 62 DRAFT, 360 total`.** That was true at its close and is left
standing. Twelve fragments have been added since and all twelve arrived PROVEN.

## Four notes that said OPEN and were not

Each was verified in the code or by running it, not inferred from a commit message.

| | said | is |
|---|---|---|
| **[FRAGMENT-ISSUES row 37](../FRAGMENT-ISSUES.md)** | *"OPEN, and it needs an add-in rebuild"* | **Withdrawn by row 46 on 2026-09-13.** A Revision's `Element.Name` really is `"Seq. N - Description"`; no rebuild was ever needed |
| **row 44** | *"wired into `gates.yml`"* | **Never true.** That commit is the unpushable one. It runs because it **rides inside `check-docs.py` as section 9** |
| **row 96** | *"OPEN"* | **Closed 2026-09-15 by the session the row itself names.** The closing sentence was appended and the first word left alone |
| **row 97** | *"OPEN. A separate session is on it"* | **That session landed as #147.** `DocumentPin._common` - *"This is the whole fix"* |

**Row 44 is the one worth keeping.** A note claiming a gate runs in CI, when it does not run there at
all, is worse than no note: it is the exact false comfort `check-signatures` was written to prevent,
told about `check-signatures`.

## Section 5's heading said "six still open" and it was twenty-nine

Ninety-four rows were appended under it. **No append was careless** - each session wrote an honest row
and left the heading to somebody else, which is what makes this recurrent rather than sloppy. It is
the prose-total drift [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) has recorded against itself **seven
times**, in the one file that had not yet caught it.

The heading carries no number now. `tools/open-defects.py` prints the ids, and **says in its own
output what it cannot see**: a row whose state still reads OPEN after a LATER row closed it. All four
above are that shape, and no pattern finds them - the closure is written in a different row, in prose.

## A strike that a person could read and a tool could not

**`E11` to `E15` were all RUN on 2026-09-15. `owner-queue.py` kept printing all five back to the owner
for eight days**, beside the genuinely open `E16`-`E18`.

The session that ran them struck the **`Do this` cell** and left the **ID** unstruck. A reader sees a
crossed-out instruction and correctly reads *done*. The tool matches
`^\|\s*(~~)?\*\*([A-Z]\d+[a-z]?)\*\*` and only ever looks at the ID.

**Two marks for one fact, kept in two places, updated in one.** The prose drifts in this repository
were all caught by running the count; this one was invisible to the count, because the count was the
thing that was wrong. Four had PASSED (`E11`, `E13`, `E14`, `E15`); `E12` FAILED and is closed rather
than passed, superseded by `E16`. **If you strike a row, strike the ID.**

## The E numbers in NEEDS-CHECKING's prose were all one too low

[`the 2026-09-16 sitting`](../handover-archive/2026-09-16-e11-e14-renumbered-mid-flight.md) warned about exactly this - *"If you remember E11
failed, that row is E12 now"* - and **the warning did not reach the file it was warning about.** The
heading `### E11 FAILED` sat directly beneath a table row reading `E11 ... PASSED`. Corrected to `E12`,
with the old numbers named rather than quietly swapped.

## Two things that are now better than the notes say

- **`test_builder.py` and `test_instructions.py` both exit 0.** The section above records them as
  *"two tests are red on `main`"*. Re-run here: both pass. Whatever fixed them is not recorded, and
  that is worth knowing - **a red test that goes green unattributed is a test nobody is reading.**
- **`check-licence`, `check-narrow-errors` and `check-signatures` all exit 0**, so the parked CI
  branches are not hiding a failure.

## The one thing that is WORSE than the notes say

**The deployed add-in is older than `main` on all three Revit releases.**

| Revit | deployed | `main`'s add-in source |
|---|---|---|
| 2020 | 2026-09-15 22:56 | **2026-09-16 13:24** (`58a613d`) |
| 2024 | 2026-09-16 02:29 | |
| 2027 | 2026-09-15 22:56 | |

So every row that ends *"needs a deploy"* - [row 10](../FRAGMENT-ISSUES.md), [row 13](../FRAGMENT-ISSUES.md)
- is still blocked, and **`E16`-`E18` cannot be attempted until it is rebuilt.** Build and deploy one
release at a time; the output folder is shared, and what is in it last wins.

## What this sitting did NOT do

- **Nothing was run against Revit.** The 80 Revit rows are untouched.
- **No branch was deleted.** Three are merged and safe to remove, and each owns a live worktree, so
  that is the owner's call with a session possibly standing in one.
- **The remaining 29 open defects were read, not fixed.** Row 8 is still the biggest single unblock:
  a bare `ElementId` result cannot be read as a quantity, and **15 DRAFT fragments have no other
  countable result at all.**


## The deploy happened, ninety minutes after the section above said it had not

**2026-09-16 18:04-18:06, on the owner's PC.** He closed Revit and ran all three. Verified by
reading the deployed binaries rather than by trusting the build exiting 0:

| Revit | framework in the deployed DLL | deployed | size |
|---|---|---|---|
| 2020 | `.NETFramework,Version=v4.7.2` | 18:06 | 156,672 |
| 2024 | `.NETFramework,Version=v4.8` | 18:04 | 156,672 |
| 2027 | `.NETCoreApp,Version=v10.0` | 18:04 | 157,184 |

**Each release got its OWN framework**, which is the thing the shared `bin` folder makes easy to get
wrong and impossible to see: a 2027 build sitting in a 2024 folder loads nothing and Revit does not
say why. `Heron.Core.dll` and `Heron.Bridge.dll` carry the same timestamp in all three folders, 2027
has its `.deps.json`, and the add-in grew from ~128 KB to ~156 KB - a real rebuild, not a re-copy.

**So `E16`-`E18` are unblocked, and the two rows that end *"needs a deploy"* have their precondition
met** - [row 10](../FRAGMENT-ISSUES.md) and [row 13](../FRAGMENT-ISSUES.md). **Neither is proved by this.**
A deployed fix is a fix that can now be run; D-30 wants it run.

**The table above this one went stale in ninety minutes**, which is the whole subject of this sitting
arriving on schedule. It is left standing and corrected here rather than edited, because the interval
is the finding: **the fastest-moving fact in this repository is the state of the machine in front of
him, and it is the one every register records as prose.**

### The command in that table was wrong, and the shell said so

It was given with `&&` between the build and the deploy. **Windows PowerShell 5.1 has no `&&`** -
*"The token '&&' is not a valid statement separator in this version."* Nothing ran; it did not parse.
The separator is `;`, and *only if the last one worked* is `if ($?) { ... }`:

```powershell
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2024; if ($?) { powershell -File tools\deploy-addin.ps1 -RevitVersion 2024 }
```

**`deploy-addin.ps1` refuses while Revit is running** (`Get-RevitBlockReason`), so the build half can
be done with Revit open and only the copy needs it closed.


## Then he asked whether the OTHER pages had been done, and three indexes had drifted the same way

Not the registers - **the pages that say what exists.** Each was a typed list of files, and each had
been appended to by sessions that added the file and not the row.

| page | said | was |
|---|---|---|
| [`work-notes/README.md`](../work-notes/README.md) §*What is here now* | **11** notes | **15** on disk. Missing: `mep-session-2026-09-16`, `proving-session-2026-09-15`, `agent-build-order-2026-09-13`, `next-steps-2026-09-12` |
| [`tools/README.md`](../../tools/README.md) | every tool | **three** were absent - `open-defects.py`, `new-agent.py`, `resign-machine-proofs.py` |
| [`FOR-THE-OWNER.md`](../FOR-THE-OWNER.md) §2 | *"The five buckets"* | **six**, and `owner-queue.py` defines six |

**`FOR-THE-OWNER.md` is the sharpest of the three.** Its §6 is titled *why this page holds no list*,
and it carried a count that had drifted - not of items, which that section forbids, but of the buckets
the items fall into. **The rule was obeyed one level up from where it was needed.**

All three now carry the command that derives them, and **all three commands were run before being
written down.** Two were wrong the first time and the repository's own rule caught both:

- the work-notes check missed `FRAGMENT-REVIEW-PLAN-CHATGPT-2026-09-07.md` and
  `PROMPT-fragment-validation-agent.md`, because `[a-z0-9/.-]+` **cannot see a capital letter**.
- the tools check reported **twenty false extras**, because `grep -oE '[a-z-]+\.py'` splits
  `heron_architect.py` at the underscore and hands back `architect.py`, a file that does not exist.

**Prove the pattern can see what you know is there.** Written in this repository since the beginning,
broken twice in one hour by the person writing the commands to enforce it, and caught both times by
running them instead of reading them.

`tools/open-defects.py`, `new-agent.py` and `resign-machine-proofs.py` are now documented. The first
is new; **the other two had been undocumented since they were written** - the quiet version of the
defect `check-signatures.py`'s own commit message names: *a gate nobody runs is the same as no gate.*
