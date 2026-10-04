<!--
Heron-Agent:  HERON-MEP-HVD-001
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 44 — Building loads from the model: Revit rooms in, a checked load report out, the values back in Revit

**Asked 2026-10-04 by the owner.** He found a published, open-source residential load-calculation skill
written by an HVAC engineer and asked for three things: check whether it is correct, fix what is wrong, and
make its idea part of Heron. In his words: say *"calculate the loads for this house"*, take the room sizes
from the Revit model, calculate, show everything in the Companion, produce a PDF, and *"after finalizing ...
it will update everything in the Revit"*.

**What this page is.** The study of that skill, the decision on how much of it Heron takes, and the plan,
in phases, for the feature. **It is a design, not a build.** Nothing it names is built until the owner
approves this page, and everything it builds starts DRAFT ([D-30](DECISIONS.md)).

**In Revit words.** Today Heron can work out one room's cooling load if you type in its walls and windows.
This feature lets Heron *read* every Space in the model - its floor, its outside walls and which way they
face, its windows - work out every Space's load in one go, show you the table, let you change the people
and lights, give you a PDF, and on your word write the loads and airflows into the Spaces and the diffusers.

---

## 1. The skill that was studied

A Claude Code skill (MIT licence, published 2026-10-01) that turns **house plans in PDF** into room-by-room
heating and cooling design loads. It reads the PDF plans, builds one geometry model, and runs it through two
engines that fail differently: ACCA Manual J (through OpenStudio-HPXML) as the answer, and an EnergyPlus
design-day model as an independent cross-check. It writes a bilingual HTML/PDF report, a clickable 3D model
of the exact surfaces it calculated, and a surface take-off CSV.

It is careful work. The design notes are honest about every limit; every input carries its source; the
user must confirm the geometry before any load runs; no safety factor is hidden.

### 1.1 Is it correct? - what was run

| Check | Result |
|---|---|
| Its own test suite on this PC (engines present in the user profile) | **39 passed, 0 failed, 0 skipped** |
| The physics core read line by line - psychrometrics, unit conversions, R/U, infiltration leakage area, ground U-values, orientation | **Correct.** Saturation pressure at 25 °C 3169.2 Pa; humidity ratio at 35/24 °C 0.01423 |
| Run on Windows from a deep folder | Fails to import its geometry library - the Windows 260-character path limit, not a code defect |

### 1.2 Defects found

Found by a code audit on 2026-10-04 and checked with numbers. They are **its** defects, recorded here so
Heron does not repeat them. Heron does not edit that project; reporting them to its author is the owner's
choice (§9, question 6).

| # | Defect | Effect | Lesson Heron takes |
|---|---|---|---|
| 1 | A window type with no SHGC is read as 0.0 and passes silently | Solar gain through it vanishes: sensible cooling **-18%** on its own example, no warning, and the cross-check gap stays under its flag | **A window with no SHGC or U refuses the run.** Never a default (§5.3) |
| 2 | One room's appliance gain set to `0` drops the kitchen default, while the report still says the default was applied | Report says one thing, the calculation did another | The report is printed from the numbers that were used, never from a separate list |
| 3 | A whole-house occupant count is ignored once any one room sets its own | Five rooms lose their people | Per-Space inputs only; no hidden whole-building fallback |
| 4 | A temperature with no unit is read as °F; a daily range with no unit as °C | -23 becomes -30.6 °C | **Every value carries its unit** in the take-off and the inputs |
| 5 | Hip-roof attic volume uses area × rise / 3 | -20% on a 12 × 6 m roof (cross-check only) | - |
| 6 | Dead branch: an ISO 13370 swap is never applied | Cross-check only | - |
| 7 | Indoor RH entered as 50 instead of 0.50 is not range-checked | Probably refused later by the engine | Range-check every input at the door |
| 8 | Cooling day fixed at 21 July; sea-level airflow factor; flat roofs get no azimuth | Wrong in the southern hemisphere and at altitude | Airflow uses the site altitude, as `supply_airflow` already does |

### 1.3 What it cannot do for the owner's work

- **Houses only.** The engine is residential and writes every model as "single-family detached". People
  are fixed at Manual J's per-person figures; there is no lighting or equipment W/m², no ASHRAE 62.1 outdoor
  air per person and per area, no curtain wall, no plenum.
- **North America.** Weather-station search covers the USA and Canada; the code lookup is Canada's NBC.
  A Doha weather file can be pointed at by hand, so a **villa in Qatar** is possible; a **Qatari office or
  school is not** without replacing the engine.
- **Plans, not models.** Most of its code reads PDF plans - calibrating scale, finding lines on scans.
  Heron has the Revit model, which already knows every room's true size.
- **About 350 MB of engines**, downloaded once into the user profile, and weather downloaded at run time.

### 1.4 Why it is not copied in

The owner asked for it *"exactly copy paste"*. The licence would allow that with its notice kept. It is not
done, for four reasons:

