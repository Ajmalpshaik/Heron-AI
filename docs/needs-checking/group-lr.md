# Needs checking — Group LR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LR - dimension types read, and one changed or copied under a new name - text size, font, tick mark, units format, colour: `REPORT_DIMENSION_TYPES`, `SET_DIMENSION_TYPE` (2026-11-02)

Asked for in the modeller's words: *"what dimension types are in this project"*, *"make a new linear dimension
type called 2.5mm Arial from Linear - 3mm Arial"*, *"set the text size of dimension type X to 2.5 mm"*, *"change
the tick mark of dimension type X to Diagonal 3mm"* and *"set dimension type X units to millimetres with no
decimals"* - Revit's Annotate > Dimension > Types and their Type Properties. heron-duplicate-checker's method, run
before writing: `brain/heron_merge.py` found no shared signature with a shared capability, and the duplicate
check and the Matcher found nothing equal. `SET_TEXT_TYPE` and `REPORT_TEXT_TYPES` (both PROVEN) are the same
job for **text** types - a label's or a text note's - and these cards copy their shape; a dimension type is
another class with a tick mark, witness lines and a units format of its own - a **look-alike**, and widening
either would make its proof stale. `DUPLICATE_TYPE` (PROVEN) copies the type behind a selection and sets nothing;
`WRITE_ELEMENT_PARAMETERS` (DRAFT) writes one parameter on selected elements in project units; `SET_PROJECT_UNITS`
(PROVEN) sets the project's units and its own routing table sends a dimension style's units away from it - each a
look-alike. Nothing was widened; two cards carry one declared sentence each for routing (below).

| What | Where | What it does |
|---|---|---|
| `REPORT_DIMENSION_TYPES` (READ) | [`brain/fragments/report-dimension-types`](../../brain/fragments/report-dimension-types/fragment.yaml) | Lists every named dimension type - or one by name, or one style - with its style and what Type Properties shows, **each setting under Revit's own name for it**: text size in paper mm, font, width factor, bold, italic, underline, text background, text offset, tick mark, line weights, colour, dimension line extension, witness line control, gap and extension, and **Units Format** - the type's own unit, rounding and symbol, or *Use project settings* with the project's. Counts the dimensions on each. A name matching none is answered with the types there are |
| `SET_DIMENSION_TYPE` (MODIFY) | [`brain/fragments/set-dimension-type`](../../brain/fragments/set-dimension-type/fragment.yaml) | Changes the one dimension type **named** - never guessed - or first **copies it under a new name** and changes the copy: text size (paper mm), font, width factor, bold, italic, underline, text background, text offset, **tick mark by an arrowhead type's exact name** (else the arrowhead types are listed), line weights, colour, line and witness line lengths, and **Units Format** - a unit Revit offers for the type's kind, a rounding it takes, a symbol or none, or the project's settings. A new name already used is refused, named, and nothing is made. Every value is checked first and **read back** after one regeneration; anything not as asked **throws**, so the copy and the changes go back together. `typeBefore` and `typeAfter` are the type as read before and after |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`, the same 2020 to 2027: `DimensionType` `StyleType`, `GetUnitsFormatOptions` and
`SetUnitsFormatOptions(FormatOptions)`, `ElementType.Duplicate(string)`, the `FormatOptions(FormatOptions)`
constructor, `UseDefault`, `Accuracy`, `IsValidAccuracy(double)`, and every built-in parameter both cards name.
The unit and symbol go by reflection across the 2021 units change, as `SET_PROJECT_UNITS` does:
`GetSpecTypeId`, `UnitUtils.GetValidUnits`, `FormatOptions.GetValidSymbols`, `SetUnitTypeId`, `SetSymbolTypeId`
and `LabelUtils.GetLabelForUnit` / `ForSymbol` from 2021; `UnitType`, `UnitUtils.GetValidDisplayUnits`,
`GetValidUnitSymbols`, `DisplayUnits`, `UnitSymbol` and `LabelUtils.GetLabelFor` on 2020. **Nothing here has run
in Revit.** Both cards are DRAFT. **Only Revit can settle** which built-in parameter Type Properties calls *Tick
Mark* on a dimension type (`DIM_LEADER_ARROWHEAD` is the card's reading, not the assemblies' - LR3); whether a
dimension type's text background is `DIM_TEXT_BACKGROUND` or `TEXT_BACKGROUND`; whether the unnamed dimension types
Revit keeps for itself are all the ones the collector returns with an empty name; and the 2020 enum route, which
only a 2020 run can show. **Not done:** alternate units, prefix and suffix (2022 on only), equality text, ordinate
settings, witness line control and centerline settings as writes - they are read where the type has them, and
asking to change one is refused naming what can be changed; and moving dimensions onto the copy -
`CHANGE_ELEMENT_TYPE` does that.

