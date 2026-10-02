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
| *"Standpipes, extinguishers, smoke detectors?"* | NFPA 14's demand (`standpipe`), NFPA 10's count by area (`extinguishers`), NFPA 72's grid (`detector_layout`), coverage within a radius (`coverage_check`) | `PLACE_FAMILY_INSTANCES` |

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

**And a figure nobody could check is not held at all.** This was written where NFPA's pages could not
be opened (§10). A table row is held only where at least one source confirmed it; a row that could only
be remembered - fittings' equivalent lengths, the C-factor table, the temperature ratings, the minimum
operating pressure, a handful of pipe schedule cells - is left out, and the question that would have
used it is asked instead. A cell left out of a table says so the moment a question reaches it, rather
than being skipped to the next size up.

**A project's governing standards are asked once, and kept for that project** ([D-111](decisions/D-111.md)):
`sprinkler_standard` (an NFPA 13 edition, or other) and `fire_authority` (QCDD, other or none). Each
calculation asks only the one it uses - a pump question is not asked which NFPA 13 edition governs.

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

- the area after its adjustments, each one the modeller gave, applied in turn - a dry or
  double-interlock system's increase (asked whenever the system is one of those), a sloped ceiling's
  increase (asked whenever a slope is given), a quick-response reduction (only when given, only on a
  wet system);
- the sprinklers in it, area over area per sprinkler rounded up; along a branch line, **1.2 sqrt A**
  over S rounded up - NFPA 13's rectangular design area; the number of branch lines;
- each sprinkler's least flow, density x area, and with a K-factor the end sprinkler's pressure - the
  larger of (q/K)^2 and the minimum pressure given;
- the least demand of the design area's sprinklers together - a floor, which the hydraulic calculation
  always exceeds as pressure rises toward the source - and with hose streams and a duration, the least
  water.

**Sprinklers above and below a ceiling.** Give `concealed_space`. Where the space above is
**combustible and not sprinklered**, NFPA 13-2022 19.2.3.1.5 asks a design area of at least 3000 ft2
(280 m2) at the table's second density point, and the offers switch to that point. Where it **is
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
  The table is held to 4 ft (1219 mm); its rows from 4 ft out were not read in this work, and a
  distance past them is said so, with a height beyond the last allowance NOT CHECKED.
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
- **`extinguishers`** gives the least Class A count by area; **`detector_layout`** lays out spot smoke or
  heat detectors to a spacing and a wall distance, NFPA 72's first way, with a heat detector's height
  factor; **`coverage_check`** tests every sampled point of an outline against a radius - hose reels,
  travel distance, 0.7 S - and says a straight line is not a walked route.

---

## 10. The reference tables, and how far each was checked

`python brain/heron_fire.py reference` lists them; each carries its own `checked` column or note.

**How the research was done, and its limit.** Two parallel readers worked the four NFPA standards and
Qatar. **Every page fetch was refused by the network** - NFPA, the industry blogs, government criteria,
document hosts, the web archive - so everything came through search-engine extracts of those pages, and
the session's search budget ran out before the last items were reached. A figure counts as confirmed
only where a search returned it without the figure being in the query. Two more sources sat beside
them: **the owner's own firefighting knowledge file**, and the **`fluids` package's** machine-readable
ASME B36.10M tables. Confirm a figure against your own copy of the standard before it governs a project.

