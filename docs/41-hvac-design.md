# 41 — HVAC design: what Heron works out before a duct is drawn

> **Status: BUILT, NOT PROVEN.** Written 2026-10-02, on the owner's request, with the engine it
> describes: [`brain/heron_hvac.py`](../brain/heron_hvac.py) and
> [`brain/heron_psychro.py`](../brain/heron_psychro.py), served as the `heron_hvac` MCP tool, owned by
> `HERON-MEP-HVD-001` in the [agent registry](28-agent-registry.md). **What is proven is arithmetic**:
> [`tests/test_hvac.py`](../tests/test_hvac.py) holds every calculation to published values, worked
> examples and hand sums. **Nothing here has been checked against a real project or a HAP run** - §14
> says what that would take. A design value is the engineer of record's, always.

---

## 0. In Revit words

Heron could already **model** ductwork - draw a run, fit its joints, size it from a velocity, set a
diffuser's flow. It could not say what any of those numbers **should be**. Now it can work them out:

| You ask | Heron works out | Then, in Revit |
|---|---|---|
| *"How much sun is on this west wall at 3 pm?"* | clear-sky irradiance on the surface, hour by hour, from the site's own sky (`solar`) | the figure goes into `cooling_load` |
| *"How much air does this office need?"* | the room's cooling load and its supply airflow (`cooling_load`, `supply_airflow`) | `SET_AIR_TERMINAL_FLOW` per diffuser, after `terminal_flows` splits it |
| *"Which month is worst, and at what hour?"* | the room's load on every month's design day, hour by hour, with the peak month and hour and the lowest month (`monthly_load`) | `READ_SPACE_LOADS` reads Revit's own figure to compare |
| *"Fresh air for this floor?"* | ASHRAE 62.1 outdoor air, zone by zone (`ventilation`) | the figure goes on the Space; `REPORT_SPACE_AIRFLOW` reads it back |
| *"How many diffusers, and where?"* | a grid, each one's flow, and the throw to look for (`diffuser_layout`) | `PLACE_FAMILY_INSTANCES` at the points it lists |
| *"Which neck size?"* | the smallest neck under a velocity, or the catalogue row under an NC (`diffuser_select`) | `CHANGE_ELEMENT_TYPE` to that size's type, then `SET_AIR_TERMINAL_FLOW` |
| *"Size this duct for 800 L/s at 1 Pa/m"* | equal friction, velocity, or both, snapped UP to your sizes (`duct_size`) | `SET_MEP_SIZE`, one call per size |
| *"What pressure does the fan need?"* | the index run's total pressure (`duct_pressure`), then the power (`fan_power`) | `REPORT_MEP_PRESSURE_DROP` reads Revit's own figure to compare |
| *"Chilled water for this AHU?"* | the coil load (`coil_load`), the water flow (`chw_flow`), the pipe (`pipe_size`) | `SET_MEP_SIZE` on the pipe |
| *"Which FCU, or which split, for this room?"* | the first unit in the maker's own catalogue that covers the total load, the sensible load and the airflow, and how far over it is (`unit_select`) | `CHANGE_ELEMENT_TYPE` to that model's type |
| *"500 cfm in L/s?"* | `convert` - exact factors, every HVAC unit | - |

`python brain/heron_hvac.py` lists every calculation and what it needs. That list is **derived** - each
calculation is run on nothing and reports what it asked for - so it cannot drift from the code.

**Two things it will never do.** It never fills in a design value you did not give (§3). And it never
changes the model: an answer NAMES the fragment that would put it into Revit, and running that fragment
is `revit_change`'s job, behind the Changes switch, exactly as before.

---

## 1. Where it sits

```text
modeller ── Claude Code ── heron_hvac (MCP, READ, no bridge operation)
                               │
                       mcp/server/heron_brain.py  hvac()        one audit line, never the inputs
                               │
                       brain/heron_hvac.py         the calculations, the references, the answer
                       brain/heron_psychro.py      moist air and water - physics only
                       brain/heron_designbasis.py  a project's design basis, kept once said (D-111)
```

| | |
|---|---|
| Layer | `brain/` - Python. The engine's two files import nothing outside Python itself, and [`tests/test_hvac.py`](../tests/test_hvac.py) parses both to hold that; the project memory borrows `heron_scope`'s knowledge folder and project key |
| Shared | the answer machinery - reading inputs, refusing, offering a cited figure, rendering - serves the fire protection engine too ([42](42-fire-protection-design.md)): `run`, `catalogue`, `describe` and the reference helpers take another engine's registry and tables, defaulting to this one's, and both go through one brain seam |
| Risk | **READ.** Arithmetic over what it is handed - no model, no network. The one thing kept is a project's governing standards once the modeller has said them ([D-111](decisions/D-111.md)): one small file per project in Heron's own knowledge folder, never the model |
| Agent | `HERON-MEP-HVD-001`, the first row of a new department, *MEP Design Engineering* - Revit Engineering acts ON a model, this works out what goes INTO one |
| Audit | `design.hvac`, one line per call: the calculation and how it ended. **Never the inputs** - a people count or a client's design figure is project information, and the trail is never pruned |
| Refusals | recorded with the codes [`heron_gaps.py`](../brain/heron_gaps.py) classifies: `needs_request_values` (asked), `unusable_request_values` (refused), `unknown_op` (no such calculation) - all three are correct refusals, not gaps |

