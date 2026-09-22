# Session note — Sitting of 2026-09-16 into 2026-09-17 - a rename that Revit refuses, and fourteen questions that answered with a write

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---


# Sitting of 2026-09-16 into 2026-09-17 - a rename that Revit refuses, and fourteen questions that answered with a write

**Started as "rename the Existing phase to Design".** Ended with a chain that can hand elements
between fragments safely, pipes and fittings Heron can actually select, and nine findings - most of
them about Heron rather than about Revit. Rows **107 to 117** in
[FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md).

*The banner work of the same night is deliberately not described here. It is being taken further and
a description written now would be stale before it was read.*

## Revit will not rename a phase, and that took building the thing to find out

A `RENAME_PHASE` fragment was written, compiled on 2020-2027, and run against real models on **both**
Revit 2020 and Revit 2024. Each refused identically: `Element.Name` throws *"This element does not
support assignment of a user-specified name"*, and `BuiltInParameter.PHASE_NAME` behind it reports
`IsReadOnly`. **Two doors, two releases, one answer - so it is the object, not the release.**

**Nothing short of running it could have found that.** `check-fragments-compile` passes on all eight
releases and `check-api-surface` passes on all eight, and both are *correct*: the member genuinely
exists. A member that exists and throws is exactly what `api-surface/changes.json` lists under
`cannotSee`.

**The fragment was removed.** What replaced it is a routing note in `REPORT_PHASES`, beside the one
that already said Revit exposes no CREATE call for a phase. The same fact, from the other end, and
nobody had read across. Row 107.

## `revit_change` had never once reported what a write did

It read `reply.get("answer")` - **a key nothing emits, on either side of the bridge**. The add-in
leaves `provides`, `bound`, `verdict`, `applied`, `rolledBack`; grep for `"answer"` across `mcp/`
returns one hit, the read itself. So the value was always `None` and **every write through that tool
reported only that it had run**.

Measured: `RENAME_PHASE` answered *"ran in PIPE"* while the fragment had produced `renamed: false`
and Revit's own refusal. Only reading the model back showed the difference.

**The comment directly above the bug was right** - *"on 2026-09-09 a rollback did not hold while this
side claimed it had"*. The lesson had been learned and written down; the key name was wrong. Row 111.

`tests/test_change_reporting.py` was confirmed to **fail on the old key** before being kept.

## Fourteen ordinary questions answered with a write, and the cause was not the ranking

Found by hand, then swept: **45 things a modeller says, 17 reached a capability above READ, 14 beat a
READ that was right there.** *"How many pipes are there"* resolved to `CAP_OPEN_PIPE_ENDS`. *"Count
the air terminals"* to `CONNECT_AIR_TERMINALS`. *"Which parameters are empty"* to
`REMOVE_PARAMETER_VALUE`.

**`check-routing.py` reported one, and was right about everything it was allowed to ask** - it tests
each fragment's OWN declared utterances, and none of those 45 sentences is declared by anybody.

**The cause was that they never reached the ranking's safety net.** `find()` tries `short_circuit`
first: an exactly declared sentence resolves by IDENTITY and no ranking runs. Two ways to miss it,
and the library had both - **ambiguity** (`__ambiguous__` is a deliberate miss; only **7 phrases of
2,442**, two of them dangerous) and **nobody declaring the sentence at all**.

**So the fix is declaring, not demoting.** A nudge could never have done it: the quality nudge is
deliberately smaller than one fusion rank (0.00026) and these writes won by **2.4 to 2.6**. Measured
both ways, **14 to 7**. Rows 113 and 116.

`tools/check-risk-crossings.py` makes the sweep repeatable, and **its question list is meant to
grow** whenever a session produces a new one.

**AND A THIRD SWEEP JOINED THEM 2026-09-19** — `tools/check-declared-questions.py`, [row 158](../FRAGMENT-ISSUES.md). The other two ask the SEARCH; this one reads the CARDS, so it runs in CI where there is no store, and it finds what neither can see: a fragment **above the write line declaring a question in its own `utterances:` block**. Those win by `identity`, which short-circuits before ranking, so no re-ranking repairs one. **Ten today**, the starkest being `SET_VIEW_SCALE` declaring *"what scale is this view"*. It is in the `reports` job, so it runs on every pull request rather than when somebody remembers.

## A fragment can now consume what another left

[docs/36](../36-remembering-between-steps.md) built. The chain reset stays exactly as it was; the caller
**opts in** by naming what it expects, and the executor checks before binding anything.

