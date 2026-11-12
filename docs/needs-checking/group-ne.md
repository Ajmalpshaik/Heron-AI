# Needs checking — Group NE

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NE - a family's 2D linked to a Yes/No by id, and an angle labelled between two planes: `LINK_FAMILY_PARAMETER` version 4 and `LABEL_FAMILY_DIMENSION` version 3 (2026-11-11)

Found missing on 2026-11-11, when Ajmal PS asked for a family's whole 2D side to be covered while building a
diffuser family's plan arrows: a symbolic line's, a detail line's, a filled or masking region's or model text's
Visible could not be linked to a Yes/No family parameter, and no card labelled an ANGLE between two planes. Both
were closed by widening the PROVEN card that does the same kind of work, on the owner's word ("widen ... no
duplicate tools, same kind of work 2 tools no need"), so both are DRAFT again until this group is run. Why, and
what is not known, is [FRAGMENT-ISSUES rows 5b-1497 and 5b-1498](../fragment-issues/section-5b-rows-401-425.md).

| What | Where | What it does |
|---|---|---|
| `LINK_FAMILY_PARAMETER` version 4 (MODIFY) | [`brain/fragments/link-family-parameter`](../../brain/fragments/link-family-parameter/fragment.yaml) | Ids in `forms` (or `selected`) may now name a detail line, a symbolic line, a filled or masking region or model text, as well as a form or a nested copy - for their Visible on a Yes/No, or whatever Revit lets be linked. Anything else still refused by name; every link read back with `GetAssociatedFamilyParameter`. Everything else version 3's |
| `LABEL_FAMILY_DIMENSION` version 3 (MODIFY) | [`brain/fragments/label-family-dimension`](../../brain/fragments/label-family-dimension/fragment.yaml) | With an ANGLE parameter: two reference planes that meet labelled with an angular dimension on an arc centred where they cross, in the floor plan or elevation that looks along them. The parameter must already hold the angle on one side or the other - refused before anything is made otherwise; one label per parameter and per pair; read back - label, angle, neither plane turned or moved. With a LENGTH, version 2 exactly |

**What is done and what is not.** Both cards compile on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). **Nothing here has run in Revit.** The batch
half is [`tools/jobs/family-2d-link-angle-ne-2026-11-11.yaml`](../../tools/jobs/family-2d-link-angle-ne-2026-11-11.yaml),
read back by `--dry-run` as 9 WOULD RUN; its ids are placeholders NE0 fills in. Each job writes its run record to
one file per fragment, so run them one at a time (or read each record before the next) when they are run.

