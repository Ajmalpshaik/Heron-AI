# Needs checking — Group BU

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-02 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BU - a fire protection answer keeps a project's standards in its own fire record, and its figures need a person with the standards (2026-10-02)

**The owner's PC, any Revit release, two models - and a copy of NFPA 13.** [`heron_fire`](../../brain/heron_fire.py)
asks a project's NFPA 13 edition and its approving authority once ([D-111](../decisions/D-111.md)) and
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
