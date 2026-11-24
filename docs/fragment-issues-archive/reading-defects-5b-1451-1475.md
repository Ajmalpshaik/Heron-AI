# FRAGMENT-ISSUES archive — section 5b, rows 1451 to 1475

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
> Rows 1451 to 1475 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1451

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_ENERGY_MODEL` DOES NOT KNOW WHAT REVIT DOES WITH AN ENERGY MODEL ALREADY THERE, AND ON REVIT 2020 AND 2021 BUILDS FROM BUILDING ELEMENTS ONLY.** Found 2026-11-02 building it ([Group MM](../needs-checking/group-mm.md)), read only. `EnergyAnalysisDetailModel.Create` is 2020-2027 (`Create(Document, options)` obsolete in 2027 beside the new `Create(Document)`); nothing read says whether a second call rebuilds the main model, replaces it or adds another, so the card names the one there before, asks `GetMainEnergyAnalysisDetailModel` afterwards and says which happened - and deletes nothing. `EnergyModelType.AnalysisMode`, which follows the Energy Settings' mode, is 2022 on; on 2020-2021 only `SpatialElement` and `BuildingElement` exist, so a mode other than building elements is refused by name rather than mapped onto one. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MM's MM6 records the second run's finding; the card's words are corrected from it before any proof.

---

### Row 5b-1452

*Moved from the register on 2026-11-17.*

**Defect.** **`IMPORT_GBXML` CALLS `Document.Import(string, GBXMLImportOptions)` INSIDE THE HOST'S OPEN TRANSACTION, AND WHETHER REVIT ALLOWS THAT HAS NOT BEEN SEEN.** Found 2026-11-02 building it ([Group MM](../needs-checking/group-mm.md)), read only. The method is one signature 2020 to 2027 and `GBXMLImportOptions` has only a constructor (`tools/api-surface --members`). The refusals RevitAPI.dll carries for it, read with `strings` on 2020, 2024 and 2027, are an empty file name, *"The GBXml file does not contains what it should!"* and *"The GBXml file is not valid for importing!"* - no *"close the transaction"* refusal of the kind `Document.EditFamily` carries in the same file. That is why it was built rather than written up as impossible; it is not a run. Which elements the import makes is not assumed either: the card lists the model before and after and hands back what is new.

**State.** **MOVED TO ISSUE [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MM's MM7 records first whether Revit runs the import inside the host's transaction at all. If it refuses, the card is retired with Revit's words and this row says why.

---

### Row 5b-1453

*Moved from the register on 2026-11-17.*

**Defect.** **ROUTING AFTER GROUP MM: TWO MADE-UP QUESTIONS REACH A SETTINGS WRITE WITHOUT THE HOST'S QUESTION FLAG, TWO MADE-UP REQUESTS MISS `SET_MECHANICAL_SETTINGS`, AND ONE OWNER'S SENTENCE WAS DECLARED SO IT COULD NOT MOVE.** Measured 2026-11-02 with `heron_retrieve.find` on fresh scratch stores of the tree at `0634fb7` and of this change, Revit 2024, with and without the host's question flag (the `READ` ceiling), over every declared utterance and routing-table sentence of the library and 26 probes. Every sentence the six cards declare reaches its card by identity. *"what is the duct connector separator"* and *"what building type is set in the energy settings"* reach `SET_MECHANICAL_SETTINGS` and `SET_ENERGY_SETTINGS` (ADMIN) without the flag; with it, `REPORT_CONNECTORS` and `REPORT_ENERGY_SETTINGS`. *"set the duct fitting annotation size to 3 mm"* still reaches `EDIT_MEP_SIZES` and *"set the flat on top annotation to FOT"* `ADD_BEAM_ANNOTATIONS`; *"what are the duct settings"* reaches `REPORT_DUCT_CONDUIT_SIZES`, a read of the size lists. *"make it mechanical equipment and work plane based"*, which `SET_FAMILY_SETTINGS`' own routing table claims, moved to `CREATE_MEP_SYSTEM` on one intermediate store and is now declared on that card (card text only; its impl/ and proof untouched). `tests/test_routing_phrases.py`'s *"put the duct tags above the ducts with a leader"* left `OFFSET_TAGS_FROM_HOST` on a first draft - the two mechanical cards entered its nearness pool of 20 and pushed the answer out - and the two cards' utterances, identity and purpose were reworded until it came back; that suite fails exactly its known seven. Without the flag the only other sentence that moved is *"put it back"*, `RESET_GRAPHIC_OVERRIDES` to `PLACE_FAMILY_INSTANCES`, the flip row 1375 records. With the flag 38 sentences moved between reads; recorded, not tuned. `tools/check-routing.py`: no question answered by a write.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - the probes are nobody's words, so none is declared (D-86's rule against inventing utterances); the host's question flag keeps each question from a write. Reworded the day a modeller asks one of them.

---

### Row 5b-1454

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_REBAR_SHAPE` READS A BEND'S ACUTE, RIGHT OR OBTUSE FROM THE ANGLE TURNED, AND MEASURES EACH SEGMENT TO THE OUTSIDE OF A BEND - BOTH CHOSEN WITHOUT SEEING REVIT'S OWN SHAPES DO IT.** Found 2026-11-02 building it ([Group MN](../needs-checking/group-mn.md)), read only. `RebarShapeBendAngle` is `Acute`, `Right` or `Obtuse` on every release 2020 to 2027 (`tools/api-surface --members`) and RevitAPI.xml says only *"Specify whether the bend is acute, obtuse, etc."* - not whether that is the angle the bar turns through or the angle between its legs. The card reads it as the angle turned first (a 180 degree bend has a method of its own, `SetSegmentAs180DegreeBend`, which suggests the ladder runs acute, right, obtuse, 180) and, when a bend is not 90 degrees and Revit's solver (`CheckDefaultParameterValues`) cannot solve the shape that way, builds it once more with the two swapped and says so in a finding. Each length is measured to the outside of a bend at a corner and from the free end at either end of the bar (`AddConstraintParallelToSegment`'s two flags), as bar schedules dimension a shape; nothing read says Revit's shipped shapes do the same. The lengths are driven by the model's own rebar shape parameters A, B, C ... found with `RebarShapeParameters.GetAllRebarShapeParameters`; a model without them is refused, never given new shared parameters. Which bar types a new shape is allowed for (`RebarShape.GetAllowed`) is read back, never set. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MN's MN2 records the 45 degree shape's finding and the browser's picture, and MN3 the lengths Revit's own Rebar Shape Browser shows; the card is corrected from them before any proof.

