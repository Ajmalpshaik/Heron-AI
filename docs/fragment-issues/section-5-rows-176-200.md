# Fragment issues — section 5, rows 176 to 200

> Rows 176 to 200 of section 5 of [the register](../FRAGMENT-ISSUES.md), in their own file since 2026-09-23 so that
> they can be read alone. **The section's own rules are on that page, under its heading.** A new
> row goes at the end of the section's last file, whatever its number, and the next run of
> [`tools/split-register.py`](../../tools/split-register.py) moves it to the file its number
> belongs to. [`tools/register-text.py`](../../tools/register-text.py) reads every row back into
> the register for every tool that reads it, so a row here is seen exactly as it was seen there.

| # | Defect | State |
|---|---|---|
| **176** | FOUR OF THE OWNER'S OWN QUESTIONS THAT ASK FOR NO CHANGE ARE ANSWERED BY SOMETHING THAT CHANGES THE… — [full row](../fragment-issues-archive/proving-defects-176-200.md#row-176) | **MOVED TO ISSUE [#954](https://github.com/Ajmalpshaik/Heron-AI/issues/954) 2026-11-16** |
| **177** | CREATE_FAMILY_EXTRUSION AND A LINKED EXTRUSION END DISAGREED ABOUT WHICH SIDE OF "PORT RIGHT" A… — [full row](../fragment-issues-archive/proving-defects-176-200.md#row-177) | **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** |
| **178** | **SET_FAMILY_TYPE_VALUES CANNOT WRITE A MATERIAL PARAMETER, SO A LINKED "BODY MATERIAL" STAYED `<none>` AND THE FAMILY SHOWED GREY.** Found 2026-10-04 in TRG_PLMB_VLV_PPR Check Valve Inline_CVI_R0 (Revit 2024). A type Material parameter `Body Material` was added and SET_FAMILY_FORM_MATERIAL linked eight forms to it. `values=Body Material=TRG_PPR_Green` was refused: *"holds a kind this does not write (autodesk.spec.aec:material-1.0.0) - a material, an area or a flow is set in the Family Types dialog for now"*. Ajmal, asked to assign it, could not see any material. The link was dropped for a fixed `TRG_Copper` on each form, losing per-type material control in the project. **Fix when owned:** let SET_FAMILY_TYPE_VALUES write a material parameter by material name (refused when the name is missing or matches twice), reading it back as the material's name. | **FIXED 2026-10-06 as row 5b-341 - a duplicate.** `SET_FAMILY_TYPE_VALUES` version 2 writes a material parameter by the material's name, refuses a name the family does not hold naming the three closest, and reads it back; signed by Ajmal PS on Family3 ([Group CI](../needs-checking/group-ci.md) CI1 to CI4), and PROVEN again at version 3. Was: **OPEN - recorded, not fixed.** |
| **179** | NO TOOL WAS FOUND THAT SETS THE DETAIL LEVELS OF A NESTED FAMILY INSTANCE, SO A HANDWHEEL STILL… — [full row](../fragment-issues-archive/proving-defects-176-200.md#row-179) | **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** |
| **180** | LABEL_FAMILY_RADIUS REFUSES A SECOND LABEL ON ONE PARAMETER, SO EVERY SYMMETRIC FAMILY NEEDS TWIN… — [full row](../fragment-issues-archive/proving-defects-176-200.md#row-180) | **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** |
