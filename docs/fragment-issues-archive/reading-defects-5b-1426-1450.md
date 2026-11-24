# FRAGMENT-ISSUES archive — section 5b, rows 1426 to 1450

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
> Rows 1426 to 1450 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1426

*Moved from the register on 2026-11-17.*

**Defect.** ***"PUT IT BACK"* MOVED A FOURTH TIME, AND A MADE-UP PARAPHRASE OF A QUESTION MOVED ONTO A WRITE, WHEN `SET_OBJECT_STYLE` WAS ADDED.** Seen 2026-11-02 by `tools/check-routing.py` on two scratch stores, without the card and with this change, `model` backend: *"put it back"* (`FRG-VIEW-007 claims it`) went from `FRG-VIEW-005 is SERVED` back to `FRG-ELE-004 is SERVED` - the lookup with no question flag answered `RESET_GRAPHIC_OVERRIDES` before and `PLACE_FAMILY_INSTANCES` after, the flip of rows 1375, 1405 and 1424. Nothing else left a card that claims it. Separately, three probe questions nobody has said, asked to check the new card's pull: *"what is the projection line weight of Doors"* went from `LIST_CATEGORIES` to `SET_OBJECT_STYLE` without the flag; *"what line pattern do ducts use"* and *"which line weight is the cut line of walls"* from `CREATE_LINE_PATTERN` and `CREATE_LINE_STYLE` to it - write to write. With the flag all three reach a read. On the keyword route alone *"all the ducts"* and *"connect these two ducts"* rank a different other card first, and the lookup answers both by identity on their own cards as before.

**State.** **MOVED TO ISSUE [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - *"put it back"* is not declared, for row 1375's reason (the owner's choice between unhiding and clearing overrides). The three probes are not declared anywhere because nobody said them (never invent utterances); a rewording of the new card's identity to push them off was measured and moved nothing, and was taken back. The host's question flag keeps them off every write (D-128).

---

### Row 5b-1428

*Moved from the register on 2026-11-17.*

**Defect.** ***"PUT IT BACK"* MOVED A FIFTH TIME, WRITE TO WRITE, WHEN `ADD_BEAM_ANNOTATIONS` WAS ADDED.** Seen 2026-11-02 while building it (FRG-VIEW-172, DRAFT, a write - [Group MC](../needs-checking/group-mc.md)), by `tools/check-routing.py` on two scratch `HERON_KNOWLEDGE` stores, a copy of the tree at `2ccab64` without the card and this change, `model` backend: *"put it back"* (`FRG-VIEW-007 claims it`) went from `FRG-ELE-004 is SERVED` to `FRG-VIEW-005 is SERVED` - the host's lookup (`heron_brain.lookup`, Revit 2024, 2027 and none, no question flag) answered `PLACE_FAMILY_INSTANCES` before and `RESET_GRAPHIC_OVERRIDES` after, the flip of rows 1375, 1405, 1424 and 1426, through the ranking (the new card shares no word with it). Nothing else left a card that claims it: claimed and not reached was 66 before and 66 after, the same lines but this one. On the keyword route alone *"add spot elevations"* (declared by `ADD_SPOT_ELEVATIONS`) now ranks the new card first where it ranked `REPORT_SPOT_DIMENSIONS` first - the lookup answers it by identity on `ADD_SPOT_ELEVATIONS` before and after, on all three releases; every sentence the new card declares reaches it by identity.

