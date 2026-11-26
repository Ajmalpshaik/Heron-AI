# Needs checking — Group PK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PK - eight reading tools count what they pass over (D-64, work-note item WN-7) - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1202](https://github.com/Ajmalpshaik/Heron-AI/issues/1202) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [D-64](../decisions/D-64.md) - a reading tool that goes looking and can drop a candidate declares how many it
dropped, and says so only on the empty answer. Its worklist is [row 5b-1909](../fragment-issues/row-5b-1909.md),
work-note item WN-7 of issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005); the list is
`python tools/check-revit-gate.py --list reporting`. This change takes the eight DRAFT reading cards on it whose loop
passed over something without a word and whose empty answer could then say the wrong thing - *"No view in this model
has a filter on it"* of views it never read. Each card's C# changed, so each is **DRAFT** with its version raised until
its check below runs and Ajmal PS signs (D-30); a proof already on a card is kept there as the record:

- [`REPORT_LINKED_VIEW_USAGE`](../../brain/fragments/report-linked-view-usage/fragment.yaml) **version 2** -
  `skippedPairs`: view-and-link pairs whose read threw in a view that takes graphic overrides. And a link instance
  with no override of its own in a view (Revit's documented null) now reads its link TYPE's settings, where version 1
  dropped the pair - so its *"N are By Host View"* was short and a type set By Linked View was never listed.
- [`LIST_LINKED_MODELS`](../../brain/fragments/list-linked-models/fragment.yaml) **version 3** - `skippedPlacements`:
  link placements with no link type. [Group OQ](group-oq.md) is still owed for version 2; run it first.
- [`REPORT_STRUCTURAL_CONNECTIONS`](../../brain/fragments/report-structural-connections/fragment.yaml) **version 2** -
  `unresolvedConnections`: connections that would not say what they join. [Group LD](group-ld.md) holds version 1's
  arrangement.
- [`REPORT_ROOF_SLOPES`](../../brain/fragments/report-roof-slopes/fragment.yaml) **version 2** - `skippedRoofs`, and
  the empty answer says how many roofs were passed over instead of *"this model has no roof by footprint or by
  extrusion"*. [Group PG](group-pg.md) is still owed for version 1; run it first.
- [`REPORT_DIVIDED_SURFACES`](../../brain/fragments/report-divided-surfaces/fragment.yaml) **version 4** -
  `missingNodes`, and a node list the 50 cap cut says how many more it left out. [Group PB](group-pb.md) (PB1, PB2) is
  still owed for version 3; run it first.
- [`REPORT_FABRICATION_SETUP`](../../brain/fragments/report-fabrication-setup/fragment.yaml) **version 2** -
  `skippedButtons`: palette buttons Revit gave back as nothing. [Group LO](group-lo.md) holds version 1's arrangement.
- [`REPORT_CATEGORY_OVERRIDES`](../../brain/fragments/report-category-overrides/fragment.yaml) **version 5** -
  `skippedCategories`: categories drawn in the view whose overrides Revit would not give. [Group OR](group-or.md)'s
  OR16 is still owed for version 4; run it first.
- [`REPORT_VIEW_FILTERS`](../../brain/fragments/report-view-filters/fragment.yaml) **version 6** - `skippedViews`, asked
  for every view (`*`). [Group OR](group-or.md)'s OR13 is still owed for version 5; run it first.

**What every check here looks for, the same rule each time:** the new count is always in the answer; its sentence is
there **only** on the empty answer, and only when the count is above 0; and an answer that lists something reads
exactly as the version before - the two exceptions are `REPORT_DIVIDED_SURFACES`' cap line and
`REPORT_LINKED_VIEW_USAGE`'s pairs read through their link type (PK1). **Write each count down
even when it is 0** - nobody knows yet how often Revit refuses these reads on an ordinary model, and that number is
what this group is for. Arranging a model where Revit refuses one is not asked: no one knows how to make it do so.

Written in a cloud container with no Revit; every fragment compiles on 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-21). No routing words changed. **Nothing here has run in Revit.**

