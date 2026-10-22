# Needs checking — Group IR

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IR - a connector's tick boxes, above all Allow Slope Adjustments: `SET_FAMILY_CONNECTOR_ROLES` version 4 and `REPORT_FAMILY_CONNECTORS` version 2 (2026-10-22)

From [row 5b-1317](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-22 the owner built a nested
floor-drain family through Heron on Revit 2020 - four Sanitary pipe connectors, one DN82 outlet and three
DN50 inlets - and asked whether to tick *Allow Slope Adjustments* on them. Yes: drain pipes are laid to a
fall. Nothing in Heron could tick it, so he ticked the four by hand. His instruction: the existing tool,
widened, never a second one.

**What was built, by widening - no new capability.** `SET_FAMILY_CONNECTOR_ROLES` version 4 (FRG-MEP-055)
takes in `settings`: `allowSlopeAdjustments=yes|no` on a PIPE connector; `utility=yes|no`; `angle=90` on a
fitting's connector (duct, pipe, cable tray, conduit); `flowFactor=0.5` and a preset `presetFlow=12 L/s` (m3/h
and cfm too) on a duct or pipe connector - angle, flow factor and flow each a value, a `[family parameter]`
or both, as the electrical values are; and `powerFactorState=Lagging|Leading` on an electrical connector.
A key another domain's connector has not got is refused in one sentence naming the connector and its
domain. `REPORT_FAMILY_CONNECTORS` version 2 (FRG-MEP-057) prints them in the same row, and the electrical
values with them. Version 3's proof (Revit 2020, 2026-10-18, Group DD) and version 1's (2026-10-21) are
kept as records; both cards read DRAFT until signed here.

**Measured without Revit, 2026-10-22.** The seven built-in parameters and `PowerFactorStateType` are on
every release 2020 to 2027 with one value each (`tools/api-surface --members BuiltInParameter`); their
labels are the Revit 2020 API reference's. The angle and flow factors - radians, cubic feet per second -
are checked against the running Revit's own `UnitUtils` before anything is written, as the voltage's is.
Routing was measured on four backends, main against the branch, on a frozen sentence set.
**Nothing below has run in Revit.** The arrangement is in
[`set-family-connector-ticks-2026-10-22.yaml`](../../tools/jobs/set-family-connector-ticks-2026-10-22.yaml).

**Two things only Revit can say.** Whether a pipe connector's box is the element's own parameter
(`RBS_ADJUSTABLE_CONNECTOR` on the `ConnectorElement`), which the API reference names and a project reads
back as the placed connector's `AllowsSlopeAdjustments`; and which domains' connectors carry a Utility box
at all - version 4 asks any connector and refuses by name one that has none.

