# Needs checking — Group DM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DM - MEP systems made and re-typed, panel circuits read, wires drawn, spaces kept in step with linked rooms (2026-10-12)

On 2026-10-12 the owner had another Revit AI plugin's published tool list checked against Heron, job by
job, and asked for every job Heron could not do to be built the Heron way - general, widening an existing
fragment where one nearly fits. The plan and its build list are in
[`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md). This group is
its first batch, the MEP one: five jobs no fragment did.

**What was built - five new capabilities, after the nearest were weighed.**
`CREATE_MEP_SYSTEM` (FRG-MEP-062) makes one duct or piping system from equipment and the terminals or
fixtures it serves; `CREATE_MEP_SYSTEM_TYPE` makes a type, not a system. `SET_MEP_SYSTEM_TYPE`
(FRG-MEP-063) moves ducts, pipes, flex and whole systems to another system type; `CHANGE_ELEMENT_TYPE`
changes an element's own type, not its system. `REPORT_PANEL_CIRCUITS` (FRG-MEP-064) reads every
panel's circuits with Revit's own formatted loads; `SELECT_BY_ELECTRICAL_CIRCUIT` selects one circuit's
devices. `CREATE_WIRES` (FRG-MEP-065) draws wires in a plan, through points or device to device;
`CREATE_ELECTRICAL_RUN` draws tray and conduit. `SYNC_SPACES_WITH_LINKED_ROOMS` (FRG-GEO-062) makes and
renames spaces to match a linked model's rooms and lists spaces with no room; `PLACE_ROOMS` fills
enclosed areas with spaces but names none of them.

**Measured without Revit, 2026-10-12.** Every Revit API member used was read off `RevitAPI.xml` in the
Nice3point reference packages for all eight releases. All five compile on all eight
(`tools/check-fragments-compile.py`), pass the author gate (duplicate check, Matcher, `heron_generate`),
and the job file reads `5 WOULD RUN` under `batch-prove --dry-run`. **Nothing below has run in Revit.**

**What the code assumes and only Revit can say** - each is reported in the answer rather than refused:
that `MEPSystem.Add` takes terminals' connectors and a base equipment set after it holds; that a flex
duct's System Type parameter can be written when it has no set-system-type call; that a panel's
`AsValueString` totals are what its panel schedule prints; that `Wire.Create` lands an end on a device's
connector and reads back connected; and that a linked room's transformed height equals the host level's
`ProjectElevation` for every Elevation Base setting.

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DM1** | Arrange on **Revit 2024**: the scratch project the header of [`gap-fill-mep-2026-10-12.yaml`](../../tools/jobs/gap-fill-mep-2026-10-12.yaml) describes, saved as **Heron Gap Fill MEP Test** | the FCU and three diffusers on no system; three loose supply ducts; two loose cold water pipes; DB-1 with one circuit; three more sockets on no circuit; the rooms link and the empty link |
| **DM2** | `python tools/batch-prove.py tools/jobs/gap-fill-mep-2026-10-12.yaml`, PROJECT-TITLE filled | create-mep-system: `memberIds` 4 then `refused` with each diffuser in `skipped`. set-mep-system-type: `changedIds` 3 then `changed` 0, each pipe "a duct system type". report-panel-circuits: DB-1's circuit then an empty `schedule`. create-wires: a chain of wires then `made` 0 for the 3D view. sync-spaces: `created` 3 then 0. Look at the model afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DM3** | By hand through `revit_change`, create-mep-system's positive kept | the System Browser shows one Supply Air system, the FCU as base equipment, the three diffusers on it; the same run again makes nothing |
| **DM4** | set-mep-system-type on a duct JOINED to the DM3 system | record what Revit does - refuses, or carries Return Air along the whole run. `report` names it either way |
| **DM5** | report-panel-circuits beside DB-1's panel schedule view | the same circuit numbers, load names, ratings and totals, in the project's units |
| **DM6** | create-wires through three points ending on a socket, snapMm 300 | one wire, its end on the socket and its start free - `looseEnds` 1 |
| **DM7** | sync-spaces kept; rename room 102 in the architectural model, reload, run with updateNames true; then delete room 103, reload, run again | `renamed` 1, then the old corridor's space in `orphans` and still in the model |
| **DM8** | DM2 on **Revit 2020** and **2027**, the arrangement made again from that release's template | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DM9** | The signatures, on the owner's word only, after DM2 to DM7 | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each of the five |
