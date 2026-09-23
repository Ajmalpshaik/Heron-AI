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
| [`data/owner-questions.yaml`](data/owner-questions.yaml) | **The owner's own 79 questions**, from his log of real work, each with the capability, skill or tool that should answer it — chosen from the capability list, never from what the search returns, and confirmed by him. [`tools/score-routing.py`](../tools/score-routing.py) scores Heron's search against it and `test_score_routing.py` holds it honest. **A question here is never reworded and an answer changes only on his word** |
| `Heron.*.TestHost/` | The C# hosts that link Heron's own C# **by source** and run it with no Revit, no Windows and no model. `Bridge` stands in for the add-in so the named-pipe round trip can be tested, and has to be built before `test_bridge_roundtrip.py` can say anything; `Kernel`, `StackGuard`, `Banner` and `BindingNote` each have a runner in this folder, `Installer` is run by `test_installer_engine.py`, and `FailureNote` - the fragment write path's failure rule, added 2026-09-23 - by `test_failure_note.py`. **This row said "Five of them" until 2026-09-23**, while `Installer` made six: a typed count one sentence from the command that derives it. **It named only `Bridge` until 2026-09-21**, in the table headed *what is here* — and `StackGuard` was the one that compiled every day and ran on no day ([row 170](../docs/FRAGMENT-ISSUES.md)). Derive them: `ls -d tests/Heron.*.TestHost` |

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

**Every suite that cannot run now says so with 3.** This paragraph named `test_bridge_roundtrip.py`
and `test_served_claims.py` as exiting 1 instead, until 2026-09-21; both return 3, and the first one
carries the reason in its own source — *"EXIT 3, NOT 1, AND THE DIFFERENCE IS THE WHOLE OF
FRAGMENT-ISSUES ROW 162"*. The exception was written before that row fixed it and outlived it by a
week ([row 5b-57](../docs/FRAGMENT-ISSUES.md)).

That matters more than a tidy sentence: **CI now splits on the exit code**, so a suite that came back
1 where 3 was expected would be a failure rather than a skip. Read the output anyway — an exit code
says *whether* something ran, never *why* it broke.

## `check-gaps.py` runs every suite on disk

It skips none. **This section said it ran *"40 of the 41"* and skipped `test_bridge_roundtrip.py`
until 2026-09-21, and told a reader reconciling the two numbers to expect a difference of one — the
difference is zero, so anyone who followed that went looking for a lost suite that does not exist**
([row 5b-57](../docs/FRAGMENT-ISSUES.md)). The skip was real once; it stopped being real when that
suite learned to say *could not run* instead of failing.

Derive both rather than reading either here:

```bash
ls tests/test_*.py | wc -l     # suites on disk
python tools/check-gaps.py     # what it ran, sorted into unfinished vs waiting
```

## Before you change a test

**Never edit a test until it passes.** A red test is a claim about the code; making the claim quieter
does not make the code right. If a suite is genuinely testing the wrong thing, say so and re-base it
deliberately — `test_embed` and `test_retrieve` were re-based against a changed backend, which is a
different act from editing until green, and it was recorded as such.

**A suite that needs a real Revit is not a unit test.** Nothing in this folder proves a model changed.
That evidence lives in [`NEEDS-CHECKING.md`](../docs/NEEDS-CHECKING.md) and the proving workflow, and
a compile or a green suite is not a substitute for it ([D-30](../docs/DECISIONS.md)).