---

### Row 5b-1455

*Moved from the register on 2026-11-17.*

**Defect.** **`PLACE_REINFORCEMENT_SYMBOLS` PLACES ALL THREE SYMBOLS THE API OFFERS, AND WHAT REVIT DOES IN A STRUCTURAL PLAN OR A SECTION, WITH THE CATEGORY RevitAPI.xml NAMES, IS NOT KNOWN.** Found 2026-11-02 building it ([Group MN](../needs-checking/group-mn.md)), read only. **What the API can do, built:** `RebarSystemSpanSymbol.Create` (area reinforcement), `PathReinSpanSymbol.Create` (path reinforcement) and `FabricReinSpanSymbol.Create` (a fabric sheet) are one signature on every release 2020 to 2027 (`tools/api-surface --members`); a fabric area handed in gives each of its sheets a symbol. **TAGS are not this card's:** a Structural Path, Area or Fabric Reinforcement tag or a Fabric Area tag is placed by `TAG_ELEMENTS_IN_VIEW` (PROVEN), whose category map works OST_PathRein out to OST_PathReinTags, OST_AreaRein to OST_AreaReinTags, OST_FabricReinforcement to OST_FabricReinforcementTags and OST_FabricAreas to OST_FabricAreaTags from Revit's own names - read in its impl/, not run on those categories. **What is not known:** RevitAPI.xml says of all three that *"only floor plan, reflected ceiling plans and elevations are permitted"*, so a structural plan and a section are passed on and Revit's refusal reported; and its text for the path and area symbols says the type must be of category OST_FabricReinSpanSymbol - read here as the fabric page copied, so each kind is given a type of its own category (OST_PathReinSpanSymbol, OST_AreaReinSpanSymbol). **Not built:** a symbol on reinforcement in a LINKED model (the `LinkElementId` takes one; nothing here finds the link), and a head placed anywhere but the middle of the host as the view bounds it. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MN's MN4 records whether Revit takes each kind's own category, MN5 whether a structural plan and a section take a symbol; the card is corrected from them before any proof. Linked hosts are a version of their own the day a modeller asks.

