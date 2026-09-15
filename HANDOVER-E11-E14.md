# Handover — E12 failed in front of Revit, the fix is written and NOT deployed

**2026-09-16. Branch `claude/friendly-hypatia-196de4`, rebased onto `main` at `dda7a71` (#149),
pushed, no conflicts.**

**The E rows were renumbered on main while this work was running.** #147's unsaved-model row
became E11 and pushed #148's four down. Everything below uses **main's** numbering. If you
remember "E11 failed", that row is **E12** now.

## Do this first, when you are free of Revit

**The add-in on disk is OLDER than this branch**, and `check-compile.py` has since built
2020–2027 into the SHARED output folder — so what is sitting in `bin` is **.NET 10 for Revit
2027**, which Revit 2024 refuses. Do not deploy what is there.

```
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2020
powershell -File tools\deploy-addin.ps1 -RevitVersion 2020
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2024
powershell -File tools\deploy-addin.ps1 -RevitVersion 2024
dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2027
powershell -File tools\deploy-addin.ps1 -RevitVersion 2027
```

Build a version and deploy THAT version before building the next. Revit must be closed. Only
2020, 2024 and 2027 are installed on this PC.

## What was run in front of Revit

Revit 2024.3 session 2924, two blank never-saved projects and `M_Rectangular Elbow - Radius.rfa`.

| Row (main's numbering) | Result |
|---|---|
| **E11** unsaved model, read tool then change | **PASSED** — same run as E15 |
| **E12** switch to a second project | **FAILED** — `CREATE_LEVEL ran in Project2` |
| **E13** switch to a family | **PASSED** — *"has no Project Information … NOTHING was written"* |
| **E14** fresh chat, change first | **PASSED** — pin was `None`, it ran |
| **E15** same model, no switching | **PASSED** — but proves less than it looks |

**E12 is the one the whole change existed for.** `ProjectKey` is
`ProjectInformation.UniqueId`, **inherited from the template**:

| Document | key |
|---|---|
| Project1 (2024) | `8764c510-…-0000c160` |
| Project2 (2024) | `8764c510-…-0000c160` |
| `PIPE.rvt` (2020, unrelated) | `8764c510-…-0000c160` |
| the family | `None` |

The guard compared two equal strings and let the write through. **E13 passes only because `None`
is genuinely different. E15 passes for a reason indistinguishable from the bug** — every project
on this machine returns the same key, so it cannot tell a working identity from a colliding one.

CI was green throughout: 5/5 checks, 188 tests, compiled 2020–2027. That gap is what D-30 is for.

## What was done about it — D-74, written and unrun

The question is inverted: not *"did the user move?"* but *"which model was I told to work on?"*,
which needs no key.

- `revit_change` sends `document` (pinned title) and `documentPath` (pinned path).
- `RevitFragment.Run` resolves the **path first** (unique among open models), title second.
- A title matching **twice** is `ambiguous_document` — the lookup used to let the last document
  round the loop win silently.
- `no_such_document` and `ambiguous_document` joined the failure table, and
  `ambiguous_document` the gaps table. **`no_such_document` predates all of this and was in
  neither** — the same gap `wrong_document` had.
- `expectProject` **stays**. No longer the mechanism; still catches the family case.

**It is what the owner asked for**, in his words: *"pin project 1, refer to project 2, and I move
to project 3 — Heron needs to work on project 1… that is agentic work."*

**Demonstrated over the bridge with the FAMILY in front throughout:** read
`ajmal testing level @ 12500 mm` out of Project2 by name, wrote ten levels into Project1 by name
(`ajmal testing level - 01` @ 15000 mm … `- 10` @ 42000 mm), verified by reading back — Project1
3 → 13 levels, **Project2 untouched at 5**, every reply `wasActiveDocument=False`.

**That is not a proof of this change.** It was driven over the bridge directly, not through
`revit_change`, so the line D-74 actually changes has never run.

## What it owes — E16, E17, E18

| | |
|---|---|
| **E16** | E12's arrangement again. The change must **land in Project1**, not be refused — the write is aimed now, so moving the screen is no longer an error. Check Project2 did NOT gain it |
| **E17** | Pin a project, **close it**, ask for a change → `no_such_document`, naming what is open, nothing written |
| **E18** | Two models both called `Project1`, pin one, ask → `ambiguous_document`. **Then save one and repeat** — the paths differ, so it must resolve cleanly |

## The other half of this branch — the `binds:` work

Separate from the write guard, and also **unproven**:

- **The executor never read `binds:`.** Main's [FRAGMENT-ISSUES row 96](docs/FRAGMENT-ISSUES.md)
  recorded it as `needs_unbound`; **row 100 measured what it actually did**, which is worse — it
  fell through to the selection branch and bound *whatever was selected in Revit*, reporting a
  clean run. Fixed.
- **Millimetres out.** `distances` → `distancesMm`, `gaps` → `gapsMm`. D-71 left result units
  open; `measure-distance` answered the same question 304.8× apart.
- **`check-minimum-clearance.rules` was used raw, in feet** — `Walls=150` demanded 150 FEET, and
  only for rule-judged pairs (row 102).
- **D-73** adds a table-by-name input (`"Walls=150; Structural Framing=50"`), unblocking
  `check-minimum-clearance` and `check-model-standards`, which had never run a line (row 103).

`tools/jobs/binds-2026-09-15.yaml` dry-runs clean. **Its arrangement was measured in a Project1 on
Revit 2020 that is almost certainly gone — re-measure before trusting it.**

## Open, and the owner's

- **D-73 and D-74 are both marked Proposed** and await him reading them back.
- **`write.enabled` is TRUE.** `revit_health` warns about it. Switch Changes off in the ribbon
  unless deliberately testing.
- **Stray test levels.** Project1: `HERON_E13` + ten `ajmal testing level - NN`.
  Project2: `HERON_E11`, `HERON_E14`. Both blank projects.
- **A UI rough edge, found by hitting it:** the Heron ribbon button is a toggle with no visible
  on/off state. It was switched off mid-session and the only sign was `Disconnected from the
  ribbon` in the add-in log — from the client side it looks exactly like "not connected", with
  nothing saying why.
