# Session note — A SKILL MAY NOT NAME A FRAGMENT, AND `isupper()` WAS NEVER THAT RULE

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A SKILL MAY NOT NAME A FRAGMENT, AND `isupper()` WAS NEVER THAT RULE

**[Row 5b-106](../FRAGMENT-ISSUES.md). FIXED.**

`brain/heron_skill.py` states its central rule in capitals — *"A SKILL NAMES CAPABILITIES, NEVER
FRAGMENTS ... So a fragment can be improved, replaced, split into three or retired, and not one skill
is edited"* — and `validate()` carried the matching refusal word for word. **The test behind that
sentence was `capability.isupper()`, and `'FRG-ELE-001'.isupper()` is `True`.**

So `needs: [FRG-ELE-001]` validated **clean**, and `main()` then listed it under **CAPABILITY GAPS** —
the list that tool calls *"what to build next, in the order real work asks for it - not a guess"*.
**Naming a fragment produced an instruction to go and build a capability called `FRG-ELE-001`.**

**The pattern is the fragment side's own, imported rather than written again.**
`heron_fragment.CAPABILITY_PATTERN` is `^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$`, and `heron_skill` already
imports that module — so the two halves cannot come to disagree about what a capability name looks
like, which is how a rule ends up enforced in one place and not the other.

**Measured against every real name rather than an example, before the rule was tightened:**

| | |
|---|---|
| capabilities in the library it accepts | **396 of 396** |
| fragment ids it accepts | **0 of 396** — every one carries hyphens |
| requirements the ten skills already declare that it refuses | **none**, of 22 distinct |

**Shown to FAIL**: `tests/test_skills.py` §8, **1 red** against the module as found — and the four
checks that nothing legitimate is refused were **green before the fix**, which is what made it safe to
make rather than a hope.

> **SECTION 2 OF THAT SUITE COULD NOT HAVE CAUGHT IT, and it is [row 5b-102](../FRAGMENT-ISSUES.md)'s
> shape a third time.** It asserts `all(c.isupper() ...)` over the **real** skills, so every case ever
> put through the check was a genuine capability out of the real library, and nothing ever handed it
> the thing the rule exists to refuse. **The input set was the gap, not the checker.** §8 hands it one.
