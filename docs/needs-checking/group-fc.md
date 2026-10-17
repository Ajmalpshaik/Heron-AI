# Needs checking — Group FC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group FC - what each element passes through in the links, and a parameter against its thickness: `FIND_CLASHES` version 3 (2026-10-17)

Asked 2026-10-15 on a real project ([row 5b-540](../fragment-issues/section-5b-rows-401-425.md)),
`4355-BHVD-3D-21P60-ST001I`, Revit 2020 - read only, never written to, and not to be used for any row
here: *"check all the cut-out wall width"*. Eighteen opening families `TRG_GEN_CutOut_Rectangular_HVAC`
carried `Wall Width` 255 or 260; three read probes found the architectural link holds no walls there, the
structural link only a 30 mm wall type `WL T0010` inside each opening's box, 93-128 mm off its centre, an
Air Terminal 305 mm deep (`HVAC_VERTICAL_LOUVER`) at twelve louvre openings, and damper actuators
(Electrical Fixtures) of the electrical link in some. No tool could say any of it.

Version 3 of [`FIND_CLASHES`](../../brain/fragments/find-clashes/fragment.yaml) (FRG-QA-003) adds
`categories` (left out: version 2's building list; `all`; or names), `includeHost` (this model too) and
`compareParameter` (a length on each element), and answers in `passedThrough` (each element, everything it
passes through, with category, 'Family: Type', thickness and side), `comparison` (every difference) and
`mismatched` (this model's elements that differ). With the three left out it answers as version 2 did.
The card is **DRAFT**, as version 2 was.

**Already done, 2026-10-17, in a cloud container with no Revit:** compiled on all eight releases 2020-2027
(`python tools/check-fragments-compile.py`, every fragment), the proof job file dry-run (`WOULD RUN`), and
routing measured main against branch - the row says what moved. **Nothing below has run.**

**THE MODELS for every row:** a new, unsaved project from the default metric **Mechanical** template linking
a small SAVED test project `HeronClashLink.rvt` origin to origin - both made as
[`tools/jobs/find-clashes-v3-link-2026-10-17.yaml`](../../tools/jobs/find-clashes-v3-link-2026-10-17.yaml)'s
header says. Never a 4355 model.

| # | Run | Look for |
|---|---|---|
| **FC1** | **NOT RUN - the proof.** `python tools/batch-prove.py tools/jobs/find-clashes-v3-link-2026-10-17.yaml --dry-run`, then without `--dry-run`, after filling in the two duct ids and the host's title. Then read [`brain/proof-drafts/find-clashes.yaml`](../../brain/proof-drafts/README.md), `python brain/heron_validate.py accept find-clashes --by "Ajmal PS"` and set `heron-status: PROVEN` - on his word | positive (D1, 300 wide, through the 200 mm linked wall): `mismatched` holds D1 and `comparison` reads "1 element(s) differ, 0 agree" and "Width 300 mm - 100 mm MORE than HeronClashLink: Walls 'Basic Wall: Generic - 200mm' (id N in the link), wall 200 mm thick, centre 1000 mm to the wall's exterior (or interior) side of the element's centre"; negative (D2, 200 wide, through the same wall): `mismatched` EMPTY and "0 element(s) differ, 1 agree". **A comparison counting D2 as agreeing while its line says another wall thickness is a FAIL whatever the verdict says** |
| **FC2** | **NOT RUN - the second route.** Revit's own Tape Measure across the linked wall's two faces in the host's plan (Tab into the link), and D1's `Width` in Properties; then the wall's flip arrows in the link | 200 mm and 300 mm, and the exterior side FC1 named agrees with the flip arrows. Two routes agree |
| **FC3** | **NOT RUN - any category.** D1's run with `categories` `all`, the link's door at 0,3000 in the wall, and a fourth duct through the door; then the same with `categories` left out | with `all`: that duct's `passedThrough` line names the Doors element as well as the wall, with "N mm deep along the element's run"; with categories left out: the door is NOT named (version 2's building list has no doors) and `linkedMatches`' summary reads "clash with its walls, floors, roofs, ceilings, columns, beams, foundations or stairs" |
| **FC4** | **NOT RUN - a misspelt category.** `categories` "Wals, Doors" | `linkedMatches`' first line and `passedThrough` read "NOTHING WAS LOOKED FOR, in the links or here - 'Wals' is not a category this model knows", `linksSearched` 0, `comparison` "NOT compared". Nothing read for Doors either |
| **FC5** | **NOT RUN - this model.** A 200 mm wall drawn in the HOST from 5000,-4000 to 5000,4000, a duct through it, `includeLinks` false, `includeHost` true, `categories` Walls | "this model: Walls 'Basic Wall: Generic - 200mm' (id N), wall 200 mm thick, ..." and the duct itself never named; `clashes` empty, because `against` was blank |
| **FC6** | **NOT RUN - not a length.** `compareParameter` "Comments" (text), then "Mark" | "'Comments' not a length on 1 element(s)", nothing compared, `mismatched` empty |
| **FC7** | **NOT RUN - one name, two parameters.** A project parameter named "Width" (Length, instance) bound to Ducts beside the built-in one, then FC1's positive | "'Width' NOT READ - 2 parameters share that name on 1 element(s)", never either value (D-54 s3, 5b-203); `mismatched` empty. Remove the parameter afterwards - the project is never saved anyway |
| **FC8** | **NOT RUN - an element with no solid.** A generic model family that is ONLY a void (or the owner's cut-out family, if it is one), placed so its box takes in the linked wall, `includeLinks` true | either its hits read "(box)" and `linkedMatches` says it was tested by its bounding box - or, if Revit gives it no box, it is NOT tested and named so. Write down WHICH: whether a void-only family has a box is not known here |
| **FC9** | **NOT RUN - the job's own family, in the test project.** The owner's `TRG_GEN_CutOut_Rectangular_HVAC` loaded into the host from the company library (never saved out of a 4355 model by Heron), placed in the 200 mm linked wall with `Wall Width` 255; `compareParameter` "Wall Width", `categories` Walls | "Wall Width 255 mm - 55 mm MORE than ... wall 200 mm thick" and a side matching where it was placed. If the family has no solid, FC8's "(box)" too. Whether its "front" is the face its maker meant is read here, not assumed |
| **FC10** | **NOT RUN - the real job, as a READ.** On `4355-BHVD-3D-21P60-ST001I` with the 18 openings selected: `categories` "Walls, Air Terminals, Electrical Fixtures", `includeLinks` true, `compareParameter` "Wall Width". Not a proof (a real project, D-30 wants a test one) - the answer the modeller asked for | the 30 mm `WL T0010` walls reading "wall 30 mm thick", 93-128 mm off each centre as the probes found, `HVAC_VERTICAL_LOUVER` 305 mm deep at the louvre openings, the actuators named, and every Wall Width of 255 or 260 listed as differing. Anything else is a finding for the row |
| **FC11** | **NOT RUN - version 2 unchanged.** Any version 2 call with `categories`, `includeHost` and `compareParameter` all left out - FIND_CLASHES' own version 2 cases in [`tests/cases.yaml`](../../brain/fragments/find-clashes/tests/cases.yaml) | `clashes`, `clashing`, `clear`, `notChecked`, `findings` and `linkedMatches` just as version 2 gave them; `comparison` "No parameter named - nothing compared"; the one new word is a no-solid element tested by its box (FC8) |
| **FC12** | **NOT RUN - Revit 2021 to 2027.** FC1 on any later release. The length check goes through `Definition.GetDataType` from 2022 and `ParameterType` before it, by reflection; only 2020's branch is in FC1 | as FC1 |
