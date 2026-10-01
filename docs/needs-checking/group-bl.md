# Needs checking — Group BL

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group BL - family forms: an extrusion of any shape, a revolve, a blend, a sweep and a swept blend, solid or void (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** Five new tools, written on 2026-10-01 when the owner asked for
every modelling form a family is built from - "extrude, revolve, all" - so that a family can be built from a
sketch or a picture of the thing:

- [`create-family-extrusion`](../../brain/fragments/create-family-extrusion/fragment.yaml) - `CREATE_FAMILY_EXTRUSION`, FRG-GEO-038
- [`create-family-revolution`](../../brain/fragments/create-family-revolution/fragment.yaml) - `CREATE_FAMILY_REVOLUTION`, FRG-GEO-039
- [`create-family-blend`](../../brain/fragments/create-family-blend/fragment.yaml) - `CREATE_FAMILY_BLEND`, FRG-GEO-040
- [`create-family-sweep`](../../brain/fragments/create-family-sweep/fragment.yaml) - `CREATE_FAMILY_SWEEP`, FRG-GEO-041
- [`create-family-swept-blend`](../../brain/fragments/create-family-swept-blend/fragment.yaml) - `CREATE_FAMILY_SWEPT_BLEND`, FRG-GEO-042

**What is already known, and it is NOT a proof.** All five compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01), and every Revit call they make was read in the RevitAPI
reference assemblies of all eight, with Autodesk's own XML remarks, the same day: the five creation calls
have the same signature from 2020 to 2027. **None has met a model.** Every one is `DRAFT`, and each
`tests/cases.yaml` names the negative case its proof owes. The proof plan is
[`tools/jobs/family-forms-2026-10-01.yaml`](../../tools/jobs/family-forms-2026-10-01.yaml), and its dry run
says all five would run as written.

**One shape language for all five.** A profile is loops with `|` between - `circle CX,CY R`,
`rect X1,Y1 X2,Y2`, `polygon CX,CY R N`, or corners `X,Y; X,Y; arc MX,MY X,Y` - in millimetres, in the
work plane's own two model coordinates: X,Y on a level or a horizontal plane, X,Z on a plane facing front or
back, Y,Z on a plane facing left or right. A sweep's and a swept blend's profile is in its own two
coordinates, 0,0 on the path. The rows below are what proves the language means what its cards say.

**The arrangement:** File, New, Family, the metric Generic Model template, saved once as `Heron Family Forms
Test` and left open in front - a family nobody needs. The job file rolls every write back.

| # | Check | Expected |
|---|---|---|
| **BL1** | `create-family-extrusion`: `circle 0,0 150` on `Ref. Level`, 0 to 500; then `0,0; 600,0; 600,100; 100,100; 100,400; 0,400` on `Center (Front/Back)`, -50 to 50; then `rect -300,-200 300,200 \| circle 0,0 50` on `Ref. Level`, 0 to 20 | A cylinder X/Y -150 to 150, Z 0 to 500, **35.343 L**; an L-shaped plate X 0 to 600, **Y -50 to 50**, Z 0 to 400 - the profile read in X,Z because the plane faces front and back; a plate with a round hole, **4.643 L**. Each LOOKED AT in 3D. **Negative:** `0,0; 600,0; 1200,0` refused as enclosing no area, `formId` empty |
| **BL2** | `create-family-revolution` on `Center (Front/Back)`, axis `0,0 0,1000`: `0,0; 150,0; 150,300; 0,300` 0 to 360; `0,0; 200,0; 0,400` 0 to 360; then the first profile 0 to 90 | A cylinder X/Y -150 to 150, Z 0 to 300, **21.206 L**; a cone 400 across and 400 high, **16.755 L**; a quarter turn - **record which quarter it swept into**, which is the one thing the card says it cannot predict. **Negative:** `-100,0; 100,0; 100,300; -100,300` refused as crossing its axis |
| **BL3** | `create-family-blend` on `Ref. Level`: `rect -300,-200 300,200` to `circle 0,0 125` at 300; `rect -200,-200 200,200` to `rect -50,-50 50,50` at 400 | A square-to-round piece X -300 to 300, Y -200 to 200, Z 0 to 300; a pyramid frustum, **28.0 L**. **Record which order Revit took** - the findings say "base first" or "top first", and that settles a question NewBlend's own remarks leave open. **Negative:** a base profile with a hole refused as more than one loop |
| **BL4** | `create-family-sweep` on `Ref. Level`: path `0,0; 1000,0`, profile `circle 0,0 25`; path `0,0; 1000,0; arc 1300,300 1000,600`, profile `circle 0,0 25 \| circle 0,0 20`; then path `0,0; 1000,0` with the ASYMMETRIC profile `rect 0,0 100,20` | A rod X 0 to 1000, **1.963 L**, the findings' area x length agreeing; a tube with one smooth bend; for the third, **record which way the profile's own X and Y landed** - up, sideways, which side of the path - because the card says only that Revit turns it, not how. **Negative:** path `0,0` refused as having no length |
| **BL5** | `create-family-swept-blend` on `Ref. Level`: path `0,0; 600,0`, `circle 0,0 100` to `circle 0,0 50`; path `0,0; arc 424.26,175.74 600,600`, `rect -150,-100 150,100` to `circle 0,0 100` | A frustum along X, **10.996 L**; a rectangular-to-round quarter bend, LOOKED AT. **Negative:** path `0,0; 600,0; 600,600` refused as two pieces, naming CREATE_FAMILY_SWEEP |
| **BL6** | `create-family-extrusion`, `circle 0,0 100`, 0 to 300 - on two horizontal reference planes at 200 mm drawn in the Front elevation, `Low Top A` drawn left to right and `Low Top B` right to left, so that one of the two has its normal pointing DOWN | Z **200 to 500** on BOTH - up from the plane, as the card promises whichever way a plane's normal points. Z -100 to 200 on either would mean the sign turn is wrong: that is the defect this row exists to catch, and every form tool shares the rule |
| **BL7** | BL1 to BL5 as void forms (`solid` false), each beside a solid it overlaps | Each void built and read back, its findings saying it **cuts nothing yet** - and the solid it overlaps LOOKED AT, uncut. A void that DOES cut on its own here would make that sentence wrong in all five cards |
| **BL8** | BL1 to BL7 on **Revit 2020** | The same answers. Every call is the same on both releases by the reference assemblies; this is the run that says so in Revit |

**Not in this group, because not built here:** locking a form's faces to planes, labelling a round size, and
cutting a solid with a void - the steps that make these forms parametric and make a void cut. Each form
tool's findings say so in its own words.
