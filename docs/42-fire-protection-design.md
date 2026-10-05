# 42 — Fire protection design: what Heron works out before a sprinkler is placed

> **Status: BUILT, NOT PROVEN.** Written 2026-10-02, on the owner's request, with the engine it
> describes: [`brain/heron_fire.py`](../brain/heron_fire.py), served as the `heron_fire` MCP tool, owned
> by `HERON-MEP-FPD-002` in the [agent registry](28-agent-registry.md), beside the HVAC engine of
> [41](41-hvac-design.md) whose answer machinery it shares. **What is proven is arithmetic**:
> [`tests/test_fire.py`](../tests/test_fire.py) derives Hazen-Williams from its general form, checks the
> network solver against an independent method, and holds every table to its own units. **Nothing here
> has been checked against a real project or a listed hydraulic program** - §14 says what that would
> take. A design value is the fire consultant's, and the design is the authority's to approve - QCDD's
> on a Qatar project.
>
> **Round two, the same day**, on the owner's *"do the best research ... not only NFPA"*: a project
> may now follow **BS EN 12845** or **FM Global** instead of NFPA 13 (§4a); NFPA 13's C values, K
> list, temperature ratings, design area percentages and more of the beam rule are held; and
> `temperature_rating`, `equivalent_length` and `hose_reels` are new, with **BS 5839-1**, **BS 5306**,
> **BS 9990**, and what is known of **Qatar** and the **UAE** beside NFPA's figures (§9a, §11).

---

## 0. In Revit words

Heron could already **model** a sprinkler system - read a room, place heads at points, find the beam
over one, report what each head covers, read the K-factor on a connector. It could not say how many
heads a room needs, how far apart they may be, what pipe feeds ten of them, or what pressure the riser
needs. Now it can work them out:

| You ask | Heron works out | Then, in Revit |
|---|---|---|
| *"What hazard class is a car park?"* | what NFPA 13's annex and your own file list beside it, and what each class asks for - **never the class itself** (`hazard_class`) | the class you confirm, written to the Spaces |
| *"How many sprinklers for this room?"* | the fewest on a grid inside your spacing, area and wall limits, with their points (`sprinkler_layout`) | `PLACE_FAMILY_INSTANCES` at the points, then `CHECK_OBSTRUCTIONS` |
| *"Check my sprinkler spacing"* | each drawn head's S, L and area as NFPA 13 measures them, its walls and its nearest neighbour (`sprinkler_spacing`) | the heads and the outline come from `READ_ROOM_GEOMETRY` and the model |
| *"What pipe size for 10 sprinklers, light hazard?"* | the pipe schedule's size, steel or copper (`pipe_schedule`) | `SET_MEP_SIZE`, one call per size |
| *"Sprinklers above AND below the ceiling on one branch?"* | the above-and-below schedule, counted over two levels, and the 8-and-8 branch limit (`pipe_schedule`) | `SET_MEP_SIZE` |
| *"The beam is 700 mm away - how high can the deflector be?"* | the beam rule's allowance, the three-times rule, the wide-obstruction rule (`obstruction`) | `CHECK_OBSTRUCTIONS` finds the beams |
| *"How many heads in the design area, and what flow?"* | the area after its adjustments, its sprinklers, 1.2 sqrt A along a branch, each head's flow and pressure (`design_area`) | `hydraulic` takes them as the operating heads |
| *"What pressure does the riser need?"* | a tree or a looped grid solved node by node (`hydraulic`), against a flow test (`water_supply`) | `READ_MEP_SYSTEM` and `REPORT_CONNECTOR_LOADS` give the network |
| *"How big is the fire tank? Is this pump right?"* | the volume over each demand's duration (`water_storage`); NFPA 20's curve rules and the demand (`fire_pump`) | - |
| *"Standpipes, extinguishers, smoke detectors?"* | NFPA 14's demand (`standpipe`), NFPA 10's or BS 5306-8's count (`extinguishers`), NFPA 72's grid or BS 5839-1's radius (`detector_layout`), coverage within a radius - by one device or two (`coverage_check`) | `PLACE_FAMILY_INSTANCES` |
| *"Hose reels - what flow and pressure?"* | the reels at once, each one's flow, the pressure at the most remote, rise and friction (`hose_reels`) | `PLACE_FAMILY_INSTANCES`, then `coverage_check` reached by 2 |
| *"Which temperature rating at 45 C?"* | the classifications NFPA 13's table allows - or EN 12845's bulbs at 30 C above (`temperature_rating`) | `CHANGE_ELEMENT_TYPE` to the type chosen |
| *"Equivalent length of two tees and a gate valve?"* | NFPA 13's chart, adjusted to the pipe's C and bore (`equivalent_length`) | `hydraulic` and `friction_loss` take it |
| *"This project is to EN 12845 / FM Global"* | that standard's classes and figures offered instead of NFPA 13's (§4a) | - |

`python brain/heron_fire.py` lists every calculation and what it needs. That list is **derived** - each
calculation is run on nothing and reports what it asked for - so it cannot drift from the code.

**Three things it will never do.** It never fills in a design value you did not give (§3). It never
decides a hazard class. And it never changes the model: an answer NAMES the fragment that would put it
into Revit, and running that fragment is `revit_change`'s job, behind the Changes switch.

---

## 1. Where it sits