**The model - a SAVED scratch family, never the owner's own families** (not the KDF diffuser, not
`Diffuser Flow Arrow_DRAFT.rfa`). File, New, Family, the metric Generic Model template, saved as "Heron Link Angle
Test" under `%TEMP%\heron-work\link-angle`, **Revit 2024** - a filled region in a 3D family needs Revit 2023 and
a masking region 2024 (`DRAW_FAMILY_FILLED_REGION`'s card). Every call with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

**The arrangement (NE0), made with Heron's own PROVEN tools and KEPT, except one plane drawn by hand:**
type parameters Yes/No "Show Handle" and "Show Arrow", Length "Width", "Wrong Width" and "Arrow Length", Angle
"Arrow Angle", "Arrow Back Angle" and "Wrong Angle" (`ADD_FAMILY_PARAMETERS`); in one type, Width 600, Wrong
Width 450, Arrow Length 300, Arrow Angle 30, Arrow Back Angle 150, Wrong Angle 45, both Yes/No ticked
(`SET_FAMILY_TYPE_VALUES`); "Box Left" at X -300 and "Box Right" at X 300 (`CREATE_REFERENCE_PLANES`); one solid
extrusion (`CREATE_FAMILY_FORMS`); a Supply Air duct connector (`ADD_FAMILY_CONNECTOR`) with an Air Flow type
parameter "Nominal Air Flow" = 94.4 L/s (`ADD_FAMILY_PARAMETERS`) - its Flow Configuration set to Preset in
Properties if the connector is not made so; height planes "Floor Top" at Z 0, "Trap Top" at Z 300, "Same A" and
"Same B" both at Z 500 (`CREATE_REFERENCE_PLANES`), with Length "Trap Depth" = 300, "Rim Height" = 300 and "Gap C"
= 100, and **by hand** an aligned dimension Ref. Level to "Trap Top" labelled "Rim Height" in the Front elevation
(`LABEL_FAMILY_DIMENSION` is DRAFT and cannot be kept); in Ref. Level a symbolic line "path 0,0; 300,0"
(`DRAW_FAMILY_SYMBOLIC_LINES`), a detail line "path 0,-200; 300,-200" (`DRAW_FAMILY_DETAIL_LINES`), a filled
region "polygon 300,80 600,0 300,-80" (`DRAW_FAMILY_FILLED_REGION`) and a model text "NE" (`ADD_FAMILY_MODEL_TEXT`).
**By hand** - no Heron card draws a plane at an angle (row 5b-1498): in Ref. Level, Create, Reference Plane, from
the origin to about X 600, Y 346 (30 degrees from "Center (Front/Back)"), named "Arrow Slope" in Properties, not
pinned; and a second, from the origin to about X 580, Y -155 (-15 degrees), named "Arrow Low", not pinned, with an
Angle "Spread Angle" = 45. Record the UniqueId of the extrusion, the symbolic line, the detail line, the filled region, the model
text, "Box Left" and the level "Ref. Level", and write them into the job file - the connector is named by category. Save.

| # | Run | Look for |
|---|---|---|
| **NE0** | *Open and arrange, KEEP it,* as above | The title bar names the scratch family; nothing linked, nothing labelled; "Arrow Slope" reads 30 degrees from "Center (Front/Back)" by a hand angular dimension, then deleted |
| ~~**NE1**~~ | *Version 3's form path, re-proved* - forms = the extrusion's id, links "Visible=Show Handle"; negative forms = the extrusion's and Box Left's ids; both rolled back | Positive: changed 1, "Extrusion id N: Visible not linked -> Show Handle". Negative: refused naming Box Left as none of the kinds linked by id - and the extrusion NOT linked (all or nothing) **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE1b**~~ | *Version 3's OWN proven case, re-proved* - category "Duct Connector", system "Supply Air", links "Flow=Nominal Air Flow"; negative "Flow=ZZZ No Such Parameter"; rolled back. Then by name: system "Exhaust Air" | Positive: changed 1, "Duct Connector (Supply Air) id N: Flow not linked -> Nominal Air Flow" - version 3's answer. Negative: refused, "No family parameter is called ZZZ No Such Parameter", naming Nominal Air Flow as the one that could drive Flow. Exhaust Air: refused with what the family has - **version 4's words** "Symbolic Line", "Filled Region", "Detail Line", "Model Text" where version 3 printed a Revit category; record the whole sentence **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE2**~~ | *The new ability* - forms = the symbolic line's and the filled region's ids, links "Visible=Show Arrow"; negative the symbolic line with "Visible=Arrow Length"; rolled back | Positive: changed 2, "Symbolic Line id N: Visible not linked -> Show Arrow", "Filled Region id M: ..." - **if Revit says Visible cannot be linked on either, that is the first thing this group learns** and the card is changed, not proved. Negative: refused naming Length and Yes/No, nothing linked **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE3**~~ | *The rest of the new kinds* - the detail line's and the model text's ids, links "Visible=Show Arrow"; negative the symbolic line and the level Ref. Level; rolled back | Positive: changed 2, named "Detail Line" and "Model Text". Negative: the level refused by name, the symbolic line NOT linked **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE4**~~ | *Version 2's length path, re-proved* - Box Left to Box Right, Width, centre "Center (Left/Right)"; negative the same with "Wrong Width" (450); rolled back | Positive: reading 600 mm, equalised true, angleDegrees 0 - version 2's answer. Negative: refused with 600 and 450, naming SET_FAMILY_TYPE_VALUES, nothing made **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE4b**~~ | *Version 2's OWN proven case, re-proved* - "Floor Top" to "Trap Top", "Trap Depth", no centre, pinFreeEnd left out; negative "Same A" to "Same B" with "Gap C"; rolled back | Positive: reading 300 mm in an elevation, `freeEnd` naming "Floor Top" as held by nothing and "Trap Top" as held by the dimension labelled "Rim Height" - version 2's answer. Negative: refused as in the same place, naming the pin route, nothing made **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE5**~~ | *The angle* - "Center (Front/Back)" to "Arrow Slope", "Arrow Angle", no centre; negative with "Wrong Angle" (45); rolled back | Positive: angleDegrees 30, labelled in Ref. Level, a finding that "Center (Front/Back)" defines the origin so "Arrow Slope" is expected to turn. Negative: refused before anything is made - "meet at 30 degrees (150 on the other side) and Wrong Angle holds 45". **If Revit refuses the arc or the references, that is the first thing learned** and the card is changed **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE5b**~~ | *Two free planes, and the pin* - "Arrow Slope" to "Arrow Low", "Spread Angle", pinFreeEnd=true; negative "Center (Front/Back)" to "Arrow Slope", "Arrow Angle", pinFreeEnd=true; rolled back | Positive: angleDegrees 45; `freeEnd` saying neither plane is pinned or defines the origin, "Pinned now"; `pinnedEnd` "Arrow Slope" pinned first and read back pinned. Negative: labelled, NOTHING pinned, the finding naming "Center (Front/Back)" as held fast **HELD 2026-11-12, Revit 2020 (session 33000) - Run 1 below; the card signed PROVEN that day.** |
| ~~**NE6**~~ | *The other side, and parallel planes* - the same planes with "Arrow Back Angle" (150); negative "Arrow Angle" between Box Left and Box Right; rolled back | Positive: angleDegrees 150, the arc on the obtuse side. Negative: refused as parallel, making no angle, nothing made **HELD 2026-11-12, Revit 2020 (session 33000), after row 5b-1565's repair - Run 1 below; the card signed PROVEN that day.** |
| **NE7** | *KEPT, and the flex* - NE2's and NE5's positives through `try-once` and KEEP; then `SET_FAMILY_TYPE_VALUES` Show Arrow off, then on; Arrow Angle 45, then 30; then NE5's call once more | Show Arrow off: the symbolic line and region leave Ref. Level; on: they return. Arrow Angle 45: **record which plane turned, and about what point** - "Arrow Slope" is expected, not known; the dimension reads 45. NE5 again: refused, "Arrow Angle already labels a dimension" |
| **NE8** | *Revit 2020* - a copy of NE0 made in Revit 2020 without the region (the API has none there); NE2 with the symbolic line alone, NE5 | The same answers as on 2024 - the release the owner builds families in |
| **NE9** | *Second routes.* By hand on a copy: the symbolic line's Visible, the button at the end of its row, "Show Arrow"; Annotate, Angular between the two planes, labelled "Arrow Angle" | The Associate Family Parameter dialog shows what `linkReport` said; the two angular dimensions read the same |

**Before the owner's own families.** Only once NE is through. The first run on the KDF diffuser or its arrow family
is the owner's choice, through `try-once`, behind Revit's Keep or Discard.

## Run 1 - Revit 2020, 2026-11-12 (session 33000, Ajmal PS watching)

**Model:** the SAVED scratch families `%TEMP%\heron-work\link-angle\Heron Link Angle Test.rfa` (metric Generic
Model) and `Heron Link Detail Test.rfa` (metric Detail Item), both made by `CREATE_FAMILY_DOCUMENT` this session -
never the owner's own families. Everything below was rolled back unless it says KEPT.

| Row | Positive | Negative | Verdict |
|---|---|---|---|
| NE1 | changed 1, "Extrusion id 2704: Visible not linked -> Show Handle (reads Yes)" | refused naming Box Left, the extrusion NOT linked | held |
| NE1b | changed 1, "Duct Connector (Supply Air) id 2722: Flow not linked -> Nominal Air Flow (reads 0.0 L/s)" - version 3's answer | refused, "No family parameter is called ZZZ No Such Parameter ... Nominal Air Flow" | held. Exhaust Air by name: "What it has that can be linked: 1 Duct Connector (Supply Air), 1 Extrusion, 1 Model Text, 1 Symbolic Line - in the Revit categories Connector Element, Lines" (version 4's words) |
| NE2 | changed 1, "Symbolic Line id 2735: Visible not linked -> Show Arrow" | refused, "Arrow Length (Length) cannot drive Visible (Yes/No) ... The ones that could drive it: Show Arrow, Show Handle" | held |
| NE2/NE3 (Detail Item family) | changed 2, "Detail Line id 2060 ... Show Arrow  \|\|  Filled Region id 2063 ... Show Arrow" | refused, no such parameter, naming Show Arrow | held - **the filled region on Revit 2020, in a 2D family** |
| NE3 | changed 1, "Model Text id 2739: Visible not linked -> Show Arrow" | refused naming plane "Box Right" (the level's id was not to hand), the symbolic line NOT linked | held |
| NE4 | 600 mm, equalised, angleDegrees 0 - version 2's answer | refused, "600 mm apart and Wrong Width holds 450 mm" | held |
| NE4b | 300 mm; freeEnd: "Floor Top is held by nothing ... while Trap Top is held by the dimension labelled Rim Height" - version 2's answer | refused, "Same A and Same B are in the same place" | held. The Rim Height label was made by this card's own `try-once`, KEPT by the owner |
| NE5 | 30 degrees, labelled in Ref. Level | refused, "meet at 30 degrees (150 on the other side) and Wrong Angle holds 45" | held |
| NE5b | 45 degrees between two free planes; "Arrow Slope pinned first ... reads back pinned" | labelled 30, nothing pinned | held |
| NE6 | **first run threw "The labelled angle reads 330 degrees where Arrow Back Angle holds 150"** - fixed (row 5b-1565), re-run: 150 degrees | refused, parallel planes make no angle | held after the fix |
| NE7, NE8 (2024), NE9 | - | - | NOT RUN |

**The sloped planes were made by Heron**, not by hand: the owner's hand-drawn planes did not reach the family (Heron
read none), so `CREATE_REFERENCE_PLANES` was widened to draw a plane at an angle and its `try-once` was KEPT by the
owner - Group NN, row 5b-1564.

