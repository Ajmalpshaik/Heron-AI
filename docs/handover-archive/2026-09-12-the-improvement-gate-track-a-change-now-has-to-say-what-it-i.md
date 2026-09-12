# HANDOVER — 2026-09-12 (the IMPROVEMENT-GATE track): a change now has to say what it is for, and two red suites went green

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Nothing here has been near Revit, and no fragment status moved.** Full ledger, every phase note and
every measurement: [`work-notes/plans/improvement-gate-execution-record.md`](../work-notes/plans/improvement-gate-execution-record.md).

**What exists now that did not before.** Four things, and each answers a question nothing in this
repository was asking:

| | |
|---|---|
| [`tools/check-change.py`](../../tools/check-change.py) | Does this change do only what it said it would? Compares the diff against a declared `intent` / `area` / `risk`, using the **layering table** rather than word overlap — so `brain/` is *supporting* work for an `mcp/` change and `revit/` is not |
| [`tools/change-evidence.py`](../../tools/change-evidence.py) | Is it better than before? The same measurements twice, compared as sets, ruling `KEEP` · `REVERT` · **`NO CHANGE MEASURED`**. It cannot change anything, which is what makes the loop safe to point at a prompt or a fragment description |
| [`tools/check-package.py`](../../tools/check-package.py) | Would the delivered thing install? **Nothing in this repository read `Heron.addin`** — the first file Revit opens. Now a fourth gate that must pass |
| [`mcp/server/heron_runtime.py`](../../mcp/server/heron_runtime.py) | Why can Heron *not* do this? Six verdicts where `heron_capability.resolve()` had one `None` for five different reasons. **Wired to nothing yet, on purpose** — [PROPOSALS F1](../PROPOSALS.md) |

Plus the **Python half of the layering table**, enforced for the first time since Step 1 in
[`check-structure.py`](../../tools/check-structure.py) — its own source had admitted for a fortnight that
two thirds of the repository went unchecked.

**Two suites that were red on every machine are green, and both were the same defect.** `test_graph.py`
and `test_reachable.py` each carried a fixture describing a repository that had moved on: one matched
two exact adjacent lines and `role:` arrived between them, so it broke **none** of the 50 providers it
meant to break; the other excused a function that had acquired a legitimate caller. **Neither test was
edited until it passed** — both claims are unchanged. `.github/workflows/gates.yml` and the
[`heron-ship`](../../.claude/skills/heron-ship/SKILL.md) skill were corrected in the same change, because
that workflow errors when a known failure starts passing and asks for exactly this.

**What is owed.** The gate has only ever been run by the session that built it, which is
[Golden Rule 7](../14-golden-rules.md) unsatisfied. Two new rows in
[NEEDS-CHECKING](../NEEDS-CHECKING.md) — **A12** and **A13** — are the delivery questions no script can
answer: whether the add-in loads, and whether an upgrade and a rollback work. Three defects found on the
way are in [PROPOSALS Part F](../PROPOSALS.md) rather than fixed.

**Next:** use it on the next real task. `python tools/check-change.py --intent "..." --area <part>
--risk low`, then `tools/change-evidence.py capture` before and after.

---
