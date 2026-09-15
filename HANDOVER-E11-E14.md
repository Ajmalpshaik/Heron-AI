# Handover — everything is deployed, E11–E14 are the only thing left

**Written 2026-09-15, just before Ajmal restarts Claude Code so the heron MCP connector
reloads. Nothing here is a plan; it is all done except the last section.**

## Read this first

Branch **`claude/friendly-hypatia-196de4`**, pushed. It is `main` + three commits:

| | |
|---|---|
| `98f4a21` | PR #147's document-pin fix (was `cc3c6df`) |
| `776714e` | `binds:` at the executor, millimetres out, D-73's table-by-name |
| `272dfca` | PR #148's wrong-model guard (was `03d557f`) |

**PR #147 and PR #148 are both still open and both still conflict with each other** — they
independently add the same `documentPath`/`projectKey` lines to `RevitFragment.Report`. This
branch is the resolved combination of the two plus the fragment work. Merging either PR on its
own will conflict with this branch; that is Ajmal's call, not a session's.

## What is ALREADY deployed, verified by byte-search of the shipped DLLs

Revit **2020, 2024 and 2027** (the only three installed), built and deployed one version at a
time from this branch. Each DLL was searched for the actual strings, not assumed:

- `"NOTHING was written"` — PR #148's guard ✔
- `"has no Project Information"` — the family-document refusal ✔
- `"filled from"` — the `binds:` refusal ✔
- `"documentPath"` / `"expectProject"` ✔
- `"stopped before the transaction opened"` — **absent**, correctly: that was a DUPLICATE guard
  this session wrote before PR #148 arrived, and it has been removed in favour of theirs

## Why PR #148's version replaced the one written here

This session independently implemented the same guard (commit `93f48e7`, now dropped, kept at
tag `wip-before-148`). PR #148's is better and was taken instead:

- it adds **`wrong_document` to `heron_failure.py`**, which the local one missed entirely.
  Without that row, `analyse(writes=True)` falls through to its fail-closed rule, calls a refusal
  that returns *above* the transaction an UNKNOWN outcome, and tells the user to go and check the
  model — frightening, and false about the one failure that is certain nothing happened
- its refusal says *"the one this would have run in"* rather than *"in front of Revit now"*,
  because this path can be given a document BY NAME

## Gates, all run on this branch

| | |
|---|---|
| `check-gaps` | **216 suites ok, 0 failures.** The earlier brief said it "wrote zero bytes / is unrun" — that was wrong. It is a full suite runner and simply takes a long time. Its exit 1 follows the UNFINISHED list, not a failure |
| `check-compile` | ok, all four projects, 2020 → 2027 |
| `check-docs`, `check-metadata`, `check-structure` | pass (4 broken links remain, all pre-existing in `PROPOSALS.md` and D-72) |
| `test_agents.py` | **now returns in seconds.** It was the one UNFINISHED item; it is fixed by #146, which this branch was missing until it was rebased onto main |

## THE ONLY THING LEFT: E11–E14 in front of Revit

**Nothing below has been run. Do not tick a row in `docs/NEEDS-CHECKING.md` that was not observed.**

### Why the session had to be restarted

`.mcp.json` launches `python mcp/server/heron_mcp_server.py` by RELATIVE path, and Python reads
the file once at process start. Five `heron_mcp_server` processes were alive, all started
**before** this code existed, so none of them would send `expectProject` — and an absent key means
*"do not check"*, so the add-in gate sits switched off and `revit_change` runs. That failure looks
nothing like a bug; it looks like the test passing. **Confirm the connector is fresh before
trusting any E-row.**

### Setup

Revit **2024**, Ribbon → Heron AI → Heron to connect, then `python mcp\client\heron_bridge_client.py ping`.
Open **two projects and one family**. Why each is needed:

- **two projects** — E11 is "click into a different model and ask again". It needs a second model
  to click into. Two BLANK projects are ideal: their undo stacks start empty, so "both undo stacks
  unchanged" is unambiguous rather than a judgement about a busy model
- **one family** — E12 is the family case. A family has no Project Information and therefore no
  project key, and a family editor becomes the active document the moment it opens. That is the
  ordinary way a write lands somewhere nobody pointed at, and the guard must refuse it in its own
  sentence rather than let a null fall through a string comparison

**E13 and E14 WRITE FOR REAL.** `revit_change` applies and keeps the work. Ajmal had not chosen
the models when this was written — ask before writing into anything of his.

### The rows

| | |
|---|---|
| **E11** | `revit_change` in one project, click into the second, ask again. PASS: refuses **before** anything is written, names the model it would have run in, says NOTHING was written, and **both undo stacks are unchanged** |
| **E12** | Same, but click into the **family**. PASS: still refuses, in a **different** sentence |
| **E13** | **The one that matters most.** Fresh chat, `revit_change` FIRST. PASS: it **RUNS**. If it refuses, the guard is inverted and every chat is bricked — stop, report, and do not leave it deployed |
| **E14** | `revit_select_by_category` then `revit_change`, same model, no switching. PASS: it **RUNS** — both tools must produce the same key. Do it on a saved project AND on an unsaved one |

Rollback if anything fails: `git revert 272dfca`, rebuild and redeploy the same three versions,
restart Revit.

## Also unfinished, and separate from the above

`find-nearest-elements` and `check-minimum-clearance` are **still unproven**. The job file is
written and dry-runs clean:

```
python tools/batch-prove.py tools/jobs/binds-2026-09-15.yaml --dry-run
python tools/batch-prove.py tools/jobs/binds-2026-09-15.yaml
```

Arrangement measured in Project1 on Revit 2020 (that model was unsaved and is probably gone —
**re-measure before trusting these**): `FloorPlan: 1 - Mech` held 21 elements across twelve MEP
categories, `1 - Plumbing` held exactly 1, `2 - Mech` held 1.

**D-73 is marked Proposed.** Ajmal asked for "what is best" rather than picking, so the row
records what was done and why, for him to accept or send back.
