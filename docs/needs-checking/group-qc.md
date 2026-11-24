# Needs checking — Group QC

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group QC - CHANGE_ELEMENT_TYPE version 3: every refused element says why - DRAFT until proved (2026-11-24)

**Tracked as GitHub issue [#1327](https://github.com/Ajmalpshaik/Heron-AI/issues/1327) since 2026-11-24**, with [row 5b-2011](../fragment-issues/row-5b-2011.md) - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**LOCAL work - it needs Revit on the owner's PC.** A cloud session cannot run any of it.

**Why:** [row 5b-2011](../fragment-issues/row-5b-2011.md). On 2026-11-24, Revit 2020, a work-plane-based exhaust
louvre asked to take a type of a level-based louvre family came back `refused 1` with no reason. **Version 3** of
[`CHANGE_ELEMENT_TYPE`](../../brain/fragments/change-element-type/fragment.yaml) gives each refused element its
own line in `findings`, from what Revit answers: whether Revit lists the type as valid for it (`IsValidType`,
`GetValidTypes`), both categories, how each family is placed (`FamilyPlacementType`), and Revit's own message
when `ChangeTypeId` throws. What is changed and what is refused is meant to be exactly as version 2. Its C#
changed, so it is **DRAFT** until this group runs and Ajmal PS signs (D-30). The version 2 proof stays on the card
as the record.

**Not yet run anywhere.** Compiled for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud
container, 2026-11-24). **Not known, and what QC2 settles:** whether Revit 2020's `IsValidType` answers false for
a level-based type on a work-plane-based instance, or the refusal comes from somewhere else - write down the
whole line it gives.

**The model: a scratch copy, never the working model** - a sample project with two loadable Air Terminal
families loaded, one work-plane-based and one level-based, saved as `%TEMP%\heron-work\qc\qc.rvt`. Revit 2020
first. Every run is Undone after.

| ID | Do this | Pass looks like |
|---|---|---|
| **QC1** | *Positive - two level-based families.* One instance of a level-based air terminal, `newTypeId` a type of ANOTHER level-based Air Terminals family, Keep | `changed 1`, `refused 0`, the instance reads the new type in Properties, and the line that only the TYPE changed. No refusal line. Undo |
| **QC2** | *Negative - work plane to level.* One instance of the work-plane-based family, `newTypeId` a type of the level-based family (the 2026-11-24 case) | `changed 0`, `refused 1`, and a line naming the element: *"... was refused: Revit does not offer '...' as a valid type for it ..."*, naming each family and how it is placed - work-plane-based and level-based - and the way on (replace-in-place-family, step 6). Nothing in the model changed. If the line names Revit's own message instead, write it down: that is what Revit says, and the card's wording follows it |
| **QC3** | *Regression - a mixed batch.* The QC1 instance and the QC2 instance handed in together, `newTypeId` the level-based type, Keep | `changed 1` (the level-based one), `refused 1` (the work-plane one) with the same line as QC2 - the refusal does not stop the rest of the batch. Undo |

**Signing, once QC1 to QC3 pass:** the card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model.
