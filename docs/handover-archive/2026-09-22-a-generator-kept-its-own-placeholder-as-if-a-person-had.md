# Session note — A GENERATOR KEPT ITS OWN PLACEHOLDER AS IF A PERSON HAD WRITTEN IT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A GENERATOR KEPT ITS OWN PLACEHOLDER AS IF A PERSON HAD WRITTEN IT

**[Row 5b-124](../FRAGMENT-ISSUES.md), FIXED. [Row 5b-125](../FRAGMENT-ISSUES.md) raised, and it is the
owner's.** `tools/generate-decision-summary.py` read end to end — 167 lines, never opened before.
The **fourth** of the six unheld tools, and the one that writes into
[`DECISIONS.md`](../DECISIONS.md).

It rebuilds the Status summary table and **keeps an existing status cell verbatim**. That is right,
and it is the best idea in the file: *"read back 2026-09-06"* records a conversation, not a fact on
disk, and a generator that discarded those would lose the record of every read-back the owner has
ever done.

**But a decision that states no `**Status:**` line gets `• status not stated` derived for it, and
that cell was then preserved like any other.**

**Measured**, on a copy so the real document was never touched — give D-72 a
`**Status:** Accepted · **Date:** 2026-09-14` line and:

```
  --check exit 0 | Status summary is current: 97 decision(s), none missing.
  D-72 cell afterwards: '• status not stated'
```

The one thing this tool exists to prevent — the table falling behind the decisions, silently — it
does on its own placeholder. **Seven of the 97 decisions carry no `**Status:**` line**, so the
trigger is not hypothetical.

### What it does now

There is **one spelling of the placeholder**, used both to write it and to recognise it, and a cell
matching it is **re-derived rather than kept**. A cell a *person* wrote is untouched — including the
six hand-filled ones on D-75 to D-80, which do not match. A decision that still states nothing still
reads `• status not stated`, so nothing is invented. `--check` names what caught up as well as what
was missing.

The note the generator writes into `DECISIONS.md` said *"a status cell is kept verbatim once written
… it only fills in rows that do not exist yet"*, which the fix would have made untrue, so it is
rewritten in the same change. **That is the only line of `DECISIONS.md` this touches.**

**`tests/test_decision_summary.py`**: **3 red** against the module as found, every one failing
rather than crashing, and the checks either side of them green before and after — a curated cell
surviving, and a decision that states nothing still saying so. That is what makes this a *narrowing*
of what counts as curation rather than a loosening of the rule.

### Three things checked and found right, rather than assumed

| | |
|---|---|
| *"`--check` is what CI runs"* | **True, traced not grepped.** `check-docs.py` section 8 runs it as a subprocess and sets `failed` on a non-zero return, and check-docs is a CI gate. A false claim was nearly raised here before tracing it |
| the six-line window after each heading | **Wide enough** — measured, **zero** of the 97 decisions carry their `**Status:**` line further down |
| one status begins with a tick, not a word | The mark map falls through to the bullet and would double the mark — but it is D-00, its cell is curated, and no run derives it. **A shape, not a defect**, so it is not in the register |

**One check was caught green for the wrong reason and narrowed** before the fix went in: the
baseline table was typed by hand, which the generator calls stale on sight because of the note it
writes, so the case meant to show the freeze was passing on the wrong staleness. The baseline is the
tool's own output now.

### What is left, and it is one line of typing

**[Row 5b-125](../FRAGMENT-ISSUES.md) is the owner's.** D-72's cell is that placeholder today, and
after the fix no run will change it, because D-72 still states nothing of its own. Its body is not
ambiguous — dated 2026-09-14, a `### Decision` heading, superseding a paragraph of D-67 by name, and
the four types it released are in the library. But **a status cell is a claim about what he
decided**, which is D-30's shape exactly, so it is recorded rather than written. Either line settles
it: a `**Status:**` line in D-72's own block, or the cell typed by hand the way D-75 to D-80's were.

**Two of the six unheld tools remain**: `measure-graph`, `module-reach`. Both are **reports**, not
writers.
