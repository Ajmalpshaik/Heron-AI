# Session note — Sitting of 2026-09-15 — the phantom modification, and why it was not one

> **Archived session note** from 2026-09-15. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---


# Sitting of 2026-09-15 — the phantom modification, and why it was not one

**Branch:** `claude/quirky-engelbart-19a941` (worktree). **Tree left clean apart from this file and
the `A17` row added to [NEEDS-CHECKING.md](../NEEDS-CHECKING.md).** No test was changed, because nothing
was shown to need changing.

## What was asked

A brief reported that running the whole Python suite left
`brain/fragments/filter-elements-by-type/fragment.yaml` modified in git, byte-identical apart from
line endings — HEAD LF, working copy CRLF — and asked for the test that does it to be found and fixed
at source. It explicitly forbade closing it with a `.gitattributes` rule, on the grounds that the
file is not the problem, the test writing to it is. **That instruction was right and was followed;
no `.gitattributes` was added.**

## What was found — the reported cause is disproved

**A pure LF-versus-CRLF difference cannot make a file show as modified on this machine.**
`core.autocrlf=true` is set in the **system** gitconfig (`Git/etc/gitconfig`, the Git-for-Windows
installer default — not something this repository chose), and **no `.gitattributes` is tracked
anywhere in the repo**. Git therefore normalises CRLF to LF on the way in, and the two spellings are
the same object to it.

Measured on an **untouched tree, with no test run**:

| | bytes | CR | LF | CRLF | `\r\r\n` |
|---|---|---|---|---|---|
| HEAD blob | 4282 | 0 | 102 | 0 | 0 |
| working copy on disk | 4384 | 102 | 102 | 102 | 0 |

…and `git status` **clean**. So the state the brief describes as *"the file afterwards"* is the
**permanent resting state of every checkout on this machine** — equally true before the suite and
after it, and on a fresh clone. `cmp` reporting DIFFERENT while `diff <(tr -d '\r' ...)` reports
nothing is **evidence of Windows, not evidence that a test wrote to the file.** The brief's own
verification step cannot distinguish a dirtied file from a clean one, which is why it looked like a
finding.

What *would* genuinely dirty it was established by pushing each variant through git's own clean
filter (`git hash-object --path`):

    lf     -> 0462ad5d...  == HEAD blob   clean
    crlf   -> 0462ad5d...  == HEAD blob   clean
    mixed  -> 0462ad5d...  == HEAD blob   clean
    crcrlf -> 0a877af4...  != HEAD blob   DIRTY

Only **double-CR** (`\r\r\n`) survives normalisation as a difference, and that needs a
**CR-preserving read paired with a translating write**. A static sweep finds no such pair on this
path: there are **no `newline=''` reads anywhere in the repository**; the only two `newline=''`
*writes* (`tools/resign-machine-proofs.py:100`, `tools/generate-decision-summary.py:159`) are the
safe non-translating kind; and the **only** writer of a real `fragment.yaml` in the entire codebase
is `heron_validate.accept` / `restamp`, which reads and writes both in text mode and so round-trips
CRLF unchanged. `tools/batch-prove.py` never writes one at all, and every `fragment.yaml` write in
`tests/test_validate_agent.py` goes into a `tempfile.mkdtemp()` workspace.

## What is NOT settled — read this before quoting the above

The run that would actually answer *"does any test write to that file"* — **every test alone,
fingerprinting the fragment after each** — **was killed at roughly 10 minutes of 99 tests** when the
sitting ended. It had reported **no change to any byte up to that point, and the tree was clean**,
but it never reached `DONE`. **So "no test writes to that file" is UNPROVEN and must not be repeated
as proven.** It is filed as **`A17`** in [NEEDS-CHECKING.md](../NEEDS-CHECKING.md) with the command and
what a pass looks like. To resume:

    git checkout -- .; foreach ($f in Get-ChildItem tests\test_*.py) { python $f.FullName *> $null }; git status --short

