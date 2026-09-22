# Session note — THE API PAGE'S ONE FINDING WAS FALSE, AND TWO DOCSTRINGS TYPED A STALE COUNT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE API PAGE'S ONE FINDING WAS FALSE, AND TWO DOCSTRINGS TYPED A STALE COUNT

**[Rows 5b-146 and 5b-147](../FRAGMENT-ISSUES.md), both FIXED.** Three generators read end to end —
`generate-skill-catalog` (660), `generate-fragment-catalog` (359), `generate-api-docs` (338) — through
`tests/test_catalog.py`'s rule: *a generator that only draws needs no test; one that **concludes**
does.* **All three conclude, and all three already had a suite.**

#### 5b-147 — the one that mattered

`generate-api-docs`'s stated conclusion is *"A PARAMETER NOTHING EXPLAINS"*, and the page reported
exactly one: **`revit_change: nothing explains capability`**.

**It is explained.** `heron_mcp_server.py:806` says *"Ask for the **CAPABILITY**, never a fragment id"*
— capitals being the house style for emphasis. `explains()` matched with `\b…\b` and **no
`re.IGNORECASE`**, so `capability` missed `CAPABILITY`.

A page carrying one finding, with that finding wrong, is the failure its sibling already records:
`generate-contract-reference.py` says *"A page of 135 findings that are all wrong is worse than no
page: it teaches the reader to skip the table, which is where the real ones are."* At one finding it
teaches the same lesson faster.

Fixed case-insensitively. **The word boundary is what does the work and is untouched** — `full` is
still not explained by `FULLY`, asserted in both cases now. The page reports **34 tools, 26 parameters,
2 that change the model, and no unexplained parameter**.

#### 5b-146 — two docstrings typing a count the page derives one line lower

| file | typed, present tense | derived |
|---|---|---|
| `generate-fragment-catalog.py` | *"349 are catalogued"* | **396** |
| `generate-api-docs.py` | *"defines eighteen tools"* | **34** |

The fragment catalogue is the file whose own argument is *"a generated artefact cannot lie about its
source"*. **The page never did; its docstring did.** Both now name the command instead. The dated
sentences in each — api-docs' *"the first time this ran"*, and 349 as what the library held the day it
was written — are records of a run and stay as they are.

#### `generate-skill-catalog` is SOUND — a negative result, ledger `clean`

It says so itself: *"IT CONCLUDES, SO IT HAS A TEST."* Measured rather than taken on trust:

- Its docstring's claim — *"seven of the ten skills rest on a chain that is PROVEN all the way down,
  and of those seven exactly ONE has every sentence it declares reaching a capability it declares"* —
  **still holds**: the page prints `7 PROVEN all the way down, 3 weaker` and `1 reach throughout`.
- Its staleness guard is clean: `words_moved` reports **zero** gone and **zero** fresh phrases across
  all ten skills, and every skill has a recording (`routing-2026-09-19b.json`).
- `tests/test_skill_catalog.py` covers **both** halves — the chain (six numbered claims) and the words
  (`routing()` with a missing folder and an empty one, `said()` with a five-field old recording and a
  six-field one).

**Teeth:** 2 red against `generate-api-docs` as found; 4 red with the word boundary dropped. All four
suites that touch these three tools pass. Every page was run into a scratch path, never over anything
committed.
