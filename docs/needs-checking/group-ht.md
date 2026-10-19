# Needs checking — Group HT

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HT - FLIP_ELEMENTS v2: a flipped door's From and To Room follow its swing (2026-10-19)

From [row 5b-1242](../fragment-issues/section-5b-rows-401-425.md). On Revit 2020, in the owner's
'Kerala house design', five doors flipped with `FLIP_ELEMENTS flipFacing=true` swung into the right
rooms, but `get_ToRoom` still named the hall: Revit does not swap From and To Room on a facing flip.

**What changed.** `flip-elements` version 2: after a facing flip each **door** is read on the last
phase, and `FlipFromToRoom` is called only when the room a short way out along its facing is its From
Room and the room behind is its To Room. Each swap is read back and named in `roomsSwapped`; every door
or window checked and not swapped is named in `roomsLeft` with why. A window is never swapped. The new
optional `fromToRoomOnly` flips nothing and does the same check on every door handed in - for doors
already wrong.

**What is done and what is not.** It compiles for 2020 to 2027 (`tools/check-fragments-compile.py`).
**Nothing here has run in Revit.** The card is DRAFT until HT1 and HT2 pass and the owner signs; version
1's proof is kept on the card as a record and is stale. Run on a **scratch copy** of the model - never
the working one - with rooms placed and enclosed, on the last phase. Note each door's From Room and To
Room in Properties before each run.

| # | Run | Look for |
|---|---|---|
| **HT1** | *Positive.* One door with a room on both sides, `flipFacing=true`, `flipHand=false` | `flipped` 1, the door swings into the other room on the plan, and `roomsSwapped` one line naming From -> To before and after. **Read back:** To Room in Properties is the room it now swings into; `REPORT_DOOR_ROOM_LINKS` on it finds no disagreement. **FAIL:** To Room still the old room, or `roomsSwapped` empty |
| **HT2** | *Negative.* One door with no room on either side (a wall with no rooms placed), `flipFacing=true` | `flipped` 1, `roomsSwapped` empty, `roomsLeft` one line *"no room on either side, so nothing was swapped"*. From and To Room still blank. **FAIL:** any line in `roomsSwapped` |
| **HT3** | *The owner's five doors.* On the 'Kerala house design', select doors 345958, 345961, 345964, 345968, 345970; `fromToRoomOnly=true` | `flipped` 0, no door moves on the plan, five lines in `roomsSwapped` (Living and Dining no longer the To Room). Then `REPORT_DOOR_ROOM_LINKS` on the same doors: 0 disagreements. Run it once rolled back first; keep it only after that reads right |
| **HT4** | *A door already right.* A door whose To Room already matches its swing, `fromToRoomOnly=true` | `roomsSwapped` empty, `roomsLeft` *"already right"*. Nothing changes in Properties |

**Then sign** `flip-elements` on HT1 and HT2. HT3 is the owner's real job and HT4 the case
`tests/cases.yaml` names beside them.