| Table | Source | Checked |
|---|---|---|
| `hazard_definitions` | NFPA 13 5.2-5.4.2 (2016), 4.3.2-4.3.6 (2019) | two extracts, one for extra hazard group 2; summarised in Heron's words |
| `hazard_examples` | the annex, and the owner's file | light and ordinary group 1 one search, group 2 two, extra group 1 one; **no extra group 2 example** |
| `design_criteria` | 2022 Table 19.2.3.1.1 and 19.2.3.1.2 | the first points four sources; the second NFPA's own errata 13-22-1; hose and duration two |
| `design_area_rules` | 2022 19.2.3.1.5 | one extract and the errata - **the dry, slope, quick-response and high-temperature figures were not read** |
| `spacing_standard_spray` | 2022 Table 10.2.4.2.1 | light hazard 225 ft2 and ordinary 130 ft2 by an extract and the owner's file; the rest one source; **light hazard's combustible-construction rows and extra hazard's spacing not read** |
| `sprinkler_rules` | 2022 10.2.5, 10.2.6 | half the spacing, 4 in and 6 ft by extracts and the owner's file; the clearance to storage the owner's file only |
| `beam_rule_standard` | 2019 Table 10.2.7.1.2 | the first three rows two extracts, the next four one; **from 4 ft out not read** |
| `obstruction_rules` | 2019/2022 10.2.7.2.1.3, 9.5.5.3 | two extracts each, the 18 in boundary one |
| `pipe_schedule_*` | 2022 28.5 | one extract each, 3 in ordinary steel two; **five cells not held** (§7) |
| `pipe_schedule_rules` | 2022 28.5 | where it may be used and the pipe schedule supply two; the rest one |
| `steel_pipe_bores` | ASME B36.10M | the `fluids` package's tables, every row OD - 2t, schedule 40 equal to the HVAC engine's |
| `standpipe_rules` | NFPA 14 (2016, 2019) | two extracts each; the 2019 pressure-regulating text unsettled between sources |
| `fire_pump_rules`, `fire_pump_ratings` | NFPA 20 (2016-2022) | two or three extracts, the owner's file for the curve rules |
| `extinguisher_rules` | NFPA 10 Table 6.2.1.1 | two extracts |
| `detector_rules`, `heat_detector_height` | NFPA 72 (2013-2019) | two extracts; the height factors two for 0.91, 0.64 and 0.34, one for the rest |
| `qatar` | Qatar's documents, as extracts quote them | the guide's existence and QCS 2014 Section 23 two; the hose reels one, reported |
| `office_firefighting` | the owner's file | read whole, 2026-10-02 |
| `en12845_criteria` | BS EN 12845:2015+A1:2019 | light and ordinary group 1 two, the rest one; for comparison only |

**What is applied without being a table row:** Hazen-Williams in NFPA's 4.52 and 6.05 x 10^5 forms,
**derived** in the suite from the general Hazen-Williams equation (within 0.7 % over sprinkler flows and
bores) and from each other; 0.433 psi per foot, water's own weight, which an extract's worked figure
also uses; q = K sqrt(p), the K-factor's definition. **One method constant is recalled rather than
read: the 1.2 in 1.2 sqrt A** - every hydraulic text reproduces it, none could be opened here, and
NEEDS-CHECKING [Group BU](needs-checking/group-bu.md) asks a person with the standard to confirm it.

**Not held, and asked for instead:** fittings' equivalent lengths and their C multipliers, the C-factor
table, the nominal K-factor list, the temperature ratings, the minimum operating pressure, a
sprinkler's rated pressure, sidewall and extended-coverage spacing, light hazard's
combustible-construction rows, the design area adjustments' percentages, and the beam rule past 4 ft.

---

## 11. Qatar

| | What is known |
|---|---|
| The authority's document | The **Civil Defence Technical Requirements Guide** - Ministry of Interior, General Directorate of Civil Defence, launched March 2022, updating the 2015 Fire Safety Guidelines and their annex; a copy marked 2023 circulates. Read only through search extracts |
| NFPA in Qatar | NFPA is the reference where the guide is silent - NFPA 10, 13, 14, 20, 22, 24, 101, 170 and 5000 are named. **Which editions was not established** - so `sprinkler_standard` is asked once per project |
| QCS | QCS 2014 **Section 23, Fire Fighting and Fire Alarm Systems** (Part 1 General, Part 2 Fire Alarm and Detection). QCS 2024 exists; its fire section's number was not established |
| Water storage | Set by the authority's own tables by building height, area and occupancy. **Not held**: `water_storage` asks the duration, and a QCDD project's answer says the guide governs. The owner's file: *"never quote a tank volume from memory"* |
| Hose reels | Reported - 25 mm bore, 30 m of hose, a 6 m throw - from a search extract, not the guide. `coverage_check` takes whatever radius the consultant gives |
| What `fire_authority` changes | With QCDD, every answer that touches a figure QCDD sets - storage, hose reels, classification, extinguisher and pump arrangement - says QCDD's figure governs over NFPA's and that Heron holds none of its tables |

