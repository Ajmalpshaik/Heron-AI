# Needs checking — Group BY

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group BY - deleting family parameters by name, refused while anything still uses them (2026-10-04)

**The owner's PC, Revit 2024, in an UNSAVED scratch family.** Asked for on 2026-10-04 after two leftover
parameters in a PPR check valve family had to be deleted by hand, because no tool could:

- [`delete-family-parameters`](../../brain/fragments/delete-family-parameters/fragment.yaml) -
  `DELETE_FAMILY_PARAMETERS`, FRG-PAR-033

**What is already measured, and it is not yet signed.** 2026-10-04, Revit 2024 session 8804, scratch
`Family15` made by CREATE_FAMILY_DOCUMENT from Metric Generic Model and never saved: four type lengths
`Heron Del A` 100, `B` 200, `C` 300, `D` with the formula `Heron Del B * 2`; `C` labelling the
dimension between two reference planes 300 apart. Run by `validate --in Family15 --write`, rolled back:
deleting `A` deleted it and read it back gone; deleting `B` was refused quoting D's formula; deleting `C`
was refused saying it labels 1 dimension; `A, C` together was refused whole, `A` not deleted;
`Heron Del Zed` was refused offering the four near names; a project was refused as not a family. Then
KEPT in the scratch family: `A, D` deleted, and REPORT_FAMILY_PARAMETERS read back only `B` 200 and `C`
300. The fragment compiles on all eight releases (2026-10-04).

| # | Check | Expected |
|---|---|---|
| **BY1** | The owner signs the recorded run - `python brain/heron_validate.py accept delete-family-parameters --by "Ajmal PS"` in this worktree | The proof block written; the card promoted to PROVEN |
| **BY2** | The second route: the same family by hand, Family Types, the same parameter, Delete Parameter | Revit's dialog agrees with what this deleted or refused |
| **BY3** | A parameter linked to a form field (Extrusion End, Visible or Material), to a nested family's parameter, and to a connector's size | Each refused naming the link - only the formula and label refusals have been seen in Revit |
| **BY4** | The same on Revit 2020 | Same answers - every call it makes reads the same 2020 to 2027, but no older Revit has run it |
