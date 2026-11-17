# FRAGMENT-ISSUES archive — section 5b, rows 976 to 1000

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 976 to 1000 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-977

*Moved from the register on 2026-11-17.*

**Defect.** **A READ-LEVEL JOB CAN STILL OPEN A TRANSACTION OF ITS OWN IN THE MODEL IT RUNS IN, COMMIT IT, AND KEEP THE CHANGE - AND THE RUN GUARD DOES NOT CATCH IT.** Found 2026-10-17 building [row 976](../fragment-issues/section-5b-rows-401-425.md)'s guard. `run_fragment_read`'s guarantee is that it opens NO transaction, so Revit refuses any change - `mcp/server/heron_tools.py` says *"What holds for every caller is Revit's own refusal of a change made outside a transaction"*. But nothing stops the fragment's own code calling `new Transaction(doc)` and committing; only `needs_write_path` refuses a fragment that DECLARES it opens its own. The run guard treats every change to the model a job runs in as the transaction group's business, so this one passes it too. Read in the source, not run. Measured 2026-10-17: no fragment in the library at READ, ANALYZE or EXECUTE opens a `Transaction`, `SubTransaction` or `TransactionGroup` today, so nothing exercises the hole. **Narrowed the same day by [row 350](../fragment-issues/section-5b-rows-326-350.md)'s detector**: a read run whose model's element or type count, or `IsModified`, moves is now answered `read_changed_model`. That catches such a commit in a saved model, or one that adds or removes an element; a commit that only edits existing elements in a model that already had unsaved changes still passes both. **And by [row 592](../fragment-issues/section-5b-rows-401-425.md)**: the add-in now refuses, from its source, a read or show fragment that opens a transaction of its own. What is left is behaviour the source does not show.

**State.** **MOVED TO ISSUE [#932](https://github.com/Ajmalpshaik/Heron-AI/issues/932) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** The run guard is where it belongs: a committed `DocumentChanged` in ANY open model while a job below Modify runs is that job seen changing a model. Not done in row 976's change because a read-level fragment that legitimately commits something would then be refused - none is known, and whether one exists is to be listed and checked in Revit before the rule changes.

---

### Row 5b-980

*Moved from the register on 2026-11-17.*

**Defect.** **`score-routing.py`'S *"THE INDEX THIS RAN AGAINST"* CANNOT COUNT A STORE IT HAS JUST BUILT, SO EVERY RUN FROM A FRESH SCRATCH STORE - THE WAY [HERON-SHIP](../../.claude/skills/heron-ship/SKILL.md) SAYS TO RUN THE STORE CHECKERS - PRINTS NO ROW COUNTS BESIDE ITS NUMBERS.** Found 2026-10-17 in a cloud container while widening the tool for its session's own sentence sets; measured on main `c2f2765a` and on that branch, lexical, no Revit and no model data. `main()` reads `check-skill-routing._index_fingerprint()` before asking (*"READ BEFORE ASKING"*, so asking cannot move the store under the fingerprint), right after `store_for_this_tree()` has rebuilt the fragments table - and the `identities` table is made by `heron_search.index()` on the first lookup. So the first run on `HERON_KNOWLEDGE=$(mktemp -d)` prints `counts COULD NOT BE READ - OperationalError: no such table: identities`, with an md5 of the store before its search index existed, and a second run on the same store prints `fragments 571, identities 3941, utterances 0, vectors 571`. Honest - D-52 refuses to call an unreadable store empty - but the one block printed so that two runs can be compared is empty on every scratch-store run.

**State.** **FIXED 2026-10-17 - the store is prepared before the fingerprint is taken, as `check-risk-crossings.py` already does.** `main()` opens it once through `heron_brain._Open()` - the same preparation every lookup makes - after `store_for_this_tree()` and before `_index_fingerprint()`, so the order *"READ BEFORE ASKING"* stands. Measured on main `0e8b80b5`, lexical, a fresh scratch store: the block reads `fragments 571, identities 3950, utterances 0, vectors 571`, its md5 equals the store's after all 199 lookups, and every score is as it was (18 / 9 / 4 / 33 and 8 handed a change; the plain set's 12 and 47). `tests/test_score_routing.py` section 10 runs the tool on an empty scratch folder with its questions stood in for and requires the counts; it fails on the tool as it stood. **And the same day, on the owner's word (*"fix the other two too"*), the two other tools that took it at the same point:** `tools/check-skill-routing.py` printed `md5 unreadable` on an empty scratch folder - no store existed until its first lookup made one - and then a false *"THE STORE CHANGED WHILE THIS RAN"*, blaming the asking or another session for the store it had just built; `tools/prove-skill.py` took it at the same point. Each now opens the store first, and a store that cannot be opened is left to its lookups, which name why as before. On fresh scratch stores both blocks read `fragments 571, identities 3950, utterances 0, vectors 571`, each md5 equals the store's after the run, and `check-skill-routing`'s report is otherwise line for line what it was. `tests/test_skill_routing.py` section 7 and a check in `tests/test_skill_proving.py` run each tool's `main()` on an empty scratch folder with `heron_brain.lookup` stood in for; both fail on the tools as they stood.

