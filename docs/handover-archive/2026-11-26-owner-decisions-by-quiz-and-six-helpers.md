# Session note — OWNER DECISIONS BY QUIZ, AND SIX HELPERS THAT BUILT THEM

> **Session note** from 2026-11-26, written straight into this folder as its README asks. Nothing here is
> specification: where it disagrees with [DECISIONS.md](../DECISIONS.md), the
> [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those win**. A
> note records what was true on its own day.

---

### 2026-11-26 — OWNER DECISIONS BY QUIZ, AND SIX HELPERS THAT BUILT THEM

A cloud session (no Revit) put the owner's open "Your decision" issues to him as short multiple-choice
questions, wrote each answer into its register, and sent the code work to helper sessions in their own
worktrees. The session is [session_01XqSwba1ujTrQCkV2SMCjum](https://claude.ai/code/session_01XqSwba1ujTrQCkV2SMCjum),
which is the link the decisions below carry for [D-206](../decisions/D-206.md).

**NEW - merged on main**

| Pull request | What it did |
|---|---|
| #1438 | #954 / #955: six question rows ANSWERED as covered by D-128's question flag; no retrieval change |
| #1439 | [D-204](../decisions/D-204.md): by-hand reading for two tools that never come back empty; sign on the run Revit checked. #1070 (a new view is enough) and #1156 (take a refused copy back) answered |
| #1448 | [D-206](../decisions/D-206.md): an Accepted decision links where the owner said yes; `tools/check-decision-sources.py` in CI's gates job |
| #1451 | `CREATE_FLOOR` and `CREATE_EXTRUSION_ROOF` back to DRAFT until OU21 / OU22 run as written (#1233) |
| #1454 | `MEASURE_DISTANCE` and `MEASURE_AVAILABLE_FALL` take two selected elements or two typed ids (#1308) |
| #1456 | [D-208](../decisions/D-208.md): `CHECK_VERTICAL_CLEARANCE` merged into `CHECK_MINIMUM_CLEARANCE`; every service through a sleeve checked; openings add insulation only where the model has it |
| #1458 | [D-207](../decisions/D-207.md): `*` wildcards for family names; a datum maker skips a taken name and says why; a link placed twice asks which copy |
| #1464 | [D-205](../decisions/D-205.md): the usage counter removed (supersedes D-191) |

**The owner's standing word, given this day:** do not ask him when a tool goes back to DRAFT because its code
changed - a correct tool comes first, and proving can be done any time.

**MISTAKES worth not repeating**

- A quoted yes heard only in one session's chat cannot be checked by anyone else. Link the session, an issue or a
  pull request on the Accepted line - D-206 now refuses one that does not.
- With other sessions merging all day, a green pull request goes stale within the hour. Merge them one at a time,
  bringing main in before each; the generated tool catalogue is regenerated, never hand-merged, and two branches
  answering items of one register row keep both answers on that row.
- A helper's worktree may hold a different branch than its name suggests (one helper stacked two). Check
  `git branch --show-current` before pushing from it.

**TO DO**

1. [#1465](https://github.com/Ajmalpshaik/Heron-AI/pull/1465): one datum tool (`CHECK_DATUMS_AGAINST_LINK` into
   `REPORT_LEVEL_ELEVATIONS`, copy-monitored datums added) - merges when green; then #1088 can close.
2. #1156: the `CREATE_SHEETS` copy-job change that the answer needs.
3. Revit runs owed on the owner's PC: Group OU (OU21, OU22), QX, QY, QZ, RA, and the scratch-family run of #1070.
4. The remaining "Your decision" issues - another session is taking them.
