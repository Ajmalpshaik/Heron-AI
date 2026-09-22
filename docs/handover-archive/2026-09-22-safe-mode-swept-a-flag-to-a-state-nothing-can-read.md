# Session note — SAFE MODE SWEPT A FLAG TO A STATE NOTHING CAN READ

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — SAFE MODE SWEPT A FLAG TO A STATE NOTHING CAN READ

**[Row 5b-116](../FRAGMENT-ISSUES.md), FIXED.** `brain/heron_safemode.py` is read end to end — 370
lines, 2 public functions, **one suite**, and nothing in `brain/` or `mcp/` imports it. That is why it
was picked: thinnest-held of the six part-read brain modules, and the highest cost if it is wrong.

Safe Mode restores every flag to the state it held at a moment the person names. `_target()` walks the
recorded changes backwards and returns the `was` of the earliest one at or after that moment.

**`heron_flags` records `was` as the state a change moved AWAY from — so the FIRST change to a flag
carries `was: ''`.** There was no state before it.

**Measured by running the real `set_flag`, not inferred:**

| a flag declared as | its first change records |
|---|---|
| `{}` | **`was=''`** |
| `None` | **`was=''`** |
| `{"state": "ON"}` | `was='ON'` |

**So a flag created after the named moment was swept to `state: ''`** — and `''` is not one of
`heron_flags.STATES`, which is `('OFF', 'ON', 'TEST')`.

**The flag agent itself refuses that state.** `set_flag` answers `NOT_A_FLAG_STATE`; `read()` answers
`NOT_A_FLAG_STATE` with **`runs: False`** — and `read()`'s own comment calls that the failure it
exists to prevent: *"a typo answered silently leaves a component switched off forever and nothing ever
says why."*

**And `could_not` named nothing**, against this module's rule stated twice in its own docstring — what
it cannot judge is *"named and left alone"*, because *"the failure of the alternative is silent."* It
was neither: the flag was changed, and to nothing.

### The suite already asked for this and could not catch it

Section 1 asserts `FLG.meaning(state) is not None` over **every swept flag**. The fixture `table()`
holds no flag that was created after the moment — so **nothing ever handed the check the thing the
rule exists to refuse.** [Rows 5b-102](../FRAGMENT-ISSUES.md), 5b-106 and 5b-109 are that shape exactly,
and this is the fourth.

`_target()` refuses with a new **`NO_STATE_THEN`** and leaves the flag alone, which is the answer this
file already gives `NO_HISTORY` for the same reason. **The test is the flag agent's own vocabulary,
not a second copy** — `FLG.meaning(target) is None`. **The flag is not removed**: Golden Rule 4, a
record is never destroyed.

```bash
python tests/test_safemode.py      # section 8b, 4 red against the module as found
```

**It had to go in at 8b rather than at the end**, because section 8's last check asserts every
declared failure was **reached** — a new refusal exercised after it reads as unreached. **Confirmed
the hard way rather than assumed**: the fix was reverted, the suite re-run to see the same four go red
at the new position, and restored.

**One thing checked and dismissed**: `_moment` accepts both `T` and a space as the date/time separator
and compares as text, so two changes at the same instant in different spellings would sort wrongly.
`heron_flags` writes whatever the approval's `at` says, so the shape comes from the caller — but **no
mixed-format table exists anywhere in the repository**, and a row about it would be a finding made
from a shape rather than from a measurement.
