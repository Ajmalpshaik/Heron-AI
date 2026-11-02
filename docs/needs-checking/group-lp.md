# Needs checking — Group LP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LP - a rigid duct turned into flex between the same two fittings, and Connect Into with a fixture's right connector: `CONVERT_TO_FLEX_DUCT`, `CONNECT_AIR_TERMINALS` widened (2026-11-02)

Asked for in the modeller's words: *"convert this last duct run to the diffuser into flex duct"*, *"change these
short ducts to flex"*, *"connect this diffuser into the main duct"*, *"connect this sink into the waste pipe"* and
*"tap this equipment into the main"* - Revit's Systems > Convert to Flex Duct and Connect Into. Revit has no API
call for either. heron-duplicate-checker's method: Convert to Flex Duct is a **look-alike** of `CREATE_DUCT_RUNS`
(which draws new flex along points and joins nothing; its predecessors `CREATE_FLEX_DUCT` and `CREATE_FLEX_PIPE` are
retired into it), so a new card; Connect Into **overlaps** `CONNECT_AIR_TERMINALS` (DRAFT), which already taps
terminals into the duct handed in and branches sprinklers and fixtures to the pipe handed in, so that card was
widened to version 3 rather than a second written. `FIT_MEP_JOINTS` and `CONNECT_OPEN_ENDS` (PROVEN) join pieces
already drawn and were not touched.

| What | Where | What it does |
|---|---|---|
| `CONVERT_TO_FLEX_DUCT` (MODIFY) | [`brain/fragments/convert-to-flex-duct`](../../brain/fragments/convert-to-flex-duct/fragment.yaml) | Each **straight duct with exactly two joined ends** and nothing tapped into its side is **removed** and a flex duct of the type **named - never guessed** (not named or not found lists the loaded flex duct types with their shapes) is drawn between the same two fittings or terminals, joined at both ends, at the rigid duct's size, system and level. A flex type of another shape, a duct over `maxLengthMm`, a placeholder, flex, pipe or a borrowed element is named and left. Each duct is its own sub-transaction: Revit deleting more than what hangs on the duct, either fitting gone, a size or system Revit changes, or an open end **puts that duct back**. Every rigid duct removed by id (`flexDucts`, `removed`, `notConverted`) |
| `CONNECT_AIR_TERMINALS` version 3 (MODIFY) | [`brain/fragments/connect-air-terminals`](../../brain/fragments/connect-air-terminals/fragment.yaml) | **Connect Into**: the main is only ever one handed in. A fixture with **more than one open pipe connector** - a sink's waste, cold and hot - uses only the connector whose system classification is the main's; none or more than one matching draws nothing and names the connectors. **`created`** holds every element made - the tap fitting Revit cut for a terminal, and each riser and arm of a branch. Versions 1 and 2 otherwise unchanged |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`, the same 2020 to 2027: `Creation.Document.NewFlexDuct(Connector, Connector,
FlexDuctType)`, `Document.Delete(ElementId)`, `Element.GetDependentElements`, `Connector` `ConnectorType`, `AllRefs`,
`Owner`, `Origin`, `Shape`, `IsConnected` and `PipeSystemType`, `MEPCurve` `ConnectorManager`, `ReferenceLevel`,
`Diameter`, `Width`, `Height`, `MEPCurveType.Shape`, `MEPSystemType.SystemClassification`, `FamilyInstance.MEPModel`,
`WorksharingUtils.GetCheckoutStatus`. **Nothing here has run in Revit.** Both cards are DRAFT. **Only Revit can
settle** whether deleting a branch duct leaves its takeoff in place (if Revit deletes the takeoff with it, the card
puts the duct back and says so - LP2 records which); whether `NewFlexDuct` takes the rigid duct's size from the
fittings or needs it set; and whether a connector's `PipeSystemType` name matches its system type's classification
name on a real sink family. **Not done:** Connect Into from **equipment** - *"tap this equipment into the main"* is
not declared on any card; a duct connector on mechanical equipment still goes to the air-terminal tap call, and a
sideways pipe connector is still refused.

**Routing.** `tools/check-routing.py` on its own scratch store, on a copy of the tree at `d73d605` and on this
change (`model` backend): no question answered by a write; claimed and not reached 67 before and 67 after. Adding
`CONVERT_TO_FLEX_DUCT` pulled *"run flex to that diffuser"* - `CREATE_DUCT_RUNS`' own routing row - onto the new
card; it is declared on `CREATE_DUCT_RUNS` (DRAFT, card only) and reaches it again by identity -
[row 5b-1396](../fragment-issues/section-5b-rows-401-425.md), FIXED. On the keyword route alone one line was added,
*"what level is this equipment on"* (`READ_ELEMENT_LEVEL` second there, neither card of this change first); the
host's lookup still answers it with `READ_ELEMENT_LEVEL` by identity. The four declared sentences reach their cards
by identity; *"tap this equipment into the main"* reaches `CONNECT_AIR_TERMINALS` on the hybrid route, as before.

**The models** - scratch projects from Revit's metric Mechanical and Plumbing templates, saved under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is
[`tools/jobs/flex-and-connect-into-lp-2026-11-02.yaml`](../../tools/jobs/flex-and-connect-into-lp-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LP1** | *Arrange.* `flex-lp`: in "Level 1" on Supply Air, a round 300 mm main with a takeoff to a round 200 mm straight D1 (about 1200 mm) ending in a round-neck diffuser; a round 200 mm straight D2 (about 800 mm) between two elbows; a rectangular 300x200 straight D3 between two elbows; a round 200 mm straight D4 joined at one end only - write down every id and the round flex duct type's exact name. `connect-lp`: one diffuser, open, under one straight supply duct. `sink-lp`: one sink (sanitary facing down, cold, hot) above one straight Sanitary pipe; `sink-lp-two`, the same plus a straight Domestic Cold Water pipe. Save each | The ids, and the flex type's name in the type selector |
| **LP2** | *Convert to flex - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, `flexDuctType` "Flex": nothing changed, the loaded flex duct types listed with their shapes. Positive: D1 and D2 removed and replaced by flex joined at both ends, d200 on Supply Air; the main (*something is tapped into its side*), D3 (*rectangular ... round*) and D4 (*an end is not joined*) named and still rigid. **RECORD whether the takeoff and the diffuser are still there after D1 went** |
| **LP3** | *Connect Into, a diffuser - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back | Negative, `maxDistanceMm` 10: the diffuser in `noDuctNear`, `created` empty. Positive: `connected` 1, `created` the takeoff's id, the finding naming it; read `moved` (row 5b-187) |
| **LP4** | *Connect Into, a sink - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's third job, `--write`, rolled back | Negative, `ducts` "Ducts": nothing drawn, *no straight pipe was handed in as a main*. Positive: riser and arm from the **sanitary** connector on the Sanitary pipe's type and system, their ids in `created`; cold and hot still open |
| **LP5** | *Never guessed - the version 3 rule.* `sink-lp-two`, the sink selected, `ducts` "Pipes", `maxDistanceMm` 1500, `--write`, rolled back | Nothing drawn; `refused` names the sink's three connectors and both systems - *hand in the one main its connector belongs on* |
| **LP6** | *The second route.* Revit's own Convert to Flex Duct on D1 and Connect Into on the diffuser and the sink, by hand on copies of the same saved files | The same fittings joined, the same takeoff kept or gone, the same sizes and systems as the reports gave; Undo takes each run back as one entry |
