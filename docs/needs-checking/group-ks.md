# Needs checking — Group KS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KS - one multi-rebar annotation across bars in a view: `PLACE_MULTI_REBAR_ANNOTATION` (2026-11-01)

Asked for in the modeller's words: *"add a multi-rebar annotation to these bars"*, *"put a multi-rebar tag
with a dimension line on this rebar set"*, *"annotate these bars with one tag and a dimension"* - Revit's
Annotate > Multi-Rebar, Aligned and Linear. Nothing in Heron placed one: no card calls
`MultiReferenceAnnotation`. `TAG_ELEMENTS_IN_VIEW` and `TAG_MATERIAL_OR_MULTI_CATEGORY` place one tag per
element and no dimension, `CREATE_DIMENSION` and `CREATE_LINEAR_DIMENSION` a dimension and no tag,
`REPORT_HOST_REBAR` reads bars, `SET_REBAR_VIEW_STATE` shows them, and the two rebar numbering cards number
them. `brain/heron_retrieve.py` on each sentence, Revit 2024, offered `PLACE_BEAM_REBAR`,
`CREATE_REBAR_COVER_TYPE` or `REPORT_TAGS_AND_TARGETS` first.

| What | Where | What it does |
|---|---|---|
| `PLACE_MULTI_REBAR_ANNOTATION` (MODIFY) | [`brain/fragments/place-multi-rebar-annotation`](../../brain/fragments/place-multi-rebar-annotation/fragment.yaml) | Places **one** multi-rebar annotation - Revit's dimension across the bars and the tag its type names - of the multi-rebar annotation type the modeller **names**, by its type name or *Family: Type*. **None named, or one not in the model: nothing is placed** and the types come back with their reference category, tag family and dimension style - even when only one exists. `style` aligned (left out) or linear; `direction` horizontal or vertical, left out along the first bar set's spread across the view; `offsetMm` paper millimetres from the bars, times the view scale. Each bar handed in is named in one list: `notRebar`, `notVisible` (not drawn in the view), `alreadyAnnotated` (a multi-rebar annotation's dimension in that view already witnesses it), `notAligned` (refused by Revit's own check bar by bar), or `dimensioned`. If the bars left fail Revit's checks together, nothing is placed and the check is named. What is made is read back - its type, its dimension in the view witnessing the bars, its tag - and a wrong one removed in the same undo |

**Why a new card and not a widening.** A multi-rebar annotation is one Revit element owning a dimension and
a tag, made by its own call from a type that names both; no card's contract has that type, and the tagging
and dimensioning cards each make one of the two parts through other calls.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `MultiReferenceAnnotation.Create`, `DimensionId`, `TagId` and its
five static checks, the `MultiReferenceAnnotationOptions` members the card sets and asks, the type's
`ReferenceCategoryId`, `TagTypeId` and `DimensionStyleId`, `IndependentTag.MultiReferenceAnnotationId`,
`RebarShapeDrivenAccessor.Normal` and `RebarInSystem.Normal` are present and unchanged 2020-2027.
`MultiReferenceAnnotationOptions.ReferenceCategory`, named in the request, exists on no release; and
`Dimension.MultiReferenceAnnotationId`, which Revit documents, is in no release's reference assembly - the
card reads the dimension through the annotation instead. **Nothing here has run in Revit.** The card is
DRAFT; its risk is MODIFY - it adds one annotation and deletes nothing it did not just make.

**Not known from the assemblies, and recorded here rather than guessed** (KS1 to KS5 measure them): whether
the metric structural template carries a multi-rebar annotation type at all; that Aligned is
`DimensionStyleType.Linear` and Linear is `LinearFixed` (read from Revit's own text for the checks, not
seen on a model); whether a set's shape-driven `Normal` is the way its bars spread, which the default
direction relies on; what Revit's check says of C1's tie set, which lies in the plan; that the annotation's dimension names
each bar it witnesses by `Reference.ElementId` - the read-back and `alreadyAnnotated` both rely on it, and
if it does not, KS2 comes back *"its dimension witnesses none of the bars - and was removed"*, a finding
against the card; and where the dimension and tag land beside the column at 10 paper millimetres.

