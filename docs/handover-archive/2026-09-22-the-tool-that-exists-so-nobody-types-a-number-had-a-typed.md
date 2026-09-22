# Session note — THE TOOL THAT EXISTS SO NOBODY TYPES A NUMBER HAD A TYPED NUMBER IN IT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE TOOL THAT EXISTS SO NOBODY TYPES A NUMBER HAD A TYPED NUMBER IN IT

**[Row 5b-123](../FRAGMENT-ISSUES.md), FIXED — and one wrong number in a governing document
corrected with it.** `tools/recount-agent-registry.py` read end to end — 98 lines, never opened
before. The **third** of the six unheld tools. Its one job is AGENTS.md's second Never — *never
type a number a command can derive* — for [`28-agent-registry.md`](../28-agent-registry.md).

**Two things measured by running it**, on a copy so the real document was never touched.

**One.** Six of its seven prose substitutions are `\d+` regexes. The seventh was
`s.replace('across 244 agents', ...)` — a **literal** — so it worked exactly once. Run against the
registry as it stands, it printed `TOTAL=250`, printed `verify: all departments reconcile`, exited
**0**, and left **`across 249 agents`** in the file. A stated count, wrong by one, in the document
this tool owns, frozen since the total left 244.

**Two.** Given a registry whose table has gained one column:

```
  summary table rebuilt (21 departments)
  TOTAL=0  T1=0 T2=0 T3=0  departments=21
  verify: all departments reconcile
exit=0
```

It wrote **`Totals: 0 agents · 0 T1 · 0 T2 · 0 T3`** and a summary table of zeros over a document
still listing 250 rows. The tier is read from **column four of the split row**, so every row landed
in a bucket that was not T1, T2 or T3 — [D-52](../DECISIONS.md), the plausible zero.

**Its `verify` could see neither**, because it recounts rows per department against headings the
same pass had just written from the same counter, and looks at no other figure in the file.

### What it does now

- the seventh substitution is a `\d+` regex like the other six;
- a tier that is not `T1`, `T2` or `T3` is **refused before anything is written**, and so is a
  registry with no agent rows, or one whose tier count and row count disagree;
- the whole document is **rebuilt in memory and read back** by a new `disagreements()`, which
  checks department headings, the totals line, the summary table's Total row and every
  `across N agents` against the rows underneath them. Anything still disagreeing means **nothing
  is written**, and the exit code says so — it was 0 whatever happened before;
- the path comes from `__file__`, and `main()` plus `sys.exit(main())` mean importing it no longer
  rewrites the registry.

**`tests/test_recount_registry.py`** is the first suite this tool has ever had, and each half was
shown to have teeth on its own:

| what was broken | red |
|---|---|
| the module as found — no `main()`, no absolute path, will not load safely | **3** |
| the frozen literal put back | **7** |
| all three tier guards removed — the run that writes `0 agents` and exits 0 | **3** |

**One check was caught green for the wrong reason and narrowed before the fix went in**: the moved
column was first inserted in *front* of the id, which stops a row being a row at all and was
refused by a different branch entirely.

**What is not changed, and is recorded rather than fixed**: there is still no `--check` that
reports without writing, so this cannot be a CI gate — PROPOSALS F6, and the owner's.

**Three of the six unheld tools remain**: `measure-graph`, `module-reach`,
`generate-decision-summary`.
