<!--
Heron-Agent:  HERON-MEP-FPD-002
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Sprinkler hydraulics from the model - implementation plan (phase 1)

> **FINAL - 2026-10-04.** Written, reviewed by an independent read of the code it builds on (20 defects,
> each with evidence), corrected, and finalized. The corrections are in the tasks below and listed in the
> **Review log** at the end; where a task line and the log disagree, the log wins.

**Goal:** a modeller says *"run the hydraulics on this sprinkler system"*. Heron reads the fire protection
piping system from the open Revit model, turns it into `heron_fire`'s network, shows it in a new Companion
**Sprinkler** panel with a 3D view, asks only what the model cannot say, suggests the remote area, solves
it with the existing `hydraulic` calculation, and writes a hydraulic calculation sheet (HTML, CSV, PDF).
**Nothing is written to Revit in phase 1.**

**Spec:** [docs/46](../../46-sprinkler-hydraulics-from-the-model.md). Read it first. The solver is
[docs/42 §9](../../42-fire-protection-design.md); the pattern copied is the Loads panel,
[docs/44](../../44-building-loads-from-the-model.md) and [its plan](building-loads-2026-10-04.md).

**Architecture:** one new READ fragment, `REPORT_SPRINKLER_NETWORK`, returns the raw network as one JSON
string. Everything else is Python in `brain/`: a network reader and checks (`heron_sprinkler_takeoff.py`),
a runner over the unchanged solver (`heron_sprinkler_run.py`), the 3D view's data
(`heron_sprinkler_view.py`) and the sheet (`heron_sprinkler_report.py`). `mcp/server/heron_brain.py`
gains the seams; the MCP server gains one tool, `revit_sprinkler_hydraulics`, and the panel's hooks; the
Companion gains `SprinklerPanel`, its routes, a page section and `sprinkler3d.js`.

## Global constraints

- **No new Python package.** Standard library and Heron's own `brain/` modules only.
- **No physics outside `heron_fire.py`.** Every flow, pressure and loss comes from `hydraulic`'s answer.
  The only arithmetic in the runner is the remote-area ranking (a path sum, not a hydraulic figure) and
  the fitting-to-pipe bookkeeping.
- **D-33.** Every criterion is asked, with the figure offered and named. K-factors are confirmed per
  type. Fitting equivalent lengths are asked per kind and size, with the chart's figure offered where
  `heron_fire.REFERENCES["equivalent_lengths"]` holds it.
- **Every value carries a source label** - `model`, `instruction`, `standard:<table>` - and every number
  its unit in its key name.
- **Never "compliant".** The sheet and every answer carry `heron_fire.DISCLAIMER` and `NOT_LISTED`.
- **No write to Revit.** No `revit_change`, no Finalize button.
- **The Revit API only in `revit/` and fragment `impl/`**, and not in a comment or doc outside them.
- **Five-field metadata header** on every new source file.
- **The fragment starts DRAFT.** Its proof is a run on a named model with a negative case (D-30).
- **No pytest.** Suites are plain scripts, `python tests/test_x.py`, as the loads suites are.
- **Stage explicit paths. Never `git add -A`.**

---

## Network format 1 (owned here, once)

```json
{"format": 1, "document": "...",
 "systems": [{"id": "123", "name": "FP 1", "classification": "Wet Fire Protection", "chosen": true}],
 "system": {"id": "123", "name": "FP 1", "classification": "...", "base_equipment": "456" | null},
 "elements": [
   {"id": "789", "kind": "pipe" | "fitting" | "accessory" | "sprinkler" | "equipment" | "other",
    "category": "Pipes", "family": "...", "type": "...", "type_id": "...",
    "inner_diameter_m": 0.0525 | null, "nominal_diameter_m": 0.05 | null, "length_m": 3.2 | null,
    "part_type": "Tee" | null, "angle_deg": 90.0 | null,
    "k": {"connector": 5.6 | null, "parameter": "80 L/min/bar^1/2" | null} | null,
    "level": "Level 1" | null, "space": "101 Office" | null, "space_id": "7001" | null,
    "at": [x, y, z] | null,
    "connectors": [{"id": 0, "point": [x, y, z], "radius_m": 0.025 | null,
                    "to": [["790", 1], ...]}]}],
 "spaces": [{"id": "7001", "number": "101", "name": "Office", "level": "Level 1",
             "area_m2": 24.0 | null, "outline": [[x, y], ...] | null,
             "inner_loops": 0, "separation_edges": 0}],
 "findings": ["..."]}
```

