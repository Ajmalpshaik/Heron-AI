# Needs checking — Group JJ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JJ - one curtain wall's own grid lines, added by position or in equal panels: `ADD_CURTAIN_GRID_LINES` version 1 (2026-10-28)

A new card, [`add-curtain-grid-lines`](../../brain/fragments/add-curtain-grid-lines/fragment.yaml) (FRG-ELE-129),
written in a cloud session for the modeller's *"divide this curtain wall into six panels"*, *"add a grid line
at 1200 from the bottom of this curtain wall"*, *"add vertical grid lines at 900 and 2700 on just this
wall"*, *"split this curtain wall into 4 equal bays with mullions"* and *"add a transom at 1100"*. Until it,
[`set-curtain-wall-grid`](../../brain/fragments/set-curtain-wall-grid/fragment.yaml)'s routing table sent the
first and *"add a grid line here"* to **NOT HERE, and nothing does yet**: that card changes a TYPE's layout and
refuses Fixed Number, because the count is each wall's own. This one adds lines to the selected wall's own
curtain grid (`CurtainGrid.AddGridLine`), optionally with mullions of a named type
(`CurtainGridLine.AddMullions`), and writes nothing on the type.

**What is done and what is not.** Every member it calls was read off all eight releases' reference
assemblies with `tools/api-surface` on 2026-10-28, one signature each, and it compiles for 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-28, in the cloud). Its five sentences reach it by identity on
this container's backend. **Nothing has met a Revit.** `revit_change` refuses it until JJ10 is signed.

**What only a Revit can settle**, each written on the card as an ASSUMPTION or a measured choice:

- **Which of Revit's U and V sets runs vertical on a wall.** The card takes it from the wall's own lines;
  on a wall with none, from the one recorded measurement (SET_CURTAIN_WALL_GRID's proof, Project2, Revit
  2024: a wall's vertical lines came back in Revit's V set). Every new line is then measured, and one that
  runs the other way stops the call with nothing kept. JJ1 records the set on a release with no line to
  read from.
- **What a later change to the type's own grid does to lines added here** - kept, moved or dropped (JJ6).
- **Whether Revit puts the wall type's own interior mullions on a line added by hand** (JJ7).
- **That the point each line is put through is accepted** - on the grid's own plane, measured off the wall's
  curtain cells, in the middle of the widest gap between the lines already there the other way.

**The rig (JJ0).** A new project, saved as `%TEMP%\heron-work\group-jj\Heron JJ Curtain Grid.rvt` - a run
through `try-once` needs a saved model. The batch proof draws its own wall inside the rolled-back group;
the rig also holds two walls drawn by hand for the rows that need a wall already there.
[`tools/jobs/add-curtain-grid-lines.yaml`](../../tools/jobs/add-curtain-grid-lines.yaml) runs JJ1 and JJ2
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and nothing selected unless the row says so.

