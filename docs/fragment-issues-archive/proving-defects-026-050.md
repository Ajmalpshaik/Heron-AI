# FRAGMENT-ISSUES archive — section 5, rows 26 to 50

> **Finished rows, moved out of the live register.** These are rows from section 5 of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by proving — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 26 to 50 of section 5 belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 36

*Moved from the register on 2026-11-17.*

**Defect.** **`create-sheet-list`'s DUPLICATE-NAME REFUSAL CANNOT FIRE, because Revit lets a schedule take a floor plan's name.** `fragment.cs:31` sets `schedule.Name` inside a `try` and the `catch` reports *"'{0}' is already the name of another view - the sheet list was created as '{1}' instead"*. Run 2026-09-13 on `test projject` with `scheduleName = "1 - Mech"`, the name of an existing floor plan: **`refused 0`**, `sheetList ViewSchedule`, `fieldsAdded 2`. No exception was raised, so the sentence never ran.

**State.** **MOVED TO ISSUE [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN as an observation, and the fragment is not obviously wrong.** A `ViewSchedule` and a `ViewPlan` may genuinely be allowed to share a name in Revit, in which case the `catch` is dead code guarding against something that cannot happen, and the honest fix is to delete it rather than leave a refusal nobody can trigger. **The experiment that settles it is one applied run and a look in the Project Browser** - whether two views called `1 - Mech` now appear. Not run here, because it cannot be undone by a rollback. This is [row 24](../FRAGMENT-ISSUES.md#)'s rule again: *Revit allows more than it looks like it should*, and a refusal path built on an assumed refusal is untestable. The proof that stands uses the OTHER declared refusal - a column a sheet list cannot carry: `fieldsAdded 0` with all four bad names reported

---

### Row 41

*Moved from the register on 2026-11-17.*

**Defect.** **ONE SHAPE, EIGHT FRAGMENTS: A `role: result` THAT MEASURES REACH, STATE OR INPUT RATHER THAN WORK DONE.** Rows 26, 32, 35, 39 and 40 are all this, found separately over one day, and three more turned up after them. **The test that separates them is a single question: COULD THIS NUMBER HAVE BEEN ZERO IF THE FRAGMENT HAD DONE NOTHING?** Where the answer is no, D-30's second leg is unreachable and the gate correctly reports *"the negative case returned content instead of nothing"*. **Four were corrected and are now `accounting`**: `create-flex-duct.lengthMm` (row 35 - it reported the length of a route it refused to build), `test-view-filter-match.outOfScope` (elements ruled out of scope, never found), `replace-material.typesTouched` (types the swap REACHED - 1 even when nothing was replaced), and `set-view-phase` was a different fault entirely (row 38). **Four are still open**: `create-mep-system-type.created` returns the object (row 26); `place-room-at-point.created` and `place-rooms.created` can never be empty because a point always makes a room (row 32); `set-view-template-control.nowHeld` is a deliberate read-back of the template's whole state (row 39); `describe-blank-parameters` declares NO `role: result` at all, only `findings`, and separately needs `blank` AND `absent` both non-empty in one run, which no arrangement in this model produces.

**State.** **MOVED TO ISSUE [#908](https://github.com/Ajmalpshaik/Heron-AI/issues/908) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - BUT TWO OF THE FOUR IT NAMES ARE CLOSED AND THE ONE RULE NEVER HAPPENED - corrected 2026-09-19.** This cell asked for *one rule, not eight patches*, and the library answered it case by case instead: [row 26](../FRAGMENT-ISSUES.md#)'s `create-mep-system-type.created` was closed by a JUDGE rule (`_result_appeared`) with the contract left alone; [row 39](../FRAGMENT-ISSUES.md#)'s `set-view-template-control.nowHeld` was closed by [row 53](../FRAGMENT-ISSUES.md#)'s tracking, which argued the read-back IS the fragment's answer and must stay `result`; and [row 89](../FRAGMENT-ISSUES.md#) settled `set-sheet-title-block` by reading the fragment's own declared case. **A blanket demotion would have been WRONG for at least two of them**, which is the finding. **TWO SURVIVE**: `place-room-at-point.created` and `place-rooms.created` ([row 32](../FRAGMENT-ISSUES.md#)), and `describe-blank-parameters`, which declares no `role: result` at all and is one of a PAIR with `report-findings` - two fragments whose whole output is language, which no rule that looks for a number can judge ([row 81](../FRAGMENT-ISSUES.md#)). **THE TEST BELOW IS RIGHT AND ITS LIST IS NOT - read it with that correction or it undoes the two resolutions above.** The question *could this number have been zero if the fragment had done nothing* still separates them correctly. What the original went on to assert - that **state after** is therefore always `accounting` - is the part [row 53](../FRAGMENT-ISSUES.md#) disproved: where the state IS the fragment's answer, it stays `role: result` and is proved by TRACKING instead, because an empty leg is also what doing nothing produces. Demote an echo of the input, a count of what was excluded, or a measure of reach; do not demote a state a fragment exists to report. **The original wording, kept for the test it names:** the rule that fits every case already exists in [D-52](../DECISIONS.md)'s words for accounting: *"Never a thing it FOUND"*. Applied to results it reads: **a `role: result` is a measurement of what this call DID, and anything else - reach, state after, an echo of the input, a count of what was excluded - is `accounting` however useful it is to a reader.** Nothing is lost by the demotion: every one of the four corrected fields is still reported and still in the run record. What changes is only whether the machine reads it as evidence that something was found
