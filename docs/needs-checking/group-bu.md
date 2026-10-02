# Needs checking — Group BU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-02 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BU - a fire protection answer keeps a project's standards in its own fire record, and its figures need a person with the standards (2026-10-02)

**The owner's PC, any Revit release, two models - and copies of NFPA 13, BS EN 12845, FM Global data
sheet 3-26 and BS 5839-1 for BU2 and BU4 to BU8.** [`heron_fire`](../../brain/heron_fire.py)
asks a project's sprinkler standard and its approving authority once ([D-111](../decisions/D-111.md)) and
keeps them in `%APPDATA%\Heron\knowledge\projects\<key>.fire.json`, beside the HVAC record and never in
it. **What is already known, and it is NOT a proof:** [`tests/test_fire.py`](../../tests/test_fire.py)
section 8 holds the keeping and the separation against stand-in project keys, and the solver against an
independent method. **Nothing has run against the key Revit itself reports, and no figure has been read
against the standard's own text.**

| # | Check | Expected |
|---|---|---|
| **BU1** | Open a model, ask Heron to count something in it, then ask `pipe_schedule` for ten light hazard sprinklers on steel with `sprinkler_standard` and `fire_authority` given; ask again without them | The first answer says *kept for this project*, the second asks nothing and says *recorded for this project*, and one new `.fire.json` file sits under `knowledge\projects\` named by the model's Project Information UniqueId, with the model's `.hvac.json` (if any) unchanged. **Negative:** in a new chat that has read nothing from the model, the first request says *NOT KEPT* and writes no file |
| **BU2** | With the owner's copy of NFPA 13 open, read every row `reference` lists against the edition the office uses, and the 1.2 in `design_area`'s 1.2 sqrt A | Every held figure agrees, or a row in [FRAGMENT-ISSUES](../FRAGMENT-ISSUES.md) names the one that does not. **Negative:** one figure deliberately mistyped in a copy of the table is found by the same read |
| **BU3** | One real sprinkler system's design area, calculated in a listed hydraulic program and by `hydraulic` with the same network | The governing sprinkler is the same and the source pressure and flow agree within the difference the two methods explain (the listed program's velocity-pressure option, its fitting lengths). **Negative:** a pipe given the wrong bore in Heron's copy moves the answer by the amount Hazen-Williams predicts |
| **BU4** | With a licensed copy of BS EN 12845:2015+A2:2026 - the current edition, which no figure here was read against - read `reference en12845_criteria`, `en12845_rules` and `en12845_precalculated` | Every held figure agrees with A2:2026, or a row names the one that does not; the pre-calculated wet rows for OH2 to OH4, left out because two searches disagreed, are read and put in. **Negative:** a dry LH system given to `design_area` with EN 12845 still FAILS |
| **BU5** | With FM Global data sheet 3-26's current revision, read `reference fm_criteria` | HC-1's dry area - left out because both sources gave 1500 ft2, the wet area - is read, and the 30 to 60 ft rows for HC-2 and HC-3 are filled. **Negative:** an FM project's `pipe_schedule` is still refused |
| **BU6** | With BS 5839-1:2025, read `reference bs5839_rules` | The 7.5 m and 5.3 m radii, the 500 mm, the ceiling heights and the call point distances agree with 2025 - in particular whether 2025 kept the 25 m call point reduction, which the sources disputed. **Negative:** `detector_layout` with radius 7.5 m on a 20 x 12 m ceiling still gives 3 detectors |
| **BU7** | With NFPA 13's equivalent length chart and its C and bore adjustments, read `reference equivalent_lengths` and `equivalent_length`'s factors | The fifteen cells held agree, the rest of the chart is read in, and NFPA's own adjustment for another bore is the (bore / schedule 40 bore)^4.87 Heron derives - or a row says it is not. **Negative:** a 45 degree elbow, not read, is still refused until its cell is |
| **BU8** | With NFPA 13's annex example for more than one adjustment, run `design_area` for a dry system with high-temperature sprinklers | The example's area is 2625 ft2 from 2500, each adjustment on the area first selected and added, as Heron now does - if NFPA multiplies them, a row in FRAGMENT-ISSUES says so and the engine changes back. **Negative:** one adjustment alone gives the same area either way |
