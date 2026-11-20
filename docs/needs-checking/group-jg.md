# Needs checking — Group JG

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JG - a wall that starts above its level: `CREATE_WALL` version 3, `baseOffset` (2026-10-28)

**Tracked as GitHub issue [#993](https://github.com/Ajmalpshaik/Heron-AI/issues/993) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

From [row 5b-1345](../fragment-issues/section-5b-rows-1326-1350.md). On 2026-10-28 a 40-floor tower was built
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
| ~~**JG0**~~ | Open. The scratch project open and in front in Revit 2020, saved with nothing unsaved — [full row](../needs-checking-archive/group-jg.md#row-jg0) | The title bar reads JG Wall Offset Struck 2026-11-13: PASSED - the verdict is in this… |
| ~~**JG1**~~ | The offset left out, and 0 - version 2 unchanged, rolled back. — [full row](../needs-checking-archive/group-jg.md#row-jg1) | Both: `created 1`, `baseOffset 0`, `offsetsReadMm` holding one 0 - the read back off the… |
| ~~**JG2**~~ | The positive - the D-30 positive - KEPT. Save (Ctrl+S). — [full row](../needs-checking-archive/group-jg.md#row-jg2) | `KEPT`; findings "1 wall(s) built..." then "Base Offset +1500 mm from 'Level 1', so each… |
| ~~**JG3**~~ | *Measured by two PROVEN readers, on the kept wall.* With the JG2 wall selected in Revit (click it): `python mcp/client/heron_bridge_client.py fragment --in "JG Wall Offset" read-element-level`, then `fragment --in "JG Wall Offset" report-bounding-box` | READ_ELEMENT_LEVEL: `levelName` Level 1, **`offsetMm` 1500**. REPORT_BOUNDING_BOX: the wall's line ends *"... x 3000 mm, lowest corner at (..., ..., 1500) mm"* - **3000 tall, Z from 1500 to 4500**, the height measured from the base. **FAIL:** a lowest corner at Z 0, or a box 4500 tall **HELD 2026-11-20 on Revit 2020 (build 20191106_1200, add-in 0.2.0.0),** not *JG Wall Offset* (not on this PC any more) but *House Scratch 2* - a copy of `rac_basic_sample_project.rvt` in `%TEMP%\heron-work\issues-1120`, for issue [#993](https://github.com/Ajmalpshaik/Heron-AI/issues/993): JG2's wall made again with `CREATE_WALL level=Level 1 points=0,80000,0; 6000,80000,0 height=3000 baseOffset=1500`, type *Interior - Partition* (the sample has no *Generic - 200mm*), kept on the scratch copy - *"Base Offset +1500 mm from 'Level 1', so each wall stands from +1500 to +4500 mm ... Read back off the walls: 1 of 1 as asked"*. Selected with `SELECT_IN_REGION` (1 wall, id 1101805): `READ_ELEMENT_LEVEL` *levelName {1101805: Level 1}, offsetMm {1101805: 1500}*; `REPORT_BOUNDING_BOX` *"Interior - Partition (id 1101805): 6000 x 120 x 3000 mm, lowest corner at (0, 79940, 1500) mm"* - 3000 tall, Z 1500 to 4500. |
| ~~**JG4**~~ | The negative - the D-30 negative - rolled back. python mcp/client/heron_bridge_client.py validate… — [full row](../needs-checking-archive/group-jg.md#row-jg4) | Only the negative runs, rolled back: `created` empty, `refused 1`, findings "A base… |
| ~~**JG5**~~ | Signing. python brain/heron_validate.py draft create-wall --from… — [full row](../needs-checking-archive/group-jg.md#row-jg5) | The proof block written - the positive the kept run (D-120), the negative JG4's. |
| **JG6** | *Then Undo.* Ctrl+Z once in Revit, or leave the scratch project unsaved | The JG2 wall gone. Nothing of the owner's was ever touched **2026-11-20:** JG3's wall is in *House Scratch 2*, which is never saved; it goes when that copy is closed without saving. Strike once it is closed. |

**The job file** [`tools/jobs/create-wall-base-offset-2026-10-28.yaml`](../../tools/jobs/create-wall-base-offset-2026-10-28.yaml)
runs JG2's wall and JG4's negative, both rolled back (`--dry-run` reads it back and sends nothing). It judges
only `created` against empty: the read-back line, JG1 and JG3 are read by hand. **Run it only after JG5**: it
writes `brain/proof-drafts/runs/create-wall.json`, the record JG5 signs from, and would replace the kept run.

### Results - Revit 2020, 2026-11-01

Run in the owner's scratch *TEST MASS* (his empty *Project1*, Revit 2020's default metric template, saved to
`Desktop\Heron Test\TEST MASS.rvt` - the rig's name differs from JG0's, nothing else), session 6588, with
`HERON_CLIENT_ID=ajmal-pc`. Code run: fingerprint `cb379fa16bc2aee6`, version 3.

| # | Result | What it read |
|---|---|---|
| JG0 | PASS | *TEST MASS* in front, saved, 2 levels with nothing on them |
| JG1 | PASS | The offset left out, then `baseOffset=0`: both `created 1`, `baseOffset 0`, `offsetsReadMm` [0], ONE finding (*"1 wall(s) built on 'Level 1' at 3000 mm high, unconnected to..."*), no Base Offset line; rolled back |
| JG2 | PASS, kept | `try-once`, `baseOffset=1500`: `KEPT`, `created 1`, `offsetsReadMm` [1500], findings *"1 wall(s) built..."* and *"Base Offset +1500 mm from 'Level 1', so each wall stands fro..."*; the owner pressed Keep. Properties was not read by hand |
| JG3 | PASS, one of two | `SELECT_IN_REGION` (Walls, X -500..6500, Y 79000..81000) found the one wall; `REPORT_BOUNDING_BOX`: *Generic - 200mm (id 345445): 6000 x 200 x 3000 mm*, `minZ` 4.92126 ft (1500 mm), `maxZ` 14.76378 ft (4500 mm). `READ_ELEMENT_LEVEL` ran but the command line printed its two answers only as *"1 entry(ies)"*, so its `offsetMm` was not read |
| JG4 | PASS | `validate --write --kept`, `baseOffset=200000000`: `created` empty, `refused 1`, finding *"A base offset is millimetres above the level - 1500, or -300..."*; rolled back |
| JG5 | PASS | `draft` then `accept create-wall --by "Ajmal PS"`, run in his own terminal on his word *"Yes, I sign it"*; card set PROVEN in the same change |
| JG6 | NOT YET | The kept wall (id 345445) is still in *TEST MASS*, to be undone before the model is used for anything else |

**What this group does not settle.** Whether the crown posts of the 2026-10-28 tower are best drawn as walls
at all is the modeller's. A wall whose top is tied to the level above (SET_WALL_CONSTRAINTS) after it was
drawn with a base offset is not run here. Revit 2024 and 2027 are owed as `proof.releases` entries
([D-131](../decisions/D-131.md)) once 2020 is signed.
