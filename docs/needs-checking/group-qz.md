# Needs checking — Group QZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QZ - D-207's name rules: CHECK_FAMILY_STANDARDS v5 reads a name pattern as a * wildcard, CREATE_GRIDS v5 and CREATE_LEVELS v3 skip a name already taken and make the rest - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1444](https://github.com/Ajmalpshaik/Heron-AI/issues/1444) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the owner's answers of 2026-11-26 to [row 5b-1879](../fragment-issues/row-5b-1879.md) items 3 and 4
(issue [#1088](https://github.com/Ajmalpshaik/Heron-AI/issues/1088)), recorded as
[D-207](../decisions/D-207.md): *"Wildcards \*"* - `CHECK_FAMILY_STANDARDS`' `namePattern` is a `*` wildcard on
the whole family name, as `CHECK_MODEL_STANDARDS` reads its patterns, where versions 1 to 4 read it as
*contains*; and *"Skip it, make the rest, and the skipped one needs to inform the user that we skipped because of
this"* - [`CREATE_GRIDS`](../../brain/fragments/create-grids/fragment.yaml) and
[`CREATE_LEVELS`](../../brain/fragments/create-levels/fragment.yaml) skip a name already taken, make the rest and
name each skip in `skipped`, as `CREATE_PROJECT_REFERENCE_PLANES` already did, where they refused the whole call.
All three cards were PROVEN; their C# moved, so each is DRAFT until this group is run and signed (D-30). Each
card's `tests/cases.yaml` holds every case; these rows are the run.

**What is done and what is not.** Each change is in code and compiles for every release the card claims, 2020 to
2027 (`tools/check-fragments-compile.py`, 2026-11-26, in a Linux container). **Never run in Revit.** That a grid
or level taken out of the plan before anything is made leaves the rest made under one undo entry is expected, not
seen.

**The model: scratch, never a working one** - Revit 2020 first, then 2024. `names-scratch`: new from the metric
template, saved under `%TEMP%\heron-work\qx\`. It has the template's `Level 1` and `Level 2`; add straight grids
`B` and `b2` anywhere, and load three families renamed `TRG_Supply Diffuser`, `TRG_Return Grille` and
`Supply Diffuser TRG` (any small loadable family, three copies).

| ID | Do this | Pass looks like |
|---|---|---|
| **QZ1** | **Negative, first.** `CREATE_LEVELS` with elevations `9000` and names `Level 2` only | NOTHING MADE - the level count in an elevation unchanged; `created` empty; `skipped` one line naming `Level 2`, 9000 mm and level `Level 2`; `refused` saying no level was created because every level asked for was skipped. The reply must not read as a change (no Keep or Discard for nothing) |
| **QZ2** | `CREATE_LEVELS` with elevations `7000, 9000` and names `Level 3, level 2` | `Level 3` made at 7000 mm and kept - one entry in the undo list; nothing at 9000 mm; `skipped` naming `level 2` and the level `Level 2` that holds it - capitals ignored; `refused` null |
| **QZ3** | **Negative.** `CREATE_GRIDS` with gridLines one line `0,20000,0; 30000,20000,0` and gridNames `B` | NOTHING MADE; `skipped` naming B and grid B; `refused` starting "NO GRID WAS CREATED: every grid asked for was skipped" |
| **QZ4** | `CREATE_GRIDS` with firstLetter `A`, spacingsAcross `6000,6000`, no spacings up | grids A and C made, C 12000 mm from A, nothing at 6000 mm; `skipped` one line for B; `findings` saying 1 grid skipped and 2 made; one undo entry takes both away |
| **QZ5** | **Negative.** `CHECK_FAMILY_STANDARDS` in the project with namePattern `TRG` - an old *contains* value | all three families off standard for their name, *"name does not match the pattern 'TRG'"* - none is named exactly TRG. Version 4 passed all three |
| **QZ6** | `CHECK_FAMILY_STANDARDS` with namePattern `TRG*`, then `*TRG*` | `TRG*`: the two starting TRG pass the name check and `Supply Diffuser TRG` fails it. `*TRG*`: all three pass |
| **QZ7** | Open one of the three families in the Family Editor; `CHECK_FAMILY_STANDARDS` with namePattern `TRG*` | the family's own report reads *"Name matches the pattern"* for a TRG_ family, and FAIL with the rename fix for `Supply Diffuser TRG` |
| **QZ8** | QZ2, QZ4 and QZ6 on Revit 2024 | the same answers |

Then sign each card from the run on its proof in the owner's name (the [`fragment-proving`](../../.claude/skills/fragment-proving/SKILL.md)
skill) - never by a session.
