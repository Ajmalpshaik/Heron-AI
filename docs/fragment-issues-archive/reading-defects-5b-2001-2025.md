# FRAGMENT-ISSUES archive — section 5b, rows 2001 to 2025

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
> Rows 2001 to 2025 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-2001

*Moved from the register on 2026-11-24.*

**Defect.** **`SET_FAMILY_FORMULA` READ AN INTEGER FORMULA'S VALUE BACK BLANK: [`brain/fragments/set-family-formula/impl/any/fragment.cs`](../../brain/fragments/set-family-formula/impl/any/fragment.cs) VERSION 1 READ EVERY VALUE WITH `FamilyType.AsValueString`, AND FOR AN INTEGER PARAMETER THAT GAVE NOTHING - *""Blade_Count" = rounddown((Test_Height - 36 mm) / 45 mm), read back from the family. In the current type "Scratch" it gives ."*** Found 2026-11-23 building a parametric louvre family in Revit 2020 (session 49564, add-in 0.3.0.0). The same happened for `Blades_Up = if(rounddown(...) < 2, 2, rounddown(...))`; both were INSTANCE Integer parameters. The parameters worked - a flex read 5, 7, 12 and 25 blades - and length formulas read back fine (*"it gives 594.0"*), so only the read-back was wrong. **Fixed in this change, in code only:** version 2 reads the value by the parameter's kind - `FamilyType.AsInteger` for an Integer, *Yes* or *No* for a Yes/No (found by `SpecTypeId.Boolean.YesNo` from 2022 and `ParameterType` before, the check `CREATE_SHEETS` already makes), `AsString` for a text, and `AsValueString` for a length or other measured value as before - on the set path and on the refusal path, and says *"no value Revit would show"* rather than a blank when the type holds none. The card is back to `DRAFT` (was `PROVEN` version 1). It compiles for Revit 2020 to 2027 (`tools/check-fragments-compile.py`, 2026-11-23); that is not a proof ([D-30](../DECISIONS.md)). Not measured here: whether `AsValueString` returns an empty string or null for an Integer - either now reads the same. The Revit proof, positive and negative, is owed as [NEEDS-CHECKING Group PX](../needs-checking/group-px.md), PX3 to PX5.

