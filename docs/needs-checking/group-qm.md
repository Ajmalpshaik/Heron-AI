# Needs checking — Group QM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QM - CREATE_CONCEPTUAL_FORM version 2: a loft Revit refuses says what is in the profiles - DRAFT until proved (2026-11-24)

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-2021](../fragment-issues/row-5b-2021.md), issue [#1345](https://github.com/Ajmalpshaik/Heron-AI/issues/1345).
On Revit 2024, in the Tower 171 glass-mass family, two lofts through 3 closed profiles answered only Revit's own
*"Cannot create loft form"*. Version 2 of [`create-conceptual-form`](../../brain/fragments/create-conceptual-form/fragment.yaml)
reads the profiles **after** Revit has said no and names what it finds: different numbers of sides, two straight
sides on one line (with the point), a profile open where the others close, one not flat, two in one plane - or
says none was found and to loft two profiles at a time. **Nothing is refused before Revit is asked**, so a loft
that works today is not blocked. It compiles for Revit 2020 to 2027 (`tools/check-fragments-compile.py`).
**None of it has met a Revit.** The card is `DRAFT` until QM1 and QM2 pass ([D-30](../DECISIONS.md)). Rebuild
the add-in and restart Revit first (`python tools/heron-ready.py`) - that needs Revit closed, so it is the
owner's step.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`create-conceptual-form`](../../brain/fragments/create-conceptual-form/fragment.yaml) (`CREATE_CONCEPTUAL_FORM`) | PROVEN v1 | DRAFT v2 | [5b-2021](../fragment-issues/row-5b-2021.md) | QM1, QM2, QM3 |

**The model: a scratch mass family, never a working one** - a new *Metric Mass* family saved in
`%TEMP%\heron-work\qm`. Draw the profiles with `DRAW_FAMILY_POINT_CURVES` (reference lines through points).

| ID | Do this | Pass looks like |
|---|---|---|
| **QM1** | *Positive, as version 1 was proved.* Two closed 4-line rectangles, one at Z 0 and one at Z 3000; profiles the two rectangles' ids, a bar between the two, depthMm empty, solid true. | A solid loft, its volume read back; nothing about the profiles is said. |
| **QM2** | *Negative - two profiles in one plane.* Two closed 4-line rectangles side by side at Z 0; the same call. | The call fails and rolls back: Revit's message, then *"In these profiles: profiles 1 and 2 lie in one plane"*. Nothing made. **If Revit makes a form instead**, record it: the check reads only after Revit says no. |
| **QM3** | *The case from Tower 171.* Three closed profiles one above another; the bottom one a rectangle drawn as FIVE lines (one long side split in two), the others four lines each. | **RECORD** which comes back: a loft (Revit took it - then nothing is said), or a failure naming *"different numbers of sides - 5, 4, 4"* and *"profile 1 has two straight sides on one line, meeting at (x, y, z) mm"*. Either is a pass for the card; which one it is tells whether the corner on a line was the Tower 171 cause. |
