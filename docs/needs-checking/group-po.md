# Needs checking — Group PO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PO - ADD_FAMILY_CONNECTOR version 7: only a face that faces out, and at the point given, counts - DRAFT until proved (2026-11-21)

**Tracked as GitHub issue [#1231](https://github.com/Ajmalpshaik/Heron-AI/issues/1231) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** GitHub issue [#1029](https://github.com/Ajmalpshaik/Heron-AI/issues/1029), [row 5b-1920](../fragment-issues/row-5b-1920.md).
On 2026-11-19, Revit 2020, a fixed pump family's discharge flange was refused twice: on the plane at the raised
face's top, *"2 solid faces lie on "Discharge Face" (facing up, up)"* - the second the top of four lid screws about
1 m away at the same height; on the plane at the flange top, *"(facing up, down)"* - the second the raised face's own
underside. Version 6 (Group OT, OT15 and OT16) added `form=` and `facing=`, but with both left out it still counted
every face on the whole plane. [`ADD_FAMILY_CONNECTOR`](../../brain/fragments/add-family-connector/fragment.yaml)
**version 7** counts only a face that faces out of the family's solids, and - given `at=`, the connector's centre in
mm - only a face whose centre lies within 1 mm of it; and it never puts a connector at a face's centre where another
solid covers that point on the side the face points. Its C# changed, so it is **DRAFT** until this group runs and
Ajmal PS signs (D-30). The version 5 proof stays on the card as the record.

**Not yet run anywhere.** Compiled for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, in a cloud container,
2026-11-21). **This group replaces OT15 and OT16**, which were written for version 6: run these instead.

**The model - Heron builds it, on Revit 2020 first, and it is not the owner's own work.** A scratch family from
Revit 2020's `Metric Pipe Accessory.rft`, saved as `%TEMP%\heron-work\po\po-pump.rfa`: a flange disc, extrusion
Z 870 to 882, 200 mm across, centred at X 0, Y 0; a raised face on it, extrusion Z 882 to 884, 150 mm across, same
centre; a small box *Lid Screw* at X 1000, Y 0, 20 x 20 mm, Z 864 to 884 - each its own extrusion, joined to nothing;
reference planes *Discharge Face* (Z 884) and *Discharge Flange Top* (Z 882); a Length parameter *Discharge Dia* =
150. Every run
is ROLLED BACK. While the card is DRAFT, `revit_change` refuses it ([D-117](../decisions/D-117.md)): each is run
through the proving client, a preview.

| ID | Do this | Pass looks like |
|---|---|---|
| **PO1** | *Negative first - a face that faces into the body.* `facePlane=Discharge Flange Top`, system *Domestic Cold Water*, `sizeParameters=Discharge Dia`, nothing else | Refused, nothing made: the answer names the raised face extrusion's id as covering the flange disc's top at its centre, and asks for the plane of its outer face. The raised face's underside is not offered as a face to choose - it faces into the disc. Version 6 refused it *"2 solid faces ... (facing up, down)"*. Check in the family that no connector was left |
| **PO2** | *Negative - a point that names no face.* `facePlane=Discharge Face`, the same system and size, `at=500, 500, 884`; then `at=0, 0, 900`; then `at=150, 0` | First: *"No solid face centred at the point given in at= lies on "Discharge Face""*, listing the raised face and the screw top with their ids and centres. Second: the point is 16 mm off the plane. Third: three numbers asked for. Nothing made each time - never the nearest face |
| **PO3** | *Positive - a face that faces out is still taken with nothing given.* The screw moved down by hand (Z 860 to 880), so only the raised face's top lies on *Discharge Face*. `facePlane=Discharge Face`, the same system and size, nothing else; then the screw put back (Z 864 to 884) | A round pipe connector 150 mm at X 0, Y 0, Z 884, pointing up; no *"facing into"* finding. Version 6's answer for the same call |
| **PO4** | *Positive - at= settles the issue's case.* `facePlane=Discharge Face`, the same system and size, nothing else; then the same with `at=` the raised face's centre exactly as that refusal lists it (*X 0, Y 0, Z 884 mm*), then as three bare numbers | First refused: *"2 solid faces lie on "Discharge Face" (facing up, up)"*, each face with its form id and centre, asking for form= or at=. Second: the connector 150 mm at the raised face's centre, pointing up, and *"1 other face(s) lie on "Discharge Face" with their centre more than 1 mm from the point given in at=, and were left out"* - the screw's top |
| **PO5** | *Record - how long the face test takes.* PO4's second call on the issue's own transformer or pump family, if one is to hand (a scratch copy, never the owner's working file), else on po-pump | The time the call took, written down. Each face on the plane looks out from up to 37 points (a 5 x 5 grid and twelve triangle middles) against every solid of the family near it; nobody has measured that on a real-size family |
| **PO6** | *Negative and positive - a box top only PART covered (found in review).* In po-pump, a 600 x 400 box on its own (X 2000), its top on plane *Box Top*, with a 400 x 300 neck centred on it, the neck's top on *Neck Top*. `facePlane=Box Top`; then `facePlane=Neck Top` | First refused, the neck named as covering the box top's centre - and **not** *"No solid face that faces out of the body"*: the box top is open round the neck. Second: the connector on the neck top, with no *"facing into"* finding |
| **PO7** | *Negative - a bored ring under a bored raised ring (found in review).* In a second scratch family: a flange ring Z 870 to 882, 200 across, bore 150; a raised ring on it Z 882 to 884, 170 across, bore 150; each its own extrusion of two circles; plane *Discharge Flange Top* at Z 882. `facePlane=Discharge Flange Top`, the same system and size | Refused, nothing made, the raised ring named as covering the port. Never a connector at Z 882 inside the raised ring's bore - the issue's mistake in its bored form |
| **PO8** | *Regression - the cases v7's filters now always pass through.* The version 5 ring (*Casing Right*, the damper of the card's proof - a scratch copy), the version 3 joined face (*Port Left*, `tests/cases.yaml`) and the thin-solid side step flush with a cut face, each run exactly as its own case | Each made exactly as its own case says, with no *"facing into"* finding and no *"covers its port"* refusal |

**Signing, once PO1 to PO4 and PO6 to PO8 pass:** the card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model. Then close issue
#1029.

### Results, 2026-11-23 - Revit 2020, session 35764, scratch family `fp-scratch`

| Row | Result |
|---|---|
| PO2 (first call) | **PASS.** `facePlane=Ref. Level`, `system=Domestic Cold Water`, `sizeParameters=Port Dia`, `at=-5000, -5000, 0`: *"Nothing was made. No solid face centred at the point given in at= lies on "Ref. Level". What does lie on it: ..."*, each of the 74 downward faces listed with its form id and centre; rolled back |
| PO4 (second call, the at= half) | **PASS on another arrangement.** 74 faces lie on *Ref. Level*; `at=-800, -300, 0` (a 400 x 400 box's underside): *"pipe connector, Domestic Cold Water, round 100 mm, at X -800, Y -300, Z 0, pointing down"*, `sizedBy` *connector diameter <- Port Dia*, *"73 other face(s) ... left out"*; rolled back. An electrical connector first tried as the positive was refused at `accept` as POSITIVE EMPTY (`sizedBy` 0) - a sized port is the evidence |
| PO1, PO3, PO5-PO8 | NOT RUN - still owed |

**Signed on the owner's word, 2026-11-23, before PO1, PO3 and PO5-PO8 ran** - so a family build could go ahead;
the proof is D-30's two legs above. Issue #1029 stays open for the rows still owed.
