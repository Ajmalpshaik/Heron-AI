# Needs checking — Group EF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-14 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EF - a family instance dimensioned by its own references and faces: `CREATE_LINEAR_DIMENSION` version 3 (2026-10-15)

Asked for on 2026-10-15 on a real project (Revit 2020): a builders-work opening - a Generic Model family -
dimensioned in a section from its level and its grid, and by its own width and height. The family published
only CenterLeftRight, CenterFrontBack and nine weak references, and
[`CREATE_LINEAR_DIMENSION`](../../brain/fragments/create-linear-dimension/fragment.yaml) (FRG-VIEW-101)
version 2 reached grids, levels, named planes, lines and wall faces - none of them. No tool in the library
could draw it. [Row 5b-482](../fragment-issues/section-5b-rows-401-425.md) has the finding.

**What changed.** Version 3 takes two words more in `between`, and every version 2 word reads as it did:
`family ID WHICH` - the instance's own reference, Left to CenterElevation or a reference plane's name, one it
does not publish refused with the list it does - and `face ID SIDE`, the element's flat solid face toward
left, right, top or bottom of the view, read from the geometry the view shows with references computed. The
same point given twice for `lineFrom` and `lineTo` runs the string through that point, square to what it
measures; it was refused before, so no caller changes. The card is **DRAFT**: version 2's proof is stale
and `check-signatures` lists it under STALE.

**Compiled for every release 2020 to 2027, not yet run in Revit.** The family-reference calls were read off
the reference assemblies for all eight releases on 2026-10-15. Whether Revit dimensions to a face read from
a family's symbol geometry in a section is what EF3 measures, and nothing here has seen it do so.

**THE TEST PROJECT IS A NEW ONE, NEVER THE REAL PROJECT.** The header of
[`tools/jobs/create-linear-dimension-family-face-2026-10-15.yaml`](../../tools/jobs/create-linear-dimension-family-face-2026-10-15.yaml)
says how to make it: a box family from the Generic Model template with no reference plane of its own -
so it publishes only CenterLeftRight and CenterFrontBack, like the opening - one placed 1200 mm above Level
1, grid A 2000 mm to its left, and a section looking at it. **Ask the owner before opening it** in a Revit
that has his real project open, and have him Synchronize with Central first: on 2026-10-15 his Revit 2020
ran out of memory and lost unsynchronised work.

| # | Run | Look for |
|---|---|---|
| **EF1** | The job file's first job: `grid A \| family BOXID CenterLeftRight`, then `grid A \| family BOXID Left` as the negative | measured `2500 mm`; the negative refused, naming what the box publishes - CenterLeftRight and CenterFrontBack among it - and offering `face BOXID left\|right\|top\|bottom`; nothing created |
| **EF2** | The second job: `level Level 1 \| face BOXID bottom \| face BOXID top` with the same point, `700,-1000,1500`, for both ends; `face BOXID near \| face BOXID far` as the negative | measured `1200 mm + 600 mm`, and the finding "The string runs through 700,-1000,1500 mm"; the negative refused, saying none of the references says which way the string runs; nothing created |
| **EF3** | The third job, which writes the proof draft: `grid A \| face BOXID left \| face BOXID right`; `grid A \| face BOXID up` as the negative | measured `2000 mm + 1000 mm`; the negative refused, naming the six sides; nothing created |
| **EF4** | The second route: in "Heron Dim Section", Revit's own Annotate > Aligned dimension picked on the same faces and the same grid, by hand | the same three values as EF1 to EF3 |
| **EF5** | Version 2's own case still holds - `grid A \| grid B \| grid C` in a floor plan with two different points, as its proof ran it | the same chained string it read before |
