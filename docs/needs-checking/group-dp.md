# Needs checking — Group DP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DP - modelling: walls attached, faces painted, mass floors, wall sweeps, fascias, gutters and slab edges, reference plane lengths, radial grids (2026-10-12)

The sixth batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)).

**What was built - seven new capabilities.** `ATTACH_WALLS` (FRG-ELE-081, Revit 2025 on - the release that
opened wall attachment to code). `PAINT_FACES` (082): all, vertical, top, bottom, a wall's sides, or its
reveals. `CREATE_MASS_FLOORS` (083). `PLACE_WALL_SWEEPS` (084). `CREATE_EDGE_SWEEPS` (085): fascia and gutter
on roof eaves, slab edge round a floor, the edges taken from the host. `SET_REFERENCE_PLANE_LENGTH` (086).
`CREATE_RADIAL_GRIDS` (FRG-GEO-063) - `CREATE_GRIDS` is PROVEN and straight only.

**Not built, and recorded in the plan:** a floor boundary replaced in place (needs sketch-edit mode, which
cannot start inside an open change); a wall's location line and slanted walls are parameters
`WRITE_ELEMENT_PARAMETERS` already writes by Revit's own words, owed its proof on them.

**What the code assumes and only Revit can say:** that the reveal faces are found as the code reasons (upright
faces square to the wall that are not its ends, and level faces that are not its top or bottom); that an eave
is the lowest level edge of a roof's upward face; that a wall sweep's `Distance` is from the wall's base.

**Measured without Revit, 2026-10-12.** Every Revit API member was read off `RevitAPI.xml` in the Nice3point
reference packages for all eight releases. Each fragment compiles on every release it declares
(`tools/check-fragments-compile.py`, all eight run), passes the author gate, and the job file reads `7 WOULD RUN`
under `batch-prove --dry-run`. **Nothing below has run in Revit.**

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DP1** | Arrange on **Revit 2025**: the scratch project the header of [`gap-fill-modelling-2026-10-12.yaml`](../../tools/jobs/gap-fill-modelling-2026-10-12.yaml) describes | the Level 1 room under the hip roof, the two Level 2 walls, the floor, the mass, the three reference planes |
| **DP2** | `python tools/batch-prove.py tools/jobs/gap-fill-modelling-2026-10-12.yaml`, the title filled | each positive answers and each negative is empty or refused as the job's notes say. Look at the model afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DP3** | paint-faces reveals by hand, kept, in a Realistic 3D view | the window's jambs, head and sill and the door's jambs and head painted; the wall faces and ends not |
| **DP4** | create-edge-sweeps fascia on the hip roof, kept | a fascia along the four eaves, joined at the corners |
| **DP5** | place-wall-sweeps skirting, kept | the skirting runs each wall and stops at the door |
| **DP6** | `WRITE_ELEMENT_PARAMETERS` on a wall: Location Line = Core Centerline, then Cross-Section = Slanted with Angle From Vertical 5 | both read back in Properties - the two jobs the plan says need no new card |
| **DP7** | The rest on **Revit 2020** and **2027** (attach-walls on 2027 only) | the same answers. Compiled on every declared release; what Revit DOES was measured on none |
| **DP8** | The signatures, on the owner's word only, after the rows above | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each of the seven |
