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
Revit 2020's `Metric Pipe Accessory.rft`, saved as `%TEMP%\heron-work\pn\pn-pump.rfa`: a flange disc, extrusion
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
| **PO4** | *Positive - at= settles the issue's case.* `facePlane=Discharge Face`, the same system and size, nothing else; then the same with `at=` the raised face's centre exactly as that refusal lists it | First refused: *"2 solid faces lie on "Discharge Face" (facing up, up)"*, each face with its form id and centre, asking for form= or at=. Second: the connector 150 mm at the raised face's centre, pointing up, and *"1 other face(s) lie on "Discharge Face" with their centre more than 1 mm from the point given in at=, and were left out"* - the screw's top |
| **PO5** | *Record - how long the face test takes.* PO4's second call on the issue's own transformer or pump family, if one is to hand (a scratch copy, never the owner's working file), else on pn-pump | The time the call took, written down. Each face on the plane looks out from up to twelve points against every solid of the family near it; nobody has measured that on a real-size family |

**Signing, once PO1 to PO4 pass:** the card's proof is written by `tools/batch-prove.py` with the owner, and
`heron-status` set to PROVEN in the same change - never by a session, and never on a working model. Then close issue
#1029.