**Routing.** Measured through `heron_retrieve.find` on two fresh scratch stores - the tree at `eedfcf4`
without the card, and with it - `model` backend, re-ranker absent, no Revit filter and Revit 2024, over
every declared utterance and routing-table sentence of every live card (4,321): the three sentences reach
this card; no declared utterance and no `TAG_ELEMENTS_IN_VIEW` sentence moved. One routing-table sentence
moved between two writes neither its table names - *"put it back"*, `SHOW_ELEMENTS`' and unreached before -
recorded with five undeclared rebar sentences as [row 5b-1375](../fragment-issues/section-5b-rows-401-425.md),
OPEN. `tools/check-routing.py`: no new crossing; on the keyword route alone *"add a linear dimension"* now
ranks this card first where `CREATE_DIMENSION` did, and the lookup answers it on `CREATE_LINEAR_DIMENSION`
by identity, before and after.

**The model - Group HY's scratch project in Revit 2020**, from Revit's metric structural template, in
`%TEMP%\heron-work\rebar-hy`, **after HY6 is kept**, so column C1 holds two main bar sets and one tie set
([group-hy.md](group-hy.md)). Every run rolled back except KS6's one run. The job file is
[`tools/jobs/place-multi-rebar-annotation-ks-2026-11-01.yaml`](../../tools/jobs/place-multi-rebar-annotation-ks-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). **Its annotation type is left for KS1 to read** and is marked
in the file; until it is filled in the run is refused naming the types - expected, NOT RUN. Every line
below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KS0** | *Open.* The HY scratch project, HY6 kept, open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads the scratch copy's name; C1's bars show in `FloorPlan: Level 1` |
| **KS1** | *Read the types - also the "none named" negative.* This card on C1's bars, view `Level 1`, `annotationType` left out | `placed` 0, *No multi-rebar annotation type was named, and one is never chosen for you*, `typesLoaded` listing each type with its reference category and tag family. Write one into the job file, both places. **If none exists**, record it: making one is a Revit job by hand on the scratch copy, then save and run KS1 again |
| **KS2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file's job | Negative: the columns instead of their bars - `placed` 0, every column in `notRebar`, *Nothing was placed.* Positive: `placed` 1, `annotationId`, `dimensionId` and `tagId` real (tagId invalid only if KS1 showed no tag family), both main bar sets in `dimensioned`. **Record** where the tie set went and what Revit's check said; read in the same rolled-back group, the plan shows one dimension across C1's main bar points beside the column and the tag beyond it |
| **KS3** | *A second run, and a name nobody has.* KS2's positive twice in one rolled-back group; then `annotationType` `KS no such type` | Second run: `placed` 0, both main bar sets in `alreadyAnnotated`. The unknown name: refused, the types named |
| **KS4** | *Linear.* KS2's positive with `style=linear`, `direction` left out; then `direction=horizontal` and `direction=vertical` | `placed` 1 each time, the dimension line on the plan's horizontal or vertical. A refusal from Revit's Linear check is recorded with its words - a finding, not a pass |
| **KS5** | *Where it is refused.* KS2's positive with view `{3D}` (not locked); then a sheet's name | Both refused by name, `placed` 0 |
| **KS6** | *Kept, through `try-once`, on the modeller's yes.* KS2's positive on a saved copy, `Level 1` in front | Keep or Discard; afterwards select the annotation - Properties names its type - and Revit's own Annotate > Multi-Rebar > Aligned across the same bars by hand, then this card: both in `alreadyAnnotated` (the second route). One Ctrl+Z takes the annotation back |
| **KS7** | *Revit 2026 or 2027.* KS2 again on a copy of the same project opened there | The same read-back as on 2020; a difference is a finding against the card, never a pass |
