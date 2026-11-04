# Needs checking — Group KQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KQ - which rebar numbers each partition uses: `REPORT_REBAR_NUMBERING` (2026-11-01)

Asked for in the modeller's words: *"what rebar numbers are in partition B2"* - Revit's rebar numbering,
Structure > Reinforcement > Reinforcement Numbers, where rebar, couplers and fabric sheets are numbered
in **partitions** (the Partition parameter on each bar). No card read it: a grep of `brain/fragments` for
`NumberingSchema` and `NUMBER_PARTITION_PARAM` that day found nothing. `RENUMBER_SEQUENTIAL` writes a
running sequence into a parameter the modeller names and knows nothing of Revit's own rebar numbering.

| What | Where | What it does |
|---|---|---|
| `REPORT_REBAR_NUMBERING` (READ) | [`brain/fragments/report-rebar-numbering`](../../brain/fragments/report-rebar-numbering/fragment.yaml) | Lists every numbering **partition** for rebar, couplers and fabric sheets - or the one asked for, by its exact name - with the numbers it uses as Revit's own ranges, the lowest and highest, the **gaps**, and how many elements carry them (`report`, `partitions`, `numberedElements`); names each **selected** bar's partition and number (`selected`), and on a workshared model the bars in those partitions **another user owns** (`ownedByOthers`). A name the model does not have is answered with the names it has - never a near one. Opens no transaction |

**The read half of a pair** - every question about rebar numbers is answered here, so none reaches the
write that changes them.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `NumberingSchema.GetNumberingSchema(Document,
NumberingSchemaType)`, `NumberingSchemaTypes.StructuralNumberingSchemas.Rebar`, `RebarCoupler` and
`ReinforcementFabric`, `GetNumberingSequences`, `GetNumbers(string)`, `NumberingParameterId`,
`IntegerRange.Low` and `High`, `BuiltInParameter.NUMBER_PARTITION_PARAM` and
`WorksharingUtils.GetCheckoutStatus(Document, ElementId, out string)` are present 2020-2027; **Revit 2027
marks the schema-type route obsolete** in favour of a schema name, and still ships it. **Nothing here has
run in Revit.** The card is DRAFT.

**Routing.** Measured through `heron_retrieve.find` on two fresh scratch stores - the tree at `2795ce4` without the two rebar numbering cards, and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence in the library: *"what rebar numbers are in partition B2"* reaches `REPORT_REBAR_NUMBERING` with and without the host's question flag (`LIST_REBAR_TYPES` before); the four change sentences reach `EDIT_REBAR_NUMBERING` without it and `REPORT_REBAR_NUMBERING` with it (writes and reads elsewhere before). Without the flag two other sentences moved: *"change this one mullion"* - declared since on `CHANGE_ELEMENT_TYPE`, which three routing tables send it to ([row 5b-1374](../fragment-issues/section-5b-rows-401-425.md), FIXED) - and *"put the numbers in a chart"*, which no card claims, from `FIND_SHEETS` to `REPORT_REBAR_NUMBERING`, a read for a read. With the flag only sentences writes declare moved, from one read to another. *"rename the selected elements"* (`tests/test_routing_phrases.py`) reached the write on the first wording and reaches `EDIT_TEXT_VALUES` on this one.

**The model - a scratch copy of [Group HY](group-hy.md)'s project in Revit 2020**, never a working model.
Nothing below changes the model. The job file is
[`tools/jobs/report-rebar-numbering-kq-2026-11-01.yaml`](../../tools/jobs/report-rebar-numbering-kq-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KQ1** | *Arrange.* A copy of the rebar-hy project after HY6 and HY8 are kept, saved as `rebar-numbering-kq`. Give C1's three rebar sets Partition **P1** and B1's three sets Partition **B2** (Properties > Partition). In Structure > Reinforcement > Reinforcement Numbers, partition P1, change one number to **9**. Write down what the dialog shows for P1 and B2 | P1 has a gap; B2 has none |
| **KQ2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, partition **Z9**: `partitions` 0, `report` empty, findings *No partition is named "Z9" in this model. Its partitions are: "B2" (rebar), "P1" (rebar).* Positive, partition **B2**: `partitions` 1, `numberedElements` 3, B2's numbers, lowest and highest as KQ1 wrote down, no gaps; P1 nowhere in it |
| **KQ3** | *The second route.* The card with partition left out, and with partition **b2** | All: P1 with its gap named and B2, as the dialog shows them - and a Structural Rebar schedule with Partition and Rebar Number gives the same numbers per partition. **b2**: `partitions` 0, the finding naming "B2" as differing only in capitals |
| **KQ4** | *The selection.* Select C1's tie set, the card with partition left out | `selected` names it in partition "P1" with the Rebar Number its Properties show |
| **KQ5** | *Revit 2026 or 2027.* KQ1 and KQ2 again on a copy opened there | The same answers. 2027 marks the schema-type route obsolete; a refusal there is a finding against this card, never a pass |
