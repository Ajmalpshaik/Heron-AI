# Needs checking — Group CB

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CB - checking an MEP family will connect and resize, in the Family Editor, before it is loaded (2026-10-04)

**`check-family-standards` (FRG-QA-021) version 2, DRAFT.** Built 2026-10-04 after three PPR valves
built through Heron in Revit 2024 looked finished and were not: the two check valves would not connect
to a pipe (Work Plane-Based Yes; the inline one also Part Type Normal) and the gate valve would not
resize with its pipe (Nominal Diameter and the formulas off it were TYPE parameters). Row
[5b-313](../fragment-issues/section-5b-rows-176-200.md). **Compiled on 2020 to 2027; nothing below
has run in Revit.** Every run is a READ: run it with `validate` and no `--write`, so Revit refuses any
change. Inputs for every family run: `namePattern=` (empty), `requiredParameters=` (empty),
`mustHaveConnectors=false`, unless the row says otherwise. The answer to read is `familyReport` -
one string, every check with PASS / FAIL / WARN / NOT CHECKED and its fix.

| # | Run | Look for |
|---|---|---|
| **CB1** | **THE PLANTED FAMILY - the case that must FAIL by name.** A scratch family from the metric Pipe Accessory template, rolled back (`validate --setup`): one solid extrusion along X; two pipe connectors on its end faces; then Work Plane-Based **Yes** and Part Type **Normal** (SET_FAMILY_SETTINGS); one connector flipped to point INTO the body (SET_FAMILY_CONNECTOR_ROLES `flip=true`); both Diameters associated to a **type** parameter `Size` that two other type formulas read (`Cap = Size + 10 mm`, `Body = Cap * 2`); one more parameter `Leftover` that nothing uses | `offStandard` **6**, each named in `familyReport`: **FAIL Work Plane-Based**, **FAIL Part Type is Normal**, **FAIL the inward connector by id**, **FAIL "Size" ... TYPE parameter ... 2 formula(s) depend on it (1 directly)**, **FAIL "Leftover"** with `DELETE_FAMILY_PARAMETERS "Leftover"`. and **FAIL the two connectors are not opposite** - a flipped connector makes the pair point the same way, the 5b-309 symptom Ajmal saw. Any other FAIL is read and explained before the run is called a pass |
| **CB2** | **TRG_PLMB_VLV_PPR Gate Valve_GV_R0, as saved** (open it, run with `--in`) | **FAIL `Nominal Diameter` is a TYPE parameter** with the formulas depending on it counted - the build reported 35; check the count against REPORT_FAMILY_PARAMETERS. **PASS** Work Plane-Based No, Part Type Valve Breaks Into, both connectors pointing out, opposite on one line, Global. Any unused-parameter FAIL is checked by hand before it is believed |
| **CB3** | **TRG_PLMB_VLV_PPR Check Valve Inline_CVI_R0** and **Check Valve Swing_CVS_R0**, as saved after SET_FAMILY_SETTINGS fixed them - **the D-30 case that must NOT contain what CB1 reported** | No placement FAIL, no size FAIL, no inward connector. Their sizes are INSTANCE parameters; `familyReport` says how many formulas follow them. A WARN for a connector still Domestic Cold Water is expected if it was never set to Global |
| **CB4** | **The Coarse / Medium WARN** on the gate valve, or on CB1 with a nested family placed over the body: a nested family whose own forms show at Coarse and Medium, and a solid form of the host shown at every level, inside it | A WARN naming the form id, the levels and the nested family, with `SET_FAMILY_FORM_VISIBILITY ... Fine only`. **This rests on one unmeasured assumption:** that a nested instance's geometry read with `Options.DetailLevel = Coarse` holds only what it draws at Coarse. If the WARN never fires on a case built to make it fire, record that here and drop the check rather than leave it silent |
| **CB5** | **TRG_MECH_EQP_Fan Coil Unit_FCU_R0** - equipment, a run's END | No Work Plane-Based and no Part Type check at all. A connector sized by a TYPE parameter is **PASS**, not FAIL - a fixed size per type is right for equipment. A connector sized by nothing is a WARN |
| **CB6** | **A Generic Models family** (any scratch family, Generic Model template) | No connector FAIL; a finding that says only the name, parameters and tidiness were checked. With `mustHaveConnectors=true`, ONE FAIL: no connectors |
| **CB7** | **Version 1 unchanged, in a project** - `test projject`, the inputs of version 1's proof | The same shape of answer version 1 signed: `offStandard`, `notChecked`, the system families excluded; `familyReport` empty and `warned` 0 |
| **CB8** | CB1 to CB3 on **Revit 2020** | The same answers. `PartType` and category names are read by NAME on the running Revit; OST_PlumbingEquipment does not exist there and must simply never match |

**Then sign.** CB1 is the positive case and CB3 the negative for D-30; CB2, CB5 and CB6 are the
second look. Nothing is kept in any family by any of these runs.
