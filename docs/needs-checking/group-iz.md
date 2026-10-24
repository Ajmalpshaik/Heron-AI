# Needs checking — Group IZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IZ - a hexagon whose size across the flats follows a parameter: `EXTRUDE_POLYGON_BETWEEN_PLANES` version 1 (2026-10-24)

A new card, [`extrude-polygon-between-planes`](../../brain/fragments/extrude-polygon-between-planes/fragment.yaml),
written for the owner's fastener library: a bolt head and a nut are hexagons, and until this card the only
parametric hexagon Heron could make was one fixed hexagon per size, switched by type through a Yes/No
visibility formula (the first *TRG_Generic Model_Hex Bolt*, 2026-10-24). It makes a vertical reference plane
per side, labels each pair of opposite sides with the parameter and an EQ about a plane through the origin -
the family's own Center plane where the pair faces that way, otherwise a plane made through the origin and
pinned - and locks every side face, and the top, of a prism sketched on the base.

**What is done and what is not.** The library compiles for 2020 and 2027 (`tools/check-fragments-compile.py`,
2026-10-24) and every Revit member it calls exists in every release (`tools/check-api-surface.py`). It was
measured in Revit 2020 only - the rows below. `revit_change` refuses it until IZ5 is signed. The fragment's
C# is read live - no restart.

**The family.** A scratch family, never a working one: *Heron IZ Hexagon.rfa* in `%TEMP%\heron-work\group-iz`,
made from Revit 2020's *Metric Generic Model.rft* by `CREATE_FAMILY_DOCUMENT`; in it, through `revit_change`
with PROVEN tools, the type lengths *Head Across Flats* = 18 and *Zero Flats* (left at 0), and the plane *Head
Top* at Z 7.5. Every row is `validate --in "Heron IZ Hexagon" extrude-polygon-between-planes --write`, rolled
back, with `basePlane=Ref. Level` and `topPlane=Head Top`. A flex is the same with the card as `--setup` and
`FLEX_FAMILY` as the fragment - the family holds no other form, so its one box is the prism's.

| # | Run | Look for |
|---|---|---|
| **IZ0** | *Negative FIRST.* `parameterName=Zero Flats`, `sides=6` | Refused before anything is made - *"... holds 0 mm in type "Test" - no size yet ..."*; `size` empty, `acrossFlatsMm` 0, nothing in the family |
| **IZ1** | *The hexagon.* `parameterName=Head Across Flats`, `sides=6` | `size` *"6 sides, 18 mm across the flats and 20.78 mm across the corners, 7.5 mm high, centred at X 0, Y 0, from Z 0 to 7.5"*; 7 locks (six sides and the top); two centre planes made and pinned, the front-back pair on Center (Front/Back) |
| **IZ2** | *The flex.* IZ1 as setup, `trials=Head Across Flats=46 \| Head Across Flats=10` | The prism MEASURED 53.12 x 46 and 11.55 x 10 mm, centred at X 0, Y 0, and put back to 20.78 x 18 |
| **IZ3** | *Other even counts.* `sides=8`, then `sides=4`; the octagon flexed as IZ2 | Octagon 18 across the flats and 19.48 across the corners, 9 locks, flexing to 46 x 46 and 10 x 10; square 18 x 18, 5 locks, no centre plane made |
| **IZ4** | *An odd count.* `sides=5` | Refused - *"an EVEN number of sides, 4 to 12 ..."* - nothing made |
| **IZ5** | The signature, on the owner's word only, after IZ0 to IZ4 | `python brain/heron_validate.py accept extrude-polygon-between-planes --by "Ajmal PS"` |

A row that fails stays open; nothing in a row changes to make it pass.

### Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568

| # | What came back | State |
|---|---|---|
| IZ0 | *"Nothing was built. "Zero Flats" holds 0 mm in type "Test" - no size yet. Labelling now would pull the planes onto each other: give it the size across the flats with SET_FAMILY_TYPE_VALUES first."* | **PASS** |
| IZ1 | First run: **threw** - *"Revit would not label sides 2 and 5 with "Head Across Flats" about "Head Across Flats Centre 2": The references are not geometric references."* - a centre plane made and dimensioned before Revit regenerated. Fixed the same hour (every plane made, one regeneration, then the dimensions - as the probe that preceded the card did) and run again: *"6 sides, 18 mm across the flats and 20.78 mm across the corners, 7.5 mm high, centred at X 0, Y 0, from Z 0 to 7.5"*, `acrossFlatsMm` 18, 7 locks | **PASS** (after the fix) |
| IZ2 | *"Trial 1 (Head Across Flats=46): solid 53.12 x 46 x 7.5 mm, centre X 0 Y 0, base Z 0 across 1 solid(s); 3 of 3 driven sizes read their parameters"*, *"Trial 2 (Head Across Flats=10): solid 11.55 x 10 x 7.5 mm ..."*, `allHeld true`, `restored true` | **PASS** |
| IZ3 | Octagon: *"8 sides, 18 mm across the flats and 19.48 mm across the corners ..."*, 9 locks; flex 46 x 46 and 10 x 10, restored. Square: *"4 sides, 18 mm across the flats and 25.46 mm across the corners ..."*, 5 locks | **PASS** |
| IZ4 | *"Nothing was built. A prism here has an EVEN number of sides, 4 to 12 - 6 for a hexagon - and 5 was asked for ..."* | **PASS** |

The proof draft has IZ1 as its positive and IZ0 as its negative, fingerprint `506b8485027c7bb2`, second route
NOT ESTABLISHED. Not run: any Revit after 2020, and the top face following a labelled *Head Top* - the top
lock is the one `EXTRUDE_BETWEEN_PLANES` makes; the first bolt rebuilt with this card flexes its head height.
