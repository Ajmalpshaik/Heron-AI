# Needs checking — Group DE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-12 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group DE - a family parameter replaced in place by the office's shared parameter (2026-10-12)

On Revit 2020 the owner asked for the size parameters of "TRG_Mechanical Equipment_Split Air
Conditioner Outdoor Unit_Wall Mounted" - Width, Depth, Height and Wall Clearance, family parameters
labelling dimensions - to become his shared parameters Unit_Width, Unit_Thickness, Unit_Height and
Clearance_Rear from his shared parameter file. Heron had no way to do what Family Types > Modify >
Shared parameter > Select does. The only route was `ADD_FAMILY_SHARED_PARAMETERS` beside each one and
`SET_FAMILY_FORMULA` "Width = Unit_Width", which leaves two parameters per size in the Properties
palette ([row 5b-436](../fragment-issues/section-5b-rows-401-425.md)).

**What was built, by widening - no new capability.** `ADD_FAMILY_SHARED_PARAMETERS` version 2
(FRG-PAR-031) takes an optional `replaceExisting`: true, and each of `parameterNames` is a pair
`Old=SharedName`, semicolons between. `FamilyManager.ReplaceParameter` turns each family parameter
into the office's shared one, keeping its scope; `parameterGroup` empty keeps its heading. The
precedent is `ADD_FAMILY_PARAMETERS`, which gained `switchExisting` and `renameExisting` the same way.
Version 1's proof is kept as a record and the card reads STALE until it is proved again.

**Refused by name before anything changes:** a shared name not in the file (with the close ones), a
kind that differs (a Length replaced by a Text), an old parameter already shared or built in or not in
the family, a shared name already in the family by name or GUID, and **a formula that names the shared
name - Revit refuses the replace while one does.** So the owner's family, which already carries the
workaround, is refused as it stands: its formulas are cleared and the extra shared parameters deleted
first (DE4).

**Read back, all or nothing:** shared with the file's GUID, the old scope and heading, no family
parameter left under the old name, every formula that read it now reading the shared name, every other
formula unchanged, labelled dimensions, associations and every type's value the same before and after.

**Measured without Revit, 2026-10-12.** `ReplaceParameter(FamilyParameter, ExternalDefinition, group,
bool)` takes a BuiltInParameterGroup 2020 to 2024 and a ForgeTypeId 2022 to 2027
(`tools/api-surface --members FamilyManager`), so it is picked by reflection as `AddParameter` is. It
compiles on all eight releases. "replace the family parameter Width with the shared parameter
Unit_Width" reaches it by its exact words on Revit 2020 and 2024, and "change these family parameters
to shared parameters" ranks it first; "replace width depth and height with my shared parameters unit
width unit thickness unit height" goes to `LABEL_FAMILY_DIMENSION` instead - recorded, not chased.
**Nothing below has run in Revit.** The arrangement is in
[`replace-family-shared-parameters-2026-10-12.yaml`](../../tools/jobs/replace-family-shared-parameters-2026-10-12.yaml),
read by `batch-prove --dry-run` as 1 WOULD RUN.