---

### Row 5b-1456

*Moved from the register on 2026-11-17.*

**Defect.** **`CREATE_SHAPED_STAIRS` VERSION 2 SKETCHES A STAIR WITH ONE RISER LINE PER RISER, THE FIRST AND LAST INCLUDED, AND THAT COUNT HAS NOT BEEN SEEN.** Found 2026-11-02 widening it ([Group MN](../needs-checking/group-mn.md)), read only. Asked for as a ribbon gap - Architecture > Stair > Create Sketch, boundary and riser lines; version 1 made component runs only. `StairsRun.CreateSketchedRun(Document, ElementId, double, IList<Curve>, IList<Curve>, IList<Curve>)` is one signature 2020 to 2027 (`tools/api-surface --members`); RevitAPI.xml asks for two boundary chains that do not touch, at least two riser chains each running between them, and a path through every riser from the first to the last - and says nothing of how many risers N lines make. The card draws N lines for N risers, by the rule `CREATE_STAIRS` measured for a straight run (*(risers - 1) treads*), and reads the risers back: a stair whose count differs is cancelled inside its edit scope, never kept. The sketch is drawn at the base level's `ProjectElevation`, as version 1 draws its run paths, with the run's base elevation 0 (relative to the stair, RevitAPI.xml). Straight boundary segments only - no arcs in this version. Direction, turn and run width became optional for the sketch's sake; on L, U and spiral each is refused left out in the card's words, where the add-in refused it before, and nothing else about those three changed. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MN's MN6 records the risers Revit made from the riser lines drawn; the card is corrected from it before any proof.

---

### Row 5b-1457

*Moved from the register on 2026-11-17.*

