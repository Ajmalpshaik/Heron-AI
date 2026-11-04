# Needs checking — Group KI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group KI - material tags and multi-category tags: `TAG_MATERIAL_OR_MULTI_CATEGORY` (2026-11-01)

Asked for in the modeller's words: *"add material tags to these walls"*, *"tag the materials of this
floor"*, *"multi-category tag these elements"*, *"tag these with the multi-category tag"* - Revit's
Annotate > Material Tag and Multi-Category Tag. Nothing in Heron placed either on purpose.
`brain/heron_retrieve.py` on each sentence, Revit 2024, offered `TAG_ELEMENTS_IN_VIEW`, `TAG_ROOMS` and
`COPY_TAGS_TO_REPLACEMENTS` first.

| What | Where | What it does |
|---|---|---|
| `TAG_MATERIAL_OR_MULTI_CATEGORY` (MODIFY) | [`brain/fragments/tag-material-or-multi-category`](../../brain/fragments/tag-material-or-multi-category/fragment.yaml) | Places one material tag or one multi-category tag on each element handed in, in one plan, section, elevation, detail view or locked 3D view, of the tag type the modeller **names** - a Material Tags type makes material tags, a Multi-Category Tags type multi-category tags. Any other view is refused by name. **No tag type named, or one neither kind has: nothing is placed** and both kinds' loaded types are listed; a wall tag or room tag type is named with its own category. An element already carrying **that kind** of tag in the view is left and named - a wall tag does not stop a material tag. Rooms, spaces and areas, types and annotation, elements with no material (for a material tag) and elements with no location are named with the reason. Every tag is read back - its kind, type and element - and a wrong one removed inside the same undo |

**Why a new card and not a widening.** `TAG_ELEMENTS` would hand a multi-category tag type to the same
Revit call, but it is PROVEN and any change under its `impl/` would make its proof stale; it skips an
element carrying *any* tag, never checks the kind of tag type, and lists nothing when none is named.
**Why one card for both kinds:** the same call, the same inputs and the same answer, with the tag type
deciding which - as `TAG_ROOMS` carries rooms, spaces and zones. No neighbouring card was edited.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of
all eight with `tools/api-surface --members`: both `IndependentTag.Create` overloads, `IsMaterialTag`,
`IsMulticategoryTag`, `TagText`, `Element.GetMaterialIds(bool)`, `Options.ComputeReferences`,
`Face.MaterialElementId`, `View3D.IsLocked`, `OST_MaterialTags` and `OST_MultiCategoryTags` are present
and unchanged 2020-2027. What a tag points at splits at 2022 (`TaggedLocalElementId` before,
`GetTaggedLocalElementIds` after) and is read by name, as `TAG_ROOMS` does. **Nothing here has run in
Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (KI2 and KI3 measure them):
whether Revit takes a **material tag on the element** itself or only on a **face** of it - the card tries
the element first, then the faces drawn in the view, largest first, and counts the face ones in
`findings`; **which layer's material** a material tag on a compound wall reads; whether
`IndependentTag.Create` honours a material or multi-category tag type passed to it -
`TAG_ELEMENTS_IN_VIEW` measured it giving the default type instead, so the card sets the type again and
reads it back; and whether a dependent view shares these tags with its primary.

**Routing.** Checked through `heron_retrieve.find` on two scratch stores, before and after the card, over
every declared utterance and routing-table sentence: no declared utterance moved. One routing-table
sentence moved between two cards its table does not name - *"draw a curtain wall"*, claimed by
`CREATE_WALL` and not reached since 2026-09-24, from `REPORT_CURTAIN_ELEMENTS` to
`REPORT_CURTAIN_WALL_TYPE`, both reads. Declaring it was weighed in row 5b-387 and left to the owner, so it
is not declared here. Three sentences **nobody declares** - *"tag the walls"*, *"tag these walls"* (from
`TAG_ELEMENTS_IN_VIEW`) and *"tag the floors"* (from `TAG_ROOMS`) - now reach this card, pulled by the
modeller's own *"... these walls"* and *"... this floor"*; a wall tag type named here is refused and
nothing placed. Recorded on the card for the owner; no words were declared elsewhere to win them back.

**The model - the Project2 scratch model in Revit 2020**, never saved, as built on 2026-10-18: five
Generic - 200mm walls on Level 1 with a partition, two rooms and one floor
([`revit2020-scratch-round1-2026-10-18.yaml`](../../tools/jobs/revit2020-scratch-round1-2026-10-18.yaml)
lists it). Every run rolled back except KI5's one run. The job file is
[`tools/jobs/tag-material-or-multi-category-ki-2026-11-01.yaml`](../../tools/jobs/tag-material-or-multi-category-ki-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **KI0** | *Open.* The Project2 scratch model open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon. If no Multi-Category Tags family is loaded, load one (Insert > Load Family, Annotations) | The title bar reads Project2 |
| **KI1** | *Read the types - also the "none named" negative.* `tag-material-or-multi-category` with `--write`, `view` FloorPlan: Level 1, `tagType` left out, the walls of Level 1 from `filter-elements-by-category` | `tagged` 0, *No tag type was named, and one is never chosen for you*, `typesLoaded` listing the material and the multi-category tag types. Write the material one into the job file, both legs |
| **KI2** | *Material tags: the D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: the rooms of Level 1 - `tagged` 0, every room in `notTaggable`, *a room, space or area, which takes its own kind of tag*. Positive: one material tag per wall, *<n> material tag(s) placed in "Level 1"*. **Record** the *taken by Revit on a face* count and what each tag reads in *The tags read* |
| **KI3** | *Multi-category tags.* KI2 again with the multi-category tag type KI1 listed, both legs | Negative: as KI2. Positive: one multi-category tag per wall, *<n> multi-category tag(s) placed*; any wall Revit refused named in `notPlaced` with Revit's words |
| **KI4** | *The refusals.* KI2's positive with `tagType` a Wall Tags type the model has; then with `view` the wall schedule | First: refused naming the type and *(Wall Tags), not a material or multi-category tag type*. Second: refused naming the view. Nothing placed either time |
| **KI5** | *Kept, through `try-once`, on the modeller's yes.* KI2's positive on a saved copy, FloorPlan: Level 1 in front | Keep or Discard. Then the same run again tags nothing - every wall in `alreadyTaggedHere` - and Select All Instances > Visible in View on one new tag counts `tagged`: the second route |
| **KI6** | *Revit 2024 or later.* KI2 and KI3 again | The same answers - and on 2022 or later the reading of what a tag points at goes the other way |
