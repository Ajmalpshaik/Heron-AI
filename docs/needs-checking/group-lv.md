# Needs checking — Group LV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LV - Manage > Project Information written - project number, client name, address, building name, status or an added shared parameter, each by its exact name and read back: `SET_PROJECT_INFORMATION` (2026-11-02)

Asked for in the modeller's words: *"set the project number to QA-2026-114"*, *"change the client name to
Ashghal"*, *"fill in the project address and building name"*, *"set the project status to Issued for
Construction"* and *"set the shared parameter Project Code on project information to PRJ-77"*. No card wrote
Project Information: `REPORT_PROJECT_INFORMATION`'s own routing table answered *"set the project number"* with
*"nothing in Heron yet"*, and of every fragment's C# only that read and `LIST_EXPORT_SETUPS` named
`ProjectInformation`. heron-duplicate-checker's method, run before writing: `brain/heron_merge.py` found no
shared signature with a shared capability; the duplicate check (`0 already here`) and the Matcher (`0 match(es),
0 partial`) found nothing equal. **Look-alikes, kept separate:** `REPORT_PROJECT_INFORMATION` (PROVEN) is the read
half - a read widened into a write would put a Keep or Discard in front of every question it answers (D-86);
`WRITE_ELEMENT_PARAMETERS` (PROVEN) writes one value onto elements a selection or a filter hands in, reports them
by id, gives no before value and no list of names - Project Information is never selected, and widening it would
put its proof out of date; `SET_PARAMETER_VALUES_BY_ID` (DRAFT) writes a different value per named element
against a table read earlier; `SET_GLOBAL_PARAMETER` (DRAFT) writes a global, which is its own element - a Project
Information parameter driven by one is refused here and pointed there.

| What | Where | What it does |
|---|---|---|
| `SET_PROJECT_INFORMATION` (MODIFY, Revit 2020-2027) | [`brain/fragments/set-project-information`](../../brain/fragments/set-project-information/fragment.yaml) | Takes one or more *"Parameter Name = value"* pairs and writes them to the Project Information element only. A name is matched **exactly** - Revit's own label or an added project or shared parameter's name; one matching nothing is refused and every name Project Information carries is listed. Read-only parameters, ones driven by a global parameter, ones holding another element, a name two parameters share, and a value of the wrong kind are refused by name - and then **nothing** is written, not even the good pairs. Text as typed; a whole number or a Yes/No as such; any other number through Revit's own parse in the project's units, no factor applied. Every value read back after one regeneration, before and after in `changes`; one already holding the value is `alreadyThat` |

**What is done and what is not.** It compiles on every release 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-02, a Linux container). Every Revit member it calls was read off the reference assemblies of all eight
releases with `tools/api-surface --members` - the list is in the card's `compatibility-note`. **Nothing here has
run in Revit.** The card is DRAFT. **Only Revit can settle:** that `GetParameters` finds Revit's ten by the labels
the dialog shows, and an added shared parameter by its name (LV2, LV3); that a refused request leaves every value
as it was (LV2); that a number parameter takes text in the project's units the way the dialog does (LV4). **Not
done:** a parameter holding another element (a material, a level, a type), the Energy Settings and Route Analysis
buttons, and line breaks inside the address - an address is set on one line.

**Routing.** `heron_retrieve.find` on fresh scratch stores of the tree at `ddda3a8` and of this change, over the
five sentences and twenty-five near ones - `REPORT_PROJECT_INFORMATION`'s utterances and routing rows,
`WRITE_ELEMENT_PARAMETERS`' and `SET_GLOBAL_PARAMETER`'s - on Revit 2024, 2027 and no Revit named, with and
without the host's question flag. The five reach this card without the flag (before: the read, or
`REPORT_GLOBAL_PARAMETERS`); with it they reach a read. *"What is the project number"*, *"who is the client"* and
every other question of the read still reach `REPORT_PROJECT_INFORMATION`, with and without the flag. One
write's sentence moved between two reads with the flag only - *"fill in the parameter"*, from
`REPORT_PARAMETER_INVENTORY` to `REPORT_SHARED_PARAMETER_FILE`; without the flag it still reaches
`WRITE_ELEMENT_PARAMETERS` - recorded, not tuned. `tools/check-routing.py` on scratch stores: no question
answered by a write before or after; claimed in a routing table and not reached 69 before, 68 after (one line
reached by chance, none lost). `REPORT_PROJECT_INFORMATION`'s routing row *"set the project number -> nothing in
Heron yet"* now names this card - card text only, its fingerprint unchanged.

**The model** - a scratch project from Revit's default architectural template, saved under `%TEMP%\heron-work`,
never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/set-project-information-lv-2026-11-02.yaml`](../../tools/jobs/set-project-information-lv-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LV1** | *Arrange.* A new project from the default architectural template. Manage > Project Information: Project Number *0001*, Client Name *Owner*, Building Name empty. A scratch shared parameter file with a Text parameter *Project Code*, bound by Manage > Project Parameters as an instance parameter to Project Information, left empty. Write down all values. Save under `%TEMP%\heron-work` | The dialog shows *Project Code* under its group, empty |
| **LV2** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's job, `--write`, rolled back | Negative, *"Project Number=QA-2026-114\|Client Nme=Ashghal"*: refused *"NOTHING WAS WRITTEN ... no parameter named exactly 'Client Nme'"*, `parameterNames` listing *Client Name* and *Project Code*; Project Number still *0001*. Positive: `written` 1, `changes` *"Project Number: 0001 -> QA-2026-114"*. **A good half written while the other half was refused is a defect, never a pass** |
| **LV3** | *Several, and a shared parameter.* `validate set-project-information --write --set "values=Project Address=12 Corniche Road, Doha\|Building Name=Tower B\|Project Code=PRJ-77"`, rolled back | `written` 3; `changes` with each before and after; ONE entry in Revit's undo list when kept through `try-once` on the modeller's yes |
| **LV4** | *A number and a refusal of the wrong kind.* On a second scratch copy with *Project Code* re-made as a Number parameter: `values=Project Code=PRJ-77`, then `values=Project Code=12.5`, each rolled back | The first refused naming *Project Code* and *PRJ-77*, nothing written; the second `written` 1 and the dialog showing 12.5 as the project shows numbers |
| **LV5** | *Already that, and a family.* `values=Client Name=Owner` on LV1's project; then any values with a family open in front | `alreadyThat` *["Client Name"]*, `written` 0; then `notAProject` true and nothing written |
| **LV6** | *The second route.* Manage > Project Information by hand on the same copy, the same values typed | The same labels; a title block on a sheet showing the new values; `REPORT_PROJECT_INFORMATION` after the run giving `changes`' after column |

**Written back 2026-11-12, read in a cloud container (no Revit) - nothing was run here.** Since this group was
written: `SET_PROJECT_INFORMATION` reads PROVEN on its card, at version 2 - its `proof` block is signed by Ajmal PS,
2026-11-07, on Test for Issue (6,120 elements), Revit 2024, session 16836. So where this page calls a card named here
DRAFT, or says nothing has run in Revit, that was true when it was written and is out of date as a statement about the
card. Whether LV1 to LV6 were what that proof ran is NOT sorted here: for the owner.
