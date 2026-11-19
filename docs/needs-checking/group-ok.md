# Needs checking — Group OK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OK - reference planes on a coloured subcategory: CREATE_LINE_STYLE version 3 and SET_FAMILY_PLANE_REFERENCE version 2, DRAFT until proved (2026-11-17)

**Why:** the owner's standing rule of 2026-11-17 - in every family the outline and centre reference planes
stay on Reference Planes (green by default) and every other plane goes on a gray subcategory, *"Secondary
References"*, RGB 128,128,128. No tool made a subcategory of Reference Planes or set a plane's Subcategory
([row 5b-1849](../fragment-issues/row-5b-1849.md)). Two PROVEN cards were widened, never a third made:

- [`create-line-style`](../../brain/fragments/create-line-style/fragment.yaml) **version 3** - optional
  `parentCategory` (absent `none`): a subcategory under any category that takes one, Reference Planes above
  all. Left out, version 2 to the letter.
- [`set-family-plane-reference`](../../brain/fragments/set-family-plane-reference/fragment.yaml) **version 2** -
  optional `subcategories` (per plane) and `otherPlanesSubcategory` (every plane that is not an outline or
  centre plane), each plane's Subcategory read before and after; `planes` became optional.

Both are **DRAFT again at a new version, their old proof blocks kept on the cards as the record of what they
proved** ([D-30](../DECISIONS.md)). Both compile on all eight releases (2026-11-17, cloud container, no
Revit). **Nothing here has run in Revit.** The proof is the owner's, on his PC.

**The rig, every row:** a new family from the metric *Generic Model* template, saved as a scratch file. Run
in this order; each row's setup is the row before it. Everything runs inside the rolled-back run unless the
row says *kept*.

| Row | What to check | Pass when |
|---|---|---|
| **OK1** | *`create-line-style` v3 - the subcategory is made.* `styleName=Secondary References`, `colour=128,128,128`, `weight=0`, `patternName=`, `parentCategory=Reference Planes`. **Negative:** `parentCategory=No Such Category` | Positive `madeNew true`, findings *"Made subcategory "Secondary References" of Reference Planes: colour 128,128,128, ..."*, and Manage > Object Styles > Annotation Objects shows it gray. Negative refused *"This family has no category named "No Such Category""*, nothing made |
| **OK2** | *`create-line-style` - nothing older moved.* Version 2's own positive case unchanged (a line style with `parentCategory` not said); then `parentCategory=Reference Planes` with `familyCategory=true` | First: a line style under Lines, as the kept proof read. Second: refused *"Both familyCategory and parentCategory ... only one may"*, nothing made |
| **OK3** | *`set-family-plane-reference` v2 - what Revit STORES for "Reference Planes itself" is not documented.* With OK1 kept and a plane `Insert` (CREATE_REFERENCE_PLANES): `subcategories=Insert=Secondary References`, then `subcategories=Insert=Reference Planes` | Each call `onSubcategory 1` and Properties > Subcategory reads so. The second's finding *"Reference Planes itself was written as ..."* is the answer - **record which value it names**, and on which release |
| **OK4** | *The owner's rule, in one call.* Planes `Box Left`, `Box Right`, `Insert` made; `planes=Box Left=Left; Box Right=Right`, `otherPlanesSubcategory=Secondary References`. **Negative:** `otherPlanesSubcategory=No Such Subcategory` | Positive: `Insert` (and every other plane that is not an outline or centre plane) `Subcategory Reference Planes -> Secondary References`; `Box Left`, `Box Right`, `Center (Left/Right)`, `Center (Front/Back)` named in findings as left on Reference Planes; gray and green in Ref. Level; every plane's Is Reference and Defines Origin read back as asked. Negative refused, listing the subcategories and naming CREATE_LINE_STYLE, **no plane's Subcategory changed** |
| **OK5** | *`set-family-plane-reference` - version 1 unchanged.* Its first kept case: `planes=Box Left=Left; Box Right=Right`, nothing else | `changed 2`, `onSubcategory 0`, `planeReport` as the kept proof read it, every plane's Subcategory as before |
| **OK6** | *The method.* A family built end to end through `heron_method family-creation`, from a few sizes | Step 5 makes Secondary References once and puts every other plane on it; the outline and centre planes stay green; a plane a later step makes (a hexagon's side planes) is put on it too |

**A second route for both:** select each plane and read Subcategory in Properties; Manage > Object Styles
by eye; one Ctrl+Z takes each call back in a single step.

**Then:** the owner signs each card from its draft ([fragment-proving skill](../../.claude/skills/fragment-proving/SKILL.md)),
and OK3's answer is written into `set-family-plane-reference`'s compatibility note.
