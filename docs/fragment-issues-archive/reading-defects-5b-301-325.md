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

### Row 5b-305

*Moved from the register on 2026-11-17.*

**Defect.** **LABEL_FAMILY_RADIUS (FRG-GEO-045) PUT ITS LABEL WHERE REVIT ACCEPTS IT AND IGNORES IT: ON THE FINISHED FORM'S ROUND EDGE.** Found 2026-10-03 in Family1 (Revit 2024, the PPR gate valve): a diameter label on the stem extrusion's edge was accepted, and `flex-family` at 14 mm refused - *the dimension labelled Stem Diameter reads 8 mm and the parameter holds 14 mm*. That answered [BM3](../needs-checking/group-bm.md) NO, and it is why that valve was built as nine shape sets switched by `Show NN` formulas. **Measured the same evening, every run rolled back:** a dimension made on the form's SKETCH curve (`Extrusion.Sketch.Profile`), outside sketch mode and with no `SketchEditScope`, goes into the sketch and drives the form - a cylinder flexed from 100 to 300 and 40 mm, on one made in the run and on the valve's stem made days before. A revolve's radius follows a straight dimension from a REFERENCE PLANE through its axis to the profile line (300, 40, back to 50, the axis held); measured from the revolve's own axis line instead, Revit moved the AXIS (to X 2750 from 3000) and the second size broke the solid. **Fixed:** version 2 labels inside the sketch - an extrusion's circle (radial or diameter), or a revolve's radius from a plane the axis lies on, with the axis locked to that plane; a revolve with no such plane, or asked for a diameter, is refused by name.

**State.** **FIXED - PROVEN, signed by Ajmal PS 2026-10-03, run in a scratch family.** Rolled back in Family2 (Revit 2024, session 8804): the label positive and its negative, and `flex-family` with it as setup on a cylinder and a revolve at 300 and 40. Compiles on 2020 to 2027. **For the owner:** BM7 on Revit 2020.

---

### Row 5b-306

*Moved from the register on 2026-11-17.*

**Defect.** **LABEL_FAMILY_RADIUS (FRG-GEO-045) REFUSED A TORUS BOTH WAYS: A REVOLVE WHOSE PROFILE IS A CIRCLE HAD NO ROUND SIZE IT KNEW.** Found 2026-10-03 in Family4 (Revit 2024, unsaved, the TRG_Handwheel rim: profile "circle 46,0 4" on Center (Front/Back), axis vertical). Version 2 knew only a profile LINE alongside the axis, so "Rim Tube Diameter" (8) was refused as *a revolve's round size is a RADIUS* and "Rim Centre Radius" (46) as *no line of the revolve's profile runs alongside its axis 46 mm from it*. **Measured on scratch families, nothing of his touched:** a diameter or radial dimension on the profile circle's sketch arc drives the tube (Family7, Tube Diameter 40 flexed to 80: 480 x 480 x 80 mm; with the ring at 150 and the tube at 10: 310 across). A straight dimension from the axis's plane to the circle's CENTRE POINT is refused by Revit - *Invalid number of references* - with `CurveElement.CenterPointReference` as given and read back from its stable form. Ajmal's own way, read from his Family5: a reference plane through the circle's centre, parallel to the axis's plane, and the dimension between the two planes. That drives the ring (Family7: 400 gave 840 across, 60 gave 160). The centre point does take a locked ALIGNMENT to that plane once its reference is read back from its stable form. In Family8, built as Family4 is, with the plane labelled by LABEL_FAMILY_DIMENSION and no lock, the ring followed BEFORE the lock was added too: the lock is held as insurance, not as the measured cause. **Fixed:** a circle in a revolve's profile now has two sizes. The circle's own radius or diameter is labelled on its arc. Its centre's distance from the axis is labelled from the axis's plane to a plane through the centre: the plane is made when the family has none and named after the parameter, the centre is locked to it, and the axis is locked as before. A plane already labelled with the same parameter from the axis's plane is used as it stands, and is not refused as a second label. When one value matches more than one size of the revolve, the run is refused and asks for the label to be placed by hand.