1. **Heron's standing rule.** Reference projects are studied and re-authored, never imported - not their
   names, code or dependencies ([AGENTS.md](../AGENTS.md), [31](31-studying-the-existing-libraries.md)).
2. **It solves a different building.** Its engine is residential; the owner's buildings are offices and
   schools in Qatar.
3. **Heron already has the commercial engine.** `heron_hvac` ([41](41-hvac-design.md)) computes a room's
   cooling load component by component, the month-by-month peak with ASHRAE's clear-sky sun checked to
   ASHRAE's own workbook, heating loss, 62.1 outdoor air, supply airflow, and the diffuser flows that
   `SET_AIR_TERMINAL_FLOW` writes. It imports only Python's standard library.
4. **The engines break Heron's install rules.** 350 MB of outside programs and a weather download at run
   time sit against Heron's offline, per-user, degrade-don't-break install ([`requirements-optional.txt`](../requirements-optional.txt)).

**For houses, the owner keeps it as it is.** It is already installed on his PC as its own Claude skill, and
it works from PDF plans with a Doha weather file. That path needs nothing from Heron.

---

## 2. His calculation against Heron's - what is extra, and what Heron must add

The owner asked: *"is there anything extra in his calculation, or do we need to update our skills?"*

| | That skill | Heron today | Heron takes it? |
|---|---|---|---|
| Room-by-room loads for the **whole building in one run** | Yes | No - one room per call, walls typed in | **Yes - the core of this plan** (§4, §5) |
| **Geometry read automatically** | From PDF | No | **Yes - from Revit Spaces** (§4) |
| **Confirm the geometry before any load runs** | Hard stop | No | **Yes** (§6, gate 1) |
| QA - areas reconcile, gaps, every input has a source | Yes | Per value only (D-33) | **Yes** (§6) |
| **3D view of the exact surfaces calculated** | Yes | No | **Later** (phase 6) - first the table; Revit itself is the 3D view |
| Surface take-off CSV | Yes | No | **Yes** - free once the take-off exists |
| **PDF report** with every input's source | Yes | No PDF writer | **Yes** (§8) |
| What-if copies (airtightness, glass, orientation) | Yes | No | **Yes** - a second run of the same take-off with one value changed |
| A second method that fails differently | EnergyPlus | No | **Not now** - §9 question 5 |
| Ground and slab loss, attic and crawlspace buffer zones | Yes | Floors as a surface only | **Heating only, later** - minor in Qatar cooling |
| Infiltration from a blower-door test (ACH50) | Yes | As given, L/s or ACH | No - commercial practice gives ACH or L/s·m² |
| Duct loads in an unconditioned attic | Yes | As given | No - ducts above a ceiling are inside the envelope in his buildings |
| Commercial gains - lighting and equipment W/m², people by activity | No | Yes | Heron keeps its own |
| ASHRAE 62.1 outdoor air per person and per area | No | Yes | Heron keeps its own |
| Latent load per room | No (whole house) | Yes | Heron keeps its own |
| Peak month and hour | One July day | Every month, hour by hour | Heron keeps its own |
| Duct, pipe, diffuser sizing; write into Revit | No | Yes | Heron keeps its own |

**Skills to update** once it is built: `brain/skills/space-airflow.yaml` (DRAFT) today reads loads Revit
already holds. It gains the step that calculates them first. That is the owner's open question in
[41 §15](41-hvac-design.md) item 4 - how a skill names a brain-side calculation - and this feature answers
it (§7).

---

## 3. What the owner gets - the whole flow

```
"calculate the loads for this building"
   │
   ▼  1 READ  (fragment, READ - nothing in the model changes)
 Revit Spaces → take-off: every Space's floor, height, volume, and each face -
 outside wall / partition / roof / floor - with its area, the way it faces, its
 windows and doors, and the type each one is
   │
   ▼  2 CHECK  (gate 1 - stop and show)
 QA: Spaces not enclosed, areas that do not add up, a window with no U or SHGC,
 a wall type with no U, no True North, no site location → listed; he confirms
   │
   ▼  3 ASK ONCE  (D-33, D-111)
 Design conditions, standards, and per-Space-type people / lights / equipment / OA -
 each value labelled: a named standard, his instruction, or an assumption
   │
   ▼  4 CALCULATE  (heron_hvac, unchanged engine, every Space)
 per Space: sensible, latent, total, heating, supply airflow, OA, TR
 per zone and building: the coincident peak (block load), not the sum of peaks
   │
   ▼  5 REVIEW  in the Companion "Loads" panel
 inputs editable, results beside them; Recalculate; what-if
   │
   ▼  6 REPORT  PDF + take-off CSV, written to the project folder
   │
   ▼  7 FINALIZE  (his click - gate 2; MODIFY, one undo entry)
 Spaces get their loads and airflows; diffusers get their share of the flow
```

---

## 4. Reading the model - the take-off

### 4.1 Why Spaces, not Rooms

