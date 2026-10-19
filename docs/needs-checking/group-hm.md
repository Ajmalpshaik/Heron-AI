# Needs checking — Group HM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HM - PLACE_STRUCTURAL_FAMILY v4: a beam is refused, a placement with no geometry is a failure (2026-10-19)

From [row 5b-1201](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020.2 a Structural Framing
beam asked of `PLACE_STRUCTURAL_FAMILY` at three points answered placed 3, failed 0, and Revit then
deleted the geometry of two - zero-length beams kept as a success.

**What changed.** `place-structural-family` version 4 refuses, before anything is placed, a request
for a beam or a brace and any Structural Framing family, with a sentence naming
`PLACE_LINE_BASED_FAMILY` and its `curves` form. A column or foundation family asked as a beam keeps
version 3's sentence. After one regenerate every placement is read, and one that is gone or holds no
solid is removed again inside the same undo and counted in `failed`, never in `placed`.

**What is done and what is not.** It compiles for 2020 to 2027 (`tools/check-fragments-compile.py`).
**Nothing here has run in Revit.** The card is DRAFT until HM1 to HM4 pass and the owner signs; version
3's proof is kept on the card as a record and is stale. Run on a **scratch copy** with Level 1 at 0 and
Level 2 above it, a structural column type, `M_Concrete-Rectangular Beam : 400 x 800mm` (or any
Structural Framing type) and a Generic Model family that holds only model lines loaded - never a working
model. Count Structural Columns, Structural Framing and Generic Models in `{3D}` before each run.

| # | Run | Look for |
|---|---|---|
| **HM1** | *Positive - a column still goes in.* Two points on Level 1, the structural column type, `structuralType=Column`, `topLevelName` and `topOffset` blank | `placed` 2, `failed` 0, a finding *"2 placed as Column on 'Level 1'"*, and **no** finding about solid geometry. Both columns read as structural in the column schedule. Then once more with `topLevelName=Level 2`, `topOffset=-150`: Top Level and Top Offset read so in Properties, `tops` 2 lines, `topWrong` empty |
| **HM2** | *Negative - row 5b-1201's own case.* The Structural Framing type, `structuralType=Beam`, Level 1, points `23420,0,0; 23640.272,2142.771,4000; 23569.859,4319.298,8000` | **Nothing placed:** `placed` empty, `failed` 0, one finding naming the family as Structural Framing and saying to ask `PLACE_LINE_BASED_FAMILY` with `curves`. The Structural Framing count is the same as before; no *"Line is too short"* or *"Can't create sweep"* warning. Then the same type with `structuralType=Column`: refused the same way. **FAIL:** any beam in the model |
| **HM3** | *Negative - a brace of another family.* The Generic Model family, `structuralType=Brace`, one point | Refused, *"A brace is placed along a line between its two ends"*, naming `PLACE_LINE_BASED_FAMILY`; nothing placed. Then the column type with `structuralType=Beam`: version 3's sentence - a Structural Columns family, ask for it as a column - and nothing placed |
| **HM4** | *The geometry read-back.* The Generic Model family (model lines only), two points, `structuralType=non-structural` | `placed` empty, `failed` 2, one finding naming both points as left with no solid geometry and removed again; the Generic Models count is the same as before; **no** delete box (the elements were made by the same run). **Say which it was** if Revit kept them: whether `get_Geometry` at Fine detail read a solid that is not there, or the delete was refused |
| **HM5** | *The tool it names takes the job.* `PLACE_LINE_BASED_FAMILY` with the same Structural Framing type, `structuralType=Beam`, `curves` `23420,0,0; 23640.272,2142.771,4000 \| 23640.272,2142.771,4000; 23569.859,4319.298,8000` | Two sloped beams, each with its geometry, and no warning. **Not part of HM's verdict** - that tool's proof is the owner's call; this row only says whether the sentence HM2 gives leads somewhere that works |

**Then sign** `place-structural-family` on HM1 and HM2. HM3 and HM4 are the cases its `tests/cases.yaml`
names beside them; HM5 is for `PLACE_LINE_BASED_FAMILY`'s own proof, when the owner takes it.
