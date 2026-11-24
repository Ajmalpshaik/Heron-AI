# Needs checking — Group QH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QH - nine reading tools count what they pass over, WN-7 batch 1 (D-64) - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1346](https://github.com/Ajmalpshaik/Heron-AI/issues/1346) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [D-64](../decisions/D-64.md) - a reading tool that goes looking and can drop a candidate declares how many it
dropped. Its worklist is [row 5b-1909](../fragment-issues/row-5b-1909.md), work-note item WN-7 of issue
[#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005); the list is
`python tools/check-revit-gate.py --list reporting`. This is batch 1 of that work: each card below passed over a
candidate whose read Revit refused - a circuit that would not give its loads, an area that would not say its scheme -
without a word, so a count could be short or a failed read could land in a "no" (*"on no panel"*, *"bare"*). Each now
counts it as an accounting output and says it in one sentence **only when the count is above 0**. Each card's C#
changed, so each is **DRAFT** with its version raised until its check below runs and Ajmal PS signs (D-30); a proof
already on a card is kept there as the record:

- [`REPORT_DESIGN_OPTIONS`](../../brain/fragments/report-design-options/fragment.yaml) **version 2** (was PROVEN) -
  `unresolvedElements`: elements whose design option would not read, in no option's count.
- [`FILTER_ELEMENTS_BY_TYPE`](../../brain/fragments/filter-elements-by-type/fragment.yaml) **version 3** (was PROVEN) -
  `unresolvedType`: the exemplar with no type D-64 names, whose `found: 0` read the same as a type nobody uses.
- [`REPORT_MASSES`](../../brain/fragments/report-masses/fragment.yaml) **version 2** (was PROVEN) - `skippedMasses`:
  elements of the Mass category that are not a mass family instance this reads.
- [`SELECT_BY_INSULATION`](../../brain/fragments/select-by-insulation/fragment.yaml) **version 3** (was PROVEN) -
  `unresolvedCoverings`: insulation or lining that would not say what it wraps, whose host read bare.
- [`SELECT_BY_ELECTRICAL_CIRCUIT`](../../brain/fragments/select-by-electrical-circuit/fragment.yaml) **version 4** (was
  PROVEN) - `unresolvedCircuits`: circuits that would not give their loads, whose loads read as on no circuit.
- [`REPORT_MEP_PRESSURE_DROP`](../../brain/fragments/report-mep-pressure-drop/fragment.yaml) **version 3** (was PROVEN)
  - `skippedSections`: sections that would not read, missing from the rows and the critical-path total.
- [`REPORT_PANEL_CIRCUITS`](../../brain/fragments/report-panel-circuits/fragment.yaml) **version 2** (was PROVEN) -
  `unresolvedCircuits`: circuits whose panel would not read, which version 1 listed ON NO PANEL.
- [`REPORT_AREA_VOLUME_COMPUTATIONS`](../../brain/fragments/report-area-volume-computations/fragment.yaml) **version 2**
  (was PROVEN) - `unresolvedAreas`: areas and area plans whose scheme would not read.
- [`REPORT_SPRINKLER_NETWORK`](../../brain/fragments/report-sprinkler-network/fragment.yaml) **version 3** (DRAFT) -
  `skippedConnectors`: piping connectors whose domain or type would not read, missing from the network. Run
  [Group PA](group-pa.md) first.

Not in this group, nothing to run: `REPORT_LEVEL_ELEVATIONS` version 6 declares `unreadable`, which its C# already
counted - no line of its C# changed; and `REPORT_WALL_ATTACHMENTS`' `unread` was already counted - the gate learned
the word ([`check-revit-gate.py`](../../tools/check-revit-gate.py)).

**What every check here looks for, the same rule each time:** the new count is always in the answer; its sentence is
there **only** when the count is above 0 (and for `REPORT_MASSES` and `FILTER_ELEMENTS_BY_TYPE` only on the empty
answer); and an answer with nothing passed over reads exactly as the version before. **Write each count down even when
it is 0** - nobody knows yet how often Revit refuses these reads on an ordinary model, and that number is what this
group is for. Arranging a model where Revit refuses one is not asked: no one knows how to make it do so. The one count
that CAN be made above 0 on purpose is `FILTER_ELEMENTS_BY_TYPE`'s - a room has no type (QH2).

Written in a cloud container with no Revit; every fragment compiles on 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-24). No routing words changed. **Nothing here has run in Revit.**

