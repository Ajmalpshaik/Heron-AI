# Needs checking — Group NG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NG - one value, blank or filled, into MANY types in ONE call: `SET_FAMILY_TYPE_VALUES` version 5 in a family, `SET_PARAMETER_VALUES_BY_ID` version 8 in a project (2026-11-11)

On 2026-11-11 (Revit 2020) the owner cleared brand data - Manufacturer, Model, URL, Model_Number,
Family_Source - from three air terminal families of seven types each. Every type took its own
`SET_FAMILY_TYPE_VALUES` call, and each raised a "delete 1 element" question: making a type current
deletes an automatic sketch dimension, the geometry untouched - 21 questions for one job. He asked for
one call that writes a value into many types, in a family and in a project. Why, and what is not known
yet, is [FRAGMENT-ISSUES row 5b-1504](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `SET_FAMILY_TYPE_VALUES` version 5 (MODIFY) | [`brain/fragments/set-family-type-values`](../../brain/fragments/set-family-type-values/fragment.yaml) | `typeName` "all" or a comma list writes the same values into every type it names, in one change, every type read back. A list writes only types the family has - one unknown name refuses the whole call. A type already holding every value is not made current. TEXT into EVERY type is first tried with no type made current (a formula that is the text, set and cleared in a sub-transaction); otherwise each type is made current in turn and the starting type is current again at the end. One type named: version 4's path |
| `SET_PARAMETER_VALUES_BY_ID` version 8 (MODIFY) | [`brain/fragments/set-parameter-values-by-id`](../../brain/fragments/set-parameter-values-by-id/fragment.yaml) | In the key form, a key with a `*` names every element or type whose key matches it, ignoring case, and the row's values go onto all of them - all or nothing for that row, each element that cannot take them named. Every other row: version 7's |

**What is done and what is not.** Both cards compile on all eight releases, 2020 to 2027, with the whole
library (`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). `FamilyType` having no setter,
and `FamilyParameter.CanAssignFormula`, were read from the 2020 RevitAPI.dll by reflection the same day.
**The family card ran on Revit 2024 on 2026-11-12 and is PROVEN - Run 1 below; the project card has not run and stays DRAFT.** The delete-confirmation rule (Article 7, D-126) is NOT changed: a
switch that deletes something is still asked about - once per call now, not once per type.

**Three things only a run can say,** and NG1 asks the first:
1. Whether Revit takes a text parameter's formula that is only a quoted string - `"Acme"`, and `""` for a
   blank - on a built-in identity parameter (Manufacturer, Model, URL) and on a family text parameter,
   and whether every type KEEPS its value when the formula is cleared. If not, the card says "tried and
   did not hold" and switches types; that is a correct run, not a failure - but the card's purpose then
   over-promises and is changed.
2. Whether that formula trial deletes nothing (no Keep or Discard question with a deletion in it).
3. Whether a type's `Family Name` narrows a project's key search (`Family Name=<family>` on the first
   row) - the narrowing is version 2's, never run against a type's family name.

**The models - SAVED scratch files, never the owner's families or jobs, and only with his go-ahead.**
- *Family:* File, New, Family, the metric Generic Model template, Revit 2020, saved as "Heron Many Types
  Test" under `%TEMP%\heron-work\many-types`. Through Heron (`SET_FAMILY_TYPE_VALUES`, one type a call, as
  version 3): seven types "600 x 600", "625 x 625", "300 x 300", "450 x 450", "500 x 500", "750 x 750",
  "900 x 900"; a type length "Width" labelling a dimension between two planes; a family text parameter
  "Model_Number" (`ADD_FAMILY_PARAMETERS`); Manufacturer "Acme", URL "https://example.com", Model_Number
  "M-1" in every type. Save.
- *Project:* a copy of a Revit 2020 sample project saved as "Heron Many Types Project" beside it; the
  saved family above loaded (seven Air Terminal types), and a second air terminal family of two types
  WITHOUT "Model_Number". Save.

Every line with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`. **`--negative-set` REPLACES the values** - repeat every value the negative needs.

| # | Run | Look for |
|---|---|---|
| **NG0** | *Open and arrange, KEEP it.* Both scratch files built as above and saved. Record the family's types and the current type ("600 x 600") | The title bar names the scratch family; the Family Types dialog lists seven types, each with Manufacturer "Acme" |
| **NG1** | *The D-30 negative FIRST, then the positive.* Rolled back. Negative: `typeName=600 x 600, No Such Type`, `values=Manufacturer=Changed`. Positive: `typeName=all`, `values=Manufacturer=; URL=; Model_Number=` | Negative: REFUSED naming "No Such Type" and listing the seven types; nothing written - "600 x 600" still reads "Acme". Positive: `typesWritten` seven, every type reading the three empty; **`route` recorded** - "no type made current" answers questions 1 and 2 above (check the Keep or Discard question held no deletion), "each type made current in turn" with "tried and did not hold" answers question 1 the other way; the current type "600 x 600" |
| **NG2** | *A parameter one type cannot take, by value kind.* Rolled back: `typeName=all`, `values=Manufacturer=Acme; Width=abc` | Refused naming "abc" as not a number for Width - no type written |
| **NG3** | *Lengths through a list, KEPT.* Through `try-once` (or `revit_change` once PROVEN): `typeName=600 x 600, 625 x 625`, `values=Width=900`, and KEEP | Both read Width 900 mm in the Family Types dialog, the five others unchanged; `route` "each type made current in turn"; the dimension reads 900 in each when that type is chosen; "600 x 600" current again; ONE entry in the undo list; the Keep or Discard question asked ONCE, its deletions (if any) counted once |
| **NG4** | *Already held.* NG3's call again | Nothing written, `alreadyHeld` both, no type made current, no question |
| **NG5** | *Project, the negative FIRST, then the positive.* Rolled back, in the scratch project. Negative: `rows=key=Type Name|in=Air Terminals|types=yes;*|Model_Number=X` - the second family's two types lack Model_Number. Positive: `rows=key=Type Name|in=Air Terminals|types=yes|Family Name=Heron Many Types Test;*|Manufacturer=|URL=|Model_Number=` | Negative: that row REFUSED naming each type without Model_Number, "NONE of the nine is written" - the seven types' Model_Number still "M-1". Positive: `written` 7, the three empty on all seven in Type Properties, the second family untouched - **this also answers question 3** (Family Name narrows a type search) |
| **NG6** | *A name pattern.* Rolled back: `rows=key=Type Name|in=Air Terminals|types=yes;6*|Comments=six` | "600 x 600" and "625 x 625" written, no other type |
| **NG7** | *Pattern matching nothing; a pattern and an exact row on one type.* Rolled back: `...;ZZZ*|Comments=x` then `...;6*|Comments=x;600 x 600|Comments=y` | First: refused, "no type ... matching 'ZZZ*'". Second: both rows refused - "another row's key names it too" - nothing written |
| **NG8** | *Second route.* By hand on a copy: the Family Types dialog, each type chosen and its three values cleared; compared with NG1's positive kept on another copy | The two copies read the same in every type |

**Before the owner's own families.** The three air terminal families he cleared by hand on 2026-11-11 are
done; nothing here is run on them or on any job. The first use on a real family after NG passes is his
choice, through `try-once`, behind Revit's Keep or Discard.

## Run 1 - Revit 2024, 2026-11-12 (session 13164) - the family card PROVEN, the project card not run

**Model:** the scratch family `%TEMP%\heron-work\many-types\Heron Many Types Test.rfa` (metric Generic Model),
made by `CREATE_FAMILY_DOCUMENT` this session - never the owner's own families. Built through Heron with
version 4: seven types, a type length Width labelling the dimension between Box Left and Box Right (EQ about
the centre), a box locked to the planes, a text parameter Model_Number and an Air Flow Nominal Air Flow;
every type holding Manufacturer "Acme", URL "https://example.com", Model_Number "M-1", 100 L/s. Every run
below was `fragment --write` or `validate --write` from the branch, ROLLED BACK. Revit 2024 was the only
Revit connected, so this is evidence about 2024 alone.

| Row | Positive | Negative | Verdict |
|---|---|---|---|
| NG1 | `typeName=all`, Manufacturer, URL, Model_Number blank: `typesWritten` 7, all 21 values read back "" (was "Acme", the URL, "M-1"), `route` "each type made current in turn", "600 x 600" current again - **the proof, signed by Ajmal PS** | `typeName=600 x 600, No Such Type`: refused, "This family has no type "No Such Type", so NO type in the list was written", the seven types listed | held |
| NG1b | Question 1 settled: `Model_Number=` (blank), `Model_Number=M-2` and `Manufacturer=Beta` each into all seven took `route` "no type made current" and read back in every type | `URL=` alone: `route` "each type made current in turn" - Revit gives a URL parameter no formula (`CanAssignFormula` false). The first NG1 run said nothing about why; the answer now names it ("Revit gives "URL" no formula") | held. A call with URL in it switches types |
| NG2 | - | `values=Manufacturer=Acme; Width=abc`: refused, ""abc" is not a number for the length "Width"", `typesWritten` 0 | held |
| NG3 | `typeName=600 x 600, 625 x 625`, `Width=900`: both read 900 mm (were 600 and 625), "These parameters drive 1 labelled dimension(s) ... every labelled dimension reads its parameter in every type written", one change, "600 x 600" current again | - | held (rolled back, not kept) |
| NG4 | `typeName=all`, Manufacturer "Acme" and Model_Number "M-1", already held: `alreadyHeld` 7, `typesWritten` 0, "nothing was written and no type was made current" | - | held |
| NG-flow | `typeName=all`, `Nominal Air Flow=200 L/s`: seven types read 200.0 L/s (was 100.0) - version 4's measured kind through the many-types path | - | held |
| NG-rename | - | `typeName=600 x 600, 625 x 625`, `Type Name=Big`: refused, "A rename names one type" | held |
| NG-one | `typeName=625 x 625`, `Width=650`: `askedMany` false, "Wrote into the type "625 x 625"" - version 4's road | - | held |
| NG5, NG6, NG7 (project) | - | - | NOT RUN - the owner said the project card is not needed now; `SET_PARAMETER_VALUES_BY_ID` version 8 stays DRAFT |
| NG8 | - | - | NOT RUN - no second route |

**Still not known:** question 2 - whether a type switch, or the formula trial, deletes anything - was not
counted: the command-line runs do not print the change counts, and on Revit 2024 the Keep or Discard question
was never raised (every run rolled back). The first kept many-types call on Revit 2020, where the owner met the
deletions, answers it. The other releases are not run.

## Run 2 - Revit 2020, 2026-11-13 (session 41776) - the project card, NG5 to NG7

**Models, all made through Heron this session and saved under `%TEMP%\heron-work\many-types-2020`** - never
the owner's families or jobs. Run 1's family is a Revit 2024 file and cannot open in 2020, so the project
half was arranged again on 2020, with every name the run depends on changed and said:
- *Family* "Heron Many Types 2020.rfa" (`CREATE_FAMILY_DOCUMENT` from 2020's `Metric Generic Model.rft`;
  `SET_FAMILY_SETTINGS` Category=Air Terminals; `ADD_FAMILY_PARAMETERS` Model_Number, Text, type, Identity
  Data; seven types by `SET_FAMILY_TYPE_VALUES` one a call, each read back Manufacturer "Acme", URL
  "https://example.com", Model_Number "M-1"). **No Width and no dimension** - the project rows do not use them.
- *Second family* "Heron Two Types 2020.rfa": Air Terminals, types "Small" and "Large", Manufacturer
  "Other Maker", no Model_Number.
- *Project* "Heron Many Types Project 2020.rvt" from 2020's `US Metric\DefaultMetric.rte`, both families
  loaded with `LOAD_FAMILY`. `SELECT_TYPES` Air Terminals: **9**, these nine and no other.

Every run `--write` without `--apply`: rolled back. Afterwards `READ_ELEMENT_PARAMETERS` on the nine read
Model_Number "M-1" on the seven (2 absent), Manufacturer "Acme" on the seven and "Other Maker" on the two,
URL on the seven, Type Comments blank on all nine - nothing kept.

| Row | Positive | Negative | Verdict |
|---|---|---|---|
| NG5 | `validate`, `rows=key=Type Name\|in=Air Terminals\|types=yes\|Family Name=Heron Many Types 2020;*\|Manufacturer=\|URL=\|Model_Number=` (the family's name is this run's): `written` 7, *"'*' named 7 type(s) in Air Terminals types with Family Name 'Heron Many Types 2020', and all 7 were written"*, all 21 values read back "" - the second family not named. **A type's Family Name narrows a key search - the third thing only a run could say** | `rows=...;*\|Model_Number=X`: refused, *"the pattern names 9 type(s) and 2 cannot take these values - 'Heron Two Types 2020: Small' (id 347041): it has no parameter called Model_Number; 'Heron Two Types 2020: Large' (id 347043): ... - so NONE of the 9 is written"*, `written` 0 | **PASS** - draft `brain/proof-drafts/set-parameter-values-by-id.yaml`, gap *no second route*; not signed |
| NG6 | `...;6*\|Comments=six`: refused - *"it has no parameter called Comments"* for 600 x 600 and 625 x 625, NONE written. The pattern named those two and no other; a type has **Type Comments**, not Comments | - | **FAIL as written - the row's premise, not the card** ([row 5b-1634](../fragment-issues/row-5b-1634.md)). Measured beside it, not counted: `Type Comments=six` wrote 2, both read back "six" |
| NG7 | - | (a) `...;ZZZ*\|Comments=x`: refused, *"no type in Air Terminals types has a Type Name matching 'ZZZ*' - not written"*. (b) `...;6*\|Comments=x;600 x 600\|Comments=y`: both rows refused - *"600 x 600: another row's key names the same element (id 346225)"*, and the pattern's *"another row's key names it too"* - `written` 0 | **PASS** - (b)'s 625 x 625 half was also refused for the missing Comments (row 5b-1634) |
| NG8 | - | - | NOT RUN - no second route |

**Still not known:** question 2 (whether a type switch deletes anything) - this family has no sketch
dimension to lose, so it cannot answer it.
