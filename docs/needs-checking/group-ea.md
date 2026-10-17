# Needs checking — Group EA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-14 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EA - the saved View/Sheet Sets read back: `LIST_SHEET_SETS`, every set's sheets and views, and the empty ones (2026-10-14)

Asked by Ajmal PS on 2026-10-14 - *"how many sets are there"* - on his Revit 2020 job model
4355-BHVD-3D-21S10-BL001A, where Heron had nothing that read a saved set. [Row 5b-465](../fragment-issues/section-5b-rows-401-425.md)
has the measurements.

**What changed.** New: [`LIST_SHEET_SETS`](../../brain/fragments/list-sheet-sets/fragment.yaml) (FRG-SHT-032,
READ, DRAFT). It needs only the document and leaves `sheetSets` - every saved set as one string: its name,
how many sheets and views it holds, every sheet as "number - name" and every view as "name (view type)",
and an empty set marked EMPTY - with `setCount`, `emptySets` and `findings`. The print dialog's in-session
set is not saved in the model and is not counted. `LIST_SHEETS` and `MANAGE_SHEET_SETS` are untouched
below their routing tables, so both stay PROVEN.

**2026-10-17: both sheet-set cards are version 2** ([Group FA](group-fa.md), [row 5b-545](../fragment-issues/section-5b-rows-401-425.md)).
`MANAGE_SHEET_SETS` is DRAFT again - it adds sheets to and takes them out of an existing set, and puts the
print range back. This card now also flags two sets holding the same sheets and views, in a new `sameSets`:
EA1 below proves version 2, and is run after FA5 and read with it.

**Declared 2020 to 2027 and compiles on all eight** (`tools/check-fragments-compile.py`). Nothing has been
run in Revit through this card. A scratch read with the same collector and member walk answered on the
owner's Revit 2020 that day - six sets, one empty - and that is the observation that asked for the card,
not its proof.

| # | Run | Look for |
|---|---|---|
| **EA1** | Revit 2020, BL001A and a new empty project "Heron Sheet Sets Empty" open together (make the second first - the job says how): `python tools/batch-prove.py tools/jobs/list-sheet-sets-bl001a-2026-10-14.yaml`, then read [`brain/proof-drafts/list-sheet-sets.yaml`](../../brain/proof-drafts/README.md) and `python brain/heron_validate.py accept list-sheet-sets --by "Ajmal PS"` and set `heron-status: PROVEN` | **Count BL001A's sets in File - Print - Settings first - the number moves:** six on 2026-10-14 by a scratch read, and a seventh made there with `MANAGE_SHEET_SETS` the same day. positive: `setCount` equal to that count, every set in name order with its sheets and views named, each empty set marked EMPTY and named in `emptySets`; negative: `setCount 0`, nothing in `sheetSets`, the finding that no set is saved. Then in the same dialog: every set's ticked sheets and views are the ones named |
| **EA2** | Revit 2024 or later: EA1's positive on any model with a saved set | the same answer on a release where `ElementId` is 64-bit and a set has an order of its own (`OrderedViewList`, 2023+), which this card does not read |
| **EA3** | A set that holds sheets AND views, made in the Print dialog on a scratch copy | one row with both counts; every sheet counted once, as a sheet, never again as a view |
