# Needs checking — Group CV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CV - one smooth freeform form: a loft through sections, a sweep along a 3D path (2026-10-07)

`CREATE_FAMILY_FREEFORM_SOLID` (FRG-GEO-058, DRAFT) makes ONE smooth form in the family open in the Family
Editor where the classic forms cannot: a LOFT through two or more sections placed anywhere and facing any
way, or a SWEEP of a profile along a path that leaves the plane - a helix, a spline through points, or
straight pieces and arcs in X,Y,Z. It is built as a solid (`GeometryCreationUtilities`) and kept as a
FREEFORM form (`FreeFormElement.Create`), solid or void. **It is not parametric** - a fixed shape, no
sketch, no parameter, no plane moves it. Asked for by the owner on 2026-10-07 after a coil spring of 144
stacked revolves was rejected as slow and heavy and an elephant of ellipsoid revolves showed seams
([row 5b-368](../fragment-issues/section-5b-rows-351-375.md)).

Compiled for Revit 2020 to 2027 on 2026-10-07. **Every run below was on Revit 2024, session 9240, never on
the owner's Family1.** The API probes ran rolled back in `SpringProbe` (a scratch Generic Model family of
the spring session, nothing kept); the tool itself ran in **`FreeformProof`**, a family made for this
group by `CREATE_FAMILY_DOCUMENT` from *Metric Generic Model* and saved empty to
`%TEMP%\heron-work\freeform\FreeformProof.rfa`. Kept in its open window, unsaved: the freeform spring, the
freeform body (CV3) and the twelve swept blends of CV9 - so they can be LOOKED at (CV10).

