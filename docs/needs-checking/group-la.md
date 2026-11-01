# Needs checking — Group LA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LA - add a structural load case: `CREATE_LOAD_CASE` (2026-11-01)

Asked for in the modeller's words: *"add a load case called Wind X"* - Revit's Analyze > Load Cases > Add.
No card made one: a grep of every fragment's C# that day for `LoadCase.Create` found nothing.
`ADD_LOAD_CLASSIFICATION` adds an electrical load classification - a different job sharing a word.

| What | Where | What it does |
|---|---|---|
| `CREATE_LOAD_CASE` (MODIFY) | [`brain/fragments/create-load-case`](../../brain/fragments/create-load-case/fragment.yaml) | Adds **one load case** - the modeller's name, a **category** (Dead, Live, Wind, Snow, Roof Live, Accidental, Temperature, Seismic, or one the project added) and a **load nature** the project has, optionally a case number - and reads the load cases back before and after (`added`, `before`, `after`). A name already present in any capitals is skipped; no name, no category, a category or nature the project lacks, or a number already used is refused. The category is never read out of the name; a nature left out is the one called the same as the category, and the answer says so |

**One of a set.** `REPORT_STRUCTURAL_LOADS` ([Group KZ](group-kz.md)) answers every question about load cases
and natures, so none reaches this write.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `LoadCase.Create(Document, string, ElementId, LoadCaseCategory)` and
`(Document, string, ElementId, ElementId)`, `Number`, `NatureId`, `IsNumberUnique`, `LoadNature`, the eight
`LoadCaseCategory` values and `BuiltInCategory.OST_LoadCases` carry the same signatures 2020-2027. **Nothing
here has run in Revit.** The card is DRAFT. **Only Revit can settle** that the metric structural template
names one nature per category (the default for a nature left out) - LA3 checks it.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `243861c` without the four structural load cards and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020 and 2024, with and without the host's question flag - over every declared utterance and routing-table sentence of a live card (4,351 sentences, five probes included). *"add a load case called Wind X"* reaches this card without the flag (`ADD_LOAD_CLASSIFICATION`, the electrical load classification write, before) and `REPORT_STRUCTURAL_LOADS` with it. *"add a load classification called Kitchen Equipment"* still reaches `ADD_LOAD_CLASSIFICATION`. No sentence any other card declares or claims moved to this card. See [Group KZ](group-kz.md) for the set's whole measurement and row 5b-1380.

**The model - Group KZ's `loads-kz` scratch project** (KZ1), never a working model. Every write here is
ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/create-load-case-la-2026-11-01.yaml`](../../tools/jobs/create-load-case-la-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LA1** | *Arrange.* Open `loads-kz`; Analyze > Load Cases: write down every case (number, nature, category) and nature | DL1 and LL1 among the template's cases |
| **LA2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, `name` "dl1", `category` dead: `added` 0, `skipped` names DL1, before and after the same. Positive, `name` "Wind X", `category` wind: `added` 1, "Wind X" with nature Wind and Revit's next number in `after`; findings say the nature Wind was used because none was named |
| **LA3** | *The second route.* LA2's positive kept once with `try-once`, then Analyze > Load Cases | Wind X listed with the number, nature and category `after` gave; every other case unchanged |
| **LA4** | *Never guessed.* `name` "Wind X2", `category` left out; then `category` "gravity"; then `category` wind with `number` DL1's number | `added` 0 each time - refused lists the categories, names "gravity", says the number is DL1's |
