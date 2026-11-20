# FRAGMENT-ISSUES archive — section 5b, rows 1401 to 1425

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
> Rows 1401 to 1425 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1402

*Moved from the register on 2026-11-17.*

**Defect.** **`EXPORT_MODEL_TO_IFC`'S `schema` CANNOT BE TYPED - BOTH LEGS REFUSED `bad_request_value` BEFORE ANYTHING RAN.** Measured Revit 2020 session 59096 (33624 before a restart), on "P1102 MEP" - a scratch copy of Revit 2020's own `Samples\rme_advanced_sample_project.rvt` in `%TEMP%\heron-work\prove-1102` - 2026-11-02, run by the owner from `prove-admin-publish.py` with Changes, Admin and Publish on: `schema=IFC2x3` (and `scopeViewId=3D Duct & Equipment`) came back `bad_request_value: Heron can be handed a view, a level, a category, an element type ...` in the positive and the negative alike. The card takes `schema` as an `IFCVersion` on purpose; the request binder has no way to build one from text, so the card cannot run through `revit_change` or `validate` at all. Heron's input layer, not the model.

**State.** **MOVED TO ISSUE [#951](https://github.com/Ajmalpshaik/Heron-AI/issues/951) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - Recorded, not fixed.**

---

### Row 5b-1404

*Moved from the register on 2026-11-17.*

**Defect.** **`LIST_MATERIALS` WITH `withAllTabs=true` CRASHED INSIDE REVIT 2020 WITH AN ACCESS VIOLATION.** Measured Revit 2020 session 59096 (33624 before a restart), on "P1102 MEP" - a scratch copy of Revit 2020's own `Samples\rme_advanced_sample_project.rvt` in `%TEMP%\heron-work\prove-1102` - 2026-11-02, job file `tools/jobs/p1102-round2-reads-2026-11-02.yaml`: `withAllTabs: true, namePattern: "*Concrete*"` came back `fragment_threw: 'list-materials' threw while running: Attempted to read or write protected memory. This is often an indication that other memory is corrupt.` The same card with `withAllTabs: false, namePattern: "*"` ran cleanly (materialCount 190, 0 for `ZZZ*`) and is the proof signed that day. Not traced to the line: the tabs that read appearance and thermal assets are the likely place. (Written as row 5b-1396 and renumbered, because open PR #781 holds 1396.)

**State.** **MOVED TO ISSUE [#949](https://github.com/Ajmalpshaik/Heron-AI/issues/949) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - Recorded, not fixed.** The proof stands only for `withAllTabs` false on Revit 2020; Revit kept running and the next twelve jobs passed. **2026-11-11:** Looked at again when the card's other defect was repaired (row 5b-203, version 7) - and left: the crash was never traced to a line, so no repair is stated and none was invented. The card is DRAFT again for that other change, and its re-proof ([NEEDS-CHECKING Group NK](../needs-checking/group-nk.md) NK8) runs `withAllTabs` on Revit 2024 only, for this row's reason.

---

### Row 5b-1405

*Moved from the register on 2026-11-17.*

**Defect.** ***"PUT IT BACK"* MOVED AGAIN, WRONG TO WRONG, WHEN `CREATE_SHAPED_STAIRS` AND `ADD_STAIRS_LANDING` WERE ADDED.** Seen 2026-11-02 while building the two cards (Group LS), by `tools/check-routing.py` on its own scratch store, on a copy of the tree without the two cards and on this change: on the `model` backend *"put it back"* (`FRG-VIEW-007 claims it`) went from `FRG-VIEW-005 is SERVED` to `FRG-ELE-004 is SERVED`, and the host's lookup (`heron_brain.lookup`, Revit 2024) answered `RESET_GRAPHIC_OVERRIDES` before and `PLACE_FAMILY_INSTANCES` after, both writes its claimant does not name - the same sentence and the same two writes as [row 1375](../FRAGMENT-ISSUES.md#), moved back the other way, through the ranking (neither new card has *put* or *back* in its words). On lexical, with the embedding libraries hidden as CI has none, nothing moved. **The move is this change's; the miss is not** - row 1375 found the sentence unreached before either card existed. Nothing else moved: claimed and not reached 67 before and 67 after on both backends, the same lines; no question answered by a write; every sentence `CREATE_STAIRS` and `CREATE_RAILING` declare reaches its own card by identity before and after.

**State.** **MOVED TO ISSUE [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - not declared on any card, for row 1375's reason: whether the bare sentence means unhiding (`SHOW_ELEMENTS`) or clearing overrides (`RESET_GRAPHIC_OVERRIDES` declares *"put it back how it was"*) is the owner's choice.

---

### Row 5b-1406

*Moved from the register on 2026-11-17.*

**Defect.** **A `CREATE_TEXT_NOTE` SETUP STEP'S `created` REACHED `FIND_DATES_IN_VIEWS` AND `EDIT_TEXT_VALUES` AS THEIR `elements` AND THREW A CAST.** Measured Revit 2020 session 59096, on "P1102 MEP" - a scratch copy of Revit 2020's own `Samples\rme_advanced_sample_project.rvt` in `%TEMP%\heron-work\prove-1102` - 2026-11-02, `tools/jobs/p1102-round11-2026-11-02.yaml`: both cards with `setup: [create-text-note]` (`noteText "ISSUED 09 SEP 2026"`, `textTypeId "1.5mm Arial"`), the second with `keep-chain`, came back `fragment_threw: ... Unable to cast object of type 'Autodesk.Revit.DB.ElementId' to type 'System.Collections.Generic.IList`1[Autodesk.Revit.DB.Element]'` in the positive. `create-text-note` provides one `created` (`ElementId`); a single id was bound into an `IList<Element>` need and cast at run time, where the binder should either wrap it or refuse before the run. Heron's binder, not the cards: neither card was reached.

**State.** **MOVED TO ISSUE [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - recorded, not fixed.** Read 2026-11-11 in a cloud container (no Revit): `CREATE_TEXT_NOTE`, `EDIT_TEXT_VALUES` and `FIND_DATES_IN_VIEWS` all read PROVEN now, each with a proof that still describes its code (`python tools/check-signatures.py`) - *both cards stay DRAFT* below is out of date. Whether a text note can now be handed to them by the binding this row is about was NOT checked. Was: **OPEN 2026-11-02 - recorded, not fixed.** Both cards stay DRAFT until a text note can be handed to them - a model that already holds one, or this binding fixed.

---

### Row 5b-1407

*Moved from the register on 2026-11-17.*

**Defect.** **`EXPORT_FAMILIES` EXPORTED NOTHING ON REVIT 2020 - REVIT REFUSED TO OPEN EITHER ELBOW FAMILY FOR EDITING.** Measured Revit 2020 session 59096, on "P1102 MEP" - a scratch copy of Revit 2020's own `Samples\rme_advanced_sample_project.rvt` in `%TEMP%\heron-work\prove-1102` - 2026-11-02, run by the owner from `prove-admin-publish.py` with Changes, Admin and Publish on: `nameContains "M_Rectangular Elbow"` matched `M_Rectangular Elbow - Mitered` and `M_Rectangular Elbow - Radius`, and `refused` read `Revit refused to open it for e...` for both (the reply cuts lists to 60 characters, so the rest of Revit's sentence was not seen); `saved` and `savedAs` were empty. Not traced: whether `EditFamily` is refused because the run sits inside the write path's transaction group, or for these two families alone.

**State.** **MOVED TO ISSUE [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - recorded, not fixed.** The card stays DRAFT; the next try names a family outside the duct fittings and reads the whole refusal.

---

### Row 5b-1408

*Moved from the register on 2026-11-17.*

**Defect.** **`RELOAD_LINKS` RELOADED NOTHING - REVIT REFUSED EVERY ONE OF THE SIX LINKS INSIDE THE PROOF'S ROLLED-BACK RUN.** Measured Revit 2024.3 session 59960, on "Snowdon Towers Sample HVAC" - a scratch copy of Revit 2024's own `Samples\Snowdon Towers Sample HVAC.rvt` in `%TEMP%\heron-work\prove-1102\s2024`, its 6 links loaded from the copies beside it - 2026-11-02, job file `tools/jobs/p1102-round16-revit2024-hvac-2026-11-02.yaml`: the 6 RVT Links selected, `unload false`, returned `done 0` and `refused 6` - `Snowdon Towers Sample Structural.rvt: Operation is not permi...` and, for the others, `The link "this RevitLinkT...` (the reply cuts each to 60 characters). Since 2026-10-17 a MODIFY card is proved only through the write path, inside a TransactionGroup; reloading or unloading a link is one of the operations Revit allows only with no transaction open, so the card's own action is refused before it can be rolled back. Its 2026-09 proof ran on the read path, which no longer takes a MODIFY card. Not traced past the cut message.

**State.** **MOVED TO ISSUE [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - recorded, not fixed.** The card stays DRAFT; it needs a proof route outside the transaction group, or its kept run (D-120) on a model the modeller chooses.

---

### Row 5b-1409

*Moved from the register on 2026-11-17.*

**Defect.** **`SET_MEP_JUSTIFICATION` REFUSED EVERY DUCT ON REVIT 2024 TOO - ROW 5b-1397 IS NOT A REVIT 2020 MATTER.** Measured Revit 2024.3 session 59960, on "Snowdon Towers Sample HVAC" - a scratch copy of Revit 2024's own `Samples\Snowdon Towers Sample HVAC.rvt` in `%TEMP%\heron-work\prove-1102\s2024`, its 6 links loaded from the copies beside it - 2026-11-02, job file `tools/jobs/p1102-round16-revit2024-hvac-2026-11-02.yaml`: the 191 ducts of `FloorPlan: L2`, `horizontalOffsetMm 50, verticalOffsetMm 50`, returned `set 0` and 193 findings beginning `Refused: id 1365662: 2 of 2 offset(s) did not take the value` - the same answer row 5b-1397 recorded on Revit 2020 the same morning, now on a second release and a second model. Rolled back.

**State.** **MOVED TO ISSUE [#942](https://github.com/Ajmalpshaik/Heron-AI/issues/942) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - recorded, not fixed.** Read with row 5b-1397; the card stays DRAFT.

---

### Row 5b-1410

*Moved from the register on 2026-11-17.*

**Defect.** **TWO CARDS DECLARED AN ALWAYS-FILLED REPORT SENTENCE `role: result` - `CHECK_FAMILY_SIZE_TABLES`' `report` AND `CHECK_FAMILY_STANDARDS`' `familyReport` - SO NO NEGATIVE COULD COME BACK EMPTY.** Measured on Revit 2024.3 session 59960, 2026-11-02, on "P1102 Elbow24" - a copy of Revit 2020's US Metric `Pipe\Fittings\Generic\M_Elbow - Generic.rfa` in `%TEMP%\heron-work\prove-1102\fam`, job files `tools/jobs/p1102-round18-families-revit2024-2026-11-02.yaml` and `p1102-round19-revit2024-2026-11-02.yaml`. `check-family-size-tables`: the family's table replaced by one with no FOD column gave `problemCount 1` (MISSING COLUMN: 'Fitting Outside Diameter'), the same rows written as a new table gave `problemCount 0` - and `report` still read `ROWS NOT CHECKED ...` and `UNUSED TABLE ...`, so batch-prove and `accept` called the negative NEG NOT EMPTY. `check-family-standards`: `namePattern "TRG_"` gave `offStandard 1`, no name rule gave `offStandard 0, warned 0` (0 FAIL, 13 PASS) - and `familyReport` still listed every PASS line. The same card already declares its other always-filled sentence, `classificationReport`, `role: accounting`. Both sentences are now `role: accounting` with a comment naming this row; the counts are the result. `fragment.yaml` is outside the proof seal: `brain/heron_fragment.py --show` printed the same fingerprint before and after (`1dacdb5181c7e77a`, `b9a7beb08bb9010e`), and each draft was re-made from the run already recorded, not a new one. Also measured: `namePattern` is a CONTAINS test, so `*` fails every family by name - the 2026-10-18 job's `"*"` negative was never a wildcard.

**State.** **FIXED 2026-11-02 - the two roles, in the cards' own text.** Both cards proved the same day on those runs.

---

### Row 5b-1411

*Moved from the register on 2026-11-17.*

**Defect.** **`EXPORT_FAMILIES` IS REFUSED ON REVIT 2024 TOO, FOR A DIFFERENT FAMILY - ROW 5b-1407 IS THE PROOF ROUTE, NOT THE ELBOWS.** Measured on Revit 2024.3 session 59960, 2026-11-02, run by the owner from `prove-admin-publish-4.py` with Changes, Admin and Publish on, on "Snowdon Towers Sample Structural" - a scratch copy of Revit 2024's own `Samples\Snowdon Towers Sample Structural.rvt` in `%TEMP%\heron-work\prove-1102\s2024`: `nameContains "Concrete-Rectangular-Column"` returned `saved 0`, `savedAs 0 entry(ies)` and `refused 1 item(s) [Concrete-Rectangular-Column: Revit refused to open it for ed...]` - the same refusal row 5b-1407 met for two duct elbows on Revit 2020, now on a second release, a second model and a structural family. So the cause is not those families. Every proof of a PUBLISH card runs inside the write path's TransactionGroup, and opening a family for editing is one of the calls Revit makes only with no transaction open; the card cannot reach `EditFamily` there. Not traced past the cut message. Three earlier attempts the same minute were refused `model_in_use` - this chat's own name lookup held the model for three quiet minutes; a refused call does not renew a hold, and Free this model frees only the model in front.

**State.** **MOVED TO ISSUE [#941](https://github.com/Ajmalpshaik/Heron-AI/issues/941) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN 2026-11-02 - recorded, not fixed.** The card stays DRAFT until a proof route outside the transaction group exists, or its kept run (D-120) on a model the modeller chooses.

---

### Row 5b-1412

*Moved from the register on 2026-11-17.*

**Defect.** **THREE MORE CARDS DECLARED AN ALWAYS-FILLED FIELD `role: result` - `REPORT_SPRINKLER_NETWORK`' `networkJson` (ROW 5b-1401), `SET_EXPORT_SETUP_OPTIONS`' `kindUsed` AND `SET_SCHEDULE_OPTIONS`' `applied`.** Measured on Revit 2020, "P1102 MEP" - a scratch copy of Revit 2020's own `Samples\rme_advanced_sample_project.rvt` - 2026-11-02, job files `tools/jobs/p1102-round5-2026-11-02.yaml`, `p1102-round8-2026-11-02.yaml` and `p1102-round9-2026-11-02.yaml`, read again from the run records those rounds wrote. `report-sprinkler-network`: `elementCount 29` for "Fire Protection Wet 1" and `0` for "ZZZ No Such System", while `networkJson` carried the list of systems it chose from in both. `set-export-setup-options`: `done true`, `changed "fileVersion: 2018 -> 2013"` against `done false`, `changed` empty for `units=parsecs` - and `kindUsed "DWG"` in both, because it names the setup that was FOUND. `set-schedule-options`: `changed 4` against `changed 0` for `showTitle "maybe"` - and `applied` read "nothing it could set" for each schedule in the negative. Each field is now `role: accounting` with a comment naming this row, row 5b-1410's shape; the count or flag beside it is the result. `fragment.yaml` only: `brain/heron_fragment.py --show` printed the same fingerprints before and after (`8cffde063d891c0f`, `35a30fad0832ce4c`, `4b32d0d29b408cc2`), and each draft was re-made from the run already recorded. The cards that were looked at and NOT changed, because their sentence is the answer itself: `report-findings`, `report-category-overrides` (its hidden categories are real in any view), `report-browser-organization`, `report-level-elevations`; `describe-blank-parameters` declares no result at all.

**State.** **FIXED 2026-11-02 - the three roles, in the cards' own text; row 5b-1401 with it.** The three cards proved the same day on those runs.

---

### Row 5b-1413

*Moved from the register on 2026-11-17.*

**Defect.** ***"WHICH UNITS DOES THIS MODEL SHOW COOLING LOAD IN"* - A QUESTION `tests/test_routing_phrases.py` HOLDS (row 356's, repaired onto `REPORT_PROJECT_UNITS` on 2026-10-14) - REACHES `RUN_HEATING_COOLING_LOADS`, A WRITE, ON THE TRAINED MODEL WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 while building `RUN_HEATING_COOLING_LOADS` (Group LU): the suite, which asks `heron_retrieve.find` with no Revit and no flag on the `model` backend, failed eight lines where the tree at `1327aed` fails seven - this is the eighth. Through `find` on scratch stores: at `1327aed` `REPORT_PROJECT_UNITS` on Revit 2027, 2024 and no Revit, with and without the flag; with this change the write on Revit 2027 and with no Revit named without the flag, and `REPORT_PROJECT_UNITS` with the flag and on Revit 2024, where the write is not offered. `brain/heron_retrieve.py` calls it *A COIN TOSS*: the write and the read tie at 0.0323 - the write first by nearness, on its declared *"run the loads analysis and show the cooling load per space"*. Two card-text rewordings were tried and put back - the write's `semantic-identity` without *"this model"*, and the read's `purpose` naming *"the units the model shows every value in"* - neither moved it; nor did two more on the write's own `semantic-identity` and `purpose`, tried and put back the same day. Declaring the sentence on `REPORT_PROJECT_UNITS` was tried and put back too: the suite then failed nine lines, its section 2 refusing the word-for-word declaration and *"show cooling and heating loads in Btu/h and airflow in CFM"* moving to `READ_SPACE_LOADS`. CI is not reached: with no trained model the suite exits 3 there.

**State.** **MOVED TO ISSUE [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Not repaired here: the suite forbids declaring its sentence word for word (it would then test nothing), row 356's repair - question paraphrases declared on the read - needs words a modeller has said, and none has been, and the write's three sentences are the modeller's and are not weakened. The owner's to choose: a units question somebody has asked, declared on `REPORT_PROJECT_UNITS`.

---

### Row 5b-1417

*Moved from the register on 2026-11-17.*

**Defect.** ***"WHICH ONE IS CUTTING"* - A ROUTING ROW OF `JOIN_GEOMETRY` THAT SENDS IT TO `REPORT_JOINED_ELEMENTS` - REACHES NEITHER.** Seen 2026-11-02 while building `REPORT_GEOMETRY_CUTS` (Group LW), with `heron_retrieve.find` on fresh scratch stores on Revit 2024, 2027 and none named, with and without the host's question flag: on the tree at `cd323cb` it reached `REPORT_DIVIDED_SURFACES`; with the new card it reaches `REPORT_GEOMETRY_CUTS`. No card declares the sentence, and `tools/check-routing.py` checks only rows that say *-> here*, so it is in no list. Not caused by this change - the row was not reached before it.

**State.** **MOVED TO ISSUE [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** For the owner: is *"which one is cutting"* a join's question (`REPORT_JOINED_ELEMENTS`, as `JOIN_GEOMETRY`'s table says) or either kind of cut? Declaring it on `REPORT_JOINED_ELEMENTS` would settle the first - card text only - once somebody has said it; nobody has said it to Heron in a recorded request.

---

### Row 5b-1420

*Moved from the register on 2026-11-17.*

**Defect.** **NO HERON CARD CAN READ OR SET THE KEYNOTE NUMBERING METHOD: THE REVIT API HAS NO MEMBER FOR IT ON ANY RELEASE.** Found 2026-11-02 when asked to build Annotate > Keynoting Settings - the modeller's words *"set keynote numbering to by sheet"*. Keynoting Settings' Numbering Method (By keynote / By sheet) has no type, property or parameter in RevitAPI.xml on any release 2020 through 2027: no `KeynotingOptions` type, no `BuiltInParameter` beyond `KEYNOTE_PARAM`, `KEYNOTE_NUMBER`, `KEYNOTE_TEXT` and `SHEET_ASSEMBLY_KEYNOTE`, and `KeynoteTable` declares only `GetKeynoteTable`. The one member that reaches the dialog is `PostableCommand.KeynotingSettings`, which opens it for the modeller and reports nothing back. The sentence reaches `REPORT_KEYNOTES`, a READ, with and without the host's question flag - a question card for a change nobody can make. Not caused by this change; no card was written, and the sentence is declared on no card. `LOAD_KEYNOTE_FILE`, `REPORT_KEYNOTES` and `REPORT_KEYNOTE_FILE` (Group LZ) say so in their routing tables - the last since the keynote-file read moved to it on 2026-11-02; the sentence still reaches `REPORT_KEYNOTES`, with and without the flag.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** For the owner: whether a card that posts `PostableCommand.KeynotingSettings` - the dialog opened for the modeller to choose By sheet by hand, as `OPEN_RENDER_DIALOG` does for rendering - is wanted, or whether the host should answer the sentence in words. Until one is chosen, nothing claims it.

---

### Row 5b-1423

*Moved from the register on 2026-11-17.*

**Defect.** **NO HERON DOOR CAN RELOAD LATEST OR SYNC WITH CENTRAL: REVIT REFUSES BOTH WHILE ANY TRANSACTION GROUP IS OPEN, AND EVERY DOOR THAT MAY CHANGE A MODEL RUNS INSIDE ONE.** Found 2026-11-02 when asked to build Revit's Collaborate > Reload Latest - the modeller's words *"reload latest"*, *"get the latest changes from central"*, *"update my local copy from the central model"*. `Document.ReloadLatest(ReloadLatestOptions)` and `Document.SynchronizeWithCentral` each throw *"Operation is not permitted when there is any open sub-transaction, transaction, or transaction group"* - RevitAPI.xml read on every release 2020 through 2027. `RevitFragment.Run` starts one `TransactionGroup` in the target model before any `MODIFY`, `PUBLISH` or `ADMIN` fragment runs, and a fragment declaring `noTransactionOpen` ([D-125](../DECISIONS.md)) runs inside that group too. The read door opens none, but counts the model before and after and refuses a changed one (row 5b-350), and a reload declared `READ` would be a false risk (Golden Rule 19). So `SYNC_WITH_CENTRAL` (DRAFT, never run in Revit) cannot sync as written either - [row 365](../fragment-issues/section-5b-rows-351-375.md) said so in passing and left it as found. The one route in the library with no group is `UIApplication.PostCommand`, which `OPEN_RENDER_DIALOG` uses; `PostableCommand.ReloadLatest` is present 2020 through 2027, but a posted command runs after Heron has answered, so a card could report neither Revit's result nor the model after the reload, and Keep or Discard would count no change. Not caused by this change; no card was written.

**State.** **ANSWERED 2026-11-19 - by hand: Reload Latest and Sync with Central stay the modeller's own clicks on Revit's Collaborate tab. No reload card is built, no route is opened with no transaction group, no posted command is used for either, and `SYNC_WITH_CENTRAL` stays as found (DRAFT, never run in Revit). Recorded as [D-185](../DECISIONS.md). Nothing in code changed.** Tracked as issue [#911](https://github.com/Ajmalpshaik/Heron-AI/issues/911) until then. Was: **OPEN.** For the owner: an add-in route that runs one named fragment with no group open - the route row 365 found missing for a save - or a posted Revit command for reload latest whose result is read back by a separate read afterwards; and which risk either carries. Until one is chosen no reload card is written and `SYNC_WITH_CENTRAL` stays as found.

---

### Row 5b-1424

*Moved from the register on 2026-11-17.*

**Defect.** ***"PUT IT BACK"* MOVED A THIRD TIME, WRONG TO WRONG, WHEN `CREATE_ARC_LENGTH_DIMENSION` WAS ADDED.** Seen 2026-11-02 while building it (FRG-VIEW-171, DRAFT, a write - [Group MA](../needs-checking/group-ma.md)), by `tools/check-routing.py` on its own scratch `HERON_KNOWLEDGE` store, on a copy of the tree without the card and on this change, `model` backend: *"put it back"* (`FRG-VIEW-007 claims it`) went from `FRG-ELE-004 is SERVED` to `FRG-VIEW-005 is SERVED`, and the host's lookup (`heron_brain.lookup`, Revit 2025, no question flag) answered `PLACE_FAMILY_INSTANCES` before and `RESET_GRAPHIC_OVERRIDES` after - the same sentence and the same two writes as [row 1375](../FRAGMENT-ISSUES.md#) and row 1405, through the ranking (the new card shares only *put* with it). **The move is this change's; the miss is not** - row 1375 found the sentence unreached before this card existed. Nothing else moved off a card that claims it: claimed and not reached was the same list before and after but for this line; every sentence the card declares reaches it by identity; on the keyword route alone *"link this nested beam's length to Beam Length"* and *"disconnect these two elements"* now rank another card first, and the lookup answers both on their own cards (`LINK_FAMILY_PARAMETER`, `DISCONNECT_CONNECTORS`) before and after.

**State.** **MOVED TO ISSUE [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - not declared on any card, for row 1375's reason: whether the bare sentence means unhiding (`SHOW_ELEMENTS`) or clearing overrides (`RESET_GRAPHIC_OVERRIDES` declares *"put it back how it was"*) is the owner's choice.

---
