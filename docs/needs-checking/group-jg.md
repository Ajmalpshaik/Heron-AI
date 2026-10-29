# Needs checking — Group JG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JG - a wall that starts above its level: `CREATE_WALL` version 3, `baseOffset` (2026-10-28)

From [row 5b-1345](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-28 a 40-floor tower was built
in Revit 2020 through Heron, and its crown posts, which should have started part-way up a storey, had to be
moved sideways instead: `CREATE_WALL` handed Revit a Base Offset of 0 every time and had no input for one.

**What changed.** [`create-wall`](../../brain/fragments/create-wall/fragment.yaml) version 3 takes
`baseOffset`, Revit's Base Offset in millimetres - negative starts below the level - optional and 0 when
left out. [`impl/any/fragment.cs`](../../brain/fragments/create-wall/impl/any/fragment.cs) hands it to
`Wall.Create`'s own `offset` argument, which versions 1 and 2 filled with 0, and never to the curve, which
stays at the level's project elevation. The height is measured from that base, as Revit measures
Unconnected Height. Each wall's Base Offset is read back off the wall (`WALL_BASE_OFFSET`) into
`offsetsReadMm`, and said in the findings whenever it is not 0 or not as asked; with 0 every finding is
version 2's word for word. A base offset that is not a number, or is past Revit's own limit of 30000 feet
either way, refuses the whole run.

**What is done and what is not.** It compiles for all eight releases
(`tools/check-fragments-compile.py`, 2026-10-28). Revit's API documentation for 2020 and 2027 (the
`RevitAPI.xml` in the reference packages the compile uses) says `Wall.Create`'s `offset` *"Modifies the
wall's Base Offset parameter to determine its vertical placement"* and must be *"no more than 30000 feet in
absolute value"*. **Nothing here has run in Revit**: that the wall then stands where its documentation says
is what JG2 and JG3 measure.

**UNTIL THIS GROUP IS RUN AND SIGNED, CREATE_WALL IS DRAFT.** Version 2's proof no longer describes the code
(D-30), so a change kept from it is refused ([D-117](../decisions/D-117.md)): a chat cannot draw a wall, and
neither can `--write --apply` - including every open group that keeps a wall as an arrangement step
(`grep -li "create.wall" docs/needs-checking/group-*.md`). What still runs: its one `try-once`
([D-119](../decisions/D-119.md)) - JG2 spends it - and any rolled-back run.

**The model - scratch, never a working project.** A new project from Revit 2020's default metric template,
saved as `%TEMP%\heron-work\group-jg\JG Wall Offset.rvt`, nothing else in it. Level 1 must read elevation 0
in an elevation view; if it does not, add its elevation to every Z below. The wall type is
`Generic - 200mm`; if the template has none by that name, put the one it has in every command and in the
job file. **Needs:** the add-in as it is - nothing in `revit/` changed, so no rebuild - and Changes on.
Every line below in Git Bash from the Heron folder, with `HERON_CLIENT_ID=ajmal-pc`, `--session <process
id>` when two Revits are connected, and `< /dev/null`. **Order: JG0, JG1, JG2, JG3, then JG4 to JG6.**