`spaces` and each head's `space_id` were added in phase 1b (2026-10-05, [the plan](fire-water-and-spacing-2026-10-05.md)):
every Space a head of the system sits in, its largest boundary loop as `outline` (metres, plan), how
many other loops it has, and how many of the outline's edges are separation lines. A read made before
phase 1b has no `spaces` key, and the spacing check says it was "not read".

`systems` lists every fire protection piping system in the model, the chosen one marked. When none is
chosen (`system` null) `elements` is empty and the chat asks which. Points are metres in model
coordinates, to the millimetre.

---

## Task 1 - the solver gives its numbers as data

**File:** `brain/heron_fire.py`, `calc_hydraulic` only; `tests/test_fire.py`.

- [ ] At the end of a solved `calc_hydraulic`, set `a.data` to:
  `{"governing": id, "demand_lpm", "source": id, "source_bar", "supply_bar" (with device loss),
  "hose_lpm", "total_lpm", "heads": {id: {"k", "q_req_lpm", "p_req_bar", "q_lpm", "p_bar"}},
  "pressures": {id: bar}, "pipes": [{"from", "to", "q_lpm" (signed, a->b as given), "v_ms",
  "loss_bar", "bore_mm", "c", "length_m", "eq_m"}], "supply": {"static_bar", "residual_bar",
  "test_flow_lpm", "at_demand_bar"} | null}`.
- [ ] Add one check to `tests/test_fire.py` in its own `check()` style: a solved two-head tree's `data`
  agrees with its own tables (demand, governing, every head's pressure) and pipe flows balance at a node.
- [ ] Nothing else in the engine changes; `python tests/test_fire.py` stays green.

## Task 2 - REPORT_SPRINKLER_NETWORK

**Files:** `brain/fragments/report-sprinkler-network/fragment.yaml`, `impl/any/fragment.cs`,
`tests/cases.yaml`.

- [ ] Contract: needs `doc` (Document), `uidoc` (UIDocument), `systemName` (string, request) - `*` means
  "the selection's system, else the only one". Provides `networkJson` (string, result), `elements` count
  (int, result), `findings` (IList<string>, accounting).
- [ ] Choose the system: the first selected element that is in a `PipingSystem`; else the system whose
  name equals `systemName`; else the only fire protection system. Fire protection = the system type's
  classification is one of FireProtectWet, FireProtectDry, FireProtectPreaction, FireProtectOther (read
  by name with `Enum.TryParse` / `ToString()` so no member is named that a release lacks).
- [ ] Walk the chosen system's `PipingNetwork` elements. For each: kind by category (Pipes, Pipe
  Fittings, Pipe Accessories, Sprinklers, Mechanical Equipment, else other); pipe inner and nominal
  diameter and length from `RBS_PIPE_INNER_DIAM_PARAM`, `RBS_PIPE_DIAMETER_PARAM`, `CURVE_ELEM_LENGTH`
  in feet to metres; fitting `PartType` and angle (`RBS_PIPE_ANGLE`... read by name, null when absent);
  sprinkler K: the first connector's `Coefficient` raw (guarded) and any instance or type parameter
  whose name holds both "K" and "Factor", as `AsValueString`; level name; Space at the point (guarded,
  null when none).
- [ ] Every connector: its `Id`, `Origin`, `Radius` when round, and `AllRefs` filtered to physical
  connectors on other elements (`ConnectorType.End`, `Curve`, `Physical`), as `[element id, connector id]`.
- [ ] Nothing defaulted - a value Revit does not give is null. Ids with `ToString()`. JSON built by hand
  with the escaping read-element-table / report-space-envelope use.
- [ ] Routing utterances that no other card owns: "run the hydraulics on this sprinkler system",
  "read the sprinkler pipe network for a hydraulic calculation", "sprinkler hydraulic take-off".
- [ ] `python tools/check-fragments-compile.py` (every release) green for it; `python tools/check-routing.py`
  green.
- [ ] Cases: positive (one wet system; counts match the System Browser), negative (no fire protection
  system: no network, `systems` empty, a finding), and the K read-back against the type's Properties.

