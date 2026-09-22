# Session note — SEVEN CI GATES HAVE NEVER BEEN OPENED, AND THE FIRST COULD BE SATISFIED BY A COMMENT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — SEVEN CI GATES HAVE NEVER BEEN OPENED, AND THE FIRST COULD BE SATISFIED BY A COMMENT

**[Row 5b-128](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-narrow-errors.py` read end to end —
130 lines, never opened before, and **one of the ten gates a pull request has to pass**.

**The next target was measured, not chosen.** All seventeen brain modules a conversation can
actually reach are now read end to end, so the sweep moved to `tools/`. Of the **12 tools CI runs
directly, 7 had never been opened**:

| |
|---|
| `check-fragments-compile` 400 · `check-products` 527 · `check-metadata` 310 |
| `check-licence` 288 · `check-intrusion` 213 · `check-signatures` 155 · `check-narrow-errors` 130 |

These decide whether a change is allowed into the repository at all. `check-narrow-errors` was
taken first because [row 5b-110](../FRAGMENT-ISSUES.md) is OPEN on whether it should be widened.

### A gate a comment could satisfy

It looks for D-52's commonest shape — an `except sqlite3.OperationalError` that swallows a locked,
malformed or out-of-date store and reports it as an empty one. It **reads text rather than an AST
on purpose**, and the reason is good: the rule is about what a maintainer sees beside the handler.

**But the narrowing test searched the handler's raw text for the word `raise`.** Measured, on
modules written for the purpose:

| the handler swallowed, and carried | |
|---|---|
| `# deliberately do not raise here` | **passed the gate** |
| `log("nothing to raise")` | **passed the gate** |
| `note = "we could raise"` | **passed the gate** |

A handler that says in words it will not re-raise, and then does not, is the clearest possible case
of the thing this gate exists to catch, and it was the one shape it could not see.

A `_code()` takes comment text and string contents out of each line before the search now, tracking
quotes so a `#` inside a string does not cut the line short and skipping an escaped character so a
quote inside a string does not end it early. **The gate is still green on the repository**, so the
fix flags no correct code.

**Also fixed, smaller**: the comment above `WINDOW` said **five** while the value said **twelve** —
a typed number gone stale, in the file whose whole argument is that a shape is something a command
should look for.

**`tests/test_narrow_errors.py`**: **3 red** against the module as found, all three about comments,
with the checks either side green before and after — the bare swallow still reported, both
narrowing forms still accepted, a `raise` in the *next* handler still not rescuing this one, and a
`ValueError` handler still left alone.

### One thing measured and deliberately not fixed

A handler written as a **tuple** — `except (AttributeError, sqlite3.OperationalError):` — is
invisible to the pattern. There is exactly one in the repository, `heron_embed._try_vec_extension`,
and it is **correct**: both types there mean the sqlite extension is unavailable, it answers a
boolean rather than a store's contents, and the fallback gives the same answers. **Widening today
would flag right code**, which is the same shape as the question [row 5b-110](../FRAGMENT-ISSUES.md)
already asks. Recorded in the docstring and added to that row rather than settled here.

**Six CI gates remain unopened.** By size and by what a defect in each would cost, the next is
`check-metadata` or `check-licence` — the first enforces the five-field header on every source
file, the second is the one that keeps a redistributed Revit assembly out.
