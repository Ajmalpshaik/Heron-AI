# Needs checking — Group CO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CO - family parameters renamed in place, formulas and labels read back with the new names (2026-10-06)

**The owner's PC, Revit 2024, on a scratch family made for the purpose and confirmed by its title - never on
GM_PipeSupport_* or a FamilyN document he has open.** Asked for on 2026-10-06, when the pipe support family
GM_PipeSupport_FloorType1 needed "CC 1 to 2", "CC 2 to 3" and "CC 3 to 4" renamed to "Pipe CC 1 to 2" and so
on, and no tool renamed a family parameter (row [5b-349](../fragment-issues/section-5b-rows-176-200.md)):

- [`add-family-parameters`](../../brain/fragments/add-family-parameters/fragment.yaml) - `ADD_FAMILY_PARAMETERS`, FRG-PAR-022, version 3 (`renameExisting`)

Compiles on all eight releases, 2020 to 2027 (2026-10-06). **Not yet run on a model.**

| # | Check | Expected |
|---|---|---|
| **CO1** | On the scratch family - CC 1 to 2 and CC 2 to 3 (type lengths), Total = CC 1 to 2 + CC 2 to 3, a dimension labelled CC 1 to 2 - renameExisting true, "CC 1 to 2=Pipe CC 1 to 2; CC 2 to 3=Pipe CC 2 to 3", `validate --write`, rolled back | Both renamed; formulasReadBack shows Total = Pipe CC 1 to 2 + Pipe CC 2 to 3; keptLinks shows the label on Pipe CC 1 to 2 |
| **CO2** | The same family renamed, then FLEX_FAMILY setting Pipe CC 1 to 2 to a new length | The labelled plane moves - the label drives under its new name |
| **CO3** | A new name already in the family in other capitals, "CC 1 to 2=cc 2 to 3" | Refused naming it before anything changes; names, formula and label read back unchanged |
| **CO4** | A SHARED parameter renamed | Refused, saying its name comes with its GUID; nothing renamed |
| **CO5** | A new name with an operator (Pipe CC 1-2), a digit first, or a character Revit refuses (Pipe:CC) | Each refused by name, nothing renamed |
| **CO6** | A parameter a nested family's parameter is linked to, renamed | The link read back unchanged in keptLinks - **not yet run**: the scratch family has no nested family |
| **CO7** | The first positive case on Revit 2020 | The same read-back as on 2024 |
| **CO8** | The run records in `brain/proof-drafts/runs/` read and signed | `accept --by` on the owner's word only; until then DRAFT |