The HVAC load belongs to the **MEP Space**: the engineer's conditioned volume, bounded by the ceiling
(VA BIM Manual v2.2, 2.13.1: MEP Spaces are the volumes used for load calculations). Rooms are the
architect's. The take-off reads Spaces; a model with Rooms and no Spaces gets a QA line saying so and the
existing `PLACE_ROOM_AT_POINT asSpace=true` route offered.

### 4.2 The new reader

**One new READ fragment**, working name `REPORT_SPACE_ENVELOPE`. For each Space it returns:

- id, number, name, level, area, volume, height, zone, Space Type, Condition Type;
- **every boundary face**, from Revit's spatial-element geometry calculator (it gives each face the element
  that bounds it and whether it is a side, a top or a bottom). For each face:
  - the bounding element and its type; gross and net area;
  - **what is on the other side**: outside, another Space (which one), an unconditioned Space, ground, roof;
  - **the way it faces**: the outward normal turned to true north by the project's True North angle -
    reported as an azimuth in degrees and a compass word;
  - its tilt (wall, roof, floor);
- **every window, door and curtain-wall panel** in those faces: width, height, area, type;
- per **type** used: the thermal values Revit holds - U (heat transfer coefficient), and for glazing SHGC
  and visible transmittance - and whether each was set or is missing;
- once per model: the site latitude, longitude and elevation, the True North angle, and the units.

Every length and value comes back **with its unit** (defect 4). No value is defaulted in C#; a missing one
is reported missing.

**What this needs proving on a real Revit** (it becomes a NEEDS-CHECKING group): where each release keeps
a window type's U and SHGC; whether a curtain wall is returned as one bounding face or as panels; a Space
bounded by a room separator line; a Space whose ceiling is not room-bounding; True North on a rotated
project.

### 4.3 What already exists and is reused

`READ_SPACE_LOADS`, `REPORT_SPACE_AIRFLOW`, `REPORT_ROOM_SPACE_DATA` (PROVEN) for checks and read-backs;
`CREATE_HVAC_ZONE` (PROVEN) when zones are missing; `SET_PARAMETER_VALUES_BY_ID` (DRAFT), the Companion's
own writer, and `SET_AIR_TERMINAL_FLOW` (DRAFT) for the write-back. Nothing in Heron reads Revit's energy
analytical model or exports gbXML, and this plan does not need either: gbXML goes through Autodesk's cloud
for its own loads, and the model is never uploaded ([D-26](DECISIONS.md)).

---

## 5. Calculating - the building runner

### 5.1 A new brain module, the engine untouched

A new module in `brain/` turns the take-off plus the inputs into one `heron_hvac` call per Space, and sums
the results. **It adds no physics.** Every number is computed by the existing, tested calculations:
`monthly_load` (or `cooling_load`), `heating_load`, `ventilation`, `supply_airflow`, `terminal_flows`.
One fact, one home: if the room method changes, it changes in [`heron_hvac.py`](../brain/heron_hvac.py)
alone.

It imports only the standard library, like the engine.

### 5.2 Block load

A building's or a zone's cooling peak is **not the sum of its rooms' peaks** - a west office peaks at
16:00 in one month, an east office at 09:00. `monthly_load` already gives each Space its load at every
hour of every month's design day; the runner adds them hour by hour and takes the largest sum. The report
shows both: the sum of peaks (what each room's terminal is sized to) and the block (what the plant is sized
to), with the month and hour of each.

### 5.3 The inputs, and where each comes from

Following [D-33](DECISIONS.md) and the owner's own rule that every design value says where it came from:

| Input | Comes from | If missing |
|---|---|---|
| Floor area, height, volume, faces, orientation, glass areas | The model (§4) | QA line; Space left out |
| Wall, roof, glass U; glass SHGC | The model's type properties | **Refused run** for that Space, listed - never defaulted (defect 1) |
| Outdoor design conditions | The project's design basis (D-111) or asked | Asked once |
| Indoor set point and RH | Asked once per project, editable per Space | Asked |
| People, lighting W/m², equipment W/m² | Asked **once per Space Type** (e.g. Office, Corridor), editable per Space in the Companion | Asked, with a standard's figure **offered** and named |
| Outdoor air | 62.1 rates for the Space Type, or his figure | Asked |
| Infiltration | His figure, ACH or L/s·m² | Asked |
| Safety factor | His figure, shown as its own line | None applied |

Every value in the run carries one label: **from the model**, **a named standard and table**, **your
instruction**, or **an assumption** - and the PDF prints the label beside the value.

### 5.4 Saved with the project

The take-off, the inputs and each run's results are kept beside the project's design basis (D-111) in
Heron's knowledge folder, so a run can be re-opened, compared with the last one ("what changed since
Tuesday"), and copied for a what-if.

---

## 6. The two gates

