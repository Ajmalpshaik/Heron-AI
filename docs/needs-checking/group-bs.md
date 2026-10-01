# Needs checking — Group BS

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BS - the family's own settings (Work Plane-Based, Shared, Always Vertical) and the office's shared parameters in a family (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** Two new tools, asked for by the owner on 2026-10-01 after the
family stack was reviewed for gaps: [`set-family-settings`](../../brain/fragments/set-family-settings/fragment.yaml)
- `SET_FAMILY_SETTINGS`, FRG-DOC-036 - because Group BR's own advice is to make a nested family Work
Plane-Based and nothing could, and
[`add-family-shared-parameters`](../../brain/fragments/add-family-shared-parameters/fragment.yaml) -
`ADD_FAMILY_SHARED_PARAMETERS`, FRG-PAR-031 - because a project tags and schedules only SHARED parameters, and
ADD_FAMILY_PARAMETERS makes family ones.

**What is already known, and it is NOT a proof.** Both compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01), and every Revit call they make reads the same 2020 to 2027
apart from the shared parameter call, which takes a BuiltInParameterGroup to 2024 and a ForgeTypeId from 2022 -
picked by reflection, so **2020 is a different path, not a repeat**. **Which categories carry each switch, and
when Revit holds Always Vertical read-only, are Revit's** and are read at run time. The shared parameter tool
only READS the office's file and never sets or writes it. **Neither has met a model**, and both are `DRAFT`. The
proof plan is [`tools/jobs/family-settings-shared-2026-10-01.yaml`](../../tools/jobs/family-settings-shared-2026-10-01.yaml).

| # | Check | Expected |
|---|---|---|
| **BS1** | Group BM's family: settings `Work Plane-Based=yes; Shared=yes` | Both read Yes in Family Category and Parameters, LOOKED AT; the answer lists anything Revit changed on its own. **Negative:** `Work Plane-Based=maybe` - refused, nothing changed |
| **BS2** | Then `Always Vertical=no`, and again with Work Plane-Based set back to No first | **Record when Revit holds Always Vertical read-only** - the card says only that it belongs to a Work Plane-Based family |
| **BS3** | Revit's shared parameter file set to one holding a Text parameter `Heron Test Tag`: parameterNames `Heron Test Tag`, type, group `Identity Data` | Added, shared, with the file's GUID - Family Types shows the shared marker; a tag in a test project reads it once the family is loaded. **Negative:** `Not In The File` - refused naming the file and the close names, nothing added |
| **BS4** | BS3 with NO shared parameter file set in Revit | Refused - set the office's file in Manage > Shared Parameters - and the setting still empty afterwards |
| **BS5** | BS1 and BS3 on **Revit 2020** | The same answers - the shared parameter call takes the BuiltInParameterGroup overload there |
