# Needs checking — Group KX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KX - which sizes the duct, conduit and cable tray size lists hold: `REPORT_DUCT_CONDUIT_SIZES` (2026-11-01)

Asked for in the modeller's words: *"what duct sizes are available for round ducts"* - Revit's Manage >
MEP Settings > Mechanical Settings > Duct Settings (Rectangular, Oval, Round) and Electrical Settings >
Conduit Settings > Size and Cable Tray Settings > Size. No card read them: a grep of `brain/fragments` that
day for `DuctSizeSettings`, `ConduitSizeSettings` and `CableTraySizes` found nothing. `AUTO_SIZE_MEP` and
`SET_MEP_SIZE` size ducts in the model and `SELECT_BY_CONNECTOR_SIZE` selects by size - look-alikes, none
reads the size lists. **Pipe segments are not here on purpose:** the modeller's *"list the sizes in the
copper pipe segment"* is `REPORT_PIPE_SEGMENTS`' (PROVEN), which returns one segment's whole size table
when named down to it, and still reaches it with these cards in the library.

| What | Where | What it does |
|---|---|---|
| `REPORT_DUCT_CONDUIT_SIZES` (READ) | [`brain/fragments/report-duct-conduit-sizes`](../../brain/fragments/report-duct-conduit-sizes/fragment.yaml) | Lists the **round, rectangular and oval duct sizes**, **each conduit standard's sizes** with inside diameter, outside diameter and bend radius, and the **cable tray sizes** - every size in mm, smallest first, a size hidden from the size list or left out of sizing marked (`report`, `tables`, `sizeCount`, `names`). `show` picks lists, `standard` narrows the conduit standards; a word that is not a list is named, never guessed. Opens no transaction |

**The read half of a pair** - every question about these lists is answered here, so none reaches
`EDIT_MEP_SIZES` ([Group KY](group-ky.md)).

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `DuctSizeSettings` (its `DuctShape` indexer), `DuctSizes`,
`MEPSize`, `ConduitSizeSettings` (enumerated standard by standard), `ConduitSizes`, `ConduitSize` and
`CableTraySizes` are present 2020-2027 with the same signatures. **Nothing here has run in Revit.** The card
is DRAFT. **Only Revit can settle** that a rectangular or oval size reads back as one number in
`NominalDiameter`, as the Duct Settings dialog shows it - KX3 checks it against the dialog.

**Routing.** Measured through `heron_retrieve.find` on three fresh scratch stores - the tree at `b1a892d` without the two size cards, with `REPORT_DUCT_CONDUIT_SIZES` alone, and with both - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence of a live card (4,348 sentences, eight probes of the same requests in other words included), with and without the host's question flag. *"what duct sizes are available for round ducts"* reaches this card with and without the flag (`AUTO_SIZE_MEP`, a write, without the flag before, and `MEASURE_MEP_VELOCITY` with it); *"list the sizes in the copper pipe segment"* still reaches `REPORT_PIPE_SEGMENTS` both ways. So do the probes *"what conduit sizes do we have"*, *"show me the rectangular duct sizes"* and *"what cable tray sizes are set up"* (two of them from a write before). **Without the flag no sentence any card declares or claims moved** but these. With the flag, 32 sentences that are writes' own words moved, every one to a read - from a read but for three cable tray sentences that left `PLAN_CONNECTION_ORDER` (ANALYZE); none left a card that declares or claims it. **Still reaching a write without the flag:** the probes *"is 315 in the round duct sizes"* and *"which sizes does the EMT conduit standard have"* reach `EDIT_MEP_SIZES` (`AUTO_SIZE_MEP` and `CREATE_PIPE_SEGMENT` before - write to write); nobody's words, so not declared; with the flag both reach this read. `tools/check-routing.py` on a scratch store: no question answered by a write; *CLAIMED IN A ROUTING TABLE AND NOT REACHED* 71 before and after, the same rows.

**The model - a scratch project new from Revit's metric SYSTEMS template in Revit 2020**, never a working
model. Nothing below changes the model. The job file is
[`tools/jobs/report-duct-conduit-sizes-kx-2026-11-01.yaml`](../../tools/jobs/report-duct-conduit-sizes-kx-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KX1** | *Arrange.* A new project from the metric Systems template (Mechanical where there is none), saved as `mep-sizes-kx` under `%TEMP%\heron-work`. Open Duct Settings > Round, Rectangular and Oval, Conduit Settings > Size (every standard in its dropdown) and Cable Tray Settings > Size, and write down every size | The lists the template ships |
| **KX2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, conduit narrowed to **ZZNOTASTANDARD**: `tables` 0, findings *No conduit standard has a name containing "ZZNOTASTANDARD". The standards in this model: ...* naming every standard KX1 wrote down. Positive, `show` round: `tables` 1, every round size KX1 wrote down, smallest first, and no other list |
| **KX3** | *The second route.* The card with `show` and `standard` left out | Every list as KX1 wrote it down - the rectangular and oval sizes as single numbers, each conduit size's ID, OD and bend radius as the dialog shows them |
| **KX4** | *A list word that is not one.* `show` "segments" | findings name "segments" as not a size list and point at `REPORT_PIPE_SEGMENTS`; `tables` 0 |
| **KX5** | *Revit 2026 or 2027.* KX2 and KX3 again on a project from that release's template | The same answers |