```text
modeller ── Claude Code ── heron_fire (MCP, READ, no bridge operation)
                               │
                       mcp/server/heron_brain.py  fire()  ── _design()   one seam with hvac()
                               │
                       brain/heron_fire.py         the calculations, the tables, the solver
                       brain/heron_hvac.py         the answer machinery both engines share
                       brain/heron_designbasis.py  a project's design basis, one file per discipline (D-111)
```

| | |
|---|---|
| Layer | `brain/` - Python. The engine imports the standard library and `heron_hvac` only, and [`tests/test_fire.py`](../tests/test_fire.py) parses it to hold that |
| Shared, not copied | reading inputs, refusing, offering a cited figure, the answer's shape and its rendering are `heron_hvac.py`'s - `run`, `catalogue`, `describe` and the reference helpers took an engine's own registry and table set as arguments, defaulting to HVAC's, so the HVAC suite runs unchanged |
| Risk | **READ.** Arithmetic over what it is handed - no model, no network. The one thing kept is a project's governing standards once the modeller has said them ([D-111](decisions/D-111.md)), in `projects/<key>.fire.json` beside the HVAC record, never in it |
| Agent | `HERON-MEP-FPD-002`, the second row of *MEP Design Engineering* |
| Audit | `design.fire`, one line per call: the calculation and how it ended. **Never the inputs** |
| Refusals | the HVAC engine's codes, through the same seam: `needs_request_values`, `unusable_request_values`, `unknown_op` - all correct refusals, not gaps |

---

## 2. Why a new agent, and the two verdicts that were set aside

[`HERON-AHR-WFP-015`](../brain/heron_workforce.py), the agent that says *no* before anything is hired,
was asked twice. With the row first written in the HVAC row's own sentence frame - *"Calculates what a
sprinkler system needs before it is modelled ..."* - it answered **`ALREADY_AN_AGENT`:
`HERON-MEP-HVD-001`, at 0.47**, against the 0.45 that decides it. Reworded, it answered
**`ALREADY_A_CAPABILITY`: `READ_CEILING_GRID`, at 0.67**, on *ceiling* and *grid*.

Both are word overlap, which the planner says of itself by reporting the real question as **NOT
JUDGED** for want of a router. HVAC and fire protection are separate disciplines - ASHRAE against
NFPA, a mechanical engineer against a fire consultant, a different authority's sign-off - and a fire
answer folded into the HVAC agent would give one agent two primary responsibilities (Golden Rule 2).
Reading a ceiling grid calculates nothing. Both verdicts are recorded here and set aside.

---

## 3. The rule that shapes every answer - D-33, and where its line falls in fire

[D-33](decisions/D-33.md): **Heron never supplies a value the user did not give.** For fire protection
the line falls in four places, and the engine's docstring states the same four:

- **A design criterion is an input, every time.** The hazard class, a density, a design area, a spacing
  or area limit, a wall distance, a minimum pressure, a C factor, a hose allowance, a duration, a
  margin. Missing one, a calculation computes **nothing** and answers *ASK THE MODELLER*. Where NFPA
  holds a figure for it, the figure is **offered** beside the question - *"NFPA 13-2022 Table
  19.2.3.1.1 gives 6.1 mm/min for ordinary hazard group 1 - offer it to the modeller, do not assume
  it"* - and the calculation still waits.
- **The hazard class above all.** `hazard_class` shows what NFPA 13's annex - and the owner's own file -
  lists beside an occupancy, and every class those lists name; it never picks one. A room's name does
  not decide its class: what is stored there, and how high, does.
- **A fact or a table asked about is looked up.** A steel pipe's bore, named by its schedule, comes from
  ASME B36.10M as `heron_hvac.py` takes a named material's roughness. And where the modeller asks a
  table's own question - *"what does the light hazard schedule give for ten sprinklers"*, *"how high
  may the deflector sit at 700 mm"* - the table answers, labelled as that table's figure. The question
  asked for NFPA's figure, not for Heron's. **This line is new, and F43 item 1 puts it to the owner.**
- **A factor that enlarges a demand is asked; one that shrinks it is never applied unless given.** A dry
  or double-interlock system works a larger design area, and leaving that out would err small - so the
  increase is a required input. A quick-response reduction is applied only when given. Every answer
  lists what it was not given under **NOT GIVEN, SO NOT APPLIED**.

**And a figure nobody could check is not held at all.** This was written where the standards' pages
could not be opened (§10). A table row is held only where at least one source confirmed it; a row that
could only be remembered - most of the fittings chart, a handful of pipe schedule cells, FM Global's
spacing - is left out, and the question that would have used it is asked instead. A cell left out of a
table says so the moment a question reaches it, rather than being skipped to the next size up.

**A project's governing standards are asked once, and kept for that project** ([D-111](decisions/D-111.md)):
`sprinkler_standard` (an NFPA 13 edition, BS EN 12845, FM Global, or other) and `fire_authority` (QCDD,
UAE Civil Defence, other or none). Each calculation asks only the ones it uses - a pump question is not
asked which sprinkler standard governs.

---

## 4. Hazard classes

NFPA 13 sorts occupancies into **light**, **ordinary group 1 and 2** and **extra group 1 and 2** by
what is there to burn and how fast it burns - the definitions are summarised, in Heron's own words,
in `reference hazard_definitions`. The annex's examples are guidance, not a rule.

