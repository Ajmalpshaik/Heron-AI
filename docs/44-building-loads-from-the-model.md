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
is recorded with the run.

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