| # | Run | Look for |
|---|---|---|
| **JJ0** | *The rig, by hand.* New project; *Level 1*; REPORT_CURTAIN_WALL_TYPE on *Curtain Wall: Curtain Wall*; SELECT_TYPES on Walls for *Basic Wall: Generic - 200mm* and on Curtain Wall Mullions for a rectangular mullion type. Then, kept: Duplicate *Curtain Wall* as **HERON JJ 1500**, Vertical Grid Layout Fixed Distance, Spacing 1500; draw one 9000 x 3000 mm wall of it from 0,70000 to 9000,70000 on Level 1; draw one 9000 x 3000 mm wall of *Curtain Wall: Curtain Wall* from 0,80000 to 9000,80000 (**W1**) a second beside it from 0,85000 to 9000,85000 (**W2**), and one *Basic Wall: Generic - 200mm* from 0,90000 to 9000,90000 (**B1**). Save | Both layouts of *Curtain Wall: Curtain Wall* **None**; the basic and the mullion type exist (their names go into the job file and the cases if they differ); the line 0,60000 to 9000,60000 empty; saved |
| **JJ1** | *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/add-curtain-grid-lines.yaml --session <pid>`, its positive leg - CREATE_WALL draws a 9000 x 3000 wall of *Curtain Wall: Curtain Wall*, then `direction=vertical`, `positions=equal 6`, rolled back | Binding note *"elements from create-wall as 'created' (1)"*. **`added` 5**, `mullionsAdded` 0, `skipped` and `notCurtain` empty; `readBack` *"9000 mm long and 3000 mm high"*, vertical grid lines 0 before and 5 now, *"Vertical lines now at 1500, 3000, 4500, 6000, 7500 mm from its start"*, panels 1 before and 6 now, the flag *"from the one recorded measurement"*. **RECORD** which of Revit's U and V sets the five are in. **FAIL:** a line measured at another position. A refusal *"Revit made the line asked for at 1500 mm vertical in its V set ... and it runs horizontal"* is the recorded set not holding on this release - a finding about Revit, recorded, and nothing was kept |
| **JJ2** | *The negative - the D-30 negative.* JJ1's negative leg: the same request on a *Basic Wall: Generic - 200mm* drawn on the same line | `added` 0, `readBack` empty; `notCurtain` names the wall *"is a basic wall, not a curtain wall - it has no grid"*; `refused` begins *"Nothing given is a curtain wall"* |
| **JJ3** | *The other cases, rolled back.* `python mcp/client/heron_bridge_client.py validate add-curtain-grid-lines --in "Heron JJ Curtain Grid" --write --setup create-wall` with JJ1's wall values and, one run each: `direction=horizontal positions=1100 mullionType=<the rig's rectangular mullion type>`; `direction=vertical "positions=900, 2700"`; `positions=1.2`; `direction=diagonal`; `positions=1200mm`; `"mullionType=Rectangular Mullion: ZZZ No Such Size"` | As [`cases.yaml`](../../brain/fragments/add-curtain-grid-lines/tests/cases.yaml) says, each: 1 line at 1100 from the base and `mullionsAdded` 1 (or the JJ7 finding); 2 lines at 900 and 2700; then four refusals, each *"Nothing was changed"*, the last WITH the mullion types the rig has |
| **JJ4** | *A wall whose type already places lines.* The **HERON JJ 1500** wall selected; `validate ... --write` with `direction=vertical "positions=750, 1500"`; then `positions=equal 4`; then `positions=equal 6` | First: `added` 1, `skipped` *"1500 mm - the wall already has a vertical line there, at 1500 mm"*, the flag *"measured on this wall"*. Second: refused, naming 1500, 3000, 6000 and 7500 as not on the quarters, *"8 panels, not 4"*. Third: `skipped` 5, `added` 0. HERON JJ 1500's Type Properties still Fixed Distance 1500 after each |
| **JJ5** | *The kept run, on the owner's yes only.* **W1** selected, the rig saved, a view showing W1 in front: `python mcp/client/heron_bridge_client.py try-once add-curtain-grid-lines --set direction=vertical --set "positions=equal 6"`; the owner chooses Keep or Discard | Kept: W1 shows five vertical lines in six equal panels; **W2** - REPORT_CURTAIN_ELEMENTS before and after - unchanged; one Ctrl+Z takes it back. Its negative half is the proving session's: `validate add-curtain-grid-lines --write --kept ...` with **B1** selected and the same request, rolled back - `added` 0, B1 named in `notCurtain` |
| **JJ6** | *ASSUMPTION - a type change afterwards.* After JJ5 is kept: Edit Type on W1, Vertical Grid Layout Fixed Distance 2000, OK; look at W1; then Ctrl+Z | Whether the five lines added to W1 stay, move or go when the type's own grid changes - **not known**; recorded as a finding on the card |
| **JJ7** | *ASSUMPTION - the type's interior mullions.* Duplicate *Curtain Wall* as **HERON JJ MULLIONS**, Horizontal Mullions Interior Type the rig's rectangular mullion, layouts None; draw one wall of it; select it; `validate ... --write` with `direction=horizontal positions=1100` and no mullion type | `readBack`'s mullions before and after: whether Revit put the type's interior mullion on the new line - **not known**. Then with `mullionType` named: `mullionsAdded` 1, or the finding *"Revit had already put mullions on 1 of the new lines"* |
| **JJ8** | *Revit 2024* (or 2020, whichever JJ1 was not). JJ0 to JJ2 on that release, JJ1 with `--release <year>` once the first release is signed | The same answers, and the same U-or-V set or a recorded difference |
| **JJ9** | *Routing, on the PC's backend.* `heron_lookup` for each of the five sentences; *"add a grid line"*; *"how many grid lines are in this curtain wall"*; *"where are the grid lines on this curtain wall"* | The five reach ADD_CURTAIN_GRID_LINES; *"add a grid line"* reaches CREATE_GRID; the count question reaches REPORT_CURTAIN_ELEMENTS (declared there 2026-10-28). The last one **reached this card on the container's backend** - no READ says where a wall's grid lines sit; record what the trained model answers |
| **JJ10** | The signature, on the owner's word only, after JJ0 to JJ5 | `python brain/heron_validate.py accept add-curtain-grid-lines --by "Ajmal PS"` and `heron-status` set to PROVEN in the same change; `--release` for the other release after JJ8 |

A row that fails stays open; nothing in a row changes to make it pass.
