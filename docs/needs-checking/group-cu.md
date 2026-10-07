# Needs checking — Group CU

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CU - a parametric coil spring: swept blend labels, a nested family arrayed up, its last copy locked (2026-10-07)

Asked by the owner on 2026-10-07: a compression spring for vibration isolators, built the way ThomRVT's
video *How to create Spring Revit Family (Parametric)* builds it - each half-turn of the coil one swept
blend, a one-turn family nested in the main one and arrayed up, ground end turns whose profile is a half
circle - with few clear parameters ([row 5b-366](../fragment-issues/section-5b-rows-176-200.md)). Three
PROVEN fragments were widened rather than three new ones made; each was proved again on a scratch family
and signed by Ajmal PS on 2026-10-07 (CU7), so each is PROVEN at its new version:

- `LABEL_FAMILY_RADIUS` version 4 (FRG-GEO-045) labels a SWEPT BLEND: its path's arc radius, the arc's
  centre locked to the planes through it; every profile circle's own radius, a half circle's arc included;
  and a profile circle centre's height off the path's plane - a plane through the centre, made and named
  after the parameter, the centre locked to it, the plane labelled from the level the path lies on. One
  parameter may label the same kind of size on several swept blends.
- `ARRAY_FAMILY_FORMS` version 2 (FRG-GEO-048, on top of PR #437's 5b-357 fix) arrays ONE NESTED FAMILY
  as well as a form, and gives the copies back in order along the row.
- `LOCK_NESTED_FAMILY_TO_PLANES` version 2 (FRG-GEO-049) locks an array's copy, named by its group's id.

`CREATE_FAMILY_SWEPT_BLEND` (FRG-GEO-042) was NOT changed and stays PROVEN: the path's centre lock comes
with the radius label, and a half circle is already a profile it takes - `"-6,0; arc 0,6 6,0"`, closed
back to its start by a straight side. Its profile's local Y is the model's Z when the path lies on Ref.
Level - measured twice on 2026-10-07, once in the owner's Family1 by the session that raised this, once on
SpringProbe: bottom `circle 0,0 5`, top `circle 0,15 5` read Z -5 to 20.

Compiled for Revit 2020 to 2027 on 2026-10-07. **Measured on scratch families only, never Family1.**

**THE ARRANGEMENT - KEPT, in a new scratch family `SpringProof`**, made 2026-10-07 by
`CREATE_FAMILY_DOCUMENT` from *Metric Generic Model* (Revit 2024, session 9240), reached by `--in
SpringProof` on every line, `HERON_CLIENT_ID=ajmal-pc` and `< /dev/null`:

```
fragment --in SpringProof add-family-parameters --write --apply --set "parameterNames=Mean Radius, Wire Radius, Half Pitch, Odd Size" --set kind=length --set instance=false --set parameterGroup=
fragment --in SpringProof set-family-type-values --write --apply --set typeName=Proof --set deleteType=false --set "values=Mean Radius=40; Wire Radius=5; Half Pitch=15; Odd Size=22"
fragment --in SpringProof create-reference-planes --write --apply --set "planes=Pitch=30; Top Turn Start=120" --set axis=height
fragment --in SpringProof load-family --write --apply --set familyPath=<SpringProbe.rfa: a half-turn, planes Coil Base at 0 (Bottom) and Coil Top at 30 (Top)> --set reload=false --set overwriteValues=false
fragment --in SpringProof place-nested-family --write --apply --set family=SpringProbe --set type=SpringProbe --set point=0,0,0 --set "host=Ref. Level"
fragment --in SpringProof filter-elements-by-category --set "category=Generic Models" --set "levelId=Ref. Level" --set includeLinks=false
fragment --in SpringProof move-elements --write --apply --set offset=0,0,30 --expect-from filter-elements-by-category
fragment --in SpringProof lock-nested-family-to-planes --write --apply --set instance=<the nested family> --set "locks=Bottom=Pitch"
fragment --in SpringProof array-family-forms --write --apply --set form=<the nested family> --set count=3 --set "step=Z 60 last"
fragment --in SpringProof place-nested-family --write --apply --set family=SpringProbe --set type=SpringProbe --set point=0,300,0 --set "host=Ref. Level"
fragment --in SpringProof create-family-swept-blend --write --apply --set "workPlane=Ref. Level" --set "path=40,0; arc 0,40 -40,0" --set "bottomProfile=circle 0,0 5" --set "topProfile=circle 0,15 5" --set solid=true
```

Its ids on 2026-10-07 - an unsaved family loses them when Revit closes, and the arrangement is then made
again: half-turn `337ead81-...-00000eb6`, second nested family `...-00000eb4`, array `...-00000ead`, the
array's last copy `...-00000eac`. Every run below is rolled back (`validate --write`); the job file
[`spring-swept-blend-2026-10-07.yaml`](../../tools/jobs/spring-swept-blend-2026-10-07.yaml) carries them.

| # | Run | Look for |
|---|---|---|
| **CU1** | `validate --in SpringProof --negative-in SpringProof label-family-radius --write --set form=<half-turn> --set "parameterName=Mean Radius" --set diameter=false`, negative the same with `parameterName=Odd Size` | `labelled` the path arc's radius reading 40, its centre locked to Center (Front/Back) and Center (Left/Right); the negative refused - *"Nothing in the swept blend is 22 mm in size"*, its sizes listed. **RAN 2026-10-07, both legs ok, rolled back; SIGNED by Ajmal PS - PROVEN** |
| **CU2** | the second route for CU1: `validate ... flex-family --write --setup label-family-radius` with `trials=Mean Radius=60 \| Mean Radius=25`, negative `trials=Odd Size=30` | the FORM moving, not only the label reading (the lesson of PR #402): **RAN 2026-10-07** - 130 x 65 x 25 mm at 60, 60 x 28.53 x 25 at 25, the negative unchanged at 90 x 45 x 25. Written into the CU1 draft's second route |
| **CU3** | CU1 with `Wire Radius` (5), then `Half Pitch` (15) | two radial labels, one per profile; then a plane `Half Pitch` made through the top circle's centre, the centre locked, labelled from Ref. Level reading 15. Measured kept on SpringProbe 2026-10-07 and flexed 136 x 68 x 41 and 56 x 26.63 x 12 mm |
| **CU4** | `validate --in SpringProof --negative-in SpringProof array-family-forms --write --set form=<second nested family> --set count=3 --set "step=Z 60 last"`, negative `form=<the array>` | three nested families up +Z, every copy read back, `members` two group ids in order; the negative refused - neither a form nor a nested family. **RAN 2026-10-07, both legs ok; SIGNED by Ajmal PS - PROVEN** |
| **CU5** | `validate --in SpringProof --negative-in SpringProof lock-nested-family-to-planes --write --set instance=<the array's last copy> --set "locks=Top=Top Turn Start"`, negative `instance=<the array>` | the nested family inside the group found and its Top locked, in an elevation, its extent unchanged; the negative refused - not a nested family. **RAN 2026-10-07, both legs ok; SIGNED by Ajmal PS - PROVEN** |
| **CU6** | the whole spring - TRG_Spring_One Turn and TRG_Spring_Compression Spring, built 2026-10-07 through Heron (61 calls) and saved under `%TEMP%\heron-work\spring` | **MEASURED, rolled back:** the one-turn family flexed 95, 114 and 127 mm across; the main family 95 x 147, 114 x 181 and 127 x 191 mm, base at 0; the arrayed turns, read by a scratch probe because `FLEX_FAMILY` does not read nested families (5b-367), 4, 5, 5 and 8 of them, each one Pitch apart. LOOK at it in a 3D view - the coils should touch nothing and the ends sit flat |
| **CU7** | the signatures | `python brain/heron_validate.py accept label-family-radius --by "Ajmal PS"`, then `array-family-forms` and `lock-nested-family-to-planes` - on the owner's word only. **DONE 2026-10-07 on his word ("sign it")**, fingerprints `90c6543736410662`, `d55056abd9b5cf1e` and `44a4578e0c006726`, each card `proof stands`; any later change under `impl/` makes them stale. A spot-check by eye in Revit is still worth doing |
| **CU8** | CU1, CU4 and CU5 on **Revit 2020** and **2027** | The same answers. Every member used reads the same 2020 to 2027 (api-surface) and compiles on all eight releases; not run there. `PLACE_NESTED_FAMILY` places a level-based nested family only from 2024, so on 2020 to 2023 the spring's one-turn family must be made Work Plane-Based - and whether its copies then leave their plane is NOT measured |

**Routing, measured 2026-10-07 - not proof.** The five new sentences resolve to their fragments by
identity. By ranking, three of the label's lose to `CREATE_FAMILY_SWEPT_BLEND` and *"lock the last copy of
the array to the top plane"* to `ARRAY_FAMILY_FORMS` - listed among the sentences two fragments both want,
none a crossing. `check-intrusion` printed the same as before but for its utterance count.

**Saving the one-turn family before it is loaded** needs `SAVE_DOCUMENT` version 2 (PR #438, merged
2026-10-07, Group CS): version 1 saved the document Heron's own transaction group is open on, and Revit
refused. The spring was built with that branch's fragment, run from a scratch copy before it merged.
