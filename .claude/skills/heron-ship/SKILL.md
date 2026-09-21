---
name: heron-ship
description: What to run before pushing Heron, in what order, and which failures are the machine rather than the change. Use before any commit or push, when a gate or test fails and it is not obvious whether the change caused it, or when asked whether the work is ready. Covers stating the change's intent and capturing its before/after evidence, the four gates you run before pushing and the nine CI actually decides on, the reports whose findings are questions, the checker whose exit code follows the unfinished list, and the six checks that need a tool this container has not got.
allowed-tools:
  - Bash
  - Read
---

# Before you push

**The rule this skill exists for:** *a check that has to be remembered is a check that is skipped on the
day it matters.* Every tool below is documented one at a time in
[`tools/README.md`](../../../tools/README.md). **What was written nowhere is which to run, in what
order, and — the part that costs an hour when it is missing — which failures mean the machine rather
than the change.**

Adapted from `garrytan/gstack`'s `ship` ([33 §5.7](../../../docs/33-external-repository-research.md)),
which does the same job for a web product: merge the base branch, run the tests, review the diff, bump
the version. **Heron's version is different because Heron's failures are different** — six of its checks
need a compiler or a knowledge store this container has not got, and four of its suites cannot run here
at all. Confusing any of those for a regression is the mistake this file prevents.

Every number here was **measured on 2026-09-12**, not estimated. The previous set was measured on
2026-09-09 and four of them had gone stale by the time anyone read them again — which is why the counts
below are commands wherever a command can produce them.

**And it happened a third time, to this file, while that sentence was sitting in it.** On 2026-09-17
two of the counts below still read **163 suites** against a real 199, and one said **three** suites
cannot run here where the table in §2 says four. A rule written down is not a rule kept: the drift
this paragraph exists to warn about went on happening one line under the warning. Both totals are
commands now, and the sentence to hold onto is the register's — **a prose total is a cache with no
invalidation**.

---

## 0. Say what the change is for, before you run anything

```bash
python tools/check-change.py --intent "one line saying exactly what should change" \
                             --area brain --risk low
```

It compares the diff against the parts the change said it would touch, and names any file that is in
neither those parts nor a part they may depend on. **It refuses to call a change with no evidence a
pass**, so it is run twice: once early to see the scope, once at the end with `--evidence`.

```bash
python tools/change-evidence.py capture --out before.json --tests all   # before
python tools/change-evidence.py capture --out after.json --tests all --against before.json
python tools/check-change.py --intent "..." --area brain --risk low --evidence after.json
```

`change-evidence` never changes anything — it measures, compares, and rules `KEEP`, `REVERT` or
**`NO CHANGE MEASURED`**, which is the honest answer for a change whose effect nothing it can see moved.

## 1. The four that must pass

Fast, and they fail loudest. Run these first — a broken link or a missing header is cheaper to fix
before the tests than after.

```bash
python tools/check-docs.py        # ~1.2 s
python tools/check-metadata.py    # ~0.1 s
python tools/check-structure.py   # ~0.2 s
python tools/check-package.py     # ~0.1 s
```

**A non-zero exit from any of these is your change.** They need no environment, no compiler and no
knowledge store.

- **`check-docs`** — broken links, and **every count that can be derived**: open questions, test suites,
  tools. It fails on a *stated* number that disagrees with a *derived* one. When it says `DRIFT`, the
  sentence is wrong, not the code.
- **`check-structure`** — the layering, and the adapter boundary: the Revit vendor namespace only inside
  `revit/` or `tools/`. **It greps file text, so a comment counts.**
  [`heron-guard`](../heron-guard/SKILL.md) now refuses that one at edit time; this still catches
  everything else and anything that reached disk another way.

  **The way this actually fires is a FIXTURE or a COMMENT, four times in one session on 2026-09-15.**
  Writing a `brain/` agent about imported pyRevit code, or about what a clash check would need, the
  natural thing to type is the real namespace — in a demo folder's fake `script.py`, in a test's
  "a .txt that holds code", in a docstring explaining what cannot be done here. Every one was caught and
  every one was the gate being right: the fixture only ever needed *code*, not *Revit* code, and the
  prose can say "the Revit API's own solid-intersection filter" without naming it. Reach for a different
  vendor prefix in fixtures, and describe the API rather than spelling it.
- **`check-metadata`** — every source file's header.
- **`check-package`** — the delivery questions, and it is the only thing in the repository that reads
  `Heron.addin`. An entry class that no longer exists, an assembly the project does not build, or a
  `<ManifestSettings>` element (which **crashes Revit 2025 and older**) all leave every other gate on
  this page green and cost a modeller the whole add-in.

