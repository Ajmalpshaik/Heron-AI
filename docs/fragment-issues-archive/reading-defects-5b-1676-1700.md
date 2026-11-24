# FRAGMENT-ISSUES archive — section 5b, rows 1676 to 1700

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
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
> Rows 1676 to 1700 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1700

*Moved from the register on 2026-11-24.*

**Defect.** **THE DELETE BOX NAMED NOTHING IT WOULD DELETE - (no category) AND A COUNT - AND WHEN NOBODY ANSWERED IT STAYED OPEN PAST ITS DEADLINE, SO REVIT REFUSED EVERY LATER REQUEST AS BUSY UNTIL A PERSON CAME BACK.** Reported 2026-11-11 by Sruthina in Ajmalpshaik/Heron-AI#853 (item C1; E5 of its second comment, another session's, reports the same box), one house build on Revit 2024, model `villa 2`: the box came up on `CHANGE_ELEMENT_TYPE` (windows, doors, columns), `REPLACE_MATERIAL` (window frames, 16 elements each time), `SET_PARAMETER_VALUES_BY_ID` (a column's Material), `CREATE_WALL` (1 element) and `WRITE_ELEMENT_PARAMETERS` (10 floors' Height Offset From Level), and none named what would go. **Confirmed in code:** the box is Heron's own, D-142's (`revit/Heron.Revit.Addin/RevitFragmentDelete.cs`), asked of every kept run whose `DeletedIds` - Revit's own `DocumentChanged` report - is not empty, whatever the card. Each element was listed under its category's name, and one with none under `"(no category)"` alone (`HeronDeleteQuestion.NoCategory`). The box is a `TaskDialog` whose `Show()` holds Revit's thread until it is answered; past `RevitDispatcher.TimeToAnswer` a Delete is not kept, but nothing closed the box, so the dispatcher's ExternalEvent could not run and every later request answered `revit_busy`. **Only reported:** what those elements are. A type change, a material swap and a parameter write deleting elements in their own right is unlikely; Revit deleting and remaking elements of its own - a sketch, an opening's cut - is the likeliest reading, and not measured.

**State.** **MOVED TO ISSUE [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) 2026-11-19** - still open, tracked there: the owner's D-142 question at the end of this cell. Was: **RUN IN REVIT 2024, 2026-11-16 ([Group NY](../needs-checking/group-ny.md), scratch `ny-scratch`): (1) PASS, NY1; (2) PASS on NY2's re-run**, after its first run found the closer missing the title Revit gives the box, *"Heron AI - Delete 8 elements?"* ([row 5b-1748](../fragment-issues/row-5b-1748.md), merged in #884, `ecf05b14`): the box closed by itself at its deadline, the answer was `timed_out`, nothing was deleted, and the next request ran at once - so Revit's task dialog does take `WM_CLOSE`. Revit 2020, 2026 and 2027 not run. **Before that, 2026-11-15: fixed in the add-in, built, not yet run in Revit.** (1) An element with no category is listed as `(no category)` and Revit's own name for its kind, `(no category) SketchPlane` (`ListedAs`, `HeronDeleteQuestion.WithoutCategory`). (2) At its deadline the box closes itself (`BoxCloser`): a thread-pool timer finds the box among the windows of the thread that showed it, by its class and exact title (`HeronDeleteQuestion.IsTheBox`), and posts it `WM_CLOSE`, which the box takes as Esc. The answer is `timed_out`, the refusal `delete_too_late`, nothing deleted; the box says it will close and when. If Revit's task dialog does not take `WM_CLOSE`, the box stays open as before. `tests/test_keep_question.py` section 16 and `tests/test_delete_box.py` section 9 hold it, and fail on the code before the change (2 and 6 checks). **Still open, the owner's (D-142):** whether elements Revit deletes and remakes in the same run should be asked about at all - NY1's list says what they are; leaving them out is a change to D-142's "anything deleting from the model".

---
