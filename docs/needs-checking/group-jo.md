# Needs checking — Group JO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JO - a massing study from block to building: `REPORT_MASSES`, `SET_MASS_FLOOR_USAGE`, `REMOVE_MASS_FLOORS`, `CREATE_WALLS_BY_FACE` and the `massing-study` and `mass-family-creation` methods (2026-10-28)

Asked for by the owner on 2026-10-28: Heron's massing tools brought up to what a massing study needs. Before
this, Heron could build forms in a mass family and add mass floors in a project
([`CREATE_MASS_FLOORS`](../../brain/fragments/create-mass-floors/fragment.yaml)), and nothing else - no tool
read a mass's gross floor area, set a mass floor's Usage, took a mass floor off, or made anything by face, and
[docs/43 §11](../43-building-revit-families.md) listed the conceptual mass as a kind *"with no skill of its own"*.

| What | Where | What it does |
|---|---|---|
| `REPORT_MASSES` (READ) | [`brain/fragments/report-masses`](../../brain/fragments/report-masses/fragment.yaml) | every mass's Gross Volume, Gross Surface Area, Gross Floor Area, base, top and height; its mass floors level by level with area, perimeter, exterior surface, volume and Usage; totals by level and by Usage; plot ratio and site coverage from a site area or the property lines; unjoined overlaps measured on the solids; each mass floor's outline in `CREATE_FLOOR`'s writing |
| `SET_MASS_FLOOR_USAGE` (MODIFY) | [`brain/fragments/set-mass-floor-usage`](../../brain/fragments/set-mass-floor-usage/fragment.yaml) | each mass floor's Usage, level by level - `Level 1=Retail; Level 2..Level 6=Office` |
| `REMOVE_MASS_FLOORS` (MODIFY) | [`brain/fragments/remove-mass-floors`](../../brain/fragments/remove-mass-floors/fragment.yaml) | mass floors taken off at the levels named; an empty value refuses |
| `CREATE_WALLS_BY_FACE` (MODIFY) | [`brain/fragments/create-walls-by-face`](../../brain/fragments/create-walls-by-face/fragment.yaml) | Wall by Face with a wall type; a curtain system type is sent to `CREATE_CURTAIN_SYSTEM` ([Group JM](group-jm.md)), a floor or roof type refused by name |
| `massing-study` | [`brain/skills/massing-study.yaml`](../../brain/skills/massing-study.yaml) | the method: levels, masses, mass floors, usage, joins, the numbers, a schedule, then the building on the faces |
| `mass-family-creation` | [`brain/skills/mass-family-creation.yaml`](../../brain/skills/mass-family-creation.yaml) | the method for a mass family: points, reference lines, an extrusion, a loft or a revolve, voids cut |

**What the API does not have, read off the reference assemblies of all eight releases on 2026-10-28 with
`tools/api-surface`:** no face-floor or face-roof class and no creation call for either, so **Floor by Face and
Roof by Face are by hand** or, for a slab, `REPORT_MASSES`' outline through `CREATE_FLOOR` - which does not
follow the mass; and no call makes an **in-place** family, so a mass Heron builds is a loaded one.
`MassLevelData` is marked obsolete on 2023 alone and not on 2024 to 2027; it compiles on all eight.

**What is done and what is not.** All four compile for all eight releases (and again after `CREATE_ELEMENTS_BY_FACE` was narrowed to walls as `CREATE_WALLS_BY_FACE` on 2026-10-29, when `CREATE_CURTAIN_SYSTEM` reached main first) (`tools/check-fragments-compile.py`,
2026-10-28) and the two methods validate (`brain/heron_skill.py`). **Nothing here has run in Revit.** Every
card is DRAFT.

