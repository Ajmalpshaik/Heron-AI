<!--
Heron-Agent:  HERON-MEP-FPD-002
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 46 — Sprinkler hydraulics from the model: a Revit sprinkler system in, a checked hydraulic calculation out

> **Status: A DESIGN, with phase 1 built beside it (§12). Nothing in it is proven.** Asked 2026-10-04 by
> the owner, after the Loads panel ([44](44-building-loads-from-the-model.md)): *"same like that we need
> for firefighting also ... for sprinkler what we can do"*, then *"write the sprinkler panel design ...
> make the good plan ... and do it"*. Every fragment it adds starts DRAFT ([D-30](DECISIONS.md)). A
> hydraulic calculation is life safety: the fire consultant approves every value and the authority -
> QCDD on a Qatar project - approves the design.

**In Revit words.** Today you can ask Heron *"what pressure does the riser need for these ten
sprinklers"*, and you have to type in every pipe, every length and every head. This feature lets you say
*"run the hydraulics on this sprinkler system"*. Heron reads the system from Revit - every pipe with its
length and inside diameter, every fitting, every valve and every sprinkler with its K-factor - shows it to
you in the Companion in 3D, lets you confirm the K-factors and pick the remote area, solves it with its
fire engine, and gives you a hydraulic calculation sheet as a PDF.

---

## 1. What exists, and what is missing

| | Today |
|---|---|
| **The solver** | Built. `heron_fire`'s `hydraulic` ([42 §9](42-fire-protection-design.md)) solves a tree or a looped grid by Newton's method: Hazen-Williams in every pipe, q = K√p at every operating head, elevation, the governing head, the source pressure, velocity and pressure checks, a flow test's supply curve. Up to 400 nodes and 600 pipes |
| **The design criteria** | Built. Density, design area, minimum pressure, C factor, hose allowance - each ASKED with NFPA 13's (or EN 12845's, or FM Global's) figure offered, never applied (D-33). The project's sprinkler standard is asked once and kept (D-111) |
| **The network** | **Missing.** The solver takes nodes and pipes typed in by hand. Nothing reads them from Revit. `READ_MEP_SYSTEM` gives a system's name; `REPORT_CONNECTOR_LOADS` gives a connector's values; neither gives which pipe joins which |
| **A place to see it and check it** | **Missing.** No panel, no 3D view, no report |

So this feature is **a reader and a panel, not new physics**. One fact, one home: every number about
water is worked out in `heron_fire.py`, as every number about air is in `heron_hvac.py`.

---

## 2. What the owner gets - the whole flow

1. He says *"run the hydraulics on this sprinkler system"*, with the system selected or named.
2. Heron reads the system (`REPORT_SPRINKLER_NETWORK`, which changes nothing) and turns it into the
   solver's nodes and pipes.
3. **Model checks** come first: heads with no K-factor, pipes with no diameter, open pipe ends, an
   element that belongs to another system, more than one supply point.
4. **Questions** - only what the model cannot say: the design density, the minimum pressure, the C
   factor, the area each head protects, the hose allowance, the flow test, and the equivalent length of
   each kind and size of fitting the system has. Each with the standard's figure offered.
5. The Companion's **Sprinkler panel** opens: the K-factor of each sprinkler type, the fittings table, the
   criteria, the list of heads with the **remote area** ticked, a 3D view of the very pipes the solve
   used, and the results.
6. **Suggest the remote area** ticks the heads hydraulically farthest from the source, as many as the
   design area holds. The modeller changes the ticks and confirms.
7. **Calculate** runs the solver. Results: the governing head, the demand at the source, every head's
   flow and pressure, every pipe's flow, velocity and loss, the supply margin.
8. **Report** writes the hydraulic calculation sheet - HTML, CSV and, where Edge or Chrome is found, PDF.
9. **Nothing is written to Revit in phase 1** (§9).

---

## 3. Reading the model - REPORT_SPRINKLER_NETWORK

One new READ fragment, one implementation for Revit 2020 to 2027. It returns the network as one JSON
string, `networkJson` (format 1), for the same reason REPORT_SPACE_ENVELOPE does - a list crosses as three
names ([FRAGMENT-ISSUES 5b-195](FRAGMENT-ISSUES.md)).