---

## 2. Why a new agent, and the verdict that was set aside

[`HERON-AHR-WFP-015`](../brain/heron_workforce.py), the agent that says *no* before anything is hired,
was asked first. Its verdict was **`ALREADY_A_CAPABILITY`: `SET_AIR_TERMINAL_FLOW`**, at 0.50 word
overlap. That fragment's own purpose says *"It does not work out what the flow should be. Splitting a
space's airflow across its terminals is the caller's arithmetic"* - so the overlap is the word *flow*,
not the job. The verdict is recorded here and set aside; the planner itself reported the real question
as **NOT JUDGED** for want of a model to ask.

No neighbour could absorb it either. `HERON-REVIT-SYS-030` owns duct systems *in Revit*; this reads no
Revit. The Standards rows cite clauses from documents a company ingests; this computes.

---

## 3. The rule that shapes every answer - D-33

[D-33](decisions/D-33.md): **Heron never supplies a value the user did not give.** For a design engine
that is the whole shape of the thing:

- **A design criterion is an input, every time.** Friction rate, velocity limit, supply temperature,
  people, outdoor-air rate, Ez, fan efficiency, neck sizes, pipe bores. Missing one, a calculation
  computes **nothing** and answers *ASK THE MODELLER* with the reason.
- **A standard's figure is OFFERED, never applied.** Name a zone's occupancy and 62.1's rate for it
  appears **beside the question** - *"ANSI/ASHRAE 62.1-2022 Table 6-1 gives 2.5 L/s per person for
  office space - offer it to the modeller, do not assume it"*. The calculation still waits. That is
  [D-83](decisions/D-83.md)'s *"do you want the same here?"*, applied to a standard instead of a
  project.
- **A factor that would REDUCE a load is not invented.** No use factor given - all the lights are on.
  No safety factor - none added. No interior blinds credited. Each answer lists these under **NOT GIVEN,
  SO NOT APPLIED**, so an omission errs toward the larger number and says so.
