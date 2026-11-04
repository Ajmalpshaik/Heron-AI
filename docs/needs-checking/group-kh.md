# Needs checking — Group KH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KH - area tags in an area plan: `TAG_AREAS` (2026-11-01)

Asked for in the modeller's words: *"tag all the areas in this area plan"*, *"put area tags on these
areas"* - Revit's Annotate > Area Tag. Nothing in Heron placed an area tag. `brain/heron_retrieve.py` on
both sentences, Revit 2024, offered `PLACE_AREAS` first, then `CREATE_AREA_PLAN`, `TAG_ROOMS` and
`REPORT_AREAS`.

| What | Where | What it does |
|---|---|---|
| `TAG_AREAS` (MODIFY) | [`brain/fragments/tag-areas`](../../brain/fragments/tag-areas/fragment.yaml) | Places one area tag on each area handed in - the selection, or `FILTER_ELEMENTS_BY_CATEGORY` with Areas and the plan's level - in **one area plan**, of the area tag type the modeller **names**. Any other view is refused by name, nothing placed. **No tag type named, or one no area tag type has: nothing is placed** and the loaded area tag types are listed; a room tag type is named with its own category. Named and left, each with its reason: not an area, another area scheme, another level, already tagged in this plan, not placed, not enclosed, refused by Revit. Every tag is read back - its type and its area - and a wrong one removed inside the same undo |

**Why a new card and not a widening.** `TAG_ROOMS` is PROVEN, so any change under its `impl/` would make
its proof stale; it was not edited. And an area tag needs an area plan of the area's own scheme, where
a room tag needs a floor plan - the view rule, the scheme rule and the creation call all differ.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: `Creation.Document.NewAreaTag(ViewPlan, Area, UV)`,
`AreaTag.Area`, `ViewPlan.AreaScheme`, `Area.AreaScheme` and `BuiltInCategory.OST_AreaTags` are present
and unchanged 2020-2027. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KH3 and KH4 measure them):
whether `NewAreaTag` makes the project's default area tag type or the last one used - the card sets the
named type either way and reads it back; whether a dependent view of an area plan shares its area tags
with its primary, as `TAG_ROOMS` believes of room tags; and whether an area tag placed at an area's own
point lands inside it.

**Routing.** Checked through `heron_retrieve.find` on two scratch stores, before and after the card, over
every declared utterance and routing-table sentence: none changed the card it reaches. Two questions
nobody declares - *"which areas are not tagged"*, *"are all the areas tagged"* - moved from
`FIND_UNTAGGED_ELEMENTS` (a read) to this card, which writes. That read cannot see area tags either.
Recorded on the card, as `TAG_ROOMS` records the same for rooms; not repaired by declaring words nobody
said.

**The model - the Project2 scratch model in Revit 2020**, never saved, where `CREATE_AREA_PLAN`,
`PLACE_AREAS` and `REPORT_AREAS` were proved on 2026-10-18. Every run below is rolled back except KH1's
arrangement and KH5's one run. The job file is
[`tools/jobs/tag-areas-kh-2026-11-01.yaml`](../../tools/jobs/tag-areas-kh-2026-11-01.yaml) (`--dry-run`
reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KH0** | *Open.* The Project2 scratch model open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads Project2 |
| **KH1** | *Arrange, and KEEP it.* By hand, or with `create-area-plan`, `create-area-boundary-lines` and `place-areas` kept: a Gross Building area plan of Level 1 called **Heron Area Plan**, boundary lines on the outside of the walls, and two areas A1 and A2 in it, with **no area tag** - Revit's own Area tool tags each area as it is placed unless *Tag on Placement* is off, so delete any it made | Project Browser > Area Plans (Gross Building) shows Heron Area Plan; the plan shows A1 and A2 and no area tag |
| **KH2** | *Read the types - also the "none named" negative.* `tag-areas` with `--write`, `view` Heron Area Plan, `tagType` left out, the areas of Level 1 from `filter-elements-by-category` | `tagged` 0, *No area tag type was named, and one is never chosen for you*, `typesLoaded` listing the area tag types. Write the one to use into the job file, both legs |
| **KH3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: the walls of Level 1 - `tagged` 0, every wall in `notAreas`. Positive: `tagged` 2, `newTags` 2 ids, *2 area tag(s) placed in "Heron Area Plan" (Gross Building)*. **Record** whether either new tag needed its type changed |
| **KH4** | *The refusals.* KH3's positive with `view` Level 1 (a floor plan); then with `tagType` a room tag type the model has | First: refused *"Level 1" is a FloorPlan view, not an area plan*, nothing placed. Second: refused naming the room tag type and its category, nothing placed |
| **KH5** | *Kept, through `try-once`, on the modeller's yes.* KH3's positive on a saved copy, Heron Area Plan in front | Keep or Discard. Then the same run again tags nothing - both areas in `alreadyTaggedHere` - and Revit's own Select All Instances > Visible in View on one new tag counts 2: the second route |
| **KH6** | *Revit 2024 or later.* KH3 again | The same answers. Nothing the card calls changed between releases |
