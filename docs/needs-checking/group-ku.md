# Needs checking — Group KU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KU - which voltages, distribution systems, load classifications and demand factors a model has: `REPORT_ELECTRICAL_SETTINGS` (2026-11-01)

Asked for in the modeller's words: *"what voltages and distribution systems are set up in this model"* and
*"list the load classifications and demand factors"* - Revit's Manage > MEP Settings > Electrical Settings
(Voltage Definitions, Distribution Systems, Wiring Types), Load Classifications and Demand Factors. No card
read them: a grep of `brain/fragments` that day for `ElectricalSetting`, `VoltageType`,
`DistributionSysType` and `ElectricalDemandFactorDefinition` found nothing. `SET_FAMILY_CONNECTOR_ROLES`
picks a load classification for a family connector, `REPORT_PANEL_CIRCUITS` reads circuits and panel
loads, `CREATE_WIRES` picks a wire type - look-alikes, none reads the settings.

| What | Where | What it does |
|---|---|---|
| `REPORT_ELECTRICAL_SETTINGS` (READ) | [`brain/fragments/report-electrical-settings`](../../brain/fragments/report-electrical-settings/fragment.yaml) | Lists the **voltage types** (actual, minimum and maximum volts, in use), the **distribution systems** (1- or 3-phase, wye or delta, wires, line-to-line and line-to-ground voltages), the **wire types** (material, temperature rating, insulation, maximum size, neutral), the **load classifications** with the demand factor each uses, and the **demand factors** with their rule and steps in kVA or quantity and percent (`report`, five counts, `names`). `show` picks lists, `nameContains` narrows them; a word that is not a list is named, never guessed. Opens no transaction |

**The read half of a set** - every question about these settings is answered here, so none reaches
`ADD_ELECTRICAL_DISTRIBUTION` ([Group KV](group-kv.md)) or `ADD_LOAD_CLASSIFICATION`
([Group KW](group-kw.md)).

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: every member it calls is present 2020-2027 with the same
signature, except `ElectricalLoadClassification.Abbreviation` (2021 on, `#if !REVIT2020`) and `WireType`'s
material, insulation and temperature rating (objects to 2025, `ElementId`s from 2026) and `MaxSize` (a
`WireSize` to 2025, a string from 2026), each behind `#if REVIT2026_OR_GREATER`. **Nothing here has run in
Revit.** The card is DRAFT. **Two things only Revit can settle:** that a demand factor's `Factor` is a
fraction (0.8 shown as 80%), and that an open-ended step's upper end reads back as a very large number
(shown "and over") - KU3 checks both against the dialog.

**Routing.** Measured through `heron_retrieve.find` on three fresh scratch stores - the tree at `5ab0068` without the three electrical settings cards, with `REPORT_ELECTRICAL_SETTINGS` alone, and with all three - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence in the library (4,338 with eleven probes), with and without the host's question flag. *"what voltages and distribution systems are set up in this model"* and *"list the load classifications and demand factors"* reach this card with and without the flag (`READ_MEP_SYSTEM` and `REPORT_PANEL_CIRCUITS` before), and so do the probes *"what voltage types are in this model"*, *"what distribution systems do we have"*, *"what wire types are set up"* and *"show me the electrical settings"*. Without the flag two sentences moved, neither from a card that claims it, both written in the card: *"put it back"*, claimed by `SHOW_ELEMENTS` and reaching it on neither tree, from `PLACE_FAMILY_INSTANCES` to `RESET_GRAPHIC_OVERRIDES` - write to write, the flip [row 5b-1375](../fragment-issues/section-5b-rows-401-425.md) (OPEN) records; and *"model the ground from these levels"*, `CREATE_TOPOSOLID`'s own words, which still reaches it by identity on 2024 and later and, behind the 2020 wall where no card makes a toposolid, went from `REMOVE_MASS_FLOORS` (a write) to this read. With the flag every sentence that moved now reaches a read - from a read, but for *"draw wires between these sockets"* (`PLAN_CONNECTION_ORDER`, ANALYZE, before) and *"we installed a new Revit and the content is on the old one"* (`OPEN_FAMILY_FOR_EDITING`, EXECUTE, before). **Still reaching a write without the flag:** the probe *"which demand factor does the lighting load classification use"* reaches `ADD_LOAD_CLASSIFICATION` - nobody's words, so not declared; with the flag it reaches this read. `tools/check-routing.py` on a scratch store: no question answered by a write; *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 71 before and after, the same rows.

**The model - a scratch project new from Revit's metric ELECTRICAL template in Revit 2020**, never a
working model. Nothing below changes the model. The job file is
[`tools/jobs/report-electrical-settings-ku-2026-11-01.yaml`](../../tools/jobs/report-electrical-settings-ku-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KU1** | *Arrange.* A new project from the metric electrical template (Systems template where there is none), saved as `electrical-settings-ku` under `%TEMP%\heron-work`. Open Manage > MEP Settings > Electrical Settings, Load Classifications and Demand Factors and write down every name and value | The lists the template ships |
| **KU2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, voltages narrowed to **ZZNOTASETTING**: `voltageTypes` 0, findings *Nothing in the lists read has a name containing "ZZNOTASETTING"*. Positive, `show` voltages: every voltage type KU1 wrote down with its actual, minimum and maximum volts; the other four counts 0 |
| **KU3** | *The second route.* The card with `show` and `nameContains` left out | Every list as KU1 wrote it down - each distribution system's phase, wires and voltages, each load classification's demand factor, each demand factor's rule and percentages **as percentages** (80%, not 0.8%) and kVA (not 10,000 kVA) |
| **KU4** | *A list word that is not one.* `show` "panels" | findings name "panels" as not one of the lists; every count 0 |
| **KU5** | *Revit 2026 or 2027.* KU1 to KU3 again on a project from that release's template | The same answers; the wire types' material, insulation and temperature rating names read through the 2026 `ElementId` branch, not "none" |