| # | Run | Look for |
|---|---|---|
| **JG0** | *Open.* The scratch project open and in front in Revit 2020, saved with nothing unsaved | The title bar reads *JG Wall Offset* |
| **JG1** | *The offset left out, and 0 - version 2 unchanged, rolled back. Half of the D-30 negative.* `python mcp/client/heron_bridge_client.py fragment --in "JG Wall Offset" create-wall --write --set "level=Level 1" --set "wallType=Generic - 200mm" --set "points=0,80000,0; 6000,80000,0" --set height=3000 --set structural=false`, then the same with `--set baseOffset=0` added | Both: `created 1`, `baseOffset 0`, `offsetsReadMm` holding one 0 - the read back off the wall, not the input - and findings exactly *"1 wall(s) built on 'Level 1' at 3000 mm high, unconnected to the level above. 0 refused, 0 skipped as too short. NOTHING has been joined..."* and nothing more: **no "Base Offset" line**. Rolled back. **FAIL:** a Base Offset line, an `offsetsReadMm` other than 0, or any word unlike HM1's |
| **JG2** | *The positive - the D-30 positive - KEPT.* Save (Ctrl+S). `python mcp/client/heron_bridge_client.py try-once --in "JG Wall Offset" create-wall --set "level=Level 1" --set "wallType=Generic - 200mm" --set "points=0,80000,0; 6000,80000,0" --set height=3000 --set structural=false --set baseOffset=1500` - Revit shows the wall and asks Keep or Discard; press **Keep** | `KEPT`; findings *"1 wall(s) built..."* then *"Base Offset +1500 mm from 'Level 1', so each wall stands from +1500 to +4500 mm, its 3000 mm height measured from its base. Read back off the walls: 1 of 1 as asked"*. Select the wall: Properties reads **Base Constraint Level 1, Base Offset 1500, Unconnected Height 3000** - the second route, by hand. ONE undo entry. **FAIL:** Base Offset 0 with the wall drawn up there anyway, 3000 (the offset counted twice), or an Unconnected Height other than 3000 |
| **JG3** | *Measured by two PROVEN readers, on the kept wall.* With the JG2 wall selected in Revit (click it): `python mcp/client/heron_bridge_client.py fragment --in "JG Wall Offset" read-element-level`, then `fragment --in "JG Wall Offset" report-bounding-box` | READ_ELEMENT_LEVEL: `levelName` Level 1, **`offsetMm` 1500**. REPORT_BOUNDING_BOX: the wall's line ends *"... x 3000 mm, lowest corner at (..., ..., 1500) mm"* - **3000 tall, Z from 1500 to 4500**, the height measured from the base. **FAIL:** a lowest corner at Z 0, or a box 4500 tall |
| **JG4** | *The negative - the D-30 negative - rolled back.* `python mcp/client/heron_bridge_client.py validate --in "JG Wall Offset" --write --kept --negative-set "level=Level 1" --negative-set "wallType=Generic - 200mm" --negative-set "points=0,90000,0; 6000,90000,0" --negative-set height=3000 --negative-set structural=false --negative-set baseOffset=200000000 create-wall` (the value options go BEFORE the fragment's name - HM4 found that) | Only the negative runs, rolled back: `created` empty, `refused 1`, findings *"A base offset is millimetres above the level - 1500, or -300 below it - and Revit takes none past 9144000 (30000 feet) either way. 200000000 is not one Heron can start a wall at. Nothing was built."* Nothing new in the model. **FAIL:** a wall built, or Revit's own words instead of these |
| **JG5** | *Signing.* `python brain/heron_validate.py draft create-wall --from brain/proof-drafts/runs/create-wall.json`, read the draft against JG2 to JG4, then `python brain/heron_validate.py accept create-wall --by "Ajmal PS"` | The proof block written - the positive the kept run (D-120), the negative JG4's. **`heron-status: PROVEN` is a separate edit, the owner's**; until it is made, D-117 still refuses CREATE_WALL's kept changes |
| **JG6** | *Then Undo.* Ctrl+Z once in Revit, or leave the scratch project unsaved | The JG2 wall gone. Nothing of the owner's was ever touched |

**The job file** [`tools/jobs/create-wall-base-offset-2026-10-28.yaml`](../../tools/jobs/create-wall-base-offset-2026-10-28.yaml)
runs JG2's wall and JG4's negative, both rolled back (`--dry-run` reads it back and sends nothing). It judges
only `created` against empty: the read-back line, JG1 and JG3 are read by hand. **Run it only after JG5**: it
writes `brain/proof-drafts/runs/create-wall.json`, the record JG5 signs from, and would replace the kept run.

**What this group does not settle.** Whether the crown posts of the 2026-10-28 tower are best drawn as walls
at all is the modeller's. A wall whose top is tied to the level above (SET_WALL_CONSTRAINTS) after it was
drawn with a base offset is not run here. Revit 2024 and 2027 are owed as `proof.releases` entries
([D-131](../decisions/D-131.md)) once 2020 is signed.