**State.** **FIXED - PROVEN, signed by Ajmal PS 2026-10-04, run in a scratch family.** Rolled back in scratch Family16 (Revit 2024, session 8804): the ring labelled from a plane the call made through the circle's centre, and the refusal of a 40 mm size the torus does not have. Measured before that on scratch Family7 and Family8 (Revit 2024, session 8804), kept in those scratch families and flexed rolled back. Compiles on 2020 to 2027. The handwheel was rebuilt in Family9 with its 12 mm tube, and that family work was closed by Ajmal on 2026-10-04.

---

### Row 5b-308

*Moved from the register on 2026-11-17.*

**Defect.** **NO TOOL SWITCHED A FAMILY PARAMETER BETWEEN TYPE AND INSTANCE, SO A FAMILY BUILT ON TYPE SIZES COULD NOT BE MADE TO FOLLOW ITS PIPE.** Found 2026-10-04 by Ajmal on his PPR gate valve (TRG_PLMB_VLV_PPR Gate Valve_GV_R0, Revit 2024): its 36 size parameters are TYPE parameters - Nominal Diameter drives the connectors and 35 are formulas off it - so the valve does not resize to the pipe it sits on, while the two PPR check valves, built with INSTANCE parameters, do. Revit has `FamilyManager.MakeInstance` and `MakeType` on every release 2020 to 2027; nothing in the library called them, and ADD_FAMILY_PARAMETERS told the modeller to change scope by hand in Family Types. **Fixed by widening ADD_FAMILY_PARAMETERS (FRG-PAR-022) to version 2**, not by a new tool: `switchExisting` true switches named parameters, or `all`, to the scope `instance` names. A type formula cannot read an instance parameter, so the order is worked out from the formulas (to instance: what reads, before what is read; to type: the reverse), and a switch that would leave a type formula reading an instance parameter is refused naming it, before anything changes. A parameter a nested family's TYPE parameter is linked to, a type catalogue column and a built-in are refused by name too. Every parameter is read back - scope, formula, associated connector and nested parameters, labelled dimensions - all or nothing.

**State.** **FIXED - DRAFT until signed (D-30).** Measured on scratch Family17 (Revit 2024, session 8804, unsaved, made for it): all to instance switched C, B, A and read back with the connector on A and the label on B kept; A alone refused naming B; back to type switched A, B, C; C alone refused naming C. Compiles on 2020 to 2027. Group CA owes the signature, the nested-link and catalogue refusals on a real family, and 2020.

---

### Row 5b-314

*Moved from the register on 2026-11-17.*

**Defect.** **THE LOADS PANEL'S FINALIZE IS TWO UNDO ENTRIES, NOT ONE.** Built 2026-10-04 with the building loads feature ([docs/44](../44-building-loads-from-the-model.md)): Finalize writes Design Cooling Load, Design Heating Load and Specified Supply Airflow into every calculated Space through ONE SET_PARAMETER_VALUES_BY_ID (the Companion table's own Apply - stale rows refuse it whole), then each Space's flow across its diffusers through ONE SET_AIR_TERMINAL_FLOW, chained after FILTER_ELEMENTS_BY_ID. Each is its own revit_change, so Revit's undo list holds two entries, and Article 8 asks for one per request. The panel says "two undo entries" beside the button and in its answer.

