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

### Row 5b-1627

*Moved from the register on 2026-11-17.*

**Defect.** **GROUP NK ARRANGES `LIST_MATERIALS`' "TWO PARAMETERS SHARE THE NAME 'KEYWORDS'" CASE ON THE MATERIAL BROWSER'S KEYWORDS BOX, AND THAT BOX IS NO PARAMETER - SO ONE "KEYWORDS" PROJECT PARAMETER GIVES A MATERIAL ONE PARAMETER OF THAT NAME, NOT TWO, AND "PROBE" TYPED IN THE BOX CAN NEVER BE READ.** Found 2026-11-13 on the owner's PC, arranging [Group NK](../needs-checking/group-nk.md)'s row NK0 for the batch [`tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml`](../../tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml). NK0 asks for *"on 'Heron A', Identity tab, Keywords 'probe'"* and a project parameter "Keywords" bound to Materials, and says *"a material shows Keywords twice"*; the job's `list-materials` note expects *"keywords NOT READ - 2 parameters share that name"*, and NK8 (a) expects *"keywords 'probe'"* once that project parameter is deleted. **Measured, Revit 2024, session 22684, scratch project "Heron Reads Test" under `%TEMP%\heron-work\nk`, material "Heron A" (id 352514), before any project parameter was added:** a probe sent through the bridge under `set-material-properties`' contract and rolled back listed every parameter the material carries - Image, Keynote, Category (twice), Name, IFC Predefined Type, Export to IFC As, Export to IFC, IfcGUID, Design Option, Model, Manufacturer, Comments, URL, Description, Shininess, Smoothness, Glow, Transparency, Color, Family Name, Type Name, Cost, Mark - with `LookupParameter("Keywords")` **null**, `GetParameters("Keywords").Count` **0**, and `PROPERTY_SET_KEYWORDS` not on the material. The same day [`set-material-properties`](../../brain/fragments/set-material-properties/fragment.yaml) with `fieldValues=Keywords=probe` threw *"Revit offers no 'Keywords' on the material 'Heron A' - not as a parameter of its own and not by that name. Set it in the Material Browser's Identity tab; nothing from this call was kept"*. [Group IA](../needs-checking/group-ia.md) measured the same on Revit 2020 on 2026-10-20 (IA2, and EYE2 by the owner's eye: *"Keywords is a box the browser shows and no parameter reaches"*), and `list-materials`' own source says *"KEYWORDS HAS NO BUILT-IN ID HERE, SO IT IS READ BY NAME"*. **So, as NK0 is written:** after the project parameter is added, a material carries ONE parameter named Keywords; `list-materials` reads it (empty) and has nothing to refuse, so the batch job's positive can pass on `materialCount` while the repair of row 5b-203 for this card is never exercised; and NK8 (a) can never read 'probe'. The other twelve cards of the group are not touched by this: the walls' "Comments" is Revit's own `ALL_MODEL_INSTANCE_COMMENTS`, a parameter named Comments, so a project parameter of that name does make two. **What would arrange it, not done:** TWO parameters named Keywords bound to Materials - one project parameter and one shared one, the way NK8 (b) already arranges "Carbon Factor" - with "probe" written into the one that stays when the other is deleted. **Arranged that way the same day, at the owner's word ("you have to create it"), once `ADD_PROJECT_PARAMETER` version 3 was signed PROVEN on this model (Group NL, run 2026-11-13):** `ADD_PROJECT_PARAMETER` kept, `createMissing=True`, file `%TEMP%\heron-work\nk\heron-nk-shared.txt` (made by the same card with `createFile=True` for "Comments" on Walls), "Keywords" in group `HERON NK` bound to Materials as an instance parameter; then `set-material-properties` kept `Keywords=probe` on "Heron A" - found by name, ONE such parameter then - *"1 field(s) changed on 1 of the 1 material(s) named"*; then a second "Keywords", group `HERON NK 2`, bound to Materials - Revit accepted it, `bound true`. A rolled-back probe then read `GetParameters("Keywords").Count=2` on Heron A: shared `19661eed-4aea-491d-8bcc-83b88ca2d3ba` = 'probe', shared `1dca9f27-c8dd-4c46-bab8-02405cb9d111` = '', and `LookupParameter` returned the first. **Both are shared parameters, not one project and one shared** - a non-shared project parameter has no PROVEN card that makes one. **Seen in passing, and it is [row 5b-1534](../fragment-issues/section-5b-rows-1526-1550.md)'s *"Not checked"* measured, not a new defect:** with the two Keywords bound, `set-material-properties` `fieldValues=Keywords=zzz` on Heron A, rolled back, answered *"1 field(s) changed on 1 of the 1 material(s) named"* - it wrote whichever Keywords Revit handed it, where the readers of row 5b-203 refuse.

**State.** **ANSWERED 2026-11-19 - the owner: arrange two shared "Keywords" parameters, as the run of 2026-11-13 did. Group NK's NK0 and NK8 (a), and the job file's header and note ([`tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml`](../../tools/jobs/proven-reads-repaired-nk-2026-11-11.yaml)), reworded 2026-11-20 to say so: the first bound, "probe" written into it, then the second bound; NK8 (a) deletes the second. No card, C# or test changed.** Tracked as issue [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) until then. Was: **OPEN - an arrangement defect in the register's row and the job's note; no card, no C# and no test is wrong.** NK0 as written cannot fill `list-materials`' case; the scratch project "Heron Reads Test" is arranged the corrected way above for the run of 2026-11-13, and Group NK's record of that run says so. The row's own wording is the owner's call.

---

### Row 5b-1632

*Moved from the register on 2026-11-17.*

**Defect.** **GROUP NK ROW NK8 (c) EXPECTS EVERY VIEW TO BE "NOT READ" ONCE A "DRAWN BY" PROJECT PARAMETER IS BOUND TO VIEWS ONLY - BUT A VIEW HAS NO "DRAWN BY" OF ITS OWN AND A SHEET IS NOT IN THE VIEWS CATEGORY, SO NOTHING CARRIES THE NAME TWICE AND `CHECK_SHEET_SETS_BY_PARAMETER` IS RIGHT TO READ EVERY ONE.** Found 2026-11-13 on the owner's PC. NK8 (c) asks for a Text project parameter "Drawn By" bound to **Views only** and expects *"every view in `ambiguous`, each set NOT CONFIRMED and counted in `unconfirmed`, `inStep` 0"*. **Measured, Revit 2024, session 22684, the scratch copy of "Snowdon Towers Sample Architectural" under `%TEMP%\heron-work\nk`:** sets kept for each Drawn By value with `manage-sheet-sets` (*"'APF' (48); 'Author' (7)"*); a shared "Drawn By" (group `HERON DB`, file `heron-nk-shared.txt`) bound to Views through `add-project-parameter` - shared, not a project parameter, because no PROVEN card makes a non-shared one. [`check-sheet-sets-by-parameter`](../../brain/fragments/check-sheet-sets-by-parameter/fragment.yaml) version 2 then answered `ambiguous` 0, `inStep` 2, `unconfirmed` 0, *"205 sheet(s) and view(s) carry 'Drawn By' with no value, so belong to no set"* - which is what its code does (`view.GetParameters(wanted).Count` per view, sheets included) and what the model holds. **The doubled case is (d), and it was run:** a second shared "Drawn By" (group `HERON DB 2`) bound to **Sheets**, beside Revit's own sheet Drawn By - `ambiguous` 55 (every sheet), `inStep` 0, `outOfStep` 0, *"55 sheet(s) and view(s) carry TWO OR MORE parameters called 'Drawn By' - NOT READ"*. (d)'s *"Nothing was checked"* refusal was not seen, because the Views binding of (c) was left in place - the owner asked that nothing be deleted while he was away - so the views still carry one, blank, Drawn By.

**State.** **ANSWERED 2026-11-19 - the owner: "Drawn By" can sit on views, on sheets or in Project Information, as a project or a shared parameter; Heron checks where it lives in that model first, and when more than one place carries the name it asks the modeller which one before acting - *"it should be checked before acting"*. Recorded as [D-184](../DECISIONS.md). Group NK's NK8 (c) reworded 2026-11-20: it binds the name to views beside the sheets' own, and expects Heron to find both places and ask; by hand the card's answer is the one measured, `ambiguous` 0. The card is unchanged.** Tracked as issue [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) until then. Was: **OPEN - an arrangement defect in the register's row; the card is not wrong.** NK8 (c) as written cannot produce what it expects; (d) is where the refusal is shown. The row's wording is the owner's call.

---

### Row 5b-1634

*Moved from the register on 2026-11-17.*

**Defect.** **GROUP NG ROWS NG6 AND NG7 WRITE "COMMENTS" INTO AIR TERMINAL TYPES - BUT IN A PROJECT A TYPE HAS "TYPE COMMENTS", AND "COMMENTS" IS ONLY ON PLACED ELEMENTS, SO NG6 CANNOT WRITE WHAT IT EXPECTS AND `SET_PARAMETER_VALUES_BY_ID` IS RIGHT TO REFUSE.** Found 2026-11-13 on the owner's PC. NG6 sends `rows=key=Type Name\|in=Air Terminals\|types=yes;6*\|Comments=six` and expects *"600 x 600" and "625 x 625" written, no other type*. **Measured, Revit 2020, session 41776, the scratch project "Heron Many Types Project 2020" under `%TEMP%\heron-work\many-types-2020`** (two scratch families loaded, nine Air Terminal types), rolled back: refused, *"6*: the pattern names 2 type(s) and 2 cannot take these values - 'Heron Many Types 2020: 600 x 600' (id 346225): it has no parameter called Comments; 'Heron Many Types 2020: 625 x 625' (id 346227): it has no parameter called Comments - so NONE of the 2 is written"*. The pattern named the right two types; the parameter is the row's mistake. **Measured beside it, and not counted as NG6:** the same row with `Type Comments=six` - `written` 2, both read back "six" (ids 346225, 346227), nothing else written. NG7's second line uses `Comments` too; its own point - *"another row's key names it too"* - was shown all the same (`600 x 600: another row's key names the same element (id 346225) - not written`), while its `625 x 625` half was refused for the missing Comments.

**State.** **ANSWERED 2026-11-19 - the owner: Revit has two - Comments, an instance parameter on placed elements, and Type Comments, a type parameter; check which one the step means. Checked 2026-11-20: NG6 and NG7 name TYPES (`types=yes`, a Type Name pattern), so both now write Type Comments, with a note in Group NG saying why. The card is unchanged.** Tracked as issue [#916](https://github.com/Ajmalpshaik/Heron-AI/issues/916) until then. Was: **OPEN - an arrangement defect in the register's row; the card is not wrong.** NG6 and NG7 as written ask for a parameter types do not have; `Type Comments` is the type's own. The row's wording is the owner's call.

---

### Row 5b-1640

*Moved from the register on 2026-11-17.*

**Defect.** **`REPORT_STRUCTURAL_LOADS` READS EACH LOAD CASE'S CATEGORY THE WAY [ROW 5b-1637](../fragment-issues/row-5b-1637.md) MEASURED PRINTING `?` FOR EVERY CASE ON REVIT 2020 - THROUGH `LOAD_CASE_SUBCATEGORY`'S STRING - SO ITS *"Load cases (n)"* LIST NAMES NO CATEGORY.** Found 2026-11-13 by reading, while repairing [row 5b-1639](../fragment-issues/row-5b-1639.md) on the same card; **not run**. [`fragment.cs`](../../brain/fragments/report-structural-loads/impl/any/fragment.cs), the `wantCases` block (line 227 on 2026-11-13): `c.get_Parameter(BuiltInParameter.LOAD_CASE_SUBCATEGORY)` and `AsValueString()`, `?` when either is null - the same two lines `CREATE_LOAD_CASE` version 1 carried in `caseLine`, which on Revit 2020 (P1102 STR, session 41776) printed *"1 "DL1" - nature Dead, category ?"*, *"2 "LL1" - nature Live, category ?"* and *"4 "SNOW1" - nature Snow, category ?"*. This card's own runs that day used `show=loads`, which skips the list, so its output was never seen. **What it would be:** `CREATE_LOAD_CASE` version 2's `categoryOf` - `LoadCase.SubcategoryId` matched by id among the subcategories of Structural Load Cases and named by the category's own Name.

**State.** **MOVED TO ISSUE [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - found by reading, not repaired.** Outside the row the card was opened for (5b-1639, whose repair left the C# alone on purpose), so recorded, not fixed (AGENTS.md). Repair it after [Group NX](../needs-checking/group-nx.md) NX3 shows `SubcategoryId` reads on 2020: if it does, the same repair here; if it does not, both cards need another reading.

---

### Row 5b-1642

*Moved from the register on 2026-11-17.*

**Defect.** **`tools/check-gaps.py` CRASHES ON WINDOWS WHEN ITS OUTPUT GOES TO A FILE - `UnicodeEncodeError: 'charmap' codec can't encode character '\u2192'` WHILE PRINTING THE WAITING LIST - AND THE SUITES IT RUNS UNDER IT FAIL ON THE SAME CODE PAGE, SO ITS SUMMARY IS CUT OFF AND ITS UNFINISHED LIST CARRIES TWO SUITES THAT PASS.** Found 2026-11-13 on the owner's PC, `python tools/check-gaps.py > "%TEMP%\heron-work\gaps.txt" 2>&1` from Git Bash, Python 3.12, about forty minutes: the run reached `WAITING - 2668 item(s)` and died at line 605 (`print("     - %s" % what)`) on a register row holding `→`; exit 1 from the traceback, so the exit code said nothing about the UNFINISHED list. Of the six suites that list named, `test_read_back.py` (line 265, *"characters in position 302-306"*) and `test_sheet_list.py` (*"'\u201b' in position 141"*) failed on cp1252 and **pass** run alone with `PYTHONIOENCODING=utf-8`. The other four were real or known: `test_sheet_list_tool.py` ([row 5b-1641](../fragment-issues/row-5b-1641.md)); `test_companion.py` - `ConnectionResetError` [WinError 10054], and the same on a clean worktree of `main` at 10334996, so the machine; `test_retrieve.py` and `test_routing_phrases.py`, this PC's backend (rows 5b-1513, 5b-1514). The same class as [row 5b-1487](../fragment-issues/section-5b-rows-1476-1500.md) (the bridge client) and 5b-1544 (two brain command lines), both repaired by printing UTF-8.

**State.** **MOVED TO ISSUE [#939](https://github.com/Ajmalpshaik/Heron-AI/issues/939) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** `check-gaps.py` neither prints UTF-8 nor hands its suites `PYTHONIOENCODING`; on this PC run it as `PYTHONIOENCODING=utf-8 python tools/check-gaps.py` until it does.

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

---

### Row 5b-1650

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_VIEW_FILTER` PUTS ITS FILTERS ON A VIEW WHOSE TEMPLATE OWNS THAT VIEW'S FILTERS, READS THEIR COLOURS BACK OFF IT, AND SAYS NOTHING ABOUT THE TEMPLATE - THOUGH `REPORT_VIEW_FILTERS` SAYS OF THE SAME VIEW *"Anything changed there is discarded when the template reasserts"*.** Seen 2026-11-14 on the owner's PC running Group NR's NR5 (d), which asks it on purpose. Revit 2020, session 30228, the scratch copy *P1102 MEP NR*. *Level 3 HVAC Plan* was given the template *Mechanical Plan* (`APPLY_VIEW_TEMPLATE`, kept); `REPORT_VIEW_FILTERS` then read `templateOwned true` and *"The view template 'Mechanical Plan' owns the filters on 'Level 3 HVAC Plan' ... change it ON THE TEMPLATE"*; `COPY_VIEW_FILTERS` onto it answered `blockedByTemplate`. `CREATE_VIEW_FILTER perValue=true` with `view=Level 3 HVAC Plan` made four filters *NR2 - ...*, the owner pressed Keep, and its `readBack` said each was *"on 'Level 3 HVAC Plan', line colour read back 235,60,60"* and so on, with no word about the template; `REPORT_VIEW_FILTERS` afterwards listed nine filters on the view, the four new ones among them. The owner's Ctrl+Z took it back (25 filters in the project again). The method stops at step 1 on `templateOwned`; the card itself does not, and its sibling `COPY_VIEW_FILTERS` does.

**State.** **MOVED TO ISSUE [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Whether `CREATE_VIEW_FILTER` refuses a template-owned view as `COPY_VIEW_FILTERS` blocks one, or makes the filters and says they will not stay, is the repair's choice. What a later template re-apply does to the four was not measured.

---
