<!--
Heron-Agent:  HERON-MEP-FPD-002
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Fire water and spacing per Space - implementation plan (phase 1b)

> **FINAL - 2026-10-05.** Written, reviewed by an independent read of the code (13 defects, each with
> evidence, measured against the first build), corrected, and finalized. Where a task line and the
> **Review log** disagree, the log wins. The first build was committed before the review returned; the
> fixes are the second commit.

**Goal:** the Sprinkler panel gains two sections. **Fire water**: the standpipes, hose reels, tank and
fire pump, put together with the sprinkler demand the panel solved. **Spacing**: every head checked
against its Space's outline and its hazard class's limits, room by room, from the model. Nothing is
written to Revit.

**Spec:** [docs/46 §13](../../46-sprinkler-hydraulics-from-the-model.md). Built on phase 1's
[plan](sprinkler-panel-2026-10-04.md) and its code. Same rules: no physics outside `heron_fire.py`;
every criterion asked with the standard's figure offered (D-33); every value labelled with its source;
the page holds data and calls hooks; plain-script tests; explicit staging.

---

## Task 1 - the engine gives its numbers as data, and its offers by name

**File:** `brain/heron_fire.py`; `tests/test_fire.py` section 11.

- [ ] `a.data` at the end of a solved `standpipe` (`flow_lpm`, `source_bar`), `hose_reels`
  (`flow_lpm`, `source_bar`, `volume_m3` or null), `water_storage` (`effective_m3`, `total_m3`),
  `fire_pump` (`rated_lpm`, `rated_bar`, `at_demand_bar` or null, `ok` - true when no check FAILED),
  `water_supply` (`at_demand_bar`, `margin_bar`), `sprinkler_spacing` (`heads`: id -> `s_m`, `l_m`,
  `area_m2`, `end_wall_m`, `nearest_m`, `fails` list; `lines`; `farthest`: `x_mm`, `y_mm`,
  `distance_m`).
- [ ] `spacing_offers(hazard, standard)` - {criterion: sentence} for `_limits`' five, through
  `_spacing_offer`, `_wall_offer`, `_least_spacing_offer` and the minimum wall rule - as
  `hydraulic_offers` does for the hydraulic criteria.
- [ ] `fields(calculation)` - the inputs a calculation reads, derived by running it on nothing:
  `[{input, unit, why, required}]` from its `missing` and `optional`. For the page, so it holds no list.
- [ ] Tests: each `data` agrees with the same answer's results text; the offers name the class; `fields`
  of `fire_pump` holds `rated_flow_lpm` as required.

## Task 2 - the read gives the Spaces

**File:** `brain/fragments/report-sprinkler-network/` (DRAFT, this branch's own).

- [ ] Each sprinkler carries `space_id` (the Space at its point, `ToString()`, null when none).
- [ ] A top-level `spaces`: for every Space a head of the system sits in, `{id, number, name, level,
  area_m2, outline}` - `outline` the first boundary loop of `GetBoundarySegments` with the default
  options, each curve tessellated, `[x, y]` in metres, consecutive duplicates dropped. A Space whose
  boundary cannot be read is listed with `outline` null and a finding.
- [ ] Recompile 2020, 2024, 2027; cases.yaml gains a positive (a head's Space outline equals the Space's
  boundary on plan) and a negative (a head outside every Space carries `space_id` null and is listed).
- [ ] `heron_sprinkler_takeoff.fingerprint` leaves out `space_id` and `spaces` - the spacing does not
  change the network a solve was confirmed for.

## Task 3 - spacing per Space

**File:** new `brain/heron_sprinkler_spacing.py`; `tests/test_sprinkler_spacing.py`.

- [ ] `spaces(n)` - per Space with heads: outline in mm, heads `(id, x_mm, y_mm)`; and the heads in no
  Space.
- [ ] `branch_angle(n, space)` - the length-weighted direction, modulo 180, of the horizontal pipe
  segments with both ends inside the Space's outline (doubled-angle average); `None` with no such pipe.
  Returned with the metres it was read from. A FACT of the model, shown, overridable.
- [ ] `check(n, inputs, recorded)` - for each Space: hazard from `inputs["spacing"]["hazard"][id]`,
  limits from `inputs["spacing"]["limits"][hazard]`, angle from `inputs["spacing"]["angle_deg"][id]` else
  the model's. Rotate outline and heads by minus the angle about the outline's first point, then
  `heron_fire.run("sprinkler_spacing", {outline_mm, sprinklers, branch_axis: "x", hazard, limits...})`.
  Per Space `{id, name, status: ok | fail | asked | not checked | refused, heads, answer, asked}`.
  A Space with no hazard is "not checked". No hazard given anywhere -> nothing asked but the hazards.
- [ ] `asked(n, inputs)` - each Space's hazard (offer: the standard's class names), then each class's
  limits (offers from `spacing_offers`).
