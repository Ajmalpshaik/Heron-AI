# Needs checking — Group IL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-21 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IL - a label's or text note's TYPE read and changed in millimetres, copied and switched: `REPORT_TEXT_TYPES` and `SET_TEXT_TYPE` (2026-10-21)

From [row 5b-1300](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-21 the owner, building the pipe
tag family *TRG_Pipe_Tag* on Revit 2020, wanted his labels' text changed - 2.5 to 5 mm, font, bold - and the
same for text notes. Nothing in Heron read or changed a text type. Built the same day, in the cloud, with no
Revit. *(Renumbered from Group IJ with `tools/renumber.py` the same day - [#713](https://github.com/Ajmalpshaik/Heron-AI/pull/713), the tag labels work, took IJ first.)*

| What | Where | What it does |
|---|---|---|
| `REPORT_TEXT_TYPES` | [`report-text-types`](../../brain/fragments/report-text-types/fragment.yaml) | READ - every label and text type with what Type Properties shows: Text Size, Leader/Border Offset and Tab Size in paper mm, Text Font, Width Factor, Bold, Italic, Underline, Background, Color (R,G,B), Line Weight, Show Border, Leader Arrowhead - where the type has them - and how many labels and text notes use each. `textTypeName` narrows: a name, `Family: Type`, or `labels` |
| `SET_TEXT_TYPE` | [`set-text-type`](../../brain/fragments/set-text-type/fragment.yaml) | MODIFY - any of those settings on a named type (or the one type the family's labels use), read back; `duplicateAs` copies it first and changes the copy; `switchTo` moves the labels or text notes on the original onto the copy. All checked before anything is written; anything not read back as asked takes the whole call back |
| `detail-family-creation`, its label type paragraph | [`brain/skills/detail-family-creation.yaml`](../../brain/skills/detail-family-creation.yaml) | a label's text is its type: read it, then change it |

**What is done and what is not.** Both cards validate (`python brain/heron_fragment.py`) and compile for
2020 to 2027 (`tools/check-fragments-compile.py`); every Revit member they call was read with
`tools/api-surface --members` on all eight releases. Lengths go between millimetres and Revit's internal
feet by 304.8 with no units API, so there is no `#if`. The method validates (`python brain/heron_skill.py`).
**Nothing here has run in Revit.** Both cards are DRAFT until these rows pass and the owner signs.

**Three things read off the assemblies that only Revit can confirm, and these rows settle.**

- **What a label is.** `TextElement`'s only subclass is `TextNote` 2020 to 2026 (and `AnnotationLabel` too
  from 2027), so a label in a 2020 family is read as a `TextElement` that is not a `TextNote`, and its type
  as a `TextElementType` through `GetTypeId()`. **IL2** settles that the labels' type comes back as a text
  type at all - if it does not, `SET_TEXT_TYPE` refuses in plain words and these rows stop there.
- **Whether `ChangeTypeId` moves a LABEL.** On every release by name; never run on a label. **IL6.**
- **Two numbers Revit stores without a word for them.** Background is written 0 for Opaque and 1 for
  Transparent, and Color as R + G×256 + B×65536. The read-back checks Background against Revit's own word
  for it; Color has no such word. **IL6** checks both by eye in Type Properties.

**The arrangement.** On **Revit 2020**, all of it in `%TEMP%\heron-work\text-il`, never a working family or
model:

- `Heron Text Type Test.rfa` - a COPY of `TRG_Pipe_Tag` saved under that name, its labels all on ONE label
  type, and one text note placed in its view on a text type. Write down the label type's name and every
  value in its Type Properties.
- `Heron Model Text Test.rfa` - a new Generic Model family with one Model Text placed, so it holds a model
  text type. Note that type's name.
- `Heron Text Type Project.rvt` - a new project from the metric template, two text types (`2.5mm Arial` and
  one other), three text notes on `2.5mm Arial` and one on the other.

[`tools/jobs/text-types-il-2026-10-21.yaml`](../../tools/jobs/text-types-il-2026-10-21.yaml) runs IL1 to
IL4 as a batch, with the tag family in front; the rows below are run by name. Every change but IL9 is
rolled back (`validate ... --write`).

| # | Run | Look for |
|---|---|---|
| **IL0** | *Set up.* The three files above. `Heron Text Type Test` open in the Family Editor, in front | Type Properties of the label type noted: Text Size, Text Font, Bold, Background, Color |
| **IL1** | *The read's negative FIRST.* `REPORT_TEXT_TYPES` textTypeName `No Such Text Type` | typeCount **0**, textTypes empty, a finding naming the label and text types there are - never another type's settings |
| **IL2** | *The read.* textTypeName `labels`; then left out (`all`) | **First:** typeCount **1**, the label type IL0 noted, each value as Type Properties shows it - Text Size in mm, Background in Revit's word, Color as R,G,B - **no** Leader Arrowhead on its line, `used by` every label and 0 text notes. **Second:** the label type and the text type, the text type with its Leader Arrowhead; labelCount the labels, textNoteCount 1. **Write down** the family name each line starts with (Label, Text or what Revit says) |
| **IL3** | *The change's negative FIRST.* `SET_TEXT_TYPE` textTypeName `No Such Text Type`, changes `Text Size=5` | refused *No label or text type is named ... NOTHING WAS CHANGED*, changedTypeId invalid, switched 0. Rolled back, and nothing to roll back |
| **IL4** | *The modeller's change, rolled back.* textTypeName left out (`labels`), changes `Text Size=5` | changedTypeId the label type; typeBefore its IL0 size; typeAfter **Text Size 5 mm**; usedBy every label; a finding that tags in a project change only when the family is loaded back. Rolled back: Type Properties shows the IL0 size after |
| **IL5** | *Model text, both cards.* In `Heron Model Text Test`: `REPORT_TEXT_TYPES` textTypeName = the model text type's name; then `SET_TEXT_TYPE` the same name, changes `Text Size=5` | **Read:** typeCount 0, notTextTypes *is a MODEL TEXT type ... ADD_FAMILY_MODEL_TEXT*. **Change:** refused saying the same, NOTHING WAS CHANGED; the model text's size as before |
| **IL6** | *The copy and the switch, rolled back.* In the tag family: textTypeName left out, changes `Text Size=5; Bold=yes; Text Font=Arial Narrow; Background=transparent; Color=255,0,0`, duplicateAs `5mm Arial Narrow Bold`, switchTo `labels` | typeAfter `... 5mm Arial Narrow Bold` with each value as asked; switched = the labels; usedBy those labels and 0 text notes; typeBefore the original unchanged; typeAfter's Background in Revit's own word, **Transparent**. If a label did not move, the run must have THROWN (*did not move onto ...*) and kept nothing - write down what Revit said. **The colour, by a second route:** by hand, Duplicate the label type in Type Properties, set its Color to pure red and its Background to Transparent, then `REPORT_TEXT_TYPES` with that name - Color **255,0,0**, Background **Transparent**. Undo the duplicate after |
| **IL7** | *A setting a label type has not got.* textTypeName left out, changes `Leader Arrowhead=Arrow 30 Degree` | refused *cannot take: Leader Arrowhead - this type has none ... Its settings are: ...* - NOTHING WAS CHANGED |
| **IL8** | *Labels on two types.* Move one label by hand onto a second label type (a Duplicate in Type Properties), save; textTypeName left out, changes `Text Size=5` | refused naming both types and how many labels each holds; neither changed. Put the label back |
| **IL9** | *The kept run, on the modeller's yes only (D-119).* `try-once set-text-type --set "changes=Text Size=5"` with `Heron Text Type Test` saved and in front | Revit's Keep or Discard box; on Keep, one entry in the undo list named for the change, Type Properties 5 mm, the labels drawn at 5 mm. Then save |
| **IL10** | *A project, notes switched.* In `Heron Text Type Project`: textTypeName `2.5mm Arial`, changes `Text Size=3.5; Leader Arrowhead=Arrow 30 Degree`, duplicateAs `3.5mm Arial`, switchTo `text notes` (rolled back); then switchTo `labels` alone | **First:** `3.5mm Arial` at 3.5 mm with that arrowhead; switched **3**; `2.5mm Arial` still 2.5 mm; usedBy 3 text notes. **Second:** refused - a project has no labels to move |
| **IL11** | *Back in a project.* The IL9 family loaded into `Heron Text Type Project` with a pipe and a pipe tag of it (LOAD_FAMILY) | the tag's text drawn at 5 mm - Revit's own behaviour, recorded so the finding's sentence is known true |
| **IL12** | *The method.* In a chat with `Heron Text Type Test` open: *"change the label text height from 2.5 to 5 in this tag"* | `heron_method detail-family-creation` (its label type paragraph), or the lookup, reaches `SET_TEXT_TYPE`; the chat reads with `REPORT_TEXT_TYPES` first and says which type it will change and what else uses it |
| **IL13** | *A later release.* IL2, IL4 and IL6 again on **Revit 2027** (or 2024 when 2027 is not installed), the family upgraded on a copy | On 2027 the labels are `AnnotationLabel`s - the same counts and the same values; the switch moves them |
| **IL14** | *Routing on the owner's PC* - `model+rerank`, the backend there. The 29 sentences below, main against the branch, with `tools/score-routing.py --sentences` | The four phrasings reach `SET_TEXT_TYPE`; no question reaches a write; *make the first word of this note bold* stays `EDIT_TEXT_VALUES`. Write down where *make the label text bold* lands |

**The sentences of row 5b-1300**, frozen in two sets. The first was written before either card existed; the
second after the first build, from what moved:

```yaml
groups:
  - group: the task's four phrasings
    expect: want SET_TEXT_TYPE
    sentences: [change the label text height to 5 mm, make the tag text bold,
                change the text font to Arial Narrow, make a 2.5 mm text type]
  - group: variants of a text type change
    expect: want SET_TEXT_TYPE
    sentences: [change the label text size from 2.5 to 5 mm, make the text note type italic,
                make the label background transparent, set the width factor of the label text to 0.8,
                duplicate the text type as 3.5mm Arial and use it for the labels]
  - group: questions about a text type
    expect: want REPORT_TEXT_TYPES
    sentences: [what text size is the label in this tag, which font does the text type use,
                list the text types in this project, what text types are there and which notes use them]
  - group: neighbours that must stay where they are
    sentences:
      - {say: add a text note here saying provide access, expect: want CREATE_TEXT_NOTE}
      - {say: change the wording of this text note, expect: want EDIT_TEXT_VALUES}
      - {say: add model text to the family, expect: want ADD_FAMILY_MODEL_TEXT}
      - {say: duplicate this type, expect: want DUPLICATE_TYPE}
      - {say: change these to the 300 type, expect: want CHANGE_ELEMENT_TYPE}
      - {say: label this dimension with the Width parameter, expect: want LABEL_FAMILY_DIMENSION}
      - {say: tag these pipes, expect: want TAG_ELEMENTS}
      - {say: make the dimension text 3 mm, expect: neutral}
  # The guard set, frozen after the first build.
  - group: a note's own formatting and questions about it
    sentences:
      - {say: is the first line of the note bold, expect: not-a-write}
      - {say: is this text note bold, expect: not-a-write}
      - {say: make the first word of this note bold, expect: want EDIT_TEXT_VALUES}
      - {say: make the second line of this note bold, expect: want EDIT_TEXT_VALUES}
      - {say: show the object style of Generic Annotations, expect: want LIST_CATEGORIES}
  - group: more text type changes
    expect: want SET_TEXT_TYPE
    sentences: [make the label text bold, change the tag label font, set the text type size to 3.5 mm]
```
