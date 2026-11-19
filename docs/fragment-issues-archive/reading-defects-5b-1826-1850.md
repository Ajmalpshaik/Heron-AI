# FRAGMENT-ISSUES archive — section 5b, rows 1826 to 1850

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
> Rows 1826 to 1850 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1826

*Moved from the register on 2026-11-17.*

**Defect.** **DRAW_FAMILY_DETAIL_LINES CANNOT NAME A PROJECT'S FLOOR PLAN WHEN A CEILING PLAN HAS THE SAME NAME.** NEEDS-CHECKING Group OC's OC0 step (7), Revit 2024 (session 18772), `Heron OC Tags` (a scratch project from English\DefaultMetric.rte, whose floor plan and ceiling plan are both `Level 1`), 2026-11-16, through `revit_change` from main `bb3235af`: `view=FloorPlan: Level 1`, `view=Floor Plan: Level 1`, `view=Floor Plans: Level 1` and `view=312` (the plan's id) were each refused *"No single view is called ..."*, and `view=Level 1` as ambiguous. Every new project from that template has the clash, so a detail line cannot be drawn in its plan at all. `revit_view_image` accepts the *"ViewType: Name"* form for exactly this case. Worked round by renaming the ceiling plan *Level 1 RCP* (`EDIT_TEXT_VALUES`, field Name, suffix), after which `view=Level 1` drew the line and read it back.

**State.** **MOVED TO ISSUE [#920](https://github.com/Ajmalpshaik/Heron-AI/issues/920) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card is PROVEN; accepting `ViewType: Name` (and an id) moves its C#, so it goes back to DRAFT - the owner's call.

---

### Row 5b-1827

*Moved from the register on 2026-11-17.*

**Defect.** **NO CARD CAN SET A WALL'S LOCATION LINE; WRITE_ELEMENT_PARAMETERS REFUSES IT WITHOUT A REASON.** NEEDS-CHECKING Group OC's OC0 step (11), Revit 2024 (session 18772), `Heron OC Tags`, 2026-11-16, through `revit_change` from main `bb3235af`: wall 352519 selected by `FILTER_ELEMENTS_BY_ID`, then `WRITE_ELEMENT_PARAMETERS` `parameterName=Location Line` with `value=Finish Face: Exterior`, and again with `value=2` (`WallLocationLine.FinishFaceExterior`): both `outcome: REFUSED`, `refused [352519]`, `written 0`, `readOnly` empty and **no line saying why**. A grep of `brain/fragments` finds `WALL_KEY_REF_PARAM` only in `create-walls-by-face`. The modeller set it by hand in Properties. A refusal with no reason is the defect; the missing capability is the gap.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Whether WRITE_ELEMENT_PARAMETERS should write an enumerated wall parameter by its display text is the owner's call; the refusal must at least name why.

---

### Row 5b-1828

*Moved from the register on 2026-11-17.*

**Defect.** **MOVE_ELEMENTS v3 MOVES AN ELEVATION MARKER BUT CANNOT READ IT BACK.** NEEDS-CHECKING Group OC's OC2 by-hand half, Revit 2024 (session 18772), `Heron OC Tags`, 2026-11-16, `validate move-elements --write` from branch `claude/loving-brown-t1boj3` (PR #887), the template's elevation marker 8142 (category Elevations) by id, `offset=1000,0,0`, rolled back: `moved 0`, `unverified [8142]`, *"0 moved the whole offset, 0 part of it, 0 did not move, 1 could not be read back"*. Not refused - the row's main point holds - but OC2 expected it `moved`. An `ElevationMarker` has no `Location` point, so the read-back has nothing to compare; its bounding box in a plan would do.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Group OC's OC2 notes it; the card stays DRAFT v3 until the owner decides whether this blocks the sign.

---

### Row 5b-1830

*Moved from the register on 2026-11-17.*

**Defect.** **A DRAFT'S ONE KEPT RUN (`try-once`) THAT RAISES HERON'S DELETE BOX COMES BACK `needs_unbound` AND KEEPS NOTHING.** NEEDS-CHECKING Group OE's OE7 and OE9, Revit 2024 (session 18772), `Heron OE Roofs`, 2026-11-16, `try-once` from branch `claude/trusting-edison-vloacn` (PR #890): the elements were selected by `SET_SELECTION` and **read back as selected (4) by a separate process just before**, then `try-once set-element-level` (and, earlier, `delete-elements` on roof 352583). The add-in log shows the job ran - *"commit inside Heron's own job ['Heron: set-element-level']"* - then *"Delete box: 'set-element-level' would delete 2 elements in 'Heron OE Roofs'; the modeller answered delete."* - and the client then printed `[needs_unbound]` *"Nothing is selected in Revit ..."*; no Keep or Discard box came and the model read back unchanged. The same `try-once` route kept `shape-wall-tops-to-roof` (no delete) and, in Group OC, `move-elements`. So the kept route breaks exactly where the delete box is asked inside it.

**State.** **MOVED TO ISSUE [#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) 2026-11-16** - still open, tracked there; this row is kept whole. Read 2026-11-19, not repaired: the second run is the add-in's - after Delete, RevitFragment.cs goes back to `runAgain` and binds the needs again from the live selection, which the delete box's put-back can leave empty, so `needs_unbound` comes before any Keep or Discard box. No chat-side fix: the repair is the add-in binding the second run from the first run's own ids (C#), and a Revit run proves it. Was: **OPEN - recorded, not repaired.** OE7 and OE9's kept halves are NOT RUN because of it.

---

### Row 5b-1831

*Moved from the register on 2026-11-17.*

**Defect.** **SET_ELEMENT_LEVEL v3 WOULD DELETE 2 ELEMENTS WHEN MOVING FOUR COLUMNS TO ANOTHER LEVEL.** Same run as row 5b-1830: four columns (2 architectural, 2 structural, none attached) and `newLevel=OE Plate`, `topLevelId=OE Upper`, `keepWallBase=true` - Heron's delete box counted 2 elements the job would delete, twice (12:07:13Z and 12:11:22Z). Changing a column's base and top level should delete nothing. Which two is not known: the batch's rolled-back run (OE6, PASS, `moved 4`) raises no box and does not report deletions.

**State.** **MOVED TO ISSUE [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card is held, not offered for signing, until the two are named.

---

### Row 5b-1832

*Moved from the register on 2026-11-17.*

**Defect.** **ROUTING, Group OE's OE10 on the owner's PC (Revit 2024, `model+rerank`), 2026-11-16, branch `claude/trusting-edison-vloacn`:** first through `heron_lookup` on an older scratch store, then **re-measured by `tools/score-routing.py --sentences ... --revit 2024 --backend model+rerank` on a store built from this branch's own 742 cards (tree `cab56658`)** - the same answer both times, the wanted card second each time: *"attach these walls to the roof above"* -> `ATTACH_COLUMNS` (with the line that ATTACH_WALLS is not on Revit 2024), where the row expects `SHAPE_WALL_TOPS_TO_ROOF`; *"delete this roof"* -> `REMOVE_MASS_FLOORS`, where it expects `DELETE_ELEMENTS`. The other three reached their card. The cloud container measured all five as written on its own backend.

**State.** **MOVED TO ISSUE [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - a routing row, not a card's failure.** The 2026 half was NOT RUN.

---