**The models are scratch copies, never a working model** - the ones each card's last proof or arrangement used, named
in the job file. The batch runs as one file:

```text
python tools/batch-prove.py tools/jobs/qh-wn7-batch1-revit2020-2026-11-24.yaml --dry-run
python tools/batch-prove.py tools/jobs/qh-wn7-batch1-revit2020-2026-11-24.yaml --session <process id>
```

Its dry run on 2026-11-24: 9 jobs, 9 WOULD RUN. Every run is a read: nothing changes and the undo list gains no entry.

| ID | Do this | Pass looks like |
|---|---|---|
| **QH1** | *`REPORT_DESIGN_OPTIONS` version 2 - Revit 2020. THE NEGATIVE FIRST.* A project with no design option; then Project2 with one option made by hand (Manage > Design Options, one set, one option, saved) | No option: version 1's *"this project has NO design options"* and nothing more, `unresolvedElements` 0. With the option: named under its set and marked primary, as version 1's proof; `unresolvedElements` written down (0 expected) and **no** *"would not say which design option they are in"* sentence while it is 0 |
| **QH2** | *`FILTER_ELEMENTS_BY_TYPE` version 3 - round 8's arrangement on Project1.* The job's two legs; then BY HAND one room selected and handed in as the exemplar | The job: version 2's walls, `unresolvedType` 0, no *"has no type"* line. The room: `found` 0, `unresolvedType` 1, and the last line of `linkedMatches` *"The element given has no type, so nothing was searched for - found 0 is not "none of this type""* |
| **QH3** | *`REPORT_MASSES` version 2 - P1102 MEP, round 2's arrangement. THE NEGATIVE FIRST.* `masses` NoSuchFamily; then every mass | Negative: version 1's *"No mass matched"*, `skippedMasses` written down, the *"passed over"* sentence there exactly when it is above 0. Every mass: version 1's six masses and lines, `skippedMasses` written down (0 expected), no such sentence |
| **QH4** | *`SELECT_BY_INSULATION` version 3 - Project2, round 1's arrangement.* Ducts, bare then insulated | Both legs as version 2's proof; `unresolvedCoverings` written down (0 expected) and **no** *"would not say what they wrap"* sentence while it is 0 |
| **QH5** | *`SELECT_BY_ELECTRICAL_CIRCUIT` version 4 - Project1, round 13's arrangement. THE NEGATIVE FIRST.* `circuitKind` power with a name nothing has; then `circuitKind` none | Negative: version 3's *"0 element(s) across 0 circuit(s)"* and **no** *"would not give their loads"* sentence. None: version 3's element on no circuit, `unresolvedCircuits` written down (0 expected), no such sentence while it is 0 |
| **QH6** | *`REPORT_MEP_PRESSURE_DROP` version 3 - Project1, round 8's arrangement.* Every system; then a name nothing has | Both as version 2's; `skippedSections` written down (0 expected) and no *"could not be read and are in neither the rows"* line under any system while it is 0 |
| **QH7** | *`REPORT_PANEL_CIRCUITS` version 2 - P1102 MEP, Revit's own MEP sample. THE NEGATIVE FIRST.* `panelName` NO-SUCH-PANEL; then empty | Negative: version 1's *"No circuit is fed from a panel named "NO-SUCH-PANEL""*, `unresolvedCircuits` 0. Empty: panels and circuits as Revit's panel schedules show them, `unresolvedCircuits` written down (0 expected) and no *"would not say which panel feeds them"* sentence while it is 0 |
| **QH8** | *`REPORT_AREA_VOLUME_COMPUTATIONS` version 2 - Project2 and Family1, round 3's arrangement.* | Family1: version 1's answer, `unresolvedAreas` 0. Project2: every line as version 1's, `unresolvedAreas` written down (0 expected) and no *"would not say which scheme they belong to"* sentence while it is 0 |
| **QH9** | *`REPORT_SPRINKLER_NETWORK` version 3 - P1102 MEP, after Group PA.* PA's two legs again | `networkJson` exactly as PA1's run gives it, `skippedConnectors` written down (0 expected) and no *"would not say their domain or type"* sentence while it is 0; the negative as PA2 (b), `skippedConnectors` 0 |