**A fifth is not on that list, and should be**: `python tools/check-dependencies.py`. It exits 1 only
when a **required** package is missing, so it says nothing about your change — it says whether this
machine can run Heron at all. Run it after a fresh clone, after touching `requirements.txt` or
`requirements-optional.txt`, and when a suite fails for a reason that smells like an absent import. A
missing **optional** package exits 0 on purpose: silent degradation is the designed behaviour, and a
checker that failed on one would be arguing with the requirement that allows it.

### THESE FOUR ARE NOT WHAT DECIDES THE PULL REQUEST. NINE DO.

`.github/workflows/gates.yml` has a job named **"The gates that must pass"**, and it runs **nine**
commands under `bash -e` — so a non-zero exit from **any** of them turns the PR red. The four above
are four of the nine. The other five are:

```bash
python tools/check-signatures.py       # a proof whose code moved under it
python tools/check-licence.py          # a licence header, and what may not be redistributed
python tools/check-narrow-errors.py    # a bare except is a swallowed cause
HERON_KNOWLEDGE=$(mktemp -d) python tools/check-routing.py    # every capability still reachable
HERON_KNOWLEDGE=$(mktemp -d) python tools/check-intrusion.py  # one fragment crowding out another
```

**This was measured the expensive way on 2026-09-21** ([row 5b-70](../../../docs/FRAGMENT-ISSUES.md)):
all four gates green locally, pushed, and `check-routing` exited **2** because a comment edited inside
a `PROVEN` fragment's `impl/` broke D-30's fingerprint. Nothing a contributor is told to run would have
caught it.

**The two with `HERON_KNOWLEDGE` need somewhere to put a knowledge store** and exit **2** saying so
when there is none — an empty folder is enough, and CI makes one. On Windows `%APPDATA%` already
answers, so the variable is only needed here.

## 2. The tests

```bash
for t in tests/test_*.py; do python "$t" >/dev/null 2>&1 || echo "FAIL $t"; done
```

Derive the number — `ls tests/test_*.py | wc -l`. Do not read a pass total here either. What matters is
that the failures **do not share a reason**, because a lump total is how a real regression hides.

**Some cannot run in full without an optional dependency, and they prove nothing either way.**
**TWO INSTALLS COVER ALL OF THEM**, and that is the part worth remembering rather than the list:

| needs | |
|---|---|
| the **MCP SDK** | `pip install --user mcp` |
| a **.NET SDK** | `apt-get update && apt-get install -y dotnet-sdk-10.0` |

**Derive which ones, rather than reading a list here** — the same rule as the suite total four lines
up, and for the same reason it is written twice:

```bash
grep -l 'COULD_NOT_RUN\|exit(3)' tests/test_*.py    # the suites that CAN say "could not run"
for t in tests/test_*.py; do python "$t" >/dev/null 2>&1; [ $? -eq 3 ] && echo "waiting $t"; done
```

**They exit 3, not 1**, so `check-gaps.py` reports them as waiting rather than failing.

> **This was a table of FOUR named suites until 2026-09-21, and it had been wrong for longer than
> that.** `test_kernel.py`, `test_binding_note.py` and `test_stack_guard.py` each build and run a C#
> test host and each exits 3 without an SDK, and none of the three was ever added — so the sentence
> *"a fourth failure is probably yours"* below could send somebody hunting their own change over a
> missing `dotnet`. It is the third time this file has gone stale in the same way, under a warning
> about going stale in that way, which is why what replaces it is a COMMAND. The two installs above
> do not change when a suite is added; a list of names does.

**The fourth was this list's own missing row, and it cost a session.** `test_dotnet.py` was on nobody's
list and **crashed** with `KeyError: 'buildable'` on any machine with no SDK — because
`HERON-DEV-NET-006` refuses with `NO_DOTNET`, correctly, and the suite indexed that refusal anyway. So
the machine's missing SDK read as a broken repository, and `check-gaps.py` counted it UNFINISHED, which
is the bucket that means *somebody could fix this here*. Nobody could.

Fixed 2026-09-17 in the suite, never in the agent: it now proves the refusal, names the four claims it
is leaving unproven, and exits 3. **CI has an SDK and still proves all eight**, so the
known-**NOT-RUNNABLE** list in `gates.yml` is unchanged and must stay that way. (It was a
known-failure list until row 5b-49 taught the job to tell *could not run* from *failed*; this line
said the old name until 2026-09-21.)