**Defect.** **ROUTING AFTER GROUP MN: TWO OWNERS' SENTENCES WERE DECLARED SO THEY COULD NOT MOVE, AND SIX MADE-UP SENTENCES REACH A NEW WRITE WITHOUT THE HOST'S QUESTION FLAG.** Measured 2026-11-02 with `heron_retrieve.find` on fresh scratch stores of the tree at `9659655` and of this change, Revit 2024, with and without the host's question flag (the `READ` ceiling), over every declared utterance and routing-table sentence of the library and 24 probes. Every sentence the three cards declare reaches its card by identity. *"what shape is this element"*, which `MEASURE_ELEMENT_VOLUME`'s routing table claims, moved to `CREATE_REBAR_SHAPE` - a question reaching a write - and *"make the leaders L-shaped"*, claimed by `FORCE_TAG_LEADER_LSHAPE`, moved to `CREATE_SHAPED_STAIRS`; rewording the new card's identity did not bring either back, so each is now declared on its owner (card text only - both cards PROVEN, their impl/ and proofs untouched) and reaches it by identity. The stair card's identity is version 1's, unchanged. Probes, nobody's words: *"does this path reinforcement have a symbol"* went from `REPORT_STRUCTURAL_SETTINGS` (a read that cannot answer it) to `PLACE_REINFORCEMENT_SYMBOLS`; *"what symbols are on this path reinforcement"* from `PLACE_PATH_REINFORCEMENT`, *"what shape is this rebar"* from `PLACE_REBAR` and *"what are the riser lines of this stair"* from `CREATE_STAIRS` - writes before and after; with the flag each reaches a read. *"tag the path reinforcement"* and *"tag the area reinforcement"* went from the cards that make those systems to `PLACE_REINFORCEMENT_SYMBOLS`; a tag is `TAG_ELEMENTS_IN_VIEW`'s, which neither reached before. Without the flag the only other sentence that moved is *"make a sheet set for each value of TCM_Project Browser 1"*, `CHECK_SHEET_SETS_BY_PARAMETER` to `REPORT_BROWSER_ORGANIZATION`, read to read, neither its claimer. With the flag six sentences moved between reads; recorded, not tuned. `tests/test_routing_phrases.py` fails exactly its known seven, *"put the duct tags above the ducts with a leader"* still `OFFSET_TAGS_FROM_HOST`. `tools/check-routing.py`: no question answered by a write.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - the probes are nobody's words, so none is declared (D-86's rule against inventing utterances); the host's question flag keeps each question from a write. No card reads a reinforcement system's symbols or a stair's riser lines; each is a card of its own the day a modeller asks.

---

### Row 5b-1458

*Moved from the register on 2026-11-17.*

**Defect.** **`OFFSET_LINE_CHAIN` OFFSETS EACH CHAIN AS ONE LOOP AND SETS ITS PIECES ONE BY ONE - WHAT REVIT DOES AT THE JOINS, AND WHICH SIDE A CHAIN GOES BY THE SIGN, HAS NOT BEEN SEEN.** Found 2026-11-03 building it ([Group MO](../needs-checking/group-mo.md)), read only. Asked for as a ribbon gap - Modify > Offset for model and detail lines and chains of them, as a copy or a move; the ribbon coverage note had Offset PARTIAL, *"moves only; no copy option, no line chains"*. `OFFSET_ELEMENTS` (PROVEN) moves each linear run on its own and says a parallel copy is `COPY_ELEMENTS` followed by it - two pieces of a chain offset alone overlap or part at their corner - so a card of its own was written and that one is not touched. **What the API can do, built:** the pieces are sorted into chains by their meeting ends, each chain becomes one `CurveLoop`, and `CurveLoop.CreateViaOffset(CurveLoop, double, XYZ)` (2020 to 2027, `tools/api-surface --members`) trims every offset piece to meet its neighbour; a copy is `ElementTransformUtils.CopyElement` and then the copy's curve set (`CurveElement.SetGeometryCurve(Curve, true)` for a line, `LocationCurve.Curve` for a wall), so the copy keeps its line style or wall type. RevitAPI.xml: a positive distance goes *"to the right"* of the loop with its normal as up, and an ellipse or spline comes back a HermiteSpline, which `SetGeometryCurve` refuses as a curve of another type - named, never forced. `towards`, a point on the side wanted, turns the sign round. **What is not known:** whether a joined chain moved piece by piece with `overrideJoins` true stays joined at its new corners; what a moved wall's unselected neighbours do; and whether a copied wall brings its doors and windows as Revit's own copy does. Every piece is read back after one regeneration and any piece Revit moved is named. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MO's MO2 records the joins, the side and the read-back; the card is corrected from them before any proof.

---

### Row 5b-1459

*Moved from the register on 2026-11-17.*

**Defect.** **`INSERT_VIEWS_FROM_FILE` OPENS A PROJECT FILE INSIDE THE HOST'S TRANSACTION ON ANOTHER MODEL, AND SETS NO HANDLER FOR A TYPE NAME ALREADY HERE - NEITHER HAS BEEN SEEN WITH A PROJECT FILE.** Found 2026-11-03 building it ([Group MO](../needs-checking/group-mo.md)), read only. Asked for as a ribbon gap - Insert > Insert from File > Insert Views from File and Insert 2D Elements from File; the ribbon coverage note had it PARTIAL, *"the source file must already be open"*. `TRANSFER_VIEWS_BETWEEN_DOCUMENTS` (PROVEN) needs the source open in the same Revit and its purpose declines to guess at a file on disk; its impl/ is not touched. **Can a fragment open another .rvt?** Yes on the evidence there is: `UPGRADE_FAMILY_FILES`' proof (2026-09-11, Revit 2024) opened and closed a family with `Application.OpenDocumentFile` and `Document.Close(false)` inside a run Revit wrapped in a transaction, and Group EX's EX4 records the run guard closing a document a job opened and left open. The card opens the file detached (`DetachAndPreserveWorksets`), as the wrong user allowed, with its worksets open, copies with `ElementTransformUtils.CopyElements` - the document form for the view, the view form for what it owns - and closes it unsaved in a `finally`. A file already open is used and left open. **What is not known:** that the same holds for a project file; what Revit does with a type of the same name when no `CopyPasteOptions` handler is set - a handler must be a class, which a fragment cannot declare (`TRANSFER_VIEWS_BETWEEN_DOCUMENTS`' finding); and whether a legend's legend components arrive. Every view is counted back. Schedules and sheets, which Revit's own Insert Views from File also takes, are not built here. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MO's MO3 records the open, the copy and the close; a schedule or sheet from a file is a version of its own the day a modeller asks.