**State.** **MOVED TO ISSUE [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** One entry needs both writes inside one TransactionGroup, which is an add-in change (a request that carries two fragments, or a group the bridge opens around a chain) - not something the server can do by calling revit_change twice. Until then the modeller undoes twice, and the second write runs only if the first was kept. Group [CC](../needs-checking/group-cc.md) row CC11 checks the two entries on a real model.

---

### Row 5b-318

*Moved from the register on 2026-11-17.*

**Defect.** **A PROFILE CORNER A HAIR ACROSS ITS AXIS PASSED THE REVOLVE'S OWN CHECK, AND REVIT BUILT A REVOLVE WITH NO SOLID IN IT.** Found 2026-10-04 on Family3 (unsaved family, Revit 2024, session 18156) building a power transformer. A porcelain bushing revolve on reference plane "Phase A" (facing left or right, X -1200), axisLine `-900.0,3100.0 -2775.0,6347.6` (tilted 30 degrees), built and read back. A turret revolve on the same plane and axis, profile `-900,3100; -696.5,3217.5; -1096.5,3910.3; -1035.9,3945.3; -1055.9,3979.9; -1320,3827.4` (radius 235 then 305, 840 mm along the axis), was refused by CREATE_FAMILY_REVOLUTION (FRG-GEO-039, PROVEN) with *"The revolve was built and has no extent to read back. NOTHING from this call was kept."* The same happened on Phase B and Phase C and with a variant shape; a ring at radius 232 to 240, touching no axis, built. **It looked like Revit merging a revolve that overlaps an earlier one. It is not.** Three rolled-back `validate --in "Family3" --write` runs, same session: (1) the turret as given - the same refusal; (2) the same turret moved 10 m up the plane, overlapping nothing - the same refusal; (3) the turret in its original place, still enclosing the bushing, with only the last corner moved 1 mm onto the profile's side, `-1319.13,3827.96` - **built**, X -1505.37 to -894.63, Y -1584.44 to -696.5, Z 2982.51 to 3980.12 mm, volume 150.458 L (pi x (0.305^2 x 0.04 + 0.235^2 x 0.8) m3 = 150.49 L), transaction group ROLLED BACK. The cause is the rounding: `-1320,3827.4` lies **0.034 mm on the far side** of the axis while every other corner is 235 or 305 mm on the near side. The fragment's side check ignores anything within `ShortCurveTolerance` (about 0.78 mm) of the axis, so the profile passed as touching it; Revit then built a revolve whose profile crosses its axis by a sliver, with no faces and no bounding box, and the fragment reported that as "no extent". Extrusions never showed it because they have no axis.

**State.** **FIXED - PROVEN again, signed by Ajmal PS on 2026-10-04 from the M_Fixed run below.** Before the curves are made, a corner lying within `ShortCurveTolerance` of the axis is put on the axis exactly, along its normal, and the reply says how many were moved; a side that this leaves shorter than Revit's shortest line is refused by name. The empty-solid refusal now names the likely cause. Run 2026-10-04 on a scratch copy of `M_Fixed` opened from Project2 (Revit 2024, session 18156; Family3 had been closed), every run rolled back: the turret exactly as given BUILT on "Center", volume 150.464 L, with "1 corner(s) lay within 0.78 mm of the axis and were put on it exactly"; the same turret with that corner 5 mm across, `-1324.33,3824.96`, refused as crossing its axis; the cylinder (21.208 L) and the 32 mm sphere (0.137 L) from the cases read as before, with no corner moved. Compiles on 2020 to 2027. The new proof replaces the 2026-10-03 one; like it, no second route by hand was run.

---

### Row 5b-319

*Moved from the register on 2026-11-17.*

**Defect.** **A MODELLER ASKING HERON TO PLACE DOORS AND WINDOWS WAS HANDED THE LOADS TAKE-OFF, A READ.** Found 2026-10-04 by the session building a test building in Project2: `heron_lookup` *"place doors and windows hosted in walls at points"* answered REPORT_SPACE_ENVELOPE (*words #4 + nearness #3*), with SELECT_BY_HOST, CREATE_WALL, DIMENSION_WALL_OPENINGS and REPORT_DOOR_ROOM_LINKS behind it and PLACE_HOSTED_FAMILY - the tool that places them - not in the five. The same session then found *"read the thermal U value of every floor type and roof type"* answered CREATE_ROOF, a MODIFY. **NOT A STALE INDEX:** the shared store's rows, searchable text and vectors equalled `main`'s cards for every card it held, and a private store built fresh from `main` gave the same answer, candidate for candidate. **TWO CARDS, MEASURED.** (1) The trained search model reads only the first 512 word-pieces of a card - identity, utterances, then the purpose (row [320](../FRAGMENT-ISSUES.md#)) - and #410 opened PLACE_HOSTED_FAMILY's purpose with its version-3 sill rules. The card is far longer (measured 2026-10-04: 1,355 word-pieces), so the model stopped reading its door-and-wall text: the phrase's nearness rank for that card was **1** before #410 and **35** after, its cosine 0.46 then 0.22. (2) The words route matches a typed word as a **prefix**, so *walls* never matches *wall* and *doors* never *door*: every phrasing PLACE_HOSTED_FAMILY declared was singular, while REPORT_SPACE_ENVELOPE's (#409) were plural and twice said *outside walls*. Before #409, and between #409 and #410, the door tool won the phrase.

**State.** **FIXED 2026-10-04 in the cards - routing, measured without Revit.** PLACE_HOSTED_FAMILY: a plain summary of what a modeller asks it for now opens the purpose, the two rules that still hold sit above the sill rules and the history below them, five plural phrasings were added, and a comment says the top of the purpose is what the search reads. REPORT_SPACE_ENVELOPE: the one phrasing that named *outside walls*, where the take-off reads every face, now names *every space's walls, roofs, floors and glass with their U-values*, and one more asks *what U-value does each roof and floor type have* - roof and floor and not wall, because *wall* in it, measured that day, sent the outside-walls request straight back to the take-off. SELECT_BY_HOST: three question phrasings (row [321](../FRAGMENT-ISSUES.md#)) - one first worded *which doors and windows are in these walls* moved it one place nearer *isolate all the pipes* and tipped that request from ISOLATE_ELEMENTS to CAP_OPEN_PIPE_ENDS, a change, on a margin of 0.00004 that `main` already had; reworded, it does not. **Measured on private stores built from the branch, on the trained model the owner's PC uses:** the four phrasings of the brief 0 then 4 of 4; 34 door-placement phrasings 1 then 30, 26 of them written before any wording was chosen; the take-off's own 15 phrasings 15 then 15; twelve neighbouring requests unchanged; the owner's 79 questions (`tools/score-routing.py`) unchanged but for #62, *not found* then 3rd; the session's U-value question CREATE_ROOF then REPORT_SPACE_ENVELOPE, a READ - though it reads the U-value only of the types around Spaces - and of six U-value phrasings written before any wording, 3 went to a change on `main` and none after. **On spelling alone, as CI ranks:** the brief 2 then 3 of 4 (*add windows to the outside walls* goes to DIMENSION_WALL_OPENINGS), placement 16 then 24 of 34, the owner's 79 unchanged, the session's U-value question CREATE_ROOF then REPORT_SPACE_ENVELOPE, and 3 then 2 of the six on a change. Guarded by [`tests/test_routing_phrases.py`](../../tests/test_routing_phrases.py), on the trained model, watched failing 5 checks on `main`'s cards, and by the U-value question added to [`check-risk-crossings`](../../tools/check-risk-crossings.py). **Live on the owner's PC** once the merge is in the main checkout, the shared store rebuilt from it and the Claude app restarted.

---

### Row 5b-320

*Moved from the register on 2026-11-17.*

**Defect.** **THE TRAINED SEARCH MODEL READS ONLY THE FIRST 512 WORD-PIECES OF A CARD, AND A LONG CARD'S OPENING DECIDES WHAT IT IS NEAR.** Found 2026-10-04 beside row [319](../FRAGMENT-ISSUES.md#). `heron_embed._text_for` joins identity, capability, domain, utterances and the whole purpose, and `model2vec`'s `encode` cuts at `max_length=512` unless told otherwise - nothing in Heron said so. Measured 2026-10-04: 126 of the 469 cards are longer than that. So *whatever opens a long card's purpose is what nearness thinks the card is*: a new version's notes written at the top, as #410 did, silently move a tool away from its own job, and history paragraphs pull questions toward the tool they describe. On PLACE_HOSTED_FAMILY as it stood on `main`, the same text against row 319's phrase: cosine 0.50 cut at 128, 0.39 at 256, 0.22 at 512, 0.32 uncut. Every routing check read past it: `check-routing` asks declared words, which answer by identity, and CI ranks by spelling, which has no cut.

**State.** **MOVED TO ISSUE [#957](https://github.com/Ajmalpshaik/Heron-AI/issues/957) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Changing what is embedded - identity, utterances and the purpose's first paragraph, say - re-ranks every card on the owner's PC, and needs its own change measured against his 79 questions. Until then the rule is in PLACE_HOSTED_FAMILY's card as a comment: **a purpose opens with what a modeller asks the tool for, and a new version's rules go below that, never above it.** A check naming the cards whose first 512 word-pieces never reach their own job would find the next one before a modeller does.

---

### Row 5b-321

*Moved from the register on 2026-11-17.*

**Defect.** **QUESTIONS ABOUT THE DOORS AND WINDOWS ALREADY IN A WALL GO TO TOOLS THAT CHANGE THE MODEL.** Measured 2026-10-04 on 22 such questions while fixing row [319](../FRAGMENT-ISSUES.md#), on the trained model. On `main`, 8 went to a MODIFY: CREATE_STAIRS (*which doors are hosted in the stair core wall*, *are there any doors in the stair wall*), DIMENSION_WALL_OPENINGS (*how many windows are in this wall*, *which windows sit in this wall*), CREATE_WALL (*list the doors in the corridor walls*), CREATE_ROOF (the U-value question), SET_WALL_CONSTRAINTS (*what is in this wall*) and PLACE_HOSTED_FAMILY (*what is the sill height of these windows*). After 319's fix it is still 8, and not the same 8: the stair-core and U-value questions now reach a READ, *which doors are in this wall* and *count the windows in the external walls* now reach PLACE_HOSTED_FAMILY, and two more moved from one change to another - *how many windows are in this wall* and *list the doors in the corridor walls* to PLACE_HOSTED_FAMILY. On spelling alone it is 7, then 9. **#410 had hidden part of this:** before it, *select the doors hosted in this wall* and *what room does this door swing into* went to PLACE_HOSTED_FAMILY too. **Why:** SELECT_BY_HOST owns these questions and its card is mostly duct insulation and MEP fittings - the model ranks it 10th for *which doors are in this wall* while the words rank it 1st - and every history paragraph on PLACE_HOSTED_FAMILY's card (the host, a wall selected by hand, the swing, the To Room) is nearer to some question than to placing anything.

**State.** **MOVED TO ISSUE [#955](https://github.com/Ajmalpshaik/Heron-AI/issues/955) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** `heron_lookup` sends the risk with every candidate and warns when a question lands on a change, and PLACE_HOSTED_FAMILY cannot run without the door type, points and rooms a question never gives, so nothing changes unasked - but the answer is still wrong. Row 319 gave SELECT_BY_HOST three question phrasings and went no further. The repair is SELECT_BY_HOST's own card - doors and windows first, a count beside the selection - and *what is the sill height of these windows* sent to READ_ELEMENT_PARAMETERS: each its own change, measured on the same 22 questions. **2026-10-09:** SELECT_BY_HOST's opening reworded as this row says: doors and windows first ("Finds the doors and windows already cut into a wall - selects them, and counts them"), the count beside the selection described, "A read, never a change". Separately, *what is the sill height of these windows* is declared on READ_ELEMENT_PARAMETERS ("Sill Height" in RevitAPI.xml, every release; utterance only, proof stands). Card words only; SELECT_BY_HOST is DRAFT. The phrasings were written before the wording: the 12 questions this row names, 14 more questions and 10 placement requests. A held-out set of 12 questions and 4 requests was written at the same time and never used to choose. Measured through heron_retrieve.find on private stores, before -> after. **Trained model:** questions reaching a write 13 -> 7 of 26. Of the row's own 12 it is 7 -> 6, and only the sill height moved: *which doors are in this wall*, *which windows sit in this wall*, *list the doors in the corridor walls*, both stair-wall questions and *what is in this wall* still reach a change. Requests sent to the wrong tool 1 -> 1. Held-out: 4 -> 2 questions, requests 1 -> 1. **Spelling:** questions 10 -> 10. *show me every window hosted in the facade walls* left PLACE_HOSTED_FAMILY, but *which doors are hosted in the stair core wall* moved to CREATE_STAIRS on a margin of 0.04 of a rank; the row's 12 went 4 -> 5. Requests 3 -> 4 (*put two windows in the north wall* -> SELECT_BY_HOST, a read). Held-out: 5 -> 3 questions, requests unchanged. **WHY NOT FURTHER, MEASURED:** a door-and-window heavier opening reached 7 of 26 on the model without the sill declaration. But it tied *place doors and windows hosted in walls at points* (5b-319, in test_routing_phrases) to this read on fused score, and the tie went to the read on words. About 390 wordings were swept on both backends and none was clean on both. The brief now wins by 2 ranks. Read-to-read cost: *show me what is hosted on this ceiling* -> CHECK_CEILING_COORDINATION on the model; on spelling, three of this card's own utterances lost nearness #1 but keep words #1 and identity. The rest would need PLACE_HOSTED_FAMILY's history paragraphs dealt with, as this row says; that card was not touched. Guarded by test_routing_phrases: three moved questions, each at least 1.9 ranks clear, plus the sill height in section 8. Seen failing on the cards as they stood.

---
