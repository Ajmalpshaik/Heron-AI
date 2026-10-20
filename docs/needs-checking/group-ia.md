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
