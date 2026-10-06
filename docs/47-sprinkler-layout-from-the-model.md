<!--
Heron-Agent:  HERON-MEP-FPD-002
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 47 — Sprinkler layout from the model: a room in, its sprinklers previewed, checked and placed

> **Status: A DESIGN, corrected by its plan's review ([plan](work-notes/plans/sprinkler-layout-2026-10-06.md), R1-R19), with what is built beside it (§11). Nothing in it is proven.** Asked 2026-10-06 by
> the owner, after the Sprinkler panel ([46](46-sprinkler-hydraulics-from-the-model.md)): *"Place the
> sprinkler sprinklers in the this room ... so it will auto place ... in the companion app I can select
> and it will place any any shape"*. Every fragment it adds starts DRAFT ([D-30](DECISIONS.md)). A
> sprinkler layout is life safety: the fire consultant approves it and the authority - QCDD on a Qatar
> project - approves the design. **This feature writes to Revit**, so it waits on the Changes switch
> (`write.enabled`, off by default) like every other write.

**In Revit words.** Today Heron can tell you how many sprinklers a *rectangular* room needs and give you
the points, and you place them. This feature lets you select one or more Spaces or Rooms of **any
shape**, say *"place sprinklers in these rooms"*, and see the layout in the Companion first - every head,
its spacing and its distance to the walls, OK or FAIL - with the ceiling height read from the model and
the sprinkler type picked from the ones loaded in your project. You change what you want, press
**Apply**, and Heron places the heads in Revit, reads them back from the model, and checks the spacing
again from what is really there. **It does not draw pipes.**

---

## 1. What exists, and what is missing

| | Today |
|---|---|
| **A layout for a rectangle** | Built. `heron_fire`'s `sprinkler_layout` ([42 §5](42-fire-protection-design.md)) finds the fewest heads on a grid in a length x width room, each limit ASKED with the standard's figure offered (D-33) |
| **A check of any shape** | Built. `sprinkler_spacing` measures S, L, area and wall distance of heads in any outline, as NFPA 13 does, and the farthest uncovered point. Phase 1b ([46 §13.2](46-sprinkler-hydraulics-from-the-model.md)) runs it per Space |
| **Placing** | Built and PROVEN. `PLACE_FAMILY_INSTANCES` places one type at points on a level, one undo entry. It sets no height of its own: the height has to be in the points (its TRAP note, NEEDS-CHECKING AJ4) |
| **A layout for any shape** | **Missing.** An L-shaped office gets nothing |
| **The room, its ceiling and the types, read together** | **Missing.** `READ_ROOM_GEOMETRY` gives one room's outline by name; nothing gives the ceiling over it, the sprinklers already in it, or the sprinkler types loaded |
| **A place to see it before it is placed** | **Missing** |

So this feature is **one reader, one engine calculation, one panel and one write path** made of a
fragment already proven.

---

## 2. What the owner gets - the whole flow

1. Select the Spaces or Rooms in Revit and say *"place sprinklers in these rooms"*.
2. Heron reads them (`REPORT_SPRINKLER_LAYOUT_SPACES`, which changes nothing): each one's outline and
   holes, its level, the ceilings over it, the sprinklers already in it, and every sprinkler type loaded.
3. The Companion's **Sprinkler Layout** panel opens. Per room: the hazard class (asked), the ceiling
   height (read from the model, confirmed), the branch-line direction (read from the longest wall,
   changeable), and the heads already there.
4. Once for the job: the sprinkler type (a list of the types in the project), the distance from the
   ceiling to the deflector (asked), and per hazard class the spacing limits - each with the standard's
   figure offered.
5. **Preview** lays each room out: a plan of the room with every head, coloured OK or FAIL by the same
   spacing check phase 1b runs, with the count, S, L and the wall distances.
6. **Apply** places the heads of every room on one level that passed - one undo entry - then reads
   the rooms back from Revit and checks the heads that are really there.
7. Then: draw the pipes, and the Sprinkler panel ([46](46-sprinkler-hydraulics-from-the-model.md)) runs
   the hydraulics on them.

---

## 3. Reading the model - REPORT_SPRINKLER_LAYOUT_SPACES

A new READ fragment, `FRG-MEP-060`, DRAFT. One input besides the document:

| Input | |
|---|---|
| `spaces` (string, request) | `*` = the Spaces and Rooms selected now. Otherwise their **UniqueIds**, separated by `;` - how Apply reads the same rooms again without depending on what is selected by then (a UniqueId inside a string value, as SET_PARAMETER_VALUES_BY_ID takes it; a number is not unique enough). One that matches nothing is a finding, never a guess |

It gives **one JSON string** (`layoutJson`), for the reason REPORT_SPRINKLER_NETWORK does: a list output
is cut to three items on the way back.

| Key | Holds |
|---|---|
| `spaces[]` | `id`, `kind` (`space` or `room`), `number`, `name`, `level`, `level_elevation_m`, `area_m2`, `outline` (the largest boundary loop, as the network read takes it) and `holes` (every other loop - columns, shafts), `separation_edges` |
| `spaces[].ceilings[]` | Every ceiling on the same level whose plan outline holds the room's location point: `id`, `type`, `height_m` (its Height Offset From Level) |
| `spaces[].sprinklers[]` | Every sprinkler **on the room's level inside the room's plan bounding box**: `id`, `type`, `x`, `y`, `z`, its Elevation from Level. Which are IN the room is decided by the brain in plan, against the same outline the layout uses - never by Revit's volume test, which misses a head above the room's upper limit (NEEDS-CHECKING AJ5) |
| `sprinkler_types[]` | Every loaded sprinkler type: `name` as `Family: Type` (exactly how `PLACE_FAMILY_INSTANCES` matches it), `placement` (the family's placement type), `k` (the K-Factor parameter text, shown only - never used) |
| Units | Metres throughout, in the model's own coordinates, to the millimetre - as the network read (D-20) |
| `findings[]` | A room with no boundary, an unplaced room, a room in a link (not read - §9), a number that matched nothing |

**Ceiling height is a model fact, not a design value**, so it is offered from the model - but the
modeller confirms it, because a ceiling found by the room's point may be the wrong one (a bulkhead, two
ceilings in one room). No ceiling found: asked.

---

## 4. Laying out any shape - `sprinkler_layout_room`

A new calculation in `heron_fire.py`, so the physics stays in one home.

| Input | |
|---|---|
| `outline_mm`, `holes_mm` | The room, in the model's own coordinates |
| `branch_angle_deg` | The branch lines' direction in plan. Offered: the longest outline edge's angle. Asked when not given |
| the limits | `_limits` - max spacing, max area, max wall distance, min spacing, min wall distance - exactly as `sprinkler_layout` asks them, with the hazard's figures offered |
| `mounting_z_mm` | Where the heads go: the level's elevation + the ceiling height - the deflector distance. The runner works it out from three values, each said |

**Method.** Turn the room so the branch lines run along x. Over its bounding box, try grids of `n x m`
modules - the fewest heads first - each module centred, and at each a few shifts of the grid's start.
Keep the heads that fall inside the room and outside its holes. Check each candidate with the very
`sprinkler_spacing` check, **The candidate with the fewest heads that passes is the layout**; the
farthest uncovered point is then measured once and WARNs, as it does in `sprinkler_spacing` - it is not
an NFPA rule, so it does not fail a layout. Turn it back.

**The least distance to a wall is required here**, offered from the standard: a head on a re-entrant
corner's wall would otherwise pass the check. Candidate points closer than it to any edge or hole are
dropped before measuring.

**If none passes** - a narrow corridor wing, a room shaped so a regular grid cannot meet the wall
distance - the candidate with the fewest failing heads is shown with those heads marked FAIL, and the
room is **not** placed. The answer says why and what to do: split the room, change the angle, or place
the failing heads by hand. Heron never places a layout it checked and found failing.

**Search bound.** A room needing more than 400 heads is refused, as the hydraulic solver refuses more
than 400 nodes: one Space of that size should be split.

---

## 5. The runner and the brain seam

`brain/heron_sprinkler_layout.py` holds no physics. It:

- reads the JSON (`read`), and lists rooms, ceilings, existing heads and types;
- asks per room (hazard, ceiling height, angle) and once (type, deflector distance, the standard, the
  limits per class - the same `spacing_offers` phase 1b uses);
- runs `sprinkler_layout_room` per room and keeps each answer;
- builds the Apply plan: per level and type, the points `x,y,z` in mm;
- compares the room read at Apply against the room previewed (`fingerprint`: outline, holes, level,
  ceiling heights) and refuses on a difference;
- reads back: the heads now in each room minus the heads there before = what was placed; each within
  5 mm of a point it was sent to and at the height asked; then `sprinkler_spacing` on **every** head now
  in the room - the proof comes from the model, not from the plan.