**Gate 1 - the geometry is right.** Before any load runs, the QA list is shown: Spaces not enclosed or
not placed, Spaces with no outside face (fine, but said), the sum of Space areas against the level's gross
area, a face with nothing behind it, a window whose host wall bounds no Space, any type missing U or SHGC,
no True North, no site location. The owner confirms, or fixes in Revit and reads again. The confirmation
is recorded with the run. **The site location is fixed in Revit by hand** (Manage > Location): nothing in
Heron sets it ([FRAGMENT-ISSUES 5b-322](FRAGMENT-ISSUES.md), recorded 2026-10-04, not built).

**Gate 2 - write only on his word.** Finalize is a `MODIFY` through `revit_change`'s own path ([D-108](DECISIONS.md)):
one undo entry, the Companion table is the preview Article 9 asks for, a stale row refuses the whole
Apply (Article 12c), and writes stay behind the Changes switch.

---

## 7. The Companion "Loads" panel

The owner's Companion scope ([40 §21.1](40-heron-companion.md)) allows tables for parameter changes and
schedule-style lists; this is both. The panel follows the existing pattern - a new section on the page,
a route in the server, a holder the MCP server fills - and keeps the Companion's own rules: localhost only,
no AI, **no BIM logic in the companion folder** (the calculation stays in `brain/`).

- **Inputs table** - one row per Space: Space Type, people, lighting, equipment, OA, set point - editable.
  Editing one Space Type's row can fill every Space of that type.
- **Results table** beside it - sensible, latent, total W, W/m², TR, heating W, supply L/s, OA L/s, ACH,
  peak month and hour - with zone and building totals and the block load.
- **QA list** from gate 1, always visible.
- **Buttons:** *Recalculate* (brain only, nothing in Revit changes), *What-if* (copy this run), *Report*
  (writes the PDF and the CSV), *Finalize to Revit* (gate 2).

The skill that ties the steps together - read, check, ask, calculate, show - is `space-airflow`'s next
version, or a new `building-loads` skill if the owner prefers one name per job.

---

## 8. The report

**No PDF goes in.** That skill reads house *drawings* in PDF because it has no model; most of its code is
that. Heron reads the Revit model directly (§4), so none of it is needed. **One PDF comes out**, and only
when he presses *Report*: the **load calculation sheet** - the document handed to a consultant or kept on
the project file to show how each Space's load was worked out.

- **Content:** project and model name; date; design conditions with sources; the standards in force
  (D-111); the QA list and the geometry confirmation; per-Space inputs with their source labels; per-Space
  results with the component breakdown (walls, roof, glass conduction, glass solar, people, lights,
  equipment, infiltration, OA); zone and block totals with peak month and hour; the take-off summary; what
  was refused and why; and Heron's disclaimer - a design aid from published methods, a peak estimate, not
  an hourly simulation like HAP; the engineer of record approves every value.
- **How the PDF is made:** Heron renders the report as one self-contained HTML page (`heron_render.py`
  already renders pages), then prints it to PDF with the **Microsoft Edge already on every Windows PC**
  in its headless print mode. No new Python package. Where no Edge or Chrome is found, the HTML page is
  delivered and the PDF is reported as not made - degrade, never break.
- **Where:** a folder beside the Revit model, named for the run.
- Exporting is a `PUBLISH` action under [D-107](DECISIONS.md) only when it writes from Revit; this report is
  written by the brain from its own numbers - §9 question 4 asks the owner to confirm it stays outside the
  Publish switch.

---

## 9. Owner questions - answered 2026-10-04

The owner answered the first and left the rest to Claude (*"you can decide the things"*). Each decision
below is Claude's unless marked his, and he can overturn any of them.

1. **Scope of version 1 - HIS ANSWER:** every commercial building - offices, schools and the rest - through
   Heron's own engine; houses stay with the separately installed skill. **Qatar first.** Another country
   ("this school in India") is kept possible and built later: `heron_hvac` already takes any site's own
   monthly design weather (`months`) where it holds no named set, so until a country's set is added Heron
   says it holds no design weather for that place and asks for it - never a guess. Testing waits for a model
   he will name.
2. **People, lights, equipment - decided:** typed once per Space Type in the Companion, editable per Space.
   Revit's own Space Type values are read-only through the API on Spaces, and typed values carry the
   "your instruction" label the report needs.
3. **What Finalize writes - decided:** Revit's own Space fields (Design Cooling Load, Design Heating Load,
   Specified Supply Airflow), through the existing `SET_PARAMETER_VALUES_BY_ID`; then the diffuser flows
   through `terminal_flows` and `SET_AIR_TERMINAL_FLOW`. No new write tool.
4. **The PDF - decided:** not a Publish action. It is written by the brain from Heron's own numbers, into
   the run's folder; nothing is exported from Revit.
5. **A second method - decided:** not now. Revisit after the first comparison with an engineer's HAP run.
6. **The defects in §1.2 - decided:** not reported. It would be a public post from his account; he can ask
   for it at any time.

---

## 10. Build phases

Each phase is a pull request on its own, merged on the owner's word, with the gates of
[heron-ship](../.claude/skills/heron-ship/SKILL.md) green and its evidence captured.

