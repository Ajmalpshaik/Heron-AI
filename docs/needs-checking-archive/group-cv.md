# NEEDS-CHECKING archive — Group CV

> **Checks that were done, moved out of the live register.** These are rows of Group CV of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *one smooth freeform form: a loft through sections, a sweep along a 3D path (2026-10-07)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row CV1

*Moved from the register on 2026-11-17.*

**Run.** `validate --in "FreeformProof" --write --set kind=sweep --set "profiles=circle 0,0 7" --set "path=helix 0,0,0 55 25 6" --set solid=true --negative-set kind=sweep --negative-set "profiles=circle 0,0 20" --negative-set "path=spline 0,0,0; 1000,1000,0; 1000,0,0; 0,1000,0" --negative-set solid=true create-family-freeform-solid` - or the job file [`create-family-freeform-solid-2026-10-07.yaml`](../../tools/jobs/create-family-freeform-solid-2026-10-07.yaml)

**Look for.** ONE form, a spring 124 mm outside, wire 14, six turns at 25: X and Y -62 to 62, Z -6.98 to 156.94 mm, about 0.320 L (pi x 49 mm2 x 2078.87 mm of helix). **RAN 2026-10-07, ROLLED BACK:** exactly that - *a smooth curve through 97 points, 2078.92 mm long ... volume 0.32 L*; record `brain/proof-drafts/runs/create-family-freeform-solid.json`, draft fingerprint `6f337d5ecf03de86`, UNSIGNED **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CV2

*Moved from the register on 2026-11-17.*

**Run.** the negative half of CV1: a path that crosses itself

**Look for.** refused BY NAME, both places named, `formId` empty. **RAN 2026-10-07, ROLLED BACK:** *The path comes within 7.59 mm of itself, near 208.86,503.8,0 and again near 208.86,496.2,0, 3582.78 mm further along it - less than twice the 20 mm the profile reaches from it*. Asked directly, Revit BUILDS this sweep without complaint (probe: 5.8694 L against 5.8731 for the wire with no crossing - the overlap counted twice), which is why the check exists **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CV3

*Moved from the register on 2026-11-17.*

**Run.** `--set kind=loft --set "profiles=at 0,0,800 facing X: ellipse 0,0 60 50 \| at 300,0,850 facing X: ellipse 0,0 220 250 \| at 700,0,820 facing X: ellipse 0,0 260 300 \| at 1100,0,800 facing X: ellipse 0,0 200 240 \| at 1400,0,900 facing X: ellipse 0,0 80 90" --set path= --set solid=true`

**Look for.** ONE body, X 0 to 1400 mm, about 221.36 L, no seam. **RAN 2026-10-07:** 221.355 L, X 0 to 1400, Y -261.47 to 261.47, Z 512.21 to 1136.8 mm; the probe read 4 faces - two sides and two ends. KEPT in FreeformProof **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CV4

*Moved from the register on 2026-11-17.*

**Run.** the loft's own checks and Revit's: the CV3 sections with the middle one `ellipse 0,0 260 300 4` (2/2/4/2/2 pieces); the sections out of order (0, 700, 300, 1100); a rectangle lofted into a circle cut in 4

**Look for.** **RAN 2026-10-07, ROLLED BACK:** unequal counts refused naming 2/2/4/2/2 (a probe let Revit build it: 201.1 L, 10 faces - distorted); out of order refused by Revit, *Failed to generate the requested loft geometry*, nothing kept; rectangle to circle 37.902 L, X -200 to 200, Y -150 to 150 - the circle started level with the rectangle's first corner. Started on its + side instead it read 34.337 L, twisted **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CV5

*Moved from the register on 2026-11-17.*

**Run.** sweeps that must be refused: `circle 0,0 13` on CV1's helix (26 mm wire, 25 mm pitch); `circle 0,0 9` on `helix 0,0,0 5 25 6` (a coil tighter than the wire); `circle 0,0 20` on `0,0,0; 1000,0,0; 0,30,0` (a hairpin)

**Look for.** **RAN 2026-10-07, ROLLED BACK:** all three refused by name, nothing built - *within 24.94 mm of itself*; *bends tighter than its profile near 5,0,0 - a radius of 7.45 mm, where the profile reaches 9 mm*; *within 1.25 mm of itself*. Revit itself built the first two when asked directly (probe) **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CV6

*Moved from the register on 2026-11-17.*

**Run.** sweeps that must be built: a hollow tube `circle 0,0 20 \| circle 0,0 15` on `0,0,0; 500,0,0; arc 641.42,0,58.58 700,0,200; 700,0,800`; a square-cornered path `0,0,0; 500,0,0; 500,500,300`; `rect -5,-3 5,3` on `helix 0,0,500 40 20 4 X left`

**Look for.** **RAN 2026-10-07, ROLLED BACK:** tube 0.777 L - pi x (400 - 225) mm2 x 1414.16 mm, so the hole is real - X 0 to 720, Z -20 to 800; corner 1.361 L (mitred by Revit, the wire's own volume); the X-axis left-hand coil X -3.59 to 85.7, Y -45.44 to 45.47, Z 454.93 to 545.59. A loft through a spline section and sections facing `1,0,0.3` and `1,0,1`: 175.137 L **Struck 2026-11-13:** passed, as the result written in this row records.

---

### Row CV9

*Moved from the register on 2026-11-17.*

**Run.** the second route: the same spring as twelve `CREATE_FAMILY_SWEPT_BLEND` half-turns (`355,0; arc 300,55 245,0` and back, profiles `circle 0,12.5k 7` to `circle 0,12.5(k+1) 7`), kept in FreeformProof beside the freeform one

**Look for.** **RAN 2026-10-07, KEPT, read without a transaction:** twelve swept blends 0.318648 L, 48 faces, X 238 to 362, Z -7 to 157; the one freeform spring 0.319513 L, 4 faces, X -61.18 to 62, Z -6.98 to 156.94; the true helix 0.32001 L. One form, the same shape, within 0.3% **Struck 2026-11-13:** passed, as the result written in this row records.

---