**Three quarters of it already existed** - `Chain` recorded the document, the producing fragment and
the time. What was missing was *what the producer ran with* and a caller-declared expectation.

Proved on `4355-BHVD-3D-50C10-BL001A`: **271 elements handed over**, and then, with the **same
carried values and nothing else changed**, a wrong declaration refused (`chain_inputs_differ`) and a
right one bound 143. All four refusals fired live. Row 115.

**It did real work the same hour** - `read-element-parameters` then `group-and-count`, the exact
hand-over `fragment-proving` records as failing.

## And it caught me giving the owner a confident wrong answer

Asked to isolate the pipes except the condensate drain, I said twice there was none. **Both checks
were real and both answered the wrong question**: the named systems `PCD 3`, `CDP 1` are genuinely
empty, and pipe TYPE names carry no service. The service lives on the element's **System Type** -
**105 Refrigerant Condensate, 103 Refrigerant Supply, 63 Condensate Drain** out of 271.

He was looking at them on screen. Row 114, and the rule: **"zero on the named system" is not "zero of
that system type"** - 688 MEP elements in that model sit on no named system and every one still
carries a type.

## What is still owed

| | |
|---|---|
| **Seven crossings** | ~~duct size, pipe diameter, an element's workset, what is missing its Mark, views on a sheet, insulation thickness, selecting pipes in a view~~ **MEASURED 2026-09-19, and it is not seven — [FRAGMENT-ISSUES rows 144 and 146](../FRAGMENT-ISSUES.md).** Five are still live; *pipe diameter* and *what is missing its Mark* did not come back; and **three this row never named are crossings today**, one of them *"how high is this off the floor"* answered by MOVING the thing. Of the eleven live, **seven have an owner already in the library that never declared the sentence** — named per sentence in row 146 — and **four have no READ to give them to at all**: nothing reports an MEP element's size, an element's workset or an insulation thickness, and `SET_VIEW_SCALE` is the only capability in the library with SCALE in its name. **THAT LAST SENTENCE WAS TOO STRONG AND IS CORRECTED 2026-09-19** ([row 158](../FRAGMENT-ISSUES.md)). Row 146 enumerated the capabilities with SIZE, WORKSET, INSULATION or SCALE **in their names** and was right that none of them reports the value; *"no READ to give the sentence to at all"* is a bigger claim and does not follow. `READ_ELEMENT_PARAMETERS` reads one named parameter off every element and **declares *"get the size parameter"*** - SIZE nowhere in its name, and a candidate owner for three of the four. **One is certainly a gap:** a view's scale is not an element parameter, and `SET_VIEW_SCALE` is still the only capability with SCALE in its name. **Try the existing READ before writing a new fragment.** **AND TEN MORE WERE FOUND THE SAME DAY, DECLARED IN WRITING** - `SET_VIEW_SCALE` has *"what scale is this view"* in its own `utterances:` block, so it wins by `identity` and no re-ranking repairs it. `python tools/check-declared-questions.py` lists all ten and runs on every pull request |
| **Five ambiguous phrases** | ~~all read-vs-read, so none is dangerous~~ **IT IS SIX AND TWO ARE WRITE-VS-WRITE — [row 134](../FRAGMENT-ISSUES.md), and both were judged 2026-09-19.** *"change the insulation thickness"* belongs to `SET_MEP_INSULATION`: the other claimant edits a wall, floor, roof or ceiling **TYPE**, so every element of that type in the project changes, where `set-mep-insulation` edits what was handed in. *"duplicate with detailing"* is far smaller — both claimants are right and the phrase carries no NUMBER. **Neither was edited**; all four files are under `brain/fragments/`, and the exact one-line repairs are written into row 134. **BOTH ARE REPAIRED NOW, 2026-09-20** - the four one-line edits were applied as written, and read from disk either side of them **6 phrases claimed twice became 4**, every one of the four READ against READ. No proof was staled: the fingerprint walks `impl/` only and cannot see `utterances:`. **Live on this machine already** - the shared store reports `built_from` = the worktree that made the edit, and reading it back, *"change the insulation thickness"* now returns `FRG-MEP-007` alone. **It reverts the moment a tree without the edit indexes next**, because an utterance edit leaves the ID set unchanged and the drift guard cannot see it - [row 131](../FRAGMENT-ISSUES.md) |
| ~~**Row 117**~~ **DONE, and this line was still asking for it 2026-09-19** | ~~a write that renamed nothing said *"the model was CHANGED"*~~. **[Row 117](../FRAGMENT-ISSUES.md) reads FIXED**: `WithVerdict`'s `applied` branch now says *"'X' was KEPT: Revit accepted the transaction rather than rolling it back"* and sends the reader to the counts already in the same reply, with a test that fails on the old wording. It landed in [PR #198](https://github.com/Ajmalpshaik/Heron-AI/pull/198) and this table was never told. **That is [row 153](../FRAGMENT-ISSUES.md)'s shape in the file the owner reads first** - a list of what is owed is a cache with no invalidation, and nothing sweeps it. Kept struck through rather than deleted, because a line that vanishes teaches nobody why it was here |
| **12 of the 24 sentences that REACH, Heron says it found nothing for** | Measured 2026-09-19 across all 43 skill utterances ([row 157](../FRAGMENT-ISSUES.md)). 24 reach a capability their skill declares - and for **half of them** the retriever's own note says *a coin toss*, where neither route preferred the winner, or that the words route *ranked the library rather than selecting from it*. **Both crossings carry one too.** The plainest case is Heron's own opening example: *"select all the ducts"* lands on `SELECT_BY_CATEGORIES` correctly, from a route with no claim on the sentence, while *"show me the ones you found"* - the one a fragment DECLARES - carries no complaint at all. **That is the argument for declaring a sentence rather than ranking for it, in data**, and the work owed is declaring them. It needs no Revit |
| **`set-wall-constraints`** | still the one STALE signature. Predates this sitting and was not touched |

