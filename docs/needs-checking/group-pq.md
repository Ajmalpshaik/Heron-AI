# Needs checking — Group PQ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PQ - the folded pattern-based panel: a tip point pushed off a point's plane, its Offset linked to Tip Depth, and the two divided-surface tools on Revit 2020 - DRAFT until proved (2026-11-22)

**Tracked as GitHub issue [#1272](https://github.com/Ajmalpshaik/Heron-AI/issues/1272) since 2026-11-22** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** the owner is building a twisting fin tower in Revit 2020 and wants its triangle folding-screen
cladding to become possible the pattern-based way too - a triangle panel on a divided mass surface, folded
to a tip whose depth follows a family parameter. Heron could host a point ON another point's plane but not
push one OFF it, and could not link a reference point's Offset to a family parameter. Written 2026-11-22
with no Revit; **none of it has met a Revit.** Every tool below compiles for Revit 2020 to 2027
(`tools/check-fragments-compile.py`).

| Change | Files | Checks |
|---|---|---|
| [`PLACE_ADAPTIVE_POINTS`](../../brain/fragments/place-adaptive-points/fragment.yaml) v2 (DRAFT, was PROVEN v1): `on point <n or id> normal <mm>` - off the host's plane that lies in the panel, along the panel's normal; `on point <n or id> xy\|yz\|xz U,V offset <mm>` - a plane named. Offset reads the number typed; the answer says which side | `place-adaptive-points` `fragment.cs` | PQ1 to PQ4 |
| [`LINK_FAMILY_PARAMETER`](../../brain/fragments/link-family-parameter/fragment.yaml) v6 (DRAFT, was DRAFT v5): a REFERENCE POINT named by id in `forms` - its Offset linked to a family length parameter such as Tip Depth | `link-family-parameter` `fragment.cs` | PQ5, PQ6 |
| `DIVIDE_FAMILY_SURFACE` v2 and `SET_DIVIDED_SURFACE_LAYOUT` v2 - unchanged here; their Group OT and PB rows were written for a Revit 2024 model, and this group gives them a Revit 2020 job | [`tools/jobs/pq-divided-surface-scratch-box-revit2020-2026-11-22.yaml`](../../tools/jobs/pq-divided-surface-scratch-box-revit2020-2026-11-22.yaml) | PQ8 |

**The models: scratch copies, never a working model** - in `%TEMP%\heron-work\pq`: *PQ Tip Panel*, new from
*Metric Curtain Panel Pattern Based.rft*; *PQ Box 2020*, a copy of Revit 2020's `Mass\M_Box.rfa`. Each job
file says how it is made. **Run the job files from the pull request's checkout until it is merged** - the
client sends the fragment code from the checkout it is run in.

| ID | Do this | Pass looks like |
|---|---|---|
| **PQ1** | *Which plane faces the panel.* [`tools/jobs/pq-tip-point-pattern-panel-revit2020-2026-11-22.yaml`](../../tools/jobs/pq-tip-point-pattern-panel-revit2020-2026-11-22.yaml) on *PQ Tip Panel*: `on point 1 normal 300`, `reference`, rolled back; then its by-hand centroid line, `on point <C's id> normal 300`, where C is a point KEPT on the 1,3 diagonal at 0.5 | One point each time, and `Pushed off: ... point 1's <plane> plane, Offset 300 mm`. **Record** which plane Revit's frame gave for a placement point and for a point hosted on a line - the card finds it at run time, and this says what it found. *"no point 1"* means the template's points are not placement points ([BV4](group-bv.md)): record it. *"none of point ...'s planes lies in the panel"* with its three angles is a measurement, not a pass - record the angles. **Negative:** `normal 0` refused, `pointIds` empty, nothing made |
| **PQ2** | *Which side, and Offset as typed.* PQ1's positive | Properties of the new point: **Offset 300** whichever way Revit's plane faces; the finding names the side as *"from which placement points 1, 2, 3, 4 run anticlockwise"* or *"clockwise"* - look from the tip and check the words. **Record** whether the *"Revit's Offset on this plane runs that way"* note was added (Revit's Offset ran against the panel's normal). **Negative:** `normal -300` - Offset reads -300 and the point is on the other side |
| **PQ3** | *It follows.* PQ1's centroid tip KEPT (a DRAFT's one run, asked), then drag point 1 in the Family Editor | C stays on the diagonal and the tip stays over C, square to the panel, 300 mm off. **Negative:** a point placed FREE at the tip's position (`X,Y,Z`) does not move - the difference hosting makes |
| **PQ4** | *The triangle's centroid.* The tile pattern set to Triangle BY HAND (the API cannot set it); edge 2,3 drawn, its mid-point M at 0.5, the median 1 to M drawn, C at 0.6667 on it, then `on point <C's id> normal 300` | The tip over the triangle's centroid - a third of the way from each edge - 300 mm off. **Negative:** `on point <C's id> normal 300` with `placement` refused - a hosted point is a reference point |
| **PQ5** | *Tip Depth drives it.* PQ3's family with a Length parameter *Tip Depth* = 300 added by `ADD_FAMILY_PARAMETERS`; `LINK_FAMILY_PARAMETER` with `forms` the tip's id and `links` `Offset=Tip Depth` | Linked and read back; then Tip Depth 500 in Family Types moves the tip to 500 mm off. **Record** whether Revit lets a hosted reference point's Offset be associated at all - nobody has seen it |
| **PQ6** | *A link Revit would refuse.* The same with a Yes/No family parameter (*Show Tip*) in place of Tip Depth; then `forms` a FREE placement point's id (hosted on nothing, so no Offset) with `Offset=Tip Depth`; then a project in front | The Yes/No link refused naming both parameters and both kinds; the free point refused naming *"Reference Point id N"* - **record** which of the card's two refusals Revit gives; the project refused, `notAFamily` true. Nothing changed in any |
| **PQ7** | *The folded panel on a divided surface - by hand, the owner's eye.* PQ4's family with the corner-to-tip lines (`DRAW_FAMILY_POINT_CURVES`) and a facet per edge (`CREATE_CONCEPTUAL_FORM`), saved and loaded into *PQ Box 2020*; its top face divided (`DIVIDE_FAMILY_SURFACE`, Triangle tile by hand) with this panel as the pattern | Every cell folded, every tip on the same side of the surface, and a Tip Depth change in the panel family, reloaded, moves every tip. **Negative:** a free tip point (not hosted) - the cells break, which is why the tip is hosted |
| **PQ9** | *A plane named, then pushed off it.* A Generic Model Adaptive family with points 1 and 2 at 0,0,0 and 3000,0,0; `on point 1 xy 0,0 offset 250`, then `yz` and `xz` the same, each `reference`, rolled back | A point 250 mm off each plane, Offset 250 each time. **Record**, per plane, whether the *"Revit's Offset on this plane runs the other way"* note was added - which way Offset runs on each of a point's three planes (the card guesses X x Z for `xz`, which is -Y). **Negative:** `on point 1 xy 0,0 offset 0` refused, nothing made |
| **PQ8** | *The divided-surface tools on Revit 2020.* [`tools/jobs/pq-divided-surface-scratch-box-revit2020-2026-11-22.yaml`](../../tools/jobs/pq-divided-surface-scratch-box-revit2020-2026-11-22.yaml) on *PQ Box 2020* | `DIVIDE_FAMILY_SURFACE` passes as [OT17](group-ot.md) says, and `SET_DIVIDED_SURFACE_LAYOUT` as [PB3 and PB4](group-pb.md) say - write each count down there. **Negative:** as those rows say |