`hazard_class` searches the annex's examples and the owner's own list for an occupancy's words - with a
small table of everyday words beside the annex's (*car* beside *automobile*, *kitchen* beside
*restaurant service*, *mosque* beside *church*) that changes only which rows are shown - and returns
every row that matches, with the design figures each class carries and a warning wherever two classes
match. **A kitchen** shows restaurant service areas under group 1 (the annex, and the owner's file)
and kitchens under group 2 (the owner's file): exactly the question the engineer has to answer, so it
is shown rather than settled.

---

## 4a. One standard at a time - NFPA 13, BS EN 12845, FM Global

A project's sprinklers follow **one** standard, and `sprinkler_standard` says which, once
([D-111](decisions/D-111.md)): an edition of **NFPA 13**, **BS EN 12845** (any national body's EN
12845, with or without its edition), or **FM Global**'s data sheets - or `other`. NFPA 13R and 13D are
refused as a project standard and held only as reference rows (`reference nfpa_residential`).

| | NFPA 13 | BS EN 12845 | FM Global (DS 3-26) |
|---|---|---|---|
| Classes | light, OH1, OH2, EH1, EH2 | LH, OH1-OH4, HHP1-HHP4, HHS1-HHS4 | HC-1, HC-2, HC-3 |
| Design figures offered | density/area, hose, duration (`design_criteria`) | density, wet and dry area, duration, area and spacing per sprinkler, least pressure (`en12845_criteria`) | density, wet and dry area, hose, 60 min, least K, by ceiling height (`fm_criteria`) |
| Pipe schedule, beam rule | held | **refused** - a pre-calculated EN system has its own tables, not read | **refused** - calculated |
| Branch count, 1.2 sqrt A | given | not given - EN's rule for the area's shape was not read | not given |

**A class is read only in the project's own standard.** EN 12845's OH1 is 5.0 mm/min over 72 m2; NFPA
13's OH1 is 6.1 mm/min over 140 m2 - the same letters, a different class. So `OH3` under an NFPA 13
project is refused, saying it is an EN 12845 class, and `EH1` under an EN 12845 project the same way;
until the standard is known, NFPA 13's classes are read and every answer says so.

**What each standard's own rules change.** An EN 12845 dry or alternate system is offered its table's
dry area (25 % above the wet one, read from the table, not applied as a rule); a dry LH or OH4 system
FAILS - EN 12845 designs them as OH1 and HHP1. EN 12845's HHS classes and HHP4 are designed from tables
not read, and say so. FM Global's HC-1 dry area is left out: both sources gave 1500 ft2, which sits
oddly beside HC-2's and HC-3's 3500 ft2. **Nothing found shows QCDD accepting EN 12845 or FM Global in
place of NFPA 13** - on a Qatar project they are offered only when the project says it follows them.

---

## 5. Sprinkler spacing and layout

- **`sprinkler_layout`** finds the fewest sprinklers on a grid in a rectangular room meeting the most
  spacing, the most area per sprinkler and the most wall distance given - with the least spacing and
  wall distance checked where given - each centred in its module, the squarest module among equal
  counts. With a room corner and a mounting height it gives the points `PLACE_FAMILY_INSTANCES` takes;
  without a height it says ASK.
- **`sprinkler_spacing`** checks heads already drawn, from their plan positions and the room's outline,
  as NFPA 13 measures a head's protection area: **S** along its branch line is the larger of the
  distances to the next head either way - or twice the distance to the wall for an end head - and **L**
  is the same between branch lines; the area is S x L. Walls are found by casting a line from each head
  along and across its branch, so an L-shaped room's inside corner counts as the wall it is. Each head
  gets its S, L, area, end-wall distance and nearest neighbour, and a named FAIL. A sampled farthest
  point beyond the half-diagonal of the largest square module allowed is flagged as a part of the room
  that may have no sprinkler over it - a check NFPA does not state, labelled as Heron's.
- The limits are **asked**, with NFPA 13's rows for the hazard offered - light hazard 20.9 m2 (225
  ft2) hydraulically calculated and 18.6 m2 (200 ft2) on a pipe schedule, ordinary hazard 12.1 m2
  (130 ft2), 4.6 m (15 ft) for both, half the allowable spacing to a wall, 1.8 m (6 ft) between heads,
  100 mm (4 in) from a wall.

---

## 6. The design area - and sprinklers above and below a ceiling

`design_area` takes the density, the design area and the layout's own area per sprinkler and
spacing, and gives:

- the area after its adjustments, each one the modeller gave - a dry or double-interlock system's
  increase (asked whenever the system is one of those, NFPA 13's 30 % offered), a sloped ceiling's
  increase (asked whenever a slope is given; 30 % above 2 in 12 offered), a quick-response reduction
  (only when given, only on a wet system; NFPA's 40 % to 25 % ends offered, and never to fewer than five
  sprinklers - a FAIL), a high-temperature reduction at extra hazard (only when given; 25 %, never below
  2000 ft2 - a FAIL);
- **each adjustment is a percentage of the area first selected, and they are added** - NFPA 13's own
  example as a search extract reproduces it: 2500 + 750 - 625 = 2625 ft2 for a dry system with
  high-temperature sprinklers. Until round two the engine multiplied them in turn, which would give
  2437.5; it was changed on that evidence (§12 row 11);
- the sprinklers in it, area over area per sprinkler rounded up; along a branch line, **1.2 sqrt A**
  over S rounded up - NFPA 13's rectangular design area, now confirmed by two result sets; the number
  of branch lines;
- each sprinkler's least flow, density x area, and with a K-factor the end sprinkler's pressure - the
  larger of (q/K)^2 and the minimum pressure given;
- the least demand of the design area's sprinklers together - a floor, which the hydraulic calculation
  always exceeds as pressure rises toward the source - and with hose streams and a duration, the least
  water.

**Sprinklers above and below a ceiling.** Give `concealed_space`. Where the space above is
**combustible and not sprinklered**, NFPA 13-2022 19.2.3.1.5 asks a design area of at least 3000 ft2
(280 m2) at the table's second density point, and the offers switch to that point - the minimum checked
**after** every other adjustment, as NFPA 13 says. Where it **is
sprinklered**, give its sprinklers as `above_ceiling` - density, design area, area per sprinkler - and
say whether the two levels are **combined** in one design area or **calculated apart**, the larger
governing. That choice is asked every time: NFPA 13's own wording on it was not read in this work, and
an authority may set its own.

---

## 7. Pipe schedules - including sprinklers above and below a ceiling

`pipe_schedule` answers the schedule's question - the smallest size the table allows for the number of
sprinklers a pipe feeds - for light or ordinary hazard, steel or copper, one pipe or a list of them,
with `SET_MEP_SIZE` named per size.

- **Above and below a ceiling**, on common branch lines or one cross main: each pipe is counted as the
  sprinklers above plus those below - the greatest number on any two adjacent levels - and sized from
  the above-and-below table. Past that table's last figure (light hazard: 50 on 2 1/2 in steel, 65
  copper) the pipe is the next size, then sized from the regular schedule by the larger of the two
  levels, whichever is bigger. The branch line limit is 8 above and 8 below on either side of the cross
  main.
- **Ordinary hazard at more than 3.7 m (12 ft)** between heads or branch lines uses its own 2 1/2 to 3
  1/2 in rows.
- **Extra hazard** is refused and sent to `hydraulic`: NFPA 13 keeps the schedule for additions to an
  existing extra hazard pipe schedule system only.
- **Every answer says the schedule is a table, not a hydraulic design**, and NFPA 13's limit on where a
  new system may use it - 5000 ft2 (465 m2), or larger only with the schedule's supply at 50 psi (3.4
  bar) residual at the highest sprinkler - and that an authority may require every system calculated.

