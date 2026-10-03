# Needs checking — Group BM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group BM - what makes a family's forms parametric and makes a void cut: join and cut, face locks, a round size labelled, visibility, subcategory, and a list of the forms (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** Six new tools, the second of the three changes the owner asked
for on 2026-10-01 - [Group BL](../needs-checking/group-bl.md) built the forms, and these make them behave:

- [`combine-family-forms`](../../brain/fragments/combine-family-forms/fragment.yaml) - `COMBINE_FAMILY_FORMS`, FRG-GEO-043 - join solids, or cut a solid with a void
- [`lock-form-to-planes`](../../brain/fragments/lock-form-to-planes/fragment.yaml) - `LOCK_FORM_TO_PLANES`, FRG-GEO-044 - align and lock any form's flat faces
- [`label-family-radius`](../../brain/fragments/label-family-radius/fragment.yaml) - `LABEL_FAMILY_RADIUS`, FRG-GEO-045 - a round form's radius or diameter labelled
- [`set-family-form-visibility`](../../brain/fragments/set-family-form-visibility/fragment.yaml) - `SET_FAMILY_FORM_VISIBILITY`, FRG-VIEW-114
- [`set-family-form-subcategory`](../../brain/fragments/set-family-form-subcategory/fragment.yaml) - `SET_FAMILY_FORM_SUBCATEGORY`, FRG-VIEW-115
- [`report-family-forms`](../../brain/fragments/report-family-forms/fragment.yaml) - `REPORT_FAMILY_FORMS`, FRG-GEO-046 - every form, with its id

**What is already known, and it is NOT a proof.** All six compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01), and every Revit call they make reads the same 2020 to 2027
in the reference assemblies. **None has met a model; every one is `DRAFT`.** The proof plan is
[`tools/jobs/family-constraints-2026-10-01.yaml`](../../tools/jobs/family-constraints-2026-10-01.yaml) - its
header says how the arrangement is made and where the two form ids come from, because the dry run does NOT
catch a placeholder left in.

**Two of these rows answer questions nobody here could.** BM1 says whether the family editor's geometry
combination really cuts with a void made through the API - the API's remarks say it is the only route in an
ordinary family, and an earlier family build found a void that never cut. BM3 says whether a label on a
form's round EDGE drives its circle - the calls exist on every release, the sketch itself cannot be edited
through the API in a family, and [row 5b-261](../FRAGMENT-ISSUES.md) records why the old "impossible on 2020"
was about projects.

**The arrangement:** `Heron Family Constraints Test`, exactly as the job file's header gives it - a plane
`Body Top` at 500, a length type parameter `Neck Diameter` holding 300, a solid cylinder `circle 0,0 150` 0 to
500 and a void `circle 0,0 50` -10 to 510 on `Ref. Level`, both made with `create-family-extrusion` APPLIED.

| # | Check | Expected |
|---|---|---|
| **BM1** | `combine-family-forms` with the solid's and the void's ids; then, on a copy, a void `circle 1000,0 50` that misses the solid | `cut` true; the solid **35.343 L** before and about **31.416 L** after - the 3.927 L bore taken away - and LOOKED AT in 3D as a tube. The void that misses: **refused**, the volume unchanged, no combination left. **If the first is refused too, the geometry combination does not cut through the API either** - record Revit's message, it is the answer to the question this row exists for |
| **BM2** | `lock-form-to-planes`, the solid, planes `Body Top, Ref. Level`; then `Body Top` dragged to 800 by hand | Two locks in an elevation, two closed padlocks LOOKED AT; after the drag the cylinder **measures 800 tall**. **Negative:** planes `Center (Left/Right)` - the round side has no flat face there - refused, nothing locked |
| **BM3** | `label-family-radius`, the solid, `Neck Diameter`, diameter true; then `flex-family` with `Neck Diameter=400` | A diameter dimension in the plan reading **300**, labelled. Then the flex: **the cylinder measured 400 across, or not** - either answer is the finding; record it, with the release, because whether a label on a form's EDGE drives its sketch is known to nobody here. **Negative:** diameter false (a radius of 300 asked) refused, listing the radius 150 **ANSWERED 2026-10-03, Revit 2024: NO, and version 2 does it another way.** Version 1's label on the finished form's round EDGE was accepted and drove nothing: on the stem of the PPR gate valve in Family1 it read 8 mm while its parameter held 14, and `flex-family` refused. A dimension on the form's SKETCH curve does drive it - measured the same evening at 300 and 40 mm on a new cylinder and on one built days earlier - so version 2 labels inside the sketch: an extrusion's circle, or a revolve's radius measured from a reference plane through its axis. Run rolled back in a scratch Family2 (Metric Generic Model): `Pipe Diameter` 100 on a cylinder, flexed 300 (the family 300 deep) and 40; `Bell Radius` 60 on a revolve, flexed 300 (600 deep) and 40; a revolve with no plane through its axis refused by name. Row [5b-305](../fragment-issues/section-5b-rows-176-200.md). **Signed by Ajmal PS 2026-10-03 - PROVEN.** **Still owed:** the same on Revit 2020 (BM7) |
| **BM4** | `set-family-form-visibility`, the solid, `medium, fine` and `front, left`; then the family loaded into a project | The form's Visibility Settings, opened by hand, ticked exactly so; in the project, absent from a coarse plan and present in a fine elevation. **Negative:** `rough` refused by name |
| **BM5** | `set-family-form-subcategory`, the solid, `Casing`; then again with `none` | `createdSubcategory` true and Object Styles listing `Casing` under Generic Models; then back on the family's own category, read back. **Negative:** a subcategory of nothing refused |
| **BM6** | `report-family-forms`, `all`; then `void`; then `revolve` | Both forms, with the same ids `create-family-extrusion` gave back, their extents and the solid's volume; then the void alone; then nothing, with `scanned` 2. After BM1 on a kept copy, the combination listed with both members |
| **BM7** | BM1 to BM6 on **Revit 2020** | The same answers - and BM3's answer on 2020 is the one the old card said was impossible |

**Not in this group, because not built:** materials (set in Properties, or LINK_FAMILY_PARAMETER for every
form of one kind), reference lines and angles, nested families, and the order a family is built in from a
picture - that is the third change.
