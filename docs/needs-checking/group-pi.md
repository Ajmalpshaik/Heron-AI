# Needs checking — Group PI

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PI - CREATE_GRIDS version 4: what was made is said in findings, and refused holds a refusal and nothing else - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1200](https://github.com/Ajmalpshaik/Heron-AI/issues/1200) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-21, on the owner's PC (Revit 2024, the scratch project `roofjoin-oy`), one grid named
*Wing* drawn between two points and kept came back with `refused` holding *"1 grid line(s) drawn between
the points given, each one level"* - [row 5b-1948](../fragment-issues/row-5b-1948.md), issue
[#1174](https://github.com/Ajmalpshaik/Heron-AI/issues/1174). Version 3 did it on purpose in all three of
its ways of making grids. Version 4 says what was made in a new output, `findings`, word for word as
version 3 said it in `refused`, and leaves `refused` a reason nothing was made. That changed the C# of a
PROVEN card, so it went back to DRAFT at version 4, on the owner's standing word that a real defect is
fixed even though the proof goes stale; version 3's proof (2026-11-02, P1102 MEP, Revit 2020) is kept on
the card as the record ([D-30](../DECISIONS.md)).

| Tool | Was | Now | Row | Checks |
|---|---|---|---|---|
| [`create-grids`](../../brain/fragments/create-grids/fragment.yaml) (`CREATE_GRIDS`) | PROVEN v3 | DRAFT v4 | [5b-1948](../fragment-issues/row-5b-1948.md) | PI1 to PI4 |

It compiles on every release it claims, 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud
container, 2026-11-21), and `tests/test_grid_and_copy_values.py` reads where each sentence goes. **None
of it has run in Revit.** The card's own `tests/cases.yaml` holds the same cases, marked NOT RUN.

**The model - Heron builds it, and it is nobody's work.** A new scratch project from Revit's default
template, on Revit 2024 (where the fault was seen) and then Revit 2020, saved under `%TEMP%\heron-work\pi`.
Every run is ROLLED BACK - no check here keeps a grid. While the card is DRAFT, `revit_change` refuses
it ([D-117](../decisions/D-117.md)): each run goes through the proving client as a rolled-back preview.

| ID | Do this | Pass looks like |
|---|---|---|
| **PI1** | *Positive, between points - the call seen on 2026-11-21.* `gridLines=4000,-6000,0; 8000,-6000,0` and `gridNames=Wing`, every other value left out | `created` 1 item, `namedAs` [Wing]; **`refused` 0 items**; `findings` one item, *"1 grid line(s) drawn between the points given, each one level"*. Version 3 put that line in `refused` - if it is there, this FAILS |
| **PI2** | *Positive, bay spacings.* `spacingsAcross=6000, 6000`, `spacingsUp=6000, 6000`, `firstLetter` and `firstNumber` names the project does not hold | `created` 6 items; `refused` 0 items; `findings` one item, *"2 spacing(s) across gave 3 lettered grid(s), and 2 spacing(s) up gave 3 numbered grid(s). Spacings are GAPS ..."* |
| **PI3** | *Positive, radial.* `centreMm=0,0`, `radiiMm=6000`, `spokeAnglesDegrees=0, 180`, `startDegrees=0`, `endDegrees=90` | `created` 3 items - one arc, two spokes; `refused` 0 items; `findings` two items - the arcs and spokes with the names Revit gave them, read back, and *"the spoke at 180 degrees lies outside the arcs' sweep - drawn as asked"*. **RECORD** whether `radialNotes` also arrives in the reply as a list of its own, and how many times the spoke's line is printed |
| **PI4** | *Negative.* `gridLines=0,0,0; 30000,0,500` | Refused before anything is made: `refused` names 500 mm and says a grid is a vertical plane; `created` 0 items; **`findings` 0 items** - a refusal says nothing was made, and nothing in the answer says it was; the project's grid count unchanged |

**Signing, once PI1 to PI4 pass:** the proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
