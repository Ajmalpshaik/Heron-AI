# Needs checking — Group PE

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PE - five cards changed for issues #950, #1005, #1112 and #1114, and a read of the open models that answers whatever the pin - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1166](https://github.com/Ajmalpshaik/Heron-AI/issues/1166) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** one cloud session worked the rows of issues [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949),
[#950](https://github.com/Ajmalpshaik/Heron-AI/issues/950), [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005),
[#1082](https://github.com/Ajmalpshaik/Heron-AI/issues/1082), [#1112](https://github.com/Ajmalpshaik/Heron-AI/issues/1112)
and [#1114](https://github.com/Ajmalpshaik/Heron-AI/issues/1114) that need no Revit. Five cards' C# changed, so each is
**DRAFT** with its version raised until its check below runs and Ajmal PS signs (D-30); a proof already on a card is
kept there as the record:

- [`SET_VIEW_FILTER_CATEGORIES`](../../brain/fragments/set-view-filter-categories/fragment.yaml) **version 3**, was
  PROVEN v2 - `matchValue` *"has no value"* (or *"is blank"*) builds Revit's has-no-value rule, as `CREATE_VIEW_FILTER`
  has since 2026-11-01 ([row 5b-1364](../fragment-issues/section-5b-rows-1351-1375.md) item 1). Version 2 matched
  the three words as text.
- [`REPORT_CONNECTORS`](../../brain/fragments/report-connectors/fragment.yaml) **version 4**, was PROVEN v3 -
  `connectorMeasurements` (millimetres and a facing vector, one entry a number) and `connectorKinds`, keyed by element
  id and connector number (work-note item WN-4). The text outputs are unchanged.
- [`CREATE_SHEETS`](../../brain/fragments/create-sheets/fragment.yaml) **version 5**, DRAFT v4 before - with typed
  numbers, `titleBlockName` *placeholder* makes placeholder sheets (`ViewSheet.CreatePlaceholder`, work-note item
  WN-6). [Group OZ](group-oz.md) is still owed for version 4; run it first, then PE3.
- [`APPLY_VIEW_FILTER`](../../brain/fragments/apply-view-filter/fragment.yaml) **version 4** and
  [`MEASURE_DISTANCE`](../../brain/fragments/measure-distance/fragment.yaml) **version 2**, both DRAFT before - words
  only: one sentence in each named a retired card, now the live one ([row 5b-1363](../fragment-issues/section-5b-rows-1351-1375.md)
  item 4). [Group OR](group-or.md)'s OR27 is still owed for `APPLY_VIEW_FILTER` version 3.

And one change outside the cards: `revit_read` sends [`REPORT_OPEN_DOCUMENTS`](../../brain/fragments/report-open-documents/fragment.yaml)
aimed at no model, and its answer neither sets the chat's pin nor is refused by it ([row 5b-1887](../fragment-issues/row-5b-1887.md);
`mcp/server/heron_mcp_server.py`, `SESSION_READS`; `tests/test_read_door.py` section 15, red on the code before). The
card itself did not change. And `SET_MEP_JUSTIFICATION` lost the utterance *"set the mep offset"* ([row 5b-1918](../fragment-issues/row-5b-1918.md)),
measured on lexical and model here; the re-ranked backend was PE7's - [D-201](../decisions/D-201.md) (2026-11-26) removed it, and PE7 is now the PC's own `model`.

Written in a cloud container with no Revit; every fragment compiles on 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-21). **Nothing here has run in Revit.**

**The model is a scratch copy, never a working model** - a SAVED copy of Project1 on Revit 2020 for PE1 to PE6 (it
has ducts, pipes and view filters); the copy MY1 / OZ1 arranges will do for PE3. Every run below is ROLLED BACK.
Every command line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`, in PowerShell in the Heron folder; the long ones are written out under the table, by row.

| ID | Do this | Pass looks like |
|---|---|---|
| **PE0** | *Open.* Revit 2020, the scratch copy, Changes on at the ribbon. Record the name of one rule-based view filter whose categories all have Comments (if the copy has none, make one by hand in Manage, Filters and save the copy), and the ids of one round duct with both ends open, one rectangular duct and one lighting fixture with an electrical connector | The names and ids recorded here |
| **PE1** | *`SET_VIEW_FILTER_CATEGORIES` version 3 - the D-30 pair, THE NEGATIVE FIRST.* The PE1 lines below: `matchValue` *"has no value Level 1"*, then *"has no value"*, then *"equals has no value"*, each with `parameterName` Comments | Negative: refused *"... "has no value" and "is blank" take nothing after them ..."*, changed empty, the filter unchanged in Manage, Filters. Positive: changed names the filter; readBack says *"Comments has no value"* and counts the elements whose Comments is empty. Third: readBack *"Comments equals 'has no value'"* - the words matched as text. **This fails on version 2**, which read the second as equals *"has no value"* |
| **PE2** | *`REPORT_CONNECTORS` version 4 - the D-30 pair.* The PE2 line below with PE0's three ids | Round duct: `connectorMeasurements` holds `<id>/1/diameterMm` and `<id>/2/diameterMm` at its size, `xMm` `yMm` `zMm` at each end and a facing vector of length 1 out of each end; `connectorKinds` `<id>/1` reads *"DomainHvac ..., Round"*. Rectangular: `widthMm` and `heightMm`, no `diameterMm`. **Negative - the fixture's electrical connector:** a `connectorKinds` entry with *"no profile"*, its position and facing, and **no** `diameterMm`, `widthMm` or `heightMm` - never a size of 0. Compare one duct's numbers with its Properties palette; `connectorSummary` and `findings` read as version 3's |
| **PE3** | *`CREATE_SHEETS` version 5, placeholders - THE NEGATIVE FIRST, after Group OZ.* The PE3 lines below: a number already in the register with `titleBlockName` placeholder; then PH-101 and PH-102 with two names; then the same with previewOnly true | Negative: the number in numberTaken, created empty, **the sheet count unchanged**. Positive: created two ids, numberedAs PH-101 and PH-102, namedAs the two names, noTitleblock false, findings *"2 sheet(s) were made as PLACEHOLDER sheets ..."*; in a sheet list schedule both show; neither has a title block or a view. **Record** where Revit shows a placeholder in the Project Browser, if anywhere. Preview: nothing new, findings *"PREVIEW - nothing was kept. 2 of 2 number(s) would be made"* and the placeholder line with *"would be"* |
| **PE4** | *`APPLY_VIEW_FILTER` version 4's one sentence.* The PE4 line below - no filter given | refused, nothing changed. **Record** who refused: the add-in's binder (the card's `filter` is not optional), naming `filter` - then the card's own sentence cannot be reached this way and that is written here - or the card, its sentence naming CREATE_VIEW_FILTER and *"UPDATE_SAVED_SET with mode create"*, never CREATE_SELECTION_FILTER. Group OR's OR27 is the card's own pair |
| **PE5** | *`MEASURE_DISTANCE` version 2 - the pair.* Two ducts that touch end to end, then two that are 500 mm apart - the PE5 lines below | Touching: straightMm 0 and the finding naming FIND_CLASHES *"... though it counts touching as clashing too"*, never SELECT_TOUCHING. Apart: about 500 and no such sentence |
| **PE6** | *A read of the open models after the pinned one is closed* ([row 5b-1887](../fragment-issues/row-5b-1887.md)). In a chat with Heron, after the copy and a scratch family are both open: pin the chat to the family (`revit_use_this_model` with it in front), save and close the family, then ask `revit_read` for `REPORT_OPEN_DOCUMENTS` | It answers, listing the copy and not the family, with the line *"It reads every model open in this Revit, whichever one this chat is working on; the model this chat is pinned to is unchanged."* **Negative:** then `revit_read` of a read that is about one model - `REPORT_LEVEL_ELEVATIONS` - is still refused *"No open model saved at ..."* naming the closed family: the pin did not slide onto the copy (Constitution Article 12a). On version 3's server the first was refused the same way |
| **PE7** | *Routing on the owner's PC, on `model` (re-pointed 2026-11-26: this row asked for model with the re-ranker, and [D-201](../decisions/D-201.md) removed it)* ([row 5b-1918](../fragment-issues/row-5b-1918.md)). `heron_lookup` of *"set the mep offset"*, *"line these runs up on their tops"*, *"set the justification on these ducts"* *(Re-pointed again 2026-11-26: [D-203](../decisions/D-203.md) removed `model` as well, so the PC ranks on `lexical`, the one search every PC and CI share - the run owed is on `lexical`, and its search half is the same answer in any checkout of the same commit.)* | **Record** where each lands. The first must not be `SET_MEP_JUSTIFICATION`; the other two must be. On lexical and model here (2026-11-21) the first moved to `SET_ROOM_LIMITS` and `OFFSET_MEP_SECTION` - neither sets a run's offset, which is row 5b-1944's gap, not this check's |

### The command lines, by row

Each is ONE line in PowerShell; it is broken here only to be read. Put PE0's names and ids in.

**PE1** - negative, then the two positives:

```text
python mcp/client/heron_bridge_client.py fragment set-view-filter-categories --write
  --set "filterNames=<PE0's filter>" --set "parameterName=Comments" --set "matchValue=has no value Level 1"
python mcp/client/heron_bridge_client.py fragment set-view-filter-categories --write
  --set "filterNames=<PE0's filter>" --set "parameterName=Comments" --set "matchValue=has no value"
python mcp/client/heron_bridge_client.py fragment set-view-filter-categories --write
  --set "filterNames=<PE0's filter>" --set "parameterName=Comments" --set "matchValue=equals has no value"
```

**PE2:**

```text
python mcp/client/heron_bridge_client.py fragment report-connectors
```

**First SELECT the round duct, the rectangular duct and the fixture in Revit, then run it with no `--set`.** `elements` is not a typed input: the card reads the Revit selection, and a typed list of ids is never read - nothing says it was dropped (row 5b-1971). Or arrange them with a setup step: `filter-elements-by-id` with `elementIds` typed.

**PE3** - negative, positive, preview:

```text
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=<a number in the register>"
  --set "names=HERON PH TAKEN" --set "titleBlockName=placeholder"
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=PH-101,PH-102"
  --set "names=HERON PLACEHOLDER 1,HERON PLACEHOLDER 2" --set "titleBlockName=placeholder"
python mcp/client/heron_bridge_client.py fragment create-sheets --write --set "numbers=PH-101,PH-102"
  --set "names=HERON PLACEHOLDER 1,HERON PLACEHOLDER 2" --set "titleBlockName=placeholder" --set "previewOnly=true"
```

**PE4:**

```text
python mcp/client/heron_bridge_client.py fragment apply-view-filter --write --set "view=<a floor plan>"
```

**PE5** - touching, then apart. **CANNOT BE ARRANGED AS TYPED (row 5b-1970):** `first` and `second` are single-element needs, the only word that binds one is `selected`, and it means exactly one element - so two different ducts cannot be handed over yet, and `--set "first=<duct A>"` is refused. Following `selected` for both measures a duct against itself (0 mm), an answer and not a refusal. Do not run these two lines until a way to name two elements exists:

```text
python mcp/client/heron_bridge_client.py fragment measure-distance --set "first=<duct A>" --set "second=<duct B touching A>"
python mcp/client/heron_bridge_client.py fragment measure-distance --set "first=<duct A>" --set "second=<duct C 500 mm away>"
```
