# Needs checking — Group KW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KW - add a load classification or a demand factor: `ADD_LOAD_CLASSIFICATION` (2026-11-01)

Asked for in the modeller's words: *"add a load classification called Kitchen Equipment"* and *"add a
demand factor of 80% over 10 kVA"* - Revit's Manage > MEP Settings > Load Classifications and Demand
Factors. No card made either: a grep of `brain/fragments` that day for `ElectricalLoadClassification.Create`
and `ElectricalDemandFactorDefinition` found nothing. `SET_FAMILY_CONNECTOR_ROLES` gives a family's
connector a load classification the project already has and never makes one.

| What | Where | What it does |
|---|---|---|
| `ADD_LOAD_CLASSIFICATION` (MODIFY) | [`brain/fragments/add-load-classification`](../../brain/fragments/add-load-classification/fragment.yaml) | Adds **one load classification** - optionally using a demand factor the model has, matched exactly - or **one demand factor**: constant (`percent`), or a table (`steps` "0-10:100; 10-:80") over kVA (`load`, `loadPortion`) or quantity (`quantity`, `quantityPortion`). A name is never made up; steps with no rule are refused, because "80% over 10 kVA" is a table of the whole load or of each portion and the two give different demand loads. A name already present, in any capitals, is **skipped and named**. The list before and after. Never renames, overwrites or deletes |

**One of a set** with `REPORT_ELECTRICAL_SETTINGS` ([Group KU](group-ku.md)), which answers every question,
and `ADD_ELECTRICAL_DISTRIBUTION` ([Group KV](group-kv.md)); why two writes is written in Group KV.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `ElectricalLoadClassification.Create(Document, string)` and `DemandFactorId`,
`ElectricalDemandFactorDefinition.Create(Document, string)`, `RuleType`, `SetValues`, `GetValues`,
`ElectricalDemandFactorValue(double, double, double)` and `SetMaxRangeToUnlimited` are present 2020-2027
with the same signatures - neither `Create` differs before 2021. kVA are written as kVA x 1000 x 1/0.3048²
internal units - `SET_FAMILY_CONNECTOR_ROLES`' volt-ampere factor - checked against `UnitUtils` on the
release that runs. **Nothing here has run in Revit.** The card is DRAFT. **Not known without Revit:** that
a percentage is stored as its fraction (80% as 0.8), and how Revit stores a constant demand factor's one
value - KW3 reads both off the Demand Factors dialog.

**Routing.** Measured through `heron_retrieve.find` on three fresh scratch stores - the tree at `5ab0068` without the three electrical settings cards, with `REPORT_ELECTRICAL_SETTINGS` alone, and with all three - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence in the library (4,338 with eleven probes), with and without the host's question flag. *"add a load classification called Kitchen Equipment"* and *"add a demand factor of 80% over 10 kVA"* reach this card without the flag (`SET_FAMILY_SETTINGS` and `SET_FAMILY_CONNECTOR_ROLES` before) and `REPORT_ELECTRICAL_SETTINGS` with it. The probe *"which demand factor does the lighting load classification use"* - a question nobody has said - reaches this write without the flag (`SET_FAMILY_LIGHT_SOURCE`, a write, before) and the read with it; not declared. What else moved is written in [Group KU](group-ku.md).

**The model - Group KU's scratch project** (`electrical-settings-ku`, [KU1](group-ku.md)) in Revit 2020,
never a working model. The job file is
[`tools/jobs/add-load-classification-kw-2026-11-01.yaml`](../../tools/jobs/add-load-classification-kw-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every write is **rolled back** (`validate --write`) except
the one `try-once`, which the modeller keeps or discards. `REPORT_ELECTRICAL_SETTINGS` before and after
every row. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KW1** | *Arrange.* In KU's project, write down the load classifications with the demand factor each uses (Load Classifications dialog) and the demand factors (Demand Factors dialog). The job file assumes a load classification named **Lighting**; use the template's name if it differs | The lists the template ships |
| **KW2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, a load classification named **Lighting**: `added` 0, skipped *A load classification is already named "Lighting" - left as it is*, Lighting's demand factor the same in `before` and `after`. Positive, **Kitchen Equipment**: `added` 1, it in `after` beside every name before, and in the dialog |
| **KW3** | *The demand factors.* `add` demandFactor, `name` "Kitchen 80 over 10", `rule` load, `steps` "0-10:100; 10-:80"; then `name` "Flat 70", `percent` 70; then `name` "Kitchen 2" with the same steps and `rule` left out | First: the dialog shows Calculation Method *By Load*, 0-10 kVA at 100% and over 10 kVA at 80% - **not 0.8% and not 10,000 kVA**. Second: constant 70%. Third: `added` 0, refused - steps with no rule, never guessed |
| **KW4** | *A load classification with a demand factor.* `add` loadClassification, `name` "Kitchen Equipment 2", `demandFactor` "Kitchen 80 over 10" (kept in KW3), then `demandFactor` ZZNOFACTOR | First: the dialog shows that demand factor against it. Second: `added` 0, refused naming the model's demand factors |
| **KW5** | *Revit 2026 or 2027.* KW2 and KW3's first case again on a project from that release's template | The same answers |
