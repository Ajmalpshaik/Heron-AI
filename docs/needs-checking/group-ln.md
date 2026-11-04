# Needs checking — Group LN

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LN - a shaft opening cut between two named levels, and a wall's own rectangular opening: `CREATE_SHAFT_OPENING`, `CREATE_OPENING` widened (2026-11-02)

Asked for in the modeller's words: *"cut a shaft opening from Level 1 to Roof through all the slabs here"*, *"add a
shaft for the riser at these corners, base Level 0 top Level 6"* and *"put a 1200 x 800 opening in this wall at 900
above the floor"* - Revit's Architecture > Opening > Shaft and Opening > Wall. No card cut a shaft: a grep of every
fragment's C# that day for `NewOpening` found the host-element, framing and family calls in `CREATE_OPENING` and the
host-element call in `CREATE_STAIRS`, never the one that takes two levels. `CREATE_OPENING` (DRAFT, version 3) named
walls in its purpose but sent a wall to the call Revit answers *"The hostElement is not a floor, ceiling or roof"*
(its own proof, 2026-10-18; [AJ9](group-aj.md) asked for the wall). So the wall half **widened that card** to version
4, and the shaft is **a card of its own** - two levels and a plan outline, no host, cutting every slab between: a
different contract (heron-duplicate-checker's method; `SELECT_OPENINGS` finds shafts and makes none - a look-alike).

| What | Where | What it does |
|---|---|---|
| `CREATE_SHAFT_OPENING` (MODIFY) | [`brain/fragments/create-shaft-opening`](../../brain/fragments/create-shaft-opening/fragment.yaml) | Cuts a **shaft** from a base level up to a top level, both **by exact name - never guessed**: a name no level has, or two share, makes nothing and **lists every level** with its elevation. The outline is **corners in plan in mm**, at least three, closed back to the first; a top level **not above** the base, two corners in one place, a side shorter than Revit's shortest line, no area and an outline that **crosses itself** are refused by name. A family in front is refused. **Read back, all or nothing**: the outline's extent to a millimetre, and its Base and Top Constraint against the two levels - a miss rolls the call back. Every floor, roof and ceiling in its reach has its **volume read before and after**; those that lost volume are `slabsCut` (`created`, `openingId`, `built`, `slabsCut`, `refused`) |
| `CREATE_OPENING` version 4 (MODIFY) | [`brain/fragments/create-opening`](../../brain/fragments/create-opening/fragment.yaml) | A **wall** takes Revit's own wall opening, `NewOpening(Wall, XYZ, XYZ)` - an upright rectangle from `wallOpening` *"1200 x 800 at 900"* (width, height, sill above the wall's base level, mm; centred on the wall unless *"along 2500"*), or from boundary points that are one rectangle's corners on the wall. A **curved** wall is refused - whether the call cuts one is not known - and a **curtain** wall by name; an opening running off the wall's end, bottom or top, and `wallOpening` with any other host or in a family, are refused. **Read back** from the opening's own rectangle - size, sill and centre to a millimetre; a miss deletes it again. `perpendicularFace` may now be left out (false). Floors, roofs, ceilings, beams and families unchanged |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: all four `Creation.Document.NewOpening` overloads, `Opening.BoundaryCurves`,
`BoundaryRect`, `IsRectBoundary` and `Host`, `Wall.Width`, `WallType.Kind`, `WallKind.Curtain`,
`Level.ProjectElevation`, `Parameter.AsElementId`, `Document.Delete` and `BuiltInParameter` `WALL_BASE_CONSTRAINT`,
`WALL_HEIGHT_TYPE` and `HOST_VOLUME_COMPUTED` are the same 2020 to 2027. **Nothing here has run in Revit.** Both cards
are DRAFT. **Only Revit can settle** whether a shaft carries its levels in `WALL_BASE_CONSTRAINT` and
`WALL_HEIGHT_TYPE` (when either is missing the findings say its levels were not read back); whether a floor's
`HOST_VOLUME_COMPUTED` drops inside the transaction when a shaft cuts it - `CREATE_STAIRS` measured a shaft from Level 1
to Level 2 that removed nothing from either slab (2026-10-03, rolled back), so a shaft that took nothing out is said,
not refused; whether a slab whose top is AT the shaft's top level is cut; and whether `NewOpening(Wall, ...)` cuts a
curved wall, which is refused until somebody checks. LN2 to LN5 check them.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `c91de86` and of this change
(`model` backend, no re-ranker; Revit 2024; with and without the host's question flag). The three sentences reach these
cards by identity - before, `SELECT_OPENINGS` (a read), `CREATE_STAIRS` and `CREATE_FROM_ROOM_BOUNDARIES`. One sentence
the shaft card pulled away, *"cut an opening in this slab"* (`CREATE_STAIRS`' row for `CREATE_OPENING`), is declared on
`CREATE_OPENING` and written in both cards - [row 5b-1393](../fragment-issues/section-5b-rows-401-425.md), FIXED.
`tools/check-routing.py` (its own scratch store): no question answered by a write before or after.

**The model - `shaft-ln`**, a scratch project from the default architectural template, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is
[`tools/jobs/create-shaft-and-wall-opening-ln-2026-11-02.yaml`](../../tools/jobs/create-shaft-and-wall-opening-ln-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LN1** | *Arrange.* `shaft-ln`: levels "Level 1" at 0, "Level 2" at 4000 and "Roof" at 8000 mm; one floor 200 mm thick on "Level 2" and one on "Roof", each 0,0 to 10000,10000 mm; one straight basic wall on "Level 1", 200 mm thick, 0,0 to 6000,0 mm, 3000 mm high unconnected; "Section 1" looking north across the middle. Save | Three levels, two floors, one wall |
| **LN2** | *The shaft - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, base "Roof" top "Level 1": nothing cut, refused naming both levels and their elevations. Positive, "Level 1" to "Roof" over 4000,4000 to 5200,4800: `created` 1, `built` *1200 x 800 mm, 0.96 m2 - read back*; RECORD `slabsCut` and each volume, and whether the levels were read back or the finding says not. **If nothing was cut**, look in Section 1 and record it - that is the CREATE_STAIRS observation again |
| **LN3** | *The wall - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back | Negative, "1200 x 800 at 2500": nothing cut, refused *it would run off the wall's bottom or top*. Positive, "1200 x 800 at 900": `created` 1, `built` *A 1200 x 800 mm opening ..., its sill 900 mm above "Level 1", its centre 3000 mm along the wall*; the centred finding |
| **LN4** | *Never guessed, refused by name.* Shaft with base "Level 0" top "Level 6"; corners "0,0,0; 1000,1000,0; 1000,0,0; 0,1000,0"; corners "0,0,0; 1000,0,0". Wall opening on a CURVED wall drawn for the purpose; on a curtain wall; `wallOpening` with a floor as host | The first refused naming both names and listing the three levels; the bow tie *crosses itself*; two corners *at least three*; the curved wall refused *not checked*; the curtain wall by name; the floor *`wallOpening` is a wall's* - nothing cut by any |
| **LN5** | *The second route; what Revit does that only Revit can say.* Architecture > Opening > Shaft drawn by hand from "Level 1" to "Roof" over the same corners beside a kept one; Opening > Wall by hand at 1200 x 800, sill 900. Then, on a scratch copy only, `NewOpening(Wall, ...)` on a curved wall by a probe outside the repository | The two shafts and the two wall openings alike in Section 1 and an elevation, and counted by `SELECT_OPENINGS`; whether the Roof floor is cut; what Revit says to a curved wall - recorded, so the refusal can be kept or lifted |
