# Needs checking — Group IS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-22 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IS - a revolve's radius labelled on Revit 2020, both references checked before Revit is asked: `LABEL_FAMILY_RADIUS` version 5 (2026-10-22)

From row [5b-1318](../fragment-issues/section-5b-rows-401-425.md). Seen on Revit 2020 (session 78736) on
2026-10-22 in the owner's nested floor drain families: `LABEL_FAMILY_RADIUS` version 4, PROVEN on Revit 2024,
threw Revit's own *Invalid number of references* three times on its revolve route - a trap's two outlet sockets
from a made horizontal plane in an elevation, a floor-based host's end disc from a template plane in a plan -
while its extrusion route labelled three sketched circles in the same session. Fixed the same day with no Revit:

| What | Where | What changed |
|---|---|---|
| `LABEL_FAMILY_RADIUS` v5 | [`label-family-radius`](../../brain/fragments/label-family-radius/fragment.yaml) | MODIFY. Before Revit is asked, the plane's reference and the profile line's (or the centre plane's) are read and checked - that each exists and names the element it should; a call with one missing is refused in one sentence naming which, and the release. Each reference's kind is read and said - a surface for a plane, a line for a sketch curve are expected - but another kind is a note, not a refusal: Revit decides. When Revit refuses the dimension with both in hand, it is asked again with each reference read back from its written form, so the answer names the one it would not take as first read; the sentence that reaches the modeller is Heron's, counting the tries made, with Revit's words after it in quotes. A labelled call's answer says what was read of both. The extrusion and swept blend routes are untouched (5b-1318) |

**What is done and what is not.** The library compiles for 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-10-22). Every member version 5 calls reads the same on all eight releases (`tools/api-surface --members`,
2026-10-22), so the fault is in what Revit 2020 makes of the two references, not in a call that moved.
**Nothing here has run in Revit.** `label-family-radius` is DRAFT, so `revit_change` refuses it until IS7 is
signed; version 4's proof stays in the card as a record. **Which of the two references Revit 2020 will not
take is NOT known** - version 5 does not assume one; IS1's finding line is the measurement. A DRAFT runs
through `python brain/heron_validate.py` (`--write`, rolled back), never through `revit_change` (D-117).
The fragment's C# is read live - no restart.

**The family.** A scratch family, never a working one: a new family from Revit 2020's *Metric Generic
Model.rft*, saved as *Heron IS Radius.rfa* in `%TEMP%\heron-work\group-is`. In it, through `revit_change`
with the PROVEN tools: `ADD_FAMILY_PARAMETERS` - Length instance parameters *Outlet Radius*, *Outlet Disc
Radius* and *Bell Radius*, and *Ring Radius*; `SET_FAMILY_TYPE_VALUES` - *Outlet Radius* 41, *Outlet Disc
Radius* 41, *Bell Radius* 60, *Ring Radius* 200; `CREATE_REFERENCE_PLANES` - a horizontal plane *Outlet
Axis* at Z -140, made in the Front elevation; `CREATE_FAMILY_FORMS` - four solid revolves, each row's id
kept from the answer: **A** *"56,-140; 56,-99; 118,-99; 118,-140"* about *"56,-140 118,-140"* on Center
(Front/Back), the trap's outlet socket of the day; **B** *"0,300; 60,300; 60,500; 0,500"* about *"0,300
0,500"* on Center (Front/Back), version 4's own proof case; **C** *"1000,300; 1060,300; 1060,500; 1000,500"*
about *"1000,300 1000,500"* on Center (Front/Back), no plane through its axis; **D** sketched on *Outlet
Axis*, profile X,Y *"170,0; 172,0; 172,41; 170,41"* about *"170,0 172,0"*, the floor-based host's end disc
of the day. Run every row on **Revit 2020** first, then IS5 on the newest Revit on the PC.

