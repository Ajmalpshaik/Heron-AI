# Needs checking — Group QS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in this
> file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back into the
> register for every tool that reads the register.

## Group QS - MOVE_ELEMENTS version 4: a family's own forms and lines moved in the Family Editor, a type still refused - DRAFT until proved (2026-11-26)

**Tracked as GitHub issue [#1413](https://github.com/Ajmalpshaik/Heron-AI/issues/1413) since 2026-11-26** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** on 2026-11-26, Revit 2020, session 21400, the owner's family `TRG_Mechanical Equipment_Axial Fan_Short
Case` needed its fan body lifted 240 mm to sit on a floor stand. `REPORT_FAMILY_FORMS which=solid` handed on 31
extrusions and blends, and [`MOVE_ELEMENTS`](../../brain/fragments/move-elements/fragment.yaml) answered NOTHING
CHANGED with all 31 in `notModelElements` - version 3's guard (row 5b-1707) refuses anything with no category,
and those forms read Category null in the Family Editor. Version 4 lets a GenericForm or a CurveElement (model
or symbolic line) with no category through in a FAMILY document only; a type and a family are still refused
everywhere ([row 5b-2046](../fragment-issues/row-5b-2046.md), issue
[#1412](https://github.com/Ajmalpshaik/Heron-AI/issues/1412)). The card was PROVEN at version 3; its C# moved, so it is DRAFT until this group is run and signed
(D-30). Its `tests/cases.yaml` holds every case.

**What is done and what is not.** The guard is changed in code and compiles for 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-26). **Never run in Revit.** That `ElementTransformUtils.MoveElement`
moves a form in a family, and that a form's model box reads back there, are expected, not seen; that a line
reads Category null in a family is not seen either - only forms were seen refused.

**The model: scratch, never the owner's fan family** - Revit 2020 (the release the defect was met on). A new
family from `Metric Generic Model.rft`, saved under `%TEMP%\heron-work\qs\`, with two extrusions drawn free of
any reference plane, a third with its top and bottom locked to reference planes, one model line, one symbolic
line in the Ref. Level plan, and any small family loaded into it (Insert > Load Family) so a FamilySymbol exists.
Every run rolled back.

| ID | Do this | Pass looks like |
|---|---|---|
| **QS1** | **Negative, first.** One free extrusion's id and the loaded family's FamilySymbol id in one call, offset 0,0,100 | Refused before anything moves: moved 0, the symbol's id alone in `notModelElements`, and the extrusion's extents unchanged when read back with `REPORT_FAMILY_FORMS` |
| **QS2** | The two free extrusions, ids from `REPORT_FAMILY_FORMS which=solid`, offset 0,0,100 | moved 2, `movedIds` both, `notModelElements` and `unverified` empty - THEN `REPORT_FAMILY_FORMS` reads each 100 mm higher, X and Y unchanged. Forms in `unverified` mean the model box does not read back in a family: a new row, not a pass |
| **QS3** | The locked extrusion alone, offset 0,0,100 | NOT in `movedIds`; write down which it came back as - `blocked`, `partly`, or refused for the constraint. This is the state the owner's fan body is most likely in |
| **QS4** | The model line (0,0,100), then the symbolic line (100,0,0) | Each moved, neither refused; write down what Category reads on each |
| **QS5** | In a project (any Revit sample), a wall and its wall type's id, offset 1000,0,0 | Still refused as in version 3 - the family exception does not reach a project |
