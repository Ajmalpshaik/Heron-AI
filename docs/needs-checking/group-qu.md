# Needs checking — Group QU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QU - REPORT_CURTAIN_ELEMENTS version 3: a curtain system read face by face, curtain walls word for word as before - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1427](https://github.com/Ajmalpshaik/Heron-AI/issues/1427) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on main, measured 2026-11-24 and again 2026-11-26 with `tools/score-routing.py` on all four backends,
*"how many panels are on this curtain system"* reached `SET_CURTAIN_PANEL_TYPE` or `ADD_CURTAIN_GRID_LINES` -
writes (D-86) - because no read knew a curtain system: [`REPORT_CURTAIN_ELEMENTS`](../../brain/fragments/report-curtain-elements/fragment.yaml)
version 2 read curtain WALLS only and named a curtain system "not a wall" ([row 5b-2028](../fragment-issues/row-5b-2028.md),
issue [#1365](https://github.com/Ajmalpshaik/Heron-AI/issues/1365)). On the owner's choice of 2026-11-26 (*"Teach
it (Revit later)"*), version 3 reads each face's grid of a curtain system (`CurtainSystem.CurtainGrids`) the way
it reads a curtain wall's one grid, and declares the question. The card was PROVEN at version 2; its C# moved, so
it is DRAFT until this group is run and signed (D-30). Its `tests/cases.yaml` holds every case.

**What is done and what is not.** The code compiles for Revit 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-26). **Never run in Revit.** That `CurtainGrids` gives one grid per face, and that a face grid's
`GetPanelIds` and `GetMullionIds` answer as a curtain wall's do, are expected, not seen.

**The model: scratch** - Revit 2020 (the release version 2 was proved on). A new project from the default metric
template, saved under `%TEMP%\heron-work\qu\`: one In-Place or loadable mass box, a curtain system made By Face on
two of its faces with a type that has a grid each way and mullions, and one ordinary curtain wall beside it. Every
run is a read; nothing is kept.

| ID | Do this | Pass looks like |
|---|---|---|
| **QU2** | **Negative, first.** Select the mass alone (not its curtain system) and run `REPORT_CURTAIN_ELEMENTS` | The mass named in `notCurtain` *"is not a wall or a curtain system"*; `curtainSystemsFound` 0, `panelCount` 0. Read as a curtain system because one sits on it is a FAIL |
| **QU1** | Select the curtain system alone and run it | `curtainSystemsFound` 1, `curtainWallsFound` 0; findings open *"0 curtain wall(s) and 1 curtain system(s) (2 face grid(s)): ..."*, then two blocks headed *"curtain system face 1 of 2:"* and *"face 2 of 2:"*. COUNT each face's panels, mullions and grid lines on the elevation in Revit and write them beside what came back - never take the return value as its own proof |
| **QU3** | Select the curtain system and the curtain wall together | The wall's block word for word as version 2 writes it, the system's two face blocks after it; `panelCount` and the last line (*"Across the 1 curtain wall(s) and 1 curtain system(s): ..."*) count all three grids |
| **QU4** | Select the curtain wall alone | findings word for word as version 2 - opening *"1 curtain wall(s): ..."*, last line *"Across the 1 curtain wall(s): ..."* - and `curtainSystemsFound` 0 |
| **QU5** | In a chat, ask *"how many panels are on this curtain system"* with the system selected | `heron_lookup` / `revit_read` answers with `REPORT_CURTAIN_ELEMENTS`, not a change; write down the answer's opening line |