**State.** **MOVED TO ISSUE [#1296](https://github.com/Ajmalpshaik/Heron-AI/issues/1296) 2026-11-23** - still open, tracked there. Was: **OPEN 2026-11-23 - fixed in code in this change, not yet run in Revit.** Written in a cloud session with no Revit; the card stays `DRAFT` until Group PX is run on the owner's PC.

---

### Row 5b-2003

*Moved from the register on 2026-11-24.*

**Defect.** **TWO OLD CAD-TO-DIRECTSHAPE JOBS GIVE THE CARD `Ducts` AS THE NEGATIVE CATEGORY, WHICH REVIT MAY ACCEPT, AND ONE OF THEM IMPORTS A PLAN EXPORT THAT MAY CARRY NO SOLIDS.** Found 2026-11-21 by the session that recorded [row 5b-1977](../fragment-issues/row-5b-1977.md), and handed on 2026-11-23 by the session that fixed that row - **read from the files named, not run or measured in Revit**. [`tools/jobs/revit2020-scratch-round17-2026-10-18.yaml`](../../tools/jobs/revit2020-scratch-round17-2026-10-18.yaml) (lines 49-53) and [`tools/jobs/sweep-snowdon.yaml`](../../tools/jobs/sweep-snowdon.yaml) (lines 109-113) give [`convert-cad-to-directshape`](../../brain/fragments/convert-cad-to-directshape/fragment.yaml) the negative `category: "Ducts"`. The card makes DirectShapes in that category and its `impl/any/fragment.cs` asks `DirectShape.IsValidCategoryId` (line 37); `Ducts` is a model category that call may well accept, so the negative may make shapes rather than be refused. Round 17's import, `HeronPlan.dwg`, is a plan export that probably carries no solids, so its positive may make nothing. **Not checked:** either - both need a Revit and the import on a scratch copy of a model.

**State.** **MOVED TO ISSUE [#1303](https://github.com/Ajmalpshaik/Heron-AI/issues/1303) 2026-11-23** - still open, tracked there. Was: **OPEN 2026-11-23 - recorded, not fixed.** On a scratch copy in Revit, ask the card to make DirectShapes in `Ducts` from an import that has solids, and say whether it is refused. If it is not, the negative in those two job files is not a different case and they are corrected or removed as row 5b-1977's three were. Then say whether `HeronPlan.dwg` has any solids.

---

### Row 5b-2005

*Moved from the register on 2026-11-24.*

**Defect.** **DOOR 422466 IN *House Scratch 1042* IS DESCRIBED TWO WAYS - [OU6](../needs-checking/group-ou.md) AND [`flip-elements`](../../brain/fragments/flip-elements/tests/cases.yaml) VERSION 4 - AND ONLY THE MODEL SAYS WHICH IS TRUE.** Found 2026-11-21 and handed on 2026-11-23 by the session that fixed [row 5b-1973](../fragment-issues/row-5b-1973.md) - **read from the files, not run in Revit**. The case says door 422466 has its From and To the wrong way round for its swing; OU6 runs `fromToRoomOnly` on the same door *"with From and To already right"*. Both texts now say to READ the door first; neither can be corrected without the model. **Not checked:** in Revit.

**State.** **MOVED TO ISSUE [#1307](https://github.com/Ajmalpshaik/Heron-AI/issues/1307) 2026-11-23** - still open, tracked there. Was: **OPEN 2026-11-23 - recorded, not fixed.** On a scratch copy of *House Scratch 1042*, read door 422466's From and To with `REPORT_DOOR_ROOM_LINKS` on *Working Drawings*, say which description is true, and correct the other text in `flip-elements/tests/cases.yaml` or OU6.

---

### Row 5b-2006

*Moved from the register on 2026-11-24.*

**Defect.** **[`measure-distance`](../../brain/fragments/measure-distance/fragment.yaml) AND [`measure-available-fall`](../../brain/fragments/measure-available-fall/fragment.yaml) HAVE NO WAY TO BE HANDED TWO DIFFERENT ELEMENTS.** Found 2026-11-21 ([row 5b-1970](../fragment-issues/row-5b-1970.md)) and handed on 2026-11-23 by the session that wrote that row's interim fix - **read from [`RevitFragment.cs`](../../revit/Heron.Revit.Addin/RevitFragment.cs) `OneElement`, not run in Revit**. The only word that binds a single `Element` is `selected`, it means exactly one element and binds it to both needs, and any typed text is refused. The cards' `tests/cases.yaml`, Group PE's PE5 and `generate-jobs.py` now say two elements cannot be handed over yet; the way to hand them over is a binder choice (typed ids, a second word, or the cards reshaped to one list) and what may be typed is D-54's to rule. **Not checked:** in Revit.

**State.** **MOVED TO ISSUE [#1308](https://github.com/Ajmalpshaik/Heron-AI/issues/1308) 2026-11-23** - still open, tracked there. Was: **OPEN 2026-11-23 - recorded, not fixed.** The owner chooses how two particular elements are named: typed ids for a singular `Element` need, a second word for a second selected element, or the two cards reshaped to take one list. Then the binder is changed, the cards and PE5 are rearranged, and `generate-jobs.py` stops marking them.

---

### Row 5b-2007

*Moved from the register on 2026-11-24.*

**Defect.** **THREE DRAFT CARDS DECLARE A VALUE AS `role: result` THAT THEIR OWN PRESCRIBED NEGATIVE ALWAYS FILLS - [`create-from-room-boundaries`](../../brain/fragments/create-from-room-boundaries/fragment.yaml) `finishSummary`, [`report-structural-connections`](../../brain/fragments/report-structural-connections/fragment.yaml) `typeCount`, [`place-accessory-on-run`](../../brain/fragments/place-accessory-on-run/fragment.yaml) `placed`.** Found 2026-11-21 ([row 5b-1968](../fragment-issues/row-5b-1968.md), items 1, 4 and 6) and handed on 2026-11-23 by the session that fixed that row's other three cards - **read from the files named, not run in Revit**. The group rows HX2, LD2, PK3 and OR9 (b) now say a batch reads their negative NEG NOT EMPTY. Whether each value could honestly be `role: accounting` (for `typeCount` it counts every connection type whatever is selected; for `placed` it may be the card's real answer, and then the row's negative is what changes) is each card's author's call, and a contract change is a card version. **Not checked:** in Revit.

**State.** **MOVED TO ISSUE [#1308](https://github.com/Ajmalpshaik/Heron-AI/issues/1308) 2026-11-23** - still open, tracked there. Was: **OPEN 2026-11-23 - recorded, not fixed.** For each card, one of two edits: the always-filled value becomes `role: accounting` in the contract (a card version), or the group row and `tests/cases.yaml` prescribe a negative where every declared result is empty.

---

### Row 5b-2009

*Moved from the register on 2026-11-24.*

**Defect.** **`SET_FAMILY_FORMULA` VERSION 2's ORDINARY JOB - SETTING A FORMULA - CANNOT BE SIGNED: ITS CARD [`brain/fragments/set-family-formula/fragment.yaml`](../../brain/fragments/set-family-formula/fragment.yaml) DECLARES `cleared` (a bool, false whenever a formula is set) AS `role: result` BESIDE THE TWO STRING RESULTS `formulaNow` AND `valueNow`, SO [`brain/heron_validate.py`](../../brain/heron_validate.py) `positive_worked` SETS THE STRINGS ASIDE AND JUDGES THE RUN ON `cleared` ALONE - *"POSITIVE EMPTY - every declared result came back zero (cleared); beside a count, a sentence is not counted (formulaNow, valueNow)."*** Measured 2026-11-24 in Revit 2020 (session 29500) on the scratch family PX Box: the positive set `rounddown((Height - 36 mm) / 45 mm)` and read back `formulaNow rounddown((Height - 36 mm) / 45 mm)`, `valueNow 4`, `cleared false` - the fragment worked - and `python brain/heron_validate.py accept` refused it with the words above. The rule that did it is D-51's *a sentence never outranks a count* (the block in `positive_worked` beginning *"THE COUNTS DECIDE (D-51), AND A SENTENCE NEVER OUTRANKS ONE"*, added by [row 5b-411](../fragment-issues/section-5b-rows-401-425.md)); version 1 was signed 2026-10-03 with the same contract shape, before that rule. **What was done instead, by the owner's choice:** the signed positive was re-run CLEARING a formula (`formula=` on `Neck Top`, `cleared true`), which passes; the set-path read-backs (4, Yes, No, 350.0) were run rolled back and recorded as [NEEDS-CHECKING Group PX](../needs-checking/group-px.md) results, not as the signed positive. So a signature on version 2 proves clearing, not setting. **Fix options - the owner chooses:** (a) declare `cleared` as `role: accounting` in the contract; (b) add a counted result such as `set` (true when a formula was set); (c) let `accept` honour a declared `expect:` that names the string results. (a) and (b) are a card version. **Do not do it twice:** a separate session is already moving this card's C# to version 3 for a different defect - a self-referencing formula (`Neck Top + 10 mm` on `Neck Top`) comes back `fragment_threw` instead of `refused` (Group PX, PX5's negative) - so whoever takes this row folds the contract change into that version or waits for it. **Not checked:** whether any other DRAFT or PROVEN card pairs a bool `role: result` with string results the same way (a grep of the cards' `outputs:` would say); option (c)'s effect on the 5b-411 case `set-material-colour`.

**State.** **MOVED TO ISSUE [#1322](https://github.com/Ajmalpshaik/Heron-AI/issues/1322) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - recorded, not fixed.** One of the three fixes above, on the owner's word, then the set path - PX3's `valueNow 4` - re-run and signed as the positive.

---

### Row 5b-2010

*Moved from the register on 2026-11-24.*

**Defect.** **`SET_FAMILY_FORMULA` THREW REVIT'S REFUSAL INSTEAD OF RETURNING IT: [`brain/fragments/set-family-formula/impl/any/fragment.cs`](../../brain/fragments/set-family-formula/impl/any/fragment.cs) VERSION 2 CAUGHT `FamilyManager.SetFormula`'S FAILURE AND THREW IT AGAIN, SO A FORMULA REVIT WILL NOT TAKE ENDED `fragment_threw` - HOW A CRASH LOOKS - NOT THE CARD'S `refused` OUTPUT.** Measured 2026-11-24 proving version 2 on Revit 2020 (session 29500, add-in 0.3.0+bf62abe3b, scratch family *PX Box*): parameterName `Neck Top`, formula `Neck Top + 10 mm` (a formula that uses the parameter it sets) came back *"'set-family-formula' threw while running: Revit refused the formula "Neck Top + 10 mm" on "Neck Top": Formula setting failed. Names are written bare, spaces and all; a unit may follow a number (60 mm); and a formula cannot use the parameter it sets, directly or through another. Nothing was kept."* The words were right; where they came out was wrong, so the loop negative of [NEEDS-CHECKING Group PX](../needs-checking/group-px.md) PX5 failed. The same tool's missing-parameter refusal came back correctly through `refused`. **Fixed in this change, in code only:** version 3 runs `SetFormula` and the regenerate inside a `SubTransaction`; on Revit's refusal it is rolled back and the same words come back through `refused` and `findings`, nothing kept. `formulaNow` and `valueNow` are then read from the family after the rollback, as every other refusal of this card does when the parameter exists (its own rule: an empty `formulaNow` means "no formula", never "nothing was looked at") - so they show what Neck Top held before the call, not blanks. The card stays `DRAFT`, version 3. Compiling for Revit 2020 to 2027 is not a proof ([D-30](../DECISIONS.md)). Not checked here: Revit's exact exception text on other releases. The Revit run is owed as Group PX **PX6**.

**State.** **MOVED TO ISSUE [#1325](https://github.com/Ajmalpshaik/Heron-AI/issues/1325) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - fixed in code in this change, not yet run in Revit.** Written in a cloud session with no Revit; the card stays `DRAFT` until PX6 is run on the owner's PC.

---

### Row 5b-2012

*Moved from the register on 2026-11-24.*

**Defect.** **ON THE TRAINED MODEL ALONE, *"SELECT ALL ELEMENTS OF TYPE CURTAIN WALL"* NOW REACHES [`SET_CURTAIN_PANEL_TYPE`](../../brain/fragments/set-curtain-panel-type/fragment.yaml), A WRITE, WHERE IT REACHED `REPORT_CURTAIN_WALL_TYPE`, A READ - ONE MORE OF ROW 5B-329'S GUARDS IN `tests/test_routing_phrases.py` FAILING.** Measured 2026-11-24 with `tools/score-routing.py --sentences` on row 5b-329's five selection sentences, main (`bf62abe3`) against the WN-11 branch (`32697ec4`): the **model** backend swapped its top two, `REPORT_CURTAIN_WALL_TYPE` then `SET_CURTAIN_PANEL_TYPE` on main, the other way on the branch; **lexical, lexical+rerank and model+rerank did not change** (rerank: 0 of 5 reach a write before and after). The suite fails 6 checks on the branch where it fails 5 on main in the same container (system Python, model2vec, no re-ranker); CI does not run it. Taking out one card at a time on a scratch tree: the swap goes away only without [`edit-curtain-grid-segments`](../../brain/fragments/edit-curtain-grid-segments/fragment.yaml), whose own card is not in the top two - it ranks between the two near-tied cards and tips their fused order. Fewer *"curtain wall"* words in its sentences did not move it. `SET_CURTAIN_PANEL_TYPE`'s own routing table already records this sentence flipping on a first wording of that card. The sentence is a guard written for the test, not a modeller's words, so it is not declared on a card to buy it back. **Not checked:** in Revit.

**State.** **MOVED TO ISSUE [#1328](https://github.com/Ajmalpshaik/Heron-AI/issues/1328) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - recorded, not fixed.** The repair is row 5b-329's: a select-by-type answer that wins these paraphrases on the trained model by its own words, so no near tie between two curtain wall cards decides them.

---

### Row 5b-2013

*Moved from the register on 2026-11-24.*

**Defect.** **`LINK_FAMILY_PARAMETER` REFUSED A PLAIN ELEMENT ID IN `forms`: [`brain/fragments/link-family-parameter/impl/any/fragment.cs`](../../brain/fragments/link-family-parameter/impl/any/fragment.cs) VERSION 6 READ EVERY ID WITH `Document.GetElement(string)`, WHICH KNOWS UNIQUE IDS ONLY - SO THE NUMBER EVERY READ TOOL PRINTS FOR AN ELEMENT WAS ANSWERED *"Nothing was changed. No element in this family has the id"*.** Measured 2026-11-24 in Revit 2020 (session 29500, add-in 0.3.0) on a mechanical equipment family open in the Family Editor, wiring an inertial air filter's airflow parameters to its nested shared bleed fan: `REPORT_PARAMETER_INVENTORY`, after `SELECT_BY_PARAMETER_VALUE` Family contains "Centrifugal Fan", printed the nested fan copy as `ELEMENT id 9659268 ... unique id 79c6e3cb-4346-46ea-91fe-31cc4c8d455c-00936384`. `forms=9659268`, `links=Actual Exhaust Air Flow=Actual Exhaust Air Flow` was refused as above; the same call with `forms=79c6e3cb-4346-46ea-91fe-31cc4c8d455c-00936384` changed 1 link and read it back. The card said forms are named *"by the ids the form tools or PLACE_FAMILY_INSTANCES gave back"*, and those give unique ids, so the read tools' number was never in its contract - but it is the id a modeller sees. **Fixed in this change, in code only:** version 7 reads an id of digits alone as the element id - an `ElementId` built from a `long` from Revit 2024 on and an `int` before (`REVIT2024_OR_GREATER`) - and anything else as a unique id, as before; an id that names nothing is still refused, and the refusal now says both kinds are taken. The same path serves forms, nested copies, lines, regions, model text and reference points. The card is back to `DRAFT` (was `PROVEN` version 6, its proof block kept as the record). Compiling it for Revit 2020 to 2027 is not a proof ([D-30](../DECISIONS.md)). The Revit proof, positive and negative, is owed as [NEEDS-CHECKING Group QE](../needs-checking/group-qe.md), QE1 and QE2. **Not checked:** whether other cards that take ids in a typed string read them with `GetElement(string)` alone - `grep -rn "GetElement(part)\|GetElement(word)" brain/fragments/*/impl/any/fragment.cs` is where to start.

**State.** **MOVED TO ISSUE [#1332](https://github.com/Ajmalpshaik/Heron-AI/issues/1332) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - fixed in code in this change, not yet run in Revit.** Written in a cloud session with no Revit; the card stays `DRAFT` until Group QE is run on the owner's PC.

---

### Row 5b-2014

*Moved from the register on 2026-11-24.*

**Defect.** **ON LEXICAL+RERANK, *"FIND THE LEVEL NAMED ROOF AND SELECT IT"* - A SELECT REQUEST - NOW REACHES `REMOVE_MASS_FLOORS`, A WRITE, SINCE THE NEW READ `REPORT_WALL_ATTACHMENTS` ENTERED ITS SHORTLIST.** Found 2026-11-24 in a cloud container (no Revit), measuring pull request #1331 ([S10] Read card: walls attached to a roof, row [5b-1924](../fragment-issues/row-5b-1924.md)) with `tools/score-routing.py --sentences` on `tests/data/routing-sentences-2026-11-21-row132.yaml`, origin/main `bf62abe3` against the branch, lexical+rerank in a venv holding `cross-encoder/ms-marco-MiniLM-L-6-v2`, no filter, `--revit 2024` and `--revit 2026` alike. On main it reached `READ_ELEMENT_TABLE` (a read, also not the `SELECT_BY_PARAMETER_VALUE` the set wants); on the branch the re-ranker's shortlist holds [`REPORT_WALL_ATTACHMENTS`](../../brain/fragments/report-wall-attachments/fragment.yaml) - its words say *roof* - in second place, `READ_ELEMENT_TABLE` has left it, and the re-ranker puts `REMOVE_MASS_FLOORS` (*"remove the mass floor at the roof level"*) first. The tool counts it **neutral** (wanted neither before nor after); it is recorded because a select request now first on a write is worse than one first on a read. Lexical, model and model+rerank: unchanged. **Tried and taken back:** two of the new card's utterances without *roof* (*"what holds each wall up"*, *"is any wall top still free"*) - the same answer.

**State.** **MOVED TO ISSUE [#1333](https://github.com/Ajmalpshaik/Heron-AI/issues/1333) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24** - recorded, not repaired. The repair is `SELECT_BY_PARAMETER_VALUE`'s words for selecting a level by name, or `REMOVE_MASS_FLOORS`' for the request it does, measured on all four backends; not the new card's, whose words were tried.

---

### Row 5b-2015

*Moved from the register on 2026-11-24.*

**Defect.** **THE SHEET IN FRONT COULD NOT BE TOLD FROM TWO OTHERS OF ITS NAME, AND NO SHEET COULD BE NAMED BY ITS NUMBER.** Seen 2026-11-24 on Revit 2020 (session 29500, add-in 0.3.0) on 4355-BHVD-3D-60P00-BL003A - 15 sheets, three of them named *Unnamed* - filling a site sheet's title block from a reference sheet: `revit_view_image` with `view` empty answered *"A picture of 'Unnamed' (DrawingSheet)"*, and `view=Unnamed` refused with *"3 views are called 'Unnamed'. Name one exactly as written here: DrawingSheet: Unnamed, DrawingSheet: Unnamed, DrawingSheet: Unnamed"* - three identical choices, none usable. The binder's `OneView` (`revit/Heron.Revit.Addin/RevitFragment.cs`), which every card's `view`/`views` input goes through, listed them the same way. **Fixed in this change:** both lookups now take a sheet NUMBER (*"4355-BH-VD-VL224880584"*, *"Sheet 4355-BH-VD-VL224880584"* or *"Sheet 4355-BH-VD-VL224880584 - Unnamed"*) or a view's element id, read only where no view carries the text as its name, so every name that worked before reaches what it reached; sheets sharing a name are listed by number; text that is one sheet's number and another view's id is refused. `revit_view_image` names a pictured sheet by its number and gives every named view's id, and `revit_views` gives the view in front by id and number. Held without Revit by `tests/test_sheet_by_number.py`; compiled for 2020 to 2027. **Not checked:** in Revit - [NEEDS-CHECKING Group QF](../needs-checking/group-qf.md), QF1.

**State.** **MOVED TO ISSUE [#1336](https://github.com/Ajmalpshaik/Heron-AI/issues/1336) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - fixed in code in this change, not yet run in Revit.** Written in a cloud session with no Revit; QF1 on the owner's PC closes it.

---

### Row 5b-2016

*Moved from the register on 2026-11-24.*

**Defect.** **[`SELECT_BY_PARAMETER_VALUE`](../../brain/fragments/select-by-parameter-value/fragment.yaml) COULD NOT PICK ONE OF TWO SAME-NAMED PARAMETERS.** Seen 2026-11-24 on Revit 2020 (session 29500, add-in 0.3.0) on 4355-BHVD-3D-60P00-BL003A: every sheet carries *Sheet Number* twice (built-ins `SHEET_NUMBER` and `VIEWER_SHEET_NUMBER`) and *Category* twice, so the card refused all 15 sheets - *"15 element(s) carry TWO OR MORE parameters named 'Sheet Number' and were NOT tested"*, which is right (row 5b-203) - and `parameterName=Sheet Number {SHEET_NUMBER}` answered *"NOTHING carries 'Sheet Number {SHEET_NUMBER}'"*. [`SET_PARAMETER_VALUES_BY_ID`](../../brain/fragments/set-parameter-values-by-id/fragment.yaml) has read `Name {BUILT_IN}` and `Name {GUID}` since its version 9 ([row 5b-1703](../fragment-issues/row-5b-1703.md)); this card never did. **Fixed in this change, version 5, back to DRAFT:** the same two forms reach ONE parameter, only while it is still called the name before the braces; a name as typed is read exactly as version 4 read it; the shared-name guard counts through the same lookup, and its refusal now names each parameter in the form that reaches it. Held without Revit by `tests/test_sheet_by_number.py` and `tests/test_same_name_guard.py`; compiled for 2020 to 2027. **Not checked:** in Revit - [NEEDS-CHECKING Group QF](../needs-checking/group-qf.md), QF2 and QF3.

**State.** **MOVED TO ISSUE [#1337](https://github.com/Ajmalpshaik/Heron-AI/issues/1337) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - fixed in code in this change, not yet run in Revit.** Written in a cloud session with no Revit; the card stays `DRAFT` until QF2 and QF3 are run on the owner's PC.

---

### Row 5b-2017

*Moved from the register on 2026-11-24.*

**Defect.** **[`SET_PARAMETER_VALUES_BY_ID`](../../brain/fragments/set-parameter-values-by-id/fragment.yaml) THREW WHERE IT SHOULD HAVE REFUSED.** Seen 2026-11-24 on Revit 2020 (session 29500, add-in 0.3.0) on 4355-BHVD-3D-60P00-BL003A: a CSV writing `-1` into six shared Yes/No sheet parameters ended `fragment_threw` with *"Nothing was kept. 9567876: Revit would not take '-1' for Hold List (Yes/No) {f13626b3-...}; ..."* - the right words, nothing kept, but an exception, which is how a crash looks. The id rows (and so every CSV cell) checked everything but the value, and a value Revit turned down at the write was thrown so the group rolled back. **Fixed in this change, version 11, back to DRAFT:** a tick box takes 1 or 0 (yes, no, true, false, on, off) and anything else is turned down while the rows are checked, before any row is written - *"is a Yes/No, which takes 1 or 0 (yes or no) - not '-1'"* - in `stale` for id rows and a CSV, in `refused` by key; and by id every value is written in ONE sub-transaction, so one Revit still turns down, or throws on, rolls it back and comes back in `stale` with `applied` false - all or nothing as before, the shape of the answer only. Held without Revit by `tests/test_companion.py` (seen red on version 10); compiled for 2020 to 2027. **Not checked:** in Revit - [NEEDS-CHECKING Group QF](../needs-checking/group-qf.md), QF4 and QF5.

**State.** **MOVED TO ISSUE [#1338](https://github.com/Ajmalpshaik/Heron-AI/issues/1338) 2026-11-24** - still open, tracked there. Was: **OPEN 2026-11-24 - fixed in code in this change, not yet run in Revit.** Written in a cloud session with no Revit; the card stays `DRAFT` until QF4 and QF5 are run on the owner's PC.

---
