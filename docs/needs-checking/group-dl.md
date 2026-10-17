# Needs checking — Group DL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DL - text note wording, spot and keynote reads, datum elbows, line and fill patterns, line styles, dimensions by value (2026-10-12)

The third batch of the gap fill ([`work-notes/plans/gap-fill-2026-10-12.md`](../work-notes/plans/gap-fill-2026-10-12.md)),
the annotation jobs no fragment did.

**What was built.** One widening and seven new capabilities:

- `EDIT_TEXT_VALUES` **version 3** (FRG-PAR-009, DRAFT before and after): a text note's own wording is one
  more field, `Text`, read and written through the note, and mode `set` writes a note's whole wording. It
  already did prefix, suffix and replace on any parameter or name, so a second text fragment was not added.
- `REPORT_SPOT_DIMENSIONS` (FRG-VIEW-122): every spot elevation, coordinate and slope, its text and what it
  measures, and spots measuring nothing. `ADD_SPOT_ELEVATIONS` places them and reads none.
- `REPORT_KEYNOTES` (FRG-VIEW-123): keynote tags and every keynote value on types and materials, against the
  loaded keynote table.
- `ADD_DATUM_LEADERS` (FRG-VIEW-124): the elbow on grid and level heads in a view. `SET_DATUM_BUBBLES` shows
  and hides the bubble and adds no elbow.
- `CREATE_LINE_PATTERN`, `CREATE_LINE_STYLE`, `CREATE_FILL_PATTERN` (FRG-VIEW-125 to 127): made, or replaced
  in place on the caller's word. `REMAP_LINE_STYLES` and the transfer fragments move or copy styles; none
  made one.
- `SELECT_DIMENSIONS_BY_VALUE` (FRG-SEL-035): linear dimensions by measured value, handed on to
  `OVERRIDE_GRAPHICS_IN_VIEW` - the colour-by-value job as two fragments that each do one thing.

**A binding rule found while building, applied to every card in the gap fill.** The add-in refuses an absent
`source: request` need unless it is a `bool` marked `optional: true` (`HeronBindingNote.AbsentValue`). So a
view, link, phase or point list that may be left out is a **string** read inside the fragment, empty meaning
"every view" or "the only link", and the job files pass every value - `""` or `0` where the default is meant.

**Measured without Revit, 2026-10-12.** Every Revit API member was read off `RevitAPI.xml` for all eight
releases; all eight compile on 2020, 2024 and 2027 and pass the author gate; the job file reads `8 WOULD RUN`
under `batch-prove --dry-run`. **Nothing below has run in Revit.**

Never on a working project. With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **DL1** | Arrange on **Revit 2024**: the scratch project the header of [`gap-fill-annotation-2026-10-12.yaml`](../../tools/jobs/gap-fill-annotation-2026-10-12.yaml) describes, saved as **Heron Gap Fill Annotation Test** | the text notes, spots, keynotes, grids, dimensions and the empty section all there |
| **DL2** | `python tools/batch-prove.py tools/jobs/gap-fill-annotation-2026-10-12.yaml`, PROJECT-TITLE filled | edit-text-values `changed` 2 then 0; spots 4 then 0; keynotes `tagCount` 2 then 0 and `missingFromTable` naming 99 99 99; elbows 4 then 0; line pattern made then refused; line style made then refused; fill pattern made then refused; dimensions 3 then 0. Look at the plan afterwards - nothing left: rollback has failed on this PC before (BD1) |
| **DL3** | edit-text-values by hand, field Text, mode set, on one note | the note reads exactly the text given, with no stray line break at its end |
| **DL4** | report-spot-dimensions after deleting the floor under one spot | that spot in `onNothing` |
| **DL5** | add-datum-leaders by hand, kept, then the same again | `added` 4, then `alreadyHad` 4 - and each elbow draggable in the plan |
| **DL6** | create-line-style updateExisting on Heron Setout with colour 255,0,0 | readBack shows red; a line already drawn in it turns red |
| **DL7** | select-dimensions-by-value then `OVERRIDE_GRAPHICS_IN_VIEW` red, expect_from the select | the three short dimensions red in the plan, the rest unchanged |
| **DL8** | DL2 on **Revit 2020** and **2027** | the same answers. Compiled on three releases here; what Revit DOES was measured on none |
| **DL9** | The signatures, on the owner's word only, after DL2 to DL7 | `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` for each of the eight |
| **DL10** | `REPORT_SPOT_DIMENSIONS` **version 2** (2026-10-15, [row 5b-483](../fragment-issues/section-5b-rows-401-425.md)): it lists every dimension, not only spots, in a new `dimensions` output. DL2 runs it with `expect: [spots, dimensions]` | `dimensionCount` 5 then 0: each plan line names the view, `Dimensions (Linear)`, the type, the value as the plan shows it (the string as `300 + 2400`) and what it measures between - a grid by its name, a wall by category and id; `spots` exactly as version 1 gave it |
| **DL11** | version 2 by hand on the same plan: type `EQ` over one dimension's value; then align a wall to a grid with the Align tool and close its padlock; run it again | the typed-over line reads `"EQ" TYPED OVER, measures` and the value it measures; the alignment adds no line - read `findings` for `1 lock(s) or alignment(s) left out` and record whether a view's collector returned the constraint at all, which nothing has measured |
| **DL12** | version 2, READ ONLY, on a real job's section with dimensions to grids, levels, a family and a linked wall - the owner's `4355-BHVD-3D-21P60-ST001I`, `Section-04`, which carries four (6649376 to 6649379); never written | each reference named in modeller's words: `grid 2`, `level +110.700`, the opening by family, type, Mark and id, `a face in link ...` for the linked wall |
| **DL13** | `EDIT_TEXT_VALUES` **version 4** and `READ_ELEMENT_PARAMETERS` **version 3** (2026-10-17, [row 5b-520](../fragment-issues/section-5b-rows-401-425.md)): a note is edited through its formatted text, and `Text` on a note reads its wording. **RUN on Revit 2020, rolled back**, on the scratch copy "Heron note scratch" (Autodesk's `rst_basic_sample_project.rvt`, copied to `%TEMP%\heron-work\tn-295356`): replace, set, prefix and suffix each with a negative, the read with a negative, and the read on 109 notes through SELECT_VISIBLE_IN_VIEW. Owed: the signatures | `python brain/heron_validate.py accept edit-text-values --by "Ajmal PS"` and the same for `read-element-parameters`, from the worktree that holds the drafts (copies in `%TEMP%\heron-work\tn-295356\keep\drafts`). Read each draft first: its arrangement names `scratch-numbered-notes`, a setup step kept in that folder and never in the repository |
| **DL14** | DL3 on a numbered note, by hand: a saved scratch project, a drafting view, a note made a 1-2-3 list with Revit's own list button, one paragraph with Shift+Enter; then `EDIT_TEXT_VALUES` field Text, mode replace, through `try-once` (D-119) and Keep | the numbers 1, 2, 3 still beside every paragraph on screen, the soft line break still a line break - the second route nothing rolled back can show. Then DL2 and DL8 cover version 4 on 2024 and 2027 as they stand |