**That is PowerShell, and it has to be.** This section first carried the bash form
(`for t in tests/test_*.py; do ...; done`). PowerShell 5.1 rejects it at parse time — `&&` is not a
valid statement separator there, `<` is reserved, and `/dev/null` is not a path — so **nothing runs
at all, including the `git checkout -- .` at the front.** It produces a wall of red `ParserError`
and changes nothing, which is safe but reads like a disaster. The repository already had this
lesson: **`A14` states its loop in Windows form for the same reason.**

Pass is `git status --short` **empty**. A **fail is worth more than a pass** — it names the test, and
that test is then the real defect the brief was reaching for.

## Two things found on the way, neither of them the reported fault

| | where | what |
|---|---|---|
| **1** | [`tests/test_scope_store.py:174`](../../tests/test_scope_store.py) | Genuinely **writes into the live fragment library** — it creates `brain/fragments/zz-broken-temp/` with a deliberately malformed `fragment.yaml`, to prove a malformed fragment is skipped rather than indexed. It is a **new folder**, removed in a `finally`, and it never touches an existing fragment, so it is **not** the cause of anything reported here. But *"a test should not write into `brain/fragments/`, which is library source and not scratch"* is the brief's own principle, and this is the one place it bends. The author already met the interrupt case — there is a comment explaining why the folder is created **inside** the `try` with `exist_ok`, because an interrupted run once poisoned the test for good. **Worth a decision, not an emergency** |
| **2** | [`tests/test_embed.py:111`](../../tests/test_embed.py) | Calls `os.utime()` on the **real** `brain/fragments/set-selection/fragment.yaml` to prove that touching a file without changing it embeds nothing — which is the right thing to prove, and the content is never altered. Its side effect is that it **invalidates git's stat cache** for a library file and forces a re-hash on the next `git status`. Harmless, but it is the most likely reason a `git status` oddity was noticed around this area at all |

## The honest summary

**The brief's worry was sound and its evidence was not.** A phantom modification on a clean tree
really would train a reader to ignore `git status`, and that is worth chasing. But on this machine
the specific check used — CRLF on disk against LF in HEAD — is **always** true and proves nothing,
so it cannot be the thing that showed the file as modified. Either something else did, or the file
was never modified. **`A17` is what decides which, and it has not been run to completion.**

## A17 RAN TO COMPLETION, LATER THE SAME DAY — and it passes

**The section above says the run was killed unfinished. It was, and then it was run again properly.
That paragraph stays as written; this supersedes it.**

**On the owner's PC, 2026-09-15, 18:10:34 to 18:25:27, exit 0. All 99 tests, and the tree was clean
after every one of them.** The run checked `git status --short` after each individual test rather
than only at the end, so this is **99 observations, not one** — the changed-file set never once
differed from the empty baseline. Final `git status --short` **empty**.

The fragment the brief named is **byte-identical to before the suite**:

| | sha256 | bytes | CRLF | `\r\r\n` |
|---|---|---|---|---|
| before the run | `b2599e4878a6f72d` | 4384 | 102 | 0 |
| after 99 tests | `b2599e4878a6f72d` | 4384 | 102 | 0 |

**So the reported phantom modification does not exist on current HEAD**, and the cause the brief
named was already disproved on its own terms — `core.autocrlf=true` makes a CRLF-versus-LF
difference invisible to git on this machine, so the check that raised the alarm **could never have
detected a write in the first place**.

**Nothing was fixed, because nothing was broken.** No test was changed. **No `.gitattributes` was
added** — the brief was right to forbid it, and it would have hidden a real defect had one existed.

### What this still does not say

One run, in file-name order, on one machine. It **cannot** speak for a test that writes only under a
different ordering, a different Revit, or a failing path. And two tests do touch the live library
without dirtying it — `tests/test_scope_store.py:174` creates and removes
`brain/fragments/zz-broken-temp/`, and `tests/test_embed.py:111` `utime`s a real fragment. **Neither
is the reported fault.** The first is still worth a decision: *a test should not write into
`brain/fragments/`, which is library source and not scratch* is the brief's own principle, and it is
the one place it bends.

