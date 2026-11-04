# Needs checking — Group KV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KV - add a voltage type or a distribution system: `ADD_ELECTRICAL_DISTRIBUTION` (2026-11-01)

Asked for in the modeller's words: *"add a 230 V voltage type"* and *"add a 400/230 V 3-phase 4-wire
distribution system"* - Revit's Manage > MEP Settings > Electrical Settings, Voltage Definitions and
Distribution Systems. No card added either: a grep of `brain/fragments` that day for `AddVoltageType` and
`AddDistributionSysType` found nothing. `CREATE_MEP_SYSTEM_TYPE` duplicates a duct or pipe system type, a
different setting.

| What | Where | What it does |
|---|---|---|
| `ADD_ELECTRICAL_DISTRIBUTION` (MODIFY) | [`brain/fragments/add-electrical-distribution`](../../brain/fragments/add-electrical-distribution/fragment.yaml) | Adds **one voltage type** - actual, minimum and maximum volts - or **one distribution system** - 1- or 3-phase, wye or delta, its wires, and its line-to-line and line-to-ground voltages taken **by value from the voltage types already there**. A name already present, in any capitals, is **skipped and named**; with no name given, an item with the same values is skipped instead of copied. Phases, wires and wye/delta are never guessed: missing, refused. A voltage no voltage type has is refused with the model's voltages listed. The list before and after (`before`, `after`, `added`, `skipped`, `refused`). Never renames, overwrites or deletes |

**Why two writes and not one.** Voltages and distribution systems live in Electrical Settings and take
volts, phases and wires; load classifications and demand factors ([Group KW](group-kw.md)) are elements
of their own and take percentages over kVA. One contract for all four would carry a dozen inputs, most
meaningless for any one run. Every question is answered by `REPORT_ELECTRICAL_SETTINGS`
([Group KU](group-ku.md)).

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `ElectricalSetting.AddVoltageType(string, double, double, double)` and
`AddDistributionSysType(string, ElectricalPhase, ElectricalPhaseConfiguration, int, VoltageType,
VoltageType)` and every property it reads are present 2020-2027 with the same signatures. Volts are written
as volts x 1/0.3048² internal units - `SET_FAMILY_CONNECTOR_ROLES`' factor - checked against `UnitUtils`
on the release that runs (`DisplayUnitType.DUT_VOLTS` in 2020, `UnitTypeId.Volts` from 2021). **Nothing
here has run in Revit.** The card is DRAFT. **Not known without Revit:** whether `AddVoltageType` takes a
minimum and maximum equal to the actual value (the default here), and whether a distribution system with
no line-to-ground voltage (`null`) is accepted - KV3 and KV4 find out.

**Routing.** Measured through `heron_retrieve.find` on three fresh scratch stores - the tree at `5ab0068` without the three electrical settings cards, with `REPORT_ELECTRICAL_SETTINGS` alone, and with all three - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence in the library (4,338 with eleven probes), with and without the host's question flag. *"add a 230 V voltage type"* and *"add a 400/230 V 3-phase 4-wire distribution system"* reach this card without the flag (`SET_FAMILY_CONNECTOR_ROLES` before) and `REPORT_ELECTRICAL_SETTINGS` with it. No other sentence moved to or from this card; what moved is written in [Group KU](group-ku.md).

**The model - Group KU's scratch project** (`electrical-settings-ku`, [KU1](group-ku.md)) in Revit 2020,
never a working model. The job file is
[`tools/jobs/add-electrical-distribution-kv-2026-11-01.yaml`](../../tools/jobs/add-electrical-distribution-kv-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every write is **rolled back** (`validate --write`) except
the one `try-once`, which the modeller keeps or discards. `REPORT_ELECTRICAL_SETTINGS` before and after
every row. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KV1** | *Arrange.* In KU's project, delete any voltage type of 230 V or 999 V by hand (Electrical Settings > Voltage Definitions) and write down the voltage types left | No 230 V or 999 V voltage type |
| **KV2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, a 999 V 3-phase wye 4-wire distribution system: `added` 0, refused *No voltage type of 999 V for the line-to-line voltage. The model's voltage types are: ...*, distribution systems unchanged. Positive, voltage 230: `added` 1, `after` holds *"230 V" 230 V (min 230, max 230)* beside everything before |
| **KV3** | *A name already present.* `add` voltage, `volts` 999, `name` - the exact name of a voltage type KV1 wrote down; then the same name in other capitals | `added` 0 both times; skipped *A voltage type is already named ... - left as it is*; that voltage type's values the same in `before` and `after` |
| **KV4** | *The distribution system.* Voltage 400 (`minVolts` 380, `maxVolts` 420) and 230 kept by `try-once` or by hand; then `add` distribution, `volts` 400, `lineToGroundVolts` 230, `phases` 3, `configuration` wye, `wires` 4; then the same with `phases` left out; then `phases` 3, `configuration` delta, `wires` 3, no line-to-ground | First: `added` 1, *"400/230 V 3-phase 4-wire" 3-phase wye 4-wire, line to line 400 V, line to ground 230 V*, and the Distribution Systems tab shows it. Second: refused, phases never guessed. Third: added with line to ground none - or Revit's refusal, recorded as a finding against the card |
| **KV5** | *Revit 2026 or 2027.* KV2 again on a project from that release's template | The same answers |
