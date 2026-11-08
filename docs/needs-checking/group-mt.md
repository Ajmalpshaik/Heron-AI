# Needs checking — Group MT

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MT - tagging many views in one pass by a rule per category, each tag placed clear of the other tags: `TAG_VIEWS_BY_RULES` (2026-11-08)

The owner's ask: [docs/48](../48-studying-bimlogiq.md) section 5 item 2, *"Annotation template + Tag in one
pass, no overlaps, batch over many views"*, and his word on how - *"upgrade the tag tool or combine the tool
or if you need to make as new also you can do it"*. Made NEW, at DRAFT, because both cards it overlaps are
PROVEN and widening either would send it back to DRAFT and out of the chat's reach (D-117) until proved
again. The overlap and the merge planned once this is proved are
[FRAGMENT-ISSUES row 5b-1477](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `TAG_VIEWS_BY_RULES` (MODIFY) | [`brain/fragments/tag-views-by-rules`](../../brain/fragments/tag-views-by-rules/fragment.yaml) | In every view named (none named: the **active view**), every element of the categories its **rules** name - separated by semicolons on one line, *category \| tag family: type \| leader \| shortest run in mm* (a line break is refused: the chat's `revit_change` drops a second line) - gets one tag of the rule's type (blank: the **project's default** for that tag category, never the first loaded). Each tag is made on its element with no leader, every head in the view is measured (leaderless tags only), and each new head goes to the first spot clear of every other head by `minGapMm` paper mm: on the element, then a head's size away above, below, right, left and diagonally, then - only where the rule says `straight` - two and three sizes out on a straight leader. Nothing clear: it stays on the element and is named in `crowded`; read back after, any head touching another is named too. Elements already tagged are left alone (with a type named, only those carrying THAT type), read from every tag OWNED by the view, its primary or the primary's dependents - never from what the view shows, so a hidden tag still counts; with the crop on, a run is tagged on the stretch of it inside the crop, and one with nothing inside is named in `outsideCrop`; rooms, spaces and zones of another phase than the view's are counted and left alone; risers and runs under the rule's length are skipped and named; rooms, spaces and zones get their own kind of tag in a plan of their level and are not moved. One rule that cannot be read refuses the whole call before anything is placed. Every view in the list is tagged, several dependents of one plan included; a view later in the list finds the tags an earlier one placed. A head moved off its element with no leader is kept clear of other tags only, never of the model, so it may sit over a neighbouring element - `findings` counts them. Every view is one undo |

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-08, a Linux container). Every Revit member it uses is in the
card's compatibility note, looked up with `tools/api-surface --members` or read off `TAG_ELEMENTS_IN_VIEW`'s
note; the one split - what an existing tag points at - is written with `#if` as that card writes it.
**Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (MT2 to MT9 measure them):
whether `Element.get_BoundingBox(view)` on a tag made a moment ago, after `doc.Regenerate()` inside the
open transaction, gives the head's real size - the whole placement rests on it; whether setting
`IndependentTag.HasLeader = true` and then `TagHeadPosition` gives a straight leader from the element to the
head, with the head where it was sent (`findings` counts any that did not go there); whether a leaderless
tag's box sits where this card expects against its head point; how long two regenerations per view take
on a large plan; whether the space path, re-authored from `TAG_ELEMENTS_IN_VIEW`'s version 7, behaves as
there (MT6 - rooms and zones are NOT covered by a row: the Snowdon HVAC sample is used for its spaces);
whether a tag placed in one dependent view is OWNED by a view of that primary's family, so the next
dependent finds it (MT8); and whether the crop box's own coordinates put a duct's visible stretch where
the drawing shows it (MT9). **The phase rule is not covered**: whether the Snowdon HVAC sample holds rooms
or spaces of two phases on one level was not checked - MT10 asks, and says NOT COVERED if it does not.

**Routing.** Measured 2026-11-08 with `tools/check-routing.py` (model backend, scratch stores) on this tree
against the same tree without the card, and `tools/score-routing.py --sentences` (33 sentences: 27 frozen
before the card was written, 6 more from the review; lexical and model - the re-ranked backends NOT ASKED,
no cross-encoder on the container). **The first wording did send questions to this write**: a review found
*"which tags overlap"* and *"check the tags in these views for overlaps"* (`FIND_OVERLAPPING_TAGS`) and *"tag
all the views on this sheet"* (`REPORT_TAGS_AND_TARGETS`) reaching it on the model backend, which the 27
frozen sentences had not asked. The identity and purpose were reworded; measured again, the 21 sentences
that reached `TAG_ELEMENTS_IN_VIEW`, `ARRANGE_TAGS`, `STACK_TAGS` and `FIND_OVERLAPPING_TAGS` reach them,
and the 5 questions in the set reach no write, on both backends - which says no more than that set says.
Details in row 5b-1477. Neither PROVEN card was edited.

**The model - a SAVED scratch copy of Revit's "Snowdon Towers Sample HVAC", Revit 2024.** Never a working
model. Every run below is rolled back except MT1's arrangement and MT5's one run. The job file is
[`tools/jobs/tag-views-by-rules-mt-2026-11-08.yaml`](../../tools/jobs/tag-views-by-rules-mt-2026-11-08.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MT0** | *Open.* Revit 2024, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MT1** | *Arrange, and KEEP it.* "FloorPlan: M1" with its ducts and NO duct tags (remove any in the copy); a second small plan with ducts and air terminals, untagged - **record its name**; one plan whose ducts run close together. Save | Both plans show ducts with no tags |
| **MT2** | *The rules.* `TAG_VIEWS_BY_RULES` with views "FloorPlan: M1" and rules `Ductz`; then `Ducts \| <a pipe tag type the model has>`; then `Ducts; Doors \| ZZZ No Such Tag`; then `Ducts \| \| L`; then views a 3D view and a sheet; then, in the chat, rules typed on two lines (`Ducts \| \| straight` and `Pipes`) - each rolled back | Each refused in its own words and **nothing placed** - but the two-line chat case, where the chat drops the second line before it reaches Revit: rulesRead's first line saying **1** rule read, Ducts - the count is what shows the loss - the second naming it a Pipe Tags type and listing the loaded duct tags; the third refusing rule 2 alone and placing not even the ducts' tags |
| **MT3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: tagged 0, every duct in tooShort or verticalSkipped. Positive: tagged equal to M1's ducts less risers, the project's default Duct Tags named in tagTypesUsed, perView one line - **record** typeCorrections and crowded |
| **MT4** | *Placement and leaders.* The close-together plan with rules `Ducts \| \| straight` and minGapMm 4; then views "FloorPlan: M1, <MT1's second plan>" with rules `Ducts; Air Terminals` | First: some heads moved off their ducts, leadersDrawn above 0 on straight leaders, crowded naming the rest - **look** whether any two heads overlap that `crowded` does not name, and **look at every head `findings` says was moved with no leader: record any that sits over a NEIGHBOURING duct** and reads as its tag. Second: perView two lines, and ONE entry in Revit's undo list. Then `FIND_OVERLAPPING_TAGS` on each plan, read against crowded - the second route |
| **MT5** | *Kept, through `try-once`, on the modeller's yes.* MT3's positive on the saved copy, M1 in front | Keep or Discard. Then the same call again, rolled back: tagged 0, every duct in alreadyTagged - nothing stacked |
| **MT6** | *Spaces, and a rerun.* A Snowdon HVAC floor plan of one level with placed, enclosed spaces and no space tags - **record its name**; rules `Spaces`, rolled back; then the same on a copy where the first run was kept, rolled back | First: one space tag per space of the level, each pointing at its own space, the default Space Tags in tagTypesUsed, not moved off the space's point. Rerun: tagged 0, every space in alreadyTagged |
| **MT7** | *A hidden tag still counts (the review's fix 2).* After MT5 kept M1's duct tags: turn Duct Tags off in M1's Visibility/Graphics (or its template); rules `Ducts`, rolled back | tagged 0, every duct in alreadyTagged - **nothing new placed**. The first draft read only the tags the view showed and tagged every duct again |
| **MT8** | *Several dependent views (fix 3).* Two dependent views of one plan with crops that overlap and ducts in both, no duct tags - **record their names**; views `<dependent 1>, <dependent 2>`, rules `Ducts`, rolled back; then `<the plan>, <dependent 1>, <dependent 2>` | First: both perView lines; the second dependent tags only ducts outside the first's crop and counts the rest as already tagged. Second: the plan tags all; both dependents tag 0. In both, select any duct in the overlap: **one** duct tag. **Record** each new tag's owner view (Properties or `READ_ELEMENT_PARAMETERS`) |
| **MT9** | *A duct crossing the crop (fix 4).* A dependent view whose crop cuts a long duct so its middle is OUTSIDE - **record which**; rules `Ducts`, rolled back | That duct's tag inside the crop, on the part of the duct the view shows; a duct whose box touches the crop but whose line does not, in outsideCrop |
| **MT10** | *Another phase (fix 5).* Only if the sample holds rooms or spaces of two phases on one level - **record whether it does**; rules `Spaces` in a plan of one of them, rolled back | Only the view's phase tagged; perView counting the others "of another phase than the view's ..."; none in notTagged. **If the sample has no such level: NOT COVERED - say so on the row; nothing here proves the phase rule** |
