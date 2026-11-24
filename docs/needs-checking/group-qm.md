# Needs checking — Group QM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QM - CREATE_CONCEPTUAL_FORM version 2: a loft Revit refuses says what is in the profiles - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1371](https://github.com/Ajmalpshaik/Heron-AI/issues/1371) since 2026-11-24** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**THIS CARD IS BLOCKED UNTIL QM1 PASSES.** `CREATE_CONCEPTUAL_FORM` is `DRAFT`, so a chat refuses it
([D-117](../DECISIONS.md)) - every surface, extrusion, loft, sweep and revolve in a mass, adaptive or
pattern-based family, **Tower 171's glass-mass work included**. The owner chose this on 2026-11-24 (Ajmal: *"A"* -
say exactly which profile is wrong, and accept the card going back to DRAFT until re-proved). **The quick way
back:** [`tools/jobs/conceptual-form-loft-qm-2026-11-24.yaml`](../../tools/jobs/conceptual-form-loft-qm-2026-11-24.yaml)
- its header has the five-minute arrangement; then `python tools/batch-prove.py` on it, and the owner signs.

**Why:** [row 5b-2021](../fragment-issues/row-5b-2021.md), issue [#1345](https://github.com/Ajmalpshaik/Heron-AI/issues/1345).
On Revit 2024, in the Tower 171 glass-mass family, two lofts through 3 closed profiles answered only Revit's own
*"Cannot create loft form"*. Version 2 of [`create-conceptual-form`](../../brain/fragments/create-conceptual-form/fragment.yaml)
reads the profiles **after** Revit has said no and names what it finds: different numbers of sides, two straight
sides on one line (with the point), a profile open where the others close, one not flat, two in one plane - or
says none was found and to loft two profiles at a time. **Nothing is refused before Revit is asked**, so a loft
that works today is not blocked. It compiles for Revit 2020 to 2027 (`tools/check-fragments-compile.py`).
**None of it has met a Revit.** The card is `DRAFT` until QM1 passes ([D-30](../DECISIONS.md)). Rebuild
the add-in and restart Revit first (`python tools/heron-ready.py`) - that needs Revit closed, so it is the
owner's step.

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`create-conceptual-form`](../../brain/fragments/create-conceptual-form/fragment.yaml) (`CREATE_CONCEPTUAL_FORM`) | PROVEN v1 | DRAFT v2 | [5b-2021](../fragment-issues/row-5b-2021.md) | QM1, QM2, QM3 |

**The model: a scratch mass family, never a working one** - a new *Metric Mass* family saved in
`%TEMP%\heron-work\qm`. Draw the profiles with `DRAW_FAMILY_POINT_CURVES` (reference lines through points).

| ID | Do this | Pass looks like |
|---|---|---|
| **QM1** | *The D-30 proof - the job file above.* Positive: two closed rectangles of reference lines, 6000 x 3000 on Level 1 and 4000 x 2000 at 3000 mm; both as profiles, depth empty. Negative: the same with depthMm 500. | `PASS`: a solid loft, its volume read back (about 38000 L) and nothing said about the profiles; the negative refused *"A depth was given with 2 profiles"*, nothing made. Then the owner signs, and the card is `PROVEN` again. |
| **QM2** | *Version 2's words - in a chat, on the same family.* Draw two more closed rectangles side by side on Level 1; ask for a loft through the two. | The call fails and rolls back: Revit's message, then *"In these profiles: profiles 1 and 2 lie in one plane"*. Nothing made. **If Revit makes a form instead**, record it: the words are read only after Revit says no. |
| **QM3** | *The case from Tower 171.* Three closed profiles one above another; the bottom one a rectangle drawn as FIVE lines (one long side split in two), the others four lines each. | **RECORD** which comes back: a loft (Revit took it - then nothing is said), or a failure naming *"different numbers of sides - 5, 4, 4"* and *"profile 1 has two straight sides on one line, meeting at (x, y, z) mm"*. Either is a pass for the card; which one tells whether the corner on a line was the Tower 171 cause. |
