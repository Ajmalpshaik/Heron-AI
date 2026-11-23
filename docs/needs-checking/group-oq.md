# Needs checking — Group OQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OQ - seven tools changed on the owner's answers of 2026-11-19/20: tag gaps on paper, the dashboard's subcategories, a link's settings, reloading with no undo, spaces with no area, Object Styles' material, revision numbers that do not move - DRAFT until proved (2026-11-20)

**Tracked as GitHub issue [#1077](https://github.com/Ajmalpshaik/Heron-AI/issues/1077) since 2026-11-20** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-19 and 2026-11-20 the owner, Ajmal PS, answered decision rows on
[#924](https://github.com/Ajmalpshaik/Heron-AI/issues/924), [#921](https://github.com/Ajmalpshaik/Heron-AI/issues/921) and
[#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926), with his standing word that a real defect in a
PROVEN tool is fixed even though the tool goes back to DRAFT. Each tool below was changed in its C#, its
version raised, and set to DRAFT; each PROVEN card keeps its old proof block as the record of what it proved.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`arrange-tags`](../../brain/fragments/arrange-tags/fragment.yaml) (`ARRANGE_TAGS`) | PROVEN v2 | DRAFT v3 | [5b-240](../fragment-issues/section-5b-rows-226-250.md) | OQ1, OQ2 |
| [`report-model-dashboard`](../../brain/fragments/report-model-dashboard/fragment.yaml) (`REPORT_MODEL_DASHBOARD`) | PROVEN v3 | DRAFT v4 | [5b-1355](../fragment-issues/section-5b-rows-1351-1375.md) | OQ3, OQ4 |
| [`list-linked-models`](../../brain/fragments/list-linked-models/fragment.yaml) (`LIST_LINKED_MODELS`) | PROVEN v1 | DRAFT v2 | [5b-474](../fragment-issues/section-5b-rows-451-475.md) | OQ5, OQ6 |
| [`reload-links`](../../brain/fragments/reload-links/fragment.yaml) (`RELOAD_LINKS`) | DRAFT v2 | DRAFT v3 | [73](../fragment-issues/section-5-rows-051-075.md) | OQ7, OQ8 |
| [`sync-spaces-with-linked-rooms`](../../brain/fragments/sync-spaces-with-linked-rooms/fragment.yaml) (`SYNC_SPACES_WITH_LINKED_ROOMS`) | PROVEN v1 | DRAFT v2 | [5b-1125](../fragment-issues/section-5b-rows-1101-1125.md) | OQ9, OQ10 |
| [`list-categories`](../../brain/fragments/list-categories/fragment.yaml) (`LIST_CATEGORIES`) | PROVEN v3 | DRAFT v4 | [5b-1427](../fragment-issues/section-5b-rows-1426-1450.md) | OQ11, OQ12 |
| [`edit-revision-numbering-sequence`](../../brain/fragments/edit-revision-numbering-sequence/fragment.yaml) (`EDIT_REVISION_NUMBERING_SEQUENCE`) | PROVEN v2 | DRAFT v3 | [5b-414](../fragment-issues/section-5b-rows-401-425.md) | OQ13, OQ14 |

**What changed, in one line each:**

- **`ARRANGE_TAGS` v3** - `minGapMm`, `fallbackTagWidthMm` and `fallbackTagHeightMm` are PAPER millimetres,
  multiplied by the view's scale, as `STACK_TAGS` takes its gap. A size measured off a leaderless tag is not
  multiplied. [`annotate-a-view`](../../brain/skills/annotate-a-view.yaml) now sends the paper figure as given;
  it told the chat to multiply it first, which would now scale it twice. Group NR's NR1 and NR2 (d) were
  corrected to match.
- **`REPORT_MODEL_DASHBOARD` v4** - a `categories` row whose category has a parent (a subcategory) is named
  with the parent beside it - *"Center line (Ducts)"* - and carries `parent` and its first 5 element ids in
  `sampleIds`; the findings line counts categories by the one each row belongs to, and names the
  subcategories apart. `brain/heron_model_dashboard.py` counts the same way. `elementTotal` and `instances`
  keep their rule.
- **`LIST_LINKED_MODELS` v2** - each link FILE: path type (Relative, Absolute, Server), saved path, Room
  Bounding, Overlay or Attachment, nested or not, unloaded for this user only or not; each PLACEMENT:
  pinned, workset, and its name as Revit shows it (which ends with its shared position). A new `summary`
  string carries every line whole. Every member it reads is on 2020 to 2027 (`tools/api-surface`, 2026-11-20).
  **While it is DRAFT, Heron's starter checklist** (`brain/heron_checklists.py` `STARTER`, whose checks are
  PROVEN reads) **checks** *every linked file is loaded* **with `REPORT_EXTERNAL_REFERENCES`' `unloaded`**
  instead of its `notLoaded`; put it back once OQ5 and OQ6 pass.
- **`RELOAD_LINKS` v3** - when anything was done, the FIRST finding reads *"No undo: Revit's Undo and Discard
  cannot take this back."*; the card says to ask first, every time, on a real model, and that it is proved
  on a scratch copy of a sample model only.
- **`SYNC_SPACES_WITH_LINKED_ROOMS` v2** - a space made that reads back with no area leaves `created` and is
  counted in a new `noArea`, its line saying whether the link's type is Room Bounding. Version 1 counted
  it as made. A space is still made only where the linked model has a placed room.
- **`LIST_CATEGORIES` v4** - every Object Styles row ends with two more columns: the Material (`-` on an
  annotation row, `none` where none is set) and the category's kind (Model, Annotation, Analytical). The
  first five columns are unchanged.
- **`EDIT_REVISION_NUMBERING_SEQUENCE` v3** - a settings change is refused only when an ISSUED revision's
  printed number would really change: each one's number as Revit prints it now is placed in the numbering
  and printed again under the settings asked for. The same is allowed and said; a different one refused
  with both numbers; one that cannot be worked out (numbering per SHEET above all) refused as before.

**Read off the code and the shipped assemblies, NOT run in Revit:** each compiles for every release it
claims (`tools/check-fragments-compile.py` 2020, 2022, 2024, 2025 and 2027, 2026-11-20, a cloud container
with the .NET 10 SDK) and every Revit member it names is on 2020 to 2027 (`tools/check-api-surface.py`, the
same day). **Nothing here has run in Revit.** Owed a real Revit: what `Category.Parent` names for the
Snowdon *Center line* elements (row 5b-1355 never read one); whether `RevitLinkType.PathType`,
`AttachmentType` and the link type's `WALL_ATTR_ROOM_BOUNDING` read what Manage Links and Type Properties
show; whether Revit permits a RELOAD inside the transaction `revit_change` opens (row 73 measured only the
unload); whether linked walls bound a host space inside a group that is KEPT (row 5b-1125); that
`Category.Material` is the Material Object Styles shows; and that `Revision.RevisionNumber` is what a
sheet's revision schedule prints under per-project numbering.

**The models, every row: scratch copies, never a working model.** Copy
`Snowdon Towers Sample HVAC.rvt` (Revit 2024 or later, its six links beside it) to
`%TEMP%\heron-work\oq\oq-snowdon.rvt` for OQ3, OQ5 and OQ7; a new project from the default template,
saved as `%TEMP%\heron-work\oq\oq-blank.rvt`, for OQ4, OQ6, OQ11, OQ13 and OQ14; a scratch Generic
Annotation family for OQ12; and for OQ9 and OQ10 a scratch MEP project with **only** an architect's
sample model linked into it (origin to origin), saved as `oq-mep.rvt` beside its link `oq-arch.rvt`. Open
the copy in a Revit with no working model in front, or save and close the working model first. **OQ7 and
OQ8 change a link outside Revit's undo: run them on `oq-snowdon.rvt` only, and only after the owner has
said yes for that run** - row 73, his answer of 2026-11-20. Read the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **OQ1** | *`ARRANGE_TAGS` v3 positive, rolled back.* On `oq-blank.rvt`, a floor plan at **1:100**: two doors, each with a leaderless door tag, the two tags dragged on top of each other. `FILTER_ELEMENTS_BY_CATEGORY` *Door Tags* in that plan, then `ARRANGE_TAGS` with `expect_from` naming it, `minGapMm=2`, `maxPasses=10`, `fallbackTagWidthMm=12`, `fallbackTagHeightMm=4`. Press **Keep**; measure the gap on the plan; Undo | `moved` 1 or 2, `stillOverlapping` empty, `measuredFrom` 2, and in the run record `gap` **0.656 ft** (2 mm x 100 = 200 mm). The two tag boxes about 2 mm apart at 1:100. Version 2 gave `gap` 0.00656 ft - boxes touching |
| **OQ2** | *`ARRANGE_TAGS` v3 negative - nothing clashes.* The same plan with the two door tags apart, each in clear space; the same call | `moved 0`, `stillOverlapping` empty, `notTags` empty - nothing was moved. Then once with `minGapMm=200` on the OQ1 pair: `gap` **65.6 ft** in the record - a figure scaled by the caller is scaled again; write `moved` down and Undo |
| **OQ3** | *`REPORT_MODEL_DASHBOARD` v4 positive.* `oq-snowdon.rvt`, its links loaded. `revit_read REPORT_MODEL_DASHBOARD` | Every row of `categories` that JT1 read as *"Center line"* is now named with its parent beside it - *"Center line (Ducts)"* or whatever the parent is - with `parent` set and up to 5 `sampleIds`; every other row `parent` null. `elementTotal` still the sum of every row's `instances` (JT1's 4750 on that copy, give or take what the copy holds). The findings line counts those rows as subcategories, not categories. **Then select one `sampleIds` id in Revit (Select by ID) and read its category in Properties - write it down: row 5b-1355 never knew what these elements are** |
| **OQ4** | *`REPORT_MODEL_DASHBOARD` v4 - a by-eye check of the sub-category naming, not an empty D-30 leg: on `oq-blank` its one wall is counted in `elementTotal`, the card's one result (row 5b-1969).* `oq-blank.rvt` holding one wall and no model line (a model line's line style is a subcategory of Lines) | Every row `parent` null; no `sampleIds` key anywhere; the findings line's category count equal to the rows, and no *"subcategor"* in it |
| **OQ5** | *`LIST_LINKED_MODELS` v2 positive.* `oq-snowdon.rvt`. First read, by hand: Manage Links (Reference, Path Type, Saved Path) for every link; one link type's Room Bounding in Type Properties; one placement's Pinned and Workset in Properties. Then `revit_read LIST_LINKED_MODELS` | `summary` names all six files, each with the path type, saved path, Room Bounding, Overlay or Attachment and nested that Revit shows; each placement pinned or NOT pinned and its workset as Properties shows; no setting reads NOT READ on a loaded link. `findings` one line a file. `elements` six placements, `notLoaded` empty |
| **OQ6** | *`LIST_LINKED_MODELS` v2 negative.* `oq-blank.rvt`, no link | `summary` EMPTY, `elements` empty, `notLoaded` empty. **And on `oq-snowdon.rvt` after OQ5**, with one link type's Room Bounding turned off by hand: that file reads *"Room Bounding: no"*, the others as OQ5 read them, and Manage Links exactly as before - the read changed nothing |
| **OQ7** | *`RELOAD_LINKS` v3 positive - SCRATCH COPY ONLY, the owner's yes first.* `oq-snowdon.rvt`: one link placement selected, `revit_change RELOAD_LINKS unload=false` | Whatever Revit does inside the transaction `revit_change` opens, written down - **the first thing to find out** (row 73 measured only the unload: refused inside, done outside). If it reloads: the FIRST finding reads *"No undo: Revit's Undo and Discard cannot take this back."*, then Revit's raw result code; Manage Links reads Loaded; and Discard (or Undo) leaves the reload in place - write down which |
| **OQ8** | *`RELOAD_LINKS` v3 negative - SCRATCH COPY ONLY.* On `oq-snowdon.rvt` unload one link by hand in Manage Links, then `revit_change RELOAD_LINKS unload=true` with that link selected | `done 0`, its type id in `alreadyUnloaded`, *"was ALREADY unloaded - nothing was done to it"*, and **no** *"No undo"* line - nothing was changed. Reload it by hand afterwards |
| **OQ9** | *`SYNC_SPACES_WITH_LINKED_ROOMS` v2 positive, KEPT.* `oq-mep.rvt`: `oq-arch.rvt`'s link type Room Bounding ON (Type Properties, by hand), no spaces yet. `revit_change SYNC_SPACES_WITH_LINKED_ROOMS linkName=oq-arch phaseName= updateNames=false`; press **Keep**; read Revit's Space schedule | `created` one per placed room in the link, `noArea` EMPTY, no line saying NO AREA; every space in the schedule with an area and its room's number and name. **If a space reads NO AREA here - link bounding and the change kept - that is the finding row 5b-1125 waited for:** write down its id and its line. Undo |
| **OQ10** | *`SYNC_SPACES_WITH_LINKED_ROOMS` v2 negative.* The same, with the link type's Room Bounding **OFF** and no walls in `oq-mep.rvt` | `created` EMPTY; `noArea` one space per placed room, each line *"NO AREA ... NOT counted as made; its link is NOT Room Bounding in this model"*; the summary line naming them apart. Version 1 gave `created 2` on Project1 for two such spaces and judged PASS. Discard |
| **OQ11** | *`LIST_CATEGORIES` v4 positive.* `oq-blank.rvt`: Manage > Object Styles, Generic Models given a material (write its name down). `revit_read LIST_CATEGORIES categories="Generic Models"` | The Generic Models row ends *"\| <that material> \| Model"*; each subcategory row under it ends with its own material or *none*, and *Model*; the first five columns as Object Styles shows them |
| **OQ12** | *`LIST_CATEGORIES` v4 negative.* In the scratch Generic Annotation family, `categories="Generic Annotations"`; and on `oq-blank.rvt`, `categories="Doors"` with no material set for Doors | Every Generic Annotations row's material `-` and kind *Annotation*, no material name on any; the Doors row's material *none* |
| **OQ13** | *`EDIT_REVISION_NUMBERING_SEQUENCE` v3 positive, rolled back.* `oq-blank.rvt`, revisions numbered **per project**: an alphanumeric numbering whose custom list is `AA, BB, CC, D, E`; four revisions on it, the first three **Issued**; read a sheet's revision schedule (AA, BB, CC, D). `revit_change EDIT_REVISION_NUMBERING_SEQUENCE` with that numbering's name (on 2020 or 2021 `name=Alphanumeric`) and `customSequence=AA, BB, CC, DD, EE`; press **Keep**, read the schedule, Undo | `changed` names the list change and says the issued revisions print exactly what they printed before - AA, BB, CC; the schedule reads AA, BB, CC, **DD**. Version 2 refused it. If the schedule shows AA to CC differently, `RevisionNumber` is not what the sheet prints - that is the finding |
| **OQ14** | *`EDIT_REVISION_NUMBERING_SEQUENCE` v3 negative.* The OQ13 model as it was, the fourth revision **Issued** too: `customSequence=AA, BB, CC, DD, EE`. Then, all four issued again, revisions numbered **per sheet** (Sheet Issues/Revisions), the same call | First: `refused` naming the fourth revision and *"'D' -> 'DD'"*, nothing changed. Per sheet: `refused` saying the project numbers revisions per sheet and what each sheet would print is not worked out; nothing changed |

**Signing, once OQ1 to OQ14 pass:** each card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
