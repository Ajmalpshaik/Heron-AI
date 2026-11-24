# FRAGMENT-ISSUES archive — section 5b, rows 301 to 325

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
> Rows 301 to 325 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-301

*Moved from the register on 2026-11-17.*

**Defect.** **PLACE_NESTED_FAMILY THROWS WHEN THE NESTED FAMILY CARRIES ITS OWN OFFSET.** Found 2026-10-03 in Family1 (Revit 2024): `Round void : HWC 10 cm` (Revit's Structural Precast library, Work Plane-Based) on `Ref. Level` at `0,-1000,0` threw *"The placed family stands at 0, -1000, -200 mm, not 0, -1000, 0 mm"* and rolled back. Revit put it where the family's own offset says, 200 mm below its plane; the read-back calls that a failure. `Solid Wall - Bushing` and `M_People Content in Route Analysis` placed and read back cleanly at the same point. **Fix when owned:** compare the point ON the host plane (the location minus the offset along the plane's normal), and report the family's own offset in `placed` rather than failing.

**State.** **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.**

---

### Row 5b-302

*Moved from the register on 2026-11-17.*

**Defect.** **ADD_FAMILY_TYPE_PARAMETER FINDS NOTHING IT CAN LINK, ON EITHER ROUTE, IN REVIT 2024.** Found 2026-10-03 in Family1: a placed copy of `Solid Wall - Bushing` (one type) and of `M_People Content in Route Analysis` (two types) were each refused *"has no type or profile Revit lets be linked to a family parameter"*; a sweep drawn with the loaded profile `Support Profile : Support Profile` was refused the same way. With the `CanElementParameterBeAssociated` gate removed in a scratch copy, the nested copy still found no `ElementId` carrier among `ELEM_FAMILY_AND_TYPE_PARAM`, `ELEM_TYPE_PARAM`, `SYMBOL_ID_PARAM`, and the sweep's `PROFILE_FAM_TYPE` was refused by Revit itself: *"This parameter cannot be associated"*. **Measured further the same day, rolled back:** with a Family Type parameter of the nested family's own category made first (Structural Connections, Generic Models, Profiles) AND its current-type value set to the type the copy shows - docs/43's reported way round Autodesk's linking defect - `AssociateElementParameterToFamilyParameter` still answered *"This parameter cannot be associated"* for `ELEM_TYPE_LABEL` (the Label the Properties palette shows), `ELEM_FAMILY_AND_TYPE_PARAM`, `ELEM_TYPE_PARAM` and `ELEM_FAMILY_PARAM` on both copies, and for the sweep's `Profile`; `CanElementParameterBeAssociated` was false for every one before and after. So on Revit 2024 the API refuses the link the Label list makes by hand; nothing in the fragment's choice of parameter would change that. Stays DRAFT. **Fix when owned:** run the same probe on Revit 2027 (and 2020); if one release accepts it, gate the fragment to it and prove there, and if none does, take the capability out of the library in favour of a modeller step the reply names.

**State.** **MOVED TO ISSUE [#901](https://github.com/Ajmalpshaik/Heron-AI/issues/901) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.**

---

### Row 5b-303

*Moved from the register on 2026-11-17.*

**Defect.** **DIVIDE_FAMILY_SURFACE PRINTS EACH GRID WITH THE OTHER DIRECTION'S LINE COUNT.** Found 2026-10-03 in Family5 (a Mass, Revit 2024) on a 3000 x 2000 x 2500 mm form: top face `uGrid=20 vGrid=4` read *"U number 20, 7 gridline(s); V number 4, 23 gridline(s)"*; front face `uGrid=3 vGrid=1000 mm` read 6 and 6; `uGrid=6 vGrid=500 mm` read 9 and 9. The grid follows the input - the counts move with it - but `NumberOfUGridlines` counts lines running along U, which the V rule makes, so a modeller reading "U number 20, 7 gridlines" is told something false. The fragment's own finding already defers the division-or-line question to NEEDS-CHECKING BV21. **Fix when owned:** pair the U rule with `NumberOfVGridlines` and the V rule with `NumberOfUGridlines`, or word each as the lines it makes, and settle BV21 by eye on one surface.

**State.** **MOVED TO ISSUE [#944](https://github.com/Ajmalpshaik/Heron-AI/issues/944) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.**

---

### Row 5b-304

*Moved from the register on 2026-11-17.*

**Defect.** **AN UNSAVED FAMILY IS NAMED "" IN TWO FAMILY REPORTS.** Found 2026-10-03 on six new, never-saved families (Revit 2024): REPORT_FAMILY_TEMPLATE printed *Family ""* and *"" is a detail item*, and REPORT_FAMILY_TYPE_CATALOG gave `fileName` `.txt` - while the window title, the document's `Title`, reads `Family1` to `Family6`. REPORT_FAMILY_TEMPLATE also returns its private helpers (`axisOf`, `countOf`, `mm`, `shown`, `stored` as delegates, `invariant` as a culture) as if they were results. **Fix when owned:** fall back to `doc.Title` when the owner family's name is empty, and declare only real results in REPORT_FAMILY_TEMPLATE's `provides`.

**State.** **MOVED TO ISSUE [#945](https://github.com/Ajmalpshaik/Heron-AI/issues/945) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.**

---

### Row 5b-314

*Moved from the register on 2026-11-17.*

**Defect.** **THE LOADS PANEL'S FINALIZE IS TWO UNDO ENTRIES, NOT ONE.** Built 2026-10-04 with the building loads feature ([docs/44](../44-building-loads-from-the-model.md)): Finalize writes Design Cooling Load, Design Heating Load and Specified Supply Airflow into every calculated Space through ONE SET_PARAMETER_VALUES_BY_ID (the Companion table's own Apply - stale rows refuse it whole), then each Space's flow across its diffusers through ONE SET_AIR_TERMINAL_FLOW, chained after FILTER_ELEMENTS_BY_ID. Each is its own revit_change, so Revit's undo list holds two entries, and Article 8 asks for one per request. The panel says "two undo entries" beside the button and in its answer.

**State.** **MOVED TO ISSUE [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** One entry needs both writes inside one TransactionGroup, which is an add-in change (a request that carries two fragments, or a group the bridge opens around a chain) - not something the server can do by calling revit_change twice. Until then the modeller undoes twice, and the second write runs only if the first was kept. Group [CC](../needs-checking/group-cc.md) row CC11 checks the two entries on a real model.

---

### Row 5b-321

*Moved from the register on 2026-11-17.*

**Defect.** **QUESTIONS ABOUT THE DOORS AND WINDOWS ALREADY IN A WALL GO TO TOOLS THAT CHANGE THE MODEL.** Measured 2026-10-04 on 22 such questions while fixing row [319](../FRAGMENT-ISSUES.md#), on the trained model. On `main`, 8 went to a MODIFY: CREATE_STAIRS (*which doors are hosted in the stair core wall*, *are there any doors in the stair wall*), DIMENSION_WALL_OPENINGS (*how many windows are in this wall*, *which windows sit in this wall*), CREATE_WALL (*list the doors in the corridor walls*), CREATE_ROOF (the U-value question), SET_WALL_CONSTRAINTS (*what is in this wall*) and PLACE_HOSTED_FAMILY (*what is the sill height of these windows*). After 319's fix it is still 8, and not the same 8: the stair-core and U-value questions now reach a READ, *which doors are in this wall* and *count the windows in the external walls* now reach PLACE_HOSTED_FAMILY, and two more moved from one change to another - *how many windows are in this wall* and *list the doors in the corridor walls* to PLACE_HOSTED_FAMILY. On spelling alone it is 7, then 9. **#410 had hidden part of this:** before it, *select the doors hosted in this wall* and *what room does this door swing into* went to PLACE_HOSTED_FAMILY too. **Why:** SELECT_BY_HOST owns these questions and its card is mostly duct insulation and MEP fittings - the model ranks it 10th for *which doors are in this wall* while the words rank it 1st - and every history paragraph on PLACE_HOSTED_FAMILY's card (the host, a wall selected by hand, the swing, the To Room) is nearer to some question than to placing anything.

**State.** **MOVED TO ISSUE [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** `heron_lookup` sends the risk with every candidate and warns when a question lands on a change, and PLACE_HOSTED_FAMILY cannot run without the door type, points and rooms a question never gives, so nothing changes unasked - but the answer is still wrong. Row 319 gave SELECT_BY_HOST three question phrasings and went no further. The repair is SELECT_BY_HOST's own card - doors and windows first, a count beside the selection - and *what is the sill height of these windows* sent to READ_ELEMENT_PARAMETERS: each its own change, measured on the same 22 questions. **2026-10-09:** SELECT_BY_HOST's opening reworded as this row says: doors and windows first ("Finds the doors and windows already cut into a wall - selects them, and counts them"), the count beside the selection described, "A read, never a change". Separately, *what is the sill height of these windows* is declared on READ_ELEMENT_PARAMETERS ("Sill Height" in RevitAPI.xml, every release; utterance only, proof stands). Card words only; SELECT_BY_HOST is DRAFT. The phrasings were written before the wording: the 12 questions this row names, 14 more questions and 10 placement requests. A held-out set of 12 questions and 4 requests was written at the same time and never used to choose. Measured through heron_retrieve.find on private stores, before -> after. **Trained model:** questions reaching a write 13 -> 7 of 26. Of the row's own 12 it is 7 -> 6, and only the sill height moved: *which doors are in this wall*, *which windows sit in this wall*, *list the doors in the corridor walls*, both stair-wall questions and *what is in this wall* still reach a change. Requests sent to the wrong tool 1 -> 1. Held-out: 4 -> 2 questions, requests 1 -> 1. **Spelling:** questions 10 -> 10. *show me every window hosted in the facade walls* left PLACE_HOSTED_FAMILY, but *which doors are hosted in the stair core wall* moved to CREATE_STAIRS on a margin of 0.04 of a rank; the row's 12 went 4 -> 5. Requests 3 -> 4 (*put two windows in the north wall* -> SELECT_BY_HOST, a read). Held-out: 5 -> 3 questions, requests unchanged. **WHY NOT FURTHER, MEASURED:** a door-and-window heavier opening reached 7 of 26 on the model without the sill declaration. But it tied *place doors and windows hosted in walls at points* (5b-319, in test_routing_phrases) to this read on fused score, and the tie went to the read on words. About 390 wordings were swept on both backends and none was clean on both. The brief now wins by 2 ranks. Read-to-read cost: *show me what is hosted on this ceiling* -> CHECK_CEILING_COORDINATION on the model; on spelling, three of this card's own utterances lost nearness #1 but keep words #1 and identity. The rest would need PLACE_HOSTED_FAMILY's history paragraphs dealt with, as this row says; that card was not touched. Guarded by test_routing_phrases: three moved questions, each at least 1.9 ranks clear, plus the sill height in section 8. Seen failing on the cards as they stood.

---
