# Needs checking — Group LU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LU - Revit's heating and cooling loads, run on the model with its own Energy Settings, a loads report made and each space's calculated loads read back: `RUN_HEATING_COOLING_LOADS` (2026-11-02)

Asked for in the modeller's words: *"run the heating and cooling loads"*, *"calculate the HVAC loads for this
model"* and *"run the loads analysis and show the cooling load per space"* - Revit's Analyze > Heating and Cooling
Loads. heron-duplicate-checker's method, run before writing: `brain/heron_merge.py` found no shared signature with
a shared capability, and the duplicate check and the Matcher found nothing equal. No fragment's C# ran the
analysis. `READ_SPACE_LOADS` (PROVEN), `REPORT_SPACE_ENVELOPE` (PROVEN) and `REPORT_ROOM_SPACE_DATA` (DRAFT) read
what the spaces already hold and change nothing - **look-alikes**: this one makes a view and changes every space's
calculated loads, and a read widened into a write would put a Keep or Discard in front of every question it
answers. `REPORT_SPACE_AIRFLOW` is DEPRECATED into `READ_SPACE_LOADS`. The read-back here is `READ_SPACE_LOADS'`
reading, copied, and that card's `impl/` is not touched.

**Only Revit 2027 can run it from the API.** Read off the shipped assemblies with `tools/api-surface --members`
and `--dump`, 2026-11-02: `ViewHVACLoadsReport` - `Create(Document, string)`, `RunLoadsAnalysis()`,
`IsAnalysisCompleted()` - is in RevitAPI 2027 and in no release 2020 to 2026. `EnergyDataSettings` (2020-2027)
holds the settings and runs nothing; `EnergyAnalysisDetailModel` (2020-2027) builds the energy analytical model
and calculates no loads; no Run, Calculate or Compute member on any Analysis or Mechanical type of 2020-2026 runs
them. `PostableCommand.HeatingAndCoolingLoads` (2020, 2021 and 2027; absent 2022-2026) only opens Revit's dialog
for a person to press Calculate. So the card claims Revit 2027 alone.

| What | Where | What it does |
|---|---|---|
| `RUN_HEATING_COOLING_LOADS` (MODIFY, Revit 2027) | [`brain/fragments/run-heating-cooling-loads`](../../brain/fragments/run-heating-cooling-loads/fragment.yaml) | Makes a loads report view - named as asked, or the first free *Heating and Cooling Loads Report* - runs Revit's heating and cooling loads on it with the model's **own** Energy Settings, read and never set, and reads back each placed space's **calculated** cooling and heating load in watts, converted by Revit's own `UnitUtils`, naming a typed design load where one overrides it. Refused before anything is made: a family, a model with no spaces or none placed, no Energy Settings, and a report name a view already has. Revit's own refusal to run is passed on in its words and nothing is kept. The figures are calculated values, never a statement that anything complies |

**What is done and what is not.** It compiles for Revit 2027, the one release it claims
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member it calls was read off the
2027 assemblies with `tools/api-surface --members` - the list is in the card's `compatibility-note`;
`EnergyDataSettings.GetEnergyDataSettings` is used rather than `GetFromDocument`, which 2027 marks obsolete.
**Nothing here has run in Revit.** The card is DRAFT. **Only Revit can settle:** whether `RunLoadsAnalysis` runs
inside the host's open transaction at all (LU2); whether the analysis has finished when it returns -
`IsAnalysisCompleted` is asked and handed back as `analysisCompleted`, and when it is false the spaces' figures
are not handed back as this run's (LU3); what Revit says for Energy Settings it cannot run with (LU4); and how long
it takes on a model of Snowdon's size against the bridge's time limit (LU3). **Not done:** changing any Energy
Setting, building or deleting the energy model, Systems Analysis, and exporting the loads report.

**A finding on the copied reading, not acted on.** `READ_SPACE_LOADS` converts a load with a factor its own card
calls assumed and unproven (British thermal units per second). This card does not copy the factor: Revit
converts its own internal value with `UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Watts)`. LU3's
comparison against Properties checks this card's figures; it says nothing about the other card's factor, whose
check is that card's own.

