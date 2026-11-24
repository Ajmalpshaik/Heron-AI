# Needs checking — Group QB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QB - four facade tools: a curtain grid line or segment removed, a grid line or mullion moved, a mullion's join, a panel's flatness - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1320](https://github.com/Ajmalpshaik/Heron-AI/issues/1320) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** WN-11 in GitHub issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) - the facade note's
*Still NOT YET* list ([`facade-capability.md`](../work-notes/plans/facade-capability.md)): no tool removed a grid
line or cut a segment, moved a mullion or changed its join, read or set the curtain type's *Automatically Embed*,
or measured whether a panel is flat enough for one sheet of glass. Four new cards, written in a cloud session.
The fifth, `set-curtain-wall-embed` for *Automatically Embed*, was taken out of this change on 2026-11-24: a plain
question about the box ranked it, a write, first, and no read card reads the box - [row 5b-2008](../fragment-issues/row-5b-2008.md),
issue [#1012](https://github.com/Ajmalpshaik/Heron-AI/issues/1012), the owner's call. The owner chose to widen the read
on 2026-11-24, and the card landed with `REPORT_CURTAIN_WALL_TYPE` version 2; its Revit run is
[Group QG](group-qg.md).

| Card | What it does in Revit words |
|---|---|
| [`edit-curtain-grid-segments`](../../brain/fragments/edit-curtain-grid-segments/fragment.yaml) (FRG-ELE-173) | a grid line taken away from one curtain wall, or segments of it taken out or put back - Add/Remove Segments |
| [`move-curtain-grid-line`](../../brain/fragments/move-curtain-grid-line/fragment.yaml) (FRG-ELE-174) | a grid line moved along its wall - or the one a picked mullion sits on - by a distance or to a position |
| [`set-mullion-join`](../../brain/fragments/set-mullion-join/fragment.yaml) (FRG-ELE-175) | Make Continuous or Break at Join on the mullions picked, measured off each mullion's solid |
| [`check-panel-flatness`](../../brain/fragments/check-panel-flatness/fragment.yaml) (FRG-QA-039) | how far each adaptive panel's corners sit off one plane, in mm, against the modeller's limit - read only |

The tile pattern of a divided surface stays NOT YET: Revit's `CurtainPanelTilePattern` is read-only.

**What is done and what is not.** Every Revit member the four call was read off all eight releases' reference
assemblies with `tools/api-surface` on 2026-11-24, one signature each, and all four compile for 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-24, in a cloud container). Their sentences were measured main against
the branch on all four routing backends (the pull request has the numbers). **Not tested in Revit.** `revit_change`
refuses each until its row below is signed.

**What only a Revit can settle**, each written on its card as an ASSUMPTION:

- whether the grid lines a curtain wall TYPE lays out read **pinned** through the API (both grid cards refuse a pinned line);
- whether `ElementTransformUtils.MoveElement` moves an unpinned grid line **with its mullions**;
- whether a mullion's length along its run, off its solid, tells **continuous from broken**;
- whether a **pattern-based** panel's corners come back as adaptive placement points.

**The model: a scratch copy, never the working model** - "Heron QB Facade", a new project saved for this, with
"Level 1", "Curtain Wall: Curtain Wall" (both grid layouts None - read with `REPORT_CURTAIN_WALL_TYPE`), "Basic
Wall: Generic - 200mm" and one rectangular mullion type. Group JJ's rig copied will do. Revit 2020 first, then 2024.
Each card's `tests/cases.yaml` has every case in full.

| ID | Do this | Pass looks like |
|---|---|---|
| **QB0** | Make the rig above and save it | The three types are there; both layouts None |
| **QB1** | `batch-prove.py tools/jobs/edit-curtain-grid-segments.yaml` - the positive (a line taken away) and its negative (a basic wall) | Positive `changed 1`, *"vertical lines 5 before, 4 now"*; negative `changed 0`, *"Nothing given is a curtain wall"* |
| **QB2** | `edit-curtain-grid-segments` on a kept copy with a transom at 1100: segmentsAt `500` remove, then restore | The bottom segment at 3000 out, then back; two panels become one, then two |
| **QB3** | `edit-curtain-grid-segments` on a wall whose lines come from its type (Fixed Distance 1500) | Refused *"is pinned"* - or deleted, which is the ASSUMPTION failing: recorded, and the card changed before it is proved |
| **QB4** | The case file's other negatives - a position with no line, a point on a joint, every segment named | Each refused before anything is written |
| **QB5** | `batch-prove.py tools/jobs/move-curtain-grid-line.yaml` - the positive (3000 to 3300) and its negative | Positive `moved 1`, *"from 3000 to 3300 mm"*; negative `moved 0` |
| **QB6** | `move-curtain-grid-line` with one interior mullion picked, move `-200` | *"from 4500 to 4300 mm, with its 1 mullion"*; panels 1300 and 1700 wide - or the call stops *"has 0 on it now"*: the ASSUMPTION failing, recorded |
| **QB7** | `move-curtain-grid-line`: a move past the next line, `0.3`, a type's pinned line, a border mullion | Each refused, nothing moved |
| **QB8** | `set-mullion-join` continuous on a mullion that stops at a transom, then break | `changed 1` each time, the length longer by about the transom's width, then back within 1 mm |
| **QB9** | `set-mullion-join` with the curtain wall picked, and continuous twice | Refused *"Nothing given is a mullion"*; the second `changed 0`, in `unchanged` |
| **QB12** | `check-panel-flatness` on a flat and a twisted 4-point adaptive panel, limit `10` | The twisted one *24.98 mm - OVER 10 mm*, the flat one 0 - within 10; the flat one alone is never flagged |
| **QB13** | `check-panel-flatness` on pattern-based panels of a divided surface, and on curtain wall panels | RECORD whether pattern-based panels are measured; curtain wall panels all in `notMeasured` |

QB10 and QB11 were the embed card's two rows; they left with it on 2026-11-24 (row 5b-2008) and are owed by
nothing in this group - they are QG5 and QG6 of [Group QG](group-qg.md).

**Closing:** each card goes to PROVEN only with its positive, its negative and a person's name (D-30); its rows
here are struck with the release, model and date.
