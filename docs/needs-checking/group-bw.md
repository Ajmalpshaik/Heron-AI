# Needs checking — Group BW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-03 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by hand, in the shape
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py) gives every group.

## Group BW - `place-room-at-point` measures a space after Revit regenerates, so an enclosed space is no longer called not enclosed (2026-10-03)

**The owner's PC, Revit 2024, a model with walls that close at least one area and leave somewhere open.**
[`place-room-at-point`](../../brain/fragments/place-room-at-point/fragment.yaml) - PLACE_ROOM_AT_POINT,
FRG-GEO-029 - read each new space's area before Revit had worked one out, so on Project2 on 2026-10-03 it
called 37 of 37 and 42 of 42 spaces *not enclosed* while REPORT_ROOM_SPACE_DATA read the same spaces as
enclosed ([row 5b-298](../FRAGMENT-ISSUES.md#)). It now places every point, regenerates once, then
measures. **It was PROVEN and is DRAFT now**: the change is inside `impl/`, so the 2026-09-13 proof no
longer matches the code and is kept in the card as the record of the old code ([D-30](../DECISIONS.md)).
That proof itself recorded `unenclosed 5` of 5 rooms, which the same early read probably explains.
**What is already known, and it is NOT yet a signed proof:** it compiles on every release from 2020 to 2027, and on 2026-10-03 BW1 and BW2 were both RUN on Project2 saved as *c bulding test* (Revit 2024, session 41260), each leg rolled back, against an 8000 x 6000 mm box of four `Generic - 200mm` walls built on the empty Roof level 300 m from the building and deleted afterwards on the owner's word:

| Run | Points | created | unenclosed |
|---|---|---|---|
| Rooms, new code | inside + outside | 2 | 1 - the outside one only |
| Rooms, new code | inside only | 1 | 0 |
| Spaces, new code | inside + outside | 2 | 1 - the outside one only |
| Spaces, new code | inside only | 1 | 0 |
| Spaces, OLD code, same box | inside + outside | 2 | **2** - the defect |
| Spaces, OLD code, same box | inside only | 1 | **1** - the defect |

**Why it is not signed:** `heron_validate accept` takes an empty negative leg, which this fragment cannot give because it always places something, or a D-53 tracking set, and `validate --vary` splits its values on commas, which every point is written with.

| # | Check | Expected |
|---|---|---|
| **BW1** | PLACE_ROOM_AT_POINT `asSpace=false` with points inside closed areas, and one point outside every enclosure | Every room is in `created`; only the outside point's room is in `unenclosed`. **Negative:** the outside room reads *Not Enclosed* in Properties and REPORT_ROOM_SPACE_DATA names it unenclosed |
| **BW2** | The same points with `asSpace=true` | Every space is in `created`; only the outside point's space is in `unenclosed` - **not** every space, which is the defect. REPORT_ROOM_SPACE_DATA agrees space by space. **Negative:** the outside space is named, so the fragment is not simply reporting none |
