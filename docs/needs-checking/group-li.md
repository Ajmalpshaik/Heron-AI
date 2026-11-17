# Needs checking — Group LI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LI - a circuit naming scheme made, or the project's circuit naming set: `SET_CIRCUIT_NAMING_SCHEME` (2026-11-02)

**Tracked as GitHub issue [#1003](https://github.com/Ajmalpshaik/Heron-AI/issues/1003) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Asked for in the modeller's words: *"which circuit naming scheme is the project using"*, *"make a circuit
naming scheme Panel-Circuit with a dash"* and *"use the Panel-Circuit naming for this project"* - Revit's
Manage > MEP Settings > Electrical Settings > Circuit Naming. No card made a scheme or set one: a grep of
`brain/fragments` that day for `CircuitNamingScheme` and `CircuitNamingSchemeSettings` found nothing.
`EDIT_ELECTRICAL_CIRCUIT` changes a circuit's devices and panel, `CREATE_MEP_SYSTEM` makes a circuit,
`REPORT_PANEL_CIRCUITS` reads circuit numbers - none touches how circuits are named; look-alikes, not
duplicates.

| What | Where | What it does |
|---|---|---|
| `SET_CIRCUIT_NAMING_SCHEME` (MODIFY, **Revit 2021 on**) | [`brain/fragments/set-circuit-naming-scheme`](../../brain/fragments/set-circuit-naming-scheme/fragment.yaml) | `action` **make**: a new circuit naming scheme (`schemeName`) from circuit parameters in order (`parts` "panel; circuit number" - Revit's labels, panel, circuit number, load name, start slot, or a project parameter by its exact name) with `separator` between each two. Revit's `IsValidCombinedParameters` is asked first. `action` **use**: the project's circuit naming scheme set to one the model has. No scheme name or part is guessed: one not there exactly is refused with the names listed, one differing in capitals named and not taken. A new name already used and the scheme already the project's are skipped and named. Every scheme with its parts and the project's marked, before and after; a read-back that disagrees throws, so the whole change rolls back. No panel's own Circuit Naming parameter is touched. Never renames, overwrites or deletes |
| `REPORT_ELECTRICAL_SETTINGS` (READ), widened | [`brain/fragments/report-electrical-settings`](../../brain/fragments/report-electrical-settings/fragment.yaml) | A seventh list, `show` **naming**: each circuit naming scheme's parameters, prefixes, suffixes and separators in order, and the one the project uses (`circuitNamingSchemes`). On Revit 2020 it says schemes arrived in 2021 and reads nothing for that list. Its other lists are [Group KU](group-ku.md)'s and [Group LH](group-lh.md)'s |

**One of a pair.** `REPORT_ELECTRICAL_SETTINGS` answers every question about the schemes - which there are,
their parts, which is the project's - so none reaches the write (D-86).

**What is done and what is not.** The write compiles for 2021 to 2027 and the read for all eight releases
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `CircuitNamingScheme.Create(Document, string,
IList<TableCellCombinedParameterData>)`, `GetCombinedParameters`, `IsNameUnique` and
`IsValidCombinedParameters`, and `CircuitNamingSchemeSettings.GetCircuitNamingSchemeSettings`,
`CircuitNamingSchemeId` (get and set) and `IsValidCircuitNamingSchemeId`, are the same 2021 to 2027 and **not
in 2020** - the write claims 2021 on, and the read's list is behind `#if !REVIT2020`.
`TableCellCombinedParameterData` and the circuit parameters `RBS_ELEC_CIRCUIT_PANEL_PARAM`,
`RBS_ELEC_CIRCUIT_NUMBER`, `RBS_ELEC_CIRCUIT_NAME` and `RBS_ELEC_CIRCUIT_START_SLOT` are 2020 to 2027.
**Nothing here has run in Revit.** Both cards are DRAFT. **Only Revit can settle** which parameters Revit
accepts as parts (the write asks `IsValidCombinedParameters` and refuses otherwise), whether a scheme draws a
part's separator after it as a schedule's combined parameter does (`ADD_SCHEDULE_COMBINED_FIELD` measured that
in Revit 2020), and what names the electrical template's own schemes have - LI1 to LI4.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree with Group LH's change
and with this one too (`model` backend, re-ranker absent; no Revit filter, 2020, 2024 and 2025; every declared
utterance and routing-table sentence in the library, 4,365 with the probes), with and without the host's
question flag. The question reaches `REPORT_ELECTRICAL_SETTINGS` with and without the flag on every release
(`SELECT_BY_ELECTRICAL_CIRCUIT` before). The two commands reach the write without the flag from 2021 on
(`EDIT_ELECTRICAL_CIRCUIT`, a write, and `REPORT_PANEL_CIRCUITS` before) and the read with it; behind a 2020
wall, where no card can make or set a scheme, they reach `EDIT_ELECTRICAL_CIRCUIT` and `REPORT_PANEL_CIRCUITS`
as before ([row 5b-1389](../fragment-issues/section-5b-rows-401-425.md), OPEN). Without the flag no other sentence moved. With it, write sentences that no read can own -
`CREATE_MEP_SYSTEM`'s *"put these devices on one circuit"* among them - moved from one read to another; none
reaches its claimant on either tree. Probes nobody said, so not declared: *what are the circuit naming
schemes* reaches the write without the flag on 2021 on and the read with it; *name the circuits panel dash
number* reaches `REPORT_PANEL_CIRCUITS`.

**The model - `electrical-settings-li`**, Group KU's scratch project made again in **Revit 2024**, new from the
metric electrical template under `%TEMP%\heron-work`, never a working model: one branch panelboard DB-1 with
one circuit. The job file is
[`tools/jobs/set-circuit-naming-scheme-li-2026-11-02.yaml`](../../tools/jobs/set-circuit-naming-scheme-li-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every write is **rolled back** (`validate --write`) except
the one `try-once`, which the modeller keeps or discards. `REPORT_ELECTRICAL_SETTINGS` with `show` naming
before and after every row. Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when
two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LI1** | *Arrange.* Make `electrical-settings-li` in Revit 2024; in Circuit Naming write down every scheme, its parameters and separators and the project's - the job file assumes one named **Standard**; place DB-1 with one circuit; save | The schemes the electrical template ships and the circuit's name under the project's |
| **LI2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's first job | Negative, a new scheme named **Standard**: `changed` 0, skipped *A circuit naming scheme is already named "Standard" - left as it is, nothing made.*, `before` and `after` the same. Positive, **Panel-Circuit**: `changed` 1, *"Panel-Circuit": Panel "-" + Circuit Number* in `after`, and the Circuit Naming dialog shows Panel, a dash, then Circuit Number - **not a dash after the number** |
| **LI3** | *Using it.* With Panel-Circuit kept once (`try-once`, in a scratch copy): `action` use, `schemeName` Panel-Circuit; then the same again | First: `changed` 1, the dialog shows it as the project's and DB-1's circuit reads DB-1-1 if the panel follows the project setting. Second: `changed` 0, skipped *The project already uses "Panel-Circuit"* |
| **LI4** | *Never guessed.* `action` make with `parts` "panel; colour"; with `parts` left out; `action` use with `schemeName` ZZNOSCHEME | Refused, naming the parts Heron knows; refused - never guessed; refused, naming the model's schemes. `changed` 0 for each |
| **LI5** | *The read.* The job file's second job; then `show` naming on Group KU's Revit 2020 project | Negative, ZZNOSCHEME: `circuitNamingSchemes` 0 and *Nothing in the lists read has a name containing "ZZNOSCHEME"*. Positive: every scheme LI1 wrote down, in order, the project's marked. 2020: `circuitNamingSchemes` 0 and the line saying schemes arrived in 2021 |
| **LI6** | *Revit 2026 or 2027.* LI2 and LI3's first case again on a project from that release's template | The same answers |

### Run record - 2026-11-14 night, Revit 2024 (session 3592), the owner asleep

On the scratch *electrical-mi* of [Group MI](group-mi.md) (Heron-made from Revit 2024's metric Electrical template)
instead of a separate *electrical-settings-li*: the same template. `tools/jobs/set-circuit-naming-scheme-li-2026-11-02.yaml`
with `in: "electrical-mi"` - `1 POSITIVE EMPTY, 1 ALREADY` (`report-electrical-settings` is PROVEN).

| # | Result | What was seen |
|---|---|---|
| LI1 | **Read** | `show=naming`: *"Circuit naming schemes (0): none. The project uses none Heron could read."* A rolled-back probe agrees: no `CircuitNamingScheme` element, the project's scheme id -1. So there is no *Standard* to be the negative |
| LI2 | **FAIL** | Both legs *"Revit does not accept Panel + Circuit Number as the parts of a circuit naming scheme. Nothing was made."* - Revit's `IsValidCombinedParameters` refuses `RBS_ELEC_CIRCUIT_NUMBER`, the card's *circuit number*, and accepts `RBS_ELEC_CIRCUIT_NAMING_INDEX` - [row 5b-1660](../fragment-issues/row-5b-1660.md). With `parts=panel; load name` instead (rolled back, kept apart as evidence, not the draft): *"Made the circuit naming scheme "Panel-Load": Panel "-" + Load Name"*, `changed 1` |
| LI4 | **PASS, one case** | `action=use schemeName=ZZNOSCHEME`: *"No circuit naming scheme is named "ZZNOSCHEME". The model's schemes are: none."*, `changed 0` |
| LI3, LI5, LI6 | **NOT RUN** | LI3 needs a scheme kept (`try-once`, his Keep); LI5's card is PROVEN; LI6 needs Revit 2026 or 2027 |

### 2026-11-16 - set-circuit-naming-scheme version 2 (row 5b-1660)

LI2's positive was written against version 1, which mapped *circuit number* to `RBS_ELEC_CIRCUIT_NUMBER` - a parameter Revit refuses in a naming scheme, so it could never pass (the 2026-11-14 night's FAIL above). Version 2 maps it to `RBS_ELEC_CIRCUIT_NAMING_INDEX` and *start slot* to `RBS_ELEC_CIRCUIT_SLOT_INDEX`, so LI2's positive now reads *"Panel-Circuit": Panel "-" + Circuit Naming Index* in `after`, the dialog showing Panel, a dash, then the circuit's number. LI1's arrangement needs a scheme named **Standard** made first: the metric Electrical template ships none on Revit 2024. The rows above are kept as written; this note is what LI2 is read against from now. NOT RUN on version 2 yet - NEEDS REAL REVIT.

### 2026-11-16 - set-circuit-naming-scheme version 3 (row 5b-1739)

Version 2 took Revit's own parameter for a word such as *phase* or *voltage* without a word, even where the model has a project parameter of electrical circuits by that name. Version 3 refuses that word with both named and asks for *revit: phase* or *project: Phase*; a parameter bound to other categories only is never counted. The cases are in the card's `tests/cases.yaml`. NOT RUN - NEEDS REAL REVIT, and the arrangement needs the modeller's hands: two parameters made in Manage > Project Parameters.

| # | Run | Look for |
|---|---|---|
| **LI7** | *Two parameters of one name.* By hand: a text project parameter **Phase** bound to Electrical Circuits, a parameter **Voltage** bound to Electrical Equipment only, and a shared **Phase** bound to Electrical Equipment only. Then `action` make with `parts` "panel; phase" (THE NEGATIVE FIRST), "panel; revit: phase", "panel; project: Phase" and "panel; voltage", each rolled back | "panel; phase": `changed` 0, refused naming both and the two ways to write it. "revit: phase": `changed` 1, Revit's phase. "project: Phase": `changed` 1, `after` reading *Phase (project parameter)* - the circuit one, never *2 project parameters are named* - or Revit's own refusal of it, recorded as it comes. "panel; voltage": `changed` 1, Revit's Voltage, not refused |
