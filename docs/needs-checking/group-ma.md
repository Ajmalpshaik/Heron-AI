# Needs checking — Group MA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MA - Annotate > Arc Length - an arc length dimension along a curved wall, curved beam or arc line, Revit 2025 and later: `CREATE_ARC_LENGTH_DIMENSION` (2026-11-02)

Asked for in the modeller's words: *"add an arc length dimension to this curved wall"*, *"dimension the
length of this arc"*, *"put an arc length dimension on this curved beam"* - Revit's Annotate > Arc Length.
Nothing in Heron drew one: `brain/heron_retrieve.py`, Revit 2025, offered `CREATE_ANGULAR_DIMENSION` for
all three, which measures an angle between two straight arms. `CREATE_RADIAL_DIMENSION` (DRAFT) was weighed
for widening and not widened: radius and diameter are one Revit call with a flag, an arc length is another
call with three references.

| What | Where | What it does |
|---|---|---|
| `CREATE_ARC_LENGTH_DIMENSION` (MODIFY) | [`brain/fragments/create-arc-length-dimension`](../../brain/fragments/create-arc-length-dimension/fragment.yaml) | Draws one arc length dimension in **one view** along ONE curved wall, curved beam or arc model or detail line - selected, or typed as `wall ID`, `beam ID`, `line ID` - between its two ends: for a line its own end points, for a wall or beam its end faces, along the `exterior` (left out) or `interior` side face. The dimension line sits `offsetMm` outside the arc, 500 mm when left out. **An arc length dimension type by name, or the project's default arc length type - named in the answer.** Refused by name, nothing drawn: nothing or more than one selected, an element that is not a wall, beam or line, a straight one, a full circle, a view Revit draws no dimension in (a schedule, a sheet, a legend, a perspective or unlocked 3D view, a template), a view that sees the arc edge-on, a type that is not an arc length type. Read back: the length and the type; for a line the length must equal the line's to 0.5 mm or the call is rolled back |

**What is done and what is not.** It compiles on the three releases it claims, 2025, 2026 and 2027, and
the whole library on all eight (`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every
Revit member it uses was looked up with `tools/api-surface --members`: `ArcLengthDimension.Create(Document,
View, Arc, Reference, IList<Reference>)` is in 2025 to 2027 only - the type does not exist before 2025 -
and everything else is present 2020 to 2027. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (MA2 to MA4 measure them):
whether Revit takes a wall's curved SIDE FACE as the arc reference in a plan (the UI does, the API's
remarks do not say); whether a curved beam's faces from `GetSymbolGeometry` carry references Revit will
dimension to, or a beam swept per instance has none there; whether the create call sets the default arc
length type itself (the card sets the type afterwards either way and reads it back); and whether a joined
wall end reads back the length the card reports or refuses.

**Routing.** Measured with `tools/check-routing.py` and `heron_brain.lookup` on two scratch stores, before
and after the card: every sentence it declares reaches it. *"put it back"* moved from one write it does
not name to another - [row 5b-1424](../fragment-issues/section-5b-rows-401-425.md), OPEN, with rows 1375
and 1405.

**The model - a scratch project from the metric architectural template in Revit 2025 or later.** Never a
working model. Every run below is rolled back except MA1's arrangement and MA4's one run. The job file is
[`tools/jobs/create-arc-length-dimension-ma-2026-11-02.yaml`](../../tools/jobs/create-arc-length-dimension-ma-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MA0** | *Open.* Revit 2025 or later, a new project from the metric architectural template, **nothing selected**, Changes on at the ribbon | The title bar names the scratch project |
| **MA1** | *Arrange, and KEEP it.* By hand on Level 1: W1 (Wall > Arc, radius 5000 mm to its location line, 90 degrees, unjoined), W2 (a straight wall, unjoined), L1 (an arc detail line in "FloorPlan: Level 1", radius 3000 mm, 60 degrees), B1 (a curved beam from Structure > Beam, arc). Save. Write L1's and W2's ids into the job file | "FloorPlan: Level 1" shows W1, W2, L1 and B1, and no dimension |
| **MA2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative: W2 - created none, *is straight*. Positive: one id, *arc length 3141.6 mm*, `typeUsed` the default arc length type, named as the default |
| **MA3** | *Walls, beams, types and the refusals.* W1 selected; W1 with `face` interior and `dimensionType` an arc length type the model has; `beam <B1's id>`; W1 in a section across it, an unlocked 3D view, a schedule; W1 with `dimensionType` a linear type; W1 and L1 selected together | W1: one dimension along the exterior face - **record** whether Revit took the face. Interior: a different length by the wall's thickness x pi / 2, the type read back as named. B1: **record** whether Revit took the beam's faces. Section, 3D, schedule: refused by name. Linear type: refused as one, the arc length types listed. Two selected: refused |
| **MA4** | *Kept, through `try-once`, on the modeller's yes.* MA2's positive on the saved project, "Level 1" in front | Keep or Discard. Then Revit's own Annotate > Arc Length picked on L1 reads the same length: the second route |
