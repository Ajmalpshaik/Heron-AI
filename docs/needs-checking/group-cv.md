# Needs checking — Group CV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CV - one smooth freeform form: a loft through sections, a sweep along a 3D path (2026-10-07)

**Tracked as GitHub issue [#978](https://github.com/Ajmalpshaik/Heron-AI/issues/978) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

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
| ~~**CV1**~~ | validate --in "FreeformProof" --write --set kind=sweep --set "profiles=circle 0,0 7" --set… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | ONE form, a spring 124 mm outside, wire 14, six turns at 25: X and Y -62 to 62, Z -6.98… |
| ~~**CV2**~~ | the negative half of CV1: a path that crosses itself — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | refused BY NAME, both places named, `formId` empty. |
| ~~**CV3**~~ | --set kind=loft --set "profiles=at 0,0,800 facing X: ellipse 0,0 60 50 \| at 300,0,850 facing X… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | ONE body, X 0 to 1400 mm, about 221.36 L, no seam. |
| ~~**CV4**~~ | the loft's own checks and Revit's: the CV3 sections with the middle one `ellipse 0,0 260 300 4`… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **RAN 2026-10-07, ROLLED BACK:** |
| ~~**CV5**~~ | sweeps that must be refused: circle 0,0 13 on CV1's helix (26 mm wire, 25 mm pitch); circle 0,0… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **RAN 2026-10-07, ROLLED BACK:** |
| ~~**CV6**~~ | sweeps that must be built: a hollow tube circle 0,0 20 \| circle 0,0 15 on 0,0,0; 500,0,0; arc… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **RAN 2026-10-07, ROLLED BACK:** |
| **CV7** | a void: CV1 with `solid=false`, then `COMBINE_FAMILY_FORMS` with that id and a solid's | **RAN 2026-10-07, ROLLED BACK:** built, reads back as a VOID (`IsSolid` false). The combine is measured on the probe only: a freeform void cylinder (r 100, 400 high) combined with a freeform 600 x 600 x 300 box by `Document.CombineElements` read 98.576 L from 108 - the cylinder's 9.42 L cut out. **OWED:** the same through `COMBINE_FAMILY_FORMS` with the two ids |
| **CV8** | a material: `SET_FAMILY_FORM_MATERIAL` with CV1's kept id | the form shows the material in Shaded. **OWED.** The probe read a freeform form's Material field present and writable |
| ~~**CV9**~~ | the second route: the same spring as twelve CREATE_FAMILY_SWEPT_BLEND half-turns (355,0; arc… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **RAN 2026-10-07, KEPT, read without a transaction:** |
| **CV10** | LOOK at FreeformProof in 3D, Shaded and Fine: the spring beside the twelve swept blends, the body above | the freeform spring smooth all the way round, with no joins every half-turn; the body with no seam between sections. **OWED - the owner's eyes** |
| **CV11** | a project in front: `validate --in "<a project>" create-family-freeform-solid` with `--write` and no `--apply` - not with NO `--write`, which the command line refuses for a MODIFY fragment since 2026-10-17 ([row 5b-350](../fragment-issues/section-5b-rows-326-350.md)) | `notAFamily` true, refused, nothing built, and the group rolled back. **OWED:** no project was open in that Revit on 2026-10-07 |
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

**Written back 2026-11-12, read in a cloud container (no Revit) - nothing was run here.** Since this group was
written: `CREATE_FAMILY_FREEFORM_SOLID` reads PROVEN on its card, at version 1 - its `proof` block is signed by Ajmal
PS, 2026-10-18, on Family1, Revit 2020, session 13520. So where this page calls a card named here DRAFT, or says
nothing has run in Revit, that was true when it was written and is out of date as a statement about the card. Whether
CV1 to CV14 were what that proof ran is NOT sorted here: for the owner.
