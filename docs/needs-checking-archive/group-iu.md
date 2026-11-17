# NEEDS-CHECKING archive — Group IU

> **Checks that were done, moved out of the live register.** These are rows of Group IU of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *a dictionary's entries reach the chat (`providesEntries`, `READ_ELEMENT_PARAMETERS` version 5), and the end a flex would move (`LABEL_FAMILY_DIMENSION` version 2) (2026-10-22)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row IU5

*Moved from the register on 2026-11-17.*

**Run.** *Negative FIRST for the label - the ordinary height.* In the scratch family: `CREATE_REFERENCE_PLANES` "Box Top" at Z 250, nothing on it; `ADD_FAMILY_PARAMETERS` Height (length); `SET_FAMILY_TYPE_VALUES` Height=250; then `LABEL_FAMILY_DIMENSION fromPlane=Ref. Level toPlane=Box Top parameterName=Height`, `pinFreeEnd` left out; Undo; the same with `pinFreeEnd=true`

**Look for.** `labelled` *Height drives "Ref. Level" to "Box Top", reading 250 mm*; `freeEnd` EMPTY; a finding *""Ref. Level" is a level and "Box Top" is held by nothing, so a flex of "Height" moves "Box Top" - the ordinary case ..."*; `pinnedEnd` empty. With `pinFreeEnd=true`: `pinnedEnd` still empty, a finding *"pinFreeEnd was asked and nothing was pinned: "Ref. Level" is a level, so "Box Top" is the only end a flex can move ..."*, and Box Top shows NO pin in Properties. **No free-end warning either time** - this is the negative case **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---

### Row IU6

*Moved from the register on 2026-11-17.*

**Run.** *The fault's own case.* A new family: "Trap Top" at Z 300 with TWO other labelled dimensions on it (e.g. Trap Top to a "Rim" plane at Z 350 labelled Rim Height, holding 50), "Floor Top" a NEW plane at Z 0 - unpinned, nothing on it; "Trap Depth" (length) = 300; `LABEL_FAMILY_DIMENSION fromPlane=Floor Top toPlane=Trap Top parameterName=Trap Depth`, `pinFreeEnd` left out

**Look for.** `labelled` reading 300 mm AND `freeEnd` *""Floor Top" is held by nothing ... while "Trap Top" is held by the dimension labelled "Rim Height" ... A flex of "Trap Depth" may move "Floor Top" and leave "Trap Top" where it is. Pin it (SET_PIN_STATE, pinned=true) or dimension it to a level ..."*; `pinnedEnd` empty; Floor Top shows NO pin. **Then** `SET_FAMILY_TYPE_VALUES` Trap Depth=400: Floor Top moves to Z 100 (the fault, as warned) - look in the Front elevation, then Undo **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---

### Row IU7

*Moved from the register on 2026-11-17.*

**Run.** IU6 again with `pinFreeEnd=true`

**Look for.** `pinnedEnd` *""Floor Top" pinned first, as asked (pinFreeEnd), and reads back pinned: a flex of "Trap Depth" now leaves it where it is and moves "Trap Top" instead."*; Floor Top shows the pin; `SET_FAMILY_TYPE_VALUES` Trap Depth=400 now moves TRAP TOP to Z 400 (and Rim to 450) and Floor Top stays at Z 0 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---

### Row IU8

*Moved from the register on 2026-11-17.*

**Run.** *Both free.* Two NEW planes "A" at Z 0 and "B" at Z 200, nothing on either, "Gap" = 200; label A to B with `pinFreeEnd=true`

**Look for.** `freeEnd` *"Both ends are held by nothing ... which one a flex of "Gap" moves is Revit's choice ... pins "A", the first named."*; `pinnedEnd` names "A"; A pinned, B not; Gap=300 moves B to Z 300 **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---

### Row IU9

*Moved from the register on 2026-11-17.*

**Run.** *Same place.* "A" and "C" both at Z 0, "Gap" = 0; label A to C

**Look for.** Refused: *"... are in the same place - there is no distance to label. Pin the one that must stay (SET_PIN_STATE, pinned=true), move the other ..., set "Gap" to that distance (SET_FAMILY_TYPE_VALUES), then label ..."*; nothing made, nothing pinned **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---

### Row IU11

*Moved from the register on 2026-11-17.*

**Run.** *An EQ across the centre.* "Box Left" at X -300 and "Box Right" at X 300, nothing on either, "Width" = 600; label with `centrePlane=Center (Left/Right)` and `pinFreeEnd=true`

**Look for.** `labelled` and `equalised true` as version 1; `freeEnd` EMPTY; a finding *"With the EQ across "Center (Left/Right)", "Box Left" and "Box Right" move equally about it when "Width" changes - no end is free to move on its own."*; NOTHING pinned, the pinFreeEnd finding saying a pinned end would fight the EQ; Width=800 moves both planes 100 outward **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---

### Row IU12

*Moved from the register on 2026-11-17.*

**Run.** *A lock to geometry does not count.* After `EXTRUDE_BETWEEN_PLANES` has built a box locked to "Box Top" and Ref. Level, IU5 again on a fresh "Box Top 2" that the box is also locked to

**Look for.** The finding still reads "Box Top 2" as held by nothing - the lock to the form is not a hold on the plane - and a flex moves the plane and the face with it **Struck 2026-11-13:** PASSED - the verdict is in this file's *Results - 2026-10-24, the owner's laptop: Revit 2020, session 83568 - IU5 to IU12 ('label-family-dimension')*, which records it run and passed.

---
