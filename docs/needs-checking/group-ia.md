# Needs checking — Group IA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-20 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group IA - materials: every tab of the Material Browser, material adding, and giving elements a material (2026-10-20)

From [rows 5b-1263 and 5b-1264](../fragment-issues/section-5b-rows-401-425.md). On 2026-10-20, Revit 2020,
Project1, a duct heater family whose materials `CREATE_MATERIAL` made drew in colour in a Shaded view and
**flat dark grey in Realistic** (`EXPORT_VIEW_IMAGE`): a material Heron made had a shading colour and no
appearance asset, and no Heron tool could give it one. The owner asked the same day that "all the parts"
of a material be covered, and for "material adding". One of three sister tasks: the rendering tools and the
Visualiser method that uses both are the other two.

### What existed, and what each Material Browser field has now

Read off every card that touches a material, DRAFT included, on 2026-10-20. *Widened* means an existing
card took optional inputs only; *new* means nothing owned the field.

| Tab · field | Before | Gap | Now |
|---|---|---|---|
| Identity · Name | none - `RENAME_ELEMENTS` takes elements, and a material cannot be selected in a view | no route | `SET_MATERIAL_PROPERTIES` `Name=` (new) |
| Identity · Description, Class, Comments, Keywords, Manufacturer, Model, Cost, URL, Keynote, Mark | `LIST_MATERIALS` read the class only | not written; not read | `SET_MATERIAL_PROPERTIES` (new); `LIST_MATERIALS` v3 `withAllTabs` reads them (widened) |
| Graphics · Shading colour | `SET_MATERIAL_COLOUR` (PROVEN, one material), `CREATE_MATERIAL` (PROVEN, at creation) | many at once, by pattern | `SET_MATERIAL_PROPERTIES` `Shading Colour=` beside the rest; one material's colour stays `SET_MATERIAL_COLOUR` |
| Graphics · Transparency | `CREATE_MATERIAL` at creation | after creation | `SET_MATERIAL_PROPERTIES` `Transparency=` |
| Graphics · Use Render Appearance | none | no route | `SET_MATERIAL_PROPERTIES` `Use Render Appearance=` |
| Graphics · Surface and Cut, Foreground and Background, pattern and colour | `CREATE_FILL_PATTERN` makes a pattern; nothing put one on a material. `TRANSFER_MATERIALS_BETWEEN_DOCUMENTS` copies them with a material | not written; not read | `SET_MATERIAL_PROPERTIES` (`Surface Pattern` and `Cut Pattern`, the pre-2019 names, mean the foreground); read by `LIST_MATERIALS` v3 |
| Appearance · which asset | none; `TRANSFER_MATERIALS_BETWEEN_DOCUMENTS` reports one that did not arrive | no route - and the namespace was not in the fragment contract (row 5b-1264) | `SET_MATERIAL_APPEARANCE` - a library asset by name, a copy of another material's, or a Generic of its own (new); D-146 |
| Appearance · colour, image (path, size, rotation, offset), image fade, glossiness, highlights, reflectivity, transparency, refraction, translucency, cutout, self-illumination, bump, tint | `REPORT_MATERIAL_TEXTURES` reads image paths only | not written | `SET_MATERIAL_APPEARANCE` values; `matchShading` for a family `CREATE_MATERIAL` built |
| Appearance · who shares an asset | none | an edit rippling into another material | `SET_MATERIAL_APPEARANCE` gives the material its own copy and names the sharers; `LIST_MATERIALS` v3 counts them |
| Appearance · what Revit's library holds | none | a library name could only be guessed | `LIST_MATERIALS` v3 `withLibraryAppearances` (widened) |
| Physical and Thermal · which asset | `LIST_MATERIALS` v2 `withAssets` reads the values | not assigned | `SET_MATERIAL_PROPERTIES` `Physical Asset=`, `Thermal Asset=` - an asset the document holds; Revit's library is reachable for appearance assets only |
| Physical and Thermal · density, conductivity, specific heat and the rest | `LIST_MATERIALS` v2 `withAssets` reads them | not written | `SET_MATERIAL_PROPERTIES` `Physical: <name>=`, `Thermal: <name>=`, in the project's units; a shared asset is copied first |
| Material adding | `CREATE_MATERIAL` (PROVEN) - name, colour, transparency | no appearance; one at a time | `CREATE_STARTER_MATERIALS` - twelve TRG_ materials with their own appearances (new) |
| Giving elements a material | `WRITE_ELEMENT_PARAMETERS` v5 (a parameter named on the element), `SET_COMPOUND_LAYER_WIDTH` (a layer), `SET_FAMILY_FORM_MATERIAL` and `SET_FAMILY_TYPE_VALUES` (in a family), `REPLACE_MATERIAL` (a swap), `PAINT_FACES` (a face) | nothing found where an element's material lives | `SET_ELEMENT_MATERIAL` - the element's, its type's or its system type's material parameter, read back (new); layered types go on to `SET_COMPOUND_LAYER_WIDTH` |
| The read report | `LIST_MATERIALS` v2 - name, class, id, physical and thermal values | no other tab | `LIST_MATERIALS` v3 `withAllTabs` - one line per material, every tab; `namePattern` |

