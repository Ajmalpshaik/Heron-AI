# Needs checking — Group BO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group BO - one form's material: linked to a family material parameter, or set to a material, one form at a time (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** One new tool, added the same day as Groups BL to BN because
the `family-creation` skill's own NOT YET line named materials, and nothing could give the body and the neck
of one family different ones: [`set-family-form-material`](../../brain/fragments/set-family-form-material/fragment.yaml)
- `SET_FAMILY_FORM_MATERIAL`, FRG-PAR-028. LINK_FAMILY_PARAMETER links every element of a category at
once; this is the Material row in Properties for the forms named - linked to a family material parameter,
set to one material, or put back to <By Category>.

**What is already known, and it is NOT a proof.** It compiles on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01), and every Revit call it makes reads the same 2020 to 2027.
A parameter's kind is read by reflection - `SpecTypeId.Reference.Material` from 2022, `ParameterType.Material`
before it, gone from 2023 - so **2020 is a different path, not a repeat**. **It has not met a model**, and it
is `DRAFT`. The proof plan is [`tools/jobs/family-form-material-2026-10-01.yaml`](../../tools/jobs/family-form-material-2026-10-01.yaml).

| # | Check | Expected |
|---|---|---|
| **BO1** | Group BM's arrangement plus a material type parameter `Body Material`: the cylinder's id, material `Body Material`; then a second solid, material `Default`; then the first again with `by category` | The first LINKED - its Material row greyed with the associate button on Body Material, LOOKED AT; the second SET to Default, the first still linked; then the first back to <By Category> with its link removed, the answer saying so. **Negative:** `Neck Diameter`, a length, refused as not a material parameter, nothing changed |
| **BO2** | BO1 on **Revit 2020** | The same answers - the reflection path for a parameter's kind is the other branch there |
