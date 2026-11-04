# Needs checking — Group ME

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group ME - Analyze > Load Combinations - add load combinations from factored load cases, with their state and type, and list the combinations a model has: `CREATE_LOAD_COMBINATION`, `REPORT_STRUCTURAL_LOADS` widened (2026-11-02)

Asked for in the modeller's words: *"add a load combination 1.2DL + 1.6LL"*, *"make a combination ULS1 = 1.4
Dead"*, *"add the serviceability combination SLS1 = 1.0DL + 1.0LL"* and *"what load combinations are in this
model"* - Revit's Analyze > Load Combinations. No card made or read one: a grep of every fragment's C# that
day for `LoadCombination` and `LoadUsage` found nothing. Widening `CREATE_LOAD_CASE` ([Group LA](group-la.md))
was weighed and declined: a load case takes a category, a nature and a number, a combination a formula of
factored cases, a state, a type and usages - no input in common but the document - so one card would be two
jobs behind a switch. The question widens `REPORT_STRUCTURAL_LOADS` ([Group KZ](group-kz.md)), DRAFT, the
read of the same set. `COMBINE_FAMILY_FORMS` joins solids in a family - a different job sharing a word.

| What | Where | What it does |
|---|---|---|
| `CREATE_LOAD_COMBINATION` (MODIFY) | [`brain/fragments/create-load-combination`](../../brain/fragments/create-load-combination/fragment.yaml) | Adds **load combinations** - each written as a name, "=" and factored load cases (`ULS1 = 1.2 DL1 + 1.6 LL1`, several separated by ;), with a **state** (Ultimate or Serviceability; from 2026 the further states Revit has), a **type** (Combination or Envelope) and **usages** the project has - and reads every combination back before and after with its factors and load cases (`added`, `before`, `after`). Each load case is matched by its **exact** name; one no case has, one in other capitals, a load combination named as a component or a case twice refuses that combination with the cases listed - **a load case is never made**. A name already used in any capitals is skipped. A state or type left out keeps Revit's default, read back and stated; an entry with no name is named after its formula, and the answer says so. Each combination is made in its own sub-transaction, so one Revit refuses leaves nothing of itself |
| `REPORT_STRUCTURAL_LOADS` (READ), widened | [`brain/fragments/report-structural-loads`](../../brain/fragments/report-structural-loads/fragment.yaml) | Now also lists **every load combination** with its state, type, factored load cases and usages, and the load usages the project has (`show` combinations; `combinationCount`). Every other list as before |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `LoadCombination.Create(Document, string)` and
`(Document, string, LoadCombinationType, LoadCombinationState)`, `State` and `Type` with setters,
`GetComponents`, `SetComponents(IList<LoadComponent>)`, `GetUsageIds`, `SetUsageIds(IList<ElementId>)`,
`new LoadComponent(ElementId, double)` with `LoadCaseOrCombinationId` and `Factor`, `LoadUsage`, the two
`LoadCombinationType` values and `Serviceability` and `Ultimate` carry the same signatures 2020-2027;
`LoadCombinationState` gains `ServiceabilityQuasiPermanent`, `ServiceabilityFrequent`, `ServiceabilityRare`,
`Accidental` and `Special` in 2026 (`#if REVIT2026_OR_GREATER`). **Nothing here has run in Revit.** Both
cards are DRAFT. **Only Revit can settle** what state and type `LoadCombination.Create(Document, string)`
gives a combination (the answer reads them back rather than assuming them - ME2 records them), and that
`SetComponents` takes the factors as given - ME4 checks it in the dialog.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `0597dfa` and of
this change - `model` backend, re-ranker absent, no Revit filter and Revit 2020 and 2024, with and without
the host's question flag (the `EXECUTE` ceiling) - over every declared utterance and routing-table sentence
of a live card and six probes (4,465 sentences). The three write sentences reach `CREATE_LOAD_COMBINATION`
without the flag and a read with it (`REPORT_STRUCTURAL_LOADS`; *"make a combination ULS1 = 1.4 Dead"*
`SELECT_BY_MEP_SYSTEM`). *"what load combinations are in this model"* reaches `REPORT_STRUCTURAL_LOADS` with
and without the flag, and so do the probes *"show me the load combinations"* (`REPORT_PANEL_CIRCUITS`
before), *"which load combinations does this model have"* and *"list the load combinations"*. The first
draft's purposes pulled the second and third probes to the write without the flag - closed in the same
change by rewording both purposes, [row 5b-1430](../fragment-issues/section-5b-rows-401-425.md). **Still
reaching the write without the flag:** the probe *"what combinations are in this model"*, without the word
"load" - nobody's words, so not declared ([row 5b-1431](../fragment-issues/section-5b-rows-401-425.md)).
Without the flag no other sentence any card declares or claims moved but *"give me the analytical model for
the structural engineer"* on 2020, where its claimant `REPORT_ANALYTICAL_MODEL` (2023 on) cannot run -
`REPORT_STRUCTURAL_LOADS` to `REPORT_STRUCTURAL_CONNECTIONS`, read to read. With the flag three writes' own
sentences moved, each read to read, none from a card that declares or claims it. `tools/check-routing.py` on
a scratch store: no question answered by a write; *claimed in a routing table and not reached* 66 before and
after, the same rows.

**The model - Group KZ's `loads-kz` scratch project** (KZ1), never a working model. Every write here is
ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/create-load-combination-me-2026-11-02.yaml`](../../tools/jobs/create-load-combination-me-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **ME1** | *Arrange.* Open `loads-kz`; Analyze > Load Combinations: add **ULS-A** = 1.2 x DL1, Ultimate, Combination. Save. Write down every combination (name, state, type, factors, load cases) and every load case | DL1 and LL1 among the cases; ULS-A the one combination made by hand |
| **ME2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, `combinations` "uls-a = 1.4 DL1": `added` 0, `skipped` names ULS-A, before and after the same, ULS-A still 1.2 x DL1. Positive, "ULS-T1 = 1.2 DL1 + 1.6 LL1", `state` ultimate: `added` 1, ULS-T1 - Ultimate: 1.2 x "DL1" + 1.6 x "LL1" in `after`; findings name the type Revit gave it |
| **ME3** | *The read.* The job file's second job | `show` combinations: "Load combinations (n)" with ULS-A as Ultimate, Combination: 1.2 x "DL1", and the load usages, no other section. `show` loads: no combinations section, `combinationCount` still n |
| **ME4** | *The second route.* ME2's positive kept once with `try-once`, then Analyze > Load Combinations | ULS-T1 listed with the state, type, factors and load cases `after` gave; ULS-A unchanged |
| **ME5** | *Never guessed.* `combinations` "ULS-T2 = 1.4 Dead"; then "ULS-T3 = 1.2 dl1 + 1.6 LL1"; then "ULS-T4 = 1.2 DL1 + 1.6 LL1" with `state` "permanent" | `added` 0 each time - refused lists the load cases and makes no case Dead; names "dl1" as DL1 in other capitals, not taken; names "permanent" and the states this Revit has |
| **ME6** | *Revit 2026 or later.* `combinations` "SLS-T2 = 1.0 DL1 + 1.0 LL1", `state` "frequent", on a project made the same way in that release; then the same on 2020 | 2026: `added` 1, Serviceability Frequent. 2020: `added` 0, refused says the state is 2026 and later only |
