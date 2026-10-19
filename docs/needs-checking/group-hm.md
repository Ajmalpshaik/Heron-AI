# Needs checking — Group HM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HM - a wall and a slab edge along an arc: CREATE_WALL and CREATE_FLOOR version 2 (2026-10-19)

From [row 5b-1216](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-19 an oval twisting tower
built for the owner in Revit 2020 had to take a 20-sided polygon for its glass, because no Heron tool drew
an arc wall or an arc slab edge.

**What changed.** [`create-wall`](../../brain/fragments/create-wall/fragment.yaml) and
[`create-floor`](../../brain/fragments/create-floor/fragment.yaml), version 2: `points` and `boundary` are
read as text in the fragment, and `arc MX,MY X,Y` after a point makes the next wall, or the next edge, an
arc through MX,MY ending at X,Y - the writing `CREATE_FAMILY_EXTRUSION`'s loops take. A full circle is two
arcs. Text with no `arc` in it is read exactly as the add-in read it for version 1. An arc whose three
points are in a line, or two in one place, makes nothing.

**What is tested and not run.** Both compile for Revit 2020 to 2027 (`tools/check-fragments-compile.py`),
and `Arc.Create(XYZ, XYZ, XYZ)` is on every release (`tools/api-surface`). The reading of the text was run
without Revit in a scratch harness: the add-in's own `OnePoint` and `ManyPoints`, copied verbatim, against
the two fragments' reading code, on 200,000 random texts with no arc - the same points to the bit or the
same refusal in the same words - and the arc cases below; it went red on a copy that divided by 304.8 a
different way. **Nothing here has run in Revit.**

**UNTIL THIS GROUP IS RUN AND SIGNED, BOTH TOOLS ARE DRAFT.** Their version 1 proofs no longer describe
their code (D-30), so a change kept from either is refused ([D-117](../decisions/D-117.md)): a chat cannot
draw a wall or a slab, and neither can `--write --apply` - including every open group that keeps one as an
arrangement step (`grep -li -e "create.wall" -e "create.floor" docs/needs-checking/group-*.md`).
What still runs: each tool's one `try-once` ([D-119](../decisions/D-119.md)), and any rolled-back run.

**Needs.** The add-in as it is - nothing in `revit/` changed, so no rebuild. Changes on. A **saved test
copy** of `test projject` (Revit 2024) with `Curtain Wall: Storefront`, `Curtain Wall: Curtain Wall 1` and
a floor type - `Generic 300mm` was used in Project1; if the copy has none by that name, put the one it has
in every command and in the job file. For HM6, the same on a Revit 2020 or 2021 copy. Run in Git Bash from
the Heron folder, the copy open and in front. **Order: HM1, then HM2 to HM5, then HM6 and HM7.**

| # | Run | Look for |
|---|---|---|
| **HM1** | *Version 1 unchanged, rolled back.* `python mcp/client/heron_bridge_client.py fragment --in "test projject" create-wall --write --set "level=Level 1" --set "wallType=Generic - 200mm" --set "points=0,0,0; 6000,0,0; 6000,4000,0" --set height=3000 --set structural=false < /dev/null`, then the same with `points=0,60000,0; 0,60000,0`; then `fragment --in "test projject" create-floor --write --set "level=Level 1" --set "floorType=Generic 300mm" --set "boundary=0,0,0; 6000,0,0; 6000,4000,0; 0,4000,0"`, and again with `boundary=0,0,0; 6000,0,0` | Word for word what version 1's proofs recorded: `created` 2 and *"2 wall(s) built on 'Level 1' at 3000 mm high, unconnected to the level above. 0 refused, 0 skipped as too short..."*; then `created` 0, `tooShort` 1. The floor: an id and *"Floor created on 'Level 1' using the Revit 2022 and later call..."*; then *"A floor needs at least three boundary points and 2 were given"*. No *"arc"* line in any of them. **FAIL:** any other word, or a point read in other units |
| **HM2** | *An arc curtain wall, KEPT and measured.* Save the copy (Ctrl+S). `python mcp/client/heron_bridge_client.py try-once --in "test projject" create-wall --set "level=Level 1" --set "wallType=Curtain Wall: Storefront" --set "points=0,60000,0; arc 5000,65000 10000,60000" --set height=3000 --set structural=false < /dev/null` - Revit shows the wall and asks Keep or Discard; press **Keep** | The client prints `KEPT`; findings *"1 wall(s) built..."* and *"1 of them follow an arc"*. In Revit, select the wall: **Length in Properties 15708 mm** (pi x 5000, to the millimetre) - **not 10000**, the chord. In plan, a half circle bulging north to y = 65000. ONE undo entry. **Record, not judged:** how Storefront's panels and mullions sit on the arc - flat between grid lines, or curved - in a 3D view |
| **HM3** | *A round slab, KEPT and measured.* Save, then `python mcp/client/heron_bridge_client.py try-once --in "test projject" create-floor --set "level=Level 1" --set "floorType=Generic 300mm" --set "boundary=5000,60000,0; arc 0,65000 -5000,60000; arc 0,55000 5000,60000" < /dev/null`; press **Keep** | `KEPT`; findings *"Floor created on 'Level 1' using the Revit 2022 and later call..."* and *"2 of its 2 edges are arcs"*. Select the slab: **Area in Properties 78.54 m2** (pi x 25) - **not 77.25**, the 20-sided polygon. In plan, a circle of radius 5000 mm round 0,60000, with no straight edge. ONE undo entry |
| **HM4** | *The negative: a bad arc refused, rolled back.* `python mcp/client/heron_bridge_client.py validate create-wall --write --kept --negative-set "level=Level 1" --negative-set "wallType=Curtain Wall: Storefront" --negative-set "points=0,60000,0; arc 5000,60000 10000,60000" --negative-set height=3000 --negative-set structural=false < /dev/null`; then `validate create-floor --write --kept` with `--negative-set "boundary=5000,60000,0; arc 0,60000 -5000,60000; arc 0,55000 5000,60000"` and its level and floor type restated | Only the negative runs, rolled back. The wall: `created` empty, `refused` 1, findings *"The arc from 0,60000 through 5000,60000 to 10000,60000 is not an arc - its three points are in a line, or two are in the same place. Nothing was built."* The floor: no id, findings naming the arc from 5000,60000 through 0,60000 to -5000,60000. Nothing new in the model. **FAIL:** anything built, or Revit's own words instead of these |
| **HM5** | *Signing.* For each: `python brain/heron_validate.py draft <fragment> --from brain/proof-drafts/runs/<fragment>.json`, read the draft against HM2 to HM4, then `python brain/heron_validate.py accept <fragment> --by "Ajmal PS"` | Both proof blocks written - the positive the kept run, the negative HM4's. **`heron-status: PROVEN` is a separate edit, the owner's**; until it is made, D-117 still refuses both |
| **HM6** | *Revit 2020 or 2021 - the pre-2022 floor call.* After HM5, on a saved test copy in that release: `python tools/batch-prove.py tools/jobs/create-wall-floor-arcs-test-projject-2026-10-19.yaml --dry-run`, then without it (the `in:` line names that copy) | Both positives `created`; the floor's findings say *"pre-2022 call"* - `NewFloor` took the arcs in its `CurveArray`. Both negatives refused as in HM4. A `proof.releases` entry for that release ([D-131](../decisions/D-131.md)), the owner's to accept. **FAIL:** a missing-method error, or the round slab made as a polygon |
| **HM7** | *In a chat, after HM5 and the owner's PROVEN edit.* *"Draw a curved curtain wall of Curtain Wall 1 on Level 1 from 0,70000 through 5000,75000 to 10000,70000, 3000 high"* - Curtain Wall 1 has no grid | It reaches `CREATE_WALL` with an `arc` in the points, and a curtain wall is drawn. **Record, not judged:** what Revit shows for an arc curtain wall with no vertical grid - a curved panel, one flat panel across the chord, or a warning. Then the curtain-wall method's step 4 on it, a vertical grid every 1500 mm, and say how the panels follow the arc after |

### Run 2026-10-19, Revit 2020 (laptop, session 15072) - PASS, signed, both PROVEN

Run for the owner's Kerala house build, which needed walls and slabs the same afternoon. **On Revit 2020,
not 2024**: the test models were two copies of his saved, empty `Kerala house design.rvt` (Revit 2020
metric template) in `%TEMP%\heron-work\kerala-hm-proof` - `HM wall test` and `HM floor test` - opened with
`fragment activate-document` (read path). The same types as the job file existed by name.

| # | Result |
|---|---|
| **HM1** | **PASS on 2020.** Wall: `created 2 item(s)`, *"2 wall(s) built on 'Level 1' at 3000 mm high, unconnected to..."*, `refused 0`; the too-short run `tooShort 1`, nothing made. Floor: an id and *"Floor created on 'Level 1' using the pre-2022 call..."*; two points *"A floor needs at least three boundary points and 2 were give..."*. `arcs 0` in all four. All rolled back |
| **HM2** | **PASS on 2020.** A rolled-back rehearsal first (1 Storefront, 91 elements added and removed). Then `try-once` on `HM wall test`: the owner pressed **Keep**; `KEPT`, `created 1 item(s) [Storefront]`, findings *"1 wall(s) built..."* and *"1 of them follow an arc..."*. Measured by `filter-elements-by-category` (Walls, Level 1: 1 match) then `measure-element-lengths`: **`totalMm 15707.963267949`** = pi x 5000, not the 10000 mm chord. How Storefront's panels sit on the arc was **not looked at** - no view was opened |
| **HM3** | **PASS on 2020, through the pre-2022 call** (so this is HM6's floor half as well). Rehearsal rolled back, then `try-once` on `HM floor test`, **Keep**: `KEPT`, `created 1 item(s) [id 345453]`, *"Floor created on 'Level 1' using the pre-2022 call..."* and *"2 of its 2 edges are arcs..."*. Measured by `select-by-numeric-parameter` Area, Floors: **between 845.0 and 845.8 ft2 (78.50 to 78.58 m2) - 1 of 1**; between 831 and 832 ft2 (the 77.25 m2 polygon) - **0 of 1** |
| **HM4** | **PASS.** Wall: `created 0 item(s)`, `refused 1`, *"The arc from 0,60000 through 5000,60000 to 10000,60000 is not an arc - its three points are in a line, or two are in the same place."* Floor: `created (null)`, the arc from 5000,60000 through 0,60000 to -5000,60000 named the same way. Both rolled back. **The commands as printed above do not run**: `validate create-wall --write --kept --negative-set ...` prints the usage (*"Which fragment?"*), because `validate` takes its value options only BEFORE the fragment's name. What ran: `validate --in "HM wall test" --write --kept --negative-set ... create-wall` |
| **HM5** | **Signed.** `heron_validate.py draft` from each `runs/<fragment>.json` (positive = the kept run, D-120; negative = HM4), read for contrast - positive `created 1`, negative `created 0` / `(null)` with the refusal - then `accept --by "Ajmal PS"` and the `heron-status: PROVEN` edit, both from one script the owner's terminal ran. Fingerprints `65352cd44f07b61e` (wall) and `afec7d7a9549e170` (floor); `check-signatures` no longer lists either as STALE |
| **HM6** | Covered on 2020 by HM2 to HM4 above - the proof's own release is 2020. The job file was not run |
| **Owed** | **The 2022-and-later floor call (`Floor.Create`, CurveLoop) has not run with an arc** - a `proof.releases` entry for 2024 (HM2 to HM4 on `test projject`, D-131). HM7 not run. Storefront's panels on the arc not looked at. No second route was written into either proof; the two measurements above were taken by PROVEN readers and are recorded only here |