| Phase | Builds | Proof |
|---|---|---|
| **0** | This page; the owner's answers to §9 recorded as a decision | His approval |
| **1** | `REPORT_SPACE_ENVELOPE` - the take-off reader, all Revit releases | On his L-shaped office ("heron ai bulding", 15 Spaces, 5 zones): face areas against a hand take-off of one corner office and one inner office; the negative case a Space not enclosed. NEEDS-CHECKING group for §4.2's questions |
| **2** | The building runner in `brain/` - take-off + inputs → every Space, zone and block load; the QA gate | Tests with a hand-summed two-room building; the same office run against `heron_hvac` called room by room - identical to the watt |
| **3** | The Companion "Loads" panel - inputs, results, QA, Recalculate, What-if | The page tests' pattern; then on his PC, one input changed and recalculated |
| **4** | The report - HTML and PDF through Edge, and the take-off CSV | A report generated for the office; every number in it traced back by `heron_report.py`'s check |
| **5** | Finalize - the write-back to Spaces, then `terminal_flows` → `SET_AIR_TERMINAL_FLOW` | On a rolled-back run: written, read back through `READ_SPACE_LOADS` and `REPORT_SPACE_AIRFLOW`, undone in one Ctrl+Z |
| **6** | Later, each on his word: a 3D view of the faces calculated, heating with ground floors, a second method | - |

**What would make the loads trustworthy, not just consistent:** one office from his building run in HAP or
TRACE by an engineer, same inputs, and the difference explained ([41 §14](41-hvac-design.md)). Until then
every report says it is a design aid.

---

## 11. What is built - 2026-10-04

Built from [the plan](work-notes/plans/building-loads-2026-10-04.md) in one pull request, **not merged**,
**nothing run in Revit**. Every part is DRAFT; the proof is [Group CC](needs-checking/group-cc.md), on a
model the owner names. Where the build had to depart from the plan, the plan says so in its own text.

| Part | What it does | Its test | Status |
|---|---|---|---|
| `heron_hvac` answers carry `data` | `monthly_load` gives every hour of every month and the components at its peak as numbers; `heating_load` its loss; `supply_airflow` its flow. No new physics | [`tests/test_hvac.py`](../tests/test_hvac.py) section 9 | DRAFT |
| [`brain/heron_takeoff.py`](../brain/heron_takeoff.py) | Reads the take-off whole or refuses it whole; gate 1's checks; faces into engine surfaces, windows netted out, azimuths turned to true north in ONE place | [`tests/test_takeoff.py`](../tests/test_takeoff.py) | DRAFT |
| [`brain/heron_building_loads.py`](../brain/heron_building_loads.py) | Every Space through the room engine; the block load hour by hour beside the sum of peaks; questions with a standard's figure offered; source labels; runs kept per project; the rows Finalize writes, in the model's own units | [`tests/test_building_loads.py`](../tests/test_building_loads.py) | DRAFT |
| `REPORT_SPACE_ENVELOPE` ([FRG-MEP-058](../brain/fragments/report-space-envelope/fragment.yaml)) | The take-off: every Space's faces, openings, the types' U / SHGC / absorptance, the site and True North, the units shown. Every Space in the document - not the selection | compiles on 2020 to 2027; [Group CC](needs-checking/group-cc.md) | DRAFT |
| `revit_building_loads` (MCP tool) | Reads the take-off, asks or calculates, opens the Loads panel, tells the chat only the totals | `tests/test_building_loads.py` (through `heron_brain`) | DRAFT |
| The Companion's Loads panel | Inputs per Space type, results, zone and building totals, the model's checks, Recalculate, Report, Finalize, earlier runs | [`tests/test_companion.py`](../tests/test_companion.py) `test_loads`; the page checked in a browser at desktop width and 375 px | DRAFT |
| [`brain/heron_loads_report.py`](../brain/heron_loads_report.py) | The load calculation sheet: HTML, CSV, take-off CSV, and a PDF printed by Edge or Chrome when one is on the PC | [`tests/test_loads_report.py`](../tests/test_loads_report.py); a PDF printed by Edge on the owner's PC and read back | DRAFT |
| Finalize | The three Space fields through the table's own Apply, then the diffusers through `SET_AIR_TERMINAL_FLOW`, then read back. **Two undo entries** until one TransactionGroup spans both writes - [register row 5b-314](fragment-issues/section-5b-rows-176-200.md) | `tests/test_building_loads.py` (the rows and units); `tests/test_companion.py` (the order and the model guard) | DRAFT |
| [`space-airflow`](../brain/skills/space-airflow.yaml) skill, version 2 | Calculate first, review, then Finalize | `tests/test_skills.py` | DRAFT |

**Two things the real code said, and the plan was changed to match:** the fragment takes `doc` only,
because the add-in refuses a selection-bound fragment when nothing is selected before its code runs; and
Revit 2027 renamed the U-value parameter, so the fragment finds it by name at run time (Group CC row CC3).

---

## 12. The 3D view, the geometry gate, and what else was taken - asked 2026-10-04

