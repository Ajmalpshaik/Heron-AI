# Session note — A PACKAGE THAT IS BOTH REQUIRED AND OPTIONAL, SETTLED BY TYPING ORDER

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A PACKAGE THAT IS BOTH REQUIRED AND OPTIONAL, SETTLED BY TYPING ORDER

**[Row 5b-121](../FRAGMENT-ISSUES.md), FIXED.** `brain/heron_dependencies.py` read end to end — 359
lines, 3 public functions, two suites. **The part-read queue is now empty**: all six of those brain
modules are read in full.

`review()` builds `entries[name] = dict(entry, kind=kind)`, so the **last** entry with a given name
wins.

**Measured, with nothing importable:**

| the list | the verdict |
|---|---|
| `[required, optional]` for one package | **0 required missing, 1 running degraded** |
| the same two, **reversed** | **1 required missing, 0 degraded** |

Same input, opposite verdicts, decided by which one was typed second — and nothing in the answer says
a choice was made.

**The module argues against exactly this, twice, and refuses it in the lines immediately above.** Its
docstring: *"The two get opposite treatment … so reading the wrong one is not a small error. A
dependency arriving without a kind is **REFUSED rather than defaulted**, in either direction:
defaulting to required turns a normal install into a failure, and **defaulting to optional turns a
failure into silence, which is worse**."*

**It refuses a dependency with NO kind so that nobody picks one for it — and then picked one for a
dependency that arrived with TWO.** Last-write-wins is a default by another name, and the direction it
picks is whichever the list happened to end with.

**And the count disagrees with the input**: the answer reads *"0 of 1 importable"* about a list of
two entries — [rows 5b-111](../FRAGMENT-ISSUES.md) and 5b-120's shape, a third time.

A name already seen with a **different** kind is refused with a new **`KIND_DISAGREES`**. **An exact
repeat is still one dependency, named twice** — the same answer `heron_brain_init` gives about a store
and row 5b-120 gives about an index, because there is nothing to disagree about.

```bash
python tests/test_dependencies.py      # section 3b, 3 red against the module as found
```

**The three checks that an exact repeat is not a conflict were green BEFORE the fix**, which is what
makes this a refusal of a **contradiction** rather than of a duplicate.

**What is right here is what the agent exists for**: what is installed is **asked** through a reader
rather than stated, and with no reader, a non-callable, a raising reader or one answering the wrong
type it refuses `CANNOT_SEE_WHAT_IS_INSTALLED` rather than reporting a healthy system nobody looked
at; an optional dependency that does not say what is **lost** without it is refused, because
PROPOSALS F7 is the gap between that sentence existing in a file and reaching a person; installing is
a **second** decision, per package, and the consent must name the package; and every answer says that
`pip install` is **not** the gate a Heron package goes through — no register, no approver, no hash —
so the quieter route does not look like the safer one.

### Where the sweep goes next

**The part-read queue is empty.** The measurement that picked targets — suites reaching a module
against its public surface — is spent on `brain/`. Re-measure before choosing: **`tools/` is the
strongest candidate**, because those are what decide whether a change is allowed through at all, and
[rows 5b-90](../FRAGMENT-ISSUES.md), 5b-92 and 5b-104 all came out of that folder.

**Still hold the installer-session STALE files** — `CONTRIBUTING.md`, `README.md`, `docs/32`,
`docs/33`, `docs/README.md`, the installer C#, `deploy-addin.ps1`, `test_deploy_script.py`,
`test_installer_window.py` — until **PR #253** merges. Reading them now only makes them stale again.