Never on the owner's own families. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two
Revits are connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **IR1** | Arrange on **Revit 2020**: File, New, Family, Metric Mechanical Equipment.rft, saved as **Heron Connector Ticks Test**. `CREATE_REFERENCE_PLANES` "Outlet Face" at -300 (left), "Inlet Face" at 300 (right), "Duct Face" at 600 (height); `CREATE_FAMILY_FORMS` one box "kind=extrusion workPlane=Ref. Level profile=rect -300,-150 300,150 startMm=0 endMm=600 solid=true"; `ADD_FAMILY_CONNECTOR` Sanitary pipe connector on "Outlet Face" and on "Inlet Face", a Supply Air duct connector on "Duct Face"; `ADD_FAMILY_PARAMETERS` "Width", Length, type | one box, two Sanitary pipe connectors, one duct connector. `REPORT_FAMILY_CONNECTORS` reads **"no slope adjustments"** on each pipe row and nothing of slope on the duct row - the read of the box before any write |
| **IR2** | `python tools/batch-prove.py tools/jobs/set-family-connector-ticks-2026-10-22.yaml`, FAMILY-TITLE filled | positive: `changed` two entries *"Allow Slope Adjustments no -> yes"*, connectorReport **"slope adjustments allowed"** on both pipe connectors, two findings quoting what Revit shows - record them whole. negative (the duct connector): refused *'duct connector NNNN on "Duct Face" is a duct connector - Allow Slope Adjustments is a pipe connector's'*, `changed` empty, the box as it was. A positive refused *"has no Allow Slope Adjustments in this Revit"* means the box is NOT the element's own parameter - stop, record it as a new row, nothing else here can pass |
| **IR3** | By hand through `revit_change`, IR2's positive kept; select the outlet connector in the Family Editor | Properties: **Allow Slope Adjustments ticked** - the second route. Then `"allowSlopeAdjustments=no"` on "Outlet Face": `changed` *"yes -> no"*, Properties unticked, the report reading "no slope adjustments" on it and "slope adjustments allowed" on the inlet |
| **IR4** | `"allowSlopeAdjustments=yes"` again on "Outlet Face" after IR3; then on the electrical connector of DD1's family (Heron Connector Load Test) | the first: *"was already ticked"* when it is, else set; the second refused *'... is an electrical connector - Allow Slope Adjustments is a pipe connector's'*, nothing changed |
| **IR5** | `"utility=yes"` with connectors "all" on IR1's family, then on DD1's electrical connector | each connector that carries the box reads **"utility"** in its row and Properties show Utility ticked; one that has none is refused *"has no Utility in this Revit"* and the whole call changes nothing. **Record which domains carry it** - the card says "any connector" until this says otherwise. Then `"utility=no"` puts them back |
| **IR6** | `"flowConfiguration=Preset; presetFlow=12 L/s"` on "Outlet Face"; then `"flowConfiguration=System; flowFactor=50%"` on "Duct Face" | the pipe row reads *"flow configuration Preset, preset flow 12 L/s"* and the finding quotes Revit's Flow in the family's units; the duct row *"flow configuration System, flow factor 0.5"*. **A flow shown as 0.34 L/s or 423.8 L/s is the factor wrong**; the UnitUtils check should have refused it first. Then `"presetFlow=100 m3/h"` reads 27.778 L/s and `"presetFlow=250 cfm"` 117.987 L/s |
| **IR7** | A Metric Pipe Fitting family, Part Type Elbow, two Fitting pipe connectors on "Inlet" and "Outlet" (the mep-fitting method's steps 1 to 6) and a type Angle parameter; `"angle=90 [Angle]"` with connectors "all" | both rows read *"angle 90 degrees (<- Angle)"*, Angle holds 90 in the current type, the connector's associate button shows Angle; `FLEX_FAMILY` at Angle 45 moves the outlet. Then `"angle=45"` alone on a connector already driven: refused naming the family parameter and LINK_FAMILY_PARAMETER - the rule the electrical values follow |
| **IR8** | `"angle=90"` on IR1's Sanitary outlet connector - not a fitting's | set and read back, **or** refused *"has no Angle in this Revit"*: record which. The card prints an angle only on a Fitting connector; if Revit carries one on every pipe connector, the read's rule stands and only this row's answer is recorded |
| **IR9** | The refusals: `"powerFactorState=Lagging"` on "Outlet Face" (a pipe connector); `"angle=90"` on DD1's electrical connector; `"allowSlopeAdjustments=maybe"`; `"presetFlow=12 L/s; presetFlow=13 L/s"`; `"presetFlow=-5 L/s"`; a project in front | each refused with its sentence - the first two naming the connector and its domain - `changed` empty, nothing written; the project `notAFamily` true |
| **IR10** | `"powerFactorState=Leading"` on DD1's electrical connector, then `"powerFactorState=Lagging"` | the row reads *"power factor state Leading"*, then *"Lagging"*; Properties agree each time |
| **IR11** | `"lossCoefficient=0.5"` on "Outlet Face" - a PIPE connector, which version 3 never tried (its proof was a duct) | set and read back, **or** refused *"has no Loss Coefficient in this Revit"* - then a pipe connector's field is `RBS_PIPE_FITTING_LOSS_KFACTOR_PARAM` ("K Coefficient"), a new row, and the card's `lossCoefficient` owes a pipe route |
| **IR12** | `REPORT_FAMILY_CONNECTORS` on IR1's family after IR2 kept, and on DD1's family | every pipe row carries its slope box either way, the duct row nothing of it; DD1's electrical row reads its voltage, poles, load, power factor, power factor state and load classification - the values version 1 never printed |
| **IR13** | IR2, IR6 and IR12 on **Revit 2024** and **2027** - the ForgeTypeId side of every reflected unit and group | the same answers. Compiled on all eight releases; what Revit does was measured on none |
| **IR14** | The signatures, on the owner's word only, after IR2, IR3, IR6 and IR12 | `python brain/heron_validate.py accept set-family-connector-roles --by "Ajmal PS"` and `python brain/heron_validate.py accept report-family-connectors --by "Ajmal PS"` |

**Then** row 5b-1317 can close, and the family method's step 11 line about a drain's connectors loses
its "DRAFT until Group IR passes".