**Asked by the owner after section 11 was built**, in his words: check the studied skill again, *"in the
companion app we need to see this 3D views also, all the settings in there"* - because a table of faces
is heavy to review and *"we cannot see something visually"* - take its engineering ideas into Heron
(*"not exactly copy paste, you can do like that"*), and get everything the calculation needs from the
Revit model rather than from PDF plans. Phase 6's 3D view (section 10) is brought forward on that word.

**Checked again:** the studied skill's public repository is at the same commit section 1 studied
(published 2026-10-01, nothing since), so section 1.2 stands as written. Its 3D viewer, its take-off
checks and its design notes were read again for what a Revit take-off can use. Nothing below is its code
or its wording; the mechanisms are re-authored in Heron's own terms (section 1.4).

### 12.1 The 3D view in the Loads panel

The Companion's Loads panel draws **the very faces the loads were worked out from** - every Space's
walls, roof, floor and partitions as REPORT_SPACE_ENVELOPE outlined them, and the windows and doors where
they sit - so the modeller can turn the building round and check the take-off before anything leaves
Heron.

| Setting | What it does |
|---|---|
| **Colour by** | Surface type, what is beyond each face, U-value, which way it faces, Space cooling W/m2, Space heating W/m2, Space status |
| **Space** | One Space alone, every other ghosted - also from the Space's name in the results table |
| **Show** | Each level; roofs and ceilings; floors; walls between Spaces; windows and doors; see-through; Space names |
| **Top / 3D / Fit / + / -** | Plan with project north up; the corner view; the whole building; closer, further. Drag turns it, right-drag or Shift-drag pans, the wheel or a pinch zooms |
| **Compass** | True North, turned by the model's own angle |
| **Click a face** | What it is, **what it counted as in the load**, its type, element id and link, area, U, SHGC, the way it faces, what is beyond it, and its Space's loads |

Two settings are Heron's own, because a model, not a drawing, is the source: **colour by the way it
faces** shows at a glance whether True North was read the right way round (Group CC row CC4), and
**colour by Space status** shows every refused Space in red with its reason a click away.

**It is a small renderer of Heron's own** (`mcp/companion/static/loads3d.js`), not a 3D library: the
Companion loads nothing from the internet (its README rule 1, its content security policy) and adds no
package. **Everything that means something is decided in `brain/heron_loads_view.py`** - the
categories, the colours, the legends, what each face counted as - and the page only projects and paints
(the Companion's rule 4). Model text reaches the screen as text.

### 12.2 Gate 1, built: "The take-off is right"

Section 6's first gate was designed and not built in section 11. It is now: the panel's **The take-off
is right** records that the modeller checked the take-off, **with the take-off's own fingerprint**, in
the run kept for the project. Until then the report is stamped **DRAFT - THE TAKE-OFF WAS NOT
CONFIRMED** and Finalize stays shut - and the brain itself refuses Finalize's rows for an unconfirmed
take-off, so the rule does not depend on the page. A Recalculate of the same geometry keeps the
confirmation; a model read again with any face changed needs it again.

### 12.3 Also taken, re-authored

- **Spaces on a level that share no wall** are a check - a gap between them, or a corridor, stair or
  shaft with no Space.
- **Glass by the way it faces** - glass area beside the outside wall it sits in, the share of each
  facing that is glass, and glass against floor area - in the panel and on the report, from the model's
  own windows.
- **The model, not plans, is the source of every size.** REPORT_SPACE_ENVELOPE now also gives every
  face's outline and every window and door's position and size - what the 3D view draws - and, when the
  modeller says the walls are linked, reads them **from the architect's link** (D-59's `includeLinks`,
  `linksSearched`; `include_links` on `revit_building_loads`). Without it a linked wall's face is
  "unknown" beyond and the checks say the links were not read.

**Not taken**, because the Revit model already answers it: reading PDF plans, scale calibration,
dimension chains, area reconciliation against a printed schedule. **Not taken** because it is a house's,
not an office's: below-grade window wells, bedroom egress, attic and crawlspace buffer zones (section 2).

### 12.4 Where it is checked

[`tests/test_loads_view.py`](../tests/test_loads_view.py) (an L-shaped building of three offices with
real outlines), `tests/test_companion.py` (`test_loads`), `tests/test_building_loads.py` and
`tests/test_loads_report.py` (the gate), and the page in a browser at desktop width and at 375 px:
turning, clicking a face, the colour modes, top view, one Space alone, confirming, and the report.
**Nothing here has met a real Revit**: [Group CC](needs-checking/group-cc.md) rows CC14 to CC17.

### 12.5 The review of 2026-10-04, and what it changed

A fresh reviewer read the whole branch before the pull request. Its findings, and what was done with each:

