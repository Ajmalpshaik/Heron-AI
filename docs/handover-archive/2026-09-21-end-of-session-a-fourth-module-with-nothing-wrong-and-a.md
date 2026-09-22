# Session note — (end of session) — A FOURTH MODULE WITH NOTHING WRONG, AND A FIFTH WITH A TABLE THE CODE OUTGREW

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 (end of session) — A FOURTH MODULE WITH NOTHING WRONG, AND A FIFTH WITH A TABLE THE CODE OUTGREW

**[Row 5b-87](../FRAGMENT-ISSUES.md). FIXED.** `brain/heron_contract.py` — the module **all 249 other
agents declare themselves to**, and the file that defines the word BREAKING for this repository. Its
docstring lists **seven** rules for a breaking contract change. `compare()` enforces **eight**: a
**shortened timeout** as well, with its own argument beside it. **Measured:** 60s → 30s is
`BREAKING`, 60s → 120s is `COMPATIBLE` — the asymmetry is right, and it was entirely absent from the
table a reader reads. Somebody lowering a timeout on the strength of seven rules would expect
`COMPATIBLE` and raise a MINOR version.

**The test is the part that lasts.** `tests/test_contract.py` now asserts the behaviour **and** that
the module's own `__doc__` carries the rule — read from `CON.__doc__`, **not a copy of the table** —
so the two cannot drift again. Copying it into the suite would have created the second home this
module's own docstring refuses for `tier` and `risk`.

**Verified rather than taken, in the same file:** `registry_ids()` returns exactly **250**, matching
what `tools/agent-count.py` reconciles, and **all 127 contracts on disk** name an agent in it.

**`brain/heron_registry.py` read word by word. No defect.** That is the result rather than an absence
of one, and it is worth saying: four live-path modules read today, three had a defect and this one did
not.

**One thing was checked and discarded.** Its header regex accepts only `//` and `#` comments, so the
eight `.md` files carrying `<!-- Heron-Agent: -->` read as unidentified. **Correct and deliberate**:
`tools/check-metadata.py` uses the *identical* regex at line 129 and scans
`SOURCE_EXT = (.cs, .py, .ps1)` only, so a markdown header is outside the gate's scope by design, and
this agent agrees with the gate rather than inventing a second rule.

**A second was checked and discarded in `brain/heron_contract.py`**, which is otherwise **not yet
read**. Its docstring quotes the register as *"across **249** agents"* while its own `main()` prints
*"the **250** agents in the register"*, and `docs/28` says 250 in four places and 249 in exactly one
— the `HERON-AHR-CON-017` row itself. **That is almost certainly deliberate**: the same file writes
*"the 146 agents that need no Revit … all 145 others declare themselves to it"* two lines apart, so
*N and N−1 others* is this author's own idiom and 249 reads as *the other 249*. Unclear, not wrong.
**Not written up as a row**, because a suspicion recorded as a defect is worse than one discarded.

**Where the live-path reading stands: 4 of 53 done.** `heron_contract` (447 lines, partly read) and
`heron_classify` are the obvious next two.
