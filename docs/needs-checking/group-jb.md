# Needs checking — Group JB

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JB - a nested part that came in upside down flips its work plane: `FLIP_ELEMENTS` version 3 (2026-10-24)

From [row 5b-1335](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020 the work-plane-based nested
family *TRG_Duct Accessory_Damper Quadrant Handle*, placed by `PLACE_NESTED_FAMILY` on a front reference
plane made by `CREATE_REFERENCE_PLANES` - every plane it makes has normal +Y - came in on the front (-Y)
side facing inward and upside down. `ROTATE_ELEMENTS_ABOUT_AXIS` was refused by Revit (*"Can't rotate
element into this position"*); a rolled-back probe setting `IsWorkPlaneFlipped = true` put it right. Version
2 of `FLIP_ELEMENTS` could not, so the owner flipped it by hand.

**What changed.** [`flip-elements`](../../brain/fragments/flip-elements/fragment.yaml) version 3: a new
optional `flipWorkPlane` (absent = false, version 2's run). Each family instance handed in is asked
`CanFlipWorkPlane`; where true its `IsWorkPlaneFlipped` is toggled, and after one regeneration the flag, its
`FacingOrientation` and its bounding box are read back against what they were - one line each in
`workPlaneFlips`. One that cannot is named in `cannotFlipWorkPlane` and left. A throw is not caught, so the
whole call rolls back.

**What is done and what is not.** It compiles for 2020 and 2027 (`tools/check-fragments-compile.py`,
2026-10-24), and `CanFlipWorkPlane` and `IsWorkPlaneFlipped` are in every release's shipped RevitAPI.dll
2020 to 2027. **Nothing here has run in Revit.** The card is DRAFT until JB0 and JB1 pass and the owner
signs; version 2's proof is kept on the card as a record and is stale.

**The family.** A scratch family, never the owner's: *Heron JB Flip.rfa* in `%TEMP%\heron-work\group-jb`,
made from Revit 2020's *Metric Generic Model.rft* by `CREATE_FAMILY_DOCUMENT`. In it, with PROVEN tools: a
front reference plane made by `CREATE_REFERENCE_PLANES` at Y -300 (normal +Y, the wrong way for a part on
the front); and a scratch nested part *Heron JB Part.rfa*, Work Plane-Based = yes, one extrusion that stands
44 mm out of its work plane and runs 0 to 150 mm up it - so a wrong flip shows in Y and Z both - loaded with
`LOAD_FAMILY` and placed with `PLACE_NESTED_FAMILY` on that plane. Read its box with `REPORT_BOUNDING_BOX`
before each run. Every row is run once rolled back first.

| # | Run | Look for |
|---|---|---|
| **JB0** | *Negative FIRST.* The nested part and the extrusion of the host handed in, `flipWorkPlane` left out, `flipHand=false`, `flipFacing=false` | `flipped` 0, `workPlaneFlips` empty, the part's id in `cannotFlip` and the extrusion's in `notFamilyInstance`; `REPORT_BOUNDING_BOX` on the part reads what it read before. **FAIL:** any line in `workPlaneFlips`, or the part moved |
| **JB1** | *Positive.* The nested part, `flipWorkPlane=true`, `flipHand=false`, `flipFacing=false` | `flipped` 1, one line in `workPlaneFlips`: *"not flipped -> flipped"*, the facing reversed against its *was*, and the box moved to the front of the plane - Y from about -344 to -300, Z from 0 to 150, against the *was* on the other side. **Read back:** `REPORT_BOUNDING_BOX` on the part agrees with the line, and it stands the right way up in 3D. **FAIL:** the box where it was, or a line saying the flip did NOT take |
| **JB2** | *Back again.* JB1 kept, then the same call again | `workPlaneFlips` *"flipped -> not flipped"*, the box back to JB0's numbers |
| **JB3** | *One that cannot.* A family instance whose `CanFlipWorkPlane` reads false - which instance in a family does so on 2020 is not known; a door hosted in a wall of a scratch project is one to try - `flipWorkPlane=true` | Named in `cannotFlipWorkPlane`, its id in `cannotFlip`, `flipped` 0, nothing moved, no exception. If no such instance can be found, say so here; nothing in the row changes to make it pass |
| **JB4** | The signature, on the owner's word only, after JB0 and JB1 | `python brain/heron_validate.py accept flip-elements --by "Ajmal PS"` |

A row that fails stays open; nothing in a row changes to make it pass. The 44 mm and 150 mm are the scratch
part's own sizes: if it is built otherwise, JB1's numbers are its sizes, read off JB0's box.
