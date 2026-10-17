# Needs checking — Group FE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FE - change a value and keep a face where it is: `SET_PARAMETER_VALUES_BY_ID` version 3, `keepFace` (2026-10-17)

[Row 5b-560](../fragment-issues/section-5b-rows-401-425.md). `SET_PARAMETER_VALUES_BY_ID` (FRG-PAR-027) was
PROVEN at version 2 and is **DRAFT at version 3**; version 2's proof is kept on the card as a record. Version 3
adds one optional input, `keepFace`: `outside` - the face looking out of the building - or `left`, `right`,
`top`, `bottom`, `near` or `far` of a named view (`left of Section 3`). Before the write each element's face is
measured off its geometry; after the write and a regeneration it is measured again and the element is moved
back by exactly the difference, in the same transaction - **one undo entry** - and measured a third time. A
face not back within 0.1 mm throws and takes every row back. Left out, `keepFace` reads `none` and the run is
version 2's.

**Why.** On 2026-10-15/17 the owner set the instance `Wall Width` of 18 openings
(`TRG_GEN_CutOut_Rectangular_HVAC : 12.5mm Cutout Buffer`, a real project, Revit 2020) from 255.5 or 260 to
100 and kept the outside face. By hand: this tool once and `MOVE_ELEMENTS` five times - six undo entries - and
13 of the 18 held 255.5 while Revit showed 260, so a move worked out from the shown value was 2.25 mm out.

**What is built and what is not run.** The fragment compiles for all eight releases 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-17, in the cloud), and the job file below reads `WOULD RUN` under
`batch-prove --dry-run`. Routing was measured main against branch (5b-560). **None of it has run in Revit.**
Every number below about what Revit does is the design's, not a measurement.

**A NEW SCRATCH PROJECT, never a working model - and never any `4355-*` model.** With
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null` on
every command line.

| # | Run | Look for |
|---|---|---|
| **FE1** | Arrange, **Revit 2020**: a new project from the default metric template, saved as **Heron Keep Face Test**. Project Units > Length rounded to **10 mm**. On Level 1: a 250 mm **exterior** wall, a floor with **Structural** ticked on **one side** of it only (the inside), and a 250 mm **interior** wall with that floor on **both** sides. A new **Generic Model** family: a box whose depth is an instance length parameter **Wall Width**, two reference planes **equal** about the origin (the way the real opening shrinks about its LocationPoint); load it and place one in each wall at mid-height, depth across the wall, **Wall Width 255.5**. Mark **BOX-OUT** in the exterior wall, **BOX-MID** in the interior one | Properties shows Wall Width **260** for both - the rounding trap is in place. A dimension from a grid to BOX-OUT's outside face, written down |
| **FE2** | `python tools/batch-prove.py tools/jobs/keep-face-scratch-2026-10-17.yaml --dry-run`, then without `--dry-run` | Positive `written` 1; in `resultJson` the readBack entry's `face`: side outside, `writeMovedItMm` **-77.75**, `movedBackMm` **77.75**, `offByMm` 0, `signs.slab` naming the floor. Negative (BOX-MID) `written` 0 and `refused` saying **none of the three signs of the outside applies**. Afterwards BOX-OUT still reads 260 and its dimension is unchanged - both legs are rolled back |
| **FE3** | The **second route** (D-30), by hand: `revit_change` with BOX-OUT's row and `keepFace` `outside`, through `try-once` (D-119), and press **Keep** | The grid-to-outside-face dimension from FE1 **unchanged to the millimetre**; the inside face moved 155.5 mm; Wall Width reads 100. **One** entry in Revit's undo list, `Heron: ...` - Ctrl+Z once puts the value **and** the position back |
| **FE4** | FE3's row with `keepFace` **left out** | Version 2's answer: BOX-OUT shrinks about its centre, **both** faces move 77.75 mm, nothing is moved, and `resultJson` has **no** `keepFace` key. Ctrl+Z |
| **FE5** | A view side: BOX-OUT's **Height** (or any length that moves a face up or down) with `keepFace` `bottom of South` | The bottom face where it was, the top moved by the whole change. Then `left of No Such View`: nothing written, the reply names the view |
| **FE6** | The refusals, nothing written each time: BOX-OUT **pinned** with `keepFace` `outside` by id; `keepFace` `left` with no view; BOX-OUT in a **model group** | Each names its reason in words - pinned, needs a view, in a model group - and the model is unchanged |
| **FE7** | The other two signs, on the same scratch project: link a second new project holding a **20 mm** wall 15 mm outside the exterior wall, and draw two ducts **inside** within 3 m of BOX-OUT; run FE3 again | `signs.skin` names the 20 mm linked wall and `signs.ducts` counts the duct ends inside; all three agree. Then move the 20 mm wall to the **inside**: the signs disagree and the run refuses, naming what each found |
| **FE8** | FE2 on **Revit 2024** and **2027** | The same answers. Compiled on eight releases here; what Revit DOES was measured on none |
| **FE9** | The signature, on the owner's word only, after FE2 to FE7 | `python brain/heron_validate.py accept set-parameter-values-by-id --by "Ajmal PS"` - and only then may the card go back to PROVEN |