## Task 3 - the network reader and its checks

**File:** `brain/heron_sprinkler_takeoff.py`; `tests/test_sprinkler_takeoff.py`.

- [ ] `read(raw)` - format 1 or `NetworkError`; never half.
- [ ] `sprinkler_types(n)` - `{type_key: {"name", "heads": [ids], "connector", "parameter"}}`; type_key is
  `type_id` else `family|type`.
- [ ] `fitting_rows(n)` - one row per (fitting kind, nominal mm): kind from `part_type` and `angle_deg`
  (Elbow ~90 -> "90 degree standard elbow", ~45 -> "45 degree elbow", Tee/Cross/Wye/Tap -> "tee or cross,
  flow turned 90 degrees", Transition/Union/Coupling/Cap/other -> their Revit name) and for accessories
  `heron_fire.fitting_key` of the family and type names, else the type name; size = the smallest nominal
  pipe meeting it. Count each. `key = "kind|DNnn"`.
- [ ] `network(n, k_by_type, eq_by_row, operating, source)` - the solver's `nodes` and `pipes`:
  union-find over joined connectors; a non-pipe element is one node (id = its element id); a pipe end
  joined only to another pipe is a junction node `j<pipe id>.<connector id>` (the smaller of the pair);
  an open end is `e<pipe id>.<connector id>`. Elevation = the connector's z. Operating heads carry
  `k_lpm_bar`; others are plain nodes. Each fitting's equivalent length is added to the smallest-bore
  pipe meeting it (ties: the larger element id, so it is deterministic). Returns
  `(nodes, pipes, notes)`.
- [ ] `qa(n, k_by_type, source)` - FAIL / WARN / INFO per docs/46 §4, FAIL first.
- [ ] `source_candidates(n)` - the base equipment, and every open pipe end, with its point and size.
- [ ] `fingerprint(n)` - sha256 of the network without `findings` and `document`, 16 hex.
- [ ] Tests on a hand-built format-1 network (a riser, a main, two branch lines, four heads, two tees,
  an elbow, a gate valve): node and pipe counts; a pipe-to-pipe joint becomes one junction; an elbow's
  length lands on the smaller pipe; a head with no K is FAIL; an open end is WARN and is a source
  candidate; an island is FAIL; the fingerprint ignores `findings` and changes with a diameter.

## Task 4 - the runner

**File:** `brain/heron_sprinkler_run.py`; `tests/test_sprinkler_run.py`.

- [ ] `CRITERIA` - name: (unit, why, low, high) for density_mm_min, area_per_sprinkler_m2,
  design_area_m2, min_pressure_bar, c_factor, hose_allowance_lpm, max_velocity_ms, supply_static_bar,
  supply_residual_bar, supply_test_flow_lpm, device_loss_bar, safety_margin_bar. Required: density,
  area_per_sprinkler, design_area, min_pressure, c_factor. The rest optional (the solver says what
  leaving them out means).
- [ ] `needs(n, inputs)` - the questions, each `{"input", "unit", "why", "offer"}`: missing required
  criteria (offers from `heron_fire.run("hydraulic", {})`'s own missing entries, so the offer is the
  engine's text, one home), every sprinkler type without a K, every fitting row without an equivalent
  length (offer = the chart's row from `REFERENCES["equivalent_lengths"]` when one matches kind and
  nominal inches, else "not held - from your copy of the chart or the maker's data"), and the source
  when there is no base equipment and none was chosen.
- [ ] `suggest(n, inputs)` - `ceil(design_area / area_per_sprinkler)` heads with the largest
  friction-weighted path from the source (Dijkstra, weight = (length + eq) / bore^4.87); returns ids in
  order, and the count. Needs only the network, the source and those two criteria.
- [ ] `run(n, inputs, recorded=None)` - when anything is asked: `{"status": "missing", "asked": [...]}`,
  nothing solved. Else builds the `hydraulic` input (nodes with `elevation_m`, `k_lpm_bar` and
  `area_per_sprinkler_m2` for operating heads; pipes with `bore_mm`, `length_m`,
  `equivalent_length_m`, `c_factor`; criteria) and runs `heron_fire.run("hydraulic", ..., recorded)`.
  Returns `{"run_id", "when", "status", "asked", "qa", "inputs" (labelled), "operating", "suggested",
  "source", "answer" (the engine's dict), "system", "network_fingerprint", "notes"}`. `status` is the
  engine's; a refused answer keeps its sentences.
- [ ] `confirm(result, n)` / `confirmed(result, n)` - gate 1 against the fingerprint and the K map.
- [ ] Keep runs per project: `save`, `runs`, `load` in `<knowledge>/projects/<key>.sprinkler/`, the
  network kept beside it once per fingerprint (the loads module's shape, its own folder).
- [ ] `summary_text(result)` - three to five lines for the chat: system, demand and pressure at the
  source, governing head, supply margin or "no flow test given", what is still asked.
- [ ] Tests: missing criteria ask and solve nothing; a type with no K is asked by its name; a chart
  fitting row is offered with its figure and a non-chart one is asked plainly; suggest picks the far
  heads on the test network; a full run's demand equals `heron_fire.run("hydraulic")` on the same
  hand-typed nodes and pipes (one fact, one home); a non-operating head flows nothing; confirm holds for
  the same network and breaks when a diameter changes; save/load round-trips under a temporary
  `HERON_KNOWLEDGE`.

## Task 5 - the 3D view's data

**File:** `brain/heron_sprinkler_view.py`; `tests/test_sprinkler_view.py`.

- [ ] `build(n, result=None, inputs=None)` - `{"segments": [{"id", "a": [x,y,z], "b": [x,y,z],
  "values": {...}, "colour": {mode: hex}, "info": [[label, text]]}], "points": [{"id", "at", "kind",
  "values", "colour", "info"}], "modes": [{"key", "label", "legend": [[hex, text]]}], "levels": [...]}`.
  Modes: size (by nominal), operating, velocity, flow, pressure, checks. A mode with no result yet is
  omitted. Colours and legends decided here, never in the page.
- [ ] Tests: every pipe is a segment, every head a point; with a result, an operating head is coloured
  differently from a dry one; a pipe above max velocity is the FAIL colour in `checks`.

## Task 6 - the sheet

**File:** `brain/heron_sprinkler_report.py`; `tests/test_sprinkler_report.py`.

- [ ] `html(result, network, standards=None, project_name=None)` - one self-contained page per docs/46
  §8, every model text escaped; stamped `DRAFT - THE NETWORK WAS NOT CONFIRMED` until gate 1.
- [ ] `csv_heads`, `csv_pipes`; `write(folder, network, result, ...)` writing html, two CSVs and the PDF
  through `heron_loads_report.pdf` (reused, not copied).
- [ ] Tests: the stamp present then gone after confirm; a model name with `<script>` is escaped; the
  demand in the HTML is the engine's text; a missing browser gives the HTML and says no PDF.

## Task 7 - the brain seams

**File:** `mcp/server/heron_brain.py`.

- [ ] `sprinkler_hydraulics(network_json, inputs, project=None, project_name=None, save=True)` -
  reads the network, merges the inputs with the project's last run (not asked twice), reads the
  project's FIRE standards record (D-111) as `fire()` does, runs, keeps a finished run, builds the view,
  returns `{"network", "result", "qa", "asked", "view", "said", "sources", "types", "fittings"}`. One
  audit line `design.sprinkler` with status only, never the inputs.
- [ ] `sprinkler_suggest(network, inputs)`, `sprinkler_confirm(network, result, project)`,
  `sprinkler_report(network, result, model_path, project, project_name, folder)`.

## Task 8 - the MCP tool and hooks

**File:** `mcp/server/heron_mcp_server.py`.

- [ ] `revit_sprinkler_hydraulics(system: str = "", inputs: str = "", expect_from: str = "")`: Companion
  off -> the same sentence the loads tool gives. `revit_read("REPORT_SPRINKLER_NETWORK",
  "systemName=" + (system or "*"))`; the pin's refusal stands; no system chosen -> the list of fire
  protection systems as the answer; else `brain.sprinkler_hydraulics`, open the panel, return `said`.
- [ ] Hooks: `_sprinkler_calculate(network, inputs, identity)` (brain only, refused if the model moved),
  `_sprinkler_suggest`, `_sprinkler_confirm`, `_sprinkler_report`. Set where the loads hooks are set.
- [ ] Docstring says: reads only, nothing written, the modeller confirms K and the remote area, never
  fill in a criterion.

## Task 9 - the Companion panel

**Files:** `mcp/companion/heron_companion.py`, `static/index.html`, `static/companion.js`,
`static/companion.css`, new `static/sprinkler3d.js`; `tests/test_companion.py`; `mcp/companion/README.md`.

- [ ] `SprinklerPanel` - holds data, calls hooks, nothing else: `open`, `current`, `view`, `calculate`,
  `suggest`, `confirm`, `report`, `report_file`. `SPRINKLER_PANEL` singleton.
- [ ] Routes: GET `/api/sprinkler`, `/api/sprinkler/view`; POST `/api/sprinkler/calculate`,
  `/suggest`, `/confirm`, `/report`; `/report/` files resolved through whichever panel issued the token.
  Every route behind `_api_ok()`; body length capped as the loads routes are.
- [ ] Page: a Sprinkler card (hidden until a network is open) with the sections of docs/46 §7; editable
  K, fittings, criteria, ticks; *Suggest*, *Calculate*, *The network is right*, *Report*. Model text by
  `textContent` only. `sprinkler3d.js`: lines and dots, colour by the modes the brain sent, click to
  inspect, Top / 3D / Fit, the same camera code idea as `loads3d.js` re-written for segments.
- [ ] Tests: the panel answers GONE with no hooks; calculate passes the body's inputs to the hook; the
  routes refuse without the session; `PAGES` serves `sprinkler3d.js`; the page holds no fetch outside
  its own origin (the existing check covers the new file).

## Task 10 - documents

- [ ] `docs/README.md` row for 46; `docs/42` §13 points at 46 for "a hydraulic calculation read straight
  from Revit"; `docs/40` §21 mentions the Sprinkler panel; `mcp/companion/README.md` lists the new files.
- [ ] NEEDS-CHECKING rows for the fragment, the K read-back and the solve against a listed program, in a
  new group file the way the loads rows went into group CC.
- [ ] `docs/HANDOVER.md` - where this stopped.

## Task 11 - gates

- [ ] `python tests/test_fire.py`, the four new suites, `tests/test_companion.py`.
- [ ] `python tools/check-docs.py`, `check-metadata.py`, `check-structure.py`, `check-package.py`,
  `git diff --check`; and the seven CI-only ones that can run here (`check-signatures`, `check-licence`,
  `check-narrow-errors`, `check-products`, `check-decision-titles`, `check-routing`, `check-intrusion`).
- [ ] `python tools/check-fragments-compile.py` for the new fragment.
- [ ] `tools/check-change.py --intent ... --area brain --risk low`.

---

## Review focus

1. **A network bigger than the solver** (over 400 nodes once joined) - refused in words with what to do
   (select the part of the system that holds the remote area), never truncated silently.
2. **A K-factor taken from Revit's stored value** - never; the type's K is the modeller's.
3. **A pipe with no inside diameter** (a pipe type with no segment) - FAIL, named, never a default bore.
4. **The source** - never guessed when there is no base equipment.
5. **Fittings counted twice** - a fitting joined to three pipes adds its length once.
6. **Two networks** - an island the source cannot reach is FAIL, not dropped.
7. **The page** - model text through `textContent`; the companion folder holds no BIM rule (the kind
   names, colours and legends all come from `brain/`).

---

## Review log

An independent review read the plan against `heron_fire.py`, `RevitFragment.cs`'s binder, the 2024
RevitAPI.xml, the Companion and the MCP server, and probed the solver read-only. Every finding was
accepted; what each changed:

| # | Finding | Change to the plan |
|---|---|---|
| R1 | `hydraulic` run on nothing asks only for nodes, pipes and source - its criteria offers do not come back that way, hose is never "missing" | Task 1 adds `heron_fire.hydraulic_offers(family)`, wrapping the engine's own offer functions - one home for the offer text |
| R2 | `design_area_m2`, and `area_per_sprinkler_m2` anywhere but on an operating head, come back IGNORED | The runner keeps both out of the solver's top level and puts the area on operating heads only |
| R3 | The project's sprinkler standard (`ask_once`) was never asked or kept | `needs()` asks `standards.sprinkler_standard` when the project record does not hold it; the brain seam keeps it with `KEEP.record`, as `_design` does |
| R4 | `Connector.Coefficient` is Revit's calculated coefficient, not the K-factor | The fragment reads `AssignedKCoefficient` (guarded), the member the PROVEN REPORT_CONNECTOR_LOADS reads |
| R5 | `PipingNetwork` holds pipes and fittings, not the terminals or the base equipment | The fragment walks the union of `PipingNetwork`, `Elements` and `BaseEquipment`, de-duplicated by id |
| R6 | Taps and spuds join a pipe part-way, through a Curve connector - a pipe can have more than two connectors; PartType names were wrong | Each connector is marked End or Curve. The take-off splits a pipe at its Curve connectors in order along it, sharing the length by geometry. Kinds use the real enum names; Tap* and Spud* by prefix |
| R7 | Two non-pipe elements joined directly had no id rule; two heads on one fitting would add their K | Merged node id: sprinkler, then equipment, then the rest, then the smallest id. More than one sprinkler at one node is FAIL. A reference to an element outside the system is WARN "joined to another system", and that end is open |
| R8 | 400 nodes / 600 pipes refuses most real floors | Before the solve, dead-end branches with no operating head and not the source are removed leaf by leaf - exact, they carry no flow - and the count is on the sheet. The limit is checked after |
| R9 | Pipe length is cut to cut, so every fitting's centre-to-end length was lost - errs small | Format 1 carries each non-pipe element's location point (`at`); the distance from it to each connector is added to the pipe that connector meets, and the sheet says so |
| R10 | A size held in inches (25.4 mm) does not match a DN | Sizes snap to the nearest standard nominal within 1 mm, as DN or as inches; anything else is named as not a standard size |
| R11 | The chart's cell is schedule 40 steel at C = 120, so the raw cell is the wrong figure for another pipe | The offer comes from `heron_fire.run("equivalent_length")` with the pipe's bore and the C when C is given; otherwise the raw cell, labelled "schedule 40 steel at C = 120" |
| R12 | `elements` is the name every consumer binds as `IList<Element>` | The count is `elementCount` |
| R13 | The selection must not beat an explicit name, and is unsafe when the model is not the one in front | A name wins; the selection only for `*` and only when `uidoc.Document` is `doc`; a PipingSystem picked in the System Browser counts; a finding says which route chose the system |
| R14 | "run the hydraulics on this sprinkler system" would route a chat to a raw JSON read | The card claims take-off wording only; the MCP tool's description carries the hydraulics wording (D-01) |
| R15 | No tool-registry row - the tool would be labelled destructive and the API doc count fails | `"revit_sprinkler_hydraulics": (ANALYZE, "run_fragment_read")` in `heron_tools.TOOLS`; its tests updated |
| R16 | `tests/test_companion.py` holds the page file list exactly | Widened by exactly `/sprinkler3d.js`, said in the PR, with the same no-import check loads3d.js has |
| R17 | The loads Report asks where the sheet goes; the plan did not | The Sprinkler Report mirrors it (`{"ask": true}`, `folder_hook`); `/report/` resolves a key through either panel; html and pdf only |
| R18 | The fragment-author steps were missing | Task 2 adds: id FRG-MEP-059 under HERON-REVIT-SYS-030; `python brain/heron_fragment.py`; `tools/check-declared-questions.py`; `tests/test_library_total.py`; each API member looked up in the reference assemblies; a proof job in `tools/jobs/` dry-run with `batch-prove.py`; NEEDS-CHECKING `group-ce.md` with its index row; docs/40 §13 gets the tool's row. `check-routing.py` always exits 0 - the criterion is no new crossing |
| R19 | The negative case was an empty model | Negative case: a domestic water system beside the wet one - its pipes must not appear. `second_route` added |
| R20 | Per-pipe data could not be mapped back to Revit pipes after pruning | Each `data["pipes"]` row carries `index`, its position in the input |

**Risks kept, and said on the sheet or the page:** an equal-size tee's length goes to the larger id and a
straight run through a tee is counted as a turn (errs long); the suggestion can pick heads that are not
next to each other - the modeller's ticks are the guard; one area per sprinkler and one minimum pressure
for every head (per head is phase 2); equipment with more than one connector in the system (a pump) is a
WARN; the fingerprint hashes geometry, sizes and K, never level or Space names; the chart holds few cells,
so most fitting rows are asked on the first run - the page says that is expected. Only the 2024 reference
assemblies are in this container, so the compile of other releases may be NOT RUN - said, never claimed.