**State.** **MOVED TO ISSUE [#928](https://github.com/Ajmalpshaik/Heron-AI/issues/928) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - not declared on any card, for row 1375's reason: whether the bare sentence means unhiding (`SHOW_ELEMENTS`) or clearing overrides (`RESET_GRAPHIC_OVERRIDES` declares *"put it back how it was"*) is the owner's choice.

---

### Row 5b-1429

*Moved from the register on 2026-11-17.*

**Defect.** **A FRAGMENT CANNOT ANSWER REVIT'S "DUPLICATE TYPES" QUESTION, SO A COPY BETWEEN PROJECTS THAT DRAGS A TYPE ALREADY NAMED HERE IS ROLLED BACK WHOLE.** Found 2026-11-02 while building `TRANSFER_PROJECT_STANDARDS` (FRG-ELE-161, DRAFT, a write - [Group MD](../needs-checking/group-md.md)), read only. `CopyPasteOptions.SetDuplicateTypeNamesHandler` takes an `IDuplicateTypeNamesHandler`, which needs a class - a fragment is a body of statements and cannot declare one - and RevitFragment binds no ambient answer for it as it binds `familyLoadOptions` for Revit's "Family Already Exists" ([D-114](../decisions/D-114.md)). RevitAPI.xml, every release: *"By default, Revit displays a modal dialog with options to either copy new types only, or cancel the operation."* `HeronDialogWatch` declines a dialog no row names, and its own comment says a fragment that sets no handler *"meets 'Duplicate Types' here and is cancelled"*. So a text type whose arrowhead, a wall type whose profile or a stacked wall whose member type is already named in the project in front stops the whole run, and nothing is kept. The new card keeps this project's by never handing a same-name type to the copy, but cannot reach the types those USE. The same holds for every document-to-document copy in the library (`TRANSFER_VIEWS_BETWEEN_DOCUMENTS`, `TRANSFER_VIEW_FILTERS_BETWEEN_DOCUMENTS`, `TRANSFER_MATERIALS_BETWEEN_DOCUMENTS`, `COPY_ELEMENTS_BETWEEN_DOCUMENTS`). Nothing was run: no Revit here, and the dialog's id is not recorded.

**State.** **MOVED TO ISSUE [#910](https://github.com/Ajmalpshaik/Heron-AI/issues/910) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - the safe failure (nothing replaced, all rolled back), not this change's to repair: the add-in would bind an ambient handler answering `UseDestinationTypes` - D-114's pattern - or a [D-145](../decisions/D-145.md) caller row once a real run records the dialog's id (Group MD, MD4). The owner decides.

---

### Row 5b-1431

*Moved from the register on 2026-11-17.*

**Defect.** ***"WHAT COMBINATIONS ARE IN THIS MODEL"* - A PROBE WITHOUT THE WORD "LOAD" - REACHES `CREATE_LOAD_COMBINATION`, A WRITE, WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 while building it ([Group ME](../needs-checking/group-me.md)), the measurement of row 1430: no Revit filter, 2020 and 2024 alike; with the flag it reaches `REPORT_STRUCTURAL_LOADS`. Before the card it reached `COMPARE_MODELS`, a read. The declared question *"what load combinations are in this model"* reaches `REPORT_STRUCTURAL_LOADS` with and without the flag.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody's words, so not declared on either card (D-86's rule against inventing utterances); the host's question flag is what keeps it from the write. Declared on `REPORT_STRUCTURAL_LOADS` the day a modeller says it.

---

### Row 5b-1433

*Moved from the register on 2026-11-17.*

**Defect.** **FOUR MADE-UP QUESTIONS ABOUT STRUCTURAL AND REINFORCEMENT SETTINGS REACH THE NEW ADMIN WRITES WITHOUT THE HOST'S QUESTION FLAG, AND *"PUT IT BACK"* FLIPPED AGAIN.** Seen 2026-11-02 building `REPORT_STRUCTURAL_SETTINGS`, `SET_STRUCTURAL_SETTINGS` and `SET_REINFORCEMENT_SETTINGS` ([Group MF](../needs-checking/group-mf.md)), through `heron_retrieve.find` on fresh scratch stores of the tree at `65062d4` and of this change, Revit 2024, over every declared utterance and routing-table sentence (4,459): *"what is the beam symbolic cutback distance"* went from `SET_FRAMING_END_EXTENSION` to `SET_STRUCTURAL_SETTINGS`; *"what is the rebar rounding"* from `PLACE_MULTI_REBAR_ANNOTATION`, and *"is rebar shape defines hooks on"* and *"does the rebar shape include hooks"* from `LIST_REBAR_TYPES`, to `SET_REINFORCEMENT_SETTINGS` - write to write, and read to write for the last two. With the flag all four reach a read (`REPORT_STRUCTURAL_SETTINGS`, or `LIST_REBAR_TYPES` for the hooks pair). *"put it back"* (claimed by `SHOW_ELEMENTS`) answered `RESET_GRAPHIC_OVERRIDES` before and `PLACE_FAMILY_INSTANCES` after with no flag - row 1426's flip. The modeller's two questions reach the read with and without the flag, and no other card's declared sentence lost the card it reached.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody said the four, so they are declared nowhere (never invent utterances). Naming them in the read's `purpose` was measured: it brought *"show the structural settings"* back to the read and moved none of the four, and was kept for that. The host's question flag keeps them off both writes (D-128), and both are ADMIN, behind the owner's switch. *"put it back"*: row 1375's reason.

---

### Row 5b-1435

*Moved from the register on 2026-11-17.*

**Defect.** **THREE MADE-UP QUESTIONS ABOUT THE ANALYTICAL MODEL REACH THE NEW ANALYTICAL WRITES WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 building them ([Group MG](../needs-checking/group-mg.md)), through `heron_retrieve.find` on fresh scratch stores of the tree at `ef290af` and of this change, Revit 2024, over every declared utterance and routing-table sentence of a live card and ten probes (5,634 sentences): *"is this beam associated with an analytical member"* went from `REPORT_ANALYTICAL_MODEL` to `ASSOCIATE_ANALYTICAL_ELEMENTS`, *"what analytical links are in this model"* from `REPORT_ANALYTICAL_MODEL` to `CREATE_ANALYTICAL_ELEMENTS`, and *"where is the analytical line of this beam"* from `PLACE_LINE_BASED_FAMILY` to `ADJUST_ANALYTICAL_ELEMENTS`. With the flag all three reach `REPORT_ANALYTICAL_MODEL`. Four other analytical probes reach the read with and without the flag. No card's declared sentence lost the card it reached, and no routing-table sentence lost the card that claims it.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody said the three, so they are declared nowhere (never invent utterances). The host's question flag keeps them off the writes (D-128). Declared on `REPORT_ANALYTICAL_MODEL` the day a modeller says one.

---

### Row 5b-1440

*Moved from the register on 2026-11-17.*

**Defect.** **NO CARD LINKS AN IFC FILE, AND NONE CAN WHILE A FRAGMENT CANNOT RUN REVIT'S IFC IMPORTER.** Found 2026-11-02 while widening `LINK_FILES` to Insert > Point Cloud ([Group MK](../needs-checking/group-mk.md)), read only. `tools/api-surface --members RevitLinkType`: `CreateFromIFC(Document, string ifcFilePath, string revitLinkedFilePath, bool recreateLink, RevitLinkOptions)` and its `ExternalResourceReference` twin are present **2020 to 2027**, as are `UpdateFromIFC`. RevitAPI.xml, every release: it *"creates a new Revit link type from an existing Revit file created via import by reference of an associated IFC file"*, is *"one of a series of steps necessary for linking an IFC file"* - the others are in the open-source IFC importer's `Importer.ImportIFC`, `IFCImportAction.Link` branch, an add-in rather than RevitAPI - and *"regenerates the input document"*. So the `.ifc.RVT` that Revit's Insert > Link IFC writes must already exist; a fragment, a body of statements inside the add-in's transaction, has no way to make it. `LINK_FILES` version 2 names an `.ifc` file in its report and links nothing, rather than faking it. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#919](https://github.com/Ajmalpshaik/Heron-AI/issues/919) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - not buildable as a fragment. The owner decides whether linking an `.ifc.RVT` that already exists (written by an earlier Insert > Link IFC) is worth a card, or whether the add-in gains a route to the IFC importer.

---

### Row 5b-1441

*Moved from the register on 2026-11-17.*

**Defect.** **`LINK_FILES` LINKS A POINT CLOUD ORIGIN TO ORIGIN ONLY; BY SHARED COORDINATES IS NAMED AND NOT LINKED.** Found 2026-11-02 while building version 2 ([Group MK](../needs-checking/group-mk.md)). `PointCloudInstance.Create(Document, ElementId, Transform)` takes the placement as a transform (2020 to 2027, `tools/api-surface --members`); origin to origin is `Transform.Identity`. By shared coordinates needs the transform from the project's shared coordinates to its internal ones - `ProjectLocation`'s, read off `Instance.GetTotalTransform`, whose RevitAPI.xml entry says only *"gets the total transform"* and not which way it maps - so which of it or its inverse places a scan where Insert > Point Cloud's *Auto - By Shared Coordinates* does is not on any page read here. Guessing would put a building's scan in the wrong place with no error; version 2 names the file - *"a point cloud is linked origin to origin only - by shared coordinates is not built here, so it was not linked"* - and never links it origin to origin instead. A Revit or CAD link by shared coordinates is unchanged.

**State.** **MOVED TO ISSUE [#946](https://github.com/Ajmalpshaik/Heron-AI/issues/946) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MK's MK3 records where Revit's own *Auto - By Shared Coordinates* puts a scan in a project whose shared coordinates are moved off its internal origin; the transform is written from that, and proved, before shared is built.

---

### Row 5b-1443

*Moved from the register on 2026-11-17.*

**Defect.** ***"WHICH IMAGES NEED RELOADING"* - A MADE-UP QUESTION - REACHES `RELOAD_IMAGES`, A WRITE, WITHOUT THE HOST'S QUESTION FLAG; AND THREE POINT CLOUD AND IFC PROBES REACH WRITES AS THEY DID BEFORE.** Seen 2026-11-02, the measurement of row 1442, over every declared utterance, identity and routing-table sentence of every live card and 22 probes (5,084 sentences). *"which images need reloading"* reached `LIST_IMAGES` before; with the flag it still does. No card can answer it truly - Manage Images cannot say a file changed since it was loaded. Unchanged by this change, before and after, without the flag: *"link this ifc file"* reaches `EXPORT_MODEL_TO_IFC` (PUBLISH - no card links an IFC, row 1440), *"is the point cloud loaded"* `ADD_REVISION_CLOUD` and *"what point clouds are in this model"* `DRAW_FAMILY_POINT_CURVES`; with the flag each reaches a read. The rest of the sweep: every sentence the two cards declare reaches them by identity; *"link this point cloud"* moved from `ADD_REVISION_CLOUD` to `LINK_FILES` and *"reload the images"* from `LOAD_FAMILY` to `RELOAD_IMAGES`; one write-to-write move on Revit 2020 only, *"subdivide the toposolid for this path"* (`CREATE_TOPOSURFACE` to `EDIT_SLAB_SHAPE`), whose claimant `EDIT_TOPOSOLID` is 2024 on; four read-to-read moves with the flag, not tuned. `tools/check-routing.py`: no question answered by a write, the same 66 claimed sentences unreached, two contests fewer (*"is there a CAD link in here"* no longer lost to `LINK_FILES`). `tests/test_routing_phrases.py` fails exactly its known seven.

**State.** **MOVED TO ISSUE [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody's words, so none is declared (D-86's rule against inventing utterances); the host's question flag keeps each from a write. Declared on `LIST_IMAGES`, or on a point cloud reader if one is built, the day a modeller says one.

---

### Row 5b-1445

*Moved from the register on 2026-11-17.*

**Defect.** **WHICH NUMBER MEANS WHICH JUSTIFICATION IS NOT ON ANY PAGE READ HERE.** Found 2026-11-02 building `SET_FRAMING_JUSTIFICATION` ([Group ML](../needs-checking/group-ml.md)), read only. `BuiltInParameter.YZ_JUSTIFICATION`, `Y_JUSTIFICATION`, `Z_JUSTIFICATION` and their `START_` / `END_` forms are present 2020 to 2027 (`tools/api-surface --members`), and RevitAPI.xml gives each only its Properties name (*"yz Justification"*, *"z Justification"*); the values are whole numbers with no enum in RevitAPI. The card assumes Uniform 0, Independent 1; y Left 0, Center 1, Origin 2, Right 3; z Top 0, Center 1, Origin 2, Bottom 3. To keep a wrong guess from standing, each choice written is checked against the word Revit then shows (`AsValueString`): another of the same choice's words puts the old value back and refuses. `REPORT_FRAMING_JUSTIFICATION` prints Revit's word and uses the number only to tell Uniform from Independent. A Revit in another language shows other words, and the check then passes on the number alone. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group ML's ML2 and ML3 record the word Revit shows for every number on a real beam; the card's table is corrected from them, before any proof.

---

### Row 5b-1446

*Moved from the register on 2026-11-17.*

**Defect.** **`PLACE_STAIR_TREAD_NUMBERS` SETS TREAD OR RISER, AND THE START NUMBER ON REVIT 2027, FROM WHAT REVIT SHOWS RATHER THAN FROM A PAGE.** Found 2026-11-02 building it ([Group ML](../needs-checking/group-ml.md)), read only. `NumberSystem` has no property for tread or riser on any release 2020 to 2027 (`tools/api-surface --members`); the choice is the instance parameter *"Tag Type"* (`BuiltInParameter.NUMBER_SYSTEM_TAG_TYPE`), a whole number with no documented meaning, so each value 0 to 3 is tried until Revit shows the word asked for, and a run where none does is rolled back in its SubTransaction and named. The start: *"Tread/Riser Start Number"* (`STAIRS_TRISER_NUMBER_BASE_INDEX`) is on the stair on every release and numbers it in every view; Revit 2027 adds *"Start Number"* (`NUMBER_SYSTEM_START_NUMBER`) on the numbers themselves, used there when it can be set - which of the two Revit 2027 counts from is not known. RevitAPI.xml also says only floor plans and elevations are valid views; a section is passed on and Revit's refusal, if any, reported. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#903](https://github.com/Ajmalpshaik/Heron-AI/issues/903) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group ML's ML5 records the Tag Type words, which start Revit 2027 uses, and whether a section takes the numbers; the card is corrected from them before any proof.

---

### Row 5b-1447

*Moved from the register on 2026-11-17.*

**Defect.** **`REMOVE_PROJECT_PARAMETER` DELETES A PARAMETER THAT IS NOT SHARED, AND ONLY UNBINDS A SHARED ONE - BOTH CHOSEN WITHOUT SEEING WHAT REVIT'S OWN REMOVE DOES.** Found 2026-11-02 building it ([Group ML](../needs-checking/group-ml.md)), read only. RevitAPI.xml says of `BindingMap.Remove(Definition)` only that it *"is used to remove a parameter binding"*, and nothing of the `ParameterElement` behind it. So a parameter that is not shared is deleted with `Document.Delete`, which takes its binding with it and returns what else went; a shared parameter's binding is removed and its `SharedParameterElement` kept, because a loaded family may carry the same shared parameter and deleting the definition would reach into it. What the dialog's Remove does to a schedule column or a view filter rule that used the parameter is not on any page read here either; the card names them before anything goes (`alsoAffected`) and does not claim what happens to them. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#907](https://github.com/Ajmalpshaik/Heron-AI/issues/907) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group ML's ML6 records what Revit does to the "ML Walls" column, and whether a schedule still offers the shared parameter after its binding goes; ML7 compares with the dialog's Remove.

---

### Row 5b-1448

*Moved from the register on 2026-11-17.*

**Defect.** **FOUR MADE-UP SENTENCES STILL MISS THEIR CARD AFTER GROUP ML: TWO STAIR QUESTIONS REACH `PLACE_STAIR_TREAD_NUMBERS`, A WRITE, WITHOUT THE HOST'S QUESTION FLAG; *"SET THE START LEVEL OFFSET OF THIS BEAM TO 300 MM"* REACHES `REPORT_FRAMING_JUSTIFICATION`; AND THE BARE *"SET THE JUSTIFICATION"* REACHES IT TOO.** Seen 2026-11-02, the measurement of row 1444 (16 probes). *"does this stair have tread numbers"* and *"what is the tread start number of this stair"* reached `CREATE_STAIRS` before and the new write after - a write either way; with the flag each reaches a read (`REPORT_REBAR_NUMBERING` and `MEASURE_MEP_SLOPE`, neither of which can answer). No card reads tread numbers. *"set the start level offset of this beam to 300 mm"* went from `SET_FRAMING_END_EXTENSION` to `REPORT_FRAMING_JUSTIFICATION` without the flag - a request answered by a read, the safe way round. *"set the justification"*, which `SET_MEP_JUSTIFICATION`'s routing table claims (`-> here`) and which reached `MANAGE_SHEET_SETS` before, reaches `REPORT_FRAMING_JUSTIFICATION` after; it names neither ducts nor beams, and that table now says to ask. Unchanged: *"what project parameters are in this model"* reaches `TRANSFER_PROJECT_PARAMETERS_BETWEEN_DOCUMENTS` (ADMIN) without the flag before and after - no card lists project parameters. With the flag, twenty-six declared or claimed sentences moved between cards under the question ceiling - reads, one ANALYZE and two EXECUTE - fourteen of them to `REPORT_FRAMING_JUSTIFICATION`; recorded and not tuned. Without the flag, *"set the justification"* is the only one that moved. `tools/check-routing.py`: no question answered by a write.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody's words, so none is declared (D-86's rule against inventing utterances); the host's question flag keeps each question from a write. A tread number reader or a project parameter list is a card of its own the day a modeller asks.

---

### Row 5b-1449

*Moved from the register on 2026-11-17.*

**Defect.** **`SET_MECHANICAL_SETTINGS` WRITES THE FITTING AND RISE/DROP ANNOTATION SIZES AS FEET FROM MILLIMETRES, AND CANNOT REACH THE RISE/DROP SYMBOLS AT ALL.** Found 2026-11-02 building it and `REPORT_MECHANICAL_SETTINGS` ([Group MM](../needs-checking/group-mm.md)), read only. `DuctSettings.FittingAnnotationSize`, `RiseDropAnnotationSize` and `PipeSettings.FittingAnnotationSize` are plain doubles 2020 to 2027 (`tools/api-surface --members`) with no unit on any page read here; the cards take millimetres and divide by 304.8, as for every Revit length (D-20), and a paper size held another way would read back wrong. Air density and viscosity go through Revit's own `UnitUtils`, nothing assumed. The rise/drop SYMBOLS asked for have no member on `DuctSettings` or `PipeSettings` on any release; the assemblies name them only as system-type parameters (`RBS_SYSTEM_RISEDROP_*`), so the write refuses them by name and says where they live. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MM's MM3 records that 5 mm reads back as 5 mm in Mechanical Settings > Duct Settings; the cards are corrected from it before any proof. A rise/drop symbol card is one of its own the day a modeller asks.

---

### Row 5b-1450

*Moved from the register on 2026-11-17.*

**Defect.** **`SET_ENERGY_SETTINGS` ASKS REVIT HOW IT HOLDS THE GLAZING PERCENTAGE, AND DOES NOT KNOW WHAT THE ENERGY MODEL SWITCH DOES BY ITSELF.** Found 2026-11-02 building it and `REPORT_ENERGY_SETTINGS` ([Group MM](../needs-checking/group-mm.md)), read only. `EnergyDataSettings.PercentageGlazing` is a double with no stated form; both cards decide between 40 and 0.4 by asking `CheckRangeOfPercentageGlazing(50.0)` on the release that runs - a percent range takes 50, a fraction range does not - and print and write in that form. `EnergyModel` (2020-2027) is what Revit 2023 named as the replacement for `SetCreateAnalyticalModel`; whether setting it builds or removes an energy model by itself is not on any page read here. The sliver space tolerance, grid cell size and core offset are taken as feet from millimetres. Nothing was run: no Revit here.

**State.** **MOVED TO ISSUE [#906](https://github.com/Ajmalpshaik/Heron-AI/issues/906) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - Group MM's MM4 and MM5 record the glazing beside the dialog's, the lengths as the dialog shows them, and what REPORT_ENERGY_SETTINGS says after the switch is turned.

---