| # | Run | Look for |
|---|---|---|
| **IS0** | *Negative FIRST.* `validate --in "Heron IS Radius" label-family-radius --write --set form=<C> --set "parameterName=Bell Radius" --set diameter=false` | Refused - *"No reference plane runs through the revolve's axis"*, naming `CREATE_REFERENCE_PLANES`; `labelled` empty, `valueMm` 0; nothing locked, Undo unchanged |
| **IS1** | *The fault's own case.* `--set form=<A> --set "parameterName=Outlet Radius" --set diameter=false` | **EITHER** `labelled` - a straight dimension from *Outlet Axis* to the profile line 41 mm out reading **41**, *"Its axis is locked to "Outlet Axis""*, and the finding *"Its two references, read on Revit 2020 before the dimension was asked for: the plane "Outlet Axis" gave a surface reference (...); the profile line gave a linear reference (...)"* - **copy that finding into the run record whole**, written forms included; **OR** nothing labelled, in Heron's own sentence - *"On Revit 2020 the profile line gives no reference ..."* or *"Revit 2020 would not take one of the two references ..."* - naming the plane or the line, what was read of both, whether the axis was locked, and Revit's words after it in quotes. **Never** *"Revit refused to label ... Invalid number of references"* alone: that sentence reaching the record reopens 5b-1318. If the answer says Revit took the dimension *only once ... read again from its written form*, the reference it names is the one 2020 would not take - the row's question, answered by Revit. **Look** in the Front elevation: Edit Revolve shows the dimension in the sketch |
| **IS2** | *Second route for IS1, when labelled.* `validate ... flex-family --write --setup label-family-radius` with IS1's values and `trials=Outlet Radius=60 \| Outlet Radius=30` | The revolve MEASURED **120** and **60** across (twice the radius), the axis still at Z -140 - the FORM moving, not only the label reading; rolled back |
| **IS3** | *The plan case.* `--set form=<D> --set "parameterName=Outlet Disc Radius" --set diameter=false` | As IS1, from *Center (Front/Back)* in the Ref. Level plan, 41 mm out. Either outcome copied whole |
| **IS4** | *Version 4's own case on 2020.* `--set form=<B> --set "parameterName=Bell Radius" --set diameter=false` | As IS1, from *Center (Left/Right)* 60 mm out reading **60**; then the flex *Bell Radius=300 \| Bell Radius=40* MEASURED 600 and 80 across - what row 5b-305 measured on 2024 |
| **IS5** | IS1 and IS4 again on the **newest Revit on the PC** (2024 or later), in the same scratch family opened there (upgraded, never saved back) | Labelled as version 4 did on 2024, the finding reading both references by kind - **copy the kinds it reads**: they are the first measurement of what `ElementReferenceType` a plane's and a sketch line's reference carry, which nothing here records yet. **If version 5 refuses on 2024 what version 4 labelled there, the check read a reference as missing that Revit takes - the check is wrong, not Revit** - narrow it, reopen 5b-1318, nothing signed |
| **IS6** | *The torus ring, whose centre plane is made at the label.* A solid revolve **E** *"circle 200,0 20"* about *"0,-100 0,100"* on Center (Front/Back); `--set form=<E> --set "parameterName=Ring Radius" --set diameter=false` | A plane *Ring Radius* made through the circle's centre and checked like the others; then either outcome as IS1, the made plane named in the reference line. Flex *Ring Radius=400* MEASURED 840 across (row 5b-306's number on 2024) |
| **IS7** | The signature, on the owner's word only, after IS1, IS3 and IS4 have run with IS0's negative and IS5 agrees | `python brain/heron_validate.py accept label-family-radius --by "Ajmal PS"` - PROVEN again, at version 5 |

**Then** close row 5b-1318 with IS1's finding line copied whole: it says which reference Revit 2020 would not
take, or that version 5 labels there as 2024 does. IS1 run on 2020 is also [BM7](group-bm.md)'s answer for the
revolve route. A row that fails reopens its row; nothing in a row changes to make it pass.

### Results - 2026-10-22 night, the owner's laptop: Revit 2020.2.1 (20.2.1.1), session 60544 - NOT the rows above