## Two things about the machine, not the code

**The Revit ribbon button and the Claude app restart different halves.** The ribbon reconnects the
bridge; only restarting the app reloads `mcp/` Python. Three stale `heron_mcp_server` processes ran
for hours with fixed code sitting unused on disk, which reads exactly like the fix not working.

**`.agents/skills/` came back and the `.gitignore` tripwire caught it**, exactly as its comment says
it would. Five of its six files were byte-identical to `.claude/skills/`; the sixth differed only in
paths pointing at `.Codex/`. It was deleted, and `test_change_gate.py` went green the moment it was -
that untracked folder was the only red suite on the board.

## The owner answered the last four questions, and the way they were asked is the finding

**`Q-51`, `Q-54`, `Q-55` and `Q-56` close as [D-81](../DECISIONS.md) to [D-84](../DECISIONS.md).** The
register, which **until 2026-09-20 said 52 answered / 4 open**, now reads **56 / 0**. The owner
queue goes **144 to 140**, decisions waiting on
him **25 to 21**, and [OPEN-QUESTIONS](../OPEN-QUESTIONS.md) **drops out of the queue entirely.**

**All four were put to him at once and dismissed. The same four translated into Malayalam, at his own
request, were dismissed again. Asked ONE AT A TIME, in plain words with a worked example each, he
answered all four in minutes** - and asked for `Q-51` to be re-explained *in English with a different
example* before answering it. **The language was never the barrier. The batch was**, and that is worth
more than the four answers: this register has carried unanswered questions for months while the way of
asking went unexamined.

**Two of his answers were better than any option offered**, which is the argument for asking rather
than choosing a default on his behalf:

| | |
|---|---|
| **`Q-54`** | The question offered cloud opt-in **per scope**, as [D-24](../DECISIONS.md) frames it. He drew the line by **what the thing is** instead - all documents may go, **models and families never** - and asked for the earlier position to be changed outright. [D-82](../DECISIONS.md) supersedes D-24's framing, not its caution |
| **`Q-55`** | The question offered the read-time ceiling - a count and a clause number, never a clause. **He declined the ceiling**: he wants the **prior practice by name**, *"in Project A you used 700mm, do you want the same here?"* [D-83](../DECISIONS.md) |

`Q-51` took shape 1 - **carried text is stamped, not scanned**; Heron attributes rather than asserts.
`Q-56` took the cheapest honest answer - **nothing more than today, and the word "sandbox" goes**,
which makes the **rename the deliverable** rather than a tidy-up.

**Two things he asked while deciding are recorded inside [D-82](../DECISIONS.md)**, because the answers
bound the decision. *"More context means no hallucination - am I right?"* - half right, and
[row 114](../FRAGMENT-ISSUES.md) is the counter-example from this repository. *"It will not go to GitHub -
am I right?"* - correct, and **verified against `.gitignore` rather than asserted**: the store lives
outside the repository, the databases are ignored, and the SOURCE documents are blocked by extension.

### Two suites that looked red were the sweep, not the code

