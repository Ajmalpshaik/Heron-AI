# Needs checking — Group MC

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MC - Annotate > Beam Annotations - on each beam in a plan, in one go, a beam tag at its start, middle or end and spot elevations on its top or bottom at its ends, beams already tagged left alone: `ADD_BEAM_ANNOTATIONS` (2026-11-02)

Asked for in the modeller's words: *"annotate all the beams in this plan with tags and top-of-steel spot
elevations"*, *"add beam annotations to these beams - tag at the middle, elevation at both ends"*, *"put
beam tags at mid-span on every beam in this view"*. Nothing in Heron placed annotations at a position
along a beam: `brain/heron_retrieve.py` on a scratch store, Revit 2024, sent the three to
`REPORT_SPOT_DIMENSIONS` (a read), `COPE_FRAMING` and `CREATE_BEAM_SYSTEM`. `TAG_ELEMENTS_IN_VIEW` tags
at an element's own point and `ADD_SPOT_ELEVATIONS` puts one spot at a face's middle; both are PROVEN,
so neither was widened, and neither knows a start, middle or end.

| What | Where | What it does |
|---|---|---|
| `ADD_BEAM_ANNOTATIONS` (MODIFY) | [`brain/fragments/add-beam-annotations`](../../brain/fragments/add-beam-annotations/fragment.yaml) | On each **beam** handed in, in **one plan** (floor, structural or reflected ceiling), a beam tag at its `tagAt` - start, middle (left out) or end, or none - and spot elevations at its `spotsAt` - both ends (left out), start, end, or none - on its `face`, top (top of steel, left out) or bottom. A start or an end is 5% of the beam's length in from it, never more than 300 mm. The tag type and spot elevation type are those **named**, or the **project's defaults**, and the answer says which; one named and not loaded, or no default: **nothing is placed** and the loaded types are listed. Any other view - a section, an elevation, a 3D view, a template - is refused by name. Named and left: a beam already tagged in the plan (no tag, no spot), not a beam (a column, a brace), not shown in the plan, of another model, no top (or bottom) face over the beam's line at that end, a spot already within 1 mm, refused by Revit. Every tag read back for its type and view, every spot for its type, view and the level it shows, and a wrong one removed inside the same undo |

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member it uses was looked
up with `tools/api-surface --members` and is 2020 to 2027 with one signature -
`IndependentTag.Create(Document, ElementId symId, ElementId, Reference, bool, TagOrientation, XYZ)` and
`NewSpotElevation(View, Reference, XYZ, XYZ, XYZ, XYZ, bool)` among them - but for what an existing tag
points at, `TaggedLocalElementId` on 2020 and 2021 and `GetTaggedLocalElementIds` after, split with `#if`
as `TAG_ELEMENTS_IN_VIEW` splits it. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (MC2 to MC4 measure them):
whether Revit accepts a beam's BOTTOM face as a spot elevation's reference in a floor or structural plan,
where it faces away from the viewer - the card asks for it and reports Revit's refusal in its own words;
whether a steel beam's top flange face comes back from the instance's own geometry or only from inside
its family (the card reads both); whether `Dimension.ValueString` gives back the level a spot elevation
shows; whether `Dimension.Origin` of a spot is the point it marks (the no-stacking check relies on it); and
whether a horizontal tag at the middle of a beam drawn at an angle reads acceptably - Revit's own Beam
Annotations can align a tag with the beam, and this card does not.

**Routing.** Measured with `tools/check-routing.py` and `heron_brain.lookup` on two scratch stores, before
and after the card: every sentence it declares reaches it by identity on Revit 2024, 2027 and none;
*"add spot elevations"*, *"add tags to these"* and *"annotate these on the drawing"* still reach
`ADD_SPOT_ELEVATIONS` and `TAG_ELEMENTS_IN_VIEW` by identity; no question is answered by a write.
*"put it back"* moved from one write it does not name to another -
[row 5b-1428](../fragment-issues/section-5b-rows-401-425.md), OPEN. Routing rows naming this card were
added to `ADD_SPOT_ELEVATIONS` and `TAG_ELEMENTS_IN_VIEW` - card text only, outside `impl/`, their proof
fingerprints unchanged.

**The model - a scratch project from the metric structural template, Revit 2024.** Never a working
model. Every run below is rolled back except MC1's arrangement and MC5's one run. The job file is
[`tools/jobs/add-beam-annotations-mc-2026-11-02.yaml`](../../tools/jobs/add-beam-annotations-mc-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MC0** | *Open.* Revit 2024, a new project from the metric structural template, Changes on at the ribbon | The title bar names the scratch project |
| **MC1** | *Arrange, and KEEP it.* Steel columns C1 and C2 6 m apart, Level 1 to Level 2; beam B1 (a W-shape) between their tops, B2 parallel 3 m away; structural plan "Level 2" shows all four, with no tag or spot. Save | "EngineeringPlan: Level 2" shows two beams and two columns, unannotated |
| **MC2** | *The types.* `ADD_BEAM_ANNOTATIONS` on B1 with tagType `ZZZ No Such Tag`, rolled back | refused *No beam tag type is called 'ZZZ No Such Tag'*, nothing placed; `typesLoaded` lists the template's structural framing tag and spot elevation types - **record** them |
| **MC3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: tagged 0, nothing placed, C1 and C2 named *not structural framing*. Positive: tagged 2 at the middle, newSpots 4, the findings line naming both default types, `report` quoting each spot's level - Level 2's height for a top at Level 2 |
| **MC4** | *Positions, the bottom face and the refusals.* B1 with tagAt `start`, spotsAt `end`, face `bottom`; B1 with tagAt `none`; the beams in a section; tagAt `quarter`; tagType naming a Door Tag type; then the positive again after MC5 kept it | Tag near B1's start, one spot near its end lower than MC3's by the beam's depth - **record** whether Revit took the bottom face in a plan; spots only; each refusal by name, nothing placed; the re-run: alreadyTagged B1 and B2, nothing stacked |
| **MC5** | *Kept, through `try-once`, on the modeller's yes.* MC3's positive on the saved project, "Level 2" in front | Keep or Discard. Then each spot's Properties name its type and the level `report` quoted, and B1's Elevation at Top agrees: the second route |
