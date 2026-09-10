# tests/

**What is here, how to run it, and how to read an exit code.**

This is local orientation only. The testing *model* — the eight levels, what each is for — is
[docs/13](../docs/13-testing-and-quality.md). The *order to run things in before pushing* is the
[`heron-ship`](../.claude/skills/heron-ship/SKILL.md) skill. Neither is repeated here.

## What is here

| | |
|---|---|
| `test_*.py` | The suites. One file per subject, each runnable on its own, each printing what it checked rather than a dot |
| [`golden/cases.py`](golden/cases.py) | **The Golden Test Library** — a permanent record of what Heron has been *proven* to do, run again after every major change ([docs/13 §2b](../docs/13-testing-and-quality.md)) |
| `Heron.Bridge.TestHost/` | A tiny C# host that stands in for the Revit add-in so the named-pipe round trip can be tested **without Revit**. It has to be built before `test_bridge_roundtrip.py` can say anything |

Derive the count; do not read one here:

```bash
ls tests/test_*.py | wc -l          # how many suites
python tests/test_fragment_store.py # run one
python tools/check-gaps.py          # run them all, sorted into unfinished vs waiting
```

## The three exit codes

This is the part that is easy to get wrong, and it is written down nowhere else.

| Exit | Means | Treat as |
|---|---|---|
| **0** | Every check passed | **PASS** |
| **1** | A check failed | **FAIL** — investigate it |
| **3** | The suite **could not run** for want of an optional dependency, and says so | **NOT RUN** — it proves nothing either way |

**Exit 3 is deliberate and is not a pass.** `test_mcp_serves.py` uses it when the MCP SDK is absent,
and `tools/check-gaps.py` reads it as *waiting for a machine* rather than counting it green. A suite
that cannot run must never be allowed to look like one that ran.

Not every suite that needs something exits 3 — `test_bridge_roundtrip.py` and `test_served_claims.py`
exit 1 when their dependency is missing. So **an exit code alone does not tell you whether a failure
is yours.** Read the output.

## `check-gaps.py` runs 40 of the 41, on purpose

It skips `test_bridge_roundtrip.py`, with the reason in its own source: that suite needs the compiled
test host, its absence is a build step rather than a gap in the code, and `check-compile.py` covers the
compiling.

So **"41 suites" and "what `check-gaps` ran" are two different numbers by design.** Anyone reconciling
one against the other should expect the difference of one and not go looking for a lost suite.

## Before you change a test

**Never edit a test until it passes.** A red test is a claim about the code; making the claim quieter
does not make the code right. If a suite is genuinely testing the wrong thing, say so and re-base it
deliberately — `test_embed` and `test_retrieve` were re-based against a changed backend, which is a
different act from editing until green, and it was recorded as such.

**A suite that needs a real Revit is not a unit test.** Nothing in this folder proves a model changed.
That evidence lives in [`NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) and the proving workflow, and
a compile or a green suite is not a substitute for it ([D-30](../docs/DECISIONS.md)).
