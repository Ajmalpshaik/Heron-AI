# FRAGMENT-ISSUES archive — section 5b, rows 1626 to 1650

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
> Rows 1626 to 1650 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1626

*Moved from the register on 2026-11-17.*

**Defect.** **D-158 AND D-159 ARE MARKED ACCEPTED ON *"YOU CAN DO IT"* - LEAVE TO BUILD, NOT A READ-BACK.** Found 2026-11-12 in an independent review of #866 ([D-159](../decisions/D-159.md)), which ran it against a stand-in for Revit - **not seen in Revit**. Each decision's *Accepted* quotes the owner's leave to build the item; neither was read back to him. Three of their numbers he never heard: identity by **Revit's process id** (since repaired to the file path, [row 5b-1614](../fragment-issues/row-5b-1614.md) - a departure from D-159's point 3 as written), the **20** runs kept (now per model, [row 5b-1616](../fragment-issues/row-5b-1616.md)), and the **5,000** ids a list sends (`IdsSentPerList`).

**State.** **MOVED TO ISSUE [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - for the owner.** Ajmal PS is to read D-158 and D-159 back, with the repaired note at the foot of D-159. Their status is not changed here.

---

### Row 5b-1640

*Moved from the register on 2026-11-17.*

**Defect.** **`REPORT_STRUCTURAL_LOADS` READS EACH LOAD CASE'S CATEGORY THE WAY [ROW 5b-1637](../fragment-issues/row-5b-1637.md) MEASURED PRINTING `?` FOR EVERY CASE ON REVIT 2020 - THROUGH `LOAD_CASE_SUBCATEGORY`'S STRING - SO ITS *"Load cases (n)"* LIST NAMES NO CATEGORY.** Found 2026-11-13 by reading, while repairing [row 5b-1639](../fragment-issues/row-5b-1639.md) on the same card; **not run**. [`fragment.cs`](../../brain/fragments/report-structural-loads/impl/any/fragment.cs), the `wantCases` block (line 227 on 2026-11-13): `c.get_Parameter(BuiltInParameter.LOAD_CASE_SUBCATEGORY)` and `AsValueString()`, `?` when either is null - the same two lines `CREATE_LOAD_CASE` version 1 carried in `caseLine`, which on Revit 2020 (P1102 STR, session 41776) printed *"1 "DL1" - nature Dead, category ?"*, *"2 "LL1" - nature Live, category ?"* and *"4 "SNOW1" - nature Snow, category ?"*. This card's own runs that day used `show=loads`, which skips the list, so its output was never seen. **What it would be:** `CREATE_LOAD_CASE` version 2's `categoryOf` - `LoadCase.SubcategoryId` matched by id among the subcategories of Structural Load Cases and named by the category's own Name.

**State.** **MOVED TO ISSUE [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - found by reading, not repaired.** Outside the row the card was opened for (5b-1639, whose repair left the C# alone on purpose), so recorded, not fixed (AGENTS.md). Repair it after [Group NX](../needs-checking/group-nx.md) NX3 shows `SubcategoryId` reads on 2020: if it does, the same repair here; if it does not, both cards need another reading.

---

### Row 5b-1644

*Moved from the register on 2026-11-17.*

**Defect.** **`PLACE_COLUMN_REBAR` PLACES NO TIE THAT HAS A HOOK ON REVIT 2020 - EVERY TIE HOOK THE CARD ITSELF OFFERS ENDS IN *"Revit refused a bar - An internal error has occurred"*, WHILE THE SAME TIES WITH NO HOOK, AND THE MAIN BARS, ARE PLACED AND READ BACK.** Found 2026-11-14 on the owner's PC, proving the card. Revit 2020, session 17824, the saved scratch copy *P1102 STR* (`%TEMP%\heron-work\prove-1102`), every run rolled back; column 122477 *"450mm"*, Concrete - Cast-in-Place Concrete - 28 MPa, 3800 mm, `cover=40` (without it the column is skipped - *"has no rebar cover set on every face"*, as the card says). `mainBars=4 x 13M, ties=none`: `placed` 4, read back. `mainBars=none, ties=10M @ 300, tieHook=none`: `placed` 1. `ties=10M @ 300` with `tieHook=Stirrup/Tie - 135 deg.`, with `Stirrup/Tie - 90 deg.`, and `13M @ 300` with the 135: `placed` 0, `skipped` *"Structural Columns '450mm' (id 122477): Revit refused a bar - An internal error has occurred. - every bar made in it was removed again"*. The hooks named are the three the card's own refusal lists as *"Hooks for these bars"*. The 2020-2026 route is `Rebar.CreateFromCurves(doc, style, type, startHook, endHook, host, normal, curves, startOrientation, endOrientation, true, true)` ([`fragment.cs`](../../brain/fragments/place-column-rebar/impl/any/fragment.cs) lines 146-165, the hook side worked out from Revit's remarks) - so the closed tie loop, its normal, or the side given for its hooks is what Revit will not take; which, is not measured. Whether the column is round or rectangular was not read.

**State.** **MOVED TO ISSUE [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card stays DRAFT and is **not** drafted for signing: a proof run without a hook would hide the case a modeller asks for first. The repair's first question is what Revit 2020 accepts for a hooked closed tie - a loop open at the hook, or the orientations reversed.

---

### Row 5b-1645

*Moved from the register on 2026-11-17.*

**Defect.** **`revit_health` SAYS A REVIT IS *"(free)"* WHILE ANOTHER CHAT HOLDS THE MODEL IN FRONT - IT READS THE SESSION LEASE, AND A MODEL HOLD ([D-141](../decisions/D-141.md)) IS SOMETHING ELSE - SO THE ONE THING A CHAT MOST NEEDS BEFORE IT ASKS IS NOT IN THE ANSWER THAT EXISTS TO SAY IT.** Found 2026-11-14 on the owner's PC running Group NP's NP5. Revit 2020, session 26612, *P1102 MEP* in front, Ask to Keep ON. A second chat (`HERON_CONVERSATION_ID=np5-second`) read the selection; the add-in log: *"06:54:47Z Model hold: 'P1102 MEP' is now held by chat np5-seco."* Then, from this chat: `revit_health` - *"Revit 2020, session 26612 - (free) - open model: P1102 MEP"* and the Ask to Keep ON line; `revit_read READ_SELECTION` seconds later - *"Model 'P1102 MEP' is in use by another chat ... Nothing was sent to the model"*, `outcome: NOT RUN`; the second chat's next read was answered, not refused. The column is written by [`heron_bridge_client.availability()`](../../mcp/client/heron_bridge_client.py) from `lease_state()` - `inUse`, `mine`, `leaseSecondsRemaining` - and the hold is checked only when a request reaches the model. On 2026-11-13 the same answer did say *"Model '...' is in use by another chat"*, when the other chat still had the session's lease as well, so which of the two a chat is told depends on timing it cannot see.

**State.** **MOVED TO ISSUE [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953) 2026-11-16** - still open, tracked there; this row is kept whole. Read 2026-11-19, not repaired: the bridge's `info` (revit/Heron.Bridge/BridgeServer.cs) carries no field for the model hold (HeronModelHold.cs), and the one probe that would see it claims the lease. The repair is a field on `info` from the add-in, then `availability()` reading it - C# and a Revit run. Was: **OPEN - recorded, not repaired.** NP5's *"says it is in use by another chat"* is not met for a hold alone; the Ask to Keep half of the row and its *"the second chat ... was not cut off"* are. Whether `info` should carry the hold (it takes no lease and claims nothing, so asking would still be free) is the repair's question.

---

### Row 5b-1646

*Moved from the register on 2026-11-17.*

**Defect.** **"DELETE THESE TWO WALLS" LOOKS UP `DISALLOW_JOIN` FIRST; `DELETE_ELEMENTS`, THE CARD THAT DOES IT, COMES THIRD - AND THE TOP ANSWER IS A MODIFY, SO A CHAT THAT TOOK IT WOULD CHANGE WALL JOINS WHEN ASKED TO DELETE.** Seen 2026-11-14 on the owner's PC, running Group NP's NP7: `heron_lookup("delete these two walls")` on this PC's backend (model and re-ranker, *"Filtered to Revit 2020"*) answered *"would need DISALLOW_JOIN ... FRG-ELE-048 ... PROVEN ... MODIFY"*, *"best found by words #14 + nearness #16 - both agree, re-ranker #1"*, then `DISCONNECT_CONNECTORS` second and `DELETE_ELEMENTS` third (*"words #4 + nearness #1 - both agree, re-ranker #3"*) - so the trained model put the right card first and the re-ranker moved it down. A chat that asked by phrase rather than by name would be offered the wrong change; nothing ran here - the delete was asked by capability and went through Revit's own Delete box. Read with the routing rows the PC's backend keeps open ([5b-1513, 5b-1514](../fragment-issues/section-5b-rows-401-425.md)).

**State.** **MOVED TO ISSUE [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - measured once, not repaired.** The phrase belongs in `DELETE_ELEMENTS`'s routing words or a guard row, measured on all four backends before and after as the routing method asks; one lookup is not a sweep.

---

### Row 5b-1648

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_EXTRUSION_ROOF` CAN KEEP NO ROOF ON REVIT 2020: REVIT MAKES IT, AND THE CARD'S OWN READ-BACK THROWS *"... another level. NOTHING from this call was kept."* EVERY TIME, BECAUSE IT COMPARES `Element.LevelId` WITH THE LEVEL THE ROOF WAS MADE ON.** Found 2026-11-14 on the owner's PC, proving the card. Revit 2020, session 26612, the saved scratch copy *P1102 MEP*, rolled back. `level=Roof Level, roofType=Generic - 400mm, plane=1, profile=0,0; 6000,2000; 12000,0, extrusionStartMm=0, extrusionEndMm=6000` - `fragment_threw`: *"The roof did not read back as asked: Extrusion Start -6000 and End 0 mm (asked 0 and -6000 along the plane's normal), type "Basic Roof: Generic - 400mm", 2 profile edge(s) of 2, another level."* The same on grid `A`, and with the ends given as -6000 and 0. Without `roofType` the card refuses cleanly, listing *"Basic Roof: Generic - 400mm, Sloped Glazing: Sloped Glazing"*. In [`fragment.cs`](../../brain/fragments/create-extrusion-roof/impl/any/fragment.cs) the extents already hold under its swap rule (start and end may come back either way round, and here they did), the type and both edges match - the one failing term is `made.LevelId != host.Id` (line 373). `NewExtrusionRoof(curves, plane, level, type, start, end)` takes the level as the roof's reference level; what `LevelId` answers for an extrusion roof on 2020 - `InvalidElementId`, or another level - was not read.

**State.** **MOVED TO ISSUE [#948](https://github.com/Ajmalpshaik/Heron-AI/issues/948) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The card stays DRAFT and cannot be drafted: no positive can pass. The repair reads the roof's reference level (its Reference Level parameter, or whatever `LevelId` is measured to answer) before comparing.

---

### Row 5b-1649

*Moved from the register on 2026-11-17.*

**Defect.** **"APPLY THE VIEW TEMPLATE HVAC DUCT PLAN TO THE VIEW LEVEL 3 HVAC DUCT PLAN" LOOKS UP `CREATE_VIEW_TEMPLATE_FROM_VIEW` FIRST, AND `APPLY_VIEW_TEMPLATE` - PROVEN, THE CARD THAT DOES IT - IS NOT IN THE FIVE CANDIDATES AT ALL.** Seen 2026-11-14 on the owner's PC running Group NR's NR5 (c): `heron_lookup` on this PC's backend answered *"would need CREATE_VIEW_TEMPLATE_FROM_VIEW ... best found by words #8, re-ranker #1. NO candidate was found by both routes, so treat this as a weak match"*, then `APPLY_COLOR_FILL_SCHEME`, `TAG_ELEMENTS_IN_VIEW`, `FIND_VIEWS` and `CREATE_OPENING`. `APPLY_VIEW_TEMPLATE` ([`apply-view-template`](../../brain/fragments/apply-view-template/fragment.yaml)) was found by grepping the library for `ViewTemplateId =`, and then did the job by name, kept on the owner's Keep. A chat that took the top answer would make a new template out of the view instead of applying one. Read with [row 5b-1646](../fragment-issues/row-5b-1646.md) - the second phrase in two days whose card the lookup does not offer.

**State.** **MOVED TO ISSUE [#956](https://github.com/Ajmalpshaik/Heron-AI/issues/956) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - measured once, not repaired.** The phrase belongs in `APPLY_VIEW_TEMPLATE`'s routing words, measured on all four backends before and after as the routing method asks.

### Row 5b-1643

*Moved from the register on 2026-11-24.*

**Defect.** **[`CREATE_FROM_ROOM_BOUNDARIES`](../../brain/fragments/create-from-room-boundaries/fragment.yaml) IS PROVEN AT VERSION 2, AND ITS PROOF NEVER MADE A WALL FINISH - THE ONE THING VERSION 2 ADDED.** Found 2026-11-13 in a cloud container (no Revit), sorting the open rows of NEEDS-CHECKING against the cards they check. Version 2 (row 5b-1255) is wall finishes by room; [Group HX](../needs-checking/group-hx.md) was written to prove it, with HX1's positive expecting `finishWalls` **9**. The card's `proof:` block (2026-10-21, Ajmal PS, *Cinematic ID Test*, Revit 2020, fingerprint `6fc4aa3670f34e0d`) ran a **floor**: `hostType=Floor: Generic 150mm`, `wantFloor true`, `wantFinish false`, `finishWalls 0 item(s)` in both cases. HX1 to HX3 have no recorded result, and HX4, the signing, says to read the draft against them. So the card's status covers floors and ceilings - version 1's path - and a wall-finish request routed to it runs as PROVEN on code no proof has exercised. **How many other cards were signed on a case that misses their newest version's addition is not known** - this is the one the sort could show from the files, because the proof's own words name the path it took.

**State.** **MOVED TO ISSUE [#995](https://github.com/Ajmalpshaik/Heron-AI/issues/995) 2026-11-21** - still open, tracked there. Was: OPEN - NEEDS REAL REVIT. **2026-11-16 - BACK TO DRAFT, on the owner's answer.** Asked whether the card goes back to DRAFT until Group HX is run, Ajmal PS answered in his own words *"go with you suggetions"* to the suggestion that it does: `heron-status: DRAFT`, the 2026-10-21 proof block kept as the record, and a `proof-held:` key saying why it is not used, so `tools/check-signatures.py` reads it as HELD rather than as a forgotten promotion. **What a chat now does with it:** `revit_change` and `fragment --write --apply` refuse to keep its change before anything is sent, floors and ceilings by room included - so step 10 of `building-shell` and step 8 of `room-setup` are not kept from a chat - while a rolled-back run, a dry run or a proof, still runs ([D-117](../decisions/D-117.md)); its one kept run is `try-once` behind Revit's Keep or Discard box, on a saved model in front, on the modeller's word ([D-119](../decisions/D-119.md)). Owed: Group HX's HX1 to HX3 on a model, then a new proof whose positive makes wall finishes, then the owner's PROVEN edit. HX4 is left open with a pointer here. **As recorded:** OPEN - recorded, not fixed; NEEDS REAL REVIT. Owed: Group HX's HX1 to HX3 on a model, then a new proof whose positive makes wall finishes. Whether the card goes back to DRAFT until then is the owner's call - the status is honest for version 1's path and not for version 2's. HX4 is left open with a pointer here.

---
