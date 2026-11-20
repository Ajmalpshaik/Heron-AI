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
footprint** is refused on every release - its edges are its sketch, which this card does not edit (row
[5b-1930](../fragment-issues/row-5b-1930.md)). The `join-roofs` method offers it in its step 1. Written in a cloud
container with no Revit; it compiles on 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-20). **Nothing
here has run in Revit.** No add-in code changed: the card travels as source.

**The model is a scratch copy, never a working model** - *roofjoin-oy*, the arrangement in the card's
`tests/cases.yaml`, and the job file
[`tools/jobs/extend-roof-to-roof-oy-2026-11-20.yaml`](../../tools/jobs/extend-roof-to-roof-oy-2026-11-20.yaml).
Every run rolled back unless the row says kept. Any release; the one the owner's runs use first.

| ID | Do this | Pass looks like |
|---|---|---|
| **OY1** | *Build roofjoin-oy* - the arrangement in the card's `tests/cases.yaml`. From the default metric architectural template: Level 2 at 3000 mm; a roof by footprint on Level 2, *Generic - 400mm*, the rectangle 0,0 to 12000,8000 mm, south and north edges Defining Slope at 30 degrees, the ends upright; a reference plane *Wing* in Level 2 from 4000,-6000 to 8000,-6000 mm and on it a roof by extrusion, *Generic - 400mm*, profile *0,500; 2000,1655; 4000,500* (its eaves 500 mm above Level 2), Extrusion Start 0 and End 3000 mm (`CREATE_EXTRUSION_ROOF` makes it); a second the same on a plane *Far* from 20000,0 to 24000,0 mm; a copy of the main roof 30000 mm north. Save it under `%TEMP%\heron-work`. **Write down the wing's Extrusion Start and End as Properties shows them** | The wing runs NORTH from y = -6000 and stops about 3000 mm short of the main roof's south eave - if it runs south, set Start and End by hand so it runs north, and write down what they are; the far roof runs north beside the main roof, facing nothing; {3D} shows all four roofs |
| **OY2** | *Negative - THE FIRST CASE TO RUN.* The job file's negative: the wing and the main roof selected, view3d {3D}, `maxExtendMm=500` | `extended` 0; `refused` *"... has to extend <needMm> mm to meet roof by footprint ..., more than the 500 mm allowed. Nothing was changed"*; `needMm` more than 3000 and less than 7000; the wing's two values as written down in OY1 |
| **OY3** | *Positive.* The job file's positive, `maxExtendMm=10000`. Then, as a run of its own, the same pair with `maxExtendMm` left out (values `minMm=-500,-6500,2500 maxMm=12500,8500,9000 exact=false categories=Roofs view3d={3D}`), its output `brain/proof-drafts/runs/extend-roof-to-roof-oy3-measure.json` | `extended` 1, `extendedRoof` the wing's id, `report` *"EXTENDED: ... its end moved <needMm> mm toward roof by footprint ... read back off the roof's geometry, its other end where it was"*, and a finding that the flat end went further elsewhere - through the main roof and on under it - offering JOIN_GEOMETRY. In a section through x = 6000 the wing's ridge reaches the main roof's slope with no gap; in a section through x = 4500 the end passes through the slope and on under it, as the finding says; its south end has not moved. **Measure needMm against a dimension in the x = 6000 section.** Left out: `extended` 0, `refused` begins *"MEASURED, NOTHING CHANGED"* with the same distance |
| **OY4** | *Negatives - nothing faces, and two footprint roofs*, each a run of its own. (a) The main roof and the far roof: `minMm=-500,-500,2500 maxMm=24500,8500,9000 exact=false categories=Roofs view3d={3D} maxExtendMm=50000`, output `...-oy4a.json`. (b) The main roof and its copy: `minMm=-500,-500,2500 maxMm=12500,38500,9000 exact=false categories=Roofs view3d={3D} maxExtendMm=10000`, output `...-oy4b.json` | (a) `refused` begins *"NOTHING MET"*, the report lists both ends of the far roof with 0 points meeting. (b) `refused` *"Both roofs are roofs by footprint, and this tool does not extend a roof by footprint on Revit <release> ... Modify > Join/Unjoin Roof"*, naming the release Revit reports. Nothing changed either time |
| **OY5** | *The second route.* On a scratch copy KEPT once with OY3's positive, then on another copy as saved before it, Modify > Join/Unjoin Roof by hand: the wing's north end edge, then the main roof's south slope | Write down the wing's Extrusion End (or Start) both ways. Revit's own join cuts the end to the slope; the card's flat end reaches the farthest point - say whether the difference is the card's nearest-to-farthest spread |