---

### Row 5b-1460

*Moved from the register on 2026-11-17.*

**Defect.** **`RUN_SYSTEMS_ANALYSIS` STARTS AN ANALYSIS THAT RUNS IN THE BACKGROUND - WHAT BECOMES OF IT WHEN THE HOST ROLLS ITS VIEW BACK, AND WHETHER THE REQUEST RUNS INSIDE A TRANSACTION AT ALL, IS NOT KNOWN.** Found 2026-11-03 building it ([Group MO](../needs-checking/group-mo.md)), read only. Asked for as a ribbon gap - Analyze > Systems Analysis; the ribbon coverage note had it MISSING, earliest release UNSURE. **What the API offers** (`tools/api-surface --members`, RevitAPI 2020 to 2027): `ViewSystemsAnalysisReport.Create`, `RequestSystemsAnalysis(SystemsAnalysisOptions)`, `IsAnalysisCompleted`, `GetReportContent`, `GetLatestSystemsAnalysisReport` and the read-only weather, workflow and output folder in all eight; `ReportStyle` only 2022 to 2027 and `CancelSystemsAnalysis` only 2023 to 2027, neither used. RevitAPI.xml says *"since 2020.1"*, so Revit 2020 needs that update. **Built:** a report view, the request with the modeller's own weather or workflow file if one is named and found on the PC, and what Revit says when the call returns - the request runs *"in the background"*, so the result is read in Revit's own report view. The output folder is never set: Revit's default is the system TEMP folder, so no shared location is written (Article 10). Revit's refusals - *"A valid energy model is required"*, no weather station within 500 nautical miles - are thrown in its words and nothing is kept. **What is not known:** whether `RequestSystemsAnalysis` runs inside the host's open transaction; what happens to the background analysis, and its files in TEMP, when the modeller presses Discard and the view is rolled back; how long it takes. No card reads a finished report (`GetLatestSystemsAnalysisReport`, `GetReportContent`); none was asked for. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MO's MO4 and MO5 record the run, the rollback and the timing; the card is corrected from them before any proof.

---

### Row 5b-1461

*Moved from the register on 2026-11-17.*