**Plan corrections** (row 5b-1565): a detail line cannot exist in a Generic Model, so it ran in the Detail Item family;
a triangle is written as corners ("300,80; 600,0; 300,-80"), not "polygon", and the type is "Solid Fill - Black";
`SET_FAMILY_TYPE_VALUES` cannot write an Air Flow value, and the link needs none.

**Proof drafts:** `brain/proof-drafts/link-family-parameter.yaml` (the detail line and filled region run) and
`brain/proof-drafts/label-family-dimension.yaml` (NE5, after the fix). Both await the owner's signature.

**SIGNED 2026-11-12 by Ajmal PS - `LINK_FAMILY_PARAMETER` version 4 and `LABEL_FAMILY_DIMENSION` version 3 are
PROVEN** on Run 1's two drafts (the proof block in each card: "Heron Link Detail Test", 1,503 elements, and
"Heron Link Angle Test", 1,546 elements; Revit 2020, session 33000). **Revit 2024, the same day:** each card
carries a `proof.releases` entry signed by Ajmal PS - "R24 Generic", Revit 2024, session 13164, both halves
rolled back; the link on a symbolic line, a masking region and model text, the angle at 150 degrees (the 2024
section of the [work note](../work-notes/investigations/2026-11-11-family-2d-capabilities.md)). That is the other
release NE8 asked for, on scratch files made for it and not on a copy of NE0, so NE8 is left as written. Still
open, not blocking: NE7 (the kept flex), NE9 (second routes). "Both are DRAFT again" and "Nothing here has run
in Revit", above, were true on 2026-11-11.
*Written back 2026-11-12 from the cards, in a cloud container (no Revit); nothing was run here.*
