# Facades - every kind, what Heron covers, and what was added

> | | |
> |---|---|
> | **Type** | One-time plan, being executed. Retires as described at the bottom |
> | **Asked for** | By the owner, 2026-10-28: *"Do you have capability for facade creation in Revit? ... parametric facade, all type of facade and facade creation, everything related to the facade ... Be stronger on facade creation. All type of facade creation, facade family, family creations, everything."* |
> | **Owner of this note** | The session building it; after that, whoever proves Groups JI to JM |
> | **Authority** | None of its own. The fragment cards, the [`facade`](../../../brain/skills/facade.yaml) method, [NEEDS-CHECKING](../../NEEDS-CHECKING.md) and [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) are where each fact lives |

## Where it started

Searched 2026-10-28 with `brain/heron_retrieve.py` and by reading every card that names a curtain wall,
a mass face, a divided surface, an adaptive point or a part. The library already held a curtain wall's
**type** grid and mullions, a curved curtain wall on an arc, a divided surface with a pattern-based
panel, adaptive points and placement, parts, wall sweeps and edge sweeps - most of it PROVEN. Run
`grep -h '^heron-status:'` over each card named below for its state today; it is not typed here.

What nothing did, read off the cards' own NOT YET and routing lines:

| Gap | Where it was recorded |
|---|---|
| A curtain **system** on the faces of a mass - the usual route for a sloped or faceted tower | no card named it; `Creation.Document.NewCurtainSystem2` is called nowhere |
| A panel type changed by **position** - the bottom row to spandrels, column 2 to louvres | [`curtain-wall`](../../../brain/skills/curtain-wall.yaml) NOT YET; `set-curtain-wall-grid` routing: *"change the curtain panel -> NOT HERE"* |
| A grid line on **one** wall, or one wall divided into N equal panels, without changing the type | `set-curtain-wall-grid` routing: *"divide this curtain wall into six panels -> NOT HERE"*, *"add a grid line here -> NOT HERE"* |
| One method that says which route a facade takes | none - each route's method existed, nothing chose between them |
| An adaptive point's Orientation - Host and Loop System for a panel or fin on a sloped face | [`adaptive-family-creation`](../../../brain/skills/adaptive-family-creation.yaml): *"a Properties choice no Heron tool sets yet"* |
| A divided surface changed after it is made - rotated for a diamond pattern, offset, cut at each level | `divide-family-surface` makes both grids *"centred, unrotated, with no offset"* and refuses a face already divided |

Every Revit member the three new tools call was read with `tools/api-surface` on all eight releases,
2020 to 2027, on 2026-10-28: `Creation.Document.NewCurtainSystem2(ReferenceArray, CurtainSystemType)`,
`CurtainGrid.ChangePanelType(Element, ElementType)`, `CurtainGrid.AddGridLine(bool, XYZ, bool)` and
`CurtainGridLine.AddMullions(Curve, MullionType, bool)` - one signature on every release.

## Every kind of facade, and its route

The [`facade`](../../../brain/skills/facade.yaml) method is the map; this table is the same map with the
tools behind it.

| Facade | Revit element | Route | Tools |
|---|---|---|---|
| Shopfront, storefront, ribbon glazing, curved glass | Curtain wall | A | the `curtain-wall` method, then `add-curtain-grid-lines`, `set-curtain-panel-type` |
| Sloped, faceted or tapered tower skin; glazed roof with walls | Curtain system on mass faces | B | `create-curtain-system`, then `set-curtain-panel-type`; `create-mass-floors` |
| Diamond, hex, triangle, twisting or free-form pattern | Divided surface + pattern-based panel | C | `pattern-based-family-creation`, `divide-family-surface`, `set-divided-surface-layout`, `report-divided-surfaces` |
| Fins, louvres, brise-soleil, blades, brackets | Adaptive or face-based family, repeated | D | `adaptive-family-creation`, `set-adaptive-point-orientation`, `place-adaptive-components`, `place-family-on-face`, `array-elements`, a profile in a family sweep; `create-edge-sweeps` for a slab-edge band |
| Rainscreen, cladding with joints, bands, reveals | Parts and wall sweeps on a wall | E | `create-parts`, `divide-parts`, `offset-part-faces`, `place-wall-sweeps`, `paint-faces` |
| Mullion profile, panel family, curtain door | Families | - | `profile-family-creation`, `pattern-based-family-creation`, `door-window-family-creation`, `load-family` |