**Defect.** **`PUBLISH_COORDINATES` RECORDS THE POSITION IN THE LINK BUT NEVER SAVES THE LINKED FILE - REVIT ASKS AT THE NEXT SAVE OF THE HOST, AND WHAT IT DOES WHEN THAT SAVE IS HERON'S IS NOT KNOWN.** Found 2026-11-03 building it ([Group MO](../needs-checking/group-mo.md)), read only. Asked for as a ribbon gap - Manage > Coordinates > Publish Coordinates. `Document.PublishCoordinates(LinkElementId)` is one signature 2020 to 2027 (`tools/api-surface --members`); its `LinkElementId` names the link instance and a `ProjectLocation` in the LINKED document, and RevitAPI.xml says only a `RevitLinkInstance` is supported. **Risk and confirmation:** ADMIN, as `ACQUIRE_COORDINATES` is (D-106, project setup); Article 7 - it touches a linked model - so it runs only with `confirm` repeating the link's name exactly, its file name as Manage Links lists it or its instance name, as `REMOVE_PROJECT_PARAMETER` and `DELETE_REVISION_NUMBERING_SEQUENCE` ask for theirs. **Article 10, and why the link is not saved here:** the call records the position in the session; the linked FILE changes only when it is saved. `RevitLinkType.SavePositions(ISaveSharedCoordinatesCallback)` would save it now - but its callback must be a class, which a fragment cannot declare, it cannot be undone (RevitAPI.xml), and it is a save of another file, never this card's on its own initiative. Revit's own Location Position Changed question at the modeller's next save of the host is where the link is written, and the answer is the modeller's. `RevitLinkType.HasSaveablePositions` is read back to say whether a position is waiting. **What is not known:** whether `PublishCoordinates` runs inside the host's open transaction (`ACQUIRE_COORDINATES`' proof says its sibling did, on Revit 2020); whether the linked document's `ProjectLocation` reads the new values before the link is saved and reloaded; whether the run guard counts a published position as a change to another model; and what Revit does with the waiting position when the host is saved by `SAVE_DOCUMENT` rather than the modeller's Ctrl+S. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#905](https://github.com/Ajmalpshaik/Heron-AI/issues/905) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MO's MO6 and MO7 record the publish, the read-back and the save question; saving into the link stays the modeller's in Revit's own dialog.

---

### Row 5b-1462

*Moved from the register on 2026-11-17.*

**Defect.** **ROUTING AFTER GROUP MO: NO QUESTION REACHES A WRITE WITH THE HOST'S QUESTION FLAG; WITHOUT IT, SEVEN SENTENCES NOW REACH ONE OF THE FOUR NEW WRITES, NONE OF THEM DECLARED BY ITS OWNER.** Measured 2026-11-03 with `heron_retrieve.find` on fresh scratch stores of the tree at `e4ffca9` and of this change, Revit 2024, with and without the `READ` ceiling, over every declared utterance, identity and routing-table sentence in the library and ten probes (6,021 sentences). Every sentence the four cards declare reaches its card by identity. `check-routing` exits 0 on the same 66 claimed-and-unreached sentences as before; its keyword half gains two contests, *"diff this model against the one on disk"* (`INSERT_VIEWS_FROM_FILE` first on words) and *"put this element at exactly this coordinate"* (`PUBLISH_COORDINATES` first on words), both still answered by `find` as before. `test_routing_phrases` fails the same 7 lines as at `e4ffca9`. **With the flag**, 13 sentences moved read to read. **Without it**, these moved onto a new write: *"open this file from disk"* (`SWITCH_ACTIVE_PROJECT`'s table says nothing in Heron opens a file; it reached a read, now `INSERT_VIEWS_FROM_FILE`); *"what file is this"*, *"what is this file"*, *"what is in this file"* (`READ_MODEL_FILE_INFO` claims the first; all three already reached a write, `RELOAD_IMAGES`, and now reach `INSERT_VIEWS_FROM_FILE`); *"where is it now"* (`REPORT_LOCATION` claims it; it already reached an ADMIN write, `ACQUIRE_COORDINATES`, and now reaches `PUBLISH_COORDINATES`, which still refuses without the link's name repeated as `confirm`); *"offset"* and *"put these lines on the right"* (reached reads, now `OFFSET_LINE_CHAIN`); and the probes *"what is the systems analysis result"* and the bare *"Systems Analysis Report"* (reached `REPORT_ENERGY_SETTINGS`, now `RUN_SYSTEMS_ANALYSIS`). Two write-to-write moves: *"make a new duct type at 300 wide"* (`DUPLICATE_TYPE`'s own words, reached neither before nor after) and *"resize that scope box"* (a NOT POSSIBLE line, now a read). None is declared anywhere, because no owner's sentence that reached its owner before was moved, and the rest are made-up wording or NOT HERE lines. The gates were finished by the parent session after two container restarts cut off the authoring runs.

**State.** **MOVED TO ISSUE [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - the host's question flag keeps every question above off a write; the owner decides whether *"where is it now"* and *"what file is this"* should be declared on their readers.
