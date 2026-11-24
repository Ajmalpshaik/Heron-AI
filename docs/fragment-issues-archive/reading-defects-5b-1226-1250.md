# FRAGMENT-ISSUES archive — section 5b, rows 1226 to 1250

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 1226 to 1250 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1242

*Moved from the register on 2026-11-17.*

**Defect.** **`FLIP_ELEMENTS` TURNED A DOOR TO SWING INTO ITS ROOM AND LEFT ITS FROM AND TO ROOM THE OLD WAY ROUND - THE DOOR SCHEDULE READ BACKWARDS.** Measured 2026-10-19 on Revit 2020, the owner's 'Kerala house design': five `M_Single-Flush` doors (345958, 345961, 345964, 345968, 345970) were placed by `PLACE_HOSTED_FAMILY` before any room existed - so its To Room step could not turn them - then `FLIP_ELEMENTS` (v1, PROVEN) with `flipFacing=true` turned them to swing into their rooms. Read back: `FacingFlipped` True and the room on the facing side right (e.g. Living 2), but `get_ToRoom` still the hall (Living and Dining). **Revit does not swap From and To Room on a facing flip**, so the door schedule and `REPORT_DOOR_ROOM_LINKS` (5 disagreements) were wrong. The only `FlipFromToRoom()` in the library was inside `place-hosted-family`.

**State.** **MOVED TO ISSUE [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the card is PROVEN at version 3 since, and its proof does not cover this row.** Read 2026-11-11 in a cloud container (no Revit): the proof block (2026-11-02, P1102 MEP, Revit 2020) answers `roomsSwapped 0 item(s)` and `roomsLeft 0 item(s)` in both cases - 44 elements from a selection, `flipHand` alone - so no door's From and To Room was swapped or left in it. The swap is still owed a case ([Group HT](../needs-checking/group-ht.md)). Below, *Not run in any Revit* and *the card went PROVEN to DRAFT* were true on 2026-10-19. Was: **OPEN - widened in source 2026-10-19 at DRAFT, version 2; a Revit run is owed ([Group HT](../needs-checking/group-ht.md)).** `flip-elements` version 2, no new capability and no new required input: after a facing flip each **door** is read on the last phase and `FlipFromToRoom` is called only when the room a short way out along its facing (wall half-width + 300 mm, a foot above it) is its From Room and the room behind is its To Room; read back and named in the new `roomsSwapped`. Already right, no room either side, no point location, or rooms a swap would not make agree: left and named in the new `roomsLeft`. A **window** is never swapped - its To Room is not tied to its facing (`place-hosted-family` sets it to the room inside while the window faces out) - and is named in `roomsLeft`. The new optional `fromToRoomOnly` (absent = false, version 1's run) flips nothing and does the same check on every door handed in, for doors already wrong - the owner's five. Compiled on 2020 to 2027 (`tools/check-fragments-compile.py`, exit 0), and seen to fail on 2020 with `FlipFromToRoom` misspelt, so the build reads the file. **Not run in any Revit.** The card went PROVEN to DRAFT: version 1's proof is kept as a record and is stale. `REPORT_DOOR_ROOM_LINKS` gained a routing row naming `FLIP_ELEMENTS` for the fix; it stays read-only.
