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

**Declared 2020 to 2027 and compiles on all eight** (`tools/check-fragments-compile.py`). Nothing has been
run in Revit through this card. A scratch read with the same collector and member walk answered on the
owner's Revit 2020 that day - six sets, one empty - and that is the observation that asked for the card,
not its proof.

| # | Run | Look for |
|---|---|---|
| **EA1** | Revit 2020, BL001A and a new empty project "Heron Sheet Sets Empty" open together (make the second first - the job says how): `python tools/batch-prove.py tools/jobs/list-sheet-sets-bl001a-2026-10-14.yaml`, then read [`brain/proof-drafts/list-sheet-sets.yaml`](../../brain/proof-drafts/README.md) and `python brain/heron_validate.py accept list-sheet-sets --by "Ajmal PS"` and set `heron-status: PROVEN` | **Count BL001A's sets in File - Print - Settings first - the number moves:** six on 2026-10-14 by a scratch read, and a seventh made there with `MANAGE_SHEET_SETS` the same day. positive: `setCount` equal to that count, every set in name order with its sheets and views named, each empty set marked EMPTY and named in `emptySets`; negative: `setCount 0`, nothing in `sheetSets`, the finding that no set is saved. Then in the same dialog: every set's ticked sheets and views are the ones named |
| **EA2** | Revit 2024 or later: EA1's positive on any model with a saved set | the same answer on a release where `ElementId` is 64-bit and a set has an order of its own (`OrderedViewList`, 2023+), which this card does not read |
| **EA3** | A set that holds sheets AND views, made in the Print dialog on a scratch copy | one row with both counts; every sheet counted once, as a sheet, never again as a view |

**2026-10-17 - EA1 run on Revit 2020 (session 51568): `PASS`. Ajmal PS's signature is owed.** Not on BL001A:
Ajmal PS chose the scratch "Project1" he had open - a new project from Revit 2020's mechanical template, never
saved - and saved the set there himself in File - Print - Print... - Selected views/sheets - Select...: "HERON
PROOF SET", four elevation views; a second set, "Set 1", holding the same four views, was saved in the same visit.
[`tools/jobs/list-sheet-sets-project1-2026-10-17.yaml`](../../tools/jobs/list-sheet-sets-project1-2026-10-17.yaml):
positive on Project1 `setCount 2`, both sets in name order, each *"0 sheets, 4 views"* with every view as *"name
(Elevation)"*; negative on "EQ-2020 rme_advanced_sample_project" (another chat's scratch copy of the Revit 2020 MEP
sample, idle in the same Revit) `setCount 0` and *"No View/Sheet Set is saved"*. By eye in the same dialog Ajmal
PS read exactly those two names - the draft's second route. **Not covered:** a sheet in a set (Project1 has none),
an EMPTY set, and BL001A's own seven. **If [PR #574](https://github.com/Ajmalpshaik/Heron-AI/pull/574) merges, this
proof goes stale** - it changes this card's code; Project1's two sets hold the same four views, the case its
`sameSets` flags. Signing, from the checkout that holds the draft:
`python brain/heron_validate.py accept list-sheet-sets --by "Ajmal PS"`, then `heron-status: PROVEN` on his word.