`check-gaps.py` reported `test_brain_reachable.py` and `test_docs_guard.py` as **FAIL**, and **both
pass when run on their own** - the first takes 103s inside a serial sweep. **That was told to the owner
as "2 failing tests" before it was checked, and corrected in the same session.** Run a suite alone
before reporting it red.

### And `check-docs.py`'s exit code is not what `| tail` reports

`python tools/check-docs.py | tail -20; echo $?` prints the exit of **`tail`**, which is always 0. The
gate was read as green while it was **exiting 1**. Redirect to a file and read `$?` from the command
itself.

### The four counts that stayed at 52 were quotations, not drift

`check-docs.py` gates on every stated count **including the archive**, and it carries a deliberate
exemption for a figure **quoted as history** - `used to`, `it said`, `until 20XX`, `superseded`. All
four stale lines were genuine quotations **missing the marker that says so**, so they carry one now
rather than being rewritten. **The archived handover keeps its title** and gains *"superseded"*. The
marker is load-bearing by design: *"Write 'it said 14 answered' and this stays quiet."*

### The balance as it stands tonight

The owner asked for this in one table, and asked that it be given in this shape whenever he asks again.

| | Balance |
|---|---|
| **Skill runner** | **1 - does not exist.** [Row 141](../FRAGMENT-ISSUES.md) - the only real build item, and the blocker for every skill proof |
| Skills proved | **0 of 10** |
| Fragments proved | **328 of 395** - 67 owed |
| Revit agents proved | **15 of 36** - 21 owed. The other ~214 never touch a model and are covered by passing suites |
| Agents to build | **2** - both Documentation, parked until the first release tag, blocking nothing |
| Open defects | **38** of 144 |
| Waiting on the owner | **140** - 21 decisions, 21 proposals, **0 questions** |
| Platform, add-in, install, rollback | **Nothing owed.** Loads on 2020, 2024 and 2027 |

> **THE TABLE ABOVE WAS TRUE WHEN WRITTEN AND FOUR ROWS HAVE MOVED SINCE, 2026-09-20 LATER THE SAME DAY.** Derived, not typed - run the commands in
> [work-notes/BALANCE-OF-WORK.md](../work-notes/BALANCE-OF-WORK.md) rather than believing either version.
>
> | | Was | Is |
> |---|---|---|
> | Fragments proved | 328 of 395 | **329 of 395**, 66 owed |
> | Open defects | 38 of 144 | **33 of 163** - the register grew by 19 rows and more closed than opened |
> | Waiting on the owner | 140 | **146** |
> | Questions | 0 | **1** - `Q-57`, *may a fragment or skill that WRITES declare a question* |
> | `set-wall-constraints` STALE | the one stale signature | **none is stale.** It reads PROVEN and `check-signatures.py` says nothing is waiting; `create-roof` is HELD on purpose |
>
> **AND THE ONE THAT IS NOT A COUNT: FOURTEEN QUESTIONS ARE ANSWERED BY SOMETHING THAT WRITES**, measured twice by two independent routes and named one by one in
> [FRAGMENT-ISSUES row 116](../FRAGMENT-ISSUES.md). Asking the diameter of a pipe CREATES a pipe; asking to select the pipes in a view CAPS THEIR OPEN ENDS; asking
> which workset something is on CREATES a workset. Every one of the fourteen has a READ sitting beside it that lost, so *there is no READ to give the sentence to*
> is disproved - but the right owner is obvious for some and wrong for others, so none was declared and the choice is still his.

**Safe on a live project today: 144 proven `READ` fragments** - that number read **114** in this file's
own cold-start instructions until 2026-09-20, understating by thirty what the owner was allowed to use.
Derive it, never read it: `grep -h '^heron-status:' brain/fragments/*/fragment.yaml`.

## Seven suites were red on the owner's PC, CI could see none of them, and all 244 pass now

**2026-09-22.** It started as one failure - `tests/test_bridge_roundtrip.py` reporting *"an unbounded
line returned {}"*, which reads like the request ceiling failing inside Revit's process. Chasing it
properly turned up six more suites red on this machine and green in CI. **Every one is fixed, each in
its own pull request, each proved by reverting the fix and watching the suite go red again - and a full
sweep of `main` afterwards passed 244 of 244**, with nothing failing, nothing waiting and nothing timed
out.

**The shape is the finding.** CI is a clean Linux clone: no worktrees, no cached assemblies, no stray
folders, a UTF-8 console. The owner's PC has all of those, and several sessions running at once. All
seven were green in CI forever and could only ever go red where somebody actually works.

