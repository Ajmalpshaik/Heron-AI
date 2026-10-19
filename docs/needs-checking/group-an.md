# Needs checking — Group AN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group AN - family creation, from an empty template to a flexed family (2026-09-24)

**The owner's PC, Revit 2024 first, then 2020.** Nine fragments and one skill, written on 2026-09-24
when the owner asked for his earlier brain's family-creation skill to become part of Heron - package
C11 of [the earlier-brain plan](../work-notes/plans/earlier-brain/02-work-packages.md), taken ahead of
C7 and C9 on his word:

- [`set-family-category`](../../brain/fragments/set-family-category/fragment.yaml) - `SET_FAMILY_CATEGORY`, FRG-DOC-035
- [`add-family-parameters`](../../brain/fragments/add-family-parameters/fragment.yaml) - `ADD_FAMILY_PARAMETERS`, FRG-PAR-022
- [`set-family-type-values`](../../brain/fragments/set-family-type-values/fragment.yaml) - `SET_FAMILY_TYPE_VALUES`, FRG-PAR-023
- [`set-family-formula`](../../brain/fragments/set-family-formula/fragment.yaml) - `SET_FAMILY_FORMULA`, FRG-PAR-024
- [`create-reference-planes`](../../brain/fragments/create-reference-planes/fragment.yaml) - `CREATE_REFERENCE_PLANES`, FRG-GEO-034
- [`label-family-dimension`](../../brain/fragments/label-family-dimension/fragment.yaml) - `LABEL_FAMILY_DIMENSION`, FRG-GEO-035
- [`extrude-between-planes`](../../brain/fragments/extrude-between-planes/fragment.yaml) - `EXTRUDE_BETWEEN_PLANES`, FRG-GEO-036
- [`add-family-connector`](../../brain/fragments/add-family-connector/fragment.yaml) - `ADD_FAMILY_CONNECTOR`, FRG-MEP-053
- [`flex-family`](../../brain/fragments/flex-family/fragment.yaml) - `FLEX_FAMILY`, FRG-QA-033
- [`family-creation`](../../brain/skills/family-creation.yaml) - the skill that lists them in order

**What is already known, and it is NOT a proof.** All nine compile on all eight releases
(`tools/check-fragments-compile.py`, 2026-09-24) and route their own sentences. **None has met a
model.** Every one is `DRAFT`, and each `tests/cases.yaml` names the negative case its proof owes.

> **2026-10-18 - where these tools stand now, read off disk.** *None has met a model* and *every one is
> `DRAFT`* above describe the day this group was written. Since 2026-10-03 every tool listed above has been
> run in Revit 2024 and signed by Ajmal PS - the first with the family-editor proofs of PR #395, the rest
> later; the `proof:` block in each card says on what and when. `add-family-connector` and `flex-family` are DRAFT again: the code moved under the
> signature, and `python tools/check-signatures.py` lists both STALE. Today's status of any of them:
> `grep '^heron-status:' brain/fragments/<name>/fragment.yaml`. **The rows below are not closed by those
> proofs:** they build this group's own family in order, and none of them is recorded as run here, so every row
> stays open.

**The arrangement is one family, built by the rows in order**, so each row is the setup for the next:
File, New, Family, the metric Generic Model template, left open in front - a new family nobody needs,
saved nowhere until AN9 has passed.

