# Needs checking — Group IJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IJ - labels in tag, annotation and title block families: `REPORT_FAMILY_LABELS`, `SET_FAMILY_LABEL_LAYOUT`, `COPY_LABELS_FROM_FAMILY` and the `tag-family-creation` method (2026-10-21)

From [row 5b-1294](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21 Ajmal PS, building the pipe tag *TRG_Pipe_Tag* on Revit 2020, found Heron could not touch a
label - every tag and title block went through Edit Label by hand - and asked for labels end to end: made,
placed, edited, updated, read, with each parameter's sample value, prefix, suffix, spaces, break and unit
format. The Revit API was read that day for all eight releases ([docs/43 §8.3a](../43-building-revit-families.md)):
**no release can make a label or change what one shows**; 2027 alone can READ what one shows, and keeps its
create and set calls internal. Built the same day, in the cloud, with no Revit:

| What | Where | What it does |
|---|---|---|
| `REPORT_FAMILY_LABELS` | [`report-family-labels`](../../brain/fragments/report-family-labels/fragment.yaml) | READ - every label in the family: view, type and the types it can take, point in mm, width, alignment, angle, Keep Readable, Visible link, Revit's text, and every parameter Properties shows on it. **On 2027 only**, each label's parameters in order with sample value, prefix, suffix, spaces, break and unit format |
| `SET_FAMILY_LABEL_LAYOUT` | [`set-family-label-layout`](../../brain/fragments/set-family-label-layout/fragment.yaml) | MODIFY - one label to a point, or several moved; the box's width; horizontal and vertical alignment; Keep Readable; read back. Refuses a text note, and says what a label shows is Edit Label's |
| `COPY_LABELS_FROM_FAMILY` | [`copy-labels-from-family`](../../brain/fragments/copy-labels-from-family/fragment.yaml) | MODIFY - labels copied view to view from a seed family of the SAME category open in the same Revit, the first at a point; each copy read back against its seed - on 2027 parameter by parameter |
| `tag-family-creation` | [`brain/skills/tag-family-creation.yaml`](../../brain/skills/tag-family-creation.yaml) | the method for a tag family: template, category, labels from a seed, layout, label type, box, settings, save, load, tag |
| `detail-family-creation` step 7 | [`brain/skills/detail-family-creation.yaml`](../../brain/skills/detail-family-creation.yaml) | labels in a symbol or title block, by the same three tools |

**What is done and what is not.** The three cards validate (`python brain/heron_fragment.py`) and compile for
2020 to 2027 (`tools/check-fragments-compile.py`); the 2027-only branch was seen to compile on 2027 alone by
breaking it on purpose. Every Revit member they call was read with `tools/api-surface --members` on all eight
releases. The methods validate (`python brain/heron_skill.py`). **Nothing here has run in Revit.** The cards
are DRAFT until these rows pass and the owner signs.

**Five things the cards do not claim, and these rows settle.**

1. That Revit 2020 to 2026 hand a label back as a text element that is not a text note (IJ1).
2. What `TextElement.Text` holds for a label - its sample values, its parameter names, or nothing (IJ2).
3. That `GetParameterFormatOptions` on a 2027 label returns that label's own *Edit parameter's unit format*
   setting, rather than the project's (IJ3).
4. That `Width`, the alignments and Keep Readable are accepted on a LABEL as on a text note (IJ6).
5. **What a copied label keeps** - its parameter, sample value, prefix, suffix, spaces, break and unit format,
   and a shared parameter's GUID - which is the whole of the seed route (IJ10, IJ11).

**The arrangement.** On **Revit 2020** first, all of it in `%TEMP%\heron-work\labels-ij`, never the office's
own tags and never a working model:

- `Heron Label Test.rfa` - File > New > Annotation Symbol > *Metric Generic Tag*, Family Category and
  Parameters set to **Pipe Tags**, ONE label at the planes' crossing: System Abbreviation; Diameter, prefix
  `Ø`; Bottom Elevation, prefix `BOP `, *Edit parameter's unit format* set to millimetres, 0 decimal places,
  with the + sign; all on one line. ONE text note reading `NOT A LABEL` beside it.
- `Heron Label Seed.rfa` - the same, but THREE labels, one per line, each one parameter set up as above.
- `Heron Label Target.rfa` - a new Pipe Tag from the same template, category Pipe Tags, no labels, saved.
- `Heron Duct Seed.rfa` - a Duct Tag with one label.

Before IJ1, write down in Edit Label, for each label of the seed: its parameters in order, each one's sample
value, prefix, suffix, spaces, break and unit format. IJ10 is compared against that note, not against memory.
[`tools/jobs/family-labels-ij-2026-10-21.yaml`](../../tools/jobs/family-labels-ij-2026-10-21.yaml) runs IJ1
and IJ2, IJ6's batch half and IJ8 with IJ10's batch half; the other rows are run by name. Each MODIFY card's
kept run is the modeller's one try-once ([D-119](../DECISIONS.md)).

| # | Run | Look for |
|---|---|---|
| **IJ0** | *Set up.* The four families above, all open in one Revit 2020, `Heron Label Test` in front | Edit Label notes written down for the seed's three labels and the test label |
| **IJ1** | *`REPORT_FAMILY_LABELS`, the negative FIRST.* `labelContains` `ZZZ_Not_A_Label` | labelCount **0**, labelList empty, textElements **2** - the family was read - textNotes 1, and a finding that no label holds that text. **Then `all`: labelCount 1** - so Revit 2020 hands the label back as a text element that is not a text note. A labelCount 0 with textNotes 2 means the opposite, and the card's rule is wrong |
| **IJ2** | *The positive, Revit 2020.* `labelContains` `all` | familyCategory `Pipe Tags`; labelList the label's view, its type, its point near 0,0 mm, its width and alignment; canReadParameters false, parameterCount 0, and the finding that Revit 2020 cannot say which parameters a label shows. **Record** what `text` holds - sample values, parameter names or empty - and what `labelProperties` lists; write both into the card's purpose |
| **IJ3** | *Revit 2027.* `Heron Label Test` upgraded on a COPY, opened in 2027; `all` | canReadParameters true, parameterCount **3**, parameterList in the label's order: System Abbreviation, Diameter with prefix `Ø`, Bottom Elevation with prefix `BOP `, each with its sample value, spaces 0 and break no; Wrap between parameters only as Edit Label shows. **Record** Bottom Elevation's unit format line against Edit Label's box: *millimetres, rounding 1, + sign on* means `GetParameterFormatOptions` reads the label's override; *Use project settings* means it does not - and the card's purpose says which |
| **IJ4** | *Not a family.* A scratch project in front | notAFamily true, labelCount 0, the finding that labels live in a family; nothing changed |
| **IJ5** | *`SET_FAMILY_LABEL_LAYOUT`, the negative FIRST.* `labelIds` the TEXT NOTE's id (Manage > IDs of Selection, with the note picked), `moveByMm` `0,10` | changed 0, the report *is a text note, not a label*, NOTHING WAS CHANGED - the note and the label both where they were |
| **IJ6** | *The positive.* `labelIds` the label's id, `positionMm` `0,5`, `widthMm` `30`, `horizontalAlign` `left`, `verticalAlign` `middle`, `keepReadable` `yes` - first rolled back, then the try-once kept | changed 1, readBack *at 0,5 mm \| width 30 mm \| Left/Middle \| keep readable yes* with no NOT AS ASKED; REPORT_FAMILY_LABELS afterwards gives the same; the label's text unchanged; one entry in the undo list. A setter Revit refuses on a label is named in the report - write which one into the card |
| **IJ7** | *Two more negatives.* `widthMm` `0.01`; then no setting at all | **First:** changed 0, the width range Revit allows named, NOTHING WAS CHANGED. **Second:** changed 0 and the report that nothing to change was given, naming Edit Label for parameters, prefix, suffix and unit format |
| **IJ8** | *`COPY_LABELS_FROM_FAMILY`, the negative FIRST.* `Heron Label Target` in front, `sourceDocumentTitle` `Heron Duct Seed` | copied 0, Revit's rule quoted - *Can't paste Labels across Families of different Categories* - Duct Tags against Pipe Tags, NOTHING WAS COPIED; the target has no labels afterwards |
| **IJ9** | *Two more negatives.* `sourceDocumentTitle` `No Such Seed`; then `Heron Label Seed` with `labelContains` `ZZZ_Not_A_Label` | **First:** the open documents named, NOTHING WAS COPIED. **Second:** *no label matched*, the seed's label count named |
| **IJ10** | *THE MEASUREMENT - what a copy keeps, Revit 2020.* `sourceDocumentTitle` `Heron Label Seed`, `labelContains` `all`, `placeAtMm` `0,5`, kept by try-once | copied 3, readBack each copy against its seed id, the top left one at 0,5 mm. **Then open Edit Label on each copy and compare with IJ0's note**, field by field: parameter, order, sample value, prefix, suffix, spaces, break, unit format. **Record every field kept and every field lost** in the card's purpose and in docs/43 §8.3b. Nothing in the seed changed; one undo entry in the target |
| **IJ11** | *Revit 2027.* IJ10 again on upgraded copies, `labelContains` `Bottom Elevation` | copied 1, kept 1 - or DIFFERS naming the field Revit did not carry, parameter by parameter |
| **IJ12** | *A shared parameter, and a duplicate type.* Add an office-style shared parameter from a scratch shared parameter file to one seed label (Edit Label > Add Parameter); give the seed's label type the NAME of a different type in the target. Copy that label | **First:** REPORT_SHARED_PARAMETER_FILE `compareWithDocument` on the target finds the parameter carried by the seed's GUID. **Second:** Revit's duplicate-types box - Heron declines it, the copy stops, nothing stays, and the reply quotes the box. **Record its DialogId** - a D-145 caller answer for it needs a real run's id |
| **IJ13** | *The method's category step.* A new Generic Tag; `SET_FAMILY_CATEGORY` to Pipe Tags | The category reads Pipe Tags afterwards, or the refusal is recorded and the method's step 1 says to set it by hand |
| **IJ14** | *The method end to end.* In a chat with a new Generic Tag open: *"build a pipe tag family showing system abbreviation, diameter with prefix Ø and bottom elevation with prefix BOP in mm with a + sign, on one line"* | `heron_method` tag-family-creation runs; the chat says before starting that Revit's API cannot make a label; the label comes from `Heron Label Seed` or the modeller makes it; the tag is loaded into a scratch project and tags a pipe whose Bottom Elevation is +1200 as *BOP +1200* |
| **IJ15** | *Routing on the owner's PC* - `model+rerank`, the backend there. The modeller's sentences: *"read the labels in this tag family"*, *"which parameters does this label show and in what order"*, *"move the diameter label up 2 mm"*, *"copy the diameter label from the seed pipe tag into this tag family"*, *"build a pipe tag family"*, *"add a label to this tag"*, *"is this downloaded family any good"*, main against the branch with `tools/score-routing.py` | Each reaches the card or method named; *"is this downloaded family any good"* answered as on main; no question reaches a write that did not before |