**Install them rather than excusing them.** On 2026-09-15 a session treated all three as unavoidable
on Linux for weeks. They are not: the commands above take a few minutes on a fresh container and
**every suite then passes**. None of them ever needed Windows or Revit. If a run reports only the four
in that table, the honest next step is to install and re-run, not to write "the known three".

> **This paragraph said "all 163 suites then pass" until 2026-09-17, when there were 199.** Measured
> 2026-09-12 and left to rot for five days while thirty-six suites were added. The rule it broke is
> four lines up this page — *derive the number, do not read a pass total here* — so the file was
> telling its reader to do the one thing its own prose had stopped doing. **No total is typed here
> now**, and `check-docs` cannot catch the next one: its pattern is `N test suites` and this said
> `N suites`. Widening it would fire on twenty legitimate subset counts elsewhere, so it stays
> narrow and this stays a sentence with no number in it.

Two wrinkles worth not re-discovering:

* `pip install --user mcp` alone leaves the SDK importable but **panicking** on some images —
  `pyo3_runtime.PanicException` out of the distro's `cryptography`. `pip install --user --upgrade
  cryptography` fixes it.
* The .NET SDK is an apt package (`dotnet-sdk-8.0`), but 2025–2027 need the **WindowsDesktop targets**
  that only `dotnet-sdk-10.0` carries — `tools/check-compile.py` says so itself when it skips them, and
  a skip is not a pass. Run `apt-get update` first; a stale index 404s on the .deb.

**This is about your machine, not about CI.** `.github/workflows/gates.yml` leaves both out on
purpose — they drag in native dependencies that break for reasons unrelated to this repository — and
its `fixed` check **fails the build if a listed suite passes there**. So install locally, run every
suite, and leave that list alone unless the runner itself changes.

**Nothing else should fail on any machine.** `test_graph.py` and `test_reachable.py` were on this list
until 2026-09-12, when both were fixed rather than excused — each had a fixture describing a repository
that had moved on, and neither test's claim changed. `.github/workflows/gates.yml` holds the same list
and the two were removed from it in the same change.

**On Windows the list was WRONG until 2026-09-12, and silently.** `test_context.py` also failed there,
on one check, because it compared a source path against a hardcoded `/` while the value arrives
spelled `brain\fragments\...`. Nothing named it, so the machine showed three failures where this file
promised two. Fixed at the comparison — but **fixed on Linux, where the defect cannot appear**, so if
you are on Windows this is the first run that proves it: [NEEDS-CHECKING](../../../docs/NEEDS-CHECKING.md) **A14**.

**A failure that is NOT one of the waiting ones is probably yours — check this section first, then say
so either way.** A suite that exits **3** is waiting on a dependency; one that exits **1** is a
finding. If a waiting one starts passing, somebody installed something. If a failure appears only on
your operating system, suspect a path assumption before you suspect your change: that is twice now.

This sentence counted to four until 2026-09-21. It does not count any more, for the reason the block
above gives - **the number changes every time a suite learns to say "could not run", and the exit code
says which kind of failure you are looking at without anybody maintaining a total.**

## 3. The reports — a finding is a question, not a failure

These **exit 0 whatever they find**. Read them; do not treat a hit as a break.

```bash
python tools/check-reachable.py    # ~0.5 s  built, and no production code calls it
python tools/check-revit-gate.py   # ~1.7 s  the fourteen Revit questions, as a list
python tools/agent-count.py        # ~0.05 s the register reconciles
```

`check-revit-gate`'s two standing hits are **linked documents** and **rollback / refusal reporting** —
[`Q-46`](../../../docs/OPEN-QUESTIONS.md) and [`Q-48`](../../../docs/OPEN-QUESTIONS.md), open and
waiting on the owner. **They are not new and they are not yours.** **Read the tool's own two numbers
rather than one written here**: it said 62 and 59 until 2026-09-21, when they were 67 and 64, because
the figures move with every fragment added. Derive `check-reachable`'s count the same way, for the
same reason.

## 4. The one that exits 1 on purpose

```bash
HERON_KNOWLEDGE=/tmp/heron-kb python tools/check-gaps.py
```

**`check-gaps` exits 1 while anything is UNFINISHED, and 0 when everything outstanding is only
WAITING.** Both are the tool working, and the distinction is the whole of it.

**It exits 0 on a plain container now, and getting there took two fixes rather than an excuse.** This
file used to give the reason for a non-zero exit as *"218 fragments have never met a Revit model"*;
those fragments moved to the **WAITING** bucket, and what was left was two suites landing in
**UNFINISHED** — the bucket that means *somebody could fix this here* — for an absent optional
dependency that nobody here could install into the repository.

Both were the same defect, one layer apart, and both are fixed in the suites rather than in the agents
they test:

| | was | now |
|---|---|---|
| `test_dotnet.py` | **crashed**, `KeyError: 'buildable'`, having indexed `HERON-DEV-NET-006`'s correct `NO_DOTNET` refusal | proves the refusal, names the four claims it leaves unproven, exits 3 |
| `test_served_claims.py` | exited **1**, so `check-gaps` could not tell it from a real failure | exits 3, and says what to install |

**The second one needed `BaseException`, not `ImportError`, and that is not defensive coding.** Measured
on 2026-09-17: `pip install --user mcp` on this image leaves the SDK importable but **panicking** —
`pyo3_runtime.PanicException` out of the distro's `cryptography`, under a missing `_cffi_backend`. An
`except ImportError` catches none of it and the suite dies with a traceback exactly as before.
`pip install --user --upgrade cryptography cffi` clears it, and then both suites run in full and pass.

Its own closing line is the sentence to keep: *"Waiting is not failing — but a waiting item is still
UNPROVEN."* **Read the buckets, not the exit code, and do not report either value as a break.**

> **Read this tool's exit code with `code=$?` on its own line.** `python tools/check-gaps.py | tail -50;
> echo $?` reports **`tail`'s** exit code, which is always 0 — and on 2026-09-12 that turned this very
> section into a claim that the gate passes. Two characters, in the optimistic direction.

## 5. The six that need something — and only ONE of them needs the owner's PC

Not failures. They need a tool, and the error says which.

| | needs | error you will see |
|---|---|---|
| `check-routing.py`, `check-intrusion.py` | a knowledge store | `No %APPDATA% and no HERON_KNOWLEDGE` |
| `check-compile.py`, `check-fragments-compile.py`, `check-api-surface.py` | **`dotnet`** | `FileNotFoundError: 'dotnet'` |
| `batch-prove.py` | **a real Revit**, and the owner | — |

The first two run fine with a store: `export HERON_KNOWLEDGE=/tmp/heron-kb`.

**This section said the middle three "are the owner's PC" until 2026-09-17, and that was WRONG — the
same shape of mistake as §2's "the known three".** They need a .NET SDK, which is one apt package, and
nothing about Windows or Revit. Measured on a plain Linux container that day:

```bash
apt-get update && apt-get install -y dotnet-sdk-10.0     # ~2 minutes
```

| | result |
|---|---|
| `check-compile.py` | **every project on all 8 releases, 2020–2027** - the tool prints the count and the projects by name; this row said **5** until 2026-09-21, when it was **8**, three rows above the one that already says to derive a total for exactly this reason |
| `check-fragments-compile.py` | **every fragment on every release each one claims** - derive the total with `ls brain/fragments \| wc -l`, because this row said 372 for three days after it was 395 |
| `check-api-surface.py` | **every Revit member Heron calls exists in every release** |
| `tests/test_bridge_roundtrip.py` | **passes**, after the one build its own failure message prints |

**Install `dotnet-sdk-10.0`, not 8.0.** 2025–2027 need the WindowsDesktop targets that only 10 carries,
and it builds the older releases too — one package covers the whole span.

**`check-api-surface.py` needed a one-line fix before it would run**, and the reason generalises:
`tools/api-surface/ApiSurface.csproj` targets `net8.0`, so on a machine carrying only the .NET 10
runtime it refused to start — *"You must install or update .NET to run this application … The following
frameworks were found: 10.0.12"*. The machine best set up to build Heron was the one that could not run
the tool that checks it. It now carries `<RollForward>LatestMajor</RollForward>`.

**`check-compile` has a trap worth knowing: it builds 2020–2027 into one folder and the newest wins, so
running it leaves .NET 10 binaries that Revit 2024 refuses.** `deploy-addin.ps1` guards this now rather
than trusting whoever ran it. **That trap is why this matters on the owner's PC and not here** — a
container with no Revit has nothing to break, so the compile gates are cheaper to run here than there.

## 6. Then the branch

- **Never push to `main`.** Work on the session's branch and open a **draft** pull request.
- Rebuilt anything under `revit/`? **Redeploy the add-in**, and check the framework first (§5).
- Changed a count anybody states in prose? `check-docs` already told you. Fix the sentence.

## What this deliberately does not do

**It does not prove anything works.** Every gate above is a compiler or a text check.
[D-30](../../../docs/DECISIONS.md) needs a real named model, a positive case **and a negative one**, and
a staleness fingerprint — see [`fragment-proving`](../fragment-proving/SKILL.md). **A green board and a
proven fragment are different claims**, and this file only gets you the first.