- [ ] Tests: a 6 x 4 m room with four heads at 3 m passes at 4.6 m / 21 m2 and fails at 2.5 m; the same
  room turned 30 degrees, with its pipes turned too, gives the same S and L; a head outside its Space
  refuses that Space only; a Space with no hazard is "not checked", never ok; the angle read from pipes
  along y is 90.

## Task 4 - fire water

**File:** new `brain/heron_sprinkler_water.py`; `tests/test_fire_water.py`.

- [ ] `PARTS` - standpipe, hose_reels, storage, pump; each with its engine calculation and the inputs
  the runner fills (pump: demand flow and pressure; storage: sprinkler flow, hose allowance, other
  demands).
- [ ] `run(sprinkler_result, inputs, recorded)` - only after a solved sprinkler run. Included standpipe
  and hose reels run first; the total flow = sprinkler `total_lpm` + every included part ticked
  `simultaneous`; the pressure = the highest of the sprinkler `supply_bar` and those parts'
  `source_bar`. Storage when `tank` included: sprinkler flow, hose allowance and each simultaneous part
  over its own `duration_min` (asked). Pump when included: the data-sheet points and suction, with the
  total. Supply when standpipes or hose reels are included and the flow test was given: `water_supply`
  on the total. Returns `{status, asked, parts: {name: answer}, total: {flow_lpm, pressure_bar, from},
  notes}`; a part asked does not stop another.
- [ ] `fields()` - per part, `heron_fire.fields(...)` less what the runner fills.
- [ ] Tests: nothing included -> only the sprinkler demand, and the notes say each part was not
  included; a standpipe ticked simultaneous adds its flow and its higher pressure governs; not ticked,
  its flow is not added; storage equals the engine's own `water_storage` on the same figures; a pump
  short of the demand is FAIL from the engine; a missing pump curve asks the pump only.

## Task 5 - the run carries both

**Files:** `brain/heron_sprinkler_run.py`, `mcp/server/heron_brain.py`.

- [ ] `normalise` keeps `spacing` and `water` (dicts, validated as dicts; values checked by the engine).
- [ ] `run` - after the solve, `result["spacing"] = SPACING.check(...)` (also when the solve is only
  asked: spacing does not need the hydraulics) and `result["water"] = WATER.run(...)` when solved.
- [ ] `carried` carries `spacing` and `water` like the criteria, a blank clearing a kept value.
- [ ] `summary_text` adds one line for each.
- [ ] The brain seam returns `spacing_fields` (the class names offered) and `water_fields`.

## Task 6 - the view and the sheet

- [ ] `heron_sprinkler_view.build` - a `spacing` mode (OK, FAIL, not checked, no Space) on the heads;
  `outlines`: each Space's outline at its heads' height, drawn as a closed line.
- [ ] `heron_sprinkler_report` - a **Fire water** section (each part's tables and checks, or "not
  included by the modeller"; the total and how it was made) and a **Spacing** section (per Space: hazard,
  limits, result, every failing head).
- [ ] Tests in `tests/test_sprinkler_view.py`.

## Task 7 - the page