- **What is built in is what no designer chooses** - D-33's own boundary, *"a technical choice is not
  an assumption"*. Constants of nature and definitions (the Hyland-Wexler coefficients, the exact
  foot), and the constants that belong to a published METHOD (Colebrook's 3.7 and 2.51, Huebscher's
  1.30, the 0.25 m/s that defines a T50 throw, the 3.7 K long-wave term for a roof that ASHRAE's 2017
  workbook computes - §12 row 8 - and the design day's hourly fractions of §4.4). Every answer names
  its method and sources so the choice can be checked.
- **The owner's own figures are the owner's answers, kept as decisions.** A supply diffuser neck in an
  NC/RC 30 room is held to **2.5 m/s** ([D-110](decisions/D-110.md)) - applied only at that criterion,
  named as the office's own figure every time, and given way to by any figure the modeller states.
- **A project's governing standards are asked once, and kept for that project**
  ([D-111](decisions/D-111.md)) - §11 says which four, and what each one changes.
- **The air is asked for too.** *"Standard air"* (ASHRAE's 1.204 kg/m3, 20 C, 101.325 kPa) or a
  temperature **and** an altitude - an airflow is a different mass of air in Doha in August than in a
  test lab, and that is a design condition.

A key nobody reads is named **IGNORED** - `flow_lps` is not quietly dropped while `flow_ls` is asked for.

---

## 4. Loads - what HAP does, what ASHRAE prescribes, what Heron does

### 4.1 Carrier HAP and the hourly methods

Carrier's **Hourly Analysis Program** sizes systems from hour-by-hour design days - one 24-hour cooling
design day for each month, from ASHRAE design weather - and reports at three levels: the **space**,
the **zone** (where zone airflow, reheat and terminals are sized - its *Zone Sizing Summary*) and the
**air system** (central coils and fans, with ventilation and fan heat added at the system's own
coincident peak - its *Air System Sizing Summary*). HAP 4.x calculated with ASHRAE's **Transfer
Function Method**; HAP v6 moved to ASHRAE's **Heat Balance** method on the EnergyPlus engine. Both
come from Carrier's pages read only through search summaries - carrier.com was not reachable - and
how HAP sizes airflow or applies 62.1 internally could not be read at all.

What the hourly methods share: each heat gain is split into a **convective** part, which is load at
once, and a **radiant** part, absorbed by walls, floor and furniture and released hours later. That
delay is why a west office peaks in the late afternoon, and why an hourly peak is **lower** than the
sum of every component's own peak. ASHRAE's current simplified method is the **Radiant Time Series**
(Fundamentals Ch. 18), derived from the heat balance: a conduction time series turns each wall's
sol-air heat input into heat gain, a radiant time series turns each radiant gain into load. ASHRAE's
own 2017 example workbooks show it on one room: a window gaining **3,416 Btu/h** at 15:00 is a cooling
load of **2,612 Btu/h** at that hour - the gap is the storage the method models.

### 4.2 What Heron does instead, and says every time

`cooling_load` is a **peak component estimate**:

| Component | Equation | Inputs it asks for |
|---|---|---|
| Wall, roof | U.A.(te - troom), te the sol-air temperature to + (a/ho).E - e.dR/ho, the long-wave term 0 for a wall and 3.7 K for a roof; or U.A.dT with a stated equivalent dT; or outdoor air temperature where `shaded: true` | area, U, and ONE of irradiance (from `solar`) with absorptance/ho, an equivalent dT, or shaded |
| Glass | conduction U.A.(to - troom), plus solar A.SHGC.E.IAC | area, U, SHGC, peak irradiance; IAC only if blinds are credited |
| Partition | U.A.(tadjacent - troom) | area, U, the temperature beyond |
| People | count x sensible and latent per person | all three |
| Lights, equipment | W (or W/m2 x area) x use factor x allowance | the power; factors only if given |
| Infiltration | outdoor air brought to room condition, by mass | L/s or ACH, and both humidities |
| Outdoor air at the coil | the same, added to the COIL load, not the room's | L/s |

Every component is taken at **its own peak** and summed, with no thermal storage and no lag - so for the
same inputs it is larger than an hourly method's peak, never smaller. **It is the right tool for
checking a figure and the wrong one for selecting a chiller.** Every load answer carries that sentence
(`NOT_HAP` in the code), and the test fails if it stops doing so.

From the room's sensible load and a supply temperature it gives the **supply airflow**, by mass and then
as a volume at the supply temperature - and from the latent load and the room humidity, the **supply
humidity the coil must reach**, as a dew point. In Doha that second number is often the binding one: a
room can be cooled to temperature by air too wet to hold it at humidity.

The air-side constants for standard air are 1.23, 3010 and 1.20 W per L/s for sensible, latent and
total - the figures quoted from Fundamentals Ch. 18, and the SI forms of the 1.10 and 4840 that
ASHRAE's own 2017 load-calculation workbook uses (converted exactly: 1.2295 and 3005.6; the I-P total
factor 4.5 gives 1.2014). **No ASHRAE page printing the SI figures could be read in this work** - they
are checked by that conversion, not against the text. Given an altitude and the air's state instead,
Heron works from the air's own density and enthalpy.

**In Doha the peak dry bulb is dry air.** July's monthly 0.4 % condition, 45.7 C with a 22.2 C wet bulb,
holds 7.1 g of water per kg of air - less than a 23 C, 50 % room's 8.8. Its outdoor-air latent load is
**negative**, so a coil sized at the peak dry bulb is undersized for moisture. Latent and coil loads
need the site's **dehumidification** design condition, and `cooling_load` says so whenever the outdoor
air it is given is drier than the room.

`heating_load` is conduction and infiltration with **no credit** for people, lights or sun, as a design
heat loss takes none - which is exactly how ASHRAE's own 2017 example computes it.

### 4.3 The sun - built, and checked against ASHRAE's own numbers

`solar` gives the clear-sky irradiance on a wall, roof or window, at one hour or for every hour of the
design day with its peak: the sun's position from the equation of time and declination, the air mass
of Kasten & Young (1989), the beam and diffuse irradiance of ASHRAE's clear-sky model from the site's
own optical depths tau_b and tau_d, and on the surface the beam, the sky diffuse with ASHRAE's Y factor
for a vertical surface, and the ground-reflected part.

**These equations reproduce ASHRAE's own 2017 load-calculation workbook to 1e-4**, over every sun-up
hour of its twelve design days - which is what makes them the equations ASHRAE computes with, not ones
that look like them. Two traps found on the way, both in published open-source code: the beam exponent
carries **+0.021 tau_b tau_d** (one library prints minus, and misses ASHRAE's figures by up to 38 W/m2),
and the Y factor is **max(0.45, 0.55 + 0.437 cos theta + 0.313 cos^2 theta)**, not a step at
cos theta = -0.2. ASHRAE's simplified declination is up to about 1 degree from NREL's precise solar
position in spring and autumn and within 0.1 degree in July; the answer says so.

For Doha on 21 July, with ASHRAE's 2017 optical depths for the month, a west wall takes **670 W/m2 at
15:00** - 383 beam, 228 sky, 59 ground - and the suite holds every one of those figures.

### 4.4 Month by month - the peak month and hour

`monthly_load` runs one room through **every month's design day, hour by hour**, and reports each
month's peak, the peak month and hour, the peak day hour by hour, and the lowest monthly peak:

- **The outdoor air at each hour** comes from ASHRAE's design-day profile: t(h) = DB - f(h).DR, and the
  wet bulb MCWB - f(h).WBR, never above the dry bulb. The fractions f(h) are ASHRAE's, as its own 2017
  load-calculation workbook carries them, and the suite reproduces that workbook's Atlanta table
  within its 0.1 F print - including the small hours of December, where the wet bulb is held at the
  dry bulb.
- **The sun at each hour** is §4.3's, on every wall, roof and window, from that month's optical
  depths.
- **People, lights, equipment and partitions** are taken as they are given, every hour; infiltration
  and outdoor air at that hour's outdoor state.
- **The weather is named or given, never chosen for you.** Name a set Heron holds (`doha-0.4`, the
  `design_weather` table of §10) or give your own months; with neither it asks, with both it refuses,
  and a month whose wet bulb is above its dry bulb is refused rather than capped into sense.
  ASHRAE's workbook lists the daily ranges beside the **5 %** dry bulb; Heron applies them to the 0.4 %
  values it holds, and the answer says so.

It is **hour-by-hour steady state**: each gain counts at its own hour, with no storage and no lag. That
finds the month and the hour that govern, but the radiant part of a gain is not delayed - an hourly
method with radiant time series or a heat balance moves the peak later and lowers it. Every answer
says so (`STEADY_HOURS` in the code). **The lowest monthly peak is not a part-load figure** - part load
is how a unit and its plant run, and the maker's data says it. A room whose load nothing outdoors
changes - people and lights only - is the same at every hour, and the answer says the hour it names
as the peak is only the first.

The suite's Doha room - one 4 m2 west window, two people, 8 W/m2 of lights - peaks on a June
afternoon and is lowest in December. Its July 15:00 hour is **1,368 W** sensible, the hand sum of
150 W of people, 160 W of lights, 254 W through the glass and 804 W of sun - the last from §4.3's
670 W/m2.

### 4.5 What it would take to be HAP-like

An hourly method needs, per wall and roof, a **conduction time series** for its construction and, per
room, a **radiant time series** for its mass - ASHRAE Fundamentals Ch. 18 Tables 16, 17, 19 and 20, or
the same computed from each construction's layers. Those tables are ASHRAE copyright and are not here;
the sun hour by hour already is. That is a separate piece of work, and §13 lists it.

---

## 5. Moist air and coils

[`heron_psychro.py`](../brain/heron_psychro.py) is ASHRAE Fundamentals Ch. 1 re-authored: Hyland &
Wexler saturation pressure over ice and over water, the standard atmosphere, humidity ratio, enthalpy,
specific volume, and wet bulb **solved** from its equation rather than read off a chart. A dew point is
the saturation equation solved backwards - the exact answer the published Peppers correlation fits.
Every function refuses outside its equation's range rather than extrapolating.

The constants are the 2009-onward ones (0.621945, 287.042, and 2501 + 1.86t in the enthalpy). Ice
gives way to water at the triple point, 0.01 C, as PsychroLib and EnergyPlus do - the Handbook's text
says 0 C, which leaves a 0.06 Pa step in the curve; the wet bulb's own equations switch at 0 C.
[`tests/test_hvac.py`](../tests/test_hvac.py) holds this module to ASHRAE's own Examples 1 and 4, as two
open test suites quote their printed answers, and to PsychroLib's published dew point. No Handbook
page was read directly.

`psychrometrics` gives a whole state point; `air_mixing` mixes outdoor and return air **by dry-air
mass**, not volume; `coil_load` gives total, sensible and latent coil load, the sensible heat ratio and
the condensate, from the air entering and leaving - entering either given or mixed.

---

## 6. Outdoor air - ASHRAE 62.1 Ventilation Rate Procedure

| | Equation | 62.1-2022 |
|---|---|---|
| Breathing zone | Vbz = Rp.Pz + Ra.Az | Eq. 6-1 |
| Zone | Voz = Vbz / Ez | Eq. 6-2 |
| Single-zone system | Vot = Voz | Eq. 6-3 |
| 100 % outdoor-air system (a DOAS) | Vot = sum of Voz - no diversity, no Ev | Eq. 6-4 |
| Multiple-zone, uncorrected | Vou = D.sum(Rp.Pz) + sum(Ra.Az), D = Ps / sum(Pz) | Eqs. 6-5, 6-6 |
| Simplified procedure | Ev = 0.88 D + 0.22 for D < 0.60, else 0.75; each zone's minimum primary airflow at least 1.5 Voz | Eqs. 6-7 to 6-9 |
| Intake | Vot = Vou / Ev | Eq. 6-10 |
| Appendix A, single supply | Xs = Vou / Vps, Zpz = Voz / Vpz, Evz = 1 + Xs - Zpz, Ev = the smallest Evz | Eqs. A-1 to A-3, A-9 |

The simplified procedure arrived in **62.1-2019** and stays in the body of 2022; Appendix A is the
alternative. Both are built, and the simplified one checks its own Eq. 6-9 condition when the zone's
minimum primary airflow is given. Appendix A is applied for a **single supply path** - secondary
recirculation (its Eqs. A-4 to A-8) is not built, and the answer says so.

`exhaust` sums fixtures and areas at the modeller's rates and says which way the space is pressurised.

**The worked examples the suite holds** (a 100 m2 office of 5 people is 42.5 L/s; three zones give 212.0
L/s by the simplified procedure, 193.3 L/s by Appendix A and 174.0 L/s on a DOAS; 62.1 Appendix M's
office check value is 0.57 L/s per m2) were worked from the equations, and reproduce.

---

## 7. Ducts

- **Friction** is Darcy-Weisbach with the **Colebrook** friction factor, solved by iteration from the
  Altshul-Tsal estimate ASHRAE gives. Standard air is 1.204 kg/m3 with ASHRAE's own Reynolds number,
  Re = 66.4.Dh.V. A rectangle or flat oval is computed as its **equivalent round** - Huebscher's
  1.30(ab)^0.625/(a+b)^0.25, Heyt & Diaz's 1.55A^0.625/P^0.25 - **at the same flow**, which is what the
  equivalent diameter is defined to mean; its velocity is its own.
- **Sizing** (`duct_size`) takes a friction rate (equal friction), a velocity limit, or both, and the
  sizes the project actually uses - the duct type's own size table, or Revit snaps to its nearest -
  and picks the **smallest that meets every limit**. It **never rounds down**: the size below puts the
  friction and the velocity over the figure given, the one direction that brings noise and rework.
  Rectangles hold a depth, a width or a ratio. Given many ducts at once - flows read from Revit - it
  groups them by size, one `SET_MEP_SIZE` call per group.
- **Static regain and the T-method** - the two other methods ASHRAE describes - are **not built** (§13).
- **The index run** (`duct_pressure`) sums each section's friction and its fittings as loss coefficient
  x velocity pressure, plus the drop through every coil, filter and terminal in the run. **Heron holds
  no fitting coefficients**: they come from the ASHRAE Duct Fitting Database or a manufacturer, depend
  on exact geometry and flow split, and are copyright - so they are the modeller's input.
- **Fan power** (`fan_power`) is air power = Q x total pressure, through fan, motor and drive
  efficiencies, to the specific fan power.

---

## 8. Air distribution

- **`diffuser_layout`** finds the fewest diffusers on a grid that meets a per-diffuser flow limit, a
  spacing, or a count; centres each in its module; gives each one's flow, the **characteristic length**
  L (half the smaller module - to the wall, or to where neighbouring jets meet) and its plan
  coordinates. With a room corner and a **mounting height** it gives the points
  `PLACE_FAMILY_INSTANCES` takes; without a height it says ASK, because a point with no height lands a
  ceiling diffuser on the floor (NEEDS-CHECKING AJ4).
- **ADPI.** Air Diffusion Performance Index is the share of occupied-zone points whose effective draft
  temperature lies between -1.7 and +1.1 K at under 0.35 m/s. ASHRAE's selection guide relates it to
  T50/L - a diffuser's throw to 0.25 m/s over its characteristic length. Given a diffuser type and the
  room's load per area, the layout OFFERS the throw to look for in the catalogue.
- **`diffuser_select`** sizes a neck to a velocity limit - and says NC and throw are the product's -
  or, given the manufacturer's own catalogue rows, interpolates NC and T50 at the design flow and picks
  the first size that meets the NC limit and the throw range. NC at the same neck velocity ranges from
  18 to 41 between products, which is why Heron holds no velocity rule as if it were an NC - **with one
  exception the owner made**: given `max_nc` 30, a supply neck is held to the office's own **2.5 m/s**
  ([D-110](decisions/D-110.md)), not the 2.2 ASHRAE's table prints, and the answer says whose figure it
  is. Any other criterion is asked, with ASHRAE's row for it offered; a `max_neck_velocity_ms` the
  modeller gives always wins; and the product's catalogue NC still governs the product.
- **`throw`** is the free-jet decay Vx/V0 = K.sqrt(A0)/x solved for x at 0.75, 0.5 and 0.25 m/s, with
  the product's own throw constant K.
- **`terminal_flows`** splits a room's air across its terminals so the shares add up **exactly**, and
  writes the id-and-flow file `SET_AIR_TERMINAL_FLOW` reads.
- **`grille_velocity`** checks a return, exhaust or transfer grille's face velocity.

---

## 9. Water side

- **`chw_flow`**: m = Q / (cp.dT), with water's density and cp at the mean temperature. Where the
  project follows ASHRAE 90.1-2022, Section 6.5.4.7 asks for a coil selected for **at least 8.33 K rise
  and 13.89 C leaving water**, with exceptions; the answer says whether this one is.
- **`pipe_size`**: Darcy-Weisbach with Colebrook, water density by **Kell (1975)** and viscosity by
  **Laliberte (2007)** - both checked against the IAPWS reference values for every degree from 0 to
  99 C, within 0.0015 % and 0.32 % - against the bores the project uses, or ASME B36.10M schedule 40.
- **`unit_select`** picks a **fan coil** or a **split** for a room from the maker's own catalogue rows,
  listed smallest first: the first that covers the total load, and the sensible load and the airflow
  where they are given - **never on its total alone**, because a unit with enough total and too little
  sensible leaves the room warm. Each row shows how far over the load it is, and a margin past the
  `max_oversize_pct` given is flagged. **Heron holds no unit's capacity**: without a catalogue it
  asks, and when no row covers the load it says FAIL rather than offering the biggest. A fan coil's
  answer points on to `chw_flow` and `pipe_size`; a split's says its refrigerant piping is the maker's.

---

## 10. The reference tables, and how far each was checked

`python brain/heron_hvac.py reference` lists them. **A figure is used only once the modeller confirms
it.** Each table names its source in the code; this is how far each was checked:

| Table | Source | Checked |
|---|---|---|
| `ventilation_rates` | 62.1-2022 Table 6-1, SI column as printed | read in the 2022 text; the I-P values agree with NREL's ventilation dataset and an IMC-based code |
| `zone_air_distribution` | 62.1-2022 Table 6-4 | read in the 2022 text |
| `exhaust_rates` | 62.1-2022 Table 6-2 | read in the 2022 text |
| `adpi` | ASHRAE's classic ADPI selection guide | **only the rows whose numbers came back from searches that did not contain them**, through manufacturers' reproductions; the Handbook page was not read |
| `air_terminal_guidance` | ASHRAE HVAC Applications (2019) Ch. 49, Table 9 | one transcription citing the Handbook page |
| `comfort_air_speed` | ASHRAE 55-2023 Section 5.3.4.2 | the 2023 text and an independent implementation agree |
| `duct_roughness` | Fundamentals Ch. 21 Table 1 | galvanized steel 0.09 mm only - the friction chart's own basis, from several sources |
| `duct_design_guidance` | HVAC Applications, Noise and Vibration Control | the complete table in both unit systems, which convert into each other exactly |
| `smacna_pressure_classes` | SMACNA HVAC Duct Construction Standards, 3rd ed. | all seven classes read in US federal criteria |
| `round_duct_sizes` | EN 1506:2007 | two independent transcriptions, not the standard |
| `duct_insulation` | 90.1-2022 Table 6.8.2 | the 2022 text, and search summaries agree |
| `fan_power_limits` | 90.1-2022 Table 6.5.3.1-1; Approved Document L 2021 Vol 2 Table 6.9 | 90.1 from PNNL's standards dataset and the text; Part L from the regulation's own text |
| `pipe_roughness` | Moody (1944), as reproduced | secondary sources, consistent with the Handbook of Hydraulic Resistance |
| `pipe_sizes` | ASME B36.10M schedule 40 | one machine-readable reproduction, and every row checks as OD - 2t |
| `pipe_design_guidance` | 90.1-2022 Table 6.5.4.6 | the 2022 text |
| `chilled_water_practice` | 90.1-2022 Section 6.5.4.7 | the 2022 text |
| `design_weather` | ASHRAE Fundamentals 2017 climatic data, Doha International | read from ASHRAE's own 2017 workbook, with each month's daily dry-bulb and coincident wet-bulb ranges, which it lists beside the 5 % dry bulb; its optical depths reproduce ASHRAE's tabulated noon irradiance for Doha within the table's rounding. **Monthly** values only - the annual 0.4/1/2 % and the dehumidification condition could not be read |
| `sol_air` | Fundamentals Ch. 18, as ASHRAE's 2017 example uses it | the example's inputs, absorptance 0.45 and 0.9 over 17 W/m2.K |
| `people_heat_gain` | Fundamentals Ch. 18 Table 1 | **two rows only** - moderately active office work and seated very light work - against ASHRAE's own example and an open implementation |

**How the research was done, and its limit.** The network this was written on reached GitHub and the
package index and almost nothing else: ashrae.org, smacna.org, cibse.org, iccsafe.org and every Qatari
host were blocked, and the web-search budget ran out part-way. So the standards were read through
public datasets that encode them (PNNL, NREL), through US federal criteria that quote them, through
published code, and through transcriptions - **confirm a figure against your own copy of the standard
before it governs a project.** No standard's text is reproduced here; the figures are facts, cited.

---

## 11. Qatar

| | What is known |
|---|---|
| QCS | QCS 2014 Section 22 is *"Air Conditioning, Refrigeration and Ventilation"* - title level only, from two sources. QCS 2024 exists, approved by Ministerial Decision 15/2024 as an optional standard |
| Which 62.1 edition | **Not established.** Nothing readable says which edition QCS, Civil Defence, Kahramaa or GSAS requires. It is a project input, asked once per project (next row) |
| Which standards govern a project | **Asked once per project and kept for it** ([D-111](decisions/D-111.md)): `ventilation_standard` (a 62.1 edition, or other), `energy_standard` (a 90.1 edition, other or none), `qcs_edition` (a QCS edition, or none), `cibse_beside_ashrae`. Asked by `cooling_load`, `monthly_load`, `ventilation`, `exhaust`, `fan_power`, `chw_flow` and `pipe_size` - `python brain/heron_hvac.py` marks each one *asks once per project* - never as a blocker - the four sit at the top of the answer until known, and each check that needs one says it was not run. Kept in `projects/<key>.hvac.json` in Heron's knowledge folder, named by the open model's Project Information UniqueId; a chat that has not read the model keeps nothing and says so. What they change: an edition other than 62.1-2022 or 90.1-2022 is named beside every figure and check it touches; a project not governed by 62.1 is told the procedure is a comparison; 90.1's coil, fan power and pipe-size limits are raised only on a 90.1 project; QCS 2014 brings the reported conditions below into the load answer, marked unread |
| District cooling temperatures | **Not established** - Qatar Cool and Kahramaa hosts were blocked. The utility's interface letter is the source; Heron asks for supply and return every time |
| Climate zone | Doha works out as ASHRAE 169 zone **0B** (extremely hot, dry) from the zone definitions - derived, not looked up in the station list |
| Design weather | ASHRAE 2017 **monthly** design data for Doha International (WMO 411700) is held as a reference: July 0.4 % is 45.7 C DB with 22.2 C mean coincident WB, heating 11.8 C at 99.6 %, and each month's daily ranges and clear-sky optical depths. Offered, and used only where the modeller names it - `monthly_load` with `design_weather: doha-0.4`; the annual and dehumidification conditions could not be read |
| QCS design conditions | QCS 2014 Section 22 Part 1 is reported to set **46 C DB / 30 C WB** outdoors and **23 +/- 1 C, 50 +/- 5 %** indoors - from search summaries of the text, not the text. 46/30 C is a far more humid pair than ASHRAE's peak (20.3 g/kg against 7.1), a conservative combined condition rather than a coincident statistic; per 100 L/s of outdoor air brought to a 23 C, 50 % room it is about 3.5 kW of latent load, where ASHRAE's July peak gives a negative one |

---

## 12. Where two sources disagree - recorded, not resolved

| | One side | The other | Who resolves |
|---|---|---|---|
| 1 | A supply diffuser neck at **2.5 m/s** for NC/RC 30, in the owner's own AJ-Tools knowledge file | ASHRAE HVAC Applications (2019) Ch. 49 Table 9 as transcribed: **2.2 m/s** supply at RC 30 - 2.5 is RC 35, or RC 30 for a return | **resolved 2026-10-02 by the owner: 2.5 m/s governs** ([D-110](decisions/D-110.md)). ASHRAE's table is kept as it prints |
| 2 | The SI effective draft temperature coefficient **7.66** (the exact conversion of ASHRAE's 0.07 F/fpm) | **8**, in a 2020 journal article | the Fundamentals SI edition, Ch. 20 |
| 3 | 62.1-2022's stratified Ez rows print **"60 fpm (0.25 m/s)"** | 0.25 m/s is **49 fpm** | ASHRAE's errata |
| 4 | Public toilet exhaust: the higher rate where **heavy use** is expected (62.1-2022 note D) | the higher rate where the fan runs **intermittently** (IMC-based codes) | the project's adopted code |
| 5 | IMC-based tables carry older rates for gyms, warehouses, locker rooms and private toilets | 62.1-2022 | the project's adopted code |
| 6 | An inch of water at 60 F, **248.84 Pa**, as ASHRAE's SI tables give it - what Heron uses | the conventional inch at 4 C, **249.09 Pa** - 0.1 % apart | a convention, recorded so a reader comparing figures knows |
| 7 | ASHRAE 55's vertical temperature difference of 3 K seated / 4 K standing (before 2021) | 55-2023's gradient formula, which depends on thermal sensation | the edition the project follows |
| 8 | A roof's long-wave term **3.7 K** - ASHRAE's own 2017 workbook computes 20 Btu/h.ft2 over 3.0, and Heron uses it | the Handbook prose's rounded *"about 7 F"*, **3.9 K** | a convention; 0.2 K on a roof's sol-air temperature |
| 9 | Outdoor design air for latent loads: ASHRAE's dehumidification condition for the site | QCS 2014's reported 46 C DB / 30 C WB | the project's adopted basis - the answer shows which was given |

---

## 13. What is not built

- **An hourly load method** - the radiant and conduction time series of RTS, or a heat balance. The sun
  hour by hour is built (§4.3), and so is a month-by-month sweep in steady state (§4.4); the time
  series need ASHRAE's tables or a layer-by-layer calculation (§4.5).
- **Part load.** `monthly_load`'s lowest month is the smallest design-day peak, not a part-load
  condition, and Heron holds no unit's part-load data.
- **Refrigerant piping** for a split or a VRF system - line sizes, lengths and lift are the maker's to
  state.
- **A dehumidification design case** run alongside the peak dry bulb. The engine warns when the outdoor
  air is drier than the room; the site's dehumidification condition is the project's to give.
- **Static regain and the T-method** for duct sizing.
- **A fitting-loss library** - the coefficients are the ASHRAE Duct Fitting Database's, and copyright.
- **Acoustics** - an NC from a duct system, breakout, attenuators. Product NC is read from a catalogue
  the modeller supplies.
- **Duct heat gain, fan heat from a selection, return-air gains** - added only as given.
- **A fragment that sizes ducts by friction inside Revit.** Today the host reads the flows, `duct_size`
  sizes them, and `SET_MEP_SIZE` writes each size group. A fragment would do it in one call and would
  carry the same arithmetic in C# - a second home for one fact - so it is a decision, not a gap.
- **Skills.** A skill names capabilities, and this is an MCP tool, not a fragment capability - so
  `space-airflow`, `terminal-layout` and `duct-layout` do not yet name it. How a skill should reach a
  brain-side calculation is the owner's question (§15).

---

## 14. How it was checked, and what would prove it

[`tests/test_hvac.py`](../tests/test_hvac.py) holds: saturation pressure against steam-table values; a
state point against a chart, ASHRAE's Examples 1 and 4 and PsychroLib's published dew point; Colebrook
against a published library's own doctest; ten duct-friction points against an independent solver and
the one published duct example found; the equivalent diameters against their identities; the sun on
Doha against the chain that reproduces ASHRAE's 2017 workbook, and against ASHRAE's printed Doha table;
water's density, cp and viscosity against IAPWS; every 62.1 worked example; a room load summed by
hand; a duct sweep in which every size meets the rate and the size below does not; every calculation
handed nothing computing nothing; refusals; and that the two modules import only the standard library.
[`tests/test_mcp_serves.py`](../tests/test_mcp_serves.py) calls the tool through the MCP SDK's own
dispatch - where the SDK is installed, which CI's runner is not, so CI reports that suite as not
runnable.

Section 7 of the same suite holds the owner's two answers: the office's 2.5 m/s at NC/RC 30 and nowhere
else ([D-110](decisions/D-110.md)), and a project's standards asked once, kept for that project only,
replaced with a record of the old, set aside rather than overwritten when unreadable, and not kept at all
when no project is known ([D-111](decisions/D-111.md)) - against stand-in project keys. The key Revit
itself reports is NEEDS-CHECKING [Group BT](needs-checking/group-bt.md).

Section 8 holds the month-by-month sweep and the unit: the design day's hourly shape against ASHRAE's
own 2017 workbook; the Doha room above over twelve months, and its July 15:00 hour against the hand
sum; `unit_select` refusing to pick on the total alone, picking nothing without a catalogue and
failing when nothing fits; and that every Revit tool an answer names is a capability in
[`brain/fragments`](../brain/fragments), read from the source so a line no test reaches is held too.

**None of that is a proof in [D-30](decisions/D-30.md)'s sense**, and this engine has no fragment to
prove. What would carry weight is a **comparison against an engineer's own run**: one real room in HAP
or TRACE, the same inputs here, and the difference explained - expected to be higher here, for the
reason §4.2 gives. Until then every answer is marked a design aid.

---

## 15. For the owner

1. **The reference tables are OFFERED with their figures.** D-33 is kept - nothing is applied unasked -
   but a figure shown beside a question is still a figure shown. Keep it, or show only the table's name?
2. **Which standards govern a Qatari project** - the 62.1 edition, 90.1 or not, CIBSE alongside ASHRAE -
   is the project's to state. Should Heron ask once per project and remember it (D-33's *asks once*)?
   **Answered 2026-10-02: yes** - [D-111](decisions/D-111.md), built as §11 describes.
3. **A friction-sizing fragment in Revit** (§13) - worth the second copy of the arithmetic?
4. **Skills** - should `space-airflow` and its neighbours name `heron_hvac`, and if so, how does a skill
   name a brain-side calculation?
5. **Conflict 1 in §12** is your own file against ASHRAE's table - which governs your work?
   **Answered 2026-10-02: your 2.5 m/s** - [D-110](decisions/D-110.md), built as §8 describes.