| Finding | What changed | Where it is held |
|---|---|---|
| **A slab between two storeys was read as "nothing beyond"** - the look past a face started half a foot beyond its underside, inside any thicker slab - and such a face was then counted as a wall to an unconditioned space: a third more cooling on the office below | The look starts past the element's own thickness (a floor's, roof's or ceiling's, else its box) and tries two feet further on. **A face Revit still finds nothing beyond is a question, once per element** - outside, unconditioned, conditioned or ground - and its Spaces wait for the answer. It is never guessed | `heron_takeoff.beyond`, `role`, `unknowns`; `needs()` asks `beyond:<element>`; Group CC rows CC18 and CC19 |
| The block load left out the outdoor air yet was called "what the plant is sized to" | Two blocks: the rooms' own, and **with the outdoor air at the coil**, hour by hour. No sentence says a block sizes plant | `_block`, the summary, the report, the panel |
| One unconditioned temperature served cooling and heating | `heating_unconditioned_temp_c` beside `unconditioned_temp_c` | `PROJECT_KEYS` |
| An outside door's type carries no absorptance in Revit, so every Space with one refused | Asked once, **`door_absorptance`**, offered from the sol-air table - only when such a door exists | `needs()`; CC21 |
| Every curtain panel went into every face of its wall, so a curtain wall across two Spaces refused both | A panel, window or door belongs to the face its box's middle projects onto | the reader; CC22 |
| Skylights were never read | Windows in a roof, or in a floor above, are read as skylights | the reader; CC20 |
| Faces Revit could not work out vanished, and the Space ran on internal gains | A placed Space with no faces is a FAIL | `qa()` |
| A window or door in an outside wall that sits in no Space's face was dropped | A finding names it | the reader; CC25 |
| Glass in a facade wall whose type is not Exterior gets no sun | A WARN tells the modeller to set the type's Function | `qa()` |
| A face bounded by an unread link said "type None" | It says the link was not read, and how to read it | `heron_takeoff.UNREAD_LINK` |
| The site the sun is worked out for was never shown, and a template's default city would run Doha weather with another city's sun | The site is said every run and printed on the sheet; **a site more than 150 km from the design weather's station is a FAIL** | `site_checks()`; CC23 |
| A project value that is not a number crashed the run; 1000 W/m2.K or RH 0.5 was taken | Every project value is range-checked and refused in words | `_checked_project()` |
| Recalculate never reads the model, yet the page showed its time as the read time; a load from an old take-off could be written after walls moved | The panel keeps the time the model was read. **Finalize reads the model again and refuses if its geometry changed** (the fingerprint leaves out the values Finalize writes), and checks every row against what Revit holds now | `_loads_finalize`; CC16 |
| Finalize always said two undo entries | It counts the entries it made: one, or two | `_loads_finalize`; CC24 |
| Per-Space values, the take-off kept with a run, the read-back on the page | Built: a Space's own people, lights, equipment, outdoor air and set points from its row; each run keeps the take-off it came from (once per fingerprint) and drops its hour-by-hour rows; the read-back after Finalize is on the page | `run()`, `save()`, `load_takeoff()`; the panel |

**One thing the review asked for was not built, on purpose: reconciling the Spaces' area against each
level's floors.** A model's floors are often drawn twice (a finish and a structural slab) and an MEP
model often has none of its own, so the check would warn on sound models. The separate-groups check and
the 3D view stand in for it.

**The smaller findings are left for later and listed in the pull request.** They include: people counted
with round-half-even; supply air sized at the peak-total hour, not the peak-sensible one; the `m3/h`
symbol; the report and the runs reading the live pin instead of the panel's model; the panel's poll size;
a name starting with `=` in a CSV; Spaces not filtered by phase or design option; the Space Type found by
its English name; and building area including refused Spaces.

### 12.6 The second review, of the 3D view and the fixes

The same reviewer then read everything after its first range. No critical finding. What it found, and
what was done:

| Finding | What changed |
|---|---|
| The page's "Checks on the model" were not the run's own - no site line, and a face already answered still listed as asked | The panel shows the run's own checks, with the answers and the site; before a run, the same two |
| A Space's own value cleared on the page came back from the kept run | A cleared value is sent as nothing, and nothing clears what was kept: the Space's type value applies again |
| A floor answered "outside" or "unconditioned" was counted as on the ground | Open below: conduction to the outdoor air, hour by hour, both seasons, no sun. Over an unconditioned space: the unconditioned temperatures. Only "ground" is the ground |
| A Space face nothing bounds was dropped, and a face with no element was blamed on links | The reader writes such a face, marked `bounded_by: nothing`, and marks an unread link as one; the Space is refused with the right sentence. A placed Space with no face above it is a FAIL |
| A curtain wall's frames needed a U-value of their own, which such types do not carry | The frames are counted at the area-weighted U of the panels in that face, with no sun, and the sheet says so |
| The compass worked True North out with a formula of its own | The brain sends the north direction made by the same rule as every facing |
| Finalize took each row's old value from the fresh read, so a load edited in Revit since the read would be overwritten | The rows carry the values Heron showed: an edit made in Revit refuses the whole write again (Article 12c). After a Finalize, what was written is what Heron holds |
| Changing an answer about what is beyond kept the confirmation | The confirmation covers the answers too |
| Confirming did not check the chat was still on the same model | It does, before anything is kept |
| Glass with no SHGC was shown refused where no SHGC is needed; three checks read what Revit found and not the answers | Both follow what each face counts as |
| The second look past a wall could see across a narrow shaft | The second look is for ceilings and floors only |
| The diffuser write counted as an undo entry when it changed nothing | It counts only when something changed |
| A wrong answer word was asked again with no word about why | The question says which answer was not one of the four |
| A face's direction was taken from the subface on trust | It is turned to point out of the Space's own solid when it does not |

