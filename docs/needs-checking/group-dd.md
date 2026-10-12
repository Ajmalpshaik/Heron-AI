# Needs checking — Group DD

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-11 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DD - an electrical connector's voltage, poles, apparent load and power factor, set or driven by a family parameter (2026-10-12)

The same three Revit 2020 families as [Group DC](group-dc.md) - a fire alarm control panel, a
three-phase rotary isolator, an LV switchboard - each got an electrical connector from
`ADD_FAMILY_CONNECTOR`, and nothing could give it a voltage, a number of poles or a load:
`SET_FAMILY_CONNECTOR_ROLES` set system, flow, loss, description, flip and size, nothing electrical.

**What was built, by widening - no new capability.** `SET_FAMILY_CONNECTOR_ROLES` version 3
(FRG-MEP-055) takes `voltage` (V, kV), `poles` (1 to 3), `apparentLoad` (VA, kVA), `powerFactor` (0 to 1,
or a percentage) and `loadClassification` (a name) in its `settings`, on an electrical connector only.
Each of the first four is a value, a `[family parameter]` that drives it - made, as a type parameter of
the field's own kind under Electrical - Loads, when the family has none of that name - or both, which
also writes the value into the current type. The house rule chose the widening: it is the same job, a
setting of a connector already in the family, named the same way. Version 2's proof is kept as a record
and the card reads STALE until it is proved again.

**Measured without Revit, 2026-10-12.** The five built-in parameters and `ElectricalLoadClassification`
are on every release 2020 to 2027; a parameter's kind, `FamilyManager.AddParameter` and the unit
conversion moved at 2021 and 2022 and are reached by reflection (`tools/api-surface --members`). It
compiles on all eight releases, and "set the connector voltage and poles" and "make the connector 3
phase 400 V" reach it on Revit 2020 and 2024. **Nothing below has run in Revit.** The arrangement is in
[`set-family-connector-electrical-2026-10-12.yaml`](../../tools/jobs/set-family-connector-electrical-2026-10-12.yaml),
read by `batch-prove --dry-run` as 1 WOULD RUN.

**The unit is the thing to watch.** Revit keeps a voltage and a load in feet-based internal units; the
code writes 1 V and 1 VA as 1 / 0.3048² of them, and refuses before changing anything if Revit's own
`UnitUtils` on the running release disagrees. Every value read back is quoted as Revit shows it.

Never on the owner's own families. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two
Revits are connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DD1** | Arrange on **Revit 2020**: File, New, Family, Metric Electrical Equipment.rft, saved as **Heron Connector Load Test**. `CREATE_REFERENCE_PLANES` "Box Top" at 600 (height); `CREATE_FAMILY_FORMS` "kind=extrusion workPlane=Ref. Level profile=rect -300,-150 300,150 startMm=0 endMm=600 solid=true"; `ADD_FAMILY_CONNECTOR` "Power Balanced" on "Box Top"; `ADD_FAMILY_PARAMETERS` "Width", Length, type | one box, one Power Balanced connector on its top, a Length parameter Width |
| **DD2** | `python tools/batch-prove.py tools/jobs/set-family-connector-electrical-2026-10-12.yaml`, FAMILY-TITLE filled | positive: `changed` four entries; connectorReport 400 V, 3 pole(s), 1500 VA, power factor 0.85; four findings quoting **what Revit shows** - record them whole. A voltage shown as 37.16 V or 4305.56 V is the factor wrong; the UnitUtils check should have refused it first. negative ("voltage=400 V [Width]"): refused naming both kinds, `changed` empty, no parameter made. Look at the connector afterwards - Revit's defaults: rollback has failed on this PC before (BD1) |
| **DD3** | By hand through `revit_change`, DD2's positive kept; select the connector | Properties: Voltage 400 V, Number of Poles 3, Apparent Load 1500 VA, Power Factor 0.85 - the second route for the values |
| **DD4** | `revit_change` connectors "Box Top", settings "voltage=230 V [Supply Voltage]; apparentLoad=[Rated Load]", neither in the family | two type parameters under Electrical - Loads; Family Types shows Supply Voltage 230 V; the connector's Voltage reads 230 V with the small associate button showing Supply Voltage; Apparent Load driven by Rated Load, 0 VA. Then the same call again: "already driven", nothing made twice |
| **DD5** | `revit_change` settings "loadClassification=Power" - then "loadClassification=No Such Class" | the first set and read back, **or refused listing the classifications the family holds** - which of the two says whether a family document carries load classifications at all. The second refused with that list |
| **DD6** | The refusals: "voltage=400 V" on a pipe connector (the valve family of the version 2 proof); "poles=4"; "voltage=400 V" on a connector whose Voltage DD4 linked; a project in front | each refused with its reason, nothing changed; the linked one names the family parameter and LINK_FAMILY_PARAMETER; the project `notAFamily` true |
| **DD7** | "apparentLoad=1500 VA" with the connector's system set to **Power Unbalanced** first (`system=Power Unbalanced`) | refused as read-only - Revit works it out per phase - or set; record which. Per-phase loads are not offered yet |
| **DD8** | The family loaded into a test project and placed; its Properties and an Electrical Equipment schedule | which of Voltage, Number of Poles and Apparent Load a project shows **without** a shared parameter - the owner's question of whether `[shared parameter]` is needed for a schedule |
| **DD9** | DD2 and DD4 on **Revit 2024** and **2027** - the ForgeTypeId side of every reflected call | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| **DD10** | The signature, on the owner's word only, after DD2, DD3 and DD4 | `python brain/heron_validate.py accept set-family-connector-roles --by "Ajmal PS"` |
