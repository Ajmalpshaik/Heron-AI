# Needs checking — Group MD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MD - Manage > Transfer Project Standards - fill patterns, line patterns, text, dimension, wall, floor, roof, ceiling, filled region, grid and level types from another open project, this project's kept where a name is already here: `TRANSFER_PROJECT_STANDARDS` (2026-11-02)

Asked for in the modeller's words: *"transfer the fill patterns from the template project"*, *"copy the
text types and dimension types from Office Standard.rvt"*, *"bring the line styles and object styles over
from the other open project"*, *"transfer project standards: wall types and materials from the
template"*. Nothing in Heron copied types or patterns between projects: `brain/heron_retrieve.py` on a
scratch store, Revit 2024, sent the first to `TRANSFER_VIEWS_BETWEEN_DOCUMENTS`, the second to
`REPORT_DIMENSION_TYPES` (a read), the fourth to `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS`. The third is
two cards that already exist, both PROVEN - `TRANSFER_LINE_STYLES_BETWEEN_DOCUMENTS` and
`TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS` - and is not declared on the new card. `COPY_ELEMENTS_BETWEEN_DOCUMENTS`
(DRAFT) copies model elements by category, already carries two merged jobs, and takes no kind of type, so
it was not widened. `heron_duplicates.look` found nothing already here.

| What | Where | What it does |
|---|---|---|
| `TRANSFER_PROJECT_STANDARDS` (MODIFY) | [`brain/fragments/transfer-project-standards`](../../brain/fragments/transfer-project-standards/fragment.yaml) | From another **project open in the same Revit**, named **exactly** (with or without `.rvt`), copies the **kinds** named from a fixed list - fill patterns, line patterns, text types, dimension types, wall, floor, roof, ceiling, filled region, grid and level types - into the project in front, optionally only names containing `nameContains`. **A name already here is kept** and counted skipped, never handed to the copy; a fill pattern's name counts with its target. `duplicates` "overwrite" is refused: Revit's API has no overwrite. Materials, line styles, object styles, view filters, views and project parameters are named back per kind with the card that carries each, and counted refused. "all" or "everything", a word outside the list, a family in front, a source not open, a family or link of that name, two open projects of one name: **nothing is copied**, and the open projects are listed. The source is only read. Per kind: copied, skipped, refused, each copy read back by id |

**What is done and what is not.** It compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member it uses was looked
up with `tools/api-surface --members` and is 2020 to 2027 with one signature, among them
`ElementTransformUtils.CopyElements(Document, ICollection<ElementId>, Document, Transform, CopyPasteOptions)`
and `DuplicateTypeAction` - `UseDestinationTypes` and `Abort` only. **Nothing here has run in Revit.** The
card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (MD3 and MD4 measure them):
whether a type copied in that USES a type already named here - a text type's arrowhead, a wall type's
profile - raises Revit's "Duplicate Types" dialog even when the two are identical. A fragment cannot set
the handler that would answer it ([row 5b-1429](../fragment-issues/section-5b-rows-401-425.md), OPEN), so
Heron declines it and the whole run is rolled back; the card says so and counts it refused. Also whether
`FilteredElementCollector.OfClass(typeof(DimensionType))` returns spot dimension types with the linear
ones, and whether a copied dimension or text type keeps its arrowhead when the source's is not here.

**Routing.** Measured with `tools/check-routing.py` on two scratch `HERON_KNOWLEDGE` stores, the tree
with and without the card, `model` backend, and `heron_brain.lookup`: the three sentences it declares
reach it by identity; *"bring the line styles and object styles over from the other open project"* still
reaches `TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS`, *"bring the line styles over from the template"* and
*"bring the materials over from the template"* their own cards, and the furniture and generic-model
sentences `COPY_ELEMENTS_BETWEEN_DOCUMENTS`. Claimed and not reached was 66 before and 66 after, the same
lines. On the keyword route alone *"put a ceiling at 2700"* now ranks `CREATE_PROJECT_REFERENCE_PLANES`
first; the lookup answers it by identity on `CREATE_FLOOR`, as before, and that card's routing row already
says so. No question is answered by a write.

**The models - two scratch projects, Revit 2024.** Never a working model. Every run below is rolled back
except MD1's arrangement and MD5's one run. The job file is
[`tools/jobs/transfer-project-standards-md-2026-11-02.yaml`](../../tools/jobs/transfer-project-standards-md-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MD0** | *Open.* Revit 2024, Changes on at the ribbon | Revit's title bar |
| **MD1** | *Arrange, and KEEP it.* "Heron Standards Target.rvt", a new project from the default metric template, saved. "Heron Standards Source.rvt", a copy of the same template, holding by hand a DRAFTING fill pattern "Heron Hatch 45", a text type "Heron 2.5mm Arial", a linear dimension type "Heron Dim 2.5mm" and a basic wall type "Heron Block 190". Both open, the target in front | `REPORT_OPEN_DOCUMENTS` lists both; the target has none of the four |
| **MD2** | *The refusals.* sourceDocumentTitle `No Such File`; `Heron Standards` (a part of the name); kinds `everything`; kinds `fill patterns` with duplicates `overwrite`; kinds `line styles, object styles`; kinds `curtain panels` - each rolled back | Each refused in its own words, nothing copied: the open projects listed; the fixed list; *no overwrite*, pointing at Manage > Transfer Project Standards; the two TRANSFER_ cards named |
| **MD3** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative (nameContains `Diagonal up`): copied 0, copiedIds empty, skipped 1 or more, the target's Diagonal up unchanged. Positive (nameContains `Heron`): copied 1, one id read back, `report` naming Heron Hatch 45 (drafting) |
| **MD4** | *The other kinds, and the dialog.* kinds `text types and dimension types` with sourceDocumentTitle `Heron Standards Source.rvt`; kinds `wall types, materials`, nameContains `Heron` - rolled back | Two copied, one line per kind; Heron Block 190 copied and the materials line naming TRANSFER_MATERIALS_BETWEEN_DOCUMENTS. **Record** any Revit dialog the reply lists - its id and words - and whether the run stopped on it (row 5b-1429) |
| **MD5** | *Kept, through `try-once`, on the modeller's yes.* MD3's positive on the saved target, a drafting view in front | Keep or Discard. Then Manage > Transfer Project Standards, Fill Patterns only, New Only, brings no Heron pattern - it is already here: the second route |
