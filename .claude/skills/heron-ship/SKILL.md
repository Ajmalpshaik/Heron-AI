---
name: heron-ship
description: What to run before pushing Heron, in what order, and which failures are the machine rather than the change. Use before any commit or push, when a gate or test fails and it is not obvious whether the change caused it, or when asked whether the work is ready. Covers stating the change's intent and capturing its before/after evidence, the four gates that must pass, the reports whose findings are questions, the checker whose exit code follows the unfinished list, and the six checks that need a tool this container has not got.
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
need a compiler or a knowledge store this container has not got, and three of its suites cannot run here
at all. Confusing any of those for a regression is the mistake this file prevents.

Every number here was **measured on 2026-09-12**, not estimated. The previous set was measured on
2026-09-09 and four of them had gone stale by the time anyone read them again — which is why the counts
below are commands wherever a command can produce them.

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

## 2. The tests

```bash
for t in tests/test_*.py; do python "$t" >/dev/null 2>&1 || echo "FAIL $t"; done
```

Derive the number — `ls tests/test_*.py | wc -l`. Do not read a pass total here either. What matters is
that the failures **do not share a reason**, because a lump total is how a real regression hides.

**Four cannot run in full without an optional dependency.** They prove nothing either way:

| | needs | |
|---|---|---|
| `test_mcp_serves.py` | the **MCP SDK** | `pip install --user mcp` |
| `test_served_claims.py` | the **MCP SDK** | same |
| `test_bridge_roundtrip.py` | a **built .NET test host** | `dotnet build tests/Heron.Bridge.TestHost` |
| `test_dotnet.py` | **any `dotnet`** — four of its eight claims only | `apt-get install -y dotnet-sdk-8.0` |

**All four exit 3, not 1**, so `check-gaps.py` reports them as waiting rather than failing.
`test_served_claims.py` was the last one to join them, on 2026-09-17 — see §4.

**The fourth was this list's own missing row, and it cost a session.** `test_dotnet.py` was on nobody's
list and **crashed** with `KeyError: 'buildable'` on any machine with no SDK — because
`HERON-DEV-NET-006` refuses with `NO_DOTNET`, correctly, and the suite indexed that refusal anyway. So
the machine's missing SDK read as a broken repository, and `check-gaps.py` counted it UNFINISHED, which
is the bucket that means *somebody could fix this here*. Nobody could.

Fixed 2026-09-17 in the suite, never in the agent: it now proves the refusal, names the four claims it
is leaving unproven, and exits 3. **CI has an SDK and still proves all eight**, so the known-failure
list in `gates.yml` is unchanged and must stay that way.

**Install them rather than excusing them.** On 2026-09-15 a session treated all three as unavoidable
on Linux for weeks. They are not: the three commands above take a few minutes on a fresh container and
**all 163 suites then pass**. None of them ever needed Windows or Revit. If a run reports these three
and nothing else, the honest next step is to install and re-run, not to write "the known three".

Two wrinkles worth not re-discovering:

* `pip install --user mcp` alone leaves the SDK importable but **panicking** on some images —
  `pyo3_runtime.PanicException` out of the distro's `cryptography`. `pip install --user --upgrade
  cryptography` fixes it.
* The .NET SDK is an apt package (`dotnet-sdk-8.0`), but 2025–2027 need the **WindowsDesktop targets**
  that only `dotnet-sdk-10.0` carries — `tools/check-compile.py` says so itself when it skips them, and
  a skip is not a pass. Run `apt-get update` first; a stale index 404s on the .deb.

**This is about your machine, not about CI.** `.github/workflows/gates.yml` leaves both out on
purpose — they drag in native dependencies that break for reasons unrelated to this repository — and
its `fixed` check **fails the build if a listed suite passes there**. So install locally, run the full
163, and leave that list alone unless the runner itself changes.

**Nothing else should fail on any machine.** `test_graph.py` and `test_reachable.py` were on this list
until 2026-09-12, when both were fixed rather than excused — each had a fixture describing a repository
that had moved on, and neither test's claim changed. `.github/workflows/gates.yml` holds the same list
and the two were removed from it in the same change.

**On Windows the list was WRONG until 2026-09-12, and silently.** `test_context.py` also failed there,
on one check, because it compared a source path against a hardcoded `/` while the value arrives
spelled `brain\fragments\...`. Nothing named it, so the machine showed three failures where this file
promised two. Fixed at the comparison — but **fixed on Linux, where the defect cannot appear**, so if
you are on Windows this is the first run that proves it: [NEEDS-CHECKING](../../../docs/NEEDS-CHECKING.md) **A14**.

**A fourth failure is probably yours — check this section first, then say so either way.** If one of the
three starts passing, somebody installed something. If a fourth appears only on your operating system,
suspect a path assumption before you suspect your change: that is twice now.

## 3. The reports — a finding is a question, not a failure

These **exit 0 whatever they find**. Read them; do not treat a hit as a break.

```bash
python tools/check-reachable.py    # ~0.5 s  built, and no production code calls it
python tools/check-revit-gate.py   # ~1.7 s  the fourteen Revit questions, as a list
python tools/agent-count.py        # ~0.05 s the register reconciles
```

`check-revit-gate` reports **62** for links and **59** for refusal reporting. Those are
[`Q-46`](../../../docs/OPEN-QUESTIONS.md) and [`Q-48`](../../../docs/OPEN-QUESTIONS.md), open and
waiting on the owner. **They are not new and they are not yours.** Derive `check-reachable`'s count
rather than reading one here — it moves whenever a module is added.

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

## 5. The six that need something this container has not got

Not failures. They need a tool, and the error says which.

| | needs | error you will see |
|---|---|---|
| `check-routing.py`, `check-intrusion.py` | a knowledge store | `No %APPDATA% and no HERON_KNOWLEDGE` |
| `check-compile.py`, `check-fragments-compile.py`, `check-api-surface.py` | **`dotnet`** | `FileNotFoundError: 'dotnet'` |
| `batch-prove.py` | **a real Revit**, and the owner | — |

The first two run fine with a store: `export HERON_KNOWLEDGE=/tmp/heron-kb`. The middle three need the
.NET SDK and are the owner's PC. **`check-compile` has a trap worth knowing: it builds 2020–2027 into
one folder and the newest wins, so running it leaves .NET 10 binaries that Revit 2024 refuses.**
`deploy-addin.ps1` guards this now rather than trusting whoever ran it.

## 6. Then the branch

- **Never push to `main`.** Work on the session's branch and open a **draft** pull request.
- Rebuilt anything under `revit/`? **Redeploy the add-in**, and check the framework first (§5).
- Changed a count anybody states in prose? `check-docs` already told you. Fix the sentence.

## What this deliberately does not do

**It does not prove anything works.** Every gate above is a compiler or a text check.
[D-30](../../../docs/DECISIONS.md) needs a real named model, a positive case **and a negative one**, and
a staleness fingerprint — see [`fragment-proving`](../fragment-proving/SKILL.md). **A green board and a
proven fragment are different claims**, and this file only gets you the first.