With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` and `< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| **CV1** | `validate --in "FreeformProof" --write --set kind=sweep --set "profiles=circle 0,0 7" --set "path=helix 0,0,0 55 25 6" --set solid=true --negative-set kind=sweep --negative-set "profiles=circle 0,0 20" --negative-set "path=spline 0,0,0; 1000,1000,0; 1000,0,0; 0,1000,0" --negative-set solid=true create-family-freeform-solid` - or the job file [`create-family-freeform-solid-2026-10-07.yaml`](../../tools/jobs/create-family-freeform-solid-2026-10-07.yaml) | ONE form, a spring 124 mm outside, wire 14, six turns at 25: X and Y -62 to 62, Z -6.98 to 156.94 mm, about 0.320 L (pi x 49 mm2 x 2078.87 mm of helix). **RAN 2026-10-07, ROLLED BACK:** exactly that - *a smooth curve through 97 points, 2078.92 mm long ... volume 0.32 L*; record `brain/proof-drafts/runs/create-family-freeform-solid.json`, draft fingerprint `6f337d5ecf03de86`, UNSIGNED |
| **CV2** | the negative half of CV1: a path that crosses itself | refused BY NAME, both places named, `formId` empty. **RAN 2026-10-07, ROLLED BACK:** *The path comes within 7.59 mm of itself, near 208.86,503.8,0 and again near 208.86,496.2,0, 3582.78 mm further along it - less than twice the 20 mm the profile reaches from it*. Asked directly, Revit BUILDS this sweep without complaint (probe: 5.8694 L against 5.8731 for the wire with no crossing - the overlap counted twice), which is why the check exists |
| **CV3** | `--set kind=loft --set "profiles=at 0,0,800 facing X: ellipse 0,0 60 50 \| at 300,0,850 facing X: ellipse 0,0 220 250 \| at 700,0,820 facing X: ellipse 0,0 260 300 \| at 1100,0,800 facing X: ellipse 0,0 200 240 \| at 1400,0,900 facing X: ellipse 0,0 80 90" --set path= --set solid=true` | ONE body, X 0 to 1400 mm, about 221.36 L, no seam. **RAN 2026-10-07:** 221.355 L, X 0 to 1400, Y -261.47 to 261.47, Z 512.21 to 1136.8 mm; the probe read 4 faces - two sides and two ends. KEPT in FreeformProof |
| **CV4** | the loft's own checks and Revit's: the CV3 sections with the middle one `ellipse 0,0 260 300 4` (2/2/4/2/2 pieces); the sections out of order (0, 700, 300, 1100); a rectangle lofted into a circle cut in 4 | **RAN 2026-10-07, ROLLED BACK:** unequal counts refused naming 2/2/4/2/2 (a probe let Revit build it: 201.1 L, 10 faces - distorted); out of order refused by Revit, *Failed to generate the requested loft geometry*, nothing kept; rectangle to circle 37.902 L, X -200 to 200, Y -150 to 150 - the circle started level with the rectangle's first corner. Started on its + side instead it read 34.337 L, twisted |
| **CV5** | sweeps that must be refused: `circle 0,0 13` on CV1's helix (26 mm wire, 25 mm pitch); `circle 0,0 9` on `helix 0,0,0 5 25 6` (a coil tighter than the wire); `circle 0,0 20` on `0,0,0; 1000,0,0; 0,30,0` (a hairpin) | **RAN 2026-10-07, ROLLED BACK:** all three refused by name, nothing built - *within 24.94 mm of itself*; *bends tighter than its profile near 5,0,0 - a radius of 7.45 mm, where the profile reaches 9 mm*; *within 1.25 mm of itself*. Revit itself built the first two when asked directly (probe) |
| **CV6** | sweeps that must be built: a hollow tube `circle 0,0 20 \| circle 0,0 15` on `0,0,0; 500,0,0; arc 641.42,0,58.58 700,0,200; 700,0,800`; a square-cornered path `0,0,0; 500,0,0; 500,500,300`; `rect -5,-3 5,3` on `helix 0,0,500 40 20 4 X left` | **RAN 2026-10-07, ROLLED BACK:** tube 0.777 L - pi x (400 - 225) mm2 x 1414.16 mm, so the hole is real - X 0 to 720, Z -20 to 800; corner 1.361 L (mitred by Revit, the wire's own volume); the X-axis left-hand coil X -3.59 to 85.7, Y -45.44 to 45.47, Z 454.93 to 545.59. A loft through a spline section and sections facing `1,0,0.3` and `1,0,1`: 175.137 L |
| **CV7** | a void: CV1 with `solid=false`, then `COMBINE_FAMILY_FORMS` with that id and a solid's | **RAN 2026-10-07, ROLLED BACK:** built, reads back as a VOID (`IsSolid` false). The combine is measured on the probe only: a freeform void cylinder (r 100, 400 high) combined with a freeform 600 x 600 x 300 box by `Document.CombineElements` read 98.576 L from 108 - the cylinder's 9.42 L cut out. **OWED:** the same through `COMBINE_FAMILY_FORMS` with the two ids |
| **CV8** | a material: `SET_FAMILY_FORM_MATERIAL` with CV1's kept id | the form shows the material in Shaded. **OWED.** The probe read a freeform form's Material field present and writable |
| **CV9** | the second route: the same spring as twelve `CREATE_FAMILY_SWEPT_BLEND` half-turns (`355,0; arc 300,55 245,0` and back, profiles `circle 0,12.5k 7` to `circle 0,12.5(k+1) 7`), kept in FreeformProof beside the freeform one | **RAN 2026-10-07, KEPT, read without a transaction:** twelve swept blends 0.318648 L, 48 faces, X 238 to 362, Z -7 to 157; the one freeform spring 0.319513 L, 4 faces, X -61.18 to 62, Z -6.98 to 156.94; the true helix 0.32001 L. One form, the same shape, within 0.3% |
| **CV10** | LOOK at FreeformProof in 3D, Shaded and Fine: the spring beside the twelve swept blends, the body above | the freeform spring smooth all the way round, with no joins every half-turn; the body with no seam between sections. **OWED - the owner's eyes** |
| **CV11** | a project in front: `validate --in "<a project>" create-family-freeform-solid` with NO `--write` | `notAFamily` true, refused, nothing built. **OWED:** no project was open in that Revit on 2026-10-07 |
| **CV12** | the weight: FreeformProof saved with the freeform spring alone, then with the twelve swept blends alone | the file sizes, side by side. **OWED:** saving is the Publish switch's |
| **CV13** | the signature | `python brain/heron_validate.py accept create-family-freeform-solid --by "Ajmal PS"` on the CV1/CV2 record (`brain/proof-drafts/runs/create-family-freeform-solid.json`), on the owner's word only, after CV10 |
| **CV14** | CV1 to CV6 on **Revit 2020** and **2027** | The same answers. Every API member it calls reads the same 2020 to 2027 (api-surface) and it compiles on all eight releases; what Revit DOES with them was measured on 2024 only |

**What the probes measured first** (rolled back, `SpringProbe`, a scratch fragment kept outside the
repository): Revit's own `CylindricalHelix` is refused for a sweep - *The input curve points to a helical
curve and is not supported for this operation* - so the helix is a smooth curve through points; how far
it strays from a true helix of radius 55 mm by points a turn - 4: 5.49 mm, 6: 1.44, 8: 0.51, 12: 0.11,
16: 0.036, 24: 0.007 - so the tool uses 16, every build about 25 ms and 4 faces; a closed spline cut in
two is refused by the loft, two open splines whose end directions match are not (221.53 L for CV3's body
drawn that way); a warped section lofts.

**Routing, measured 2026-10-07 on scratch stores - not proof.** Against main's library, both search
backends, 32 sentences: *make a spring as one smooth form*, *make the helix spring in one piece, not many
pieces*, *make a smooth organic body from cross sections* and *loft the elephant body through oval
sections* now reach it, where they reached `LOCK_FORM_TO_PLANES`, `CONVERT_CAD_TO_DIRECTSHAPE`,
`CREATE_SECTION_VIEW` and `CREATE_ROOM_ELEVATIONS`. Every neighbour's own sentence stays put. **Two moved
the wrong way on the trained model and are recorded, not tuned away** (row 5b-368): *make a parametric
spring* reaches this card, where it reached `LABEL_FAMILY_DIMENSION` - neither is that job, which is the
chain of PR #440 - and *array the coil turn up the spring* reaches `ARRAY_ELEMENTS_RADIAL` where it reached
`ARRAY_FAMILY_FORMS` (the card's first wording took it itself). `check-routing` lists no crossing for it.

**Re-measured 2026-10-08 against main with #440 merged (`cdc16ab0`), both backends, the same 32
sentences: neither moves any more** - *make a parametric spring* and *array the coil turn up the spring*
land where they land without this card. The four gains above still hold. One sentence still moves, on the
lexical backend only: *give the spring a steel material* goes from `SET_MATERIAL_COLOUR` to
`SELECT_BY_MATERIAL` - neither is its job (`SET_FAMILY_FORM_MATERIAL`), and this card wins neither.
