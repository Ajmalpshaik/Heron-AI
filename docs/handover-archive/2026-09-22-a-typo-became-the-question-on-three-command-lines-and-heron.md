# Session note — A TYPO BECAME THE QUESTION ON THREE COMMAND LINES, AND `heron_ground.py` IS FINISHED

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A TYPO BECAME THE QUESTION ON THREE COMMAND LINES, AND `heron_ground.py` IS FINISHED

**[Row 5b-112](../FRAGMENT-ISSUES.md), FIXED. [Row 5b-113](../FRAGMENT-ISSUES.md), OPEN.**
`brain/heron_ground.py` is read end to end — 1,115 lines, completing the part-read of 2026-09-21.

**5b-112 is [row 5b-104](../FRAGMENT-ISSUES.md)'s twin**: that one was a flag with no VALUE, this is a
flag with no MEANING. **Measured by running all ten query command lines** with a flag that does not
exist:

| | |
|---|---|
| `heron_company.py "how thick is duct insulation" --scope project` | searched for `'how thick is duct insulation --scope project'`, **exit 0** |
| `heron_search.py "how thick is duct insulation" --top 5` | printed **`Asked:  how thick is duct insulation --top 5`**, **exit 0** |
| `heron_ground.py --draft <file> --question "…" --rebuild` | the flag vanished, the store opened, the answer came back about the question alone |

**`heron_company` is the sharp one**, because the comment three lines above that loop states the rule
in the present tense — *"THE FLAGS STOP THE QUESTION, AND AN UNKNOWN ONE IS REFUSED"* — and only the
two flags it **has** were ever stopped. **Row 5b-104 closed that door for `--subject` and `--stage` on
the same day and left it open for every other flag.** Fixed one half of a finding and left its twin,
which this register has now recorded four times.

**THE HOUSE ANSWER HAS EXISTED SINCE 2026-08-30, IN THREE MODULES.** `heron_retrieve`,
`heron_conflict` and `heron_research` all print `not a flag this tool has: --top` and exit **2**, and
`heron_retrieve.main` carries the reason in a dated comment: a typo silently searched for *"reads as a
measured result rather than a typo"*. Three weeks, and it reached no sibling. **All three refuse in
those words now, before the store is opened**, so a typo costs nothing.

```bash
python tests/test_company.py     # section 10, 3 red before the fix
python tests/test_search.py      #             4 red
python tests/test_ground.py      # section 12, 3 red
```

**THE EXIT CODE IS GREEN EITHER WAY ON `heron_ground`, AND THAT IS WRITTEN INTO THE SUITE.** An empty
store already refuses with 2, so `code == 2` proves nothing there — what separates the two worlds is
whether the flag is **named**, in whose **words**, and whether `NO DOCUMENT IS INDEXED` was ever
reached. **And the first version of the `test_search` check CRASHED rather than failed**: it ran after
`HERON_KNOWLEDGE` had been popped, so it went red with a `ValueError` out of `open_scope` and proved
nothing about the flag. It arranges its own store now. **That is heron-ship §2a, hit again in the same
session that recorded it.**

### And row 5b-113, which is a question rather than a fix

**A sub-clause letter turns a citation into an invented fact, and the same clause spelled out gets the
right answer.** Measured end to end, one sentence against a packet holding clause 9.1.1, citing a
clause the packet does **not** carry:

| the draft cites | verdict | reported as invented |
|---|---|---|
| `[9.1.2]` | **unresolved** | `25mm` |
| `[7a]` | **uncited** | `25mm`, **`7a`** |
| `[4.1a]` | **uncited** | `25mm`, **`4.1a`**, **`4`** |
| `[clause 4.1a]` | **unresolved** | `25mm` |

`check()` separates those two verdicts deliberately — *"TWO DIFFERENT WRONGS, AND THEY NEED DIFFERENT
ANSWERS"*, R-65 for a sentence that cites nothing and R-22 for a citation a human cannot follow — so a
modeller who **did** cite is told they cited nothing, and the citation's own text is listed as a fact
the source does not carry. This module's docstring calls that the failure that gets it switched off:
*"every false flag it produces is a true sentence called a lie."*

**NOT FIXED, and the reason is that the obvious fix re-opens a door two review rounds closed.**
`_MEASURED` exists so that `[50mm]` stays a claim — bracketing a value turned the fabrication check
off for that sentence, twice. **Narrowing its letters to Heron's own `_UNITS` does not help, and that
was checked rather than assumed: `a` is amperes and `7a` still loses.** The module already has a
stated policy for exactly this — `_is_marker`'s note on `[99]`, *"the honest edge"*, says `known`
decides and **where nothing resolves it, this still reads it as a MARKER**. The lettered case does the
opposite of the policy written beside it. **That one sentence is the decision, and it is his**: a
sub-clause letter in brackets — is it a locator or a value?

**What was NOT swept in, measured and left alone**: `heron_matrix`, `heron_gaps` and `heron_skill`
take no arguments at all and ignore what they are given, which is a different question from a flag
reaching a search; `heron_capability` treats its arguments as capability names by design.