**A room that already has sprinklers** is shown with them and is not laid out in this version: a grid
laid over heads it cannot see is a clash, never a layout. **A room bounded partly by separation lines**
is refused too - the layout would keep its heads a wall's distance from a line that is not a wall.

---

## 6. The write - Apply

Through the chat's own write path, exactly as the Loads panel's Finalize:

1. Under the lock every tool holds, check the pin: the same Revit and the same project as the preview.
2. Read the rooms again **by number**, and refuse if any room's fingerprint moved since Preview.
3. **One level per Apply** - one undo entry per press (Golden Rule 16): `PLACE_FAMILY_INSTANCES` with
   `symbol`, `level` and `points` - by name and by value, never by element id and never by selection.
   The previewed job is used once: a second press places nothing.
4. Read the rooms again and run the read-back (§5).
5. Answer: rooms placed, heads per room, and the read-back - each head's difference from where it was
   sent, to the millimetre, said and never judged against a tolerance Heron made up.

The lock every tool holds is held from step 1 to step 4.

**The Changes switch.** With `write.enabled` off, the add-in refuses at step 3 and the page says so in
those words, with where the switch is. Heron never turns it on.

**The height.** `PLACE_FAMILY_INSTANCES` places at the point's z. Whether Revit takes that z as the
project elevation or adds the level's elevation to it has **not been measured** - so the read-back
compares every head's z with the height asked, and a difference is a FAIL with Revit's undo named, not
a quiet success.

**Only a level-based sprinkler type is offered for Apply.** A face-hosted type needs a ceiling face to
sit on, which `PLACE_FAMILY_INSTANCES` does not take. Such a type is listed, disabled, with the reason.

---

## 7. The Companion "Sprinkler Layout" panel

A card of its own, beside the Loads and Sprinkler panels, opened by the chat.

- **The job**: the standard, the sprinkler type, the deflector distance, the limits per hazard class used.
- **The rooms**: one row each - number, name, level, area, heads already there, hazard class, ceiling
  height (the model's offered), branch angle (the model's offered), "add to the heads already here".
- **Preview**: per room, a plan drawn to scale - the outline, holes, existing heads in grey, new heads
  green OK or red FAIL - with the count, S, L, and the farthest point when it warns.
- **Apply**: enabled when at least one room passed. A confirm names how many heads go into how many rooms.
- **After Apply**: the read-back per room.

Model text reaches the page as text, never as HTML (Golden Rule 19), as on every panel.

---

## 8. The gates

| Gate | Holds |
|---|---|
| Preview before Apply | Apply sends only a layout previewed and passed, for a room whose fingerprint has not moved |
| The Changes switch | The add-in's own; Heron never widens it |
| The pin | The same Revit and project as the preview |
| One undo per level and type | Revit's undo list names each |
| The read-back | Count, position and height of every head placed; the spacing of every head in the room |

---

## 9. What is not here, and why

| Not here | Why |
|---|---|
| Pipes | Routing branch lines and mains is a much larger job; the modeller draws them, then the Sprinkler panel runs the hydraulics |
| Obstructions - beams, ducts, lights | The grid cannot see them. `CHECK_OBSTRUCTIONS` and the engine's `obstruction` rule check them after; said in the answer |
| Face-hosted sprinkler types | `PLACE_FAMILY_INSTANCES` takes a level, not a face - and **many sprinkler families are face-based**, so this will often bite. A face needs Revit's own picking, not a typed value |
| Rooms in a linked model | The read is this model's Spaces and Rooms. An MEP model usually has its own Spaces - place those |
| Sloped ceilings, sidewall and extended-coverage heads | Each is its own rule set in NFPA 13; a grid at one height is not them |
| A layout that fails | Shown, never placed (§4) |
| A room that already has heads; a room with separation lines | Shown, not laid out (§5) |
| Ceilings in a linked model | Not read - the ceiling height is then asked, which in an MEP model is the common case |

---

## 10. Decisions taken here, each the owner's to overturn

1. **UniqueIds in a string carry the rooms from Preview to Apply** - the add-in refuses typed element
   ids, numbers repeat, and the selection may have changed by then.
2. **A failing room is never placed.** The owner may place the failing heads by hand, knowingly.
3. **A room with heads in it is left alone.**
4. **One level per Apply**, so each press is one undo entry.

---

## 11. What is built

Filled when the build lands.
