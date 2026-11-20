# Needs checking — Group OY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OY - EXTEND_ROOF_TO_ROOF, a roof by extrusion extended to meet another roof by rays: the owner's other way to Join/Unjoin Roof - DRAFT until proved (2026-11-20)

**Tracked as GitHub issue [#1144](https://github.com/Ajmalpshaik/Heron-AI/issues/1144) since 2026-11-20** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** row [5b-1877](../fragment-issues/row-5b-1877.md), GitHub issue
[#1087](https://github.com/Ajmalpshaik/Heron-AI/issues/1087), on the owner's answer in
[#918](https://github.com/Ajmalpshaik/Heron-AI/issues/918): Revit's API has no Join/Unjoin Roof on any release, so
cast a ray from the end of one roof to the other roof's face, work out how far the first has to extend, and extend
it there - offered first, and run only on a yes. The new card
[`extend-roof-to-roof`](../../brain/fragments/extend-roof-to-roof/fragment.yaml) (`EXTEND_ROOF_TO_ROOF`, DRAFT
v1) does that for a **roof by extrusion**, whose extent is its Extrusion Start and End on every release; a **roof by
footprint** is refused on every release, because the API gives no way to edit its sketch (row
[5b-1927](../fragment-issues/row-5b-1927.md)). The `join-roofs` method offers it in its step 1. Written in a cloud
container with no Revit; it compiles on 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-20). **Nothing
here has run in Revit.** No add-in code changed: the card travels as source.

**The model is a scratch copy, never a working model** - *roofjoin-oy*, the arrangement in the card's
`tests/cases.yaml` and the job file
[`tools/jobs/extend-roof-to-roof-oy-2026-11-20.yaml`](../../tools/jobs/extend-roof-to-roof-oy-2026-11-20.yaml).
Every run rolled back unless the row says kept. Any release; the one the owner's runs use first.

| ID | Do this | Pass looks like |
|---|---|---|
| **OY1** | *Build roofjoin-oy.* From the default metric architectural template: Level 2 at 3000 mm; a roof by footprint on Level 2, *Generic - 400mm*, the rectangle 0,0 to 12000,8000 mm, south and north edges Defining Slope at 30 degrees, the ends upright; a reference plane *Wing* in Level 2 from 4000,-6000 to 8000,-6000 mm and on it a roof by extrusion, *Generic - 400mm*, profile *0,0; 2000,1155; 4000,0*, Extrusion Start 0 and End 3000 mm; a second the same on a plane *Far* from 20000,-6000 to 24000,-6000 mm. Save it under `%TEMP%\heron-work`. **Write down the wing's Extrusion Start and End as Properties shows them** | The wing runs north from y = -6000 and stops 3000 mm short of the main roof's south eave; {3D} shows all three roofs |
| **OY2** | *Negative - THE FIRST CASE TO RUN.* Both the wing and the main roof selected, view3d {3D}, `maxExtendMm=500` (the job file's negative) | `extended` 0; `refused` *"... has to extend <needMm> mm to meet roof by footprint ..., more than the 500 mm allowed. Nothing was changed"*; `needMm` more than 3000; the wing's two values as written down in OY1 |
| **OY3** | *Positive.* The same pair, `maxExtendMm=10000` (the job file's positive). Then once more with `maxExtendMm` left out | `extended` 1, `extendedRoof` the wing's id, `report` *"EXTENDED: ... its end moved <needMm> mm toward roof by footprint ... read back off the roof's geometry, its other end where it was"*, a finding offering JOIN_GEOMETRY for the overlap. In a section through x = 6000 the wing's ridge reaches the main roof's slope and no gap shows along its end; its south end has not moved. **Measure needMm against a dimension in that section.** Left out: `extended` 0, `refused` begins *"MEASURED, NOTHING CHANGED"* with the same distance |
| **OY4** | *Negative - nothing faces.* The far roof and the main roof, `maxExtendMm=50000`. Then the main roof and a copy of it 30000 mm east, `maxExtendMm=10000` | First: `refused` begins *"NOTHING MET"*, the report lists both ends of the far roof with 0 points meeting. Second: `refused` *"Both roofs are roofs by footprint, and Revit <release>'s API cannot extend one ... Modify > Join/Unjoin Roof"*, naming the release Revit reports. Nothing changed either time |
| **OY5** | *The second route.* On a scratch copy KEPT once with OY3's positive, then on another copy as saved before it, Modify > Join/Unjoin Roof by hand: the wing's north end edge, then the main roof's south slope | Write down the wing's Extrusion End (or Start) both ways. The card's reaches the farthest point; Revit's own join cuts the end to the slope - the difference is the card's nearest-to-farthest spread, and the card says so |