### The lesson worth keeping

**The brief's worry was sound and its evidence was not.** A phantom modification on a clean tree
really would train a reader to ignore `git status`, and that was worth chasing. But the specific
check used — CRLF on disk against LF in HEAD — is **permanently true on this machine**, before and
after anything, so it proved nothing. **Verify what git would actually store**
(`git hash-object --path <path> <file>` against `git rev-parse HEAD:<path>`), never `cmp` against
`git show`.

## The test that wrote into the library, and the two failures found on the way out

### The fix

`tests/test_scope_store.py` proved that a malformed fragment is skipped rather than indexed, and it
proved it by writing `zz-broken-temp/` **straight into `brain/fragments/`** — library source — and
deleting it afterwards. An interrupted run left it there. An earlier fix had moved the `makedirs`
inside the `try` with `exist_ok` so the NEXT run could clear it, which made the mess **survivable
rather than stopped**.

It now builds a temp library and points `heron_fragment.FRAGMENTS_DIR` at it for the duration,
restored in the `finally`. `rebuild()` calls `load_all()` with no root and `load_all` resolves
`root or FRAGMENTS_DIR` at **call** time, so one module global redirects it. Same proof, and it
cannot outlive the run. Commit `7b85557`.

### What the fix exposed, which is worth more than the tidy-up

The good fragment is **built, not copied**, and the reason is a defect the old test could not see.

Copying was tried first. It does not work: a PROVEN fragment's proof is fingerprinted against its own
bytes and path, so a copy out of `brain/fragments/` fails validation and the temp library indexes
**nothing**. And the test still said `ok` — because `after == before` never asked whether either
count was *real*. **A library indexing zero satisfied it: `0 == 0`.** A `before == 1` check now sits
beside it, and it is what caught this.

Proved both directions: with the malformed fragment the count stays 1; adding a **valid** second
fragment moves it 1 → 2, so the comparison genuinely observes additions and is not vacuous.

### THE SUITE IS NOT GREEN, AND A17 COULD NEVER HAVE TOLD YOU

**Measured 2026-09-15, 19:29 to 19:43 — 99 suites, 3 failing:**

| suite | why | whose |
|---|---|---|
| `test_bridge_roundtrip` | needs a built .NET test host — **expected**, recorded in §Tests above | the machine |
| `test_builder` | *"one file is planned, and it is not a test"*, then `KeyError: 'brain/heron_duct_sizing_reviewer.py'` | **`45a6746`** |
| `test_instructions` | *"a duplicate id is refused by name"* and *"both files are named, so neither is the silent loser"* — its own 16 evaluation assertions still pass | **`45a6746`** |