**No PROVEN card was changed.** `LIST_MATERIALS` was DRAFT at version 2 (Group CX, never run), so its
widening stales nothing. `CREATE_MATERIAL`, `SET_MATERIAL_COLOUR`, `REPLACE_MATERIAL`,
`SET_FAMILY_FORM_MATERIAL` and `SET_COMPOUND_LAYER_WIDTH` stay as they are.

| Capability | Folder | What it does |
|---|---|---|
| `SET_MATERIAL_PROPERTIES` | [`set-material-properties`](../../brain/fragments/set-material-properties/fragment.yaml) | Identity, Graphics, Physical and Thermal fields as `Field=Value`, on one material or a name pattern |
| `SET_MATERIAL_APPEARANCE` | [`set-material-appearance`](../../brain/fragments/set-material-appearance/fragment.yaml) | the appearance asset - library, copied, or Generic of its own - and the Generic values; `matchShading` |
| `CREATE_STARTER_MATERIALS` | [`create-starter-materials`](../../brain/fragments/create-starter-materials/fragment.yaml) | twelve TRG_ materials, each with a shading colour and its own appearance |
| `SET_ELEMENT_MATERIAL` | [`set-element-material`](../../brain/fragments/set-element-material/fragment.yaml) | elements' material where it lives - element, type or system type - read back |
| `LIST_MATERIALS` v3 | [`list-materials`](../../brain/fragments/list-materials/fragment.yaml) | `withAllTabs`, `withLibraryAppearances`, `namePattern` |

The method step: [`family-creation`](../../brain/skills/family-creation.yaml) step 12 - a family that will
be rendered gives each material an appearance (`SET_MATERIAL_APPEARANCE`, `matchShading`).

**What is done and what is not.** Every card validates (`python brain/heron_fragment.py`) and compiles for
2020 to 2027 (`tools/check-fragments-compile.py`). **Nothing here has run in Revit.** Every card is DRAFT
until its rows below pass and the owner signs. Three things the reference assemblies could not answer, and
the rows that do: whether Revit 2020 offers a material's **Keywords** by that name (no Revit parameter of
its own on any release - IA2); whether **glossiness and reflectivity** are stored 0 to 1 as the browser's
0 to 100 suggests (Revit documents that range for fade, transparency and translucency only - IA5); and
which **names** the PC's appearance library holds (IA1).

