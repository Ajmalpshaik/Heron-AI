# Family modelling - every form, its constraints, and a family from a picture

> | | |
> |---|---|
> | **Type** | One-time plan, being executed. Retires as described at the bottom |
> | **Asked for** | By the owner, 2026-10-01: *"do we have any capability to create family? ... extrude, revolve, all modelling capability ... the parameters, dimensioning, locking, all kind of thing ... maybe I will give only an image"* |
> | **Owner of this note** | The session building it; after that, whoever proves Groups BL to BR |
> | **Authority** | None of its own. The fragment cards, [NEEDS-CHECKING](../../NEEDS-CHECKING.md) and [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) row 5b-261 are where each fact lives |

## Where it started

Before this plan, the library could build a family only out of **rectangular boxes locked between four
planes** - [`family-creation`](../../../brain/skills/family-creation.yaml) and its nine fragments, all
`DRAFT`, [Group AN](../../needs-checking/group-an.md). No revolve, blend, sweep, swept blend, void, round
shape or free profile existed, and nothing else in the library answered those words (searched
2026-10-01 with `brain/heron_retrieve.py`, and the duplicate and Matcher gates found nothing to start
from).

## The plan - three pull requests, each standing on the one before

| # | What | Group | State |
|---|---|---|---|
| 1 | **The forms.** `create-family-extrusion` (any shape, holes, solid or void), `-revolution`, `-blend`, `-sweep`, `-swept-blend` - FRG-GEO-038 to 042 | [BL](../../needs-checking/group-bl.md) | built, DRAFT, compiles 2020-2027 |
| 2 | **What makes them parametric, and makes a void cut.** `combine-family-forms` (join, or cut with a void), `lock-form-to-planes`, `label-family-radius`, `set-family-form-visibility`, `set-family-form-subcategory`, and `report-family-forms` to read every form's id - FRG-GEO-043 to 046, FRG-VIEW-114 and 115 | [BM](../../needs-checking/group-bm.md) | built, DRAFT, compiles 2020-2027 |
| 3 | **A family from a picture or a sketch.** The `family-creation` skill widened to every form, with a step zero - read the thing first, ask for every size once - and the order a host follows from an image: category, parameters, values, planes, labels, forms, locks, round labels, voids, connector, report, flex. Row 5b-265 | [BN](../../needs-checking/group-bn.md) | written, DRAFT |

**Stacked, not side by side.** Each adds fragments, so each changes the same derived counts and takes the
next NEEDS-CHECKING letters; two opened from `main` at once would conflict on both. The second and third
are opened against the branch before them, and say so.

**A fourth, the same day:** `set-family-form-material` (FRG-PAR-028, [Group BO](../../needs-checking/group-bo.md)) - one form's material, linked to a family material parameter or set - because the widened skill's own NOT YET line named materials and nothing could give two forms of one family different ones. Row 5b-266.

**A fifth, the same day:** `link-family-form-parameter` (FRG-PAR-029) - one form's Visible, a revolve's angles or an extrusion's ends linked to family parameters - and `set-family-plane-reference` (FRG-GEO-047) - Is Reference and Defines Origin - [Group BP](../../needs-checking/group-bp.md), because the skill's NOT YET line named Is Reference, and a part only some types have, or a bend whose angle the types choose, could not be built on one form. Row 5b-267.

**A sixth, the same day:** `draw-family-symbolic-lines` (FRG-VIEW-116) - a plan or elevation symbol - and `array-family-forms` (FRG-GEO-048) - one form repeated in a row, its count an Integer parameter - [Group BQ](../../needs-checking/group-bq.md), for the grilles, louvres and fans a picture shows. Row 5b-268.

**A seventh, the same day:** `place-nested-family` (FRG-ELE-075), `lock-nested-family-to-planes` (FRG-GEO-049) and `link-nested-type-parameter` (FRG-PAR-030) - a family inside a family - [Group BR](../../needs-checking/group-br.md). Row 5b-269. **Still NOT YET after it:** reference lines and angles - a part that swings about a pivot, because no documented call puts a form on a reference line's own plane - and a Family Type parameter that swaps a nested family's type.

## What was decided, and where each decision is recorded

- **One tool per form, not one tool with a `kind`** - a request need cannot be left out unless it is a
  `bool`, so a combined tool would make the host type values for inputs its form does not have. Row 5b-261.
- **One shape language** for every form - loops of `circle`, `rect`, `polygon` or corners with
  `arc MX,MY X,Y`, in millimetres, in the work plane's own two MODEL coordinates. Each card's purpose.
- **A void cuts nothing until combined.** The solid-cut utilities refuse an ordinary family document and
  the join utility says it is not for family documents, 2020 to 2027, in Autodesk's own remarks; the
  geometry combination is the family editor's own join and cut. Row 5b-261; built in PR 2.
- **A circle's size may be drivable after all.** The radial and diameter dimension calls exist in a
  FAMILY on every release; the "impossible on 2020 to 2024" measured in
  [16 §Break 2](../../16-version-support-strategy.md) is for projects. Whether a label on a family
  circle drives it is NEEDS REAL REVIT. Row 5b-261; asked in PR 2.

## How it was researched, so it can be done again

The RevitAPI XML documentation ships inside the `Nice3point.Revit.Api.RevitAPI` NuGet package, one per
release, next to the reference assembly `tools/check-api-surface.py` already downloads. Reading every
member's summary, remarks and exceptions across all eight answered every "which release has it" and
most "what does Revit do with it" questions without a Revit. The Revit SDK's own samples
(`GenericModelCreation`, `WindowWizard`) were read for how the calls fit together, and nothing was copied
from them. The Autodesk forum was out of reach from the cloud session.

## Closes when

Every row of Groups BL to BR has been run on the owner's PC - passed, or turned into a defect row -
and the skill's order has been followed once from a picture (BN). At that point the durable half is
already in the cards, the register and row 5b-261; this note is deleted and its row in the
[work-notes index](../README.md) struck.