- [ ] `companion.js` - a **Spacing** section: per Space, hazard (choice), angle (with what it was read
  from), heads, result; a limits table per class used; and a **Fire water** section: per part an
  "include" tick and "runs with the sprinklers" tick, its fields from `water_fields`, its result; the
  total. Both post with Calculate.
- [ ] `sprinkler3d.js` - draws `outlines`.
- [ ] `tests/test_companion.py` - the panel passes `spacing` and `water` through; the page names both
  sections.

## Task 8 - documents and gates

- [ ] docs/46 §13 built table; NEEDS-CHECKING group CE gains rows for the Space outline and the
  spacing on a real model; HANDOVER note; docs/42 §13 line on standpipes from the model.
- [ ] All gates as phase 1; browser check of both sections; push to PR #415.

---

## Review log

| # | Finding | Change |
|---|---|---|
| R1 | The doubled-angle mean lets branch lines and cross mains (90 degrees apart) cancel: 30 x 10 m with 120 x 10 m gave 76.7 degrees - every head then FAILs | The **grid direction** is the mean of 4 theta (modulo 90); which of its two axes is the branch line is the one carrying more of the smallest pipe size; a spread grid (resultant under 0.9 of the length) is ASKED, not guessed. Test with equal main and branch lengths |
| R2 | `family_of("BS EN 12845")` is None, so an EN project got NFPA classes and offers | Every `family_of` on a said standard goes through `standard_value` first (`heron_fire.family_said`), in `hydraulic_offers`, `spacing_offers`, the spacing check and the seam |
| R3 | The farthest point comes back in the turned frame | Turned back to model coordinates before the view or the sheet; the sheet says the engine's own WARN is in the Space's turned frame |
| R4 | A network read before this phase has no `spaces`, and every head showed "in no Space" | `spaces` absent -> spacing "not read", said, and no Spacing colour. Format 1's definition gains `spaces` and `space_id` |
| R5 | A Space's edge may be a separation line, which the check holds as a wall; `loops[0]` is not promised to be the outer loop | The fragment takes the loop with the largest area, counts the others, and writes each edge's bounding element category; a Space with separation-line edges is never plain "ok" - its status says the edge is not a wall |
| R6 | `fire_authority` is asked once by every water part, and was dropped - the QCDD and UAE checks went quiet | Each part's `ask_once` is asked (`standards.fire_authority`), and every answer's standards given in the request are kept with `KEEP.record`; each calculation gets only the standards it reads |
| R7 | `fields()` lost the offers; the pump's suction is required once a demand is given | `fields()` carries each input's offer (optional inputs keep theirs too); the standpipe duration offers NFPA 14's row; the pump's suction is marked required |
| R8 | "Flows add, the highest pressure governs" was arithmetic outside the engine | A `combined_demand` calculation in `heron_fire`, with its own words and test; each part's `height_m` question names the sprinkler source it is measured from; each part's assumptions travel with the total |
| R9 | The sprinkler hose allowance and a simultaneous standpipe may be the same water | Said beside the tick and on the sheet - the modeller decides; NFPA 14's combined-system rule is not held, and is offered as "from your copy" |
| R10 | Spacing and water answers given while the hydraulics are unsolved were not kept | A run is kept when it solved OR carries spacing or water inputs |
| R11 | The Companion holder and the page offers | `_inputs` and `open` carry both sections (done in the first build); the seam returns each class's offers, so a limits table always has them; the page shows each Space's WARNs |
| R12 | No escaping test for a Space name | A Space named `<script>` on the sheet, and the page puts Space labels on screen as text |
| R13 | Smaller | A malformed spacing or water map refuses that section only; `_inside` is the engine's; the graph is built once per check; `area_m2` from square feet is said |

**Risks kept:** a head the Space's own height does not reach (uprights above the Space's upper limit,
Spaces in a link, another phase) reads "in no Space" - counted on the sheet and NEEDS-CHECKING CE8 asks
for it; a head a millimetre outside a curved Space's chord refuses that Space; the row tolerance stays
the engine's 100 mm; the kept network copy is written once per fingerprint and may hold old outlines.