**The model.** A **scratch** project from Revit's metric **Systems** template, saved to
`%TEMP%\heron-work\materials-ia`, never a working model. In it, made by hand: two `M_Rectangular Column`
instances of one type (load the family if the template has not got it); a 3 m run of three ducts in a
Supply Air system; two Basic Wall instances of a layered type; one door; and with `CREATE_MATERIAL`
(PROVEN, kept) three probes - `IA Probe Steel` (128,128,128), `IA Probe Copper` (184,115,51) and
`IA Probe Wood` (171,128,82). Then, in the Material Browser, give `IA Probe Steel` and `IA Probe Copper`
**one shared** appearance asset and **one shared** thermal asset (Duplicate one material's asset onto the
other) - the negatives need sharing. A copy of the owner's duct heater family, `TRG_Duct Accessory_Electric
Duct Heater` (row 5b-1261), saved to the same folder. Run every row on **Revit 2020**, then IA7 on **Revit
2026 or 2027**. Every value below is the proof's own, chosen for this model - never a design value Heron
offers (D-33).

| # | Run | Look for |
|---|---|---|
| **IA0** | **The add-in first** (D-146). On the owner's PC: `python tools/check-compile.py`, then redeploy the add-in for Revit 2020 as `tools/deploy-addin.ps1` does, and restart Revit. Then `LIST_MATERIALS` (version 3) with every switch left out, and a PROVEN read - `READ_ELEMENT_MATERIAL` on one column | **Positive:** both answer as before - the add-in built with the longer namespace list compiles and runs a fragment unchanged. **Negative:** before the redeploy, `SET_MATERIAL_APPEARANCE` fails to compile at the PC naming the edit scope - the reason the redeploy is row 0 |
| **IA1** | `LIST_MATERIALS` `withAllTabs` true, `namePattern` `IA Probe*`; then `withLibraryAppearances` true; then `namePattern` `ZZZ*` | **Positive:** three lines, each with its identity, its shading colour and patterns, `IA Probe Steel` and `IA Probe Copper` "shared with 1 other material(s)"; `libraryAppearances` lists the asset browser's library by the names it shows - compare five by eye, and **write down three to use in IA6**. **Negative:** `ZZZ*` gives `materialCount` 0 |
| **IA2** | `SET_MATERIAL_PROPERTIES` on `IA Probe Steel`: `Manufacturer=IA Maker; Model=M-1; Class=Metal; Keynote=05 12 00; Mark=IA-1; Keywords=ia probe; Surface Pattern=<Solid fill>; Surface Pattern Colour=128,128,128; Cut Pattern=Diagonal crosshatch; Use Render Appearance=no`; then the same again; then `materialsNamed` `IA Probe Stel`; then `IA Probe*` with `Name=IA One` | **Positive:** every field in the Identity and Graphics tabs as given, each in `materialReport` before and after. **Keywords:** either written and read back, or the call refused saying Revit offers no Keywords - record which, and if refused run again without it. **Negatives:** the repeat `fieldsChanged` 0; the misspelling refused naming `IA Probe Steel` among the closest; two materials renamed refused - nothing changed |
| **IA3** | `SET_MATERIAL_PROPERTIES` on `IA Probe Steel`: `Thermal: Thermal Conductivity=50` (the units the project shows); then `Physical Asset=` a physical asset the template holds (read its name in IA1); then `Thermal Asset=No Such Asset` | **Positive:** `sharedAssets` names `IA Probe Copper`; `IA Probe Steel` has a new thermal asset named after it reading 50, and `IA Probe Copper`'s still reads what it did; the physical asset assigned. **Negative:** the unknown asset refused naming the closest; nothing changed |
| **IA4** | **The measured case.** In the heater family copy, `SET_MATERIAL_APPEARANCE` `materialsNamed` `TRG_*`, `matchShading` true; `EXPORT_VIEW_IMAGE` of its 3D view in Realistic before and after | **Positive:** every TRG material has an appearance named after it whose colour is its shading colour, and the Realistic image is in those colours, not flat dark grey. **Negative:** run it again - `assetsMade` empty (each already has its own), the colours unchanged |
| **IA5** | `SET_MATERIAL_APPEARANCE` on `IA Probe Wood`, `appearance` generic, values `Image=<a .jpg that exists, full path>; Image Size=1200 x 600; Image Rotation=90; Image Offset=100,50; Glossiness=40; Highlights=non-metallic; Reflectivity Direct=10; Bump Amount=30` | **Positive:** the Appearance tab shows that image at 1200 x 600 mm, 90 degrees, offset 100 / 50 in the texture editor; **Glossiness 40 and Reflectivity Direct 10 in the browser** - if it shows 0.4 or 4000, the scale is wrong and the card says so. **Negative:** `Image=C:\no\such\file.jpg` refused - nothing changed |
| **IA6** | `SET_MATERIAL_APPEARANCE` `IA Probe Steel`, `appearance` one of IA1's library names; then `appearance` `from IA Probe Wood` on `IA Probe Copper`; then `IA Probe Steel` with values `Colour=10,20,30` while it still shares with `IA Probe Copper` (on a fresh copy of the model); then `appearance` `No Such Look`; then a library Metal asset with `Glossiness=50` | **Positive:** Steel shows the library asset under its own name; Copper a copy of Wood's under its own name. **Negatives:** the shared edit gives Steel its own copy coloured 10,20,30 and names Copper in `sharedBefore`, Copper's colour unchanged; the unknown name refused naming the closest from the document and the library; the Metal asset with a value refused as not Generic |
| **IA7** | **The second route.** IA5 again on **Revit 2026 or 2027** - an image's size and offset convert millimetres by `GetUnitTypeId()` there and by `DisplayUnitType` on 2020 | The same 1200 x 600 and 100 / 50 in the texture editor. A difference is a finding against the `#if REVIT2020` branch, never a pass |
| **IA8** | `CREATE_STARTER_MATERIALS` with everything left out; a Realistic `EXPORT_VIEW_IMAGE` of twelve boxes painted one each; then the same call again; then `starterPick` `granite`; then `starterPick` copper, `libraryLooks` `Pipe Copper=No Such Look`, `namePrefix` `IA_` | **Positive:** twelve TRG_ materials, each with its class, shading colour and an appearance named after it; the glass see-through and the metals glossy in the image. **Negatives:** the repeat makes nothing and lists all twelve in `alreadyThere`; granite refused naming the twelve; the unknown look refused - no IA_ material made |
| **IA9** | `SET_ELEMENT_MATERIAL` with the two columns selected, `material` `TRG_Concrete Smooth`; again; the three ducts, `TRG_Duct Galvanised`; the two walls; the door with `materialParameter` left out, then with one of its material parameters named | **Positive:** the column type's Material written once, the report counting every placed column of the type; the Supply Air system type's Material written and said to reach every element of it - the ducts draw galvanised in Realistic. **Negatives:** the repeat `alreadyAsAsked` 1; the walls skipped naming `SET_COMPOUND_LAYER_WIDTH`; the door skipped naming its material parameters until one is named |
| **IA10** | `SET_ELEMENT_MATERIAL` with the heater family copy in front | Refused naming `SET_FAMILY_FORM_MATERIAL` and `SET_FAMILY_TYPE_VALUES` - nothing changed |
| **IA11** | **The method.** `heron_method` for *"build a duct heater family that will be rendered"* | Step 12 names `SET_MATERIAL_APPEARANCE` with `matchShading` after `CREATE_MATERIAL` |

