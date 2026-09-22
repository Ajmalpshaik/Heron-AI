# Session note — "A KEY NAMED company-standards.keywords, WHICH SAYS THE VALUE IS A CREDENTIAL"

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — "A KEY NAMED company-standards.keywords, WHICH SAYS THE VALUE IS A CREDENTIAL"

**[Row 5b-117](../FRAGMENT-ISSUES.md), FIXED.** `brain/heron_configuration.py` read end to end — 348
lines, 2 public functions, **one suite**, nothing imports it.

Article 17 says a secret never goes in a settings file, so `split()` refuses a value that looks like a
credential **and** a key whose NAME says it is one. The name test was **`word in lowered`** over the
whole dotted path, with nothing anchoring it — so any path carrying those letters **anywhere** was
refused.

**Measured, on settings a practice would really write:**

| the setting | what it gets |
|---|---|
| `company-standards: {keywords: [duct]}` | **`SECRET_IN_CONFIGURATION`** |
| `update-policies: {turnkey: true}` | **`SECRET_IN_CONFIGURATION`** |
| `model-routing: {monkey: no}` | **`SECRET_IN_CONFIGURATION`** |

And the refusal is not merely cautious — **it asserts something untrue about the reader's own file**:

> a key named `'company-standards.keywords[0]'`, **which says the value is a credential whatever it
> looks like**

with a proposal to put the word **duct** in the credential store and keep the handle.

**A refusal on a security boundary is allowed to be cautious. It is not allowed to name evidence it
does not have** — and this file already knows the difference: its `unjudged` list spells out exactly
what the MACHINE check does and does not catch (*"it narrows the leak; it does not close it"*) and
says nothing at all about the secret check's false side.

The name is matched as a **word** now — the path is split on every non-alphanumeric character, and a
segment has to **be** one of the names, or one with a trailing `s`. **`secrets`, `tokens`,
`passwords` and `credentials` are still caught**, which is the half that makes this a narrowing rather
than a hole opened for three examples. `public-key-pinning` **stays refused**, deliberately: *key* is
a word in it, and the rule this agent states is that a key named for a credential is refused whatever
its value looks like.

```bash
python tests/test_configuration.py     # section 5b, 3 red against the module as found
```

**The twelve checks that nothing was loosened were green BEFORE the fix** — every spelling of a
credential name the suite already had, plus the plurals, asserted refused both before and after. That
is what made it safe to make.

**The four true positives it exists for all still fire**, measured in the same run: a pasted `sk-`
key, a key named only by its name, a Windows path in a portable setting, and a key outside docs/21
§9's list.

**One thing checked and dismissed**: the refusal returns on the **first** problem rather than naming
every one, so a caller fixing three settings makes three round trips — `heron_safemode` names all of
them in `could_not`. Here the refusal **is** the product, on a boundary where the first one is enough
to stop the write: a difference of shape rather than a defect.
