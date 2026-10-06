<!--
Heron-Agent:  HERON-MEP-FPD-002
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# Sprinkler layout from the model - implementation plan

> **FINAL - 2026-10-06.** Written, reviewed by an independent read of the code (19 findings, the
> engine's measured in Python here), corrected, and finalized. The **Review log** below says what each
> finding changed. **Where a task line and the log disagree, the log wins.**

**Goal:** select Spaces or Rooms of any shape, preview a checked sprinkler layout per room in the
Companion, press Apply, and the heads are placed in Revit and read back.

**Spec:** [docs/47](../../47-sprinkler-layout-from-the-model.md). Same rules as
[the Sprinkler panel's plan](sprinkler-panel-2026-10-04.md): no physics outside `heron_fire.py`; every
criterion asked with the standard's figure offered (D-33); every value labelled with its source; the page
holds data and calls hooks; plain-script tests; explicit staging; `write.enabled` never touched.

**Layout JSON format 1** is defined here, once:

```
{format: 1, document, spaces: [{id, kind: "space"|"room", number, name, level, level_elevation_m,
  area_m2, outline: [[x,y],...] | null, holes: [[[x,y],...],...], separation_edges,
  location: [x,y] | null,
  ceilings: [{id, type, height_m}], sprinklers: [{id, type, x, y, z}]}],
 sprinkler_types: [{name: "Family : Type", family, type, placement, k}],
 asked: "*" | "uniqueId;uniqueId", heads_read: true | false, findings: [text]}
```

Metres in the model's own coordinates, rounded to the millimetre; null where Revit gives nothing.

---

## Task 1 - the read: REPORT_SPRINKLER_LAYOUT_SPACES

**Folder:** `brain/fragments/report-sprinkler-layout-spaces/` (fragment.yaml, impl/any/fragment.cs,
tests/cases.yaml). `FRG-MEP-060`, DRAFT, READ, `heron-agent: HERON-REVIT-SYS-030` as FRG-MEP-059.

- [ ] Needs `doc`, `uidoc`, `spaces` (string, source request). Provides `layoutJson` (string),
  `spaceCount` (int), `findings` (IList<string>).
- [ ] `spaces` = `*`: the selection's `SpatialElement`s that are a `Space` or a `Room` (no Area). Else
  one number per line (`\n` or `;`): every Space and Room in the document with that `Number`; none ->
  finding; more than one -> finding, none taken.
- [ ] Per room: the outline and holes as REPORT_SPRINKLER_NETWORK takes them (largest loop by shoelace,
  tessellated, consecutive duplicates dropped), but `holes` kept as loops; `separation_edges` the same;
  `location` from `LocationPoint`; `level_elevation_m` from `Level.Elevation` (project elevation base,
  said); `area_m2` from ROOM_AREA.
- [ ] Ceilings: `FilteredElementCollector(doc).OfClass(typeof(Ceiling))`, the ones whose `LevelId`
  equals the room's, whose bounding box in plan holds `location`; `height_m` from
  `CEILING_HEIGHTABOVELEVEL_PARAM`. A room with no location: none read, finding.
- [ ] Sprinklers: `OST_Sprinklers` `FamilyInstance`s whose `LocationPoint` is inside the room - a
  Space by `Space.IsPointInSpace`, a Room by `Room.IsPointInRoom` (each is tested on its own; the point
  as Revit holds it).
- [ ] Types: every `FamilySymbol` of `OST_Sprinklers`: `Family.Name + " : " + Name`, `placement`
  `Family.FamilyPlacementType.ToString()`, `k` from a parameter named like K-Factor as text.
- [ ] Recompile 2020, 2024, 2027 (`python tools/check-fragments-compile.py 2020 2024 2027`).
- [ ] cases.yaml: positive (one selected Space returns its outline equal to its boundary, its ceiling,
  its heads); negative (a number matching nothing returns no space and a finding).

## Task 2 - the engine: `sprinkler_layout_room`

**File:** `brain/heron_fire.py`; `tests/test_fire.py` new section.

- [ ] `@calculation("sprinkler_layout_room", "Sprinkler layout for a room of any shape", "layout")`.
- [ ] Inputs: `outline_mm` (read_outline), `holes_mm` (optional list of outlines), `branch_angle_deg`
  (required, -360..360), the hazard and `_limits` exactly as `sprinkler_layout`, `mounting_z_mm`
  (optional; when given the points table has z), `standards.sprinkler_standard` through
  `project_standards`.
- [ ] Method: turn outline and holes by minus the angle about the outline's first point; bounding box
  W x H; for module counts `cols` from `ceil(W / smax)` and `rows` from `ceil(H / smax)` up, ordered by
  `rows * cols` then squareness, keep a module only if `W/cols <= smax`, `H/rows <= smax`,
  `(W/cols)*(H/rows) <= amax`; for each, grid start shifts 0, 1/4, 1/2 of a module each way (first the
  centred one); keep the points inside the outline and outside every hole; reject an empty set; check
  with the same per-head measure `calc_sprinkler_spacing` uses (factor that measure out to a function
  both call - `_measure_heads(outline, points, axis, limits, tol)` - so there is one copy) plus the
  farthest point against `sqrt(amax / 2)`; the first with no fail is the layout.
- [ ] Bound: stop after 400 heads in a candidate or 2000 candidates; refuse with the reason.
- [ ] None passes: the candidate with the fewest failing heads, its fails in `a.data`, a FAIL check
  saying it is not to be placed, and what to do.
- [ ] `a.data = {"points": [{"x_mm", "y_mm"}], "heads": per-head measure keyed "N1".., "passed": bool,
  "modules": {"s_m", "l_m"}, "farthest": {...}}` - all turned back to model coordinates.
- [ ] Tests: a 6 x 4 m rectangle gives the same count as `sprinkler_layout` on the same limits; an L
  (10 x 10 less 5 x 5) passes with every head inside; the same L turned 30 degrees with the angle given
  gives the same count; a hole (a 1 m column) holds no head; a 1.2 m x 20 m corridor at 4.6 m / 21 m2 /
  2.3 m passes in one line; limits nothing can meet -> FAIL, `passed` false; a missing limit asks.

## Task 3 - the runner

**File:** new `brain/heron_sprinkler_layout.py`; `tests/test_sprinkler_layout.py`.

- [ ] `read(text)` -> dict, refusing anything but format 1.
- [ ] `rooms(data)` -> per room: key (number), label, level, outline mm, holes mm, ceilings, existing
  heads, the angle offered (longest outline edge, modulo 180).
- [ ] `fields()` - the job's inputs: standard, type, deflector_mm, limits per class (offers from
  `heron_fire.spacing_offers(hazard, standard)`), per room hazard, ceiling_mm, angle_deg, add_to_existing.
- [ ] `preview(data, inputs, recorded)` -> per room `{key, status: ok | fail | asked | skipped |
  refused, answer, points (mm, with z), count, existing, asked}`. `mounting_z_mm = level_elevation_mm +
  ceiling_mm - deflector_mm`, each said. A room with heads and no tick: `skipped`.
- [ ] `plan(preview, inputs)` -> `[{level, symbol, points: "x,y,z; ..."}]` for ok rooms only;
  refuses a type whose placement is not level-based (`OneLevelBased`).
- [ ] `fingerprint(room)` - outline, holes, level, level elevation, ceilings; `same(before, after)`
  per key.
- [ ] `read_back(before, after, sent, inputs)` - per room: new heads = after minus before ids; count
  equals sent; each new head within 5 mm in plan of a point sent and within 5 mm in z; then
  `sprinkler_spacing` (through `heron_sprinkler_spacing`'s angle-turned path or directly at the room's
  angle) on every head now in the room.
- [ ] Save/load the job beside the others (`<key>.sprinkler-layout`), as the hydraulics runs are kept.
- [ ] Tests: each function above; the module imports only `heron_fire`, `heron_hvac`, the stdlib and
  its siblings; no brain module name is a prefix of another (`test_references`).

## Task 4 - the server

**Files:** `mcp/server/heron_brain.py`, `mcp/server/heron_mcp_server.py`, `mcp/server/heron_tools.py`.

- [ ] `heron_brain.sprinkler_layout(data, inputs, project)` - preview, kept standards (KEEP.record,
  discipline "fire"), the fields and offers.
- [ ] MCP tool `revit_sprinkler_layout(spaces="*", inputs="", expect_from="")`: `revit_read(
  "REPORT_SPRINKLER_LAYOUT_SPACES", "spaces=" + spaces)`, opens the panel. `heron_tools`:
  `(ANALYZE, "run_fragment_read")`.
- [ ] Hook `_sprinkler_place(job, identity)`: lock; `_moved_since(identity)`; re-read by numbers;
  `same` per room; per plan row `_through(revit_change, ...)("PLACE_FAMILY_INSTANCES",
  "symbol=...\nlevel=...\npoints=...")`; stop at the first refusal and say which were placed; re-read;
  `read_back`. `COMPANION_ACTIONS["companion_sprinkler_place"] = (MODIFY, "run_fragment_write")`.
- [ ] Hooks for preview and report wired where the sprinkler hooks are.

## Task 5 - the Companion

**Files:** `mcp/companion/heron_companion.py`, `static/companion.js`, `static/index.html`,
`static/companion.css`, `tests/test_companion.py`.

- [ ] `SprinklerLayoutPanel` (open, current, preview, place) and routes `/api/layout`,
  `/api/layout/preview`, `/api/layout/place`; the card hidden until opened.
- [ ] The page: the job table, the rooms table, per room a plan in SVG (outline, holes, existing grey,
  new OK/FAIL), Preview, Apply with a confirm naming heads and rooms, the read-back.
- [ ] Model text as text. No outgoing connection. Nothing printed.

## Task 6 - documents and gates

- [ ] docs/47 §11 built table; docs/README row 47; NEEDS-CHECKING group CG (the read, the ceiling, the
  height of a placed head - the z question, a real L-shaped room, Apply with Changes off); HANDOVER row
  and note; the proof job `tools/jobs/report-sprinkler-layout-spaces-2026-10-06.yaml`.
- [ ] All gates; browser check of the panel on a test room; push; draft PR.

---

## Review log

| # | Finding | Change |
|---|---|---|
| R1 | `IsPointInSpace` / `IsPointInRoom` test the volume: a head at ceiling height above a room's upper limit reads outside (NEEDS-CHECKING AJ5, FRAGMENT-ISSUES 5b-175) - existing heads missed, the read-back blind | The read emits every sprinkler **on the room's level inside the room's plan bounding box**, with x, y, z; the brain tests x, y against the same outline and holes the engine uses. REPORT_SPRINKLER_NETWORK's `GetSpaceAtPoint` has the same exposure - recorded, not fixed here |
| R2 | A second Apply places a second grid: the fingerprint left out the heads already there | The fingerprint holds the room's sprinkler ids and points; a previewed job is single-use, marked applied under the panel lock before the first write; `_revit_lock` is held across re-read, place and read-back |
| R3 | `ElementsNamed` matches `Family: Type` - no space before the colon (RevitFragment.cs:3384) | The read emits `Family: Type` and Apply sends exactly that; a test holds the string |
| R4 | A newline splits a value (`_values_array`); numbers repeat across Rooms and Spaces | Rooms travel from Preview to Apply by **UniqueId** inside a string value (as SET_PARAMETER_VALUES_BY_ID does), separated by `;`; `*` is the selection; `same()` refuses a room missing from the re-read |
| R5 | A farthest-point gate rejects `sprinkler_layout`'s own 3 x 4 m answer and is no NFPA rule | It stays a WARN, as in `sprinkler_spacing`; pass is S, L, area, wall and the minimum distances |
| R6 | Heads exactly on a re-entrant wall pass (`inside` counts the edge; a ray at t=0 is ignored) | `min_wall_distance_m` is **required** in `sprinkler_layout_room`, offered from the standard, and a candidate point closer than it to any edge or hole is dropped before measuring; the L test is non-aligned (10 x 10 less 4 x 6). The checker's on-wall defect is recorded in FRAGMENT-ISSUES |
| R7 | 2000 candidates x `farthest_point` is over an hour | The search measures without the farthest point and stops at a room's first failing head; candidates ordered by their clipped head count; at most 200 measured; `farthest_point` once on the winner, its samples outside the holes; `_measure_heads` refactor output-identical, test_fire run before and after |
| R8 | `Level.Elevation` vs `ProjectElevation` disagree across two fragments; whether `NewFamilyInstance` reads z as absolute is unmeasured | The read emits both elevations and each existing head's z with its Elevation from Level; points are sent at `ProjectElevation + ceiling - deflector`; the read-back prints every head's z against the height asked. The conflict is recorded in FRAGMENT-ISSUES; CG rows use a level not at zero and ask whether the family's insertion point is its deflector |
| R9 | "Add to the heads already here" cannot pass - the layout cannot see them | Left out of v1: a room with heads is shown with them and not laid out |
| R10 | Golden Rule 16: one action, one undo | **Apply places one level at a time** - one undo entry per press; the page has an Apply per level |
| R11 | Write-path details | `ok` and `failed` from the reply, never `placed`; `_apply_table`'s 700000-character refusal before a points line; `pinned.check` after each call |
| R12 | Separation lines are laid out as walls | A room with separation-line edges is refused in v1, with the reason |
| R13 | PLACE_FAMILY_INSTANCES owns "put the sprinklers in" | The read's utterances are take-off wording only; the MCP tool's docstring carries "place sprinklers in these rooms"; a ROUTING line in PLACE's card; `check-routing` run |
| R14 | An invented 5 mm tolerance; an offered angle applied | The read-back prints each head's difference at the JSON's 1 mm rounding, a difference larger than that said, never judged; the offered angle is shown and never used until given; the shift fractions said in `uses()` |
| R15 | Ceilings: outline vs bounding box; ceilings live in the link | The ceiling's bounding box in plan holds the location point - said; ceilings in a link are not read, so "asked" is the common case, and the page says so |
| R16 | Most sprinkler families are face-based | docs/47 s9 says so; such a type is listed, disabled |
| R17 | The read-back cannot use `heron_sprinkler_spacing.check` (it needs a take-off network) | `heron_fire.run("sprinkler_spacing")` directly, at the room's angle, on the heads inside by the 2D test |
| R18 | Companion: fixed page files, no `innerHTML`, registry rows | The plan is drawn in companion.js with `createElementNS`, no new static file; `COMPANION_ACTIONS` and `TOOLS` rows with tests; every route behind `_api_ok` |
| R19 | Gates | FRG-MEP-060 and group CF were free (CF was then taken by #419 first, so this group is CG); `docs/needs-checking/group-cg.md` is its own file named in the index; `run("sprinkler_layout_room", {})` comes back missing with no results |
