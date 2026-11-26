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
| **QS3** | The locked extrusion alone, offset 0,0,100 | `notModelElements` empty (version 3 refused it there), NOT in `movedIds`; write down which it came back as - `blocked`, `partly`, or refused for the constraint. This is the state the owner's fan body is most likely in |
| **QS4** | The model line (0,0,100), then the symbolic line (100,0,0) | Each moved, neither refused; write down what Category reads on each. It says something about version 4 only where a line reads Category null - otherwise version 3 moved it too; say so |
| **QS5** | **Negative.** In a project - Heron OC Tags - a wall and id 398, the version 3 proof's own no-category negative (read first that it is not a type; pick another if it is), offset 1000,0,0 | Still refused as in version 3, 398 in `notModelElements` - the family exception does not reach a project |
| **QS6** | **Negative - the one that reaches version 4's changed line.** In the scratch family, a free extrusion and an element with NO category that is neither a form nor a line (a form's sketch or sketch plane is the likely one; write down which, and what its Category read), offset 0,0,100 | Refused before anything moves, that element's id alone in `notModelElements`. Both moved means the rule is too wide |
| **QS7** | **Negative.** A free extrusion and the loaded family's own Family id, offset 0,0,100 | Refused, the Family id in `notModelElements`, nothing moved |
| **QS8** | Two extrusions locked to ONE shared reference plane, in one call, offset 0,0,100 - the field case was 31 forms in one call | Write down what each did. One carried by the other is in `unverified` with the family finding (*"carried by a constraint ... move the reference plane"*), never the tag advice, never in `movedIds` |
| **QS9** | A free extrusion with toPoint 0,0,1000, offset left out | In `noLocationPoint`, not moved; the finding says it has no location point (*"a family form"*), not that it is positioned by a curve |
