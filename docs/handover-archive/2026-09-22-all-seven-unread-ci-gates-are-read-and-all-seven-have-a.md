# Session note — ALL SEVEN UNREAD CI GATES ARE READ, AND ALL SEVEN HAVE A SUITE

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — ALL SEVEN UNREAD CI GATES ARE READ, AND ALL SEVEN HAVE A SUITE

**[Row 5b-135](../FRAGMENT-ISSUES.md), FIXED. [Row 5b-136](../FRAGMENT-ISSUES.md) raised, and it is a
definition the owner writes.** `tools/check-intrusion.py` read end to end — 213 lines, never opened
before. **The last of the seven.**

Its docstring records what the tool measured the first time it ran — **0.313 over 330 utterances**
— and then says in terms:

> Quote what THIS TOOL prints, never this line. An earlier hand-written probe on the same day gave
> **0.234** over 312 utterances … **Two numbers for one measurement is how a figure becomes a
> remembered composite that matches no run that ever happened**, which is a failure this repository
> has already had once and now has a checker for.

**And then the branch that fires when the correlation has gone strong printed the hand probe's
0.234 as what the tool measured** — at exactly the moment a reader needs the right baseline. It
quotes **0.313** now, with the corpus it came from.

**Also fixed, the same shape as [5b-127](../FRAGMENT-ISSUES.md)**: `--top` was read by index, so
`--top` alone was an `IndexError`, `--top six` a `ValueError`, and a misspelt flag was ignored in
silence — and this tool retrieves for **every utterance in the library**, so a mistyped flag costs
minutes before it costs a wrong answer.

**Run, not just read**: it answers **0.117** today over **2,227 utterances from 396 fragments**,
lexical backend — weaker than the 0.313 of 59 fragments, so the docstring's conclusion holds *more*
strongly than when it was written, and the strong branch has still never fired.

### The docstring is one of the most careful in the repository

It asks the question `check-routing` **cannot**: that one asks whether a fragment still wins its
*own* sentences, which says nothing about the shortlist a user sees — and five slots holding three
plausible answers and two irrelevant ones is a worse answer than three.

An intrusion is explicitly **not** a defect, because a shortlist is *meant* to hold more than one
candidate, and it exits 0 for that reason: *"a gate here would be a gate on how ordinary somebody's
phrasing is."*

And the obvious hypothesis — purpose **length** — was **tested and disproved** rather than assumed,
with decisive counter-examples rather than marginal ones. That is worth having measured before
anyone spends a day shortening prose. The finding is placed on the **retrieval layer**, not on any
fragment, because the generic phrasing at the top of the list is real sentences a modeller says,
and `brain/retrieval-history.md` rules out taking them away to buy a number.

### A trap found while proving the teeth, now in heron-ship §2a

**A break and its restore inside the same second, at the same file size, are served stale
bytecode.** CPython validates a `.pyc` against the source's mtime at **one-second resolution**.

```
tools/__pycache__/check-intrusion.cpython-311.pyc   09:49:36.754
tools/check-intrusion.py                            09:49:36.760
```

Six milliseconds apart, identical length. The restored run went **red on the broken behaviour with
the correct source on disk**, and `inspect.getsource` printed the correct source while the loaded
function returned the broken answer.

**The dangerous direction is the other one**: a break served stale *good* bytecode reads as **no
red** — which is exactly what would be reported as *"the suite does not cover this"*. Every teeth
proof in this session was re-checked against that: each no-red result was a block removal, which
changes the file size and invalidates the cache regardless, and each had an explanation recorded at
the time. `rm -rf tools/__pycache__` between every break and every restore is now written into the
skill.

### What is left, and it is a definition

**[Row 5b-136](../FRAGMENT-ISSUES.md).** Four tools declare `Heron-Layer: brain` while living in
`tools/` — `check-routing`, `check-intrusion`, `check-skill-routing`, `check-risk-crossings` — and
every other tool says `tool`. [`docs/29`](../29-metadata-standard.md) lists the six permitted values
and **defines none of them**; `check-metadata` only asks that the value is one of the six. **The
folder is not the answer either**: all of `mcp/` says `bridge`.

Nothing is broken — `check-structure` does its layering from the folder, not from this header. What
is missing is one sentence saying which reading governs, and once it exists **either four files
change or forty do**, which is why guessing it from here would be the wrong kind of tidy.

### The seven, finished

| gate | outcome |
|---|---|
| `check-narrow-errors` | [5b-128](../FRAGMENT-ISSUES.md) — a gate a comment could satisfy |
| `check-licence` | [5b-129](../FRAGMENT-ISSUES.md) — a German word counted as a licence |
| `check-metadata` | **sound** — and the suite caught itself first |
| `check-products` | [5b-132](../FRAGMENT-ISSUES.md) — a newline was a folder name |
| `check-signatures` | [5b-130](../FRAGMENT-ISSUES.md) fixed · [5b-131](../FRAGMENT-ISSUES.md) for the owner |
| `check-fragments-compile` | [5b-133](../FRAGMENT-ISSUES.md) fixed · [5b-134](../FRAGMENT-ISSUES.md) needs the SDK |
| `check-intrusion` | [5b-135](../FRAGMENT-ISSUES.md) fixed · [5b-136](../FRAGMENT-ISSUES.md) needs a definition |

**Next by the same measurement**: `tools/` holds 47 tools; the six no suite named and the seven CI
runs are done. `python tools/review-ledger.py --next 20` picks the next, and the STALE files from
the installer session are still held until PR #253 merges.
