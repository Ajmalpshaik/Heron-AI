# Session note — PICKING THE TARGET BY MEASUREMENT, AND TRACING INSTEAD OF GREPPING

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 — PICKING THE TARGET BY MEASUREMENT, AND TRACING INSTEAD OF GREPPING

**[Row 5b-101](../FRAGMENT-ISSUES.md). FIXED.** Two methods changed here and both are worth keeping.

**THE TARGET WAS CHOSEN BY MEASUREMENT, NOT BY ORDER.** For every module transitively reachable from
`mcp/`, count the distinct suites that import it against its public surface. `brain/heron_ground.py`
came out thinnest by a distance — **1,115 lines, 12 public functions, 2 suites** — and that is where
the row was. **Re-run that measurement rather than reading `brain/` alphabetically**; the command is
in the row's own commit.

**AND THE FINDING WAS FOUND BY TRACING, NOT BY GREPPING.** `cited_ids(draft)` pulls the chunk ids a
draft cites so the MCP seam can look each up **by id** — which that file calls *"the one lookup whose
answer cannot depend on a score"*. A marker it misses is a real clause never carried, and the citation
then resolves to nothing.

`sys.settrace` over all four suites that import the module: **33 of its functions execute in
`test_review_findings` alone, and `cited_ids` is not one of them in any of the four.** Its only
coverage asserted the **string** `"cited_ids"` appears in `heron_brain`'s source — **[row
5b-100](../FRAGMENT-ISSUES.md)'s shape exactly, one module along.**

**MY OWN MEASUREMENT WAS WRONG TWICE BEFORE THE TRACE.**

| attempt | said | truth |
|---|---|---|
| count call sites | **6 of 12** public functions uncalled | — |
| trace execution | — | **1 of 12** |
| first scan | the suite called **nothing** | it kept only the LAST `import heron_ground` alias, and a bare one at line 245 overwrote the `as G` at line 90 |

**Shape is not behaviour, and a call site is not an execution.** `check()` reaches almost every
function internally, so counting names who call it overstates the gap by six. The trace is twenty
lines and settles it:

```python
def tracer(frame, event, arg):
    if event == "call" and frame.f_code.co_filename == target:
        ran.add(frame.f_code.co_name)
sys.settrace(tracer); runpy.run_path("tests/<suite>.py", run_name="__main__")
```

**Use it before writing a row about coverage.** Rows [5b-86](../FRAGMENT-ISSUES.md),
[5b-92](../FRAGMENT-ISSUES.md) and [5b-95](../FRAGMENT-ISSUES.md) were all a scan being wrong about a shape;
this is the first time in the session the trace was run instead, and it changed the answer.

**Shown to fail against two realistic regressions**, not invented ones: tightening `cited_ids` to
hex-only ids — the obvious *tidy this up* edit — **4 red**; a neater `[^\]]*` regex that forgets the
whitespace rule, **2 red**.
