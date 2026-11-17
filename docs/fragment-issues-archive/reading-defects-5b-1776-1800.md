# FRAGMENT-ISSUES archive — section 5b, rows 1776 to 1800

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
> Rows 1776 to 1800 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1780

*Moved from the register on 2026-11-17.*

**Defect.** **`ROTATE_ELEMENTS` TESTS A WHOLE TURN TO 1e-9 RADIANS, SO 2 PI TYPED TO SEVEN PLACES - 6.2831853 - IS NOT REFUSED: REVIT IS ASKED TO TURN IT A WHOLE TURN LESS 7e-9, AND IT READS EITHER AS `rotated` OR AS `blocked` ("A GROUP MEMBER, OR HELD BY A CONSTRAINT"), BOTH WRONG.** Found 2026-11-16 in a cloud session reviewing the Group OH change for row [5b-1708](../fragment-issues/row-5b-1708.md), before anything ran: the job file's negative first typed 6.2831853. **Confirmed in code** (v3 and v4 alike, `impl/any/fragment.cs`, the whole-turn test, now `isNoTurn`): `Math.Abs(left - whole) < 1e-9`, and \|6.2831853 - 2 pi\| = 7.18e-9 (measured in Python). Which of the two readings Revit then gives depends on how `LocationPoint.Rotation` and the bounding box round at six places - **NEEDS REAL REVIT**. Not repaired here: widening the tolerance changes which turns the card refuses, and the change for row 5b-1708 was kept to its words. Group OH's OH7 types the whole turn in full (6.283185307179586) so its negative means what it says.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The fix: test a whole turn, or none, to a tolerance a typed angle can meet - a thousandth of a degree, as `PLACE_FAMILY_INSTANCES` v6's `rotationDegrees` already does in degrees - and say in the refusal what the angle came to. C# of a DRAFT card (v4): the owner's choice whether it rides with Group OH or after it.

---

### Row 5b-1800

*Moved from the register on 2026-11-17.*

**Defect.** **`MOVE_ELEMENTS` WITH `toPoint` PUTS A TAG IN `noLocationPoint` AND SAYS IT IS *"positioned by a curve rather than a point"* - A TAG HAS NO LOCATION AT ALL, AND ITS POINT IS ITS HEAD.** Found 2026-11-16 reading the card while repairing [row 5b-1733](../fragment-issues/row-5b-1733.md) - in code, not in Revit. `brain/fragments/move-elements/impl/any/fragment.cs` (version 3, DRAFT) keeps an element for `toPoint` only when its `Location` is a `LocationPoint`, and counts everything else in `noLocationPoint`; the finding then calls all of them *"positioned by a curve"*. Version 3's `probe` reads a tag by its `TagHeadPosition` for the offset move only. **The card for a tag put AT a point is `MOVE_ANNOTATION`** (PROVEN), which moves a tag by its head and reads it back.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Left out of the 5b-1733 repair on purpose: that row asked for the read-back of a move, and widening `toPoint` widens a reviewable change. The fix: in the `toPoint` branch, name a tag as *"a tag - MOVE_ANNOTATION puts a tag at a point"* rather than as a curve, or put it at the point by its head as `MOVE_ANNOTATION` does. Either is C# on a DRAFT card (version 3, Group OC); the owner's call which.

---
