# Session note — heron_retrieve: A NEGATIVE RESULT, AND A FINDING NOT WRITTEN

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-21 — heron_retrieve: A NEGATIVE RESULT, AND A FINDING NOT WRITTEN

**Started `brain/heron_retrieve.py` (1,329 lines, never read) and it is the first live-path module
this sweep has opened where the answer is "nothing to fix here yet".** Recorded rather than left
silent, because the next session should not re-derive it.

**Every major claim in it has a named test.** The version wall in `tests/test_retrieve.py` §2 and §7;
`Contest` has a suite of its own, `tests/test_contest.py`; `find_documents` is exercised by
`test_document_retrieval.py` and four others; fusion, the quality nudge, weak-match labelling and
retired-not-offered are §3 to §6. **This is the module that argues hardest and is held best**, which
is worth knowing after three rows in a row of the opposite.

**A FINDING CHECKED AND DELIBERATELY NOT WRITTEN.** The version wall compares release strings
**exactly**: `eligible()` splits `row["revit"]` on commas and asks `str(revit) not in supported`, and
`heron_fragment.supported` does **not** strip. So a declaration carrying a stray space would make a
fragment silently unavailable on the one release it supports — **silent over-refusal on the rule this
module calls non-negotiable.**

**It is not a defect today, and the measurements are why.** All **395** fragments were checked: every
`revit:` entry is a quoted string with **no inner whitespace**, and the stored column reads
`'2020,2021,...'`. The caller's side is machine-supplied too — `_revit_version()` takes the release
from `binding.sessions()`, a connected Revit reporting itself, not text anybody types.

**So both sides are generated, and a `.strip()` would be a provable no-op.** Adding it with a row
saying "fixed" would be [row 5b-95](../FRAGMENT-ISSUES.md)'s failure in miniature — a finding
manufactured out of a shape rather than found in behaviour. **Written here instead**, so if a release
list ever starts being edited by hand, this is the first place to look.

> **Two near misses in two modules today** — `screen()`'s 80-character cap and this — both looked like
> violations of a rule stated in capitals, and both were sound on inspection. **A rule stated loudly
> attracts false positives**, because the reader goes looking for it. Measure the behaviour before
> writing the row.
