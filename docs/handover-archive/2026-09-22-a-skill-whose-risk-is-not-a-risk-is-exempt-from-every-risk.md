# Session note — A SKILL WHOSE RISK IS NOT A RISK IS EXEMPT FROM EVERY RISK CHECK

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A SKILL WHOSE RISK IS NOT A RISK IS EXEMPT FROM EVERY RISK CHECK

**[Row 5b-140](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-skill-routing.py` read end to end — 770
lines, and **no suite loads it**.

`classify()` decides a crossing by comparing two declared risks. `rung()` answers **-1** for anything
off the ladder and `ASKS_A_QUESTION` does not hold, so **every risk branch falls through**:

```
classify("",         "ADMIN",  ...)  ->  miss
classify("Modifies", "MODIFY", ...)  ->  miss
```

And such a skill **is** routed. `heron_skill.load_all()` handles a file it could not parse and a file
with no id, but **does not call `heron_skill.validate()`** — validation is a separate function.
Measured on a fixture: a skill with no `risk:` and one with `risk: Modifies` both came back in `found`
with **zero problems reported**. The report's only heading about skills it did not route says *"SKILLS
THAT WOULD NOT LOAD"*, which such a skill is not one of — so the reader is shown **`CROSSINGS: 0`** for
a skill whose risk nothing knows.

**Not a live failure, and that is part of the finding.** All ten skills declare a risk on the ladder,
and `tests/test_skills.py` calls `validate()`, so CI would name such a file. What was missing is this
tool saying so about the verdicts **it is the only one making**. `skills()` answers
`(loaded, problems, unrated)` now, and the report names them.

#### A correction to this session's own measurement

**Two pull requests carried it, so it is written down here.** This tool was called *"named by a suite
only in prose"*. That was **wrong**: `classify`, `unsettled`, `words_moved`, `fingerprint_lines` and
`NOTHING_FOUND` are all exercised by `tests/test_skill_proving.py` and `tests/test_skill_catalog.py`,
**through `prove-skill.py` and `generate-skill-catalog.py`, which import them** — a second hop the
first scan did not follow.

**Re-measured counting that hop, exactly two never-read tools are run by nothing at all** — not
directly, not through another tool, not by CI:

| tool | lines |
|---|---|
| `check-risk-crossings.py` | 440 |
| `api-changes.py` | 208 |

What nothing touched *here* is the tool's **own** half — `skills`, `without_the_store`,
`declared_by_fragments`, `margin`, `rung` — and that is what the new suite is. It deliberately does
**not** re-assert the five verdicts `test_skill_proving.py` already pins: two copies of a judgement is
the thing this tool's own docstring argues against.

#### Traced and not raised

`--revit` is not validated, but an unsupported release is **not** the [row 5b-127](../FRAGMENT-ISSUES.md)
shape. Measured at `2019` and at `banana`: all 43 utterances land in a loudly printed
**NOT RESOLVED (43)** with every other list at zero, and the release used is the first line of the
report. Argparse also refuses an unknown flag here, unlike rows 5b-112, 5b-127, 5b-129 and 5b-132.

**Left for whoever reads `prove-skill.py`:** `classify` is **imported** by it, so the same silence is
in its proof path. Changing `classify`'s five return values from inside a tool that has not been read
would be widening someone else's contract, so the report was made honest instead.

| Break | Red |
|---|---|
| **`skills()` back to two answers** (the module as found) | **1** |
| `rung()` answering 0 instead of -1 for an unknown risk | 2 |
| an OUT counted as inside the skill's plan | 1 |
| counts that could not be read reported as zero | 1 |
| one sentence claimed by two skills no longer reported | 1 |

#### Four things measured and found right

The identity/ranking split is read from disk so it cannot wobble between runs (row 116); an OUT is a
guaranteed disagreement and is marked as one; `margin` reports the two routes' **ranks** rather than
inventing a scalar the seam does not carry — which is why row 109's *"2.4 ranks clear"* is not
reproducible here and was taken by hand; and a store whose counts could not be read says so under D-52
instead of reading as empty.