---

### Row 5b-981

*Moved from the register on 2026-11-17.*

**Defect.** **[`next-id.py`](../../tools/next-id.py) CALLED A SESSION'S OWN SQUASH-MERGED ROW TAKEN ON MAIN: ITS BRANCH STILL ADDS THE ROW AGAINST ITS OLD MERGE-BASE, AND MAIN NOW HOLDS THE SAME ROW, SO "ADDED HERE AND ON MAIN" READ AS A CLASH.** Found 2026-10-17, right after #599 ([row 5b-941](../fragment-issues/section-5b-rows-926-950.md)) merged: run on its own branch, the tool named `5b-941 TAKEN ON MAIN` for `origin/claude/jolly-rubin-qxvub2` and its worktree. A squash-merge puts a branch's work on main as one new commit, so the branch's `git merge-base` with main never moves and every id it wrote stays in what it ADDS. TAKEN ON MAIN is said only for live work - an open pull request or this checkout - and a session still on its merged branch is this checkout, so the false clash fell on exactly the session that had just finished.

**State.** **FIXED 2026-10-17 in `next-id.py` alone.** TAKEN ON MAIN now compares the id's ENTRY at the holder with main's: a 5b row's whole table line, a group's own file `docs/needs-checking/group-xx.md`, a question's or a decision's heading line. The same entry is the work's own, merged, and is listed as **MERGED** (`already_merged` in `--json`); different words under the number stay TAKEN ON MAIN. Entries are read only for an id that needs comparing - a worktree's from disk, its CRLF read as LF - and the id readers still return sets, so `renumber.py`, which imports them, is unchanged. **Tested, offline:** `tests/test_next_id.py` section 13 commits this checkout's ids in all four registers, then a squash of them onto main with the second of each in other words: the first is not TAKEN ON MAIN and is MERGED, the second still is, and a worktree with CRLF on disk reads the same. Against the tool as it stood the section fails 10 checks, with no traceback; its four "still is" checks pass on both. `tests/test_renumber.py` passes unchanged. **Run on this repository:** the #599 branch checked out in a scratch worktree and asked with `--no-fetch` - main's tool prints `TAKEN ON MAIN 5b-941`, this one `MERGED 5b-941`; this row's number is this tool's `next` - 5b-980 at first, and 5b-981 when the same tool, run again before pushing, named 980 HELD TWICE with #621. **Not covered:** a merged entry main has EDITED since - a row a later pull request marked FIXED - still reads as TAKEN ON MAIN, because the files cannot tell that from a clash; and a group written only as an index heading, with no file of its own, is never MERGED.

---
