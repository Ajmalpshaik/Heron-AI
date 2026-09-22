# Session note — check-metadata IS SOUND, AND THE SUITE PROVING IT CAUGHT ITSELF FIRST

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — check-metadata IS SOUND, AND THE SUITE PROVING IT CAUGHT ITSELF FIRST

**A NEGATIVE RESULT.** `tools/check-metadata.py` read end to end — 310 lines, never opened before,
and **nothing is wrong with it**. No register row. It is the **third** of the seven unread CI
gates, and one of the four AGENTS.md tells you to run before claiming anything.

**Four things checked by measuring rather than by reading, and all four hold:**

| | |
|---|---|
| `CURRENT_STEP = 6` **is not stale** — the thing its own comment warns about, after it was left at 2 while steps 3 to 5 were finished | every step number in the registry is **6 or less**, and steps one to six are each fully implemented or delegated |
| no registry row is silently skipped for having too few columns | measured — **zero** |
| the source roots are complete | **no** `.cs`, `.py` or `.ps1` outside them carries a Heron header |
| `platform/heron-products.json` is a **third** place a version is stated, and `check_version_agreement` compares only two | not unchecked — **`check-products.py` compares it against `Directory.Build.props` itself.** One gate owns one question, and a row here would have been a finding made by grepping instead of tracing |

### The parts worth knowing

**Fragments are deliberately not scanned.** A `Heron-Status` in the `.cs` beside `status:` in the
`.yaml` is this repository's most-repeated failure with a new face on it — **one place per fact**.

**`HOST_PROVIDED` is nine agents the host performs** under [D-01](../DECISIONS.md) and
[D-80](../DECISIONS.md), **listed rather than deleted** so the audit stays honest in both directions:
an agent with no file is either delegated on purpose or work still to do, and silence would make
those two look alike. A delegation naming an id **not** in the registry is itself a problem,
because an exemption covering nothing is the shape a rename leaves behind.

The unimplemented list is printed and is **not** an error — it is the to-do list for finishing a
step. And `check_phase_counts` fails when the claim **disappears** as well as when it disagrees,
which is the rarer half and the one that turns a check into a check that passes for the wrong
reason.

### The suite caught itself first

**`tests/test_check_metadata.py`** was shown to have teeth by breaking the tool four ways:

| what was broken | red |
|---|---|
| `Heron-Since` dropped from `FIELDS` | **2** |
| the missing-Phase-claim branch removed | **1** |
| fragments scanned like Heron's own source | **1** |
| a version disagreement no longer reported | **1** |

**The first of those found a weakness in the suite rather than in the tool.** The field loop read
`tool.FIELDS`, so dropping a field from that list made the suite **stop asking about it** — it was
testing the tool against its own opinion. It names the five docs/29 fields itself now, and asserts
the tool asks for exactly those. That is why a suite is proved by breaking the thing it guards and
not by watching it pass.

**One thing recorded rather than fixed**: `REGISTRY` and `SOURCE_ROOTS` are **relative** paths,
unlike every other tool's `__file__` root — but `main()` refuses with *"FAIL could not read"* and
exit 1 when the registry is not there, so running from the wrong directory fails loudly rather
than auditing nothing.

**Four CI gates remain unopened**: `check-fragments-compile`, `check-products`, `check-intrusion`,
`check-signatures`.