**Cells not held.** The light hazard 3 1/2 in steel figure, the ordinary hazard 3 and 3 1/2 in copper
figures and the ordinary hazard above-and-below 3 in row could not be confirmed (§10). Every schedule
carries strictly more sprinklers on each larger size, so an unread cell carries fewer than the next one
held: a count at or above that next figure is sized past it, safely; a count below it is **refused**,
never rounded to a guess.

---

## 8. Obstructions

`obstruction` answers for **standard upright and pendent spray sprinklers** only - every other type has
its own rules in NFPA 13 and in its listing, and is refused:

- **The beam rule**: at a horizontal distance A from the side of a beam or duct, how far the deflector
  may sit above the obstruction's bottom - or, given that height, how far away the sprinkler must be.
  The table is held to 5 ft 6 in (1676 mm), its last three rows added in round two by two extracts;
  past it one source's table goes on rising and another stops at 18 in, so a distance past it is said
  so, with a height beyond the last allowance NOT CHECKED.
- **The three-times rule**: at least three times the obstruction's largest dimension, never more than
  24 in (610 mm) - with a source's note that the cap may not apply to a column - and, in light and
  ordinary hazard, structural members only.
- **Wide obstructions**: wider than 4 ft (1.2 m), sprinklers are needed under them.

---

## 9. Hydraulics, supply and the rest

- **The solver** (`hydraulic`) takes the network as nodes - each with its elevation, and for a sprinkler
  its K-factor and either its area (for the density) or its least flow - and pipes, each with its
  length, its bore or nominal size and schedule, its C and its fittings' equivalent length. It solves
  every node pressure at once by **Newton's method**: Hazen-Williams in every pipe, q = K sqrt(p) at
  every operating sprinkler, flow balanced at every node, elevation at NFPA's 0.433 psi per foot. The
  source pressure is raised until the **governing** sprinkler has exactly its need and none has less -
  for a tree and for a looped grid alike, with no hand balancing at junctions. The answer gives each
  sprinkler's flow and pressure, each pipe's flow, velocity and loss, the demand at the source with
  hose streams and device losses, and checks against a velocity limit, a sprinkler's rated pressure
  and a flow test where given. **Every answer says it is not a listed hydraulic program.**
- **The method is the total-pressure one** - velocity pressure is not taken off at the heads, which NFPA
  13 permits and which errs toward more pressure.
- **`friction_loss`** and **`sprinkler_flow`** are the two relations on their own.
- **`water_supply`** reads a flow test as NFPA's N^1.85 graph does, P = Ps - (Ps - Pr)(Q/Qt)^1.85, and
  gives the margin at the demand and the most flow at the demand's pressure, warning past the tested flow.
- **`water_storage`** sums each demand's flow over its own duration, plus any volume below the pump
  suction; the duration is asked, because on a Qatar project the authority's own table sets it.
- **`fire_pump`** holds a pump's three curve points to NFPA 20's two shape rules - churn at most 140 % of
  rated pressure, at least 65 % at 150 % of rated flow - reads the curve at the demand through the
  parabola those points define, adds the suction, and offers NFPA 20's rated capacities at or above it.
- **`standpipe`** sums the first standpipe's flow and each additional one's, to the most given, and the
  pressure at the source from the outlet's residual, the rise and the friction; with the source's
  static pressure, the lowest outlet's, for a pressure-regulating device.
