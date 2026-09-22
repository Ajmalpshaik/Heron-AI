# Session note — THE SCAFFOLDER WROTE `Heron-Step: banana` AND EXITED 0

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE SCAFFOLDER WROTE `Heron-Step: banana` AND EXITED 0

**[Row 5b-144](../FRAGMENT-ISSUES.md), FIXED.** `tools/new-agent.py` read end to end — 410 lines.

It puts three things into a header: the part's layer, the module name, and the step. **`--part` is
checked against a list, `--module` against a pattern, and `--step` was not checked at all.** Measured:

```
new-agent.py HERON-DEV-ARC-003 --step banana
  ->  "written" three times, exit 0
  ->  # Heron-Step:   banana   in the module AND the test
```

Its own docstring is why that matters:

> *"Done by hand 146 times that is 146 chances to mistype a layer, invent a field, or copy a header from
> a file in a different part — and **check-metadata.py finds the mistake after the work, not before
> it**."*

**And `check-metadata` would not name it**, traced rather than assumed: line 235 asks only that the
five fields are *present*, and the step arithmetic at lines 188 and 276 is guarded by `.isdigit()` — so
a file whose step is not a number is quietly left out of the counts rather than reported.

#### The suite had never asserted anything it writes

`tests/test_new_agent.py` already owned this tool and **every one of its cases was a refusal**. It says
so itself: *"EVERY CASE BELOW WRITES NOTHING … this runs inside the real repository, because the
register and the module list are what it is asserting against."* Extended rather than duplicated, with
the write cases pointed at a temp tree and `NA.ROOT`/`NA.register` put back afterwards — and a final
check that the real repository is untouched.

#### A negative result made permanent, and it is the valuable half

The module template argues at length that the stub must **not** claim the agent, because
`agent-count.py` reads any commented `Heron-Agent` line in the first 40 lines *including one quoted
inside a docstring* — so an example of the line would raise the built count for work nobody has done.
**Nothing checked that the template obeys its own argument.** Measured: both templates declare exactly
`Heron-Agent:  none`, and the agent id appears only in prose. It is asserted now, on both files.

| Break | Red |
|---|---|
| **the module as found** | **5** |
| the stub claiming the agent in its header | 2 |
| the layer no longer following the part | 1 |
| a stub claiming DRAFT rather than DISCOVERED | 1 |
| writing one file at a time instead of all-three-or-none | 1 |

**Nothing was written into the repository** — checked with `git status` after the run.
