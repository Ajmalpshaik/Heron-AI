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
| `TAG_VIEWS_BY_RULES` (MODIFY) | [`brain/fragments/tag-views-by-rules`](../../brain/fragments/tag-views-by-rules/fragment.yaml) | In every view named (none named: the **active view**), every element of the categories its **rules** name - one per line, *category \| tag family: type \| leader \| shortest run in mm* - gets one tag of the rule's type (blank: the **project's default** for that tag category, never the first loaded). Each tag is made on its element with no leader, every head in the view is measured (leaderless tags only), and each new head goes to the first spot clear of every other head by `minGapMm` paper mm: on the element, then a head's size away above, below, right, left and diagonally, then - only where the rule says `straight` - two and three sizes out on a straight leader. Nothing clear: it stays on the element and is named in `crowded`; read back after, any head touching another is named too. Elements already tagged are left alone (with a type named, only those carrying THAT type); risers and runs under the rule's length are skipped and named; rooms, spaces and zones get their own kind of tag in a plan of their level and are not moved. One rule that cannot be read refuses the whole call before anything is placed. A dependent view sharing its tags with a view already in the list is left out by name. Every view is one undo |

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-08, a Linux container). Every Revit member it uses is in the
card's compatibility note, looked up with `tools/api-surface --members` or read off `TAG_ELEMENTS_IN_VIEW`'s
note; the one split - what an existing tag points at - is written with `#if` as that card writes it.
**Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (MT2 to MT4 measure them):
whether `Element.get_BoundingBox(view)` on a tag made a moment ago, after `doc.Regenerate()` inside the
open transaction, gives the head's real size - the whole placement rests on it; whether setting
`IndependentTag.HasLeader = true` and then `TagHeadPosition` gives a straight leader from the element to the
head, with the head where it was sent (`findings` counts any that did not go there); whether a leaderless
tag's box sits where this card expects against its head point; how long two regenerations per view take
on a large plan; and whether the room, space and zone path, re-authored from `TAG_ELEMENTS_IN_VIEW`'s
version 7, behaves as there.

**Routing.** Measured 2026-11-08 with `tools/check-routing.py` (model backend, scratch stores) on this tree
against the same tree without the card, and `tools/score-routing.py --sentences` (27 sentences frozen before
the card was written; lexical and model - the re-ranked backends NOT ASKED, no cross-encoder on the
container). The 21 sentences that reached `TAG_ELEMENTS_IN_VIEW`, `ARRANGE_TAGS`, `STACK_TAGS` and
`FIND_OVERLAPPING_TAGS` before reach them after, on both backends; no question reaches a write. Details,
and the one nearness crossing left, in row 5b-1477. Neither PROVEN card was edited.

**The model - a SAVED scratch copy of Revit's "Snowdon Towers Sample HVAC", Revit 2024.** Never a working
model. Every run below is rolled back except MT1's arrangement and MT5's one run. The job file is
[`tools/jobs/tag-views-by-rules-mt-2026-11-08.yaml`](../../tools/jobs/tag-views-by-rules-mt-2026-11-08.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MT0** | *Open.* Revit 2024, the scratch copy, Changes on at the ribbon | The title bar names the scratch copy |
| **MT1** | *Arrange, and KEEP it.* "FloorPlan: M1" with its ducts and NO duct tags (remove any in the copy); a second small plan with ducts and air terminals, untagged - **record its name**; one plan whose ducts run close together. Save | Both plans show ducts with no tags |
| **MT2** | *The rules.* `TAG_VIEWS_BY_RULES` with views "FloorPlan: M1" and rules `Ductz`; then `Ducts \| <a pipe tag type the model has>`; then `Ducts; Doors \| ZZZ No Such Tag`; then `Ducts \| \| L`; then views a 3D view and a sheet - each rolled back | Each refused in its own words and **nothing placed** - the second naming it a Pipe Tags type and listing the loaded duct tags; the third refusing rule 2 alone and placing not even the ducts' tags |
| **MT3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: tagged 0, every duct in tooShort or verticalSkipped. Positive: tagged equal to M1's ducts less risers, the project's default Duct Tags named in tagTypesUsed, perView one line - **record** typeCorrections and crowded |
| **MT4** | *Placement and leaders.* The close-together plan with rules `Ducts \| \| straight` and minGapMm 4; then views "FloorPlan: M1, <MT1's second plan>" with rules `Ducts; Air Terminals` | First: some heads moved off their ducts, leadersDrawn above 0 on straight leaders, crowded naming the rest - **look** whether any two heads overlap that `crowded` does not name. Second: perView two lines, and ONE entry in Revit's undo list. Then `FIND_OVERLAPPING_TAGS` on each plan, read against crowded - the second route |
| **MT5** | *Kept, through `try-once`, on the modeller's yes.* MT3's positive on the saved copy, M1 in front | Keep or Discard. Then the same call again, rolled back: tagged 0, every duct in alreadyTagged - nothing stacked |
