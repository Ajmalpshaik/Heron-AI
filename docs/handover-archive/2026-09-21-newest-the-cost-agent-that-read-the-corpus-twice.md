# Session note — (newest) — THE COST AGENT THAT READ THE CORPUS TWICE

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (newest) — THE COST AGENT THAT READ THE CORPUS TWICE

**[Row 5b-88](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_classify.py`, the **sixth** live-path brain
module. Its `_shape()` reads `SNIFF = 8192` bytes to decide text or binary, under a comment saying the
bound is there because it is *"small enough that a folder of large binaries costs nothing"* — and then
called `json.load` on the **whole file** for every file that came back text. **The bound protected
binaries and not text, which is the half this agent actually walks.**

**Why it is worth a row rather than a shrug: cost is this file's own argument.** Its docstring is
headed *"THE COST PROBLEM IS THE DESIGN PROBLEM, AND IT IS WRITTEN DOWN"*, quotes
[19 §96](../19-context-and-cost.md) — *"Batch classification of 20,000 fragments should not run on a
frontier model"* — and groups files so sixty-one thousand become a handful of questions. It reports
the question count **so nobody has to guess what a run will cost**, and then read the corpus twice
without saying so. And its input is **somebody else's repository**, which is the one place a 120 MB
log or SQL dump is ordinary.

**Measured twice, and the numbers are the deliverable.**

| | old | new |
|---|---|---|
| 120 MB of prose starting with `t` | read in full, **240 MB peak**, 0.12s warm | **not opened** |
| this repository, bytes read | **30.27 MB** | **11.50 MB** |
| this repository, files opened twice | **2,127** of 2,127 | **7** |

**The head test is sound, not a heuristic**, which is the only reason it may decide anything: RFC 8259
says a JSON text is one value, and every value begins with `{`, `[`, a quote, `-`, a digit, or the
exact words `true`, `false`, `null`. A file inside the sniff is parsed from the bytes already in hand;
a larger one whose head cannot begin a value is rejected on that head; **only a large file that really
does start like JSON is read in full**, and that is the one case where reading it is the only way to
know.

**Equivalence was proved BEFORE the cost was** — 22 cases through both paths, including the near-misses
`truthy`, `nullify` and `for the record`: **cases where the answer changed: 0.**

**THE TEST COUNTS OPENS RATHER THAN SECONDS.** A timing assertion would be flaky on somebody else's
machine. `tests/test_classify.py` §8 swaps a counting `io` into the module and asserts how many times
each fixture is opened, end to end through `_shape`. **Shown to fail: 20 checks go red** against the
module as it stood.

**AND I REPEATED [ROW 5b-85](../FRAGMENT-ISSUES.md)'S OWN MISTAKE THREE ROWS LATER.** The first draft of
that section read `CLS.JSON_STARTS` directly, so against the old module it raised `AttributeError` and
the remaining checks never ran — **one traceback where there were twenty failures to report**. Same
lesson, same day, same session, three rows apart. `getattr` with a default now, **and the reason is
written beside it in the suite**, because knowing the lesson and reaching for it are evidently two
different things. If you write a check against a name the module may not have, use `getattr`.

**Gates: all ten green.** `check-routing` and `check-intrusion` exit **2** on a Linux container until
`HERON_KNOWLEDGE` points at a folder — an empty one is enough — and then exit 0. That is the machine,
not the change, and it has now cost time twice.

**Live-path brain modules read: 6 of 53.** Next: `heron_flags`, `heron_gaps`, `heron_audit`.
