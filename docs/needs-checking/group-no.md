# Needs checking — Group NO

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NO - a family connector's shape changed, and a connector moved onto a reference plane: `SET_FAMILY_CONNECTOR_ROLES` version 5 (2026-11-11)

Found missing on 2026-11-11 while finishing the owner's diffuser plenum families on Revit 2020: a hand-placed
duct connector could not be made round, its whole set-up took three or four calls, and it could not sit on a
reference plane without a solid under it. Both were closed by widening the PROVEN card that edits a connector
already in the family, so it is DRAFT again until this group is run. Why, and what is not known, is
[FRAGMENT-ISSUES rows 5b-1566 and 5b-1567](../fragment-issues/section-5b-rows-401-425.md); the lock that is still
missing is row 5b-1568.

| What | Where | What it does |
|---|---|---|
| `SET_FAMILY_CONNECTOR_ROLES` version 5 (MODIFY) | [`brain/fragments/set-family-connector-roles`](../../brain/fragments/set-family-connector-roles/fragment.yaml) | `settings` takes "shape=Round / Rectangular / Oval" on a duct connector, through its Shape parameter, with `sizeParameters` counted against the new shape; and "host=<reference plane>" on Revit 2023 on, moving the connector onto the plane where it sat. Everything else version 4's |

**What is done and what is not.** The card compiles on all eight releases, 2020 to 2027, with the whole library
(`tools/check-fragments-compile.py`, 2026-11-11, the owner's PC). **Nothing had run in Revit when this group was written** - the Revit 2020 run of 2026-11-12 is under
the table.

**The model - a SAVED scratch family, never the owner's own families** (not the KDF diffusers, not the plenum
boxes). File, New, Family, the metric Mechanical Equipment template, saved as "Heron Connector Shape Test" under
`%TEMP%\heron-work\connector-no`. **NO1-NO3 and NO7-NO9 on Revit 2020** (the owner's release for these families);
**NO4-NO6 on Revit 2024** (`ChangeHostReference` is 2023 on) - the same family saved up to 2024 as a copy. Every
call with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

**The arrangement (NO0), made with Heron's own PROVEN tools and KEPT, except one connector placed by hand:**
Length type parameters "Box Width" = 400, "Box Depth" = 300, "Neck Diameter" = 250 and an Air Flow type parameter
"Designed_Air_Flow" = 94.4 L/s (`ADD_FAMILY_PARAMETERS`, `SET_FAMILY_TYPE_VALUES`); a reference plane "Box Top" at
Z 300 and "Box Front" at Y -150 (`CREATE_REFERENCE_PLANES`); a box from Ref. Level to "Box Top"
(`EXTRUDE_BETWEEN_PLANES`); **by hand**, a duct connector on the box's top face (Create, Duct Connector, on the
face) - Revit makes it Rectangular 304.8 x 304.8; a pipe connector on the face lying on "Box Front"
(`ADD_FAMILY_CONNECTOR`, "Domestic Cold Water", sized by Neck Diameter). Record both connectors' ids
(`REPORT_FAMILY_CONNECTORS`). Save.

| # | Run | Look for |
|---|---|---|
| **NO0** | *Open and arrange, KEEP it,* as above | The title bar names the scratch family; the hand-placed connector reads Rectangular 304.8 x 304.8 in `REPORT_FAMILY_CONNECTORS` |
| **NO1** | *Shape and its size in one call* - connectors "Box Top", settings "shape=Round; sizeParameters=Neck Diameter"; negative the same connector with "sizeParameters=Neck Diameter" alone; both rolled back | Positive: changed "shape rectangular -> round", "diameter <- Neck Diameter"; read back round 250 mm. **If Revit refuses the Shape parameter, or Heron refuses because its number is not the connector's shape, that is the first thing this group learns** - record the sentence and the number, and the card is changed, not proved. Negative: refused - rectangular, TWO length parameters - with the hint to give "shape=Round"; nothing changed |
| **NO2** | *Oval and back* - "shape=Oval; sizeParameters=Box Width, Box Depth", then (separately) "shape=Rectangular"; negative "shape=Triangle"; rolled back | Oval 400 x 300, width and height driven; rectangular read back; Triangle refused, nothing changed |
| **NO3** | *The whole set-up in one call* - "shape=Round; sizeParameters=Neck Diameter; system=Supply Air; flowDirection=In; flowConfiguration=Preset; presetFlow=[Designed_Air_Flow]"; negative connectors = the pipe connector's id with "shape=Round"; rolled back | Positive: all six changed and read back. Negative: refused naming the pipe connector and its domain - Shape is a duct connector's |
| **NO4** | *Revit 2024: onto the plane* - the connector made on the box's top face by `ADD_FAMILY_CONNECTOR` (Supply Air, Box Width, Box Depth), settings "host=Box Top"; KEEP; then delete the box by hand | Changed "hosted on the reference plane Box Top" at the point it sat, still pointing up, the finding that it is not locked; after the box is deleted the connector is STILL THERE. **If `ChangeHostReference` throws on a reference plane's reference, record Revit's words** - row 5b-1567 becomes an API limit on every release. Undo the delete afterwards |
| **NO5** | *Revit 2024 negatives* - "host=Center (Left/Right)" (a vertical plane, the connector pointing up); "host=ZZZ No Such Plane" | Each refused - not square to the way it points; no such plane - nothing changed |
| **NO6** | *Revit 2024: a hand-placed work-plane connector* - place a duct connector by hand with Place on Work Plane on "Box Top", then settings "shape=Round; sizeParameters=Neck Diameter" naming it by its id; rolled back | It is found by its id (and by "Box Top"); round and sized - the honest way round on 2020 works the same |
| **NO7** | *Revit 2020: the refusal* - "host=Box Top" | Refused naming Revit 2020, ChangeHostReference arriving in 2023, and both ways round; nothing changed |
| **NO8** | *Version 4's own proven case, re-proved* - on "Heron Connector Ticks Test" (or a fresh copy of its arrangement) connectors "Outlet Face, Inlet Face", "allowSlopeAdjustments=yes"; negative "Duct Face" with the same; rolled back | Version 4's answer and its refusal, word for word |
| **NO9** | *The proof* - `heron validate` on NO1 (positive) and NO1's negative, Revit 2020; then the signing | A proof block naming the model, both cases and a fingerprint; the card PROVEN again on Ajmal PS's word |

**Run 2026-11-12, Revit 2020, session 45552, on "Heron Connector Shape Test" (every run rolled back; records in `%TEMP%\heron-work\connector-no`).** NO0 made with Heron's PROVEN tools - the pipe connector is id 2572 on "Box Front", the hand-placed duct connector id 2583 on "Box Top", Rectangular 304.8 x 304.8 as expected. **NO1's first run refused "has no Shape in this Revit"** - the card read a parameter a family connector has not got; the Shape is `CONNECTOR_PROFILE_TYPE` (row 5b-1577), fixed the same day. With the fix: **NO1, NO2, NO3, NO7 and NO8 held**, both halves each - the answers are on row 5b-1577. NO2's "then shape=Rectangular" was not run on its own: on a rolled-back family the connector is rectangular already, so it would only test "was already". **NO9:** the proof draft is written from NO1; the signing is the owner's. **NO4-NO6 not run** - they need Revit 2023 or later.