`test_builder` and `test_instructions` are **real assertion failures needing no special machine**, and
both files plus their `brain/` modules were last touched by `45a6746` (*"The seams first, then the
departments they made cheap"*, #141). **They are NOT fixed, deliberately** — the owner's instruction
was to note them, and quietly patching another session's failing test buries the problem rather than
closing it. Filed as **`A18`** in [NEEDS-CHECKING.md](../NEEDS-CHECKING.md).

**How this went unseen is the part to keep.** `A17`'s command runs every suite and checks
`git status`, and **throws every exit code away** (`python $f.FullName *> $null`, no `$LASTEXITCODE`
test). It answers *does the suite dirty the tree* and **cannot see a failing test at all** — so the
earlier "99 of 99 clean" in this file is true and says **nothing whatever** about pass or fail. Those
are two different questions and one command cannot answer both. `tools/check-gaps.py` does report
unfinished suites, but its exit code follows the UNFINISHED list, so it returns 1 on a healthy tree
and a reader learns to ignore it.

**THE SENTENCE ABOVE IS WRONG AND IT STAYS ONLY AS THE RECORD OF A WRONG BELIEF.** It said
`check-gaps.py` could not be trusted to show a failing test. Measured 2026-09-15:
`tools/check-gaps.py:131-181` **runs every suite, reads every return code, prints `FAIL <name>`**,
appends it to UNFINISHED, handles a `SKIPPED=3` sentinel and a 300-second hang bound, and skips
only `test_bridge_roundtrip` deliberately. It would have named both failures in plain sight. The
instrument existed and worked. **Nobody had run it on this PC** - which is precisely what row `K1`
of [NEEDS-CHECKING.md](../NEEDS-CHECKING.md) asks for, and it was still open. The failure was
procedural, not instrumental, and that distinction is the whole lesson: before building a second
tool, check whether the first one was ever switched on.

### One mistake of mine, recorded because it nearly cost the fix

The A17 script opened with `git checkout -- .`. It was re-run while the test fix was still
uncommitted, and **discarded it**. Nothing else was lost — every commit was intact — and the work was
redone. The script now **refuses on a dirty tree** instead of forcing one: starting clean is a
precondition to check, not a state to impose. Clearing up after it also left a stale `index.lock`
(0 bytes, one minute old, no git process, this worktree only), removed after checking those four
things rather than on sight.


## The gate nobody ran, the host nobody built, and two failures nobody had seen

**2026-09-15, on the owner's PC, after #142 landed and the tree went from 99 suites to 188.**

### `test_bridge_roundtrip` is not "the machine" any more - it PASSES

It has been recorded for weeks as *needs a built .NET test host*, and counted as the one honest
machine-dependent failure. **The owner closed Revit and said to build it.** One command -
`dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024` - exit 0, and the suite then ran
**31 assertions and passed every one**, ending *"Step 1 PASSED - the bridge works, without Revit."*
Ping, info, the lease across three chats, preemption, malformed JSON, a bad token, and the bridge
toggled off and back on. **Nothing was broken. The host had simply never been built here**, and the
absence of a build had been carried in the register as though it were a property of the machine.

### `check-signatures` is now in CI, without touching a workflow file

`tools/check-signatures.py` has existed since 2026-09-13 and **nothing ran it** - the defect its own
commit message names: *"A gate nobody runs is the same as no gate."* The commit that wires it
properly sits on `ci/run-check-signatures`, rebased onto current main and **unpushable**: the token
carries `repo` but not `workflow`, and GitHub refuses
*"to allow an OAuth App to create or update workflow `.github/workflows/gates.yml`"*.

So it rides inside `tools/check-docs.py` as **section 9**, exactly as section 8 already does for the
generated decision table and for exactly the same recorded reason. `check-docs` runs in *"The gates
that must pass"*, so the signature gate now runs on every pull request. Verified: `check-docs.py`
exits **0** before and after, and section 9 reports *"Signatures in the library: 299"* with the one
STALE signature that D-30 is supposed to surface. **It is not a documentation check and the comment
says so** - if anyone ever edits the workflow by hand, give it its own step and delete the section.

### Two failures that were in nobody's list

A partial `check-gaps` sweep - stopped deliberately at 14 of 188 so the fixes could be applied
without corrupting a measurement mid-flight - named two things `A18` never knew about:

| suite | what |
|---|---|
| `test_agents.py` | **HUNG** - still running after the 300-second bound, gave up. It is 225 lines and imports only `io`, `os`, `sys` and `heron_agents`; nothing in it reads stdin. Undiagnosed |
| `test_authoring.py` | **FAILS** in 0.3s. A real assertion failure, not a machine |

**Both arrived with #142**, which added 89 suites written on Linux - the same provenance as the two
fixed above, and the prediction written down before the run was that more Windows-only failures
would be found. **A theory that did not survive checking, recorded so it is not re-run:**
`tools/api-surface/.assemblies/` really is 264 MB of Revit DLLs and `agent-count.py` really does not
skip that folder, but it filters by extension before opening anything, so the DLLs are never read
and that is **not** the cause of the hang.


### The two above were BOTH fixed, and one of my own notes above is WRONG

**`test_agents.py` does not hang.** The row above says HUNG and it stays as written, because being
wrong about it in the same hour is the finding. On a quiet machine it **passes in 279.3 seconds**
against check-gaps' 300-second bound - **twenty seconds of margin, 7%**. The first run died because
a build and a second sweep were loading the machine at the same time. So it is not broken and it is
not healthy either: **a suite that passes with 7% margin will fail at random forever**, and every
time it does, somebody will chase a defect that is not there. It needs making faster, not fixing.
Nothing here diagnoses why 225 lines that import four modules take four and a half minutes.

**`test_authoring.py` was the SAME BUG A THIRD TIME.** `brain/heron_authoring.py:109` -
`os.path.relpath(path, ROOT)`, unguarded, inside `_where()`, which exists to word a refusal. A draft
written to a temp folder put the path on `C:` with the repository on `D:`, and the crash replaced
the message it was building. The module **already imported `heron_fragment as FRAG`** and simply did
not use it here.

**And a fourth copy was found before it could bite.** `brain/heron_tag.py:95` held a **byte-identical
copy** of that helper - same docstring, same missing guard - reached through a caller-supplied
`workflow` path. Both are guarded now. Four instances of one defect in one day is no longer a bug,
it is a pattern: **`os.path.relpath` against `ROOT` is unsafe anywhere the path can come from a
caller, a test, or another drive**, and `heron_fragment.repo_relative` is the repository's one
answer. The remaining call sites were checked and left alone deliberately - they take module-level
constants (`WORKFLOW`, `TEMPLATE`, `AGENTS`, `PROPS`, `EVIDENCE`) that are under `ROOT` by
construction, and filing them would put false positives in the register.

**`test_mcp_stdio.py` was a stale claim, and the test said so itself.** Its own comment set the
standard - *"re-base it on what is true, with the reason written down, never edit it until green"* -
and then **the very assertion that comment was defending went stale in the same way.** It asserted
the reply still contains *"no way to reach Revit"*, calling it *"the limit that has NOT moved"*.
It moved: **#144, "Changes ON now changes something"**. `_cannot_run()` now says a writing fragment
reaches Revit through `revit_change`, which KEEPS what it did, while the ribbon switch is on. The
old sentence survives **only in a comment**, so the check could never have passed again. Re-based on
the durable claim instead - **writing is GATED, and Heron says so** - asserting both that the reply
names the switch and that it names the refusal, so deleting the permission wording still fails it.
A Heron that claimed it could write freely is the danger worth a test.


### `test_agents.py`: 279 seconds to 5.5, and it was one missing argument

The row above says it needs making faster and does not say why. It is one line, and the mechanism is
worth keeping because nothing about it looked like a performance bug.

`heron_agents.record()` line 174 reads `deals = contracts() if deals is None else deals`, and
`contracts()` re-reads every contract from disk - **0.6 seconds a call**. `tests/test_agents.py`
built two list comprehensions over all **250** agents calling `record(a, agents, claims, host)`
**without `deals`**, plus one more per spanning agent. Five hundred calls, 0.6s each. **That is the
279 seconds**, and the value it kept rebuilding was already sitting in `deals` at the top of
`main()`, computed once at line 64.

| | |
|---|---|
| before | **279.3s** of check-gaps' 300s bound - 7% margin |
| after | **5.5s** |
| assertions | **43, unchanged** - `deals` is the identical value `record()` built for itself |

**What this cost before it was found.** It HUNG - exceeded the bound and was killed - on **two of
three** runs, passing only on the one where nothing else was using the machine. Each hang cost five
minutes and reported a failure that did not exist, and `A18`'s whole lesson is what a report that
cries wolf does to the person reading it. The first of those hangs was recorded in this file as an
undiagnosed defect in a suite that had nothing wrong with it.

**The shape to remember:** a function that lazily rebuilds an expensive input when an argument is
omitted is invisible at one call site and quadratic at two hundred and fifty. `records()` - plural -
was never slow, because it builds `deals` once and passes it down. The singular call in a loop is
the trap.
