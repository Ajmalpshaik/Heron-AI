# Session note — ELEVEN OF FOURTEEN REPORT `0`, AND THAT ZERO MEANS THREE THINGS

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — ELEVEN OF FOURTEEN REPORT `0`, AND THAT ZERO MEANS THREE THINGS

**[Row 5b-148](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-revit-gate.py` read end to end — 651 lines,
a checker CI does not run.

It asks the master architecture document's fourteen questions of every fragment and gives each one of
**four** verdicts, and its docstring is emphatic about why there are four: they *"describe EVIDENCE,
not lifecycle — docs/24's two axes are untouched and this adds no third vocabulary"*.

**Then `sweep()` collected only the `LOOK` verdicts and threw the rest away.** The summary a reader
actually reads was fourteen rows of a single number.

**Measured by tallying every verdict across all 396 fragments** — eleven of the fourteen read `0`:

| row | reads | actually means |
|---|---|---|
| Q11 units | `0` | **ANSWERED for all 396** — the check ran and found nothing |
| Q9 element ids | `0` | **NEEDS A RUN for all 396** — nothing was checked at all |
| Q13 could it modify the model | `0` | **BY DESIGN for all 396** — and this file's own docstring says it **cannot** decide that question; D-28's executor answers it |

**One presentation, three meanings**, in the tool whose whole argument is that the four verdicts are
different. AGENTS.md is blunter: *"Separate the four states and never merge them: PASS · FAIL · NOT RUN
(say why) · NEEDS REAL REVIT."* A question nobody asked reading as a zero is that rule broken in the
tool's own headline.

**A leftover says it used to know**: `labels = {LOOK: "worth a look"}` sat two lines above, assigned
and never used.

#### The fix

`sweep()` tallies every verdict, and a row with no looks carries the verdict that stood instead, under
a three-line legend. **Nothing else changed** — the fourteen questions, their order, the four verdicts
and every judgement in `ask()` are untouched, and it still exits 0 because a finding here is a question
for a person.

#### The first draft of the case passed for the wrong reason

It sat inside the block where the library is a **temp fixture**, so `entries` was empty, every row had
no verdict to report, and the check read as a defect in the tool. Moved to the section that loads the
real library. `tests/test_revit_gate.py` already owned this tool — five claims — and was **extended,
not duplicated**: **2 red** against the module as found. Teeth two ways: the note dropped from the row
(2 red), and the note kept but no longer naming which verdict stood (2 red).

#### Traced and not raised

Every remedy the report names **works**: `--list links` prints 68 names and `--list reporting` 65,
measured. The `> 20` spread threshold is a display choice with the count still printed beside it. An
unknown `--list` key is ignored in silence — the shape of rows 5b-112, 5b-127, 5b-129 and 5b-132 — but
here the output names the right key on the very line that sends you looking.

**And the docstring is one of the best in the repository**: it records a failed design honestly
(`.Create(` flagged three READ fragments and all three were wrong; narrowing to calls taking `doc`
found zero and missed 76 MODIFY fragments) and concludes *"the write surface is the API, and no word
list is the API"* — then says the executor answers question 13 better than any text search could, and
declines to compete.
