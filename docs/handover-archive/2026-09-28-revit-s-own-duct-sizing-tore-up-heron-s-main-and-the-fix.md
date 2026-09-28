# Session note — REVIT'S OWN DUCT SIZING TORE UP HERON'S MAIN, AND THE FIX PUTS THE TRANSITION ON THE UNIT

> **Session note** from 2026-09-28, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-09-28 — REVIT'S OWN DUCT SIZING TORE UP HERON'S MAIN, AND THE FIX PUTS THE TRANSITION ON THE UNIT

Model: "heron ai bulding", Revit 2024 (session 27288), the owner's L-shaped 14-office building.

**NEW.**

- **The owner's Duct Sizing broke Heron's supply run.** The `duct-layout` skill drew a 700 mm piece of
  850x195 duct out of each fan coil unit for the transition to sit in, leaving a 36 mm stub. Revit's Duct
  Sizing resized the stub, deleted the main carrying both taps and left two diffusers of three on open
  ends - read back before and after with `REPORT_CONNECTORS`. **Fixed in PR #354** ([row 5b-250](../fragment-issues/section-5b-rows-176-200.md),
  proof owed as [AL7](../needs-checking/group-al.md)): `FIT_MEP_JOINTS` version 2 builds the transition
  straight onto the outlet, and the skill draws the main from the outlet at the main's size. On the
  owner's word every duct in the model was removed and all 14 offices redrawn this way;
  `SELECT_BY_CONNECTION_STATUS` found no open end, and his Duct Sizing on Offices 01 and 02 stayed joined.
- **All 14 units moved 275 mm forward** for the owner's new rule: 600 mm from the unit's back - the
  return-air connection's face - to the door-side wall. Read back on all 14 at 600 mm. The supply ducts
  stayed joined: Revit shortened each main.
- **Fan coil data** (all written with `IMPORT_PARAMETER_VALUES`, read back numerically): Designed
  supply and return air per office from the spaces' own figures; two new types,
  `TRG_FanCoilUnit_1.5TR` (12 units) and `TRG_FanCoilUnit_2.0TR` (Offices 04 and 05), carrying total
  cooling 5.28 and 7.04 kW, sensible 4.45 and 5.31 kW (an estimate from the design air at 11 K), 240 V
  single phase; `Air_Change_Rate` about 12 per hour on every unit. The unused type `FCU-01` was deleted
  on the owner's word. A **Mechanical Equipment Schedule** was created. The owner re-linked the two
  connectors' Flow from `Designed_*` to `Actual_*_Air_Flow` in the family by hand; a separate session
  built the general tool for it, `LINK_FAMILY_PARAMETER`, and proved it on that family with nothing kept.
- **Approximate cooling**: 120 W/m² over the 14 offices (the corridor left out on purpose) is 71.8 kW,
  20.4 TR - the value the owner chose on 2026-09-24, not a heat-load calculation.

**MISTAKES WORTH NOT REPEATING.**

- **Moving a unit together with its connected transition moved both twice** (550 mm for 275).
  Select the unit ONLY: the connected transition follows once and Revit shortens the main.
- **Revit stores power in its own internal unit** (watts x 10.7639), so a numeric read-back of a
  capacity or a voltage has to convert first - a search for 5275 found nothing while 5.28 kW was there.
- **`check-routing` and `check-intrusion` were run without `HERON_KNOWLEDGE`** and rebuilt the shared
  store from this branch. It held exactly main's cards, so nothing was lost this time.
- **Heron's print-the-whole-list trick** (a joined string added to a read-only fragment, read once,
  then `git checkout`) was needed twice for `REPORT_PARAMETER_INVENTORY`; its `findings` list is cut
  to three items in every reply.

**TO DO.**

- **Room name and number on 104 elements** (14 units, 90 diffusers): `ID_Room_Name` and
  `ID_Room_Number` exist in the owner's company shared parameter file (group `Location_Data`) but not in
  the project, and `ADD_PROJECT_PARAMETER` is risk ADMIN, which Heron refuses. A cloud session is
  building owner-controlled **Admin** and **Publish** switches on the ribbon; once they are installed the
  parameters can be bound and filled - which element sits in which room was worked out by position and
  checked in three parts of the building.
- **Values waiting on the owner**: chilled-water temperatures (`Chilled_Water_Flow` still carries the
  family's 0.9 L/s), people per office (fresh air), fan power (power, amps and VA follow).
- The owner's Duct Sizing on Offices 03 to 14; the return air and the corridor unit; chilled water and
  power to the units.
- The owner's Revit points its shared parameter file at an old Heron scratch file; his company file is
  `D:\Ajmal\BIM Resources\NEW\Modeling\06_Shared_Parameters\Shared_Parameters.txt`.
