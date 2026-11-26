# Needs checking — Group QD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QD - REPORT_WALL_ATTACHMENTS: which walls are attached to a roof, read and never changed - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1330](https://github.com/Ajmalpshaik/Heron-AI/issues/1330) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Tracked as GitHub issue - number written here once it is opened.** The checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-1924](../fragment-issues/row-5b-1924.md), issue [#1127](https://github.com/Ajmalpshaik/Heron-AI/issues/1127).
*"which walls are attached to the roof"* reached `SHAPE_WALL_TOPS_TO_ROOF` or `ATTACH_WALLS` - two writes - because no
read said which walls are attached. [`REPORT_WALL_ATTACHMENTS`](../../brain/fragments/report-wall-attachments/fragment.yaml)
(FRG-ELE-180, READ, version 1) is that read. From Revit 2025 it names the element each wall end is attached to
(`Wall.GetAttachmentIds`); on 2020 to 2024 Revit says only THAT an end is attached (the read-only *Top is Attached* /
*Base is Attached* values), so those walls are listed apart and the answer says the roof cannot be confirmed there.
A wall that could not be read is named and never counted as not attached (D-52).

**Not yet run anywhere.** Compiles for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, cloud container,
2026-11-24). Not tested in Revit.

**The model: a scratch project, never a working model** - `qd-attach`, the default architectural template (metric),
saved under `%TEMP%\heron-work\qd`. Arranged by hand, as [`tools/jobs/wall-attachments-qd-2026-11-24.yaml`](../../tools/jobs/wall-attachments-qd-2026-11-24.yaml)
says: walls A to D in a 10000 x 8000 rectangle on Level 1 (Unconnected 4000), a roof by footprint R over them at
Level 2, a floor F at Level 2 over wall D alone; A and B Attach Top to R, D Attach Top to F, C free; a fifth wall E away
from the rest, free. Write down every wall's *Top is Attached* and *Base is Attached* from Properties before the run.
Made once in Revit 2026 and once in Revit 2024 (the same steps).

| ID | Do this | Pass looks like |
|---|---|---|
| **QD1** | Make the arrangement above and save it, on Revit 2026 | Properties: A, B, D *Top is Attached* ticked; C, E not; every *Base is Attached* off |
| **QD2** | *Positive, Revit 2026.* `python tools/batch-prove.py tools/jobs/wall-attachments-qd-2026-11-24.yaml --session <2026 pid>` - wall A alone | `attachedToRoof` holds A; the report *"Walls <A> ...: top attached to Roofs <R> ..."*; nothing changed (no new entry in the undo list) |
| **QD3** | *Negative, Revit 2026.* The same run's negative - wall E alone | `attachedToRoof` empty, `wallsRead` 1, `unread` empty; *"not attached to anything at its top or its base"*. Not called attached |
| **QD4** | *Revit 2026, by hand through `revit_read`:* the roof R alone, then wall D and R together | R alone: A and B in `attachedToRoof`, D counted as *attached to something else*. D and R: `attachedToRoof` empty, *"not attached to the roof selected - attached to Floors <F>"*, and *"Roofs <R> ...: no wall read is attached to it"* |
| **QD5** | *Revit 2024,* the 2024 copy, R selected | `attachedToRoof` EMPTY; `attachedTargetUnread` A, B and D; the answer opens *"... BEFORE REVIT 2025 REVIT DOES NOT SAY WHAT A WALL IS ATTACHED TO"*, and says R could not be checked against. Wall C alone: *"not attached to anything"* |
| **QD6** | *Routing, on the PC (`model`, re-pointed 2026-11-26: this row asked for `model+rerank`, and [D-201](../decisions/D-201.md) removed the re-ranker):* `heron_lookup` *"which walls are attached to the roof"* on 2024 and on 2026 | `REPORT_WALL_ATTACHMENTS` first; *"attach these walls to the roof"* still `ATTACH_WALLS` on 2026 and `SHAPE_WALL_TOPS_TO_ROOF` on 2024 |

**Closing:** once QD2 and QD3 pass and Ajmal PS signs the proof, the card's `proof:` block is written and it leaves
DRAFT; row 5b-1924's walls-to-roof part names the release, model and date; each id here struck at both ends.