**Left as minors, with the first review's:** an opening whose box middle is far from its face (deep
shading in a family, a door modelled open) is an orphan; the active view's phase decides what is beyond,
so Finalize from a view of another phase reads as "the model changed"; an earlier run cannot yet be
reopened with its own geometry on the page; a misspelt per-Space key is kept and printed; a skylight in
a sloped roof is drawn at its box's middle height.

### 12.7 The page and the sheet, after the first test (2026-10-04)

The owner calculated a three-room test building in Project2 and asked for five changes. Each is built
and tested, and docs/40 21.6 has the page's side:

| He asked | What changed |
|---|---|
| "Which load is it - AC or heating, or HAP?" | The panel and the sheet are titled **HVAC Load Calculation**: cooling (AC) and heating load per Space, ASHRAE method, a peak estimate and not an hourly simulation like HAP. The results group their columns under Cooling (AC) load, Heating load and Air |
| "I can give the location" | Report opens a Windows folder window where the project's last report went. The sheet goes straight into the folder chosen, the run's id in every file name, and the folder is kept for the project (`report_folder`). A folder that is not there is refused, never made; a kept folder that has since gone falls back to the usual place |
| "I cannot see it in here" | Open report and Open PDF open the sheet in the browser, through a key only the paired page holds; the sheet may run no script |
| One column, the results below | The inputs sit above the results, each the page's full width |
| The 3D view turns the wrong way | It turns the way the mouse moves, as Revit's orbit does |

The page also leads with the building's figures, which the brain now adds up itself so the page sums
nothing (mcp/companion README rule 4): the supply air and outdoor air of the Spaces calculated, the load
per square metre of the floor that WAS calculated, and how many Spaces were. The sheet opens with the
same figures as its Summary.

### 12.8 The first real Finalize (Project2, Revit 2024, 2026-10-05)

The owner calculated the three-room test building in Project2 (Revit 2024, session 18156), confirmed the
take-off and pressed **Finalize to Revit**. What it did, read back by hand:

- **The Spaces were written right.** All nine values - Design Cooling Load, Design Heating Load and
  Specified Supply Airflow for Office 1, Reception 2 and Office 3 - sat inside a 0.5 % band around what was
  calculated, each band matching exactly its own Space (SELECT_BY_NUMERIC_PARAMETER, internal units).
- **The read-back and the diffuser step asked the add-in for elements by typed ids**, through
  FILTER_ELEMENTS_BY_ID, which never accepts one ("elementIds ... cannot be typed"). The read-back was
  refused (`bad_request_value`, then `chain_empty`), and on a model with diffusers no flow would have
  been written. The tests had stood Revit in with something that took the ids.
- **Fixed.** The diffusers are handed over by category, once per level of the calculated Spaces, and the
  file of ids picks which are written - SET_AIR_TERMINAL_FLOW already leaves every other terminal alone.
  Proved on Project2: FILTER_ELEMENTS_BY_CATEGORY (Air Terminals, Level 1) handed over six, and a one-row
  file set one terminal's flow and read it back through its connector. The read-back reads the take-off
  again and sets each written value beside what Revit now prints (`read_back`), allowing Revit's own
  rounding; the diffusers through REPORT_SPACE_AIRFLOW on each level's Spaces.
- **READ_SPACE_LOADS could not have read them back anyway** on this model: it reads the design and the
  calculated figures in one guard, and the calculated ones throw until Revit's own loads analysis has
  run. Recorded as [FRAGMENT-ISSUES row 5b-323](fragment-issues/section-5b-rows-176-200.md); Finalize no
  longer uses it.
- **A family finding, not a Heron one:** the office's `TRG_SAD_T202_SupplyAirDiffuser_SquarePlaqueFaceType`
  ties its duct connector's flow to no parameter, so no tool can set its flow from outside - the tool says
  `noFlowParameter` and writes nothing. `TCM_SAD_Supply Diffuser - Rectangular Face Rectangular Neck Auto
  Sizing` does tie it, and took the flow.
- **The owner's addition: Finalize makes the Spaces schedule** - "HVAC Load Calculation - Spaces", with
  Number, Name, Level, Area and the three values written (`SCHEDULE_NAME`, `SCHEDULE_FIELDS`). Revit
  refuses a second schedule of the same name, and a second Finalize says the schedule is already there.
