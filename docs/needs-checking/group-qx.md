# Needs checking — Group QX

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QX - MEASURE_DISTANCE version 3 and MEASURE_AVAILABLE_FALL version 2: the two elements selected in Revit, or two ids typed - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1443](https://github.com/Ajmalpshaik/Heron-AI/issues/1443) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** neither card could be handed two different elements ([row 5b-2006](../fragment-issues/row-5b-2006.md),
issue [#1308](https://github.com/Ajmalpshaik/Heron-AI/issues/1308)). Each had two single-element needs
(`first`/`second`, `upstream`/`downstream`), and the add-in's one word for those, `selected`, means exactly one
element and binds it to both - so every call measured an element against itself. The owner, 2026-11-26: *"select
both in revit or type id which is the best one as per job you can decide and do it"*. Both ways now, in the shape
[`REPORT_PARAMETER_INVENTORY`](../../brain/fragments/report-parameter-inventory/fragment.yaml) takes ids in:
`elements` is the two selected in Revit (or handed on by a step before), `elementIds` is two ids typed as Revit
shows them, and **typed ids decide**. Exactly two either way - one, three or none is refused, saying how many came.
A selection has no order, so `MEASURE_AVAILABLE_FALL` takes the higher of two selected as upstream and says so;
typed ids keep their order, upstream first, and only they can report a rise. The cards'
[`measure-distance/tests/cases.yaml`](../../brain/fragments/measure-distance/tests/cases.yaml) and
[`measure-available-fall/tests/cases.yaml`](../../brain/fragments/measure-available-fall/tests/cases.yaml) hold
every case; these rows are the run.

**What is done and what is not.** Compiles for 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-26). The ids
are matched as TEXT against each element's own `Id.ToString()`, as `REPORT_PARAMETER_INVENTORY` matches them, so no
`ElementId` is built from a number. **Never run in Revit.** Both cards were DRAFT before and are DRAFT now; this
group is what proves them (D-30). [Group PE](group-pe.md)'s PE5 asked `MEASURE_DISTANCE` version 2's touching
sentence and could not be arranged - QX3 asks the same sentence of version 3.

**The model: scratch, never a working one** - a saved copy of Project1 on Revit 2020 first, then QX8 on 2024. Every
run is a read; nothing changes. Every command line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when
two Revits are connected, and `< /dev/null`, in PowerShell in the Heron folder.

| ID | Do this | Pass looks like |
|---|---|---|
| **QX0** | *Arrange, and write the ids here.* Three ducts on one level: `A`; `B` touching `A` end to end; `C` parallel to `A`, 500 mm clear of it. Two generic models (or a gully and a manhole if the copy has them) on one level: `UP` with its lowest point 300 mm above `DOWN`'s, their centres 24000 mm apart on plan | Five ids written here |
| **QX1** | *`MEASURE_DISTANCE` - selection, THE NEGATIVE FIRST.* Select `A`, `B` and `C`; run with no `--set`. Then select `A` alone and run again | Both: `straightMm`, `horizontalMm` and `verticalMm` 0, and the finding *"3 element(s) are selected (or handed on by the step before), and this measures between exactly TWO ..."* (*"1 element(s)"* the second time). Nothing selected at all: *"Nothing is selected and no element ids were typed ..."* |
| **QX2** | *`MEASURE_DISTANCE` - typed ids, the negative.* `--set "elementIds=<A>"`, then `--set "elementIds=<A>, 999999999"` | Every number 0. First: *"1 element id(s) were typed, and this measures between exactly TWO ..."*. Second: *"No element in '<copy>' has the id 999999999 ..."* |
| **QX3** | *`MEASURE_DISTANCE` - selection, the positive.* Select `A` and `C`, run with no `--set`; then select `A` and `B` | `A`/`C`: `straightMm` about 500 (compare Revit's tape measure), no touching sentence. `A`/`B`: `straightMm` 0 and the finding naming FIND_CLASHES *"... though it counts touching as clashing too"*, never SELECT_TOUCHING - PE5's check, on version 3 |
| **QX4** | *`MEASURE_DISTANCE` - typed ids, the positive.* Select `B` alone, then `--set "elementIds=<A>, <C>"` | The same `straightMm` as QX3's `A`/`C`, and *"The 1 element(s) selected were NOT used - ids were typed, and typed ids decide"*. A number of 0 here means the selection won, which is the defect |
| **QX5** | *`MEASURE_AVAILABLE_FALL` - both negatives.* Select `A`, `B` and `C`, run with no `--set`; then `--set "elementIds=<UP>"`; then `--set "elementIds=<UP>, 999999999"` | Each: `fallMm`, `runMm` and `gradientOneIn` 0. *"3 element(s) are selected ... exactly TWO points"*; *"1 element id(s) were typed ..."*; *"No element in '<copy>' has the id 999999999 ..."* |
| **QX6** | *`MEASURE_AVAILABLE_FALL` - selection, the positive.* Select `UP` and `DOWN`, run with no `--set`; clear the selection, select them in the other order, run again | Both runs the same: `fallMm` 300, `runMm` about 24000, `gradientOneIn` about 80, and the finding *"Taken from the selection, which has no order: the higher of the two by its lowest point, ... (<UP>), is taken as upstream ..."* |
| **QX7** | *`MEASURE_AVAILABLE_FALL` - typed ids, the positive and the rise.* Select `A` alone, then `--set "elementIds=<UP>, <DOWN>"`; then `--set "elementIds=<DOWN>, <UP>"` | First: 1 in about 80, no sentence about the selection's order, and *"The 1 element(s) selected were NOT used ..."*. Second: *"The downstream point is 300 mm HIGHER than the upstream one ... That is a rise"*, `gradientOneIn` 0 |
| **QX8** | *Revit 2024 - ids are 64-bit there.* A saved copy on Revit 2024 arranged as QX0; run QX2 and QX4 | The same answers; the ids printed and matched in full |

### The command lines

Each is ONE line in PowerShell. Put QX0's ids in. A selection run is the same line with no `--set`.

```text
python mcp/client/heron_bridge_client.py fragment measure-distance
python mcp/client/heron_bridge_client.py fragment measure-distance --set "elementIds=<A>"
python mcp/client/heron_bridge_client.py fragment measure-distance --set "elementIds=<A>, 999999999"
python mcp/client/heron_bridge_client.py fragment measure-distance --set "elementIds=<A>, <C>"
python mcp/client/heron_bridge_client.py fragment measure-available-fall
python mcp/client/heron_bridge_client.py fragment measure-available-fall --set "elementIds=<UP>"
python mcp/client/heron_bridge_client.py fragment measure-available-fall --set "elementIds=<UP>, 999999999"
python mcp/client/heron_bridge_client.py fragment measure-available-fall --set "elementIds=<UP>, <DOWN>"
python mcp/client/heron_bridge_client.py fragment measure-available-fall --set "elementIds=<DOWN>, <UP>"
```