| suite | what was wrong | fixed in |
|---|---|---|
| `test_bridge_roundtrip` | launched a host binary from 2026-09-16, older than the ceiling it was testing | [#285](https://github.com/Ajmalpshaik/Heron-AI/pull/285) |
| `test_api_digest` | the tool's REFUSING TO WRITE path raised on a two-drive path, and the suite assumed a machine with no SDK | [#286](https://github.com/Ajmalpshaik/Heron-AI/pull/286) |
| `test_library_total` | read other sessions' worktrees - **140 of 140** findings were there | [#287](https://github.com/Ajmalpshaik/Heron-AI/pull/287) |
| `test_config_and_health` | the same - **4 of 4** findings in worktrees | [#287](https://github.com/Ajmalpshaik/Heron-AI/pull/287) |
| `test_change_gate` | its verdicts read the working tree, and `.agents/` was in it | [#288](https://github.com/Ajmalpshaik/Heron-AI/pull/288) |
| `test_check_dependencies` | assumed `model2vec` is absent; it is installed here | [#288](https://github.com/Ajmalpshaik/Heron-AI/pull/288) |
| `test_decision_summary` | did not fail, it CRASHED - a tick printed with the output redirected, which Windows encodes in cp1252 (a console never shows it) | [#288](https://github.com/Ajmalpshaik/Heron-AI/pull/288) |

**Nothing under `revit/` changed.** No add-in rebuild, no redeploy, no Revit restart: every change was a
suite or `tools/api-changes.py`.

### The bridge was never broken, and rebuilding could not have shown it

`Directory.Build.props` writes every build to `bin/x64/<Configuration>/<RevitVersion>/`. The suite read
a flat `bin/x64/Debug/`, where a binary from 2026-09-16 still sat. So a correct, successful rebuild **in
either configuration** landed somewhere the suite never looked, and the failure survived it - which is
what made it read as a transport defect for two sessions. Against that stale binary a
2,000,000-character line, nearly twice the ceiling, was accepted and answered `pong`; against the
current one it is refused. **Rebuilding is not the check. Where the build landed is.** The flat leftovers
are still on disk, harmless now that nothing reads them.

### A test rewrote committed evidence

Section 6 of `test_api_digest` asserted *"this machine cannot answer"*. This machine can - so by the time
it ran, with `OUT` already restored to the real `tools/api-surface/changes.json`, `main()` performed the
full comparison and **rewrote the committed digest as a side effect of a test.** It came out
byte-identical, which is luck and not design. The section now forces the condition with empty folders
and writes to a temp file, so it is meaningful on every machine and cannot touch the committed digest.

### `.agents/` came back, and this time the suite stopped caring

*Two things about the machine, not the code*, above, records `.agents/skills/` returning once and being
deleted to turn `test_change_gate` green. **It is back again**, untracked. It was **left alone** this
time - deleting it is the owner's call - and instead the four calls in `test_change_gate` that weigh a
verdict now pass `--range HEAD..HEAD`, so no untracked file can decide one. Proved both ways: with the
folder moved aside the old suite passed, and the new suite passes with it present. The `.gitignore`
tripwire for it is untouched - it exists to keep the hook script visible, and `git status` still shows
the folder.

### The rule the three assumption failures share

`test_api_digest`, `test_check_dependencies` and `test_change_gate` each froze the bare machine they were
written on into an assertion, and then failed the tool for giving the correct answer on a better
equipped one. **Force the condition, never assume it** - point at empty folders, name a package that
cannot exist, hand the subprocess an empty range. And before believing any failure from a suite that
walks the repository, count how many of its findings sit under `.claude/worktrees/`: this time it was
all of them.

### What this leaves

| | |
|---|---|
| Suites red on the owner's PC | **0 of 244**, swept on `main` 2026-09-22, 20:15 to 20:40 |
| Pull requests open from this work | **0** - #285 to #288 are all merged |
| `.agents/` | present again, untracked - **the owner's to delete or keep** |
| Flat `tests/Heron.Bridge.TestHost/bin/x64/Debug/` leftovers | still on disk from 2026-09-16; nothing reads them now |
| Anything to rebuild or redeploy | **nothing** |

**One caution for the next sweep on this machine:** other sessions run suites at the same time - three
were running alongside this sweep. Most suites use their own temp folders, but run a red one alone before
reporting it, as *Two suites that looked red were the sweep, not the code* already says.
