# Needs checking — Group BT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-02 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BT - an HVAC answer asks a project's standards once, and keeps them for that project only (2026-10-02)

**The owner's PC, any Revit release, two models.** [D-111](../decisions/D-111.md): `heron_hvac` asks a
project's governing standards once and keeps them in `%APPDATA%\Heron\knowledge\projects\<key>.hvac.json`,
named by the Project Information UniqueId the open model reports. **What is already known, and it is NOT
a proof:** [`tests/test_hvac.py`](../../tests/test_hvac.py) section 7 holds the keeping, the replacing and
the refusal to guess, against stand-in project keys. **Nothing has run against the key Revit itself
reports**, which is the half that decides which file an answer lands in.

| # | Check | Expected |
|---|---|---|
| **BT1** | Open a model, ask Heron to count something in it, then ask `ventilation` for one office zone with the four standards given; ask again without them | The first answer says *kept for this project*, the second asks nothing and says *recorded for this project*, and one new file sits under `knowledge\projects\`, named by the model's Project Information UniqueId. **Negative:** in a new chat that has read nothing from the model, the same first request says *NOT KEPT* and writes no file |
| **BT2** | Open a second model in the same Revit, say *use this model*, count something in it, and ask `ventilation` again | The four are asked afresh and nothing from the first model's file is used; a second file appears and the first is unchanged. **Negative:** back on the first model, nothing is asked |