**The model is a scratch copy, never a working model** - the copy each card's earlier group arranged, named in the
table. Every run is a read: nothing changes and the undo list gains no entry. Every command line with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`, in PowerShell
in the Heron folder; they are written out under the table, by row.

| ID | Do this | Pass looks like |
|---|---|---|
| **PK1** | *`REPORT_LINKED_VIEW_USAGE` version 2 - Revit 2024 (the card's first release), a saved copy of Snowdon Towers Sample HVAC (6 links). THE NEGATIVE FIRST.* The PK1 lines below: `linkName` *No Such Link*; then `linkName` empty; then a saved copy of Project1 with one model linked into it by hand (Insert > Link Revit) and every view left By Host View | Negative: `linkCount` 0, `skippedPairs` 0, the one finding *"No link's name starts "No Such Link"."* and nothing else. Snowdon: the views version 1 listed, listed the same, and **no** "passed over" sentence whenever `notByHost` is above 0; *"N are By Host View"* written down beside version 1's - it may be larger, by the pairs version 1 dropped; `skippedPairs` written down (0 expected). Project1 with a link, every view By Host View: `notByHost` 0, `usage` empty, `skippedPairs` 0 and **no** "passed over" sentence - an instance with no override of its own is read through its type, never counted as passed over. Then, in one plan's Revit Links tab, set the link's FILE row (not the instance under it) By Linked View, if the tab lets the two be set apart: that plan is listed, read from the type - **record** whether the tab let them be set apart |
| **PK2** | *`LIST_LINKED_MODELS` version 3 - after Group OQ, the same two models as its proof (Revit 2024).* The PK2 line on Project1 (no link), then on the Snowdon copy | Negative - Project1: `summary` empty, `elements` empty, `skippedPlacements` 0 and no "link placement(s) are in this model but Revit gives no link type" sentence. Snowdon: six files as version 2 lists them, `skippedPlacements` 0, no such sentence |
| **PK3** | *`REPORT_STRUCTURAL_CONNECTIONS` version 2 - Group LD's scratch project and arrangement (C1, C2, B1, W1, CN1, CN2). THE NEGATIVE FIRST. `typeCount` counts every connection type in the model and is `role: accounting` since row 5b-1968, so a batch judges the W1 negative on `connectionCount`.* The PK3 lines: W1 selected with `show` connections; then nothing selected | Negative - W1: `connectionCount` 0, `notConnected` names W1, `unresolvedConnections` written down, and the *"would not say what they join"* sentence there exactly when it is above 0 (0 expected: CN1 and CN2 each say what they join). Nothing selected: the version 1 answer and no such sentence |
| **PK4** | *`REPORT_ROOF_SLOPES` version 2 - after Group PG, on its copy roofjoin-oy. THE NEGATIVE FIRST.* The PK4 lines: only a wall selected (**`roofjoin-oy` has no wall - draw one in the copy first, row 5b-1973**); then the main roof; then one roof by face (Massing & Site > Roof by Face on a mass face, made by hand in the copy and saved) | Negative - the wall: `skippedRoofs` 0 and the first finding exactly version 1's *"No roof by footprint or by extrusion is among the elements selected ..."*. Main roof: PG2's answer and `skippedRoofs` 0. Roof by face: `roofsRead` 0, `skippedRoofs` 1, the first finding *"No roof's slope was read: 1 roof(s) among the elements selected were passed over, each named below - not the same as there being no roof"*, and the roof named as one this does not read |
| **PK5** | *`REPORT_DIVIDED_SURFACES` version 4 - after Group PB, on PB1's mass box (its top face divided uGrid 20, vGrid 4 - 7 and 23 gridlines in row 5b-303, so more than 50 nodes) with one model line in it divided by hand (Divide Path, Fixed Number 60). THE NEGATIVE FIRST.* The PK5 lines: `withNodes` false, then true | Negative - false: `missingNodes` 0 and no node list. True: the surface's line as version 3's, then *"the list stops at 50: N more node(s) not listed"* - check N is the U-gridline count times the V-gridline count less 50 and less the line's *"node(s) gave no point"*; the path's line lists 50 nodes and the same words, N written down beside its node count; a surface or path of 50 nodes or fewer has no such words; `missingNodes` the sum of every surface line's *"node(s) gave no point"* |
| **PK6** | *`REPORT_FABRICATION_SETUP` version 2 - Group LO's scratch project `fabrication-lo` (LO1: one ductwork and one pipework service loaded). THE NEGATIVE FIRST.* The PK6 lines (**`configurationName` is filled whatever `service` says, so a batch reads this negative NEG NOT EMPTY - the D-30 negative is LO1's second project with no configuration, as `negative-in`; row 5b-1968**): `show` buttons with `service` ZZNOTASERVICE; then `show` buttons with one loaded service | Negative: `loadedServices` 0, version 1's *"No loaded service has a name or abbreviation matching ..."*, `skippedButtons` 0 and no *"could not be read - Revit gave each back as nothing"* sentence. Positive: each palette's buttons named as version 1 names them, `skippedButtons` written down, no such sentence while any button is named |
| **PK7** | *`REPORT_CATEGORY_OVERRIDES` version 5 - Revit 2020, after OR16, a scratch copy of P1102 MEP. THE NEGATIVE FIRST.* The PK7 lines: a plan with nothing overridden at category level (**a by-hand reading, not an empty D-30 leg: `hiddenCategories` is a result filled on every view and version 4 was proved by D-53 tracking, row 5b-1969**); then the same plan after one category is given a halftone override by hand in Visibility/Graphics (saved) | Negative - nothing overridden: version 4's plain zero and `overrideList` empty; `skippedCategories` written down; the *"would not give their overrides"* sentence there exactly when it is above 0. Positive: `overrideList` and `findings` as version 4's and no such sentence |
| **PK8** | *`REPORT_VIEW_FILTERS` version 6 - after OR13. THE NEGATIVE FIRST.* The PK8 lines: `viewName` `*` on a new project with no filter; then `viewName` `*` on OR13's copy of P1102 MEP, given filters | Negative - the new project: `filterSettings` empty, `skippedViews` 0 - the project and system browsers throw here, take no graphic override and are not counted - and the finding exactly version 5's *"No view in this model has a filter on it."* A *"passed over because Revit would not give their filter list"* sentence, or `skippedViews` above 0, is written down with the views it came from. P1102 MEP: every view with a filter listed as version 5 lists them, and no "passed over" sentence |

### The command lines, by row

Each is ONE line in PowerShell; it is broken here only to be read. Put the ids and names in. If the binder refuses
`linkName=` as an empty value, record the refusal in PK1 and ask the same in a chat with Heron instead - *"which views
show the links by linked view"*.

**PK1** - negative, Snowdon, Project1 with a link:

```text
python mcp/client/heron_bridge_client.py fragment report-linked-view-usage --set "linkName=No Such Link"
python mcp/client/heron_bridge_client.py fragment report-linked-view-usage --set "linkName="
python mcp/client/heron_bridge_client.py fragment report-linked-view-usage --set "linkName="
```

**PK2** - Project1, then Snowdon:

```text
python mcp/client/heron_bridge_client.py fragment list-linked-models
python mcp/client/heron_bridge_client.py fragment list-linked-models
```

**PK3** - W1, then nothing selected:

```text
python mcp/client/heron_bridge_client.py fragment report-structural-connections --set "elements=<W1>"
  --set "show=connections"