**None of IS0 to IS7 was run:** the scratch family above needs kept build steps, and the owner was asleep with
*Ask to Keep* on. What ran instead is evidence for IS1's question, on Revit 2020's own library families copied to
`%TEMP%\heron-work\night-1022\families`, each call a rolled-back `validate --write` whose two setup steps
(`add-family-parameters`, `set-family-type-values`, both PROVEN) made a TYPE length *N22 Radius* and set it
inside the same group. Nothing was kept.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| IS1-like | 2020 | *N22 M_Pendant Light - Hemisphere* (`M_Pendant Light - Hemisphere.rfa`): the canopy, `solid revolve 85a96555-88e2-46b1-8ccb-716595067a3b-00000773 (on Light Axis): X -76.2 to 76.2, Y -76.2 to 76.2, Z 2377.5 to 2438.4 mm`, *N22 Radius* 76.2. Threw, nothing kept - the finding whole: *"Nothing was labelled. Revit 2020 would not take one of the two references for the radius of the revolve, from "Light Axis" to its profile line 76.2 mm out, asked 4 times (as read, with the plane's read back, with the profile line's read back, with both read back) with both read before the call: the plane "Light Axis" gave a none reference - not the surface a straight dimension is expected to take (85a96555-88e2-46b1-8ccb-716595067a3b-0000071c); the profile line gave a linear reference (85a96555-88e2-46b1-8ccb-716595067a3b-00000775:0:LINEAR). Its axis is locked to "Light Axis". Which of the two it refused Revit does not say; its words were "Invalid number of references.". NOTHING from this call was kept."* The same with *N22 Radius* 1: *"Nothing was labelled. Nothing in the revolve's profile is 1 mm in size ... its lines alongside the axis lie 0, 76.2 mm from it - set the parameter to one of those first."* | **FAIL** - 5b-1318's fault, reproduced in a library family |
| circle route | 2020 | *N22 M_Downlight - Recessed Can*: an EXTRUSION, `solid extrusion c34a5d43-c948-47d7-8db8-cd73aa6d2c43-000008bf`, *N22 Radius* 100 in type *203mm Incandescent - 120V*: labelled, `valueMm` 100; 1 mm refused, `valueMm` 0. Version 5 left this route untouched, so it says nothing about IS1 | **PASS**, not IS's route |

