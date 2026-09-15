# Handover — E11-E14 were run, E11 failed, the fix is written and NOT deployed

**Updated 2026-09-15, late. Branch `claude/friendly-hypatia-196de4`, pushed.**

## Do this first

**The add-in on disk is OLDER than this branch.** The last deploy (22:56) predates the
targeting fix. `check-compile.py` has since run across 2020-2027 into the SHARED build
folder, so what is sitting in `bin` is **.NET 10 for Revit 2027** and Revit 2024 would
refuse it. Do not deploy what is there.

```
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2020
powershell -File tools\deploy-addin.ps1 -RevitVersion 2020
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2024
powershell -File tools\deploy-addin.ps1 -RevitVersion 2024
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2027
powershell -File tools\deploy-addin.ps1 -RevitVersion 2027
```

Build a version and deploy THAT version before building the next. Revit must be closed.
Only 2020, 2024 and 2027 are installed on this PC.

## What was run in front of Revit, and what it found

Revit 2024.3 session 2924, two blank projects and `M_Rectangular Elbow - Radius.rfa`.

| Row | Result |
|---|---|
| **E13** fresh chat, `revit_change` first | **PASSED** — pin was `None`, `CREATE_LEVEL ran in Project1` |
| **E14** same model, no switching | **PASSED** — but see below; it proves less than it looks |
| **E12** switch to a family | **PASSED** — *"has no Project Information … NOTHING was written"* |
| **E11** switch to a second project | **FAILED** — `CREATE_LEVEL ran in Project2` |

**E11 is the one the whole change existed for.** `ProjectKey` is
`ProjectInformation.UniqueId`, which is inherited from the TEMPLATE:

| Document | key |
|---|---|
| Project1 (2024) | `8764c510-…-0000c160` |
| Project2 (2024) | `8764c510-…-0000c160` |
| `PIPE.rvt` (2020, unrelated) | `8764c510-…-0000c160` |
| the family | `None` |

So the guard compared two equal strings and let the write through. **E12 passes only
because `None` is genuinely different. E14 passes for a reason indistinguishable from the
bug** — every project on this machine returns the same key, so that row cannot tell a
working identity from a colliding one.

CI was green the whole time: 5/5 checks, 188 tests, compiled 2020-2027. That gap is what
D-30 exists for.

## What was done about it — D-74, written and unrun

The question is inverted: not *"did the user move?"* but *"which model was I told to work
on?"*, which needs no key.

- `revit_change` sends `document` (pinned title) and `documentPath` (pinned path).
- `RevitFragment.Run` resolves the **path first** (unique among open models), title second.
- A title matching **twice** is `ambiguous_document`. The lookup used to let the last
  document round the loop win silently.
- `no_such_document` and `ambiguous_document` joined the failure table.
  **`no_such_document` predates all of this and was never in it** — the same gap
  `wrong_document` had.
- `expectProject` **stays**. No longer the mechanism; still catches the family case.

**It is what the owner asked for**, in his words: *"pin project 1, refer to project 2, and
I move to project 3 — Heron needs to work on project 1… that is agentic work."*

**Demonstrated over the bridge with the FAMILY in front the whole time:** read
`ajmal testing level @ 12500 mm` out of Project2 by name, wrote ten levels into Project1 by
name (`ajmal testing level - 01` @ 15000 mm … `- 10` @ 42000 mm), verified by reading back —
Project1 3 → 13 levels, **Project2 untouched at 5**, every reply `wasActiveDocument=False`.

**That is not a proof of this change.** It was driven over the bridge directly, not through
`revit_change`, so the line D-74 actually changes has never run.

## What it owes — E15, E16, E17 in NEEDS-CHECKING Group E

| | |
|---|---|
| **E15** | E11's arrangement again. The change must **land in Project1**, not be refused — the write is aimed now, so moving the screen is no longer an error. Check Project2 did NOT gain it |
| **E16** | Pin a project, **close it**, ask for a change → `no_such_document`, naming what is open, nothing written |
| **E17** | Two models both called `Project1`, pin one, ask → `ambiguous_document`. **Then save one and repeat** — the paths differ, so it must resolve cleanly |

## Also still open

- **`write.enabled` is TRUE.** `revit_health` warns about it. Switch Changes off in the
  ribbon unless deliberately testing.
- **Stray test levels.** Project1: `HERON_E13` + ten `ajmal testing level - NN`.
  Project2: `HERON_E11`, `HERON_E14`. Both blank projects.
- **PRs #147 and #148 are both open and conflict with each other** — both add the same
  identity lines to `RevitFragment.Report`. This branch has them resolved together, plus the
  fragment work. Merging either alone will conflict. The owner's call.
- **D-73 and D-74 are both marked Proposed** and await the owner reading them back.
- **`find-nearest-elements` and `check-minimum-clearance` are still unproven.**
  `tools/jobs/binds-2026-09-15.yaml` dry-runs clean. Its arrangement was measured in a
  Project1 on Revit 2020 that is almost certainly gone — **re-measure before trusting it**.
