# Needs checking — Group DK

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DK - walls, grids, lines and reference planes a fraction of a degree off an axis turned onto it (2026-10-12)

The second batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)).
Revit's "slightly off axis" warning comes on walls, grids, lines, reference planes, room separation, area
boundary and sketch lines. Here it is one job, because it is: a straight line a fraction of a degree off an
axis, turned about one end until it lies on it.

**What was built - one new capability.** `STRAIGHTEN_OFF_AXIS_LINES` (FRG-ELE-079). `ROTATE_ELEMENTS` was
weighed: it turns by a given angle about a vertical axis, every element by the same amount, and this turns
each element by its own error onto the nearest axis, refusing lines that are off on purpose. Folding the
measuring into it would give one card two jobs, and it is PROVEN. `SNAP_TO_GRID` moves points onto a grid's
cells and is not this either.

**Measured without Revit, 2026-10-12.** Every Revit API member used was read off `RevitAPI.xml` for all eight
releases; it compiles on all eight and passes the author gate; the job file reads `1 WOULD RUN` under
`batch-prove --dry-run`. **Nothing below has run in Revit.**

**What the code assumes and only Revit can say:** that turning a wall, grid or reference plane about its
start by a few hundredths of a degree clears the warning rather than moving it to a joined neighbour; that
room separation and area boundary lines, which are model curves, turn the same way; and that a sketch line
is recognised by its category and left alone.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DK1** | Arrange on **Revit 2024**: the scratch project the header of [`gap-fill-off-axis-2026-10-12.yaml`](../../tools/jobs/gap-fill-off-axis-2026-10-12.yaml) describes, saved as **Heron Off Axis Test** | Manage > Warnings shows "slightly off axis" for the two Level 1 walls and nothing for Level 2 |
| **DK2** | `python tools/batch-prove.py tools/jobs/gap-fill-off-axis-2026-10-12.yaml`, PROJECT-TITLE filled | positive: `straightened` 2. negative: `straightened` 0, the 30-degree wall in `tooFarOff`, the east-west wall in `alreadyOnAxis`. The Level 1 warnings still there afterwards - rollback has failed on this PC before (BD1) |
| **DK3** | By hand through `revit_change`, after `SELECT_WITH_WARNINGS` for the off-axis warning, positive kept | Manage > Warnings no longer lists the two walls; each wall's start point unchanged to the millimetre |
| **DK4** | Add a grid, a detail line, a reference plane, a room separation line and an area boundary line, each drawn 0.02 degrees off; run on all five | `straightened` 5 and their warnings gone. Record any that Revit would not turn |
| **DK5** | Join a third wall to the end of a Level 1 wall, then straighten the first | record whether the joined wall's end followed; `report` says the join may carry it |
| **DK6** | A floor whose sketch has one line slightly off; run on the floor's sketch lines | each named in `notStraightened` as a line inside a sketch, nothing changed |
| **DK7** | DK2 on **Revit 2020** and **2027** | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DK8** | The signature, on the owner's word only, after DK2, DK3 and DK4 | `python brain/heron_validate.py accept straighten-off-axis-lines --by "Ajmal PS"` |
