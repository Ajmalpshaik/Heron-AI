# Needs checking — Group BI

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-29 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BI - D-59's fourth batch: six coordination checks look inside linked models when asked (2026-09-29)

**The same shape as [Group BH](group-bh.md), on six coordination checks whose other side is usually in the
architect's or structural link** - `includeLinks` (absent means this model only), `linksSearched` and
`linkedMatches`. Nothing from a link is selected or carried ([row 75](../FRAGMENT-ISSUES.md)); a tool's
pass/fail lists stay this model's answer and what the links show is TEXT. Only model elements are read from
a link, never its views or sheets. Why these six, and what was decided: [row 5b-258](../FRAGMENT-ISSUES.md).

**All six compile on all eight releases and none has been run.** Five were `PROVEN` and went back to `DRAFT`
as version 2 ([D-30](../DECISIONS.md)); `find-clashes` was already `DRAFT`.

**BE1 must pass first.** The model is `Snowdon-scratch_ajmal.al`; the negative case for every row is a model
with no links, checked in Manage Links first.

| ID | Do this | Pass looks like |
|---|---|---|
| **BI1** | `propose-mep-openings` with pipes that cross a wall of the architectural link, `hosts` EMPTY, `allowanceMm=25`, `includeLinks=true` | `proposals` empty, and each crossing in `linkedMatches` with the linked wall's type, id in the link, Fire Rating, hole size and a centre that lands on the wall in a section of THIS model. **Negative:** the same with `includeLinks` absent is refused - *"Both services and hosts are needed"* |
| **BI2** | `find-clashes` with ducts, one through a linked beam, `against` EMPTY, `includeLinks=true` | The link's line reads `1 of N element(s) clash` and names the duct and the beam. **Negative:** ducts clear of the link read `0 of N` |
| **BI3** | `check-ceiling-coordination` on air terminals under ceilings that are only in the link, `tolerance=5`, `includeLinks=true` | `outOfPlane` and `noCeilingAbove` unchanged; each terminal's line gives the gap to the linked ceiling. **Negative:** hide the link in the first 3D view - every device reads `no ceiling above`, and the summary names that view |
| **BI4** | `measure-ceiling-height` on this model's spaces, `includeLinks=true` | Every dictionary unchanged; each space's line gives the linked ceiling's clear height in mm, matching the ceiling's Height Offset From Level in the link |
| **BI5** | `check-room-mep-completeness`, devices = the smoke detectors, one rule "every room needs 1 Fire Alarm Devices", `includeLinks=true` | `shortRooms` unchanged; the link's counts, and each short linked room named. Open one in plan with the link shown. **Negative:** no rules - `Links NOT read` |
| **BI6** | `check-equipment-clearance` on an AHU with a linked column in its front zone, `includeLinks=true` | `blocked` unchanged; the AHU's line names the linked column. **Negative:** a zone overlapping only a linked room gives no line |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the six compile on
2020 to 2027, and every suite passes.