**The model - scratch, never a working project.** A new project from the Metric architectural template in
Revit 2020, saved as `%TEMP%\heron-work\group-jo\JO Massing.rvt`. The job file is
[`tools/jobs/massing-jo-2026-10-28.yaml`](../../tools/jobs/massing-jo-2026-10-28.yaml) (`--dry-run` reads it
back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two
Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JO0** | *Open.* The scratch project open and in front in Revit 2020 | The title bar reads *JO Massing* |
| **JO1** | *Arrange, by hand, kept in the copy.* Levels 1 to 4 at 0, 3000, 6000 and 9000 mm. Massing & Site > In-Place Mass *A*: a 30000 x 20000 mm rectangle from 0,0 on Level 1, Create Form, 12000 mm high. In-Place Mass *B*: 10000 x 10000 mm from 25000,5000, 6000 mm high - half of it inside A, **not joined**. Select A, Mass Floors, tick Levels 1 to 4. One Generic - 200mm wall away from both | A's Properties: Gross Floor Area 2400 m2; B's 0 m2 |
| **JO2** | *`REPORT_MASSES`, the D-30 positive.* The job file's `report-masses` leg: `siteArea=4500`, `withOutlines=true` | `massCount` 2; A 2400 m2 over four floors of 600 m2; the finding *"Masses A and B overlap by ..."* with about 300 m3 (5 x 10 x 6); plot ratio 0.53 on 4500 m2; four outlines, each four corners 30000 x 20000 mm. **FAIL:** a floor without its level's name - it means `MassLevelData.LevelId` is not the mass floor's level, and the floors' order and names are wrong |
| **JO3** | *Its negative.* `masses=NoSuchFamily` | `massCount` 0, the finding naming *NoSuchFamily* |
| **JO4** | *A second route for JO2.* A Mass Floor schedule by hand - Mass: Type, Level, Floor Area, Usage - beside JO2's answer | Each row's area as JO2 gave it |
| **JO5** | *Join, then read again.* `JOIN_GEOMETRY` on A and B, kept; `REPORT_MASSES` again | The overlap finding gone; whether the gross floor area of B changes is **recorded, not assumed** - Autodesk Help says the joined mass floors are adjusted |
| **JO6** | *`SET_MASS_FLOOR_USAGE`, positive and negative.* The job file's leg - both masses selected, `usage=Level 1=Retail; Level 2..Level 4=Office`; negative the wall | `changed` 4 on A, B named as having no mass floors; negative *NOTHING WAS CHANGED*, the wall not a mass; rolled back |
| **JO7** | *`SET_MASS_FLOOR_USAGE`, a level the model has not got.* By hand through the bridge client, `--write` without `--apply`: `usage=Level 9=Office` | *NOTHING WAS CHANGED*, Level 9 named and the levels listed |
| **JO8** | *`REMOVE_MASS_FLOORS`, positive and negative.* The job file's leg: `levelNames=Level 4`; negative the wall | `removed` 1, A's floors Levels 1 to 3 after, `notThere` 1 for B; negative *NOTHING WAS REMOVED*; rolled back |
| **JO9** | *`REMOVE_MASS_FLOORS`, empty.* `levelNames=` (empty), A selected | *NOTHING WAS REMOVED*, a level must be named; all four floors still there |
| **JO10** | *`CREATE_WALLS_BY_FACE`, walls.* The job file's leg: both masses, `wallType=Generic - 200mm` | Walls by face on the side faces, each with its type and area, rolled back. **FAIL:** every face refused by Revit's own check - the references read through the family's geometry are not taken, and JM1 on `CREATE_CURTAIN_SYSTEM` will say the same |
| **JO11** | *`CREATE_WALLS_BY_FACE`, twice.* Kept through `try-once` on the saved copy, then the same call again rolled back | Second call `created` 0, `skipped` 2, each mass named as already carrying walls by face. **FAIL:** a second set of walls - `GetJoinedElementIds` does not list walls by face, and the skip needs another way to see them |
| **JO12** | *`CREATE_WALLS_BY_FACE`, a curtain system type.* `wallType=` a curtain system type the model has | *NOTHING WAS MADE*, the finding naming `CREATE_CURTAIN_SYSTEM` |
| **JO13** | *`CREATE_WALLS_BY_FACE`, a floor type.* `wallType=Generic 300mm` | *NOTHING WAS MADE*, Floor by Face named as having no API, `REPORT_MASSES` and `CREATE_FLOOR` named |
| **JO14** | *A slab from an outline.* One outline from JO2 into `CREATE_FLOOR` on Level 2, rolled back | A floor whose Area is 600 m2 |
| **JO15** | *A Mass Floor schedule through Heron* - `massing-study` step 9: `CREATE_SCHEDULE` of the Mass Floor category with Level, Usage and Floor Area | The schedule made, its rows A's floors. **FAIL:** the category refused - the step needs another route, and the method is corrected the same day |
| **JO16** | *`mass-family-creation`, end to end, watching, not a proof.* A new Conceptual Mass; *"make a mass family for a 20 x 15 m block 24 m tall"* | Points, four reference lines, one extrusion 24000 mm; a void cut through `COMBINE_FAMILY_FORMS` if asked - **record whether that call takes a mass family**; loaded and placed with `PLACE_FAMILY_INSTANCES` on Level 1 |
| **JO17** | *`massing-study`, end to end, watching, not a proof.* JO1's project; *"massing study on a 4500 m2 site, retail on Level 1 and offices above"* | The brief table asked for before anything is built; mass floors, usage, the overlap joined, then plot ratio and area by use reported - and nothing called compliant |

**Then sign**, on the owner's word only, one card at a time: `python brain/heron_validate.py accept <fragment>
--by "Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same change. A
row that fails is recorded here and the card corrected; nothing in a row changes to make it pass.

**What this group does not settle.** Whether the plot ratio and site coverage a study gives meet a local rule
is never Heron's to say. Site coverage is read off each mass's lowest mass floor, which is right for a block
standing on its ground level and wrong for one that overhangs its base - recorded in the card, and owed a
better reading only if a real study needs it.
