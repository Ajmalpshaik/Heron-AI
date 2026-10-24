# Needs checking — Group JA

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JA - a duct port on a flange ring, sized by its opening: `ADD_FAMILY_CONNECTOR` version 5 (2026-10-24)

[`add-family-connector`](../../brain/fragments/add-family-connector/fragment.yaml) version 5, written for the
owner's volume control damper ([row 5b-1334](../fragment-issues/section-5b-rows-401-425.md)): its ends are a
nested casing frame, so no face of the damper's own lies there, a connector cannot go on a reference plane
through the API, and one on the nested frame's face sat off-centre and did not follow *Length*. A 3 mm ring
sketched on each end plane is the face; version 5 measures a face whose inner loop holds the connector's
centre by that opening, and refuses a connector off the opening's centre.

**What is done and what is not.** The library compiles for 2020 and 2027 (`tools/check-fragments-compile.py`,
2026-10-24). Measured in Revit 2020 only - the rows below. `revit_change` refuses version 5 until JA4 is
signed. The fragment's C# is read live - no restart.

**The family.** The owner's *TRG_Duct Accessory_Volume Control Damper* (`D:_BIM_Revit_Families_HVAC_Duct_Accessories`),
Duct Accessories, duct along X, *Width* 600, *Height* 400, *Length* 300: in it, through `revit_change` with
PROVEN tools, a 3 mm ring on *Casing Right* (X 147 to 150) and one on *Casing Left*, outside *Ring Width* x
*Ring Height* (= *Width* + 40, *Height* + 40), every edge face locked to the ring and duct planes; the two old
1 mm pads deleted, so nothing else of the damper's own lies on either end plane. Every row is
`validate --in "TRG_Duct Accessory_Volume Control Damper" add-family-connector --write`, rolled back, with
`facePlane=Casing Right` and `system=Supply Air`.

| # | Run | Look for |
|---|---|---|
| **JA0** | *Negative FIRST.* `sizeParameters=Ring Width, Ring Height` (640, 440 - the ring's outside) | Refused - *"The opening of the ring face on "Casing Right" is 600 x 400 mm ..."* - `sizedBy` empty, nothing kept |
| **JA1** | *The port.* `sizeParameters=Width, Height` | `connector` *"duct connector, Supply Air, rectangular 600 x 400 mm, at X 150, Y 0, Z 0, pointing right (+X) ... centred in the ring face's opening and sized by it"* |
| **JA2** | *The flex.* JA1 kept on the owner's word, then *Length* 400, *Width* 1000, *Height* 250 through `SET_FAMILY_TYPE_VALUES` and the connector read | At X 200, Y 0, Z 0, 1000 x 250 mm; put back to 300 / 600 / 400 |
| **JA3** | *A face of one loop is unchanged.* `facePlane` naming a plain box's face, as version 4's proof | The same answer as version 4, no word of an opening |
| **JA4** | The signature, on the owner's word only, after JA0 to JA3 | `python brain/heron_validate.py accept add-family-connector --by "Ajmal PS"` |

A row that fails stays open; nothing in a row changes to make it pass.

### Results - 2026-10-24, the owner's laptop: Revit 2020, session 94928

Before them, the same afternoon in session 83568, rolled back: a connector on the reference plane *Casing Right*
threw *"oEdgeLoopRef.ptr() is NULL"*; on a sketch plane made from it *"The reference is not a planar face"*; on the
nested frame's face it went to the centre of one 30 mm flange strip (Z 200.5) and stayed at X 150 when *Length*
moved the frame to X 200; on a 3 mm ring sketched on *Casing Right* it went to X 150, Y 0, Z 0 and moved to X 200
with the plane (row 5b-1334). Revit 2020 then closed itself (Windows reported its paging file too small); the
damper saved at 15:12 was reopened and the rings rebuilt before the rows below.

| # | What came back | State |
|---|---|---|
| JA0 | *"The opening of the ring face on "Casing Right" is 600 x 400 mm, and "Ring Width" and "Ring Height" hold 640 and 440 mm - a port sized by them would not fit the face it sits on. NOTHING from this call was kept."* - a throw, rolled back; recorded as the check, not as the proof's negative (a thrown negative is not one) | **PASS** |
| JA1 | *"duct connector, Supply Air, rectangular 600 x 400 mm, at X 150, Y 0, Z 0, pointing right (+X), on the face lying on "Casing Right", centred in the ring face's opening and sized by it"*, `sizedBy` *connector width <- Width; connector height <- Height*. Negative in the same run, `facePlane=Center (Front/Back)`: *"Nothing was made. No solid face lies on "Center (Front/Back)" ..."* | **PASS** |
| JA2 | After JA4 was signed, both connectors KEPT through the branch's client (`fragment ... --write --apply`): *"... 600 x 400 mm, at X 150, Y 0, Z 0, pointing right (+X) ... centred in the ring face's opening"* and *"... at X -150, Y 0, Z 0, pointing left (-X) ..."*. Then *Length* 400, *Width* 1000, *Height* 250: `REPORT_FAMILY_CONNECTORS` read *"rectangular 1000 x 250 mm (width <- Width, height <- Height) ... at X 200, Y 0, Z 0"* and *"... at X -200, Y 0, Z 0"*; put back to 300 / 600 / 400 and read 600 x 400 at X 150 and -150 | **PASS** |
| JA3 | `facePlane=Link Right` (the linkage bar's side, one loop, 3 x 266.67): *"The face on "Link Right" is 3 x 266.67 mm ..."* - measured whole, no word of an opening, as version 4 | **PASS** |

The proof draft has JA1 as its positive and its negative, fingerprint `43d83188366b7b85`, second route NOT
ESTABLISHED. Not run: any Revit after 2020; a round port or a pipe on a ring (the round case compares the
diameter to its parameter and never measured the face, before or after).