**Then sign** each card on its own rows: `list-materials` IA1, `set-material-properties` IA2 + IA3,
`set-material-appearance` IA4 + IA5 + IA6 + IA7, `create-starter-materials` IA8, `set-element-material`
IA9 + IA10. The proof plan for the batch runner is
[`tools/jobs/materials-ia-2026-10-20.yaml`](../../tools/jobs/materials-ia-2026-10-20.yaml).

### Run 2026-10-20 - Revit 2020, the owner's laptop

**The model** was built as above on Revit 2020 (session 29012), `Materials IA Test.rvt` in
`%TEMP%\heron-work\materials-ia`. The Systems template's plans are `1 - Mech`, `2 - Mech` and the like - no
`Level 1` - so the job file's `inViewOnly` now reads `FloorPlan: 1 - Mech`. Rolled-back runs went from the
command line (`HERON_CLIENT_ID=ajmal-pc`). The KEPT runs went through `try-once` on the saved model, and the
owner pressed Keep in Revit each time (D-119, D-120). Their negatives were run with `validate --kept`.
**Nothing is signed.** The drafts wait in `brain/proof-drafts/` for the owner.

**Every try-once needed a fresh Ctrl+S.** A rolled-back run still leaves Revit's model marked as changed
(`IsModified`, as row 5b-350's detector reads it). D-121 then refuses an unproven tool's one run with
*"has changes that are not saved"*. So the owner saved before each of the five kept runs, and the order
was: every rolled-back run first, then the save, then the kept run.

| # | Result | What was seen |
|---|---|---|
| IA0 | **PASS** | Before the redeploy, `SET_MATERIAL_APPEARANCE` came back `compile_failed`. After the add-in was rebuilt and redeployed for Revit 2020, `LIST_MATERIALS` and `READ_ELEMENT_MATERIAL` ran as before |
| IA1 | **FAIL on version 3, fixed in version 4 ([row 5b-1272](../fragment-issues/section-5b-rows-401-425.md)), PASS on version 4** | Version 3 listed 3,088 library assets, every one by Revit's code - `ACADGen-001`, `Prism-113`. On Revit 2020 a library asset's Title is empty and its UIName IS the code. Version 4 lists 1,926 and leaves out 1,162 that Revit marks hidden. 674 are shown by the browser's name with the code - `Copper - polished [Prism-050]` - and 1,252 by the code alone. The three probes come back as three lines, `IA Probe Steel` and `IA Probe Copper` each *"shared with 1 other material(s)"*. `ZZZ*` gives `materialCount` 0. **Three library names for IA6:** `Copper - polished` (Prism-050), `Glass - clear` (Prism-152), `Concrete - Cast in Place` (Prism-1100). **By eye:** EYE1 below |
| IA2 | **PASS** | **Keywords: Revit 2020 has none a tool can reach.** The call was refused, *"Revit offers no 'Keywords' on the material 'IA Probe Steel' - not as a parameter of its own and not by that name"*, nothing kept, and it was run again without it. **Kept:** 8 fields changed and Use Render Appearance was already *no*: Manufacturer `IA Maker`, Model `M-1`, Class `Metal`, Keynote `05 12 00`, Mark `IA-1`, Surface Foreground `<Solid fill>` 128,128,128, Cut Foreground `Diagonal crosshatch` (id 6473). **Negatives, rolled back:** the repeat gave `fieldsChanged` 0 and `alreadyAsAsked` 9. `IA Probe Stel` was refused naming `IA Probe Steel` among the closest. `IA Probe*` with `Name=IA One` was refused, *"Name renames ONE material, and 3 were named"*. **By eye:** EYE2 |
| IA3 | **PASS** (rolled back) | `Thermal: Thermal Conductivity=50` read 45 -> 50 W/(m·K) on a copy named `IA Probe Steel`. `sharedAssets` says *"'Steel(2)' was shared with 'IA Probe Copper' - given its own copy ... and 'Steel(2)' was not touched"*. `Physical Asset=Iron - Ductile Cast A536 65-45-12` changed none -> that asset. `Thermal Asset=No Such Asset` was refused, nothing changed |
| IA4 | **NOT RUN** - put off by the owner, 2026-10-20 (*"leave it about the heater"*) | The heater copy is in the folder for it. `set-material-appearance`'s one kept run went to IA5 |
| IA5 | **PASS by Heron's read-back; the browser by eye is the owner's** | **Kept** on `IA Probe Wood`: a Generic asset of its own named after it, with the image `...\Textures\1\Mats\ceramic_wood_color.jpg`, Image Size 1200 x 600, Rotation 90, Offset 100,50, Glossiness 40 (stored 0.4), non-metallic, Reflectivity Direct 10, Bump Amount 30. Every value was read back off the asset, and a mismatch would have refused the call. **Negative:** `Image=C:\no\such\file.jpg` was refused, *"is not there - a missing image renders as plain colour"*, nothing changed. **By eye:** EYE5 |
| IA6 | **PASS** (rolled back, version 2) | **By the browser's name**, `Copper - Polished`, and **by its code**, `Prism-050`: Steel's asset `Steel - Satin` -> `IA Probe Steel` (PrismMetal). **From another material:** `from IA Probe Steel` on Wood - Wood had no asset to copy from until IA5 - gave a copy named `IA Probe Wood`. **The shared edit** ran on `Air Infiltration Barrier`, whose `Generic(1)` the template shares with `Vapour Retarder`. Steel and Copper share a Prism Metal asset, which takes no values, so they could not stand in. Colour 80,80,80 -> 10,20,30 on its own copy, `sharedBefore` names `Vapour Retarder`, and `Generic(1)` was not touched. **Negatives:** `No Such Look` was refused naming the library's closest. `Copper - Polished` with `Glossiness=50` was refused, *"a PrismMetalSchema appearance, not a Generic one"* |
| IA7 | **NOT RUN** - needs Revit 2026 or 2027 | Nothing reads the image size there yet. That release still has to be checked by eye |
| IA8 | **PASS, except the image** | **Kept:** twelve `TRG_` materials, each with its class, shading colour and a Generic appearance of its own named after it. Glass is 80% transparent shaded and 90 in its appearance, with refraction 1.52. **Negatives, rolled back:** the repeat made 0 and lists all 12 in `alreadyThere`. `granite` was refused naming the twelve. `starterPick` copper with `libraryLooks` `Pipe Copper=No Such Look` and `IA_` was refused, and no `IA_` material was made. **Version 2's library look:** `Pipe Copper=Copper - Polished` gave `IA_Pipe Copper` the appearance `Copper - polished` (rolled back). **NOT RUN:** the Realistic image of twelve painted boxes - the model has no twelve boxes |
| IA9 | **PASS** | **Kept,** with the two columns selected: the type `M_Rectangular Column: 457 x 475mm`'s Material <By Category> -> `TRG_Concrete Smooth`, *"a type change: all 2 placed element(s) of this type changed"*. **Rolled back:** the repeat gave `alreadyAsAsked` 1 and `writtenIds` 0. On the three ducts, `Duct System: Supply Air`'s Material -> `TRG_Duct Galvanised`, *"every element of this system type draws with it now"*. Both walls were skipped as *"built of layers - its material is a layer's, set with SET_COMPOUND_LAYER_WIDTH"*. The door was skipped as *"holds 2 material parameters - 'Trim Material' ..."*. With `materialParameter` `Trim Material`, its type's Trim Material `Paint - Sienna` -> `TRG_Wood Oak`. **Not seen:** the ducts galvanised in Realistic - that run was rolled back |
| IA10 | **NOT RUN** - no family was open. Put off with the heater (IA4) | |
| IA11 | **PASS** | `heron_method family-creation`, step 12: *"A FAMILY THAT WILL BE RENDERED ... SET_MATERIAL_APPEARANCE with materialsNamed the names CREATE_MATERIAL made ... and matchShading true"* |

**By eye, the owner's** - none of these can be read by a tool:

| # | Look at | Answer |
|---|---|---|
| **EYE1** | Asset Browser: do `Copper - Polished`, `Glass - Clear`, `Concrete - Cast in Place`, `Aluminum - Brushed` and `Wood - Cherry` appear by those names? (IA1) | **Half.** Seen by the owner 2026-10-20: `Copper - Polished` is there by that name (Metal: Copper). Prism-152, listed as `Glass - clear`, shows as **`Glass (Clear)`** (Transparent, Glass: Smooth). Revit's description is therefore not always the browser's name: a look typed as the browser shows it may be refused, while its code always matches. Row 5b-1272 records it. The other three names were not looked at |
| **EYE2** | Material Browser, `IA Probe Steel`, Identity tab: is there a **Keywords** box at all on Revit 2020? (IA2) | **Yes.** Seen by the owner 2026-10-20: Descriptive Information has Description, Class `Metal`, Comments and **Keywords** (empty). Product Information has Manufacturer `IA Maker` and Model `M-1`. Keynote is `05 12 00` and Mark `IA-1` - IA2's kept values, read by eye. Keywords is a box the browser shows and no parameter reaches. The tool's refusal says to set it there, which is right |
| **EYE5** | `IA Probe Wood`, Appearance tab: Glossiness **40**? Reflectivity Direct **10**? The image: 1200 x 600 mm, rotation 90, offset 100 / 50 in the texture editor? (IA5) | **Glossiness 40 and Reflectivity Direct 10: YES** - seen by the owner 2026-10-20, Generic, with image `ceramic_wood_color.jpg`, Image Fade 100, Highlights Non-Metallic and Oblique 0. **The 0-100 scale the card stores as 0-1 is the browser's.** The texture editor - size, rotation, offset - was not opened: **still owed** |

**SIGNED 2026-10-20 by Ajmal PS - all five PROVEN.** He signed after the by-eye answers above, knowing that
IA4, IA7, IA10 and EYE5's texture editor were not run. The proofs, each with a kept run as its positive wherever
the tool changes the model:

| Tool | Positive | Negative | Draft fingerprint |
|---|---|---|---|
| `list-materials` v4 | IA1, three probes with every tab and the library | `ZZZ*`, `materialCount` 0 | `ea0007418a1a97d7` |
| `set-material-properties` | IA2, kept | `IA Probe Stel`, refused | `f5d8cadb4637b26c` |
| `set-material-appearance` v2 | IA5, kept | a missing image, refused | `affdd751d0750c40` |
| `create-starter-materials` v2 | IA8, kept | `granite`, refused | `37f530e6fab754bf` |
| `set-element-material` | IA9, kept | the layered walls, skipped | `330f01f3a3fbd93d` |

Each draft's gap is *"no second route was run"* - for `set-material-appearance` that route is IA7.
`set-material-appearance`'s sign plan also names IA4 and IA7. Signing it with those two not run is the
owner's decision, and the note goes with the signature. A repeat whose report reads back *"already ..."* is
not empty by the draft's measure: the count is 0, but the sentence is not. So each recorded negative is a
refusal, and each repeat is recorded above.

**Overlap with older tools, checked 2026-10-20 at the owner's question.** Nineteen cards touch a material. None does what any of the five does, but two share a field. `SET_MATERIAL_PROPERTIES` sets `Shading Colour=` on many materials, and the PROVEN `SET_MATERIAL_COLOUR` sets one material's - the card sends one material's colour to the older tool. `SET_ELEMENT_MATERIAL` writes a material parameter, and `WRITE_ELEMENT_PARAMETERS` version 5 (DRAFT, row 5b-1259) writes one too when it is named on the element given - a type's Material when the type is given. What the newer tool adds is finding the parameter: on the element, its type or its duct system type, the layered walls and a door's two parameters named rather than guessed. Whether the two should become one is the owner's question, not decided here.

**Version 3, 2026-10-20 (row 5b-1280).** EYE1's `Glass (Clear)` is now taken: `SET_MATERIAL_APPEARANCE` and `CREATE_STARTER_MATERIALS` match a library name with capitals, spaces and punctuation ignored when no name matches exactly. Both were proved again, rolled back - `Glass (Clear)` on `IA Probe Steel`, and `Glass Clear=Glass (Clear)` for a starter, `No Such Look` and `granite` refused. Both were signed again by Ajmal PS and are PROVEN at version 3. The kept runs above were version 2's.
