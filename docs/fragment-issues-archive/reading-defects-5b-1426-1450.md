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

### Row 5b-1427

*Moved from the register on 2026-11-17.*

**Defect.** **`LIST_CATEGORIES` READS NO MATERIAL, SO *"WHAT MATERIAL IS SET FOR GENERIC MODELS IN OBJECT STYLES"* IS ANSWERED WITHOUT ONE.** Found 2026-11-02 while building `SET_OBJECT_STYLE`: Object Styles' Model Objects tab has a Material column, `LIST_CATEGORIES` version 3 reports colour, projection and cut weight and pattern only, and the question routes there (the right card - a read). The only thing in Heron that reads a category's material is the new write's `before` line, which is not a question's answer.

**State.** **MOVED TO ISSUE [#926](https://github.com/Ajmalpshaik/Heron-AI/issues/926) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - not this change's to fix: widening `LIST_CATEGORIES` is a change under its `impl/` and would make its proof stale (PROVEN on main). The owner decides whether it gains a material column.

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

### Row 5b-1430

*Moved from the register on 2026-11-17.*

**Defect.** **THE FIRST DRAFT OF `CREATE_LOAD_COMBINATION` PULLED THREE QUESTIONS ABOUT LOAD COMBINATIONS TO THE WRITE WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 while building it (FRG-ELE-162, DRAFT, a write - [Group ME](../needs-checking/group-me.md)), through `heron_retrieve.find` on fresh scratch `HERON_KNOWLEDGE` stores of the tree at `0597dfa` and of this change, `model` backend, no Revit filter, 2020 and 2024: the probes *"which load combinations does this model have"* (`REPORT_STRUCTURAL_LOADS`, or `READ_SPACE_LOADS` on a release, before), *"show me the load combinations"* (`REPORT_PANEL_CIRCUITS` before) and *"what combinations are in this model"* (`COMPARE_MODELS` before) reached `CREATE_LOAD_COMBINATION` without the flag. Nobody's words, so none is declared.

**State.** **FIXED in the same change, at DRAFT** - the write's purpose says the combinations are read back from the model rather than reported, and `REPORT_STRUCTURAL_LOADS`' purpose says which load combinations the model has (outside `impl/`): measured again, *"which load combinations does this model have"*, *"show me the load combinations"* and *"list the load combinations"* reach `REPORT_STRUCTURAL_LOADS` with and without the flag on all three. The probe without the word "load" still reaches the write (row 1431).

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

### Row 5b-1434

*Moved from the register on 2026-11-17.*

**Defect.** ***"EXPORT THE ANALYTICAL MODEL"* - `REPORT_ANALYTICAL_MODEL`'S OWN ROUTING ROW - WENT TO `CREATE_ANALYTICAL_ELEMENTS`, A WRITE, WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 building `CREATE_ANALYTICAL_ELEMENTS`, `ADJUST_ANALYTICAL_ELEMENTS` and `ASSOCIATE_ANALYTICAL_ELEMENTS` ([Group MG](../needs-checking/group-mg.md)): `tools/check-routing.py` on fresh scratch `HERON_KNOWLEDGE` stores of the tree at `ef290af` and of this change listed it under CLAIMED IN A ROUTING TABLE AND NOT REACHED only after - `REPORT_ANALYTICAL_MODEL` (FRG-ELE-089) claims it, the new create card was served, by the hybrid route. The sentence was written in that card's routing row, not declared as an utterance.

**State.** **FIXED in the same change** - declared on `REPORT_ANALYTICAL_MODEL` (DRAFT on main; its card only, outside `impl/`, so no fingerprint moves), with routing rows naming the three new cards. Measured again: it reaches `REPORT_ANALYTICAL_MODEL` by identity, and `check-routing` no longer lists it.

---

### Row 5b-1435

*Moved from the register on 2026-11-17.*

**Defect.** **THREE MADE-UP QUESTIONS ABOUT THE ANALYTICAL MODEL REACH THE NEW ANALYTICAL WRITES WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 building them ([Group MG](../needs-checking/group-mg.md)), through `heron_retrieve.find` on fresh scratch stores of the tree at `ef290af` and of this change, Revit 2024, over every declared utterance and routing-table sentence of a live card and ten probes (5,634 sentences): *"is this beam associated with an analytical member"* went from `REPORT_ANALYTICAL_MODEL` to `ASSOCIATE_ANALYTICAL_ELEMENTS`, *"what analytical links are in this model"* from `REPORT_ANALYTICAL_MODEL` to `CREATE_ANALYTICAL_ELEMENTS`, and *"where is the analytical line of this beam"* from `PLACE_LINE_BASED_FAMILY` to `ADJUST_ANALYTICAL_ELEMENTS`. With the flag all three reach `REPORT_ANALYTICAL_MODEL`. Four other analytical probes reach the read with and without the flag. No card's declared sentence lost the card it reached, and no routing-table sentence lost the card that claims it.

**State.** **MOVED TO ISSUE [#929](https://github.com/Ajmalpshaik/Heron-AI/issues/929) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody said the three, so they are declared nowhere (never invent utterances). The host's question flag keeps them off the writes (D-128). Declared on `REPORT_ANALYTICAL_MODEL` the day a modeller says one.

---

### Row 5b-1436

*Moved from the register on 2026-11-17.*

**Defect.** ***"PLACE THE AREAS"* - CLAIMED FOR `PLACE_ROOMS` BY `CREATE_ROOM_SEPARATION_LINES`' ROUTING TABLE - WENT TO `CREATE_AREA_PLAN` WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 building `CHANGE_FABRICATION_SERVICE_SIZE`, `ADD_FABRICATION_HANGERS` and `PLACE_FABRICATION_PART` ([Group MH](../needs-checking/group-mh.md)), through `heron_retrieve.find` on fresh scratch stores of the tree at `743c641` and of this change, Revit 2024, with and without the flag, over every declared utterance and routing-table sentence of a live card (5,630 sentences): it answered `PLACE_ROOMS` before and `CREATE_AREA_PLAN` after. The sentence was written in a routing row, not declared as an utterance, so it rode the ranked route, and three new cards shifted the fusion. Seven other sentences changed answer, none to or from a card that claims them except one that moved TO its claimant (*"list the materials with their appearance"*, `SET_MATERIAL_APPEARANCE` to `LIST_MATERIALS`): *"add a row to the duct schedule"* (claimed by nothing), and with the flag, where the write that claims each is held back, *"change the type of these air terminals to another type"*, *"line these up"*, *"line these up on their tops"*, *"make a sheet for each level"* and a quoted log line moved from one read to another - recorded, not tuned. No sentence reached the three new cards but their own six.

**State.** **FIXED in the same change** - declared on `PLACE_ROOMS` (PROVEN on main; its card only, outside `impl/`, so no fingerprint moves), with a routing row. Measured again: it reaches `PLACE_ROOMS` by identity.

---

### Row 5b-1437

*Moved from the register on 2026-11-17.*

**Defect.** ***"PUT THE DUCT TAGS ABOVE THE DUCTS WITH A LEADER"* - HELD FOR `OFFSET_TAGS_FROM_HOST` BY `tests/test_routing_phrases.py` - NOW ANSWERS `TAG_ELEMENTS_IN_VIEW`.** Seen 2026-11-02 building `CHANGE_FABRICATION_SERVICE_SIZE`, `ADD_FABRICATION_HANGERS` and `PLACE_FABRICATION_PART` ([Group MH](../needs-checking/group-mh.md)). On a clean copy of the tree at `743c641` the suite failed its known seven; with this change, eight. `heron_retrieve.find` (no Revit filter) on scratch stores before and after: `OFFSET_TAGS_FROM_HOST` was words #1 and nearness **#20** - the last place the fusion reads - and `ADD_FABRICATION_HANGERS` (its utterance *"put a hanger on this fabrication pipe"*) came in at nearness #17, so the tag card fell to #21, kept only its words rank, and `TAG_ELEMENTS_IN_VIEW` (words #7, nearness #12) won by 0.00008 before and outright after. The suite refuses a declared copy of the sentence - it must be reached by ranking - so declaring it is not the repair. Rewording the hanger card's `purpose` ("the structure above") alone was measured and moved nothing.

**State.** **FIXED in the same change** - `ADD_FABRICATION_HANGERS`' own `semantic-identity` and `purpose` reworded (no utterance touched, no other card): fewer of the sentence's words (*put ... on these*, *above*) and what the card does in fabrication words (hosted on horizontal straights, the button's rods, bearer and condition). Measured again on a fresh store: the hanger card falls from nearness 0.343 to 0.328, below `OFFSET_TAGS_FROM_HOST` (0.338), which is back at #20 and answers; `tests/test_routing_phrases.py` fails exactly its known seven; the hanger card's own two sentences still reach it.

---

### Row 5b-1438

*Moved from the register on 2026-11-17.*

**Defect.** ***"WHAT LATITUDE IS THE SITE SET TO"* - A QUESTION `REPORT_LOCATION`'S ROUTING TABLE CLAIMS ("-> here") - REACHED `SET_PROJECT_LOCATION`, AN ADMIN WRITE, WHEN `ADD_WIRE_TYPE`, `ADD_AREA_BASED_LOAD` AND `ADD_ELECTRICAL_ANALYTICAL_NODE` WERE ADDED.** Seen 2026-11-02 building the three cards ([Group MI](../needs-checking/group-mi.md)), through `heron_retrieve.find` on fresh scratch `HERON_KNOWLEDGE` stores of the tree at `d7acd42` and of this change, `model` backend, re-ranker absent, no Revit filter, 2024 and 2020, with and without the host's question flag (D-128), over every declared utterance and routing-table sentence of a live card (4,486). Without the flag it answered `REPORT_LOCATION` before and `SET_PROJECT_LOCATION` after on all three - **caused by this change**, through the ranking: the sentence was written in a routing row, not declared, so it rode the ranked route. With the flag it stayed on `REPORT_LOCATION`. Every other sentence asked without the flag kept its answer but the new cards' own five and *"put it back"*, which flipped from `PLACE_FAMILY_INSTANCES` to `RESET_GRAPHIC_OVERRIDES`, write to write, on all three - the flip [row 1375](../FRAGMENT-ISSUES.md#) (OPEN) records, claimed by neither. With the flag seven sentences each claimed by a write moved from one read to another - *"bring the excluded parts back"*, *"change the type of these air terminals to another type"*, *"copy the legends from the other project"*, *"create a power circuit from this selection"*, *"i need this schedule in excel"*, *"make level 1 retail and the rest office"* and *"transfer project standards: wall types and materials from the template"* - recorded, not tuned.

**State.** **FIXED in the same change** - declared on `REPORT_LOCATION` (FRG-GEO-025, PROVEN on main; its card only, outside `impl/`, so no fingerprint moves), with a dated comment. Measured again on a fresh store: it reaches `REPORT_LOCATION` by identity with and without the flag, and no other sentence moved but the ones above. `tests/test_routing_phrases.py` fails exactly its known seven.

---

### Row 5b-1439

*Moved from the register on 2026-11-17.*

**Defect.** ***"DRAW A NEW DUCT"* - CLAIMED FOR `CREATE_MEP_RUNS` BY `SPLIT_MEP_RUN`'S ROUTING TABLE - WENT TO `CONVERT_TO_FLEX_DUCT` WHEN `CREATE_TOPOSURFACE` AND `CREATE_PROPERTY_LINE` WERE ADDED.** Seen 2026-11-02 building the two cards ([Group MJ](../needs-checking/group-mj.md)), through `heron_retrieve.find` on fresh scratch `HERON_KNOWLEDGE` stores of the tree at `dd31db0` and of this change, `model` backend, re-ranker absent, no Revit filter, with and without the host's question flag (D-128, ceiling EXECUTE), over every declared utterance, identity and routing-table sentence of a live card (5,792 before, 5,799 after). Without the flag it answered `CREATE_MEP_RUNS` before and `CONVERT_TO_FLEX_DUCT` after, write to write, a coin toss of 0.4 of one fusion rank: `CREATE_MEP_RUNS` fell from #5 to #6 on words - **caused by this change**, through the ranking, the sentence written in a routing row and never declared. Declaring it on `CREATE_MEP_RUNS` then sent *"how long is this duct run"* - claimed for `MEASURE_RUN_QUANTITIES` by `REPORT_BOUNDING_BOX`'s routing table - from that read to `CREATE_MEP_RUNS`, a question answered by a change, so it is declared too. After both, without the flag, two sentences answered by neither claimant before or after moved: *"how many ducts"* (claimed for `COUNT_ELEMENTS`) from `REMOVE_MEP_INSULATION`, a write, to `MEASURE_RUN_QUANTITIES`, a read; and *"why is this duct different from that one"* (claimed for `COMPARE_ELEMENTS`) from `SPLIT_MEP_RUN` to `CONVERT_TO_FLEX_DUCT`, write to write. With the flag, thirteen sentences moved from one read to another - recorded, not tuned.

**State.** **FIXED in the same change** - *"draw a new duct"* declared on `CREATE_MEP_RUNS` (FRG-MEP-052) and *"how long is this duct run"* on `MEASURE_RUN_QUANTITIES`, both PROVEN on main, each with a dated comment, their cards only, outside `impl/`, so no fingerprint moves. Measured again on a fresh store: both reach their cards by identity; the new cards' own sentences reach them. `tools/check-routing.py` on a scratch store: no question answered by a write, the 66 routing-table sentences not reached the same 66 as before. `tests/test_routing_phrases.py` fails exactly its known seven.

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

### Row 5b-1442

*Moved from the register on 2026-11-17.*

**Defect.** **THE FIRST DRAFT OF `RELOAD_IMAGES` TOOK TWO IMAGE QUESTIONS FROM `LIST_IMAGES`: *"WHAT IMAGES ARE LOADED IN THIS MODEL"* ON THE KEYWORD ROUTE, AND *"ARE THE IMAGES LOADED"* WITHOUT THE HOST'S QUESTION FLAG.** Seen 2026-11-02 while building it (FRG-DOC-068, DRAFT, a write - [Group MK](../needs-checking/group-mk.md)). `tools/check-routing.py` on a scratch `HERON_KNOWLEDGE` store listed `LIST_IMAGES`' declared *"what images are loaded in this model"* as *"FRG-DOC-062 is #2, FRG-DOC-068 answers instead"* - one half of the fusion only; `find` answers it by identity - through the word *loaded* in the write's purpose (*"every image loaded from a file"*). And through `heron_retrieve.find` on fresh scratch stores of the tree at `79a709d` and of this change (`model` backend; no Revit filter, 2020 and 2024; with and without the question flag), the probe *"are the images loaded"* - nobody's words - went from `LIST_IMAGES` to `RELOAD_IMAGES` without the flag.

**State.** **FIXED in the same change, at DRAFT** - the write's purpose says *"every image that came from a file"*, and `LIST_IMAGES`' purpose (DRAFT, this branch, outside `impl/`) says it reads *"whether it is loaded and its file found"*, which its report already does on 2021 on. Measured again: the contest is gone from `check-routing`, and *"are the images loaded"* reaches `LIST_IMAGES` with and without the flag on all three.

---

### Row 5b-1443

*Moved from the register on 2026-11-17.*

**Defect.** ***"WHICH IMAGES NEED RELOADING"* - A MADE-UP QUESTION - REACHES `RELOAD_IMAGES`, A WRITE, WITHOUT THE HOST'S QUESTION FLAG; AND THREE POINT CLOUD AND IFC PROBES REACH WRITES AS THEY DID BEFORE.** Seen 2026-11-02, the measurement of row 1442, over every declared utterance, identity and routing-table sentence of every live card and 22 probes (5,084 sentences). *"which images need reloading"* reached `LIST_IMAGES` before; with the flag it still does. No card can answer it truly - Manage Images cannot say a file changed since it was loaded. Unchanged by this change, before and after, without the flag: *"link this ifc file"* reaches `EXPORT_MODEL_TO_IFC` (PUBLISH - no card links an IFC, row 1440), *"is the point cloud loaded"* `ADD_REVISION_CLOUD` and *"what point clouds are in this model"* `DRAW_FAMILY_POINT_CURVES`; with the flag each reaches a read. The rest of the sweep: every sentence the two cards declare reaches them by identity; *"link this point cloud"* moved from `ADD_REVISION_CLOUD` to `LINK_FILES` and *"reload the images"* from `LOAD_FAMILY` to `RELOAD_IMAGES`; one write-to-write move on Revit 2020 only, *"subdivide the toposolid for this path"* (`CREATE_TOPOSURFACE` to `EDIT_SLAB_SHAPE`), whose claimant `EDIT_TOPOSOLID` is 2024 on; four read-to-read moves with the flag, not tuned. `tools/check-routing.py`: no question answered by a write, the same 66 claimed sentences unreached, two contests fewer (*"is there a CAD link in here"* no longer lost to `LINK_FILES`). `tests/test_routing_phrases.py` fails exactly its known seven.

**State.** **MOVED TO ISSUE [#930](https://github.com/Ajmalpshaik/Heron-AI/issues/930) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN** - nobody's words, so none is declared (D-86's rule against inventing utterances); the host's question flag keeps each from a write. Declared on `LIST_IMAGES`, or on a point cloud reader if one is built, the day a modeller says one.

---

### Row 5b-1444

*Moved from the register on 2026-11-17.*

**Defect.** **THE FIRST DRAFT OF `SET_FRAMING_JUSTIFICATION` TOOK `SET_MEP_JUSTIFICATION`'S *"SET THE JUSTIFICATION ON THESE DUCTS"* ON THE KEYWORD ROUTE, `REMOVE_PROJECT_PARAMETER`'S TOOK *"REMOVE THESE"*, *"REMOVE THESE ELEMENTS"* AND *"REMOVE THE VALUE FROM THESE"* THERE, AND THE QUESTION *"WHAT IS THE Z JUSTIFICATION OF THIS BEAM"* REACHED THE WRITE.** Seen 2026-11-02 building [Group ML](../needs-checking/group-ml.md). `tools/check-routing.py` on a scratch `HERON_KNOWLEDGE` store listed *"set the justification on these ducts"* as *"FRG-MEP-035 is #2, FRG-ELE-166 answers instead"* and the three *remove* sentences (declared by `DELETE_ELEMENTS` and `WRITE_ELEMENT_PARAMETERS`) with `FRG-PAR-036` first on words - one half of the fusion only; `find` answers all four by identity. The cause was the words *set the justification* and *justification* repeated in the write's identity and purpose, and *remove* and *values* in the removal's. And through `heron_retrieve.find` on the same store, the probe *"what is the z justification of this beam"* - nobody's words, but the request asked to *"set and read"* - reached `SET_FRAMING_JUSTIFICATION` without the host's question flag, words #1 and nearness #1; on the tree at `987161b` it reached `COPE_FRAMING`, a write too.

**State.** **FIXED in the same change, at DRAFT** - the write's identity names *"beam and brace justification - where structural framing sits on its line"* and its purpose says *justification* once; the removal's identity and purpose say *take ... out of the project* and *data*; and the read the request asked for is a card of its own, `REPORT_FRAMING_JUSTIFICATION` (FRG-ELE-167, READ). A row naming `SET_FRAMING_JUSTIFICATION` added to the routing tables of `SET_MEP_JUSTIFICATION` (DRAFT on main) and `SET_FRAMING_END_EXTENSION` (DRAFT, this branch), card text only. Measured again on fresh stores of the tree at `987161b` and of this change (`model` backend; no Revit filter, 2020 and 2024; with and without the question flag; 5,084 and 5,095 sentences): the four contests are gone from `check-routing`, and the question reaches `REPORT_FRAMING_JUSTIFICATION` with and without the flag on all three, as do *"what is the yz justification of these braces"* (`SET_STRUCTURAL_SETTINGS`, ADMIN, before) and *"what are the start and end level offsets of this beam"* (`ADD_BEAM_ANNOTATIONS` before). `check-routing` lists two new keyword-half contests, the write's own two sentences with the read first on words; `find` answers both by identity, and it is the safe way round - rewording the read's identity to clear them put the write first on the read's own *"read the justification of these beams"*, a write over a read, so the wording was put back. One older contest, *"make a view list schedule of every view placed on a sheet"*, is gone. `tests/test_routing_phrases.py` fails exactly its known seven.

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