---

## 12. Where two sources disagree - recorded, not resolved

| | One side | The other | Who resolves |
|---|---|---|---|
| 1 | The owner's file: **kitchens** are ordinary hazard group 2 | NFPA 13's annex lists **restaurant service areas** under group 1 | the fire consultant, per kitchen; F43 item 2 asks the owner which the office follows |
| 2 | The owner's file: standpipe flow **at most 1250 gpm** | NFPA 14: **1000 gpm** where the building is sprinklered throughout, 1250 only where it is not | F43 item 2 |
| 3 | The owner's file: clearance below a deflector to storage **450 mm** | NFPA 13's own figure **was not read in this work**, so whether the two agree is open - a copy of the standard decides it | F43 items 2 and 3 |
| 4 | The owner's file: extinguisher travel **23 m** | NFPA 10: 75 ft, **22.9 m** | F43 item 2 - 0.14 m, on the side of more travel |
| 5 | The owner's file: jockey pump **about 1 %** of the main pump's flow | NFPA 20's annex: the allowable leakage in 10 minutes or 1 gpm, and less than one sprinkler's flow | F43 item 2 |
| 6 | The owner's file: manual call point travel **30 m**, as common QCDD practice | NFPA 72: **61 m (200 ft)**. QCS 2014 Section 23 is reported to cite BS 5839 for alarms, and BS 5839-1 is reported at 30 m in a straight line - both from search extracts | the project's alarm standard; F43 item 2 |
| 7 | The owner's file and the 2019 text: design area **139 m2** | The 2022 SI prints **140 m2** (1500 ft2 is 139.35 m2) | a rounding, recorded so a reader comparing figures knows |
| 8 | The owner's file: deflector **25 to 300 mm** | 1 to 12 in is **25.4 to 304.8 mm** | a rounding, on the safe side |
| 9 | NFPA 14 2013/2016: pressure-regulating devices above **175 psi** static | 2019 and later changed the thresholds, and the sources read disagree how | the edition the project follows |
| 10 | NFPA 20 to 2022: the fire pump starts **5 psi** below the jockey's start | 2025 reportedly **10 psi** | the edition the project follows |

---

## 13. What is not built

- **The tables that could not be checked** (§10) - the first thing a session with page access, or the
  owner's copy of NFPA 13, should fill. Each is a question today, not a gap in an answer.
- **Sidewall, extended-coverage, residential, ESFR and storage sprinklers** - their spacing and
  obstruction rules are their own and their listings', and none was read.
- **A temperature rating lookup** - built first, then removed with its table, which could not be
  checked.
- **A hydraulic calculation read straight from Revit.** Today the host reads the network
  (`READ_MEP_SYSTEM`, `REPORT_CONNECTOR_LOADS`) and hands it to `hydraulic`. A fragment would carry the
  same arithmetic in C# - a second home for one fact - so it is a decision, F43 item 4.
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

**None of that is a proof in [D-30](decisions/D-30.md)'s sense**, and this engine has no fragment to
prove. What would carry weight is **a comparison against a listed hydraulic program** on one real
system: the same network in that program and here, and the difference explained. Until then every
hydraulic answer says it is not a listed program, and every answer is marked a design aid.

---

## 15. For the owner

[F43](proposals/f43.md) holds these as decisions:

1. **The line in §3** - a table the modeller asks about answered as that table (the pipe schedule, the
   beam rule), while every criterion a calculation needs is asked. Keep it, or ask for every figure?
2. **Your own file against NFPA** (§12 rows 1 to 6) - which governs the office's work?
3. **The tables not held** (§10) - fill them from your copy of NFPA 13, or leave them asked?
4. **A hydraulic calculation that reads the pipe network from Revit itself** (§13) - worth the second
   home for the arithmetic?
5. **Sprinklers above and below a ceiling** - asked every time whether they combine in one design area.
   Should the office's practice be recorded once, as D-110 recorded the neck velocity?
