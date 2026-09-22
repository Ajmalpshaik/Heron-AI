# Session note — SESSION CLOSED. THE READING SWEEP THROUGH `tools/`, AND WHERE IT STOPPED

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — SESSION CLOSED. THE READING SWEEP THROUGH `tools/`, AND WHERE IT STOPPED

**Read this entry first if you are picking the sweep back up.** It is the close of a long Linux-only
session — no Windows, no Revit, no fragment proving. Every number below is derived, and the command
that derives it is beside it.

> ### DEFERRED BY THE OWNER, 2026-09-22 — THIS IS NOT UNFINISHED WORK
>
> **Everything named below as "next" is deliberately left for later.** The owner decided at the close
> of this session that none of it has to be done now, and said plainly that later is better. **Nothing
> here is blocked, half-done, or waiting on a fix that went wrong** — it is measured, written down and
> parked on purpose.
>
> **What is parked:**
>
> - **Rows 5b-150 and 5b-151** — the two findings recorded and not repaired. They stay `OPEN` in
>   [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) because the defects are real and still there. **Open
>   means the defect exists, not that somebody is mid-way through it.**
> - **The seven `tools/` files never opened** — the table further down. The sweep stops here by
>   choice, not because it ran into something.
>
> **What this means for the next session:** do not treat any of it as a rescue. There is no broken
> state to recover, no red build, no branch left dangling. Pick it up when the owner asks for it, in
> the order below, and if he asks for something else instead, that comes first.
>
> **Why it is written here rather than left implicit.** A decision to defer that nobody records reads
> exactly like work somebody forgot — and the next session either re-derives it or, worse, rushes it.
> [D-54](../DECISIONS.md)'s lesson is that a sentence describing a state has to be corrected when the
> state changes: **when this work is picked up, delete this block.**

#### Where the sweep stands

```bash
python tools/review-ledger.py            # how much of the repository has been read
python tools/open-defects.py             # what is still open, by id
python tools/check-gaps.py               # what is genuinely unfinished
```

`tools/` is the folder that got the attention. **Ten of its 47 `.py` files had never been opened at
the start of this session; seven remain**, and every one of them is a big file:

| left to read | lines | why it is worth a sitting |
|---|---|---|
| `generate-jobs.py` | 1157 | the biggest, and three other tools import `write_threshold()` from it |
| `prove-skill.py` | 950 | writes proof records — the thing D-30 is about |
| `batch-prove.py` | 735 | same, in bulk |
| `prove-tracking.py` | 633 | same family |
| `measure-brain.py` | 435 | a measurer, so its own numbers are the risk |
| `generate-agent-map.py` | 400 | CI runs it with the name in a loop variable |
| `measure-routes.py` | 365 | already named in two tools' docstrings as having been fooled by its own prose |

**Read them in that order reversed — smallest first** — because the three proving tools are the ones a
wrong reading costs most, and by the time you reach them you will have the pattern.

#### The four rules that earned their keep, and the cost of each

1. **Trace, don't grep. A call site is not an execution.** Row 5b-149 was found by *running* the tool's
   own `survey()` over the tree and printing what it actually held — 636 names — not by reading the
   code and reasoning about it. A scan over source text was wrong 28 times out of 28 on one earlier
   sweep.
2. **A fix is not proved until its test has been seen to FAIL — and it must FAIL, not CRASH.** An
   `AttributeError` proves nothing. Guard every new name with `getattr(MODULE, "NAME", None)`;
   [`heron-ship` §2a](../../.claude/skills/heron-ship/SKILL.md) has it.
3. **Shape is not behaviour.** Three separate findings this session were traced, measured, and **not
   raised**, because they fire nowhere today: `check-reachable`'s name-only `decorated` set, its
   invisibility to `ast.AsyncFunctionDef`, and `check-declared-questions`' `told`/`s_told` count. Each
   is written down as not-a-finding so the next reader does not "discover" it.
4. **A negative result is a result.** `tools/owner-queue.py` was read end to end and is sound; it is
   marked `clean` in the ledger with what was checked, so nobody reads it again.

#### The one I nearly got wrong, and how it was caught

`tools/check-api-surface.py` types its own `ALL_VERSIONS`, which reads exactly like the third copy of
the supported-release list. **It is not.** `tests/test_supported_releases.py` finds all five
declarations and pins each against `heron_dotnet.RELEASES`, naming this one by file and line — and
**PROPOSALS.md F2 already holds the merge question for the owner**. Binding it would have pre-empted
his decision and deleted a guarded duplication that has a written reason.

**The rule that caught it: run the suite before calling a duplication unguarded.** A typed list is not
evidence of anything until you have asked what holds it.

#### What is recorded and deliberately NOT fixed

Rows **5b-150** and **5b-151** are open findings with the file left alone, which is
[§5b](../FRAGMENT-ISSUES.md)'s own rule: *the sweep records, it does not repair.* Both remedies are
named in the rows and neither needs design work:

- **5b-150** `check-api-surface.py` — every way it cannot answer exits **1**, the same code a genuinely
  missing Revit API member uses. Measured here with no `dotnet` on PATH: a traceback. It is the
  **fourth** tool this session with no third state, after rows 133, 142 and 143, whose remedy is
  settled — a `cannot_answer()` asked after the argument check and before any build, printing NOT RUN
  and exiting **3**. **The failing case is the default one on a Linux box**, so the fix proves itself
  here.
- **5b-151** `check-declared-questions.py` — ten fragment rows print an absolute container path while
  the skill rows two blocks below print a relative one. The rest of that tool was traced and is sound;
  the row says exactly what was checked so it is not re-read.

Neither has a suite touching `main()`, which is why both survived. **That is the pattern worth
carrying forward: in this repository the report block is where the untested code is.**

#### What is waiting on the owner and cannot move here

```bash
python tools/owner-queue.py          # the list, derived from the registers
```

Rows needing a decision only he can make: **5b-83, 5b-95, 5b-145** — and 5b-145 is the sharpest, because
it is a *definition*, not a bug: `agent-count.py` counts a test suite's header as BUILT while
`generate-contract-reference.py` says in capitals that **a suite is not the agent**. If a suite-only
claim is not a build, then *"0 of 250 agents left"* is wrong by three. **Do not resolve it.**

Everything in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) needs his PC, and most of it needs Revit open.
`check-gaps.py` reports **UNFINISHED — nothing**: everything buildable on a Linux container is built,
and the whole remaining list is waiting on a machine, a dependency or a person.
