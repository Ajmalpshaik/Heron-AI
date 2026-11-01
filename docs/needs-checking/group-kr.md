# Needs checking — Group KR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KR - change Revit's rebar numbers: `EDIT_REBAR_NUMBERING` (2026-11-01)

Asked for in the modeller's words: *"renumber the rebar in partition P1"*, *"move these bars to partition
Level 3 slab"*, *"start rebar numbering at 100 in this partition"*, *"compact the rebar numbers"* - Revit's
Structure > Reinforcement > Reinforcement Numbers dialog. No card changed Revit's rebar numbering.
`RENUMBER_SEQUENTIAL` (PROVEN) writes a running sequence into a parameter the modeller names; Revit keeps
rebar numbers itself, per partition, so this is a different job and that card is untouched.

| What | Where | What it does |
|---|---|---|
| `EDIT_REBAR_NUMBERING` (MODIFY) | [`brain/fragments/edit-rebar-numbering`](../../brain/fragments/edit-rebar-numbering/fragment.yaml) | One action on **named** partitions of rebar, couplers or fabric sheets: **start** (the partition's numbers start at `startAt`), **compact** (its gaps closed; `all` closes each partition's own gaps), **renumber** (gaps closed, then started at `startAt` when given), **change** (`fromNumber` becomes `toNumber`, refused when `toNumber` is in use), **rename** (to a `toPartition` not yet in use), **merge** (the partitions in `partition` into `toPartition`), **move** (the selected bars into `partition`). Each partition's numbers **before and after**; `changed` counts elements whose partition or number differs afterwards, read off the model. A partition holding an element **another user owns** is skipped and the element and owner named; a move skips only the bars owned. An unknown partition is refused with the names the model has |

**The write half of a pair.** Every question about rebar numbers is answered by `REPORT_REBAR_NUMBERING`
([Group KQ](group-kq.md)), so none reaches this write; *"this partition"* is read there first.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `NumberingSchema.ShiftNumbers`, `RemoveGaps`, `ChangeNumber`,
`MoveSequence`, `AppendSequence`, `MergeSequences`, `AssignElementsToSequence`, `IsValidPartitionName`,
`MaximumStartingNumber`, `GetNumbers` and `GetNumberingSequences` are present 2020-2027; **Revit 2027 marks
the schema-type route, `MoveSequence`, `AppendSequence`, `MergeSequences` and `AssignElementsToSequence`
obsolete** and still ships them. **Not known, and not guessed:** whether `ShiftNumbers` makes `startAt` the
partition's lowest number, and what `ChangeNumber` does to a number already in use - the card refuses the
second rather than find out on a model. **Nothing here has run in Revit.** The card is DRAFT.

**Routing.** Measured through `heron_retrieve.find` on two fresh scratch stores - the tree at `2795ce4` without the two rebar numbering cards, and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence in the library: *"what rebar numbers are in partition B2"* reaches `REPORT_REBAR_NUMBERING` with and without the host's question flag (`LIST_REBAR_TYPES` before); the four change sentences reach `EDIT_REBAR_NUMBERING` without it and `REPORT_REBAR_NUMBERING` with it (writes and reads elsewhere before). Without the flag two other sentences moved: *"change this one mullion"* - declared since on `CHANGE_ELEMENT_TYPE`, which three routing tables send it to ([row 5b-1374](../fragment-issues/section-5b-rows-401-425.md), FIXED) - and *"put the numbers in a chart"*, which no card claims, from `FIND_SHEETS` to `REPORT_REBAR_NUMBERING`, a read for a read. With the flag only sentences writes declare moved, from one read to another. *"rename the selected elements"* (`tests/test_routing_phrases.py`) reached the write on the first wording and reaches `EDIT_TEXT_VALUES` on this one.

**The model - [Group KQ](group-kq.md)'s scratch copy in Revit 2020** (KQ1), never a working model. Every run
below is rolled back except KR7's one run. The job file is
[`tools/jobs/edit-rebar-numbering-kr-2026-11-01.yaml`](../../tools/jobs/edit-rebar-numbering-kr-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`. Read the Reinforcement Numbers
dialog, or run `REPORT_REBAR_NUMBERING`, before and after each row - the second route.

| # | Run | Look for |
|---|---|---|
| **KR1** | *Arrange.* [Group KQ](group-kq.md)'s KQ1 model, saved | P1 has a gap, B2 none, as KQ1 wrote down |
| **KR2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, rolled back | Negative, compact **B2**: `changed` 0, `before` and `after` the same for B2. Positive, compact **P1**: `changed` above 0, `before` with the gap, `after` without. P1's gap is back afterwards - rolled back |
| **KR3** | *start.* `validate --write` with action start, partition B2, startAt 100; then partition all, startAt 100 | B2's numbers start at 100, as many numbers as before, P1 unchanged - **record whether 100 is the lowest number** (`ShiftNumbers`). `all`: refused, *"all" is taken by compact alone*, `changed` 0 |
| **KR4** | *change.* action change, partition P1, fromNumber 9 and toNumber a free number; then toNumber a number in use | The bars numbered 9 read the new number; in use: refused naming P1's numbers, `changed` 0 |
| **KR5** | *rename and merge.* action rename, partition P1, toPartition P1A; then toPartition B2; then action merge, partition P1, toPartition B2 | P1 is called P1A; into B2 refused - *ask for merge*; the merge leaves one partition B2 holding C1's and B1's bars, numbered as Revit numbers an append |
| **KR6** | *move.* Select C1's tie set; action move, partition "Level 3 slab" | Its Partition reads *Level 3 slab* and it has a number there; P1 and *Level 3 slab* before and after. Then select a column: refused, not numbered reinforcement |
| **KR7** | *The one run* (`try-once`, the modeller asks) - action compact, partition P1, the Reinforcement Numbers dialog open beside it | The modeller chooses Keep or Discard; Keep is the positive half of the proof (D-120), its negative KR2's, rolled back |
| **KR8** | *Worksharing.* A workshared copy with a second user owning one of B1's bars; compact B2 | Nothing renumbered, the bar and its owner named in `skipped`; ownership never taken |
| **KR9** | *Revit 2026 or 2027.* KR2 and KR6 again on a copy opened there | The same. 2027 marks these calls obsolete; a refusal there is a finding against this card, never a pass |
