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

### Row 5b-1239

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_STAIRS` (PROVEN, v3) REFUSED EVERY U STAIR IN THE OWNER'S KERALA HOUSE MODEL: REVIT 2020 WILL NOT MAKE A `Monolithic Stair` RUN WHOSE RISER IS 177.8 MM, AND THE TOOL NEVER TRIED ANOTHER COUNT.** Reported 2026-10-19 by the house chat on `Kerala house design` (Revit 2020, session 15072, Level 1 at 0, Level 2 at 3200): `Monolithic Stair`, shape u, entry south, first run west, hall X 11057.5..15887.5, Y 6057.5..9887.5 mm, run width 1100 with start offset 300 and 1000 with 200 - each *"Nothing was made. Revit would not keep the stair's runs"* (runs `wf-20261019-120330-0073-db08`, `wf-20261019-120358-0076-acba`), although the read-back had passed: 18 risers, two runs, a landing, the extent inside the hall. **Measured 2026-10-19 on a COPY of that model** (`%TEMP%\heron-work\stair-fix`, opened with `ACTIVATE_DOCUMENT`, every run rolled back), with Revit's failures read through `FailuresProcessing`: Revit posts one error, *"Can't make stairs"*, on run 1 (run type `150mm Depth`) whenever the riser is 3200 / 18 = 177.8 mm - 9 + 9, 10 + 8, with or without the landing, treads 280, 285 and 300, 1100 or 1500 mm wide, 100 mm off the walls, a straight run of 18 and a lone run of 9. 17, 19, 20, 21 and 22 risers were made, and at 18 `Precast Stair`, `Private`, `Industrial and Assembly` and `190mm max riser 250mm going` were made. Why Revit refuses that one height is not known; that it does is measured. On 2026-10-13 the same type made 23 risers of 173.9 mm, which is why it was never seen.

**State.** **FIXED IN CODE - version 4, DRAFT, run rolled back only (Group HS).** When the runs' transaction is rolled back by Revit, the stair is laid out once more with one riser more - every riser lower, still within the type's maximum - and the answer says *"Revit would not make 18 risers of 177.8 mm with 'Monolithic Stair' (Revit's words) so the stair was made with 19"*; a second refusal, or one riser more that does not fit, is refused with both. On a U of an odd count the first run takes the smaller half when only that fits along the hall (the second run comes back into the start offset, never past the entry side): 19 as 10 + 9 needed 3920 mm of a 3830 mm hall, 9 + 10 needs 3640. On the copy, the owner's values made 19 risers of 168.4 mm (9 + 10), one landing, extent X 11057.5..15887.5, Y 6077.5..9697.5, an opening in the Level 2 slab and 2 railings of type `1100mm` - rolled back. Card DRAFT until HS1 to HS5 run and are signed.

---

### Row 5b-1242

*Moved from the register on 2026-11-17.*

**Defect.** **`FLIP_ELEMENTS` TURNED A DOOR TO SWING INTO ITS ROOM AND LEFT ITS FROM AND TO ROOM THE OLD WAY ROUND - THE DOOR SCHEDULE READ BACKWARDS.** Measured 2026-10-19 on Revit 2020, the owner's 'Kerala house design': five `M_Single-Flush` doors (345958, 345961, 345964, 345968, 345970) were placed by `PLACE_HOSTED_FAMILY` before any room existed - so its To Room step could not turn them - then `FLIP_ELEMENTS` (v1, PROVEN) with `flipFacing=true` turned them to swing into their rooms. Read back: `FacingFlipped` True and the room on the facing side right (e.g. Living 2), but `get_ToRoom` still the hall (Living and Dining). **Revit does not swap From and To Room on a facing flip**, so the door schedule and `REPORT_DOOR_ROOM_LINKS` (5 disagreements) were wrong. The only `FlipFromToRoom()` in the library was inside `place-hosted-family`.

**State.** **MOVED TO ISSUE [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - the card is PROVEN at version 3 since, and its proof does not cover this row.** Read 2026-11-11 in a cloud container (no Revit): the proof block (2026-11-02, P1102 MEP, Revit 2020) answers `roomsSwapped 0 item(s)` and `roomsLeft 0 item(s)` in both cases - 44 elements from a selection, `flipHand` alone - so no door's From and To Room was swapped or left in it. The swap is still owed a case ([Group HT](../needs-checking/group-ht.md)). Below, *Not run in any Revit* and *the card went PROVEN to DRAFT* were true on 2026-10-19. Was: **OPEN - widened in source 2026-10-19 at DRAFT, version 2; a Revit run is owed ([Group HT](../needs-checking/group-ht.md)).** `flip-elements` version 2, no new capability and no new required input: after a facing flip each **door** is read on the last phase and `FlipFromToRoom` is called only when the room a short way out along its facing (wall half-width + 300 mm, a foot above it) is its From Room and the room behind is its To Room; read back and named in the new `roomsSwapped`. Already right, no room either side, no point location, or rooms a swap would not make agree: left and named in the new `roomsLeft`. A **window** is never swapped - its To Room is not tied to its facing (`place-hosted-family` sets it to the room inside while the window faces out) - and is named in `roomsLeft`. The new optional `fromToRoomOnly` (absent = false, version 1's run) flips nothing and does the same check on every door handed in, for doors already wrong - the owner's five. Compiled on 2020 to 2027 (`tools/check-fragments-compile.py`, exit 0), and seen to fail on 2020 with `FlipFromToRoom` misspelt, so the build reads the file. **Not run in any Revit.** The card went PROVEN to DRAFT: version 1's proof is kept as a record and is stale. `REPORT_DOOR_ROOM_LINKS` gained a routing row naming `FLIP_ELEMENTS` for the fix; it stays read-only.

---

### Row 5b-1243

*Moved from the register on 2026-11-17.*

**Defect.** **`tests/test_owner_queue.py` FAILS ON `main` - GROUP HM'S ROWS HM1 TO HM6 ARE WRITTEN TWICE IN `needs-checking/group-hm.md`.** Found 2026-10-19 by `tools/check-gaps.py` while doing row 5b-1242: check *"no row is reported twice"* fails, and the same on a clean worktree of `main` at `2c97217a`. `docs/needs-checking/group-hm.md` holds two tables that each start `\| **HM1** \|`, so `tools/owner-queue.py` reports HM1 to HM6 twice.

**State.** **FIXED 2026-10-19 in [#691](https://github.com/Ajmalpshaik/Heron-AI/pull/691), at the owner's word.** The results table under *"Run 2026-10-19"* now writes its ids plain (`\| HM1 \|`), as `group-aa.md` does; the run's text is unchanged. `tests/test_owner_queue.py` passes: no row reported twice.

---
