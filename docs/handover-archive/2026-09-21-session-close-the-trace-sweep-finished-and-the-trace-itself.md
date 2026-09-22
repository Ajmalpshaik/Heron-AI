# Session note — (session close) — THE TRACE SWEEP FINISHED, AND THE TRACE ITSELF WAS WRONG ONCE

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (session close) — THE TRACE SWEEP FINISHED, AND THE TRACE ITSELF WAS WRONG ONCE

**[Row 5b-101](../FRAGMENT-ISSUES.md)'s method, run over every remaining thinly-held live-path module at
once. No new row came out of it, and that is the result** — recorded here so the next session does
not re-run a four-hour sweep to learn the same thing.

**Seven modules traced**, each under the suites that reach it, `public - ran`:

| module | public | never executed |
|---|---|---|
| `heron_flags` | 7 | none |
| `heron_conflict` | 6 | none |
| `heron_rerank` | 5 | none |
| `heron_skill` | 4 | none |
| `heron_capability` | 9 | `Capability` — **an artefact, see below** |
| `heron_gaps` | 7 | `audit_dir` — **wrong, see below** · `wanted_but_unprovided` |
| `heron_matrix` | 6 | `fragment_rows` |

**THE TRACE WAS WRONG ABOUT `audit_dir`, AND THE REASON IS THE SWEEP'S OWN WINDOW.** The first pass
ran only the suites that **name** the module in an `import`. `heron_gaps` is named by four; it is
**reached** by twenty, because `heron_audit`, `heron_timing` and `heron_rollback` all import it and
their suites do not. Widened to the transitive importers, `audit_dir` **executes**. So the sweep's
first answer was a false positive, and the correction is one line of the script:

```python
# WRONG: suites that name the module
if mod in imports_of(suite):
# RIGHT: suites that name it, OR name anything that reaches it
if mod in direct or any(reaches(d, mod) for d in direct if d in edges):
```

**`Capability` IS AN ARTEFACT OF `sys.settrace`, NOT A GAP.** A class name only appears as a frame
name when the **class body** executes, which is at import — before the tracer is installed for the
second and every later suite. **A class can never be reported as executed by this method after the
first import, so exclude `ast.ClassDef` from the public surface rather than believing it.** Its
methods trace normally under their own names.

**THE TWO THAT SURVIVED ARE REPORT SURFACES, NOT SEAMS**, and that is why neither is a row.
`heron_matrix.fragment_rows` is reachable only through `main(--fragments)`; `heron_gaps
.wanted_but_unprovided` only through that module's own report. **Neither is on the path a modeller's
request takes**, so an untested one costs a person a printed table, not a wrong answer about a model
— which is the distinction [row 5b-101](../FRAGMENT-ISSUES.md) turned on and the reason that one WAS
written. **Recorded as owed coverage, not as a defect.**

> **THE LESSON IS ABOUT THE METHOD AND IT IS THE FOURTH OF ITS KIND THIS SESSION.** Rows
> [5b-86](../FRAGMENT-ISSUES.md), [5b-92](../FRAGMENT-ISSUES.md) and [5b-95](../FRAGMENT-ISSUES.md) were a
> **scan** wrong about a shape. This one was a **trace** — the thing brought in to stop that — and it
> was still wrong, because it was pointed at the wrong suites. **Tracing beats grepping and still
> needs its input checked.** The corrected script takes the transitive closure and drops classes.
