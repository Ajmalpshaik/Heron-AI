# Needs checking — Group MU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MU - dimensioning the MEP runs of many plans in one pass by a rule per category, each run measured to the nearest grid, wall, column or run: `DIMENSION_VIEWS_BY_RULES` (2026-11-08)

The owner's ask: [docs/48](../48-studying-bimlogiq.md) section 5 item 3, *"Dimension template for MEP runs -
categories, which faces, spacing, string placement, per view"*, and his own add-in's Auto MEP Dimension tool,
studied for its lessons only - no code, name or wording taken ([D-25](../DECISIONS.md),
[docs/31](../31-studying-the-existing-libraries.md)). Made NEW, at DRAFT, because the cards it overlaps -
`CREATE_DIMENSION`, `CREATE_LINEAR_DIMENSION`, `DIMENSION_ELEMENT_FACES`, `DIMENSION_ROOMS` - are PROVEN, and
widening one would send it back to DRAFT and out of the chat's reach ([D-117](../DECISIONS.md)) until proved
again. The overlap and the merge planned once this is proved are
[FRAGMENT-ISSUES row 5b-1478](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `DIMENSION_VIEWS_BY_RULES` (MODIFY) | [`brain/fragments/dimension-views-by-rules`](../../brain/fragments/dimension-views-by-rules/fragment.yaml) | In every plan named (none named: the **active view**), every straight run of the categories its **rules** name - separated by semicolons on one line, *run category \| measure to \| side \| search band in mm*, measure to being grids, walls, columns, same-service or other-service (a line break is refused: the chat's `revit_change` drops a second line) - gets ONE string square to it, from the closest target on the nearest side (or on each side, `both`) within the band, to the run's centreline. A target counts only when it is PARALLEL to the run, REACHES the run's station along it, is more than 1 mm off its line and - with the crop on - would end inside the crop; for a wall or column the face nearer the run is used. The string sits on the run's stretch inside the crop, `offsetMm` paper mm along it from that stretch's middle. `chain` one: one string; separate: a dimension per gap, all or nothing per run. Type: named, or the project's default LINEAR type - never the first loaded. Runs already dimensioned are left alone, read from every dimension OWNED by the view, its primary or the primary's dependents - never from what the view shows; the same string is never drawn twice whatever `skipDimensioned` says. Every string is read back - fewer references than asked, or a type that will not take, and it is taken out and the run named. Every run left is in `skipped` with its reason counted in `skippedWhy`. Runs seen end-on are always skipped, steep ones with `skipVertical`. Linked models are NOT read. Every plan in the list is one undo |

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-08, a Linux container). Every Revit member it uses is in the
card's compatibility note, looked up with `tools/api-surface --members` or read off `CREATE_DIMENSION`'s,
`CREATE_LINEAR_DIMENSION`'s and `TAG_VIEWS_BY_RULES`' notes. **Nothing here has run in Revit.** The card is
DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (the rows below measure them):
whether a wall's side-face references from `HostObjectUtils.GetSideFaces`, a grid's whole-element reference
and a run's centreline reference, held together in ONE string, are accepted by `NewDimension` in a plan
(each is proven alone on another card, never together - MU4, MU5); whether a column's face references read
through `GetSymbolGeometry` with the view set on the options are accepted (MU6 - CREATE_LINEAR_DIMENSION
proved that route on named faces, not in a batch); whether a string whose line sits at the run's own height
is drawn where the plan shows the run; whether a sloped run's centreline, measured across its plan direction,
is accepted (MU11, only if the copy has one); whether a dimension drawn in one dependent view is OWNED by a
view of that primary's family, so the next dependent finds it (MU9 - the same belief `TAG_VIEWS_BY_RULES`
holds, Group MT's MT8); whether `Dimension.References` read back on a dimension this new holds every
reference asked for; how long a large plan takes.

**Linked models are not read** - grids, walls, columns or runs inside a link are never a target. A link
reference has to be rebuilt from Revit's undocumented stable representation before `NewDimension` takes it,
which is not safe and simple enough for version 1; `findings` says so on every run. **In the Snowdon HVAC
sample the architecture is a link**, so MU1 records which targets the HVAC model holds of its own and draws
what MU5 and MU6 need in the copy.

**Routing.** Measured 2026-11-08 with `tools/score-routing.py --sentences` (44 sentences frozen before the
card was written, then 8 more, after, from what `check-routing` named; lexical and model - the re-ranked
backends NOT ASKED, no cross-encoder on the container) and `tools/check-routing.py` (model, scratch stores),
on this tree against the same tree without the card. Of the 22 sentences wanting `CREATE_DIMENSION`,
`CREATE_LINEAR_DIMENSION`, `DIMENSION_ELEMENT_FACES` or `DIMENSION_ROOMS`, the same reach them after as before
(all 22 on model, 21 on lexical, before and after alike); the 5 wanting a dimension READ card reach it after;
no question of either set reaches this card on either backend. The first two wordings did send questions here, and were changed before this was written -
details in row 5b-1478. None of the PROVEN cards was edited.

**The model - a SAVED scratch copy of Revit's "Snowdon Towers Sample HVAC", Revit 2024.** Never a working
model. Every run below is rolled back except MU1's arrangement and MU7's one run. The job file is
[`tools/jobs/dimension-views-by-rules-mu-2026-11-08.yaml`](../../tools/jobs/dimension-views-by-rules-mu-2026-11-08.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MU0** | *Open.* Revit 2024, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MU1** | *Arrange, and KEEP it.* "FloorPlan: M1" with its ducts and NO dimensions on them (remove any in the copy). **Record** whether M1 shows grids of the HVAC model ITSELF (select one: Properties names no link) and whether the HVAC model holds walls or columns of its own. Then, in the copy: two straight walls of this model either side of one straight duct of M1, parallel to it, about 600 mm from its centreline - **record the duct's id**; a third wall about 4 m from another duct; if a column family can be loaded, one column beside a third duct - **record which**. A second small plan with ducts AND pipes, undimensioned - **record its name**. Save | Each arranged element visible in M1; nothing dimensioned |
| **MU2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: dimensioned 0, every duct of M1 in skipped ("no same-service run within 1 mm" or vertical), skippedWhy's counts adding up to skipped. Positive: dimensioned above 0, each new dimension holding TWO references, dimensionTypeUsed the project's default linear type, perView one line. **Look** at the plan: each string square to its duct, centreline to centreline, 10 paper mm along the duct from the middle of its part shown; a pair of parallel ducts with ONE string, the second named "the same dimension is already in this view" |
| **MU3** | *The rules and values, each refused.* Rules `Ductz \| grids`; `Ducts`; `Ducts \| grids; Air Terminals \| grids`; `Ducts \| floors`; `Ducts \| grids \| left`; then `Ducts \| same-service` with dimensionType a spot elevation or angular type of the model, then `ZZZ No Such Type`; views a 3D view and a section; then, in the chat, rules typed on two lines - each rolled back | Each refused in its own words and **nothing drawn** - the third refusing rule 2 alone and drawing not even the ducts' strings; the dimension type refusals listing the loaded LINEAR types. The two-line chat case is NOT refused (the chat drops the second line before Revit sees it): rulesRead's first line says **1** rule read - the count is what shows the loss |
| **MU4** | *Grids.* Only if MU1 found grids of the model itself: rules `Ducts \| grids \| nearest \| 6000` on M1, rolled back. Otherwise **NOT COVERED** - say so on the row | Each duct parallel to a grid within 6 m measured to the NEAREST such grid, grid line to centreline; a duct square to every grid in skipped "no grid within 6000 mm" - **never** a string to a grid running across the duct |
| **MU5** | *Walls, both sides, separate segments.* Rules `Ducts \| walls \| both \| 1500` with chain `separate`, then with chain left out; then `Ducts \| walls \| both \| 3000` on a duct whose only walls are in the LINKED architecture - each rolled back | First: MU1's duct with TWO dimensions end to end on one line, each to the wall face NEARER the duct (**look**: not the far face), dimensioned counting it once; the 4 m wall's duct in skipped "no wall within 1500 mm". Second: ONE string with THREE references. Third: dimensioned 0 for that duct, findings saying linked models are not read - never a string to a linked wall |
| **MU6** | *Columns.* Only if MU1 placed one: rules `Ducts \| columns \| nearest \| 2000`, rolled back. Otherwise **NOT COVERED** | MU1's duct measured to the column face nearer it - **record** whether Revit took the reference or refused it (skippedWhy names Revit's words) |
| **MU7** | *Kept, through `try-once`, on the modeller's yes.* MU2's positive on the saved copy, M1 in front | Keep or Discard. Then, rolled back: the same call again - dimensioned 0, every duct dimensioned before (and every duct its string reached) in skipped "already dimensioned in this view"; then with skipDimensioned false - nothing new, "the same dimension is already in this view" |
| **MU8** | *A hidden dimension still counts.* After MU7 kept M1's strings: Dimensions off in M1's Visibility/Graphics (or its template); MU2's positive again, rolled back | dimensioned 0 - **nothing new drawn**. A draft reading only what the view shows would draw every string again |
| **MU9** | *Dependent views.* Two dependent views of one plan with crops that overlap and ducts in both, undimensioned - **record their names**; views `<dependent 1>, <dependent 2>`, rules `Ducts \| same-service`, rolled back; then `<the plan>, <dependent 1>, <dependent 2>` | Both perView lines; the second dependent naming as already dimensioned every duct the first's strings hold; in the second call both dependents drawing nothing the plan drew. Select any duct in the overlap: **no** two identical strings on it. **Record** each new dimension's owner view (Properties or `READ_ELEMENT_PARAMETERS`) |
| **MU10** | *The crop edge.* A dependent view whose crop cuts a long duct so its middle is OUTSIDE - **record which**; rules `Ducts \| same-service`, rolled back | That duct's string inside the crop, on the part of the duct the view shows; a duct with no part inside in skipped "no part of it inside the view's crop"; any target whose end of the string would fall outside the crop passed over and counted in findings |
| **MU11** | *Vertical and short.* A plan with risers - vertical ducts or pipes - **record which**; rules `Pipes \| same-service` (or Ducts), rolled back; then `Ducts \| same-service` on M1 with minRunMm 100000; then, only if the copy has a SLOPED run, skipVertical false on its plan - else **NOT COVERED** | Risers in skipped "vertical - seen end-on in the plan"; every duct in skipped "shorter than 100000 mm" (or vertical); a sloped run's string **looked at** - square to its plan direction, or Revit's refusal recorded |
| **MU12** | *Two plans, two rules, one undo.* Views `FloorPlan: M1, <MU1's second plan>`, rules `Ducts \| same-service; Pipes \| same-service, other-service \| nearest \| 2000`; then the second route | perView two lines; rulesRead opening "2 rule(s) read, of 2"; ONE entry in Revit's undo list - one Ctrl+Z takes every string in both plans away. Second route: select three new dimensions and read their value and references in Properties, against what `MEASURE_DISTANCE` reports between the same two elements |
