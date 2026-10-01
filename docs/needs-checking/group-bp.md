# Needs checking — Group BP

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BP - one form's Visible, angles and extrusion ends linked to parameters; Is Reference and Defines Origin on planes (2026-10-01)

**The owner's PC, Revit 2024 first, then 2020.** Two new tools, added the same day as Groups BL to BO,
because the `family-creation` skill's own NOT YET line named Is Reference, and a part that some types
leave out, a bend whose angle the types choose, and a depth that follows a parameter had no way to be
built on ONE form:
[`link-family-form-parameter`](../../brain/fragments/link-family-form-parameter/fragment.yaml) -
`LINK_FAMILY_FORM_PARAMETER`, FRG-PAR-029 - and
[`set-family-plane-reference`](../../brain/fragments/set-family-plane-reference/fragment.yaml) -
`SET_FAMILY_PLANE_REFERENCE`, FRG-GEO-047.

**What is already known, and it is NOT a proof.** Both compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-10-01), and every Revit call they make reads the same 2020 to
2027. A parameter's kind is read by reflection, so **2020 is a different path, not a repeat**. **The
numbers Is Reference stores are not documented anywhere** - the API's `FamilyInstanceReferenceType`
numbers its values (Left 0 to Top 8, Strong 9, Weak 10, Not a Reference 11) and says only that it
"corresponds to" them - so the tool tries a number, reads the words back, and reports the number that
gave them. **Neither tool has met a model**, and both are `DRAFT`. The proof plan is
[`tools/jobs/family-form-links-2026-10-01.yaml`](../../tools/jobs/family-form-links-2026-10-01.yaml).

| # | Check | Expected |
|---|---|---|
| **BP1** | Group BM's arrangement plus a Yes/No type parameter `Show_Handle`: a second solid's id, links `Visible=Show_Handle` | That form's Visible row greyed with the associate button on Show_Handle, LOOKED AT; the cylinder untouched. Loaded into a test project, a type with Show_Handle cleared shows no second solid. **Negative:** `Visible=Neck Diameter`, a length, refused naming both kinds, nothing changed |
| **BP2** | A Length type parameter `Depth` and an extrusion's id: links `Extrusion End=Depth`, then FLEX_FAMILY at another Depth | The top follows Depth both times, read with REPORT_FAMILY_FORMS. **Then the same on a form whose top face LOCK_FORM_TO_PLANES already locked to a plane** - record what Revit does when a face is held twice; the card says FLEX is where it shows, and this row says what it showed |
| **BP3** | A solid revolve from create-family-revolution and an Angle type parameter `Bend_Angle` holding another angle: links `End Angle=Bend_Angle` | The sweep ends at Bend_Angle, read back; again after a flex. **Negative:** `End Angle=Bend_Angle` on an EXTRUSION's id - refused, the extrusion has no End Angle, listing what can be linked on it |
| **BP4** | Group BM's arrangement: planes `Body Top=Top`, then `Body Top=Strong Reference` | Body Top reads Top, then Strong Reference, in Properties; every other plane as it was. **Write down the numbers each answer reports** - the question this row exists for is whether Revit stores `FamilyInstanceReferenceType`'s numbers or others, and the answer says which it used. **Negative:** `Body Top=Left` - refused, Left goes on a plane facing left or right |
| **BP5** | A plane `Box Left` facing left-right at -300, made with CREATE_REFERENCE_PLANES: planes `Box Left=Strong Reference, origin` | Box Left Strong and Defines Origin Yes; `Center (Left/Right)` reported giving up the origin and reading No in Properties. Loaded into a test project, the family places by Box Left. **Negative:** `Box Left=Center (Left/Right)` while the template's centre plane holds it - refused naming that plane, nothing changed |
| **BP6** | BP1 to BP5 on **Revit 2020** | The same answers - the kind check's reflection takes the other branch there |
