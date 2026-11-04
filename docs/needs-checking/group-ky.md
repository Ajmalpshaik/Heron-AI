# Needs checking — Group KY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KY - add or remove sizes in a duct size list, a pipe segment, a conduit standard or the cable tray sizes: `EDIT_MEP_SIZES` (2026-11-01)

Asked for in the modeller's words: *"add 315 mm to the round duct sizes"*, *"add DN 65 to the steel pipe
segment"*, *"remove 125 mm from the duct sizes"* and *"add a 32 mm conduit size"* - Revit's Manage > MEP
Settings > Mechanical Settings > Duct Settings and Pipe Settings > Segments and Sizes, and Electrical
Settings > Conduit Settings > Size and Cable Tray Settings > Size. No card added or removed a size in a
list that exists: a grep of `brain/fragments` that day for `DuctSizeSettings`, `ConduitSizeSettings`,
`CableTraySizes` and `RemoveSize` found nothing. `CREATE_PIPE_SEGMENT` (PROVEN) makes a **new** segment and
adds sizes only as it makes it; `SET_MEP_SIZE` and `AUTO_SIZE_MEP` resize ducts and pipes in the model -
look-alikes, no card edited.

| What | Where | What it does |
|---|---|---|
| `EDIT_MEP_SIZES` (MODIFY) | [`brain/fragments/edit-mep-sizes`](../../brain/fragments/edit-mep-sizes/fragment.yaml) | Adds sizes to, or removes sizes from, **one** list - the round, rectangular or oval duct sizes, **an existing pipe segment's** sizes, **one conduit standard's** sizes, or the cable tray sizes - in mm (`/ 304.8` to Revit's feet). A size already there is **skipped and named**; a size to remove that is not there, skipped and named. A pipe size's outside and inside diameter and a conduit size's bend radius are **never guessed** - missing, refused - and one such size is added per run. **A size that ducts or flex ducts of that shape, pipes on that segment, conduits of that standard or cable trays still carry is not removed** - refused with the count. A segment or standard is taken by exact name, or by a part only one name holds; otherwise refused with the names. The list before and after (`before`, `after`, `added`, `removed`, `skipped`, `refused`), counted from the list read back. No other list is touched |

**Why one write for four dialogs.** The job is one - put a size in a list or take one out - and the
contract is one but for the outside and inside diameter (and a conduit's bend radius) an added pipe or
conduit size needs; those three are optional inputs, refused by name when needed and missing. Every
question is answered by `REPORT_DUCT_CONDUIT_SIZES` ([Group KX](group-kx.md)) or, for a pipe segment,
`REPORT_PIPE_SEGMENTS`.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `DuctSizeSettings.AddSize(DuctShape, MEPSize)` and `RemoveSize(DuctShape,
double)`, `Segment.AddSize(MEPSize)` and `RemoveSize(double)`, `ConduitSizeSettings.AddSize(string,
ConduitSize)` and `RemoveSize(string, double)`, `CableTraySizes.AddSize(MEPSize)` and
`RemoveSize(MEPSize)`, the `MEPSize` and `ConduitSize` constructors, `Pipe.PipeSegment`,
`MEPCurveType.Shape` and every parameter it reads are present 2020-2027 with the same signatures. **Nothing
here has run in Revit.** The card is DRAFT. **Not known without Revit:** what Revit itself does when
`RemoveSize` is handed a size still in use - refuses, or removes it and leaves the elements as they are
(the shipped assemblies carry no remarks, and no Revit API documentation was reachable from the session).
The card does not depend on the answer for ducts, pipes, conduits and trays - it refuses first - but it
does not search **fittings**, so KY3 also removes a size only a fitting carries and records what Revit
does. Whether a duct size added with inside = outside = nominal shows in Duct Settings as typed, and
whether `CONDUIT_STANDARD_TYPE_PARAM`'s value text is the standard's name (a conduit whose standard
cannot be read is counted as using the size), are KY2 and KY5's.

**Routing.** Measured through `heron_retrieve.find` on three fresh scratch stores - the tree at `b1a892d` without the two size cards, with `REPORT_DUCT_CONDUIT_SIZES` alone, and with both - `model` backend, re-ranker absent, no Revit filter and Revit 2020, 2024 and 2025, over every declared utterance and routing-table sentence of a live card (4,348 sentences, eight probes of the same requests in other words included), with and without the host's question flag. The four requests reach this card without the flag (identity) - `AUTO_SIZE_MEP`, `REPORT_PIPE_SEGMENTS`, `SET_DUCT_LINING` and `CREATE_PIPE_SEGMENT` before - and a read with it: `REPORT_DUCT_CONDUIT_SIZES`, or `REPORT_PIPE_SEGMENTS` for *"add DN 65 to the steel pipe segment"*. No other sentence moved to or from this card with the full search; what moved is written in [Group KX](group-kx.md). **On the keyword route alone** (`check-routing`'s *SENTENCES TWO FRAGMENTS BOTH WANT*, a report): *"what size is this pipe"* and *"what sizes are these pipes"* now rank this card first instead of `AUTO_SIZE_PIPE` - write to write - and `REPORT_FAMILY_CONNECTORS`' own *"list the connectors in this family"* and *"list this family's connectors"* now rank `REMOVE_FAMILY_CONNECTOR` (a write) first, where they ranked their own card before - neither card is one of these, the shift is in word weights, and the full search still answers all four by identity, unchanged.

**The model - Group KX's scratch project** (`mep-sizes-kx`, [KX1](group-kx.md)) in Revit 2020, never a
working model. The job file is
[`tools/jobs/edit-mep-sizes-ky-2026-11-01.yaml`](../../tools/jobs/edit-mep-sizes-ky-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every write is **rolled back** (`validate --write`) except
the one `try-once`, which the modeller keeps or discards. `REPORT_DUCT_CONDUIT_SIZES` (or
`REPORT_PIPE_SEGMENTS` for a segment) before and after every row. Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KY1** | *Arrange.* In KX's project: make sure Duct Settings > Round holds 250 and not 315; draw one round duct at 250 mm; draw one pipe on a steel segment at that segment's smallest size, and write that segment's sizes down | 250 in the round list, 315 not; one duct, one pipe |
| **KY2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, round, add 250: `added` 0, skipped *250 mm is already in the round duct size list - left as it is*, `before` and `after` the same. Positive, round, add 315: `added` 1, `after` holds 315 beside everything before, and Duct Settings > Round shows 315; rectangular and oval untouched |
| **KY3** | *A size in use.* Round, remove 250; then the steel segment, remove the size the drawn pipe carries; then round, remove a size only a round fitting in the model carries (place one by hand) | First two: `removed` 0, refused *... was NOT removed - 1 element(s) in the model carry it*, the list unchanged. Third: what Revit does - refused by Revit, or removed with the fitting left as it is - recorded as a finding against the card |
| **KY4** | *A pipe size.* `sizeList` pipe, `name` the exact steel segment, add `sizes` 65 with no diameters; then with `outsideMm` 76.1, `insideMm` 70.3 | First: refused, the outside and inside diameter named as needed and never guessed. Second: `added` 1, Segments and Sizes shows nominal 65, ID 70.3, OD 76.1; every other segment unchanged |
| **KY5** | *A conduit size.* `sizeList` conduit, `name` a standard KX1 wrote down, add 32 with `outsideMm`, `insideMm` and `bendRadiusMm` from that standard's nearest size; then remove it | `added` 1 then `removed` 1, Conduit Settings > Size for that standard only; every other standard unchanged |
| **KY6** | *Revit 2026 or 2027.* KY2 again on a project from that release's template | The same answers |