## What this plan adds

| # | What | Group | State |
|---|---|---|---|
| 1 | `create-curtain-system` - a curtain system on a mass's side faces, all faces or its top, with the type the modeller names; panels and lines read back | [JM](../../needs-checking/group-jm.md) | DRAFT |
| 2 | `set-curtain-panel-type` - panels on one wall changed by row or column; one picked panel stays CHANGE_ELEMENT_TYPE's | [JI](../../needs-checking/group-ji.md) | DRAFT |
| 3 | `add-curtain-grid-lines` - grid lines at given offsets, or N equal bays, on one wall only, with mullions if asked | [JJ](../../needs-checking/group-jj.md) | DRAFT |
| 4 | `set-adaptive-point-orientation` - an adaptive family's placement points turned to Instance, Host, Host and Loop System or Global, in the Family Editor; the setting a fin or panel on a sloped face needs | [JK](../../needs-checking/group-jk.md) | DRAFT |
| 5 | `set-divided-surface-layout` - an existing divided surface's grids changed: rotation (a diamond pattern), offset, justification, border tiles, cut at levels | [JL](../../needs-checking/group-jl.md) | DRAFT |
| 6 | `report-divided-surfaces` widened to version 2 - also reads the rotations, flip, mirror, indents and what cuts each surface, so those questions stop ranking the write in 5 first | [DR](../../needs-checking/group-dr.md) rows DR5a, DR5b | DRAFT |
| 7 | the [`facade`](../../../brain/skills/facade.yaml) method - routes A to E | - | DRAFT |
| 8 | `report-curtain-elements` widened to version 2 (2026-10-29, on the owner's word) - groups each wall's panels by **size**, width x height as Revit gives them, within a tolerance (5 mm unless the modeller gives one), per wall and across the walls handed in, as [doc 45 §6](../../45-five-more-downloaded-skill-files.md) recorded; a size Revit does not give is named, never guessed | [JO](../../needs-checking/group-jo.md) | DRAFT |

None of it is proved. A compile is not a proof (D-30): each tool is refused for a kept change until its
group has been run on a named model with its negative case (D-117).

## Waiting for the owner's word - each widens a PROVEN tool

A change under a PROVEN card's `impl/` makes its proof stale, so none of these was built:

| Widen | So that it | Why it matters for a facade |
|---|---|---|
| `set-curtain-wall-grid`, `set-curtain-wall-mullions` | also take a **curtain system type** | route B's grid and mullions are set by hand until then |
| `report-family-template` | reads each adaptive point's **Orientation** | nothing reads back what `set-adaptive-point-orientation` writes, except Properties |

## Still NOT YET after it

Said by the `facade` method when asked, never worked round:

- a curtain **system type's** grid and mullions - the wall-type setter does not reach it;
- a grid line **removed**, or one segment cut, on one wall;
- a mullion moved, or its corner join changed;
- the tile pattern of a divided surface - Revit's `CurtainPanelTilePattern` is read-only;
- whether each cell of a twisted divided surface is flat enough for one sheet of glass - nothing measures it;
- whether a fin or blade follows a later change to the curtain grid it was placed against - not measured;
- the curtain type's *Automatically Embed* box - no tool reads or sets it;
- glazing area by orientation and each facade's bearing from true north - [doc 45 §6](../../45-five-more-downloaded-skill-files.md).

## Retiring this note

When Groups JI to JM are proved, or the owner drops one, this note is deleted. Its durable parts are
already where they live: the cards, the `facade` method and the groups.
