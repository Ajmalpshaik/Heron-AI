# Session note — A QUARTER OF THE SWEEP'S QUESTIONS CANNOT PRODUCE ITS FINDING

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A QUARTER OF THE SWEEP'S QUESTIONS CANNOT PRODUCE ITS FINDING

**[Row 5b-141](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-risk-crossings.py` read end to end — 440
lines, and **one of exactly two never-read tools that nothing runs**: no suite directly, no suite
through another tool, and not CI.

Its docstring is explicit about what makes the list worth anything:

> *"A question a fragment already declares is worth less here, not more: the point is the sentences
> nobody thought to claim."*

and the ten skill sentences added on 2026-09-19 were chosen because each *"is declared in a skill's
`utterances:` and declared by NO fragment — measured, not judged"*.

**That criterion has gone stale under the list.** Measured against the live store:

```
asked                78
answered by IDENTITY 26   <- a fragment declares these now
left to RANKING      52   <- and ranking is what answers a question with a write
```

An identity answer is fixed at that fragment's own declared risk, so it leaves by the risk test's
`continue` and **can never be a crossing here**. Eight of the ten skill sentences the criterion was
written for are among the 26 — including *"how many sprinklers"*, *"is the ductwork still connected"*
and *"what sizes are the ducts"*. The report said `questions asked: 78`, which is the size of the
**list** rather than the size of the **question**: the sweep finds **7** crossings, and that is 7 of
52, not 7 of 78.

**Also found, and the file argues against it itself.** `CHANGES_THE_MODEL` and `CHANGES_A_VIEW` — the
two tuples that decide the whole verdict — were a verbatim second copy of `check-skill-routing.py`'s.
Three functions earlier the same file explains why it *imports* the store fingerprint rather than
keeping one: *"two copies of one rule is how one of them goes stale."* They are imported now.

#### The extraction was proved, not asserted

`read_that_lost(found, winner)` lifts the discriminator out of `main()`, where nothing could reach it
without opening a store — and a count from that store is a **sample**, not a measurement (row 116:
three sweeps minutes apart gave 7, 9 and 8, two on byte-identical inputs). So both forms were run end
to end and returned **the same seven crossings, in the same order, with the same beaten read**:

| question | answered by | beat |
|---|---|---|
| what views are on this sheet | `PLACE_VIEWS_ON_SHEET` | `FIND_UNPLACED_VIEWS` |
| what is the insulation thickness | `SET_MEP_INSULATION` | `CHECK_INSULATION_CLEARANCE` |
| how high is this off the floor | `MOVE_TO_RAY_HIT` | `MEASURE_CEILING_HEIGHT` |
| find the fire dampers | `PLACE_ACCESSORY_ON_RUN` | `SELECT_BY_FAMILY` |
| hide everything except the walls | `HIDE_ELEMENTS` | `REPORT_COMPOUND_STRUCTURE` |
| what is the scale of this view | `SET_VIEW_SCALE` | `REPORT_VIEW_TEMPLATE_CONTROL` |
| what levels are in this model | `CREATE_LEVELS` | `LIST_LEVELS` |

The store's md5 **differed between the two runs**, which is the tool's own documented behaviour and not
a fault — it prints *"Asking moved the index"* for exactly this.

#### One of the suite's own checks crashed rather than failed

It called `denominator_lines` before it existed and got an `AttributeError`, **which proves nothing**
([`heron-ship` §2a](../../.claude/skills/heron-ship/SKILL.md)). Both new names are read through `getattr`
now, so the run against the module as found is clean red.

| Break | Red |
|---|---|
| **the module exactly as found** | **4** |
| the two tuples copied back alone | 2 |
| EXECUTE counted as a read that lost (row 145's mistake) | 1 |
| the winner counted as the read it beat | 1 |
| the denominator no longer naming the route | 1 |

**Traced and not raised:** argparse refuses an unknown flag here; the list holds no duplicate and no
stray whitespace; and the docstring's *"45 ordinary questions found 17"* is a dated historical
measurement rather than a stale count, so it is left as written.

**`api-changes.py` (208) is now the only tool left that nothing runs** — and it needs the network, so
it can be read and its claims traced, not run.