**Which system.** The piping system of the elements selected; else the one named in `systemName`; else,
when the model has exactly one fire protection system, that one; else it reads none and lists the fire
protection systems there are, so the chat can ask which.

**What it reads, and nothing more.** For every element in the system:

| Element | Read |
|---|---|
| Pipe | id, inside diameter (m), nominal diameter (m), length (m), pipe type name |
| Fitting | id, its part type as Revit names it (Elbow, Tee, Cross, Transition, Union, Cap...), its angle where Revit holds one, family and type name |
| Pipe accessory (valve) | id, family and type name |
| Sprinkler | id, family and type name, the K-factor **as Revit holds it** (§3.1), its point, its level, the Space or Room it sits in where the model has one |
| Every element | each connector's point (m, model coordinates) and **which connectors it is joined to** (element id and connector id) |

The system's own **base equipment**, when it has one, is named as the likely source.

**It calculates nothing and joins nothing.** Which connectors meet is Revit's answer
(`Connector.AllRefs`); turning that into nodes is the brain's (§4). A connector joined to nothing is
written as joined to nothing - an open end is a finding, never quietly closed.

### 3.1 The K-factor - read, shown, confirmed

Revit holds a sprinkler's K-factor on its connector, and the unit it is stored in is not documented to a
standard Heron can rely on. **So no K-factor is converted or trusted on its own.** The fragment reports,
per sprinkler TYPE, the connector's raw value and any type parameter whose name holds "K" and "Factor" as
Revit displays it. The panel shows them, and **the modeller types or confirms the metric K (L/min/bar^½)
once per type**. Until every operating head's type has a confirmed K, nothing is calculated. Sprinkler
types in a system are few - this is a minute's work and it removes a whole class of silent error.

---

## 4. From model to network - the brain's take-off

`brain/heron_sprinkler_takeoff.py`, standard library only.

- **Nodes.** Connectors Revit says are joined are one point. A fitting, an accessory or a sprinkler is
  one node (all its connectors joined through it). A pipe is an edge between the nodes at its two ends.
- **Elevation** of a node is its connector's z, in metres.
- **Fittings and valves carry no length of their own.** Their friction is an **equivalent length**, from
  the table the modeller confirms (§5): one row per fitting kind and nominal size the system holds. A
  fitting's equivalent length is added to **the smallest pipe that meets it** - the branch side of a tee,
  the outlet of a reducer. That is the conservative choice where the flow direction is not yet known, and
  it is written on the report.
- **The source** is the system's base equipment if it has one; else the open pipe end the modeller picks
  from the panel. Never guessed.
- **Model checks (gate 1, §6):**
  - **FAIL** - a sprinkler with no confirmed K; a pipe with no inside diameter; the network is not one
    piece (an island of pipes the source cannot reach); no source; more than 400 nodes or 600 pipes once
    joined (the solver's limit - then a design area's part of the system is read, not the whole building).
  - **WARN** - an open pipe end that is not the source (a capped end Revit has no cap on, or a break);
    a sprinkler that is not connected; an element on another system; a fitting kind the chart has no name
    for.
  - **INFO** - counts by element kind, total pipe length by size.

Every number carries its unit in its key name (`_m`, `_mm`, `_lpm`, `_bar`), as in the loads take-off.

---

## 5. Calculating - the runner

`brain/heron_sprinkler_run.py` builds one input for `heron_fire`'s `hydraulic` and runs it. **It adds no
physics.** What it adds:

- **The project's criteria, asked once and kept with the project:** design density, design area,
  area per sprinkler, minimum pressure, C factor, hose allowance, maximum velocity, the flow test, the
  device losses (alarm valve, backflow preventer), the safety margin. Each is the modeller's, labelled
  `instruction`; the figure offered beside the question is labelled with its table (D-33).
- **The fittings table** - one row per kind and size; the chart's figure offered where Heron holds it,
  asked where it does not. Never filled in from memory ([42 §10](42-fire-protection-design.md)).
- **The remote area suggestion** - the heads farthest from the source along the pipe, by friction-weighted
  path (the sum of length × 1/d^4.87 to each head, which is what Hazen-Williams makes expensive), as many as
  `ceil(design area / area per sprinkler)`. It is a **suggestion**: the modeller ticks and confirms. The
  NFPA 13 rectangle (1.2√A along the branch lines) is the modeller's to check; `design_area` in the fire
  engine reports it.