**What the IS1-like run adds.** Version 5 read the two references before asking Revit, and on Revit 2020 the
plane's reference reads as kind **none**, not a surface - while the profile line's is a line. That points at the
plane, but Revit's own words do not name either, and the four tries (each reference also read back from its
written form) were all refused. Not checked: whether *Light Axis* is a reference plane or a reference line in that
family (the revolve's work plane and its locked axis both carry the name), and what kind the same plane's
reference reads on Revit 2024. Row 5b-1318 stays open; `label-family-radius` stays DRAFT.

### Results - 2026-10-23, the owner's laptop: Revit 2024.3 (24.3.60.12), session 83588 - IS5's question, on the same revolve

The same library copy, *N22 M_Pendant Light - Hemisphere*, opened in Revit 2024 (upgraded in memory, never saved);
the same rolled-back `validate --write` with the same two setup steps and *N22 Radius* 76.2.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| IS5-like | 2024 | The canopy revolve `85a96555-88e2-46b1-8ccb-716595067a3b-00000773`: **labelled** - *"The radius of the revolve, from "Light Axis" to its profile line 76.2 mm out, labelled "N22 Radius" inside the form's sketch, reading 76.2 mm. Its axis is locked to "Light Axis"."* - and the finding whole (read by running the card's own source, rolled back, with one joined string added): *"Its two references, read on Revit 2024 before the dimension was asked for: the plane "Light Axis" gave a none reference - not the surface a straight dimension is expected to take (85a96555-88e2-46b1-8ccb-716595067a3b-0000071c); the profile line gave a linear reference (85a96555-88e2-46b1-8ccb-716595067a3b-00000775:0:LINEAR)."* *N22 Radius* 1: refused, *"Nothing in the revolve's profile is 1 mm in size ..."*, `valueMm` 0 | **PASS** on 2024 |

**What 2020 and 2024 together say.** The two references read the same on both releases - the plane's kind *none*,
the profile line's *linear*, the same written forms - and Revit 2024 takes the dimension while Revit 2020 refuses it
with *"Invalid number of references."*. So version 5's check reads nothing wrong (IS5's warning does not apply:
it refused nothing 2024 takes), and the fault is in what Revit 2020 accepts for this pair, not in the references
Heron hands it. The run record and draft for the 2024 pass are in the night worktree's `brain/proof-drafts/`
(`label-family-radius`, Revit 2024) - a proof on 2024 only, which leaves row 5b-1318's 2020 revolve fault standing.

### Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - the rows above, in their own scratch family

**The family.** *Heron IS Radius.rfa* in `%TEMP%\heron-work\group-is`, made from Revit 2020's *Metric Generic Model.rft*
by `CREATE_FAMILY_DOCUMENT`, built as the section above says through `revit_change` with PROVEN tools - *Outlet Axis* at Z
-140, the instance lengths with their values, and revolves A to E in one `CREATE_FAMILY_FORMS` call, row file
`%TEMP%\heron-work\group-is\forms.txt` - plus two things the plan did not ask for, for the route a bolt needs: an
EXTRUSION **F**, *"circle 2000,0 10"* on Ref. Level from 0 to 100, and the lengths *Shaft Radius* 10 and *Shaft Diameter*
20. The ids: A `...0a83`, B `...0a8b`, C `...0a93`, D `...0a9b`, E `...0aa3`, F `...0aa9` (prefix
`b26ea862-ec61-474f-bf8a-eb580e5b8bff-00000`). Every label below is `validate --write`, rolled back. Each form's own
bounding box was read before and after a size change by a copy of the card in the session's scratchpad that sets the
parameter after the label, regenerates and reads every `GenericForm` - because `FLEX_FAMILY` reads the family as one box.

| # | What came back | State |
|---|---|---|
| IS0 | C: *"Nothing was labelled. No reference plane runs through the revolve's axis ... make a reference plane through the axis first (CREATE_REFERENCE_PLANES), then label."* | **PASS** |
| IS1 | A: *"Nothing was labelled. Revit 2020 would not take one of the two references for the radius of the revolve, from "Outlet Axis" to its profile line 41 mm out, asked 4 times ... the plane "Outlet Axis" gave a none reference ... the profile line gave a linear reference (...:0:LINEAR). Its axis is locked to "Outlet Axis". Which of the two it refused Revit does not say; its words were "Invalid number of references."."* - the row's second outcome, Heron's own sentence | **PASS** (refused - 5b-1318 stands on 2020) |
| IS2 | Not run: IS1 labelled nothing, so there is nothing to flex | NOT RUN |
| IS3 | D: the same sentence from *"Center (Front/Back)"*, 41 mm out | **PASS** (refused) |
| IS4 | B: the same sentence from *"Center (Left/Right)"*, 60 mm out. Its flex is not run - nothing labelled | **PASS** (refused); flex NOT RUN |
| IS5 | Not run in this family. Its question - does version 5 refuse on 2024 what version 4 labelled there - was answered on 2026-10-23 above (*IS5-like*, Revit 2024.3, a library revolve: labelled). The change below does not touch that route | from 2026-10-23 |
| IS6 | E: **labelled** - *"from "Center (Left/Right)" to a new reference plane through the circle's centre, 200 mm out ... reading 200 mm"* - and Ring Radius=400 grew E to X -420..420, **and moved C, D and F 200 mm with the made plane** (C X 940..1060 -> 1140..1260, D 170..172 -> 370..372, F 1990..2010 -> 2190..2210). `FLEX_FAMILY` alone said *"allHeld true"* | **FAIL** - row [5b-1333](../fragment-issues/section-5b-rows-401-425.md) |
| IS6 again | *After the 5b-1333 change:* *"Nothing was labelled. A ring's distance from the revolve's axis is not labelled by this tool for now ... (row 5b-1333). Label it by hand in Edit Revolve, then change the size once and check that nothing else moved."* | **PASS** (refused) |
| F, radius | *"The radius of the extrusion's sketched circle (radius 10 mm), labelled "Shaft Radius" inside the form's sketch, reading 10 mm."* Shaft Radius 25, 6 and 400: F's X edge at 2025, 2006, and F X 1600..2400 Y -400..400 - **only F moved** | **PASS** |
| F, diameter | *"The diameter of the extrusion's sketched circle (radius 10 mm), labelled "Shaft Diameter" ... reading 20 mm."* Shaft Diameter 48, 12 and 400: F's X edge at 2024, 2006, and F X 1800..2200 - **only F moved** | **PASS** |
| E, tube | With *Tube Radius* 20 made in the same rolled-back group: *"The radius of the revolve's profile circle (radius 20 mm), labelled "Tube Radius" inside the form's sketch, reading 20 mm."* Tube Radius=40: E X -240..240, Z -40..40 - **only E moved** (400 was refused by Revit itself, *"The profile must not cross the axis of revolution."* - a 400 mm tube on a 200 mm ring) | **PASS** |

All of these were run again on the changed code the same day and read as above. **The proof draft's positive is F's
radius and its negative IS0** - the route this card is proved for on Revit 2020 is a round extrusion's circle, by radius
or diameter, and a revolve's tube; a revolve's radius to a profile line is refused there by Revit (5b-1318), and the
ring by Heron (5b-1333). Fingerprint `aa807d3d07693727`, second route NOT ESTABLISHED.