- **`extinguishers`** gives the least Class A count by area - NFPA 10's way, or BS 5306-8's by a
  combined rating per m2 when `rating_per_m2` is given; the two are never mixed, because an EN 3 13A
  and an NFPA 2-A are measured on different scales. **`detector_layout`** lays out spot smoke or heat
  detectors to a spacing and a wall distance (NFPA 72's first way) or, given `radius_m`, so that every
  point is within the radius (NFPA 72's 0.7 S, BS 5839-1's 7.5 m and 5.3 m), with a heat detector's
  height factor, a least wall distance and a ceiling height checked where given.
  **`coverage_check`** tests every sampled point of an outline against a radius - by one device, or by
  two (`reached_by`), as QCDD is reported to ask of hose reels - and says a straight line is not a
  walked route.
- **`water_storage`** says, on a QCDD project, the two compartments and the 6 hour refill QCDD is
  reported to ask, worked for the volume; on a UAE project, the hour of pump capacity.

---

## 9a. Round two's calculations

- **`temperature_rating`** - the classifications NFPA 13's Table 7.2.4.1 allows at the highest
  temperature a ceiling may reach, the lowest first, each with its rating and colours; under EN 12845,
  the least rating 30 C above it and the glass bulbs held at or above that. A table answered as the
  table: the rating fitted stays the engineer's, and a sprinkler near a heat source needs NFPA 13's
  distance table, which was not read.
- **`equivalent_length`** - the fittings and valves named, read from NFPA 13's equivalent length chart
  (schedule 40 steel at C = 120), times **(C / 120)^1.85** - the form two result sets give - and
  **(bore / schedule 40 bore)^4.87**, the same Hazen-Williams algebra for a pipe of another bore: a
  fitting taken to lose what it loses in schedule 40 steel. Only fifteen cells of the chart came back;
  every other fitting and size is refused and asked for.
- **`hose_reels`** - the reels at once times each one's flow, and the pressure at the source from the
  most remote reel's, the rise and the friction, with the water for the time given. QCDD is reported to
  ask two hoses at every point for 15 minutes, BS 5306-1 two reels at 24 L/min each, the UAE 4.5 bar at
  the most remote - each offered, none applied.

---

## 10. The reference tables, and how far each was checked

`python brain/heron_fire.py reference` lists them; each carries its own `checked` column or note.

**How the research was done, and its limit.** Round one: two parallel readers worked the four NFPA
standards and Qatar. Round two: four more - NFPA 13's missing tables; BS EN 12845; Qatar, the UAE and
Saudi Arabia; BS 5839-1, BS 5306, BS 9990, FM Global and NFPA 13R/13D - each with 40 to 45 searches.
**Every page fetch was refused by the network** - NFPA, the industry blogs, government criteria,
document hosts, the web archive - so everything came through search-engine extracts of those pages. A
figure counts as confirmed only where a search returned it **without the figure being in the query**;
*two result sets* means two searches with different results agreed, *one* that one search returned it,
and a value the reader had typed, or only remembered, was discarded. Two more sources sat beside them:
**the owner's own firefighting knowledge file**, and the **`fluids` package's** machine-readable ASME
B36.10M tables. Confirm a figure against your own copy of the standard before it governs a project -
several of the copies the searches read were unofficial uploads, cited here by the standard, never by
their address.

| Table | Source | Checked |
|---|---|---|
| `hazard_definitions` | NFPA 13 5.2-5.4.2 (2016), 4.3.2-4.3.6 (2019) | two extracts, one for extra hazard group 2; summarised in Heron's words |
| `hazard_examples` | the annex, and the owner's file | light and ordinary group 1 one search, group 2 two, extra group 1 one; **no extra group 2 example** |
| `design_criteria` | 2022 Table 19.2.3.1.1 and 19.2.3.1.2 | the first points four sources; the second NFPA's own errata 13-22-1; hose and duration two |
| `design_area_rules` | 2016 11.2.3.2-11.2.3.3, 2022 19.2.3 | the concealed space rule an extract and the errata; 1.2 sqrt A and the dry 30 % two result sets; the high-temperature 25 % two; double-interlock, slope, quick-response and the 2000 ft2 floor one; adding the adjustments one site, two pages |
| `spacing_standard_spray` | 2022 Table 10.2.4.2.1 | light hazard 225 ft2 and ordinary 130 ft2 by an extract and the owner's file; light hazard's combustible-construction rows (168, 130, 120 ft2) one; **extra hazard's spacing not read** |
| `sprinkler_rules` | 2022 10.2.5, 10.2.6 | half the spacing, 4 in and 6 ft by extracts and the owner's file; the clearance to storage, 7 psi and 175 psi one each |
| `beam_rule_standard` | 2019 Table 10.2.7.1.2 | the first three rows two extracts, the next four one, 4 ft to 5 ft 6 in two; **past 5 ft 6 in the sources disagree** |
| `obstruction_rules` | 2019/2022 10.2.7.2.1.3, 9.5.5.3 | two extracts each, the 18 in boundary one |
| `pipe_schedule_*` | 2022 28.5 | one extract each, 3 in ordinary steel two; **five cells not held** (§7) |
| `pipe_schedule_rules` | 2022 28.5 | where it may be used and the pipe schedule supply two; the rest one |
| `steel_pipe_bores` | ASME B36.10M | the `fluids` package's tables, every row OD - 2t, schedule 40 equal to the HVAC engine's |
| `standpipe_rules` | NFPA 14 (2016, 2019) | two extracts each; the 2019 pressure-regulating text unsettled between sources |
| `fire_pump_rules`, `fire_pump_ratings` | NFPA 20 (2016-2022) | two or three extracts, the owner's file for the curve rules |
| `extinguisher_rules` | NFPA 10 Table 6.2.1.1 | two extracts |
| `detector_rules`, `heat_detector_height` | NFPA 72 (2013-2019) | two extracts; the height factors two for 0.91, 0.64 and 0.34, one for the rest |
| `qatar` | the Technical Requirements Guide (2022) and the 2015 guidelines' annex, as extracts quote them | hose reels, the tank's compartments and refill, 9 m and 65 mm landing valves, NFPA 20 and NFPA 10 two result sets; storage figures two each, **which row carries which unsettled** |
| `uae` | the UAE Fire and Life Safety Code, chapter 9 | hose reels, 4.5 bar, 65 mm, the pumps' arrangement two; 60 min two overlapping; the rest one |
| `office_firefighting` | the owner's file | read whole, 2026-10-02 |
| `en12845_criteria`, `en12845_hazards`, `en12845_rules`, `en12845_precalculated` | BS EN 12845:2015+A1:2019 | density, wet and dry areas, durations, spacing and least pressures two result sets; the walls, the least spacing, the deflector, sidewalls one; **pre-calculated OH2-OH4 wet, HHP4, HHS and Annex A not held**; A2:2026 not read |
| `fm_criteria`, `fm_hazards` | FM Global data sheet 3-26 (2019, 2020) | the densities and areas up to 30 ft two; **HC-1's dry area and HC-2 and HC-3 at 30-60 ft not held**; the definitions and DS 2-0's spacing not read |
| `c_factors_nfpa`, `c_factors_en12845` | NFPA 13's C table; EN 12845 | black steel, galvanized, iron, CPVC and copper two; **cement-lined not held** (140 and 120 both came back); EN's cast iron, mild steel, stainless and copper two |
| `k_factors` | NFPA 13 Table 6.2.3.1 (2016) | K 11.2 (160) two; the rest one; K 33.6 not found |
| `equivalent_lengths` | NFPA 13's chart (22.4.3.1.1 in 2010) | fifteen cells, one or two each; **the rest of the chart not read** |
| `temperature_ratings`, `temperature_en12845` | NFPA 13-2019 Table 7.2.4.1; EN 12845 | the F figures, colours and ceilings two; the C ratings one; EN's five bulbs two |
| `spacing_sidewall`, `spacing_extended_coverage` | NFPA 13 | one set of pages each; **the sidewall areas not read** |
| `nfpa_residential` | NFPA 13R, 13D | reference rows only |
| `bs5839_rules` | BS 5839-1 (2017, 2025) | the radii, the 500 mm, the heights, corridors, beams, call points and sounders two result sets, through makers' and the FIA's guides |
| `hose_reel_rules`, `bs9990_rules`, `bs5306_8` | QCDD, the UAE code, BS 5306-1:2006, BS EN 671-1, BS 9990:2015, Approved Document B, BS 5306-8:2023 | two result sets for most rows; each row says |
| `coverage_rules` | each row's own | each row says |

**What is applied without being a table row:** Hazen-Williams in NFPA's 4.52 and 6.05 x 10^5 forms,
**derived** in the suite from the general Hazen-Williams equation (within 0.7 % over sprinkler flows and
bores) and from each other; 0.433 psi per foot, water's own weight, which an extract's worked figure
also uses; q = K sqrt(p), the K-factor's definition. **The 1.2 in 1.2 sqrt A**, recalled in round
one, came back in round two from two result sets - still not read in the standard itself, and
NEEDS-CHECKING [Group BU](needs-checking/group-bu.md) still asks a person with the standard to confirm
it. **The bore factor in `equivalent_length`**, (bore / schedule 40 bore)^4.87, is derived here from
Hazen-Williams as the C factor is - not read.

**Not held, and asked for instead:** most of the equivalent length chart, the cement-lined C value,
K 33.6, the sidewall protection areas, extended coverage at extra hazard, the beam rule past 5 ft 6 in,
five pipe schedule cells, EN 12845's pipe size tables and its storage (HHS) design, FM Global's spacing
and HC-1's dry area, and the temperature ratings near a heat source.

---

## 11. Qatar

| | What is known |
|---|---|
| The authority's document | The **Civil Defence Technical Requirements Guide** - Ministry of Interior, General Directorate of Civil Defence, launched March 2022, updating the 2015 Fire Safety Guidelines and their annex; a copy marked 2023 circulates. Read only through search extracts |
| NFPA in Qatar | The guide adopts NFPA as its principal reference for whatever it does not set (two result sets); NFPA 10, 13, 14, 20, 22, 24, 25, 70, 72, 90A, 90B, 92, 101, 110, 111, 170, 204, 701 and 5000 are named. **Which editions was not established** - so `sprinkler_standard` is asked once per project |
| QCS | QCS 2014 **Section 23, Fire Fighting and Fire Alarm Systems**; its Part 03 is analogue addressable fire alarm systems. QCS 2024 exists; its fire section was not established |
| Which buildings | Both the height and the area limit must be met, or the next class applies; basements of two or more levels or over 1,115 m2 are sprinklered; hotels: low-rise to 15 m and 500 m2, medium to 28 m and 1,000 m2 - **whether 28 m itself is medium or high, the copies disagree** (§12) |
| Hose reels | 25 mm bore, 30 m of hose; every part reached by **two** hoses, each throwing at most 6 m; water for 15 min for at least two hoses - two result sets. Their flow and pressure were **not found**. `coverage_check` with `reached_by` 2, and `hose_reels`, take what the consultant gives |
| Standpipes | Above a highest habitable floor of 9 m; 65 mm landing valves on every level; NFPA 14 for the rest - two result sets |
| Fire pumps and the tank | Pumps sized to NFPA 20 for the largest single demand, a duty and a standby; the tank non-combustible, in two compartments of at most 50 % each, refilled in 6 hours - two result sets. `water_storage` works the compartments and the refill out for the volume, as reported |
| Water storage | The annex's own tables by building class - 24,000 US gal for 60 min at ordinary hazard, 1,500 to 3,000 gal for 30 min at low-rise light hazard, 19,500 to 60,000 gal for medium-rise - each figure two result sets, **which row carries which unsettled**, so none is applied. The owner's file: *"never quote a tank volume from memory"* |
| Extinguishers, alarms | NFPA 10 (two result sets) and NFPA 72 (one). A Qatar travel distance, detector spacing and call point distance were **not found**; BS 5839 did not appear |
| What `fire_authority` changes | With QCDD, every answer that touches a figure QCDD sets says its figure governs over the standard's, and points at `reference qatar` - what search extracts reproduce of it, no more |

**The UAE** - `fire_authority` UAE Civil Defence, `reference uae`: the UAE Fire and Life Safety Code of
Practice, chapter 9, through copies marked 2017 and 2018 - 25 mm hose reels with 30 m of hose (or a
40 mm rack) and at least 4.5 bar at the most remote; 65 mm landing valves, at most 12 bar residual and
a pressure-regulating valve above 7 bar; a main electric and a standby diesel pump on the lowest level,
pumping up; storage of at least an hour of the pump set's capacity. Its hazard classes and densities
did not come back - what did was Singapore's and EN 12845's - and are not held. **Saudi Arabia**'s SBC
801 (2018, from the IBC 2015) was found, its sprinkler thresholds were not.

---

## 12. Where two sources disagree - recorded, not resolved

| | One side | The other | Who resolves |
|---|---|---|---|
| 1 | The owner's file: **kitchens** are ordinary hazard group 2 | NFPA 13's annex lists **restaurant service areas** under group 1 | the fire consultant, per kitchen; F43 item 2 asks the owner which the office follows |
| 2 | The owner's file: standpipe flow **at most 1250 gpm** | NFPA 14: **1000 gpm** where the building is sprinklered throughout, 1250 only where it is not | F43 item 2 |
| 3 | The owner's file: clearance below a deflector to storage **450 mm** | NFPA 13: **18 in (457 mm)** for standard spray sprinklers, 36 in (914 mm) for special ones - one extract, round two. The owner's figure is 7 mm less, as an SI edition may print 18 in | F43 item 2 |
| 4 | The owner's file: extinguisher travel **23 m** | NFPA 10: 75 ft, **22.9 m** | F43 item 2 - 0.14 m, on the side of more travel |
| 5 | The owner's file: jockey pump **about 1 %** of the main pump's flow | NFPA 20's annex: the allowable leakage in 10 minutes or 1 gpm, and less than one sprinkler's flow | F43 item 2 |
| 6 | The owner's file: manual call point travel **30 m**, as common QCDD practice | NFPA 72: **61 m (200 ft)**. BS 5839-1: **45 m** along the route, **30 m in a straight line** where the layout is not yet known - two result sets, round two - so the owner's 30 m is BS 5839-1's design-stage figure. The Qatar searches named NFPA 72 for alarms, and BS 5839 did not appear | the project's alarm standard; F43 item 2 |
| 7 | The owner's file and the 2019 text: design area **139 m2** | The 2022 SI prints **140 m2** (1500 ft2 is 139.35 m2) | a rounding, recorded so a reader comparing figures knows |
| 8 | The owner's file: deflector **25 to 300 mm** | 1 to 12 in is **25.4 to 304.8 mm** | a rounding, on the safe side |
| 9 | NFPA 14 2013/2016: pressure-regulating devices above **175 psi** static | 2019 and later changed the thresholds, and the sources read disagree how | the edition the project follows |
| 10 | NFPA 20 to 2022: the fire pump starts **5 psi** below the jockey's start | 2025 reportedly **10 psi** | the edition the project follows |
| 11 | This engine before round two: design area adjustments **multiplied in turn** - Heron's own assumption, labelled | NFPA 13's example, through one site's two pages: each **taken on the area first selected, and added** - 2625 ft2 where in turn gives 2437.5 | **changed to the evidence** in round two; NEEDS-CHECKING BU2 asks a copy to confirm |
| 12 | Qatar's MOI summary: high-rise **from 28 m** | The 2014 and 2015 copies: medium-rise **to 28 m**, high-rise above | QCDD; held as both |
| 13 | Qatar's MOI summary: a pump room of at least **9 m2**, its least side 2.5 m | The 2014 and 2015 copies: a 0.8 m working width and least sides of **2, 3 or 4 m** by system | the edition in force; neither held as a rule |
| 14 | Suppliers' pages: a UAE extinguisher within **25 m** | The same pages: the UAE adopts NFPA 10 - **22.9 m** | the UAE code itself; neither held |
| 15 | Both FM Global summaries: HC-1's dry area **1500 ft2**, as wet | HC-2 and HC-3 grow from 2500 to **3500 ft2** dry | data sheet 3-26; not held |
| 16 | EN 12845 pre-calculated, wet OH2: **750 L/min at 1.3 bar** in one search | **1100 L/min at 1.7 bar** in another - one set physically odd | the standard; OH2 to OH4 wet not held |
| 17 | NFPA 13 cement-lined iron: C **140** | C **120**, labelled underground | the standard; not held |
| 18 | NFPA 13's beam rule from 5 ft 6 in: one table **goes on rising** (20, 24, 30, 35 in) | another **stops at 18 in** | the standard; held to 5 ft 6 in |

---

## 13. What is not built

- **The tables that could not be checked** (§10) - the first thing a session with page access, or the
  owner's copies of the standards, should fill. Each is a question today, not a gap in an answer.
- **Sidewall, extended-coverage, residential, ESFR and storage sprinklers** - sidewall and extended
  coverage spacing are reference rows, offered; their obstruction rules, ESFR, CMSA, in-rack and
  storage design are their own and their listings', and none was read. EN 12845's high hazard storage
  (HHS) is the same.
- **EN 12845's pre-calculated pipe sizes** - the design points at the control valve are held
  (`en12845_precalculated`), the pipe size tables are not, so an EN system is sized by `hydraulic`.
- **FM Global's spacing and its other data sheets** - only DS 3-26's table came back.
- **The temperature rating was built again in round two**, from NFPA 13-2019 Table 7.2.4.1 as two
  result sets reproduce it; the rating near a heat source is not built.
- **A hydraulic calculation read straight from Revit.** Today the host reads the network
  (`READ_MEP_SYSTEM`, `REPORT_CONNECTOR_LOADS`) and hands it to `hydraulic`. A fragment would carry the
  same arithmetic in C# - a second home for one fact - so it is a decision, F43 item 4. **Since
  2026-10-04 the network is read from Revit without that:** `REPORT_SPRINKLER_NETWORK` reads one system
  and the brain joins it and hands it to this same `hydraulic`, in the Companion's Sprinkler panel -
  [46](46-sprinkler-hydraulics-from-the-model.md). The arithmetic still has one home, here.
- **Velocity pressure, the normal-pressure method, and pumps inside the network.** The solver is the
  total-pressure method with the source as the one boundary; a pump is checked on its own against the
  demand.
- **Skills.** `sprinkler-layout` names capabilities, and this is an MCP tool - the same question F42
  item 4 asks for HVAC.

---

## 14. How it was checked, and what would prove it

[`tests/test_fire.py`](../tests/test_fire.py) holds: Hazen-Williams derived from its general form and
its two NFPA constants from each other; 0.433 psi per foot as water's weight; the K-factor conversion
against K80; the supply curve through its own points; the solver against a branch walked by hand, a
two-branch tree solved by an independent shooting method, and a looped grid whose every node balances
and every pipe obeys Hazen-Williams; a worked example for every calculation; every table against its
own units and its own monotony; every calculation handed nothing computing nothing; every unread cell
refusing; no pipe and no layout undersized across sweeps; refusals and IGNORED keys; that the engine
imports nothing beyond the standard library and the HVAC engine; and D-111 - asked once, kept in the
project's own fire record, never its HVAC one. Five deliberate mutations of the engine - a constant, a
schedule cell, the solver's governing search, the dry-system question, an unread cell filled in - each
turned the suite red.

Round two added section 9: EN 12845, FM Global and the UAE's authority read however written; a class
refused across standards both ways; each standard's offers; NFPA 13's 2625 ft2 example worked
exactly; the QR, high-temperature and concealed space rules; the pipe schedule and beam rule refused
under EN and FM without calling their inputs ignored; the temperature ratings at their edges; the
fittings against a hand sum; hose reels; coverage by two devices; the BS 5839-1 radius against a
brute-force search for any grid with fewer detectors; BS 5306-8's count; QCDD's tank; and every new
table against its own units. **Six more mutations** - adjustments multiplied in turn, the radius
ignored, coverage by one device for two, a class read across standards, FM's conversion off, a dry EN
LH system not failed - each turned it red.

**None of that is a proof in [D-30](decisions/D-30.md)'s sense**, and this engine has no fragment to
prove. What would carry weight is **a comparison against a listed hydraulic program** on one real
system: the same network in that program and here, and the difference explained. Until then every
hydraulic answer says it is not a listed program, and every answer is marked a design aid.

---

## 15. For the owner

[F43](proposals/f43.md) holds these as decisions:

1. **The line in §3** - a table the modeller asks about answered as that table (the pipe schedule, the
   beam rule), while every criterion a calculation needs is asked. Keep it, or ask for every figure?
2. **Your own file against NFPA** (§12 rows 1 to 6) - which governs the office's work? Round two
   narrowed two of them: the 450 mm clearance is NFPA's 18 in, 7 mm short; the 30 m call point is
   BS 5839-1's straight-line figure, not NFPA 72's.
3. **The tables not held** (§10) - fill them from your copy of NFPA 13, or leave them asked?
4. **A hydraulic calculation that reads the pipe network from Revit itself** (§13) - worth the second
   home for the arithmetic?
5. **Sprinklers above and below a ceiling** - asked every time whether they combine in one design area.
   Should the office's practice be recorded once, as D-110 recorded the neck velocity?
6. **QCDD's own tables** (§11) - the hose reel flow and pressure, the storage by building class, the 28 m
   boundary and the pump room came through search extracts, some disagreeing. A copy of the Technical
   Requirements Guide and its annex would let them be read rather than reported.