| # | Check | Expected |
|---|---|---|
| **AN1** | `set-family-category`, categoryName `Air Terminals` | `category` Air Terminals, read in Family Category and Parameters; `parametersGained` above zero, matching the flow fields in Family Types. **Negative:** `Air Terminal` refused with `Air Terminals` offered |
| **AN2** | `add-family-parameters`: `Width, Depth, Height, Neck Width, Neck Depth, Neck Height`, Length, type, group empty | Six type lengths under Dimensions in Family Types, all 0. **Negative:** the same again adds nothing; kind `Lenght` refused with nothing added |
| **AN3** | `set-family-type-values`: type `600 x 600`, `Width=600; Depth=600; Height=250; Neck Width=400; Neck Depth=300; Neck Height=100` - the neck deliberately NOT square | `typeCreated` true and the six values in Family Types. **Negative:** `Width=600mm` refused, nothing written |
| **AN4** | `create-reference-planes`, three calls: Box Left/Right at -300/300 left-right; Box Front/Back -300/300 front-back; Box Top 250 and Neck Top 350 height - then Neck Left/Right at -200/200 and Neck Front/Back at -150/150 | Every plane MEASURED where it was asked, in plan or elevation. **Negative:** Box Left again at -400 refused, and Box Left not moved |
| **AN5** | `label-family-dimension`: Width across Box Left/Right with `Center (Left/Right)`; Depth with `Center (Front/Back)`; the neck's two the same way; Height from `Ref. Level` to Box Top; Neck Height from Box Top to Neck Top | Each labelled dimension reads its value; typing Width 800 in Family Types moves BOTH box planes 100 mm, centred. **Negative, and the one that matters:** a label asked while its parameter still reads 0 is refused with both numbers and the planes NOT collapsed |
| **AN6** | `extrude-between-planes`: the body (Box sides, `Ref. Level`, Box Top), then the neck (Neck sides, base Box Top, top Neck Top) | 600 x 600 x 250 and 400 x 300 x 100 read back; five closed padlocks on each box, LOOKED AT |
| **AN7** | `add-family-connector`: facePlane Neck Top, system `Supply Air`, sizes `Neck Width, Neck Depth` | A rectangular Supply Air connector whose width and height lie along the neck's 400 and 300 sides, pointing up, selected and read in Properties; the report says whether the two were turned to fit. **Negative:** sizes empty refused; facePlane Box Top refused as two faces; sizes holding 600 and 600 refused as not fitting the face. **Also record:** whether Revit accepts a LENGTH parameter for the connector's width at all - if its associate list offers only Duct Size, the Length-only rule here is wrong |
| **AN8** | `set-family-formula`: Neck Width = `Width - 200`, then the same with `"Width"` in quotes, then cleared | 400 mm and greyed in Family Types; the quoted one refused before Revit is asked; cleared leaves 400 editable |
| **AN9** | `flex-family`, two trials - `Width=900; Depth=500`, then `Width=400; Depth=700; Neck Width=250` - written with a pipe between them | `allHeld` true, `restored` true, the body 900 x 500 then 400 x 700, centred, base at 0, the neck and its connector following Neck Width. A third trial changing ONLY `Neck Width=250` must also hold - the body does not move, the neck does. **Negative:** the same flex on a copy whose body was drawn by hand WITHOUT locks - NOT HELD, naming planes that moved under a solid that did not |
| **AN10** | AN1 to AN9 on **Revit 2020** | The same answers. AddParameter and a parameter's kind take the OLDER calls there (by reflection), so 2020 is a different path, not a repeat |
| **AN11** | The `family-creation` skill asked for in one sentence in a chat - "make an air terminal family 600 by 600 with a 300 neck and a supply air connector" - on a fresh template | The host reads the skill's order and runs the nine in it, asking for any size not given. Record where it strays from the order - that is the case for D2's `steps:` list |

**Not in this group, because not built:** void cuts, round shapes, materials, subcategories and Is
Reference - the skill says so in its own words.

### 2026-10-11 - `add-family-connector` version 3: a connector on a face of joined or cut forms (row 5b-410)

**DRAFT, compiled for Revit 2020 to 2027 on 2026-10-11, NOT RUN - no Revit was connected.** On 2026-10-10
(Revit 2020, session 36948) the owner's gate valve, Family1, had its two flanges cut by a bore void and two
bolt-hole voids through `COMBINE_FAMILY_FORMS`, all in one combination, and `ADD_FAMILY_CONNECTOR` on the
plane `Flange Face Left` answered *"No solid face lies on "Flange Face Left""* with the flange's face exactly
on it ([row 5b-410](../fragment-issues/section-5b-rows-401-425.md)). Read in the source:
the tool looked for the face in each form's own geometry, and once a form is joined or cut Revit keeps its
faces in the combination's - `REPORT_FAMILY_FORMS` reads such a form at 0 L.

**Version 3** also reads every combination's geometry, as `COMBINE_FAMILY_FORMS` reads one, and uses its
faces when no form's own face lies on the plane - exactly one, or refused with the count. A form's own face
still wins, so the way round the row used (a thin ring joined to nothing on the flange face) keeps working,
and when it wins with joined faces on the same plane the result says how many. **What Revit's reference to a
joined face names, and whether Revit takes a connector on it at all, is not known: it is reported as Revit
answers.** If Revit refuses, the refusal carries its own words and the way round; nothing is faked.