**Routing.** `heron_retrieve.find` on fresh scratch stores of the tree at `1327aed` and of this change, over every
declared utterance and routing-table sentence of every live card plus five load questions, on Revit 2027, Revit
2024 and no Revit, with and without the host's question flag, on the `model` and `lexical` backends. The three
sentences reach this card on Revit 2027 and with no Revit named, without the flag; with the flag, and on Revit
2024 where the card is not offered, they reach a read. *"What is the cooling load of this space"* moved from
`READ_SPACE_LOADS` to this card on `model` without the flag, and is now declared on `READ_SPACE_LOADS` - [row
5b-1406](../fragment-issues/section-5b-rows-401-425.md), FIXED. Its paraphrase *"what is the heating load of this
space"*, which nobody has said, still reaches this card on `model` without the flag on Revit 2027 and with no
Revit named - recorded in that row, not tuned. With the flag five sentences claimed by writes moved from one read
to another (*"rename the sheet set"*, *"bring the excluded parts back"*, *"make a dependent view"*, *"set up a sheet
for this"*, *"make all the architecture faint"*) - recorded, not tuned. `tools/check-routing.py`: no question
answered by a write; claimed in a routing table and not reached 67 before and 67 after on `1327aed`, and 69 before and 69 after on `184eb96` - the same lines each time. **One question is not repaired:** `tests/test_routing_phrases.py`'s *"which units does this model show cooling load in"* is a coin toss between this card and `REPORT_PROJECT_UNITS` on the trained model without the flag, on Revit 2027 and with no Revit named, and this card takes it - [row 5b-1407](../fragment-issues/section-5b-rows-401-425.md), OPEN, the owner's to choose.

**The model** - a scratch copy of the Snowdon Towers Sample HVAC model, opened and saved in **Revit 2027** under
`%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job
file is [`tools/jobs/run-heating-cooling-loads-lu-2026-11-02.yaml`](../../tools/jobs/run-heating-cooling-loads-lu-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LU1** | *Arrange.* The Snowdon Towers Sample HVAC model opened in Revit 2027 and saved as a scratch copy; Area and Volume Computations on; Analyze > Energy Settings left as shipped and a screenshot taken; three Level 1 spaces' Calculated Cooling Load and Calculated Heating Load noted from Properties. Save | The floor plan *M1* in the Project Browser; the three spaces' figures written down |
| **LU2** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's job, `--write`, rolled back | Negative, report name *M1*: refused *"A view called "M1" is already in this model ... NOTHING WAS RUN ..."*, `reportView` invalid, no new view. Positive: `reportView` 1 id, `coolingW` and `heatingW` one entry per placed bounded space, the report naming the Energy Settings it ran with. **A refusal from Revit that the analysis cannot run inside a transaction is a finding for the author, never a pass** |
| **LU3** | *What Revit calculated.* LU2's positive, kept through `try-once` on the modeller's yes | `analysisCompleted` true; the three noted spaces' Calculated Cooling and Heating Load in Properties now equal to their `coolingW` and `heatingW` within 1 W; the report view under Reports showing Revit's loads report; Energy Settings unchanged against LU1's screenshot; how long it took. **`analysisCompleted` false, or a time-out, is recorded as it came, never a pass** |
| **LU4** | *Revit's refusal.* `validate run-heating-cooling-loads --write` on a scratch copy whose Energy Settings Revit's own Heating and Cooling Loads dialog refuses, rolled back | The call throws *"Revit refused to run the heating and cooling loads: ... NOTHING from this call was kept ..."* with Revit's words; no view after; settings unchanged |
| **LU5** | *No spaces.* `validate run-heating-cooling-loads --write` on a scratch project from the metric Architectural template with rooms and no spaces, saved in Revit 2027, rolled back | Refused *"This model has no spaces ... a Room carries no load ..."*; nothing made |
| **LU6** | *The second route.* Revit's own Analyze > Heating and Cooling Loads by hand on the same copy, Calculate pressed with the dialog's settings | The three spaces' Calculated Cooling and Heating Load the same as LU3's within 1 W; Undo takes the Heron run back as one entry |