**Routing.** `tools/check-routing.py` on its own scratch store, on a copy of the tree without the two cards and on
this change (`model` backend): no question answered by a write; claimed in a routing table and not reached 67
before and 67 after, the same lines - once two sentences these cards had pulled were declared on the cards that
claim them ([row 5b-1404](../fragment-issues/section-5b-rows-401-425.md), FIXED). The five declared sentences
reach their card by identity; through the host's lookup, with and without its question flag, on Revit 2024 and
with no Revit filter, the question reaches `REPORT_DIMENSION_TYPES` every time, and each change reaches
`SET_DIMENSION_TYPE` without the flag and the read with it. A paraphrase nobody has said, *"what tick mark does
the Linear - 3mm Arial dimension type use"*, reaches `SET_DIMENSION_TYPE` without the flag - the read with it;
recorded, not declared, because nobody said it.

**The model** - "Heron Dimension Type Test", a scratch project from Revit's metric Architectural template, saved
under `%TEMP%\heron-work`, never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The
job file is [`tools/jobs/dimension-types-lr-2026-11-02.yaml`](../../tools/jobs/dimension-types-lr-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LR1** | *Arrange.* "Heron Dimension Type Test" from the metric Architectural template; in the floor plan "Level 1" three walls and three linear dimensions on "Linear - 3mm Arial" (or the template's nearest linear type - write its name into the job file); "Diagonal 3mm" among Manage > Additional Settings > Arrowheads. Save | The linear type's name, its Text Size and Tick Mark in Type Properties, and whether Units Format has *Use project settings* ticked |
| **LR2** | *The read - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job | Negative, "No Such Dimension Type": `typeCount` 0, `dimensionTypes` empty, the finding listing the types. Positive: `typeCount` 1, Text Size 3 mm, the tick mark's name, Units Format as Type Properties shows it, *used by 3 dimension(s)*. Then `dimensionTypeName` left out: every type Annotate > Dimension > Types lists, no more |
| **LR3** | *Which parameter is the Tick Mark.* LR2's positive line | **RECORD the name the tick mark is printed under.** *Tick Mark* confirms `DIM_LEADER_ARROWHEAD`; any other name (*Leader Arrowhead*, *Interior Tick Mark*) means the card's reading is wrong and both cards go back to the author |
| **LR4** | *The change - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back | Negative, Tick Mark *No Such Tick*: refused listing the arrowhead types, NOTHING WAS CHANGED, no copy. Positive, copy as "2.5mm Arial" with Text Size 2.5: `typeAfter` *Text Size 2.5 mm*, `typeBefore` 3 mm, `usedBy` 0 |
| **LR5** | *Tick mark, colour and units, by hand.* `validate set-dimension-type --write` with `changes` *"Tick Mark=Diagonal 3mm; Color=255,0,0"*, then *"Units Format=Millimeters, 0 decimal places"*, then *"Units Format=Degrees"*, each rolled back | The first two read back in `typeAfter`; the third refused listing the length units Revit offers. A name already used for `duplicateAs`: refused naming that type |
| **LR6** | *The second route.* Type Properties of the type after a Keep through `try-once`, on the modeller's yes | Text Size, Tick Mark, Color and Units Format as `typeAfter` says; Undo takes the change back as one entry |