**THE ARRANGEMENT - made KEPT, once, in a new scratch family**, never Family1 and never one of the owner's
saved families. `CREATE_FAMILY_DOCUMENT` from *Metric Generic Model* gives a family; with it in front, and
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` and `< /dev/null` on every line:

```
python mcp/client/heron_bridge_client.py fragment create-reference-planes --write --apply --set "planes=Port Left=-150; Port Right=150" --set axis=left-right
python mcp/client/heron_bridge_client.py fragment create-reference-planes --write --apply --set "planes=Box Top=100" --set axis=height
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect -150,-50 -50,50" --set startMm=0 --set endMm=100 --set solid=true
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Ref. Level" --set "profile=rect 50,-50 150,50" --set startMm=0 --set endMm=100 --set solid=true
python mcp/client/heron_bridge_client.py fragment create-family-extrusion --write --apply --set "workPlane=Center (Left/Right)" --set "profile=circle 0,50 20" --set startMm=-160 --set endMm=160 --set solid=false
python mcp/client/heron_bridge_client.py fragment combine-family-forms --write --apply --set "forms=<box A id>, <box B id>, <void id>"
python mcp/client/heron_bridge_client.py fragment add-family-parameters --write --apply --set "parameterNames=Port Dia" --set kind=Length --set instance=false --set parameterGroup=Dimensions
python mcp/client/heron_bridge_client.py fragment set-family-type-values --write --apply --set typeName=T1 --set "values=Port Dia=40"
python mcp/client/heron_bridge_client.py fragment report-family-forms
```

The three ids are the `formId`s the extrusions gave back. Before the rows: `REPORT_FAMILY_FORMS` reads both
boxes at 0 L and "in" the one combination, the void too. Every run below is rolled back (`validate --write`)
unless it says KEPT.

| # | Run | Look for |
|---|---|---|
| **AN12** | `validate --in "<family>" --negative-in "<family>" --write --set "facePlane=Port Left" --set "system=Domestic Cold Water" --set "sizeParameters=Port Dia" --negative-set "facePlane=Box Top" --negative-set "system=Domestic Cold Water" --negative-set "sizeParameters=Port Dia" add-family-connector` - or the job file [`add-family-connector-joined-face-2026-10-11.yaml`](../../tools/jobs/add-family-connector-joined-face-2026-10-11.yaml) | a pipe connector, round 40 mm, at X -150, Y 0, Z 50, pointing left (-X); `sizedBy` "connector diameter <- Port Dia"; `connector` naming the combination and **what Revit's reference to the face names - write that sentence down whole**: it is the measurement the change was asked for. **If Revit refuses instead**, record its words; the card's purpose then says so in place of "found too", and row 5b-410 stays open with the way round as the answer |
| **AN13** | the negative half of AN12: `Box Top`, where both boxes' tops lie | refused, *"2 faces of joined or cut forms lie on "Box Top" (facing up, up)"*, `sizedBy` EMPTY |
| **AN14** | the way round kept: a ring joined to nothing on box B's right face, `create-family-extrusion` on `Center (Left/Right)`, `circle 0,50 35 \| circle 0,50 20`, 149 to 150, solid, made KEPT after AN12 and AN13 (it touches neither `Port Left` nor `Box Top`), then `facePlane=Port Right` | the connector on the RING's face - a form's own face first - at X 150, pointing right (+X), and `connector` ending *"1 face(s) of joined or cut forms lie on the same plane too"* |
| **AN15** | a duct connector on the joined face: `system=Supply Air`, `sizeParameters=Port W, Port H` on `Port Left` - two type lengths added KEPT, holding 100 and 100 | Revit's answer recorded. A rectangular port is measured against the face's outer edges - box A's left face is 100 x 100 with the bore inside - so it either reads 100 x 100 and fits, or refuses naming both sizes. Either is an answer; write which |
| **AN16** | the family after AN12 to AN15 | `REPORT_FAMILY_CONNECTORS`: no connector; `REPORT_FAMILY_FORMS`: the same one combination, and AN14's ring joined to nothing. Look: rollback has failed on this PC before (BD1) |
| **AN17** | the signature | `python brain/heron_validate.py accept add-family-connector --by "Ajmal PS"` on the AN12/AN13 record, on the owner's word only |
| **AN18** | AN12 and AN13 on **Revit 2020** (where row 5b-410 was seen) and on **2024** | The same answers. Every member version 3 adds reads the same 2020 to 2027 (RevitAPI.xml, 2026-10-11) and compiles on all eight releases; not run on any |

## Proven 2026-10-18 (Revit 2020, session 64176, the owner's scratch families Family3 and Family4)

Each by `validate --write`, both legs rolled back, then signed by Ajmal PS and set PROVEN by him. This group's other rows were NOT RUN that day.

- `flex-family` - positive Family4, trials *Duct Width=900; Duct Height=450 | Duct Width=400; Duct Height=200*: `allHeld` true, both trials read back (974 mm overall at 900), `restored` true; negative *No Such Size=500*: nothing tried, refused by name.
- `add-family-connector` - positive Family4 on *Duct Front*: Supply Air rectangular 150 x 300 mm, sized by Frame Length and Duct Height; negative on *Center (Front/Back)*: no solid face, nothing made.
