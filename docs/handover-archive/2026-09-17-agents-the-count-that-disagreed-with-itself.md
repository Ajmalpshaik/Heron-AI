# The agent count that disagreed with itself, and the fourteen that are not waiting on a writer

**2026-09-17.** The AGENTS session, on `agent/count-reconciliation`. No Revit, and none needed.

The sitting was briefed as *"three tools disagree on how many agents are built — establish which is
right, then build the ones that are missing."* The first half has an answer with a named defect in it.
The second half has an answer nobody expected: **there is nothing in the remainder that a session
without Revit and without `revit/` may write.** Every one of the fourteen is held, and eleven of the
holds are already written down somewhere in this repository.

**Nought agents were built, one defect was fixed, and one fix was written and then thrown away** —
which is not the shape the brief expected and is what the evidence supports:

| | |
|---|---|
| agents built | **0** of 14 — 10 need `revit/`, 4 are held by [D-75](../DECISIONS.md) and [D-77](../DECISIONS.md) |
| `check-gaps.py` counting 225 where everything else counted 227 | **fixed**, and the recursive half proved with a planted probe |
| a registry row whose unblocking trigger had already fired on a WIP tag | **corrected** |
| `heron_buildmatrix.py` crashing while writing its own refusal, on two-drive Windows | found, fixed, and **reverted** — [PR #174](https://github.com/Ajmalpshaik/Heron-AI/pull/174) had already fixed it properly, and my version was the fourth private copy of a helper the repository already owns (§4) |
| suites | 199 run · 2 were red · one was one `dotnet build` away · one is #174's |

---

## 1. The three numbers

| stated | what produced it | verdict |
|---|---|---|
| **227 built of 250, 14 left** | [`tools/agent-count.py`](../../tools/agent-count.py) | **right** |
| **225 agent ids claimed by code** | [`tools/check-gaps.py`](../../tools/check-gaps.py) | **wrong, by exactly 2** |
| **208 claimed** | *"a plain grep of `brain/` + `mcp/`"* | **not reproducible, and could not have been an answer** |

Measured on `7d3d7c0`, which was `origin/main` and `HEAD` at the time, on a clean tree. Nothing here
depends on uncommitted work.

### 227 is right, and it is right from both directions

`agent-count.py` walks six source roots — `revit`, `mcp`, `brain`, `platform`, `tests`, `tools` — into
every subdirectory, over `.cs`, `.py` **and `.ps1`**, reading the first 41 lines of each file for an
anchored `Heron-Agent:` comment.

Two things could have made it under-report, and neither does:

- **The 41-line window.** A whole-file rescan of the same roots finds the same 227. Nothing declares an
  agent below line 41.
- **The `brain/fragments/` skip.** Nothing inside the fragment library declares one at all — the 372
  fragments carry `heron-agent:` in their own `fragment.yaml`, lowercase, in a data field rather than a
  header comment, which is the arrangement [`check-metadata.py`](../../tools/check-metadata.py) skips
  them for.

It also skips the file named `agent-count.py`, because a scanner that scans itself finds itself. That
costs nothing here: `HERON-DOC-AGT-002` is claimed by three files, and the other two are enough.

### 225 is wrong because `check_agents()` cannot see PowerShell

One line, in `check-gaps.py`:

```python
if not name.endswith((".py", ".cs")):
    continue
```

**`.ps1` is a source extension in this repository and that tuple does not have it.** Two agents are
declared in PowerShell and are invisible to this tool, and to nothing else:

| agent | file | department |
|---|---|---|
| `HERON-INS-ORC-001` | [`tools/setup.ps1`](../../tools/setup.ps1) | Installation & Update |
| `HERON-REVIT-DEP-024` | [`tools/deploy-addin.ps1`](../../tools/deploy-addin.ps1) | Revit Engineering |

Both are real registry rows. Both are genuinely built. 225 + 2 = 227, and the difference is exactly
those two, with nothing else cancelling in either direction.

**The gates are not affected.** `check-metadata.py` declares `SOURCE_EXT = (".cs", ".py", ".ps1")` and
reads both files, and so does
[`generate-contract-reference.py`](../../tools/generate-contract-reference.py). `check-gaps.py` is the
only scanner in the repository with this hole, which is why the number was quietly two low for however
long rather than failing anything.

### A second hole in the same function, which costs nothing TODAY

`check_agents()` also lists **flat**, with `os.listdir`, over eight hardcoded folder names:

```
brain, mcp/client, mcp/server, platform/Heron.Core,
revit/Heron.Bridge, revit/Heron.Revit.Addin, tests, tools
```

Today that is free — no subfolder of those eight holds a `.py` or `.cs` that declares an agent, so the
flat listing and a recursive walk agree. **It stops being free the first time somebody adds one.** A
module under `brain/agents/`, a command under `revit/Heron.Revit.Addin/Commands/`, a new project under
`platform/` or `revit/` that is not on that list of eight: each would be counted by `agent-count.py`,
by `check-metadata.py` and by the contract generator, and silently not counted here.

That is the same defect as the `.ps1` one — a scan whose reach is narrower than the tree it claims to
cover — so both were fixed together rather than one now and one after it bites.

### Both are fixed, and `check_agents()` now says 227

`check_agents()` walks **the same six roots, the same three extensions and the same skips as
`agent-count.py`**, deliberately, so the two cannot drift apart again. It now reports:

```
227 agent id(s) claimed by code, 250 known to the registry
ok    every claimed agent exists in the registry
```

**The recursive half was proved rather than assumed.** A throwaway file was planted at
`brain/agents/_probe_delete_me.py` — a path the old flat `os.listdir("brain")` could not reach — and
the tool named it:

```
HERON-PROBE-ZZZ-999 is claimed by brain/agents/_probe_delete_me.py but is not in the registry
```

The probe was deleted immediately and the tree is clean. Proving the pattern can see what you know is
there is this repository's own rule, and it is the rule the original scan was written without.

`tools/**` is outside this session's declared ownership. The owner was asked and said to use
judgement; the change is one function, and the reasoning is in the file.

### 208 is not reproducible, and the question behind it is the wrong question

Nothing on this tree produces 208. The nearest honest figures are:

| scope | count |
|---|---|
| unique ids declared by a `Heron-Agent:` header in `brain/` + `mcp/` | **172** |
| unique ids mentioned anywhere in a `.py` under `brain/` + `mcp/` | **204** |
| unique ids mentioned anywhere in any text file under those two roots | **220** |

No filter over those two folders lands on 208, and rather than reverse-engineer a command nobody
recorded, the honest entry is that it could not be reproduced.

**One warning for whoever tries again: `grep -r` over `brain/` reads `__pycache__`.** A `.pyc`
carries the agent id strings from the module it was compiled from, so the same command run before and
after a test sweep returns different totals — 220 became 245 mid-sitting for no reason but a suite
warming the cache. Every figure above was re-derived with `-I` and `--exclude-dir=__pycache__`. It is
a plausible origin for a number nobody can reproduce.

**It would not have been an answer even if it had reproduced.** `brain/` + `mcp/` is two of the six
roots agents are declared in. 25 agents are declared first in `revit/`, 8 in `platform/`, 13 in
`tests/`, 11 in `tools/`. A count over two roots cannot answer *"how many of the 250 are built"*; it
answers *"how many are built in the brain"*, which is a different sentence with a different number.

**The general shape is worth keeping:** two of these three numbers were produced by a scanner whose
reach was narrower than the claim it was being read as. Neither scanner said so.

---

## 2. The fourteen, and why not one of them was written today

`agent-count.py` reports 250 agents, 227 built, 9 host-provided, **14 left**. Every one of the fourteen
was checked against [`DECISIONS.md`](../DECISIONS.md), [`PROPOSALS.md`](../PROPOSALS.md) and the
registry — searching the id **without its `HERON-` prefix**, which is the trap
[F27](../PROPOSALS.md) recorded after a prefixed grep wrongly cleared ten rows that were all written
about. The pattern was proved against a row known to be there before it was trusted.

### The four non-Revit rows are all deliberately held, and three say so in the registry

| row | held by | why |
|---|---|---|
| `HERON-DEV-INT-012` *integration tests against a mocked Revit boundary* | [D-75](../DECISIONS.md) | Its candidates — [`tests/test_bridge_roundtrip.py`](../../tests/test_bridge_roundtrip.py) and `tests/Heron.Bridge.TestHost` — **fit the row exactly** and are both layer `test`. *"Allowed, awkward, and the owner's call."* D-75 adds that writing a third file to dodge the awkwardness would be the agent explosion `HERON-AHR-WFP-015` exists to refuse |
| `HERON-DEV-PRF-015` *execution time and resource cost* | [D-75](../DECISIONS.md) | **Two working candidates, both on Heron**: [`tools/measure-brain.py`](../../tools/measure-brain.py) and [`brain/heron_devperf.py`](../../brain/heron_devperf.py). D-75 explicitly does not separate them, and says picking one *"is exactly the guess that produced F27 in the first place"* |
| `HERON-DOC-REL-005` *release notes* | [D-77](../DECISIONS.md) | Deferred: no releases to write notes about |
| `HERON-DOC-CHG-008` *change log* | [D-77](../DECISIONS.md) | Deferred: one version, and `Fixed` vs `Improved` cannot be told from a diff without the synonym table [D-34](../DECISIONS.md) refuses |

Two of those are the owner's call and were left alone. The other two are discussed in §3, because one of
their premises has moved.

### The ten Revit Engineering rows need a folder this session may not open

`REVIT-FAM-012` families · `REVIT-VIE-013` views · `REVIT-WRK-014` worksets · `REVIT-EXP-018` export ·
`REVIT-IMP-019` import · `REVIT-SCH-026` schedules · `REVIT-LVL-027` levels and grids ·
`REVIT-RM-028` rooms and spaces · `REVIT-SHT-029` sheets · `REVIT-DIM-031` dimensions and annotation.

**None of the ten has a recorded blocker anywhere in `docs/`.** They are genuinely the remaining work.
They are also, every one of them, a live Revit operation at `MODIFY` or `PUBLISH` risk, and the
precedent in their own department is unambiguous:

| where the 26 built Revit Engineering agents live | |
|---|---|
| `revit/Heron.Revit.Addin/*.cs` | **20** |
| `brain/*.py` | 4 |
| `platform/Heron.Core/*.cs` | 1 |
| `tools/*.ps1` | 1 |

The four that live in the brain are the ones whose subject is **recorded evidence or knowledge**, never
a live call: [`heron_timing.py`](../../brain/heron_timing.py) reads back the audit trail's `ms`,
[`heron_rollback.py`](../../brain/heron_rollback.py) reads the audit log, and the two API agents hold
version knowledge. Every agent in this department that *does something to a model* — parameters, links,
systems, phases, groups — is a `.cs` file in
[`revit/Heron.Revit.Addin/`](../../revit/Heron.Revit.Addin/RevitSystems.cs).

Ten more of the same would be ten more files in that folder. **This session was scoped out of `revit/`,
`mcp/`, `tools/`, `.github/`, `platform/` and `brain/fragments/`**, so the work exists, is unblocked,
and is not this session's to do. Writing a brain-side stand-in instead would be inventing an
architecture nobody has decided — which is the move F27 exists to warn about.

**Nothing was already built and merely unclaimed.** Only three modules in `brain/` carry
`Heron-Agent: none`, and their withheld claims are `HERON-DEV-PRF-015`, the audit trail and the
provider adapter. None of the ten is hiding under a blank header.

> **Built this sitting: 0. Left: 14 — 10 needing `revit/`, 4 held by decision.**
> That is the finding, not a shortfall against it.

---

## 3. Four things found on the way, one of them fixed here, two of them left alone on purpose

### a. D-77's unblocking trigger for `HERON-DOC-REL-005` has already fired, on the wrong thing

The registry row said, in as many words: *"there are **no releases**: `git tag` returns nothing …
**Unblocked by the first tag.**"*

`git tag` now returns **two**:

```
wip-before-148
wip-before-rebase-onto-149
```

Neither is a release. Both are working-tree safety markers from earlier sittings, and their messages say
so. **The reasoning is untouched — there is still nothing to write release notes about — but the
trigger as written is now satisfied by something that is not a release**, so the next reader who checks
the condition mechanically gets the wrong answer.

The row now states its condition as *the first release tag*, and its premise as what is true today
rather than what was true on 2026-09-16. That is the one edit to
[`docs/28-agent-registry.md`](../28-agent-registry.md) in this change, and it does not unblock the row.

### b. `HERON-DOC-CHG-008`'s row states a number that no scope on this tree reproduces

The row says *"683 files carry `Heron-Since: 0.1.0`"*. Today:

| scope | count |
|---|---|
| source files (`.cs`/`.py`/`.ps1`) with the header | **435** |
| every file containing `Heron-Since: 0.1.0`, case-sensitive | **453** |
| every file containing it case-insensitively, which is the one that also catches the 372 fragment yamls | **835** |

Files do not disappear, so 683 cannot have shrunk to 453 in a day; it was almost certainly counted over
a scope nobody wrote down. **The row's conclusion survives every one of them** — every file that
carries the field says `0.1.0`, so there is exactly one version and a change log would still be one
section listing everything. The argument is sound and the number is unverifiable, which is the opposite
of the usual failure and still worth recording.

**Left as it is.** Replacing an unreproducible number with a fresh one whose scope is equally undeclared
would be the same defect with a newer date.

### c. `generate-contract-reference.py` states a category that 23 of its own findings contradict

It prints:

```
100 agent(s) built without a contract (tool and C# layers)
```

**23 of those 100 are layer `brain`.** Among them `HERON-RAG-CTX-007`, `HERON-RAG-LIB-001`,
`HERON-KRN-DEP-013` and all five Standards rows served by `brain/heron_company.py`. The count is
right; the parenthetical explaining it is not, and it is the parenthetical a reader acts on — it says
*nothing is missing here, these are layers that do not use contracts*, and for 23 of them that is not
what is true.

Whether a brain-layer agent is **required** to carry a contract is a real question, and it is not
answered anywhere I could find. It is not answered here either. `tools/**` is not this session's, so
the line stands.

### d. Two agents are claimed by a test and by nothing that implements them

`HERON-RAG-RIX-011` and `HERON-RAG-DUP-012` are declared only in the header of
`tests/test_maintenance.py`. The generator already reports this, in its own words — *"ONLY A SUITE …
claimed by no file that implements it"* — and the implementations are in `brain/heron_ingest.py` and
its neighbours, under different ids.

This is the same shape D-75 called *"allowed, awkward, and the owner's call"* for `DEV-INT-012`. It is
already visible in a generated report, so it is noted rather than raised: **two of the 227 are built
in a place their claim does not point at.**

---

## 4. Two suites were red before this change, and only one of them was the machine

199 suites ran. Two came back non-zero, both of them before any file here was touched, and they are
different animals.

### `test_bridge_roundtrip.py` — the machine, and it took one command

```
FAIL  tests\Heron.Bridge.TestHost\bin\x64\Debug\Heron.Bridge.TestHost.exe not found.
      dotnet build tests\Heron.Bridge.TestHost -p:RevitVersion=2024
```

Ran exactly that line — the suite prints the command it needs, which is the design — and it built in
7 seconds, 0 warnings, 0 errors. **The suite then passed.** Nothing is committed by it: the output is
`bin/`, which is ignored, and the project is a plain console exe over `Heron.Bridge`, which has no
Revit reference at all. It does not touch the deployed add-in.

Two notes for whoever maintains the ship list, measured here rather than assumed:

- **It exits `1`, not `3`.** The `heron-ship` skill says all four optional-dependency suites exit 3
  *"so `check-gaps.py` reports them as waiting rather than failing"*. This one does not, so a machine
  without the test host has it land in **UNFINISHED** — the bucket that means *somebody could fix this
  here*. Which, this time, was true: one command fixed it. But the skill and the suite disagree.
- **The other three on that list all pass on this machine.** `test_mcp_serves.py`,
  `test_served_claims.py` and `test_dotnet.py` each exit 0 — the MCP SDK and a .NET SDK are both
  present here. The "known three" is a statement about a container, not about this PC.

### `test_buildmatrix.py` — a real defect, Windows-only, and it needs TWO DRIVES to appear

```
File "brain/heron_buildmatrix.py", line 259, in regression
    "should be." % os.path.relpath(where, ROOT)}
ValueError: path is on mount 'C:', start on mount 'D:'
```

`regression()` correctly refuses a missing baseline with `NO_GOLDEN` — and then **crashes while
writing the sentence that explains the refusal.** `os.path.relpath` cannot express one Windows path
relative to another on a different drive, and it raises rather than returning anything.

It takes two drives to see. The repository is on `D:`; the suite builds its fixture under `TEMP`,
which is on `C:`. On Linux there are no drives and it cannot happen; on a Windows machine whose temp
and checkout share a letter it cannot happen either. **CI is green and will stay green.**

**The same call appears about twenty lines earlier**, in the `EMPTY_MATRIX` refusal —
`os.path.relpath(props or PROPS, ROOT)` — so a props file passed from another drive fails identically.
Both are the refusal path, which is the worst place for it: the failure only fires when something has
already gone wrong, so it converts a clean refusal into a traceback at exactly the moment a caller
needed the explanation.

This is the same family as the `test_context.py` defect the ship skill records for 2026-09-12 — a path
comparison that assumed one shape of filesystem — and it is the second time a Windows-only path
assumption has been found by running the suites on Windows rather than by reading them.

**Found here independently, fixed by somebody else, and my fix was reverted — which is the part
worth keeping.**

I wrote a private `_shown()` helper in this module and routed all four call sites through it. It
worked, `test_buildmatrix.py` went green, and it was **the wrong fix.**

[PR #174](https://github.com/Ajmalpshaik/Heron-AI/pull/174) — open on
`tools/gate-lists-and-cross-drive` while this sitting ran — had already fixed the same four call
sites, through `heron_fragment.repo_relative()`, which has lived in
[`brain/heron_fragment.py`](../../brain/heron_fragment.py) all along and does exactly what my helper
did. Its comment names my mistake before I made it:

> *"`heron_fragment` owns the one answer; a fourth private copy would be the actual mistake."*

It also records that `heron_authoring`, `heron_tag` and `heron_context` each carry a comment about
this same defect, that the first two were byte-identical copies of one another bug included, and that
all three were fixed on 2026-09-15 — **two days before `heron_buildmatrix.py` was written carrying it
anyway.**

So the count is: the lesson was written down three times, did not reach the module written next, and
then did not reach me either. **A fifth copy is what this session nearly committed.** Nothing in the
repository could have refused the spelling, because `os.path.relpath` is correct nearly everywhere it
appears — which is the whole reason it keeps arriving.

My change was reverted in full. `brain/heron_buildmatrix.py` on this branch is byte-identical to
`origin/main`, so **`test_buildmatrix.py` is red here and green on #174**, and that is the correct
place for it to be fixed. #174 also adds a behavioural check and a text check to the suite, which is
more than I did.

The only thing this sitting contributes is the independent confirmation: two sessions on two tracks
reached the same crash the same day, from opposite directions — theirs from `check-gaps`' one
UNFINISHED item, mine from a plain suite sweep.

---

## 5. What the next session should do with this

1. **The ten Revit Engineering rows are the real remaining work**, they have no recorded blocker, and
   they belong in `revit/Heron.Revit.Addin/`. A session with that folder can take them.
2. **`DEV-INT-012` and `DEV-PRF-015` are the owner's**, unchanged since D-75 and not for want of
   anybody writing something.
3. **`DOC-REL-005` and `DOC-CHG-008` stay deferred.** The premise correction in §3a does not unblock
   either of them.
4. **`generate-contract-reference.py`'s parenthetical is still wrong** (§3c), and the question under
   it — whether a brain-layer agent must carry a contract — is still unanswered. 23 agents turn on it.
5. **`test_buildmatrix.py` stays red on this branch and is green on
   [#174](https://github.com/Ajmalpshaik/Heron-AI/pull/174).** Nothing here needs doing about it; it is
   noted so the next person running the sweep does not diagnose it a third time.

Nothing in this note is proven against a Revit model, and nothing in it claims to be. It is a count,
a diff of three scanners, four rows read carefully, and two red suites told apart.
