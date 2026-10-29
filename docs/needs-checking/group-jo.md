# Needs checking — Group JO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JO - how many different panel sizes a curtain wall has, within a tolerance: `REPORT_CURTAIN_ELEMENTS` version 2 (2026-10-29)

Recorded as worth building in [doc 45 §6](../45-five-more-downloaded-skill-files.md) (*"Panel sizes on a
curtain wall, grouped within the modeller's tolerance"*) and in the
[facade plan](../work-notes/plans/facade-capability.md), where it waited for the owner's word because it
widens a PROVEN tool. The owner gave it on 2026-10-29 (*"build the three widenings too"*), knowing the change
makes the proof stale. *"How many different panel sizes does this facade have"* is the fabricator's first
question, and version 1 counted a wall's panels without saying how big any of them was.

**What changed.** [`report-curtain-elements`](../../brain/fragments/report-curtain-elements/fragment.yaml)
version 2 reads each panel's Width and Height as Revit gives them - `CURTAIN_WALL_PANELS_WIDTH` and
`CURTAIN_WALL_PANELS_HEIGHT`, off the element in the cell, never its type and never the grid's spacing - and
groups them within `toleranceMm`, **5 mm when the modeller gives none**, 0 for exact. Each wall's block in the
answer ends with its own sizes; the last line groups every wall handed in together, and `differentPanelSizes`
and `panelSizes` are that answer. A panel whose size Revit does not give is named in `sizesNotRead` with the
reason and is in no size (D-52). An empty cell is in no size and is still counted in `emptyCells`. A door or a
basic wall in a cell is sized like any cell and said - *"(1 of them a door)"*. **Everything version 1 wrote is
still written, in its words and its order**; the new lines are added after it.

**What is done and what is not.** Every member the new code calls was read with `tools/api-surface --members`
on all eight releases on 2026-10-29 - one signature each, 2020 to 2027 - and it compiles for 2020 to 2027
(`tools/check-fragments-compile.py`, the same day, in a cloud session with no Revit). The declared sentence
*"how many different panel sizes are on this curtain wall"* reaches it by identity on the container's backend.
**Nothing has met a Revit.** The card went from PROVEN to DRAFT with this change; version 1's proof is kept on
it as a record and is stale. It is a READ, so `revit_read` still runs it as a DRAFT - this group is what makes
its answer trustworthy, not what lets it run.

**What only a Revit can settle**, each written on the card as an ASSUMPTION:

- **That a curtain panel carries the two values at all**, as the Properties palette shows them (JO1).
- **Whether they are the CELL's size or the glass inside the mullions** (JO6). The rig has no mullions, so
  JO1 to JO5 hold either way.
- **Whether a curtain wall door and a basic wall in a cell carry them** - sized if they do, named in
  `sizesNotRead` if they do not (JO7). Never worked out from the grid either way.

**The rig (JO0) - a scratch project, never a working one.** A new project from Revit 2020's (or 2024's) default
metric template, saved as `%TEMP%\heron-work\group-jo\Heron JO Panel Sizes.rvt`, made by hand - CREATE_WALL is
DRAFT (version 3, Group JG), so no Heron tool draws the walls. The job file
[`tools/jobs/report-curtain-elements-jo-2026-10-29.yaml`](../../tools/jobs/report-curtain-elements-jo-2026-10-29.yaml)
runs JO1 and JO2 (`--dry-run` reads it back and sends nothing); it picks its walls by **Mark**, through
SELECT_BY_PARAMETER_VALUE and SET_SELECTION, both PROVEN. Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JO0** | *The rig, by hand.* Duplicate *Curtain Wall: Curtain Wall* as **HERON JO 1500**: Vertical Grid Layout *Fixed Distance*, Spacing *1500*, Justification *Beginning*; Horizontal Grid Layout *None*; every mullion *None*. On Level 1, Unconnected Height 3000, Mark as named: **JO-MIXED**, HERON JO 1500, 0,60000 to 10000,60000; **JO-ONE**, HERON JO 1500, 0,65000 to 9000,65000; **JO-TOL**, *Curtain Wall: Curtain Wall*, 0,70000 to 6003,70000, with ONE vertical grid line added by hand 3000 mm from its start (type the temporary dimension); **JO-OTHER**, *Curtain Wall: Curtain Wall*, 0,72000 to 4000,72000, no grid line; **JO-BASIC**, *Basic Wall: Generic - 200mm*, 0,75000 to 10000,75000. Save | Five walls, each Mark once. JO-MIXED shows six panels 1500 wide and a seventh 1000 wide at its end; JO-TOL two panels. If the template's basic wall is not *Generic - 200mm*, any basic wall will do - its name goes nowhere |
| **JO1** | *The positive - the D-30 positive.* `python tools/batch-prove.py tools/jobs/report-curtain-elements-jo-2026-10-29.yaml --session <pid>`, its positive leg: JO-MIXED, tolerance left out | Binding note: `elements` from the selection (1). Findings, in order: *"1 curtain wall(s): 7 panel cell(s), 0 mullion(s), 0 empty cell(s). 0 of what was handed in was not a curtain wall"*, *"'HERON JO 1500' (id ...):"*, *"7 curtain panel(s)"*, *"no mullions - the grid lines carry none"*, the grid line - **version 1's lines, word for word** - then *"panel sizes, width x height within 5 mm: 2 - 1 at 1000 x 3000 mm, 6 at 1500 x 3000 mm"* and last *"Across the 1 curtain wall(s): 2 different panel size(s) ..."*. **`differentPanelSizes` 2**, `sizesNotRead` empty. **FAIL:** one size, a width other than 1000 and 1500, or a version 1 line changed |
| **JO2** | *The negative - the D-30 negative.* JO1's negative leg: JO-BASIC, the same request | `curtainWallsFound` 0, **`differentPanelSizes` 0**, `panelSizes` and `sizesNotRead` empty, `notCurtain` *"... is a wall but not a CURTAIN wall"*, and findings the one summary line - no size line, the answer version 1 gave |
| **JO3** | *All one size.* JO-ONE selected: `python mcp/client/heron_bridge_client.py fragment --in "Heron JO Panel Sizes" report-curtain-elements` | **Exactly one size**: `differentPanelSizes` 1, `panelSizes` *"6 at 1500 x 3000 mm"*. Two sizes is a panel measured or grouped wrong - **FAIL** |
| **JO4** | *Two walls, and one left out.* JO-MIXED and JO-ONE selected together, JO-OTHER and JO-TOL not; the same line | Each wall's own size line - 2 sizes, then 1 - and across them `differentPanelSizes` 2, `panelSizes` *"1 at 1000 x 3000 mm"*, *"12 at 1500 x 3000 mm"*: a shared size is one size. **FAIL:** a 4000 mm or a 3000/3003 mm size - a wall not handed in was counted |
| **JO5** | *The tolerance.* JO-TOL selected: the same line, then with `--set toleranceMm=2`, `--set toleranceMm=0`, `--set toleranceMm=-1` | Left out: 1 size, *"2 at 3000-3003 x 3000 mm"*. 2 and 0: 2 sizes, *"1 at 3000 x 3000 mm"*, *"1 at 3003 x 3000 mm"*. -1: no size line, `sizesNotRead` *"a tolerance of -1 mm is not a size - give 0 mm or more; no panel was sized"*, and version 1's lines all still there. RECORD the widths as printed - a line placed at 3000.4 prints 3000.4 |
| **JO6** | *ASSUMPTION - with mullions.* Duplicate HERON JO 1500 as **HERON JO MULLIONS**, every mullion a rectangular one the template has; draw a 9000 mm wall of it, Mark JO-MULL; select it; the same line | RECORD what Width reads for an inner panel - **1500**, the cell, or less, the glass inside the mullions - and put the answer on the card's `panelSizes` comment. Not a FAIL either way; the card says it is not known |
| **JO7** | *ASSUMPTION - a door and a basic wall in cells.* On JO-ONE: one panel changed to a curtain wall door (load *Door-Curtain-Wall-Single-Glass* or whichever the library has; write the one used), one to *Basic Wall: Generic - 200mm*; select the wall; the same line; then Ctrl+Z both | For each, RECORD which: in a size with *"(1 of them a door)"* / *"(1 of them a basic wall)"* after it, or named in `sizesNotRead` *"Revit gives it no panel Width or Height"*. Version 1's *"1 cell(s) filled with a BASIC WALL"* line still there. **FAIL:** a size for either that Revit's own Properties do not show |
| **JO8** | *An empty cell.* On JO-MIXED, unpin and delete one 1500 panel; select the wall; the same line; then Ctrl+Z | `emptyCells` 1 and version 1's *"1 EMPTY cell(s)"* line; the size line ends *"1 empty cell(s) in no size - there is no panel there to measure"* and counts *"5 at 1500 x 3000 mm"*. If Revit will not delete the panel, RECORD that and the row closes |
| **JO9** | *The second route, by hand.* A Curtain Panel schedule: fields Width, Height, Count; sorted and grouped by Width then Height; *Itemize every instance* off; filtered by the host wall's Mark if the template offers it, else read on JO-MIXED's panels | One row per size, the same counts as JO1 and JO4 at tolerance 0 |
| **JO10** | *Routing, on the PC's backend.* `heron_lookup` for *"how many different panel sizes are on this curtain wall"*, *"how many different panel sizes does this facade have"*, *"group the curtain panels by size"* | The first reaches REPORT_CURTAIN_ELEMENTS - declared there. The other two are not declared; **record** where they land. On the container's backend on 2026-10-29 the second reached it by ranking, 7 ranks clear, and the third was **a coin toss** with SET_CURTAIN_PANEL_TYPE, a write, second - 0.4 of a rank apart while this card was PROVEN, 0.0 once version 2 made it DRAFT, still first by words. A write answering it here is a finding for the card's routing table, not a reason to declare words the modeller has not said |
| **JO11** | *Signing, on the owner's word only, after JO1 to JO5.* `python brain/heron_validate.py draft report-curtain-elements --from brain/proof-drafts/runs/report-curtain-elements.json`, read it against JO1 and JO2, then `python brain/heron_validate.py accept report-curtain-elements --by "Ajmal PS"` | The proof block written. **`heron-status: PROVEN` is a separate edit, the owner's**, in the same change; `--release` for the other release once it is run |

A row that fails stays open; nothing in a row changes to make it pass.

**What this group does not settle.** A curtain **system**'s panels: the tool reads a curtain wall, and a curtain
system handed in is *"not a wall"* - the [`facade`](../../brain/skills/facade.yaml) method's route B step 4 says
to run it on the system, which version 1 does not do either (recorded, not changed here). The glass area by
orientation is [doc 45 §6](../45-five-more-downloaded-skill-files.md)'s, not this.
