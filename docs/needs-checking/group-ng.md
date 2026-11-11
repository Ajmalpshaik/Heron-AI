# Needs checking — Group NG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NG - one value, blank or filled, into MANY types in ONE call: `SET_FAMILY_TYPE_VALUES` version 4 in a family, `SET_PARAMETER_VALUES_BY_ID` version 8 in a project (2026-11-11)

On 2026-11-11 (Revit 2020) the owner cleared brand data - Manufacturer, Model, URL, Model_Number,
Family_Source - from three air terminal families of seven types each. Every type took its own
`SET_FAMILY_TYPE_VALUES` call, and each raised a "delete 1 element" question: making a type current
deletes an automatic sketch dimension, the geometry untouched - 21 questions for one job. He asked for
one call that writes a value into many types, in a family and in a project. Why, and what is not known
yet, is [FRAGMENT-ISSUES row 5b-1504](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `SET_FAMILY_TYPE_VALUES` version 4 (MODIFY) | [`brain/fragments/set-family-type-values`](../../brain/fragments/set-family-type-values/fragment.yaml) | `typeName` "all" or a comma list writes the same values into every type it names, in one change, every type read back. A list writes only types the family has - one unknown name refuses the whole call. A type already holding every value is not made current. TEXT into EVERY type is first tried with no type made current (a formula that is the text, set and cleared in a sub-transaction); otherwise each type is made current in turn and the starting type is current again at the end. One type named: version 3's path |
| `SET_PARAMETER_VALUES_BY_ID` version 8 (MODIFY) | [`brain/fragments/set-parameter-values-by-id`](../../brain/fragments/set-parameter-values-by-id/fragment.yaml) | In the key form, a key with a `*` names every element or type whose key matches it, ignoring case, and the row's values go onto all of them - all or nothing for that row, each element that cannot take them named. Every other row: version 7's |

**What is done and what is not.** Both cards compile on all eight releases, 2020 to 2027, with the whole
library (`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). `FamilyType` having no setter,
and `FamilyParameter.CanAssignFormula`, were read from the 2020 RevitAPI.dll by reflection the same day.
**Nothing here has run in Revit.** The delete-confirmation rule (Article 7, D-126) is NOT changed: a
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
