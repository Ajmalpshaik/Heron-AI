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
- **`check-metadata`** — every source file's header.
- **`check-package`** — the delivery questions, and it is the only thing in the repository that reads
  `Heron.addin`. An entry class that no longer exists, an assembly the project does not build, or a
  `<ManifestSettings>` element (which **crashes Revit 2025 and older**) all leave every other gate on
  this page green and cost a modeller the whole add-in.

## 2. The tests

```bash
for t in tests/test_*.py; do python "$t" >/dev/null 2>&1 || echo "FAIL $t"; done
```

Derive the number — `ls tests/test_*.py | wc -l`. Do not read a pass total here either. What matters is
that the failures **do not share a reason**, because a lump total is how a real regression hides.

**Three cannot run at all without an optional dependency.** They prove nothing either way:

| | needs | |
|---|---|---|
| `test_mcp_serves.py` | the **MCP SDK** | `pip install --user mcp` |
| `test_served_claims.py` | the **MCP SDK** | same |
| `test_bridge_roundtrip.py` | a **built .NET test host** | `dotnet build tests/Heron.Bridge.TestHost` |

`test_mcp_serves.py` exits **3**, not 1, so `check-gaps.py` reports it as waiting rather than failing.

**Nothing else fails here.** `test_graph.py` and `test_reachable.py` were on this list until
2026-09-12, when both were fixed rather than excused — each had a fixture describing a repository that
had moved on, and neither test's claim changed. `.github/workflows/gates.yml` holds the same list and
the two were removed from it in the same change.

**A fourth failure is yours. So is any change to that list.** If one of the three starts passing,
somebody installed something — say so rather than quietly recording a better number.

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

**It exits 1 on a plain container, and the reason has changed.** This file used to give the reason as
*"218 fragments have never met a Revit model"* — those fragments are all in the **WAITING** bucket now,
and the single unfinished item is `test_served_claims.py FAILS`, which fails for want of the **MCP SDK**
and exits **1 rather than 3**, so `check-gaps` cannot tell it from a real failure. Install the SDK and it
exits 0.

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