Never on the owner's own families, and never with his office file set in Manage > Shared Parameters:
a scratch file, and the one set before put back by hand afterwards - the setting is outside every Undo.
With `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and
`< /dev/null` on every line.

| # | Run | Look for |
|---|---|---|
| ~~**DE1**~~ | Arrange on **Revit 2020**: Manage > Shared Parameters, Create, a scratch file with a group "Heron Test" holding **Heron Unit Width** and **Heron Unit Depth** (Length) and **Heron Test Tag** (Text) - write down the file that was set before. File, New, Family, Metric Generic Model.rft, saved as **Heron Replace Shared Test**. `ADD_FAMILY_PARAMETERS` "Width, Half Width, Depth", Length, type, Dimensions; `SET_FAMILY_TYPE_VALUES` Width 600, Half Width 300, and a second type with Width 900; a dimension between the template's Left and Right planes labelled Width (`LABEL_FAMILY_DIMENSION`); `SET_FAMILY_FORMULA` "Half Width = Width / 2" | two types; Width labelling one dimension; Half Width driven by the formula - **Struck 2026-10-18:** arranged and read back 2026-10-12 - *Run 2026-10-12* below. |
| ~~**DE2**~~ | `python tools/batch-prove.py tools/jobs/replace-family-shared-parameters-2026-10-12.yaml`, FAMILY-TITLE filled | positive ("Width=Heron Unit Width; Depth=Heron Unit Depth", two in one call as the owner's four will be): `replaced` two entries - Width -> Heron Unit Width and Depth -> Heron Unit Depth, Length, type, under Dimensions, the file's GUIDs; formulasReadBack "Half Width = Heron Unit Width / 2"; keptLinks 1 labelled dimension and both types' values. negative ("Width=Heron Test Tag"): refused naming Length and Text, `replaced` empty. Look at Family Types afterwards - Width still there: rollback has failed on this PC before (BD1) - **Struck 2026-10-18:** `batch-prove` PASS 2026-10-12 - *Run 2026-10-12* below - and signed with DE9. |
| **DE3** | By hand through `revit_change`, DE2's positive kept. Then Family Types: change Heron Unit Width to 700 in a type | Family Types shows Heron Unit Width with the shared marker where Width was, values 600 and 900; the dimension's label reads Heron Unit Width; at 700 the planes move and Half Width reads 350 - the second route, and the flex |
| ~~**DE4**~~ | The owner's case: in a fresh copy of DE1's family, the workaround first - `ADD_FAMILY_SHARED_PARAMETERS` "Heron Unit Width" beside Width, `SET_FAMILY_FORMULA` "Width = Heron Unit Width" - then the replace "Width=Heron Unit Width" | **refused**, naming Width's formula and that Heron Unit Width is already in the family; nothing changed. Then clear the formula (`SET_FAMILY_FORMULA`), delete Heron Unit Width (`DELETE_FAMILY_PARAMETERS`) and replace again: as DE2's positive. This is the path his real family takes - **Struck 2026-10-18:** run 2026-10-12 as looked for, the replace kept - *Run 2026-10-12* below. |
| **DE5** | The other refusals, each its own call: "Width=Not In The File"; "Heron Unit Width=Heron Test Tag" after DE3 (already shared); "Height=Heron Unit Width" where Height is not in the family; "Width=Heron Unit Width; Half Width=Heron Unit Width"; a project in front | each refused with its reason, nothing changed; the project `notAFamily` true |
| **DE6** | **Measure Revit's own rule.** A family parameter named exactly as the shared one - "Heron Unit Width" made by `ADD_FAMILY_PARAMETERS` - then "Heron Unit Width=Heron Unit Width", once with no formula reading it and once with Half Width reading it; the second also by hand in Family Types > Modify > Shared parameter | without a formula: replaced. With one: Heron refuses, by the rule it was given; **record whether Revit's own dialog refuses too.** If Revit allows it, the rule is wider than Revit's and the owner decides whether to narrow it |
| **DE7** | `parameterGroup` "Identity Data" on DE2's positive; and with the family saved and a type catalogue "Heron Replace Shared Test.txt" beside it whose first line names Width | the shared one under Identity Data; a finding naming Width in the catalogue's first line |
| **DE8** | DE2 and DE4 on **Revit 2024** and **2027** - the ForgeTypeId side of every reflected call | the same answers. Compiled on all eight releases; what Revit DOES was measured on none |
| ~~**DE9**~~ | The signature, on the owner's word only, after DE2, DE3 and DE4 | `python brain/heron_validate.py accept add-family-shared-parameters --by "Ajmal PS"`. Drafted 2026-10-12 from `runs/add-family-shared-parameters.json` (DE2; gap: no second route run). **SIGNED 2026-10-12 by Ajmal PS on his word ("Yes, I sign it") - PROVEN** - **Struck 2026-10-18:** signed, as it says (fingerprint `d9d2e24338639f6f`). The card is DRAFT again at version 3, and keeps this proof as a record ([Group DF](group-df.md)). |

### Run 2026-10-12 - Revit 2020, scratch Family4 (DE1 to DE5)

The owner asked for the proof on **Family4**, a new unsaved Generic Model family, and nowhere else; the
split-unit family and a BIM 360 project were open beside it and nothing reached them - every call named
`--in Family4`. **One change to the plan above, said here:** Revit was set to the office's file
`D:\04_BIM\06_Shared_Parameters\Shared_Parameters.txt`, and the run used it **as it was set** - the
setting was not changed, and the tool only reads the file - with the office's own names: Unit_Width,
Unit_Thickness, Unit_Height (Length) and Item_Tag (Text). Values below are as Revit printed them in
Family4's feet-and-inches units.

| # | Result |
|---|---|
| DE1 | Arranged, every step kept and read back: Width, Half Width, Depth (Length, type, Dimensions); types Large (900, 450, 450) and Small (600, 300, 300, current); planes Box Left / Box Right at -300 / 300; Width labelling them, centred on Center (Left/Right); Half Width = Width / 2 |
| DE2 | `batch-prove` **PASS**, both legs rolled back. Positive "Width=Unit_Width; Depth=Unit_Thickness": `replaced` 2 - Width -> Unit_Width and Depth -> Unit_Thickness, Length, type, under Dimensions, the file's GUIDs; formulasReadBack "Half Width = Unit_Width / 2"; keptLinks "Unit_Width: 1 labelled dimension(s), values Large 2' - 11 111/256", Small 1' - 11 159/256"; Unit_Thickness: values Large 1' - 5 183/256", Small 0' - 11 13/16"". Negative "Width=Item_Tag": refused, "'Width' holds Length and the shared 'Item_Tag' holds Text", `replaced` 0. **Read back after:** Width and Depth family parameters again, Half Width = Width / 2 - the rollback held |
| DE3 | Kept. The same two replaced and read back as DE2; then Unit_Width = 700 in Small (`SET_FAMILY_TYPE_VALUES`): Box Left / Box Right moved to -350 / 350 mm and Half Width read 1' - 1 25/32" (350 mm) by its formula - the label and the formula follow the shared parameter. Family Types by eye is the owner's, still owed |
| DE4 | The workaround built on Height: Height (family) and Unit_Height (shared) side by side, Unit_Height 600 / 500, Height = Unit_Height. The replace "Height=Unit_Height" **refused** with both reasons - Unit_Height already in the family, and Height's formula names it - nothing changed. Then the formula cleared (`SET_FAMILY_FORMULA`, empty), Unit_Height deleted (`DELETE_FAMILY_PARAMETERS`) and the replace run kept: `replaced` 1, Height -> Unit_Height, values Large 1' - 11 159/256" (600 mm) and Small 1' - 7 175/256" (500 mm) kept. **This is the path the owner's split-unit family takes** |
| DE5 | One call, no transaction: "Unit_Width=Clearance_Rear; NoSuchParameter=Clearance_Side; Half Width=Supply_Air_Width" refused by name three times - Unit_Width already SHARED (its GUID), NoSuchParameter not in the family, Half Width Length against Supply_Air_Width Duct Size. The project leg was not run |

**Still owed:** DE6 (whether Revit's own dialog refuses a same-name replace while a formula reads it),
DE7 (heading and type catalogue), DE8 (Revit 2024 and 2027) and the Family Types look in DE3. DE9 is
done: signed by Ajmal PS on his word, 2026-10-12, and the card is PROVEN. Family4 keeps Unit_Width, Unit_Thickness, Unit_Height and Half Width
as the run left them; it is unsaved and is the owner's to close.
