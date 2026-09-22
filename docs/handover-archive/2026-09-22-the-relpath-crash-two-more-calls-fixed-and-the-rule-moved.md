# Session note — THE RELPATH CRASH: TWO MORE CALLS FIXED, AND THE RULE MOVED WHERE EVERY TOOL CAN IMPORT IT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE RELPATH CRASH: TWO MORE CALLS FIXED, AND THE RULE MOVED WHERE EVERY TOOL CAN IMPORT IT

A Windows session - repository on `D:`, `TEMP` on `C:` - working the brief the session below wrote.
That brief's reproduction was already green when this started: #286 had fixed `tools/api-changes.py`.
As the entry below asks, PR #296 was checked against #286 and #288 before it went for merging.

#### What was done - PR #296

- **Every `os.path.relpath` call in `tools/`, `brain/` and `mcp/` traced to where its path comes from.**
  58 calls: 53 safe and left alone (joined onto `ROOT`, or walked from under it), 3 copies of the one
  helper, and 2 reachable and unguarded - `prove-skill.py --jobs` and `build-release-assets.py --out`,
  both raising AFTER their work was done. [Row 5b-152](../FRAGMENT-ISSUES.md) holds the audit.
- **The rule moved to [`brain/heron_relpath.py`](../../brain/heron_relpath.py)**, which imports nothing but
  `os`. Its old home, `heron_fragment`, needs PyYAML, and three tools must run without it - which is why
  the copies kept appearing. `heron_fragment`, `api-changes.py` and `check-licence.py` now ask it.
- **[`tests/test_relpath.py`](../../tests/test_relpath.py) forces the two-drive condition**, so it fails on
  CI's single mount too - 11 checks red against the unfixed tools, all green with the fix.

#### Checked against #286, #288 and #294

- **#286 is kept, not duplicated.** Its `short()` stays and now asks `heron_relpath`;
  `tests/test_api_digest.py` passes 30 of 30.
- **#288 touched none of these files.** Its two suites were the only reds in the before-sweep, measured on
  both sides, and they pass on `main`.
- **#294 was merged into the branch and tested with it**: its suite, both relpath suites and five gates
  exit 0.

#### What is left

- **One item: a gate.** Nothing refuses a NEW raw `os.path.relpath` outside `heron_relpath`, and a new
  call is how every one of these arrived. It changes CI's gate list and `tests/test_tag.py`, which pins
  that list - its own piece of work.
- **For the owner: nothing**, once #296 is merged.
