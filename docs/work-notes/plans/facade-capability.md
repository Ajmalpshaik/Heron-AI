# Facades - every kind, what Heron covers, and what was added

> | | |
> |---|---|
> | **Type** | One-time plan, being executed. Retires as described at the bottom |
> | **Asked for** | By the owner, 2026-10-28: *"Do you have capability for facade creation in Revit? ... parametric facade, all type of facade and facade creation, everything related to the facade ... Be stronger on facade creation. All type of facade creation, facade family, family creations, everything."* |
> | **Owner of this note** | The session building it; after that, whoever proves Groups JH, JI and JJ |
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
| Diamond, hex, triangle, twisting or free-form pattern | Divided surface + pattern-based panel | C | `pattern-based-family-creation`, `divide-family-surface`, `report-divided-surfaces`, `check-surface-fit` |
| Fins, louvres, brise-soleil, blades, brackets | Adaptive or face-based family, repeated | D | `adaptive-family-creation`, `place-adaptive-components`, `place-family-on-face`, `array-elements`, profile + edge sweep |
| Rainscreen, cladding with joints, bands, reveals | Parts and wall sweeps on a wall | E | `create-parts`, `divide-parts`, `offset-part-faces`, `place-wall-sweeps`, `paint-faces` |
| Mullion profile, panel family, curtain door | Families | - | `profile-family-creation`, `pattern-based-family-creation`, `door-window-family-creation`, `load-family` |

## What this plan adds

| # | What | Group | State |
|---|---|---|---|
| 1 | `create-curtain-system` - a curtain system on a mass's side faces, all faces or its top, with the type the modeller names; panels and lines read back | [JH](../../needs-checking/group-jh.md) | DRAFT |
| 2 | `set-curtain-panel-type` - panels on one wall changed by row or column; one picked panel stays CHANGE_ELEMENT_TYPE's | [JI](../../needs-checking/group-ji.md) | DRAFT |
| 3 | `add-curtain-grid-lines` - grid lines at given offsets, or N equal bays, on one wall only, with mullions if asked | [JJ](../../needs-checking/group-jj.md) | DRAFT |
| 4 | the [`facade`](../../../brain/skills/facade.yaml) method - routes A to E | - | DRAFT |

None of it is proved. A compile is not a proof (D-30): each tool is refused for a kept change until its
group has been run on a named model with its negative case (D-117).

## Still NOT YET after it

Said by the `facade` method when asked, never worked round:

- a curtain **system type's** grid and mullions - the wall-type setter does not reach it;
- a grid line **removed**, or one segment cut, on one wall;
- a mullion moved, or its corner join changed;
- the tile pattern of a divided surface - Revit's `CurtainPanelTilePattern` is read-only;
- glazing area by orientation and each facade's bearing from true north - [doc 45 §6](../../45-five-more-downloaded-skill-files.md).

## Retiring this note

When Groups JH, JI and JJ are proved, or the owner drops one, this note is deleted. Its durable parts are
already where they live: the cards, the `facade` method and the groups.
