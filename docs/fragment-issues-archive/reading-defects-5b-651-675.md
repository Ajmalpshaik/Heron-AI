# FRAGMENT-ISSUES archive — section 5b, rows 651 to 675

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
> Rows 651 to 675 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-665

*Moved from the register on 2026-11-17.*

**Defect.** **TWO COMMENTS STILL SAY NOBODY HAS PUT A TAPE MEASURE ON THE MOVE, AND BOTH WERE LEFT THAT WAY ON PURPOSE ON THE DAY THE TAPE MEASURE WAS TAKEN.** Found 2026-10-17 closing D3 ([Group D](../needs-checking/group-d.md)): the banner of [`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs) is headed *"RUN, BUT NOT MEASURED"* and says *"D3 ... is not struck through"*. D3 was measured that day in Revit 2024 - three ducts +200.0000 mm, the pinned control unmoved, one undo - and every other copy of the sentence was corrected in the same change: README, docs/README, PROJECT-MAP, docs/27, a test's docstring and the health warning. **Two were not.** `RevitWrite.cs` is the file the golden library fingerprinted for G-09, G-10, G-11 and G-17 that same day: a comment edit in it reports all four STALE, while the bytes Revit ran are exactly the ones committed. And [`HeronPermissions.cs`](../../platform/Heron.Core/HeronPermissions.cs) - *"WHAT IS STILL NOT PROVEN IS THE DISTANCE ... Nobody has put a tape on what it did"* - is the permission file: `check-change` flags any edit to it as a trust change for a person to review (Articles 13 and 18), and it should change once, with the owner's answer to D-19, not twice.

**State.** **MOVED TO ISSUE [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Correct `RevitWrite.cs`'s banner in the next change that has to touch it anyway, and re-take G-09 to G-11 then; correct `HeronPermissions.cs` in the change that records the owner's `write.enabled` decision. Until then both comments are behind the register, and the register is right.

---

### Row 5b-666

*Moved from the register on 2026-11-17.*

**Defect.** ***"MOVE THE DUCTS UP 200 MM"* MOVES EVERY UNPINNED DUCT IN THE WHOLE MODEL, WHATEVER IS SELECTED, AND NOTHING THE CHAT IS TOLD SAYS SO.** Measured 2026-10-17 arranging D3: on a copy of Autodesk's MEP sample with 724 ducts, `revit_preview_move` answered *"This would move 724 ducts up 200 mm"*. [`RevitWrite.Partition`](../../revit/Heron.Revit.Addin/RevitWrite.cs) collects `new FilteredElementCollector(doc).OfCategory(...)` - the document, not the selection and not the view - and leaves out only pinned, owned, changed-in-central and similar elements. The tool's description calls the category *"the BIM word the user said"* and never says a selection is ignored. A modeller with three ducts selected who says *"move these up 200 mm"* is shown the whole model's count, and that count is the only thing between the sentence and every duct in the building. The rails held - the preview counted honestly, and the re-count before the write compares the set of ids, not a number - so nothing wrong moved; D3's negative case had to be arranged by pinning 725 ducts by hand.

**State.** **MOVED TO ISSUE [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - a question for the owner, not a fix to make quietly.** Whether the move should act on the selection when there is one, and how its preview must say which it took, belongs with the `write.enabled` decision ([D-19](../DECISIONS.md)). Not changed here: it changes what the write path does.

---