python mcp/client/heron_bridge_client.py fragment report-structural-connections
```

**PK4** - the wall, the main roof, the roof by face:

```text
python mcp/client/heron_bridge_client.py fragment report-roof-slopes --set "elements=<the wall>"
python mcp/client/heron_bridge_client.py fragment report-roof-slopes --set "elements=<the main roof>"
python mcp/client/heron_bridge_client.py fragment report-roof-slopes --set "elements=<the roof by face>"
```

**PK5** - without nodes, then with:

```text
python mcp/client/heron_bridge_client.py fragment report-divided-surfaces --set "withNodes=false"
python mcp/client/heron_bridge_client.py fragment report-divided-surfaces --set "withNodes=true"
```

**PK6** - no such service, then one service:

```text
python mcp/client/heron_bridge_client.py fragment report-fabrication-setup --set "show=buttons"
  --set "service=ZZNOTASERVICE"
python mcp/client/heron_bridge_client.py fragment report-fabrication-setup --set "show=buttons"
  --set "service=<one loaded service>"
```

**PK7** - nothing overridden, then one category overridden:

```text
python mcp/client/heron_bridge_client.py fragment report-category-overrides --set "view=<the plan>"
python mcp/client/heron_bridge_client.py fragment report-category-overrides --set "view=<the plan>"
```

**PK8** - the new project, then P1102 MEP:

```text
python mcp/client/heron_bridge_client.py fragment report-view-filters --set "viewName=*"
python mcp/client/heron_bridge_client.py fragment report-view-filters --set "viewName=*"
```
