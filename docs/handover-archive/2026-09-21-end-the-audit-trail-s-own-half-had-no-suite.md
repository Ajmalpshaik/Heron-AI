# Session note — (end) — THE AUDIT TRAIL'S OWN HALF HAD NO SUITE

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (end) — THE AUDIT TRAIL'S OWN HALF HAD NO SUITE

**[Row 5b-91](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_audit.py`, the **eighth** live-path brain
module.

**Nothing under `tests/` imported it.** The BUILD ORDER check in `tools/check-gaps.py` covers the
**fourteen steps docs/27 describes**; this file carries `Heron-Step: 17`, so no gate noticed.

It is Golden Rule 14's requirement for the half of Heron that never reaches Revit, it is D-62's
deliverable, and every line it writes goes into a file that is **append-only and never pruned**. Its
docstring argues carefully about exactly that — the day `ms` went in quoted and a reader compared
*"9"* against *"6620"* as text; never writing the user's sentence; the refusal codes that stop a
correct refusal reading as a fault. **None of that argument was held by anything.**

**And the promise in capitals was broken three ways.** `record()` says *NEVER FATAL*, and its
`numbers` loop does `int(value)` **outside the `try`**. Measured: `ms="fast"` → `ValueError`,
`float("nan")` → `ValueError`, `float("inf")` → **`OverflowError`, not even in that except list**, a
list → `TypeError`. **The `ValueError` sitting in the except clause is the tell**: the author
anticipated the conversion failing and guarded the wrong statement.

**Not reachable from any caller** — all thirteen pass a `len()` or a `rowcount` — **and that is the
dangerous half**: the early `return False` when there is nowhere to write means this code only ever
runs where the trail really writes, which is somebody's machine and never this container.

**Fixed**: the conversion is guarded per key, a bad number costs that one field and never the line,
and it is **not** written as a string instead (the log is never pruned, so a quoted number is
permanent). A `dropped` field names the key, because Golden Rule 14 does not allow a silent discard.

**`tests/test_audit.py` is the larger half.** Eight sections holding the docstring's *arguments*
rather than its lines. The central one runs **end to end through the reader**: three real refusals
written, then `heron_gaps.analyse()` asked, and they must come back as **three correct refusals, none
unclassified, none a defect** — which is `heron_gaps`'s own founding mistake tested from the other
side. §6 proves **Q-44** the same way: an add-in file and a brain file in one directory come back as
one list sorted by `at`, a truncated line costing one entry and not the report.

**Shown to fail: 9 checks**, and the suite still ran to the end — each call sits in its own `try`, so
an escape is reported as the failure it is rather than ending the run. **Two checks were vacuous in
the first draft** (`all()` over an empty list is True); both now require a non-empty list first, which
is [row 5b-79](../FRAGMENT-ISSUES.md)'s false green in another shape.

**And the blind spot that let it happen is [row 5b-92](../FRAGMENT-ISSUES.md), measured and closed in
the same sitting.** `check-gaps.py`'s BUILD ORDER section asks *does every step docs/27 names have
code and a test* — not *does every brain module have a test*. A module above the fourteen is invisible
to it. **Measured with an AST walk over every suite: exactly ONE of the 145 was imported by no suite
at all, and it was `heron_audit`.** A single instance, not a class — said plainly rather than left as
a suspicion. The *THE BRAIN* section now asks the question too, and an untested module goes on the
UNFINISHED list by name. **A filename match would have reported six gaps that are not there** — six
modules have no suite of their own name and every one is well covered, `heron_fragment` by forty
suites — so it resolves imports, not names.

**Live-path brain modules read: 8 of 53.** Next: `heron_gaps` (read in passing for this row, not
marked), then `heron_capability`.