- **Every head not ticked is non-operating** - in the network, flowing nothing.
- **The run is kept** with the project, like a loads run, so an earlier run can be reopened.

---

## 6. The gates

**Gate 1 - the network is right.** The 3D view and the model checks are on the panel. **The network is
right** records the modeller's check with the network's fingerprint (geometry, sizes, K-factors). Until
then the report is stamped **DRAFT - THE NETWORK WAS NOT CONFIRMED**.

**Gate 2 - the remote area is the modeller's.** The ticks are the modeller's choice and the report says
which heads they were and that Heron's suggestion was or was not changed.

**No gate 3 in phase 1** - nothing is written to Revit (§9).

---

## 7. The Companion "Sprinkler" panel

The Companion's rules hold ([40](40-heron-companion.md), its README): localhost only, no AI, **no BIM
logic in the companion folder** - the panel holds data and calls hooks the MCP server sets.

- **Header:** system name, element counts, when the model was read, Gate 1 state.
- **Model checks** - FAIL, WARN, INFO, always visible.
- **Sprinkler types** - type name, what Revit holds, *K metric* (editable), heads of that type.
- **Fittings** - kind, size, count, *equivalent length m* (editable, the chart's figure offered).
- **Criteria** - density, area per sprinkler, design area, minimum pressure, C, hose, max velocity,
  flow test, device loss, margin - each with its offered figure as the field's hint.
- **Remote area** - every head with a tick; *Suggest* fills the ticks; the count beside the design area's.
- **Results** - governing head, demand, pressure at the source, supply margin; the heads table; the pipes
  table; the checks; and the supply curve against the demand point drawn as a small chart.
- **3D view** - the pipes as lines and the heads as dots, from the network the solve used. *Colour by:*
  pipe size, velocity, flow, pressure at the head, operating or not, check status. Click a pipe or a head:
  its id, size, length, flow, velocity, loss, pressure. Top, 3D and Fit.
- **Buttons:** *Calculate* (brain only), *Suggest remote area*, *The network is right* (gate 1),
  *Report*.

---

## 8. The report

The hydraulic calculation sheet: project, model, system, date; the standard in force (D-111); the
criteria with their source labels; the model checks and the gate 1 confirmation; the fittings table; the
remote area's heads; the summary (demand, source pressure, hose, total, supply margin); every operating
head; every pipe in the flow direction; the checks; the method lines; the assumptions; the disclaimer.
HTML first, CSV of heads and pipes, PDF by headless Edge or Chrome as the loads report does (it reuses
that printer - one home). Written by the brain from its own numbers; nothing is exported from Revit.

---

## 9. What is not in phase 1, and why

| Not built | Why |
|---|---|
| **Writing to Revit** - flows and pressures into the heads, bigger pipes | Heads carry no standard parameter for a design flow or pressure, so it needs shared parameters the project chooses; resizing a pipe is a design change. Phase 2, through `SET_PARAMETER_VALUES_BY_ID` and `SET_MEP_SIZE`, behind the Changes switch, one undo entry, Article 12c |
| **The remote area drawn in Revit** (a filled region or a view filter) | Phase 2, with the write-back |
| **Coverage and spacing per room on the panel** | `sprinkler_spacing` and `coverage_check` exist; they need each room's outline. Phase 3, reading the Spaces or Rooms the heads sit in |
| **The 1.2√A rectangle found automatically** | The suggestion is by hydraulic distance; the rectangle is checked by the modeller. Phase 3 |
| **Standpipes, hose reels, pumps from the model** | The same reader serves them; their panels are phase 4 |
| **A comparison against a listed hydraulic program** | The proof (§11). Needs the owner's model and a calculation from one |

---

## 10. Decisions taken here, each the owner's to overturn

1. **Phase 1 is read and calculate only.** No write; the report is the deliverable.
2. **K is confirmed per type, never converted from Revit's stored value** (§3.1).
3. **Fitting equivalent lengths go on the smallest pipe that meets the fitting** (§4), written on the report.
4. **The remote area is suggested by friction-weighted path, then the modeller's** (§5, gate 2).
5. **One system per run.** Two systems are two runs.
6. **No decision number is spent.** It follows the Loads panel's pattern, which [D-108](DECISIONS.md)
   and [D-109](DECISIONS.md) already allow; it opens no new door to Revit.

---

## 11. What would prove it

- **REPORT_SPRINKLER_NETWORK** on a named model: one wet system, every pipe and head counted against the
  system browser; a negative case (a model with no fire protection system) returns no network and lists
  none. NEEDS-CHECKING.
- **The solve** against a listed hydraulic program's calculation of the same remote area on the same
  model: demand and source pressure within 2 %. Until then the sheet says it is not a listed program.
- The arithmetic is already held by `tests/test_fire.py`; the take-off, the runner and the report by
  their own suites.

---

## 12. What is built - phase 1 (2026-10-04)

The plan is [`docs/work-notes/plans/sprinkler-panel-2026-10-04.md`](work-notes/plans/sprinkler-panel-2026-10-04.md).
It was written, then reviewed by an independent read of the code it builds on, which found 20 defects;
every one was taken, and its review log says what each changed. The main changes to this design:

- **K is `AssignedKCoefficient`,** not the connector's calculated coefficient (§3.1 reads it raw, as before).
- **The elements are three sets joined** - the pipe network, the terminals and the base equipment (§3).
- **A tap or a spud splits its pipe** where it joins, the length shared by where it sits (§4).
- **A fitting's centre-to-end length is added** to the pipe meeting it, because Revit measures a pipe cut
  to cut; leaving it out erred small (§4).
- **Dead-end branches with no head in the remote area are left out of the solve** - exact, they carry no
  water - so a whole floor fits the solver's 400-point limit (§4).
- **The project's sprinkler standard is asked once and kept** (D-111), and the criteria's offers come
  from the engine's own offer text (`heron_fire.hydraulic_offers`).
- **A fitting's offer is the chart's figure adjusted to the pipe's bore and C** by the engine's own
  `equivalent_length`, or the chart's cell labelled for what it is.

| Part | What it is | Its test | Status |
|---|---|---|---|
| `REPORT_SPRINKLER_NETWORK` (FRG-MEP-059) | The read, §3 | `tools/check-fragments-compile.py` (2020, 2024, 2027 compiled here); cases in its `tests/` | DRAFT - [NEEDS-CHECKING group CE](needs-checking/group-ce.md) |
| [`heron_sprinkler_takeoff.py`](../brain/heron_sprinkler_takeoff.py) | Nodes, segments, fittings rows, checks, pruning, fingerprint | `tests/test_sprinkler_takeoff.py` | DRAFT |
| [`heron_sprinkler_run.py`](../brain/heron_sprinkler_run.py) | Questions, suggestion, the run, gate 1, kept runs | `tests/test_sprinkler_run.py` - the same answer as `hydraulic` by hand, and pruning exact | DRAFT |
| [`heron_sprinkler_view.py`](../brain/heron_sprinkler_view.py), [`heron_sprinkler_report.py`](../brain/heron_sprinkler_report.py) | The 3D data; the sheet | `tests/test_sprinkler_view.py` | DRAFT |
| `heron_fire.calc_hydraulic` | Now also gives its numbers as `data`, each pipe row with its `index` | `tests/test_fire.py` section 10 | unchanged method |
| `revit_sprinkler_hydraulics` (MCP tool), the Sprinkler panel | §7 | `tests/test_companion.py` | DRAFT |

**Seen in a browser** (headless Chromium, on the test network of `tests/test_sprinkler_takeoff.py`, not
a model): the panel with its questions, then solved with its figures, checks, tables and 3D view coloured
by velocity, with no script error. **Not run in Revit** - group CE is the next step.

---

## 13. Phase 1b - the fire water and the spacing, asked 2026-10-05

**Asked by the owner** after phase 1: of what was left for firefighting, *"1 and 2 - complete that
first"* - (1) the standpipes, hose reels, fire pump and tank, and (2) the coverage and spacing of the
heads per room. Both are in the same panel: they are the same system's questions, and one sheet is
what a consultant is handed.

### 13.1 Fire water - what the source must give, and how much water

The engine already calculates each part on its own ([42 §9](42-fire-protection-design.md)):
`standpipe`, `hose_reels`, `water_storage`, `fire_pump`, `water_supply`. What is missing is putting
them together with the sprinkler demand the panel has just solved. A new **Fire water** section of the
panel does that:

- **Which systems the source serves** - standpipes and hose reels are each *included* or not by the
  modeller's tick. Nothing is included by default, and the sheet says, for each, **"not included by
  the modeller"** - never silence.
- **What runs at the same time as the sprinklers** - for each included system, a tick. The total flow
  at the source is the sprinkler demand (with its hose allowance) plus every system ticked; the
  pressure the source must give is **the highest any included demand needs there**. That second rule
  is a simplification - each system's own path is not solved together with the others - and the sheet
  says so in words.
- **Each part's inputs are the engine's own questions**, asked only for the parts included, each with
  the standard's figure offered (D-33). The page holds no list of its own: the fields come from running
  each calculation on nothing.
- **The tank** - `water_storage` with the sprinkler flow, the hose allowance and each simultaneous
  system's flow over its own duration, which is asked.
- **The pump** - `fire_pump` with the pump's data-sheet points, checked against the total demand at
  the highest pressure.
- **The supply** - with standpipes or hose reels included, the flow test is checked again against the
  total, through `water_supply`.

The fire water answers never block the sprinkler solve: they come after it, and a missing pump curve is
a question in its own section.

### 13.2 Spacing and coverage per Space

The engine's `sprinkler_spacing` already checks a drawn layout as NFPA 13 measures it - each head's S,
L, area and wall distance - from a room outline and the heads' positions. What is missing is the room
outline and the heads from the model, room by room.

- **The read** - REPORT_SPRINKLER_NETWORK also gives, for every MEP Space a head of the system sits in,
  its outline in plan (metres, model coordinates) and its level, and each head carries its Space's id.
  Spaces, not Rooms: Spaces are in the MEP model, where the sprinklers are. **A head in no Space is
  listed, never checked**.
- **Which way the branch lines run** - read from the model's own pipes: the length-weighted direction
  of the horizontal pipes in the Space. Shown with what it was read from, and the modeller can change
  it. The layout is turned by that angle before the check, so a building at an angle is measured along
  its own branch lines.
- **The hazard class of each Space** - the engineer's, asked per Space, never assumed. **The limits of
  each class** - most spacing, most area, most wall distance and the optional minimums - asked once per
  class, with the standard's figures for that class offered.
- **Each Space's result** - every head OK or FAIL with the reason, and the farthest point from any head.
  The 3D view gets a **Spacing** colour, and the Space outlines are drawn.
- **A Space with no hazard given is "not checked"**, said on the panel and the sheet, never "OK".

### 13.3 What is still not here

Obstructions to spray (the beam rule) per head; heads above a ceiling; sidewall and extended-coverage
heads, which measure differently; standpipe and hose reel outlets read from the model; a standpipe-only
building with no sprinkler system. Each stays a question for a later phase.

The plan is [`docs/work-notes/plans/fire-water-and-spacing-2026-10-05.md`](work-notes/plans/fire-water-and-spacing-2026-10-05.md).

### 13.4 What is built - phase 1b (2026-10-05)

| Part | What it is | Its test |
|---|---|---|
| `heron_fire` | `data` on `standpipe`, `hose_reels`, `water_storage`, `fire_pump`, `water_supply` and `sprinkler_spacing`; `spacing_offers()`; `fields()` - a calculation's inputs, derived by running it on nothing | `tests/test_fire.py` section 11 |
| REPORT_SPRINKLER_NETWORK | Each head's `space_id`; `spaces` with each one's outline | compiled on 2020, 2024 and 2027 here; [group CE](needs-checking/group-ce.md) CE8 |
| [`heron_sprinkler_spacing.py`](../brain/heron_sprinkler_spacing.py) | Spacing per Space, the branch angle from the pipes, the layout turned to it | `tests/test_sprinkler_spacing.py` |
| [`heron_fire_water.py`](../brain/heron_fire_water.py) | The fire water parts put together with the sprinkler demand | `tests/test_sprinkler_spacing.py` |
| The run, the sheet, the 3D view, the page | Both sections; a Spacing colour and the Space outlines | `tests/test_sprinkler_run.py`, `tests/test_sprinkler_view.py`, `tests/test_companion.py` |

**Seen in a browser** on a test office turned 20 degrees (not a model): both sections, the outline and
the Spacing colour, and Calculate sent from the page and answered, with no script error. **Not run in
Revit** - CE8 to CE10.
